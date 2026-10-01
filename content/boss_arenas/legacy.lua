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
}
