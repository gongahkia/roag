local Registry = require("src.content.registry")

local function sources()
  return {
    abilities = require("content.abilities.legacy"),
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
      assert(registry:get_ability("ability.explosive.self_destruct").implementation == "enemy_self_destruct")
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
        implementation = "enemy_self_destruct",
        callback = function() end,
      }
      assert_failure("must be declarative data, not a function", function()
        Registry.new(invalid_sources)
      end)
    end,
  },
}
