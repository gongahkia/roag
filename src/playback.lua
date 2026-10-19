-- Presentation history only. Its clock never advances the simulation.
local Playback = {}

local durations = {
    WindupStarted = 0.55,
    AttackPerformed = 0.26,
    DamageTaken = 0.20,
    DamageBlocked = 0.20,
    Died = 0.30,
    WindupCancelled = 0.20,
    BoonActivated = 0.22,
    EffectApplied = 0.22,
    DelayedEffectScheduled = 0.18,
    DelayedEffectResolved = 0.28,
    BoonGranted = 0.28,
    StatusApplied = 0.12,
    StatusTick = 0.12,
    SecretDiscovered = 0.48,
    SecretRewardSpawned = 0.28,
    DoorOpened = 0.22,
    TerrainTriggered = 0.12,
    TerrainBroken = 0.20,
    HeatVentWarned = 0.16,
    HeatVentBurst = 0.16,
    FragileArmed = 0.10,
    FragileCollapsed = 0.12,
    Healed = 0.08,
    SupplyCollected = 0.08,
    ElitePhaseChanged = 0.50,
    EliteDefeated = 0.80,
    RegentPhaseChanged = 0.45,
    RegentDefeated = 0.80,
    PursuerArrived = 0.80,
}

-- Routine secondary feedback may be dense in a high-stack boon chain. It is
-- presented as a short shared cascade beat; the ordered simulation events
-- remain intact; unrelated deaths, locked attacks, impacts and motion stay
-- separate.
local cascade = {
    DamageTaken=true, DamageBlocked=true, BoonActivated=true,
    EffectApplied=true, StatusApplied=true, StatusTick=true,
    DelayedEffectScheduled=true, DelayedEffectResolved=true,
    TerrainTriggered=true,
    Healed=true,SupplyCollected=true,
}
Playback.max_cascade_seconds = 0.36

-- Concurrent actors share a beat. Each actor follows its own ordered path;
-- many enemy moves cost one short presentation beat, not N beats.
Playback.tile_seconds = 0.08
Playback.max_beat_seconds = 0.32

function Playback.new()
    return {items={}, remaining=0, poses={}, facings={}}
end

local function horizontal(current, dx)
    if dx < 0 then return -1 elseif dx > 0 then return 1 end
    return current
end

-- Use the committed cells, never the player's later position. Symmetric AOE
-- and vertical attacks retain the previous horizontal facing.
local function attack_facing(current, event)
    local origin = event.from_x or event.source_x
    if not origin then return current end
    local dx = 0
    for _,cell in ipairs(event.target_cells or {}) do dx=dx+cell.x-origin end
    return horizontal(current,dx)
end

local function item_facing(item, id, current, elapsed)
    local track=item.motions and item.motions[id]
    if track then
        local count=#track.path-1
        local leg=math.min(count,math.floor(math.max(0,elapsed)/track.duration*count)+1)
        for i=1,leg do current=horizontal(current,track.facing_dx[i] or 0) end
    end
    for _,event in ipairs(item.events or {item.event}) do
        if event.source_id==id and
            (event.kind=='WindupStarted' or event.kind=='AttackPerformed') then
            current=attack_facing(current,event)
        end
    end
    return current
end

