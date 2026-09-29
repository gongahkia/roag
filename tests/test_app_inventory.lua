local App = require("src.app.app")

return {
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
      assert(#app:salvage_options() == 1)
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
}
