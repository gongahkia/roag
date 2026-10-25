"""A bounded native client lifecycle and single-request mouse transport. No shell."""
import contextlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
import uuid
from agent_support.workspace import atomic_json, identifier, utc
from agent_support import runner
from .recordings import read

KINDS = ['control', 'tile', 'actor', 'unit', 'point', 'hover', 'drag', 'wait', 'capture', 'mark', 'stop']
SCENES = ['world', 'world-resources', 'world-wildlife', 'world-situation', 'world-camp', 'world-discovery',
          'world-battle', 'world-cooperation', 'world-boss', 'world-progression', 'world-encounters']
MAX_ACTIONS = 1000
RETAINED_STEPS = 64


def location(repo, run_id, root=None):
    path = Path(root or repo.root / 'artifacts/playtests').resolve() / identifier(run_id)
    if path.is_symlink():
        raise ValueError('Playtest directory must not be a link.')
    manifest = read(path / 'playtest.json')
    if not manifest or manifest.get('schemaVersion') != 1 or manifest.get('id') != run_id:
        raise ValueError('Unknown playtest ID: ' + run_id)
    return path, manifest


def build(repo):
    import fcntl
    repo.artifacts.mkdir(parents=True, exist_ok=True)
    with (repo.artifacts / 'verification.lock').open('a') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError('A verifier/build is running. Reconnect to it, or use --no-build with an existing build.')
        result = subprocess.run([str(repo.root / 'tools/dotnet.sh'), 'build', 'src/Vagabonds.Client', '--configuration', 'Release', '--nologo', '-v:q'],
                                cwd=repo.root, capture_output=True, text=True, timeout=180)
        if result.returncode:
            raise ValueError('Client build failed: ' + (result.stdout + result.stderr)[-4000:])


def prune(root, keep=10, maximum=536870912):
    """Only recognized, closed, locally owned playtests are eligible."""
    root = Path(root)
    if not root.exists():
        return
    candidates = []
    for path in root.iterdir():
        try:
            if not path.is_dir() or path.is_symlink():
                continue
            data = read(path / 'playtest.json', {})
            if data.get('schemaVersion') != 1 or data.get('id') != path.name or runner.alive(data.get('process')):
                continue
            if any(p.is_symlink() for p in path.rglob('*')):
                continue
            candidates.append((path, sum(p.stat().st_size for p in path.rglob('*') if p.is_file())))
        except (OSError, ValueError):
            continue
    total, count = sum(size for _, size in candidates), len(candidates)
    for path, size in sorted(candidates):
        if count < keep and total <= maximum:
            break
        data = read(path / 'playtest.json', {})
        if not runner.alive(data.get('process')):
            shutil.rmtree(path); total -= size; count -= 1


