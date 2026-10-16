-- Short, presentation-only feedback for positive damage to the player.
-- This reads the displayed playback beat, never the simulation clock or RNG.
local HitFeedback = {}

HitFeedback.shake_seconds = 0.16
HitFeedback.flash_seconds = 0.22

function HitFeedback.new()
    return {floor_id=nil, active_item=nil, shake_age=HitFeedback.shake_seconds,
        flash_age=HitFeedback.flash_seconds, strength=0}
end

function HitFeedback.update(feedback, playback, dt, player_id, floor_id)
    if feedback.floor_id ~= floor_id then
        feedback.floor_id = floor_id
        feedback.active_item = nil
        feedback.shake_age = HitFeedback.shake_seconds
        feedback.flash_age = HitFeedback.flash_seconds
        feedback.strength = 0
    end

    local item = playback.items[1]
    if item ~= feedback.active_item then
        feedback.active_item = item
        local loss = 0
        if item then
            for _,event in ipairs(item.events or {item.event}) do
                if event.kind == "DamageTaken" and event.target_id == player_id
                    and (not event.floor_id or event.floor_id == floor_id) then
                    loss = loss + math.max(0, event.actual_loss or 0)
                end
            end
        end
        if loss > 0 then
            feedback.shake_age = 0
            feedback.flash_age = 0
            feedback.strength = math.min(5, 2 + math.sqrt(loss))
            return
        end
    end
    local step = math.max(0, dt)
    feedback.shake_age = math.min(HitFeedback.shake_seconds,
        feedback.shake_age + step)
    feedback.flash_age = math.min(HitFeedback.flash_seconds,
        feedback.flash_age + step)
end

function HitFeedback.offset(feedback)
    if feedback.shake_age >= HitFeedback.shake_seconds then return 0, 0 end
    local age = feedback.shake_age
    local fade = (1 - age / HitFeedback.shake_seconds)^2
    local strength = feedback.strength * fade
    local x = math.floor(math.sin(age * 2 * math.pi * 12) * strength + 0.5)
    local y = math.floor(math.cos(age * 2 * math.pi * 17) * strength + 0.5)
    return x, y
end

function HitFeedback.flash_alpha(feedback)
    if feedback.flash_age >= HitFeedback.flash_seconds then return 0 end
    local fade = 1 - feedback.flash_age / HitFeedback.flash_seconds
    return 0.17 * fade * fade
end

return HitFeedback
