// 4d790b63eac9e28679b5d9f5611180bbee0b9981
import { rngFor } from './rng';
export const MACRO_TOPOLOGIES = ['tight', 'broad', 'vertical', 'directedFlow', 'looped'];
const role = (width, height, landmark, terrain, visual) => ({
  footprint: {
    width,
    height
  },
  landmark,
  terrain,
  visual
});
const edgeRole = (connector, terrain, visual) => ({
  connector,
  terrain,
  visual
});
const nodeRoles = (landmark, terrain) => ({
  start: role(6, 6, 'entry', 'floor', 'entry-marker'),
  landmark: role(7, 6, landmark, terrain, 'landmark-marker'),
  fork: role(6, 6, 'fork', terrain, 'choice-marker'),
  objective: role(7, 6, 'objective', terrain, 'objective-marker'),
  optionalReward: role(6, 6, 'reward', terrain, 'reward-marker'),
  exit: role(4, 5, 'exit', 'floor', 'exit-marker'),
  boss: role(7, 7, 'boss', terrain, 'boss-marker')
});
const edgeRoles = terrain => ({
  main: edgeRole('horizontalFirst', terrain, 'main-route'),
  safe: edgeRole('horizontalFirst', terrain, 'safe-route'),
  costly: edgeRole('verticalFirst', terrain, 'costly-route'),
  optional: edgeRole('verticalFirst', terrain, 'optional-route')
});
const tight = {
  start: {
    x: 11,
    y: 48
  },
  landmark: {
    x: 28,
    y: 48
  },
  fork: {
    x: 48,
    y: 48
  },
  objective: {
    x: 76,
    y: 48
  },
  optionalReward: {
    x: 63,
    y: 77
  },
  exit: {
    x: 92,
    y: 48
  },
  boss: {
    x: 91,
    y: 48
  }
};
const broad = {
  start: {
    x: 10,
    y: 52
  },
  landmark: {
    x: 28,
    y: 30
  },
  fork: {
    x: 49,
    y: 52
  },
  objective: {
    x: 72,
    y: 28
  },
  optionalReward: {
    x: 74,
    y: 78
  },
  exit: {
    x: 93,
    y: 52
  },
  boss: {
    x: 91,
    y: 52
  }
};
const vertical = {
  start: {
    x: 22,
    y: 84
  },
  landmark: {
    x: 32,
    y: 64
  },
  fork: {
    x: 45,
    y: 48
  },
  objective: {
    x: 70,
    y: 18
  },
  optionalReward: {
    x: 76,
    y: 66
  },
  exit: {
    x: 91,
    y: 12
  },
  boss: {
    x: 91,
    y: 14
  }
};
const directedFlow = {
  start: {
    x: 9,
    y: 28
  },
  landmark: {
    x: 28,
    y: 68
  },
  fork: {
    x: 48,
    y: 42
  },
  objective: {
    x: 80,
    y: 70
  },
  optionalReward: {
    x: 70,
    y: 18
  },
  exit: {
    x: 93,
    y: 38
  },
  boss: {
    x: 91,
    y: 42
  }
};
const looped = {
  start: {
    x: 10,
    y: 50
  },
  landmark: {
    x: 27,
    y: 26
  },
  fork: {
    x: 48,
    y: 42
  },
  objective: {
    x: 74,
    y: 50
  },
  optionalReward: {
    x: 48,
    y: 77
  },
  exit: {
    x: 93,
    y: 50
  },
  boss: {
    x: 91,
    y: 50
  }
};
const recipe = (id, biome, topology, anchors, pilot = false) => ({
  id,
  biome,
  topology,
  pilot,
  nodeRoles: nodeRoles(`${biome}-landmark`, topology === 'directedFlow' ? 'current' : topology === 'vertical' ? 'ledge' : 'floor'),
  edgeRoles: edgeRoles(topology === 'directedFlow' ? 'current' : topology === 'vertical' ? 'ledge' : 'floor'),
  anchors
});
const recipes = [recipe('mine:rail-spine', 'mine', 'tight', tight, true), recipe('mine:branching-drifts', 'mine', 'broad', broad, true), recipe('mine:collapse-loop', 'mine', 'looped', looped, true), recipe('caverns:tide-chambers', 'caverns', 'directedFlow', directedFlow, true), recipe('caverns:sinkhole-galleries', 'caverns', 'vertical', vertical, true), recipe('caverns:fault-tunnels', 'caverns', 'looped', looped, true), recipe('wilds:river-clearings', 'wilds', 'broad', broad, true), recipe('wilds:root-maze', 'wilds', 'looped', looped, true), recipe('wilds:wetland-causeways', 'wilds', 'directedFlow', directedFlow, true), recipe('ruins:circular-precinct', 'ruins', 'broad', broad, true), recipe('ruins:broken-processional-loop', 'ruins', 'looped', looped, true), recipe('ruins:courtyard-lattice', 'ruins', 'directedFlow', directedFlow, true), recipe('furnace:stepped-kiln-chain', 'furnace', 'vertical', vertical, true), recipe('furnace:smoke-choked-service-route', 'furnace', 'directedFlow', directedFlow, true), recipe('furnace:lift-and-ash-loop', 'furnace', 'looped', looped, true), recipe('floodedRuins:braided-current-delta', 'floodedRuins', 'directedFlow', directedFlow, true), recipe('floodedRuins:anchor-gated-ruin', 'floodedRuins', 'looped', looped, true), recipe('floodedRuins:island-hop-network', 'floodedRuins', 'broad', broad, true), recipe('cliffs:switchback-face', 'cliffs', 'vertical', vertical, true), recipe('cliffs:ravine-bridge-loop', 'cliffs', 'looped', looped, true), recipe('cliffs:anchor-chain', 'cliffs', 'directedFlow', directedFlow, true), recipe('burial:stone-circle-center', 'burial', 'broad', broad, true), recipe('burial:mound-procession', 'burial', 'vertical', vertical, true), recipe('burial:cemetery-settlement-edge', 'burial', 'tight', tight, true), recipe('burial:ossuary-hollow', 'burial', 'directedFlow', directedFlow, true), recipe('burial:ancestor-path-loop', 'burial', 'looped', looped, true), recipe('saltFlats:crust-island-chain', 'saltFlats', 'broad', broad, true), recipe('saltFlats:brine-maze', 'saltFlats', 'directedFlow', directedFlow, true), recipe('saltFlats:caravan-causeway', 'saltFlats', 'vertical', vertical, true), recipe('saltFlats:mirror-basin-loop', 'saltFlats', 'looped', looped, true), recipe('saltFlats:salt-ridge-refuge', 'saltFlats', 'tight', tight, true), recipe('frostReliquary:frozen-lake-crossing', 'frostReliquary', 'broad', broad, true), recipe('frostReliquary:ridge-hollow-loop', 'frostReliquary', 'looped', looped, true), recipe('frostReliquary:pressure-crack-maze', 'frostReliquary', 'directedFlow', directedFlow, true), recipe('frostReliquary:shore-reliquary-route', 'frostReliquary', 'vertical', vertical, true), recipe('frostReliquary:storm-refuge-chain', 'frostReliquary', 'tight', tight, true), recipe('wilds:legacy', 'wilds', 'broad', broad), recipe('caverns:legacy', 'caverns', 'tight', tight), recipe('ruins:legacy', 'ruins', 'looped', looped), recipe('furnace:legacy', 'furnace', 'vertical', vertical), recipe('floodedRuins:legacy', 'floodedRuins', 'directedFlow', directedFlow), recipe('cliffs:legacy', 'cliffs', 'vertical', vertical), recipe('burial:legacy', 'burial', 'looped', looped), recipe('saltFlats:legacy', 'saltFlats', 'broad', broad), recipe('frostReliquary:legacy', 'frostReliquary', 'tight', tight)];
const baseRecipeId = id => id.replace(/-remix$/, '');
export const macroRecipeFor = contract => {
  var _recipes$find;
  return (_recipes$find = recipes.find(candidate => candidate.id === `${contract.biome}:${baseRecipeId(contract.recipeId)}`)) !== null && _recipes$find !== void 0 ? _recipes$find : recipes.find(candidate => candidate.id === `${contract.biome}:legacy`);
};
const center = footprint => ({
  x: footprint.x + Math.floor(footprint.width / 2),
  y: footprint.y + Math.floor(footprint.height / 2)
});
const inside = (point, width, height) => point.x > 0 && point.y > 0 && point.x < width - 1 && point.y < height - 1;
const overlaps = (left, right) => left.x < right.x + right.width && left.x + left.width > right.x && left.y < right.y + right.height && left.y + left.height > right.y;
const same = (left, right) => left.x === right.x && left.y === right.y;
const axisPath = (from, to, horizontalFirst) => {
  const cells = [];
  const append = point => {
    if (!cells.length || !same(cells[cells.length - 1], point)) cells.push(point);
  };
  const line = (start, end) => {
    const dx = Math.sign(end.x - start.x);
    const dy = Math.sign(end.y - start.y);
    for (let x = start.x, y = start.y;; x += dx, y += dy) {
      append({
        x,
        y
      });
      if (x === end.x && y === end.y) return;
    }
  };
  const bend = horizontalFirst ? {
    x: to.x,
    y: from.y
  } : {
    x: from.x,
    y: to.y
  };
  line(from, bend);
  line(bend, to);
  return cells;
};
const connectorPoint = (from, to) => {
  const fromCenter = center(from);
  const toCenter = center(to);
  if (Math.abs(toCenter.x - fromCenter.x) >= Math.abs(toCenter.y - fromCenter.y)) return {
    x: toCenter.x >= fromCenter.x ? from.x + from.width - 1 : from.x,
    y: Math.max(from.y, Math.min(from.y + from.height - 1, toCenter.y))
  };
  return {
    x: Math.max(from.x, Math.min(from.x + from.width - 1, toCenter.x)),
    y: toCenter.y >= fromCenter.y ? from.y + from.height - 1 : from.y
  };
};
const edgeMode = edge => edge.modes.includes('optional') ? 'optional' : edge.modes.includes('costly') ? 'costly' : edge.modes.includes('safe') ? 'safe' : 'main';
const compileAttempt = (contract, recipe, options, attempt) => {
  const diagnostics = [];
  const jitter = attempt ? rngFor(contract.campaignSeed, 'generation', contract.floorIndex, 'macro-retry', recipe.id, attempt) : undefined;
  const nodes = [];
  for (const node of contract.nodes) {
    var _jitter$int, _jitter$int2;
    const role = recipe.nodeRoles[node.kind];
    if (!role) {
      diagnostics.push(`node ${node.id}: recipe ${recipe.id} has no role`);
      continue;
    }
    const anchor = recipe.anchors[node.kind];
    const x = Math.round(anchor.x / 100 * (options.width - 1)) - Math.floor(role.footprint.width / 2) + ((_jitter$int = jitter === null || jitter === void 0 ? void 0 : jitter.int(-2, 2)) !== null && _jitter$int !== void 0 ? _jitter$int : 0);
    const y = Math.round(anchor.y / 100 * (options.height - 1)) - Math.floor(role.footprint.height / 2) + ((_jitter$int2 = jitter === null || jitter === void 0 ? void 0 : jitter.int(-2, 2)) !== null && _jitter$int2 !== void 0 ? _jitter$int2 : 0);
    const footprint = {
      x,
      y,
      ...role.footprint
    };
    if (!inside({
      x,
      y
    }, options.width, options.height) || !inside({
      x: x + footprint.width - 1,
      y: y + footprint.height - 1
    }, options.width, options.height)) diagnostics.push(`node ${node.id}: footprint exceeds ${options.width}x${options.height} on attempt ${attempt}`);
    const placed = {
      nodeId: node.id,
      kind: node.kind,
      footprint,
      terrain: role.terrain,
      landmark: role.landmark,
      visual: role.visual
    };
    const previous = nodes.find(other => overlaps(other.footprint, footprint));
    if (previous) diagnostics.push(`node ${node.id}: footprint overlaps node ${previous.nodeId} on attempt ${attempt}`);
    nodes.push(placed);
  }
  const byId = new Map(nodes.map(node => [node.nodeId, node]));
  const edges = [];
  for (const edge of contract.edges) {
    const from = byId.get(edge.from);
    const to = byId.get(edge.to);
    if (!from || !to) {
      diagnostics.push(`edge ${edge.id}: missing endpoint footprint on attempt ${attempt}`);
      continue;
    }
    const role = recipe.edgeRoles[edgeMode(edge)];
    const fromPoint = connectorPoint(from.footprint, to.footprint);
    const toPoint = connectorPoint(to.footprint, from.footprint);
    const cells = axisPath(fromPoint, toPoint, role.connector === 'horizontalFirst');
    const invalid = cells.find(point => !inside(point, options.width, options.height));
    if (invalid) diagnostics.push(`edge ${edge.id}: connector leaves bounds at ${invalid.x},${invalid.y} on attempt ${attempt}`);
    edges.push({
      edgeId: edge.id,
      modes: [...edge.modes],
      from: fromPoint,
      to: toPoint,
      cells,
      terrain: role.terrain,
      visual: role.visual
    });
  }
  return {
    valid: !diagnostics.length,
    recipeId: recipe.id,
    topology: recipe.topology,
    attempt,
    nodes,
    edges,
    diagnostics
  };
};
export const compileRouteContract = (contract, options) => {
  var _options$attempts;
  const recipe = macroRecipeFor(contract);
  const attempts = (_options$attempts = options.attempts) !== null && _options$attempts !== void 0 ? _options$attempts : 3;
  let failed;
  for (let attempt = 0; attempt < attempts; attempt++) {
    const compiled = compileAttempt(contract, recipe, options, attempt);
    if (compiled.valid) return compiled;
    failed = compiled;
  }
  return {
    ...failed,
    diagnostics: Array.from({
      length: attempts
    }, (_, attempt) => compileAttempt(contract, recipe, options, attempt).diagnostics.map(error => `attempt ${attempt}: ${error}`)).flat()
  };
};
export const macroConnectorPoints = debug => debug.edges.flatMap(edge => edge.cells);
export const validateMacroRealization = (debug, traversable) => debug.edges.flatMap(edge => {
  const blocked = edge.cells.find(point => !traversable(point));
  return blocked ? [`edge ${edge.edgeId}: connector blocked at ${blocked.x},${blocked.y}`] : [];
});
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJybmdGb3IiLCJNQUNST19UT1BPTE9HSUVTIiwicm9sZSIsIndpZHRoIiwiaGVpZ2h0IiwibGFuZG1hcmsiLCJ0ZXJyYWluIiwidmlzdWFsIiwiZm9vdHByaW50IiwiZWRnZVJvbGUiLCJjb25uZWN0b3IiLCJub2RlUm9sZXMiLCJzdGFydCIsImZvcmsiLCJvYmplY3RpdmUiLCJvcHRpb25hbFJld2FyZCIsImV4aXQiLCJib3NzIiwiZWRnZVJvbGVzIiwibWFpbiIsInNhZmUiLCJjb3N0bHkiLCJvcHRpb25hbCIsInRpZ2h0IiwieCIsInkiLCJicm9hZCIsInZlcnRpY2FsIiwiZGlyZWN0ZWRGbG93IiwibG9vcGVkIiwicmVjaXBlIiwiaWQiLCJiaW9tZSIsInRvcG9sb2d5IiwiYW5jaG9ycyIsInBpbG90IiwicmVjaXBlcyIsImJhc2VSZWNpcGVJZCIsInJlcGxhY2UiLCJtYWNyb1JlY2lwZUZvciIsImNvbnRyYWN0IiwiX3JlY2lwZXMkZmluZCIsImZpbmQiLCJjYW5kaWRhdGUiLCJyZWNpcGVJZCIsImNlbnRlciIsIk1hdGgiLCJmbG9vciIsImluc2lkZSIsInBvaW50Iiwib3ZlcmxhcHMiLCJsZWZ0IiwicmlnaHQiLCJzYW1lIiwiYXhpc1BhdGgiLCJmcm9tIiwidG8iLCJob3Jpem9udGFsRmlyc3QiLCJjZWxscyIsImFwcGVuZCIsImxlbmd0aCIsInB1c2giLCJsaW5lIiwiZW5kIiwiZHgiLCJzaWduIiwiZHkiLCJiZW5kIiwiY29ubmVjdG9yUG9pbnQiLCJmcm9tQ2VudGVyIiwidG9DZW50ZXIiLCJhYnMiLCJtYXgiLCJtaW4iLCJlZGdlTW9kZSIsImVkZ2UiLCJtb2RlcyIsImluY2x1ZGVzIiwiY29tcGlsZUF0dGVtcHQiLCJvcHRpb25zIiwiYXR0ZW1wdCIsImRpYWdub3N0aWNzIiwiaml0dGVyIiwiY2FtcGFpZ25TZWVkIiwiZmxvb3JJbmRleCIsInVuZGVmaW5lZCIsIm5vZGVzIiwibm9kZSIsIl9qaXR0ZXIkaW50IiwiX2ppdHRlciRpbnQyIiwia2luZCIsImFuY2hvciIsInJvdW5kIiwiaW50IiwicGxhY2VkIiwibm9kZUlkIiwicHJldmlvdXMiLCJvdGhlciIsImJ5SWQiLCJNYXAiLCJtYXAiLCJlZGdlcyIsImdldCIsImZyb21Qb2ludCIsInRvUG9pbnQiLCJpbnZhbGlkIiwiZWRnZUlkIiwidmFsaWQiLCJjb21waWxlUm91dGVDb250cmFjdCIsIl9vcHRpb25zJGF0dGVtcHRzIiwiYXR0ZW1wdHMiLCJmYWlsZWQiLCJjb21waWxlZCIsIkFycmF5IiwiXyIsImVycm9yIiwiZmxhdCIsIm1hY3JvQ29ubmVjdG9yUG9pbnRzIiwiZGVidWciLCJmbGF0TWFwIiwidmFsaWRhdGVNYWNyb1JlYWxpemF0aW9uIiwidHJhdmVyc2FibGUiLCJibG9ja2VkIl0sInNvdXJjZXMiOlsibWFjcm8tcmVjaXBlLnRzIl0sInNvdXJjZXNDb250ZW50IjpbImltcG9ydCB7IHJuZ0ZvciB9IGZyb20gJy4vcm5nJ1xuaW1wb3J0IHR5cGUgeyBCaW9tZSwgUG9pbnQgfSBmcm9tICcuL3R5cGVzJ1xuaW1wb3J0IHR5cGUgeyBSb3V0ZUNvbnRyYWN0LCBSb3V0ZUVkZ2UsIFJvdXRlRWRnZU1vZGUsIFJvdXRlTm9kZUtpbmQgfSBmcm9tICcuL3JvdXRlLWNvbnRyYWN0J1xuXG5leHBvcnQgY29uc3QgTUFDUk9fVE9QT0xPR0lFUyA9IFsndGlnaHQnLCAnYnJvYWQnLCAndmVydGljYWwnLCAnZGlyZWN0ZWRGbG93JywgJ2xvb3BlZCddIGFzIGNvbnN0XG5leHBvcnQgdHlwZSBNYWNyb1RvcG9sb2d5ID0gdHlwZW9mIE1BQ1JPX1RPUE9MT0dJRVNbbnVtYmVyXVxuXG5leHBvcnQgaW50ZXJmYWNlIE1hY3JvRm9vdHByaW50IHsgd2lkdGg6IG51bWJlcjsgaGVpZ2h0OiBudW1iZXIgfVxuZXhwb3J0IGludGVyZmFjZSBNYWNyb05vZGVSb2xlIHsgZm9vdHByaW50OiBNYWNyb0Zvb3RwcmludDsgbGFuZG1hcms6IHN0cmluZzsgdGVycmFpbjogc3RyaW5nOyB2aXN1YWw6IHN0cmluZyB9XG5leHBvcnQgaW50ZXJmYWNlIE1hY3JvRWRnZVJvbGUgeyBjb25uZWN0b3I6ICdob3Jpem9udGFsRmlyc3QnIHwgJ3ZlcnRpY2FsRmlyc3QnOyB0ZXJyYWluOiBzdHJpbmc7IHZpc3VhbDogc3RyaW5nIH1cbmV4cG9ydCBpbnRlcmZhY2UgUm91dGVNYWNyb1JlY2lwZSB7XG4gIGlkOiBzdHJpbmdcbiAgYmlvbWU6IEJpb21lXG4gIHRvcG9sb2d5OiBNYWNyb1RvcG9sb2d5XG4gIHBpbG90OiBib29sZWFuXG4gIG5vZGVSb2xlczogUmVjb3JkPFJvdXRlTm9kZUtpbmQsIE1hY3JvTm9kZVJvbGU+XG4gIGVkZ2VSb2xlczogUmVjb3JkPFJvdXRlRWRnZU1vZGUsIE1hY3JvRWRnZVJvbGU+XG4gIGFuY2hvcnM6IFJlY29yZDxSb3V0ZU5vZGVLaW5kLCBQb2ludD5cbn1cblxuZXhwb3J0IGludGVyZmFjZSBNYWNyb05vZGVQbGFjZW1lbnQgeyBub2RlSWQ6IHN0cmluZzsga2luZDogUm91dGVOb2RlS2luZDsgZm9vdHByaW50OiB7IHg6IG51bWJlcjsgeTogbnVtYmVyOyB3aWR0aDogbnVtYmVyOyBoZWlnaHQ6IG51bWJlciB9OyB0ZXJyYWluOiBzdHJpbmc7IGxhbmRtYXJrOiBzdHJpbmc7IHZpc3VhbDogc3RyaW5nIH1cbmV4cG9ydCBpbnRlcmZhY2UgTWFjcm9FZGdlUGxhY2VtZW50IHsgZWRnZUlkOiBzdHJpbmc7IG1vZGVzOiBSb3V0ZUVkZ2VNb2RlW107IGZyb206IFBvaW50OyB0bzogUG9pbnQ7IGNlbGxzOiBQb2ludFtdOyB0ZXJyYWluOiBzdHJpbmc7IHZpc3VhbDogc3RyaW5nIH1cbmV4cG9ydCBpbnRlcmZhY2UgTWFjcm9SZWNpcGVEZWJ1ZyB7XG4gIHZhbGlkOiBib29sZWFuXG4gIHJlY2lwZUlkOiBzdHJpbmdcbiAgdG9wb2xvZ3k6IE1hY3JvVG9wb2xvZ3lcbiAgYXR0ZW1wdDogbnVtYmVyXG4gIG5vZGVzOiBNYWNyb05vZGVQbGFjZW1lbnRbXVxuICBlZGdlczogTWFjcm9FZGdlUGxhY2VtZW50W11cbiAgZGlhZ25vc3RpY3M6IHN0cmluZ1tdXG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgTWFjcm9Db21waWxlT3B0aW9ucyB7IHdpZHRoOiBudW1iZXI7IGhlaWdodDogbnVtYmVyOyBhdHRlbXB0cz86IG51bWJlciB9XG5cbmNvbnN0IHJvbGUgPSAod2lkdGg6IG51bWJlciwgaGVpZ2h0OiBudW1iZXIsIGxhbmRtYXJrOiBzdHJpbmcsIHRlcnJhaW46IHN0cmluZywgdmlzdWFsOiBzdHJpbmcpOiBNYWNyb05vZGVSb2xlID0+ICh7IGZvb3RwcmludDogeyB3aWR0aCwgaGVpZ2h0IH0sIGxhbmRtYXJrLCB0ZXJyYWluLCB2aXN1YWwgfSlcbmNvbnN0IGVkZ2VSb2xlID0gKGNvbm5lY3RvcjogTWFjcm9FZGdlUm9sZVsnY29ubmVjdG9yJ10sIHRlcnJhaW46IHN0cmluZywgdmlzdWFsOiBzdHJpbmcpOiBNYWNyb0VkZ2VSb2xlID0+ICh7IGNvbm5lY3RvciwgdGVycmFpbiwgdmlzdWFsIH0pXG5jb25zdCBub2RlUm9sZXMgPSAobGFuZG1hcms6IHN0cmluZywgdGVycmFpbjogc3RyaW5nKTogUmVjb3JkPFJvdXRlTm9kZUtpbmQsIE1hY3JvTm9kZVJvbGU+ID0+ICh7XG4gIHN0YXJ0OiByb2xlKDYsIDYsICdlbnRyeScsICdmbG9vcicsICdlbnRyeS1tYXJrZXInKSxcbiAgbGFuZG1hcms6IHJvbGUoNywgNiwgbGFuZG1hcmssIHRlcnJhaW4sICdsYW5kbWFyay1tYXJrZXInKSxcbiAgZm9yazogcm9sZSg2LCA2LCAnZm9yaycsIHRlcnJhaW4sICdjaG9pY2UtbWFya2VyJyksXG4gIG9iamVjdGl2ZTogcm9sZSg3LCA2LCAnb2JqZWN0aXZlJywgdGVycmFpbiwgJ29iamVjdGl2ZS1tYXJrZXInKSxcbiAgb3B0aW9uYWxSZXdhcmQ6IHJvbGUoNiwgNiwgJ3Jld2FyZCcsIHRlcnJhaW4sICdyZXdhcmQtbWFya2VyJyksXG4gIGV4aXQ6IHJvbGUoNCwgNSwgJ2V4aXQnLCAnZmxvb3InLCAnZXhpdC1tYXJrZXInKSxcbiAgYm9zczogcm9sZSg3LCA3LCAnYm9zcycsIHRlcnJhaW4sICdib3NzLW1hcmtlcicpXG59KVxuY29uc3QgZWRnZVJvbGVzID0gKHRlcnJhaW46IHN0cmluZyk6IFJlY29yZDxSb3V0ZUVkZ2VNb2RlLCBNYWNyb0VkZ2VSb2xlPiA9PiAoe1xuICBtYWluOiBlZGdlUm9sZSgnaG9yaXpvbnRhbEZpcnN0JywgdGVycmFpbiwgJ21haW4tcm91dGUnKSxcbiAgc2FmZTogZWRnZVJvbGUoJ2hvcml6b250YWxGaXJzdCcsIHRlcnJhaW4sICdzYWZlLXJvdXRlJyksXG4gIGNvc3RseTogZWRnZVJvbGUoJ3ZlcnRpY2FsRmlyc3QnLCB0ZXJyYWluLCAnY29zdGx5LXJvdXRlJyksXG4gIG9wdGlvbmFsOiBlZGdlUm9sZSgndmVydGljYWxGaXJzdCcsIHRlcnJhaW4sICdvcHRpb25hbC1yb3V0ZScpXG59KVxuXG5jb25zdCB0aWdodDogUmVjb3JkPFJvdXRlTm9kZUtpbmQsIFBvaW50PiA9IHsgc3RhcnQ6IHsgeDogMTEsIHk6IDQ4IH0sIGxhbmRtYXJrOiB7IHg6IDI4LCB5OiA0OCB9LCBmb3JrOiB7IHg6IDQ4LCB5OiA0OCB9LCBvYmplY3RpdmU6IHsgeDogNzYsIHk6IDQ4IH0sIG9wdGlvbmFsUmV3YXJkOiB7IHg6IDYzLCB5OiA3NyB9LCBleGl0OiB7IHg6IDkyLCB5OiA0OCB9LCBib3NzOiB7IHg6IDkxLCB5OiA0OCB9IH1cbmNvbnN0IGJyb2FkOiBSZWNvcmQ8Um91dGVOb2RlS2luZCwgUG9pbnQ+ID0geyBzdGFydDogeyB4OiAxMCwgeTogNTIgfSwgbGFuZG1hcms6IHsgeDogMjgsIHk6IDMwIH0sIGZvcms6IHsgeDogNDksIHk6IDUyIH0sIG9iamVjdGl2ZTogeyB4OiA3MiwgeTogMjggfSwgb3B0aW9uYWxSZXdhcmQ6IHsgeDogNzQsIHk6IDc4IH0sIGV4aXQ6IHsgeDogOTMsIHk6IDUyIH0sIGJvc3M6IHsgeDogOTEsIHk6IDUyIH0gfVxuY29uc3QgdmVydGljYWw6IFJlY29yZDxSb3V0ZU5vZGVLaW5kLCBQb2ludD4gPSB7IHN0YXJ0OiB7IHg6IDIyLCB5OiA4NCB9LCBsYW5kbWFyazogeyB4OiAzMiwgeTogNjQgfSwgZm9yazogeyB4OiA0NSwgeTogNDggfSwgb2JqZWN0aXZlOiB7IHg6IDcwLCB5OiAxOCB9LCBvcHRpb25hbFJld2FyZDogeyB4OiA3NiwgeTogNjYgfSwgZXhpdDogeyB4OiA5MSwgeTogMTIgfSwgYm9zczogeyB4OiA5MSwgeTogMTQgfSB9XG5jb25zdCBkaXJlY3RlZEZsb3c6IFJlY29yZDxSb3V0ZU5vZGVLaW5kLCBQb2ludD4gPSB7IHN0YXJ0OiB7IHg6IDksIHk6IDI4IH0sIGxhbmRtYXJrOiB7IHg6IDI4LCB5OiA2OCB9LCBmb3JrOiB7IHg6IDQ4LCB5OiA0MiB9LCBvYmplY3RpdmU6IHsgeDogODAsIHk6IDcwIH0sIG9wdGlvbmFsUmV3YXJkOiB7IHg6IDcwLCB5OiAxOCB9LCBleGl0OiB7IHg6IDkzLCB5OiAzOCB9LCBib3NzOiB7IHg6IDkxLCB5OiA0MiB9IH1cbmNvbnN0IGxvb3BlZDogUmVjb3JkPFJvdXRlTm9kZUtpbmQsIFBvaW50PiA9IHsgc3RhcnQ6IHsgeDogMTAsIHk6IDUwIH0sIGxhbmRtYXJrOiB7IHg6IDI3LCB5OiAyNiB9LCBmb3JrOiB7IHg6IDQ4LCB5OiA0MiB9LCBvYmplY3RpdmU6IHsgeDogNzQsIHk6IDUwIH0sIG9wdGlvbmFsUmV3YXJkOiB7IHg6IDQ4LCB5OiA3NyB9LCBleGl0OiB7IHg6IDkzLCB5OiA1MCB9LCBib3NzOiB7IHg6IDkxLCB5OiA1MCB9IH1cblxuY29uc3QgcmVjaXBlID0gKGlkOiBzdHJpbmcsIGJpb21lOiBCaW9tZSwgdG9wb2xvZ3k6IE1hY3JvVG9wb2xvZ3ksIGFuY2hvcnM6IFJlY29yZDxSb3V0ZU5vZGVLaW5kLCBQb2ludD4sIHBpbG90ID0gZmFsc2UpOiBSb3V0ZU1hY3JvUmVjaXBlID0+ICh7IGlkLCBiaW9tZSwgdG9wb2xvZ3ksIHBpbG90LCBub2RlUm9sZXM6IG5vZGVSb2xlcyhgJHtiaW9tZX0tbGFuZG1hcmtgLCB0b3BvbG9neSA9PT0gJ2RpcmVjdGVkRmxvdycgPyAnY3VycmVudCcgOiB0b3BvbG9neSA9PT0gJ3ZlcnRpY2FsJyA/ICdsZWRnZScgOiAnZmxvb3InKSwgZWRnZVJvbGVzOiBlZGdlUm9sZXModG9wb2xvZ3kgPT09ICdkaXJlY3RlZEZsb3cnID8gJ2N1cnJlbnQnIDogdG9wb2xvZ3kgPT09ICd2ZXJ0aWNhbCcgPyAnbGVkZ2UnIDogJ2Zsb29yJyksIGFuY2hvcnMgfSlcbmNvbnN0IHJlY2lwZXM6IFJvdXRlTWFjcm9SZWNpcGVbXSA9IFtcbiAgcmVjaXBlKCdtaW5lOnJhaWwtc3BpbmUnLCAnbWluZScsICd0aWdodCcsIHRpZ2h0LCB0cnVlKSxcbiAgcmVjaXBlKCdtaW5lOmJyYW5jaGluZy1kcmlmdHMnLCAnbWluZScsICdicm9hZCcsIGJyb2FkLCB0cnVlKSxcbiAgcmVjaXBlKCdtaW5lOmNvbGxhcHNlLWxvb3AnLCAnbWluZScsICdsb29wZWQnLCBsb29wZWQsIHRydWUpLFxuICByZWNpcGUoJ2NhdmVybnM6dGlkZS1jaGFtYmVycycsICdjYXZlcm5zJywgJ2RpcmVjdGVkRmxvdycsIGRpcmVjdGVkRmxvdywgdHJ1ZSksXG4gIHJlY2lwZSgnY2F2ZXJuczpzaW5raG9sZS1nYWxsZXJpZXMnLCAnY2F2ZXJucycsICd2ZXJ0aWNhbCcsIHZlcnRpY2FsLCB0cnVlKSxcbiAgcmVjaXBlKCdjYXZlcm5zOmZhdWx0LXR1bm5lbHMnLCAnY2F2ZXJucycsICdsb29wZWQnLCBsb29wZWQsIHRydWUpLFxuICByZWNpcGUoJ3dpbGRzOnJpdmVyLWNsZWFyaW5ncycsICd3aWxkcycsICdicm9hZCcsIGJyb2FkLCB0cnVlKSxcbiAgcmVjaXBlKCd3aWxkczpyb290LW1hemUnLCAnd2lsZHMnLCAnbG9vcGVkJywgbG9vcGVkLCB0cnVlKSxcbiAgcmVjaXBlKCd3aWxkczp3ZXRsYW5kLWNhdXNld2F5cycsICd3aWxkcycsICdkaXJlY3RlZEZsb3cnLCBkaXJlY3RlZEZsb3csIHRydWUpLFxuICByZWNpcGUoJ3J1aW5zOmNpcmN1bGFyLXByZWNpbmN0JywgJ3J1aW5zJywgJ2Jyb2FkJywgYnJvYWQsIHRydWUpLFxuICByZWNpcGUoJ3J1aW5zOmJyb2tlbi1wcm9jZXNzaW9uYWwtbG9vcCcsICdydWlucycsICdsb29wZWQnLCBsb29wZWQsIHRydWUpLFxuICByZWNpcGUoJ3J1aW5zOmNvdXJ0eWFyZC1sYXR0aWNlJywgJ3J1aW5zJywgJ2RpcmVjdGVkRmxvdycsIGRpcmVjdGVkRmxvdywgdHJ1ZSksXG4gIHJlY2lwZSgnZnVybmFjZTpzdGVwcGVkLWtpbG4tY2hhaW4nLCAnZnVybmFjZScsICd2ZXJ0aWNhbCcsIHZlcnRpY2FsLCB0cnVlKSxcbiAgcmVjaXBlKCdmdXJuYWNlOnNtb2tlLWNob2tlZC1zZXJ2aWNlLXJvdXRlJywgJ2Z1cm5hY2UnLCAnZGlyZWN0ZWRGbG93JywgZGlyZWN0ZWRGbG93LCB0cnVlKSxcbiAgcmVjaXBlKCdmdXJuYWNlOmxpZnQtYW5kLWFzaC1sb29wJywgJ2Z1cm5hY2UnLCAnbG9vcGVkJywgbG9vcGVkLCB0cnVlKSxcbiAgcmVjaXBlKCdmbG9vZGVkUnVpbnM6YnJhaWRlZC1jdXJyZW50LWRlbHRhJywgJ2Zsb29kZWRSdWlucycsICdkaXJlY3RlZEZsb3cnLCBkaXJlY3RlZEZsb3csIHRydWUpLFxuICByZWNpcGUoJ2Zsb29kZWRSdWluczphbmNob3ItZ2F0ZWQtcnVpbicsICdmbG9vZGVkUnVpbnMnLCAnbG9vcGVkJywgbG9vcGVkLCB0cnVlKSxcbiAgcmVjaXBlKCdmbG9vZGVkUnVpbnM6aXNsYW5kLWhvcC1uZXR3b3JrJywgJ2Zsb29kZWRSdWlucycsICdicm9hZCcsIGJyb2FkLCB0cnVlKSxcbiAgcmVjaXBlKCdjbGlmZnM6c3dpdGNoYmFjay1mYWNlJywgJ2NsaWZmcycsICd2ZXJ0aWNhbCcsIHZlcnRpY2FsLCB0cnVlKSxcbiAgcmVjaXBlKCdjbGlmZnM6cmF2aW5lLWJyaWRnZS1sb29wJywgJ2NsaWZmcycsICdsb29wZWQnLCBsb29wZWQsIHRydWUpLFxuICByZWNpcGUoJ2NsaWZmczphbmNob3ItY2hhaW4nLCAnY2xpZmZzJywgJ2RpcmVjdGVkRmxvdycsIGRpcmVjdGVkRmxvdywgdHJ1ZSksXG4gIHJlY2lwZSgnYnVyaWFsOnN0b25lLWNpcmNsZS1jZW50ZXInLCAnYnVyaWFsJywgJ2Jyb2FkJywgYnJvYWQsIHRydWUpLFxuICByZWNpcGUoJ2J1cmlhbDptb3VuZC1wcm9jZXNzaW9uJywgJ2J1cmlhbCcsICd2ZXJ0aWNhbCcsIHZlcnRpY2FsLCB0cnVlKSxcbiAgcmVjaXBlKCdidXJpYWw6Y2VtZXRlcnktc2V0dGxlbWVudC1lZGdlJywgJ2J1cmlhbCcsICd0aWdodCcsIHRpZ2h0LCB0cnVlKSxcbiAgcmVjaXBlKCdidXJpYWw6b3NzdWFyeS1ob2xsb3cnLCAnYnVyaWFsJywgJ2RpcmVjdGVkRmxvdycsIGRpcmVjdGVkRmxvdywgdHJ1ZSksXG4gIHJlY2lwZSgnYnVyaWFsOmFuY2VzdG9yLXBhdGgtbG9vcCcsICdidXJpYWwnLCAnbG9vcGVkJywgbG9vcGVkLCB0cnVlKSxcbiAgcmVjaXBlKCdzYWx0RmxhdHM6Y3J1c3QtaXNsYW5kLWNoYWluJywgJ3NhbHRGbGF0cycsICdicm9hZCcsIGJyb2FkLCB0cnVlKSxcbiAgcmVjaXBlKCdzYWx0RmxhdHM6YnJpbmUtbWF6ZScsICdzYWx0RmxhdHMnLCAnZGlyZWN0ZWRGbG93JywgZGlyZWN0ZWRGbG93LCB0cnVlKSxcbiAgcmVjaXBlKCdzYWx0RmxhdHM6Y2FyYXZhbi1jYXVzZXdheScsICdzYWx0RmxhdHMnLCAndmVydGljYWwnLCB2ZXJ0aWNhbCwgdHJ1ZSksXG4gIHJlY2lwZSgnc2FsdEZsYXRzOm1pcnJvci1iYXNpbi1sb29wJywgJ3NhbHRGbGF0cycsICdsb29wZWQnLCBsb29wZWQsIHRydWUpLFxuICByZWNpcGUoJ3NhbHRGbGF0czpzYWx0LXJpZGdlLXJlZnVnZScsICdzYWx0RmxhdHMnLCAndGlnaHQnLCB0aWdodCwgdHJ1ZSksXG4gIHJlY2lwZSgnZnJvc3RSZWxpcXVhcnk6ZnJvemVuLWxha2UtY3Jvc3NpbmcnLCAnZnJvc3RSZWxpcXVhcnknLCAnYnJvYWQnLCBicm9hZCwgdHJ1ZSksXG4gIHJlY2lwZSgnZnJvc3RSZWxpcXVhcnk6cmlkZ2UtaG9sbG93LWxvb3AnLCAnZnJvc3RSZWxpcXVhcnknLCAnbG9vcGVkJywgbG9vcGVkLCB0cnVlKSxcbiAgcmVjaXBlKCdmcm9zdFJlbGlxdWFyeTpwcmVzc3VyZS1jcmFjay1tYXplJywgJ2Zyb3N0UmVsaXF1YXJ5JywgJ2RpcmVjdGVkRmxvdycsIGRpcmVjdGVkRmxvdywgdHJ1ZSksXG4gIHJlY2lwZSgnZnJvc3RSZWxpcXVhcnk6c2hvcmUtcmVsaXF1YXJ5LXJvdXRlJywgJ2Zyb3N0UmVsaXF1YXJ5JywgJ3ZlcnRpY2FsJywgdmVydGljYWwsIHRydWUpLFxuICByZWNpcGUoJ2Zyb3N0UmVsaXF1YXJ5OnN0b3JtLXJlZnVnZS1jaGFpbicsICdmcm9zdFJlbGlxdWFyeScsICd0aWdodCcsIHRpZ2h0LCB0cnVlKSxcbiAgcmVjaXBlKCd3aWxkczpsZWdhY3knLCAnd2lsZHMnLCAnYnJvYWQnLCBicm9hZCksXG4gIHJlY2lwZSgnY2F2ZXJuczpsZWdhY3knLCAnY2F2ZXJucycsICd0aWdodCcsIHRpZ2h0KSxcbiAgcmVjaXBlKCdydWluczpsZWdhY3knLCAncnVpbnMnLCAnbG9vcGVkJywgbG9vcGVkKSxcbiAgcmVjaXBlKCdmdXJuYWNlOmxlZ2FjeScsICdmdXJuYWNlJywgJ3ZlcnRpY2FsJywgdmVydGljYWwpLFxuICByZWNpcGUoJ2Zsb29kZWRSdWluczpsZWdhY3knLCAnZmxvb2RlZFJ1aW5zJywgJ2RpcmVjdGVkRmxvdycsIGRpcmVjdGVkRmxvdyksXG4gIHJlY2lwZSgnY2xpZmZzOmxlZ2FjeScsICdjbGlmZnMnLCAndmVydGljYWwnLCB2ZXJ0aWNhbCksXG4gIHJlY2lwZSgnYnVyaWFsOmxlZ2FjeScsICdidXJpYWwnLCAnbG9vcGVkJywgbG9vcGVkKSxcbiAgcmVjaXBlKCdzYWx0RmxhdHM6bGVnYWN5JywgJ3NhbHRGbGF0cycsICdicm9hZCcsIGJyb2FkKSxcbiAgcmVjaXBlKCdmcm9zdFJlbGlxdWFyeTpsZWdhY3knLCAnZnJvc3RSZWxpcXVhcnknLCAndGlnaHQnLCB0aWdodClcbl1cblxuY29uc3QgYmFzZVJlY2lwZUlkID0gKGlkOiBzdHJpbmcpOiBzdHJpbmcgPT4gaWQucmVwbGFjZSgvLXJlbWl4JC8sICcnKVxuZXhwb3J0IGNvbnN0IG1hY3JvUmVjaXBlRm9yID0gKGNvbnRyYWN0OiBSb3V0ZUNvbnRyYWN0KTogUm91dGVNYWNyb1JlY2lwZSA9PiByZWNpcGVzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gYCR7Y29udHJhY3QuYmlvbWV9OiR7YmFzZVJlY2lwZUlkKGNvbnRyYWN0LnJlY2lwZUlkKX1gKSA/PyByZWNpcGVzLmZpbmQoY2FuZGlkYXRlID0+IGNhbmRpZGF0ZS5pZCA9PT0gYCR7Y29udHJhY3QuYmlvbWV9OmxlZ2FjeWApIVxuXG5jb25zdCBjZW50ZXIgPSAoZm9vdHByaW50OiBNYWNyb05vZGVQbGFjZW1lbnRbJ2Zvb3RwcmludCddKTogUG9pbnQgPT4gKHsgeDogZm9vdHByaW50LnggKyBNYXRoLmZsb29yKGZvb3RwcmludC53aWR0aCAvIDIpLCB5OiBmb290cHJpbnQueSArIE1hdGguZmxvb3IoZm9vdHByaW50LmhlaWdodCAvIDIpIH0pXG5jb25zdCBpbnNpZGUgPSAocG9pbnQ6IFBvaW50LCB3aWR0aDogbnVtYmVyLCBoZWlnaHQ6IG51bWJlcik6IGJvb2xlYW4gPT4gcG9pbnQueCA+IDAgJiYgcG9pbnQueSA+IDAgJiYgcG9pbnQueCA8IHdpZHRoIC0gMSAmJiBwb2ludC55IDwgaGVpZ2h0IC0gMVxuY29uc3Qgb3ZlcmxhcHMgPSAobGVmdDogTWFjcm9Ob2RlUGxhY2VtZW50Wydmb290cHJpbnQnXSwgcmlnaHQ6IE1hY3JvTm9kZVBsYWNlbWVudFsnZm9vdHByaW50J10pOiBib29sZWFuID0+IGxlZnQueCA8IHJpZ2h0LnggKyByaWdodC53aWR0aCAmJiBsZWZ0LnggKyBsZWZ0LndpZHRoID4gcmlnaHQueCAmJiBsZWZ0LnkgPCByaWdodC55ICsgcmlnaHQuaGVpZ2h0ICYmIGxlZnQueSArIGxlZnQuaGVpZ2h0ID4gcmlnaHQueVxuY29uc3Qgc2FtZSA9IChsZWZ0OiBQb2ludCwgcmlnaHQ6IFBvaW50KTogYm9vbGVhbiA9PiBsZWZ0LnggPT09IHJpZ2h0LnggJiYgbGVmdC55ID09PSByaWdodC55XG5jb25zdCBheGlzUGF0aCA9IChmcm9tOiBQb2ludCwgdG86IFBvaW50LCBob3Jpem9udGFsRmlyc3Q6IGJvb2xlYW4pOiBQb2ludFtdID0+IHtcbiAgY29uc3QgY2VsbHM6IFBvaW50W10gPSBbXVxuICBjb25zdCBhcHBlbmQgPSAocG9pbnQ6IFBvaW50KTogdm9pZCA9PiB7IGlmICghY2VsbHMubGVuZ3RoIHx8ICFzYW1lKGNlbGxzW2NlbGxzLmxlbmd0aCAtIDFdLCBwb2ludCkpIGNlbGxzLnB1c2gocG9pbnQpIH1cbiAgY29uc3QgbGluZSA9IChzdGFydDogUG9pbnQsIGVuZDogUG9pbnQpOiB2b2lkID0+IHtcbiAgICBjb25zdCBkeCA9IE1hdGguc2lnbihlbmQueCAtIHN0YXJ0LngpXG4gICAgY29uc3QgZHkgPSBNYXRoLnNpZ24oZW5kLnkgLSBzdGFydC55KVxuICAgIGZvciAobGV0IHggPSBzdGFydC54LCB5ID0gc3RhcnQueTsgOyB4ICs9IGR4LCB5ICs9IGR5KSB7XG4gICAgICBhcHBlbmQoeyB4LCB5IH0pXG4gICAgICBpZiAoeCA9PT0gZW5kLnggJiYgeSA9PT0gZW5kLnkpIHJldHVyblxuICAgIH1cbiAgfVxuICBjb25zdCBiZW5kID0gaG9yaXpvbnRhbEZpcnN0ID8geyB4OiB0by54LCB5OiBmcm9tLnkgfSA6IHsgeDogZnJvbS54LCB5OiB0by55IH1cbiAgbGluZShmcm9tLCBiZW5kKVxuICBsaW5lKGJlbmQsIHRvKVxuICByZXR1cm4gY2VsbHNcbn1cbmNvbnN0IGNvbm5lY3RvclBvaW50ID0gKGZyb206IE1hY3JvTm9kZVBsYWNlbWVudFsnZm9vdHByaW50J10sIHRvOiBNYWNyb05vZGVQbGFjZW1lbnRbJ2Zvb3RwcmludCddKTogUG9pbnQgPT4ge1xuICBjb25zdCBmcm9tQ2VudGVyID0gY2VudGVyKGZyb20pXG4gIGNvbnN0IHRvQ2VudGVyID0gY2VudGVyKHRvKVxuICBpZiAoTWF0aC5hYnModG9DZW50ZXIueCAtIGZyb21DZW50ZXIueCkgPj0gTWF0aC5hYnModG9DZW50ZXIueSAtIGZyb21DZW50ZXIueSkpIHJldHVybiB7IHg6IHRvQ2VudGVyLnggPj0gZnJvbUNlbnRlci54ID8gZnJvbS54ICsgZnJvbS53aWR0aCAtIDEgOiBmcm9tLngsIHk6IE1hdGgubWF4KGZyb20ueSwgTWF0aC5taW4oZnJvbS55ICsgZnJvbS5oZWlnaHQgLSAxLCB0b0NlbnRlci55KSkgfVxuICByZXR1cm4geyB4OiBNYXRoLm1heChmcm9tLngsIE1hdGgubWluKGZyb20ueCArIGZyb20ud2lkdGggLSAxLCB0b0NlbnRlci54KSksIHk6IHRvQ2VudGVyLnkgPj0gZnJvbUNlbnRlci55ID8gZnJvbS55ICsgZnJvbS5oZWlnaHQgLSAxIDogZnJvbS55IH1cbn1cbmNvbnN0IGVkZ2VNb2RlID0gKGVkZ2U6IFJvdXRlRWRnZSk6IFJvdXRlRWRnZU1vZGUgPT4gZWRnZS5tb2Rlcy5pbmNsdWRlcygnb3B0aW9uYWwnKSA/ICdvcHRpb25hbCcgOiBlZGdlLm1vZGVzLmluY2x1ZGVzKCdjb3N0bHknKSA/ICdjb3N0bHknIDogZWRnZS5tb2Rlcy5pbmNsdWRlcygnc2FmZScpID8gJ3NhZmUnIDogJ21haW4nXG5cbmNvbnN0IGNvbXBpbGVBdHRlbXB0ID0gKGNvbnRyYWN0OiBSb3V0ZUNvbnRyYWN0LCByZWNpcGU6IFJvdXRlTWFjcm9SZWNpcGUsIG9wdGlvbnM6IE1hY3JvQ29tcGlsZU9wdGlvbnMsIGF0dGVtcHQ6IG51bWJlcik6IE1hY3JvUmVjaXBlRGVidWcgPT4ge1xuICBjb25zdCBkaWFnbm9zdGljczogc3RyaW5nW10gPSBbXVxuICBjb25zdCBqaXR0ZXIgPSBhdHRlbXB0ID8gcm5nRm9yKGNvbnRyYWN0LmNhbXBhaWduU2VlZCwgJ2dlbmVyYXRpb24nLCBjb250cmFjdC5mbG9vckluZGV4LCAnbWFjcm8tcmV0cnknLCByZWNpcGUuaWQsIGF0dGVtcHQpIDogdW5kZWZpbmVkXG4gIGNvbnN0IG5vZGVzOiBNYWNyb05vZGVQbGFjZW1lbnRbXSA9IFtdXG4gIGZvciAoY29uc3Qgbm9kZSBvZiBjb250cmFjdC5ub2Rlcykge1xuICAgIGNvbnN0IHJvbGUgPSByZWNpcGUubm9kZVJvbGVzW25vZGUua2luZF1cbiAgICBpZiAoIXJvbGUpIHsgZGlhZ25vc3RpY3MucHVzaChgbm9kZSAke25vZGUuaWR9OiByZWNpcGUgJHtyZWNpcGUuaWR9IGhhcyBubyByb2xlYCk7IGNvbnRpbnVlIH1cbiAgICBjb25zdCBhbmNob3IgPSByZWNpcGUuYW5jaG9yc1tub2RlLmtpbmRdXG4gICAgY29uc3QgeCA9IE1hdGgucm91bmQoYW5jaG9yLnggLyAxMDAgKiAob3B0aW9ucy53aWR0aCAtIDEpKSAtIE1hdGguZmxvb3Iocm9sZS5mb290cHJpbnQud2lkdGggLyAyKSArIChqaXR0ZXI/LmludCgtMiwgMikgPz8gMClcbiAgICBjb25zdCB5ID0gTWF0aC5yb3VuZChhbmNob3IueSAvIDEwMCAqIChvcHRpb25zLmhlaWdodCAtIDEpKSAtIE1hdGguZmxvb3Iocm9sZS5mb290cHJpbnQuaGVpZ2h0IC8gMikgKyAoaml0dGVyPy5pbnQoLTIsIDIpID8/IDApXG4gICAgY29uc3QgZm9vdHByaW50ID0geyB4LCB5LCAuLi5yb2xlLmZvb3RwcmludCB9XG4gICAgaWYgKCFpbnNpZGUoeyB4LCB5IH0sIG9wdGlvbnMud2lkdGgsIG9wdGlvbnMuaGVpZ2h0KSB8fCAhaW5zaWRlKHsgeDogeCArIGZvb3RwcmludC53aWR0aCAtIDEsIHk6IHkgKyBmb290cHJpbnQuaGVpZ2h0IC0gMSB9LCBvcHRpb25zLndpZHRoLCBvcHRpb25zLmhlaWdodCkpIGRpYWdub3N0aWNzLnB1c2goYG5vZGUgJHtub2RlLmlkfTogZm9vdHByaW50IGV4Y2VlZHMgJHtvcHRpb25zLndpZHRofXgke29wdGlvbnMuaGVpZ2h0fSBvbiBhdHRlbXB0ICR7YXR0ZW1wdH1gKVxuICAgIGNvbnN0IHBsYWNlZDogTWFjcm9Ob2RlUGxhY2VtZW50ID0geyBub2RlSWQ6IG5vZGUuaWQsIGtpbmQ6IG5vZGUua2luZCwgZm9vdHByaW50LCB0ZXJyYWluOiByb2xlLnRlcnJhaW4sIGxhbmRtYXJrOiByb2xlLmxhbmRtYXJrLCB2aXN1YWw6IHJvbGUudmlzdWFsIH1cbiAgICBjb25zdCBwcmV2aW91cyA9IG5vZGVzLmZpbmQob3RoZXIgPT4gb3ZlcmxhcHMob3RoZXIuZm9vdHByaW50LCBmb290cHJpbnQpKVxuICAgIGlmIChwcmV2aW91cykgZGlhZ25vc3RpY3MucHVzaChgbm9kZSAke25vZGUuaWR9OiBmb290cHJpbnQgb3ZlcmxhcHMgbm9kZSAke3ByZXZpb3VzLm5vZGVJZH0gb24gYXR0ZW1wdCAke2F0dGVtcHR9YClcbiAgICBub2Rlcy5wdXNoKHBsYWNlZClcbiAgfVxuICBjb25zdCBieUlkID0gbmV3IE1hcChub2Rlcy5tYXAobm9kZSA9PiBbbm9kZS5ub2RlSWQsIG5vZGVdKSlcbiAgY29uc3QgZWRnZXM6IE1hY3JvRWRnZVBsYWNlbWVudFtdID0gW11cbiAgZm9yIChjb25zdCBlZGdlIG9mIGNvbnRyYWN0LmVkZ2VzKSB7XG4gICAgY29uc3QgZnJvbSA9IGJ5SWQuZ2V0KGVkZ2UuZnJvbSlcbiAgICBjb25zdCB0byA9IGJ5SWQuZ2V0KGVkZ2UudG8pXG4gICAgaWYgKCFmcm9tIHx8ICF0bykgeyBkaWFnbm9zdGljcy5wdXNoKGBlZGdlICR7ZWRnZS5pZH06IG1pc3NpbmcgZW5kcG9pbnQgZm9vdHByaW50IG9uIGF0dGVtcHQgJHthdHRlbXB0fWApOyBjb250aW51ZSB9XG4gICAgY29uc3Qgcm9sZSA9IHJlY2lwZS5lZGdlUm9sZXNbZWRnZU1vZGUoZWRnZSldXG4gICAgY29uc3QgZnJvbVBvaW50ID0gY29ubmVjdG9yUG9pbnQoZnJvbS5mb290cHJpbnQsIHRvLmZvb3RwcmludClcbiAgICBjb25zdCB0b1BvaW50ID0gY29ubmVjdG9yUG9pbnQodG8uZm9vdHByaW50LCBmcm9tLmZvb3RwcmludClcbiAgICBjb25zdCBjZWxscyA9IGF4aXNQYXRoKGZyb21Qb2ludCwgdG9Qb2ludCwgcm9sZS5jb25uZWN0b3IgPT09ICdob3Jpem9udGFsRmlyc3QnKVxuICAgIGNvbnN0IGludmFsaWQgPSBjZWxscy5maW5kKHBvaW50ID0+ICFpbnNpZGUocG9pbnQsIG9wdGlvbnMud2lkdGgsIG9wdGlvbnMuaGVpZ2h0KSlcbiAgICBpZiAoaW52YWxpZCkgZGlhZ25vc3RpY3MucHVzaChgZWRnZSAke2VkZ2UuaWR9OiBjb25uZWN0b3IgbGVhdmVzIGJvdW5kcyBhdCAke2ludmFsaWQueH0sJHtpbnZhbGlkLnl9IG9uIGF0dGVtcHQgJHthdHRlbXB0fWApXG4gICAgZWRnZXMucHVzaCh7IGVkZ2VJZDogZWRnZS5pZCwgbW9kZXM6IFsuLi5lZGdlLm1vZGVzXSwgZnJvbTogZnJvbVBvaW50LCB0bzogdG9Qb2ludCwgY2VsbHMsIHRlcnJhaW46IHJvbGUudGVycmFpbiwgdmlzdWFsOiByb2xlLnZpc3VhbCB9KVxuICB9XG4gIHJldHVybiB7IHZhbGlkOiAhZGlhZ25vc3RpY3MubGVuZ3RoLCByZWNpcGVJZDogcmVjaXBlLmlkLCB0b3BvbG9neTogcmVjaXBlLnRvcG9sb2d5LCBhdHRlbXB0LCBub2RlcywgZWRnZXMsIGRpYWdub3N0aWNzIH1cbn1cblxuZXhwb3J0IGNvbnN0IGNvbXBpbGVSb3V0ZUNvbnRyYWN0ID0gKGNvbnRyYWN0OiBSb3V0ZUNvbnRyYWN0LCBvcHRpb25zOiBNYWNyb0NvbXBpbGVPcHRpb25zKTogTWFjcm9SZWNpcGVEZWJ1ZyA9PiB7XG4gIGNvbnN0IHJlY2lwZSA9IG1hY3JvUmVjaXBlRm9yKGNvbnRyYWN0KVxuICBjb25zdCBhdHRlbXB0cyA9IG9wdGlvbnMuYXR0ZW1wdHMgPz8gM1xuICBsZXQgZmFpbGVkOiBNYWNyb1JlY2lwZURlYnVnIHwgdW5kZWZpbmVkXG4gIGZvciAobGV0IGF0dGVtcHQgPSAwOyBhdHRlbXB0IDwgYXR0ZW1wdHM7IGF0dGVtcHQrKykge1xuICAgIGNvbnN0IGNvbXBpbGVkID0gY29tcGlsZUF0dGVtcHQoY29udHJhY3QsIHJlY2lwZSwgb3B0aW9ucywgYXR0ZW1wdClcbiAgICBpZiAoY29tcGlsZWQudmFsaWQpIHJldHVybiBjb21waWxlZFxuICAgIGZhaWxlZCA9IGNvbXBpbGVkXG4gIH1cbiAgcmV0dXJuIHsgLi4uZmFpbGVkISwgZGlhZ25vc3RpY3M6IEFycmF5LmZyb20oeyBsZW5ndGg6IGF0dGVtcHRzIH0sIChfLCBhdHRlbXB0KSA9PiBjb21waWxlQXR0ZW1wdChjb250cmFjdCwgcmVjaXBlLCBvcHRpb25zLCBhdHRlbXB0KS5kaWFnbm9zdGljcy5tYXAoZXJyb3IgPT4gYGF0dGVtcHQgJHthdHRlbXB0fTogJHtlcnJvcn1gKSkuZmxhdCgpIH1cbn1cblxuZXhwb3J0IGNvbnN0IG1hY3JvQ29ubmVjdG9yUG9pbnRzID0gKGRlYnVnOiBNYWNyb1JlY2lwZURlYnVnKTogUG9pbnRbXSA9PiBkZWJ1Zy5lZGdlcy5mbGF0TWFwKGVkZ2UgPT4gZWRnZS5jZWxscylcbmV4cG9ydCBjb25zdCB2YWxpZGF0ZU1hY3JvUmVhbGl6YXRpb24gPSAoZGVidWc6IE1hY3JvUmVjaXBlRGVidWcsIHRyYXZlcnNhYmxlOiAocG9pbnQ6IFBvaW50KSA9PiBib29sZWFuKTogc3RyaW5nW10gPT4gZGVidWcuZWRnZXMuZmxhdE1hcChlZGdlID0+IHtcbiAgY29uc3QgYmxvY2tlZCA9IGVkZ2UuY2VsbHMuZmluZChwb2ludCA9PiAhdHJhdmVyc2FibGUocG9pbnQpKVxuICByZXR1cm4gYmxvY2tlZCA/IFtgZWRnZSAke2VkZ2UuZWRnZUlkfTogY29ubmVjdG9yIGJsb2NrZWQgYXQgJHtibG9ja2VkLnh9LCR7YmxvY2tlZC55fWBdIDogW11cbn0pXG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLE1BQU0sUUFBUSxPQUFPO0FBSTlCLE9B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QyxFQUFFO0VBQUcsQ0FBQztFQUFFcEIsUUFBUSxFQUFFO0lBQUVtQixDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVaLElBQUksRUFBRTtJQUFFVyxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVYLFNBQVMsRUFBRTtJQUFFVSxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVWLGNBQWMsRUFBRTtJQUFFUyxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVULElBQUksRUFBRTtJQUFFUSxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVSLElBQUksRUFBRTtJQUFFTyxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRztBQUFFLENBQUM7QUFDMU8sTUFBTUUsUUFBc0MsR0FBRztFQUFFZixLQUFLLEVBQUU7SUFBRVksQ0FBQyxFQUFFLEVBQUU7SUFBRUMsQ0FBQyxFQUFFO0VBQUcsQ0FBQztFQUFFcEIsUUFBUSxFQUFFO0lBQUVtQixDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVaLElBQUksRUFBRTtJQUFFVyxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVYLFNBQVMsRUFBRTtJQUFFVSxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVWLGNBQWMsRUFBRTtJQUFFUyxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVULElBQUksRUFBRTtJQUFFUSxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRyxDQUFDO0VBQUVSLElBQUksRUFBRTtJQUFFTyxDQUFDLEVBQUUsRUFBRTtJQUFFQyxDQUFDLEVBQUU7RUFBRztBQUFFLENBQUM7QUFDN08sTUFBTUcsWUFBMEMsR0FBRztFQUFF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