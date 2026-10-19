-- Small cosmetic accents. Reads committed, visibility-filtered events only;
-- never owns graphics handles, gameplay time, input gates or random state.
local Playback=require('src.playback')
local J={reward_seconds=.48,reward_limit=16,attack_seconds=.12,
    impact_seconds=.18,crumble_seconds=.24,celebration_seconds=.8,
    impact_limit=16,crumble_limit=8,celebration_limit=4}
local function round(n)return math.floor(n+.5)end
local function floor_of(e)return e.floor_id or e.source_position and e.source_position.floor_id end

function J.attack_offset(playback,id,floor_id,enabled)
    local item=playback.items[1]
    if not enabled or not item or item.motions and item.motions[id]then return 0,0 end
    local duration=math.min(J.attack_seconds,item.duration)
    local elapsed=item.duration-playback.remaining
    if duration<=0 or elapsed<=0 or elapsed>=duration then return 0,0 end
    for _,e in ipairs(Playback.current_pose_events(playback)or {})do
        if e.kind=='AttackPerformed' and e.source_id==id and floor_of(e)==floor_id then
            local origin=e.source_position
            local ox,oy=e.from_x or origin and origin.x,e.from_y or origin and origin.y
            if ox and oy then
                local dx,dy=0,0
                for _,c in ipairs(e.aim_cells or e.target_cells or {})do
                    dx,dy=dx+c.x-ox,dy+c.y-oy
                end
                local span=math.max(math.abs(dx),math.abs(dy))
                if span>0 then
                    local kick=3*math.sin(math.pi*elapsed/duration)
                    return round(kick*dx/span),round(kick*dy/span)
                end
            end
        end
    end
    return 0,0
end

