#!/usr/bin/env python3
"""Check illustrated class cards and record natural combat/chest feedback, muted."""
import argparse
import copy
import json
import os
from pathlib import Path
import shutil
import subprocess
import time
from verify_native_journey import Client, Journey


class FeedbackClient(Client):
    collecting = False
    marks = None
    recording_start = None
    chest_seen = False
    defer_reload = False
    check_chest_preferences = False

    def key(self, key, seconds=.16):
        return super().key(key, seconds)

    def resume(self):
        if self.defer_reload: self.control_click('dungeon:save')
        else: super().resume()

    def finish(self, spec, physical_key=None):
        super().finish(spec, physical_key)
        if self.marks is None or self.collecting: return self.ui
        feedback = self.ui['nativeDungeon']['feedback']
        samples = {}
        for effect in feedback['effects']:
            if effect['kind'] == 'AttackPerformed' and physical_key and physical_key.startswith('f+'):
                samples.setdefault('attack', effect['age'])
            if effect['kind'] == 'DamageTaken':
                samples.setdefault('player-hit' if effect['playerHit'] else 'enemy-hit', effect['age'])
            if effect['kind'] == 'ChestClaimed': samples.setdefault('chest-claimed', effect['age'])
        if self.ui['panel'] == 'dungeon-chest':
            self.chest_seen = True; samples['chest-opening'] = feedback['chestOpeningAge']
        for name, age in samples.items():
            if name in self.marks: continue
            self.marks[name] = max(0, time.monotonic() - self.recording_start - age + .07)
            # Preserve the already published frame, before a second capture
            # request can age the brief flash/recoil out of its visible window.
            shutil.copy2(self.control / 'frame.png', self.output / ('feedback-' + name + '.png'))
            shutil.copy2(self.control / 'observation.json', self.output / ('feedback-' + name + '.json'))
            print('feedback-' + name, self.ui['nativeDungeon']['clock'], flush=True)
            if name in ('chest-opening', 'chest-claimed'):
                self.collecting = True
                try:
                    clock = self.ui['nativeDungeon']['clock']
                    self.request('wait', seconds=.4)
                    assert self.ui['nativeDungeon']['clock'] == clock
                    assert self.ui['nativeDungeon']['feedback']['celebration']['coinBudget'] > 0
                    self.capture('chest-coin-spray' if name == 'chest-opening' else 'boon-coin-spray')
                    if name == 'chest-opening' and self.check_chest_preferences:
                        player = copy.deepcopy(self.ui['nativeDungeon']['player'])
                        for mode, settings, budget in [('reduced-motion', ['motion'], 8),
                            ('effects-off', ['combat-effects'], 0), ('restored', ['motion', 'combat-effects'], 48)]:
                            self.control_click('dungeon:menu'); self.control_click('dungeon:settings')
                            for setting in settings: self.control_click('dungeon:setting:' + setting)
                            self.control_click('dungeon:close'); self.key('Return')
                            assert self.ui['panel'] == 'dungeon-chest'
                            assert self.ui['nativeDungeon']['feedback']['celebration']['coinBudget'] == budget
                            self.capture('chest-coins-' + mode)
                            assert self.ui['nativeDungeon']['clock'] == clock and self.ui['nativeDungeon']['player'] == player
                        self.check_chest_preferences = False
                finally: self.collecting = False
        return self.ui


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('output', type=Path)
parser.add_argument('--runtime', type=Path)
parser.add_argument('--classes-only', action='store_true')
parser.add_argument('--feedback-set', choices=['all', 'combat', 'chest'], default='all')
parser.add_argument('--continue-from', type=Path)
parser.add_argument('--window-size', default='1440x900')
parser.add_argument('--chest-preferences', action='store_true')
args = parser.parse_args()
client = FeedbackClient(args.output.resolve(), 37, args.runtime.resolve() if args.runtime else None,
    continuation=args.continue_from.resolve() if args.continue_from else None, window_size=args.window_size)
