-- COS-G02 bounded 15,000-tick integrated trace.  The fixture arranges only a
-- clear shaft, metal and a known specimen; tools, rope, bench, charge and the
-- journey are all created through normal commands/jobs/campaign history.
local W=require('src.world')
local M=require('src.materials')
local Ecology=require('src.ecology')
local Visibility=require('src.visibility')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')
local Equipment=require('src.equipment')
local Codec=require('src.campaign_codec')

local seed=tonumber(arg[1]) or 9602;local ticks=tonumber(arg[2]) or 15000
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/g02_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=15000 and ticks<=100000,'Ticks must be 15000..100000')
-- An explicit third argument lets COS-G03 reuse this real integrated route
-- without changing the normal G02 compatibility trace.
local psychology=arg[3]=='psychology'
local options={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=psychology}
local c=Campaign.newRegion(seed,options);local home=c.sites[1].world
home.content.flora={};home.content.fauna={};home.content.sites={};home.content.ruins={};home.content.signals={};home.content.discoveries={};home.content.observed={}
local sx=((home.home.left+3-1)%4==0 and home.home.left+3 or (math.floor((home.home.left+3-1)/4)*4+1));local sy=home.home.floor
local expert=home.workers[1];expert.x,expert.y=sx+1,sy
for y=sy-3,sy do W.put(home,sx,y,M.AIR);W.put(home,sx+1,y,M.AIR) end
W.put(home,sx+1,sy+1,M.ROCK);W.put(home,sx+2,sy+1,M.ROCK)
local pickX,pickY=sx+13,sy-1;W.put(home,pickX,pickY,M.ROCK)
local plant={id=W.id(home),kind='filter',x=sx+6,y=sy-1,food=0,water=0,phase=0,alive=true}
local dark={id=W.id(home),kind='filter',x=sx+35,y=sy-1,food=0,water=0,phase=0,alive=true}
home.content.flora={plant,dark}
-- The coil is still an ordinary starter item; the controlled start merely puts
-- it at a reachable shaft lip instead of in an arbitrary generated pile.
Equipment.drop(c,c.equipment.items[3],1,sx+1,sy)
local function effect(tick,subject,expect)
 c.tick=tick;for _,site in ipairs(c.sites) do site.world.tick=tick end
 W.put(home,subject.x+1,subject.y,M.STEAM);Visibility.update(home,{campaign=c,siteId=1});Ecology.step(home,{campaign=c,siteId=1})
 local found;for _,o in ipairs(home.workers[1].frontier.knowledge.observations) do if o.source.id==subject.id then found=o end end
 local has=found and #found.effects>0 or false
 assert(has==expect,'G02 light/P05 witness boundary changed at '..tick)
end
effect(20,dark,false);effect(40,plant,true);effect(60,plant,true)
W.stack(home,'stone',40,home.home.left+20,sy);W.stack(home,'metal',40,home.home.left+21,sy);W.stack(home,'food',6,home.home.left+22,sy)
local h=History.new(c)
local function advance(n) for _=1,n do assert(h:advance()) end end
local function world(id) return h.live.sites[id].world end
local function worker(id,index) return world(id).workers[index] end
local function queue(payload) assert(h:queue({scope='site',siteId=1,payload=payload})) end
local function field(kind,a,target,n) queue({type='field',kind=kind,target=target,worker=a.id,priority=3});advance(n) end
local function buildSpot(kind)
 local w=world(1);local S=require('src.structures')
 for gy=2,w.rows-1 do for gx=2,w.cols-1 do
  local x,y=gx*4-2,gy*4-2
  if Visibility.currentlyVisible(w,x,y,{campaign=h.live,siteId=1}) and S.siteClear(w,gx,gy,kind) then return gx,gy end
 end end
 error('No visible supported build position for '..kind)
end

