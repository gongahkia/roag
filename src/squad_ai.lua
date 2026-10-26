-- Read-only squad decisions. Windups, movement and recovery use the ordinary
-- scheduler. No off-turn facing changes, heals, damage or reinforcement spawns.
local S=require('src.enemy_squads')
local A={}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
local function wind(name,cells,extra)
    local out={kind='windup',ability_id='core:ability/'..name,cells=cells}
    for k,v in pairs(extra or {}) do out[k]=v end
    return out
end
local function cell(p) return {x=p.x,y=p.y} end
function A.choose(state,e,player,h)
    local role=assert(S.roles[e.definition_id])
    local p,pos=player.position,e.position
    local distance=S.distance(pos,p)
    local line=distance<=h.definition.range and h.clear_line(state,pos,p)
    local function close(target,diagonal)
        return h.next_step(state,e,h.adjacent_goals(state,e,target or player,diagonal))
            or {kind='wait'}
    end
    local function retreat()
        for _,d in ipairs(dirs) do
            local x,y=pos.x+d[1],pos.y+d[2]
            if h.free(state,e.id,x,y) and S.distance({x=x,y=y},p)>distance then
                return {kind='move',dx=d[1],dy=d[2]}
            end
        end
    end
    local function tap(name)
        if distance==1 then return wind(name,{cell(p)}) end
        return close()
    end
    if role=='ward_weaver' then
        if not e.ai_memory.ward_ready and distance<=8 then
            return wind('ward_channel',{cell(pos)},{support={kind='ward'}})
        end
        if distance<=2 then return retreat() or tap('ward_tap') end
        return {kind='wait'}
    elseif role=='bodyguard_brute' then
        local leader=S.leader(state,e)
        if leader and S.distance(pos,leader.position)>S.guard_leash then return close(leader) end
        if distance==1 then
            local dx,dy=p.x-pos.x,p.y-pos.y
            local cells={cell(p)}
            for _,side in ipairs({-1,1}) do
                local x,y=p.x-dy*side,p.y+dx*side
                if h.projectile_passable(h.map,x,y) then cells[#cells+1]={x=x,y=y} end
            end
            return wind('brute_sweep',cells)
        end
        local move=close()
        if leader and move.kind=='move' and S.distance(
            {x=pos.x+move.dx,y=pos.y+move.dy},leader.position)>S.guard_leash then
            return {kind='wait'}
        end
        return move
    elseif role=='mend_acolyte' then
        local wounded
        for _,ally in ipairs(state.entities) do
            if ally.id~=e.id and ally.squad_id and ally.squad_id==e.squad_id and
                ally.hp>0 and ally.hp<ally.max_hp and
                S.distance(pos,ally.position)<=4 and S.clear(state,pos,ally.position) and
                (not wounded or ally.hp*wounded.max_hp<wounded.hp*ally.max_hp) then
                wounded=ally
            end
        end
        if wounded then return wind('acolyte_mend',{cell(wounded.position)},
            {support={kind='heal',power=2,target_id=wounded.id}}) end
        if distance<=2 then return retreat() or tap('acolyte_tap') end
        if distance>5 then return close() end
        return {kind='wait'}
    elseif role=='rushwing' or role=='banner_follower' then
        if distance==1 then return wind(role=='rushwing' and 'rushwing_bite' or 'follower_strike',
            {cell(p)}) end
        local leader=S.leader(state,e)
        if role=='rushwing' and leader and e.hp<e.max_hp and distance>2 and
            S.distance(pos,leader.position)>2 then return close(leader) end
        -- Give each member a different first-choice approach. Fall back to BFS
        -- when its preferred side is blocked; no teleport or free side step.
        local preferred=dirs[e.id%4+1]
        local x,y=p.x+preferred[1],p.y+preferred[2]
        if h.free(state,e.id,x,y) then
            local next_move=h.next_step(state,e,{[(y-1)*h.map.width+x]=true})
            if next_move then return next_move end
        end
        return close()
    elseif role=='snare_scribe' then
        local count,used=0,{}
        for _,trap in ipairs(state.enemy_snares or {}) do
            if trap.source_id==e.id then count=count+1 end
            if trap.floor_id==pos.floor_id then used[trap.x..','..trap.y]=true end
        end
        local escapes={}
        for _,d in ipairs(dirs) do
            local x,y=p.x+d[1],p.y+d[2]
            if h.free(state,player.id,x,y) and not used[x..','..y] then
                escapes[#escapes+1]={x=x,y=y}
            end
        end
        if line and count<S.snare_cap and #escapes>=2 then
            local chosen=escapes[e.id%#escapes+1]
            return wind('scribe_snare',{chosen},{support={kind='snare'}})
        end
        if distance<=2 then return retreat() or tap('scribe_tap') end
        return {kind='wait'}
    elseif role=='siege_wisp' then
        if line then
            -- Compact 2x2 blast leaves different routes from the existing star.
            local cells={}
            for _,d in ipairs({{0,0},{1,0},{0,1},{1,1}}) do
                local x,y=p.x+d[1],p.y+d[2]
                if h.projectile_passable(h.map,x,y) then cells[#cells+1]={x=x,y=y} end
            end
            return wind('siege_square',cells,{ranged='area'})
        end
        return close()
    elseif role=='shield_bearer' then
        local dx,dy=p.x-pos.x,p.y-pos.y
        local fx,fy=0,0
        if math.abs(dx)>=math.abs(dy) then fx=dx>=0 and 1 or -1
        else fy=dy>=0 and 1 or -1 end
        local out=tap('shield_bash')
        out.shield_dx,out.shield_dy=fx,fy
        return out
    elseif role=='pin_archer' then
        local dx,dy=p.x-pos.x,p.y-pos.y
        if (dx==0 or dy==0) and distance>=2 and line then
            local sx,sy=dx==0 and 0 or dx/math.abs(dx),dy==0 and 0 or dy/math.abs(dy)
            local cells={}
            local blocked=false
            for i=1,distance do
                local x,y=pos.x+sx*i,pos.y+sy*i
                for _,other in ipairs(state.entities) do
                    if other.id~=e.id and other.faction==e.faction and other.hp and other.hp>0 and
                        other.position.floor_id==pos.floor_id and other.position.x==x and
                        other.position.y==y then blocked=true end
                end
                cells[#cells+1]={x=x,y=y}
            end
            if not blocked then return wind('pin_shot',cells,{ranged=true}) end
        end
        if distance<=2 then return retreat() or tap('pin_tap') end
        local goals={}
        for _,d in ipairs(dirs) do for i=2,h.definition.range do
            local x,y=p.x+d[1]*i,p.y+d[2]*i
            if h.free(state,e.id,x,y) and h.clear_line(state,{x=x,y=y},p) then
                goals[(y-1)*h.map.width+x]=true
            end
        end end
        return h.next_step(state,e,goals) or {kind='wait'}
    elseif role=='banner_captain' then
        return tap('captain_cut')
    end
end
return A
