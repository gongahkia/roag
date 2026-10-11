-- Opt-in measurements. Synthetic crowd scenarios use valid Game.spawn_enemy,
-- while the guided route records only ordinary Game.submit actions.
package.path='./?.lua;./?/init.lua;'..package.path
assert(_G.love==nil,'headless profile must not require LÖVE')
local Game=require('src.game')
local Content=require('src.content')
local Play=require('tests.legal_playthrough')

local function quantile(values,p)
    table.sort(values)
    return values[math.max(1,math.ceil(#values*p))] or 0
end
local function measure(name,state,intents,actors,setup,seed)
    collectgarbage('collect')
    local start_memory=collectgarbage('count')
    local initial=state.clock
    local samples,events,effects,peak_memory={},0,0,start_memory
    local start=os.clock()
    for _,intent in ipairs(intents) do
        local t=os.clock()
        local ok,reason,output=Game.submit(state,intent)
        if not ok then error(name..': '..tostring(reason)) end
        samples[#samples+1]=(os.clock()-t)*1000
        events=events+#output
        for _,event in ipairs(output) do
            if event.kind=='BoonActivated' or event.kind=='ScheduledEffectResolved' then
                effects=effects+1
            end
        end
        peak_memory=math.max(peak_memory,collectgarbage('count'))
    end
    local elapsed=os.clock()-start
    local max=quantile(samples,1)
    local total_ms=0
    for _,value in ipairs(samples) do total_ms=total_ms+value end
    print(('%s\tseed=%d\tactors=%d\tactions=%d\tsim_units=%d\tcpu_s=%.3f\tmean_ms=%.3f\tp50_ms=%.3f\tp95_ms=%.3f\tmax_ms=%.3f\tevents=%d\tactivation_or_due_events=%d\theap_start_kb=%.0f\theap_peak_kb=%.0f\tqueue_peak=unavailable\tsetup=%s'):
        format(name,seed or 21,actors,#samples,state.clock-initial,elapsed,
        total_ms/#samples,quantile(samples,0.50),
            quantile(samples,0.95),max,events,effects,start_memory,peak_memory,
            setup or 'normal'))
end

local function crowd(count)
    local state=Game.new_stage(21)
    local map=Content.map_for(state)
    local occupied={}
    for _,entity in ipairs(state.entities) do
        if entity.blocks_movement then
            occupied[entity.position.y*map.width+entity.position.x]=true
        end
    end
    local player=Game.player(state).position
    local added=0
    for y=2,map.height-1 do
        for x=2,map.width-1 do
            local k=y*map.width+x
            if added<count and Content.walkable(map,x,y) and not occupied[k]
                and math.abs(x-player.x)+math.abs(y-player.y)>8 then
                Game.spawn_enemy(state,'core:enemy/ruin_skitter',x,y,state.clock+100)
                occupied[k]=true;added=added+1
            end
        end
    end
    assert(added==count,'insufficient valid crowd positions')
    return state
end

local count=tonumber(arg[1]) or 12
local idle=Game.new_stage(21)
local waits={}
for i=1,count do waits[i]={kind='wait'} end
measure('normal_wait',idle,waits,#idle.entities,'generated stage')
for _,n in ipairs({15,40,90}) do
    local state=crowd(n)
    local actions={}
    for i=1,math.max(2,math.floor(count/3)) do actions[i]={kind='wait'} end
    measure('crowd_'..n,state,actions,#state.entities,
        'synthetic valid spawns, normal scheduler')
end
local boon=Game.new(21)
local player=Game.player(boon)
for _,name in ipairs({'storm_conductor','detonation_bloom',
    'resonant_wounds','temporal_echo','static_footsteps'}) do
    Game.set_boon_stacks(boon,player.id,'core:boon/'..name,'legendary',8)
end
for _,position in ipairs({{3,2},{3,3},{2,3},{4,2},{5,2}}) do
    Game.spawn_enemy(boon,'core:enemy/ruin_skitter',position[1],position[2],100)
end
measure('high_stack_chain',boon,{
    {kind='sweeping_slash',dx=1,dy=0},
    {kind='wait'},{kind='wait'},
},#boon.entities,'40 legendary copies; actual Sweep/proc chain')

local guided=Play.stage_one(3)
local route={}
for i=1,math.min(count,#guided.trace) do route[i]=guided.trace[i].intent end
local guided_actions,guided_events,guided_stage,guided_hp=
    #guided.trace,#guided.events,guided.state.stage.index,Game.player(guided.state).hp
guided=nil
collectgarbage('collect')
local route_state=Game.new_stage(3)
measure('legal_route',route_state,route,#route_state.entities,
    'guided ordinary-action trace prefix',3)
print(('guided_seed_3\tlegal_actions=%d\tevents=%d\tstage=%d\thp=%d'):
    format(guided_actions,guided_events,guided_stage,guided_hp))