client.check_chest_preferences = args.chest_preferences
video = None
try:
    assert not any(c['id'].startswith('roag:') for c in client.ui['controls'])
    assert client.ui['map']['y'] + client.ui['map']['height'] == client.ui['height'] - 16
    before = copy.deepcopy(client.ui['nativeDungeon']['player'])
    cards = [c for c in client.ui['controls'] if c['id'].startswith('dungeon:class:')]
    if not args.continue_from:
        assert len(cards) == 15 and all(c['sprite'] and c['detail'] for c in cards)
        client.capture('01-illustrated-classes')
    else: client.capture('01-exact-continuing-run')
    if args.classes_only:
        client.control_click('dungeon:close'); client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
        client.control_click('dungeon:setting:text-size'); client.control_click('dungeon:close')
        client.control_click('dungeon:menu'); client.control_click('dungeon:classes')
        assert client.ui['nativeDungeon']['clock'] == 0 and client.ui['nativeDungeon']['player'] == before
        assert client.ui['nativeDungeon']['presentation']['textSize'] == 'Large'
        client.capture('02-large-illustrated-classes')
        (client.output / 'result.json').write_text(json.dumps(dict(passed=True, window=args.window_size,
            checks=['all 15 portraits/descriptions', 'normal and large fonts', 'free class/settings inspection']), indent=2) + '\n')
    else:
        client.recording_start = time.monotonic(); client.marks = {}
        video_log = (client.output / 'video.log').open('wb')
        video = subprocess.Popen(['ffmpeg', '-nostdin', '-y', '-f', 'x11grab', '-framerate', '20',
            '-window_id', client.window, '-video_size', args.window_size, '-i', os.environ['DISPLAY'],
            '-vf', 'scale=960:-2', '-c:v', 'libx264', '-preset', 'ultrafast', '-crf', '20', '-pix_fmt', 'yuv420p',
            '-an', str(client.output / 'gameplay.mp4')], stdout=video_log, stderr=subprocess.STDOUT)
        client.request('wait', seconds=.5)
        if video.poll() is not None: raise RuntimeError('Video capture failed; inspect video.log.')
        if not args.continue_from: client.control_click('dungeon:class:1')
        client.request('point', x=20, y=20)
        client.defer_reload = True
        journey = Journey(client); journey.last_floor = client.ui['position']['floor']
        needed = {'attack', 'enemy-hit'}
        if args.feedback_set != 'chest': needed.add('player-hit')
        if args.feedback_set != 'combat': needed.update(['chest-opening', 'chest-claimed'])
        for journey.steps in range(1, 901):
            journey.observe()
            assert journey.player['health'] > 0, 'Natural probe defeated before collecting feedback.'
            # Deliberately accept a visible locked attack once to inspect its
            # feedback. This is an ordinary paid Wait, never injected damage.
            if 'player-hit' in needed and 'player-hit' not in client.marks and journey.at in journey.danger:
                client.key('space')
            else:
                assert journey.fight() or journey.quiet_tasks() or journey.explore(), 'No known ordinary route.'
            if needed <= client.marks.keys(): break
        assert needed <= client.marks.keys(), client.marks
        clock = client.ui['nativeDungeon']['clock']; player = copy.deepcopy(client.ui['nativeDungeon']['player'])
        client.request('wait', seconds=3)
        assert client.ui['nativeDungeon']['clock'] == clock and client.ui['nativeDungeon']['player'] == player
        assert not client.ui['nativeDungeon']['feedback']['effects'] and client.ui['nativeDungeon']['feedback']['reward'] is None
        assert client.ui['nativeDungeon']['feedback']['celebration'] is None
        assert not any(c['id'].startswith('roag:') for c in client.ui['controls'])
        client.capture('feedback-settled-without-an-action')
        client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
        for setting in ['motion', 'shake', 'flash', 'combat-effects']: client.control_click('dungeon:setting:' + setting)
        preferences = client.ui['nativeDungeon']['presentation']
        assert preferences['reducedMotion'] and not preferences['shake'] and not preferences['flash'] and not preferences['combatEffects']
        client.control_click('dungeon:close')
        video.terminate(); video.wait(timeout=10); client.defer_reload = False
        client.resume()
        assert client.ui['nativeDungeon']['clock'] == clock and client.ui['nativeDungeon']['player'] == player
        assert client.ui['nativeDungeon']['presentation']['reducedMotion']
        client.capture('reduced-motion-exact-continuation')
        (client.output / 'result.json').write_text(json.dumps(dict(passed=True, actions=journey.steps, window=args.window_size,
            captured=sorted(client.marks), required=sorted(needed), highlights=client.marks, checks=['ordinary gameplay inputs',
                'effects expire on presentation time without game actions', 'reduced motion/effect switches persist',
                'exact continuing build/clock']), indent=2) + '\n')
    client.request('stop'); client.process.wait(timeout=15)
    print('Rendered class/feedback checks passed.', flush=True)
finally:
    if video is not None:
        video.terminate()
        try: video.wait(timeout=10)
        except subprocess.TimeoutExpired: video.kill(); video.wait()
        video_log.close()
    client.close()
