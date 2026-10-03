-- Campaign quick-loadout ownership.  This deliberately stays small: a slot
-- records a stable physical provider plus an authored ability.  A future
-- carried tool can use another source_kind without changing slot semantics.
local Component = require("src.body.component")

local Loadout = { SLOT_COUNT = 2 }

local function copy_binding(binding)
  if not binding then return nil end
  return {
    source_kind = binding.source_kind,
    physical_id = binding.physical_id,
    ability_id = binding.ability_id,
  }
end

local function empty_slots()
  return { nil, nil }
end

function Loadout.is_weapon(ability)
  return ability and ability.loadout_kind == "weapon"
end

function Loadout.is_ability(ability)
  return ability and ability.loadout_kind == "ability"
end

function Loadout.new_for(session, actor)
  local loadout = {
    body_actor_id = actor and actor.actor_id or nil,
    weapon_slots = empty_slots(),
    ability_slots = empty_slots(),
    active_weapon = 1,
    active_ability = 1,
  }
  if not actor or not actor.body then return loadout end

  local weapon_index, ability_index = 1, 1
  -- Slot order is body topology order, then declarative ability order. This
  -- is stable across Lua versions and does not depend on hash iteration.
  for _, installed in ipairs(actor.body:list_installed_slots()) do
    local definition = session.registry:get_component(installed.component.definition_id)
    for _, ability_id in ipairs(definition.abilities or {}) do
      local ability = session.registry:get_ability(ability_id)
      if Component.is_functional(installed.component) and Loadout.is_weapon(ability) and weapon_index <= Loadout.SLOT_COUNT then
        loadout.weapon_slots[weapon_index] = {
          source_kind = "component", physical_id = installed.component.id, ability_id = ability_id,
        }
        weapon_index = weapon_index + 1
      elseif Component.is_functional(installed.component) and Loadout.is_ability(ability) and ability_index <= Loadout.SLOT_COUNT then
        loadout.ability_slots[ability_index] = {
          source_kind = "component", physical_id = installed.component.id, ability_id = ability_id,
        }
        ability_index = ability_index + 1
      end
    end
  end
  -- Dash is actor-owned rather than body-component-owned, but travels through
  -- exactly the same quick ability control path.
  if ability_index <= Loadout.SLOT_COUNT then
    loadout.ability_slots[ability_index] = { source_kind = "actor", ability_id = "ability.mobility.dash" }
  end
  return loadout
end

function Loadout.to_data(loadout)
  loadout = loadout or {}
  local data = {
    body_actor_id = loadout.body_actor_id,
    weapon_slots = empty_slots(), ability_slots = empty_slots(),
    active_weapon = loadout.active_weapon or 1,
    active_ability = loadout.active_ability or 1,
    ammo_migration_actor_id = loadout.ammo_migration_actor_id,
  }
  for index = 1, Loadout.SLOT_COUNT do
    data.weapon_slots[index] = copy_binding(loadout.weapon_slots and loadout.weapon_slots[index])
    data.ability_slots[index] = copy_binding(loadout.ability_slots and loadout.ability_slots[index])
  end
  return data
end

function Loadout.from_data(data)
  if type(data) ~= "table" then return nil end
  local result = {
    body_actor_id = type(data.body_actor_id) == "string" and data.body_actor_id or nil,
    weapon_slots = empty_slots(), ability_slots = empty_slots(),
    active_weapon = tonumber(data.active_weapon) == 2 and 2 or 1,
    active_ability = tonumber(data.active_ability) == 2 and 2 or 1,
    ammo_migration_actor_id = type(data.ammo_migration_actor_id) == "string" and data.ammo_migration_actor_id or nil,
  }
  local function parse(binding)
    if binding == nil then return nil end
    if type(binding) ~= "table" or type(binding.source_kind) ~= "string" or type(binding.ability_id) ~= "string" then
      return nil
    end
    if binding.source_kind == "component" and (type(binding.physical_id) ~= "string" or binding.physical_id == "") then return nil end
    if binding.source_kind ~= "component" and binding.source_kind ~= "actor" then return nil end
    return copy_binding(binding)
  end
  for index = 1, Loadout.SLOT_COUNT do
    result.weapon_slots[index] = parse(data.weapon_slots and data.weapon_slots[index])
    result.ability_slots[index] = parse(data.ability_slots and data.ability_slots[index])
  end
  return result
end

function Loadout.resolve(session, actor, binding, kind)
  if not binding then return nil, { code = "slot_empty", reason = "Quick slot is empty" } end
  local ability = session.registry.abilities[binding.ability_id]
  if not ability or (kind == "weapon" and not Loadout.is_weapon(ability))
    or (kind == "ability" and not Loadout.is_ability(ability)) then
    return nil, { code = "slot_unavailable", reason = "Quick slot is unavailable" }
  end
  if binding.source_kind == "actor" then
    if ability.implementation == "dash" then return { ability = ability, binding = binding, provider = nil } end
    return nil, { code = "slot_unavailable", reason = "Actor quick ability is unavailable" }
  end
  if not actor or not actor.body then return nil, { code = "slot_unavailable", reason = "No active body" } end
  local installed = actor.body:find_component(binding.physical_id)
  if not installed then return nil, { code = "provider_missing", reason = "Assigned provider is not installed" } end
  local component = installed.component
  local definition = session.registry:get_component(component.definition_id)
  local supplies = false
  for _, ability_id in ipairs(definition.abilities or {}) do
    if ability_id == ability.id then supplies = true; break end
  end
  if not supplies then return nil, { code = "slot_unavailable", reason = "Assigned provider no longer supplies that action" } end
  if not Component.is_functional(component) then return nil, { code = "provider_broken", reason = "Assigned provider is broken" } end
  return { ability = ability, binding = binding, provider = component, slot_id = installed.slot_id }
end

function Loadout.candidates(session, actor, kind)
  local result = {}
  if actor and actor.body then
    for _, installed in ipairs(actor.body:list_installed_slots()) do
      local definition = session.registry:get_component(installed.component.definition_id)
      for _, ability_id in ipairs(definition.abilities or {}) do
        local ability = session.registry:get_ability(ability_id)
        if (kind == "weapon" and Loadout.is_weapon(ability)) or (kind == "ability" and Loadout.is_ability(ability)) then
          result[#result + 1] = {
            source_kind = "component", physical_id = installed.component.id, ability_id = ability_id,
            display_name = ability.display_name, provider_name = definition.display_name,
            available = Component.is_functional(installed.component),
            condition = Component.condition(installed.component),
          }
        end
      end
    end
  end
  if kind == "ability" then
    result[#result + 1] = {
      source_kind = "actor", ability_id = "ability.mobility.dash", display_name = "Dash",
      provider_name = "Body movement", available = true, condition = "ready",
    }
  end
  return result
end

return Loadout
