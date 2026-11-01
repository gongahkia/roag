#!/usr/bin/env python3
"""Pin original branch geometry and paid quest progression, without rewriting listeners."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local B=require('src.corridor_branches');local G=require('src.game');local C=require('src.content')
local V=require('src.visibility');local F=require('src.review_features')
local design
for i=1,20 do local name,value=debug.getupvalue(B.attach,i);if name=='design'then design=value;break end end
assert(design,'missing original branch designer')
local function find(fn,wanted,seen)
 seen=seen or{};if seen[fn]then return end;seen[fn]=true
 for i=1,100 do
  local name,value=debug.getupvalue(fn,i);if not name then break end
  if name==wanted then return value end
  if type(value)=='function'then local result=find(value,wanted,seen);if result then return result end end
 end
end
local path=assert(find(require('src.generation').generate,'corridor_path'))
function capture_path(seed,strategy,horizontal)
 local a={x=10,y=12,facing=horizontal and'E'or'S'}
 local b={x=horizontal and 40 or 19,y=horizontal and 20 or 44}
 local rng=require('src.rng').new(seed)
 return{cells=path(a,b,rng,{strategy=strategy}),random=rng.state}
end
function capture_branch(kind,dx,dy,side)
 local function at(u,v)return{x=20+u*dx-v*dy*side,y=20+u*dy+v*dx*side}end
 local cells,nodes=design(kind,at);return{cells=cells,nodes=nodes}
end
function capture_quest(input)
 local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/'..(input.class or'adventurer')})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.idea_terrain={version=1,nodes={}};s.currency=0
 local map={};for k,v in pairs(C.map_for(s))do map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.placements={stairs={}};map.editable_cells={};map.rooms={{id=1,role='start',structures={}}}
 local function at(u,v)return{x=12+u,y=15+v}end
 local cells,nodes=design(input.kind,at)
 map.corridor_quests={{id='test:quest',kind=input.kind,edge_id=1,cells=cells,nodes=nodes}};s.generated_map=map
 local target
 for _,n in ipairs(nodes)do if n.kind=='enemy'then target=G.spawn_enemy(s,'core:enemy/mossbound_guard',n.x,n.y,100000);target.hp=1;target.defense=0 end end
 F.populate(s,map);s.rng.state=37;V.recompute(s);local result={}
 for _,command in ipairs(input.commands)do
  local preview=G.preview(s,command[1],command[2],command[3]);local ok,why,events=G.submit(s,{kind=command[1],dx=command[2],dy=command[3]})
  local trace={};for _,e in ipairs(events or{})do if e.kind:sub(1,9)=='Sidequest'or e.kind=='CurrencyCollected'then trace[#trace+1]=e.kind end end
  local active={};local ready,collected=false,false;local gate=false
  for _,f in ipairs(s.review_features)do
   active[#active+1]=f.discovered or f.collected or f.activated or f.lit or false
   if f.kind=='quest_reward'then ready=f.unlocked;collected=f.collected end
  end
  for _,e in ipairs(s.entities)do if e.controller=='review_gate'then gate=e.open end end
  result[#result+1]={valid=ok,recovery=preview.valid and preview.cost or 0,clock=s.clock,x=p.position.x-15,y=p.position.y-16,
   gold=s.currency,gate=gate,ready=ready,collected=collected,active=active,trace=trace,random=s.rng.state}
 end
 return result
end
""")

def move(dx,dy): return ['move',dx,dy]
E=move(1,0);W=move(-1,0);S=move(0,1);N=move(0,-1)
specs=[dict(kind='spur',commands=[E,E,S,S,W,W,N,S,E,E,W,E]),
       dict(kind='braid',commands=[E,S,S,W,W,E,E,S,S,N,S]),
       dict(kind='switchback',commands=[E,E,E,S,W,W,W,W,S,S,E,E,E,E,W,E]),
       dict(kind='hub',commands=[E,W,S,S,N,N,E,E,N,N,N,N,S,S,S,S,W,E,W]),
       dict(kind='false_end',commands=[E,E,S,W,S,W,S,E,['basic_attack',1,0],N,E,W,E]),
       dict(kind='spur',**{'class':'berserker'},commands=[S,S,E,E,W,W,N])]
geometry=[]
for kind in ['spur','braid','switchback','hub','false_end']:
    for dx,dy in [(0,-1),(1,0),(0,1),(-1,0)]:
        for side in [-1,1]:
            r=lua.globals().capture_branch(kind,dx,dy,side)
            geometry.append(dict(kind=kind,dx=dx,dy=dy,side=side,
                cells=array(r['cells'],lambda p:[p['x'],p['y']]),
                nodes=array(r['nodes'],lambda n:dict(kind=n['kind'],x=n['x'],y=n['y']))))
fields=['valid','recovery','clock','x','y','gold','gate','ready','collected','random']
cases=[]
for spec in specs:
    rows=array(lua.globals().capture_quest(table(spec)),lambda r:dict(**{k:r[k] for k in fields},active=array(r['active']),trace=array(r['trace'])))
    cases.append(dict(input=spec,expected=rows))
paths=[]
for strategy in ['ruins','warrens','canals','courtyards','observatory','archive','forge']:
    for horizontal in [False,True]:
        for seed in [1,2,7,37,42,919,12345]:
            r=lua.globals().capture_path(seed,strategy,horizontal)
            paths.append(dict(seed=seed,strategy=strategy,horizontal=horizontal,random=r['random'],cells=array(r['cells'],lambda p:[p['x'],p['y']])))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-corridors.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),geometry=geometry,paths=paths,cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(geometry)} original branch shapes, {len(paths)} corridor paths and {sum(len(c["expected"])for c in cases)} public quest commands.')