local function motion(event)
    if event.kind ~= "Moved" and event.kind ~= "Displaced" then return nil end
    local id = event.kind == "Displaced" and event.target_id or event.source_id
    if not id or not event.from_x or not event.from_y or not event.x or not event.y then
        return nil
    end
    local path = {{x=event.from_x, y=event.from_y}}
    if event.path then
        for _,cell in ipairs(event.path) do path[#path+1] = {x=cell.x, y=cell.y} end
    else
        path[#path+1] = {x=event.x, y=event.y}
    end
    -- Multi-cell displacement has no authoritative traversed path.
    local snap = event.kind == "Displaced" and
        math.abs(event.x-event.from_x)+math.abs(event.y-event.from_y) > 1
    return id, {floor_id=event.floor_id, path=path, snap=snap}
end

function Playback.add(playback, events, player_id)
    local motions, order, first_event = {}, {}, nil
    local first_new_item = #playback.items + 1
    local player_attacks = {}
    local function flush()
        if not first_event then return end
        local longest = 1
        for _,id in ipairs(order) do
            local track=motions[id]
            longest = math.max(longest, #track.path-1)
            track.duration=math.min(Playback.max_beat_seconds,
                math.max(1,#track.path-1)*Playback.tile_seconds)
        end
        playback.items[#playback.items+1] = {
            event=first_event, motions=motions, order=order,
            duration=math.min(Playback.max_beat_seconds,
                longest*Playback.tile_seconds),
        }
        motions,order,first_event={}, {}, nil
    end
    for _,event in ipairs(events) do
        local id, leg = motion(event)
        if leg then
            local track = motions[id]
            if track and track.floor_id ~= leg.floor_id then
                flush()
                track = nil
            end
            if not track then
                track = {floor_id=leg.floor_id, path={}, facing_dx={}, snap=false}
                motions[id] = track
                order[#order+1] = id
                if not playback.poses[id] then
                    playback.poses[id] = {floor_id=leg.floor_id,
                        x=leg.path[1].x, y=leg.path[1].y}
                end
            end
            if #track.path == 0 then
                track.path[1] = leg.path[1]
            elseif track.path[#track.path].x ~= leg.path[1].x or
                track.path[#track.path].y ~= leg.path[1].y then
                track.snap = true -- discontinuity must not cross the map
            end
            for i=2,#leg.path do
                local index=#track.path
                -- Knockback changes position, not the actor's chosen facing.
                track.facing_dx[index]=event.kind=='Moved' and
                    leg.path[i].x-leg.path[i-1].x or 0
                track.path[index+1] = leg.path[i]
            end
            track.snap = track.snap or leg.snap
            first_event = first_event or event
        else
            local duration = durations[event.kind]
            if event.kind == "AttackPerformed" and event.source_id == player_id then
                flush()
                -- Player attacks used to have no presentation beat. Show the
                -- pose during the next existing beat, if one follows.
                player_attacks[#player_attacks+1]={event=event,index=#playback.items+1}
            elseif duration then
                flush()
                -- Consecutive locked attacks have no intervening impact or
                -- movement to show. Routine boon/damage feedback similarly
                -- shares a bounded beat. Both groups retain event order.
                local previous = playback.items[#playback.items]
                local group_kind = event.kind == "WindupStarted" and "windup"
                    or cascade[event.kind] and event.kind or nil
                if event.kind == "Died" and #playback.items >= first_new_item
                    and previous and previous.event.kind == "DamageTaken"
                    and not previous.events and event.target_id and
                    previous.event.target_id == event.target_id and
                    previous.event.time == event.time and
                    previous.event.floor_id == event.floor_id then
                    -- A lethal hit and its resulting death are one readable
                    -- impact, while both ordered facts remain available.
                    previous.events={previous.event,event}
                    previous.duration=math.max(previous.duration,duration)
                    previous.group_kind="lethal"
                elseif group_kind and #playback.items >= first_new_item and previous
                    and previous.group_kind == group_kind and
                    previous.event.floor_id == event.floor_id then
                    previous.events = previous.events or {previous.event}
                    previous.events[#previous.events+1] = event
                    if cascade[event.kind] then
                        previous.duration=math.min(Playback.max_cascade_seconds,
                            math.max(previous.duration,duration)+0.015)
                    end
                else
                    playback.items[#playback.items+1] = {
                        event=event,duration=duration,group_kind=group_kind}
                end
            end
        end
    end
    flush()
    for _,attack in ipairs(player_attacks) do
        local item=playback.items[attack.index]
        if not item then
            -- A miss with no other feedback still needs one visible pose.
            item={event=attack.event,duration=Playback.tile_seconds,
                pose_only=true}
            playback.items[#playback.items+1]=item
        else
            item.pose_events=item.pose_events or {}
            item.pose_events[#item.pose_events+1]=attack.event
        end
    end
    if playback.remaining == 0 and #playback.items > 0 then
        playback.remaining = playback.items[1].duration
    end
end

function Playback.current(playback)
    return playback.items[1] and playback.items[1].event or nil
end

function Playback.current_group(playback)
    return playback.items[1] and playback.items[1].events or nil
end

function Playback.current_pose_events(playback)
    local item=playback.items[1]
    if not item then return nil end
    if not item.pose_events then return item.events or {item.event} end
    local events={}
    for _,event in ipairs(item.events or {item.event}) do
        events[#events+1]=event
    end
    for _,event in ipairs(item.pose_events) do events[#events+1]=event end
    return events
end

function Playback.busy(playback)
    return #playback.items > 0
end

local function finish_item(playback, item)
    if item.motions then
        for _,id in ipairs(item.order) do
            playback.facings[id]=item_facing(item,id,playback.facings[id] or 1,item.duration)
        end
    else
        for _,event in ipairs(item.events or {item.event}) do
            if event.source_id and
                (event.kind=='WindupStarted' or event.kind=='AttackPerformed') then
                local id=event.source_id
                playback.facings[id]=attack_facing(playback.facings[id] or 1,event)
            end
        end
    end
    if not item.motions then return end
    for _,id in ipairs(item.order) do
        local track = item.motions[id]
        local last = track.path[#track.path]
        playback.poses[id] = {floor_id=track.floor_id,x=last.x,y=last.y}
    end
end

-- Read-only presentation facing. On Continue, reconstruct a locked windup
-- from the existing snapshot; no facing is written into simulation or saves.
function Playback.facing(playback, id, pending_attack, last_dx)
    local current=playback.facings[id] or 1
    local item=playback.items[1]
    if item then return item_facing(item,id,current,item.duration-playback.remaining) end
    if not playback.facings[id] then current=horizontal(current,last_dx or 0) end
    return pending_attack and attack_facing(current,pending_attack) or current
end

function Playback.update(playback, dt)
    if #playback.items == 0 then return end
    playback.remaining = playback.remaining - math.max(0, dt)
    while playback.remaining <= 0 and #playback.items > 0 do
        finish_item(playback,table.remove(playback.items,1))
        if #playback.items > 0 then
            playback.remaining = playback.remaining + playback.items[1].duration
        else
            playback.remaining = 0
            playback.poses = {}
        end
    end
end

-- Visual tile coordinate only. Gameplay still uses the authoritative grid.
function Playback.position(playback, id, floor_id, final_x, final_y)
    local pose = playback.poses[id]
    if not pose or pose.floor_id ~= floor_id then return final_x,final_y end
    local item = playback.items[1]
    local track = item and item.motions and item.motions[id]
    if not track then return pose.x,pose.y end
    local path=track.path
    if track.snap or #path < 2 then return pose.x,pose.y end
    local count=#path-1
    local elapsed=item.duration-playback.remaining
    local progress=math.min(1,math.max(0,elapsed/track.duration))*count
    local leg=math.min(count,math.floor(progress)+1)
    local t=progress-(leg-1)
    t=t*t*(3-2*t) -- exact endpoints, no overshoot
    return path[leg].x+(path[leg+1].x-path[leg].x)*t,
        path[leg].y+(path[leg+1].y-path[leg].y)*t
end

function Playback.visible_position(playback, id, floor_id, final_x, final_y, visible)
    local x,y=Playback.position(playback,id,floor_id,final_x,final_y)
    return x,y,visible(final_x,final_y) and
        visible(math.floor(x+0.5),math.floor(y+0.5))
end

return Playback
