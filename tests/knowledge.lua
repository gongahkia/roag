-- COS-P05 headless evidence, study, portability, and storage contracts.
local U=require('src.util')
local W=require('src.world')
local M=require('src.materials')
local Content=require('src.content')
local Ecology=require('src.ecology')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Codec=require('src.campaign_codec')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')
local F=require('tests.fixtures')
local T={}

local function advance(history,n)
 for _=1,n do assert(history:advance()) end
end
local function personal(worker) return worker.frontier.knowledge end
local function observation(worker)
 return personal(worker).observations[1]
end
local function controlled(kind)
 local world=F.world('practice');local a=F.worker(world,16,24,'A');local b=F.worker(world,7,24,'B')
 Content.install(world,{version=1,crew=3,flora={{kind=kind or 'filter',x=20,y=24,food=0,water=0,phase=0}},fauna={},sites={},ruins={}})
 local campaign=Campaign.new(world,{knowledge=true})
 return campaign,campaign.sites[1].world.workers[1],campaign.sites[1].world.workers[2],campaign.sites[1].world.content.flora[1]
end
local function effect(campaign,tick,material)
 local world=campaign.sites[1].world;local plant=world.content.flora[1]
 campaign.tick=tick;world.tick=tick;W.put(world,plant.x+1,plant.y,material)
 Ecology.step(world,{campaign=campaign,siteId=1});Campaign.validate(campaign)
 return plant.x+1,plant.y
end
local function field(history,kind,worker,target,limit)
 assert(history:queue({scope='site',siteId=1,payload={type='field',kind=kind,target=target,worker=worker.id,priority=3}}))
 advance(history,limit)
end
local function filterEvidence(campaign)
 effect(campaign,20,M.STEAM);effect(campaign,40,M.STEAM)
end
local function learnFilter(campaign)
 filterEvidence(campaign);local history=History.new(campaign);local world=history.live.sites[1].world;local a=world.workers[1];local plant=world.content.flora[1]
 field(history,'survey',a,plant.id,120);assert(Knowledge.identified(a,'flora','filter'))
 field(history,'study',a,plant.id,320);assert(Knowledge.supports(a,'flora','filter'))
 return history,a,plant
end
local function regional(seed)
 return Campaign.newRegion(seed,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true})
end
local function controlledRegional(seed)
 local campaign=regional(seed);local world=campaign.sites[1].world;world.content.flora={};world.content.fauna={};world.content.sites={};world.content.signals={};world.content.discoveries={};world.content.observed={}
 local plant={id=W.id(world),kind='filter',x=world.home.left+25,y=world.home.floor-1,food=0,water=0,phase=0,alive=true};world.content.flora={plant}
 local a=world.workers[1];a.x,a.y=plant.x-4,plant.y
 for _,site in ipairs(campaign.sites) do site.world.tick=0 end
 Campaign.validate(campaign);return campaign
end
local function regionalEffect(campaign,tick)
 local world=campaign.sites[1].world;local plant=world.content.flora[1]
 campaign.tick=tick;for _,site in ipairs(campaign.sites) do site.world.tick=tick end
 W.put(world,plant.x+1,plant.y,M.STEAM);Ecology.step(world,{campaign=campaign,siteId=1});Campaign.validate(campaign)
end
local function personAt(campaign,siteId,personId)
 for _,worker in ipairs(campaign.sites[siteId].world.workers) do if worker.personId==personId then return worker end end
end

