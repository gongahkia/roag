-- Authoritative current-run simulation. This module intentionally has no
-- dependency on LÖVE so it can be created and stepped by tests or tools.
local Content = require("src.content.legacy")
local Generator = require("src.generation.map")
local Grid = require("src.world.grid")
local Rng = require("src.rng")

local Session = {}
Session.__index = Session

local DIRECTIONS = {
  w = { 0, 1, "N" },
  a = { -1, 0, "W" },
  s = { 0, -1, "S" },
  d = { 1, 0, "E" },
}
local BOSS_WINDUP = 4

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function entity(kind, x, y, values)
  local result = { kind = kind, x = x, y = y }
  for name, value in pairs(values or {}) do
    result[name] = value
  end
  return result
end

local function remove(values, index)
  local value = values[index]
  table.remove(values, index)
  return value
end

function Session.new(options)
  options = options or {}
  local self = setmetatable({}, Session)
  self.content = options.content or Content
  self.seed = options.seed or 1
  self.rng = options.rng or Rng.new(self.seed)
  self.seed = self.rng.seed
  self.emit = options.emit or function() end
  self.state = {
    stage = 1,
    score = 0,
    log = {},
    curse_bag = {},
    effects = {},
  }
  return self
end

function Session:_event(event_type, value)
  self.emit({ type = event_type, value = value })
end

function Session:_sound(name)
  self:_event("sound", name)
end

function Session:_log(message)
  table.insert(self.state.log, 1, message)
  while #self.state.log > 5 do
    table.remove(self.state.log)
  end
end

function Session:_open(x, y)
  return self.state.space[Grid.key(x, y)]
end

function Session:_move_entity(value, x, y)
  value.x, value.y = x, y
end

function Session:_apply(settings, modifiers)
  settings.health = settings.health + (modifiers.health or 0)
  for name, value in pairs(modifiers) do
    if name ~= "health" then
      settings[name] = (settings[name] or 0) + value
    end
  end
  settings.health = clamp(settings.health, 1, 5)
  settings.ammo = math.max(0, settings.ammo)
  settings.bombs = math.max(0, settings.bombs)
  settings.flares = math.max(0, settings.flares)
  settings.vision = math.max(1, settings.vision)
  settings.enemies = math.max(0, settings.enemies)
  settings.torches = math.max(0, settings.torches)
  settings.score = math.max(1, settings.score)
  settings.dash_cooldown = math.max(1, settings.dash_cooldown)
end

function Session:_settings_for_stage()
  local settings = Grid.copy(self.content.stages[self.state.stage])
  settings.health, settings.bombs, settings.flares = 2, 1, 1
  settings.torch_radius, settings.dash_cooldown = 4, 3
  settings.bomb_radius, settings.bomb_fuse = 2, 3
  settings.bullet_range, settings.reload_penalty = nil, 0

  if self.state.curse then
    local modifiers = self.state.curse.modifiers
    if modifiers.health then
      settings.health = modifiers.health
    end
    if modifiers.bombs ~= nil then
      settings.bombs = modifiers.bombs
    end
    if modifiers.flares ~= nil then
      settings.flares = modifiers.flares
    end
    for _, name in ipairs({
      "vision", "enemies", "ammo", "score", "torches", "torch_radius",
      "dash_cooldown", "bomb_radius", "bomb_fuse", "reload_penalty",
    }) do
      if modifiers[name] ~= nil then
        settings[name] = (settings[name] or 0) + modifiers[name]
      end
    end
    if modifiers.bullet_range then
      settings.bullet_range = modifiers.bullet_range
    end
  end

  self:_apply(settings, self.state.class.modifiers)
  self:_apply(settings, self.state.boon.modifiers)
  return settings
end

