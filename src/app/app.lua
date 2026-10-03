local Content = require("src.content.legacy")
local Input = require("src.app.input")
local Rng = require("src.rng")
local Session = require("src.simulation.session")
local SoundBank = require("src.audio.sound_bank")
local Assets = require("src.rendering.assets")
local Presentation = require("src.rendering.presentation")
local Renderer = require("src.rendering.renderer")
local ActiveRun = require("src.persistence.active_run")
local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local MetaProfile = require("src.persistence.meta_profile")
local FallenArchive = require("src.persistence.fallen_archive")
local FallenRecurrence = require("src.simulation.fallen_recurrence")
local Registry = require("src.content.registry")
local ScreenManager = require("src.ui.screen_manager")
local CursorManager = require("src.ui.cursor_manager")
local PresentationFlow = require("src.presentation.presentation_flow")
local ArtPackConfig = require("src.presentation.art_pack_config")
local Grid = require("src.world.grid")
local InventoryLayout = require("src.ui.inventory_layout")

local App = {}
App.__index = App
App.CAMPAIGN_SLOT_COUNT = 3

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
  -- Slot one deliberately keeps the original `campaign` directory. Existing
  -- installs therefore retain their campaign without a migration; the extra
  -- slots live beside it. Tests and integrations can inject all three stores
  -- explicitly through campaign_slot_stores.
  local primary_campaign_store = options.campaign_store or SaveStore.runtime_directory("campaign")
  self.campaign_stores = options.campaign_slot_stores or {}
  self.campaign_stores[1] = self.campaign_stores[1] or primary_campaign_store
  for index = 2, App.CAMPAIGN_SLOT_COUNT do
    if not self.campaign_stores[index] then
      self.campaign_stores[index] = love and love.filesystem
        and SaveStore.runtime_directory("campaign/slot_" .. index)
        or SaveStore.memory_directory()
    end
  end
  self.active_campaign_slot = 1
  -- Kept as the active-store alias for the campaign/save boundary and older
  -- callers that inject a single campaign_store.
  self.campaign_store = self.campaign_stores[self.active_campaign_slot]
  self.meta_store = options.meta_store or SaveStore.runtime("meta_profile.json")
  self.archive_store = options.archive_store or SaveStore.runtime("fallen_characters.json")
  self.meta_profile, self.meta_status = MetaProfile.load(self.meta_store, self.registry)
  self.meta_error = nil
  if not self.meta_profile then self.meta_error = self.meta_status end
  if not self.meta_profile then self.meta_profile = MetaProfile.new() end
  self.fallen_archive, self.archive_status = FallenArchive.load(self.archive_store)
  self.archive_error = nil
  if not self.fallen_archive then self.archive_error = self.archive_status end
  if not self.fallen_archive then self.fallen_archive = FallenArchive.new() end
  self.art_pack_config, self.art_pack_config_status = ArtPackConfig.load()
  self.art_pack_config_error = self.art_pack_config_status and self.art_pack_config_status.fresh and nil or self.art_pack_config_status
  self.screens, self.screen_definition_error = ScreenManager.load()
  if not self.screens then self.screens = ScreenManager.fallback() end
  self.presentation_flow, self.presentation_flow_error = PresentationFlow.load()
  if not self.presentation_flow then self.presentation_flow = PresentationFlow.fallback() end
  self.seed_stream = Rng.new(options.seed or clock_seed())
  self.screen, self.menu = "title", 1
  self.assets = Assets.new({ art_pack_id = self.art_pack_config.art_pack_id })
  self.sounds = SoundBank.new()
  self.presentation = Presentation.new()
  self.renderer = Renderer.new(self.assets)
  self.cursors = CursorManager.new()
  self.movement_keys = {}
  self:_reconcile_pending_death()
  self:refresh_continue()
  self:refresh_campaign_continue()
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
    local flow, flow_failure = PresentationFlow.load()
    if flow then
      self.presentation_flow, self.presentation_flow_error = flow, nil
    else
      self.presentation_flow_error = flow_failure
    end
    local art_pack, art_pack_status = ArtPackConfig.load()
    self.art_pack_config, self.art_pack_config_error = art_pack,
      (art_pack_status and art_pack_status.fresh and nil or art_pack_status)
    if art_pack.art_pack_id ~= self.assets.art_pack_id then
      self.assets:select_art_pack(art_pack.art_pack_id)
    end
  end
