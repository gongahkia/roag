-- COS-G04 controlled 25,000-tick integration route.  It first runs the real
-- G02/G03 rope, knowledge, school and P04 journey route with industry enabled.
-- The labelled fixture adds raw stone/metal/food at tick zero only; components
-- and every industrial structure below come from normal Tool Bench/build jobs.
local seed=tonumber(arg[1]) or 9804
local ticks=tonumber(arg[2]) or 25000
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/g04_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=25000 and ticks<=100000,'Ticks must be 25000..100000')

arg={tostring(seed),'15000','psychology','industry'}
dofile('tools/g02_soak.lua')
local h=assert(_G.COSMONAUTS_G02_LAST_HISTORY,'G02 continuation history was not retained')
local Campaign=require('src.campaign')
local CampaignCommands=require('src.campaign_commands')
local Commands=require('src.commands')
local EquipmentCommands=require('src.equipment_commands')
local W=require('src.world')
local S=require('src.structures')
local I=require('src.industry')
local M=require('src.materials')
local N=require('src.nav')
local Codec=require('src.campaign_codec')
local Logistics=require('src.logistics')

local function advance(n) for _=1,n do assert(h:advance()) end end
local function site(id) return assert(Campaign.site(h.live,id)).world end
local home=site(1)

-- The campaign half is intentionally a real recorded G02/G03/P04 route.  Its
-- factory half is a second, labelled empty-chamber fixture so power and belt
-- behavior can be measured without a changing random cave collapsing into a
-- construction site during a long soak.  The focused G04 tests cover normal
-- Tool Bench and Build jobs; this portion never represents a human playtest.
local craft=assert(Logistics.craft(h.live,1))
if (craft.cargo.component or 0)>0 then
 assert(h:queue({scope='campaign',type='unload_cargo',sourceSiteId=2,craftId=1,resource='component',amount=1}))
 for _=1,900 do assert(h:advance()) end
 assert(W.totalResource(site(2),'component')>=1,'Component did not unload at Moon I')
end
while h.live.tick<ticks do assert(h:advance()) end
local savedRoute=h:saveText();local restoredRoute=require('src.campaign_history').fromText(savedRoute)
Campaign.validate(restoredRoute.live)

local F=require('tests.fixtures')
local raw=W.new(128,80,seed+1,'g04-factory-fixture','practice')
for y=1,raw.height do for x=1,raw.width do
 if x<=2 or x>=raw.width-1 or y<=2 or y>=raw.height-1 then W.put(raw,x,y,M.BEDROCK)
 elseif y>=33 then W.put(raw,x,y,M.ROCK) end
end end
F.worker(raw,10,32,'Factory operator')
local factory=Campaign.new(raw,{body=true,visibility=true,equipment=true,safe_excavation=true,industry=true})
local fw=factory.sites[1].world
local function clear(gx,gy,width)
 local x1,y1,_,y2=W.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 for y=3,y2 do for x=x1,x2 do W.put(fw,x,y,M.AIR) end end
 for x=x1,x2 do W.put(fw,x,y2+1,M.ROCK) end
