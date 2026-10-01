-- First-party route, biome, and tier content registry.  It deliberately
-- stays separate from physical-content Registry: route data describes run
-- structure rather than world entities.
local Definitions = {}
Definitions.__index = Definitions

local function fail(message)
  error("Route content validation failed: " .. message, 3)
end

local function semantic_id(value, namespace, label)
  if type(value) ~= "string" or not value:match("^" .. namespace:gsub("([^%w])", "%%%1") .. "%.[a-z0-9_%.]+$") then
    fail(label .. " has invalid semantic ID '" .. tostring(value) .. "'")
  end
end

local function string(value, label)
  if type(value) ~= "string" or value == "" then fail(label .. " must be a non-empty string") end
end

local function integer(value, label)
  if type(value) ~= "number" or value % 1 ~= 0 or value <= 0 then fail(label .. " must be a positive integer") end
end

local function declarative(value, label, seen)
  if type(value) == "function" then fail(label .. " must be declarative data, not a function") end
  if type(value) ~= "table" then return end
  seen = seen or {}
  if seen[value] then fail(label .. " contains a cyclic table") end
  seen[value] = true
  for key, child in pairs(value) do declarative(child, label .. "." .. tostring(key), seen) end
  seen[value] = nil
end

local function index(values, namespace, label)
  if type(values) ~= "table" then fail(label .. " definitions must be a list") end
  local result, order = {}, {}
  for position, value in ipairs(values) do
    declarative(value, label .. "[" .. position .. "]")
    if type(value) ~= "table" then fail(label .. "[" .. position .. "] must be a table") end
    semantic_id(value.id, namespace, label .. "[" .. position .. "]")
    if result[value.id] then fail("Duplicate " .. label .. " ID '" .. value.id .. "'") end
    result[value.id], order[#order + 1] = value, value.id
  end
  return result, order
end

function Definitions.new(sources)
  sources = sources or {}
  local self = setmetatable({}, Definitions)
  self.biomes, self.biome_order = index(sources.biomes, "biome", "biome")
  self.tiers, self.tier_order = index(sources.tiers, "tier", "tier")
  self.profiles, self.profile_order = index(sources.profiles, "route_profile", "route profile")
  self:validate()
  return self
end

function Definitions.load()
  return Definitions.new({
    biomes = require("content.biomes.legacy"),
    tiers = require("content.tiers.legacy"),
    profiles = require("content.routes.legacy"),
  })
end

function Definitions:get_biome(id)
  local value = self.biomes[id]
  if not value then fail("Unknown biome ID '" .. tostring(id) .. "'") end
  return value
end

function Definitions:get_tier(id)
  local value = self.tiers[id]
  if not value then fail("Unknown tier ID '" .. tostring(id) .. "'") end
  return value
end

function Definitions:get_profile(id)
  local value = self.profiles[id]
  if not value then fail("Unknown route profile ID '" .. tostring(id) .. "'") end
  return value
end

function Definitions:validate()
  local tier_numbers = {}
  for _, id in ipairs(self.biome_order) do
    local biome = self.biomes[id]
    string(biome.display_name, "Biome '" .. id .. "' display_name")
    string(biome.generator, "Biome '" .. id .. "' generator")
    string(biome.terrain, "Biome '" .. id .. "' terrain")
    if biome.enemy_family ~= "wilds" and biome.enemy_family ~= "cultists" then
      fail("Biome '" .. id .. "' enemy_family must be 'wilds' or 'cultists'")
    end
  end
  for _, id in ipairs(self.tier_order) do
    local tier = self.tiers[id]
    integer(tier.number, "Tier '" .. id .. "' number")
    if tier_numbers[tier.number] then fail("Duplicate tier number '" .. tier.number .. "'") end
    tier_numbers[tier.number] = true
    if type(tier.settings) ~= "table" then fail("Tier '" .. id .. "' settings must be a table") end
    for _, field in ipairs({ "targets", "enemies", "score", "ammo", "vision", "torches" }) do
      integer(tier.settings[field], "Tier '" .. id .. "' settings." .. field)
    end
  end
  for _, id in ipairs(self.profile_order) do
    local profile = self.profiles[id]
    string(profile.display_name, "Route profile '" .. id .. "' display_name")
    if type(profile.layers) ~= "table" or #profile.layers < 3 then fail("Route profile '" .. id .. "' must define ordered layers") end
    local saw_start, saw_shop, saw_boss, normal_floors, has_branch = false, false, false, 0, false
    for layer_index, layer in ipairs(profile.layers) do
      if type(layer) ~= "table" or (layer.type ~= "floor" and layer.type ~= "shop" and layer.type ~= "boss") then
        fail("Route profile '" .. id .. "' layer " .. layer_index .. " has invalid type")
      end
      if type(layer.nodes) ~= "table" or #layer.nodes == 0 then fail("Route profile '" .. id .. "' layer " .. layer_index .. " has no nodes") end
      if layer_index == 1 then
        if layer.type ~= "floor" or #layer.nodes ~= 1 then fail("Route profile '" .. id .. "' must begin with one floor node") end
        saw_start = true
      end
      if layer.type == "floor" then normal_floors = normal_floors + 1 end
      if layer.type == "shop" then
        saw_shop = true
        if layer_index ~= #profile.layers - 1 or #layer.nodes ~= 1 then
          fail("Route profile '" .. id .. "' shop must be one node immediately before boss")
        end
      end
      if layer.type == "boss" then
        saw_boss = true
        if layer_index ~= #profile.layers then fail("Route profile '" .. id .. "' boss must be terminal") end
      end
      local keys, choices = {}, {}
      if layer.type == "floor" and #layer.nodes >= 2 then has_branch = true end
      for node_index, node in ipairs(layer.nodes) do
        if type(node) ~= "table" then fail("Route profile '" .. id .. "' layer " .. layer_index .. " node " .. node_index .. " must be a table") end
        string(node.key, "Route profile '" .. id .. "' node key")
        if keys[node.key] then fail("Route profile '" .. id .. "' duplicates node key '" .. node.key .. "'") end
        keys[node.key] = true
        if layer.type == "floor" then
          self:get_biome(node.biome_id)
          self:get_tier(node.tier_id)
          local choice = node.biome_id .. ":" .. node.tier_id
          if choices[choice] then fail("Route profile '" .. id .. "' layer " .. layer_index .. " duplicates floor choice '" .. choice .. "'") end
          choices[choice] = true
        elseif node.biome_id ~= nil or node.tier_id ~= nil then
          fail("Route profile '" .. id .. "' special node '" .. node.key .. "' cannot define biome/tier")
        end
      end
    end
    if not (saw_start and saw_shop and saw_boss and normal_floors == 3 and has_branch) then
      fail("Route profile '" .. id .. "' must define start, three floors, shop, and boss")
    end
  end
  return true
end

return Definitions
