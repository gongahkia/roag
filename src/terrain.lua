-- Shared semantic terrain data. Rows contain symbols; art and derived atlases
-- remain presentation-only. Values here are provisional and deterministic.
local Terrain={version="roeg-terrain/1"}

Terrain.order={"b","g","i","B","m","c"}
Terrain.definitions={
    ["."]={id="core:terrain/floor",name="Floor",walkable=true,
        blocks_vision=false,projectile_passable=true},
    ["#"]={id="core:terrain/wall",name="Wall",walkable=false,
        blocks_vision=true,projectile_passable=false},
    ["~"]={id="core:terrain/water",name="Shallow water",walkable=false,
        blocks_vision=false,projectile_passable=true},
    b={id="core:terrain/bramble_bed",name="Bramble bed",walkable=true,
        blocks_vision=false,projectile_passable=true,contact_damage=1},
    g={id="core:terrain/sticky_bog",name="Sticky bog",walkable=true,
        blocks_vision=false,projectile_passable=true,walk_extra=40,dash_extra=20},
    i={id="core:terrain/glazed_ice",name="Glazed ice",walkable=true,
        blocks_vision=false,projectile_passable=true,dash_extend=true},
    B={id="core:terrain/barricade",name="Breakable barricade",walkable=false,
        blocks_vision=true,projectile_passable=false,breakable=true},
    m={id="core:terrain/obscuring_mist",name="Obscuring mist",walkable=true,
        blocks_vision=true,projectile_passable=true},
    c={id="core:terrain/resonant_crystal",name="Resonant crystal",walkable=true,
        blocks_vision=false,projectile_passable=true,crystal=true},
}

function Terrain.get(symbol) return Terrain.definitions[symbol] end
function Terrain.symbol(map,x,y)
    if not map or x<1 or x>map.width or y<1 or y>map.height then return nil end
    return map.rows[y]:sub(x,x)
end
function Terrain.at(map,x,y) return Terrain.get(Terrain.symbol(map,x,y)) end
function Terrain.is_valid(symbol) return Terrain.get(symbol)~=nil end
function Terrain.validate()
    local ids={}
    for symbol,def in pairs(Terrain.definitions) do
        assert(type(symbol)=="string" and #symbol==1 and type(def)=="table"
            and type(def.id)=="string" and not ids[def.id],
            "invalid terrain identity")
        ids[def.id]=true
        assert(type(def.walkable)=="boolean" and
            type(def.blocks_vision)=="boolean" and
            type(def.projectile_passable)=="boolean",
            "invalid terrain collision flags")
        if def.contact_damage then
            assert(def.walkable and def.contact_damage>0 and
                def.contact_damage%1==0,"invalid contact hazard")
        end
        if def.walk_extra or def.dash_extra then
            assert(def.walkable and (def.walk_extra or 0)>=0 and
                (def.dash_extra or 0)>=0,"invalid sticky terrain")
        end
        if def.dash_extend then assert(def.walkable,"ice must be walkable") end
        if def.breakable then assert(not def.walkable,"barricade must block") end
    end
    return true
end

function Terrain.move_extra(map,kind,cells)
    local extra=0
    for _,cell in ipairs(cells or {}) do
        if cell.status=="open" then
            local def=Terrain.at(map,cell.x,cell.y)
            extra=extra+(kind=="dash" and (def and def.dash_extra or 0)
                or (def and def.walk_extra or 0))
        end
    end
    return extra
end

function Terrain.can_extend_dash(map,cells)
    for index=1,math.min(2,#cells) do
        local cell=cells[index]
        if cell.status=="open" then
            local def=Terrain.at(map,cell.x,cell.y)
            if def and def.dash_extend then return true end
        end
    end
    return false
end

-- Runtime interactions are driven by canonical action/damage events. The
-- caller supplies the existing event emitter and damage resolver; terrain
-- never owns a second HP, reward, timer or random pipeline.
function Terrain.on_event(state,map,event,seen,entity,emit,damage)
    if event.floor_id~=map.floor_id then return end
    local symbol=Terrain.symbol(map,event.x or event.target_x or -1,
        event.y or event.target_y or -1)
    if event.kind=="TileEntered" and symbol=="b" then
        local id=tostring(event.action_id)..":"..tostring(event.source_id)
        if seen[id] then return end
        seen[id]=true
        local target=entity(event.source_id)
        if target and target.hp and target.hp>0 then
            emit("TerrainTriggered",{terrain_id=Terrain.get("b").id,
                floor_id=event.floor_id,x=event.x,y=event.y,target_id=target.id},
                0,event.action_id)
            damage(target,0,event.action_id,1,"bramble")
        end
    elseif event.kind=="AttackPerformed" then
        for _,cell in ipairs(event.target_cells or {}) do
            if cell.floor_id==map.floor_id and Terrain.symbol(map,cell.x,cell.y)=="B" then
                local k=cell.x..","..cell.y
                map.rows[cell.y]=map.rows[cell.y]:sub(1,cell.x-1).."."..
                    map.rows[cell.y]:sub(cell.x+1)
                map.broken_barricades=map.broken_barricades or {}
                map.broken_barricades[k]=true
                if map.studio_visuals then map.studio_visuals[k]=nil end
                emit("TerrainBroken",{terrain_id=Terrain.get("B").id,
                    floor_id=cell.floor_id,x=cell.x,y=cell.y},event.source_id,
                    event.action_id)
            end
        end
    elseif event.kind=="DamageTaken" and event.damage_type=="lightning"
        and (event.actual_loss or 0)>0 then
        local crystal
        for y=event.y-1,event.y+1 do for x=event.x-1,event.x+1 do
            if Terrain.symbol(map,x,y)=="c" then
                if not crystal or y<crystal.y or (y==crystal.y and x<crystal.x) then
                    crystal={x=x,y=y}
                end
            end
        end end
        if crystal then
            local target,best_distance
            for _,actor in ipairs(state.entities) do
                local p=actor.position
                if actor.id~=event.target_id and actor.hp and actor.hp>0
                    and actor.faction~=event.source_faction
                    and p and p.floor_id==map.floor_id
                    and math.max(math.abs(p.x-crystal.x),math.abs(p.y-crystal.y))<=1 then
                    local distance=math.abs(p.x-crystal.x)+math.abs(p.y-crystal.y)
                    if not target or distance<best_distance
                        or (distance==best_distance and actor.id<target.id) then
                        target,best_distance=actor,distance
                    end
                end
            end
            if target then
                emit("TerrainTriggered",{terrain_id=Terrain.get("c").id,
                    floor_id=map.floor_id,x=crystal.x,y=crystal.y,
                    target_id=target.id},event.source_id,event.action_id)
                damage(target,event.source_id,event.action_id,1,
                    "crystal_arc",event.source_faction)
            end
        end
    end
end

return Terrain
