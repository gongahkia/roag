local Analysis = require("src.generation.analysis")
local Batch = require("src.generation.batch_analysis")
local InspectionFloor = require("src.generation.inspection_floor")
local Inspector = require("src.tools.generation_inspector")
local Json = require("src.persistence.json")
local Registry = require("src.content.registry")
local SaveStore = require("src.persistence.save_store")
local World = require("src.world.world")
local Grid = require("src.world.grid")

local function layout(points)
  local result = {}
  for _, point in ipairs(points) do result[Grid.key(point[1], point[2])] = true end
  return result
end

local function fixture(points)
  local registry = Registry.load()
  local owner = { next_world_object_sequence = 1, next_hazard_sequence = 1, next_fire_sequence = 1 }
  return World.new(registry, "cave", layout(points), owner)
end

local function report_for(floor)
  return Analysis.analyze(floor.world, {
    seed = floor.seed, stage = floor.stage, terrain = floor.terrain,
    state = floor.state, session = floor.session, provenance = floor.provenance,
  })
end

return {
  {
    name = "generation inspector source and batch analyzer reproduce the same authoritative floor report",
    run = function()
      local floor = assert(InspectionFloor.generate({ stage = "cave", seed = 73101 }))
      local direct = report_for(floor)
      local batch = assert(Batch.run({ stage = "cave", seed = 73101, count = 1 }))
      local a, a_error = Json.encode(direct)
      local b, b_error = Json.encode(batch.reports[1])
      assert(a, a_error)
      assert(b, b_error)
      assert(a == b)
    end,
  },
  {
    name = "headless inspector controller regenerates the same source without LÖVE or save state",
    run = function()
      local inspector = Inspector.new({ stage = "cave", seed = 73102 })
      local direct = assert(InspectionFloor.generate({ stage = "cave", seed = 73102 }))
      local a, a_error = Json.encode(inspector.report)
      local b, b_error = Json.encode(report_for(direct))
      assert(a, a_error); assert(b, b_error); assert(a == b)
      inspector.selected = { x = 1, y = 1 }
      inspector:next_seed()
      assert(inspector.selected == nil and inspector.seed == 73103)
      inspector:toggle_layer("gas")
      assert(not inspector.layers.gas)
    end,
  },
  {
    name = "generation analysis distinguishes reachable and unreachable critical objectives",
    run = function()
      local reachable = fixture({ { 1, 1 }, { 2, 1 }, { 3, 1 } })
      local good = Analysis.analyze(reachable, { player = { x = 1, y = 1 }, targets = { { x = 3, y = 1 } }, seed = 1, stage = 1 })
      assert(good.valid and good.connectivity.components[1].size == 3)

      local blocked = fixture({ { 1, 1 }, { 3, 1 } })
      local bad = Analysis.analyze(blocked, { player = { x = 1, y = 1 }, targets = { { x = 3, y = 1 } }, seed = 1, stage = 1 })
      assert(not bad.valid and bad.errors[1].code == "unreachable_target")
    end,
  },
  {
    name = "generation report and overlay model expose authoritative layered metrics",
    run = function()
      local world = fixture({ { 1, 1 }, { 2, 1 }, { 3, 1 } })
      assert(world:add_liquid(1, 1, "liquid.water.legacy", 2).applied)
      assert(world:add_gas(2, 1, "gas.toxic.legacy", 3).applied)
      assert(world:place_hazard("hazard.legacy.spike_field", 3, 1))
      local report = Analysis.analyze(world, { player = { x = 1, y = 1 }, targets = { { x = 3, y = 1 } }, seed = 2, stage = 1 })
      assert(report.metrics.passable_cells == 3)
      assert(report.metrics.liquid_volume == 2 and report.metrics.gas_volume == 3)
      assert(report.metrics.hazards == 1 and report.metrics.harmful_gas_cells == 1)
      local overlay = Analysis.overlay_model(world, report)
      assert(#overlay.terrain == Grid.width * Grid.height and #overlay.liquids == 1 and #overlay.gases == 1)
      assert(#overlay.hazards == 1 and #overlay.conductivity == 1)
    end,
  },
  {
    name = "all current generated stages analyze without crashes and retain deterministic reports",
    run = function()
      for _, stage in ipairs(InspectionFloor.stages()) do
        local first = assert(InspectionFloor.generate({ stage = stage.index, seed = 73110 + stage.index }))
        local second = assert(InspectionFloor.generate({ stage = stage.index, seed = 73110 + stage.index }))
        local a, a_error = Json.encode(report_for(first))
        local b, b_error = Json.encode(report_for(second))
        assert(a, a_error); assert(b, b_error); assert(a == b)
      end
    end,
  },
  {
    name = "batch generation is reproducible and each seed is isolated from prior generation",
    run = function()
      local first = assert(Batch.run({ stage = "dungeon", seed = 73200, count = 4 }))
      local second = assert(Batch.run({ stage = "dungeon", seed = 73200, count = 4 }))
      local a, a_error = Json.encode(first)
      local b, b_error = Json.encode(second)
      assert(a, a_error); assert(b, b_error); assert(a == b)
      assert(first.summary.outliers.smallest_passable_area.seed == first.summary.statistics.passable_cells.min_seed)
      local alone = assert(InspectionFloor.generate({ stage = "dungeon", seed = 73203 }))
      local after_other = assert(InspectionFloor.generate({ stage = "forest", seed = 1 }))
      assert(after_other)
      local again = assert(InspectionFloor.generate({ stage = "dungeon", seed = 73203 }))
      local direct, direct_error = Json.encode(report_for(alone))
      local later, later_error = Json.encode(report_for(again))
      assert(direct, direct_error); assert(later, later_error); assert(direct == later)
    end,
  },
  {
    name = "batch diagnostics retain concrete failed seeds from an invalid fixture",
    run = function()
      local invalid = fixture({ { 1, 1 } })
      local result = assert(Batch.run({
        stage = "forest", seed = 73300, count = 2,
        generate = function(options)
          return { seed = options.seed, stage = options.stage, terrain = "cave", world = invalid,
            state = { player = { x = 9, y = 9 }, targets = {}, enemies = {} }, provenance = { streams = {} } }
        end,
      }))
      assert(#result.failures == 2 and result.failures[1].seed == 73300 and result.failures[2].seed == 73301)
      assert(result.failures[1].errors[1].code == "invalid_player_spawn")
    end,
  },
  {
    name = "generation diagnostics never touch injected active save storage",
    run = function()
      local store = SaveStore.memory()
      assert(store:write("keep-this-save"))
      assert(InspectionFloor.generate({ stage = "cave", seed = 73400 }))
      assert(Batch.run({ stage = "cave", seed = 73400, count = 2 }))
      local data = assert(store:read())
      assert(data == "keep-this-save")
    end,
  },
}
