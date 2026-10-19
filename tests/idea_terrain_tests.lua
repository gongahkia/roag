return function(test,equal)
 local G=require('src.game');local T=require('src.idea_terrain');local C=require('src.content')
 local V=require('src.visibility');local F=require('src.floors');local P=require('src.persistence');local Q=require('src.scheduler')
 local function submit(s,intent)local ok,why,events=G.submit(s,intent);assert(ok,why);return events end
 local function count(events,kind)local n=0;for _,e in ipairs(events)do if e.kind==kind then n=n+1 end end;return n end
 local function plain(kind,x,y,dx,dy)
  local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6;V.recompute(s)
  return s,p,T.place(s,C.map_for(s),kind,x or 8,y or 6,dx,dy)
 end
 local function room(class)
  local s=G.new_generated(37,{class_id=class});s.meta.eligible=false
  for i=#s.entities,1,-1 do local e=s.entities[i];if e.controller=='ai'then Q.remove_for_actor(s,e.id);table.remove(s.entities,i)end end
  s.idea_terrain={version=1,nodes={}};local map=C.map_for(s);local p=G.player(s)
  local protected={};for _,c in ipairs(map.protected_cells)do protected[c.x..','..c.y]=true end
  for _,c in ipairs(map.editable_cells)do
   if T.optional_cell(map,c.x,c.y)
    and C.walkable(map,c.x-1,c.y) and C.walkable(map,c.x+1,c.y) and T.alternate_route(s,map,c.x,c.y)then
    local okay=true
    for _,e in ipairs(s.entities)do if e.id~=p.id and e.blocks_movement and e.position.y==c.y and math.abs(e.position.x-c.x)<=1 then okay=false end end
    if okay then p.position.x,p.position.y=c.x-1,c.y;V.recompute(s);return s,p,map end
   end
  end;error('no clear prepared shortcut')
 end
 test('terrain inspection, idle previews and rendering time do not advance clocks or consume RNG',function()
  local s,p,n=plain('heat_vent');local before=G.snapshot(s)
  for _=1,20 do assert(#G.interaction_options(s,n.id).entries==0);assert(#G.nearby_interactions(s)>=1);G.preview(s,'move',1,0)end
  assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='trigger'}))
  equal(G.snapshot(s),before);assert(n.phase=='cooldown' and not n.schedule_id)
 end)
 test('visible vent locks a world-time warning and impact wins its tie with the next decision',function()
  local s,p,n=plain('heat_vent');local first=submit(s,{kind='wait'})
  assert(n.phase=='warning' and n.due==200 and s.clock==100 and count(first,'HeatVentWarned')==1)
  local strikes=G.pending_strikes(s);assert(#strikes==1 and strikes[1].terrain_heat and strikes[1].due==200)
  local hp=p.hp;local events=submit(s,{kind='move',dx=1,dy=0})
  assert(p.hp==hp-2 and s.clock==200 and n.phase=='cooldown' and n.cooldown_until==500)
  assert(count(events,'HeatVentBurst')==1 and count(events,'DamageTaken')==1 and G.restore(G.snapshot(s)))
 end)
 test('leaving a locked vent avoids its impact without retargeting or inventing a reaction turn',function()
  local s,p,n=plain('heat_vent',7,6);submit(s,{kind='wait'});local hp=p.hp
  local events=submit(s,{kind='move',dx=-1,dy=0});assert(p.hp==hp and n.phase=='cooldown' and count(events,'DamageTaken')==0)
  for _,e in ipairs(events)do if e.kind=='HeatVentBurst'then assert(e.x==7 and e.y==6 and e.time==200)end end
 end)
 test('unseen vents never arm and cooldowns require committed visible-world actions',function()
  local s,p,n=plain('heat_vent',11,6);V.recompute(s,1)
  submit(s,{kind='wait'});assert(n.phase=='cooldown' and not n.schedule_id)
  V.recompute(s,8);submit(s,{kind='wait'});assert(n.phase=='warning' and n.due==300)
  p.position.x,p.position.y=2,2;V.recompute(s,1)
  local events=submit(s,{kind='wait'});assert(count(events,'HeatVentBurst')==0 and n.phase=='cooldown')
 end)
 test('vent fire uses ordinary damage, ward, death and enemy friendly-fire handling',function()
  local s,p,n=plain('heat_vent',7,6);p.pickup_ward_actions=3
  submit(s,{kind='wait'});local hp=p.hp;local events=submit(s,{kind='wait'})
  assert(p.hp==hp and count(events,'WardFlaskBlocked')==1 and p.pickup_ward_actions==0)
  local a,b,c=plain('heat_vent');local enemy=G.spawn_enemy(a,'core:enemy/mossbound_guard',8,6,100000)
  submit(a,{kind='wait'});local old=enemy.hp;events=submit(a,{kind='wait'})
  assert(enemy.hp==old-math.max(0,2-enemy.defense) and count(events,'DamageTaken')==1)
 end)
 test('a pending heat impact and cooldown pause/rebase with their exact floor time',function()
  local s,p,n=plain('heat_vent');submit(s,{kind='wait'});F.park(s,s.active_floor_id)
  s.clock=s.clock+1000;F.resume(s,s.active_floor_id);assert(n.due==1200 and n.cooldown_until==1000)
  local b=G.restore(P.decode(P.encode(G.snapshot(s))));equal(submit(s,{kind='wait'}),submit(b,{kind='wait'}));equal(G.snapshot(s),G.snapshot(b))
  assert(n.cooldown_until==1500)
 end)
 test('an entered arrow performs one legal push with normal cost, pickups and motion events',function()
  local s,p,n=plain('push_floor',8,6,1,0);local events=submit(s,{kind='move',dx=1,dy=0})
  assert(p.position.x==9 and p.position.y==6 and s.clock==100)
  assert(count(events,'PushFloorTriggered')==1 and count(events,'Displaced')==1 and count(events,'TileEntered')==2)
  assert(G.restore(G.snapshot(s)))
 end)
 test('push floors stop at creatures instead of bypassing collision or consuming another action',function()
  local s,p,n=plain('push_floor',8,6,1,0);G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
  local events=submit(s,{kind='move',dx=1,dy=0});assert(p.position.x==8 and s.clock==100)
  assert(count(events,'PushFloorBlocked')==1 and count(events,'Displaced')==0)
 end)
 test('cyclic arrows terminate after four pushes without truncating ordinary gameplay work',function()
  local s,p,n=plain('push_floor',8,6,1,0);T.place(s,C.map_for(s),'push_floor',9,6,-1,0)
  local events=submit(s,{kind='move',dx=1,dy=0})
  assert(count(events,'PushFloorTriggered')==4 and count(events,'PushFloorBlocked')==1 and s.clock==100 and G.restore(G.snapshot(s)))
 end)
 test('enemy movement triggers the same neutral push and preserves strict scheduler ordering',function()
  local s,p,n=plain('push_floor',8,6,0,-1);p.position.x=6;V.recompute(s)
  local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,0)
  local events=submit(s,{kind='wait'})
  assert(e.position.x==8 and e.position.y==5 and count(events,'PushFloorTriggered')==1)
  assert(s.clock==100 and G.restore(G.snapshot(s)))
 end)
 test('Dash crossing an intermediate arrow does not retroactively move the landing actor',function()
  local s,p,n=plain('push_floor',8,6,0,-1);local events=submit(s,{kind='dash',dx=1,dy=0})
  assert(p.position.x==9 and p.position.y==6 and count(events,'PushFloorTriggered')==0)
 end)
 test('fragile shortcut arms on entry, holds an occupant and permanently closes only after departure',function()
  local s,p,map=room();local n=T.place(s,map,'fragile_crossing',p.position.x+1,p.position.y)
  assert(T.alternate_route(s,map,n.x,n.y));submit(s,{kind='move',dx=1,dy=0})
  assert(n.phase=='armed' and C.walkable(map,n.x,n.y));local hp=p.hp
  submit(s,{kind='wait'});assert(n.phase=='armed' and p.hp==hp)
  local events=submit(s,{kind='move',dx=1,dy=0})
  assert(n.phase=='closed' and not C.walkable(map,n.x,n.y) and map.rows[n.y]:sub(n.x,n.x)=='~' and p.hp==hp)
  assert(count(events,'FragileCollapsed')==1 and G.restore(G.snapshot(s)))
  local b=G.restore(G.snapshot(s));equal(G.snapshot(b),G.snapshot(s));assert(T.find(b,n.id).phase=='closed')
 end)
 test('Dash can cross a finite fragile shortcut and no occupied cell suffers collapse damage',function()
  local s,p,map=room();local n=T.place(s,map,'fragile_crossing',p.position.x+1,p.position.y);local hp=p.hp
  local events=submit(s,{kind='dash',dx=1,dy=0})
  assert(n.phase=='closed' and p.hp==hp and count(events,'FragileArmed')==1 and count(events,'FragileCollapsed')==1)
 end)
 test('a collected reward retains its valid historical position after its shortcut becomes a gap',function()
  local s,p,map=room();local n=T.place(s,map,'fragile_crossing',p.position.x+1,p.position.y)
  require('src.rewards').spawn_currency(s,1,map.floor_id,n.x,n.y,function()end,p.id)
  submit(s,{kind='move',dx=1,dy=0});assert(s.drops[1].collected)
  submit(s,{kind='move',dx=1,dy=0});assert(n.phase=='closed' and G.restore(G.snapshot(s)))
 end)
 test('an uncollected reward holds a vacated shortcut until it can be collected safely',function()
  local s,p,map=room();local n=T.place(s,map,'fragile_crossing',p.position.x+1,p.position.y)
  require('src.rewards').spawn_currency(s,1,map.floor_id,n.x,n.y,function()end,p.id)
  n.phase='armed';submit(s,{kind='wait'});assert(n.phase=='armed' and C.walkable(map,n.x,n.y))
  submit(s,{kind='move',dx=1,dy=0});submit(s,{kind='move',dx=1,dy=0})
  assert(n.phase=='closed' and G.restore(G.snapshot(s)))
 end)
 test('a placed class snare holds a shortcut until its ordinary replacement releases it',function()
  local s,p,map=room('core:actor/trapper');local n=T.place(s,map,'fragile_crossing',p.position.x+1,p.position.y)
  submit(s,{kind='sweeping_slash',dx=1,dy=0});assert(s.class_special.snare)
  submit(s,{kind='move',dx=1,dy=0});submit(s,{kind='move',dx=1,dy=0})
  assert(n.phase=='armed' and G.restore(G.snapshot(s)))
  local replaced=false
  for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
   local view=G.preview(s,'sweeping_slash',d[1],d[2])
   if view.valid and (p.position.x+d[1]~=n.x or p.position.y+d[2]~=n.y)then
    submit(s,{kind='sweeping_slash',dx=d[1],dy=d[2]});replaced=true;break
   end
  end
  assert(replaced and n.phase=='closed' and G.restore(G.snapshot(s)))
 end)
 test('malformed directions, phases, duplicate IDs, changed holes and mismatched heat work reject on restore',function()
  local makers={
   function()local s,p,n=plain('push_floor',8,6,1,0);n.dx=2;return s end,
   function()local s,p,n=plain('heat_vent');n.phase='fire';return s end,
   function()local s,p,n=plain('heat_vent');s.idea_terrain.nodes[2]=n;return s end,
   function()local s,p,n=plain('heat_vent');submit(s,{kind='wait'});n.due=n.due+1;return s end,
   function()local s,p,n=plain('heat_vent');s.idea_terrain.version=99;return s end,
   function()local s,p,n=plain('heat_vent');submit(s,{kind='wait'});s.idea_terrain=nil;return s end,
  }
  for _,make in ipairs(makers)do assert(not pcall(G.restore,G.snapshot(make())))end
  local s=G.new_stage(1,{multifloor=true});s.idea_terrain=nil
  equal(G.snapshot(G.restore(G.snapshot(s))),G.snapshot(s))
 end)
 test('optional terrain placement changes no combat RNG, entity, scheduler or geometry bytes',function()
  local s=G.new_stage(3,{multifloor=true});local map=C.map_for(s);s.idea_terrain=nil
  local before=P.encode({rng=s.rng,entities=s.entities,schedule=s.schedule,map=map,clock=s.clock})
  T.populate(s,map)
  assert(P.encode({rng=s.rng,entities=s.entities,schedule=s.schedule,map=map,clock=s.clock})==before)
 end)
 test('seven-biome seed corpus preserves all routes after every finite shortcut and exact reopening',function()
  local B=require('src.biomes');local seen={}
  for _,id in ipairs(B.order)do for seed=1,4 do
   local first=id==B.order[1];local options={multifloor=true,stage_options={biome_id=id,index=first and 1 or 2,
    route_history=first and {} or {(id:gsub('^core:biome/','core:route/'))}}}
   local s=G.new_stage(seed,options);local budgets={}
   for _,n in ipairs(s.idea_terrain.nodes)do
    seen[n.kind]=true;budgets[n.floor_id]=budgets[n.floor_id]or {};assert(not budgets[n.floor_id][n.kind]);budgets[n.floor_id][n.kind]=true
    local map=F.map(s,n.floor_id)
    if n.kind=='fragile_crossing'then assert(T.alternate_route(s,map,n.x,n.y))end
    if n.kind=='push_floor'then assert(T.alternate_route(s,map,n.x,n.y))end
    for _,r in ipairs(map.rooms)do
     if n.x>=r.x and n.x<r.x+r.width and n.y>=r.y and n.y<r.y+r.height then assert(r.role~='start' and r.role~='elite' and r.role~='exit')end
    end
   end
   equal(G.snapshot(G.restore(G.snapshot(s))),G.snapshot(s));equal(G.snapshot(G.new_stage(seed,options)),G.snapshot(s))
  end end
  assert(seen.heat_vent and seen.push_floor and seen.fragile_crossing)
 end)
 test('the fixed renderer resolves all new terrain and the permitted vent derivative preserves neutral pixels and alpha',function()
  local Audit=require('src.sprite_audit');local report=Audit.collect()
  for _,pack in pairs(report.packs)do assert(#pack.missing==0)end
  for _,color in ipairs({{1,0,0,.5},{.2,.3,.8,1},{0,0,0,0},{1,1,1,.2}})do
   local r,g,b,a=require('src.visual_monochrome').pixel(unpack(color));assert(r==g and g==b and a==color[4] and (r==.07 or r==.9))
  end
  assert(report.packs.onebit.usage.tiny['9'] and report.packs.onebit.usage.tiny['29'])
  local O=require('src.onebit_visuals');local called={}
  local function read(path)local f=io.open(path,'rb');if f then local b=f:read('*a');f:close();return b end end
  local function image(raw,path,mode)
   called[path]=mode
   local function size(at)local a,b,c,d=raw:byte(at,at+3);return ((a*256+b)*256+c)*256+d end
   return {getWidth=function()return size(17)end,getHeight=function()return size(21)end}
  end
  local function quad(x,y,w,h)return {x=x,y=y,w=w,h=h}end
  local full=O.new(read,image,quad)
  local fallback=O.new(function(path)if path==O.path then return read(path)end end,image,quad)
  assert(full:resolve(T.binding('heat_vent_warning')).approved_sheet=='tiny')
  assert(called['assets/kenney/approved/tiny-dungeon.png']=='approved-monochrome')
  assert(fallback:resolve(T.binding('heat_vent_warning')).symbol_index==565 and fallback.signature~=full.signature)
 end)
end
