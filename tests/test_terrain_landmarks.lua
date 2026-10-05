local Analysis = require("src.generation.analysis")
local InspectionFloor = require("src.generation.inspection_floor")
local Json = require("src.persistence.json")

local function floor(biome, tier, seed)
  return assert(InspectionFloor.generate({
    biome = biome,
    tier = tier,
    seed = seed,
    discovery_state = { enabled = false, assigned_discovery_ids = {} },
    reinforcement_state = { enabled = false },
  }))
end

local function object_count(world, definition_id)
  local count = 0
  for _, object in ipairs(world:list_objects()) do
    if object.definition_id == definition_id then count = count + 1 end
  end
  return count
end

local function object(world, definition_id)
  for _, value in ipairs(world:list_objects()) do
    if value.definition_id == definition_id then return value end
  end
end

local function report_for(value)
  return Analysis.analyze(value.world, {
    seed = value.seed,
    biome_id = value.biome_id,
    tier_id = value.tier_id,
    terrain = value.terrain,
    state = value.state,
    session = value.session,
    provenance = value.provenance,
  })
end

return {
  {
    name = "forest generation builds deterministic connected clearings, streams, groves, ridges, and a stone hollow",
    run = function()
      local first = floor("biome.legacy.forest", "tier.legacy.1", 94001)
      local second = floor("biome.legacy.forest", "tier.legacy.1", 94001)
      local first_data, first_error = Json.encode(first.world:to_data())
      local second_data, second_error = Json.encode(second.world:to_data())
      assert(first_data, first_error); assert(second_data, second_error); assert(first_data == second_data)

      local report = report_for(first)
      assert(report.valid and report.metrics.connected_region_count == 1)
      assert(report.metrics.passable_cells >= 550 and report.metrics.passable_cells < 1500)
      assert((report.metrics.material_counts["material.terrain.forest_soil"] or 0) >= 9)
      assert((report.metrics.material_counts["material.terrain.granite"] or 0) > 0)
      assert(#(first.state.generation_metadata.clearings or {}) == 12)
      assert(report.metrics.landmarks == 5)
      assert(object_count(first.world, "world_object.feature.old_growth_tree") == 6)
      assert(object_count(first.world, "world_object.feature.granite_boulder") == 4)
      assert(object_count(first.world, "world_object.feature.fallen_log") == 2)
      assert(first.world:total_liquid_amount("liquid.water.legacy") >= 15)
    end,
  },
  {
    name = "terrain landmarks use ordinary physical material behavior instead of decorative exceptions",
    run = function()
      local value = floor("biome.legacy.forest", "tier.legacy.1", 94002)
      local tree = assert(object(value.world, "world_object.feature.old_growth_tree"))
      assert(tree.current_integrity == 6 and not tree.movable_by_force)
      assert(value.world:blocks_vision(tree.x, tree.y) and value.world:blocks_projectile(tree.x, tree.y))
      local result = value.world:damage_object(tree, { amount = 6, cause = "test" })
      assert(result.applied and result.destroyed and value.world:is_passable(tree.x, tree.y))
      assert(value.world:validate())

      local reactor = floor("biome.legacy.reactor", "tier.legacy.3", 94003)
      local cable = assert(object(reactor.world, "world_object.feature.cable_trunk"))
      local conductivity = reactor.world:conductivity_at(cable.x, cable.y)
      assert(conductivity.conductive and conductivity.object == cable.id)
      assert(not cable.blocks_movement and not cable.blocks_projectiles)
    end,
  },
  {
    name = "cave dungeon and reactor receive distinct diegetic landmark composition",
    run = function()
      local cave = floor("biome.legacy.cave", "tier.legacy.3", 94010)
      assert(object_count(cave.world, "world_object.feature.stalagmite") == 5)
      assert(object_count(cave.world, "world_object.feature.granite_boulder") == 3)
      assert(cave.world:total_liquid_amount("liquid.water.legacy") >= 18)

      local dungeon = floor("biome.legacy.dungeon", "tier.legacy.3", 94011)
      assert(object_count(dungeon.world, "world_object.feature.rubble_pile") == 4)
      assert(object_count(dungeon.world, "world_object.feature.ruined_statue") == 2)

      local reactor = floor("biome.legacy.reactor", "tier.legacy.3", 94012)
      local report = report_for(reactor)
      assert(report.valid and report.metrics.landmarks == 4)
      assert(object_count(reactor.world, "world_object.feature.machine_bank") == 3)
      assert(object_count(reactor.world, "world_object.feature.reinforced_access_panel") == 1)
      assert(object_count(reactor.world, "world_object.feature.cable_trunk") == 6)
      assert(reactor.world:total_liquid_amount("liquid.water.legacy") >= 30)
    end,
  },
  {
    name = "terrain landmark provenance is exposed to generation analysis and remains noncritical across representative seeds",
    run = function()
      for _, spec in ipairs({
        { biome = "biome.legacy.forest", tier = "tier.legacy.1", seed = 94020 },
        { biome = "biome.legacy.cave", tier = "tier.legacy.2", seed = 94040 },
        { biome = "biome.legacy.dungeon", tier = "tier.legacy.3", seed = 94060 },
        { biome = "biome.legacy.reactor", tier = "tier.legacy.3", seed = 94080 },
      }) do
        for offset = 0, 5 do
          local value = floor(spec.biome, spec.tier, spec.seed + offset)
          local report = report_for(value)
          assert(report.valid and report.metrics.landmarks >= 3)
          assert(#report.landmarks == report.metrics.landmarks)
          assert(#Analysis.overlay_model(value.world, report).landmarks == report.metrics.landmarks)
        end
      end
      -- These were the sparse-forest regression seeds that showed why local
      -- clearance alone is insufficient: a real tree/boulder formation must
      -- not separate a target from the arrival clearing.
      for _, seed in ipairs({ 931013, 931026, 931044 }) do
        local value = floor("biome.legacy.forest", "tier.legacy.1", seed)
        assert(report_for(value).valid)
      end
    end,
  },
}
