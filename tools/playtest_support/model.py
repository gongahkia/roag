"""Optional Codex CLI decision loop. Only the validated mouse driver acts on decisions."""
import json
from pathlib import Path
import shutil
import subprocess
import time
import threading
from collections import deque
from agent_support.workspace import atomic_json, utc
from agent_support import runner
from . import driver

ACTION_PROPERTIES = {
    'kind': {'type': 'string', 'enum': driver.KINDS},
    'control': {'type': ['string', 'null']},
    **{key: {'type': ['integer', 'null']} for key in ['x', 'y', 'endX', 'endY', 'unit', 'actor', 'button']},
    'seconds': {'type': ['number', 'null']}, 'label': {'type': ['string', 'null']}}
DECISION_SCHEMA = {
    'type': 'object', 'additionalProperties': False,
    'required': ['reason', 'done', 'action', 'finding'],
    'properties': {
        'reason': {'type': 'string'}, 'done': {'type': 'boolean'},
        'action': {'anyOf': [{'type': 'null'}, {'type': 'object', 'additionalProperties': False,
                                            'required': list(ACTION_PROPERTIES), 'properties': ACTION_PROPERTIES}]},
        'finding': {'anyOf': [{'type': 'null'}, {'type': 'object', 'additionalProperties': False,
            'required': ['category', 'severity', 'description', 'expected'],
            'properties': {'category': {'type': 'string', 'enum': ['usability', 'navigation', 'combat', 'performance', 'visual', 'bug', 'balance']},
                           'severity': {'type': 'string', 'enum': ['note', 'warning', 'error']},
                           'description': {'type': 'string'}, 'expected': {'type': 'string'}}}]}}}


def command(executable, model, reasoning, folder, schema, answer, screenshot=None):
    argv = [executable, 'exec', '--model', model, '--sandbox', 'read-only', '--skip-git-repo-check', '--ephemeral',
            '--cd', str(folder), '--config', 'model_reasoning_effort=' + reasoning,
            '--json', '--color', 'never', '--output-schema', str(schema), '--output-last-message', str(answer)]
    if screenshot:
        argv.extend(['--image', str(screenshot)])
    return [*argv, '-']


def prompt(objective, observation, history, remaining):
    return '\n'.join([
        'You are playtesting Vagabonds, a mouse-driven fantasy party RPG. Return one JSON decision matching the schema.',
        'Do not edit code, run shell commands, inspect repository internals, use external tools, or invent game state. The controller executes your one validated mouse action.',
        'The game is deliberately held while you think. Use wait (0.05..30 seconds) to advance real simulation, travel, dialogue animation and enemy turns.',
        'Only current visible controls/actors/tiles are available. Use control IDs exactly as shown. Unavailable controls have reasons. Tile x/y are WORLD coordinates; point x/y are SCREEN pixels.',
        'control: click a visible enabled control. actor: click a visible actor ID. tile: left/right click a visible cell. unit: click a displayed combat unit. hover: inspect a tile. drag: world x/y to endX/endY. capture/mark: diagnostics.',
        'For dialogue use dialogue-next to reveal/advance, then a visible conversation choice. For combat choose skill:N and then a unit; group/self skills fire immediately. Wait or use combat-skip while combatPresentation.busy.',
        'Buttons can change panels without spending world time. Travel and jobs need waits. Right-click point on an empty area closes panels. Prefer close when shown.',
        'The map legend includes unknown cells; do not guess hidden terrain or target unobserved places. Use short visible steps to explore. Never alter saves or grant resources.',
        'A delivered mouse event is not proof that the intended gameplay action succeeded. Read the next notice, panel, party and battle state. Use screenshots for visual findings.',
        'Report concrete feedback only with evidence from the current observation. A finding is an opinion; never claim tests passed without observing them.',
        'If stuck, state exactly what blocked you. End with done=true and action=null when the objective is met, impossible with current evidence, or there is no useful next action.',
        'Set unused action fields to null. Keep reasoning short. Do not ask questions; document uncertainty. Remaining decisions: ' + str(remaining),
        'Objective: ' + objective, 'Recent controller results: ' + json.dumps(history[-5:]),
        'Current observation JSON: ' + json.dumps(observation, ensure_ascii=False)])


