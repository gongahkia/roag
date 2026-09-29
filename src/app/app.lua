local Content = require("src.content.legacy")
local Input = require("src.app.input")
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local SoundBank = require("src.audio.sound_bank")
local Assets = require("src.rendering.assets")
local Presentation = require("src.rendering.presentation")
local Renderer = require("src.rendering.renderer")

local App = {}
App.__index = App

local HOLD_INITIAL_DELAY, HOLD_REPEAT_DELAY = 0.28, 0.11

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function clock_seed()
  return math.floor((os.time() * 1000) % 2147483646) + 1
end

function App.new(options)
  options = options or {}
  local self = setmetatable({}, App)
  self.content = options.content or Content
  self.seed_stream = Rng.new(options.seed or clock_seed())
  self.screen, self.menu = "title", 1
  self.assets = Assets.new()
  self.sounds = SoundBank.new()
  self.presentation = Presentation.new()
  self.renderer = Renderer.new(self.assets)
  self.sprite_lab = { slot = 1, x = 25, y = 1 }
  return self
end

function App:load()
  self.assets:load()
  self.sounds:load()
end

function App:focus(focused)
  if focused then
    self.assets:refresh_sprite_mappings()
  end
end

function App:quit()
  love.event.quit()
end

function App:play_sound(name)
  self.sounds:play(name)
end

function App:_handle_session_event(event)
  if event.type == "sound" then
    self:play_sound(event.value)
  elseif event.type == "hit" then
    self.presentation:hit()
  end
end

function App:_new_session()
  local seed = self.seed_stream:next()
  self.session = Session.new({
    seed = seed,
    content = self.content,
    emit = function(event)
      self:_handle_session_event(event)
    end,
  })
end

function App:move_menu(amount, limit)
  self.menu = clamp(self.menu + amount, 1, limit)
  self:play_sound("select")
end

function App:select_class(class)
  self.selected_class = class
  self:_new_session()
  self.boon_options = self.session:choose_boons(3)
  self.screen, self.menu = "boon", 1
end

function App:select_boon(boon)
  self.session:start_run(self.selected_class, boon)
  self.screen = "game"
  self.held_direction, self.hold_timer = nil, nil
  self.presentation:reset(self.session)
end

function App:select_curse(curse)
  self.session:choose_curse(curse)
  self.screen = "game"
  self.held_direction, self.hold_timer = nil, nil
  self.presentation:reset(self.session)
end

function App:buy_selected()
  self.session:buy(self.content.shop[self.menu])
end

function App:sell_selected()
  self.session:sell(self.content.shop[self.menu])
end

function App:start_boss()
  self.session:start_boss()
  self.screen = "game"
  self.held_direction, self.hold_timer = nil, nil
  self.presentation:reset(self.session)
end

function App:return_to_title()
  self.screen, self.menu = "title", 1
  self.session = nil
  self.held_direction, self.hold_timer = nil, nil
end

function App:_handle_turn_result(result)
  if result == "curse" then
    self.screen, self.menu = "curse", 1
  elseif result == "shop" then
    self.screen, self.menu = "shop", 1
  elseif result == "gameover" or result == "victory" then
    self.screen, self.menu = result, 1
    self.held_direction, self.hold_timer = nil, nil
  end
end

function App:perform_turn(input)
  if self.screen ~= "game" then
    return
  end
  self:_handle_turn_result(self.session:turn(input))
end

function App:close_overlay()
  self.screen = "game"
  self.menu = 1
  self.inventory_selected_id = nil
  self.salvage_corpse_id = nil
end

function App:open_inventory()
  if not self.session or not self.session.state.inventory then
    return false
  end
  self.inventory_cursor = self.inventory_cursor or { x = 1, y = 1 }
  self.inventory_selected_id = nil
  self.screen = "inventory"
  self:play_sound("select")
  return true
end

function App:move_inventory_cursor(delta_x, delta_y)
  local inventory = self.session.state.inventory
  self.inventory_cursor = self.inventory_cursor or { x = 1, y = 1 }
  self.inventory_cursor.x = clamp(self.inventory_cursor.x + delta_x, 1, inventory.width)
  self.inventory_cursor.y = clamp(self.inventory_cursor.y + delta_y, 1, inventory.height)
  self:play_sound("select")
end

function App:inventory_select_or_place()
  local inventory = self.session.state.inventory
  local cursor = self.inventory_cursor
  if self.inventory_selected_id then
    local entry = inventory:get(self.inventory_selected_id)
    local moved, reason = inventory:move(self.inventory_selected_id, cursor.x, cursor.y, entry and entry.rotated)
    if moved then
      self.session:_log("Repacked " .. moved.item.display_name .. ".")
      self.inventory_selected_id = nil
      self:play_sound("pickup")
    else
      self.session:_log(reason)
      self:play_sound("select")
    end
    return moved
  end
  local entry = inventory:item_at(cursor.x, cursor.y)
  if entry then
    self.inventory_selected_id = entry.physical_id
    self.session:_log("Selected " .. entry.item.display_name .. ".")
    self:play_sound("select")
    return entry
  end
  self.session:_log("No item at cursor.")
  return nil
end

