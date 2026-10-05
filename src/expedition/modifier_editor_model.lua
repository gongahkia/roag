-- Non-rendering controller for the Studio Modifier page. Keeping this model
-- pure makes all CRUD/validation/simulation behavior testable without LÖVE.
local Definitions = require("src.expedition.modifiers")
local Store = require("src.expedition.modifier_store")
local Simulator = require("src.expedition.modifier_simulator")

local Editor = {}
Editor.__index = Editor

local function clone(value)
  if type(value) ~= "table" then return value end
  local result = {}; for key, child in pairs(value) do result[key] = clone(child) end; return result
end
local function slug(value)
  return value:lower():gsub("[^a-z0-9]+", "_"):gsub("^_+", ""):gsub("_+$", "")
end

function Editor.new(options)
  options = options or {}
  local self = setmetatable({ store = options.store or Store.new({ writable = true }), registry = options.registry, selected_id = nil, draft = nil, dirty = false, message = "", pending_delete = nil }, Editor)
  assert(self:reload())
  return self
end
function Editor:reload()
  local files, failure = self.store:list(); if not files then return nil, failure end
  local file_map = { ["manifest.json"] = assert(self.store:read("manifest.json")) }
  for _, filename in ipairs(files) do file_map[filename] = assert(self.store:read(filename)) end
  local definitions, reason = Definitions.load({ files = file_map, registry = self.registry })
  if not definitions then return nil, reason end
  self.modifiers, self.files = definitions, files
  if self.selected_id and not definitions:get(self.selected_id) then self.selected_id = nil end
  if not self.selected_id and definitions.ordered[1] then self.selected_id = definitions.ordered[1].id end
  self.draft, self.dirty, self.message = nil, false, "Loaded serialized modifier content."
  return true
