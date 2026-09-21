-- Focused COS-G02 contracts.  The long trace exercises the whole campaign;
-- these groups keep the safety and custody boundaries small enough to diagnose.
local F=require('tests.fixtures')
local W=require('src.world')
local M=require('src.materials')
local N=require('src.nav')
local S=require('src.structures')
local J=require('src.jobs')
local A=require('src.colonists')
local Campaign=require('src.campaign')
local E=require('src.equipment')
local EC=require('src.equipment_commands')
local Suite={}

local function one()
 local source=F.world('practice');F.worker(source,12,24,'A')
 return Campaign.new(source,{body=true,visibility=true,equipment=true,safe_excavation=true})
end
local function advance(c,n) for _=1,n do Campaign.step(c) end end
function Suite.run()
 local r={groups=0,assertions=0}
 local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end
 local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' vs '..tostring(b)) end
 local function group(name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end

 group('G02-A/C post-dig support is checked at mutation and remains pending without a coil',function()
  local c=one();local w=c.sites[1].world;local j=J.add(w,'dig',3,7,nil,3);advance(c,100)
  check(j.state=='open','Unsafe dig was cancelled instead of retained');eq(W.get(w,12,25),M.ROCK,'Miner removed its own support');check(j.reason:match('Unsafe descent'),'Unsafe dig did not explain its rope requirement');Campaign.validate(c)
 end)
 group('G02-B automatic rope attachment preserves the original downward dig',function()
  local c=one();local w=c.sites[1].world;local a=w.workers[1]
  -- Only the right foot remains supported. The shaft below is deliberately
  -- deeper than a controlled drop, so there is no alternate safe pose.
  for y=25,55 do for x=9,16 do W.put(w,x,y,M.AIR) end end
  W.put(w,13,25,M.ROCK);E.create(c,1,'rope_coil',12,24)
  local j=J.add(w,'dig',4,7,nil,3);advance(c,180)
  eq(j.state,'done');eq(W.get(w,13,25),M.AIR,'Original downward dig did not resume after rope assist');eq(#w.ropes,1,'Automatic descent did not install a physical rope')
  check(E.find(c,w.ropes[1].itemId).state=='rope','Automatic rope duplicated or lost its physical coil');eq(a.fall,0,'Ordinary rope-assisted dig produced an uncontrolled fall');Campaign.validate(c)
 end)
 group('G02-D ropes are physical two-cell climb lanes with bounded recovery',function()
  local c=one();local w=c.sites[1].world;local a=w.workers[1];a.x=12
  for y=21,44 do W.put(w,9,y,M.AIR);W.put(w,10,y,M.AIR) end
  E.create(c,1,'rope_coil',12,24);local j=J.add(w,'rope',3,6,nil,3);advance(c,150)
  eq(j.state,'done');eq(#w.ropes,1);eq(w.ropes[1].length,24);check(N.ladder(w,9,30),'Rope did not create a climb lane');check(not N.ladder(w,11,30),'Rope widened beyond its two-cell lane')
 local remove={type='remove_rope',ropeId=w.ropes[1].id,priority=3};assert(EC.valid(c,c.sites[1],remove));assert(EC.apply(c,c.sites[1],remove));advance(c,120);eq(#w.ropes,0);eq(E.forSite(c,1,'rope_coil','loose')[1].kind,'rope_coil');Campaign.validate(c)
end)
 group('G02 ropes can be unfurled upward from a lower anchor',function()
  local c=one();local w=c.sites[1].world;local a=w.workers[1]
  -- The first, already-installed line is fixture infrastructure that gives the
  -- worker a real safe way down to the lower ledge. The command under test
  -- fetches a second physical coil there and unfurls it upward.
  for y=21,44 do for x=12,21 do W.put(w,x,y,M.AIR) end end
  for x=12,21 do W.put(w,x,45,M.ROCK) end
  local access=E.create(c,1,'rope_coil',12,24);assert(E.reserve(access,a));E.carry(c,access,a);E.installRope(c,w,1,access,12,25,20)
  E.create(c,1,'rope_coil',18,44)
  local payload={type='place_rope',gx=5,gy=11,direction='up',priority=3};assert(EC.valid(c,c.sites[1],payload));assert(EC.apply(c,c.sites[1],payload));advance(c,180)
  eq(#w.ropes,2);local rope=w.ropes[2];eq(rope.anchorY,21);eq(rope.length,24);check(N.ladder(w,17,30),'Upward rope did not create the shared climb lane');Campaign.validate(c)
 end)
group('G02-E pickaxe contribution is fixed by physical material class',function()
  local c=one();local a=c.sites[1].world.workers[1];eq(E.digWork(c,a,M.ROCK),1)
  local pick=E.create(c,1,'pickaxe',a.x,a.y);E.equip(c,pick,a)
  eq(E.digWork(c,a,M.SOIL),2);eq(E.digWork(c,a,M.SAND),2);eq(E.digWork(c,a,M.ICE),2);eq(E.digWork(c,a,M.ROCK),3);eq(E.digWork(c,a,M.ORE),3)
 end)
 group('G02-F/I unique custody and G02-H cargo capacity retain actual tool IDs',function()
  local c=Campaign.newRegion(9821,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true})
  eq(#c.equipment.items,6);local item=c.equipment.items[1];local payload={type='load_tool',equipmentId=item.id,craftId=1,priority=3};assert(EC.valid(c,c.sites[1],payload));assert(EC.apply(c,c.sites[1],payload));advance(c,500);eq(item.state,'craft');eq(item.craftId,1);eq(E.craftCount(c,1),1)
  local unload={type='unload_tool',equipmentId=item.id,craftId=1,priority=3};assert(EC.valid(c,c.sites[1],unload));assert(EC.apply(c,c.sites[1],unload));advance(c,400);eq(item.state,'loose');eq(item.siteId,1);Campaign.validate(c)
 end)
 group('G02-G real fabrication consumes escrowed metal and yields one pickaxe',function()
  local c=one();local w=c.sites[1].world;local a=w.workers[1];a.x=16;local bench=S.install(w,3,6,'tool_bench');W.stack(w,'metal',2,16,24)
  local payload={type='fabricate',slot=W.slot(w,3,6),kind='pickaxe',priority=3};assert(EC.valid(c,c.sites[1],payload));assert(EC.apply(c,c.sites[1],payload));advance(c,500)
  local job=w.jobs[1];eq(job.state,'done');eq(job.delivered,2);eq(job.progress,120);eq(E.forSite(c,1,'pickaxe','loose')[1].kind,'pickaxe');Campaign.validate(c)
 end)
 group('G02-J/K panic hysteresis and G02 fall-damage bands are deterministic',function()
  local c=one();local w=c.sites[1].world;local a=w.workers[1]
  E.stress(c,a,80,w.tick);check(a.panic);a.stress=50;E.recover(c,a,w);check(not a.panic,'Panic did not recover at 50')
  a.fall=8;local hp=a.hp;A.step(w,{campaign=c,siteId=1});eq(a.hp,hp-15,'5..8-cell fall band');a.fall=12;hp=a.hp;A.step(w,{campaign=c,siteId=1});eq(a.hp,hp-30,'9..12-cell fall band')
 end)
 group('G02-M established demolition charges arm through work and reject panic',function()
  local c=one();local w=c.sites[1].world;local a=w.workers[1];local charge=S.install(w,5,6,'charge');local j=J.add(w,'arm',5,6,nil,3);advance(c,180);check(charge.fuseAt,'Charge did not arm through its field-work job')
  local c2=one();local w2=c2.sites[1].world;local a2=w2.workers[1];a2.panic=true;a2.stress=80;S.install(w2,5,6,'charge');local panic=J.add(w2,'arm',5,6,nil,3);advance(c2,80);check(not w2.structures[W.slot(w2,5,6)].fuseAt,'Panicked worker armed a charge');check(panic.reason:match('Panicked'),'Panic arm rejection was not readable')
 end)
 group('G02-O/P legacy campaigns retain no tools, ropes, or stress fields',function()
  local source=F.world('practice');F.worker(source,12,24,'Legacy');local legacy=Campaign.new(source,{body=true,visibility=true});Campaign.validate(legacy)
  check(legacy.features.equipment==nil and legacy.equipment==nil);check(legacy.sites[1].world.ropes==nil);check(legacy.sites[1].world.workers[1].stress==nil)
 end)
 print(r.groups..' G02 groups; '..r.assertions..' assertions passed.');return r
end
return Suite
