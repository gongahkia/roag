-- Headless bounded lifecycle validation for finite physical reinforcement
-- sources. It deliberately constructs sources instead of simulating whole
-- floors: the point is legal visible arrival, finite state transitions, and
-- cancellation across every authored source profile.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local Reinforcements = require("src.simulation.reinforcements")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local function parse(arguments)
  local options = { seed = 98000, count = 300 }
  local index = 1
  while index <= #arguments do
    local value = arguments[index]
    if value == "--seed" then options.seed = assert(tonumber(arguments[index + 1]), "--seed requires an integer"); index = index + 2
    elseif value == "--count" then options.count = assert(tonumber(arguments[index + 1]), "--count requires a positive integer"); index = index + 2
    else error("Unknown argument " .. tostring(value)) end
  end
  assert(options.seed % 1 == 0 and options.count % 1 == 0 and options.count > 0, "seed/count must be integers and count must be positive")
  return options
end

local function open_session(seed)
  local session = Session.new({ seed = seed })
  session:start_run(Content.classes[1], Content.boons[1])
  local layout = {}
  for x = 0, Grid.width - 1 do for y = 0, Grid.height - 1 do layout[Grid.key(x, y)] = true end end
  local state = session.state
  state.world = World.new(session.registry, "forest", layout, state)
  state.enemies, state.targets, state.corpses = {}, {}, {}
  state.bullets, state.bombs, state.flares, state.area_attacks, state.effects = {}, {}, {}, {}, {}
  state.torches, state.ammo, state.exit, state.boss = {}, nil, nil, nil
  state.phase, state.ended = "combat", nil
  state.player.x, state.player.y = 10, 10
  return session
end

local function source_for(session, profile)
  local definition_id = profile.source_type == "lift" and "world_object.reinforcement.lift" or "world_object.reinforcement.nest"
  local wave = {}
  for index = 1, profile.wave_size do wave[index] = profile.entries[1].enemy_id end
  return assert(session.state.world:place_object(definition_id, 40, 25, {
    reinforcement_profile_id = profile.id,
    reinforcement_faction_id = profile.faction_id,
    reinforcement_charges = 1,
    reinforcement_state = "idle",
    reinforcement_just_armed = false,
    reinforcement_wave_enemy_ids = wave,
    reinforcement_provenance = "validation.reinforcement." .. profile.id,
  }))
end

local function run_case(seed, profile, mode)
  local session = open_session(seed)
  local actor = session:_make_enemy(profile.entries[1].enemy_id, { x = 11, y = 10 }, { scrap_award = true })
  session.state.enemies = { actor }
  local source = source_for(session, profile)
  if mode == "before" then
    assert(session:damage_world_object(source, { amount = source.current_integrity, cause = "kinetic", source = "validation" }).destroyed)
    assert(not session:_notify_combat(session.state.player, actor).applied)
    assert(source.reinforcement_state == "cancelled" and #session.state.enemies == 1)
  else
    assert(session:_notify_combat(session.state.player, actor).applied)
    assert(source.reinforcement_state == "armed" and source.reinforcement_delay == 2)
    if mode == "during" then
      assert(session:damage_world_object(source, { amount = source.current_integrity, cause = "kinetic", source = "validation" }).destroyed)
      Reinforcements.tick(session)
      assert(source.reinforcement_state == "cancelled" and #session.state.enemies == 1)
    else
      Reinforcements.tick(session)
      Reinforcements.tick(session)
      Reinforcements.tick(session)
      assert(source.reinforcement_state == "spent" and source.reinforcement_charges == 0)
      assert(#session.state.enemies == 1 + profile.wave_size)
      Reinforcements.tick(session)
      assert(#session.state.enemies == 1 + profile.wave_size, "finite source deployed more than once")
    end
  end
  assert(session.state.world:validate())
end

local options = parse(arg)
local profiles = require("content.reinforcements.legacy")
local modes = { "arrival", "before", "during" }
local profile_counts, failures = {}, {}
for index = 1, options.count do
  local profile = profiles[((index - 1) % #profiles) + 1]
  local mode = modes[((index - 1) % #modes) + 1]
  local ok, failure = xpcall(function() run_case(options.seed + index - 1, profile, mode) end, debug.traceback)
  profile_counts[profile.id] = (profile_counts[profile.id] or 0) + 1
  if not ok then failures[#failures + 1] = { index = index, profile = profile.id, mode = mode, error = failure } end
end

local ids = {}
for id in pairs(profile_counts) do ids[#ids + 1] = id end
table.sort(ids)
io.write(string.format("Reinforcement scenarios: %d  failures: %d\n", options.count, #failures))
for _, id in ipairs(ids) do io.write(string.format("  %s=%d\n", id, profile_counts[id])) end
for _, failure in ipairs(failures) do
  io.stderr:write(string.format("FAIL scenario %d %s (%s): %s\n", failure.index, failure.profile, failure.mode, failure.error))
end
if #failures > 0 then os.exit(1) end
