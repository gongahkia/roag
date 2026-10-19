-- Kenney 1-Bit Pack 1.2, CC0. Explicit semantic bindings to untouched native
-- 16px art; packed stride is 16, not the alternative spaced sheet's 17.
local Atlas=require('src.studio_atlas')
local Squads=require('src.enemy_squads')
local IdeaEnemies=require('src.idea_enemies')
local Comp=require('src.idea_companions')
local S={path='assets/kenney/1-bit-pack/symbols.png',width=784,height=352,tile=16}
S.checksum='3fc46971ed866cba'
local ink={.90,.89,.82}
local monochrome_ink={.90,.90,.90}
local lightning={1,.88,.43}
local fire={1,.58,.34}
local defense={.65,.85,.72}
local movement={.49,.84,.88}
local arcane={.76,.66,.95}
local physical={.84,.84,.91}
-- Stable ID -> native tile, readable source motif, presentation tint.
local definitions={
    storm_conductor={616,'radiating spark',lightning},
    detonation_bloom={618,'burst',fire},
    thorn_mirror={233,'spiked shield',defense},
    static_footsteps={1012,'winged boot',lightning},
    resonant_wounds={708,'clustered stars',arcane},
    temporal_echo={629,'hourglass',arcane},
    crescent_reach={563,'curved slash',physical},
    turncoat_spark={617,'small spark',lightning},
    emberbrand={567,'flame',fire},
    flashpoint={566,'fire burst',fire},
    grave_dividend={622,'skull',defense},
    opening_cut={326,'dagger',physical},
    crowdbreaker={384,'great axe',physical},
    harriers_response={623,'target corners',lightning},
    perfect_guard={184,'round shield',defense},
    final_spark={619,'fading spark ring',lightning},
    pulse_step={705,'pulse ring',movement},
    packed_powder={486,'bomb',fire},
    longblade_geometry={378,'long sword',physical},
    phantom_lunge={564,'long slash',movement},
    guardians_vow={236,'ward shield',defense},
    prospectors_heart={818,'coins',defense},
    circuit_chorus={575,'linked nodes',lightning},
    crownfire={141,'crown',fire},
    stonewake={262,'stone block',physical},
    bramble_compact={104,'thorn bush',defense},
    froststep_catalyst={707,'frost sparks',movement},
    fracture_dividend={2,'stone fragments',physical},
    mist_veil={717,'drifting flecks',movement},
    crystal_harmony={522,'faceted gem',arcane},
    sunderers_mark={383,'pickaxe',physical},
    counterweight={230,'mace',defense},
    bloodrush={1013,'rushing boot',fire},
    pain_dividend={532,'marked heart',fire},
    executioners_thread={381,'execution axe',physical},
    shared_agony={531,'paired hearts',fire},
    windup_hunter={624,'target reticle',physical},
    crossfire_omen={569,'crossed blades',fire},
    pursuit_spark={1010,'movement arrow',lightning},
    ordered_ranks={187,'formation shield',physical},
    far_witness={771,'eye',arcane},
    betrayal_ward={185,'quartered shield',defense},
    rhythm_of_steel={823,'musical notes',physical},
    cluster_rupture={830,'scattered burst',fire},
    splitflight={282,'drawn bow',movement},
    recurve={333,'recurve bow',movement},
    expanding_sigil={709,'circular sigil',arcane},
    alchemical_flip={669,'potion',arcane},
    earthshaker={380,'war hammer',physical},
    battle_march={89,'marching boots',physical},
    aftershock={706,'shock burst',physical},
    reprisal_nova={716,'radial burst',defense},
    ring_of_reversal={337,'reversal ring',movement},
}
S.boons={}
for slug,definition in pairs(definitions)do
    S.boons['core:boon/'..slug]=definition
end
for _,id in ipairs(require('src.idea_boon_content').order)do
    local art=require('src.idea_boon_content').definitions[id].art
    S.boons[id]={art.index,art.motif,defense}
end
-- Terrain symbols are an inset marker on plain ground, never random floor
-- decoration. Insets preserve integer 2x scaling at ROAG's fixed 40px tile.
S.terrain={
    ['core:terrain/bramble_bed']={104,'thorn bush',{.52,.78,.35},ground={.07,.13,.055}},
    ['core:terrain/sticky_bog']={268,'uneven ripples',{.60,.64,.32},ground={.10,.12,.055}},
    ['core:terrain/glazed_ice']={616,'ice star',{.51,.84,1},ground={.08,.16,.22}},
    ['core:terrain/barricade']={121,'cross-braced crate',{.84,.59,.35},ground={.15,.095,.05}},
    ['core:terrain/obscuring_mist']={267,'layered wisps',{.69,.79,.81},ground={.115,.15,.16}},
    ['core:terrain/resonant_crystal']={523,'crystal',{.78,.59,.97},ground={.13,.085,.19}},
}
function S.region(index)
    assert(type(index)=='number' and index%1==0 and index>=0 and index<1078,
        'invalid Kenney symbol index')
    return index%49*16,math.floor(index/49)*16,16,16
end
function S.new(read,make_texture,make_quad,options)
    local monochrome=options and options.monochrome
    local bytes=assert(read(S.path),'missing Kenney 1-Bit symbol sheet')
    assert(Atlas.checksum(bytes)==S.checksum,'corrupt Kenney 1-Bit symbol sheet')
    -- The original PNG already has transparency. No background key/repainting.
    local image=assert(make_texture(bytes,S.path,false),'unreadable Kenney symbols')
    assert(image:getWidth()==S.width and image:getHeight()==S.height,
        'Kenney symbol sheet dimensions differ')
    local quads,handles={},{}
    local function handle(definition,terrain)
        local index=definition[1]
        local x,y,w,h=S.region(index)
        if not quads[index]then quads[index]=make_quad(x,y,w,h,S.width,S.height)end
        return {texture=image,quad=quads[index],width=w,height=h,offset_x=x,offset_y=y,
            source=monochrome and 'onebit' or 'kenney',symbol_index=index,motif=definition[2],
            tint=monochrome and monochrome_ink or definition[3] or ink,
            tile_inset=terrain and 4 or nil,
            ground=definition.ground and (monochrome and {.06,.06,.06} or definition.ground)}
    end
    for id,definition in pairs(S.boons)do
        handles[id..'/icon']=handle(definition)
        handles[id..'/detail']=handles[id..'/icon']
    end
    for id,definition in pairs(S.terrain)do handles[id]=handle(definition,true)end
    for name,index in pairs(Squads.cues) do
        handles['core:ui/'..name]=handle({index,name,arcane})
    end
    for id,index in pairs(IdeaEnemies.symbols or {})do handles[id]=handle({index,id,arcane})end
    for id,index in pairs(Comp.symbols)do handles[id]=handle({index,id,arcane})end
    return {texture=image,handles=handles,signature=S.checksum..':semantic-1'}
end
return S
