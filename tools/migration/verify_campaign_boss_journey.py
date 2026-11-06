#!/usr/bin/env python3
"""Play a generated campaign using journal leads and ordinary rendered input.

No hidden save geometry, actor coordinates, inventory or outcomes guide play.
Saves are read only to audit exact continuation and the final canonical receipt.
A defeat, pause, blocked route or action budget is incomplete evidence.
"""
import argparse
from collections import Counter, deque
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import time

from verify_native_journey import Client, Journey, DIRECTIONS, distance, position


KINDS = ('Bellkeeper', 'Sluice-Maw', 'Threadmother', 'Ossuary Captain')
CLASS_KEYS = dict(zip((*KINDS, 'Hollow Regent'),
                      ('bellkeeper', 'sluice_maw', 'threadmother', 'ossuary_captain', 'hollow_regent')))
RANGES = dict(Ranger=4, Arcanist=3, Alchemist=3, Geomancer=3,
              Spiritbinder=3, Tinkerer=3, Stormcaller=3, Shade=2,
              Wayfinder=2, Duelist=2)


class CampaignClient(Client):
    def until(self, check):
        # Observe the same pending input during grace. Never restart or replay it.
        for attempt in range(4):
            try:
                return super().until(check)
            except TimeoutError as error:
                record = dict(error=str(error), attempt=attempt + 1, deadlineSeconds=45,
                              clientPid=self.process.pid, replayed=False)
                with (self.output / 'observation-timeouts.jsonl').open('a') as stream:
                    stream.write(json.dumps(record) + '\n')
                print('observation-timeout', json.dumps(record), flush=True)
                subprocess.run(['xdotool', 'keyup', 'w', 'a', 's', 'd', 'f', 'r', 'x',
                                'q', 'e', 'z', 'c', 'space'], check=False)
                if attempt == 3:
                    raise


def hold(client):
    if not client.ui['panel']:
        client.control_click('dungeon:menu')


def exact(client, label):
    hold(client)
    client.control_click('dungeon:save')
    before = json.loads(client.slot.read_text())
    (client.output / (label + '-before-campaign.json')).write_text(json.dumps(before, indent=2) + '\n')
    chunks = Path(str(client.slot) + '.chunks')
    hashes = {p.relative_to(chunks).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
              for p in sorted(chunks.rglob('*')) if p.is_file()}
    client.resume()
    client.control_click('dungeon:save')
    after = json.loads(client.slot.read_text())
    (client.output / (label + '-after-campaign.json')).write_text(json.dumps(after, indent=2) + '\n')
    current = {p.relative_to(chunks).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
               for p in sorted(chunks.rglob('*')) if p.is_file()}
    (client.output / (label + '-exact.json')).write_text(json.dumps(dict(
        matched=before == after and hashes == current, clock=client.ui['nativeDungeon']['clock'],
        manifestSha256=hashlib.sha256(client.slot.read_bytes()).hexdigest(),
        chunks=hashes, profile=after['state']['tactics'].get('profile')), indent=2) + '\n')
    assert before == after, 'Ordinary reload changed the complete continuing manifest'
    assert hashes == current, 'Ordinary reload changed the saved chunk files'
    return after['state']


def reports(client, label):
    before = client.ui['nativeDungeon']['clock']
    selected = client.ui['nativeDungeon']['controlledActor']
    client.control_click('dungeon:menu')
    client.control_click('dungeon:journal')
    assert client.ui['panel'] == 'dungeon-journal'
    pages = []
    # Read the static report pages, stopping before unrelated expedition history.
    while next(c for c in client.ui['controls'] if c['id'] == 'dungeon:journal-prev')['enabled']:
        client.control_click('dungeon:journal-prev')
    data = client.ui['expeditions']
    leads = list(data.get('majorBosses', []))
    entrances = list(data['entrances'])
    for page in range(40):
        pages.extend(row['text'] for row in client.ui['expeditions']['rows'])
        client.capture(label + '-page-' + str(page))
        if 'EXPEDITION HISTORY /' in ' '.join(pages):
            break
        if not next(c for c in client.ui['controls'] if c['id'] == 'dungeon:journal-next')['enabled']:
            break
        client.control_click('dungeon:journal-next')
    else:
        raise AssertionError('Journal page budget reached; no report inferred')
    client.control_click('dungeon:close')
    assert client.ui['nativeDungeon']['clock'] == before
    assert client.ui['nativeDungeon']['controlledActor'] == selected
    text = ' '.join(pages)
    match = re.search(r'CAMPAIGN OBJECTIVE / (.+?) guards .+?\((-?\d+),\s*(-?\d+)\), depth (\d+)\.', text)
    assert match, 'The displayed journal lacks an unambiguous generated final destination'
    result = dict(majorBosses=leads, entrances=entrances, text=text,
                  finalName=match[1], finalDestination=tuple(map(int, match.groups()[1:])))
    (client.output / (label + '-reports.json')).write_text(json.dumps(result, indent=2) + '\n')
    return result


