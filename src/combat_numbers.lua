-- Presentation-only hit feedback; consumes displayed playback items, not game state.
local Numbers={}
Numbers.lifetime=0.78
Numbers.limit=24

function Numbers.new()
    return {items={},active_item=nil,floor_id=nil}
end

local function add_item(numbers,hit)
    local lane=0
    for _,prior in ipairs(numbers.items) do
        if prior.x==hit.x and prior.y==hit.y then lane=lane+1 end
    end
    hit.lane=({0,-1,1})[lane%3+1]
    hit.age=0
    numbers.items[#numbers.items+1]=hit
    if #numbers.items>Numbers.limit then table.remove(numbers.items,1) end
end

local function activate(numbers,item,player_id,floor_id)
    local events=item.events or {item.event}
    local ordered,by_target={},{}
    for _,event in ipairs(events) do
        if (event.kind=='DamageTaken' or event.kind=='DamageBlocked') and
            event.x and event.y and (not event.floor_id or event.floor_id==floor_id) then
            local key=tostring(event.target_id or '')..':'..event.x..':'..event.y
            local hit=by_target[key]
            if not hit then
                hit={x=event.x,y=event.y,amount=0,blocked=false,
                    critical=false,from_player=false}
                by_target[key]=hit
                ordered[#ordered+1]=hit
            end
            if event.kind=='DamageTaken' and (event.actual_loss or 0)>0 then
                hit.amount=hit.amount+event.actual_loss
                hit.critical=hit.critical or event.critical==true
                hit.from_player=hit.from_player or event.source_id==player_id
            elseif event.kind=='DamageBlocked' then
                hit.blocked=true
            end
        end
    end
    for _,hit in ipairs(ordered) do
        if hit.amount>0 or hit.blocked then add_item(numbers,hit) end
    end
end

function Numbers.update(numbers,playback,dt,player_id,floor_id)
    if numbers.floor_id~=floor_id then
        numbers.items={}
        numbers.active_item=nil
        numbers.floor_id=floor_id
    end
    for i=#numbers.items,1,-1 do
        local hit=numbers.items[i]
        hit.age=hit.age+math.max(0,dt)
        if hit.age>=Numbers.lifetime then table.remove(numbers.items,i) end
    end
    local item=playback.items[1]
    if item~=numbers.active_item then
        numbers.active_item=item
        if item then activate(numbers,item,player_id,floor_id) end
    end
end

return Numbers
