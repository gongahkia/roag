#!/usr/bin/env python3
"""Capture original class-filtered chest offers; no generated production rules."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute('''
local G=require('src.game');local R=require('src.rng');local C=require('src.content')
local B=require('src.boon_content');local Rewards=require('src.rewards')
function chest_offers(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id=input.class_id})
 s.available_boons=B.normal_order()
 local offers=Rewards.make_offers(s,C.get_chest(input.chest),R.new(input.seed))
 return offers
end
''')
classes=['adventurer','ranger','arcanist','berserker','sentinel','alchemist','shade','hookrunner','trapper','duelist','geomancer','spiritbinder','tinkerer','stormcaller','wayfinder']
cases=[]
for kind in classes:
 for chest in ['standard','free_elite']:
  for seed in [1,37,48271,2147483646]:
   spec=dict(class_id='core:actor/'+kind,chest='core:chest/'+chest,seed=seed)
   result=lua.globals().chest_offers(table(spec))
   cases.append(dict(input=spec,offers=array(result,lambda o:dict(boon=o.boon_id,rarity=o.rarity))))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-chest-offers.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original chest draws / {len(cases)*3} offers.')
