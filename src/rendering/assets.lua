local Assets = {}
Assets.__index = Assets

local function clone_sprites(source)
  local result = {}
  for kind, tile in pairs(source) do
    result[kind] = { tile[1], tile[2] }
  end
  return result
end

function Assets.new()
  local sprites = require("sprite_map")
  return setmetatable({
    sprites = clone_sprites(sprites),
    default_sprites = clone_sprites(sprites),
  }, Assets)
end

function Assets:_mapping_contents()
  local source = love.filesystem.getSource()
  if source:sub(-5):lower() == ".love" then
    local path = love.filesystem.getSourceBaseDirectory() .. "/sprite_editor/mappings.json"
    local file = io.open(path, "rb")
    if file then
      local contents = file:read("*a")
      file:close()
      return contents
    end
  end
  return love.filesystem.read("sprite_editor/mappings.json")
end

function Assets:refresh_sprite_mappings()
  local contents = self:_mapping_contents()
  if not contents then
    return
  end
  for kind, column, row in contents:gmatch('\"([%w_]+)\"%s*:%s*{%s*\"column\"%s*:%s*(%d+)%s*,%s*\"row\"%s*:%s*(%d+)%s*}') do
    column, row = tonumber(column), tonumber(row)
    if self.default_sprites[kind] and column >= 1 and column <= 49 and row >= 1 and row <= 22 then
      self.sprites[kind] = { column, row }
    end
  end
end

function Assets:reset_sprite(kind)
  local tile = self.default_sprites[kind]
  self.sprites[kind] = { tile[1], tile[2] }
end

function Assets:load()
  love.graphics.setDefaultFilter("nearest", "nearest")
  self.font = love.graphics.newFont("assets/fonts/BigBlueTermPlusNerdFontMono-Regular.ttf", 16)
  love.graphics.setFont(self.font)
  self.sheet = love.graphics.newImage("assets/kenney/Tilesheet/colored-transparent_packed.png")
  self.quads = {}
  for column = 1, 49 do
    for row = 1, 22 do
      self.quads[column .. ":" .. row] = love.graphics.newQuad((column - 1) * 16, (row - 1) * 16, 16, 16, self.sheet)
    end
  end
  self:refresh_sprite_mappings()
end

function Assets:quad(kind)
  local sprite = self.sprites[kind] or self.sprites.target
  return self.quads[sprite[1] .. ":" .. sprite[2]]
end

function Assets:draw_sprite(kind, x, y, size, tint)
  if tint then
    love.graphics.setColor(tint[1], tint[2], tint[3], tint[4] or 1)
  else
    love.graphics.setColor(1, 1, 1)
  end
  love.graphics.draw(self.sheet, self:quad(kind), x, y, 0, size / 16, size / 16)
  love.graphics.setColor(1, 1, 1)
end

return Assets
