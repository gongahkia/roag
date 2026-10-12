-- Read-only pacing and presentation probe. No renderer or gameplay mutation.
package.path='./?.lua;./?/init.lua;'..package.path
local Biomes=require('src.biomes')
local Content=require('src.content')
local Game=require('src.game')
local Generation=require('src.generation')
local Persistence=require('src.persistence')
local Playback=require('src.playback')
local Play=require('tests.legal_playthrough')

local count=tonumber(arg[1]) or 20
assert(count>=1 and count<=100 and count%1==0)
local dirs={{1,0},{-1,0},{0,1},{0,-1}}
local function percentile(values,p)
    table.sort(values)
    return values[math.max(1,math.ceil(#values*p))] or 0
end
local function distances(map)
    local cells,queue={},{{x=map.start_x,y=map.start_y}}
    local function key(x,y) return (y-1)*map.width+x end
    cells[key(map.start_x,map.start_y)]=0
    local i=1
    while i<=#queue do
        local at=queue[i]
        for _,d in ipairs(dirs) do
            local x,y=at.x+d[1],at.y+d[2]
            local k=key(x,y)
            if cells[k]==nil and Content.walkable(map,x,y) then
                cells[k]=cells[key(at.x,at.y)]+1
                queue[#queue+1]={x=x,y=y}
            end
        end
        i=i+1
    end
    local function distance(p) return cells[key(p.x,p.y)] end
    local function nearest(list)
        local best
        for _,p in ipairs(list) do
            local n=distance(p)
            if n and (not best or n<best) then best=n end
        end
        return best
    end
    return nearest(map.placements.enemies),nearest(map.placements.chests),
        distance(map.elite)
end
local function lane_metrics(map)
    local clear,without_exit,longest,exposed,steps=0,0,0,0,0
    for _,p in ipairs(map.placements.enemies) do
        if p.definition_id=='core:enemy/thornspitter' or
            p.definition_id=='core:enemy/canal_lancer' then
            for _,d in ipairs(dirs) do
                local length,has_exit=0,false
                for step=1,7 do
                    local x,y=p.x+step*d[1],p.y+step*d[2]
                    if not Content.projectile_passable(map,x,y) then break end
                    length=step
                    steps=steps+1
                    local side=Content.walkable(map,x-d[2],y+d[1]) or
                        Content.walkable(map,x+d[2],y-d[1])
                    if side then has_exit=true else exposed=exposed+1 end
                end
                longest=math.max(longest,length)
                if length>=5 then
                    clear=clear+1
                    if not has_exit then without_exit=without_exit+1 end
                end
            end
        end
    end
    return clear,without_exit,longest,exposed,steps
end

for _,id in ipairs(Biomes.order) do
    local enemy,chest,elite,corridor={}, {}, {}, {}
    local clear,trapped,longest,retries,repairs,fallback=0,0,0,0,0,0
    local exposed_steps,lane_steps=0,0
    for seed=1,count do
        local map=Generation.generate(seed,{biome_id=id})
        local a,b,c=distances(map)
        assert(a and b and c,'unreachable landmark '..id..' seed '..seed)
        enemy[#enemy+1],chest[#chest+1],elite[#elite+1]=a,b,c
        for _,passage in ipairs(map.corridors) do
            corridor[#corridor+1]=passage.length
        end
        local lanes,without_exit,far,exposed,steps=lane_metrics(map)
        clear,trapped,longest=clear+lanes,trapped+without_exit,
            math.max(longest,far)
        exposed_steps,lane_steps=exposed_steps+exposed,lane_steps+steps
        retries=retries+#map.diagnostics.attempts-1
        repairs=repairs+#map.repairs
        if map.diagnostics.fallback then fallback=fallback+1 end
    end
    print(('%s seeds=%d first_enemy_tiles_p50/p95=%d/%d first_chest=%d/%d elite=%d/%d corridor_p50/p95/max=%d/%d/%d ranged_lanes_5plus=%d lanes_without_lateral_exit=%d locally_exposed_steps=%d/%d longest_spawn_lane=%d retries=%d repairs=%d fallback=%d'):
        format(id,count,percentile(enemy,.5),percentile(enemy,.95),
            percentile(chest,.5),percentile(chest,.95),
            percentile(elite,.5),percentile(elite,.95),
            percentile(corridor,.5),percentile(corridor,.95),
            percentile(corridor,1),clear,trapped,exposed_steps,lane_steps,longest,
            retries,repairs,fallback))
end

local run=Play.complete_run(3)
local replay=Game.new_stage(3)
local durations,items={},{}
local quiet_moves,actions,total_duration,grouped_events=0,0,0,0
local top={}
local function shown(event,state)
    if not (state.generated_map or state.finale) then return true end
    local floor=state.active_floor_id
    local function visible(x,y) return Game.is_visible(state,floor,x,y) end
    local spatial=false
    if event.x and event.y then
        spatial=true
        if visible(event.x,event.y) then return true end
    end
    for _,cell in ipairs(event.target_cells or {}) do
        spatial=true
        if visible(cell.x,cell.y) then return true end
    end
    if not spatial and event.source_position and
        visible(event.source_position.x,event.source_position.y) then return true end
    return event.kind=='BoonGranted' or event.kind=='StatIncreased'
end
for i,record in ipairs(run.trace) do
    local old_floor=replay.active_floor_id
    local ok,reason,events=Game.submit(replay,record.intent)
    assert(ok==record.ok and reason==record.reason and replay.clock==record.clock)
    local visible={}
    for _,event in ipairs(events) do
        if shown(event,replay) then visible[#visible+1]=event end
    end
    local playback=Playback.new()
    if old_floor~=replay.active_floor_id then playback=Playback.new() end
    Playback.add(playback,visible,replay.player_id)
    local seconds=0
    for _,item in ipairs(playback.items) do
        seconds=seconds+item.duration
        if item.events then
            grouped_events=grouped_events+#item.events
        end
    end
    total_duration=total_duration+seconds
    durations[#durations+1]=seconds
    items[#items+1]=#playback.items
    if record.intent.kind=='move' then
        local quiet=true
        for _,kind in ipairs(record.event_kinds) do
            if kind=='AttackPerformed' or kind=='DamageTaken' or
                kind=='WindupStarted' or kind=='BoonActivated' or
                kind=='CurrencyCollected' or kind=='Died' then quiet=false;break end
        end
        if quiet then quiet_moves=quiet_moves+1 end
    end
    actions=actions+1
    local labels={}
    for _,item in ipairs(playback.items) do
        labels[#labels+1]=item.event.kind..':'..string.format('%.2f',item.duration)
    end
    top[#top+1]={i=i,seconds=seconds,items=#playback.items,
        kind=record.intent.kind,events=#events,labels=table.concat(labels,',')}
    if i==327 or i==332 then
        print(('playback_example action=%d seconds=%.2f items=%d beats=%s'):
            format(i,seconds,#playback.items,top[#top].labels))
    end
end
assert(Persistence.encode(Game.snapshot(replay))==
    Persistence.encode(Game.snapshot(run.state)),'legal replay diverged')
table.sort(top,function(a,b)return a.seconds>b.seconds end)
for i=1,math.min(5,#top) do
    local t=top[i]
    print(('playback_tail action=%d kind=%s seconds=%.2f items=%d events=%d beats=%s'):
        format(t.i,t.kind,t.seconds,t.items,t.events,t.labels))
end
print(('legal_seed_3 actions=%d quiet_move_actions=%d playback_p50/p95/max_s=%.2f/%.2f/%.2f items_p95/max=%d/%d total_visual_s=%.2f grouped_events=%d victory_hp=%d'):
    format(actions,quiet_moves,percentile(durations,.5),percentile(durations,.95),
        percentile(durations,1),percentile(items,.95),percentile(items,1),
        total_duration,grouped_events,
        run.state.victory.hp))
for _,checkpoint in ipairs(run.checkpoints) do
    print(('checkpoint %s action=%d clock=%d hp=%d currency=%d stage=%d'):
        format(checkpoint.name,checkpoint.actions,checkpoint.clock,
            checkpoint.hp,checkpoint.currency,checkpoint.stage))
end

local chain=Game.new(21)
for _,name in ipairs({'storm_conductor','detonation_bloom',
    'resonant_wounds','temporal_echo','static_footsteps'}) do
    Game.set_boon_stacks(chain,chain.player_id,'core:boon/'..name,
        'legendary',8)
end
for _,p in ipairs({{3,2},{3,3},{2,3},{4,2},{5,2}}) do
    Game.spawn_enemy(chain,'core:enemy/ruin_skitter',p[1],p[2],100)
end
local _,_,chain_events=Game.submit(chain,{kind='sweeping_slash',dx=1,dy=0})
local chain_playback=Playback.new()
Playback.add(chain_playback,chain_events,chain.player_id)
local seconds=0
for _,item in ipairs(chain_playback.items) do seconds=seconds+item.duration end
print(('high_stack_chain events=%d visual_items=%d visual_seconds=%.2f'):
    format(#chain_events,#chain_playback.items,seconds))
