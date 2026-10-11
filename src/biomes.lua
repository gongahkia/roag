-- Plain-data biome rules shared by generation, stages and presentation.
local Biomes={}
Biomes.order={"core:biome/overgrown_ruins","core:biome/fungal_hollows",
    "core:biome/sunken_aqueduct","core:biome/ashen_bastion"}
Biomes.definitions={
    [Biomes.order[1]]={id=Biomes.order[1],name="Overgrown Ruins",
        description="Mossy ruins and broken galleries",strategy="ruins",
        map_id="core:map/overgrown_ruins",floor_id="core:floor/overgrown_ruins",
        enemy_ids={"core:enemy/mossbound_guard","core:enemy/thornspitter","core:enemy/ruin_skitter"},
        room_prefix="core:room/",corridor_visual="c",floor_visual="s",wall_visual="w",
        clue_color={0.39,0.55,0.30}},
    [Biomes.order[2]]={id=Biomes.order[2],name="Fungal Hollows",
        description="Winding tunnels and luminous pockets",strategy="warrens",
        map_id="core:map/fungal_hollows",floor_id="core:floor/fungal_hollows",
        enemy_ids={"core:enemy/spore_herald","core:enemy/ruin_skitter","core:enemy/mossbound_guard"},
        room_prefix="core:room/fungal_",corridor_visual="f",floor_visual="f",wall_visual="F",
        clue_color={0.67,0.54,0.85}},
    [Biomes.order[3]]={id=Biomes.order[3],name="Sunken Aqueduct",
        description="Canal galleries, bridges and flank paths",strategy="canals",
        map_id="core:map/sunken_aqueduct",floor_id="core:floor/sunken_aqueduct",
        enemy_ids={"core:enemy/canal_lancer","core:enemy/mossbound_guard","core:enemy/thornspitter"},
        room_prefix="core:room/aqueduct_",corridor_visual="b",floor_visual="b",wall_visual="W",
        clue_color={0.48,0.72,0.74}},
    [Biomes.order[4]]={id=Biomes.order[4],name="Ashen Bastion",
        description="Courtyards, ramparts and defended wings",strategy="courtyards",
        map_id="core:map/ashen_bastion",floor_id="core:floor/ashen_bastion",
        enemy_ids={"core:enemy/cinder_bellkeeper","core:enemy/mossbound_guard","core:enemy/ruin_skitter"},
        room_prefix="core:room/bastion_",corridor_visual="t",floor_visual="t",wall_visual="T",
        hub_template_id="core:room/bastion_gallery",
        clue_color={0.76,0.52,0.36}},
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
    if stage_index==1 then ids={Biomes.order[2],Biomes.order[3]}
    else
        local visited={[Biomes.order[1]]=true}
        for _,route_id in ipairs(history or {}) do
            local biome_id=route_id:gsub("^core:route/","core:biome/")
            visited[biome_id]=true
        end
        for index=2,4 do
            if not visited[Biomes.order[index]] then ids[#ids+1]=Biomes.order[index] end
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
    local ids,maps,floors={},{},{}
    local strategies={ruins=true,warrens=true,canals=true,courtyards=true}
    for _,id in ipairs(Biomes.order) do
        local b=assert(Biomes.get(id),"missing biome")
        assert(not ids[id] and b.id==id and type(b.name)=="string"
            and type(b.description)=="string" and strategies[b.strategy]
            and not maps[b.map_id] and not floors[b.floor_id]
            and #b.enemy_ids==3 and #b.room_prefix>0
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
    end
    return true
end
return Biomes
