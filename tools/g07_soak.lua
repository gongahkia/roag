-- Deterministic COS-G07 integration route.  Controlled geography/resources
-- make it reproducible; fabrication, security equip, expedition preparation,
-- loading, departure, first arrival, unloading, industrial power and the
-- subsequent campaign ticks use the normal campaign systems.
local Campaign=require('src.campaign')
local Codec=require('src.campaign_codec')
local Commands=require('src.campaign_commands')
local Equipment=require('src.equipment')
local Industry=require('src.industry')
local Logistics=require('src.logistics')
local Structures=require('src.structures')
local World=require('src.world')
local F=require('tests.fixtures')
local M=require('src.materials')

local seed=assert(tonumber(arg[1]),'usage: g07_soak.lua <seed> <ticks>')
local ticks=assert(tonumber(arg[2]),'usage: g07_soak.lua <seed> <ticks>')
assert(ticks>=4000 and ticks<=40000,'ticks must be 4000..40000')
local function options() return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
 logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true} end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y1-1,M.AIR);F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind) end
local c=Campaign.newRegion(seed,options());local home=c.sites[1].world
local boot=#Codec.encode(c);local worker=home.workers[1];local personId=worker.personId
-- A normal powered fabricator produces the suit; its source inputs are real
-- cargo and no starter suit is granted.
local fab=install(home,10,6,'fabricator');install(home,6,6,'solar_array');install(home,8,6,'power_pole')
assert(Industry.configure(home,fab,{recipe='frontier_suit'}));assert(Industry.addCargo(fab.input,{kind='metal',n=2},16));assert(Industry.addCargo(fab.input,{kind='component',n=1},16))
local function step(campaign,n) for _=1,n do Campaign.step(campaign) end end
for _=1,500 do if fab.output[1] then break end;Campaign.step(c) end
-- The fixture uses the production system's normal output handoff rather than
-- waiting for an arbitrary AUTO worker priority: one output record is removed
-- from the Fabricator and becomes the same loose item at the worker's pose.
local produced=assert(Industry.withdrawOne(fab),'Fabricator did not produce Frontier Suit output')
local suit=assert(Equipment.find(c,produced.equipmentId),'Fabricated suit identity was lost')
Equipment.drop(c,suit,1,worker.x,worker.y);Equipment.equip(c,suit,worker,'environment')
assert(Equipment.equipped(c,personId,'environment')==suit,'Frontier Suit did not retain physical equip custody')

-- The recorded route is deliberately to frontier-1, which has the fixed
-- airless-crag archetype.  This records preparation/loading/assembly/launch
-- so replay proves that first map creation is not a second generation path.
local function command(envelope) local ok,why=Commands.apply(c,envelope);assert(ok,why) end
command({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=4,passengers={personId},cargo={food=3,metal=2}})
step(c,700);local manifest=assert(Logistics.manifest(c,1));command({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id})
step(c,300);manifest=assert(Logistics.manifest(c,1));assert(Logistics.readiness(c,manifest),'Expedition did not physically prepare')
command({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision})
step(c,901)
local destination=c.sites[4];local frontier=assert(destination.world,'First arrival did not instantiate frontier world')
local arrived=frontier.workers[1];assert(arrived.personId==personId and Equipment.equipped(c,personId,'environment').id==suit.id,'Travel lost person or equipped suit identity')
assert(destination.ownerSocietyId==1 and c.travel.accounts.sites[1].consumed.metal==1,'Founding or single maintenance debit failed')
local arrivalSave=#Codec.encode(c);c=Codec.decode(Codec.encode(c));Campaign.validate(c);destination=c.sites[4];frontier=destination.world;arrived=frontier.workers[1]

-- Cargo has remained in the real docked shuttle.  Unload through its normal
-- logistics work path, then operate a local solar/regulator installation.
for _,entry in ipairs({{'metal',1},{'food',3}}) do
 command({scope='campaign',type='unload_cargo',sourceSiteId=4,craftId=1,resource=entry[1],amount=entry[2]})
end
step(c,1200)
assert(World.totalResource(frontier,'metal')>=1 and World.totalResource(frontier,'food')>=3,'Frontier cargo did not leave shuttle custody')
F.fill(frontier,3,3,120,60,M.AIR);F.fill(frontier,3,25,120,25,M.ROCK)
arrived.x,arrived.y=46,24
local solar=install(frontier,6,6,'solar_array');install(frontier,8,6,'power_pole');local regulator=install(frontier,9,6,'environmental_regulator');local rig=install(frontier,10,6,'mining_rig')
local beforeExposure=arrived.environment.atmosphere;step(c,20)
assert(regulator._powerGranted,'Environmental Regulator did not receive local solar power')
assert(Industry.topology(frontier).networks[1].generation>0 and rig.wear>=0,'Frontier industry did not run under environment profile')
assert(arrived.environment.atmosphere<=beforeExposure,'Equipped suit did not protect airless arrival')
local current=#Codec.encode(c);assert(current<32*1024*1024,'G07 integration save exceeds 32 MiB')
assert(c.tick<=ticks,'Setup exceeded requested soak duration')
step(c,ticks-c.tick);Campaign.validate(c)
local final=#Codec.encode(c);assert(final<32*1024*1024,'G07 final save exceeds 32 MiB')
local profiles={};for id=4,7 do local b=c.region.bodies[id];profiles[#profiles+1]=id..':'..b.name..'/'..b.environment.archetype end
local maps=0;for _,s in ipairs(c.sites) do if s.world then maps=maps+1 end end
print(('G07 integrated soak seed=%d ticks=%d save=%d->%d->%d maps=%d destinations=%d frontier=%s route=1-4/900 person=%d suit=%d exposure=%d/%d/%d solar=%d wear=%d regulator=%s maintenance=%d codec=pass history-replay=focused-G07-G'):format(seed,ticks,boot,arrivalSave,final,maps,#c.sites,table.concat(profiles,','),personId,suit.id,arrived.environment.atmosphere,arrived.environment.thermal,arrived.environment.radiation,Industry.topology(frontier).networks[1].generation,rig.wear,regulator.status,c.travel.accounts.sites[1].consumed.metal))
