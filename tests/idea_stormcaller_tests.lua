return function(test,equal)
 local G=require('src.game');local S=require('src.idea_stormcaller');local P=require('src.persistence')
 local function trial()
  local s=G.new(37,nil,{class_id=S.id});local p=G.player(s);p.position.x,p.position.y=7,6
  local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,100000);return s,p,e
 end
 local function charge(s,n)for _=1,n do assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))end end
 test('Stormcaller earns at most three charges from actual direct bolt hits and pure previews earn none',function()
  local s,p,e=trial();local snap=G.snapshot(s)
  for _=1,30 do G.preview(s,'basic_attack',1,0);G.preview(s,'sweeping_slash',1,0)end;equal(snap,G.snapshot(s))
  charge(s,4);assert(p.storm_charge==3 and e.hp==6)
  assert(G.submit(s,{kind='basic_attack',dx=-1,dy=0}));assert(p.storm_charge==3)
 end)
 test('discharge consumes capped charge and resolves one fixed cross on simulation time',function()
  local s,p,e=trial();charge(s,3);local clock=s.clock
  local aim=G.preview(s,'sweeping_slash',1,0);assert(aim.valid and #aim.cells==4)
  local _,_,events=G.submit(s,{kind='sweeping_slash',dx=1,dy=0})
  assert(p.storm_charge==0 and not s.class_special.storm and not G.entity(s,e.id) and s.clock==clock+150)
  local n=0;for _,v in ipairs(events)do if v.kind=='StormDischarged'then assert(v.time==clock+100 and v.damage==8);n=n+1 end end;assert(n==1)
 end)
 test('uncharged or obstructed discharges are rejected without spending an action',function()
  local s,p=trial();local snap=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));equal(snap,G.snapshot(s))
  charge(s,1);p.position.x,p.position.y=8,4;snap=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=1}));equal(snap,G.snapshot(s))
 end)
 test('grounding Dash spends charge into exactly one positive-damage ward',function()
  local s,p=trial();charge(s,2);local before=s.clock
  assert(G.submit(s,{kind='dash',dx=0,dy=1}));assert(p.storm_charge==0 and p.storm_ward and s.clock==before+130)
  assert(not S.absorb(p,0) and p.storm_ward)
  local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,8,s.clock)
  local hp=p.hp;assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}))
  assert(p.hp==hp and not p.storm_ward)
  for _=1,4 do assert(G.submit(s,{kind='wait'}))end;assert(p.hp<hp)
 end)
 test('secondary effects cannot build charge and delayed discharge cannot trigger extra Echo',function()
  local s,p,e=trial();charge(s,1)
  G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
  assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));assert(not next(s.delayed_effects) and p.storm_charge==0)
  assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(p.storm_charge==1)
  assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}));assert(p.storm_charge==1)
 end)
 test('a fast Stormcaller retains a locked pending cross and cannot track a moved target',function()
  local s,p,e=trial();charge(s,2);p.speed=300
  local before=s.clock;assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));assert(s.clock==before+50)
  local q=assert(s.class_special.storm);local threats=G.pending_strikes(s);assert(#threats==1 and threats[1].due==before+100)
  e.position.x,e.position.y=11,7;local hp=e.hp
  for _=1,3 do assert(G.submit(s,{kind='wait'}))end;assert(e.hp==hp and not s.class_special.storm)
 end)
 test('pending storm and held ward roundtrip with exact effects and forged resources reject',function()
  local s,p=trial();charge(s,2);p.speed=300;assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}))
  local snap=G.snapshot(s);local b=G.restore(P.decode(P.encode(snap)))
  for _=1,4 do local _,_,x=G.submit(s,{kind='wait'});local _,_,y=G.submit(b,{kind='wait'});equal(x,y);equal(G.snapshot(s),G.snapshot(b))end
  for _,change in ipairs({function(v)for _,e in ipairs(v.entities)do if e.id==v.player_id then e.storm_charge=4 end end end,
   function(v)v.class_special.storm.damage=-1 end,
   function(v)for i=#v.schedule,1,-1 do if v.schedule[i].idea_storm then table.remove(v.schedule,i)end end end})do
   local v=P.decode(P.encode(snap));change(v);assert(not pcall(G.restore,v))
  end
 end)
end
