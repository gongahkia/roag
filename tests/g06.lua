-- Focused G06 coverage: each case uses ordinary campaign state rather than a
-- parallel combat fixture, so feature-off validation and custody remain live.
local Campaign=require('src.campaign')
local Security=require('src.security')
local Equipment=require('src.equipment')
local F=require('tests.fixtures')
local M=require('src.materials')
local T={}
local function opts() return {body=true,visibility=true,equipment=true,safe_excavation=true,knowledge=true,psychology=true,industry=true,factions=true,security=true} end
local function campaign()
 local w=F.world('practice');local a=F.worker(w,10,24,'A');local b=F.worker(w,22,24,'B');F.fill(w,3,3,61,24,M.AIR);F.fill(w,3,25,61,25,M.ROCK)
 local c=Campaign.new(w,opts());return c,c.sites[1].world.workers[1],c.sites[1].world.workers[2]
end
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
 print(r.groups..' G06 groups; '..r.assertions..' assertions passed.');return r
end
return T
