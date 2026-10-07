-- Safe, narrow filesystem surface for Expedition Workbench JSON.  The store
-- cannot address arbitrary paths and writes files atomically.
local Json = require("src.persistence.json")
local Writer = require("src.rooms.json_writer")

local Store = {}
Store.__index = Store

local ROOTS = { chambers = "chambers", encounters = "encounters" }
local function valid_kind(kind) return ROOTS[kind] ~= nil end
local function valid_file(filename) return type(filename) == "string" and filename:match("^[a-z0-9_%-]+%.json$") and filename ~= "manifest.json" end
local function join(a, b) return a .. "/" .. b end
local function read_file(path)
  if love and love.filesystem and love.filesystem.getInfo(path) then return love.filesystem.read(path) end
  local handle = io.open(path, "rb"); if not handle then return nil end
  local text = handle:read("*a"); handle:close(); return text
end
local function write_file(path, text)
  local handle, err = io.open(path, "wb"); if not handle then return nil, err end
  local ok, write_err = handle:write(text); handle:close(); return ok and true or nil, write_err
end

function Store.new(options)
  options = options or {}
  return setmetatable({ root = options.root or "content/expedition" }, Store)
end
function Store:path(kind, filename)
  assert(valid_kind(kind), "invalid Expedition content kind")
  assert(filename == "manifest.json" or valid_file(filename), "unsafe Expedition content filename")
  return join(join(self.root, ROOTS[kind]), filename)
end
function Store:read(kind, filename)
  return read_file(self:path(kind, filename))
end
function Store:list(kind)
  local manifest = self:read(kind, "manifest.json")
  if not manifest then return nil, { code = "missing_manifest", reason = "Expedition " .. kind .. " manifest is missing" } end
  local value, err = Json.decode(manifest)
  if not value or type(value.files) ~= "table" then return nil, { code = "invalid_manifest", reason = tostring(err or "manifest.files is required") } end
  local files, seen = {}, {}
  for _, filename in ipairs(value.files) do
    if not valid_file(filename) or seen[filename] then return nil, { code = "invalid_manifest", reason = "Manifest has an unsafe or duplicate filename" } end
    seen[filename] = true; files[#files + 1] = filename
  end
  table.sort(files); return files
end
function Store:write(kind, filename, text)
  if not valid_file(filename) then return nil, { code = "unsafe_filename", reason = "Expedition content filenames must be safe JSON basenames" } end
  local path, temp = self:path(kind, filename), self:path(kind, filename) .. ".tmp"
  local ok, err = write_file(temp, text)
  if not ok then return nil, { code = "write_failed", reason = tostring(err) } end
  local renamed, rename_err = os.rename(temp, path)
  if not renamed then os.remove(temp); return nil, { code = "replace_failed", reason = tostring(rename_err) } end
  return true
end
function Store:write_manifest(kind, files)
  local ordered, seen = {}, {}
  for _, filename in ipairs(files or {}) do
    if not valid_file(filename) or seen[filename] then return nil, { code = "invalid_manifest", reason = "Cannot write unsafe or duplicate manifest entry" } end
    seen[filename] = true; ordered[#ordered + 1] = filename
  end
  table.sort(ordered)
  local text, err = Writer.encode({ files = ordered })
  if not text then return nil, { code = "encode_failed", reason = tostring(err) } end
  local path, temp = self:path(kind, "manifest.json"), self:path(kind, "manifest.json") .. ".tmp"
  local ok, write_err = write_file(temp, text)
  if not ok then return nil, { code = "write_failed", reason = tostring(write_err) } end
  local renamed, rename_err = os.rename(temp, path)
  if not renamed then os.remove(temp); return nil, { code = "replace_failed", reason = tostring(rename_err) } end
  return true
end
function Store:remove(kind, filename)
  if not valid_file(filename) then return nil, { code = "unsafe_filename" } end
  local removed, err = os.remove(self:path(kind, filename))
  if not removed then return nil, { code = "remove_failed", reason = tostring(err) } end
  local files, list_err = self:list(kind); if not files then return nil, list_err end
  local kept = {}; for _, value in ipairs(files) do if value ~= filename then kept[#kept + 1] = value end end
  return self:write_manifest(kind, kept)
end
function Store:filename_for(kind, id)
  local tail = type(id) == "string" and id:match("^[^%.]+%.(.+)$") or nil
  if not tail then return nil end
  local filename = tail:gsub("[^a-z0-9_%-]", "_") .. ".json"
  return valid_file(filename) and filename or nil
end

return Store
