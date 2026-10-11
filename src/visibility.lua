-- Player knowledge is authoritative plain data. Rendering only queries it.
local Content = require("src.content")

local Visibility = {}
Visibility.radius = 8

local function index(map, x, y)
    return (y - 1) * map.width + x
end

local function line_clear(state, map, x0, y0, x1, y1)
    local dx, dy = math.abs(x1-x0), math.abs(y1-y0)
    local sx, sy = x0<x1 and 1 or -1, y0<y1 and 1 or -1
    local error_value = dx-dy
    local x,y=x0,y0
    while x~=x1 or y~=y1 do
        local twice=2*error_value
        local old_x,old_y=x,y
        if twice > -dy then error_value=error_value-dy; x=x+sx end
        if twice < dx then error_value=error_value+dx; y=y+sy end
        if x~=old_x and y~=old_y
            and Content.blocks_vision(map,x,old_y,state)
            and Content.blocks_vision(map,old_x,y,state) then return false end
        -- A blocking target is itself visible, but hides cells beyond it.
        if (x~=x1 or y~=y1) and Content.blocks_vision(map,x,y,state) then
            return false
        end
    end
    return true
end

function Visibility.recompute(state, radius)
    local map=assert(Content.map_for(state),"missing visibility map")
    local player
    for _,entity in ipairs(state.entities) do
        if entity.id==state.player_id then player=entity; break end
    end
    assert(player,"missing visibility observer")
    radius=radius or Visibility.radius
    assert(type(radius)=="number" and radius%1==0 and radius>=1 and radius<=32,
        "invalid sight radius")
    local floor_id=player.position.floor_id
    state.explored=state.explored or {}
    local explored=state.explored[floor_id]
    if not explored then
        explored={cells={},order={}}
        state.explored[floor_id]=explored
    end
    local visible={}
    local px,py=player.position.x,player.position.y
    for y=math.max(1,py-radius),math.min(map.height,py+radius) do
        for x=math.max(1,px-radius),math.min(map.width,px+radius) do
            local dx,dy=x-px,y-py
            if dx*dx+dy*dy<=radius*radius
                and line_clear(state,map,px,py,x,y) then
                local at=index(map,x,y)
                visible[at]=true
                if not explored.cells[at] then
                    explored.cells[at]=true
                    explored.order[#explored.order+1]=at
                end
            end
        end
    end
    state.visible={floor_id=floor_id,cells=visible,radius=radius}
end

function Visibility.is_visible(state,floor_id,x,y)
    local map=Content.map_for(state)
    return map and floor_id==state.visible.floor_id and x>=1 and x<=map.width
        and y>=1 and y<=map.height and state.visible.cells[index(map,x,y)]==true or false
end

function Visibility.was_explored(state,floor_id,x,y)
    local map=Content.map_for(state)
    local explored=state.explored and state.explored[floor_id]
    return map and explored and x>=1 and x<=map.width and y>=1 and y<=map.height
        and explored.cells[index(map,x,y)]==true or false
end

return Visibility
