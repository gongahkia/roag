"""Read and validate live room definitions for the deterministic room compiler."""

import hashlib
import json
import os
from pathlib import Path
import subprocess

if __package__:
    from .room_prototypes import build_proposals
else:
    from room_prototypes import build_proposals

ROOT = Path(__file__).resolve().parents[1]


def room_data():
    result = subprocess.run(
        [os.environ.get("LUAJIT", "luajit"), "tools/export_rooms.lua"],
        cwd=ROOT, text=True, capture_output=True, check=True,
    )
    rooms = []
    by_id = {}
    for line in result.stdout.splitlines():
        kind, *fields = line.split("\t")
        if kind == "ROOM":
            (room_id, name, biome_id, biome_name, strategy, family, roles,
             origin, source, mirror, rotations, biomes) = fields
            assert room_id not in by_id, room_id
            record = {
                "id": room_id, "name": name, "biome_id": biome_id,
                "biome_name": biome_name, "strategy": strategy,
                "family": family, "roles": roles.split(","),
                "origin": origin, "source": source,
                "mirror": mirror == "1",
                "rotations": [int(turn) * 90 for turn in rotations.split(",")],
                "biomes": biomes.split(","), "rows": [], "sockets": [],
                "markers": {}, "editable": [],
            }
            rooms.append(record)
            by_id[room_id] = record
        elif kind == "ROW":
            room_id, row = fields
            by_id[room_id]["rows"].append(row)
        elif kind == "SOCKET":
            room_id, x, y, facing, width = fields
            by_id[room_id]["sockets"].append({
                "x": int(x), "y": int(y), "facing": facing, "width": int(width),
            })
        elif kind == "MARKER":
            room_id, marker_kind, x, y = fields
            by_id[room_id]["markers"].setdefault(marker_kind, []).append(
                {"x": int(x), "y": int(y)}
            )
        elif kind == "EDITABLE":
            room_id, x, y = fields
            by_id[room_id]["editable"].append({"x": int(x), "y": int(y)})
        else:
            raise ValueError(f"Unexpected export record: {kind}")
    assert rooms and len(by_id) == len(rooms)
    for record in rooms:
        rows = record["rows"]
        assert rows and len({len(row) for row in rows}) == 1, record["id"]
        record["width"], record["height"] = len(rows[0]), len(rows)
        for row in rows:
            assert set(row) <= set(".#~bgiBmc "), record["id"]
        for point in (*record["sockets"], *record["editable"],
                      *(point for group in record["markers"].values() for point in group)):
            assert 1 <= point["x"] <= record["width"]
            assert 1 <= point["y"] <= record["height"]
    source = json.dumps(rooms, ensure_ascii=False, sort_keys=True,
                        separators=(",", ":")).encode()
    proposals = build_proposals(rooms)
    proposal_source = json.dumps(proposals, ensure_ascii=False, sort_keys=True,
                                 separators=(",", ":")).encode()
    return {
        "schema": "roag-room-review-data/1",
        "source_fingerprint": hashlib.sha256(source).hexdigest()[:16],
        "proposal_fingerprint": hashlib.sha256(proposal_source).hexdigest()[:16],
        "rooms": rooms,
        "proposals": proposals,
    }
