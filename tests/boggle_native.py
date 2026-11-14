"""Native X11 journeys against the passive observer; requires xdotool/ffmpeg.

Run with DISPLAY and ROAG_BOGGLE_NATIVE_OUTPUT pointing at the launched disposable
app. All gameplay input goes through the X server, never Lua input callbacks.
"""
import os
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ['ROAG_BOGGLE_NATIVE_OUTPUT'])
DISPLAY = os.environ['DISPLAY']
STATE = OUT / 'native-state.roag'


def xdo(*args):
    subprocess.run(['xdotool', *map(str, args)], check=True, stdout=subprocess.DEVNULL)


def query(plan=False):
    if not STATE.exists():
        return 'missing'
    args = ['luajit', 'tests/boggle_native_plan.lua', str(STATE)]
    if not plan:
        args.append('--status')
    return ' '.join(subprocess.check_output(args, cwd=ROOT, text=True).split())


def wait(check, timeout=25):
    end = time.monotonic() + timeout
    while time.monotonic() < end:
        status = query()
        if check(status):
            return status
        time.sleep(.12)
    raise AssertionError('Native journey timed out: ' + query())


def point(cell):
    return round(672 + ((cell - 1) % 6) * 53 + 24.5), round(112 + ((cell - 1) // 6) * 53 + 24.5 + 64)


def shot(name):
    time.sleep(.15)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-f', 'x11grab', '-video_size',
                    '1024x640', '-i', DISPLAY + '+0,64', '-frames:v', '1', str(OUT / (name + '.png'))], check=True)


def drag(cells, slow=.04):
    xdo('mousemove', *point(cells[0]), 'mousedown', 1)
    for cell in cells[1:]:
        xdo('mousemove', *point(cell))
        time.sleep(slow)
    xdo('mouseup', 1)
    time.sleep(.10)


def action_changed(previous):
    return wait(lambda s: s != 'busy' and s != previous)


wait(lambda s: s == 'home')
xdo('key', 'Return')
wait(lambda s: s.startswith('combat 0'))
shot('native-opening')

# One fast sample crosses the harmful Wound cell; release never commits.
xdo('mousemove', *point(9), 'mousedown', 1, 'mousemove', *point(12), 'mouseup', 1)
time.sleep(.15)
assert query().startswith('combat 0')
shot('native-fast-harmful')
xdo('key', 'c')
xdo('mousemove', *point(9), 'mousedown', 1, 'mousemove', *point(12))
time.sleep(.1)
xdo('mousemove', *point(10), 'mouseup', 1)
shot('native-backtracked')
assert query().startswith('combat 0')

xdo('key', 'c')
drag([5, 4, 3, 9], .12)
shot('native-safer-boon-preview')
assert query().startswith('combat 0')
xdo('key', 'c')
drag([10, 9, 15, 21], .08)
shot('native-costly-preview')
xdo('key', 'Return')
wait(lambda s: s.startswith('combat 400'))
shot('native-costly-after')

old = query()
xdo('key', 'y')
action_changed(old)
shot('native-shuffle')
old = query()
xdo('key', 'Escape')
time.sleep(.15)
shot('native-reset-cost')
xdo('key', 'e')
action_changed(old)
shot('native-reset-after')
for _ in range(12):
    if query() == 'death':
        break
    old = query()
    xdo('key', 'Escape')
    time.sleep(.1)
    xdo('key', 'e')
    action_changed(old)
assert query() == 'death'
shot('native-death')
xdo('key', 'Return')
wait(lambda s: s.startswith('combat 0'))
shot('native-replay')

routes = 0
while routes < 45:
    status = query(plan=True)
    if status == 'victory':
        break
    if status.startswith('edit'):
        cell = int(status.split()[1])
        shot('native-intermission')
        xdo('key', 'r')
        xdo('mousemove', *point(cell), 'click', 1)
        shot('native-rotate-preview')
        xdo('key', 'Return')
        wait(lambda s: s.startswith('combat'))
        shot('native-fight-two')
        continue
    assert status.startswith('route'), status
    cells = [int(x) for x in status.split()[1].split(',')]
    old = query()
    drag(cells)
    if routes in (0, 1, 2, 3, 5, 7, 17):
        shot(f'native-route-{routes + 1}-preview')
    xdo('key', 'Return')
    result = action_changed(old)
    routes += 1
    print('Native route', routes, cells, result, flush=True)
    if routes in (1, 2, 3, 4, 6, 8, 18):
        shot(f'native-route-{routes}-after')
assert query() == 'victory'
shot('native-victory')
xdo('key', 'Return')
wait(lambda s: s.startswith('combat 0'))
xdo('key', 'Escape', 't')
wait(lambda s: s == 'home')

# Existing mode menu paths, using the untouched disposable active run for Classic.
xdo('key', 't', 'Right', '2', 'Return')
wait(lambda s: s.startswith('poker 0'))
xdo('key', 'Return', 'a')
wait(lambda s: s.startswith('poker 100'))
shot('native-poker')
xdo('key', 'Escape', 't')
wait(lambda s: s == 'home')
xdo('key', 't', 'Tab', '1', 'Return', 'a')
wait(lambda s: s.startswith('fixed 100'))
shot('native-fixed')
xdo('key', 'Escape', 't')
wait(lambda s: s == 'home')
xdo('key', 'c', 'Return')
wait(lambda s: s.startswith('classic 0'))
xdo('key', 'd')
wait(lambda s: s.startswith('classic 100'))
shot('native-classic')
xdo('key', 'F12')  # Passive observer's SDL quit hook; no gameplay mutation.
(OUT / 'native-results.txt').write_text(f'PASS native X11 slow/fast dragging, harmful crossing, backtrack, release/commit, paid utilities, death/replay, {routes} routes through both fights/Rotate/victory, Poker/Fixed/Classic launch+movement.\n')
print('PASS native X11 journey')
