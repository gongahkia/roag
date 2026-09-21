-- Campaign logistics is deliberately narrow: a docked craft owns cargo while
-- local workers still own bodies, paths, carries and work tasks.
local U=require('src.util')
local W=require('src.world')
local N=require('src.nav')
local S=require('src.structures')
local L={version=1,maxCrafts=4,maxManifests=4,maxOperations=16}
local resources={'food','metal','soil','stone','water'}
local known={};for _,kind in ipairs(resources) do known[kind]=true end

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
local function craft(logistics,id)
 for _,record in ipairs(logistics.crafts) do if record.id==id then return record end end
end
local function manifest(logistics,id)
 for _,record in ipairs(logistics.manifests) do if record.id==id then return record end end
end
local function operation(logistics,id)
 for _,record in ipairs(logistics.operations) do if record.id==id then return record end end
end
local function workerByPerson(w,id)
 for _,worker in ipairs(w.workers) do if worker.personId==id then return worker end end
end
local function cargoTotal(cargo)
 local total=0;for _,kind in ipairs(resources) do total=total+(cargo[kind] or 0) end;return total
end
local function sameCargo(a,b)
 for _,kind in ipairs(resources) do if (a[kind] or 0)~=(b[kind] or 0) then return false end end
 return true
end
local function samePeople(a,b)
 if #a~=#b then return false end
 for i=1,#a do if a[i]~=b[i] then return false end end
 return true
end
local function currentManifestForCraft(logistics,record)
 return record.activeManifestId and manifest(logistics,record.activeManifestId) or nil
end
local function validResource(kind) return type(kind)=='string' and known[kind] end
local function normalizeCargo(value)
 assert(type(value)=='table','Expedition cargo must be a table')
 local out={}
 for key,n in pairs(value) do
  assert(validResource(key),'Unknown cargo resource')
  U.integer(n,'Cargo quantity',0,24)
  if n>0 then out[key]=n end
 end
 return out
