-- Small, deterministic build-effect vocabulary.  This is deliberately not a
-- general event bus: Session owns action order and physical consequences;
-- this module only describes which equipped charm effects are eligible.
local BuildEffects = {
  MAX_CHAIN_DEPTH = 6,
  MAX_EXECUTIONS = 128,
}
local RunModifiers = require("src.simulation.run_modifiers")
local ExpeditionModifiers = require("src.expedition.modifiers")

BuildEffects.TRIGGERS = {
  on_attack = true,
  on_hit = true,
  on_kill = true,
  on_component_break = true,
  on_pierce = true,
  on_push_collision = true,
  on_reload = true,
  on_terrain_break = true,
}

BuildEffects.EFFECT_KINDS = {
  chain_electricity = true,
  kinetic_burst = true,
  small_explosion = true,
  magazine_refund = true,
  cooldown_reduction = true,
  ignite = true,
}

BuildEffects.ATTACK_TAGS = {
  melee = true,
  projectile = true,
  scatter = true,
  piercing = true,
  electric = true,
  explosive = true,
  fire = true,
  forceful = true,
  tool = true,
}

local IMPLEMENTATION_TAGS = {
  projectile = { "projectile" },
  scattershot = { "projectile", "scatter" },
  piercing_projectile = { "projectile", "piercing" },
  electrical_discharge = { "electric" },
  self_destruct = { "explosive", "fire", "forceful" },
  melee = { "melee", "forceful" },
}

local function copy_set(values)
  local result = {}
  for key, value in pairs(values or {}) do result[key] = value end
  return result
end

function BuildEffects.tags_for_ability(ability)
  local tags = {}
  for _, tag in ipairs((ability and ability.attack_tags) or IMPLEMENTATION_TAGS[ability and ability.implementation] or {}) do
    tags[tag] = true
  end
  return tags
end

function BuildEffects.tags_for_tool()
  return { tool = true, melee = true, forceful = true }
end

function BuildEffects.new_chain(root_label)
  return {
    root_label = root_label or "action",
    depth = 0,
    ancestry = {},
    trace_parent_id = nil,
    budget = { executions = 0, trace = {}, trace_sequence = 0, truncated = false },
  }
end

function BuildEffects.derive_chain(chain, effect_key, trace_parent_id)
  local next_chain = {
    root_label = chain.root_label,
    depth = chain.depth + 1,
    ancestry = copy_set(chain.ancestry),
    budget = chain.budget,
    trace_parent_id = trace_parent_id or chain.trace_parent_id,
  }
  next_chain.ancestry[effect_key] = true
  return next_chain
end

local function has_capability(session, actor, capability)
  if not capability then return true end
  return actor and session:actor_has_capability(actor, capability)
end

function BuildEffects.effect_status(session, actor, effect, event)
  local conditions = effect.conditions or {}
  if conditions.requires_capability and not has_capability(session, actor, conditions.requires_capability) then
    return false, "NO " .. session.registry:get_ability(conditions.requires_capability).display_name:upper()
  end
  local tag = conditions.attack_tag
  if tag and event and not (event.attack_tags or {})[tag] then
    return false, "REQUIRES " .. tag:upper() .. " ATTACK"
  end
  if conditions.requires_ranged and event and not (event.attack_tags or {}).projectile then
    return false, "REQUIRES RANGED ATTACK"
  end
  return true, "ACTIVE"
end

-- Equipped charm effects are already an ordered physical build state: slot
-- position is meaningful to players, while charm/effect IDs make the result
-- stable across save/load and table insertion order.
function BuildEffects.resolve(session, actor, event)
  if actor ~= session.state.player then return {} end
  local slots = session.state.charms and session.state.charms.slots or {}
  local results = {}
  for slot_index = 1, RunModifiers.charm_slots(session.state) do
    local charm_id = slots[slot_index]
    local charm = charm_id and session.registry.charms[charm_id] or nil
    for effect_index, effect in ipairs(charm and charm.reactive_effects or {}) do
      if effect.trigger == event.type then
        local active, reason = BuildEffects.effect_status(session, actor, effect, event)
        results[#results + 1] = {
          charm_id = charm_id,
          charm = charm,
          slot_index = slot_index,
          effect_index = effect_index,
          effect = effect,
          -- Slot identity keeps separately equipped duplicate charms
          -- independently meaningful while the ancestry guard still blocks a
          -- particular charm effect from recursively causing itself again.
          key = charm_id .. ":" .. slot_index .. ":" .. effect.id,
          active = active,
          reason = reason,
        }
      end
    end
  end
  local expedition = session.state.expedition
  if expedition then
    for _, entry in ipairs(ExpeditionModifiers.resolve_hooks(session, actor, event, session.modifier_registry)) do results[#results + 1] = entry end
  end
  table.sort(results, function(left, right)
    if left.charm_id ~= right.charm_id then return left.charm_id < right.charm_id end
    if left.slot_index ~= right.slot_index then return left.slot_index < right.slot_index end
    return left.effect.id < right.effect.id
  end)
  return results
end

function BuildEffects.describe(session, actor)
  local slots = session.state.charms and session.state.charms.slots or {}
  local result = {}
  for slot_index = 1, RunModifiers.charm_slots(session.state) do
    local charm_id = slots[slot_index]
    local charm = charm_id and session.registry.charms[charm_id] or nil
    for _, effect in ipairs(charm and charm.reactive_effects or {}) do
      local active, reason = BuildEffects.effect_status(session, actor, effect, nil)
      result[#result + 1] = {
        charm_id = charm_id,
        charm_name = charm.display_name,
        effect_id = effect.id,
        description = effect.description,
        active = active,
        reason = reason,
      }
    end
  end
  local expedition = session.state.expedition
  if expedition then
    local registry = session.modifier_registry or ExpeditionModifiers.default({ registry = session.registry })
    for _, definition in ipairs(registry.ordered) do
      local count = expedition.passive_stacks[definition.id] or 0
      if count > 0 then
        for hook_index, hook in ipairs(definition.hooks or {}) do
          result[#result + 1] = {
            charm_id = definition.id, charm_name = definition.name, effect_id = hook.trigger .. ":" .. hook_index,
            description = definition.description, active = true, reason = "ACTIVE", stack_count = count,
          }
        end
      end
    end
  end
  table.sort(result, function(left, right)
    if left.charm_name ~= right.charm_name then return left.charm_name < right.charm_name end
    return left.effect_id < right.effect_id
  end)
  return result
end

return BuildEffects
