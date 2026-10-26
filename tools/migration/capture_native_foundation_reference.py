#!/usr/bin/env python3
"""Capture original Lua behavior as data, never as a production dependency.

Run with a Python environment containing lupa. Sources are read directly from
the pinned Git revision, not the working tree or generated C# translation.
Normal .NET tests consume the checked-in JSON and need neither Python nor Lua.
"""
import hashlib
import json
from pathlib import Path
import subprocess

from lupa import LuaRuntime

ROOT = Path(__file__).resolve().parents[2]
REVISION = "d498fe0889050d2a23f2ff86d80db6084ed6467a"
lua = LuaRuntime(unpack_returned_tuples=True)
sources = {}


def source(module):
    path = module.replace(".", "/") + ".lua"
    value = subprocess.check_output(["git", "show", f"{REVISION}:{path}"], cwd=ROOT)
    sources[path] = hashlib.sha256(value).hexdigest()
    return value.decode()


lua.globals().original_source = source
lua.execute("""
package.path = ''
package.cpath = ''
package.searchers = {function(name)
    return assert(load(original_source(name), '@original/' .. name))
end}
Classes = require('src.classes')
RNG = require('src.rng')
function geometry(id, kind, dx, dy, walls, has_spirit, body_step, explicit)
    local p = {x=1000, y=2000, floor_id='underground'}
    local owner = {id=1, definition_id=id, position=p}
    local state = {entities={owner}}
    if has_spirit then
        state.entities[#state.entities+1] = {controller='idea_companion', companion_kind='spirit',
            position={x=990,y=1990,floor_id='underground'}}
    end
    if body_step > 0 then
        state.entities[#state.entities+1] = {hp=10,
            position={x=p.x+dx*body_step, y=p.y+dy*body_step, floor_id='underground'}}
    end
    local content = {
        map_for=function() return {} end,
        projectile_passable=function(_,x,y)
            for _,c in ipairs(walls) do if c.x==x and c.y==y then return false end end
            return true
        end
    }
    return Classes.attack(state,owner,kind,dx,dy,content,explicit)
end
function progression(id, seed, amounts)
    local Rewards = require('src.rewards')
    local c = Classes.get(id)
    local actor = {id=1, max_hp=c.hp, hp=c.hp, damage=c.damage, slash_damage=c.slash_damage,
        defense=c.defense, speed=c.speed, crit_chance_ppm=c.crit_chance_ppm,
        crit_damage_percent=c.crit_damage_percent}
    local state = {player_id=1, entities={actor}, progression={level=1,xp_total=0}, rng=RNG.new(seed)}
    local result = {}
    for _,amount in ipairs(amounts) do
        local growth = {}
        Rewards.gain_xp(state, amount, {}, function(kind,event)
            if kind=='LevelUp' then growth[#growth+1]=event.stat end
        end)
        result[#result+1] = {level=state.progression.level, xp=state.progression.xp_total,
            random=state.rng.state, growth=growth, stats={actor.max_hp,actor.damage,actor.slash_damage,
                actor.defense,actor.speed,actor.crit_chance_ppm,actor.crit_damage_percent}}
    end
    return result
end
""")


def cells(table):
    return [[int(table[i].x), int(table[i].y)] for i in range(1, len(table) + 1)]


definitions = lua.globals().Classes
classes = []
for i in range(1, len(definitions.order) + 1):
    entry = definitions.definitions[definitions.order[i]]
    keys = ["id", "name", "hp", "damage", "slash_damage", "defense", "speed",
            "dash_distance", "basic_id", "special_id", "basic_name", "special_name", "dash_name",
            "crit_chance_ppm", "crit_damage_percent"]
    classes.append({**{key: entry[key] for key in keys},
                    "costs": [entry.costs[key] for key in ["move", "basic_attack", "sweeping_slash", "dash"]]})

directions = [(0,-1), (1,-1), (1,0), (1,1), (0,1), (-1,1), (-1,0), (-1,-1)]
geometries = []
for entry in classes:
    for facing, (dx, dy) in enumerate(directions):
        scenarios = [(False, 0, True, walls) for walls in [[], [[1000+dx,2000+dy]],
            [[1000+2*dx,2000+2*dy]], [[1000+dx,2000], [1000,2000+dy]]]]
        if entry["id"] == "core:actor/spiritbinder":
            scenarios += [(owns, step, True, []) for owns in [False, True] for step in [1,2,3]]
        if entry["id"] == "core:actor/duelist":
            scenarios.append((False, 0, False, []))
        for action in ["basic_attack", "sweeping_slash"]:
            for owns, body, explicit, walls in scenarios:
                obstacles = lua.table_from([lua.table_from({"x":x, "y":y}) for x,y in walls])
                result = lua.globals().geometry(entry["id"], action, dx, dy, obstacles, owns, body, explicit)
                geometries.append({"class":entry["id"], "action":action, "facing":facing,
                    "walls":walls, "ownsSpirit":owns, "bodyStep":body, "explicitAim":explicit,
                    "cells":cells(result[0]), "ability":result[1], "ranged":result[2], "element":result[3]})

rng = lua.globals().RNG
random_cases = []
for seed in [1,42,123456789,2147483646]:
    for maximum in [1,2,6,100,1073741824,2147483646]:
        stream = rng.new(seed)
        values = [int(rng.next_int(stream, maximum)) for _ in range(32)]
        random_cases.append({"seed":seed, "maximum":maximum, "values":values, "state":int(stream.state)})
derived = [{"seed":seed, "label":label, "value":int(rng.derive(seed,label))}
           for seed in [1,42,2147483646] for label in ["progression", "room:123", "洞窟🔥"]]
progressions = []
amounts = [4,1,10,100,500,1,1000]
for entry in classes:
    for seed in [1,42,2147483646]:
        steps = lua.globals().progression(entry["id"], seed, lua.table_from(amounts))
        progressions.append({"class":entry["id"], "seed":seed, "amounts":amounts,
            "steps":[{"level":int(s.level), "xp":int(s.xp), "random":int(s.random),
                      "stats":[int(s.stats[i]) for i in range(1,len(s.stats)+1)],
                      "growth":[s.growth[i] for i in range(1,len(s.growth)+1)]}
                     for s in [steps[i] for i in range(1,len(steps)+1)]]})
output = {"revision":REVISION, "sources":sources, "classes":classes, "geometry":geometries,
          "random":random_cases, "derived":derived, "progression":progressions}
destination = ROOT / "tests/Roag.Core.Tests/Tactics/Fixtures/original-foundation.json"
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_text(json.dumps(output, ensure_ascii=False, separators=(",", ":")) + "\n")
print(f"Captured {len(classes)} classes, {len(geometries)} footprints, {len(random_cases)} RNG sequences -> {destination}")
