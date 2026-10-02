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
local MetaProfile = require("src.persistence.meta_profile")
local FallenArchive = require("src.persistence.fallen_archive")
local FallenRecurrence = require("src.simulation.fallen_recurrence")
local Registry = require("src.content.registry")
local ArtPackSettings = require("src.persistence.art_pack_settings")
local ScreenManager = require("src.ui.screen_manager")
local CursorManager = require("src.ui.cursor_manager")

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
  self.registry = options.registry or Registry.load()
  self.save_store = options.save_store or SaveStore.runtime()
  self.meta_store = options.meta_store or SaveStore.runtime("meta_profile.json")
  self.archive_store = options.archive_store or SaveStore.runtime("fallen_characters.json")
  self.art_pack_store = options.art_pack_store or SaveStore.runtime("art_pack_settings.json")
  self.meta_profile, self.meta_status = MetaProfile.load(self.meta_store, self.registry)
  self.meta_error = nil
  if not self.meta_profile then self.meta_error = self.meta_status end
  if not self.meta_profile then self.meta_profile = MetaProfile.new() end
  self.fallen_archive, self.archive_status = FallenArchive.load(self.archive_store)
  self.archive_error = nil
  if not self.fallen_archive then self.archive_error = self.archive_status end
  if not self.fallen_archive then self.fallen_archive = FallenArchive.new() end
  self.art_pack_settings, self.art_pack_status = ArtPackSettings.load(self.art_pack_store)
  self.art_pack_error = nil
  if not self.art_pack_settings then self.art_pack_error = self.art_pack_status end
  if not self.art_pack_settings then self.art_pack_settings = ArtPackSettings.new() end
  self.screens, self.screen_definition_error = ScreenManager.load()
  if not self.screens then self.screens = ScreenManager.fallback() end
  self.seed_stream = Rng.new(options.seed or clock_seed())
  self.screen, self.menu = "title", 1
  self.assets = Assets.new({ art_pack_id = self.art_pack_settings.art_pack_id })
  self.sounds = SoundBank.new()
  self.presentation = Presentation.new()
  self.renderer = Renderer.new(self.assets)
  self.cursors = CursorManager.new()
  self.movement_keys = {}
  self:_reconcile_pending_death()
  self:refresh_continue()
  return self
end

function App:load()
  self.assets:load()
  self.sounds:load()
  self.cursors:load()
  self.cursors:set("default")
end

function App:focus(focused)
  if focused then
    self.assets:refresh_sprite_mappings()
    -- Screen copy/layout data is development-authored presentation only. A
    -- refocus picks up a saved Studio edit without altering any run state.
    local screens, failure = ScreenManager.load()
    if screens then
      self.screens, self.screen_definition_error = screens, nil
    else
      self.screen_definition_error = failure
    end
  end
end

function App:quit()
  self:autosave("quit")
  if love and love.event then love.event.quit() end
end

