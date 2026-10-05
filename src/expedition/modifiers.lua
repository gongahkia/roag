-- Serialized Expedition modifier runtime.  Definitions are strict data; this
-- module owns validation, deterministic compilation and stack arithmetic.
-- Combat consequences remain in Session's reusable effect primitives.
local Json = require("src.persistence.json")
local Writer = require("src.rooms.json_writer")

local Modifiers = {
  SCHEMA_VERSION = 1,
  MANIFEST_FORMAT = "roag.expedition_modifier_manifest",
  MANIFEST_VERSION = 1,
  DIRECTORY = "content/expedition/modifiers",
}

local VALID_STATS = {
  max_health = true, dash_cooldown = true, bomb_radius = true,
  melee_damage = true, melee_force = true, projectile_damage = true,
  projectile_count = true, scatter_pellets = true, projectile_pierce = true,
  projectile_range = true, magazine_capacity = true, ammo_on_kill = true,
  clear_heal = true,
}

Modifiers.TAGS = {
  projectile = true, melee = true, explosive = true, electric = true,
  force = true, sustain = true, mobility = true, defense = true, geometry = true,
  damage = true, chain = true, utility = true, dash = true, cooldown = true,
  ammo = true, ranged = true, kill = true, heal = true, clear = true,
  health = true, collision = true, range = true, magazine = true, piercing = true,
  reload = true, component = true, scatter = true, terrain = true, fire = true,
}

Modifiers.CATEGORIES = {
  damage = true, geometry = true, electrical = true, electric = true, force = true, kinetic = true,
  explosive = true, sustain = true, mobility = true, defense = true, utility = true,
}

-- Metadata is deliberately shared by validation and authoring tools.  No
-- second UI-only vocabulary is allowed to drift from the actual runtime.
Modifiers.TRIGGERS = {
  on_attack = { id = "on_attack", display_name = "Attack", description = "A player attack begins.", fields = { "source_actor", "weapon_ability", "attack_tags" } },
  on_hit = { id = "on_hit", display_name = "Hit", description = "An actor takes a player-caused hit.", fields = { "source_actor", "target", "attack_tags", "damage_type" } },
  on_kill = { id = "on_kill", display_name = "Kill", description = "A player-caused hit kills an actor.", fields = { "source_actor", "target", "attack_tags" } },
  on_component_break = { id = "on_component_break", display_name = "Component Break", description = "A target body component becomes broken.", fields = { "source_actor", "target", "component", "attack_tags" } },
  on_pierce = { id = "on_pierce", display_name = "Projectile Pierce", description = "A projectile continues through an actor.", fields = { "source_actor", "target", "attack_tags" } },
  on_push_collision = { id = "on_push_collision", display_name = "Push Collision", description = "A forced actor collides with solid geometry.", fields = { "source_actor", "target", "target_cell", "attack_tags" } },
  on_reload = { id = "on_reload", display_name = "Reload", description = "An authoritative reload succeeds.", fields = { "source_actor", "weapon_ability", "attack_tags" } },
  on_terrain_break = { id = "on_terrain_break", display_name = "Terrain Break", description = "Player-caused terrain or object destruction completes.", fields = { "source_actor", "target_cell", "attack_tags" } },
}

