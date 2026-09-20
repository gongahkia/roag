-- CPU-only diagnostic for COS-P02.  It does not write saves or estimate FPS.
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Sim=require('src.sim')
local Codec=require('src.campaign_codec')
local ticks=tonumber(arg and arg[1]) or 2000
assert(ticks==math.floor(ticks) and ticks>0 and ticks<=100000,'ticks must be 1..100000')
local options={preset='frontier',mode='challenge',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3}
local started=os.clock();local campaign=Campaign.newRegion(12345,options);local generation=os.clock()-started
print('maps,generation_cpu_s,whole_tick_cpu_s,per_site_tick_cpu_s,checkpoint_cpu_s,encoded_bytes,lua_heap_mib')
for count=1,3 do
 local worlds={};for i=1,count do worlds[i]=Campaign.clone(campaign).sites[i].world end
 local begin=os.clock()
 for _=1,ticks do
  for _,world in ipairs(worlds) do Sim.begin(world) end
  for _,world in ipairs(worlds) do Sim.body(world) end
 end
 local elapsed=os.clock()-begin;local checkpoint=0;local bytes=0
 if count==3 then
  local history=History.new(Campaign.clone(campaign));local at=os.clock()
  for _=1,history.checkpointEvery do history:advance() end
  checkpoint=os.clock()-at;bytes=#Codec.encode(history:bundle())
 end
 print(string.format('%d,%.6f,%.6f,%.9f,%.6f,%d,%.3f',count,generation,elapsed,elapsed/(ticks*count),checkpoint,bytes,collectgarbage('count')/1024))
end
print('Method: os.clock CPU time; one, two, and three-map rows are an isolated fixture, not a production schema variant. Generation is timed once; no GPU FPS or RSS is measured.')
print('PASS P02-J diagnostic completed; timings are host-specific observations, not a portability assertion.')
