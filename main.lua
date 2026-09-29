-- LÖVE bootstrap. Application flow, simulation, and presentation live in src/.
local App = require("src.app.app")

local app = App.new()

function love.load(...)
  app:load(...)
end

function love.update(dt)
  app:update(dt)
end

function love.draw()
  app:draw()
end

function love.keypressed(...)
  app:keypressed(...)
end

function love.keyreleased(...)
  app:keyreleased(...)
end

function love.focus(focused)
  app:focus(focused)
end