Modifiers.CONDITIONS = {
  always = { id = "always", display_name = "Always", description = "Always eligible.", fields = {} },
  attack_has_tag = { id = "attack_has_tag", display_name = "Attack Has Tag", description = "Requires an attack tag.", fields = { { name = "tag", type = "attack_tag", required = true } } },
  attack_lacks_tag = { id = "attack_lacks_tag", display_name = "Attack Lacks Tag", description = "Requires no matching attack tag.", fields = { { name = "tag", type = "attack_tag", required = true } } },
  has_capability = { id = "has_capability", display_name = "Has Capability", description = "Requires a functional body capability.", fields = { { name = "capability", type = "ability_id", required = true } } },
  lacks_capability = { id = "lacks_capability", display_name = "Lacks Capability", description = "Requires a missing body capability.", fields = { { name = "capability", type = "ability_id", required = true } } },
  weapon_is_ranged = { id = "weapon_is_ranged", display_name = "Weapon Is Ranged", description = "Requires a projectile attack.", fields = {} },
  weapon_is_melee = { id = "weapon_is_melee", display_name = "Weapon Is Melee", description = "Requires a melee attack.", fields = {} },
  magazine_empty = { id = "magazine_empty", display_name = "Magazine Empty", description = "Requires an empty active magazine.", fields = {} },
  magazine_not_empty = { id = "magazine_not_empty", display_name = "Magazine Not Empty", description = "Requires a loaded active magazine.", fields = {} },
  stack_count_at_least = { id = "stack_count_at_least", display_name = "Stack Count At Least", description = "Requires this modifier's stack count.", fields = { { name = "count", type = "integer", min = 1, required = true } } },
  source_is_character = { id = "source_is_character", display_name = "Source Is Character", description = "Requires a named Expedition character.", fields = { { name = "character", type = "character_id", required = true } } },
  source_hp_below_fraction = { id = "source_hp_below_fraction", display_name = "Source HP Below", description = "Requires HP below a fraction.", fields = { { name = "fraction", type = "number", min = 0, max = 1, required = true } } },
  target_component_broken = { id = "target_component_broken", display_name = "Target Component Broken", description = "Requires the event component to be broken.", fields = {} },
  event_damage_type = { id = "event_damage_type", display_name = "Damage Type", description = "Requires an event damage type.", fields = { { name = "damage_type", type = "string", required = true } } },
}

Modifiers.EFFECTS = {
  modify_stat = { id = "modify_stat", display_name = "Modify Stat", group = "static", description = "Adds a resolved value to a run stat.", fields = { { name = "stat", type = "stat", required = true }, { name = "value", type = "stack_expression", required = true } } },
  chain_electricity = { id = "chain_electricity", display_name = "Chain Electricity", group = "reactive", description = "Uses the authoritative electrical propagation system.", fields = { { name = "max_cells", type = "stack_expression", required = true }, { name = "damage", type = "stack_expression", required = true } } },
  kinetic_burst = { id = "kinetic_burst", display_name = "Force Burst", group = "reactive", description = "Uses the authoritative Force collision system.", fields = { { name = "radius", type = "stack_expression", required = true }, { name = "force", type = "stack_expression", required = true } } },
  small_explosion = { id = "small_explosion", display_name = "Explosion", group = "reactive", description = "Uses the ordinary explosion, terrain and Force rules.", fields = { { name = "radius", type = "stack_expression", required = true }, { name = "damage", type = "stack_expression", required = true } } },
  magazine_refund = { id = "magazine_refund", display_name = "Refund Magazine", group = "reactive", description = "Loads rounds into the active compatible magazine.", fields = { { name = "amount", type = "stack_expression", required = true } } },
  cooldown_reduction = { id = "cooldown_reduction", display_name = "Reduce Cooldown", group = "reactive", description = "Reduces the active ability cooldown to its safe floor.", fields = { { name = "amount", type = "stack_expression", required = true } } },
  ignite = { id = "ignite", display_name = "Ignite", group = "reactive", description = "Uses ordinary fire propagation.", fields = { { name = "radius", type = "stack_expression", required = true } } },
}

Modifiers.STACK_EXPRESSIONS = {
  constant = { id = "constant", display_name = "Constant", fields = { { name = "value", type = "number", required = true } } },
  linear = { id = "linear", display_name = "Linear", fields = { { name = "base", type = "number", required = true }, { name = "per_stack", type = "number", required = true } } },
  geometric = { id = "geometric", display_name = "Geometric", fields = { { name = "base", type = "number", required = true }, { name = "multiplier_per_stack", type = "number", required = true } } },
  every_n = { id = "every_n", display_name = "Every N Stacks", fields = { { name = "base", type = "number", required = true }, { name = "per_step", type = "number", required = true }, { name = "n", type = "integer", min = 1, required = true } } },
  thresholds = { id = "thresholds", display_name = "Thresholds", fields = { { name = "values", type = "threshold_table", required = true } } },
}

local function finite(value) return type(value) == "number" and value == value and value ~= math.huge and value ~= -math.huge end
local function list_is_array(value)
  if type(value) ~= "table" then return false end
  for key in pairs(value) do if type(key) ~= "number" or key < 1 or key % 1 ~= 0 or key > #value then return false end end
  return true
end
local function copy(value)
  if type(value) ~= "table" then return value end
  local result = {}
  for key, child in pairs(value) do result[key] = copy(child) end
  return result
