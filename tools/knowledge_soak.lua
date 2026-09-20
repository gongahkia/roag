-- Reproducible P05 trace: real ecology receipts, survey/study jobs, one actual
-- production-duration flight, save/restore, replay verification, then a bounded
-- 10,000-tick continuation.  It uses an explicit controlled ecology fixture.
local W=require('src.world')
local M=require('src.materials')
local Ecology=require('src.ecology')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local Knowledge=require('src.knowledge')
local Codec=require('src.campaign_codec')

local seed=tonumber(arg[1]) or 9051
local ticks=tonumber(arg[2]) or 10000
assert(seed and seed%1==0 and seed>=1 and seed<=2147483646,'Usage: luajit tools/knowledge_soak.lua [seed] [ticks]')
assert(ticks and ticks%1==0 and ticks>=1000 and ticks<=100000,'Ticks must be 1000..100000')
local c=Campaign.newRegion(seed,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true})
local home=c.sites[1].world
home.content.flora={};home.content.fauna={};home.content.sites={};home.content.signals={};home.content.discoveries={};home.content.observed={}
local plant={id=W.id(home),kind='filter',x=home.home.left+25,y=home.home.floor-1,food=0,water=0,phase=0,alive=true}
home.content.flora={plant};home.workers[1].x,home.workers[1].y=plant.x-4,plant.y;home.workers[1].directive={x=plant.x-4,y=plant.y}
local function effect(tick)
 c.tick=tick;for _,site in ipairs(c.sites) do site.world.tick=tick end
 W.put(home,plant.x+1,plant.y,M.STEAM);Ecology.step(home,{campaign=c,siteId=1});Campaign.validate(c)
 assert(W.get(home,plant.x+1,plant.y)==M.WATER,'Controlled steam did not reach the real glass-reed mutation')
end
effect(20);effect(40)
require('src.jobs').release(home,home.workers[1],true);home.workers[1].directive=nil;home.workers[1].thinkAt=home.tick
local h=History.new(c)
local function advance(n) for _=1,n do assert(h:advance()) end end
local function field(kind,worker,target,n)
 assert(h:queue({scope='site',siteId=1,payload={type='field',kind=kind,target=target,worker=worker.id,priority=3}}));advance(n)
end
local expert=h.live.sites[1].world.workers[1];local personId=expert.personId;plant=h.live.sites[1].world.content.flora[1]
field('survey',expert,plant.id,140);field('study',expert,plant.id,330)
assert(Knowledge.supports(expert,'flora','filter'),'Study did not complete from actual field actions')
local studyTick=h.live.tick;local partial=h:saveText();h=History.fromText(partial);expert=h.live.sites[1].world.workers[1]
assert(h:queue({scope='campaign',type='prepare_expedition',sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={personId},cargo={food=2,metal=2}}));advance(520)
local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue({scope='campaign',type='assemble_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id}));advance(220)
manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest);assert(ready,why)
assert(h:queue({scope='campaign',type='launch_expedition',sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision}));advance(400)
local arrived
for _,worker in ipairs(h.live.sites[2].world.workers) do if worker.personId==personId then arrived=worker;break end end
assert(arrived and Knowledge.supports(arrived,'flora','filter'),'Flight did not retain the learned fact')
local foundingTick=h.live.tick;local mid=h:saveText();h=History.fromText(mid)
while h.live.tick<ticks do assert(h:advance()) end
local verified,why=h:verifyReplay(ticks+1000);assert(verified,why)
local encoded=h:saveText();local facts=0
for _,site in ipairs(h.live.sites) do for _,worker in ipairs(site.world.workers) do if worker.frontier then facts=facts+#worker.frontier.knowledge.facts end end end
print(string.format('PASS knowledge soak: seed %d tick %d evidence 20,40 study %d founding %d facts %d checkpoints %d bytes %d',seed,h.live.tick,studyTick,foundingTick,facts,#h.checkpoints,#encoded))
