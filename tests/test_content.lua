local Registry = require("src.content.registry")

local function sources()
  return {
    abilities = require("content.abilities.legacy"),
    materials = require("content.materials.legacy"),
    liquids = require("content.liquids.legacy"),
    gases = require("content.gases.legacy"),
    world_objects = require("content.world_objects.legacy"),
    hazards = require("content.hazards.legacy"),
    components = require("content.components.legacy"),
    topologies = require("content.body_topologies.normal"),
    actors = require("content.actors.player_legacy"),
    factions = require("content.factions.legacy"),
    enemies = require("content.enemies.legacy"),
  }
end

local function copy_list(values)
  local result = {}
  for index, value in ipairs(values) do
    result[index] = value
  end
  return result
end

local function copy_table(values)
  local result = {}
  for key, value in pairs(values) do
    result[key] = value
  end
  return result
end

local function assert_failure(expected, callback)
  local ok, err = pcall(callback)
  assert(not ok, "Expected content validation to fail")
  assert(tostring(err):find(expected, 1, true), tostring(err))
end

return {
  {
    name = "semantic content definitions load and resolve by ID",
    run = function()
      local registry = Registry.load()
      assert(registry:get_component("component.internal.legacy_volatile_charge").display_name == "Volatile Charge")
      assert(registry:get_enemy("enemy.legacy.bomber").body_topology_id == "body.topology.normal")
      assert(registry:get_ability("ability.explosive.self_destruct").implementation == "self_destruct")
      assert(registry:get_ability("ability.locomotion.move").implementation == "locomotion")
      assert(registry:get_component("component.leg.legacy_locomotor").abilities[1] == "ability.locomotion.move")
      assert(registry:get_material("material.terrain.brush").max_integrity == 2)
      assert(registry:get_material("material.terrain.brush").flammable)
      assert(not registry:get_material("material.terrain.brush").conductive)
      assert(registry:get_material("material.structure.conductive_metal").conductive)
      assert(registry:get_material("material.structure.wood").burn_rate == 1)
      assert(registry:get_liquid("liquid.water.legacy").max_depth == 3)
      assert(registry:get_liquid("liquid.water.legacy").conductive)
      assert(registry:get_gas("gas.toxic.legacy").exposure_threshold == 2)
      assert(registry:get_world_object("world_object.cover.timber_crate").material_id == "material.structure.wood")
      assert(registry:get_world_object("world_object.cover.conductive_metal_crate").material_id == "material.structure.conductive_metal")
      assert(registry:get_component("component.internal.legacy_shock_coil").abilities[1] == "ability.electrical.discharge")
      assert(registry:get_hazard("hazard.legacy.spike_field").effect.type == "kinetic_damage")
    end,
  },
  {
    name = "gas definitions validate concentration exposure damage and semantic IDs",
    run = function()
      local invalid_maximum = sources()
      invalid_maximum.gases = copy_list(invalid_maximum.gases)
      invalid_maximum.gases[#invalid_maximum.gases + 1] = {
        id = "gas.invalid.maximum",
        display_name = "Invalid Maximum",
        max_concentration = 0,
        exposure_threshold = 1,
        damage = 1,
        render_style = "toxic_fumes",
      }
      assert_failure("Gas 'gas.invalid.maximum' max_concentration must be a positive integer", function()
        Registry.new(invalid_maximum)
      end)

      local invalid_threshold = sources()
      invalid_threshold.gases = copy_list(invalid_threshold.gases)
      invalid_threshold.gases[#invalid_threshold.gases + 1] = {
        id = "gas.invalid.threshold",
        display_name = "Invalid Threshold",
        max_concentration = 2,
        exposure_threshold = 3,
        damage = 1,
        render_style = "toxic_fumes",
      }
      assert_failure("Gas 'gas.invalid.threshold' exposure_threshold cannot exceed max_concentration", function()
        Registry.new(invalid_threshold)
      end)

      local invalid_damage = sources()
      invalid_damage.gases = copy_list(invalid_damage.gases)
      invalid_damage.gases[#invalid_damage.gases + 1] = {
        id = "gas.invalid.damage",
        display_name = "Invalid Damage",
        max_concentration = 2,
        exposure_threshold = 1,
        damage = -1,
        render_style = "toxic_fumes",
      }
      assert_failure("Gas 'gas.invalid.damage' damage must be a non-negative number", function()
        Registry.new(invalid_damage)
      end)

      local duplicate = sources()
      duplicate.gases = copy_list(duplicate.gases)
      duplicate.gases[#duplicate.gases + 1] = duplicate.gases[1]
      assert_failure("Duplicate gas ID 'gas.toxic.legacy'", function()
        Registry.new(duplicate)
      end)
    end,
  },
  {
    name = "liquid definitions validate discrete depth extinguishing metadata and semantic IDs",
    run = function()
      local invalid_depth = sources()
      invalid_depth.liquids = copy_list(invalid_depth.liquids)
      invalid_depth.liquids[#invalid_depth.liquids + 1] = {
        id = "liquid.invalid.depth",
        display_name = "Invalid Depth",
        max_depth = 0,
        extinguishes_fire = true,
        conductive = true,
        render_style = "water",
      }
      assert_failure("Liquid 'liquid.invalid.depth' max_depth must be a positive integer", function()
        Registry.new(invalid_depth)
      end)

      local invalid_extinguish = sources()
      invalid_extinguish.liquids = copy_list(invalid_extinguish.liquids)
      invalid_extinguish.liquids[#invalid_extinguish.liquids + 1] = {
        id = "liquid.invalid.extinguish",
        display_name = "Invalid Extinguishing",
        max_depth = 1,
        extinguishes_fire = "yes",
        conductive = true,
        render_style = "water",
      }
      assert_failure("Liquid 'liquid.invalid.extinguish' extinguishes_fire must be a boolean", function()
        Registry.new(invalid_extinguish)
      end)

      local invalid_conductivity = sources()
      invalid_conductivity.liquids = copy_list(invalid_conductivity.liquids)
      invalid_conductivity.liquids[#invalid_conductivity.liquids + 1] = {
        id = "liquid.invalid.conductivity",
        display_name = "Invalid Conductivity",
        max_depth = 1,
        extinguishes_fire = true,
        conductive = "yes",
        render_style = "water",
      }
      assert_failure("Liquid 'liquid.invalid.conductivity' conductive must be a boolean", function()
        Registry.new(invalid_conductivity)
      end)

      local duplicate = sources()
      duplicate.liquids = copy_list(duplicate.liquids)
      duplicate.liquids[#duplicate.liquids + 1] = duplicate.liquids[1]
      assert_failure("Duplicate liquid ID 'liquid.water.legacy'", function()
        Registry.new(duplicate)
      end)
    end,
  },
  {
    name = "material flammability metadata validates explicit burn behavior",
    run = function()
      local missing_rate = sources()
      missing_rate.materials = copy_list(missing_rate.materials)
      missing_rate.materials[#missing_rate.materials + 1] = {
        id = "material.invalid.missing_burn_rate",
        display_name = "Missing Burn Rate",
        solid = true,
        blocks_movement = true,
        blocks_vision = true,
        destructible = true,
        max_integrity = 1,
        destruction_material_id = "material.terrain.air",
        flammable = true,
        conductive = false,
      }
      assert_failure("Material 'material.invalid.missing_burn_rate' burn_rate must be a positive number", function()
        Registry.new(missing_rate)
      end)

      local invalid_nonflammable = sources()
      invalid_nonflammable.materials = copy_list(invalid_nonflammable.materials)
      invalid_nonflammable.materials[#invalid_nonflammable.materials + 1] = {
        id = "material.invalid.nonflammable_rate",
        display_name = "Invalid Burn Rate",
        solid = true,
        blocks_movement = true,
        blocks_vision = true,
        destructible = true,
        max_integrity = 1,
        destruction_material_id = "material.terrain.air",
        flammable = false,
        burn_rate = 1,
        conductive = false,
      }
      assert_failure("Nonflammable material 'material.invalid.nonflammable_rate' cannot define burn_rate", function()
        Registry.new(invalid_nonflammable)
      end)

      local invalid_conductivity = sources()
      invalid_conductivity.materials = copy_list(invalid_conductivity.materials)
      invalid_conductivity.materials[#invalid_conductivity.materials + 1] = {
        id = "material.invalid.conductivity",
        display_name = "Invalid Conductivity",
        solid = true,
        blocks_movement = true,
        blocks_vision = true,
        destructible = true,
        max_integrity = 1,
        destruction_material_id = "material.terrain.air",
        flammable = false,
        conductive = "yes",
      }
      assert_failure("Material 'material.invalid.conductivity' conductive must be a boolean", function()
        Registry.new(invalid_conductivity)
      end)
    end,
  },
  {
    name = "hazard definitions validate declarative trigger and kinetic effect metadata",
    run = function()
      local invalid_trigger = sources()
      invalid_trigger.hazards = copy_list(invalid_trigger.hazards)
      invalid_trigger.hazards[#invalid_trigger.hazards + 1] = {
        id = "hazard.invalid.trigger",
        display_name = "Invalid Trigger",
        trigger = "periodic",
        effect = { type = "kinetic_damage", amount = 1 },
        render_style = "spikes",
      }
      assert_failure("Hazard 'hazard.invalid.trigger' trigger must be 'on_enter'", function()
        Registry.new(invalid_trigger)
      end)

      local invalid_effect = sources()
      invalid_effect.hazards = copy_list(invalid_effect.hazards)
      invalid_effect.hazards[#invalid_effect.hazards + 1] = {
        id = "hazard.invalid.effect",
        display_name = "Invalid Effect",
        trigger = "on_enter",
        effect = { type = "fire_damage", amount = 1 },
        render_style = "spikes",
      }
      assert_failure("Hazard 'hazard.invalid.effect' effect.type must be 'kinetic_damage'", function()
        Registry.new(invalid_effect)
      end)

      local invalid_amount = sources()
      invalid_amount.hazards = copy_list(invalid_amount.hazards)
      invalid_amount.hazards[#invalid_amount.hazards + 1] = {
        id = "hazard.invalid.amount",
        display_name = "Invalid Amount",
        trigger = "on_enter",
        effect = { type = "kinetic_damage", amount = 0 },
        render_style = "spikes",
      }
      assert_failure("Hazard 'hazard.invalid.amount' effect.amount must be a positive integer", function()
        Registry.new(invalid_amount)
      end)

      local duplicate = sources()
      duplicate.hazards = copy_list(duplicate.hazards)
      duplicate.hazards[#duplicate.hazards + 1] = duplicate.hazards[1]
      assert_failure("Duplicate hazard ID 'hazard.legacy.spike_field'", function()
        Registry.new(duplicate)
      end)
    end,
  },
  {
    name = "world object definitions validate material and physical metadata",
    run = function()
      local invalid_sources = sources()
      invalid_sources.world_objects = copy_list(invalid_sources.world_objects)
      invalid_sources.world_objects[#invalid_sources.world_objects + 1] = {
        id = "world_object.invalid.cover",
        display_name = "Invalid Cover",
        material_id = "material.missing.cover",
        blocks_movement = true,
        blocks_vision = true,
        blocks_projectiles = true,
        movable_by_force = false,
        render_style = "cover",
      }
      assert_failure("Unknown material ID 'material.missing.cover'", function()
        Registry.new(invalid_sources)
      end)

      local invalid_metadata = sources()
      invalid_metadata.world_objects = copy_list(invalid_metadata.world_objects)
      invalid_metadata.world_objects[#invalid_metadata.world_objects + 1] = {
        id = "world_object.invalid.metadata",
        display_name = "Invalid Metadata",
        material_id = "material.structure.wood",
        blocks_movement = "yes",
        blocks_vision = true,
        blocks_projectiles = true,
        movable_by_force = false,
        render_style = "cover",
      }
      assert_failure("World object 'world_object.invalid.metadata' blocks_movement must be a boolean", function()
        Registry.new(invalid_metadata)
      end)

      local invalid_gas_blocking = sources()
      invalid_gas_blocking.world_objects = copy_list(invalid_gas_blocking.world_objects)
      invalid_gas_blocking.world_objects[#invalid_gas_blocking.world_objects + 1] = {
        id = "world_object.invalid.gas_blocking",
        display_name = "Invalid Gas Blocking",
        material_id = "material.structure.wood",
        blocks_movement = true,
        blocks_vision = true,
        blocks_projectiles = true,
        blocks_gas = "sealed",
        movable_by_force = false,
        render_style = "cover",
      }
      assert_failure("World object 'world_object.invalid.gas_blocking' blocks_gas must be a boolean", function()
        Registry.new(invalid_gas_blocking)
      end)
    end,
  },
  {
    name = "interactive device definitions validate declarative roles and powered-door metadata",
    run = function()
      local unknown_role = sources()
      unknown_role.world_objects = copy_list(unknown_role.world_objects)
      local invalid = copy_table(unknown_role.world_objects[1])
      invalid.id = "world_object.invalid.unknown_role"
      invalid.interaction_role = "terminal"
      unknown_role.world_objects[#unknown_role.world_objects + 1] = invalid
      assert_failure("World object 'world_object.invalid.unknown_role' has unknown interaction_role 'terminal'", function()
        Registry.new(unknown_role)
      end)

      local invalid_door = sources()
      invalid_door.world_objects = copy_list(invalid_door.world_objects)
      local door = copy_table(require("content.world_objects.legacy")[4])
      door.id = "world_object.invalid.powered_door"
      door.power_required = nil
      invalid_door.world_objects[#invalid_door.world_objects + 1] = door
      assert_failure("World object 'world_object.invalid.powered_door' power_required must be a boolean", function()
        Registry.new(invalid_door)
      end)
    end,
  },
  {
    name = "material semantic IDs and destruction references validate",
    run = function()
      local duplicate_sources = sources()
      duplicate_sources.materials = copy_list(duplicate_sources.materials)
      duplicate_sources.materials[#duplicate_sources.materials + 1] = duplicate_sources.materials[1]
      assert_failure("Duplicate material ID 'material.terrain.air'", function()
        Registry.new(duplicate_sources)
      end)

      local invalid_durability = sources()
      invalid_durability.materials = copy_list(invalid_durability.materials)
      invalid_durability.materials[#invalid_durability.materials + 1] = {
        id = "material.invalid.zero_durability",
        display_name = "Invalid Material",
        solid = true,
        blocks_movement = true,
        blocks_vision = true,
        destructible = true,
        max_integrity = 0,
        destruction_material_id = "material.terrain.air",
        flammable = false,
        conductive = false,
      }
      assert_failure("Material 'material.invalid.zero_durability' max_integrity must be a positive number", function()
        Registry.new(invalid_durability)
      end)

      local invalid_reference = sources()
      invalid_reference.materials = copy_list(invalid_reference.materials)
      invalid_reference.materials[#invalid_reference.materials + 1] = {
        id = "material.invalid.destroyed_into_void",
        display_name = "Invalid Material Reference",
        solid = true,
        blocks_movement = true,
        blocks_vision = true,
        destructible = true,
        max_integrity = 1,
        destruction_material_id = "material.missing.void",
        flammable = false,
        conductive = false,
      }
      assert_failure("Unknown material ID 'material.missing.void'", function()
        Registry.new(invalid_reference)
      end)
    end,
  },
  {
    name = "duplicate semantic IDs fail validation",
    run = function()
      local duplicate_sources = sources()
      duplicate_sources.components = copy_list(duplicate_sources.components)
      duplicate_sources.components[#duplicate_sources.components + 1] = duplicate_sources.components[1]
      assert_failure("Duplicate component ID 'component.head.legacy_optic'", function()
        Registry.new(duplicate_sources)
      end)
    end,
  },
  {
    name = "missing content references fail validation with their semantic ID",
    run = function()
      local invalid_sources = sources()
      invalid_sources.components = copy_list(invalid_sources.components)
      invalid_sources.components[#invalid_sources.components + 1] = {
        id = "component.internal.invalid_reference",
        display_name = "Invalid Reference",
        compatible_slots = { "internal" },
        max_integrity = 1,
        mass = 1,
        wear_per_use = 0,
        inventory = { width = 1, height = 1, rotatable = false },
        abilities = { "ability.missing.nope" },
      }
      assert_failure("Unknown ability ID 'ability.missing.nope'", function()
        Registry.new(invalid_sources)
      end)
    end,
  },
  {
    name = "content definitions reject executable callbacks",
    run = function()
      local invalid_sources = sources()
      invalid_sources.abilities = copy_list(invalid_sources.abilities)
      invalid_sources.abilities[#invalid_sources.abilities + 1] = {
        id = "ability.invalid.callback",
        display_name = "Invalid Callback",
        implementation = "self_destruct",
        callback = function() end,
      }
      assert_failure("must be declarative data, not a function", function()
        Registry.new(invalid_sources)
      end)
    end,
  },
  {
    name = "negative usage wear fails validation",
    run = function()
      local invalid_sources = sources()
      invalid_sources.components = copy_list(invalid_sources.components)
      invalid_sources.components[#invalid_sources.components + 1] = {
        id = "component.internal.invalid_wear",
        display_name = "Invalid Wear",
        compatible_slots = { "internal" },
        max_integrity = 1,
        mass = 1,
        wear_per_use = -1,
        inventory = { width = 1, height = 1, rotatable = false },
        abilities = {},
      }
      assert_failure("wear_per_use must be a non-negative number", function()
        Registry.new(invalid_sources)
      end)
    end,
  },
  {
    name = "invalid inventory footprint fails validation",
    run = function()
      local invalid_sources = sources()
      invalid_sources.components = copy_list(invalid_sources.components)
      invalid_sources.components[#invalid_sources.components + 1] = {
        id = "component.internal.invalid_footprint",
        display_name = "Invalid Footprint",
        compatible_slots = { "internal" },
        max_integrity = 1,
        mass = 1,
        wear_per_use = 0,
        inventory = { width = 0, height = 1, rotatable = false },
        abilities = {},
      }
      assert_failure("inventory.width must be a positive integer", function()
        Registry.new(invalid_sources)
      end)
    end,
  },
  {
    name = "invalid ability resource metadata fails validation",
    run = function()
      local invalid_sources = sources()
      invalid_sources.abilities = copy_list(invalid_sources.abilities)
      invalid_sources.abilities[#invalid_sources.abilities + 1] = {
        id = "ability.invalid.resource",
        display_name = "Invalid Resource",
        implementation = "projectile",
        resource = { name = "ammo", amount = 0 },
      }
      assert_failure("resource.amount must be a positive integer", function()
        Registry.new(invalid_sources)
      end)
    end,
  },
}
