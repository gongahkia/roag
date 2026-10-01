-- Authoritative, renderer-independent DAG for one run's macro route.
local Rng = require("src.rng")

local Graph = {}
Graph.__index = Graph

local function failure(code, reason)
  return nil, { code = code, reason = reason }
end

local function copy_node(node)
  return {
    id = node.id, key = node.key, type = node.type, depth = node.depth,
    biome_id = node.biome_id, tier_id = node.tier_id, floor_seed = node.floor_seed,
    service_id = node.service_id,
  }
end

local function unlock_set(values)
  local result = {}
  for _, value in ipairs(values or {}) do result[value] = true end
  return result
end

local function sorted_ids(values)
  local copy = {}
  for _, value in ipairs(values or {}) do copy[#copy + 1] = value end
  table.sort(copy)
  return copy
end

function Graph.new(root_seed, definitions, profile_id, unlock_ids)
  assert(definitions, "Route graph requires route definitions")
  local profile = definitions:get_profile(profile_id or "route_profile.legacy.base")
  local route_rng = Rng.new(root_seed):derive("route.graph")
  local graph = setmetatable({
    profile_id = profile.id,
    root_seed = Rng.new(root_seed).seed,
    nodes = {}, edges = {}, node_order = {}, start_node_id = nil,
    current_node_id = nil, completed_node_ids = {}, path = {}, unlock_ids = sorted_ids(unlock_ids or {}),
  }, Graph)
  local layers = {}
  local sequence = 0
  for depth, layer in ipairs(profile.layers) do
    layers[depth] = {}
    for _, source in ipairs(layer.nodes) do
      sequence = sequence + 1
      local id = string.format("route_node:%06d", sequence)
      local node = {
        id = id, key = source.key, type = layer.type, depth = depth,
        biome_id = source.biome_id, tier_id = source.tier_id,
        service_id = source.service_id,
      }
      if layer.type == "floor" then
        node.floor_seed = route_rng:derive("node." .. id):next()
      end
      graph.nodes[id] = node
      graph.node_order[#graph.node_order + 1] = id
      layers[depth][#layers[depth] + 1] = id
      if depth == 1 then graph.start_node_id = id end
    end
  end
  if #(profile.edges or {}) > 0 then
    local by_key = {}
    for _, id in ipairs(graph.node_order) do by_key[graph.nodes[id].key] = id end
    for _, edge in ipairs(profile.edges) do
      graph.edges[#graph.edges + 1] = { from = by_key[edge.from], to = by_key[edge.to], requires_unlock = edge.requires_unlock }
    end
  else
    for depth = 1, #layers - 1 do
      for _, from in ipairs(layers[depth]) do
        for _, to in ipairs(layers[depth + 1]) do
          graph.edges[#graph.edges + 1] = { from = from, to = to }
        end
      end
    end
  end
  graph.current_node_id = graph.start_node_id
  graph.path = { graph.start_node_id }
  assert(graph:validate(definitions))
  return graph
end

function Graph:node(id)
  return self.nodes[id]
end

function Graph:outgoing(id)
  local result = {}
  for _, edge in ipairs(self.edges) do
    if edge.from == id then result[#result + 1] = self.nodes[edge.to] end
  end
  table.sort(result, function(first, second) return first.id < second.id end)
  return result
end

function Graph:has_unlock(unlock_id)
  return unlock_set(self.unlock_ids)[unlock_id] == true
end

function Graph:_edge_available(edge)
  return not edge.requires_unlock or self:has_unlock(edge.requires_unlock)
end

function Graph:available_outgoing(id)
  local result = {}
  for _, edge in ipairs(self.edges) do
    if edge.from == id and self:_edge_available(edge) then result[#result + 1] = self.nodes[edge.to] end
  end
  table.sort(result, function(first, second) return first.id < second.id end)
  return result
end

function Graph:locked_outgoing(id)
  local result = {}
  for _, edge in ipairs(self.edges) do
    if edge.from == id and edge.requires_unlock and not self:_edge_available(edge) then
      result[#result + 1] = { node = self.nodes[edge.to], requires_unlock = edge.requires_unlock }
    end
  end
  table.sort(result, function(first, second) return first.node.id < second.node.id end)
  return result
end

function Graph:incoming(id)
  local result = {}
  for _, edge in ipairs(self.edges) do
    if edge.to == id then result[#result + 1] = self.nodes[edge.from] end
  end
  table.sort(result, function(first, second) return first.id < second.id end)
  return result
end

function Graph:available()
  if not self.completed_node_ids[self.current_node_id] then return {} end
  return self:available_outgoing(self.current_node_id)
end

function Graph:status(id)
  if id == self.current_node_id then return self.completed_node_ids[id] and "completed_current" or "current" end
  if self.completed_node_ids[id] then return "completed" end
  for _, node in ipairs(self:available()) do if node.id == id then return "available" end end
  return "future"
end

function Graph:complete_current()
  local current = self:node(self.current_node_id)
  if not current then return failure("invalid_current", "Route has no current node") end
  if self.completed_node_ids[current.id] then return failure("already_completed", "Current route node is already completed") end
  self.completed_node_ids[current.id] = true
  return { applied = true, node = current, outgoing = self:available_outgoing(current.id) }
end

function Graph:select(id)
  if type(id) ~= "string" or not self.nodes[id] then return failure("unknown_node", "Unknown route node") end
  if not self.completed_node_ids[self.current_node_id] then return failure("current_incomplete", "Current route node is not complete") end
  local selected
  for _, node in ipairs(self:available_outgoing(self.current_node_id)) do if node.id == id then selected = node break end end
  if not selected then return failure("not_connected", "Route node is not reachable from the current node") end
  if self.completed_node_ids[id] then return failure("completed", "Route node is already complete") end
  self.current_node_id = id
  self.path[#self.path + 1] = id
  return { applied = true, node = selected }
end

function Graph:to_data()
  local nodes, edges, completed, path = {}, {}, {}, {}
  for _, id in ipairs(self.node_order) do nodes[#nodes + 1] = copy_node(self.nodes[id]) end
  for _, edge in ipairs(self.edges) do edges[#edges + 1] = { from = edge.from, to = edge.to, requires_unlock = edge.requires_unlock } end
  table.sort(edges, function(first, second)
    return first.from == second.from and first.to < second.to or first.from < second.from
  end)
  for id in pairs(self.completed_node_ids) do completed[#completed + 1] = id end
  completed = sorted_ids(completed)
  for _, id in ipairs(self.path) do path[#path + 1] = id end
  return {
    profile_id = self.profile_id, root_seed = self.root_seed, nodes = nodes, edges = edges,
    start_node_id = self.start_node_id, current_node_id = self.current_node_id,
    completed_node_ids = completed, path = path, unlock_ids = sorted_ids(self.unlock_ids),
  }
end

function Graph.from_data(data, definitions)
  if type(data) ~= "table" then return failure("invalid_route", "Route data must be a table") end
  local graph = setmetatable({
    profile_id = data.profile_id, root_seed = data.root_seed, nodes = {}, edges = {}, node_order = {},
    start_node_id = data.start_node_id, current_node_id = data.current_node_id, completed_node_ids = {}, path = {}, unlock_ids = sorted_ids(data.unlock_ids or {}),
  }, Graph)
  if type(graph.profile_id) ~= "string" then return failure("invalid_route", "Route profile ID is missing") end
  local ok, err = pcall(function() definitions:get_profile(graph.profile_id) end)
  if not ok then return failure("missing_content", tostring(err)) end
  if type(graph.root_seed) ~= "number" or graph.root_seed % 1 ~= 0 then return failure("invalid_route", "Route root seed is invalid") end
  for _, node in ipairs(data.nodes or {}) do
    if type(node) ~= "table" or type(node.id) ~= "string" or graph.nodes[node.id] then
      return failure("invalid_route", "Route node IDs must be unique strings")
    end
    graph.nodes[node.id] = copy_node(node)
    graph.node_order[#graph.node_order + 1] = node.id
  end
  for _, edge in ipairs(data.edges or {}) do graph.edges[#graph.edges + 1] = { from = edge.from, to = edge.to, requires_unlock = edge.requires_unlock } end
  for _, id in ipairs(data.completed_node_ids or {}) do
    if type(id) ~= "string" then return failure("invalid_route", "Completed route node ID is invalid") end
    graph.completed_node_ids[id] = true
  end
  for _, id in ipairs(data.path or {}) do graph.path[#graph.path + 1] = id end
  local valid, failure_data = graph:validate(definitions)
  if not valid then return nil, failure_data end
  return graph
end

function Graph:validate(definitions)
  local function invalid(reason) return failure("invalid_route", reason) end
  local profile_ok = pcall(function() definitions:get_profile(self.profile_id) end)
  if not profile_ok then return invalid("Route references an unknown profile") end
  if type(self.start_node_id) ~= "string" or not self.nodes[self.start_node_id] then return invalid("Route has no valid start node") end
  if type(self.current_node_id) ~= "string" or not self.nodes[self.current_node_id] then return invalid("Route has no valid current node") end
  local bosses, edge_seen = 0, {}
  for _, id in ipairs(self.node_order) do
    local node = self.nodes[id]
    if not node or node.id ~= id or type(node.key) ~= "string" or type(node.depth) ~= "number" then return invalid("Route node data is malformed") end
    if node.type ~= "floor" and node.type ~= "shop" and node.type ~= "boss" then return invalid("Route node '" .. id .. "' has invalid type") end
    if node.type == "floor" then
      local biome_ok = pcall(function() definitions:get_biome(node.biome_id) end)
      local tier_ok = pcall(function() definitions:get_tier(node.tier_id) end)
      if not biome_ok or not tier_ok or type(node.floor_seed) ~= "number" or node.floor_seed % 1 ~= 0
        or (node.service_id ~= nil and (type(node.service_id) ~= "string" or not node.service_id:match("^service%."))) then
        return invalid("Floor node '" .. id .. "' has invalid biome, tier, or seed")
      end
    elseif node.biome_id ~= nil or node.tier_id ~= nil or node.floor_seed ~= nil then
      return invalid("Special node '" .. id .. "' has floor fields")
    end
    if node.type == "boss" then bosses = bosses + 1 end
  end
  if bosses ~= 1 then return invalid("Route must contain exactly one boss node") end
  for _, edge in ipairs(self.edges) do
    if type(edge.from) ~= "string" or type(edge.to) ~= "string" or not self.nodes[edge.from] or not self.nodes[edge.to]
      or (edge.requires_unlock ~= nil and (type(edge.requires_unlock) ~= "string" or not edge.requires_unlock:match("^unlock%.[a-z0-9_%.]+$"))) then
      return invalid("Route edge references an unknown node")
    end
    if self.nodes[edge.from].depth >= self.nodes[edge.to].depth then return invalid("Route edge is not forward") end
    local key = edge.from .. ">" .. edge.to
    if edge_seen[key] then return invalid("Route contains duplicate edge '" .. key .. "'") end
    edge_seen[key] = true
  end
  local visited, queue, cursor = { [self.start_node_id] = true }, { self.start_node_id }, 1
  while queue[cursor] do
    local id = queue[cursor]; cursor = cursor + 1
    for _, node in ipairs(self:outgoing(id)) do
      if not visited[node.id] then visited[node.id] = true; queue[#queue + 1] = node.id end
    end
  end
  for _, id in ipairs(self.node_order) do if not visited[id] then return invalid("Route has unreachable node '" .. id .. "'") end end
  local can_reach_boss, reverse_queue, reverse_cursor = {}, {}, 1
  for _, id in ipairs(self.node_order) do if self.nodes[id].type == "boss" then can_reach_boss[id] = true; reverse_queue[#reverse_queue + 1] = id end end
  while reverse_queue[reverse_cursor] do
    local id = reverse_queue[reverse_cursor]; reverse_cursor = reverse_cursor + 1
    for _, node in ipairs(self:incoming(id)) do if not can_reach_boss[node.id] then can_reach_boss[node.id] = true; reverse_queue[#reverse_queue + 1] = node.id end end
  end
  for _, id in ipairs(self.node_order) do if not can_reach_boss[id] then return invalid("Route node cannot reach boss '" .. id .. "'") end end
  local allowed, queue, cursor = { [self.start_node_id] = true }, { self.start_node_id }, 1
  while queue[cursor] do
    local id = queue[cursor]; cursor = cursor + 1
    for _, node in ipairs(self:available_outgoing(id)) do
      if not allowed[node.id] then allowed[node.id] = true; queue[#queue + 1] = node.id end
    end
  end
  local unlocked_boss = false
  for _, id in ipairs(self.node_order) do if self.nodes[id].type == "boss" and allowed[id] then unlocked_boss = true end end
  if not unlocked_boss then return invalid("Route unlock configuration cannot reach boss") end
  if not self.nodes[self.start_node_id] or self.nodes[self.start_node_id].depth ~= 1 then return invalid("Route start must be in first layer") end
  for id in pairs(self.completed_node_ids) do if not self.nodes[id] then return invalid("Completed route references unknown node") end end
  for _, id in ipairs(self.path) do if not self.nodes[id] then return invalid("Route path references unknown node") end end
  if #self.path == 0 or self.path[#self.path] ~= self.current_node_id then return invalid("Route path does not end at current node") end
  local has_branch = false
  for _, id in ipairs(self.node_order) do if #self:outgoing(id) >= 2 then has_branch = true break end end
  if not has_branch then return invalid("Route has no branching decision") end
  return true
end

return Graph
