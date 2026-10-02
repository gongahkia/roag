-- ROAG sprite assignments.
--
-- Each entry is { column, row } in Kenney's 49 x 22 colored-packed sheet.
-- Edit these values directly, or use the standalone Sprite Editor to save
-- repository-local mappings without changing normal-game state.

return {
  player = {25, 1},
  target = {38, 3},
  ammo = {23, 5},
  torch = {20, 7},
  door = {22, 1},
  bullet = {34, 3},
  bomb = {38, 6},
  flare = {23, 6},
  -- Simulation-owned terrain and effect layers are presentation roles too.
  -- They intentionally remain editable in the standalone Sprite Editor.
  ground = {1, 22},
  water_shallow = {10, 6},
  water_deep = {11, 6},
  spikes = {15, 11},
  fire = {15, 11},
  gas = {1, 22},
  electric_arc = {28, 21},
  necromancer = {27, 10},
  wolf = {31, 9},
  bomber = {20, 9},
  cultist = {28, 10},
  ripper = {31, 9},
  skirmisher = {29, 10},
  conductor = {27, 10},
  bulwark = {30, 9},
  reclaimer = {26, 10},
  gunner_elite = {25, 10},
  shock_bruiser = {28, 9},
  volatile_heavy = {20, 9},
  arc_cutter = {27, 10},
  maintenance_heavy = {30, 9},
  reactor_suppressor = {29, 10},
  arc_warden = {28, 9},
  boss = {30, 2},
  -- World props are named roles too.  Runtime code must draw these through an
  -- art-pack mapping rather than synthesising replacement geometry.
  wall_left = {10, 4},
  wall_right = {11, 4},
  wall_up = {12, 4},
  wall_down = {13, 4},
  -- Wall corners and the closed centre complete the terrain autotile set.
  -- The standalone Sprite Editor owns the shipped coordinates for the
  -- original sheet, so these defaults are intentionally editable too.
  wall_top_left = {17, 14},
  wall_top_right = {19, 14},
  wall_bottom_left = {17, 16},
  wall_bottom_right = {19, 16},
  wall_center = {18, 15},
  old_growth_tree = {5, 2},
  fallen_log = {9, 3},
  granite_boulder = {2, 14},
  stalagmite = {3, 14},
  rubble_pile = {17, 15},
  ruined_statue = {18, 15},
  machine_bank = {24, 11},
  cable_trunk = {24, 10},
  barricade = {16, 11},
  crate = {12, 9},
  metal_crate = {14, 9},
  powered_door = {22, 1},
  generator = {19, 8},
  breaker = {20, 8},
  service_kiosk = {23, 8},
  reinforced_barrier = {15, 12},
  maintenance_hatch = {17, 12},
  discovery_cache = {22, 8},
  discovery_clue = {21, 8},
  reinforcement_nest = {8, 3},
  reinforcement_lift = {24, 8},
}
