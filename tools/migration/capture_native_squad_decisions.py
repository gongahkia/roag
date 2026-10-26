#!/usr/bin/env python3
"""Capture pinned squad AI with real pathfinding, group identity and trap occupancy."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local AI=require('src.ai');local S=require('src.enemy_squads')
function capture_squad(input)
 local rows={};for y=1,17 do rows[y]=string.rep('.',17)end
 for _,kind in ipairs({'walls','mist'})do
  local symbol=kind=='walls'and'#'or'm'
  for _,c in ipairs(input[kind])do local x,y=9+c[1],9+c[2];rows[y]=rows[y]:sub(1,x-1)..symbol..rows[y]:sub(x+1)end
 end
 local p={id=1,faction='player',controller='player',hp=30,max_hp=32,blocks_movement=true,position={floor_id=1,x=9+input.dx,y=9+input.dy}}
 local e={id=input.actor,definition_id=input.id,faction='enemy',controller='ai',hp=input.hp,max_hp=10,blocks_movement=true,
  position={floor_id=1,x=9,y=9},ai_memory={ward_ready=input.ward},squad_id='test',squad_kind=input.group,squad_leader_id=input.leader}
 local s={clock=0,active_floor_id=1,player_id=1,entities={p,e},generated_map={width=17,height=17,floor_id=1,rows=rows},class_special={},enemy_snares={}}
 for _,n in ipairs(input.neighbors)do s.entities[#s.entities+1]={id=n.id,definition_id=n.definition,hp=n.hp,max_hp=n.max_hp,
  controller=n.ally and'ai'or'player',faction=n.ally and'enemy'or'player',blocks_movement=n.hp>0,
  position={floor_id=1,x=9+n.dx,y=9+n.dy},squad_id=n.group,squad_kind=input.group,squad_leader_id=input.leader}end
 for _,t in ipairs(input.snares)do s.enemy_snares[#s.enemy_snares+1]={source_id=t.source,floor_id=1,x=9+t.dx,y=9+t.dy}end
 if input.decoy then s.class_special.decoy={floor_id=1,x=5,y=9}end
 local r=AI.choose(s,e);local cells={}
 for _,c in ipairs(r.cells or{})do cells[#cells+1]={x=c.x-9,y=c.y-9}end
 return {kind=r.kind,dx=r.dx,dy=r.dy,ability=r.ability_id,cells=cells,delivery=r.ranged==true and'projectile'or r.ranged or'melee',
  support=r.support,shield_dx=r.shield_dx,shield_dy=r.shield_dy}
end
""")
groups=[('warded_bodyguards','ward_weaver','bodyguard_brute'),('healer_rushwings','mend_acolyte','rushwing'),
 ('snares_and_siege','snare_scribe','siege_wisp'),('shield_and_archer','shield_bearer','pin_archer'),('captain_and_followers','banner_captain','banner_follower')]
cases=[]
for kind,leader,follower in groups:
 for role in [leader,follower]:
  for dx,dy in [(0,-1),(1,0),(1,1),(2,0),(4,0),(3,2),(-2,-3),(0,7)]:
   for actor in [2,3,4,5]:
    for scene in ['open','wounded','other_group','far_leader','dead_leader','walls','mist','snares','blocked_lane','decoy']:
     leader_id=actor if role==leader else 90
     neighbors=[]
     if leader_id!=actor:
      neighbors.append(dict(id=90,definition='core:enemy/'+leader,dx=-7 if scene=='far_leader' else -3,dy=0,
       hp=0 if scene=='dead_leader' else 10,max_hp=10,ally=True,group='test'))
     avoid={(0,0),(dx,dy)}
     if scene in ['wounded','other_group','blocked_lane']:
      for i,(x,y) in enumerate([(1,0),(1,1),(-1,0)]):
       if (x,y) not in avoid: neighbors.append(dict(id=10+i,definition='core:enemy/'+follower,dx=x,dy=y,hp=1+i,max_hp=10+5*i,ally=True,group='other'if scene=='other_group'else'test'))
     spec=dict(id='core:enemy/'+role,actor=actor,hp=5 if scene=='wounded'else 10,ward=scene!='open',group=kind,leader=leader_id,dx=dx,dy=dy,
      neighbors=neighbors,decoy=scene=='decoy',
      walls=[list(p)for p in [(1,-2),(1,-1),(1,1),(1,2),(3,0)]if p not in avoid]if scene=='walls'else[],
      mist=[[1,0],[0,1],[-1,0],[0,-1]]if scene=='mist'else[],
      snares=[dict(source=actor,dx=dx-1,dy=dy),dict(source=actor,dx=dx+1,dy=dy)]if scene=='snares'else[])
     r=lua.globals().capture_squad(table(spec))
     support=None if r.support is None else dict(kind=r.support.kind,power=r.support.power or 0,target=r.support.target_id or 0)
     cases.append(dict(input=spec,expected=dict(kind=r.kind,dx=r.dx,dy=r.dy,ability=r.ability,cells=array(r.cells,lambda c:dict(x=c.x,y=c.y)),
      delivery=r.delivery,support=support,shield_dx=r.shield_dx,shield_dy=r.shield_dy)))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-squad-decisions.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original squad decisions.')
