-- Pure authoring model for the standalone sprite editor. Keeping selection,
-- validation, history, filtering, and JSON data separate from LÖVE makes the
-- editor testable and gives future ROAG Studio adapters a stable seam.
local Model = {}
Model.__index = Model

Model.COLUMNS, Model.ROWS = 49, 22
Model.FORMAT_VERSION = 1

local DEFAULT_MAPPINGS = {
  player = { 25, 1 }, target = { 38, 3 }, ammo = { 23, 5 }, torch = { 20, 7 }, door = { 22, 1 },
  bullet = { 34, 3 }, bomb = { 38, 6 }, flare = { 23, 6 }, necromancer = { 27, 10 }, wolf = { 31, 9 },
  ground = { 1, 22 }, water_shallow = { 10, 6 }, water_deep = { 11, 6 }, spikes = { 15, 11 },
  fire = { 15, 11 }, gas = { 1, 22 }, electric_arc = { 28, 21 },
  bomber = { 20, 9 }, cultist = { 28, 10 }, ripper = { 31, 9 }, skirmisher = { 29, 10 },
  conductor = { 27, 10 }, bulwark = { 30, 9 }, reclaimer = { 26, 10 }, gunner_elite = { 25, 10 },
  shock_bruiser = { 28, 9 }, volatile_heavy = { 20, 9 }, arc_cutter = { 27, 10 },
  maintenance_heavy = { 30, 9 }, reactor_suppressor = { 29, 10 }, arc_warden = { 28, 9 }, boss = { 30, 2 },
  wall_left = { 10, 4 }, wall_right = { 11, 4 }, wall_up = { 12, 4 }, wall_down = { 13, 4 },
  wall_top_left = { 17, 14 }, wall_top_right = { 19, 14 },
  wall_bottom_left = { 17, 16 }, wall_bottom_right = { 19, 16 }, wall_center = { 18, 15 },
  old_growth_tree = { 5, 2 }, fallen_log = { 9, 3 }, granite_boulder = { 2, 14 }, stalagmite = { 3, 14 },
  rubble_pile = { 17, 15 }, ruined_statue = { 18, 15 }, machine_bank = { 24, 11 }, cable_trunk = { 24, 10 },
  barricade = { 16, 11 }, crate = { 12, 9 }, metal_crate = { 14, 9 }, powered_door = { 22, 1 },
  generator = { 19, 8 }, breaker = { 20, 8 }, service_kiosk = { 23, 8 }, reinforced_barrier = { 15, 12 },
  maintenance_hatch = { 17, 12 }, discovery_cache = { 22, 8 }, discovery_clue = { 21, 8 },
  reinforcement_nest = { 8, 3 }, reinforcement_lift = { 24, 8 },
}

