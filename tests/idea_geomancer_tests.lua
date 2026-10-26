return function(test,equal)
 local G=require('src.game');local Geo=require('src.idea_geomancer');local C=require('src.content')
 local P=require('src.persistence');local V=require('src.visibility');local F=require('src.floors')
 local function trial()
  local s=G.new(37,nil,{class_id=Geo.id});local p=G.player(s);p.position.x,p.position.y=7,6
  V.recompute(s);return s,p
 end
 local function block(s)for _,e in ipairs(s.entities)do if e.controller==Geo.controller then return e end end end
 test('Geomancer placement preview is pure and a paid block preserves original terrain and RNG',function()
  local s,p=trial();local rows=table.concat(C.map_for(s).rows);local rng=s.rng.state;local before=G.snapshot(s)
  for _=1,20 do assert(G.preview(s,'sweeping_slash',0,1).valid)end;equal(before,G.snapshot(s))
  local ok,why,events=G.submit(s,{kind='sweeping_slash',dx=0,dy=1});assert(ok,why)
  local e=assert(block(s));assert(s.clock==153 and e.expires==300 and not e.hp)
  assert(rows==table.concat(C.map_for(s).rows) and rng==s.rng.state)
  for _,v in ipairs(events)do assert(v.kind~='AttackPerformed' and v.kind~='AbilityUsed')end
  assert(G.restore(G.snapshot(s)))
 end)
 test('bulwark stops movement projectiles and sight without becoming an enemy or reward source',function()
  local s,p=trial();assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}))
  local e=block(s);assert(e and not G.preview(s,'move',1,0).valid)
  assert(not C.projectile_passable(C.map_for(s),8,6,s) and not V.can_see(s,p.position.floor_id,10,6))
  assert(G.preview(s,'basic_attack',1,0).cells[1].x==8)
  local gold=s.currency;assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
  assert(s.currency==gold and block(s)) -- 153+141 = 294, six units before expiry
 end)
 test('bulwark expires at the exact scheduled time and replacement cancels the old timer',function()
  local s,p=trial();assert(G.submit(s,{kind='sweeping_slash',dx=0,dy=1}))
  local first=block(s);local timer=first.expiry_schedule_id
  assert(G.submit(s,{kind='sweeping_slash',dx=-1,dy=0}));local second=assert(block(s))
  assert(second.id~=first.id and second.expires==453 and s.clock==306)
  for _,w in ipairs(s.schedule)do assert(w.id~=timer)end
  assert(G.submit(s,{kind='wait'}));assert(block(s))
  local _,_,events=G.submit(s,{kind='wait'});assert(not block(s))
  local expired=false;for _,v in ipairs(events)do if v.kind=='GeomancerBlockExpired'then assert(v.time==453);expired=true end end
  assert(expired and G.restore(G.snapshot(s)))
 end)
 test('occupied diagonal and one-cell route sealing requests are rejected without advancing time',function()
  local s,p=trial();local before=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=1}));equal(before,G.snapshot(s))
  G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
  before=G.snapshot(s);assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));equal(before,G.snapshot(s))
  local map={floor_id=p.position.floor_id,width=5,height=3,rows={'#####','#...#','#####'}}
  p.position.x,p.position.y=2,2;s.entities={p}
  local ok,why=Geo.preview(s,map,3,2);assert(not ok and why=='cannot seal an accessible route')
 end)
 test('stone bolt stops at the first body and a blocked diagonal corner',function()
  local s,p=trial();local a=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
  local b=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
  assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(a.hp==6 and b.hp==10 and s.clock==141)
  p.position.x,p.position.y=8,4;local aim=G.preview(s,'basic_attack',1,1)
  assert(#aim.cells==1 and aim.cells[1].x==9 and aim.cells[1].y==5)
 end)
 test('saved bulwarks require one valid owner matching finite expiry and no orphan timer',function()
  local s=trial();assert(G.submit(s,{kind='sweeping_slash',dx=0,dy=1}))
  local snap=G.snapshot(s);equal(snap,G.snapshot(G.restore(P.decode(P.encode(snap)))))
  for _,change in ipairs({function(v)for _,e in ipairs(v.entities)do if e.controller==Geo.controller then e.owner_id=9999 end end end,
   function(v)for _,e in ipairs(v.entities)do if e.controller==Geo.controller then e.expires=e.expires+1 end end end,
   function(v)for _,w in ipairs(v.schedule)do if w.idea_geomancer then w.idea_geomancer=false end end end})do
   local b=P.decode(P.encode(snap));change(b);assert(not pcall(G.restore,b))
  end
 end)
 test('paused-floor bulwark expiry rebases with its work and replacement removes parked work',function()
  local s=G.new_stage(9,{class_id=Geo.id,multifloor=true});local p=G.player(s);local map=C.map_for(s)
  local chosen
  for y=2,map.height-1 do for x=2,map.width-1 do if not chosen and C.walkable(map,x,y)then
   p.position.x,p.position.y=x,y
   for _,d in ipairs({{0,1},{1,0}})do if Geo.preview(s,map,x+d[1],y+d[2])then chosen={x+d[1],y+d[2]};break end end
  end end end
  assert(chosen);local e=Geo.place(s,p,chosen[1],chosen[2],1,function()end)
  local old=s.active_floor_id;F.park(s,old);s.clock=1000
  F.resume(s,old);assert(e.expires==1300)
  local count=0;for _,w in ipairs(s.schedule)do if w.idea_geomancer then assert(w.due==1300);count=count+1 end end;assert(count==1)
 end)
 test('Geomancer placement preserves connected required access across genuine seeded floors',function()
  for seed=1,20 do
   local s=G.new_stage(seed,{class_id=Geo.id,multifloor=true});local p=G.player(s);local map=C.map_for(s)
   local before=P.encode(G.snapshot(s))
   for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
    local ok=Geo.preview(s,map,p.position.x+d[1],p.position.y+d[2]);assert(type(ok)=='boolean')
   end
   assert(before==P.encode(G.snapshot(s)))
   for _,stair in ipairs(map.placements.stairs)do p.position.x,p.position.y=stair.x-1,stair.y
    assert(not Geo.preview(s,map,stair.x,stair.y))end
  end
 end)
 test('all class pages use matching bounded visible rows and mouse hitboxes',function()
  local UI=require('src.pixel_ui');local order=require('src.classes').order
  for i,id in ipairs(order)do local first,last=UI.class_page(order,id)
   assert(i>=first and i<=last and last-first<9)
   assert(UI.class_at(300,150+(i-first)*36+10,first,last)==i)
   assert(not UI.class_at(300,150+(last-first+1)*36+10,first,last))
  end
 end)
end
