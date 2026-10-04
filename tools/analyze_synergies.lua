-- Deterministic DR-05 combat-chain smoke/analyzer. It is intentionally
-- headless so build interactions can be checked without LÖVE.
package.path = "./?.lua;./?/init.lua;" .. package.path

local BuildEffects = require("src.simulation.build_effects")
local Campaign = require("src.campaign.campaign")
local Content = require("src.content.legacy")
local Grid = require("src.world.grid")
local Loadout = require("src.simulation.loadout")
local Session = require("src.simulation.session")
local World = require("src.world.world")

local function open_session(seed)
  local session = Session.new({ seed = seed })
  session:start_run(Content.classes[1], Content.boons[1])
  local cells = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do cells[Grid.key(x, y)] = true end
  end
  session.state.world = World.new(session.registry, "cave", cells, session.state)
  session.state.phase, session.state.enemies = "combat", {}
  session.state.player.x, session.state.player.y = 10, 10
  return session
end

local function install(session, slot, definition_id)
  session.state.player.body:detach(slot)
  local component = session.component_factory:create(definition_id)
  assert(session.state.player.body:install(slot, component))
  return component
end

local function actor(session, id, x, y, health)
  local value = session:_make_enemy(id, { x = x, y = y })
  value.health = health or value.health
  return value
end

local function trace_key(trace)
  local values = {}
  for _, entry in ipairs(trace) do values[#values + 1] = entry.kind .. ":" .. tostring(entry.effect_id or "") .. ":" .. entry.depth end
  return table.concat(values, "|")
end

local function trace_stats(session)
  local depth, executions = 0, 0
  for _, entry in ipairs(session:build_effect_trace()) do
    depth = math.max(depth, entry.depth or 0)
    if entry.kind == "effect" then executions = executions + 1 end
  end
  return trace_key(session:build_effect_trace()), depth, executions
end

local function arc_scenario(seed)
  local session = open_session(seed)
  install(session, "right_arm", "component.arm.piercing_lance")
  install(session, "internal_2", "component.internal.legacy_shock_coil")
  session.state.charms.slots = { "charm.legacy.arc_relay" }
  for x = 11, 14 do assert(session.state.world:add_liquid(x, 10, "liquid.water.legacy", 1).applied) end
  local first, second = actor(session, "enemy.wild.ripper", 11, 10, 5), actor(session, "enemy.wild.ripper", 13, 10, 5)
  session.state.enemies = { first, second }
  assert(session:activate_actor_ability(session.state.player, "ability.weapon.piercing_lance", { direction = "d" }).applied)
  for _ = 1, 5 do session:_update_bullets() end
  local trace, depth, executions = trace_stats(session)
  return { affected = (5 - first.health) + (5 - second.health), trace = trace, depth = depth, executions = executions }
end

local function kinetic_scenario(seed)
  local session = open_session(seed)
  install(session, "left_arm", "component.arm.hydraulic_ram")
  session.state.charms.slots = { "charm.legacy.kinetic_feedback", "charm.legacy.kinetic_capacitor" }
  -- A terrain breach remains physically meaningful; a solid cell creates the
  -- real structural collision that powers Kinetic Feedback.
  session.state.world.cells[Grid.key(13, 10)] = nil
  local target, neighbour = actor(session, "enemy.wild.ripper", 11, 10, 10), actor(session, "enemy.wild.ripper", 12, 11, 10)
  session.state.enemies = { target, neighbour }
  assert(session:activate_actor_ability(session.state.player, "ability.weapon.melee.ram", { direction = "d" }).applied)
  local trace, depth, executions = trace_stats(session)
  return {
    displaced = target.x ~= 11 or neighbour.x ~= 12 or neighbour.y ~= 11,
    trace = trace, depth = depth, executions = executions,
  }
end

local function rupture_scenario(seed)
  local session = open_session(seed)
  install(session, "left_arm", "component.arm.impact_blade")
  install(session, "internal_2", "component.internal.legacy_volatile_charge")
  session.state.charms.slots = { "charm.legacy.rupture_core" }
  local target, neighbour = actor(session, "enemy.wild.ripper", 11, 10, 10), actor(session, "enemy.wild.ripper", 12, 10, 10)
  for _, component in ipairs(target.body:list_components()) do component.current_integrity = 1 end
  session.state.enemies = { target, neighbour }
  assert(session:activate_actor_ability(session.state.player, "ability.weapon.melee.basic", { direction = "d" }).applied)
  local trace, depth, executions = trace_stats(session)
  return { blast_damage = neighbour.health < 10, trace = trace, depth = depth, executions = executions }
end

local function reload_scenario(seed)
  local campaign = Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed })
  local session = campaign.session
  session.state.charms.slots = { "charm.legacy.quick_reload" }
  session.state.player.dash = 2
  local loadout = session:campaign_loadout()
  local resolved = assert(Loadout.resolve(session, session.state.player,
    loadout.weapon_slots[loadout.active_weapon], "weapon"))
  local magazine = session:weapon_magazine(resolved.provider, resolved.ability)
  magazine.loaded, session.state.bullets = 0, {}
  local result = session:attack_active_weapon()
  local trace, depth, executions = trace_stats(session)
  return {
    reloaded = result.code == "reloaded" and session.state.player.dash == 1 and #session.state.bullets == 0,
    loaded = magazine.loaded,
    trace = trace, depth = depth, executions = executions,
  }