function App:refresh_continue()
  if self.death_archive_error then
    self.continue_available = false
    self.title_error = self.death_archive_error
    return false
  end
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
  options[#options + 1] = { name = "RESEARCH", description = "Spend persistent RESEARCH DATA on future runs." }
  options[#options + 1] = { name = "FALLEN", description = "Inspect bodies lost on earlier descents." }
  options[#options + 1] = { name = "ART PACKS", description = "Choose the visual tile pack. This never changes gameplay." }
  return options
end

function App:art_pack_options()
  local result = {}
  for _, definition in ipairs(self.assets:available_art_packs()) do
    result[#result + 1] = {
      id = definition.id,
      name = definition.display_name,
      description = definition.description,
      license = definition.license,
      credit = definition.credit,
      source_url = definition.source_url,
      selected = definition.id == self.art_pack_settings.art_pack_id,
    }
  end
  return result
end

function App:open_art_packs()
  self.screen, self.menu = "art_packs", 1
  for index, option in ipairs(self:art_pack_options()) do
    if option.selected then self.menu = index; break end
  end
  return true
end

function App:select_art_pack(id)
  local selected_id = id or (self:art_pack_options()[self.menu] or {}).id
  if not selected_id then return nil, { code = "unknown_art_pack", reason = "No art pack is selected" } end
  local applied, apply_error = self.assets:select_art_pack(selected_id)
  if not applied then return nil, apply_error end
  local candidate = ArtPackSettings.copy(self.art_pack_settings)
  candidate.art_pack_id = selected_id
  local saved, save_error = ArtPackSettings.save(candidate, self.art_pack_store)
  if not saved then
    self.assets:select_art_pack(self.art_pack_settings.art_pack_id)
    self.art_pack_error = save_error
    return nil, save_error
  end
  self.art_pack_settings, self.art_pack_error = candidate, nil
  self:play_sound("select")
  return true
end

function App:_save_meta(candidate)
  if self.meta_error then return nil, self.meta_error end
  local saved, error_data = MetaProfile.save(candidate, self.meta_store, self.registry)
  if not saved then self.meta_error = error_data; return nil, error_data end
  self.meta_profile, self.meta_status = candidate, nil
  return true
end

function App:_save_fallen_archive(candidate)
  if self.archive_error then return nil, self.archive_error end
  local saved, error_data = FallenArchive.save(candidate, self.archive_store)
  if not saved then self.archive_error = error_data; return nil, error_data end
  self.fallen_archive, self.archive_status = candidate, nil
  return true
end

-- Death first becomes authoritative in the active run.  Only after that
-- snapshot is safely written do we append its immutable body to the separate
-- archive; source-run deduplication makes a retry safe after interruption.
function App:_archive_pending_death(session)
  local pending = session and session.state.death_pending_archive
  if not pending then return true end
  if self.archive_error then return nil, self.archive_error end
  local candidate = FallenArchive.copy(self.fallen_archive)
  local result = FallenArchive.append(candidate, pending)
  if result.applied then
    local saved, error_data = self:_save_fallen_archive(candidate)
    if not saved then return nil, error_data end
  end
  return result.record
end

function App:_pending_death_session()
  local session, error_data = ActiveRun.load(self.save_store, {
    content = self.content,
    registry = self.registry,
    meta_snapshot = MetaProfile.snapshot(self.meta_profile, self.registry),
  })
  if not session then return nil, error_data end
  if session.state.ended ~= "gameover" then return nil end
  return session
end

function App:_reconcile_pending_death()
  local session, error_data = self:_pending_death_session()
  if not session then
    -- Missing/corrupt/living active saves remain the responsibility of normal
    -- Continue validation.  A dead legacy save has no eligible run identity
    -- and is retired without fabricating archive history.
    if error_data and error_data.code ~= "missing_file" then self.pending_death_load_error = error_data end
    return false
  end
  local archived, archive_error = self:_archive_pending_death(session)
  if not archived then
    self.death_archive_error = archive_error or { code = "write_failed", reason = "Could not archive fallen body" }
    return nil, self.death_archive_error
  end
  local retired, retire_error = ActiveRun.retire(self.save_store)
  if not retired then
    self.death_archive_error = retire_error
    return nil, retire_error
  end
  self.death_archive_error = nil
  return true
end

function App:_claim_meta_reward(reward_id, amount)
  if self.meta_error then return { applied = false, code = "meta_unavailable", reason = self.meta_error.reason } end
  local candidate = MetaProfile.copy(self.meta_profile)
  local result = MetaProfile.claim_reward(candidate, reward_id, amount)
  if result.applied then
    local saved, error_data = self:_save_meta(candidate)
    if not saved then return { applied = false, code = "write_failed", reason = error_data.reason } end
  end
  return result
end

function App:_allocate_new_run()
  if self.meta_error then
    -- A corrupt profile must never trap or overwrite an otherwise playable
    -- game. This fallback deliberately has no persistent reward handler and
    -- leaves the damaged profile untouched for manual recovery.
    return "unprofiled:" .. tostring(self.seed_stream.seed), MetaProfile.snapshot(MetaProfile.new(), self.registry)
  end
  local candidate = MetaProfile.copy(self.meta_profile)
  local run_id = MetaProfile.allocate_run(candidate)
  local saved, error_data = self:_save_meta(candidate)
  if not saved then return nil, error_data end
  return run_id, MetaProfile.snapshot(candidate, self.registry)
end

function App:autosave(_boundary)
  if not self.session then return true end
  if self.session.state.ended == "gameover" then
    local saved, save_error = ActiveRun.save(self.session, self.save_store)
    if not saved then
      self.save_error = save_error
      return nil, save_error
    end
    local archived, archive_error = self:_archive_pending_death(self.session)
    if not archived then
      self.death_archive_error = archive_error or { code = "write_failed", reason = "Could not archive fallen body" }
      self.save_error = self.death_archive_error
      self:refresh_continue()
      return nil, self.death_archive_error
    end
    local retired, error_data = ActiveRun.retire(self.save_store)
    if not retired then self.save_error = error_data end
    if retired then self.death_archive_error = nil end
    self:refresh_continue()
    return retired, error_data
  end
  if self.session.state.ended == "victory" then
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
  local run_id, snapshot = self:_allocate_new_run()
  if not run_id then self.title_error = snapshot; return false end
  self:_new_session(run_id, snapshot)
  self.session:start_run()
  self.screen, self.menu = "game", 1
  self:clear_held_movement()
  self.presentation:reset(self.session)
  self:autosave("new_run")
  self:play_sound("select")
  return true
end

function App:confirm_replace_save()
  local run_id, snapshot = self:_allocate_new_run()
  if not run_id then self.title_error = snapshot; self.screen = "title"; return false end
  self:_new_session(run_id, snapshot)
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
    registry = self.registry,
    meta_snapshot = MetaProfile.snapshot(self.meta_profile, self.registry),
    on_meta_reward = function(id, amount) return self:_claim_meta_reward(id, amount) end,
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
  if selected and selected.name == "RESEARCH" then return self:open_research() end
  if selected and selected.name == "FALLEN" then return self:open_fallen_archive() end
  if selected and selected.name == "ART PACKS" then return self:open_art_packs() end
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

function App:_new_session(run_id, snapshot)
  local seed = self.seed_stream:next()
  local recurrence = nil
  if run_id and not self.archive_error then
    recurrence = FallenRecurrence.assign(run_id, seed, FallenArchive.compatible_records(self.fallen_archive, self.registry))
  end
  self.session = Session.new({
    seed = seed,
    content = self.content,
    registry = self.registry,
    run_id = run_id,
    meta_snapshot = snapshot,
    fallen_recurrence = recurrence,
    on_meta_reward = function(id, amount) return self:_claim_meta_reward(id, amount) end,
    emit = function(event)
      self:_handle_session_event(event)
    end,
  })
end

function App:fallen_archive_entries()
  local entries = {}
  for _, record in ipairs(self.fallen_archive.characters or {}) do
    local compatible, reason = FallenArchive.compatibility(record, self.registry)
    entries[#entries + 1] = {
      archive_id = record.id,
      source_run_id = record.source_run_id,
      record = record,
      compatible = compatible,
      incompatibility_reason = reason,
      name = "FALLEN SHELL — " .. string.upper(record.source_run_id),
      description = compatible and "RECURRENCE READY" or "INCOMPATIBLE WITH CURRENT CONTENT",
    }
  end
  table.sort(entries, function(first, second) return first.archive_id < second.archive_id end)
  return entries
end

function App:open_fallen_archive()
  self.screen, self.menu = "fallen_archive", 1
  return true
end

function App:current_fallen_entry()
  return self:fallen_archive_entries()[self.menu]
end

function App:research_categories()
  local values, seen = {}, {}
  for _, node in pairs(self.registry.research) do if not seen[node.category] then seen[node.category] = true; values[#values + 1] = node.category end end
  table.sort(values)
  return values
end

function App:research_options(category)
  local values = {}
  for _, node in pairs(self.registry.research) do
    if not category or node.category == category then
      local unlocked = MetaProfile.has_research(self.meta_profile, node.id)
      local ready = true
      for _, prerequisite in ipairs(node.prerequisites) do if not MetaProfile.has_research(self.meta_profile, prerequisite) then ready = false end end
      values[#values + 1] = { id = node.id, name = node.display_name, description = node.description, cost = node.cost,
        prerequisites = node.prerequisites, unlocked = unlocked, available = not unlocked and ready,
        locked = not unlocked and not ready, category = node.category }
    end
  end
  table.sort(values, function(a, b) return a.id < b.id end)
  return values
end

function App:open_research()
  self.research_category_index, self.research_node_index = 1, 1
  self.screen, self.menu = "research", 1
  return not self.meta_error
end

function App:current_research_category()
  return self:research_categories()[self.research_category_index or 1]
end

function App:purchase_selected_research()
  if self.meta_error then return { applied = false, code = "meta_unavailable", reason = self.meta_error.reason } end
  local option = self:research_options(self:current_research_category())[self.research_node_index or 1]
  if not option then return { applied = false, code = "unknown_research", reason = "No research is selected" } end
  local candidate = MetaProfile.copy(self.meta_profile)
  local result = MetaProfile.purchase(candidate, self.registry, option.id)
  if not result.applied then return result end
  local saved, error_data = self:_save_meta(candidate)
  if not saved then return { applied = false, code = "write_failed", reason = error_data.reason } end
  result.applies_next_run = self.continue_available
  return result
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
    local boss = node.boss_id and self.session.registry:get_boss(node.boss_id) or nil
    result[#result + 1] = {
      node_id = node.id,
      name = biome and biome.display_name or (boss and boss.display_name) or string.upper(node.type),
      description = biome and ("TIER " .. tier.number .. "  •  FLOOR") or (boss and "MILESTONE BOSS" or string.upper(node.type)),
      service_id = node.service_id,
      service_name = node.service_id and self.session.registry:get_service(node.service_id).display_name or nil,
      biome_id = node.biome_id,
      tier_id = node.tier_id,
      type = node.type,
    }
  end
  return result
end

function App:unlock_display_name(unlock_id)
  for _, node in pairs(self.registry.research) do
    for _, granted in ipairs(node.unlocks or {}) do
      if granted == unlock_id then return node.display_name end
    end
  end
  return "RESEARCH REQUIRED"
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
  elseif result == "boss" then
    self.screen, self.menu = "game", 1
    self:clear_held_movement()
    self.presentation:reset(self.session)
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
  local ability = self.session.registry:get_ability(ability_id)
  -- Only self-destruct is irreversible.  Ordinary body tools such as melee
  -- activate with the existing single confirmation keypress.
  if ability.implementation == "self_destruct" and not self.body_ability_confirming then
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

function App:keypressed(...)
  Input.keypressed(self, ...)
end

function App:keyreleased(...)
  Input.keyreleased(self, ...)
end

return App
