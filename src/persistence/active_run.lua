-- Versioned active-run envelope and restoration boundary.  Storage is kept
-- separate so this module is fully usable by headless tests and tools.
local Json = require("src.persistence.json")
local Session = require("src.simulation.session")

local ActiveRun = {
  FORMAT = "roag.active_run",
  VERSION = 1,
}

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

function ActiveRun.encode_session(session)
  local ok, data_or_error = pcall(function()
    return session:to_data()
  end)
  if not ok then return failure("invalid_state", tostring(data_or_error)) end
  local text, reason = Json.encode({ format = ActiveRun.FORMAT, version = ActiveRun.VERSION, run = data_or_error })
  if not text then return failure("encode_failed", tostring(reason)) end
  return text
end

function ActiveRun.decode_envelope(text)
  local envelope, reason = Json.decode(text)
  if not envelope then return failure("invalid_json", tostring(reason)) end
  if type(envelope) ~= "table" then return failure("invalid_state", "Save envelope must be an object") end
  if envelope.format ~= ActiveRun.FORMAT then return failure("unsupported_format", "Save format is not roag.active_run") end
  if envelope.version ~= ActiveRun.VERSION then return failure("unsupported_version", "Save version is not supported") end
  if type(envelope.run) ~= "table" then return failure("invalid_state", "Save envelope is missing run data") end
  return envelope
end

function ActiveRun.decode_session(text, options)
  local envelope, error_data = ActiveRun.decode_envelope(text)
  if not envelope then return nil, error_data end
  local ok, session_or_error = xpcall(function()
    return Session.from_data(envelope.run, options)
  end, debug.traceback)
  if not ok then
    local message = tostring(session_or_error)
    local code = (message:match("Missing .- content") or message:match("Unknown .- ID")) and "missing_content" or "invalid_state"
    return failure(code, message)
  end
  return session_or_error
end

function ActiveRun.save(session, store)
  local text, error_data = ActiveRun.encode_session(session)
  if not text then return nil, error_data end
  local written, write_error = store:write(text)
  if not written then return nil, write_error or { code = "write_failed", reason = "Could not write active run" } end
  return true
end

function ActiveRun.load(store, options)
  local text, read_error = store:read()
  if not text then return nil, read_error end
  return ActiveRun.decode_session(text, options)
end

function ActiveRun.has_valid_save(store, options)
  local session, error_data = ActiveRun.load(store, options)
  if session then return true end
  return false, error_data
end

function ActiveRun.retire(store)
  local removed, error_data = store:delete()
  if not removed then return nil, error_data or { code = "delete_failed", reason = "Could not retire active run" } end
  return true
end

return ActiveRun
