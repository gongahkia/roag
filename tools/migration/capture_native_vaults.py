#!/usr/bin/env python3
"""Capture all original keyed-vault outcomes and paid opening/physical rewards."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local C=require('src.content');local V=require('src.visibility');local R=require('src.rng')
function vault_seed(seed)
 local s=G.new(seed,'core:map/scrolling_room');return R.derive(s.run_seed,s.active_floor_id..':vault:19,16')
end
function capture_vault(input)
 local s=G.new(input.seed,'core:map/scrolling_room',{class_id='core:actor/'..(input.class or 'adventurer')})
 local p=G.player(s);p.position.x=15;p.position.y=16;s.entities={p};s.currency=0;s.idea_terrain={version=1,nodes={}}
 local map={};for k,v in pairs(C.map_for(s))do map[k]=v end
 local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
 map.rows=rows;map.visual_rows={};for y,row in ipairs(rows)do map.visual_rows[y]=row end
 map.placements={stairs={}};s.generated_map=map
 local vault=G.spawn_door(s,19,16,'test:vault');vault.controller='review_vault';vault.definition_id='core:object/review_vault'
 vault.opened=false;vault.blocks_vision=false;vault.blocks_projectiles=false
 local key={id='test:key',kind='key',floor_id=s.active_floor_id,x=11,y=16,collected=input.held or false}
 s.review_features={key};s.review_keys[s.active_floor_id]=input.held or nil
 s.secrets={{id='test:clue',definition_id='core:secret/moss_sigil',floor_id=s.active_floor_id,x=13,y=19,revealed=false}}
 local derived=R.derive(s.run_seed,s.active_floor_id..':vault:19,16');s.rng.state=37;V.recompute(s);local steps={}
 for _,dx in ipairs(input.commands)do
  local preview=G.preview(s,'move',dx,0);local ok,why,events=G.submit(s,{kind='move',dx=dx,dy=0})
  local opened,picked=0,0;for _,e in ipairs(events or {})do if e.kind=='VaultOpened'then opened=opened+1
   elseif e.kind=='VaultKeyCollected'then picked=picked+1 end end
  local currency,health,chests,ambush=0,0,0,0
  for _,d in ipairs(s.drops)do if not d.collected then
   if d.kind=='currency'then currency=currency+d.amount elseif d.kind=='health'then health=health+d.amount end end end
  for _,e in ipairs(s.entities)do if e.controller=='chest'then chests=chests+1 elseif e.controller=='ai'then ambush=ambush+1 end end
  steps[#steps+1]={valid=ok,clock=s.clock,x=p.position.x-15,hp=p.hp,gold=s.currency,opened=vault.opened,key=key.collected,
   held=s.review_keys[s.active_floor_id] or false,outcome=vault.opened and ({'cache','clue','boon','ambush'})[derived%4+1] or '',
   clue=s.review_clues[s.active_floor_id] or '',currency=currency,health=health,chests=chests,ambush=ambush,
   openings=opened,picked=picked,recovery=preview.valid and preview.cost or 0,rng=s.rng.state}
 end
 return {seed=derived,steps=steps}
end
""")
seeds={}
for seed in range(1,100):
    seeds.setdefault(int(lua.globals().vault_seed(seed))%4,seed)
    if len(seeds)==4: break
specs=[dict(name=name,seed=seeds[i],held=True,commands=[1,1,1,1,1])for i,name in enumerate(['cache','clue','boon','ambush'])]
specs+=[dict(name='paid-key',seed=seeds[0],commands=[-1]*4+[1]*9),
        dict(name='missing-key',seed=seeds[0],commands=[1]*5),
        dict(name='heavy-class',seed=seeds[0],held=True,**{'class':'berserker'},commands=[1]*5)]
fields=['valid','clock','x','hp','gold','opened','key','held','outcome','clue','currency','health','chests','ambush','openings','picked','recovery','rng']
cases=[]
for spec in specs:
    result=lua.globals().capture_vault(table(spec))
    cases.append(dict(input=spec,seed=result['seed'],expected=array(result['steps'],lambda r:{k:r[k] for k in fields})))
path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-vaults.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original vault scenarios / {sum(len(c["expected"])for c in cases)} public commands.')
