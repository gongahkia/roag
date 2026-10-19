local Scheduler=require('src.scheduler')
local S={id='core:actor/stormcaller',cap=3,delay=100,symbol=615}
S.definition={id=S.id,name='Stormcaller',description='Build charge on bolt hits; discharge a locked cross or Dash to ground a ward',
 hp=21,damage=2,slash_damage=2,defense=0,speed=100,crit_chance_ppm=0,crit_damage_percent=150,
 costs={move=100,basic_attack=100,sweeping_slash=150,dash=130},dash_distance=2,
 basic_id='core:ability/stormcaller_bolt',special_id='core:ability/stormcaller_discharge',
 basic_name='Charging Bolt',special_name='Locked Discharge',dash_name='Grounding Dash',
 ui_controls='F Charge bolt · R Discharge · X Ground ward'}
function S.on_event(state,event,emit)
 if event.kind~='HitConfirmed' or event.source_id~=state.player_id or event.ability_id~=S.definition.basic_id
  or not event.direct_attack or event.actual_loss<=0 or event.class_generated or event.proc_coefficient<=0 then return end
 local p;for _,e in ipairs(state.entities)do if e.id==state.player_id then p=e;break end end
 if p.definition_id~=S.id or p.storm_last_action==event.action_id then return end
 p.storm_last_action=event.action_id;p.storm_charge=math.min(S.cap,(p.storm_charge or 0)+1)
 emit('StormChargeGained',{charge=p.storm_charge,floor_id=p.position.floor_id,x=p.position.x,y=p.position.y})
end
function S.ground(owner,emit)
 if owner.definition_id==S.id and (owner.storm_charge or 0)>0 then
  owner.storm_charge=0;owner.storm_ward=true
  emit('StormGrounded',{floor_id=owner.position.floor_id,x=owner.position.x,y=owner.position.y})
 end
end
function S.absorb(owner,damage)
 if owner.definition_id==S.id and owner.storm_ward and damage>0 then owner.storm_ward=nil;return true end
 return false
end
function S.lock(state,owner,cells,action_id,emit)
 local charge=owner.storm_charge;assert(charge and charge>0)
 owner.storm_charge=0
 local work=Scheduler.enqueue(state,{kind='effect',due=state.clock+S.delay,actor_id=0,
  attack_id=action_id,floor_id=owner.position.floor_id})
 work.effect_id=work.id;work.idea_storm=true
 state.class_special.storm={schedule_id=work.id,due=work.due,action_id=action_id,owner_id=owner.id,
  floor_id=owner.position.floor_id,cells=cells,damage=owner.slash_damage+2*charge,charge=charge}
 emit('StormDischargeLocked',{target_cells=cells,due=work.due,charge=charge,damage=owner.slash_damage+2*charge,
  floor_id=owner.position.floor_id,x=owner.position.x,y=owner.position.y})
end
function S.resolve(state,item,fire)
 local r=assert(state.class_special.storm,'orphan storm work')
 assert(r.schedule_id==item.id and r.due==state.clock and r.floor_id==state.active_floor_id,'mistimed storm')
 state.class_special.storm=nil;fire(r)
end
function S.validate(state,maps)
 local player;for _,e in ipairs(state.entities)do
  if e.id==state.player_id then player=e end
  if e.storm_charge~=nil or e.storm_ward~=nil or e.storm_last_action~=nil then
   assert(e.id==state.player_id and e.definition_id==S.id,'storm resource on wrong actor')
   assert(e.storm_charge==nil or type(e.storm_charge)=='number' and e.storm_charge%1==0 and e.storm_charge>=0 and e.storm_charge<=S.cap,'invalid storm charge')
   assert(e.storm_ward==nil or e.storm_ward==true,'invalid storm ward')
   assert(e.storm_last_action==nil or type(e.storm_last_action)=='number' and e.storm_last_action%1==0 and e.storm_last_action>=1 and e.storm_last_action<state.next_action_id,'invalid storm charge history')
  end
 end
 local r=state.class_special.storm;local found=false
 local scheduled={};for _,w in ipairs(state.schedule)do scheduled[#scheduled+1]=w end
 for _,pause in pairs(state.floor_pause or {})do for _,w in ipairs(pause.schedule)do scheduled[#scheduled+1]=w end end
 for _,w in ipairs(scheduled)do if w.idea_storm then
  assert(not found and r and w.idea_storm==true and w.kind=='effect' and w.actor_id==0 and w.effect_id==w.id
   and w.id==r.schedule_id and w.due==r.due and w.floor_id==r.floor_id,'orphan storm schedule');found=true
 end end
 if r then
  local map=maps[r.floor_id];local clock=((state.floor_pause or {})[r.floor_id]or{}).suspended_at or state.clock
  assert(found and player.definition_id==S.id and r.owner_id==player.id and map
   and r.due>=clock and r.due<=clock+S.delay and type(r.damage)=='number' and r.damage%1==0 and r.damage>=0
   and type(r.charge)=='number' and r.charge%1==0 and r.charge>=1 and r.charge<=S.cap
   and type(r.cells)=='table' and #r.cells>=1 and #r.cells<=5,'invalid locked storm')
  local seen={};for _,c in ipairs(r.cells)do
   assert(c.floor_id==r.floor_id and type(c.x)=='number' and c.x%1==0 and type(c.y)=='number' and c.y%1==0
    and c.x>=1 and c.x<=map.width and c.y>=1 and c.y<=map.height,'invalid storm cell')
   local k=c.x..','..c.y;assert(not seen[k],'duplicate storm cell');seen[k]=true
  end
 end
end
return S
