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
          { key = "opening_forest", biome_id = "biome.legacy.forest", tier_id = "tier.legacy.1" },
        },
      },
      {
        type = "floor",
        nodes = {
          { key = "forest_tier_2", biome_id = "biome.legacy.forest", tier_id = "tier.legacy.2" },
          { key = "cave_tier_2", biome_id = "biome.legacy.cave", tier_id = "tier.legacy.2" },
        },
      },
      {
        type = "floor",
        nodes = {
          { key = "cave_tier_3", biome_id = "biome.legacy.cave", tier_id = "tier.legacy.3" },
          { key = "dungeon_tier_3", biome_id = "biome.legacy.dungeon", tier_id = "tier.legacy.3" },
        },
      },
      { type = "shop", nodes = { { key = "legacy_shop" } } },
      { type = "boss", nodes = { { key = "legacy_final_boss" } } },
    },
  },
}
