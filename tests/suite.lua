local U=require('src.util')
local W=require('src.world')
local M=require('src.materials')
local G=require('src.generate')
local P=require('src.particles')
local N=require('src.nav')
local S=require('src.structures')
local J=require('src.jobs')
local A=require('src.colonists')
local Cmd=require('src.commands')
local Sim=require('src.sim')
local Metrics=require('src.metrics')
local H=require('src.history')
local Codec=require('src.codec')
local F=require('tests.fixtures')
local Suite={}
function Suite.run(verbose)
 local report={groups=0,assertions=0,names={}}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'Check failed') end
 local function eq(a,b,why) check(a==b,(why or 'Mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function group(name,fn)
  local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err))
  report.groups=report.groups+1;report.names[#report.names+1]=name
  if verbose then print('PASS  '..name) end
 end
 local function budgets(w)
  local m=Metrics.measure(w);eq(m.waterResidual,0,'water budget');eq(m.mineralResidual,0,'mineral budget');eq(m.foodResidual,0,'food budget')
 end
 group('From-scratch PRNG and noise are repeatable',function()
  local R=require('src.random');local Noise=require('src.noise');local a,b=R.new(7),R.new(7)
  for _=1,100 do eq(a(),b()) end
  eq(Noise.fbm(17,0.2,0.7,3),Noise.fbm(17,0.2,0.7,3));check(Noise.value(17,0.2,0.7)~=Noise.value(18,0.2,0.7))
 end)
 group('Registered seeded generators and valid arrival footprints',function()
  for _,p in ipairs(G.presets) do
   local a,b=G.make(7,p,'challenge',128,80),G.make(7,p,'challenge',128,80)
   eq(Codec.encode(a),Codec.encode(b),'Same seed')
   check(Codec.encode(a.mat)~=Codec.encode(G.make(8,p,'challenge',128,80).mat),'Different seed')
   for _,worker in ipairs(a.workers) do check(N.stand(a,worker.x,worker.y,true),'Spawn must have clearance and support') end
  end
 end)
 group('A powder particle moves at most once per tick',function()
  local w=F.world();W.put(w,20,10,M.SAND);w.tick=1;P.step(w)
  eq(W.get(w,20,10),M.AIR);eq(W.get(w,20,11),M.SAND);eq(W.get(w,20,12),M.AIR)
 end)
 group('Closed particle domain preserves material quantities',function()
  local w=F.world();F.fill(w,12,12,25,16,M.WATER);F.fill(w,15,7,22,9,M.SAND);F.baseline(w)
  for t=1,120 do w.tick=t;P.step(w) end
  budgets(w)
  for x=1,w.width do eq(W.get(w,x,w.height),M.BEDROCK) end
 end)
 group('Lava contact makes steam and rock without destroying water',function()
  local w=F.world();W.put(w,20,24,M.LAVA);W.put(w,21,24,M.WATER);F.baseline(w);w.tick=4;P.step(w)
  eq(w.ledger.rockCooled,1);eq(Metrics.measure(w).materialCounts[M.STEAM],1);budgets(w)
 end)
 group('Ice melts on lava contact and remains in the water budget',function()
  local w=F.world();W.put(w,20,24,M.ICE);W.put(w,21,24,M.LAVA);F.baseline(w);w.tick=1;P.step(w)
  eq(Metrics.measure(w).materialCounts[M.ICE],0);budgets(w)
 end)
 group('Navigation respects body size, walls, and ladders',function()
  local w=F.world();F.fill(w,30,3,31,24,M.ROCK)
  local f=N.flood(w,10,24);eq(f.parent[W.index(w,40,24)],nil,'Cannot cross wall')
  F.fill(w,30,22,31,23,M.AIR)
  eq(N.flood(w,10,24).parent[W.index(w,40,24)],nil,'Two-high hole cannot fit three-high worker')
  F.fill(w,30,22,31,24,M.AIR);check(N.flood(w,10,24).parent[W.index(w,40,24)]~=nil)
  F.fill(w,17,25,20,36,M.AIR)
  for gy=6,9 do S.install(w,5,gy,'ladder') end
  check(N.flood(w,17,24).parent[W.index(w,17,34)]~=nil,'Ladder reaches shaft')
 end)
 group('Current settlers jump only short clear horizontal gaps',function()
  local w=F.world();w.body=1;w.rules.jumpVersion=1
  eq(w.rules.moveEvery,1,'New-world movement cadence is responsive')
  eq(w.rules.planEvery,8,'New-world idle replanning is responsive')
  -- A 2x4 body stands on the single left support at x=12, then clears a
  -- two-cell gap and lands on a supported two-cell footprint at x=15.
  F.fill(w,8,20,22,24,M.AIR);F.fill(w,8,25,22,25,M.AIR)
  W.put(w,12,25,M.ROCK);W.put(w,16,25,M.ROCK)
  check(N.edge(w,12,24,15,24),'Two-cell gap has a clear jump edge')
  local a=F.worker(w,12,24);a.task={path={W.index(w,15,24)},next=1}
  J.act(w,a);eq(a.x,15,'Movement executes the jump as one body-aware edge');eq(a.y,24)
  -- A three-cell gap is not a jump, and low clearance blocks an otherwise
  -- valid short jump.
  F.fill(w,8,20,22,25,M.AIR);W.put(w,12,25,M.ROCK);W.put(w,17,25,M.ROCK);W.put(w,18,25,M.ROCK)
  check(not N.edge(w,12,24,17,24),'Three-cell gap is beyond jump range')
  W.put(w,16,25,M.ROCK);W.put(w,13,20,M.ROCK)
  check(not N.edge(w,12,24,15,24),'A low ceiling blocks the jump')
  W.put(w,13,20,M.AIR);w.rules.jumpVersion=nil
  check(not N.edge(w,12,24,15,24),'Pre-jump world must not gain a new navigation edge')
  w.rules.jumpVersion=1;check(N.edge(w,12,24,15,24),'Current movement rules restore the legal jump')
 end)
 group('Supply planning rejects one-way drops until a ladder is present',function()
  local w=F.world();local a=F.worker(w,12,24)
  F.fill(w,17,25,32,28,M.AIR)
  W.stack(w,'stone',4,25,28)
  local f=N.flood(w,a.x,a.y);local pit=W.index(w,25,28)
  check(f.parent[pit]~=nil,'Pit is reachable by dropping')
  check(not N.returnable(w,f)[pit],'Pit has no return route')
  J.add(w,'build',3,6,'ladder',2);J.plan(w,a)
  check(not a.task,'Worker does not collect a supply that cannot return')
  S.install(w,5,6,'ladder');S.install(w,5,7,'ladder')
  local ff=N.flood(w,a.x,a.y)
  check(N.returnable(w,ff)[pit],'Installed ladder provides the return route')
  J.plan(w,a);check(a.task and a.task.stage=='fetch','Supply task now allowed')
 end)
 group('Changed terrain invalidates previously planned movement' ,function()
  local w=F.world();local a=F.worker(w);W.stack(w,'food',2,50,24);a.hunger=70
  J.plan(w,a);check(a.task and a.task.kind=='eat')
  F.fill(w,20,3,21,24,M.ROCK)
  for t=1,100 do w.tick=t;J.act(w,a) end
  check(a.x<20,'No stale route through wall');check(not a.task,'Blocked task released')
 end)
 group('Excavation yields physical resource piles',function()
  local w=F.world();F.worker(w,21,24);F.fill(w,25,21,28,24,M.ROCK);F.baseline(w)
  local j=J.add(w,'dig',7,6,nil,2);F.steps(w,320)
  eq(j.state,'done');eq(W.totalResource(w,'stone'),16);budgets(w)
 end)
 group('Construction requires hauling before installation',function()
  local w=F.world();F.worker(w,12,24);W.stack(w,'stone',4,10,24);F.baseline(w)
  local j=J.add(w,'build',12,6,'ladder',2)
  F.steps(w,4);eq(w.structures[W.slot(w,12,6)],nil,'Not instantly built')
  F.steps(w,250);check(w.structures[W.slot(w,12,6)]~=nil);eq(j.state,'done');budgets(w)
 end)
 group('Resource reservations prevent double spending',function()
  local w=F.world();F.worker(w,10,24,'A');F.worker(w,13,24,'B');W.stack(w,'stone',6,12,24);F.baseline(w)
  J.add(w,'build',8,6,'bed',2);J.add(w,'build',10,6,'bed',2);F.steps(w,400)
  eq(Metrics.measure(w).structures,1);eq(W.totalResource(w,'stone'),0);budgets(w)
 end)
 group('Cancelling partial construction returns escrow without duplication',function()
  local w=F.world();local a=F.worker(w,10,24);W.stack(w,'stone',32,11,24);F.baseline(w)
  local j=J.add(w,'build',11,6,'wall',2)
  for _=1,200 do Sim.step(w);if j.delivered>0 then break end end
  check(j.delivered>0,'Partial delivery occurred');J.cancel(w,j);budgets(w)
  eq(j.delivered,0);eq(j.state,'cancelled');check(not a.task)
 end)
 group('Walls never overwrite liquids or occupied worker bodies',function()
  local w=F.world();local a=F.worker(w,29,24)
  check(not S.siteClear(w,8,6,'wall'),'Occupied by worker')
  a.x=10;W.put(w,30,24,M.WATER)
  check(not S.siteClear(w,8,6,'wall'),'Occupied by liquid')
  eq(W.get(w,30,24),M.WATER)
 end)
 group('Blueprints do not block water; completed walls do',function()
  local w=F.world();J.add(w,'build',8,6,'wall',2)
  check(not W.blocked(w,30,24));S.install(w,8,6,'wall');check(W.blocked(w,30,24))
 end)
 group('Pump transfers actual water and stops at a blocked outlet',function()
  local w=F.world();local s=S.install(w,8,6,'pump');s.intake={x=35,y=24};s.outlet={x=24,y=20}
  W.put(w,35,24,M.WATER);F.baseline(w)
  check(S.pump(w,s));eq(W.get(w,35,24),M.AIR);eq(W.get(w,24,20),M.WATER);budgets(w)
  W.put(w,35,24,M.WATER);check(not S.pump(w,s),'Occupied outlet refuses transfer')
 end)
 group('Crops consume stored water and create only recorded food',function()
  local w=F.world();F.worker(w,22,24);local s=S.install(w,7,6,'farm');s.tank=12;F.baseline(w)
  F.steps(w,1350);check(w.ledger.foodGrown>=3,'Worker harvested a full growth cycle')
  check(w.ledger.waterUsed>=12);budgets(w)
 end)
 group('Workers physically collect and deliver irrigation water',function()
  local w=F.world();F.worker(w,20,24);local s=S.install(w,7,6,'farm')
  F.fill(w,35,24,38,24,M.WATER);F.baseline(w)
  F.steps(w,400);check(s.tank>0 or w.ledger.waterUsed>0,'Irrigation reached farm');budgets(w)
 end)
 group('Demolition recovers half of costs and preserves stored water',function()
  local w=F.world();F.worker(w,23,24);local s=S.install(w,7,6,'farm');s.tank=5;F.baseline(w)
  J.add(w,'remove',7,6);F.steps(w,150)
  eq(w.structures[W.slot(w,7,6)],nil);eq(w.ledger.demolitionWaste,4);budgets(w)
 end)
 group('Starvation is lethal with no replacement settlers',function()
  local w=F.world();local a=F.worker(w);a.hunger=100;a.hp=3;F.baseline(w)
  F.steps(w,100);check(not a.alive);check(w.extinct);eq(#w.workers,1);eq(a.reason,'starvation');budgets(w)
 end)
 group('Drowning kills a trapped settler and releases carried inventory',function()
  local w=F.world();local a=F.worker(w,20,22);a.breath=1;a.hp=8;a.carry={kind='metal',n=4}
  F.fill(w,19,19,22,23,M.ROCK);F.fill(w,20,20,21,22,M.WATER);F.baseline(w)
  F.steps(w,70);check(not a.alive);eq(a.reason,'drowning');eq(W.totalResource(w,'metal'),4);budgets(w)
 end)
 group('Long falls can be fatal without scripted rescue',function()
  local w=F.world();local a=F.worker(w,20,7);a.hp=12
  F.steps(w,60);check(not a.alive);eq(a.reason,'fall injuries')
 end)
 group('Challenge archive cannot resurrect or edit past states',function()
  local w=F.world();local a=F.worker(w);a.hunger=100;a.hp=3
  local h=H.new(w);h.checkpointEvery=10;h.checkpointLimit=2
  local golden
  for _=1,100 do h:advance();if h.live.tick==20 then golden=Codec.encode(h.live) end end
  check(h.live.extinct);local live=Codec.encode(h.live)
  h:seek(20);while h.seekTarget do h:updateSeek(7) end
  eq(Codec.encode(h.view),golden);check(h.view.workers[1].alive)
  local ok=h:queue({type='order',kind='dig',gx=6,gy=6});check(not ok)
  eq(Codec.encode(h.live),live);h:seek(h.frontier);check(h.view.extinct)
 end)
 group('Full colony replay survives checkpoint eviction',function()
  local w=G.make(12345,'cistern','challenge',128,80);local h=H.new(w);h.checkpointEvery=20;h.checkpointLimit=2
  local gy=(w.home.floor-1)/4
  h:queue({type='order',kind='build',build='farm',gx=9,gy=gy})
  local gold={}
  for _=1,360 do h:advance();if h.live.tick==25 or h.live.tick==175 then gold[h.live.tick]=Codec.encode(h.live) end end
  for _,tick in ipairs({25,175}) do h:seek(tick);while h.seekTarget do h:updateSeek(20) end;eq(Codec.encode(h.view),gold[tick]) end
  eq(#h.checkpoints,2);budgets(h.live)
 end)
 group('Practice branching truncates the old future',function()
  local w=F.world('practice');F.worker(w);local h=H.new(w)
  for _=1,30 do h:advance() end
  h:queue({type='paint',x=30,y=10,material=M.LAVA});h:advance()
  h:seek(10);while h.seekTarget do h:updateSeek(10) end
  check(h:queue({type='paint',x=40,y=10,material=M.WATER}));eq(h.frontier,10);eq(h.commands[31],nil)
  h:advance();eq(Metrics.measure(h.live).lavaCells,0);budgets(h.live)
 end)
 group('Safe data codec rejects executable and malformed input',function()
  local t={a='hello\nworld',n=1.25,b=true,[4]={2,3},z=false}
  eq(Codec.encode(Codec.decode(Codec.encode(t))),Codec.encode(t))
  for _,bad in ipairs({'return os.execute("bad")','t9999999999:','nNaN;','s5:ab','b9','t1:s1:azgarbage'}) do check(not pcall(Codec.decode,bad),'Reject '..bad) end
 end)
 group('Save/restore retains pending input and exact simulation state',function()
  local w=F.world();F.worker(w);W.stack(w,'stone',16,10,24);local h=H.new(w)
  h:queue({type='order',kind='build',build='bed',gx=10,gy=6})
  for _=1,70 do h:advance() end
  h:queue({type='order',kind='dig',gx=12,gy=7})
  local restored=H.fromText(h:saveText())
  eq(Codec.encode(h.live),Codec.encode(restored.live))
  for _=1,100 do h:advance();restored:advance() end
  eq(Codec.encode(h.live),Codec.encode(restored.live));budgets(restored.live)
 end)
 group('Invalid commands and incompatible save versions are rejected',function()
  local w=F.world()
  check(not Cmd.valid(w,{type='paint',x=20,y=20,material=M.WATER}))
  check(not Cmd.valid(w,{type='order',kind='build',build='unknown',gx=3,gy=3}))
  check(not Cmd.valid(w,{type='order',kind='dig',gx=2.5,gy=3}))
  check(not Cmd.valid(w,{type='priority',gx=3,gy=3,value=200}))
  local h=H.new(w);local b=h:bundle();b.version='invalid';check(not pcall(H.restore,b))
 end)
 group('Extreme accepted seeds generate correctly',function()
  for _,seed in ipairs({0,2147483646}) do check(W.validate(G.make(seed,'cistern','challenge',128,80))) end
 end)
 group('An arrival colony builds and harvests from its finite starting well',function()
  local w=G.make(12345,'cistern','challenge',128,80);F.baseline(w)
  local gy=(w.home.floor-1)/4
  J.add(w,'build',9,gy,'farm',2);J.add(w,'build',7,gy,'bed',2)
  F.steps(w,2100)
  check(w.stats.jobsDone>=2,'Colony completed infrastructure')
  check(w.ledger.foodGrown>=3,'Irrigation and harvesting formed a complete food loop')
  eq(W.alive(w),3);budgets(w)
  for _,a in ipairs(w.workers) do check(J.canEat(w,a),'Arrival crew retains access to food after irrigation') end
 end)
 group('Multiple presets maintain accounting and finite worker state',function()
  for _,preset in ipairs(G.presets) do
   local w=G.make(717,preset,'challenge',128,80);F.baseline(w)
   F.steps(w,240);budgets(w);check(W.validate(w))
  end
 end)
 if verbose then print(string.format('%d test groups; %d assertions passed.',report.groups,report.assertions)) end
 return report
end
return Suite
