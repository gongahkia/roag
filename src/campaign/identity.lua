-- Durable identity allocation.  Campaign-owned allocation is monotonic;
-- zone-owned allocation is deterministic and isolated by canonical ZoneKey.
-- No lazily visited zone consumes a global counter.
local ZoneKey = require("src.campaign.zone_key")

local Identity = {}
Identity.__index = Identity

local function positive(value, label)
  assert(type(value) == "number" and value >= 1 and value % 1 == 0, label .. " must be a positive integer")
  return value
end

local function campaign_id(value)
  assert(type(value) == "string" and value:match("^campaign:%d+$"), "Campaign ID is invalid")
  return value
end

local function zone_state(data)
  data = data or {}
  return {
    next_component_sequence = positive(data.next_component_sequence or 1, "Zone component sequence"),
    next_actor_sequence = positive(data.next_actor_sequence or 1, "Zone actor sequence"),
    next_corpse_sequence = positive(data.next_corpse_sequence or 1, "Zone corpse sequence"),
    next_world_object_sequence = positive(data.next_world_object_sequence or 1, "Zone object sequence"),
    next_hazard_sequence = positive(data.next_hazard_sequence or 1, "Zone hazard sequence"),
    next_fire_sequence = positive(data.next_fire_sequence or 1, "Zone fire sequence"),
    next_item_sequence = positive(data.next_item_sequence or 1, "Zone item sequence"),
  }
end

function Identity.new(campaign, key, campaign_state, local_state)
  campaign_id(campaign)
  ZoneKey.validate(key)
  campaign_state = campaign_state or {}
  campaign_state.next_component_sequence = positive(campaign_state.next_component_sequence or 1, "Campaign component sequence")
  campaign_state.next_actor_sequence = positive(campaign_state.next_actor_sequence or 1, "Campaign actor sequence")
  campaign_state.next_item_sequence = positive(campaign_state.next_item_sequence or 1, "Campaign item sequence")
  return setmetatable({
    campaign_id = campaign,
    key = ZoneKey.from_data(key),
    campaign_state = campaign_state,
    zone_state = zone_state(local_state),
  }, Identity)
end

function Identity.campaign_state_data(state)
  state = state or {}
  return {
    next_component_sequence = positive(state.next_component_sequence or 1, "Campaign component sequence"),
    next_actor_sequence = positive(state.next_actor_sequence or 1, "Campaign actor sequence"),
    next_item_sequence = positive(state.next_item_sequence or 1, "Campaign item sequence"),
  }
end

function Identity:zone_state_data()
  return zone_state(self.zone_state)
end

function Identity:_zone_prefix(kind)
  return string.format("%s:zone:%d:%d:%d", self.campaign_id, self.key.world_x, self.key.world_y, self.key.z)
end

local function next_counter(state, name)
  local value = state[name]
  state[name] = value + 1
  return value
end

function Identity:allocate_component(scope)
  if scope == "campaign" then
    return string.format("cmp:%s:campaign:%06d", self.campaign_id, next_counter(self.campaign_state, "next_component_sequence"))
  end
  return string.format("cmp:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_component_sequence"))
end

function Identity:allocate_actor(scope)
  if scope == "campaign" then
    return string.format("actor:%s:campaign:%06d", self.campaign_id, next_counter(self.campaign_state, "next_actor_sequence"))
  end
  return string.format("actor:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_actor_sequence"))
end

function Identity:allocate_corpse_id()
  return string.format("corpse:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_corpse_sequence"))
end

function Identity:allocate_world_object_id()
  return string.format("object:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_world_object_sequence"))
end

function Identity:allocate_hazard_id()
  return string.format("hazard:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_hazard_sequence"))
end

function Identity:allocate_fire_id()
  return string.format("fire:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_fire_sequence"))
end

function Identity:allocate_item_id(scope)
  if scope == "campaign" then
    return string.format("item:%s:campaign:%06d", self.campaign_id, next_counter(self.campaign_state, "next_item_sequence"))
  end
  return string.format("item:%s:%06d", self:_zone_prefix(), next_counter(self.zone_state, "next_item_sequence"))
end

function Identity.is_component_id(value)
  return type(value) == "string" and (value:match("^component:%d+$") ~= nil or value:match("^cmp:campaign:%d+:(campaign|zone:%-?%d+:%-?%d+:%-?%d+):%d+$") ~= nil)
end

function Identity.is_actor_id(value)
  return type(value) == "string" and (value:match("^actor:legacy:%d+$") ~= nil
    or value:match("^actor:campaign:%d+:(campaign|zone:%-?%d+:%-?%d+:%-?%d+):%d+$") ~= nil)
end

function Identity.is_item_id(value)
  return type(value) == "string" and (value:match("^item:%d+$") ~= nil
    or value:match("^item:campaign:%d+:(campaign|zone:%-?%d+:%-?%d+:%-?%d+):%d+$") ~= nil)
end

function Identity.is_campaign_id(value)
  return type(value) == "string" and value:match("^campaign:%d+$") ~= nil
end

return Identity
