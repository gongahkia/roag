-- Headless state and safety rules for the one-room authoring tool.  It owns
-- no gameplay session or save store: content reads/writes only flow through
-- the constrained room Store supplied at construction.
local Config = require("src.rooms.config")
local Json = require("src.persistence.json")
local Writer = require("src.rooms.json_writer")
local RoomRegistry = require("src.rooms.registry")
local Store = require("src.rooms.store")
local Template = require("src.rooms.template")

local EditorModel = {}
EditorModel.__index = EditorModel

local function clone(value)
  local encoded, encode_error = Json.encode(value)
  assert(encoded, encode_error)
  local result, decode_error = Json.decode(encoded)
  assert(result, decode_error)
  return result
end

local function replace_character(value, index, replacement)
  return value:sub(1, index - 1) .. replacement .. value:sub(index + 1)
end

local function valid_side_offset(side, offset)
  local limit = (side == "east" or side == "west") and Config.HEIGHT or Config.WIDTH
  return Config.SIDES[side] and type(offset) == "number" and offset % 1 == 0
    and offset >= 0 and offset < limit
end

function EditorModel.new(options)
  options = options or {}
  local self = setmetatable({
    store = options.store or Store.new(),
    registry = options.registry,
    current = nil,
    current_filename = nil,
    original_id = nil,
    dirty = false,
    pending = nil,
    message = nil,
  }, EditorModel)
  local ok, failure = self:reload_registry()
  if not ok then return nil, failure end
  return self
end

function EditorModel:reload_registry()
  local rooms, failure = RoomRegistry.load({ store = self.store, registry = self.registry })
  if not rooms then return nil, failure end
  self.rooms = rooms
  self.registry = rooms.registry
  return true
end

function EditorModel:list()
  return self.rooms:list()
end

function EditorModel:validation()
  if not self.current then return { valid = false, errors = { { code = "no_template", message = "No room is open" } }, warnings = {} } end
  return Template.validate(self.current, self.registry, { existing_ids = self.rooms.templates, current_id = self.original_id })
end

function EditorModel:can_discard()
  return not self.dirty
end

function EditorModel:open(id, discard)
  if self.dirty and not discard then
    self.pending = { action = "open", id = id }
    return nil, { code = "unsaved_changes", reason = "Save or discard the current room before opening another template" }
  end
  local template = self.rooms:get(id)
  if not template then return nil, { code = "unknown_template", reason = "Unknown room template " .. tostring(id) } end
  self.current = clone(template)
  self.current_filename = self.rooms.filenames[id]
  self.original_id = id
  self.dirty, self.pending, self.message = false, nil, "Opened " .. id
  return self.current
end

function EditorModel:confirm_discard()
  if not self.pending then return nil, { code = "nothing_pending" } end
  local pending = self.pending
  self.pending = nil
  if pending.action == "open" then return self:open(pending.id, true) end
  if pending.action == "new" then return self:new_template(pending.id, true) end
  if pending.action == "duplicate" then return self:duplicate(pending.id, pending.new_id, true) end
  return nil, { code = "unsupported_pending" }
end

function EditorModel:new_template(id, discard)
  if self.dirty and not discard then
    self.pending = { action = "new", id = id }
    return nil, { code = "unsaved_changes", reason = "Save or discard the current room before creating another template" }
  end
  self.current = Template.default(id)
  self.current_filename, self.original_id = nil, nil
  self.dirty, self.pending, self.message = true, nil, "New room template"
  return self.current
end

function EditorModel:duplicate(id, new_id, discard)
  if self.dirty and not discard then
    self.pending = { action = "duplicate", id = id, new_id = new_id }
    return nil, { code = "unsaved_changes", reason = "Save or discard the current room before duplicating a template" }
  end
  local source = self.rooms:get(id)
  if not source then return nil, { code = "unknown_template", reason = "Unknown room template " .. tostring(id) } end
  self.current = clone(source)
  self.current.id = new_id or (id .. ".copy")
  self.current_filename, self.original_id = nil, nil
  self.dirty, self.pending, self.message = true, nil, "Duplicated " .. id
  return self.current
