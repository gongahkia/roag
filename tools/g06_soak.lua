-- Deterministic integrated COS-G06 scenario plus bounded continuation soak.
-- Setup fixes geography/resources only; armament, custody, policy, training,
-- raid progression, grievance, cell sabotage and long-run cleanup use the
-- ordinary campaign systems below.
local Campaign=require('src.campaign')
local Codec=require('src.campaign_codec')
local Commands=require('src.campaign_commands')
local Equipment=require('src.equipment')
local Industry=require('src.industry')
local Jobs=require('src.jobs')
local Psychology=require('src.psychology')
local Structures=require('src.structures')
local World=require('src.world')
local F=require('tests.fixtures')
local M=require('src.materials')

local seed=assert(tonumber(arg[1]),'usage: g06_soak.lua <seed> <ticks>')
local ticks=assert(tonumber(arg[2]),'usage: g06_soak.lua <seed> <ticks>')
assert(ticks>=3000 and ticks<=30000,'ticks must be 3000..30000')
local function options() return {body=true,visibility=true,equipment=true,safe_excavation=true,knowledge=true,psychology=true,industry=true,factions=true,security=true} end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y1-1,M.AIR);F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind) end
local source=F.world('practice')
F.worker(source,10,24,'Guard');F.worker(source,18,24,'Civilian')
F.fill(source,3,3,61,24,M.AIR);F.fill(source,3,25,61,25,M.ROCK)
local c=Campaign.new(source,options());local w=c.sites[1].world
local bootSave=#Codec.encode(c)
local guard,civilian=w.workers[1],w.workers[2]
local dissidentA,dissidentB
-- Static utility infrastructure is controlled fixture geography.  The two
-- security structures below are built through normal build jobs.
local fab=install(w,10,6,'fabricator');install(w,12,6,'solar_array');install(w,14,6,'power_pole');local relay=install(w,15,6,'signal_relay')
World.stack(w,'food',1000,10,24)
local milestones={built=false,manufactured=false,equipped=false,reloaded=false,trained=false,lockdown=false,scheduled=false,warned=false,arrived=false,combat=false,protest=false,cell=false,detected=false,sabotage=false}
local roundTrips=0
local function step(n) for _=1,n do Campaign.step(c) end end
local function untilState(label,limit,predicate)
 for _=1,limit do if predicate() then return true end;Campaign.step(c) end
 assert(predicate(),'Timed out waiting for '..label);return true
end
local function roundTrip(label)
 local text=Codec.encode(c);local loaded=Codec.decode(text);Campaign.validate(loaded);assert(Codec.encode(loaded)==text,'G06 codec round-trip changed '..label);roundTrips=roundTrips+1
end
local function command(payload)
 local ok,why=Commands.apply(c,{scope='site',siteId=1,payload=payload});assert(ok,why)
end
local function manufacture(kind)
 local recipe=assert(Industry.recipe(kind));Industry.configure(w,fab,{recipe=kind})
 for resource,n in pairs(recipe.input) do assert(Industry.addCargo(fab.input,{kind=resource,n=n},16),'Fabricator input fixture overflow') end
 untilState('fabricated '..kind,recipe.ticks+500,function()
  if kind=='ammunition' then return World.totalResource(w,'ammunition')>=6 end
  return #Equipment.forSite(c,1,kind,'loose')>0
 end)
end
manufacture('frontier_carbine');manufacture('protective_vest');manufacture('ammunition');milestones.manufactured=true
World.stack(w,'stone',24,10,24);World.stack(w,'metal',4,10,24)
assert(Jobs.add(w,'build',3,6,'training_target',3));assert(Jobs.add(w,'build',4,6,'barricade',3))
untilState('security structures to build',500,function() return w.structures[World.slot(w,3,6)] and w.structures[World.slot(w,4,6)] end);milestones.built=true
install(w,5,6,'torch')
local function addPerson(x,y,name)
 local person=F.worker(w,x,y,name);person.personId=c.nextPersonId;c.nextPersonId=c.nextPersonId+1
 require('src.knowledge').attach(person);person.stress=0;person.panic=false;person.lastStressTick=0;Psychology.attach(person,c.seed);require('src.security').attachPerson(c,person)
 return person
end
local carbine=assert(Equipment.forSite(c,1,'frontier_carbine','loose')[1]);local vest=assert(Equipment.forSite(c,1,'protective_vest','loose')[1])
-- Policy is submitted through the normal site command boundary.  Posts/refuge
-- are within a worker's current visual knowledge rather than a fog bypass.
command({type='security_guard',personId=guard.personId,enabled=true});command({type='security_equip',personId=guard.personId,equipmentId=carbine.id})
untilState('physical carbine equip',300,function() return Equipment.equipped(c,guard.personId,'weapon')==carbine end)
command({type='security_equip',personId=guard.personId,equipmentId=vest.id});untilState('physical vest equip',300,function() return Equipment.equipped(c,guard.personId,'armor')==vest end)
command({type='security_reload',personId=guard.personId,amount=6});untilState('physical ammunition reload',300,function() return guard.security.ammo==6 end);milestones.equipped=true;milestones.reloaded=true
command({type='security_policy',posts={{x=18,y=24}},refuge={x1=8,y1=24,x2=8,y2=24},expectedPolicyRevision=w.security.policyRevision})
command({type='security_posture',posture='alert',expectedPolicyRevision=w.security.policyRevision});untilState('Alert guard post',240,function() return guard.x==18 and guard.y==24 end)
command({type='security_posture',posture='normal',expectedPolicyRevision=w.security.policyRevision});local xp=guard.security.combatXP;untilState('dry training',120,function() return guard.security.combatXP>xp end);milestones.trained=true
command({type='security_posture',posture='lockdown',expectedPolicyRevision=w.security.policyRevision});untilState('Lockdown refuge',240,function() return civilian.x==8 and civilian.y==24 end);milestones.lockdown=true
command({type='security_posture',posture='alert',expectedPolicyRevision=w.security.policyRevision})
-- Move the warning relay to the accessible side only after the real factory
-- output has been physically fetched.  This avoids trapping its loose output
-- behind the new solar bank while keeping the later sabotage target reachable.
w.structures[World.slot(w,15,6)]=nil;w.structures[World.slot(w,10,6)]=nil;w.industry.topologyRevision=w.industry.topologyRevision+1;w._industryTopology=nil;w.navRevision=w.navRevision+1
relay=install(w,6,6,'signal_relay');install(w,7,6,'power_pole');install(w,8,6,'solar_array')