local function append(list,e,limit)
    list[#list+1]=e
    if #list>limit then table.remove(list,1)end
end
local function celebration(model,kind,x,y,floor_id,level)
    -- Several automatic levels in one action share one readable badge.
    for _,e in ipairs(model.celebrations)do
        if e.kind==kind and e.x==x and e.y==y then
            e.level=math.max(e.level or 0,level or 0);e.age=0;return
        end
    end
    append(model.celebrations,{kind=kind,x=x,y=y,floor_id=floor_id,
        level=level,age=0},J.celebration_limit)
end
local function activate(model,item,playback,player_id,floor_id)
    local hits,deaths={},{}
    for _,e in ipairs(item.events or {item.event})do
        if floor_of(e)==floor_id and e.x and e.y then
            if (e.kind=='DamageTaken' and (e.actual_loss or 0)>0 and e.direct_attack
                or e.kind=='DamageBlocked')then
                local key=tostring(e.target_id)..':'..e.x..':'..e.y
                if not hits[key]then
                    hits[key]=true
                    local p=e.source_position
                    local dx,dy=p and e.x-p.x or 0,p and e.y-p.y or 0
                    local length=math.sqrt(dx*dx+dy*dy)
                    if length>0 then dx,dy=dx/length,dy/length end
                    append(model.impacts,{kind=e.kind=='DamageBlocked' and 'blocked' or 'hit',
                        x=e.x,y=e.y,dx=dx,dy=dy,age=0,floor_id=floor_id,
                        friendly=e.source_id==player_id},J.impact_limit)
                end
            elseif e.kind=='Died' and e.target_faction=='enemy' and
                e.target_definition_id and not deaths[e.target_id]then
                deaths[e.target_id]=true
                append(model.crumbles,{binding=e.target_definition_id,x=e.x,y=e.y,
                    floor_id=floor_id,age=0,flip=Playback.facing(playback,e.target_id)==-1},
                    J.crumble_limit)
            elseif e.kind=='SecretDiscovered' then
                celebration(model,'secret',e.x,e.y,floor_id)
            end
        end
    end
end
function J.new()
    return {items={},impacts={},crumbles={},celebrations={},floor_id=nil,active_item=nil}
end
function J.update(model,dt,floor_id,enabled,playback,player_id)
    if not enabled or model.floor_id~=floor_id then
        model.items={};model.impacts={};model.crumbles={};model.celebrations={}
        model.active_item=nil
    end
    model.floor_id=floor_id
    for _,group in ipairs({{model.items,J.reward_seconds},{model.impacts,J.impact_seconds},
        {model.crumbles,J.crumble_seconds},{model.celebrations,J.celebration_seconds}})do
        for i=#group[1],1,-1 do
            local e=group[1][i];e.age=e.age+math.max(0,dt)
            if e.age>=group[2]then table.remove(group[1],i)end
        end
    end
    if playback then
        local item=playback.items[1]
        if item~=model.active_item then
            model.active_item=item
            if enabled and item then activate(model,item,playback,player_id,floor_id)end
        end
    end
end
function J.add(model,events,player_id,floor_id,enabled)
    J.update(model,0,floor_id,enabled)
    if not enabled then return end
    for _,e in ipairs(events)do
        if e.source_id==player_id and floor_of(e)==floor_id then
            local binding,kind
            if e.kind=='CurrencyCollected' and e.amount>0 then
                binding,kind='core:drop/currency','gold'
            elseif e.kind=='SupplyCollected' and (e.actual_gain or 0)>0 then
                binding,kind='core:drop/'..e.supply_kind,'health'
            elseif e.kind=='UtilityCollected' then
                binding,kind='core:object/interaction_'..e.utility_kind,'utility'
            elseif e.kind=='WardFlaskCollected' or e.kind=='VaultKeyCollected' then
                binding,kind='core:object/review_'..(e.kind=='WardFlaskCollected' and 'ward' or 'key'),'utility'
            elseif e.kind=='ChestClaimed' or e.kind=='BoonGranted' and not e.chest_id then
                binding,kind=e.boon_id..'/icon','boon'
            end
            local p=e.source_position
            local x,y=e.x or p and p.x,e.y or p and p.y
            if e.kind=='LevelUp' and x and y then
                celebration(model,'level',x,y,floor_id,e.new_level)
            end
            if binding and x and y then
                model.items[#model.items+1]={binding=binding,kind=kind,x=x,y=y,
                    floor_id=floor_id,age=0,chest_id=e.kind=='ChestClaimed' and e.chest_id or nil}
                if #model.items>J.reward_limit then table.remove(model.items,1)end
            end
        end
    end
end
function J.impact_pose(e,index,tile)
    local t=math.min(1,e.age/J.impact_seconds)
    local angle=(index-3)*.48
    local dx,dy=e.dx,e.dy
    if dx==0 and dy==0 then dx,dy=math.cos(index*math.pi*.4),math.sin(index*math.pi*.4)end
    local c,s=math.cos(angle),math.sin(angle)
    local distance=tile*(.08+.3*t)
    return round((dx*c-dy*s)*distance),round((dx*s+dy*c)*distance),
        t<.55 and 2 or 1,(1-t)^2
end
function J.ring_pose(e,tile)
    local t=math.min(1,e.age/J.impact_seconds)
    return round(tile*(.12+.22*t)),(1-t)^2
end
function J.celebration_pose(e,tile)
    local t=math.min(1,e.age/J.celebration_seconds)
    return round(tile*(.25+.48*t)),math.min(1,(1-t)*2),round(10*t)
end
function J.crumble_piece(e,index,size)
    local col,row=(index-1)%3,math.floor((index-1)/3)
    local left,top=math.floor(col*size/3),math.floor(row*size/3)
    local width,height=math.floor((col+1)*size/3)-left,math.floor((row+1)*size/3)-top
    local t=math.min(1,e.age/J.crumble_seconds)
    -- Source-aligned fragments drift apart; no per-frame Quad or Image allocation.
    return left,top,width,height,round((col-1)*7*t),round((row-1)*5*t+3*t*t),(1-t)^2
end
function J.chest_offset(model,id,enabled)
    if enabled then
        for _,e in ipairs(model.items)do
            if e.chest_id==id and e.age<.22 then
                return -round(4*math.sin(math.pi*e.age/.22))
            end
        end
    end
    return 0
end
function J.reward_pose(e,x,y,target_x,target_y)
    local t=math.min(1,math.max(0,e.age/J.reward_seconds))
    local travel=t*t*(3-2*t)
    local hop=math.sin(math.pi*t)
    local size=(e.kind=='boon' and 24 or 18)*(1-.5*travel)+2*hop
    return round(x+(target_x-x)*travel),round(y+(target_y-y)*travel-30*hop),
        round(size),math.min(1,(1-t)/.3)
end
return J
