-- Small discrete progression profiles.  These retain the numerical tuning of
-- the former stage table without making a biome imply a particular tier.
return {
  {
    id = "tier.legacy.1",
    number = 1,
    -- Opening floors establish the physical roster without swarming a fresh
    -- body.  Every initial target/enemy can still contribute to the finite
    -- first-generation SCRAP pool.
    settings = { level = 0, targets = 3, enemies = 4, objective_required = 4, ammo = 2, vision = 6, torches = 3,
      completion_scrap_reward = 3, completion_data_reward = 1 },
  },
  {
    id = "tier.legacy.2",
    number = 2,
    -- Tier two broadens the roster before the first milestone instead of
    -- briefly becoming the sparsest normal floor in a run.
    settings = { level = 1, targets = 3, enemies = 4, objective_required = 5, ammo = 2, vision = 5, torches = 3,
      completion_scrap_reward = 3, completion_data_reward = 1 },
  },
  {
    id = "tier.legacy.3",
    number = 3,
    -- Tier three is the densest normal floor, but its objective remains
    -- achievable from the initial population unless Long Hunt is selected.
    settings = { level = 2, targets = 2, enemies = 5, objective_required = 6, ammo = 2, vision = 4, torches = 3,
      completion_scrap_reward = 3, completion_data_reward = 1 },
  },
}