function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'P05 assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'P05 mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected rejection') end
 local function group(name,fn) local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err));report.groups=report.groups+1;print('PASS  '..name) end

 group('P05-A personal sightings use living local bodies, clear range, and no camera state',function()
  local c,a,b=controlled('filter');effect(c,20,M.STEAM)
  check(#personal(a).observations==1,'Near living observer missed a visible encounter');eq(#personal(b).observations,0,'Distant worker gained a sighting')
  a.alive=false;effect(c,40,M.STEAM);eq(observation(a).effects[1].distinctTicks,1,'Dead worker gained later evidence')
 end)

 group('P05-B actual glass-reed and iron-thorn mutations issue typed successful-effect evidence only',function()
  local filter,a=controlled('filter');local x,y=effect(filter,20,M.STEAM);eq(W.get(filter.sites[1].world,x,y),M.WATER);eq(observation(a).effects[1].kind,'filter_steam_to_water')
  local thorn,ta=controlled('thorn');local tx,ty=effect(thorn,240,M.SAND);eq(W.get(thorn.sites[1].world,tx,ty),M.ROCK);eq(observation(ta).effects[1].kind,'thorn_sand_to_rock')
  local failed,fa=controlled('filter');effect(failed,20,M.WATER);eq(#personal(fa).observations,1);eq(#observation(fa).effects,0,'Unrelated water state produced an ecology receipt')
 end)

 group('P05-C survey identifies only its acting person and remains repeatable for another person',function()
  local c=controlled('filter');filterEvidence(c);local h=History.new(c);local world=h.live.sites[1].world;local a,b,plant=world.workers[1],world.workers[2],world.content.flora[1]
  field(h,'survey',a,plant.id,120);check(Knowledge.identified(a,'flora','filter'));check(not Knowledge.identified(b,'flora','filter'),'Survey globally identified the encounter')
  field(h,'survey',b,plant.id,180);check(Knowledge.identified(b,'flora','filter'),'A later personal survey was blocked by A')
 end)

 group('P05-D study requires one actor identification, two distinct firsthand effects, and 120 work actions',function()
  local c,a,_,plant=controlled('filter');local h=History.new(c)
  field(h,'study',a,plant.id,30);check(not Knowledge.supports(a,'flora','filter'),'Study bypassed missing identification/evidence')
  filterEvidence(c);h=History.new(c);a=h.live.sites[1].world.workers[1];plant=h.live.sites[1].world.content.flora[1]
  field(h,'survey',a,plant.id,120);field(h,'study',a,plant.id,80);check(not Knowledge.supports(a,'flora','filter'),'Study completed before 120 real actions')
  field(h,'study',a,plant.id,280);check(Knowledge.supports(a,'flora','filter'),'Eligible personal study did not complete')
  eq(#personal(a).facts,2,'Repeated study created duplicate personal facts')
 end)

 group('P05-E cancellation, handover, death, and destroyed targets retain only earned personal progress',function()
  local c=controlled('filter');filterEvidence(c);local h=History.new(c);local w=h.live.sites[1].world;local a,b,plant=w.workers[1],w.workers[2],w.content.flora[1]
  field(h,'survey',a,plant.id,120);field(h,'study',a,plant.id,70);local progress=personal(a).studies[1].progress;check(progress>0 and progress<120)
  local gx,gy=W.tile(w,plant.x,plant.y);assert(h:queue({scope='site',siteId=1,payload={type='cancel',gx=gx,gy=gy}}));advance(h,1)
  eq(personal(a).studies[1].progress,progress,'Cancellation erased earned personal progress')
  field(h,'study',b,plant.id,60);eq(#personal(b).studies,0,'B inherited A evidence or progress')
  plant.alive=false;advance(h,1);eq(personal(a).studies[1].progress,progress,'Destroyed target corrupted retained study state')
  a.alive=false;Campaign.validate(h.live);check(#personal(a).studies==1,'Dead record lost its historical progress')
 end)

 group('P05-F real logistics and travel preserve an expert while other people remain untrained',function()
  local c=controlledRegional(9051);regionalEffect(c,20);regionalEffect(c,40);local h=History.new(c);local world=h.live.sites[1].world;local a,b,plant=world.workers[1],world.workers[2],world.content.flora[1]
  field(h,'survey',a,plant.id,140);field(h,'study',a,plant.id,330);local fact=assert(Knowledge.supports(a,'flora','filter'));local personId=a.personId;check(not Knowledge.supports(b,'flora','filter'))
  assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={personId},cargo={food=2,metal=2}}));advance(h,520)
  local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(h,220);manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest);assert(ready,why)
  assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(h,400)
  local arrived=assert(personAt(h.live,2,personId));check(Knowledge.supports(arrived,'flora','filter'),'Travel lost the expert fact');eq(arrived.frontier.knowledge.facts[2].tick,fact.tick,'Travel changed fact acquisition timing');check(not Knowledge.supports(h.live.sites[1].world.workers[1],'flora','filter'),'Home colleague gained travel expert knowledge')
 end)

 group('P05-G labels stay neutral until personal identification and historical state is not a capability',function()
  local c,a=controlled('filter');eq(Knowledge.label(a,'flora','filter'),'Unidentified growth');filterEvidence(c);local h=History.new(c);a=h.live.sites[1].world.workers[1];field(h,'survey',a,h.live.sites[1].world.content.flora[1].id,120)
  eq(Knowledge.label(a,'flora','filter'),'Glass reed');check(not Knowledge.supports(a,'flora','filter'));check(#h.live.knowledge.history==1,'Survey acquisition did not record bounded history')
 end)

 group('P05-H raw observations cap deterministically while learned facts remain separate',function()
  local c,a=controlled('filter');local context={campaign=c,siteId=1}
  for id=1,65 do Knowledge.sighting(context,a,{siteId=1,category='flora',id=1000+id,kind='filter',definitionVersion=1}) end
  eq(#personal(a).observations,64);eq(personal(a).observations[1].source.id,1002,'Oldest raw observation was not evicted first')
 end)

 group('P05-I save, replay, seek, and practice branch preserve personal evidence and progress',function()
  local c=controlled('filter');filterEvidence(c);local h=History.new(c);local a=h.live.sites[1].world.workers[1];local plant=h.live.sites[1].world.content.flora[1]
  field(h,'survey',a,plant.id,120);field(h,'study',a,plant.id,70);local saved=h:saveText();local restored=History.fromText(saved)
  advance(h,50);advance(restored,50);eq(Codec.encode(h.live),Codec.encode(restored.live),'Knowledge continuation diverged after save/load')
  local verified,why=History.fromText(saved):verifyReplay(2000);check(verified,why)
  restored:seek(restored.frontier-10);while restored.seekTarget do restored:updateSeek(20) end;check(restored:queue({scope='site',siteId=1,payload={type='paint',x=8,y=8,material=M.AIR}}),'Practice branch rejected coherent personal knowledge history')
 end)

 group('P05-J observations leave material conversions and ledgers unchanged',function()
  local withKnowledge=controlled('filter');local withoutKnowledge=controlled('filter');withoutKnowledge.features.knowledge=nil;withoutKnowledge.knowledge=nil;withoutKnowledge.sites[1].world.frontier.knowledge=nil
  for _,worker in ipairs(withoutKnowledge.sites[1].world.workers) do worker.frontier=nil end
  local x,y=effect(withKnowledge,20,M.STEAM);local ox,oy=effect(withoutKnowledge,20,M.STEAM)
  eq(W.get(withKnowledge.sites[1].world,x,y),W.get(withoutKnowledge.sites[1].world,ox,oy));eq(withKnowledge.sites[1].world.ledger.waterMade,withoutKnowledge.sites[1].world.ledger.waterMade,'Knowledge receipt changed resource accounting')
 end)

 group('P05-K witness capture uses event-time geometry and no later position',function()
  local c,a,b,plant=controlled('filter');b.x=plant.x+10;effect(c,20,M.STEAM);b.x=plant.x-2;effect(c,40,M.STEAM)
  eq(#personal(b).observations,1,'Later entry did not observe the later visible event');eq(observation(b).effects[1].distinctTicks,1,'Later entry received retroactive first-event evidence')
 end)

 group('P05-L evidence identity isolates people and source instances',function()
  local c,a,b=controlled('filter');filterEvidence(c);local source=observation(a).source
  local clone={siteId=1,category='flora',id=source.id+1,kind='filter',definitionVersion=1};Knowledge.sighting({campaign=c,siteId=1},a,clone)
  eq(#personal(a).observations,2,'Distinct source IDs coalesced');check(#personal(b).observations==0,'Evidence pooled across people')
 end)

 group('P05-M actor progress resumes only for that actor and rebinding needs new evidence',function()
  local c=controlled('filter');filterEvidence(c);local h=History.new(c);local a=h.live.sites[1].world.workers[1];local p=h.live.sites[1].world.content.flora[1]
  field(h,'survey',a,p.id,120);field(h,'study',a,p.id,70);local earned=personal(a).studies[1].progress
  local source={siteId=1,category='flora',id=p.id+10,kind='filter',definitionVersion=1};local ok,why=Knowledge.canStudy({campaign=h.live,siteId=1},a,source);check(not ok and why:match('observations'),'Rebinding bypassed new-source evidence');eq(personal(a).studies[1].progress,earned)
 end)

 group('P05-N history remains informational after an expert leaves or dies',function()
  local c=controlled('filter');local h,a=learnFilter(c);local id=a.personId;a.alive=false;Campaign.validate(h.live)
  check(#h.live.knowledge.history>=2);check(not Knowledge.localExperts(h.live.sites[1].world,'flora','filter')[1],'Dead expert remained a live capability')
  eq(id,h.live.knowledge.history[#h.live.knowledge.history].personId)
 end)

 group('P05-O strict personal versions and duplicate facts reject while feature-off campaigns stay unchanged',function()
  local c=controlled('filter');local h,a=learnFilter(c);local bad=Campaign.clone(h.live);bad.sites[1].world.workers[1].frontier.knowledge.facts[1].version=2;reject(function() Campaign.validate(bad) end)
  local old=Campaign.new(F.world('practice'));check(old.features.knowledge==nil and old.knowledge==nil,'Feature-off campaign silently gained personal knowledge')
 end)

 group('P05-Q integrated no-grant trace learns, saves, and retains one expert',function()
  local c=controlled('filter');local h,a=learnFilter(c);local b=h.live.sites[1].world.workers[2]
  check(Knowledge.supports(a,'flora','filter'));check(not Knowledge.supports(b,'flora','filter'));check(#personal(a).facts==2);check(#h.live.knowledge.history==2)
  local encoded=Codec.encode(h.live);Campaign.validate(h.live);check(#encoded>0,'Knowledge trace did not encode')
 end)
 print(report.groups..' knowledge groups; '..report.assertions..' assertions passed.')
 return report
end
return T