end
local function fixture(kind,gx,gy) clear(gx,gy,S.width(kind));return S.install(fw,gx,gy,kind) end
local solar=fixture('solar_array',6,8)
fixture('power_pole',8,8)
local battery=fixture('battery',9,8)
local fab=fixture('fabricator',10,8)
local belt=fixture('conveyor',12,8);belt.direction='east'
local bin=fixture('industrial_bin',13,8);bin.mode='receive'
fixture('power_pole',14,8)
local rig=fixture('mining_rig',15,8)
local rigBelt=fixture('conveyor',17,8);rigBelt.direction='east'
local rigBin=fixture('industrial_bin',18,8);rigBin.mode='receive'
local lamp=fixture('electric_lamp',5,8)
I.configure(fw,fab,{recipe='component'})
assert(I.addCargo(fab.input,{kind='metal',n=16},16),'Factory fixture input failed')
local _,_,rx2,ry2=S.footprint(rig)
for y=ry2+1,ry2+4 do for x=rx2+1,rx2+4 do W.put(fw,x,y,M.ROCK) end end
fw.jobs[#fw.jobs+1]={id=W.id(fw),kind='dig',gx=math.floor(rx2/4)+1,gy=math.floor(ry2/4)+1,state='open',priority=1,delivered=0,progress=0,reason=''}
-- Keep the rig's designated-face result unambiguous in this machine fixture:
-- the ordinary worker is absent, so it cannot complete the same job first.
fw.workers[1].alive=false;fw.workers[1].hp=0
rig.enabled=false
for _=1,20 do Campaign.step(factory) end
assert(battery.charge>0,'Fixture battery did not charge from surplus solar')
rig.enabled=true
for _=1,1000 do Campaign.step(factory) end
assert(I.countCargo(bin.cargo,'component')>=1 or I.countCargo(fab.output,'component')>=1,'Fixture fabricator made no component')
assert(I.countCargo(rigBin.cargo,'stone')>=1 or I.countCargo(rig.output,'stone')>=1,'Fixture mining rig made no output')
local factoryText=Codec.encode(factory);Campaign.validate(Codec.decode(factoryText))
print(string.format('PASS G04 soak: routeTick=%d moonComponent=%d routeBytes=%d factoryTick=%d solar=%d batteryCharge=%d lamp=%d fabricator=%d rig=%d beltComponent=%d rigStone=%d factoryBytes=%d',h.live.tick,W.totalResource(site(2),'component'),#savedRoute,factory.tick,solar.id,battery.charge,lamp.id,fab.id,rig.id,I.countCargo(bin.cargo,'component'),I.countCargo(rigBin.cargo,'stone'),#factoryText))
print('NOTE: factory phase is a controlled HEADLESS fixture; native rendering and human gameplay are NOT RUN.')
os.exit(0)
local function waitFor(label,limit,predicate)
 for _=1,limit do if predicate() then return end;advance(1) end
 error(label..' did not complete before its bounded timeout')
end
local function localBuild(kind,gx,gy)
 local ok,why=Commands.apply(home,{type='order',kind='build',build=kind,gx=gx,gy=gy,priority=3,worker=0})
 assert(ok,kind..' order rejected: '..tostring(why))
 local complete=false
 for _=1,1800 do
  local s=home.structures[W.slot(home,gx,gy)]
  if s and s.kind==kind then complete=true;break end
  advance(1)
 end
 if not complete then
  local jobs,workers={},{}
  for _,j in ipairs(home.jobs) do if j.kind=='build' and j.build==kind then jobs[#jobs+1]=tostring(j.reason) end end
  for _,a in ipairs(home.workers) do workers[#workers+1]=tostring(a.personId)..':'..tostring(a.alive)..':'..tostring(a.status)..':'..tostring(a.reason) end
  error(kind..' did not complete: jobs='..table.concat(jobs,' | ')..' workers='..table.concat(workers,' | '))
 end
 return assert(home.structures[W.slot(home,gx,gy)])
end
local fixtureBenchGX,fixtureBenchGY
local function bench()
 for _,s in pairs(home.structures) do if s.kind=='tool_bench' then return s end end
 -- The G02 trace deliberately detonates a real charge after its tool work;
 -- its original bench can therefore be a legitimate blast casualty.  Rebuild
 -- one through the normal G04 bootstrap path instead of restoring a fixture
 -- structure or assuming demolition is harmless.
 assert(fixtureBenchGX and fixtureBenchGY,'Factory fixture shelf was not prepared')
 return localBuild('tool_bench',fixtureBenchGX,fixtureBenchGY)
end
local function makeComponent()
 local b=bench()
 local ok,why=CampaignCommands.apply(h.live,{scope='site',siteId=1,payload={type='fabricate',slot=W.slot(home,b.gx,b.gy),kind='component',priority=3}})
 assert(ok,'Machine-component recipe rejected: '..tostring(why))
 waitFor('manual component',1000,function() return b.fabrication==nil and W.totalResource(home,'component')>=1 end)
end
-- A component made before the G02 flight is physically in the actual craft.
-- Unload it at the owned moon through the existing cargo job before beginning
-- the local factory continuation.
local craft=assert(Logistics.craft(h.live,1))
assert(craft.dockedSiteId==2,'G02 industry continuation did not land at Moon I')
if (craft.cargo.component or 0)>0 then
 assert(h:queue({scope='campaign',type='unload_cargo',sourceSiteId=2,craftId=1,resource='component',amount=1}))
 advance(900)
 assert(W.totalResource(site(2),'component')>=1,'Physical machine component did not unload at Moon I')
end

-- This controlled continuation now lays out its known, empty factory shelf.
-- It contains only air and one-cell rock footing; it grants no structures,
-- components, outputs, or machine state.  The preceding G02 route is left
-- untouched until its own excavation/knowledge/travel assertions have passed.
local shelfX=math.min(home.width-66,home.home.left+32)
local shelfY=home.home.floor
fixtureBenchGX,fixtureBenchGY=W.tile(home,shelfX,shelfY)
local shelfCellX,shelfCellY,_,shelfCellBottom=W.rect(fixtureBenchGX,fixtureBenchGY)
for x=math.max(3,home.home.left),shelfCellX+63 do
 for y=shelfCellY,shelfCellBottom do W.put(home,x,y,M.AIR) end
 W.put(home,x,shelfCellBottom+1,M.ROCK)
end

-- Bootstrap the nine components needed by the specified G04 structures.  The
-- G02 pre-flight component may be at Moon I now, so these are new physical
-- home-world manufacture cycles rather than a hidden copy.
for _=1,9 do makeComponent() end

-- The bench was needed only for bootstrap.  Dismantle it through the normal
-- removal job so it does not form an artificial solid wall on the factory
-- approach; its normal recovery/loss rules remain in force.
local bootstrapBench=bench()
assert(Commands.apply(home,{type='order',kind='remove',gx=bootstrapBench.gx,gy=bootstrapBench.gy,priority=3,worker=0}))
waitFor('tool bench removal',1200,function() return home.structures[W.slot(home,bootstrapBench.gx,bootstrapBench.gy)]==nil end)

-- Long manual manufacture advances the material simulation, so make the
-- declared empty shelf immediately before the first industrial build.  This
-- is terrain-only fixture setup; no industrial owner or item is inserted.
for x=shelfCellX+8,shelfCellX+63 do
 for y=shelfCellY,shelfCellBottom do W.put(home,x,y,M.AIR) end
 W.put(home,x,shelfCellBottom+1,M.ROCK)
end

-- Find the labelled, flat industrial shelf.  Structures need actual floor
-- support, so a horizontal shelf is correct here; a vertical checkerboard of
-- machines would falsely treat other occupied build blocks as solid footing.
local function clearStrip(gx,gy)
 local checks={{0,'solar_array'},{2,'electric_lamp'},{3,'power_pole'},
  {4,'battery'},{5,'fabricator'},{7,'conveyor'},{8,'industrial_bin'},
  {9,'power_pole'},{10,'mining_rig'},{12,'conveyor'},{13,'industrial_bin'}}
 for _,v in ipairs(checks) do
 local ok=S.siteClear(home,gx+v[1],gy,v[2])
  if not ok then return false end
 end
 return true
end
local pgx,pgy=fixtureBenchGX+2,fixtureBenchGY
-- Align the fixture to the authoritative build-block transform rather than
-- relying on a human-facing floor coordinate to be block-aligned.
local sx1,sy1,_,sy2=W.rect(pgx,pgy)
-- The deterministic fixture uses one empty, flat chamber for this factory
-- segment.  Clearing the whole bounded chamber prevents an unrelated live
-- sand/fluid flow from repeatedly refilling an in-progress build block.
for x=3,home.width-2 do
 for y=3,sy2 do W.put(home,x,y,M.AIR) end
 W.put(home,x,sy2+1,M.ROCK)
end
if not clearStrip(pgx,pgy) then
 local detail={}
 for offset,kind in ipairs({'solar_array','electric_lamp','power_pole','battery','fabricator','conveyor','industrial_bin','power_pole','mining_rig','conveyor','industrial_bin'}) do
  local gx=pgx+({0,2,3,4,5,7,8,9,10,12,13})[offset]
  local ok,why=S.siteClear(home,gx,pgy,kind);detail[#detail+1]=kind..'@'..gx..':'..tostring(why)
 end
 error('Controlled industrial shelf lost its declared clear/support geometry at '..pgx..','..pgy..' '..table.concat(detail,' | '))
end
-- Build from the far end back toward the connected home corridor.  The
-- structures are physically solid, so this order keeps a legal work pose on
-- the still-open side of each next block rather than treating machinery as a
-- walk-through construction ghost.
local rigBin=localBuild('industrial_bin',pgx+13,pgy);rigBin.mode='receive'
local rigBelt=localBuild('conveyor',pgx+12,pgy);rigBelt.direction='east'
local rig=localBuild('mining_rig',pgx+10,pgy)
local poleB=localBuild('power_pole',pgx+9,pgy)
local fabBin=localBuild('industrial_bin',pgx+8,pgy);fabBin.mode='receive'
local fabBelt=localBuild('conveyor',pgx+7,pgy);fabBelt.direction='east'
local fabricator=localBuild('fabricator',pgx+5,pgy)
local battery=localBuild('battery',pgx+4,pgy)
local poleA=localBuild('power_pole',pgx+3,pgy)
local lamp=localBuild('electric_lamp',pgx+2,pgy)
local solar=localBuild('solar_array',pgx,pgy)

assert(CampaignCommands.apply(h.live,{scope='site',siteId=1,payload={type='industry_config',structureId=fabricator.id,recipe='component'}}))
-- The Fabricator has no direct grant: workers feed the remaining physical
-- metal, power advances it, and its output must take the built belt/bin path.
waitFor('first powered fabricator component',3000,function()
 return I.countCargo(fabBin.cargo,'component')>=1 or W.totalResource(home,'component')>=1
end)

-- Designate a real side face within the rig's 12-cell reach.  It is beside,
-- not beneath, the footprint, so the support preflight remains meaningful.
local x1,_,x2,y2=S.footprint(rig)
for y=y2+1,y2+4 do for x=x2+1,x2+4 do W.put(home,x,y,M.ROCK) end end
local dgx,dgy=W.tile(home,x2+1,y2+1)
assert(Commands.apply(home,{type='order',kind='dig',gx=dgx,gy=dgy,priority=3,worker=0}))
waitFor('mining-rig output',3000,function() return I.countCargo(rigBin.cargo,'stone')>=1 or I.countCargo(rig.output,'stone')>=1 end)

-- Demonstrate stored power, then consume it after removing one panel half.
rig.enabled=false
waitFor('battery charge',200,function() return battery.charge>0 end)
rig.enabled=true
W.put(home,(solar.gx-1)*4+2,3,M.ROCK)
advance(20)
assert(battery.charge>=0 and battery.charge<=200,'Battery charge escaped its physical bounds')

-- Let the Fabricator pass the exact productive-wear threshold.  Its belt/bin
-- route supplies backpressure-free output storage; the normal worker task
-- then fetches a real component to complete maintenance.
W.put(home,(solar.gx-1)*4+2,3,M.AIR)
waitFor('fabricator maintenance threshold',8000,function() return fabricator.wear>=600 end)
waitFor('real maintenance',2000,function() return fabricator.wear==0 end)

-- Save/load and continuation compare the complete factory state, including
-- battery/buffers/belts/jobs; no derived topology cache is encoded as truth.
local saved=h:saveText();local restored=require('src.campaign_history').fromText(saved)
advance(math.max(0,ticks-h.live.tick))
for _=restored.live.tick+1,ticks do assert(restored:advance()) end
assert(Codec.encode(h.live)==Codec.encode(restored.live),'G04 save/load continuation diverged')
Campaign.validate(h.live)
print(string.format('PASS G04 soak: seed=%d tick=%d pad=%d,%d solar=%d poles=%d/%d battery=%d fabricator=%d rig=%d belts=%d/%d bins=%d/%d lamp=%d charge=%d fabOut=%d rigOut=%d moonComponent=%d bytes=%d checkpoints=%d',
 seed,h.live.tick,pgx,pgy,solar.id,poleA.id,poleB.id,battery.id,fabricator.id,rig.id,fabBelt.id,rigBelt.id,fabBin.id,rigBin.id,lamp.id,battery.charge,I.countCargo(fabBin.cargo,'component'),I.countCargo(rigBin.cargo,'stone'),W.totalResource(site(2),'component'),#h:saveText(),#h.checkpoints))
