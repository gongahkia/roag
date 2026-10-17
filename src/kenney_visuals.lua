-- Kenney Micro Roguelike 1.4, CC0. Exact native 8px regions from its packed
-- sheet: no gutters, generated repainting, gameplay RNG or simulation state.
local Atlas=require('src.studio_atlas')
local Biomes=require('src.biomes')
local K={path='assets/kenney/micro-roguelike/tiles.png',
    checksum='5407eab40dd3457c',width=128,height=80,tile=8}
-- Native plain ground. Wall caps, door frames and stairs are not floor detail.
local ground_index=17
local actors={
    ['core:actor/adventurer']=5,['core:actor/ranger']=11,
    ['core:actor/arcanist']=6,['core:actor/berserker']=4,
    ['core:actor/sentinel']=10,['core:actor/alchemist']=7,
    ['core:enemy/mossbound_guard']=12,['core:enemy/thornspitter']=20,
    ['core:enemy/ruin_skitter']=25,['core:enemy/spore_herald']=14,
    ['core:enemy/canal_lancer']=22,['core:enemy/cinder_mender']=7,
    ['core:enemy/cinder_bellkeeper']=15,['core:enemy/glass_charger']=29,
    ['core:enemy/crystal_bombardier']=27,['core:enemy/vault_lurker']=31,
    ['core:enemy/mire_binder']=21,['core:enemy/pack_chorister']=28,
    ['core:enemy/gloom_strider']=26,['core:enemy/forge_conjurer']=13,
    ['core:enemy/mossbreaker']=23,['core:enemy/bastion_bulwark']=30,
    ['core:enemy/thorn_warden']=9,['core:enemy/mycelial_matriarch']=14,
    ['core:enemy/canal_lockmaster']=22,['core:enemy/ash_marshal']=15,
    ['core:enemy/prism_curator']=10,['core:enemy/index_keeper']=6,
    ['core:enemy/ember_crucible']=13,['core:enemy/extraction_pursuer']=24,
    ['core:enemy/hollow_regent']=9,
}
local objects={
    ['core:drop/currency']=90,['core:drop/food']=137,['core:drop/health']=135,
    ['core:chest/standard']=75,['core:chest/free_elite']=76,
    ['core:object/chest_open']=77,['core:object/door_closed']=36,
    ['core:object/door_open']=38,['core:object/stairs_down']=37,
    ['core:object/stairs_up']=118,['core:object/exit']=138,
    ['core:ui/hp_icon']=100,['core:ui/xp_icon']=129,
    ['core:ui/currency_icon']=90,['core:ui/hit_effect']=130,
    ['core:ui/boon_effect']=132,['core:ui/burning_effect']=136,
    ['core:ui/block_effect']=102,
    ['core:terrain/bramble_bed']=68,['core:terrain/sticky_bog']=69,
    ['core:terrain/glazed_ice']=126,['core:terrain/barricade']=61,
    ['core:terrain/resonant_crystal']=129,
}
K.actors=actors;K.objects=objects
local function has(mask,bit)return math.floor(mask/bit)%2==1 end
local corners={{1,2,16},{2,4,32},{4,8,64},{8,1,128}}
function K.canonical(mask)
    local value=mask%16
    for _,c in ipairs(corners)do
        if has(mask,c[1]) and has(mask,c[2]) and has(mask,c[3])then value=value+c[3]end
    end
    return value
end
K.wall_masks={};local wall_index={}
for mask=0,255 do if K.canonical(mask)==mask then
    wall_index[mask]=#K.wall_masks;K.wall_masks[#K.wall_masks+1]=mask
end end
-- Assemble native 8px wall borders on the pack's own gray wall material. Its
-- sample maps use thin outlines around black space; ROAG has solid wall cells.
-- This derived page separates the solid masses from walkable black floors.
function K.wall_sheet(source)
    local out=source:clone()
    local function sample(index,x,y)
        return source:getPixel(index%16*8+x,math.floor(index/16)*8+y)
    end
    for i,mask in ipairs(K.wall_masks)do
        local ox,oy=(i-1)%16*8,math.floor((i-1)/16)*8
        for y=0,7 do for x=0,7 do
            local r,g,b,a=sample(1,4,4)
            if not has(mask,1) and y<3 then r,g,b,a=sample(1,x,y)end
            if not has(mask,4) and y>=5 then r,g,b,a=sample(1,x,7-y)end
            if not has(mask,8) and x<3 then r,g,b,a=sample(16,x,y)end
            if not has(mask,2) and x>=5 then r,g,b,a=sample(19,x,y)end
            for _,c in ipairs({{1,8,128,x,y},{1,2,16,7-x,y},
                {4,8,64,x,7-y},{4,2,32,7-x,7-y}})do
                local dx,dy=c[4],c[5]
                if dx<3 and dy<3 then
                    if not has(mask,c[1]) and not has(mask,c[2])then
                        r,g,b,a=sample(0,dx,dy)
                    elseif has(mask,c[1]) and has(mask,c[2]) and not has(mask,c[3])then
                        r,g,b,a=sample(0,2-dx,2-dy)
                    end
                end
            end
            out:setPixel(ox+x,oy+y,r,g,b,a)
        end end
    end
    return out
