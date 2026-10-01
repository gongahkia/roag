-- LÖVE event translation only. Game rules remain in Session.
local Input = {}

local MOVE_KEYS = { w = true, a = true, s = true, d = true }
local SHOT_KEYS = { up = "w", left = "a", down = "s", right = "d" }

function Input.keypressed(app, key, _, is_repeat)
  if app.screen == "sprite_lab" then
    app:handle_sprite_lab_key(key)
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

  if key == "escape" then
    app:quit()
    return
  end
  if app.screen == "title" then
    if key == "p" then
      app:open_sprite_lab()
    elseif key == "w" or key == "up" then
      app:move_menu(-1, #app:title_options())
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app:title_options())
    elseif key == "return" or key == "space" then
      app:activate_title_choice()
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
  if app.screen == "class" then
    if key == "w" or key == "up" then
      app:move_menu(-1, #app.content.classes)
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app.content.classes)
    elseif key == "return" or key == "e" then
      app:select_class(app.content.classes[app.menu])
    end
    return
  end
  if app.screen == "boon" then
    if key == "w" or key == "up" then
      app:move_menu(-1, #app.boon_options)
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app.boon_options)
    elseif key == "return" or key == "e" then
      app:select_boon(app.boon_options[app.menu])
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
  if app.screen == "shop" then
    if key == "w" or key == "up" then
      app:move_menu(-1, #app.content.shop)
    elseif key == "s" or key == "down" then
      app:move_menu(1, #app.content.shop)
    elseif key == "b" then
      app:buy_selected()
    elseif key == "v" then
      app:sell_selected()
    elseif key == "return" or key == "e" then
      app:start_boss()
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
    if MOVE_KEYS[key] then
      if not is_repeat then
        local direction = app:set_movement_key(key, true)
        if direction then
          app:start_held_move(direction)
          app:perform_turn(direction)
        end
      end
    elseif SHOT_KEYS[key] then
      if not is_repeat then
        app:perform_turn("shoot_" .. SHOT_KEYS[key])
      end
    elseif key == "i" then
      app:open_inventory()
    elseif key == "g" then
      app:open_salvage()
    elseif key == "u" then
      app:perform_turn("interact")
    elseif key == "q" or key == "e" or key == "b" or key == "f" then
      app:perform_turn(key)
    elseif key == "x" then
      app:open_body_abilities()
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
