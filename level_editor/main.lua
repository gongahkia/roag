-- Standalone developer entry point for ROAG's level-facing tooling.
--
-- Launch from the repository root:
--   love level_editor                       # generation inspector
--   love level_editor --room-editor         # room-template authoring
--
-- The actual simulation and room schema remain shared authoritative modules
-- in ../src.  This entry point intentionally creates no App or active-run
-- save store.
local source = love.filesystem.getSource()
local repository_root = source:match("^(.*)/level_editor$")
if repository_root then
  package.path = source .. "/?.lua;" .. source .. "/?/init.lua;"
    .. repository_root .. "/?.lua;" .. repository_root .. "/?/init.lua;" .. package.path
end

local tool
local cursor_manager

local function requested(arguments, flag)
  for _, value in ipairs(arguments or arg or {}) do
    if value == flag then return true end
  end
  return false
end

function love.load(...)
  local arguments = ({ ... })[1]
  if requested(arguments, "--room-editor") then
    tool = require("room_editor").new()
  else
    tool = require("generation_inspector").new()
    tool:fit(love.graphics.getDimensions())
  end
  -- The generic editor cursor is unrelated to world/actor rendering and lets
  -- this retained authoring tool share the repository's single cursor source.
  cursor_manager = require("src.ui.cursor_manager").new((repository_root or ".") .. "/assets/cursors/kenney/PNG/Basic/Default/")
  cursor_manager:load()
  cursor_manager:set("default")
end

function love.update(dt)
  tool:update(dt)
  if cursor_manager then cursor_manager:set(tool.dragging and "pan" or "action") end
end
function love.draw() tool:draw() end
function love.keypressed(...) tool:keypressed(...) end
function love.textinput(...) if tool.textinput then tool:textinput(...) end end
function love.mousepressed(...) tool:mousepressed(...) end
function love.mousereleased(...) tool:mousereleased(...) end
function love.mousemoved(...) tool:mousemoved(...) end
function love.wheelmoved(...) tool:wheelmoved(...) end
function love.resize(...) if tool.resize then tool:resize(...) end end
