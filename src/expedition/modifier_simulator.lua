-- Controlled headless modifier resolver used by Studio and tests.  It reuses
-- the production definition parser, condition runtime and effect compiler;
-- only actor/world fixtures are synthetic.
local Definitions = require("src.expedition.modifiers")

local Simulator = {}

local function trace_node(nodes, parent_id, kind, value)
  local node = { id = #nodes + 1, parent_id = parent_id, kind = kind }
  for key, child in pairs(value or {}) do node[key] = child end
  nodes[#nodes + 1] = node
  return node
end

function Simulator.run(config)
  config = config or {}
  local definition = assert(config.definition, "Simulator requires modifier definition")
  local registry = assert(config.modifier_registry, "Simulator requires a modifier registry")
  local stack_count = math.max(1, math.floor(config.stack_count or 1))
  local actor = { kind = "player", health = config.hp or 10, max_health = config.max_hp or 10 }
  local session = {
    state = { player = actor, expedition = { character_id = config.character_id or "expedition.gunner", passive_stacks = { [definition.id] = stack_count } } },
    modifier_registry = registry,
    actor_has_capability = function(_, _, capability)
      return (config.capabilities or {})[capability] == true
    end,
  }
  local event = {
    type = config.trigger or "on_hit", source_actor = actor, target = config.target and { kind = "enemy", health = config.target_hp or 5, max_health = config.target_hp or 5 } or nil,
    attack_tags = config.attack_tags or { projectile = true }, damage_type = config.damage_type,
  }
  local nodes = { trace_node({}, nil, "action", { label = config.label or "SIMULATED ACTION" }) }
  local root = nodes[1]
  local trigger = trace_node(nodes, root.id, "trigger", { event = event.type })
  local resolved = Definitions.resolve_hooks(session, actor, event, registry)
  local effects = {}
  for _, entry in ipairs(resolved) do
    if entry.active then
      local modifier = trace_node(nodes, trigger.id, "modifier", { modifier_id = entry.source_id, modifier_name = entry.source_name, stacks = entry.passive_count })
      local effect = trace_node(nodes, modifier.id, "effect", { effect = entry.effect.id, value = entry.effect.effect })
      effects[#effects + 1] = { modifier_id = entry.source_id, effect = entry.effect.effect, trace_id = effect.id }
    end
  end
  return {
    state = { target_hp = event.target and event.target.health or nil, magazine_delta = 0, cooldown_delta = 0 },
    trace = { root_action_id = config.label or "simulated_action", nodes = nodes },
    effects = effects, chain_depth = 1, effect_executions = #effects, truncated = false,
  }
end

return Simulator
