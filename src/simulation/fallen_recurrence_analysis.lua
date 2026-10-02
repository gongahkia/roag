-- Read-only recurrence placement diagnostics.  It builds isolated sessions
-- from synthetic archived body data and never opens active/meta/archive
-- storage, making it safe for headless batch validation.
local Registry = require("src.content.registry")
local Session = require("src.simulation.session")
local Recurrence = require("src.simulation.fallen_recurrence")

local Analysis = {}

local function source_record(registry)
  local source = Session.new({ seed = 711001, registry = registry, run_id = "run:711001" })
  source:start_run()
  return {
    id = "fallen:711001",
    source_run_id = "run:711001",
    body = source.state.player.body:to_data(),
    metadata = { route_path = {}, charm_ids = {}, research_ids = {} },
  }
end

local function start_target_depth(session, depth, variant)
  -- Diagnostics need initial geometry at the requested depth, not a combat
  -- simulation of all earlier floors. Advance the already generated route
  -- graph directly, then invoke the exact normal route-node floor builder.
  local graph = require("src.routes.graph").new(session.seed, session.route_definitions,
    "route_profile.legacy.base", session.state.meta_snapshot.unlock_ids)
  session.state.route = graph
  assert(graph:complete_current().applied)
  local first_choices = graph:available()
  assert(graph:select(first_choices[(variant % #first_choices) + 1].id).applied)
  if depth == 3 then
    assert(graph:complete_current().applied)
    local boss_choices = graph:available()
    assert(#boss_choices == 1 and boss_choices[1].type == "boss")
    assert(graph:select(boss_choices[1].id).applied)
    assert(graph:complete_current().applied)
    local third_choices = graph:available()
    assert(graph:select(third_choices[(math.floor(variant / 2) % #third_choices) + 1].id).applied)
  end
  session:start_route_node(graph.current_node_id)
end

function Analysis.batch(options)
  options = options or {}
  local seed, count = tonumber(options.seed), tonumber(options.count)
  if not seed or seed % 1 ~= 0 then return nil, { code = "invalid_seed", reason = "Seed must be an integer" } end
  if not count or count < 1 or count % 1 ~= 0 then return nil, { code = "invalid_count", reason = "Count must be a positive integer" } end
  local registry, record = options.registry or Registry.load(), options.record
  record = record or source_record(registry)
  local report = {
    format = "roag.fallen_recurrence_report",
    version = 1,
    options = { seed = seed, count = count },
    summary = { generated = 0, corpse = 0, hostile = 0, placement_failures = 0, critical_failures = 0 },
    failures = {},
  }
  for offset = 0, count - 1 do
    local run_seed, run_id = seed + offset, string.format("run:%06d", 800000 + offset)
    local spec = assert(Recurrence.assign(run_id, run_seed, { record }))
    -- Exercise both recurrence forms and both supported later depths in a
    -- deterministic schedule instead of sampling extra random state.
    spec.mode = offset % 2 == 0 and "corpse" or "hostile"
    spec.target_depth = offset % 4 < 2 and 2 or 3
    local session = Session.new({ seed = run_seed, registry = registry, run_id = run_id, fallen_recurrence = spec })
    start_target_depth(session, spec.target_depth, offset)
    local saved = session.state.fallen_recurrence
    report.summary.generated = report.summary.generated + 1
    report.summary[saved.mode] = report.summary[saved.mode] + 1
    if not saved.spawned or saved.placement_failure then
      report.summary.placement_failures = report.summary.placement_failures + 1
      report.failures[#report.failures + 1] = { seed = run_seed, code = saved.placement_failure and saved.placement_failure.code or "not_spawned" }
    else
      local placement, path = saved.placement, session:_reachable_floor_cells()
      local reachable = false
      for _, point in ipairs(path) do if point.x == placement.x and point.y == placement.y then reachable = true end end
      local blocked = session.state.world:object_at(placement.x, placement.y) or session.state.world:is_hazardous(placement.x, placement.y)
      if not reachable or blocked then
        report.summary.critical_failures = report.summary.critical_failures + 1
        report.failures[#report.failures + 1] = { seed = run_seed, code = "illegal_or_unreachable_placement" }
      end
    end
  end
  return report
end

return Analysis
