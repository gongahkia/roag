-- COS-P06 bounded integration trace.  It uses real ecology/fieldwork, normal
-- construction, school attendance, a P04 flight, and record study.
local W=require('src.world')
local M=require('src.materials')
local Ecology=require('src.ecology')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Codec=require('src.campaign_codec')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')

local seed=tonumber(arg[1]) or 9051
local ticks=tonumber(arg[2]) or 20000
local fixtureDir=arg[3]
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/education_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=20000 and ticks<=100000,'Ticks must be 20000..100000')
if fixtureDir then
 local root=os.getenv('COSMONAUTS_PLAYTEST_ROOT')
 assert(root and fixtureDir==root..'/fixtures','Fixture output must be the marked playtest root fixtures directory')
 local marker=assert(io.open(root..'/.cosmonauts-playtest-root','rb'),'Missing playtest root marker')
 local text=marker:read('*a');marker:close();assert(text:find('format=cosmonauts-playtest-root-v1',1,true),'Invalid playtest root marker')
end
local function fixture(name,text)
 if not fixtureDir then return end
 local path=fixtureDir..'/'..name..'.campaign';local prior=io.open(path,'rb')
 assert(not prior,'Refusing to overwrite fixture '..path)
 local f=assert(io.open(path,'wb'));assert(f:write(text));assert(f:close())
end
local options={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,crew=3,logistics=true,travel=true,knowledge=true,education=true}
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
-- These are explicit fixture supplies, placed before history begins so replay
-- re-executes the same campaign state rather than receiving a later grant.
W.stack(home,'stone',4,home.home.left+30,home.home.floor-1)
W.stack(home,'metal',2,home.home.left+31,home.home.floor-1)
W.stack(home,'food',4,home.home.left+12,home.home.floor-1)
W.stack(home,'metal',4,home.home.left+13,home.home.floor-1)
local h=History.new(c)
local initialFixture=h:saveText()
local function advance(n) for _=1,n do assert(h:advance()) end end
local function world(siteId) return h.live.sites[siteId].world end
local function worker(siteId,index) return world(siteId).workers[index] end
local function personAt(siteId,personId)
 for _,candidate in ipairs(world(siteId).workers) do if candidate.personId==personId then return candidate end end
end
local function observationCount(person)
 return #person.frontier.knowledge.observations
end
local function queue(payload) assert(h:queue({scope='site',siteId=1,payload=payload})) end
local function field(kind,actor,target,n)
 queue({type='field',kind=kind,target=target,worker=actor.id,priority=3});advance(n)
end
local expert=worker(1,1);local expertId=expert.personId;plant=world(1).content.flora[1]
field('survey',expert,plant.id,160);field('study',expert,plant.id,380)
assert(Knowledge.supports(worker(1,1),'flora','filter'),'Real P05 study did not produce the operational fact')
local studyTick=h.live.tick
local gx,gy=W.tile(world(1),world(1).home.left+28,world(1).home.floor-1)
queue({type='order',kind='build',build='field_school',gx=gx,gy=gy,priority=3,worker=0});advance(900)
local slot=W.slot(world(1),gx,gy);local school=assert(world(1).structures[slot],'Real construction did not install a field school')
local function schoolPolicy(enabled,mode,topic,version)
 school=assert(world(1).structures[slot])
 queue({type='school_policy',slot=slot,schoolId=school.education.id,expectedPolicyRevision=school.education.policyRevision,enabled=enabled,mode=mode,topicId=topic or false,topicVersion=version or 0,priority=3})
