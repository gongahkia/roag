local Content=require("src.content")
local Generation=require("src.generation")
local RNG=require("src.rng")
local Scheduler=require("src.scheduler")
local Visibility=require("src.visibility")

local Stage={}
local directions={{0,-1},{1,0},{0,1},{-1,0}} -- stable N,E,S,W

local function copy(value)
    if type(value)~="table" then return value end
    local result={}
    for key,child in pairs(value) do result[key]=copy(child) end
    return result
end

local function occupant(state,x,y)
    for _,entity in ipairs(state.entities) do
        local p=entity.position
        if entity.blocks_movement and p and p.floor_id==state.active_floor_id
            and p.x==x and p.y==y then return entity end
    end
end

local function distance(a,b)
    return math.abs(a.x-b.x)+math.abs(a.y-b.y)
end

local function reachable(state)
    local map=Content.map_for(state)
    local player=state.entities[1]
    local queue={{x=player.position.x,y=player.position.y}}
    local seen={[player.position.y*map.width+player.position.x]=true}
    local head=1
    while head<=#queue do
        local p=queue[head]; head=head+1
        for _,d in ipairs(directions) do
            local x,y=p.x+d[1],p.y+d[2]
            local key=y*map.width+x
            local block=occupant(state,x,y)
            if not seen[key] and Content.walkable(map,x,y)
                and (not block or block.controller=="ai") then
                seen[key]=true
                queue[#queue+1]={x=x,y=y}
            end
        end
    end
    return seen
end

local function legal(state,x,y,minimum,seen)
    local map=Content.map_for(state)
    local player=state.entities[1]
    if not Content.walkable(map,x,y) or occupant(state,x,y)
        or distance(player.position,{x=x,y=y})<minimum
        or Visibility.is_visible(state,state.active_floor_id,x,y)
        or not seen[y*map.width+x] then return false end
    if x==map.start_x and y==map.start_y then return false end
    if x==map.elite.x and y==map.elite.y then return false end
    for _,secret in ipairs(state.secrets) do
        if (secret.x==x and secret.y==y) or
            (secret.cache_x==x and secret.cache_y==y) then return false end
    end
    return true
end

local function anchor_candidates(state,minimum,seen)
    local candidates={}
    for _,room in ipairs(state.generated_map.rooms) do
        if room.role~="start" and room.role~="elite" then
            for _,p in ipairs(room.markers.enemy or {}) do
                if legal(state,p.x,p.y,minimum,seen) then
                    candidates[#candidates+1]={x=p.x,y=p.y}
                end
            end
        end
    end
    return candidates
end

local function free_near(state,x,y,reserved)
    local map=Content.map_for(state)
    local cells={{x=x,y=y}}
    for radius=1,5 do
        for dy=-radius,radius do
            for dx=-radius,radius do
                if math.abs(dx)+math.abs(dy)==radius then
                    cells[#cells+1]={x=x+dx,y=y+dy}
                end
            end
        end
    end
    for _,p in ipairs(cells) do
        local key=p.x..","..p.y
        if Content.walkable(map,p.x,p.y) and not occupant(state,p.x,p.y)
            and not reserved[key] then return p end
    end
    error("elite arena has no free reward/exit cell")
end

function Stage.init(state,spawn_enemy)
    if not state.generated_map then return end
    local options=state.stage_options or {}
    local map=state.generated_map
    local elite=spawn_enemy(state,"core:enemy/thorn_warden",map.elite.x,map.elite.y,0)
    Scheduler.remove_for_actor(state,elite.id,"actor_ready")
    elite.dormant=true
    elite.ai_memory.attacks=0
    local rules=Content.get_stage_rules()
    state.stage={index=options.index or 1,phase="exploring",elite_id=elite.id,
        pressure_rng=RNG.new(RNG.derive(state.root_seed,
            "pressure:"..(options.index or 1)..":"..state.run_seed)),
        pressure_count=0,next_pressure=rules.pressure_first,
        route_history=options.route_history or {},
        diagnostics={},elite_reward_id=nil,exit_id=nil,
        defeat_time=nil,deadline=nil,pursuer_id=nil}
    Scheduler.enqueue(state,{kind="stage_pressure",due=rules.pressure_first})
    state.stage_options=nil
end

function Stage.after_player_action(state,emit)
    local stage=state.stage
    if not stage or stage.phase~="exploring" then return end
    local elite
    for _,entity in ipairs(state.entities) do
        if entity.id==stage.elite_id then elite=entity; break end
    end
    if not elite or not elite.dormant then return end
    local player=state.entities[1]
    if distance(player.position,elite.position)<=Content.get_stage_rules().elite_activation_radius then
        elite.dormant=nil
        stage.phase="elite_active"
        Scheduler.enqueue(state,{kind="actor_ready",due=state.clock,
            actor_id=elite.id,is_player=false})
        emit("EliteEngaged",{elite_id=elite.id,floor_id=state.active_floor_id,
            x=elite.position.x,y=elite.position.y},elite.id)
    end
end

function Stage.on_event(state,event,emit,spawn_chest)
    local stage=state.stage
    if not stage then return end
    if event.kind=="DamageTaken" and event.target_id==stage.elite_id
        and stage.phase=="elite_active" then
        local elite
        for _,entity in ipairs(state.entities) do
            if entity.id==stage.elite_id then elite=entity; break end
        end
        if elite and not elite.phase_two and elite.hp>0
            and elite.hp*100<=elite.max_hp*Content.get_stage_rules().elite_phase_percent then
            elite.phase_two=true
            elite.speed=elite.speed+25
            emit("ElitePhaseChanged",{elite_id=elite.id,phase=2,
                floor_id=state.active_floor_id,x=elite.position.x,y=elite.position.y},elite.id)
        end
    elseif event.kind=="EntityKilled" and event.target_id==stage.elite_id
        and (stage.phase=="exploring" or stage.phase=="elite_active") then
        stage.phase="extraction"
        stage.defeat_time=event.time
        stage.deadline=event.time+Content.get_stage_rules().grace
        local reserved={}
        local chest_at=free_near(state,event.x,event.y,reserved)
        reserved[chest_at.x..","..chest_at.y]=true
        local chest=spawn_chest("core:chest/free_elite",chest_at.x,chest_at.y,
            "elite:stage:"..stage.index)
        stage.elite_reward_id=chest.id
        local exit_at=free_near(state,event.x,event.y,reserved)
        local exit={id=state.next_entity_id,definition_id="core:exit/arena",
            controller="exit",faction="neutral",position={floor_id=state.active_floor_id,
                x=exit_at.x,y=exit_at.y},blocks_movement=true,
            routes=copy(Content.get_stage_rules().routes)}
        state.next_entity_id=exit.id+1
        state.entities[#state.entities+1]=exit
        stage.exit_id=exit.id
        Scheduler.enqueue(state,{kind="stage_deadline",due=stage.deadline})
        emit("EliteDefeated",{elite_id=stage.elite_id,free_chest_id=chest.id,
            exit_id=exit.id,deadline=stage.deadline,floor_id=state.active_floor_id,
            x=event.x,y=event.y},event.source_id)
    end
end

local function pressure_due(state,emit,spawn_enemy)
    local stage=state.stage
    if stage.phase~="exploring" and stage.phase~="elite_active" then return end
    local rules=Content.get_stage_rules()
    local normal=0
    for _,e in ipairs(state.entities) do
        if e.controller=="ai" and e.id~=stage.elite_id and e.id~=stage.pursuer_id then
            normal=normal+1
        end
    end
    local seen=reachable(state)
    local candidates=anchor_candidates(state,rules.pressure_safe_radius,seen)
    if normal<rules.pressure_max_active and #candidates>0 then
        local at=candidates[RNG.next_int(stage.pressure_rng,#candidates)]
        local pool={"core:enemy/mossbound_guard","core:enemy/thornspitter",
            "core:enemy/ruin_skitter"}
        local type_id=pool[RNG.next_int(stage.pressure_rng,#pool)]
        local enemy=spawn_enemy(state,type_id,at.x,at.y,state.clock)
        stage.pressure_count=stage.pressure_count+1
        emit("PressureSpawned",{enemy_id=enemy.id,definition_id=type_id,
            tier=math.min(3,1+math.floor(stage.pressure_count/3)),
            floor_id=state.active_floor_id,x=at.x,y=at.y},enemy.id)
    else
        stage.diagnostics[#stage.diagnostics+1]={time=state.clock,
            kind=normal>=rules.pressure_max_active and "population_cap" or "no_legal_anchor"}
    end
    local tier=math.min(3,1+math.floor(stage.pressure_count/3))
    stage.next_pressure=state.clock+rules.pressure_periods[tier]
    Scheduler.enqueue(state,{kind="stage_pressure",due=stage.next_pressure})
end

local function pursuer_due(state,emit,spawn_enemy)
    local stage=state.stage
    if stage.phase~="extraction" or stage.pursuer_id then return end
    local seen=reachable(state)
    local rules=Content.get_stage_rules()
    local candidates=anchor_candidates(state,rules.pursuer_spawn_distance,seen)
    if #candidates==0 then
        -- Deterministic exhaustive fallback over traversable, reachable, unseen tiles.
        local map=Content.map_for(state)
        for y=2,map.height-1 do
            for x=2,map.width-1 do
                if legal(state,x,y,rules.pursuer_spawn_distance,seen) then
                    candidates[#candidates+1]={x=x,y=y}
                end
            end
        end
    end
    stage.phase="pursuit"
    if #candidates==0 then
        stage.diagnostics[#stage.diagnostics+1]={time=state.clock,kind="no_pursuer_spawn"}
        emit("PursuerSpawnFailed",{deadline=stage.deadline},nil)
        return
    end
    local at=candidates[#candidates] -- far stable anchor, no gameplay RNG consumed
    local pursuer=spawn_enemy(state,"core:enemy/extraction_pursuer",at.x,at.y,state.clock)
    stage.pursuer_id=pursuer.id
    emit("PursuerArrived",{pursuer_id=pursuer.id,deadline=stage.deadline,
        floor_id=state.active_floor_id,x=at.x,y=at.y},pursuer.id)
end

function Stage.resolve_due(state,item,emit,spawn_enemy)
    if not state.stage then return end
    if item.kind=="stage_pressure" then pressure_due(state,emit,spawn_enemy)
    elseif item.kind=="stage_deadline" then pursuer_due(state,emit,spawn_enemy) end
end

function Stage.route_options(state,exit_id)
    local stage=state.stage
    if not stage or stage.exit_id~=exit_id or
        (stage.phase~="extraction" and stage.phase~="pursuit") then
        return nil,"exit unavailable"
    end
    local exit
    for _,entity in ipairs(state.entities) do
        if entity.id==exit_id then exit=entity; break end
    end
    if not exit or exit.controller~="exit" then return nil,"exit unavailable" end
    local player=state.entities[1]
    if distance(player.position,exit.position)~=1 then return nil,"exit not adjacent" end
    return {exit_id=exit_id,selected=1,routes=exit.routes,stage_index=stage.index}
end

function Stage.validate(state)
    local stage=state.stage
    if not stage then return true end
    local phases={exploring=true,elite_active=true,extraction=true,
        pursuit=true,prototype_endpoint=true}
    assert(phases[stage.phase] and stage.index>=1 and stage.index<=3
        and type(stage.route_history)=="table"
        and #stage.route_history==stage.index-1
            +(stage.phase=="prototype_endpoint" and 1 or 0),
        "invalid active stage state")
    local base=Generation.floor_id
    assert(state.active_floor_id==(stage.index==1 and base
        or base.."/stage/"..stage.index),"stage floor identity mismatch")
    assert(type(stage.pressure_count)=="number" and stage.pressure_count>=0
        and type(stage.pressure_rng)=="table", "invalid pressure director")
    RNG.new(stage.pressure_rng.state)
    local ids={}
    for _,entity in ipairs(state.entities) do ids[entity.id]=entity end
    if stage.phase=="exploring" or stage.phase=="elite_active" then
        assert(ids[stage.elite_id] and ids[stage.elite_id].ai_id=="warden",
            "active stage missing Warden")
    else
        assert(not ids[stage.elite_id] and ids[stage.elite_reward_id]
            and ids[stage.elite_reward_id].controller=="chest"
            and ids[stage.exit_id] and ids[stage.exit_id].controller=="exit"
            and type(stage.defeat_time)=="number" and
            stage.deadline==stage.defeat_time+Content.get_stage_rules().grace,
            "invalid extraction state")
        if stage.pursuer_id then
            assert(ids[stage.pursuer_id] and ids[stage.pursuer_id].invulnerable,
                "missing active pursuer")
        end
    end
    return true
end

return Stage
