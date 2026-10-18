"""Headless validation for the approved, live room expansion."""

import subprocess

from tools.build_room_review import room_data


def lua(value):
    if isinstance(value, bool):
        return str(value).lower()
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, str):
        return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'
    if isinstance(value, list):
        return "{" + ",".join(map(lua, value)) + "}"
    if isinstance(value, dict):
        return "{" + ",".join(f"[{lua(k)}]={lua(v)}" for k, v in value.items()) + "}"
    raise TypeError(value)


def main():
    proposals = room_data()["proposals"]
    families = proposals["families"]
    rooms = [room for family in families for room in family["variants"]]
    assert len(families) == 15 and len(rooms) == 105
    assert all(len(family["variants"]) == 7 for family in families)
    assert len(proposals["corridors"]) == 5
    assert all(len(concept["variants"]) == 7 for concept in proposals["corridors"])
    assert all(len({tuple(variant["rows"]) for variant in concept["variants"]}) == 7
               for concept in proposals["corridors"])
    assert len({room["id"] for room in rooms}) == 105
    assert len({tuple(room["rows"]) for room in rooms}) == 105
    assert len({tuple("".join("#" if tile == "#" else "." for tile in row)
                      for row in room["rows"]) for room in rooms}) == 105
    assert len({(room["width"], room["height"]) for room in rooms}) == 15
    assert all(len(room["structures"]) == 3 for room in rooms)
    false_end = next(concept for concept in proposals["corridors"]
                     if concept["family"] == "false_end")
    assert false_end["rows"][9][20] == "#"
    assert any(node["kind"] == "secret_wall" and (node["x"], node["y"]) == (21, 10)
               for node in false_end["quest_nodes"])
    definitions = {
        room["id"]: {
            "id": room["id"], "rows": room["rows"], "roles": room["roles"],
            "sockets": room["sockets"], "markers": room["markers"],
            "editable": room["editable"], "rotations": [0, 1, 2, 3],
            "mirror": room["mirror"], "protected_default": True,
        }
        for room in rooms
    }
    code = "\n".join((
        "local Rooms=require('src.rooms')",
        "local Generation=require('src.generation')",
        "local Biomes=require('src.biomes')",
        "local defs=" + lua(definitions),
        "assert(Rooms.validate(defs," + lua(list(definitions)) + "))",
        "for id,def in pairs(defs) do",
        "  local live=assert(Rooms.definitions[id],id..' missing from game')",
        "  assert(#live.rows==#def.rows,id..' height changed')",
        "  for i,row in ipairs(def.rows) do assert(live.rows[i]==row,id..' geometry changed') end",
        "  assert(#live.structures==3,id..' structure slots missing')",
        "end",
        "local failures,seen_templates,fallbacks,repairs=0,{},0,0",
        "for _,id in ipairs(Biomes.order) do",
        "  for seed=1,100 do",
        "    local ok,maps=pcall(Generation.generate_pair,seed,{biome_id=id})",
        "    if not ok then print('FAIL',id,seed,maps);failures=failures+1",
        "    elseif not Generation.validate_pair(maps) then",
        "      print('INVALID',id,seed);failures=failures+1",
        "    else for _,map in ipairs(maps) do",
        "      if map.diagnostics.fallback then fallbacks=fallbacks+1 end",
        "      repairs=repairs+#map.repairs",
        "      for _,room in ipairs(map.rooms) do",
        "      if defs[room.template_id] then seen_templates[room.template_id]=true end",
        "    end end end",
        "  end",
        "end",
        "assert(failures==0,failures..' generation failures')",
        "assert(fallbacks==0 and repairs==0,'prototype pool caused fallback or repairs')",
        "local sampled=0;for _ in pairs(seen_templates) do sampled=sampled+1 end",
        "assert(sampled==105,'some proposed templates were never selected')",
        "print('105 live templates validated; 700 biome/seed pairs generated; '..sampled..' sampled; '..fallbacks..' fallbacks; '..repairs..' repairs')",
    ))
    result = subprocess.run(["luajit", "-"], input=code, text=True,
                            capture_output=True, check=False)
    if result.returncode:
        raise SystemExit(result.stdout + result.stderr)
    print(result.stdout.strip())


if __name__ == "__main__":
    main()
