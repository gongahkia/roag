#!/usr/bin/env python3
"""Capture original finite bell rewards, falling crosses and free route clues."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
local I=require('src.idea_interactions');local R=require('src.rng')
function capture_assembly(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id=input.fast and 'core:actor/ranger'or 'core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.currency=0
 s.idea_rooms={version=require('src.idea_interaction_content').version,nodes={},groups={}};s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};map.rooms={{id=1,x=10,y=10,width=15,height=15}};s.generated_map=map
 local node;local sequence={1,2,3};local bells={};local gid='fixture-bells'
 if input.mode=='bells'then
  local rng=R.new(37);for i=3,2,-1 do local j=R.next_int(rng,i);sequence[i],sequence[j]=sequence[j],sequence[i]end
  s.idea_rooms.groups[gid]={kind='bells',floor_id=map.floor_id,sequence=sequence,progress=0,completed=false}
  node=I.place(s,'bell_clue',map,15,16,{group=gid})
  for i,d in ipairs({{0,-1},{1,0},{0,1}})do bells[i]=I.place(s,'bell',map,15+d[1],16+d[2],{group=gid,bell=i})end
 elseif input.mode=='sign'then node=I.place(s,'route_sign',map,16,16,{dx=1,dy=0,hint='Treasure'})
 else
  local hx=input.mode=='safe'and 16 or 15;local cx=hx+1;local cells={}
  for _,d in ipairs({{0,0},{0,-1},{1,0},{0,1},{-1,0}})do cells[#cells+1]={floor_id=map.floor_id,x=cx+d[1],y=16+d[2]}end
  node=I.place(s,'falling_fixture',map,hx,16,{cells=cells,center_x=cx,center_y=16})
  if input.mode=='mist'then rows[16]=rows[16]:sub(1,15)..'m'..rows[16]:sub(17)end
 end
 V.recompute(s);local detail=I.options(s,node.id).detail;local result={};local commands=input.commands
 if input.mode=='bells'then commands={'wrong',tostring(sequence[1]),tostring(sequence[2]),tostring(sequence[3]),'repeat'}end
 for _,command in ipairs(commands)do
  local intent
  if input.mode=='bells'then
   local number=command=='wrong'and sequence[1]%3+1 or command=='repeat'and 1 or tonumber(command)
   intent={kind='interact',node_id=bells[number].id,choice_id='ring'}
  elseif command=='leave'then intent={kind='move',dx=-1,dy=0}
  else intent={kind='interact',node_id=node.id,choice_id='trigger'}end
  local ok,why,events=G.submit(s,intent);local fell,hits=0,0
  for _,e in ipairs(events or {})do if e.kind=='FixtureFell'then fell=fell+1 elseif e.kind=='DamageTaken'then hits=hits+1 end end
  local puzzle=s.idea_rooms.groups[gid]
  result[#result+1]={valid=ok,clock=s.clock,hp=p.hp,gold=s.currency,progress=puzzle and puzzle.progress or 0,
   completed=puzzle and puzzle.completed or false,pending=node.pending~=nil,spent=node.spent,fell=fell,hits=hits}
 end
 return {sequence=sequence,commands=commands,detail=detail,results=result}
end
""")
specs=[dict(mode='bells',commands=[]),dict(mode='self',commands=['trigger']),dict(mode='safe',commands=['trigger']),
       dict(mode='fast',fast=True,commands=['trigger','leave']),dict(mode='mist',commands=['trigger']),dict(mode='sign',commands=[])]
fields=['valid','clock','hp','gold','progress','completed','pending','spent','fell','hits']
cases=[]
for spec in specs:
    r=lua.globals().capture_assembly(table(spec))
    cases.append(dict(input=spec,commands=array(r.commands),sequence=array(r.sequence),detail=r.detail,
                      expected=array(r.results,lambda x:{k:x[k] for k in fields})))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-assemblies.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original assembly scenarios / {sum(len(c["expected"])for c in cases)} public commands.')
