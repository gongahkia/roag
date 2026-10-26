-- Read-only tactical decisions for expansion enemies. Effects stay plain data
-- on the ordinary locked windup and resolve in game.lua at the due timestamp.
local RegularAI={}
RegularAI.roles={
    ["core:enemy/mossbreaker"]="breaker",
    ["core:enemy/gloom_strider"]="skirmisher",
    ["core:enemy/mire_binder"]="denial",
    ["core:enemy/pack_chorister"]="coordinator",
    ["core:enemy/vault_lurker"]="ambusher",
    ["core:enemy/crystal_bombardier"]="artillery",
    ["core:enemy/cinder_mender"]="support",
    ["core:enemy/glass_charger"]="charger",
    ["core:enemy/forge_conjurer"]="summoner",
    ["core:enemy/bastion_bulwark"]="retaliator",
}
local directions={{0,-1},{1,0},{0,1},{-1,0}}

local function cell(x,y) return {x=x,y=y} end
local function windup(name,cells,ranged,extra)
    local intent={kind="windup",ability_id="core:ability/"..name,
        cells=cells,ranged=ranged}
    for key,value in pairs(extra or {}) do intent[key]=value end
    return intent
end
local function at(state,enemy,x,y)
    for _,other in ipairs(state.entities) do
        local p=other.position
        if other.id~=enemy.id and other.hp and other.hp>0 and p
            and p.floor_id==enemy.position.floor_id and p.x==x and p.y==y then
            return other
        end
    end