def execute(argv, text, timeout):
    """Run a trusted installed CLI with a timeout; preserve bounded diagnostic output."""
    process = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                               start_new_session=True)
    token = runner.identity(process.pid)
    chunks = deque(maxlen=128)  # At most one MiB, even if a CLI/tool floods stdout.
    def drain():
        while True:
            data = process.stdout.read(8192)
            if not data:
                return
            chunks.append(data)
    def supply():
        try:
            process.stdin.write(text.encode()); process.stdin.close()
        except (BrokenPipeError, OSError):
            pass
    reader = threading.Thread(target=drain, daemon=True); reader.start()
    writer = threading.Thread(target=supply, daemon=True); writer.start()
    try:
        process.wait(timeout=timeout)
    except BaseException:
        runner.stop_child(token)
        process.wait(timeout=5)
        raise
    finally:
        reader.join(timeout=2); writer.join(timeout=2)
        if not reader.is_alive():
            process.stdout.close()
        if not writer.is_alive() and not process.stdin.closed:
            process.stdin.close()
    return process.returncode, b''.join(chunks).decode(errors='replace')


def run(repo, run_id, model, objective, reasoning='low', max_steps=30, timeout=600,
        decision_timeout=60, root=None, executable='codex', keep_open=False, dry_run=False):
    import fcntl
    path, _ = driver.location(repo, run_id, root)
    with (path / 'model.lock').open('a') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError('A model tester already owns this playtest. Inspect its report instead of launching another.')
        return _run(repo, run_id, model, objective, reasoning, max_steps, timeout, decision_timeout, root, executable, keep_open, dry_run)


