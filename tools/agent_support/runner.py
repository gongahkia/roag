"""Durable local verification workers. No shell commands and no dependency installation."""
import contextlib
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import time
import uuid
import xml.etree.ElementTree as ET

if __package__ in (None, ''):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from agent_support.workspace import Repository, atomic_json, digest, identifier, read_json, utc

_workers = []

def reap():
    _workers[:] = [p for p in _workers if p.poll() is None]

TERMINAL = {'passed', 'failed', 'blocked', 'cancelled', 'timed_out', 'interrupted', 'stale'}
NATIVE_SCENES = ['world', 'world-resources', 'world-wildlife', 'world-situation', 'world-camp', 'world-discovery', 'world-battle', 'world-cooperation', 'world-boss', 'world-progression', 'world-encounters']

def identity(pid):
    """Linux/WSL identity includes boot and process start ticks; never signal a reused PID."""
    try:
        stat = Path(f'/proc/{pid}/stat').read_text().rsplit(')', 1)[1].split()
        if stat[0] == 'Z': return None
        return {'pid': pid, 'start': stat[19], 'boot': Path('/proc/sys/kernel/random/boot_id').read_text().strip()}
    except (OSError, IndexError): return None

def alive(token):
    return bool(token and identity(token['pid']) == token)

def run_dir(repo, run_id):
    return repo.artifacts / 'runs' / identifier(run_id)

def stop_child(token):
    if not alive(token): return
    with contextlib.suppress(ProcessLookupError): os.killpg(token['pid'], signal.SIGTERM)
    deadline = time.monotonic() + 2
    while alive(token) and time.monotonic() < deadline: time.sleep(.05)
    # The group can retain children after its leader exits; it was ours at entry.
    with contextlib.suppress(ProcessLookupError): os.killpg(token['pid'], signal.SIGKILL)

def profile_steps(repo, profile, folder, scenes=None):
    dotnet = str(repo.root / 'tools/dotnet.sh'); python = sys.executable
    dll = lambda project: str(repo.root / f'src/{project}/bin/Release/net10.0/{project}.dll')
    tool = [dotnet, dll('Vagabonds.Tools')]; client = [dotnet, dll('Vagabonds.Client')]
    steps = []
    def add(name, argv, timeout=300, evidence=None):
        if not any(s['name'] == name for s in steps): steps.append({'name': name, 'argv': argv, 'timeout': timeout, 'evidence': evidence})
    def build(): add('build', [dotnet, 'build', 'Vagabonds.sln', '--configuration', 'Release', '--nologo', '-v:q'])
    profiles = ['tools', 'core', 'assets', 'world', 'native'] if profile == 'full' else [profile]
    for p in profiles:
        if p in ['quick', 'core', 'world', 'native', 'playtest']: build()
        if p == 'tools': add('agent-tests', [python, '-m', 'unittest', 'discover', '-s', 'tests/agent', '-v'])
        if p in ['quick', 'core']:
            add('content', tool + ['content', 'validate'], evidence='valid-json')
        if p in ['quick', 'world']:
            add('scenarios', tool + ['scenario', 'suite', 'devtools/scenarios', '--output', str(folder/'scenarios.json')], evidence='passed-json')
        if p == 'core':
            add('core-tests', [dotnet, 'test', 'Vagabonds.sln', '--configuration', 'Release', '--no-build', '--nologo', '-v:minimal', '--logger', 'trx;LogFileName=tests.trx', '--results-directory', str(folder/'tests')], evidence='trx')
        if p == 'assets':
            add('catalog', [python, 'tools/build_game_catalog.py', '--check'])
            add('catalog-tests', [python, '-m', 'unittest', 'discover', '-s', 'tests/catalog', '-v'])
        if p == 'world':
            add('headless', client + ['--world-headless', '--seed', '42'])
            for kind in ['world', 'exploration', 'wildlife', 'situations']:
                output = folder / ('audit-' + kind + ('.json' if kind in ['wildlife', 'situations'] else ''))
                add('audit-' + kind, tool + ['audit-' + kind, '--seed', '42', '--samples', '4', '--output', str(output)])
            add('audit-expedition', tool + ['audit-expedition', '--seeds', '42,1337,-42,2026', '--steps', '900', '--output', str(folder/'audit-expedition.json')], timeout=600, evidence='passed-json')
            add('audit-encounters', tool + ['audit-encounters', '--seeds', '42,1337,-42,2026', '--output', str(folder/'audit-encounters.json')], timeout=600, evidence='passed-json')
            add('audit-counterplay', tool + ['audit-counterplay', '--seed', '42', '--output', str(folder/'audit-counterplay.json')], evidence='passed-json')
            add('audit-simulation', tool + ['audit-simulation', '--seeds', '42,1337,-42', '--steps', '7200', '--output', str(folder/'audit-simulation.json')], timeout=600, evidence='passed-json')
        if p == 'native':
            add('native', ['xvfb-run', '-a', '-s', '-screen 0 1920x1200x24', python, 'tools/verify_world_input.py', '--output', str(folder/'native'), '--scenes', *(scenes or NATIVE_SCENES)], timeout=900, evidence='native')
        if p in ['native', 'playtest']:
            add('playtest-suite', [python, 'tools/playtest_tools.py', 'playtest', 'suite', '--no-build', '--output', str(folder/'playtests')], timeout=600, evidence='playtest')
    return steps

