-- Approved optional terrain: explicit world-time vents, legal shoves and finite
-- shortcuts. No renderer, simulation RNG, extra HP pipeline or new map alphabet.
local Content=require('src.content')
local RNG=require('src.rng')
local Scheduler=require('src.scheduler')
local Visibility=require('src.visibility')
local T={version=1,warning_time=100,cooldown_time=300,heat_damage=2,push_cap=4}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
local names={heat_vent='Heat Vent',push_floor='Push Floor',fragile_crossing='Fragile Shortcut'}
T.names=names
T.art={
 heat_vent_cooldown={'tiny',9,565},heat_vent_warning={'tiny',29,565},heat_vent_burst={'tiny',29,565},
 push_north={'onebit',1003,1003},push_east={'onebit',1004,1004},
 push_south={'onebit',1005,1005},push_west={'onebit',1006,1006},
 fragile_intact={'rpg',495,70},fragile_armed={'rpg',495,600},fragile_closed={'caves',94,600},
}
function T.binding(name)return 'core:terrain/'..name end
local function key(x,y)return x..','..y end
local function distance(a,b)return math.abs(a.x-b.x)+math.abs(a.y-b.y)end
local function protected(map,x,y)
 for _,c in ipairs(map.protected_cells or {})do if c.x==x and c.y==y then return true end end
 -- Primary corridor cells are also an explicit generation/save invariant,
 -- even when an alternate geometric route happens to exist.
 for _,corridor in ipairs(map.corridors or {})do for _,c in ipairs(corridor.cells)do
  if c.x==x and c.y==y then return true end
 end end
 return false
end
local function inside(r,x,y)return x>=r.x and x<r.x+r.width and y>=r.y and y<r.y+r.height end
function T.optional_cell(map,x,y)
 return map.rows[y] and map.rows[y]:sub(x,x)=='.' and not protected(map,x,y)
end
local function player(s)for _,e in ipairs(s.entities)do if e.id==s.player_id then return e end end end
local function map_for(s,floor)return floor==s.active_floor_id and Content.map_for(s) or s.inactive_floor_maps[floor]end
function T.find(s,id)
 for _,n in ipairs(s.idea_terrain and s.idea_terrain.nodes or {})do if n.id==id then return n end end
end
function T.at(s,floor,x,y)
 for _,n in ipairs(s.idea_terrain and s.idea_terrain.nodes or {})do
  if n.floor_id==floor and n.x==x and n.y==y then return n end
 end
end
function T.closed(s,floor,x,y)
 local n=T.at(s,floor,x,y);return n and n.kind=='fragile_crossing' and n.phase=='closed' or false
