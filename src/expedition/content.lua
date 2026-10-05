-- Expedition characters and encounter grammar. Passive modifier authority is
-- deliberately elsewhere: strict JSON files compiled by expedition.modifiers.
local Content = {}
local ModifierDefinitions = require("src.expedition.modifiers")

Content.DEFAULT_CHARACTER_IDS = { "expedition.gunner", "expedition.bruiser" }

Content.CHARACTERS = {
  { id = "expedition.gunner", display_name = "GUNNER", description = "Fast projectile pressure. Build magazines, pellets and pierce.", base_hp = 6,
    weapon_component = "component.arm.legacy_projectile_emitter", weapon_ability = "ability.weapon.projectile.basic", ability_component = "component.internal.legacy_support", active_ability = "ability.mobility.dash", ammo_family = "bullets", starting_reserve = 16, base_modifiers = { projectile_damage = 1 }, unlock = "expedition.unlock.character.gunner" },
  { id = "expedition.bruiser", display_name = "BRUISER", description = "Heavy melee and collision control. Build Force into pinball rooms.", base_hp = 9,
    weapon_component = "component.arm.impact_maul", weapon_ability = "ability.weapon.melee.impact_maul", ability_component = "component.internal.legacy_support", active_ability = "ability.mobility.dash", starting_reserve = 0, base_modifiers = { melee_damage = 1, melee_force = 1 }, unlock = "expedition.unlock.character.bruiser" },
  { id = "expedition.conductor", display_name = "CONDUCTOR", description = "Piercing lines and electrical cascades. Fight around conductive terrain.", base_hp = 6,
    weapon_component = "component.arm.piercing_lance", weapon_ability = "ability.weapon.piercing_lance", ability_component = "component.internal.legacy_shock_coil", active_ability = "ability.electrical.discharge", ammo_family = "energy_cells", starting_reserve = 14, base_modifiers = { projectile_pierce = 1 }, unlock = "expedition.unlock.character.conductor", unlock_description = "REACH STAGE 2 IN AN EXPEDITION" },
  { id = "expedition.demolitionist", display_name = "DEMOLITIONIST", description = "Explosions and component breaks. Turn damaged bodies into area denial.", base_hp = 7,
    weapon_component = "component.arm.siege_emitter", weapon_ability = "ability.weapon.projectile.siege", ability_component = "component.internal.legacy_volatile_charge", active_ability = "ability.mobility.dash", ammo_family = "shells", starting_reserve = 12, base_modifiers = { projectile_damage = 1 }, starting_passives = { "expedition.passive.demolition_kit" }, unlock = "expedition.unlock.character.demolitionist", unlock_description = "COMPLETE AN EXPEDITION" },
}

Content.ENCOUNTERS = {
  { id = "expedition.encounter.swarm", display_name = "SWARM", kind = "swarm", description = "Many weak close threats.", roles = { "rusher", "flanker" }, minimum_roles = { rusher = 2 }, profiles = { "biome.legacy.forest", "biome.legacy.cave" }, topologies = { "open", "pockets" }, max_enemies = 5 },
  { id = "expedition.encounter.crossfire", display_name = "CROSSFIRE", kind = "crossfire", description = "Separated ranged positions.", roles = { "ranged", "controller" }, minimum_roles = { ranged = 2 }, profiles = { "biome.legacy.forest", "biome.legacy.dungeon", "biome.legacy.reactor" }, topologies = { "cross", "lane" }, max_enemies = 5 },
  { id = "expedition.encounter.pincer", display_name = "PINCER", kind = "pincer", description = "Threats converge from multiple sides.", roles = { "rusher", "flanker", "ranged" }, minimum_roles = { rusher = 1, flanker = 1 }, profiles = { "biome.legacy.cave", "biome.legacy.dungeon" }, topologies = { "pockets", "cross" }, max_enemies = 5 },
  { id = "expedition.encounter.duel", display_name = "DUEL", kind = "duel", description = "One dangerous high-value threat with minimal support.", roles = { "heavy", "ranged" }, minimum_roles = { heavy = 1 }, profiles = { "biome.legacy.cave", "biome.legacy.dungeon" }, topologies = { "lane", "open" }, max_enemies = 3 },

  { id = "expedition.encounter.hazard", display_name = "HAZARD", kind = "hazard", description = "Combat where water, fire or cover matters.", roles = { "controller", "ranged", "rusher" }, minimum_roles = { controller = 1 }, profiles = { "biome.legacy.cave", "biome.legacy.reactor" }, hazard = true, topologies = { "conductive", "volatile" }, max_enemies = 5 },
  { id = "expedition.encounter.breach", display_name = "BREACH", kind = "breach", description = "Break or flank a defended chokepoint.", roles = { "heavy", "ranged", "controller" }, minimum_roles = { ranged = 1, heavy = 1 }, profiles = { "biome.legacy.dungeon", "biome.legacy.reactor" }, topologies = { "breakable", "choke" }, max_enemies = 5 },
  { id = "expedition.encounter.encirclement", display_name = "ENCIRCLEMENT", kind = "encirclement", description = "Threats close from several visible sides.", roles = { "rusher", "flanker", "ranged" }, minimum_roles = { rusher = 1, flanker = 1 }, profiles = { "biome.legacy.cave", "biome.legacy.dungeon" }, topologies = { "cross", "open" }, max_enemies = 6 },
  { id = "expedition.encounter.hunter_kite", display_name = "HUNTER / KITE", kind = "hunter_kite", description = "A mobile hunter pressures movement while zoning fire controls space.", roles = { "flanker", "ranged", "controller" }, minimum_roles = { flanker = 1, ranged = 1 }, profiles = { "biome.legacy.forest", "biome.legacy.reactor" }, topologies = { "lane", "pockets" }, max_enemies = 5 },

  { id = "expedition.encounter.elite_hunt", display_name = "ELITE HUNT", kind = "elite_hunt", description = "A dangerous elite with limited support.", roles = { "heavy", "ranged", "rusher" }, minimum_roles = { heavy = 1 }, profiles = { "biome.legacy.dungeon", "biome.legacy.reactor" }, elite = true, topologies = { "pinball", "choke" }, max_enemies = 5 },
  { id = "expedition.encounter.reinforcement_pressure", display_name = "REINFORCEMENT PRESSURE", kind = "reinforcement_pressure", description = "Destroy the finite source or withstand its wave.", roles = { "rusher", "ranged", "controller" }, minimum_roles = { rusher = 1 }, profiles = { "biome.legacy.reactor", "biome.legacy.dungeon" }, reinforcement = true, topologies = { "choke", "breakable" }, max_enemies = 6 },
  { id = "expedition.encounter.volatile_arena", display_name = "VOLATILE ARENA", kind = "volatile_arena", description = "Flammable barriers and tight groups reward detonations.", roles = { "rusher", "heavy", "controller" }, minimum_roles = { rusher = 1, heavy = 1 }, profiles = { "biome.legacy.cave", "biome.legacy.reactor" }, hazard = true, topologies = { "volatile", "breakable" }, max_enemies = 6 },
  { id = "expedition.encounter.breakpoint", display_name = "BREAKPOINT", kind = "breakpoint", description = "A compact build-showcase board with dense viable targets.", roles = { "rusher", "flanker", "controller", "heavy" }, minimum_roles = { rusher = 2, controller = 1 }, profiles = { "biome.legacy.reactor", "biome.legacy.dungeon" }, topologies = { "conductive", "pinball" }, max_enemies = 7, showcase = true },
}

