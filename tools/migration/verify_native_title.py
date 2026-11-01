#!/usr/bin/env python3
"""Check the animated title, isolated save, settings and continuing run; muted."""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
from verify_native_journey import Client


class TitleClient(Client):
    def start(self, flags):
        super().start([*flags, '--title'])


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('output', type=Path)
parser.add_argument('--runtime', type=Path)
parser.add_argument('--continue-from', type=Path)
parser.add_argument('--window-size', default='1440x900')
parser.add_argument('--video', action='store_true', help='Record the first title sequence with FFmpeg.')
args = parser.parse_args()
client = TitleClient(args.output.resolve(), 37, args.runtime.resolve() if args.runtime else None,
    continuation=args.continue_from.resolve() if args.continue_from else None, window_size=args.window_size)
video = video_log = None
try:
    assert client.ui['panel'] == 'title' and client.ui['title']
    original = {key: copy.deepcopy(client.ui[key]) for key in ['position', 'nativeDungeon']}
    def snapshots():
        return {str(p.relative_to(client.control)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in client.control.rglob('campaign.json*') if p.is_file()} | {
            str(p.relative_to(client.control)): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in (client.control / 'campaign.json.chunks').rglob('*') if p.is_file()}
    saved = snapshots()
    client.capture('01-title')
    if args.video:
        video_log = (client.output / 'video.log').open('wb')
        video = subprocess.Popen(['ffmpeg', '-nostdin', '-y', '-f', 'x11grab', '-framerate', '20',
            '-window_id', client.window, '-video_size', args.window_size, '-i', os.environ['DISPLAY'],
            '-vf', 'scale=960:-2', '-c:v', 'libx264', '-preset', 'ultrafast', '-crf', '20', '-pix_fmt', 'yuv420p',
            '-an', str(client.output / 'title.mp4')], stdout=video_log, stderr=subprocess.STDOUT)
    camera = copy.deepcopy(client.ui['title']['camera']); moved = False; hit_frame = False
    actor_positions = None; previous_scene = None; enemies_moved = False
    for i in range(32):
        client.request('wait', seconds=.65)
        title = client.ui['title']; moved |= title['camera'] != camera
        positions = {a['id']: a['position'] for a in title['actors'] if a['sprite'].startswith('roag-enemy-')}
        if previous_scene == title['scenes'] and actor_positions:
            enemies_moved |= any(actor_positions[k] != p for k, p in positions.items() if k in actor_positions)
        actor_positions = positions; previous_scene = title['scenes']
        if not hit_frame and any(e['kind'] == 'DamageTaken' for e in title['effects']):
            client.capture('02-live-damage'); hit_frame = True
        assert client.ui['position'] == original['position']
        for field in ['clock', 'player', 'stage', 'victory']:
            assert client.ui['nativeDungeon'][field] == original['nativeDungeon'][field]
    assert moved and enemies_moved and hit_frame and client.ui['title']['hits'] > 0
    assert snapshots() == saved, 'Watching the title must not write the continuing save.'
    client.key('d'); client.key('F5')
    assert client.ui['nativeDungeon']['clock'] == original['nativeDungeon']['clock'] and snapshots() == saved
    client.control_click('title:settings')
    assert all(c['id'] == 'dungeon:close' or c['id'].startswith('dungeon:setting:') for c in client.ui['controls'])
    for setting in ['font', 'text-size', 'motion']: client.control_click('dungeon:setting:' + setting)
    assert client.ui['nativeDungeon']['presentation']['reducedMotion']
    client.key('Escape'); assert client.ui['panel'] == 'title'
    camera = client.ui['title']['camera']; client.request('wait', seconds=1)
    assert client.ui['title']['camera'] == camera
    client.capture('03-large-sans-reduced-motion-title')
    if video:
        assert video.poll() is None, 'Title recording failed; inspect video.log.'
        video.terminate(); video.wait(timeout=10)
    assert snapshots() == saved
    # Continue uses the already opened selected character, never the demo actor.
    client.control_click('title:continue')
    assert client.ui['title'] is None
    for field in ['clock', 'player', 'stage', 'victory']:
        assert client.ui['nativeDungeon'][field] == original['nativeDungeon'][field]
    if not args.continue_from:
        client.control_click('dungeon:class:7'); client.key('s')
        assert client.ui['nativeDungeon']['clock'] > 0
    client.capture('04-real-run')
    client.control_click('dungeon:menu'); client.control_click('dungeon:title')
    assert client.ui['panel'] == 'title'
    continuing = copy.deepcopy(client.ui['nativeDungeon']['player']); clock = client.ui['nativeDungeon']['clock']
    saved = snapshots()
    client.control_click('title:new'); assert client.ui['panel'] == 'title-new-confirm'
    client.capture('05-new-run-confirmation'); client.control_click('title:cancel')
    assert snapshots() == saved and client.ui['nativeDungeon']['player'] == continuing
    client.control_click('title:continue'); client.resume()
    assert client.ui['panel'] == 'title' and client.ui['nativeDungeon']['clock'] == clock
    assert client.ui['nativeDungeon']['player'] == continuing
    assert client.ui['nativeDungeon']['presentation']['reducedMotion']
    client.capture('06-title-exact-continuation')
    saved = snapshots()
    # Quit through the actual mouse control. Exit occurs before another frame,
    # so there is intentionally no subsequent observation to wait for.
    client.send('control', control='title:quit'); client.process.wait(timeout=20)
    assert client.process.returncode == 0 and snapshots() == saved
    client.log.close(); client.start(['--load'])
    client.control_click('title:new'); client.control_click('title:new-go')
    assert client.ui['title'] is None and client.ui['panel'] == 'dungeon-class'
    assert client.ui['nativeDungeon']['clock'] == 0
    fresh = client.ui['nativeDungeon']['player'][0]
    assert fresh['classId'] == 'Adventurer' and fresh['level'] == 1 and not fresh['boons']
    client.capture('07-confirmed-fresh-run')
    client.resume(); assert client.ui['panel'] == 'title' and client.ui['nativeDungeon']['clock'] == 0
    client.send('control', control='title:quit'); client.process.wait(timeout=20)
    assert client.process.returncode == 0
    (client.output / 'result.json').write_text(json.dumps(dict(passed=True, window=args.window_size,
        checks=['native enemy motion and damage', 'scrolling translucent title', 'save unchanged while watching',
            'settings and stationary reduced-motion camera', 'no gameplay keys/save from title',
            'continue exact selected character', 'return to title saves actual run', 'new-run cancel',
            'exact restart with preferences', 'quit leaves selected save unchanged',
            'confirmed fresh run starts at class selection and reloads']), indent=2) + '\n')
    print('Rendered title, autonomous combat, settings and continuing-save checks passed.', flush=True)
finally:
    if video:
        video.terminate()
        try: video.wait(timeout=10)
        except subprocess.TimeoutExpired: video.kill(); video.wait()
        video_log.close()
    client.close()
