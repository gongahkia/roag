local Campaign = require("src.campaign.campaign")
local App = require("src.app.app")
local Grid = require("src.world.grid")
local Presentation = require("src.rendering.presentation")
local Renderer = require("src.rendering.renderer")
local Tuning = require("src.rendering.tuning")
local Input = require("src.app.input")

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
    name = "FEEL-02 movement presentation uses a 36ms player target and bounds visual backlog",
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
    name = "FEEL-02 Expedition camera frames an entire normal chamber as a stable board",
    run = function()
      local player = { kind = "player", x = 14, y = 14 }
      local session = { state = { player = player, enemies = {}, bullets = {}, boss = nil,
        expedition = { current_topology = "open", chamber = { bounds = { min_x = 8, max_x = 21, min_y = 9, max_y = 18 } } } } }
      local presentation = Presentation.new()
      presentation:reset(session)
      -- A 14x10 maximum normal chamber fits inside the 16x12 useful board
      -- view, so piece movement never drags the camera across the room.
      assert(math.abs(presentation.camera_x - 14.5) < 0.001 and math.abs(presentation.camera_y - 13.5) < 0.001)
      player.x, player.y = 21, 18
      presentation:update(session, 0.12)
      assert(math.abs(presentation.camera_x - 14.5) < 0.01 and math.abs(presentation.camera_y - 13.5) < 0.01)
    end,
  },
  {
    name = "FEEL-02 chamber layout frames a 16 by 12 board between left status and right progression HUDs",
    run = function()
      local renderer = Renderer.new({})
      local chamber = renderer:layout_for_dimensions(16, 12, 1920, 1080, true)
      assert(chamber.size > 24 and chamber.size <= 96)
      assert(chamber.hud_x < chamber.board_x and chamber.hud_width >= 240)
      assert(chamber.board_x + 16 * chamber.size < chamber.progress_x)
      assert(chamber.progress_x > 1920 / 2 and chamber.progress_width >= 160)
      assert(chamber.board_y >= 0 and chamber.board_y + 12 * chamber.size <= 1080)
      local sandbox = renderer:layout_for_dimensions(39, 25, 1920, 1080, false)
      assert(sandbox.size <= 24, "Sandbox keeps its legacy wide-world layout")
    end,
  },
  {
    name = "FEEL-02 progress model keeps large XP and cash data on the dedicated right HUD",
    run = function()
      local renderer = Renderer.new({})
      local model = renderer:expedition_progress_model({ expedition = {
        level = 6, xp = 43, xp_to_next = 58, currency = 72,
      } }, { action_receipt = { time = Tuning.action_receipt_lifetime, xp = 12, cash = 7 } })
      assert(model.level == 6 and model.xp == "43 / 58" and model.cash == "72")
      assert(model.xp_gain == 12 and model.cash_gain == 7 and model.pulse == 1)
    end,
  },
  {
    name = "FEEL-02 board movement uses one-cell shared segments and exact destination snaps",
    run = function()
      local player, enemy = { kind = "player", x = 5, y = 4 }, { kind = "ripper", x = 8, y = 4 }
      local session = { state = { player = player, enemies = { enemy }, bullets = {}, boss = nil } }
      local presentation = Presentation.new()
      presentation:reset(session)
      player.x, enemy.x = 6, 7
      local beat = presentation:begin_board_turn(session)
      assert(#beat.segments == 2 and beat.segments[1].distance == 1 and beat.segments[2].distance == 1)
      presentation:update(session, Tuning.player_move_duration / 2)
      local player_x, enemy_x = presentation:position(player), presentation:position(enemy)
      assert(player_x > 5 and player_x < 6 and enemy_x < 8 and enemy_x > 7)
      presentation:update(session, Tuning.player_move_duration / 2)
      player_x, enemy_x = presentation:position(player), presentation:position(enemy)
      assert(player_x == 6 and enemy_x == 7 and not presentation:is_board_turn_settled())
      presentation:update(session, Tuning.board_settle_duration)
      assert(presentation:is_board_turn_settled())
    end,
  },
  {
    name = "FEEL-02 forced displacement is also rendered as discrete board edges",
    run = function()
      local player = { kind = "player", x = 5, y = 4 }
      local session = { state = { player = player, enemies = {}, bullets = {}, boss = nil } }
      local presentation = Presentation.new()
      presentation:reset(session)
      player.x = 8
      local beat = presentation:begin_board_turn(session)
      assert(#beat.segments == 3)
      for index, segment in ipairs(beat.segments) do
        assert(segment.distance == 1 and segment.from_x == 4 + index and segment.to_x == 5 + index)
      end
      presentation:update(session, Tuning.player_move_duration / 2)
      assert(select(1, presentation:position(player)) > 5 and select(1, presentation:position(player)) < 6)
      presentation:update(session, Tuning.player_move_duration / 2)
      assert(select(1, presentation:position(player)) == 6)
      presentation:update(session, Tuning.player_move_duration / 2)
      assert(select(1, presentation:position(player)) > 6 and select(1, presentation:position(player)) < 7)
    end,
  },
  {
    name = "FEEL-02 held movement dispatches one turn per visible board beat without stale backlog",
    run = function()
      local player, enemy = { kind = "player", x = 5, y = 5 }, { kind = "ripper", x = 9, y = 5 }
      local session = { state = { player = player, enemies = { enemy }, bullets = {}, boss = nil }, last_action_result = nil }
      local turns, enemy_opportunities, inputs = 0, 0, {}
      function session:can_move(direction) return direction ~= "x" end
      function session:turn(input)
        turns, enemy_opportunities = turns + 1, enemy_opportunities + 1
        inputs[#inputs + 1] = input
        local delta = ({ w = { 0, 1 }, a = { -1, 0 }, s = { 0, -1 }, d = { 1, 0 } })[input]
        if input == "x" then self.last_action_result = { code = "blocked_terrain" }
        elseif input == "z" then self.last_action_result = { code = "enemy_bump" }
        elseif input == "attack" then self.last_action_result = nil
        else
          self.last_action_result = nil
          player.x, player.y = player.x + delta[1], player.y + delta[2]
          enemy.x = enemy.x - 1
        end
      end
      local run = { session = session, turn = function(_, input) return session:turn(input) end }
      local app = setmetatable({ screen = "game", session = session, expedition = run, presentation = Presentation.new(),
        held_direction = nil, held_movement_blocked = false, pending_movement_inputs = {} }, App)
      app.presentation:reset(session)
      local observed_segments, begin_board_turn = {}, app.presentation.begin_board_turn
      function app.presentation:begin_board_turn(value)
        local beat = begin_board_turn(self, value)
        for _, segment in ipairs(beat.segments) do observed_segments[#observed_segments + 1] = segment end
        return beat
      end
      app:start_held_move("d")
      assert(app:request_movement("d", "direct") and turns == 1 and enemy_opportunities == 1)
      -- A held repeat cannot pass the active player/enemy board segment.
      app:update(Tuning.player_move_duration / 2)
      assert(turns == 1)
      for target = 2, 5 do
        for _ = 1, 8 do app:update(0.02); if turns >= target then break end end
        assert(turns == target and enemy_opportunities == target)
      end
      -- Rapid direct directions keep distinct, bounded segments rather than
      -- collapsing an A→C visual jump into one tween.
      app:clear_held_move_intent()
      app:request_movement("d", "direct")
      app:request_movement("w", "direct")
      app:request_movement("a", "direct")
      assert(#app.pending_movement_inputs <= App.MAX_PENDING_MOVEMENT_INPUTS)
      for _ = 1, 24 do app:update(0.02) end
      assert(turns == 8 and enemy_opportunities == 8)
      assert(inputs[6] == "d" and inputs[7] == "w" and inputs[8] == "a")
      for _, segment in ipairs(observed_segments) do assert(segment.distance == 1) end
      app:request_movement("x", "direct") -- wall bump consumes one direct Expedition turn
      assert(turns == 9 and enemy_opportunities == 9)
      app:start_held_move("z")
      app:request_movement("z", "direct") -- hostile bump is also exactly one turn
      assert(turns == 10 and enemy_opportunities == 10 and app.held_movement_blocked)
      app:clear_held_move_intent()
      assert(#app.pending_movement_inputs == 0)
      app:perform_turn("attack")
      assert(turns == 11 and enemy_opportunities == 11 and inputs[11] == "attack")
    end,
  },
  {
    name = "FEEL-01 debug overlay is opt-in and never alters a simulation turn",
    run = function()
      local calls = 0
      local app = { screen = "game", debug_overlay = false }
      function app:perform_turn() calls = calls + 1 end
      Input.keypressed(app, "f3", nil, false)
      assert(app.debug_overlay and calls == 0)
      Input.keypressed(app, "f3", nil, false)
      assert(not app.debug_overlay and calls == 0)
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
