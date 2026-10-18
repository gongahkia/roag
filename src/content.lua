local Content = {}
local Biomes = require("src.biomes")
local Finale = require("src.finale")
local Terrain = require("src.terrain")
local EliteAI = require("src.elite_ai")
local RegularAI = require("src.regular_ai")
local Classes = require("src.classes")
local Squads = require('src.enemy_squads')

Content.version = "roag-content/8"

local tiles = Terrain.definitions

local adventurer = Classes.get("core:actor/adventurer")

local progression = {
    first_threshold = 5, threshold_step = 5,
    stat_order = {"max_health", "damage", "defense", "speed",
        "crit_chance", "crit_damage"},
    stat_weights = {max_health=1, damage=1, defense=1, speed=1,
        crit_chance=1, crit_damage=1},
    growth = {max_health=4, damage=1, defense=1, speed=10,
        crit_chance=50000, crit_damage=25},
}

local chests = {
    ["core:chest/standard"] = {id="core:chest/standard", name="Boon Chest",
        price=5, rarity_weights={common=70, uncommon=25, rare=4, legendary=1}},
    ["core:chest/free_elite"] = {id="core:chest/free_elite", name="Elite Boon Chest",
        price=0, rarity_weights={common=20, uncommon=45, rare=28, legendary=7}},
}

-- Provisional stage pacing, in authoritative simulation units.
local stage_rules = {
    elite_activation_radius=7, elite_phase_percent=50,
    grace=1500, pressure_first=1200, pressure_periods={1200,900,650},
    pressure_max_active=18, pressure_safe_radius=8,
    pursuer_spawn_distance=14,
    routes={Biomes.routes_for(1,{})[1],Biomes.routes_for(1,{})[2],
        Biomes.routes_for(2,{"core:route/fungal_hollows"})[2],
        Biomes.routes_for(3,{})[1]},
}
local ability_proc_coefficients = {
    ["core:ability/basic_attack"] = 1,
    ["core:ability/sweeping_slash"] = 1,
    ["core:ability/guard_strike"] = 1,
    ["core:ability/thorn_lane"] = 1,
    ["core:ability/skitter_stab"] = 1,
    ["core:ability/spore_burst"] = 1,
    ["core:ability/canal_thrust"] = 1,
    ["core:ability/bell_cross"] = 1,
    ["core:ability/warden_sweep"] = 1,
    ["core:ability/warden_roots"] = 1,
    ["core:ability/regent_line"] = 1,
    ["core:ability/regent_crown"] = 1,
    ["core:ability/regent_sigil"] = 1,
    ["core:ability/pursuer_strike"] = 0,
}
local ability_names={
    ["core:ability/regent_line"]="Regent's Line",
    ["core:ability/regent_crown"]="Broken Crown",
    ["core:ability/regent_sigil"]="Hollow Sigil",
}
for _,name in ipairs({'ward_channel','ward_tap','brute_sweep','acolyte_mend',
    'acolyte_tap','rushwing_bite','scribe_snare','scribe_tap','siege_square',
    'shield_bash','pin_shot','pin_tap','captain_cut','follower_strike'}) do
    local id='core:ability/'..name
    ability_proc_coefficients[id]=1
    ability_names[id]=name:gsub('_',' ')
end
for _,id in ipairs(Classes.order) do
    local class=Classes.get(id)
    ability_proc_coefficients[class.basic_id]=1
    ability_proc_coefficients[class.special_id]=1
    ability_names[class.basic_id]=class.basic_name
    ability_names[class.special_id]=class.special_name
end
for _,profile in pairs(EliteAI.profiles) do
    for _,shape in ipairs(profile) do
        local id="core:ability/elite_"..shape
        ability_proc_coefficients[id]=1
        ability_names[id]=shape:gsub("_"," ")
    end
end
for _,name in ipairs({"mossbreaker_crack","mossbreaker_cleave","gloom_fork",
    "mire_ring","pack_chorus","vault_ambush","crystal_barrage",
    "cinder_mend","cinder_tap","glass_charge","glass_jab",
    "forge_call","forge_tap","bulwark_retort","bulwark_bash"}) do
    local id="core:ability/"..name
    ability_proc_coefficients[id]=1
    ability_names[id]=name:gsub("_"," ")
end

