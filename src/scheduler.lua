local Scheduler = {}

local function priority(item)
    if item.kind == "effect" then return 0 end
    assert(item.kind == "actor_ready", "unknown scheduled kind")
    return 1
end

local function before(a, b)
    if a.due ~= b.due then return a.due < b.due end
    local ap, bp = priority(a), priority(b)
    if ap ~= bp then return ap < bp end
    if ap == 1 and a.is_player ~= b.is_player then return a.is_player end
    if a.seq ~= b.seq then return a.seq < b.seq end
    return a.id < b.id
end

function Scheduler.enqueue(state, item)
    assert(type(item.due) == "number" and item.due % 1 == 0 and item.due >= state.clock,
        "scheduled time must be an integer at or after world time")
    priority(item)
    local id = state.next_schedule_id
    state.next_schedule_id = id + 1
    local entry = {
        id = id,
        seq = id,
        due = item.due,
        kind = item.kind,
        actor_id = item.actor_id,
        is_player = item.is_player,
        effect_id = item.effect_id,
    }
    state.schedule[#state.schedule + 1] = entry
    table.sort(state.schedule, before)
    return entry
end

function Scheduler.peek(state)
    return state.schedule[1]
end

function Scheduler.pop(state)
    return table.remove(state.schedule, 1)
end

function Scheduler.sort(state)
    table.sort(state.schedule, before)
end

return Scheduler
