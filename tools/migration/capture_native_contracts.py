#!/usr/bin/env python3
"""Capture original paid archive-contract outcomes without an AI scheduling shortcut."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local I=require('src.idea_interactions')
function capture_contract(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.currency=0
 s.idea_rooms={version=require('src.idea_interaction_content').version,nodes={},groups={}};s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};map.rooms={{id=1,x=10,y=10,width=15,height=15}};s.generated_map=map
 local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,10000);enemy.hp=1
 if input.mode=='utility'then p.utility={kind='escape_scroll'}end
 local n=I.place(s,'archive_contract',map,15,17,{room_id=1,room=map.rooms[1],targets={enemy.id},status='offered',defeated={}})
 V.recompute(s);local result={}
 for i,command in ipairs(input.commands)do
  if command=='leave'then p.position.x=24;V.recompute(s)end
  local intent=command=='accept'and {kind='interact',node_id=n.id,choice_id='accept'}
   or command=='abandon'and {kind='interact',node_id=n.id,choice_id='abandon'}
   or command=='kill'and {kind='basic_attack',dx=1,dy=0}
   or command=='dash'and {kind='dash',dx=0,dy=1}
   or command=='utility'and {kind='use_utility',dx=0,dy=1,steps=2}
   or command=='leave'and {kind='move',dx=1,dy=0}or {kind='wait',dx=1,dy=0}
  local ok,why,events=G.submit(s,intent);assert(ok,why)
  local defeated=0;for _,v in pairs(n.defeated)do if v then defeated=defeated+1 end end
  local rewards=0;for _,e in ipairs(events)do if e.kind=='CurrencyCollected'and e.amount==8 then rewards=rewards+1 end end
  result[#result+1]={clock=s.clock,hp=p.hp,gold=s.currency,status=n.status,failure=n.failure or '',defeated=defeated,spent=n.spent,rewards=rewards}
 end
 return result
end
""")
specs=[dict(mode='kill',commands=['accept','kill','wait']), dict(mode='dash',commands=['accept','dash']),
       dict(mode='utility',commands=['accept','utility']),dict(mode='abandon',commands=['accept','abandon']),
       dict(mode='leave',commands=['accept','leave'])]
fields=['clock','hp','gold','status','failure','defeated','spent','rewards']
cases=[dict(input=s,expected=array(lua.globals().capture_contract(table(s)),lambda r:{k:r[k] for k in fields}))for s in specs]
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-contracts.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original contract scenarios / {sum(len(c["expected"])for c in cases)} commands.')
