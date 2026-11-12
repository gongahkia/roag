-- Presentation adapter: game roles -> immutable catalogue IDs -> exact crops.
-- Attack/hurt aliases are static casts, never inferred animation frames.
local Slots=require('src.visual_slots')
local Masks=require('src.kenney_terrain') -- Pure neighbour geometry; no artwork reads.
local Environment=require('src.idea_environment')
local C={label='Urizen catalog',style='catalog'}
local directions={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},
    {1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
local function inside(room,x,y)
    return x>=room.x and x<room.x+room.width and y>=room.y and y<room.y+room.height
end
local function material(plan,x,y,kind)
    for _,entry in ipairs(plan.rooms)do
        if inside(entry.room,x,y)then
            return kind=='floor' and entry.patch and inside(entry.patch,x,y) and entry.patch.material or entry[kind]
        end
    end
end
function C.new(read,make_texture,make_quad)
    make_quad=make_quad or love.graphics.newQuad
    -- Read fresh generated data for F5, keeping each resource generation isolated.
    local chunk=assert(loadstring(assert(read('src/classic_sprite_data.lua'), 'Missing catalog sprite data'), '@classic_sprite_data'))
    setfenv(chunk,{})
    local Data=chunk()
    assert(type(Data)=='table' and Data.schema_version==1 and type(Data.bindings)=='table'
        and type(Data.sources)=='table' and type(Data.requirements)=='table','Invalid catalog sprite data')
    local required,count={},0
    for _,role in ipairs(Data.requirements)do
        assert(type(role)=='string' and Slots.bindings[role] and not required[role] and Data.bindings[role],'Missing or repeated catalog role: '..tostring(role))
        required[role]=true
    end
    for role in pairs(Data.bindings)do assert(required[role],'Unknown catalog role: '..tostring(role));count=count+1 end
    assert(count==599 and #Data.requirements==count and Data.requirement_count==count,'Incomplete catalog sprite coverage')
    assert(require('src.studio_atlas').checksum(table.concat(Data.requirements,'\n'))=='32b5117cf760fa6d','Classic sprite role registry differs')
    local self=setmetatable({data=Data,handles={},textures={},bytes={},surfaces={},
        environment={plan={rooms={},props={}},failures={}},pages={},
        terrain_renderer=true,terrain_mode='tiled',style='catalog',label=C.label,
        signature=Data.catalog_sha256..':'..assert(Data.bindings_sha256,'Missing binding fingerprint')}, {__index=C})
    local paths={};for path in pairs(Data.sources)do paths[#paths+1]=path end;table.sort(paths)
    for _,path in ipairs(paths)do
        local source=Data.sources[path]
        local bytes=assert(read(path),'Missing catalog artwork: '..path)
        if love and love.data and love.data.hash then
            local digest=love.data.encode('string','hex',love.data.hash('sha256',bytes))
            assert(digest==source.sha256,'Changed catalog artwork: '..path)
        end
        local texture=assert(make_texture(bytes,path,source),'Unreadable catalog artwork: '..path)
        assert(texture:getWidth()==source.width and texture:getHeight()==source.height,'Catalog image dimensions differ: '..path)
        self.textures[path]=texture;self.bytes[path]=bytes;self.pages[path]={texture=texture}
    end
    local shared={}
    for role,entry in pairs(Data.bindings)do
        assert(entry.flip_x==nil or type(entry.flip_x)=='boolean','Invalid catalog facing: '..role)
        local source=assert(Data.sources[entry.source],'Unknown catalogue source')
        assert(entry.x>=0 and entry.y>=0 and entry.width>0 and entry.height>0
            and entry.x+entry.width<=source.width and entry.y+entry.height<=source.height,'Catalog crop outside image: '..role)
        local key=entry.id..':'..tostring(entry.flip_x==true)
        local h=shared[key]
        if not h then
            h={texture=self.textures[entry.source],quad=make_quad(entry.x,entry.y,entry.width,entry.height,source.width,source.height),
                width=entry.width,height=entry.height,offset_x=entry.x,offset_y=entry.y,
                source='catalog',catalog_id=entry.id,catalog_source=entry.source,identity_confidence=entry.identity_confidence,
                flip_x=entry.flip_x==true}
            shared[key]=h
        end
        self.handles[role]=h
    end
    self.make_texture=make_texture
    self.environment.prop=function(_,name)return self.handles[Environment.content.prop_id(name)]end
    return self,'loaded'
end
function C:resolve(id)return self.handles[id]end
function C:set_world(state)
    local map=state.generated_map or state.final_map
    local key=tostring(map)..':'..tostring(state.active_floor_id)..':'..tostring(state.next_event_id)
    if self.world_key~=key then
        self.environment.plan=map and Environment.plan(state,map)or {rooms={},props={}}
        self.world_key=key
    end
end
function C:terrain(map,x,y,kind,known)
    assert(kind=='floor' or kind=='wall' or kind=='water','Unknown terrain kind')
    local plan=self.environment.plan
    local name=material(plan,x,y,kind)
    local biome=(map.biome_id or 'core:biome/overgrown_ruins'):match('([^/]+)$')
    local role=name and Environment.content.material_id(name)or 'core:terrain/'..biome..'/'..kind..'/base'
    local entry=assert(self.data.bindings[role],'Missing catalog terrain role: '..role)
    local mask=255
    if kind~='floor'then
        mask=0;local mark=kind=='wall' and '#' or '~'
        for _,d in ipairs(directions)do
            local xx,yy=x+d[1],y+d[2];local row=map.rows[yy]
            if row and xx>=1 and xx<=#row and ((known and not known(xx,yy))or
                row:sub(xx,xx)==mark and material(plan,xx,yy,kind)==name)then mask=mask+d[3]end
        end
        mask=Masks.canonical(mask)
    end
    local key=role..':'..kind..':'..mask
    if not self.surfaces[key]then
        -- A cached opaque surface and derived border use this single source crop.
        -- Neighbouring atlas cells are never assumed to be matching edge tiles.
        local texture=assert(self.make_texture(self.bytes[entry.source],entry.source,
            {catalog_surface={x=entry.x,y=entry.y,width=entry.width,height=entry.height,kind=kind,mask=mask}}))
        self.surfaces[key]={texture=texture,width=entry.width,height=entry.height,
            source='catalog',catalog_id=entry.id,catalog_source=entry.source,
            derived=true,mask=mask,material=name,terrain_pack='catalog'}
    end
    return self.surfaces[key]
end
return C
