-- Kenney Micro Roguelike characters/items, Caves & Dungeons terrain and
-- 1-Bit boon/mechanic symbols. Presentation only; source PNGs stay intact.
local Atlas=require('src.studio_atlas')
local Terrain=require('src.kenney_terrain')
local Symbols=require('src.kenney_symbols')
local K={path='assets/kenney/micro-roguelike/tiles.png',
    checksum='5407eab40dd3457c',width=128,height=80,tile=8}
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
}
K.actors=actors;K.objects=objects
-- Connectivity belongs to the Caves & Dungeons terrain adapter.
K.canonical=Terrain.canonical
K.wall_masks=Terrain.masks
function K.region(index)
    assert(type(index)=='number' and index%1==0 and index>=0 and index<160,
        'invalid Kenney tile index')
    return index%16*8,math.floor(index/16)*8,8,8
end
function K.new(read,make_texture,make_quad)
    local bytes=read(K.path)
    if not bytes then return nil,'absent' end
    assert(Atlas.checksum(bytes)==K.checksum,'corrupt Kenney Micro Roguelike sheet')
    local cutout=assert(make_texture(bytes,K.path,true),'unreadable Kenney sprite sheet')
    assert(cutout:getWidth()==K.width and cutout:getHeight()==K.height,
        'Kenney sheet dimensions differ')
    make_quad=make_quad or love.graphics.newQuad
    local symbols=Symbols.new(read,make_texture,make_quad)
    local terrain=Terrain.new(read,make_texture,make_quad)
    local regions,handles={},{}
    -- ^ Fixed-size texture/quad cache; refresh replaces the whole resolver.
    local function handle(index,transparent)
        local key=index..':'..tostring(transparent)
        if not regions[key]then
            local x,y,w,h=K.region(index)
            regions[key]={texture=cutout,
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
    for id,symbol in pairs(symbols.handles)do handles[id]=symbol end
    for id,tile in pairs(terrain.bindings)do handles[id]=tile end
    return setmetatable({handles=handles,region=handle,terrain_mode='tiled',
        style='kenney',signature=K.checksum..':'..symbols.signature..':'..terrain.signature,
        status='loaded',wall_regions=terrain.walls.stone,terrain_renderer=terrain,
        pages={sprites={texture=cutout},symbols={texture=symbols.texture},
            terrain={texture=terrain.native},surfaces={texture=terrain.surface}}},{__index=K}),'loaded'
end
function K:resolve(id)return self.handles[id]end
function K:terrain(map,x,y,kind,known)
    return self.terrain_renderer:resolve(map,x,y,kind,known)
end
return K