local enemies = {
    ["core:enemy/hollow_regent"] = {
        id="core:enemy/hollow_regent",name="Hollow Regent",ai="hollow_regent",
        hp=62,damage=4,defense=1,speed=75,windup=110,recovery=145,
        move_cost=100,range=6,xp_reward=0,currency_drop=0,
    },
    ["core:enemy/thorn_warden"] = {
        id="core:enemy/thorn_warden",name="Thorn Warden",ai="warden",
        hp=44,damage=5,defense=1,speed=85,windup=85,recovery=120,
        move_cost=100,range=4,xp_reward=12,currency_drop=8,
    },
    ["core:enemy/extraction_pursuer"] = {
        id="core:enemy/extraction_pursuer",name="Extraction Pursuer",ai="pursuer",
        hp=1,damage=9,defense=0,speed=190,windup=45,recovery=55,
        move_cost=70,range=1,xp_reward=0,currency_drop=0,
        invulnerable=true,
    },
    ["core:enemy/mossbound_guard"] = {
        id = "core:enemy/mossbound_guard", name = "Mossbound Guard", ai = "guard",
        hp = 10, damage = 3, defense = 1, speed = 70,
        windup = 80, recovery = 120, move_cost = 100, range = 1,
        xp_reward = 5, currency_drop = 4,
    },
    ["core:enemy/thornspitter"] = {
        id = "core:enemy/thornspitter", name = "Thornspitter", ai = "spitter",
        hp = 6, damage = 2, defense = 0, speed = 90,
        windup = 90, recovery = 130, move_cost = 100, min_range = 2, range = 7,
        xp_reward = 4, currency_drop = 3,
    },
    ["core:enemy/ruin_skitter"] = {
        id = "core:enemy/ruin_skitter", name = "Ruin Skitter", ai = "skitter",
        hp = 4, damage = 1, defense = 0, speed = 160,
        windup = 35, recovery = 70, move_cost = 100, range = 1,
        xp_reward = 3, currency_drop = 2,
    },
    ["core:enemy/spore_herald"] = {
        id="core:enemy/spore_herald",name="Spore Herald",ai="spore_herald",
        hp=7,damage=2,defense=0,speed=85,windup=100,recovery=135,
        move_cost=100,range=5,xp_reward=5,currency_drop=3,
    },
    ["core:enemy/canal_lancer"] = {
        id="core:enemy/canal_lancer",name="Canal Lancer",ai="canal_lancer",
        hp=9,damage=3,defense=1,speed=90,windup=75,recovery=120,
        move_cost=100,range=2,xp_reward=5,currency_drop=4,
    },
    ["core:enemy/cinder_bellkeeper"] = {
        id="core:enemy/cinder_bellkeeper",name="Cinder Bellkeeper",ai="cinder_bellkeeper",
        hp=12,damage=3,defense=1,speed=65,windup=115,recovery=145,
        move_cost=100,range=4,xp_reward=6,currency_drop=4,
    },
}

local biome_elites={
    {"mycelial_matriarch","Mycelial Matriarch",48,4,75,105,135,5},
    {"canal_lockmaster","Canal Lockmaster",50,4,80,95,130,5},
    {"ash_marshal","Ash Marshal",52,5,70,110,140,5},
    {"prism_curator","Prism Curator",46,4,85,100,130,6},
    {"index_keeper","Index Keeper",48,4,80,105,135,6},
    {"ember_crucible","Ember Crucible",54,5,65,115,145,5},
}
for _,row in ipairs(biome_elites) do
    local id="core:enemy/"..row[1]
    enemies[id]={id=id,name=row[2],ai="biome_elite",hp=row[3],damage=row[4],
        defense=1,speed=row[5],windup=row[6],recovery=row[7],
        move_cost=100,range=row[8],xp_reward=12,currency_drop=8}
end

local expansion_regulars={
    {"mossbreaker","Mossbreaker",13,3,1,65,100,130,1},
    {"gloom_strider","Gloom Strider",6,2,0,120,85,115,5},
    {"mire_binder","Mire Binder",8,2,0,75,120,145,4},
    {"pack_chorister","Pack Chorister",9,2,0,85,85,115,1},
    {"vault_lurker","Vault Lurker",7,3,0,100,120,135,4},
    {"crystal_bombardier","Crystal Bombardier",7,2,0,70,125,150,5},
    {"cinder_mender","Cinder Mender",8,1,0,75,105,125,3},
    {"glass_charger","Glass Charger",9,3,0,110,100,120,4},
    {"forge_conjurer","Forge Conjurer",9,1,0,70,120,145,5},
    {"bastion_bulwark","Bastion Bulwark",13,2,2,55,105,135,2},
}
for _,row in ipairs(expansion_regulars) do
    local id="core:enemy/"..row[1]
    enemies[id]={id=id,name=row[2],ai="expansion_regular",hp=row[3],
        damage=row[4],defense=row[5],speed=row[6],windup=row[7],
        recovery=row[8],move_cost=100,range=row[9],
        xp_reward=4,currency_drop=3}
