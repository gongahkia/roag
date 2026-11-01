#!/usr/bin/env python3
"""Verify the rendered native minimap, original keys and free inspection.

Run under xvfb-run with LIBGL_ALWAYS_SOFTWARE=1; the shared Client mutes audio.
This focused checkpoint never injects game state and is not full-run acceptance.
"""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('output', type=Path)
parser.add_argument('--runtime', type=Path)
args = parser.parse_args()
client = Client(args.output.resolve(), 42, args.runtime.resolve() if args.runtime else None)
try:
    client.control_click('dungeon:class:0')
    before = client.ui['nativeDungeon']['player']
    assert client.ui['nativeDungeon']['mapVisible']
    client.capture('01-explored-map')
    hidden = client.key('Tab')
    assert not hidden['nativeDungeon']['mapVisible']
    assert hidden['nativeDungeon']['clock'] == 0 and hidden['nativeDungeon']['player'] == before
    client.capture('02-map-hidden')
    shown = client.key('Tab')
    assert shown['nativeDungeon']['mapVisible'] and shown['nativeDungeon']['clock'] == 0
    boons = client.key('i')
    assert boons['panel'] == 'dungeon-boons' and boons['nativeDungeon']['clock'] == 0
    client.capture('03-original-boon-key')
    client.key('Escape')
    box = client.ui['map']
    covered = client.request('point', x=box['x'] + 20, y=box['y'] + 36)
    assert 'informational' in covered['notice']
    assert covered['nativeDungeon']['clock'] == 0 and covered['nativeDungeon']['player'] == before
    origin = covered['position']
    moved = client.key('s')
    assert moved['position'] == dict(x=origin['x'], y=origin['y'] + 1, floor=origin['floor'])
    assert moved['nativeDungeon']['clock'] == 100
    client.capture('04-paid-step-map')
    client.resume()
    assert client.ui['nativeDungeon']['mapVisible']
    client.capture('05-exact-map-run-continuation')
    client.request('stop'); client.process.wait(timeout=15)
    (client.output / 'result.json').write_text(json.dumps(dict(passed=True,
        checks=['default explored current-floor map', 'free physical Tab toggles', 'original I boon inspection',
            'map overlay consumes clicks without game actions', 'paid physical keyboard step', 'exact saved continuation']), indent=2) + '\n')
    print('Rendered minimap, original inspection keys and continuing save passed.', flush=True)
finally: client.close()
