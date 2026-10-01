-- Room-template data storage. Runtime reads source or packaged content;
-- editor writes are explicitly constrained to a source checkout directory.
local Json = require("src.persistence.json")
local Config = require("src.rooms.config")

local Store = {}
Store.__index = Store

-- `love level_editor` mounts level_editor/ as the source root, while the
-- authored corpus remains one directory above it. Resolve that source-checkout
-- location here so both standalone developer tools use the same content as
-- normal gameplay. Headless tests and packaged builds retain the relative
-- runtime path.
local function default_directory()
  if love and love.filesystem and love.filesystem.getSource then
    local source = love.filesystem.getSource()
    local root = type(source) == "string" and source:match("^(.*)/level_editor$")
    if root then return root .. "/" .. Config.DIRECTORY end
  end
  return Config.DIRECTORY
end

local function safe_filename(filename)
  return type(filename) == "string" and filename:match("^[a-z0-9_%-]+%.room%.json$") ~= nil
end

function Store.new(options)
  options = options or {}
  return setmetatable({ directory = options.directory or default_directory(), files = options.files, writable = options.writable }, Store)
end

function Store:can_write()
  if self.files then return self.writable == true end
  if self.writable ~= nil then return self.writable end
  if love and love.filesystem and love.filesystem.isFused and love.filesystem.isFused() then return false end
  return true
end

function Store:read(filename)
  if not safe_filename(filename) and filename ~= "manifest.json" then return nil, { code = "unsafe_path", reason = "Invalid room filename" } end
  if self.files then
    local text = self.files[filename]
    if not text then return nil, { code = "missing_file", reason = "Room file is missing" } end
    return text
  end
  local path = self.directory .. "/" .. filename
  if love and love.filesystem and love.filesystem.getInfo(path) then
    local text, error_message = love.filesystem.read(path)
    if not text then return nil, { code = "read_failed", reason = tostring(error_message) } end
    return text
  end
  local handle = io.open(path, "rb")
  if not handle then return nil, { code = "missing_file", reason = "Room file is missing: " .. path } end
  local text = handle:read("*a")
  handle:close()
  return text
end

function Store:list()
  local manifest_text, failure = self:read("manifest.json")
  if not manifest_text then return nil, failure end
  local manifest, error_message = Json.decode(manifest_text)
  if not manifest then return nil, { code = "invalid_manifest", reason = error_message } end
  if manifest.format ~= "roag.room_manifest" or manifest.version ~= 1 or type(manifest.files) ~= "table" then
    return nil, { code = "invalid_manifest", reason = "Room manifest has unsupported format" }
  end
  local result, seen = {}, {}
  for _, filename in ipairs(manifest.files) do
    if not safe_filename(filename) or seen[filename] then return nil, { code = "invalid_manifest", reason = "Room manifest has invalid filename" } end
    seen[filename] = true
    result[#result + 1] = filename
  end
  table.sort(result)
  return result
end

function Store:filename_for_id(id)
  if type(id) ~= "string" or not id:match("^room%.dungeon%.[a-z0-9_%.]+$") then return nil end
  return id:gsub("^room%.dungeon%.", ""):gsub("%.", "_") .. ".room.json"
end

function Store:write(filename, text)
  if not safe_filename(filename) then return nil, { code = "unsafe_path", reason = "Room storage rejects paths outside the corpus" } end
  if not self:can_write() then return nil, { code = "read_only", reason = "Packaged room content is read-only; run the editor from a source checkout" } end
  if type(text) ~= "string" then return nil, { code = "write_failed", reason = "Room JSON must be text" } end
  if self.files then self.files[filename] = text; return true end
  local path = self.directory .. "/" .. filename
  local handle, error_message = io.open(path, "wb")
  if not handle then return nil, { code = "write_failed", reason = tostring(error_message) } end
  handle:write(text)
  handle:close()
  return true
end

-- The manifest is part of the authored corpus index, not an arbitrary editor
-- path.  Keeping its write path separate preserves the normal room-file path
-- guard while allowing a new validated template to become discoverable.
function Store:write_manifest(files)
  if not self:can_write() then
    return nil, { code = "read_only", reason = "Packaged room content is read-only; run the editor from a source checkout" }
  end
  if type(files) ~= "table" then return nil, { code = "invalid_manifest", reason = "Room manifest files must be an array" } end
  local values, seen = {}, {}
  for _, filename in ipairs(files) do
    if not safe_filename(filename) or seen[filename] then
      return nil, { code = "invalid_manifest", reason = "Room manifest has invalid filename" }
    end
    seen[filename] = true
    values[#values + 1] = filename
  end
  table.sort(values)
  local text, encode_error = Json.encode({ format = "roag.room_manifest", version = 1, files = values })
  if not text then return nil, { code = "write_failed", reason = tostring(encode_error) } end
  if self.files then self.files["manifest.json"] = text; return true end
  local path = self.directory .. "/manifest.json"
  local handle, error_message = io.open(path, "wb")
  if not handle then return nil, { code = "write_failed", reason = tostring(error_message) } end
  handle:write(text)
  handle:close()
  return true
end

function Store.memory(files, writable)
  return Store.new({ files = files or {}, writable = writable ~= false })
end

return Store
