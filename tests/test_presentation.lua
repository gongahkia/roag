local Presentation = require("src.rendering.presentation")
local Renderer = require("src.rendering.renderer")

local function idle_session()
  local player = { kind = "player", content_id = "actor.player.legacy", x = 10, y = 10 }
  return {
    state = { player = player, enemies = {}, bullets = {}, boss = nil },
  }, player
end

return {
  {
    name = "terrain remains renderable outside tactical line of sight",
    run = function()
      local renderer = Renderer.new({})
      assert(renderer:terrain_is_renderable(20, 10))
      assert(not renderer:terrain_is_renderable(-1, 10))
    end,
  },
  {
    name = "facing marker remains a small cardinal presentation affordance",
    run = function()
      local renderer = Renderer.new({})
      local north_x, north_y, marker = assert(renderer:facing_marker_bounds("w", 10, 20, 20))
      local east_x, east_y = assert(renderer:facing_marker_bounds("d", 10, 20, 20))
      assert(marker >= 2 and north_y < east_y and east_x > north_x)
      assert(renderer:facing_marker_bounds("ne", 10, 20, 20) == nil)
    end,
  },
  {
    name = "idle sprite transforms are stable, bounded, and never mutate authoritative actor state",
    run = function()
      local session, player = idle_session()
      local enemy = { kind = "ripper", content_id = "enemy.wild.ripper", x = 12, y = 10 }
      session.state.enemies = { enemy }
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:update(session, 0)
      local first = assert(presentation:idle_transform(session, enemy, 0.35, 20))
      local second = assert(presentation:idle_transform(session, enemy, 0.35, 20))
      assert(first.offset_x == second.offset_x and first.offset_y == second.offset_y)
      assert(first.scale_x == second.scale_x and first.scale_y == second.scale_y)
      assert(math.abs(first.offset_x) <= 0.25 and math.abs(first.offset_y) <= 0.7)
      assert(first.scale_x >= 0.988 and first.scale_x <= 1.012)
      assert(first.scale_y >= 0.98 and first.scale_y <= 1.02)
      assert(player.x == 10 and player.y == 10 and enemy.x == 12 and enemy.y == 10)
    end,
  },
  {
    name = "idle transforms suspend while the player or an actor is interpolating and bosses interpolate normally",
    run = function()
      local session, player = idle_session()
      local enemy = { kind = "ripper", content_id = "enemy.wild.ripper", x = 12, y = 10 }
      local boss = { kind = "boss", content_id = "boss.apex.kinetic_harbinger", x = 16, y = 10 }
      session.state.enemies, session.state.boss = { enemy }, boss
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:update(session, 0)
      player.x = 11
      presentation:update(session, 0.01)
      assert(presentation:idle_transform(session, enemy, 0.5, 20) == nil)
      player.x = 10
      presentation:reset(session)
      presentation:update(session, 0)
      enemy.x = 13
      boss.x = 17
      presentation:update(session, 0.01)
      assert(presentation:idle_transform(session, enemy, 0.5, 20) == nil)
      local boss_x = select(1, presentation:position(boss))
      assert(boss_x > 16 and boss_x < 17)
    end,
  },
  {
    name = "reset seeds actor positions so the first move slides and gets a motion stretch",
    run = function()
      local session, player = idle_session()
      local presentation = Presentation.new()
      presentation:reset(session)
      player.x = 11
      presentation:update(session, 0.01)
      local rendered_x = select(1, presentation:position(player))
      assert(rendered_x > 10 and rendered_x < 11, "The first move after a reset must not snap")
      local transform = assert(presentation:movement_transform(player))
      assert(transform.scale_x > 1 and transform.scale_y < 1)
      assert(player.x == 11, "Presentation transforms must not mutate simulation state")
    end,
  },
  {
    name = "enemy bump presentation recoils without changing authoritative player position",
    run = function()
      local session, player = idle_session()
      local presentation = Presentation.new()
      presentation:reset(session)
      presentation:bump("d")
      presentation:update(session, 0.04)
      local transform = assert(presentation:player_bump_transform(20))
      assert(transform.offset_x ~= 0 and transform.offset_y == 0)
      assert(player.x == 10 and player.y == 10)
      presentation:update(session, 1)
      assert(presentation:player_bump_transform(20) == nil)
    end,
  },
  {
    name = "forecast rendering draws an underlay and visible danger outline without changing telegraph authority",
    run = function()
      local prior_love, calls = love, {}
      love = { graphics = {
        setColor = function(...) calls[#calls + 1] = { kind = "color", values = { ... } } end,
        rectangle = function(mode, ...) calls[#calls + 1] = { kind = "rectangle", mode = mode, values = { ... } } end,
        setLineWidth = function(width) calls[#calls + 1] = { kind = "line_width", width = width } end,
      } }
      local ok, reason = xpcall(function()
        local renderer = Renderer.new({})
        renderer:_draw_forecast_fill(5, 7, 20, "warn", 0)
        renderer:_draw_forecast_outline(5, 7, 20, "danger", 0)
      end, debug.traceback)
      love = prior_love
      assert(ok, reason)
      assert(calls[2].kind == "rectangle" and calls[2].mode == "fill")
      local outlined = false
      for _, call in ipairs(calls) do
        outlined = outlined or (call.kind == "rectangle" and call.mode == "line")
      end
      assert(outlined, "forecast must retain a foreground outline above actors")
    end,
  },
}
