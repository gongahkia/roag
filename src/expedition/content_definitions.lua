-- Versioned JSON authority for Expedition boards and encounter grammar.
-- This module purposefully validates authored content without absorbing the
-- deterministic combat/spawn algorithms that belong to ExpeditionRun.
local Json = require("src.persistence.json")
local Grid = require("src.world.grid")
local Vocabulary = require("src.expedition.vocabulary")

local Definitions = {}
Definitions.CHAMBER_SCHEMA_VERSION = 1
Definitions.ENCOUNTER_SCHEMA_VERSION = 1
Definitions.CHAMBER_ROOT = "content/expedition/chambers"
Definitions.ENCOUNTER_ROOT = "content/expedition/encounters"

local function failure(kind, file, message)
  return nil, { kind = kind, file = file, message = message }
end

local function array(value) return type(value) == "table" and #value > 0 end
local function integer(value) return type(value) == "number" and value % 1 == 0 end
local function string_array(value)
  if type(value) ~= "table" then return false end
  for _, item in ipairs(value) do if type(item) ~= "string" then return false end end
  return true
end
local function unique_strings(value)
  local seen = {}
  for _, item in ipairs(value or {}) do if seen[item] then return false end; seen[item] = true end
  return true
end
local function point_key(point) return tostring(point.x) .. ":" .. tostring(point.y) end
local function read_disk(path)
  if love and love.filesystem and love.filesystem.getInfo(path) then return love.filesystem.read(path) end
  local handle = io.open(path, "rb")
  if not handle then return nil end
  local data = handle:read("*a"); handle:close(); return data
end

local function source_read(options, kind, filename)
  if options and options.store then return options.store:read(kind, filename) end
  local root = kind == "chambers" and Definitions.CHAMBER_ROOT or Definitions.ENCOUNTER_ROOT
  return read_disk(root .. "/" .. filename)
end

local function decode(options, kind, filename)
  local data = source_read(options, kind, filename)
  if not data then return failure(kind, filename, "file is missing") end
  local value, err = Json.decode(data)
  if not value then return failure(kind, filename, "malformed JSON: " .. tostring(err)) end
  if type(value) ~= "table" then return failure(kind, filename, "root must be an object") end
  return value
end

local function passable(definition, row, x)
  local symbol = row:sub(x, x)
  local semantic = definition.tile_legend[symbol]
  return Vocabulary.TILES[semantic] and Vocabulary.TILES[semantic].walkable
end

