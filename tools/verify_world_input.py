#!/usr/bin/env python3
"""Local X11/WSLg input acceptance; uses temporary save slots only."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dll', type=Path, default=ROOT / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll')
    parser.add_argument('--output', type=Path, default=ROOT / 'artifacts/native-integration')
    args = parser.parse_args()
    for tool in ('xdotool', 'ffmpeg'):
        if not shutil.which(tool): raise SystemExit(f'{tool} is required for local graphical input acceptance.')
    if not os.environ.get('DISPLAY'): raise SystemExit('An X11/WSLg DISPLAY is required.')
    output = args.output.resolve(); output.mkdir(parents=True, exist_ok=True)
    checks = {}
    def check(name, condition):
        checks[name] = bool(condition)
        if not condition: raise AssertionError(name)
    def x(*arguments):
        return subprocess.run(['xdotool', *map(str, arguments)], check=True, capture_output=True, text=True).stdout.strip()
    class Client:
        def __init__(self, scene, slot):
            self.slot = slot
            self.log = (output / (scene + '.log')).open('w')
            self.process = subprocess.Popen([str(ROOT / 'tools/dotnet.sh'), str(args.dll.resolve()), '--scene', scene, '--seed', '42', '--save', str(slot), '--window-size', '1024x720'], cwd=ROOT, stdout=self.log, stderr=subprocess.STDOUT)
            deadline = time.monotonic() + 20
            while time.monotonic() < deadline:
                if self.process.poll() is not None:
                    self.log.close(); raise RuntimeError(f'{scene} exited before its window was ready.')
                result = subprocess.run(['xdotool', 'search', '--onlyvisible', '--pid', str(self.process.pid), '--name', 'Vagabonds'], capture_output=True, text=True)
                if result.stdout.split():
                    self.window = result.stdout.split()[-1]
                    # WSLg can expose the window before rendering/input are ready. Pointer first, focus second.
                    x('mousemove', '--window', self.window, 100, 100)
                    time.sleep(2)
                    x('windowfocus', '--sync', self.window)
                    time.sleep(.3)
                    return
                time.sleep(.1)
            self.process.terminate(); self.process.wait(timeout=10); self.log.close()
            raise RuntimeError('Window readiness timed out.')
        def key(self, key):
            x('windowfocus', '--sync', self.window)
            x('keydown', key); time.sleep(.18); x('keyup', key); time.sleep(.18)
        def click(self, px, py):
            x('mousemove', '--window', self.window, px, py); x('windowfocus', '--sync', self.window)
            x('mousedown', 1); time.sleep(.18); x('mouseup', 1); time.sleep(.18)
        def save(self):
            self.key('F5')
            return json.loads(self.slot.read_text())
        def capture(self, name):
            subprocess.run(['ffmpeg', '-nostdin', '-loglevel', 'error', '-y', '-f', 'x11grab', '-window_id', self.window, '-video_size', '1024x720', '-i', os.environ['DISPLAY'], '-frames:v', '1', '-update', '1', str(output / (name + '.png'))], check=True, timeout=15)
        def close(self):
            if self.process.poll() is None:
                try:
                    self.key('Control_L+q'); check('cleanNativeExit', self.process.wait(timeout=15) == 0)
                finally:
                    if self.process.poll() is None: self.process.terminate(); self.process.wait(timeout=10)
            self.log.close()
    with tempfile.TemporaryDirectory(prefix='vagabonds-native-') as temporary:
        slot = Path(temporary) / 'world.json'
        client = Client('world', slot)
        try:
            initial = client.save()['state']
            x('mousemove', '--window', client.window, 385, 329); time.sleep(.3); client.capture('hover-route')
            client.key('b'); client.key('6'); client.click(353, 329)
            queued = client.save()['state']
            check('nativeWorkReservesBeforeBuilding', len(queued['work']) == 1 and not queued['structures'] and queued['inventory']['wood'] == initial['inventory']['wood'] - 4)
            client.key('Escape'); client.key('k'); client.key('Tab'); client.key('Return')
            assigned = client.save()['state']
            check('nativeWorkerAssignment', assigned['work'][0]['worker'] not in assigned['party'])
            client.capture('worker-panel'); client.key('Escape'); client.key('Shift_L+period')
            built = client.save()['state']
            check('nativeTimedCamp', not built['work'] and len(built['structures']) == 1 and built['tick'] >= 300)
            client.key('b'); client.key('1'); client.click(385, 329); client.key('Escape'); client.key('k'); client.key('c'); client.key('Escape')
            cancelled = client.save()['state']
            check('nativeCancellationRefund', not cancelled['work'] and cancelled['inventory']['stone'] == initial['inventory']['stone'])
            client.key('r'); rested = client.save()['state']
            check('nativeTimedRest', rested['tick'] == cancelled['tick'] + 300)
            client.key('c'); client.key('2'); client.key('Escape'); client.key('Shift_L+period')
            crafted = client.save()['state']
            recipe = json.loads((ROOT / 'content/recipe/camp-weapon.json').read_text())
            gear = sorted((json.loads(p.read_text()) for p in (ROOT / 'content/item').glob('*.json')), key=lambda item: item['id'])
            equipment = [item for item in gear if item.get('slot', 'none') != 'none']
            index = next(i for i, item in enumerate(equipment) if item['id'] == recipe['output'])
            check('nativeCrafting', crafted['inventory'].get(equipment[index]['stack'], 0) > 0)
            client.key('i')
            for _ in range(index): client.key('Down')
            client.key('Return'); equipped = client.save()['state']
            hero = next(a for a in equipped['actors'] if a['id'] == equipped['party'][0])
            check('nativeEquipment', hero['equipment']['weapon']['item'] == recipe['output'])
            client.capture('party-equipment'); client.key('Escape')
            client.key('space'); client.key('a'); client.key('space')
            before = client.save()['state']
            check('nativeMovement', before['position'] != equipped['position'])
            slot.write_text('{"format":"vagabonds-world","version":999}')
            client.key('F9'); restored = client.save()['state']
            check('nativeRejectedF9PreservesState', before == restored)
            client.capture('exploration')
        finally: client.close()
        client = Client('world-boss', slot)
        try:
            before = client.save()['state']; check('nativeBossEncounter', before['battle'] is not None)
            client.key('i'); client.key('1'); modal = client.save()['state']
            check('combatMenuConsumesNoTurnOrRandomness', before['battle'] == modal['battle'] and before['combatRandom'] == modal['combatRandom'] and before['tick'] == modal['tick'])
            client.key('Escape'); client.key('1'); after = client.save()['state']
            check('nativePartyAbility', after['battle'] != modal['battle'] and after['combatRandom'] != modal['combatRandom'])
            client.capture('boss-combat')
            # Explicit scenario fixture: wound the selected boss, retain legal actor/unit continuation.
            fixture = json.loads(slot.read_text()); state = fixture['state']; battle = state['battle']
            final_id = state['endgame']['actorId']; unit_index = battle['actorIds'].index(final_id)
            battle['units'][unit_index]['health'] = 1
            next(a for a in state['actors'] if a['id'] == final_id)['health'] = 1
            battle['turnIndex'] = next(i for i, u in enumerate(battle['order']) if battle['units'][u]['heroId'] >= 0 and battle['units'][u]['health'] > 0)
            slot.write_text(json.dumps(fixture)); client.key('F9'); client.key('1')
            ending = client.save()['state']; check('nativeFinalBossCompletion', ending['endgame']['victory'] and ending['battle'] is None)
            client.capture('victory'); client.key('n'); fresh = client.save()
            check('nativeFreshRun', fresh['plan']['seed'] != 42 and fresh['state']['tick'] == 0 and len(fresh['state']['party']) == 1 and not fresh['state']['endgame']['victory'])
        finally: client.close()
    report = {'nativeInput': 'passed', 'checks': checks, 'scope': 'Local X11/WSLg; temporary slots, authored work/crafting and an explicitly wounded-boss victory fixture. Not a balance playthrough.'}
    (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))

if __name__ == '__main__': main()
