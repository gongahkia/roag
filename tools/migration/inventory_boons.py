#!/usr/bin/env python3
"""Inventory registered original boons, including dynamically constructed entries.

Requires lupa only for this reference capture. This is preservation evidence, not
generated production code or a claim that the native effect pipeline is implemented.
"""
import hashlib
import json
from pathlib import Path
import subprocess

from lupa import LuaRuntime, lua_type

ROOT = Path(__file__).resolve().parents[2]
REVISION = "d498fe0889050d2a23f2ff86d80db6084ed6467a"
SOURCES = {}


def original_source(module):
    path = module.replace(".", "/") + ".lua"
    raw = subprocess.check_output(["git", "show", f"{REVISION}:{path}"], cwd=ROOT)
    SOURCES[path] = hashlib.sha256(raw).hexdigest()
    return raw.decode()


def data(value):
    if lua_type(value) == "table":
        keys = list(value.keys())
        if set(keys) == set(range(1, len(keys) + 1)):
            return [data(value[i]) for i in range(1, len(keys) + 1)]
        return {str(key): data(value[key]) for key in sorted(keys)}
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    raise TypeError(f"Non-data catalog entry: {lua_type(value)}")


def predicates(node):
    if isinstance(node, dict):
        if "op" in node:
            yield node["op"]
        for child in node.values():
            yield from predicates(child)
    elif isinstance(node, list):
        for child in node:
            yield from predicates(child)


def main():
    lua = LuaRuntime(unpack_returned_tuples=True)
    lua.globals().original_source = original_source
    lua.execute("""
package.path = ''
package.cpath = ''
package.searchers = {function(name)
    return assert(load(original_source(name), '@original/' .. name))
end}
Catalog = require('src.boon_content')
assert(Catalog.validate())
Patterns = require('src.boon_patterns')
function named_upvalue(fn, wanted)
    for i=1,100 do
        local name,value=debug.getupvalue(fn,i)
        if not name then break end
        if name==wanted then return value end
    end
    error('Missing original validation registry: '..wanted)
end
SupportedEvents = named_upvalue(Catalog.validate, 'valid_events')
SupportedEffects = named_upvalue(Catalog.validate, 'valid_effects')
SupportedPredicates = named_upvalue(named_upvalue(Catalog.validate, 'validate_predicate'), 'valid_predicates')
""")
    catalog = lua.globals().Catalog
    order = data(catalog.order)
    assert len(order) == len(set(order)) == len(list(catalog.definitions.keys()))
    definitions = [data(catalog.get(identifier)) for identifier in order]
    mechanics = {name: {} for name in ["triggers", "effects", "predicates", "modifiers", "stackPolicies"]}
    for definition in definitions:
        uses = {"triggers": definition["triggers"],
                "effects": [effect["kind"] for effect in definition["effects"]],
                "predicates": list(predicates(definition)),
                "modifiers": [modifier["kind"] for modifier in definition.get("modifiers", [])],
                "stackPolicies": [definition["stack_policy"]]}
        for category, names in uses.items():
            for name in sorted(set(names)):
                mechanics[category].setdefault(name, []).append(definition["id"])
    result = {"revision": REVISION, "sources": dict(sorted(SOURCES.items())),
              "scope": "Registered boon catalog after all ordered expansion merges; original art fields are reference only and are not approved final sprite bindings.",
              "catalogVersion": catalog.version, "rarities": data(catalog.rarities),
              "starterPool": data(catalog.starter_order()), "normalPool": data(catalog.normal_order()),
              "definitions": definitions, "mechanics": mechanics,
              "supportedSchema": {"triggers": sorted(lua.globals().SupportedEvents.keys()),
                                  "effects": sorted(lua.globals().SupportedEffects.keys()),
                                  "predicates": sorted(lua.globals().SupportedPredicates.keys()),
                                  "patterns": sorted(lua.globals().Patterns.names.keys())},
              "weaponForms": data(lua.globals().Patterns.weapon_forms)}
    destination = ROOT / "docs/migration/original-boon-inventory.json"
    destination.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n")
    print(f"Inventoried {len(definitions)} registered boons, "
          f"{len(mechanics['effects'])} effect kinds, {len(mechanics['predicates'])} predicate operations "
          f"-> {destination}")


if __name__ == "__main__":
    main()