Content.ENEMY_COSTS = { rusher = 1, flanker = 2, ranged = 2, controller = 3, heavy = 4 }
Content.ENEMY_BY_ROLE = {
  rusher = { "enemy.wild.ripper", "enemy.legacy.bomber" }, flanker = { "enemy.wild.skirmisher", "enemy.cave.lance_acolyte" },
  ranged = { "enemy.legacy.cultist", "enemy.dungeon.scatter_gunner" }, controller = { "enemy.cave.conductor", "enemy.reactor.arc_cutter" },
  heavy = { "enemy.dungeon.bulwark", "enemy.reactor.maintenance_heavy" },
}

local VALID_MODIFIER_KEYS = { max_health = true, dash_cooldown = true, bomb_radius = true, melee_damage = true, melee_force = true, projectile_damage = true, projectile_count = true, scatter_pellets = true, projectile_pierce = true, projectile_range = true, magazine_capacity = true, ammo_on_kill = true, clear_heal = true }
local function find(values, id) for _, value in ipairs(values) do if value.id == id then return value end end end
function Content.character(id) return find(Content.CHARACTERS, id) end
function Content.encounter(id) return find(Content.ENCOUNTERS, id) end
function Content.passive(id) return Content.modifier_registry:get(id) end
function Content.sorted_passives() return Content.modifier_registry:list() end

function Content.validate(registry)
  local loaded, failure = ModifierDefinitions.load({ registry = registry })
  assert(loaded, failure and failure.message or "Unable to load Expedition modifiers")
  Content.modifier_registry, Content.PASSIVES = loaded, loaded.ordered -- compatibility read-only list; JSON remains authority.
  assert(#loaded.ordered >= 20, "Expedition requires a meaningful passive pool")
  local behavior_count = 0
  for _, passive in ipairs(loaded.ordered) do if passive.behavior_changing then behavior_count = behavior_count + 1 end end
  assert(behavior_count >= 8, "Expedition requires eight behavior-changing passives")
  assert(#Content.ENCOUNTERS >= 12, "Expedition requires twelve chamber encounter archetypes")
  for _, character in ipairs(Content.CHARACTERS) do
    assert(registry.components[character.weapon_component] and registry.abilities[character.weapon_ability], "missing Expedition class weapon")
    assert(registry.components[character.ability_component] and registry.abilities[character.active_ability], "missing Expedition class ability")
    assert(character.base_hp > 0, "invalid Expedition HP")
    for key, value in pairs(character.base_modifiers or {}) do assert(VALID_MODIFIER_KEYS[key] and type(value) == "number", "invalid Expedition character modifier: " .. tostring(key)) end
    for _, passive_id in ipairs(character.starting_passives or {}) do assert(Content.passive(passive_id), "missing Expedition starting passive: " .. passive_id) end
  end
  for _, encounter in ipairs(Content.ENCOUNTERS) do
    assert(encounter.minimum_roles and next(encounter.minimum_roles), "encounter requires composition constraint")
    for role in pairs(encounter.minimum_roles) do assert(Content.ENEMY_COSTS[role], "unknown Expedition role: " .. role) end
    for _, role in ipairs(encounter.roles or {}) do assert(Content.ENEMY_COSTS[role], "unknown Expedition encounter role: " .. role) end
    assert(type(encounter.topologies) == "table" and #encounter.topologies > 0, "encounter requires chamber topology")
    assert(type(encounter.max_enemies) == "number" and encounter.max_enemies >= 1, "encounter requires maximum enemy count")
  end
  for role, enemy_ids in pairs(Content.ENEMY_BY_ROLE) do for _, enemy_id in ipairs(enemy_ids) do assert(Content.ENEMY_COSTS[role] and registry.enemies[enemy_id], "missing Expedition enemy: " .. enemy_id) end end
  return true
end

Content.modifier_registry = assert(ModifierDefinitions.load())
Content.PASSIVES = Content.modifier_registry.ordered
return Content
