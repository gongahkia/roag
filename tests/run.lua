package.path = "./?.lua;./?/init.lua;" .. package.path
assert(_G.love == nil, "headless tests must not initialize LÖVE")

local Content = require("src.content")
local Game = require("src.game")
local RNG = require("src.rng")
local Scheduler = require("src.scheduler")
local Camera = require("src.camera")

local passed, failed = 0, 0
local function equal(a, b, path)
    path = path or "root"
    assert(type(a) == type(b), path .. " type differs")
    if type(a) ~= "table" then assert(a == b, path .. " differs"); return end
    for key, value in pairs(a) do equal(value, b[key], path .. "." .. tostring(key)) end
    for key in pairs(b) do assert(a[key] ~= nil, path .. " extra key " .. tostring(key)) end
end
local function test(name, fn)
    local ok, err = pcall(fn)
    if ok then passed = passed + 1; print("PASS " .. name)
    else failed = failed + 1; io.stderr:write("FAIL " .. name .. ": " .. tostring(err) .. "\n") end
end

test("map content identity and validation", function()
    assert(Content.validate_all())
    local map = Content.get_map("core:map/test_room")
    assert(map.width == 15 and map.height == 11)
    assert(map.id == "core:map/test_room")
end)

test("idle state unchanged", function()
    local state = Game.new(123)
    local before = Game.snapshot(state)
    for _ = 1, 10000 do Game.player(state) end
    equal(before, Game.snapshot(state))
end)

