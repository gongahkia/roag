-- Headless authoring model for Expedition chambers and encounters.  Keeping
-- CRUD/validation/preview here makes Studio input thin and testable.
local Json = require("src.persistence.json")
local Writer = require("src.rooms.json_writer")
local Definitions = require("src.expedition.content_definitions")
local Vocabulary = require("src.expedition.vocabulary")
local Store = require("src.expedition.content_store")

local Model = {}
Model.__index = Model
local AUTHORING_SYMBOLS = { ["."]="floor", ["#"]="wall", ["~"]="water", ["^"]="spikes", f="fire", g="gas", b="breakable", v="volatile", d="door.closed" }

local function clone(value)
  local encoded, err = Json.encode(value); assert(encoded, err)
  local decoded, decode_err = Json.decode(encoded); assert(decoded, decode_err)
  return decoded
end
local function replace(value, index, character) return value:sub(1, index - 1) .. character .. value:sub(index + 1) end
local function kind_for(id) return id and id:match("^chamber%.") and "chambers" or "encounters" end
local function default_chamber(id)
  return { schema_version = 1, id = id, name = "NEW CHAMBER", enabled = true, weight = 1, width = 10, height = 8,
    topology_tags = { "open" }, tile_legend = { ["."] = "floor", ["#"] = "wall", ["~"] = "water", ["^"] = "spikes", ["f"] = "fire", ["g"] = "gas", ["b"] = "breakable", ["v"] = "volatile", d = "door.closed" },
    tiles = { "..........", "..........", "..........", "..........", "..........", "..........", "..........", ".........." },
    markers = { player_spawn = { { x = 3, y = 4 } }, enemy_spawns = { { x = 9, y = 4, group = "east" }, { x = 5, y = 7, group = "north" } }, exits = { { x = 9, y = 2 } }, caches = {}, reinforcements = {}, boss_spawns = {} } }
end
local function default_encounter(id)
  return { schema_version = 1, id = id, name = "NEW ENCOUNTER", archetype = "swarm", stage = 1, enabled = true,
    profiles = { "biome.legacy.forest" }, threat_budget = { base = 4 }, roles = { { role = "rusher", min = 1, max = 4, weight = 1 } }, max_enemies = 4,
    spawn_intent = "ring", elite = { allowed = false }, reinforcement = { enabled = false }, compatible_topology_tags = { "open" }, incompatible_topology_tags = {}, clear_condition = "eliminate", reward_intent = "scheduled" }
end

function Model.new(options)
  options = options or {}
  local self = setmetatable({ store = options.store or Store.new(), current = nil, current_kind = nil, original_id = nil, filename = nil, dirty = false, pending = nil, undo_stack = {}, redo_stack = {}, message = nil }, Model)
  local ok, failure = self:reload(); if not ok then return nil, failure end
  return self
end
function Model:reload()
  local registry, failure = Definitions.load({ store = self.store })
  if not registry then return nil, failure end
  self.registry = registry
  return true
