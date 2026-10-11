local Content = {}

Content.version = "roeg-content/6"

local tiles = {
    ["."] = { walkable = true, blocks_vision = false, projectile_passable = true },
    ["#"] = { walkable = false, blocks_vision = true, projectile_passable = false },
}

local adventurer = {
    hp = 24, damage = 4, slash_damage = 3, defense = 0, speed = 100,
    crit_chance_ppm = 0, crit_damage_percent = 150,
}

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
    routes={
        {id="core:route/verdant",name="Verdant passage",description="Prototype: Overgrown Ruins layout"},
        {id="core:route/ruined",name="Ruined causeway",description="Prototype: Overgrown Ruins layout"},
    },
}
local ability_proc_coefficients = {
    ["core:ability/basic_attack"] = 1,
    ["core:ability/sweeping_slash"] = 1,
    ["core:ability/guard_strike"] = 1,
    ["core:ability/thorn_lane"] = 1,
    ["core:ability/skitter_stab"] = 1,
    ["core:ability/warden_sweep"] = 1,
    ["core:ability/warden_roots"] = 1,
    ["core:ability/pursuer_strike"] = 0,
}

local enemies = {
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
}

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
    return maps[id]
end

-- Generated runs carry their concrete map; authored fixtures remain registry lookups.
function Content.map_for(state)
    return state.generated_map or Content.get_map(state.map_id)
end

function Content.get_enemy(id)
    return enemies[id]
end

function Content.get_adventurer()
    return adventurer
end

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
        assert(not row:find("[^.#]"), "unknown map tile")
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

function Content.blocks_vision(map, x, y, state)
    local tile = tile_at(map, x, y)
    return tile == nil or tile.blocks_vision
        or blocking_entity(state,map,x,y,"blocks_vision")
end

function Content.validate_all()
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
    local ids = { "core:map/test_room", "core:map/scrolling_room" }
    local seen = {}
    for _, id in ipairs(ids) do
        assert(not seen[id], "duplicate content ID")
        seen[id] = true
        local map = assert(maps[id], "missing map definition")
        assert(map.id == id, "map definition ID mismatch")
        Content.validate_map(map)
    end
    local enemy_ids = {
        "core:enemy/mossbound_guard", "core:enemy/thornspitter", "core:enemy/ruin_skitter",
        "core:enemy/thorn_warden", "core:enemy/extraction_pursuer",
    }
    for _, id in ipairs(enemy_ids) do
        assert(not seen[id], "duplicate content ID")
        seen[id] = true
        local definition = assert(enemies[id], "missing enemy definition")
        assert(definition.id == id and (definition.ai == "guard" or definition.ai == "spitter"
            or definition.ai == "skitter" or definition.ai == "warden"
            or definition.ai == "pursuer"), "invalid enemy identity")
        for _, field in ipairs({"hp", "damage", "defense", "speed", "windup", "recovery", "move_cost", "range",
            "xp_reward", "currency_drop"}) do
            local value = definition[field]
            assert(type(value) == "number" and value % 1 == 0 and value >= 0, "invalid enemy stat")
        end
        assert(definition.hp > 0 and definition.speed > 0 and definition.windup > 0
            and definition.recovery > 0 and definition.move_cost > 0 and definition.range > 0
            and (definition.invulnerable or (definition.xp_reward > 0 and definition.currency_drop > 0)),
            "invalid enemy timing")
        if definition.ai == "spitter" then
            assert(type(definition.min_range) == "number" and definition.min_range % 1 == 0
                and definition.min_range >= 2 and definition.min_range <= definition.range,
                "invalid ranged minimum")
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
            "invalid prototype route")
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
