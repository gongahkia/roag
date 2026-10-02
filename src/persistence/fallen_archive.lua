-- Persistent historical bodies are deliberately separate from both the
-- active run and account research.  Records preserve data even when current
-- content later makes them unavailable for recurrence.
local Json = require("src.persistence.json")
local Body = require("src.body.body")
local Identity = require("src.campaign.identity")

local FallenArchive = {
  FORMAT = "roag.fallen_archive",
  VERSION = 1,
}

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function copy_component(component)
  if not component then return nil end
  return {
    id = component.id,
    definition_id = component.definition_id,
    max_integrity = component.max_integrity,
    current_integrity = component.current_integrity,
    condition = component.condition,
  }
end

function FallenArchive.copy_body(body)
  local result = { topology_id = body.topology_id, slots = {} }
  for _, slot in ipairs(body.slots or {}) do
    result.slots[#result.slots + 1] = {
      slot_id = slot.slot_id,
      component = copy_component(slot.component),
    }
  end
  return result
end

local function copy_strings(values, preserve_order)
  local result = {}
  for _, value in ipairs(values or {}) do result[#result + 1] = value end
  if not preserve_order then table.sort(result) end
  return result
end

local function copy_metadata(metadata)
  metadata = metadata or {}
  return {
    route_node_id = metadata.route_node_id,
    biome_id = metadata.biome_id,
    tier_id = metadata.tier_id,
    route_depth = metadata.route_depth,
    -- The chosen path is historical sequence data, not an unordered set.
    route_path = copy_strings(metadata.route_path, true),
    charm_ids = copy_strings(metadata.charm_ids),
    research_ids = copy_strings(metadata.research_ids),
    death_cause = metadata.death_cause,
  }
end

local function copy_record(record)
  return {
    id = record.id,
    source_run_id = record.source_run_id,
    body = FallenArchive.copy_body(record.body),
    metadata = copy_metadata(record.metadata),
  }
end

local function assert_id(value, pattern, label)
  assert(type(value) == "string" and value:match(pattern), label .. " is invalid")
end

local function validate_body_shape(body)
  assert(type(body) == "table", "Fallen record body must be a table")
  assert_id(body.topology_id, "^body%.topology%.[a-z0-9_%.]+$", "Fallen record topology ID")
  assert(type(body.slots) == "table", "Fallen record body must list slots")
  local slots, components = {}, {}
  for _, slot in ipairs(body.slots) do
    assert(type(slot) == "table", "Fallen record body slot must be a table")
    assert_id(slot.slot_id, "^[a-z0-9_]+$", "Fallen record slot ID")
    assert(not slots[slot.slot_id], "Fallen record duplicates body slot '" .. slot.slot_id .. "'")
    slots[slot.slot_id] = true
    if slot.component then
      local component = slot.component
      assert(Identity.is_component_id(component.id), "Fallen source component ID is invalid")
      assert_id(component.definition_id, "^component%.[a-z0-9_%.]+$", "Fallen component definition ID")
      assert(not components[component.id], "Fallen record duplicates source component '" .. component.id .. "'")
      components[component.id] = true
      assert(type(component.current_integrity) == "number" and component.current_integrity >= 0
        and component.current_integrity % 1 == 0, "Fallen component integrity is invalid")
      assert(type(component.max_integrity) == "number" and component.max_integrity > 0
        and component.max_integrity % 1 == 0 and component.current_integrity <= component.max_integrity,
        "Fallen component max integrity is invalid")
    end
  end
end

local function validate_metadata(metadata)
  assert(type(metadata) == "table", "Fallen record metadata must be a table")
  for _, field in ipairs({ "route_node_id", "biome_id", "tier_id", "death_cause" }) do
    assert(metadata[field] == nil or type(metadata[field]) == "string", "Fallen metadata " .. field .. " is invalid")
  end
  assert(metadata.route_depth == nil or (type(metadata.route_depth) == "number" and metadata.route_depth >= 1
    and metadata.route_depth % 1 == 0), "Fallen metadata route depth is invalid")
  for _, field in ipairs({ "route_path", "charm_ids", "research_ids" }) do
    assert(type(metadata[field] or {}) == "table", "Fallen metadata " .. field .. " must be a list")
    for _, value in ipairs(metadata[field] or {}) do assert(type(value) == "string", "Fallen metadata " .. field .. " contains invalid value") end
  end
end

function FallenArchive.new()
  return { next_archive_sequence = 1, characters = {} }
end

function FallenArchive.copy(archive)
  local result = { next_archive_sequence = archive.next_archive_sequence, characters = {} }
  for _, record in ipairs(archive.characters or {}) do result.characters[#result.characters + 1] = copy_record(record) end
  return result
end

function FallenArchive.validate(archive)
  assert(type(archive) == "table", "Fallen archive must be a table")
  assert(type(archive.next_archive_sequence) == "number" and archive.next_archive_sequence >= 1
    and archive.next_archive_sequence % 1 == 0, "Fallen archive sequence is invalid")
  assert(type(archive.characters) == "table", "Fallen archive characters must be a list")
  local ids, runs, highest = {}, {}, 0
  for _, record in ipairs(archive.characters) do
    assert(type(record) == "table", "Fallen archive record must be a table")
    assert_id(record.id, "^fallen:%d+$", "Fallen archive ID")
    assert_id(record.source_run_id, "^run:%d+$", "Fallen source run ID")
    assert(not ids[record.id], "Fallen archive duplicates ID '" .. record.id .. "'")
    assert(not runs[record.source_run_id], "Fallen archive duplicates source run '" .. record.source_run_id .. "'")
    ids[record.id], runs[record.source_run_id] = true, true
    highest = math.max(highest, tonumber(record.id:match("(%d+)$")) or 0)
    validate_body_shape(record.body)
    validate_metadata(record.metadata or {})
  end
  assert(archive.next_archive_sequence > highest, "Fallen archive sequence collides with an existing record")
  table.sort(archive.characters, function(first, second) return first.id < second.id end)
  return true
end

function FallenArchive.to_data(archive)
  FallenArchive.validate(archive)
  return FallenArchive.copy(archive)
end

function FallenArchive.encode(archive)
  local text, reason = Json.encode({
    format = FallenArchive.FORMAT,
    version = FallenArchive.VERSION,
    next_archive_sequence = archive.next_archive_sequence,
    characters = FallenArchive.to_data(archive).characters,
  })
  if not text then return failure("encode_failed", tostring(reason)) end
  return text
end

function FallenArchive.decode(text)
  local envelope, reason = Json.decode(text)
  if not envelope then return failure("invalid_json", tostring(reason)) end
  if type(envelope) ~= "table" then return failure("invalid_state", "Fallen archive envelope must be an object") end
  if envelope.format ~= FallenArchive.FORMAT then return failure("unsupported_format", "Save format is not roag.fallen_archive") end
  if envelope.version ~= FallenArchive.VERSION then return failure("unsupported_version", "Fallen archive version is not supported") end
  local archive = {
    next_archive_sequence = envelope.next_archive_sequence,
    characters = envelope.characters,
  }
  local ok, validation = pcall(FallenArchive.validate, archive)
  if not ok then return failure("invalid_state", tostring(validation)) end
  return FallenArchive.copy(archive)
end

function FallenArchive.load(store)
  local text, error_data = store:read()
  if not text and error_data and error_data.code == "missing_file" then return FallenArchive.new(), { fresh = true } end
  if not text then return nil, error_data end
  return FallenArchive.decode(text)
end

function FallenArchive.save(archive, store)
  local text, error_data = FallenArchive.encode(archive)
  if not text then return nil, error_data end
  local written, write_error = store:write(text)
  if not written then return nil, write_error or { code = "write_failed", reason = "Could not write fallen archive" } end
  return true
end

function FallenArchive.find_by_source_run(archive, source_run_id)
  for _, record in ipairs(archive.characters or {}) do
    if record.source_run_id == source_run_id then return record end
  end
  return nil
end

function FallenArchive.append(archive, snapshot)
  assert(type(snapshot) == "table", "Fallen snapshot is required")
  assert_id(snapshot.source_run_id, "^run:%d+$", "Fallen source run ID")
  local existing = FallenArchive.find_by_source_run(archive, snapshot.source_run_id)
  if existing then return { applied = false, code = "already_archived", record = existing } end
  local record = {
    id = string.format("fallen:%06d", archive.next_archive_sequence),
    source_run_id = snapshot.source_run_id,
    body = FallenArchive.copy_body(snapshot.body),
    metadata = copy_metadata(snapshot.metadata),
  }
  validate_body_shape(record.body)
  validate_metadata(record.metadata)
  archive.next_archive_sequence = archive.next_archive_sequence + 1
  archive.characters[#archive.characters + 1] = record
  table.sort(archive.characters, function(first, second) return first.id < second.id end)
  return { applied = true, record = record }
end

function FallenArchive.compatibility(record, registry)
  local ok, reason = pcall(Body.from_data, registry, record.body)
  if not ok then return false, tostring(reason) end
  return true
end

function FallenArchive.compatible_records(archive, registry)
  local result = {}
  for _, record in ipairs(archive.characters or {}) do
    local compatible, reason = FallenArchive.compatibility(record, registry)
    if compatible then
      result[#result + 1] = record
    else
      result[#result + 1] = { id = record.id, source_run_id = record.source_run_id, incompatible = true, incompatibility_reason = reason, record = record }
    end
  end
  return result
end

return FallenArchive
