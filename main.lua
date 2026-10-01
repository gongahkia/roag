-- LÖVE bootstrap. The generation inspector is an isolated developer mode;
-- normal application/save construction does not occur when it is requested.
local app, inspector, room_editor

local function requested(arguments, flag)
  for _, value in ipairs(arguments or arg or {}) do
    if value == flag then return true end
  end
  return false
end

function love.load(...)
  local arguments = ({ ... })[1]
  if requested(arguments, "--generation-inspector") then
    inspector = require("src.tools.generation_inspector").new()
    inspector:fit(love.graphics.getDimensions())
  elseif requested(arguments, "--room-editor") then
    room_editor = require("src.tools.room_editor").new()
  else
    app = require("src.app.app").new()
    app:load(...)
  end
end

function love.update(dt)
  if inspector then inspector:update(dt) elseif room_editor then room_editor:update(dt) else app:update(dt) end
end

function love.draw()
  if inspector then inspector:draw() elseif room_editor then room_editor:draw() else app:draw() end
end

function love.keypressed(...)
  if inspector then inspector:keypressed(...) elseif room_editor then room_editor:keypressed(...) else app:keypressed(...) end
end

function love.keyreleased(...)
  if not inspector and not room_editor then app:keyreleased(...) end
end

function love.textinput(...)
  if room_editor then room_editor:textinput(...) end
end

function love.mousepressed(...)
  if inspector then inspector:mousepressed(...) elseif room_editor then room_editor:mousepressed(...) end
end

function love.mousereleased(...)
  if inspector then inspector:mousereleased(...) elseif room_editor then room_editor:mousereleased(...) end
end

function love.mousemoved(...)
  if inspector then inspector:mousemoved(...) elseif room_editor then room_editor:mousemoved(...) end
end

function love.wheelmoved(...)
  if inspector then inspector:wheelmoved(...) elseif room_editor then room_editor:wheelmoved(...) end
end

function love.resize(width, height)
  if inspector then inspector:resize(width, height) end
  if room_editor then room_editor:resize(width, height) end
end

function love.focus(focused)
  if not inspector and not room_editor then app:focus(focused) end
end

function love.quit()
  -- Regular safe-boundary autosaves are the primary protection. This final
  -- best-effort checkpoint only runs when LÖVE delivers a normal quit event.
  if app then app:autosave("quit") end
end
