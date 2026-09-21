-- COS-G01 bounded 10,000-tick integration trace. It uses a controlled frontier
-- only for terrain/specimen placement; the torch, study, school and flight use
-- ordinary commands, jobs, materials and the campaign codec.
local W=require('src.world')
local M=require('src.materials')
local Ecology=require('src.ecology')
local Visibility=require('src.visibility')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')
local Codec=require('src.campaign_codec')

local seed=tonumber(arg[1]) or 9501;local ticks=tonumber(arg[2]) or 10000
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/g01_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=10000 and ticks<=100000,'Ticks must be 10000..100000')
local options={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true}
local c=Campaign.newRegion(seed,options);local home=c.sites[1].world
home.content.flora={};home.content.fauna={};home.content.sites={};home.content.ruins={};home.content.signals={};home.content.discoveries={};home.content.observed={}
local plant={id=W.id(home),kind='filter',x=home.home.left+25,y=home.home.floor-1,food=0,water=0,phase=0,alive=true}
local dark={id=W.id(home),kind='filter',x=home.home.left+45,y=home.home.floor-1,food=0,water=0,phase=0,alive=true}
home.content.flora={plant,dark};home.workers[1].x,home.workers[1].y=plant.x-4,plant.y;home.workers[1].directive={x=plant.x-4,y=plant.y}
local function at(tick,subject,expectEvidence)
 c.tick=tick;for _,site in ipairs(c.sites) do site.world.tick=tick end
 W.put(home,subject.x+1,subject.y,M.STEAM);Visibility.update(home,{campaign=c,siteId=1});Ecology.step(home,{campaign=c,siteId=1})
 assert(W.get(home,subject.x+1,subject.y)==M.WATER,'Controlled steam did not execute glass-reed mutation')
 local record;for _,candidate in ipairs(home.workers[1].frontier.knowledge.observations) do if candidate.source.id==subject.id then record=candidate end end
 if expectEvidence then assert(record and #record.effects>0,'Lit effect did not provide P05 evidence') else assert(not record or #record.effects==0,'Dark effect supplied P05 evidence') end
end
at(20,dark,false);at(40,plant,true);at(60,plant,true)
require('src.jobs').release(home,home.workers[1],true);home.workers[1].directive=nil;home.workers[1].thinkAt=home.tick
W.stack(home,'stone',8,home.home.left+30,home.home.floor-1);W.stack(home,'metal',8,home.home.left+31,home.home.floor-1);W.stack(home,'food',4,home.home.left+12,home.home.floor-1)
local h=History.new(c);local function advance(n) for _=1,n do assert(h:advance()) end end
local function world(id) return h.live.sites[id].world end
local function worker(id,index) return world(id).workers[index] end
local function queue(payload) assert(h:queue({scope='site',siteId=1,payload=payload})) end
local function field(kind,a,target,n) queue({type='field',kind=kind,target=target,worker=a.id,priority=3});advance(n) end
local torchGX,torchGY=W.tile(world(1),world(1).home.left+20,world(1).home.floor-1)
queue({type='order',kind='build',build='torch',gx=torchGX,gy=torchGY,priority=3,worker=0})
local expert=worker(1,1);local expertId=expert.personId;plant=world(1).content.flora[1]
field('survey',expert,plant.id,160);field('study',expert,plant.id,420)
advance(520);assert(require('src.structures').torchCount(world(1))==1,'Real torch construction did not complete')
expert=worker(1,1);assert(Knowledge.supports(expert,'flora','filter'),'Lit P05 study did not complete')
local schoolGX,schoolGY=W.tile(world(1),world(1).home.left+28,world(1).home.floor-1)
queue({type='order',kind='build',build='field_school',gx=schoolGX,gy=schoolGY,priority=3,worker=0});advance(900)
local slot=W.slot(world(1),schoolGX,schoolGY);local school=assert(world(1).structures[slot],'Real field-school construction did not complete')
local function policy(mode,topic)
 school=assert(world(1).structures[slot]);queue({type='school_policy',slot=slot,schoolId=school.education.id,expectedPolicyRevision=school.education.policyRevision,enabled=true,mode=mode,topicId=topic,topicVersion=1,priority=3})
end
policy('record','identify/flora/filter/v1');advance(600);policy('teach','identify/flora/filter/v1');advance(1450)
local pupil=worker(1,2);assert(Knowledge.identified(pupil,'flora','filter'),'2x4 school pair did not complete a lesson')
expert=worker(1,1);assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={expertId},cargo={food=2,metal=2}}));advance(900)
local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(650)
manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest);assert(ready,why)
assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));local departure=h.live.tick;advance(400)
local arrived;for _,a in ipairs(world(2).workers) do if a.personId==expertId then arrived=a end end
assert(arrived and Knowledge.supports(arrived,'flora','filter'),'Flight lost personal P05 knowledge')
local arrival=h.live.tick;local saved=h:saveText();local restored=History.fromText(saved);advance(math.max(0,ticks-h.live.tick));for _=restored.live.tick+1,ticks do assert(restored:advance()) end
assert(Codec.encode(h.live)==Codec.encode(restored.live),'G01 save/reload continuation diverged')
local checked,reason=h:verifyReplay(ticks+1000);assert(checked,reason)
local encoded=h:saveText();local w=world(1);local visible,explored=0,0
for y,row in ipairs(w.visibility.rows) do for x=1,#row do if row:byte(x)>0 then explored=explored+1 end end end
for _ in pairs(Visibility.derive(w,{campaign=h.live,siteId=1}).current) do visible=visible+1 end
print(string.format('PASS G01 soak: seed=%d tick=%d dark=20 lit=40,60 torch=%d school=%d departure/arrival=%d/%d visible/explored=%d/%d bytes=%d checkpoints=%d',seed,h.live.tick,require('src.structures').torchCount(w),school.education.id,departure,arrival,visible,explored,#encoded,#h.checkpoints))
print(string.format('TRACE people expert=%d fact=%s pupil=%d identity=%s body=%s visibility=%s.',expertId,'operational/flora/filter/steam-to-water/v1',pupil.personId,tostring(Knowledge.identified(pupil,'flora','filter')),require('src.body').profile(w).id,tostring(w.frontier.visibility)))
