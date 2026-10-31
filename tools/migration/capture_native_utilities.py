#!/usr/bin/env python3
"""Capture pinned public commands for carried utilities and prepared supplies."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility')
function capture_utilities(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p}
 s.idea_rooms={version=require('src.idea_interaction_content').version,nodes={},groups={}}
 s.idea_terrain={version=1,nodes={}}
 local original=C.map_for(s);local map={};for k,v in pairs(original)do map[k]=v end
 local rows={};for y,row in ipairs(original.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};map.rooms={{id=1,x=10,y=10,width=15,height=15}};s.generated_map=map
 for _,t in ipairs(input.tiles or {})do local x,y=15+t[1],16+t[2]
  rows[y]=rows[y]:sub(1,x-1)..t[3]..rows[y]:sub(x+1)end
 if input.utility then p.utility={kind=input.utility}end
 p.supply_focus_actions=input.focus or 0;p.pickup_ward_actions=input.ward or 0
 V.recompute(s);local result={}
 for _,c in ipairs(input.commands)do
  local preview=G.preview(s,c[1],c[2],c[3],c[4])
  local ok,why,events=G.submit(s,{kind=c[1],dx=c[2],dy=c[3],steps=c[4]})
  local entered,moved,spent=0,0,0
  for _,e in ipairs(events or {})do
   if e.kind=='TileEntered'then entered=entered+1 elseif e.kind=='Moved'then moved=moved+1
   elseif e.kind=='SupplyFocusSpent'then spent=spent+1 end
  end
  result[#result+1]={valid=ok,clock=s.clock,hp=p.hp,x=p.position.x-15,y=p.position.y-16,
   recovery=preview.valid and preview.cost or 0,focus=p.supply_focus_actions or 0,ward=p.pickup_ward_actions or 0,
   utility=p.utility~=nil,planted=p.utility and p.utility.planted~=nil or false,entered=entered,moved=moved,spent=spent}
 end
 return result
end
""")
specs = [
    dict(name='scroll', utility='escape_scroll', commands=[['use_utility', 1, 0, 3]]),
    dict(name='bog_scroll', utility='escape_scroll', tiles=[[1, 0, 'g'], [2, 0, 'g']], commands=[['use_utility', 1, 0, 3]]),
    dict(name='ward_scroll', utility='escape_scroll', ward=3, tiles=[[1, 0, 'b'], [2, 0, 'b']], commands=[['use_utility', 1, 0, 3]]),
    dict(name='blocked_scroll', utility='escape_scroll', tiles=[[2, 0, '#']], commands=[['use_utility', 1, 0, 3]]),
    dict(name='return', utility='return_token', commands=[['use_utility', 1, 0], ['move', 1, 0], ['move', 1, 0], ['use_utility', 1, 0]]),
    dict(name='focus', focus=3, commands=[['wait', 1, 0], ['basic_attack', 1, 0], ['wait', 1, 0]]),
    dict(name='focus_expiry', focus=3, commands=[['wait', 1, 0], ['wait', 1, 0], ['wait', 1, 0], ['basic_attack', 1, 0]]),
]
fields = ['valid', 'clock', 'hp', 'x', 'y', 'recovery', 'focus', 'ward', 'utility', 'planted', 'entered', 'moved', 'spent']
cases = [dict(input=s, expected=array(lua.globals().capture_utilities(table(s)), lambda r: {k: r[k] for k in fields})) for s in specs]
path = ROOT / 'tests/Roag.Core.Tests/Tactics/Fixtures/original-utilities.json'
path.write_text(json.dumps(dict(revision=REVISION, sources=dict(sorted(SOURCES.items())), cases=cases), separators=(',', ':')) + '\n')
print(f'Captured {len(cases)} original utility scenarios / {sum(len(c["expected"]) for c in cases)} commands.')
