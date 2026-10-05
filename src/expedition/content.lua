-- Expedition prototype content is deliberately mechanical.  It owns the
-- small class/item/encounter vocabulary without taking ownership of Sandbox
-- world content or saves.
local Content = {}
local ModifierDefinitions = require("src.expedition.modifiers")

local VALID_MODIFIER_KEYS = {
    max_health = true, dash_cooldown = true, bomb_radius = true,
    melee_damage = true, melee_force = true, projectile_damage = true,
    projectile_count = true, scatter_pellets = true, projectile_pierce = true,
    projectile_range = true, magazine_capacity = true, ammo_on_kill = true,
    clear_heal = true,
}
local VALID_TRIGGERS = {
    on_attack = true, on_hit = true, on_kill = true, on_component_break = true,
    on_pierce = true, on_push_collision = true, on_reload = true, on_terrain_break = true,
}
local VALID_EFFECT_KINDS = {
    chain_electricity = true, kinetic_burst = true, small_explosion = true,
    magazine_refund = true, cooldown_reduction = true, ignite = true,
}

Content.DEFAULT_CHARACTER_IDS = {
    "expedition.gunner",
    "expedition.bruiser",
}

Content.CHARACTERS = {
    {
        id = "expedition.gunner",
        display_name = "GUNNER",
        description = "Fast projectile pressure. Build magazines, pellets and pierce.",
        base_hp = 6,
        weapon_component = "component.arm.legacy_projectile_emitter",
        weapon_ability = "ability.weapon.projectile.basic",
        ability_component = "component.internal.legacy_support",
        active_ability = "ability.mobility.dash",
        ammo_family = "bullets",
        starting_reserve = 16,
        base_modifiers = { projectile_damage = 1 },
        unlock = "expedition.unlock.character.gunner",
    },
    {
        id = "expedition.bruiser",
        display_name = "BRUISER",
        description = "Heavy melee and collision control. Build Force into pinball rooms.",
        base_hp = 9,
        weapon_component = "component.arm.impact_maul",
        weapon_ability = "ability.weapon.melee.impact_maul",
        ability_component = "component.internal.legacy_support",
        active_ability = "ability.mobility.dash",
        ammo_family = nil,
        starting_reserve = 0,
        base_modifiers = { melee_damage = 1, melee_force = 1 },
        unlock = "expedition.unlock.character.bruiser",
    },
    {
        id = "expedition.conductor",
        display_name = "CONDUCTOR",
        description = "Piercing lines and electrical cascades. Fight around conductive terrain.",
        base_hp = 6,
        weapon_component = "component.arm.piercing_lance",
        weapon_ability = "ability.weapon.piercing_lance",
        ability_component = "component.internal.legacy_shock_coil",
        active_ability = "ability.electrical.discharge",
        ammo_family = "energy_cells",
        starting_reserve = 14,
        base_modifiers = { projectile_pierce = 1 },
        unlock = "expedition.unlock.character.conductor",
        unlock_description = "REACH STAGE 2 IN AN EXPEDITION",
    },
    {
        id = "expedition.demolitionist",
        display_name = "DEMOLITIONIST",
        description = "Explosions and component breaks. Turn damaged bodies into area denial.",
        base_hp = 7,
        weapon_component = "component.arm.siege_emitter",
        weapon_ability = "ability.weapon.projectile.siege",
        ability_component = "component.internal.legacy_volatile_charge",
        active_ability = "ability.mobility.dash",
        ammo_family = "shells",
        starting_reserve = 12,
        base_modifiers = { projectile_damage = 1 },
        starting_passives = { "expedition.passive.demolition_kit" },
        unlock = "expedition.unlock.character.demolitionist",
        unlock_description = "COMPLETE AN EXPEDITION",
    },
}