end

function App:quit()
  self:autosave_campaign("quit")
  self:autosave("quit")
  if love and love.event then love.event.quit() end
end

function App:campaign_slot_store(index)
  index = tonumber(index)
  if not index or index % 1 ~= 0 or index < 1 or index > App.CAMPAIGN_SLOT_COUNT then return nil end
  return self.campaign_stores[index]
end

function App:_campaign_persistence_options()
  return {
    content = self.content,
    registry = self.registry,
    meta_snapshot = MetaProfile.snapshot(self.meta_profile, self.registry),
  }
end

-- Campaign slots are intentionally separate from the historical active-run
-- slot. OW-01 never interprets, migrates, or deletes active_run.json.
function App:refresh_campaign_continue()
  local any_available, first_error = false, nil
  self.campaign_slots = {}
  for index = 1, App.CAMPAIGN_SLOT_COUNT do
    local available, error_data = CampaignPersistence.has_valid_campaign(self.campaign_stores[index], self:_campaign_persistence_options())
    local slot = { index = index, store = self.campaign_stores[index], available = available == true, error = error_data }
    self.campaign_slots[index] = slot
    any_available = any_available or slot.available
    first_error = first_error or error_data
  end
  self.campaign_continue_available = any_available
  self.campaign_continue_error = any_available and nil or first_error
  self.legacy_active_run_present = self.save_store:exists()
  return any_available
end

function App:first_empty_campaign_slot()
  for _, slot in ipairs(self.campaign_slots or {}) do
    if not slot.available then return slot.index end
  end
  return nil
end

function App:first_campaign_slot()
  for _, slot in ipairs(self.campaign_slots or {}) do
    if slot.available then return slot.index end
  end
  return nil
end

function App:campaign_slot_count()
  local count = 0
  for _, slot in ipairs(self.campaign_slots or {}) do
    if slot.available then count = count + 1 end
  end
  return count
end

function App:campaign_slot_options()
  local mode = self.campaign_slot_mode or "continue"
  local options = {}
  for _, slot in ipairs(self.campaign_slots or {}) do
    local suffix, description
    if slot.available then
      suffix = mode == "new" and "OCCUPIED" or "CONTINUE"
      description = mode == "new" and "Selecting this slot asks before replacing its campaign."
        or "Resume the campaign saved in this slot."
    else
      suffix = mode == "new" and "EMPTY" or "EMPTY"
      description = mode == "new" and "Start a fresh campaign in this empty slot."
        or "No campaign is saved in this slot."
    end
    options[#options + 1] = {
      slot_index = slot.index,
      available = slot.available,
      name = "SLOT " .. slot.index .. " — " .. suffix,
      description = description,
    }
  end
  return options
end

function App:open_campaign_slots(mode)
  self:refresh_campaign_continue()
  self.campaign_slot_mode = mode == "new" and "new" or "continue"
  local preferred = self.campaign_slot_mode == "new" and self:first_empty_campaign_slot() or self:first_campaign_slot()
  self.screen, self.menu = "campaign_slots", preferred or 1
  return true
end

