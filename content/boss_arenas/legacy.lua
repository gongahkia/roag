-- Fixed placements stay intentionally small and declarative. The arena
-- builder validates every placement against the common World APIs.
return {
  {
    id = "boss_arena.forest.iron_colossus",
    display_name = "OVERGROWN KILLBOX",
    terrain = "arena",
    player_spawn = { x = 8, y = 9 },
    boss_spawn = { x = 31, y = 9 },
    cover = {
      { definition_id = "world_object.cover.timber_crate", x = 17, y = 7 },
      { definition_id = "world_object.cover.timber_crate", x = 17, y = 11 },
      { definition_id = "world_object.cover.masonry_barricade", x = 24, y = 9 },
    },
    hazards = {
      { definition_id = "hazard.legacy.spike_field", x = 20, y = 7 },
      { definition_id = "hazard.legacy.spike_field", x = 20, y = 11 },
    },
  },
  {
    id = "boss_arena.cave.flooded_conductor",
    display_name = "FLOODED CONDUIT",
    terrain = "arena",
    player_spawn = { x = 8, y = 9 },
    boss_spawn = { x = 31, y = 9 },
    liquid = {
      { liquid_id = "liquid.water.legacy", amount = 2, x = 19, y = 8 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 20, y = 8 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 21, y = 8 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 19, y = 9 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 20, y = 9 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 21, y = 9 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 19, y = 10 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 20, y = 10 },
      { liquid_id = "liquid.water.legacy", amount = 2, x = 21, y = 10 },
    },
    cover = {
      { definition_id = "world_object.cover.conductive_metal_crate", x = 16, y = 6 },
      { definition_id = "world_object.cover.masonry_barricade", x = 25, y = 12 },
    },
  },
  {
    id = "boss_arena.legacy.final",
    display_name = "LEGACY CITADEL",
    terrain = "arena",
    player_spawn = { x = 8, y = 9 },
    boss_spawn = { x = 31, y = 9 },
    cover = {
      { definition_id = "world_object.cover.masonry_barricade", x = 19, y = 6 },
      { definition_id = "world_object.cover.masonry_barricade", x = 19, y = 12 },
    },
    hazards = {
      { definition_id = "hazard.legacy.spike_field", x = 24, y = 7 },
      { definition_id = "hazard.legacy.spike_field", x = 24, y = 11 },
    },
  },
  {
    id = "boss_arena.wild.ash_mauler",
    display_name = "CINDER THICKET",
    terrain = "arena",
    player_spawn = { x = 8, y = 9 },
    boss_spawn = { x = 31, y = 9 },
    cover = {
      { definition_id = "world_object.cover.timber_crate", x = 16, y = 6 },
      { definition_id = "world_object.cover.timber_crate", x = 16, y = 12 },
      { definition_id = "world_object.cover.masonry_barricade", x = 23, y = 9 },
      -- The existing finite fire system consumes this ordinary timber crate.
      { definition_id = "world_object.cover.timber_crate", x = 25, y = 5 },
    },
    hazards = {
      { definition_id = "hazard.legacy.spike_field", x = 20, y = 9 },
    },
    gas = {
      { gas_id = "gas.toxic.legacy", concentration = 4, x = 22, y = 6 },
      { gas_id = "gas.toxic.legacy", concentration = 4, x = 22, y = 12 },
    },
    fires = {
      { target_kind = "object", x = 25, y = 5 },
    },
  },
  {
    id = "boss_arena.industrial.barrage_custodian",
    display_name = "BULKHEAD ARRAY",
    terrain = "arena",
    player_spawn = { x = 8, y = 9 },
    boss_spawn = { x = 31, y = 9 },
    -- This is presentation/content use of World.new's existing material
    -- layout input: conductive infrastructure without a Reactor-only rule.
    terrain_cells = {
      { material_id = "material.floor.conductive_metal", x = 17, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 18, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 19, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 20, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 21, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 22, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 23, y = 9 },
      { material_id = "material.floor.conductive_metal", x = 18, y = 8 },
      { material_id = "material.floor.conductive_metal", x = 18, y = 10 },
      { material_id = "material.floor.conductive_metal", x = 19, y = 8 },
      { material_id = "material.floor.conductive_metal", x = 19, y = 10 },
    },
    circuits = {
      { id = "power.circuit.boss_industrial_control", enabled = true },
    },
    devices = {
      { definition_id = "world_object.power.generator_legacy", x = 15, y = 7,
        circuit_id = "power.circuit.boss_industrial_control", generator_online = true },
      { definition_id = "world_object.power.breaker_legacy", x = 15, y = 11,
        circuit_id = "power.circuit.boss_industrial_control" },
      -- The closed door cuts a sightline but does not seal the arena, so
      -- disabling power can never turn this boss into a progression gate.
      { definition_id = "world_object.door.powered_legacy", x = 20, y = 9,
        circuit_id = "power.circuit.boss_industrial_control", door_state = "closed" },
    },
    cover = {
      { definition_id = "world_object.cover.conductive_metal_crate", x = 17, y = 6 },
      { definition_id = "world_object.cover.conductive_metal_crate", x = 17, y = 12 },
      { definition_id = "world_object.cover.masonry_barricade", x = 25, y = 6 },
    },
    liquid = {
      { liquid_id = "liquid.water.legacy", amount = 1, x = 18, y = 8 },
      { liquid_id = "liquid.water.legacy", amount = 1, x = 19, y = 8 },
    },
  },
}
