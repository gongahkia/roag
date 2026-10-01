-- LÖVE bootstrap. The generation inspector is an isolated developer mode;
-- normal application/save construction does not occur when it is requested.
local app, inspector

local function inspector_requested(arguments)
  for _, value in ipairs(arguments or arg or {}) do
    if value == "--generation-inspector" then return true end
  end
  return false
end

function love.load(...)
  if inspector_requested(({ ... })[1]) then
    inspector = require("src.tools.generation_inspector").new()
    inspector:fit(love.graphics.getDimensions())
  else
    app = require("src.app.app").new()
    app:load(...)
  end
end

function love.update(dt)
  if inspector then inspector:update(dt) else app:update(dt) end
end

function love.draw()
  if inspector then inspector:draw() else app:draw() end
end

function love.keypressed(...)
  if inspector then inspector:keypressed(...) else app:keypressed(...) end
end

function love.keyreleased(...)
  if not inspector then app:keyreleased(...) end
end

function love.mousepressed(...)
  if inspector then inspector:mousepressed(...) end
end

function love.mousereleased(...)
  if inspector then inspector:mousereleased(...) end
end

function love.mousemoved(...)
  if inspector then inspector:mousemoved(...) end
end

function love.wheelmoved(...)
  if inspector then inspector:wheelmoved(...) end
end

function love.resize(width, height)
  if inspector then inspector:resize(width, height) end
end

function love.focus(focused)
  if not inspector then app:focus(focused) end
end

function love.quit()
  -- Regular safe-boundary autosaves are the primary protection. This final
  -- best-effort checkpoint only runs when LÖVE delivers a normal quit event.
  if app then app:autosave("quit") end
end
