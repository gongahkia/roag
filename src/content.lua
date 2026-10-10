local Content = {}

Content.version = "roeg-content/2"

local tiles = {
    ["."] = { walkable = true, blocks_vision = false, projectile_passable = true },
    ["#"] = { walkable = false, blocks_vision = true, projectile_passable = false },
}

local adventurer = { hp = 24, damage = 4, slash_damage = 3, defense = 0, speed = 100 }
local ability_proc_coefficients = {
    ["core:ability/basic_attack"] = 1,
    ["core:ability/sweeping_slash"] = 1,
    ["core:ability/guard_strike"] = 1,
    ["core:ability/thorn_lane"] = 1,
    ["core:ability/skitter_stab"] = 1,
}

local enemies = {
    ["core:enemy/mossbound_guard"] = {
        id = "core:enemy/mossbound_guard", name = "Mossbound Guard", ai = "guard",
        hp = 10, damage = 3, defense = 1, speed = 70,
        windup = 80, recovery = 120, move_cost = 100, range = 1,
    },
    ["core:enemy/thornspitter"] = {
        id = "core:enemy/thornspitter", name = "Thornspitter", ai = "spitter",
        hp = 6, damage = 2, defense = 0, speed = 90,
        windup = 90, recovery = 130, move_cost = 100, min_range = 2, range = 7,
    },
    ["core:enemy/ruin_skitter"] = {
        id = "core:enemy/ruin_skitter", name = "Ruin Skitter", ai = "skitter",
        hp = 4, damage = 1, defense = 0, speed = 160,
        windup = 35, recovery = 70, move_cost = 100, range = 1,
    },
}

local demo_spawns = {
    { definition_id = "core:enemy/mossbound_guard", x = 7, y = 3 },
    { definition_id = "core:enemy/thornspitter", x = 3, y = 9 },
    { definition_id = "core:enemy/ruin_skitter", x = 6, y = 6 },
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

function Content.get_enemy(id)
    return enemies[id]
end

function Content.get_adventurer()
    return adventurer
end

function Content.ability_proc_coefficient(id)
    return ability_proc_coefficients[id] or 1
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

function Content.projectile_passable(map, x, y)
    local tile = tile_at(map, x, y)
    return tile ~= nil and tile.projectile_passable
end

function Content.blocks_vision(map, x, y)
    local tile = tile_at(map, x, y)
    return tile == nil or tile.blocks_vision
end

function Content.validate_all()
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
    }
    for _, id in ipairs(enemy_ids) do
        assert(not seen[id], "duplicate content ID")
        seen[id] = true
        local definition = assert(enemies[id], "missing enemy definition")
        assert(definition.id == id and (definition.ai == "guard" or definition.ai == "spitter"
            or definition.ai == "skitter"), "invalid enemy identity")
        for _, field in ipairs({"hp", "damage", "defense", "speed", "windup", "recovery", "move_cost", "range"}) do
            local value = definition[field]
            assert(type(value) == "number" and value % 1 == 0 and value >= 0, "invalid enemy stat")
        end
        assert(definition.hp > 0 and definition.speed > 0 and definition.windup > 0
            and definition.recovery > 0 and definition.move_cost > 0 and definition.range > 0,
            "invalid enemy timing")
        if definition.ai == "spitter" then
            assert(type(definition.min_range) == "number" and definition.min_range % 1 == 0
                and definition.min_range >= 2 and definition.min_range <= definition.range,
                "invalid ranged minimum")
        end
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
    return true
end

return Content
