-- Deterministic structural batch for physical boss arenas. It operates on
-- synthetic sessions only; no active run, meta profile, or fallen archive is
-- read or written.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Content = require("src.content.legacy")
local Registry = require("src.content.registry")
local Session = require("src.simulation.session")

local count = tonumber((arg or {})[1]) or 100
assert(count > 0 and count % 1 == 0, "Usage: luajit tools/analyze_boss_arenas.lua [positive count per boss]")
local registry = Registry.load()
local boss_ids = {}
for id in pairs(registry.bosses) do boss_ids[#boss_ids + 1] = id end
table.sort(boss_ids)

local failures, totals = {}, {}
for boss_index, boss_id in ipairs(boss_ids) do
  totals[boss_id] = { generated = 0, liquid = 0, hazards = 0, cover = 0, exits = 0 }
  for offset = 1, count do
    local session = Session.new({ seed = 970000 + boss_index * 10000 + offset })
    session:start_run(Content.classes[1], Content.boons[1])
    local route = session.state.route
    for _, id in ipairs(route.node_order) do
      if route:node(id).boss_id == boss_id then route.current_node_id = id; route.path = { id }; break end
    end
    local ok, error_data = pcall(function()
      session:start_boss()
      local state, world = session.state, session.state.world
      assert(state.boss and state.boss.boss_id == boss_id)
      assert(world:is_passable(state.player.x, state.player.y) and world:is_passable(state.boss.x, state.boss.y))
      assert(state.player.x ~= state.boss.x or state.player.y ~= state.boss.y)
      session:validate_world()
      session:validate_physical_ownership()
      totals[boss_id].liquid = totals[boss_id].liquid + #world:list_liquids()
      totals[boss_id].hazards = totals[boss_id].hazards + #world:list_hazards()
      totals[boss_id].cover = totals[boss_id].cover + #world:list_objects()
      if boss_id ~= "boss.legacy.final" then
        state.boss.health = 1
        session:_damage_boss(1)
        assert(state.exit and world:is_passable(state.exit.x, state.exit.y))
        local reaches_exit = false
        for _, point in ipairs(session:_reachable_floor_cells()) do
          if point.x == state.exit.x and point.y == state.exit.y then reaches_exit = true; break end
        end
        assert(reaches_exit)
        totals[boss_id].exits = totals[boss_id].exits + 1
      end
    end)
    if ok then totals[boss_id].generated = totals[boss_id].generated + 1
    else failures[#failures + 1] = { boss_id = boss_id, seed = 970000 + boss_index * 10000 + offset, reason = tostring(error_data) } end
  end
end

for _, boss_id in ipairs(boss_ids) do
  local total = totals[boss_id]
  io.write(string.format("%s: %d/%d valid  avg cover %.2f  avg hazards %.2f  avg liquid %.2f  exits %d\n",
    boss_id, total.generated, count, total.cover / count, total.hazards / count, total.liquid / count, total.exits))
end
io.write(string.format("Boss arena batch: %d constructions, %d structural failures\n", #boss_ids * count, #failures))
for _, failure in ipairs(failures) do
  io.write(string.format("  %s seed %d: %s\n", failure.boss_id, failure.seed, failure.reason))
end
if #failures > 0 then os.exit(1) end
