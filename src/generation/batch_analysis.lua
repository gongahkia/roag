-- Headless aggregation over isolated inspector floor construction.
local InspectionFloor = require("src.generation.inspection_floor")
local Analysis = require("src.generation.analysis")

local Batch = {}

local function update_range(range, value, seed)
  if range.min == nil or value < range.min then range.min, range.min_seed = value, seed end
  if range.max == nil or value > range.max then range.max, range.max_seed = value, seed end
  range.total = range.total + value
end

local function average(range, count)
  return count > 0 and range.total / count or 0
end

local function merge_counts(destination, source)
  for key, value in pairs(source or {}) do destination[key] = (destination[key] or 0) + value end
end

function Batch.run(options)
  options = options or {}
  local stage = InspectionFloor.resolve_stage(options.stage or 1)
  local seed, count = tonumber(options.seed), tonumber(options.count)
  if not stage then return nil, { code = "invalid_stage", reason = "Unknown stage" } end
  if not seed or seed % 1 ~= 0 then return nil, { code = "invalid_seed", reason = "Seed must be an integer" } end
  if not count or count <= 0 or count % 1 ~= 0 then return nil, { code = "invalid_count", reason = "Count must be a positive integer" } end
  local make_floor, analyze = options.generate or InspectionFloor.generate, options.analyze or Analysis.analyze
  -- Large batches need aggregate diagnostics, not hundreds of full
  -- cell-by-cell reports retained in memory. Small runs keep details for
  -- programmatic comparison; callers may override this explicitly.
  local retain_reports = options.include_reports
  if retain_reports == nil then retain_reports = count <= 25 end
  local reports, report_count, failures = {}, 0, {}
  local ranges = {
    passable_cells = { total = 0 }, enemies = { total = 0 }, hazards = { total = 0 },
    liquid_volume = { total = 0 }, gas_volume = { total = 0 }, circuits = { total = 0 }, powered_circuits = { total = 0 },
    room_count = { total = 0 },
  }
  local material_counts, object_counts, template_usage, rotation_counts, connector_patterns, graph_degrees = {}, {}, {}, {}, {}, {}
  for offset = 0, count - 1 do
    local current_seed = seed + offset
    local floor, failure = make_floor({ stage = stage, seed = current_seed })
    if not floor then
      failures[#failures + 1] = { seed = current_seed, code = failure.code, message = failure.reason }
    else
      local report = analyze(floor.world, {
        seed = current_seed, stage = stage, terrain = floor.terrain, state = floor.state, session = floor.session, provenance = floor.provenance,
      })
      report_count = report_count + 1
      if retain_reports then reports[#reports + 1] = report end
      if not report.valid then failures[#failures + 1] = { seed = current_seed, errors = report.errors } end
      local metrics = report.metrics
      for name, range in pairs(ranges) do update_range(range, metrics[name], current_seed) end
      merge_counts(material_counts, metrics.material_counts)
      merge_counts(object_counts, metrics.object_types)
      merge_counts(template_usage, metrics.template_usage)
      merge_counts(rotation_counts, metrics.rotation_counts)
      merge_counts(connector_patterns, metrics.connector_pattern_counts)
      merge_counts(graph_degrees, metrics.graph_degree_counts)
    end
  end
  local statistics = {}
  for name, range in pairs(ranges) do
    statistics[name] = { min = range.min or 0, min_seed = range.min_seed, max = range.max or 0, max_seed = range.max_seed,
      average = average(range, report_count) }
  end
  local outliers = {
    smallest_passable_area = { seed = statistics.passable_cells.min_seed, value = statistics.passable_cells.min },
    largest_passable_area = { seed = statistics.passable_cells.max_seed, value = statistics.passable_cells.max },
    most_enemies = { seed = statistics.enemies.max_seed, value = statistics.enemies.max },
    most_hazards = { seed = statistics.hazards.max_seed, value = statistics.hazards.max },
    most_liquid = { seed = statistics.liquid_volume.max_seed, value = statistics.liquid_volume.max },
    most_gas = { seed = statistics.gas_volume.max_seed, value = statistics.gas_volume.max },
  }
  return {
    format = "roag.generation_report",
    version = 1,
    options = { stage = stage, terrain = (InspectionFloor.stages()[stage] or {}).terrain, seed = seed, count = count },
    summary = { generated = report_count, failures = #failures, statistics = statistics, outliers = outliers,
      material_counts = material_counts, object_counts = object_counts, template_usage = template_usage,
      rotation_counts = rotation_counts, connector_pattern_counts = connector_patterns, graph_degree_counts = graph_degrees },
    failures = failures,
    reports = reports,
    reports_included = retain_reports,
  }
end

return Batch
