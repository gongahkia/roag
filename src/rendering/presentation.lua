-- Non-authoritative visual state. Simulation entities retain only their grid
-- positions; this layer interpolates them and follows the player smoothly.
local Presentation = {}
Presentation.__index = Presentation

local IDLE_SETTLE_EPSILON = 0.01
local IDLE_BEAT_SECONDS = 1.35
local MOVE_SPEED = 7.5

local function stable_phase(entity)
  -- Components and fallen records are already stable physical identities.  A
  -- display-only phase derived from them keeps adjacent identical enemies
  -- from moving in lockstep without allocating simulation state or RNG.
  local identity = entity.fallen_archive_id or entity.content_id or entity.kind or "actor"
  if entity.body and entity.body.list_components then
    local components = entity.body:list_components()
    if components[1] then identity = identity .. ":" .. components[1].id end
  end
  local hash = 0
  for index = 1, #identity do hash = (hash * 33 + identity:byte(index)) % 6283 end
  return hash / 1000
end

local function slide_axis(current, target, dt, speed)
  local delta = target - current
  local step = dt * (speed or 12)
  if math.abs(delta) <= step then
    return target
  end
  return current + (delta > 0 and step or -step)
end

local function follow_camera_axis(current, target, dt)
  -- Let a moving actor lead the camera for a beat.  Following at the old
  -- near-lockstep speed kept the player visually pinned to the screen centre,
  -- which made a real interpolation read like a tile-to-tile teleport.
  local speed = math.abs(target - current) > 1.5 and 12 or 3.2
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
  local state, player = session.state, session.state.player
  local function seed(entity)
    if entity then self.positions[entity] = { x = entity.x, y = entity.y } end
  end
  seed(player)
  for _, values in ipairs({ state.enemies, state.bullets }) do
    for _, entity in ipairs(values or {}) do seed(entity) end
  end
  seed(state.boss)
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
  position.x = slide_axis(position.x, entity.x, dt, MOVE_SPEED)
  position.y = slide_axis(position.y, entity.y, dt, MOVE_SPEED)
  return position
end

-- A restrained stretch makes the slide feel physical while position remains
-- fully presentation-only.  It is intentionally unavailable at rest so it
-- composes cleanly with the quieter idle breathing animation.
function Presentation:movement_transform(entity)
  local x, y = self:position(entity)
  local delta_x, delta_y = entity.x - x, entity.y - y
  local magnitude = math.max(math.abs(delta_x), math.abs(delta_y))
  if magnitude <= IDLE_SETTLE_EPSILON then return nil end
  local stretch = math.min(0.075, magnitude * 0.075)
  if math.abs(delta_x) >= math.abs(delta_y) then
    return { scale_x = 1 + stretch, scale_y = 1 - stretch * 0.55 }
  end
  return { scale_x = 1 - stretch * 0.55, scale_y = 1 + stretch }
end

function Presentation:position(entity)
  local position = self.positions[entity]
  if not position then
    position = { x = entity.x, y = entity.y }
    self.positions[entity] = position
  end
  return position.x, position.y
end

function Presentation:is_settled(entity)
  if not entity then return false end
  local x, y = self:position(entity)
  return math.abs(x - entity.x) <= IDLE_SETTLE_EPSILON and math.abs(y - entity.y) <= IDLE_SETTLE_EPSILON
end

-- A sprite-pack-independent transform for living actors at rest.  It is
-- deliberately derived only from wall-clock time and actor identity: no
-- active-run state, save data, or deterministic simulation RNG is touched.
function Presentation:idle_transform(session, entity, time, tile_size)
  local player = session and session.state and session.state.player
  if not player or not entity or not self:is_settled(player) or not self:is_settled(entity) or self.hit_flash > 0 then
    return nil
  end
  local phase = stable_phase(entity)
  local beat = (time or 0) * (math.pi * 2 / IDLE_BEAT_SECONDS) + phase
  local sway = math.sin(beat * 0.5 + phase * 0.37)
  local bob = math.sin(beat)
  local size = tile_size or 16
  return {
    offset_x = sway * size * 0.012,
    offset_y = bob * size * 0.035,
    scale_x = 1 + sway * 0.012,
    scale_y = 1 - bob * 0.02,
  }
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
  if state.boss then self:_animate(state.boss, dt) end
end

return Presentation
