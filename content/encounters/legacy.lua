-- Authored normal-floor encounter pools.  They choose real enemy body
-- definitions; runtime combat only sees the materialized bodies and their
-- live capabilities.  Weights are selection likelihood, never HP scaling.
return {
  {
    id = "encounter_pool.legacy.forest.tier1",
    biome_id = "biome.legacy.forest",
    tier_id = "tier.legacy.1",
    entries = {
      { enemy_id = "enemy.wild.ripper", weight = 4 },
      { enemy_id = "enemy.wild.skirmisher", weight = 2 },
      { enemy_id = "enemy.legacy.bomber", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.forest.tier2",
    biome_id = "biome.legacy.forest",
    tier_id = "tier.legacy.2",
    entries = {
      { enemy_id = "enemy.wild.ripper", weight = 3 },
      { enemy_id = "enemy.wild.skirmisher", weight = 3 },
      { enemy_id = "enemy.wild.scatter_skirmisher", weight = 1 },
      { enemy_id = "enemy.legacy.bomber", weight = 2 },
    },
  },
  {
    id = "encounter_pool.legacy.forest.tier3",
    biome_id = "biome.legacy.forest",
    tier_id = "tier.legacy.3",
    entries = {
      { enemy_id = "enemy.wild.ripper", weight = 2 },
      { enemy_id = "enemy.wild.skirmisher", weight = 3 },
      { enemy_id = "enemy.wild.scatter_skirmisher", weight = 2 },
      { enemy_id = "enemy.legacy.bomber", weight = 3 },
    },
  },
  {
    id = "encounter_pool.legacy.cave.tier1",
    biome_id = "biome.legacy.cave",
    tier_id = "tier.legacy.1",
    entries = {
      { enemy_id = "enemy.wild.ripper", weight = 2 },
      { enemy_id = "enemy.cave.conductor", weight = 2 },
    },
  },
  {
    id = "encounter_pool.legacy.cave.tier2",
    biome_id = "biome.legacy.cave",
    tier_id = "tier.legacy.2",
    entries = {
      { enemy_id = "enemy.cave.conductor", weight = 4 },
      { enemy_id = "enemy.legacy.cultist", weight = 2 },
      { enemy_id = "enemy.cave.lance_acolyte", weight = 1 },
      { enemy_id = "enemy.wild.skirmisher", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.cave.tier3",
    biome_id = "biome.legacy.cave",
    tier_id = "tier.legacy.3",
    entries = {
      { enemy_id = "enemy.cave.conductor", weight = 3 },
      { enemy_id = "enemy.legacy.cultist", weight = 3 },
      { enemy_id = "enemy.cave.lance_acolyte", weight = 2 },
      { enemy_id = "enemy.elite.shock_bruiser", weight = 1 },
      -- A rare Ravager intruder creates occasional readable cross-faction
      -- contact without erasing the cave's Altered identity.
      { enemy_id = "enemy.wild.ripper", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.dungeon.tier1",
    biome_id = "biome.legacy.dungeon",
    tier_id = "tier.legacy.1",
    entries = {
      { enemy_id = "enemy.wild.skirmisher", weight = 2 },
      { enemy_id = "enemy.dungeon.bulwark", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.dungeon.tier2",
    biome_id = "biome.legacy.dungeon",
    tier_id = "tier.legacy.2",
    entries = {
      { enemy_id = "enemy.dungeon.bulwark", weight = 3 },
      { enemy_id = "enemy.dungeon.reclaimer", weight = 2 },
      { enemy_id = "enemy.dungeon.scatter_gunner", weight = 1 },
      { enemy_id = "enemy.legacy.cultist", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.dungeon.tier3",
    biome_id = "biome.legacy.dungeon",
    tier_id = "tier.legacy.3",
    entries = {
      { enemy_id = "enemy.dungeon.bulwark", weight = 3 },
      { enemy_id = "enemy.dungeon.reclaimer", weight = 4 },
      { enemy_id = "enemy.dungeon.scatter_gunner", weight = 2 },
      { enemy_id = "enemy.elite.redundant_gunner", weight = 1 },
      { enemy_id = "enemy.elite.volatile_heavy", weight = 1 },
      { enemy_id = "enemy.legacy.cultist", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.reactor.tier2",
    biome_id = "biome.legacy.reactor",
    tier_id = "tier.legacy.2",
    entries = {
      { enemy_id = "enemy.reactor.arc_cutter", weight = 4 },
      { enemy_id = "enemy.cave.conductor", weight = 2 },
      { enemy_id = "enemy.reactor.rail_hunter", weight = 1 },
      { enemy_id = "enemy.reactor.suppressor", weight = 1 },
    },
  },
  {
    id = "encounter_pool.legacy.reactor.tier3",
    biome_id = "biome.legacy.reactor",
    tier_id = "tier.legacy.3",
    entries = {
      { enemy_id = "enemy.reactor.arc_cutter", weight = 3 },
      { enemy_id = "enemy.reactor.maintenance_heavy", weight = 3 },
      { enemy_id = "enemy.reactor.suppressor", weight = 2 },
      { enemy_id = "enemy.reactor.rail_hunter", weight = 2 },
      { enemy_id = "enemy.elite.arc_warden", weight = 1 },
      -- Limited intrusion only: Reactor remains overwhelmingly machine-held.
      { enemy_id = "enemy.cave.conductor", weight = 1 },
    },
  },
}