end
local function allies(state,enemy,radius)
    local found={}
    for _,other in ipairs(state.entities) do
        local p=other.position
        if other.id~=enemy.id and other.controller=="ai" and other.hp>0
            and p.floor_id==enemy.position.floor_id
            and math.abs(p.x-enemy.position.x)+math.abs(p.y-enemy.position.y)<=radius then
            found[#found+1]=other
        end
    end
    table.sort(found,function(a,b) return a.id<b.id end)
    return found
end
local function area(map,passable,x,y,offsets)
    local result={}
    for _,d in ipairs(offsets) do
        local cx,cy=x+d[1],y+d[2]
        if passable(map,cx,cy) then result[#result+1]=cell(cx,cy) end
    end
    return result
end

function RegularAI.choose(state,enemy,player,h)
    local role=assert(RegularAI.roles[enemy.definition_id],"unknown regular role")
    local p,e=player.position,enemy.position
    local dx,dy=p.x-e.x,p.y-e.y
    local distance=math.abs(dx)+math.abs(dy)
    local map=h.map
    local passable=h.projectile_passable
    local line=distance<=h.definition.range and h.clear_line(state,e,p)
    local function close()
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false))
            or {kind="wait"}
    end
    if role=="breaker" then
        local best
        for _,d in ipairs(directions) do
            local x,y=e.x+d[1],e.y+d[2]
            if map.rows[y] and map.rows[y]:sub(x,x)=="B" then
                local score=math.abs(p.x-x)+math.abs(p.y-y)
                if not best or score<best.score then best={x=x,y=y,score=score} end
            end
        end
        if best then return windup("mossbreaker_crack",{cell(best.x,best.y)},false) end
        if distance==1 then
            return windup("mossbreaker_cleave",{cell(p.x,p.y)},false)
        end
        return close()
    elseif role=="skirmisher" then
        if distance<=2 then
            for _,d in ipairs(directions) do
                local x,y=e.x+d[1],e.y+d[2]
                if h.free(state,enemy.id,x,y)
                    and math.abs(p.x-x)+math.abs(p.y-y)>distance then
                    return {kind="move",dx=d[1],dy=d[2]}
                end
            end
        end
        if line then
            local offsets={{0,0}}
            if math.abs(dx)>=math.abs(dy) then
                offsets[2]={0,-1};offsets[3]={0,1}
            else offsets[2]={-1,0};offsets[3]={1,0} end
            return windup("gloom_fork",area(map,passable,p.x,p.y,offsets),"area")
        end
        return close()
    elseif role=="denial" then
        if line then
            -- Safe center, dangerous neighboring exits: a locked containment ring.
            local offsets={{-1,-1},{0,-1},{1,-1},{1,0},
                {1,1},{0,1},{-1,1},{-1,0}}
            return windup("mire_ring",area(map,passable,p.x,p.y,offsets),"area")
        end
        return close()
    elseif role=="coordinator" then
        if distance==1 then
            local nearby=allies(state,enemy,2)
            return windup("pack_chorus",{cell(p.x,p.y)},false,
                {damage_bonus=math.min(2,#nearby)})
        end
        return close()
    elseif role=="ambusher" then
        if distance>5 and not enemy.ai_memory.alerted then return {kind="wait"} end
        if line then
            return windup("vault_ambush",area(map,passable,p.x,p.y,
                {{0,0},{-1,-1},{1,-1},{1,1},{-1,1}}),"area",
                {alert=true})
        end
        local intent=close()
        intent.alert=true
        return intent
    elseif role=="artillery" then
        if line then
            return windup("crystal_barrage",area(map,passable,p.x,p.y,
                {{0,0},{0,-2},{2,0},{0,2},{-2,0}}),"area")
        end
        return close()
    elseif role=="support" then
        local wounded
        for _,ally in ipairs(allies(state,enemy,3)) do
            if ally.hp<ally.max_hp and
                (not wounded or ally.hp* wounded.max_hp < wounded.hp*ally.max_hp) then
                wounded=ally
            end
        end
        if wounded then
            return windup("cinder_mend",{cell(wounded.position.x,wounded.position.y)},
                false,{support={kind="heal",power=3}})
        end
        if distance==1 then return windup("cinder_tap",{cell(p.x,p.y)},false) end
        return close()
    elseif role=="charger" then
        if distance>=2 and distance<=4 and (dx==0 or dy==0) and line then
            local sx=dx==0 and 0 or dx/math.abs(dx)
            local sy=dy==0 and 0 or dy/math.abs(dy)
            local cells={}
            for step=1,distance do cells[#cells+1]=cell(e.x+sx*step,e.y+sy*step) end
            return windup("glass_charge",cells,"piercing_line",
                {advance={dx=sx,dy=sy,max_steps=math.min(3,distance)}})
        end
        if distance==1 then return windup("glass_jab",{cell(p.x,p.y)},false) end
        return close()
    elseif role=="summoner" then
        local nearby=allies(state,enemy,5)
        if distance<=5 and #nearby<3 and (enemy.ai_memory.summons or 0)<3 then
            for _,d in ipairs(directions) do
                local x,y=e.x+d[1],e.y+d[2]
                if h.free(state,enemy.id,x,y) and not at(state,enemy,x,y) then
                    return windup("forge_call",{cell(x,y)},false,
                        {support={kind="summon",enemy_id="core:enemy/ruin_skitter",
                            max_active=3}})
                end
            end
        end
        if distance==1 then return windup("forge_tap",{cell(p.x,p.y)},false) end
        return close()
    elseif role=="retaliator" then
        if distance<=2 and enemy.ai_memory.retaliate then
            local cells=area(map,passable,p.x,p.y,{{0,0},{0,-1},{1,0},{0,1},{-1,0}})
            return windup("bulwark_retort",cells,"area",
                {clear_retaliate=true})
        end
        if distance==1 then return windup("bulwark_bash",{cell(p.x,p.y)},false) end
        return close()
    end
end

function RegularAI.validate()
    local count=0
    for id,role in pairs(RegularAI.roles) do
        assert(id:match("^core:enemy/[a-z_]+$") and type(role)=="string")
        count=count+1
    end
    assert(count==10,"expected exactly ten expansion regulars")
    return true
end
return RegularAI
