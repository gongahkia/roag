-- Editor-friendly authored Overgrown Ruins rooms. All definition fields are plain data.
local Content = require("src.content")
local Biomes = require("src.biomes")
local Terrain = require("src.terrain")
local Rooms = {}
Rooms.version = "roeg-rooms/2"
Rooms.biome = {id="core:biome/overgrown_ruins", floor_id="core:floor/overgrown_ruins",
    width=140,height=90,room_min=8,room_max=12,standard_chests=5,
    retry_limit=2,
    enemy_ids={"core:enemy/mossbound_guard","core:enemy/thornspitter",
        "core:enemy/ruin_skitter"}}
Rooms.order = {"core:room/entry", "core:room/watch", "core:room/gallery",
    "core:room/cache", "core:room/shrine", "core:room/arena"}

local function sockets(w, h)
    return {{x=(w+1)/2,y=1,facing="N",width=1},
        {x=w,y=(h+1)/2,facing="E",width=1},
        {x=(w+1)/2,y=h,facing="S",width=1},
        {x=1,y=(h+1)/2,facing="W",width=1}}
end

Rooms.definitions = {
    ["core:room/entry"] = {id="core:room/entry", roles={"start"},
        rows={"####.####","#.......#","#..#....#",".........",
            "#....#..#","#.......#","####.####"},
        sockets=sockets(9,7), markers={player={{x=5,y=4}},
            enemy={},chest={},landmark={},secret_anchor={{x=2,y=2}}},
        editable={{x=4,y=3},{x=6,y=5}}, rotations={0,1,2,3}, mirror=true,
        protected_default=true},
    ["core:room/watch"] = {id="core:room/watch", roles={"combat"},
        rows={"####.####","#.......#","#.#...#.#",".........",
            "#...#...#","#.......#","####.####"},
        sockets=sockets(9,7), markers={player={},enemy={{x=4,y=3}},
            chest={{x=7,y=5}},landmark={},secret_anchor={{x=2,y=6}},
            secret_wall={{x=1,y=6}}},
        editable={{x=3,y=3},{x=7,y=3},{x=5,y=5}},
        rotations={0,1,2,3}, mirror=true, protected_default=true},
    ["core:room/gallery"] = {id="core:room/gallery", roles={"combat","landmark"},
        rows={"#######.#######","#.............#","#..#......#...#",
            "#.............#","#.....#.......#","...............",
            "#.......#.....#","#.............#","#...#......#..#",
            "#.............#","#######.#######"},
        sockets=sockets(15,11), markers={player={},enemy={{x=4,y=4},{x=11,y=8}},
            chest={{x=12,y=3}},landmark={{x=8,y=6}},secret_anchor={{x=2,y=2}},
            secret_wall={{x=1,y=2}}},
        editable={{x=4,y=3},{x=11,y=3},{x=7,y=5},{x=9,y=7},{x=5,y=9}},
        rotations={0,1,2,3}, mirror=true, protected_default=true},
    ["core:room/cache"] = {id="core:room/cache", roles={"treasure","combat"},
        rows={"#######.#######","#.............#","#..###........#",
            "#..#..........#","#..#....###...#","...............",
            "#.......#.....#","#.............#","#...###.......#",
            "#.............#","#######.#######"},
        sockets=sockets(15,11), markers={player={},enemy={{x=11,y=8}},
            chest={{x=12,y=3}},landmark={},secret_anchor={{x=2,y=2}},
            secret_wall={{x=1,y=2}}},
        editable={{x=4,y=3},{x=10,y=5},{x=5,y=9}},
        rotations={0,1,2,3}, mirror=true, protected_default=true},
    ["core:room/shrine"] = {id="core:room/shrine", roles={"landmark","treasure"},
        rows={"####.####","#.......#","#.###...#",".........",
            "#...###.#","#.......#","####.####"},
        sockets=sockets(9,7), markers={player={},enemy={{x=3,y=5}},
            chest={{x=7,y=2}},landmark={{x=5,y=4}},secret_anchor={{x=2,y=2}},
            secret_wall={{x=1,y=2}}},
        editable={{x=3,y=3},{x=7,y=5}},
        rotations={0,1,2,3}, mirror=true, protected_default=true},
    ["core:room/arena"] = {id="core:room/arena", roles={"elite"},
        rows={"###########.###########",
            "#.....................#","#.....................#",
            "#....###.......###....#","#.....................#",
            "#.....................#","#.....................#",
            "#..##.............##..#",".......................",
            "#..##.............##..#","#.....................#",
            "#.....................#","#.....................#",
            "#....###.......###....#","#.....................#",
            "#.....................#","###########.###########"},
        sockets=sockets(23,17), markers={player={},enemy={},chest={},
            landmark={{x=12,y=9}},elite={{x=12,y=9}},secret_anchor={{x=3,y=3}}},
        editable={{x=6,y=4},{x=18,y=4},{x=6,y=14},{x=18,y=14}},
        rotations={0,1,2,3}, mirror=true, protected_default=true},
}