end
local function normalizePeople(value)
 dense(value,'Expedition passenger list',32)
 local out,seen={},{}
 for _,id in ipairs(value) do
  U.integer(id,'Passenger person ID',1,100000000);assert(not seen[id],'Duplicate passenger')
  seen[id]=true;out[#out+1]=id
 end
 table.sort(out);return out
end
local function findPerson(c,id)
 for _,record in ipairs(c.sites) do
  local worker=workerByPerson(record.world,id)
  if worker then return record,worker end
 end
end
local function manifestHasPerson(logistics,id,except)
 for _,record in ipairs(logistics.manifests) do
  if record~=except then for _,person in ipairs(record.passengers) do if person==id then return true end end end
 end
 return false
end
local function hasOperation(logistics,craftId)
 for _,record in ipairs(logistics.operations) do if record.craftId==craftId then return record end end
end

function L.enabled(c) return c.features and c.features.logistics==1 end
function L.resources() return resources end
function L.craft(c,id) return c.logistics and craft(c.logistics,id) end
function L.manifest(c,id) return c.logistics and manifest(c.logistics,id) end
function L.operation(c,id) return c.logistics and operation(c.logistics,id) end
function L.craftsAt(c,siteId)
 local out={};if not c.logistics then return out end
 for _,record in ipairs(c.logistics.crafts) do if record.dockedSiteId==siteId then out[#out+1]=record end end
 table.sort(out,function(a,b) return a.id<b.id end);return out
end

function L.new(homeSite,travel)
 local w=homeSite.world;assert(w.home,'Campaign home needs a landing marker')
 local anchor={x=w.home.x,y=w.home.y}
 assert(N.stand(w,anchor.x,anchor.y,true),'Campaign home craft anchor is not a safe standing pose')
 local craft={id=1,ownerSocietyId=1,seats=3,capacity=24,dockedSiteId=homeSite.id,anchor=anchor,cargo={},activeManifestId=nil}
 if travel then craft.passengers={};craft.journey=nil end
 return {version=L.version,rules={version=1,seats=3,cargoCapacity=24,maintenanceMetal=1,assemblyRadius=8},
  nextCraftId=2,nextManifestId=1,nextOperationId=1,
  crafts={craft},manifests={},operations={}}
end

local function validateCargo(cargo,capacity,label)
 allowed(cargo,known,label)
 local total=0
 for _,kind in ipairs(resources) do
  local n=cargo[kind]
  if n~=nil then U.integer(n,label..' '..kind,1,capacity);total=total+n end
 end
 assert(total<=capacity,label..' exceeds craft capacity')
 return total
end
local function validateDirective(worker,logistics,record)
 local d=worker.directive
 if not d or d.kind~='assembly' then return end
 exact(d,{kind=true,manifestId=true,revision=true,x=true,y=true},'Assembly directive')
 U.integer(d.manifestId,'Assembly directive manifest ID',1,logistics.nextManifestId-1)
 U.integer(d.revision,'Assembly directive revision',1,100000000)
 U.integer(d.x,'Assembly directive x',1,record.world.width);U.integer(d.y,'Assembly directive y',1,record.world.height)
 local m=manifest(logistics,d.manifestId)
 assert(m and m.sourceSiteId==record.id and m.revision==d.revision,'Dangling assembly directive')
end

function L.validate(c)
 local logistics=c.logistics
 local travel=c.features and c.features.travel==1
 exact(logistics,{version=true,rules=true,nextCraftId=true,nextManifestId=true,nextOperationId=true,crafts=true,manifests=true,operations=true},'Campaign logistics')
 assert(logistics.version==L.version,'Unsupported campaign logistics version')
 exact(logistics.rules,{version=true,seats=true,cargoCapacity=true,maintenanceMetal=true,assemblyRadius=true},'Campaign logistics rules')
 assert(logistics.rules.version==1,'Unsupported campaign logistics rules version')
 U.integer(logistics.rules.seats,'Craft seats',1,32);U.integer(logistics.rules.cargoCapacity,'Craft cargo capacity',1,1000)
 U.integer(logistics.rules.maintenanceMetal,'Craft maintenance metal',1,logistics.rules.cargoCapacity);U.integer(logistics.rules.assemblyRadius,'Assembly radius',1,32)
 local craftCount=dense(logistics.crafts,'Campaign crafts',L.maxCrafts);local manifestCount=dense(logistics.manifests,'Campaign manifests',L.maxManifests);local operationCount=dense(logistics.operations,'Campaign cargo operations',L.maxOperations)
 U.integer(logistics.nextCraftId,'Next craft ID',1,100000000);U.integer(logistics.nextManifestId,'Next manifest ID',1,100000000);U.integer(logistics.nextOperationId,'Next cargo operation ID',1,100000000)
 local seenCraft,maxCraft={},0
 for i=1,craftCount do
  local record=logistics.crafts[i]
  local craftKeys={id=true,ownerSocietyId=true,seats=true,capacity=true,dockedSiteId=true,anchor=true,cargo=true,activeManifestId=true}
  if travel then craftKeys.passengers=true;craftKeys.journey=true end
  allowed(record,craftKeys,'Campaign craft')
  for _,key in ipairs({'id','ownerSocietyId','seats','capacity','anchor','cargo'}) do assert(record[key]~=nil,'Missing Campaign craft key '..key) end
  U.integer(record.id,'Craft ID',1,logistics.nextCraftId-1);assert(not seenCraft[record.id],'Duplicate craft ID');seenCraft[record.id]=true;maxCraft=math.max(maxCraft,record.id)
  assert(record.ownerSocietyId==c.society.id,'Invalid craft ownership');U.integer(record.seats,'Craft seats',1,32);U.integer(record.capacity,'Craft capacity',1,1000)
  assert(record.seats==logistics.rules.seats and record.capacity==logistics.rules.cargoCapacity,'Craft rules differ from campaign rules')
  local dock=record.dockedSiteId and site(c,record.dockedSiteId) or nil
  if travel then
   assert(type(record.passengers)=='table','Travel craft passengers are missing')
   assert((record.journey==nil)==(record.dockedSiteId~=nil),'Craft dock/journey state mismatch')
  else assert(dock,'Craft docking site is missing') end
  if dock then assert(dock.ownerSocietyId==c.society.id,'Craft is not docked at an owned site') end
  exact(record.anchor,{x=true,y=true},'Craft anchor');U.integer(record.anchor.x,'Craft anchor x',1,(dock and dock.world.width) or 512);U.integer(record.anchor.y,'Craft anchor y',1,(dock and dock.world.height) or 256)
  local packed=validateCargo(record.cargo,record.capacity,'Craft cargo')
  if c.features and c.features.equipment==1 then assert(packed+require('src.equipment').craftCount(c,record.id)<=record.capacity,'Craft equipment exceeds cargo capacity') end
  if record.activeManifestId~=nil then U.integer(record.activeManifestId,'Active manifest ID',1,logistics.nextManifestId-1) end
 end
 assert(logistics.nextCraftId>maxCraft,'Next craft ID was already allocated')
 local seenManifest,maxManifest,manifestByCraft={},0,{}
 for i=1,manifestCount do
  local record=logistics.manifests[i]
  exact(record,{id=true,revision=true,craftId=true,sourceSiteId=true,destinationSiteId=true,passengers=true,cargo=true,assembly=true},'Expedition manifest')
  U.integer(record.id,'Manifest ID',1,logistics.nextManifestId-1);assert(not seenManifest[record.id],'Duplicate manifest ID');seenManifest[record.id]=true;maxManifest=math.max(maxManifest,record.id)
  U.integer(record.revision,'Manifest revision',1,100000000);local source=site(c,record.sourceSiteId);local destination=site(c,record.destinationSiteId)
  assert(source and destination and source.id~=destination.id,'Invalid expedition route');assert(source.ownerSocietyId==c.society.id,'Manifest source is not owned')
  local vehicle=craft(logistics,record.craftId);assert(vehicle and vehicle.dockedSiteId==source.id and vehicle.ownerSocietyId==c.society.id and not vehicle.journey,'Manifest craft/source mismatch')
  assert(not manifestByCraft[record.craftId],'Multiple active manifests for one craft');manifestByCraft[record.craftId]=record.id
  dense(record.passengers,'Manifest passengers',record.seats or 32);local prior=0
  for _,personId in ipairs(record.passengers) do
   U.integer(personId,'Manifest person ID',1,100000000);assert(personId>prior,'Manifest passengers are not sorted');prior=personId
   local personSite=select(1,findPerson(c,personId));assert(personSite and personSite.id==source.id,'Manifest passenger is not at its source site')
  end
  if destination.ownerSocietyId~=c.society.id then assert(#record.passengers>0,'Unowned destination needs a passenger') end
  local planned=validateCargo(record.cargo,vehicle.capacity,'Manifest cargo')
  if c.features and c.features.equipment==1 then assert(planned+require('src.equipment').craftCount(c,vehicle.id)<=vehicle.capacity,'Manifest plus equipment exceeds cargo capacity') end
  dense(record.assembly,'Assembly positions',32);local seenAssembly={}
  for _,pose in ipairs(record.assembly) do
   exact(pose,{personId=true,x=true,y=true},'Assembly position');U.integer(pose.personId,'Assembly person ID',1,100000000);assert(not seenAssembly[pose.personId],'Duplicate assembly person');seenAssembly[pose.personId]=true
   U.integer(pose.x,'Assembly x',1,source.world.width);U.integer(pose.y,'Assembly y',1,source.world.height)
  end
 end
 assert(logistics.nextManifestId>maxManifest,'Next manifest ID was already allocated')
 for _,record in ipairs(logistics.crafts) do
  assert((record.activeManifestId==nil)==(manifestByCraft[record.id]==nil),'Craft manifest link mismatch')
  if record.activeManifestId then assert(record.activeManifestId==manifestByCraft[record.id],'Craft manifest link differs') end
 end
 local seenOperation,maxOperation,operationByCraftResource={},0,{}
 for i=1,operationCount do
  local record=logistics.operations[i]
  exact(record,{id=true,craftId=true,sourceSiteId=true,resource=true,amount=true,remaining=true},'Cargo unload operation')
  U.integer(record.id,'Cargo operation ID',1,logistics.nextOperationId-1);assert(not seenOperation[record.id],'Duplicate cargo operation ID');seenOperation[record.id]=true;maxOperation=math.max(maxOperation,record.id)
  local vehicle=craft(logistics,record.craftId);local source=site(c,record.sourceSiteId);assert(vehicle and source and vehicle.dockedSiteId==source.id,'Cargo operation craft/source mismatch')
  assert(validResource(record.resource),'Unknown cargo operation resource');U.integer(record.amount,'Cargo operation amount',1,vehicle.capacity);U.integer(record.remaining,'Cargo operation remaining',0,record.amount)
  local key=record.craftId..':'..record.resource;assert(not operationByCraftResource[key],'Duplicate cargo unload operation');operationByCraftResource[key]=true
 end
 assert(logistics.nextOperationId>maxOperation,'Next cargo operation ID was already allocated')
 for _,record in ipairs(c.sites) do for _,worker in ipairs(record.world.workers) do validateDirective(worker,logistics,record) end end
 for _,record in ipairs(c.sites) do
  for _,job in ipairs(record.world.jobs) do if job.logistics then
   allowed(job.logistics,{craftId=true,manifestId=true,revision=true,resource=true,remaining=true,operationId=true},'Cargo job reference')
   for _,key in ipairs({'craftId','resource','remaining'}) do assert(job.logistics[key]~=nil,'Missing Cargo job reference key '..key) end
   local vehicle=craft(logistics,job.logistics.craftId);assert(vehicle,'Cargo job craft is missing')
   assert(validResource(job.logistics.resource),'Unknown cargo job resource');U.integer(job.logistics.remaining,'Cargo job remaining',0,vehicle.capacity)
   if job.state=='open' then
    assert(vehicle.dockedSiteId==record.id,'Cargo job craft/site mismatch')
    local m=job.logistics.manifestId and manifest(logistics,job.logistics.manifestId) or nil
    local op=job.logistics.operationId and operation(logistics,job.logistics.operationId) or nil
    assert((job.kind=='load' and m and not op) or (job.kind=='unload' and op and not m),'Invalid cargo job kind/reference')
    if m then assert(m.sourceSiteId==record.id and m.revision==job.logistics.revision,'Stale cargo job manifest reference') end
    if op then assert(op.sourceSiteId==record.id and op.craftId==vehicle.id,'Cargo job operation reference mismatch') end
   else assert(job.kind=='load' or job.kind=='unload','Invalid terminal cargo job kind') end
  end end
 end
 return true
end

local function commandPlan(c,command)
 local logistics=assert(c.logistics,'Logistics are unavailable in this older campaign')
 local source=site(c,command.sourceSiteId);assert(source,'Unknown source site');assert(source.ownerSocietyId==c.society.id,'Source site is not owned by this society')
 local vehicle=craft(logistics,command.craftId);assert(vehicle,'Unknown craft');assert(vehicle.ownerSocietyId==c.society.id and vehicle.dockedSiteId==source.id,'Craft is not docked at the named source site')
 return logistics,source,vehicle
end
local function preparePlan(c,command)
 local logistics,source,vehicle=commandPlan(c,command);local destination=site(c,command.destinationSiteId)
 assert(destination and destination.id~=source.id,'Invalid expedition destination')
 local passengers,cargo=normalizePeople(command.passengers),normalizeCargo(command.cargo)
 assert(cargoTotal(cargo)+(c.features.equipment==1 and require('src.equipment').craftCount(c,vehicle.id) or 0)<=vehicle.capacity,'Cargo exceeds craft capacity')
 local current=currentManifestForCraft(logistics,vehicle)
 local same=current and current.destinationSiteId==destination.id and samePeople(current.passengers,passengers) and sameCargo(current.cargo,cargo)
 if same then return {kind='same',logistics=logistics,source=source,vehicle=vehicle,manifest=current,passengers=passengers,cargo=cargo,destination=destination} end
 if current and hasOperation(logistics,vehicle.id) then error('Finish or cancel unloading first') end
 local retained={};if current then for _,id in ipairs(current.passengers) do retained[id]=true end end
 for _,personId in ipairs(passengers) do
  local personSite,worker=findPerson(c,personId)
  assert(personSite and personSite.id==source.id,'Passenger is absent from the named source site')
  assert((worker.alive or retained[personId]),'Select a living passenger')
  assert(not manifestHasPerson(logistics,personId,current),'Passenger is reserved by another manifest')
 end
 if destination.ownerSocietyId~=c.society.id then
  local living=false;for _,personId in ipairs(passengers) do local _,worker=findPerson(c,personId);if worker and worker.alive then living=true end end
  assert(living,'Unowned destination requires a living passenger')
 end
 return {kind=current and 'edit' or 'new',logistics=logistics,source=source,vehicle=vehicle,manifest=current,passengers=passengers,cargo=cargo,destination=destination}
end

function L.valid(c,command)
 local ok,result=pcall(function()
  if command.type=='prepare_expedition' then return preparePlan(c,command) end
  local logistics,source,vehicle=commandPlan(c,command)
  if command.type=='cancel_expedition' or command.type=='assemble_expedition' then
   local m=manifest(logistics,command.manifestId);assert(m and m.craftId==vehicle.id and m.sourceSiteId==source.id,'Manifest is not active at the named source')
   return {logistics=logistics,source=source,vehicle=vehicle,manifest=m}
  elseif command.type=='unload_cargo' then
   assert(validResource(command.resource),'Unknown cargo resource');U.integer(command.amount,'Unload amount',1,vehicle.capacity)
   local current=currentManifestForCraft(logistics,vehicle);local existing
   for _,op in ipairs(logistics.operations) do if op.craftId==vehicle.id and op.resource==command.resource then existing=op end end
   if existing then assert(existing.amount==command.amount,'Conflicting unload is already active');return {logistics=logistics,source=source,vehicle=vehicle,operation=existing,same=true} end
   assert((vehicle.cargo[command.resource] or 0)>=command.amount,'Craft cargo is insufficient')
   if current then assert(command.amount<=math.max(0,(vehicle.cargo[command.resource] or 0)-(current.cargo[command.resource] or 0)),'Lower the target or cancel preparation before unloading intended cargo') end
   return {logistics=logistics,source=source,vehicle=vehicle}
  elseif command.type=='cancel_cargo_unload' then
   local op=operation(logistics,command.operationId);assert(op and op.craftId==vehicle.id and op.sourceSiteId==source.id,'Cargo unload operation is not active at the named source')
   return {logistics=logistics,source=source,vehicle=vehicle,operation=op}
  end
  error('Unknown campaign logistics command')
 end)
 return ok,ok and result or tostring(result)
end

local function cancelManifestJobs(c,m)
 local J=require('src.jobs');local source=site(c,m.sourceSiteId)
 for _,job in ipairs(source.world.jobs) do
  if job.logistics and job.logistics.manifestId==m.id then J.cancel(source.world,job) end
 end
 for _,worker in ipairs(source.world.workers) do
  local d=worker.directive
  if d and d.kind=='assembly' and d.manifestId==m.id and d.revision==m.revision then
   if worker.task and worker.task.kind=='rally' then J.release(source.world,worker,true) end
   worker.directive=nil;worker.thinkAt=source.world.tick
  end
 end
 m.assembly={}
end
local function removeRecord(array,record)
 for i,value in ipairs(array) do if value==record then table.remove(array,i);return end end
end
local function cargoExact(vehicle,m) return sameCargo(vehicle.cargo,m.cargo) end
local function outstanding(c,vehicle)
 local source=site(c,vehicle.dockedSiteId)
 for _,job in ipairs(source.world.jobs) do if job.state=='open' and job.logistics and job.logistics.craftId==vehicle.id then return true end end
 for _,op in ipairs(c.logistics.operations) do if op.craftId==vehicle.id then return true end end
 return false
end
local function sortedPeople(m)
 local out={};for i,id in ipairs(m.passengers) do out[i]=id end;table.sort(out);return out
end
local function posesFor(c,m)
 local source=site(c,m.sourceSiteId);local vehicle=craft(c.logistics,m.craftId);local w=source.world;local radius=c.logistics.rules.assemblyRadius
 local candidates={}
 for y=math.max(1,vehicle.anchor.y-radius),math.min(w.height,vehicle.anchor.y+radius) do for x=math.max(1,vehicle.anchor.x-radius),math.min(w.width,vehicle.anchor.x+radius) do
  local distance=math.abs(x-vehicle.anchor.x)+math.abs(y-vehicle.anchor.y)
  if distance<=radius and N.stand(w,x,y,true) and (not w.workClaims or not w.workClaims[W.index(w,x,y)]) then candidates[#candidates+1]={x=x,y=y,distance=distance} end
 end end
 table.sort(candidates,function(a,b) if a.distance~=b.distance then return a.distance<b.distance end;if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
 local used,out={},{}
 for _,personId in ipairs(sortedPeople(m)) do
  local worker=workerByPerson(w,personId);assert(worker and worker.alive,'Assembly needs a living source passenger')
  local reachable=N.flood(w,worker.x,worker.y,true)
  local selected
  for _,candidate in ipairs(candidates) do
   local key=candidate.x..':'..candidate.y
   if not used[key] and reachable.parent[W.index(w,candidate.x,candidate.y)]~=nil then selected=candidate;used[key]=true;break end
  end
  assert(selected,'No adequate safe, reachable assembly pose')
  out[#out+1]={personId=personId,x=selected.x,y=selected.y}
 end
 return out
end

function L.readiness(c,m)
 if not L.enabled(c) then return false,'Logistics unavailable' end
 local vehicle=craft(c.logistics,m.craftId);local source=site(c,m.sourceSiteId);local destination=site(c,m.destinationSiteId)
 if not vehicle or not source or not destination or vehicle.dockedSiteId~=source.id then return false,'Craft route is no longer valid' end
 if not cargoExact(vehicle,m) then return false,'Cargo does not match the manifest target' end
 if (vehicle.cargo.metal or 0)<c.logistics.rules.maintenanceMetal then return false,'Future departure needs one metal unit aboard' end
 if outstanding(c,vehicle) then return false,'Cargo operations are still outstanding' end
 local assigned={};for _,pose in ipairs(m.assembly) do assigned[pose.personId]=pose end
 for _,personId in ipairs(m.passengers) do
  local worker=workerByPerson(source.world,personId);local pose=assigned[personId]
  if not worker or not worker.alive then return false,'A selected passenger is no longer living' end
  if not pose or worker.x~=pose.x or worker.y~=pose.y then return false,'Passengers are not assembled' end
  local d=worker.directive
  if not d or d.kind~='assembly' or d.manifestId~=m.id or d.revision~=m.revision or d.x~=pose.x or d.y~=pose.y then return false,'Assembly directive changed' end
  if not N.stand(source.world,worker.x,worker.y,true) then return false,'Assembly pose is no longer safe' end
 end
 return true,c.features.travel==1 and 'Preparation ready' or 'Preparation ready; departure is not implemented'
end

function L.closeManifest(c,m)
 local vehicle=craft(c.logistics,m.craftId);assert(vehicle and vehicle.activeManifestId==m.id,'Manifest is not active')
 cancelManifestJobs(c,m);vehicle.activeManifestId=nil;removeRecord(c.logistics.manifests,m)
 return true
end

function L.apply(c,command)
 local ok,plan=L.valid(c,command);if not ok then return false,plan end
 if command.type=='prepare_expedition' then
  if plan.kind=='same' then return true,'Expedition plan is already active' end
  local m=plan.manifest
  if m then
   cancelManifestJobs(c,m);m.revision=m.revision+1;m.destinationSiteId=plan.destination.id;m.passengers=plan.passengers;m.cargo=plan.cargo;m.assembly={}
   return true,'Expedition plan updated'
  end
  local id=plan.logistics.nextManifestId;plan.logistics.nextManifestId=id+1
  m={id=id,revision=1,craftId=plan.vehicle.id,sourceSiteId=plan.source.id,destinationSiteId=plan.destination.id,passengers=plan.passengers,cargo=plan.cargo,assembly={}}
  plan.logistics.manifests[#plan.logistics.manifests+1]=m;plan.vehicle.activeManifestId=id
  return true,'Expedition plan prepared'
 elseif command.type=='cancel_expedition' then
  L.closeManifest(c,plan.manifest)
  return true,'Expedition preparation cancelled; loaded cargo remains aboard'
 elseif command.type=='assemble_expedition' then
  local m=plan.manifest
  if not cargoExact(plan.vehicle,m) then return false,'Cargo does not match the manifest target' end
  if (plan.vehicle.cargo.metal or 0)<plan.logistics.rules.maintenanceMetal then return false,'Future departure needs one metal unit aboard' end
  if outstanding(c,plan.vehicle) then return false,'Finish or cancel cargo operations before assembling' end
  local posed,positions=pcall(posesFor,c,m)
  if not posed then return false,tostring(positions) end
  local source=plan.source;local J=require('src.jobs')
  local unchanged=#positions==#m.assembly
  if unchanged then for i,pose in ipairs(positions) do local old=m.assembly[i];if not old or old.personId~=pose.personId or old.x~=pose.x or old.y~=pose.y then unchanged=false;break end end end
  if unchanged then
   local ready=true;for _,pose in ipairs(positions) do local worker=workerByPerson(source.world,pose.personId);local d=worker and worker.directive;if not d or d.kind~='assembly' or d.manifestId~=m.id or d.revision~=m.revision or d.x~=pose.x or d.y~=pose.y then ready=false;break end end
   if ready then return true,'Assembly already requested' end
  end
  for _,worker in ipairs(source.world.workers) do
   local d=worker.directive
   if d and d.kind=='assembly' and d.manifestId==m.id and d.revision==m.revision then if worker.task and worker.task.kind=='rally' then J.release(source.world,worker,true) end;worker.directive=nil end
  end
  m.assembly=positions
  for _,pose in ipairs(positions) do
   local worker=workerByPerson(source.world,pose.personId)
   if worker.task then J.release(source.world,worker,true) end
   worker.directive={kind='assembly',manifestId=m.id,revision=m.revision,x=pose.x,y=pose.y};worker.thinkAt=source.world.tick
  end
  return true,'Assembly requested'
 elseif command.type=='unload_cargo' then
  if plan.same then return true,'Cargo unload is already active' end
  local id=plan.logistics.nextOperationId;plan.logistics.nextOperationId=id+1
  plan.logistics.operations[#plan.logistics.operations+1]={id=id,craftId=plan.vehicle.id,sourceSiteId=plan.source.id,resource=command.resource,amount=command.amount,remaining=command.amount}
  return true,'Cargo unload requested'
 elseif command.type=='cancel_cargo_unload' then
  local J=require('src.jobs')
  for _,job in ipairs(plan.source.world.jobs) do if job.logistics and job.logistics.operationId==plan.operation.id then J.cancel(plan.source.world,job) end end
  removeRecord(plan.logistics.operations,plan.operation)
  return true,'Cargo unload cancelled'
 end
 return false,'Unknown campaign logistics command'
end

local function jobRecord(c,siteId,job)
 local ref=job.logistics;if not ref or job.state~='open' then return end
 local vehicle=craft(c.logistics,ref.craftId);if not vehicle or vehicle.dockedSiteId~=siteId then return end
 if job.kind=='load' then
  local m=manifest(c.logistics,ref.manifestId)
  if not m or m.revision~=ref.revision or m.sourceSiteId~=siteId or m.craftId~=vehicle.id then return end
  return vehicle,m,nil
 elseif job.kind=='unload' then
  local op=operation(c.logistics,ref.operationId)
  if not op or op.sourceSiteId~=siteId or op.craftId~=vehicle.id then return end
  return vehicle,nil,op
 end
end
function L.job(c,siteId,job) return jobRecord(c,siteId,job) end
local function addJob(w,kind,vehicle,resource,remaining,manifestRecord,operationRecord)
 if #w.jobs>=1024 then return nil,'Job limit reached' end
 local gx,gy=W.tile(w,vehicle.anchor.x,vehicle.anchor.y)
 local reference={craftId=vehicle.id,manifestId=manifestRecord and manifestRecord.id or nil,revision=manifestRecord and manifestRecord.revision or nil,
  resource=resource,remaining=remaining,operationId=operationRecord and operationRecord.id or nil}
 local job={id=W.id(w),kind=kind,gx=gx,gy=gy,build=nil,priority=2,state='open',delivered=0,progress=0,reason='Waiting for a hauler',logistics=reference}
 w.jobs[#w.jobs+1]=job;return job
end
local function cancelStale(c,siteId)
 local J=require('src.jobs');local source=site(c,siteId)
 for _,job in ipairs(source.world.jobs) do if job.logistics and job.state=='open' and not jobRecord(c,siteId,job) then J.cancel(source.world,job) end end
end
local function pendingLoad(c,source,m,resource)
 local n=0
 for _,job in ipairs(source.world.jobs) do
  local ref=job.logistics
  if job.state=='open' and ref and job.kind=='load' and ref.manifestId==m.id and ref.revision==m.revision and ref.resource==resource then n=n+ref.remaining end
 end
 return n
end
local function activeUnloadJob(source,op)
 for _,job in ipairs(source.world.jobs) do if job.state=='open' and job.logistics and job.logistics.operationId==op.id then return job end end
end
function L.reconcile(c)
 if not L.enabled(c) then return end
 for _,record in ipairs(c.sites) do cancelStale(c,record.id) end
 for _,m in ipairs(c.logistics.manifests) do
  local source=site(c,m.sourceSiteId);local vehicle=craft(c.logistics,m.craftId)
  for _,resource in ipairs(resources) do
   local wanted=(m.cargo[resource] or 0)-(vehicle.cargo[resource] or 0)-pendingLoad(c,source,m,resource)
   if wanted>0 then addJob(source.world,'load',vehicle,resource,wanted,m,nil) end
  end
 end
 local finished={}
 for _,op in ipairs(c.logistics.operations) do
  local source=site(c,op.sourceSiteId);local vehicle=craft(c.logistics,op.craftId);local job=activeUnloadJob(source,op)
  if op.remaining>0 and not job then addJob(source.world,'unload',vehicle,op.resource,op.remaining,nil,op)
  elseif op.remaining==0 and (not job or not job.assigned) then
   if job then job.state='done';job.reason='Completed';source.world.stats.jobsDone=source.world.stats.jobsDone+1 end
   finished[#finished+1]=op
  end
 end
 for i=#finished,1,-1 do removeRecord(c.logistics.operations,finished[i]) end
end

function L.offer(w,a,context,f,closest,itemChoice,offer)
 if not context or not context.campaign or not L.enabled(context.campaign) then return end
 local c=context.campaign;local siteId=context.siteId
 for _,job in ipairs(w.jobs) do
  if job.state=='open' and not job.assigned and job.logistics then
   local vehicle,m,op=jobRecord(c,siteId,job)
   if vehicle and job.logistics.remaining>0 then
    local resource=job.logistics.resource
    if job.kind=='load' then
     local pile,path,node,distance=itemChoice(w,a,f,resource,true)
     if pile and N.reach(w,pile.x,pile.y,vehicle.anchor.x,vehicle.anchor.y,4) then
      offer({kind='cargo',stage='fetch',job=job.id,item=pile.id,path=path,node=node,label='Loading '..resource},200,distance)
     else job.reason='Needs accessible '..resource..' with a route to the craft' end
    elseif job.kind=='unload' then
     local path,node,distance=closest(w,a,f,function(x,y) return N.reach(w,x,y,vehicle.anchor.x,vehicle.anchor.y,4) end)
     if path then offer({kind='cargo',stage='fetch',job=job.id,path=path,node=node,label='Unloading '..resource},200,distance)
     else job.reason='Craft is unreachable' end
    end
   end
  end
 end
end

function L.loadFetch(c,siteId,job,pile,a)
 local vehicle,m=jobRecord(c,siteId,job);if not vehicle or not m or job.kind~='load' then return nil,'Cargo job is no longer current' end
 if pile.kind~=job.logistics.resource or pile.n<=0 then return nil,'Supply moved or became inaccessible' end
 local n=math.min(pile.n,12,job.logistics.remaining)
 if n<=0 then return nil,'Cargo target is already satisfied' end
 return n
end
function L.loadDeliver(c,siteId,job,a)
 local vehicle,m=jobRecord(c,siteId,job);if not vehicle or not m or job.kind~='load' then return false,'Cargo job is no longer current' end
 local carry=a.carry
 if not carry or carry.kind~=job.logistics.resource or carry.n<=0 then return false,'Missing carried cargo' end
 if not N.reach(site(c,siteId).world,a.x,a.y,vehicle.anchor.x,vehicle.anchor.y,4) then return false,'Craft is no longer reachable' end
 local current=vehicle.cargo[carry.kind] or 0
 if carry.n>job.logistics.remaining or current+carry.n>(m.cargo[carry.kind] or 0) or cargoTotal(vehicle.cargo)+carry.n+((c.features.equipment==1 and require('src.equipment').craftCount(c,vehicle.id)) or 0)>vehicle.capacity then return false,'Craft cargo target changed' end
 vehicle.cargo[carry.kind]=current+carry.n;job.logistics.remaining=job.logistics.remaining-carry.n;a.carry=nil
 if job.logistics.remaining==0 then job.state='done';job.reason='Loaded';site(c,siteId).world.stats.jobsDone=site(c,siteId).world.stats.jobsDone+1 end
 return true
end
function L.unloadFetch(c,siteId,job,a)
 local vehicle,_,op=jobRecord(c,siteId,job);if not vehicle or not op or job.kind~='unload' then return nil,'Cargo unload is no longer current' end
 local available=vehicle.cargo[job.logistics.resource] or 0;local n=math.min(available,12,job.logistics.remaining,op.remaining)
 if n<=0 then return nil,'Craft cargo is no longer available' end
 if not N.reach(site(c,siteId).world,a.x,a.y,vehicle.anchor.x,vehicle.anchor.y,4) then return nil,'Craft is no longer reachable' end
 vehicle.cargo[job.logistics.resource]=available-n;job.logistics.remaining=job.logistics.remaining-n;op.remaining=op.remaining-n
 return n
end
function L.unloadDeliver(c,siteId,job,a)
 if not a.carry then return false,'Missing carried cargo' end
 W.stack(site(c,siteId).world,a.carry.kind,a.carry.n,a.x,a.y);a.carry=nil
 return true
end
function L.routeToCraft(c,siteId,w,a,job,reRoute)
 local vehicle=select(1,jobRecord(c,siteId,job));if not vehicle then return false end
 return reRoute(w,a,function(x,y) return N.reach(w,x,y,vehicle.anchor.x,vehicle.anchor.y,4) end)
end

function L.reconciliation(c,siteId)
 local source=site(c,siteId);assert(source,'Unknown campaign site');local result={ground={},carry={},escrow={},cargo={},total={}}
 for _,kind in ipairs(resources) do result.ground[kind]=0;result.carry[kind]=0;result.escrow[kind]=0;result.cargo[kind]=0;result.total[kind]=0 end
 for _,item in ipairs(source.world.items) do if result.ground[item.kind] then result.ground[item.kind]=result.ground[item.kind]+item.n end end
 for _,worker in ipairs(source.world.workers) do if worker.carry and result.carry[worker.carry.kind] then result.carry[worker.carry.kind]=result.carry[worker.carry.kind]+worker.carry.n end end
 for _,job in ipairs(source.world.jobs) do if (job.delivered or 0)>0 and job.build then local def=S.def[job.build];if def and result.escrow[def.resource] then result.escrow[def.resource]=result.escrow[def.resource]+job.delivered end end end
 for _,vehicle in ipairs(L.craftsAt(c,siteId)) do for _,kind in ipairs(resources) do result.cargo[kind]=result.cargo[kind]+(vehicle.cargo[kind] or 0) end end
 for _,kind in ipairs(resources) do result.total[kind]=result.ground[kind]+result.carry[kind]+result.escrow[kind]+result.cargo[kind] end
 return result
end
return L