end

for _,row in ipairs(Squads.enemies) do
    local id='core:enemy/'..row[1]
    enemies[id]={id=id,name=row[2],ai='squad',hp=row[3],damage=row[4],
        defense=row[5],speed=row[6],windup=row[7],recovery=row[8],
        move_cost=100,range=row[9],xp_reward=2,currency_drop=1}
end

local demo_spawns = {
    { definition_id = "core:enemy/mossbound_guard", x = 7, y = 3 },
    { definition_id = "core:enemy/thornspitter", x = 3, y = 9 },
    { definition_id = "core:enemy/ruin_skitter", x = 6, y = 6 },
}
local demo_chests = {
    {definition_id="core:chest/standard", x=4, y=4},
}

local maps = {
    ["core:map/test_room"] = {
        id = "core:map/test_room",
        floor_id = "core:floor/test",
        width = 15,
        height = 11,
        start_x = 2,
        start_y = 2,
        rows = {
            "###############",
            "#.............#",
            "#..###........#",
            "#.............#",
            "#.......###...#",
            "#.............#",
            "#....#........#",
            "#....#........#",
            "#.............#",
            "#.............#",
            "###############",
        },
    },
    ["core:map/scrolling_room"] = {
        id = "core:map/scrolling_room",
        floor_id = "core:floor/test",
        width = 35,
        height = 25,
        start_x = 3,
        start_y = 3,
        rows = {
            "###################################",
            "#.................................#",
            "#.................................#",
            "#....####.........................#",
            "#.................................#",
            "#.....................#####.......#",
            "#.................................#",
            "#.................................#",
            "#.........######..................#",
            "#.................................#",
            "#.................................#",
            "#..................................",
            "#..................####...........#",
            "#.................................#",
            "#.................................#",
            "#......#####......................#",
            "#.................................#",
            "#.................................#",
            "#.........................#####...#",
            "#.................................#",
            "#.................................#",
            "#.............####................#",
            "#.................................#",
            "#.................................#",
            "###################################",
        },
    },
}

function Content.get_map(id)
    return maps[id] or (id==Finale.map.id and Finale.map or nil)
end

-- Generated runs carry their concrete map; authored fixtures remain registry lookups.
function Content.map_for(state)
    return state.generated_map or state.final_map or Content.get_map(state.map_id)
end

function Content.get_enemy(id)
    return enemies[id]
end

function Content.get_adventurer()
    return adventurer
end
function Content.get_class(id) return Classes.get(id) end

function Content.get_progression() return progression end
function Content.get_stage_rules() return stage_rules end
function Content.get_chest(id) return chests[id] end
function Content.demo_chests(map_id)
    if map_id == "core:map/scrolling_room" then return demo_chests end
    return {}
end

function Content.ability_proc_coefficient(id)
    return ability_proc_coefficients[id] or 1
end

function Content.has_ability(id)
    return ability_proc_coefficients[id] ~= nil
end
function Content.ability_name(id)
    return ability_names[id] or id:gsub('^core:ability/',''):gsub('_',' ')
end

function Content.demo_spawns(map_id)
    if map_id == "core:map/scrolling_room" then return demo_spawns end
    return {}
end

local function tile_at(map, x, y)
    if x < 1 or x > map.width or y < 1 or y > map.height then return nil end
    return tiles[map.rows[y]:sub(x, x)]
end