end
local identity='identify/flora/filter/v1';local operation='operational/flora/filter/steam-to-water/v1'
schoolPolicy(true,'record',identity,1);advance(60);local partialCopy=h:saveText();advance(460);school=world(1).structures[slot];assert(#school.education.records==1,'Identity record did not complete')
schoolPolicy(true,'record',operation,1);advance(520);school=world(1).structures[slot];assert(#school.education.records==2,'Operational record did not complete')
local b,cPerson=worker(1,2),worker(1,3)
local bId,cId=b.personId,cPerson.personId
queue({type='rally',worker=cPerson.id,x=cPerson.x,y=cPerson.y});advance(1)
schoolPolicy(true,'teach',identity,1);advance(200);local partialLesson=h:saveText();advance(1300);b=worker(1,2);assert(Knowledge.fact(b,identity),'B did not learn the identity through attended teaching')
schoolPolicy(true,'teach',operation,1);advance(1500);b=worker(1,2);assert(Knowledge.fact(b,operation),'B did not learn the operational fact through attended teaching')
local bFact=Knowledge.fact(b,operation)
expert=assert(personAt(1,expertId));local expertFieldworkXP=expert.frontier.education.fieldworkXP;local expertTeachingXP=expert.frontier.education.teachingXP
assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={expertId},cargo={food=2,metal=2}}));advance(900)
local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(600)
manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest);assert(ready,why)
assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(1)
local departureTick=h.live.tick;advance(199)
local transit=h:saveText();h=History.fromText(transit);advance(200)
local arrived=assert(personAt(2,expertId));assert(Knowledge.fact(arrived,operation) and arrived.frontier.education.fieldworkXP==expertFieldworkXP and arrived.frontier.education.teachingXP==expertTeachingXP,'Travel did not retain A knowledge and expertise')
local arrivalTick=h.live.tick;local homeB=assert(personAt(1,bId));assert(Knowledge.fact(homeB,operation) and Knowledge.fact(homeB,operation).tick==bFact.tick,'A flight changed B knowledge at home')
assert(world(1).structures[slot] and #world(1).structures[slot].education.records==2,'Home records travelled with A')
queue({type='releaserally',worker=assert(personAt(1,cId)).id});advance(1)
schoolPolicy(true,'study',identity,1);advance(1500);cPerson=assert(personAt(1,cId));assert(Knowledge.fact(cPerson,identity),'C did not learn identity from the installed record')
schoolPolicy(true,'study',operation,1);advance(80)
local partialRecordStudy=h:saveText();local restored=History.fromText(partialRecordStudy);local cObservationCount=observationCount(assert(personAt(1,cId)))
advance(1420)
for _=1,1420 do assert(restored:advance()) end
assert(Codec.encode(h.live)==Codec.encode(restored.live),'Record-study save/reload continuation diverged')
cPerson=assert(personAt(1,cId));local cFact=assert(Knowledge.fact(cPerson,operation),'C did not learn operation from the installed record')
assert(cFact.method=='record' and observationCount(cPerson)==cObservationCount,'Record study copied eyewitness state or used wrong provenance')
local checkpoints={copy=#partialCopy,lesson=#partialLesson,transit=#transit,recordStudy=#partialRecordStudy}
while h.live.tick<ticks do advance(1) end
local verified,reason=h:verifyReplay(30000);assert(verified,reason)
local encoded=h:saveText();local homeSchool=world(1).structures[slot]
fixture('initial',initialFixture)
fixture('partial-copy',partialCopy)
fixture('partial-lesson',partialLesson)
fixture('transit',transit)
fixture('partial-record-study',partialRecordStudy)
print(string.format('PASS COS-P06 education soak: seed=%d tick=%d study=%d school=%d records=%d checkpoints=%d,%d,%d,%d bytes=%d heapKiB=%.1f',seed,h.live.tick,studyTick,homeSchool.education.id,#homeSchool.education.records,checkpoints.copy,checkpoints.lesson,checkpoints.transit,checkpoints.recordStudy,#encoded,collectgarbage('count')))
print(string.format('TRACE A person=%d departure/arrival=%d/%d; B person=%d fact=%s tick=%d method=%s; C person=%d fact=%s tick=%d method=%s.',expertId,departureTick,arrivalTick,b.personId,bFact.id,bFact.tick,bFact.method,cPerson.personId,cFact.id,cFact.tick,cFact.method))
