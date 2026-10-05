-- Derived run modifiers compose the secondary charm layer with the temporary
-- curse layer.  They never mutate base player statistics permanently.
local Modifiers = {}
local ExpeditionContent = require("src.expedition.content")

Modifiers.BASE_CHARM_SLOTS = 3

local function add_all(result, values)
  for key, value in pairs(values or {}) do result[key] = (result[key] or 0) + value end
end

function Modifiers.values(state, registry)
  local result = {}
  -- A run snapshots account research at creation.  Never consult the live
  -- profile here: a purchase during a run must affect only a later body.
  add_all(result, state.meta_snapshot and state.meta_snapshot.modifiers)
  local slots = state.charms and state.charms.slots or {}
  for index = 1, Modifiers.charm_slots(state) do
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
  -- Expedition passives are a separate run-local stack map.  They are
  -- intentionally not charm-slot limited and never touch Sandbox inventory.
  local expedition = state.expedition
  if expedition then
    add_all(result, expedition.base_modifiers)
    for passive_id, count in pairs(expedition.passive_stacks or {}) do
      local passive = ExpeditionContent.passive(passive_id)
      if passive and count > 0 then
        for key, value in pairs(passive.modifiers or {}) do
          result[key] = (result[key] or 0) + value * count
        end
      end
    end
  end
  return result
end

function Modifiers.charm_slots(state)
  local meta = state and state.meta_snapshot and state.meta_snapshot.modifiers or {}
  return math.max(0, Modifiers.BASE_CHARM_SLOTS + (meta.charm_slots or 0))
end

function Modifiers.value(state, registry, key)
  return Modifiers.values(state, registry)[key] or 0
end

return Modifiers