-- These are passive *run* pickups.  They have no physical IDs, no weight and
-- no slot cap; a stack is represented by a single count in ExpeditionRun.
Content.PASSIVES = {
    {
        id = "expedition.passive.ballistic_lens", display_name = "BALLISTIC LENS",
        description = "+1 projectile damage per stack.", category = "damage",
        modifiers = { projectile_damage = 1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.edge_tuning", display_name = "EDGE TUNING",
        description = "+1 melee damage per stack.", category = "damage",
        modifiers = { melee_damage = 1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.demolition_kit", display_name = "DEMOLITION KIT",
        description = "Explosive projectile hits burst outward. Each stack adds 1 blast damage.", category = "damage",
        behavior_changing = true,
        reactive_effects = {
            { id = "demolition_kit", trigger = "on_hit", conditions = { attack_tag = "explosive" },
              effect = { kind = "small_explosion", radius = 1, damage = 1 }, stack_scale = { damage = 1 } },
        },
    },
    {
        id = "expedition.passive.kinetic_capacitor", display_name = "KINETIC CAPACITOR",
        description = "+1 Force on melee hits per stack.", category = "force",
        modifiers = { melee_force = 1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.iron_heart", display_name = "IRON HEART",
        description = "+2 maximum HP per stack.", category = "defense",
        modifiers = { max_health = 2 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.windwalker", display_name = "WINDWALKER",
        description = "Dash cooldown is reduced by 1 per stack, to its safe floor.", category = "mobility",
        modifiers = { dash_cooldown = -1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.quick_reload", display_name = "QUICK RELOAD",
        description = "Each reload reduces active ability cooldown by 1 per stack.", category = "sustain",
        behavior_changing = true,
        reactive_effects = {
            { id = "quick_reload", trigger = "on_reload", effect = { kind = "cooldown_reduction", amount = 1 }, stack_scale = { amount = 1 } },
        },
    },
    {
        id = "expedition.passive.recycler", display_name = "RECYCLER",
        description = "Ranged kills load 1 compatible round per stack into the active magazine.", category = "sustain",
        behavior_changing = true,
        reactive_effects = {
            { id = "recycler", trigger = "on_kill", conditions = { attack_tag = "projectile" }, effect = { kind = "magazine_refund", amount = 1 }, stack_scale = { amount = 1 } },
        },
    },
    {
        id = "expedition.passive.scatter_matrix", display_name = "SCATTER MATRIX",
        description = "+1 visible projectile/pellet per stack.", category = "geometry",
        modifiers = { projectile_count = 1, scatter_pellets = 1 }, behavior_changing = true,
    },
    {
        id = "expedition.passive.piercing_rounds", display_name = "PIERCING ROUNDS",
        description = "+1 projectile pierce per stack.", category = "geometry",
        modifiers = { projectile_pierce = 1 }, behavior_changing = true,
    },
    {
        id = "expedition.passive.long_barrel", display_name = "LONG BARREL",
        description = "+1 projectile range per stack.", category = "geometry",
        modifiers = { projectile_range = 1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.arc_relay", display_name = "ARC RELAY",
        description = "Projectile pierces arc electricity. Each stack adds one chain.", category = "electrical",
        unlock = "expedition.unlock.item.arc_relay", behavior_changing = true,
        reactive_effects = {
            { id = "arc_relay", trigger = "on_pierce", conditions = { attack_tag = "projectile", requires_capability = "ability.electrical.discharge" }, effect = { kind = "chain_electricity", max_cells = 3, damage = 1 }, stack_scale = { max_cells = 2 } },
        },
    },
    {
        id = "expedition.passive.static_conduit", display_name = "STATIC CONDUIT",
        description = "Electrical hits arc once farther per stack.", category = "electrical",
        behavior_changing = true,
        reactive_effects = {
            { id = "static_conduit", trigger = "on_hit", conditions = { attack_tag = "electric" }, effect = { kind = "chain_electricity", max_cells = 2, damage = 1 }, stack_scale = { max_cells = 1 } },
        },
    },
    {
        id = "expedition.passive.kinetic_feedback", display_name = "KINETIC FEEDBACK",
        description = "Push collisions emit a Force burst. Each stack adds Force.", category = "force",
        behavior_changing = true,
        reactive_effects = {
            { id = "kinetic_feedback", trigger = "on_push_collision", effect = { kind = "kinetic_burst", radius = 1, force = 1 }, stack_scale = { force = 1 } },
        },
    },
    {
        id = "expedition.passive.shockwave_emitter", display_name = "SHOCKWAVE EMITTER",
        description = "Melee hits emit a short Force burst. Each stack adds Force.", category = "force",
        behavior_changing = true,
        reactive_effects = {
            { id = "shockwave_emitter", trigger = "on_hit", conditions = { attack_tag = "melee" }, effect = { kind = "kinetic_burst", radius = 1, force = 1 }, stack_scale = { force = 1 } },
        },
    },
    {
        id = "expedition.passive.rupture_core", display_name = "RUPTURE CORE",
        description = "Breaking an enemy component creates an explosion. Each stack adds radius.", category = "explosive",
        unlock = "expedition.unlock.item.rupture_core", behavior_changing = true,
        reactive_effects = {
            { id = "rupture_core", trigger = "on_component_break", effect = { kind = "small_explosion", radius = 1, damage = 1 }, stack_scale = { radius = 1 } },
        },
    },
    {
        id = "expedition.passive.shrapnel_core", display_name = "SHRAPNEL CORE",
        description = "Kills create a small explosion. Each stack adds radius.", category = "explosive",
        behavior_changing = true,
        reactive_effects = {
            { id = "shrapnel_core", trigger = "on_kill", effect = { kind = "small_explosion", radius = 1, damage = 1 }, stack_scale = { radius = 1 } },
        },
    },
    {
        id = "expedition.passive.spark_igniter", display_name = "SPARK IGNITER",
        description = "Projectile hits ignite around their target. Each stack expands the burst.", category = "explosive",
        behavior_changing = true,
        reactive_effects = {
            { id = "spark_igniter", trigger = "on_hit", conditions = { attack_tag = "projectile" }, effect = { kind = "ignite", radius = 0 }, stack_scale = { radius = 1 } },
        },
    },
    {
        id = "expedition.passive.overclocked_magazine", display_name = "OVERCLOCKED MAGAZINE",
        description = "+2 magazine capacity per stack.", category = "sustain",
        modifiers = { magazine_capacity = 2 }, behavior_changing = true,
    },
    {
        id = "expedition.passive.ammo_scavenger", display_name = "AMMO SCAVENGER",
        description = "Kills restore 1 reserve round per stack for your active ammo family.", category = "sustain",
        modifiers = { ammo_on_kill = 1 }, behavior_changing = true,
    },
    {
        id = "expedition.passive.field_repair", display_name = "FIELD REPAIR",
        description = "Restore 1 HP after each cleared encounter per stack.", category = "sustain",
        modifiers = { clear_heal = 1 }, behavior_changing = true,
    },
    {
        id = "expedition.passive.siege_charge", display_name = "SIEGE CHARGE",
        description = "Terrain breaks create a small explosion. Each stack adds damage.", category = "explosive",
        behavior_changing = true,
        reactive_effects = {
            { id = "siege_charge", trigger = "on_terrain_break", effect = { kind = "small_explosion", radius = 1, damage = 1 }, stack_scale = { damage = 1 } },
        },
    },
    {
        id = "expedition.passive.ability_battery", display_name = "ABILITY BATTERY",
        description = "Dash cooldown is reduced by 1 per stack, to its safe floor.", category = "mobility",
        modifiers = { dash_cooldown = -1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.brutal_edge", display_name = "BRUTAL EDGE",
        description = "+1 melee damage and +1 Force per stack.", category = "damage",
        modifiers = { melee_damage = 1, melee_force = 1 }, behavior_changing = false,
    },
    {
        id = "expedition.passive.conductive_payload", display_name = "CONDUCTIVE PAYLOAD",
        description = "Projectile hits emit an electrical arc. Each stack adds chain distance.", category = "electrical",
        behavior_changing = true,
        reactive_effects = {
            { id = "conductive_payload", trigger = "on_hit", conditions = { attack_tag = "projectile" }, effect = { kind = "chain_electricity", max_cells = 1, damage = 1 }, stack_scale = { max_cells = 1 } },
        },
    },
}

Content.ENCOUNTERS = {
    {
        id = "expedition.encounter.swarm", display_name = "SWARM", kind = "swarm",
        description = "Many weak close threats.", roles = { "rusher", "flanker" }, minimum_roles = { rusher = 2 },
        profiles = { "biome.legacy.forest", "biome.legacy.cave" },
    },
    {
        id = "expedition.encounter.crossfire", display_name = "CROSSFIRE", kind = "crossfire",
        description = "Separated ranged positions.", roles = { "ranged", "controller" }, minimum_roles = { ranged = 2 },
        profiles = { "biome.legacy.forest", "biome.legacy.dungeon", "biome.legacy.reactor" },
    },
    {
        id = "expedition.encounter.pincer", display_name = "PINCER", kind = "pincer",
        description = "Threats converge from multiple sides.", roles = { "rusher", "flanker", "ranged" }, minimum_roles = { rusher = 1, flanker = 1 },
        profiles = { "biome.legacy.cave", "biome.legacy.dungeon" },
    },
    {
        id = "expedition.encounter.hazard", display_name = "HAZARD", kind = "hazard",
        description = "Combat where water, fire or cover matters.", roles = { "controller", "ranged", "rusher" }, minimum_roles = { controller = 1 },
        profiles = { "biome.legacy.cave", "biome.legacy.reactor" }, hazard = true,
    },
    {
        id = "expedition.encounter.elite_hunt", display_name = "ELITE HUNT", kind = "elite_hunt",
        description = "A dangerous elite with limited support.", roles = { "heavy", "ranged", "rusher" }, minimum_roles = { heavy = 1 },
        profiles = { "biome.legacy.dungeon", "biome.legacy.reactor" }, elite = true,
    },
    {
        id = "expedition.encounter.reinforcement_pressure", display_name = "REINFORCEMENT PRESSURE", kind = "reinforcement_pressure",
        description = "Destroy the finite source or withstand its wave.", roles = { "rusher", "ranged", "controller" }, minimum_roles = { rusher = 1 },
        profiles = { "biome.legacy.reactor", "biome.legacy.dungeon" }, reinforcement = true,
    },
}

Content.ENEMY_COSTS = {
    rusher = 1,
    flanker = 2,
    ranged = 2,
    controller = 3,
    heavy = 4,
}

Content.ENEMY_BY_ROLE = {
    rusher = { "enemy.wild.ripper", "enemy.legacy.bomber" },
    flanker = { "enemy.wild.skirmisher", "enemy.cave.lance_acolyte" },
    ranged = { "enemy.legacy.cultist", "enemy.dungeon.scatter_gunner" },
    controller = { "enemy.cave.conductor", "enemy.reactor.arc_cutter" },
    heavy = { "enemy.dungeon.bulwark", "enemy.reactor.maintenance_heavy" },
}

function Content.character(id)
    for _, definition in ipairs(Content.CHARACTERS) do
        if definition.id == id then return definition end
    end
    return nil
end

function Content.passive(id)
    return Content.modifier_registry and Content.modifier_registry:get(id) or nil
end

function Content.encounter(id)
    for _, definition in ipairs(Content.ENCOUNTERS) do
        if definition.id == id then return definition end
    end
    return nil
end

function Content.sorted_passives()
    return Content.modifier_registry:list()
end

function Content.validate(registry)
    local loaded, failure = ModifierDefinitions.load({ registry = registry })
    assert(loaded, failure and failure.message or "Unable to load Expedition modifiers")
    Content.modifier_registry = loaded
    assert(#loaded.ordered >= 20, "Expedition requires a meaningful passive pool")
    local behavior_count, seen = 0, {}
    for _, passive in ipairs(loaded.ordered) do
        assert(type(passive.id) == "string" and not seen[passive.id], "invalid/duplicate Expedition passive")
        assert(type(passive.name) == "string" and passive.name ~= "", "passive needs display name")
        assert(#(passive.static_effects or {}) + #(passive.hooks or {}) > 0, "passive needs behavior")
        seen[passive.id] = true
        if #(passive.hooks or {}) > 0 then behavior_count = behavior_count + 1 end
    end
    assert(behavior_count >= 8, "Expedition requires eight behavior-changing passives")
    assert(#Content.ENCOUNTERS >= 6, "Expedition requires six encounter archetypes")
    for _, character in ipairs(Content.CHARACTERS) do
        assert(registry.components[character.weapon_component], "missing Expedition weapon component: " .. character.weapon_component)
        assert(registry.abilities[character.weapon_ability], "missing Expedition weapon ability: " .. character.weapon_ability)
        assert(registry.components[character.ability_component], "missing Expedition ability component: " .. character.ability_component)
        assert(registry.abilities[character.active_ability], "missing Expedition active ability: " .. character.active_ability)
        assert(character.base_hp > 0, "invalid Expedition HP")
        for key, value in pairs(character.base_modifiers or {}) do
            assert(VALID_MODIFIER_KEYS[key] and type(value) == "number", "invalid Expedition character modifier: " .. tostring(key))
        end
        for _, passive_id in ipairs(character.starting_passives or {}) do
            assert(Content.passive(passive_id), "missing Expedition starting passive: " .. passive_id)
        end
    end
    for _, encounter in ipairs(Content.ENCOUNTERS) do
        assert(encounter.minimum_roles and next(encounter.minimum_roles), "encounter requires composition constraint")
        for role in pairs(encounter.minimum_roles) do
            assert(Content.ENEMY_COSTS[role], "unknown Expedition role: " .. role)
        end
        for _, role in ipairs(encounter.roles or {}) do
            assert(Content.ENEMY_COSTS[role], "unknown Expedition encounter role: " .. role)
        end
    end
    for role, enemy_ids in pairs(Content.ENEMY_BY_ROLE) do
        assert(Content.ENEMY_COSTS[role], "enemy roster has unknown role: " .. role)
        for _, enemy_id in ipairs(enemy_ids) do assert(registry.enemies[enemy_id], "missing Expedition enemy: " .. enemy_id) end
    end
    return true
end

-- Load the serialized registry once for ordinary callers. `validate` reloads
-- against the active content registry so capability references stay checked.
Content.modifier_registry = assert(ModifierDefinitions.load())
Content.PASSIVES = Content.modifier_registry.ordered -- compatibility read-only alias; JSON is authority.

return Content
