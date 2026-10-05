-- Separate versioned account progression.  It deliberately contains no
-- current-run body, world, route, SCRAP, or presentation state.
local Json = require("src.persistence.json")

local MetaProfile = {
  FORMAT = "roag.meta_profile",
  VERSION = 1,
}

-- Account-level Expedition unlocks deliberately unlock possibilities rather
-- than permanent numerical power.  Keep their additive data separate from
-- Campaign research and normalize it into old profiles on load.
MetaProfile.DEFAULT_EXPEDITION_UNLOCK_IDS = {
  "expedition.unlock.character.bruiser",
  "expedition.unlock.character.gunner",
}

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function sorted_unique(values, label)
  local seen, result = {}, {}
  for _, value in ipairs(values or {}) do
    assert(type(value) == "string" and value ~= "", label .. " must contain non-empty strings")
    assert(not seen[value], label .. " contains duplicate '" .. value .. "'")
    seen[value] = true
    result[#result + 1] = value
  end
  table.sort(result)
  return result, seen
end

local function copy_map(values)
  local result = {}
  for key, value in pairs(values or {}) do result[key] = value end
  return result
end

function MetaProfile.new()
  return { research_data = 0, unlocked_research_ids = {}, next_run_sequence = 1, next_campaign_sequence = 1,
    claimed_reward_ids = {}, discovered_discovery_ids = {},
    expedition_unlock_ids = copy_map((function()
      local values = {}
      for _, id in ipairs(MetaProfile.DEFAULT_EXPEDITION_UNLOCK_IDS) do values[#values + 1] = id end
      return values
    end)()) }
end

function MetaProfile.copy(profile)
  return {
    research_data = profile.research_data,
    unlocked_research_ids = sorted_unique(profile.unlocked_research_ids, "unlocked_research_ids"),
    next_run_sequence = profile.next_run_sequence,
    next_campaign_sequence = profile.next_campaign_sequence or 1,
    claimed_reward_ids = sorted_unique(profile.claimed_reward_ids, "claimed_reward_ids"),
    discovered_discovery_ids = sorted_unique(profile.discovered_discovery_ids, "discovered_discovery_ids"),
    expedition_unlock_ids = sorted_unique(profile.expedition_unlock_ids or MetaProfile.DEFAULT_EXPEDITION_UNLOCK_IDS,
      "expedition_unlock_ids"),
  }
end

function MetaProfile.validate(profile, registry)
  assert(type(profile) == "table", "Meta profile must be a table")
  assert(type(profile.research_data) == "number" and profile.research_data >= 0 and profile.research_data % 1 == 0,
    "Meta profile research_data must be a non-negative integer")
  assert(type(profile.next_run_sequence) == "number" and profile.next_run_sequence >= 1 and profile.next_run_sequence % 1 == 0,
    "Meta profile next_run_sequence must be a positive integer")
  -- Optional in pre-OW-01 profiles. Normalizing here keeps old account
  -- research/discovery history intact while giving campaigns a durable ID.
  if profile.next_campaign_sequence == nil then profile.next_campaign_sequence = 1 end
  assert(type(profile.next_campaign_sequence) == "number" and profile.next_campaign_sequence >= 1
    and profile.next_campaign_sequence % 1 == 0, "Meta profile next_campaign_sequence must be a positive integer")
  local unlocked, unlocked_set = sorted_unique(profile.unlocked_research_ids, "Meta profile unlocked research IDs")
  local claims = sorted_unique(profile.claimed_reward_ids, "Meta profile claimed reward IDs")
  -- Discovery history deliberately tolerates semantic IDs no longer present
  -- in current content. Account history is more valuable than a strict
  -- content prune, and future content can safely reintroduce an ID.
  local discoveries = sorted_unique(profile.discovered_discovery_ids, "Meta profile discovered discovery IDs")
  local expedition_unlocks = sorted_unique(profile.expedition_unlock_ids or MetaProfile.DEFAULT_EXPEDITION_UNLOCK_IDS,
    "Meta profile Expedition unlock IDs")
  local expedition_set = {}
  for _, id in ipairs(expedition_unlocks) do
    assert(id:match("^expedition%.unlock%.[a-z0-9_%.%-]+$"), "Meta profile has malformed Expedition unlock ID '" .. id .. "'")
    expedition_set[id] = true
  end
  -- A profile predating Expedition always starts with the two prototype
  -- characters. Do not make historic accounts earn their basic controls.
  for _, id in ipairs(MetaProfile.DEFAULT_EXPEDITION_UNLOCK_IDS) do
    if not expedition_set[id] then expedition_unlocks[#expedition_unlocks + 1] = id end
  end
  table.sort(expedition_unlocks)
  for _, id in ipairs(unlocked) do
    assert(registry.research[id], "Meta profile references unknown research ID '" .. id .. "'")
    for _, prerequisite in ipairs(registry:get_research(id).prerequisites or {}) do
      assert(unlocked_set[prerequisite], "Meta profile research '" .. id .. "' is missing prerequisite '" .. prerequisite .. "'")
    end
  end
  for _, id in ipairs(claims) do
    assert(id:match("^[%w:_%.%-]+$"), "Meta profile has malformed claimed reward ID '" .. id .. "'")
  end
  for _, id in ipairs(discoveries) do
    assert(id:match("^discovery%.[a-z0-9_%.]+$"), "Meta profile has malformed discovery ID '" .. id .. "'")
  end
  profile.unlocked_research_ids, profile.claimed_reward_ids, profile.discovered_discovery_ids = unlocked, claims, discoveries
  profile.expedition_unlock_ids = expedition_unlocks
  return true
end

function MetaProfile.to_data(profile, registry)
  MetaProfile.validate(profile, registry)
  return MetaProfile.copy(profile)
end

function MetaProfile.encode(profile, registry)
  local text, reason = Json.encode({ format = MetaProfile.FORMAT, version = MetaProfile.VERSION, profile = MetaProfile.to_data(profile, registry) })
  if not text then return failure("encode_failed", tostring(reason)) end
  return text
end

function MetaProfile.decode(text, registry)
  local envelope, reason = Json.decode(text)
  if not envelope then return failure("invalid_json", tostring(reason)) end
  if type(envelope) ~= "table" then return failure("invalid_state", "Meta profile envelope must be an object") end
  if envelope.format ~= MetaProfile.FORMAT then return failure("unsupported_format", "Save format is not roag.meta_profile") end
  if envelope.version ~= MetaProfile.VERSION then return failure("unsupported_version", "Meta profile version is not supported") end
  if type(envelope.profile) ~= "table" then return failure("invalid_state", "Meta profile envelope is missing profile data") end
  local ok, validation = pcall(MetaProfile.validate, envelope.profile, registry)
  if not ok then return failure("invalid_state", tostring(validation)) end
  return MetaProfile.copy(envelope.profile)
end

function MetaProfile.load(store, registry)
  local text, error_data = store:read()
  if not text and error_data and error_data.code == "missing_file" then return MetaProfile.new(), { fresh = true } end
  if not text then return nil, error_data end
  return MetaProfile.decode(text, registry)
end

function MetaProfile.save(profile, store, registry)
  local text, error_data = MetaProfile.encode(profile, registry)
  if not text then return nil, error_data end
  local written, write_error = store:write(text)
  if not written then return nil, write_error or { code = "write_failed", reason = "Could not write meta profile" } end
  return true
end

function MetaProfile.has_research(profile, id)
  for _, unlocked in ipairs(profile.unlocked_research_ids or {}) do if unlocked == id then return true end end
  return false
end

function MetaProfile.allocate_run(profile)
  local id = string.format("run:%06d", profile.next_run_sequence)
  profile.next_run_sequence = profile.next_run_sequence + 1
  return id
end

function MetaProfile.allocate_campaign(profile)
  assert(type(profile) == "table" and type(profile.next_campaign_sequence) == "number"
    and profile.next_campaign_sequence >= 1 and profile.next_campaign_sequence % 1 == 0,
    "Meta profile campaign sequence is invalid")
  local id = string.format("campaign:%06d", profile.next_campaign_sequence)
  profile.next_campaign_sequence = profile.next_campaign_sequence + 1
  return id
end

function MetaProfile.snapshot(profile, registry)
  MetaProfile.validate(profile, registry)
  local result = { unlocked_research_ids = {}, modifiers = {}, unlock_ids = {}, discovered_discovery_ids = {} }
  for _, id in ipairs(profile.unlocked_research_ids) do
    local node = registry:get_research(id)
    result.unlocked_research_ids[#result.unlocked_research_ids + 1] = id
    for key, value in pairs(node.modifiers or {}) do result.modifiers[key] = (result.modifiers[key] or 0) + value end
    for _, unlock in ipairs(node.unlocks or {}) do result.unlock_ids[#result.unlock_ids + 1] = unlock end
  end
  table.sort(result.unlock_ids)
  for _, id in ipairs(profile.discovered_discovery_ids) do result.discovered_discovery_ids[#result.discovered_discovery_ids + 1] = id end
  return result
end

function MetaProfile.has_discovery(profile, discovery_id)
  for _, id in ipairs(profile.discovered_discovery_ids or {}) do if id == discovery_id then return true end end
  return false
end

function MetaProfile.has_unlock(snapshot, unlock_id)
  for _, id in ipairs(snapshot and snapshot.unlock_ids or {}) do if id == unlock_id then return true end end
  return false
end

function MetaProfile.has_expedition_unlock(profile, unlock_id)
  for _, id in ipairs(profile.expedition_unlock_ids or {}) do
    if id == unlock_id then return true end
  end
  return false
end

function MetaProfile.unlock_expedition(profile, unlock_id)
  assert(type(unlock_id) == "string" and unlock_id:match("^expedition%.unlock%.[a-z0-9_%.%-]+$"),
    "Expedition unlock ID is invalid")
  profile.expedition_unlock_ids = profile.expedition_unlock_ids or {}
  if MetaProfile.has_expedition_unlock(profile, unlock_id) then
    return { applied = false, unlock_id = unlock_id, code = "already_unlocked" }
  end
  profile.expedition_unlock_ids[#profile.expedition_unlock_ids + 1] = unlock_id
  table.sort(profile.expedition_unlock_ids)
  return { applied = true, unlock_id = unlock_id }
end

function MetaProfile.purchase(profile, registry, research_id)
  local node = registry.research[research_id]
  if not node then return failure("unknown_research", "Unknown research node") end
  if MetaProfile.has_research(profile, research_id) then return failure("already_unlocked", "Research is already unlocked") end
  for _, prerequisite in ipairs(node.prerequisites or {}) do
    if not MetaProfile.has_research(profile, prerequisite) then return failure("missing_prerequisite", "Missing prerequisite research") end
  end
  if profile.research_data < node.cost then return failure("insufficient_research_data", "Not enough RESEARCH DATA") end
  profile.research_data = profile.research_data - node.cost
  profile.unlocked_research_ids[#profile.unlocked_research_ids + 1] = node.id
  table.sort(profile.unlocked_research_ids)
  return { applied = true, research_id = node.id, cost = node.cost, research_data = profile.research_data }
end

function MetaProfile.claim_reward(profile, reward_id, amount)
  assert(type(reward_id) == "string" and reward_id:match("^[%w:_%.%-]+$"), "Reward ID is invalid")
  assert(type(amount) == "number" and amount >= 0 and amount % 1 == 0, "Reward amount is invalid")
  for _, id in ipairs(profile.claimed_reward_ids) do
    if id == reward_id then return { applied = false, code = "already_claimed", reward_id = reward_id, research_data = profile.research_data } end
  end
  profile.claimed_reward_ids[#profile.claimed_reward_ids + 1] = reward_id
  table.sort(profile.claimed_reward_ids)
  profile.research_data = profile.research_data + amount
  return { applied = true, reward_id = reward_id, amount = amount, research_data = profile.research_data }
end

-- A discovery record and its DATA reward are committed to one copied profile
-- by App before it is written. The reward claim remains stable for crash
-- reconciliation while the semantic discovery record makes later runs use
-- their repeat-SCRAP path.
function MetaProfile.claim_discovery(profile, discovery_id, reward_id, amount)
  assert(type(discovery_id) == "string" and discovery_id:match("^discovery%.[a-z0-9_%.]+$"), "Discovery ID is invalid")
  profile.discovered_discovery_ids = profile.discovered_discovery_ids or {}
  local discovered = MetaProfile.has_discovery(profile, discovery_id)
  if not discovered then
    profile.discovered_discovery_ids[#profile.discovered_discovery_ids + 1] = discovery_id
    table.sort(profile.discovered_discovery_ids)
  end
  local reward = MetaProfile.claim_reward(profile, reward_id, amount)
  if reward.applied or not discovered then
    return {
      applied = true,
      code = reward.applied and "claimed" or "discovery_recorded",
      discovery_id = discovery_id,
      reward_id = reward_id,
      amount = reward.applied and amount or 0,
      research_data = profile.research_data,
    }
  end
  return {
    applied = false,
    code = "already_discovered",
    discovery_id = discovery_id,
    reward_id = reward_id,
    research_data = profile.research_data,
  }
end

return MetaProfile