def _run(repo, run_id, model, objective, reasoning='low', max_steps=30, timeout=600,
        decision_timeout=60, root=None, executable='codex', keep_open=False, dry_run=False):
    if not model or len(model) > 160 or model.startswith('-') or any(c.isspace() for c in model):
        raise ValueError('Provide an explicit model ID, for example gpt-5.6-luna. It is never silently substituted.')
    if not isinstance(objective, str) or not objective.strip() or len(objective) > 2000:
        raise ValueError('Provide a playtest objective of 1..2000 characters.')
    if reasoning not in ['none', 'low', 'medium', 'high', 'xhigh', 'max'] or not 1 <= max_steps <= 200 or not 1 <= timeout <= 7200 or not 1 <= decision_timeout <= 300:
        raise ValueError('Invalid reasoning/time/step limits.')
    binary = shutil.which(executable)
    if not binary:
        raise ValueError('Codex CLI is not installed. Any existing coding agent can instead use playtest observe/act/feedback.')
    path, _ = driver.location(repo, run_id, root)
    folder = path / 'model'; folder.mkdir(exist_ok=True)
    schema = folder / 'decision.schema.json'; atomic_json(schema, DECISION_SCHEMA)
    observed = driver.observe(repo, run_id, root)
    answer = folder / 'answer.json'
    argv = command(binary, model, reasoning, folder, schema, answer, observed['screenshot'])
    if dry_run:
        return {'id': run_id, 'argv': argv, 'objective': objective, 'model': model, 'reasoning': reasoning,
                'maxSteps': max_steps, 'timeoutSeconds': timeout, 'modelInvoked': False}
    started = time.monotonic(); history, usage, count = [], {'input_tokens': 0, 'output_tokens': 0}, 0
    report = {'schemaVersion': 1, 'id': run_id, 'model': model, 'reasoning': reasoning, 'objective': objective,
              'started': utc(), 'status': 'running', 'maxSteps': max_steps, 'timeoutSeconds': timeout,
              'backend': 'codex-cli-structured-decisions', 'modelExecutable': binary,
              'assertions': 'Model findings are tester opinions. The deterministic suite reports assertions separately.'}
    atomic_json(path / 'model-report.json', report)
    try:
        for index in range(max_steps):
            remaining = timeout - (time.monotonic() - started)
            if remaining <= 1:
                report['status'] = 'time-budget'; break
            observed = driver.observe(repo, run_id, root)
            if not observed['processAlive']:
                report['status'] = 'client-exited'; break
            current = prompt(objective, observed['state'], history, max_steps - index)
            (folder / ('prompt-' + str(index % 8) + '.txt')).write_text(current[:50000])
            answer.unlink(missing_ok=True)
            argv = command(binary, model, reasoning, folder, schema, answer, observed['screenshot'])
            code, output = execute(argv, current, min(decision_timeout, remaining))
            (folder / ('events-' + str(index % 8) + '.jsonl')).write_text(output[-1048576:])
            for line in output.splitlines():
                try:
                    event = json.loads(line)
                    if event.get('type') == 'turn.completed':
                        for key in usage:
                            usage[key] += event.get('usage', {}).get(key, 0)
                except (ValueError, TypeError):
                    pass
            if code or not answer.is_file():
                raise ValueError('Codex decision failed (no model substitution). Exit ' + str(code) + ': ' + output[-2000:])
            if answer.stat().st_size > 32768:
                with answer.open('rb') as stream:
                    prefix = stream.read(32768)
                answer.write_bytes(prefix)
                raise ValueError('Model decision exceeds 32 KiB.')
            decision = json.loads(answer.read_text()); validate_decision(decision); count += 1
            atomic_json(folder / ('decision-' + str(index % 8) + '.json'), {'index': index + 1, 'observation': observed['state']['observation'], 'decision': decision})
            if decision['finding']:
                finding = decision['finding']
                driver.feedback(repo, run_id, finding['description'], finding['category'], finding['severity'], root, finding['expected'])
            if decision['done']:
                report['status'] = 'model-finished'; report['conclusion'] = decision['reason']; break
            if decision['action'] is None:
                raise ValueError('An unfinished model decision needs a mouse action.')
            result = driver.act(repo, run_id, decision['action'], root, expected=observed['state']['observation'], timeout=min(45, max(1, timeout - (time.monotonic() - started))))
            history.append({'step': result['step'], 'action': decision['action'], 'delivered': result['delivered'],
                            'reason': result['response']['reason'], 'notice': result['state']['notice'], 'panel': result['state']['panel'], 'position': result['state']['position']})
            report.update(decisions=count, lastStep=result['step'], recentActions=history[-8:], usage=usage); atomic_json(path / 'model-report.json', report)
            if decision['action']['kind'] == 'stop':
                report['status'] = 'model-stopped-client'; break
        else:
            report['status'] = 'step-budget'
    except (ValueError, OSError, TimeoutError, subprocess.TimeoutExpired, KeyError, TypeError) as e:
        report.update(status='model-error', error=str(e))
    except KeyboardInterrupt:
        report.update(status='interrupted')
    finally:
        report.update(finished=utc(), elapsedSeconds=time.monotonic() - started, decisions=count, usage=usage, recentActions=history[-8:])
        atomic_json(path / 'model-report.json', report)
        if not keep_open:
            driver.stop(repo, run_id, root)
    return report


def validate_decision(value):
    if not isinstance(value, dict) or set(value) != {'reason', 'done', 'action', 'finding'} or type(value['done']) is not bool or not isinstance(value['reason'], str):
        raise ValueError('Invalid structured model decision.')
    if value['action'] is not None:
        driver.validate_action(value['action'])
    if value['finding'] is not None:
        finding = value['finding']
        if not isinstance(finding, dict) or set(finding) != {'category', 'severity', 'description', 'expected'}:
            raise ValueError('Invalid structured finding.')
        if not all(isinstance(v, str) for v in finding.values()):
            raise ValueError('Finding fields must be strings.')
