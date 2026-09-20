-- COS-P04 headless contracts.  The mocked presentation wiring is intentionally
-- separate in tests/travel_gui.lua; neither establishes a real LÖVE window pass.
local U=require('src.util')
local W=require('src.world')
local M=require('src.materials')
local Codec=require('src.campaign_codec')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local Travel=require('src.travel')
local Colonists=require('src.colonists')
local T={}

local function options(mode)
 return {preset='frontier',mode=mode or 'practice',width=128,height=80,layout='hybrid',climate='balanced',
  openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true}
end
local function quiet(c)
 for _,site in ipairs(c.sites) do
  local content=site.world.content
  content.flora={};content.fauna={};content.sites={};content.ruins={};content.signals={};content.discoveries={};content.observed={}
 end
 return c
end
local function newCampaign(seed,mode) return quiet(Campaign.newRegion(seed or 7201,options(mode))) end
local function history(seed,mode) return History.new(newCampaign(seed,mode)) end
local function command(kind,fields)
 local out={scope='campaign',type=kind};for key,value in pairs(fields) do out[key]=value end;return out
end
local function advance(h,n)
 for _=1,n do assert(h:advance()) end
end
local function craft(c) return assert(Logistics.craft(c,1)) end
local function worker(c,index,siteId) return assert(Campaign.site(c,siteId or 1)).world.workers[index or 1] end
local function personAt(c,personId,siteId)
 for _,candidate in ipairs(assert(Campaign.site(c,siteId)).world.workers) do if candidate.personId==personId then return candidate end end