def launch(repo, profile, task=None, scenes=None, steps=None):
    if not sys.platform.startswith('linux'):
        raise ValueError('Managed verification currently requires Linux/WSL process identities. Existing ./dev test and game tools remain available elsewhere.')
    if task: identifier(task)
    if scenes and (profile not in ['native', 'full'] or any(s not in NATIVE_SCENES for s in scenes)):
        raise ValueError('--scenes requires the native/full profile and known scene names.')
    for path in (repo.artifacts/'runs').glob('*/run.json'):
        previous = read_json(path)
        if not alive(previous.get('worker')) and alive(previous.get('child')):
            raise ValueError('An interrupted verifier still has a live child. Use runs cancel ' + previous['id'] + ' before retrying.')
    # Do not duplicate a live task/profile run after reconnecting.
    for old in list_runs(repo):
        if task and old.get('task') == task and old['profile'] == profile and old.get('scenes') == scenes and old['status'] not in TERMINAL:
            return old['id'], True
    run_id = time.strftime('%Y%m%d-%H%M%S', time.gmtime()) + '-' + uuid.uuid4().hex[:8]
    folder = run_dir(repo, run_id); folder.mkdir(parents=True)
    manifest = {'schemaVersion': 1, 'id': run_id, 'profile': profile, 'task': task, 'scenes': scenes,
                'status': 'queued', 'created': utc(), 'createdEpoch': time.time(), 'heartbeat': utc(),
                'worker': None, 'child': None, 'currentStep': None, 'steps': steps or profile_steps(repo, profile, folder, scenes),
                'results': [], 'source': repo.fingerprint(), 'head': repo.git('rev-parse', 'HEAD').decode().strip()}
    atomic_json(folder/'run.json', manifest)
    with (folder/'worker.log').open('wb') as log:
        child = subprocess.Popen([sys.executable, str(Path(__file__).resolve()), str(repo.root), run_id], cwd=repo.root,
                         stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT, start_new_session=True, close_fds=True)
    _workers.append(child); reap()
    return run_id, False

def observe(repo, run_id, verify_artifacts=False):
    reap()
    folder = run_dir(repo, run_id); state = read_json(folder/'run.json')
    if state['status'] not in TERMINAL:
        if alive(state.get('worker')): state['processAlive'] = True
        elif time.time() - state['createdEpoch'] > 5:
            state['status'] = 'interrupted'; state['processAlive'] = False
            state['reason'] = 'Recorded worker identity is no longer live. Inspect retained logs; retry explicitly.'
    if state['status'] in TERMINAL:
        state['fresh'] = state['source']['sha256'] == repo.fingerprint()['sha256']
    if verify_artifacts:
        state['artifactErrors'] = []
        for artifact in state.get('artifacts', []):
            path = folder / artifact['path']
            if not path.resolve().is_relative_to(folder.resolve()) or not path.is_file() or digest(path) != artifact['sha256']: state['artifactErrors'].append(artifact['path'])
        if not state.get('artifacts'): state['artifactErrors'].append('No sealed artifacts recorded.')
    return state