-- Rope uses normal campaign commands. The focused G02 safety case covers the
-- support-removal negative path; this trace mines a separate lit face so the
-- starter pick is acquired through ordinary scheduling.
local ropeGX,ropeGY=W.tile(world(1),sx,sy-3)
local shaftTorchGX,shaftTorchGY=ropeGX+2,ropeGY
queue({type='order',kind='build',build='torch',gx=shaftTorchGX,gy=shaftTorchGY,priority=3,worker=0});advance(500)
queue({type='place_rope',gx=ropeGX,gy=ropeGY,priority=3});advance(300)
assert(#world(1).ropes==1,'Real rope installation did not complete');local rope=world(1).ropes[1]
local digGX,digGY=W.tile(world(1),pickX,pickY);queue({type='order',kind='dig',gx=digGX,gy=digGY,priority=3,worker=0});advance(600)
local miner;for _,a in ipairs(world(1).workers) do if Equipment.pickFor(h.live,a) then miner=a end end
assert(miner and miner.fall==0,'Miner did not physically equip a starter pickaxe without a fall');local minerId=miner.personId

local benchGX,benchGY=buildSpot('tool_bench')
queue({type='order',kind='build',build='tool_bench',gx=benchGX,gy=benchGY,priority=3,worker=0});advance(700)
local bench=assert(world(1).structures[W.slot(world(1),benchGX,benchGY)],'Tool bench construction did not complete')
queue({type='fabricate',slot=W.slot(world(1),benchGX,benchGY),kind='rope_coil',priority=3});advance(500)
local coils=Equipment.forSite(h.live,1,'rope_coil','loose');assert(#coils>=3,'Tool bench did not fabricate a physical replacement coil')

local chargeGX,chargeGY=buildSpot('charge')
queue({type='order',kind='build',build='charge',gx=chargeGX,gy=chargeGY,priority=3,worker=0});advance(500)
local charge=world(1).structures[W.slot(world(1),chargeGX,chargeGY)]

local torchGX,torchGY=buildSpot('torch')
queue({type='order',kind='build',build='torch',gx=torchGX,gy=torchGY,priority=3,worker=0});advance(500)
expert=worker(1,1);plant=world(1).content.flora[1];field('survey',expert,plant.id,180);field('study',expert,plant.id,450)
assert(Knowledge.supports(worker(1,1),'flora','filter'),'G02 expert did not retain lit P05 field study')
local schoolGX,schoolGY=buildSpot('field_school')
queue({type='order',kind='build',build='field_school',gx=schoolGX,gy=schoolGY,priority=3,worker=0});advance(900)
local slot=W.slot(world(1),schoolGX,schoolGY);local school=assert(world(1).structures[slot],'School construction did not complete')
local function policy(mode,topic)
 school=assert(world(1).structures[slot]);queue({type='school_policy',slot=slot,schoolId=school.education.id,expectedPolicyRevision=school.education.policyRevision,enabled=true,mode=mode,topicId=topic,topicVersion=1,priority=3})
end
policy('record','identify/flora/filter/v1');advance(650);policy('teach','identify/flora/filter/v1');advance(3500)
local pupil=worker(1,2)
local pupilId=pupil.personId
assert(Knowledge.identified(pupil,'flora','filter'),string.format('2x4 school pair did not complete instruction (records=%d session=%s tuition=%d hunger=%d fatigue=%d)',#school.education.records,tostring(school.education.session and school.education.session.mode),#pupil.frontier.education.tuition,pupil.hunger,pupil.fatigue))
-- Release the continuous teaching policy before the demolition milestone so a
-- real local worker, rather than a stale paired session, can take the charge.
school=assert(world(1).structures[slot])
queue({type='school_policy',slot=slot,schoolId=school.education.id,expectedPolicyRevision=school.education.policyRevision,enabled=false,mode='teach',topicId='identify/flora/filter/v1',topicVersion=1,priority=3});advance(20)

-- A loose coil takes one real craft slot and uses the normal hauling route.
local cargoCoil=Equipment.forSite(h.live,1,'rope_coil','loose')[1]
if cargoCoil then queue({type='load_tool',equipmentId=cargoCoil.id,craftId=1,priority=3});advance(500);assert(cargoCoil.state=='craft','Loose tool did not become craft custody') end
expert=worker(1,1);local expertId=expert.personId
-- Leave the instructed pupil at home so an ordinary non-passenger can arm and
-- evacuate from the established charge after the expedition has assembled.
local passengers=expertId==minerId and {expertId} or {expertId,minerId}
assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers=passengers,cargo={food=2,metal=2}}));advance(900)
local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(700)
manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest);assert(ready,why)
-- Arm the established charge after the travelling expert/miner are assembled.
-- A released local worker performs the ordinary arming/evacuation route; its
-- fuse then continues at the unviewed home site during flight.
local travelling={};for _,id in ipairs(passengers) do travelling[id]=true end
local armer;for _,a in ipairs(world(1).workers) do if not travelling[a.personId] then armer=a;break end end
assert(armer,'Controlled trace needs one local charge armer')
queue({type='releaserally',worker=armer.id});advance(5)
queue({type='arm',slot=W.slot(world(1),chargeGX,chargeGY),priority=3,worker=0})
local armed=false
for _=1,180 do
 advance(1)
 if charge.fuseAt then armed=true;break end
end
local armReason='missing'
for _,job in ipairs(world(1).jobs) do if job.kind=='arm' then armReason=job.reason end end
local armWorkers={};for _,a in ipairs(world(1).workers) do armWorkers[#armWorkers+1]=string.format('%d:%s:%s:%s',a.personId,tostring(a.alive),a.status or '',a.reason or '') end
assert(armed,'Existing demolition charge did not arm through real field work: '..tostring(armReason)..' / '..table.concat(armWorkers,' | '))
assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));local departure=h.live.tick;advance(400)
local arrival,minerArrival;for _,a in ipairs(world(2).workers) do if a.personId==expertId then arrival=a end;if a.personId==minerId then minerArrival=a end end
assert(arrival and Knowledge.supports(arrival,'flora','filter'),'P05 fact did not survive travel');assert(minerArrival and Equipment.pickFor(h.live,minerArrival),'Equipped pickaxe did not follow the persistent person through P04 travel')
local saved=h:saveText();local restored=History.fromText(saved)
advance(math.max(0,ticks-h.live.tick));for _=restored.live.tick+1,ticks do assert(restored:advance()) end
assert(Codec.encode(h.live)==Codec.encode(restored.live),'G02 save/reload continuation diverged')
local checked,reason=h:verifyReplay(ticks+1000);assert(checked,reason)
local final=world(1);local equipment=h.live.equipment.items;local panics=0;for _,a in ipairs(final.workers) do if a.panic then panics=panics+1 end end
print(string.format('PASS G02 soak: seed=%d tick=%d rope=%d/%d bench=%d chargeArmed=%s pick=%d cargoTools=%d departure/arrival=%d/%d bytes=%d checkpoints=%d',seed,h.live.tick,rope.id,rope.length,bench.id,tostring(armed),Equipment.pickFor(h.live,minerArrival).id,Equipment.craftCount(h.live,1),departure,h.live.tick,#h:saveText(),#h.checkpoints))
print(string.format('TRACE expert=%d miner=%d pupil=%d fact=operational/flora/filter/steam-to-water/v1 stress=%d panic=%d body=2x4 equipment=%d safe_excavation=%d psychology=%s.',expertId,minerId,pupilId,arrival.stress or 0,panics,h.live.features.equipment,h.live.features.safe_excavation,tostring(h.live.features.psychology==1)))
