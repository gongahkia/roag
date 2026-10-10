package.path = "./?.lua;./?/init.lua;" .. package.path
assert(_G.love == nil, "headless tests must not initialize LÖVE")

local Content = require("src.content")
local Game = require("src.game")
local RNG = require("src.rng")
local Scheduler = require("src.scheduler")
local Camera = require("src.camera")
local AI = require("src.ai")
local Playback = require("src.playback")

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
        if event.time == 0 and event.source_id ~= state.player_id then
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

print(("%d passed, %d failed"):format(passed, failed))
if failed > 0 then os.exit(1) end
