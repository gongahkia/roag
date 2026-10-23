"""Assertions against rendered native game sessions; no save/stat/terrain fixture edits."""
import json
from pathlib import Path
import time
from agent_support.workspace import atomic_json, utc
from . import driver, recordings, model


def run(repo, output=None, no_build=False):
    output = Path(output or repo.root / 'artifacts/playtest-suite' / time.strftime('%Y%m%d-%H%M%S', time.gmtime())).resolve()
    output.mkdir(parents=True, exist_ok=True)
    if (output / 'report.json').exists():
        raise ValueError('Choose a fresh suite output directory; existing evidence is preserved.')
    if not no_build:
        driver.build(repo)
    root = output / 'runs'; checks, runs = [], []
    current = None

    def check(name, condition, detail=''):
        checks.append({'name': name, 'passed': bool(condition), 'detail': detail, 'run': current})
        if current:
            path, _ = driver.location(repo, current, root); atomic_json(path / 'assertions.json', [c for c in checks if c['run'] == current])
        if not condition:
            raise AssertionError(name + ': ' + detail)

    def observe():
        return driver.observe(repo, current, root, full=True)['state']['ui']

    def act(action, accepted=True):
        result = driver.act(repo, current, action, root)
        if accepted and not result['delivered']:
            raise AssertionError('Mouse action rejected: ' + result['response']['reason'])
        return result

    def click(identifier):
        return act({'kind': 'control', 'control': identifier})

    def wait_for(predicate, budget=12):
        for _ in range(budget):
            ui = observe()
            if predicate(ui):
                return ui
            act({'kind': 'wait', 'seconds': .5})
        raise AssertionError('Wait condition not met; notice: ' + observe()['notice'])

    def dialogue():
        for _ in range(16):
            ui = observe()
            if ui['panel'] not in ('person', 'camp-story') or ui.get('dialogue', {}).get('finished'):
                return
            click('dialogue-next')
        raise AssertionError('Dialogue did not finish.')

    def start(scene):
        nonlocal current
        result = driver.start(repo, scene=scene, no_build=True, root=root); current = result['id']; runs.append(current)
        return result

    report = {'schemaVersion': 1, 'started': utc(), 'passed': False, 'scenes': ['world', 'world-battle'], 'paidModelInvoked': False,
              'scope': 'Rendered native Client mouse-state input. Isolated saves and explicit preview scenes; no fixture save edits, teleporting, free supplies or altered stats. Codex integration is tested with a local deterministic stub; no paid model is invoked.'}
    try:
        from .ordinary import run as ordinary_run
        ordinary = ordinary_run(repo, output / 'ordinary-launch')
        atomic_json(output / 'ordinary-launch/report.json', ordinary)
        for assertion in ordinary['checks']:
            check('ordinary-launch-' + assertion['name'], assertion['passed'])
        first = start('world'); path, state = driver.location(repo, current, root); ui = observe()
        check('starts-paused-alone-with-recording', ui['paused'] and len(ui['party']) == 1 and bool(ui['sessionId']))
        check('automation-has-an-isolated-slot', not Path(state['isolatedSave']).exists())
        click('panel:journal')
        check('compact-observation-retains-the-visible-journal', bool(driver.observe(repo, current, root)['state']['journal']))
        click('close'); click('panel:map')
        compact = driver.observe(repo, current, root)['state']
        check('compact-observation-retains-visible-known-places', bool(compact['places']) and all(
            any(c['id'] == 'waypoint:' + p['id'] for c in compact['controls']) for p in compact['places']))
        click('close')
        time.sleep(.25); check('thinking-does-not-advance-game-time', observe()['tick'] == ui['tick'])
        rejected = act({'kind': 'control', 'control': 'absent-button'}, accepted=False)
        check('invalid-controls-return-explicit-feedback', not rejected['delivered'] and 'not visible' in rejected['response']['reason'])
        stale = driver.act(repo, current, {'kind': 'capture'}, root, expected=0)
        check('stale-observations-cannot-act', not stale['delivered'] and observe()['position'] == ui['position'])
        unseen = act({'kind': 'tile', 'x': 4095, 'y': 4095}, accepted=False)
        check('unobserved-tiles-cannot-be-targeted', not unseen['delivered'] and 'unobserved' in unseen['response']['reason'])

        p = observe()['position']
        candidates = [c for c in observe()['cells'] if c['groundOpen'] and not c['occupied'] and not c.get('prop')
                      and abs(c['x'] - p['x']) + abs(c['y'] - p['y']) == 1]
        check('ordinary-world-has-a-free-local-step', bool(candidates))
        target = candidates[0]
        act({'kind': 'hover', 'x': target['x'], 'y': target['y']})
        check('hover-exposes-real-inspection', observe()['hover'] is not None)
        act({'kind': 'tile', 'x': target['x'], 'y': target['y']})
        wait_for(lambda u: u['position']['x'] == target['x'] and u['position']['y'] == target['y'])
        check('mouse-travel-changes-position', observe()['position'] != p)
        if not observe()['paused']:
            click('pause')

        ui = observe(); p = ui['position']
        onward = next(c for c in ui['cells'] if c['groundOpen'] and not c['occupied'] and not c.get('prop')
                      and c['y'] == p['y'] and c['x'] == p['x'] + 4)
        act({'kind': 'tile', 'x': onward['x'], 'y': onward['y']})
        moving = observe(); check('capture-fixture-has-a-pending-real-route', moving['routeCount'] > 0)
        time.sleep(.3); act({'kind': 'capture'})
        check('diagnostic-capture-does-not-advance-travel-or-time', observe()['position'] == moving['position'] and observe()['tick'] == moving['tick'])
        click('stop')
        if not observe()['paused']:
            click('pause')

        person = min((a for a in observe()['actors'] if a['kind'] == 'Person'), key=lambda a: abs(a['x'] - target['x']) + abs(a['y'] - target['y']))
        act({'kind': 'actor', 'actor': person['id']})
        wait_for(lambda u: u['panel'] == 'person', budget=20); dialogue(); click('conversation:interaction.recruit'); dialogue()
        check('real-dialogue-recruits-a-companion', len(observe()['party']) == 2)
        if observe()['panel']:
            click('close')
        check('recruits-occupy-separate-tiles', len({(a['x'], a['y'], a['floor']) for a in observe()['party']}) == 2)
        if not observe()['paused']:
            click('pause')
        ui = observe(); p = ui['position']
        clear = {(c['x'], c['y']): c for c in ui['cells'] if c['groundOpen'] and not c['occupied'] and not c.get('prop')
                 and c['material'] in ('Grass', 'Earth', 'Floor') and 2 <= abs(c['x'] - p['x']) + abs(c['y'] - p['y']) <= 5}
        dig = next((c for c in clear.values() if (c['x'] + 1, c['y']) in clear), None)
        check('local-ground-supports-a-two-tile-work-selection', dig is not None)
        act({'kind': 'drag', 'x': dig['x'], 'y': dig['y'], 'endX': dig['x'] + 1, 'endY': dig['y']})
        check('left-drag-selects-observed-tiles', len(observe()['selection']) == 2)
        act({'kind': 'tile', 'x': dig['x'], 'y': dig['y'], 'button': 3})
        click('context:build'); click('context:construct:Wall')
        wait_for(lambda u: not u['work'], budget=40)
        check('selected-walls-are-built-through-real-work', all(c['material'] == 'Wall' for c in observe()['cells']
              if c['y'] == dig['y'] and c['x'] in (dig['x'], dig['x'] + 1)))
        if not observe()['paused']:
            click('pause')
        act({'kind': 'drag', 'x': dig['x'], 'y': dig['y'], 'endX': dig['x'] + 1, 'endY': dig['y']})
        act({'kind': 'tile', 'x': dig['x'], 'y': dig['y'], 'button': 3})
        check('selection-has-contextual-digging', any(c['id'] == 'context:dig' and c['enabled'] for c in observe()['controls']))
        click('context:dig')
        check('contextual-dig-enqueues-real-work', any(o['kind'] == 'Dig' for o in observe()['work']))
        if not observe()['paused']:
            click('pause')
        click('panel:game'); click('mark-moment')
        check('marker-mouse-control-returns-to-world', observe()['panel'] == '' and 'Moment marked' in observe()['notice'])
        click('save'); check('explicit-save-writes-only-the-isolated-slot', Path(state['isolatedSave']).is_file())
        click('load'); check('real-load-success-is-explicit', observe()['notice'].startswith('World restored'))
        driver.feedback(repo, current, 'Suite retained an actual mouse-driven expedition and contextual work order.', root=root)
        driver.stop(repo, current, root)
        sessions = recordings.sessions(path / 'recordings'); recording = recordings.report(path / 'recordings', sessions[0]['id'])
        marker = recording['summary']['markers'][0]
        check('marker-retains-image-and-state', (Path(recording['folder']) / marker['snapshot']).is_file() and (Path(recording['folder']) / marker['screenshot']).is_file())
        check('ordinary-rule-events-and-performance-are-recorded', recording['summary']['metrics'].get('tilesMoved', 0) >= 1 and recording['summary']['counts'].get('performance', 0) >= 1)
        check('normal-close-flushes-recording', recording['summary']['status'] == 'closed' and recording['summary']['droppedEvents'] == 0)

        start('world-battle'); ui = observe()
        check('combat-scene-exposes-intents-and-sprites', ui['battle'] is not None and bool(ui['combatIntents']) and len(ui['combatPresentation']['units']) == 8)
        wait_for(lambda u: u['battle'] is None or u['battle']['activeFriendly'], budget=30)
        ui = observe(); before_health = sum(u['health'] for u in ui['combatPresentation']['units'] if not u['friendly'])
        skill = next(s for s in ui['skills'] if s['target'] == 'Enemy' and not s['reason'] and not s['cooperative'])
        click('skill:' + str(skill['index'])); foe = next(u for u in observe()['battle']['units'] if not u['friendly'] and u['alive'])
        act({'kind': 'unit', 'unit': foe['id']})
        check('mouse-ability-starts-real-presentation', observe()['combatPresentation']['busy'])
        check('agent-observation-hides-unplayed-resolution', not observe()['skills'] and not observe()['combatFeedback'] and observe()['encounterReward'] is None)
        click('combat-skip'); act({'kind': 'capture'})
        check('ability-resolves-real-damage', sum(u['health'] for u in observe()['combatPresentation']['units'] if not u['friendly']) < before_health)
        driver.act(repo, current, {'kind': 'mark', 'label': 'Combat feedback capture'}, root)
        driver.stop(repo, current, root)
        path, _ = driver.location(repo, current, root)
        session = recordings.sessions(path / 'recordings')[0]
        combat_report = recordings.report(path / 'recordings', session['id'])
        check('combat-damage-and-command-are-recorded', combat_report['summary']['metrics'].get('damage', 0) > 0 and combat_report['summary']['counts'].get('combat-action', 0) >= 1)

        start('world')
        path, _ = driver.location(repo, current, root)
        stub = output / 'codex-stub.py'
        stub.write_text('''#!/usr/bin/env python3
import sys,json
from pathlib import Path
text=sys.stdin.read()
ui=json.loads(text.split("Current observation JSON: ",1)[1])
done=ui["panel"]=="inventory"
action=None if done else dict.fromkeys(["kind","control","x","y","endX","endY","unit","actor","button","seconds","label"])
if action is not None: action.update(kind="control",control="panel:inventory")
comment={"topic":"interaction" if done else "orientation","impression":"positive" if done else "mixed",
 "observed":"The Party panel displays my character's health and class." if done else "The world starts paused with Party and Play controls.",
 "thought":"I can find my character's condition in one place." if done else "I can see how to inspect my party, but have not learned where nearby adventures are.",
 "expected":"Opening Party would explain my character's condition.","suggestion":"","confidence":"medium"}
debrief={"summary":"I found the Party panel; this short check does not cover exploration.","understanding":"The Party control opens character information.",
 "worldImpression":"I have not explored enough to judge the world.","friction":"No failed click was observed; dungeon navigation remains untested.",
 "nextAttempt":"Ask an inhabitant about nearby places and follow a clue.","wouldContinue":"unsure"} if done else None
value={"reason":"Observed the requested party panel" if done else "Open the party panel", "done":done,"action":action,"finding":None,
 "plan":{"goal":"Inspect my party","targetId":None,"nextStep":"Review" if done else "Open Party","remember":"Exploration is not tested yet."},"comments":[comment],"debrief":debrief}
Path(sys.argv[sys.argv.index("--output-last-message")+1]).write_text(json.dumps(value))
print(json.dumps({"type":"turn.completed","usage":{"input_tokens":100,"output_tokens":20}}))
''')
        stub.chmod(0o755)
        result = model.run(repo, current, 'fixture-model', 'Open the party panel and report the observed result.', max_steps=3,
                           timeout=30, decision_timeout=10, root=root, executable=str(stub))
        check('structured-model-loop-drives-and-observes-real-controls', result['status'] == 'model-finished' and result['decisions'] == 2)
        check('model-feedback-is-evidence-linked-and-labelled-opinion', driver.report(repo, current, root)['feedback'][0]['judgment'] == 'tester-opinion')
        check('model-loop-retains-usage-and-ends-client', result['usage']['input_tokens'] == 200 and not driver.observe(repo, current, root)['processAlive'])
        from . import experience, memory
        review = experience.review(path)
        check('playtester-comments-explain-observation-and-interpretation', len(review['comments']) == 2 and all(
            c['observed'] and c['thought'] and c['expected'] and c['evidenceRetained'] for c in review['comments']))
        check('playtester-reflection-admits-untested-world-coverage', 'not explored' in review['debrief']['assessment']['worldImpression'])
        check('playtester-notebook-is-readable-with-linked-evidence', '## End-of-run reflection' in (path / 'playtester-notes.md').read_text()
              and '[Screenshot]' in (path / 'playtester-notes.md').read_text())
        remembered = memory.load(path, current, 'Continue the party inspection')
        check('model-intent-and-action-survive-controller-continuation', remembered['plan']['goal'] == 'Inspect my party'
              and remembered['actions'][0]['action']['control'] == 'panel:inventory')
        check('model-review-does-not-invent-journey-completion', 'entered-dungeon' not in review['checkpoints'])
        report['passed'] = True
    except (Exception, KeyboardInterrupt) as e:
        report['error'] = type(e).__name__ + ': ' + str(e)
    finally:
        if current:
            try:
                driver.stop(repo, current, root)
            except (ValueError, OSError, TimeoutError):
                pass
        report.update(finished=utc(), checks=checks, runs=runs, output=str(output))
        atomic_json(output / 'report.json', report)
    return report
