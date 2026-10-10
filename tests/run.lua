package.path = "./?.lua;./?/init.lua;" .. package.path
assert(_G.love == nil, "headless tests must not initialize LÖVE")

local Content = require("src.content")
local Game = require("src.game")
local RNG = require("src.rng")
local Scheduler = require("src.scheduler")
local Camera = require("src.camera")
local AI = require("src.ai")
local Playback = require("src.playback")
local Targeting = require("src.targeting")
local Boons = require("src.boons")
local BoonContent = require("src.boon_content")
local Rewards = require("src.rewards")

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
        assert(committed and #events == 2 and events[1].kind == "Moved"
            and events[2].kind == "TileEntered")
        assert(events[2].x == Game.player(state).position.x
            and events[2].y == Game.player(state).position.y)
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
    assert(state.last_action.distance == 1 and #events == 2)
    assert(events[2].kind == "TileEntered" and events[2].x == 3)
    assert(Game.player(state).position.x == 3 and Game.player(state).position.y == 3)
    assert(#events[1].path == 1 and events[1].path[1].x == 3)
end)

test("Dash crosses two open cells and emits one ordered path", function()
    local state = Game.new(4)
    local preview = Game.preview(state, "dash", 1, 0)
    assert(preview.valid and preview.distance == 2)
    local ok, _, events = Game.submit(state, { kind = "dash", dx = 1, dy = 0 })
    assert(ok and state.clock == 130 and #events == 3 and events[1].kind == "Moved")
    assert(events[2].kind == "TileEntered" and events[2].x == 3
        and events[3].kind == "TileEntered" and events[3].x == 4)
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

local function events_of(events, kind)
    local result = {}
    for _, event in ipairs(events) do
        if event.kind == kind then result[#result + 1] = event end
    end
    return result
end

test("enemy bump is one basic hit and one recovery without movement", function()
    local state = Game.new(10)
    local enemy = Game.spawn_enemy(state, "core:enemy/mossbound_guard", 3, 2, 1000)
    local preview = Game.preview(state, "move", 1, 0)
    assert(preview.valid and preview.bump and preview.distance == 0)
    local ok, _, events = Game.submit(state, {kind="move",dx=1,dy=0})
    assert(ok and state.clock == 100 and state.last_action.kind == "basic_attack")
    assert(state.last_action.bump and state.last_action.cost == 100)
    assert(Game.player(state).position.x == 2 and enemy.hp == 7)
    assert(#events_of(events, "AttackPerformed") == 1)
    assert(#events_of(events, "DamageTaken") == 1)
    assert(events[1].bump and events[1].action_id == events_of(events,"DamageTaken")[1].action_id)
end)

test("explicit diagonal Sword Strike damages its occupied tile", function()
    local state = Game.new(10)
    local enemy = Game.spawn_enemy(state,"core:enemy/ruin_skitter",3,3,1000)
    local _, _, events = Game.submit(state,{kind="basic_attack",dx=1,dy=1})
    assert(state.clock == 100 and enemy.hp == 0)
    assert(Game.entity(state,enemy.id) == nil)
    assert(events[1].target_x == 3 and events[1].target_y == 3)
    assert(#events_of(events,"DamageTaken") == 1)
end)

test("empty explicit attacks, walls and cancel retain prior semantics with enemies", function()
    local state = Game.new(11)
    Game.spawn_enemy(state, "core:enemy/mossbound_guard", 5, 2, 1000)
    local before = Game.snapshot(state)
    assert(not Game.submit(state, {kind="move",dx=-1,dy=0}))
    assert(not Game.submit(state, {kind="cancel"}))
    equal(before, Game.snapshot(state))
    local ok, _, events = Game.submit(state, {kind="basic_attack",dx=-1,dy=-1})
    assert(ok and state.clock == 100 and #events == 1 and events[1].kind == "AttackPerformed")
end)

test("damage floor, death removal and ordered provenance", function()
    local state = Game.new(12)
    local enemy = Game.spawn_enemy(state, "core:enemy/ruin_skitter", 3, 2, 1000)
    enemy.hp = 2
    local id = enemy.id
    local ok, _, events = Game.submit(state, {kind="basic_attack",dx=1,dy=0})
    assert(ok and Game.entity(state,id) == nil)
    equal({events[1].kind,events[2].kind,events[3].kind,events[4].kind,events[5].kind},
        {"AttackPerformed","DamageBatchResolved","DamageAttempted","DamageTaken","Died"})
    assert(events[3].actual_loss == 2 and events[3].hp_after == 0)
    for _, event in ipairs(events) do
        assert(event.source_id == state.player_id and event.action_id == events[1].action_id)
    end
end)

test("fully blocked damage has no DamageTaken", function()
    local state = Game.new(13)
    local enemy = Game.spawn_enemy(state, "core:enemy/mossbound_guard", 3, 2, 1000)
    enemy.defense = 100
    local hp = enemy.hp
    local _, _, events = Game.submit(state, {kind="basic_attack",dx=1,dy=0})
    assert(enemy.hp == hp and #events_of(events,"DamageAttempted") == 1)
    assert(#events_of(events,"DamageBlocked") == 1)
    assert(#events_of(events,"DamageTaken") == 0 and #events_of(events,"Died") == 0)
end)

test("Sweep commits three target HP outcomes before individual damage records", function()
    local state = Game.new(14, "core:map/scrolling_room")
    local positions = {{2,2},{3,2},{4,2}}
    for _, position in ipairs(positions) do
        local enemy = Game.spawn_enemy(state,"core:enemy/ruin_skitter",position[1],position[2],1000)
        enemy.hp = 2
    end
    local ok, _, events = Game.submit(state, {kind="sweeping_slash",dx=0,dy=-1})
    assert(ok and state.clock == 150 and #state.entities == 1)
    assert(events[1].kind == "AttackPerformed" and events[2].kind == "DamageBatchResolved")
    assert(#events[2].outcomes == 3 and #events_of(events,"Died") == 3)
    for index, outcome in ipairs(events[2].outcomes) do
        assert(outcome.hp_after == 0 and outcome.x == positions[index][1])
    end
    assert(events[3].kind == "DamageAttempted")
end)

test("Dash still rejects first blocker and stops at occupied second tile", function()
    local state = Game.new(15)
    Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2,1000)
    local before = Game.snapshot(state)
    assert(not Game.submit(state,{kind="dash",dx=1,dy=0}))
    equal(before,Game.snapshot(state))
    local enemy = Game.entity(state,2)
    enemy.position.x = 4
    local preview = Game.preview(state,"dash",1,0)
    assert(preview.valid and preview.distance == 1 and preview.cells[2].status == "occupied")
    assert(Game.submit(state,{kind="dash",dx=1,dy=0}))
    assert(state.clock == 130 and Game.player(state).position.x == 3)
end)

test("Guard detours around a wall and commits cardinal melee", function()
    local state = Game.new(16)
    Game.player(state).position.x, Game.player(state).position.y = 2, 3
    local guard = Game.spawn_enemy(state,"core:enemy/mossbound_guard",7,3)
    local first = AI.choose(state,guard)
    assert(first.kind == "move" and first.dy == -1)
    local seen_windup = false
    for _ = 1, 12 do
        local ok, _, events = Game.submit(state,{kind="wait"})
        assert(ok)
        for _, event in ipairs(events) do
            if event.kind == "WindupStarted" and event.source_id == guard.id then
                seen_windup = true
                assert(#event.target_cells == 1)
            end
        end
        if seen_windup then break end
    end
    assert(seen_windup)
    assert(Content.walkable(Content.get_map(state.map_id),guard.position.x,guard.position.y))
end)

test("Thornspitter respects blocking wall and repositions for lane", function()
    local state = Game.new(17)
    Game.player(state).position.x, Game.player(state).position.y = 3, 3
    local spitter = Game.spawn_enemy(state,"core:enemy/thornspitter",7,3)
    local intent = AI.choose(state,spitter)
    assert(intent.kind == "move")
    local _, _, events = Game.submit(state,{kind="wait"})
    assert(#events_of(events,"WindupStarted") == 0)
    assert(spitter.position.x ~= 6 or spitter.position.y ~= 3)
end)

test("Skitter seeks diagonal flank and uses faster locked windup", function()
    local state = Game.new(18)
    local skitter = Game.spawn_enemy(state,"core:enemy/ruin_skitter",3,3)
    local intent = AI.choose(state,skitter)
    assert(intent.kind == "windup" and intent.ability_id == "core:ability/skitter_stab")
    local _, _, events = Game.submit(state,{kind="wait"})
    local starts = events_of(events,"WindupStarted")
    assert(#starts >= 1 and starts[1].due == 22)
    local attacks = events_of(events,"AttackPerformed")
    assert(#attacks >= 1 and attacks[1].time == 22 and attacks[1].time < 100)
    assert(Game.player(state).hp < Game.player(state).max_hp)
end)

test("Skitter repositions from cardinal adjacency while Guard attacks", function()
    local skitter_state = Game.new(18)
    local skitter = Game.spawn_enemy(skitter_state,"core:enemy/ruin_skitter",3,2)
    local before = Game.snapshot(skitter_state)
    local skitter_intent = AI.choose(skitter_state,skitter)
    assert(skitter_intent.kind == "move" and skitter_intent.dx == 0 and skitter_intent.dy == 1)
    equal(before,Game.snapshot(skitter_state))
    local guard_state = Game.new(18)
    local guard = Game.spawn_enemy(guard_state,"core:enemy/mossbound_guard",3,2)
    assert(AI.choose(guard_state,guard).kind == "windup")
end)

test("spitter target lane stays locked while player moves away", function()
    local state = Game.new(19)
    local spitter = Game.spawn_enemy(state,"core:enemy/thornspitter",2,5)
    spitter.speed = 60 -- test a longer windup than the player's 100 recovery
    assert(Game.submit(state,{kind="wait"}))
    assert(state.clock == 100 and spitter.pending_attack and spitter.pending_attack.due == 150)
    local locked = Game.snapshot(spitter.pending_attack.target_cells)
    local hp = Game.player(state).hp
    local _, _, events = Game.submit(state,{kind="move",dx=1,dy=0})
    assert(Game.player(state).position.x == 3 and Game.player(state).hp == hp)
    local strikes = events_of(events,"AttackPerformed")
    assert(#strikes == 1 and strikes[1].source_id == spitter.id and strikes[1].time == 150)
    equal(locked,strikes[1].target_cells)
end)

test("committed impact wins timestamp tie; player wins ready enemy tie", function()
    local state = Game.new(20)
    local spitter = Game.spawn_enemy(state,"core:enemy/thornspitter",2,5)
    local guard = Game.spawn_enemy(state,"core:enemy/mossbound_guard",7,2,100)
    local _, _, events = Game.submit(state,{kind="wait"})
    assert(state.clock == 100 and Game.player(state).hp == 22)
    assert(#events_of(events,"AttackPerformed") == 1)
    local ready = Scheduler.peek(state)
    assert(ready.actor_id == state.player_id and ready.due == 100)
    local _, _, later = Game.submit(state,{kind="wait"})
    local guard_acted = false
    for _, event in ipairs(later) do
        if event.source_id == guard.id then guard_acted = true end
    end
    assert(guard_acted and spitter.id ~= guard.id)
end)

test("equal-time enemy turns keep stable entity sequence", function()
    local state = Game.new(21)
    local first = Game.spawn_enemy(state,"core:enemy/mossbound_guard",7,2)
    local second = Game.spawn_enemy(state,"core:enemy/mossbound_guard",9,2)
    local _, _, events = Game.submit(state,{kind="wait"})
    local at_zero = {}
    for _, event in ipairs(events) do
        if event.time == 0 and event.kind == "Moved" and event.source_id ~= state.player_id then
            at_zero[#at_zero + 1] = event.source_id
        end
    end
    equal(at_zero,{first.id,second.id})
end)

test("enemy ranged strike damages an allied blocker before player", function()
    local state = Game.new(22)
    local spitter = Game.spawn_enemy(state,"core:enemy/thornspitter",2,5)
    local ally = Game.spawn_enemy(state,"core:enemy/mossbound_guard",2,4,1000)
    local player_hp, ally_hp = Game.player(state).hp, ally.hp
    local _, _, events = Game.submit(state,{kind="wait"})
    assert(ally.hp == ally_hp - math.max(0, spitter.damage - ally.defense)
        and Game.player(state).hp == player_hp)
    local taken = events_of(events,"DamageTaken")
    assert(#taken == 1 and taken[1].target_id == ally.id and taken[1].source_id == spitter.id)
end)

test("ordinary damage leaves windup active and impact still occurs", function()
    local state = Game.new(23)
    local guard = Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
    assert(Game.submit(state,{kind="wait"}))
    assert(guard.pending_attack and guard.pending_attack.due == 114)
    local _, _, events = Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    assert(guard.hp == 7 and #events_of(events,"WindupCancelled") == 0)
    assert(#events_of(events,"AttackPerformed") >= 2 and Game.player(state).hp == 21)
end)

test("stun cancels windup and cannot ghost-hit after restore", function()
    local state = Game.new(24)
    local guard = Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
    assert(Game.submit(state,{kind="wait"}))
    local ok, _, events = Game.stun(state,guard.id,200,state.player_id,99)
    assert(ok and #events_of(events,"WindupCancelled") == 1)
    assert(guard.pending_attack == nil)
    local restored = Game.restore(Game.snapshot(state))
    local hp = Game.player(state).hp
    for _, run in ipairs({state,restored}) do
        local _, _, future = Game.submit(run,{kind="wait"})
        assert(#events_of(future,"AttackPerformed") == 0 and Game.player(run).hp == hp)
    end
    equal(Game.snapshot(state),Game.snapshot(restored))
end)

test("displacement cancels windup and prevents late impact", function()
    local state = Game.new(25)
    local guard = Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
    assert(Game.submit(state,{kind="wait"}))
    local ok, _, events = Game.displace(state,guard.id,4,2,state.player_id,100)
    assert(ok and guard.position.x == 4 and guard.pending_attack == nil)
    assert(#events_of(events,"WindupCancelled") == 1)
    local hp = Game.player(state).hp
    local _, _, future = Game.submit(state,{kind="wait"})
    assert(#events_of(future,"AttackPerformed") == 0 and Game.player(state).hp == hp)
end)

test("death cancels pending strike and removes enemy ready work", function()
    local state = Game.new(26)
    local guard = Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
    assert(Game.submit(state,{kind="wait"}))
    guard.hp = 3
    local id = guard.id
    local _, _, events = Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    assert(Game.entity(state,id) == nil and #events_of(events,"Died") == 1)
    assert(#events_of(events,"WindupCancelled") == 1)
    for _, item in ipairs(state.schedule) do assert(item.actor_id ~= id) end
    assert(Game.player(state).hp == 24)
    local restored = Game.restore(Game.snapshot(state))
    local _, _, later = Game.submit(restored,{kind="wait"})
    assert(#events_of(later,"AttackPerformed") == 0 and Game.player(restored).hp == 24)
end)

test("pending windup snapshot restores locked geometry and future ordering", function()
    local state = Game.new(27)
    Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
    assert(Game.submit(state,{kind="wait"}))
    assert(#Game.pending_strikes(state) == 1)
    for _, item in ipairs(state.schedule) do
        assert(item.actor_id ~= 2 or item.kind ~= "actor_ready")
    end
    local restored = Game.restore(Game.snapshot(state))
    equal(Game.pending_strikes(state),Game.pending_strikes(restored))
    local a_ok, _, a_events = Game.submit(state,{kind="wait"})
    local b_ok, _, b_events = Game.submit(restored,{kind="wait"})
    assert(a_ok and b_ok)
    equal(a_events,b_events)
    equal(Game.snapshot(state),Game.snapshot(restored))
end)

test("player death ends the run and snapshot stays restorable", function()
    local state = Game.new(29)
    Game.player(state).hp = 1
    Game.spawn_enemy(state,"core:enemy/thornspitter",2,5)
    local ok, _, events = Game.submit(state,{kind="wait"})
    assert(ok and state.game_over and Game.player(state).hp == 0)
    assert(#events_of(events,"Died") == 1)
    assert(not Game.submit(state,{kind="wait"}))
    local restored = Game.restore(Game.snapshot(state))
    equal(Game.snapshot(state),Game.snapshot(restored))
end)

test("demo replay, idle state and presentation playback are deterministic", function()
    local function run()
        local state = Game.new(28,"core:map/scrolling_room",{demo_enemies=true})
        local before = Game.snapshot(state)
        for _ = 1,1000 do Game.pending_strikes(state) end
        equal(before,Game.snapshot(state))
        local output = {}
        for _ = 1,3 do
            local ok, _, events = Game.submit(state,{kind="wait"})
            assert(ok)
            for _, event in ipairs(events) do output[#output+1] = event end
        end
        return Game.snapshot(state),output
    end
    local a,ae = run()
    local b,be = run()
    equal(a,b)
    equal(ae,be)
    local playback = Playback.new()
    Playback.add(playback,ae,1)
    local before = Game.snapshot(a)
    local previous_id = 0
    while Playback.busy(playback) do
        local current = Playback.current(playback)
        assert(current.id >= previous_id)
        previous_id = current.id
        Playback.update(playback,1)
    end
    equal(before,Game.snapshot(a))
end)

local function quiet_enemy(state, definition_id, x, y)
    local enemy = Game.spawn_enemy(state, definition_id, x, y)
    Scheduler.remove_for_actor(state, enemy.id)
    return enemy
end

local function grant(state, name, rarity, count)
    Game.set_boon_stacks(state, state.player_id, "core:boon/" .. name, rarity, count or 1)
end

test("boon catalogue, four rarity strengths and invalid references", function()
    assert(BoonContent.validate())
    for _, id in ipairs(BoonContent.order) do
        local def = BoonContent.get(id)
        local previous = -1
        for _, rarity in ipairs(BoonContent.rarities) do
            local value = def.parameters[rarity].power
            assert(value > previous)
            previous = value
        end
    end
    local copy_defs = {}
    for id, def in pairs(BoonContent.definitions) do copy_defs[id] = def end
    copy_defs["core:boon/storm_conductor"] = nil
    assert(not pcall(BoonContent.validate, copy_defs, BoonContent.order))
    assert(not pcall(Game.set_boon_stacks, Game.new(1), 1, "missing:boon", "rare", 1))
end)

test("boon inventory mixed stacks, removal and snapshot", function()
    local state = Game.new(91)
    grant(state, "storm_conductor", "common", 1)
    grant(state, "storm_conductor", "rare", 2)
    grant(state, "storm_conductor", "legendary", 1000)
    assert(Boons.aggregate("core:boon/storm_conductor", Game.player(state).boons["core:boon/storm_conductor"],
        "chance") == 500380000)
    grant(state, "storm_conductor", "rare", 0)
    assert(Game.player(state).boons["core:boon/storm_conductor"].rare == nil)
    equal(Game.snapshot(state), Game.snapshot(Game.restore(Game.snapshot(state))))
end)

test("mixed rarity additive probability, overflow, zero coefficient and seeded rolls", function()
    local state = Game.new(99)
    local stacks = {common=1, rare=1, legendary=1}
    assert(Boons.aggregate("core:boon/storm_conductor", stacks, "chance") == 700000)
    local before = state.rng.state
    assert(Boons.proc_count(state, 1500000, 0) == 0 and state.rng.state == before)
    local mirror = Game.restore(Game.snapshot(state))
    local saw_one, saw_two = false, false
    for _ = 1, 30 do
        local a = Boons.proc_count(state, 1500000, 1)
        local b = Boons.proc_count(mirror, 1500000, 1)
        assert(a == b and (a == 1 or a == 2))
        saw_one, saw_two = saw_one or a == 1, saw_two or a == 2
    end
    assert(saw_one and saw_two)
end)

test("all four Storm rarities retain lightning behavior and zero attack coefficient suppresses chance only", function()
    local copies = {common=50, uncommon=17, rare=6, legendary=2}
    for _, rarity in ipairs(BoonContent.rarities) do
        local state = Game.new(101)
        grant(state, "storm_conductor", rarity, copies[rarity])
        quiet_enemy(state, "core:enemy/ruin_skitter", 3, 2)
        quiet_enemy(state, "core:enemy/thornspitter", 4, 2)
        local _, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
        assert(#events_of(events, "BoonActivated") >= 1)
        assert(events_of(events, "EffectApplied")[1].damage_type == "lightning")
    end
    local state = Game.new(102)
    grant(state, "storm_conductor", "legendary", 2)
    grant(state, "detonation_bloom", "common")
    quiet_enemy(state, "core:enemy/ruin_skitter", 3, 2)
    local original = Content.ability_proc_coefficient
    Content.ability_proc_coefficient = function(id)
        if id == "core:ability/basic_attack" then return 0 end
        return original(id)
    end
    local ok, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    Content.ability_proc_coefficient = original
    assert(ok and events[1].proc_coefficient == 0)
    assert(#events_of(events, "EntityKilled") == 1)
    local activated = events_of(events, "BoonActivated")
    assert(#activated == 1 and activated[1].boon_id == "core:boon/detonation_bloom")
end)

test("attack contact, empty attack and blocked damage have distinct events", function()
    local state = Game.new(13)
    grant(state, "crescent_reach", "common") -- turns on extended event facts without a proc
    local _, _, miss = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(#events_of(miss, "AttackPerformed") == 1 and #events_of(miss, "HitConfirmed") == 0)
    local target = quiet_enemy(state, "core:enemy/mossbound_guard", 3, 2)
    target.defense = 99
    local _, _, blocked = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(#events_of(blocked, "HitConfirmed") == 1)
    assert(#events_of(blocked, "DamageBlocked") == 1 and #events_of(blocked, "DamageTaken") == 0)
    assert(blocked[1].root_action_id == blocked[1].action_id and blocked[1].proc_coefficient == 1)
    assert(blocked[1].source_position.x == 2)
end)

test("Storm secondary lightning and Static Footsteps movement use real HP pipeline", function()
    local state = Game.new(17)
    grant(state, "storm_conductor", "legendary", 2)
    local first = quiet_enemy(state, "core:enemy/ruin_skitter", 3, 2)
    local second = quiet_enemy(state, "core:enemy/thornspitter", 4, 2)
    local _, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(Game.entity(state, first.id) == nil and Game.entity(state, second.id) == nil)
    assert(#events_of(events, "BoonActivated") == 1)
    assert(#events_of(events, "EffectApplied") == 1)
    local hit = events_of(events, "DamageTaken")
    assert(#hit == 2 and hit[2].damage_type == "lightning"
        and hit[2].source_id == state.player_id and hit[2].chain_id == hit[1].chain_id)

    local movement = Game.new(18)
    grant(movement, "static_footsteps", "legendary", 2)
    local foe = quiet_enemy(movement, "core:enemy/ruin_skitter", 4, 2)
    local _, _, moved = Game.submit(movement, {kind="move", dx=1, dy=0})
    assert(#events_of(moved, "TileEntered") == 1 and #events_of(moved, "BoonActivated") >= 1)
    assert(Game.entity(movement, foe.id) == nil)
end)

test("Thorn Mirror reacts to positive owner damage but not fully blocked damage", function()
    local function run(defense)
        local state = Game.new(41)
        local player = Game.player(state)
        player.position.x, player.position.y, player.defense = 8, 5, defense
        grant(state, "thorn_mirror", "rare")
        local spitter = Game.spawn_enemy(state, "core:enemy/thornspitter", 8, 2)
        local _, _, events = Game.submit(state, {kind="wait"})
        return state, spitter, events
    end
    local hit, attacker, events = run(0)
    assert(Game.player(hit).hp == 22 and attacker.hp == 3)
    assert(#events_of(events, "BoonActivated") == 1)
    local blocked, unharmed, blocked_events = run(99)
    assert(Game.player(blocked).hp == 24 and unharmed.hp == 6)
    assert(#events_of(blocked_events, "DamageTaken") == 0)
    assert(#events_of(blocked_events, "BoonActivated") == 0)
end)

test("Resonant Wounds correlates adjacent event-time targets once per pair and chain", function()
    local function run(positions)
        local state = Game.new(57)
        Game.player(state).position.x, Game.player(state).position.y = 5, 5
        grant(state, "resonant_wounds", "common")
        for _, point in ipairs(positions) do
            quiet_enemy(state, "core:enemy/mossbound_guard", point[1], point[2])
        end
        local _, _, events = Game.submit(state, {kind="sweeping_slash", dx=0, dy=-1})
        return state, events
    end
    local _, adjacent = run({{4,4},{5,4},{6,4}})
    assert(#events_of(adjacent, "BoonActivated") == 2)
    assert(#events_of(adjacent, "EffectApplied") == 2)
    local batch = events_of(adjacent, "DamageBatchResolved")[1]
    assert(#batch.outcomes == 3 and batch.outcomes[1].hp_after == 8
        and batch.outcomes[2].hp_after == 8 and batch.outcomes[3].hp_after == 8)
    local last_primary, first_reaction = 0, nil
    for index, event in ipairs(adjacent) do
        if event.kind == "DamageTaken" and event.damage_type == "physical" then
            last_primary = index
        elseif event.kind == "BoonActivated" and not first_reaction then
            first_reaction = index
        end
    end
    assert(first_reaction and last_primary < first_reaction)
    local _, separated = run({{4,4},{6,4}})
    assert(#events_of(separated, "BoonActivated") == 0)
    local one, single = run({{4,4}})
    assert(#events_of(single, "BoonActivated") == 0)
    local _, _, later = Game.submit(one, {kind="basic_attack", dx=0, dy=-1})
    assert(#events_of(later, "BoonActivated") == 0)

    local separate = Game.new(58)
    Game.player(separate).position.x, Game.player(separate).position.y = 5, 5
    grant(separate, "resonant_wounds", "common")
    quiet_enemy(separate, "core:enemy/mossbound_guard", 5, 4)
    quiet_enemy(separate, "core:enemy/mossbound_guard", 6, 4)
    local _, _, first = Game.submit(separate, {kind="basic_attack", dx=0, dy=-1})
    local _, _, second = Game.submit(separate, {kind="basic_attack", dx=1, dy=-1})
    assert(#events_of(first, "DamageTaken") == 1 and #events_of(second, "DamageTaken") == 1)
    assert(#events_of(first, "BoonActivated") == 0 and #events_of(second, "BoonActivated") == 0)
end)

test("Resonant Wounds correlates distinct sibling lightning branches", function()
    local state = Game.new(59, "core:map/scrolling_room")
    Game.player(state).position.x, Game.player(state).position.y = 5, 5
    grant(state, "storm_conductor", "legendary", 4) -- two guaranteed siblings
    grant(state, "resonant_wounds", "common")
    quiet_enemy(state, "core:enemy/ruin_skitter", 6, 5)
    quiet_enemy(state, "core:enemy/ruin_skitter", 8, 5)
    quiet_enemy(state, "core:enemy/ruin_skitter", 9, 5)
    local _, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    local storm, resonance = 0, 0
    for _, event in ipairs(events_of(events, "BoonActivated")) do
        if event.boon_id == "core:boon/storm_conductor" then storm = storm + 1 end
        if event.boon_id == "core:boon/resonant_wounds" then resonance = resonance + 1 end
    end
    assert(storm == 2 and resonance == 1)
    local lightning = {}
    for _, event in ipairs(events_of(events, "DamageTaken")) do
        if event.damage_type == "lightning" then lightning[#lightning + 1] = event end
    end
    assert(#lightning == 2 and lightning[1].target_id ~= lightning[2].target_id)
    assert(lightning[1].chain_id == lightning[2].chain_id)
end)

test("Resonant Wounds deduplicates each unordered pair despite repeated later damage", function()
    local state = Game.new(60)
    Game.player(state).position.x, Game.player(state).position.y = 5, 5
    grant(state, "storm_conductor", "legendary", 2)
    grant(state, "resonant_wounds", "common")
    for _, point in ipairs({{5,4},{6,4},{6,5}}) do
        local enemy = quiet_enemy(state, "core:enemy/mossbound_guard", point[1], point[2])
        enemy.hp, enemy.max_hp, enemy.defense = 50, 50, 0
    end
    local _, _, events = Game.submit(state, {kind="sweeping_slash", dx=1, dy=-1})
    local resonance = 0
    for _, event in ipairs(events_of(events, "BoonActivated")) do
        if event.boon_id == "core:boon/resonant_wounds" then resonance = resonance + 1 end
    end
    assert(resonance == 3)
    assert(#events_of(events, "DamageTaken") > 3)
end)

test("Turncoat Spark observes enemy friendly fire and credits player reaction", function()
    local state = Game.new(61)
    Game.player(state).position.x, Game.player(state).position.y = 8, 5
    grant(state, "turncoat_spark", "rare")
    local spitter = Game.spawn_enemy(state, "core:enemy/thornspitter", 8, 2)
    local guard = quiet_enemy(state, "core:enemy/mossbound_guard", 8, 4)
    local _, _, events = Game.submit(state, {kind="wait"})
    assert(guard.hp == 9 and spitter.hp == 3)
    assert(#events_of(events, "BoonActivated") == 1)
    local damage = events_of(events, "DamageTaken")
    assert(#damage == 2 and damage[1].source_id == spitter.id and damage[1].target_id == guard.id)
    assert(damage[2].source_id == state.player_id and damage[2].target_id == spitter.id)
    assert(damage[2].chain_id == damage[1].chain_id)
end)

test("Crescent Reach composes geometry and power without mutating base ability", function()
    local state = Game.new(64)
    Game.player(state).position.x, Game.player(state).position.y = 5, 5
    grant(state, "crescent_reach", "common", 1)
    grant(state, "crescent_reach", "rare", 2)
    local cells, damage = Game.effective_ability(state, "sweeping_slash", 1, -1)
    assert(#cells == 4 and cells[4].x == 7 and cells[4].y == 3 and damage == 10)
    assert(#Targeting.cells("sweeping_slash", 5, 5, 1, -1) == 3)
    local restored = Game.restore(Game.snapshot(state))
    local again, power = Game.effective_ability(restored, "sweeping_slash", 1, -1)
    equal(cells, again); assert(power == damage)
    grant(restored, "crescent_reach", "rare", 0)
    local changed, reduced = Game.effective_ability(restored, "sweeping_slash", 1, -1)
    assert(#changed == 4 and reduced == 4)
    grant(restored, "crescent_reach", "common", 0)
    assert(#Game.effective_ability(restored, "sweeping_slash", 1, -1) == 3)

    local blocked = Game.new(66)
    Game.player(blocked).position.x, Game.player(blocked).position.y = 4, 3
    grant(blocked, "crescent_reach", "common")
    local behind_wall = Game.effective_ability(blocked, "sweeping_slash", 1, -1)
    assert(#behind_wall == 3) -- (5,2) is open, but the reach cell (6,1) is wall
end)

test("two compatible declared ability modifiers compose in stable registry order", function()
    local id = "core:boon/test_edge"
    local definition = {
        id=id, name="Test Edge", description="Test-only compatible power modifier",
        tags={"transformation"},
        triggers={}, scope={kind="owner"}, stack_policy="add_power", effects={},
        parameters={common={chance=0,power=2}, uncommon={chance=0,power=3},
            rare={chance=0,power=4}, legendary={chance=0,power=5}},
        modifiers={{ability_id="core:ability/sweeping_slash", kind="add_damage"}},
        incompatible={},
    }
    BoonContent.definitions[id] = definition
    BoonContent.order[#BoonContent.order + 1] = id
    local ok, result = pcall(function()
        local state = Game.new(67)
        grant(state, "crescent_reach", "common")
        Game.set_boon_stacks(state, state.player_id, id, "common", 1)
        local cells, damage = Game.effective_ability(state, "sweeping_slash", 1, 0)
        assert(#cells == 4 and damage == 6)
    end)
    BoonContent.definitions[id] = nil
    table.remove(BoonContent.order)
    assert(ok, result)
end)

test("declared boon incompatibilities reject a conflicting loadout", function()
    local state = Game.new(65)
    local reach = BoonContent.get("core:boon/crescent_reach")
    local original = reach.incompatible
    reach.incompatible = {"core:boon/storm_conductor"}
    grant(state, "storm_conductor", "common")
    local ok = pcall(grant, state, "crescent_reach", "common")
    reach.incompatible = original
    assert(not ok and Game.player(state).boons["core:boon/crescent_reach"] == nil)
end)

test("Temporal Echo resolves at 200 before player readiness and restores identically", function()
    local state = Game.new(71)
    grant(state, "temporal_echo", "common")
    local target = quiet_enemy(state, "core:enemy/thornspitter", 3, 2)
    local _, _, first = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(state.clock == 100 and target.hp == 2)
    assert(#events_of(first, "DelayedEffectScheduled") == 1)
    local schedule = events_of(first, "DelayedEffectScheduled")[1]
    assert(schedule.due == 200 and #schedule.target_cells == 1)
    local restored = Game.restore(Game.snapshot(state))
    local _, _, a = Game.submit(state, {kind="wait"})
    local _, _, b = Game.submit(restored, {kind="wait"})
    equal(a,b); equal(Game.snapshot(state),Game.snapshot(restored))
    assert(state.clock == 200 and target.hp == 1)
    local delayed = events_of(a, "DelayedEffectResolved")[1]
    assert(delayed and delayed.time == 200 and delayed.chain_id == schedule.chain_id)
    assert(#delayed.ancestry == 1 and delayed.ancestry[1] ==
        "1:core:boon/temporal_echo")
    assert(events_of(a, "DamageTaken")[1].source_id == state.player_id)
end)

test("locked delayed area hits its current occupant and never advances during idle presentation", function()
    local state = Game.new(72)
    grant(state, "temporal_echo", "rare")
    local original = quiet_enemy(state, "core:enemy/mossbound_guard", 3, 2)
    local replacement = quiet_enemy(state, "core:enemy/ruin_skitter", 4, 2)
    Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(state.clock == 100 and #Game.pending_boon_effects(state) == 1)
    local before = Game.snapshot(state)
    for _ = 1, 1000 do Game.pending_boon_effects(state); Game.player(state) end
    equal(before, Game.snapshot(state))
    original.position.x, replacement.position.x = 4, 3
    local old_hp, new_hp = original.hp, replacement.hp
    local _, _, events = Game.submit(state, {kind="wait"})
    assert(state.clock == 200 and original.hp == old_hp and replacement.hp < new_hp)
    assert(events_of(events, "DamageTaken")[1].target_id == replacement.id)
end)

test("boon seed replay and plain-data pending state preserve ordered continuation", function()
    local function run()
        local state = Game.new(83)
        Game.player(state).position.x, Game.player(state).position.y = 5, 5
        grant(state, "storm_conductor", "rare", 4)
        grant(state, "temporal_echo", "uncommon", 1)
        quiet_enemy(state, "core:enemy/mossbound_guard", 6, 5)
        quiet_enemy(state, "core:enemy/ruin_skitter", 7, 5)
        local events = {}
        for _, intent in ipairs({{kind="basic_attack",dx=1,dy=0},
            {kind="wait"}, {kind="wait"}}) do
            local _, _, batch = Game.submit(state, intent)
            for _, event in ipairs(batch) do events[#events + 1] = event end
        end
        local function plain(value)
            assert(type(value) ~= "function" and type(value) ~= "userdata"
                and type(value) ~= "thread")
            if type(value) == "table" then
                for key, child in pairs(value) do plain(key); plain(child) end
            end
        end
        plain(Game.snapshot(state))
        return Game.snapshot(state), events
    end
    local a, ae = run()
    local b, be = run()
    equal(a,b); equal(ae,be)
end)

test("multi-stage Sword lightning explosion chain retains sibling branches and stops recursion", function()
    local state = Game.new(73)
    Game.player(state).position.x, Game.player(state).position.y = 5, 5
    grant(state, "storm_conductor", "legendary", 2)
    grant(state, "detonation_bloom", "rare", 1)
    local first = quiet_enemy(state, "core:enemy/mossbound_guard", 6, 5)
    local second = quiet_enemy(state, "core:enemy/thornspitter", 7, 5)
    local third = quiet_enemy(state, "core:enemy/ruin_skitter", 8, 5)
    third.hp = 1
    local _, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(Game.entity(state, first.id) and Game.entity(state, first.id).hp == 5
        and Game.entity(state, second.id) == nil
        and Game.entity(state, third.id) == nil)
    local activations = events_of(events, "BoonActivated")
    assert(#activations == 2 and activations[1].boon_id == "core:boon/storm_conductor"
        and activations[2].boon_id == "core:boon/detonation_bloom")
    assert(#events_of(events, "EntityKilled") == 2)
    assert(#events_of(events, "EffectApplied") == 2)
    assert(#events_of(events, "ExperienceGained") == 2
        and #events_of(events, "CurrencyDropped") == 2)
    assert(state.progression.xp_total == 7 and #state.drops == 2)
    local deaths = events_of(events, "Died")
    assert(deaths[1].target_id == second.id and deaths[1].damage_type == "lightning")
    assert(deaths[2].target_id == third.id and deaths[2].damage_type == "explosion")
    local original_chain = events[1].chain_id
    for _, event in ipairs(events) do assert(event.chain_id == original_chain) end
    assert(#events < 100)
end)

test("dozens of enemies and high proc activity finish without recursive overflow", function()
    local state = Game.new(79, "core:map/scrolling_room")
    Game.player(state).position.x, Game.player(state).position.y = 17, 16
    grant(state, "storm_conductor", "legendary", 4)
    grant(state, "detonation_bloom", "legendary", 2)
    grant(state, "resonant_wounds", "rare", 2)
    local count = 0
    for y = 14, 19 do
        for x = 18, 23 do
            if Content.walkable(Content.get_map(state.map_id), x, y) then
                quiet_enemy(state, "core:enemy/ruin_skitter", x, y)
                count = count + 1
            end
        end
    end
    assert(count >= 30)
    local _, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(#events_of(events, "BoonActivated") >= 4)
    assert(#events_of(events, "DamageTaken") >= 5)
    assert(#events < 10000)
end)

test("boon-free kill emits canonical contact, death, XP and one drop", function()
    local state = Game.new(101)
    local enemy = quiet_enemy(state, "core:enemy/ruin_skitter", 3, 2)
    local _, _, events = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(#events_of(events,"HitConfirmed") == 1)
    assert(#events_of(events,"EntityKilled") == 1)
    assert(#events_of(events,"ExperienceGained") == 1)
    assert(#events_of(events,"CurrencyDropped") == 1)
    assert(state.progression.xp_total == 3 and #state.drops == 1)
    assert(state.drops[1].killed_entity_id == enemy.id and state.drops[1].amount == 2)
    local killed = events_of(events,"EntityKilled")[1]
    assert(killed.source_id == state.player_id and killed.target_id == enemy.id)
    assert(killed.x == 3 and killed.y == 2 and killed.root_action_id == events[1].action_id)
    local _, _, miss = Game.submit(state, {kind="basic_attack", dx=1, dy=0})
    assert(#events_of(miss,"AttackPerformed") == 1 and #events_of(miss,"HitConfirmed") == 0)
    assert(state.progression.xp_total == 3 and #state.drops == 1)
end)

test("Sweep multi-kills finish HP batch before reward events and award once each", function()
    local state = Game.new(102)
    local a = quiet_enemy(state,"core:enemy/ruin_skitter",2,3)
    local b = quiet_enemy(state,"core:enemy/ruin_skitter",3,3)
    a.hp, b.hp = 2, 2
    local _, _, events = Game.submit(state,{kind="sweeping_slash",dx=0,dy=1})
    assert(#events_of(events,"DamageBatchResolved") == 1)
    assert(#events_of(events,"EntityKilled") == 2)
    assert(#events_of(events,"ExperienceGained") == 2)
    assert(#events_of(events,"CurrencyDropped") == 2)
    assert(state.progression.xp_total == 6 and #state.drops == 2)
    local batch = events_of(events,"DamageBatchResolved")[1]
    assert(batch.outcomes[1].hp_after == 0 and batch.outcomes[2].hp_after == 0)
    assert(batch.id < events_of(events,"ExperienceGained")[1].id)
    local kills = events_of(events,"EntityKilled")
    assert(kills[1].target_id ~= kills[2].target_id)
    assert(state.rewarded_kills[a.id] and state.rewarded_kills[b.id])
end)

test("player-owned immediate and delayed boon kills receive XP; enemy friendly fire does not", function()
    local lightning = Game.new(103)
    grant(lightning,"storm_conductor","legendary",2)
    local first = quiet_enemy(lightning,"core:enemy/mossbound_guard",3,2)
    local second = quiet_enemy(lightning,"core:enemy/ruin_skitter",4,2)
    first.hp, second.hp = 10, 1
    local _, _, events = Game.submit(lightning,{kind="basic_attack",dx=1,dy=0})
    assert(#events_of(events,"EntityKilled") == 1)
    assert(events_of(events,"EntityKilled")[1].damage_type == "lightning")
    assert(lightning.progression.xp_total == 3 and #lightning.drops == 1)

    local delayed = Game.new(104)
    grant(delayed,"temporal_echo","common")
    local echo_target = quiet_enemy(delayed,"core:enemy/ruin_skitter",3,2)
    echo_target.hp = 5
    Game.submit(delayed,{kind="basic_attack",dx=1,dy=0})
    assert(delayed.progression.xp_total == 0 and echo_target.hp == 1)
    local _, _, due_events = Game.submit(delayed,{kind="wait"})
    assert(#events_of(due_events,"EntityKilled") == 1)
    assert(events_of(due_events,"EntityKilled")[1].time == 200)
    assert(delayed.progression.xp_total == 3)

    local friendly = Game.new(105)
    local shooter = Game.spawn_enemy(friendly,"core:enemy/thornspitter",2,5)
    local ally = quiet_enemy(friendly,"core:enemy/ruin_skitter",2,4)
    ally.hp = 1
    local _, _, ff_events = Game.submit(friendly,{kind="wait"})
    local killed = events_of(ff_events,"EntityKilled")
    assert(#killed == 1 and killed[1].source_id == shooter.id
        and killed[1].target_id == ally.id)
    assert(friendly.progression.xp_total == 0 and #friendly.drops == 1)
end)

test("automatic level overflow uses seeded stat picks and no full heal", function()
    local state = Game.new(106)
    local mirror = Game.restore(Game.snapshot(state))
    Game.player(state).hp, Game.player(mirror).hp = 10, 10
    local function level(run)
        local events = {}
        Rewards.gain_xp(run,35,{target_id=99,target_definition_id="test",
            floor_id=run.active_floor_id,x=3,y=2},function(kind,fields)
            events[#events+1] = {kind=kind,fields=fields}
        end)
        return events
    end
    local a, b = level(state), level(mirror)
    equal(a,b)
    assert(state.progression.level == 4 and state.progression.xp_total == 35)
    assert(Game.next_level_xp(state) == 50)
    assert(#a == 7 and a[1].kind == "ExperienceGained")
    assert(Game.player(state).hp < Game.player(state).max_hp)
    equal(Game.snapshot(state),Game.snapshot(mirror))
end)

test("all six growth stats affect real combat values and speed stays positive", function()
    local rules = Content.get_progression()
    local original = {}
    for stat, value in pairs(rules.stat_weights) do original[stat] = value end
    local ok, err = pcall(function()
        for _, selected in ipairs(rules.stat_order) do
            local state = Game.new(107)
            local actor = Game.player(state)
            actor.hp = 10
            for stat in pairs(rules.stat_weights) do
                rules.stat_weights[stat] = stat == selected and 1000000 or 1
            end
            local previous = actor[selected] or actor[selected .. "_ppm"]
            local records = {}
            Rewards.gain_xp(state,5,{target_id=9,target_definition_id="test",
                floor_id=state.active_floor_id,x=3,y=2},function(kind,fields)
                records[#records+1]={kind=kind,fields=fields}
            end)
            assert(records[3].kind == "StatIncreased" and records[3].fields.stat == selected)
            assert(Content.validate_all())
            if selected == "max_health" then
                assert(actor.max_hp == 28 and actor.hp == 14)
                assert(records[3].fields.old_hp == 10 and records[3].fields.new_hp == 14)
            elseif selected == "damage" then
                assert(actor.damage == 5 and actor.slash_damage == 4)
                local target = quiet_enemy(state,"core:enemy/ruin_skitter",3,2)
                target.hp = 6
                Game.submit(state,{kind="basic_attack",dx=1,dy=0})
                assert(target.hp == 1)
            elseif selected == "defense" then
                assert(actor.defense == 1)
                local attacker = Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2)
                assert(attacker.damage == 3)
                Game.submit(state,{kind="wait"})
                Game.submit(state,{kind="wait"})
                assert(actor.hp == 8)
            elseif selected == "speed" then
                assert(actor.speed == 110 and Game.effective_cost(100,actor.speed) == 91)
                local _, _, wait = Game.submit(state,{kind="wait"})
                assert(wait[1].kind == "Waited" and state.clock == 91)
            elseif selected == "crit_chance" then
                assert(actor.crit_chance_ppm == 50000)
            elseif selected == "crit_damage" then
                assert(actor.crit_damage_percent == 175)
            end
        end
    end)
    for stat, value in pairs(original) do rules.stat_weights[stat] = value end
    assert(ok,err)
    assert(Game.effective_cost(100,4503599627370496) == 1)
end)

test("speed gained from a kill affects only future recovery", function()
    local rules = Content.get_progression()
    local original = {}
    for stat, value in pairs(rules.stat_weights) do
        original[stat] = value
        rules.stat_weights[stat] = stat == "speed" and 1000000 or 1
    end
    local ok, err = pcall(function()
        local state = Game.new(117)
        local guard = quiet_enemy(state,"core:enemy/mossbound_guard",3,2)
        guard.hp = 1
        local _, _, killed = Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(events_of(killed,"StatIncreased")[1].stat == "speed")
        assert(Game.player(state).speed == 110)
        assert(state.last_action.cost == 100 and state.clock == 100)
        Game.submit(state,{kind="wait"})
        assert(state.last_action.cost == 91 and state.clock == 191)
    end)
    for stat, value in pairs(original) do rules.stat_weights[stat] = value end
    assert(ok,err)
end)

test("direct critical strikes use seeded rolls and do not crit secondary effects", function()
    local state = Game.new(108)
    local actor = Game.player(state)
    actor.crit_chance_ppm, actor.crit_damage_percent = 1000000, 200
    local target = quiet_enemy(state,"core:enemy/mossbound_guard",3,2)
    target.hp, target.max_hp = 20, 20
    local mirror = Game.restore(Game.snapshot(state))
    local _, _, a = Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    local _, _, b = Game.submit(mirror,{kind="basic_attack",dx=1,dy=0})
    equal(a,b)
    assert(a[1].critical and a[1].raw_damage == 8)
    assert(Game.entity(state,target.id).hp == 13)
end)

test("currency drops are nonblocking, auto-collect once, and round-trip", function()
    local state = Game.new(109)
    quiet_enemy(state,"core:enemy/ruin_skitter",3,2)
    Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    local before = Game.snapshot(state)
    local clone = Game.restore(before)
    local _, _, a = Game.submit(state,{kind="move",dx=1,dy=0})
    local _, _, b = Game.submit(clone,{kind="move",dx=1,dy=0})
    equal(a,b)
    assert(state.clock == 200 and state.currency == 2 and state.drops[1].collected)
    assert(#events_of(a,"TileEntered") == 1 and #events_of(a,"CurrencyCollected") == 1)
    Game.submit(state,{kind="move",dx=-1,dy=0})
    local _, _, repeat_move = Game.submit(state,{kind="move",dx=1,dy=0})
    assert(state.currency == 2 and #events_of(repeat_move,"CurrencyCollected") == 0)
end)

test("Dash picks up currency on each traversed cell without an extra action", function()
    local state = Game.new(113)
    state.drops = {
        {id=1,kind="currency",amount=2,position={floor_id=state.active_floor_id,x=3,y=2},
            collected=false,killed_entity_id=11},
        {id=2,kind="currency",amount=3,position={floor_id=state.active_floor_id,x=4,y=2},
            collected=false,killed_entity_id=12},
    }
    state.next_drop_id = 3
    local _, _, events = Game.submit(state,{kind="dash",dx=1,dy=0})
    assert(state.clock == 130 and state.currency == 5 and state.last_action.cost == 130)
    assert(#events_of(events,"TileEntered") == 2)
    assert(#events_of(events,"CurrencyCollected") == 2)
    assert(state.drops[1].collected and state.drops[2].collected)
end)

test("chest caches three valid distinct offers; cancel, affordability, claim and traversal", function()
    local state = Game.new(110)
    local chest = Game.spawn_chest(state,"core:chest/standard",3,2)
    local initial = Game.snapshot(state)
    local options = assert(Game.chest_options(state,chest.id))
    assert(#options.offers == 3 and options.price == 5 and options.shortfall == 5)
    local names = {}
    for _, offer in ipairs(options.offers) do
        assert(BoonContent.get(offer.boon_id) and not names[offer.boon_id])
        names[offer.boon_id] = true
    end
    equal(initial,Game.snapshot(state)) -- opening/cancelling is presentation-only
    local reopened = Game.chest_options(state,chest.id)
    equal(options,reopened)
    assert(not Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=1}))
    equal(initial,Game.snapshot(state))
    state.currency = 5
    local _, reason = Game.submit(state,{kind="move",dx=1,dy=0})
    assert(reason == "chest" and state.clock == 0)
    local chosen = options.offers[1]
    local committed, _, events = Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=1})
    assert(committed and state.clock == 100 and state.currency == 0 and chest.claimed)
    assert(not chest.blocks_movement and #events_of(events,"ChestClaimed") == 1)
    assert(Game.player(state).boons[chosen.boon_id][chosen.rarity] == 1)
    assert(not Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=1}))
    assert(Game.submit(state,{kind="move",dx=1,dy=0}))
    assert(state.currency == 0)
end)

test("elite chest uses free price and favorable validated rarity profile", function()
    local standard = Content.get_chest("core:chest/standard")
    local elite = Content.get_chest("core:chest/free_elite")
    assert(Content.validate_all())
    assert(standard.price == 5 and elite.price == 0)
    assert(standard.rarity_weights.common == 70 and standard.rarity_weights.uncommon == 25
        and standard.rarity_weights.rare == 4 and standard.rarity_weights.legendary == 1)
    assert(elite.rarity_weights.common == 20 and elite.rarity_weights.uncommon == 45
        and elite.rarity_weights.rare == 28 and elite.rarity_weights.legendary == 7)
    local state = Game.new(111)
    local chest = Game.spawn_chest(state,"core:chest/free_elite",3,2)
    assert(Game.chest_options(state,chest.id).shortfall == 0)
    assert(Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=2}))
    assert(state.currency == 0 and chest.claimed and state.clock == 100)
end)

test("separate chests can add mixed-rarity stacks of the same boon", function()
    local state = Game.new(114)
    local first = Game.spawn_chest(state,"core:chest/free_elite",3,2)
    local second = Game.spawn_chest(state,"core:chest/free_elite",2,3)
    first.offers[1] = {boon_id="core:boon/storm_conductor",rarity="common"}
    second.offers[1] = {boon_id="core:boon/storm_conductor",rarity="legendary"}
    -- Maintain each chest's three distinct valid offers in this authored test setup.
    for _, chest in ipairs({first,second}) do
        local used = {[chest.offers[1].boon_id]=true}
        for index = 2, 3 do
            if used[chest.offers[index].boon_id] then
                for _, id in ipairs(BoonContent.order) do
                    if not used[id] then
                        chest.offers[index].boon_id = id
                        break
                    end
                end
            end
            used[chest.offers[index].boon_id] = true
        end
        assert(Rewards.validate_chest(chest))
    end
    assert(Game.submit(state,{kind="claim_chest",chest_id=first.id,offer_index=1}))
    assert(Game.submit(state,{kind="claim_chest",chest_id=second.id,offer_index=1}))
    local stacks = Game.player(state).boons["core:boon/storm_conductor"]
    assert(stacks.common == 1 and stacks.legendary == 1)
    equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
end)

test("committed enemy impact precedes newly ready chest purchaser at same time", function()
    local state = Game.new(115)
    local chest = Game.spawn_chest(state,"core:chest/standard",3,2)
    state.currency = 5
    local shooter = Game.spawn_enemy(state,"core:enemy/thornspitter",2,5)
    local hp = Game.player(state).hp
    local _, _, events = Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=1})
    assert(state.clock == 100 and state.last_action.cost == 100)
    local started, impact, damage
    for index, event in ipairs(events) do
        if event.kind == "WindupStarted" and event.source_id == shooter.id then started=index end
        if event.kind == "AttackPerformed" and event.source_id == shooter.id then impact=index end
        if event.kind == "DamageTaken" and event.source_id == shooter.id then damage=index end
    end
    assert(started and impact and damage and started < impact and impact < damage)
    assert(events[impact].time == 100 and Game.player(state).hp == hp - shooter.damage)
end)

test("pending delayed credited kill retains XP and drop continuation after restore", function()
    local state = Game.new(116)
    grant(state,"temporal_echo","common")
    local victim = quiet_enemy(state,"core:enemy/ruin_skitter",3,2)
    victim.hp = 5
    Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    local copy_state = Game.restore(Game.snapshot(state))
    local _, _, a = Game.submit(state,{kind="wait"})
    local _, _, b = Game.submit(copy_state,{kind="wait"})
    equal(a,b)
    assert(#events_of(a,"ExperienceGained") == 1
        and #events_of(a,"CurrencyDropped") == 1)
    equal(Game.snapshot(state),Game.snapshot(copy_state))
end)

test("chest offers and critical future remain identical after snapshot restore", function()
    local state = Game.new(112)
    local chest = Game.spawn_chest(state,"core:chest/standard",3,2)
    state.currency = 10
    Game.player(state).crit_chance_ppm = 250000
    local copy_state = Game.restore(Game.snapshot(state))
    equal(Game.chest_options(state,chest.id),Game.chest_options(copy_state,chest.id))
    local _, _, a = Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=3})
    local _, _, b = Game.submit(copy_state,{kind="claim_chest",chest_id=chest.id,offer_index=3})
    equal(a,b)
    local target_a = quiet_enemy(state,"core:enemy/mossbound_guard",2,3)
    quiet_enemy(copy_state,"core:enemy/mossbound_guard",2,3)
    local _, _, attack_a = Game.submit(state,{kind="basic_attack",dx=0,dy=1})
    local _, _, attack_b = Game.submit(copy_state,{kind="basic_attack",dx=0,dy=1})
    equal(attack_a,attack_b)
    assert(Game.entity(state,target_a.id))
    equal(Game.snapshot(state),Game.snapshot(copy_state))
end)

test("ordinary kill, pickup, level, purchase and boon transform form a complete loop", function()
    local state = Game.new(1)
    local chest = Game.spawn_chest(state,"core:chest/standard",5,2)
    assert(chest.offers[1].boon_id == "core:boon/crescent_reach")
    local guard = quiet_enemy(state,"core:enemy/mossbound_guard",3,2)
    guard.hp = 1
    local _, _, kill = Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    assert(#events_of(kill,"EntityKilled") == 1 and state.progression.level == 2)
    assert(#events_of(kill,"StatIncreased") == 1)
    assert(Game.submit(state,{kind="move",dx=1,dy=0}))
    assert(state.currency == 4)
    local skitter = quiet_enemy(state,"core:enemy/ruin_skitter",4,2)
    skitter.hp = 1
    Game.submit(state,{kind="basic_attack",dx=1,dy=0})
    Game.submit(state,{kind="move",dx=1,dy=0})
    assert(state.currency == 6)
    local before = Game.snapshot(state)
    local _, reason = Game.submit(state,{kind="move",dx=1,dy=0})
    assert(reason == "chest")
    equal(before,Game.snapshot(state))
    local _, _, claim = Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=1})
    assert(state.currency == 1 and #events_of(claim,"BoonGranted") == 1)
    local cells = Game.effective_ability(state,"sweeping_slash",1,0)
    assert(#cells == 4 and cells[4].x == 6 and cells[4].y == 2)
    local foe = quiet_enemy(state,"core:enemy/ruin_skitter",6,2)
    foe.hp = 1
    local _, _, slash = Game.submit(state,{kind="sweeping_slash",dx=1,dy=0})
    assert(#events_of(slash,"EntityKilled") == 1 and Game.entity(state,foe.id) == nil)
end)

print(("%d passed, %d failed"):format(passed, failed))
if failed > 0 then os.exit(1) end