-- Each variant is materialized as the same plain-data template schema. The
-- construction recipe only authors protected geometry; runtime never mutates
-- a shared definition. An editor can serialize the resulting rows directly.
local function variant(base,biome)
    local function clone(value)
        if type(value)~="table" then return value end
        local out={}
        for k,v in pairs(value) do out[k]=clone(v) end
        return out
    end
    local def=clone(base)
    def.id=biome.room_prefix..base.id:match("[^/]+$")
    local w,h=#def.rows[1],#def.rows
    local reserved={}
    for _,socket in ipairs(def.sockets) do
        reserved[socket.y..":"..socket.x]=true
        local dx=socket.facing=="W" and 1 or socket.facing=="E" and -1 or 0
        local dy=socket.facing=="N" and 1 or socket.facing=="S" and -1 or 0
        reserved[(socket.y+dy)..":"..(socket.x+dx)]=true
    end
    for _,points in pairs(def.markers) do
        for _,p in ipairs(points) do
            reserved[p.y..":"..p.x]=true
            for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
                reserved[(p.y+d[2])..":"..(p.x+d[1])]=true
            end
        end
    end
    reserved[math.floor((h+1)/2)..":"..math.floor((w+1)/2)]=true
    local grid={}
    local new_walls={}
    for y,row in ipairs(def.rows) do
        grid[y]={}
        for x=1,w do grid[y][x]=row:sub(x,x) end
    end
    local function wall(x,y)
        if x>1 and x<w and y>1 and y<h and not reserved[y..":"..x] then
            grid[y][x]="#"
            new_walls[y..":"..x]=true
        end
    end
    if biome.strategy=="warrens" then
        -- Broken roundish corners and offset mushroom clusters leave wide
        -- central pockets and four protected socket approaches.
        for _,p in ipairs({{3,2},{w-2,2},{2,3},{w-1,3},
            {2,h-2},{w-1,h-2},{3,h-1},{w-2,h-1}}) do wall(p[1],p[2]) end
        if w>=15 then
            for _,p in ipairs({{5,4},{6,4},{w-5,h-3},{w-6,h-3}}) do wall(p[1],p[2]) end
        end
    elseif biome.strategy=="canals" then
        -- Paired channel banks with multiple bridge gaps; the long gallery
        -- axis stays open for the Lancer and can be flanked from either side.
        if w>=15 then
            for y=3,h-2 do
                if y~=math.floor((h+1)/2) and y~=4 and y~=h-3 then
                    wall(4,y);wall(w-3,y)
                end
            end
        else wall(3,3);wall(w-2,h-2) end
        -- Authored channel cells are impassable shallow water but transparent
        -- to sight and projectiles. Boundary masonry stays solid stone.
        for y=2,h-1 do for x=2,w-1 do
            if grid[y][x]=="#" then grid[y][x]="~" end
        end end
    elseif biome.strategy=="courtyards" then
        -- Corner ramparts and staggered wing partitions frame an open center.
        for _,p in ipairs({{3,3},{w-2,3},{3,h-2},{w-2,h-2}}) do wall(p[1],p[2]) end
        if w>=15 then
            for x=5,w-4 do
                if x~=math.floor((w+1)/2) and x%3~=0 then
                    wall(x,3);wall(x,h-2)
                end
            end
        end
    elseif biome.strategy=="observatory" then
        -- Offset star points frame an open axial crossing. Ice is a deliberate
        -- route choice, not a compulsory automatic walking slide.
        if w>=15 then
            for _,p in ipairs({{4,4},{w-3,4},{4,h-3},{w-3,h-3},
                {6,3},{w-5,h-2}}) do wall(p[1],p[2]) end
        else wall(3,3);wall(w-2,h-2) end
    elseif biome.strategy=="archive" then
        -- Interrupted shelf aisles create alternate narrow sight lines.
        if w>=15 then
            for y=3,h-2 do
                if y%3~=1 then wall(4,y);wall(w-3,h+1-y) end
            end
        else wall(3,3);wall(w-2,h-2) end
    elseif biome.strategy=="forge" then
        -- Staggered furnace bays and open center leave flanking approaches.
        if w>=15 then
            for x=4,w-3 do
                if x%4~=0 then
                    wall(x,3)
                    if x%4~=2 then wall(w+1-x,h-2) end
                end
            end
        else wall(3,h-2);wall(w-2,3) end
    end
    local source_editable={}
    for _,p in ipairs(base.editable or {}) do
        source_editable[p.y..":"..p.x]=true
    end
    local function feature(x,y,symbol)
        if x>1 and x<w and y>1 and y<h and grid[y][x]=="."
            and not reserved[y..":"..x] and not source_editable[y..":"..x] then
            grid[y][x]=symbol
        end
    end
    if biome.strategy=="observatory" then
        for _,p in ipairs({{3,4},{w-2,h-3},{w-3,3},{4,h-2}}) do
            feature(p[1],p[2],"i")
        end
    elseif biome.strategy=="archive" then
        for _,p in ipairs({{5,3},{w-4,h-2},{5,h-3}}) do
            feature(p[1],p[2],"m")
        end
        if w<15 then feature(3,(h+1)/2,"m") end
    elseif biome.strategy=="forge" then
        for _,p in ipairs({{4,4},{w-3,h-3},{5,h-2}}) do
            feature(p[1],p[2],"c")
        end
        if w<15 then feature(3,(h+1)/2,"c") end
    end
    for y=1,h do def.rows[y]=table.concat(grid[y]) end
    -- Retain authored variation points outside new partitions. In canals,
    -- editable water toggles to walkable bridge cells during generation.
    def.editable={}
    for _,p in ipairs(base.editable or {}) do
        if not new_walls[p.y..":"..p.x] then
            def.editable[#def.editable+1]={x=p.x,y=p.y}
        end
    end
    return def
end
for index=2,#Biomes.order do
    local biome=Biomes.get(Biomes.order[index])
    for _,base_id in ipairs(Rooms.order) do
        local definition=variant(Rooms.definitions[base_id],biome)
        Rooms.definitions[definition.id]=definition
    end
end

local facings = {N=true,E=true,S=true,W=true}
local marker_kinds = {player=true,enemy=true,chest=true,landmark=true,
    elite=true,secret_anchor=true,secret_wall=true,stair=true}
local roles = {start=true,combat=true,treasure=true,landmark=true,elite=true,secret=true}
local function point_key(x,y) return x .. "," .. y end
local function has_role(def, role)
    for _, value in ipairs(def.roles) do if value == role then return true end end
    return false
end
Rooms.has_role = has_role

function Rooms.validate(definitions, order, biome)
    definitions, order = definitions or Rooms.definitions, order or Rooms.order
    biome=biome or Rooms.biome
    assert(type(biome.id)=="string" and type(biome.floor_id)=="string"
        and type(biome.width)=="number" and biome.width%1==0 and biome.width>=80
        and type(biome.height)=="number" and biome.height%1==0 and biome.height>=60
        and type(biome.room_min)=="number" and biome.room_min%1==0
        and type(biome.room_max)=="number" and biome.room_max%1==0
        and biome.room_min>=8 and biome.room_max<=12
        and biome.room_min<=biome.room_max
        and type(biome.standard_chests)=="number"
        and biome.standard_chests%1==0 and biome.standard_chests>0
        and biome.standard_chests<=biome.room_min-2
        and type(biome.retry_limit)=="number" and biome.retry_limit%1==0
        and biome.retry_limit>=0 and biome.retry_limit<=8,
        "invalid Overgrown Ruins biome rules")
    local enemy_seen={}
    assert(type(biome.enemy_ids)=="table" and #biome.enemy_ids==3,
        "biome needs three enemy archetypes")
    for _,enemy_id in ipairs(biome.enemy_ids) do
        assert(Content.get_enemy(enemy_id) and not enemy_seen[enemy_id],
            "invalid biome enemy reference: "..tostring(enemy_id))
        enemy_seen[enemy_id]=true
    end
    local seen = {}
    for _, id in ipairs(order) do
        assert(not seen[id] and definitions[id] and definitions[id].id == id,
            "duplicate or missing room ID: " .. tostring(id))
        seen[id] = true
        local def = definitions[id]
        local w, h = #def.rows[1], #def.rows
        assert(w >= 7 and h >= 7 and w % 2 == 1 and h % 2 == 1,
            "invalid room dimensions: " .. id)
        for y, row in ipairs(def.rows) do
            assert(#row == w,"invalid room tile in " .. id .. " row " .. y)
            for x=1,#row do
                local symbol=row:sub(x,x)
                assert(symbol==" " or Terrain.is_valid(symbol),
                    "invalid room tile in " .. id .. " row " .. y)
            end
        end
        assert(def.protected_default == true, "room needs a protected mask: " .. id)
        assert(type(def.roles) == "table" and #def.roles > 0, "room needs role: " .. id)
        local role_seen = {}
        for _, role in ipairs(def.roles) do
            assert(roles[role] and not role_seen[role], "invalid room role: " .. id)
            role_seen[role] = true
        end
        assert(type(def.rotations) == "table" and #def.rotations > 0,
            "room needs rotations: " .. id)
        local rotation_seen = {}
        for _, turn in ipairs(def.rotations) do
            assert(type(turn) == "number" and turn % 1 == 0 and turn >= 0 and turn <= 3
                and not rotation_seen[turn], "invalid room rotation: " .. id)
            rotation_seen[turn] = true
        end
        assert(type(def.mirror) == "boolean", "invalid mirror permission: " .. id)
        local reserved = {}
        assert(type(def.sockets) == "table" and #def.sockets == 4,
            "room needs four sockets: " .. id)
        local facing_seen = {}
        for _, socket in ipairs(def.sockets) do
            local x,y,facing = socket.x,socket.y,socket.facing
            assert(facings[facing] and not facing_seen[facing]
                and socket.width == 1 and type(x)=="number" and type(y)=="number"
                and x%1==0 and y%1==0 and x>=1 and x<=w and y>=1 and y<=h,
                "invalid room socket: " .. id)
            facing_seen[facing] = true
            assert((facing=="N" and y==1 or facing=="S" and y==h
                or facing=="W" and x==1 or facing=="E" and x==w)
                and def.rows[y]:sub(x,x)==".", "socket must open at boundary: " .. id)
            local ix = x + (facing=="W" and 1 or facing=="E" and -1 or 0)
            local iy = y + (facing=="N" and 1 or facing=="S" and -1 or 0)
            assert(def.rows[iy]:sub(ix,ix)==".", "socket lacks inner clearance: " .. id)
            reserved[point_key(x,y)] = true
        end
        assert(type(def.markers)=="table", "room needs markers: " .. id)
        assert(not def.markers.stair or #def.markers.stair<=1,
            "room may have only one stair marker: " .. id)
        local blocking_markers={}
        for kind, points in pairs(def.markers) do
            assert(marker_kinds[kind] and type(points)=="table", "invalid marker kind: " .. id)
            for _, p in ipairs(points) do
                assert(type(p.x)=="number" and type(p.y)=="number" and p.x%1==0
                    and p.y%1==0 and p.x>=1 and p.x<=w and p.y>=1 and p.y<=h
                    and def.rows[p.y]:sub(p.x,p.x)==
                        (kind=="secret_wall" and "#" or "."),
                    "invalid " .. kind .. " marker: " .. id)
                reserved[point_key(p.x,p.y)] = true
                if kind=="player" or kind=="enemy" or kind=="chest" or kind=="stair" then
                    assert(not blocking_markers[point_key(p.x,p.y)],
                        "overlapping blocking markers: " .. id)
                    blocking_markers[point_key(p.x,p.y)]=true
                end
            end
        end
        if #(def.markers.secret_wall or {})>0 then
            assert(#def.markers.secret_wall==1
                and #(def.markers.secret_anchor or {})==1,
                "secret wall needs one approach anchor: "..id)
            local wall=def.markers.secret_wall[1]
            local anchor=def.markers.secret_anchor[1]
            assert((wall.x==1 or wall.x==w or wall.y==1 or wall.y==h)
                and math.abs(wall.x-anchor.x)+math.abs(wall.y-anchor.y)==1,
                "secret wall needs reachable adjacent approach: "..id)
        end
        if has_role(def,"start") then
            assert(def.markers.player and #def.markers.player==1,
                "start room needs one player marker: " .. id)
        end
        if has_role(def,"elite") then
            assert(def.markers.elite and #def.markers.elite==1,
                "elite room needs one arena marker: " .. id)
        end
        assert((has_role(def,"start") or #(def.markers.player or {})==0)
            and (has_role(def,"elite") or #(def.markers.elite or {})==0),
            "marker incompatible with room role: " .. id)
        if has_role(def,"combat") then
            assert(def.markers.enemy and #def.markers.enemy>0,
                "combat room needs enemy marker: " .. id)
        end
        if has_role(def,"treasure") then
            assert(def.markers.chest and #def.markers.chest>0,
                "treasure room needs chest marker: " .. id)
        end
        if has_role(def,"landmark") then
            assert(def.markers.landmark and #def.markers.landmark>0,
                "landmark room needs landmark marker: " .. id)
        end
        local editable_seen={}
        for _, p in ipairs(def.editable or {}) do
            assert(type(p.x)=="number" and type(p.y)=="number" and p.x%1==0
                and p.y%1==0 and p.x>1 and p.x<w and p.y>1 and p.y<h
                and ({["."]=true,["#"]=true,["~"]=true})[
                    def.rows[p.y]:sub(p.x,p.x)]
                and not reserved[point_key(p.x,p.y)] and
                not editable_seen[point_key(p.x,p.y)],
                "editable mask violates protected geometry: " .. id)
            editable_seen[point_key(p.x,p.y)]=true
        end
    end
    return true
end

local clockwise = {N="E",E="S",S="W",W="N"}
function Rooms.transform(def, turns, mirror)
    assert(def and type(turns)=="number" and turns%1==0 and turns>=0 and turns<=3,
        "invalid room transformation")
    local permitted=false
    for _, turn in ipairs(def.rotations) do if turn==turns then permitted=true end end
    assert(permitted and (not mirror or def.mirror), "room transformation not permitted")
    local w,h=#def.rows[1],#def.rows
    local function transform_point(x,y,facing)
        if mirror then
            x=w+1-x
            if facing=="E" then facing="W" elseif facing=="W" then facing="E" end
        end
        local tw,th=w,h
        for _=1,turns do
            x,y=th+1-y,x
            tw,th=th,tw
            if facing then facing=clockwise[facing] end
        end
        return x,y,facing
    end
    local out_w,out_h=w,h
    if turns%2==1 then out_w,out_h=h,w end
    local grid={}
    for y=1,out_h do grid[y]={} end
    for y=1,h do for x=1,w do
        local nx,ny=transform_point(x,y)
        grid[ny][nx]=def.rows[y]:sub(x,x)
    end end
    local rows={}
    for y=1,out_h do rows[y]=table.concat(grid[y]) end
    local sockets_out={}
    for _, s in ipairs(def.sockets) do
        local x,y,facing=transform_point(s.x,s.y,s.facing)
        sockets_out[#sockets_out+1]={x=x,y=y,facing=facing,width=s.width}
    end
    local markers={}
    for kind, points in pairs(def.markers) do
        markers[kind]={}
        for _, p in ipairs(points) do
            local x,y=transform_point(p.x,p.y)
            markers[kind][#markers[kind]+1]={x=x,y=y}
        end
    end
    local editable={}
    for _, p in ipairs(def.editable or {}) do
        local x,y=transform_point(p.x,p.y)
        editable[#editable+1]={x=x,y=y}
    end
    local studio_protected={}
    for _,p in ipairs(def.studio_protected or {}) do
        local x,y=transform_point(p.x,p.y)
        studio_protected[#studio_protected+1]={x=x,y=y}
    end
    local studio_visual_cells={}
    for location,asset_id in pairs(def.studio_visual_cells or {}) do
        local x,y=location:match('^(%d+),(%d+)$')
        x,y=transform_point(tonumber(x),tonumber(y))
        studio_visual_cells[x..','..y]=asset_id
    end
    return {id=def.id,roles=def.roles,width=out_w,height=out_h,rows=rows,
        sockets=sockets_out,markers=markers,editable=editable,
        turns=turns,mirror=mirror,protected_default=true,
        studio_visual_cells=studio_visual_cells,studio_protected=studio_protected}
end

function Rooms.for_role(role,biome_id)
    local result={}
    local biome=Biomes.get(biome_id or Rooms.biome.id)
    assert(biome,"unknown room biome")
    for _, base_id in ipairs(Rooms.order) do
        local id=biome.room_prefix..base_id:match("[^/]+$")
        local def=Rooms.definitions[id]
        if has_role(def,role) then result[#result+1]=def end
    end
    return result
end

return Rooms
