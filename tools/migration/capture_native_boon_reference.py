#!/usr/bin/env python3
"""Execute pinned Lua boon arithmetic/memory rules into portable native test data."""
import json

from lupa import LuaRuntime
from inventory_boons import ROOT, REVISION, SOURCES, original_source


def array(table):
    return [table[i] for i in range(1, len(table) + 1)]


lua = LuaRuntime(unpack_returned_tuples=True)
lua.globals().original_source = original_source
lua.execute("""
package.path = ''
package.cpath = ''
package.searchers = {function(name) return assert(load(original_source(name), '@original/' .. name)) end}
Catalog=require('src.boon_content')
Boons=require('src.boons')
RNG=require('src.rng')
assert(Catalog.validate())
function ward_case(ids, clock, damage, reversed)
    local owner={boons={},boon_memory={}}
    local wards,speeds,expires={2,7,100},{4,6,100},{100,100,99}
    for index=1,3 do
        local n=reversed and 4-index or index
        owner.boons[ids[n]]={common=1}
        owner.boon_memory[ids[n]]={ward=wards[n],speed=speeds[n],expires_at=expires[n]}
    end
    local state={clock=clock}
    local total=Boons.ward_total(state,owner)
    local speed=Boons.speed_bonus(state,owner)
    local absorbed=Boons.consume_ward(state,owner,damage)
    local remaining={}
    for _,id in ipairs(ids) do remaining[#remaining+1]=owner.boon_memory[id].ward end
    return total,speed,absorbed,remaining
end
""")
catalog, boons, rng = lua.globals().Catalog, lua.globals().Boons, lua.globals().RNG
aggregates = []
for identifier in array(catalog.order):
    for counts in [[1, 0, 0, 0], [1, 2, 3, 4], [0, 0, 0, 10], [100, 0, 0, 0]]:
        stacks = lua.table_from(dict(zip(array(catalog.rarities), counts)))
        aggregates.append({"id": identifier, "counts": counts,
                           "chance": int(boons.aggregate(identifier, stacks, "chance")),
                           "power": int(boons.aggregate(identifier, stacks, "power"))})
rolls = []
for seed in [1, 42, 2147483646]:
    for chance in [0, 1, 499999, 500000, 999999, 1000000, 1250000, 3000000, 7500000]:
        for coefficient in [0, .25, .5, 1, 1.333333, 2, 10]:
            state = lua.table_from({"rng": rng.new(seed)})
            values = [int(boons.proc_count(state, chance, coefficient)) for _ in range(16)]
            rolls.append({"seed": seed, "chance": chance, "coefficient": coefficient,
                          "values": values, "state": int(state.rng.state)})
ward_ids = [identifier for identifier in array(catalog.order)
            if any(effect.kind == "ward" for effect in array(catalog.get(identifier).effects))][:3]
wards = []
for clock in [98, 99, 100, 101]:
    for damage in [0, 1, 2, 3, 9, 10, 200]:
        for reverse in [False, True]:
            total, speed, absorbed, remaining = lua.globals().ward_case(lua.table_from(ward_ids), clock, damage, reverse)
            wards.append({"clock": clock, "damage": damage, "reverse": reverse, "total": total,
                          "speed": speed, "absorbed": absorbed, "remaining": array(remaining)})
dice = []
for seed in [1, 42, 2147483646]:
    owner = lua.table_from({})
    boons.set_stacks(owner, "core:boon/fateful_dice", "uncommon", 2)
    boons.set_stacks(owner, "core:boon/fateful_dice", "legendary", 1)
    state = lua.table_from({"rng": rng.new(seed)})
    values = []
    for action in range(1, 33):
        value = boons.roll_dice(state, owner, action)
        values.append({"face": value.face, "power": value.power})
    dice.append({"seed": seed, "values": values, "state": int(state.rng.state)})
result = {"revision": REVISION, "sources": dict(sorted(SOURCES.items())), "aggregates": aggregates,
          "rolls": rolls, "wardIds": ward_ids, "wards": wards, "dice": dice}
destination = ROOT / "tests/Roag.Core.Tests/Tactics/Fixtures/original-boon-rules.json"
destination.write_text(json.dumps(result, separators=(",", ":")) + "\n")
print(f"Captured {len(aggregates)} stack cases, {len(rolls)} chance sequences, {len(wards)} ward cases and {len(dice)} dice sequences.")
