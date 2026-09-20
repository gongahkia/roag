-- COS-P02 headless acceptance coverage.  GUI-specific contracts live beside
-- the existing mock adapters in tests/region_gui.lua.
local U=require('src.util')
local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local L=require('src.labor')
local Codec=require('src.campaign_codec')
local Random=require('src.campaign_random')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local T={}

local function options(mode,order)
 return {preset='frontier',mode=mode or 'practice',width=128,height=80,layout='hybrid',climate='balanced',
  openness=.48,biomeScale=1,features='living',density=1,crew=3,bodyOrder=order}
end
local function region(seed,mode,order) return Campaign.newRegion(seed or 12345,options(mode,order)) end
local function envelope(siteId,payload) return {scope='site',siteId=siteId,payload=payload} end
local function grant(c,siteId)
 local site=assert(Campaign.site(c,siteId));site.ownerSocietyId=c.society.id
 local w=site.world
 -- Test-only owned-settlement fixture: remove generated encounter instances so
 -- this worker deliberately shares the home site's local ID without a local
 -- entity collision.  Production moons remain unpopulated.
 w.content.flora={};w.content.fauna={};w.content.sites={};w.nextId=1
 local source=U.deep(c.sites[1].world.workers[1]);w.nextId=source.id+1;source.personId=c.nextPersonId;c.nextPersonId=c.nextPersonId+1
 source.x,source.y=w.home.x+4,w.home.y;source.task=nil;source.carry=nil;source.alive=true;source.hp=100;source.hunger=25;source.fatigue=15;source.breath=100
 w.workers[#w.workers+1]=source;w.labor=L.default(w)
 return source
end

function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'P02 assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'P02 mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected rejection') end
 local function group(name,fn) local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err));report.groups=report.groups+1;print('PASS  '..name) end

 group('P02-A seeded region construction is independent of body construction order',function()
  local first=region(12345);local reversed=region(12345,'practice',{3,2,1})
  eq(Codec.encode(first),Codec.encode(reversed),'Region initial state changed with construction order')
  check(#first.sites==3 and first.sites[1].ownerSocietyId==1 and first.sites[2].ownerSocietyId==nil)
 end)
 group('P02-B isolated cosmetic streams do not affect region terrain or recipes',function()
  local before=region(98765);local cosmetic=Random.new(Random.derive(98765,'region/cosmetic/test/v1'))
  for _=1,400 do Random.uniform(cosmetic,19) end
  local after=region(98765)
  eq(Codec.encode(before),Codec.encode(after),'Cosmetic campaign draws changed generated state')
 end)
 group('P02-C campaign RNG vectors, bounds and stream continuation are explicit',function()
  local expected={48271,182605794,1291394886,1914720637,2078669041,407355683,1105902161,854716505,564586691,1596680831};local stream=Random.new(1)
  for _,value in ipairs(expected) do eq(Random.next(stream),value,'Campaign RNG vector') end
  eq(Random.derive(12345,'region/body/1/terrain/v1'),1792981176);eq(Random.derive(12345,'region/body/2/terrain/v1'),123199946);eq(Random.derive(12345,'region/body/3/terrain/v1'),600902363)
  local saved=U.deep(stream);local value=Random.next(stream);eq(Random.next(saved),value,'Campaign stream restore')
  reject(function() Random.new(0) end);reject(function() Random.uniform(Random.new(1),0) end);reject(function() Random.derive(1,'\255') end)
 end)
group('P02-D all generated sites advance on one clock while unviewed',function()
  local c=region(24680);grant(c,2)
  local farmWorld=c.sites[2].world;local farmGX,farmGY=W.tile(farmWorld,farmWorld.home.left+12,farmWorld.home.floor-1)
  local farm=S.install(farmWorld,farmGX,farmGY,'farm');farm.tank=2
  local chargeGX,chargeGY=W.tile(farmWorld,farmWorld.home.left+47,farmWorld.home.floor-1);local charge=S.install(farmWorld,chargeGX,chargeGY,'charge');charge.fuseAt=6
  local fluidWorld=c.sites[3].world;local fluidX,fluidY=fluidWorld.home.left+3,fluidWorld.home.floor-12;W.put(fluidWorld,fluidX,fluidY,M.WATER)
  local fauna;for _,candidate in ipairs(fluidWorld.content.fauna) do if candidate.kind~='sentinel' then fauna=candidate;break end end;assert(fauna,'Fixture needs a non-sentinel creature');fauna.phase=0;fauna.food=1;local faunaCount=#fluidWorld.content.fauna
  local history=History.new(c);local home=history.live.sites[1].world;local moon=history.live.sites[2].world;local remote=history.live.sites[3].world;local hunger=home.workers[1].hunger
  for _=1,2000 do check(history:advance()) end
  for _,site in ipairs(history.live.sites) do eq(site.world.tick,history.live.tick,'Site did not share campaign tick') end
  check(home.workers[1].hunger~=hunger,'Unviewed campaign needs did not advance')
  check(moon.structures[W.slot(moon,farmGX,farmGY)].growth>0,'Owned-fixture farm did not advance off-screen')
  check(moon.structures[W.slot(moon,chargeGX,chargeGY)]==nil,'Off-screen fuse did not resolve')
  check(W.get(remote,fluidX,fluidY)~=M.WATER,'Unviewed fluid did not move')
  check(#remote.content.fauna<faunaCount,'Unviewed fauna/ecology did not advance')
 end)
 group('P02-E local IDs may repeat across sites but commands remain site-bound and stable',function()
  local c=region(54321);local worker=grant(c,2);eq(c.sites[1].world.workers[1].id,worker.id,'Fixture did not retain deliberately colliding local worker ID')
  Campaign.validate(c);local h=History.new(c);local reference=Campaign.clone(h.live)
  check(h:queue(envelope(2,{type='paint',x=12,y=12,material=M.WATER})));check(h:advance())
  Campaign.step(reference);eq(Codec.encode(h.live.sites[1].world),Codec.encode(reference.sites[1].world),'Site-2 command changed site 1')
  local reordered=Campaign.clone(h.live);reordered.sites[1],reordered.sites[3]=reordered.sites[3],reordered.sites[1]
  Campaign.validate(reordered);Campaign.step(reordered);Campaign.step(h.live)
  for id=1,3 do eq(Codec.encode(Campaign.site(reordered,id).world),Codec.encode(Campaign.site(h.live,id).world),'Storage order changed site processing') end
 end)
 group('P02-F moons are unpopulated frontier worlds without legacy schema changes',function()
  local c=region(13579);for id=2,3 do local w=c.sites[id].world;eq(#w.workers,0);eq(#w.items,0);check(next(w.structures)==nil);check(w.generation and w.generation.version=='frontier-v2') end
  local legacy=require('src.generate').make(7,'cistern','challenge',128,80);check(legacy.frontier==nil and legacy.workers[1].personId==nil,'Campaign metadata leaked into local generation')
 end)
 group('P02-G empty home is not campaign extinction while another owned site has crew',function()
  local c=region(24681,'challenge');local lunar=grant(c,2);for _,worker in ipairs(c.sites[1].world.workers) do worker.alive=false end;Campaign.validate(c)
  local h=History.new(c);check(not Campaign.extinct(h.live));check(h:queue(envelope(1,{type='order',kind='dig',gx=6,gy=6})), 'Unstaffed owned home command was rejected')
  h.live.sites[2].world.workers[1].alive=false;check(Campaign.extinct(h.live));check(not h:queue(envelope(1,{type='order',kind='dig',gx=7,gy=6})), 'Global extinction accepted challenge input')
 end)
 group('P02-H whole-region save, seek and branch preserve every site and ownership',function()
  local c=region(11223,'practice');grant(c,2);Campaign.validate(c);local h=History.new(c)
  for tick=1,240 do if tick==20 then check(h:queue(envelope(2,{type='paint',x=12,y=12,material=M.WATER}))) end;check(h:advance()) end
  local saved=h:saveText();local restored=History.fromText(saved);eq(Codec.encode(restored.live),Codec.encode(h.live),'Regional save/load changed live state')
  eq(restored.live.region.bodies[1].name,'Home Planet');eq(restored.live.region.bodies[2].name,'Moon I');eq(restored.live.region.bodies[3].name,'Moon II')
  restored:seek(37);while restored.seekTarget do restored:updateSeek(17) end;check(restored.view.sites[2].ownerSocietyId==1,'Archive lost historical ownership')
  check(restored:queue(envelope(2,{type='paint',x=13,y=12,material=M.AIR})));check(restored.frontier==37,'Practice branch did not truncate campaign frontier')
  local verified,why=History.fromText(saved):verifyReplay(1000);check(verified,why)
 end)
 group('P02 validation rejects duplicate IDs, holes, versions and tick divergence',function()
  local c=region(77889);local duplicate=Campaign.clone(c);duplicate.sites[3].id=2;reject(function() Campaign.validate(duplicate) end)
  local hole=Campaign.clone(c);hole.sites[2]=nil;reject(function() Campaign.validate(hole) end)
  local badFeature=Campaign.clone(c);badFeature.features.region=2;reject(function() Campaign.validate(badFeature) end)
  local badTick=Campaign.clone(c);badTick.sites[3].world.tick=1;reject(function() Campaign.validate(badTick) end)
  local badPerson=Campaign.clone(c);badPerson.sites[2].world.workers[1]=U.deep(badPerson.sites[1].world.workers[1]);badPerson.sites[2].world.workers[1].id=badPerson.sites[2].world.nextId;badPerson.sites[2].world.nextId=badPerson.sites[2].world.nextId+1;reject(function() Campaign.validate(badPerson) end)
 end)
 print(report.groups..' region groups; '..report.assertions..' assertions passed.')
 return report
end
return T
