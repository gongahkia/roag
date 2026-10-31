#!/usr/bin/env python3
"""Capture original public commands discovering cracked caches and movement sigils."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
function capture_secret(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..input.class})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.visual_rows={};for y,row in ipairs(rows)do map.visual_rows[y]=row end
 map.placements={stairs={}};s.generated_map=map;s.currency=0
 local secret={id='secret:test',floor_id=s.active_floor_id,x=16,y=16,revealed=false}
 if input.kind=='cracked' then
  secret.definition_id='core:secret/cracked_cache';secret.wall_x=16;secret.wall_y=16;secret.cache_x=17;secret.cache_y=16
  rows[16]=rows[16]:sub(1,15)..'##'..rows[16]:sub(18)
 else secret.definition_id='core:secret/moss_sigil' end
 s.secrets={secret}
 if input.bump then
  local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,100000);e.hp=1000;e.max_hp=1000
  rows[16]=rows[16]:sub(1,15)..'.'..rows[16]:sub(17)
 end
 s.rng.state=37;V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local preview=G.preview(s,command,1,0);local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0})
  local discovered,rewarded=0,0
  for _,e in ipairs(events or {})do if e.kind=='SecretDiscovered'then discovered=discovered+1
   elseif e.kind=='SecretRewardSpawned'then rewarded=rewarded+1 end end
  result[#result+1]={valid=ok,clock=s.clock,x=p.position.x-15,gold=s.currency,revealed=secret.revealed,
   wall=rows[16]:sub(16,16),cache=rows[16]:sub(17,17),discovered=discovered,rewarded=rewarded,
   recovery=preview.valid and preview.cost or 0}
 end
 return result
end
""")
specs=[dict(name='explicit-sword',kind='cracked',**{'class':'adventurer'},commands=['basic_attack','basic_attack','move']),
       dict(name='other-basic',kind='cracked',**{'class':'ranger'},commands=['basic_attack']),
       dict(name='special',kind='cracked',**{'class':'adventurer'},commands=['sweeping_slash']),
       dict(name='walk-sigil',kind='sigil',**{'class':'adventurer'},commands=['move','move']),
       dict(name='dash-sigil',kind='sigil',**{'class':'adventurer'},commands=['dash']),
       dict(name='bump-sigil',kind='sigil',bump=True,**{'class':'adventurer'},commands=['move'])]
fields=['valid','clock','x','gold','revealed','wall','cache','discovered','rewarded','recovery']
cases=[dict(input=s,expected=array(lua.globals().capture_secret(table(s)),lambda r:{k:r[k] for k in fields}))for s in specs]
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-secrets.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original secret scenarios / {sum(len(c["expected"])for c in cases)} public commands.')
