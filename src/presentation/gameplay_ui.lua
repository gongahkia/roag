-- Player-facing formatting and view-model helpers.  These functions are
-- intentionally observational: Session, Body, Economy, and Interaction keep
-- authority over rules and transactions while the renderer receives stable,
-- readable labels instead of raw content or physical IDs.
local Component = require("src.body.component")
local Grid = require("src.world.grid")
local PhysicalItem = require("src.inventory.physical_item")
local Loadout = require("src.simulation.loadout")

local GameplayUI = {}

local FAILURE_TEXT = {
  already_full_integrity = "ALREADY FULLY REPAIRED",
  charm_slots_full = "NO FREE CHARM SLOT",
  crawl_recovering = "CRAWLING — RECOVERING",
  destroyed = "OBJECT DESTROYED",
  insufficient_currency = "NOT ENOUGH SCRAP",
  insufficient_ammo = "NOT ENOUGH AMMO",
  invalid_phase = "ACTION UNAVAILABLE HERE",
  inventory_full = "INVENTORY FULL",
  insufficient_material = "NOT ENOUGH MATERIAL",
  blocks_travel_connection = "BLOCKS TRAVEL CONNECTION",
  blocks_reconstruction_anchor = "BLOCKS RECONSTRUCTION ANCHOR",
  campaign_only = "CAMPAIGN ONLY",
  no_ammo = "NOT ENOUGH AMMO",
  no_choice = "NO ROUTE SELECTED",
  no_world = "NO ACTIVE WORLD",
  not_interactable = "NOTHING TO INTERACT WITH",
  out_of_stock = "OUT OF STOCK",
  out_of_range = "TOO FAR AWAY",
  requires_power = "NO POWER",
  requires_unlock = "RESEARCH REQUIRED",
  write_failed = "SAVE FAILED",
}

local function uppercase_words(value)
  return tostring(value or "UNKNOWN"):gsub("_", " "):upper()
end

local function list_labels(values)
  if not values or #values == 0 then return "NONE" end
  return table.concat(values, ", ")
end

function GameplayUI.slot_label(slot_id)
  return uppercase_words(slot_id)
end

function GameplayUI.condition_label(component)
  return uppercase_words(Component.condition(component))
end

function GameplayUI.failure_text(value)
  if type(value) == "table" then
    if FAILURE_TEXT[value.code] then return FAILURE_TEXT[value.code] end
    value = value.reason or value.code
  end
  if not value or value == "" then return "ACTION UNAVAILABLE" end
  local text = tostring(value)
  if FAILURE_TEXT[text] then return FAILURE_TEXT[text] end
  return text:gsub("_", " "):upper()
end

