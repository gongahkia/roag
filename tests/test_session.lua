local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local Session = require("src.simulation.session")

local function new_run(seed)
  local session = Session.new({ seed = seed })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function snapshot(session)
  local state = session.state
  local values = {
    session.seed,
    state.stage,
    state.player.x,
    state.player.y,
    state.player.health,
    state.player.ammo,
    state.player.score,
    #state.targets,
    #state.enemies,
    #state.bullets,
    #state.bombs,
    #state.flares,
  }
  for _, enemy in ipairs(state.enemies) do
    values[#values + 1] = table.concat({
      enemy.kind,
      tostring(enemy.x),
      tostring(enemy.y),
      tostring(enemy.health),
      tostring(enemy.attack),
      tostring(enemy.stun),
    }, ":")
  end
  for index, value in ipairs(values) do
    values[index] = tostring(value)
  end
  return table.concat(values, "|")
end

return {
  {
    name = "session construction is headless",
    run = function()
      assert(rawget(_G, "love") == nil)
      local session = new_run(901)
      assert(session.state.player)
      assert(session.state.visible[Grid.key(session.state.player.x, session.state.player.y)])
      assert(#session.state.enemies == session.state.settings.enemies)
    end,
  },
  {
    name = "tactical visibility hides unperceived information without map discovery state",
    run = function()
      local session = new_run(900)
      local state = session.state
      state.player.x, state.player.y = 10, 10
      state.settings.vision = 2
      state.torches, state.bombs, state.flares = {}, {}, {}
      session:refresh_visibility()
      assert(state.visible[Grid.key(10, 10)])
      assert(not state.visible[Grid.key(20, 10)])
      assert(state.explored == nil and session:to_data().explored == nil)

      state.torches = { { kind = "torch", x = 20, y = 10, light = 3 } }
      session:refresh_visibility()
      assert(state.visible[Grid.key(20, 10)])
      state.torches = {}
      session:refresh_visibility()
      assert(not state.visible[Grid.key(20, 10)] and state.explored == nil)
    end,
  },
  {
    name = "same seed and actions reproduce session state",
    run = function()
      local first, second = new_run(902), new_run(902)
      for _, action in ipairs({ "w", "d", "b", "w" }) do
        first:turn(action)
        second:turn(action)
      end
      assert(snapshot(first) == snapshot(second))
    end,
  },
  {
    name = "a movement action updates the authoritative grid position",
    run = function()
      local session = new_run(903)
      local player = session.state.player
      local original_x, original_y = player.x, player.y
      session:turn("w")
      assert(player.x == original_x)
      assert(player.y == original_y + 1)
    end,
  },
  {
    name = "hazards advance after the player action in the same turn",
    run = function()
      local session = new_run(904)
      session:turn("b")
      assert(#session.state.bombs == 1)
      assert(session.state.bombs[1].fuse == 2)
    end,
  },
  {
    name = "projectiles are created by an action before their first movement turn",
    run = function()
      local session = new_run(905)
      local player = session.state.player
      local original_ammo = player.ammo
      session:turn("shoot_w")
      assert(player.ammo == original_ammo - 1)
      assert(#session.state.bullets == 1)
      assert(session.state.bullets[1].active)
      assert(session.state.bullets[1].x == player.x and session.state.bullets[1].y == player.y)
    end,
  },
}
