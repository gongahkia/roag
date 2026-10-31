#!/usr/bin/env python3
"""Capture original ore, found wards and timed mist-clearing braziers via Game.submit."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
function capture_room_feature(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..(input.class or 'adventurer')})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.idea_terrain={version=1,nodes={}}
 local map={};for k,v in pairs(C.map_for(s))do map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.visual_rows={};for y,row in ipairs(rows)do map.visual_rows[y]=row end
 map.placements={stairs={}};s.generated_map=map;s.currency=0
 local feature={id='test:feature',kind=input.kind,floor_id=s.active_floor_id,x=15+(input.offset or 1),y=16,
  collected=false,broken=false,lit_until=0}
 s.review_features={feature};p.supply_focus_actions=input.focus or 0
 if input.kind=='brazier'then rows[16]=rows[16]:sub(1,16)..'m'..rows[16]:sub(18)end
 if input.enemy then G.spawn_enemy(s,'core:enemy/mossbound_guard',16,17,250)end
 if input.sentry then G.spawn_enemy(s,'core:enemy/mossbound_guard',19,16,100000)end
 s.rng.state=37;V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
  local broken,collected,lit,blocked=0,0,0,0
  for _,e in ipairs(events)do if e.kind=='OreBroken'then broken=broken+1 elseif e.kind=='WardFlaskCollected'then collected=collected+1
   elseif e.kind=='BrazierLit'then lit=lit+1 elseif e.kind=='WardFlaskBlocked'then blocked=blocked+1 end end
  result[#result+1]={clock=s.clock,x=p.position.x-15,gold=s.currency,hp=p.hp,focus=p.supply_focus_actions or 0,
   ward=p.pickup_ward_actions or 0,used=feature.collected or feature.broken,litUntil=feature.lit_until,
   mist=C.mist_blocks(map,17,16,s),broken=broken,collected=collected,lit=lit,blocked=blocked}
 end
 return result
end
""")
specs=[dict(kind='ore',commands=['basic_attack','basic_attack','move']),
       dict(kind='ore',commands=['sweeping_slash','move']),
       dict(kind='ward',focus=3,commands=['move','wait','wait','wait']),
       dict(kind='ward',enemy=True,commands=['move','wait','wait','wait']),
       dict(kind='brazier',commands=['move','wait','wait','wait']),
       dict(kind='ore',offset=2,sentry=True,**{'class':'tinkerer'},commands=['sweeping_slash','wait','wait'])]
fields=['clock','x','gold','hp','focus','ward','used','litUntil','mist','broken','collected','lit','blocked']
cases=[dict(input=s,expected=array(lua.globals().capture_room_feature(table(s)),lambda r:{k:r[k] for k in fields}))for s in specs]
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-room-features.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original room feature scenarios / {sum(len(c["expected"])for c in cases)} public commands.')
