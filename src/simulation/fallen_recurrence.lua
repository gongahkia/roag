-- A run snapshots one selected historical body at creation.  This module
-- never reads persistent storage: it turns that immutable snapshot into
-- fresh, current-run physical components only when its target floor starts.
local Body = require("src.body.body")
local Component = require("src.body.component")
local Grid = require("src.world.grid")
local Rng = require("src.rng")

local Recurrence = {
  ECHO_HEALTH = 3,
}

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

local function copy_body(body)
  local result = { topology_id = body.topology_id, slots = {} }
  for _, slot in ipairs(body.slots or {}) do
    result.slots[#result.slots + 1] = { slot_id = slot.slot_id, component = copy_component(slot.component) }
  end
  return result
end

local function assert_spec_shape(spec)
  assert(type(spec) == "table", "Fallen recurrence must be a table")
  assert(type(spec.archive_id) == "string" and spec.archive_id:match("^fallen:%d+$"), "Fallen recurrence archive ID is invalid")
  assert(type(spec.source_run_id) == "string" and spec.source_run_id:match("^run:%d+$"), "Fallen recurrence source run ID is invalid")
  assert(spec.mode == "corpse" or spec.mode == "hostile", "Fallen recurrence mode is invalid")
  assert(type(spec.target_depth) == "number" and (spec.target_depth == 2 or spec.target_depth == 3), "Fallen recurrence depth is invalid")
  assert(type(spec.encounter_seed) == "number" and spec.encounter_seed % 1 == 0, "Fallen recurrence seed is invalid")
  assert(type(spec.body) == "table" and type(spec.body.topology_id) == "string" and type(spec.body.slots) == "table",
    "Fallen recurrence body is invalid")
end

function Recurrence.copy_spec(spec)
  if not spec then return nil end
  assert_spec_shape(spec)
  return {
    archive_id = spec.archive_id,
    source_run_id = spec.source_run_id,
    mode = spec.mode,
    target_depth = spec.target_depth,
    encounter_seed = spec.encounter_seed,
    body = copy_body(spec.body),
    spawned = spec.spawned == true,
    resolved = spec.resolved == true,
    skipped = spec.skipped == true,
    placement = spec.placement and { x = spec.placement.x, y = spec.placement.y, node_id = spec.placement.node_id } or nil,
    placement_failure = spec.placement_failure and { code = spec.placement_failure.code, reason = spec.placement_failure.reason } or nil,
  }
end

function Recurrence.assign(run_id, run_seed, records)
  local compatible = {}
  for _, entry in ipairs(records or {}) do
    if not entry.incompatible then compatible[#compatible + 1] = entry.record or entry end
  end
  table.sort(compatible, function(first, second) return first.id < second.id end)
  if #compatible == 0 then return nil end
  local rng = Rng.new(run_seed):derive("fallen.recurrence." .. run_id)
  local record = rng:choice(compatible)
  return {
    archive_id = record.id,
    source_run_id = record.source_run_id,
    mode = rng:int(1, 2) == 1 and "corpse" or "hostile",
    target_depth = rng:int(2, 3),
    encounter_seed = rng:derive("encounter." .. record.id):next(),
    body = copy_body(record.body),
    spawned = false,
    resolved = false,
  }
end

function Recurrence.validate_spec(spec, registry)
  if not spec then return true end
  local ok, reason = pcall(function()
    assert_spec_shape(spec)
    Body.from_data(registry, spec.body)
    if spec.placement then
      assert(type(spec.placement.x) == "number" and type(spec.placement.y) == "number"
        and Grid.in_bounds(spec.placement.x, spec.placement.y), "Fallen recurrence placement is invalid")
      assert(type(spec.placement.node_id) == "string", "Fallen recurrence placement node is invalid")
    end
  end)
  if not ok then return nil, tostring(reason) end
  return true
end

function Recurrence.materialize_body(session, spec)
  local valid, reason = Recurrence.validate_spec(spec, session.registry)
  assert(valid, reason)
  local body = Body.new(session.registry, spec.body.topology_id)
  for _, slot in ipairs(spec.body.slots) do
    if slot.component then
      local source = slot.component
      local component = session.component_factory:create(source.definition_id)
      component.current_integrity = math.max(0, math.min(component.max_integrity, source.current_integrity))
      component.origin = {
        archive_id = spec.archive_id,
        source_run_id = spec.source_run_id,
        source_component_id = source.id,
      }
      local installed, install_reason = body:install(slot.slot_id, component)
      assert(installed, install_reason)
    end
  end
  body:validate()
  return body
end

return Recurrence
