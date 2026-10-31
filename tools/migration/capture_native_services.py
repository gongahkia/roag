#!/usr/bin/env python3
"""Capture original service offers, payments, ownership and one-use receipts."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
local I=require('src.idea_interactions');local R=require('src.rng');local B=require('src.boons')
function capture_service(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p}
 s.idea_rooms={version=require('src.idea_interaction_content').version,nodes={},groups={}};s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};map.rooms={{id=1,x=10,y=10,width=15,height=15}};s.generated_map=map
 local pool={};for _,id in ipairs(require('src.boon_content').normal_order())do if id~='core:boon/road_flask'and id~='core:boon/ration_reserve'then pool[#pool+1]=id end end
 s.available_boons=pool;s.currency=input.gold or 0;p.hp=p.hp-(input.missing or 0)
 if input.owned then B.set_stacks(p,'core:boon/emberbrand','rare',2)end
 if input.utility then p.utility={kind='cleanse_flask'}end
 local n=I.place(s,input.kind,map,16,16);V.recompute(s)
 local selection=input.kind=='attunement_forge'and {id='core:boon/emberbrand',rarity='rare'}or nil
 local options=I.options(s,n.id,selection);local captured={};local chosen
 for _,e in ipairs(options.entries)do
  if input.kind~='travelling_trader'or e.utility then
   captured[#captured+1]={price=e.price,hp_cost=e.hp_cost or 0,heal=e.heal or 0,ward=e.ward or false,focus=e.focus or false,
    utility=e.utility or '',offer=e.offer and e.offer.boon_id or '',rarity=e.offer and e.offer.rarity or '',enabled=e.enabled}
   if #captured==(input.choice or 1)then chosen=e.id end
  end
 end
 local ok,why,events=G.submit(s,{kind='interact',node_id=n.id,choice_id=chosen,selection=selection,replace=input.replace})
 local owned={};for id,counts in pairs(p.boons)do owned[id]={common=counts.common or 0,uncommon=counts.uncommon or 0,rare=counts.rare or 0,legendary=counts.legendary or 0}end
 return {seed=R.derive(s.root_seed or s.run_seed,n.id),options=captured,valid=ok,clock=s.clock,hp=p.hp,gold=s.currency,
  focus=p.supply_focus_actions or 0,ward=p.pickup_ward_actions or 0,utility=p.utility and p.utility.kind or '',spent=n.spent,owned=owned}
end
""")
specs = [dict(kind='offering_font'), dict(kind='offering_font', missing=19),
         dict(kind='cleanse_flask'), dict(kind='escape_scroll'), dict(kind='return_token'),
         dict(kind='escape_scroll', utility=True), dict(kind='escape_scroll', utility=True, replace=True),
         dict(kind='charm_well'), dict(kind='charm_well', choice=2), dict(kind='camp', missing=4), dict(kind='camp', choice=2),
         dict(kind='travelling_trader', gold=3), dict(kind='attunement_forge', owned=True, gold=3),
         dict(kind='attunement_forge', owned=True), dict(kind='memorial', owned=True, missing=2)]
fields = ['price', 'hp_cost', 'heal', 'ward', 'focus', 'utility', 'offer', 'rarity', 'enabled']
cases=[]
for spec in specs:
    r=lua.globals().capture_service(table(spec))
    expected={k:r[k] for k in ['valid','clock','hp','gold','focus','ward','utility','spent']}
    expected['options']=array(r.options, lambda o:{k:o[k] for k in fields})
    expected['owned']={id:dict(counts.items()) for id,counts in r.owned.items()}
    cases.append(dict(input=spec, seed=r.seed, expected=expected))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-services.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original public service commands.')
