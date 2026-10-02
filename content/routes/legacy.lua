-- The first production route profile is intentionally small: it preserves
-- three normal floors before the existing shop and boss while making the two
-- middle descents explicit, deterministic player choices.
return {
  {
    id = "route_profile.legacy.base",
    display_name = "LEGACY DESCENT",
    layers = {
      {
        type = "floor",
        nodes = {
          { key = "opening_forest", biome_id = "biome.legacy.forest", tier_id = "tier.legacy.1", service_id = "service.supply.legacy" },
        },
      },
      {
        type = "floor",
        nodes = {
          { key = "forest_tier_2", biome_id = "biome.legacy.forest", tier_id = "tier.legacy.2", service_id = "service.repair.legacy" },
          { key = "cave_tier_2", biome_id = "biome.legacy.cave", tier_id = "tier.legacy.2", service_id = "service.salvager.legacy" },
        },
      },
      {
        type = "boss",
        nodes = {
          { key = "forest_milestone_boss", boss_id = "boss.forest.iron_colossus" },
          { key = "cave_milestone_boss", boss_id = "boss.cave.flooded_conductor" },
        },
      },
      {
        type = "floor",
        nodes = {
          { key = "cave_tier_3", biome_id = "biome.legacy.cave", tier_id = "tier.legacy.3", service_id = "service.charm_vendor.legacy" },
          { key = "dungeon_tier_3", biome_id = "biome.legacy.dungeon", tier_id = "tier.legacy.3", service_id = "service.supply.legacy" },
          { key = "reactor_tier_3", biome_id = "biome.legacy.reactor", tier_id = "tier.legacy.3", service_id = "service.salvager.legacy" },
          { key = "forest_tier_3_breach", biome_id = "biome.legacy.forest", tier_id = "tier.legacy.3", service_id = "service.repair.legacy" },
        },
      },
      {
        type = "boss",
        nodes = {
          { key = "wild_second_milestone_boss", boss_id = "boss.wild.ash_mauler" },
          { key = "industrial_second_milestone_boss", boss_id = "boss.industrial.barrage_custodian" },
        },
      },
      -- The two hubs share ordinary final-service behaviour, but remain
      -- explicit route nodes so late route history deterministically selects
      -- both the universal Apex node and the terminal boss without a hidden
      -- Session conditional.
      {
        type = "shop",
        nodes = {
          { key = "legacy_shop" },
          { key = "industrial_final_hub" },
        },
      },
      {
        type = "boss",
        nodes = {
          { key = "wild_apex_boss", boss_id = "boss.apex.kinetic_harbinger" },
          { key = "industrial_apex_boss", boss_id = "boss.apex.kinetic_harbinger" },
        },
      },
      {
        type = "boss",
        nodes = {
          { key = "legacy_final_boss", boss_id = "boss.legacy.final" },
          { key = "industrial_final_boss", boss_id = "boss.industrial.terminal_bastion" },
        },
      },
    },
    -- Explicit forward grammar preserves the legacy routes while adding one
    -- optional, account-gated third-floor branch.
    edges = {
      { from = "opening_forest", to = "forest_tier_2" },
      { from = "opening_forest", to = "cave_tier_2" },
      { from = "forest_tier_2", to = "forest_milestone_boss" },
      { from = "cave_tier_2", to = "cave_milestone_boss" },
      { from = "forest_milestone_boss", to = "cave_tier_3" },
      { from = "forest_milestone_boss", to = "dungeon_tier_3" },
      { from = "forest_milestone_boss", to = "reactor_tier_3" },
      { from = "forest_milestone_boss", to = "forest_tier_3_breach", requires_unlock = "unlock.traversal.reinforced_breach" },
      { from = "cave_milestone_boss", to = "cave_tier_3" },
      { from = "cave_milestone_boss", to = "dungeon_tier_3" },
      { from = "cave_milestone_boss", to = "reactor_tier_3" },
      { from = "cave_milestone_boss", to = "forest_tier_3_breach", requires_unlock = "unlock.traversal.reinforced_breach" },
      -- Tier-three biome is the deterministic second-milestone assignment;
      -- there is no reroll on entry.
      { from = "cave_tier_3", to = "wild_second_milestone_boss" },
      { from = "forest_tier_3_breach", to = "wild_second_milestone_boss" },
      { from = "dungeon_tier_3", to = "industrial_second_milestone_boss" },
      { from = "reactor_tier_3", to = "industrial_second_milestone_boss" },
      { from = "wild_second_milestone_boss", to = "legacy_shop" },
      { from = "industrial_second_milestone_boss", to = "industrial_final_hub" },
      { from = "legacy_shop", to = "wild_apex_boss" },
      { from = "industrial_final_hub", to = "industrial_apex_boss" },
      -- Wild and breach paths retain the Legacy Warden. Industrial paths
      -- receive the alternate physical terminal without a reroll at entry.
      { from = "wild_apex_boss", to = "legacy_final_boss" },
      { from = "industrial_apex_boss", to = "industrial_final_boss" },
    },
  },
}
