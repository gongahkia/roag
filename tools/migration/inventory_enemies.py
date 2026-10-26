#!/usr/bin/env python3
"""Inventory every registered original enemy, including generated catalog entries.

This reads the pinned registry through Lua's debug API without rewriting any game
function. It is reference data for the native port, not generated runtime code.
"""
import json
from collections import Counter
from lupa.luajit21 import lua_type
from capture_native_boon_chains import lua
from inventory_boons import ROOT, REVISION, SOURCES


def data(value):
    if lua_type(value) == 'table':
        return {str(k): data(value[k]) for k in sorted(value.keys())}
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    raise TypeError('Non-data enemy definition')


registry = lua.execute("""
local content=require('src.content');assert(content.validate_all())
for i=1,20 do
    local name,value=debug.getupvalue(content.get_enemy,i)
    if not name then break end
    if name=='enemies' then return value end
end
error('Original enemy registry not found')
""")
entries = data(registry)
assert len(entries) == 50, f'Review original registry changes: {len(entries)} enemies'
for identifier, definition in entries.items():
    assert identifier == definition['id']
result = dict(revision=REVISION, sources=dict(sorted(SOURCES.items())), count=len(entries),
              ai_families=dict(sorted(Counter(d['ai'] for d in entries.values()).items())),
              entries=entries)
destination = ROOT/'docs/migration/original-enemy-inventory.json'
destination.write_text(json.dumps(result, indent=2, ensure_ascii=False)+'\n')
print(f'Inventoried {len(entries)} original enemies across {len(result["ai_families"])} AI families.')