end

function EditorModel:set_id(id)
  if not self.current then return nil, { code = "no_template" } end
  if self.original_id then return nil, { code = "id_locked", reason = "Open a duplicate or new template to change its semantic ID" } end
  self.current.id, self.dirty = id, true
  return true
end

function EditorModel:set_metadata(field, value)
  if not self.current then return nil, { code = "no_template" } end
  if field ~= "tags" and field ~= "weight" and field ~= "allow_rotation" then
    return nil, { code = "invalid_metadata", reason = "Unsupported room metadata field" }
  end
  self.current[field], self.dirty = value, true
  return true
end

function EditorModel:paint(x, y, glyph)
  if not self.current then return nil, { code = "no_template" } end
  if x < 0 or x >= Config.WIDTH or y < 0 or y >= Config.HEIGHT then return nil, { code = "out_of_bounds" } end
  if type(glyph) ~= "string" or #glyph ~= 1 or not self.current.legend[glyph] then
    return nil, { code = "unknown_glyph", reason = "Glyph is not in this room legend" }
  end
  local row_index = self.current.height - y
  self.current.layout[row_index] = replace_character(self.current.layout[row_index], x + 1, glyph)
  self.dirty = true
  return true
end

function EditorModel:toggle_connector(side, offset)
  if not self.current then return nil, { code = "no_template" } end
  if not valid_side_offset(side, offset) then return nil, { code = "invalid_connector", reason = "Connectors must be on a valid room boundary" } end
  for index, connector in ipairs(self.current.connectors) do
    if connector.side == side and connector.offset == offset then
      table.remove(self.current.connectors, index)
      self.dirty = true
      return { applied = true, present = false }
    end
  end
  self.current.connectors[#self.current.connectors + 1] = { side = side, offset = offset }
  table.sort(self.current.connectors, function(a, b)
    local ai, bi = 0, 0
    for index, current_side in ipairs(Config.SIDE_ORDER) do
      if current_side == a.side then ai = index end
      if current_side == b.side then bi = index end
    end
    return ai == bi and a.offset < b.offset or ai < bi
  end)
  self.dirty = true
  return { applied = true, present = true }
end

function EditorModel:rotation_preview(turns)
  if not self.current then return nil, { code = "no_template" } end
  if not self.current.allow_rotation and (turns or 0) % 4 ~= 0 then
    return nil, { code = "rotation_disabled", reason = "This template does not permit rotation" }
  end
  return Template.rotate(self.current, turns or 0)
end

function EditorModel:save()
  if not self.current then return nil, { code = "no_template" } end
  local validation = self:validation()
  if not validation.valid then
    return nil, { code = "invalid_template", reason = "Room validation failed", errors = validation.errors }
  end
  local filename = self.current_filename or self.store:filename_for_id(self.current.id)
  if not filename then return nil, { code = "invalid_id", reason = "Room semantic ID cannot be mapped to a safe file name" } end
  local text, encode_error = Writer.encode(self.current)
  if not text then return nil, { code = "write_failed", reason = tostring(encode_error) } end
  local written, write_failure = self.store:write(filename, text)
  if not written then return nil, write_failure end
  local files, list_failure = self.store:list()
  if not files then return nil, list_failure end
  local found = false
  for _, current_filename in ipairs(files) do if current_filename == filename then found = true end end
  if not found then
    files[#files + 1] = filename
    local manifest_written, manifest_failure = self.store:write_manifest(files)
    if not manifest_written then return nil, manifest_failure end
  end
  local loaded, reload_failure = self:reload_registry()
  if not loaded then return nil, reload_failure end
  self.current = clone(assert(self.rooms:get(self.current.id)))
  self.current_filename, self.original_id, self.dirty = self.rooms.filenames[self.current.id], self.current.id, false
  self.message = "Saved " .. self.current.id
  return { applied = true, filename = self.current_filename }
end

return EditorModel
