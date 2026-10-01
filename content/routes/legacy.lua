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
      { type = "shop", nodes = { { key = "legacy_shop" } } },
      { type = "boss", nodes = { { key = "legacy_final_boss", boss_id = "boss.legacy.final" } } },
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
      { from = "cave_tier_3", to = "legacy_shop" },
      { from = "dungeon_tier_3", to = "legacy_shop" },
      { from = "reactor_tier_3", to = "legacy_shop" },
      { from = "forest_tier_3_breach", to = "legacy_shop" },
      { from = "legacy_shop", to = "legacy_final_boss" },
    },
  },
}
