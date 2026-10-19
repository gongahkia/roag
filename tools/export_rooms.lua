-- Emit actual authored room definitions for the deterministic room compiler.
local Rooms=require('src.rooms')
local Biomes=require('src.biomes')
local Finale=require('src.finale')
local Published=require('src.studio_world_runtime')

local function emit(...)
    local fields={...}
    for i,value in ipairs(fields) do
        fields[i]=tostring(value)
        assert(not fields[i]:find('[\t\r\n]'),'invalid room export field')
    end
    io.write(table.concat(fields,'\t'),'\n')
end

local function room(def,meta)
    emit('ROOM',def.id,meta.name,meta.biome_id,meta.biome_name,
        meta.strategy,meta.family,table.concat(def.roles,','),meta.origin,
        meta.source,def.mirror and '1' or '0',
        table.concat(def.rotations or {0},','),
        table.concat(meta.biomes or {meta.biome_id},','))
    for _,row in ipairs(def.rows) do emit('ROW',def.id,row) end
    for _,socket in ipairs(def.sockets or {}) do
        emit('SOCKET',def.id,socket.x,socket.y,socket.facing,socket.width)
    end
    local kinds={}
    for kind in pairs(def.markers or {}) do kinds[#kinds+1]=kind end
    table.sort(kinds)
    for _,kind in ipairs(kinds) do
        for _,point in ipairs(def.markers[kind]) do
            emit('MARKER',def.id,kind,point.x,point.y)
        end
    end
    for _,point in ipairs(def.editable or {}) do
        emit('EDITABLE',def.id,point.x,point.y)
    end
end

for _,biome_id in ipairs(Biomes.order) do
    local biome=assert(Biomes.get(biome_id))
    for _,base_id in ipairs(Rooms.order) do
        local family=assert(base_id:match('([^/]+)$'))
        local id=biome.room_prefix..family
        local def=assert(Rooms.definitions[id],'missing room '..id)
        room(def,{name=family:sub(1,1):upper()..family:sub(2),
            biome_id=biome.id,biome_name=biome.name,strategy=biome.strategy,
            family=family,origin='built-in',source='src/rooms.lua'})
    end
end

local final=Finale.map
room({id=final.id,roles={'finale'},rows=final.rows,rotations={0},
    markers={player={{x=final.start_x,y=final.start_y}},
        elite={{x=final.boss_x,y=final.boss_y}}}},
    {name='Final Sanctum',biome_id='core:biome/final_sanctum',
        biome_name='Final Sanctum',strategy='authored',family='finale',
        origin='finale',source='src/finale.lua'})

local published,status=Published.inspect(function(path)
    local file=io.open(path,'rb')
    if not file then return nil end
    local bytes=file:read('*a');file:close();return bytes
end)
assert(published or status=='absent','published Studio world '..tostring(status))
if published then
    for _,def in ipairs(published.rooms) do
        room(def,{name=def.name,biome_id='studio:published',
            biome_name='Published Studio',strategy='authored',
            family='studio',origin='published-studio',
            source='assets/studio/world-*.pack',biomes=def.biomes})
    end
end
