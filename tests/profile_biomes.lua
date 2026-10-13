-- Opt-in CPU measurements for actual biome stages. The high-HP test player is
-- a profiling fixture, not an assisted gameplay acceptance claim.
package.path='./?.lua;./?/init.lua;'..package.path
local Biomes=require('src.biomes')
local Game=require('src.game')

local function quantile(samples,fraction)
    table.sort(samples)
    return samples[math.max(1,math.ceil(#samples*fraction))]
end
for index=1,#Biomes.order do
    local biome_id=Biomes.order[index]
    local history={}
    if index>1 then history[1]=biome_id:gsub('^core:biome/','core:route/') end
    local stage_index=index==1 and 1 or 2
    if index==4 then
        stage_index=3
        history={'core:route/fungal_hollows','core:route/ashen_bastion'}
    end
    local state=Game.new_stage(23,{multifloor=true,stage_options={index=stage_index,
        biome_id=biome_id,route_history=history}})
    local player=Game.player(state)
    player.hp,player.max_hp,player.defense=1000,1000,100
    collectgarbage('collect')
    local heap_start=collectgarbage('count')
    local samples,events,peak={},0,heap_start
    for action=1,16 do
        local started=os.clock()
        local ok,reason,out=Game.submit(state,{kind='wait'})
        assert(ok,reason)
        samples[#samples+1]=(os.clock()-started)*1000
        events=events+#out
        peak=math.max(peak,collectgarbage('count'))
    end
    print(('%s seed=23 actors=%d actions=16 sim_units=%d p50_ms=%.3f p95_ms=%.3f max_ms=%.3f events=%d heap_start_kb=%.0f heap_peak_kb=%.0f')
        :format(biome_id,#state.entities,state.clock,
            quantile(samples,0.50),quantile(samples,0.95),
            quantile(samples,1),events,heap_start,peak))
end