end
function Editor:list(query)
  local result = {}
  query = query and query:lower() or nil
  for _, definition in ipairs(self.modifiers.ordered) do
    local text = (definition.id .. " " .. definition.name .. " " .. definition.category):lower()
    if not query or text:find(query, 1, true) then
      result[#result + 1] = { id = definition.id, name = definition.name, category = definition.category, tags = clone(definition.tags), enabled = definition.pool.enabled, valid = true }
    end
  end
  return result
end
function Editor:select(id)
  local definition = self.modifiers:get(id); if not definition then return nil, { code = "missing_modifier", reason = "Modifier is not in the registry" } end
  if self.dirty then return nil, { code = "dirty", reason = "Save or discard the current draft before switching modifiers" } end
  self.selected_id, self.draft = id, clone(definition); self.draft._source_file = nil; self.draft.behavior_changing = nil
  return self.draft
end
function Editor:current()
  if self.draft then return self.draft end
  local definition = self.selected_id and self.modifiers:get(self.selected_id)
  if definition then self.draft = clone(definition); self.draft._source_file, self.draft.behavior_changing = nil, nil end
  return self.draft
end
function Editor:touch() self.dirty = true end
function Editor:add_hook(trigger)
  local definition = assert(self:current()); assert(Definitions.TRIGGERS[trigger], "Unknown trigger")
  definition.hooks[#definition.hooks + 1] = { trigger = trigger, conditions = {}, effects = {} }; self:touch()
  return definition.hooks[#definition.hooks]
end
function Editor:add_condition(hook_index, kind)
  local hook = assert(self:current().hooks[hook_index], "Unknown hook"); assert(Definitions.CONDITIONS[kind], "Unknown condition")
  local condition = { kind = kind }; hook.conditions[#hook.conditions + 1] = condition; self:touch(); return condition
end
function Editor:add_effect(hook_index, kind)
  local hook = assert(self:current().hooks[hook_index], "Unknown hook"); assert(Definitions.EFFECTS[kind] and kind ~= "modify_stat", "Unknown reactive effect")
  local effect = { kind = kind }
  for _, field in ipairs(Definitions.EFFECTS[kind].fields) do if field.type == "stack_expression" then effect[field.name] = { kind = "constant", value = 1 } end end
  hook.effects[#hook.effects + 1] = effect; self:touch(); return effect
end
function Editor:add_static_stat(stat)
  assert(Definitions.EFFECTS.modify_stat); local effect = { kind = "modify_stat", stat = stat, value = { kind = "linear", base = 1, per_stack = 1 } }
  self:current().static_effects[#self:current().static_effects + 1] = effect; self:touch(); return effect
end
function Editor:remove(list, index)
  assert(type(list) == "table" and list[index], "Unknown editor list entry"); table.remove(list, index); self:touch()
end
function Editor:validate()
  local definition = self:current(); if not definition then return nil, { code = "missing_modifier", reason = "No modifier selected" } end
  return Definitions.validate(definition, { file = self.store.directory .. "/" .. (self.modifiers:filename_for(definition.id) or "draft.json"), registry = self.registry })
end
function Editor:create(id)
  id = id or "expedition.passive.new_modifier"
  if self.modifiers:get(id) then return nil, { code = "duplicate_id", reason = "Modifier ID already exists" } end
  self.selected_id, self.draft, self.dirty = id, Definitions.new_definition(id), true
  return self.draft
end
function Editor:duplicate(id, new_id)
  local original = assert(self.modifiers:get(id), "Unknown modifier")
  new_id = new_id or (id .. "_copy")
  if self.modifiers:get(new_id) then return nil, { code = "duplicate_id", reason = "Modifier ID already exists" } end
  self.selected_id, self.draft = new_id, clone(original)
  self.draft.id, self.draft.name, self.draft.pool.enabled = new_id, original.name .. " COPY", false
  self.draft._source_file, self.draft.behavior_changing, self.dirty = nil, nil, true
  return self.draft
end
function Editor:discard() self.draft, self.dirty, self.pending_delete = nil, false, nil; return self:select(self.selected_id) end
function Editor:stack_preview(expression, stacks)
  local result = {}
  for _, count in ipairs(stacks or { 1, 2, 3, 5, 10 }) do result[#result + 1] = { stacks = count, value = assert(Definitions.evaluate_expression(expression, count)) } end
  return result
end
function Editor:description_preview(stacks)
  local definition = self:current(); if not definition then return nil end
  return Definitions.stack_preview(definition, stacks or 1)
end
function Editor:simulate(config)
  local definition = self:current(); local ok, failure = self:validate(); if not ok then return nil, failure end
  local files = {}
  for _, filename in ipairs(self.files) do files[filename] = assert(self.store:read(filename)) end
  local filename = self.modifiers:filename_for(definition.id) or self.store:filename_for_id(definition.id)
  files["manifest.json"] = assert(Definitions.canonical_json({ format = Definitions.MANIFEST_FORMAT, version = Definitions.MANIFEST_VERSION, files = (function() local list = clone(self.files); local found = false; for _, name in ipairs(list) do if name == filename then found = true end end; if not found then list[#list + 1] = filename; table.sort(list); end; return list end)() }))
  files[filename] = assert(Definitions.canonical_json(definition))
  local runtime = assert(Definitions.load({ files = files, registry = self.registry }))
  config = config or {}; config.definition, config.modifier_registry = runtime:get(definition.id), runtime
  return Simulator.run(config)
end
function Editor:save()
  local definition = self:current(); local ok, failure = self:validate(); if not ok then return nil, failure end
  local filename = self.modifiers:filename_for(definition.id) or self.store:filename_for_id(definition.id)
  if not filename then return nil, { code = "unsafe_path", reason = "Modifier ID cannot map to a content filename" } end
  local write_ok, write_failure = self.store:write_definition(filename, definition, self.registry); if not write_ok then return nil, write_failure end
  local files, known = self.store:list(); if not files then return nil, known end
  local found = false; for _, item in ipairs(files) do if item == filename then found = true end end
  if not found then files[#files + 1] = filename; table.sort(files); local manifest_ok, manifest_failure = self.store:write_manifest(files); if not manifest_ok then return nil, manifest_failure end end
  self.selected_id, self.dirty = definition.id, false
  return self:reload()
end
function Editor:request_delete(id)
  local definition = self.modifiers:get(id or self.selected_id); if not definition then return nil, { code = "missing_modifier", reason = "Modifier is not in the registry" } end
  self.pending_delete = definition.id
  return { id = definition.id, filename = self.modifiers:filename_for(definition.id), unlock = definition.unlock, pool_enabled = definition.pool.enabled }
end
function Editor:confirm_delete(confirmed)
  local id = self.pending_delete; self.pending_delete = nil
  if not confirmed then return { applied = false, code = "cancelled" } end
  local filename = id and self.modifiers:filename_for(id); if not filename then return nil, { code = "missing_modifier", reason = "Modifier is not in the registry" } end
  local files = assert(self.store:list()); local retained = {}
  for _, value in ipairs(files) do if value ~= filename then retained[#retained + 1] = value end end
  local deleted, failure = self.store:delete(filename); if not deleted then return nil, failure end
  local manifest_ok, manifest_failure = self.store:write_manifest(retained); if not manifest_ok then return nil, manifest_failure end
  self.selected_id, self.draft, self.dirty = nil, nil, false
  return self:reload()
end

return Editor
