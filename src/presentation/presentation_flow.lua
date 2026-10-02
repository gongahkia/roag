-- Declarative navigation copy for ROAG's title-facing presentation.  This is
-- deliberately narrower than gameplay: content can order and label the
-- supported actions, but App remains the sole owner of what an action does.
local Json = require("src.persistence.json")

local Flow = {}
Flow.__index = Flow

Flow.FORMAT = "roag.presentation_flow"
Flow.VERSION = 1
Flow.DEFAULT_PATH = "content/presentation/flow.json"

local ACTIONS = {
  new_run = { target = "replace_save" },
  continue = { target = "game", conditional = "continue_available" },
  research = { target = "research" },
  fallen = { target = "fallen_archive" },
  help = { target = "help" },
}

local FALLBACK = {
  format = Flow.FORMAT,
  version = Flow.VERSION,
  home = "title",
  title_actions = {
    { id = "new_run", label = "NEW RUN", description = "Begin a new descent.", target = "replace_save" },
    { id = "continue", label = "CONTINUE", description = "Resume the current active run.", target = "game" },
    { id = "research", label = "RESEARCH", description = "Spend persistent RESEARCH DATA on future runs.", target = "research" },
    { id = "fallen", label = "FALLEN", description = "Inspect bodies lost on earlier descents.", target = "fallen_archive" },
    { id = "help", label = "HOW TO PLAY", description = "Read the core loop and current controls.", target = "help" },
  },
}

local function copy(value)
  local encoded, reason = Json.encode(value)
  assert(encoded, reason)
  return assert(Json.decode(encoded))
end

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function text(value)
  return type(value) == "string" and value ~= ""
end

function Flow.actions()
  local result = {}
  for id, detail in pairs(ACTIONS) do result[id] = { target = detail.target, conditional = detail.conditional } end
  return result
end

function Flow.validate(data)
  if type(data) ~= "table" then return failure("invalid_presentation_flow", "Presentation flow must be an object") end
  if data.format ~= Flow.FORMAT then return failure("invalid_presentation_flow_format", "Expected " .. Flow.FORMAT) end
  if data.version ~= Flow.VERSION then return failure("unsupported_presentation_flow_version", "Expected version " .. Flow.VERSION) end
  if data.home ~= "title" then return failure("invalid_presentation_flow_home", "The presentation home must be title") end
  if type(data.title_actions) ~= "table" or #data.title_actions == 0 then
    return failure("invalid_presentation_actions", "At least one title action is required")
  end
  local seen, actions = {}, {}
  for index, action in ipairs(data.title_actions) do
    if type(action) ~= "table" or not ACTIONS[action.id] then
      return failure("invalid_presentation_action", "Title action " .. index .. " has an unknown id")
    end
    if seen[action.id] then return failure("duplicate_presentation_action", "Title action " .. action.id .. " appears twice") end
    if not text(action.label) or not text(action.description) then
      return failure("invalid_presentation_action_copy", "Title action " .. action.id .. " requires label and description")
    end
    if action.target ~= ACTIONS[action.id].target then
      return failure("invalid_presentation_transition", "Title action " .. action.id .. " has an invalid target")
    end
    seen[action.id] = true
    actions[#actions + 1] = { id = action.id, label = action.label, description = action.description, target = action.target }
  end
  if not seen.new_run then return failure("missing_presentation_action", "NEW RUN must remain available") end
  return actions
end

function Flow.decode(payload)
  local data, reason = Json.decode(payload)
  if not data then return failure("invalid_presentation_flow_json", tostring(reason)) end
  local actions, validation = Flow.validate(data)
  if not actions then return nil, validation end
  return { format = data.format, version = data.version, home = data.home, title_actions = actions }
end

function Flow.encode(data)
  local actions, validation = Flow.validate(data)
  if not actions then return nil, validation end
  return Json.encode({ format = Flow.FORMAT, version = Flow.VERSION, home = "title", title_actions = actions })
end

local function read_file(path)
  if love and love.filesystem then return love.filesystem.read(path) end
  local file, reason = io.open(path, "rb")
  if not file then return nil, reason end
  local payload = file:read("*a")
  file:close()
  return payload
end

function Flow.load(options)
  options = options or {}
  local payload, reason = options.payload, nil
  if not payload then payload, reason = (options.read or read_file)(options.path or Flow.DEFAULT_PATH) end
  if not payload then return failure("presentation_flow_read_failed", tostring(reason)) end
  local data, failure_data = Flow.decode(payload)
  if not data then return nil, failure_data end
  return Flow.new(data)
end

function Flow.new(data)
  local actions, validation = Flow.validate(data or FALLBACK)
  if not actions then return nil, validation end
  local by_id = {}
  for _, action in ipairs(actions) do by_id[action.id] = action end
  return setmetatable({ format = Flow.FORMAT, version = Flow.VERSION, home = "title", title_actions = actions, by_id = by_id }, Flow)
end

function Flow.fallback()
  return assert(Flow.new(copy(FALLBACK)))
end

function Flow:available(context)
  local result = {}
  for _, action in ipairs(self.title_actions) do
    local detail = ACTIONS[action.id]
    if not detail.conditional or context[detail.conditional] then result[#result + 1] = action end
  end
  return result
end

function Flow:to_data()
  return { format = self.format, version = self.version, home = self.home, title_actions = copy(self.title_actions) }
end

return Flow
