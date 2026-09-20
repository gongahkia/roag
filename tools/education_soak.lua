-- COS-P06 bounded integration trace.  It uses real ecology/fieldwork, normal
-- construction, school attendance, a P04 flight, and record study.
local W=require('src.world')
local N=require('src.nav')
local M=require('src.materials')
local Ecology=require('src.ecology')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')

local seed=tonumber(arg[1]) or 9051
local ticks=tonumber(arg[2]) or 20000
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/education_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=20000 and ticks<=100000,'Ticks must be 20000..100000')
local options={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true}
local c=Campaign.newRegion(seed,options)
local home=c.sites[1].world
home.content.flora={};home.content.fauna={};home.content.sites={};home.content.ruins={};home.content.signals={};home.content.discoveries={};home.content.observed={}
local plant={id=W.id(home),kind='filter',x=home.home.left+25,y=home.home.floor-1,food=0,water=0,phase=0,alive=true}
home.content.flora={plant};home.workers[1].x,home.workers[1].y=plant.x-4,plant.y;home.workers[1].directive={x=plant.x-4,y=plant.y}
local function effect(tick)
 c.tick=tick;for _,site in ipairs(c.sites) do site.world.tick=tick end
 W.put(home,plant.x+1,plant.y,M.STEAM);Ecology.step(home,{campaign=c,siteId=1});Campaign.validate(c)
 assert(W.get(home,plant.x+1,plant.y)==M.WATER,'Controlled steam did not execute the real glass-reed mutation')
end
effect(20);effect(40)
require('src.jobs').release(home,home.workers[1],true);home.workers[1].directive=nil;home.workers[1].thinkAt=home.tick
local h=History.new(c)
local function advance(n) for _=1,n do assert(h:advance()) end end
local function world(siteId) return h.live.sites[siteId].world end
local function worker(siteId,index) return world(siteId).workers[index] end
local function queue(payload) assert(h:queue({scope='site',siteId=1,payload=payload})) end
local function field(kind,actor,target,n)
 queue({type='field',kind=kind,target=target,worker=actor.id,priority=3});advance(n)
end
local expert=worker(1,1);local expertId=expert.personId;plant=world(1).content.flora[1]
field('survey',expert,plant.id,160);field('study',expert,plant.id,380)
assert(Knowledge.supports(worker(1,1),'flora','filter'),'Real P05 study did not produce the operational fact')
local studyTick=h.live.tick
local gx,gy=W.tile(world(1),world(1).home.left+28,world(1).home.floor-1)
W.stack(world(1),'stone',4,world(1).home.left+30,world(1).home.floor-1);W.stack(world(1),'metal',2,world(1).home.left+31,world(1).home.floor-1)
queue({type='order',kind='build',build='field_school',gx=gx,gy=gy,priority=3,worker=0});advance(900)
local slot=W.slot(world(1),gx,gy);local school=assert(world(1).structures[slot],'Real construction did not install a field school')
local function schoolPolicy(enabled,mode,topic,version)
 school=assert(world(1).structures[slot])
 queue({type='school_policy',slot=slot,schoolId=school.education.id,expectedPolicyRevision=school.education.policyRevision,enabled=enabled,mode=mode,topicId=topic or false,topicVersion=version or 0,priority=3})
