return function(test,equal)
 local G=require('src.game');local I=require('src.idea_interactions');local C=require('src.content')
 local P=require('src.persistence');local V=require('src.visibility');local Q=require('src.scheduler')
 local Def=require('src.idea_interaction_content');local Boons=require('src.boons')
 local function fixture(kind,extra)
  local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6;V.recompute(s)
  local n=I.place(s,kind,C.map_for(s),7,5,extra)
  return s,p,n
 end
 local function use(s,n,id,selection,replace)
  local ok,why,events=G.submit(s,{kind='interact',node_id=n.id,choice_id=id,selection=selection,replace=replace})
  assert(ok,why);return events
 end
 local function count(events,kind)
  local n=0;for _,e in ipairs(events)do if e.kind==kind then n=n+1 end end;return n
 end
 local function room_fixture(class)
  local s=G.new_generated(37,{class_id=class});s.meta.eligible=false
  for index=#s.entities,1,-1 do local e=s.entities[index]
   if e.controller=='ai'then Q.remove_for_actor(s,e.id);table.remove(s.entities,index)end
  end
  s.idea_rooms={version=1,nodes={},groups={}}
  local p=G.player(s);local map=C.map_for(s)
  for _,r in ipairs(map.rooms)do for y=r.y+1,r.y+r.height-3 do for x=r.x+1,r.x+r.width-4 do
   local okay=true
   for yy=y,y+2 do for xx=x,x+3 do
    if not C.walkable(map,xx,yy)then okay=false end
    for _,e in ipairs(s.entities)do if e.id~=p.id and e.blocks_movement and e.position.x==xx and e.position.y==yy then okay=false end end
   end end
   if okay then p.position.x,p.position.y=x,y;V.recompute(s);return s,p,map,r end
  end end end
  error('no clear prepared room')
 end

 test('interaction inspection, cancellation and invalid choices leave exact state and RNG intact',function()
  local s,p,n=fixture('offering_font');local before=G.snapshot(s)
  for _=1,4 do assert(#G.interaction_options(s,n.id).entries==3)end
  assert(not G.submit(s,{kind='cancel'}));assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='99'}))
  assert(not G.submit(s,{kind='interact',node_id='../bad',choice_id='1'}))
  equal(G.snapshot(s),before)
  p.position.x=2;p.position.y=2;V.recompute(s);assert(not G.interaction_options(s,n.id))
 end)
 test('offering font pays nonlethal exact HP once and grants a cached class-eligible boon',function()
  local s,p,n=fixture('offering_font');p.hp=18;local offers=P.encode(n.offers);local rng=s.rng.state
  local e=G.interaction_options(s,n.id).entries[1];local id=e.offer.boon_id;local rarity=e.offer.rarity
  local events=use(s,n,'1');assert(p.hp==13 and p.boons[id][rarity]==1 and n.spent and s.clock==100)
  assert(P.encode(n.offers)==offers and s.rng.state==rng and count(events,'BoonGranted')==1)
  assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='2'}));assert(G.restore(G.snapshot(s)))
  local a,b,c=fixture('offering_font');b.hp=5;local before=G.snapshot(a)
  assert(not G.submit(a,{kind='interact',node_id=c.id,choice_id='1'}));equal(G.snapshot(a),before)
 end)
 test('ration and camp recovery is capped, finite, and refuses a no-op full-health choice',function()
  for _,kind in ipairs({'ration_cache','camp'})do
   local s,p,n=fixture(kind);local before=G.snapshot(s)
   assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='food'}));equal(G.snapshot(s),before)
   p.hp=10;use(s,n,'food');assert(p.hp==(kind=='camp' and 16 or 14) and n.spent)
   assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='ward'}));assert(G.restore(G.snapshot(s)))
  end
 end)
 test('a supplied ward blocks exactly one real hit in a simultaneous enemy burst',function()
  local s,p,n=fixture('ration_cache')
  G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,0);G.spawn_enemy(s,'core:enemy/mossbound_guard',6,6,0)
  use(s,n,'ward');assert(p.pickup_ward_actions==3)
  local hp=p.hp;local _,_,events=G.submit(s,{kind='wait'})
  assert(count(events,'WardFlaskBlocked')==1 and p.hp<hp and p.pickup_ward_actions==0)
  assert(G.restore(G.snapshot(s)))
 end)
 test('well focus matches attack preview, applies to one primary batch and expires after three actions',function()
  local s,p,n=fixture('charm_well');use(s,n,'focus');assert(p.supply_focus_actions==3)
  local target=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
  local _,damage=G.effective_ability(s,'basic_attack',1,0,true);assert(damage==p.damage+2)
  local hp=target.hp;local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
  assert(target.hp==hp-math.max(0,damage-target.defense) and p.supply_focus_actions==0 and count(events,'SupplyFocusSpent')==1)
  local a,b,c=fixture('charm_well');use(a,c,'focus')
  for _=1,3 do assert(G.submit(a,{kind='wait'}))end;assert(b.supply_focus_actions==0)
 end)
 test('focus applies to every member of one simultaneous primary batch but not its delayed echo',function()
  local s,p,n=fixture('charm_well');G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
  use(s,n,'focus')
  for _,y in ipairs({5,6,7})do local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,y,100000);e.hp=40;e.max_hp=40 end
  local _,damage=G.effective_ability(s,'sweeping_slash',1,0,true)
  local _,_,events=G.submit(s,{kind='sweeping_slash',dx=1,dy=0});local direct=0
  for _,e in ipairs(events)do if e.kind=='DamageAttempted' and e.direct_attack then assert(e.attempted_damage==damage);direct=direct+1 end end
  assert(direct==3 and count(events,'SupplyFocusSpent')==1 and p.supply_focus_actions==0)
  local _,_,later=G.submit(s,{kind='wait'});local echoes=0
  for _,e in ipairs(later)do if e.kind=='DamageAttempted' and e.damage_type=='echo'then assert(e.attempted_damage==1);echoes=echoes+1 end end
  assert(echoes==3 and G.restore(G.snapshot(s)))
 end)
 test('utility collection has one slot and requires explicit replacement without losing a charge on refusal',function()
  local s,p,n=fixture('cleanse_flask');use(s,n,'take');assert(p.utility.kind=='cleanse_flask')
  local second=I.place(s,'escape_scroll',C.map_for(s),8,6);local before=G.snapshot(s)
  assert(not G.submit(s,{kind='interact',node_id=second.id,choice_id='take'}));equal(G.snapshot(s),before)
  use(s,second,'take',nil,true);assert(p.utility.kind=='escape_scroll' and n.spent and second.spent)
  assert(G.restore(G.snapshot(s)))
 end)
 test('cleanse removes actual Burning and only its pending tick; unused healthy doses are retained',function()
  local s,p,n=fixture('cleanse_flask');use(s,n,'take')
  local before=G.snapshot(s);assert(not G.submit(s,{kind='use_utility'}));equal(G.snapshot(s),before)
  local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,s.clock)
  G.set_boon_stacks(s,enemy.id,'core:boon/emberbrand','common',1)
  assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}));assert(p.statuses.burning)
  local burn_id=p.statuses.burning.schedule_id;local hp=p.hp
  local ok,why,events=G.submit(s,{kind='use_utility'});assert(ok,why)
  assert(not p.utility and not p.statuses.burning and p.hp==hp and count(events,'StatusCleansed')==1)
  for _,w in ipairs(s.schedule)do assert(w.id~=burn_id)end
  assert(G.restore(G.snapshot(s)))
 end)
 test('camp preparation cleanses Burning while retaining exactly three future ward actions',function()
  local s,p,n=fixture('camp');local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,0)
  G.set_boon_stacks(s,enemy.id,'core:boon/emberbrand','common',1)
  assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}));assert(p.statuses.burning)
  use(s,n,'ward');assert(not p.statuses.burning and p.pickup_ward_actions==3 and n.spent)
 end)
 test('escape scroll previews a chosen exact path, consumes one charge and emits each entered tile',function()
  local s,p,n=fixture('escape_scroll');use(s,n,'take');local before=G.snapshot(s)
  local view=G.preview(s,'use_utility',1,0,3);assert(view.valid and #view.cells==3)
  equal(G.snapshot(s),before)
  local ok,why,events=G.submit(s,{kind='use_utility',dx=1,dy=0,steps=3});assert(ok,why)
  assert(p.position.x==10 and p.position.y==6 and not p.utility and count(events,'TileEntered')==3)
  assert(G.restore(G.snapshot(s)))
 end)
 test('escape refuses occupied, unseen, wall and invalid-range paths with exact no-op state',function()
  for _,case in ipairs({'occupied','unseen','wall','steps'})do
   local s,p,n=fixture('escape_scroll');use(s,n,'take')
   local intent={kind='use_utility',dx=1,dy=0,steps=3}
   if case=='occupied'then G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
   elseif case=='unseen'then V.recompute(s,1)
   elseif case=='wall'then p.position.x=6;intent.dx=0;intent.dy=1
   else intent.steps=4 end
   local before=G.snapshot(s);assert(not G.submit(s,intent),case);equal(G.snapshot(s),before)
  end
 end)
 test('trader stock and prices persist; failed purchases do not spend gold or reroll',function()
  local s,p,n=fixture('travelling_trader');local stock=n.stock;local before=G.snapshot(s)
  assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='utility'}));equal(G.snapshot(s),before)
  s.currency=5;local price=stock=='cleanse_flask' and 2 or 3
  local restored=G.restore(G.snapshot(s));equal(G.interaction_options(s,n.id),G.interaction_options(restored,n.id))
  use(s,n,'utility');assert(s.currency==5-price and p.utility.kind==stock and n.spent)
  assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='food'}))
 end)
 test('forge trades exactly one selected rarity, same-rarity replacement and fixed saved alternatives',function()
  local s,p,n=fixture('attunement_forge');s.currency=10
  G.set_boon_stacks(s,p.id,'core:boon/crescent_reach','common',2)
  G.set_boon_stacks(s,p.id,'core:boon/crescent_reach','rare',1)
  local selection={id='core:boon/crescent_reach',rarity='common'}
  local before=G.snapshot(s);local opts=G.interaction_options(s,n.id,selection)
  assert(#opts.entries==3 and opts.entries[1].detail:find('cells'));equal(G.snapshot(s),before)
  local e;for _,candidate in ipairs(opts.entries)do if candidate.enabled then e=candidate;break end end;assert(e)
  local b=G.restore(G.snapshot(s));equal(opts,G.interaction_options(b,n.id,selection))
  use(s,n,e.id,selection)
  assert(s.currency==7 and p.boons[selection.id].common==1 and p.boons[selection.id].rare==1)
  assert(p.boons[e.offer.boon_id].common==1 and n.spent and G.restore(G.snapshot(s)))
 end)
 test('memorial previews transformation removal, retires one stack, heals and preserves other stacks/stats',function()
  local s,p,n=fixture('memorial');p.hp=6
  G.set_boon_stacks(s,p.id,'core:boon/crescent_reach','common',1)
  G.set_boon_stacks(s,p.id,'core:boon/emberbrand','rare',2)
  local hpmax,damage,speed=p.max_hp,p.damage,p.speed
  local opts=G.interaction_options(s,n.id);local e
  for _,v in ipairs(opts.entries)do if v.remove.id=='core:boon/crescent_reach'then e=v end end;assert(e and e.detail:find('cells'))
  use(s,n,e.id);assert(p.hp==15 and not p.boons['core:boon/crescent_reach'] and p.boons['core:boon/emberbrand'].rare==2)
  assert(p.max_hp==hpmax and p.damage==damage and p.speed==speed and G.restore(G.snapshot(s)))
 end)
 test('retiring Temporal Echo preserves its already committed locked effect',function()
  local s,p,n=fixture('attunement_forge');s.currency=10;p.speed=300
  G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
  local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000);enemy.hp=40;enemy.max_hp=40
  assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(next(s.delayed_effects))
  local selection={id='core:boon/temporal_echo',rarity='common'};local entry
  for _,e in ipairs(G.interaction_options(s,n.id,selection).entries)do if e.enabled then entry=e;break end end;assert(entry)
  use(s,n,entry.id,selection);assert(not p.boons[selection.id] and next(s.delayed_effects))
  local b=G.restore(G.snapshot(s));local resolved=0
  for _=1,5 do local _,_,a=G.submit(s,{kind='wait'});local _,_,c=G.submit(b,{kind='wait'});equal(a,c);resolved=resolved+count(a,'DelayedEffectResolved')end
  assert(resolved==1);equal(G.snapshot(s),G.snapshot(b))
 end)
 test('return token uses two paid actions in one real room and never overwrites the Wayfinder beacon',function()
  local s,p,map=room_fixture('core:actor/wayfinder');local n=I.place(s,'return_token',map,p.position.x,p.position.y+1)
  use(s,n,'take');assert(G.submit(s,{kind='use_utility'}));local marker=p.utility.planted;assert(marker)
  local x,y=p.position.x,p.position.y;assert(G.submit(s,{kind='move',dx=1,dy=0}));assert(G.submit(s,{kind='move',dx=1,dy=0}))
  assert(G.submit(s,{kind='use_utility'}));assert(p.position.x==x and p.position.y==y and not p.utility and not s.class_special.beacon)
  assert(G.restore(G.snapshot(s)))
 end)
 test('token refuses blocked landing and expires on leaving its actual room by ordinary movement',function()
  local s,p,map,r=room_fixture();local n=I.place(s,'return_token',map,p.position.x,p.position.y+1)
  use(s,n,'take');assert(G.submit(s,{kind='use_utility'}));local b=p.utility.planted
  assert(G.submit(s,{kind='move',dx=1,dy=0}));local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',b.x,b.y,100000)
  local before=G.snapshot(s);assert(not G.submit(s,{kind='use_utility'}));equal(G.snapshot(s),before)
  Q.remove_for_actor(s,enemy.id);for i,e in ipairs(s.entities)do if e.id==enemy.id then table.remove(s.entities,i);break end end
  -- Prepared location followed by a real submitted move across this room boundary.
  local exited=false
  for y=r.y,r.y+r.height-1 do for x=r.x,r.x+r.width-1 do
   for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
    local target=I.room_at(map,x+d[1],y+d[2])
    if not exited and (not target or target.id~=r.id) and C.walkable(map,x+d[1],y+d[2]) and C.walkable(map,x,y)then
     p.position.x,p.position.y=x,y;V.recompute(s)
     local v=G.preview(s,'move',d[1],d[2]);if v.valid and v.distance==1 then assert(G.submit(s,{kind='move',dx=d[1],dy=d[2]}));exited=true end
    end
   end
  end end
  assert(exited and not p.utility);assert(G.restore(G.snapshot(s)))
 end)
 test('bell puzzle keeps fixed clue/progress through restore, resets wrong input and pays only once',function()
  local s,p,n=fixture('bell_clue',{group='test-bells'});s.idea_rooms.groups['test-bells']={kind='bells',floor_id=s.active_floor_id,sequence={2,1,3},progress=0,completed=false}
  local map=C.map_for(s);local bells={I.place(s,'bell',map,8,6,{group='test-bells',bell=1}),
   I.place(s,'bell',map,6,6,{group='test-bells',bell=2}),I.place(s,'bell',map,7,7,{group='test-bells',bell=3})}
  local before=G.snapshot(s);assert(G.interaction_options(s,n.id).detail:find('2'));equal(G.snapshot(s),before)
  use(s,bells[1],'ring');assert(s.idea_rooms.groups['test-bells'].progress==0)
  use(s,bells[2],'ring');local restored=G.restore(G.snapshot(s));equal(G.snapshot(restored),G.snapshot(s))
  use(s,bells[1],'ring');use(s,bells[3],'ring');assert(s.currency==5)
  assert(not G.submit(s,{kind='interact',node_id=bells[3].id,choice_id='ring'}));assert(G.restore(G.snapshot(s)))
 end)
 test('falling fixture locks exact cells, hits allies/player normally and resolves before same-time readiness',function()
  local s,p,n=fixture('falling_fixture',{center_x=8,center_y=6,cells={{floor_id='core:floor/test',x=8,y=6},
   {floor_id='core:floor/test',x=8,y=5},{floor_id='core:floor/test',x=9,y=6},{floor_id='core:floor/test',x=8,y=7},{floor_id='core:floor/test',x=7,y=6}}})
  local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000);local hp=enemy.hp;local player_hp=p.hp
  local events=use(s,n,'trigger');assert(enemy.hp==hp-math.max(0,4-enemy.defense) and p.hp==player_hp-4 and n.spent and not n.pending)
  assert(count(events,'FixtureLocked')==1 and count(events,'FixtureFell')==1)
  for _,e in ipairs(events)do if e.kind=='FixtureFell'then assert(e.time==100)end end
  assert(G.restore(G.snapshot(s)))
 end)
 test('fast fixture commitment parks/rebases and exact pending cells survive Continue',function()
  local s,p,n=fixture('falling_fixture',{center_x=8,center_y=6,cells={{floor_id='core:floor/test',x=8,y=6},
   {floor_id='core:floor/test',x=8,y=5},{floor_id='core:floor/test',x=9,y=6},{floor_id='core:floor/test',x=8,y=7},{floor_id='core:floor/test',x=7,y=6}}})
  p.speed=300;use(s,n,'trigger');assert(n.pending and n.pending.due==100 and s.clock==33)
  local F=require('src.floors');F.park(s,s.active_floor_id);s.clock=s.clock+1000;F.resume(s,s.active_floor_id)
  assert(n.pending.due==1100);local b=G.restore(P.decode(P.encode(G.snapshot(s))))
  for _=1,3 do local _,_,a=G.submit(s,{kind='wait'});local _,_,c=G.submit(b,{kind='wait'});equal(a,c)end
  assert(not n.pending);equal(G.snapshot(s),G.snapshot(b))
 end)
 test('sign inspection gives only a fixed coarse hint with exact no-op state',function()
  local s,p,n=fixture('route_sign',{dx=1,dy=0,hint='Treasure'});local before=G.snapshot(s)
  local o=G.interaction_options(s,n.id);assert(o.detail:find('Treasure east') and #o.entries==0)
  assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='read'}));equal(G.snapshot(s),before)
 end)
 test('contract rewards one voluntary legal kill sequence and never disables movement controls',function()
  local s,p,map,r=room_fixture();local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',p.position.x+1,p.position.y,100000);enemy.hp=1
  local n=I.place(s,'archive_contract',map,p.position.x,p.position.y+1,{room_id=r.id,room=r,targets={enemy.id},status='offered',defeated={}})
  use(s,n,'accept');local gold=s.currency;local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
  assert(n.status=='completed' and s.currency==gold+8 and count(events,'ContractChanged')==1)
  assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='accept'}));assert(G.restore(G.snapshot(s)))
 end)
 test('interaction population changes only its bounded store, not RNG, entities, scheduler or geometry',function()
  local s=G.new_stage(9,{multifloor=true});local map=C.map_for(s)
  local before={clock=s.clock,rng=s.rng,entities=s.entities,schedule=s.schedule,map=map}
  local bytes=P.encode(before);s.idea_rooms={version=1,nodes={},groups={}}
  I.populate(s,map)
  assert(P.encode({clock=s.clock,rng=s.rng,entities=s.entities,schedule=s.schedule,map=map})==bytes)
  assert(#s.idea_rooms.nodes>=3)
 end)
 test('legitimate font and memorial use preserve achievement eligibility and do not mislabel rest as a food pickup',function()
  local s,p,n=fixture('offering_font');s.meta=require('src.achievements').new_run(true,'finite-service-test')
  local offer=n.offers[1];use(s,n,'1');assert(s.meta.eligible and p.boons[offer.boon_id])
  local memorial=I.place(s,'memorial',C.map_for(s),8,6)
  local choice=G.interaction_options(s,memorial.id).entries[1]
  local events=use(s,memorial,choice.id)
  assert(s.meta.eligible and count(events,'Healed')==1 and count(events,'SupplyCollected')==0 and G.restore(G.snapshot(s)))
 end)
 test('a contract fails on actual Dash while the ordinary movement still commits',function()
  local s,p,map,r=room_fixture();local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',p.position.x+3,p.position.y+2,100000)
  local n=I.place(s,'archive_contract',map,p.position.x,p.position.y+1,{room_id=r.id,room=r,targets={enemy.id},status='offered',defeated={}})
  use(s,n,'accept');local x=p.position.x
  local ok,why,events=G.submit(s,{kind='dash',dx=1,dy=0});assert(ok,why)
  assert(p.position.x>x and n.status=='failed' and n.failure=='movement ability used' and count(events,'ContractChanged')==1)
  assert(G.restore(G.snapshot(s)))
 end)
 test('room contract permits a nonmovement class deployment and has an explicit paid abandon',function()
  local s,p,map,r=room_fixture('core:actor/spiritbinder');local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',p.position.x+3,p.position.y+2,100000)
  local n=I.place(s,'archive_contract',map,p.position.x,p.position.y+1,{room_id=r.id,room=r,targets={enemy.id},status='offered',defeated={}})
  use(s,n,'accept');assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));assert(n.status=='active')
  use(s,n,'abandon');assert(n.status=='failed' and n.failure=='abandoned' and G.restore(G.snapshot(s)))
 end)
 test('stale forge selections, invalid stack metadata and insufficient funds are exact no-ops',function()
  local s,p,n=fixture('attunement_forge');G.set_boon_stacks(s,p.id,'core:boon/emberbrand','common',1)
  for _,selection in ipairs({false,1,'bad',{id='core:boon/missing',rarity='common'},{id='core:boon/emberbrand',rarity='legendary'}})do
   local before=G.snapshot(s);assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='1',selection=selection}));equal(G.snapshot(s),before)
  end
  local before=G.snapshot(s);assert(not G.submit(s,{kind='interact',node_id=n.id,choice_id='1',selection={id='core:boon/emberbrand',rarity='common'}}));equal(G.snapshot(s),before)
 end)
 test('malformed utility, font, forge, bell, fixture and contract snapshots reject before activation',function()
  local cases={
   function()local s,p,n=fixture('offering_font');n.offers[2]=n.offers[1];return s end,
   function()local s,p,n=fixture('attunement_forge');n.trades['core:boon/emberbrand'].common[1]='../bad';return s end,
   function()local s,p,n=fixture('cleanse_flask');use(s,n,'take');p.utility.kind='unknown';return s end,
   function()local s,p,n=fixture('ration_cache');p.supply_focus_actions=-1;return s end,
   function()local s,p,n=fixture('ration_cache');n.x=n.x+1;return s end,
   function()local s,p,n=fixture('ration_cache');s.idea_rooms.version=99;return s end,
   function()local s,p,n=fixture('ration_cache');s.idea_rooms.nodes[2]=n;return s end,
  }
  for _,create in ipairs(cases)do assert(not pcall(G.restore,G.snapshot(create())))end
  local s,p,n=fixture('falling_fixture',{center_x=8,center_y=6,cells={{floor_id='core:floor/test',x=8,y=6},
   {floor_id='core:floor/test',x=8,y=5},{floor_id='core:floor/test',x=9,y=6},{floor_id='core:floor/test',x=8,y=7},{floor_id='core:floor/test',x=7,y=6}}})
  p.speed=300;use(s,n,'trigger');local snap=G.snapshot(s)
  snap.idea_rooms.nodes[1].pending.cells[1].x=9;assert(not pcall(G.restore,snap))
  snap=G.snapshot(s);for _,w in ipairs(snap.schedule)do if w.idea_fixture then w.due=w.due+1 end end;assert(not pcall(G.restore,snap))
 end)
 test('existing saves without the optional interaction store reopen without populating new rewards',function()
  local s=G.new_stage(9,{multifloor=true});s.idea_rooms=nil
  local snap=G.snapshot(s);local restored=G.restore(P.decode(P.encode(snap)))
  assert(restored.idea_rooms==nil);equal(G.snapshot(restored),snap)
 end)
 test('a carried charge survives all stage handoffs and the final chamber without a schema change',function()
  local s=G.new_stage(2);s.meta.eligible=false;G.player(s).utility={kind='cleanse_flask'}
  for index=1,3 do
   local p=G.player(s);local elite=G.entity(s,s.stage.elite_id)
   for _,e in ipairs(s.entities)do if e.controller=='ai'then Q.remove_for_actor(s,e.id)end end
   local aim
   for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
    local x,y=elite.position.x+d[1],elite.position.y+d[2]
    if C.walkable(s.generated_map,x,y)then p.position.x,p.position.y=x,y;aim={-d[1],-d[2]};break end
   end
   assert(aim);p.damage=elite.hp+elite.defense+2;V.recompute(s)
   assert(G.submit(s,{kind='basic_attack',dx=aim[1],dy=aim[2]}));local exit=G.entity(s,s.stage.exit_id)
   for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
    local x,y=exit.position.x+d[1],exit.position.y+d[2]
    if C.walkable(s.generated_map,x,y)then p.position.x,p.position.y=x,y;break end
   end
   assert(G.submit(s,{kind='choose_route',exit_id=exit.id,route_index=1}))
   assert(G.player(s).utility.kind=='cleanse_flask' and G.restore(G.snapshot(s)))
  end
  assert(s.finale and s.schema=='roag-run/11' and #s.idea_rooms.nodes==0)
 end)
 test('all fourteen features occur naturally with bounded service counts and deterministic exact reopening',function()
  local B=require('src.biomes');local seen={};local total=0
  for _,id in ipairs(B.order)do for seed=1,5 do
   local first=id==B.order[1]
   local s=G.new_stage(seed,{multifloor=true,stage_options={biome_id=id,index=first and 1 or 2,
    route_history=first and {} or {(id:gsub('^core:biome/','core:route/'))}}})
   local budget={}
   for _,n in ipairs(s.idea_rooms.nodes)do
    seen[n.kind]=true;total=total+1;budget[n.floor_id]=budget[n.floor_id]or {services=0,assemblies=0,signs=0}
    local b=budget[n.floor_id]
    if n.kind=='falling_fixture' or n.kind=='bell_clue'then b.assemblies=b.assemblies+1
    elseif n.kind=='route_sign'then b.signs=b.signs+1
    elseif n.kind~='bell'then b.services=b.services+1 end
   end
   for _,b in pairs(budget)do assert(b.services<=2 and b.assemblies<=1 and b.signs<=1)end
   local snap=G.snapshot(s);equal(G.snapshot(G.restore(snap)),snap)
   equal(G.snapshot(G.new_stage(seed,{multifloor=true,stage_options={biome_id=id,index=first and 1 or 2,
    route_history=first and {} or {(id:gsub('^core:biome/','core:route/'))}}})),snap)
  end end
  for _,kind in ipairs(Def.order)do assert(seen[kind],'not placed: '..kind)end
  assert(total>200)
 end)
 local function callbacks(s)
  -- Load real callbacks into an isolated table, without love.load, display, GPU or global LÖVE.
  assert(not _G.love);local env=setmetatable({love={}}, {__index=_G})
  local chunk=assert(loadfile('main.lua'));setfenv(chunk,env);chunk()
  local function set(name,value)
   for i=1,60 do local key=debug.getupvalue(env.love.keypressed,i)
    if key==name then debug.setupvalue(env.love.keypressed,i,value);return end
   end
   error('callback upvalue missing: '..name)
  end
  set('state',s);set('title',false);set('spectator_enabled',false)
  return env.love,set
 end
 test('real keyboard and mouse service callbacks inspect freely and reject confirmation during playback',function()
  local s,p,n=fixture('ration_cache');p.hp=10
  local love,set=callbacks(s);local Playback=require('src.playback');local playback=Playback.new();set('playback',playback)
  local before=G.snapshot(s);love.keypressed('e');assert(love.roagUIState().page=='interaction')
  love.keypressed('return');assert(love.roagMenuState().confirm);equal(G.snapshot(s),before)
  Playback.add(playback,{{kind='AttackPerformed',source_id=p.id,time=s.clock,floor_id=s.active_floor_id,target_cells={{x=8,y=6}}}},p.id)
  assert(Playback.busy(playback));love.keypressed('return');love.mousepressed(700,520,1);equal(G.snapshot(s),before)
  Playback.update(playback,10);love.keypressed('return');assert(p.hp==14 and s.clock==100 and not love.roagMenuState())
  assert(not _G.love and G.restore(G.snapshot(s)))
 end)
 test('real inventory item button and scroll targeting callbacks cancel safely and commit the chosen distance',function()
  local s,p,n=fixture('escape_scroll');use(s,n,'take');local love=callbacks(s)
  local before=G.snapshot(s);love.keypressed('i');love.mousepressed(700,495,1)
  assert(love.roagUIState().page=='interaction');love.keypressed('return');love.keypressed('return')
  love.keypressed('1');love.keypressed('d');love.keypressed('escape');equal(G.snapshot(s),before)
  love.keypressed('u');love.keypressed('return');love.keypressed('return');love.keypressed('2');love.keypressed('d');love.keypressed('return')
  assert(p.position.x==9 and p.position.y==6 and not p.utility and G.restore(G.snapshot(s)))
  assert(not _G.love)
 end)
 test('real service renderer issues bounded menu draw calls with injected graphics and no state or RNG changes',function()
  local s,p,n=fixture('attunement_forge');s.currency=10
  G.set_boon_stacks(s,p.id,'core:boon/crescent_reach','common',1)
  local fake,set=callbacks(s);fake.keypressed('e');fake.keypressed('return')
  local visited={}
  local function find(fn,wanted,depth)
   if visited[fn] or depth>4 then return end;visited[fn]=true
   local children={}
   for i=1,60 do local name,value=debug.getupvalue(fn,i);if not name then break end
    if name==wanted then return value end
    if type(value)=='function'then children[#children+1]=value end
   end
   for _,f in ipairs(children)do local value=find(f,wanted,depth+1);if value then return value end end
  end
  local draw=assert(find(fake.draw,'draw_interaction_menu',0));local font={}
  function font:getWidth(text)return #text*7 end;function font:getHeight()return 14 end
  local seen={};local g={};fake.graphics=g
  function g.getFont()return font end;function g.setFont()end
  function g.getDimensions()return 1024,640 end
  function g.getWidth()return 1024 end
  function g.setColor()end;function g.setLineWidth()end
  function g.rectangle(mode,x,y,w,h)assert(x>=0 and y>=0 and x+w<=1024 and y+h<=640);seen[#seen+1]=mode end
  function g.printf(text,x,y,w)assert(type(text)=='string' and x>=0 and x+w<=1024 and y>=0 and y<640);seen[#seen+1]=text end
  function g.print(text,x,y)assert(type(text)=='string' and x>=0 and x<1024 and y>=0 and y<640);seen[#seen+1]=text end
  for i=1,60 do local name=debug.getupvalue(draw,i);if not name then break end
   if name=='ui_display_font' or name=='ui_body_font' or name=='ui_menu_font'then debug.setupvalue(draw,i,font)end
  end
  local before=G.snapshot(s);_G.love=fake;local ok,why=pcall(draw);_G.love=nil
  assert(ok,why);assert(#seen>15);equal(G.snapshot(s),before)
 end)
 test('service menu controller pages, cancels confirmations and never mutates a game snapshot',function()
  local M=require('src.interaction_menu');local s=G.new(37);local snap=G.snapshot(s);local entries={}
  for i=1,15 do entries[i]={id=tostring(i),label='Choice '..i,enabled=i~=2}end
  local m=M.new({entries=entries});M.key(m,'right');assert(m.selected==7 and #M.rows(m)==6)
  M.key(m,'return');assert(m.confirm);M.key(m,'escape');assert(not m.confirm)
  M.key(m,'down');assert(m.selected==8);M.key(m,'return');local action,e=M.key(m,'return');assert(action=='choose' and e.id=='8')
  m=M.new({entries=entries});M.key(m,'2');action=M.key(m,'return');assert(action=='status' and not m.confirm)
  M.mouse(m,130,166,1);assert(m.selected==1);M.mouse(m,700,520,1);assert(m.confirm)
  action=M.mouse(m,700,520,1);assert(action=='choose');equal(G.snapshot(s),snap)
 end)
end