function App:rotate_inventory_item()
  local inventory = self.session.state.inventory
  local entry = self.inventory_selected_id and inventory:get(self.inventory_selected_id)
    or inventory:item_at(self.inventory_cursor.x, self.inventory_cursor.y)
  if not entry then
    self.session:_log("No item selected.")
    return nil
  end
  local rotated, reason = inventory:rotate(entry.physical_id)
  if rotated then
    self.session:_log("Rotated " .. entry.item.display_name .. ".")
    self:play_sound("select")
  else
    self.session:_log(reason)
  end
  return rotated
end

function App:open_salvage()
  local corpse = self.session and self.session:nearby_corpse()
  if not corpse then
    if self.session then
      self.session:_log("No corpse within salvage range.")
    end
    return false
  end
  self.salvage_corpse_id = corpse.id
  self.screen, self.menu = "salvage", 1
  self:play_sound("select")
  return true
end

function App:salvage_options()
  local corpse = self.session and self.session:find_corpse(self.salvage_corpse_id)
  return corpse and corpse:list_components() or {}
end

function App:salvage_selected()
  local options = self:salvage_options()
  local selection = options[self.menu]
  if not selection then
    self.session:_log("Corpse has no salvageable components.")
    return nil
  end
  local result = self.session:salvage_corpse_component(self.salvage_corpse_id, selection.slot_id)
  local remaining = self:salvage_options()
  self.menu = clamp(self.menu, 1, math.max(1, #remaining))
  return result
end

function App:start_held_move(direction)
  self.held_direction, self.hold_timer = direction, HOLD_INITIAL_DELAY
end

function App:update(dt)
  if self.screen == "game" and self.session then
    if self.held_direction then
      self.hold_timer = (self.hold_timer or HOLD_INITIAL_DELAY) - dt
      if self.hold_timer <= 0 then
        self.hold_timer = HOLD_REPEAT_DELAY
        local player = self.session.state.player
        if player.direction == self.held_direction and self.session:can_move(self.held_direction) then
          self:perform_turn(self.held_direction)
        end
      end
    end
    self.presentation:update(self.session, dt)
  end
end

function App:draw()
  self.renderer:draw(self)
end

function App:_sprite_lab_item()
  local items = {
    { kind = "player", label = "PLAYER" },
    { kind = "target", label = "TARGET" },
    { kind = "ammo", label = "AMMO" },
    { kind = "torch", label = "TORCH" },
    { kind = "door", label = "EXIT DOOR" },
    { kind = "bullet", label = "BULLET" },
    { kind = "bomb", label = "BOMB" },
    { kind = "flare", label = "FLARE" },
    { kind = "wolf", label = "WOLF" },
    { kind = "bomber", label = "BOMBER" },
    { kind = "necromancer", label = "NECROMANCER" },
    { kind = "cultist", label = "CULTIST" },
    { kind = "boss", label = "BOSS" },
  }
  return items[self.sprite_lab.slot]
end

function App:open_sprite_lab()
  self.screen = "sprite_lab"
  local tile = self.assets.sprites[self:_sprite_lab_item().kind]
  self.sprite_lab.x, self.sprite_lab.y = tile[1], tile[2]
end

function App:_select_sprite_slot(delta)
  self.sprite_lab.slot = clamp(self.sprite_lab.slot + delta, 1, 13)
  local tile = self.assets.sprites[self:_sprite_lab_item().kind]
  self.sprite_lab.x, self.sprite_lab.y = tile[1], tile[2]
  self:play_sound("select")
end

function App:handle_sprite_lab_key(key)
  if key == "escape" or key == "p" then
    self.screen = "title"
    return
  end
  if key == "q" then
    self:_select_sprite_slot(-1)
  elseif key == "e" then
    self:_select_sprite_slot(1)
  elseif key == "a" or key == "left" then
    self.sprite_lab.x = clamp(self.sprite_lab.x - 1, 1, 49)
  elseif key == "d" or key == "right" then
    self.sprite_lab.x = clamp(self.sprite_lab.x + 1, 1, 49)
  elseif key == "w" or key == "up" then
    self.sprite_lab.y = clamp(self.sprite_lab.y - 1, 1, 22)
  elseif key == "s" or key == "down" then
    self.sprite_lab.y = clamp(self.sprite_lab.y + 1, 1, 22)
  elseif key == "return" or key == "space" then
    self.assets.sprites[self:_sprite_lab_item().kind] = { self.sprite_lab.x, self.sprite_lab.y }
    self:play_sound("select")
  elseif key == "r" then
    self.assets:reset_sprite(self:_sprite_lab_item().kind)
    local tile = self.assets.sprites[self:_sprite_lab_item().kind]
    self.sprite_lab.x, self.sprite_lab.y = tile[1], tile[2]
    self:play_sound("select")
  elseif key == "x" then
    for kind in pairs(self.assets.sprites) do
      self.assets:reset_sprite(kind)
    end
    local tile = self.assets.sprites[self:_sprite_lab_item().kind]
    self.sprite_lab.x, self.sprite_lab.y = tile[1], tile[2]
    self:play_sound("select")
  end
end

function App:keypressed(...)
  Input.keypressed(self, ...)
end

function App:keyreleased(...)
  Input.keyreleased(self, ...)
end

return App