function App:select_campaign_slot()
  local option = self:campaign_slot_options()[self.menu]
  if not option then return nil, { code = "invalid_slot", reason = "No campaign slot is selected" } end
  if self.campaign_slot_mode == "new" then
    if option.available then
      self.campaign_replace_slot = option.slot_index
      self.screen, self.menu = "replace_campaign", 1
      return false
    end
    return self:request_new_campaign(option.slot_index)
  end
  if not option.available then
    return nil, { code = "empty_slot", reason = "This campaign slot is empty" }
  end
  return self:continue_campaign(option.slot_index)
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
  -- The sibling Unpolished Bees workbench owns title labels/order and art
  -- selection.  Runtime only filters a declared action for actual save state.
  local actions, options = self.presentation_flow:available({
    continue_available = self.campaign_continue_available or self.continue_available,
  }), {}
  for _, action in ipairs(actions) do
    local name, description = action.label, action.description
    if action.id == "continue" and self.campaign_continue_available then
      name, description = "CONTINUE CAMPAIGN", "Resume the persistent one-zone campaign."
    elseif action.id == "continue" and self.continue_available then
      name, description = "LEGACY RUN", "Resume a preserved pre-open-world run in compatibility mode."
    end
    options[#options + 1] = { id = action.id, name = name, description = description, target = action.target }
  end
  return options
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

function App:_claim_meta_reward(reward_id, amount, event)
  if self.meta_error then return { applied = false, code = "meta_unavailable", reason = self.meta_error.reason } end
  local candidate = MetaProfile.copy(self.meta_profile)
  local result
  if event and event.kind == "discovery" then
    result = MetaProfile.claim_discovery(candidate, event.discovery_id, reward_id, amount)
  else
    result = MetaProfile.claim_reward(candidate, reward_id, amount)
  end
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

function App:_allocate_new_campaign()
  if self.meta_error then
    return "campaign:000001", MetaProfile.snapshot(MetaProfile.new(), self.registry)
  end
  local candidate = MetaProfile.copy(self.meta_profile)
  local campaign_id = MetaProfile.allocate_campaign(candidate)
  local saved, error_data = self:_save_meta(candidate)
  if not saved then return nil, error_data end
  return campaign_id, MetaProfile.snapshot(candidate, self.registry)
end

function App:request_new_campaign(slot_index)
  slot_index = slot_index or self:first_empty_campaign_slot()
  local store = self:campaign_slot_store(slot_index)
  if not store then
    return nil, { code = "no_empty_campaign_slot", reason = "All campaign slots are occupied" }
  end
  local campaign_id, snapshot = self:_allocate_new_campaign()
  if not campaign_id then self.campaign_continue_error = snapshot; return nil, snapshot end
  local campaign = Campaign.new({
    seed = self.seed_stream:next(),
    campaign_id = campaign_id,
    content = self.content,
    registry = self.registry,
    meta_snapshot = snapshot,
    on_meta_reward = function(id, amount, event) return self:_claim_meta_reward(id, amount, event) end,
    emit = function(event) self:_handle_session_event(event) end,
  })
  self.active_campaign_slot, self.campaign_store = slot_index, store
  campaign:set_persistence_directory(store)
  self.campaign, self.session = campaign, campaign.session
  self.screen, self.menu = "game", 1
  self:clear_held_movement()
  self.presentation:reset(self.session)
  local saved, error_data = self:autosave_campaign("new_campaign")
  if not saved then return nil, error_data end
  self:play_sound("select")
  return campaign
end

function App:continue_campaign(slot_index)
  slot_index = slot_index or self:first_campaign_slot()
  local store = self:campaign_slot_store(slot_index)
  if not store then
    return nil, { code = "missing_campaign", reason = "No campaign slot is available to continue" }
  end
  local campaign, error_data = CampaignPersistence.load(store, {
    content = self.content,
    registry = self.registry,
    meta_snapshot = MetaProfile.snapshot(self.meta_profile, self.registry),
    on_meta_reward = function(id, amount, event) return self:_claim_meta_reward(id, amount, event) end,
    emit = function(event) self:_handle_session_event(event) end,
  })
  if not campaign then
    self.campaign_continue_error = error_data
    self.campaign_continue_available = false
    return nil, error_data
  end
  self.active_campaign_slot, self.campaign_store = slot_index, store
  campaign:set_persistence_directory(store)
  self.campaign, self.session = campaign, campaign.session
  self.screen, self.menu = "game", 1
  self:clear_held_movement()
  self.presentation:reset(self.session)
  self.campaign_continue_available, self.campaign_continue_error = true, nil
  if self.campaign_slots and self.campaign_slots[slot_index] then
    self.campaign_slots[slot_index].available, self.campaign_slots[slot_index].error = true, nil
  end
  return campaign
end

function App:autosave_campaign(_boundary)
  if not self.campaign then return true end
  local saved, error_data = CampaignPersistence.save(self.campaign, self.campaign_store)
  if not saved then
    self.campaign_save_error = error_data
    self.campaign.session:_log("Campaign save failed: " .. tostring(error_data and error_data.reason or "unknown error"))
    return nil, error_data
  end
  self.campaign_save_error = nil
  self.campaign_continue_available = true
  if self.campaign_slots and self.campaign_slots[self.active_campaign_slot] then
    self.campaign_slots[self.active_campaign_slot].available = true
    self.campaign_slots[self.active_campaign_slot].error = nil
  end
  return true
end

function App:autosave(_boundary)
  if self.campaign and self.session == self.campaign.session then
    return self:autosave_campaign(_boundary)
  end
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

-- The profile's initial sequence number is enough to identify the first-ever
-- run.  This adds no persistence field: cancelling the briefing leaves the
-- account untouched, while direct/test callers can still start a run through
-- the existing request_new_run API.
function App:begin_new_run()
  if not self.continue_available and not self.meta_error and self.meta_profile.next_run_sequence == 1 then
    self.screen, self.menu = "onboarding", 1
    return true
  end
  return self:request_new_run()
end

function App:begin_new_campaign()
  self:refresh_campaign_continue()
  local empty_slot = self:first_empty_campaign_slot()
  if empty_slot then return self:request_new_campaign(empty_slot) end
  -- All slots are in use. Show the player exactly which campaign they are
  -- replacing instead of treating their existing save as a New Run blocker.
  return self:open_campaign_slots("new")
end

function App:confirm_replace_campaign()
  local slot_index = self.campaign_replace_slot
  if not self:campaign_slot_store(slot_index) then
    self.screen, self.menu = "campaign_slots", 1
    return nil, { code = "invalid_slot", reason = "No campaign slot was selected for replacement" }
  end
  self.campaign, self.session = nil, nil
  self.title_error = nil
  local campaign, error_data = self:request_new_campaign(slot_index)
  self.campaign_replace_slot = nil
  if not campaign then
    self.title_error = error_data or { code = "write_failed", reason = "Could not start a new campaign" }
    self.screen, self.menu = "title", 1
  end
  return campaign, error_data
end

function App:onboarding_sections()
  return {
    "YOUR BODY IS TEMPORARY.",
    "BREAK ENEMY BODIES. SALVAGE USEFUL PARTS. SURVIVE THE FLOOR.",
    "REBUILD BETWEEN FLOORS. DEATH ENDS THE RUN; RESEARCH SURVIVES.",
    "WASD MOVE   ARROWS SHOOT   X BODY ABILITIES   G SALVAGE   I INVENTORY   U INTERACT",
  }
end

function App:help_sections()
  return {
    { title = "CORE LOOP", text = "Survive a floor, salvage physical parts, then reconstruct your body before the next descent." },
    { title = "MOVEMENT + COMBAT", text = "WASD moves. Arrow keys shoot. Q dashes. B throws a bomb. F places a flare." },
    { title = "BODY DAMAGE", text = "Broken components lose their granted capabilities. IMPAIRED or CRAWLING means locomotion parts were damaged." },
    { title = "SALVAGE + INVENTORY", text = "G opens a nearby corpse. Parts need space in the grid; R rotates selected cargo." },
    { title = "RECONSTRUCTION", text = "Install salvaged parts only between floors. Reconstruction never repairs a damaged component." },
    { title = "SERVICES + ROUTE", text = "U accesses nearby services. Spend SCRAP on supplies, repairs, parts, or charms; route choices are one way." },
    { title = "RESEARCH + DEATH", text = "RESEARCH DATA unlocks future runs. Death archives the body; a fallen shell can recur later." },
  }
end

function App:open_help()
  self.screen, self.menu = "help", 1
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
    on_meta_reward = function(id, amount, event) return self:_claim_meta_reward(id, amount, event) end,
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
  if selected and selected.id == "continue" then
    if not self.campaign_continue_available then return self:continue_run() end
    self:refresh_campaign_continue()
    local first = self:first_campaign_slot()
    -- A single saved campaign keeps the original one-click Continue flow.
    if first and self:campaign_slot_count() == 1 then
      return self:continue_campaign(first)
    end
    return self:open_campaign_slots("continue")
  end
  if selected and selected.id == "research" then return self:open_research() end
  if selected and selected.id == "fallen" then return self:open_fallen_archive() end
  if selected and selected.id == "help" then return self:open_help() end
  return self:begin_new_campaign()
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
    on_meta_reward = function(id, amount, event) return self:_claim_meta_reward(id, amount, event) end,
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
      local prerequisite_names = {}
      for _, prerequisite in ipairs(node.prerequisites) do
        local prerequisite_node = self.registry:get_research(prerequisite)
        prerequisite_names[#prerequisite_names + 1] = prerequisite_node.display_name
      end
      values[#values + 1] = { id = node.id, name = node.display_name, description = node.description, cost = node.cost,
        prerequisites = node.prerequisites, unlocked = unlocked, available = not unlocked and ready,
        locked = not unlocked and not ready, category = node.category, prerequisite_names = prerequisite_names }
    end
  end
  table.sort(values, function(a, b) return a.id < b.id end)
  return values
end

function App:discovery_history()
  local entries = {}
  for id, definition in pairs(self.registry.discoveries) do
    local discovered = MetaProfile.has_discovery(self.meta_profile, id)
    entries[#entries + 1] = {
      id = id,
      discovered = discovered,
      -- The UI model itself withholds undiscovered names/descriptions, so a
      -- future presentation surface cannot accidentally spoil the corpus.
      name = discovered and definition.display_name or "???",
      description = discovered and definition.description or nil,
      biome_id = definition.allowed_biome_ids[1],
    }
  end
  table.sort(entries, function(left, right) return left.id < right.id end)
  return entries
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
  if result == "storage" then
    self:open_storage(self.session.state.active_storage_object_id)
  end
end

function App:perform_turn(input)
  if self.screen ~= "game" then
    return
  end
  local result = self.session:turn(input)
  if (result == "zone_transition" or result == "campaign_succession") and self.campaign then
    -- Campaign atomically swaps its active local simulator only after the
    -- zone shards and manifest commit. Presentation observes that new zone.
    self.session = self.campaign.session
    self:clear_held_movement()
    self.presentation:reset(self.session)
  end
  self:_handle_turn_result(result)
  self:autosave("turn")
  return result
end

function App:close_overlay()
  self.screen = "game"
  self.menu = 1
  self.inventory_selected_id = nil
  self.inventory_drag = nil
  self.salvage_corpse_id = nil
  self.storage_object_id = nil
  if self.session and self.session.state then self.session.state.active_storage_object_id = nil end
end

function App:open_build()
  if not self.session or not self.campaign then return false end
  self.build_recipe_index = 1
  self.build_recipe_id = nil
  self.screen, self.menu = "build", 1
  self:play_sound("select")
  return true
end

function App:build_recipes()
  if not self.session then return {} end
  return require("src.construction.building").recipes(self.session.registry)
end

function App:select_build_recipe()
  local recipe = self:build_recipes()[self.menu]
  if not recipe then return nil end
  local player = self.session.state.player
  local delta = ({ w = { 0, 1 }, a = { -1, 0 }, s = { 0, -1 }, d = { 1, 0 } })[player.direction] or { 1, 0 }
  self.build_recipe_id = recipe.id
  self.build_cursor = { x = clamp(player.x + delta[1], 0, Grid.width - 1), y = clamp(player.y + delta[2], 0, Grid.height - 1) }
  self.screen = "build_place"
  self:play_sound("select")
  return recipe
end

function App:move_build_cursor(dx, dy)
  local cursor = self.build_cursor or { x = self.session.state.player.x, y = self.session.state.player.y }
  cursor.x, cursor.y = clamp(cursor.x + dx, 0, Grid.width - 1), clamp(cursor.y + dy, 0, Grid.height - 1)
  self.build_cursor = cursor
  self:play_sound("select")
end

function App:build_preview()
  if not self.session or not self.build_recipe_id or not self.build_cursor then
    return { applied = false, code = "unknown_recipe", reason = "No construction recipe selected" }
  end
  return require("src.construction.building").validate(self.session, self.build_recipe_id, self.build_cursor.x, self.build_cursor.y)
end

function App:confirm_build()
  if not self.build_recipe_id or not self.build_cursor then return nil end
  self.session.state.last_build_result = nil
  self.screen = "game"
  self:perform_turn(string.format("build:%s:%d:%d", self.build_recipe_id, self.build_cursor.x, self.build_cursor.y))
  local result = self.session.state.last_build_result
  if result and not result.applied then self.screen = "build_place" end
  if result and result.applied then self.build_recipe_id = nil end
  return result
end

function App:open_storage(object_id)
  local object = self.session and self.session.state.world and self.session.state.world:get_object(object_id)
  if not object or not object.storage_inventory then return false end
  self.storage_object_id = object_id
  self.storage_focus, self.storage_index = "player", 1
  self.screen, self.menu = "storage", 1
  self:play_sound("select")
  return true
end

function App:storage_inventory()
  local object = self.session and self.session.state.world and self.session.state.world:get_object(self.storage_object_id)
  return object and object.storage_inventory or nil
end

function App:storage_entries()
  local inventory = self.storage_focus == "storage" and self:storage_inventory()
    or (self.session and self.session.state.inventory)
  return inventory and inventory.entries or {}
end

function App:toggle_storage_focus()
  self.storage_focus = self.storage_focus == "player" and "storage" or "player"
  self.storage_index = 1
  self:play_sound("select")
end

function App:move_storage_selection(amount)
  self.storage_index = clamp((self.storage_index or 1) + amount, 1, math.max(1, #self:storage_entries()))
  self:play_sound("select")
end

function App:storage_transfer_selected()
  local entry = self:storage_entries()[self.storage_index or 1]
  if not entry then return nil end
  local direction = self.storage_focus == "player" and "to_storage" or "to_player"
  local result = self.session:storage_transfer(self.storage_object_id, entry.physical_id, direction)
  if result.applied then
    self.session:_log("MOVED " .. result.item.display_name .. ".")
    self.storage_index = clamp(self.storage_index, 1, math.max(1, #self:storage_entries()))
    self:autosave("storage")
    self:play_sound("pickup")
  else
    self.session:_log(result.reason)
  end
  return result
end

function App:open_inventory()
  if not self.session or not self.session.state.inventory then
    return false
  end
  self.inventory_cursor = self.inventory_cursor or { x = 1, y = 1 }
  self.inventory_selected_id = nil
  self.inventory_drag = nil
  self.screen = "inventory"
  self:play_sound("select")
  return true
end

function App:open_reconstruction()
  if not self.session or not self.session:_reconstruction_allowed() then
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
    if entry.item.item_type ~= "component" then
      self.session:_log("Only components can be installed into a body.")
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
    if result.next == "combat" then self.screen, self.menu = "game", 1 end
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

function App:inventory_layout(viewport_width, viewport_height)
  local inventory = self.session and self.session.state and self.session.state.inventory
  if not inventory then return nil end
  if type(viewport_width) ~= "number" or type(viewport_height) ~= "number" then
    if not (love and love.graphics) then return nil end
    viewport_width, viewport_height = love.graphics.getDimensions()
  end
  return InventoryLayout.for_viewport(inventory, viewport_width, viewport_height)
end

function App:title_option_at(x, y, viewport_width, viewport_height)
  if self.screen ~= "title" then return nil end
  if type(viewport_width) ~= "number" or type(viewport_height) ~= "number" then
    if not (love and love.graphics) then return nil end
    viewport_width, viewport_height = love.graphics.getDimensions()
  end
  local function option_at(pointer_x, pointer_y)
    local start_x = viewport_width / 2 - 84
    local end_x = viewport_width / 2 + 260
    if pointer_x < start_x or pointer_x > end_x then return nil end
    for index = 1, #self:title_options() do
      local line_y = viewport_height / 2 + 26 + index * 29
      if pointer_y >= line_y - 8 and pointer_y <= line_y + 20 then return index end
    end
    return nil
  end
  local selected = option_at(x, y)
  if selected then return selected end
  -- Windows can report pointer coordinates in physical pixels while a
  -- non-high-DPI LÖVE canvas is rendered in logical pixels.  The title is the
  -- first pointer-driven screen, so accept that scaled coordinate space too.
  local dpi = love and love.window and love.window.getDPIScale and love.window.getDPIScale() or 1
  -- Prefer the 150% scale visible in the shipped Windows build before less
  -- common guesses: scaled row spacing can otherwise map one click to a
  -- neighbouring title option.
  local scales, tried = { dpi, 1.5, 1.25, 2 }, {}
  for _, scale in ipairs(scales) do
    if type(scale) == "number" and scale > 1 and not tried[scale] then
      tried[scale] = true
      selected = option_at(x / scale, y / scale)
      if selected then return selected end
    end
  end
  return nil
end

function App:campaign_slot_at(x, y, viewport_width, viewport_height)
  if self.screen ~= "campaign_slots" then return nil end
  if type(viewport_width) ~= "number" or type(viewport_height) ~= "number" then
    if not (love and love.graphics) then return nil end
    viewport_width, viewport_height = love.graphics.getDimensions()
  end
  local options = self:campaign_slot_options()
  local function option_at(pointer_x, pointer_y)
    if pointer_x < viewport_width * 0.18 or pointer_x > viewport_width * 0.82 then return nil end
    local first_y, stride = 138, 70
    for index = 1, #options do
      local y = first_y + (index - 1) * stride
      if pointer_y >= y and pointer_y <= y + 58 then return index end
    end
    return nil
  end
  local selected = option_at(x, y)
  if selected then return selected end
  local dpi = love and love.window and love.window.getDPIScale and love.window.getDPIScale() or 1
  local scales, tried = { dpi, 1.5, 1.25, 2 }, {}
  for _, scale in ipairs(scales) do
    if type(scale) == "number" and scale > 1 and not tried[scale] then
      tried[scale] = true
      selected = option_at(x / scale, y / scale)
      if selected then return selected end
    end
  end
  return nil
end

function App:_update_inventory_drag(pointer_x, pointer_y, layout)
  local drag = self.inventory_drag
  if not drag then return nil end
  local inventory = self.session.state.inventory
  local entry = inventory:get(drag.physical_id)
  if not entry then
    self.inventory_drag, self.inventory_selected_id = nil, nil
    return nil
  end
  local cell_x, cell_y = InventoryLayout.cell_at(layout, pointer_x, pointer_y, true)
  drag.x, drag.y = cell_x - drag.grab_x, cell_y - drag.grab_y
  drag.valid, drag.reason = inventory:can_place(entry.item, drag.x, drag.y, drag.rotated, entry.physical_id)
  return drag
end

function App:inventory_mousepressed(x, y, button, viewport_width, viewport_height)
  if self.screen ~= "inventory" or button ~= 1 then return nil end
  local layout = self:inventory_layout(viewport_width, viewport_height)
  if not layout then return nil end
  local cell_x, cell_y = InventoryLayout.cell_at(layout, x, y)
  if not cell_x then return nil end
  self.inventory_cursor.x, self.inventory_cursor.y = cell_x, cell_y
  local inventory = self.session.state.inventory
  local entry = inventory:item_at(cell_x, cell_y)
  if not entry then
    self.inventory_selected_id = nil
    return nil
  end
  self.inventory_selected_id = entry.physical_id
  self.inventory_drag = {
    physical_id = entry.physical_id,
    grab_x = cell_x - entry.x,
    grab_y = cell_y - entry.y,
    x = entry.x,
    y = entry.y,
    rotated = entry.rotated,
    valid = true,
  }
  self:play_sound("select")
  return self.inventory_drag
end

function App:inventory_mousemoved(x, y, _, _, viewport_width, viewport_height)
  if self.screen ~= "inventory" or not self.inventory_drag then return nil end
  local layout = self:inventory_layout(viewport_width, viewport_height)
  return layout and self:_update_inventory_drag(x, y, layout) or nil
end

function App:inventory_mousereleased(x, y, button, viewport_width, viewport_height)
  if self.screen ~= "inventory" or button ~= 1 or not self.inventory_drag then return nil end
  local layout = self:inventory_layout(viewport_width, viewport_height)
  if layout then self:_update_inventory_drag(x, y, layout) end
  local drag = self.inventory_drag
  local inventory = self.session.state.inventory
  local entry = drag and inventory:get(drag.physical_id) or nil
  self.inventory_drag = nil
  if drag and entry and drag.x == entry.x and drag.y == entry.y and drag.rotated == entry.rotated then
    -- A plain click keeps the familiar keyboard-style selection without
    -- producing an unnecessary save; only a changed drop repacks cargo.
    self.inventory_selected_id = entry.physical_id
    return entry
  end
  self.inventory_selected_id = nil
  if not drag or not drag.valid then
    self:play_sound("select")
    return nil, drag and drag.reason or "Item was not dropped on the inventory"
  end
  local moved, reason = inventory:move(drag.physical_id, drag.x, drag.y, drag.rotated)
  if moved then
    self.inventory_cursor.x, self.inventory_cursor.y = drag.x, drag.y
    self.session:_log("Repacked " .. moved.item.display_name .. ".")
    self:play_sound("pickup")
    self:autosave("inventory")
  else
    self.session:_log(reason)
    self:play_sound("select")
  end
  return moved, reason
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
  if self.inventory_drag then
    local drag = self.inventory_drag
    local entry = inventory:get(drag.physical_id)
    if not entry then return nil end
    drag.rotated = not drag.rotated
    local layout = self:inventory_layout()
    if layout and love and love.mouse then
      local pointer_x, pointer_y = love.mouse.getPosition()
      self:_update_inventory_drag(pointer_x, pointer_y, layout)
    else
      drag.valid, drag.reason = inventory:can_place(entry.item, drag.x, drag.y, drag.rotated, entry.physical_id)
    end
    self:play_sound("select")
    return drag
  end
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
  if not corpse then return {} end
  local options = corpse:list_components()
  for _, entry in ipairs(corpse:list_carried_items()) do
    options[#options + 1] = {
      slot_id = "CARRIED", component = entry.item.object, item = entry.item, physical_id = entry.physical_id, carried = true,
    }
  end
  return options
end

function App:salvage_selected()
  local options = self:salvage_options()
  local selection = options[self.menu]
  if not selection then
    self.session:_log("Corpse has no salvageable components.")
    return nil
  end
  local result = selection.carried
    and self.session:salvage_corpse_carried_item(self.salvage_corpse_id, selection.physical_id)
    or self.session:salvage_corpse_component(self.salvage_corpse_id, selection.slot_id)
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

function App:mousepressed(x, y, button, viewport_width, viewport_height)
  if self.screen == "campaign_slots" and button == 1 then
    self.menu = self:campaign_slot_at(x, y, viewport_width, viewport_height) or self.menu
    return self:select_campaign_slot()
  end
  if self.screen == "replace_campaign" and button == 1 then
    return self:confirm_replace_campaign()
  end
  if self.screen == "title" and button == 1 then
    local index = self:title_option_at(x, y, viewport_width, viewport_height)
    -- The default selection is NEW RUN.  Treat any title-screen click as
    -- confirmation so scaled Windows window coordinates cannot make the
    -- title feel unresponsive; a recognised row still selects that row.
    self.menu = index or self.menu
    return self:activate_title_choice()
  end
  return self:inventory_mousepressed(x, y, button, viewport_width, viewport_height)
end

function App:mousemoved(...)
  return self:inventory_mousemoved(...)
end

function App:mousereleased(x, y, button, viewport_width, viewport_height)
  -- Some Windows touchpad drivers reliably deliver the button-up event even
  -- when their button-down event was consumed by desktop scaling.  It is safe
  -- to confirm here too: a successful press has already changed the screen.
  if self.screen == "title" and button == 1 then
    local index = self:title_option_at(x, y, viewport_width, viewport_height)
    self.menu = index or self.menu
    return self:activate_title_choice()
  end
  if self.screen == "campaign_slots" and button == 1 then
    self.menu = self:campaign_slot_at(x, y, viewport_width, viewport_height) or self.menu
    return self:select_campaign_slot()
  end
  return self:inventory_mousereleased(x, y, button, viewport_width, viewport_height)
end

return App
