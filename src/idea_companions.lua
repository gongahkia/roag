-- Explicitly commanded friendly objects. They are not AI actors, observers,
-- damageable reward sources or autonomous free attacks.
local C={spirit_id='core:actor/spiritbinder',controller='idea_companion',
 spirit_object='core:object/bound_spirit'}
C.symbols={[C.spirit_object]=417}
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
function C.validate(state,maps)
 local seen={}
 for _,e in ipairs(state.entities)do if e.controller==C.controller then
  local owner;for _,p in ipairs(state.entities)do if p.id==e.owner_id then owner=p;break end end
  assert(e.companion_kind=='spirit' and not seen.spirit and owner and owner.id==state.player_id
   and owner.definition_id==C.spirit_id and e.definition_id==C.spirit_object and e.hp==nil
   and e.faction=='player' and e.blocks_movement==false and e.blocks_vision==false
   and e.blocks_projectiles==false,'invalid bound spirit')
  local map=maps[e.position.floor_id]
  assert(map and map.rows[e.position.y] and map.rows[e.position.y]:sub(e.position.x,e.position.x)=='.',
   'invalid spirit ground')
  seen.spirit=true
 end end
end
return C
