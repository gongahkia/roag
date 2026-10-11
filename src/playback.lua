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
    ElitePhaseChanged = 0.50,
    EliteDefeated = 0.80,
    PursuerArrived = 0.80,
}

-- Concurrent actors share a beat. Each actor follows its own ordered path;
-- many enemy moves cost one short presentation beat, not N beats.
Playback.tile_seconds = 0.08
Playback.max_beat_seconds = 0.32

function Playback.new()
    return {items={}, remaining=0, poses={}}
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
                track = {floor_id=leg.floor_id, path={}, snap=false}
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
            for i=2,#leg.path do track.path[#track.path+1] = leg.path[i] end
            track.snap = track.snap or leg.snap
            first_event = first_event or event
        else
            local duration = durations[event.kind]
            if duration and (event.kind ~= "AttackPerformed" or event.source_id ~= player_id) then
                flush()
                playback.items[#playback.items+1] = {event=event,duration=duration}
            end
        end
    end
    flush()
    if playback.remaining == 0 and #playback.items > 0 then
        playback.remaining = playback.items[1].duration
    end
end

function Playback.current(playback)
    return playback.items[1] and playback.items[1].event or nil
end

function Playback.busy(playback)
    return #playback.items > 0
end

local function finish_item(playback, item)
    if not item.motions then return end
    for _,id in ipairs(item.order) do
        local track = item.motions[id]
        local last = track.path[#track.path]
        playback.poses[id] = {floor_id=track.floor_id,x=last.x,y=last.y}
    end
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
