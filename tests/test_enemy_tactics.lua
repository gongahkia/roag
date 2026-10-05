local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local SCATTER = "ability.weapon.scatter_caster"
local LANCE = "ability.weapon.piercing_lance"

local function open_session(seed)
  local session = Session.new({ seed = seed or 203001 })
  session:start_run(Content.classes[1], Content.boons[1])
  local layout = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do layout[Grid.key(x, y)] = true end
  end
  local state = session.state
  state.world = World.new(session.registry, "forest", layout, state)
  state.enemies, state.targets, state.corpses = {}, {}, {}
  state.bullets, state.bombs, state.flares, state.area_attacks, state.effects = {}, {}, {}, {}, {}
  state.torches, state.ammo, state.exit, state.phase = {}, nil, nil, "combat"
  state.player.x, state.player.y = 10, 10
  return session
end

return {
  {
    name = "scatter caster launches a deterministic three pellet fan",
    run = function()
      local session = open_session(203002)
      local player = session.state.player
      local arm = assert(player.body:detach("right_arm"))
      local caster = session.component_factory:create("component.arm.scatter_caster")
      assert(player.body:install("right_arm", caster))
      local ammo = player.ammo
      local result = session:activate_actor_ability(player, SCATTER, { direction = "d" })
      assert(result.applied and #result.projectiles == 3 and player.ammo == ammo - 1)
      local directions = {}
      for _, projectile in ipairs(result.projectiles) do directions[#directions + 1] = projectile.direction end
      assert(table.concat(directions, ",") == "ne,d,se")
      assert(arm.id ~= caster.id)
    end,
  },
  {
    name = "piercing lance damages two lined-up hostiles but stops after its pierce budget",
    run = function()
      local session = open_session(203003)
      local player = session.state.player
      local old_arm = assert(player.body:detach("right_arm"))
      local lance = session.component_factory:create("component.arm.piercing_lance")
      assert(player.body:install("right_arm", lance))
      player.ammo = 5
      local first = session:_make_enemy("enemy.wild.ripper", { x = 12, y = 10 })
      local second = session:_make_enemy("enemy.wild.ripper", { x = 14, y = 10 })
      first.health, second.health = 3, 3
      session.state.enemies = { first, second }
      assert(session:activate_actor_ability(player, LANCE, { direction = "d" }).applied)
      for _ = 1, 5 do session:_update_bullets() end
      assert(first.health == 1 and second.health == 1)
      assert(#session.state.bullets == 0 and old_arm.id ~= lance.id)
    end,
  },
  {
    name = "enemy navigation reuses faction and route fields while keeping tactical spacing",
    run = function()
      local session = open_session(203004)
      local first = session:_make_enemy("enemy.wild.ripper", { x = 20, y = 10 })
      local second = session:_make_enemy("enemy.wild.ripper", { x = 20, y = 13 })
      local third = session:_make_enemy("enemy.wild.ripper", { x = 20, y = 16 })
      session.state.enemies = { first, second, third }
      session:_enemy_turn()
      local stats = session:navigation_stats()
      assert(stats.hostile_fields == 1 and stats.route_fields == 1 and stats.fallbacks == 0)
      assert(first.x < 20 and second.x < 20 and third.x < 20)
      assert(Grid.key(first.x, first.y) ~= Grid.key(second.x, second.y))
    end,
  },
  {
    name = "tactical roles create retreating skirmishers and delayed flank fire",
    run = function()
      local session = open_session(203005)
      local skirmisher = session:_make_enemy("enemy.wild.scatter_skirmisher", { x = 11, y = 10 })
      session.state.enemies = { skirmisher }
      session:_enemy_turn()
      assert(skirmisher.ai_role == "skirmisher" and Grid.distance(skirmisher, session.state.player) > 1)
      assert(#session.state.bullets == 0)

      local flanker = session:_make_enemy("enemy.cave.lance_acolyte", { x = 15, y = 10 })
      session.state.enemies = { flanker }
      session:_enemy_turn()
      assert(flanker.ai_role == "flanker" and flanker.x < 15 and #session.state.bullets == 0)
      session:_enemy_turn()
      session:_enemy_turn()
      assert(#session.state.bullets == 1 and session.state.bullets[1].ability_id == LANCE)
    end,
  },
}