end
local function manifest(c) return assert(Logistics.manifest(c,craft(c).activeManifestId)) end
local function prepare(h,cargo,passengers,destination,source)
 local c=h.live;source=source or 1;local ids={}
 for _,index in ipairs(passengers or {1}) do ids[#ids+1]=worker(c,index,source).personId end
 assert(h:queue(command('prepare_expedition',{sourceSiteId=source,craftId=1,destinationSiteId=destination or 2,passengers=ids,cargo=cargo or {food=3,metal=2}})))
end
local function readyLaunch(h,cargo,passengers,destination,source)
 prepare(h,cargo,passengers,destination,source);advance(h,460)
 local m=manifest(h.live);assert(h:queue(command('assemble_expedition',{sourceSiteId=source or 1,craftId=1,manifestId=m.id})));advance(h,180)
 m=manifest(h.live);local ready,why=Logistics.readiness(h.live,m);assert(ready,why)
 assert(h:queue(command('launch_expedition',{sourceSiteId=source or 1,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision})));advance(h,1)
 return craft(h.live)
end
local function landSoon(h)
 local vehicle=craft(h.live);assert(vehicle.journey);vehicle.journey.remainingTicks=1;advance(h,1);return vehicle
end
local function countResource(c,kind)
 local total=0
 for _,site in ipairs(Campaign.sites(c)) do total=total+W.totalResource(site.world,kind) end
 for _,vehicle in ipairs(c.logistics.crafts) do total=total+(vehicle.cargo[kind] or 0) end
 return total
end
local function fillLanding(world)
 local home=assert(world.home)
 for y=math.max(1,home.y-10),math.min(world.height,home.y+10) do
  for x=math.max(1,home.x-10),math.min(world.width,home.x+10) do W.put(world,x,y,M.ROCK) end
 end
end

function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'P04 assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'P04 mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected rejection') end
 local function group(name,fn) local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err));report.groups=report.groups+1;print('PASS  '..name) end

 group('P04-A actual commands move people and cargo through departure, landing, unload, and return preparation',function()
  local h=history(7201);local first=worker(h.live);local personId=first.personId;local sourceFood=countResource(h.live,'food');local vehicle=readyLaunch(h,{food=3,metal=2},{1},2)
  eq(#h.live.sites[1].world.workers,2,'Departure did not detach exactly one source worker');eq(#vehicle.passengers,1);eq(vehicle.passengers[1].personId,personId);eq(vehicle.cargo.food,3);eq(vehicle.cargo.metal,1)
  landSoon(h);eq(vehicle.dockedSiteId,2);eq(vehicle.journey,nil);eq(h.live.sites[2].ownerSocietyId,1);eq(#vehicle.passengers,0)
  local arrived=worker(h.live,1,2);eq(arrived.personId,personId);check(arrived.id<h.live.sites[2].world.nextId,'Arrival did not allocate a destination-local ID')
  assert(h:queue(command('unload_cargo',{sourceSiteId=2,craftId=1,resource='food',amount=1})));advance(h,180);eq(vehicle.cargo.food,2);eq(countResource(h.live,'food'),sourceFood,'Docked unload changed physical food total')
  prepare(h,{food=2,metal=1},{1},1,2);advance(h,1);local back=manifest(h.live);assert(h:queue(command('assemble_expedition',{sourceSiteId=2,craftId=1,manifestId=back.id})));advance(h,160);back=manifest(h.live);local ready,why=Logistics.readiness(h.live,back);check(ready,why)
  assert(h:queue(command('launch_expedition',{sourceSiteId=2,craftId=1,manifestId=back.id,expectedManifestRevision=back.revision})));advance(h,1);landSoon(h);eq(vehicle.dockedSiteId,1);check(personAt(h.live,personId,1),'Return did not restore the same person at home');Campaign.validate(h.live)
 end)

 group('P04-B travel engine gives each passenger one safe-cabin physiology update per transit tick',function()
  local h=history(7202);local vehicle=readyLaunch(h,{food=3,metal=2},{1},2);local passenger=vehicle.passengers[1];local hunger=passenger.hunger;local rate=vehicle.journey.physiology.hungerRate
  advance(h,1);eq(passenger.hunger,hunger+rate,'Passenger physiology updated more or less than once on an ordinary travel tick')
  vehicle.journey.remainingTicks=1;advance(h,1);local arrived=worker(h.live,1,2);eq(arrived.hunger,hunger+rate*2,'Arrival tick also ran local physiology')
  advance(h,1);eq(arrived.hunger,hunger+rate*3,'First local physiology was not deferred until the following tick')
  local a={alive=true,hp=.01,hunger=100,fatigue=5,breath=20};local died,reason=Colonists.transitStep(a,{hungerRate=0,fatigueRate=.04},function() return true end);check(died and reason=='starvation','Cabin starvation ordering changed before food consumption')
  local function durationFixture(duration)
   local fixture=history(7300+duration);prepare(fixture,{food=3,metal=2},{1},2);advance(fixture,460);local plan=manifest(fixture.live);assert(fixture:queue(command('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=plan.id})));advance(fixture,180);plan=manifest(fixture.live)
   local launched,why=Travel.apply(fixture.live,command('launch_expedition',{sourceSiteId=1,craftId=1,manifestId=plan.id,expectedManifestRevision=plan.revision}));assert(launched,why)
   local craftState=craft(fixture.live);local start=craftState.passengers[1].hunger;craftState.journey.duration=duration;craftState.journey.remainingTicks=duration
   for _=1,duration do Travel.step(fixture.live) end
   local landed=worker(fixture.live,1,2);eq(landed.hunger,start+duration*fixture.live.sites[1].world.rules.hungerRate,'Controlled D='..duration..' journey did not use exactly D transit updates')
  end
  durationFixture(1);durationFixture(2)
 end)

 group('P04-C travellers count toward campaign survival while empty owned settlements remain manageable',function()
  local h=history(7203,'challenge');local vehicle=readyLaunch(h,{food=2,metal=2},{1},2)
  for _,a in ipairs(h.live.sites[1].world.workers) do a.alive=false end
  check(not Campaign.extinct(h.live),'Living traveller was not counted for campaign extinction')
  check(h:queue({scope='site',siteId=1,payload={type='order',kind='dig',gx=5,gy=5}}),'Empty owned home rejected a legal queued order while traveller lived')
  vehicle.passengers[1].alive=false;vehicle.passengers[1].hp=0;vehicle.passengers[1].deathTick=h.live.tick
  check(Campaign.extinct(h.live),'Campaign with no local or transit living people was not extinct')
  check(not h:queue({scope='site',siteId=1,payload={type='order',kind='dig',gx=6,gy=5}}),'Challenge accepted a new command after total extinction')
 end)

 group('P04-D portable people retain personal state but release source-local execution state',function()
  local h=history(7204);local original=worker(h.live);original.mine=73;original.build=61;original.hunger=39;original.fatigue=47;original.breath=88
  local vehicle=readyLaunch(h,{food=2,metal=2},{1},2);local departed=U.deep(vehicle.passengers[1]);check(not W.find(h.live.sites[1].world.workers,original.id),'Source kept a departed local worker')
  landSoon(h);local arrived=worker(h.live,1,2);eq(arrived.personId,original.personId);eq(arrived.mine,departed.mine);eq(arrived.build,departed.build);eq(arrived.hunger,departed.hunger+h.live.sites[1].world.rules.hungerRate);eq(arrived.fatigue,math.max(0,departed.fatigue-.035));eq(arrived.breath,math.min(100,departed.breath+1.6));check(arrived.task==nil and arrived.carry==nil and arrived.directive==nil,'Arrival imported source-local execution state')
  Campaign.validate(h.live)
 end)

 group('P04-E departure, transit food, and arrival use one cargo owner and ordered receipts',function()
  local h=history(7205);local beforeFood,beforeMetal=countResource(h.live,'food'),countResource(h.live,'metal');local vehicle=readyLaunch(h,{food=3,metal=2},{1},2)
  local travel=h.live.travel;eq(travel.receipts[1].kind,'part');eq(travel.receipts[2].kind,'departure');eq(travel.receipts[1].cargo.metal,1);eq(travel.receipts[2].cargo.food,3);eq(travel.receipts[2].cargo.metal,1)
  eq(countResource(h.live,'food'),beforeFood);eq(countResource(h.live,'metal'),beforeMetal-1,'Only the maintenance part may leave the physical metal total')
  landSoon(h);local last=travel.receipts[#travel.receipts];eq(last.kind,'arrival');eq(last.cargo.food,3);eq(last.cargo.metal,1);eq(countResource(h.live,'food'),beforeFood);eq(countResource(h.live,'metal'),beforeMetal-1)
  local prior=0;for _,receipt in ipairs(travel.receipts) do check(receipt.id>prior,'Transfer receipt IDs were not ordered');prior=receipt.id end
 end)

 group('P04-F invalid launch requests reject without consuming a part or moving a person',function()
  local h=history(7206);prepare(h,{food=2,metal=2},{1},2);advance(h,460);local m=manifest(h.live);local before=Codec.encode(h.live)
  local invalid={
   command('launch_expedition',{sourceSiteId=2,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision}),
   command('launch_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision+1}),
   command('launch_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision}),
  }
  for _,value in ipairs(invalid) do check(not h:queue(value),'Invalid/unassembled launch was accepted') end
  eq(Codec.encode(h.live),before)
 end)

 group('P04-G blocked landing holds without terrain repair and a paid return can itself hold',function()
  local h=history(7207);local vehicle=readyLaunch(h,{food=2,metal=2},{1},2);fillLanding(h.live.sites[2].world);landSoon(h)
  eq(vehicle.journey.status,'holding');eq(vehicle.journey.leg,'outbound');check(h.live.sites[2].ownerSocietyId==nil,'Blocked landing claimed an unowned moon')
  local j=vehicle.journey;assert(h:queue(command('return_to_origin',{craftId=1,journeyId=j.id,expectedLeg='outbound'})));advance(h,1);eq(vehicle.journey.leg,'return');eq(vehicle.cargo.metal,nil,'Return did not consume its actual metal part')
  fillLanding(h.live.sites[1].world);vehicle.journey.remainingTicks=1;advance(h,1);eq(vehicle.journey.status,'holding');eq(vehicle.journey.leg,'return');check(vehicle.journey.holdingReason:match('Landing'),'Blocked original landing did not remain a saveable holding state')
 end)

 group('P04-H all-dead unowned journeys remain in holding without a false colony',function()
  local h=history(7208);local vehicle=readyLaunch(h,{food=1,metal=2},{1},2);local passenger=vehicle.passengers[1];passenger.hunger=100;passenger.hp=.01;vehicle.journey.remainingTicks=1;advance(h,1)
  eq(vehicle.journey.status,'holding');eq(vehicle.journey.holdingReason,'No living landing party');eq(h.live.sites[2].ownerSocietyId,nil);eq(#vehicle.passengers,1);check(not vehicle.passengers[1].alive,'Final transit deprivation did not retain a dead passenger record')
 end)

 group('P04-I save, replay, seek, and practice branch preserve active transit state',function()
  local h=history(7209);readyLaunch(h,{food=3,metal=2},{1},2);advance(h,17);local saved=h:saveText();local restored=History.fromText(saved)
  advance(h,23);advance(restored,23);eq(Codec.encode(restored.live),Codec.encode(h.live),'Save/load continuation diverged in transit')
  restored:seek(3);while restored.seekTarget do restored:updateSeek(19) end;check(restored:queue({scope='site',siteId=1,payload={type='paint',x=8,y=8,material=M.AIR}}),'Practice branch failed from a whole-campaign transit archive');eq(restored.frontier,3)
  local verified,why=History.fromText(saved):verifyReplay(4000);check(verified,why)
 end)

 group('P04-J render-independent campaign stepping keeps all maps and transit deterministic',function()
  local first=history(7210);local second=History.fromText(first:saveText());readyLaunch(first,{food=2,metal=2},{1},2);readyLaunch(second,{food=2,metal=2},{1},2)
  for i=1,60 do if i%2==0 then Campaign.site(second.view,1) else Campaign.site(second.view,2) end;advance(first,1);advance(second,1) end
  eq(Codec.encode(first.live),Codec.encode(second.live),'Read-only site inspection changed campaign simulation')
 end)

 group('P04-K return reoccupies an existing world without regeneration or starter grants',function()
  local h=history(7211);local home=h.live.sites[1].world;local generation=Codec.encode(home.generation);local sentinel=home.mat[1];local vehicle=readyLaunch(h,{food=2,metal=2},{1},2);landSoon(h)
  local person=worker(h.live,1,2).personId;prepare(h,{food=2,metal=1},{1},1,2);advance(h,1);local m=manifest(h.live);assert(h:queue(command('assemble_expedition',{sourceSiteId=2,craftId=1,manifestId=m.id})));advance(h,170);m=manifest(h.live);assert(h:queue(command('launch_expedition',{sourceSiteId=2,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision})));advance(h,1);landSoon(h)
  eq(Codec.encode(h.live.sites[1].world.generation),generation,'Return regenerated or reset home generation metadata');eq(h.live.sites[1].world.mat[1],sentinel,'Return replaced the persistent home terrain');check(#h.live.sites[1].world.workers==3,'Return granted a replacement crew');check(personAt(h.live,person,1),'Returned person was replaced')
 end)

 group('P04-L stale and duplicate launch or return identities have one deterministic effect',function()
  local h=history(7212);prepare(h,{food=2,metal=2},{1},2);advance(h,460);local m=manifest(h.live);assert(h:queue(command('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id})));advance(h,170);m=manifest(h.live)
  local stale=command('launch_expedition',{sourceSiteId=1,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision});assert(h:queue(command('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=3,passengers=m.passengers,cargo={food=2,metal=2}})));assert(h:queue(stale));advance(h,1);eq(craft(h.live).journey,nil,'Stale queued launch used a revised manifest')
  local revised=manifest(h.live);assert(h:queue(command('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=revised.id})));advance(h,170);revised=manifest(h.live);local launch=command('launch_expedition',{sourceSiteId=1,craftId=1,manifestId=revised.id,expectedManifestRevision=revised.revision});assert(h:queue(launch));assert(h:queue(launch));advance(h,1);eq(craft(h.live).cargo.metal,1,'Duplicate launch consumed more than one maintenance part')
 end)

 group('P04-M cabin physiology consumes actual food once and has no hidden ground source',function()
  local h=history(7213);local vehicle=readyLaunch(h,{food=1,metal=2},{1},2);local passenger=vehicle.passengers[1];passenger.hunger=60;local food=vehicle.cargo.food;advance(h,1);eq(vehicle.cargo.food or 0,food-1);check(passenger.hunger<60,'Cabin passenger did not eat its actual craft food')
  vehicle.cargo.food=nil;passenger.hunger=99.99;passenger.hp=.01;advance(h,1);check(not passenger.alive,'Passenger ate a hidden settlement food source')
 end)

 group('P04-N strict travel validation rejects contradictory portable and route state',function()
  local c=newCampaign(7214);local bad=Campaign.clone(c);bad.features.travel=nil;reject(function() Campaign.validate(bad) end)
  local h=history(7215);local vehicle=readyLaunch(h,{food=2,metal=2},{1},2);local duplicate=Campaign.clone(h.live);duplicate.sites[1].world.workers[1].personId=vehicle.passengers[1].personId;reject(function() Campaign.validate(duplicate) end,'Portable/local duplicate person was accepted')
  local routeBad=Campaign.clone(h.live);routeBad.logistics.crafts[1].journey.duration=1;reject(function() Campaign.validate(routeBad) end,'Unsupported route duration was accepted')
 end)

 group('P04-O receipt retention is bounded and allocation counters stay monotonic',function()
  local h=history(7216);local vehicle=readyLaunch(h,{food=2,metal=2},{1},2);local travel=h.live.travel;local exemplar=U.deep(vehicle.passengers[1]);vehicle.cargo={food=24}
  local nextPerson=h.live.nextPersonId
  for index=1,4 do
   local record=index==1 and vehicle or {id=index,ownerSocietyId=1,seats=3,capacity=24,dockedSiteId=nil,anchor=U.deep(vehicle.anchor),cargo={food=24},activeManifestId=nil,passengers={},journey=U.deep(vehicle.journey)}
   if index>1 then record.journey.id=index;h.live.logistics.crafts[#h.live.logistics.crafts+1]=record end
   while #record.passengers<3 do local passenger=U.deep(exemplar);passenger.personId=nextPerson;passenger.name='Fixture '..nextPerson;nextPerson=nextPerson+1;record.passengers[#record.passengers+1]=passenger end
   table.sort(record.passengers,function(a,b) return a.personId<b.personId end)
  end
  h.live.nextPersonId=nextPerson;h.live.logistics.nextCraftId=5;travel.nextJourneyId=5
  for _=1,11 do for _,record in ipairs(h.live.logistics.crafts) do record.cargo.food=24;for _,passenger in ipairs(record.passengers) do passenger.hunger=60 end end;advance(h,1) end
  Campaign.validate(h.live);eq(#travel.receipts,128);check(travel.receipts[1].id>1,'Oldest receipt was not pruned by real transit consumption');check(travel.nextReceiptId>128,'Receipt allocator regressed after pruning')
 end)

 group('P04-Q feature-off histories and templates remain outside travel state',function()
  local old=Campaign.newRegion(7217,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true})
  check(old.features.travel==nil and old.travel==nil,'P03 history silently gained travel');local map=require('src.mapfile').fromWorld(history(7218).live.sites[1].world);check(map.travel==nil and map.logistics==nil and map.frontier==nil,'Local map template exported campaign travel state')
 end)
 print(report.groups..' travel groups; '..report.assertions..' assertions passed.')
 return report
end
return T