def start(repo, seed=42, scene='world', visible=False, no_build=False, root=None, launch_prefix=None):
    if not sys.platform.startswith('linux'):
        raise ValueError('The managed playtest driver currently requires Linux/WSL. The Client mouse protocol itself is platform-independent.')
    if scene not in SCENES:
        raise ValueError('Unknown scene.')
    if not -(2**31) <= seed < 2**31:
        raise ValueError('Seed must be a signed 32-bit integer.')
    if visible and not os.environ.get('DISPLAY'):
        raise ValueError('--visible requires a working DISPLAY.')
    if visible and launch_prefix:
        raise ValueError('Instrumented launches require an isolated private display.')
    if not visible and not shutil.which('xvfb-run'):
        raise ValueError('Unattended playtests require xvfb-run. Use --visible with a working DISPLAY.')
    if not no_build:
        build(repo)
    dll = repo.root / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll'
    if not dll.is_file():
        raise ValueError('Build the Release client first.')
    root = Path(root or repo.root / 'artifacts/playtests').resolve(); root.mkdir(parents=True, exist_ok=True); prune(root)
    run_id = time.strftime('%Y%m%d-%H%M%S', time.gmtime()) + '-' + uuid.uuid4().hex[:8]
    path = root / run_id; path.mkdir()
    argv = [str(repo.root / 'tools/dotnet.sh'), str(dll), '--scene', scene, '--seed', str(seed),
            '--save', str(path / 'campaign.json'), '--playtest-control', str(path), '--window-size', '1024x720']
    if not visible:
        argv = ['xvfb-run', '-a', '-s', '-screen 0 1280x900x24', *(launch_prefix or []), *argv]
    env = {**os.environ, 'SDL_VIDEODRIVER': 'x11'}
    if not visible:
        env.update({'LIBGL_ALWAYS_SOFTWARE': '1', 'SDL_AUDIODRIVER': 'dummy'})
    state = {'schemaVersion': 1, 'id': run_id, 'created': utc(), 'status': 'starting', 'scene': scene, 'seed': seed,
             'source': repo.fingerprint(), 'head': repo.git('rev-parse', 'HEAD').decode().strip(),
             'argv': argv, 'process': None, 'steps': 0, 'isolatedSave': str(path / 'campaign.json'),
             'inputBackend': 'client-mouse-states', 'agentHold': True, 'display': 'native' if visible else 'xvfb-software'}
    atomic_json(path / 'playtest.json', state)
    with (path / 'client.log').open('wb') as log:
        process = subprocess.Popen(argv, cwd=repo.root, stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT,
                                   start_new_session=True, env=env, close_fds=True)
    state['process'] = runner.identity(process.pid); atomic_json(path / 'playtest.json', state)
    try:
        deadline = time.monotonic() + 40
        while time.monotonic() < deadline:
            observation = read(path / 'observation.json')
            if observation:
                state['status'] = 'running'; state['sessionId'] = observation['ui'].get('sessionId'); atomic_json(path / 'playtest.json', state)
                return {'id': run_id, 'folder': str(path), 'sessionId': state['sessionId'], 'observation': compact(observation),
                        'inspect': './dev playtest observe ' + run_id}
            if process.poll() is not None:
                raise ValueError('Client exited before readiness: ' + (path / 'client.log').read_text(errors='replace')[-3000:])
            time.sleep(.05)
        raise TimeoutError('Client readiness timed out; inspect ' + str(path / 'client.log'))
    except BaseException:
        runner.stop_child(state['process']); state['status'] = 'failed'; atomic_json(path / 'playtest.json', state); raise


