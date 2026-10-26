-- Presentation-only, opt-in measurements. Never mutates or samples game RNG.
local Diagnostics={}
local function percentile(values,p)
    if #values==0 then return nil end
    local sorted={}
    for i,value in ipairs(values) do sorted[i]=value end
    table.sort(sorted)
    return sorted[math.max(1,math.ceil(#sorted*p))]
end

function Diagnostics.new()
    return {frame_ms={},update_ms={},draw_ms={},last_frame=nil,max_samples=600}
end

function Diagnostics.sample(state,now,update_ms,draw_ms,visible_actor_count,stats)
    if state.last_frame then
        local sample=(now-state.last_frame)*1000
        local values=state.frame_ms
        values[#values+1]=sample
        if #values>state.max_samples then table.remove(values,1) end
    end
    state.last_frame=now
    state.update_ms[#state.update_ms+1]=update_ms or 0
    state.draw_ms[#state.draw_ms+1]=draw_ms or 0
    if #state.update_ms>state.max_samples then table.remove(state.update_ms,1) end
    if #state.draw_ms>state.max_samples then table.remove(state.draw_ms,1) end
    state.visible_actors=visible_actor_count or 0
    state.draw_calls=stats and stats.drawcalls or nil
    state.texture_bytes=stats and stats.texturememory or nil
end

function Diagnostics.summary(state)
    return {samples=#state.frame_ms,
        frame_p50_ms=percentile(state.frame_ms,0.50),
        frame_p95_ms=percentile(state.frame_ms,0.95),
        frame_p99_ms=percentile(state.frame_ms,0.99),
        update_p95_ms=percentile(state.update_ms,0.95),
        draw_p95_ms=percentile(state.draw_ms,0.95),
        visible_actors=state.visible_actors,
        draw_calls=state.draw_calls,texture_bytes=state.texture_bytes}
end

return Diagnostics
