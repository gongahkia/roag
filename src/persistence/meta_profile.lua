-- Separate versioned account progression.  It deliberately contains no
-- current-run body, world, route, SCRAP, or presentation state.
local Json = require("src.persistence.json")

local MetaProfile = {
  FORMAT = "roag.meta_profile",
  VERSION = 1,
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
  return { research_data = 0, unlocked_research_ids = {}, next_run_sequence = 1, claimed_reward_ids = {} }
end

function MetaProfile.copy(profile)
  return {
    research_data = profile.research_data,
    unlocked_research_ids = sorted_unique(profile.unlocked_research_ids, "unlocked_research_ids"),
    next_run_sequence = profile.next_run_sequence,
    claimed_reward_ids = sorted_unique(profile.claimed_reward_ids, "claimed_reward_ids"),
  }
end

function MetaProfile.validate(profile, registry)
  assert(type(profile) == "table", "Meta profile must be a table")
  assert(type(profile.research_data) == "number" and profile.research_data >= 0 and profile.research_data % 1 == 0,
    "Meta profile research_data must be a non-negative integer")
  assert(type(profile.next_run_sequence) == "number" and profile.next_run_sequence >= 1 and profile.next_run_sequence % 1 == 0,
    "Meta profile next_run_sequence must be a positive integer")
  local unlocked = sorted_unique(profile.unlocked_research_ids, "Meta profile unlocked research IDs")
  local claims = sorted_unique(profile.claimed_reward_ids, "Meta profile claimed reward IDs")
  for _, id in ipairs(unlocked) do
    assert(registry.research[id], "Meta profile references unknown research ID '" .. id .. "'")
  end
  for _, id in ipairs(claims) do
    assert(id:match("^[%w:_%.%-]+$"), "Meta profile has malformed claimed reward ID '" .. id .. "'")
  end
  profile.unlocked_research_ids, profile.claimed_reward_ids = unlocked, claims
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

function MetaProfile.snapshot(profile, registry)
  MetaProfile.validate(profile, registry)
  local result = { unlocked_research_ids = {}, modifiers = {}, unlock_ids = {} }
  for _, id in ipairs(profile.unlocked_research_ids) do
    local node = registry:get_research(id)
    result.unlocked_research_ids[#result.unlocked_research_ids + 1] = id
    for key, value in pairs(node.modifiers or {}) do result.modifiers[key] = (result.modifiers[key] or 0) + value end
    for _, unlock in ipairs(node.unlocks or {}) do result.unlock_ids[#result.unlock_ids + 1] = unlock end
  end
  table.sort(result.unlock_ids)
  return result
end

function MetaProfile.has_unlock(snapshot, unlock_id)
  for _, id in ipairs(snapshot and snapshot.unlock_ids or {}) do if id == unlock_id then return true end end
  return false
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

return MetaProfile
