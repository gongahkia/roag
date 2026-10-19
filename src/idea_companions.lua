-- Explicitly commanded friendly objects. They are not AI actors, observers,
-- damageable reward sources or autonomous free attacks.
local Scheduler=require('src.scheduler')
local C={spirit_id='core:actor/spiritbinder',controller='idea_companion',
 spirit_object='core:object/bound_spirit'}
C.symbols={[C.spirit_object]=417}
C.tinker_id='core:actor/tinkerer';C.sentry_object='core:object/bolt_sentry'
C.tinker={id=C.tinker_id,name='Tinkerer',description='Hand bolt, one three-charge sentry, utility roll',
 hp=23,damage=3,slash_damage=0,defense=0,speed=95,crit_chance_ppm=0,crit_damage_percent=150,
 costs={move=100,basic_attack=100,sweeping_slash=160,dash=120},dash_distance=2,
 basic_id='core:ability/tinkerer_bolt',special_id='core:ability/tinkerer_deploy',
 basic_name='Hand Bolt',special_name='Deploy Sentry',dash_name='Utility Roll',
 ui_controls='F Bolt · R Sentry (3 bolts) · X Roll'}
C.spirit={id=C.spirit_id,name='Spiritbinder',description='Tether bolt, one commanded spirit, short spirit step',
 hp=20,damage=3,slash_damage=3,defense=0,speed=100,crit_chance_ppm=0,crit_damage_percent=150,
 costs={move=100,basic_attack=110,sweeping_slash=140,dash=120},dash_distance=2,
 basic_id='core:ability/spiritbinder_bolt',special_id='core:ability/spiritbinder_command',
 basic_name='Tether Bolt',special_name='Command Spirit',dash_name='Spirit Step',
 ui_controls='F Bolt · R Spirit / command · X Step'}
function C.owned(state,kind)
 for _,e in ipairs(state.entities)do if e.controller==C.controller and e.companion_kind==kind then return e end end
end
local function body(state,floor,x,y)
 for _,e in ipairs(state.entities)do local p=e.position
  if p.floor_id==floor and p.x==x and p.y==y and (e.hp or e.blocks_movement)then return e end
 end
end
function C.cells(state,owner,dx,dy,content)
 local p=owner.position;local map=content.map_for(state);local last
 if owner.definition_id==C.tinker_id then return {{x=p.x+dx,y=p.y+dy}}end
 for step=1,3 do
  local x,y=p.x+dx*step,p.y+dy*step
  if dx~=0 and dy~=0 and (not content.projectile_passable(map,x-dx,y,state)
   or not content.projectile_passable(map,x,y-dy,state))then break end
  if not content.projectile_passable(map,x,y,state)then break end
  local hit=body(state,p.floor_id,x,y)
  if hit then
   if not C.owned(state,'spirit')then return last and {last} or {}end
   return {{x=x,y=y}}
  end
  if step<=2 then last={x=x,y=y}end
 end
 return last and {last} or {}
end
function C.preview(state,owner,cells,content,visible)
 local c=cells[1];local p=owner.position;local map=content.map_for(state)
 if not c or not visible(state,p.floor_id,c.x,c.y)then return false,'choose visible nearby ground'end
 local hit=body(state,p.floor_id,c.x,c.y)
 if owner.definition_id==C.tinker_id and (hit or math.abs(c.x-p.x)+math.abs(c.y-p.y)~=1)then
  return false,'sentry needs empty adjacent cardinal floor'
 end
 if hit then
  local spirit=C.owned(state,'spirit')
  if hit.faction~='enemy' or not hit.hp or not spirit or spirit.position.floor_id~=p.floor_id
   or math.max(math.abs(c.x-spirit.position.x),math.abs(c.y-spirit.position.y))~=1 then
   return false,'place spirit beside the target first'
  end
  local q=spirit.position
  if q.x~=c.x and q.y~=c.y and (not content.projectile_passable(map,q.x,c.y,state)
   or not content.projectile_passable(map,c.x,q.y,state))then return false,'spirit strike corner blocked'end
  return true,nil,'strike',spirit
 end
 if not content.walkable(map,c.x,c.y) or map.rows[c.y]:sub(c.x,c.x)~='.'then return false,'needs plain floor'end
 for _,stair in ipairs(map.placements and map.placements.stairs or {})do
  if stair.x==c.x and stair.y==c.y then return false,'stairs must stay clear'end
 end
 return true,nil,'place'
