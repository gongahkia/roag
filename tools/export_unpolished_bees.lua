-- Export the presentation-only art workbench into a fresh sibling checkout.
-- Example: luajit tools/export_unpolished_bees.lua ../unpolished-bees
-- The exporter refuses an existing destination; it never overwrites a user
-- project, active run, profile, or archive.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Export = require("src.presentation.art_workbench_export")
local Json = require("src.persistence.json")

local function usage()
  io.stderr:write("Usage: luajit tools/export_unpolished_bees.lua <new-directory>\n")
end

local function quote(value)
  return "'" .. value:gsub("'", "'\\''") .. "'"
end

local function command_ok(command)
  local first, _, code = os.execute(command)
  return first == true or first == 0 or code == 0
end

local function parent(path)
  return path:match("^(.*)/[^/]+$") or "."
end

local destination = arg and arg[1]
if type(destination) ~= "string" or destination == "" or destination:sub(1, 1) == "-" then
  usage()
  os.exit(2)
end
if not Export.validate() then os.exit(3) end
if command_ok("test -e " .. quote(destination)) then
  io.stderr:write("Refusing to overwrite existing destination: " .. destination .. "\n")
  os.exit(2)
end
assert(command_ok("mkdir -p " .. quote(destination)), "Could not create export destination")

for _, entry in ipairs(Export.entries) do
  local target = destination .. "/" .. entry.destination
  assert(command_ok("mkdir -p " .. quote(parent(target))), "Could not create export directory")
  assert(command_ok("cp -R " .. quote(entry.source) .. " " .. quote(target)), "Could not export " .. entry.source)
end

local manifest, reason = Json.encode(Export.manifest())
assert(manifest, reason)
local file = assert(io.open(destination .. "/unpolished-bees.manifest.json", "wb"))
assert(file:write(manifest))
file:close()
io.write("Exported presentation workbench to " .. destination .. "\n")