local function reachable(definition, start)
  local seen, queue, head = { [point_key(start)] = true }, { start }, 1
  local cardinal = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }
  while queue[head] do
    local current = queue[head]; head = head + 1
    for _, delta in ipairs(cardinal) do
      local x, y = current.x + delta[1], current.y + delta[2]
      if x >= 1 and x <= definition.width and y >= 1 and y <= definition.height and passable(definition, definition.tiles[y], x) then
        local point = { x = x, y = y }; local id = point_key(point)
        if not seen[id] then seen[id] = true; queue[#queue + 1] = point end
      end
    end
  end
  return seen
end

local MARKERS = { "player_spawn", "enemy_spawns", "exits", "caches", "reinforcements", "boss_spawns" }
local function validate_marker_points(definition, file)
  local markers, occupied = definition.markers, {}
  if type(markers) ~= "table" then return failure("chambers", file, "markers must be an object") end
  if type(markers.player_spawn) ~= "table" or #markers.player_spawn ~= 1 then return failure("chambers", file, "exactly one player_spawn marker is required") end
  if type(markers.enemy_spawns) ~= "table" or #markers.enemy_spawns < 1 then return failure("chambers", file, "at least one enemy_spawn marker is required") end
  if type(markers.exits) ~= "table" or #markers.exits < 1 then return failure("chambers", file, "at least one exit marker is required") end
  for _, name in ipairs(MARKERS) do
    local values = markers[name] or {}
    if type(values) ~= "table" then return failure("chambers", file, name .. " must be an array") end
    for index, point in ipairs(values) do
      if type(point) ~= "table" or not integer(point.x) or not integer(point.y) or point.x < 1 or point.x > definition.width or point.y < 1 or point.y > definition.height then
        return failure("chambers", file, name .. " marker " .. index .. " is outside board bounds")
      end
      if not passable(definition, definition.tiles[point.y], point.x) then return failure("chambers", file, name .. " marker " .. index .. " is not on a walkable tile") end
      local id = point_key(point)
      if occupied[id] then return failure("chambers", file, "markers overlap at " .. id) end
      occupied[id] = true
      if name == "enemy_spawns" and point.group ~= nil and type(point.group) ~= "string" then return failure("chambers", file, "enemy_spawn group must be a string") end
    end
  end
  local connected = reachable(definition, markers.player_spawn[1])
  for y, row in ipairs(definition.tiles) do
    for x = 1, #row do
      if passable(definition, row, x) and not connected[tostring(x) .. ":" .. tostring(y)] then return failure("chambers", file, "walkable tile " .. x .. "," .. y .. " is disconnected from player_spawn") end
    end
  end
  for _, exit in ipairs(markers.exits) do if not connected[point_key(exit)] then return failure("chambers", file, "exit is unreachable") end end
  return true
end

function Definitions.validate_chamber(definition, file)
  file = file or definition and definition.id or "<chamber>"
  if type(definition) ~= "table" then return failure("chambers", file, "definition must be an object") end
  if definition.schema_version ~= Definitions.CHAMBER_SCHEMA_VERSION then return failure("chambers", file, "unsupported schema_version") end
  if type(definition.id) ~= "string" or not definition.id:match("^chamber%.[a-z0-9_%-]+$") then return failure("chambers", file, "id must be stable chamber.* ID") end
  if type(definition.name) ~= "string" or definition.name == "" then return failure("chambers", file, "name is required") end
  if not integer(definition.width) or definition.width < 8 or definition.width > 14 or not integer(definition.height) or definition.height < 7 or definition.height > 10 then return failure("chambers", file, "dimensions must be within 8x7 through 14x10") end
  if not array(definition.topology_tags) or not string_array(definition.topology_tags) or not unique_strings(definition.topology_tags) then return failure("chambers", file, "topology_tags must be a unique nonempty string array") end
  for _, tag in ipairs(definition.topology_tags) do if not Vocabulary.TOPOLOGY_TAGS[tag] then return failure("chambers", file, "unknown topology tag '" .. tag .. "'") end end
  if type(definition.tile_legend) ~= "table" or type(definition.tiles) ~= "table" or #definition.tiles ~= definition.height then return failure("chambers", file, "tiles must contain exactly height rows with a tile_legend") end
  for symbol, semantic in pairs(definition.tile_legend) do
    if type(symbol) ~= "string" or #symbol ~= 1 or not Vocabulary.TILES[semantic] then return failure("chambers", file, "tile_legend contains unknown semantic tile") end
  end
  for index, row in ipairs(definition.tiles) do
    if type(row) ~= "string" or #row ~= definition.width then return failure("chambers", file, "tile row " .. index .. " has invalid width") end
    for x = 1, #row do if not definition.tile_legend[row:sub(x, x)] then return failure("chambers", file, "tile row " .. index .. " contains unknown symbol") end end
  end
  if definition.enabled ~= nil and type(definition.enabled) ~= "boolean" then return failure("chambers", file, "enabled must be boolean") end
  if definition.weight ~= nil and (type(definition.weight) ~= "number" or definition.weight <= 0) then return failure("chambers", file, "weight must be positive") end
  if definition.boss_compatible ~= nil and type(definition.boss_compatible) ~= "boolean" then return failure("chambers", file, "boss_compatible must be boolean") end
  return validate_marker_points(definition, file)
end

local function tag_set(values) local result = {}; for _, value in ipairs(values or {}) do result[value] = true end; return result end

function Definitions.validate_encounter(definition, file)
  file = file or definition and definition.id or "<encounter>"
  if type(definition) ~= "table" then return failure("encounters", file, "definition must be an object") end
  if definition.schema_version ~= Definitions.ENCOUNTER_SCHEMA_VERSION then return failure("encounters", file, "unsupported schema_version") end
  if type(definition.id) ~= "string" or not definition.id:match("^expedition%.encounter%.[a-z0-9_%-]+$") then return failure("encounters", file, "id must be stable expedition.encounter.* ID") end
  if type(definition.name) ~= "string" or definition.name == "" or not Vocabulary.ARCHETYPES[definition.archetype] then return failure("encounters", file, "name and known archetype are required") end
  if not integer(definition.stage) or definition.stage < 1 or definition.stage > 3 then return failure("encounters", file, "stage must be 1, 2 or 3") end
  if not array(definition.profiles) or not string_array(definition.profiles) then return failure("encounters", file, "profiles must be a nonempty string array") end
  if type(definition.roles) ~= "table" or #definition.roles < 1 then return failure("encounters", file, "at least one role row is required") end
  local roles, minimum_count, max_count = {}, 0, 0
  for index, row in ipairs(definition.roles) do
    if type(row) ~= "table" or not Vocabulary.ROLE_COSTS[row.role] or roles[row.role] or not integer(row.min) or not integer(row.max) or row.min < 0 or row.max < row.min then return failure("encounters", file, "role row " .. index .. " is invalid") end
    roles[row.role] = true; minimum_count = minimum_count + row.min; max_count = max_count + row.max
    if row.weight ~= nil and (type(row.weight) ~= "number" or row.weight <= 0) then return failure("encounters", file, "role row " .. index .. " has invalid weight") end
  end
  if not integer(definition.max_enemies) or definition.max_enemies < minimum_count or definition.max_enemies > max_count then return failure("encounters", file, "max_enemies conflicts with role rows") end
  if type(definition.threat_budget) ~= "table" or type(definition.threat_budget.base) ~= "number" or definition.threat_budget.base <= 0 then return failure("encounters", file, "threat_budget.base must be positive") end
  if not Vocabulary.SPAWN_INTENTS[definition.spawn_intent] or not Vocabulary.CLEAR_CONDITIONS[definition.clear_condition] or not Vocabulary.REWARD_INTENTS[definition.reward_intent] then return failure("encounters", file, "unknown spawn_intent, clear_condition or reward_intent") end
  for _, field in ipairs({ "compatible_topology_tags", "incompatible_topology_tags" }) do
    if definition[field] ~= nil and (not string_array(definition[field]) or not unique_strings(definition[field])) then return failure("encounters", file, field .. " must be unique strings") end
    for _, tag in ipairs(definition[field] or {}) do if not Vocabulary.TOPOLOGY_TAGS[tag] then return failure("encounters", file, "unknown topology tag '" .. tag .. "'") end end
  end
  if type(definition.elite) ~= "table" or type(definition.elite.allowed) ~= "boolean" then return failure("encounters", file, "elite.allowed must be boolean") end
  if type(definition.reinforcement) ~= "table" or type(definition.reinforcement.enabled) ~= "boolean" then return failure("encounters", file, "reinforcement.enabled must be boolean") end
  if definition.enabled ~= nil and type(definition.enabled) ~= "boolean" then return failure("encounters", file, "enabled must be boolean") end
  return true
end

function Definitions.compatibility(chamber, encounter)
  local tags, needs = tag_set(chamber.topology_tags), tag_set(encounter.compatible_topology_tags)
  local required_count, matched = #(encounter.compatible_topology_tags or {}), false
  for tag in pairs(needs) do if tags[tag] then matched = true end end
  if required_count > 0 and not matched then return false, "encounter requires compatible topology tag" end
  for _, tag in ipairs(encounter.incompatible_topology_tags or {}) do if tags[tag] then return false, "encounter forbids topology tag '" .. tag .. "'" end end
  if encounter.reinforcement.enabled and #(chamber.markers.reinforcements or {}) < 1 then return false, "reinforcement encounter requires a reinforcement marker" end
  if encounter.archetype == "crossfire" then
    local groups = {}; for _, marker in ipairs(chamber.markers.enemy_spawns or {}) do groups[marker.group or "default"] = true end
    local count = 0; for _ in pairs(groups) do count = count + 1 end
    if count < 2 then return false, "crossfire requires at least two enemy spawn groups" end
  end
  return true
end

local function load_kind(options, kind)
  local manifest, error = decode(options, kind, "manifest.json")
  if not manifest then return nil, error end
  if type(manifest.files) ~= "table" then return failure(kind, "manifest.json", "manifest.files must be an array") end
  local files, names = {}, {}
  for _, filename in ipairs(manifest.files) do
    if type(filename) ~= "string" or not filename:match("^[a-z0-9_%-]+%.json$") or filename == "manifest.json" or names[filename] then return failure(kind, "manifest.json", "manifest contains invalid or duplicate filename") end
    names[filename] = true; files[#files + 1] = filename
  end
  table.sort(files)
  local values, by_id, filenames = {}, {}, {}
  for _, filename in ipairs(files) do
    local definition, err = decode(options, kind, filename); if not definition then return nil, err end
    local valid, failure_value
    if kind == "chambers" then
      valid, failure_value = Definitions.validate_chamber(definition, filename)
    else
      valid, failure_value = Definitions.validate_encounter(definition, filename)
    end
    if not valid then return nil, failure_value end
    if by_id[definition.id] then return failure(kind, filename, "duplicate stable ID '" .. definition.id .. "'") end
    definition.display_name = definition.name -- runtime-only convenience; never written by the editor.
    values[#values + 1], by_id[definition.id], filenames[definition.id] = definition, definition, filename
  end
  return { ordered = values, by_id = by_id, filenames = filenames, files = files }
end

function Definitions.load(options)
  local chambers, error = load_kind(options, "chambers"); if not chambers then return nil, error end
  local encounters, encounter_error = load_kind(options, "encounters"); if not encounters then return nil, encounter_error end
  local registry = { chambers = chambers.ordered, encounters = encounters.ordered, chamber_by_id = chambers.by_id, encounter_by_id = encounters.by_id, filenames = { chambers = chambers.filenames, encounters = encounters.filenames } }
  for _, encounter in ipairs(registry.encounters) do
    local found = false
    for _, chamber in ipairs(registry.chambers) do local compatible = Definitions.compatibility(chamber, encounter); if compatible and chamber.enabled ~= false and encounter.enabled ~= false then found = true; break end end
    if not found then return failure("encounters", registry.filenames.encounters[encounter.id], "encounter has no compatible enabled chamber") end
  end
  return registry
end

function Definitions.compatible_chambers(registry, encounter, options)
  options = options or {}
  local result = {}
  for _, chamber in ipairs(registry.chambers) do
    local compatible = Definitions.compatibility(chamber, encounter)
    if compatible and chamber.enabled ~= false and (options.include_boss or not chamber.boss_compatible) then result[#result + 1] = chamber end
  end
  return result
end

return Definitions
