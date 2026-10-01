-- Headless structural diagnostics for route graphs.  It only observes the
-- graph produced by the authoritative route module and never generates floors
-- or touches active-run storage.
local RouteGraph = require("src.routes.graph")

local Analysis = {}

local function increment(values, key)
  values[key] = (values[key] or 0) + 1
end

function Analysis.analyze(graph, definitions)
  assert(graph, "Route analysis requires a graph")
  local errors = {}
  local valid, failure = graph:validate(definitions)
  if not valid then errors[#errors + 1] = failure end
  local biome_by_depth, types, degree_counts, nodes = {}, {}, {}, {}
  local branches, convergences = 0, 0
  for _, id in ipairs(graph.node_order) do
    local node = graph:node(id)
    local outgoing, incoming = graph:outgoing(id), graph:incoming(id)
    increment(types, node.type)
    degree_counts[tostring(#outgoing)] = (degree_counts[tostring(#outgoing)] or 0) + 1
    if node.type == "floor" then
      biome_by_depth[node.depth] = biome_by_depth[node.depth] or {}
      increment(biome_by_depth[node.depth], node.biome_id)
    end
    if #outgoing >= 2 then branches = branches + 1 end
    if #incoming >= 2 then convergences = convergences + 1 end
    nodes[#nodes + 1] = {
      id = node.id, key = node.key, type = node.type, depth = node.depth,
      biome_id = node.biome_id, tier_id = node.tier_id, floor_seed = node.floor_seed,
      incoming = #incoming, outgoing = #outgoing, status = graph:status(node.id),
    }
  end
  return {
    format = "roag.route_report",
    version = 1,
    valid = valid == true,
    errors = errors,
    profile_id = graph.profile_id,
    root_seed = graph.root_seed,
    current_node_id = graph.current_node_id,
    path = graph:to_data().path,
    nodes = nodes,
    metrics = {
      node_count = #graph.node_order,
      edge_count = #graph.edges,
      node_types = types,
      biome_by_depth = biome_by_depth,
      branch_count = branches,
      convergence_count = convergences,
      outgoing_degree_counts = degree_counts,
    },
  }
end

function Analysis.batch(options)
  options = options or {}
  local definitions, seed, count = assert(options.definitions, "Route batch requires definitions"), tonumber(options.seed), tonumber(options.count)
  if not seed or seed % 1 ~= 0 then return nil, { code = "invalid_seed", reason = "Seed must be an integer" } end
  if not count or count <= 0 or count % 1 ~= 0 then return nil, { code = "invalid_count", reason = "Count must be a positive integer" } end
  local profile_id = options.profile_id or "route_profile.legacy.base"
  local failures, biome_by_depth, degree_counts = {}, {}, {}
  local branch_total, convergence_total = 0, 0
  for offset = 0, count - 1 do
    local current_seed = seed + offset
    local ok, graph_or_error = pcall(RouteGraph.new, current_seed, definitions, profile_id)
    if not ok then
      failures[#failures + 1] = { seed = current_seed, code = "route_generation_failed", message = tostring(graph_or_error) }
    else
      local report = Analysis.analyze(graph_or_error, definitions)
      if not report.valid then failures[#failures + 1] = { seed = current_seed, code = "invalid_route", errors = report.errors } end
      branch_total = branch_total + report.metrics.branch_count
      convergence_total = convergence_total + report.metrics.convergence_count
      for depth, values in pairs(report.metrics.biome_by_depth) do
        biome_by_depth[depth] = biome_by_depth[depth] or {}
        for biome_id, value in pairs(values) do
          biome_by_depth[depth][biome_id] = (biome_by_depth[depth][biome_id] or 0) + value
        end
      end
      for degree, value in pairs(report.metrics.outgoing_degree_counts) do degree_counts[degree] = (degree_counts[degree] or 0) + value end
    end
  end
  return {
    format = "roag.route_report",
    version = 1,
    options = { profile_id = profile_id, seed = seed, count = count },
    summary = {
      generated = count - #failures, failures = #failures,
      biome_by_depth = biome_by_depth, outgoing_degree_counts = degree_counts,
      average_branch_count = (count - #failures) > 0 and branch_total / (count - #failures) or 0,
      average_convergence_count = (count - #failures) > 0 and convergence_total / (count - #failures) or 0,
    },
    failures = failures,
  }
end

return Analysis