def list_runs(repo):
    result = []
    for path in sorted((repo.artifacts/'runs').glob('*/run.json'), reverse=True):
        try:
            state = read_json(path)
            if state['status'] not in TERMINAL and not alive(state.get('worker')) and time.time()-state['createdEpoch'] > 5: state['status'] = 'interrupted'
            result.append({k: state.get(k) for k in ['id', 'profile', 'task', 'scenes', 'status', 'currentStep', 'heartbeat']})
        except (OSError, ValueError, KeyError): result.append({'id': path.parent.name, 'status': 'unreadable'})
    return result

def cancel(repo, run_id):
    folder = run_dir(repo, run_id); state = observe(repo, run_id)
    if state['status'] in TERMINAL:
        if state['status'] == 'interrupted': stop_child(state.get('child'))
        return {'id': run_id, 'status': state['status'], 'requested': False}
    atomic_json(folder/'cancel.json', {'requested': utc()})
    return {'id': run_id, 'status': state['status'], 'requested': True}

def tail(repo, run_id, step=None, lines=40):
    if not 1 <= lines <= 200: raise ValueError('Choose 1–200 lines.')
    folder = run_dir(repo, run_id); state = read_json(folder/'run.json')
    names = [s['name'] for s in state['steps']]
    name = step or state['currentStep'] or (state['results'][-1]['name'] if state['results'] else None)
    if name not in names: return {'id': run_id, 'step': name, 'text': 'No step log yet.'}
    path = folder/(name+'.log')
    if not path.exists(): return {'id': run_id, 'step': name, 'text': 'No step log yet.'}
    with path.open('rb') as stream:
        stream.seek(max(0, path.stat().st_size-32000)); text = stream.read().decode(errors='replace')
    return {'id': run_id, 'step': name, 'text': '\n'.join(text.splitlines()[-lines:])[-16000:], 'log': str(path)}

def inspect_evidence(step, folder, log):
    kind = step.get('evidence')
    if kind == 'trx':
        files = list((folder/'tests').glob('*.trx'))
        if not files: return {'valid': False, 'reason': 'No TRX test result produced.'}
        failures = []; total = passed = 0
        for path in files:
            root = ET.parse(path).getroot(); ns = {'t': root.tag.split('}')[0].strip('{')}
            counts = root.find('.//t:Counters', ns)
            if counts is None: return {'valid': False, 'reason': 'TRX has no counters.'}
            total += int(counts.get('total', 0)); passed += int(counts.get('passed', 0))
            for case in root.findall('.//t:UnitTestResult', ns):
                if case.get('outcome') not in ['Passed', 'NotExecuted']:
                    failures.append({'test': case.get('testName'), 'error': (case.findtext('.//t:Message', '', ns))[:2000]})
        return {'valid': total > 0 and passed == total, 'total': total, 'passed': passed, 'failures': failures[:20]}
    if kind == 'native':
        p = folder/'native/report.json'
        if not p.exists(): return {'valid': False, 'reason': 'No native report.'}
        data = read_json(p); checks = data.get('checks', {})
        return {'valid': data.get('nativeMouseInput') == 'passed' and bool(checks) and all(checks.values()), 'checks': len(checks), 'failed': [k for k,v in checks.items() if not v]}
    if kind == 'playtest':
        path = folder/'playtests/report.json'
        if not path.exists(): return {'valid': False, 'reason': 'No recorded playtest suite report.'}
        data = read_json(path); checks = data.get('checks', [])
        return {'valid': data.get('passed') is True and bool(checks) and all(c.get('passed') is True for c in checks)
                and {'world', 'world-battle'}.issubset(data.get('scenes', [])),
                'checks': len(checks), 'failed': [c.get('name') for c in checks if c.get('passed') is not True],
                'paidModelInvoked': data.get('paidModelInvoked', False)}
    if kind in ['valid-json', 'passed-json']:
        data = read_json(log); return {'valid': data.get('valid' if kind == 'valid-json' else 'passed') is True}
    return {'valid': True}

