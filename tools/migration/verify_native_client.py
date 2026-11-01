#!/usr/bin/env python3
"""Exercise native rendered controls using real mouse states and X11 key presses.

Run after building the client:
  LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a \
    python3 tools/migration/verify_native_client.py artifacts/migration/verification/native-client/interactive
OpenAL null/SDL dummy keep initial and resumed test clients muted.
This is a focused rendered checkpoint, not full-tranche end-to-end acceptance.
"""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
output = Path(sys.argv[1]).resolve()
output.mkdir(parents=True, exist_ok=True)
control = output / 'control'
if control.exists():
    raise SystemExit('Use a fresh evidence directory; existing evidence is preserved.')
control.mkdir()
slot = control / 'campaign.json'
dll = ROOT / 'src/Roag.Client/bin/Debug/net10.0/Roag.Client.dll'
log = (output / 'client.log').open('wb')
environment = {**os.environ, 'ALSOFT_DRIVERS': 'null', 'SDL_AUDIODRIVER': 'dummy'}
process = subprocess.Popen([str(ROOT / 'tools/dotnet.sh'), str(dll), '--new', '--seed', '42',
    '--save', str(slot), '--playtest-control', str(control), '--window-size', '1024x720', '--no-recording'],
    cwd=ROOT, env=environment, stdout=log, stderr=subprocess.STDOUT)

def until(check, seconds=30):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError('Client exited: ' + (output / 'client.log').read_text()[-4000:])
        value = check()
        if value: return value
        time.sleep(.025)
    raise TimeoutError('Rendered client did not reach the expected boundary.')

def read(name):
    path = control / name
    return json.loads(path.read_text()) if path.exists() else None

sequence = 0
def request(kind, **args):
    global sequence
    sequence += 1
    observation = read('observation.json')
    spec = dict(schemaVersion=1, id='native-' + str(sequence), observation=observation['observation'], kind=kind, **args)
    temporary = control / 'request.tmp'
    temporary.write_text(json.dumps(spec)); temporary.replace(control / 'request.json')
    response = until(lambda: (r if (r := read('response.json')) and r['id'] == spec['id'] else None))
    assert response['success'], response
    return read('observation.json')['ui']

def evidence(name):
    request('capture')
    shutil.copy2(control / 'frame.png', output / (name + '.png'))
    shutil.copy2(control / 'observation.json', output / (name + '.json'))

def physical_key(key):
    global sequence
    sequence += 1
    observation = read('observation.json')
    spec = dict(schemaVersion=1, id='native-' + str(sequence), observation=observation['observation'], kind='wait', seconds=.4)
    temporary = control / 'request.tmp'; temporary.write_text(json.dumps(spec)); temporary.replace(control / 'request.json')
    until(lambda: not (control / 'request.json').exists())
    subprocess.run(['xdotool', 'keydown', key], check=True)
    try:
        until(lambda: (r if (r := read('response.json')) and r['id'] == spec['id'] else None))
        return read('observation.json')['ui']
    finally: subprocess.run(['xdotool', 'keyup', key], check=True)

