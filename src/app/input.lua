-- LÖVE event translation only. Game rules remain in Session.
local Input = {}

local MOVE_KEYS = { w = "w", a = "a", s = "s", d = "d" }
local SHOT_KEYS = { up = "w", left = "a", down = "s", right = "d" }

function Input.keypressed(app, key, _, is_repeat)
  if app.screen == "sprite_lab" then
    app:handle_sprite_lab_key(key)
    return
  end

  if key == "escape" then
    app:quit()
    return
  end
  if app.screen == "title" then
    if key == "p" then
      app:open_sprite_lab()
    elseif key == "return" or key == "space" then
      app.screen, app.menu = "class", 1
      app:play_sound("select")
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
    local direction = MOVE_KEYS[key]
    if direction then
      if not is_repeat then
        app:start_held_move(direction)
        app:perform_turn(direction)
      end
    elseif SHOT_KEYS[key] then
      if not is_repeat then
        app:perform_turn("shoot_" .. SHOT_KEYS[key])
      end
    elseif key == "q" or key == "e" or key == "b" or key == "f" then
      app:perform_turn(key)
    end
  end
end

function Input.keyreleased(app, key)
  local direction = MOVE_KEYS[key]
  if direction and app.held_direction == direction then
    app.held_direction, app.hold_timer = nil, nil
  end
end

return Input