def worker(repo, run_id):
    import fcntl
    folder = run_dir(repo, run_id); path = folder/'run.json'; state = read_json(path)
    state['worker'] = identity(os.getpid())
    def persist(): state['heartbeat'] = utc(); atomic_json(path, state)
    persist()
    def finish(status, reason=''):
        state.update(status=status, reason=reason, finished=utc(), child=None, currentStep=None)
        state['artifacts'] = [{'path': str(p.relative_to(folder)), 'sha256': digest(p)} for p in sorted(folder.rglob('*'))
                              if p.is_file() and p.name not in ['run.json', 'worker.log', 'cancel.json'] and not p.name.endswith('.tmp')]
        persist()
    try:
        with (repo.artifacts/'verification.lock').open('a') as lock:
            # One active verifier per checkout avoids concurrent builds corrupting shared outputs.
            while True:
                if (folder/'cancel.json').exists(): finish('cancelled'); return
                try: fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB); break
                except BlockingIOError: persist(); time.sleep(.5)
            if state['source']['sha256'] != repo.fingerprint()['sha256']:
                finish('stale', 'Source changed while queued; start a new run for the current tree.'); return
            if any(s['name'] == 'native' for s in state['steps']) and not repo.doctor()['native']['available']:
                finish('blocked', 'Native prerequisites missing; run ./dev agent doctor. No graphical check passed.'); return
            if any(s['name'] == 'playtest-suite' for s in state['steps']) and not all(__import__('shutil').which(tool) for tool in ['xvfb-run', 'xdotool']):
                finish('blocked', 'The recorded playtest suite requires xvfb-run and xdotool.'); return
            state['status'] = 'running'; persist()
            for step in state['steps']:
                if (folder/'cancel.json').exists(): finish('cancelled'); return
                state['currentStep'] = step['name']; persist(); started = time.monotonic()
                log = folder/(step['name']+'.log'); outcome = None
                with log.open('wb') as stream:
                    child = subprocess.Popen(step['argv'], cwd=repo.root, stdin=subprocess.DEVNULL, stdout=stream, stderr=subprocess.STDOUT,
                                             start_new_session=True, env={**os.environ, 'LIBGL_ALWAYS_SOFTWARE': '1'})
                    state['child'] = identity(child.pid); persist()
                    while child.poll() is None:
                        if (folder/'cancel.json').exists(): outcome = 'cancelled'
                        elif time.monotonic()-started > step['timeout']: outcome = 'timed_out'
                        if outcome: stop_child(state['child']); child.wait(timeout=5); break
                        persist(); time.sleep(.3)
                    exit_code = child.wait(); state['child'] = None
                try: evidence = inspect_evidence(step, folder, log)
                except (OSError, ValueError, ET.ParseError) as e: evidence = {'valid': False, 'reason': str(e)}
                passed = exit_code == 0 and evidence['valid'] and outcome is None
                state['results'].append({'name': step['name'], 'status': 'passed' if passed else outcome or 'failed',
                    'exitCode': exit_code, 'seconds': round(time.monotonic()-started, 2), 'evidence': evidence, 'log': log.name})
                persist()
                if not passed: finish(outcome or 'failed', tail(repo, run_id, step['name'], 15)['text']); return
            finish('passed' if state['source']['sha256'] == repo.fingerprint()['sha256'] else 'stale',
                   'All requested checks exited successfully. Review requirement coverage and rendered artifacts separately.')
    except BaseException as e:
        stop_child(state.get('child')); finish('failed', type(e).__name__ + ': ' + str(e))

if __name__ == '__main__':
    worker(Repository(sys.argv[1]), identifier(sys.argv[2]))
