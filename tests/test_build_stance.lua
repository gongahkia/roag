local App = require("src.app.app")
local Building = require("src.construction.building")
local Grid = require("src.world.grid")
local Input = require("src.app.input")
local SaveStore = require("src.persistence.save_store")

local function campaign_app(seed)
  local slots = { SaveStore.memory_directory(), SaveStore.memory_directory(), SaveStore.memory_directory() }
  local app = App.new({ seed = seed, campaign_slot_stores = slots, meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
  assert(app:request_new_campaign())
  return app
end

local function clear_cross(session)
  local world = session.state.world
  for x = 2, Grid.width - 4 do
    for y = 2, Grid.height - 3 do
      local clear = true
      for _, point in ipairs({ { x = x, y = y }, { x = x + 1, y = y }, { x = x + 2, y = y }, { x = x, y = y + 1 } }) do
        clear = clear and world:is_passable(point.x, point.y) and not world:object_at(point.x, point.y)
          and not world:is_hazardous(point.x, point.y) and not session:_actor_at(point.x, point.y)
      end
      if clear then return { x = x, y = y } end
    end
  end
  error("No clear build-stance fixture")
end

local function add_resource(session, resource_id, amount)
  local maximum = session.registry:get_resource(resource_id).max_stack
  while amount > 0 do
    local quantity = math.min(amount, maximum)
    assert(session.state.inventory:auto_place(session:create_resource_stack(resource_id, quantity, "campaign")))
    amount = amount - quantity
  end
end

local function choose_recipe(app, recipe_id)
  for index, recipe in ipairs(app:build_recipes()) do
    if recipe.id == recipe_id then
      app.build_recipe_index = index
      return recipe
    end
  end
  error("Unknown build recipe " .. recipe_id)
end

return {
  {
    name = "Campaign build stance is free, deterministic, and contextually replaces attack/swap controls",
    run = function()
      local app = campaign_app(960001)
      local session, player = app.session, app.session.state.player
      local point = clear_cross(session)
      session.state.enemies = {}
      player.x, player.y, player.direction = point.x, point.y, "d"
      add_resource(session, "resource.material.timber", 4)
      add_resource(session, "resource.material.metal", 2)
      local recipe_count = #app:build_recipes()
      assert(recipe_count >= 10)
      Input.keypressed(app, "c", nil, false)
      assert(app:is_build_stance() and app.screen == "game")
      local first = assert(app:active_build_recipe()).id
      Input.keypressed(app, "r", nil, false)
      local second = assert(app:active_build_recipe()).id
      assert(first ~= second and session.last_action_result == nil)
      Input.keypressed(app, "x", nil, false)
      assert(app:active_build_recipe().id == first and session.last_action_result == nil)
      choose_recipe(app, "construction.timber_floor")
      local target = assert(app:build_target())
      assert(target.x == point.x + 1 and target.y == point.y)
      local enemy = session:_make_enemy("enemy.legacy.cultist", { x = point.x + 2, y = point.y })
      session.state.enemies = { enemy }
      local ai_cycle = enemy.ai_cycle
      Input.keypressed(app, "e", nil, false)
      local floor = assert(session.state.world:object_at(target.x, target.y))
      assert(floor.definition_id == "world_object.build.timber_floor")
      assert(session.last_action_result.applied and session.last_action_result.recipe_id == "construction.timber_floor")
      assert(enemy.ai_cycle == ai_cycle + 1, "a successful placement receives one ordinary enemy response")
      assert(app:is_build_stance(), "a successful placement retains live build stance")
      -- The occupied preview is a free failure, not an enemy/world turn.
      choose_recipe(app, "construction.timber_wall")
      ai_cycle = enemy.ai_cycle
      Input.keypressed(app, "e", nil, false)
      assert(session.last_action_result.code == "occupied" and enemy.ai_cycle == ai_cycle)
      assert(floor == session.state.world:object_at(target.x, target.y))
      Input.keypressed(app, "c", nil, false)
      assert(not app:is_build_stance() and app.screen == "game")
    end,
  },
  {
    name = "Campaign build stance retains ordinary movement and safely suspends through inventory",
    run = function()
      local app = campaign_app(960002)
      local session, player = app.session, app.session.state.player
      local point = clear_cross(session)
      session.state.enemies = {}
      player.x, player.y, player.direction = point.x, point.y, "d"
      Input.keypressed(app, "c", nil, false)
      Input.keypressed(app, "w", nil, false)
      assert(app:is_build_stance() and player.y == point.y + 1 and player.direction == "w")
      Input.keyreleased(app, "w")
      local x, y = player.x, player.y
      Input.keypressed(app, "i", nil, false)
      assert(app.screen == "inventory" and app.build_stance and app.held_direction == nil)
      Input.keypressed(app, "e", nil, false)
      app:update(1)
      assert(player.x == x and player.y == y, "no placement or movement leaks behind paused inventory")
      Input.keypressed(app, "i", nil, false)
      assert(app.screen == "game" and app:is_build_stance())
      Input.keypressed(app, "escape", nil, false)
      assert(not app:is_build_stance())
    end,
  },
  {
    name = "Campaign build stance clears at a zone transition and returns E to attack",
    run = function()
      local app = campaign_app(960003)
      local connection = assert(app.campaign.active_zone.connections.east)
      local player = app.session.state.player
      Input.keypressed(app, "c", nil, false)
      assert(app:is_build_stance())
      player.x, player.y, player.direction = connection.boundary.x, connection.boundary.y, "d"
      Input.keypressed(app, "d", nil, false)
      assert(app.session == app.campaign.session and not app:is_build_stance())
      assert(app.held_direction == nil and next(app.movement_keys) == nil)
    end,
  },
  {
    name = "construction recipes expose stable structural then functional field order",
    run = function()
      local app = campaign_app(960004)
      local recipes = app:build_recipes()
      local seen_functional = false
      for _, recipe in ipairs(recipes) do
        if recipe.kind == "functional_device" then seen_functional = true end
        assert(recipe.kind == "functional_device" or recipe.kind == "structural_piece")
        assert(not (seen_functional and recipe.kind == "structural_piece"))
      end
      assert(recipes[1].id == "construction.timber_wall" and recipes[#recipes].id == "construction.reconstruction_station")
    end,
  },
}
