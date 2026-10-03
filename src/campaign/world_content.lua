-- Deterministic, campaign-owned placement of the persistent world content
-- introduced in OW-06.  This module deliberately owns *where* semantic sites
-- are.  The generated zone shard remains authoritative for every mutable
-- consequence of a site: stock, claimed caches, boss damage, corpses, doors,
-- construction and environmental state.
local Rng = require("src.rng")
local ZoneKey = require("src.campaign.zone_key")
local WorldTopology = require("src.campaign.world_topology")

local WorldContent = {
  VERSION = 1,
  SERVICE_OUTPOST_COUNT = 5,
  RUIN_COUNT = 3,
  REACTOR_COUNT = 2,
}

local SERVICE_IDS = {
  "service.supply.legacy",
  "service.repair.legacy",
  "service.salvager.legacy",
  "service.charm_vendor.legacy",
}

local VALID_HABITATS = { ruin = true, reactor = true, cave = true }

local LOCATION_LABELS = {
  ["zone_profile.legacy.forest"] = "OLD-GROWTH FOREST",
  ["zone_profile.legacy.cave"] = "SUBTERRANEAN CAVERN",
  ["zone_profile.legacy.deep_cave"] = "DEEP CAVERN",
  ["zone_profile.world.dungeon"] = "RUIN COMPLEX",
  ["zone_profile.world.deep_dungeon"] = "DEEP RUIN",
  ["zone_profile.world.reactor"] = "REACTOR COMPLEX",
  ["zone_profile.world.reactor_core"] = "REACTOR CORE",
  ["zone_profile.world.boss_lair"] = "THREAT LAIR",
}

local function copy_key(key)
  return ZoneKey.to_data(ZoneKey.from_data(key))
end

local function encode(key)
  return ZoneKey.encode(ZoneKey.from_data(key))
end

