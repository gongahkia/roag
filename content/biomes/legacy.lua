-- Biome identity is deliberately separate from floor progression.  The
-- current generators remain the authority for their terrain implementation.
return {
  {
    id = "biome.legacy.forest",
    display_name = "FOREST",
    generator = "forest",
    terrain = "forest",
  },
  {
    id = "biome.legacy.cave",
    display_name = "CAVE",
    generator = "cave",
    terrain = "cave",
  },
  {
    id = "biome.legacy.dungeon",
    display_name = "DUNGEON",
    generator = "dungeon",
    terrain = "dungeon",
  },
}
