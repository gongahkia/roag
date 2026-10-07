-- Shared Expedition authoring vocabulary.  This is deliberately small: it
-- describes content names, while selection, spawning and combat stay in Lua.
local Vocabulary = {}

Vocabulary.ROLE_COSTS = { rusher = 1, flanker = 2, ranged = 2, controller = 3, heavy = 4 }
Vocabulary.ENEMIES_BY_ROLE = {
  rusher = { "enemy.wild.ripper", "enemy.legacy.bomber" },
  flanker = { "enemy.wild.skirmisher", "enemy.cave.lance_acolyte" },
  ranged = { "enemy.legacy.cultist", "enemy.dungeon.scatter_gunner" },
  controller = { "enemy.cave.conductor", "enemy.reactor.arc_cutter" },
  heavy = { "enemy.dungeon.bulwark", "enemy.reactor.maintenance_heavy" },
}

Vocabulary.TOPOLOGY_TAGS = {
  open = true, lane = true, cross = true, choke = true, pinball = true,
  pockets = true, conductive = true, volatile = true, breakable = true, boss = true,
}
Vocabulary.ARCHETYPES = {
  swarm = true, crossfire = true, pincer = true, duel = true, hazard = true,
  breach = true, encirclement = true, hunter_kite = true, elite_hunt = true,
  reinforcement_pressure = true, volatile_arena = true, breakpoint = true,
}
Vocabulary.SPAWN_INTENTS = {
  ring = true, pincer = true, crossfire = true, east = true, hunter = true,
}
Vocabulary.REWARD_INTENTS = { scheduled = true, none = true, random_modifier = true, choice_modifier = true, elite_bonus = true, paid_cache = true }
Vocabulary.CLEAR_CONDITIONS = { eliminate = true, reinforcement = true }

-- Tile semantics are intentionally renderer-independent.  `walkable` is
-- used by validation and board instantiation; effects are applied by Session.
Vocabulary.TILES = {
  floor = { walkable = true }, water = { walkable = true, liquid = true },
  gas = { walkable = true, gas = true }, fire = { walkable = false, fire = true, material = "material.structure.wood" },
  spikes = { walkable = true, hazard = "hazard.legacy.spike_field" },
  wall = { walkable = false, material = "material.structure.reinforced" },
  breakable = { walkable = false, material = "material.structure.masonry" },
  volatile = { walkable = false, material = "material.structure.wood" },
  ["door.closed"] = { walkable = false, material = "material.structure.reinforced" },
}

function Vocabulary.sorted_keys(values)
  local result = {}
  for value in pairs(values) do result[#result + 1] = value end
  table.sort(result)
  return result
end

return Vocabulary
