-- Non-authoritative visual state. Simulation entities retain only their grid
-- positions; this layer interpolates them and follows the player smoothly.
local Presentation = {}
Presentation.__index = Presentation

local function slide_axis(current, target, dt, speed)
  local delta = target - current
  local step = dt * (speed or 12)
  if math.abs(delta) <= step then
    return target
  end
  return current + (delta > 0 and step or -step)
end

local function follow_camera_axis(current, target, dt)
  local speed = math.abs(target - current) > 1.5 and 18 or 6
  return slide_axis(current, target, dt, speed)
end

function Presentation.new()
  return setmetatable({
    positions = setmetatable({}, { __mode = "k" }),
    hit_flash = 0,
    hit_shake = 0,
  }, Presentation)
end

function Presentation:reset(session)
  self.positions = setmetatable({}, { __mode = "k" })
  local player = session.state.player
  if player then
    self.camera_x, self.camera_y = player.x, player.y
  end
end

function Presentation:hit()
  self.hit_flash, self.hit_shake = 0.32, 0.24
end

function Presentation:_animate(entity, dt)
  local position = self.positions[entity]
  if not position then
    position = { x = entity.x, y = entity.y }
    self.positions[entity] = position
  end
  position.x = slide_axis(position.x, entity.x, dt)
  position.y = slide_axis(position.y, entity.y, dt)
  return position
end

function Presentation:position(entity)
  local position = self.positions[entity]
  if not position then
    position = { x = entity.x, y = entity.y }
    self.positions[entity] = position
  end
  return position.x, position.y
end

function Presentation:update(session, dt)
  self.hit_flash = math.max(0, self.hit_flash - dt)
  self.hit_shake = math.max(0, self.hit_shake - dt)
  local state, player = session.state, session.state.player
  if not player then
    return
  end

  local player_position = self:_animate(player, dt)
  self.camera_x = follow_camera_axis(self.camera_x or player_position.x, player_position.x, dt)
  self.camera_y = follow_camera_axis(self.camera_y or player_position.y, player_position.y, dt)
  for _, values in ipairs({ state.enemies, state.bullets }) do
    for _, entity in ipairs(values or {}) do
      self:_animate(entity, dt)
    end
  end
end

return Presentation
