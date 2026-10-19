return function(test,equal)
 local E=require('src.idea_environment');local D=E.content;local U=require('src.idea_presentation')
 local G=require('src.game');local P=require('src.persistence');local N=require('src.kenney_terrain')
 local function read(path)local f=io.open(path,'rb');if f then local b=f:read('*a');f:close();return b end end
 local function image(raw)
  local function size(at)local a,b,c,d=raw:byte(at,at+3);return ((a*256+b)*256+c)*256+d end
  return {getWidth=function()return size(17)end,getHeight=function()return size(21)end}
 end
 local function quad(x,y,w,h,pw,ph)return {x=x,y=y,w=w,h=h,pw=pw,ph=ph}end
 local function resolver(style,reader,factory)return E.new(style,reader or read,factory or image,quad)end
 local function copy(v)return P.decode(P.encode(v))end
 test('approved room materials and foreground art have distinct stable IDs and bounded native regions',function()
  local ids={}
  for _,name in ipairs(D.material_order)do local id=D.material_id(name);assert(not ids[id]);ids[id]=true
   for _,style in ipairs({'kenney','onebit'})do local m=E.material(name,style);local source=D.sources[m.sheet]
    for _,index in ipairs(m.grid or {m.center})do assert(index>=0 and index%source.columns*source.stride+16<=source.width and math.floor(index/source.columns)*source.stride+16<=source.height)end
   end
  end
  for name,p in pairs(D.props)do assert(not ids[D.prop_id(name)]);ids[D.prop_id(name)]=true;assert(D.sources[p[1]])end
  assert(D.materials.pale.mixed.center==8 and D.props.grave_a[2]==564)
 end)
 test('crypt and pale sanctuary are one existing-room family with memorial context and separate prop roles',function()
  local s=G.new_stage(1,{multifloor=true});local m=s.generated_map;local r=m.rooms[2]
  local context={idea_rooms={nodes={{kind='memorial',floor_id=m.floor_id,x=r.x+1,y=r.y+1}}}}
  local plan=E.plan(context,m);local found
  for _,e in ipairs(plan.rooms)do if e.room.id==r.id then found=e end end
  assert(found.family=='crypt_sanctuary' and found.floor=='pale' and found.wall=='ruin_carved')
  assert(#m.rooms==#plan.rooms and #require('src.biomes').order==7)
 end)
 test('all approved room families and contextual dressing appear across genuine seven-biome floors',function()
  local B=require('src.biomes');local seen,props={},{}
  for _,id in ipairs(B.order)do for seed=1,5 do
   local first=id==B.order[1];local s=G.new_stage(seed,{multifloor=true,stage_options={biome_id=id,index=first and 1 or 2,
    route_history=first and {} or {(id:gsub('^core:biome/','core:route/'))}}})
   local maps={s.generated_map};for _,m in pairs(s.inactive_floor_maps)do maps[#maps+1]=m end
   for _,m in ipairs(maps)do local plan=E.plan(s,m);assert(#plan.props<=#m.rooms*2)
    for _,e in ipairs(plan.rooms)do seen[e.family]=true end
    for _,p in ipairs(plan.props)do assert(m.rows[p.y]:sub(p.x,p.x)=='#');props[p.id]=true end
   end
  end end
  for name in pairs(D.families)do assert(seen[name],'unseen family '..name)end
  for _,name in ipairs({'camp_table','camp_seat','workshop_supplies','shelf_full','shelf_empty','chapel_window','prison_bars','rock_a','bone_a'})do assert(props[D.prop_id(name)],'unseen dressing '..name)end
 end)
 test('room planning is deterministic and cannot change geometry, entity state, RNG or profile eligibility',function()
  local s=G.new_stage(9,{multifloor=true});local before=G.snapshot(s)
  local first=E.plan(s,s.generated_map);equal(first,E.plan(s,s.generated_map));equal(G.snapshot(s),before)
 end)
 test('source-preserving room decoration avoids secret walls and authored visual overrides',function()
  local s=G.new_stage(3);local map=s.generated_map;local a=E.plan(s,map);assert(#a.props>0)
  local p=a.props[1];local context=copy(map);context.studio_visuals=context.studio_visuals or {};context.studio_visuals[p.x..','..p.y]='studio:reserved'
  for _,v in ipairs(E.plan(s,context).props)do assert(v.x~=p.x or v.y~=p.y)end
  for _,v in ipairs(a.props)do for _,secret in ipairs(map.placements.secrets)do assert(v.x~=secret.wall_x or v.y~=secret.wall_y)end end
 end)
 test('all 256 neighbor cases resolve canonical connected environment walls and hide unknown connections',function()
  local env=resolver('kenney');local neighbors={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},{1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
  for mask=0,255 do
   local cells={{'.','.','.'},{'.','#','.'},{'.','.','.'}}
   for _,n in ipairs(neighbors)do if math.floor(mask/n[3])%2==1 then cells[n[2]+2][n[1]+2]='#'end end
   local map={rows={},seed=1,floor_id='fixture',rooms={{id=1,x=1,y=1,width=3,height=3,role='landmark'}},placements={}}
   for i,row in ipairs(cells)do map.rows[i]=table.concat(row)end
   env:set_world({generated_map=map,active_floor_id=map.floor_id,next_event_id=1})
   local h=assert(env:resolve(map,2,2,'wall'));assert(h.mask==N.canonical(mask))
   assert(env:resolve(map,2,2,'wall',function()return false end).mask==255)
  end
 end)
 test('derived material layouts are deterministic, bounded and nonoverlapping across cached pages',function()
  for _,style in ipairs({'kenney','onebit'})do for sheet,meta in pairs(D.sources)do
   local list=E.surfaces(sheet,style);equal(list,E.surfaces(sheet,style));local seen={}
   for _,e in ipairs(list)do local x,y=e.slot%math.floor(meta.width/16)*16,math.floor(e.slot/math.floor(meta.width/16))*16
    assert(not seen[e.slot] and x+16<=meta.width and y+16<=meta.height);seen[e.slot]=true
   end
  end end
 end)
 local function sampled(sheet,style)
  local meta=D.sources[sheet];local written={};local source={}
  function source:getPixel(x,y)
   assert(x>=0 and x<meta.width and y>=0 and y<meta.height)
   local index=math.floor(y/meta.stride)*meta.columns+math.floor(x/meta.stride)
   return index/2048,x%meta.stride/16,y%meta.stride/16,1
  end
  function source:setPixel()error('source pixel overwritten')end
  function source:clone()return {setPixel=function(_,x,y,r,g,b,a)
   assert(x>=0 and x<meta.width and y>=0 and y<meta.height and a==1)
   local k=y*meta.width+x;assert(not written[k],'overlapping derived writes');written[k]={r,g,b,a}
  end}end
  E.surface_sheet(source,sheet,style)
  local function pixel(name,mask,x,y)
   for _,e in ipairs(E.surfaces(sheet,style))do if e.name==name and e.mask==mask then
    local ox,oy=e.slot%math.floor(meta.width/16)*16,math.floor(e.slot/math.floor(meta.width/16))*16
    return assert(written[(oy+y)*meta.width+ox+x])
   end end
  end
  return written,pixel
 end
 test('fully connected wall pixels sample the correct native center at untrimmed local coordinates',function()
  local written,pixel=sampled('rpg','kenney')
  for _,p in ipairs({{0,0},{7,5},{15,15}})do
   local c=pixel('ruin',255,p[1],p[2]);assert(c[1]==762/2048 and c[2]==p[1]/16 and c[3]==p[2]/16)
  end
  assert(next(written))
  -- The carpet starts at column 10, not 12: the adjacent cells are masonry.
  local c=pixel('carpet',255,7,5);assert(c[1]==980/2048 and c[2]==7/16 and c[3]==5/16)
  equal(D.materials.carpet.mixed.grid,{922,923,924,979,980,981,1036,1037,1038})
 end)
 test('water exterior and concave corners use the inspected RPG bank family rather than stray floor sprites',function()
  local _,pixel=sampled('rpg','kenney')
  assert(pixel('drowned',0,0,0)[1]==2/2048 and pixel('drowned',0,15,15)[1]==118/2048)
  assert(pixel('drowned',127,0,0)[1]==115/2048)
  assert(pixel('drowned',239,15,0)[1]==114/2048)
  assert(pixel('drowned',223,15,15)[1]==57/2048)
  assert(pixel('drowned',191,0,15)[1]==58/2048)
 end)
 test('monochrome environment surfaces contain neutral opaque pixels and keep native originals immutable',function()
  for _,sheet in ipairs({'rpg','caves','onebit'})do
   local written=sampled(sheet,'onebit');for _,c in pairs(written)do assert(c[1]==c[2] and c[2]==c[3] and c[4]==1 and c[1]>=0 and c[1]<=1)end
  end
 end)
 test('environment handles cache every texture and Quad instead of decoding them per tile or frame',function()
  local calls=0;local env=resolver('kenney',nil,function(raw)calls=calls+1;return image(raw)end)
  local initial=calls;local s=G.new_stage(3)
  for _=1,20 do env:set_world(s);for _,e in ipairs(env.plan.rooms)do env:resolve(s.generated_map,e.room.x,e.room.y,'wall')end end
  assert(calls==initial and initial<=6)
  local id=D.prop_id('grave_a');assert(env.handles[id]==env.handles[id] and env.handles[id].width==16)
 end)
 test('missing optional colored counterparts preserve native actors/material fallback and a distinct refresh signature',function()
  local O=require('src.onebit_visuals');local full=O.new(read,image,quad)
  local fallback=O.new(function(path)if path~=D.sources.rpg.path and path~=D.sources.caves.path then return read(path)end end,image,quad)
  assert(full.signature~=fallback.signature and fallback:resolve('core:actor/adventurer'))
  local map={rows={'~~~','~~~','~~~'},rooms={},floor_id='fixture'}
  assert(fallback:terrain(map,2,2,'water') and next(fallback.environment.failures))
 end)
 test('room materials and dressing survive exact save reopening without entering the authoritative snapshot',function()
  local s=G.new_stage(11,{multifloor=true});local before=G.snapshot(s);local env=resolver('onebit');env:set_world(s)
  local first=E.plan(s,s.generated_map);local restored=G.restore(P.decode(P.encode(before)))
  equal(first,E.plan(restored,restored.generated_map));equal(before,G.snapshot(s));assert(s.environment==nil)
 end)
 test('floor switches and visual refresh replace the presentation plan without regenerating either floor',function()
  local s=G.new_stage(5,{multifloor=true});local before=G.snapshot(s);local env=resolver('kenney');env:set_world(s)
  local old=env.plan;local map
  for _,m in pairs(s.inactive_floor_maps)do map=m end
  local context={generated_map=map,active_floor_id=map.floor_id,next_event_id=s.next_event_id,idea_rooms=s.idea_rooms}
  env:set_world(context);assert(env.plan~=old and env.plan.rooms[1].room==map.rooms[1])
  local next=resolver('kenney');next:set_world(s);equal(next.plan,E.plan(s,s.generated_map));equal(G.snapshot(s),before)
 end)
 test('every playable class has compatible F/R/X art and weapon exclusions stay out of the new family',function()
  local K=require('src.kenney_visuals').new(read,image,quad);local O=require('src.onebit_visuals').new(read,image,quad)
  for _,id in ipairs(require('src.classes').order)do for _,kind in ipairs({'basic_attack','sweeping_slash','dash'})do
   local binding=U.weapon(id,kind);assert(K:resolve(binding) and O:resolve(binding))
  end end
  for _,index in pairs(D.symbols)do assert(type(index)=='number' and index>=0 and index<1078)end
  for _,p in pairs(D.props)do assert(p[1]~='micro')end
 end)
 test('status icons reflect real Burning, supply wards, focus and charge while retaining text labels',function()
  local p={definition_id='core:actor/stormcaller',storm_charge=2,pickup_ward_actions=2,supply_focus_actions=1,statuses={burning={remaining=2}}}
  local before=copy(p);local statuses=U.statuses(p);assert(#statuses==4 and statuses[4].label=='Charge 2')
  assert(statuses[4].id=='core:ui/status_charge_2_icon');equal(before,p)
  assert(#U.statuses({})==0 and U.signal('warning')=='core:ui/signal_armed_effect' and U.signal('closed')=='core:ui/signal_spent_effect')
 end)
 test('static combat cues distinguish damage types, blocked damage, boon activations and real recovery',function()
  assert(U.effect({kind='DamageTaken',actual_loss=0})==nil)
  for damage,name in pairs({physical='slash',fire='fire',lightning='spark',echo='echo',spirit='echo'})do
   assert(U.effect({kind='DamageTaken',damage_type=damage,actual_loss=2})=='core:ui/cue_'..name..'_effect')
  end
  assert(U.effect({kind='DamageBlocked'})=='core:ui/cue_block_effect')
  assert(U.effect({kind='Healed',actual_gain=2})=='core:ui/status_heal_icon')
  assert(not U.effect({kind='Moved'}) and not U.effect({kind='Healed',actual_gain=0}))
 end)
 test('a genuine authoritative hit resolves semantic artwork and playback cannot change its state or RNG',function()
  local s=G.new(17);local p=G.player(s);p.position.x,p.position.y=7,6
  local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
  local okay,why,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(okay,why)
  local before=G.snapshot(s);local got=false;local k=require('src.kenney_visuals').new(read,image,quad)
  for _,e in ipairs(events)do if e.kind=='DamageTaken'then assert(e.target_id==enemy.id and k:resolve(U.effect(e)));got=true end end
  assert(got);local playback=require('src.playback');local b=playback.new();playback.add(b,events,p.id)
  for _=1,60 do playback.update(b,.05)end;equal(G.snapshot(s),before)
 end)
 test('saved catalogue covers all new stable visual IDs without introducing boons or changing class bodies',function()
  local a=require('src.sprite_audit').collect();assert(#a.requirements==491)
  for _,p in pairs(a.packs)do assert(#p.missing==0 and p.actor_count==65 and p.static_pose_aliases==130)end
  assert(#require('src.boon_content').order==53)
 end)
 test('actual world and HUD draw paths show fog-safe dressing and rewards with the minimap hidden',function()
  assert(not _G.love);local fake={};local env=setmetatable({love=fake},{__index=_G})
  local chunk=assert(loadfile('main.lua'));setfenv(chunk,env);chunk()
  local function visit(roots,callback)
   local seen={};local function walk(fn)
    if seen[fn]then return end;seen[fn]=true;local children={}
    for i=1,200 do local name,value=debug.getupvalue(fn,i);if not name then break end
     callback(fn,i,name,value);if type(value)=='function'then children[#children+1]=value end
    end
    for _,f in ipairs(children)do walk(f)end
   end
   for _,f in ipairs(roots)do walk(f)end
  end
  local world,hud
  visit({fake.draw},function(_,_,name,value)if name=='draw_world'then world=value elseif name=='draw_panel'then hud=value end end)
  assert(world and hud)
  local s=G.new_stage(1,{multifloor=true});local k=require('src.kenney_visuals').new(read,image,quad)
  local assets={generated=k,has_terrain=true,resolve=function(_,id)return k:resolve(id)end}
  local p=G.player(s);local camera=require('src.camera').new(s.generated_map,{x=16,y=16,width=992,height=520},40,p.position.x,p.position.y)
  local font={getWidth=function(_,text)return #text*7 end,getHeight=function()return 14 end}
  local inject={state=s,visual_assets=assets,camera=camera,spectator_enabled=false,minimap_visible=false,
   ui_body_font=font,ui_display_font=font,ui_menu_font=font}
  visit({fake.draw,fake.keypressed},function(fn,i,name)if inject[name]~=nil then debug.setupvalue(fn,i,inject[name])end end)
  local drawn={};fake.graphics=setmetatable({draw=function(texture,q)drawn[q]=(drawn[q]or 0)+1 end,
   getFont=function()return font end,getDimensions=function()return 1024,640 end,getWidth=function()return 1024 end},
   {__index=function()return function()end end})
  local before=G.snapshot(s);world();hud();k:set_world(s)
  local hidden=0
  for _,prop in ipairs(k.environment.plan.props)do
   if not G.is_visible(s,s.active_floor_id,prop.x,prop.y)then hidden=hidden+1;assert(not drawn[k:resolve(prop.id).quad])end
  end
  assert(hidden>0 and drawn[k:resolve(U.weapon(p.definition_id,'basic_attack')).quad])
  -- A spectator fixture traces every prop without committing any gameplay action.
  visit({fake.draw,fake.keypressed},function(fn,i,name)if name=='spectator_enabled'then debug.setupvalue(fn,i,true)end end)
  -- This diagnostic injects closed upvalues, unlike ordinary game input.
  -- Retire compiled traces before changing the visibility fixture underneath it.
  if jit then jit.flush()end
  drawn={};world();hud()
  for _,prop in ipairs(k.environment.plan.props)do assert(drawn[k:resolve(prop.id).quad],'world dressing absent with hidden minimap')end
  for _,n in ipairs(s.idea_terrain.nodes)do if n.floor_id==s.active_floor_id then
   assert(drawn[k:resolve(require('src.idea_terrain').visual(n)).quad],'terrain reward pass absent: '..n.kind..' / '..tostring(require('src.idea_terrain').visual(n)))
  end end
  equal(G.snapshot(s),before);assert(not _G.love)
 end)
end