def compact(observation):
    ui = observation['ui']
    cells = ui.get('cells', [])
    glyphs = {'Wall': '#', 'Rock': '#', 'Water': '~', 'Tree': 'T', 'StairsDown': '>', 'StairsUp': '<',
              'Storage': 'C', 'Camp': '^', 'Door': '+', 'Bridge': '=', 'Earth': '.', 'Floor': '.', 'Grass': ','}
    chart = None
    if cells:
        left, top = min(c['x'] for c in cells), min(c['y'] for c in cells)
        right, bottom = max(c['x'] for c in cells), max(c['y'] for c in cells)
        if right - left >= 64 or bottom - top >= 64:
            raise ValueError('Visible observation exceeds the supported local map bounds.')
        grid = [['?' for _ in range(right - left + 1)] for _ in range(bottom - top + 1)]
        for c in cells:
            grid[c['y'] - top][c['x'] - left] = 'O' if c.get('prop') else glyphs.get(c['material'], '?')
        for actor in ui.get('actors', []):
            if left <= actor['x'] <= right and top <= actor['y'] <= bottom:
                grid[actor['y'] - top][actor['x'] - left] = '!' if actor['hostile'] else 'n'
        for index, actor in enumerate(ui.get('party', [])):
            if left <= actor['x'] <= right and top <= actor['y'] <= bottom:
                grid[actor['y'] - top][actor['x'] - left] = str(index + 1)
        chart = {'left': left, 'top': top, 'floor': ui['position']['floor'], 'rows': [''.join(row) for row in grid],
                 'legend': '? unknown, # rock/wall, T tree, ~ water, O prop, C storage, ^ camp, > down, < up, n person, ! hostile, 1-4 party; . , + = terrain. Center targets must be inside the map.'}
    controls = [{k: c[k] for k in ('id', 'label', 'enabled', 'reason', 'tip', 'detail', 'selected') if c.get(k) not in ('', None)} for c in ui.get('controls', [])]
    return {'schemaVersion': 1, 'observation': observation['observation'], 'utc': observation['utc'], 'agentHold': True,
            'seed': ui.get('seed'), 'sessionId': ui.get('sessionId'), 'panel': ui['panel'], 'paused': ui['paused'],
            'tick': ui['tick'], 'position': ui['position'], 'notice': ui['notice'], 'routeCount': ui['routeCount'],
            'party': [{k: v for k, v in a.items() if k != 'bounds'} for a in ui.get('party', [])],
            'inventory': ui.get('inventory'), 'controls': controls,
            'actors': [{k: v for k, v in a.items() if k != 'bounds'} for a in ui.get('actors', [])],
            'map': chart, 'hover': ui.get('hover'), 'selection': ui.get('selection'), 'context': ui.get('context'),
            'work': ui.get('work'), 'dialogue': {k: v for k, v in (ui.get('dialogue') or {}).items() if k != 'fullText'} or None, 'skills': ui.get('skills'),
            'journal': ui.get('journal'), 'waypoint': ui.get('waypoint'), 'places': ui.get('places'),
            'discoveries': [{k: d[k] for k in ('id', 'name', 'x', 'y', 'floor', 'visited', 'outcome') if k in d}
                            for d in ui.get('discoveries', [])],
            'localFeatures': [{k: c[k] for k in ('x', 'y', 'floor', 'material')} for c in cells
                              if c['material'] in ('StairsDown', 'StairsUp')],
            'localGround': [[c['x'], c['y'], bool(c.get('groundOpen')), bool(c.get('occupied')), c.get('clickable', True)] for c in cells],
            'combatFeedback': ui.get('combatFeedback'), 'equipment': ui.get('equipment'), 'progression': ui.get('progression'),
            'selectedSkill': ui.get('selectedSkill'), 'battle': ui.get('battle'),
            'combatPresentation': ui.get('combatPresentation'), 'combatIntents': ui.get('combatIntents'),
            'encounterReward': ui.get('encounterReward'), 'screenshotAvailable': observation.get('screenshot') is not None}


def observe(repo, run_id, root=None, full=False):
    import fcntl
    path, state = location(repo, run_id, root)
    with (path / 'action.lock').open('a') as lock:
        with contextlib.suppress(BlockingIOError):
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
            reconcile(path)
    state = read(path / 'playtest.json')
    observation = read(path / 'observation.json')
    if not observation:
        raise ValueError('Client has not published an observation.')
    return {'id': run_id, 'processAlive': runner.alive(state.get('process')), 'steps': state['steps'], 'actionPending': (path / 'pending.json').exists(),
            'screenshot': str(path / 'frame.png') if observation.get('screenshot') else None,
            'state': observation if full else compact(observation)}


def validate_action(action):
    allowed = {'kind', 'control', 'x', 'y', 'endX', 'endY', 'unit', 'actor', 'button', 'seconds', 'label'}
    if not isinstance(action, dict) or set(action) - allowed or action.get('kind') not in KINDS:
        raise ValueError('Provide a known mouse action and supported fields; use ./dev playtest help.')
    # Structured model schemas require every optional field, represented by null.
    # Apply defaults only after removing those absent values.
    action = {key: value for key, value in action.items() if value is not None}
    if len(json.dumps(action).encode()) > 8192:
        raise ValueError('Action is too large.')
    for key, maximum in [('control', 200), ('label', 500)]:
        if action.get(key) is not None and (not isinstance(action[key], str) or len(action[key]) > maximum):
            raise ValueError(key + ' must be a bounded string.')
    for key in ('x', 'y', 'endX', 'endY', 'unit', 'actor', 'button'):
        if key in action and action[key] is not None and (type(action[key]) is not int):
            raise ValueError(key + ' must be an integer.')
        if action.get(key) is not None and not -(2**(63 if key == 'actor' else 31)) <= action[key] < 2**(63 if key == 'actor' else 31):
            raise ValueError(key + ' is outside the protocol integer range.')
    if action.get('button', 1) not in (1, 3):
        raise ValueError('Use mouse button 1 or 3.')
    if 'seconds' in action and action['seconds'] is not None and (type(action['seconds']) not in (int, float) or not .05 <= action['seconds'] <= 30):
        raise ValueError('Wait 0.05..30 real seconds.')
    return {k: v for k, v in action.items() if v is not None}