end
function Model:list(kind, query, tag)
  local values = kind == "chambers" and self.registry.chambers or self.registry.encounters
  local result, needle = {}, (query or ""):lower()
  for _, value in ipairs(values) do
    local matches_text = needle == "" or value.id:lower():find(needle, 1, true) or value.name:lower():find(needle, 1, true)
    local tags = kind == "chambers" and value.topology_tags or { value.archetype }
    local matches_tag = not tag or tag == ""; for _, value_tag in ipairs(tags) do if value_tag == tag then matches_tag = true end end
    if matches_text and matches_tag then result[#result + 1] = value end
  end
  return result
end
function Model:current_definition() return self.current end
function Model:snapshot()
  if self.current then self.undo_stack[#self.undo_stack + 1] = clone(self.current); if #self.undo_stack > 64 then table.remove(self.undo_stack, 1) end; self.redo_stack = {} end
end
function Model:touch() self.dirty = true end
function Model:open(kind, id, discard)
  if self.dirty and not discard then self.pending = { action = "open", kind = kind, id = id }; return nil, { code = "unsaved_changes", reason = "Save or discard changes before opening another definition" } end
  local values = kind == "chambers" and self.registry.chamber_by_id or self.registry.encounter_by_id
  local value = values[id]; if not value then return nil, { code = "unknown_definition", reason = "Unknown " .. kind .. " definition" } end
  self.current, self.current_kind, self.original_id = clone(value), kind, id
  self.filename, self.dirty, self.pending, self.undo_stack, self.redo_stack = self.registry.filenames[kind][id], false, nil, {}, {}
  self.message = "Opened " .. id; return self.current
end
function Model:confirm_discard()
  local pending = self.pending; if not pending then return nil, { code = "nothing_pending" } end
  self.pending = nil
  if pending.action == "open" then return self:open(pending.kind, pending.id, true) end
  if pending.action == "new" then return self:new_definition(pending.kind, pending.id, true) end
  if pending.action == "duplicate" then return self:duplicate(pending.kind, pending.id, pending.new_id, true) end
end
function Model:new_definition(kind, id, discard)
  if self.dirty and not discard then self.pending = { action = "new", kind = kind, id = id }; return nil, { code = "unsaved_changes" } end
  self.current_kind, self.current, self.original_id, self.filename = kind, kind == "chambers" and default_chamber(id) or default_encounter(id), nil, nil
  self.dirty, self.undo_stack, self.redo_stack, self.pending = true, {}, {}, nil; return self.current
end
function Model:duplicate(kind, id, new_id, discard)
  if self.dirty and not discard then self.pending = { action = "duplicate", kind = kind, id = id, new_id = new_id }; return nil, { code = "unsaved_changes" } end
  local values = kind == "chambers" and self.registry.chamber_by_id or self.registry.encounter_by_id
  if not values[id] then return nil, { code = "unknown_definition" } end
  self.current, self.current_kind, self.original_id, self.filename = clone(values[id]), kind, nil, nil
  self.current.id, self.current.name, self.dirty, self.undo_stack, self.redo_stack = new_id, self.current.name .. " COPY", true, {}, {}
  return self.current
end
function Model:undo()
  if #self.undo_stack == 0 then return false end
  self.redo_stack[#self.redo_stack + 1] = clone(self.current); self.current = table.remove(self.undo_stack); self.dirty = true; return true
end
function Model:redo()
  if #self.redo_stack == 0 then return false end
  self.undo_stack[#self.undo_stack + 1] = clone(self.current); self.current = table.remove(self.redo_stack); self.dirty = true; return true
end
function Model:set_metadata(field, value)
  if not self.current then return nil, { code = "no_definition" } end
  local allowed = self.current_kind == "chambers"
    and { name = true, enabled = true, weight = true, topology_tags = true, boss_compatible = true }
    or { name = true, archetype = true, stage = true, max_enemies = true, spawn_intent = true, clear_condition = true, reward_intent = true, compatible_topology_tags = true, incompatible_topology_tags = true }
  if not allowed[field] then return nil, { code = "unsupported_metadata", reason = "Unsupported " .. self.current_kind .. " metadata field" } end
  self:snapshot(); self.current[field] = value; self:touch(); return true
end
function Model:set_threat_base(value)
  if not self.current or self.current_kind ~= "encounters" or type(value) ~= "number" or value <= 0 then return nil, { code = "invalid_threat" } end
  self:snapshot(); self.current.threat_budget.base = value; self:touch(); return true
end
function Model:set_role(index, field, value)
  if not self.current or self.current_kind ~= "encounters" or not self.current.roles[index] or (field ~= "role" and field ~= "min" and field ~= "max" and field ~= "weight") then return nil, { code = "invalid_role_row" } end
  self:snapshot(); self.current.roles[index][field] = value; self:touch(); return true
end
function Model:set_flag(group, value)
  if not self.current or self.current_kind ~= "encounters" or (group ~= "elite" and group ~= "reinforcement") then return nil, { code = "invalid_flag" } end
  self:snapshot(); self.current[group].allowed = group == "elite" and value or self.current[group].allowed; self.current[group].enabled = group == "reinforcement" and value or self.current[group].enabled; self:touch(); return true
end
function Model:paint(x, y, symbol)
  if not self.current or self.current_kind ~= "chambers" then return nil, { code = "no_chamber" } end
  if not AUTHORING_SYMBOLS[symbol] or x < 1 or y < 1 or x > self.current.width or y > self.current.height then return nil, { code = "invalid_paint" } end
  self:snapshot(); self.current.tile_legend[symbol] = self.current.tile_legend[symbol] or AUTHORING_SYMBOLS[symbol]
  self.current.tiles[y] = replace(self.current.tiles[y], x, symbol); self:touch(); return true
end
function Model:place_marker(kind, x, y, group)
  if not self.current or self.current_kind ~= "chambers" or not self.current.markers[kind] then return nil, { code = "invalid_marker" } end
  self:snapshot()
  if kind == "player_spawn" then self.current.markers[kind] = { { x = x, y = y } }
  else self.current.markers[kind][#self.current.markers[kind] + 1] = { x = x, y = y, group = group } end
  self:touch(); return true
end
function Model:erase_marker(kind, x, y)
  if not self.current or self.current_kind ~= "chambers" then return nil, { code = "no_chamber" } end
  self:snapshot(); for index = #self.current.markers[kind], 1, -1 do local marker = self.current.markers[kind][index]; if marker.x == x and marker.y == y then table.remove(self.current.markers[kind], index) end end; self:touch(); return true
end
function Model:resize(width, height, confirm)
  if not self.current or self.current_kind ~= "chambers" then return nil, { code = "no_chamber" } end
  if width < 8 or width > 14 or height < 7 or height > 10 then return nil, { code = "invalid_dimensions" } end
  local destructive = width < self.current.width or height < self.current.height
  if destructive and not confirm then return nil, { code = "resize_confirmation_required", reason = "Shrinking can remove tiles and markers" } end
  self:snapshot(); local rows = {}
  for y = 1, height do
    local row = self.current.tiles[y] or string.rep(".", self.current.width)
    rows[y] = row:sub(1, width) .. string.rep(".", math.max(0, width - #row))
  end
  self.current.width, self.current.height, self.current.tiles = width, height, rows
  for _, values in pairs(self.current.markers) do for index = #values, 1, -1 do if values[index].x > width or values[index].y > height then table.remove(values, index) end end end
  self:touch(); return true
end
function Model:add_role(role)
  if not self.current or self.current_kind ~= "encounters" or not Vocabulary.ROLE_COSTS[role] then return nil, { code = "invalid_role" } end
  self:snapshot(); self.current.roles[#self.current.roles + 1] = { role = role, min = 0, max = 1, weight = 1 }; self:touch(); return true
end
function Model:remove_role(index)
  if not self.current or self.current_kind ~= "encounters" or not self.current.roles[index] then return nil, { code = "invalid_role_row" } end
  self:snapshot(); table.remove(self.current.roles, index); self:touch(); return true
end
function Model:validate()
  if not self.current then return nil, { code = "no_definition" } end
  return self.current_kind == "chambers" and Definitions.validate_chamber(self.current) or Definitions.validate_encounter(self.current)
end
function Model:references(kind, id)
  local result = {}
  if kind == "chambers" then
    local chamber = self.registry.chamber_by_id[id]
    for _, encounter in ipairs(self.registry.encounters) do local okay = chamber and Definitions.compatibility(chamber, encounter); if okay then result[#result + 1] = "compatible with " .. encounter.id end end
  else
    local encounter = self.registry.encounter_by_id[id]; if encounter then result[#result + 1] = "stage " .. encounter.stage .. " encounter pool" end
  end
  return result
end
function Model:request_delete()
  if not self.current or not self.original_id then return nil, { code = "unsaved_definition" } end
  self.pending = { action = "delete", kind = self.current_kind, id = self.original_id, references = self:references(self.current_kind, self.original_id) }
  return self.pending
end
function Model:confirm_delete(confirm)
  local pending = self.pending; if not pending or pending.action ~= "delete" then return nil, { code = "nothing_pending" } end
  if not confirm then self.pending = nil; return false end
  local deleted, failure = self.store:remove(pending.kind, self.registry.filenames[pending.kind][pending.id]); if not deleted then return nil, failure end
  self.current, self.current_kind, self.original_id, self.filename, self.dirty, self.pending = nil, nil, nil, nil, false, nil
  return self:reload()
end
function Model:save()
  if not self.current then return nil, { code = "no_definition" } end
  local valid, failure = self:validate(); if not valid then return nil, failure end
  local filename = self.filename or self.store:filename_for(self.current_kind, self.current.id)
  if not filename then return nil, { code = "unsafe_id" } end
  local text, err = Writer.encode(self.current); if not text then return nil, { code = "encode_failed", reason = tostring(err) } end
  local written, write_failure = self.store:write(self.current_kind, filename, text); if not written then return nil, write_failure end
  local files, list_failure = self.store:list(self.current_kind); if not files then return nil, list_failure end
  local present = false; for _, value in ipairs(files) do if value == filename then present = true end end
  if not present then files[#files + 1] = filename; local manifest_ok, manifest_failure = self.store:write_manifest(self.current_kind, files); if not manifest_ok then return nil, manifest_failure end end
  local reloaded, reload_failure = self:reload(); if not reloaded then return nil, reload_failure end
  self.current = clone((self.current_kind == "chambers" and self.registry.chamber_by_id or self.registry.encounter_by_id)[self.current.id])
  self.original_id, self.filename, self.dirty, self.undo_stack, self.redo_stack = self.current.id, self.registry.filenames[self.current_kind][self.current.id], false, {}, {}
  return true
end

-- Uses the actual ExpeditionRun/Session composition path with an in-memory
-- profile and a one-chamber plan. No account store, unlock state or campaign
-- object is supplied, so preview cannot mutate a real run.
function Model:preview(options)
  options = options or {}
  local Run = require("src.expedition.run")
  local MetaProfile = require("src.persistence.meta_profile")
  local Registry = require("src.content.registry")
  local chamber_id = options.chamber_id or (self.current_kind == "chambers" and self.current and self.current.id)
  local encounter_id = options.encounter_id or (self.current_kind == "encounters" and self.current and self.current.id)
  if not chamber_id or not encounter_id then return nil, { code = "preview_selection_required", reason = "Select one chamber and one encounter for preview" } end
  local stage = options.stage or (self.registry.encounter_by_id[encounter_id] and self.registry.encounter_by_id[encounter_id].stage) or 1
  local seed = tonumber(options.seed) or 1
  local plan = Run.preview_plan(seed, encounter_id, chamber_id, stage, self.registry)
  local profile = MetaProfile.new()
  profile.expedition_unlock_ids = {
    "expedition.unlock.character.bruiser", "expedition.unlock.character.gunner",
    "expedition.unlock.character.conductor", "expedition.unlock.character.demolitionist",
  }
  local run = Run.new({ seed = seed, character_id = options.character_id or "expedition.gunner", meta_profile = profile,
    registry = Registry.load(), definition_registry = self.registry, plan_override = { plan }, on_event = options.on_event or function() end })
  return { run = run, plan = plan, seed = seed, static = { chamber = plan.chamber, encounter = plan.template, enemies = plan.enemies, threat_budget = plan.budget } }
end

return Model
