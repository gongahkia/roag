-- LÖVE event translation only. Game rules remain in Session.
local Input = {}

local MOVE_KEYS = { w = true, a = true, s = true, d = true }
local SHOT_KEYS = { up = "w", left = "a", down = "s", right = "d" }

local function campaign_field(app)
  return app.is_campaign_mode and app:is_campaign_mode()
end

function Input.keypressed(app, key, _, is_repeat)
  if app.screen == "build" then
    local recipes = app:build_recipes()
    if key == "escape" or key == "c" then
      app:close_overlay()
    elseif key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #recipes))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #recipes))
    elseif key == "return" or key == "space" then
      app:select_build_recipe()
    end
    return
  end

  if app.screen == "build_place" then
    if key == "escape" or key == "c" then
      app.screen = "build"
    elseif key == "w" or key == "up" then
      app:move_build_cursor(0, -1)
    elseif key == "s" or key == "down" then
      app:move_build_cursor(0, 1)
    elseif key == "a" or key == "left" then
      app:move_build_cursor(-1, 0)
    elseif key == "d" or key == "right" then
      app:move_build_cursor(1, 0)
    elseif key == "return" or key == "space" then
      app:confirm_build()
    end
    return
  end

  if app.screen == "storage" then
    if key == "escape" or key == "u" then
      app:close_overlay()
    elseif key == "tab" then
      app:toggle_storage_focus()
    elseif key == "w" or key == "up" then
      app:move_storage_selection(-1)
    elseif key == "s" or key == "down" then
      app:move_storage_selection(1)
    elseif key == "return" or key == "space" then
      app:storage_transfer_selected()
    end
    return
  end

  if app.screen == "inventory" then
    if key == "escape" or key == "i" then
      app:close_overlay()
    elseif key == "w" or key == "up" then
      app:move_inventory_cursor(0, -1)
    elseif key == "s" or key == "down" then
      app:move_inventory_cursor(0, 1)
    elseif key == "a" or key == "left" then
      app:move_inventory_cursor(-1, 0)
    elseif key == "d" or key == "right" then
      app:move_inventory_cursor(1, 0)
    elseif key == "return" or key == "space" then
      app:inventory_select_or_place()
    elseif key == "r" then
      app:rotate_inventory_item()
    end
    return
  end

  if app.screen == "salvage" then
    local options = app:salvage_options()
    if key == "escape" or key == "g" then
      app:close_overlay()
    elseif key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #options))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #options))
    elseif key == "return" or key == "e" then
      app:salvage_selected()
    end
    return
  end

  if app.screen == "reconstruction" then
    if key == "tab" then
      app:toggle_reconstruction_focus()
    elseif key == "w" or key == "up" then
      app:move_reconstruction_selection(-1)
    elseif key == "s" or key == "down" then
      app:move_reconstruction_selection(1)
    elseif key == "return" or key == "space" then
      app:reconstruction_confirm()
    elseif key == "r" then
      app:rotate_reconstruction_item()
    elseif key == "f" then
      app:finish_reconstruction()
    end
    return
  end

  if app.screen == "body_abilities" then
    if key == "escape" or key == "x" then
      app:close_overlay()
    elseif key == "w" or key == "up" then
      app:move_menu(-1, #app.body_ability_options)
      app.body_ability_confirming = false
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app.body_ability_options)
      app.body_ability_confirming = false
    elseif key == "return" or key == "space" then
      app:confirm_body_ability()
    end
    return
  end

  -- Research and fallen-history are title-side screens; their own handlers
  -- return to title rather than turning an ordinary browse action into quit.
  if app.screen == "campaign_slots" then
    local options = app:campaign_slot_options()
    if key == "escape" then
      app.screen, app.menu = "title", 1
    elseif key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #options))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #options))
    elseif key == "return" or key == "space" then
      app:select_campaign_slot()
    end
    return
  end
  if key == "escape" and app.screen ~= "research" and app.screen ~= "fallen_archive" and app.screen ~= "help" and app.screen ~= "onboarding" then
    app:quit()
    return
  end
  if app.screen == "title" then
    if key == "w" or key == "up" then
      app:move_menu(-1, #app:title_options())
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app:title_options())
    elseif key == "return" or key == "space" then
      app:activate_title_choice()
    end
    return
  end
  if app.screen == "help" then
    if key == "escape" or key == "return" or key == "space" then app.screen, app.menu = "title", 1 end
    return
  end
  if app.screen == "onboarding" then
    if key == "escape" then app.screen, app.menu = "title", 1
    elseif key == "return" or key == "space" then app:request_new_run() end
    return
  end
  if app.screen == "research" then
    local categories = app:research_categories()
    local options = app:research_options(app:current_research_category())
    if key == "escape" then
      app.screen, app.menu = "title", 1
    elseif key == "a" or key == "left" then
      app.research_category_index = math.max(1, (app.research_category_index or 1) - 1)
      app.research_node_index = 1
    elseif key == "d" or key == "right" then
      app.research_category_index = math.min(#categories, (app.research_category_index or 1) + 1)
      app.research_node_index = 1
    elseif key == "w" or key == "up" then
      app.research_node_index = math.max(1, (app.research_node_index or 1) - 1)
    elseif key == "s" or key == "down" then
      app.research_node_index = math.min(math.max(1, #options), (app.research_node_index or 1) + 1)
    elseif key == "return" or key == "e" then
      app:purchase_selected_research()
    end
    return
  end
  if app.screen == "fallen_archive" then
    local entries = app:fallen_archive_entries()
    if key == "escape" then
      app.screen, app.menu = "title", 1
    elseif key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #entries))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #entries))
    end
    return
  end
  if app.screen == "replace_save" then
    if key == "return" or key == "space" then
      app:confirm_replace_save()
    elseif key == "escape" then
      app.screen, app.menu = "title", 1
    end
    return
  end
  if app.screen == "replace_campaign" then
    if key == "return" or key == "space" then
      app:confirm_replace_campaign()
    elseif key == "escape" then
      app.screen, app.menu = "title", 1
    end
    return
  end
  if app.screen == "curse" then
    if key == "w" or key == "up" then
      app:move_menu(-1, #app.session.state.curse_options)
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app.session.state.curse_options)
    elseif key == "return" or key == "e" then
      app:select_curse(app.session.state.curse_options[app.menu])
    end
    return
  end
  if app.screen == "route" then
    local options = app:route_options()
    if key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #options))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #options))
    elseif key == "return" or key == "e" then
      app:select_route_choice()
    end
    return
  end
  if app.screen == "service_hub" then
    local options = app:service_hub_options()
    if key == "w" or key == "up" then
      app:move_menu(-1, #options)
    elseif key == "s" or key == "down" then
      app:move_menu(1, #options)
    elseif key == "return" or key == "e" then
      app:select_service_hub_option()
    end
    return
  end
  if app.screen == "service" then
    local options = app:service_options()
    if key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #options))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #options))
    elseif key == "b" or key == "return" or key == "e" then
      app:service_execute_selected()
    elseif key == "v" then
      local option = options[app.menu]
      if option and (option.action == "sell_component" or option.action == "remove_charm") then
        app:service_execute_selected()
      end
    elseif key == "escape" then
      app:close_service()
    end
    return
  end
  if app.screen == "gameover" or app.screen == "victory" then
    if key == "return" then
      app:return_to_title()
    end
    return
  end
  if app.screen == "game" then
    local campaign = campaign_field(app)
    if MOVE_KEYS[key] then
      if not is_repeat then
        local direction = app:set_movement_key(key, true)
        if direction then
          app:start_held_move(direction)
          app:perform_turn(direction)
        end
      end
    elseif SHOT_KEYS[key] and not campaign then
      if not is_repeat then
        app:perform_turn("shoot_" .. SHOT_KEYS[key])
      end
    elseif key == "i" then
      app:open_inventory()
    elseif key == "g" and not campaign then
      app:open_salvage()
    elseif key == "u" then
      app:perform_turn("interact")
    elseif key == "e" and campaign then
      app:perform_turn("attack")
    elseif key == "q" or key == "e" or key == "b" or key == "f" then
      app:perform_turn(key)
    elseif key == "x" then
      app:open_body_abilities()
    elseif key == "c" then
      app:open_build()
    end
  end
end

function Input.keyreleased(app, key)
  if MOVE_KEYS[key] then
    local direction = app:set_movement_key(key, false)
    if direction then
      app:start_held_move(direction)
    else
      app.held_direction, app.hold_timer = nil, nil
    end
  end
end

return Input
