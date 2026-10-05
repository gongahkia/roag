-- LÖVE event translation only. Game rules remain in Session.
local Input = {}

local MOVE_KEYS = { w = true, a = true, s = true, d = true }
local SHOT_KEYS = { up = "w", left = "a", down = "s", right = "d" }

local function campaign_field(app)
  return app.is_campaign_mode and app:is_campaign_mode()
end

local function expedition_field(app)
  return app.is_expedition_mode and app:is_expedition_mode()
end

function Input.keypressed(app, key, _, is_repeat)
  if key == "f3" and not is_repeat and app.screen == "game" then
    app.debug_overlay = not app.debug_overlay
    return
  end
  if app.screen == "expedition_character_select" then
    local options = app:expedition_character_options_list()
    if key == "escape" then
      app.screen, app.menu = "title", 1
    elseif key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #options))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #options))
    elseif key == "return" or key == "space" or key == "e" then
      app:select_expedition_character()
    end
    return
  end

  if app.screen == "expedition_reward" then
    local choices = app.expedition and app.expedition.pending_reward or {}
    if key == "w" or key == "up" then
      app:move_menu(-1, math.max(1, #choices))
    elseif key == "s" or key == "down" then
      app:move_menu(1, math.max(1, #choices))
    elseif key == "return" or key == "space" or key == "e" then
      app:choose_expedition_reward()
    end
    return
  end

  if app.screen == "expedition_chest" then
    if key == "return" or key == "space" or key == "e" then
      app:open_expedition_chest()
    elseif key == "escape" or key == "x" then
      app:skip_expedition_chest()
    end
    return
  end

  if app.screen == "expedition_build" then
    if key == "escape" or key == "i" or key == "return" then app:close_overlay() end
    return
  end

  if app.screen == "expedition_summary" then
    if key == "return" or key == "space" or key == "escape" or key == "e" then app:return_to_title() end
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
    elseif key == "tab" then
      app:toggle_inventory_loadout_panel()
    elseif app.inventory_panel == "loadout" then
      if key == "w" or key == "up" then
        app:move_loadout_selection(-1)
      elseif key == "s" or key == "down" then
        app:move_loadout_selection(1)
      elseif key == "a" or key == "left" then
        app:move_loadout_focus(-1)
      elseif key == "d" or key == "right" then
        app:move_loadout_focus(1)
      elseif key == "return" or key == "space" then
        app:assign_selected_loadout()
      end
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
    elseif key == "delete" or key == "backspace" then
      app:inventory_drop_selected()
    end
    return
  end

  if app.screen == "salvage" then
    if campaign_field(app) then
      if key == "escape" or key == "u" then
        app:close_overlay()
      elseif key == "tab" then
        app:toggle_salvage_focus()
      elseif key == "w" or key == "up" then
        app:move_salvage_cursor(0, -1)
      elseif key == "s" or key == "down" then
        app:move_salvage_cursor(0, 1)
      elseif key == "a" or key == "left" then
        app:move_salvage_cursor(-1, 0)
      elseif key == "d" or key == "right" then
        app:move_salvage_cursor(1, 0)
      elseif key == "return" or key == "space" or key == "e" then
        app:salvage_select_or_place()
      elseif key == "r" then
        app:rotate_salvage_item()
      end
      return
    end
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
    if key == "escape" then
      -- A live Campaign station may be left without changing the world. The
      -- legacy route reconstruction phase has a real continuation behind F,
      -- so Escape must never silently advance that route.
      if campaign_field(app) and app.session and app.session.state.active_reconstruction_station_id then
        app:finish_reconstruction()
      else
        app.session:_log("FINISH RECONSTRUCTION WITH F.")
      end
    elseif key == "tab" then
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

  -- Death is a Campaign handoff, not an unexplained zone transition. This
  -- acknowledgement has no simulation cost, and Escape must not quit from it.
  if app.screen == "campaign_succession" then
    if key == "escape" or key == "return" or key == "space" or key == "e" then
      app:continue_campaign_succession()
    end
    return
  end

  -- Build stance is a live Campaign field mode, not one of the paused menu
  -- screens above.  Escape must leave it before the ordinary game-level
  -- Escape handler considers quitting the application.
  if app.screen == "game" and campaign_field(app) and app:is_build_stance() and key == "escape" then
    app:exit_build_stance()
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
  -- A successful hit has already resolved in Session. This brief guard only
  -- prevents another field action from being dispatched before its visual
  -- confirmation is visible; menu/modal input above remains unaffected.
  if app.screen == "game" and app.is_gameplay_input_blocked and app:is_gameplay_input_blocked() then
    return
  end
  if app.screen == "game" then
    local campaign = campaign_field(app)
    local expedition = expedition_field(app)
    if campaign and app:is_build_stance() then
      if MOVE_KEYS[key] then
        if not is_repeat then
          local direction = app:set_movement_key(key, true)
          if direction then
            app:start_held_move(direction)
            app:perform_turn(direction)
          end
        end
      elseif key == "e" and not is_repeat then
        app:place_active_build()
      elseif key == "r" and not is_repeat then
        app:cycle_build_recipe(1)
      elseif key == "x" and not is_repeat then
        app:cycle_build_recipe(-1)
      elseif key == "c" then
        app:exit_build_stance()
      elseif key == "i" then
        app:open_inventory()
      elseif key == "u" then
        -- USE remains available in build stance: construction is a field
        -- posture, not an interaction lockout.
        app:perform_turn("interact")
      elseif key == "q" and not is_repeat then
        -- Retaining the active ability is useful for emergency movement or
        -- control without making a second build-only verb.
        app:activate_campaign_ability()
      elseif key == "b" or key == "f" then
        app:perform_turn(key)
      end
      return
    end
    if MOVE_KEYS[key] then
      if not is_repeat then
        local direction = app:set_movement_key(key, true)
        if direction then
          app:start_held_move(direction)
          app:perform_turn(direction)
        end
      end
    elseif SHOT_KEYS[key] and not campaign and not expedition then
      if not is_repeat then
        app:perform_turn("shoot_" .. SHOT_KEYS[key])
      end
    elseif key == "i" then
      app:open_inventory()
    elseif key == "g" and not campaign and not expedition then
      app:open_salvage()
    elseif key == "u" then
      app:perform_turn("interact")
    elseif key == "e" and (campaign or expedition) then
      app:perform_turn("attack")
    elseif key == "r" and campaign and not is_repeat then
      app:perform_turn("swap_weapon")
    elseif key == "q" and campaign and not is_repeat then
      app:activate_campaign_ability()
    elseif key == "q" and expedition and not is_repeat then
      app:perform_turn("q")
    elseif key == "r" and expedition and not is_repeat then
      app:perform_turn("swap_weapon")
    elseif key == "x" and campaign and not is_repeat then
      app:perform_turn("swap_ability")
    elseif key == "x" and expedition and not is_repeat then
      app:perform_turn("swap_ability")
    elseif key == "q" or key == "e" or key == "b" or key == "f" then
      app:perform_turn(key)
    elseif key == "x" and not campaign and not expedition then
      app:open_body_abilities()
    elseif key == "c" and not expedition then
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
