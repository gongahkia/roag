-- COS-G08 focused archaeology and deep-space contracts.  The deliberately
-- compact fixtures use the same campaign/worker/industry paths as play; long
-- transit is covered again by tools/g08_soak.lua.
local Campaign=require('src.campaign')
local Commands=require('src.campaign_commands')
local Codec=require('src.campaign_codec')
local History=require('src.campaign_history')
local Relics=require('src.relics')
local Travel=require('src.travel')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')
local Structures=require('src.structures')
local Education=require('src.education')
local Labor=require('src.labor')
local World=require('src.world')
local F=require('tests.fixtures')
local M=require('src.materials')
local T={}

local function options()
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
  logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true,relics=true}
end
local function campaign(seed) return Campaign.newRegion(seed or 10801,options()) end
local function advance(h,n) for _=1,n do assert(h:advance()) end end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind) end
local function moveWorker(c,fromId,toId,index)
 local from,to=c.sites[fromId].world,c.sites[toId].world;local a=table.remove(from.workers,index or 1)
 a.id=World.id(to);a.x,a.y=to.home.x,to.home.y;to.workers[#to.workers+1]=a
 from.labor=Labor.default(from);from.laborAssignments=Labor.allocate(from);to.labor=Labor.default(to);to.laborAssignments=Labor.allocate(to)
 return a
end
local function launch(c,fromId,toId,personId,cargo)
 local source=c.sites[fromId].world;local a
 for _,worker in ipairs(source.workers) do if worker.personId==personId then a=worker end end
 assert(a,'Launch fixture lost its traveller');local craft=c.logistics.crafts[1];a.x,a.y=craft.anchor.x,craft.anchor.y
 for kind,n in pairs(cargo) do if n>0 then World.stack(source,kind,n,a.x,a.y) end end
 assert(Commands.apply(c,{scope='campaign',type='prepare_expedition',sourceSiteId=fromId,craftId=craft.id,destinationSiteId=toId,passengers={personId},cargo=cargo}))
 for _=1,700 do Campaign.step(c) end
 local manifest=assert(Logistics.manifest(c,craft.activeManifestId));assert(Commands.apply(c,{scope='campaign',type='assemble_expedition',sourceSiteId=fromId,craftId=craft.id,manifestId=manifest.id}))
 for _=1,300 do Campaign.step(c) end
 manifest=assert(Logistics.manifest(c,craft.activeManifestId));assert(Logistics.readiness(c,manifest));assert(Commands.apply(c,{scope='campaign',type='launch_expedition',sourceSiteId=fromId,craftId=craft.id,manifestId=manifest.id,expectedManifestRevision=manifest.revision}))
 for _=1,Travel.duration(c,fromId,toId)+1 do Campaign.step(c) end
 return c.sites[toId].world
end
local function group(r,name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end

function T.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 group(r,'G08-A gate, deterministic hidden systems, and bounded lazy identities',function()
  local old=Campaign.newRegion(10802,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true})
  eq(#old.sites,7);check(old.relics==nil and old.region.systems==nil,'G07 campaign acquired relic state')
  local a,b=campaign(10803),campaign(10803);eq(#a.sites,11);eq(#a.region.systems,3);eq(#a.relics.items,9);eq(#a.relics.caches,5);eq(Codec.encode(a.region),Codec.encode(b.region),'Remote system metadata rerolled')
  for id=8,11 do check(a.sites[id].world==nil and not a.region.bodies[id].visited,'Remote map was eagerly created');check(Relics.hiddenSite(a,id),'Undetected remote site leaked') end
  eq(Travel.duration(a,1,8),2400);eq(Travel.duration(a,1,10),3200);eq(Travel.duration(a,8,10),2800);eq(Travel.duration(a,8,9),700);eq(Travel.duration(a,10,11),900)
  check(not pcall(function() Campaign.newRegion(1,{logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,relics=true}) end),'relics=1 accepted without environments=1')
 end)
 group(r,'G08-B physical 180-action excavation creates unique loose relic custody',function()
  local c=campaign(10804);local w=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=1;local a=moveWorker(c,1,4,1);local q=Relics.cache(c,1)
  a.x,a.y=q.gx*4-2,q.gy*4;q.workerId=a.personId
  for _=1,180 do local ok=Relics.act(c,w,a,{mode='excavate',cacheId=q.id});check(ok,'Physical cache work failed') end
  check(q.empty and q.progress==180,'Cache did not persistently empty after exact work');local first,second=Relics.find(c,1),Relics.find(c,4)
  eq(first.state,'ground');eq(second.state,'ground');eq(first.siteId,4);eq(first.x,a.x);check(Relics.pickup(c,4,first.id,a.personId),'Reachable relic could not enter worker custody');eq(first.state,'person');eq(first.personId,a.personId)
  local craft=c.logistics.crafts[1];craft.dockedSiteId=4;craft.anchor={x=a.x,y=a.y};check(Relics.loadCraft(c,4,craft.id,first.id,a.personId),'Physical relic could not enter shuttle cargo');eq(first.state,'craft');eq(Relics.craftCount(c,craft.id),1)
  local cargoRoundTrip=Codec.decode(Codec.encode(c));Campaign.validate(cargoRoundTrip);eq(Relics.find(cargoRoundTrip,first.id).state,'craft','Relic shuttle custody did not persist through codec')
  craft.cargo.food=craft.capacity;check(not Relics.loadCraft(c,4,craft.id,first.id,a.personId),'Full shuttle accepted duplicate relic cargo');craft.cargo={};check(Relics.unloadCraft(c,4,craft.id,first.id,a.personId),'Shuttle relic cargo could not leave through a real worker');eq(first.state,'person')
  Campaign.validate(c);local bad=Codec.decode(Codec.encode(c));bad.relics.drive.installed=true;bad.relics.drive.craftId=1;bad.relics.drive.sockets={first.id,first.id,0};first=Relics.find(bad,1);first.state='drive';first.craftId=1;first.socket=1;check(not pcall(Campaign.validate,bad),'Duplicate relic drive custody escaped validation')
 end)
 group(r,'G08-C Analyzer power/work reveals personal role/signature and P06 can teach it',function()
  local c=campaign(10805);local w=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=1;w.environment.atmosphere='breathable';w.environment.thermalSeverity=0;w.environment.radiationSeverity=0;F.fill(w,3,3,100,24,M.AIR);F.fill(w,3,25,100,25,M.ROCK);local analyst=moveWorker(c,1,4,1);local learner=moveWorker(c,1,4,1);local q=Relics.cache(c,1)
  analyst.x,analyst.y=q.gx*4-2,q.gy*4;q.workerId=analyst.personId;for _=1,180 do assert(Relics.act(c,w,analyst,{mode='excavate',cacheId=1})) end
  assert(Relics.pickup(c,4,1,analyst.personId));local analyzer=install(w,6,6,'relic_analyzer');analyzer._powerGranted=true;analyst.x,analyst.y=analyzer.gx*4-2,analyzer.gy*4
  assert(Relics.loadAnalyzer(c,4,analyzer.id,1,analyst.personId));assert(Relics.beginAnalysis(c,4,analyzer.id,1,analyst.personId))
  for _=1,119 do assert(Relics.act(c,w,analyst,{mode='analysis',structureId=analyzer.id})) end
  check(not Relics.knowsRole(analyst,1),'Role revealed before 120 physical Analyzer actions');analyzer._powerGranted=false;check(not Relics.act(c,w,analyst,{mode='analysis',structureId=analyzer.id}),'Power loss did not pause analysis');analyzer._powerGranted=true
  for _=120,300 do assert(Relics.act(c,w,analyst,{mode='analysis',structureId=analyzer.id})) end
  check(Relics.knowsRole(analyst,1) and Relics.knowsSignature(analyst,1),'Analyzer did not grant personal decoded facts');check(not Relics.knowsRole(learner,1),'Analysis incorrectly made knowledge global')
  analyst.x,analyst.y=22,24;learner.x,learner.y=30,24;local school=install(w,10,6,'field_school');local h=History.new(c);assert(h:queue({scope='site',siteId=4,payload={type='school_policy',slot=World.slot(w,school.gx,school.gy),schoolId=school.education.id,expectedPolicyRevision=school.education.policyRevision,enabled=true,mode='teach',topicId=Relics.roleFact(1),topicVersion=1,priority=3}}));advance(h,1500)
  local found;for _,a in ipairs(h.live.sites[4].world.workers) do if a.personId==learner.personId then found=a end end;check(found and Knowledge.fact(found,Relics.roleFact(1)),'P06 teaching did not transmit decoded relic fact')
 end)
 group(r,'G08-D decoded lens scan reveals systems without leaking cache coordinates',function()
  local c=campaign(10806);local w=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=1;local a=moveWorker(c,1,4,1);local lens=Relics.find(c,2);local q=Relics.cache(c,2);q.empty=true;Relics.drop(c,lens,4,a.x,a.y);assert(Relics.pickup(c,4,2,a.personId))
  local analyzer=install(w,6,6,'relic_analyzer');local relay=install(w,9,6,'signal_relay');analyzer._powerGranted=true;relay._powerGranted=true;a.x,a.y=analyzer.gx*4-2,analyzer.gy*4
  local source={siteId=4,category='sites',id=2,kind='ancient_cache',definitionVersion=1};assert(Knowledge.learn({campaign=c,siteId=4},a,Relics.roleFact(2),'record',source,{}));assert(Relics.loadAnalyzer(c,4,analyzer.id,2,a.personId));relay._powerGranted=false;check(not Relics.beginScan(c,4,analyzer.id,2,a.personId),'Unpowered Signal Relay started a deep-space scan');relay._powerGranted=true;assert(Relics.beginScan(c,4,analyzer.id,2,a.personId));relay._powerGranted=false;check(not Relics.act(c,w,a,{mode='scan',structureId=analyzer.id}),'Signal Relay power loss did not pause scan work');relay._powerGranted=true
  for _=1,300 do assert(Relics.act(c,w,a,{mode='scan',structureId=analyzer.id})) end
  check(Relics.revealed(c,2) and Relics.revealed(c,3),'Decoded powered lens scan did not reveal both remote systems');check(not c.relics.caches[4].placed and not c.relics.caches[5].placed,'Deep scan leaked/instantiated remote cache coordinates');check(not Relics.hiddenSite(c,8),'Revealed system remained hidden')
 end)
 group(r,'G08-E physical frame/sockets distinguish incompatible discharge from understood stability',function()
  local c=campaign(10807);local w=c.sites[1].world;local a=w.workers[1];local d=c.relics.drive;local craft=c.logistics.crafts[1]
  a.x,a.y=craft.anchor.x,craft.anchor.y;local metal,component=World.totalResource(w,'metal'),World.totalResource(w,'component');World.stack(w,'metal',4,a.x,a.y);World.stack(w,'component',3,a.x,a.y);assert(Relics.beginFrame(c,1,craft.id,a.personId));for _=1,300 do assert(Relics.act(c,w,a,{mode='frame'})) end;check(d.installed,'Frame did not consume physical work/materials');eq(World.totalResource(w,'metal'),metal);eq(World.totalResource(w,'component'),component)
  for _,id in ipairs({1,2,3}) do local q=Relics.cache(c,id);q.empty=true;Relics.drop(c,Relics.find(c,id),1,a.x,a.y);assert(Relics.socket(c,1,craft.id,id,a.personId,id)) end
  eq(Relics.compatibility(c,craft.id,a),'unknown','Hidden signatures leaked compatible status');local ok,reason=Relics.activation(c,c.sites[1],craft,{a});check(ok and reason.unstable,'Compatible unknown trio did not allow unstable activation')
  local source={siteId=1,category='sites',id=1,kind='ancient_cache',definitionVersion=1};for _,id in ipairs({1,2,3}) do assert(Knowledge.learn({campaign=c,siteId=1},a,Relics.roleFact(id),'record',source,{}));assert(Knowledge.learn({campaign=c,siteId=1},a,Relics.signatureFact(id),'record',source,{})) end
  eq(Relics.compatibility(c,craft.id,a),'compatible');ok,reason=Relics.activation(c,c.sites[1],craft,{a});check(ok and not reason.unstable,'Knowledgeable operator did not stabilize same physical trio')
  d.sockets[3]=4;local old=Relics.find(c,3);Relics.drop(c,old,1,a.x,a.y);local decoy=Relics.find(c,4);decoy.state='drive';decoy.craftId=craft.id;decoy.socket=3;local hp=a.hp;ok=Relics.activation(c,c.sites[1],craft,{a});check(not ok and d.cooldownUntil==c.tick+600,'Incompatible drive did not enter deterministic cooldown');eq(a.hp,hp-5,'Nearby incompatible discharge did not use normal health')
 end)
 group(r,'G08-F long deep-space launch charges metal/component once, pulses, lazily arrives and replays',function()
  local c=campaign(10808);local w=c.sites[1].world;local a=w.workers[1];local d=c.relics.drive;d.installed=true;d.craftId=1;d.siteId=1;d.sockets={1,2,3}
  for _,id in ipairs({1,2,3}) do local q=Relics.cache(c,id);q.empty=true;local x=Relics.find(c,id);x.state='drive';x.craftId=1;x.socket=id end
  c.region.systems[2].revealed=true;c.region.systems[3].revealed=true;World.stack(w,'component',1,a.x,a.y);local h=History.new(c);local person=a.personId
  assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=8,passengers={person},cargo={food=8,metal=2,component=1}}));advance(h,650);local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(h,250);manifest=assert(Logistics.manifest(h.live,1));assert(Logistics.readiness(h.live,manifest));assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(h,1)
  local craft=h.live.logistics.crafts[1];check(craft.journey and craft.journey.deep and craft.journey.unstable,'G08 launch did not enter unstable deep-space transit');eq(h.live.travel.accounts.sites[1].consumed.metal,1);eq(h.live.travel.accounts.sites[1].consumed.component,1)
  advance(h,2400);local remote=h.live.sites[8];check(remote.world and remote.ownerSocietyId==1 and remote.world.workers[1].personId==person,'First remote arrival did not lazily found normal settlement');check(remote.region==nil,'Remote site created a parallel region');check(remote.world.workers[1].hp<100 and remote.world.workers[1].stress>0,'Unstable transit pulses did not affect real passenger psychology/health')
  local saved=h:saveText();local restored=History.fromText(saved);eq(Codec.encode(restored.live),Codec.encode(h.live),'Deep-space codec round-trip diverged');restored:seek(1);while restored.seekTarget do restored:updateSeek(200) end;check(restored.view.sites[8].world==nil and restored.view.sites[8].ownerSocietyId==nil and restored.view.region.systems[2].revealed,'Pre-arrival seek leaked remote map/ownership or lost pre-existing scan metadata')
  -- Practice branching uses the one existing whole-campaign history.  A
  -- security command is sufficient to force the alternate branch without
  -- inventing a second G08 timeline; the abandoned arrival cannot leak its
  -- instantiated world, ownership, or passenger into that branch.
  assert(restored:queue({scope='site',siteId=1,payload={type='security_posture',posture='alert',expectedPolicyRevision=restored.view.sites[1].world.security.policyRevision}}))
  check(restored.live.sites[8].world==nil and restored.live.sites[8].ownerSocietyId==nil and restored.live.region.systems[2].revealed,'Practice branch inherited abandoned deep-space arrival state')
 end)
 group(r,'G08-G trade, raider custody transitions, and bounded relic state conserve one ID',function()
  local c=campaign(10809);local w=c.sites[1].world;local a=w.workers[1];local q=Relics.cache(c,1);q.empty=true;local relic=Relics.find(c,1);Relics.drop(c,relic,1,a.x,a.y);assert(Relics.pickup(c,1,relic.id,a.personId))
  local depot=install(w,6,6,'trade_depot');local relay=install(w,9,6,'signal_relay');relay._powerGranted=true;a.x,a.y=depot.gx*4-2,depot.gy*4;assert(Relics.storeDepot(c,1,depot.id,relic.id,a.personId));assert(Relics.trade(c,1,depot.id,relic.id,2));eq(relic.state,'courier');c.tick=c.tick+400;Relics.step(c);eq(relic.state,'faction');eq(relic.factionId,2)
  F.fill(w,3,3,100,24,M.AIR);F.fill(w,3,25,100,25,M.ROCK);Relics.drop(c,relic,1,30,24);local raider={id=77,factionId=2,x=29,y=24,alive=true};relic.state='raider';relic.siteId=1;relic.raiderId=raider.id;relic.factionId=2;Relics.dropRaider(c,w,raider);eq(relic.state,'ground');raider.x,raider.y=relic.x-1,relic.y;check(Relics.raidSteal(c,w,raider,relic),'Perceived reachable relic was not stolen by raider');Relics.retreatRaider(c,raider);eq(relic.state,'faction');for _,record in ipairs(c.sites) do if record.world then record.world.tick=c.tick end end;Campaign.validate(c)
 end)
 group(r,'G08-H remote local and remote-to-remote legs retain normal versus deep costs',function()
  local c=campaign(10810);local remote=Campaign.instantiate(c,8);c.sites[8].ownerSocietyId=1;local a=moveWorker(c,1,8,1);local person=a.personId;local craft=c.logistics.crafts[1];craft.dockedSiteId=8;craft.anchor={x=remote.home.x,y=remote.home.y};c.region.systems[2].revealed=true;c.region.systems[3].revealed=true
  local d=c.relics.drive;d.installed=true;d.craftId=craft.id;d.siteId=8;d.sockets={1,2,3}
  for _,id in ipairs({1,2,3}) do local relic=Relics.find(c,id);c.relics.caches[id].empty=true;relic.state='drive';relic.cacheId=nil;relic.craftId=craft.id;relic.socket=id end
  local cargo=function(food,metal,component) return {food=food,metal=metal,component=component,soil=0,stone=0,water=0} end
  local moon=launch(c,8,9,person,cargo(2,1,0));check(moon and c.sites[9].ownerSocietyId==1 and moon.workers[1].personId==person,'Remote primary-to-satellite did not use ordinary P04 arrival')
  eq(c.travel.accounts.sites[8].consumed.metal,1);eq(c.travel.accounts.sites[8].consumed.component,0,'Remote local route incorrectly charged deep component')
  local primary=launch(c,9,8,person,cargo(2,1,0));check(primary and primary.workers[1].personId==person,'Remote satellite-to-primary return did not preserve person identity')
  local other=launch(c,8,10,person,cargo(6,1,1));check(other and c.sites[10].ownerSocietyId==1 and other.workers[1].personId==person,'Direct remote-to-remote deep route failed after both systems were revealed')
  eq(c.travel.accounts.sites[8].consumed.metal,2);eq(c.travel.accounts.sites[8].consumed.component,1,'Remote deep route did not charge exactly one additional component');eq(c.travel.accounts.sites[9].consumed.component,0)
  Campaign.validate(c)
 end)
 print(r.groups..' G08 groups; '..r.assertions..' assertions passed.');return r
end
return T
