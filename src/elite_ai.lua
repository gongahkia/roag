-- Six biome elites share the ordinary locked-windup and damage pipeline.
-- Only their plain-data footprint recipes differ; no combat is resolved here.
local EliteAI={}

EliteAI.profiles={
    ["core:enemy/mycelial_matriarch"]={"target_diagonals","source_ring"},
    ["core:enemy/canal_lockmaster"]={"source_lane","target_sidebars"},
    ["core:enemy/ash_marshal"]={"source_fan","target_cross"},
    ["core:enemy/prism_curator"]={"target_star","target_ring"},
    ["core:enemy/index_keeper"]={"target_row","target_columns"},
    ["core:enemy/ember_crucible"]={"source_spokes","target_box"},
}

local function sign(n) return n<0 and -1 or 1 end

local function cells_for(shape,source,target,map,passable)
    local cells,seen={},{}
    local function add(x,y)
        if x>=1 and y>=1 and x<=map.width and y<=map.height
            and passable(map,x,y) then
            local key=x..","..y
            if not seen[key] then
                cells[#cells+1]={x=x,y=y};seen[key]=true
            end
        end
    end
    local x,y=target.x,target.y
    local sx,sy=source.x,source.y
    local dx,dy=x-sx,y-sy
    local fx,fy=0,0
    if math.abs(dx)>=math.abs(dy) then fx=sign(dx) else fy=sign(dy) end
    local px,py=-fy,fx
    if shape=="target_diagonals" then
        add(x,y)
        for _,d in ipairs({{-1,-1},{1,-1},{1,1},{-1,1}}) do add(x+d[1],y+d[2]) end
    elseif shape=="source_ring" then
        for oy=-2,2 do for ox=-2,2 do
            if math.max(math.abs(ox),math.abs(oy))==2 then add(sx+ox,sy+oy) end
        end end
    elseif shape=="source_lane" then
        for step=1,4 do
            local cx,cy=sx+fx*step,sy+fy*step
            if not passable(map,cx,cy) then break end
            add(cx,cy)
        end
    elseif shape=="target_sidebars" then
        for step=-1,1 do
            add(x+px+fx*step,y+py+fy*step)
            add(x-px+fx*step,y-py+fy*step)
        end
    elseif shape=="source_fan" then
        for step=1,2 do for side=-step,step do
            add(sx+fx*step+px*side,sy+fy*step+py*side)
        end end
    elseif shape=="target_cross" then
        add(x,y)
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do add(x+d[1],y+d[2]) end
    elseif shape=="target_star" then
        add(x,y)
        for distance=1,2 do
            for _,d in ipairs({{-1,-1},{1,-1},{1,1},{-1,1}}) do
                add(x+d[1]*distance,y+d[2]*distance)
            end
        end
    elseif shape=="target_ring" then
        for oy=-2,2 do for ox=-2,2 do
            if math.max(math.abs(ox),math.abs(oy))==2 then add(x+ox,y+oy) end
        end end
    elseif shape=="target_row" then
        for offset=-2,2 do add(x+offset,y) end
    elseif shape=="target_columns" then
        for offset=-1,1 do
            add(x-1,y+offset);add(x+1,y+offset)
        end
    elseif shape=="source_spokes" then
        for step=1,3 do
            add(sx+fx*step,sy+fy*step)
            add(sx+px*step,sy+py*step)
            add(sx-px*step,sy-py*step)
        end
    elseif shape=="target_box" then
        for oy=-1,1 do for ox=-1,1 do add(x+ox,y+oy) end end
    end
    return cells
end

function EliteAI.choose(state,enemy,player,helpers)
    local profile=assert(EliteAI.profiles[enemy.definition_id],"unknown biome elite")
    local definition=helpers.definition
    local dx=player.position.x-enemy.position.x
    local dy=player.position.y-enemy.position.y
    local distance=math.abs(dx)+math.abs(dy)
    if distance<=definition.range and
        helpers.clear_line(state,enemy.position,player.position) then
        local attacks=enemy.ai_memory.attacks or 0
        local index=(attacks+(enemy.phase_two and 1 or 0))%2+1
        local shape=profile[index]
        local cells=cells_for(shape,enemy.position,player.position,
            helpers.map,helpers.projectile_passable)
        if #cells>0 then
            return {kind="windup",ability_id="core:ability/elite_"..shape,
                cells=cells,ranged="area"}
        end
    end
    return helpers.next_step(state,enemy,
        helpers.adjacent_goals(state,enemy,player,false)) or {kind="wait"}
end

function EliteAI.validate()
    local seen={}
    for id,profile in pairs(EliteAI.profiles) do
        assert(type(id)=="string" and #profile==2 and profile[1]~=profile[2])
        for _,shape in ipairs(profile) do
            assert(not seen[id..shape],"duplicate elite footprint")
            seen[id..shape]=true
        end
    end
    return true
end

return EliteAI
