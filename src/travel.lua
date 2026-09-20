-- Campaign travel owns only off-map craft custody.  Local worlds keep workers,
-- navigation, jobs and physical cargo whenever a craft is docked.
local U=require('src.util')
local W=require('src.world')
local N=require('src.nav')
local J=require('src.jobs')
local Labor=require('src.labor')
local Colonists=require('src.colonists')
local Logistics=require('src.logistics')
local T={version=1,maxReceipts=128}
local resources=Logistics.resources()
local resourceKeys={};for _,kind in ipairs(resources) do resourceKeys[kind]=true end

local function exact(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do assert(keys[key],'Unknown '..label..' key '..tostring(key)) end
 for key in pairs(keys) do assert(t[key]~=nil,'Missing '..label..' key '..key) end
end
local function allowed(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do assert(keys[key],'Unknown '..label..' key '..tostring(key)) end
end
local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local count,max=0,0
 for key in pairs(t) do U.integer(key,label..' index',1,limit);count=count+1;if key>max then max=key end end
 assert(count==max and count<=limit,label..' has a hole or exceeds its limit')
 return max
end
local function site(c,id)
 for _,record in ipairs(c.sites) do if record.id==id then return record end end
end
local function craft(c,id) return c.logistics and Logistics.craft(c,id) end
local function zero()
 local value={};for _,kind in ipairs(resources) do value[kind]=0 end;return value
end
local function copyVector(value)
 local out=zero();for _,kind in ipairs(resources) do out[kind]=value[kind] or 0 end;return out
end
local function add(target,value)
 for _,kind in ipairs(resources) do target[kind]=target[kind]+(value[kind] or 0) end
end
local function nonzero(value)
 for _,kind in ipairs(resources) do if (value[kind] or 0)>0 then return true end end
 return false
end
local function vector(value,label,limit)
 exact(value,resourceKeys,label)
 for _,kind in ipairs(resources) do U.integer(value[kind],label..' '..kind,0,limit or 100000000) end
end
local function account(c,siteId)
 for _,record in ipairs(c.travel.accounts.sites) do if record.siteId==siteId then return record end end
end
local function route(c,a,b)
 local lo,hi=math.min(a,b),math.max(a,b)
 for _,record in ipairs(c.travel.rules.routes) do if record.from==lo and record.to==hi then return record.duration end end
end
local function living(records)
 local total=0;for _,record in ipairs(records or {}) do if record.alive then total=total+1 end end;return total
end
local function sortedCrafts(c)
 local out={};for i,record in ipairs(c.logistics.crafts) do out[i]=record end;table.sort(out,function(a,b) return a.id<b.id end);return out
end
local function portable(worker)
 return {personId=worker.personId,name=worker.name,alive=worker.alive,hp=worker.hp,hunger=worker.hunger,
  fatigue=worker.fatigue,breath=worker.breath,mine=worker.mine,build=worker.build,status=worker.status,
  reason=worker.reason,fall=0,worked=false,progress=0}
end
local function validatePassenger(record)
 allowed(record,{personId=true,name=true,alive=true,hp=true,hunger=true,fatigue=true,breath=true,mine=true,build=true,status=true,reason=true,fall=true,worked=true,progress=true,deathTick=true},'Transit passenger')
 for _,key in ipairs({'personId','name','alive','hp','hunger','fatigue','breath','mine','build','status','reason','fall','worked','progress'}) do assert(record[key]~=nil,'Missing transit passenger key '..key) end
 U.integer(record.personId,'Transit person ID',1,100000000);assert(type(record.name)=='string' and type(record.alive)=='boolean' and type(record.status)=='string' and type(record.reason)=='string','Malformed transit identity')
 for _,key in ipairs({'hp','hunger','fatigue','breath'}) do assert(U.finite(record[key]) and record[key]>=0 and record[key]<=100,'Invalid transit '..key) end
 assert(U.finite(record.mine) and record.mine>0 and record.mine<=100 and U.finite(record.build) and record.build>0 and record.build<=100,'Invalid transit aptitude')
 U.integer(record.fall,'Transit fall',0,100000);assert(type(record.worked)=='boolean','Invalid transit worked flag');U.integer(record.progress,'Transit progress',0,100000000)
 if record.alive then assert(record.deathTick==nil,'Living transit passenger has a death tick') else assert(record.hp==0,'Dead transit passenger has HP');U.integer(record.deathTick,'Transit death tick',0,10000000) end
end
local function notice(c,siteId,kind,text,ordinal)
 local Campaign=require('src.campaign')
 Campaign.addNotice(c,siteId,kind,text,ordinal or 0)
end
local function closeManifest(c,manifest)
 Logistics.closeManifest(c,manifest)
end
local function releaseDepartureWorker(w,worker)
 J.release(w,worker,true)
 for _,job in ipairs(w.jobs) do if job.owner==worker.id then job.owner=nil end end
end
local function detach(c,source,workers)
 local gone={};for _,worker in ipairs(workers) do gone[worker.id]=true;releaseDepartureWorker(source.world,worker) end
 local kept={};for _,worker in ipairs(source.world.workers) do if not gone[worker.id] then kept[#kept+1]=worker end end;source.world.workers=kept
 if source.world.labor then
  local people={};for _,entry in ipairs(source.world.labor.people) do if not gone[entry.id] then people[#people+1]=entry end end
  source.world.labor.people=people;source.world.laborAssignments=Labor.allocate(source.world)
 end
end
local function transfer(c,fromDomain,fromSite,toDomain,toSite,kind,vehicle,journey,value)
 if not nonzero(value) then return end
 local travel=c.travel;local from=fromDomain=='site' and account(c,fromSite) or travel.accounts.transit
 local target=toDomain=='site' and account(c,toSite) or travel.accounts.transit
 if fromDomain=='site' then add(from.exports,value) elseif fromDomain=='transit' then add(from.exports,value) end
 if toDomain=='site' then add(target.imports,value) elseif toDomain=='transit' then add(target.imports,value) end
 if toDomain=='sink' then add(from.consumed,value) end
 local id=travel.nextReceiptId;travel.nextReceiptId=id+1
 travel.receipts[#travel.receipts+1]={id=id,tick=c.tick,kind=kind,sourceDomain=fromDomain,sourceSiteId=fromSite or 0,
  destinationDomain=toDomain,destinationSiteId=toSite or 0,craftId=vehicle.id,journeyId=journey and journey.id or 0,cargo=copyVector(value)}
 while #travel.receipts>T.maxReceipts do table.remove(travel.receipts,1) end
end
local function one(kind)
 local out=zero();out[kind]=1;return out
end
local function journeyEndpoints(journey)
 if journey.leg=='outbound' then return journey.originSiteId,journey.destinationSiteId end
 return journey.destinationSiteId,journey.originSiteId
end
local function candidatePoses(world,anchor)
 local candidates={}
 for y=math.max(1,anchor.y-8),math.min(world.height,anchor.y+8) do for x=math.max(1,anchor.x-8),math.min(world.width,anchor.x+8) do
  local distance=math.abs(x-anchor.x)+math.abs(y-anchor.y)
  if distance<=8 and N.stand(world,x,y,true) then candidates[#candidates+1]={x=x,y=y,distance=distance} end
 end end
 table.sort(candidates,function(a,b) if a.distance~=b.distance then return a.distance<b.distance end;if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
 return candidates
end
local function landingPlan(c,vehicle,journey)
 local _,destinationId=journeyEndpoints(journey);local destination=site(c,destinationId);assert(destination,'Journey destination is missing')
 local livingCount=living(vehicle.passengers)
 if destination.ownerSocietyId~=c.society.id and livingCount==0 then return nil,'No living landing party' end
 if #destination.world.workers+#vehicle.passengers>32 then return nil,'Destination worker capacity is full' end
 local anchor=destination.world.home and {x=destination.world.home.x,y=destination.world.home.y}
 if not anchor then return nil,'Destination has no landing anchor' end
 local candidates=candidatePoses(destination.world,anchor);local required=math.max(1,livingCount)
 if #candidates<required then return nil,'Landing clearing is blocked' end
 local poses,at={},1
 for _,passenger in ipairs(vehicle.passengers) do
  if passenger.alive then poses[passenger.personId]=candidates[at];at=at+1 end
 end
 return {destination=destination,anchor=anchor,poses=poses,deadPose=candidates[1]}
end
local function addArrivalWorker(world,passenger,pose)
 local worker={id=W.id(world),personId=passenger.personId,name=passenger.name,alive=passenger.alive,hp=passenger.hp,hunger=passenger.hunger,
  fatigue=passenger.fatigue,breath=passenger.breath,mine=passenger.mine,build=passenger.build,status=passenger.status,reason=passenger.reason,
  fall=0,worked=false,job=nil,carry=nil,thinkAt=world.tick+1,progress=0}
 worker.x,worker.y=pose.x,pose.y
 if not worker.alive then worker.hp=0;worker.status='Dead';worker.deathTick=passenger.deathTick end
 world.workers[#world.workers+1]=worker
 return worker
end
local function arrive(c,vehicle,journey,plan)
 local destination=plan.destination;local arrivals={};for i,passenger in ipairs(vehicle.passengers) do arrivals[i]=passenger end
 table.sort(arrivals,function(a,b) return a.personId<b.personId end)
 local created={}
 for _,passenger in ipairs(arrivals) do created[#created+1]=addArrivalWorker(destination.world,passenger,passenger.alive and plan.poses[passenger.personId] or plan.deadPose) end
 if destination.world.labor then
  for _,worker in ipairs(created) do destination.world.labor.people[#destination.world.labor.people+1]={id=worker.id,role='auto',prefs={dig=2,build=2,haul=2,farm=2,pump=2,field=2}} end
 else destination.world.labor=Labor.default(destination.world) end
 destination.world.laborAssignments=Labor.allocate(destination.world)
 if living(created)>0 then destination.world.extinct=false end
 local wasOwned=destination.ownerSocietyId==c.society.id
 if not wasOwned then destination.ownerSocietyId=c.society.id end
 transfer(c,'transit',nil,'site',destination.id,'arrival',vehicle,journey,vehicle.cargo)
 vehicle.dockedSiteId=destination.id;vehicle.anchor=plan.anchor;vehicle.journey=nil;vehicle.passengers={}
 W.event(destination.world,wasOwned and 'arrival' or 'foundation',(wasOwned and 'Craft returned with ' or 'A lunar outpost was founded by ')..#created..' passenger records.',vehicle.id)
end
local function hold(c,vehicle,journey,reason)
 journey.status='holding';journey.remainingTicks=0
 if journey.holdingReason~=reason then journey.holdingReason=reason;local _,destination=journeyEndpoints(journey);notice(c,destination,'holding','Craft '..vehicle.id..' holding: '..reason,vehicle.id) end
end

function T.enabled(c) return c.features and c.features.travel==1 end
function T.new()
 return {version=T.version,rules={version=1,routes={{from=1,to=2,duration=400},{from=1,to=3,duration=600},{from=2,to=3,duration=800}}},
  nextJourneyId=1,nextReceiptId=1,receipts={},accounts={sites={{siteId=1,imports=zero(),exports=zero(),consumed=zero()},{siteId=2,imports=zero(),exports=zero(),consumed=zero()},{siteId=3,imports=zero(),exports=zero(),consumed=zero()}},transit={imports=zero(),exports=zero(),consumed=zero()}}}
end
function T.duration(c,a,b) return route(c,a,b) end
function T.living(c)
 local total=0;if not T.enabled(c) then return total end
 for _,vehicle in ipairs(c.logistics.crafts) do total=total+living(vehicle.passengers) end
 return total
end
function T.transitCargo(c)
 local out=zero();if not T.enabled(c) then return out end
 for _,vehicle in ipairs(c.logistics.crafts) do if vehicle.journey then add(out,vehicle.cargo) end end
 return out
end
function T.account(c,siteId) return account(c,siteId) end
function T.reconciliation(c)
 return {transit={cargo=T.transitCargo(c),imports=copyVector(c.travel.accounts.transit.imports),exports=copyVector(c.travel.accounts.transit.exports),consumed=copyVector(c.travel.accounts.transit.consumed)},receipts=#c.travel.receipts}
end

function T.validate(c)
 local travel=c.travel
 exact(travel,{version=true,rules=true,nextJourneyId=true,nextReceiptId=true,receipts=true,accounts=true},'Campaign travel')
 assert(travel.version==T.version,'Unsupported campaign travel version')
 exact(travel.rules,{version=true,routes=true},'Campaign travel rules');assert(travel.rules.version==1,'Unsupported campaign travel rules version')
 assert(dense(travel.rules.routes,'Campaign travel routes',3)==3,'Campaign travel requires three routes')
 local expected={{from=1,to=2,duration=400},{from=1,to=3,duration=600},{from=2,to=3,duration=800}}
 for i,record in ipairs(travel.rules.routes) do exact(record,{from=true,to=true,duration=true},'Campaign travel route');assert(record.from==expected[i].from and record.to==expected[i].to and record.duration==expected[i].duration,'Unsupported campaign travel route') end
 U.integer(travel.nextJourneyId,'Next journey ID',1,100000000);U.integer(travel.nextReceiptId,'Next transfer receipt ID',1,100000000)
 exact(travel.accounts,{sites=true,transit=true},'Campaign travel accounts');assert(dense(travel.accounts.sites,'Campaign travel site accounts',3)==3,'Campaign travel needs all site accounts')
 for i,record in ipairs(travel.accounts.sites) do exact(record,{siteId=true,imports=true,exports=true,consumed=true},'Campaign travel site account');assert(record.siteId==i,'Campaign travel account order');vector(record.imports,'Campaign site imports');vector(record.exports,'Campaign site exports');vector(record.consumed,'Campaign site consumption') end
 exact(travel.accounts.transit,{imports=true,exports=true,consumed=true},'Campaign transit account');vector(travel.accounts.transit.imports,'Campaign transit imports');vector(travel.accounts.transit.exports,'Campaign transit exports');vector(travel.accounts.transit.consumed,'Campaign transit consumption')
 local receiptCount=dense(travel.receipts,'Campaign transfer receipts',T.maxReceipts);local prior,maxReceipt=0,0
 for i=1,receiptCount do
  local record=travel.receipts[i];exact(record,{id=true,tick=true,kind=true,sourceDomain=true,sourceSiteId=true,destinationDomain=true,destinationSiteId=true,craftId=true,journeyId=true,cargo=true},'Campaign transfer receipt')
  U.integer(record.id,'Transfer receipt ID',1,travel.nextReceiptId-1);assert(record.id>prior,'Transfer receipts are not ordered');prior=record.id;maxReceipt=record.id;U.integer(record.tick,'Transfer receipt tick',0,c.tick)
  assert(record.kind=='departure' or record.kind=='arrival' or record.kind=='part' or record.kind=='meal','Unknown transfer receipt kind')
  assert(record.sourceDomain=='site' or record.sourceDomain=='transit','Invalid transfer source domain');assert(record.destinationDomain=='site' or record.destinationDomain=='transit' or record.destinationDomain=='sink','Invalid transfer destination domain')
  if record.sourceDomain=='site' then assert(site(c,record.sourceSiteId)) else assert(record.sourceSiteId==0) end
  if record.destinationDomain=='site' then assert(site(c,record.destinationSiteId)) else assert(record.destinationSiteId==0) end
  U.integer(record.craftId,'Transfer receipt craft ID',1,100000000);U.integer(record.journeyId,'Transfer receipt journey ID',0,100000000);vector(record.cargo,'Transfer receipt cargo');assert(nonzero(record.cargo),'Empty transfer receipt')
 end
 assert(travel.nextReceiptId>maxReceipt,'Next transfer receipt ID was already allocated')
 local seenJourney,seenPeople,maxJourney,maxPerson={}, {},0,0
 for _,vehicle in ipairs(c.logistics.crafts) do
  assert(type(vehicle.passengers)=='table' and vehicle.journey~=nil or (type(vehicle.passengers)=='table' and vehicle.journey==nil),'Travel craft state is missing')
  dense(vehicle.passengers,'Craft passengers',vehicle.seats)
  if vehicle.journey then
   assert(vehicle.dockedSiteId==nil,'Travelling craft is still docked')
   local journey=vehicle.journey;allowed(journey,{id=true,originSiteId=true,destinationSiteId=true,leg=true,status=true,legStartTick=true,duration=true,remainingTicks=true,physiology=true,holdingReason=true},'Craft journey')
   for _,key in ipairs({'id','originSiteId','destinationSiteId','leg','status','legStartTick','duration','remainingTicks','physiology'}) do assert(journey[key]~=nil,'Missing Craft journey key '..key) end
   U.integer(journey.id,'Journey ID',1,travel.nextJourneyId-1);assert(not seenJourney[journey.id],'Duplicate journey ID');seenJourney[journey.id]=true;maxJourney=math.max(maxJourney,journey.id)
   U.integer(journey.originSiteId,'Journey origin site ID',1,3);U.integer(journey.destinationSiteId,'Journey destination site ID',1,3);assert(journey.originSiteId~=journey.destinationSiteId and route(c,journey.originSiteId,journey.destinationSiteId)==journey.duration,'Invalid journey route')
   assert(journey.leg=='outbound' or journey.leg=='return','Invalid journey leg');assert(journey.status=='travelling' or journey.status=='holding','Invalid journey status');U.integer(journey.legStartTick,'Journey leg start tick',0,c.tick);U.integer(journey.duration,'Journey duration',1,100000);U.integer(journey.remainingTicks,'Journey remaining ticks',0,journey.duration)
   exact(journey.physiology,{hungerRate=true,fatigueRate=true},'Journey physiology');assert(U.finite(journey.physiology.hungerRate) and journey.physiology.hungerRate>=0 and journey.physiology.hungerRate<=1 and U.finite(journey.physiology.fatigueRate) and journey.physiology.fatigueRate>=0 and journey.physiology.fatigueRate<=1,'Invalid journey physiology')
   if journey.status=='travelling' then assert(journey.remainingTicks>0 and journey.holdingReason==nil,'Travelling journey timing mismatch') else assert(journey.remainingTicks==0 and type(journey.holdingReason)=='string' and #journey.holdingReason<=160,'Holding journey state mismatch') end
  else assert(vehicle.dockedSiteId~=nil and #vehicle.passengers==0,'Docked craft has transit state') end
  local priorPerson=0
  for _,passenger in ipairs(vehicle.passengers) do
   validatePassenger(passenger);assert(passenger.personId>priorPerson,'Craft passengers are not ordered');priorPerson=passenger.personId;assert(not seenPeople[passenger.personId],'Duplicate portable person');seenPeople[passenger.personId]=true;maxPerson=math.max(maxPerson,passenger.personId)
   for _,localSite in ipairs(c.sites) do for _,worker in ipairs(localSite.world.workers) do assert(worker.personId~=passenger.personId,'Portable person still exists locally') end end
  end
 end
 assert(travel.nextJourneyId>maxJourney,'Next journey ID was already allocated');assert(c.nextPersonId>maxPerson,'Next person ID was already allocated')
 return true
end

function T.valid(c,command)
 local ok,result=pcall(function()
  assert(T.enabled(c),'Travel unavailable in this older campaign')
  if command.type=='launch_expedition' then
   local source=site(c,command.sourceSiteId);assert(source and source.ownerSocietyId==c.society.id,'Launch source is not owned')
   local vehicle=craft(c,command.craftId);assert(vehicle and vehicle.ownerSocietyId==c.society.id and vehicle.dockedSiteId==source.id and not vehicle.journey,'Craft is not docked at the named source')
   local manifest=Logistics.manifest(c,command.manifestId);assert(manifest and vehicle.activeManifestId==manifest.id and manifest.craftId==vehicle.id and manifest.sourceSiteId==source.id,'Manifest is not active at the named source')
   assert(manifest.revision==command.expectedManifestRevision,'Manifest revision changed before launch')
   local ready,why=Logistics.readiness(c,manifest);assert(ready,why)
   local duration=route(c,source.id,manifest.destinationSiteId);assert(duration,'No supported travel route')
   return {source=source,vehicle=vehicle,manifest=manifest,destination=site(c,manifest.destinationSiteId),duration=duration}
  elseif command.type=='return_to_origin' then
   local vehicle=craft(c,command.craftId);assert(vehicle and vehicle.ownerSocietyId==c.society.id and vehicle.journey,'Craft has no active journey')
   local journey=vehicle.journey;assert(journey.id==command.journeyId and journey.leg==command.expectedLeg,'Journey identity changed before return')
   assert(journey.leg=='outbound' and journey.status=='holding','Only outbound holding may return')
   assert((vehicle.cargo.metal or 0)>=c.logistics.rules.maintenanceMetal,'Return needs one metal unit aboard')
   return {vehicle=vehicle,journey=journey}
  end
  error('Unknown campaign travel command')
 end)
 return ok,ok and result or tostring(result)
end

function T.apply(c,command)
 local ok,plan=T.valid(c,command);if not ok then return false,plan end
 if command.type=='launch_expedition' then
  local workers={};for _,personId in ipairs(plan.manifest.passengers) do
   local selected
   for _,worker in ipairs(plan.source.world.workers) do if worker.personId==personId then selected=worker;break end end
   assert(selected,'Launch passenger vanished after validation');workers[#workers+1]=selected
  end
  local passengers={};for _,worker in ipairs(workers) do passengers[#passengers+1]=portable(worker) end
  local cost=c.logistics.rules.maintenanceMetal;plan.vehicle.cargo.metal=plan.vehicle.cargo.metal-cost;if plan.vehicle.cargo.metal==0 then plan.vehicle.cargo.metal=nil end
  transfer(c,'site',plan.source.id,'sink',nil,'part',plan.vehicle,nil,one('metal'))
  closeManifest(c,plan.manifest);detach(c,plan.source,workers)
  local id=c.travel.nextJourneyId;c.travel.nextJourneyId=id+1
  local journey={id=id,originSiteId=plan.source.id,destinationSiteId=plan.destination.id,leg='outbound',status='travelling',legStartTick=c.tick,duration=plan.duration,remainingTicks=plan.duration,
   physiology={hungerRate=plan.source.world.rules.hungerRate,fatigueRate=plan.source.world.rules.fatigueRate},holdingReason=nil}
  plan.vehicle.dockedSiteId=nil;plan.vehicle.passengers=passengers;plan.vehicle.journey=journey
  transfer(c,'site',plan.source.id,'transit',nil,'departure',plan.vehicle,journey,plan.vehicle.cargo)
  W.event(plan.source.world,'departure','Craft '..plan.vehicle.id..' departed with '..#passengers..' passengers.',plan.vehicle.id)
  return true,'Craft departed'
 elseif command.type=='return_to_origin' then
  local cost=c.logistics.rules.maintenanceMetal;plan.vehicle.cargo.metal=plan.vehicle.cargo.metal-cost;if plan.vehicle.cargo.metal==0 then plan.vehicle.cargo.metal=nil end
  transfer(c,'transit',nil,'sink',nil,'part',plan.vehicle,plan.journey,one('metal'))
  plan.journey.leg='return';plan.journey.status='travelling';plan.journey.legStartTick=c.tick;plan.journey.remainingTicks=plan.journey.duration;plan.journey.holdingReason=nil
  return true,'Craft began its return journey'
 end
 return false,'Unknown campaign travel command'
end

function T.step(c)
 if not T.enabled(c) then return end
 local due={}
 for _,vehicle in ipairs(sortedCrafts(c)) do
  local journey=vehicle.journey
  if journey then
   for _,passenger in ipairs(vehicle.passengers) do if passenger.alive then
    local died,reason=Colonists.transitStep(passenger,journey.physiology,function()
     if (vehicle.cargo.food or 0)<1 then return false end
     vehicle.cargo.food=vehicle.cargo.food-1;if vehicle.cargo.food==0 then vehicle.cargo.food=nil end
     transfer(c,'transit',nil,'sink',nil,'meal',vehicle,journey,one('food'));return true
    end)
    if died then passenger.alive=false;passenger.hp=0;passenger.status='Dead';passenger.reason=reason or 'injuries';passenger.deathTick=c.tick;notice(c,journey.originSiteId,'transit_death',passenger.name..' died in transit: '..passenger.reason..'.',vehicle.id) end
   end end
   if journey.status=='travelling' then journey.remainingTicks=journey.remainingTicks-1 end
   if journey.remainingTicks==0 then due[#due+1]=vehicle end
  end
 end
 for _,vehicle in ipairs(due) do
  local journey=vehicle.journey
  if journey and journey.remainingTicks==0 then
   local ok,plan,why=pcall(landingPlan,c,vehicle,journey)
   if not ok then hold(c,vehicle,journey,tostring(plan))
   elseif not plan then hold(c,vehicle,journey,why or 'Landing plan unavailable')
   else arrive(c,vehicle,journey,plan) end
  end
 end
end

return T