end
function T.place(s,map,kind,x,y,dx,dy)
 assert(names[kind] and Content.walkable(map,x,y) and not T.at(s,map.floor_id,x,y),'invalid special terrain placement')
 assert(kind~='fragile_crossing' or map==s.generated_map,'fragile crossing requires a concrete generated floor')
 assert(kind~='fragile_crossing' or not protected(map,x,y),'protected geometry cannot collapse')
 s.idea_terrain=s.idea_terrain or {version=T.version,nodes={}}
 local n={id=map.floor_id..':terrain:'..kind..':'..key(x,y),kind=kind,floor_id=map.floor_id,x=x,y=y,
  phase=kind=='heat_vent' and 'cooldown' or kind=='push_floor' and 'ready' or 'intact'}
 if kind=='push_floor'then
  assert(type(dx)=='number' and type(dy)=='number' and dx%1==0 and dy%1==0 and math.abs(dx)+math.abs(dy)==1,'invalid push direction')
  n.dx,n.dy=dx,dy
 elseif kind=='heat_vent'then n.cooldown_until=s.clock end
 s.idea_terrain.nodes[#s.idea_terrain.nodes+1]=n;return n
end
-- The entire currently reachable geometric component survives removing this
-- tile, including static props and every mandatory placement, not just the elite.
function T.alternate_route(s,map,x,y)
 local obstacles={}
 for _,e in ipairs(s.entities)do local p=e.position
  if p.floor_id==map.floor_id and e.blocks_movement and not e.hp and e.controller~='door'then obstacles[key(p.x,p.y)]=true end
 end
 local function flood(skip,blocked)
  local q={{x=map.start_x,y=map.start_y}};local seen={[key(map.start_x,map.start_y)]=true};local h=1
  while h<=#q do local p=q[h];h=h+1
   for _,d in ipairs(dirs)do local a,b=p.x+d[1],p.y+d[2];local k=key(a,b)
    if not seen[k] and not (skip and a==x and b==y) and Content.walkable(map,a,b)
     and not blocked[k]then seen[k]=true;q[#q+1]={x=a,y=b}end
   end
  end
  return seen
 end
 if x==map.start_x and y==map.start_y then return false end
 for _,blocked in ipairs({{},obstacles})do
  local before=flood(false,blocked);if not before[key(x,y)]then return false end
  local after=flood(true,blocked)
  for cell in pairs(before)do if cell~=key(x,y) and not after[cell]then return false end end
 end
 return true
end
function T.populate(s,map)
 if not map.rooms then return end
 local taken,choices={},{}
 local function reserve(x,y) taken[key(x,y)]=true end
 for _,e in ipairs(s.entities)do if e.position.floor_id==map.floor_id then reserve(e.position.x,e.position.y)end end
 for _,list in ipairs({s.review_features or {},s.idea_rooms and s.idea_rooms.nodes or {},
  map.placements.stairs or {},map.placements.secrets or {},map.placements.chests or {},map.placements.doors or {}})do
  for _,p in ipairs(list)do if not p.floor_id or p.floor_id==map.floor_id then
   reserve(p.x,p.y);for _,d in ipairs(dirs)do reserve(p.x+d[1],p.y+d[2])end
  end end
 end
 local seed=RNG.derive(s.run_seed,map.floor_id..':optional-terrain')
 for _,r in ipairs(map.rooms)do if r.role~='start' and r.role~='elite' and r.role~='exit'then
  for y=r.y+1,r.y+r.height-2 do for x=r.x+1,r.x+r.width-2 do
   if not taken[key(x,y)] and map.rows[y]:sub(x,x)=='.'
    and distance({x=x,y=y},{x=map.start_x,y=map.start_y})>=6 then
    choices[#choices+1]={x=x,y=y,room=r,score=RNG.derive(seed,key(x,y))}
   end
  end end
 end end
 table.sort(choices,function(a,b)if a.score~=b.score then return a.score<b.score end;if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
 local used={}
 local function choose(test)
  for _,p in ipairs(choices)do if not taken[key(p.x,p.y)] and not used[p.room.id] and test(p)then
   reserve(p.x,p.y);used[p.room.id]=true;return p
  end end
 end
 local heat=choose(function()return true end)
 if heat then T.place(s,map,'heat_vent',heat.x,heat.y)end
 local push=choose(function(p)
  for _,d in ipairs(dirs)do local x,y=p.x+d[1],p.y+d[2]
   if inside(p.room,x,y) and map.rows[y]:sub(x,x)=='.' and not taken[key(x,y)]then
    local clear=true
    for yy=math.min(p.y,y)-1,math.max(p.y,y)+1 do for xx=math.min(p.x,x)-1,math.max(p.x,x)+1 do
     if not inside(p.room,xx,yy) or map.rows[yy]:sub(xx,xx)~='.' or taken[key(xx,yy)]then clear=false end
    end end
    if clear and T.alternate_route(s,map,p.x,p.y)then p.dx,p.dy=d[1],d[2];return true end
   end
  end
 end)
 if push then T.place(s,map,'push_floor',push.x,push.y,push.dx,push.dy)end
 -- Only a procedurally editable shortcut may collapse. Authored protected
 -- room floors stay intact; prove both geometric and currently usable routes.
 local shortcuts={}
 for _,p in ipairs(map.editable_cells or {})do
  if not taken[key(p.x,p.y)] and not protected(map,p.x,p.y) and map.rows[p.y]:sub(p.x,p.x)=='.'
   and distance(p,{x=map.start_x,y=map.start_y})>=6 then
   local restricted=false
   for _,r in ipairs(map.rooms)do if inside(r,p.x,p.y) and (r.role=='start' or r.role=='elite' or r.role=='exit')then restricted=true end end
   if not restricted then shortcuts[#shortcuts+1]={x=p.x,y=p.y,score=RNG.derive(seed,'shortcut:'..key(p.x,p.y))}end
  end
 end
 table.sort(shortcuts,function(a,b)if a.score~=b.score then return a.score<b.score end;if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
 for index,p in ipairs(shortcuts)do
  if index>24 then break end
  if T.alternate_route(s,map,p.x,p.y)then T.place(s,map,'fragile_crossing',p.x,p.y);break end
 end
end
function T.on_event(s,event,seen,emit,push)
 if event.kind~='TileEntered'then return end
 local n=T.at(s,event.floor_id,event.x,event.y);if not n then return end
 local e;for _,v in ipairs(s.entities)do if v.id==event.source_id then e=v;break end end
 if not e or not e.hp or e.hp<=0 then return end
 if n.kind=='fragile_crossing' and n.phase=='intact'then
  n.phase='armed';emit('FragileArmed',{floor_id=n.floor_id,x=n.x,y=n.y,target_id=e.id},e.id)
 elseif n.kind=='push_floor' and e.position.floor_id==n.floor_id and e.position.x==n.x and e.position.y==n.y then
  local id='push:'..tostring(event.action_id)..':'..e.id;local count=seen[id] or 0
  if count>=T.push_cap then emit('PushFloorBlocked',{floor_id=n.floor_id,x=n.x,y=n.y,target_id=e.id,reason='chain limit'},0);return end
  seen[id]=count+1
  local okay,reason=push(e,n.dx,n.dy)
  emit(okay and 'PushFloorTriggered' or 'PushFloorBlocked',{floor_id=n.floor_id,x=n.x,y=n.y,target_id=e.id,reason=reason},0)
 end
end
function T.after_action(s,action_id,emit)
 for _,n in ipairs(s.idea_terrain and s.idea_terrain.nodes or {})do if n.floor_id==s.active_floor_id then
  if n.kind=='heat_vent' and n.phase=='cooldown' and s.clock>=n.cooldown_until and not s.game_over
   and Visibility.can_see(s,n.floor_id,n.x,n.y)then
   n.phase='warning';n.due=s.clock+T.warning_time;n.action_id=action_id
   local w=Scheduler.enqueue(s,{kind='effect',due=n.due,actor_id=0,attack_id=action_id,floor_id=n.floor_id})
   w.effect_id=w.id;w.idea_terrain=n.id;n.schedule_id=w.id
   emit('HeatVentWarned',{floor_id=n.floor_id,x=n.x,y=n.y,due=n.due,target_cells={{floor_id=n.floor_id,x=n.x,y=n.y}}},0)
  elseif n.kind=='fragile_crossing' and n.phase=='armed'then
   local occupied=false
   local function holds(p)return p and p.floor_id==n.floor_id and p.x==n.x and p.y==n.y end
   for _,e in ipairs(s.entities)do local p=e.position
    if holds(p) and (e.hp or e.blocks_movement or e.controller=='idea_companion')then occupied=true;break end
   end
   -- Live rewards, revival commitments and placed class objects must remain
   -- reachable/valid. Consumed reward positions are historical, not occupants.
   for _,d in ipairs(s.drops)do if not d.collected and not d.carried_by and holds(d.position)then occupied=true end end
   for _,c in ipairs(s.idea_corpses or {})do if holds(c)then occupied=true end end
   for _,c in ipairs(s.enemy_snares or {})do if holds(c)then occupied=true end end
   local special=s.class_special or {}
   if holds(special.beacon) or holds(special.decoy) or holds(special.snare)
    or holds(player(s).utility and player(s).utility.planted)then occupied=true end
   if not occupied then
    local map=map_for(s,n.floor_id);map.rows[n.y]=map.rows[n.y]:sub(1,n.x-1)..'~'..map.rows[n.y]:sub(n.x+1)
    if map.studio_visuals then map.studio_visuals[key(n.x,n.y)]=nil end
    n.phase='closed';emit('FragileCollapsed',{floor_id=n.floor_id,x=n.x,y=n.y},0)
   end
  end
 end end
end
function T.resolve(s,item,emit,damage)
 local n=assert(T.find(s,item.idea_terrain),'orphan heat vent')
 assert(n.phase=='warning' and n.schedule_id==item.id and n.due==s.clock and n.floor_id==s.active_floor_id,'mistimed heat vent')
 n.phase='cooldown';n.cooldown_until=s.clock+T.cooldown_time;n.schedule_id=nil;n.due=nil
 if Visibility.can_see(s,n.floor_id,n.x,n.y)then
  emit('HeatVentBurst',{floor_id=n.floor_id,x=n.x,y=n.y,target_cells={{floor_id=n.floor_id,x=n.x,y=n.y}}},0,n.action_id)
  local victims={}
  for _,e in ipairs(s.entities)do local p=e.position
   if e.hp and e.hp>0 and p.floor_id==n.floor_id and p.x==n.x and p.y==n.y then victims[#victims+1]=e end
  end
  table.sort(victims,function(a,b)return a.id<b.id end)
  for _,e in ipairs(victims)do damage(e,n.action_id,T.heat_damage)end
 end
 n.action_id=nil
end
function T.rebase(s,floor,delta)
 for _,n in ipairs(s.idea_terrain and s.idea_terrain.nodes or {})do if n.floor_id==floor and n.kind=='heat_vent'then
  n.cooldown_until=n.cooldown_until+delta;if n.due then n.due=n.due+delta end
 end end
end
function T.visual(n)
 if n.kind=='heat_vent'then return T.binding('heat_vent_'..n.phase)end
 if n.kind=='fragile_crossing'then return T.binding('fragile_'..n.phase)end
 return T.binding(n.dx==1 and 'push_east' or n.dx==-1 and 'push_west' or n.dy==1 and 'push_south' or 'push_north')
end
function T.options(s,id)
 local n=T.find(s,id);if not n then return end
 local p=player(s).position
 if p.floor_id~=n.floor_id or distance(p,n)>1 or not Visibility.can_see(s,n.floor_id,n.x,n.y)then return nil,'stand beside visible terrain'end
 local detail=n.kind=='heat_vent' and ('Phase: '..n.phase..(n.due and ('; impact at '..n.due) or '; warns only while visible')..'. Base 2 fire damage. Warning lasts 100 world time, cooldown 300. No guaranteed reaction action.')
  or n.kind=='push_floor' and 'Ending a move on this arrow pushes one legal tile. Walls, creatures, exits and stairs stop it. Maximum four pushes per actor/action; displacement interrupts windups.'
  or 'One-use shortcut. Crossing arms it; it becomes a gap only after everyone leaves. An alternate route remains. No collapse damage; no refill on Continue.'
 return {node_id=id,title=names[n.kind],detail=detail,entries={}}
end
function T.nearby(s)
 local out={};local p=player(s).position
 for _,n in ipairs(s.idea_terrain and s.idea_terrain.nodes or {})do
  if n.floor_id==p.floor_id and distance(p,n)<=1 and Visibility.can_see(s,n.floor_id,n.x,n.y)then
   out[#out+1]={node_id=n.id,title=names[n.kind]..' · '..n.phase,x=n.x,y=n.y,spent=n.phase=='closed'}
  end
 end
 return out
end
local function integer(n,lo,hi)return type(n)=='number' and n%1==0 and n>=lo and n<=hi end
function T.validate(s,maps)
 local store=s.idea_terrain
 if not store then
  for _,w in ipairs(s.schedule)do assert(not w.idea_terrain,'terrain schedule without store')end
  for _,pause in pairs(s.floor_pause or {})do for _,w in ipairs(pause.schedule)do assert(not w.idea_terrain,'paused terrain schedule without store')end end
  return true
 end
 assert(type(store)=='table' and store.version==T.version and type(store.nodes)=='table' and #store.nodes<=24,'invalid optional terrain store')
 local ids,positions,pending={},{},{}
 for _,n in ipairs(store.nodes)do
  local map=maps[n.floor_id]
  assert(map and names[n.kind] and integer(n.x,1,map.width) and integer(n.y,1,map.height)
   and n.id==n.floor_id..':terrain:'..n.kind..':'..key(n.x,n.y) and not ids[n.id],'invalid optional terrain identity')
  ids[n.id]=true;local k=n.floor_id..':'..key(n.x,n.y);assert(not positions[k],'overlapping optional terrain');positions[k]=true
  local tile=map.rows[n.y]:sub(n.x,n.x)
  if n.kind=='heat_vent'then
   assert(tile=='.' and (n.phase=='cooldown' or n.phase=='warning') and integer(n.cooldown_until,0,4503599627370496),'invalid heat vent')
   if n.phase=='warning'then
    local pause=s.floor_pause and s.floor_pause[n.floor_id];local clock=pause and pause.suspended_at or s.clock
    assert(integer(n.due,clock,clock+T.warning_time) and integer(n.schedule_id,1,s.next_schedule_id-1)
     and integer(n.action_id,1,s.next_action_id-1),'invalid heat warning')
    assert(not pending[n.schedule_id],'duplicate heat work');pending[n.schedule_id]=n
   else assert(n.due==nil and n.schedule_id==nil and n.action_id==nil,'unexpected heat work')end
  elseif n.kind=='push_floor'then
   assert(tile=='.' and n.phase=='ready' and integer(n.dx,-1,1) and integer(n.dy,-1,1) and math.abs(n.dx)+math.abs(n.dy)==1,'invalid push floor')
  else assert(not protected(map,n.x,n.y) and ((n.phase=='intact' or n.phase=='armed') and tile=='.' or n.phase=='closed' and tile=='~'),'invalid fragile crossing')end
 end
 local work={};for _,w in ipairs(s.schedule)do work[#work+1]=w end
 for _,pause in pairs(s.floor_pause or {})do for _,w in ipairs(pause.schedule)do work[#work+1]=w end end
 for _,w in ipairs(work)do if w.idea_terrain then
  local n=pending[w.id];assert(n and n.id==w.idea_terrain and w.kind=='effect' and w.actor_id==0
   and w.effect_id==w.id and w.due==n.due and w.attack_id==n.action_id and w.floor_id==n.floor_id,'orphan heat schedule')
  pending[w.id]=nil
 end end
 assert(not next(pending),'missing heat schedule');return true
end
return T