test("four legal cardinal movements cost 100 each", function()
    local state = Game.new(123)
    local steps = {{1, 0}, {0, 1}, {-1, 0}, {0, -1}}
    for index, step in ipairs(steps) do
        local committed, _, events = Game.submit(state, { kind = "move", dx = step[1], dy = step[2] })
        assert(committed and #events == 1 and events[1].kind == "Moved")
        assert(state.clock == index * 100)
    end
    local pos = Game.player(state).position
    assert(pos.x == 2 and pos.y == 2)
end)

test("wall movement consumes no time", function()
    local state = Game.new(123)
    local before = Game.snapshot(state)
    local ok, reason, events = Game.submit(state, { kind = "move", dx = -1, dy = 0 })
    assert(not ok and reason == "wall" and #events == 0)
    equal(before, Game.snapshot(state))
end)

test("pending effects survive snapshot and precede ready actor", function()
    local original = Game.new(88)
    assert(Game.submit(original, { kind = "wait" }))
    Scheduler.enqueue(original, { kind = "effect", due = 200, effect_id = "test:future" })
    local restored = Game.restore(Game.snapshot(original))
    local a_ok, _, a_events = Game.submit(original, { kind = "wait" })
    local b_ok, _, b_events = Game.submit(restored, { kind = "wait" })
    assert(a_ok and b_ok)
    equal(a_events, b_events)
    assert(#a_events == 2 and a_events[2].kind == "ScheduledEffectResolved")
    assert(a_events[2].time == 200 and original.clock == 200)
    equal(Game.snapshot(original), Game.snapshot(restored))
end)

test("empty eight-direction attacks resolve immediately and cost 100", function()
    local state = Game.new(123)
    local directions = {{-1,-1},{0,-1},{1,-1},{-1,0},{1,0},{-1,1},{0,1},{1,1}}
    for index, direction in ipairs(directions) do
        local old_time = state.clock
        local ok, _, events = Game.submit(state,
            { kind = "basic_attack", dx = direction[1], dy = direction[2] })
        assert(ok and #events == 1 and events[1].kind == "AttackPerformed")
        assert(events[1].time == old_time and state.clock == index * 100)
    end
end)

test("targeting cancel consumes no time", function()
    local state = Game.new(123)
    local before = Game.snapshot(state)
    local ok, reason, events = Game.submit(state, { kind = "cancel" })
    assert(not ok and reason == "cancelled" and #events == 0)
    equal(before, Game.snapshot(state))
end)

test("wait costs 100 and invalid directions do not commit", function()
    local state = Game.new(123)
    local before = Game.snapshot(state)
    assert(not Game.submit(state, { kind = "move", dx = 1, dy = 1 }))
    equal(before, Game.snapshot(state))
    local ok, _, events = Game.submit(state, { kind = "wait" })
    assert(ok and state.clock == 100 and events[1].kind == "Waited")
end)

test("scheduler tie order: committed effects, player, stable enemies", function()
    local state = { clock = 0, schedule = {}, next_schedule_id = 1 }
    Scheduler.enqueue(state, { kind = "actor_ready", due = 100, actor_id = 10, is_player = false })
    Scheduler.enqueue(state, { kind = "actor_ready", due = 100, actor_id = 1, is_player = true })
    Scheduler.enqueue(state, { kind = "actor_ready", due = 100, actor_id = 11, is_player = false })
    Scheduler.enqueue(state, { kind = "effect", due = 100, effect_id = "test:first" })
    Scheduler.enqueue(state, { kind = "effect", due = 100, effect_id = "test:second" })
    local order = {}
    for _ = 1, 5 do order[#order + 1] = Scheduler.pop(state).id end
    equal(order, {4, 5, 2, 1, 3})
end)

test("RNG same seed and saved state reproduce draws", function()
    local a, b = RNG.new(987654), RNG.new(987654)
    for _ = 1, 20 do assert(RNG.next_int(a, 17) == RNG.next_int(b, 17)) end
    local saved = { state = a.state }
    local expected = {}
    for index = 1, 20 do expected[index] = RNG.next_int(a, 7) end
    local restored = RNG.new(saved.state)
    for index = 1, 20 do assert(RNG.next_int(restored, 7) == expected[index]) end
end)

test("fixed seed and intents replay state and ordered events", function()
    local intents = {
        { kind = "move", dx = 1, dy = 0 }, { kind = "wait" },
        { kind = "basic_attack", dx = -1, dy = -1 },
        { kind = "move", dx = 0, dy = 1 },
    }
    local function run()
        local state, output = Game.new(77), {}
        RNG.next_int(state.rng, 100)
        for _, intent in ipairs(intents) do
            local ok, _, events = Game.submit(state, intent)
            assert(ok)
            for _, event in ipairs(events) do output[#output + 1] = event end
        end
        return Game.snapshot(state), output
    end
    local a, ae = run()
    local b, be = run()
    equal(a, b)
    equal(ae, be)
end)

test("snapshot restore preserves subsequent simulation", function()
    local original = Game.new(42)
    assert(Game.submit(original, { kind = "move", dx = 1, dy = 0 }))
    RNG.next_int(original.rng, 50)
    local snapshot = Game.snapshot(original)
    local restored = Game.restore(snapshot)
    assert(restored ~= original and restored.entities ~= original.entities)
    equal(snapshot, Game.snapshot(restored))
    local next_actions = {
        { kind = "basic_attack", dx = 1, dy = 1 },
        { kind = "wait" },
        { kind = "move", dx = 0, dy = 1 },
    }
    for _, intent in ipairs(next_actions) do
        local a_ok, _, a_events = Game.submit(original, intent)
        local b_ok, _, b_events = Game.submit(restored, intent)
        assert(a_ok and b_ok)
        equal(a_events, b_events)
        assert(RNG.next_int(original.rng, 31) == RNG.next_int(restored.rng, 31))
    end
    equal(Game.snapshot(original), Game.snapshot(restored))
end)

test("snapshot rejects incompatible content", function()
    local snapshot = Game.snapshot(Game.new(1))
    snapshot.content_version = "other-content"
    assert(not pcall(Game.restore, snapshot))
end)

test("authored scrolling map is larger than the camera viewport", function()
    local map = Content.get_map("core:map/scrolling_room")
    assert(Content.validate_all())
    assert(map.width == 35 and map.height == 25)
    assert(map.width * 32 > 704 and map.height * 32 > 608)
    local state = Game.new(1, map.id)
    assert(state.map_id == map.id)
end)

test("Sword Strike keeps all eight adjacent targets and one recovery", function()
    local directions = {{0,-1},{1,-1},{1,0},{1,1},{0,1},{-1,1},{-1,0},{-1,-1}}
    local state = Game.new(2)
    for index, direction in ipairs(directions) do
        local before = state.clock
        local preview = Game.preview(state, "basic_attack", direction[1], direction[2])
        assert(preview.valid and #preview.cells == 1)
        local ok, _, events = Game.submit(state, {
            kind = "basic_attack", dx = direction[1], dy = direction[2],
        })
        assert(ok and #events == 1 and events[1].kind == "AttackPerformed")
        assert(events[1].time == before and events[1].ability_id == "core:ability/basic_attack")
        assert(events[1].target_cells[1].x == preview.cells[1].x)
        assert(events[1].target_cells[1].y == preview.cells[1].y)
        assert(state.clock == index * 100 and state.last_action.cost == 100)
    end
end)

test("Sweep N and NE arcs use counterclockwise-center-clockwise order", function()
    local state = Game.new(3)
    local north = Game.preview(state, "sweeping_slash", 0, -1)
    equal(north.cells, {{x=1,y=1},{x=2,y=1},{x=3,y=1}})
    local diagonal = Game.preview(state, "sweeping_slash", 1, -1)
    equal(diagonal.cells, {{x=2,y=1},{x=3,y=1},{x=3,y=2}})
    local before = Game.snapshot(state)
    assert(north.valid and diagonal.valid)
    equal(before, Game.snapshot(state))
    local ok, _, events = Game.submit(state, { kind = "sweeping_slash", dx = 1, dy = -1 })
    assert(ok and #events == 1 and events[1].kind == "AttackPerformed")
    assert(events[1].time == 0 and events[1].ability_id == "core:ability/sweeping_slash")
    assert(state.clock == 150 and state.last_action.cost == 150)
    for index, cell in ipairs(diagonal.cells) do
        assert(events[1].target_cells[index].x == cell.x and events[1].target_cells[index].y == cell.y)
    end
end)

test("all eight Sweep arcs have three distinct adjacent cells", function()
    local state = Game.new(3)
    local directions = {{0,-1},{1,-1},{1,0},{1,1},{0,1},{-1,1},{-1,0},{-1,-1}}
    for _, direction in ipairs(directions) do
        local preview = Game.preview(state, "sweeping_slash", direction[1], direction[2])
        assert(preview.valid and #preview.cells == 3)
        local seen = {}
        for _, cell in ipairs(preview.cells) do
            local key = cell.x .. "," .. cell.y
            assert(not seen[key])
            seen[key] = true
            assert(math.abs(cell.x - 2) <= 1 and math.abs(cell.y - 2) <= 1)
        end
    end
end)

test("Dash first wall rejects with no state or time change", function()
    local state = Game.new(4)
    local preview = Game.preview(state, "dash", -1, 0)
    assert(not preview.valid and preview.distance == 0)
    assert(preview.cells[1].status == "wall" and preview.cells[2].status == "unreachable")
    local before = Game.snapshot(state)
    local ok, reason, events = Game.submit(state, { kind = "dash", dx = -1, dy = 0 })
    assert(not ok and reason == "wall" and #events == 0)
    equal(before, Game.snapshot(state))
end)

test("Dash partial path spends 130 once when second tile is wall", function()
    local state = Game.new(4)
    assert(Game.submit(state, { kind = "move", dx = 0, dy = 1 }))
    local preview = Game.preview(state, "dash", 1, 0)
    assert(preview.valid and preview.distance == 1)
    assert(preview.cells[1].status == "open" and preview.cells[2].status == "wall")
    local ok, _, events = Game.submit(state, { kind = "dash", dx = 1, dy = 0 })
    assert(ok and state.clock == 230 and state.last_action.cost == 130)
    assert(state.last_action.distance == 1 and #events == 1)
    assert(Game.player(state).position.x == 3 and Game.player(state).position.y == 3)
    assert(#events[1].path == 1 and events[1].path[1].x == 3)
end)

test("Dash crosses two open cells and emits one ordered path", function()
    local state = Game.new(4)
    local preview = Game.preview(state, "dash", 1, 0)
    assert(preview.valid and preview.distance == 2)
    local ok, _, events = Game.submit(state, { kind = "dash", dx = 1, dy = 0 })
    assert(ok and state.clock == 130 and #events == 1 and events[1].kind == "Moved")
    assert(events[1].time == 0 and events[1].x == 4 and events[1].y == 2)
    assert(events[1].path[1].x == 3 and events[1].path[2].x == 4)
end)

test("Dash stops after open edge cell when second step is out of bounds", function()
    local state = Game.new(4, "core:map/scrolling_room")
    local position = Game.player(state).position
    position.x, position.y = 34, 12
    local preview = Game.preview(state, "dash", 1, 0)
    assert(preview.valid and preview.distance == 1)
    assert(preview.cells[1].status == "open" and preview.cells[2].status == "wall")
    assert(Game.submit(state, { kind = "dash", dx = 1, dy = 0 }))
    assert(position.x == 35 and state.clock == 130)
end)

test("Dash accepts all four cardinals with the authored path outcome", function()
    local cases = {
        {dx=0,dy=-1,distance=1}, {dx=1,dy=0,distance=2},
        {dx=0,dy=1,distance=2}, {dx=-1,dy=0,distance=1},
    }
    for _, case in ipairs(cases) do
        local state = Game.new(4, "core:map/scrolling_room")
        local preview = Game.preview(state, "dash", case.dx, case.dy)
        assert(preview.valid and preview.distance == case.distance)
        assert(Game.submit(state, {kind="dash",dx=case.dx,dy=case.dy}))
        local position = Game.player(state).position
        assert(position.x == 3 + case.dx * case.distance)
        assert(position.y == 3 + case.dy * case.distance)
        assert(state.clock == 130 and state.last_action.cost == 130)
    end
end)

test("blocking entities stop move and Dash without bump combat", function()
    local state = Game.new(5)
    state.entities[2] = { id = 2, position = {floor_id = state.active_floor_id, x = 3, y = 2},
        blocks_movement = true }
    state.next_entity_id = 3
    local before = Game.snapshot(state)
    local move_ok, move_reason = Game.submit(state, { kind = "move", dx = 1, dy = 0 })
    local dash_ok, dash_reason = Game.submit(state, { kind = "dash", dx = 1, dy = 0 })
    assert(not move_ok and move_reason == "occupied" and not dash_ok and dash_reason == "occupied")
    equal(before, Game.snapshot(state))
    state.entities[2].position.x = 4
    local preview = Game.preview(state, "dash", 1, 0)
    assert(preview.valid and preview.distance == 1 and preview.cells[2].status == "occupied")
    assert(Game.submit(state, { kind = "dash", dx = 1, dy = 0 }))
    assert(Game.player(state).position.x == 3 and state.clock == 130)
end)

test("invalid new intents and preview/cancel spend no time", function()
    local state = Game.new(5)
    local before = Game.snapshot(state)
    assert(not Game.preview(state, "dash", 1, 1).valid)
    assert(not Game.preview(state, "sweeping_slash", 0, 0).valid)
    assert(not Game.submit(state, { kind = "dash", dx = 1, dy = 1 }))
    assert(not Game.submit(state, { kind = "sweeping_slash", dx = 0, dy = 0 }))
    assert(not Game.submit(state, { kind = "cancel" }))
    equal(before, Game.snapshot(state))
end)

test("new abilities replay and restore with identical future events", function()
    local intents = {
        {kind="dash", dx=1, dy=0},
        {kind="sweeping_slash", dx=1, dy=-1},
        {kind="basic_attack", dx=-1, dy=1},
        {kind="wait"},
    }
    local function run()
        local state, output = Game.new(789), {}
        RNG.next_int(state.rng, 99)
        for _, intent in ipairs(intents) do
            local ok, _, events = Game.submit(state, intent)
            assert(ok)
            for _, event in ipairs(events) do output[#output + 1] = event end
        end
        return state, output
    end
    local a, ae = run()
    local b, be = run()
    equal(Game.snapshot(a), Game.snapshot(b))
    equal(ae, be)
    local restored = Game.restore(Game.snapshot(a))
    for _, intent in ipairs({{kind="sweeping_slash",dx=0,dy=1},{kind="dash",dx=1,dy=0}}) do
        local a_ok, _, a_events = Game.submit(a, intent)
        local b_ok, _, b_events = Game.submit(restored, intent)
        assert(a_ok and b_ok)
        equal(a_events, b_events)
    end
    equal(Game.snapshot(a), Game.snapshot(restored))
end)

test("camera follows smoothly, clamps, centers small maps, leaves simulation idle", function()
    local state = Game.new(6, "core:map/scrolling_room")
    local before = Game.snapshot(state)
    local viewport = {x=16,y=16,width=704,height=608}
    local camera = Camera.new(Content.get_map(state.map_id), viewport, 32, 3, 3)
    assert(camera.x == 352 and camera.y == 304)
    Camera.update(camera, 0.06, 25, 20)
    assert(camera.x > 352 and camera.x < 768 and camera.y > 304 and camera.y < 496)
    Camera.update(camera, 5, 35, 25)
    assert(math.abs(camera.x - 768) < 0.001 and math.abs(camera.y - 496) < 0.001)
    equal(before, Game.snapshot(state))
    local small = Camera.new(Content.get_map("core:map/test_room"), viewport, 32, 2, 2)
    assert(small.x == 240 and small.y == 176)
    local sx, sy = Camera.tile_top_left(small, 1, 1)
    assert(sx == 128 and sy == 144)
    Camera.update(small, 2, 14, 10)
    assert(small.x == 240 and small.y == 176)
    local one_step = Camera.new(Content.get_map(state.map_id), viewport, 32, 3, 3)
    local two_steps = Camera.new(Content.get_map(state.map_id), viewport, 32, 3, 3)
    Camera.update(one_step, 0.12, 20, 15)
    Camera.update(two_steps, 0.06, 20, 15)
    Camera.update(two_steps, 0.06, 20, 15)
    assert(math.abs(one_step.x - two_steps.x) < 0.000001)
    assert(math.abs(one_step.y - two_steps.y) < 0.000001)
end)

print(("%d passed, %d failed"):format(passed, failed))
if failed > 0 then os.exit(1) end