end
function K.region(index)
    assert(type(index)=='number' and index%1==0 and index>=0 and index<160,
        'invalid Kenney tile index')
    return index%16*8,math.floor(index/16)*8,8,8
end
function K.new(read,make_texture,make_quad)
    local bytes=read(K.path)
    if not bytes then return nil,'absent' end
    assert(Atlas.checksum(bytes)==K.checksum,'corrupt Kenney Micro Roguelike sheet')
    local opaque=assert(make_texture(bytes,K.path,false),'unreadable Kenney sheet')
    local cutout=assert(make_texture(bytes,K.path,true),'unreadable Kenney sprite sheet')
    local walls=assert(make_texture(bytes,K.path,'walls'),'unreadable Kenney wall sheet')
    for _,image in ipairs({opaque,cutout,walls})do
        assert(image:getWidth()==K.width and image:getHeight()==K.height,
            'Kenney sheet dimensions differ')
    end
    make_quad=make_quad or love.graphics.newQuad
    local regions,handles,wall_regions={},{},{}
    -- ^ Fixed-size texture/quad cache; refresh replaces the whole resolver.
    local function handle(index,transparent)
        local key=index..':'..tostring(transparent)
        if not regions[key]then
            local x,y,w,h=K.region(index)
            regions[key]={texture=transparent and cutout or opaque,
                quad=make_quad(x,y,w,h,K.width,K.height),width=w,height=h,
                offset_x=x,offset_y=y,source='kenney',tile_index=index}
        end
        return regions[key]
    end
    for id,index in pairs(actors)do
        handles[id]=handle(index,true)
        handles[id..'/attack']=handles[id];handles[id..'/hurt']=handles[id]
    end
    for id,index in pairs(objects)do handles[id]=handle(index,true)end
    for _,biome in ipairs(Biomes.order)do
        local root='core:terrain/'..biome:match('/([^/]+)$')..'/'
        handles[root..'floor/base']=handle(ground_index,false)
        handles[root..'wall/base']=handle(66,false)
        handles[root..'water/base']=handle(110,false)
    end
    for mask,index in pairs(wall_index)do
        local x,y,w,h=K.region(index)
        wall_regions[mask]={texture=walls,quad=make_quad(x,y,w,h,K.width,K.height),
            width=w,height=h,offset_x=x,offset_y=y,source='kenney',wall_mask=mask}
    end
    return setmetatable({handles=handles,region=handle,terrain_mode='tiled',
        style='kenney',signature=K.checksum..':walls-1:plain-ground-2',status='loaded',wall_regions=wall_regions,
        pages={tiles={texture=opaque},sprites={texture=cutout},walls={texture=walls}}},{__index=K}),'loaded'
end
function K:resolve(id)return self.handles[id]end
function K:terrain(map,x,y,kind,known)
    local function same(dx,dy)
        local xx,yy=x+dx,y+dy
        local row=map.rows[yy]
        if xx<1 or not row or xx>#row then return false end
        if known and not known(xx,yy)then return true end
        local c=row:sub(xx,xx)
        if kind=='wall'then return c=='#'end
        if kind=='water'then return c=='~'end
        return c~='#' and c~='~'
    end
    local n,e,s,w=same(0,-1),same(1,0),same(0,1),same(-1,0)
    local index
    if kind=='floor'then
        index=ground_index
    elseif kind=='water'then
        index=not n and (not w and 125 or not e and 127 or 126)
            or not s and (not w and 154 or not e and 156 or 155)
            or not w and 139 or not e and 141 or 140
    else
        local mask=(n and 1 or 0)+(e and 2 or 0)+(s and 4 or 0)+(w and 8 or 0)
        for _,d in ipairs({{1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}})do
            if same(d[1],d[2])then mask=mask+d[3]end
        end
        return self.wall_regions[K.canonical(mask)]
    end
    return self.region(index,false)
end
return K
