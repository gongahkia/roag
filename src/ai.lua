local Content = require("src.content")

local AI = {}
local directions = {{0,-1},{1,0},{0,1},{-1,0}} -- stable N, E, S, W

local function occupied(state, actor_id, x, y)
    for _, entity in ipairs(state.entities) do
        local position = entity.position
        if entity.id ~= actor_id and entity.blocks_movement and position
            and position.floor_id == state.active_floor_id and position.x == x and position.y == y then
            return true
        end
    end
    return false
end

local function free(state, actor_id, x, y)
    return Content.walkable(Content.map_for(state), x, y)
        and not occupied(state, actor_id, x, y)
end

local function lane(state, enemy, player, minimum, maximum)
    local a, b = enemy.position, player.position
    local dx, dy = b.x - a.x, b.y - a.y
    if dx ~= 0 and dy ~= 0 then return nil end
    local distance = math.abs(dx) + math.abs(dy)
    if distance < minimum or distance > maximum then return nil end
    local step_x = dx == 0 and 0 or dx / math.abs(dx)
    local step_y = dy == 0 and 0 or dy / math.abs(dy)
    local map = Content.map_for(state)
    local cells = {}
    for step = 1, distance do
        local x, y = a.x + step_x * step, a.y + step_y * step
        if not Content.projectile_passable(map, x, y, state) then return nil end
        cells[#cells + 1] = {x=x, y=y}
    end
    return cells
end

local function clear_line(state,from,to)
    local map=Content.map_for(state)
    local x,y=from.x,from.y
    local dx,dy=math.abs(to.x-x),math.abs(to.y-y)
    local sx,sy=to.x>x and 1 or -1,to.y>y and 1 or -1
    local err=dx-dy
    while x~=to.x or y~=to.y do
        local old_x,old_y=x,y
        local twice=2*err
        if twice>-dy then err=err-dy;x=x+sx end
        if twice<dx then err=err+dx;y=y+sy end
        if x~=old_x and y~=old_y and
            (not Content.projectile_passable(map,x,old_y,state)
                or not Content.projectile_passable(map,old_x,y,state)) then
            return false
        end
        if not Content.projectile_passable(map,x,y,state) then return false end
    end
    return true
end

local function goal_key(map, x, y)
    return (y - 1) * map.width + x
end

local function next_step(state, actor, goals)
    local map = Content.map_for(state)
    local start = actor.position
    -- One local occupancy snapshot per BFS. The old free() call rescanned every
    -- entity for every candidate tile, which grew sharply in crowded floors.
    -- This is read-only and discarded after the decision, so doors/movement
    -- cannot leave stale path data between decisions.
    local blocked = {}
    for _,entity in ipairs(state.entities) do
        local position=entity.position
        if entity.id~=actor.id and entity.blocks_movement and position
            and position.floor_id==state.active_floor_id then
            blocked[goal_key(map,position.x,position.y)]=true
        end
    end
    local queue = {{x=start.x, y=start.y}}
    local seen = {[goal_key(map, start.x, start.y)] = true}
    local head = 1
    while head <= #queue do
        local current = queue[head]
        head = head + 1
        for _, direction in ipairs(directions) do
            local x, y = current.x + direction[1], current.y + direction[2]
            local key = goal_key(map, x, y)
            if not seen[key] and Content.walkable(map,x,y) and not blocked[key] then
                seen[key] = true
                local first_dx = current.first_dx or direction[1]
                local first_dy = current.first_dy or direction[2]
                if goals[key] then return {kind="move", dx=first_dx, dy=first_dy} end
                queue[#queue + 1] = {x=x, y=y, first_dx=first_dx, first_dy=first_dy}
            end
        end
    end
    return nil
end

local function adjacent_goals(state, enemy, player, diagonal)
    local map = Content.map_for(state)
    local goals = {}
    local offsets = diagonal and {{-1,-1},{1,-1},{1,1},{-1,1}} or directions
    for _, offset in ipairs(offsets) do
        local x, y = player.position.x + offset[1], player.position.y + offset[2]
        if free(state, enemy.id, x, y) then goals[goal_key(map, x, y)] = true end
    end
    return goals
end

local function lane_goals(state, enemy, player, definition)
    local map = Content.map_for(state)
    local goals = {}
    for _, direction in ipairs(directions) do
        for distance = definition.min_range, definition.range do
            local x = player.position.x + direction[1] * distance
            local y = player.position.y + direction[2] * distance
            if free(state, enemy.id, x, y) then
                local mock = {position={x=x,y=y}}
                if lane(state, mock, player, definition.min_range, definition.range) then
                    goals[goal_key(map, x, y)] = true
                end
            end
        end
    end
    return goals
end

function AI.choose(state, enemy)
    local player = state.entities[1]
    for _, entity in ipairs(state.entities) do
        if entity.id == state.player_id then player = entity; break end
    end
    local definition = assert(Content.get_enemy(enemy.definition_id), "unknown AI content")
    local dx = player.position.x - enemy.position.x
    local dy = player.position.y - enemy.position.y
    local distance = math.abs(dx) + math.abs(dy)

    if definition.ai=="hollow_regent" then
        local attacks=enemy.ai_memory.attacks or 0
        local pattern=attacks%3
        local line=lane(state,enemy,player,1,5)
        if pattern==0 and line then
            return {kind="windup",ability_id="core:ability/regent_line",
                cells=line,ranged="piercing_line"}
        end
        if pattern==1 and distance<=2 then
            local fx,fy=0,0
            if math.abs(dx)>=math.abs(dy) then fx=dx>=0 and 1 or -1
            else fy=dy>=0 and 1 or -1 end
            local sx,sy=-fy,fx
            local cells={}
            local map=Content.map_for(state)
            for step=1,2 do
                for side=-1,1 do
                    local x=enemy.position.x+fx*step+sx*side
                    local y=enemy.position.y+fy*step+sy*side
                    if Content.projectile_passable(map,x,y,state) then
                        cells[#cells+1]={x=x,y=y}
                    end
                end
            end
            return {kind="windup",ability_id="core:ability/regent_crown",
                cells=cells,ranged="area"}
        end
        if distance<=definition.range and clear_line(state,enemy.position,player.position) then
            local cells={}
            local map=Content.map_for(state)
            for oy=-1,1 do
                for ox=-1,1 do
                    local x,y=player.position.x+ox,player.position.y+oy
                    if Content.projectile_passable(map,x,y,state) then
                        cells[#cells+1]={x=x,y=y}
                    end
                end
            end
            return {kind="windup",ability_id="core:ability/regent_sigil",
                cells=cells,ranged="area"}
        end
        return next_step(state,enemy,adjacent_goals(state,enemy,player,false))
            or {kind="wait"}
    end

    if definition.ai == "pursuer" then
        if distance == 1 then
            return {kind="windup",ability_id="core:ability/pursuer_strike",
                cells={{x=player.position.x,y=player.position.y}},ranged=false}
        end
        return next_step(state,enemy,adjacent_goals(state,enemy,player,false)) or {kind="wait"}
    end
    if definition.ai == "warden" then
        local line=lane(state,enemy,player,1,definition.range)
        local adjacent=distance==1
        local attacks=enemy.ai_memory.attacks or 0
        if line and (not adjacent or attacks%2==1) then
            return {kind="windup",ability_id="core:ability/warden_roots",
                cells=line,ranged=true}
        end
        if adjacent then
            local cells={{x=player.position.x,y=player.position.y}}
            if dx~=0 then
                cells[#cells+1]={x=player.position.x,y=player.position.y-1}
                cells[#cells+1]={x=player.position.x,y=player.position.y+1}
            else
                cells[#cells+1]={x=player.position.x-1,y=player.position.y}
                cells[#cells+1]={x=player.position.x+1,y=player.position.y}
            end
            return {kind="windup",ability_id="core:ability/warden_sweep",
                cells=cells,ranged=false}
        end
        return next_step(state,enemy,adjacent_goals(state,enemy,player,false)) or {kind="wait"}
    end

    if definition.ai == "guard" then
        if distance == 1 then
            return {kind="windup", ability_id="core:ability/guard_strike",
                cells={{x=player.position.x,y=player.position.y}}, ranged=false}
        end
        return next_step(state, enemy, adjacent_goals(state, enemy, player, false)) or {kind="wait"}
    end
    if definition.ai=="spore_herald" then
        if distance<=1 then
            for _,d in ipairs(directions) do
                local x,y=enemy.position.x+d[1],enemy.position.y+d[2]
                if free(state,enemy.id,x,y) and
                    math.abs(player.position.x-x)+math.abs(player.position.y-y)>distance
                    and clear_line(state,{x=x,y=y},player.position) then
                    return {kind="move",dx=d[1],dy=d[2]}
                end
            end
        end
        if distance<=definition.range and clear_line(state,enemy.position,player.position) then
            local cells={{x=player.position.x,y=player.position.y}}
            local offsets=math.abs(dx)>=math.abs(dy)
                and {{0,-1},{0,1}} or {{-1,0},{1,0}}
            local map=Content.map_for(state)
            for _,offset in ipairs(offsets) do
                local x,y=player.position.x+offset[1],player.position.y+offset[2]
                if Content.projectile_passable(map,x,y,state) then
                    cells[#cells+1]={x=x,y=y}
                end
            end
            return {kind="windup",ability_id="core:ability/spore_burst",
                cells=cells,ranged="area"}
        end
        return next_step(state,enemy,lane_goals(state,enemy,player,
            {min_range=2,range=definition.range})) or {kind="wait"}
    end
    if definition.ai=="canal_lancer" then
        local cells=lane(state,enemy,player,1,2)
        if cells then
            local step_x=dx==0 and 0 or dx/math.abs(dx)
            local step_y=dy==0 and 0 or dy/math.abs(dy)
            local map=Content.map_for(state)
            local x,y=enemy.position.x+2*step_x,enemy.position.y+2*step_y
            if #cells==1 and Content.projectile_passable(map,x,y,state) then
                cells[#cells+1]={x=x,y=y}
            end
            return {kind="windup",ability_id="core:ability/canal_thrust",
                cells=cells,ranged="piercing_line"}
        end
        return next_step(state,enemy,lane_goals(state,enemy,player,
            {min_range=1,range=2})) or {kind="wait"}
    end
    if definition.ai=="cinder_bellkeeper" then
        local sight=lane(state,enemy,player,1,definition.range)
        if sight then
            local map=Content.map_for(state)
            local cells={{x=player.position.x,y=player.position.y}}
            for _,d in ipairs(directions) do
                local x,y=player.position.x+d[1],player.position.y+d[2]
                if Content.projectile_passable(map,x,y,state) then
                    cells[#cells+1]={x=x,y=y}
                end
            end
            return {kind="windup",ability_id="core:ability/bell_cross",
                cells=cells,ranged="area"}
        end
        return next_step(state,enemy,lane_goals(state,enemy,player,
            {min_range=2,range=definition.range})) or {kind="wait"}
    end
    if definition.ai == "spitter" then
        local cells = lane(state, enemy, player, definition.min_range, definition.range)
        if cells then
            return {kind="windup", ability_id="core:ability/thorn_lane",
                cells=cells, ranged=true}
        end
        return next_step(state, enemy, lane_goals(state, enemy, player, definition)) or {kind="wait"}
    end
    if definition.ai == "skitter" then
        if math.abs(dx) == 1 and math.abs(dy) == 1 then
            return {kind="windup", ability_id="core:ability/skitter_stab",
                cells={{x=player.position.x,y=player.position.y}}, ranged=false}
        end
        local flank = next_step(state, enemy, adjacent_goals(state, enemy, player, true))
        if flank then return flank end
        if math.max(math.abs(dx), math.abs(dy)) == 1 then
            return {kind="windup", ability_id="core:ability/skitter_stab",
                cells={{x=player.position.x,y=player.position.y}}, ranged=false}
        end
        return next_step(state, enemy, adjacent_goals(state, enemy, player, false)) or {kind="wait"}
    end
    error("unknown AI behavior")
end

return AI
