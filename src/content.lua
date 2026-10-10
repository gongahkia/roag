local Content = {}

Content.version = "roeg-content/1"

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
    if x < 1 or x > map.width or y < 1 or y > map.height then return false end
    return map.rows[y]:sub(x, x) == "."
end

function Content.validate_all()
    local ids = { "core:map/test_room", "core:map/scrolling_room" }
    local seen = {}
    for _, id in ipairs(ids) do
        assert(not seen[id], "duplicate content ID")
        seen[id] = true
        local map = assert(maps[id], "missing map definition")
        assert(map.id == id, "map definition ID mismatch")
        Content.validate_map(map)
    end
    return true
end

return Content
