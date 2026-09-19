local G=require('src.generate')
local H=require('src.history')
local Sim=require('src.sim')
local Metrics=require('src.metrics')
local U=require('src.util')
local Codec=require('src.codec')
local C=require('config')
local B={}
-- Cooperative scheduling yields outside timers; it is not multithreading.
function B.create(clock,options)
 options=options or {}
 return coroutine.create(function()
  local rows={U.csv({'preset','seed','width','height','scenario','generation_ms','mean_tick_ms','p95_tick_ms',
    'materials_and_blasts_ms_mean','ecology_ms_mean','colonists_ms_mean','retained_Lua_KiB','checkpoint_bytes','alive','food',
    'water_residual','mineral_residual','food_residual'})}
  for _,preset in ipairs(options.presets or G.presets) do
   for _,seed in ipairs(options.seeds or {1,7,12345}) do
    for _,scenario in ipairs(options.scenarios or {'arrival','construction'}) do
     -- Discarded warm-up. Real JIT studies should verify warm-up sufficiency.
     local warm=G.make(seed,preset,'challenge',128,80)
     for _=1,20 do Sim.step(warm) end warm=nil;collectgarbage('collect')
     local before=collectgarbage('count');local start=clock()
     local w=G.make(seed,preset,'challenge',128,80)
     local gen=(clock()-start)*1000;w.baseline=Metrics.measure(w)
     local ticks,mat,ecology,colonists={},{},{},{}
     for tick=1,(options.ticks or 300) do
      local cmd
      if tick==1 and scenario=='construction' then
       local gy=(w.home.floor-1)/4
       local startBlock=((w.home.left or 9)-1)/4
       cmd={{type='order',kind='build',build='farm',gx=startBlock+7,gy=gy},
            {type='order',kind='build',build='bed',gx=startBlock+5,gy=gy}}
      end
      local t=clock();local p=Sim.step(w,cmd,clock);ticks[#ticks+1]=(clock()-t)*1000
      mat[#mat+1]=p.materials*1000;ecology[#ecology+1]=p.ecology*1000;colonists[#colonists+1]=p.colonists*1000
      if tick%10==0 then coroutine.yield(preset..' / '..seed..' / '..scenario..' / '..tick) end
     end
     collectgarbage('collect');local memory=collectgarbage('count')-before
     local metrics=Metrics.measure(w)
     local bytes=#Codec.encode(w)
     rows[#rows+1]=U.csv({preset,seed,128,80,scenario,gen,U.mean(ticks),U.percentile(ticks,0.95),
      U.mean(mat),U.mean(ecology),U.mean(colonists),memory,bytes,metrics.alive,metrics.food,
      metrics.waterResidual,metrics.mineralResidual,metrics.foodResidual})
    end
   end
  end
  return table.concat(rows,'\n')..'\n', 'DEEPWARD '..C.version..'\n'.._VERSION..'\n'..
   'Clock supplied by caller. Lua heap only, not process RSS or GPU memory.\n'..
   'Timing excludes rendering, history snapshots, yields and post-run evaluation.\n'..
   'Automatic GC stays on; one warm-up per case. p95 uses nearest rank.\n'..
   'Tick samples describe a changing world, not independent identical trials.\n'
 end)
end
return B
