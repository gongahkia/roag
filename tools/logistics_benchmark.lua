-- CPU-only COS-P03 diagnostic.  It neither writes saves nor estimates FPS.
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Sim=require('src.sim')
local Codec=require('src.campaign_codec')
local U=require('src.util')
local ticks=tonumber(arg and arg[1]) or 2000
U.integer(ticks,'logistics benchmark ticks',100,100000)
local base={preset='frontier',mode='challenge',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3}
local function options(logistics)
 local out=U.copy(base);out.logistics=logistics;return out
end
local started=os.clock();local campaign=Campaign.newRegion(12345,options(true));local generation=os.clock()-started
print('maps,generation_cpu_s,whole_tick_cpu_s,per_site_tick_cpu_s,checkpoint_cpu_s,encoded_bytes,lua_heap_mib')
for count=1,3 do
 local worlds={};for i=1,count do worlds[i]=Campaign.clone(campaign).sites[i].world end
 local begin=os.clock()
 for _=1,ticks do for _,world in ipairs(worlds) do Sim.begin(world) end;for _,world in ipairs(worlds) do Sim.body(world) end end
 local elapsed=os.clock()-begin;local checkpoint,bytes=0,0
 if count==3 then
  local history=History.new(Campaign.clone(campaign));local at=os.clock();for _=1,history.checkpointEvery do history:advance() end
  checkpoint=os.clock()-at;bytes=#Codec.encode(history:bundle())
 end
 print(string.format('%d,%.6f,%.6f,%.9f,%.6f,%d,%.3f',count,generation,elapsed,elapsed/(ticks*count),checkpoint,bytes,collectgarbage('count')/1024))
end
local function median(values)
 local ordered=U.copy(values);table.sort(ordered);return ordered[math.floor((#ordered+1)/2)]
end
local function trace(logistics)
 local c=Campaign.newRegion(12345,options(logistics));local start=os.clock()
 for _=1,ticks do Campaign.step(c) end
 return os.clock()-start
end
-- Warm up both code paths, then alternate seven comparable, no-command traces.
trace(false);trace(true);local p02,p03={},{}
for sample=1,7 do
 if sample%2==1 then p02[sample]=trace(false);p03[sample]=trace(true)
 else p03[sample]=trace(true);p02[sample]=trace(false) end
end
local old,new=median(p02),median(p03);local delta=(new/old-1)*100
print(string.format('three-map median CPU: P02 %.6f s; P03 empty-logistics %.6f s; delta %.1f%%.',old,new,delta))
print('P02 samples: '..table.concat(p02,', '));print('P03 samples: '..table.concat(p03,', '))
if delta>15 then print('INVESTIGATE: comparable empty-logistics median exceeded the 15% diagnostic guard.') end
print('Method: os.clock CPU time; one/two-map rows are isolated world subsets, not a production region schema. No GPU FPS, filesystem replacement, or OS RSS is measured.')
print('PASS P03-J diagnostic completed; timings are host-specific observations, not a portability assertion.')