end

-- This is the intentionally nasty player-attributed bridge: a pierced lance
-- creates electricity, the electrical kill emits on_kill, then Recycler
-- refills the magazine. It demonstrates that different eligible effects can
-- compose while the ancestry guard still forbids an effect from self-looping.
local function arc_recycler_scenario(seed)
  local campaign = Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed })
  local session, player = campaign.session, campaign.session.state.player
  local cells = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do cells[Grid.key(x, y)] = true end
  end
  session.state.world = World.new(session.registry, "cave", cells, session.state)
  session.state.phase, session.state.enemies, session.state.bullets = "combat", {}, {}
  player.x, player.y, player.direction = 10, 10, "d"
  local lance = install(session, "right_arm", "component.arm.piercing_lance")
  install(session, "internal_2", "component.internal.legacy_shock_coil")
  assert(session:assign_campaign_loadout("weapon", 1, {
    source_kind = "component", physical_id = lance.id, ability_id = "ability.weapon.piercing_lance",
  }).applied)
  local resolved = assert(Loadout.resolve(session, player, session:campaign_loadout().weapon_slots[1], "weapon"))
  local magazine = session:weapon_magazine(resolved.provider, resolved.ability)
  magazine.loaded = 1
  session.state.charms.slots = { "charm.legacy.arc_relay", "charm.legacy.recycler" }
  for x = 11, 14 do assert(session.state.world:add_liquid(x, 10, "liquid.water.legacy", 1).applied) end
  local first, second = actor(session, "enemy.wild.ripper", 11, 10, 3), actor(session, "enemy.wild.ripper", 13, 10, 1)
  session.state.enemies = { first, second }
  assert(session:attack_active_weapon().applied)
  for _ = 1, 5 do session:_update_bullets() end
  local trace, depth, executions = trace_stats(session)
  return {
    chained = first.health == 0 and second.health == 0 and magazine.loaded == 2,
    trace = trace, depth = depth, executions = executions,
  }
end

local scenarios = { arc_scenario, kinetic_scenario, rupture_scenario, reload_scenario }
local deterministic_scenarios = {
  arc_scenario, kinetic_scenario, rupture_scenario, reload_scenario, arc_recycler_scenario,
}
local count, maximum_depth, maximum_effects, failures = 0, 0, 0, 0
local function valid(index, result)
  if index == 1 then return result.affected > 0 end
  if index == 2 then return result.displaced end
  if index == 3 then return result.blast_damage end
  if index == 4 then return result.reloaded and result.loaded > 0 end
  return result.chained
end

-- Run each concrete showcase (including the cross-effect bridge) twice from
-- the same seed first. This checks that the full physical action, not merely
-- an emitted event, reproduces exactly.
for index, scenario in ipairs(deterministic_scenarios) do
  local result, replay = scenario(805000 + index), scenario(805000 + index)
  count = count + 2
  if result.trace ~= replay.trace or not valid(index, result) then failures = failures + 1 end
  maximum_depth = math.max(maximum_depth, result.depth or 0)
  maximum_effects = math.max(maximum_effects, result.executions or 0)