try:
    initial = until(lambda: read('observation.json'))['ui']
    assert initial['panel'] == 'dungeon-class', initial['panel']
    evidence('01-class-choice')
    chosen = request('control', control='dungeon:class:12') # Tinkerer
    assert chosen['nativeDungeon']['player'][0]['classId'] == 'Tinkerer'
    assert chosen['nativeDungeon']['clock'] == 0
    window = subprocess.check_output(['xdotool', 'search', '--all', '--onlyvisible', '--pid', str(process.pid), '--name', '^Roag$'], text=True).splitlines()[-1]
    subprocess.run(['xdotool', 'windowfocus', window], check=True)
    physical_key('r')
    position = chosen['position']
    placed = request('tile', x=position['x'] + 1, y=position['y'])
    assert placed['nativeDungeon']['player'][0]['resources']['sentry'] is not None, placed['notice']
    evidence('02-sentry')
    moved = physical_key('s')
    assert moved['position'] == dict(x=position['x'], y=position['y'] + 1, floor=position['floor']), moved['notice']
    clock = moved['nativeDungeon']['clock']
    def keypress(key):
        ui = physical_key(key); assert ui['nativeDungeon']['clock'] == clock; return ui
    assert keypress('F1')['panel']=='dungeon-guide'
    evidence('03-keyboard-help')
    assert keypress('Escape')['panel']==''
    assert keypress('Escape')['panel']=='dungeon-menu'
    keypress('Down')
    assert keypress('Return')['panel']=='dungeon-guide'
    assert keypress('Escape')['panel']==''
    # Inspection and rendering must not advance the shared action clock.
    request('control', control='dungeon:boons'); time.sleep(.2)
    observed = request('capture')
    assert observed['nativeDungeon']['clock'] == clock
    request('control', control='dungeon:close')
    utility = request('control', control='dungeon:utility')
    assert utility['panel'] == 'dungeon-utility'
    assert utility['nativeDungeon']['clock'] == clock
    evidence('03-empty-utility-inspection')
    request('control', control='dungeon:close')
    request('control', control='dungeon:menu')
    request('control', control='dungeon:settings')
    evidence('04-default-presentation')
    for setting in ['master','music','effects','shake','flash','combat-effects','numbers','font','text-size','contrast','motion']:
        preferences = request('control', control='dungeon:setting:' + setting)
        assert preferences['nativeDungeon']['clock'] == clock
    appearance = preferences['nativeDungeon']['presentation']
    assert appearance['font'] == 'Sans' and appearance['textSize'] == 'Large'
    assert appearance['highContrast'] and appearance['reducedMotion']
    assert appearance['effectsVolume'] == 0 and appearance['musicVolume'] == 0
    assert not appearance['shake'] and not appearance['flash']
    assert not appearance['combatEffects'] and not appearance['damageNumbers']
    evidence('05-accessible-presentation')
    request('control', control='dungeon:close')
    saved = request('control', control='dungeon:save'); assert slot.exists(), saved['notice']
    evidence('03-paid-keyboard-save')
    request('stop'); process.wait(timeout=10)
    log.close()
    for name in ['observation.json', 'response.json', 'frame.png']:
        path = control / name
        if path.exists(): path.rename(control / ('before-resume-' + name))
    log = (output / 'resume-client.log').open('wb')
    process = subprocess.Popen([str(ROOT / 'tools/dotnet.sh'), str(dll), '--load', '--save', str(slot),
        '--playtest-control', str(control), '--window-size', '1024x720', '--no-recording'],
        cwd=ROOT, env=environment, stdout=log, stderr=subprocess.STDOUT)
    resumed = until(lambda: read('observation.json'))['ui']
    assert resumed['nativeDungeon']['clock'] == clock
    assert resumed['position'] == moved['position']
    assert resumed['nativeDungeon']['player'] == saved['nativeDungeon']['player']
    for field in ['master','music','effects','font','textSize','highContrast','reducedMotion','shake','flash','combatEffects','damageNumbers']:
        assert resumed['nativeDungeon']['presentation'][field] == appearance[field]
    assert resumed['panel'] == ''
    evidence('04-resumed-native-run')
    request('stop'); process.wait(timeout=10)
    (output / 'result.json').write_text(json.dumps(dict(passed=True, seed=42, classId='Tinkerer',
        clock=clock, checks=['native class selection', 'mouse-aimed sentry', 'paid physical keyboard movement',
        'free inspection/rendering', 'free physical keyboard help/menu navigation', 'free utility inspection', 'all presentation controls consume no time', 'persisted sans/large/high-contrast/reduced-motion/mute preferences',
        'continuing save', 'rendered resume with exact class/resources/supplies/clock'], audio=appearance, display=os.environ.get('DISPLAY')), indent=2) + '\n')
    print('Native rendered class, construct, keyboard, inspection, save and exact resume checkpoint passed.')
finally:
    if process.poll() is None:
        process.terminate()
        try: process.wait(timeout=10)
        except subprocess.TimeoutExpired: process.kill(); process.wait()
    log.close()
