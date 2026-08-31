// b3424fa26dad8d762863653c6f6240c2a454f471
import { floorIndex } from './types';
const rules = {
  'mine-breach-room': [{
    rewardProfile: {
      kind: 'high-value-resource',
      label: 'rail cache',
      value: 80,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'resource-opportunity-cost',
      label: 'breach cost',
      detail: 'Opening it spends a route-clearing resource.'
    }
  }, {
    rewardProfile: {
      kind: 'kit-choice',
      label: 'salvage kit',
      value: 75,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'terrain-hazard',
      label: 'unstable rails',
      detail: 'The return passes exposed rail stone.'
    }
  }],
  'wilds-cave': [{
    rewardProfile: {
      kind: 'kit-choice',
      label: 'forager kit',
      value: 80,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'terrain-hazard',
      label: 'root snare',
      detail: 'The hollow leaves hazardous footing.'
    }
  }, {
    rewardProfile: {
      kind: 'companion-lead',
      label: 'trailfolk lead',
      value: 70,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'ambush',
      label: 'nest watch',
      detail: 'A hidden watcher may contest the cache.'
    }
  }],
  'cavern-hidden-chamber': [{
    rewardProfile: {
      kind: 'lore-relic',
      label: 'tide reliquary',
      value: 105,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'terrain-hazard',
      label: 'undertow',
      detail: 'Current pressure makes the chamber costly.'
    }
  }, {
    rewardProfile: {
      kind: 'high-value-resource',
      label: 'sealed stores',
      value: 85,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'route-isolation',
      label: 'flooded return',
      detail: 'The side route can isolate a careless courier.'
    }
  }],
  'ritual-hidden-chamber': [{
    rewardProfile: {
      kind: 'lore-relic',
      label: 'glyph archive',
      value: 110,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'tool-cooldown',
      label: 'ward drag',
      detail: 'The ward taxes the next traversal tool.'
    }
  }, {
    rewardProfile: {
      kind: 'companion-lead',
      label: 'ritualist lead',
      value: 75,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'ambush',
      label: 'guardian echo',
      detail: 'The glyphs can call a defender.'
    }
  }],
  'furnace-service-space': [{
    rewardProfile: {
      kind: 'high-value-resource',
      label: 'service stores',
      value: 85,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'terrain-hazard',
      label: 'smoke lane',
      detail: 'Heat and smoke guard the service route.'
    }
  }, {
    rewardProfile: {
      kind: 'kit-choice',
      label: 'kiln kit',
      value: 80,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'tool-cooldown',
      label: 'heat warp',
      detail: 'The route delays the next tool use.'
    }
  }],
  'cliff-alcove': [{
    rewardProfile: {
      kind: 'kit-choice',
      label: 'climber kit',
      value: 80,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'route-isolation',
      label: 'exposed return',
      detail: 'Wind can cut the alcove off from the main route.'
    }
  }, {
    rewardProfile: {
      kind: 'shortcut-access',
      label: 'ridge bypass',
      value: 100,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'resource-opportunity-cost',
      label: 'rope commitment',
      detail: 'The climb commits scarce rope and time.'
    }
  }],
  'burial-crypt': [{
    rewardProfile: {
      kind: 'companion-lead',
      label: 'ancestor lead',
      value: 90,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'ambush',
      label: 'restless dead',
      detail: 'The crypt can answer with an ambush.'
    }
  }, {
    rewardProfile: {
      kind: 'lore-relic',
      label: 'ossuary relic',
      value: 105,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'route-isolation',
      label: 'sealed procession',
      detail: 'The burial route narrows the return.'
    }
  }],
  'frost-cave': [{
    rewardProfile: {
      kind: 'shortcut-access',
      label: 'ice shelf bypass',
      value: 100,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'tool-cooldown',
      label: 'frozen gear',
      detail: 'Cold delays the next tool cycle.'
    }
  }, {
    rewardProfile: {
      kind: 'lore-relic',
      label: 'rime reliquary',
      value: 110,
      cap: 1,
      duplicateRule: 'once-per-run'
    },
    riskProfile: {
      kind: 'terrain-hazard',
      label: 'frost rime',
      detail: 'The hollow leaves freezing ground.'
    }
  }]
};
const sourceKindFor = sourceId => sourceId.startsWith('mine-breach:') ? 'mine-breach-room' : sourceId.startsWith('wilds-cave:') ? 'wilds-cave' : sourceId.startsWith('cavern-hidden:') ? 'cavern-hidden-chamber' : sourceId.startsWith('ritual-hidden:') ? 'ritual-hidden-chamber' : sourceId.startsWith('furnace-service:') ? 'furnace-service-space' : sourceId.startsWith('cliff-alcove:') ? 'cliff-alcove' : sourceId.startsWith('burial-crypt:') ? 'burial-crypt' : 'frost-cave';
const sourceHash = sourceId => [...sourceId].reduce((value, char) => value * 31 + char.charCodeAt(0) >>> 0, 0);
export const secretRulesForSourceId = sourceId => {
  const choices = rules[sourceKindFor(sourceId)];
  const selected = choices[sourceHash(sourceId) % choices.length];
  return {
    rewardProfile: {
      ...selected.rewardProfile
    },
    riskProfile: {
      ...selected.riskProfile
    }
  };
};
const profileFor = space => space.kind === 'mine-breach-room' ? {
  kind: 'hidden-room',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'fractured rail stone',
  clueChannel: 'terrain',
  accessMethod: 'breach',
  rewardClass: 'supplies',
  risk: 'dust'
} : space.kind === 'wilds-cave' ? {
  kind: 'hidden-room',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'root-choked hollow',
  clueChannel: 'sight',
  accessMethod: 'breach',
  rewardClass: 'supplies',
  risk: 'dust'
} : space.kind === 'cavern-hidden-chamber' ? {
  kind: 'hidden-room',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'current-fed fissure',
  clueChannel: 'sound',
  accessMethod: 'breach',
  rewardClass: 'ritual',
  risk: 'undertow'
} : space.kind === 'ritual-hidden-chamber' ? {
  kind: 'hidden-room',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'broken glyph seam',
  clueChannel: 'ritual',
  accessMethod: 'breach',
  rewardClass: 'ritual',
  risk: 'ward'
} : space.kind === 'furnace-service-space' ? {
  kind: 'hidden-room',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'warm service vent',
  clueChannel: 'sound',
  accessMethod: 'breach',
  rewardClass: 'supplies',
  risk: 'smoke'
} : space.kind === 'cliff-alcove' ? {
  kind: 'side-pocket',
  entryCondition: 'anchored-rope',
  discoveryClue: 'weathered rope anchor',
  clueChannel: 'sight',
  accessMethod: 'climb',
  rewardClass: 'supplies',
  risk: 'fall'
} : space.kind === 'burial-crypt' ? {
  kind: 'side-pocket',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'settled cairn seam',
  clueChannel: 'prop',
  accessMethod: 'breach',
  rewardClass: 'ritual',
  risk: 'spirits'
} : {
  kind: 'hidden-room',
  entryCondition: 'sealed-breakwall',
  discoveryClue: 'rime-covered hollow',
  clueChannel: 'terrain',
  accessMethod: 'breach',
  rewardClass: 'ritual',
  risk: 'cold'
};
const entriesFor = space => space.kind === 'burial-crypt' ? space.entries : [space.entry];
export const placeSecretMetadata = floor => {
  var _floor$sideSpaces, _floor$sideSpaces2;
  const rooms = ((_floor$sideSpaces = floor.sideSpaces) !== null && _floor$sideSpaces !== void 0 ? _floor$sideSpaces : []).map(space => {
    const profile = profileFor(space);
    const rule = secretRulesForSourceId(space.id);
    const id = `secret-room:${space.id}`;
    space.reward.secretId = id;
    return {
      version: 1,
      id,
      sourceId: space.id,
      ...profile,
      ...rule,
      approach: {
        ...space.approach
      },
      entries: entriesFor(space).map(point => ({
        ...point
      })),
      chamber: space.chamber.map(point => ({
        ...point
      })),
      safeFallback: true
    };
  });
  const roomFor = new Map(rooms.map(room => [room.sourceId, room]));
  const routes = ((_floor$sideSpaces2 = floor.sideSpaces) !== null && _floor$sideSpaces2 !== void 0 ? _floor$sideSpaces2 : []).flatMap(space => {
    const room = roomFor.get(space.id);
    const access = room.entries.map((entry, index) => ({
      version: 1,
      id: `secret-route:${room.id}:access:${index}`,
      roomId: room.id,
      kind: 'concealed-passage',
      from: {
        ...room.approach
      },
      entry: {
        ...entry
      },
      entryCondition: room.entryCondition,
      discoveryClue: room.discoveryClue,
      accessMethod: room.accessMethod,
      rewardClass: room.rewardClass,
      risk: room.risk,
      safeFallback: true
    }));
    if (space.kind !== 'mine-breach-room' || !space.rareTransition) return access;
    return [...access, {
      version: 1,
      id: `secret-route:${room.id}:transition`,
      roomId: room.id,
      kind: 'rare-transition',
      from: {
        ...room.approach
      },
      entry: {
        ...space.entry
      },
      entryCondition: room.entryCondition,
      discoveryClue: room.discoveryClue,
      accessMethod: room.accessMethod,
      rewardClass: 'shortcut',
      risk: room.risk,
      safeFallback: true,
      destination: {
        biome: space.rareTransition.targetBiome,
        floor: space.rareTransition.targetFloor
      },
      direction: 'one-way',
      arrival: 'floor-start',
      returnSemantics: 'no-return'
    }];
  });
  floor.secretRooms = rooms;
  floor.secretRoutes = routes;
};
const distance = (left, right) => Math.max(Math.abs(left.x - right.x), Math.abs(left.y - right.y));
const visible = (floor, point) => {
  var _floor$tiles$floorInd;
  return ((_floor$tiles$floorInd = floor.tiles[floorIndex(floor, point.x, point.y)]) === null || _floor$tiles$floorInd === void 0 ? void 0 : _floor$tiles$floorInd.visible) === true;
};
export const secretClueTrigger = {
  sight: 'see the marked entry',
  sound: 'hear it within two tiles',
  prop: 'inspect its marker with C',
  terrain: 'stand on its telltale terrain',
  ritual: 'work an Astral charm nearby'
};
export const secretInteractionHint = room => room.accessMethod === 'breach' ? 'Use B beside the sealed entry to breach it.' : 'Follow the rope-marked entry to climb in.';
export const isSecretDiscovered = room => room.discovery !== undefined;
export const secretDiscoveryMessage = room => {
  var _room$discovery$chann, _room$discovery;
  const channel = (_room$discovery$chann = (_room$discovery = room.discovery) === null || _room$discovery === void 0 ? void 0 : _room$discovery.channel) !== null && _room$discovery$chann !== void 0 ? _room$discovery$chann : room.clueChannel;
  return `Secret found by ${channel} (${secretClueTrigger[channel]}): ${room.discoveryClue}. ${secretInteractionHint(room)}`;
};
const clueIsAvailable = (state, room, channel) => channel === 'sight' ? room.entries.some(point => visible(state.floor, point)) : channel === 'sound' ? distance(state.hero, room.approach) <= 2 : channel === 'prop' || channel === 'terrain' ? distance(state.hero, room.approach) === 0 : distance(state.hero, room.approach) <= 4;
export const discoverSecretClues = (state, channel) => {
  var _state$floor$secretRo;
  return ((_state$floor$secretRo = state.floor.secretRooms) !== null && _state$floor$secretRo !== void 0 ? _state$floor$secretRo : []).filter(room => {
    if (room.discovery || room.clueChannel !== channel || !clueIsAvailable(state, room, channel)) return false;
    room.discovery = {
      channel,
      turn: state.turn
    };
    return true;
  });
};
export const claimSecretReward = (state, secretId) => {
  var _state$floor$secretRo2;
  const room = (_state$floor$secretRo2 = state.floor.secretRooms) === null || _state$floor$secretRo2 === void 0 ? void 0 : _state$floor$secretRo2.find(candidate => candidate.id === secretId);
  if (!room) return undefined;
  if (room.resolution) return 'already-resolved';
  const resolution = {
    turn: state.turn,
    rewardKind: room.rewardProfile.kind,
    rewardValue: room.rewardProfile.value,
    riskKind: room.riskProfile.kind
  };
  room.resolution = resolution;
  return {
    room,
    resolution
  };
};
export const secretResolutionMessage = room => `Secret resolved — ${room.rewardProfile.label} (+${room.rewardProfile.value} exploration). Risk: ${room.riskProfile.label}; ${room.riskProfile.detail}`;
export const secretShortcutReport = route => route.destination ? `${route.direction === 'two-way' ? 'two-way' : 'one-way'} ${route.destination.biome} shortcut to floor ${route.destination.floor + 1}, arrival at floor start; ${route.returnSemantics === 'return-link' ? 'return link available' : 'no return'}` : 'shortcut destination unavailable';
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJmbG9vckluZGV4IiwicnVsZXMiLCJyZXdhcmRQcm9maWxlIiwia2luZCIsImxhYmVsIiwidmFsdWUiLCJjYXAiLCJkdXBsaWNhdGVSdWxlIiwicmlza1Byb2ZpbGUiLCJkZXRhaWwiLCJzb3VyY2VLaW5kRm9yIiwic291cmNlSWQiLCJzdGFydHNXaXRoIiwic291cmNlSGFzaCIsInJlZHVjZSIsImNoYXIiLCJjaGFyQ29kZUF0Iiwic2VjcmV0UnVsZXNGb3JTb3VyY2VJZCIsImNob2ljZXMiLCJzZWxlY3RlZCIsImxlbmd0aCIsInByb2ZpbGVGb3IiLCJzcGFjZSIsImVudHJ5Q29uZGl0aW9uIiwiZGlzY292ZXJ5Q2x1ZSIsImNsdWVDaGFubmVsIiwiYWNjZXNzTWV0aG9kIiwicmV3YXJkQ2xhc3MiLCJyaXNrIiwiZW50cmllc0ZvciIsImVudHJpZXMiLCJlbnRyeSIsInBsYWNlU2VjcmV0TWV0YWRhdGEiLCJmbG9vciIsIl9mbG9vciRzaWRlU3BhY2VzIiwiX2Zsb29yJHNpZGVTcGFjZXMyIiwicm9vbXMiLCJzaWRlU3BhY2VzIiwibWFwIiwicHJvZmlsZSIsInJ1bGUiLCJpZCIsInJld2FyZCIsInNlY3JldElkIiwidmVyc2lvbiIsImFwcHJvYWNoIiwicG9pbnQiLCJjaGFtYmVyIiwic2FmZUZhbGxiYWNrIiwicm9vbUZvciIsIk1hcCIsInJvb20iLCJyb3V0ZXMiLCJmbGF0TWFwIiwiZ2V0IiwiYWNjZXNzIiwiaW5kZXgiLCJyb29tSWQiLCJmcm9tIiwicmFyZVRyYW5zaXRpb24iLCJkZXN0aW5hdGlvbiIsImJpb21lIiwidGFyZ2V0QmlvbWUiLCJ0YXJnZXRGbG9vciIsImRpcmVjdGlvbiIsImFycml2YWwiLCJyZXR1cm5TZW1hbnRpY3MiLCJzZWNyZXRSb29tcyIsInNlY3JldFJvdXRlcyIsImRpc3RhbmNlIiwibGVmdCIsInJpZ2h0IiwiTWF0aCIsIm1heCIsImFicyIsIngiLCJ5IiwidmlzaWJsZSIsIl9mbG9vciR0aWxlcyRmbG9vckluZCIsInRpbGVzIiwic2VjcmV0Q2x1ZVRyaWdnZXIiLCJzaWdodCIsInNvdW5kIiwicHJvcCIsInRlcnJhaW4iLCJyaXR1YWwiLCJzZWNyZXRJbnRlcmFjdGlvbkhpbnQiLCJpc1NlY3JldERpc2NvdmVyZWQiLCJkaXNjb3ZlcnkiLCJ1bmRlZmluZWQiLCJzZWNyZXREaXNjb3ZlcnlNZXNzYWdlIiwiX3Jvb20kZGlzY292ZXJ5JGNoYW5uIiwiX3Jvb20kZGlzY292ZXJ5IiwiY2hhbm5lbCIsImNsdWVJc0F2YWlsYWJsZSIsInN0YXRlIiwic29tZSIsImhlcm8iLCJkaXNjb3ZlclNlY3JldENsdWVzIiwiX3N0YXRlJGZsb29yJHNlY3JldFJvIiwiZmlsdGVyIiwidHVybiIsImNsYWltU2VjcmV0UmV3YXJkIiwiX3N0YXRlJGZsb29yJHNlY3JldFJvMiIsImZpbmQiLCJjYW5kaWRhdGUiLCJyZXNvbHV0aW9uIiwicmV3YXJkS2luZCIsInJld2FyZFZhbHVlIiwicmlza0tpbmQiLCJzZWNyZXRSZXNvbHV0aW9uTWVzc2FnZSIsInNlY3JldFNob3J0Y3V0UmVwb3J0Iiwicm91dGUiXSwic291cmNlcyI6WyJzZWNyZXRzLnRzIl0sInNvdXJjZXNDb250ZW50IjpbImltcG9ydCB7IGZsb29ySW5kZXgsIHR5cGUgRmxvb3IsIHR5cGUgUnVuU3RhdGUsIHR5cGUgU2VjcmV0QWNjZXNzTWV0aG9kLCB0eXBlIFNlY3JldENsdWVDaGFubmVsLCB0eXBlIFNlY3JldEVudHJ5Q29uZGl0aW9uLCB0eXBlIFNlY3JldFJlc29sdXRpb24sIHR5cGUgU2VjcmV0UmV3YXJkQ2xhc3MsIHR5cGUgU2VjcmV0UmV3YXJkUHJvZmlsZSwgdHlwZSBTZWNyZXRSaXNrLCB0eXBlIFNlY3JldFJpc2tQcm9maWxlLCB0eXBlIFNlY3JldFJvb20sIHR5cGUgU2VjcmV0Um9vbUtpbmQsIHR5cGUgU2VjcmV0Um91dGUsIHR5cGUgU2lkZVNwYWNlIH0gZnJvbSAnLi90eXBlcydcblxudHlwZSBTZWNyZXRSdWxlcyA9IHsgcmV3YXJkUHJvZmlsZTogU2VjcmV0UmV3YXJkUHJvZmlsZTsgcmlza1Byb2ZpbGU6IFNlY3JldFJpc2tQcm9maWxlIH1cbnR5cGUgU2VjcmV0U291cmNlS2luZCA9IFNpZGVTcGFjZVsna2luZCddXG5cbmNvbnN0IHJ1bGVzOiBSZWNvcmQ8U2VjcmV0U291cmNlS2luZCwgcmVhZG9ubHkgU2VjcmV0UnVsZXNbXT4gPSB7XG4gICdtaW5lLWJyZWFjaC1yb29tJzogW1xuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAnaGlnaC12YWx1ZS1yZXNvdXJjZScsIGxhYmVsOiAncmFpbCBjYWNoZScsIHZhbHVlOiA4MCwgY2FwOiAxLCBkdXBsaWNhdGVSdWxlOiAnb25jZS1wZXItcnVuJyB9LCByaXNrUHJvZmlsZTogeyBraW5kOiAncmVzb3VyY2Utb3Bwb3J0dW5pdHktY29zdCcsIGxhYmVsOiAnYnJlYWNoIGNvc3QnLCBkZXRhaWw6ICdPcGVuaW5nIGl0IHNwZW5kcyBhIHJvdXRlLWNsZWFyaW5nIHJlc291cmNlLicgfSB9LFxuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAna2l0LWNob2ljZScsIGxhYmVsOiAnc2FsdmFnZSBraXQnLCB2YWx1ZTogNzUsIGNhcDogMSwgZHVwbGljYXRlUnVsZTogJ29uY2UtcGVyLXJ1bicgfSwgcmlza1Byb2ZpbGU6IHsga2luZDogJ3RlcnJhaW4taGF6YXJkJywgbGFiZWw6ICd1bnN0YWJsZSByYWlscycsIGRldGFpbDogJ1RoZSByZXR1cm4gcGFzc2VzIGV4cG9zZWQgcmFpbCBzdG9uZS4nIH0gfVxuICBdLFxuICAnd2lsZHMtY2F2ZSc6IFtcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ2tpdC1jaG9pY2UnLCBsYWJlbDogJ2ZvcmFnZXIga2l0JywgdmFsdWU6IDgwLCBjYXA6IDEsIGR1cGxpY2F0ZVJ1bGU6ICdvbmNlLXBlci1ydW4nIH0sIHJpc2tQcm9maWxlOiB7IGtpbmQ6ICd0ZXJyYWluLWhhemFyZCcsIGxhYmVsOiAncm9vdCBzbmFyZScsIGRldGFpbDogJ1RoZSBob2xsb3cgbGVhdmVzIGhhemFyZG91cyBmb290aW5nLicgfSB9LFxuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAnY29tcGFuaW9uLWxlYWQnLCBsYWJlbDogJ3RyYWlsZm9sayBsZWFkJywgdmFsdWU6IDcwLCBjYXA6IDEsIGR1cGxpY2F0ZVJ1bGU6ICdvbmNlLXBlci1ydW4nIH0sIHJpc2tQcm9maWxlOiB7IGtpbmQ6ICdhbWJ1c2gnLCBsYWJlbDogJ25lc3Qgd2F0Y2gnLCBkZXRhaWw6ICdBIGhpZGRlbiB3YXRjaGVyIG1heSBjb250ZXN0IHRoZSBjYWNoZS4nIH0gfVxuICBdLFxuICAnY2F2ZXJuLWhpZGRlbi1jaGFtYmVyJzogW1xuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAnbG9yZS1yZWxpYycsIGxhYmVsOiAndGlkZSByZWxpcXVhcnknLCB2YWx1ZTogMTA1LCBjYXA6IDEsIGR1cGxpY2F0ZVJ1bGU6ICdvbmNlLXBlci1ydW4nIH0sIHJpc2tQcm9maWxlOiB7IGtpbmQ6ICd0ZXJyYWluLWhhemFyZCcsIGxhYmVsOiAndW5kZXJ0b3cnLCBkZXRhaWw6ICdDdXJyZW50IHByZXNzdXJlIG1ha2VzIHRoZSBjaGFtYmVyIGNvc3RseS4nIH0gfSxcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ2hpZ2gtdmFsdWUtcmVzb3VyY2UnLCBsYWJlbDogJ3NlYWxlZCBzdG9yZXMnLCB2YWx1ZTogODUsIGNhcDogMSwgZHVwbGljYXRlUnVsZTogJ29uY2UtcGVyLXJ1bicgfSwgcmlza1Byb2ZpbGU6IHsga2luZDogJ3JvdXRlLWlzb2xhdGlvbicsIGxhYmVsOiAnZmxvb2RlZCByZXR1cm4nLCBkZXRhaWw6ICdUaGUgc2lkZSByb3V0ZSBjYW4gaXNvbGF0ZSBhIGNhcmVsZXNzIGNvdXJpZXIuJyB9IH1cbiAgXSxcbiAgJ3JpdHVhbC1oaWRkZW4tY2hhbWJlcic6IFtcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ2xvcmUtcmVsaWMnLCBsYWJlbDogJ2dseXBoIGFyY2hpdmUnLCB2YWx1ZTogMTEwLCBjYXA6IDEsIGR1cGxpY2F0ZVJ1bGU6ICdvbmNlLXBlci1ydW4nIH0sIHJpc2tQcm9maWxlOiB7IGtpbmQ6ICd0b29sLWNvb2xkb3duJywgbGFiZWw6ICd3YXJkIGRyYWcnLCBkZXRhaWw6ICdUaGUgd2FyZCB0YXhlcyB0aGUgbmV4dCB0cmF2ZXJzYWwgdG9vbC4nIH0gfSxcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ2NvbXBhbmlvbi1sZWFkJywgbGFiZWw6ICdyaXR1YWxpc3QgbGVhZCcsIHZhbHVlOiA3NSwgY2FwOiAxLCBkdXBsaWNhdGVSdWxlOiAnb25jZS1wZXItcnVuJyB9LCByaXNrUHJvZmlsZTogeyBraW5kOiAnYW1idXNoJywgbGFiZWw6ICdndWFyZGlhbiBlY2hvJywgZGV0YWlsOiAnVGhlIGdseXBocyBjYW4gY2FsbCBhIGRlZmVuZGVyLicgfSB9XG4gIF0sXG4gICdmdXJuYWNlLXNlcnZpY2Utc3BhY2UnOiBbXG4gICAgeyByZXdhcmRQcm9maWxlOiB7IGtpbmQ6ICdoaWdoLXZhbHVlLXJlc291cmNlJywgbGFiZWw6ICdzZXJ2aWNlIHN0b3JlcycsIHZhbHVlOiA4NSwgY2FwOiAxLCBkdXBsaWNhdGVSdWxlOiAnb25jZS1wZXItcnVuJyB9LCByaXNrUHJvZmlsZTogeyBraW5kOiAndGVycmFpbi1oYXphcmQnLCBsYWJlbDogJ3Ntb2tlIGxhbmUnLCBkZXRhaWw6ICdIZWF0IGFuZCBzbW9rZSBndWFyZCB0aGUgc2VydmljZSByb3V0ZS4nIH0gfSxcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ2tpdC1jaG9pY2UnLCBsYWJlbDogJ2tpbG4ga2l0JywgdmFsdWU6IDgwLCBjYXA6IDEsIGR1cGxpY2F0ZVJ1bGU6ICdvbmNlLXBlci1ydW4nIH0sIHJpc2tQcm9maWxlOiB7IGtpbmQ6ICd0b29sLWNvb2xkb3duJywgbGFiZWw6ICdoZWF0IHdhcnAnLCBkZXRhaWw6ICdUaGUgcm91dGUgZGVsYXlzIHRoZSBuZXh0IHRvb2wgdXNlLicgfSB9XG4gIF0sXG4gICdjbGlmZi1hbGNvdmUnOiBbXG4gICAgeyByZXdhcmRQcm9maWxlOiB7IGtpbmQ6ICdraXQtY2hvaWNlJywgbGFiZWw6ICdjbGltYmVyIGtpdCcsIHZhbHVlOiA4MCwgY2FwOiAxLCBkdXBsaWNhdGVSdWxlOiAnb25jZS1wZXItcnVuJyB9LCByaXNrUHJvZmlsZTogeyBraW5kOiAncm91dGUtaXNvbGF0aW9uJywgbGFiZWw6ICdleHBvc2VkIHJldHVybicsIGRldGFpbDogJ1dpbmQgY2FuIGN1dCB0aGUgYWxjb3ZlIG9mZiBmcm9tIHRoZSBtYWluIHJvdXRlLicgfSB9LFxuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAnc2hvcnRjdXQtYWNjZXNzJywgbGFiZWw6ICdyaWRnZSBieXBhc3MnLCB2YWx1ZTogMTAwLCBjYXA6IDEsIGR1cGxpY2F0ZVJ1bGU6ICdvbmNlLXBlci1ydW4nIH0sIHJpc2tQcm9maWxlOiB7IGtpbmQ6ICdyZXNvdXJjZS1vcHBvcnR1bml0eS1jb3N0JywgbGFiZWw6ICdyb3BlIGNvbW1pdG1lbnQnLCBkZXRhaWw6ICdUaGUgY2xpbWIgY29tbWl0cyBzY2FyY2Ugcm9wZSBhbmQgdGltZS4nIH0gfVxuICBdLFxuICAnYnVyaWFsLWNyeXB0JzogW1xuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAnY29tcGFuaW9uLWxlYWQnLCBsYWJlbDogJ2FuY2VzdG9yIGxlYWQnLCB2YWx1ZTogOTAsIGNhcDogMSwgZHVwbGljYXRlUnVsZTogJ29uY2UtcGVyLXJ1bicgfSwgcmlza1Byb2ZpbGU6IHsga2luZDogJ2FtYnVzaCcsIGxhYmVsOiAncmVzdGxlc3MgZGVhZCcsIGRldGFpbDogJ1RoZSBjcnlwdCBjYW4gYW5zd2VyIHdpdGggYW4gYW1idXNoLicgfSB9LFxuICAgIHsgcmV3YXJkUHJvZmlsZTogeyBraW5kOiAnbG9yZS1yZWxpYycsIGxhYmVsOiAnb3NzdWFyeSByZWxpYycsIHZhbHVlOiAxMDUsIGNhcDogMSwgZHVwbGljYXRlUnVsZTogJ29uY2UtcGVyLXJ1bicgfSwgcmlza1Byb2ZpbGU6IHsga2luZDogJ3JvdXRlLWlzb2xhdGlvbicsIGxhYmVsOiAnc2VhbGVkIHByb2Nlc3Npb24nLCBkZXRhaWw6ICdUaGUgYnVyaWFsIHJvdXRlIG5hcnJvd3MgdGhlIHJldHVybi4nIH0gfVxuICBdLFxuICAnZnJvc3QtY2F2ZSc6IFtcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ3Nob3J0Y3V0LWFjY2VzcycsIGxhYmVsOiAnaWNlIHNoZWxmIGJ5cGFzcycsIHZhbHVlOiAxMDAsIGNhcDogMSwgZHVwbGljYXRlUnVsZTogJ29uY2UtcGVyLXJ1bicgfSwgcmlza1Byb2ZpbGU6IHsga2luZDogJ3Rvb2wtY29vbGRvd24nLCBsYWJlbDogJ2Zyb3plbiBnZWFyJywgZGV0YWlsOiAnQ29sZCBkZWxheXMgdGhlIG5leHQgdG9vbCBjeWNsZS4nIH0gfSxcbiAgICB7IHJld2FyZFByb2ZpbGU6IHsga2luZDogJ2xvcmUtcmVsaWMnLCBsYWJlbDogJ3JpbWUgcmVsaXF1YXJ5JywgdmFsdWU6IDExMCwgY2FwOiAxLCBkdXBsaWNhdGVSdWxlOiAnb25jZS1wZXItcnVuJyB9LCByaXNrUHJvZmlsZTogeyBraW5kOiAndGVycmFpbi1oYXphcmQnLCBsYWJlbDogJ2Zyb3N0IHJpbWUnLCBkZXRhaWw6ICdUaGUgaG9sbG93IGxlYXZlcyBmcmVlemluZyBncm91bmQuJyB9IH1cbiAgXVxufVxuXG5jb25zdCBzb3VyY2VLaW5kRm9yID0gKHNvdXJjZUlkOiBzdHJpbmcpOiBTZWNyZXRTb3VyY2VLaW5kID0+IHNvdXJjZUlkLnN0YXJ0c1dpdGgoJ21pbmUtYnJlYWNoOicpID8gJ21pbmUtYnJlYWNoLXJvb20nXG4gIDogc291cmNlSWQuc3RhcnRzV2l0aCgnd2lsZHMtY2F2ZTonKSA/ICd3aWxkcy1jYXZlJ1xuICAgIDogc291cmNlSWQuc3RhcnRzV2l0aCgnY2F2ZXJuLWhpZGRlbjonKSA/ICdjYXZlcm4taGlkZGVuLWNoYW1iZXInXG4gICAgICA6IHNvdXJjZUlkLnN0YXJ0c1dpdGgoJ3JpdHVhbC1oaWRkZW46JykgPyAncml0dWFsLWhpZGRlbi1jaGFtYmVyJ1xuICAgICAgICA6IHNvdXJjZUlkLnN0YXJ0c1dpdGgoJ2Z1cm5hY2Utc2VydmljZTonKSA/ICdmdXJuYWNlLXNlcnZpY2Utc3BhY2UnXG4gICAgICAgICAgOiBzb3VyY2VJZC5zdGFydHNXaXRoKCdjbGlmZi1hbGNvdmU6JykgPyAnY2xpZmYtYWxjb3ZlJ1xuICAgICAgICAgICAgOiBzb3VyY2VJZC5zdGFydHNXaXRoKCdidXJpYWwtY3J5cHQ6JykgPyAnYnVyaWFsLWNyeXB0J1xuICAgICAgICAgICAgICA6ICdmcm9zdC1jYXZlJ1xuY29uc3Qgc291cmNlSGFzaCA9IChzb3VyY2VJZDogc3RyaW5nKTogbnVtYmVyID0+IFsuLi5zb3VyY2VJZF0ucmVkdWNlKCh2YWx1ZSwgY2hhcikgPT4gKHZhbHVlICogMzEgKyBjaGFyLmNoYXJDb2RlQXQoMCkpID4+PiAwLCAwKVxuZXhwb3J0IGNvbnN0IHNlY3JldFJ1bGVzRm9yU291cmNlSWQgPSAoc291cmNlSWQ6IHN0cmluZyk6IFNlY3JldFJ1bGVzID0+IHtcbiAgY29uc3QgY2hvaWNlcyA9IHJ1bGVzW3NvdXJjZUtpbmRGb3Ioc291cmNlSWQpXVxuICBjb25zdCBzZWxlY3RlZCA9IGNob2ljZXNbc291cmNlSGFzaChzb3VyY2VJZCkgJSBjaG9pY2VzLmxlbmd0aF0hXG4gIHJldHVybiB7IHJld2FyZFByb2ZpbGU6IHsgLi4uc2VsZWN0ZWQucmV3YXJkUHJvZmlsZSB9LCByaXNrUHJvZmlsZTogeyAuLi5zZWxlY3RlZC5yaXNrUHJvZmlsZSB9IH1cbn1cblxuY29uc3QgcHJvZmlsZUZvciA9IChzcGFjZTogU2lkZVNwYWNlKTogeyBraW5kOiBTZWNyZXRSb29tS2luZDsgZW50cnlDb25kaXRpb246IFNlY3JldEVudHJ5Q29uZGl0aW9uOyBkaXNjb3ZlcnlDbHVlOiBzdHJpbmc7IGNsdWVDaGFubmVsOiBTZWNyZXRDbHVlQ2hhbm5lbDsgYWNjZXNzTWV0aG9kOiBTZWNyZXRBY2Nlc3NNZXRob2Q7IHJld2FyZENsYXNzOiBTZWNyZXRSZXdhcmRDbGFzczsgcmlzazogU2VjcmV0UmlzayB9ID0+IHNwYWNlLmtpbmQgPT09ICdtaW5lLWJyZWFjaC1yb29tJ1xuICA/IHsga2luZDogJ2hpZGRlbi1yb29tJywgZW50cnlDb25kaXRpb246ICdzZWFsZWQtYnJlYWt3YWxsJywgZGlzY292ZXJ5Q2x1ZTogJ2ZyYWN0dXJlZCByYWlsIHN0b25lJywgY2x1ZUNoYW5uZWw6ICd0ZXJyYWluJywgYWNjZXNzTWV0aG9kOiAnYnJlYWNoJywgcmV3YXJkQ2xhc3M6ICdzdXBwbGllcycsIHJpc2s6ICdkdXN0JyB9XG4gIDogc3BhY2Uua2luZCA9PT0gJ3dpbGRzLWNhdmUnXG4gICAgPyB7IGtpbmQ6ICdoaWRkZW4tcm9vbScsIGVudHJ5Q29uZGl0aW9uOiAnc2VhbGVkLWJyZWFrd2FsbCcsIGRpc2NvdmVyeUNsdWU6ICdyb290LWNob2tlZCBob2xsb3cnLCBjbHVlQ2hhbm5lbDogJ3NpZ2h0JywgYWNjZXNzTWV0aG9kOiAnYnJlYWNoJywgcmV3YXJkQ2xhc3M6ICdzdXBwbGllcycsIHJpc2s6ICdkdXN0JyB9XG4gICAgOiBzcGFjZS5raW5kID09PSAnY2F2ZXJuLWhpZGRlbi1jaGFtYmVyJ1xuICAgICAgPyB7IGtpbmQ6ICdoaWRkZW4tcm9vbScsIGVudHJ5Q29uZGl0aW9uOiAnc2VhbGVkLWJyZWFrd2FsbCcsIGRpc2NvdmVyeUNsdWU6ICdjdXJyZW50LWZlZCBmaXNzdXJlJywgY2x1ZUNoYW5uZWw6ICdzb3VuZCcsIGFjY2Vzc01ldGhvZDogJ2JyZWFjaCcsIHJld2FyZENsYXNzOiAncml0dWFsJywgcmlzazogJ3VuZGVydG93JyB9XG4gICAgICA6IHNwYWNlLmtpbmQgPT09ICdyaXR1YWwtaGlkZGVuLWNoYW1iZXInXG4gICAgICAgID8geyBraW5kOiAnaGlkZGVuLXJvb20nLCBlbnRyeUNvbmRpdGlvbjogJ3NlYWxlZC1icmVha3dhbGwnLCBkaXNjb3ZlcnlDbHVlOiAnYnJva2VuIGdseXBoIHNlYW0nLCBjbHVlQ2hhbm5lbDogJ3JpdHVhbCcsIGFjY2Vzc01ldGhvZDogJ2JyZWFjaCcsIHJld2FyZENsYXNzOiAncml0dWFsJywgcmlzazogJ3dhcmQnIH1cbiAgICAgICAgOiBzcGFjZS5raW5kID09PSAnZnVybmFjZS1zZXJ2aWNlLXNwYWNlJ1xuICAgICAgICAgID8geyBraW5kOiAnaGlkZGVuLXJvb20nLCBlbnRyeUNvbmRpdGlvbjogJ3NlYWxlZC1icmVha3dhbGwnLCBkaXNjb3ZlcnlDbHVlOiAnd2FybSBzZXJ2aWNlIHZlbnQnLCBjbHVlQ2hhbm5lbDogJ3NvdW5kJywgYWNjZXNzTWV0aG9kOiAnYnJlYWNoJywgcmV3YXJkQ2xhc3M6ICdzdXBwbGllcycsIHJpc2s6ICdzbW9rZScgfVxuICAgICAgICAgIDogc3BhY2Uua2luZCA9PT0gJ2NsaWZmLWFsY292ZSdcbiAgICAgICAgICAgID8geyBraW5kOiAnc2lkZS1wb2NrZXQnLCBlbnRyeUNvbmRpdGlvbjogJ2FuY2hvcmVkLXJvcGUnLCBkaXNjb3ZlcnlDbHVlOiAnd2VhdGhlcmVkIHJvcGUgYW5jaG9yJywgY2x1ZUNoYW5uZWw6ICdzaWdodCcsIGFjY2Vzc01ldGhvZDogJ2NsaW1iJywgcmV3YXJkQ2xhc3M6ICdzdXBwbGllcycsIHJpc2s6ICdmYWxsJyB9XG4gICAgICAgICAgICA6IHNwYWNlLmtpbmQgPT09ICdidXJpYWwtY3J5cHQnXG4gICAgICAgICAgICAgID8geyBraW5kOiAnc2lkZS1wb2NrZXQnLCBlbnRyeUNvbmRpdGlvbjogJ3NlYWxlZC1icmVha3dhbGwnLCBkaXNjb3ZlcnlDbHVlOiAnc2V0dGxlZCBjYWlybiBzZWFtJywgY2x1ZUNoYW5uZWw6ICdwcm9wJywgYWNjZXNzTWV0aG9kOiAnYnJlYWNoJywgcmV3YXJkQ2xhc3M6ICdyaXR1YWwnLCByaXNrOiAnc3Bpcml0cycgfVxuICAgICAgICAgICAgICA6IHsga2luZDogJ2hpZGRlbi1yb29tJywgZW50cnlDb25kaXRpb246ICdzZWFsZWQtYnJlYWt3YWxsJywgZGlzY292ZXJ5Q2x1ZTogJ3JpbWUtY292ZXJlZCBob2xsb3cnLCBjbHVlQ2hhbm5lbDogJ3RlcnJhaW4nLCBhY2Nlc3NNZXRob2Q6ICdicmVhY2gnLCByZXdhcmRDbGFzczogJ3JpdHVhbCcsIHJpc2s6ICdjb2xkJyB9XG5cbmNvbnN0IGVudHJpZXNGb3IgPSAoc3BhY2U6IFNpZGVTcGFjZSkgPT4gc3BhY2Uua2luZCA9PT0gJ2J1cmlhbC1jcnlwdCcgPyBzcGFjZS5lbnRyaWVzIDogW3NwYWNlLmVudHJ5XVxuXG5leHBvcnQgY29uc3QgcGxhY2VTZWNyZXRNZXRhZGF0YSA9IChmbG9vcjogRmxvb3IpOiB2b2lkID0+IHtcbiAgY29uc3Qgcm9vbXM6IFNlY3JldFJvb21bXSA9IChmbG9vci5zaWRlU3BhY2VzID8/IFtdKS5tYXAoc3BhY2UgPT4ge1xuICAgIGNvbnN0IHByb2ZpbGUgPSBwcm9maWxlRm9yKHNwYWNlKVxuICAgIGNvbnN0IHJ1bGUgPSBzZWNyZXRSdWxlc0ZvclNvdXJjZUlkKHNwYWNlLmlkKVxuICAgIGNvbnN0IGlkID0gYHNlY3JldC1yb29tOiR7c3BhY2UuaWR9YFxuICAgIHNwYWNlLnJld2FyZC5zZWNyZXRJZCA9IGlkXG4gICAgcmV0dXJuIHsgdmVyc2lvbjogMSwgaWQsIHNvdXJjZUlkOiBzcGFjZS5pZCwgLi4ucHJvZmlsZSwgLi4ucnVsZSwgYXBwcm9hY2g6IHsgLi4uc3BhY2UuYXBwcm9hY2ggfSwgZW50cmllczogZW50cmllc0ZvcihzcGFjZSkubWFwKHBvaW50ID0+ICh7IC4uLnBvaW50IH0pKSwgY2hhbWJlcjogc3BhY2UuY2hhbWJlci5tYXAocG9pbnQgPT4gKHsgLi4ucG9pbnQgfSkpLCBzYWZlRmFsbGJhY2s6IHRydWUgfVxuICB9KVxuICBjb25zdCByb29tRm9yID0gbmV3IE1hcChyb29tcy5tYXAocm9vbSA9PiBbcm9vbS5zb3VyY2VJZCwgcm9vbV0pKVxuICBjb25zdCByb3V0ZXM6IFNlY3JldFJvdXRlW10gPSAoZmxvb3Iuc2lkZVNwYWNlcyA/PyBbXSkuZmxhdE1hcChzcGFjZSA9PiB7XG4gICAgY29uc3Qgcm9vbSA9IHJvb21Gb3IuZ2V0KHNwYWNlLmlkKSFcbiAgICBjb25zdCBhY2Nlc3MgPSByb29tLmVudHJpZXMubWFwKChlbnRyeSwgaW5kZXgpID0+ICh7IHZlcnNpb246IDEgYXMgY29uc3QsIGlkOiBgc2VjcmV0LXJvdXRlOiR7cm9vbS5pZH06YWNjZXNzOiR7aW5kZXh9YCwgcm9vbUlkOiByb29tLmlkLCBraW5kOiAnY29uY2VhbGVkLXBhc3NhZ2UnIGFzIGNvbnN0LCBmcm9tOiB7IC4uLnJvb20uYXBwcm9hY2ggfSwgZW50cnk6IHsgLi4uZW50cnkgfSwgZW50cnlDb25kaXRpb246IHJvb20uZW50cnlDb25kaXRpb24sIGRpc2NvdmVyeUNsdWU6IHJvb20uZGlzY292ZXJ5Q2x1ZSwgYWNjZXNzTWV0aG9kOiByb29tLmFjY2Vzc01ldGhvZCwgcmV3YXJkQ2xhc3M6IHJvb20ucmV3YXJkQ2xhc3MsIHJpc2s6IHJvb20ucmlzaywgc2FmZUZhbGxiYWNrOiB0cnVlIGFzIGNvbnN0IH0pKVxuICAgIGlmIChzcGFjZS5raW5kICE9PSAnbWluZS1icmVhY2gtcm9vbScgfHwgIXNwYWNlLnJhcmVUcmFuc2l0aW9uKSByZXR1cm4gYWNjZXNzXG4gICAgcmV0dXJuIFsuLi5hY2Nlc3MsIHsgdmVyc2lvbjogMSBhcyBjb25zdCwgaWQ6IGBzZWNyZXQtcm91dGU6JHtyb29tLmlkfTp0cmFuc2l0aW9uYCwgcm9vbUlkOiByb29tLmlkLCBraW5kOiAncmFyZS10cmFuc2l0aW9uJyBhcyBjb25zdCwgZnJvbTogeyAuLi5yb29tLmFwcHJvYWNoIH0sIGVudHJ5OiB7IC4uLnNwYWNlLmVudHJ5IH0sIGVudHJ5Q29uZGl0aW9uOiByb29tLmVudHJ5Q29uZGl0aW9uLCBkaXNjb3ZlcnlDbHVlOiByb29tLmRpc2NvdmVyeUNsdWUsIGFjY2Vzc01ldGhvZDogcm9vbS5hY2Nlc3NNZXRob2QsIHJld2FyZENsYXNzOiAnc2hvcnRjdXQnIGFzIGNvbnN0LCByaXNrOiByb29tLnJpc2ssIHNhZmVGYWxsYmFjazogdHJ1ZSBhcyBjb25zdCwgZGVzdGluYXRpb246IHsgYmlvbWU6IHNwYWNlLnJhcmVUcmFuc2l0aW9uLnRhcmdldEJpb21lLCBmbG9vcjogc3BhY2UucmFyZVRyYW5zaXRpb24udGFyZ2V0Rmxvb3IgfSwgZGlyZWN0aW9uOiAnb25lLXdheScgYXMgY29uc3QsIGFycml2YWw6ICdmbG9vci1zdGFydCcgYXMgY29uc3QsIHJldHVyblNlbWFudGljczogJ25vLXJldHVybicgYXMgY29uc3QgfV1cbiAgfSlcbiAgZmxvb3Iuc2VjcmV0Um9vbXMgPSByb29tc1xuICBmbG9vci5zZWNyZXRSb3V0ZXMgPSByb3V0ZXNcbn1cblxuY29uc3QgZGlzdGFuY2UgPSAobGVmdDogeyB4OiBudW1iZXI7IHk6IG51bWJlciB9LCByaWdodDogeyB4OiBudW1iZXI7IHk6IG51bWJlciB9KTogbnVtYmVyID0+IE1hdGgubWF4KE1hdGguYWJzKGxlZnQueCAtIHJpZ2h0LngpLCBNYXRoLmFicyhsZWZ0LnkgLSByaWdodC55KSlcbmNvbnN0IHZpc2libGUgPSAoZmxvb3I6IEZsb29yLCBwb2ludDogeyB4OiBudW1iZXI7IHk6IG51bWJlciB9KTogYm9vbGVhbiA9PiBmbG9vci50aWxlc1tmbG9vckluZGV4KGZsb29yLCBwb2ludC54LCBwb2ludC55KV0/LnZpc2libGUgPT09IHRydWVcblxuZXhwb3J0IGNvbnN0IHNlY3JldENsdWVUcmlnZ2VyOiBSZWNvcmQ8U2VjcmV0Q2x1ZUNoYW5uZWwsIHN0cmluZz4gPSB7XG4gIHNpZ2h0OiAnc2VlIHRoZSBtYXJrZWQgZW50cnknLFxuICBzb3VuZDogJ2hlYXIgaXQgd2l0aGluIHR3byB0aWxlcycsXG4gIHByb3A6ICdpbnNwZWN0IGl0cyBtYXJrZXIgd2l0aCBDJyxcbiAgdGVycmFpbjogJ3N0YW5kIG9uIGl0cyB0ZWxsdGFsZSB0ZXJyYWluJyxcbiAgcml0dWFsOiAnd29yayBhbiBBc3RyYWwgY2hhcm0gbmVhcmJ5J1xufVxuXG5leHBvcnQgY29uc3Qgc2VjcmV0SW50ZXJhY3Rpb25IaW50ID0gKHJvb206IFNlY3JldFJvb20pOiBzdHJpbmcgPT4gcm9vbS5hY2Nlc3NNZXRob2QgPT09ICdicmVhY2gnID8gJ1VzZSBCIGJlc2lkZSB0aGUgc2VhbGVkIGVudHJ5IHRvIGJyZWFjaCBpdC4nIDogJ0ZvbGxvdyB0aGUgcm9wZS1tYXJrZWQgZW50cnkgdG8gY2xpbWIgaW4uJ1xuZXhwb3J0IGNvbnN0IGlzU2VjcmV0RGlzY292ZXJlZCA9IChyb29tOiBTZWNyZXRSb29tKTogYm9vbGVhbiA9PiByb29tLmRpc2NvdmVyeSAhPT0gdW5kZWZpbmVkXG5leHBvcnQgY29uc3Qgc2VjcmV0RGlzY292ZXJ5TWVzc2FnZSA9IChyb29tOiBTZWNyZXRSb29tKTogc3RyaW5nID0+IHtcbiAgY29uc3QgY2hhbm5lbCA9IHJvb20uZGlzY292ZXJ5Py5jaGFubmVsID8/IHJvb20uY2x1ZUNoYW5uZWxcbiAgcmV0dXJuIGBTZWNyZXQgZm91bmQgYnkgJHtjaGFubmVsfSAoJHtzZWNyZXRDbHVlVHJpZ2dlcltjaGFubmVsXX0pOiAke3Jvb20uZGlzY292ZXJ5Q2x1ZX0uICR7c2VjcmV0SW50ZXJhY3Rpb25IaW50KHJvb20pfWBcbn1cblxuY29uc3QgY2x1ZUlzQXZhaWxhYmxlID0gKHN0YXRlOiBSdW5TdGF0ZSwgcm9vbTogU2VjcmV0Um9vbSwgY2hhbm5lbDogU2VjcmV0Q2x1ZUNoYW5uZWwpOiBib29sZWFuID0+IGNoYW5uZWwgPT09ICdzaWdodCdcbiAgPyByb29tLmVudHJpZXMuc29tZShwb2ludCA9PiB2aXNpYmxlKHN0YXRlLmZsb29yLCBwb2ludCkpXG4gIDogY2hhbm5lbCA9PT0gJ3NvdW5kJ1xuICAgID8gZGlzdGFuY2Uoc3RhdGUuaGVybywgcm9vbS5hcHByb2FjaCkgPD0gMlxuICAgIDogY2hhbm5lbCA9PT0gJ3Byb3AnIHx8IGNoYW5uZWwgPT09ICd0ZXJyYWluJ1xuICAgICAgPyBkaXN0YW5jZShzdGF0ZS5oZXJvLCByb29tLmFwcHJvYWNoKSA9PT0gMFxuICAgICAgOiBkaXN0YW5jZShzdGF0ZS5oZXJvLCByb29tLmFwcHJvYWNoKSA8PSA0XG5cbmV4cG9ydCBjb25zdCBkaXNjb3ZlclNlY3JldENsdWVzID0gKHN0YXRlOiBSdW5TdGF0ZSwgY2hhbm5lbDogU2VjcmV0Q2x1ZUNoYW5uZWwpOiBTZWNyZXRSb29tW10gPT4gKHN0YXRlLmZsb29yLnNlY3JldFJvb21zID8/IFtdKS5maWx0ZXIocm9vbSA9PiB7XG4gIGlmIChyb29tLmRpc2NvdmVyeSB8fCByb29tLmNsdWVDaGFubmVsICE9PSBjaGFubmVsIHx8ICFjbHVlSXNBdmFpbGFibGUoc3RhdGUsIHJvb20sIGNoYW5uZWwpKSByZXR1cm4gZmFsc2VcbiAgcm9vbS5kaXNjb3ZlcnkgPSB7IGNoYW5uZWwsIHR1cm46IHN0YXRlLnR1cm4gfVxuICByZXR1cm4gdHJ1ZVxufSlcblxuZXhwb3J0IHR5cGUgU2VjcmV0UmV3YXJkQ2xhaW0gPSB7IHJvb206IFNlY3JldFJvb207IHJlc29sdXRpb246IFNlY3JldFJlc29sdXRpb24gfSB8ICdhbHJlYWR5LXJlc29sdmVkJyB8IHVuZGVmaW5lZFxuZXhwb3J0IGNvbnN0IGNsYWltU2VjcmV0UmV3YXJkID0gKHN0YXRlOiBSdW5TdGF0ZSwgc2VjcmV0SWQ6IHN0cmluZyk6IFNlY3JldFJld2FyZENsYWltID0+IHtcbiAgY29uc3Qgcm9vbSA9IHN0YXRlLmZsb29yLnNlY3JldFJvb21zPy5maW5kKGNhbmRpZGF0ZSA9PiBjYW5kaWRhdGUuaWQgPT09IHNlY3JldElkKVxuICBpZiAoIXJvb20pIHJldHVybiB1bmRlZmluZWRcbiAgaWYgKHJvb20ucmVzb2x1dGlvbikgcmV0dXJuICdhbHJlYWR5LXJlc29sdmVkJ1xuICBjb25zdCByZXNvbHV0aW9uOiBTZWNyZXRSZXNvbHV0aW9uID0geyB0dXJuOiBzdGF0ZS50dXJuLCByZXdhcmRLaW5kOiByb29tLnJld2FyZFByb2ZpbGUua2luZCwgcmV3YXJkVmFsdWU6IHJvb20ucmV3YXJkUHJvZmlsZS52YWx1ZSwgcmlza0tpbmQ6IHJvb20ucmlza1Byb2ZpbGUua2luZCB9XG4gIHJvb20ucmVzb2x1dGlvbiA9IHJlc29sdXRpb25cbiAgcmV0dXJuIHsgcm9vbSwgcmVzb2x1dGlvbiB9XG59XG5leHBvcnQgY29uc3Qgc2VjcmV0UmVzb2x1dGlvbk1lc3NhZ2UgPSAocm9vbTogU2VjcmV0Um9vbSk6IHN0cmluZyA9PiBgU2VjcmV0IHJlc29sdmVkIOKAlCAke3Jvb20ucmV3YXJkUHJvZmlsZS5sYWJlbH0gKCske3Jvb20ucmV3YXJkUHJvZmlsZS52YWx1ZX0gZXhwbG9yYXRpb24pLiBSaXNrOiAke3Jvb20ucmlza1Byb2ZpbGUubGFiZWx9OyAke3Jvb20ucmlza1Byb2ZpbGUuZGV0YWlsfWBcbmV4cG9ydCBjb25zdCBzZWNyZXRTaG9ydGN1dFJlcG9ydCA9IChyb3V0ZTogU2VjcmV0Um91dGUpOiBzdHJpbmcgPT4gcm91dGUuZGVzdGluYXRpb24gPyBgJHtyb3V0ZS5kaXJlY3Rpb24gPT09ICd0d28td2F5JyA/ICd0d28td2F5JyA6ICdvbmUtd2F5J30gJHtyb3V0ZS5kZXN0aW5hdGlvbi5iaW9tZX0gc2hvcnRjdXQgdG8gZmxvb3IgJHtyb3V0ZS5kZXN0aW5hdGlvbi5mbG9vciArIDF9LCBhcnJpdmFsIGF0IGZsb29yIHN0YXJ0OyAke3JvdXRlLnJldHVyblNlbWFudGljcyA9PT0gJ3JldHVybi1saW5rJyA/ICdyZXR1cm4gbGluayBhdmFpbGFibGUnIDogJ25vIHJldHVybid9YCA6ICdzaG9ydGN1dCBkZXN0aW5hdGlvbiB1bmF2YWlsYWJsZSdcbiJdLCJtYXBwaW5ncyI6IkFBQUEsU0FBU0EsVUFBVSxRQUF5UyxTQUFTO0FBS3JVLE1BQU1DLEtBQXVELEdBQUc7RUFDOUQsa0JBQWtCLEVBQUUsQ0FDbEI7SUFBRUMsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxxQkFBcUI7TUFBRUMsS0FBSyxFQUFFLFlBQVk7TUFBRUMsS0FBSyxFQUFFLEVBQUU7TUFBRUMsR0FBRyxFQUFFLENBQUM7TUFBRUMsYUFBYSxFQUFFO0lBQWUsQ0FBQztJQUFFQyxXQUFXLEVBQUU7TUFBRUwsSUFBSSxFQUFFLDJCQUEyQjtNQUFFQyxLQUFLLEVBQUUsYUFBYTtNQUFFSyxNQUFNLEVBQUU7SUFBK0M7RUFBRSxDQUFDLEVBQzNQO0lBQUVQLGFBQWEsRUFBRTtNQUFFQyxJQUFJLEVBQUUsWUFBWTtNQUFFQyxLQUFLLEVBQUUsYUFBYTtNQUFFQyxLQUFLLEVBQUUsRUFBRTtNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsZ0JBQWdCO01BQUVDLEtBQUssRUFBRSxnQkFBZ0I7TUFBRUssTUFBTSxFQUFFO0lBQXdDO0VBQUUsQ0FBQyxDQUNyTztFQUNELFlBQVksRUFBRSxDQUNaO0lBQUVQLGFBQWEsRUFBRTtNQUFFQyxJQUFJLEVBQUUsWUFBWTtNQUFFQyxLQUFLLEVBQUUsYUFBYTtNQUFFQyxLQUFLLEVBQUUsRUFBRTtNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsZ0JBQWdCO01BQUVDLEtBQUssRUFBRSxZQUFZO01BQUVLLE1BQU0sRUFBRTtJQUF1QztFQUFFLENBQUMsRUFDL047SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxnQkFBZ0I7TUFBRUMsS0FBSyxFQUFFLGdCQUFnQjtNQUFFQyxLQUFLLEVBQUUsRUFBRTtNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsUUFBUTtNQUFFQyxLQUFLLEVBQUUsWUFBWTtNQUFFSyxNQUFNLEVBQUU7SUFBMEM7RUFBRSxDQUFDLENBQ2xPO0VBQ0QsdUJBQXVCLEVBQUUsQ0FDdkI7SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxZQUFZO01BQUVDLEtBQUssRUFBRSxnQkFBZ0I7TUFBRUMsS0FBSyxFQUFFLEdBQUc7TUFBRUMsR0FBRyxFQUFFLENBQUM7TUFBRUMsYUFBYSxFQUFFO0lBQWUsQ0FBQztJQUFFQyxXQUFXLEVBQUU7TUFBRUwsSUFBSSxFQUFFLGdCQUFnQjtNQUFFQyxLQUFLLEVBQUUsVUFBVTtNQUFFSyxNQUFNLEVBQUU7SUFBNkM7RUFBRSxDQUFDLEVBQ3ZPO0lBQUVQLGFBQWEsRUFBRTtNQUFFQyxJQUFJLEVBQUUscUJBQXFCO01BQUVDLEtBQUssRUFBRSxlQUFlO01BQUVDLEtBQUssRUFBRSxFQUFFO01BQUVDLEdBQUcsRUFBRSxDQUFDO01BQUVDLGFBQWEsRUFBRTtJQUFlLENBQUM7SUFBRUMsV0FBVyxFQUFFO01BQUVMLElBQUksRUFBRSxpQkFBaUI7TUFBRUMsS0FBSyxFQUFFLGdCQUFnQjtNQUFFSyxNQUFNLEVBQUU7SUFBaUQ7RUFBRSxDQUFDLENBQzFQO0VBQ0QsdUJBQXVCLEVBQUUsQ0FDdkI7SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxZQUFZO01BQUVDLEtBQUssRUFBRSxlQUFlO01BQUVDLEtBQUssRUFBRSxHQUFHO01BQUVDLEdBQUcsRUFBRSxDQUFDO01BQUVDLGFBQWEsRUFBRTtJQUFlLENBQUM7SUFBRUMsV0FBVyxFQUFFO01BQUVMLElBQUksRUFBRSxlQUFlO01BQUVDLEtBQUssRUFBRSxXQUFXO01BQUVLLE1BQU0sRUFBRTtJQUEwQztFQUFFLENBQUMsRUFDbk87SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxnQkFBZ0I7TUFBRUMsS0FBSyxFQUFFLGdCQUFnQjtNQUFFQyxLQUFLLEVBQUUsRUFBRTtNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsUUFBUTtNQUFFQyxLQUFLLEVBQUUsZUFBZTtNQUFFSyxNQUFNLEVBQUU7SUFBa0M7RUFBRSxDQUFDLENBQzdOO0VBQ0QsdUJBQXVCLEVBQUUsQ0FDdkI7SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxxQkFBcUI7TUFBRUMsS0FBSyxFQUFFLGdCQUFnQjtNQUFFQyxLQUFLLEVBQUUsRUFBRTtNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsZ0JBQWdCO01BQUVDLEtBQUssRUFBRSxZQUFZO01BQUVLLE1BQU0sRUFBRTtJQUEwQztFQUFFLENBQUMsRUFDOU87SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxZQUFZO01BQUVDLEtBQUssRUFBRSxVQUFVO01BQUVDLEtBQUssRUFBRSxFQUFFO01BQUVDLEdBQUcsRUFBRSxDQUFDO01BQUVDLGFBQWEsRUFBRTtJQUFlLENBQUM7SUFBRUMsV0FBVyxFQUFFO01BQUVMLElBQUksRUFBRSxlQUFlO01BQUVDLEtBQUssRUFBRSxXQUFXO01BQUVLLE1BQU0sRUFBRTtJQUFzQztFQUFFLENBQUMsQ0FDMU47RUFDRCxjQUFjLEVBQUUsQ0FDZDtJQUFFUCxhQUFhLEVBQUU7TUFBRUMsSUFBSSxFQUFFLFlBQVk7TUFBRUMsS0FBSyxFQUFFLGFBQWE7TUFBRUMsS0FBSyxFQUFFLEVBQUU7TUFBRUMsR0FBRyxFQUFFLENBQUM7TUFBRUMsYUFBYSxFQUFFO0lBQWUsQ0FBQztJQUFFQyxXQUFXLEVBQUU7TUFBRUwsSUFBSSxFQUFFLGlCQUFpQjtNQUFFQyxLQUFLLEVBQUUsZ0JBQWdCO01BQUVLLE1BQU0sRUFBRTtJQUFtRDtFQUFFLENBQUMsRUFDaFA7SUFBRVAsYUFBYSxFQUFFO01BQUVDLElBQUksRUFBRSxpQkFBaUI7TUFBRUMsS0FBSyxFQUFFLGNBQWM7TUFBRUMsS0FBSyxFQUFFLEdBQUc7TUFBRUMsR0FBRyxFQUFFLENBQUM7TUFBRUMsYUFBYSxFQUFFO0lBQWUsQ0FBQztJQUFFQyxXQUFXLEVBQUU7TUFBRUwsSUFBSSxFQUFFLDJCQUEyQjtNQUFFQyxLQUFLLEVBQUUsaUJBQWlCO01BQUVLLE1BQU0sRUFBRTtJQUEwQztFQUFFLENBQUMsQ0FDMVA7RUFDRCxjQUFjLEVBQUUsQ0FDZDtJQUFFUCxhQUFhLEVBQUU7TUFBRUMsSUFBSSxFQUFFLGdCQUFnQjtNQUFFQyxLQUFLLEVBQUUsZUFBZTtNQUFFQyxLQUFLLEVBQUUsRUFBRTtNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsUUFBUTtNQUFFQyxLQUFLLEVBQUUsZUFBZTtNQUFFSyxNQUFNLEVBQUU7SUFBdUM7RUFBRSxDQUFDLEVBQ2hPO0lBQUVQLGFBQWEsRUFBRTtNQUFFQyxJQUFJLEVBQUUsWUFBWTtNQUFFQyxLQUFLLEVBQUUsZUFBZTtNQUFFQyxLQUFLLEVBQUUsR0FBRztNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsaUJBQWlCO01BQUVDLEtBQUssRUFBRSxtQkFBbUI7TUFBRUssTUFBTSxFQUFFO0lBQXVDO0VBQUUsQ0FBQyxDQUMzTztFQUNELFlBQVksRUFBRSxDQUNaO0lBQUVQLGFBQWEsRUFBRTtNQUFFQyxJQUFJLEVBQUUsaUJBQWlCO01BQUVDLEtBQUssRUFBRSxrQkFBa0I7TUFBRUMsS0FBSyxFQUFFLEdBQUc7TUFBRUMsR0FBRyxFQUFFLENBQUM7TUFBRUMsYUFBYSxFQUFFO0lBQWUsQ0FBQztJQUFFQyxXQUFXLEVBQUU7TUFBRUwsSUFBSSxFQUFFLGVBQWU7TUFBRUMsS0FBSyxFQUFFLGFBQWE7TUFBRUssTUFBTSxFQUFFO0lBQW1DO0VBQUUsQ0FBQyxFQUN0TztJQUFFUCxhQUFhLEVBQUU7TUFBRUMsSUFBSSxFQUFFLFlBQVk7TUFBRUMsS0FBSyxFQUFFLGdCQUFnQjtNQUFFQyxLQUFLLEVBQUUsR0FBRztNQUFFQyxHQUFHLEVBQUUsQ0FBQztNQUFFQyxhQUFhLEVBQUU7SUFBZSxDQUFDO0lBQUVDLFdBQVcsRUFBRTtNQUFFTCxJQUFJLEVBQUUsZ0JBQWdCO01BQUVDLEtBQUssRUFBRSxZQUFZO01BQUVLLE1BQU0sRUFBRTtJQUFxQztFQUFFLENBQUM7QUFFck8sQ0FBQztBQUVELE1BQU1DLGFBQWEsR0FBSUMsUUFBZ0IsSUFBdUJBLFFBQVEsQ0FBQ0MsVUFBVSxDQUFDLGNBQWMsQ0FBQyxHQUFHLGtCQUFrQixHQUNsSEQsUUFBUSxDQUFDQyxVQUFVLENBQUMsYUFBYSxDQUFDLEdBQUcsWUFBWSxHQUMvQ0QsUUFBUSxDQUFDQyxVQUFVLENBQUMsZ0JBQWdCLENBQUMsR0FBRyx1QkFBdUIsR0FDN0RELFFBQVEsQ0FBQ0MsVUFBVSxDQUFDLGdCQUFnQixDQUFDLEdBQUcsdUJBQXVCLEdBQzdERCxRQUFRLENBQUNDLFVBQVUsQ0FBQyxrQkFBa0IsQ0FBQyxHQUFHLHVCQUF1QixHQUMvREQsUUFBUSxDQUFDQyxVQUFVLENBQUMsZUFBZSxDQUFDLEdBQUcsY0FBYyxHQUNuREQsUUFBUSxDQUFDQyxVQUFVLENBQUMsZUFBZSxDQUFDLEdBQUcsY0FBYyxHQUNuRCxZQUFZO0FBQzVCLE1BQU1DLFVBQVUsR0FBSUYsUUFBZ0IsSUFBYSxDQUFDLEdBQUdBLFFBQVEsQ0FBQyxDQUFDRyxNQUFNLENBQUMsQ0FBQ1QsS0FBSyxFQUFFVSxJQUFJLEtBQU1WLEtBQUssR0FBRyxFQUFFLEdBQUdVLElBQUksQ0FBQ0MsVUFBVSxDQUFDLENBQUMsQ0FBQyxLQUFNLENBQUMsRUFBRSxDQUFDLENBQUM7QUFDbEksT0FBTyxNQUFNQyxzQkFBc0IsR0FBSU4sUUFBZ0IsSUFBa0I7RUFDdkUsTUFBTU8sT0FBTyxHQUFHakIsS0FBSyxDQUFDUyxhQUFhLENBQUNDLFFBQVEsQ0FBQyxDQUFDO0VBQzlDLE1BQU1RLFFBQVEsR0FBR0QsT0FBTyxDQUFDTCxVQUFVLENBQUNGLFFBQVEsQ0FBQyxHQUFHTyxPQUFPLENBQUNFLE1BQU0sQ0FBRTtFQUNoRSxPQUFPO0lBQUVsQixhQUFhLEVBQUU7TUFBRSxHQUFHaUIsUUFBUSxDQUFDakI7SUFBYyxDQUFDO0lBQUVNLFdBQVcsRUFBRTtNQUFFLEdBQUdXLFFBQVEsQ0FBQ1g7SUFBWTtFQUFFLENBQUM7QUFDbkcsQ0FBQztBQUVELE1BQU1hLFVBQVUsR0FBSUMsS0FBZ0IsSUFBZ05BLEtBQUssQ0FBQ25CLElBQUksS0FBSyxrQkFBa0IsR0FDalI7RUFBRUEsSUFBSSxFQUFFLGFBQWE7RUFBRW9CLGNBQWMsRUFBRSxrQkFBa0I7RUFBRUMsYUFBYSxFQUFFLHNCQUFzQjtFQUFFQyxXQUFXLEVBQUUsU0FBUztFQUFFQyxZQUFZLEVBQUUsUUFBUTtFQUFFQyxXQUFXLEVBQUUsVUFBVTtFQUFFQyxJQUFJLEVBQUU7QUFBTyxDQUFDLEdBQ3pMTixLQUFLLENBQUNuQixJQUFJLEtBQUssWUFBWSxHQUN6QjtFQUFFQSxJQUFJLEVBQUUsYUFBYTtFQUFFb0IsY0FBYyxFQUFFLGtCQUFrQjtFQUFFQyxhQUFhLEVBQUUsb0JBQW9CO0VBQUVDLFdBQVcsRUFBRSxPQUFPO0VBQUVDLFlBQVksRUFBRSxRQUFRO0VBQUVDLFdBQVcsRUFBRSxVQUFVO0VBQUVDLElBQUksRUFBRTtBQUFPLENBQUMsR0FDckxOLEtBQUssQ0FBQ25CLElBQUksS0FBSyx1QkFBdUIsR0FDcEM7RUFBRUEsSUFBSSxFQUFFLGFBQWE7RUFBRW9CLGNBQWMsRUFBRSxrQkFBa0I7RUFBRUMsYUFBYSxFQUFFLHFCQUFxQjtFQUFFQyxXQUFXLEVBQUUsT0FBTztFQUFFQyxZQUFZLEVBQUUsUUFBUTtFQUFFQyxXQUFXLEVBQUUsUUFBUTtFQUFFQyxJQUFJLEVBQUU7QUFBVyxDQUFDLEdBQ3hMTixLQUFLLENBQUNuQixJQUFJLEtBQUssdUJBQXVCLEdBQ3BDO0VBQUVBLElBQUksRUFBRSxhQUFhO0VBQUVvQixjQUFjLEVBQUUsa0JBQWtCO0VBQUVDLGFBQWEsRUFBRSxtQkFBbUI7RUFBRUMsV0FBVyxFQUFFLFFBQVE7RUFBRUMsWUFBWSxFQUFFLFFBQVE7RUFBRUMsV0FBVyxFQUFFLFFBQVE7RUFBRUMsSUFBSSxFQUFFO0FBQU8sQ0FBQyxHQUNuTE4sS0FBSyxDQUFDbkIsSUFBSSxLQUFLLHVCQUF1QixHQUNwQztFQUFFQSxJQUFJLEVBQUUsYUFBYTtFQUFFb0IsY0FBYyxFQUFFLGtCQUFrQjtFQUFFQyxhQUFhLEVBQUUsbUJBQW1CO0VBQUVDLFdBQVcsRUFBRSxPQUFPO0VBQUVDLFlBQVksRUFBRSxRQUFRO0VBQUVDLFdBQVcsRUFBRSxVQUFVO0VBQUVDLElBQUksRUFBRTtBQUFRLENBQUMsR0FDckxOLEtBQUssQ0FBQ25CLElBQUksS0FBSyxjQUFjLEdBQzNCO0VBQUVBLElBQUksRUFBRSxhQUFhO0VBQUVvQixjQUFjLEVBQUUsZUFBZTtFQUFFQyxhQUFhLEVBQUUsdUJBQXVCO0VBQUVDLFdBQVcsRUFBRSxPQUFPO0VBQUVDLFlBQVksRUFBRSxPQUFPO0VBQUVDLFdBQVcsRUFBRSxVQUFVO0VBQUVDLElBQUksRUFBRTtBQUFPLENBQUMsR0FDcExOLEtBQUssQ0FBQ25CLElBQUksS0FBSyxjQUFjLEdBQzNCO0VBQUVBLElBQUksRUFBRSxhQUFhO0VBQUVvQixjQUFjLEVBQUUsa0JBQWtCO0VBQUVDLGFBQWEsRUFBRSxvQkFBb0I7RUFBRUMsV0FBVyxFQUFFLE1BQU07RUFBRUMsWUFBWSxFQUFFLFFBQVE7RUFBRUMsV0FBVyxFQUFFLFFBQVE7RUFBRUMsSUFBSSxFQUFFO0FBQVUsQ0FBQyxHQUNyTDtFQUFFekIsSUFBSSxFQUFFLGFBQWE7RUFBRW9CLGNBQWMsRUFBRSxrQkFBa0I7RUFBRUMsYUFBYSxFQUFFLHFCQUFxQjtFQUFFQyxXQUFXLEVBQUUsU0FBUztFQUFFQyxZQUFZLEVBQUUsUUFBUTtFQUFFQyxXQUFXLEVBQUUsUUFBUTtFQUFFQyxJQUFJLEVBQUU7QUFBTyxDQUFDO0FBRXRNLE1BQU1DLFVBQVUsR0FBSVAsS0FBZ0IsSUFBS0EsS0FBSyxDQUFDbkIsSUFBSSxLQUFLLGNBQWMsR0FBR21CLEtBQUssQ0FBQ1EsT0FBTyxHQUFHLENBQUNSLEtBQUssQ0FBQ1MsS0FBSyxDQUFDO0FBRXRHLE9BQU8sTUFBTUMsbUJBQW1CLEdBQUlDLEtBQVksSUFBVztFQUFBLElBQUFDLGlCQUFBLEVBQUFDLGtCQUFBO0VBQ3pELE1BQU1DLEtBQW1CLEdBQUcsRUFBQUYsaUJBQUEsR0FBQ0QsS0FBSyxDQUFDSSxVQUFVLGNBQUFILGlCQUFBLGNBQUFBLGlCQUFBLEdBQUksRUFBRSxFQUFFSSxHQUFHLENBQUNoQixLQUFLLElBQUk7SUFDaEUsTUFBTWlCLE9BQU8sR0FBR2xCLFVBQVUsQ0FBQ0MsS0FBSyxDQUFDO0lBQ2pDLE1BQU1rQixJQUFJLEdBQUd2QixzQkFBc0IsQ0FBQ0ssS0FBSyxDQUFDbUIsRUFBRSxDQUFDO0lBQzdDLE1BQU1BLEVBQUUsR0FBRyxlQUFlbkIsS0FBSyxDQUFDbUIsRUFBRSxFQUFFO0lBQ3BDbkIsS0FBSyxDQUFDb0IsTUFBTSxDQUFDQyxRQUFRLEdBQUdGLEVBQUU7SUFDMUIsT0FBTztNQUFFRyxPQUFPLEVBQUUsQ0FBQztNQUFFSCxFQUFFO01BQUU5QixRQUFRLEVBQUVXLEtBQUssQ0FBQ21CLEVBQUU7TUFBRSxHQUFHRixPQUFPO01BQUUsR0FBR0MsSUFBSTtNQUFFSyxRQUFRLEVBQUU7UUFBRSxHQUFHdkIsS0FBSyxDQUFDdUI7TUFBUyxDQUFDO01BQUVmLE9BQU8sRUFBRUQsVUFBVSxDQUFDUCxLQUFLLENBQUMsQ0FBQ2dCLEdBQUcsQ0FBQ1EsS0FBSyxLQUFLO1FBQUUsR0FBR0E7TUFBTSxDQUFDLENBQUMsQ0FBQztNQUFFQyxPQUFPLEVBQUV6QixLQUFLLENBQUN5QixPQUFPLENBQUNULEdBQUcsQ0FBQ1EsS0FBSyxLQUFLO1FBQUUsR0FBR0E7TUFBTSxDQUFDLENBQUMsQ0FBQztNQUFFRSxZQUFZLEVBQUU7SUFBSyxDQUFDO0VBQ3ZPLENBQUMsQ0FBQztFQUNGLE1BQU1DLE9BQU8sR0FBRyxJQUFJQyxHQUFHLENBQUNkLEtBQUssQ0FBQ0UsR0FBRyxDQUFDYSxJQUFJLElBQUksQ0FBQ0EsSUFBSSxDQUFDeEMsUUFBUSxFQUFFd0MsSUFBSSxDQUFDLENBQUMsQ0FBQztFQUNqRSxNQUFNQyxNQUFxQixHQUFHLEVBQUFqQixrQkFBQSxHQUFDRixLQUFLLENBQUNJLFVBQVUsY0FBQUYsa0JBQUEsY0FBQUEsa0JBQUEsR0FBSSxFQUFFLEVBQUVrQixPQUFPLENBQUMvQixLQUFLLElBQUk7SUFDdEUsTUFBTTZCLElBQUksR0FBR0YsT0FBTyxDQUFDSyxHQUFHLENBQUNoQyxLQUFLLENBQUNtQixFQUFFLENBQUU7SUFDbkMsTUFBTWMsTUFBTSxHQUFHSixJQUFJLENBQUNyQixPQUFPLENBQUNRLEdBQUcsQ0FBQyxDQUFDUCxLQUFLLEVBQUV5QixLQUFLLE1BQU07TUFBRVosT0FBTyxFQUFFLENBQVU7TUFBRUgsRUFBRSxFQUFFLGdCQUFnQlUsSUFBSSxDQUFDVixFQUFFLFdBQVdlLEtBQUssRUFBRTtNQUFFQyxNQUFNLEVBQUVOLElBQUksQ0FBQ1YsRUFBRTtNQUFFdEMsSUFBSSxFQUFFLG1CQUE0QjtNQUFFdUQsSUFBSSxFQUFFO1FBQUUsR0FBR1AsSUFBSSxDQUFDTjtNQUFTLENBQUM7TUFBRWQsS0FBSyxFQUFFO1FBQUUsR0FBR0E7TUFBTSxDQUFDO01BQUVSLGNBQWMsRUFBRTRCLElBQUksQ0FBQzVCLGNBQWM7TUFBRUMsYUFBYSxFQUFFMkIsSUFBSSxDQUFDM0IsYUFBYTtNQUFFRSxZQUFZLEVBQUV5QixJQUFJLENBQUN6QixZQUFZO01BQUVDLFdBQVcsRUFBRXdCLElBQUksQ0FBQ3hCLFdBQVc7TUFBRUMsSUFBSSxFQUFFdUIsSUFBSSxDQUFDdkIsSUFBSTtNQUFFb0IsWUFBWSxFQUFFO0lBQWMsQ0FBQyxDQUFDLENBQUM7SUFDdlosSUFBSTFCLEtBQUssQ0FBQ25CLElBQUksS0FBSyxrQkFBa0IsSUFBSSxDQUFDbUIsS0FBSyxDQUFDcUMsY0FBYyxFQUFFLE9BQU9KLE1BQU07SUFDN0UsT0FBTyxDQUFDLEdBQUdBLE1BQU0sRUFBRTtNQUFFWCxPQUFPLEVBQUUsQ0FBVTtNQUFFSCxFQUFFLEVBQUUsZ0JBQWdCVSxJQUFJLENBQUNWLEVBQUUsYUFBYTtNQUFFZ0IsTUFBTSxFQUFFTixJQUFJLENBQUNWLEVBQUU7TUFBRXRDLElBQUksRUFBRSxpQkFBMEI7TUFBRXVELElBQUksRUFBRTtRQUFFLEdBQUdQLElBQUksQ0FBQ047TUFBUyxDQUFDO01BQUVkLEtBQUssRUFBRTtRQUFFLEdBQUdULEtBQUssQ0FBQ1M7TUFBTSxDQUFDO01BQUVSLGNBQWMsRUFBRTRCLElBQUksQ0FBQzVCLGNBQWM7TUFBRUMsYUFBYSxFQUFFMkIsSUFBSSxDQUFDM0IsYUFBYTtNQUFFRSxZQUFZLEVBQUV5QixJQUFJLENBQUN6QixZQUFZO01BQUVDLFdBQVcsRUFBRSxVQUFtQjtNQUFFQyxJQUFJLEVBQUV1QixJQUFJLENBQUN2QixJQUFJO01BQUVvQixZQUFZLEVBQUUsSUFBYTtNQUFFWSxXQUFXLEVBQUU7UUFBRUMsS0FBSyxFQUFFdkMsS0FBSyxDQUFDcUMsY0FBYyxDQUFDRyxXQUFXO1FBQUU3QixLQUFLLEVBQUVYLEtBQUssQ0FBQ3FDLGNBQWMsQ0FBQ0k7TUFBWSxDQUFDO01BQUVDLFNBQVMsRUFBRSxTQUFrQjtNQUFFQyxPQUFPLEVBQUUsYUFBc0I7TUFBRUMsZUFBZSxFQUFFO0lBQXFCLENBQUMsQ0FBQztFQUNwa0IsQ0FBQyxDQUFDO0VBQ0ZqQyxLQUFLLENBQUNrQyxXQUFXLEdBQUcvQixLQUFLO0VBQ3pCSCxLQUFLLENBQUNtQyxZQUFZLEdBQUdoQixNQUFNO0FBQzdCLENBQUM7QUFFRCxNQUFNaUIsUUFBUSxHQUFHQSxDQUFDQyxJQUE4QixFQUFFQyxLQUErQixLQUFhQyxJQUFJLENBQUNDLEdBQUcsQ0FBQ0QsSUFBSSxDQUFDRSxHQUFHLENBQUNKLElBQUksQ0FBQ0ssQ0FBQyxHQUFHSixLQUFLLENBQUNJLENBQUMsQ0FBQyxFQUFFSCxJQUFJLENBQUNFLEdBQUcsQ0FBQ0osSUFBSSxDQUFDTSxDQUFDLEdBQUdMLEtBQUssQ0FBQ0ssQ0FBQyxDQUFDLENBQUM7QUFDOUosTUFBTUMsT0FBTyxHQUFHQSxDQUFDNUMsS0FBWSxFQUFFYSxLQUErQjtFQUFBLElBQUFnQyxxQkFBQTtFQUFBLE9BQWMsRUFBQUEscUJBQUEsR0FBQTdDLEtBQUssQ0FBQzhDLEtBQUssQ0FBQy9FLFVBQVUsQ0FBQ2lDLEtBQUssRUFBRWEsS0FBSyxDQUFDNkIsQ0FBQyxFQUFFN0IsS0FBSyxDQUFDOEIsQ0FBQyxDQUFDLENBQUMsY0FBQUUscUJBQUEsdUJBQWhEQSxxQkFBQSxDQUFrREQsT0FBTyxNQUFLLElBQUk7QUFBQTtBQUU5SSxPQUFPLE1BQU1HLGlCQUFvRCxHQUFHO0VBQ2xFQyxLQUFLLEVBQUUsc0JBQXNCO0VBQzdCQyxLQUFLLEVBQUUsMEJBQTBCO0VBQ2pDQyxJQUFJLEVBQUUsMkJBQTJCO0VBQ2pDQyxPQUFPLEVBQUUsK0JBQStCO0VBQ3hDQyxNQUFNLEVBQUU7QUFDVixDQUFDO0FBRUQsT0FBTyxNQUFNQyxxQkFBcUIsR0FBSW5DLElBQWdCLElBQWFBLElBQUksQ0FBQ3pCLFlBQVksS0FBSyxRQUFRLEdBQUcsNkNBQTZDLEdBQUcsMkNBQTJDO0FBQy9MLE9BQU8sTUFBTTZELGtCQUFrQixHQUFJcEMsSUFBZ0IsSUFBY0EsSUFBSSxDQUFDcUMsU0FBUyxLQUFLQyxTQUFTO0FBQzdGLE9BQU8sTUFBTUMsc0JBQXNCLEdBQUl2QyxJQUFnQixJQUFhO0VBQUEsSUFBQXdDLHFCQUFBLEVBQUFDLGVBQUE7RUFDbEUsTUFBTUMsT0FBTyxJQUFBRixxQkFBQSxJQUFBQyxlQUFBLEdBQUd6QyxJQUFJLENBQUNxQyxTQUFTLGNBQUFJLGVBQUEsdUJBQWRBLGVBQUEsQ0FBZ0JDLE9BQU8sY0FBQUYscUJBQUEsY0FBQUEscUJBQUEsR0FBSXhDLElBQUksQ0FBQzFCLFdBQVc7RUFDM0QsT0FBTyxtQkFBbUJvRSxPQUFPLEtBQUtiLGlCQUFpQixDQUFDYSxPQUFPLENBQUMsTUFBTTFDLElBQUksQ0FBQzNCLGFBQWEsS0FBSzhELHFCQUFxQixDQUFDbkMsSUFBSSxDQUFDLEVBQUU7QUFDNUgsQ0FBQztBQUVELE1BQU0yQyxlQUFlLEdBQUdBLENBQUNDLEtBQWUsRUFBRTVDLElBQWdCLEVBQUUwQyxPQUEwQixLQUFjQSxPQUFPLEtBQUssT0FBTyxHQUNuSDFDLElBQUksQ0FBQ3JCLE9BQU8sQ0FBQ2tFLElBQUksQ0FBQ2xELEtBQUssSUFBSStCLE9BQU8sQ0FBQ2tCLEtBQUssQ0FBQzlELEtBQUssRUFBRWEsS0FBSyxDQUFDLENBQUMsR0FDdkQrQyxPQUFPLEtBQUssT0FBTyxHQUNqQnhCLFFBQVEsQ0FBQzBCLEtBQUssQ0FBQ0UsSUFBSSxFQUFFOUMsSUFBSSxDQUFDTixRQUFRLENBQUMsSUFBSSxDQUFDLEdBQ3hDZ0QsT0FBTyxLQUFLLE1BQU0sSUFBSUEsT0FBTyxLQUFLLFNBQVMsR0FDekN4QixRQUFRLENBQUMwQixLQUFLLENBQUNFLElBQUksRUFBRTlDLElBQUksQ0FBQ04sUUFBUSxDQUFDLEtBQUssQ0FBQyxHQUN6Q3dCLFFBQVEsQ0FBQzBCLEtBQUssQ0FBQ0UsSUFBSSxFQUFFOUMsSUFBSSxDQUFDTixRQUFRLENBQUMsSUFBSSxDQUFDO0FBRWhELE9BQU8sTUFBTXFELG1CQUFtQixHQUFHQSxDQUFDSCxLQUFlLEVBQUVGLE9BQTBCO0VBQUEsSUFBQU0scUJBQUE7RUFBQSxPQUFtQixFQUFBQSxxQkFBQSxHQUFDSixLQUFLLENBQUM5RCxLQUFLLENBQUNrQyxXQUFXLGNBQUFnQyxxQkFBQSxjQUFBQSxxQkFBQSxHQUFJLEVBQUUsRUFBRUMsTUFBTSxDQUFDakQsSUFBSSxJQUFJO0lBQy9JLElBQUlBLElBQUksQ0FBQ3FDLFNBQVMsSUFBSXJDLElBQUksQ0FBQzFCLFdBQVcsS0FBS29FLE9BQU8sSUFBSSxDQUFDQyxlQUFlLENBQUNDLEtBQUssRUFBRTVDLElBQUksRUFBRTBDLE9BQU8sQ0FBQyxFQUFFLE9BQU8sS0FBSztJQUMxRzFDLElBQUksQ0FBQ3FDLFNBQVMsR0FBRztNQUFFSyxPQUFPO01BQUVRLElBQUksRUFBRU4sS0FBSyxDQUFDTTtJQUFLLENBQUM7SUFDOUMsT0FBTyxJQUFJO0VBQ2IsQ0FBQyxDQUFDO0FBQUE7QUFHRixPQUFPLE1BQU1DLGlCQUFpQixHQUFHQSxDQUFDUCxLQUFlLEVBQUVwRCxRQUFnQixLQUF3QjtFQUFBLElBQUE0RCxzQkFBQTtFQUN6RixNQUFNcEQsSUFBSSxJQUFBb0Qsc0JBQUEsR0FBR1IsS0FBSyxDQUFDOUQsS0FBSyxDQUFDa0MsV0FBVyxjQUFBb0Msc0JBQUEsdUJBQXZCQSxzQkFBQSxDQUF5QkMsSUFBSSxDQUFDQyxTQUFTLElBQUlBLFNBQVMsQ0FBQ2hFLEVBQUUsS0FBS0UsUUFBUSxDQUFDO0VBQ2xGLElBQUksQ0FBQ1EsSUFBSSxFQUFFLE9BQU9zQyxTQUFTO0VBQzNCLElBQUl0QyxJQUFJLENBQUN1RCxVQUFVLEVBQUUsT0FBTyxrQkFBa0I7RUFDOUMsTUFBTUEsVUFBNEIsR0FBRztJQUFFTCxJQUFJLEVBQUVOLEtBQUssQ0FBQ00sSUFBSTtJQUFFTSxVQUFVLEVBQUV4RCxJQUFJLENBQUNqRCxhQUFhLENBQUNDLElBQUk7SUFBRXlHLFdBQVcsRUFBRXpELElBQUksQ0FBQ2pELGFBQWEsQ0FBQ0csS0FBSztJQUFFd0csUUFBUSxFQUFFMUQsSUFBSSxDQUFDM0MsV0FBVyxDQUFDTDtFQUFLLENBQUM7RUFDdEtnRCxJQUFJLENBQUN1RCxVQUFVLEdBQUdBLFVBQVU7RUFDNUIsT0FBTztJQUFFdkQsSUFBSTtJQUFFdUQ7RUFBVyxDQUFDO0FBQzdCLENBQUM7QUFDRCxPQUFPLE1BQU1JLHVCQUF1QixHQUFJM0QsSUFBZ0IsSUFBYSxxQkFBcUJBLElBQUksQ0FBQ2pELGFBQWEsQ0FBQ0UsS0FBSyxNQUFNK0MsSUFBSSxDQUFDakQsYUFBYSxDQUFDRyxLQUFLLHdCQUF3QjhDLElBQUksQ0FBQzNDLFdBQVcsQ0FBQ0osS0FBSyxLQUFLK0MsSUFBSSxDQUFDM0MsV0FBVyxDQUFDQyxNQUFNLEVBQUU7QUFDNU4sT0FBTyxNQUFNc0csb0JBQW9CLEdBQUlDLEtBQWtCLElBQWFBLEtBQUssQ0FBQ3BELFdBQVcsR0FBRyxHQUFHb0QsS0FBSyxDQUFDaEQsU0FBUyxLQUFLLFNBQVMsR0FBRyxTQUFTLEdBQUcsU0FBUyxJQUFJZ0QsS0FBSyxDQUFDcEQsV0FBVyxDQUFDQyxLQUFLLHNCQUFzQm1ELEtBQUssQ0FBQ3BELFdBQVcsQ0FBQzNCLEtBQUssR0FBRyxDQUFDLDZCQUE2QitFLEtBQUssQ0FBQzlDLGVBQWUsS0FBSyxhQUFhLEdBQUcsdUJBQXVCLEdBQUcsV0FBVyxFQUFFLEdBQUcsa0NBQWtDIiwiaWdub3JlTGlzdCI6W119