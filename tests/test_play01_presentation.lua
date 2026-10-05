local Campaign = require("src.campaign.campaign")
local App = require("src.app.app")
local Grid = require("src.world.grid")
local Presentation = require("src.rendering.presentation")
local Renderer = require("src.rendering.renderer")
local Tuning = require("src.rendering.tuning")

local function new_campaign(seed)
  return Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed })
end

local function open_line(session, length)
  local world = session.state.world
  for x = 2, Grid.width - length - 2 do
    for y = 2, Grid.height - 3 do
      local clear = true
      for offset = 0, length do clear = clear and world:is_passable(x + offset, y) and not world:object_at(x + offset, y) end
      if clear then return x, y end
    end
  end
  error("No open presentation fixture line")
end

return {
  {
    name = "PLAY-01 movement presentation uses a 60ms player target and bounds visual backlog",
    run = function()
      local session, player = { state = { enemies = {}, bullets = {}, boss = nil } }, { kind = "player", x = 10, y = 10 }
      session.state.player = player
      local presentation = Presentation.new()
      presentation:reset(session)
      player.x = 11
      presentation:update(session, Tuning.player_move_duration)
      assert(select(1, presentation:position(player)) == 11)
      player.x = 14
      presentation:update(session, 0)
      assert(player.x - select(1, presentation:position(player)) <= Tuning.max_visual_lag_cells + 0.0001)
      assert(Tuning.player_move_duration < 1 / 7.5)
    end,
  },
  {
    name = "PLAY-01 camera easing is frame-rate independent",
    run = function()
      local function follow(steps, dt)
        local session, player = { state = { enemies = {}, bullets = {}, boss = nil } }, { kind = "player", x = 10, y = 10 }
        session.state.player = player
        local presentation = Presentation.new()
        presentation:reset(session)
        player.x = 12
        for _ = 1, steps do presentation:update(session, dt) end
        return presentation.camera_x
      end
      local one = follow(1, 0.1)
      local many = follow(10, 0.01)
      assert(math.abs(one - many) < 0.0001)
    end,
  },
  {
    name = "PLAY-01 hit-stop coalesces and presentation never mutates actor authority",
    run = function()
      local session, player = { state = { enemies = {}, bullets = {}, boss = nil } }, { kind = "player", x = 10, y = 10, health = 5 }
      local enemy = { kind = "ripper", x = 11, y = 10, health = 3 }
      session.state.player, session.state.enemies = player, { enemy }
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:actor_hit({ target = enemy, source_actor = player, x = 11, y = 10, amount = 2, cause = "kinetic" })
      local first = presentation.hit_stop_remaining
      presentation:request_hit_stop(Tuning.hit_stop_duration / 2)
      assert(presentation.hit_stop_remaining == first and presentation:is_hit_stopped())
      presentation:update(session, first / 2)
      assert(presentation:is_hit_stopped() and enemy.x == 11 and enemy.health == 3)
      presentation:update(session, first)
      assert(not presentation:is_hit_stopped())
    end,
  },
  {
    name = "PLAY-01 environmental hit provenance never crashes the presentation recoil",
    run = function()
      local session = { state = { enemies = {}, bullets = {}, boss = nil } }
      local target = { kind = "ripper", x = 11, y = 10, health = 3 }
      session.state.enemies = { target }
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:actor_hit({
        target = target,
        -- A self-owned bomb has no source-to-target vector, so the visual
        -- reaction must use the supplied cardinal impact direction instead.
        source_actor = target,
        x = 11, y = 10, amount = 2, cause = "explosive", direction = "d",
      })
      local reaction = assert(presentation.reactions[target])
      assert(reaction.dx == 1 and reaction.dy == 0)
      assert(#presentation.damage_numbers == 1 and presentation.damage_numbers[1].amount == 2)
    end,
  },
  {
    name = "PLAY-01 hit-stop blocks held field dispatch without changing the simulation",
    run = function()
      local session, player = { state = { enemies = {}, bullets = {}, boss = nil } }, { kind = "player", x = 10, y = 10 }
      session.state.player = player
      function session:can_move() return true end
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:request_hit_stop(Tuning.hit_stop_duration)
      local dispatched = 0
      local app = setmetatable({ screen = "game", session = session, presentation = presentation,
        held_direction = "d", hold_timer = 0, held_movement_blocked = false }, App)
      function app:movement_repeat_interval() return 0.09 end
      function app:perform_turn() dispatched = dispatched + 1 end
      app:update(Tuning.hit_stop_duration * 2)
      assert(dispatched == 0 and player.x == 10)
      app:update(0.01)
      assert(dispatched == 1)
    end,
  },
  {
    name = "PLAY-01 damage presentation records one deterministic number per damage callback",
    run = function()
      local session, player = { state = { enemies = {}, bullets = {}, boss = nil } }, { kind = "player", x = 10, y = 10 }
      local enemy = { kind = "ripper", x = 11, y = 10 }
      session.state.player, session.state.enemies = player, { enemy }
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:actor_hit({ target = enemy, source_actor = player, x = 11, y = 10, amount = 1, cause = "kinetic" })
      presentation:actor_hit({ target = enemy, source_actor = player, x = 11, y = 10, amount = 2, cause = "electrical" })
      assert(#presentation.damage_numbers == 2 and presentation.damage_numbers[1].amount == 1 and presentation.damage_numbers[2].amount == 2)
      assert(presentation.damage_numbers[1].lane ~= presentation.damage_numbers[2].lane)
    end,
  },
  {
    name = "PLAY-01 player previews share direct weapon geometry and tool-like facing cells",
    run = function()
      local campaign = new_campaign(101001)
      local session, player = campaign.session, campaign.session.state.player
      local x, y = open_line(session, 4)
      player.x, player.y, player.direction = x, y, "d"
      session:refresh_visibility()
      local preview = assert(session:player_attack_preview())
      assert(#preview.cells >= 1 and preview.cells[1].x == x + 1 and preview.cells[1].y == y)
      local ability = session.registry:get_ability("ability.weapon.scatter_caster")
      local scatter = session:_preview_ability(player, ability, "d")
      assert(#scatter >= 3, "scatter preview must retain its weapon-specific fan")
      local tool_cell = assert(session:faced_cell(player))
      assert(tool_cell.x == x + 1 and tool_cell.y == y)
    end,
  },
  {
    name = "PLAY-01 hostile threat geometry is visible-only and disappears when its provider breaks",
    run = function()
      local campaign = new_campaign(101002)
      local session, player = campaign.session, campaign.session.state.player
      local x, y = open_line(session, 5)
      player.x, player.y = x, y
      local enemy = session:_make_enemy("enemy.wild.scatter_skirmisher", { x = x + 3, y = y })
      session.state.enemies = { enemy }
      session.state.visible = { [Grid.key(enemy.x, enemy.y)] = true }
      local preview = assert(session:enemy_threat_preview(enemy))
      assert(#preview.cells > 0)
      local provider = assert(session:actor_ability_provider(enemy, "ability.weapon.scatter_caster")).component
      provider.current_integrity = 0
      assert(session:enemy_threat_preview(enemy) == nil)
      session.state.visible = {}
      assert(#session:visible_enemy_threats() == 0)
    end,
  },
  {
    name = "PLAY-01 outlines apply only to player visible hostiles and projectiles",
    run = function()
      local renderer = Renderer.new({})
      local player, enemy, bullet = { kind = "player" }, { kind = "ripper" }, { kind = "bullet" }
      local state = { player = player, enemies = { enemy } }
      assert(renderer:outline_role(player, state) == "player")
      assert(renderer:outline_role(enemy, state) == "hostile")
      assert(renderer:outline_role(bullet, state) == "projectile")
      assert(renderer:outline_role({ kind = "tree" }, state) == nil)
    end,
  },
}