-- A valid hostile G05 fixture supplies the relation, coarse stock and known
-- relay intelligence.  The actual G06 scheduler alone commits/debits the raid.
local faction=c.factions.factions[1];faction.relations[1].tension=100;faction.relations[1].grievance=100;faction.culture.expansion=100;faction.culture.hierarchy=100;faction.stocks.food=40;faction.stocks.metal=40;c.factions.scan.known[faction.id]=true
while c.tick%1000~=999 do Campaign.step(c) end
local foodBefore,metalBefore=faction.stocks.food,faction.stocks.metal;Campaign.step(c);local raid=assert(c.security.raids[1],'Hostile faction did not schedule a raid');assert(faction.stocks.metal==metalBefore-raid.size,'Raid commitment did not debit foreign metal exactly once');milestones.scheduled=true
roundTrip('raid approach')
untilState('legitimate relay warning',600,function() return raid.warned end);milestones.warned=true
untilState('physical raid arrival',400,function() return raid.status=='active' or raid.status=='holding' end);assert(raid.status=='active','Integrated fixture unexpectedly has no valid hostile arrival pose');milestones.arrived=true;roundTrip('active raid')
local ammoBefore=guard.security.ammo;local guardHP=guard.hp
untilState('autonomous combat exchange',500,function() return guard.security.ammo<ammoBefore or guard.hp<guardHP or #c.security.lootReceipts>0 end);milestones.combat=true

-- Generate severe dissent through the normal G03 memory/event hook, never by
-- assigning grievance.  The fixed social fixture keeps the later physical
-- meeting and 120-action sabotage deterministic.
dissidentA=addPerson(4,24,'Dissident one');dissidentB=addPerson(6,24,'Dissident two')
for _,person in ipairs({dissidentA,dissidentB}) do
 person.psychology.facets.independence=70
 for i=1,15 do Psychology.memory(c,person,'nearly_starved',{source='g06-soak-hunger:'..person.personId..':'..i,stress=4,siteId=1}) end
 assert(person.security.grievance>=85 and person.stress>=60,'Event-hook grievance fixture did not reach severe threshold')
end
Psychology.relation(dissidentA,dissidentB.personId,true).trust=30;Psychology.relation(dissidentB,dissidentA.personId,true).trust=30
while c.tick%1000~=999 do Campaign.step(c) end;Campaign.step(c);untilState('nonviolent protest',20,function() return w.security.protest~=nil end);milestones.protest=true
untilState('protest conclusion',300,function() return w.security.protest==nil end)
while c.tick%1000~=990 do Campaign.step(c) end
for _,person in ipairs({dissidentA,dissidentB}) do
 Psychology.memory(c,person,'nearly_starved',{source='g06-soak-cell-pressure:'..person.personId,siteId=1})
 Psychology.changeStress(c,person,100,c.tick)
end
untilState('post-protest cell formation',30,function() return w.security.cell and w.security.cell.state=='organizing' end);milestones.cell=true
for _,person in ipairs({dissidentA,dissidentB}) do person.stress=40;person.panic=false end
untilState('physical detected meeting',160,function() return w.security.cell.detected==true end);milestones.detected=true;roundTrip('organizing detected cell')
untilState('physical internal sabotage and unchanged-person insurgency',1300,function() return w.security.cell and w.security.cell.state=='hostile' end);milestones.sabotage=true;roundTrip('post-sabotage insurgency')
assert(dissidentA.security.allegiance=='insurgent' and dissidentB.security.allegiance=='insurgent','Cell sabotage did not retain its colonists as insurgents')

local integrationSave=#Codec.encode(c);local remaining=ticks-c.tick;assert(remaining>=0,'Integrated setup exceeded requested soak length')
step(remaining);Campaign.validate(c);local encoded=Codec.encode(c);assert(#encoded<32*1024*1024,'save bound exceeded')
for key,value in pairs(milestones) do assert(value,'Integrated G06 milestone missing: '..key) end
local raids,resolved=0,0;for _,r in ipairs(c.security.raids) do raids=raids+1;if r.status=='resolved' then resolved=resolved+1 end end
local cell=w.security.cell;local members=cell and table.concat(cell.members,',') or 'none'
print(('G06 integrated soak seed=%d ticks=%d save=%d->%d->%d roundTrips=%d faction=%d raid=%d/%s foreignFood=%d->%d foreignMetal=%d->%d loot=%d guard=%d ammo=%d xp=%d insurgents=%s cell=%s events=%d raids=%d resolved=%d'):format(seed,ticks,bootSave,integrationSave,#encoded,roundTrips,faction.id,raid.id,raid.status,foodBefore,faction.stocks.food,metalBefore,faction.stocks.metal,#c.security.lootReceipts,guard.personId,guard.security.ammo,guard.security.combatXP,members,cell and cell.state or 'none',#w.security.events,raids,resolved))
