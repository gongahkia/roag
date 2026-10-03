-- LÖVE bootstrap. Developer tools are isolated: they never construct normal
-- application/save state. `--screen-editor` is the direct Studio composer.
local app, inspector, room_editor, screen_editor, tool_cursors

local function requested(arguments, flag)
  for _, value in ipairs(arguments or arg or {}) do
    if value == flag then return true end
  end
  return false
end

function love.load(...)
  local arguments = ({ ... })[1]
  if requested(arguments, "--generation-inspector") then
    inspector = require("level_editor.generation_inspector").new()
    inspector:fit(love.graphics.getDimensions())
  elseif requested(arguments, "--room-editor") then
    room_editor = require("level_editor.room_editor").new()
  elseif requested(arguments, "--screen-editor") then
    screen_editor = require("studio.screen_editor").new()
    tool_cursors = require("src.ui.cursor_manager").new()
    tool_cursors:load()
    tool_cursors:set("default")
  else
    app = require("src.app.app").new()
    app:load(...)
  end
end

function love.update(dt)
  if inspector then inspector:update(dt) elseif room_editor then room_editor:update(dt) elseif screen_editor and screen_editor.update then screen_editor:update(dt) else app:update(dt) end
end

function love.draw()
  if inspector then inspector:draw() elseif room_editor then room_editor:draw() elseif screen_editor then screen_editor:draw() else app:draw() end
end

function love.keypressed(...)
  if inspector then
    inspector:keypressed(...)
  elseif room_editor then
    room_editor:keypressed(...)
  elseif screen_editor then
    if screen_editor:keypressed(...) == "back" then love.event.quit() end
  else
    app:keypressed(...)
  end
end

function love.keyreleased(...)
  if not inspector and not room_editor and not screen_editor then app:keyreleased(...) end
end

function love.textinput(...)
  if room_editor then room_editor:textinput(...) elseif screen_editor then screen_editor:textinput(...) end
end

function love.mousepressed(...)
  if inspector then inspector:mousepressed(...) elseif room_editor then room_editor:mousepressed(...) elseif screen_editor then screen_editor:mousepressed(...) else app:mousepressed(...) end
end

function love.mousereleased(...)
  if inspector then inspector:mousereleased(...) elseif room_editor then room_editor:mousereleased(...) elseif screen_editor and screen_editor.mousereleased then screen_editor:mousereleased(...) elseif app then app:mousereleased(...) end
end

function love.mousemoved(...)
  if inspector then inspector:mousemoved(...) elseif room_editor then room_editor:mousemoved(...) elseif screen_editor and screen_editor.mousemoved then screen_editor:mousemoved(...) elseif app then app:mousemoved(...) end
end

function love.wheelmoved(...)
  if inspector then inspector:wheelmoved(...) elseif room_editor then room_editor:wheelmoved(...) elseif screen_editor and screen_editor.wheelmoved then screen_editor:wheelmoved(...) end
end

function love.resize(width, height)
  if inspector then inspector:resize(width, height) end
  if room_editor then room_editor:resize(width, height) end
  if screen_editor and screen_editor.resize then screen_editor:resize(width, height) end
end

function love.focus(focused)
  if not inspector and not room_editor and not screen_editor then app:focus(focused) end
end

function love.quit()
  -- Regular safe-boundary autosaves are the primary protection. This final
  -- best-effort checkpoint only runs when LÖVE delivers a normal quit event.
  if app then app:autosave("quit") end
end
