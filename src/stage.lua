local Content=require("src.content")
local Generation=require("src.generation")
local Biomes=require("src.biomes")
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

local function reachable(state,avoid_x,avoid_y,allow_doors)
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
                and not (x==avoid_x and y==avoid_y)
                and (not block or block.controller=="ai" or
                    (allow_doors and block.controller=="door")) then
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
    local added={}
    local offsets={{0,0},{0,-1},{1,0},{0,1},{-1,0},
        {-1,-1},{1,-1},{1,1},{-1,1},{0,-2},{2,0},{0,2},{-2,0}}
    for _,room in ipairs(state.generated_map.rooms) do
        if room.role~="start" and room.role~="elite" then
            for _,p in ipairs(room.markers.enemy or {}) do
                for _,d in ipairs(offsets) do
                    local x,y=p.x+d[1],p.y+d[2]
                    local k=x..","..y
                    if not added[k] and legal(state,x,y,minimum,seen) then
                        candidates[#candidates+1]={x=x,y=y}
                        added[k]=true
                    end
                end
            end
        end
    end
    return candidates
end

local function free_near(state,x,y,reserved,accept)
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
            and not reserved[key] and (not accept or accept(p)) then return p end
    end
    error("elite arena has no free reward/exit cell")
end

function Stage.init(state,spawn_enemy)
    if not state.generated_map then return end
    local options=state.stage_options or {}
    local map=state.generated_map
    local biome=assert(Biomes.get(map.biome_id),"unknown elite biome")
    local elite=spawn_enemy(state,biome.elite_id,map.elite.x,map.elite.y,0)
    Scheduler.remove_for_actor(state,elite.id,"actor_ready")
    elite.dormant=true
    elite.ai_memory.attacks=0
    local rules=Content.get_stage_rules()
    state.stage={index=options.index or 1,biome_id=map.biome_id or Biomes.order[1],
        phase="exploring",elite_id=elite.id,elite_floor_id=map.floor_id,
        elite_definition_id=biome.elite_id,
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
    if player.position.floor_id==elite.position.floor_id
        and distance(player.position,elite.position)<=Content.get_stage_rules().elite_activation_radius then
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
        local map=Content.map_for(state)
        local exit_at=free_near(state,event.x,event.y,reserved,function(candidate)
            local seen=reachable(state,candidate.x,candidate.y,true)
            for _,d in ipairs(directions) do
                local x,y=chest_at.x+d[1],chest_at.y+d[2]
                if seen[y*map.width+x] then return true end
            end
            return false
        end)
        local exit={id=state.next_entity_id,definition_id="core:exit/arena",
            controller="exit",faction="neutral",position={floor_id=state.active_floor_id,
                x=exit_at.x,y=exit_at.y},blocks_movement=true,
            routes=copy(Biomes.routes_for(stage.index,stage.route_history))}
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
        if e.controller=="ai" and e.position.floor_id==state.active_floor_id
            and e.id~=stage.elite_id and e.id~=stage.pursuer_id then
            normal=normal+1
        end
    end
    local seen=reachable(state)
    local candidates=anchor_candidates(state,rules.pressure_safe_radius,seen)
    if normal<rules.pressure_max_active and #candidates>0 then
        local at=candidates[RNG.next_int(stage.pressure_rng,#candidates)]
        local pool=assert(Biomes.get(stage.biome_id)).enemy_ids
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
    if player.position.floor_id~=exit.position.floor_id
        or distance(player.position,exit.position)~=1 then
        return nil,"exit not adjacent"
    end
    return {exit_id=exit_id,selected=1,routes=exit.routes,stage_index=stage.index}
end

function Stage.validate(state)
    local stage=state.stage
    if not stage then return true end
    local phases={exploring=true,elite_active=true,extraction=true,
        pursuit=true}
    assert(phases[stage.phase] and stage.index>=1 and stage.index<=3
        and type(stage.route_history)=="table"
        and #stage.route_history==stage.index-1,
        "invalid active stage state")
    local biome=assert(Biomes.get(stage.biome_id),"invalid stage biome")
    assert(stage.elite_definition_id==nil
        or stage.elite_definition_id==biome.elite_id,
        "stage elite content mismatch")
    local base=biome.floor_id
    assert(state.generated_map.biome_id==biome.id,"stage biome mismatch")
    if stage.index==1 then
        assert(stage.biome_id==Biomes.order[1],"first stage biome mismatch")
    else
        local visited={}
        for i=1,stage.index-1 do
            local route_id=stage.route_history[i]
            local chosen=type(route_id)=="string" and
                route_id:gsub("^core:route/","core:biome/") or nil
            assert(chosen and Biomes.get(chosen) and chosen~=Biomes.order[1]
                and not visited[chosen],"invalid or repeated biome route")
            visited[chosen]=true
            if i==stage.index-1 then
                assert(chosen==stage.biome_id,"route destination mismatch")
            end
        end
    end
    local expected=stage.index==1 and base or base.."/stage/"..stage.index
    if state.floor_order then
        assert(#state.floor_order==2 and
            state.floor_order[1]==base.."/stage/"..stage.index.."/depth/1"
            and state.floor_order[2]==base.."/stage/"..stage.index.."/depth/2"
            and state.active_floor_id==state.floor_order[state.active_floor_index]
            and stage.elite_floor_id==state.floor_order[2],
            "connected stage floor identity mismatch")
    else
        assert(state.active_floor_id==expected,"stage floor identity mismatch")
    end
    assert(type(stage.pressure_count)=="number" and stage.pressure_count>=0
        and type(stage.pressure_rng)=="table", "invalid pressure director")
    RNG.new(stage.pressure_rng.state)
    local ids={}
    for _,entity in ipairs(state.entities) do ids[entity.id]=entity end
    if stage.phase=="exploring" or stage.phase=="elite_active" then
        local elite=ids[stage.elite_id]
        assert(elite and elite.definition_id==
            (stage.elite_definition_id or "core:enemy/thorn_warden")
            and (elite.ai_id=="warden" or elite.ai_id=="biome_elite"),
            "active stage missing biome elite")
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
