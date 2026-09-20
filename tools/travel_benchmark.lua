-- CPU-only COS-P04 diagnostic.  It measures deterministic headless work only;
-- it does not open a window, write a save, estimate FPS, or report OS RSS.
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local U=require('src.util')
local ticks=tonumber(arg and arg[1]) or 200
local samples=tonumber(arg and arg[2]) or 3
U.integer(ticks,'travel benchmark transit ticks',50,10000);U.integer(samples,'travel benchmark samples',1,15)
local base={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true}
local function quiet(c)
 for _,site in ipairs(c.sites) do local content=site.world.content;content.flora={};content.fauna={};content.sites={};content.ruins={};content.signals={};content.discoveries={};content.observed={} end
 return c
end
local function command(kind,fields) local out={scope='campaign',type=kind};for key,value in pairs(fields) do out[key]=value end;return out end
local function advance(h,n) for _=1,n do assert(h:advance()) end end
local function make(travel)
 local options=U.copy(base);options.travel=travel
 return quiet(Campaign.newRegion(81234,options))
end
local function launch(h)
 local person=h.live.sites[1].world.workers[1].personId
 assert(h:queue(command('prepare_expedition',{sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={person},cargo={food=3,metal=2}})))
 advance(h,460);local manifest=assert(Logistics.manifest(h.live,1));assert(h:queue(command('assemble_expedition',{sourceSiteId=1,craftId=1,manifestId=manifest.id})))
 advance(h,180);manifest=assert(Logistics.manifest(h.live,1));local ready,why=Logistics.readiness(h.live,manifest);assert(ready,why)
 assert(h:queue(command('launch_expedition',{sourceSiteId=1,craftId=1,manifestId=manifest.id,expectedManifestRevision=manifest.revision})));advance(h,1)
end
print('sample,generation_cpu_s,three_local_tick_cpu_s,prepare_assemble_cpu_s,transit_tick_cpu_s,landing_cpu_s,serialize_cpu_s,encoded_bytes,lua_heap_mib')
for sample=1,samples do
 collectgarbage('collect');local at=os.clock();local p03=make(false);local generation=os.clock()-at
 at=os.clock();for _=1,ticks do Campaign.step(p03) end;local localTicks=os.clock()-at
 at=os.clock();local h=History.new(make(true));launch(h);local preparation=os.clock()-at
 at=os.clock();advance(h,ticks);local transit=os.clock()-at
 local craft=Logistics.craft(h.live,1);craft.journey.remainingTicks=1;at=os.clock();advance(h,1);local landing=os.clock()-at
 at=os.clock();local encoded=h:saveText();local serialization=os.clock()-at
 print(string.format('%d,%.6f,%.9f,%.6f,%.9f,%.6f,%.6f,%d,%.3f',sample,generation,localTicks/ticks,preparation,transit/ticks,landing,serialization,#encoded,collectgarbage('count')/1024))
end
print('Method: os.clock CPU seconds; the local row is three feature-off P03 maps, and the transit row is three maps plus one active craft. Samples are host-specific diagnostics, not an overhead guarantee, FPS, GPU measurement, or RSS measurement.')
print('PASS COS-P04 CPU diagnostic completed.')
