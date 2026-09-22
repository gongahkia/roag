-- COS-G07 focused deterministic environment/frontier contracts.  These are
-- compact integration fixtures; the long multi-site route is exercised by the
-- companion soak rather than replacing the normal command paths here.
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Codec=require('src.campaign_codec')
local Equipment=require('src.equipment')
local Industry=require('src.industry')
local Structures=require('src.structures')
local Visibility=require('src.visibility')
local Security=require('src.security')
local Environments=require('src.environments')
local Logistics=require('src.logistics')
local Jobs=require('src.jobs')
local Factions=require('src.factions')
local World=require('src.world')
local M=require('src.materials')
local Generator=require('src.generation.frontier')
local F=require('tests.fixtures')
local T={}

local function options()
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
  logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true}
end
local function campaign(seed) return Campaign.newRegion(seed or 10701,options()) end
local function advance(history,n) for _=1,n do assert(history:advance()) end end
local function group(r,name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y1-1,M.AIR);F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind) end
local function industrialStep(c,n)
 local w=c.sites[1].world
 for _=1,n do c.tick=c.tick+1;w.tick=c.tick;Industry.step(w,{campaign=c,siteId=1}) end
end
local function setTick(c,tick)
 c.tick=tick
 for _,record in ipairs(c.sites) do if record.world then record.world.tick=tick end end
end
local function safetyFloor(w)
 F.fill(w,3,3,100,24,M.AIR);F.fill(w,3,25,100,25,M.ROCK)
end

