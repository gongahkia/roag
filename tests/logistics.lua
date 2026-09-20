-- COS-P03 headless acceptance coverage.  The GUI flow is isolated in
-- tests/logistics_gui.lua beside the existing LÖVE mock adapters.
local U=require('src.util')
local W=require('src.world')
local M=require('src.materials')
local Labor=require('src.labor')
local Codec=require('src.campaign_codec')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local T={}

local function options(mode)
 return {preset='frontier',mode=mode or 'practice',width=128,height=80,layout='hybrid',climate='balanced',
  openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true}
end
local function newCampaign(seed,mode) return Campaign.newRegion(seed or 424242,options(mode)) end
local function quietSource(c)
 local e=c.sites[1].world.content
 if e then e.flora={};e.fauna={};e.sites={};e.ruins={};e.signals={};e.discoveries={};e.observed={} end
 return c
end
local function history(seed,mode) return History.new(quietSource(newCampaign(seed,mode))) end
local function campaignCommand(typeName,fields)
 local c={scope='campaign',type=typeName}
 for key,value in pairs(fields) do c[key]=value end
 return c
end
local function worker(c,index) return c.sites[1].world.workers[index or 1] end
local function advance(h,n)
 for _=1,n do assert(h:advance()) end
end
local function craft(c,id) return assert(Logistics.craft(c,id or 1)) end
local function manifest(c,id) return assert(Logistics.manifest(c,id or craft(c).activeManifestId)) end
local function prepare(h,passengers,cargo,destination)
 local c=h.live;local ids={}
 for _,index in ipairs(passengers or {1}) do ids[#ids+1]=worker(c,index).personId end
 assert(h:queue(campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=destination or 2,passengers=ids,cargo=cargo or {food=2,metal=1}})))
end
local function resourceTotals(c)
 return Logistics.reconciliation(c,1).total
end
local function grantLunarWorker(c)
 local site=assert(Campaign.site(c,2));site.ownerSocietyId=c.society.id
 local w=site.world;w.content.flora={};w.content.fauna={};w.content.sites={};w.nextId=1
 local source=U.deep(c.sites[1].world.workers[1]);source.personId=c.nextPersonId;c.nextPersonId=c.nextPersonId+1
 source.x,source.y=w.home.x+4,w.home.y;source.task=nil;source.carry=nil;source.alive=true;source.hp=100;source.hunger=25;source.fatigue=15;source.breath=100
 w.nextId=source.id+1;w.workers={source};w.labor=Labor.default(w)
 return source
end
local function addFixtureCraft(c,siteId)
 local l=c.logistics;local source=assert(Campaign.site(c,siteId));local id=l.nextCraftId;l.nextCraftId=id+1
 l.crafts[#l.crafts+1]={id=id,ownerSocietyId=1,seats=l.rules.seats,capacity=l.rules.cargoCapacity,dockedSiteId=siteId,
  anchor={x=source.world.home.x,y=source.world.home.y},cargo={},activeManifestId=nil}
 return id
end

