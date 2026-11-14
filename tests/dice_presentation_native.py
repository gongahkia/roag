"""Native X11 input against the disposable GUI observer (requires xdotool)."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time

root = Path(os.environ.get('ROAG_DICE_GUI_OUTPUT', '/tmp/roag-dice-gui'))
xdotool = os.environ.get('XDOTOOL', 'xdotool')

def state():
    for _ in range(30):
        try:
            return json.loads((root / 'status.json').read_text())
        except (ValueError, FileNotFoundError):
            time.sleep(.02)
    raise RuntimeError('missing GUI observer status')

def native(*args):
    subprocess.run([xdotool, *args], check=True)

def key(k):
    native('key', '--clearmodifiers', k)
    time.sleep(.12)

def eventually(predicate):
    deadline = time.time() + 3
    while time.time() < deadline:
        s = state()
        if predicate(s):
            return s
        time.sleep(.03)
    raise AssertionError('GUI status did not reach the expected state')

def ready():
    deadline = time.time() + 10
    while time.time() < deadline:
        if (root / 'error.txt').exists():
            raise RuntimeError((root / 'error.txt').read_text())
        s = state()
        if not s['busy']:
            return s
        time.sleep(.05)
    raise RuntimeError('playback did not finish')

window = subprocess.check_output([xdotool, 'search', '--name', '^ROAG$'], text=True).splitlines()[-1]
native('windowfocus', window)
# Native inspection of every die, then refold and cancel a target preview.
time.sleep(.3)
# Cancel an unfolding net and replay while it is refolding.
native('key', '--delay', '15', 'i', 'Escape', 'Escape', 'r')
time.sleep(.3)
assert state()['time'] == 0 and state()['hp'] == 24
print('NATIVE LIFECYCLE: replay during net motion passed', flush=True)
key('i')
assert eventually(lambda s: s['inspect'])['time'] == 0
for die in ['1', '2', '3']:
    key(die)
    time.sleep(.22)
key('Escape')
time.sleep(.2)
key('f')
native('mousemove', '--window', window, '328', '328')
time.sleep(.3)
key('Escape')
assert state()['time'] == 0
# Existing mixer APIs are controlled by the observer; all interactions remain native.
(root / 'mix.txt').write_text('0')
eventually(lambda s: s['effects'] == 0)
count = state()['audio']
key('i')
time.sleep(.2)
key('Escape')
assert state()['audio'] == count, 'muted fold emitted a voice'
(root / 'mix.txt').write_text('0.25')
eventually(lambda s: s['effects'] == .25)
key('i')
time.sleep(.05)
assert state()['audio'] > count
assert abs(state()['dice_volume'] - .0875) < .001, state()
key('Escape')
(root / 'master.txt').write_text('0')
eventually(lambda s: s['master'] == 0)
key('i')
key('Escape')
(root / 'master.txt').write_text('1')
(root / 'mix.txt').write_text('0.9')
eventually(lambda s: s['effects'] == .9 and s['master'] == 1)
print('GUI MIX: effects mute, quarter-volume dice voices and master mute passed', flush=True)
# Toggle a trial-local preference without touching normal settings.
key('Escape')
key('m')
assert state()['reduced']
key('Return')
key('i')
time.sleep(.2)
key('Escape')
key('w')
assert eventually(lambda s: s['time'] == 70)['reduced']
ready()
key('Escape')
key('r')
assert eventually(lambda s: s['time'] == 0)['reduced']
print('NATIVE REDUCED: actual Footwork, immediate results and replay passed', flush=True)
key('Escape')
key('m')
key('Return')
assert not state()['reduced']
if os.environ.get('ROAG_DICE_SMOKE_ONLY') == '1':
    key('Escape')
    key('t')
    time.sleep(.3)
    raise SystemExit(0)

directions = {(0, -1): 'w', (1, -1): 'e', (1, 0): 'd', (1, 1): 'c',
              (0, 1): 's', (-1, 1): 'z', (-1, 0): 'a', (-1, -1): 'q'}
for turn in range(80):
    s = ready()
    if s['phase'] == 'victory':
        print('NATIVE VICTORY', s, flush=True)
        time.sleep(.4)
        key('r')
        assert ready()['time'] == 0 and ready()['hp'] == 24
        key('i')
        time.sleep(.3)
        print('NATIVE REPLAY: original dice restored', flush=True)
        key('Escape')
        key('Escape')
        key('t')
        time.sleep(.4)
        assert (root / 'title.json').exists()
        print('NATIVE TITLE RETURN', flush=True)
        break
    if s['phase'] == 'face_edit':
        print('NATIVE EDIT: Footwork face 1 -> Quick Step', flush=True)
        time.sleep(.25)
        native('mousemove', '--window', window, '714', '124', 'click', '1')
        time.sleep(.2)
        native('mousemove', '--window', window, '804', '228', 'click', '1')
        time.sleep(.2)
        key('Down')
        key('Down')
        time.sleep(.4)
        key('Escape')
        assert state()['time'] == s['time']
        key('Down')
        key('Down')
        time.sleep(.4)
        key('Return')
        time.sleep(.6)
        continue
    print('NATIVE ACTION', s, flush=True)
    if s['time'] == 70 and turn == 1:
        native('keydown', 'a')
        time.sleep(.8)
        native('keyup', 'a')
        assert state()['serial'] == s['serial'] + 1, 'held key crossed roll boundary'
        continue
    if s['die'] == 1 and turn % 2 == 0:
        x = 64 + (s['x'] + s['dx'] - 1) * 48 + 24
        y = 16 + (s['y'] + s['dy'] - 1) * 48 + 24
        native('mousemove', '--window', window, str(x), str(y))
        time.sleep(.15)
        native('click', '1')
        # A deliberate native double click must not spend a second result set.
        native('click', '--repeat', '2', '--delay', '20', '1')
    elif s['die'] == 2 and turn % 2 == 1:
        key('f')
        x = 64 + (s['x'] + s['dx'] - 1) * 48 + 24
        y = 16 + (s['y'] + s['dy'] - 1) * 48 + 24
        native('mousemove', '--window', window, str(x), str(y))
        time.sleep(.15)
        native('click', '1')
    else:
        key(str(s['die']))
        if s['face'] != 'guard':
            key(directions[(s['dx'], s['dy'])])
    eventually(lambda after: after['serial'] > s['serial'])
    assert state()['serial'] == s['serial'] + 1, 'command did not execute exactly once'
    if state()['busy']:
        native('key', 'f', 'w')
        time.sleep(.03)
        assert state()['serial'] == s['serial'] + 1, 'playback input crossed roll boundary'
else:
    raise RuntimeError('native journey exceeded action budget')

files = root / 'data/love/roag-foundation'
after = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in files.iterdir() if p.is_file()}
assert after == json.loads((root / 'storage-before.json').read_text()), 'player storage changed'
print('NATIVE STORAGE: four seeded player records unchanged', flush=True)
