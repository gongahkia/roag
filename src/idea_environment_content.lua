-- Curated approved artwork, separated into ground, connected masonry and props.
-- These are presentation identities, never new collision or gameplay content.
local C={version='approved-environments/1'}
C.sources={
 rpg={path='assets/kenney/approved/roguelike-rpg.png',checksum='a2cba6dbb9cac15d',width=968,height=526,stride=17,columns=57},
 caves={path='assets/kenney/caves-dungeons/terrain.png',checksum='974b7d14df86b1d7',width=492,height=305,stride=17,columns=29},
 onebit={path='assets/kenney/1-bit-pack/symbols.png',checksum='3fc46971ed866cba',width=784,height=352,stride=16,columns=49},
}
local native_wall={18,19,20,67,68,69,116,117,118}
C.material_order={'pale','mosaic','ruin','ruin_carved','observatory','garden','carpet','drowned','broken','ironworks'}
C.materials={
 pale={kind='floor',mixed={sheet='rpg',center=8},mono={sheet='onebit',center=5}},
 mosaic={kind='floor',mixed={sheet='caves',center=512},mono={sheet='onebit',center=341}},
 ruin={kind='wall',mixed={sheet='rpg',grid={704,705,706,761,762,763,818,819,820}},mono={sheet='onebit',grid=native_wall}},
 ruin_carved={kind='wall',mixed={sheet='rpg',grid={707,708,709,764,765,766,821,822,823}},mono={sheet='onebit',grid=native_wall}},
 observatory={kind='wall',mixed={sheet='rpg',grid={710,711,712,767,768,769,824,825,826}},mono={sheet='onebit',grid=native_wall}},
 garden={kind='floor',patch=true,mixed={sheet='rpg',grid={344,345,346,401,402,403,458,459,460}},mono={sheet='onebit',center=5,accent=98}},
 carpet={kind='floor',patch=true,mixed={sheet='rpg',grid={924,925,926,981,982,983,1038,1039,1040}},mono={sheet='onebit',center=852,border=true}},
 drowned={kind='water',mixed={sheet='rpg',grid={2,3,4,59,60,61,116,117,118},inner={115,114,57,58}},
  mono={sheet='rpg',grid={2,3,4,59,60,61,116,117,118},inner={115,114,57,58},water_conversion=true}},
 broken={kind='wall',mixed={sheet='caves',center=39,frame=38,accent=3},mono={sheet='caves',center=39,frame=38,accent=3}},
 ironworks={kind='wall',mixed={sheet='caves',center=39,frame=38,accent=79},mono={sheet='onebit',grid=native_wall}},
}
-- name -> mixed sheet/cell, native 1-Bit alternate; false means an expressly
-- approved monochrome derivative (broken cavern and carved columns).
C.props={
 bone_a={'caves',58,620},bone_b={'caves',59,621},grave_a={'rpg',564,24},grave_b={'rpg',678,72},
 plant_a={'rpg',529,51},plant_b={'rpg',532,98},rock_a={'caves',3,false},rock_b={'caves',5,false},
 forge_panel={'caves',79,147},workshop_supplies={'rpg',545,405},workshop_hearth={'rpg',475,400},
 camp_table={'rpg',69,396},camp_seat={'rpg',71,452},shelf_full={'rpg',726,299},shelf_empty={'rpg',783,346},
 chapel_window={'rpg',103,246},chapel_alcove={'rpg',43,248},prison_bars={'caves',262,201},
 column_a_top={'caves',146,false},column_a_middle={'caves',175,false},column_a_bottom={'caves',204,false},
 column_b_top={'caves',147,false},column_b_middle={'caves',176,false},column_b_bottom={'caves',205,false},
 mural_face={'onebit',135,135},mural_sigil={'onebit',138,138},mural_leaf={'onebit',234,234},
}
C.symbols={
 ['ability_sword_icon']=330,['ability_slash_icon']=376,['ability_bow_icon']=283,['ability_volley_icon']=334,
 ['ability_staff_icon']=183,['ability_wand_icon']=228,['ability_mace_icon']=382,['ability_axe_icon']=385,
 ['ability_shield_icon']=188,['ability_knife_icon']=131,['ability_hook_icon']=134,['ability_vial_icon']=241,
 ['ability_move_icon']=1004,
 ['status_heal_icon']=533,['status_focus_icon']=538,['status_ward_icon']=576,
 ['status_charge_0_icon']=631,['status_charge_1_icon']=632,['status_charge_2_icon']=633,['status_charge_3_icon']=634,
 ['cue_slash_effect']=565,['cue_fire_effect']=568,['cue_spark_effect']=615,['cue_echo_effect']=710,
 ['cue_block_effect']=711,['cue_boon_effect']=713,
 ['signal_armed_effect']=630,['signal_safe_effect']=628,['signal_spent_effect']=627,
}
C.families={
 crypt_sanctuary={floor='pale',wall='ruin_carved',decor={'bone_a','grave_a','bone_b','grave_b','chapel_window'}},
 observatory={floor='mosaic',wall='observatory',decor={'chapel_window','chapel_alcove'}},
 root_garden={patch='garden',wall='ruin',decor={'plant_a','plant_b'}},
 drowned_sanctum={water='drowned',wall='ruin_carved',decor={'chapel_alcove'}},
 forge_masonry={wall='ironworks',decor={'forge_panel','prison_bars'}},
 archive_library={patch='carpet',wall='ruin_carved',decor={'shelf_full','shelf_empty'}},
 broken_cavern={wall='broken',decor={'rock_a','rock_b'}},
 ruin_masonry={wall='ruin',decor={'chapel_alcove'}},
 ruin_carved={wall='ruin_carved',decor={'chapel_window'}},
}
function C.material_id(name)return 'core:terrain/env_'..name..'/'..C.materials[name].kind..'/base'end
function C.prop_id(name)return 'core:object/env_'..name end
function C.symbol_id(name)return 'core:ui/'..name end
return C