function Content.validate_map(map)
    assert(type(map) == "table" and type(map.id) == "string" and map.id ~= "", "map needs a stable ID")
    assert(type(map.floor_id) == "string" and map.floor_id ~= "", "map needs a floor ID")
    assert(type(map.width) == "number" and map.width > 0 and map.width % 1 == 0, "invalid map width")
    assert(type(map.height) == "number" and map.height > 0 and map.height % 1 == 0, "invalid map height")
    assert(type(map.rows) == "table" and #map.rows == map.height, "invalid map row count")
    for y = 1, map.height do
        local row = map.rows[y]
        assert(type(row) == "string" and #row == map.width, "invalid map row width")
        for x=1,#row do
            assert(Terrain.is_valid(row:sub(x,x)),"unknown map tile")
        end
    end
    assert(type(map.start_x) == "number" and type(map.start_y) == "number", "missing start")
    assert(map.start_x % 1 == 0 and map.start_y % 1 == 0, "start must be on grid")
    assert(Content.walkable(map, map.start_x, map.start_y), "start must be walkable")
    return true
end

function Content.walkable(map, x, y)
    local tile = tile_at(map, x, y)
    return tile ~= nil and tile.walkable
end

local function blocking_entity(state, map, x, y, field)
    if not state then return false end
    for _,entity in ipairs(state.entities) do
        local p=entity.position
        if p and p.floor_id==map.floor_id and p.x==x and p.y==y
            and entity[field] then return true end
    end
    return false
end

function Content.projectile_passable(map, x, y, state)
    local tile = tile_at(map, x, y)
    return tile ~= nil and tile.projectile_passable
        and not blocking_entity(state,map,x,y,"blocks_projectiles")
end

function Content.mist_blocks(map,x,y,state)
    if not map or not map.rows[y] or map.rows[y]:sub(x,x)~='m' then
        return false
    end
    for _,feature in ipairs(state and state.review_features or {}) do
        if feature.kind=='brazier' and feature.floor_id==map.floor_id
            and (feature.lit_until or 0)>(state.clock or 0)
            and math.abs(feature.x-x)+math.abs(feature.y-y)<=2 then
            return false
        end
    end
    return true
end

function Content.blocks_vision(map, x, y, state)
    local tile = tile_at(map, x, y)
    return tile == nil or (tile.blocks_vision
        and (not map.rows[y] or map.rows[y]:sub(x,x)~='m'
            or Content.mist_blocks(map,x,y,state)))
        or blocking_entity(state,map,x,y,"blocks_vision")
end

function Content.validate_all()
    Terrain.validate()
    EliteAI.validate()
    RegularAI.validate()
    Classes.validate()
    Biomes.validate()
    local function positive_integer(value)
        return type(value) == "number" and value % 1 == 0 and value > 0
    end
    assert(positive_integer(progression.first_threshold)
        and positive_integer(progression.threshold_step), "invalid XP curve")
    local stats_seen = {}
    local allowed_stats = {max_health=true, damage=true, defense=true, speed=true,
        crit_chance=true, crit_damage=true}
    for _, stat in ipairs(progression.stat_order) do
        assert(allowed_stats[stat] and not stats_seen[stat]
            and positive_integer(progression.stat_weights[stat])
            and positive_integer(progression.growth[stat]), "invalid stat growth")
        stats_seen[stat] = true
    end
    assert(#progression.stat_order == 6, "expected six progression stats")
    assert(positive_integer(adventurer.crit_damage_percent)
        and type(adventurer.crit_chance_ppm) == "number"
        and adventurer.crit_chance_ppm % 1 == 0 and adventurer.crit_chance_ppm >= 0,
        "invalid starting crit stats")
    for _, id in ipairs({"core:chest/standard", "core:chest/free_elite"}) do
        local chest = assert(chests[id], "missing chest definition")
        assert(chest.id == id and type(chest.name) == "string"
            and type(chest.price) == "number" and chest.price % 1 == 0
            and chest.price >= 0, "invalid chest definition")
        local total = 0
        for _, rarity in ipairs({"common", "uncommon", "rare", "legendary"}) do
            local weight = chest.rarity_weights[rarity]
            assert(type(weight) == "number" and weight % 1 == 0 and weight >= 0,
                "invalid chest rarity weight")
            total = total + weight
        end
        assert(total > 0, "empty chest rarity profile")
    end
    for _, coefficient in pairs(ability_proc_coefficients) do
        assert(type(coefficient) == "number" and coefficient >= 0 and coefficient <= 10,
            "invalid ability proc coefficient")
    end
    local ids = { "core:map/test_room", "core:map/scrolling_room",
        Finale.map.id }
    local seen = {}
    for _, id in ipairs(ids) do
        assert(not seen[id], "duplicate content ID")
        seen[id] = true
        local map = assert(Content.get_map(id), "missing map definition")
        assert(map.id == id, "map definition ID mismatch")
        Content.validate_map(map)
    end
    local enemy_ids = {
        "core:enemy/mossbound_guard", "core:enemy/thornspitter", "core:enemy/ruin_skitter",
        "core:enemy/spore_herald","core:enemy/canal_lancer",
        "core:enemy/cinder_bellkeeper",
        "core:enemy/thorn_warden", "core:enemy/extraction_pursuer",
        "core:enemy/hollow_regent",
    }
    for _,row in ipairs(biome_elites) do
        enemy_ids[#enemy_ids+1]="core:enemy/"..row[1]
    end
    for _,row in ipairs(expansion_regulars) do
        enemy_ids[#enemy_ids+1]="core:enemy/"..row[1]
    end
    for _,row in ipairs(Squads.enemies) do enemy_ids[#enemy_ids+1]='core:enemy/'..row[1] end
    for _, id in ipairs(enemy_ids) do
        assert(not seen[id], "duplicate content ID")
        seen[id] = true
        local definition = assert(enemies[id], "missing enemy definition")
        assert(definition.id == id and (definition.ai == "guard" or definition.ai == "spitter"
            or definition.ai == "skitter" or definition.ai == "warden"
            or definition.ai == "pursuer" or definition.ai=="spore_herald"
            or definition.ai=="canal_lancer" or definition.ai=="cinder_bellkeeper"
            or definition.ai=="hollow_regent" or definition.ai=="biome_elite"
            or definition.ai=="expansion_regular" or definition.ai=='squad'),
            "invalid enemy identity")
        for _, field in ipairs({"hp", "damage", "defense", "speed", "windup", "recovery", "move_cost", "range",
            "xp_reward", "currency_drop"}) do
            local value = definition[field]
            assert(type(value) == "number" and value % 1 == 0 and value >= 0, "invalid enemy stat")
        end
        assert(definition.hp > 0 and definition.speed > 0 and definition.windup > 0
            and definition.recovery > 0 and definition.move_cost > 0 and definition.range > 0
            and (definition.invulnerable or definition.ai=="hollow_regent"
                or (definition.xp_reward > 0 and definition.currency_drop > 0)),
            "invalid enemy timing")
        if definition.ai == "spitter" then
            assert(type(definition.min_range) == "number" and definition.min_range % 1 == 0
                and definition.min_range >= 2 and definition.min_range <= definition.range,
                "invalid ranged minimum")
        end
    end
    for _,biome_id in ipairs(Biomes.order) do
        local biome=Biomes.get(biome_id)
        assert(enemies[biome.elite_id] and
            (enemies[biome.elite_id].ai=="warden"
                or enemies[biome.elite_id].ai=="biome_elite"),
            "unknown biome elite")
        for _,enemy_id in ipairs(biome.enemy_ids) do
            assert(enemies[enemy_id] and enemies[enemy_id].ai~="biome_elite"
                and enemies[enemy_id].ai~="warden",
                "unknown or invalid biome encounter")
        end
    end
    assert(stage_rules.elite_activation_radius>0 and stage_rules.grace>0
        and stage_rules.pressure_first>0 and stage_rules.pressure_max_active>0
        and #stage_rules.pressure_periods>0 and #stage_rules.routes>=2,
        "invalid stage rules")
    for index,period in ipairs(stage_rules.pressure_periods) do
        assert(positive_integer(period) and
            (index==1 or period<=stage_rules.pressure_periods[index-1]),
            "pressure frequency must rise with stage pressure")
    end
    local route_ids={}
    for _,route in ipairs(stage_rules.routes) do
        assert(type(route.id)=="string" and not route_ids[route.id]
            and type(route.name)=="string" and type(route.description)=="string",
            "invalid stage route")
        route_ids[route.id]=true
    end
    local demo_map = maps["core:map/scrolling_room"]
    local occupied = {}
    for _, spawn in ipairs(demo_spawns) do
        assert(enemies[spawn.definition_id], "unknown demo enemy")
        assert(Content.walkable(demo_map, spawn.x, spawn.y), "demo spawn blocked")
        local key = spawn.x .. "," .. spawn.y
        assert(not occupied[key], "duplicate demo spawn")
        assert(spawn.x ~= demo_map.start_x or spawn.y ~= demo_map.start_y,
            "demo enemy overlaps player")
        occupied[key] = true
    end
    for _, spawn in ipairs(demo_chests) do
        assert(chests[spawn.definition_id], "unknown demo chest")
        assert(Content.walkable(demo_map, spawn.x, spawn.y), "demo chest blocked")
        local key = spawn.x .. "," .. spawn.y
        assert(not occupied[key], "demo chest overlaps enemy")
        assert(spawn.x ~= demo_map.start_x or spawn.y ~= demo_map.start_y,
            "demo chest overlaps player")
        occupied[key] = true
    end
    return true
end

return Content
