-- Derived run modifiers compose the secondary charm layer with the temporary
-- curse layer.  They never mutate base player statistics permanently.
local Modifiers = {}

Modifiers.CHARM_SLOTS = 3

local function add_all(result, values)
  for key, value in pairs(values or {}) do result[key] = (result[key] or 0) + value end
end

function Modifiers.values(state, registry)
  local result = {}
  local slots = state.charms and state.charms.slots or {}
  for index = 1, Modifiers.CHARM_SLOTS do
    local charm_id = slots[index]
    if charm_id then
      local charm = registry:get_charm(charm_id)
      for _, boon_id in ipairs(charm.granted_boon_ids) do add_all(result, registry:get_boon(boon_id).modifiers) end
    end
  end
  if state.curse_id then
    add_all(result, registry:get_curse(state.curse_id).modifiers)
  elseif state.curse then
    -- Pre-8B saves retain their legacy curse record for the remainder of the
    -- saved floor rather than being silently stripped on restoration.
    add_all(result, state.curse.modifiers)
  end
  -- Legacy active saves may retain their former selected boon/class exactly
  -- until that run ends. New runs never set these compatibility fields.
  add_all(result, state.legacy_class and state.legacy_class.modifiers)
  add_all(result, state.legacy_boon and state.legacy_boon.modifiers)
  return result
end

function Modifiers.value(state, registry, key)
  return Modifiers.values(state, registry)[key] or 0
end

return Modifiers
