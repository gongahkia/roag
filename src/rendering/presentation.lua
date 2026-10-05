-- Non-authoritative visual state. Simulation entities retain grid positions;
-- this layer supplies interpolation, feedback, and camera motion only.
local Tuning = require("src.rendering.tuning")

local Presentation = {}
Presentation.__index = Presentation

local IDLE_SETTLE_EPSILON = 0.01
local IDLE_BEAT_SECONDS = 1.35
local BUMP_SECONDS = 0.16
local BUMP_DIRECTIONS = {
  w = { 0, -1 }, a = { -1, 0 }, s = { 0, 1 }, d = { 1, 0 },
}

local function weak_map()
  return setmetatable({}, { __mode = "k" })
end

local function stable_phase(entity)
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
  local delta, step = target - current, dt * speed
  if math.abs(delta) <= step then return target end
  return current + (delta > 0 and step or -step)
end

local function ease_axis(current, target, dt, rate)
  -- Frame-rate independent exponential camera follow.
  local blend = 1 - math.exp(-(rate or Tuning.camera_follow_rate) * dt)
  return current + (target - current) * blend
end

local function direction_vector(direction)
  return BUMP_DIRECTIONS[direction] or { 0, 0 }
end

function Presentation.new()
  return setmetatable({
    positions = weak_map(), reactions = weak_map(), attacks = weak_map(),
    hit_flash = 0, hit_shake = 0, shake_amount = 0, hit_stop_remaining = 0,
    bump_time = 0, bump_direction = nil,
    impacts = {}, tracers = {}, particles = {}, damage_numbers = {}, break_labels = {}, deaths = {}, modifier_chain = {},
  }, Presentation)
end

function Presentation:reset(session)
  self.positions = weak_map()
  local state, player = session.state, session.state.player
  local function seed(entity)
    if entity then self.positions[entity] = { x = entity.x, y = entity.y } end
  end
  seed(player)
  for _, values in ipairs({ state.enemies, state.bullets }) do
    for _, entity in ipairs(values or {}) do seed(entity) end
  end
  seed(state.boss)
  self._player = player
  if player then self.camera_x, self.camera_y = player.x, player.y end
  self.bump_time, self.bump_direction = 0, nil
  self.reactions, self.attacks = weak_map(), weak_map()
  self.impacts, self.tracers, self.particles = {}, {}, {}
  self.damage_numbers, self.break_labels, self.deaths, self.modifier_chain = {}, {}, {}, {}
  self.hit_stop_remaining, self.shake_amount, self.hit_flash, self.hit_shake = 0, 0, 0, 0
end

function Presentation:request_hit_stop(duration)
  self.hit_stop_remaining = math.max(self.hit_stop_remaining or 0, duration or Tuning.hit_stop_duration)
end

function Presentation:is_hit_stopped()
  return (self.hit_stop_remaining or 0) > 0
end

function Presentation:request_shake(amount)
  self.shake_amount = math.min(Tuning.shake_cap, math.max(self.shake_amount or 0, amount or 0))
  self.hit_shake = math.max(self.hit_shake or 0, Tuning.shake_duration)
end

function Presentation:screen_shake()
  if (self.hit_shake or 0) <= 0 then return 0 end
  return (self.shake_amount or 0) * (self.hit_shake / Tuning.shake_duration)
end

-- Compatibility path for old direct player-damage events. New actor_hit
-- events provide target-local reaction, damage text, and the same hit-stop.
function Presentation:hit()
  self.hit_flash = math.max(self.hit_flash, Tuning.hit_flash_duration)
  self:request_hit_stop(Tuning.hit_stop_duration)
  self:request_shake(Tuning.shake_light)
end

function Presentation:bump(direction)
  if BUMP_DIRECTIONS[direction] then self.bump_time, self.bump_direction = BUMP_SECONDS, direction end
end

function Presentation:impact(value)
  if not value or type(value.x) ~= "number" or type(value.y) ~= "number" then return end
  self.impacts[#self.impacts + 1] = {
    x = value.x, y = value.y, material_id = value.material_id,
    applied = value.applied == true, destroyed = value.destroyed == true, time = 0.18,
  }
  if value.applied then self:_add_particles(value.x, value.y, "terrain", 3) end