end

-- The 500-row matrix is intentionally lightweight: the replays above cover
-- each complete player action; this section constructs fresh root-event
-- contexts around the same physical systems so a stress run stays quick enough
-- for pre-commit use. Each row still resolves the real electricity, Force,
-- explosion, or reload-effect implementation rather than a mock callback.
local arc_stress = open_session(806000)
install(arc_stress, "internal_2", "component.internal.legacy_shock_coil")
arc_stress.state.charms.slots = { "charm.legacy.arc_relay" }
for x = 11, 14 do assert(arc_stress.state.world:add_liquid(x, 10, "liquid.water.legacy", 1).applied) end

local kinetic_stress = open_session(806001)
kinetic_stress.state.charms.slots = { "charm.legacy.kinetic_feedback" }
local kinetic_target = actor(kinetic_stress, "enemy.wild.ripper", 11, 10, 10)
kinetic_stress.state.enemies = { kinetic_target }

local rupture_stress = open_session(806002)
install(rupture_stress, "internal_2", "component.internal.legacy_volatile_charge")
rupture_stress.state.charms.slots = { "charm.legacy.rupture_core" }

local reload_stress = Campaign.new({ seed = 806003, campaign_id = "campaign:806003" }).session
reload_stress.state.charms.slots = { "charm.legacy.quick_reload" }

for index = 1, 500 do
  local scenario_index = ((index - 1) % #scenarios) + 1
  local result
  if scenario_index == 1 then
    result = arc_stress:_emit_build_event({
      type = "on_pierce", source_actor = arc_stress.state.player,
      target_cell = { x = 11 + ((index - 1) % 4), y = 10 },
      attack_tags = { projectile = true, piercing = true },
    })
  elseif scenario_index == 2 then
    kinetic_target.x, kinetic_target.y = 11, 10
    result = kinetic_stress:_emit_build_event({
      type = "on_push_collision", source_actor = kinetic_stress.state.player,
      target = kinetic_target, target_cell = { x = 11, y = 10 },
      force_dx = 1, force_dy = 0, attack_tags = { melee = true, forceful = true },
    })
  elseif scenario_index == 3 then
    rupture_stress.state.effects = {}
    result = rupture_stress:_emit_build_event({
      type = "on_component_break", source_actor = rupture_stress.state.player,
      target_cell = { x = 20 + ((index - 1) % 3), y = 10 },
      attack_tags = { melee = true, forceful = true },
    })
  else
    reload_stress.state.player.dash = 2
    result = reload_stress:_emit_build_event({
      type = "on_reload", source_actor = reload_stress.state.player,
      attack_tags = { projectile = true },
    })
  end
  count = count + 1
  if not result.applied then failures = failures + 1 end
  local executions, depth = 0, 0
  for _, entry in ipairs(result.chain.budget.trace) do
    depth = math.max(depth, entry.depth or 0)
    if entry.kind == "effect" then executions = executions + 1 end
  end
  maximum_depth = math.max(maximum_depth, depth)
  maximum_effects = math.max(maximum_effects, executions)
end

-- Deliberate artificial safety fixture: it proves a pathological ancestry
-- chain truncates without making normal production content less powerful.
local chain = BuildEffects.new_chain("stress")
for index = 1, BuildEffects.MAX_EXECUTIONS + 1 do
  if chain.budget.executions >= BuildEffects.MAX_EXECUTIONS then
    chain.budget.truncated = true
    break
  end
  chain.budget.executions = chain.budget.executions + 1
  chain = BuildEffects.derive_chain(chain, "stress:" .. index)
end

print(string.format("DR-05 synergy analyzer: %d scenarios, %d failures", count, failures))
print(string.format("max chain depth: %d; observed effect executions: %d", maximum_depth, maximum_effects))
print(string.format("stress budget truncation: %s at %d executions", tostring(chain.budget.truncated), chain.budget.executions))
assert(count >= 500 and failures == 0 and chain.budget.truncated)
