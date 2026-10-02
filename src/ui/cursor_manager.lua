-- Presentation-only cursor manager shared by normal ROAG and development
-- tools mounted from the repository root. It intentionally fails closed to
-- the operating-system cursor when a platform cannot create custom cursors.
local CursorManager = {}
CursorManager.__index = CursorManager

local FILES = {
  default = "pointer_a.png",
  action = "hand_point.png",
  pan = "hand_closed.png",
  picker = "drawing_picker.png",
  disabled = "cursor_disabled.png",
}

local HOTSPOTS = {
  default = { 0, 0 }, action = { 2, 1 }, pan = { 8, 8 }, picker = { 1, 1 }, disabled = { 0, 0 },
}

function CursorManager.new(directory)
  return setmetatable({ directory = directory or "assets/cursors/kenney/PNG/Basic/Default/", cursors = {}, current = nil }, CursorManager)
end

function CursorManager:load()
  if not (love and love.image and love.mouse) then return false end
  for kind, filename in pairs(FILES) do
    local ok, cursor = pcall(function()
      local data = love.image.newImageData(self.directory .. filename)
      local hotspot = HOTSPOTS[kind]
      return love.mouse.newCursor(data, hotspot[1], hotspot[2])
    end)
    if ok then self.cursors[kind] = cursor end
  end
  return self.cursors.default ~= nil
end

function CursorManager:set(kind)
  if self.current == kind then return true end
  if not (love and love.mouse and love.mouse.setCursor) then return false end
  local cursor = self.cursors[kind] or self.cursors.default
  if not cursor then return false end
  local ok = pcall(love.mouse.setCursor, cursor)
  if ok then self.current = kind end
  return ok
end

function CursorManager:reset()
  if love and love.mouse and love.mouse.setCursor then pcall(love.mouse.setCursor) end
  self.current = nil
end

return CursorManager