class CampaignJourney(Journey):
    def __init__(self, client, members, target):
        super().__init__(client)
        self.members = members
        self.target_option = target
        self.home = position(client.ui['position'])
        self.defeated = set()
        self.encountered = set()
        self.inspected = set()
        self.valves = set()
        self.previous_fighters = {}
        self.death_effects = set()
        self.saved_casts = set()
        self.flank = None
        self.travel_dash = False
        self.route_history = deque(maxlen=80)
        self.goal = None
        self.stage = 'recruit' if len(client.ui['nativeDungeon']['player']) < members else 'travel'
        self.report = None
        self.completed = []
        if members == 1:
            assert len(client.ui['nativeDungeon']['player']) == 1, 'Solo evidence requires an actual one-member expedition'
        if client.continuation and (client.continuation / 'campaign-knowledge.json').exists():
            old = json.loads((client.continuation / 'campaign-knowledge.json').read_text())
            assert old['members'] == members and old['targetOption'] == target
            self.known = {position(c): c for c in old['cells']}
            self.visits = Counter({tuple(c['position']): c['count'] for c in old['visits']})
            self.home = tuple(old['home'])
            self.defeated = set(old['defeated'])
            self.encountered = set(old['encountered'])
            self.completed = old['completed']
            self.stage = old['stage']
            self.goal = old['goal']
            self.report = old['report']
            self.valves = {tuple(p) for p in old['valves']}
            self.receipts.update(old.get('receipts', []))

    def observe(self):
        self.ui = self.client.ui
        self.state = self.ui['nativeDungeon']
        self.player = next(p for p in self.state['player'] if p['id'] == self.state['controlledActor'])
        self.at = position(self.player['position'])
        self.visible = {position(c): c for c in self.ui['cells']}
        self.known.update(self.visible)
        self.visits[self.at] += 1
        self.fighters = [f for f in self.state['fighters'] if not f['friendly'] and f['health'] > 0]
        self.occupied = {position(a) for a in self.ui['actors']}
        self.occupied |= {position(a['position']) for a in self.state['player']
                          if a['id'] != self.player['id'] and a['life'] != 'Dead'}
        self.blocked = {position(c['position']) for c in self.state['chests'] if not c['claimed']}
        self.blocked |= {position(c['position']) for c in self.state['vaults'] if not c['opened']}
        self.blocked |= {position(g['position']) for g in self.state['gates'] if not g['open']}
        self.doors = {position(d['position']) for d in self.state['doors']}
        self.danger = {position(c) for t in self.state['telegraphs']
                       if t['due'] <= self.state['clock'] + 200 for c in t['cells']}
        # Disappearance beyond vision is never treated as a kill.
        death_effects = {position(e['position']) for e in (self.state.get('feedback') or {}).get('effects', []) if e['kind'] == 'Died'}
        for effect in (self.state.get('feedback') or {}).get('effects', []):
            if effect['kind'] == 'Died' and position(effect['position']) not in self.death_effects:
                for f in self.previous_fighters.values():
                    if not f['friendly'] and position(f['position']) == position(effect['position']):
                        if f['class'] in ('core:enemy/' + k for k in CLASS_KEYS.values()):
                            self.audit_observed_death(f)
                        else:
                            self.defeated.add(f['class'])
        self.death_effects = death_effects
        for f in self.fighters:
            if any(key in f['class'] for key in ('bellkeeper', 'sluice_maw', 'threadmother', 'ossuary_captain', 'hollow_regent')):
                self.encountered.add(f['class'])
                if f['id'] not in self.inspected:
                    p = position(f['position'])
                    if self.visible.get(p, {}).get('clickable'):
                        self.client.request('hover', x=p[0], y=p[1])
                        self.client.capture('boss-' + str(f['id']) + '-visible-inspection')
                        self.inspected.add(f['id'])
                if any(t['source'] == f['id'] for t in self.state['telegraphs']) and f['class'] not in self.saved_casts:
                    self.client.capture('boss-' + str(f['id']) + '-locked-warning')
                    exact(self.client, 'boss-' + str(f['id']) + '-pending-warning')
                    self.saved_casts.add(f['class'])
                    self.receipts.add('visible-locked-warning-exact-' + f['class'])
        self.previous_fighters = {f['id']: f for f in self.state['fighters']}

    def audit_observed_death(self, observed):
        self.client.capture('observed-death-' + str(observed['id']))
        hold(self.client)
        self.client.control_click('dungeon:save')
        state = json.loads(self.client.slot.read_text())['state']
        # Verification of this visually observed outcome only. These saved
        # coordinates and other unobserved actors never guide later navigation.
        actor = next(a for a in state['actors'] if a['id'] == observed['id'])
        assert actor['life'] == 2 and actor['health'] <= 0
        assert actor['tactics']['definitionId'] == observed['class']
        assert actor['tactics']['rewardsClaimed']
        arenas = (state['tactics'].get('campaignBosses') or {}).get('arenas', [])
        arena = next((a for a in arenas if a['boss'] == observed['id']), None)
        if 'hollow_regent' not in observed['class']:
            assert arena is not None and CLASS_KEYS[KINDS[arena['kind']]] in observed['class']
        (self.client.output / ('observed-death-' + str(observed['id']) + '-receipt.json')).write_text(json.dumps(dict(
            observedId=observed['id'], observedClass=observed['class'],
            dead=actor['life'] == 2, rewardsClaimed=actor['tactics']['rewardsClaimed'],
            arena=arena['id'] if arena else None, clock=state['tactics']['timeline']['clock']), indent=2) + '\n')
        self.defeated.add(observed['class'])
        self.receipts.add('observed-dead-reward-audit-' + observed['class'])
        self.client.key('Escape')

    def aligned(self, a, b):
        limit = RANGES.get(self.player['classId'], 1)
        if a[2] != b[2] or a[0] != b[0] and a[1] != b[1] or distance(a, b) > limit:
            return False
        dx, dy = (b[0] > a[0]) - (b[0] < a[0]), (b[1] > a[1]) - (b[1] < a[1])
        p = a[0] + dx, a[1] + dy, a[2]
        while p != b:
            if not self.known.get(p, {}).get('groundOpen') or p in self.blocked or p in self.occupied:
                return False
            p = p[0] + dx, p[1] + dy, p[2]
        return True

    def open(self, p, safe=False):
        if not super().open(p, safe):
            return False
        material = self.known[p]['material']
        if material.startswith('Stairs'):
            if self.goal is None:
                return False
            if material == 'StairsUp':
                return bool(self.goal.get('returning') or self.at[2] > self.goal['depth'])
            if self.at[2] == 0 and p != position(self.goal['entrance']):
                return False
            return not self.goal.get('returning') and (self.at[2] < self.goal['depth'])
        return True

    def move(self, p):
        origin = self.at
        actor = self.player['id']
        key = next(k for x, y, k in DIRECTIONS if (x, y) == (p[0] - origin[0], p[1] - origin[1]))
        self.select_movement()
        dash = False
        if self.travel_dash and self.at[2] == 0 and self.player['classId'] == 'Ranger' and not self.fighters:
            dx, dy = p[0] - origin[0], p[1] - origin[1]
            lane = [(origin[0] + dx * n, origin[1] + dy * n, origin[2]) for n in range(1, 4)]
            target = position(self.goal['entrance'])
            dash = all(q in self.visible and self.open(q, True)
                       and not self.visible[q]['material'].startswith('Stairs') for q in lane)
            dash = dash and distance(lane[-1], target) < distance(origin, target)
        self.client.key(('x+' if dash else '') + key)
        after = next(p for p in self.client.ui['nativeDungeon']['player'] if p['id'] == actor)
        if position(after['position']) == origin:
            self.failed[p] += 1
        elif dash and distance(origin, position(after['position'])) > 1:
            self.receipts.add('paid-visible-clear-lane-ranger-dash')
        return True

    def checkpoint(self, label='checkpoint', reload=False):
        hold(self.client)
        self.client.control_click('dungeon:save')
        (self.client.output / 'campaign-knowledge.json').write_text(json.dumps(dict(
            members=self.members, targetOption=self.target_option, home=self.home,
            cells=list(self.known.values()), visits=[dict(position=p, count=n) for p, n in self.visits.items()],
            defeated=sorted(self.defeated), encountered=sorted(self.encountered), completed=self.completed,
            stage=self.stage, goal=self.goal, report=self.report, valves=sorted(self.valves),
            receipts=sorted(self.receipts)), indent=2) + '\n')
        (self.client.output / 'handoff.json').write_text(json.dumps(dict(position=self.client.ui['position'],
            clock=self.client.ui['nativeDungeon']['clock'], player=self.client.ui['nativeDungeon']['player'],
            rollback=False)) + '\n')
        self.client.capture(label)
        if reload:
            exact(self.client, label)

    def frontier(self, target=None, town=False):
        cells = [p for p in self.known if p[2] == self.at[2] and self.open(p, True) and p != self.at
                 and any(n not in self.known for n in self.neighbors(p))
                 and (not town or max(abs(p[0] - self.home[0]), abs(p[1] - self.home[1])) <= 30)]
        cells.sort(key=lambda p: self.visits[p] * 12 + distance(self.at, p) * .2
                   + (abs(p[0] - target[0]) + abs(p[1] - target[1]) if target else distance(self.home, p) * .2))
        return any(self.go([p], True) for p in cells)

    def recruit(self):
        recruits = self.state['recruits']
        adjacent = next((r for r in recruits if distance(self.at, position(r['position'])) == 1), None)
        if adjacent:
            self.client.request('actor', actor=adjacent['actor'])
            assert self.client.ui['panel'] == 'dungeon-recruit'
            self.client.capture('natural-recruit-' + str(adjacent['actor']) + '-offer')
            self.client.control_click('dungeon:recruit')
            assert any(p['id'] == adjacent['actor'] for p in self.client.ui['nativeDungeon']['player'])
            self.receipts.add('natural-recruit-' + str(adjacent['actor']))
            return True
        if recruits and self.go([p for r in recruits for p in self.neighbors(position(r['position']))]):
            return True
        return self.frontier(town=True)

    def rumour(self):
        recruits = {r['actor'] for r in self.state['recruits']}
        people = [a for a in self.ui['actors'] if a['kind'] == 'Person' and not a['hostile'] and a['id'] not in recruits]
        adjacent = next((a for a in people if distance(self.at, position(a)) == 1), None)
        if adjacent:
            self.client.request('actor', actor=adjacent['id'])
            for _ in range(40):
                if self.client.ui['nativeDungeon']['social']['finished']:
                    break
                self.client.control_click('dungeon:social-next')
            choices = [c for c in self.client.ui['controls'] if c['id'].startswith('dungeon:social:')
                       and 'rumour' in (c['id'] + c['label']).lower() and c['enabled']]
            if not choices:
                self.client.control_click('dungeon:close')
                return False
            self.client.control_click(choices[0]['id'])
            self.client.capture('paid-entrance-rumour-' + str(self.steps))
            self.client.control_click('dungeon:close')
            self.report = reports(self.client, 'updated-public-journal-' + str(self.steps))
            return True
        return self.go([p for a in people for p in self.neighbors(position(a))]) or self.frontier(town=True)

    def choose_goal(self):
        leads = self.report['majorBosses']
        final_name = self.report['finalName'].lower().replace('-', '')
        final = next((r for r in leads if KINDS[r['kind']].lower().replace('-', '') in final_name), None)
        if self.target_option == 'all':
            remaining = [r for r in leads if KINDS[r['kind']] not in self.completed and r != final]
        elif self.target_option == 'final':
            remaining = []
        else:
            remaining = [r for r in leads if KINDS[r['kind']] == self.target_option and KINDS[r['kind']] not in self.completed]
        if remaining:
            r = min(remaining, key=lambda r: abs(r['entrance']['x'] - self.at[0]) + abs(r['entrance']['y'] - self.at[1]))
            self.goal = dict(name=KINDS[r['kind']], entrance=r['entrance'], depth=1, final=False)
        elif self.target_option not in ('all', 'final'):
            return 'target-complete'
        elif final:
            self.goal = dict(name=KINDS[final['kind']], entrance=final['entrance'], depth=1, final=True)
        else:
            match = re.search(r'Deep Complex (\d+)', self.report['text'])
            assert match, 'Final Regent objective does not identify its public complex'
            entrance = next((e for e in self.report['entrances'] if e['id'] == int(match[1])), None)
            if not entrance:
                return 'need-rumour'
            self.goal = dict(name='Hollow Regent', entrance=entrance['position'],
                             depth=self.report['finalDestination'][2], final=True)
        self.route_history.clear()
        return None

    def sustain(self):
        if self.at in self.danger:
            return False
        if self.player['health'] < self.player['maxHealth'] * .4 and self.ui['inventory'].get('medicine', 0):
            self.client.key('n')
            control = next(c for c in self.client.ui['controls'] if c['id'] == 'dungeon:medicine:' + str(self.player['id']))
            if control['enabled']:
                self.client.control_click(control['id'])
                self.receipts.add('paid-conscious-medicine')
                if self.client.ui['panel']:
                    self.client.key('Escape')
                return True
            self.client.key('Escape')
        return False

    def fight(self):
        if self.at not in self.danger:
            captain = next((f for f in self.fighters if 'ossuary_captain' in f['class']), None)
            blocked = any(e['kind'] == 'BossShieldBlocked' for e in (self.state.get('feedback') or {}).get('effects', []))
            if captain and blocked and self.flank is None:
                p = position(captain['position'])
                # The visible SHIELD response identifies a blocked approach.
                # Walk to a perpendicular observed firing lane, without reading facing.
                candidates = [q for q in self.known if self.open(q, True) and self.aligned(q, p)
                              and (q[0] == p[0] if self.at[1] == p[1] else q[1] == p[1])]
                if candidates:
                    self.flank = min(candidates, key=lambda q: distance(self.at, q))
            if self.flank:
                if self.at != self.flank and self.go([self.flank], True):
                    return True
                self.flank = None
        return super().fight()

    def quiet_tasks(self):
        for drop in self.state['loot']:
            if drop['kind'] == 0 or self.player['health'] < self.player['maxHealth']:
                if self.go([position(drop['position'])], True):
                    return True
        for chest in self.state['chests']:
            if chest['claimed'] or chest['definition'] != 'core:chest/free_elite' and self.ui['inventory'].get('gold', 0) < 5:
                continue
            p = position(chest['position'])
            if distance(self.at, p) == 1 and self.visible.get(p, {}).get('clickable'):
                self.select_movement()
                self.client.request('tile', x=p[0], y=p[1])
                if self.client.ui['panel'] == 'dungeon-chest-recipient':
                    self.client.control_click('dungeon:chest-recipient:' + str(self.player['id']))
                assert self.client.ui['panel'] == 'dungeon-chest'
                self.client.capture('natural-chest-' + str(chest['id']) + '-offers')
                offers = self.client.ui['nativeDungeon']['chestOffers']
                priorities = ('vampiric', 'lifesteal', 'mending', 'regeneration', 'second_wind', 'heart', 'damage')
                choice = min(range(len(offers)), key=lambda i: next((n for n, term in enumerate(priorities)
                                  if term in offers[i]['boon']), len(priorities)))
                self.client.control_click('dungeon:claim:' + str(choice + 1))
                self.receipts.add('ordinary-personal-boon-chest')
                return True
            if self.go(self.neighbors(p), True):
                return True
        if self.at[2] == 0 and self.ui['inventory'].get('food', 0) < 3:
            self.client.key('j')
            control = next(c for c in self.client.ui['controls'] if c['id'] == 'dungeon:work-action:Forage')
            if control['enabled']:
                self.client.control_click(control['id'])
                self.receipts.add('paid-ordinary-food-gathering')
                return True
            self.client.key('Escape')
        return False

    def fixture(self):
        props = [(p, c) for p, c in self.visible.items() if c.get('prop')]
        for p, cell in sorted(props, key=lambda item: distance(self.at, item[0])):
            name = cell['prop'].lower()
            if name == 'cistern valve' and p not in self.valves:
                if distance(self.at, p) <= 1:
                    self.client.request('hover', x=p[0], y=p[1])
                    self.client.capture('visible-valve-' + str(self.steps))
                    if 'permanently drained' not in self.client.ui['hover'].lower():
                        self.select_movement()
                        self.client.request('tile', x=p[0], y=p[1])
                        assert 'drained' in self.client.ui['notice'].lower(), self.client.ui['notice']
                        self.receipts.add('paid-visible-valve')
                    self.valves.add(p)
                    return True
                if self.go(self.neighbors(p), True):
                    return True
            if name in ('funeral resonator', 'web anchor', 'reeling line', 'cuttable partition web', 'bone rampart'):
                if self.aligned(self.at, p):
                    dx, dy = (p[0] > self.at[0]) - (p[0] < self.at[0]), (p[1] > self.at[1]) - (p[1] < self.at[1])
                    key = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
                    self.client.key('f+' + key)
                    self.receipts.add('attacked-visible-' + name.replace(' ', '-'))
                    return True
                lanes = [q for q in self.known if self.open(q, True) and self.aligned(q, p)]
                if self.go(lanes, True):
                    return True
        return False

    def rescue(self):
        down = [p for p in self.state['player'] if p['life'] == 'Downed' and p['position']['floor'] == self.at[2]]
        if not down or not self.ui['inventory'].get('medicine', 0):
            return False
        target = min(down, key=lambda p: (p['bleedTurns'], distance(self.at, position(p['position']))))
        p = position(target['position'])
        if distance(self.at, p) == 1:
            key = next(k for x, y, k in DIRECTIONS if (x, y) == (p[0] - self.at[0], p[1] - self.at[1]))
            self.client.key('h+' + key)
            assert next(p for p in self.client.ui['nativeDungeon']['player'] if p['id'] == target['id'])['life'] == 'Alive'
            self.receipts.add('paid-player-rescue')
            self.client.capture('actual-paid-rescue-' + str(self.steps))
            return True
        return self.go(self.neighbors(p), True)

    def dig(self, target):
        candidates = [(p, c) for p, c in self.visible.items() if distance(self.at, p) == 1
                      and c['material'] in ('Rock', 'Wall') and not c.get('prop')]
        candidates.sort(key=lambda item: abs(item[0][0] - target[0]) + abs(item[0][1] - target[1]))
        if not candidates:
            return False
        p = candidates[0][0]
        direction = next(name for (dx, dy, _), name in zip(DIRECTIONS, ('North', 'East', 'South', 'West'))
                         if (dx, dy) == (p[0] - self.at[0], p[1] - self.at[1]))
        self.client.key('j')
        self.client.control_click('dungeon:work-target:' + direction)
        control = next(c for c in self.client.ui['controls'] if c['id'] == 'dungeon:work-action:Dig')
        if not control['enabled']:
            self.client.key('Escape')
            return False
        self.client.capture('observed-excavation-offer-' + str(self.steps))
        self.client.control_click(control['id'])
        self.receipts.add('paid-observed-excavation')
        return True

    def navigate(self):
        self.travel_dash = True
        entrance = position(self.goal['entrance'])
        depth = self.goal['depth']
        if self.at[2] == 0:
            target = entrance
        elif self.at[2] < depth:
            stairs = [p for p, c in self.known.items() if p[2] == self.at[2] and c['material'] == 'StairsDown']
            if self.go(stairs, True):
                return True
            target = tuple(self.report['finalDestination'])
        elif self.at[2] > depth or self.goal.get('returning'):
            stairs = [p for p, c in self.known.items() if p[2] == self.at[2] and c['material'] == 'StairsUp']
            if self.go(stairs, True):
                return True
            target = entrance
        else:
            target = tuple(self.report['finalDestination']) if self.goal['final'] else entrance
        self.route_history.append(abs(target[0] - self.at[0]) + abs(target[1] - self.at[1]))
        goals = [target] if target[2] == self.at[2] else []
        if goals and self.go(goals, True):
            return True
        if len(self.route_history) == 80 and min(list(self.route_history)[-40:]) >= min(list(self.route_history)[:40]) - 5:
            if self.dig(target):
                self.route_history.clear()
                return True
        if self.frontier(target):
            return True
        return self.dig(target)

    def verify_victory(self):
        self.client.capture('canonical-campaign-victory')
        state = exact(self.client, 'victory')
        if self.client.ui['panel']:
            self.client.key('Escape')
        receipt = state['nativeCampaign']['finale']
        assert receipt.get('victoryAt') is not None and receipt.get('victor', 0) > 0
        before = self.client.ui['nativeDungeon']['clock']
        self.client.key('b')
        assert self.client.ui['panel'] == 'dungeon-party'
        self.client.key('Escape')
        assert self.client.ui['nativeDungeon']['clock'] == before
        self.client.key('t')
        self.client.key('space')
        assert self.client.ui['notice'] == 'Action committed.'
        assert self.client.ui['nativeDungeon']['clock'] >= before
        after = exact(self.client, 'post-victory-paid')
        assert after['nativeCampaign']['finale'] == receipt
        assert self.client.ui['nativeDungeon']['victory']
        self.client.capture('victory-continued-exact')
        self.receipts.add('canonical-victory-and-paid-exact-continuation')

    def run_campaign(self, budget):
        if not self.client.continuation:
            self.client.capture('fresh-generated-class-choice')
            self.client.control_click('dungeon:class:1')
        if self.client.ui['nativeDungeon']['mapVisible']:
            self.client.key('Tab')
        exact(self.client, 'initial')
        if not self.client.ui['nativeDungeon']['victory']:
            self.report = reports(self.client, 'initial-public-journal')
        for self.steps in range(1, budget + 1):
            self.travel_dash = False
            if self.client.sequence >= 650:
                self.checkpoint('protocol-boundary-' + str(self.steps), reload=True)
            if self.client.ui['panel']:
                self.client.key('Escape')
            if self.client.ui['nativeDungeon']['controlledActor'] != self.client.ui['nativeDungeon']['activeActor']:
                self.client.key('t')
            self.observe()
            if (self.client.output / 'pause-request').exists():
                return 'paused'
            if not any(p['life'] != 'Dead' for p in self.state['player']):
                return 'defeat'
            if not any(p['life'] == 'Alive' for p in self.state['player']):
                return 'all-down-rescue-window'
            if self.state['victory']:
                self.verify_victory()
                if self.goal and self.goal['final'] and self.goal['name'] not in self.completed:
                    self.completed.append(self.goal['name'])
                return 'victory'
            if self.rescue() or self.sustain():
                continue
            if self.fighters:
                if self.at in self.danger and self.fight():
                    continue
                if self.fixture() or self.fight():
                    continue
            if self.stage == 'recruit':
                if len(self.state['player']) >= self.members:
                    self.stage = 'travel'
                    self.client.control_click('dungeon:member:' + str(self.state['player'][0]['id']))
                    self.client.key('b')
                    self.client.control_click('dungeon:lead-selected')
                    self.client.control_click('dungeon:regroup')
                    self.client.key('Escape')
                    self.client.capture('actual-four-member-party')
                    self.checkpoint('party-formed', reload=True)
                    continue
                elif self.recruit():
                    continue
                else:
                    return 'no-observed-recruit-route'
            if self.goal and self.goal.get('returning') and self.at[2] == 0:
                self.goal = None
            if self.goal is None:
                outcome = self.choose_goal()
                if outcome == 'need-rumour':
                    if self.rumour():
                        continue
                    return 'no-public-final-entrance'
                if outcome:
                    return outcome
            key = CLASS_KEYS[self.goal['name']]
            if any(key in c for c in self.defeated) and self.goal['name'] not in self.completed:
                self.completed.append(self.goal['name'])
                self.client.capture('observed-defeat-' + self.goal['name'].replace(' ', '-'))
                self.goal['returning'] = True
                self.checkpoint('boss-receipt-' + str(len(self.completed)), reload=True)
                continue
            if self.steps % 40 == 0:
                self.checkpoint('progress-' + str(self.steps))
                self.client.key('Escape')
                print('progress', self.steps, self.at, self.goal['name'], self.state['clock'], flush=True)
            # Each ready member pays a real turn. Preserve normal follower policy.
            leader = self.state['player'][0]
            if self.player['id'] != leader['id'] and distance(self.at, position(leader['position'])) <= 5:
                self.client.key('space')
                continue
            if self.player['id'] == leader['id'] and not self.fighters and any(
                    p['life'] == 'Alive' and distance(self.at, position(p['position'])) > 6 for p in self.state['player']):
                self.client.key('space')
                continue
            activities = [a for a in self.state['activities'] if a['id'] == self.player['id'] and a['activity']]
            if activities:
                self.client.request('wait', seconds=.2)
                continue
            if self.quiet_tasks() or self.navigate():
                continue
            return 'no-observed-route'
        return 'action-budget'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--provenance', type=Path, required=True, help='Frozen coordinator directory containing source.json/source.diff.')
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--members', type=int, choices=(1, 4), default=1)
    parser.add_argument('--target', choices=('all', 'final', *KINDS), default='all')
    parser.add_argument('--budget', type=int, default=1200)
    parser.add_argument('--continue-from', type=Path)
    args = parser.parse_args()
    client = CampaignClient.__new__(CampaignClient)
    journey = None
    keep_live = False
    for filename in ('source.json', 'source.diff'):
        assert (args.provenance / filename).is_file(), 'Missing frozen source provenance: ' + filename
    try:
        client.__init__(args.output.resolve(), args.seed, runtime_source=args.runtime.resolve(),
                        continuation=args.continue_from.resolve() if args.continue_from else None,
                        window_size='1280x800', scene='world-campaign')
        for filename in ('source.json', 'source.diff'):
            shutil.copy2(args.provenance / filename, client.output / filename)
        drivers = (Path(__file__).resolve(), Path(__file__).resolve().with_name('verify_native_journey.py'))
        (client.output / 'driver.json').write_text(json.dumps(dict(runtime=str(args.runtime.resolve()),
            sources={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in drivers}), indent=2) + '\n')
        journey = CampaignJourney(client, args.members, args.target)
        outcome = journey.run_campaign(args.budget)
        journey.checkpoint('final-' + outcome, reload=True)
        complete = outcome == 'victory' and (args.target != 'all' or all(name in journey.completed for name in KINDS))
        result = dict(outcome=outcome, passed=complete, decisionIterations=journey.steps,
                      inputReceipts=sum(1 for _ in (client.output / 'inputs.jsonl').open()),
                      requestedMembers=args.members, target=args.target, completed=journey.completed,
                      actualParty=client.ui['nativeDungeon']['player'],
                      encountered=sorted(journey.encountered), defeated=sorted(journey.defeated),
                      receipts=sorted(journey.receipts), clock=client.ui['nativeDungeon']['clock'], rollback=False,
                      limits='Observed gameplay only. Budget, pause, blocked route or defeat is incomplete; no package or aggregate acceptance claim.')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop')
        client.process.wait(timeout=15)
    except BaseException as error:
        if hasattr(client, 'journal'):
            result = dict(passed=False, outcome='automation-error', error=str(error), errorType=type(error).__name__, rollback=False)
            response = client.read('response.json')
            if journey and response and response['id'] == 'journey-' + str(client.sequence):
                try:
                    journey.checkpoint('failure-current-checkpoint')
                    result['ordinaryCheckpoint'] = True
                except Exception as checkpoint_error:
                    result['checkpointError'] = str(checkpoint_error)
            elif isinstance(error, TimeoutError) and client.process and client.process.poll() is None:
                # Preserve the actual pending process. Its input is never replayed
                # from an older save after an observation deadline.
                keep_live = True
                result.update(outcome='automation-timeout-live', liveClientPid=client.process.pid,
                              save=str(client.slot), pendingRequest='journey-' + str(client.sequence))
                (client.output / 'live-client.json').write_text(json.dumps(result, indent=2) + '\n')
            (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
            if keep_live:
                print('LIVE CLIENT AND DISPLAY RETAINED', json.dumps(result), flush=True)
                # xvfb-run owns the display. Keep its Python supervisor alive,
                # without replaying or sending further gameplay input.
                while client.process.poll() is None:
                    time.sleep(1)
        raise
    finally:
        if hasattr(client, 'journal') and hasattr(client, 'log'):
            if keep_live:
                client.journal.close()
                client.log.close()
            else:
                client.close()


if __name__ == '__main__':
    main()