def act(repo, run_id, action, root=None, expected=None, timeout=45):
    import fcntl
    action = validate_action(action)
    path, _ = location(repo, run_id, root)
    with (path / 'action.lock').open('a') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError('Another controller is acting; wait for its result.')
        _, state = location(repo, run_id, root)
        if not runner.alive(state.get('process')):
            raise ValueError('Playtest process is no longer live; inspect its report/logs.')
        if state['steps'] >= MAX_ACTIONS and action['kind'] != 'stop':
            raise ValueError('Playtest action budget exhausted; stop and start another isolated session.')
        if (path / 'pending.json').exists():
            recovered = reconcile(path)
            raise ValueError('The previous action was recovered; observe before another action.' if recovered else 'A previous action is still pending; observe instead of resubmitting it.')
        if (path / 'request.json').exists():
            raise ValueError('A previous action is still pending. Observe instead of resubmitting it.')
        before = read(path / 'observation.json')
        request = {'schemaVersion': 1, 'id': uuid.uuid4().hex, 'observation': before['observation'] if expected is None else expected, **action}
        atomic_json(path / 'pending.json', {'step': state['steps'] + 1, 'request': request, 'created': utc(),
                    'before': {'observation': before['observation'], 'ui': {k: before['ui'][k] for k in ['tick', 'position', 'panel']}}})
        atomic_json(path / 'request.json', request)
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            response = read(path / 'response.json', {})
            if response.get('id') == request['id']:
                recovered = reconcile(path)
                after = read(path / 'observation.json'); state = read(path / 'playtest.json'); step = recovered['evidence']
                return {'id': run_id, 'step': state['steps'], 'delivered': response['success'], 'response': response,
                        'evidence': step, 'state': compact(after)}
            if not runner.alive(state.get('process')):
                raise ValueError('Client exited during action; inspect ' + str(path / 'client.log'))
            time.sleep(.025)
        raise TimeoutError('Action timed out. Do not repeat it blindly; inspect response.json and observe the session.')


def reconcile(path):
    """Commit a delivered action exactly once after a controller disconnect."""
    pending = read(path / 'pending.json')
    if not pending:
        return None
    response = read(path / 'response.json', {})
    if response.get('id') != pending['request']['id']:
        return None
    after = read(path / 'observation.json')
    if after['observation'] != response['observation']:
        raise ValueError('Response/observation mismatch; inspect retained files before acting.')
    step = retain_step(path, pending['step'], pending['request'], response, pending['before'], after)
    state = read(path / 'playtest.json'); state['steps'] = max(state['steps'], pending['step']); state['updated'] = utc()
    atomic_json(path / 'playtest.json', state); (path / 'pending.json').unlink()
    return {'step': state['steps'], 'evidence': step}


def retain_step(path, number, request, response, before, after):
    steps = path / 'steps'; steps.mkdir(exist_ok=True)
    name = 'slot-' + str((number - 1) % RETAINED_STEPS).zfill(2)
    evidence = {'step': number, 'request': request, 'response': response,
                'before': {'observation': before['observation'], 'tick': before['ui']['tick'], 'position': before['ui']['position'], 'panel': before['ui']['panel']},
                'after': {'observation': after['observation'], 'tick': after['ui']['tick'], 'position': after['ui']['position'], 'panel': after['ui']['panel'], 'notice': after['ui']['notice']}}
    atomic_json(steps / (name + '.json'), evidence)
    atomic_json(steps / (name + '.observation.json'), after)
    png = path / 'frame.png'
    capture = steps / (name + '.png'); capture.unlink(missing_ok=True)
    if after.get('screenshot') and png.is_file() and png.stat().st_size <= 4194304:
        shutil.copy2(png, capture)
    return {'record': str(steps / (name + '.json')), 'observation': str(steps / (name + '.observation.json')),
            'screenshot': str(capture) if capture.exists() else None, 'retainedSteps': RETAINED_STEPS}


