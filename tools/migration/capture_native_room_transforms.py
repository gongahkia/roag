#!/usr/bin/env python3
"""Capture all allowed pinned-original room transformations for native geometry checks."""
import json
from export_native_dungeon_content import lua, data, rooms, ROOT, REVISION, SOURCES
cases=[]
for id in sorted(list(rooms.definitions.keys())):
 room=rooms.definitions[id]
 for turn in data(room.rotations):
  for mirror in [False,True] if room.mirror else [False]:
   transformed=data(rooms.transform(room,turn,mirror))
   cases.append(dict(id=id,turns=turn,mirror=mirror,rows=transformed['rows'],sockets=transformed['sockets'],
       markers=transformed['markers'],editable=transformed['editable'],structures=transformed['structures']))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-room-transforms.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original room transformations.')
