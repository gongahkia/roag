-- Final mature P01--G08 endurance soak.  Unlike the progression harness this
-- keeps its recovered relic/drive/remote settlement campaign live for a long
-- bounded run, validating the real campaign state periodically and reporting
-- the resulting retained state rather than inventing a separate benchmark.
local Campaign=require('src.campaign')
local Codec=require('src.campaign_codec')
local Commands=require('src.campaign_commands')
local Equipment=require('src.equipment')
local Industry=require('src.industry')
local Logistics=require('src.logistics')
local Relics=require('src.relics')
local Structures=require('src.structures')
local World=require('src.world')
local F=require('tests.fixtures')
local M=require('src.materials')

local seed=assert(tonumber(arg[1]),'usage: g08_soak.lua <seed> <ticks>')
local ticks=assert(tonumber(arg[2]),'usage: g08_soak.lua <seed> <ticks>')
assert(ticks>=60000 and ticks<=150000,'ticks must be 60000..150000')
local function options()
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
  logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true,relics=true}
end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y1-1,M.AIR);F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind) end
local function step(c,n) for _=1,n do Campaign.step(c) end end
local function worker(c,siteId,personId)
 for _,a in ipairs(c.sites[siteId].world.workers) do if a.personId==personId then return a end end
end
local function command(c,envelope) local ok,why=Commands.apply(c,envelope);assert(ok,why) end
local function unload(c,siteId)
 local vehicle=c.logistics.crafts[1]
 for _,kind in ipairs(Logistics.resources()) do
  local amount=vehicle.cargo[kind] or 0
  if amount>0 then command(c,{scope='campaign',type='unload_cargo',sourceSiteId=siteId,craftId=vehicle.id,resource=kind,amount=amount}) end
 end
 step(c,600)
 for _,kind in ipairs(Logistics.resources()) do assert((vehicle.cargo[kind] or 0)==0,'Unloading left stale '..kind..' cargo') end
end
local function travel(c,from,to,personId,food,metal,component,aboard)
 local w=c.sites[from].world;local a=assert(worker(c,from,personId),'Traveller missing before departure')
 -- Cache recovery deliberately leaves the archaeologist at an exposed cache.
 -- For this compact integration fixture, place that same body at the already
 -- valid landed-shuttle assembly area; departure itself still uses normal
 -- manifest loading, assembly validation, custody transfer, and transit.
 local craft=c.logistics.crafts[1];a.x,a.y=craft.anchor.x,craft.anchor.y
 if not aboard then
  if food>0 then World.stack(w,'food',food,a.x,a.y) end
  if metal>0 then World.stack(w,'metal',metal,a.x,a.y) end
  if component>0 then World.stack(w,'component',component,a.x,a.y) end
 end
 local cargo={food=food,metal=metal,component=component,stone=0,water=0}
 command(c,{scope='campaign',type='prepare_expedition',sourceSiteId=from,craftId=1,destinationSiteId=to,passengers={personId},cargo=cargo})
 step(c,700);local m=Logistics.manifest(c,c.logistics.crafts[1].activeManifestId);assert(m,'Manifest disappeared at tick '..c.tick..' active='..tostring(c.logistics.crafts[1].activeManifestId)..' dock='..tostring(c.logistics.crafts[1].dockedSiteId));command(c,{scope='campaign',type='assemble_expedition',sourceSiteId=from,craftId=1,manifestId=m.id})
 step(c,300);m=assert(Logistics.manifest(c,c.logistics.crafts[1].activeManifestId));assert(Logistics.readiness(c,m),'Expedition did not physically load and assemble')
 command(c,{scope='campaign',type='launch_expedition',sourceSiteId=from,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision})
 step(c,assert(require('src.travel').duration(c,from,to))+1)
 assert(c.sites[to].world and c.logistics.crafts[1].dockedSiteId==to,'Travel did not complete at destination '..to)
end
local function recover(c,siteId,cacheId,personId,relicId)
 local w=c.sites[siteId].world;local a=assert(worker(c,siteId,personId));local q=assert(Relics.cache(c,cacheId))
 a.x,a.y=q.gx*4-2,q.gy*4;q.workerId=personId
 for _=1,180 do assert(Relics.act(c,w,a,{mode='excavate',cacheId=cacheId})) end
 assert(q.empty,'Ancient cache did not remain empty after 180 work actions')
 assert(Relics.pickup(c,siteId,relicId,personId),'Recovered relic was not physically reachable')
 assert(Relics.find(c,relicId).state=='person','Recovered relic duplicated rather than entering person custody')
end