end

function Presentation:attack(value)
  local actor = value and value.actor
  if not actor then return end
  self.attacks[actor] = {
    time = Tuning.attack_recoil_duration,
    direction = value.direction or actor.direction,
    implementation = value.implementation,
    heavy = value.heavy == true,
  }
end

function Presentation:_add_particles(x, y, cause, count)
  count = count or Tuning.impact_particle_count
  for index = 1, count do
    -- Fixed pattern gives rich feedback without consuming simulation RNG.
    local angle = (index - 1) * (math.pi * 2 / count) + (x * 0.31 + y * 0.17)
    self.particles[#self.particles + 1] = {
      x = x, y = y, vx = math.cos(angle) * 0.22, vy = math.sin(angle) * 0.22,
      cause = cause, time = Tuning.particle_lifetime,
    }
  end
end

function Presentation:actor_hit(value)
  if not value or not value.target then return end
  local target, source = value.target, value.source_actor
  local dx, dy = target.x - (source and source.x or target.x), target.y - (source and source.y or target.y)
  if dx == 0 and dy == 0 then dx, dy = direction_vector(value.direction) end
  local length = math.max(1, math.sqrt(dx * dx + dy * dy))
  self.reactions[target] = { time = Tuning.reaction_duration, flash = Tuning.hit_flash_duration, dx = dx / length, dy = dy / length }
  self.damage_numbers[#self.damage_numbers + 1] = {
    x = value.x or target.x, y = value.y or target.y, amount = value.amount,
    cause = value.cause, time = Tuning.damage_number_lifetime, lane = (#self.damage_numbers % 5) - 2,
  }
  self:_add_particles(value.x or target.x, value.y or target.y, value.cause)
  self:request_hit_stop(Tuning.hit_stop_duration)
  self:request_shake(value.heavy and Tuning.shake_heavy or Tuning.shake_light)
  if target.kind == "player" then self.hit_flash = math.max(self.hit_flash, Tuning.hit_flash_duration) end
end

function Presentation:component_break(value)
  if not value or type(value.x) ~= "number" or type(value.y) ~= "number" then return end
  self.break_labels[#self.break_labels + 1] = { x = value.x, y = value.y, text = "BREAK", time = Tuning.break_label_lifetime }
end

function Presentation:actor_death(value)
  if not value or type(value.x) ~= "number" or type(value.y) ~= "number" then return end
  self.deaths[#self.deaths + 1] = { x = value.x, y = value.y, time = Tuning.particle_lifetime }
  self:_add_particles(value.x, value.y, "death", Tuning.impact_particle_count + 2)
  self:request_shake(Tuning.shake_heavy)
end

function Presentation:projectile_travel(value)
  if not value then return end
  self.tracers[#self.tracers + 1] = {
    from_x = value.from_x, from_y = value.from_y, to_x = value.to_x, to_y = value.to_y,
    piercing = value.piercing == true, time = Tuning.tracer_lifetime,
  }
end

-- Presentation consumes a deliberately compact view model. It never reads
-- resolver internals or affects the already-completed simulation chain.
function Presentation:modifier_effect(value)
  if not value or not value.source_name then return end
  local summary = value.summary or value.effect_id or "EFFECT"
  local previous = self.modifier_chain[#self.modifier_chain]
  if previous and previous.name == value.source_name and previous.summary == summary and previous.time > Tuning.modifier_chain_lifetime * 0.45 then
    previous.count, previous.time = previous.count + 1, Tuning.modifier_chain_lifetime
    return
  end
  self.modifier_chain[#self.modifier_chain + 1] = {
    name = value.source_name, stacks = value.stacks, summary = summary, count = 1,
    time = Tuning.modifier_chain_lifetime,
  }
end

function Presentation:player_bump_transform(tile_size)
  local direction = self.bump_direction and BUMP_DIRECTIONS[self.bump_direction]
  if not direction or self.bump_time <= 0 then return nil end
  local elapsed = 1 - self.bump_time / BUMP_SECONDS
  local amount = elapsed < 0.42 and elapsed / 0.42 * 0.16 or -((1 - elapsed) / 0.58) * 0.09
  local size = tile_size or 16
  return { offset_x = direction[1] * size * amount, offset_y = direction[2] * size * amount }
end

function Presentation.merge_transforms(first, second)
  if not first then return second end
  if not second then return first end
  return {
    offset_x = (first.offset_x or 0) + (second.offset_x or 0),
    offset_y = (first.offset_y or 0) + (second.offset_y or 0),
    scale_x = (first.scale_x or 1) * (second.scale_x or 1),
    scale_y = (first.scale_y or 1) * (second.scale_y or 1),
  }
end

function Presentation:_animate(entity, dt)
  local position = self.positions[entity]
  if not position then position = { x = entity.x, y = entity.y }; self.positions[entity] = position end
  local lag_x, lag_y = entity.x - position.x, entity.y - position.y
  -- A dash or a rapid action sequence stays at most one visual cell behind;
  -- no obsolete tween queue can play after input stops.
  if math.abs(lag_x) > Tuning.max_visual_lag_cells then
    position.x = entity.x - (lag_x > 0 and Tuning.max_visual_lag_cells or -Tuning.max_visual_lag_cells)
  end
  if math.abs(lag_y) > Tuning.max_visual_lag_cells then
    position.y = entity.y - (lag_y > 0 and Tuning.max_visual_lag_cells or -Tuning.max_visual_lag_cells)
  end
  local duration = entity == self._player and Tuning.player_move_duration
    or (entity.kind == "bullet" and Tuning.projectile_move_duration or Tuning.actor_move_duration)
  local speed = 1 / duration
  position.x = slide_axis(position.x, entity.x, dt, speed)
  position.y = slide_axis(position.y, entity.y, dt, speed)
  return position
end

function Presentation:movement_transform(entity)
  local x, y = self:position(entity)
  local delta_x, delta_y = entity.x - x, entity.y - y
  local magnitude = math.max(math.abs(delta_x), math.abs(delta_y))
  if magnitude <= IDLE_SETTLE_EPSILON then return nil end
  local stretch = math.min(Tuning.squash, magnitude * Tuning.squash)
  if math.abs(delta_x) >= math.abs(delta_y) then return { scale_x = 1 + stretch, scale_y = 1 - stretch * 0.55 } end
  return { scale_x = 1 - stretch * 0.55, scale_y = 1 + stretch }
end

function Presentation:attack_transform(entity, tile_size)
  local attack = self.attacks[entity]
  if not attack or attack.time <= 0 then return nil end
  local elapsed, vector = 1 - attack.time / Tuning.attack_recoil_duration, direction_vector(attack.direction)
  local melee = attack.implementation == "melee" or attack.implementation == "tool"
  local amount = melee and (elapsed < 0.45 and elapsed / 0.45 * Tuning.melee_lunge or -((1 - elapsed) / 0.55) * Tuning.ranged_recoil)
    or -math.sin(elapsed * math.pi) * Tuning.ranged_recoil
  local stretch, size = math.sin(elapsed * math.pi) * Tuning.squash, tile_size or 16
  local transform = { offset_x = vector[1] * size * amount, offset_y = -vector[2] * size * amount }
  if math.abs(vector[1]) >= math.abs(vector[2]) then
    transform.scale_x, transform.scale_y = 1 + stretch, 1 - stretch * 0.55
  else
    transform.scale_x, transform.scale_y = 1 - stretch * 0.55, 1 + stretch
  end
  return transform
end

function Presentation:reaction_transform(entity, tile_size)
  local reaction = self.reactions[entity]
  if not reaction or reaction.time <= 0 then return nil end
  local pulse, size = math.sin((reaction.time / Tuning.reaction_duration) * math.pi), tile_size or 16
  return {
    offset_x = reaction.dx * size * Tuning.hit_recoil * pulse,
    offset_y = -reaction.dy * size * Tuning.hit_recoil * pulse,
    scale_x = 1 + Tuning.squash * pulse, scale_y = 1 - Tuning.squash * 0.55 * pulse,
  }
end

-- Placeholder art is intentionally not rotated. This tiny front-weighted
-- offset gives a stable directional cue without pretending the base sprite
-- has four bespoke animation frames.
function Presentation:facing_transform(entity, tile_size)
  if entity ~= self._player then return nil end
  local vector = direction_vector(entity.direction)
  local size = tile_size or 16
  return { offset_x = vector[1] * size * 0.018, offset_y = -vector[2] * size * 0.018 }
end

function Presentation:reaction_flash(entity)
  local reaction = self.reactions[entity]
  return reaction and reaction.flash and reaction.flash > 0 or false
end

function Presentation:position(entity)
  local position = self.positions[entity]
  if not position then position = { x = entity.x, y = entity.y }; self.positions[entity] = position end
  return position.x, position.y
end

function Presentation:is_settled(entity)
  if not entity then return false end
  local x, y = self:position(entity)
  return math.abs(x - entity.x) <= IDLE_SETTLE_EPSILON and math.abs(y - entity.y) <= IDLE_SETTLE_EPSILON
end

function Presentation:idle_transform(session, entity, time, tile_size)
  local player = session and session.state and session.state.player
  if not player or not entity or not self:is_settled(player) or not self:is_settled(entity) or self.hit_flash > 0 then return nil end
  local phase = stable_phase(entity)
  local beat, sway, bob, size = (time or 0) * (math.pi * 2 / IDLE_BEAT_SECONDS) + phase, nil, nil, tile_size or 16
  sway, bob = math.sin(beat * 0.5 + phase * 0.37), math.sin(beat)
  return { offset_x = sway * size * 0.012, offset_y = bob * size * 0.035, scale_x = 1 + sway * 0.012, scale_y = 1 - bob * 0.02 }
end

function Presentation:update(session, dt)
  self.hit_flash = math.max(0, self.hit_flash - dt)
  self.hit_shake = math.max(0, self.hit_shake - dt)
  self.hit_stop_remaining = math.max(0, self.hit_stop_remaining - dt)
  if self.hit_shake == 0 then self.shake_amount = 0 end
  self.bump_time = math.max(0, self.bump_time - dt)
  if self.bump_time == 0 then self.bump_direction = nil end
  local function decay(values)
    for index = #values, 1, -1 do values[index].time = values[index].time - dt; if values[index].time <= 0 then table.remove(values, index) end end
  end
  decay(self.impacts); decay(self.tracers); decay(self.damage_numbers); decay(self.break_labels); decay(self.deaths); decay(self.modifier_chain)
  for index = #self.particles, 1, -1 do
    local particle = self.particles[index]
    particle.x, particle.y, particle.time = particle.x + particle.vx * dt, particle.y + particle.vy * dt, particle.time - dt
    if particle.time <= 0 then table.remove(self.particles, index) end
  end
  for entity, reaction in pairs(self.reactions) do
    reaction.time, reaction.flash = reaction.time - dt, reaction.flash - dt
    if reaction.time <= 0 then self.reactions[entity] = nil end
  end
  for entity, attack in pairs(self.attacks) do attack.time = attack.time - dt; if attack.time <= 0 then self.attacks[entity] = nil end end

  local state, player = session.state, session.state.player
  if not player then return end
  self._player = player
  local player_position = self:_animate(player, dt)
  -- Follow the authoritative target rather than frame-sampled tween state so
  -- camera easing remains identical at every refresh rate. The player sprite
  -- still finishes its own shorter interpolation first.
  self.camera_x = ease_axis(self.camera_x or player_position.x, player.x, dt, Tuning.camera_follow_rate)
  self.camera_y = ease_axis(self.camera_y or player_position.y, player.y, dt, Tuning.camera_follow_rate)
  for _, values in ipairs({ state.enemies, state.bullets }) do for _, entity in ipairs(values or {}) do self:_animate(entity, dt) end end
  if state.boss then self:_animate(state.boss, dt) end
end

return Presentation
