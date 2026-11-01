#!/usr/bin/env python3
"""Read-only capture of coherent frames from a running native gameplay probe."""
import argparse
import json
from pathlib import Path
import shutil
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('journey', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
control = args.journey / 'control'
args.output.mkdir(parents=True, exist_ok=False)
seen = set()
deadline = time.monotonic() + 3600
while time.monotonic() < deadline:
    try:
        observation_path = control / 'observation.json'; frame = control / 'frame.png'
        first = json.loads(observation_path.read_text())
        ui = first['ui']; native = ui['nativeDungeon']
        marks = []
        for fighter in native['fighters']:
            if fighter['friendly'] or fighter['health'] <= 0: continue
            species = fighter['class'].split('/')[-1]
            if species not in ['hollow_regent', 'thorn_warden', 'mycelial_matriarch', 'canal_lockmaster',
                'index_keeper', 'prism_curator', 'ash_marshal', 'ember_crucible']: continue
            marks.append(species + '-engaged')
            if any(t['source'] == fighter['id'] for t in native['telegraphs']): marks.append(species + '-locked-attack')
            if fighter['health'] * 2 <= fighter['maxHealth']: marks.append(species + '-below-half-health')
        if native['victory']: marks.append('regent-defeated')
        for mark in marks:
            if mark in seen or not frame.exists(): continue
            # Publish writes the frame before its observation. Reject an in-flight
            # replacement or a new frame paired with the previous JSON revision.
            frame_stat = frame.stat()
            if frame_stat.st_mtime_ns > observation_path.stat().st_mtime_ns: continue
            data = frame.read_bytes(); second = json.loads(observation_path.read_text())
            if second['observation'] != first['observation'] or frame.stat().st_mtime_ns != frame_stat.st_mtime_ns: continue
            (args.output / (mark + '.png')).write_bytes(data)
            (args.output / (mark + '.json')).write_text(json.dumps(first, indent=2) + '\n')
            seen.add(mark); print(mark, native['clock'], flush=True)
            (args.output / 'result.json').write_text(json.dumps(dict(observed=sorted(seen), readOnly=True), indent=2) + '\n')
        if (args.journey / 'result.json').exists(): break
    except (FileNotFoundError, json.JSONDecodeError): pass
    time.sleep(.04)