function Session:_occupied(include_boss)
  local state = self.state
  local occupied = { [Grid.key(state.player.x, state.player.y)] = true }
  for _, values in ipairs({
    state.targets, state.enemies, state.bullets, state.bombs, state.flares, state.torches,
  }) do
    for _, value in ipairs(values) do
      occupied[Grid.key(value.x, value.y)] = true
    end
  end
  if state.ammo then
    occupied[Grid.key(state.ammo.x, state.ammo.y)] = true
  end
  if state.exit then
    occupied[Grid.key(state.exit.x, state.exit.y)] = true
  end
  if include_boss then
    for x = 16, 24 do
      occupied[Grid.key(x, 1)] = true
    end
  end
  return occupied
end

function Session:_open_location(space, used, minimum)
  local options = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      local location_key = Grid.key(x, y)
      local point = Grid.cell(x, y)
      if space[location_key] and not used[location_key]
        and (not minimum or Grid.distance(point, self.state.player) >= minimum) then
        options[#options + 1] = point
      end
    end
  end
  assert(#options > 0, "No spawn location available")
  return self.rng:choice(options)
end

function Session:_enemy_type(index)
  if self.state.settings.wilds then
    return index % 3 == 0 and "bomber" or "wolf"
  end
  return self.state.settings.cultists and "cultist" or "necromancer"
end

function Session:_make_enemy(kind, point)
  return entity(kind, point.x, point.y, {
    health = 1,
    attack = 0,
    attack_kind = nil,
    attack_windup = 0,
    attack_x = nil,
    attack_y = nil,
    radius = 1,
    stun = 0,
  })
end

function Session:_spawn_entities()
  local state = self.state
  state.targets, state.enemies, state.bullets = {}, {}, {}
  state.bombs, state.flares, state.torches = {}, {}, {}
  for _ = 1, state.settings.torches do
    local point = self:_open_location(state.space, self:_occupied())
    state.torches[#state.torches + 1] = entity("torch", point.x, point.y, { light = state.settings.torch_radius })
  end
  for _ = 1, state.settings.targets do
    local point = self:_open_location(state.space, self:_occupied())
    state.targets[#state.targets + 1] = entity("target", point.x, point.y)
  end
  for index = 1, state.settings.enemies do
    local point = self:_open_location(state.space, self:_occupied())
    state.enemies[#state.enemies + 1] = self:_make_enemy(self:_enemy_type(index), point)
  end
  local point = self:_open_location(state.space, self:_occupied())
  state.ammo = entity("ammo", point.x, point.y)
end

function Session:_refill_entities()
  local state = self.state
  while #state.targets < state.settings.targets do
    local point = self:_open_location(state.space, self:_occupied())
    state.targets[#state.targets + 1] = entity("target", point.x, point.y)
  end
  while #state.enemies < state.settings.enemies do
    local point = self:_open_location(state.space, self:_occupied())
    state.enemies[#state.enemies + 1] = self:_make_enemy(self:_enemy_type(#state.enemies + 1), point)
  end
  if not state.ammo then
    local point = self:_open_location(state.space, self:_occupied())
    state.ammo = entity("ammo", point.x, point.y)
  end
end

function Session:start_run(class, boon)
  self.state.class = class
  self.state.boon = boon
  self.state.stage = 1
  self.state.score = 0
  self.state.curse = nil
  self.state.curse_bag = {}
  self.state.ended = nil
  self:start_stage()
end

function Session:start_stage()
  local state = self.state
  state.settings = self:_settings_for_stage()
  state.player = entity("player", math.floor(Grid.width / 2), math.floor(Grid.height / 2), {
    direction = "w",
    health = state.settings.health,
    ammo = state.settings.ammo,
    bombs = state.settings.bombs,
    flares = state.settings.flares,
    score = 0,
    dash = 0,
    dash_base = state.settings.dash_cooldown,
    bomb_radius = state.settings.bomb_radius,
    bomb_fuse = state.settings.bomb_fuse,
    bullet_range = state.settings.bullet_range,
    reload_penalty = state.settings.reload_penalty,
    impact = 0,
  })
  state.explored, state.effects = {}, {}
  state.exit, state.boss = nil, nil
  state.log = {}
  state.phase = "combat"
  state.space = Generator.generate(state.settings.terrain, state.player, self.rng)
  self:_spawn_entities()
  self:_log("Descend into the " .. state.settings.terrain .. ".")
  self:refresh_visibility()
end

function Session:choose_boons(count)
  local options = self.rng:shuffle(self.content.boons)
  local result = {}
  for index = 1, math.min(count or 3, #options) do
    result[index] = options[index]
  end
  return result
end

function Session:draw_curses()
  if #self.state.curse_bag < 3 then
    self.state.curse_bag = self.rng:shuffle(self.content.curses)
  end
  self.state.curse_options = {
    table.remove(self.state.curse_bag, 1),
    table.remove(self.state.curse_bag, 1),
    table.remove(self.state.curse_bag, 1),
  }
  return self.state.curse_options
end

function Session:choose_curse(curse)
  self.state.curse = curse
  self:start_stage()
end

function Session:_reload(amount, cursed)
  local player = self.state.player
  player.ammo = player.ammo + math.max(0, amount - (cursed and player.reload_penalty or 0))
end

function Session:_hurt(message)
  local state = self.state
  state.player.health = math.max(0, state.player.health - 1)
  state.player.impact = 2
  self:_event("hit")
  self:_sound("hurt")
  self:_log(message)
  if state.player.health == 0 then
    state.ended = "gameover"
  end
end

function Session:_destroy_target(index)
  remove(self.state.targets, index)
  self.state.player.score = self.state.player.score + 1
  self:_reload(1, true)
  self:_sound("hit")
  self:_log("Destroyed a target.")
end

function Session:_destroy_enemy(index)
  remove(self.state.enemies, index)
  self.state.player.score = self.state.player.score + 1
  self:_reload(2, true)
  self:_sound("hit")
  self:_log("Defeated an enemy.")
end

function Session:_boss_hitbox()
  local cells = {}
  for x = 16, 24 do
    cells[Grid.key(x, 1)] = true
  end
  return cells
end

function Session:_damage_boss(amount)
  self.state.boss.health = self.state.boss.health - amount
  self:_sound("hit")
end

function Session:_path(start, finish, blocked)
  blocked = blocked or {}
  local queue, head = { Grid.cell(start.x, start.y) }, 1
  local previous = { [Grid.key(start.x, start.y)] = false }
  blocked[Grid.key(start.x, start.y)] = nil
  blocked[Grid.key(finish.x, finish.y)] = nil

  while queue[head] do
    local point = queue[head]
    head = head + 1
    if point.x == finish.x and point.y == finish.y then
      local result = {}
      while point do
        table.insert(result, 1, point)
        point = previous[Grid.key(point.x, point.y)]
      end
      return result
    end
    for _, neighbour in ipairs(Grid.neighbours(point)) do
      local location_key = Grid.key(neighbour.x, neighbour.y)
      if self:_open(neighbour.x, neighbour.y) and not blocked[location_key] and previous[location_key] == nil then
        previous[location_key] = point
        queue[#queue + 1] = neighbour
      end
    end
  end
  return {}
end

function Session:_blast(origin, radius)
  local result, queue, head = {}, { Grid.cell(origin.x, origin.y) }, 1
  local steps = { [Grid.key(origin.x, origin.y)] = 0 }
  while queue[head] do
    local point = queue[head]
    head = head + 1
    local location_key = Grid.key(point.x, point.y)
    local distance = steps[location_key]
    if distance <= radius and self:_open(point.x, point.y) and not result[location_key] then
      result[location_key] = true
      for _, neighbour in ipairs(Grid.neighbours(point)) do
        local neighbour_key = Grid.key(neighbour.x, neighbour.y)
        if Grid.in_bounds(neighbour.x, neighbour.y) and not steps[neighbour_key] then
          steps[neighbour_key] = distance + 1
          queue[#queue + 1] = neighbour
        end
      end
    end
  end
  return result
end

function Session:_destroy_terrain(origin, radius)
  for x = origin.x - radius, origin.x + radius do
    for y = origin.y - radius, origin.y + radius do
      if x > 0 and x < Grid.width - 1 and y > 0 and y < Grid.height - 1
        and math.abs(x - origin.x) + math.abs(y - origin.y) <= radius then
        self.state.space[Grid.key(x, y)] = true
      end
    end
  end
end

function Session:_update_bullets()
  local state, remaining = self.state, {}
  for _, bullet in ipairs(state.bullets) do
    if bullet.active then
      if bullet.max and bullet.travel >= bullet.max then
        bullet.expired = true
      else
        local direction = DIRECTIONS[bullet.direction]
        self:_move_entity(bullet, bullet.x + direction[1], bullet.y + direction[2])
        bullet.travel = bullet.travel + 1
      end
    else
      bullet.active = true
    end

    local hit = bullet.expired or not self:_open(bullet.x, bullet.y)
    for index = #state.targets, 1, -1 do
      local target = state.targets[index]
      if target.x == bullet.x and target.y == bullet.y then
        self:_destroy_target(index)
        hit = true
        break
      end
    end
    if not hit then
      for index = #state.enemies, 1, -1 do
        local enemy = state.enemies[index]
        if enemy.x == bullet.x and enemy.y == bullet.y then
          enemy.health = enemy.health - 1
          if enemy.health <= 0 then
            self:_destroy_enemy(index)
          end
          hit = true
          break
        end
      end
    end
    if not hit and state.boss and self:_boss_hitbox()[Grid.key(bullet.x, bullet.y)] then
      self:_damage_boss(1)
      hit = true
    end
    if not hit then
      remaining[#remaining + 1] = bullet
    end
  end
  state.bullets = remaining
end

function Session:_update_bombs()
  local state, remaining = self.state, {}
  for _, bomb in ipairs(state.bombs) do
    bomb.fuse = bomb.fuse - 1
    if bomb.fuse > 0 then
      remaining[#remaining + 1] = bomb
    else
      self:_destroy_terrain(bomb, bomb.radius)
      local cells = self:_blast(bomb, bomb.radius)
      for location_key in pairs(cells) do
        state.effects[location_key] = true
      end
      self:_sound("boom")
      if cells[Grid.key(state.player.x, state.player.y)] then
        self:_hurt("You were caught in the blast.")
      end
      for index = #state.targets, 1, -1 do
        local target = state.targets[index]
        if cells[Grid.key(target.x, target.y)] then
          self:_destroy_target(index)
        end
      end
      for index = #state.enemies, 1, -1 do
        local enemy = state.enemies[index]
        if cells[Grid.key(enemy.x, enemy.y)] then
          enemy.health = enemy.health - 2
          if enemy.health <= 0 then
            self:_destroy_enemy(index)
          end
        end
      end
      if state.boss then
        for location_key in pairs(cells) do
          if self:_boss_hitbox()[location_key] then
            self:_damage_boss(2)
            break
          end
        end
      end
    end
  end
  state.bombs = remaining
end

function Session:_clear_enemy_attack(enemy)
  enemy.attack, enemy.attack_kind, enemy.attack_windup = 0, nil, 0
  enemy.attack_x, enemy.attack_y = nil, nil
end

function Session:_update_flares()
  local state, remaining = self.state, {}
  for _, flare in ipairs(state.flares) do
    flare.fuse = flare.fuse - 1
    if flare.fuse > 0 then
      remaining[#remaining + 1] = flare
    else
      local cells = self:_blast(flare, flare.radius)
      for location_key in pairs(cells) do
        state.effects[location_key] = true
      end
      self:_sound("flare")
      for _, enemy in ipairs(state.enemies) do
        if cells[Grid.key(enemy.x, enemy.y)] then
          enemy.stun = math.max(enemy.stun, flare.stun)
          self:_clear_enemy_attack(enemy)
        end
      end
    end
  end
  state.flares = remaining
end

function Session:_attack_cells(enemy)
  local cells = {}
  if enemy.attack == 0 then
    return cells
  end
  for x = enemy.attack_x - enemy.radius, enemy.attack_x + enemy.radius do
    for y = enemy.attack_y - enemy.radius, enemy.attack_y + enemy.radius do
      if Grid.in_bounds(x, y) then
        cells[Grid.key(x, y)] = true
      end
    end
  end
  return cells
end

function Session:_nearest_wolf()
  local leader, distance = nil, math.huge
  for _, enemy in ipairs(self.state.enemies) do
    if enemy.kind == "wolf" and Grid.distance(enemy, self.state.player) < distance then
      leader = enemy
      distance = Grid.distance(enemy, self.state.player)
    end
  end
  return leader
end

function Session:_begin_enemy_attack(enemy, kind, target, radius, windup)
  enemy.attack, enemy.attack_kind, enemy.attack_windup = 1, kind, windup
  enemy.attack_x, enemy.attack_y, enemy.radius = target.x, target.y, radius
end

function Session:_resolve_enemy_attack(enemy, index)
  if self:_attack_cells(enemy)[Grid.key(self.state.player.x, self.state.player.y)] then
    local messages = {
      pounce = "A forest wolf tore into you.",
      detonate = "A bomber detonated beside you.",
      spell = "A " .. enemy.kind .. " spell struck you.",
    }
    self:_hurt(messages[enemy.attack_kind] or "An enemy attack struck you.")
  end
  if enemy.attack_kind == "detonate" then
    remove(self.state.enemies, index)
    self:_log("A bomber exploded nearby!")
  else
    self:_clear_enemy_attack(enemy)
  end
end

function Session:_enemy_turn()
  local leader = self:_nearest_wolf()
  for index = #self.state.enemies, 1, -1 do
    local enemy = self.state.enemies[index]
    if enemy.stun > 0 then
      enemy.stun = enemy.stun - 1
    elseif enemy.attack == 0 then
      local blocked = {}
      for _, other in ipairs(self.state.enemies) do
        if other ~= enemy then
          blocked[Grid.key(other.x, other.y)] = true
        end
      end
      local hunt = self.state.player
      if enemy.kind == "wolf" and enemy ~= leader and leader and Grid.distance(enemy, leader) <= 10 then
        hunt = leader
      end
      local route = self:_path(enemy, hunt, blocked)
      if enemy.kind == "bomber" and #route <= 2 then
        self:_begin_enemy_attack(enemy, "detonate", self.state.player, 0, 1)
      elseif enemy.kind == "wolf" and Grid.distance(enemy, self.state.player) <= 1 then
        self:_begin_enemy_attack(enemy, "pounce", self.state.player, 0, 1)
      elseif enemy.kind == "wolf" and #route > 2 then
        self:_move_entity(enemy, route[2].x, route[2].y)
      elseif #route > 0 and #route - 1 <= 4 then
        self:_begin_enemy_attack(enemy, "spell", self.state.player, 1, 3)
      elseif #route > 1 then
        self:_move_entity(enemy, route[2].x, route[2].y)
      end
    else
      enemy.attack = enemy.attack + 1
      if enemy.attack > enemy.attack_windup then
        self:_resolve_enemy_attack(enemy, index)
      end
    end
  end
end

function Session:_boss_cells()
  local boss, cells = self.state.boss, {}
  if not boss.type then
    return cells
  end
  if boss.type == "crossfire" then
    for x = 0, Grid.width - 1 do
      cells[Grid.key(x, boss.y1)] = true
      cells[Grid.key(x, boss.y2)] = true
    end
    for y = 0, Grid.height - 1 do
      cells[Grid.key(boss.x1, y)] = true
      cells[Grid.key(boss.x2, y)] = true
    end
  elseif boss.type == "diagonal" then
    for y = 0, Grid.height - 1 do
      local a = boss.x1 + (y - boss.y1)
      local b = boss.x1 - (y - boss.y1)
      if Grid.in_bounds(a, y) then
        cells[Grid.key(a, y)] = true
      end
      if Grid.in_bounds(b, y) then
        cells[Grid.key(b, y)] = true
      end
    end
  else
    for x = boss.x1 - boss.radius, boss.x1 + boss.radius do
      for y = boss.y1 - boss.radius, boss.y1 + boss.radius do
        if Grid.in_bounds(x, y) and math.abs(x - boss.x1) + math.abs(y - boss.y1) == boss.radius then
          cells[Grid.key(x, y)] = true
        end
      end
    end
  end
  return cells
end

function Session:_new_boss_attack()
  local boss = self.state.boss
  local options = { "crossfire" }
  if boss.health < 8 then
    options[#options + 1] = "diagonal"
  end
  if boss.health < 4 then
    options[#options + 1] = "pulse"
  end
  boss.type = self.rng:choice(options)
  boss.name = ({ crossfire = "CROSSFIRE", diagonal = "DIAGONAL SWEEP", pulse = "ARCANE PULSE" })[boss.type]
  boss.radius = self.rng:int(2, 4)

  local points = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      if self.state.space[Grid.key(x, y)] then
        points[#points + 1] = Grid.cell(x, y)
      end
    end
  end
  local first, second = self.rng:choice(points), self.rng:choice(points)
  boss.x1, boss.y1, boss.x2, boss.y2 = first.x, first.y, second.x, second.y
end

function Session:_boss_turn()
  local boss = self.state.boss
  boss.attack = boss.attack + 1
  if boss.attack > BOSS_WINDUP then
    boss.attack = 0
    self:_new_boss_attack()
  elseif boss.attack == BOSS_WINDUP and self:_boss_cells()[Grid.key(self.state.player.x, self.state.player.y)] then
    self:_hurt("The boss attack struck you.")
  end

  local limit = boss.health < 4 and 3 or (boss.health < 8 and 2 or 1)
  if boss.attack == 0 and #self.state.enemies < limit then
    local point = self:_open_location(self.state.space, self:_occupied(true), 6)
    self.state.enemies[#self.state.enemies + 1] = self:_make_enemy("necromancer", point)
    self:_log("The boss summoned a necromancer.")
  end
end

function Session:_collect_ammo()
  local state = self.state
  if state.ammo and state.player.x == state.ammo.x and state.player.y == state.ammo.y then
    self:_reload(1, false)
    state.ammo = nil
    self:_sound("pickup")
    self:_log("Collected ammo.")
  end
end

function Session:_move_player(direction)
  local player, delta = self.state.player, DIRECTIONS[direction]
  local moved = false
  player.direction = direction
  if self:_open(player.x + delta[1], player.y + delta[2]) then
    self:_move_entity(player, player.x + delta[1], player.y + delta[2])
    moved = true
  else
    self:_log("A wall blocks your path.")
  end
  if moved then
    self:_log("Moved " .. delta[3] .. ".")
  end
end

function Session:can_move(direction)
  local player, delta = self.state.player, DIRECTIONS[direction]
  return player and delta and self:_open(player.x + delta[1], player.y + delta[2])
end

function Session:_dash()
  local player = self.state.player
  if player.dash > 0 then
    self:_log("Dash is recharging.")
    return
  end
  local delta = DIRECTIONS[player.direction]
  local original_x, original_y = player.x, player.y
  for _ = 1, 2 do
    if self:_open(player.x + delta[1], player.y + delta[2]) then
      self:_move_entity(player, player.x + delta[1], player.y + delta[2])
    else
      break
    end
  end
  if player.x == original_x and player.y == original_y then
    self:_log("Dash blocked.")
    return
  end
  player.dash = player.dash_base
  self:_sound("step")
  self:_log("Dashed forward.")
end

function Session:_shoot(direction)
  local player = self.state.player
  player.direction = direction or player.direction
  if player.ammo <= 0 then
    self:_log("No more ammo, find more to shoot.")
    return
  end
  player.ammo = player.ammo - 1
  self.state.bullets[#self.state.bullets + 1] = entity("bullet", player.x, player.y, {
    direction = player.direction,
    active = false,
    travel = 1,
    max = player.bullet_range,
    light = 2,
  })
  self:_sound("shoot")
  self:_log("Fired " .. DIRECTIONS[player.direction][3] .. ".")
end

function Session:_action(input)
  local player = self.state.player
  if DIRECTIONS[input] then
    self:_move_player(input)
    self:_sound("step")
    return
  end
  if player.impact > 0 then
    self:_log("You are recovering from the hit.")
    return
  end
  if input == "q" then
    self:_dash()
  elseif input == "e" then
    self:_shoot()
  elseif input:match("^shoot_[wasd]$") then
    self:_shoot(input:sub(-1))
  elseif input == "b" then
    if player.bombs <= 0 then
      self:_log("No bombs left. Buy bombs in the shop.")
    else
      player.bombs = player.bombs - 1
      self.state.bombs[#self.state.bombs + 1] = entity("bomb", player.x, player.y, {
        fuse = player.bomb_fuse,
        radius = player.bomb_radius,
        light = 3,
      })
      self:_sound("select")
      self:_log("Bomb armed. Move away before it explodes.")
    end
  elseif input == "f" then
    if player.flares <= 0 then
      self:_log("No flares left. Buy flares in the shop.")
    else
      player.flares = player.flares - 1
      self.state.flares[#self.state.flares + 1] = entity("flare", player.x, player.y, {
        fuse = 2,
        radius = 1,
        stun = 2,
        light = 3,
      })
      self:_sound("flare")
      self:_log("Flare lit. Necromancers will be stunned.")
    end
  end
end

function Session:_begin_exit()
  local point = self:_open_location(self.state.space, self:_occupied(), 6)
  self.state.exit = entity("door", point.x, point.y)
  self.state.phase = "exit"
  self:_log("All targets are down. Find the exit.")
end

function Session:_complete_stage()
  self.state.score = self.state.score + self.state.player.score
  if self.state.stage < #self.content.stages then
    self.state.stage = self.state.stage + 1
    self:draw_curses()
    return "curse"
  end
  return "shop"
end

function Session:start_boss()
  local state, player = self.state, self.state.player
  player.x, player.y, player.direction, player.score = 20, 9, "w", 0
  player.bombs, player.flares = math.max(1, player.bombs), math.max(1, player.flares)
  player.dash = 0
  player.dash_base = math.max(1, 3 + (state.class.modifiers.dash_cooldown or 0) + (state.boon.modifiers.dash_cooldown or 0))
  player.bomb_radius, player.bomb_fuse, player.bullet_range, player.reload_penalty = 2, 3, nil, 0
  state.settings = { terrain = "arena", vision = 99, score = 10 }
  state.space = Generator.generate("arena", player, self.rng, true)
  state.targets, state.enemies, state.bullets = {}, {}, {}
  state.bombs, state.flares, state.torches = {}, {}, {}
  state.effects, state.exit = {}, nil
  state.boss = { kind = "boss", x = 16, y = 1, health = 10, attack = 0, type = "crossfire", name = "CROSSFIRE", radius = 3, line = "PuNy MoRtAl, yoU dArE cHalLenGE mE?" }
  self:_new_boss_attack()
  local point = self:_open_location(state.space, self:_occupied(true))
  state.ammo = entity("ammo", point.x, point.y)
  state.phase = "boss"
  self:_log("The boss awaits.")
  self:refresh_visibility()
end

function Session:buy(item)
  local player = self.state.player
  if self.state.score > 0 and player[item.key] < 5 then
    player[item.key] = player[item.key] + 1
    self.state.score = self.state.score - 1
    self:_sound("pickup")
    return true
  end
  self:_log("Cannot buy that item.")
  return false
end

function Session:sell(item)
  local player = self.state.player
  if player[item.key] > item.minimum then
    player[item.key] = player[item.key] - 1
    self.state.score = self.state.score + 1
    self:_sound("select")
    return true
  end
  self:_log("That item cannot be sold.")
  return false
end

function Session:turn(input)
  local state = self.state
  assert(state.player, "A run must be started before it can advance")
  if state.ended then
    return state.ended
  end
  state.effects = {}
  state.player.dash = math.max(0, state.player.dash - 1)
  state.player.impact = math.max(0, state.player.impact - 1)

  if state.phase == "exit" then
    if DIRECTIONS[input] then
      self:_move_player(input)
    elseif input == "q" then
      self:_dash()
    end
    if state.player.x == state.exit.x and state.player.y == state.exit.y then
      local result = self:_complete_stage()
      self:refresh_visibility()
      return result
    end
    self:refresh_visibility()
    return nil
  end

  self:_action(input)
  self:_collect_ammo()
  self:_update_bullets()
  self:_update_bombs()
  self:_update_flares()
  if state.ended then
    self:refresh_visibility()
    return state.ended
  end

  local result
  if state.phase == "boss" then
    if state.boss.health <= 0 then
      state.ended = "victory"
      self:_sound("door")
      result = "victory"
    else
      self:_boss_turn()
      if state.boss.health > 0 then
        self:_enemy_turn()
      end
    end
  elseif state.player.score >= state.settings.score then
    self:_begin_exit()
  else
    self:_enemy_turn()
    self:_refill_entities()
  end
  self:refresh_visibility()
  return result or state.ended
end

function Session:_has_line_of_sight(x0, y0, x1, y1)
  local delta_x, delta_y = math.abs(x1 - x0), math.abs(y1 - y0)
  local step_x, step_y = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local error_value = delta_x - delta_y
  while x0 ~= x1 or y0 ~= y1 do
    local twice = error_value * 2
    if twice > -delta_y then
      error_value = error_value - delta_y
      x0 = x0 + step_x
    end
    if twice < delta_x then
      error_value = error_value + delta_x
      y0 = y0 + step_y
    end
    if (x0 ~= x1 or y0 ~= y1) and not self:_open(x0, y0) then
      return false
    end
  end
  return true
end

function Session:_light_area(source, radius, visible)
  if radius >= math.max(Grid.width, Grid.height) then
    for x = 0, Grid.width - 1 do
      for y = 0, Grid.height - 1 do
        visible[Grid.key(x, y)] = true
      end
    end
    return
  end
  for x = source.x - radius, source.x + radius do
    for y = source.y - radius, source.y + radius do
      local delta_x, delta_y = x - source.x, y - source.y
      if Grid.in_bounds(x, y) and delta_x * delta_x + delta_y * delta_y <= radius * radius
        and self:_has_line_of_sight(source.x, source.y, x, y) then
        visible[Grid.key(x, y)] = true
      end
    end
  end
end

function Session:refresh_visibility()
  local state = self.state
  if not state.player or not state.settings then
    return
  end
  local visible = {}
  state.explored = state.explored or {}
  self:_light_area(state.player, state.settings.vision, visible)
  for _, values in ipairs({ state.torches or {}, state.bombs or {}, state.flares or {}, state.bullets or {} }) do
    for _, value in ipairs(values) do
      if value.light then
        self:_light_area(value, value.light, visible)
      end
    end
  end
  for location_key in pairs(state.effects) do
    visible[location_key] = true
  end
  for location_key in pairs(visible) do
    state.explored[location_key] = true
  end
  state.visible = visible
end

function Session:telegraphs()
  local result = {}
  for _, enemy in ipairs(self.state.enemies or {}) do
    if enemy.attack > 0 then
      for location_key in pairs(self:_attack_cells(enemy)) do
        result[location_key] = enemy.attack >= enemy.attack_windup and "danger" or "warn"
      end
    end
  end
  if self.state.boss then
    for location_key in pairs(self:_boss_cells()) do
      result[location_key] = self.state.boss.attack >= BOSS_WINDUP - 1 and "danger" or "warn"
    end
  end
  return result
end

function Session:enemy_intent(enemy)
  if enemy.stun > 0 then
    return "STUNNED"
  end
  if enemy.attack > 0 then
    return string.upper(enemy.attack_kind) .. " IN " .. math.max(1, enemy.attack_windup - enemy.attack + 1)
  end
  return enemy.kind == "wolf" and "POUNCE" or enemy.kind == "bomber" and "DETONATE" or "ADVANCE"
end

return Session