local function ordered_surface_columns()
  local result = {}
  for y = WorldTopology.MIN_COORDINATE, WorldTopology.MAX_COORDINATE do
    for x = WorldTopology.MIN_COORDINATE, WorldTopology.MAX_COORDINATE do
      if x ~= 0 or y ~= 0 then result[#result + 1] = ZoneKey.new(x, y, 0) end
    end
  end
  table.sort(result, function(a, b) return ZoneKey.encode(a) < ZoneKey.encode(b) end)
  return result
end

local function distance(a, b)
  return math.abs(a.world_x - b.world_x) + math.abs(a.world_y - b.world_y)
end

local function site_id(kind, suffix)
  return "world_site." .. kind .. "." .. suffix
end

local function sorted_bosses(registry)
  local values = {}
  for id, definition in pairs(registry.bosses or {}) do
    if definition.world_site_family then values[#values + 1] = definition end
  end
  table.sort(values, function(a, b) return a.id < b.id end)
  return values
end

local function choose_columns(seed, stream, count, reserved, selected, minimum_distance, predicate, allow_partial)
  local candidates = {}
  for _, key in ipairs(ordered_surface_columns()) do
    local id = ZoneKey.encode(key)
    if not reserved[id] and (not predicate or predicate(key)) then candidates[#candidates + 1] = key end
  end
  candidates = Rng.new(seed):derive("world_content.plan." .. stream):shuffle(candidates)
  local result = {}
  for _, candidate in ipairs(candidates) do
    if #result >= count then break end
    local legal = true
    for _, other in ipairs(selected) do
      if distance(candidate, other) < minimum_distance then legal = false; break end
    end
    if legal then
      result[#result + 1] = candidate
      selected[#selected + 1] = candidate
      reserved[ZoneKey.encode(candidate)] = true
    end
  end
  -- A finite 9x9 map can have an unlucky greedy ordering after the protected
  -- near-start outpost has been selected. Keep the preferred spacing first,
  -- then use the same bounded deterministic candidate list to fill any
  -- remaining unique columns rather than failing a valid campaign seed.
  if #result < count then
    for _, candidate in ipairs(candidates) do
      if #result >= count then break end
      local id = ZoneKey.encode(candidate)
      if not reserved[id] then
        result[#result + 1] = candidate
        selected[#selected + 1] = candidate
        reserved[id] = true
      end
    end
  end
  assert(#result == count or allow_partial, "World content plan could not find enough eligible surface columns")
  return result
end

local function add_vertical_link(plan, source, destination, connection_type, source_label, destination_label)
  plan.vertical_links[#plan.vertical_links + 1] = {
    id = "world_link." .. tostring(#plan.vertical_links + 1),
    source = copy_key(source),
    destination = copy_key(destination),
    connection_type = connection_type,
    source_label = source_label,
    destination_label = destination_label,
  }
end

local function add_profile(plan, key, profile_id)
  plan.zone_profiles[encode(key)] = profile_id
end

local function add_site(plan, values)
  assert(type(values.id) == "string" and values.id ~= "", "World content site ID is required")
  plan.sites[#plan.sites + 1] = values
end

local function add_structure(plan, kind, column, index, boss)
  local surface = ZoneKey.new(column.world_x, column.world_y, 0)
  local upper = ZoneKey.new(column.world_x, column.world_y, -1)
  local deep = ZoneKey.new(column.world_x, column.world_y, -2)
  local is_reactor = kind == "reactor"
  local first_type = is_reactor and "elevator" or (kind == "cave" and "cave_mouth" or "stairs")
  local second_type = is_reactor and "elevator" or (kind == "cave" and "shaft" or "stairs")
  local surface_label = is_reactor and "ACCESS FACILITY" or (kind == "cave" and "DESCEND INTO CAVE" or "ENTER RUINS")
  local upper_label = is_reactor and "ASCEND TO SURFACE" or (kind == "cave" and "ASCEND TO SURFACE" or "ASCEND TO SURFACE")
  local down_label = is_reactor and "DESCEND TO REACTOR CORE" or (kind == "cave" and "DESCEND SHAFT" or "DESCEND DEEPER")
  local up_label = is_reactor and "ASCEND TO REACTOR" or (kind == "cave" and "CLIMB SHAFT" or "ASCEND TO RUINS")
  add_vertical_link(plan, surface, upper, first_type, surface_label, upper_label)
  add_vertical_link(plan, upper, deep, second_type, down_label, up_label)
  if kind == "ruin" then
    add_profile(plan, upper, "zone_profile.world.dungeon")
    add_profile(plan, deep, "zone_profile.world.deep_dungeon")
  elseif kind == "reactor" then
    add_profile(plan, upper, "zone_profile.world.reactor")
    add_profile(plan, deep, "zone_profile.world.reactor_core")
  end
  add_profile(plan, deep, "zone_profile.world.boss_lair")
  add_site(plan, {
    id = site_id(kind, tostring(index)), type = kind .. "_complex",
    surface_key = copy_key(surface), interior_keys = { copy_key(upper), copy_key(deep) },
    location_name = kind == "reactor" and "REACTOR ACCESS" or (kind == "ruin" and "RUIN ENTRANCE" or "CAVE DESCENT"),
  })
  add_site(plan, {
    id = site_id("boss", boss.id:gsub("[^%w]+", "_")), type = "boss_site", boss_id = boss.id,
    habitat = kind, zone_key = copy_key(deep), location_name = boss.display_name,
  })
end

local function discovery_environment(definition)
  local allowed = definition.allowed_biome_ids or {}
  for _, id in ipairs(allowed) do
    if id == "biome.legacy.forest" then return "surface" end
    if id == "biome.legacy.cave" then return "cave" end
    if id == "biome.legacy.dungeon" then return "dungeon" end
    if id == "biome.legacy.reactor" then return "reactor" end
  end
  error("Discovery has no supported campaign environment: " .. tostring(definition.id))
end

local function profile_candidates(plan, environment)
  local result = {}
  for _, site in ipairs(plan.sites) do
    if environment == "surface" and site.type == "service_outpost" then
      result[#result + 1] = ZoneKey.from_data(site.zone_key)
    elseif environment == "cave" and site.type == "cave_complex" then
      result[#result + 1] = ZoneKey.from_data(site.interior_keys[1])
    elseif environment == "dungeon" and site.type == "ruin_complex" then
      result[#result + 1] = ZoneKey.from_data(site.interior_keys[1])
    elseif environment == "reactor" and site.type == "reactor_complex" then
      result[#result + 1] = ZoneKey.from_data(site.interior_keys[1])
    end
  end
  table.sort(result, function(a, b) return ZoneKey.encode(a) < ZoneKey.encode(b) end)
  return result
end

-- Build a complete plan with no dependence on generation or visit order.
-- `reserved_columns` is used only while importing an OW-01..05 campaign;
-- fresh campaigns reserve only their protected origin.
function WorldContent.new(seed, registry, reserved_columns)
  assert(type(seed) == "number" and seed % 1 == 0, "World content plan seed is invalid")
  assert(registry and registry.bosses and registry.discoveries, "World content plan requires loaded content")
  local reserved, selected = {}, {}
  local compatibility_import = next(reserved_columns or {}) ~= nil
  reserved[ZoneKey.encode(ZoneKey.new(0, 0, 0))] = true
  for id in pairs(reserved_columns or {}) do reserved[id] = true end
  local plan = { version = WorldContent.VERSION, sites = {}, vertical_links = {}, zone_profiles = {} }

  -- A guaranteed early outpost uses the same deterministic candidate ordering
  -- as every other site but cannot consume the start zone itself.
  local near = choose_columns(seed, "service.near", 1, reserved, selected, 1, function(key)
    local d = math.abs(key.world_x) + math.abs(key.world_y)
    return d >= 1 and d <= 2
  end, compatibility_import)[1]
  local service_columns = near and { near } or {}
  if not near and compatibility_import then
    local fallback = choose_columns(seed, "service.compatibility", 1, reserved, selected, 1, nil, true)[1]
    if fallback then service_columns[#service_columns + 1] = fallback end
  end
  local extra = choose_columns(seed, "service.remaining", math.max(0, WorldContent.SERVICE_OUTPOST_COUNT - #service_columns), reserved, selected, 2, nil, compatibility_import)
  for _, key in ipairs(extra) do service_columns[#service_columns + 1] = key end
  local packages = {
    { SERVICE_IDS[1], SERVICE_IDS[2] }, { SERVICE_IDS[3] }, { SERVICE_IDS[4] }, { SERVICE_IDS[1] }, { SERVICE_IDS[2] },
  }
  for index, column in ipairs(service_columns) do
    add_site(plan, {
      id = site_id("service", tostring(index)), type = "service_outpost", zone_key = copy_key(column),
      service_ids = packages[index], location_name = "ABANDONED SERVICE OUTPOST",
    })
  end

  local bosses_by_habitat = { ruin = {}, reactor = {}, cave = {} }
  for _, boss in ipairs(sorted_bosses(registry)) do
    local habitat = boss.world_site_family
    assert(VALID_HABITATS[habitat] and bosses_by_habitat[habitat], "Boss has no campaign habitat: " .. boss.id)
    bosses_by_habitat[habitat][#bosses_by_habitat[habitat] + 1] = boss
  end
  for _, habitat in ipairs({ "ruin", "reactor", "cave" }) do
    table.sort(bosses_by_habitat[habitat], function(a, b) return a.id < b.id end)
    local columns = choose_columns(seed, "structure." .. habitat, #bosses_by_habitat[habitat], reserved, selected, 3, nil, compatibility_import)
    for index, column in ipairs(columns) do add_structure(plan, habitat, column, index, bosses_by_habitat[habitat][index]) end
  end

  -- One planned physical site per existing discovery.  Sites are assigned to
  -- compatible authored locations and stay hidden from ordinary player UI.
  local used = {}
  local discoveries = {}
  for _, definition in pairs(registry.discoveries) do discoveries[#discoveries + 1] = definition end
  table.sort(discoveries, function(a, b) return a.id < b.id end)
  for _, definition in ipairs(discoveries) do
    local candidates = profile_candidates(plan, discovery_environment(definition))
    if #candidates == 0 and compatibility_import then
      plan.migration_limited = true
    else
      assert(#candidates > 0, "No compatible world site for discovery " .. definition.id)
    end
    if #candidates == 0 then break end
    local shuffled = Rng.new(seed):derive("world_content.discovery." .. definition.id):shuffle(candidates)
    local zone
    for _, candidate in ipairs(shuffled) do
      local id = ZoneKey.encode(candidate)
      if not used[id] then zone = candidate; used[id] = true; break end
    end
    -- Same-environment discoveries can share a zone when the fixed content
    -- density requires it (notably two Dungeon definitions and three ruins).
    zone = zone or shuffled[1]
    add_site(plan, {
      id = site_id("discovery", definition.id:gsub("[^%w]+", "_")), type = "discovery_site",
      discovery_id = definition.id, zone_key = copy_key(zone), environment = discovery_environment(definition),
      location_name = definition.display_name,
    })
  end
  table.sort(plan.sites, function(a, b) return a.id < b.id end)
  table.sort(plan.vertical_links, function(a, b) return a.id < b.id end)
  local summary = WorldContent.summary(plan)
  if compatibility_import and (summary.services < WorldContent.SERVICE_OUTPOST_COUNT or summary.ruins < WorldContent.RUIN_COUNT
    or summary.reactors < WorldContent.REACTOR_COUNT or summary.bosses < #sorted_bosses(registry)
    or summary.discoveries < (function() local count = 0; for _ in pairs(registry.discoveries) do count = count + 1 end; return count end)()) then
    plan.migration_limited = true
  end
  WorldContent.validate(plan, registry)
  return plan
end

function WorldContent.site_for_zone(plan, key, kind)
  if not plan then return nil end
  local encoded = encode(key)
  for _, site in ipairs(plan.sites or {}) do
    local site_key = site.zone_key or site.surface_key
    if site_key and encode(site_key) == encoded and (not kind or site.type == kind) then return site end
    for _, interior in ipairs(site.interior_keys or {}) do
      if encode(interior) == encoded and (not kind or site.type == kind) then return site end
    end
  end
  return nil
end

function WorldContent.sites_for_zone(plan, key)
  local result, encoded = {}, encode(key)
  for _, site in ipairs((plan and plan.sites) or {}) do
    local matches = site.zone_key and encode(site.zone_key) == encoded
      or site.surface_key and encode(site.surface_key) == encoded
    for _, interior in ipairs(site.interior_keys or {}) do if encode(interior) == encoded then matches = true end end
    if matches then result[#result + 1] = site end
  end
  table.sort(result, function(a, b) return a.id < b.id end)
  return result
end

function WorldContent.boss_site_for_zone(plan, key)
  for _, site in ipairs(WorldContent.sites_for_zone(plan, key)) do if site.type == "boss_site" then return site end end
  return nil
end

function WorldContent.profile_for(plan, key)
  if plan and plan.zone_profiles then return plan.zone_profiles[encode(key)] end
  return nil
end

function WorldContent.location_name(plan, key, profile_id)
  local sites = WorldContent.sites_for_zone(plan, key)
  for _, site in ipairs(sites) do
    if site.type == "boss_site" or site.type == "ruin_complex" or site.type == "reactor_complex"
      or site.type == "cave_complex" or site.type == "service_outpost" then
      return site.location_name
    end
  end
  return LOCATION_LABELS[profile_id] or "WILDERNESS"
end

function WorldContent.connection_link(plan, key, direction)
  if not plan then return nil end
  local encoded = encode(key)
  for _, link in ipairs(plan.vertical_links or {}) do
    if encode(link.source) == encoded and direction == "down" then return link, true end
    if encode(link.destination) == encoded and direction == "up" then return link, false end
  end
  return nil
end

function WorldContent.reserves_column(plan, key)
  if not plan then return false end
  for _, link in ipairs(plan.vertical_links or {}) do
    local source = ZoneKey.from_data(link.source)
    if source.world_x == key.world_x and source.world_y == key.world_y then return true end
  end
  return false
end

function WorldContent.validate(plan, registry)
  assert(type(plan) == "table" and plan.version == WorldContent.VERSION, "World content plan version is invalid")
  assert(type(plan.sites) == "table" and type(plan.vertical_links) == "table" and type(plan.zone_profiles) == "table",
    "World content plan is incomplete")
  local ids, boss_ids, discovery_ids, service_roles = {}, {}, {}, {}
  local summary, major_columns = WorldContent.summary(plan), {}
  for _, site in ipairs(plan.sites) do
    assert(type(site.id) == "string" and not ids[site.id], "World content site IDs must be unique")
    ids[site.id] = true
    if site.zone_key then assert(WorldTopology.is_zone_in_bounds(ZoneKey.from_data(site.zone_key)), "World content site is out of bounds") end
    if site.surface_key then assert(WorldTopology.is_zone_in_bounds(ZoneKey.from_data(site.surface_key)), "World content site is out of bounds") end
    for _, key in ipairs(site.interior_keys or {}) do assert(WorldTopology.is_zone_in_bounds(ZoneKey.from_data(key)), "World content interior is out of bounds") end
    if site.type == "boss_site" then
      assert(registry.bosses[site.boss_id] and not boss_ids[site.boss_id], "World content boss assignment is invalid")
      boss_ids[site.boss_id] = true
    elseif site.type == "discovery_site" then
      assert(registry.discoveries[site.discovery_id] and not discovery_ids[site.discovery_id], "World content discovery assignment is invalid")
      discovery_ids[site.discovery_id] = true
    elseif site.type == "service_outpost" then
      for _, id in ipairs(site.service_ids or {}) do
        assert(registry.services[id], "World content references an unknown service")
        service_roles[registry.services[id].role] = true
      end
    end
    if site.type == "ruin_complex" or site.type == "reactor_complex" or site.type == "cave_complex" then
      local origin = ZoneKey.from_data(site.surface_key)
      assert(origin.world_x ~= 0 or origin.world_y ~= 0, "World content cannot replace the starting zone")
      local column_id = origin.world_x .. ":" .. origin.world_y
      assert(not major_columns[column_id], "Major world structures cannot share a column")
      major_columns[column_id] = origin
    end
  end
  if plan.migration_limited ~= true then
    for id, definition in pairs(registry.bosses) do
      if definition.world_site_family then assert(boss_ids[id], "World content plan omits boss " .. id) end
    end
    for id in pairs(registry.discoveries) do assert(discovery_ids[id], "World content plan omits discovery " .. id) end
    for _, role in ipairs({ "supply", "repair", "salvager", "charm_vendor" }) do assert(service_roles[role], "World content plan omits service role " .. role) end
    assert(summary.services == WorldContent.SERVICE_OUTPOST_COUNT and summary.ruins == WorldContent.RUIN_COUNT
      and summary.reactors == WorldContent.REACTOR_COUNT, "World content plan has invalid major site counts")
  end
  for _, link in ipairs(plan.vertical_links) do
    local source, destination = ZoneKey.from_data(link.source), ZoneKey.from_data(link.destination)
    assert(source.world_x == destination.world_x and source.world_y == destination.world_y and source.z == destination.z + 1,
      "World content vertical link endpoints are invalid")
    assert(WorldTopology.CONNECTION_TYPES[link.connection_type] and link.connection_type ~= "cardinal",
      "World content vertical link type is invalid")
  end
  return true
end

function WorldContent.summary(plan)
  local result = { services = 0, ruins = 0, reactors = 0, bosses = 0, discoveries = 0 }
  for _, site in ipairs((plan and plan.sites) or {}) do
    if site.type == "service_outpost" then result.services = result.services + 1
    elseif site.type == "ruin_complex" then result.ruins = result.ruins + 1
    elseif site.type == "reactor_complex" then result.reactors = result.reactors + 1
    elseif site.type == "boss_site" then result.bosses = result.bosses + 1
    elseif site.type == "discovery_site" then result.discoveries = result.discoveries + 1 end
  end
  return result
end

return WorldContent
