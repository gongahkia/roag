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

function Definitions:biome_supports_tier(biome_id, tier_id)
  local biome = self:get_biome(biome_id)
  for _, supported in ipairs(biome.supported_tier_ids or self.tier_order) do
    if supported == tier_id then return true end
  end
  return false
end

function Definitions:validate()
  local tier_numbers = {}
  for _, id in ipairs(self.biome_order) do
    local biome = self.biomes[id]
    string(biome.display_name, "Biome '" .. id .. "' display_name")
    string(biome.generator, "Biome '" .. id .. "' generator")
    string(biome.terrain, "Biome '" .. id .. "' terrain")
    if biome.enemy_family ~= "wilds" and biome.enemy_family ~= "cultists" and biome.enemy_family ~= "industrial" then
      fail("Biome '" .. id .. "' enemy_family must be 'wilds', 'cultists', or 'industrial'")
    end
    if biome.room_corpus_id ~= nil then
      semantic_id(biome.room_corpus_id, "room_corpus", "Biome '" .. id .. "' room_corpus_id")
    end
    if biome.supported_tier_ids ~= nil then
      if type(biome.supported_tier_ids) ~= "table" or #biome.supported_tier_ids == 0 then
        fail("Biome '" .. id .. "' supported_tier_ids must be a non-empty list")
      end
      local seen_tiers = {}
      for _, tier_id in ipairs(biome.supported_tier_ids) do
        self:get_tier(tier_id)
        if seen_tiers[tier_id] then fail("Biome '" .. id .. "' repeats supported tier '" .. tier_id .. "'") end
        seen_tiers[tier_id] = true
      end
    end
  end
  for _, id in ipairs(self.tier_order) do
    local tier = self.tiers[id]
    integer(tier.number, "Tier '" .. id .. "' number")
    if tier_numbers[tier.number] then fail("Duplicate tier number '" .. tier.number .. "'") end
    tier_numbers[tier.number] = true
    if type(tier.settings) ~= "table" then fail("Tier '" .. id .. "' settings must be a table") end
    -- Pre-8B tooling fixtures used the overloaded score spelling. Retain it
    -- only as an input alias; all runtime settings use objective_required.
    if tier.settings.objective_required == nil then tier.settings.objective_required = tier.settings.score end
    for _, field in ipairs({ "targets", "enemies", "objective_required", "ammo", "vision", "torches" }) do
      integer(tier.settings[field], "Tier '" .. id .. "' settings." .. field)
    end
  end
  for _, id in ipairs(self.profile_order) do
    local profile = self.profiles[id]
    string(profile.display_name, "Route profile '" .. id .. "' display_name")
    if type(profile.layers) ~= "table" or #profile.layers < 3 then fail("Route profile '" .. id .. "' must define ordered layers") end
    local saw_start, saw_shop, terminal_boss, normal_floors, has_branch, boss_count = false, false, false, 0, false, 0
    local node_keys = {}
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
        boss_count = boss_count + #layer.nodes
        if layer_index == #profile.layers then terminal_boss = true end
      end
      local keys, choices = {}, {}
      if layer.type == "floor" and #layer.nodes >= 2 then has_branch = true end
      for node_index, node in ipairs(layer.nodes) do
        if type(node) ~= "table" then fail("Route profile '" .. id .. "' layer " .. layer_index .. " node " .. node_index .. " must be a table") end
        string(node.key, "Route profile '" .. id .. "' node key")
        if keys[node.key] then fail("Route profile '" .. id .. "' duplicates node key '" .. node.key .. "'") end
        keys[node.key] = true
        node_keys[node.key] = { depth = layer_index, type = layer.type }
        if layer.type == "floor" then
          self:get_biome(node.biome_id)
          self:get_tier(node.tier_id)
          if not self:biome_supports_tier(node.biome_id, node.tier_id) then
            fail("Route profile '" .. id .. "' uses unsupported biome/tier pair '" .. node.biome_id .. "' / '" .. node.tier_id .. "'")
          end
          semantic_id(node.service_id, "service", "Route profile '" .. id .. "' service_id")
          local choice = node.biome_id .. ":" .. node.tier_id
          if choices[choice] then fail("Route profile '" .. id .. "' layer " .. layer_index .. " duplicates floor choice '" .. choice .. "'") end
          choices[choice] = true
        else
          if node.biome_id ~= nil or node.tier_id ~= nil then
            fail("Route profile '" .. id .. "' special node '" .. node.key .. "' cannot define biome/tier")
          end
          if layer.type == "boss" then
            semantic_id(node.boss_id, "boss", "Route profile '" .. id .. "' boss node '" .. node.key .. "' boss_id")
          elseif node.boss_id ~= nil then
            fail("Route profile '" .. id .. "' non-boss node '" .. node.key .. "' cannot define boss_id")
          end
        end
      end
    end
    local edge_seen = {}
    for _, edge in ipairs(profile.edges or {}) do
      if type(edge) ~= "table" or type(edge.from) ~= "string" or type(edge.to) ~= "string"
        or not node_keys[edge.from] or not node_keys[edge.to] or node_keys[edge.from].depth + 1 ~= node_keys[edge.to].depth then
        fail("Route profile '" .. id .. "' has invalid explicit edge")
      end
      if edge.requires_unlock ~= nil and (type(edge.requires_unlock) ~= "string" or not edge.requires_unlock:match("^unlock%.[a-z0-9_%.]+$")) then
        fail("Route profile '" .. id .. "' edge has invalid requires_unlock")
      end
      local edge_key = edge.from .. ">" .. edge.to
      if edge_seen[edge_key] then fail("Route profile '" .. id .. "' duplicates explicit edge '" .. edge_key .. "'") end
      edge_seen[edge_key] = true
    end
    -- Two first-milestone variants and two late-milestone variants converge
    -- into one final boss. Every new playable path therefore contains three
    -- bosses even though the complete route DAG contains five boss nodes.
    if not (saw_start and saw_shop and terminal_boss and normal_floors == 3 and boss_count == 5 and has_branch) then
      fail("Route profile '" .. id .. "' must define start, three floors, two milestone layers, shop, and one final boss")
    end
  end
  return true
end

return Definitions