local c=Campaign.newRegion(seed,options());local home=c.sites[1].world;local personId=home.workers[1].personId
local boot=#Codec.encode(c)
-- Fabricate (rather than grant) one environmental suit before the three local
-- archaeology expeditions.  The worker then carries the same physical suit
-- through every normal shuttle transfer.
local fab=install(home,10,6,'fabricator');install(home,6,6,'solar_array');install(home,8,6,'power_pole')
assert(Industry.configure(home,fab,{recipe='frontier_suit'}));assert(Industry.addCargo(fab.input,{kind='metal',n=2},16));assert(Industry.addCargo(fab.input,{kind='component',n=1},16))
for _=1,500 do if fab.output[1] then break end;step(c,1) end
local made=assert(Industry.withdrawOne(fab),'Frontier Suit fabrication stalled');local suit=assert(Equipment.find(c,made.equipmentId));local homeWorker=assert(worker(c,1,personId));Equipment.drop(c,suit,1,homeWorker.x,homeWorker.y);Equipment.equip(c,suit,homeWorker,'environment')
assert(Equipment.equipped(c,personId,'environment')==suit,'Fabricated suit lost its unique physical custody')

-- The guaranteed K-3 core/lens/anchor are deliberately recovered from three
-- different G07 frontier bodies.  Each return unloads normal physical cargo
-- before the next leg, retaining only the relic in the traveller's custody.
local traces={}
for cacheId,siteId in ipairs({4,5,6}) do
 travel(c,1,siteId,personId,6,2,0);recover(c,siteId,cacheId,personId,cacheId)
 local a=assert(worker(c,siteId,personId));traces[#traces+1]='cache'..cacheId..'@site'..siteId..' relic#'..cacheId
 if cacheId==1 then
  local vehicle=c.logistics.crafts[1];a.x,a.y=vehicle.anchor.x,vehicle.anchor.y
  assert(Relics.loadCraft(c,siteId,vehicle.id,cacheId,personId),'First recovered relic did not enter physical shuttle cargo');traces[#traces+1]='relic#1 cargo site4->Home'
 end
 local held=c.logistics.crafts[1].cargo
 travel(c,siteId,1,personId,held.food or 0,held.metal or 0,held.component or 0,true);unload(c,1)
 if cacheId==1 then
  local homeWorker=assert(worker(c,1,personId));local vehicle=c.logistics.crafts[1];homeWorker.x,homeWorker.y=vehicle.anchor.x,vehicle.anchor.y
  assert(Relics.unloadCraft(c,1,vehicle.id,cacheId,personId),'Relic cargo did not leave the returned shuttle through the same worker')
 end
end
home=c.sites[1].world;homeWorker=assert(worker(c,1,personId));assert(Equipment.equipped(c,personId,'environment').id==suit.id,'Frontier archaeology travel cloned/lost the equipped suit')

-- Powered Analyzer work derives personal facts.  A deliberately separate
-- Relay and loaded decoded lens perform the 300-action deep scan; remote maps
-- remain absent until the later physical arrival.
local analyzer=install(home,13,6,'relic_analyzer');local relay=install(home,16,6,'signal_relay');analyzer._powerGranted=true;relay._powerGranted=true
homeWorker.x,homeWorker.y=analyzer.gx*4-2,analyzer.gy*4
for _,id in ipairs({1,2,3}) do
 assert(Relics.loadAnalyzer(c,1,analyzer.id,id,personId));assert(Relics.beginAnalysis(c,1,analyzer.id,id,personId))
 for _=1,300 do assert(Relics.act(c,home,homeWorker,{mode='analysis',structureId=analyzer.id})) end
 assert(Relics.knowsRole(homeWorker,id) and Relics.knowsSignature(homeWorker,id),'Analyzer did not retain decoded personal fact')
 assert(Relics.unloadAnalyzer(c,1,analyzer.id,id,personId))
end
assert(Relics.loadAnalyzer(c,1,analyzer.id,2,personId));assert(Relics.beginScan(c,1,analyzer.id,2,personId))
for _=1,300 do assert(Relics.act(c,home,homeWorker,{mode='scan',structureId=analyzer.id})) end
assert(Relics.revealed(c,2) and Relics.revealed(c,3) and not c.sites[8].world,'Lens scan revealed systems but incorrectly pre-instantiated a remote world')
assert(Relics.unloadAnalyzer(c,1,analyzer.id,2,personId))

-- The frame consumes concrete local inputs and each socket transfers the same
-- recovered object to the shuttle.  No relic is consumed by either refit or
-- later interstellar travel.
homeWorker.x,homeWorker.y=c.logistics.crafts[1].anchor.x,c.logistics.crafts[1].anchor.y;World.stack(home,'metal',4,homeWorker.x,homeWorker.y);World.stack(home,'component',3,homeWorker.x,homeWorker.y)
assert(Relics.beginFrame(c,1,1,personId));for _=1,300 do assert(Relics.act(c,home,homeWorker,{mode='frame'})) end
for _,id in ipairs({1,2,3}) do assert(Relics.socket(c,1,1,id,personId,id)) end
assert(Relics.compatibility(c,1,homeWorker)=='compatible' and Relics.driveStatus(c,1,homeWorker)=='understood','Decoded compatible K-3 set is not a stable drive')
local scanSave=#Codec.encode(c);local decoded=Codec.decode(Codec.encode(c));Campaign.validate(decoded);assert(Codec.encode(decoded)==Codec.encode(c),'Codec changed decoded relic/drive state')

-- A 2400-tick deep leg consumes exactly its local maintenance metal plus one
-- component.  Because this operator decoded all three installed objects it is
-- stable; the focused suite separately traces the deterministic unstable case.
travel(c,1,8,personId,5,2,1)
local remote=c.sites[8];local remoteWorld=assert(remote.world);local arrived=assert(worker(c,8,personId))
assert(remote.ownerSocietyId==c.society.id and arrived.personId==personId and Equipment.equipped(c,personId,'environment').id==suit.id,'First deep arrival lost one-campaign-person or physical suit identity')
local spentMetal=0;for _,entry in ipairs(c.travel.accounts.sites) do spentMetal=spentMetal+entry.consumed.metal end
assert(c.travel.accounts.sites[1].consumed.metal==4 and spentMetal==7 and c.travel.accounts.sites[1].consumed.component==1,'Departure maintenance accounting deviated from six local plus one deep leg')
-- The remote body remains an ordinary G07 settlement: local power and a
-- Regulator function there, then its remote cache is excavated by the same
-- worker under normal environmental state.
F.fill(remoteWorld,3,3,120,60,M.AIR);F.fill(remoteWorld,3,25,120,25,M.ROCK);arrived.x,arrived.y=46,24;World.stack(remoteWorld,'food',1000,arrived.x,arrived.y)
local solar=install(remoteWorld,6,6,'solar_array');install(remoteWorld,8,6,'power_pole');local regulator=install(remoteWorld,9,6,'environmental_regulator');step(c,20)
assert(regulator._powerGranted and Industry.topology(remoteWorld).networks[1].generation>0,'Remote G07 power/environment integration failed after deep arrival')
recover(c,8,4,personId,6)
assert(Relics.find(c,6).state=='person','Remote recovered relic did not enter the original traveller custody')
local remoteRelicAtRecovery=Relics.find(c,6).state
local arrivalSave=#Codec.encode(c);c=Codec.decode(Codec.encode(c));Campaign.validate(c)
local remaining=ticks-c.tick;assert(remaining>=0,'Final mature setup exceeded requested tick budget')
-- Validate in bounded 10k slices: this is both an endurance cleanup check and
-- a guard against a late stale actor/custody record surviving unnoticed until
-- serialization at the very end.
while remaining>0 do local slice=math.min(10000,remaining);step(c,slice);Campaign.validate(c);remaining=remaining-slice end
local final=#Codec.encode(c);assert(final<32*1024*1024,'G08 integrated save exceeds 32 MiB')
local maps=0;for _,record in ipairs(c.sites) do if record.world then maps=maps+1 end end
local d=c.relics.drive;local roles={};for _,relic in ipairs(c.relics.items) do roles[#roles+1]='#'..relic.id..':'..relic.role..'/'..relic.family..'/'..relic.state end
local people={};for _,record in ipairs(c.sites) do if record.world then for _,a in ipairs(record.world.workers) do assert(not people[a.personId],'Duplicate live person representation');people[a.personId]=true end end end
for _,craft in ipairs(c.logistics.crafts) do for _,p in ipairs(craft.passengers) do assert(not people[p.personId],'Traveller duplicated as a local body');people[p.personId]=true end end
print(('FINAL mature soak seed=%d ticks=%d save=%d->%d->%d maps=%d systems=%d destinations=%d people=%d caches=%d relics=%d trace=%s stableDrive=%s remote=site8 person=%d suit=%d remoteRelic=%s->%s solar=%d regulator=%s metalHome=%d metalCampaign=%d componentHome=%d codec=pass'):format(seed,ticks,boot,scanSave,final,maps,#c.region.systems,#c.sites,(function() local n=0;for _ in pairs(people) do n=n+1 end;return n end)(),#c.relics.caches,#c.relics.items,table.concat(traces,','),Relics.driveStatus(c,1,assert(worker(c,8,personId))),personId,suit.id,remoteRelicAtRecovery,Relics.find(c,6).state,Industry.topology(c.sites[8].world).networks[1].generation,regulator.status,c.travel.accounts.sites[1].consumed.metal,spentMetal,c.travel.accounts.sites[1].consumed.component))
print('FINAL mature relic ledger '..table.concat(roles,', ')..' / drive sockets '..table.concat(d.sockets,',')..' / remote cache4=excavated / validator every 10000 ticks')