end
local identity='identify/flora/filter/v1';local operation='operational/flora/filter/steam-to-water/v1'
schoolPolicy(true,'record',identity,1);advance(520);school=world(1).structures[slot];assert(#school.education.records==1,'Identity record did not complete')
local partialCopy=h:saveText()
schoolPolicy(true,'record',operation,1);advance(520);school=world(1).structures[slot];assert(#school.education.records==2,'Operational record did not complete')
local b,cPerson=worker(1,2),worker(1,3)
queue({type='rally',worker=cPerson.id,x=cPerson.x,y=cPerson.y});advance(1)
schoolPolicy(true,'teach',identity,1);advance(1500);b=worker(1,2);assert(Knowledge.fact(b,identity),'B did not learn the identity through attended teaching')
local partialLesson=h:saveText()
schoolPolicy(true,'teach',operation,1);advance(1500);b=worker(1,2);assert(Knowledge.fact(b,operation),'B did not learn the operational fact through attended teaching')
local bFact=Knowledge.fact(b,operation)
W.stack(world(1),'food',4,world(1).home.left+12,world(1).home.floor-1);W.stack(world(1),'metal',4,world(1).home.left+13,world(1).home.floor-1)
assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={expertId},cargo={food=2,metal=2}}));advance(900)
local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(600)
manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(220)
manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest)
if not ready then local craft=Logistics.craft(h.live,1);local pose=manifest.assembly[1];local a=worker(1,1);local flood=N.flood(world(1),a.x,a.y,true);local path=N.closest(world(1),flood,function(x,y)return pose and x==pose.x and y==pose.y end);local claim=pose and world(1).workClaims and world(1).workClaims[W.index(world(1),pose.x,pose.y)];require('src.jobs').plan(world(1),a,{campaign=h.live,siteId=1});error(why..' / cargo='..tostring(craft.cargo.food)..','..tostring(craft.cargo.metal)..' assembly='..#manifest.assembly..' jobs='..#world(1).jobs..' stand='..tostring(pose and N.stand(world(1),pose.x,pose.y,true))..' reachable='..tostring(pose and flood.parent[W.index(world(1),pose.x,pose.y)]~=nil)..' path='..tostring(path and #path)..' claim='..tostring(claim)..' A='..a.x..','..a.y..' d='..tostring(a.directive and a.directive.kind)..' t='..tostring(a.task and a.task.kind)..' think='..a.thinkAt..' tick='..world(1).tick..' '..a.status..'/'..a.reason) end
assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(200)
local transit=h:saveText();h=History.fromText(transit);advance(200)
local arrived=worker(2,1);assert(arrived and arrived.personId==expertId and Knowledge.fact(arrived,operation),'Travel did not retain A knowledge and identity')
local arrivalTick=h.live.tick;assert(Knowledge.fact(worker(1,2),operation) and Knowledge.fact(worker(1,2),operation).tick==bFact.tick,'A flight changed B knowledge at home')
assert(world(1).structures[slot] and #world(1).structures[slot].education.records==2,'Home records travelled with A')
queue({type='releaserally',worker=worker(1,3).id});advance(1)
schoolPolicy(true,'study',identity,1);advance(1500);cPerson=worker(1,3);assert(Knowledge.fact(cPerson,identity),'C did not learn identity from the installed record')
local partialRecordStudy=h:saveText()
schoolPolicy(true,'study',operation,1);advance(1500);cPerson=worker(1,3);local cFact=assert(Knowledge.fact(cPerson,operation),'C did not learn operation from the installed record')
assert(cFact.method=='record' and #cPerson.frontier.knowledge.observations==0,'Record study copied eyewitness state or wrong provenance')
local checkpoints={copy=#partialCopy,lesson=#partialLesson,transit=#transit,recordStudy=#partialRecordStudy}
local restored=History.fromText(partialRecordStudy);advance(40);advance(0)
while h.live.tick<ticks do advance(1) end
local verified,reason=h:verifyReplay(30000);assert(verified,reason)
local encoded=h:saveText();local homeSchool=world(1).structures[slot]
print(string.format('PASS COS-P06 education soak: seed=%d tick=%d study=%d school=%d records=%d checkpoints=%d,%d,%d,%d bytes=%d heapKiB=%.1f',seed,h.live.tick,studyTick,homeSchool.education.id,#homeSchool.education.records,checkpoints.copy,checkpoints.lesson,checkpoints.transit,checkpoints.recordStudy,#encoded,collectgarbage('count')))
print(string.format('TRACE A person=%d departure/arrival=%d/%d; B person=%d fact=%s tick=%d method=%s; C person=%d fact=%s tick=%d method=%s.',expertId,arrivalTick-399,arrivalTick,b.personId,bFact.id,bFact.tick,bFact.method,cPerson.personId,cFact.id,cFact.tick,cFact.method))
