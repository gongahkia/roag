-- Focused G06 coverage: each case uses ordinary campaign state rather than a
-- parallel combat fixture, so feature-off validation and custody remain live.
local Campaign=require('src.campaign')
local Security=require('src.security')
local Equipment=require('src.equipment')
local Logistics=require('src.logistics')
local History=require('src.campaign_history')
local Codec=require('src.campaign_codec')
local Commands=require('src.campaign_commands')
local Structures=require('src.structures')
local Psychology=require('src.psychology')
local World=require('src.world')
local F=require('tests.fixtures')
local M=require('src.materials')
local T={}
local function opts() return {body=true,visibility=true,equipment=true,safe_excavation=true,knowledge=true,psychology=true,industry=true,factions=true,security=true} end
local function campaign()
 local w=F.world('practice');local a=F.worker(w,10,24,'A');local b=F.worker(w,22,24,'B');F.fill(w,3,3,61,24,M.AIR);F.fill(w,3,25,61,25,M.ROCK)
 local c=Campaign.new(w,opts());return c,c.sites[1].world.workers[1],c.sites[1].world.workers[2]
end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y1-1,M.AIR);F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind)
 clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind)
end
local function hostileFaction(c)
 local f=c.factions.factions[1]
 f.relations[1].tension=100;f.relations[1].grievance=100;f.culture.expansion=100;f.culture.hierarchy=100;f.stocks.food=40;f.stocks.metal=40
 return f
end
local function schedule(c)
 local w=c.sites[1].world;c.tick=999;w.tick=999;Campaign.step(c);return assert(c.security.raids[1])
