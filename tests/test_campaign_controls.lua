local App = require("src.app.app")
local Campaign = require("src.campaign.campaign")
local Economy = require("src.simulation.economy")
local Grid = require("src.world.grid")
local Input = require("src.app.input")
local SaveStore = require("src.persistence.save_store")

local function new_campaign(seed, emit)
  return Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed, emit = emit })
end

local function clear_cross(session, options)
  options = options or {}
  local world = session.state.world
  for x = 2, Grid.width - 3 do
    for y = 2, Grid.height - 3 do
      local cells = {
        { x = x, y = y }, { x = x + 1, y = y },
        { x = x, y = y + 1 }, { x = x, y = y - 1 },
      }
      if not options.west_wall then cells[#cells + 1] = { x = x - 1, y = y } end
      if options.east_two then cells[#cells + 1] = { x = x + 2, y = y } end
      local clear = true
      for _, cell in ipairs(cells) do
        clear = clear and world:is_passable(cell.x, cell.y) and not world:object_at(cell.x, cell.y)
          and not world:is_hazardous(cell.x, cell.y)
      end
      if options.west_wall then clear = clear and not world:is_passable(x - 1, y) end
      if clear then return { x = x, y = y } end
    end
  end
  error("No clear Campaign control fixture cell")
end

local function clear_east_wall(session)
  local world = session.state.world
  for x = 2, Grid.width - 3 do
    for y = 2, Grid.height - 3 do
      if world:is_passable(x, y) and not world:is_passable(x + 1, y)
        and not world:object_at(x, y) and not world:is_hazardous(x, y) then
        return { x = x, y = y }
      end
    end
  end
  error("No Campaign wall-control fixture cell")
end

local function campaign_app(seed)
  local slots = { SaveStore.memory_directory(), SaveStore.memory_directory(), SaveStore.memory_directory() }
  local app = App.new({ seed = seed, campaign_slot_stores = slots, meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
  assert(app:request_new_campaign())
  return app
end

return {
  {
    name = "Campaign movement accepts only cardinal player directions while legacy diagonal support remains isolated",
    run = function()
      local campaign = new_campaign(920001)
      local session, point = campaign.session, clear_cross(campaign.session)
      session.state.enemies = {}
      for _, direction in ipairs({ "w", "a", "s", "d" }) do
        session.state.player.x, session.state.player.y, session.state.player.direction = point.x, point.y, "w"
        assert(session:turn(direction) == nil)
        assert(session.last_action_result.applied and session.state.player.direction == direction)
      end
      for _, direction in ipairs({ "nw", "ne", "sw", "se" }) do
        session.state.player.x, session.state.player.y, session.state.player.direction = point.x, point.y, "w"
        assert(session:turn(direction) == "no_action")
        assert(session.last_action_result.code == "invalid_player_direction")
        assert(session.state.player.x == point.x and session.state.player.y == point.y and session.state.player.direction == "w")
      end
    end,
  },
  {
    name = "Campaign held input resolves to the most recently pressed cardinal key",
    run = function()
      local app = campaign_app(920002)
      assert(app:set_movement_key("w", true) == "w")
      assert(app:set_movement_key("d", true) == "d")
      assert(app:set_movement_key("d", false) == "w")
      assert(app:set_movement_key("w", false) == nil)
    end,
  },
  {
    name = "Campaign enemy bump preserves occupancy consumes one turn and emits a presentation event",
    run = function()
      local events = {}
      local campaign = new_campaign(920003, function(event) events[#events + 1] = event end)
      local session, point = campaign.session, clear_cross(campaign.session)
      local player = session.state.player
      player.x, player.y, player.direction = point.x, point.y, "w"
      local enemy = session:_make_enemy("enemy.legacy.cultist", { x = point.x + 1, y = point.y })
      session.state.enemies = { enemy }
      assert(session:turn("d") == nil)
      assert(session.last_action_result.code == "enemy_bump" and session.last_action_result.consumed)
      assert(player.x == point.x and player.y == point.y and player.direction == "d")
      assert(enemy.x == point.x + 1 and enemy.y == point.y and enemy.ai_cycle == 1)
      local bump
      for _, event in ipairs(events) do if event.type == "bump" then bump = event; break end end
      assert(bump and bump.type == "bump" and bump.value.direction == "d")
    end,
  },
  {
    name = "Campaign enemy bump permits ordinary enemy damage without a forced counterattack path",
    run = function()
      local campaign = new_campaign(920004)
      local session, point = campaign.session, clear_cross(campaign.session, { west_wall = true })
      local player = session.state.player
      player.x, player.y = point.x, point.y
      local enemy = session:_make_enemy("enemy.wild.ripper", { x = point.x + 1, y = point.y })
      session.state.enemies = { enemy }
      local health = player.health
      session:turn("d")
      assert(session.last_action_result.code == "enemy_bump")
      assert(player.health < health and player.x == point.x and player.y == point.y)
      assert(enemy.ai_cycle == 1)
    end,
  },
  {
    name = "held Campaign movement bumps an enemy once then stops until fresh input",
    run = function()
      local app = campaign_app(920005)
      local session, point = app.session, clear_cross(app.session, { east_two = true })
      session.state.player.x, session.state.player.y, session.state.player.direction = point.x, point.y, "w"
      local enemy = session:_make_enemy("enemy.legacy.cultist", { x = point.x + 2, y = point.y })
      session.state.enemies = { enemy }
      Input.keypressed(app, "d", nil, false)
      assert(session.state.player.x == point.x + 1)
      local turns_before_bump = enemy.ai_cycle
      app:update(App.HOLD_INITIAL_DELAY + 0.01)
      assert(session.last_action_result.code == "enemy_bump" and enemy.ai_cycle == turns_before_bump + 1)
      app:update(2)
      assert(enemy.ai_cycle == turns_before_bump + 1 and app.held_direction == nil and app.held_movement_blocked)
    end,
  },
  {
    name = "Campaign terrain bumps remain non-actions and do not change facing",
    run = function()
      local campaign = new_campaign(920006)
      local session, point = campaign.session, clear_east_wall(campaign.session)
      local player = session.state.player
      session.state.enemies = {}
      player.x, player.y, player.direction = point.x, point.y, "w"
      assert(session:turn("d") == "no_action")
      assert(session.last_action_result.code == "blocked_terrain")
      assert(player.x == point.x and player.y == point.y and player.direction == "w")
    end,
  },
  {
    name = "encumbrance changes only held Campaign movement repeat cadence",
    run = function()
      local light = App.movement_repeat_interval_for_encumbrance("LIGHT")
      assert(light < App.movement_repeat_interval_for_encumbrance("BURDENED"))
      assert(App.movement_repeat_interval_for_encumbrance("BURDENED") < App.movement_repeat_interval_for_encumbrance("HEAVY"))
      assert(App.movement_repeat_interval_for_encumbrance("HEAVY") < App.movement_repeat_interval_for_encumbrance("OVERLOADED"))
      assert(light == App.HOLD_REPEAT_DELAY)
    end,
  },
  {
    name = "Campaign E dispatches a facing attack while arrow keys no longer issue field attacks",
    run = function()
      local app = campaign_app(920009)
      local session, player = app.session, app.session.state.player
      session.state.enemies, session.state.bullets = {}, {}
      player.direction = "a"
      Input.keypressed(app, "left", nil, false)
      assert(player.direction == "a" and #session.state.bullets == 0)
      Input.keypressed(app, "e", nil, false)
      assert(session.last_action_result.applied and player.direction == "a" and #session.state.bullets == 1)
    end,
  },
  {
    name = "Campaign U only uses the faced tile and opens the existing corpse salvage modal",
    run = function()
      local app = campaign_app(920007)
      local session, point = app.session, clear_cross(app.session)
      local player, world = session.state.player, session.state.world
      session.state.enemies = {}
      player.x, player.y, player.direction = point.x, point.y, "d"
      local door = assert(world:place_object("world_object.build.door", point.x + 1, point.y))
      local service = assert(world:place_object("world_object.service.kiosk", point.x, point.y + 1,
        { service_id = "service.supply.legacy", service_stock = Economy.create_stock(session,
          "service.supply.legacy", session.rng:derive("campaign-controls-service"), false) }))
      local corpse_actor = session:_make_enemy("enemy.legacy.cultist", { x = point.x - 1, y = point.y })
      local corpse = assert(session:_create_corpse(corpse_actor))
      Input.keypressed(app, "u", nil, false)
      assert(door.door_state == "open" and service.interaction_role == "service" and app.screen == "game")
      Input.keypressed(app, "d", nil, false)
      Input.keypressed(app, "a", nil, false)
      assert(player.direction == "a" and player.x == point.x)
      Input.keypressed(app, "u", nil, false)
      assert(app.screen == "salvage" and app.salvage_corpse_id == corpse.id)
    end,
  },
  {
    name = "inventory opening clears held Campaign input and prevents buffered world actions",
    run = function()
      local app = campaign_app(920008)
      local session, point = app.session, clear_cross(app.session)
      session.state.enemies = {}
      session.state.player.x, session.state.player.y = point.x, point.y
      Input.keypressed(app, "d", nil, false)
      local x, y = session.state.player.x, session.state.player.y
      Input.keypressed(app, "i", nil, false)
      assert(app.screen == "inventory" and app.held_direction == nil)
      Input.keypressed(app, "e", nil, false)
      Input.keypressed(app, "u", nil, false)
      app:update(2)
      assert(session.state.player.x == x and session.state.player.y == y)
      Input.keypressed(app, "i", nil, false)
      app:update(2)
      assert(app.screen == "game" and session.state.player.x == x and session.state.player.y == y)
    end,
  },
  {
    name = "Campaign zone transitions clear held movement before the destination can receive input",
    run = function()
      local app = campaign_app(920010)
      local connection = assert(app.campaign.active_zone.connections.east)
      local player = app.session.state.player
      player.x, player.y = connection.boundary.x, connection.boundary.y
      Input.keypressed(app, "d", nil, false)
      assert(app.session == app.campaign.session and app.campaign.active_zone.key.world_x == 1)
      assert(app.held_direction == nil and next(app.movement_keys) == nil)
    end,
  },
}
