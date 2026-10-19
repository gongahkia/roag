-- Connected 2D floors. The stage clock is global; floor-local scheduler work
-- is rebased on return by precisely the simulated time spent away.
local Content=require("src.content")
local Scheduler=require("src.scheduler")

local Floors={}
local directions={{0,-1},{1,0},{0,1},{-1,0}}

function Floors.map(state,floor_id)
    if floor_id==state.active_floor_id then return state.generated_map end
    return state.inactive_floor_maps and state.inactive_floor_maps[floor_id]
end

function Floors.portal(state,x,y)
    local map=state.generated_map
    for _,stair in ipairs(map and map.placements and map.placements.stairs or {}) do
        if stair.x==x and stair.y==y then return stair end
    end
end

local function occupied(state,floor_id,x,y,exclude)
    for _,entity in ipairs(state.entities) do
        local p=entity.position
        if entity.id~=exclude and entity.blocks_movement and p
            and p.floor_id==floor_id and p.x==x and p.y==y then return true end
    end
    return false
end

function Floors.landing(state,stair)
    local map=Floors.map(state,stair.to_floor_id)
    if not map then return nil,"stair destination missing" end
    local queue={{x=stair.to_x,y=stair.to_y,distance=0}}
    local seen={[stair.to_x..","..stair.to_y]=true}
    local head=1
    while head<=#queue do
        local p=queue[head]
        if Content.walkable(map,p.x,p.y)
            and not occupied(state,map.floor_id,p.x,p.y,state.player_id) then
            return {floor_id=map.floor_id,x=p.x,y=p.y}
        end
        if p.distance<3 then
            for _,d in ipairs(directions) do
                local x,y=p.x+d[1],p.y+d[2]
                local key=x..","..y
                if not seen[key] and Content.walkable(map,x,y)
                    and not occupied(state,map.floor_id,x,y,state.player_id) then
                    seen[key]=true
                    queue[#queue+1]={x=x,y=y,distance=p.distance+1}
                end
            end
        end
        head=head+1
    end
    return nil,"stair landing blocked"
end

function Floors.park(state,floor_id)
    local local_items={}
    for index=#state.schedule,1,-1 do
        local item=state.schedule[index]
        if item.floor_id==floor_id then
            table.insert(local_items,1,table.remove(state.schedule,index))
        end
    end
    state.floor_pause=state.floor_pause or {}
    assert(not state.floor_pause[floor_id],"floor already paused")
    state.floor_pause[floor_id]={suspended_at=state.clock,schedule=local_items}
end

function Floors.resume(state,floor_id)
    local pause=assert(state.floor_pause and state.floor_pause[floor_id],
        "floor was not paused")
    local delta=state.clock-pause.suspended_at
    assert(delta>=0,"stage clock moved backwards")
    for _,item in ipairs(pause.schedule) do
        item.due=item.due+delta
        state.schedule[#state.schedule+1]=item
    end
    local storm=state.class_special and state.class_special.storm
    if storm and storm.floor_id==floor_id then storm.due=storm.due+delta end
    for _,ember in ipairs(state.idea_embers or {})do
        if ember.floor_id==floor_id then
            ember.next_due=ember.next_due+delta;ember.expires=ember.expires+delta
        end
    end
    for _,silt in ipairs(state.idea_silt or {})do
        if silt.floor_id==floor_id then silt.expires=silt.expires+delta end
    end
    for _,mirror in ipairs(state.idea_mirrors or {})do
        if mirror.floor_id==floor_id then mirror.expires=mirror.expires+delta end
    end
    for _,web in ipairs(state.idea_webs or {})do
        if web.floor_id==floor_id then web.expires=web.expires+delta end
    end
    for _,entity in ipairs(state.entities) do
        if entity.id~=state.player_id and entity.position
            and entity.position.floor_id==floor_id then
            if entity.sentry_due then entity.sentry_due=entity.sentry_due+delta end
            if entity.controller=='geomancer_block' then entity.expires=entity.expires+delta end
            if entity.pending_attack then
                entity.pending_attack.due=entity.pending_attack.due+delta
            end
            if entity.stunned_until and entity.stunned_until>pause.suspended_at then
                entity.stunned_until=entity.stunned_until+delta
            end
            for _,status in pairs(entity.statuses or {}) do
                status.next_due=status.next_due+delta
            end
        end
    end
    state.floor_pause[floor_id]=nil
    Scheduler.sort(state)
end

local function pursuer_destination(state,from)
    local map=state.generated_map
    local queue={{x=from.x,y=from.y,distance=0}}
    local seen={[from.x..","..from.y]=true}
    local farthest
    local head=1
    while head<=#queue do
        local p=queue[head]
        if p.distance>0 and not occupied(state,map.floor_id,p.x,p.y,nil) then
            farthest=p
            if p.distance>=5 then return p end
        end
        for _,d in ipairs(directions) do
            local x,y=p.x+d[1],p.y+d[2]
            local key=x..","..y
            if not seen[key] and Content.walkable(map,x,y) then
                seen[key]=true
                queue[#queue+1]={x=x,y=y,distance=p.distance+1}
            end
        end
        head=head+1
    end
    return farthest
end

function Floors.change(state,stair,landing)
    local old_id=state.active_floor_id
    local new_id=stair.to_floor_id
    assert(old_id~=new_id and Floors.map(state,new_id),"invalid stair link")
    local player
    for _,entity in ipairs(state.entities) do
        if entity.id==state.player_id then player=entity;break end
    end
    -- Statuses attached to the travelling player continue in stage time.
    -- Floor-targeted delayed attacks remain with their original floor.
    for _,status in pairs(player.statuses or {}) do
        for _,item in ipairs(state.schedule) do
            if item.id==status.schedule_id then item.floor_id=new_id;break end
        end
    end
    local pursuer
    if state.stage and state.stage.pursuer_id then
        for _,entity in ipairs(state.entities) do
            if entity.id==state.stage.pursuer_id then pursuer=entity;break end
        end
    end
    local interrupted=pursuer and pursuer.pending_attack or nil
    if pursuer then
        Scheduler.remove_for_actor(state,pursuer.id)
        pursuer.pending_attack=nil -- displacement interrupts its locked windup
    end
    Floors.park(state,old_id)
    local new_map=assert(state.inactive_floor_maps[new_id])
    state.inactive_floor_maps[new_id]=nil
    state.inactive_floor_maps[old_id]=state.generated_map
    state.generated_map=new_map
    state.active_floor_id=new_id
    for index,id in ipairs(state.floor_order or {}) do
        if id==new_id then state.active_floor_index=index;break end
    end
    player.position={floor_id=new_id,x=landing.x,y=landing.y}
    Floors.resume(state,new_id)
    local pursuer_position
    if pursuer then
        local destination=assert(pursuer_destination(state,landing),
            "no pursuer landing on connected floor")
        pursuer.position={floor_id=new_id,x=destination.x,y=destination.y}
        pursuer_position={floor_id=new_id,x=destination.x,y=destination.y}
        Scheduler.enqueue(state,{kind="actor_ready",due=state.clock+100,
            actor_id=pursuer.id,is_player=false})
    end
    return pursuer_position,interrupted
end

return Floors
