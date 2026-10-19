return function(test,equal)
 local G=require('src.game');local C=require('src.classes');local F=require('src.floors');local P=require('src.persistence')
 local ids={'geomancer','spiritbinder','tinkerer','stormcaller','wayfinder'}
 test('every new class has readable pure status and contextual placement or command instructions',function()
  for _,name in ipairs(ids)do
   local s=G.new(37,nil,{class_id='core:actor/'..name});local p=G.player(s);local snap=G.snapshot(s)
   local label,detail=C.status(s,p);assert(type(label)=='string' and type(detail)=='string' and #label>0)
   local hint=C.preview_hint(p,'sweeping_slash',{companion_mode='place'});assert(hint~='Enter to strike · Esc cancel')
   assert(C.for_player(p).weapon_icon);equal(snap,G.snapshot(s))
  end
 end)
 test('clickable class page controls reach every class and stay inside the actual page bounds',function()
  local U=require('src.pixel_ui');assert(U.class_nav_at(200,510)==-1 and U.class_nav_at(360,510)==1)
  assert(not U.class_nav_at(330,510) and not U.class_nav_at(360,540))
  local first=C.order[1];local next=U.class_page_target(C.order,first,1);assert(next==C.order[10])
  assert(U.class_page_target(C.order,next,-1)==first and U.class_page_target(C.order,next,1)==next)
  for _,id in ipairs(C.order)do local a,b=U.class_page(C.order,id);assert(b-a<=8)end
 end)
 test('sentry commitment freezes with its floor and resumes at its rebased locked impact',function()
  local s=G.new(37,nil,{class_id='core:actor/tinkerer'});local p=G.player(s);p.position.x,p.position.y=7,6
  local target=G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,100000)
  assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));local sentry=require('src.idea_companions').owned(s,'sentry')
  assert(s.clock==168 and sentry.sentry_due==180);F.park(s,s.active_floor_id);s.clock=s.clock+1000;F.resume(s,s.active_floor_id)
  assert(sentry.sentry_due==1180);assert(G.restore(G.snapshot(s)));local _,_,events=G.submit(s,{kind='wait'})
  assert(target.hp==8);local hit=false;for _,e in ipairs(events)do if e.kind=='DamageTaken'then assert(e.time==1180);hit=true end end;assert(hit)
 end)
 test('pending storm crosses freeze with their floor and reject a forged non-cross shape',function()
  local s=G.new(37,nil,{class_id='core:actor/stormcaller'});local p=G.player(s);p.position.x,p.position.y=7,6
  G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,100000)
  assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));p.speed=300
  assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));local storm=assert(s.class_special.storm)
  local due=storm.due;F.park(s,s.active_floor_id);s.clock=s.clock+1000;F.resume(s,s.active_floor_id)
  assert(storm.due==due+1000);assert(G.restore(G.snapshot(s)))
  local snap=G.snapshot(s);snap.class_special.storm.cells[1].x=2;assert(not pcall(G.restore,snap))
 end)
 test('grounded ward absorbs an actual Burning tick once and preserves the remaining status clock',function()
  local s=G.new(37,nil,{class_id='core:actor/stormcaller'});local p=G.player(s);p.position.x,p.position.y=7,6
  local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,0)
  G.set_boon_stacks(s,enemy.id,'core:boon/emberbrand','common',1)
  assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}));assert(p.statuses.burning)
  p.storm_charge=1;local hp=p.hp
  local _,_,events=G.submit(s,{kind='dash',dx=0,dy=1})
  local tick=false;for _,e in ipairs(events)do if e.kind=='DamageBlocked' and e.damage_type=='fire'then
   assert(e.block_reason=='grounded ward');tick=true
  end end
  assert(tick and not p.storm_ward and p.hp==hp-2 and p.statuses.burning.remaining==1 and p.statuses.burning.next_due==414 and G.restore(G.snapshot(s)))
 end)
 test('sentry bolts consume directional reflectors at commitment and resume their fixed turned path',function()
  local s=G.new(37,nil,{class_id='core:actor/tinkerer'});local p=G.player(s);p.position.x,p.position.y=7,6
  local hexer=G.spawn_enemy(s,'core:enemy/mirror_hexer',10,6,50)
  for _=1,3 do assert(G.submit(s,{kind='wait'}))end
  assert(s.idea_mirrors and #s.idea_mirrors==1)
  local scheduler=require('src.scheduler');scheduler.remove_for_actor(s,hexer.id)
  scheduler.enqueue(s,{kind='actor_ready',due=100000,actor_id=hexer.id,is_player=false})
  local other=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,7,100000)
  assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}))
  local sentry=require('src.idea_companions').owned(s,'sentry')
  assert(sentry.sentry_reflection and #s.idea_mirrors==0 and sentry.sentry_cells[2].x==9 and sentry.sentry_cells[2].y==7)
  local snap=G.snapshot(s);local b=G.restore(P.decode(P.encode(snap)))
  local _,_,a=G.submit(s,{kind='wait'});local _,_,c=G.submit(b,{kind='wait'})
  assert(other.hp==8 and hexer.hp==7);equal(a,c);equal(G.snapshot(s),G.snapshot(b))
  for _,e in ipairs(snap.entities)do if e.companion_kind=='sentry'then e.sentry_cells[2].x=10 end end
  assert(not pcall(G.restore,snap))
 end)

end