local ROLE_DATA = {
  { "player", "Player", "Core" }, { "target", "Target", "Core" }, { "ammo", "Ammo", "Core" },
  { "torch", "Torch", "Core" }, { "door", "Exit door", "Core" }, { "bullet", "Bullet", "Core" },
  { "bomb", "Bomb", "Core" }, { "flare", "Flare", "Core" },
  { "ground", "Ground / floor", "Terrain" }, { "water_shallow", "Shallow water / coolant", "Terrain" },
  { "water_deep", "Deep water / coolant", "Terrain" }, { "spikes", "Spike hazard", "Terrain" },
  { "fire", "Active fire", "Terrain" }, { "gas", "Toxic gas", "Terrain" },
  { "electric_arc", "Electrical discharge", "Terrain" },
  { "wall_left", "Wall left face", "Terrain" }, { "wall_right", "Wall right face", "Terrain" },
  { "wall_up", "Wall up face", "Terrain" }, { "wall_down", "Wall down face", "Terrain" },
  { "wall_top_left", "Wall top-left corner", "Terrain" }, { "wall_top_right", "Wall top-right corner", "Terrain" },
  { "wall_bottom_left", "Wall bottom-left corner", "Terrain" }, { "wall_bottom_right", "Wall bottom-right corner", "Terrain" },
  { "wall_center", "Wall centre", "Terrain" },
  { "wolf", "Wolf", "Enemies" }, { "bomber", "Bomber", "Enemies" }, { "necromancer", "Necromancer", "Enemies" },
  { "cultist", "Cultist", "Enemies" }, { "ripper", "Ripper", "Enemies" }, { "skirmisher", "Skirmisher", "Enemies" },
  { "conductor", "Conductor", "Enemies" }, { "bulwark", "Bulwark", "Enemies" }, { "reclaimer", "Reclaimer", "Enemies" },
  { "gunner_elite", "Redundant gunner", "Elites" }, { "shock_bruiser", "Shock bruiser", "Elites" },
  { "volatile_heavy", "Volatile heavy", "Elites" },
  { "arc_cutter", "Arc cutter", "Reactor" }, { "maintenance_heavy", "Maintenance heavy", "Reactor" },
  { "reactor_suppressor", "Reactor suppressor", "Reactor" }, { "arc_warden", "Arc warden", "Reactor" },
  { "boss", "Boss", "Boss" },
  { "old_growth_tree", "Old-growth tree", "World" }, { "fallen_log", "Fallen trunk", "World" },
  { "granite_boulder", "Granite boulder", "World" }, { "stalagmite", "Stone pillar", "World" },
  { "rubble_pile", "Collapsed rubble", "World" }, { "ruined_statue", "Ruined statue", "World" },
  { "machine_bank", "Derelict machine bank", "World" }, { "cable_trunk", "Exposed cable trunk", "World" },
  { "barricade", "Masonry barricade", "World" }, { "crate", "Timber crate", "World" },
  { "metal_crate", "Metal crate", "World" }, { "powered_door", "Powered bulkhead", "World" },
  { "generator", "Maintenance generator", "World" }, { "breaker", "Circuit breaker", "World" },
  { "service_kiosk", "Service kiosk", "World" }, { "reinforced_barrier", "Reinforced barrier", "World" },
  { "maintenance_hatch", "Maintenance hatch", "World" }, { "discovery_cache", "Discovery cache", "World" },
  { "discovery_clue", "Access marking", "World" }, { "reinforcement_nest", "Disturbed nest", "World" },
  { "reinforcement_lift", "Maintenance lift", "World" },
}

local function clone_tile(tile)
  return tile and { tile[1], tile[2] } or nil
end

local function clone_mappings(source)
  local result = {}
  for key, tile in pairs(source or {}) do result[key] = clone_tile(tile) end
  return result
end

local function same_tile(first, second)
  return first == second or (first and second and first[1] == second[1] and first[2] == second[2])
end

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function valid_tile(column, row)
  return type(column) == "number" and type(row) == "number" and column % 1 == 0 and row % 1 == 0
    and column >= 1 and column <= Model.COLUMNS and row >= 1 and row <= Model.ROWS
end

local function role_map()
  local result = {}
  for _, values in ipairs(ROLE_DATA) do
    result[values[1]] = { key = values[1], label = values[2], category = values[3], optional = values[4] == true }
  end
  return result
end