end
local function sorted_keys(values)
  local keys = {}; for key in pairs(values or {}) do keys[#keys + 1] = key end; table.sort(keys); return keys
end
local function failure(file, path, reason, code)
  return nil, { code = code or "invalid_modifier", file = file or "<memory>", path = path or "$", reason = reason, message = (file or "<memory>") .. ": " .. (path or "$") .. ": " .. reason }
end
local function expression_number(expression, field, file, path)
  if not finite(expression[field]) then return failure(file, path .. "." .. field, "must be a finite number", "invalid_expression") end
  return true
end

function Modifiers.evaluate_expression(expression, raw_stacks)
  local stacks = math.max(0, math.floor(tonumber(raw_stacks) or 0))
  if stacks == 0 then return 0 end
  local value
  if expression.kind == "constant" then value = expression.value
  elseif expression.kind == "linear" then value = expression.base + expression.per_stack * (stacks - 1)
  elseif expression.kind == "geometric" then value = expression.base * expression.multiplier_per_stack ^ (stacks - 1)
  elseif expression.kind == "every_n" then value = expression.base + expression.per_step * math.floor((stacks - 1) / expression.n)
  elseif expression.kind == "thresholds" then
    value = 0
    for _, threshold in ipairs(expression.values) do if stacks >= threshold.stacks then value = threshold.value else break end end
  else return nil, "unknown stack expression kind '" .. tostring(expression.kind) .. "'" end
  if expression.min ~= nil then value = math.max(value, expression.min) end
  if expression.max ~= nil then value = math.min(value, expression.max) end
  return value
end

local function validate_expression(expression, file, path)
  if type(expression) ~= "table" then return failure(file, path, "must be a stack expression object", "invalid_expression") end
  local metadata = Modifiers.STACK_EXPRESSIONS[expression.kind]
  if not metadata then return failure(file, path .. ".kind", "unknown stack expression kind '" .. tostring(expression.kind) .. "'", "invalid_expression") end
  if expression.kind == "constant" then
    local ok, data = expression_number(expression, "value", file, path); if not ok then return nil, data end
  elseif expression.kind == "linear" then
    local ok, data = expression_number(expression, "base", file, path); if not ok then return nil, data end
    ok, data = expression_number(expression, "per_stack", file, path); if not ok then return nil, data end
  elseif expression.kind == "geometric" then
    local ok, data = expression_number(expression, "base", file, path); if not ok then return nil, data end
    ok, data = expression_number(expression, "multiplier_per_stack", file, path); if not ok then return nil, data end
    if expression.multiplier_per_stack < 0 then return failure(file, path .. ".multiplier_per_stack", "must be non-negative", "invalid_expression") end
  elseif expression.kind == "every_n" then
    local ok, data = expression_number(expression, "base", file, path); if not ok then return nil, data end
    ok, data = expression_number(expression, "per_step", file, path); if not ok then return nil, data end
    if type(expression.n) ~= "number" or expression.n < 1 or expression.n % 1 ~= 0 then return failure(file, path .. ".n", "must be a positive integer", "invalid_expression") end
  elseif expression.kind == "thresholds" then
    if not list_is_array(expression.values) or #expression.values == 0 then return failure(file, path .. ".values", "must be a non-empty threshold array", "invalid_expression") end
    local previous = 0
    for index, threshold in ipairs(expression.values) do
      if type(threshold) ~= "table" or type(threshold.stacks) ~= "number" or threshold.stacks < 1 or threshold.stacks % 1 ~= 0 then return failure(file, path .. ".values[" .. index .. "].stacks", "must be a positive integer", "invalid_expression") end
      if threshold.stacks <= previous then return failure(file, path .. ".values[" .. index .. "].stacks", "must be strictly ascending", "invalid_expression") end
      if not finite(threshold.value) then return failure(file, path .. ".values[" .. index .. "].value", "must be a finite number", "invalid_expression") end
      previous = threshold.stacks
    end
  end
  for _, bound in ipairs({ "min", "max" }) do if expression[bound] ~= nil and not finite(expression[bound]) then return failure(file, path .. "." .. bound, "must be a finite number", "invalid_expression") end end
  if expression.min ~= nil and expression.max ~= nil and expression.min > expression.max then return failure(file, path, "min cannot exceed max", "invalid_expression") end
  local value, reason = Modifiers.evaluate_expression(expression, 1)
  if not finite(value) then return failure(file, path, reason or "produces a non-finite value", "invalid_expression") end
  return true
end

local function validate_condition(condition, registry, file, path)
  if type(condition) ~= "table" or type(condition.kind) ~= "string" then return failure(file, path, "must have a condition kind", "invalid_condition") end
  if not Modifiers.CONDITIONS[condition.kind] then return failure(file, path .. ".kind", "unknown condition kind '" .. tostring(condition.kind) .. "'", "invalid_condition") end
  if condition.kind == "attack_has_tag" or condition.kind == "attack_lacks_tag" then
    if not Modifiers.TAGS[condition.tag] then return failure(file, path .. ".tag", "unknown attack tag '" .. tostring(condition.tag) .. "'", "invalid_condition") end
  elseif condition.kind == "has_capability" or condition.kind == "lacks_capability" then
    if type(condition.capability) ~= "string" or (registry and registry.abilities and not registry.abilities[condition.capability]) then return failure(file, path .. ".capability", "unknown capability '" .. tostring(condition.capability) .. "'", "invalid_condition") end
  elseif condition.kind == "stack_count_at_least" then
    if type(condition.count) ~= "number" or condition.count < 1 or condition.count % 1 ~= 0 then return failure(file, path .. ".count", "must be a positive integer", "invalid_condition") end
  elseif condition.kind == "source_hp_below_fraction" then
    if not finite(condition.fraction) or condition.fraction < 0 or condition.fraction > 1 then return failure(file, path .. ".fraction", "must be between 0 and 1", "invalid_condition") end
  elseif condition.kind == "source_is_character" and type(condition.character) ~= "string" then return failure(file, path .. ".character", "must be a character ID", "invalid_condition")
  elseif condition.kind == "event_damage_type" and type(condition.damage_type) ~= "string" then return failure(file, path .. ".damage_type", "must be a string", "invalid_condition") end
  return true
end

local function validate_effect(effect, file, path)
  if type(effect) ~= "table" or type(effect.kind) ~= "string" then return failure(file, path, "must have an effect kind", "invalid_effect") end
  local metadata = Modifiers.EFFECTS[effect.kind]
  if not metadata then return failure(file, path .. ".kind", "unknown effect kind '" .. tostring(effect.kind) .. "'", "invalid_effect") end
  for _, field in ipairs(metadata.fields) do
    if field.type == "stack_expression" then
      local ok, data = validate_expression(effect[field.name], file, path .. "." .. field.name); if not ok then return nil, data end
    elseif field.type == "stat" then
      if not VALID_STATS[effect[field.name]] then return failure(file, path .. "." .. field.name, "unknown stat '" .. tostring(effect[field.name]) .. "'", "invalid_effect") end
    end
  end
  return true
end

function Modifiers.validate(definition, options)
  options = options or {}
  local file = options.file or "<memory>"
  if type(definition) ~= "table" then return failure(file, "$", "definition must be an object") end
  if definition.schema_version ~= Modifiers.SCHEMA_VERSION then return failure(file, "schema_version", "unsupported schema version '" .. tostring(definition.schema_version) .. "'", "unsupported_schema") end
  if type(definition.id) ~= "string" or not definition.id:match("^expedition%.passive%.[a-z0-9_]+$") then return failure(file, "id", "must be a stable expedition.passive.* ID") end
  if type(definition.name) ~= "string" or definition.name == "" then return failure(file, "name", "must be a non-empty string") end
  if type(definition.description) ~= "string" or definition.description == "" then return failure(file, "description", "must be a non-empty string") end
  if not Modifiers.CATEGORIES[definition.category] then return failure(file, "category", "unknown category '" .. tostring(definition.category) .. "'") end
  if not list_is_array(definition.tags or {}) then return failure(file, "tags", "must be an array") end
  for index, tag in ipairs(definition.tags or {}) do if not Modifiers.TAGS[tag] then return failure(file, "tags[" .. index .. "]", "unknown tag '" .. tostring(tag) .. "'") end end
  if type(definition.pool) ~= "table" or type(definition.pool.enabled) ~= "boolean" or not finite(definition.pool.weight) or definition.pool.weight < 0 then return failure(file, "pool", "requires enabled boolean and non-negative finite weight") end
  if definition.unlock ~= nil and (type(definition.unlock) ~= "table" or type(definition.unlock.id) ~= "string" or (definition.unlock.kind ~= "meta_unlock" and definition.unlock.kind ~= "always")) then return failure(file, "unlock", "must be an always or meta_unlock reference") end
  if definition.static_effects ~= nil and not list_is_array(definition.static_effects) then return failure(file, "static_effects", "must be an array") end
  if definition.hooks ~= nil and not list_is_array(definition.hooks) then return failure(file, "hooks", "must be an array") end
  if #(definition.static_effects or {}) + #(definition.hooks or {}) == 0 then return failure(file, "$", "requires a static effect or hook") end
  for index, effect in ipairs(definition.static_effects or {}) do
    if effect.kind ~= "modify_stat" then return failure(file, "static_effects[" .. index .. "].kind", "static effects must use modify_stat") end
    local ok, data = validate_effect(effect, file, "static_effects[" .. index .. "]"); if not ok then return nil, data end
  end
  for hook_index, hook in ipairs(definition.hooks or {}) do
    local base = "hooks[" .. hook_index .. "]"
    if type(hook) ~= "table" or not Modifiers.TRIGGERS[hook.trigger] then return failure(file, base .. ".trigger", "unknown trigger '" .. tostring(hook and hook.trigger) .. "'", "invalid_trigger") end
    if hook.conditions ~= nil and not list_is_array(hook.conditions) then return failure(file, base .. ".conditions", "must be an array") end
    if not list_is_array(hook.effects) or #hook.effects == 0 then return failure(file, base .. ".effects", "must be a non-empty array") end
    for condition_index, condition in ipairs(hook.conditions or {}) do local ok, data = validate_condition(condition, options.registry, file, base .. ".conditions[" .. condition_index .. "]"); if not ok then return nil, data end end
    for effect_index, effect in ipairs(hook.effects) do
      local ok, data = validate_effect(effect, file, base .. ".effects[" .. effect_index .. "]"); if not ok then return nil, data end
      if effect.kind == "modify_stat" then return failure(file, base .. ".effects[" .. effect_index .. "]", "modify_stat belongs in static_effects") end
    end
  end
  if definition.state_schema ~= nil then
    if type(definition.state_schema) ~= "table" then return failure(file, "state_schema", "must be an object") end
    for key, spec in pairs(definition.state_schema) do
      if type(key) ~= "string" or type(spec) ~= "table" or not ({ integer = true, number = true, boolean = true, enum = true })[spec.type] then return failure(file, "state_schema." .. tostring(key), "must be a bounded primitive state field") end
      if spec.type == "enum" and (not list_is_array(spec.values) or #spec.values == 0) then return failure(file, "state_schema." .. key .. ".values", "enum requires values") end
    end
  end
  return true
end

local function default_read(path)
  if love and love.filesystem and love.filesystem.getInfo and love.filesystem.getInfo(path) then return love.filesystem.read(path) end
  local handle = io.open(path, "rb"); if not handle then return nil, "missing file" end
  local text = handle:read("*a"); handle:close(); return text
end
local function safe_filename(filename) return type(filename) == "string" and filename:match("^[a-z0-9_%-]+%.json$") and filename ~= "manifest.json" end

function Modifiers.load(options)
  options = options or {}
  local directory, read = options.directory or Modifiers.DIRECTORY, options.read or default_read
  local function read_file(filename)
    if options.files then return options.files[filename], options.files[filename] and nil or "missing file" end
    return read(directory .. "/" .. filename)
  end
  local manifest_text, manifest_error = read_file("manifest.json")
  if not manifest_text then return failure(directory .. "/manifest.json", "$", tostring(manifest_error), "missing_manifest") end
  local manifest, decode_error = Json.decode(manifest_text)
  if not manifest then return failure(directory .. "/manifest.json", "$", tostring(decode_error), "invalid_json") end
  if manifest.format ~= Modifiers.MANIFEST_FORMAT or manifest.version ~= Modifiers.MANIFEST_VERSION or not list_is_array(manifest.files) then return failure(directory .. "/manifest.json", "$", "unsupported modifier manifest") end
  local definitions, ordered, filenames, seen_files = {}, {}, {}, {}
  for _, filename in ipairs(manifest.files) do
    if not safe_filename(filename) or seen_files[filename] then return failure(directory .. "/manifest.json", "files", "contains an invalid or duplicate filename", "invalid_manifest") end
    seen_files[filename] = true
    local text, read_error = read_file(filename)
    local file = directory .. "/" .. filename
    if not text then return failure(file, "$", tostring(read_error), "missing_file") end
    local definition, error_message = Json.decode(text)
    if not definition then return failure(file, "$", tostring(error_message), "invalid_json") end
    local ok, validation = Modifiers.validate(definition, { file = file, registry = options.registry })
    if not ok then return nil, validation end
    if definitions[definition.id] then return failure(file, "id", "duplicates '" .. definition.id .. "'", "duplicate_id") end
    -- Derived runtime/editor conveniences are never serialized back into the
    -- canonical content file.
    definition._source_file = filename
    definition.behavior_changing = #(definition.hooks or {}) > 0
    definitions[definition.id], filenames[definition.id] = definition, filename
    ordered[#ordered + 1] = definition
  end
  table.sort(ordered, function(a, b) return a.id < b.id end)
  return setmetatable({ definitions = definitions, ordered = ordered, filenames = filenames, directory = directory, manifest = manifest }, { __index = Modifiers.Registry })
end

Modifiers.Registry = {}
function Modifiers.Registry:get(id) return self.definitions[id] end
function Modifiers.Registry:list() local result = {}; for index, definition in ipairs(self.ordered) do result[index] = definition end; return result end
function Modifiers.Registry:filename_for(id) return self.filenames[id] end
function Modifiers.Registry:enabled(profile)
  local MetaProfile = require("src.persistence.meta_profile")
  local result = {}
  for _, definition in ipairs(self.ordered) do
    local unlock = definition.unlock
    if definition.pool.enabled and (not unlock or unlock.kind == "always" or MetaProfile.has_expedition_unlock(profile, unlock.id)) then result[#result + 1] = definition end
  end
  return result
end

function Modifiers.default(options)
  if not Modifiers._default or options then
    local registry = options and options.registry or nil
    local loaded, reason = Modifiers.load({ registry = registry })
    assert(loaded, reason and reason.message or "Could not load Expedition modifiers")
    if not options then Modifiers._default = loaded end
    return loaded
  end
  return Modifiers._default
end

function Modifiers.static_values(stacks, modifier_registry)
  local registry = modifier_registry or Modifiers.default()
  local values = {}
  for _, definition in ipairs(registry.ordered) do
    local count = (stacks or {})[definition.id] or 0
    if count > 0 then
      for _, effect in ipairs(definition.static_effects or {}) do
        values[effect.stat] = (values[effect.stat] or 0) + assert(Modifiers.evaluate_expression(effect.value, count))
      end
    end
  end
  return values
end

local function event_tags(event) return event and event.attack_tags or {} end
function Modifiers.condition_active(session, actor, event, condition, stacks)
  local tags = event_tags(event)
  if condition.kind == "always" then return true
  elseif condition.kind == "attack_has_tag" then return tags[condition.tag] == true
  elseif condition.kind == "attack_lacks_tag" then return not tags[condition.tag]
  elseif condition.kind == "has_capability" then return actor and session:actor_has_capability(actor, condition.capability)
  elseif condition.kind == "lacks_capability" then return not (actor and session:actor_has_capability(actor, condition.capability))
  elseif condition.kind == "weapon_is_ranged" then return tags.projectile == true
  elseif condition.kind == "weapon_is_melee" then return tags.melee == true
  elseif condition.kind == "stack_count_at_least" then return stacks >= condition.count
  elseif condition.kind == "source_is_character" then return session.state.expedition and session.state.expedition.character_id == condition.character
  elseif condition.kind == "source_hp_below_fraction" then return actor and actor.health / math.max(1, actor.max_health or actor.base_max_health or actor.health) < condition.fraction
  elseif condition.kind == "target_component_broken" then return event and event.component and event.component.integrity <= 0
  elseif condition.kind == "event_damage_type" then return event and event.damage_type == condition.damage_type
  elseif condition.kind == "magazine_empty" or condition.kind == "magazine_not_empty" then
    local resolved = session.expedition_active_weapon and session:expedition_active_weapon() or nil
    local ability = resolved and resolved.ability
    local magazine = resolved and ability and ability.ammo and session:weapon_magazine(resolved.provider, ability)
    return magazine and ((condition.kind == "magazine_empty" and magazine.loaded <= 0) or (condition.kind == "magazine_not_empty" and magazine.loaded > 0)) or false
  end
  return false
end

local function describe_conditions(session, actor, event, conditions, stacks)
  for _, condition in ipairs(conditions or {}) do if not Modifiers.condition_active(session, actor, event, condition, stacks) then return false, "INACTIVE" end end
  return true, "ACTIVE"
end
local function resolve_effect(effect, stacks)
  local result = { kind = effect.kind }
  for key, value in pairs(effect) do
    if type(value) == "table" and value.kind and Modifiers.STACK_EXPRESSIONS[value.kind] then result[key] = assert(Modifiers.evaluate_expression(value, stacks))
    elseif key ~= "kind" then result[key] = copy(value) end
  end
  return result
end

function Modifiers.resolve_hooks(session, actor, event, modifier_registry)
  if actor ~= session.state.player then return {} end
  local registry = modifier_registry or session.modifier_registry or Modifiers.default({ registry = session.registry })
  local stacks, results = (session.state.expedition and session.state.expedition.passive_stacks) or {}, {}
  for _, definition in ipairs(registry.ordered) do
    local count = stacks[definition.id] or 0
    if count > 0 then
      for hook_index, hook in ipairs(definition.hooks or {}) do
        if hook.trigger == event.type then
          local active, reason = describe_conditions(session, actor, event, hook.conditions, count)
          for effect_index, raw_effect in ipairs(hook.effects) do
            local effect = resolve_effect(raw_effect, count)
            results[#results + 1] = {
              charm_id = definition.id, source_id = definition.id, source_name = definition.name,
              passive_count = count, slot_index = 0, effect_index = effect_index,
              hook_index = hook_index, effect = { id = raw_effect.kind, effect = effect },
              key = "expedition:" .. definition.id .. ":" .. hook_index .. ":" .. effect_index,
              active = active, reason = reason, definition = definition,
            }
          end
        end
      end
    end
  end
  table.sort(results, function(a, b) return a.key < b.key end)
  return results
end

local function effect_summary(effect, stacks)
  local values = {}
  for key, value in pairs(effect) do if type(value) == "table" and value.kind and Modifiers.STACK_EXPRESSIONS[value.kind] then values[#values + 1] = key:upper() .. " " .. tostring(assert(Modifiers.evaluate_expression(value, stacks))) end end
  table.sort(values)
  return #values > 0 and table.concat(values, ", ") or effect.kind:upper()
end
function Modifiers.stack_preview(definition, stacks)
  local current, next_values = {}, {}
  for _, effect in ipairs(definition.static_effects or {}) do
    current[#current + 1] = effect.stat:upper() .. " " .. tostring(assert(Modifiers.evaluate_expression(effect.value, stacks)))
    next_values[#next_values + 1] = effect.stat:upper() .. " " .. tostring(assert(Modifiers.evaluate_expression(effect.value, stacks + 1)))
  end
  for _, hook in ipairs(definition.hooks or {}) do for _, effect in ipairs(hook.effects) do current[#current + 1] = effect_summary(effect, stacks); next_values[#next_values + 1] = effect_summary(effect, stacks + 1) end end
  return { current = table.concat(current, " • "), next = table.concat(next_values, " • ") }
end

function Modifiers.new_definition(id)
  return { schema_version = Modifiers.SCHEMA_VERSION, id = id or "expedition.passive.new_modifier", name = "NEW MODIFIER", description = "Describe the mechanical effect.", category = "utility", tags = {}, pool = { enabled = false, weight = 1 }, static_effects = {}, hooks = {} }
end
function Modifiers.canonical_json(definition)
  local data = copy(definition)
  data._source_file, data.behavior_changing = nil, nil
  return Writer.encode(data)
end
function Modifiers.round_trip(definition, options)
  local text, reason = Modifiers.canonical_json(definition); if not text then return nil, reason end
  local restored, decode_error = Json.decode(text); if not restored then return nil, decode_error end
  local ok, failure_data = Modifiers.validate(restored, options); if not ok then return nil, failure_data end
  return restored, text
end

return Modifiers
