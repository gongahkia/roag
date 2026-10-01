-- Small discrete progression profiles.  These retain the numerical tuning of
-- the former stage table without making a biome imply a particular tier.
return {
  {
    id = "tier.legacy.1",
    number = 1,
    settings = { level = 0, targets = 4, enemies = 6, objective_required = 5, ammo = 2, vision = 6, torches = 3 },
  },
  {
    id = "tier.legacy.2",
    number = 2,
    settings = { level = 1, targets = 3, enemies = 1, objective_required = 4, ammo = 1, vision = 5, torches = 3 },
  },
  {
    id = "tier.legacy.3",
    number = 3,
    settings = { level = 2, targets = 1, enemies = 2, objective_required = 5, ammo = 2, vision = 4, torches = 3 },
  },
}
