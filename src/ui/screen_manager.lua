-- Declarative, presentation-only screen definitions. Gameplay state and input
-- stay authoritative in App/Session; this module gives artists and UI authors
-- one validated JSON document for title-facing hierarchy and copy.
local Json = require("src.persistence.json")

local ScreenManager = {}
ScreenManager.__index = ScreenManager

ScreenManager.FORMAT = "roag.screen_definitions"
ScreenManager.VERSION = 1
ScreenManager.DEFAULT_PATH = "content/screens/legacy.json"

local VALID_LAYOUTS = { title_menu = true, catalog = true, list_detail = true, route = true, menu = true, notice = true }
local VALID_ACCENTS = { cyan = true, amber = true, mint = true, coral = true, violet = true }

local FALLBACK = {
  format = ScreenManager.FORMAT,
  version = ScreenManager.VERSION,
  screens = {
    { id = "title", layout = "title_menu", title = "ROAG", subtitle = "A ONE-BIT DESCENT", footer = "W/S SELECT     ENTER CONFIRM", accent = "cyan" },
  },
}

local function copy(value)
  local encoded, failure = Json.encode(value)
  assert(encoded, failure)
  local result, decode_failure = Json.decode(encoded)
  assert(result, decode_failure)
  return result
end

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function valid_text(value)
  return type(value) == "string" and value ~= ""
end

function ScreenManager.validate(data)
  if type(data) ~= "table" then return failure("invalid_screen_definitions", "Screen definitions must be an object") end
  if data.format ~= ScreenManager.FORMAT then return failure("invalid_screen_format", "Expected " .. ScreenManager.FORMAT) end
  if data.version ~= ScreenManager.VERSION then return failure("unsupported_screen_version", "Expected version " .. ScreenManager.VERSION) end
  if type(data.screens) ~= "table" or #data.screens == 0 then return failure("invalid_screen_definitions", "At least one screen is required") end
  local ids, result = {}, {}
  for index, screen in ipairs(data.screens) do
    if type(screen) ~= "table" then return failure("invalid_screen", "Screen " .. index .. " must be an object") end
    if type(screen.id) ~= "string" or not screen.id:match("^[a-z][a-z0-9_]*$") then
      return failure("invalid_screen_id", "Screen " .. index .. " has an invalid id")
    end
    if ids[screen.id] then return failure("duplicate_screen_id", "Duplicate screen id " .. screen.id) end
    if not VALID_LAYOUTS[screen.layout] then return failure("invalid_screen_layout", "Screen " .. screen.id .. " has an unknown layout") end
    if not valid_text(screen.title) or not valid_text(screen.subtitle) or not valid_text(screen.footer) then
      return failure("invalid_screen_copy", "Screen " .. screen.id .. " requires title, subtitle, and footer text")
    end
    if not VALID_ACCENTS[screen.accent] then return failure("invalid_screen_accent", "Screen " .. screen.id .. " has an unknown accent") end
    ids[screen.id] = true
    result[#result + 1] = {
      id = screen.id, layout = screen.layout, title = screen.title,
      subtitle = screen.subtitle, footer = screen.footer, accent = screen.accent,
    }
  end
  return result
end

function ScreenManager.decode(payload)
  local data, decode_failure = Json.decode(payload)
  if not data then return nil, { code = "invalid_screen_json", reason = decode_failure } end
  local screens, validation_failure = ScreenManager.validate(data)
  if not screens then return nil, validation_failure end
  return { format = data.format, version = data.version, screens = screens }
end

function ScreenManager.encode(data)
  local screens, validation_failure = ScreenManager.validate(data)
  if not screens then return nil, validation_failure end
  local lines = { "{", "  \"format\": \"" .. ScreenManager.FORMAT .. "\",", "  \"version\": 1,", "  \"screens\": [" }
  for index, screen in ipairs(screens) do
    local encoded, encode_failure = Json.encode(screen)
    if not encoded then return nil, { code = "screen_encode_failed", reason = encode_failure } end
    local suffix = index == #screens and "" or ","
    lines[#lines + 1] = "    " .. encoded .. suffix
  end
  lines[#lines + 1] = "  ]"
  lines[#lines + 1] = "}"
  return table.concat(lines, "\n") .. "\n"
end

local function read_file(path)
  if love and love.filesystem then
    local contents, reason = love.filesystem.read(path)
    if contents then return contents end
    return nil, reason
  end
  local file, reason = io.open(path, "rb")
  if not file then return nil, reason end
  local contents = file:read("*a")
  file:close()
  return contents
end

function ScreenManager.load(options)
  options = options or {}
  local payload, read_failure = options.payload, nil
  if not payload then payload, read_failure = (options.read or read_file)(options.path or ScreenManager.DEFAULT_PATH) end
  if not payload then return nil, { code = "screen_read_failed", reason = tostring(read_failure) } end
  local data, decode_failure = ScreenManager.decode(payload)
  if not data then return nil, decode_failure end
  return ScreenManager.new(data)
end

function ScreenManager.new(data)
  local screens, validation_failure = ScreenManager.validate(data or FALLBACK)
  if not screens then return nil, validation_failure end
  local by_id = {}
  for _, screen in ipairs(screens) do by_id[screen.id] = screen end
  return setmetatable({ format = ScreenManager.FORMAT, version = ScreenManager.VERSION, screens = screens, by_id = by_id }, ScreenManager)
end

function ScreenManager.fallback()
  return assert(ScreenManager.new(copy(FALLBACK)))
end

function ScreenManager:screen(id)
  return self.by_id[id]
end

function ScreenManager:text(id, field, default)
  local screen = self:screen(id)
  return screen and screen[field] or default
end

function ScreenManager:to_data()
  return { format = self.format, version = self.version, screens = copy(self.screens) }
end

return ScreenManager
