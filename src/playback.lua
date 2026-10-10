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
}

function Playback.new()
    return {items={}, remaining=0}
end

function Playback.add(playback, events, player_id)
    for _, event in ipairs(events) do
        local duration = durations[event.kind]
        if duration and (event.kind ~= "AttackPerformed" or event.source_id ~= player_id) then
            playback.items[#playback.items + 1] = {event=event, duration=duration}
        end
    end
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

function Playback.update(playback, dt)
    if #playback.items == 0 then return end
    playback.remaining = playback.remaining - math.max(0, dt)
    while playback.remaining <= 0 and #playback.items > 0 do
        table.remove(playback.items, 1)
        if #playback.items > 0 then
            playback.remaining = playback.remaining + playback.items[1].duration
        else
            playback.remaining = 0
        end
    end
end

return Playback