function T.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 group(r,'G07-A feature gate retains exact legacy region and routes',function()
  local old=Campaign.newRegion(10702,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true})
  eq(#old.sites,3);check(old.features.environments==nil and old.sites[1].world.environment==nil,'Legacy campaign gained environment state')
  eq(require('src.travel').duration(old,1,2),400);eq(require('src.travel').duration(old,1,3),600);eq(require('src.travel').duration(old,2,3),800)
  local c=campaign();eq(#c.sites,7);eq(#c.region.bodies,7);check(c.sites[4].world==nil and not c.region.bodies[4].visited,'Frontier map was eagerly simulated')
 end)
 group(r,'G07-B deterministic bounded frontier identity, diversity, names and routes',function()
  local a,b=campaign(10703),campaign(10703);eq(Codec.encode(a.region.bodies),Codec.encode(b.region.bodies),'Frontier metadata rerolled')
  local names,low,high,harsh,thermal={},false,false,false,{}
  for id=4,7 do local body=a.region.bodies[id];check(not names[body.name],'Duplicate frontier name');names[body.name]=true;local p=body.environment;low=low or p.gravityPct<=60;high=high or p.gravityPct>110;harsh=harsh or p.atmosphere=='vacuum' or p.atmosphere=='toxic';thermal[p.thermalKind..':'..p.thermalSeverity]=true
   eq(require('src.travel').duration(a,1,id),({900,1100,1300,1500})[id-3]);check(require('src.travel').duration(a,id%2==0 and 2 or 3,id),'Frontier site lacks its bounded moon link')
  end
  check(low and high and harsh and next(thermal) and (function() local n=0;for _ in pairs(thermal) do n=n+1 end;return n>=2 end)(),'Frontier archetype diversity constraints failed')
  local first=Campaign.instantiate(a,4);local second=Campaign.instantiate(a,4);eq(first,second,'First world instantiation was not persistent');check(a.region.bodies[4].visited,'First world instantiation did not persist visited state');Campaign.validate(a)
 end)
 group(r,'G07-C gravity changes navigation bounds and deterministic fall arithmetic',function()
  eq(Environments.jumpMax({environment={gravityPct=40}}),3);eq(Environments.jumpMax({environment={gravityPct=100}}),2);eq(Environments.jumpMax({environment={gravityPct=125}}),1)
  eq(Environments.fallDamage(15,{environment={gravityPct=55}}),9);eq(Environments.fallDamage(15,{environment={gravityPct=125}}),19)
 end)
 group(r,'G07-D physical Frontier Suit and bounded exposure/recovery',function()
  local c=campaign();local w=c.sites[1].world;local a=w.workers[1];w.environment.atmosphere='vacuum';w.environment.thermalSeverity=2;w.environment.radiationSeverity=2
  c.tick=10;w.tick=10;Environments.step(c,c.sites[1]);check(a.environment.atmosphere==5 and a.environment.thermal==2 and a.environment.radiation==2,'Unsuited exposure did not use current environment')
  local suit=Equipment.create(c,1,'frontier_suit',a.x,a.y);Equipment.equip(c,suit,a,'environment');eq(suit.state,'equipped');eq(Equipment.equipped(c,a.personId,'armor'),nil,'Environmental slot replaced combat armor')
  c.tick=20;w.tick=20;Environments.step(c,c.sites[1]);eq(a.environment.atmosphere,3);eq(a.environment.thermal,3);eq(a.environment.radiation,3)
  a.environment.atmosphere=100;c.tick=30;w.tick=30;local hp=a.hp;Environments.step(c,c.sites[1]);eq(a.hp,hp-1,'Catastrophic exposure did not use normal health')
  Equipment.drop(c,suit,1,a.x,a.y);check(suit.state=='loose' and suit.personId==nil and suit.siteId==1,'Suit drop duplicated custody')
 end)
 group(r,'G07-E powered regulator, vacuum torch, solar and wear are mechanical',function()
  local c=campaign();local w=c.sites[1].world;w.environment.atmosphere='vacuum';w.environment.thermalSeverity=2;w.environment.radiationSeverity=2;w.environment.solarPct=120;w.environment.machineWearPct=130
  local solar=install(w,6,6,'solar_array');install(w,8,6,'power_pole');local regulator=install(w,9,6,'environmental_regulator');local fab=install(w,10,6,'fabricator');Industry.configure(w,fab,{recipe='component'});assert(Industry.addCargo(fab.input,{kind='metal',n=2},16));industrialStep(c,10)
  check(regulator._powerGranted,'Powered regulator was not allocated actual solar electricity');check(Industry.topology(w).networks[1].generation>=7,'Solar percentage did not change physical generation')
  check(fab.wear>=13,'Machine wear percentage did not accumulate deterministically')
  local gx,gy=15,6;clear(w,gx,gy,1);local torch=Structures.install(w,gx,gy,'torch');check(torch);check(not Environments.torchWorks(w),'Vacuum preserved flame torch illumination')
  local a=w.workers[1];a.x,a.y=regulator.gx*4-2,regulator.gy*4-2;c.tick=20;w.tick=20;Environments.step(c,c.sites[1]);eq(a.environment.thermal,0);eq(a.environment.radiation,0)
  eq(a.environment.atmosphere,5,'A powered regulator incorrectly made vacuum breathable')
  regulator._powerGranted=false;c.tick=30;w.tick=30;Environments.step(c,c.sites[1]);check(a.environment.thermal>0 and a.environment.radiation>0,'Power loss left stale regulator protection');eq(a.environment.atmosphere,10,'Power loss changed atmospheric exposure instead of only ending live thermal/radiation cover')
  check(Visibility.derive(w,{campaign=c,siteId=1}),'Visibility derivation failed with a vacuum torch')
  check(solar.solarRemainder~=nil and fab.wearRemainder~=nil,'Environmental fixed-point remainders were not retained')
 end)
 group(r,'G07-F environment-biased terrain is real and lazy generation is replay-stable',function()
  local c=campaign(10706);local a=Campaign.instantiate(c,4);local b=Campaign.instantiate(c,5)
  local function signature(w) local ore,water,sand=0,0,0;for i=1,w.n do local m=w.mat[i];ore=ore+(m==M.ORE and 1 or 0);water=water+(m==M.WATER and 1 or 0);sand=sand+(m==M.SAND and 1 or 0) end;return ore..':'..water..':'..sand end
  check(signature(a)~=signature(b),'Archetype maps differed only in metadata')
  local function generated(environment)
   return Generator.makeUnpopulated(77731,'practice',128,80,{layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,environment=environment})
  end
  local airless,garden=generated('airless_crag'),generated('heavy_garden')
  local function ecologySignature(w) return #w.content.flora..':'..#w.content.fauna end
  check(signature(airless)~=signature(garden),'Environment terrain/resource generation did not change')
  check(ecologySignature(airless)~=ecologySignature(garden),'Environment ecology weighting did not change')
  local saved=Codec.encode(c);local restored=Codec.decode(saved);Campaign.validate(restored);eq(Codec.encode(restored.sites[4].world),Codec.encode(a),'Instantiated frontier map changed on save/load')
 end)
 group(r,'G07-G actual long-route founding preserves people, equipment, cargo and replay',function()
  local base=campaign(10707);local worker=base.sites[1].world.workers[1];local suit=Equipment.create(base,1,'frontier_suit',worker.x,worker.y);Equipment.equip(base,suit,worker,'environment');local personId=worker.personId
  local h=History.new(base);local c=h.live
  assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=4,passengers={personId},cargo={food=3,metal=2}}));advance(h,460)
  local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(h,180);manifest=assert(Logistics.manifest(h.live,1));assert(Logistics.readiness(h.live,manifest))
  assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(h,901)
  local destination=h.live.sites[4];check(destination.ownerSocietyId==1 and destination.world,'Long route did not instantiate/found frontier site');local arrived=destination.world.workers[1];eq(arrived.personId,personId);eq(Equipment.equipped(h.live,personId,'environment').id,suit.id,'Travel duplicated or lost equipped suit');eq(h.live.logistics.crafts[1].dockedSiteId,4);check((h.live.travel.accounts.sites[1].consumed.metal or 0)==1,'Departure maintenance metal was not consumed exactly once')
  local security=destination.world.security;assert(h:queue({scope='site',siteId=4,payload={type='security_posture',posture='alert',expectedPolicyRevision=security.policyRevision}}));assert(h:queue({scope='site',siteId=4,payload={type='security_guard',personId=personId,enabled=true}}));advance(h,1)
  check(destination.world.security.posture=='alert' and arrived.security.guardEnabled,'G06 guard posture did not operate on founded frontier settlement')
  local saved=h:saveText();local restored=History.fromText(saved);eq(Codec.encode(restored.live),Codec.encode(h.live),'Frontier travel save/load diverged');local ok,why=restored:verifyReplay(4000);check(ok,why);restored:seek(1);while restored.seekTarget do restored:updateSeek(80) end;check(restored.view.sites[4].world==nil and restored.view.sites[4].ownerSocietyId==nil,'Seek before arrival leaked future frontier world')
 end)
 group(r,'G07-H hazardous hostile expeditions pay normal cost plus suits and retain physical protection',function()
  local c=campaign(10708);local target=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=1
  -- This is a narrow hostile-G05 fixture. The two real target structures make
  -- the already-instantiated airless outpost strategically preferred over
  -- Home; scheduler, normal base cost, surplus suit debit, and physical
  -- materialization all remain production paths.
  install(target,6,6,'solar_array');install(target,9,6,'fabricator')
  check(c.region.bodies[4].environment.atmosphere=='vacuum','G07 fixture lost the airless frontier profile')
  local f=c.factions.factions[1];f.relations[1].tension=100;f.relations[1].grievance=100;f.culture.expansion=100;f.culture.hierarchy=100;f.stocks.food=40;f.stocks.metal=40
  c.tick=999;for _,record in ipairs(c.sites) do if record.world then record.world.tick=999 end end
  -- Faction background stock changes at this boundary too.  A canonical
  -- otherwise-identical no-security continuation isolates the raid debit from
  -- that established G05 production, rather than assuming background gain.
  local control=Codec.decode(Codec.encode(c));control.features.security=nil
  Campaign.step(control);Campaign.step(c)
  local raid=assert(c.security.raids[1],'Hostile G05 faction did not schedule');local cf=control.factions.factions[1];eq(raid.siteId,4,'Hostile raid did not target the owned frontier outpost')
  eq(cf.stocks.food-f.stocks.food,raid.size,'Normal food raid cost was lost');eq(cf.stocks.metal-f.stocks.metal,raid.size+raid.size,'Hazard suit surcharge was not a bounded extra metal debit');eq(raid.suitCost,raid.size)
  for _=1,800 do Campaign.step(c) end
  check(raid.status=='active' or raid.status=='holding','Hazardous raid did not retain ordinary arrival semantics')
  if raid.status=='active' then for _,actor in ipairs(raid.actors) do check(actor.suit and actor.suit.kind=='frontier_suit' and actor.environment,'Hazardous raider was magically unprotected') end end
 Campaign.validate(c)
end)
 group(r,'G07-I autonomous environmental safety yields work and security posts without conjured custody',function()
  -- An ordinary build task has begun before vacuum danger is sampled.  The
  -- following worker update releases that task, walks one normal navigation
  -- step at a time to a real loose suit, equips it only within ordinary reach,
  -- then recovers on the live exposure cadence.
  local c=campaign(10709);local w=c.sites[1].world;local a=w.workers[1];safetyFloor(w);for i=2,#w.workers do w.workers[i].x,w.workers[i].y=90,24 end
  a.x,a.y=10,24;w.environment.atmosphere='vacuum';w.environment.thermalSeverity=0;w.environment.radiationSeverity=0;a.environment.atmosphere=69
  World.stack(w,'stone',2,a.x,a.y);Jobs.add(w,'build',4,6,'platform',3);local suit=Equipment.create(c,1,'frontier_suit',14,24)
  setTick(c,9);Jobs.plan(w,a,{campaign=c,siteId=1});check(a.task and a.task.kind=='work','Safety fixture did not begin ordinary discretionary work')
  Campaign.step(c);check(a.environment.atmosphere>=70 and a.task and a.task.kind=='work','Danger did not arise while ordinary work was active')
  local beforeX=a.x;Campaign.step(c);check(a.task==nil,'Dangerous exposure left ordinary AUTO work assigned');eq(a.x,beforeX+1,'Safety response teleported instead of taking one normal navigation step');check(suit.state=='loose' and suit.personId==nil,'Safety response conjured or remotely equipped a suit')
  for _=1,5 do Campaign.step(c) end
  eq(suit.state,'equipped');eq(suit.personId,a.personId,'Safety response broke unique Frontier Suit custody');check(a.x>=10 and a.x<=14,'Suit acquisition left normal local movement bounds')
  while c.tick<40 do Campaign.step(c) end;check(a.environment.atmosphere<70 and not a.environment.danger,'Protected worker did not recover after reaching legitimate protection')

  -- The same survival need outranks a live Alert defense-post directive but
  -- does not change global security policy.  A powered regulator is reached
  -- through ordinary local movement and becomes live cover only in range.
  local g=campaign(10710);local gw=g.sites[1].world;local guard=gw.workers[1];safetyFloor(gw);for i=2,#gw.workers do gw.workers[i].x,gw.workers[i].y=90,24 end
  guard.x,guard.y=60,24;gw.environment.atmosphere='breathable';gw.environment.thermalSeverity=2;gw.environment.radiationSeverity=0
  install(gw,2,6,'solar_array');install(gw,5,6,'power_pole');local regulator=install(gw,8,6,'environmental_regulator')
  guard.security.guardEnabled=true;Security.setPosts(gw,{{x=60,y=24}},nil,gw.security.policyRevision);Security.configure(gw,{posture='alert'},gw.security.policyRevision);guard.environment.thermal=69
  setTick(g,9);Jobs.plan(gw,guard,{campaign=g,siteId=1});check(guard.task and guard.task.securityMode=='post','Alert did not create the ordinary defense-post directive')
  Campaign.step(g);check(regulator._powerGranted and guard.environment.danger,'Powered-regulator guard fixture did not reach environmental danger')
  local postX=guard.x;Campaign.step(g);check(guard.x<postX and guard.status=='Seeking Environmental Regulator','Environmental survival did not preempt Alert post movement')
  while g.tick<20 do Campaign.step(g) end
  check(guard.environment.thermal<70 and not guard.environment.danger,'Guard did not recover inside actual powered regulator coverage');eq(gw.security.posture,'alert','Environmental safety disabled security policy rather than preempting one directive')
 end)
 group(r,'G07-J protestors and insurgents retain live environmental identity',function()
  -- Protest is an actual G06 task before exposure takes precedence; the
  -- protest state remains public/nonviolent while the person seeks a real
  -- loose suit rather than receiving environmental immunity.
  local c=campaign(10711);local w=c.sites[1].world;local protestor=w.workers[1];safetyFloor(w);for i=2,#w.workers do w.workers[i].x,w.workers[i].y=90,24 end
  protestor.x,protestor.y=10,24;w.environment.atmosphere='vacuum';w.environment.thermalSeverity=0;w.environment.radiationSeverity=0;w.security.protest={members={protestor.personId},untilTick=300,meeting={x=30,y=24}}
  local protestSuit=Equipment.create(c,1,'frontier_suit',14,24);protestor.environment.atmosphere=69;setTick(c,9)
  Jobs.plan(w,protestor,{campaign=c,siteId=1});check(protestor.task and protestor.task.securityMode=='protest','Protest fixture did not use the normal G06 protest task')
  Campaign.step(c);check(protestor.environment.atmosphere>=70,'Protestor was incorrectly environmentally immune')
  Campaign.step(c);check(protestor.status=='Seeking Frontier Suit' and protestSuit.state=='loose','Protestor did not yield to actual survival movement')
  for _=1,5 do Campaign.step(c) end;eq(protestSuit.personId,protestor.personId,'Protestor suit custody was not physical')

  -- This calls the production physical 120-action internal-sabotage path.
  -- It isolates the transition itself (formation thresholds are G06-covered)
  -- while proving that a G07 environment record and equipped suit survive the
  -- same-person change of allegiance.
  local d=campaign(10712);local dw=d.sites[1].world;local a,b=dw.workers[1],dw.workers[2];safetyFloor(dw);local target=install(dw,6,6,'training_target')
  local suit=Equipment.create(d,1,'frontier_suit',a.x,a.y);Equipment.equip(d,suit,a,'environment');a.environment.atmosphere=73;a.environment.thermal=4;a.environment.radiation=2;a.environment.danger=true
  local retained={personId=a.personId,object=a,environment=Codec.encode(a.environment),suit=suit.id,hp=a.hp,facets=Codec.encode(a.psychology.facets),knowledge=Codec.encode(a.frontier),xp=a.security.combatXP}
  a.x,a.y=18,24;b.x,b.y=30,24;local cellId=d.security.nextCellId;d.security.nextCellId=cellId+1;dw.security.cell={id=cellId,members={a.personId,b.personId},state='sabotage',cause='hunger',targetId=target.id,saboteurId=a.personId,progress=0}
  setTick(d,100);local task={securityMode='sabotage',cellId=cellId,targetId=target.id};for _=1,120 do local ok=Security.act(d,dw,a,task,{campaign=d,siteId=1});check(ok,'Physical insurgent sabotage could not advance while in reach') end
  eq(dw.security.cell.state,'hostile');check(a==retained.object,'Insurgent conversion replaced the live worker object');eq(a.personId,retained.personId);eq(Codec.encode(a.environment),retained.environment,'Insurgent conversion cleared or altered live exposure');eq(Equipment.equipped(d,a.personId,'environment').id,retained.suit,'Insurgent conversion duplicated or lost Frontier Suit');eq(a.hp,retained.hp);eq(Codec.encode(a.psychology.facets),retained.facets);eq(Codec.encode(a.frontier),retained.knowledge);eq(a.security.combatXP,retained.xp)
 end)
 group(r,'G07-K frontier cargo and non-combat G05 contact use generalized owned-site paths',function()
  -- This is the established physical inter-settlement mechanism: a real G04
  -- Machine Component is loaded into the normal craft manifest, becomes
  -- transit cargo, then is unloaded by a destination-local worker.  There is
  -- intentionally no second global shipment ledger for G07 destinations.
  local base=campaign(10713);local home=base.sites[1].world;local traveller=home.workers[1];local suit=Equipment.create(base,1,'frontier_suit',traveller.x,traveller.y);Equipment.equip(base,suit,traveller,'environment')
  World.stack(home,'component',1,traveller.x,traveller.y);local homeBefore=World.totalResource(home,'component');local personId=traveller.personId;local h=History.new(base)
  assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=4,passengers={personId},cargo={food=3,metal=2,component=1}}));advance(h,560)
  local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(h,220);manifest=assert(Logistics.manifest(h.live,1));assert(Logistics.readiness(h.live,manifest),'Frontier component manifest did not become physically ready')
  local craft=h.live.logistics.crafts[1];eq(craft.cargo.component,1,'Machine Component did not leave home into craft custody');eq(World.totalResource(h.live.sites[1].world,'component'),homeBefore-1,'Machine Component remained at source after physical load')
  assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(h,1);check(craft.journey and craft.cargo.component==1,'Loaded component was not uniquely in transit')
  advance(h,900);local destination=assert(h.live.sites[4]);local outpost=assert(destination.world);eq(destination.ownerSocietyId,1);eq(craft.dockedSiteId,4);eq(craft.cargo.component,1,'Arrival duplicated or lost the physical Machine Component');eq(World.totalResource(h.live.sites[1].world,'component'),homeBefore-1,'Source component returned during frontier transit')
  local destinationBefore=World.totalResource(outpost,'component');assert(h:queue({scope='campaign',type='unload_cargo',sourceSiteId=4,craftId=1,resource='component',amount=1}));advance(h,220)
  eq(craft.cargo.component or 0,0,'Destination unload retained duplicate craft component');eq(World.totalResource(outpost,'component'),destinationBefore+1,'Machine Component did not physically arrive at owned frontier site');eq(World.totalResource(h.live.sites[1].world,'component')+World.totalResource(outpost,'component'),homeBefore+destinationBefore,'Frontier component custody was not conserved')

  -- The same dynamically selected outpost powers a real G05 Signal Relay,
  -- discovers a faction, and starts its ordinary representative audience via
  -- a site-bound command.  No original-three-site special case is involved.
  install(outpost,2,6,'solar_array');install(outpost,5,6,'power_pole');local relay=install(outpost,8,6,'signal_relay');advance(h,300);check(relay._powerGranted,'Frontier Signal Relay was not powered through normal G04 power')
  local factionId=h.live.factions.scan.order[1];check(h.live.factions.scan.known[factionId],'Powered frontier relay did not use G05 discovery')
  local arrived=outpost.workers[1];eq(arrived.personId,personId);assert(h:queue({scope='site',siteId=4,payload={type='faction_contact',factionId=factionId,personId=personId}}));advance(h,1)
  local faction=h.live.factions.factions[factionId-1];check(faction.contact.audience and faction.contact.audience.siteId==4,'G05 contact still selected an original-three site instead of the frontier settlement');advance(h,60)
  check(faction.contact.protocol and #faction.contact.protocol.steps==3,'Frontier representative did not complete ordinary G05 contact audience');check(Factions.familiarity(arrived,factionId).familiarity>=10,'Frontier representative did not retain personal G05 contact state')
  Campaign.validate(h.live)
 end)
 print(r.groups..' G07 groups; '..r.assertions..' assertions passed.');return r
end
return T
