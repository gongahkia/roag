-- Finite, biome-authored reinforcement rosters.  A placed source owns a
-- single selected roster; no profile contains a loop or a replenishment rule.
return {
  {
    id = "reinforcement_profile.feral.forest",
    display_name = "Ravager Burrow",
    source_type = "nest",
    faction_id = "faction.feral",
    allowed_biome_ids = { "biome.legacy.forest" },
    allowed_tier_ids = { "tier.legacy.1", "tier.legacy.2", "tier.legacy.3" },
    wave_size = 1,
    entries = {
      { enemy_id = "enemy.wild.ripper", weight = 6 },
      { enemy_id = "enemy.wild.skirmisher", weight = 3 },
      { enemy_id = "enemy.legacy.bomber", weight = 1 },
    },
  },
  {
    id = "reinforcement_profile.cult.cave",
    display_name = "Altered Breach",
    source_type = "nest",
    faction_id = "faction.cult",
    allowed_biome_ids = { "biome.legacy.cave" },
    allowed_tier_ids = { "tier.legacy.1", "tier.legacy.2", "tier.legacy.3" },
    wave_size = 1,
    entries = {
      { enemy_id = "enemy.cave.conductor", weight = 5 },
      { enemy_id = "enemy.legacy.cultist", weight = 4 },
    },
  },
  {
    -- Caves also receive a small feral profile.  This lets a visible nest be
    -- relevant on a cave floor whose authored encounter happened to select
    -- physical intruders rather than an Altered actor.
    id = "reinforcement_profile.feral.cave",
    display_name = "Ravager Burrow",
    source_type = "nest",
    faction_id = "faction.feral",
    allowed_biome_ids = { "biome.legacy.cave" },
    allowed_tier_ids = { "tier.legacy.1", "tier.legacy.2", "tier.legacy.3" },
    wave_size = 1,
    entries = {
      { enemy_id = "enemy.wild.ripper", weight = 6 },
      { enemy_id = "enemy.wild.skirmisher", weight = 4 },
    },
  },
  {
    id = "reinforcement_profile.machine.dungeon",
    display_name = "Bulkhead Lift",
    source_type = "lift",
    faction_id = "faction.machine",
    allowed_biome_ids = { "biome.legacy.dungeon" },
    allowed_tier_ids = { "tier.legacy.1", "tier.legacy.2", "tier.legacy.3" },
    wave_size = 1,
    entries = {
      { enemy_id = "enemy.dungeon.bulwark", weight = 5 },
    },
  },
  {
    id = "reinforcement_profile.machine.reactor",
    display_name = "Reactor Deployment Lift",
    source_type = "lift",
    faction_id = "faction.machine",
    allowed_biome_ids = { "biome.legacy.reactor" },
    allowed_tier_ids = { "tier.legacy.2", "tier.legacy.3" },
    wave_size = 2,
    entries = {
      { enemy_id = "enemy.reactor.arc_cutter", weight = 5 },
      { enemy_id = "enemy.reactor.suppressor", weight = 3 },
      { enemy_id = "enemy.reactor.maintenance_heavy", weight = 2 },
    },
  },
}