function Model.roles()
  local result = {}
  for _, values in ipairs(ROLE_DATA) do
    result[#result + 1] = { key = values[1], label = values[2], category = values[3], optional = values[4] == true }
  end
  return result
end

function Model.categories()
  return { "All", "Core", "Terrain", "Enemies", "Elites", "Reactor", "Boss", "World" }
end

function Model.new(mappings)
  local self = setmetatable({ mappings = clone_mappings(DEFAULT_MAPPINGS), history = {}, history_index = 0, dirty = false }, Model)
  if mappings then self:replace(mappings, false) end
  return self
end

function Model:role(key)
  return role_map()[key]
end

function Model:tile(key)
  return clone_tile(self.mappings[key])
end

function Model:valid_tile(column, row)
  return valid_tile(column, row)
end

function Model:_record(change)
  while #self.history > self.history_index do table.remove(self.history) end
  self.history[#self.history + 1] = change
  self.history_index = #self.history
  self.dirty = true
end

function Model:_set(key, tile, record)
  local role = self:role(key)
  if not role then return nil, { code = "unknown_role", reason = "Unknown sprite role " .. tostring(key) } end
  if tile and not valid_tile(tile[1], tile[2]) then return nil, { code = "invalid_tile", reason = "Tile is outside the 49 × 22 sheet" } end
  local before, after = clone_tile(self.mappings[key]), clone_tile(tile)
  if same_tile(before, after) then return { applied = false, mapping = after } end
  self.mappings[key] = after
  if record ~= false then self:_record({ key = key, before = before, after = after }) end
  return { applied = true, mapping = clone_tile(after) }
end

function Model:assign(key, column, row)
  return self:_set(key, { column, row })
end

function Model:clear(key)
  local role = self:role(key)
  if not role then return nil, { code = "unknown_role" } end
  if not role.optional then return nil, { code = "required_role", reason = "Core mappings cannot be cleared; use Reset instead." } end
  return self:_set(key, nil)
end

function Model:reset(key)
  local default = DEFAULT_MAPPINGS[key]
  if not default then return self:clear(key) end
  return self:_set(key, default)
end

function Model:adjust(key, axis, amount)
  local current = self.mappings[key] or DEFAULT_MAPPINGS[key] or { 1, 1 }
  local column, row = current[1], current[2]
  if axis == "column" then column = clamp(column + amount, 1, Model.COLUMNS)
  elseif axis == "row" then row = clamp(row + amount, 1, Model.ROWS)
  else return nil, { code = "invalid_axis" } end
  return self:assign(key, column, row)
end

function Model:undo()
  local change = self.history[self.history_index]
  if not change then return { applied = false, code = "nothing_to_undo" } end
  self.mappings[change.key] = clone_tile(change.before)
  self.history_index = self.history_index - 1
  self.dirty = self.history_index > 0
  return { applied = true, key = change.key, mapping = self:tile(change.key) }
end

function Model:redo()
  local change = self.history[self.history_index + 1]
  if not change then return { applied = false, code = "nothing_to_redo" } end
  self.mappings[change.key] = clone_tile(change.after)
  self.history_index = self.history_index + 1
  self.dirty = true
  return { applied = true, key = change.key, mapping = self:tile(change.key) }
end

function Model:replace(mappings, mark_dirty)
  local roles = role_map()
  local next_mappings = clone_mappings(DEFAULT_MAPPINGS)
  for key, tile in pairs(mappings or {}) do
    if roles[key] and tile and valid_tile(tile[1], tile[2]) then next_mappings[key] = clone_tile(tile) end
  end
  for key, role in pairs(roles) do
    if role.optional and mappings and mappings[key] == nil then next_mappings[key] = nil end
  end
  self.mappings, self.history, self.history_index, self.dirty = next_mappings, {}, 0, mark_dirty == true
  return true
end

function Model:filtered(category, query)
  local result, needle = {}, (query or ""):lower()
  for _, role in ipairs(Model.roles()) do
    if (not category or category == "All" or role.category == category)
      and (needle == "" or role.label:lower():find(needle, 1, true) or role.key:find(needle, 1, true)) then
      result[#result + 1] = role
    end
  end
  return result
end

function Model:roles_at(column, row)
  local result = {}
  for _, role in ipairs(Model.roles()) do
    if same_tile(self.mappings[role.key], { column, row }) then result[#result + 1] = role end
  end
  return result
end

function Model:serialize()
  local lines, roles = { "{", "  \"version\": 1,", "  \"sprites\": {" }, {}
  for _, role in ipairs(Model.roles()) do if self.mappings[role.key] then roles[#roles + 1] = role end end
  for index, role in ipairs(roles) do
    local tile, suffix = self.mappings[role.key], index == #roles and "" or ","
    lines[#lines + 1] = string.format("    \"%s\": {\"column\": %d, \"row\": %d}%s", role.key, tile[1], tile[2], suffix)
  end
  lines[#lines + 1] = "  }"
  lines[#lines + 1] = "}"
  return table.concat(lines, "\n") .. "\n"
end

function Model.deserialize(contents)
  if type(contents) ~= "string" then return nil, { code = "invalid_json", reason = "Mapping file must be text" } end
  local mappings, count, roles = {}, 0, role_map()
  for key, column, row in contents:gmatch('\"([%w_]+)\"%s*:%s*{%s*\"column\"%s*:%s*(%d+)%s*,%s*\"row\"%s*:%s*(%d+)%s*}') do
    column, row = tonumber(column), tonumber(row)
    if roles[key] and valid_tile(column, row) then mappings[key], count = { column, row }, count + 1 end
  end
  if count == 0 then return nil, { code = "no_valid_mappings", reason = "No valid ROAG sprite mappings were found" } end
  return mappings
end

return Model
