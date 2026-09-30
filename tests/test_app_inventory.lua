local App = require("src.app.app")
local Input = require("src.app.input")

return {
  {
    name = "held WASD combinations produce diagonal locomotion intents without rendering",
    run = function()
      local app = App.new({ seed = 9010 })
      app:select_class(app.content.classes[1])
      app:select_boon(app.boon_options[1])
      local player = app.session.state.player
      Input.keypressed(app, "w", nil, false)
      local x, y = player.x, player.y
      Input.keypressed(app, "d", nil, false)
      assert(player.direction == "ne" and player.x == x + 1 and player.y == y + 1)
      Input.keyreleased(app, "d")
      assert(app.held_direction == "w")
      Input.keyreleased(app, "w")
      assert(app.held_direction == nil)
    end,
  },
  {
    name = "inventory and salvage overlays operate without rendering",
    run = function()
      local app = App.new({ seed = 9011 })
      app:select_class(app.content.classes[1])
      app:select_boon(app.boon_options[1])
      local player = app.session.state.player
      local bomber = app.session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      app.session.state.enemies[#app.session.state.enemies + 1] = bomber
      local index = #app.session.state.enemies
      app.session:_destroy_enemy(index)

      assert(app:open_salvage() and app.screen == "salvage")
      assert(#app:salvage_options() == 3)
      assert(app:salvage_selected().applied)
      assert(app:open_inventory() and app.screen == "inventory")
      local entry = app.session.state.inventory.entries[1]
      app.inventory_cursor.x, app.inventory_cursor.y = entry.x, entry.y
      assert(app:inventory_select_or_place().physical_id == entry.physical_id)
      app:move_inventory_cursor(1, 0)
      assert(app:inventory_select_or_place())
      app:close_overlay()
      assert(app.screen == "game")
    end,
  },
  {
    name = "reconstruction overlay installs a selected component and requires explicit finish",
    run = function()
      local app = App.new({ seed = 9012 })
      app:select_class(app.content.classes[1])
      app:select_boon(app.boon_options[1])
      local player = app.session.state.player
      local bomber = app.session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      app.session.state.enemies[#app.session.state.enemies + 1] = bomber
      local charge = bomber.body:get_component("internal_1")
      app.session:_destroy_enemy(#app.session.state.enemies)
      assert(app.session:salvage_corpse_component(app.session.state.corpses[#app.session.state.corpses].id, "internal_1").applied)
      assert(app.session:_complete_stage() == "reconstruction")
      assert(app:open_reconstruction() and app.screen == "reconstruction")
      app.reconstruction_slot_index = 8
      app:toggle_reconstruction_focus()
      assert(app:reconstruction_confirm().physical_id == charge.id)
      assert(app:reconstruction_confirm().applied)
      assert(app.session.state.player.body:get_component("internal_2") == charge)
      assert(app:finish_reconstruction().applied and app.screen == "curse")
    end,
  },
}
