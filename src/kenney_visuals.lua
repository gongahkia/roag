-- Kenney Micro Roguelike characters/items, Caves & Dungeons terrain and
-- 1-Bit boon/mechanic symbols. Presentation only; source PNGs stay intact.
local Atlas=require('src.studio_atlas')
local Terrain=require('src.kenney_terrain')
local Symbols=require('src.kenney_symbols')
local Squads=require('src.enemy_squads')
local K={path='assets/kenney/micro-roguelike/tiles.png',
    checksum='5407eab40dd3457c',width=128,height=80,tile=8}
local actors={
    ['core:actor/adventurer']=5,['core:actor/ranger']=11,
    ['core:actor/arcanist']=6,['core:actor/berserker']=4,
    ['core:actor/sentinel']=10,['core:actor/alchemist']=7,
    ['core:actor/shade']=8,
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
    ['core:ui/attack_center_effect']=131,
    ['core:ui/attack_impact_effect']=133,
    ['core:ui/weapon_sword_icon']=70,
    ['core:ui/weapon_bow_icon']=71,
    ['core:ui/weapon_spell_icon']=72,
    ['core:ui/weapon_dagger_icon']=73,
    ['core:ui/weapon_hook_icon']=74,
    ['core:object/review_ward']=106,
    ['core:object/review_overgrowth_1']=84,
    ['core:object/review_overgrowth_2']=85,
}
K.actors=actors;K.objects=objects
K.squad_actors={}
for role,index in pairs(Squads.tiny) do K.squad_actors['core:enemy/'..role]=index end
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
    local review_pages={}
    local function approved_sheet(name,path,checksum,width,height,stride,columns,bindings)
        local png=assert(read(path),'missing approved '..name..' sheet')
        assert(Atlas.checksum(png)==checksum,'corrupt approved '..name..' sheet')
        local texture=assert(make_texture(png,path,true),"unreadable approved "..name.." sheet")
        assert(texture:getWidth()==width and texture:getHeight()==height,
            "approved "..name.." dimensions differ")
        review_pages[name]={texture=texture}
        for id,index in pairs(bindings) do
            local x,y=index%columns*stride,math.floor(index/columns)*stride
            local sprite={texture=texture,quad=make_quad(x,y,16,16,width,height),
                width=16,height=16,offset_x=x,offset_y=y,source='kenney',
                approved_sheet=name,tile_index=index}
            handles[id]=sprite
            if id:match('^core:actor/') or id:match('^core:enemy/') then
                handles[id..'/attack']=sprite;handles[id..'/hurt']=sprite
            end
        end
    end
    local tiny_bindings={
        ['core:actor/hookrunner']=111,['core:actor/trapper']=112,
        ['core:object/review_gate_tiny']=45,
        ['core:object/review_plate_tiny_up']=54,
        ['core:object/review_plate_tiny_down']=55,
    }
    for id,index in pairs(K.squad_actors) do tiny_bindings[id]=index end
    approved_sheet('tiny','assets/kenney/approved/tiny-dungeon.png',
        'd407b7ab694fa5d5',203,186,17,12,tiny_bindings)
    approved_sheet('rpg','assets/kenney/approved/roguelike-rpg.png',
        'a2cba6dbb9cac15d',968,526,17,57,{
        ['core:object/review_key']=671,
        ['core:object/review_vault_top']=158,
        ['core:object/review_vault_bottom']=215,
        ['core:object/review_gate_top']=161,
        ['core:object/review_gate_bottom']=218,
        ['core:object/review_plate_up']=959,
        ['core:object/review_plate_down']=961,
    })
    for id,index in pairs(objects)do handles[id]=handle(index,true)end
    local function cave_cell(index)
        local x,y=index%29*17,math.floor(index/29)*17
        return {texture=terrain.native,quad=make_quad(x,y,16,16,492,305),
            width=16,height=16,offset_x=x,offset_y=y,source='kenney',
            terrain_pack='caves',tile_index=index}
    end
    for id,index in pairs({
        ['core:object/review_secret_clue']=95,
        ['core:object/review_ore']=329,
        ['core:object/review_ore_depleted']=358,
        ['core:object/review_brazier_unlit']=506,
        ['core:object/review_brazier_lit']=507,
        ['core:object/review_mushroom_1']=87,
        ['core:object/review_mushroom_2']=88,
        ['core:object/review_rail_top_left']=445,
        ['core:object/review_rail_top_right']=446,
        ['core:object/review_rail_middle_left']=474,
        ['core:object/review_rail_middle_right']=475,
        ['core:object/review_rail_bottom_left']=503,
        ['core:object/review_rail_bottom_right']=504,
        ['core:object/review_pillar_top']=145,
        ['core:object/review_pillar_middle']=177,
        ['core:object/review_pillar_bottom']=174,
    }) do handles[id]=cave_cell(index) end
    for id,symbol in pairs(symbols.handles)do handles[id]=symbol end
    handles['core:boon/ring_of_reversal/icon']=handle(89,true)
    handles['core:boon/ring_of_reversal/detail']=handles['core:boon/ring_of_reversal/icon']
    for id,tile in pairs(terrain.bindings)do handles[id]=tile end
    return setmetatable({handles=handles,region=handle,terrain_mode='tiled',
        style='kenney',signature=K.checksum..':'..symbols.signature..':'..terrain.signature..
            ':d407b7ab694fa5d5:a2cba6dbb9cac15d',
        status='loaded',wall_regions=terrain.walls.stone,terrain_renderer=terrain,
        pages={sprites={texture=cutout},symbols={texture=symbols.texture},
            terrain={texture=terrain.native},surfaces={texture=terrain.surface},
            tiny=review_pages.tiny,rpg=review_pages.rpg}},{__index=K}),'loaded'
end
function K:resolve(id)return self.handles[id]end
function K:terrain(map,x,y,kind,known)
    return self.terrain_renderer:resolve(map,x,y,kind,known)
end
return K
