return function(test,equal)
 local G=require('src.game');local W=require('src.idea_wayfinder');local V=require('src.visibility');local P=require('src.persistence')
 local function trial()
  local s=G.new(37,nil,{class_id=W.id});local p=G.player(s);p.position.x,p.position.y=7,6;V.recompute(s);return s,p
 end
 local function plant(s)assert(G.submit(s,{kind='sweeping_slash',dx=-1,dy=0}));return s.class_special.beacon end
 test('Wayfinder pure beacon preview and paid planting preserve RNG without creating an attack',function()
  local s,p=trial();local snap=G.snapshot(s);local rng=s.rng.state
  for _=1,30 do assert(G.preview(s,'sweeping_slash',-1,0).valid)end;equal(snap,G.snapshot(s))
  local _,_,events=G.submit(s,{kind='sweeping_slash',dx=-1,dy=0})
  assert(s.clock==91 and s.class_special.beacon.x==6 and s.rng.state==rng)
  for _,e in ipairs(events)do assert(e.kind~='AttackPerformed' and e.kind~='AbilityUsed')end
  assert(G.restore(G.snapshot(s)))
 end)
 test('return consumes one beacon and pays normal cost while entering only the real landing',function()
  local s,p=trial();plant(s)
  for _=1,3 do assert(G.submit(s,{kind='move',dx=1,dy=0}))end
  local clock=s.clock;local snap=G.snapshot(s);local preview=G.preview(s,'dash',1,0)
  assert(preview.valid and preview.beacon_return and preview.return_distance==4);equal(snap,G.snapshot(s))
  local _,_,events=G.submit(s,{kind='dash',dx=1,dy=0})
  assert(p.position.x==6 and p.position.y==6 and s.clock==clock+100 and not s.class_special.beacon)
  local entries=0;for _,e in ipairs(events)do if e.kind=='TileEntered' and e.source_id==p.id then
   assert(e.x==6 and e.y==6 and e.forced);entries=entries+1
  end end;assert(entries==1)
  assert(not G.preview(s,'dash',1,0).beacon_return)
 end)
 test('beacons reject occupied stairs diagonal and unexplored destinations without advancing time',function()
  local s,p=trial();local snap=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=-1,dy=-1}));equal(snap,G.snapshot(s))
  G.spawn_enemy(s,'core:enemy/mossbound_guard',6,6,100000);snap=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=-1,dy=0}));equal(snap,G.snapshot(s))
  s,p=trial();local map=require('src.content').map_for(s)
  s.explored[p.position.floor_id].cells[(6-1)*map.width+6]=nil
  assert(not G.preview(s,'sweeping_slash',-1,0).valid)
 end)
 test('blocked and over-distance returns keep their beacon and spend no time',function()
  local s,p=trial();plant(s);G.spawn_enemy(s,'core:enemy/mossbound_guard',6,6,100000)
  local snap=G.snapshot(s);assert(not G.submit(s,{kind='dash',dx=1,dy=0}));equal(snap,G.snapshot(s))
  s,p=trial();plant(s);p.position.x=13;V.recompute(s);snap=G.snapshot(s)
  assert(not G.submit(s,{kind='dash',dx=-1,dy=0}));equal(snap,G.snapshot(s))
 end)
 test('return is bound to one actual generated room and cannot cross floors',function()
  for seed=1,12 do
   local s=G.new_stage(seed,{class_id=W.id,multifloor=true});local p=G.player(s);local map=require('src.content').map_for(s)
   local placed=false
   for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do if not placed and G.preview(s,'sweeping_slash',d[1],d[2]).valid then
    assert(G.submit(s,{kind='sweeping_slash',dx=d[1],dy=d[2]}));placed=true
   end end
   assert(placed);local beacon=s.class_special.beacon
   local other=map.rooms[#map.rooms];p.position.x,p.position.y=other.center.x,other.center.y
   assert(not G.preview(s,'dash',1,0).valid)
   beacon.floor_id=s.floor_order[2];assert(not G.preview(s,'dash',1,0).valid)
  end
 end)
 test('a real return collects an existing supply at the landing and leaves no fake traversal events',function()
  local s,p=trial();plant(s);p.hp=10
  require('src.rewards').spawn_supply(s,'food',p.position.floor_id,6,6,function()end,p.id,nil)
  assert(G.submit(s,{kind='move',dx=1,dy=0}));assert(G.submit(s,{kind='dash',dx=1,dy=0}))
  assert(p.hp==14 and s.drops[1].collected)
 end)
 test('planted beacons and next return resume exactly while forged owners or rooms reject',function()
  local s,p=trial();plant(s);assert(G.submit(s,{kind='move',dx=1,dy=0}))
  local snap=G.snapshot(s);local b=G.restore(P.decode(P.encode(snap)))
  local _,_,a=G.submit(s,{kind='dash',dx=1,dy=0});local _,_,c=G.submit(b,{kind='dash',dx=1,dy=0})
  equal(a,c);equal(G.snapshot(s),G.snapshot(b))
  for _,field in ipairs({'owner_id','room_id'})do local v=P.decode(P.encode(snap));v.class_special.beacon[field]=99999;assert(not pcall(G.restore,v))end
 end)
 test('Wayfinder knife uses its actual two-cell line and normal first-body armor',function()
  local s,p=trial();local a=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
  local b=G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,100000)
  assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(a.hp==8 and b.hp==10 and s.clock==91)
 end)
end