function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'P03 assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'P03 mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected rejection') end
 local function group(name,fn) local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err));report.groups=report.groups+1;print('PASS  '..name) end

 group('P03-A new logistics campaigns start with one empty docked craft and no grants',function()
  local logistics=newCampaign(101);local p02=Campaign.newRegion(101,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3})
  eq(logistics.features.logistics,1);eq(#logistics.logistics.crafts,1);local vehicle=craft(logistics)
  eq(vehicle.seats,3);eq(vehicle.capacity,24);eq(next(vehicle.cargo),nil);eq(Codec.encode(logistics.sites[1].world),Codec.encode(p02.sites[1].world),'Craft initialization changed local starter resources or crew')
 end)

 group('P03-B physical loading and unloading conserve each resource exactly once',function()
  local h=history(102);local before=resourceTotals(h.live);prepare(h,{1},{food=3,metal=2});advance(h,420)
  local vehicle=craft(h.live);eq(vehicle.cargo.food,3);eq(vehicle.cargo.metal,2);local aboard=resourceTotals(h.live);eq(aboard.food,before.food);eq(aboard.metal,before.metal)
  local m=manifest(h.live);assert(h:queue(campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers=m.passengers,cargo={food=1,metal=1}})));advance(h,1)
  assert(h:queue(campaignCommand('unload_cargo',{sourceSiteId=1,craftId=1,resource='food',amount=2})));advance(h,120)
  eq(vehicle.cargo.food,1);local after=resourceTotals(h.live);eq(after.food,before.food);eq(after.metal,before.metal)
  local metrics=Campaign.metrics(h.live).sites[1].metrics;eq(metrics.cargo.food,1);eq(metrics.reconciliation.total.food,before.food)
 end)

 group('P03-C cargo loading uses reachable local piles and stops safely when unavailable',function()
  local h=history(103);prepare(h,{1},{water=1,metal=1});advance(h,240)
  local vehicle=craft(h.live);eq(vehicle.cargo.water or 0,0,'Loading created water without a physical item source');eq(vehicle.cargo.metal,1)
  local waiting=false;for _,job in ipairs(h.live.sites[1].world.jobs) do if job.kind=='load' and job.logistics and job.logistics.resource=='water' then waiting=job.reason:match('accessible')~=nil end end
  check(waiting,'Unavailable local water did not remain a blocked physical load job')
  local routed=history(1031);prepare(routed,{1},{food=8,metal=1});local w=routed.live.sites[1].world;local carrier
  for _=1,160 do
   advance(routed,1);for _,a in ipairs(w.workers) do if a.carry and a.task and a.task.kind=='cargo' and a.task.path[a.task.next] then carrier=a;break end end
   if carrier then break end
  end
  check(carrier,'Fixture did not reach a routed cargo carry stage');local nx,ny=W.xy(w,carrier.task.path[carrier.task.next]);local before=resourceTotals(routed.live);W.put(w,nx,ny-1,M.WATER);require('src.jobs').act(w,carrier,{campaign=routed.live,siteId=1})
  local after=resourceTotals(routed.live);eq(after.food,before.food);eq(after.metal,before.metal);check(not carrier.carry,'Flooded route did not release carried cargo through the existing drop path')
 end)

 group('P03-D malformed and cross-source logistics commands reject without mutation',function()
  local h=history(104);local before=Codec.encode(h.live)
  local bad={
   campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={worker(h.live,1).personId,worker(h.live,1).personId},cargo={food=1}}),
   campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={},cargo={imaginary=1}}),
   campaignCommand('prepare_expedition',{sourceSiteId=2,craftId=1,destinationSiteId=1,passengers={},cargo={food=1}}),
   campaignCommand('unload_cargo',{sourceSiteId=1,craftId=1,resource='food',amount=25}),
  }
  for _,command in ipairs(bad) do check(not h:queue(command),'Invalid logistics command queued') end
  eq(Codec.encode(h.live),before)
  prepare(h,{1},{food=2,metal=1});advance(h,280);local m=manifest(h.live)
  assert(h:queue(campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers=m.passengers,cargo={food=1,metal=1}})))
  assert(h:queue(campaignCommand('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})))
  advance(h,1);eq(manifest(h.live).cargo.food,1,'Earlier same-tick edit was not applied safely')
 end)

 group('P03-E repeated plans are idempotent and cancellation preserves completed custody',function()
  local h=history(105);local id=worker(h.live,1).personId;local plan=campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={id},cargo={food=2,metal=1}})
  assert(h:queue(plan));assert(h:queue(plan));advance(h,1);eq(#h.live.logistics.manifests,1);eq(h.live.logistics.nextManifestId,2)
  advance(h,300);local vehicle=craft(h.live);check((vehicle.cargo.food or 0)>0,'Loading did not begin before cancellation');local loaded=vehicle.cargo.food
  local m=manifest(h.live);assert(h:queue(campaignCommand('cancel_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})));advance(h,1);eq(vehicle.cargo.food,loaded);eq(#h.live.logistics.manifests,0)
  check(not h:queue(campaignCommand('cancel_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})),'Inactive cancellation was accepted')
 end)

 group('P03-F needs and death interrupt cargo work without duplicating carried items',function()
  local h=history(106);prepare(h,{1},{food=8,metal=1})
  local w=h.live.sites[1].world;local carrier
  for _=1,160 do
   advance(h,1);for _,a in ipairs(w.workers) do if a.carry then carrier=a;break end end
   if carrier then break end
  end
  check(carrier and carrier.carry,'Fixture did not reach a real cargo carry stage');local before=resourceTotals(h.live);carrier.hp=0;advance(h,2)
  check(not carrier.alive,'Carrying worker did not die under existing injury rules');local after=resourceTotals(h.live);eq(after.food,before.food);eq(after.metal,before.metal)
  Campaign.validate(h.live)
 end)

 group('P03-G staged saves, seeks and practice branches preserve cargo state',function()
  local h=history(107);prepare(h,{1},{food=3,metal=1});advance(h,90);local saved=h:saveText();local restored=History.fromText(saved)
  advance(h,240);advance(restored,240);eq(Codec.encode(restored.live),Codec.encode(h.live),'Save/load continuation changed logistics state')
  restored:seek(40);while restored.seekTarget do restored:updateSeek(17) end;check(restored:queue(campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=3,passengers={worker(restored.view,1).personId},cargo={food=1,metal=1}})))
  eq(restored.frontier,40);local verified,why=History.fromText(saved):verifyReplay(1000);check(verified,why)
 end)

 group('P03-H readiness is pure and fails when cargo or assembly conditions differ',function()
  local h=history(108);prepare(h,{1},{food=2,metal=1});advance(h,360);local m=manifest(h.live);local before=Codec.encode(h.live);local ready,why=Logistics.readiness(h.live,m)
  check(not ready and why:match('assembled'),'Readiness ignored unassembled passenger');eq(Codec.encode(h.live),before,'Readiness mutated campaign state')
  assert(h:queue(campaignCommand('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})));advance(h,100);local person=worker(h.live,1);person.x=person.x+1
  ready=Logistics.readiness(h.live,m);check(not ready,'Readiness ignored changed passenger position')
 end)

 group('P03-I site-local IDs and fixture crafts do not share reservations or cargo',function()
  local c=newCampaign(109);local lunar=grantLunarWorker(c);local craft2=addFixtureCraft(c,2);Campaign.validate(c);local h=History.new(c)
  check(h:queue(campaignCommand('prepare_expedition',{sourceSiteId=2,craftId=craft2,destinationSiteId=1,passengers={lunar.personId},cargo={}})))
  check(not h:queue(campaignCommand('prepare_expedition',{sourceSiteId=2,craftId=1,destinationSiteId=1,passengers={lunar.personId},cargo={}})))
  advance(h,1);eq(craft(h.live,1).activeManifestId,nil);check(craft(h.live,craft2).activeManifestId~=nil)
 end)

 group('P03-J logistics uses hauling eligibility and leaves labour policy unchanged',function()
  local h=history(110);local w=h.live.sites[1].world
  for _,person in ipairs(w.labor.people) do person.prefs.haul=0 end
  prepare(h,{1},{food=2,metal=1});advance(h,240);eq(craft(h.live).cargo.food or 0,0,'Hauling OFF was bypassed for loading')
  for _,person in ipairs(w.labor.people) do eq(person.prefs.haul,0,'Cargo work changed labour preference') end
 end)

 group('P03-K all selected crew can load before explicit assembly',function()
  local h=history(111);prepare(h,{1,2,3},{food=3,metal=1});advance(h,420);local vehicle=craft(h.live);eq(vehicle.cargo.food,3);eq(vehicle.cargo.metal,1)
  for _,a in ipairs(h.live.sites[1].world.workers) do check(a.alive and a.personId,'Loading detached or replaced a selected person') end
  local m=manifest(h.live);assert(h:queue(campaignCommand('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})));advance(h,160);local ready,why=Logistics.readiness(h.live,m);check(ready,why)
 end)

 group('P03-L edits, surplus unloading and reservation bounds stay finite',function()
  local h=history(112);prepare(h,{1},{food=4,metal=2});advance(h,240);local m=manifest(h.live);local id=m.id
  check(not h:queue(campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers=m.passengers,cargo={food=25}})),'Over-capacity edit queued')
  assert(h:queue(campaignCommand('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers=m.passengers,cargo={food=1,metal=1}})));advance(h,1);eq(manifest(h.live).id,id);check(manifest(h.live).revision==2)
  local vehicle=craft(h.live);if (vehicle.cargo.food or 0)>1 then
   local excess=vehicle.cargo.food-1;assert(h:queue(campaignCommand('unload_cargo',{sourceSiteId=1,craftId=1,resource='food',amount=excess})));check(h:queue(campaignCommand('unload_cargo',{sourceSiteId=1,craftId=1,resource='food',amount=excess})));advance(h,160)
  end
  check(#h.live.logistics.operations<=1 and #h.live.logistics.manifests<=1,'Repeated edits accumulated active logistics records');Campaign.validate(h.live)
 end)

 group('P03-M assembly directives respect later rally and cancellation boundaries',function()
  local h=history(113);prepare(h,{1},{food=2,metal=1});advance(h,360);local m=manifest(h.live);assert(h:queue(campaignCommand('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})));advance(h,1)
  local person=worker(h.live,1);assert(h:queue({scope='site',siteId=1,payload={type='rally',worker=person.id,x=person.x,y=person.y}}));advance(h,1);check(person.directive and person.directive.kind==nil,'Later rally did not replace assembly intent')
  assert(h:queue(campaignCommand('cancel_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})));advance(h,1);check(person.directive,'Cancellation erased a newer rally directive')
 end)

 group('P03-O feature gates, validation and map boundaries remain explicit',function()
  local old=Campaign.newRegion(114,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3})
  check(old.features.logistics==nil and old.logistics==nil,'P02 campaign silently gained logistics')
  local bad=Campaign.clone(newCampaign(115));bad.features.region=nil;reject(function() Campaign.validate(bad) end)
  local h=history(116);prepare(h,{1},{food=2,metal=1});advance(h,40);local restored=History.fromText(h:saveText());eq(Codec.encode(restored.live),Codec.encode(h.live));local map=require('src.mapfile').fromWorld(h.live.sites[1].world);check(map.logistics==nil and map.frontier==nil,'Map template exported campaign logistics')
 end)
 print(report.groups..' logistics groups; '..report.assertions..' assertions passed.')
 return report
end
return T
