local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local World = require("src.world.world")
local PhysicalItem = require("src.inventory.physical_item")
local Session = require("src.simulation.session")

local LOCOMOTOR = "component.leg.legacy_locomotor"

local function new_run(seed)
  local session = Session.new({ seed = seed or 1701 })
  session:start_run(Content.classes[1], Content.boons[1])
  return session
end

local function open_floor(session)
  local state = session.state
  local layout = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      layout[Grid.key(x, y)] = true
    end
  end
  state.world = World.new(session.registry, state.settings.terrain, layout)
  state.enemies, state.targets = {}, {}
  state.bullets, state.bombs, state.flares, state.area_attacks = {}, {}, {}, {}
  state.settings.enemies, state.settings.targets = 0, 0
  state.ammo = nil
end

local function break_leg(session, actor, slot_id)
  return session:damage_actor_body(actor, {
    amount = 3,
    slot_id = slot_id,
    cause = "test",
  })
end

local function enter_reconstruction(session)
  assert(session:_complete_stage() == "reconstruction")
end

return {
  {
    name = "locomotion state derives from functional leg providers including breaks and detachments",
    run = function()
      local session = new_run(1702)
      local player = session.state.player
      local original_mass = player.body:installed_mass()
      assert(session:actor_has_capability(player, "ability.locomotion.move"))
      assert(session:locomotion_state(player).state == "NORMAL")
      assert(session:locomotion_provider_count(player) == 2)

      local first = break_leg(session, player, "left_leg")
      assert(first.became_broken and first.previous_locomotion == "NORMAL")
      assert(first.locomotion == "IMPAIRED" and first.locomotion_provider_count == 1)
      assert(player.body:get_component("left_leg"))
      assert(player.body:installed_mass() == original_mass)
      assert(session.state.log[1] == "LOCOMOTION IMPAIRED.")

      local second = break_leg(session, player, "right_leg")
      assert(second.became_broken and second.previous_locomotion == "IMPAIRED")
      assert(second.locomotion == "CRAWLING" and second.locomotion_provider_count == 0)
      assert(session.state.log[1] == "CRAWLING.")

      local detached = new_run(1703)
      local detached_player = detached.state.player
      local left = assert(detached_player.body:detach("left_leg"))
      assert(detached.state.inventory:auto_place(PhysicalItem.from_component(left, detached.registry)))
      assert(detached:locomotion_state(detached_player).state == "IMPAIRED")
      local right = assert(detached_player.body:detach("right_leg"))
      assert(detached.state.inventory:auto_place(PhysicalItem.from_component(right, detached.registry)))
      assert(detached:locomotion_state(detached_player).state == "CRAWLING")
      assert(left.definition_id == LOCOMOTOR)
      assert(detached:validate_physical_ownership())
    end,
  },
  {
    name = "normal impaired and crawling movement enforce categorical locomotion consequences",
    run = function()
      local normal = new_run(1704)
      open_floor(normal)
      for _, direction in ipairs({ "w", "a", "s", "d", "nw", "ne", "sw", "se" }) do
        assert(normal:can_move(direction), direction .. " should be legal while normal")
      end
      normal.state.player.direction = "w"
      local dash = normal:_dash()
      assert(dash.applied and normal.state.player.y == math.floor(Grid.height / 2) + 2)

      local impaired = new_run(1705)
      open_floor(impaired)
      local impaired_player = impaired.state.player
      assert(break_leg(impaired, impaired_player, "left_leg").locomotion == "IMPAIRED")
      local x, y = impaired_player.x, impaired_player.y
      assert(impaired:turn("ne") == nil)
      assert(impaired_player.x == x + 1 and impaired_player.y == y + 1)
      local no_impaired_dash = impaired:_dash()
      assert(not no_impaired_dash.applied and no_impaired_dash.code == "no_dash_while_impaired")

      local swift = Session.new({ seed = 1710 })
      swift:start_run(Content.classes[3], Content.boons[6]) -- Scout + Windwalker.
      open_floor(swift)
      assert(swift.state.player.dash_base == 1)
      break_leg(swift, swift.state.player, "left_leg")
      assert(swift:_dash().code == "no_dash_while_impaired")

      local crawling = new_run(1706)
      open_floor(crawling)
      local crawling_player = crawling.state.player
      break_leg(crawling, crawling_player, "left_leg")
      assert(break_leg(crawling, crawling_player, "right_leg").locomotion == "CRAWLING")
      crawling_player.dash = 2
      local crawl_x, crawl_y = crawling_player.x, crawling_player.y
      assert(crawling:turn("ne") == nil)
      assert(crawling_player.x == crawl_x and crawling_player.y == crawl_y)
      -- Invalid voluntary movement keeps the established failed-action rule:
      -- the turn advances, but does not corrupt movement state.
      assert(crawling_player.dash == 1)
      assert(crawling:turn("w") == nil)
      assert(crawling_player.x == crawl_x and crawling_player.y == crawl_y + 1)
      local no_crawl_dash = crawling:_dash()
      assert(not no_crawl_dash.applied and no_crawl_dash.code == "no_dash_while_crawling")
      local shot = crawling:_shoot("w")
      assert(shot.applied and #crawling.state.bullets == 1)
    end,
  },
  {
    name = "reconstruction installs exact functional legs to recover crawling through impaired to normal",
    run = function()
      local session = new_run(1707)
      local player = session.state.player
      break_leg(session, player, "left_leg")
      break_leg(session, player, "right_leg")
      assert(session:locomotion_state(player).state == "CRAWLING")
      enter_reconstruction(session)
      assert(session:uninstall_body_component("left_leg").applied)
      assert(session:uninstall_body_component("right_leg").applied)

      local first = session.component_factory:create(LOCOMOTOR)
      local second = session.component_factory:create(LOCOMOTOR)
      assert(session.state.inventory:auto_place(PhysicalItem.from_component(first, session.registry)))
      assert(session.state.inventory:auto_place(PhysicalItem.from_component(second, session.registry)))
      assert(session:install_inventory_component(first.id, "left_leg").applied)
      assert(player.body:get_component("left_leg") == first)
      assert(session:locomotion_state(player).state == "IMPAIRED")
      assert(session:install_inventory_component(second.id, "right_leg").applied)
      assert(player.body:get_component("right_leg") == second)
      assert(session:locomotion_state(player).state == "NORMAL")
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "body-bearing enemies share locomotion states and crawl more slowly without dying",
    run = function()
      local session = new_run(1708)
      open_floor(session)
      local player = session.state.player
      player.x, player.y = 40, 25
      local bomber = session:_make_enemy("bomber", { x = 30, y = 25 })
      session.state.enemies = { bomber }
      assert(session:locomotion_state(bomber).state == "NORMAL")
      local normal_x = bomber.x
      session:_enemy_turn()
      assert(bomber.x > normal_x)

      assert(break_leg(session, bomber, "left_leg").locomotion == "IMPAIRED")
      local impaired_x = bomber.x
      session:_enemy_turn()
      assert(bomber.x > impaired_x)

      assert(break_leg(session, bomber, "right_leg").locomotion == "CRAWLING")
      assert(bomber.health == 1 and session:actor_has_capability(bomber, "ability.explosive.self_destruct"))
      local crawl_x = bomber.x
      session:_enemy_turn()
      assert(bomber.x > crawl_x)
      local recovering_x = bomber.x
      session:_enemy_turn()
      assert(bomber.x == recovering_x)

      local cultist = session:_make_enemy("cultist", { x = 20, y = 25 })
      assert(session:locomotion_state(cultist).state == "NORMAL")
      assert(break_leg(session, cultist, "left_leg").locomotion == "IMPAIRED")
      assert(break_leg(session, cultist, "right_leg").locomotion == "CRAWLING")
      assert(session:actor_has_capability(cultist, "ability.arcane.burst"))
    end,
  },
  {
    name = "enemy locomotor identity survives corpse salvage and restores a player provider",
    run = function()
      local session = new_run(1709)
      local player = session.state.player
      local bomber = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      session.state.enemies = { bomber }
      local leg = bomber.body:get_component("left_leg")
      local id = leg.id
      session:_destroy_enemy(1)
      local corpse = session.state.corpses[1]
      assert(corpse.body:get_component("left_leg") == leg)
      assert(session:salvage_corpse_component(corpse.id, "left_leg").applied)
      assert(session.state.inventory:get(id).item.object == leg)

      enter_reconstruction(session)
      assert(session:uninstall_body_component("left_leg").applied)
      assert(session:locomotion_state(player).state == "IMPAIRED")
      assert(session:install_inventory_component(id, "left_leg").applied)
      assert(player.body:get_component("left_leg") == leg and leg.id == id)
      assert(session:locomotion_provider_count(player) == 2)
      assert(session:locomotion_state(player).state == "NORMAL")
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "leg removal persists into the next floor and exact reinstallation restores normal locomotion",
    run = function()
      local session = new_run(1711)
      local player = session.state.player
      local leg = player.body:get_component("left_leg")
      local id = leg.id
      enter_reconstruction(session)
      assert(session:uninstall_body_component("left_leg").applied)
      assert(session:locomotion_state(player).state == "IMPAIRED")
      assert(session:complete_reconstruction().next == "curse")
      local result = session:choose_curse(session.state.curse_options[1])
      if result.next == "route" then
        local choice = session:available_route_nodes()[1]
        assert(choice and session:select_route_node(choice.id).applied)
      end
      assert(session.state.phase == "combat" and session:locomotion_state(player).state == "IMPAIRED")
      assert(not player.body:get_component("left_leg"))
      assert(session.state.inventory:get(id).item.object == leg)

      enter_reconstruction(session)
      assert(session:install_inventory_component(id, "left_leg").applied)
      assert(player.body:get_component("left_leg") == leg and leg.id == id)
      assert(session:locomotion_state(player).state == "NORMAL")
      assert(session:validate_physical_ownership())
    end,
  },
}