end
function C.place(state,owner,c,emit)
 local e=C.owned(state,'spirit')
 if not e then
  e={id=state.next_entity_id,definition_id=C.spirit_object,controller=C.controller,
   companion_kind='spirit',owner_id=owner.id,faction='player',boons={},
   blocks_movement=false,blocks_vision=false,blocks_projectiles=false}
  state.next_entity_id=state.next_entity_id+1;state.entities[#state.entities+1]=e
 end
 e.position={floor_id=owner.position.floor_id,x=c.x,y=c.y}
 emit('SpiritPositioned',{target_id=e.id,floor_id=e.position.floor_id,x=c.x,y=c.y})
 return e
end
local function schedule(state,e,phase,due,action_id)
 local item=Scheduler.enqueue(state,{kind='effect',due=due,actor_id=0,attack_id=action_id,floor_id=e.position.floor_id})
 item.effect_id=item.id;item.idea_sentry=true
 e.sentry_phase,e.sentry_due,e.sentry_schedule_id=phase,due,item.id
end
function C.deploy(state,owner,c,action_id,emit)
 local old=C.owned(state,'sentry')
 if old then
  for _,list in ipairs({state.schedule})do for i=#list,1,-1 do if list[i].id==old.sentry_schedule_id then table.remove(list,i)end end end
  for _,pause in pairs(state.floor_pause or {})do for i=#pause.schedule,1,-1 do
   if pause.schedule[i].id==old.sentry_schedule_id then table.remove(pause.schedule,i)end end end
  for i,e in ipairs(state.entities)do if e.id==old.id then table.remove(state.entities,i);break end end
 end
 local e={id=state.next_entity_id,definition_id=C.sentry_object,controller=C.controller,
  companion_kind='sentry',owner_id=owner.id,faction='player',boons={},
  blocks_movement=false,blocks_vision=false,blocks_projectiles=false,
  position={floor_id=owner.position.floor_id,x=c.x,y=c.y},sentry_shots=3}
 state.next_entity_id=e.id+1;state.entities[#state.entities+1]=e
 schedule(state,e,'commit',state.clock+100,action_id)
 emit('SentryDeployed',{target_id=e.id,floor_id=e.position.floor_id,x=c.x,y=c.y,shots=3})
 return e
end
function C.resolve(state,item,content,visible,emit,fire)
 local e=C.owned(state,'sentry');assert(e and e.sentry_schedule_id==item.id,'orphan sentry work')
 assert(e.position.floor_id==state.active_floor_id and e.sentry_due==state.clock,'mistimed sentry work')
 if e.sentry_phase=='impact'then
  local cells=e.sentry_cells
  if visible(state,e.position.floor_id,e.position.x,e.position.y)then
   -- A locked line never tracks a moving target. Ordinary projectile collision
   -- and friendly fire are applied by the existing damage batch.
   local shown=true;for _,c in ipairs(cells)do if not visible(state,c.floor_id,c.x,c.y)then shown=false;break end end
   if shown then fire(e,cells,item.attack_id)end
  end
  e.sentry_cells=nil
  if e.sentry_shots>0 then schedule(state,e,'commit',state.clock+120,item.attack_id)
  else e.sentry_phase=nil;e.sentry_due=nil;e.sentry_schedule_id=nil end
 else
  e.sentry_shots=e.sentry_shots-1
  local best,dist
  if visible(state,e.position.floor_id,e.position.x,e.position.y)then
   for _,a in ipairs(state.entities)do local p=a.position;local q=e.position
    local d=math.abs(p.x-q.x)+math.abs(p.y-q.y)
    if a.controller=='ai' and a.hp>0 and not a.invulnerable and p.floor_id==q.floor_id
     and (p.x==q.x or p.y==q.y) and d>=1 and d<=4 and visible(state,p.floor_id,p.x,p.y)then
     local dx=p.x==q.x and 0 or (p.x>q.x and 1 or -1);local dy=p.y==q.y and 0 or (p.y>q.y and 1 or -1)
     local clear=true
     for step=1,d do if not content.projectile_passable(content.map_for(state),q.x+dx*step,q.y+dy*step,state)then clear=false;break end end
     if clear and (not best or d<dist or d==dist and a.id<best.id)then best,dist=a,d end
    end
   end
  end
  if best then
   local q,p=e.position,best.position
   local dx=p.x==q.x and 0 or (p.x>q.x and 1 or -1);local dy=p.y==q.y and 0 or (p.y>q.y and 1 or -1)
   e.sentry_cells={}
   for step=1,dist do e.sentry_cells[#e.sentry_cells+1]={floor_id=q.floor_id,x=q.x+dx*step,y=q.y+dy*step}end
   schedule(state,e,'impact',state.clock+80,item.attack_id)
   emit('SentryCommitted',{target_id=best.id,floor_id=q.floor_id,x=q.x,y=q.y,
    target_cells=e.sentry_cells,due=e.sentry_due,shots=e.sentry_shots})
  elseif e.sentry_shots>0 then
   emit('SentryChargeSpent',{floor_id=e.position.floor_id,x=e.position.x,y=e.position.y,shots=e.sentry_shots})
   schedule(state,e,'commit',state.clock+200,item.attack_id)
  else e.sentry_phase=nil;e.sentry_due=nil;e.sentry_schedule_id=nil end
 end
end
function C.validate(state,maps)
 local seen,work={},{}
 local scheduled={};for _,w in ipairs(state.schedule)do scheduled[#scheduled+1]=w end
 for _,pause in pairs(state.floor_pause or {})do for _,w in ipairs(pause.schedule)do scheduled[#scheduled+1]=w end end
 for _,e in ipairs(state.entities)do if e.controller==C.controller then
  local owner;for _,p in ipairs(state.entities)do if p.id==e.owner_id then owner=p;break end end
  local kind=e.companion_kind;local spirit=kind=='spirit';local sentry=kind=='sentry'
  assert((spirit or sentry) and not seen[kind] and owner and owner.id==state.player_id
   and owner.definition_id==(spirit and C.spirit_id or C.tinker_id)
   and e.definition_id==(spirit and C.spirit_object or C.sentry_object) and e.hp==nil
   and e.faction=='player' and e.blocks_movement==false and e.blocks_vision==false
   and e.blocks_projectiles==false,'invalid owned companion')
  local map=maps[e.position.floor_id]
  assert(map and map.rows[e.position.y] and map.rows[e.position.y]:sub(e.position.x,e.position.x)=='.','invalid companion ground')
  if sentry then
   assert(type(e.sentry_shots)=='number' and e.sentry_shots%1==0 and e.sentry_shots>=0 and e.sentry_shots<=3,'invalid sentry charges')
   if e.sentry_phase then
    assert(e.sentry_phase=='commit' and e.sentry_shots>0 or e.sentry_phase=='impact','invalid sentry phase')
    local found=false;local clock=((state.floor_pause or {})[e.position.floor_id]or{}).suspended_at or state.clock
    assert(type(e.sentry_due)=='number' and e.sentry_due>=clock and e.sentry_due<=clock+200,'invalid sentry due')
    for _,w in ipairs(scheduled)do if w.id==e.sentry_schedule_id and w.kind=='effect' and w.idea_sentry==true
     and w.actor_id==0 and w.effect_id==w.id and w.floor_id==e.position.floor_id and w.due==e.sentry_due then found=true end end
    assert(found,'missing sentry work');work[e.sentry_schedule_id]=true
    if e.sentry_phase=='impact'then
     assert(type(e.sentry_cells)=='table' and #e.sentry_cells>=1 and #e.sentry_cells<=4,'invalid sentry lane')
     local dx,dy
     for i,c in ipairs(e.sentry_cells)do
      local x,y=c.x-e.position.x,c.y-e.position.y
      if i==1 then dx,dy=x,y;assert(math.abs(dx)+math.abs(dy)==1,'invalid sentry direction')end
      assert(c.floor_id==e.position.floor_id and x==dx*i and y==dy*i,'unlocked sentry lane')
     end
    else assert(e.sentry_cells==nil,'uncommitted sentry lane')end
   else assert(e.sentry_shots==0 and not e.sentry_due and not e.sentry_schedule_id and not e.sentry_cells,'invalid spent sentry')end
  end
  seen[kind]=true
 end end
 for _,w in ipairs(scheduled)do if w.idea_sentry then assert(w.idea_sentry==true and work[w.id],'orphan sentry work')end end
end
return C
