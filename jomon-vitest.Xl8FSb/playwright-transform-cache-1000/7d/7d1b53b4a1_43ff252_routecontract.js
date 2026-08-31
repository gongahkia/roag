// 52fb5ffb75d6c6e1dfb7c4e6e48d068a94f81044
import { rngFor } from './rng';
import { rewardOffersFor } from './reward-contract';
export const ROUTE_NODE_KINDS = ['start', 'landmark', 'fork', 'objective', 'optionalReward', 'exit', 'boss'];
export const ROUTE_EDGE_MODES = ['main', 'safe', 'costly', 'optional'];
const tagKeys = ['terrain', 'encounter', 'reward', 'gate', 'visual', 'escalation'];
const tagSet = (tags = {}) => {
  var _tags$terrain, _tags$encounter, _tags$reward, _tags$gate, _tags$visual, _tags$escalation;
  return {
    terrain: [...((_tags$terrain = tags.terrain) !== null && _tags$terrain !== void 0 ? _tags$terrain : [])],
    encounter: [...((_tags$encounter = tags.encounter) !== null && _tags$encounter !== void 0 ? _tags$encounter : [])],
    reward: [...((_tags$reward = tags.reward) !== null && _tags$reward !== void 0 ? _tags$reward : [])],
    gate: [...((_tags$gate = tags.gate) !== null && _tags$gate !== void 0 ? _tags$gate : [])],
    visual: [...((_tags$visual = tags.visual) !== null && _tags$visual !== void 0 ? _tags$visual : [])],
    escalation: [...((_tags$escalation = tags.escalation) !== null && _tags$escalation !== void 0 ? _tags$escalation : [])]
  };
};
const biomeRouteTags = {
  mine: {
    terrain: 'rail',
    pressure: 'collapse',
    encounter: 'mine-guard',
    landmark: 'shaft'
  },
  wilds: {
    terrain: 'water',
    pressure: 'bramble',
    encounter: 'wilds-hunter',
    landmark: 'grove'
  },
  caverns: {
    terrain: 'tide',
    pressure: 'darkness',
    encounter: 'cavern-stalker',
    landmark: 'chamber'
  },
  ruins: {
    terrain: 'ward',
    pressure: 'sightline',
    encounter: 'ruin-sentinel',
    landmark: 'precinct'
  },
  furnace: {
    terrain: 'smoke',
    pressure: 'lift',
    encounter: 'cinder-guard',
    landmark: 'kiln'
  },
  floodedRuins: {
    terrain: 'current',
    pressure: 'anchor',
    encounter: 'flood-hunter',
    landmark: 'floodgate'
  },
  cliffs: {
    terrain: 'wind',
    pressure: 'climb',
    encounter: 'ledge-hunter',
    landmark: 'anchor'
  },
  burial: {
    terrain: 'ritual',
    pressure: 'spirit',
    encounter: 'grave-guardian',
    landmark: 'stone-circle'
  },
  saltFlats: {
    terrain: 'brine',
    pressure: 'mirror',
    encounter: 'salt-stalker',
    landmark: 'caravan'
  },
  frostReliquary: {
    terrain: 'ice',
    pressure: 'whiteout',
    encounter: 'frost-hunter',
    landmark: 'reliquary'
  }
};
const nodesOfKind = (contract, kind) => contract.nodes.filter(node => node.kind === kind);
const edgesFrom = (contract, id, predicate = () => true) => contract.edges.filter(edge => edge.from === id && predicate(edge));
const reaches = (contract, from, target) => {
  const queue = [from];
  const seen = new Set([from]);
  for (let cursor = 0; cursor < queue.length; cursor++) {
    const current = queue[cursor];
    if (current === target) return true;
    for (const edge of edgesFrom(contract, current)) if (!seen.has(edge.to)) {
      seen.add(edge.to);
      queue.push(edge.to);
    }
  }
  return false;
};
const findSimplePaths = (contract, from, target, predicate, limit = 2) => {
  const paths = [];
  const visit = (current, seen, path) => {
    if (paths.length >= limit) return;
    if (current === target) {
      paths.push(path);
      return;
    }
    for (const edge of edgesFrom(contract, current, predicate)) if (!seen.has(edge.to)) {
      const next = new Set(seen);
      next.add(edge.to);
      visit(edge.to, next, [...path, edge]);
    }
  };
  visit(from, new Set([from]), []);
  return paths;
};
const validTagSet = tags => {
  if (!tags || typeof tags !== 'object') return false;
  const values = tags;
  return tagKeys.every(key => Array.isArray(values[key]) && values[key].length > 0 && values[key].every(value => typeof value === 'string' && value.length > 0));
};
const validateTags = (owner, tags, errors) => {
  if (validTagSet(tags)) return;
  if (!tags || typeof tags !== 'object') {
    errors.push(`${owner}: missing tag set`);
    return;
  }
  for (const key of tagKeys) {
    const values = tags[key];
    if (!Array.isArray(values) || !values.length) errors.push(`${owner}: missing ${key} tags`);else if (values.some(value => typeof value !== 'string' || !value.length)) errors.push(`${owner}: invalid ${key} tag`);
  }
};
const validateRewardOffers = (offers, errors) => {
  if (!Array.isArray(offers)) {
    errors.push('reward offers: missing');
    return;
  }
  const expected = [['waycache', 'waycache'], ['boon-teach', 'boon'], ['boon-test', 'boon'], ['boon-payoff', 'boon']];
  if (offers.length !== expected.length) errors.push(`reward offers: expected ${expected.length}, found ${offers.length}`);
  for (const [milestoneId, kind] of expected) {
    const offer = offers.find(candidate => typeof candidate === 'object' && candidate !== null && candidate.milestoneId === milestoneId);
    if (!offer || typeof offer !== 'object') {
      errors.push(`reward offer ${milestoneId}: missing`);
      continue;
    }
    const value = offer;
    if (!value.id || typeof value.id !== 'string') errors.push(`reward offer ${milestoneId}: missing id`);
    if (value.kind !== kind) errors.push(`reward offer ${milestoneId}: invalid kind`);
    if (!Array.isArray(value.choices) || value.choices.length !== 3) {
      errors.push(`reward offer ${milestoneId}: expected three choices`);
      continue;
    }
    const roles = new Set();
    for (const choice of value.choices) {
      if (!choice || typeof choice !== 'object') {
        errors.push(`reward offer ${milestoneId}: invalid choice`);
        continue;
      }
      const annotation = choice;
      if (typeof annotation.id !== 'string' || !annotation.id) errors.push(`reward offer ${milestoneId}: choice missing id`);
      if (annotation.role !== 'safe' && annotation.role !== 'risky' && annotation.role !== 'sidegrade') errors.push(`reward offer ${milestoneId}: choice invalid role`);else roles.add(annotation.role);
      if (typeof annotation.problem !== 'string' || typeof annotation.terrain !== 'string' || typeof annotation.route !== 'string' || typeof annotation.payoff !== 'string' || annotation.biomeFit !== 'local' && annotation.biomeFit !== 'global') errors.push(`reward offer ${milestoneId}: choice missing annotation`);
    }
    if (roles.size !== 3) errors.push(`reward offer ${milestoneId}: requires safe, risky, and sidegrade choices`);
  }
};
export const validateRouteContract = contract => {
  const errors = [];
  validateRewardOffers(contract.rewardOffers, errors);
  const nodeIds = new Set();
  for (const node of contract.nodes) {
    const owner = `node ${node.id || '<empty>'}`;
    if (!node.id) errors.push(`${owner}: missing id`);else if (nodeIds.has(node.id)) errors.push(`${owner}: duplicate id`);
    nodeIds.add(node.id);
    if (!ROUTE_NODE_KINDS.includes(node.kind)) errors.push(`${owner}: invalid kind ${node.kind}`);
    validateTags(owner, node.tags, errors);
  }
  const required = ['start', 'landmark', 'fork', 'objective', 'optionalReward'];
  for (const kind of required) {
    const count = nodesOfKind(contract, kind).length;
    if (count !== 1) errors.push(`node kind ${kind}: expected one, found ${count}`);
  }
  const ends = [...nodesOfKind(contract, 'exit'), ...nodesOfKind(contract, 'boss')];
  if (ends.length !== 1) errors.push(`node kind end: expected one exit or boss, found ${ends.length}`);
  const edgeIds = new Set();
  for (const edge of contract.edges) {
    const owner = `edge ${edge.id || '<empty>'}`;
    if (!edge.id) errors.push(`${owner}: missing id`);else if (edgeIds.has(edge.id)) errors.push(`${owner}: duplicate id`);
    edgeIds.add(edge.id);
    if (!nodeIds.has(edge.from)) errors.push(`${owner}: unknown source node ${edge.from}`);
    if (!nodeIds.has(edge.to)) errors.push(`${owner}: unknown target node ${edge.to}`);
    if (!edge.modes.length) errors.push(`${owner}: missing route modes`);
    for (const mode of edge.modes) if (!ROUTE_EDGE_MODES.includes(mode)) errors.push(`${owner}: invalid route mode ${mode}`);
    validateTags(owner, edge.tags, errors);
  }
  const start = nodesOfKind(contract, 'start')[0];
  const fork = nodesOfKind(contract, 'fork')[0];
  const reward = nodesOfKind(contract, 'optionalReward')[0];
  const end = ends[0];
  if (start && end) {
    if (!reaches(contract, start.id, end.id)) errors.push(`node ${end.id}: unreachable from start ${start.id}`);
    const mainPaths = findSimplePaths(contract, start.id, end.id, edge => edge.modes.includes('main'));
    if (mainPaths.length !== 1) errors.push(`node ${start.id}: expected exactly one main route to ${end.id}, found ${mainPaths.length}`);
    if (mainPaths.length === 1) {
      const mainPathEdges = new Set(mainPaths[0].map(edge => edge.id));
      for (const edge of contract.edges) if (edge.modes.includes('main') && !mainPathEdges.has(edge.id)) errors.push(`edge ${edge.id}: main route edge is disconnected from the main route`);
    }
  }
  if (fork && end) {
    const choices = edgesFrom(contract, fork.id).filter(edge => nodeIds.has(edge.to));
    const safe = choices.filter(edge => edge.modes.includes('safe'));
    const costly = choices.filter(edge => edge.modes.includes('costly'));
    if (!safe.length) errors.push(`node ${fork.id}: missing safe route choice`);
    if (!costly.length) errors.push(`node ${fork.id}: missing costly route choice`);
    if (safe.length && costly.length && safe.some(edge => costly.some(other => other.to === edge.to))) errors.push(`node ${fork.id}: safe and costly routes share a destination`);
    for (const edge of [...safe, ...costly]) if (!reaches(contract, edge.to, end.id)) errors.push(`edge ${edge.id}: route choice cannot reach ${end.id}`);
  }
  if (reward && start && end) {
    if (!reaches(contract, start.id, reward.id)) errors.push(`node ${reward.id}: optional payoff is unreachable from ${start.id}`);
    if (!reaches(contract, reward.id, end.id)) errors.push(`node ${reward.id}: optional payoff cannot rejoin route to ${end.id}`);
    const optionalEdges = contract.edges.filter(edge => edge.to === reward.id && edge.modes.includes('optional'));
    if (!optionalEdges.length) errors.push(`node ${reward.id}: missing optional route edge`);
  }
  return {
    valid: errors.length === 0,
    errors
  };
};
const assertGenerationInput = input => {
  if (!Number.isInteger(input.campaignSeed)) throw new Error('route contract campaign seed must be an integer');
  if (!Number.isInteger(input.floorIndex) || input.floorIndex < 0) throw new Error('route contract floor index must be non-negative');
  if (!Number.isInteger(input.areaFloor) || input.areaFloor < 0 || input.areaFloor > 3) throw new Error('route contract area floor must be between 0 and 3');
  if (!input.recipeId) throw new Error('route contract recipe id is required');
  if (!input.escalationVariant) throw new Error('route contract escalation variant is required');
};
export const generateRouteContract = input => {
  assertGenerationInput(input);
  const rng = rngFor(input.campaignSeed, 'generation', input.floorIndex, 'route-contract', input.biome, input.recipeId, input.escalationVariant);
  const biome = biomeRouteTags[input.biome];
  const routeTerrain = rng.chance(50) ? biome.terrain : biome.pressure;
  const endKind = input.areaFloor === 3 ? 'boss' : 'exit';
  const prefix = `${input.biome}:${input.floorIndex}:${input.recipeId}:${input.escalationVariant}`;
  const tags = overrides => tagSet({
    escalation: [input.escalationVariant],
    ...overrides
  });
  const node = (kind, overrides) => ({
    id: `${prefix}:${kind}`,
    kind,
    tags: tags(overrides)
  });
  const nodes = [node('start', {
    terrain: ['entry'],
    encounter: ['none'],
    reward: ['none'],
    gate: ['open'],
    visual: ['entry-marker']
  }), node('landmark', {
    terrain: [biome.terrain],
    encounter: ['none'],
    reward: ['orientation'],
    gate: ['open'],
    visual: [biome.landmark]
  }), node('fork', {
    terrain: [routeTerrain],
    encounter: [biome.encounter],
    reward: ['choice'],
    gate: ['open'],
    visual: ['branch-marker']
  }), node('objective', {
    terrain: [biome.terrain],
    encounter: [biome.encounter],
    reward: ['objective'],
    gate: ['key-or-counterroute'],
    visual: ['objective-marker']
  }), node('optionalReward', {
    terrain: [biome.pressure],
    encounter: [biome.encounter],
    reward: ['optional-payoff'],
    gate: ['costly-gate'],
    visual: ['reward-marker']
  }), node(endKind, {
    terrain: [routeTerrain],
    encounter: [endKind === 'boss' ? 'boss' : 'none'],
    reward: ['exit-payoff'],
    gate: ['resolved'],
    visual: [endKind === 'boss' ? 'boss-marker' : 'exit-marker']
  })];
  const edge = (from, to, modes, overrides) => ({
    id: `${prefix}:${from}:${to}:${modes.join('+')}`,
    from: `${prefix}:${from}`,
    to: `${prefix}:${to}`,
    modes,
    tags: tags(overrides)
  });
  const edges = [edge('start', 'landmark', ['main'], {
    terrain: ['approach'],
    encounter: ['none'],
    reward: ['orientation'],
    gate: ['open'],
    visual: ['route-line']
  }), edge('landmark', 'fork', ['main'], {
    terrain: [biome.terrain],
    encounter: [biome.encounter],
    reward: ['choice'],
    gate: ['open'],
    visual: ['fork-line']
  }), edge('fork', 'objective', ['main', 'safe'], {
    terrain: [biome.terrain],
    encounter: ['guarded'],
    reward: ['objective'],
    gate: ['key'],
    visual: ['safe-route']
  }), edge('fork', 'optionalReward', ['costly', 'optional'], {
    terrain: [biome.pressure],
    encounter: ['ambush'],
    reward: ['optional-payoff'],
    gate: ['costly-gate'],
    visual: ['risk-route']
  }), edge('optionalReward', 'objective', ['costly', 'optional'], {
    terrain: [routeTerrain],
    encounter: [biome.encounter],
    reward: ['payoff'],
    gate: ['rejoin'],
    visual: ['return-route']
  }), edge('objective', endKind, ['main'], {
    terrain: [routeTerrain],
    encounter: [endKind === 'boss' ? 'boss' : 'none'],
    reward: ['completion'],
    gate: ['resolved'],
    visual: ['exit-route']
  })];
  return {
    id: `route:${prefix}`,
    ...input,
    nodes,
    edges,
    rewardOffers: rewardOffersFor(input)
  };
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJybmdGb3IiLCJyZXdhcmRPZmZlcnNGb3IiLCJST1VURV9OT0RFX0tJTkRTIiwiUk9VVEVfRURHRV9NT0RFUyIsInRhZ0tleXMiLCJ0YWdTZXQiLCJ0YWdzIiwiX3RhZ3MkdGVycmFpbiIsIl90YWdzJGVuY291bnRlciIsIl90YWdzJHJld2FyZCIsIl90YWdzJGdhdGUiLCJfdGFncyR2aXN1YWwiLCJfdGFncyRlc2NhbGF0aW9uIiwidGVycmFpbiIsImVuY291bnRlciIsInJld2FyZCIsImdhdGUiLCJ2aXN1YWwiLCJlc2NhbGF0aW9uIiwiYmlvbWVSb3V0ZVRhZ3MiLCJtaW5lIiwicHJlc3N1cmUiLCJsYW5kbWFyayIsIndpbGRzIiwiY2F2ZXJucyIsInJ1aW5zIiwiZnVybmFjZSIsImZsb29kZWRSdWlucyIsImNsaWZmcyIsImJ1cmlhbCIsInNhbHRGbGF0cyIsImZyb3N0UmVsaXF1YXJ5Iiwibm9kZXNPZktpbmQiLCJjb250cmFjdCIsImtpbmQiLCJub2RlcyIsImZpbHRlciIsIm5vZGUiLCJlZGdlc0Zyb20iLCJpZCIsInByZWRpY2F0ZSIsImVkZ2VzIiwiZWRnZSIsImZyb20iLCJyZWFjaGVzIiwidGFyZ2V0IiwicXVldWUiLCJzZWVuIiwiU2V0IiwiY3Vyc29yIiwibGVuZ3RoIiwiY3VycmVudCIsImhhcyIsInRvIiwiYWRkIiwicHVzaCIsImZpbmRTaW1wbGVQYXRocyIsImxpbWl0IiwicGF0aHMiLCJ2aXNpdCIsInBhdGgiLCJuZXh0IiwidmFsaWRUYWdTZXQiLCJ2YWx1ZXMiLCJldmVyeSIsImtleSIsIkFycmF5IiwiaXNBcnJheSIsInZhbHVlIiwidmFsaWRhdGVUYWdzIiwib3duZXIiLCJlcnJvcnMiLCJzb21lIiwidmFsaWRhdGVSZXdhcmRPZmZlcnMiLCJvZmZlcnMiLCJleHBlY3RlZCIsIm1pbGVzdG9uZUlkIiwib2ZmZXIiLCJmaW5kIiwiY2FuZGlkYXRlIiwiY2hvaWNlcyIsInJvbGVzIiwiY2hvaWNlIiwiYW5ub3RhdGlvbiIsInJvbGUiLCJwcm9ibGVtIiwicm91dGUiLCJwYXlvZmYiLCJiaW9tZUZpdCIsInNpemUiLCJ2YWxpZGF0ZVJvdXRlQ29udHJhY3QiLCJyZXdhcmRPZmZlcnMiLCJub2RlSWRzIiwiaW5jbHVkZXMiLCJyZXF1aXJlZCIsImNvdW50IiwiZW5kcyIsImVkZ2VJZHMiLCJtb2RlcyIsIm1vZGUiLCJzdGFydCIsImZvcmsiLCJlbmQiLCJtYWluUGF0aHMiLCJtYWluUGF0aEVkZ2VzIiwibWFwIiwic2FmZSIsImNvc3RseSIsIm90aGVyIiwib3B0aW9uYWxFZGdlcyIsInZhbGlkIiwiYXNzZXJ0R2VuZXJhdGlvbklucHV0IiwiaW5wdXQiLCJOdW1iZXIiLCJpc0ludGVnZXIiLCJjYW1wYWlnblNlZWQiLCJFcnJvciIsImZsb29ySW5kZXgiLCJhcmVhRmxvb3IiLCJyZWNpcGVJZCIsImVzY2FsYXRpb25WYXJpYW50IiwiZ2VuZXJhdGVSb3V0ZUNvbnRyYWN0Iiwicm5nIiwiYmlvbWUiLCJyb3V0ZVRlcnJhaW4iLCJjaGFuY2UiLCJlbmRLaW5kIiwicHJlZml4Iiwib3ZlcnJpZGVzIiwiam9pbiJdLCJzb3VyY2VzIjpbInJvdXRlLWNvbnRyYWN0LnRzIl0sInNvdXJjZXNDb250ZW50IjpbImltcG9ydCB7IHJuZ0ZvciB9IGZyb20gJy4vcm5nJ1xuaW1wb3J0IHsgcmV3YXJkT2ZmZXJzRm9yIH0gZnJvbSAnLi9yZXdhcmQtY29udHJhY3QnXG5pbXBvcnQgdHlwZSB7IEJpb21lLCBSZXdhcmRPZmZlciB9IGZyb20gJy4vdHlwZXMnXG5cbmV4cG9ydCBjb25zdCBST1VURV9OT0RFX0tJTkRTID0gWydzdGFydCcsICdsYW5kbWFyaycsICdmb3JrJywgJ29iamVjdGl2ZScsICdvcHRpb25hbFJld2FyZCcsICdleGl0JywgJ2Jvc3MnXSBhcyBjb25zdFxuZXhwb3J0IHR5cGUgUm91dGVOb2RlS2luZCA9IHR5cGVvZiBST1VURV9OT0RFX0tJTkRTW251bWJlcl1cbmV4cG9ydCBjb25zdCBST1VURV9FREdFX01PREVTID0gWydtYWluJywgJ3NhZmUnLCAnY29zdGx5JywgJ29wdGlvbmFsJ10gYXMgY29uc3RcbmV4cG9ydCB0eXBlIFJvdXRlRWRnZU1vZGUgPSB0eXBlb2YgUk9VVEVfRURHRV9NT0RFU1tudW1iZXJdXG5cbmV4cG9ydCBpbnRlcmZhY2UgUm91dGVUYWdzIHtcbiAgdGVycmFpbjogc3RyaW5nW11cbiAgZW5jb3VudGVyOiBzdHJpbmdbXVxuICByZXdhcmQ6IHN0cmluZ1tdXG4gIGdhdGU6IHN0cmluZ1tdXG4gIHZpc3VhbDogc3RyaW5nW11cbiAgZXNjYWxhdGlvbjogc3RyaW5nW11cbn1cblxuZXhwb3J0IGludGVyZmFjZSBSb3V0ZU5vZGUge1xuICBpZDogc3RyaW5nXG4gIGtpbmQ6IFJvdXRlTm9kZUtpbmRcbiAgdGFnczogUm91dGVUYWdzXG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgUm91dGVFZGdlIHtcbiAgaWQ6IHN0cmluZ1xuICBmcm9tOiBzdHJpbmdcbiAgdG86IHN0cmluZ1xuICBtb2RlczogUm91dGVFZGdlTW9kZVtdXG4gIHRhZ3M6IFJvdXRlVGFnc1xufVxuXG5leHBvcnQgaW50ZXJmYWNlIFJvdXRlQ29udHJhY3RJbnB1dCB7XG4gIGNhbXBhaWduU2VlZDogbnVtYmVyXG4gIGZsb29ySW5kZXg6IG51bWJlclxuICBiaW9tZTogQmlvbWVcbiAgYXJlYUZsb29yOiBudW1iZXJcbiAgcmVjaXBlSWQ6IHN0cmluZ1xuICBlc2NhbGF0aW9uVmFyaWFudDogc3RyaW5nXG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgUm91dGVDb250cmFjdCBleHRlbmRzIFJvdXRlQ29udHJhY3RJbnB1dCB7XG4gIGlkOiBzdHJpbmdcbiAgbm9kZXM6IFJvdXRlTm9kZVtdXG4gIGVkZ2VzOiBSb3V0ZUVkZ2VbXVxuICByZXdhcmRPZmZlcnM6IFJld2FyZE9mZmVyW11cbn1cblxuZXhwb3J0IGludGVyZmFjZSBSb3V0ZUNvbnRyYWN0VmFsaWRhdGlvbiB7IHZhbGlkOiBib29sZWFuOyBlcnJvcnM6IHN0cmluZ1tdIH1cblxuY29uc3QgdGFnS2V5cyA9IFsndGVycmFpbicsICdlbmNvdW50ZXInLCAncmV3YXJkJywgJ2dhdGUnLCAndmlzdWFsJywgJ2VzY2FsYXRpb24nXSBhcyBjb25zdFxuY29uc3QgdGFnU2V0ID0gKHRhZ3M6IFBhcnRpYWw8Um91dGVUYWdzPiA9IHt9KTogUm91dGVUYWdzID0+ICh7XG4gIHRlcnJhaW46IFsuLi4odGFncy50ZXJyYWluID8/IFtdKV0sXG4gIGVuY291bnRlcjogWy4uLih0YWdzLmVuY291bnRlciA/PyBbXSldLFxuICByZXdhcmQ6IFsuLi4odGFncy5yZXdhcmQgPz8gW10pXSxcbiAgZ2F0ZTogWy4uLih0YWdzLmdhdGUgPz8gW10pXSxcbiAgdmlzdWFsOiBbLi4uKHRhZ3MudmlzdWFsID8/IFtdKV0sXG4gIGVzY2FsYXRpb246IFsuLi4odGFncy5lc2NhbGF0aW9uID8/IFtdKV1cbn0pXG5cbmNvbnN0IGJpb21lUm91dGVUYWdzOiBSZWNvcmQ8QmlvbWUsIHsgdGVycmFpbjogc3RyaW5nOyBwcmVzc3VyZTogc3RyaW5nOyBlbmNvdW50ZXI6IHN0cmluZzsgbGFuZG1hcms6IHN0cmluZyB9PiA9IHtcbiAgbWluZTogeyB0ZXJyYWluOiAncmFpbCcsIHByZXNzdXJlOiAnY29sbGFwc2UnLCBlbmNvdW50ZXI6ICdtaW5lLWd1YXJkJywgbGFuZG1hcms6ICdzaGFmdCcgfSxcbiAgd2lsZHM6IHsgdGVycmFpbjogJ3dhdGVyJywgcHJlc3N1cmU6ICdicmFtYmxlJywgZW5jb3VudGVyOiAnd2lsZHMtaHVudGVyJywgbGFuZG1hcms6ICdncm92ZScgfSxcbiAgY2F2ZXJuczogeyB0ZXJyYWluOiAndGlkZScsIHByZXNzdXJlOiAnZGFya25lc3MnLCBlbmNvdW50ZXI6ICdjYXZlcm4tc3RhbGtlcicsIGxhbmRtYXJrOiAnY2hhbWJlcicgfSxcbiAgcnVpbnM6IHsgdGVycmFpbjogJ3dhcmQnLCBwcmVzc3VyZTogJ3NpZ2h0bGluZScsIGVuY291bnRlcjogJ3J1aW4tc2VudGluZWwnLCBsYW5kbWFyazogJ3ByZWNpbmN0JyB9LFxuICBmdXJuYWNlOiB7IHRlcnJhaW46ICdzbW9rZScsIHByZXNzdXJlOiAnbGlmdCcsIGVuY291bnRlcjogJ2NpbmRlci1ndWFyZCcsIGxhbmRtYXJrOiAna2lsbicgfSxcbiAgZmxvb2RlZFJ1aW5zOiB7IHRlcnJhaW46ICdjdXJyZW50JywgcHJlc3N1cmU6ICdhbmNob3InLCBlbmNvdW50ZXI6ICdmbG9vZC1odW50ZXInLCBsYW5kbWFyazogJ2Zsb29kZ2F0ZScgfSxcbiAgY2xpZmZzOiB7IHRlcnJhaW46ICd3aW5kJywgcHJlc3N1cmU6ICdjbGltYicsIGVuY291bnRlcjogJ2xlZGdlLWh1bnRlcicsIGxhbmRtYXJrOiAnYW5jaG9yJyB9LFxuICBidXJpYWw6IHsgdGVycmFpbjogJ3JpdHVhbCcsIHByZXNzdXJlOiAnc3Bpcml0JywgZW5jb3VudGVyOiAnZ3JhdmUtZ3VhcmRpYW4nLCBsYW5kbWFyazogJ3N0b25lLWNpcmNsZScgfSxcbiAgc2FsdEZsYXRzOiB7IHRlcnJhaW46ICdicmluZScsIHByZXNzdXJlOiAnbWlycm9yJywgZW5jb3VudGVyOiAnc2FsdC1zdGFsa2VyJywgbGFuZG1hcms6ICdjYXJhdmFuJyB9LFxuICBmcm9zdFJlbGlxdWFyeTogeyB0ZXJyYWluOiAnaWNlJywgcHJlc3N1cmU6ICd3aGl0ZW91dCcsIGVuY291bnRlcjogJ2Zyb3N0LWh1bnRlcicsIGxhbmRtYXJrOiAncmVsaXF1YXJ5JyB9XG59XG5cbmNvbnN0IG5vZGVzT2ZLaW5kID0gKGNvbnRyYWN0OiBSb3V0ZUNvbnRyYWN0LCBraW5kOiBSb3V0ZU5vZGVLaW5kKTogUm91dGVOb2RlW10gPT4gY29udHJhY3Qubm9kZXMuZmlsdGVyKG5vZGUgPT4gbm9kZS5raW5kID09PSBraW5kKVxuY29uc3QgZWRnZXNGcm9tID0gKGNvbnRyYWN0OiBSb3V0ZUNvbnRyYWN0LCBpZDogc3RyaW5nLCBwcmVkaWNhdGU6IChlZGdlOiBSb3V0ZUVkZ2UpID0+IGJvb2xlYW4gPSAoKSA9PiB0cnVlKTogUm91dGVFZGdlW10gPT4gY29udHJhY3QuZWRnZXMuZmlsdGVyKGVkZ2UgPT4gZWRnZS5mcm9tID09PSBpZCAmJiBwcmVkaWNhdGUoZWRnZSkpXG5cbmNvbnN0IHJlYWNoZXMgPSAoY29udHJhY3Q6IFJvdXRlQ29udHJhY3QsIGZyb206IHN0cmluZywgdGFyZ2V0OiBzdHJpbmcpOiBib29sZWFuID0+IHtcbiAgY29uc3QgcXVldWUgPSBbZnJvbV1cbiAgY29uc3Qgc2VlbiA9IG5ldyBTZXQ8c3RyaW5nPihbZnJvbV0pXG4gIGZvciAobGV0IGN1cnNvciA9IDA7IGN1cnNvciA8IHF1ZXVlLmxlbmd0aDsgY3Vyc29yKyspIHtcbiAgICBjb25zdCBjdXJyZW50ID0gcXVldWVbY3Vyc29yXVxuICAgIGlmIChjdXJyZW50ID09PSB0YXJnZXQpIHJldHVybiB0cnVlXG4gICAgZm9yIChjb25zdCBlZGdlIG9mIGVkZ2VzRnJvbShjb250cmFjdCwgY3VycmVudCkpIGlmICghc2Vlbi5oYXMoZWRnZS50bykpIHtcbiAgICAgIHNlZW4uYWRkKGVkZ2UudG8pXG4gICAgICBxdWV1ZS5wdXNoKGVkZ2UudG8pXG4gICAgfVxuICB9XG4gIHJldHVybiBmYWxzZVxufVxuXG5jb25zdCBmaW5kU2ltcGxlUGF0aHMgPSAoY29udHJhY3Q6IFJvdXRlQ29udHJhY3QsIGZyb206IHN0cmluZywgdGFyZ2V0OiBzdHJpbmcsIHByZWRpY2F0ZTogKGVkZ2U6IFJvdXRlRWRnZSkgPT4gYm9vbGVhbiwgbGltaXQgPSAyKTogUm91dGVFZGdlW11bXSA9PiB7XG4gIGNvbnN0IHBhdGhzOiBSb3V0ZUVkZ2VbXVtdID0gW11cbiAgY29uc3QgdmlzaXQgPSAoY3VycmVudDogc3RyaW5nLCBzZWVuOiBSZWFkb25seVNldDxzdHJpbmc+LCBwYXRoOiBSb3V0ZUVkZ2VbXSk6IHZvaWQgPT4ge1xuICAgIGlmIChwYXRocy5sZW5ndGggPj0gbGltaXQpIHJldHVyblxuICAgIGlmIChjdXJyZW50ID09PSB0YXJnZXQpIHsgcGF0aHMucHVzaChwYXRoKTsgcmV0dXJuIH1cbiAgICBmb3IgKGNvbnN0IGVkZ2Ugb2YgZWRnZXNGcm9tKGNvbnRyYWN0LCBjdXJyZW50LCBwcmVkaWNhdGUpKSBpZiAoIXNlZW4uaGFzKGVkZ2UudG8pKSB7XG4gICAgICBjb25zdCBuZXh0ID0gbmV3IFNldChzZWVuKVxuICAgICAgbmV4dC5hZGQoZWRnZS50bylcbiAgICAgIHZpc2l0KGVkZ2UudG8sIG5leHQsIFsuLi5wYXRoLCBlZGdlXSlcbiAgICB9XG4gIH1cbiAgdmlzaXQoZnJvbSwgbmV3IFNldChbZnJvbV0pLCBbXSlcbiAgcmV0dXJuIHBhdGhzXG59XG5cbmNvbnN0IHZhbGlkVGFnU2V0ID0gKHRhZ3M6IHVua25vd24pOiB0YWdzIGlzIFJvdXRlVGFncyA9PiB7XG4gIGlmICghdGFncyB8fCB0eXBlb2YgdGFncyAhPT0gJ29iamVjdCcpIHJldHVybiBmYWxzZVxuICBjb25zdCB2YWx1ZXMgPSB0YWdzIGFzIFJlY29yZDxzdHJpbmcsIHVua25vd24+XG4gIHJldHVybiB0YWdLZXlzLmV2ZXJ5KGtleSA9PiBBcnJheS5pc0FycmF5KHZhbHVlc1trZXldKSAmJiB2YWx1ZXNba2V5XS5sZW5ndGggPiAwICYmIHZhbHVlc1trZXldLmV2ZXJ5KHZhbHVlID0+IHR5cGVvZiB2YWx1ZSA9PT0gJ3N0cmluZycgJiYgdmFsdWUubGVuZ3RoID4gMCkpXG59XG5cbmNvbnN0IHZhbGlkYXRlVGFncyA9IChvd25lcjogc3RyaW5nLCB0YWdzOiB1bmtub3duLCBlcnJvcnM6IHN0cmluZ1tdKTogdm9pZCA9PiB7XG4gIGlmICh2YWxpZFRhZ1NldCh0YWdzKSkgcmV0dXJuXG4gIGlmICghdGFncyB8fCB0eXBlb2YgdGFncyAhPT0gJ29iamVjdCcpIHsgZXJyb3JzLnB1c2goYCR7b3duZXJ9OiBtaXNzaW5nIHRhZyBzZXRgKTsgcmV0dXJuIH1cbiAgZm9yIChjb25zdCBrZXkgb2YgdGFnS2V5cykge1xuICAgIGNvbnN0IHZhbHVlcyA9ICh0YWdzIGFzIFJlY29yZDxzdHJpbmcsIHVua25vd24+KVtrZXldXG4gICAgaWYgKCFBcnJheS5pc0FycmF5KHZhbHVlcykgfHwgIXZhbHVlcy5sZW5ndGgpIGVycm9ycy5wdXNoKGAke293bmVyfTogbWlzc2luZyAke2tleX0gdGFnc2ApXG4gICAgZWxzZSBpZiAodmFsdWVzLnNvbWUodmFsdWUgPT4gdHlwZW9mIHZhbHVlICE9PSAnc3RyaW5nJyB8fCAhdmFsdWUubGVuZ3RoKSkgZXJyb3JzLnB1c2goYCR7b3duZXJ9OiBpbnZhbGlkICR7a2V5fSB0YWdgKVxuICB9XG59XG5cbmNvbnN0IHZhbGlkYXRlUmV3YXJkT2ZmZXJzID0gKG9mZmVyczogdW5rbm93biwgZXJyb3JzOiBzdHJpbmdbXSk6IHZvaWQgPT4ge1xuICBpZiAoIUFycmF5LmlzQXJyYXkob2ZmZXJzKSkgeyBlcnJvcnMucHVzaCgncmV3YXJkIG9mZmVyczogbWlzc2luZycpOyByZXR1cm4gfVxuICBjb25zdCBleHBlY3RlZDogQXJyYXk8W1Jld2FyZE9mZmVyWydtaWxlc3RvbmVJZCddLCBSZXdhcmRPZmZlclsna2luZCddXT4gPSBbWyd3YXljYWNoZScsICd3YXljYWNoZSddLCBbJ2Jvb24tdGVhY2gnLCAnYm9vbiddLCBbJ2Jvb24tdGVzdCcsICdib29uJ10sIFsnYm9vbi1wYXlvZmYnLCAnYm9vbiddXVxuICBpZiAob2ZmZXJzLmxlbmd0aCAhPT0gZXhwZWN0ZWQubGVuZ3RoKSBlcnJvcnMucHVzaChgcmV3YXJkIG9mZmVyczogZXhwZWN0ZWQgJHtleHBlY3RlZC5sZW5ndGh9LCBmb3VuZCAke29mZmVycy5sZW5ndGh9YClcbiAgZm9yIChjb25zdCBbbWlsZXN0b25lSWQsIGtpbmRdIG9mIGV4cGVjdGVkKSB7XG4gICAgY29uc3Qgb2ZmZXIgPSBvZmZlcnMuZmluZChjYW5kaWRhdGUgPT4gdHlwZW9mIGNhbmRpZGF0ZSA9PT0gJ29iamVjdCcgJiYgY2FuZGlkYXRlICE9PSBudWxsICYmIChjYW5kaWRhdGUgYXMgeyBtaWxlc3RvbmVJZD86IHVua25vd24gfSkubWlsZXN0b25lSWQgPT09IG1pbGVzdG9uZUlkKVxuICAgIGlmICghb2ZmZXIgfHwgdHlwZW9mIG9mZmVyICE9PSAnb2JqZWN0JykgeyBlcnJvcnMucHVzaChgcmV3YXJkIG9mZmVyICR7bWlsZXN0b25lSWR9OiBtaXNzaW5nYCk7IGNvbnRpbnVlIH1cbiAgICBjb25zdCB2YWx1ZSA9IG9mZmVyIGFzIFJlY29yZDxzdHJpbmcsIHVua25vd24+XG4gICAgaWYgKCF2YWx1ZS5pZCB8fCB0eXBlb2YgdmFsdWUuaWQgIT09ICdzdHJpbmcnKSBlcnJvcnMucHVzaChgcmV3YXJkIG9mZmVyICR7bWlsZXN0b25lSWR9OiBtaXNzaW5nIGlkYClcbiAgICBpZiAodmFsdWUua2luZCAhPT0ga2luZCkgZXJyb3JzLnB1c2goYHJld2FyZCBvZmZlciAke21pbGVzdG9uZUlkfTogaW52YWxpZCBraW5kYClcbiAgICBpZiAoIUFycmF5LmlzQXJyYXkodmFsdWUuY2hvaWNlcykgfHwgdmFsdWUuY2hvaWNlcy5sZW5ndGggIT09IDMpIHsgZXJyb3JzLnB1c2goYHJld2FyZCBvZmZlciAke21pbGVzdG9uZUlkfTogZXhwZWN0ZWQgdGhyZWUgY2hvaWNlc2ApOyBjb250aW51ZSB9XG4gICAgY29uc3Qgcm9sZXMgPSBuZXcgU2V0PHN0cmluZz4oKVxuICAgIGZvciAoY29uc3QgY2hvaWNlIG9mIHZhbHVlLmNob2ljZXMpIHtcbiAgICAgIGlmICghY2hvaWNlIHx8IHR5cGVvZiBjaG9pY2UgIT09ICdvYmplY3QnKSB7IGVycm9ycy5wdXNoKGByZXdhcmQgb2ZmZXIgJHttaWxlc3RvbmVJZH06IGludmFsaWQgY2hvaWNlYCk7IGNvbnRpbnVlIH1cbiAgICAgIGNvbnN0IGFubm90YXRpb24gPSBjaG9pY2UgYXMgUmVjb3JkPHN0cmluZywgdW5rbm93bj5cbiAgICAgIGlmICh0eXBlb2YgYW5ub3RhdGlvbi5pZCAhPT0gJ3N0cmluZycgfHwgIWFubm90YXRpb24uaWQpIGVycm9ycy5wdXNoKGByZXdhcmQgb2ZmZXIgJHttaWxlc3RvbmVJZH06IGNob2ljZSBtaXNzaW5nIGlkYClcbiAgICAgIGlmIChhbm5vdGF0aW9uLnJvbGUgIT09ICdzYWZlJyAmJiBhbm5vdGF0aW9uLnJvbGUgIT09ICdyaXNreScgJiYgYW5ub3RhdGlvbi5yb2xlICE9PSAnc2lkZWdyYWRlJykgZXJyb3JzLnB1c2goYHJld2FyZCBvZmZlciAke21pbGVzdG9uZUlkfTogY2hvaWNlIGludmFsaWQgcm9sZWApXG4gICAgICBlbHNlIHJvbGVzLmFkZChhbm5vdGF0aW9uLnJvbGUpXG4gICAgICBpZiAodHlwZW9mIGFubm90YXRpb24ucHJvYmxlbSAhPT0gJ3N0cmluZycgfHwgdHlwZW9mIGFubm90YXRpb24udGVycmFpbiAhPT0gJ3N0cmluZycgfHwgdHlwZW9mIGFubm90YXRpb24ucm91dGUgIT09ICdzdHJpbmcnIHx8IHR5cGVvZiBhbm5vdGF0aW9uLnBheW9mZiAhPT0gJ3N0cmluZycgfHwgKGFubm90YXRpb24uYmlvbWVGaXQgIT09ICdsb2NhbCcgJiYgYW5ub3RhdGlvbi5iaW9tZUZpdCAhPT0gJ2dsb2JhbCcpKSBlcnJvcnMucHVzaChgcmV3YXJkIG9mZmVyICR7bWlsZXN0b25lSWR9OiBjaG9pY2UgbWlzc2luZyBhbm5vdGF0aW9uYClcbiAgICB9XG4gICAgaWYgKHJvbGVzLnNpemUgIT09IDMpIGVycm9ycy5wdXNoKGByZXdhcmQgb2ZmZXIgJHttaWxlc3RvbmVJZH06IHJlcXVpcmVzIHNhZmUsIHJpc2t5LCBhbmQgc2lkZWdyYWRlIGNob2ljZXNgKVxuICB9XG59XG5cbmV4cG9ydCBjb25zdCB2YWxpZGF0ZVJvdXRlQ29udHJhY3QgPSAoY29udHJhY3Q6IFJvdXRlQ29udHJhY3QpOiBSb3V0ZUNvbnRyYWN0VmFsaWRhdGlvbiA9PiB7XG4gIGNvbnN0IGVycm9yczogc3RyaW5nW10gPSBbXVxuICB2YWxpZGF0ZVJld2FyZE9mZmVycyhjb250cmFjdC5yZXdhcmRPZmZlcnMsIGVycm9ycylcbiAgY29uc3Qgbm9kZUlkcyA9IG5ldyBTZXQ8c3RyaW5nPigpXG4gIGZvciAoY29uc3Qgbm9kZSBvZiBjb250cmFjdC5ub2Rlcykge1xuICAgIGNvbnN0IG93bmVyID0gYG5vZGUgJHtub2RlLmlkIHx8ICc8ZW1wdHk+J31gXG4gICAgaWYgKCFub2RlLmlkKSBlcnJvcnMucHVzaChgJHtvd25lcn06IG1pc3NpbmcgaWRgKVxuICAgIGVsc2UgaWYgKG5vZGVJZHMuaGFzKG5vZGUuaWQpKSBlcnJvcnMucHVzaChgJHtvd25lcn06IGR1cGxpY2F0ZSBpZGApXG4gICAgbm9kZUlkcy5hZGQobm9kZS5pZClcbiAgICBpZiAoIVJPVVRFX05PREVfS0lORFMuaW5jbHVkZXMobm9kZS5raW5kKSkgZXJyb3JzLnB1c2goYCR7b3duZXJ9OiBpbnZhbGlkIGtpbmQgJHtub2RlLmtpbmR9YClcbiAgICB2YWxpZGF0ZVRhZ3Mob3duZXIsIG5vZGUudGFncywgZXJyb3JzKVxuICB9XG4gIGNvbnN0IHJlcXVpcmVkOiBSb3V0ZU5vZGVLaW5kW10gPSBbJ3N0YXJ0JywgJ2xhbmRtYXJrJywgJ2ZvcmsnLCAnb2JqZWN0aXZlJywgJ29wdGlvbmFsUmV3YXJkJ11cbiAgZm9yIChjb25zdCBraW5kIG9mIHJlcXVpcmVkKSB7XG4gICAgY29uc3QgY291bnQgPSBub2Rlc09mS2luZChjb250cmFjdCwga2luZCkubGVuZ3RoXG4gICAgaWYgKGNvdW50ICE9PSAxKSBlcnJvcnMucHVzaChgbm9kZSBraW5kICR7a2luZH06IGV4cGVjdGVkIG9uZSwgZm91bmQgJHtjb3VudH1gKVxuICB9XG4gIGNvbnN0IGVuZHMgPSBbLi4ubm9kZXNPZktpbmQoY29udHJhY3QsICdleGl0JyksIC4uLm5vZGVzT2ZLaW5kKGNvbnRyYWN0LCAnYm9zcycpXVxuICBpZiAoZW5kcy5sZW5ndGggIT09IDEpIGVycm9ycy5wdXNoKGBub2RlIGtpbmQgZW5kOiBleHBlY3RlZCBvbmUgZXhpdCBvciBib3NzLCBmb3VuZCAke2VuZHMubGVuZ3RofWApXG5cbiAgY29uc3QgZWRnZUlkcyA9IG5ldyBTZXQ8c3RyaW5nPigpXG4gIGZvciAoY29uc3QgZWRnZSBvZiBjb250cmFjdC5lZGdlcykge1xuICAgIGNvbnN0IG93bmVyID0gYGVkZ2UgJHtlZGdlLmlkIHx8ICc8ZW1wdHk+J31gXG4gICAgaWYgKCFlZGdlLmlkKSBlcnJvcnMucHVzaChgJHtvd25lcn06IG1pc3NpbmcgaWRgKVxuICAgIGVsc2UgaWYgKGVkZ2VJZHMuaGFzKGVkZ2UuaWQpKSBlcnJvcnMucHVzaChgJHtvd25lcn06IGR1cGxpY2F0ZSBpZGApXG4gICAgZWRnZUlkcy5hZGQoZWRnZS5pZClcbiAgICBpZiAoIW5vZGVJZHMuaGFzKGVkZ2UuZnJvbSkpIGVycm9ycy5wdXNoKGAke293bmVyfTogdW5rbm93biBzb3VyY2Ugbm9kZSAke2VkZ2UuZnJvbX1gKVxuICAgIGlmICghbm9kZUlkcy5oYXMoZWRnZS50bykpIGVycm9ycy5wdXNoKGAke293bmVyfTogdW5rbm93biB0YXJnZXQgbm9kZSAke2VkZ2UudG99YClcbiAgICBpZiAoIWVkZ2UubW9kZXMubGVuZ3RoKSBlcnJvcnMucHVzaChgJHtvd25lcn06IG1pc3Npbmcgcm91dGUgbW9kZXNgKVxuICAgIGZvciAoY29uc3QgbW9kZSBvZiBlZGdlLm1vZGVzKSBpZiAoIVJPVVRFX0VER0VfTU9ERVMuaW5jbHVkZXMobW9kZSkpIGVycm9ycy5wdXNoKGAke293bmVyfTogaW52YWxpZCByb3V0ZSBtb2RlICR7bW9kZX1gKVxuICAgIHZhbGlkYXRlVGFncyhvd25lciwgZWRnZS50YWdzLCBlcnJvcnMpXG4gIH1cblxuICBjb25zdCBzdGFydCA9IG5vZGVzT2ZLaW5kKGNvbnRyYWN0LCAnc3RhcnQnKVswXVxuICBjb25zdCBmb3JrID0gbm9kZXNPZktpbmQoY29udHJhY3QsICdmb3JrJylbMF1cbiAgY29uc3QgcmV3YXJkID0gbm9kZXNPZktpbmQoY29udHJhY3QsICdvcHRpb25hbFJld2FyZCcpWzBdXG4gIGNvbnN0IGVuZCA9IGVuZHNbMF1cbiAgaWYgKHN0YXJ0ICYmIGVuZCkge1xuICAgIGlmICghcmVhY2hlcyhjb250cmFjdCwgc3RhcnQuaWQsIGVuZC5pZCkpIGVycm9ycy5wdXNoKGBub2RlICR7ZW5kLmlkfTogdW5yZWFjaGFibGUgZnJvbSBzdGFydCAke3N0YXJ0LmlkfWApXG4gICAgY29uc3QgbWFpblBhdGhzID0gZmluZFNpbXBsZVBhdGhzKGNvbnRyYWN0LCBzdGFydC5pZCwgZW5kLmlkLCBlZGdlID0+IGVkZ2UubW9kZXMuaW5jbHVkZXMoJ21haW4nKSlcbiAgICBpZiAobWFpblBhdGhzLmxlbmd0aCAhPT0gMSkgZXJyb3JzLnB1c2goYG5vZGUgJHtzdGFydC5pZH06IGV4cGVjdGVkIGV4YWN0bHkgb25lIG1haW4gcm91dGUgdG8gJHtlbmQuaWR9LCBmb3VuZCAke21haW5QYXRocy5sZW5ndGh9YClcbiAgICBpZiAobWFpblBhdGhzLmxlbmd0aCA9PT0gMSkge1xuICAgICAgY29uc3QgbWFpblBhdGhFZGdlcyA9IG5ldyBTZXQobWFpblBhdGhzWzBdLm1hcChlZGdlID0+IGVkZ2UuaWQpKVxuICAgICAgZm9yIChjb25zdCBlZGdlIG9mIGNvbnRyYWN0LmVkZ2VzKSBpZiAoZWRnZS5tb2Rlcy5pbmNsdWRlcygnbWFpbicpICYmICFtYWluUGF0aEVkZ2VzLmhhcyhlZGdlLmlkKSkgZXJyb3JzLnB1c2goYGVkZ2UgJHtlZGdlLmlkfTogbWFpbiByb3V0ZSBlZGdlIGlzIGRpc2Nvbm5lY3RlZCBmcm9tIHRoZSBtYWluIHJvdXRlYClcbiAgICB9XG4gIH1cbiAgaWYgKGZvcmsgJiYgZW5kKSB7XG4gICAgY29uc3QgY2hvaWNlcyA9IGVkZ2VzRnJvbShjb250cmFjdCwgZm9yay5pZCkuZmlsdGVyKGVkZ2UgPT4gbm9kZUlkcy5oYXMoZWRnZS50bykpXG4gICAgY29uc3Qgc2FmZSA9IGNob2ljZXMuZmlsdGVyKGVkZ2UgPT4gZWRnZS5tb2Rlcy5pbmNsdWRlcygnc2FmZScpKVxuICAgIGNvbnN0IGNvc3RseSA9IGNob2ljZXMuZmlsdGVyKGVkZ2UgPT4gZWRnZS5tb2Rlcy5pbmNsdWRlcygnY29zdGx5JykpXG4gICAgaWYgKCFzYWZlLmxlbmd0aCkgZXJyb3JzLnB1c2goYG5vZGUgJHtmb3JrLmlkfTogbWlzc2luZyBzYWZlIHJvdXRlIGNob2ljZWApXG4gICAgaWYgKCFjb3N0bHkubGVuZ3RoKSBlcnJvcnMucHVzaChgbm9kZSAke2ZvcmsuaWR9OiBtaXNzaW5nIGNvc3RseSByb3V0ZSBjaG9pY2VgKVxuICAgIGlmIChzYWZlLmxlbmd0aCAmJiBjb3N0bHkubGVuZ3RoICYmIHNhZmUuc29tZShlZGdlID0+IGNvc3RseS5zb21lKG90aGVyID0+IG90aGVyLnRvID09PSBlZGdlLnRvKSkpIGVycm9ycy5wdXNoKGBub2RlICR7Zm9yay5pZH06IHNhZmUgYW5kIGNvc3RseSByb3V0ZXMgc2hhcmUgYSBkZXN0aW5hdGlvbmApXG4gICAgZm9yIChjb25zdCBlZGdlIG9mIFsuLi5zYWZlLCAuLi5jb3N0bHldKSBpZiAoIXJlYWNoZXMoY29udHJhY3QsIGVkZ2UudG8sIGVuZC5pZCkpIGVycm9ycy5wdXNoKGBlZGdlICR7ZWRnZS5pZH06IHJvdXRlIGNob2ljZSBjYW5ub3QgcmVhY2ggJHtlbmQuaWR9YClcbiAgfVxuICBpZiAocmV3YXJkICYmIHN0YXJ0ICYmIGVuZCkge1xuICAgIGlmICghcmVhY2hlcyhjb250cmFjdCwgc3RhcnQuaWQsIHJld2FyZC5pZCkpIGVycm9ycy5wdXNoKGBub2RlICR7cmV3YXJkLmlkfTogb3B0aW9uYWwgcGF5b2ZmIGlzIHVucmVhY2hhYmxlIGZyb20gJHtzdGFydC5pZH1gKVxuICAgIGlmICghcmVhY2hlcyhjb250cmFjdCwgcmV3YXJkLmlkLCBlbmQuaWQpKSBlcnJvcnMucHVzaChgbm9kZSAke3Jld2FyZC5pZH06IG9wdGlvbmFsIHBheW9mZiBjYW5ub3QgcmVqb2luIHJvdXRlIHRvICR7ZW5kLmlkfWApXG4gICAgY29uc3Qgb3B0aW9uYWxFZGdlcyA9IGNvbnRyYWN0LmVkZ2VzLmZpbHRlcihlZGdlID0+IGVkZ2UudG8gPT09IHJld2FyZC5pZCAmJiBlZGdlLm1vZGVzLmluY2x1ZGVzKCdvcHRpb25hbCcpKVxuICAgIGlmICghb3B0aW9uYWxFZGdlcy5sZW5ndGgpIGVycm9ycy5wdXNoKGBub2RlICR7cmV3YXJkLmlkfTogbWlzc2luZyBvcHRpb25hbCByb3V0ZSBlZGdlYClcbiAgfVxuICByZXR1cm4geyB2YWxpZDogZXJyb3JzLmxlbmd0aCA9PT0gMCwgZXJyb3JzIH1cbn1cblxuY29uc3QgYXNzZXJ0R2VuZXJhdGlvbklucHV0ID0gKGlucHV0OiBSb3V0ZUNvbnRyYWN0SW5wdXQpOiB2b2lkID0+IHtcbiAgaWYgKCFOdW1iZXIuaXNJbnRlZ2VyKGlucHV0LmNhbXBhaWduU2VlZCkpIHRocm93IG5ldyBFcnJvcigncm91dGUgY29udHJhY3QgY2FtcGFpZ24gc2VlZCBtdXN0IGJlIGFuIGludGVnZXInKVxuICBpZiAoIU51bWJlci5pc0ludGVnZXIoaW5wdXQuZmxvb3JJbmRleCkgfHwgaW5wdXQuZmxvb3JJbmRleCA8IDApIHRocm93IG5ldyBFcnJvcigncm91dGUgY29udHJhY3QgZmxvb3IgaW5kZXggbXVzdCBiZSBub24tbmVnYXRpdmUnKVxuICBpZiAoIU51bWJlci5pc0ludGVnZXIoaW5wdXQuYXJlYUZsb29yKSB8fCBpbnB1dC5hcmVhRmxvb3IgPCAwIHx8IGlucHV0LmFyZWFGbG9vciA+IDMpIHRocm93IG5ldyBFcnJvcigncm91dGUgY29udHJhY3QgYXJlYSBmbG9vciBtdXN0IGJlIGJldHdlZW4gMCBhbmQgMycpXG4gIGlmICghaW5wdXQucmVjaXBlSWQpIHRocm93IG5ldyBFcnJvcigncm91dGUgY29udHJhY3QgcmVjaXBlIGlkIGlzIHJlcXVpcmVkJylcbiAgaWYgKCFpbnB1dC5lc2NhbGF0aW9uVmFyaWFudCkgdGhyb3cgbmV3IEVycm9yKCdyb3V0ZSBjb250cmFjdCBlc2NhbGF0aW9uIHZhcmlhbnQgaXMgcmVxdWlyZWQnKVxufVxuXG5leHBvcnQgY29uc3QgZ2VuZXJhdGVSb3V0ZUNvbnRyYWN0ID0gKGlucHV0OiBSb3V0ZUNvbnRyYWN0SW5wdXQpOiBSb3V0ZUNvbnRyYWN0ID0+IHtcbiAgYXNzZXJ0R2VuZXJhdGlvbklucHV0KGlucHV0KVxuICBjb25zdCBybmcgPSBybmdGb3IoaW5wdXQuY2FtcGFpZ25TZWVkLCAnZ2VuZXJhdGlvbicsIGlucHV0LmZsb29ySW5kZXgsICdyb3V0ZS1jb250cmFjdCcsIGlucHV0LmJpb21lLCBpbnB1dC5yZWNpcGVJZCwgaW5wdXQuZXNjYWxhdGlvblZhcmlhbnQpXG4gIGNvbnN0IGJpb21lID0gYmlvbWVSb3V0ZVRhZ3NbaW5wdXQuYmlvbWVdXG4gIGNvbnN0IHJvdXRlVGVycmFpbiA9IHJuZy5jaGFuY2UoNTApID8gYmlvbWUudGVycmFpbiA6IGJpb21lLnByZXNzdXJlXG4gIGNvbnN0IGVuZEtpbmQ6IFJvdXRlTm9kZUtpbmQgPSBpbnB1dC5hcmVhRmxvb3IgPT09IDMgPyAnYm9zcycgOiAnZXhpdCdcbiAgY29uc3QgcHJlZml4ID0gYCR7aW5wdXQuYmlvbWV9OiR7aW5wdXQuZmxvb3JJbmRleH06JHtpbnB1dC5yZWNpcGVJZH06JHtpbnB1dC5lc2NhbGF0aW9uVmFyaWFudH1gXG4gIGNvbnN0IHRhZ3MgPSAob3ZlcnJpZGVzOiBQYXJ0aWFsPFJvdXRlVGFncz4pOiBSb3V0ZVRhZ3MgPT4gdGFnU2V0KHsgZXNjYWxhdGlvbjogW2lucHV0LmVzY2FsYXRpb25WYXJpYW50XSwgLi4ub3ZlcnJpZGVzIH0pXG4gIGNvbnN0IG5vZGUgPSAoa2luZDogUm91dGVOb2RlS2luZCwgb3ZlcnJpZGVzOiBQYXJ0aWFsPFJvdXRlVGFncz4pOiBSb3V0ZU5vZGUgPT4gKHsgaWQ6IGAke3ByZWZpeH06JHtraW5kfWAsIGtpbmQsIHRhZ3M6IHRhZ3Mob3ZlcnJpZGVzKSB9KVxuICBjb25zdCBub2RlcyA9IFtcbiAgICBub2RlKCdzdGFydCcsIHsgdGVycmFpbjogWydlbnRyeSddLCBlbmNvdW50ZXI6IFsnbm9uZSddLCByZXdhcmQ6IFsnbm9uZSddLCBnYXRlOiBbJ29wZW4nXSwgdmlzdWFsOiBbJ2VudHJ5LW1hcmtlciddIH0pLFxuICAgIG5vZGUoJ2xhbmRtYXJrJywgeyB0ZXJyYWluOiBbYmlvbWUudGVycmFpbl0sIGVuY291bnRlcjogWydub25lJ10sIHJld2FyZDogWydvcmllbnRhdGlvbiddLCBnYXRlOiBbJ29wZW4nXSwgdmlzdWFsOiBbYmlvbWUubGFuZG1hcmtdIH0pLFxuICAgIG5vZGUoJ2ZvcmsnLCB7IHRlcnJhaW46IFtyb3V0ZVRlcnJhaW5dLCBlbmNvdW50ZXI6IFtiaW9tZS5lbmNvdW50ZXJdLCByZXdhcmQ6IFsnY2hvaWNlJ10sIGdhdGU6IFsnb3BlbiddLCB2aXN1YWw6IFsnYnJhbmNoLW1hcmtlciddIH0pLFxuICAgIG5vZGUoJ29iamVjdGl2ZScsIHsgdGVycmFpbjogW2Jpb21lLnRlcnJhaW5dLCBlbmNvdW50ZXI6IFtiaW9tZS5lbmNvdW50ZXJdLCByZXdhcmQ6IFsnb2JqZWN0aXZlJ10sIGdhdGU6IFsna2V5LW9yLWNvdW50ZXJyb3V0ZSddLCB2aXN1YWw6IFsnb2JqZWN0aXZlLW1hcmtlciddIH0pLFxuICAgIG5vZGUoJ29wdGlvbmFsUmV3YXJkJywgeyB0ZXJyYWluOiBbYmlvbWUucHJlc3N1cmVdLCBlbmNvdW50ZXI6IFtiaW9tZS5lbmNvdW50ZXJdLCByZXdhcmQ6IFsnb3B0aW9uYWwtcGF5b2ZmJ10sIGdhdGU6IFsnY29zdGx5LWdhdGUnXSwgdmlzdWFsOiBbJ3Jld2FyZC1tYXJrZXInXSB9KSxcbiAgICBub2RlKGVuZEtpbmQsIHsgdGVycmFpbjogW3JvdXRlVGVycmFpbl0sIGVuY291bnRlcjogW2VuZEtpbmQgPT09ICdib3NzJyA/ICdib3NzJyA6ICdub25lJ10sIHJld2FyZDogWydleGl0LXBheW9mZiddLCBnYXRlOiBbJ3Jlc29sdmVkJ10sIHZpc3VhbDogW2VuZEtpbmQgPT09ICdib3NzJyA/ICdib3NzLW1hcmtlcicgOiAnZXhpdC1tYXJrZXInXSB9KVxuICBdXG4gIGNvbnN0IGVkZ2UgPSAoZnJvbTogUm91dGVOb2RlS2luZCwgdG86IFJvdXRlTm9kZUtpbmQsIG1vZGVzOiBSb3V0ZUVkZ2VNb2RlW10sIG92ZXJyaWRlczogUGFydGlhbDxSb3V0ZVRhZ3M+KTogUm91dGVFZGdlID0+ICh7IGlkOiBgJHtwcmVmaXh9OiR7ZnJvbX06JHt0b306JHttb2Rlcy5qb2luKCcrJyl9YCwgZnJvbTogYCR7cHJlZml4fToke2Zyb219YCwgdG86IGAke3ByZWZpeH06JHt0b31gLCBtb2RlcywgdGFnczogdGFncyhvdmVycmlkZXMpIH0pXG4gIGNvbnN0IGVkZ2VzID0gW1xuICAgIGVkZ2UoJ3N0YXJ0JywgJ2xhbmRtYXJrJywgWydtYWluJ10sIHsgdGVycmFpbjogWydhcHByb2FjaCddLCBlbmNvdW50ZXI6IFsnbm9uZSddLCByZXdhcmQ6IFsnb3JpZW50YXRpb24nXSwgZ2F0ZTogWydvcGVuJ10sIHZpc3VhbDogWydyb3V0ZS1saW5lJ10gfSksXG4gICAgZWRnZSgnbGFuZG1hcmsnLCAnZm9yaycsIFsnbWFpbiddLCB7IHRlcnJhaW46IFtiaW9tZS50ZXJyYWluXSwgZW5jb3VudGVyOiBbYmlvbWUuZW5jb3VudGVyXSwgcmV3YXJkOiBbJ2Nob2ljZSddLCBnYXRlOiBbJ29wZW4nXSwgdmlzdWFsOiBbJ2ZvcmstbGluZSddIH0pLFxuICAgIGVkZ2UoJ2ZvcmsnLCAnb2JqZWN0aXZlJywgWydtYWluJywgJ3NhZmUnXSwgeyB0ZXJyYWluOiBbYmlvbWUudGVycmFpbl0sIGVuY291bnRlcjogWydndWFyZGVkJ10sIHJld2FyZDogWydvYmplY3RpdmUnXSwgZ2F0ZTogWydrZXknXSwgdmlzdWFsOiBbJ3NhZmUtcm91dGUnXSB9KSxcbiAgICBlZGdlKCdmb3JrJywgJ29wdGlvbmFsUmV3YXJkJywgWydjb3N0bHknLCAnb3B0aW9uYWwnXSwgeyB0ZXJyYWluOiBbYmlvbWUucHJlc3N1cmVdLCBlbmNvdW50ZXI6IFsnYW1idXNoJ10sIHJld2FyZDogWydvcHRpb25hbC1wYXlvZmYnXSwgZ2F0ZTogWydjb3N0bHktZ2F0ZSddLCB2aXN1YWw6IFsncmlzay1yb3V0ZSddIH0pLFxuICAgIGVkZ2UoJ29wdGlvbmFsUmV3YXJkJywgJ29iamVjdGl2ZScsIFsnY29zdGx5JywgJ29wdGlvbmFsJ10sIHsgdGVycmFpbjogW3JvdXRlVGVycmFpbl0sIGVuY291bnRlcjogW2Jpb21lLmVuY291bnRlcl0sIHJld2FyZDogWydwYXlvZmYnXSwgZ2F0ZTogWydyZWpvaW4nXSwgdmlzdWFsOiBbJ3JldHVybi1yb3V0ZSddIH0pLFxuICAgIGVkZ2UoJ29iamVjdGl2ZScsIGVuZEtpbmQsIFsnbWFpbiddLCB7IHRlcnJhaW46IFtyb3V0ZVRlcnJhaW5dLCBlbmNvdW50ZXI6IFtlbmRLaW5kID09PSAnYm9zcycgPyAnYm9zcycgOiAnbm9uZSddLCByZXdhcmQ6IFsnY29tcGxldGlvbiddLCBnYXRlOiBbJ3Jlc29sdmVkJ10sIHZpc3VhbDogWydleGl0LXJvdXRlJ10gfSlcbiAgXVxuICByZXR1cm4geyBpZDogYHJvdXRlOiR7cHJlZml4fWAsIC4uLmlucHV0LCBub2RlcywgZWRnZXMsIHJld2FyZE9mZmVyczogcmV3YXJkT2ZmZXJzRm9yKGlucHV0KSB9XG59XG4iXSwibWFwcGluZ3MiOiJBQUFBLFNBQVNBLE1BQU0sUUFBUSxPQUFPO0FBQzlCLFNBQVNDLGVBQWUsUUFBUSxtQkFBbUI7QUFHbkQsT0FBTyxNQUFNQyxnQkFBZ0IsR0FBRyxDQUFDLE9BQU8sRUFBRSxVQUFVLEVBQUUsTUFBTSxFQUFFLFdBQVcsRUFBRSxnQkFBZ0IsRUFBRSxNQUFNLEVBQUUsTUFBTSxDQUFVO0FBRXJILE9BQU8sTUFBTUMsZ0JBQWdCLEdBQUcsQ0FBQyxNQUFNLEVBQUUsTUFBTSxFQUFFLFFBQVEsRUFBRSxVQUFVLENBQVU7QUE0Qy9FLE1BQU1DLE9BQU8sR0FBRyxDQUFDLFNBQVMsRUFBRSxXQUFXLEVBQUUsUUFBUSxFQUFFLE1BQU0sRUFBRSxRQUFRLEVBQUUsWUFBWSxDQUFVO0FBQzNGLE1BQU1DLE1BQU0sR0FBR0EsQ0FBQ0MsSUFBd0IsR0FBRyxDQUFDLENBQUM7RUFBQSxJQUFBQyxhQUFBLEVBQUFDLGVBQUEsRUFBQUMsWUFBQSxFQUFBQyxVQUFBLEVBQUFDLFlBQUEsRUFBQUMsZ0JBQUE7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