function GameplayUI.component(session, component, slot_id)
  if not component then
    return { empty = true, slot = slot_id and GameplayUI.slot_label(slot_id) or nil, condition = "EMPTY", abilities = {} }
  end
  local definition = session.registry:get_component(component.definition_id)
  local abilities = {}
  for _, ability_id in ipairs(definition.abilities or {}) do
    abilities[#abilities + 1] = session.registry:get_ability(ability_id).display_name
  end
  return {
    name = definition.display_name,
    slot = slot_id and GameplayUI.slot_label(slot_id) or nil,
    condition = GameplayUI.condition_label(component),
    functional = Component.is_functional(component),
    current_integrity = component.current_integrity,
    max_integrity = component.max_integrity,
    mass = definition.mass,
    width = definition.inventory.width,
    height = definition.inventory.height,
    abilities = abilities,
    ability_text = list_labels(abilities),
    compatible_slots = definition.compatible_slots,
    wear_per_use = definition.wear_per_use or 0,
  }
end

function GameplayUI.ability(session, actor, ability_id)
  local ability = session.registry:get_ability(ability_id)
  local provider = session:actor_ability_provider(actor, ability_id)
  local provider_model = provider and GameplayUI.component(session, provider.component, provider.slot_id) or nil
  local resource = ability.resource and (string.upper(ability.resource.name) .. ": " .. ability.resource.amount) or "NO AMMO COST"
  return {
    id = ability.id,
    name = ability.display_name,
    implementation = ability.implementation,
    provider = provider_model,
    available = provider ~= nil,
    resource = resource,
    wear = provider_model and provider_model.wear_per_use or 0,
    damage = ability.damage,
    force = ability.force,
    range = ability.range,
  }
end

function GameplayUI.body(session, actor)
  local slots = {}
  if not actor or not actor.body then return slots end
  for _, slot_id in ipairs(actor.body.slot_order) do
    local slot = actor.body:get_slot(slot_id)
    slots[#slots + 1] = GameplayUI.component(session, slot.component, slot_id)
  end
  return slots
end

function GameplayUI.hud(session)
  local state, player, inventory = session.state, session.state.player, session.state.inventory
  local charm_slots = session:modifier_value("charm_slots") or 0
  local charm_count = 0
  for index = 1, charm_slots do if state.charms and state.charms.slots[index] then charm_count = charm_count + 1 end end
  local locomotion = session:locomotion_state(player)
  local quick = nil
  if session.campaign then
    local loadout = session:campaign_loadout()
    local function slot_model(kind, index)
      local binding = loadout and (kind == "weapon" and loadout.weapon_slots[index] or loadout.ability_slots[index]) or nil
      local resolved, failure = Loadout.resolve(session, player, binding, kind)
      local label = resolved and resolved.ability.display_name or "EMPTY"
      local model = {
        label = label,
        active = index == (kind == "weapon" and loadout.active_weapon or loadout.active_ability),
        available = resolved ~= nil,
        reason = failure and failure.reason or nil,
        ability_id = binding and binding.ability_id or nil,
      }
      if resolved and resolved.ability.ammo then
        local magazine = session:weapon_magazine(resolved.provider, resolved.ability)
        model.ammo = {
          loaded = magazine.loaded,
          capacity = resolved.ability.ammo.magazine_capacity,
          family = resolved.ability.ammo.family,
          reserve = session:ammo_reserve(resolved.ability.ammo.family),
        }
      end
      return model
    end
    quick = {
      weapons = { slot_model("weapon", 1), slot_model("weapon", 2) },
      abilities = { slot_model("ability", 1), slot_model("ability", 2) },
    }
  end
  return {
    health = player.health,
    max_health = player.max_health,
    ammo = player.ammo,
    bombs = player.bombs,
    flares = player.flares,
    armed_bombs = #(state.bombs or {}),
    lit_flares = #(state.flares or {}),
    dash = player.dash == 0 and "READY" or "RECHARGING",
    -- Surface campaign traversal is never objective-gated. Legacy sessions
    -- retain the normal-floor counter until route progression is retired.
    objective_progress = session.campaign and nil or (player.objective_progress or 0),
    objective_required = session.campaign and nil or state.settings.objective_required,
    location = session.campaign and state.settings.location_name or nil,
    scrap = state.scrap or 0,
    charm_count = charm_count,
    charm_slots = charm_slots,
    curse = state.curse and (state.curse.display_name or state.curse.name) or nil,
    locomotion = locomotion.state,
    cargo_mass = inventory:total_mass(),
    encumbrance = inventory:encumbrance(),
    quick = quick,
  }
end

-- This mirrors Interaction.primary's stable ordering.  It only decides which
-- already-authoritative action to show, never which action will execute.
function GameplayUI.context_action(session)
  local state, player = session.state, session.state.player
  if state.exit and Grid.distance(player, state.exit) <= 1 then
    return { key = "MOVE", label = "EXIT READY — STEP ONTO EXIT", available = true, priority = 1 }
  end
  -- Campaign context is intentionally physical and directional. A corpse is
  -- selected before ordinary objects on the faced cell; Session handles a
  -- faced vertical connection before this UI helper is consulted.
  if session.campaign then
    local corpse = session.faced_corpse and session:faced_corpse() or nil
    if corpse then
      local count = #corpse:list_components() + #corpse:list_carried_items()
      return {
        key = "U",
        label = corpse.source_kind == "player" and "SALVAGE FALLEN BODY"
          or (corpse.fallen_archive_id and "SALVAGE FALLEN SHELL" or "SALVAGE REMAINS"),
        available = count > 0,
        reason = count > 0 and nil or "NO SALVAGE REMAINS",
        priority = 2,
      }
    end
    local interactions = session.faced_interactions and session:faced_interactions(player) or {}
    if interactions[1] and interactions[1].actions[1] then
      local action = interactions[1].actions[1]
      return {
        key = "U",
        label = action.label,
        available = action.available,
        reason = action.available and nil or GameplayUI.failure_text(action),
        object_name = interactions[1].display_name,
        priority = 3,
      }
    end
    local ground = session.faced_ground_item and session:faced_ground_item() or nil
    if ground then
      if ground.item.item_type == "resource_stack" then
        return {
          key = "MOVE",
          label = "WALK OVER " .. string.upper(ground.item.display_name),
          available = true,
          priority = 4,
        }
      end
      return {
        key = "U",
        label = "PICK UP " .. string.upper(ground.item.display_name),
        available = session.state.inventory:find_first_fit(ground.item) ~= nil,
        reason = "INVENTORY FULL",
        priority = 4,
      }
    end
    return nil
  end
  local ground = session.nearby_ground_item and session:nearby_ground_item() or nil
  if ground then
    return {
      key = "U",
      label = "PICK UP " .. string.upper(ground.item.display_name),
      available = session.state.inventory:find_first_fit(ground.item) ~= nil,
      reason = "INVENTORY FULL",
      priority = 3,
    }
  end
  local interactions = session:available_interactions(player)
  if interactions[1] and interactions[1].actions[1] then
    local action = interactions[1].actions[1]
    return {
      key = "U",
      label = action.label,
      available = action.available,
      reason = action.available and nil or GameplayUI.failure_text(action),
      object_name = interactions[1].display_name,
      priority = 2,
    }
  end
  local corpse = session:nearby_corpse()
  if corpse then
    local count = #corpse:list_components() + #corpse:list_carried_items()
    return {
      key = "G",
      label = corpse.source_kind == "player" and "SALVAGE FALLEN BODY"
        or (corpse.fallen_archive_id and "SALVAGE FALLEN SHELL" or "SALVAGE REMAINS"),
      available = count > 0,
      reason = count > 0 and nil or "NO SALVAGE REMAINS",
      priority = 4,
    }
  end
  return nil
end

function GameplayUI.salvage(session, installed)
  local model = GameplayUI.component(session, installed.component, installed.slot_id)
  local item = PhysicalItem.from_component(installed.component, session.registry)
  local placement = session.state.inventory:find_first_fit(item)
  local current = not installed.carried and session.state.player.body:get_slot(installed.slot_id) or nil
  return {
    component = model,
    fits = placement ~= nil,
    fit_text = placement and "FITS INVENTORY" or "NO INVENTORY SPACE",
    current = current and current.component and GameplayUI.component(session, current.component, installed.slot_id) or nil,
  }
end

function GameplayUI.inventory_entry(session, entry, inventory)
  if entry.item.item_type == "resource_stack" then
    local definition = session.registry:get_resource(entry.item.resource_id)
    local width, height = inventory:footprint(entry.item, entry.rotated)
    local ammo = entry.item.resource_id:match("^resource%.ammo%.") ~= nil
    return {
      item_type = ammo and "ammo" or "resource",
      category = ammo and "AMMO" or "RESOURCE",
      name = definition.display_name,
      quantity = entry.item.quantity,
      mass = entry.item.mass,
      width = width,
      height = height,
      rotated = entry.rotated == true,
      description = ammo and "PHYSICAL AMMUNITION" or "CONSTRUCTION RESOURCE",
    }
  end
  local model = GameplayUI.component(session, entry.item.object)
  model.item_type = "component"
  model.category = "COMPONENT"
  local width, height = inventory:footprint(entry.item, entry.rotated)
  model.width, model.height = width, height
  model.rotated = entry.rotated == true
  for _, ability_id in ipairs(session.registry:get_component(entry.item.object.definition_id).abilities or {}) do
    local ability = session.registry:get_ability(ability_id)
    if ability.ammo then
      local key = ability.ammo.magazine_key or ability.id
      local saved = entry.item.object.weapon_state and entry.item.object.weapon_state[key]
      local loaded = saved and saved.loaded or ability.ammo.magazine_capacity
      model.magazine = {
        loaded = math.max(0, math.min(ability.ammo.magazine_capacity, math.floor(loaded or 0))),
        capacity = ability.ammo.magazine_capacity,
        family = ability.ammo.family,
      }
      break
    end
  end
  return model
end

function GameplayUI.service_option(session, option)
  local state = session.state
  local value = {
    name = option.label or option.name or "SERVICE OPTION",
    price = option.price,
    remaining = option.remaining,
    sold = option.sold == true,
    affordable = option.price == nil or state.scrap >= option.price,
    description = option.description,
  }
  if option.integrity then value.description = "INTEGRITY " .. option.integrity .. " / " .. option.max_integrity end
  if option.action == "buy_component" and option.component_id then
    -- The component ID is physical.  Service option labels are authoritative
    -- player copy, so it is deliberately not surfaced here.
    value.description = value.description or "SALVAGEABLE BODY COMPONENT"
  elseif option.action == "sell_component" then
    value.description = "SELL FOR SCRAP"
  elseif option.action == "remove_charm" then
    value.description = "REMOVE FROM CHARM LATTICE"
  end
  return value
end

function GameplayUI.enemy(session, actor)
  local definition = actor.content_id and session.registry.enemies[actor.content_id] or nil
  local faction = actor.faction_id and session.registry.factions[actor.faction_id] or nil
  local abilities = {}
  if actor.body then
    for _, ability_id in ipairs(actor.body:list_capabilities()) do
      abilities[#abilities + 1] = session.registry:get_ability(ability_id).display_name
    end
  end
  local weapon = nil
  for _, ability_id in ipairs(actor.body and actor.body:list_capabilities() or {}) do
    local ability = session.registry:get_ability(ability_id)
    if ability.implementation == "projectile" or ability.implementation == "scattershot"
      or ability.implementation == "piercing_projectile" or ability.implementation == "melee" then
      weapon = ability.display_name
      break
    end
  end
  return {
    name = definition and definition.display_name or string.upper(actor.kind or "UNKNOWN"),
    faction = faction and faction.display_name or nil,
    health = actor.health,
    max_health = actor.max_health,
    locomotion = session:locomotion_state(actor).state,
    abilities = abilities,
    role = uppercase_words(actor.ai_role or "rusher"),
    weapon = weapon,
    intent = session:enemy_intent(actor),
  }
end

function GameplayUI.boss(session, boss)
  if not boss then return nil end
  local telegraph = nil
  if boss.pending_telegraph then
    local ability = session.registry:get_ability(boss.pending_telegraph.ability_id)
    local provider = session:actor_ability_provider(boss, boss.pending_telegraph.ability_id, boss.pending_telegraph.provider_component_id)
    telegraph = {
      ability = ability.display_name,
      remaining = boss.pending_telegraph.remaining,
      provider = provider and GameplayUI.component(session, provider.component, provider.slot_id).name or "DESTROYED PROVIDER",
      cancelled = provider == nil,
    }
  end
  local subsystems = {}
  for _, installed in ipairs(boss.body:list_installed_slots()) do
    local definition = session.registry:get_component(installed.component.definition_id)
    if #(definition.abilities or {}) > 0 then subsystems[#subsystems + 1] = GameplayUI.component(session, installed.component, installed.slot_id) end
  end
  return {
    name = boss.display_name,
    health = boss.health,
    max_health = boss.max_health,
    locomotion = session:locomotion_state(boss).state,
    telegraph = telegraph,
    subsystems = subsystems,
  }
end

function GameplayUI.layout_bounds(viewport_width, viewport_height, x, y, width, height)
  return x >= 0 and y >= 0 and width >= 0 and height >= 0
    and x + width <= viewport_width and y + height <= viewport_height
end

return GameplayUI
