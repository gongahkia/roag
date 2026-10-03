local App = require("src.app.app")
local Input = require("src.app.input")
local Renderer = require("src.rendering.renderer")

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
    name = "U routes a gameplay interaction request through the input boundary",
    run = function()
      local calls = {}
      local app = {
        screen = "game",
        perform_turn = function(_, input)
          calls[#calls + 1] = input
        end,
      }
      Input.keypressed(app, "u", nil, false)
      assert(#calls == 1 and calls[1] == "interact")
    end,
  },
  {
    name = "clicking a title option activates the same new-run flow as Enter",
    run = function()
      local app = App.new({ seed = 9015 })
      local width, height = 900, 700
      local new_run_y = height / 2 + 26 + 29
      assert(app:mousepressed(width / 2, new_run_y, 1, width, height))
      assert(app.screen == "onboarding" or app.screen == "game")
    end,
  },
  {
    name = "a title background click confirms the highlighted New Run option",
    run = function()
      local app = App.new({ seed = 9018 })
      assert(app:mousepressed(1, 1, 1, 900, 700))
      assert(app.screen == "onboarding" or app.screen == "game")
    end,
  },
  {
    name = "New Run asks before replacing an existing campaign and starts fresh after confirmation",
    run = function()
      local app = App.new({ seed = 9019 })
      local first = assert(app:request_new_campaign())
      app:return_to_title()
      assert(not app:begin_new_campaign() and app.screen == "replace_campaign")
      local replacement = assert(app:confirm_replace_campaign())
      assert(app.screen == "game" and replacement ~= first)
    end,
  },
  {
    name = "title clicks tolerate Windows physical-pixel mouse coordinates",
    run = function()
      local prior_love = love
      love = {
        graphics = { getDimensions = function() return 1280, 720 end },
        window = { getDPIScale = function() return 1.5 end },
      }
      local ok, error_data = xpcall(function()
        local app = App.new({ seed = 9016 })
        -- The rendered NEW RUN line is at logical 622,423. Windows can pass
        -- the corresponding 1.5x physical coordinate to mousepressed.
        assert(app:title_option_at(933, 635) == 1)
      end, debug.traceback)
      love = prior_love
      assert(ok, error_data)
    end,
  },
  {
    name = "title clicks retain common Windows scaling fallback when DPI reporting is unavailable",
    run = function()
      local prior_love = love
      love = {
        graphics = { getDimensions = function() return 1280, 720 end },
        window = { getDPIScale = function() return 1 end },
      }
      local ok, error_data = xpcall(function()
        local app = App.new({ seed = 9017 })
        assert(app:title_option_at(933, 635) == 1)
      end, debug.traceback)
      love = prior_love
      assert(ok, error_data)
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
    name = "inventory drag previews the live drop cell and commits only on release",
    run = function()
      local app = App.new({ seed = 9014 })
      app:select_class(app.content.classes[1])
      app:select_boon(app.boon_options[1])
      local inventory = app.session.state.inventory
      local cargo = app.session:create_resource_stack("resource.material.timber", 1, "test")
      assert(inventory:place(cargo, 1, 1))
      assert(app:open_inventory())
      local layout = assert(app:inventory_layout(900, 760))
      assert(layout.grid_x == math.floor((900 - layout.width) / 2), "Inventory grid must be centred")
      local from_x, from_y = layout.grid_x + layout.cell * 0.5, layout.grid_y + layout.cell * 0.5
      local to_x, to_y = layout.grid_x + layout.cell * 3.5, layout.grid_y + layout.cell * 2.5
      assert(app:inventory_mousepressed(from_x, from_y, 1, 900, 760))
      local drag = assert(app:inventory_mousemoved(to_x, to_y, 0, 0, 900, 760))
      assert(drag.x == 4 and drag.y == 3 and drag.valid)
      local moved = assert(app:inventory_mousereleased(to_x, to_y, 1, 900, 760))
      assert(moved.x == 4 and moved.y == 3)
      assert(inventory:item_at(4, 3).physical_id == cargo.physical_id)
      assert(not app.inventory_drag and not app.inventory_selected_id)
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
  {
    name = "content-backed curse menus use their display names without a renderer schema crash",
    run = function()
      local app = App.new({ seed = 9013 })
      app:select_class(app.content.classes[1])
      app:select_boon(app.boon_options[1])
      assert(app.session:_complete_stage() == "reconstruction")
      assert(app:finish_reconstruction().applied and app.screen == "curse")
      local curse = assert(app.session.state.curse_options[1])
      assert(curse.display_name and not curse.name)
      local renderer = Renderer.new({})
      assert(renderer:menu_item_label(curse) == curse.display_name)
    end,
  },
}
