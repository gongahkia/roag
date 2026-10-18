#!/usr/bin/env python3
"""Embed current room definitions in the offline room review page."""

import hashlib
import json
import os
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[1]
PAGE = ROOT / "docs/room-review/review.html"
BEGIN = "<!-- BEGIN GENERATED ROOM DATA -->"
END = "<!-- END GENERATED ROOM DATA -->"


def room_data():
    result = subprocess.run(
        [os.environ.get("LUAJIT", "luajit"), "tools/export_room_review.lua"],
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
    return {
        "schema": "roag-room-review-data/1",
        "source_fingerprint": hashlib.sha256(source).hexdigest()[:16],
        "rooms": rooms,
    }


def main():
    data = room_data()
    html = PAGE.read_text()
    assert html.count(BEGIN) == html.count(END) == 1
    before, _, rest = html.partition(BEGIN)
    _, _, after = rest.partition(END)
    encoded = json.dumps(data, ensure_ascii=False, separators=(",", ":"))
    encoded = encoded.replace("<", "\\u003c")
    embedded = (f'{BEGIN}\n<script id="room-data" type="application/json">'
                f'{encoded}</script>\n{END}')
    PAGE.write_text(before + embedded + after)
    counts = {origin: sum(room["origin"] == origin for room in data["rooms"])
              for origin in {room["origin"] for room in data["rooms"]}}
    print(f'Embedded {len(data["rooms"])} rooms in {PAGE.relative_to(ROOT)}: '
          + ", ".join(f"{name} {count}" for name, count in sorted(counts.items())))


if __name__ == "__main__":
    main()
