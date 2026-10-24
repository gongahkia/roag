"""Native OS mouse acceptance for ordinary launches, separate from the test transport."""
import json
import os
from pathlib import Path
import subprocess
import sys
import time
from agent_support.workspace import Repository, atomic_json
from . import recordings


def worker(repo, output, no_recording=False):
    output = Path(output).resolve(); output.mkdir(parents=True, exist_ok=True)
    root = output / 'recordings'; slot = output / 'ordinary.json'
    argv = [str(repo.root / 'tools/dotnet.sh'), str(repo.root / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll'),
            '--seed', '1337', '--save', str(slot), '--session-root', str(root), '--window-size', '1024x720']
    if no_recording:
        argv.append('--no-recording')
    def x(*args):
        return subprocess.run(['xdotool', *map(str, args)], check=True, capture_output=True, text=True, timeout=5).stdout.strip()
    with (output / 'client.log').open('wb') as log:
        process = subprocess.Popen(argv, cwd=repo.root, stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT,
                                   env={**os.environ, 'SDL_VIDEODRIVER': 'x11', 'SDL_AUDIODRIVER': 'dummy', 'LIBGL_ALWAYS_SOFTWARE': '1'})
        try:
            deadline = time.monotonic() + 30; window = None
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    raise ValueError('Ordinary client exited before readiness.')
                windows = subprocess.run(['xdotool', 'search', '--all', '--onlyvisible', '--pid', str(process.pid), '--name', 'Vagabonds'], capture_output=True, text=True).stdout.split()
                if windows:
                    window = windows[-1]; break
                time.sleep(.05)
            if not window:
                raise TimeoutError('Ordinary game window did not appear.')
            x('windowfocus', '--sync', window); time.sleep(.5)
            def click(px, py):
                x('mousemove', '--window', window, px, py); time.sleep(.08); x('mousedown', 1); time.sleep(.2); x('mouseup', 1); time.sleep(.2)
            # Fixed minimum-size menu controls; real resulting artifacts, not click
            # delivery alone, are the assertion oracle for this ordinary launch.
            click(966, 33)
            if not no_recording:
                click(220, 352)
                deadline = time.monotonic() + 5
                while time.monotonic() < deadline and not list(root.glob('*/marker-00.png')):
                    time.sleep(.05)
                if not list(root.glob('*/marker-00.png')):
                    raise ValueError('The native Mark this moment click produced no screenshot.')
                marker = recordings.read(next(root.glob('*/marker-00.json')))
                # A normal launch must advance while the player stands still, including
                # with X11 focus elsewhere. Automation previews intentionally keep holding.
                initial = marker['data']['state']['world']
                if initial['paused'] or initial['tick'] <= 0:
                    raise ValueError('Ordinary world did not start running before any movement.')
                x('windowfocus', '0')
                deadline = time.monotonic() + 15
                background = None
                while time.monotonic() < deadline:
                    latest = recordings.read(next(root.glob('*/latest.json')), {}).get('state', {})
                    if latest.get('ui', {}).get('nativeFocused') is False and latest['world']['tick'] > initial['tick']:
                        background = latest; break
                    time.sleep(.1)
                if background is None or background['world']['paused'] or background['world']['frozen'] or background['world']['position'] != initial['position']:
                    raise ValueError('Unfocused world did not keep running while the player stood still.')
                atomic_json(output / 'background-clock.json', {'initial': marker['data']['state'], 'background': background})
                x('windowfocus', '--sync', window)
                click(966, 33)
            click(220, 398); process.wait(timeout=10)
            if process.returncode != 0 or not slot.is_file():
                raise ValueError('Ordinary Save and quit did not finish successfully.')
            if no_recording:
                result = {'passed': not root.exists(), 'recordingDisabled': True, 'saveWritten': True}
            else:
                entries = recordings.sessions(root)
                report = recordings.report(root, entries[0]['id'])
                result = {'passed': report['summary']['status'] == 'closed' and report['metadata']['identity']['mode'] == 'ordinary'
                          and report['latest']['state']['seed'] == 1337 and bool(report['summary']['markers']),
                          'session': entries[0]['id'], 'recording': report, 'saveWritten': True}
            atomic_json(output / 'report.json', result)
            return result
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill(); process.wait()


def run(repo, output):
    checks = []
    output = Path(output).resolve(); output.mkdir(parents=True, exist_ok=True)
    for name, disabled in [('ordinary', False), ('disabled', True)]:
        argv = ['xvfb-run', '-a', '-s', '-screen 0 1280x900x24', sys.executable, '-m', 'playtest_support.ordinary', str(output / name)]
        if disabled:
            argv.append('--no-recording')
        result = subprocess.run(argv, cwd=repo.root / 'tools', capture_output=True, text=True, timeout=60)
        (output / (name + '.log')).write_text(result.stdout + result.stderr)
        data = recordings.read(output / name / 'report.json', {})
        checks.append({'name': name, 'passed': result.returncode == 0 and data.get('passed') is True})
    # A deliberately incompatible isolated fixture verifies startup crash capture
    # and preservation without ever opening a real player slot.
    source = output / 'ordinary/ordinary.json'
    fixture = output / 'incompatible.json'
    value = json.loads(source.read_text()); value['contentHash'] = 'incompatible-test-fixture'; fixture.write_text(json.dumps(value))
    original = fixture.read_bytes(); root = output / 'startup-error'
    result = subprocess.run([str(repo.root / 'tools/dotnet.sh'), str(repo.root / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll'),
                             '--load', '--save', str(fixture), '--session-root', str(root)], cwd=repo.root, capture_output=True, text=True, timeout=20)
    (output / 'startup-error.log').write_text(result.stdout + result.stderr)
    recorded = recordings.sessions(root)
    crash_report = recordings.report(root, recorded[0]['id']) if recorded else {}
    checks.append({'name': 'startup-error-preserves-save-and-records-managed-exception', 'passed': result.returncode == 1
                   and fixture.read_bytes() == original and crash_report.get('summary', {}).get('status') == 'crashed'
                   and (root / recorded[0]['id'] / 'crash.json').is_file() if recorded else False})
    return {'passed': all(c['passed'] for c in checks), 'checks': checks, 'output': str(output)}


if __name__ == '__main__':
    try:
        repository = Repository(Path(__file__).resolve().parents[2])
        value = worker(repository, Path(sys.argv[1]), '--no-recording' in sys.argv[2:])
        print(json.dumps(value)); raise SystemExit(0 if value['passed'] else 1)
    except (ValueError, OSError, TimeoutError, subprocess.SubprocessError) as error:
        print(json.dumps({'passed': False, 'error': str(error)})); raise SystemExit(1)
