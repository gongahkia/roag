-- Final roadmap hardening contracts.  These are deliberately cross-layer
-- checks: they exercise the canonical campaign state rather than a parallel
-- late-game save format or a test-only custody ledger.
local Campaign=require('src.campaign')
local Codec=require('src.campaign_codec')
local Commands=require('src.campaign_commands')
local History=require('src.campaign_history')
local Relics=require('src.relics')
local Structures=require('src.structures')
local World=require('src.world')
local Labor=require('src.labor')
local F=require('tests.fixtures')
local M=require('src.materials')
local T={}

local function options(relics)
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
  logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true,relics=relics==true}
end
local function campaign(seed) return Campaign.newRegion(seed or 10999,options(true)) end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,Structures.width(kind));return Structures.install(w,gx,gy,kind) end
local function move(c,fromId,toId,index)
 local from,to=c.sites[fromId].world,c.sites[toId].world;local a=table.remove(from.workers,index or 1)
 a.id=World.id(to);a.x,a.y=to.home.x,to.home.y;to.workers[#to.workers+1]=a
 from.labor=Labor.default(from);from.laborAssignments=Labor.allocate(from);to.labor=Labor.default(to);to.laborAssignments=Labor.allocate(to)
 return a
end
local function step(c,n) for _=1,n do Campaign.step(c) end end
local function group(r,name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end

function T.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 group(r,'FINAL-A feature gates, 11-site graph, stable identities and legacy boundary',function()
  local legacy=Campaign.newRegion(10999,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true})
  eq(#legacy.sites,3);check(legacy.features.environments==nil and legacy.relics==nil,'Feature-off campaign gained late systems')
  local g07=Campaign.newRegion(10999,options(false));eq(#g07.sites,7);check(g07.relics==nil and g07.region.systems==nil,'G07 campaign gained G08 state')
  local c=campaign();eq(#c.sites,11);eq(#c.region.systems,3);eq(#c.relics.items,9);eq(#c.relics.caches,5)
  for id=8,11 do check(c.sites[id].world==nil and Relics.hiddenSite(c,id),'Undetected remote map was eagerly exposed') end
  for id=4,11 do Campaign.instantiate(c,id) end
  local ids={};for _,site in ipairs(c.sites) do if site.world then for _,a in ipairs(site.world.workers) do check(not ids[a.personId],'Duplicate campaign person ID across sites');ids[a.personId]=true end end end
  Campaign.validate(c)
 end)
 group(r,'FINAL-B analysis interruption, personal knowledge, codec continuation and relic custody',function()
  local c=campaign(11000);local w=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=1;local analyst=move(c,1,4,1);local learner=move(c,1,4,1);local q=Relics.cache(c,1)
  analyst.x,analyst.y=q.gx*4-2,q.gy*4;q.workerId=analyst.personId
  for _=1,180 do check(Relics.act(c,w,analyst,{mode='excavate',cacheId=1}),'Excavation did not use physical work') end
  check(q.empty and Relics.find(c,1).state=='ground','Cache recovery did not produce one ground relic')
  check(Relics.pickup(c,4,1,analyst.personId),'Recovered relic was not physically reachable')
  local analyzer=install(w,6,6,'relic_analyzer');analyzer._powerGranted=true;analyst.x,analyst.y=analyzer.gx*4-2,analyzer.gy*4
  check(Relics.loadAnalyzer(c,4,analyzer.id,1,analyst.personId));check(Relics.beginAnalysis(c,4,analyzer.id,1,analyst.personId))
  for _=1,40 do check(Relics.act(c,w,analyst,{mode='analysis',structureId=analyzer.id})) end
  local progress=analyzer.relic.task.progress;analyzer._powerGranted=false;check(not Relics.act(c,w,analyst,{mode='analysis',structureId=analyzer.id}),'Unpowered Analyzer advanced work');eq(analyzer.relic.task.progress,progress,'Power loss changed analysis progress')
  analyzer._powerGranted=true;check(Relics.act(c,w,analyst,{mode='analysis',structureId=analyzer.id}));eq(analyzer.relic.task.progress,progress+1,'Restored Analyzer did not resume exactly once')
  local round=Codec.decode(Codec.encode(c));Campaign.validate(round);eq(Codec.encode(round),Codec.encode(c),'Codec changed active Analyzer/custody state')
  local detail=Relics.describe(learner,Relics.find(c,1));check(detail.role==nil and detail.family==nil,'Unresearched relic leaked intrinsic role/signature')
 end)
 group(r,'FINAL-C deterministic continuation, invalid-command atomicity and history branch isolation',function()
  local a,b=campaign(11001),campaign(11001);step(a,240);step(b,240);eq(Codec.encode(a),Codec.encode(b),'Same seed/ticks diverged')
  local saved=Codec.decode(Codec.encode(a));step(a,120);step(saved,120);eq(Codec.encode(a),Codec.encode(saved),'Codec continuation diverged')
  local before=Codec.encode(a);local ok=Commands.apply(a,{scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=999,passengers={},cargo={}});check(not ok,'Invalid late-route command unexpectedly applied');eq(Codec.encode(a),before,'Rejected command partially mutated campaign')
  local h=History.new(campaign(11002));for _=1,5 do check(h:advance(),'History did not advance') end;h.live.region.systems[2].revealed=true;h.view=h.live;h:seek(0);while h.seekTarget do h:updateSeek(200) end
  check(not h.view.region.systems[2].revealed,'Seek before scan leaked remote-system reveal')
  check(h:queue({scope='site',siteId=1,payload={type='security_posture',posture='alert',expectedPolicyRevision=h.view.sites[1].world.security.policyRevision}}),'Practice branch command failed')
  check(not h.live.region.systems[2].revealed and h.live.sites[8].world==nil,'Pre-scan practice branch inherited reveal or remote map')
 end)
 group(r,'FINAL-D bounded late-state validators reject duplicate relic custody and stale references',function()
  local c=campaign(11003);local bad=Campaign.clone(c);local x=Relics.find(bad,1);x.state='ground';x.siteId=1;x.x=1;x.y=1;x.cacheId=1
  check(not pcall(Campaign.validate,bad),'Duplicate cache/ground relic custody passed validation')
  local stale=Campaign.clone(c);stale.relics.drive.installed=true;stale.relics.drive.craftId=999;stale.relics.drive.siteId=1
  check(not pcall(Campaign.validate,stale),'Stale drive craft reference passed validation')
  Campaign.validate(c)
 end)
 print(r.groups..' final hardening groups; '..r.assertions..' assertions passed.');return r
end
return T
