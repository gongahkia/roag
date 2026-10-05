-- Docker-first command line for deterministic local ROAG diagnostics.
package.path = "./?.lua;./?/init.lua;" .. package.path

local DebugCockpit = require("src.tools.debug_cockpit")
local Json = require("src.persistence.json")

local function usage()
  return table.concat({
    "ROAG debug cockpit",
    "  doctor                         Validate content and report local contracts.",
    "  modifier --id ID [--stacks N] [--trigger EVENT] [--tags a,b] [--capabilities a,b]",
    "  expedition [--seed N] [--character ID]",
    "  scenario --name bomb-self [--seed N]",
    "  determinism [--seed N] [--character ID]",
    "  bundle [--label NAME]           Write a reproducible JSON bundle under .roag-debug/.",
    "  Every command accepts --out NAME to write .roag-debug/NAME.json.",
  }, "\n")
end

local function parse(argv)
  local command, options = argv[1] or "help", {}
  local index = 2
  while index <= #argv do
    local key = argv[index]
    if key:sub(1, 2) ~= "--" then error("Unexpected argument '" .. tostring(key) .. "'") end
    key = key:sub(3):gsub("-", "_")
    local value = argv[index + 1]
    if not value or value:sub(1, 2) == "--" then value = "true" else index = index + 1 end
    options[key] = value
    index = index + 1
  end
  return command, options
end

local function safe_name(value)
  assert(type(value) == "string" and value:match("^[a-zA-Z0-9][a-zA-Z0-9_.%-]*$"), "Output name must contain only letters, numbers, dot, underscore, or dash")
  return value
end

local function emit(command, data, options)
  local report = { format = "roag.debug_report", version = 1, command = command, data = data }
  local text = assert(Json.encode(report))
  if options.out then
    local name = safe_name(options.out)
    assert(os.execute("mkdir -p .roag-debug"))
    local handle = assert(io.open(".roag-debug/" .. name .. ".json", "wb"))
    assert(handle:write(text .. "\n")); handle:close()
    io.write("Wrote .roag-debug/", name, ".json\n")
  else
    io.write(text, "\n")
  end
end

local function git_output(command)
  local handle = assert(io.popen(command .. " 2>/dev/null", "r"))
  local text = handle:read("*a"); handle:close()
  return text:gsub("%s+$", "")
end

local command, options = parse(arg or {})
if command == "help" or command == "--help" then
  io.write(usage(), "\n")
  return
end

local ok, data = xpcall(function()
  if command == "doctor" then return DebugCockpit.doctor() end
  if command == "modifier" then return DebugCockpit.modifier(options) end
  if command == "expedition" then return DebugCockpit.expedition(options) end
  if command == "scenario" then
    assert(options.name == "bomb-self", "Supported scenario: bomb-self")
    return DebugCockpit.bomb_self(options)
  end
  if command == "determinism" then return DebugCockpit.determinism(options) end
  if command == "bundle" then
    local bundle = {
      doctor = DebugCockpit.doctor(),
      bomb_self = DebugCockpit.bomb_self(options),
      expedition = DebugCockpit.expedition(options),
      determinism = DebugCockpit.determinism(options),
      repository = { head = git_output("git rev-parse HEAD"), status = git_output("git status --short --branch") },
    }
    options.out = options.out or options.label or "bundle"
    return bundle
  end
  error("Unknown debug command '" .. tostring(command) .. "'\n" .. usage())
end, debug.traceback)

if not ok then
  io.stderr:write("ROAG DEBUG ERROR\n", data, "\n")
  os.exit(1)
end
emit(command, data, options)
