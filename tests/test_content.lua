local Registry = require("src.content.registry")

local function sources()
  return {
    abilities = require("content.abilities.legacy"),
    materials = require("content.materials.legacy"),
    world_objects = require("content.world_objects.legacy"),
    components = require("content.components.legacy"),
    topologies = require("content.body_topologies.normal"),
    actors = require("content.actors.player_legacy"),
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
      assert(registry:get_world_object("world_object.cover.timber_crate").material_id == "material.structure.wood")
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
