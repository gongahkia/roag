-- Storage boundary for the single active-run file.  Headless tests inject an
-- in-memory store; LÖVE runtime uses its writable filesystem rather than the
-- directory containing the packaged game.
local SaveStore = {}
SaveStore.__index = SaveStore
SaveStore.ACTIVE_RUN_FILE = "active_run.json"

function SaveStore.memory(initial)
  return setmetatable({ value = initial }, SaveStore)
end

function SaveStore:exists()
  return self.value ~= nil
end

function SaveStore:read()
  if self.value == nil then return nil, { code = "missing_file", reason = "No active run exists" } end
  return self.value
end

function SaveStore:write(payload)
  if type(payload) ~= "string" then return nil, { code = "write_failed", reason = "Save payload must be text" } end
  -- The assignment is atomic for the test boundary and deliberately replaces
  -- only the one active slot.
  self.value = payload
  return true
end

function SaveStore:delete()
  self.value = nil
  return true
end

local LoveStore = {}
LoveStore.__index = LoveStore

function LoveStore.new(filename)
  return setmetatable({ filename = filename or SaveStore.ACTIVE_RUN_FILE }, LoveStore)
end

function LoveStore:exists()
  return love and love.filesystem and love.filesystem.getInfo(self.filename) ~= nil
end

function LoveStore:read()
  if not self:exists() then return nil, { code = "missing_file", reason = "No active run exists" } end
  local ok, payload, reason = pcall(love.filesystem.read, self.filename)
  if not ok or not payload then return nil, { code = "read_failed", reason = tostring(reason or payload) } end
  return payload
end

function LoveStore:write(payload)
  if type(payload) ~= "string" then return nil, { code = "write_failed", reason = "Save payload must be text" } end
  local temporary = self.filename .. ".tmp"
  local ok, written = pcall(love.filesystem.write, temporary, payload)
  if not ok or not written then return nil, { code = "write_failed", reason = tostring(written) } end
  -- Validate the completed temporary file before replacing the sole active
  -- save. LÖVE exposes no portable rename on every supported backend, so the
  -- old save remains untouched until this final write succeeds.
  local verified = love.filesystem.read(temporary)
  if verified ~= payload then return nil, { code = "write_failed", reason = "Temporary save verification failed" } end
  local final_ok, final_written = pcall(love.filesystem.write, self.filename, payload)
  love.filesystem.remove(temporary)
  if not final_ok or not final_written then return nil, { code = "write_failed", reason = tostring(final_written) } end
  return true
end

function LoveStore:delete()
  if self:exists() and not love.filesystem.remove(self.filename) then
    return nil, { code = "delete_failed", reason = "Could not retire active run" }
  end
  return true
end

function SaveStore.runtime(filename)
  if love and love.filesystem then return LoveStore.new(filename) end
  return SaveStore.memory()
end

return SaveStore
