-- Standalone copy: `love sprite_editor` cannot assume the parent project is
-- mounted, so it reads the local mirror of Kenney Cursor Pack.
local CursorManager = {}
CursorManager.__index = CursorManager

local files = { default = "pointer_a.png", action = "hand_point.png", pan = "hand_closed.png", picker = "drawing_picker.png", disabled = "cursor_disabled.png" }
local hotspots = { default = { 0, 0 }, action = { 2, 1 }, pan = { 8, 8 }, picker = { 1, 1 }, disabled = { 0, 0 } }

function CursorManager.new()
  return setmetatable({ cursors = {}, current = nil }, CursorManager)
end

function CursorManager:load()
  for kind, filename in pairs(files) do
    local ok, cursor = pcall(function()
      local image = love.image.newImageData("cursors/" .. filename)
      return love.mouse.newCursor(image, hotspots[kind][1], hotspots[kind][2])
    end)
    if ok then self.cursors[kind] = cursor end
  end
end

function CursorManager:set(kind)
  if kind == self.current then return end
  local cursor = self.cursors[kind] or self.cursors.default
  if cursor and pcall(love.mouse.setCursor, cursor) then self.current = kind end
end

return CursorManager
