-- File boundary for the Modifier Studio page. Runtime reads the immutable
-- corpus; only this explicitly source-checkout-oriented store may write it.
local Json = require("src.persistence.json")
local Definitions = require("src.expedition.modifiers")

local Store = {}
Store.__index = Store

local function safe_filename(filename)
  return type(filename) == "string" and filename:match("^[a-z0-9_%-]+%.json$") and filename ~= "manifest.json"
end

function Store.new(options)
  options = options or {}
  return setmetatable({ directory = options.directory or Definitions.DIRECTORY, files = options.files, writable = options.writable == true }, Store)
end

function Store.memory(files, writable, options)
  options = options or {}; options.files, options.writable = files or {}, writable ~= false
  return Store.new(options)
end

function Store:can_write() return self.files and self.writable or self.writable end
function Store:_path(filename) return self.directory .. "/" .. filename end
function Store:read(filename)
  if filename ~= "manifest.json" and not safe_filename(filename) then return nil, { code = "unsafe_path", reason = "Modifier storage rejects paths outside the content directory" } end
  if self.files then
    if not self.files[filename] then return nil, { code = "missing_file", reason = "Modifier file is missing" } end
    return self.files[filename]
  end
  local handle, reason = io.open(self:_path(filename), "rb")
  if not handle then return nil, { code = "missing_file", reason = tostring(reason) } end
  local text = handle:read("*a"); handle:close(); return text
end
function Store:list()
  local text, failure = self:read("manifest.json"); if not text then return nil, failure end
  local manifest, reason = Json.decode(text)
  if not manifest or manifest.format ~= Definitions.MANIFEST_FORMAT or manifest.version ~= Definitions.MANIFEST_VERSION or type(manifest.files) ~= "table" then return nil, { code = "invalid_manifest", reason = tostring(reason or "Unsupported modifier manifest") } end
  local files, seen = {}, {}
  for _, filename in ipairs(manifest.files) do
    if not safe_filename(filename) or seen[filename] then return nil, { code = "invalid_manifest", reason = "Manifest contains an invalid filename" } end
    seen[filename], files[#files + 1] = true, filename
  end
  table.sort(files); return files
end
function Store:filename_for_id(id)
  if type(id) ~= "string" or not id:match("^expedition%.passive%.[a-z0-9_]+$") then return nil end
  return id:gsub("^expedition%.passive%.", "") .. ".json"
end
function Store:_write(filename, text)
  if not self:can_write() then return nil, { code = "read_only", reason = "Modifier content is read-only outside a writable source checkout" } end
  if self.files then self.files[filename] = text; return true end
  -- A complete temporary file is validated before replace, so an I/O failure
  -- cannot truncate the original production definition.
  local temporary = self:_path(filename .. ".tmp")
  local handle, reason = io.open(temporary, "wb")
  if not handle then return nil, { code = "write_failed", reason = tostring(reason) } end
  local ok, write_reason = handle:write(text); handle:close()
  if not ok then os.remove(temporary); return nil, { code = "write_failed", reason = tostring(write_reason) } end
  local decoded, decode_reason = Json.decode(text)
  if not decoded then os.remove(temporary); return nil, { code = "write_failed", reason = tostring(decode_reason) } end
  local replaced, replace_reason = os.rename(temporary, self:_path(filename))
  if not replaced then os.remove(temporary); return nil, { code = "write_failed", reason = tostring(replace_reason) } end
  return true
end
function Store:write_definition(filename, definition, registry)
  if not safe_filename(filename) then return nil, { code = "unsafe_path", reason = "Invalid modifier filename" } end
  local ok, failure = Definitions.validate(definition, { file = self:_path(filename), registry = registry })
  if not ok then return nil, failure end
  local text, reason = Definitions.canonical_json(definition)
  if not text then return nil, { code = "encode_failed", reason = tostring(reason) } end
  return self:_write(filename, text)
end
function Store:write_manifest(files)
  if not self:can_write() then return nil, { code = "read_only", reason = "Modifier content is read-only outside a writable source checkout" } end
  local values, seen = {}, {}
  for _, filename in ipairs(files or {}) do
    if not safe_filename(filename) or seen[filename] then return nil, { code = "invalid_manifest", reason = "Manifest contains an invalid filename" } end
    seen[filename], values[#values + 1] = true, filename
  end
  table.sort(values)
  local text = assert(Definitions.canonical_json({ format = Definitions.MANIFEST_FORMAT, version = Definitions.MANIFEST_VERSION, files = values }):gsub("\n$", ""))
  return self:_write("manifest.json", text)
end
function Store:delete(filename)
  if not safe_filename(filename) then return nil, { code = "unsafe_path", reason = "Invalid modifier filename" } end
  if not self:can_write() then return nil, { code = "read_only", reason = "Modifier content is read-only outside a writable source checkout" } end
  if self.files then self.files[filename] = nil; return true end
  local ok, reason = os.remove(self:_path(filename)); if not ok then return nil, { code = "delete_failed", reason = tostring(reason) } end
  return true
end

return Store