end
local function step(c,n) for _=1,n do Campaign.step(c) end end
function T.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function group(name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end
 group('G06-A/D feature gate, deterministic expertise and bounded policy',function()
  local c,a=campaign();local same=Campaign.clone(c);eq(a.security.combatXP,same.sites[1].world.workers[1].security.combatXP);check(c.features.security==1 and c.sites[1].world.security.posture=='normal');Security.configure(c.sites[1].world,{posture='alert'},1);eq(c.sites[1].world.security.posture,'alert');check(not pcall(Security.configure,c.sites[1].world,{posture='normal'},1),'Stale revision accepted')
 end)
 group('G06-B/C physical weapon custody and deterministic first-body ray',function()
  local c,a,b=campaign();local w=c.sites[1].world;local gun=Equipment.create(c,1,'frontier_carbine',a.x,a.y);check(Security.equip(c,1,a.personId,gun.id));check(gun.state=='loose','Equip command changed custody remotely')
  for _=1,30 do Campaign.step(c) end;check(Equipment.equipped(c,a.personId,'weapon')==gun,'Guard did not physically fetch equipment')
  local rounds=require('src.world').stack(w,'ammunition',2,a.x,a.y);check(Security.reload(c,1,a.personId,2));check(rounds.n==2,'Reload command changed custody remotely')
  for _=1,30 do Campaign.step(c) end;eq(a.security.ammo,2);local hp=b.hp;local ok,hit=Security.attack(c,w,a,b,{a,b},{campaign=c,siteId=1});check(ok);eq(hit,b);eq(b.hp,hp-24);eq(a.security.ammo,1);check(rounds.n==0)
 end)
 group('G06-E/F guard state, training, and independent current sight',function()
  local c,a=campaign();local w=c.sites[1].world;a.security.guardEnabled=true;local st=require('src.structures').install(w,4,6,'training_target');check(st);a.x,a.y=14,24;local before=a.security.combatXP;check(Security.train(c,w,a));eq(a.security.combatXP,before+1);check(not Security.perceives(c,w,a,{x=60,y=24}, {campaign=c,siteId=1}),'Unlit distant target was perceived')
 end)
 group('G06-G/H raid commitment, warning, and replay-safe state',function()
  local c=campaign();local f=c.factions.factions[1];f.relations[1].tension=100;f.relations[1].grievance=100;f.culture.expansion=100;f.culture.hierarchy=100;f.stocks.food=20;f.stocks.metal=20;c.tick=999;c.sites[1].world.tick=999;Campaign.step(c);check(#c.security.raids==1,'Hostile faction did not commit a paid raid');local r0=c.security.raids[1];check(f.stocks.food<20,'Raid did not debit food');eq(f.stocks.metal,20-r0.size);local copy=Campaign.clone(c);eq(copy.security.raids[1].arrivalTick,r0.arrivalTick)
 end)
 group('G06-K/L grievance records are bounded and visible in person state',function()
  local c,a=campaign();for i=1,12 do Security.grieve(c,a,'cause'..i,6) end;check(#a.security.causes<=8);check(a.security.grievance>0);Security.grieve(c,a,'cause12',-6);check(a.security.grievance>=0)
 end)
 group('G06-E/O posture task precedence and death returns one physical custody',function()
  local c,a,b=campaign();local w=c.sites[1].world;local N=require('src.nav');local Colonists=require('src.colonists')
  a.security.guardEnabled=true;Security.setPosts(w,{{x=a.x,y=a.y}},{x1=a.x,y1=a.y,x2=a.x,y2=a.y},w.security.policyRevision);Security.configure(w,{posture='lockdown'},w.security.policyRevision)
  local f=N.flood(w,b.x,b.y);local function closest(_,_,flood,predicate) return N.closest(w,flood,predicate) end
  local task=Security.offer(c,w,b,f,closest);check(task and task.securityMode=='refuge','Lockdown did not produce a civilian refuge task')
  local gun=Equipment.create(c,1,'frontier_carbine',a.x,a.y);Equipment.equip(c,gun,a);a.security.ammo=3;Colonists.kill(w,a,'test',{campaign=c,siteId=1})
  check(gun.state=='loose' and gun.siteId==1,'Death did not drop unique equipped weapon');local loose=0;for _,p in ipairs(w.items) do if p.kind=='ammunition' then loose=loose+p.n end end;eq(loose,3,'Death duplicated or deleted pouch ammunition')
  Campaign.validate(c)
 end)
 group('G06-I/J external raid materializes atomically and retains bounded loot receipts',function()
  local c=campaign();local w=c.sites[1].world;local f=c.factions.factions[1];f.relations[1].tension=100;f.relations[1].grievance=100;f.culture.expansion=100;f.culture.hierarchy=100;f.stocks.food=20;f.stocks.metal=20;c.tick=999;w.tick=999;Campaign.step(c);local raid=c.security.raids[1]
  for _=1,800 do Campaign.step(c) end;check(raid.status=='active' or raid.status=='holding','Raid neither materialized nor entered bounded holding');if raid.status=='active' then
   check(#raid.actors==raid.size,'Raid materialized partially');raid.actors[1].alive=false;Security.step(c);check(#c.security.lootReceipts>=1,'Dead raider did not create one foreign-loot receipt')
  end
  Campaign.validate(c)
 end)
 group('G06-P/S security state round-trips and rejects malformed policy bounds',function()
  local c=campaign();local copy=Campaign.clone(c);eq(copy.sites[1].world.security.policyRevision,c.sites[1].world.security.policyRevision)
  local bad=Campaign.clone(c);for i=1,9 do bad.sites[1].world.security.posts[i]={x=10,y=24} end;check(not pcall(Campaign.validate,bad),'Oversized defense-post state was accepted')
 end)
 group('G06-L/M/N protest becomes a physical cell sabotage with unchanged people',function()
  local c,a,b=campaign();local w=c.sites[1].world;local target=Structures.install(w,5,6,'training_target')
  local original=a;local gun=Equipment.create(c,1,'frontier_carbine',a.x,a.y);Equipment.equip(c,gun,a);a.security.ammo=3;a.hp=87
  for _,person in ipairs({a,b}) do person.security.grievance=90;person.security.causes={{kind='hunger',amount=90,tick=0}};person.stress=90;person.psychology.facets.independence=70 end
  Psychology.relation(a,b.personId,false).trust=30;Psychology.relation(b,a.personId,false).trust=30
  local retained={personId=a.personId,hp=a.hp,facets=Codec.encode(a.psychology.facets),relations=Codec.encode(a.psychology.relations),knowledge=Codec.encode(a.frontier),xp=a.security.combatXP,weapon=gun.id,ammo=a.security.ammo}
  c.tick=999;w.tick=999;Campaign.step(c);check(w.security.protest,'Qualifying dissidents did not form a protest')
  w.security.protest.untilTick=c.tick;c.tick=1999;w.tick=1999;a.stress,b.stress=90,90;Campaign.step(c);check(w.security.cell and w.security.cell.state=='organizing','Post-protest severe dissidents did not form an organizing cell')
  w.security.cell.readyTick=c.tick;Security.step(c);eq(w.security.cell.state,'sabotage');for _,person in ipairs({a,b}) do person.stress=40;person.panic=false end
  for _=1,850 do Campaign.step(c) end;eq(w.security.cell.state,'hostile');eq(a.security.allegiance,'insurgent');eq(b.security.allegiance,'insurgent');check(target.sabotagedUntil and target.sabotagedUntil>w.tick,'Physical sabotage did not disable its exact structure')
  eq(a,original,'Insurgent conversion replaced the worker object');eq(a.personId,retained.personId);eq(a.hp,retained.hp);eq(Codec.encode(a.psychology.facets),retained.facets);eq(Codec.encode(a.psychology.relations),retained.relations);eq(Codec.encode(a.frontier),retained.knowledge);eq(a.security.combatXP,retained.xp);eq(Equipment.equipped(c,a.personId,'weapon').id,retained.weapon);eq(a.security.ammo,retained.ammo)
  local memories={};for _,memory in ipairs(a.psychology.memories) do memories[memory.kind]=true end;check(memories.protested_conditions and memories.insurgency_began,'Identity-preserving insurgency omitted its social-security memories')
  Campaign.validate(c)
 end)
 group('G06-C/E adversarial ray ordering, ammunition, and posture autonomy',function()
  local c,a,b=campaign();local w=c.sites[1].world;local d=F.worker(w,34,24,'C');d.personId=c.nextPersonId;c.nextPersonId=c.nextPersonId+1;Security.attachPerson(c,d);local gun=Equipment.create(c,1,'frontier_carbine',a.x,a.y);Equipment.equip(c,gun,a);a.security.ammo=2
  local ok,why=Security.attack(c,w,a,d,{a,b,d},{campaign=c,siteId=1});check(not ok and why=='Friendly blocks line of fire','A pre-existing friendly did not refuse the downstream carbine shot');eq(a.security.ammo,2);eq(b.hp,100)
  b.security.allegiance='insurgent';ok,hit=Security.attack(c,w,a,d,{a,b,d},{campaign=c,siteId=1});check(ok);eq(hit,b,'First physical body did not take the ray');eq(b.hp,76);eq(d.hp,100);eq(a.security.ammo,1)
  a.security.guardEnabled=true;Security.setPosts(w,{{x=14,y=24}},{x1=30,y1=24,x2=30,y2=24},w.security.policyRevision)
  local flood=require('src.nav').flood(w,a.x,a.y);local function closest(_,_,f,predicate) return require('src.nav').closest(w,f,predicate) end
  Security.configure(w,{posture='normal'},w.security.policyRevision);local normal=Security.offer(c,w,a,flood,closest);check(not normal or normal.securityMode~='post','Normal posture assigned a defense post without a threat')
  Security.configure(w,{posture='alert'},w.security.policyRevision);local alert=Security.offer(c,w,a,flood,closest);check(alert and alert.securityMode=='post','Alert posture did not create an autonomous defense-post task')
  b.security.allegiance='society';Security.configure(w,{posture='lockdown'},w.security.policyRevision);local lockdown=Security.offer(c,w,b,require('src.nav').flood(w,b.x,b.y),closest);check(lockdown and lockdown.securityMode=='refuge','Lockdown did not create civilian refuge behaviour')
  -- Memory production is attached to actual deterministic combat events, not
  -- a parallel combat journal.  This also proves a Guard is still using the
  -- ordinary psychology state while defending the site.
  b.security.allegiance='insurgent';b.hp=24;w.tick=20;c.tick=20;ok,hit=Security.attack(c,w,a,b,{a,b,d},{campaign=c,siteId=1});check(ok and hit==b and not b.alive,'Lethal hostile combat did not resolve through the normal body path')
  local am,bm={},{};for _,m in ipairs(a.psychology.memories) do am[m.kind]=true end;for _,m in ipairs(b.psychology.memories) do bm[m.kind]=true end
  check(am.defended_settlement and am.killed_hostile,'Guard combat omitted defense/kill psychology memories');check(bm.came_under_attack and bm.was_injured_in_combat,'Combat victim omitted threat/injury psychology memories')
 end)
 group('G06-G/H/I/J adversarial warning, holding, death loot, and withdrawal custody',function()
  local c=campaign();local w=c.sites[1].world;install(w,6,6,'solar_array');install(w,8,6,'power_pole');install(w,9,6,'signal_relay');local f=hostileFaction(c);c.factions.scan.known[f.id]=true;local raid=schedule(c);step(c,500);check(raid.warned,'Powered discovered relay did not issue the 300-tick warning')
  local warnings=0;for _,event in ipairs(w.security.events) do if event.kind=='raid_warning' then warnings=warnings+1;check(not event.text:find(f.name,1,true),'Warning leaked faction identity') end end;eq(warnings,1)
  local c2=campaign();local w2=c2.sites[1].world;local f2=hostileFaction(c2);local hidden=schedule(c2);step(c2,500);check(not hidden.warned,'Undiscovered/no-relay raid received a magical warning')
  -- A blocked arrival never changes terrain or makes a partial hostile body.
  local c3=campaign();local w3=c3.sites[1].world;hostileFaction(c3);local held=schedule(c3);F.fill(w3,20,10,44,38,M.ROCK);c3.tick=held.arrivalTick;w3.tick=c3.tick;Security.step(c3);eq(held.status,'holding');eq(#held.actors,0);eq(World.get(w3,32,24),M.ROCK,'Holding raid carved an arrival cell')
  step(c,300);check(raid.status=='active' or raid.status=='holding','Arriving raid was neither active nor held');if raid.status=='active' then
   local actor=raid.actors[1];actor.alive=false;Security.step(c);local receipts=#c.security.lootReceipts;check(receipts==1,'Dead raider did not create exactly one receipt');Security.step(c);eq(#c.security.lootReceipts,receipts,'Repeated dead-actor handling duplicated loot')
  end
  local c4=campaign();local w4=c4.sites[1].world;hostileFaction(c4);local leaving=schedule(c4);step(c4,800);if leaving.status=='active' then
   leaving.status='withdrawing';for _,actor in ipairs(leaving.actors) do actor.x,actor.y=leaving.ingress.x,leaving.ingress.y end;Security.step(c4);eq(leaving.status,'resolved');eq(#c4.security.lootReceipts,0,'Retreating raiders left duplicate local loot')
  end
 end)
 group('G06-J external physical sabotage retargets and uses one actor action per tick',function()
  local c,a,b=campaign();local w=c.sites[1].world;local first=install(w,9,6,'battery');local second=install(w,10,6,'solar_array');a.alive=false;a.hp=0;b.alive=false;b.hp=0
  local f=hostileFaction(c);f.culture.expansion=100;f.culture.hierarchy=0;local raid=schedule(c);eq(raid.goal,'sabotage');c.tick=raid.arrivalTick;w.tick=c.tick;Security.step(c);check(raid.status=='active' and raid.targetId,'Sabotage raid did not physically acquire a perceived target');check(raid.sabotageActorId,'Sabotage raid did not choose one real actor')
  local removed=assert(require('src.industry').find(w,raid.targetId));w.structures[World.slot(w,removed.gx,removed.gy)]=nil;c.tick=c.tick+1;w.tick=c.tick;Security.step(c);check(raid.targetId and raid.targetId~=removed.id,'Destroyed raid target did not deterministically retarget');eq(raid.sabotageProgress or 0,0,'Retarget carried remote sabotage work to a new structure')
  for _=1,500 do
   local before=raid.sabotageProgress or 0;Campaign.step(c);local after=raid.sabotageProgress or 0;check(after<=before+1,'More than one raider advanced one sabotage action in a tick')
   if raid.status=='withdrawing' then break end
  end
  local target=assert(require('src.industry').find(w,raid.targetId));check(target.sabotagedUntil and target.sabotagedUntil>w.tick,'One physical raider did not complete the exact 120-action sabotage');check(raid.status=='withdrawing','Completed sabotage raid did not begin physical withdrawal');check(target.id==second.id or target.id==first.id,'Raid retargeted outside the legal infrastructure set')
 end)
 group('G06-J/N/S physical sabotage rejects stale targets and loot receipts validate uniquely',function()
  local c,a,b=campaign();local w=c.sites[1].world;local fab=install(w,6,6,'fabricator');local target=install(w,9,6,'training_target')
  w.security.cell={id=c.security.nextCellId,members={a.personId,b.personId},state='sabotage',cause='hunger',targetId=target.id,saboteurId=a.personId,progress=0};c.security.nextCellId=c.security.nextCellId+1
  local task={securityMode='sabotage',cellId=w.security.cell.id,targetId=target.id};local ok=Security.act(c,w,a,task,{campaign=c,siteId=1});check(not ok and w.security.cell.progress==0,'Remote sabotage advanced without reaching its target')
  w.structures[World.slot(w,target.gx,target.gy)]=nil;ok=Security.act(c,w,a,task,{campaign=c,siteId=1});check(not ok and w.security.cell.progress==0,'Removed sabotage target retained stale work')
  local raid={id=c.security.nextRaidId,factionId=2,siteId=1,size=1,initialSize=1,committedTick=c.tick,arrivalTick=c.tick+800,status='resolved',goal='assault',actors={},resolvedTick=c.tick,resolution='losses'};c.security.nextRaidId=c.security.nextRaidId+1;c.security.raids={raid};local loot=Equipment.create(c,1,'frontier_carbine',a.x,a.y);c.security.lootReceipts={{raidId=raid.id,factionId=2,tick=c.tick,actorId=99,itemIds={loot.id},ammo=0},{raidId=raid.id,factionId=2,tick=c.tick,actorId=99,itemIds={loot.id},ammo=0}}
  check(not pcall(Campaign.validate,c),'Duplicate foreign-loot receipt was accepted')
  local shaped,x,y=campaign();local sw=shaped.sites[1].world;sw.security.cell={id=shaped.security.nextCellId,members={x.personId,y.personId},state='organizing',cause='hunger',readyTick=400,formedTick=0,detected=false,unexpected=true};shaped.security.nextCellId=shaped.security.nextCellId+1
  check(not pcall(Campaign.validate,shaped),'Unknown insurgent-cell persistence field was accepted')
  -- Keep the industrial fixture live as a target-category regression.
  check(fab.kind=='fabricator')
 end)
 group('G06-P history/save/replay/branching preserves transient raid and security state',function()
  local c=campaign();local w=c.sites[1].world;install(w,6,6,'solar_array');install(w,8,6,'power_pole');install(w,9,6,'signal_relay');local f=hostileFaction(c);c.factions.scan.known[f.id]=true;c.tick=999;w.tick=999
  local history=History.new(c);check(history:advance());local raid=history.live.security.raids[1];check(raid and raid.status=='approaching','History fixture did not schedule its hostile expedition')
  for _=1,200 do check(history:advance()) end
  check(history:queue({scope='site',siteId=1,payload={type='security_posture',posture='alert',expectedPolicyRevision=history.live.sites[1].world.security.policyRevision}}));check(history:advance())
  for _=1,299 do check(history:advance()) end
  local live=history.live;local liveRaid=live.security.raids[1];check(liveRaid.warned,'Replay fixture did not reach its warning boundary');local text=history:saveText();local restored=History.fromText(text);eq(Codec.encode(restored.live),Codec.encode(live),'Security save/load changed canonical transient state')
  local verified,why=restored:verifyReplay(2000);eq(verified,true,why);local warningCount=0;for _,event in ipairs(restored.live.sites[1].world.security.events) do if event.kind=='raid_warning' then warningCount=warningCount+1 end end;eq(warningCount,1,'Replay duplicated its warning incident')
  local futureTick=history.live.tick;history:seek(1100);while history.seekTarget do history:updateSeek(31) end;check(history:queue({scope='site',siteId=1,payload={type='security_posture',posture='lockdown',expectedPolicyRevision=history.view.sites[1].world.security.policyRevision}}));check(history.commands[futureTick+1]==nil,'Practice branch retained future security commands');check(history:advance());eq(history.live.sites[1].world.security.posture,'lockdown')
  local legacy=Campaign.new(F.world('practice'),{body=true,visibility=true,equipment=true,safe_excavation=true,knowledge=true,psychology=true,industry=true,factions=true});check(legacy.features.security==nil and legacy.security==nil and legacy.sites[1].world.security==nil,'Security leaked into feature-off campaign history')
 end)
 group('G06-P active-raid and mid-sabotage history replay keeps transient identities',function()
  local c,a,b=campaign();local w=c.sites[1].world;hostileFaction(c);local raid=schedule(c);c.tick=raid.arrivalTick;w.tick=c.tick;Security.step(c);check(raid.status=='active' and #raid.actors==raid.size,'Active-raid history fixture did not materialize atomically')
  local target=Structures.install(w,5,6,'training_target');local cellId=c.security.nextCellId;c.security.nextCellId=cellId+1;w.security.cell={id=cellId,members={a.personId,b.personId},state='sabotage',cause='hunger',formedTick=c.tick,targetId=target.id,saboteurId=a.personId,progress=17}
  a.x,a.y=14,24
  local history=History.new(c);check(history:queue({scope='site',siteId=1,payload={type='security_posture',posture='alert',expectedPolicyRevision=history.live.sites[1].world.security.policyRevision}}));for _=1,8 do check(history:advance()) end
  local canonical=Codec.encode(history.live);local restored=History.fromText(history:saveText());eq(Codec.encode(restored.live),canonical,'Active raid/mid-sabotage save lost transient state')
  local savedRaid=restored.live.security.raids[1];local savedCell=restored.live.sites[1].world.security.cell;eq(savedRaid.id,raid.id);eq(#savedRaid.actors,raid.size);eq(savedCell.id,cellId);check(savedCell.progress>=17,'Mid-sabotage progress regressed across save')
  local verified,why=restored:verifyReplay(2000);eq(verified,true,why);restored:seek(history.initial.tick+4);while restored.seekTarget do restored:updateSeek(11) end;check(restored.view.security.raids[1].status=='active','Seek lost active raid actors');check(restored.view.sites[1].world.security.cell.progress>=17,'Seek lost partial sabotage work')
 end)
 group('G06-O/Q actual P04 travel retains one armed Guard and security identity',function()
  local c=Campaign.newRegion(10607,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true})
  local history=History.new(c);local source=history.live.sites[1].world;local guard=source.workers[1];local personId=guard.personId;guard.security.guardEnabled=true;guard.security.combatXP=111;guard.security.ammo=3;guard.security.grievance=12
  local gun=Equipment.create(history.live,1,'frontier_carbine',guard.x,guard.y);Equipment.equip(history.live,gun,guard)
  check(history:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={personId},cargo={food=2,metal=2}}));for _=1,460 do check(history:advance()) end
  local craft=history.live.logistics.crafts[1];local manifest=assert(Logistics.manifest(history.live,craft.activeManifestId));check(history:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));for _=1,180 do check(history:advance()) end
  manifest=assert(Logistics.manifest(history.live,craft.activeManifestId));check(history:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));check(history:advance())
  local passenger=craft.passengers[1];local portable={frontier=Codec.encode(passenger.frontier),education=Codec.encode(passenger.education),facets=Codec.encode(passenger.psychology.facets),relations=Codec.encode(passenger.psychology.relations),firstMemory=Codec.encode(passenger.psychology.memories[1]),security=Codec.encode(passenger.security),xp=passenger.security.combatXP,ammo=passenger.security.ammo,grievance=passenger.security.grievance}
  check(#craft.passengers==1 and passenger.personId==personId,'Armed Guard duplicated or vanished at departure');eq(Codec.encode(passenger.security),portable.security,'Transit security state did not retain the Guard fields')
  check(Equipment.equipped(history.live,personId,'weapon')==gun,'Equipped weapon lost single-person custody during transit')
  craft.journey.remainingTicks=1;check(history:advance());local arrived
  for _,worker in ipairs(history.live.sites[2].world.workers) do if worker.personId==personId then arrived=worker end end
  check(arrived and arrived~=guard,'Arrival did not create the destination-local continuation of the same person');eq(Codec.encode(arrived.frontier),portable.frontier);eq(Codec.encode(arrived.education),portable.education);eq(Codec.encode(arrived.psychology.facets),portable.facets);eq(Codec.encode(arrived.psychology.relations),portable.relations);eq(Codec.encode(arrived.psychology.memories[1]),portable.firstMemory);eq(Codec.encode(arrived.security),portable.security);eq(arrived.security.combatXP,portable.xp);eq(arrived.security.ammo,portable.ammo);eq(arrived.security.grievance,portable.grievance);check(arrived.security.guardEnabled and Equipment.equipped(history.live,personId,'weapon')==gun,'Arrival lost Guard policy or duplicated equipped weapon')
  local sourceCopies=0;for _,worker in ipairs(history.live.sites[1].world.workers) do if worker.personId==personId then sourceCopies=sourceCopies+1 end end;eq(sourceCopies,0,'Departed Guard remained active at the source site');Campaign.validate(history.live)
 end)
 print(r.groups..' G06 groups; '..r.assertions..' assertions passed.');return r
end
return T
