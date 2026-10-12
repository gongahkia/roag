-- Plain-data biome rules shared by generation, stages and presentation.
local Biomes={}
Biomes.order={"core:biome/overgrown_ruins","core:biome/fungal_hollows",
    "core:biome/sunken_aqueduct","core:biome/ashen_bastion",
    "core:biome/glacial_observatory","core:biome/fractured_archive",
    "core:biome/sundered_forge"}
Biomes.definitions={
    [Biomes.order[1]]={id=Biomes.order[1],name="Overgrown Ruins",
        description="Mossy ruins and broken galleries",strategy="ruins",
        elite_id="core:enemy/thorn_warden",
        map_id="core:map/overgrown_ruins",floor_id="core:floor/overgrown_ruins",
        enemy_ids={"core:enemy/mossbound_guard","core:enemy/thornspitter","core:enemy/ruin_skitter"},
        room_prefix="core:room/",corridor_visual="c",floor_visual="s",wall_visual="w",
        clue_color={0.39,0.55,0.30}},
    [Biomes.order[2]]={id=Biomes.order[2],name="Fungal Hollows",
        description="Winding tunnels and luminous pockets",strategy="warrens",
        elite_id="core:enemy/mycelial_matriarch",
        map_id="core:map/fungal_hollows",floor_id="core:floor/fungal_hollows",
        enemy_ids={"core:enemy/spore_herald","core:enemy/ruin_skitter","core:enemy/mossbound_guard"},
        room_prefix="core:room/fungal_",corridor_visual="f",floor_visual="f",wall_visual="F",
        clue_color={0.67,0.54,0.85}},
    [Biomes.order[3]]={id=Biomes.order[3],name="Sunken Aqueduct",
        description="Canal galleries, bridges and flank paths",strategy="canals",
        elite_id="core:enemy/canal_lockmaster",
        map_id="core:map/sunken_aqueduct",floor_id="core:floor/sunken_aqueduct",
        enemy_ids={"core:enemy/canal_lancer","core:enemy/cinder_mender","core:enemy/thornspitter"},
        room_prefix="core:room/aqueduct_",corridor_visual="b",floor_visual="b",wall_visual="W",
        clue_color={0.48,0.72,0.74}},
    [Biomes.order[4]]={id=Biomes.order[4],name="Ashen Bastion",
        description="Courtyards, ramparts and defended wings",strategy="courtyards",
        elite_id="core:enemy/ash_marshal",
        map_id="core:map/ashen_bastion",floor_id="core:floor/ashen_bastion",
        enemy_ids={"core:enemy/cinder_bellkeeper","core:enemy/mossbound_guard","core:enemy/ruin_skitter"},
        room_prefix="core:room/bastion_",corridor_visual="t",floor_visual="t",wall_visual="T",
        hub_template_id="core:room/bastion_gallery",
        clue_color={0.76,0.52,0.36}},
    [Biomes.order[5]]={id=Biomes.order[5],name="Glacial Observatory",
        description="Starward hubs, icy spokes and side rings",strategy="observatory",
        elite_id="core:enemy/prism_curator",
        map_id="core:map/glacial_observatory",floor_id="core:floor/glacial_observatory",
        enemy_ids={"core:enemy/glass_charger","core:enemy/crystal_bombardier",
            "core:enemy/vault_lurker"},
        room_prefix="core:room/observatory_",corridor_visual="i",
        floor_visual="i",wall_visual="I",clue_color={0.58,0.82,0.93}},
    [Biomes.order[6]]={id=Biomes.order[6],name="Fractured Archive",
        description="Staggered shelves and alternate reading aisles",strategy="archive",
        elite_id="core:enemy/index_keeper",
        map_id="core:map/fractured_archive",floor_id="core:floor/fractured_archive",
        enemy_ids={"core:enemy/mire_binder","core:enemy/pack_chorister",
            "core:enemy/gloom_strider"},
        room_prefix="core:room/archive_",corridor_visual="l",
        floor_visual="l",wall_visual="L",clue_color={0.80,0.70,0.48}},
    [Biomes.order[7]]={id=Biomes.order[7],name="Sundered Forge",
        description="Offset furnace bays and broken flank passages",strategy="forge",
        elite_id="core:enemy/ember_crucible",
        map_id="core:map/sundered_forge",floor_id="core:floor/sundered_forge",
        enemy_ids={"core:enemy/forge_conjurer","core:enemy/mossbreaker",
            "core:enemy/bastion_bulwark"},
        room_prefix="core:room/forge_",corridor_visual="o",
        floor_visual="o",wall_visual="O",clue_color={0.95,0.60,0.36}},
}
function Biomes.get(id) return Biomes.definitions[id] end
function Biomes.for_map(map_id)
    for _,id in ipairs(Biomes.order) do
        local biome=Biomes.definitions[id]
        if biome.map_id==map_id then return biome end
    end
end
function Biomes.routes_for(stage_index,history)
    if stage_index>=3 then return {{id="core:route/gateway",
        name="Enter the Final Sanctum",description="Face the Hollow Regent"}} end
    local ids={}
    if stage_index==1 then ids={Biomes.order[2],Biomes.order[3],Biomes.order[5]}
    else
        local visited={[Biomes.order[1]]=true}
        for _,route_id in ipairs(history or {}) do
            local biome_id=route_id:gsub("^core:route/","core:biome/")
            visited[biome_id]=true
        end
        local second=history and history[1]
        local options=second=="core:route/fungal_hollows"
            and {Biomes.order[3],Biomes.order[4],Biomes.order[6]}
            or second=="core:route/sunken_aqueduct"
            and {Biomes.order[2],Biomes.order[4],Biomes.order[7]}
            or {Biomes.order[2],Biomes.order[6],Biomes.order[7]}
        for _,id in ipairs(options) do
            if not visited[id] then ids[#ids+1]=id end
        end
    end
    local routes={}
    for _,id in ipairs(ids) do
        local biome=assert(Biomes.get(id))
        routes[#routes+1]={id=id:gsub("^core:biome/","core:route/"),
            biome_id=id,name=biome.name,description=biome.description}
    end
    return routes
end
function Biomes.validate()
    local ids,maps,floors,elites={},{},{},{}
    local strategies={ruins=true,warrens=true,canals=true,courtyards=true,
        observatory=true,archive=true,forge=true}
    for _,id in ipairs(Biomes.order) do
        local b=assert(Biomes.get(id),"missing biome")
        assert(not ids[id] and b.id==id and type(b.name)=="string"
            and type(b.description)=="string" and strategies[b.strategy]
            and not maps[b.map_id] and not floors[b.floor_id]
            and #b.enemy_ids==3 and #b.room_prefix>0
            and type(b.elite_id)=="string" and not elites[b.elite_id]
            and #b.corridor_visual==1 and #b.floor_visual==1
            and #b.wall_visual==1 and #b.clue_color==3,
            "invalid biome definition: "..id)
        assert(not b.hub_template_id or type(b.hub_template_id)=="string",
            "invalid biome hub template: "..id)
        local enemy_seen={}
        for _,enemy_id in ipairs(b.enemy_ids) do
            assert(type(enemy_id)=="string" and not enemy_seen[enemy_id],
                "duplicate biome encounter: "..id)
            enemy_seen[enemy_id]=true
        end
        for _,channel in ipairs(b.clue_color) do
            assert(type(channel)=="number" and channel>=0 and channel<=1,
                "invalid biome clue color: "..id)
        end
        ids[id]=true;maps[b.map_id]=true;floors[b.floor_id]=true
        elites[b.elite_id]=true
    end
    return true
end
return Biomes