def feedback(repo, run_id, message, category='usability', severity='note', root=None, expected=''):
    if category not in ['usability', 'navigation', 'combat', 'performance', 'visual', 'bug', 'balance'] or severity not in ['note', 'warning', 'error']:
        raise ValueError('Unknown feedback category or severity.')
    if not isinstance(message, str) or not message.strip() or len(message) > 2000 or len(expected) > 1000:
        raise ValueError('Feedback needs a nonempty description of at most 2000 characters.')
    path, state = location(repo, run_id, root)
    entries = read(path / 'feedback.json', [])
    number = entries[-1].get('number', len(entries)) + 1 if entries else 1
    slot = (number - 1) % 16
    evidence_name = 'feedback-' + str(slot).zfill(2)
    observation = read(path / 'observation.json', {})
    atomic_json(path / (evidence_name + '.json'), {'number': number, 'observation': observation})
    screenshot = path / (evidence_name + '.png'); screenshot.unlink(missing_ok=True)
    frame = path / 'frame.png'
    if observation.get('screenshot') and frame.exists() and frame.stat().st_size <= 4194304:
        shutil.copy2(frame, screenshot)
    entries.append({'utc': utc(), 'category': category, 'severity': severity, 'description': message,
                    'number': number, 'expected': expected, 'step': state['steps'], 'sessionId': state.get('sessionId'),
                    'observation': observation.get('observation'), 'judgment': 'tester-opinion',
                    'evidence': {'snapshot': evidence_name + '.json', 'screenshot': evidence_name + '.png'}})
    atomic_json(path / 'feedback.json', entries[-64:])
    return entries[-1]


def stop(repo, run_id, root=None):
    path, state = location(repo, run_id, root)
    forced = False
    if runner.alive(state.get('process')):
        try:
            act(repo, run_id, {'kind': 'stop'}, root=root, timeout=8)
        except (ValueError, TimeoutError):
            pass
        deadline = time.monotonic() + 5
        while runner.alive(state.get('process')) and time.monotonic() < deadline:
            time.sleep(.05)
        if runner.alive(state.get('process')):
            runner.stop_child(state['process']); forced = True
    state = read(path / 'playtest.json'); state['status'] = 'closed'; state['closed'] = utc(); state['forcedStop'] = forced; atomic_json(path / 'playtest.json', state)
    return {'id': run_id, 'status': state['status'], 'forcedStop': state.get('forcedStop', False), 'folder': str(path)}


def report(repo, run_id, root=None):
    from . import recordings, experience
    path, state = location(repo, run_id, root)
    evidence = [read(p) for p in sorted((path / 'steps').glob('slot-*.json')) if not p.name.endswith('.observation.json')]
    evidence.sort(key=lambda e: e['step'])
    session_reports = [recordings.report(path / 'recordings', s['id']) for s in recordings.sessions(path / 'recordings', limit=3)]
    feedback_entries = read(path / 'feedback.json', [])
    for entry in feedback_entries:
        saved = read(path / entry['evidence']['snapshot'], {}) if entry.get('evidence') else {}
        entry['evidenceRetained'] = saved.get('number') == entry.get('number')
    return {'id': run_id, 'manifest': state, 'processAlive': runner.alive(state.get('process')),
            'sourceFresh': state['source'] == repo.fingerprint(), 'retainedSteps': len(evidence), 'recentSteps': evidence[-10:],
            'feedback': feedback_entries, 'modelRun': read(path / 'model-report.json'), 'playerReview': experience.review(path),
            'pendingAction': read(path / 'pending.json'), 'lastResponse': read(path / 'response.json'),
            'recordings': session_reports, 'assertions': read(path / 'assertions.json', []),
            'scope': 'Client-generated mouse states use the real input handlers. Native OS input remains covered separately by verify-native. Model judgments are not regression assertions.'}
