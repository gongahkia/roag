// ca7dbd401030ac8e0fbda2a8dc25ab57cccb9fca
import { rngFor } from '../rng';
import { cloneCompanions, loseCompanionForRescue } from './companions';
import { cloneCarryoverDiagnostics } from './carryover';
export const BIOME_POOL = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins', 'cliffs', 'burial', 'saltFlats', 'frostReliquary'];
export const AREA_ORDER = BIOME_POOL;
export const LEGACY_AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins', 'furnace', 'floodedRuins'];
export const DEFAULT_AREA_ORDER = ['mine', 'wilds', 'caverns', 'ruins'];
export const CAMPAIGN_TIERS = ['base', 'ngPlus', 'ngPlusPlus'];
export const isCampaignAreaOrder = value => value.length === 4 && value.every(area => BIOME_POOL.includes(area)) && new Set(value).size === 4;
export const isLegacyCampaignAreaOrder = value => value.length === LEGACY_AREA_ORDER.length && value.every(area => LEGACY_AREA_ORDER.includes(area)) && new Set(value).size === LEGACY_AREA_ORDER.length;
export const campaignOrderForSeed = seed => {
  const rng = rngFor(seed, 'progression', 'campaign-area-order');
  return rng.shuffle([...rng.shuffle([...BIOME_POOL]).slice(0, 4)]);
};
export const nextArea = (biome, areaOrder = AREA_ORDER) => areaOrder[areaOrder.indexOf(biome) + 1];
export const unlockNextArea = (unlocked, completed, areaOrder = AREA_ORDER) => {
  const next = nextArea(completed, areaOrder);
  return next && !unlocked.includes(next) ? [...unlocked, next] : [...unlocked];
};
const tierIndex = tier => CAMPAIGN_TIERS.indexOf(tier);
const expectedCycleEvents = (currentTier, completedTiers) => {
  const current = tierIndex(currentTier);
  const events = [{
    sequence: 0,
    tier: 'base',
    kind: 'entered'
  }];
  for (let index = 0; index < completedTiers.length; index++) {
    const tier = CAMPAIGN_TIERS[index];
    events.push({
      sequence: events.length,
      tier,
      kind: 'victory'
    });
    if (index + 1 <= current) events.push({
      sequence: events.length,
      tier: CAMPAIGN_TIERS[index + 1],
      kind: 'entered'
    });
  }
  return events;
};
export const campaignCycleErrors = cycle => {
  const errors = [];
  const current = tierIndex(cycle.currentTier);
  if (cycle.version !== 1) errors.push('unsupported cycle version');
  if (current < 0) errors.push('invalid current tier');
  const expectedCompleted = CAMPAIGN_TIERS.slice(0, cycle.completedTiers.length);
  if (cycle.completedTiers.length > CAMPAIGN_TIERS.length || cycle.completedTiers.some((tier, index) => tier !== expectedCompleted[index])) errors.push('completed tiers must be an ordered prefix');
  if (current >= 0 && cycle.completedTiers.length !== current && cycle.completedTiers.length !== current + 1) errors.push('current tier is inconsistent with completed tiers');
  const completedCap = cycle.completedTiers.length === CAMPAIGN_TIERS.length;
  if (cycle.completedCap !== completedCap || cycle.completedCap && cycle.currentTier !== 'ngPlusPlus') errors.push('invalid completed cap');
  const expectedEvents = expectedCycleEvents(cycle.currentTier, cycle.completedTiers);
  if (cycle.events.length !== expectedEvents.length || cycle.events.some((event, index) => {
    var _expectedEvents$index, _expectedEvents$index2;
    return event.sequence !== index || event.tier !== ((_expectedEvents$index = expectedEvents[index]) === null || _expectedEvents$index === void 0 ? void 0 : _expectedEvents$index.tier) || event.kind !== ((_expectedEvents$index2 = expectedEvents[index]) === null || _expectedEvents$index2 === void 0 ? void 0 : _expectedEvents$index2.kind);
  })) errors.push('invalid cycle event history');
  return errors;
};
export const assertCampaignCycle = cycle => {
  const errors = campaignCycleErrors(cycle);
  if (errors.length) throw new Error(`invalid campaign cycle: ${errors.join('; ')}`);
  return cycle;
};
export const initialCampaignCycle = () => ({
  version: 1,
  currentTier: 'base',
  completedTiers: [],
  events: [{
    sequence: 0,
    tier: 'base',
    kind: 'entered'
  }],
  completedCap: false
});
export const cloneCampaignCycle = cycle => {
  assertCampaignCycle(cycle);
  return {
    version: 1,
    currentTier: cycle.currentTier,
    completedTiers: [...cycle.completedTiers],
    events: cycle.events.map(event => ({
      ...event
    })),
    completedCap: cycle.completedCap
  };
};
export const completeCampaignTier = cycle => {
  assertCampaignCycle(cycle);
  if (cycle.completedTiers.includes(cycle.currentTier)) throw new Error(`cannot complete campaign tier ${cycle.currentTier}: victory already recorded`);
  const completedTiers = [...cycle.completedTiers, cycle.currentTier];
  return cloneCampaignCycle({
    version: 1,
    currentTier: cycle.currentTier,
    completedTiers,
    events: [...cycle.events, {
      sequence: cycle.events.length,
      tier: cycle.currentTier,
      kind: 'victory'
    }],
    completedCap: cycle.currentTier === 'ngPlusPlus'
  });
};
export const advanceCampaignTier = cycle => {
  assertCampaignCycle(cycle);
  if (cycle.completedCap) throw new Error('cannot advance campaign tier: NG++ completed cap reached');
  if (!cycle.completedTiers.includes(cycle.currentTier)) throw new Error(`cannot advance campaign tier ${cycle.currentTier}: victory not recorded`);
  const next = CAMPAIGN_TIERS[tierIndex(cycle.currentTier) + 1];
  if (!next) throw new Error('cannot advance campaign tier: NG++ completed cap reached');
  return cloneCampaignCycle({
    version: 1,
    currentTier: next,
    completedTiers: [...cycle.completedTiers],
    events: [...cycle.events, {
      sequence: cycle.events.length,
      tier: next,
      kind: 'entered'
    }],
    completedCap: false
  });
};
export const campaignContinuationPending = cycle => {
  assertCampaignCycle(cycle);
  return !cycle.completedCap && cycle.completedTiers.includes(cycle.currentTier);
};
const cloneCompanionControlHistory = history => history.map(event => ({
  ...event
}));
const companionsForControlMode = (companions, rescues, mode) => cloneCompanions(companions, rescues).map(companion => ({
  ...companion,
  controlMode: mode
}));
const cloneCompanionControl = state => ({
  companionControlMode: state.companionControlMode,
  companionControlHistory: cloneCompanionControlHistory(state.companionControlHistory),
  companions: companionsForControlMode(state.companions, state.rescuedNpcs, state.companionControlMode)
});
export const changeCampaignCompanionControlMode = (state, mode, context) => {
  const current = cloneCompanionControl(state);
  if (context !== 'lodge') return {
    changed: false,
    message: 'Companion control can change only at the Lodge between floors.',
    state: {
      ...state,
      ...current
    }
  };
  if (mode === state.companionControlMode) return {
    changed: false,
    message: `Companion control is already ${mode}.`,
    state: {
      ...state,
      ...current
    }
  };
  const companionControlHistory = [...current.companionControlHistory, {
    sequence: current.companionControlHistory.length,
    mode,
    source: 'lodge'
  }];
  return {
    changed: true,
    message: `Companion control changed to ${mode}.`,
    state: {
      ...state,
      companionControlMode: mode,
      companionControlHistory,
      companions: companionsForControlMode(state.companions, state.rescuedNpcs, mode)
    }
  };
};
export const continueCampaignRoute = state => {
  var _state$reputation$tra, _state$reputation, _state$reputation$kam, _state$reputation2, _state$reputation$tra2, _state$reputation3, _state$reputation$kam2, _state$reputation4;
  const cycle = cloneCampaignCycle(state.cycle);
  const companionControl = cloneCompanionControl(state);
  if (cycle.completedCap) throw new Error('cannot continue campaign: NG++ completed cap reached');
  if (!campaignContinuationPending(cycle)) return {
    ...state,
    areaOrder: [...state.areaOrder],
    completedAreas: [...state.completedAreas],
    unlockedAreas: [...state.unlockedAreas],
    rescuedNpcs: state.rescuedNpcs.map(npc => ({
      ...npc
    })),
    ...companionControl,
    carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
    lineageEvents: state.lineageEvents.map(event => ({
      ...event
    })),
    legacyRecords: state.legacyRecords.map(record => ({
      ...record
    })),
    alignment: {
      ...state.alignment
    },
    reputation: {
      trailfolk: (_state$reputation$tra = (_state$reputation = state.reputation) === null || _state$reputation === void 0 ? void 0 : _state$reputation.trailfolk) !== null && _state$reputation$tra !== void 0 ? _state$reputation$tra : 0,
      kami: (_state$reputation$kam = (_state$reputation2 = state.reputation) === null || _state$reputation2 === void 0 ? void 0 : _state$reputation2.kami) !== null && _state$reputation$kam !== void 0 ? _state$reputation$kam : 0
    },
    cycle
  };
  const selectedBiome = state.areaOrder[0];
  if (!selectedBiome) throw new Error('cannot continue campaign: missing area order');
  return {
    ...state,
    areaOrder: [...state.areaOrder],
    completedAreas: [],
    unlockedAreas: [selectedBiome],
    selectedBiome,
    rescuedNpcs: state.rescuedNpcs.map(npc => ({
      ...npc
    })),
    ...companionControl,
    carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
    lineageEvents: state.lineageEvents.map(event => ({
      ...event
    })),
    legacyRecords: state.legacyRecords.map(record => ({
      ...record
    })),
    alignment: {
      ...state.alignment
    },
    reputation: {
      trailfolk: (_state$reputation$tra2 = (_state$reputation3 = state.reputation) === null || _state$reputation3 === void 0 ? void 0 : _state$reputation3.trailfolk) !== null && _state$reputation$tra2 !== void 0 ? _state$reputation$tra2 : 0,
      kami: (_state$reputation$kam2 = (_state$reputation4 = state.reputation) === null || _state$reputation4 === void 0 ? void 0 : _state$reputation4.kami) !== null && _state$reputation$kam2 !== void 0 ? _state$reputation$kam2 : 0
    },
    cycle: advanceCampaignTier(cycle)
  };
};
export const initialCampaignRoute = (seed, companionControlMode = 'autonomous') => {
  const areaOrder = seed === undefined ? [...DEFAULT_AREA_ORDER] : campaignOrderForSeed(seed);
  return {
    version: 5,
    areaOrder,
    completedAreas: [],
    unlockedAreas: [areaOrder[0]],
    selectedBiome: areaOrder[0],
    rescuedNpcs: [],
    companions: [],
    companionControlMode,
    companionControlHistory: [{
      sequence: 0,
      mode: companionControlMode,
      source: 'creation'
    }],
    carryoverDiagnostics: [],
    lineageEvents: [],
    legacyRecords: [],
    alignment: {
      kami: 0,
      villagePact: 0
    },
    reputation: {
      trailfolk: 0,
      kami: 0
    },
    cycle: initialCampaignCycle()
  };
};
export const completeCampaignArea = (state, completed) => {
  var _state$reputation$tra3, _state$reputation5, _state$reputation$kam3, _state$reputation6;
  return {
    ...state,
    areaOrder: [...state.areaOrder],
    completedAreas: state.completedAreas.includes(completed) ? [...state.completedAreas] : [...state.completedAreas, completed],
    unlockedAreas: [...state.unlockedAreas],
    selectedBiome: completed,
    rescuedNpcs: [...state.rescuedNpcs],
    ...cloneCompanionControl(state),
    carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
    lineageEvents: [...state.lineageEvents],
    legacyRecords: [...state.legacyRecords],
    alignment: {
      ...state.alignment
    },
    reputation: {
      trailfolk: (_state$reputation$tra3 = (_state$reputation5 = state.reputation) === null || _state$reputation5 === void 0 ? void 0 : _state$reputation5.trailfolk) !== null && _state$reputation$tra3 !== void 0 ? _state$reputation$tra3 : 0,
      kami: (_state$reputation$kam3 = (_state$reputation6 = state.reputation) === null || _state$reputation6 === void 0 ? void 0 : _state$reputation6.kami) !== null && _state$reputation$kam3 !== void 0 ? _state$reputation$kam3 : 0
    },
    cycle: cloneCampaignCycle(state.cycle)
  };
};
export const unlockCampaignArea = (state, biome) => {
  var _state$reputation$tra4, _state$reputation7, _state$reputation$kam4, _state$reputation8;
  return {
    ...state,
    areaOrder: [...state.areaOrder],
    completedAreas: [...state.completedAreas],
    unlockedAreas: state.unlockedAreas.includes(biome) ? [...state.unlockedAreas] : [...state.unlockedAreas, biome],
    selectedBiome: biome,
    rescuedNpcs: [...state.rescuedNpcs],
    ...cloneCompanionControl(state),
    carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
    lineageEvents: [...state.lineageEvents],
    legacyRecords: [...state.legacyRecords],
    alignment: {
      ...state.alignment
    },
    reputation: {
      trailfolk: (_state$reputation$tra4 = (_state$reputation7 = state.reputation) === null || _state$reputation7 === void 0 ? void 0 : _state$reputation7.trailfolk) !== null && _state$reputation$tra4 !== void 0 ? _state$reputation$tra4 : 0,
      kami: (_state$reputation$kam4 = (_state$reputation8 = state.reputation) === null || _state$reputation8 === void 0 ? void 0 : _state$reputation8.kami) !== null && _state$reputation$kam4 !== void 0 ? _state$reputation$kam4 : 0
    },
    cycle: cloneCampaignCycle(state.cycle)
  };
};
export const recordCampaignSacrifice = (state, event) => {
  var _state$reputation$tra5, _state$reputation9, _state$reputation$kam5, _state$reputation0;
  const rescuedNpcs = state.rescuedNpcs.filter(npc => npc.id !== event.npcId);
  return {
    ...state,
    rescuedNpcs,
    companionControlMode: state.companionControlMode,
    companionControlHistory: cloneCompanionControlHistory(state.companionControlHistory),
    companions: companionsForControlMode(state.companions.map(companion => loseCompanionForRescue(companion, event.npcId)), rescuedNpcs, state.companionControlMode),
    carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
    lineageEvents: state.lineageEvents.some(existing => existing.id === event.id) ? [...state.lineageEvents] : [...state.lineageEvents, event].slice(-12),
    alignment: {
      ...state.alignment
    },
    reputation: {
      trailfolk: (_state$reputation$tra5 = (_state$reputation9 = state.reputation) === null || _state$reputation9 === void 0 ? void 0 : _state$reputation9.trailfolk) !== null && _state$reputation$tra5 !== void 0 ? _state$reputation$tra5 : 0,
      kami: (_state$reputation$kam5 = (_state$reputation0 = state.reputation) === null || _state$reputation0 === void 0 ? void 0 : _state$reputation0.kami) !== null && _state$reputation$kam5 !== void 0 ? _state$reputation$kam5 : 0
    },
    cycle: cloneCampaignCycle(state.cycle)
  };
};
export const appendLegacyRecord = (state, record) => {
  var _state$reputation$tra6, _state$reputation1, _state$reputation$kam6, _state$reputation10;
  return {
    ...state,
    ...cloneCompanionControl(state),
    carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
    legacyRecords: [...state.legacyRecords, {
      ...record
    }].slice(-12),
    alignment: {
      ...state.alignment
    },
    reputation: {
      trailfolk: (_state$reputation$tra6 = (_state$reputation1 = state.reputation) === null || _state$reputation1 === void 0 ? void 0 : _state$reputation1.trailfolk) !== null && _state$reputation$tra6 !== void 0 ? _state$reputation$tra6 : 0,
      kami: (_state$reputation$kam6 = (_state$reputation10 = state.reputation) === null || _state$reputation10 === void 0 ? void 0 : _state$reputation10.kami) !== null && _state$reputation$kam6 !== void 0 ? _state$reputation$kam6 : 0
    },
    cycle: cloneCampaignCycle(state.cycle)
  };
};
export const addAlignment = (state, alignment) => ({
  ...state,
  ...cloneCompanionControl(state),
  carryoverDiagnostics: cloneCarryoverDiagnostics(state.carryoverDiagnostics),
  alignment: {
    ...state.alignment,
    [alignment]: state.alignment[alignment] + 1
  },
  cycle: cloneCampaignCycle(state.cycle)
});
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJybmdGb3IiLCJjbG9uZUNvbXBhbmlvbnMiLCJsb3NlQ29tcGFuaW9uRm9yUmVzY3VlIiwiY2xvbmVDYXJyeW92ZXJEaWFnbm9zdGljcyIsIkJJT01FX1BPT0wiLCJBUkVBX09SREVSIiwiTEVHQUNZX0FSRUFfT1JERVIiLCJERUZBVUxUX0FSRUFfT1JERVIiLCJDQU1QQUlHTl9USUVSUyIsImlzQ2FtcGFpZ25BcmVhT3JkZXIiLCJ2YWx1ZSIsImxlbmd0aCIsImV2ZXJ5IiwiYXJlYSIsImluY2x1ZGVzIiwiU2V0Iiwic2l6ZSIsImlzTGVnYWN5Q2FtcGFpZ25BcmVhT3JkZXIiLCJjYW1wYWlnbk9yZGVyRm9yU2VlZCIsInNlZWQiLCJybmciLCJzaHVmZmxlIiwic2xpY2UiLCJuZXh0QXJlYSIsImJpb21lIiwiYXJlYU9yZGVyIiwiaW5kZXhPZiIsInVubG9ja05leHRBcmVhIiwidW5sb2NrZWQiLCJjb21wbGV0ZWQiLCJuZXh0IiwidGllckluZGV4IiwidGllciIsImV4cGVjdGVkQ3ljbGVFdmVudHMiLCJjdXJyZW50VGllciIsImNvbXBsZXRlZFRpZXJzIiwiY3VycmVudCIsImV2ZW50cyIsInNlcXVlbmNlIiwia2luZCIsImluZGV4IiwicHVzaCIsImNhbXBhaWduQ3ljbGVFcnJvcnMiLCJjeWNsZSIsImVycm9ycyIsInZlcnNpb24iLCJleHBlY3RlZENvbXBsZXRlZCIsInNvbWUiLCJjb21wbGV0ZWRDYXAiLCJleHBlY3RlZEV2ZW50cyIsImV2ZW50IiwiX2V4cGVjdGVkRXZlbnRzJGluZGV4IiwiX2V4cGVjdGVkRXZlbnRzJGluZGV4MiIsImFzc2VydENhbXBhaWduQ3ljbGUiLCJFcnJvciIsImpvaW4iLCJpbml0aWFsQ2FtcGFpZ25DeWNsZSIsImNsb25lQ2FtcGFpZ25DeWNsZSIsIm1hcCIsImNvbXBsZXRlQ2FtcGFpZ25UaWVyIiwiYWR2YW5jZUNhbXBhaWduVGllciIsImNhbXBhaWduQ29udGludWF0aW9uUGVuZGluZyIsImNsb25lQ29tcGFuaW9uQ29udHJvbEhpc3RvcnkiLCJoaXN0b3J5IiwiY29tcGFuaW9uc0ZvckNvbnRyb2xNb2RlIiwiY29tcGFuaW9ucyIsInJlc2N1ZXMiLCJtb2RlIiwiY29tcGFuaW9uIiwiY29udHJvbE1vZGUiLCJjbG9uZUNvbXBhbmlvbkNvbnRyb2wiLCJzdGF0ZSIsImNvbXBhbmlvbkNvbnRyb2xNb2RlIiwiY29tcGFuaW9uQ29udHJvbEhpc3RvcnkiLCJyZXNjdWVkTnBjcyIsImNoYW5nZUNhbXBhaWduQ29tcGFuaW9uQ29udHJvbE1vZGUiLCJjb250ZXh0IiwiY2hhbmdlZCIsIm1lc3NhZ2UiLCJzb3VyY2UiLCJjb250aW51ZUNhbXBhaWduUm91dGUiLCJfc3RhdGUkcmVwdXRhdGlvbiR0cmEiLCJfc3RhdGUkcmVwdXRhdGlvbiIsIl9zdGF0ZSRyZXB1dGF0aW9uJGthbSIsIl9zdGF0ZSRyZXB1dGF0aW9uMiIsIl9zdGF0ZSRyZXB1dGF0aW9uJHRyYTIiLCJfc3RhdGUkcmVwdXRhdGlvbjMiLCJfc3RhdGUkcmVwdXRhdGlvbiRrYW0yIiwiX3N0YXRlJHJlcHV0YXRpb240IiwiY29tcGFuaW9uQ29udHJvbCIsImNvbXBsZXRlZEFyZWFzIiwidW5sb2NrZWRBcmVhcyIsIm5wYyIsImNhcnJ5b3ZlckRpYWdub3N0aWNzIiwibGluZWFnZUV2ZW50cyIsImxlZ2FjeVJlY29yZHMiLCJyZWNvcmQiLCJhbGlnbm1lbnQiLCJyZXB1dGF0aW9uIiwidHJhaWxmb2xrIiwia2FtaSIsInNlbGVjdGVkQmlvbWUiLCJpbml0aWFsQ2FtcGFpZ25Sb3V0ZSIsInVuZGVmaW5lZCIsInZpbGxhZ2VQYWN0IiwiY29tcGxldGVDYW1wYWlnbkFyZWEiLCJfc3RhdGUkcmVwdXRhdGlvbiR0cmEzIiwiX3N0YXRlJHJlcHV0YXRpb241IiwiX3N0YXRlJHJlcHV0YXRpb24ka2FtMyIsIl9zdGF0ZSRyZXB1dGF0aW9uNiIsInVubG9ja0NhbXBhaWduQXJlYSIsIl9zdGF0ZSRyZXB1dGF0aW9uJHRyYTQiLCJfc3RhdGUkcmVwdXRhdGlvbjciLCJfc3RhdGUkcmVwdXRhdGlvbiRrYW00IiwiX3N0YXRlJHJlcHV0YXRpb244IiwicmVjb3JkQ2FtcGFpZ25TYWNyaWZpY2UiLCJfc3RhdGUkcmVwdXRhdGlvbiR0cmE1IiwiX3N0YXRlJHJlcHV0YXRpb245IiwiX3N0YXRlJHJlcHV0YXRpb24ka2FtNSIsIl9zdGF0ZSRyZXB1dGF0aW9uMCIsImZpbHRlciIsImlkIiwibnBjSWQiLCJleGlzdGluZyIsImFwcGVuZExlZ2FjeVJlY29yZCIsIl9zdGF0ZSRyZXB1dGF0aW9uJHRyYTYiLCJfc3RhdGUkcmVwdXRhdGlvbjEiLCJfc3RhdGUkcmVwdXRhdGlvbiRrYW02IiwiX3N0YXRlJHJlcHV0YXRpb24xMCIsImFkZEFsaWdubWVudCJdLCJzb3VyY2VzIjpbImNhbXBhaWduLnRzIl0sInNvdXJjZXNDb250ZW50IjpbImltcG9ydCB0eXBlIHsgQWxpZ25tZW50LCBCaW9tZSwgQ2FtcGFpZ25DeWNsZSwgQ2FtcGFpZ25Sb3V0ZVN0YXRlLCBDYW1wYWlnblRpZXIsIENvbXBhbmlvbkNvbnRyb2xNb2RlLCBDb21wYW5pb25Db250cm9sTW9kZUV2ZW50LCBMZWdhY3lSZWNvcmQsIExpbmVhZ2VFdmVudCB9IGZyb20gJy4uL3R5cGVzJ1xuaW1wb3J0IHsgcm5nRm9yIH0gZnJvbSAnLi4vcm5nJ1xuaW1wb3J0IHsgY2xvbmVDb21wYW5pb25zLCBsb3NlQ29tcGFuaW9uRm9yUmVzY3VlIH0gZnJvbSAnLi9jb21wYW5pb25zJ1xuaW1wb3J0IHsgY2xvbmVDYXJyeW92ZXJEaWFnbm9zdGljcyB9IGZyb20gJy4vY2FycnlvdmVyJ1xuXG5leHBvcnQgY29uc3QgQklPTUVfUE9PTCA9IFsnbWluZScsICd3aWxkcycsICdjYXZlcm5zJywgJ3J1aW5zJywgJ2Z1cm5hY2UnLCAnZmxvb2RlZFJ1aW5zJywgJ2NsaWZmcycsICdidXJpYWwnLCAnc2FsdEZsYXRzJywgJ2Zyb3N0UmVsaXF1YXJ5J10gYXMgY29uc3Qgc2F0aXNmaWVzIHJlYWRvbmx5IEJpb21lW11cbmV4cG9ydCBjb25zdCBBUkVBX09SREVSID0gQklPTUVfUE9PTFxuZXhwb3J0IGNvbnN0IExFR0FDWV9BUkVBX09SREVSID0gWydtaW5lJywgJ3dpbGRzJywgJ2NhdmVybnMnLCAncnVpbnMnLCAnZnVybmFjZScsICdmbG9vZGVkUnVpbnMnXSBhcyBjb25zdCBzYXRpc2ZpZXMgcmVhZG9ubHkgQmlvbWVbXVxuZXhwb3J0IGNvbnN0IERFRkFVTFRfQVJFQV9PUkRFUiA9IFsnbWluZScsICd3aWxkcycsICdjYXZlcm5zJywgJ3J1aW5zJ10gYXMgY29uc3Qgc2F0aXNmaWVzIHJlYWRvbmx5IEJpb21lW11cbmV4cG9ydCBjb25zdCBDQU1QQUlHTl9USUVSUyA9IFsnYmFzZScsICduZ1BsdXMnLCAnbmdQbHVzUGx1cyddIGFzIGNvbnN0IHNhdGlzZmllcyByZWFkb25seSBDYW1wYWlnblRpZXJbXVxuZXhwb3J0IGNvbnN0IGlzQ2FtcGFpZ25BcmVhT3JkZXIgPSAodmFsdWU6IHJlYWRvbmx5IEJpb21lW10pOiBib29sZWFuID0+IHZhbHVlLmxlbmd0aCA9PT0gNCAmJiB2YWx1ZS5ldmVyeShhcmVhID0+IEJJT01FX1BPT0wuaW5jbHVkZXMoYXJlYSkpICYmIG5ldyBTZXQodmFsdWUpLnNpemUgPT09IDRcbmV4cG9ydCBjb25zdCBpc0xlZ2FjeUNhbXBhaWduQXJlYU9yZGVyID0gKHZhbHVlOiByZWFkb25seSBCaW9tZVtdKTogYm9vbGVhbiA9PiB2YWx1ZS5sZW5ndGggPT09IExFR0FDWV9BUkVBX09SREVSLmxlbmd0aCAmJiB2YWx1ZS5ldmVyeShhcmVhID0+IChMRUdBQ1lfQVJFQV9PUkRFUiBhcyByZWFkb25seSBCaW9tZVtdKS5pbmNsdWRlcyhhcmVhKSkgJiYgbmV3IFNldCh2YWx1ZSkuc2l6ZSA9PT0gTEVHQUNZX0FSRUFfT1JERVIubGVuZ3RoXG5leHBvcnQgY29uc3QgY2FtcGFpZ25PcmRlckZvclNlZWQgPSAoc2VlZDogbnVtYmVyKTogQmlvbWVbXSA9PiB7XG4gIGNvbnN0IHJuZyA9IHJuZ0ZvcihzZWVkLCAncHJvZ3Jlc3Npb24nLCAnY2FtcGFpZ24tYXJlYS1vcmRlcicpXG4gIHJldHVybiBybmcuc2h1ZmZsZShbLi4ucm5nLnNodWZmbGUoWy4uLkJJT01FX1BPT0xdKS5zbGljZSgwLCA0KV0pXG59XG5leHBvcnQgY29uc3QgbmV4dEFyZWEgPSAoYmlvbWU6IEJpb21lLCBhcmVhT3JkZXI6IHJlYWRvbmx5IEJpb21lW10gPSBBUkVBX09SREVSKTogQmlvbWUgfCB1bmRlZmluZWQgPT4gYXJlYU9yZGVyW2FyZWFPcmRlci5pbmRleE9mKGJpb21lKSArIDFdXG5leHBvcnQgY29uc3QgdW5sb2NrTmV4dEFyZWEgPSAodW5sb2NrZWQ6IHJlYWRvbmx5IEJpb21lW10sIGNvbXBsZXRlZDogQmlvbWUsIGFyZWFPcmRlcjogcmVhZG9ubHkgQmlvbWVbXSA9IEFSRUFfT1JERVIpOiBCaW9tZVtdID0+IHtcbiAgY29uc3QgbmV4dCA9IG5leHRBcmVhKGNvbXBsZXRlZCwgYXJlYU9yZGVyKVxuICByZXR1cm4gbmV4dCAmJiAhdW5sb2NrZWQuaW5jbHVkZXMobmV4dCkgPyBbLi4udW5sb2NrZWQsIG5leHRdIDogWy4uLnVubG9ja2VkXVxufVxuXG5jb25zdCB0aWVySW5kZXggPSAodGllcjogQ2FtcGFpZ25UaWVyKTogbnVtYmVyID0+IENBTVBBSUdOX1RJRVJTLmluZGV4T2YodGllcilcbmNvbnN0IGV4cGVjdGVkQ3ljbGVFdmVudHMgPSAoY3VycmVudFRpZXI6IENhbXBhaWduVGllciwgY29tcGxldGVkVGllcnM6IHJlYWRvbmx5IENhbXBhaWduVGllcltdKTogQ2FtcGFpZ25DeWNsZVsnZXZlbnRzJ10gPT4ge1xuICBjb25zdCBjdXJyZW50ID0gdGllckluZGV4KGN1cnJlbnRUaWVyKVxuICBjb25zdCBldmVudHM6IENhbXBhaWduQ3ljbGVbJ2V2ZW50cyddID0gW3sgc2VxdWVuY2U6IDAsIHRpZXI6ICdiYXNlJywga2luZDogJ2VudGVyZWQnIH1dXG4gIGZvciAobGV0IGluZGV4ID0gMDsgaW5kZXggPCBjb21wbGV0ZWRUaWVycy5sZW5ndGg7IGluZGV4KyspIHtcbiAgICBjb25zdCB0aWVyID0gQ0FNUEFJR05fVElFUlNbaW5kZXhdIVxuICAgIGV2ZW50cy5wdXNoKHsgc2VxdWVuY2U6IGV2ZW50cy5sZW5ndGgsIHRpZXIsIGtpbmQ6ICd2aWN0b3J5JyB9KVxuICAgIGlmIChpbmRleCArIDEgPD0gY3VycmVudCkgZXZlbnRzLnB1c2goeyBzZXF1ZW5jZTogZXZlbnRzLmxlbmd0aCwgdGllcjogQ0FNUEFJR05fVElFUlNbaW5kZXggKyAxXSEsIGtpbmQ6ICdlbnRlcmVkJyB9KVxuICB9XG4gIHJldHVybiBldmVudHNcbn1cbmV4cG9ydCBjb25zdCBjYW1wYWlnbkN5Y2xlRXJyb3JzID0gKGN5Y2xlOiBDYW1wYWlnbkN5Y2xlKTogc3RyaW5nW10gPT4ge1xuICBjb25zdCBlcnJvcnM6IHN0cmluZ1tdID0gW11cbiAgY29uc3QgY3VycmVudCA9IHRpZXJJbmRleChjeWNsZS5jdXJyZW50VGllcilcbiAgaWYgKGN5Y2xlLnZlcnNpb24gIT09IDEpIGVycm9ycy5wdXNoKCd1bnN1cHBvcnRlZCBjeWNsZSB2ZXJzaW9uJylcbiAgaWYgKGN1cnJlbnQgPCAwKSBlcnJvcnMucHVzaCgnaW52YWxpZCBjdXJyZW50IHRpZXInKVxuICBjb25zdCBleHBlY3RlZENvbXBsZXRlZCA9IENBTVBBSUdOX1RJRVJTLnNsaWNlKDAsIGN5Y2xlLmNvbXBsZXRlZFRpZXJzLmxlbmd0aClcbiAgaWYgKGN5Y2xlLmNvbXBsZXRlZFRpZXJzLmxlbmd0aCA+IENBTVBBSUdOX1RJRVJTLmxlbmd0aCB8fCBjeWNsZS5jb21wbGV0ZWRUaWVycy5zb21lKCh0aWVyLCBpbmRleCkgPT4gdGllciAhPT0gZXhwZWN0ZWRDb21wbGV0ZWRbaW5kZXhdKSkgZXJyb3JzLnB1c2goJ2NvbXBsZXRlZCB0aWVycyBtdXN0IGJlIGFuIG9yZGVyZWQgcHJlZml4JylcbiAgaWYgKGN1cnJlbnQgPj0gMCAmJiBjeWNsZS5jb21wbGV0ZWRUaWVycy5sZW5ndGggIT09IGN1cnJlbnQgJiYgY3ljbGUuY29tcGxldGVkVGllcnMubGVuZ3RoICE9PSBjdXJyZW50ICsgMSkgZXJyb3JzLnB1c2goJ2N1cnJlbnQgdGllciBpcyBpbmNvbnNpc3RlbnQgd2l0aCBjb21wbGV0ZWQgdGllcnMnKVxuICBjb25zdCBjb21wbGV0ZWRDYXAgPSBjeWNsZS5jb21wbGV0ZWRUaWVycy5sZW5ndGggPT09IENBTVBBSUdOX1RJRVJTLmxlbmd0aFxuICBpZiAoY3ljbGUuY29tcGxldGVkQ2FwICE9PSBjb21wbGV0ZWRDYXAgfHwgY3ljbGUuY29tcGxldGVkQ2FwICYmIGN5Y2xlLmN1cnJlbnRUaWVyICE9PSAnbmdQbHVzUGx1cycpIGVycm9ycy5wdXNoKCdpbnZhbGlkIGNvbXBsZXRlZCBjYXAnKVxuICBjb25zdCBleHBlY3RlZEV2ZW50cyA9IGV4cGVjdGVkQ3ljbGVFdmVudHMoY3ljbGUuY3VycmVudFRpZXIsIGN5Y2xlLmNvbXBsZXRlZFRpZXJzKVxuICBpZiAoY3ljbGUuZXZlbnRzLmxlbmd0aCAhPT0gZXhwZWN0ZWRFdmVudHMubGVuZ3RoIHx8IGN5Y2xlLmV2ZW50cy5zb21lKChldmVudCwgaW5kZXgpID0+IGV2ZW50LnNlcXVlbmNlICE9PSBpbmRleCB8fCBldmVudC50aWVyICE9PSBleHBlY3RlZEV2ZW50c1tpbmRleF0/LnRpZXIgfHwgZXZlbnQua2luZCAhPT0gZXhwZWN0ZWRFdmVudHNbaW5kZXhdPy5raW5kKSkgZXJyb3JzLnB1c2goJ2ludmFsaWQgY3ljbGUgZXZlbnQgaGlzdG9yeScpXG4gIHJldHVybiBlcnJvcnNcbn1cbmV4cG9ydCBjb25zdCBhc3NlcnRDYW1wYWlnbkN5Y2xlID0gKGN5Y2xlOiBDYW1wYWlnbkN5Y2xlKTogQ2FtcGFpZ25DeWNsZSA9PiB7XG4gIGNvbnN0IGVycm9ycyA9IGNhbXBhaWduQ3ljbGVFcnJvcnMoY3ljbGUpXG4gIGlmIChlcnJvcnMubGVuZ3RoKSB0aHJvdyBuZXcgRXJyb3IoYGludmFsaWQgY2FtcGFpZ24gY3ljbGU6ICR7ZXJyb3JzLmpvaW4oJzsgJyl9YClcbiAgcmV0dXJuIGN5Y2xlXG59XG5leHBvcnQgY29uc3QgaW5pdGlhbENhbXBhaWduQ3ljbGUgPSAoKTogQ2FtcGFpZ25DeWNsZSA9PiAoeyB2ZXJzaW9uOiAxLCBjdXJyZW50VGllcjogJ2Jhc2UnLCBjb21wbGV0ZWRUaWVyczogW10sIGV2ZW50czogW3sgc2VxdWVuY2U6IDAsIHRpZXI6ICdiYXNlJywga2luZDogJ2VudGVyZWQnIH1dLCBjb21wbGV0ZWRDYXA6IGZhbHNlIH0pXG5leHBvcnQgY29uc3QgY2xvbmVDYW1wYWlnbkN5Y2xlID0gKGN5Y2xlOiBDYW1wYWlnbkN5Y2xlKTogQ2FtcGFpZ25DeWNsZSA9PiB7XG4gIGFzc2VydENhbXBhaWduQ3ljbGUoY3ljbGUpXG4gIHJldHVybiB7IHZlcnNpb246IDEsIGN1cnJlbnRUaWVyOiBjeWNsZS5jdXJyZW50VGllciwgY29tcGxldGVkVGllcnM6IFsuLi5jeWNsZS5jb21wbGV0ZWRUaWVyc10sIGV2ZW50czogY3ljbGUuZXZlbnRzLm1hcChldmVudCA9PiAoeyAuLi5ldmVudCB9KSksIGNvbXBsZXRlZENhcDogY3ljbGUuY29tcGxldGVkQ2FwIH1cbn1cbmV4cG9ydCBjb25zdCBjb21wbGV0ZUNhbXBhaWduVGllciA9IChjeWNsZTogQ2FtcGFpZ25DeWNsZSk6IENhbXBhaWduQ3ljbGUgPT4ge1xuICBhc3NlcnRDYW1wYWlnbkN5Y2xlKGN5Y2xlKVxuICBpZiAoY3ljbGUuY29tcGxldGVkVGllcnMuaW5jbHVkZXMoY3ljbGUuY3VycmVudFRpZXIpKSB0aHJvdyBuZXcgRXJyb3IoYGNhbm5vdCBjb21wbGV0ZSBjYW1wYWlnbiB0aWVyICR7Y3ljbGUuY3VycmVudFRpZXJ9OiB2aWN0b3J5IGFscmVhZHkgcmVjb3JkZWRgKVxuICBjb25zdCBjb21wbGV0ZWRUaWVycyA9IFsuLi5jeWNsZS5jb21wbGV0ZWRUaWVycywgY3ljbGUuY3VycmVudFRpZXJdXG4gIHJldHVybiBjbG9uZUNhbXBhaWduQ3ljbGUoeyB2ZXJzaW9uOiAxLCBjdXJyZW50VGllcjogY3ljbGUuY3VycmVudFRpZXIsIGNvbXBsZXRlZFRpZXJzLCBldmVudHM6IFsuLi5jeWNsZS5ldmVudHMsIHsgc2VxdWVuY2U6IGN5Y2xlLmV2ZW50cy5sZW5ndGgsIHRpZXI6IGN5Y2xlLmN1cnJlbnRUaWVyLCBraW5kOiAndmljdG9yeScgfV0sIGNvbXBsZXRlZENhcDogY3ljbGUuY3VycmVudFRpZXIgPT09ICduZ1BsdXNQbHVzJyB9KVxufVxuZXhwb3J0IGNvbnN0IGFkdmFuY2VDYW1wYWlnblRpZXIgPSAoY3ljbGU6IENhbXBhaWduQ3ljbGUpOiBDYW1wYWlnbkN5Y2xlID0+IHtcbiAgYXNzZXJ0Q2FtcGFpZ25DeWNsZShjeWNsZSlcbiAgaWYgKGN5Y2xlLmNvbXBsZXRlZENhcCkgdGhyb3cgbmV3IEVycm9yKCdjYW5ub3QgYWR2YW5jZSBjYW1wYWlnbiB0aWVyOiBORysrIGNvbXBsZXRlZCBjYXAgcmVhY2hlZCcpXG4gIGlmICghY3ljbGUuY29tcGxldGVkVGllcnMuaW5jbHVkZXMoY3ljbGUuY3VycmVudFRpZXIpKSB0aHJvdyBuZXcgRXJyb3IoYGNhbm5vdCBhZHZhbmNlIGNhbXBhaWduIHRpZXIgJHtjeWNsZS5jdXJyZW50VGllcn06IHZpY3Rvcnkgbm90IHJlY29yZGVkYClcbiAgY29uc3QgbmV4dCA9IENBTVBBSUdOX1RJRVJTW3RpZXJJbmRleChjeWNsZS5jdXJyZW50VGllcikgKyAxXVxuICBpZiAoIW5leHQpIHRocm93IG5ldyBFcnJvcignY2Fubm90IGFkdmFuY2UgY2FtcGFpZ24gdGllcjogTkcrKyBjb21wbGV0ZWQgY2FwIHJlYWNoZWQnKVxuICByZXR1cm4gY2xvbmVDYW1wYWlnbkN5Y2xlKHsgdmVyc2lvbjogMSwgY3VycmVudFRpZXI6IG5leHQsIGNvbXBsZXRlZFRpZXJzOiBbLi4uY3ljbGUuY29tcGxldGVkVGllcnNdLCBldmVudHM6IFsuLi5jeWNsZS5ldmVudHMsIHsgc2VxdWVuY2U6IGN5Y2xlLmV2ZW50cy5sZW5ndGgsIHRpZXI6IG5leHQsIGtpbmQ6ICdlbnRlcmVkJyB9XSwgY29tcGxldGVkQ2FwOiBmYWxzZSB9KVxufVxuZXhwb3J0IGNvbnN0IGNhbXBhaWduQ29udGludWF0aW9uUGVuZGluZyA9IChjeWNsZTogQ2FtcGFpZ25DeWNsZSk6IGJvb2xlYW4gPT4ge1xuICBhc3NlcnRDYW1wYWlnbkN5Y2xlKGN5Y2xlKVxuICByZXR1cm4gIWN5Y2xlLmNvbXBsZXRlZENhcCAmJiBjeWNsZS5jb21wbGV0ZWRUaWVycy5pbmNsdWRlcyhjeWNsZS5jdXJyZW50VGllcilcbn1cbmV4cG9ydCB0eXBlIENvbXBhbmlvbkNvbnRyb2xNb2RlQ2hhbmdlQ29udGV4dCA9ICdsb2RnZScgfCAnZmxvb3InIHwgJ2NvbWJhdCcgfCAnYXV0b3BsYXknIHwgJ3JlcGxheScgfCAnY29tbWFuZCdcbmV4cG9ydCBpbnRlcmZhY2UgQ29tcGFuaW9uQ29udHJvbE1vZGVNdXRhdGlvbiB7IGNoYW5nZWQ6IGJvb2xlYW47IG1lc3NhZ2U6IHN0cmluZzsgc3RhdGU6IENhbXBhaWduUm91dGVTdGF0ZSB9XG5jb25zdCBjbG9uZUNvbXBhbmlvbkNvbnRyb2xIaXN0b3J5ID0gKGhpc3Rvcnk6IHJlYWRvbmx5IENvbXBhbmlvbkNvbnRyb2xNb2RlRXZlbnRbXSk6IENvbXBhbmlvbkNvbnRyb2xNb2RlRXZlbnRbXSA9PiBoaXN0b3J5Lm1hcChldmVudCA9PiAoeyAuLi5ldmVudCB9KSlcbmNvbnN0IGNvbXBhbmlvbnNGb3JDb250cm9sTW9kZSA9IChjb21wYW5pb25zOiBDYW1wYWlnblJvdXRlU3RhdGVbJ2NvbXBhbmlvbnMnXSwgcmVzY3VlczogQ2FtcGFpZ25Sb3V0ZVN0YXRlWydyZXNjdWVkTnBjcyddLCBtb2RlOiBDb21wYW5pb25Db250cm9sTW9kZSk6IENhbXBhaWduUm91dGVTdGF0ZVsnY29tcGFuaW9ucyddID0+IGNsb25lQ29tcGFuaW9ucyhjb21wYW5pb25zLCByZXNjdWVzKS5tYXAoY29tcGFuaW9uID0+ICh7IC4uLmNvbXBhbmlvbiwgY29udHJvbE1vZGU6IG1vZGUgfSkpXG5jb25zdCBjbG9uZUNvbXBhbmlvbkNvbnRyb2wgPSAoc3RhdGU6IENhbXBhaWduUm91dGVTdGF0ZSk6IFBpY2s8Q2FtcGFpZ25Sb3V0ZVN0YXRlLCAnY29tcGFuaW9uQ29udHJvbE1vZGUnIHwgJ2NvbXBhbmlvbkNvbnRyb2xIaXN0b3J5JyB8ICdjb21wYW5pb25zJz4gPT4gKHsgY29tcGFuaW9uQ29udHJvbE1vZGU6IHN0YXRlLmNvbXBhbmlvbkNvbnRyb2xNb2RlLCBjb21wYW5pb25Db250cm9sSGlzdG9yeTogY2xvbmVDb21wYW5pb25Db250cm9sSGlzdG9yeShzdGF0ZS5jb21wYW5pb25Db250cm9sSGlzdG9yeSksIGNvbXBhbmlvbnM6IGNvbXBhbmlvbnNGb3JDb250cm9sTW9kZShzdGF0ZS5jb21wYW5pb25zLCBzdGF0ZS5yZXNjdWVkTnBjcywgc3RhdGUuY29tcGFuaW9uQ29udHJvbE1vZGUpIH0pXG5leHBvcnQgY29uc3QgY2hhbmdlQ2FtcGFpZ25Db21wYW5pb25Db250cm9sTW9kZSA9IChzdGF0ZTogQ2FtcGFpZ25Sb3V0ZVN0YXRlLCBtb2RlOiBDb21wYW5pb25Db250cm9sTW9kZSwgY29udGV4dDogQ29tcGFuaW9uQ29udHJvbE1vZGVDaGFuZ2VDb250ZXh0KTogQ29tcGFuaW9uQ29udHJvbE1vZGVNdXRhdGlvbiA9PiB7XG4gIGNvbnN0IGN1cnJlbnQgPSBjbG9uZUNvbXBhbmlvbkNvbnRyb2woc3RhdGUpXG4gIGlmIChjb250ZXh0ICE9PSAnbG9kZ2UnKSByZXR1cm4geyBjaGFuZ2VkOiBmYWxzZSwgbWVzc2FnZTogJ0NvbXBhbmlvbiBjb250cm9sIGNhbiBjaGFuZ2Ugb25seSBhdCB0aGUgTG9kZ2UgYmV0d2VlbiBmbG9vcnMuJywgc3RhdGU6IHsgLi4uc3RhdGUsIC4uLmN1cnJlbnQgfSB9XG4gIGlmIChtb2RlID09PSBzdGF0ZS5jb21wYW5pb25Db250cm9sTW9kZSkgcmV0dXJuIHsgY2hhbmdlZDogZmFsc2UsIG1lc3NhZ2U6IGBDb21wYW5pb24gY29udHJvbCBpcyBhbHJlYWR5ICR7bW9kZX0uYCwgc3RhdGU6IHsgLi4uc3RhdGUsIC4uLmN1cnJlbnQgfSB9XG4gIGNvbnN0IGNvbXBhbmlvbkNvbnRyb2xIaXN0b3J5ID0gWy4uLmN1cnJlbnQuY29tcGFuaW9uQ29udHJvbEhpc3RvcnksIHsgc2VxdWVuY2U6IGN1cnJlbnQuY29tcGFuaW9uQ29udHJvbEhpc3RvcnkubGVuZ3RoLCBtb2RlLCBzb3VyY2U6ICdsb2RnZScgYXMgY29uc3QgfV1cbiAgcmV0dXJuIHsgY2hhbmdlZDogdHJ1ZSwgbWVzc2FnZTogYENvbXBhbmlvbiBjb250cm9sIGNoYW5nZWQgdG8gJHttb2RlfS5gLCBzdGF0ZTogeyAuLi5zdGF0ZSwgY29tcGFuaW9uQ29udHJvbE1vZGU6IG1vZGUsIGNvbXBhbmlvbkNvbnRyb2xIaXN0b3J5LCBjb21wYW5pb25zOiBjb21wYW5pb25zRm9yQ29udHJvbE1vZGUoc3RhdGUuY29tcGFuaW9ucywgc3RhdGUucmVzY3VlZE5wY3MsIG1vZGUpIH0gfVxufVxuZXhwb3J0IGNvbnN0IGNvbnRpbnVlQ2FtcGFpZ25Sb3V0ZSA9IChzdGF0ZTogQ2FtcGFpZ25Sb3V0ZVN0YXRlKTogQ2FtcGFpZ25Sb3V0ZVN0YXRlID0+IHtcbiAgY29uc3QgY3ljbGUgPSBjbG9uZUNhbXBhaWduQ3ljbGUoc3RhdGUuY3ljbGUpXG4gIGNvbnN0IGNvbXBhbmlvbkNvbnRyb2wgPSBjbG9uZUNvbXBhbmlvbkNvbnRyb2woc3RhdGUpXG4gIGlmIChjeWNsZS5jb21wbGV0ZWRDYXApIHRocm93IG5ldyBFcnJvcignY2Fubm90IGNvbnRpbnVlIGNhbXBhaWduOiBORysrIGNvbXBsZXRlZCBjYXAgcmVhY2hlZCcpXG4gIGlmICghY2FtcGFpZ25Db250aW51YXRpb25QZW5kaW5nKGN5Y2xlKSkgcmV0dXJuIHsgLi4uc3RhdGUsIGFyZWFPcmRlcjogWy4uLnN0YXRlLmFyZWFPcmRlcl0sIGNvbXBsZXRlZEFyZWFzOiBbLi4uc3RhdGUuY29tcGxldGVkQXJlYXNdLCB1bmxvY2tlZEFyZWFzOiBbLi4uc3RhdGUudW5sb2NrZWRBcmVhc10sIHJlc2N1ZWROcGNzOiBzdGF0ZS5yZXNjdWVkTnBjcy5tYXAobnBjID0+ICh7IC4uLm5wYyB9KSksIC4uLmNvbXBhbmlvbkNvbnRyb2wsIGNhcnJ5b3ZlckRpYWdub3N0aWNzOiBjbG9uZUNhcnJ5b3ZlckRpYWdub3N0aWNzKHN0YXRlLmNhcnJ5b3ZlckRpYWdub3N0aWNzKSwgbGluZWFnZUV2ZW50czogc3RhdGUubGluZWFnZUV2ZW50cy5tYXAoZXZlbnQgPT4gKHsgLi4uZXZlbnQgfSkpLCBsZWdhY3lSZWNvcmRzOiBzdGF0ZS5sZWdhY3lSZWNvcmRzLm1hcChyZWNvcmQgPT4gKHsgLi4ucmVjb3JkIH0pKSwgYWxpZ25tZW50OiB7IC4uLnN0YXRlLmFsaWdubWVudCB9LCByZXB1dGF0aW9uOiB7IHRyYWlsZm9sazogc3RhdGUucmVwdXRhdGlvbj8udHJhaWxmb2xrID8/IDAsIGthbWk6IHN0YXRlLnJlcHV0YXRpb24/LmthbWkgPz8gMCB9LCBjeWNsZSB9XG4gIGNvbnN0IHNlbGVjdGVkQmlvbWUgPSBzdGF0ZS5hcmVhT3JkZXJbMF1cbiAgaWYgKCFzZWxlY3RlZEJpb21lKSB0aHJvdyBuZXcgRXJyb3IoJ2Nhbm5vdCBjb250aW51ZSBjYW1wYWlnbjogbWlzc2luZyBhcmVhIG9yZGVyJylcbiAgcmV0dXJuIHsgLi4uc3RhdGUsIGFyZWFPcmRlcjogWy4uLnN0YXRlLmFyZWFPcmRlcl0sIGNvbXBsZXRlZEFyZWFzOiBbXSwgdW5sb2NrZWRBcmVhczogW3NlbGVjdGVkQmlvbWVdLCBzZWxlY3RlZEJpb21lLCByZXNjdWVkTnBjczogc3RhdGUucmVzY3VlZE5wY3MubWFwKG5wYyA9PiAoeyAuLi5ucGMgfSkpLCAuLi5jb21wYW5pb25Db250cm9sLCBjYXJyeW92ZXJEaWFnbm9zdGljczogY2xvbmVDYXJyeW92ZXJEaWFnbm9zdGljcyhzdGF0ZS5jYXJyeW92ZXJEaWFnbm9zdGljcyksIGxpbmVhZ2VFdmVudHM6IHN0YXRlLmxpbmVhZ2VFdmVudHMubWFwKGV2ZW50ID0+ICh7IC4uLmV2ZW50IH0pKSwgbGVnYWN5UmVjb3Jkczogc3RhdGUubGVnYWN5UmVjb3Jkcy5tYXAocmVjb3JkID0+ICh7IC4uLnJlY29yZCB9KSksIGFsaWdubWVudDogeyAuLi5zdGF0ZS5hbGlnbm1lbnQgfSwgcmVwdXRhdGlvbjogeyB0cmFpbGZvbGs6IHN0YXRlLnJlcHV0YXRpb24/LnRyYWlsZm9sayA/PyAwLCBrYW1pOiBzdGF0ZS5yZXB1dGF0aW9uPy5rYW1pID8/IDAgfSwgY3ljbGU6IGFkdmFuY2VDYW1wYWlnblRpZXIoY3ljbGUpIH1cbn1cblxuZXhwb3J0IGNvbnN0IGluaXRpYWxDYW1wYWlnblJvdXRlID0gKHNlZWQ/OiBudW1iZXIsIGNvbXBhbmlvbkNvbnRyb2xNb2RlOiBDb21wYW5pb25Db250cm9sTW9kZSA9ICdhdXRvbm9tb3VzJyk6IENhbXBhaWduUm91dGVTdGF0ZSA9PiB7XG4gIGNvbnN0IGFyZWFPcmRlciA9IHNlZWQgPT09IHVuZGVmaW5lZCA/IFsuLi5ERUZBVUxUX0FSRUFfT1JERVJdIDogY2FtcGFpZ25PcmRlckZvclNlZWQoc2VlZClcbiAgcmV0dXJuIHsgdmVyc2lvbjogNSwgYXJlYU9yZGVyLCBjb21wbGV0ZWRBcmVhczogW10sIHVubG9ja2VkQXJlYXM6IFthcmVhT3JkZXJbMF1dLCBzZWxlY3RlZEJpb21lOiBhcmVhT3JkZXJbMF0sIHJlc2N1ZWROcGNzOiBbXSwgY29tcGFuaW9uczogW10sIGNvbXBhbmlvbkNvbnRyb2xNb2RlLCBjb21wYW5pb25Db250cm9sSGlzdG9yeTogW3sgc2VxdWVuY2U6IDAsIG1vZGU6IGNvbXBhbmlvbkNvbnRyb2xNb2RlLCBzb3VyY2U6ICdjcmVhdGlvbicgfV0sIGNhcnJ5b3ZlckRpYWdub3N0aWNzOiBbXSwgbGluZWFnZUV2ZW50czogW10sIGxlZ2FjeVJlY29yZHM6IFtdLCBhbGlnbm1lbnQ6IHsga2FtaTogMCwgdmlsbGFnZVBhY3Q6IDAgfSwgcmVwdXRhdGlvbjogeyB0cmFpbGZvbGs6IDAsIGthbWk6IDAgfSwgY3ljbGU6IGluaXRpYWxDYW1wYWlnbkN5Y2xlKCkgfVxufVxuZXhwb3J0IGNvbnN0IGNvbXBsZXRlQ2FtcGFpZ25BcmVhID0gKHN0YXRlOiBDYW1wYWlnblJvdXRlU3RhdGUsIGNvbXBsZXRlZDogQmlvbWUpOiBDYW1wYWlnblJvdXRlU3RhdGUgPT4gKHsgLi4uc3RhdGUsIGFyZWFPcmRlcjogWy4uLnN0YXRlLmFyZWFPcmRlcl0sIGNvbXBsZXRlZEFyZWFzOiBzdGF0ZS5jb21wbGV0ZWRBcmVhcy5pbmNsdWRlcyhjb21wbGV0ZWQpID8gWy4uLnN0YXRlLmNvbXBsZXRlZEFyZWFzXSA6IFsuLi5zdGF0ZS5jb21wbGV0ZWRBcmVhcywgY29tcGxldGVkXSwgdW5sb2NrZWRBcmVhczogWy4uLnN0YXRlLnVubG9ja2VkQXJlYXNdLCBzZWxlY3RlZEJpb21lOiBjb21wbGV0ZWQsIHJlc2N1ZWROcGNzOiBbLi4uc3RhdGUucmVzY3VlZE5wY3NdLCAuLi5jbG9uZUNvbXBhbmlvbkNvbnRyb2woc3RhdGUpLCBjYXJyeW92ZXJEaWFnbm9zdGljczogY2xvbmVDYXJyeW92ZXJEaWFnbm9zdGljcyhzdGF0ZS5jYXJyeW92ZXJEaWFnbm9zdGljcyksIGxpbmVhZ2VFdmVudHM6IFsuLi5zdGF0ZS5saW5lYWdlRXZlbnRzXSwgbGVnYWN5UmVjb3JkczogWy4uLnN0YXRlLmxlZ2FjeVJlY29yZHNdLCBhbGlnbm1lbnQ6IHsgLi4uc3RhdGUuYWxpZ25tZW50IH0sIHJlcHV0YXRpb246IHsgdHJhaWxmb2xrOiBzdGF0ZS5yZXB1dGF0aW9uPy50cmFpbGZvbGsgPz8gMCwga2FtaTogc3RhdGUucmVwdXRhdGlvbj8ua2FtaSA/PyAwIH0sIGN5Y2xlOiBjbG9uZUNhbXBhaWduQ3ljbGUoc3RhdGUuY3ljbGUpIH0pXG5leHBvcnQgY29uc3QgdW5sb2NrQ2FtcGFpZ25BcmVhID0gKHN0YXRlOiBDYW1wYWlnblJvdXRlU3RhdGUsIGJpb21lOiBCaW9tZSk6IENhbXBhaWduUm91dGVTdGF0ZSA9PiAoeyAuLi5zdGF0ZSwgYXJlYU9yZGVyOiBbLi4uc3RhdGUuYXJlYU9yZGVyXSwgY29tcGxldGVkQXJlYXM6IFsuLi5zdGF0ZS5jb21wbGV0ZWRBcmVhc10sIHVubG9ja2VkQXJlYXM6IHN0YXRlLnVubG9ja2VkQXJlYXMuaW5jbHVkZXMoYmlvbWUpID8gWy4uLnN0YXRlLnVubG9ja2VkQXJlYXNdIDogWy4uLnN0YXRlLnVubG9ja2VkQXJlYXMsIGJpb21lXSwgc2VsZWN0ZWRCaW9tZTogYmlvbWUsIHJlc2N1ZWROcGNzOiBbLi4uc3RhdGUucmVzY3VlZE5wY3NdLCAuLi5jbG9uZUNvbXBhbmlvbkNvbnRyb2woc3RhdGUpLCBjYXJyeW92ZXJEaWFnbm9zdGljczogY2xvbmVDYXJyeW92ZXJEaWFnbm9zdGljcyhzdGF0ZS5jYXJyeW92ZXJEaWFnbm9zdGljcyksIGxpbmVhZ2VFdmVudHM6IFsuLi5zdGF0ZS5saW5lYWdlRXZlbnRzXSwgbGVnYWN5UmVjb3JkczogWy4uLnN0YXRlLmxlZ2FjeVJlY29yZHNdLCBhbGlnbm1lbnQ6IHsgLi4uc3RhdGUuYWxpZ25tZW50IH0sIHJlcHV0YXRpb246IHsgdHJhaWxmb2xrOiBzdGF0ZS5yZXB1dGF0aW9uPy50cmFpbGZvbGsgPz8gMCwga2FtaTogc3RhdGUucmVwdXRhdGlvbj8ua2FtaSA/PyAwIH0sIGN5Y2xlOiBjbG9uZUNhbXBhaWduQ3ljbGUoc3RhdGUuY3ljbGUpIH0pXG5leHBvcnQgY29uc3QgcmVjb3JkQ2FtcGFpZ25TYWNyaWZpY2UgPSAoc3RhdGU6IENhbXBhaWduUm91dGVTdGF0ZSwgZXZlbnQ6IExpbmVhZ2VFdmVudCk6IENhbXBhaWduUm91dGVTdGF0ZSA9PiB7XG4gIGNvbnN0IHJlc2N1ZWROcGNzID0gc3RhdGUucmVzY3VlZE5wY3MuZmlsdGVyKG5wYyA9PiBucGMuaWQgIT09IGV2ZW50Lm5wY0lkKVxuICByZXR1cm4geyAuLi5zdGF0ZSwgcmVzY3VlZE5wY3MsIGNvbXBhbmlvbkNvbnRyb2xNb2RlOiBzdGF0ZS5jb21wYW5pb25Db250cm9sTW9kZSwgY29tcGFuaW9uQ29udHJvbEhpc3Rvcnk6IGNsb25lQ29tcGFuaW9uQ29udHJvbEhpc3Rvcnkoc3RhdGUuY29tcGFuaW9uQ29udHJvbEhpc3RvcnkpLCBjb21wYW5pb25zOiBjb21wYW5pb25zRm9yQ29udHJvbE1vZGUoc3RhdGUuY29tcGFuaW9ucy5tYXAoY29tcGFuaW9uID0+IGxvc2VDb21wYW5pb25Gb3JSZXNjdWUoY29tcGFuaW9uLCBldmVudC5ucGNJZCkpLCByZXNjdWVkTnBjcywgc3RhdGUuY29tcGFuaW9uQ29udHJvbE1vZGUpLCBjYXJyeW92ZXJEaWFnbm9zdGljczogY2xvbmVDYXJyeW92ZXJEaWFnbm9zdGljcyhzdGF0ZS5jYXJyeW92ZXJEaWFnbm9zdGljcyksIGxpbmVhZ2VFdmVudHM6IHN0YXRlLmxpbmVhZ2VFdmVudHMuc29tZShleGlzdGluZyA9PiBleGlzdGluZy5pZCA9PT0gZXZlbnQuaWQpID8gWy4uLnN0YXRlLmxpbmVhZ2VFdmVudHNdIDogWy4uLnN0YXRlLmxpbmVhZ2VFdmVudHMsIGV2ZW50XS5zbGljZSgtMTIpLCBhbGlnbm1lbnQ6IHsgLi4uc3RhdGUuYWxpZ25tZW50IH0sIHJlcHV0YXRpb246IHsgdHJhaWxmb2xrOiBzdGF0ZS5yZXB1dGF0aW9uPy50cmFpbGZvbGsgPz8gMCwga2FtaTogc3RhdGUucmVwdXRhdGlvbj8ua2FtaSA/PyAwIH0sIGN5Y2xlOiBjbG9uZUNhbXBhaWduQ3ljbGUoc3RhdGUuY3ljbGUpIH1cbn1cbmV4cG9ydCBjb25zdCBhcHBlbmRMZWdhY3lSZWNvcmQgPSAoc3RhdGU6IENhbXBhaWduUm91dGVTdGF0ZSwgcmVjb3JkOiBMZWdhY3lSZWNvcmQpOiBDYW1wYWlnblJvdXRlU3RhdGUgPT4gKHsgLi4uc3RhdGUsIC4uLmNsb25lQ29tcGFuaW9uQ29udHJvbChzdGF0ZSksIGNhcnJ5b3ZlckRpYWdub3N0aWNzOiBjbG9uZUNhcnJ5b3ZlckRpYWdub3N0aWNzKHN0YXRlLmNhcnJ5b3ZlckRpYWdub3N0aWNzKSwgbGVnYWN5UmVjb3JkczogWy4uLnN0YXRlLmxlZ2FjeVJlY29yZHMsIHsgLi4ucmVjb3JkIH1dLnNsaWNlKC0xMiksIGFsaWdubWVudDogeyAuLi5zdGF0ZS5hbGlnbm1lbnQgfSwgcmVwdXRhdGlvbjogeyB0cmFpbGZvbGs6IHN0YXRlLnJlcHV0YXRpb24/LnRyYWlsZm9sayA/PyAwLCBrYW1pOiBzdGF0ZS5yZXB1dGF0aW9uPy5rYW1pID8/IDAgfSwgY3ljbGU6IGNsb25lQ2FtcGFpZ25DeWNsZShzdGF0ZS5jeWNsZSkgfSlcbmV4cG9ydCBjb25zdCBhZGRBbGlnbm1lbnQgPSAoc3RhdGU6IENhbXBhaWduUm91dGVTdGF0ZSwgYWxpZ25tZW50OiBBbGlnbm1lbnQpOiBDYW1wYWlnblJvdXRlU3RhdGUgPT4gKHsgLi4uc3RhdGUsIC4uLmNsb25lQ29tcGFuaW9uQ29udHJvbChzdGF0ZSksIGNhcnJ5b3ZlckRpYWdub3N0aWNzOiBjbG9uZUNhcnJ5b3ZlckRpYWdub3N0aWNzKHN0YXRlLmNhcnJ5b3ZlckRpYWdub3N0aWNzKSwgYWxpZ25tZW50OiB7IC4uLnN0YXRlLmFsaWdubWVudCwgW2FsaWdubWVudF06IHN0YXRlLmFsaWdubWVudFthbGlnbm1lbnRdICsgMSB9LCBjeWNsZTogY2xvbmVDYW1wYWlnbkN5Y2xlKHN0YXRlLmN5Y2xlKSB9KVxuIl0sIm1hcHBpbmdzIjoiQUFDQSxTQUFTQSxNQUFNLFFBQVEsUUFBUTtBQUMvQixTQUFTQyxlQUFlLEVBQUVDLHNCQUFzQixRQUFRLGNBQWM7QUFDdEUsU0FBU0MseUJBQXlCLFFBQVEsYUFBYTtBQUV2RCxPQUFPLE1BQU1DLFVBQVUsR0FBRyxDQUFDLE1BQU0sRUFBRSxPQUFPLEVBQUUsU0FBUyxFQUFFLE9BQU8sRUFBRSxTQUFTLEVBQUUsY0FBYyxFQUFFLFFBQVEsRUFBRSxRQUFRLEVBQUUsV0FBVyxFQUFFLGdCQUFnQixDQUFxQztBQUNqTCxPQUFPLE1BQU1DLFVBQVUsR0FBR0QsVUFBVTtBQUNwQyxPQUFPLE1BQU1FLGlCQUFpQixHQUFHLENBQUMsTUFBTSxFQUFFLE9BQU8sRUFBRSxTQUFTLEVBQUUsT0FBTyxFQUFFLFNB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