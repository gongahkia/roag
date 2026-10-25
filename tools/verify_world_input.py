#!/usr/bin/env python3
"""Mouse-only world acceptance under local X11/WSLg, using temporary save slots."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dll', type=Path, default=ROOT / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll')
    parser.add_argument('--output', type=Path, default=ROOT / 'artifacts/native-mouse')
    parser.add_argument('--scenes', nargs='+', choices=['world', 'world-resources', 'world-wildlife', 'world-situation', 'world-camp', 'world-discovery', 'world-battle', 'world-cooperation', 'world-boss', 'world-progression', 'world-encounters'], default=['world', 'world-resources', 'world-wildlife', 'world-situation', 'world-camp', 'world-discovery', 'world-battle', 'world-cooperation', 'world-boss', 'world-progression', 'world-encounters'], help='Run selected isolated native scenarios.')
    args = parser.parse_args()
    for tool in ('xdotool', 'ffmpeg'):
        if not shutil.which(tool): raise SystemExit(f'{tool} is required.')
    if not os.environ.get('DISPLAY'): raise SystemExit('An X11/WSLg DISPLAY is required.')
    output = args.output.resolve(); output.mkdir(parents=True, exist_ok=True)
    (output / 'report.json').unlink(missing_ok=True)
    checks = {}
    def check(name, condition):
        checks[name] = bool(condition)
        if not condition: raise AssertionError(name)
    def x(*arguments):
        return subprocess.run(['xdotool', *map(str, arguments)], check=True, capture_output=True, text=True).stdout.strip()
    class Client:
        def __init__(self, scene, slot):
            self.slot = slot; self.report = slot.with_suffix('.ui.json'); self.window = None
            self.log = (output / (scene + '.log')).open('w')
            self.process = subprocess.Popen([str(ROOT / 'tools/dotnet.sh'), str(args.dll.resolve()), '--scene', scene, '--seed', '42', '--save', str(slot), '--window-size', '1024x720', '--ui-report', str(self.report)], cwd=ROOT, stdout=self.log, stderr=subprocess.STDOUT, env={**os.environ, 'SDL_VIDEODRIVER': 'x11'})
            try:
                deadline = time.monotonic() + 20
                while time.monotonic() < deadline:
                    if self.process.poll() is not None: raise RuntimeError(f'{scene} exited before readiness.')
                    found = subprocess.run(['xdotool', 'search', '--all', '--onlyvisible', '--pid', str(self.process.pid), '--name', 'Vagabonds'], capture_output=True, text=True).stdout.split()
                    if found:
                        self.window = found[-1]; x('mousemove', '--window', self.window, 100, 100); time.sleep(2)
                        x('windowfocus', '--sync', self.window); self.wait(lambda ui: len(ui['controls']) > 0); return
                    time.sleep(.1)
                raise RuntimeError('Window readiness timed out.')
            except BaseException:
                self.abort(); raise
        def ui(self):
            try: return json.loads(self.report.read_text())
            except (FileNotFoundError, json.JSONDecodeError): return {}
        def wait(self, predicate, timeout=12, interval=.05):
            deadline = time.monotonic() + timeout
            while time.monotonic() < deadline:
                if self.process.poll() is not None: raise RuntimeError('Client exited: ' + str(self.process.returncode))
                ui = self.ui()
                if ui and predicate(ui): return ui
                time.sleep(interval)
            raise RuntimeError('UI readiness timed out: ' + json.dumps(self.ui())[:1500])
        def point(self, px, py, button=1, fast=False, exit_ok=False):
            px, py = round(px), round(py)
            for attempt in range(3):
                if attempt > 0 or not self.ui().get('focused', False):
                    x('windowfocus', '0'); x('windowraise', self.window)
                x('windowfocus', '--sync', self.window); x('mousemove', '--window', self.window, px, py)
                try:
                    self.wait(lambda ui: ui['focused'] and abs(ui['pointer']['x'] - px) <= 1 and abs(ui['pointer']['y'] - py) <= 1, timeout=2)
                    break
                except RuntimeError:
                    if attempt == 2: raise
            self.wait(lambda u: not u['mouseInput']['leftPressed'] and not u['mouseInput']['rightPressed'], timeout=3, interval=.005)
            before = self.ui()['mouseInput']['pressSequence']
            x('mousedown', button)
            try:
                # The actual Update input edge is the receipt. Clock/animation
                # changes alone cannot acknowledge a click on a slow frame.
                def received(u):
                    return (u['mouseInput']['pressSequence'] > before and u['mouseInput']['lastPress']['button'] == button
                        and u['mouseInput']['lastPress']['x'] == px and u['mouseInput']['lastPress']['y'] == py)
                if exit_ok:
                    deadline = time.monotonic() + 3
                    while self.process.poll() is None and not received(self.ui()) and time.monotonic() < deadline: time.sleep(.005)
                    if self.process.poll() not in (None, 0): raise RuntimeError('Quit failed: ' + str(self.process.returncode))
                else: self.wait(received, timeout=3, interval=.005)
            finally: x('mouseup', button)
            if exit_ok: return  # Clean process exit acknowledges Save and quit.
            released = 'leftPressed' if button == 1 else 'rightPressed'
            if not fast:
                self.wait(lambda u: not u['mouseInput'][released], timeout=3, interval=.005)
                time.sleep(.06)
        def click(self, identifier, fast=False):
            ui = self.wait(lambda u: any(c['id'] == identifier and c['enabled'] for c in u['controls']))
            button = next(c for c in ui['controls'] if c['id'] == identifier); r = button['bounds']
            self.point(r['x'] + r['width'] / 2, r['y'] + r['height'] / 2, fast=fast, exit_ok=identifier == 'quit')
            if identifier != 'quit' and self.ui()['mouseInput']['lastPress']['control'] != identifier:
                raise RuntimeError('Mouse press hit a different control: ' + str(self.ui()['mouseInput']['lastPress']))
        def combat_ready(self):
            return self.wait(lambda u: not u.get('combatPresentation', {}).get('busy', False)
                and (u['battle'] is None or u['battle']['activeFriendly']))
        def fighter(self, unit_id, fast=False):
            r = next(u for u in self.ui()['battle']['units'] if u['id'] == unit_id)['bounds']
            self.point(r['x'] + r['width'] / 2, r['y'] + r['height'] / 2, fast=fast)
        def equipment(self, item_id):
            # Hidden/unowned relics and generated equipment change the displayed
            # order. Resolve stable IDs through the runtime view, then page using
            # real mouse controls instead of reproducing the UI's filtering.
            entries = self.ui()['equipment']; entry = next(e for e in entries if e['id'] == item_id)
            identifier = 'gear:' + str(entry['index'])
            for _ in range(len(entries) // 8 + 2):
                visible = [int(c['id'][5:]) for c in self.ui()['controls'] if c['id'].startswith('gear:')]
                if entry['index'] in visible:
                    self.click(identifier); return entry
                self.click('gearpage:' + ('1' if entry['index'] > max(visible) else '-1'))
            raise RuntimeError('Equipment page did not expose ' + item_id)
        def load(self):
            self.click('load')
            ui = self.wait(lambda u: u['notice'].startswith(('World restored', 'Load failed:')))
            if not ui['notice'].startswith('World restored'):
                # Preserve only the isolated harness slot and immutable snapshots.
                # Exact-state equality alone could pass when a rejected load kept
                # the old in-memory session, so successful reload is explicit.
                shutil.copy2(self.slot, output / 'rejected-fixture.json')
                shutil.copytree(str(self.slot) + '.chunks', output / 'rejected-fixture.json.chunks', dirs_exist_ok=True)
                raise RuntimeError(ui['notice'] + ' Fixture retained in ' + str(output / 'rejected-fixture.json'))
            return ui
        def restore(self, fixture):
            self.slot.write_text(json.dumps(fixture)); self.load(); return self.combat_ready()
        def capture_phase(self, phase, name, source=None):
            observed = self.wait(lambda u: u.get('combatPresentation', {}).get('phase') == phase
                and (source is None or u['combatPresentation']['source'] == source), interval=.01)
            x('windowfocus', '0')
            frozen = self.wait(lambda u: not u['focused'], interval=.01)
            check(name + 'Phase', frozen['combatPresentation']['phase'] == phase)
            time.sleep(.2)
            still = self.ui()
            check(name + 'FocusFreeze', still['combatPresentation']['progress'] == frozen['combatPresentation']['progress']
                and still['tick'] == frozen['tick'])
            self.capture(name)
            x('windowfocus', '--sync', self.window)
            self.wait(lambda u: u['focused'])
            return observed
        def finish_dialogue(self):
            deadline = time.monotonic() + 40
            while time.monotonic() < deadline:
                ui = self.ui()
                if ui.get('panel') not in ('person', 'camp-story') or ui.get('dialogue', {}).get('finished'): return
                self.click('dialogue-next')
            raise RuntimeError('Conversation failed to advance.')
        def tile_point(self, px, py):
            ui = self.ui(); r = ui['map']; camera = ui['camera']
            return (r['x'] + r['width'] // 2 + (px + .5 - camera['x']) * camera['cellSize'], r['y'] + r['height'] // 2 + (py + .5 - camera['y']) * camera['cellSize'])
        def tile(self, px, py, button=1):
            self.point(*self.tile_point(px, py), button=button)
        def context(self, cell):
            self.tile(cell['x'], cell['y'], button=3); self.wait(lambda u: u['context'] is not None)
        def free_cell(self, distance=1):
            ui = self.ui(); p = ui['position']
            return next(c for c in ui['cells'] if c['groundOpen'] and not c['occupied'] and c['prop'] is None and abs(c['x'] - p['x']) + abs(c['y'] - p['y']) == distance)
        def drag(self, start, end):
            self.formation()
            a = self.tile_point(start['x'], start['y']); b = self.tile_point(end['x'], end['y'])
            x('windowfocus', '--sync', self.window); x('mousemove', '--window', self.window, round(a[0]), round(a[1])); time.sleep(.12)
            x('mousedown', 1); time.sleep(.12)
            try:
                for i in range(1, 7):
                    x('mousemove', '--window', self.window, round(a[0] + (b[0] - a[0]) * i / 6), round(a[1] + (b[1] - a[1]) * i / 6)); time.sleep(.06)
            finally: x('mouseup', 1)
            self.wait(lambda u: len(u['selection']) > 0 and not u['selecting'])
        def pause(self):
            if not self.ui()['paused']: self.click('pause')
        def save(self):
            before = self.slot.stat().st_mtime_ns if self.slot.exists() else None
            self.click('save'); deadline = time.monotonic() + 5
            while time.monotonic() < deadline:
                if self.slot.exists() and self.slot.stat().st_mtime_ns != before: return json.loads(self.slot.read_text())
                time.sleep(.05)
            raise RuntimeError('Mouse Save did not write the temporary slot.')
        def capture(self, name):
            # Pair pixels with semantic state so agents can diagnose and review a
            # retained phase without inferring rules or target identities from art.
            (output / (name + '.ui.json')).write_text(json.dumps(self.ui(), indent=2) + '\n')
            subprocess.run(['ffmpeg', '-nostdin', '-loglevel', 'error', '-y', '-f', 'x11grab', '-window_id', self.window, '-video_size', '1024x720', '-i', os.environ['DISPLAY'], '-frames:v', '1', '-update', '1', str(output / (name + '.png'))], check=True, timeout=15)
        def formation(self):
            # Inspect actual per-member draw bounds as well as authoritative cells.
            # Waiting for interpolation avoids treating an unfinished step as stacking.
            def settled(ui):
                camera, viewport = ui['camera'], ui['map']
                for member in ui['party']:
                    bounds = member['bounds']
                    if bounds is None: return False
                    expected_x = viewport['x'] + viewport['width'] / 2 + (member['x'] + .5 - camera['x']) * camera['cellSize']
                    expected_y = viewport['y'] + viewport['height'] / 2 + (member['y'] + .5 - camera['y']) * camera['cellSize']
                    if abs(bounds['x'] + bounds['width'] / 2 - expected_x) > 1 or abs(bounds['y'] + bounds['height'] / 2 - expected_y) > 1: return False
                return True
            ui = self.wait(settled)
            cells = {(a['x'], a['y'], a['floor']) for a in ui['party']}
            return len(cells) == len(ui['party']) and all(a['floor'] == ui['position']['floor'] for a in ui['party'])
        def abort(self):
            if self.process.poll() is None: self.process.terminate(); self.process.wait(timeout=10)
            self.log.close()
        def close(self):
            if sys.exc_info()[0] is not None:
                try:
                    (output / 'report.json').write_text(json.dumps({'nativeMouseInput': 'failed', 'checks': checks}, indent=2) + '\n')
                    (output / 'failed-ui.json').write_text(json.dumps(self.ui(), indent=2) + '\n'); self.capture('failed-input')
                    if self.slot.exists():
                        shutil.copy2(self.slot, output / 'failed-save.json')
                        chunks = Path(str(self.slot) + '.chunks')
                        if chunks.is_dir(): shutil.copytree(chunks, output / 'failed-save.json.chunks', dirs_exist_ok=True)
                except Exception: pass
                self.abort(); return
            try:
                if self.process.poll() is None:
                    self.click('panel:game'); self.click('quit'); check('cleanMouseExit', self.process.wait(timeout=15) == 0)
            finally: self.abort()
    with tempfile.TemporaryDirectory(prefix='vagabonds-mouse-') as temporary:
        slot = Path(temporary) / 'world.json'
        if 'world' in args.scenes:
            client = Client('world', slot)
            try:
                initial = client.save()['state']; stamp = slot.stat().st_mtime_ns
                # These are deliberately the only keyboard events in this acceptance run.
                for key in ('space', 'w', 'F5', 'i', '1'):
                    x('keydown', key); time.sleep(.1); x('keyup', key); time.sleep(.1)
                check('keyboardGameplayDisabled', client.ui()['tick'] == initial['tick'] and client.ui()['position'] == {k: initial['position'][k] for k in ('x', 'y', 'floor')} and client.ui()['panel'] == '' and slot.stat().st_mtime_ns == stamp)
                ui = client.ui(); check('readableDefaultCamera', ui['camera']['cellSize'] >= 36 and ui['map']['width'] / ui['camera']['cellSize'] <= 48.01)
                check('stationaryTerrainReusesBoundedLayer', ui['terrainCache']['hits'] > 0 and ui['terrainCache']['bytes'] == ui['width'] * ui['height'] * 4)
                client.capture('exploration')
                client.click('panel:game'); client.click('panel:guide'); client.capture('guide'); client.click('close')
                client.click('panel:game'); client.click('panel:inspector'); client.click('inspect:2'); client.capture('inspector')
                check('mouseGuideAndInspector', any(c['id'] == 'inspect:2' and c['selected'] for c in client.ui()['controls'])); client.click('close')
                terrain_before_zoom = client.ui()['terrainCache']['rebuilds']
                for _ in range(12): client.click('zoom:out')
                ui = client.ui(); check('boundedMouseZoom', ui['camera']['cellSize'] >= 24 and ui['map']['width'] / ui['camera']['cellSize'] <= 48.01)
                check('zoomInvalidatesTerrainLayer', ui['terrainCache']['rebuilds'] > terrain_before_zoom)
                x('windowsize', client.window, '1280', '800')
                resized = client.wait(lambda u: u['width'] == 1280 and u['height'] == 800 and u['terrainCache']['bytes'] == 1280 * 800 * 4)
                check('resizeReallocatesBoundedTerrainLayer', resized['terrainCache']['rebuilds'] > ui['terrainCache']['rebuilds'])
                x('windowsize', client.window, '1024', '720')
                client.wait(lambda u: u['width'] == 1024 and u['height'] == 720 and u['terrainCache']['bytes'] == 1024 * 720 * 4)
                client.click('zoom:default')
                ui = client.ui(); origin = ui['position']
                terrain_before_hover = ui['terrainCache']
                hover = next(c for c in ui['cells'] if c['groundOpen'] and not c['occupied'] and c['clickable']
                    and c['y'] == origin['y'] and c['x'] == origin['x'] + 4)
                px, py = client.tile_point(hover['x'], hover['y']); x('mousemove', '--window', client.window, round(px), round(py))
                client.wait(lambda u: u['hover'] is not None)
                check('hoverDoesNotMoveOrQueueTravel', client.ui()['position'] == origin and client.ui()['routeCount'] == 0)
                terrain_after_hover = client.ui()['terrainCache']
                check('hoverOverlayDoesNotRebuildTerrain', terrain_after_hover['hits'] > terrain_before_hover['hits'] and terrain_after_hover['rebuilds'] == terrain_before_hover['rebuilds'])
                client.capture('hover-selector')
                ui = client.ui(); origin = ui['position']; barrel = next(c for c in ui['cells'] if c['prop'] == 'Barrel')
                client.tile(barrel['x'], barrel['y']); client.wait(lambda u: u['context'] is not None)
                check('solidPropClickOffersDismantlingWithoutWalkingThroughIt', client.ui()['position'] == origin and client.ui()['routeCount'] == 0 and any(c['id'] == 'context:dig' for c in client.ui()['controls']))
                client.click('context:clear')
                # Digging spends ordinary work time, never an upfront food charge.
                baseline = json.loads(slot.read_text()); depleted = json.loads(json.dumps(baseline))
                depleted['state']['inventory']['food'] = 0
                slot.write_text(json.dumps(depleted)); client.load(); client.context(barrel)
                ui = client.ui(); dig = next(c for c in ui['controls'] if c['id'] == 'context:dig')
                check('digWorksWithoutFoodAndExplainsWorkCost', dig['enabled'] and 'no food is reserved' in dig['tip'])
                client.capture('context-dig'); client.click('context:clear'); slot.write_text(json.dumps(baseline)); client.load()
                ui = client.ui(); origin = ui['position']; people = [a for a in ui['actors'] if a['kind'] == 'Person']
                terrain_before_travel = ui['terrainCache']['bypasses']
                person = min(people, key=lambda a: abs(a['x'] - origin['x']) + abs(a['y'] - origin['y']))
                client.tile(person['x'], person['y']); client.wait(lambda u: u['panel'] == 'person')
                check('mouseApproachConversation', True)
                check('movingViewBypassesTerrainTarget', client.ui()['terrainCache']['bypasses'] > terrain_before_travel)
                conversation_tick = client.ui()['tick']; time.sleep(.5); client.capture('conversation')
                check('conversationPausesWorldAndRevealsText', client.ui()['tick'] == conversation_tick and bool(client.ui()['dialogue']['text']))
                voice = client.ui()['dialogue']; check('conversationVoicePlaysOrReportsMissingDevice', voice['audioUnavailable'] or voice['playedSyllables'] > 0)
                check('conversationShowsTwoLargePortraits', len(voice['portraits']) == 2 and all(p['bounds']['width'] >= 128 for p in voice['portraits']))
                client.click('speech-motion'); check('conversationReducedMotion', client.ui()['dialogue']['reducedMotion'])
                client.click('speech-motion')  # Restore full motion for the digging capture below.
                client.click('voice-volume'); check('conversationVolumeControl', client.ui()['dialogue']['volume'] == .75)
                client.finish_dialogue(); client.click('conversation:interaction.stories')
                client.wait(lambda u: u['dialogue']['speaker'] == u['party'][0]['id']); client.capture('conversation-player')
                client.finish_dialogue(); client.click('conversation:interaction.trade'); client.finish_dialogue()
                check('conversationTradeFollowsSpokenReply', client.ui()['panel'] == 'trade')
                before = client.save()['state']; client.click('buy:food'); bought = client.save()['state']
                check('mouseTradeBuy', bought['inventory']['food'] == before['inventory']['food'] + 1)
                client.click('sell:food'); client.click('close')
                ui = client.ui(); person = next(a for a in ui['actors'] if a['id'] == person['id'])
                client.tile(person['x'], person['y']); client.wait(lambda u: u['panel'] == 'person'); client.finish_dialogue(); client.click('conversation:interaction.recruit'); client.finish_dialogue(); client.pause()
                recruited = client.save()['state']; check('mouseRecruitment', len(recruited['party']) == 2)
                check('recruitedMembersStandOnSeparateTiles', client.formation()); client.capture('formation-recruited')
                camp = client.free_cell(); client.context(camp); client.click('context:build'); client.click('context:construct:Camp')
                client.pause(); client.click('panel:work'); queued = client.save()['state']
                check('mouseReservationBeforeCompletion', len(queued['work']) == 1 and not queued['structures'] and queued['inventory']['wood'] == initial['inventory']['wood'] - 4)
                ui = client.ui(); worker = next(c for c in ui['controls'] if c['id'].startswith('worker:') and int(c['id'].split(':')[1]) not in queued['party'])
                client.click(worker['id']); client.click('assign'); assigned = client.save()['state']
                check('mouseWorkerAssignment', assigned['work'][0]['worker'] not in assigned['party']); client.capture('work')
                cards = client.ui()['controls']; check('workShowsTaskWorkerAndProgress', all(c['sprite'] for c in cards if c['id'].startswith(('work:', 'worker:'))) and any(c['badgeSprite'] and c['progress'] >= 0 for c in cards if c['id'].startswith('work:')))
                client.click('close'); client.pause(); client.click('panel:actions'); client.click('wait:300')
                built = client.save()['state']; check('mouseTimedConstruction', not built['work'] and len(built['structures']) == 1)
                wall = client.free_cell(); client.context(wall); client.click('context:build'); client.click('context:construct:Wall')
                client.pause(); client.click('panel:work'); client.click('work:' + str(client.ui()['work'][0]['id'])); client.click('cancel-work'); client.click('close'); client.pause()
                cancelled = client.save()['state']; check('mouseCancellationRefund', not cancelled['work'] and cancelled['inventory']['stone'] == initial['inventory']['stone'])
                client.context(camp); client.click('context:rest'); rested = client.save()['state']
                check('mouseTimedRest', rested['tick'] == cancelled['tick'] + 300)
                client.context(camp); client.click('context:craft'); client.click('craft:recipe.camp-weapon'); client.pause(); client.click('panel:actions'); client.click('wait:300')
                crafted = client.save()['state']; recipe = json.loads((ROOT / 'content/recipe/camp-weapon.json').read_text())
                client.click('panel:inventory'); equipment = client.equipment(recipe['output']); index = equipment['index']
                check('mouseCrafting', equipment['count'] > 0)
                client.click('pause'); party_tick = client.ui()['tick']; client.wait(lambda u: u['tick'] > party_tick + 2); check('partyPanelKeepsTimeRunning', client.ui()['panel'] == 'inventory'); client.pause(); client.click('equip'); equipped = client.save()['state']
                hero = next(a for a in equipped['actors'] if a['id'] == equipped['party'][0]); check('mouseEquipment', hero['equipment']['weapon']['item'] == recipe['output'])
                cards = client.ui()['controls']; check('partyShowsPortraitsAndItemSprites', all(c['sprite'] and c['spriteBounds']['height'] >= 30 for c in cards if c['id'].startswith(('party:', 'gear:'))))
                check('equippedItemHasVisibleWornLabel', any(c['id'] == 'gear:' + str(index) and 'worn' in c['label'] for c in cards))
                client.capture('party'); client.click('close'); client.click('panel:map'); client.capture('world-map'); client.click('close')
                client.click('panel:journal'); client.click('history:6'); client.click('history:-6'); client.capture('journal')
                check('mouseJournalPaging', client.ui()['panel'] == 'journal' and client.ui()['paused'])
                check('journalShowsIllustratedHistory', any(row['start'] and row['sprite'] != 'banner' for row in client.ui()['journal'])); client.click('close')
                # Real drag/right-click gestures drive batch work and saved approach intent.
                client.pause(); client.formation(); ui = client.ui(); origin = ui['position']
                free = {(c['x'], c['y']): c for c in ui['cells'] if c['groundOpen'] and not c['occupied'] and c['prop'] is None and c['material'] in ('Earth', 'Grass')}
                first = next(c for c in free.values() if 3 <= abs(c['x'] - origin['x']) + abs(c['y'] - origin['y']) <= 4 and (c['x'] + 1, c['y']) in free)
                second = free[(first['x'] + 1, first['y'])]
                client.drag(first, second); check('dragSelectsWithoutMoving', len(client.ui()['selection']) == 2 and client.ui()['position'] == origin and client.ui()['routeCount'] == 0)
                client.context(first); client.capture('context-selection')
                check('rightClickRetainsSelection', len(client.ui()['selection']) == 2 and any(c['id'] == 'context:build' for c in client.ui()['controls']))
                client.click('context:build'); client.capture('context-build'); client.click('context:construct:Floor')
                client.click('panel:work'); work_tick = client.ui()['tick']
                client.wait(lambda u: len([o for o in u['work'] if o['worker'] in [a['id'] for a in u['party']]]) == 2, timeout=5)
                check('partyWorkersClaimSeparateJobsInManagementPanel', len({o['worker'] for o in client.ui()['work']}) == 2 and not client.ui()['frozen'])
                client.wait(lambda u: u['tick'] > work_tick + 2)
                check('workPanelKeepsTimeRunning', True); client.capture('delegated-work')
                client.wait(lambda u: len(u['work']) == 0, timeout=25); client.pause(); client.click('close'); client.formation()
                paved = client.save()['state']; check('batchConstructionApproachesAndFinishes', all(any(b['position']['x'] == c['x'] and b['position']['y'] == c['y'] for b in paved['structures']) for c in (first, second)))
                client.drag(first, second); client.context(first); client.click('context:dig'); client.pause()
                pending = client.save()['state']; check('batchDigReservationsSaved', len(pending['work']) == 2 and all(w['partyApproach'] for w in pending['work']))
                client.load(); client.wait(lambda u: len(u['work']) == 2 and u['paused']); client.click('pause')
                client.wait(lambda u: any(o['kind'] == 'Dig' and o['status'].startswith('Working:') for o in u['work']), interval=.01)
                client.capture('dismantling')
                subprocess.run(['ffmpeg', '-nostdin', '-loglevel', 'error', '-y', '-f', 'x11grab', '-framerate', '30',
                    '-window_id', client.window, '-video_size', '1024x720', '-i', os.environ['DISPLAY'], '-t', '1.8',
                    '-c:v', 'libx264', '-pix_fmt', 'yuv420p', str(output / 'dismantling.mp4')], check=True, timeout=15)
                client.wait(lambda u: len(u['work']) == 0, timeout=25); client.pause()
                dug = client.save()['state']; check('batchDigResumesAfterLoadAndRefundsMaterials', dug['inventory']['stone'] == paved['inventory']['stone'] + 4 and len(dug['structures']) == len(paved['structures']) - 2)
                origin = client.ui()['position']; client.context(client.free_cell())
                r = client.ui()['map']; client.point(r['x'] + 3, r['y'] + r['height'] - 3)
                check('contextDismissConsumesClick', client.ui()['context'] is None and client.ui()['routeCount'] == 0 and client.ui()['position'] == origin)
                companion = client.ui()['party'][-1]; old_cells = {(a['id'], a['x'], a['y']) for a in client.ui()['party']}
                client.context(companion); client.click('context:lead'); client.formation()
                check('mouseChangesLeaderWithoutCrossingMembers', client.ui()['party'][0]['id'] == companion['id'] and {(a['id'], a['x'], a['y']) for a in client.ui()['party']} == old_cells)
                ui = client.ui(); origin = ui['position']; destination = client.free_cell(distance=2); client.tile(destination['x'], destination['y']); client.wait(lambda u: u['routeCount'] == 0 and u['position'] != origin); client.pause()
                moved = client.save()['state']; check('mouseTravelStartsWithoutKeyboard', moved['position'] != equipped['position'])
                check('mouseTravelPreservesPartyFormation', client.formation()); client.capture('formation-following')
                # Isolate world-interaction rendering/input at a real site's already-generated content.
                fixture = json.loads(slot.read_text()); state = fixture['state']
                guardian = next(a for a in state['actors'] if a['archetype'].startswith('boss.') and a['position']['floor'] > 1)
                site_id = (guardian['id'] - 40000) // 8; site = fixture['plan']['sites'][site_id]
                cache = next(c for c in state['loot'] if c['id'] == f'site-{site_id}-0')
                state['position'] = dict(cache['position']); state['position']['x'] += 1
                for actor in state['actors']:
                    if actor['id'] in state['party']: actor['position'] = dict(state['position'])
                slot.write_text(json.dumps(fixture)); client.load()
                client.wait(lambda u: u['position'] == {k: state['position'][k] for k in ('x', 'y', 'floor')})
                check('olderStackedSaveSpreadsPartyOnLoad', client.formation())
                client.tile(cache['position']['x'], cache['position']['y'])
                looted = client.save()['state']; check('mouseCacheInteraction', next(c for c in looted['loot'] if c['id'] == cache['id'])['taken'])
                client.tile(site['center']['x'] + 2, site['center']['y']); client.wait(lambda u: u['position']['floor'] == 1); client.pause()
                check('mouseStairInteraction', True); client.capture('dungeon')
                check('dungeonLandingHasSeparatePartyTiles', client.formation())
                ui = client.ui(); client.tile(ui['position']['x'], ui['position']['y']); client.wait(lambda u: u['position']['floor'] == 0)
                check('surfaceLandingHasSeparatePartyTiles', client.formation())
                before = client.save()['state']; slot.write_text('{"format":"vagabonds-world","version":999}'); client.click('load'); restored = client.save()['state']
                check('mouseRejectedLoadPreservesState', before == restored)
            finally: client.close()
        if 'world-resources' in args.scenes:
            client = Client('world-resources', slot)
            try:
                ui = client.ui(); origin = ui['position']
                fruit = next(c for c in ui['cells'] if c['resource'] and c['resource']['id'] == 'resource.apple-tree' and abs(c['x'] - origin['x']) + abs(c['y'] - origin['y']) == 1)
                client.context(fruit); client.capture('resource-tree')
                check('resourceContextHasHarvest', any(c['id'] == 'context:harvest' and c['enabled'] for c in client.ui()['controls']))
                client.click('context:harvest'); client.wait(lambda u: not u['work']); client.pause()
                harvested = client.save(); check('mouseHarvestPersistsStockAndIngredients', harvested['state']['inventory'].get('apple', 0) == 2)
                client.load(); remaining = next(c for c in client.ui()['cells'] if c['x'] == fruit['x'] and c['y'] == fruit['y'])['resource']['available']
                check('resourceStockSurvivesMouseLoad', remaining == fruit['resource']['available'] - 1)
                client.click('panel:inventory'); client.click('panel:supplies'); client.capture('ingredients-pack')
                check('packIllustratesHarvestedIngredients', any(c['id'] == 'supply:item.apple' and c['sprite'] == 'apple' and 'x2' in c['label'] for c in client.ui()['controls']))
                client.click('close')
                terrain_before_depletion = client.ui()['terrainCache']['rebuilds']
                for _ in range(remaining):
                    client.context(fruit); client.click('context:harvest'); client.wait(lambda u: not u['work']); client.pause()
                client.context(fruit); client.capture('resource-depleted')
                harvest = next(c for c in client.ui()['controls'] if c['id'] == 'context:harvest')
                check('exhaustedResourceExplainsRecovery', not harvest['enabled'] and 'recovers' in harvest['reason'])
                check('depletionInvalidatesTerrainLayer', client.ui()['terrainCache']['rebuilds'] > terrain_before_depletion)
                client.click('context:clear')
                # Valid isolated deadline fixture; only the test clock is moved.
                # Keep the immutable stock snapshot and cross recovery through Play.
                depleted = client.save()
                reference = next(c for c in depleted['chunks'] if c['key'] == {'x': fruit['x'] // 64, 'y': fruit['y'] // 64, 'floor': 0})
                delta = json.loads((Path(str(slot) + '.chunks') / (reference['hash'] + '.json')).read_text())
                stock = delta['resources'][str((fruit['y'] % 64) * 64 + fruit['x'] % 64)]
                depleted['state']['tick'] = stock['recoverAt'] - 1
                slot.write_text(json.dumps(depleted)); client.load()
                terrain_before_recovery = client.ui()['terrainCache']['rebuilds']
                client.click('pause')
                client.wait(lambda u: any(c['x'] == fruit['x'] and c['y'] == fruit['y'] and c['resource'] and c['resource']['available'] > 0 for c in u['cells']))
                client.pause(); client.capture('resource-recovered')
                check('recoveryDeadlineInvalidatesTerrainLayer', client.ui()['terrainCache']['rebuilds'] > terrain_before_recovery)
            finally: client.close()
        if 'world-wildlife' in args.scenes:
            client = Client('world-wildlife', slot)
            try:
                ui = client.ui(); bee = next(a for a in ui['actors'] if a['archetype'] == 'character.bee')
                check('wildlifeStartsOutsideDefendedTerritory', not bee['hostile'] and 'honey tree' in bee['behaviour'])
                client.context(bee); client.capture('wildlife-habitat')
                hunt = next(c for c in client.ui()['controls'] if c['id'] == 'context:interact')
                check('contextNamesHuntingAndTerritory', hunt['label'].startswith('Hunt ') and 'tiles away' in hunt['tip'])
                client.click('context:clear')
                tree = next(c for c in client.ui()['cells'] if c['x'] == bee['habitat']['x'] and c['y'] == bee['habitat']['y'])
                px, py = client.tile_point(tree['x'], tree['y']); x('mousemove', '--window', client.window, round(px), round(py))
                client.wait(lambda u: u['hover'] and 'defender' in u['hover']['detail'])
                check('guardedResourceHoverExplainsDanger', 'provokes' in client.ui()['hover']['detail']); client.capture('wildlife-warning')
                client.context(bee); client.click('context:interact'); client.wait(lambda u: u['battle'] is not None)
                check('huntingStartsTurnBasedEncounter', client.ui()['frozen']); client.capture('wildlife-encounter')
                for _ in range(12):
                    ui = client.combat_ready()
                    if ui['battle'] is None: break
                    client.click('skill:0'); target = next(u for u in client.ui()['battle']['units'] if not u['friendly'] and u['alive']); r = target['bounds']
                    client.point(r['x'] + r['width'] / 2, r['y'] + r['height'] / 2)
                client.wait(lambda u: u['battle'] is None); client.combat_ready(); client.pause(); state = client.save()['state']
                remains = next(c for c in state['loot'] if c['sourceActor'] == bee['id']); reward = state['lastEncounterReward']
                check('animalLeavesIngredientsWithoutGenericGold', remains['contents'].get('honey') == 1 and reward['gold'] == 0 and reward['food'] == 0)
                client.capture('wildlife-rewards'); client.click('close')
                client.tile(remains['position']['x'], remains['position']['y']); client.wait(lambda u: 'Recovered' in u['notice']); client.pause()
                gathered = client.save()['state']; client.load(); reloaded = client.save()['state']
                check('wildlifeIngredientsAndDeathSurviveLoad', gathered['inventory'].get('honey') == reloaded['inventory'].get('honey') == 1 and len([a for a in reloaded['actors'] if a['id'] == bee['id']]) == 1 and next(c for c in reloaded['loot'] if c['sourceActor'] == bee['id'])['taken'])
            finally: client.close()
        if 'world-situation' in args.scenes:
            client = Client('world-situation', slot)
            try:
                initial = client.save(); ui = client.ui(); discovery = next(d for d in ui['discoveries'] if d['bypass'] is not None and abs(d['x'] - ui['position']['x']) + abs(d['y'] - ui['position']['y']) == 1)
                check('situationOffersWorkFightAndTunnel', all(any(c['id'] == identifier and c['enabled'] for c in ui['controls']) for identifier in ('approach:disarm', 'discovery-fight', 'show-bypass')))
                client.capture('situation-approaches'); client.click('close'); client.context(discovery)
                check('cursorMenuOffersDisarming', any(c['id'] == 'context:situation:disarm' and c['enabled'] for c in client.ui()['controls']))
                client.click('context:situation:disarm'); client.wait(lambda u: u['panel'] == 'work' and not u['work']); client.pause()
                resolved = client.save()['state']; check('mouseDelegationDisarmsTheTrap', next(d for d in resolved['discoveries'] if d['id'] == discovery['id'])['outcome'] == 'disarm')
                client.click('close'); client.tile(discovery['x'], discovery['y']); client.wait(lambda u: u['panel'] == 'discovery'); client.capture('situation-resolved')
                client.click('discovery-loot'); client.wait(lambda u: u['panel'] == 'loot'); choice = next(c for c in client.ui()['controls'] if c['id'].startswith('claim:'))
                client.click(choice['id']); claimed = client.save()['state']; client.load(); loaded = client.save()['state']
                check('situationRewardAndOutcomeSurviveLoad', next(c for c in loaded['loot'] if c['id'] == discovery['id'])['taken'] and next(d for d in loaded['discoveries'] if d['id'] == discovery['id'])['outcome'] == 'disarm' and claimed['inventory'] == loaded['inventory'])
                # Replay only the isolated fixture's original manifest to verify the independent excavation approach.
                slot.write_text(json.dumps(initial)); client.load(); client.tile(discovery['x'], discovery['y']); client.wait(lambda u: u['panel'] == 'discovery')
                client.click('show-bypass'); wall = discovery['bypass']; client.wait(lambda u: len(u['selection']) == 1); client.context(wall); client.capture('situation-tunnel')
                client.click('context:dig'); client.wait(lambda u: next(d for d in u['discoveries'] if d['id'] == discovery['id'])['outcome'] == 'tunnel'); client.pause()
                check('mouseExcavationOpensPhysicalBypass', next(c for c in client.ui()['cells'] if c['x'] == wall['x'] and c['y'] == wall['y'])['groundOpen'])
            finally: client.close()
        if 'world-camp' in args.scenes:
            client = Client('world-camp', slot)
            try:
                ui = client.ui(); camp = next(c for c in ui['cells'] if c['material'] == 'Camp'); party = ui['party']
                client.context(camp); client.click('context:camp'); client.wait(lambda u: u['panel'] == 'camp'); client.capture('camp-duties')
                check('campHasPortraitAssignmentsAndContextEntry', len([c for c in client.ui()['controls'] if c['id'].startswith('campworker:') and c['sprite']]) == 4)
                for index, role, duty in [(0, 'cook', 'camp.apple-broth'), (1, 'treat', 'camp.treatment'), (2, 'watch', 'camp.watch')]:
                    client.click('campworker:' + str(party[index]['id'])); client.click('camprole:' + role); client.click('campduty:' + duty); client.pause()
                state = client.save()['state']; orders = [w for w in state['work'] if w['definition'].startswith('camp.')]
                check('mouseAssignsThreeIndependentCampDuties', len(orders) == 3 and len({w['worker'] for w in orders}) == 3)
                client.click('pause'); tick = client.ui()['tick']; client.wait(lambda u: u['tick'] > tick + 2); client.pause()
                check('campPanelKeepsWorldRunning', client.ui()['panel'] == 'camp')
                client.click('close'); client.click('panel:actions'); client.click('wait:300')
                state = client.save()['state']; members = [a for a in state['actors'] if a['id'] in state['party']]
                check('nativeCampCookingTreatmentAndWatchComplete', not state['work'] and all(a['body']['wounds'] == 0 and {p['effect'] for p in a['preparations']} == {'burn-resist', 'watch'} for a in members))
                client.load(); state = client.save()['state']; check('campPreparationsSurviveMouseReload', all(len(a['preparations']) == 2 for a in state['actors'] if a['id'] in state['party']))
                client.context(camp); client.click('context:camp'); client.click('campworker:' + str(party[3]['id'])); client.click('camprole:stories'); client.click('campduty:camp.stories'); client.pause()
                client.click('close'); client.click('panel:actions'); client.click('wait:300'); state = client.save()['state']
                check('campStoryMarksAnActualSite', state['lastCampStory'] is not None and any(site['name'] in state['lastCampStory']['text'] and site['id'] in state['knownSites'] for site in client.save()['plan']['sites']))
                client.context(camp); client.click('context:camp'); client.click('campstory'); client.wait(lambda u: u['panel'] == 'camp-story' and u['dialogue'] is not None); client.capture('camp-conversation')
                check('campStoriesUseVoicedPortraitPresentation', bool(client.ui()['dialogue']['fullText']) and len(client.ui()['dialogue']['portraits']) == 2)
                client.finish_dialogue(); check('campConversationReturnsToDuties', client.ui()['panel'] == 'camp'); client.click('close')
            finally: client.close()
        if 'world-discovery' in args.scenes:
            client = Client('world-discovery', slot)
            try:
                client.wait(lambda u: u['panel'] == 'discovery'); client.click('discovery-loot'); ui = client.wait(lambda u: u['panel'] == 'loot')
                rewards = [c for c in ui['controls'] if c['id'].startswith('claim:')]
                check('generatedRewardsShowSpritesAndEffects', len(rewards) == 3 and all(c['sprite'] and c['detail'] and c['tip'] for c in rewards))
                client.capture('equipment-choices'); selected = rewards[0]['id'][6:]; client.click(rewards[0]['id']); client.wait(lambda u: u['panel'] == 'inventory')
                state = client.save()['state']; check('chosenEquipmentHasOnePersistentIdentity', selected in state['generatedEquipment'] and state['inventory'].get(selected) == 1)
                client.click('equip'); state = client.save()['state']; hero = next(a for a in state['actors'] if a['id'] == state['party'][0])
                check('mouseEquipsGeneratedEquipment', any(e['item'] == selected for e in hero['equipment'].values()) and state['inventory'].get(selected) == 0)
                client.capture('equipment-worn'); client.load(); state = client.save()['state']
                check('generatedEquipmentSurvivesReload', selected in state['generatedEquipment'] and any(e['item'] == selected for a in state['actors'] if a['id'] == state['party'][0] for e in a['equipment'].values()))
            finally: client.close()
        if 'world-battle' in args.scenes:
            client = Client('world-battle', slot)
            try:
                client.combat_ready()
                check('fourMembersHaveSeparateTilesInBattle', len(client.ui()['party']) == 4 and client.formation())
                ui = client.ui(); fighters = ui['battle']['units']
                check('fourByFourStageAtMinimumSize', len(fighters) == 8
                    and all(0 <= u['bounds']['x'] < u['bounds']['x'] + u['bounds']['width'] <= 1024
                        and 0 <= u['bounds']['y'] < u['bounds']['y'] + u['bounds']['height'] <= 720 for u in fighters))
                check('abilityTrayHasSprites', all(c['sprite'] for c in ui['controls'] if c['id'].startswith('skill:')))
                client.capture('battle-stage-four-by-four')
                before = client.save()['state']; client.click('panel:tactics'); client.capture('tactics-four-foes')
                check('fourFoeTacticsCostsNoTimeOrRandomness', client.save()['state'] == before)
                client.click('close'); client.load()
                check('authoredFourFoeFixtureSurvivesExactReload', client.save()['state'] == before)
                flee = next(c for c in client.ui()['controls'] if c['id'].startswith('skill:') and 'retreat' in c['label'].lower())
                client.click(flee['id']); client.wait(lambda u: u['battle'] is None); client.combat_ready(); client.pause()
                check('retreatPreservesFourSeparateTiles', client.formation()); client.capture('formation-four')
            finally: client.close()
        if 'world-cooperation' in args.scenes:
            client = Client('world-cooperation', slot)
            try:
                client.combat_ready()
                client.wait(lambda u: u['battle'] is not None and u['battle']['active'] == 0)
                check('eightCooperativeAbilityButtonsFit', len([c for c in client.ui()['controls'] if c['id'].startswith('skill:')]) == 8 and all(c['bounds']['y'] + c['bounds']['height'] <= 720 for c in client.ui()['controls']))
                initial = client.save(); before = initial['state']
                def fighter(unit_id):
                    client.fighter(unit_id)
                client.click('skill:6'); fighter(3)
                chosen = client.save()['state']
                check('helperSelectionIsFree', client.ui()['battle']['partner'] == 3 and chosen['battle'] == before['battle'] and chosen['combatRandom'] == before['combatRandom'] and chosen['tick'] == before['tick'])
                client.capture('cooperation-helper')
                client.point(500, 110, button=3)
                check('rightClickCancelsCooperationForFree', client.ui()['battle']['partner'] == -1 and client.save()['state']['battle'] == before['battle'])
                client.click('skill:6'); fighter(3); client.fighter(4, fast=True)
                check('skillsLockedDuringPlayback', client.ui()['combatPresentation']['busy']
                    and not any(c['enabled'] for c in client.ui()['controls'] if c['id'].startswith('skill:')))
                client.capture_phase('impact', 'cooperation-impact')
                client.combat_ready()
                acted = client.save()['state']; b = acted['battle']; old = before['battle']
                check('jointStrikeSpendsBothManaAndActions', b['assisting'] == [3] and b['order'][b['turnIndex']] == 1 and b['units'][0]['mana'] == old['units'][0]['mana'] - 3 and b['units'][3]['mana'] == old['units'][3]['mana'] - 3 and b['units'][4]['health'] < old['units'][4]['health'])
                client.capture('cooperation-strike'); client.load()
                check('jointActionSaveRestoresExactly', client.save()['state'] == acted)
                client.click('skill:3'); client.click('skill:3')
                client.wait(lambda u: u['battle']['round'] == 2 and u['battle']['active'] == 0); client.combat_ready()
                completed = client.save()['state']
                check('helperSkippedOnceAndRoundClockAdvances', completed['tick'] == before['tick'] + 5 and completed['battle']['assisting'] == [] and any(e['label'] == 'Assisted' for e in completed['combatEvents']))
                # Restore the wounded, isolated starting fixture to inspect group recovery.
                client.restore(initial); client.click('skill:7')
                check('sharedAidWaitsForHelper', client.save()['state']['battle'] == before['battle'])
                fighter(3); client.combat_ready(); healed = client.save()['state']['battle']
                check('sharedAidHealsPartyAndSpendsBothMana', all(healed['units'][i]['health'] > old['units'][i]['health'] for i in range(4)) and healed['units'][0]['mana'] == old['units'][0]['mana'] - 4 and healed['units'][3]['mana'] == old['units'][3]['mana'] - 3)
                client.capture('cooperation-aid')

                # The same real action supplies semantic, repeatable captures of
                # each visual beat. Focus loss freezes that beat for screenshot
                # tools; no special animation stepping or game rule is used.
                for phase in ('anticipation', 'travel', 'recovery'):
                    client.restore(initial); client.click('skill:6'); fighter(3); client.fighter(4, fast=True)
                    phase_ui = client.capture_phase(phase, 'cooperation-' + phase)
                    check(phase + 'KeepsWorldFrozen', phase_ui['frozen'] and phase_ui['tick'] == before['tick'])
                    client.combat_ready()
                    check(phase + 'HasSameCombatResult', client.save()['state'] == acted)

                client.restore(initial); client.click('combat-speed')
                check('mouseChangesCombatSpeed', client.ui()['combatPresentation']['speed'] > 1)
                client.click('skill:6'); fighter(3); client.fighter(4, fast=True); client.combat_ready()
                check('fastPlaybackHasSameCombatResult', client.save()['state'] == acted)
                client.click('combat-speed')
                client.restore(initial); client.click('skill:6'); fighter(3); client.fighter(4, fast=True)
                client.click('combat-skip', fast=True); client.combat_ready()
                check('skipPlaybackHasSameCombatResult', client.save()['state'] == acted)

                client.restore(initial); client.click('skill:6'); fighter(3); client.fighter(4, fast=True)
                check('saveStartsDuringPlayback', client.ui()['combatPresentation']['busy'])
                during = client.save()['state']; client.load(); client.combat_ready()
                check('loadingPlaybackNeverReappliesAction', client.save()['state'] == during == acted
                    and not client.ui()['combatPresentation']['busy'])

                initial_motion = client.ui()['combatPresentation']['reducedMotion']; client.click('combat-motion')
                check('mouseTogglesCombatMotion', client.ui()['combatPresentation']['reducedMotion'] != initial_motion)
                initial_volume = client.ui()['combatPresentation']['volume']; client.click('combat-volume')
                check('mouseChangesCombatVolume', client.ui()['combatPresentation']['volume'] != initial_volume)
                client.restore(initial); client.click('skill:6'); fighter(3); client.fighter(4, fast=True)
                client.capture_phase('impact', 'combat-reduced-motion'); client.combat_ready()
                check('accessibilityControlsPreserveCombatResult', client.save()['state'] == acted)
                for _ in range(4):
                    if client.ui()['combatPresentation']['volume'] == 0: break
                    client.click('combat-volume')
                check('mouseMutesCombatSound', client.ui()['combatPresentation']['volume'] == 0)
                client.restore(initial); cues = client.ui()['combatPresentation']['playedCues']
                client.click('skill:6'); fighter(3); client.fighter(4, fast=True); client.combat_ready()
                check('mutedPlaybackDoesNotPlayCues', client.ui()['combatPresentation']['playedCues'] == cues)

                # Change only the isolated actor's class/sprite identity. Keep
                # the fixture's health/mana bases and the durable enemy so the
                # real class ability paths provide reusable family previews.
                client.click('combat-motion')  # Review normal motion as well as the reduced-motion capture above.
                for name, role, sprite, skill, target, phase, family in (
                    ('projectile', 1, 'ranger', 0, 4, 'travel', 'projectile'),
                    ('mark', 1, 'ranger', 1, 4, 'impact', 'dread'),
                    ('spell', 2, 'mage', 1, 4, 'impact', 'spell'),
                    ('group-spell', 2, 'mage', 2, None, 'impact', 'spell'),
                    ('heal', 3, 'healer', 1, 3, 'impact', 'heal'),
                    ('guard', 0, 'warden', 3, None, 'impact', 'guard'),
                ):
                    fixture = json.loads(json.dumps(initial)); state = fixture['state']; battle = state['battle']
                    actor = next(a for a in state['actors'] if a['id'] == battle['actorIds'][0])
                    actor['role'] = role; actor['spriteId'] = sprite; battle['units'][0]['sprite'] = sprite
                    client.restore(fixture); client.click('skill:' + str(skill), fast=target is None)
                    if target is not None: client.fighter(target, fast=True)
                    visual = client.capture_phase(phase, 'combat-' + name)
                    check(name + 'UsesExpectedPresentationFamily', visual['combatPresentation']['family'] == family)
                    client.combat_ready()

                # A wounded enemy fixture isolates the finishing-hit transition.
                # The ordinary resolver still owns damage, death and rewards.
                finish = json.loads(json.dumps(initial)); state = finish['state']; battle = state['battle']
                battle['units'][4]['health'] = 1
                next(a for a in state['actors'] if a['id'] == battle['actorIds'][4])['health'] = 1
                client.restore(finish); client.click('skill:6'); fighter(3); client.fighter(4, fast=True)
                finishing = client.capture_phase('impact', 'combat-finishing-hit')
                check('finishingHitRetainsStageBeforeSpoils', finishing['battle'] is None
                    and finishing['combatPresentation']['stageVisible'] and finishing['combatPresentation']['endingPending']
                    and finishing['panel'] != 'spoils')
                client.combat_ready(); client.wait(lambda u: u['panel'] == 'spoils'); client.capture('combat-spoils')
                rewards = client.save()['state']; client.load(); client.combat_ready()
                check('finishedEncounterSaveDoesNotDuplicateRewards', client.save()['state'] == rewards)

                # One survivor lets a normal scheduled enemy hit demonstrate
                # defeat playback and the saved, irreversible continuation UI.
                wipe = json.loads(json.dumps(initial)); state = wipe['state']; battle = state['battle']
                for unit in battle['units']:
                    if unit['heroId'] < 0: continue
                    unit['health'] = 1 if unit['id'] == 0 else 0
                    actor = next(a for a in state['actors'] if a['id'] == battle['actorIds'][unit['id']])
                    actor['health'] = unit['health']; actor['life'] = 0 if unit['health'] else 1
                battle['order'] = [0, 4, 1, 2, 3]; battle['turnIndex'] = 0
                for plan in battle['plans'].values(): plan['target'] = 0; plan['ability'] = 'ability.strike'
                client.restore(wipe); client.click('skill:3', fast=True)
                client.wait(lambda u: u['battle'] is None and u['combatPresentation']['busy'], interval=.01)
                defeated = client.capture_phase('impact', 'combat-party-wipe')
                check('partyWipeRetainsStageBeforeEnding', defeated['combatPresentation']['stageVisible']
                    and defeated['combatPresentation']['endingPending']
                    and not any(c['id'] == 'new-world' for c in defeated['controls']))
                client.combat_ready(); client.wait(lambda u: any(c['id'] == 'new-world' for c in u['controls']))
                client.capture('combat-defeat')
            finally: client.close()
        if 'world-boss' in args.scenes:
            client = Client('world-boss', slot)
            try:
                client.combat_ready()
                guardian_fixture = client.save(); before = guardian_fixture['state']; client.click('panel:tactics'); client.capture('tactics-guardian')
                check('guardianTacticsCostsNoTimeOrRandomness', client.save()['state'] == before); client.click('close')
                before = client.save()['state']; client.click('panel:inventory'); client.click('gear:0'); client.capture('party-four'); modal = client.save()['state']
                check('mouseCombatPanelConsumesNoTurn', before['battle'] == modal['battle'] and before['combatRandom'] == modal['combatRandom'] and before['tick'] == modal['tick'])
                client.click('close'); client.click('skill:0'); selected = client.save()['state']
                check('mouseAbilityWaitsForTarget', selected['battle'] == modal['battle'] and selected['combatRandom'] == modal['combatRandom'])
                enemy = next(u for u in client.ui()['battle']['units'] if not u['friendly'] and u['alive']); r = enemy['bounds']; client.point(r['x'] + r['width'] / 2, r['y'] + r['height'] / 2)
                after = client.save()['state']; check('mouseCombatTarget', after['battle'] != selected['battle'] and after['combatRandom'] != selected['combatRandom']); client.combat_ready(); client.capture('combat')
                fixture = json.loads(slot.read_text()); state = fixture['state']; battle = state['battle']; final_id = state['endgame']['actorId']
                index = battle['actorIds'].index(final_id); battle['units'][index]['health'] = 1; next(a for a in state['actors'] if a['id'] == final_id)['health'] = 1
                battle['turnIndex'] = next(i for i, u in enumerate(battle['order']) if battle['units'][u]['heroId'] >= 0 and battle['units'][u]['health'] > 0)
                slot.write_text(json.dumps(fixture)); client.load(); client.click('skill:0')
                client.fighter(index, fast=True)
                ending_ui = client.capture_phase('impact', 'combat-final-boss-impact')
                check('finalBossEndingWaitsForImpact', ending_ui['battle'] is None and ending_ui['combatPresentation']['stageVisible']
                    and not any(c['id'] == 'new-world' for c in ending_ui['controls']))
                client.combat_ready()
                ending_manifest = client.save(); ending = ending_manifest['state']; check('mouseFinalBossCompletion', ending['endgame']['victory'] and ending['battle'] is None); client.capture('victory')
                # Replay the same authored guardian as an optional encounter so
                # its actual signature/equipment/supply spoils can be reviewed.
                optional = json.loads(json.dumps(guardian_fixture)); state = optional['state']; battle = state['battle']
                other_guardian = next(a for a in state['actors'] if a['archetype'].startswith('boss.') and a['id'] != state['endgame']['actorId'])
                state['endgame']['actorId'] = other_guardian['id']; state['endgame']['siteId'] = (other_guardian['id'] - 40000) // 8
                for unit in battle['units']:
                    if unit['heroId'] >= 0: continue
                    unit['health'] = 1; next(a for a in state['actors'] if a['id'] == battle['actorIds'][unit['id']])['health'] = 1
                client.restore(optional)
                for _ in range(8):
                    if client.ui()['battle'] is None: break
                    enemy = next(u for u in client.ui()['battle']['units'] if not u['friendly'] and u['alive'])
                    client.click('skill:0'); client.fighter(enemy['id']); client.combat_ready()
                client.wait(lambda u: u['panel'] == 'spoils'); optional_won = client.save()
                check('optionalGuardianAwardsSignatureGearAndSupplies', not optional_won['state']['endgame']['victory']
                    and len(optional_won['state']['lastEncounterReward']['items']) == 3)
                client.capture('guardian-three-item-spoils'); client.load()
                check('optionalGuardianSpoilsSurviveReload', client.save()['state'] == optional_won['state'])
                client.restore(ending_manifest)
                client.click('new-world'); fresh = client.save(); check('mouseFreshRun', fresh['plan']['seed'] != 42 and len(fresh['state']['party']) == 1 and not fresh['state']['endgame']['victory'])
            finally: client.close()
        if 'world-progression' in args.scenes:
            client = Client('world-progression', slot)
            try:
                client.wait(lambda u: u['panel'] == 'inventory'); initial = client.save()
                check('progressionFixtureHasFourClasses', {a['role'] for a in initial['state']['actors'] if a['id'] in initial['state']['party']} == {0, 1, 2, 3})
                client.click('panel:progression'); client.capture('progression-warden')
                class_choices = []
                for index in range(4):
                    client.click('party:' + str(index)); ui = client.ui(); choices = ui['progression']['choices']
                    class_choices.append({c['id'] for c in choices})
                    check('progressionClass' + str(index) + 'UsesSprites', len(choices) == 4 and all(c['sprite'] for c in ui['controls'] if c['id'].startswith('talent:')))
                check('eachClassHasDistinctDevelopments', all(not a & b for i, a in enumerate(class_choices) for b in class_choices[i+1:]))
                check('inspectingDevelopmentsCostsNothing', client.save()['state'] == initial['state'])

                client.click('party:0'); client.click('talent:talent.warden-breaker')
                check('talentSelectionNeedsExplicitCommit', client.save()['state'] == initial['state'])
                client.click('learn-talent'); chosen = client.save(); leader = chosen['state']['party'][0]
                actor = next(a for a in chosen['state']['actors'] if a['id'] == leader)
                check('mouseCommitsPermanentDevelopment', actor['talents'] == ['talent.warden-breaker']
                    and chosen['state']['tick'] == initial['state']['tick'] and chosen['state']['combatRandom'] == initial['state']['combatRandom'])
                client.click('talent:talent.warden-bulwark'); learn = next(c for c in client.ui()['controls'] if c['id'] == 'learn-talent')
                check('sameMilestoneAlternativeLocked', not learn['enabled'] and 'already committed' in learn['reason'])
                r = learn['bounds']; client.point(r['x'] + r['width']/2, r['y'] + r['height']/2)
                check('lockedTalentClickCannotSpendOrReplace', client.save()['state'] == chosen['state'])
                client.click('talent:talent.warden-sentinel'); client.click('learn-talent')
                check('secondMilestoneCanBeChosenIndependently', len(client.ui()['progression']['talents']) == 2)
                client.capture('progression-committed')
                client.click('party:2'); client.click('talent:talent.arcanist-reservoir'); client.click('learn-talent')
                client.capture('progression-arcanist'); developed = client.save(); client.load()
                check('learnedTalentsSurviveExactReload', client.save()['state'] == developed['state'])

                low = json.loads(json.dumps(initial)); actor = next(a for a in low['state']['actors'] if a['id'] == leader)
                actor['level'] = 1; actor['health'] = actor['baseHealth']; actor['mana'] = actor['baseMana']
                client.restore(low); client.click('panel:inventory'); client.click('party:0'); client.click('panel:progression')
                client.click('talent:talent.warden-breaker'); learn = next(c for c in client.ui()['controls'] if c['id'] == 'learn-talent')
                client.capture('progression-level-locked')
                check('futureMilestoneExplainsLevelRequirement', not learn['enabled'] and 'level 2' in learn['reason'])

                client.restore(developed)
                enemy = next(a for a in client.ui()['actors'] if a['kind'] == 'Beast' and a['archetype'] == 'character.prowler')
                client.context(enemy); client.click('context:interact'); client.wait(lambda u: u['battle'] is not None); client.combat_ready()
                check('developedPartyEntersNormalCombat', client.ui()['battle']['active'] == 0)
                client.click('skill:3'); client.combat_ready(); client.click('skill:3'); client.combat_ready()
                check('developedArcanistReceivesTurn', client.ui()['battle']['active'] == 2)
                before = client.save()['state']; client.click('skill:1'); target = next(u for u in client.ui()['battle']['units'] if not u['friendly'])
                client.fighter(target['id'], fast=True); client.capture_phase('impact', 'progression-actual-ember'); client.combat_ready()
                after = client.save()['state']
                check('learnedReservoirChangesRealManaCost', after['battle']['units'][2]['mana'] == before['battle']['units'][2]['mana'] - 2
                    and after['battle']['units'][target['id']]['health'] < before['battle']['units'][target['id']]['health'])
                client.click('panel:inventory'); client.click('panel:progression'); client.click('party:2')
                learn = next(c for c in client.ui()['controls'] if c['id'] == 'learn-talent')
                check('combatCannotCommitDevelopments', not learn['enabled'] and 'between encounters' in learn['reason'])
            finally: client.close()
        if 'world-encounters' in args.scenes:
            client = Client('world-encounters', slot)
            try:
                client.combat_ready(); initial = client.save(); state = initial['state']; battle = state['battle']
                charger = next(i for i, actor_id in enumerate(battle['actorIds']) if next(a for a in state['actors'] if a['id'] == actor_id)['archetype'] == 'character.cinder-channeler')
                charger_actor = battle['actorIds'][charger]
                intent = next(i for i in client.ui()['combatIntents'] if i['unitId'] == charger)
                check('preparedAttackIntentIsHonest', intent['stage'] == 'prepare' and intent['mana'] == 0
                    and all(t['minimum'] == t['maximum'] == 0 for t in intent['targets']))
                client.capture('encounter-preparation-warning')
                client.click('skill:3'); client.combat_ready(); prepared = client.save(); ready = prepared['state']['battle']
                check('enemyPreparationCostsTurnWithoutDamageOrMana', str(charger) in ready['prepared']
                    and ready['units'][charger]['mana'] == battle['units'][charger]['mana']
                    and [u['health'] for u in ready['units'][:4]] == [u['health'] for u in battle['units'][:4]])
                client.capture('encounter-prepared'); client.load()
                check('preparationSurvivesExactReload', client.save()['state'] == prepared['state'])
                client.click('skill:1'); client.fighter(charger, fast=True); client.capture_phase('impact', 'encounter-interruption'); client.combat_ready()
                broken = client.save()
                check('shieldBashBreaksPreparedAttack', str(charger) not in broken['state']['battle']['prepared']
                    and broken['state']['battle']['plans'][str(charger)]['interrupted']
                    and any(e['label'] == 'Preparation broken' for e in broken['state']['combatEvents']))
                client.load(); check('interruptedPreparationSurvivesReload', client.save()['state'] == broken['state'])
                for _ in range(3): client.click('skill:3'); client.combat_ready()
                interrupted = client.save()['state']
                check('interruptedReleaseNeverDamagesOrChargesMana', interrupted['battle']['round'] >= 2
                    and interrupted['battle']['units'][charger]['mana'] == battle['units'][charger]['mana']
                    and not any(e['kind'] == 'ability' and e['abilityId'] == 'ability.cinder-surge' for e in interrupted['combatEvents']))

                client.restore(prepared)
                for _ in range(3): client.click('skill:3'); client.combat_ready()
                intent = next(i for i in client.ui()['combatIntents'] if i['unitId'] == charger)
                check('releaseIntentNamesTargetsAndRealMana', intent['stage'] == 'release' and intent['mana'] == 4
                    and len(intent['targets']) == 4 and all(t['maximum'] >= t['minimum'] > 0 for t in intent['targets']))
                client.capture('encounter-release-warning')
                client.click('skill:3', fast=True); client.capture_phase('impact', 'encounter-release-impact', source=charger_actor); client.combat_ready()
                released = client.save()['state']
                hits = [e for e in released['combatEvents'] if e['kind'] == 'damage' and e['label'] == 'Cinder surge' and e['source'] == charger_actor]
                check('uninterruptedReleaseHitsPartyAndPaysOnce', len({e['target'] for e in hits}) == 4
                    and released['battle']['units'][charger]['mana'] == battle['units'][charger]['mana'] - 4)

                finish = json.loads(json.dumps(initial)); state = finish['state']; battle = state['battle']
                for unit in battle['units']:
                    if unit['heroId'] >= 0: continue
                    unit['health'] = 1
                    next(a for a in state['actors'] if a['id'] == battle['actorIds'][unit['id']])['health'] = 1
                client.restore(finish); client.click('skill:2', fast=True); client.combat_ready(); client.wait(lambda u: u['panel'] == 'spoils')
                won = client.save(); reward = won['state']['lastEncounterReward']
                check('composedEncounterAwardsThemedEquipment', reward['theme'] == 'Cinder armaments' and reward['experience'] == 15
                    and reward['sources'] == [charger_actor] and len(reward['items']) >= 1
                    and all(won['state']['inventory'].get(item['item'], 0) >= item['count'] for item in reward['items']))
                client.capture('encounter-themed-rewards'); client.load()
                check('themedRewardsSurviveReloadWithoutDuplication', client.save()['state'] == won['state'])
                client.click('panel:inventory'); awarded = reward['items'][0]['item']
                client.equipment(awarded); client.click('equip'); equipped = client.save()['state']
                member = next(a for a in equipped['actors'] if a['id'] == equipped['party'][0])
                check('themedRewardCanBeEquipped', any(g['item'] == awarded for g in member['equipment'].values()))
                client.capture('encounter-reward-equipped')
            finally: client.close()
    report = {'nativeMouseInput': 'passed', 'scenes': args.scenes, 'checks': checks, 'scope': 'Local X11/WSLg. Mouse gameplay only; keyboard events solely verify they are ignored. Temporary saves and explicit isolated preview fixtures, including controlled levels and durable or wounded enemies; not a balance playthrough.'}
    (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n'); print(json.dumps(report))

if __name__ == '__main__': main()
