local Content = require("src.content.legacy")
local Input = require("src.app.input")
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local SoundBank = require("src.audio.sound_bank")
local Assets = require("src.rendering.assets")
local Presentation = require("src.rendering.presentation")
local Renderer = require("src.rendering.renderer")
local ActiveRun = require("src.persistence.active_run")
local SaveStore = require("src.persistence.save_store")

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
  self.save_store = options.save_store or SaveStore.runtime()
  self.seed_stream = Rng.new(options.seed or clock_seed())
  self.screen, self.menu = "title", 1
  self.assets = Assets.new()
  self.sounds = SoundBank.new()
  self.presentation = Presentation.new()
  self.renderer = Renderer.new(self.assets)
  self.sprite_lab = { slot = 1, x = 25, y = 1 }
  self.movement_keys = {}
  self:refresh_continue()
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
  self:autosave("quit")
  if love and love.event then love.event.quit() end
end

function App:refresh_continue()
  local available, error_data = ActiveRun.has_valid_save(self.save_store, { content = self.content })
  self.continue_available = available == true
  self.title_error = self.continue_available and nil or (self.save_store:exists() and error_data or nil)
  return self.continue_available
end

function App:title_options()
  local options = { { name = "NEW RUN", description = "Begin a new descent." } }
  if self.continue_available then
    options[#options + 1] = { name = "CONTINUE", description = "Resume the current active run." }
  end
  return options
end

function App:autosave(_boundary)
  if not self.session then return true end
  if self.session.state.ended == "gameover" or self.session.state.ended == "victory" then
    local retired, error_data = ActiveRun.retire(self.save_store)
    if not retired then self.save_error = error_data end
    self:refresh_continue()
    return retired, error_data
  end
  local saved, error_data = ActiveRun.save(self.session, self.save_store)
  if not saved then
    self.save_error = error_data
    self.session:_log("Autosave failed: " .. tostring(error_data and error_data.reason or "unknown error"))
  else
    self.save_error = nil
    self.continue_available = true
  end
  return saved, error_data
end

function App:request_new_run()
  if self.continue_available then
    self.screen, self.menu = "replace_save", 1
    return false
  end
  self:_new_session()
  self.session:start_run()
  self.screen, self.menu = "game", 1
  self:clear_held_movement()
  self.presentation:reset(self.session)
  self:autosave("new_run")
  self:play_sound("select")
  return true
end

function App:confirm_replace_save()
  self:_new_session()
  self.session:start_run()
  self.screen, self.menu = "game", 1
  self:clear_held_movement()
  self.presentation:reset(self.session)
  self:autosave("new_run")
  self:play_sound("select")
end

function App:continue_run()
  local session, error_data = ActiveRun.load(self.save_store, {
    content = self.content,
    emit = function(event) self:_handle_session_event(event) end,
  })
  if not session or session.state.ended then
    self.title_error = error_data or { code = "invalid_state", reason = "Active run is already complete" }
    self.continue_available = false
    return nil, self.title_error
  end
  self.session = session
  if session.state.phase == "reconstruction" then
    self.screen = "reconstruction"
    self.reconstruction_focus, self.reconstruction_slot_index, self.reconstruction_inventory_index = "body", 1, 1
  elseif session.state.phase == "transition" then
    self.screen = session.state.transition_next == "shop" and "service_hub" or "curse"
  elseif session.state.phase == "route" then
    self.screen = "route"
  elseif session.state.phase == "service" then
    self.screen = "service"
  else
    self.screen = "game"
  end
  self.menu = 1
  self:clear_held_movement()
  self.presentation:reset(session)
  self.title_error = nil
  return session
end

function App:activate_title_choice()
  local selected = self:title_options()[self.menu]
  if selected and selected.name == "CONTINUE" then return self:continue_run() end
  return self:request_new_run()
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
  -- Compatibility helper for old save/UI tests. New-run input never routes to
  -- class or free-boon screens; callers may still explicitly construct an
  -- old-style run for migration coverage.
  self.selected_class = class
  self:_new_session()
  self.boon_options = self.session:choose_boons(3)
  self.screen, self.menu = "title", 1
end

function App:select_boon(boon)
  self.session:start_run(self.selected_class, boon)
  self.screen = "game"
  self:clear_held_movement()
  self.presentation:reset(self.session)
  self:autosave("new_run")
end

function App:select_curse(curse)
  local result = self.session:choose_curse(curse)
  self.screen = result.next == "route" and "route" or "game"
  self:clear_held_movement()
  if self.screen == "game" then self.presentation:reset(self.session) end
  self:autosave("curse")
  return result
end

function App:route_options()
  if not self.session then return {} end
  local result = {}
  for _, node in ipairs(self.session:available_route_nodes()) do
    local biome = node.biome_id and self.session.route_definitions:get_biome(node.biome_id)
    local tier = node.tier_id and self.session.route_definitions:get_tier(node.tier_id)
    result[#result + 1] = {
      node_id = node.id,
      name = biome and biome.display_name or string.upper(node.type),
      description = biome and ("TIER " .. tier.number .. "  •  FLOOR") or string.upper(node.type),
      service_id = node.service_id,
      service_name = node.service_id and self.session.registry:get_service(node.service_id).display_name or nil,
      biome_id = node.biome_id,
      tier_id = node.tier_id,
      type = node.type,
    }
  end
  return result
end

function App:select_route_choice()
  local option = self:route_options()[self.menu]
  if not option then return { applied = false, code = "no_choice", reason = "No route choice is selected" } end
  local result = self.session:select_route_node(option.node_id)
  if result.applied then
    self.screen, self.menu = "game", 1
    self:clear_held_movement()
    self.presentation:reset(self.session)
    self:play_sound("door")
    self:autosave("route_selection")
  else
    self.session:_log(result.reason)
  end
  return result
end

function App:service_options()
  return self.session and self.session:service_options(self.service_object_id) or {}
end

function App:open_service(object_id)
  self.service_object_id = object_id or self.session.state.active_service_object_id
  local opened = self.session:open_service(self.service_object_id)
  if opened.applied then self.screen, self.menu = "service", 1 end
  return opened
end

function App:service_execute_selected()
  local option = self:service_options()[self.menu]
  if not option then return { applied = false, code = "invalid_item", reason = "No service option selected" } end
  local result = self.session:service_execute(option, self.service_object_id)
  if result.applied then self:autosave("service") end
  return result
end

function App:close_service()
  local result = self.session:close_service()
  if result.applied then self.screen, self.menu, self.service_object_id = result.return_to_hub and "service_hub" or "game", 1, nil end
  return result
end

function App:service_hub_options()
  local ids = { "service.supply.legacy", "service.repair.legacy", "service.salvager.legacy", "service.charm_vendor.legacy" }
  local options = {}
  for _, id in ipairs(ids) do options[#options + 1] = { service_id = id, name = self.session.registry:get_service(id).display_name } end
  options[#options + 1] = { action = "boss", name = "ENTER FINAL BOSS" }
  return options
end

function App:select_service_hub_option()
  local option = self:service_hub_options()[self.menu]
  if option.action == "boss" then return self:start_boss() end
  -- Final hub stock is stored in the session rather than a rendered pseudo-shop.
  local hub = self.session.state.final_service_hub
  if not hub then return { applied = false, code = "invalid_service", reason = "Final service hub is unavailable" } end
  self.service_object_id = "hub:" .. option.service_id
  return self:open_service(self.service_object_id)
end

function App:start_boss()
  self.session:start_boss()
  self.screen = "game"
  self:clear_held_movement()
  self.presentation:reset(self.session)
  self:autosave("boss_transition")
end

function App:return_to_title()
  self.screen, self.menu = "title", 1
  self.session = nil
  self:clear_held_movement()
end

function App:_handle_turn_result(result)
  if result == "reconstruction" then
    self:open_reconstruction()
  elseif result == "curse" then
    self.screen, self.menu = "curse", 1
  elseif result == "shop" then
    self.screen, self.menu = "service_hub", 1
  elseif result == "route" then
    self.screen, self.menu = "route", 1
  elseif result == "gameover" or result == "victory" then
    self.screen, self.menu = result, 1
    self:clear_held_movement()
  end
  if result == "service" then
    self.service_object_id = self.session.state.active_service_object_id
    self.screen, self.menu = "service", 1
  end
end

function App:perform_turn(input)
  if self.screen ~= "game" then
    return
  end
  local result = self.session:turn(input)
  self:_handle_turn_result(result)
  self:autosave("turn")
  return result
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

function App:open_reconstruction()
  if not self.session or self.session.state.phase ~= "reconstruction" then
    return false
  end
  self.reconstruction_focus = "body"
  self.reconstruction_slot_index = 1
  self.reconstruction_inventory_index = 1
  self.reconstruction_selected_id = nil
  self.screen, self.menu = "reconstruction", 1
  self:play_sound("select")
  return true
end

function App:reconstruction_slots()
  local body = self.session and self.session.state.player and self.session.state.player.body
  if not body then
    return {}
  end
  local slots = {}
  for _, slot_id in ipairs(body.slot_order) do
    slots[#slots + 1] = body:get_slot(slot_id)
  end
  return slots
end

function App:reconstruction_slot()
  return self:reconstruction_slots()[self.reconstruction_slot_index or 1]
end

function App:reconstruction_inventory_entry()
  local inventory = self.session and self.session.state.inventory
  return inventory and inventory.entries[self.reconstruction_inventory_index or 1] or nil
end

function App:move_reconstruction_selection(amount)
  if self.reconstruction_focus == "body" then
    local slots = self:reconstruction_slots()
    self.reconstruction_slot_index = clamp((self.reconstruction_slot_index or 1) + amount, 1, math.max(1, #slots))
  else
    local entries = self.session.state.inventory.entries
    self.reconstruction_inventory_index = clamp((self.reconstruction_inventory_index or 1) + amount, 1, math.max(1, #entries))
  end
  self:play_sound("select")
end

function App:toggle_reconstruction_focus()
  self.reconstruction_focus = self.reconstruction_focus == "body" and "inventory" or "body"
  self:play_sound("select")
end

function App:reconstruction_feedback()
  local slot = self:reconstruction_slot()
  local component_id = self.reconstruction_selected_id
  if not slot then
    return { applied = false, compatible = false, reason = "No body slot selected" }
  end
  if not component_id then
    if slot.component then
      local item = require("src.inventory.physical_item").from_component(slot.component, self.session.registry)
      local placement = self.session.state.inventory:find_first_fit(item)
      return {
        applied = placement ~= nil,
        compatible = placement ~= nil,
        reason = placement and "Can uninstall to inventory" or "No inventory room for outgoing component",
      }
    end
    return { applied = false, compatible = false, reason = "Select a component from inventory" }
  end
  return self.session:reconstruction_compatibility(component_id, slot.id)
end

function App:reconstruction_confirm()
  local slot = self:reconstruction_slot()
  if not slot then
    return nil
  end
  if self.reconstruction_focus == "inventory" then
    local entry = self:reconstruction_inventory_entry()
    if not entry then
      self.session:_log("No inventory item selected.")
      return nil
    end
    self.reconstruction_selected_id = entry.physical_id
    self.reconstruction_focus = "body"
    self.session:_log("Selected " .. entry.item.display_name .. " for installation.")
    self:play_sound("select")
    return entry
  end
  if self.reconstruction_selected_id then
    local result = self.session:install_inventory_component(self.reconstruction_selected_id, slot.id)
    if result.applied then
      self.reconstruction_selected_id = nil
      self:autosave("reconstruction")
    end
    return result
  end
  local result = self.session:uninstall_body_component(slot.id)
  if result.applied then self:autosave("reconstruction") end
  return result
end

function App:rotate_reconstruction_item()
  if self.reconstruction_focus ~= "inventory" then
    self.session:_log("Select an inventory item to rotate it.")
    return nil
  end
  local entry = self:reconstruction_inventory_entry()
  if not entry then
    self.session:_log("No inventory item selected.")
    return nil
  end
  local rotated, reason = self.session.state.inventory:rotate(entry.physical_id)
  if rotated then
    self.session:_log("Rotated " .. entry.item.display_name .. ".")
    self:play_sound("select")
    self:autosave("reconstruction")
  else
    self.session:_log(reason)
  end
  return rotated
end

function App:finish_reconstruction()
  local result = self.session:complete_reconstruction()
  if result.applied then
    self.reconstruction_selected_id = nil
    self:_handle_turn_result(result.next)
    self:play_sound("door")
    self:autosave("reconstruction_complete")
  else
    self.session:_log(result.reason)
  end
  return result
end

function App:open_body_abilities()
  if not self.session then
    return false
  end
  local abilities = self.session:available_actor_abilities(self.session.state.player, "body")
  if #abilities == 0 then
    self.session:_log("No functional body abilities installed.")
    return false
  end
  self.body_ability_options = abilities
  self.body_ability_confirming = false
  self.screen, self.menu = "body_abilities", 1
  self:play_sound("select")
  return true
end

function App:confirm_body_ability()
  local ability_id = self.body_ability_options and self.body_ability_options[self.menu]
  if not ability_id then
    return nil
  end
  if not self.body_ability_confirming then
    self.body_ability_confirming = true
    self:play_sound("select")
    return { applied = false, confirmation_required = true, ability_id = ability_id }
  end
  self.body_ability_confirming = false
  self.screen = "game"
  self:perform_turn("activate_ability:" .. ability_id)
  return { applied = true, ability_id = ability_id }
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
      self:autosave("inventory")
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
    self:autosave("inventory")
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
  if result.applied then self:autosave("salvage") end
  return result
end

function App:start_held_move(direction)
  self.held_direction, self.hold_timer = direction, HOLD_INITIAL_DELAY
end

function App:clear_held_movement()
  self.held_direction, self.hold_timer = nil, nil
  self.movement_keys = {}
end

function App:set_movement_key(key, held)
  self.movement_keys = self.movement_keys or {}
  self.movement_keys[key] = held or nil
  local keys = self.movement_keys
  local vertical = keys.w and "w" or keys.s and "s" or nil
  local horizontal = keys.a and "a" or keys.d and "d" or nil
  if vertical and horizontal then
    return ({ wa = "nw", wd = "ne", sa = "sw", sd = "se" })[vertical .. horizontal]
  end
  return vertical or horizontal
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
