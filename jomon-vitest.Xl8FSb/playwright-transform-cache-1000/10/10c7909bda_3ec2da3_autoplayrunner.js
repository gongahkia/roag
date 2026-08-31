// d67eb97216a2791856901074f897c46359c8019d
import { AUTOPLAY_MAX_TURNS, autoplayDecision, autoplayRecoveryFingerprint, autoplayStateFingerprint, autoplayTraceFingerprint, createAutoplayContext, recordAutoplayTransitionSnapshot, snapshotAutoplayTransition } from './autoplay';
import { newRun, perform } from './engine';
import { AREA_ORDER, nextArea } from './engine/campaign';
import { createPolicyProfile, createPolicyRunMetadata, scorePolicyEpisode } from './autoplay-policy';
import { autoplayHeuristicProfile, autoplayHeuristicProfileRef, parseAutoplayHeuristicProfile } from './autoplay-heuristics';
import { autoplayDirectCompanionIds, createAutoplayPartyOutcomes, recordAutoplayPartyOutcome } from './autoplay-party';
import { appendPolicyFeatureHistory, encodePolicyFeatures } from './autoplay-features';
import { createAutoplayTraceDocument, createAutoplayTraceEpisode, createAutoplayTraceRecord, observeAutoplayTrace } from './autoplay-trace';
import { observeTelemetryTurn, telemetrySnapshot } from './telemetry';
import { eventLabel } from './engine/shared';
import { campaignDifficultyPackageMetadata } from './campaign-difficulty';
import { getTile } from './world';
import { DIRECTIONS } from './types';
export const isCompleteCampaign = (outcome, completedAreas, areaOrder = AREA_ORDER) => outcome === 'complete' && completedAreas.length === areaOrder.length && completedAreas.every((biome, index) => biome === areaOrder[index]);
export const autoplayReplayMetadata = (state, initialHero = state.replayHero) => {
  var _state$areaFloor, _state$areaOrder, _state$companions, _state$companionDeath;
  const floor = state.floor;
  const areaFloor = (_state$areaFloor = state.areaFloor) !== null && _state$areaFloor !== void 0 ? _state$areaFloor : floor.index % 4;
  const escalation = floor.escalation ? `${floor.escalation.arcId}:${floor.escalation.phase}` : undefined;
  const recipeId = floor.layoutId.replace(/-remix$/, '');
  const difficultyPackage = state.campaignCycle ? campaignDifficultyPackageMetadata(state.campaignCycle) : undefined;
  return {
    seed: state.seed,
    biome: floor.biome,
    areaFloor,
    floorIndex: floor.index,
    layoutId: floor.layoutId,
    macroRecipeId: `${floor.biome}:${recipeId}`,
    routeContractId: `route:${floor.biome}:${floor.index}:${floor.layoutId}:${escalation !== null && escalation !== void 0 ? escalation : 'legacy'}`,
    objectiveId: floor.objective.id,
    ...(escalation ? {
      escalation
    } : {}),
    ...(state.campaignCycle ? {
      campaignCycle: structuredClone(state.campaignCycle)
    } : {}),
    ...(difficultyPackage ? {
      difficultyPackage
    } : {}),
    ...(floor.difficulty ? {
      difficulty: structuredClone(floor.difficulty)
    } : {}),
    ...(initialHero ? {
      initialHero: structuredClone(initialHero),
      areaOrder: [...((_state$areaOrder = state.areaOrder) !== null && _state$areaOrder !== void 0 ? _state$areaOrder : [])]
    } : {}),
    ...((_state$companions = state.companions) !== null && _state$companions !== void 0 && _state$companions.length ? {
      companions: structuredClone(state.companions)
    } : {}),
    companionDeathMode: (_state$companionDeath = state.companionDeathMode) !== null && _state$companionDeath !== void 0 ? _state$companionDeath : 'injury'
  };
};
const fingerprint = state => {
  var _state$hero$relics, _state$companionDeath2;
  return JSON.stringify({
    status: state.status,
    turn: state.turn,
    floor: state.floor.index,
    hero: {
      x: state.hero.x,
      y: state.hero.y,
      health: state.hero.health,
      focus: state.hero.focus,
      gold: state.hero.gold,
      bombs: state.hero.bombs,
      ropes: state.hero.ropes,
      keys: state.hero.keys,
      xp: state.hero.xp,
      level: state.hero.level,
      skills: [...state.hero.skills].sort(),
      inventory: [...state.hero.inventory],
      equipment: state.hero.equipment,
      relics: [...((_state$hero$relics = state.hero.relics) !== null && _state$hero$relics !== void 0 ? _state$hero$relics : [])],
      relicCharges: state.hero.relicCharges
    },
    objective: state.floor.objective,
    guardianDefeated: state.floor.guardianDefeated,
    actors: state.floor.actors.filter(actor => actor.health > 0).map(actor => ({
      id: actor.id,
      x: actor.x,
      y: actor.y,
      health: actor.health
    })).sort((a, b) => a.id.localeCompare(b.id)),
    companionDeathMode: (_state$companionDeath2 = state.companionDeathMode) !== null && _state$companionDeath2 !== void 0 ? _state$companionDeath2 : 'injury',
    items: state.floor.items.map(item => ({
      id: item.id,
      x: item.x,
      y: item.y,
      count: item.count
    })).sort((a, b) => `${a.x},${a.y},${a.id}`.localeCompare(`${b.x},${b.y},${b.id}`))
  });
};
const exitPathState = state => {
  const blocked = new Set(['wall', 'lava', 'pit', 'rubble', 'bramble', 'crate', 'chest', 'deepWater', 'breakwall', 'cliffWall']);
  const key = (x, y) => `${x},${y}`;
  const reachesExit = blockActors => {
    const start = {
      x: state.hero.x,
      y: state.hero.y
    };
    const queue = [start];
    const seen = new Set([key(start.x, start.y)]);
    while (queue.length) {
      const point = queue.shift();
      if (point.x === state.floor.exit.x && point.y === state.floor.exit.y) return true;
      for (const delta of Object.values(DIRECTIONS)) {
        const x = point.x + delta.x;
        const y = point.y + delta.y;
        const pointKey = key(x, y);
        const tile = getTile(state.floor, x, y);
        if (seen.has(pointKey) || !tile || blocked.has(tile.kind) || tile.kind === 'lockedDoor' && state.hero.keys < 1) continue;
        if (blockActors && state.floor.actors.some(actor => actor.health > 0 && actor.x === x && actor.y === y)) continue;
        seen.add(pointKey);
        queue.push({
          x,
          y
        });
      }
    }
    return false;
  };
  if (!reachesExit(false)) return 'terrain-blocked';
  return reachesExit(true) ? 'clear' : 'actor-blocked';
};
export const runAutoplay = (input, options = {}) => {
  var _state$area, _state$areaOrder2, _options$mode, _options$policy, _options$turnLimit, _options$stalledLimit, _options$captureTrace, _state$areaFloor2, _state$area3, _ref, _stall;
  let state = structuredClone(input);
  const initialHero = state.replayHero ? structuredClone(state.replayHero) : undefined;
  const startBiome = (_state$area = state.area) !== null && _state$area !== void 0 ? _state$area : state.floor.biome;
  const areaOrder = (_state$areaOrder2 = state.areaOrder) !== null && _state$areaOrder2 !== void 0 ? _state$areaOrder2 : [...AREA_ORDER];
  const mode = (_options$mode = options.mode) !== null && _options$mode !== void 0 ? _options$mode : 'omniscient';
  const policy = (_options$policy = options.policy) !== null && _options$policy !== void 0 ? _options$policy : 'clear';
  const heuristicProfile = options.heuristicProfile ? parseAutoplayHeuristicProfile(options.heuristicProfile) : autoplayHeuristicProfile();
  const turnLimit = (_options$turnLimit = options.turnLimit) !== null && _options$turnLimit !== void 0 ? _options$turnLimit : AUTOPLAY_MAX_TURNS * AREA_ORDER.length * 12;
  const policyProfile = createPolicyProfile({
    policy,
    informationMode: mode
  });
  const stalledLimit = (_options$stalledLimit = options.stalledLimit) !== null && _options$stalledLimit !== void 0 ? _options$stalledLimit : 12;
  const captureTrace = (_options$captureTrace = options.captureTrace) !== null && _options$captureTrace !== void 0 ? _options$captureTrace : true;
  const traceLimit = options.traceLimit;
  const captureTraceDocument = options.captureTrace === true && traceLimit === undefined;
  const commands = [];
  const trace = [];
  const traceRecords = [];
  const resourceOutcomes = {
    selected: 0,
    deferred: 0,
    rejected: 0,
    projectedRouteGains: 0,
    criticalRouteSelections: 0
  };
  const toolOutcomes = {
    selected: 0,
    deferred: 0,
    rejected: 0,
    uses: 0,
    retirements: 0
  };
  const optionalOutcomes = {
    pursued: 0,
    deferred: 0,
    declined: 0,
    secrets: 0,
    shortcuts: 0
  };
  const partyOutcomes = createAutoplayPartyOutcomes(state);
  const traceEpisode = createAutoplayTraceEpisode(policyProfile, state.seed, turnLimit, heuristicProfile);
  let featureHistory = [];
  let context = createAutoplayContext();
  const completedAreas = [];
  let stalled = 0;
  let stall;
  let outcome = 'turn-limit';
  let error;
  let unsupported;
  const stallSnapshot = () => {
    var _context$visits$get, _context$recoveryVisi;
    const stateFingerprint = autoplayStateFingerprint(state);
    const recoveryKey = autoplayRecoveryFingerprint(state);
    return {
      turn: state.turn,
      fingerprint: autoplayTraceFingerprint(state),
      visits: (_context$visits$get = context.visits.get(stateFingerprint)) !== null && _context$visits$get !== void 0 ? _context$visits$get : 0,
      ...(context.lastReason ? {
        lastReason: context.lastReason
      } : {}),
      failed: [...context.failed.entries()].map(([command, count]) => ({
        command,
        count
      })).sort((a, b) => b.count - a.count || a.command.localeCompare(b.command)),
      recentPositions: [...context.recentPositions],
      guards: {
        strategicVisits: Math.max(0, ...context.strategicVisits.values()),
        noProgressTurns: context.noProgressTurns,
        noTurnCommands: context.noTurnCommands,
        loopRecoveries: context.loopRecoveries,
        recoveryVisits: (_context$recoveryVisi = context.recoveryVisits.get(recoveryKey)) !== null && _context$recoveryVisi !== void 0 ? _context$recoveryVisi : 0
      }
    };
  };
  try {
    while (state.status === 'playing' && state.turn < turnLimit) {
      var _state$hero$traversal, _traceRecords$at$hash, _traceRecords$at;
      const decision = autoplayDecision(state, mode, policy, context, heuristicProfile);
      if (!decision) {
        const companionIds = autoplayDirectCompanionIds(state);
        if (companionIds.length) {
          partyOutcomes.directModeRefused = true;
          unsupported = {
            kind: 'direct-companion-control',
            companionIds,
            message: 'Autoplay does not issue direct companion commands.'
          };
          outcome = 'unsupported';
        } else {
          var _context$lastReason;
          stall = stallSnapshot();
          outcome = (_context$lastReason = context.lastReason) !== null && _context$lastReason !== void 0 && _context$lastReason.startsWith('turn guard:') ? 'turn-limit' : 'stalled';
        }
        break;
      }
      const command = decision.command;
      for (const assessment of decision.resourceDiagnostics) {
        resourceOutcomes[assessment.disposition === 'select' ? 'selected' : assessment.disposition === 'defer' ? 'deferred' : 'rejected']++;
        if (assessment.disposition === 'select' && assessment.projectedRouteGain) resourceOutcomes.projectedRouteGains++;
        if (assessment.disposition === 'select' && assessment.knownCriticalRoute) resourceOutcomes.criticalRouteSelections++;
      }
      if (decision.reason.startsWith('tool:') || decision.reason.startsWith('wait tool cooldown:')) for (const assessment of decision.toolDiagnostics) toolOutcomes[assessment.disposition === 'select' ? 'selected' : assessment.disposition === 'defer' ? 'deferred' : 'rejected']++;
      if (decision.reason.startsWith('pursue optional') || decision.reason.startsWith('open optional')) for (const assessment of decision.optionalDiagnostics) {
        optionalOutcomes[assessment.disposition === 'pursue' ? 'pursued' : assessment.disposition === 'defer' ? 'deferred' : 'declined']++;
        if (assessment.disposition === 'pursue') optionalOutcomes[assessment.kind === 'shortcut' ? 'shortcuts' : 'secrets']++;
      }
      const before = telemetrySnapshot(state);
      const partyBefore = structuredClone(state);
      const transition = snapshotAutoplayTransition(state);
      const beforeTools = [...((_state$hero$traversal = state.hero.traversalTools) !== null && _state$hero$traversal !== void 0 ? _state$hero$traversal : [])];
      const beforeResources = {
        health: state.hero.health,
        focus: state.hero.focus,
        gold: state.hero.gold,
        bombs: state.hero.bombs,
        ropes: state.hero.ropes,
        keys: state.hero.keys
      };
      const beforeTrace = captureTrace ? {
        turn: state.turn,
        replay: autoplayReplayMetadata(state, initialHero),
        fingerprint: autoplayTraceFingerprint(state),
        x: state.hero.x,
        y: state.hero.y,
        health: state.hero.health,
        focus: state.hero.focus,
        bombs: state.hero.bombs,
        ropes: state.hero.ropes,
        keys: state.hero.keys,
        objective: state.floor.objective.status
      } : undefined;
      const traceObservation = captureTraceDocument ? observeAutoplayTrace(state, mode) : undefined;
      const traceFeatures = captureTraceDocument ? encodePolicyFeatures(state, mode, featureHistory) : undefined;
      const events = perform(state, command);
      recordAutoplayPartyOutcome(partyOutcomes, partyBefore, state, events);
      if (decision.reason.startsWith('confirm tool:')) {
        toolOutcomes.uses++;
        if (beforeTools.some(tool => {
          var _state$hero$traversal2;
          return !((_state$hero$traversal2 = state.hero.traversalTools) !== null && _state$hero$traversal2 !== void 0 ? _state$hero$traversal2 : []).includes(tool);
        })) toolOutcomes.retirements++;
      }
      observeTelemetryTurn(state, before, events, command);
      recordAutoplayTransitionSnapshot(context, transition, command, state);
      if (captureTrace) trace.push({
        turn: beforeTrace.turn,
        replay: beforeTrace.replay,
        fingerprint: beforeTrace.fingerprint,
        command,
        reason: decision.reason,
        candidates: decision.candidates,
        ...(decision.resourceDiagnostics.length ? {
          resourceDiagnostics: structuredClone(decision.resourceDiagnostics)
        } : {}),
        ...(decision.toolDiagnostics.length ? {
          toolDiagnostics: structuredClone(decision.toolDiagnostics)
        } : {}),
        ...(decision.optionalDiagnostics.length ? {
          optionalDiagnostics: structuredClone(decision.optionalDiagnostics)
        } : {}),
        events: events.map(eventLabel),
        nextFingerprint: autoplayTraceFingerprint(state),
        before: {
          x: beforeTrace.x,
          y: beforeTrace.y,
          health: beforeTrace.health,
          focus: beforeTrace.focus,
          bombs: beforeTrace.bombs,
          ropes: beforeTrace.ropes,
          objective: beforeTrace.objective
        },
        after: {
          x: state.hero.x,
          y: state.hero.y,
          health: state.hero.health,
          focus: state.hero.focus,
          bombs: state.hero.bombs,
          ropes: state.hero.ropes,
          objective: state.floor.objective.status,
          ...(state.modal ? {
            modal: state.modal.kind
          } : {})
        }
      });
      const resourceDelta = {
        health: state.hero.health - beforeResources.health,
        focus: state.hero.focus - beforeResources.focus,
        gold: state.hero.gold - beforeResources.gold,
        bombs: state.hero.bombs - beforeResources.bombs,
        ropes: state.hero.ropes - beforeResources.ropes,
        keys: state.hero.keys - beforeResources.keys
      };
      if (traceObservation && traceFeatures) traceRecords.push(createAutoplayTraceRecord({
        sequence: traceRecords.length,
        episode: traceEpisode,
        turn: beforeTrace.turn,
        replay: beforeTrace.replay,
        observation: traceObservation,
        features: traceFeatures,
        legalCandidates: decision.candidates.map(candidate => ({
          ...candidate
        })),
        ...(decision.resourceDiagnostics.length ? {
          resourceDiagnostics: structuredClone(decision.resourceDiagnostics)
        } : {}),
        ...(decision.toolDiagnostics.length ? {
          toolDiagnostics: structuredClone(decision.toolDiagnostics)
        } : {}),
        ...(decision.optionalDiagnostics.length ? {
          optionalDiagnostics: structuredClone(decision.optionalDiagnostics)
        } : {}),
        chosen: {
          command,
          reason: decision.reason
        },
        outcome: {
          events: events.map(eventLabel),
          nextFingerprint: autoplayTraceFingerprint(state),
          status: state.status
        },
        resourceDelta,
        previousHash: (_traceRecords$at$hash = (_traceRecords$at = traceRecords.at(-1)) === null || _traceRecords$at === void 0 ? void 0 : _traceRecords$at.hash) !== null && _traceRecords$at$hash !== void 0 ? _traceRecords$at$hash : null
      }));
      featureHistory = appendPolicyFeatureHistory(featureHistory, {
        turn: before.turn,
        command,
        reason: decision.reason,
        events: events.map(eventLabel),
        resourceDelta
      });
      if (traceLimit !== undefined && trace.length > traceLimit) trace.splice(0, trace.length - traceLimit);
      commands.push(command);
      if (events.some(event => event.type === 'floor')) {
        if (options.chainFloors === false) {
          outcome = 'complete';
          break;
        }
        context = createAutoplayContext();
        stalled = 0;
      }
      if (events.some(event => event.type === 'areaComplete')) {
        var _state$area2, _state$companionDeath3, _state$lineageEvents, _state$reputation;
        const completed = (_state$area2 = state.area) !== null && _state$area2 !== void 0 ? _state$area2 : state.floor.biome;
        completedAreas.push(completed);
        const successor = options.chainAreas === false ? undefined : nextArea(completed, areaOrder);
        if (!successor) {
          outcome = 'complete';
          break;
        }
        const next = newRun(state.seed, successor, 0, state.hero, state.rescuedNpcs, [], areaOrder, state.campaignCycle, state.companions, (_state$companionDeath3 = state.companionDeathMode) !== null && _state$companionDeath3 !== void 0 ? _state$companionDeath3 : 'injury');
        next.turn = state.turn;
        next.lineageEvents = structuredClone((_state$lineageEvents = state.lineageEvents) !== null && _state$lineageEvents !== void 0 ? _state$lineageEvents : []);
        next.telemetry = structuredClone(state.telemetry);
        next.reputation = {
          ...((_state$reputation = state.reputation) !== null && _state$reputation !== void 0 ? _state$reputation : {
            trailfolk: 0,
            kami: 0
          })
        };
        state = next;
        context = createAutoplayContext();
        stalled = 0;
        continue;
      }
      stalled = state.turn === before.turn ? stalled + 1 : 0;
      if (stalled >= stalledLimit) {
        stall = stallSnapshot();
        outcome = 'stalled';
        break;
      }
    }
    if (state.status === 'dead') outcome = 'dead';
  } catch (caught) {
    outcome = 'error';
    error = caught instanceof Error ? caught.message : String(caught);
  }
  const final = {
    status: state.status,
    areaFloor: (_state$areaFloor2 = state.areaFloor) !== null && _state$areaFloor2 !== void 0 ? _state$areaFloor2 : state.floor.index % 4,
    hero: {
      x: state.hero.x,
      y: state.hero.y,
      health: state.hero.health,
      focus: state.hero.focus,
      gold: state.hero.gold,
      bombs: state.hero.bombs,
      ropes: state.hero.ropes,
      keys: state.hero.keys
    },
    exit: {
      ...state.floor.exit
    },
    objective: structuredClone(state.floor.objective),
    guardianDefeated: state.floor.guardianDefeated,
    exitPath: exitPathState(state),
    hostiles: state.floor.actors.filter(actor => actor.hostile && actor.health > 0).map(actor => ({
      id: actor.id,
      x: actor.x,
      y: actor.y,
      health: actor.health,
      ...(actor.ai ? {
        ai: actor.ai
      } : {})
    })),
    ...(state.modal ? {
      modal: state.modal.kind
    } : {})
  };
  const finalBiome = (_state$area3 = state.area) !== null && _state$area3 !== void 0 ? _state$area3 : state.floor.biome;
  const debug = {
    objectiveId: context.objectiveId,
    objectiveTarget: context.objectiveTarget,
    objectiveTargetCount: context.objectiveTargetCount,
    rejectedObjectiveTargets: [...context.rejectedObjectiveTargets].sort(),
    bestStrategicDistance: context.bestStrategicDistance,
    noProgressTurns: context.noProgressTurns,
    noTurnCommands: context.noTurnCommands,
    loopRecoveries: context.loopRecoveries,
    recentPositions: [...context.recentPositions]
  };
  const campaignComplete = isCompleteCampaign(outcome, completedAreas, areaOrder);
  const metrics = structuredClone(state.telemetry);
  const exploredTiles = state.floor.tiles.filter(tile => tile.explored).length;
  const retainedResources = state.hero.bombs + state.hero.ropes + state.hero.keys;
  const policyMetadata = createPolicyRunMetadata(policyProfile, state.seed, turnLimit, scorePolicyEpisode({
    campaignComplete,
    outcome,
    exploredTiles,
    metrics,
    retainedResources
  }));
  const traceDocument = captureTraceDocument ? createAutoplayTraceDocument(traceEpisode, traceRecords, {
    outcome,
    reason: (_ref = error !== null && error !== void 0 ? error : (_stall = stall) === null || _stall === void 0 ? void 0 : _stall.lastReason) !== null && _ref !== void 0 ? _ref : outcome === 'complete' ? 'complete' : outcome,
    turns: state.turn,
    campaignComplete,
    finalFingerprint: autoplayTraceFingerprint(state)
  }) : undefined;
  return {
    seed: state.seed,
    biome: startBiome,
    areaOrder: [...areaOrder],
    finalBiome,
    floor: state.floor.index + 1,
    mode,
    policy,
    heuristicProfile: autoplayHeuristicProfileRef(heuristicProfile),
    policyMetadata,
    outcome,
    turns: state.turn,
    commands,
    trace,
    ...(traceDocument ? {
      traceDocument
    } : {}),
    replay: autoplayReplayMetadata(state, initialHero),
    metrics,
    resourceOutcomes,
    toolOutcomes,
    optionalOutcomes,
    partyOutcomes,
    fingerprint: fingerprint(state),
    final,
    completedAreas,
    campaignComplete,
    ...(unsupported ? {
      unsupported
    } : {}),
    ...(options.includeState ? {
      state: structuredClone(state)
    } : {}),
    ...(options.includeDebug ? {
      debug
    } : {}),
    ...(stall ? {
      stall
    } : {}),
    ...(error ? {
      error
    } : {})
  };
};
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJuYW1lcyI6WyJBVVRPUExBWV9NQVhfVFVSTlMiLCJhdXRvcGxheURlY2lzaW9uIiwiYXV0b3BsYXlSZWNvdmVyeUZpbmdlcnByaW50IiwiYXV0b3BsYXlTdGF0ZUZpbmdlcnByaW50IiwiYXV0b3BsYXlUcmFjZUZpbmdlcnByaW50IiwiY3JlYXRlQXV0b3BsYXlDb250ZXh0IiwicmVjb3JkQXV0b3BsYXlUcmFuc2l0aW9uU25hcHNob3QiLCJzbmFwc2hvdEF1dG9wbGF5VHJhbnNpdGlvbiIsIm5ld1J1biIsInBlcmZvcm0iLCJBUkVBX09SREVSIiwibmV4dEFyZWEiLCJjcmVhdGVQb2xpY3lQcm9maWxlIiwiY3JlYXRlUG9saWN5UnVuTWV0YWRhdGEiLCJzY29yZVBvbGljeUVwaXNvZGUiLCJhdXRvcGxheUhldXJpc3RpY1Byb2ZpbGUiLCJhdXRvcGxheUhldXJpc3RpY1Byb2ZpbGVSZWYiLCJwYXJzZUF1dG9wbGF5SGV1cmlzdGljUHJvZmlsZSIsImF1dG9wbGF5RGlyZWN0Q29tcGFuaW9uSWRzIiwiY3JlYXRlQXV0b3BsYXlQYXJ0eU91dGNvbWVzIiwicmVjb3JkQXV0b3BsYXlQYXJ0eU91dGNvbWUiLCJhcHBlbmRQb2xpY3lGZWF0dXJlSGlzdG9yeSIsImVuY29kZVBvbGljeUZlYXR1cmVzIiwiY3JlYXRlQXV0b3BsYXlUcmFjZURvY3VtZW50IiwiY3JlYXRlQXV0b3BsYXlUcmFjZUVwaXNvZGUiLCJjcmVhdGVBdXRvcGxheVRyYWNlUmVjb3JkIiwib2JzZXJ2ZUF1dG9wbGF5VHJhY2UiLCJvYnNlcnZlVGVsZW1ldHJ5VHVybiIsInRlbGVtZXRyeVNuYXBzaG90IiwiZXZlbnRMYWJlbCIsImNhbXBhaWduRGlmZmljdWx0eVBhY2thZ2VNZXRhZGF0YSIsImdldFRpbGUiLCJESVJFQ1RJT05TIiwiaXNDb21wbGV0ZUNhbXBhaWduIiwib3V0Y29tZSIsImNvbXBsZXRlZEFyZWFzIiwiYXJlYU9yZGVyIiwibGVuZ3RoIiwiZXZlcnkiLCJiaW9tZSIsImluZGV4IiwiYXV0b3BsYXlSZXBsYXlNZXRhZGF0YSIsInN0YXRlIiwiaW5pdGlhbEhlcm8iLCJyZXBsYXlIZXJvIiwiX3N0YXRlJGFyZWFGbG9vciIsIl9zdGF0ZSRhcmVhT3JkZXIiLCJfc3RhdGUkY29tcGFuaW9ucyIsIl9zdGF0ZSRjb21wYW5pb25EZWF0aCIsImZsb29yIiwiYXJlYUZsb29yIiwiZXNjYWxhdGlvbiIsImFyY0lkIiwicGhhc2UiLCJ1bmRlZmluZWQiLCJyZWNpcGVJZCIsImxheW91dElkIiwicmVwbGFjZSIsImRpZmZpY3VsdHlQYWNrYWdlIiwiY2FtcGFpZ25DeWNsZSIsInNlZWQiLCJmbG9vckluZGV4IiwibWFjcm9SZWNpcGVJZCIsInJvdXRlQ29udHJhY3RJZCIsIm9iamVjdGl2ZUlkIiwib2JqZWN0aXZlIiwiaWQiLCJzdHJ1Y3R1cmVkQ2xvbmUiLCJkaWZmaWN1bHR5IiwiY29tcGFuaW9ucyIsImNvbXBhbmlvbkRlYXRoTW9kZSIsImZpbmdlcnByaW50IiwiX3N0YXRlJGhlcm8kcmVsaWNzIiwiX3N0YXRlJGNvbXBhbmlvbkRlYXRoMiIsIkpTT04iLCJzdHJpbmdpZnkiLCJzdGF0dXMiLCJ0dXJuIiwiaGVybyIsIngiLCJ5IiwiaGVhbHRoIiwiZm9jdXMiLCJnb2xkIiwiYm9tYnMiLCJyb3BlcyIsImtleXMiLCJ4cCIsImxldmVsIiwic2tpbGxzIiwic29ydCIsImludmVudG9yeSIsImVxdWlwbWVudCIsInJlbGljcyIsInJlbGljQ2hhcmdlcyIsImd1YXJkaWFuRGVmZWF0ZWQiLCJhY3RvcnMiLCJmaWx0ZXIiLCJhY3RvciIsIm1hcCIsImEiLCJiIiwibG9jYWxlQ29tcGFyZSIsIml0ZW1zIiwiaXRlbSIsImNvdW50IiwiZXhpdFBhdGhTdGF0ZSIsImJsb2NrZWQiLCJTZXQiLCJrZXkiLCJyZWFjaGVzRXhpdCIsImJsb2NrQWN0b3JzIiwic3RhcnQiLCJxdWV1ZSIsInNlZW4iLCJwb2ludCIsInNoaWZ0IiwiZXhpdCIsImRlbHRhIiwiT2JqZWN0IiwidmFsdWVzIiwicG9pbnRLZXkiLCJ0aWxlIiwiaGFzIiwia2luZCIsInNvbWUiLCJhZGQiLCJwdXNoIiwicnVuQXV0b3BsYXkiLCJpbnB1dCIsIm9wdGlvbnMiLCJfc3RhdGUkYXJlYSIsIl9zdGF0ZSRhcmVhT3JkZXIyIiwiX29wdGlvbnMkbW9kZSIsIl9vcHRpb25zJHBvbGljeSIsIl9vcHRpb25zJHR1cm5MaW1pdCIsIl9vcHRpb25zJHN0YWxsZWRMaW1pdCIsIl9vcHRpb25zJGNhcHR1cmVUcmFjZSIsIl9zdGF0ZSRhcmVhRmxvb3IyIiwiX3N0YXRlJGFyZWEzIiwiX3JlZiIsIl9zdGFsbCIsInN0YXJ0QmlvbWUiLCJhcmVhIiwibW9kZSIsInBvbGljeSIsImhldXJpc3RpY1Byb2ZpbGUiLCJ0dXJuTGltaXQiLCJwb2xpY3lQcm9maWxlIiwiaW5mb3JtYXRpb25Nb2RlIiwic3RhbGxlZExpbWl0IiwiY2FwdHVyZVRyYWNlIiwidHJhY2VMaW1pdCIsImNhcHR1cmVUcmFjZURvY3VtZW50IiwiY29tbWFuZHMiLCJ0cmFjZSIsInRyYWNlUmVjb3JkcyIsInJlc291cmNlT3V0Y29tZXMiLCJzZWxlY3RlZCIsImRlZmVycmVkIiwicmVqZWN0ZWQiLCJwcm9qZWN0ZWRSb3V0ZUdhaW5zIiwiY3JpdGljYWxSb3V0ZVNlbGVjdGlvbnMiLCJ0b29sT3V0Y29tZXMiLCJ1c2VzIiwicmV0aXJlbWVudHMiLCJvcHRpb25hbE91dGNvbWVzIiwicHVyc3VlZCIsImRlY2xpbmVkIiwic2VjcmV0cyIsInNob3J0Y3V0cyIsInBhcnR5T3V0Y29tZXMiLCJ0cmFjZUVwaXNvZGUiLCJmZWF0dXJlSGlzdG9yeSIsImNvbnRleHQiLCJzdGFsbGVkIiwic3RhbGwiLCJlcnJvciIsInVuc3VwcG9ydGVkIiwic3RhbGxTbmFwc2hvdCIsIl9jb250ZXh0JHZpc2l0cyRnZXQiLCJfY29udGV4dCRyZWNvdmVyeVZpc2kiLCJzdGF0ZUZpbmdlcnByaW50IiwicmVjb3ZlcnlLZXkiLCJ2aXNpdHMiLCJnZXQiLCJsYXN0UmVhc29uIiwiZmFpbGVkIiwiZW50cmllcyIsImNvbW1hbmQiLCJyZWNlbnRQb3NpdGlvbnMiLCJndWFyZHMiLCJzdHJhdGVnaWNWaXNpdHMiLCJNYXRoIiwibWF4Iiwibm9Qcm9ncmVzc1R1cm5zIiwibm9UdXJuQ29tbWFuZHMiLCJsb29wUmVjb3ZlcmllcyIsInJlY292ZXJ5VmlzaXRzIiwiX3N0YXRlJGhlcm8kdHJhdmVyc2FsIiwiX3RyYWNlUmVjb3JkcyRhdCRoYXNoIiwiX3RyYWNlUmVjb3JkcyRhdCIsImRlY2lzaW9uIiwiY29tcGFuaW9uSWRzIiwiZGlyZWN0TW9kZVJlZnVzZWQiLCJtZXNzYWdlIiwiX2NvbnRleHQkbGFzdFJlYXNvbiIsInN0YXJ0c1dpdGgiLCJhc3Nlc3NtZW50IiwicmVzb3VyY2VEaWFnbm9zdGljcyIsImRpc3Bvc2l0aW9uIiwicHJvamVjdGVkUm91dGVHYWluIiwia25vd25Dcml0aWNhbFJvdXRlIiwicmVhc29uIiwidG9vbERpYWdub3N0aWNzIiwib3B0aW9uYWxEaWFnbm9zdGljcyIsImJlZm9yZSIsInBhcnR5QmVmb3JlIiwidHJhbnNpdGlvbiIsImJlZm9yZVRvb2xzIiwidHJhdmVyc2FsVG9vbHMiLCJiZWZvcmVSZXNvdXJjZXMiLCJiZWZvcmVUcmFjZSIsInJlcGxheSIsInRyYWNlT2JzZXJ2YXRpb24iLCJ0cmFjZUZlYXR1cmVzIiwiZXZlbnRzIiwidG9vbCIsIl9zdGF0ZSRoZXJvJHRyYXZlcnNhbDIiLCJpbmNsdWRlcyIsImNhbmRpZGF0ZXMiLCJuZXh0RmluZ2VycHJpbnQiLCJhZnRlciIsIm1vZGFsIiwicmVzb3VyY2VEZWx0YSIsInNlcXVlbmNlIiwiZXBpc29kZSIsIm9ic2VydmF0aW9uIiwiZmVhdHVyZXMiLCJsZWdhbENhbmRpZGF0ZXMiLCJjYW5kaWRhdGUiLCJjaG9zZW4iLCJwcmV2aW91c0hhc2giLCJhdCIsImhhc2giLCJzcGxpY2UiLCJldmVudCIsInR5cGUiLCJjaGFpbkZsb29ycyIsIl9zdGF0ZSRhcmVhMiIsIl9zdGF0ZSRjb21wYW5pb25EZWF0aDMiLCJfc3RhdGUkbGluZWFnZUV2ZW50cyIsIl9zdGF0ZSRyZXB1dGF0aW9uIiwiY29tcGxldGVkIiwic3VjY2Vzc29yIiwiY2hhaW5BcmVhcyIsIm5leHQiLCJyZXNjdWVkTnBjcyIsImxpbmVhZ2VFdmVudHMiLCJ0ZWxlbWV0cnkiLCJyZXB1dGF0aW9uIiwidHJhaWxmb2xrIiwia2FtaSIsImNhdWdodCIsIkVycm9yIiwiU3RyaW5nIiwiZmluYWwiLCJleGl0UGF0aCIsImhvc3RpbGVzIiwiaG9zdGlsZSIsImFpIiwiZmluYWxCaW9tZSIsImRlYnVnIiwib2JqZWN0aXZlVGFyZ2V0Iiwib2JqZWN0aXZlVGFyZ2V0Q291bnQiLCJyZWplY3RlZE9iamVjdGl2ZVRhcmdldHMiLCJiZXN0U3RyYXRlZ2ljRGlzdGFuY2UiLCJjYW1wYWlnbkNvbXBsZXRlIiwibWV0cmljcyIsImV4cGxvcmVkVGlsZXMiLCJ0aWxlcyIsImV4cGxvcmVkIiwicmV0YWluZWRSZXNvdXJjZXMiLCJwb2xpY3lNZXRhZGF0YSIsInRyYWNlRG9jdW1lbnQiLCJ0dXJucyIsImZpbmFsRmluZ2VycHJpbnQiLCJpbmNsdWRlU3RhdGUiLCJpbmNsdWRlRGVidWciXSwic291cmNlcyI6WyJhdXRvcGxheS1ydW5uZXIudHMiXSwic291cmNlc0NvbnRlbnQiOlsiaW1wb3J0IHsgQVVUT1BMQVlfTUFYX1RVUk5TLCBhdXRvcGxheURlY2lzaW9uLCBhdXRvcGxheVJlY292ZXJ5RmluZ2VycHJpbnQsIGF1dG9wbGF5U3RhdGVGaW5nZXJwcmludCwgYXV0b3BsYXlUcmFjZUZpbmdlcnByaW50LCBjcmVhdGVBdXRvcGxheUNvbnRleHQsIHJlY29yZEF1dG9wbGF5VHJhbnNpdGlvblNuYXBzaG90LCBzbmFwc2hvdEF1dG9wbGF5VHJhbnNpdGlvbiB9IGZyb20gJy4vYXV0b3BsYXknXG5pbXBvcnQgeyBuZXdSdW4sIHBlcmZvcm0gfSBmcm9tICcuL2VuZ2luZSdcbmltcG9ydCB7IEFSRUFfT1JERVIsIG5leHRBcmVhIH0gZnJvbSAnLi9lbmdpbmUvY2FtcGFpZ24nXG5pbXBvcnQgeyBjcmVhdGVQb2xpY3lQcm9maWxlLCBjcmVhdGVQb2xpY3lSdW5NZXRhZGF0YSwgc2NvcmVQb2xpY3lFcGlzb2RlLCB0eXBlIFBvbGljeVJ1bk1ldGFkYXRhIH0gZnJvbSAnLi9hdXRvcGxheS1wb2xpY3knXG5pbXBvcnQgeyBhdXRvcGxheUhldXJpc3RpY1Byb2ZpbGUsIGF1dG9wbGF5SGV1cmlzdGljUHJvZmlsZVJlZiwgcGFyc2VBdXRvcGxheUhldXJpc3RpY1Byb2ZpbGUsIHR5cGUgQXV0b3BsYXlIZXVyaXN0aWNQcm9maWxlLCB0eXBlIEF1dG9wbGF5SGV1cmlzdGljUHJvZmlsZVJlZiB9IGZyb20gJy4vYXV0b3BsYXktaGV1cmlzdGljcydcbmltcG9ydCB7IGF1dG9wbGF5RGlyZWN0Q29tcGFuaW9uSWRzLCBjcmVhdGVBdXRvcGxheVBhcnR5T3V0Y29tZXMsIHJlY29yZEF1dG9wbGF5UGFydHlPdXRjb21lIH0gZnJvbSAnLi9hdXRvcGxheS1wYXJ0eSdcbmltcG9ydCB7IGFwcGVuZFBvbGljeUZlYXR1cmVIaXN0b3J5LCBlbmNvZGVQb2xpY3lGZWF0dXJlcywgdHlwZSBQb2xpY3lGZWF0dXJlSGlzdG9yeUVudHJ5IH0gZnJvbSAnLi9hdXRvcGxheS1mZWF0dXJlcydcbmltcG9ydCB7IGNyZWF0ZUF1dG9wbGF5VHJhY2VEb2N1bWVudCwgY3JlYXRlQXV0b3BsYXlUcmFjZUVwaXNvZGUsIGNyZWF0ZUF1dG9wbGF5VHJhY2VSZWNvcmQsIG9ic2VydmVBdXRvcGxheVRyYWNlLCB0eXBlIEF1dG9wbGF5VHJhY2VEb2N1bWVudCwgdHlwZSBBdXRvcGxheVRyYWNlUmVjb3JkIH0gZnJvbSAnLi9hdXRvcGxheS10cmFjZSdcbmltcG9ydCB7IG9ic2VydmVUZWxlbWV0cnlUdXJuLCB0ZWxlbWV0cnlTbmFwc2hvdCB9IGZyb20gJy4vdGVsZW1ldHJ5J1xuaW1wb3J0IHsgZXZlbnRMYWJlbCB9IGZyb20gJy4vZW5naW5lL3NoYXJlZCdcbmltcG9ydCB7IGNhbXBhaWduRGlmZmljdWx0eVBhY2thZ2VNZXRhZGF0YSB9IGZyb20gJy4vY2FtcGFpZ24tZGlmZmljdWx0eSdcbmltcG9ydCB7IGdldFRpbGUgfSBmcm9tICcuL3dvcmxkJ1xuaW1wb3J0IHsgRElSRUNUSU9OUywgdHlwZSBBdXRvcGxheU1vZGUsIHR5cGUgQXV0b3BsYXlPcHRpb25hbE91dGNvbWVzLCB0eXBlIEF1dG9wbGF5UGFydHlPdXRjb21lcywgdHlwZSBBdXRvcGxheVBvbGljeSwgdHlwZSBBdXRvcGxheVJlcGxheU1ldGFkYXRhLCB0eXBlIEF1dG9wbGF5UmVzb3VyY2VPdXRjb21lcywgdHlwZSBBdXRvcGxheVRvb2xPdXRjb21lcywgdHlwZSBBdXRvcGxheVN0YWxsLCB0eXBlIEF1dG9wbGF5VHJhY2VFbnRyeSwgdHlwZSBCaW9tZSwgdHlwZSBSdW5UZWxlbWV0cnksIHR5cGUgUnVuU3RhdGUgfSBmcm9tICcuL3R5cGVzJ1xuXG5leHBvcnQgdHlwZSBBdXRvcGxheU91dGNvbWUgPSAnY29tcGxldGUnIHwgJ2RlYWQnIHwgJ3N0YWxsZWQnIHwgJ3R1cm4tbGltaXQnIHwgJ3Vuc3VwcG9ydGVkJyB8ICdlcnJvcidcbmV4cG9ydCBpbnRlcmZhY2UgQXV0b3BsYXlSdW5PcHRpb25zIHsgbW9kZT86IEV4Y2x1ZGU8QXV0b3BsYXlNb2RlLCAnb2ZmJz47IHBvbGljeT86IEF1dG9wbGF5UG9saWN5OyBoZXVyaXN0aWNQcm9maWxlPzogQXV0b3BsYXlIZXVyaXN0aWNQcm9maWxlOyB0dXJuTGltaXQ/OiBudW1iZXI7IHN0YWxsZWRMaW1pdD86IG51bWJlcjsgY2hhaW5BcmVhcz86IGJvb2xlYW47IGNoYWluRmxvb3JzPzogYm9vbGVhbjsgY2FwdHVyZVRyYWNlPzogYm9vbGVhbjsgdHJhY2VMaW1pdD86IG51bWJlcjsgaW5jbHVkZVN0YXRlPzogYm9vbGVhbjsgaW5jbHVkZURlYnVnPzogYm9vbGVhbiB9XG5leHBvcnQgaW50ZXJmYWNlIEF1dG9wbGF5RmluYWxTdGF0ZSB7IHN0YXR1czogUnVuU3RhdGVbJ3N0YXR1cyddOyBhcmVhRmxvb3I6IG51bWJlcjsgaGVybzogeyB4OiBudW1iZXI7IHk6IG51bWJlcjsgaGVhbHRoOiBudW1iZXI7IGZvY3VzOiBudW1iZXI7IGdvbGQ6IG51bWJlcjsgYm9tYnM6IG51bWJlcjsgcm9wZXM6IG51bWJlcjsga2V5czogbnVtYmVyIH07IGV4aXQ6IHsgeDogbnVtYmVyOyB5OiBudW1iZXIgfTsgb2JqZWN0aXZlOiBSdW5TdGF0ZVsnZmxvb3InXVsnb2JqZWN0aXZlJ107IGd1YXJkaWFuRGVmZWF0ZWQ6IGJvb2xlYW47IGV4aXRQYXRoOiAnY2xlYXInIHwgJ2FjdG9yLWJsb2NrZWQnIHwgJ3RlcnJhaW4tYmxvY2tlZCc7IGhvc3RpbGVzOiBBcnJheTx7IGlkOiBzdHJpbmc7IHg6IG51bWJlcjsgeTogbnVtYmVyOyBoZWFsdGg6IG51bWJlcjsgYWk/OiBzdHJpbmcgfT47IG1vZGFsPzogc3RyaW5nIH1cbmV4cG9ydCBpbnRlcmZhY2UgQXV0b3BsYXlSZXBvcnQgeyBzZWVkOiBudW1iZXI7IGJpb21lOiBCaW9tZTsgYXJlYU9yZGVyOiBCaW9tZVtdOyBmaW5hbEJpb21lOiBCaW9tZTsgZmxvb3I6IG51bWJlcjsgbW9kZTogRXhjbHVkZTxBdXRvcGxheU1vZGUsICdvZmYnPjsgcG9saWN5OiBBdXRvcGxheVBvbGljeTsgaGV1cmlzdGljUHJvZmlsZTogQXV0b3BsYXlIZXVyaXN0aWNQcm9maWxlUmVmOyBwb2xpY3lNZXRhZGF0YTogUG9saWN5UnVuTWV0YWRhdGE7IG91dGNvbWU6IEF1dG9wbGF5T3V0Y29tZTsgdHVybnM6IG51bWJlcjsgY29tbWFuZHM6IHN0cmluZ1tdOyB0cmFjZTogQXV0b3BsYXlUcmFjZUVudHJ5W107IHRyYWNlRG9jdW1lbnQ/OiBBdXRvcGxheVRyYWNlRG9jdW1lbnQ7IHJlcGxheTogQXV0b3BsYXlSZXBsYXlNZXRhZGF0YTsgbWV0cmljczogUnVuVGVsZW1ldHJ5OyByZXNvdXJjZU91dGNvbWVzOiBBdXRvcGxheVJlc291cmNlT3V0Y29tZXM7IHRvb2xPdXRjb21lczogQXV0b3BsYXlUb29sT3V0Y29tZXM7IG9wdGlvbmFsT3V0Y29tZXM6IEF1dG9wbGF5T3B0aW9uYWxPdXRjb21lczsgcGFydHlPdXRjb21lczogQXV0b3BsYXlQYXJ0eU91dGNvbWVzOyBmaW5nZXJwcmludDogc3RyaW5nOyBmaW5hbDogQXV0b3BsYXlGaW5hbFN0YXRlOyBjb21wbGV0ZWRBcmVhczogQmlvbWVbXTsgY2FtcGFpZ25Db21wbGV0ZTogYm9vbGVhbjsgdW5zdXBwb3J0ZWQ/OiB7IGtpbmQ6ICdkaXJlY3QtY29tcGFuaW9uLWNvbnRyb2wnOyBjb21wYW5pb25JZHM6IHN0cmluZ1tdOyBtZXNzYWdlOiBzdHJpbmcgfTsgc3RhdGU/OiBSdW5TdGF0ZTsgZGVidWc/OiB7IG9iamVjdGl2ZUlkPzogc3RyaW5nOyBvYmplY3RpdmVUYXJnZXQ/OiBzdHJpbmc7IG9iamVjdGl2ZVRhcmdldENvdW50OiBudW1iZXI7IHJlamVjdGVkT2JqZWN0aXZlVGFyZ2V0czogc3RyaW5nW107IGJlc3RTdHJhdGVnaWNEaXN0YW5jZT86IG51bWJlcjsgbm9Qcm9ncmVzc1R1cm5zOiBudW1iZXI7IG5vVHVybkNvbW1hbmRzOiBudW1iZXI7IGxvb3BSZWNvdmVyaWVzOiBudW1iZXI7IHJlY2VudFBvc2l0aW9uczogc3RyaW5nW10gfTsgc3RhbGw/OiBBdXRvcGxheVN0YWxsOyBlcnJvcj86IHN0cmluZyB9XG5cbmV4cG9ydCBjb25zdCBpc0NvbXBsZXRlQ2FtcGFpZ24gPSAob3V0Y29tZTogQXV0b3BsYXlPdXRjb21lLCBjb21wbGV0ZWRBcmVhczogcmVhZG9ubHkgQmlvbWVbXSwgYXJlYU9yZGVyOiByZWFkb25seSBCaW9tZVtdID0gQVJFQV9PUkRFUik6IGJvb2xlYW4gPT4gb3V0Y29tZSA9PT0gJ2NvbXBsZXRlJyAmJiBjb21wbGV0ZWRBcmVhcy5sZW5ndGggPT09IGFyZWFPcmRlci5sZW5ndGggJiYgY29tcGxldGVkQXJlYXMuZXZlcnkoKGJpb21lLCBpbmRleCkgPT4gYmlvbWUgPT09IGFyZWFPcmRlcltpbmRleF0pXG5cbmV4cG9ydCBjb25zdCBhdXRvcGxheVJlcGxheU1ldGFkYXRhID0gKHN0YXRlOiBSdW5TdGF0ZSwgaW5pdGlhbEhlcm8gPSBzdGF0ZS5yZXBsYXlIZXJvKTogQXV0b3BsYXlSZXBsYXlNZXRhZGF0YSA9PiB7XG4gIGNvbnN0IGZsb29yID0gc3RhdGUuZmxvb3JcbiAgY29uc3QgYXJlYUZsb29yID0gc3RhdGUuYXJlYUZsb29yID8/IGZsb29yLmluZGV4ICUgNFxuICBjb25zdCBlc2NhbGF0aW9uID0gZmxvb3IuZXNjYWxhdGlvbiA/IGAke2Zsb29yLmVzY2FsYXRpb24uYXJjSWR9OiR7Zmxvb3IuZXNjYWxhdGlvbi5waGFzZX1gIDogdW5kZWZpbmVkXG4gIGNvbnN0IHJlY2lwZUlkID0gZmxvb3IubGF5b3V0SWQucmVwbGFjZSgvLXJlbWl4JC8sICcnKVxuICBjb25zdCBkaWZmaWN1bHR5UGFja2FnZSA9IHN0YXRlLmNhbXBhaWduQ3ljbGUgPyBjYW1wYWlnbkRpZmZpY3VsdHlQYWNrYWdlTWV0YWRhdGEoc3RhdGUuY2FtcGFpZ25DeWNsZSkgOiB1bmRlZmluZWRcbiAgcmV0dXJuIHtcbiAgICBzZWVkOiBzdGF0ZS5zZWVkLFxuICAgIGJpb21lOiBmbG9vci5iaW9tZSxcbiAgICBhcmVhRmxvb3IsXG4gICAgZmxvb3JJbmRleDogZmxvb3IuaW5kZXgsXG4gICAgbGF5b3V0SWQ6IGZsb29yLmxheW91dElkLFxuICAgIG1hY3JvUmVjaXBlSWQ6IGAke2Zsb29yLmJpb21lfToke3JlY2lwZUlkfWAsXG4gICAgcm91dGVDb250cmFjdElkOiBgcm91dGU6JHtmbG9vci5iaW9tZX06JHtmbG9vci5pbmRleH06JHtmbG9vci5sYXlvdXRJZH06JHtlc2NhbGF0aW9uID8/ICdsZWdhY3knfWAsXG4gICAgb2JqZWN0aXZlSWQ6IGZsb29yLm9iamVjdGl2ZS5pZCxcbiAgICAuLi4oZXNjYWxhdGlvbiA/IHsgZXNjYWxhdGlvbiB9IDoge30pLFxuICAgIC4uLihzdGF0ZS5jYW1wYWlnbkN5Y2xlID8geyBjYW1wYWlnbkN5Y2xlOiBzdHJ1Y3R1cmVkQ2xvbmUoc3RhdGUuY2FtcGFpZ25DeWNsZSkgfSA6IHt9KSxcbiAgICAuLi4oZGlmZmljdWx0eVBhY2thZ2UgPyB7IGRpZmZpY3VsdHlQYWNrYWdlIH0gOiB7fSksXG4gICAgLi4uKGZsb29yLmRpZmZpY3VsdHkgPyB7IGRpZmZpY3VsdHk6IHN0cnVjdHVyZWRDbG9uZShmbG9vci5kaWZmaWN1bHR5KSB9IDoge30pLFxuICAgIC4uLihpbml0aWFsSGVybyA/IHsgaW5pdGlhbEhlcm86IHN0cnVjdHVyZWRDbG9uZShpbml0aWFsSGVybyksIGFyZWFPcmRlcjogWy4uLihzdGF0ZS5hcmVhT3JkZXIgPz8gW10pXSB9IDoge30pLFxuICAgIC4uLihzdGF0ZS5jb21wYW5pb25zPy5sZW5ndGggPyB7IGNvbXBhbmlvbnM6IHN0cnVjdHVyZWRDbG9uZShzdGF0ZS5jb21wYW5pb25zKSB9IDoge30pLFxuICAgIGNvbXBhbmlvbkRlYXRoTW9kZTogc3RhdGUuY29tcGFuaW9uRGVhdGhNb2RlID8/ICdpbmp1cnknXG4gIH1cbn1cblxuY29uc3QgZmluZ2VycHJpbnQgPSAoc3RhdGU6IFJ1blN0YXRlKTogc3RyaW5nID0+IEpTT04uc3RyaW5naWZ5KHtcbiAgc3RhdHVzOiBzdGF0ZS5zdGF0dXMsXG4gIHR1cm46IHN0YXRlLnR1cm4sXG4gIGZsb29yOiBzdGF0ZS5mbG9vci5pbmRleCxcbiAgaGVybzogeyB4OiBzdGF0ZS5oZXJvLngsIHk6IHN0YXRlLmhlcm8ueSwgaGVhbHRoOiBzdGF0ZS5oZXJvLmhlYWx0aCwgZm9jdXM6IHN0YXRlLmhlcm8uZm9jdXMsIGdvbGQ6IHN0YXRlLmhlcm8uZ29sZCwgYm9tYnM6IHN0YXRlLmhlcm8uYm9tYnMsIHJvcGVzOiBzdGF0ZS5oZXJvLnJvcGVzLCBrZXlzOiBzdGF0ZS5oZXJvLmtleXMsIHhwOiBzdGF0ZS5oZXJvLnhwLCBsZXZlbDogc3RhdGUuaGVyby5sZXZlbCwgc2tpbGxzOiBbLi4uc3RhdGUuaGVyby5za2lsbHNdLnNvcnQoKSwgaW52ZW50b3J5OiBbLi4uc3RhdGUuaGVyby5pbnZlbnRvcnldLCBlcXVpcG1lbnQ6IHN0YXRlLmhlcm8uZXF1aXBtZW50LCByZWxpY3M6IFsuLi4oc3RhdGUuaGVyby5yZWxpY3MgPz8gW10pXSwgcmVsaWNDaGFyZ2VzOiBzdGF0ZS5oZXJvLnJlbGljQ2hhcmdlcyB9LFxuICBvYmplY3RpdmU6IHN0YXRlLmZsb29yLm9iamVjdGl2ZSxcbiAgZ3VhcmRpYW5EZWZlYXRlZDogc3RhdGUuZmxvb3IuZ3VhcmRpYW5EZWZlYXRlZCxcbiAgYWN0b3JzOiBzdGF0ZS5mbG9vci5hY3RvcnMuZmlsdGVyKGFjdG9yID0+IGFjdG9yLmhlYWx0aCA+IDApLm1hcChhY3RvciA9PiAoeyBpZDogYWN0b3IuaWQsIHg6IGFjdG9yLngsIHk6IGFjdG9yLnksIGhlYWx0aDogYWN0b3IuaGVhbHRoIH0pKS5zb3J0KChhLCBiKSA9PiBhLmlkLmxvY2FsZUNvbXBhcmUoYi5pZCkpLFxuICBjb21wYW5pb25EZWF0aE1vZGU6IHN0YXRlLmNvbXBhbmlvbkRlYXRoTW9kZSA/PyAnaW5qdXJ5JyxcbiAgaXRlbXM6IHN0YXRlLmZsb29yLml0ZW1zLm1hcChpdGVtID0+ICh7IGlkOiBpdGVtLmlkLCB4OiBpdGVtLngsIHk6IGl0ZW0ueSwgY291bnQ6IGl0ZW0uY291bnQgfSkpLnNvcnQoKGEsIGIpID0+IGAke2EueH0sJHthLnl9LCR7YS5pZH1gLmxvY2FsZUNvbXBhcmUoYCR7Yi54fSwke2IueX0sJHtiLmlkfWApKVxufSlcblxuY29uc3QgZXhpdFBhdGhTdGF0ZSA9IChzdGF0ZTogUnVuU3RhdGUpOiBBdXRvcGxheUZpbmFsU3RhdGVbJ2V4aXRQYXRoJ10gPT4ge1xuICBjb25zdCBibG9ja2VkID0gbmV3IFNldChbJ3dhbGwnLCAnbGF2YScsICdwaXQnLCAncnViYmxlJywgJ2JyYW1ibGUnLCAnY3JhdGUnLCAnY2hlc3QnLCAnZGVlcFdhdGVyJywgJ2JyZWFrd2FsbCcsICdjbGlmZldhbGwnXSlcbiAgY29uc3Qga2V5ID0gKHg6IG51bWJlciwgeTogbnVtYmVyKSA9PiBgJHt4fSwke3l9YFxuICBjb25zdCByZWFjaGVzRXhpdCA9IChibG9ja0FjdG9yczogYm9vbGVhbik6IGJvb2xlYW4gPT4ge1xuICAgIGNvbnN0IHN0YXJ0ID0geyB4OiBzdGF0ZS5oZXJvLngsIHk6IHN0YXRlLmhlcm8ueSB9XG4gICAgY29uc3QgcXVldWUgPSBbc3RhcnRdXG4gICAgY29uc3Qgc2VlbiA9IG5ldyBTZXQoW2tleShzdGFydC54LCBzdGFydC55KV0pXG4gICAgd2hpbGUgKHF1ZXVlLmxlbmd0aCkge1xuICAgICAgY29uc3QgcG9pbnQgPSBxdWV1ZS5zaGlmdCgpIVxuICAgICAgaWYgKHBvaW50LnggPT09IHN0YXRlLmZsb29yLmV4aXQueCAmJiBwb2ludC55ID09PSBzdGF0ZS5mbG9vci5leGl0LnkpIHJldHVybiB0cnVlXG4gICAgICBmb3IgKGNvbnN0IGRlbHRhIG9mIE9iamVjdC52YWx1ZXMoRElSRUNUSU9OUykpIHtcbiAgICAgICAgY29uc3QgeCA9IHBvaW50LnggKyBkZWx0YS54XG4gICAgICAgIGNvbnN0IHkgPSBwb2ludC55ICsgZGVsdGEueVxuICAgICAgICBjb25zdCBwb2ludEtleSA9IGtleSh4LCB5KVxuICAgICAgICBjb25zdCB0aWxlID0gZ2V0VGlsZShzdGF0ZS5mbG9vciwgeCwgeSlcbiAgICAgICAgaWYgKHNlZW4uaGFzKHBvaW50S2V5KSB8fCAhdGlsZSB8fCBibG9ja2VkLmhhcyh0aWxlLmtpbmQpIHx8ICh0aWxlLmtpbmQgPT09ICdsb2NrZWREb29yJyAmJiBzdGF0ZS5oZXJvLmtleXMgPCAxKSkgY29udGludWVcbiAgICAgICAgaWYgKGJsb2NrQWN0b3JzICYmIHN0YXRlLmZsb29yLmFjdG9ycy5zb21lKGFjdG9yID0+IGFjdG9yLmhlYWx0aCA+IDAgJiYgYWN0b3IueCA9PT0geCAmJiBhY3Rvci55ID09PSB5KSkgY29udGludWVcbiAgICAgICAgc2Vlbi5hZGQocG9pbnRLZXkpXG4gICAgICAgIHF1ZXVlLnB1c2goeyB4LCB5IH0pXG4gICAgICB9XG4gICAgfVxuICAgIHJldHVybiBmYWxzZVxuICB9XG4gIGlmICghcmVhY2hlc0V4aXQoZmFsc2UpKSByZXR1cm4gJ3RlcnJhaW4tYmxvY2tlZCdcbiAgcmV0dXJuIHJlYWNoZXNFeGl0KHRydWUpID8gJ2NsZWFyJyA6ICdhY3Rvci1ibG9ja2VkJ1xufVxuXG5leHBvcnQgY29uc3QgcnVuQXV0b3BsYXkgPSAoaW5wdXQ6IFJ1blN0YXRlLCBvcHRpb25zOiBBdXRvcGxheVJ1bk9wdGlvbnMgPSB7fSk6IEF1dG9wbGF5UmVwb3J0ID0+IHtcbiAgbGV0IHN0YXRlID0gc3RydWN0dXJlZENsb25lKGlucHV0KVxuICBjb25zdCBpbml0aWFsSGVybyA9IHN0YXRlLnJlcGxheUhlcm8gPyBzdHJ1Y3R1cmVkQ2xvbmUoc3RhdGUucmVwbGF5SGVybykgOiB1bmRlZmluZWRcbiAgY29uc3Qgc3RhcnRCaW9tZSA9IHN0YXRlLmFyZWEgPz8gc3RhdGUuZmxvb3IuYmlvbWVcbiAgY29uc3QgYXJlYU9yZGVyID0gc3RhdGUuYXJlYU9yZGVyID8/IFsuLi5BUkVBX09SREVSXVxuICBjb25zdCBtb2RlID0gb3B0aW9ucy5tb2RlID8/ICdvbW5pc2NpZW50J1xuICBjb25zdCBwb2xpY3kgPSBvcHRpb25zLnBvbGljeSA/PyAnY2xlYXInXG4gIGNvbnN0IGhldXJpc3RpY1Byb2ZpbGUgPSBvcHRpb25zLmhldXJpc3RpY1Byb2ZpbGUgPyBwYXJzZUF1dG9wbGF5SGV1cmlzdGljUHJvZmlsZShvcHRpb25zLmhldXJpc3RpY1Byb2ZpbGUpIDogYXV0b3BsYXlIZXVyaXN0aWNQcm9maWxlKClcbiAgY29uc3QgdHVybkxpbWl0ID0gb3B0aW9ucy50dXJuTGltaXQgPz8gQVVUT1BMQVlfTUFYX1RVUk5TICogQVJFQV9PUkRFUi5sZW5ndGggKiAxMlxuICBjb25zdCBwb2xpY3lQcm9maWxlID0gY3JlYXRlUG9saWN5UHJvZmlsZSh7IHBvbGljeSwgaW5mb3JtYXRpb25Nb2RlOiBtb2RlIH0pXG4gIGNvbnN0IHN0YWxsZWRMaW1pdCA9IG9wdGlvbnMuc3RhbGxlZExpbWl0ID8/IDEyXG4gIGNvbnN0IGNhcHR1cmVUcmFjZSA9IG9wdGlvbnMuY2FwdHVyZVRyYWNlID8/IHRydWVcbiAgY29uc3QgdHJhY2VMaW1pdCA9IG9wdGlvbnMudHJhY2VMaW1pdFxuICBjb25zdCBjYXB0dXJlVHJhY2VEb2N1bWVudCA9IG9wdGlvbnMuY2FwdHVyZVRyYWNlID09PSB0cnVlICYmIHRyYWNlTGltaXQgPT09IHVuZGVmaW5lZFxuICBjb25zdCBjb21tYW5kczogc3RyaW5nW10gPSBbXVxuICBjb25zdCB0cmFjZTogQXV0b3BsYXlUcmFjZUVudHJ5W10gPSBbXVxuICBjb25zdCB0cmFjZVJlY29yZHM6IEF1dG9wbGF5VHJhY2VSZWNvcmRbXSA9IFtdXG4gIGNvbnN0IHJlc291cmNlT3V0Y29tZXM6IEF1dG9wbGF5UmVzb3VyY2VPdXRjb21lcyA9IHsgc2VsZWN0ZWQ6IDAsIGRlZmVycmVkOiAwLCByZWplY3RlZDogMCwgcHJvamVjdGVkUm91dGVHYWluczogMCwgY3JpdGljYWxSb3V0ZVNlbGVjdGlvbnM6IDAgfVxuICBjb25zdCB0b29sT3V0Y29tZXM6IEF1dG9wbGF5VG9vbE91dGNvbWVzID0geyBzZWxlY3RlZDogMCwgZGVmZXJyZWQ6IDAsIHJlamVjdGVkOiAwLCB1c2VzOiAwLCByZXRpcmVtZW50czogMCB9XG4gIGNvbnN0IG9wdGlvbmFsT3V0Y29tZXM6IEF1dG9wbGF5T3B0aW9uYWxPdXRjb21lcyA9IHsgcHVyc3VlZDogMCwgZGVmZXJyZWQ6IDAsIGRlY2xpbmVkOiAwLCBzZWNyZXRzOiAwLCBzaG9ydGN1dHM6IDAgfVxuICBjb25zdCBwYXJ0eU91dGNvbWVzID0gY3JlYXRlQXV0b3BsYXlQYXJ0eU91dGNvbWVzKHN0YXRlKVxuICBjb25zdCB0cmFjZUVwaXNvZGUgPSBjcmVhdGVBdXRvcGxheVRyYWNlRXBpc29kZShwb2xpY3lQcm9maWxlLCBzdGF0ZS5zZWVkLCB0dXJuTGltaXQsIGhldXJpc3RpY1Byb2ZpbGUpXG4gIGxldCBmZWF0dXJlSGlzdG9yeTogUG9saWN5RmVhdHVyZUhpc3RvcnlFbnRyeVtdID0gW11cbiAgbGV0IGNvbnRleHQgPSBjcmVhdGVBdXRvcGxheUNvbnRleHQoKVxuICBjb25zdCBjb21wbGV0ZWRBcmVhczogQmlvbWVbXSA9IFtdXG4gIGxldCBzdGFsbGVkID0gMFxuICBsZXQgc3RhbGw6IEF1dG9wbGF5U3RhbGwgfCB1bmRlZmluZWRcbiAgbGV0IG91dGNvbWU6IEF1dG9wbGF5T3V0Y29tZSA9ICd0dXJuLWxpbWl0J1xuICBsZXQgZXJyb3I6IHN0cmluZyB8IHVuZGVmaW5lZFxuICBsZXQgdW5zdXBwb3J0ZWQ6IEF1dG9wbGF5UmVwb3J0Wyd1bnN1cHBvcnRlZCddXG4gIGNvbnN0IHN0YWxsU25hcHNob3QgPSAoKTogQXV0b3BsYXlTdGFsbCA9PiB7XG4gICAgY29uc3Qgc3RhdGVGaW5nZXJwcmludCA9IGF1dG9wbGF5U3RhdGVGaW5nZXJwcmludChzdGF0ZSlcbiAgICBjb25zdCByZWNvdmVyeUtleSA9IGF1dG9wbGF5UmVjb3ZlcnlGaW5nZXJwcmludChzdGF0ZSlcbiAgICByZXR1cm4ge1xuICAgICAgdHVybjogc3RhdGUudHVybixcbiAgICAgIGZpbmdlcnByaW50OiBhdXRvcGxheVRyYWNlRmluZ2VycHJpbnQoc3RhdGUpLFxuICAgICAgdmlzaXRzOiBjb250ZXh0LnZpc2l0cy5nZXQoc3RhdGVGaW5nZXJwcmludCkgPz8gMCxcbiAgICAgIC4uLihjb250ZXh0Lmxhc3RSZWFzb24gPyB7IGxhc3RSZWFzb246IGNvbnRleHQubGFzdFJlYXNvbiB9IDoge30pLFxuICAgICAgZmFpbGVkOiBbLi4uY29udGV4dC5mYWlsZWQuZW50cmllcygpXS5tYXAoKFtjb21tYW5kLCBjb3VudF0pID0+ICh7IGNvbW1hbmQsIGNvdW50IH0pKS5zb3J0KChhLCBiKSA9PiBiLmNvdW50IC0gYS5jb3VudCB8fCBhLmNvbW1hbmQubG9jYWxlQ29tcGFyZShiLmNvbW1hbmQpKSxcbiAgICAgIHJlY2VudFBvc2l0aW9uczogWy4uLmNvbnRleHQucmVjZW50UG9zaXRpb25zXSxcbiAgICAgIGd1YXJkczogeyBzdHJhdGVnaWNWaXNpdHM6IE1hdGgubWF4KDAsIC4uLmNvbnRleHQuc3RyYXRlZ2ljVmlzaXRzLnZhbHVlcygpKSwgbm9Qcm9ncmVzc1R1cm5zOiBjb250ZXh0Lm5vUHJvZ3Jlc3NUdXJucywgbm9UdXJuQ29tbWFuZHM6IGNvbnRleHQubm9UdXJuQ29tbWFuZHMsIGxvb3BSZWNvdmVyaWVzOiBjb250ZXh0Lmxvb3BSZWNvdmVyaWVzLCByZWNvdmVyeVZpc2l0czogY29udGV4dC5yZWNvdmVyeVZpc2l0cy5nZXQocmVjb3ZlcnlLZXkpID8/IDAgfVxuICAgIH1cbiAgfVxuICB0cnkge1xuICAgIHdoaWxlIChzdGF0ZS5zdGF0dXMgPT09ICdwbGF5aW5nJyAmJiBzdGF0ZS50dXJuIDwgdHVybkxpbWl0KSB7XG4gICAgICBjb25zdCBkZWNpc2lvbiA9IGF1dG9wbGF5RGVjaXNpb24oc3RhdGUsIG1vZGUsIHBvbGljeSwgY29udGV4dCwgaGV1cmlzdGljUHJvZmlsZSlcbiAgICAgIGlmICghZGVjaXNpb24pIHtcbiAgICAgICAgY29uc3QgY29tcGFuaW9uSWRzID0gYXV0b3BsYXlEaXJlY3RDb21wYW5pb25JZHMoc3RhdGUpXG4gICAgICAgIGlmIChjb21wYW5pb25JZHMubGVuZ3RoKSB7XG4gICAgICAgICAgcGFydHlPdXRjb21lcy5kaXJlY3RNb2RlUmVmdXNlZCA9IHRydWVcbiAgICAgICAgICB1bnN1cHBvcnRlZCA9IHsga2luZDogJ2RpcmVjdC1jb21wYW5pb24tY29udHJvbCcsIGNvbXBhbmlvbklkcywgbWVzc2FnZTogJ0F1dG9wbGF5IGRvZXMgbm90IGlzc3VlIGRpcmVjdCBjb21wYW5pb24gY29tbWFuZHMuJyB9XG4gICAgICAgICAgb3V0Y29tZSA9ICd1bnN1cHBvcnRlZCdcbiAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICBzdGFsbCA9IHN0YWxsU25hcHNob3QoKVxuICAgICAgICAgIG91dGNvbWUgPSBjb250ZXh0Lmxhc3RSZWFzb24/LnN0YXJ0c1dpdGgoJ3R1cm4gZ3VhcmQ6JykgPyAndHVybi1saW1pdCcgOiAnc3RhbGxlZCdcbiAgICAgICAgfVxuICAgICAgICBicmVha1xuICAgICAgfVxuICAgICAgY29uc3QgY29tbWFuZCA9IGRlY2lzaW9uLmNvbW1hbmRcbiAgICAgIGZvciAoY29uc3QgYXNzZXNzbWVudCBvZiBkZWNpc2lvbi5yZXNvdXJjZURpYWdub3N0aWNzKSB7XG4gICAgICAgIHJlc291cmNlT3V0Y29tZXNbYXNzZXNzbWVudC5kaXNwb3NpdGlvbiA9PT0gJ3NlbGVjdCcgPyAnc2VsZWN0ZWQnIDogYXNzZXNzbWVudC5kaXNwb3NpdGlvbiA9PT0gJ2RlZmVyJyA/ICdkZWZlcnJlZCcgOiAncmVqZWN0ZWQnXSsrXG4gICAgICAgIGlmIChhc3Nlc3NtZW50LmRpc3Bvc2l0aW9uID09PSAnc2VsZWN0JyAmJiBhc3Nlc3NtZW50LnByb2plY3RlZFJvdXRlR2FpbikgcmVzb3VyY2VPdXRjb21lcy5wcm9qZWN0ZWRSb3V0ZUdhaW5zKytcbiAgICAgICAgaWYgKGFzc2Vzc21lbnQuZGlzcG9zaXRpb24gPT09ICdzZWxlY3QnICYmIGFzc2Vzc21lbnQua25vd25Dcml0aWNhbFJvdXRlKSByZXNvdXJjZU91dGNvbWVzLmNyaXRpY2FsUm91dGVTZWxlY3Rpb25zKytcbiAgICAgIH1cbiAgICAgIGlmIChkZWNpc2lvbi5yZWFzb24uc3RhcnRzV2l0aCgndG9vbDonKSB8fCBkZWNpc2lvbi5yZWFzb24uc3RhcnRzV2l0aCgnd2FpdCB0b29sIGNvb2xkb3duOicpKSBmb3IgKGNvbnN0IGFzc2Vzc21lbnQgb2YgZGVjaXNpb24udG9vbERpYWdub3N0aWNzKSB0b29sT3V0Y29tZXNbYXNzZXNzbWVudC5kaXNwb3NpdGlvbiA9PT0gJ3NlbGVjdCcgPyAnc2VsZWN0ZWQnIDogYXNzZXNzbWVudC5kaXNwb3NpdGlvbiA9PT0gJ2RlZmVyJyA/ICdkZWZlcnJlZCcgOiAncmVqZWN0ZWQnXSsrXG4gICAgICBpZiAoZGVjaXNpb24ucmVhc29uLnN0YXJ0c1dpdGgoJ3B1cnN1ZSBvcHRpb25hbCcpIHx8IGRlY2lzaW9uLnJlYXNvbi5zdGFydHNXaXRoKCdvcGVuIG9wdGlvbmFsJykpIGZvciAoY29uc3QgYXNzZXNzbWVudCBvZiBkZWNpc2lvbi5vcHRpb25hbERpYWdub3N0aWNzKSB7XG4gICAgICAgIG9wdGlvbmFsT3V0Y29tZXNbYXNzZXNzbWVudC5kaXNwb3NpdGlvbiA9PT0gJ3B1cnN1ZScgPyAncHVyc3VlZCcgOiBhc3Nlc3NtZW50LmRpc3Bvc2l0aW9uID09PSAnZGVmZXInID8gJ2RlZmVycmVkJyA6ICdkZWNsaW5lZCddKytcbiAgICAgICAgaWYgKGFzc2Vzc21lbnQuZGlzcG9zaXRpb24gPT09ICdwdXJzdWUnKSBvcHRpb25hbE91dGNvbWVzW2Fzc2Vzc21lbnQua2luZCA9PT0gJ3Nob3J0Y3V0JyA/ICdzaG9ydGN1dHMnIDogJ3NlY3JldHMnXSsrXG4gICAgICB9XG4gICAgICBjb25zdCBiZWZvcmUgPSB0ZWxlbWV0cnlTbmFwc2hvdChzdGF0ZSlcbiAgICAgIGNvbnN0IHBhcnR5QmVmb3JlID0gc3RydWN0dXJlZENsb25lKHN0YXRlKVxuICAgICAgY29uc3QgdHJhbnNpdGlvbiA9IHNuYXBzaG90QXV0b3BsYXlUcmFuc2l0aW9uKHN0YXRlKVxuICAgICAgY29uc3QgYmVmb3JlVG9vbHMgPSBbLi4uKHN0YXRlLmhlcm8udHJhdmVyc2FsVG9vbHMgPz8gW10pXVxuICAgICAgY29uc3QgYmVmb3JlUmVzb3VyY2VzID0geyBoZWFsdGg6IHN0YXRlLmhlcm8uaGVhbHRoLCBmb2N1czogc3RhdGUuaGVyby5mb2N1cywgZ29sZDogc3RhdGUuaGVyby5nb2xkLCBib21iczogc3RhdGUuaGVyby5ib21icywgcm9wZXM6IHN0YXRlLmhlcm8ucm9wZXMsIGtleXM6IHN0YXRlLmhlcm8ua2V5cyB9XG4gICAgICBjb25zdCBiZWZvcmVUcmFjZSA9IGNhcHR1cmVUcmFjZSA/IHsgdHVybjogc3RhdGUudHVybiwgcmVwbGF5OiBhdXRvcGxheVJlcGxheU1ldGFkYXRhKHN0YXRlLCBpbml0aWFsSGVybyksIGZpbmdlcnByaW50OiBhdXRvcGxheVRyYWNlRmluZ2VycHJpbnQoc3RhdGUpLCB4OiBzdGF0ZS5oZXJvLngsIHk6IHN0YXRlLmhlcm8ueSwgaGVhbHRoOiBzdGF0ZS5oZXJvLmhlYWx0aCwgZm9jdXM6IHN0YXRlLmhlcm8uZm9jdXMsIGJvbWJzOiBzdGF0ZS5oZXJvLmJvbWJzLCByb3Blczogc3RhdGUuaGVyby5yb3Blcywga2V5czogc3RhdGUuaGVyby5rZXlzLCBvYmplY3RpdmU6IHN0YXRlLmZsb29yLm9iamVjdGl2ZS5zdGF0dXMgfSA6IHVuZGVmaW5lZFxuICAgICAgY29uc3QgdHJhY2VPYnNlcnZhdGlvbiA9IGNhcHR1cmVUcmFjZURvY3VtZW50ID8gb2JzZXJ2ZUF1dG9wbGF5VHJhY2Uoc3RhdGUsIG1vZGUpIDogdW5kZWZpbmVkXG4gICAgICBjb25zdCB0cmFjZUZlYXR1cmVzID0gY2FwdHVyZVRyYWNlRG9jdW1lbnQgPyBlbmNvZGVQb2xpY3lGZWF0dXJlcyhzdGF0ZSwgbW9kZSwgZmVhdHVyZUhpc3RvcnkpIDogdW5kZWZpbmVkXG4gICAgICBjb25zdCBldmVudHMgPSBwZXJmb3JtKHN0YXRlLCBjb21tYW5kKVxuICAgICAgcmVjb3JkQXV0b3BsYXlQYXJ0eU91dGNvbWUocGFydHlPdXRjb21lcywgcGFydHlCZWZvcmUsIHN0YXRlLCBldmVudHMpXG4gICAgICBpZiAoZGVjaXNpb24ucmVhc29uLnN0YXJ0c1dpdGgoJ2NvbmZpcm0gdG9vbDonKSkge1xuICAgICAgICB0b29sT3V0Y29tZXMudXNlcysrXG4gICAgICAgIGlmIChiZWZvcmVUb29scy5zb21lKHRvb2wgPT4gIShzdGF0ZS5oZXJvLnRyYXZlcnNhbFRvb2xzID8/IFtdKS5pbmNsdWRlcyh0b29sKSkpIHRvb2xPdXRjb21lcy5yZXRpcmVtZW50cysrXG4gICAgICB9XG4gICAgICBvYnNlcnZlVGVsZW1ldHJ5VHVybihzdGF0ZSwgYmVmb3JlLCBldmVudHMsIGNvbW1hbmQpXG4gICAgICByZWNvcmRBdXRvcGxheVRyYW5zaXRpb25TbmFwc2hvdChjb250ZXh0LCB0cmFuc2l0aW9uLCBjb21tYW5kLCBzdGF0ZSlcbiAgICAgIGlmIChjYXB0dXJlVHJhY2UpIHRyYWNlLnB1c2goe1xuICAgICAgICB0dXJuOiBiZWZvcmVUcmFjZSEudHVybixcbiAgICAgICAgcmVwbGF5OiBiZWZvcmVUcmFjZSEucmVwbGF5LFxuICAgICAgICBmaW5nZXJwcmludDogYmVmb3JlVHJhY2UhLmZpbmdlcnByaW50LFxuICAgICAgICBjb21tYW5kLFxuICAgICAgICByZWFzb246IGRlY2lzaW9uLnJlYXNvbixcbiAgICAgICAgY2FuZGlkYXRlczogZGVjaXNpb24uY2FuZGlkYXRlcyxcbiAgICAgICAgLi4uKGRlY2lzaW9uLnJlc291cmNlRGlhZ25vc3RpY3MubGVuZ3RoID8geyByZXNvdXJjZURpYWdub3N0aWNzOiBzdHJ1Y3R1cmVkQ2xvbmUoZGVjaXNpb24ucmVzb3VyY2VEaWFnbm9zdGljcykgfSA6IHt9KSxcbiAgICAgICAgLi4uKGRlY2lzaW9uLnRvb2xEaWFnbm9zdGljcy5sZW5ndGggPyB7IHRvb2xEaWFnbm9zdGljczogc3RydWN0dXJlZENsb25lKGRlY2lzaW9uLnRvb2xEaWFnbm9zdGljcykgfSA6IHt9KSxcbiAgICAgICAgLi4uKGRlY2lzaW9uLm9wdGlvbmFsRGlhZ25vc3RpY3MubGVuZ3RoID8geyBvcHRpb25hbERpYWdub3N0aWNzOiBzdHJ1Y3R1cmVkQ2xvbmUoZGVjaXNpb24ub3B0aW9uYWxEaWFnbm9zdGljcykgfSA6IHt9KSxcbiAgICAgICAgZXZlbnRzOiBldmVudHMubWFwKGV2ZW50TGFiZWwpLFxuICAgICAgICBuZXh0RmluZ2VycHJpbnQ6IGF1dG9wbGF5VHJhY2VGaW5nZXJwcmludChzdGF0ZSksXG4gICAgICAgIGJlZm9yZTogeyB4OiBiZWZvcmVUcmFjZSEueCwgeTogYmVmb3JlVHJhY2UhLnksIGhlYWx0aDogYmVmb3JlVHJhY2UhLmhlYWx0aCwgZm9jdXM6IGJlZm9yZVRyYWNlIS5mb2N1cywgYm9tYnM6IGJlZm9yZVRyYWNlIS5ib21icywgcm9wZXM6IGJlZm9yZVRyYWNlIS5yb3Blcywgb2JqZWN0aXZlOiBiZWZvcmVUcmFjZSEub2JqZWN0aXZlIH0sXG4gICAgICAgIGFmdGVyOiB7IHg6IHN0YXRlLmhlcm8ueCwgeTogc3RhdGUuaGVyby55LCBoZWFsdGg6IHN0YXRlLmhlcm8uaGVhbHRoLCBmb2N1czogc3RhdGUuaGVyby5mb2N1cywgYm9tYnM6IHN0YXRlLmhlcm8uYm9tYnMsIHJvcGVzOiBzdGF0ZS5oZXJvLnJvcGVzLCBvYmplY3RpdmU6IHN0YXRlLmZsb29yLm9iamVjdGl2ZS5zdGF0dXMsIC4uLihzdGF0ZS5tb2RhbCA/IHsgbW9kYWw6IHN0YXRlLm1vZGFsLmtpbmQgfSA6IHt9KSB9XG4gICAgICB9KVxuICAgICAgY29uc3QgcmVzb3VyY2VEZWx0YSA9IHsgaGVhbHRoOiBzdGF0ZS5oZXJvLmhlYWx0aCAtIGJlZm9yZVJlc291cmNlcy5oZWFsdGgsIGZvY3VzOiBzdGF0ZS5oZXJvLmZvY3VzIC0gYmVmb3JlUmVzb3VyY2VzLmZvY3VzLCBnb2xkOiBzdGF0ZS5oZXJvLmdvbGQgLSBiZWZvcmVSZXNvdXJjZXMuZ29sZCwgYm9tYnM6IHN0YXRlLmhlcm8uYm9tYnMgLSBiZWZvcmVSZXNvdXJjZXMuYm9tYnMsIHJvcGVzOiBzdGF0ZS5oZXJvLnJvcGVzIC0gYmVmb3JlUmVzb3VyY2VzLnJvcGVzLCBrZXlzOiBzdGF0ZS5oZXJvLmtleXMgLSBiZWZvcmVSZXNvdXJjZXMua2V5cyB9XG4gICAgICBpZiAodHJhY2VPYnNlcnZhdGlvbiAmJiB0cmFjZUZlYXR1cmVzKSB0cmFjZVJlY29yZHMucHVzaChjcmVhdGVBdXRvcGxheVRyYWNlUmVjb3JkKHsgc2VxdWVuY2U6IHRyYWNlUmVjb3Jkcy5sZW5ndGgsIGVwaXNvZGU6IHRyYWNlRXBpc29kZSwgdHVybjogYmVmb3JlVHJhY2UhLnR1cm4sIHJlcGxheTogYmVmb3JlVHJhY2UhLnJlcGxheSwgb2JzZXJ2YXRpb246IHRyYWNlT2JzZXJ2YXRpb24sIGZlYXR1cmVzOiB0cmFjZUZlYXR1cmVzLCBsZWdhbENhbmRpZGF0ZXM6IGRlY2lzaW9uLmNhbmRpZGF0ZXMubWFwKGNhbmRpZGF0ZSA9PiAoeyAuLi5jYW5kaWRhdGUgfSkpLCAuLi4oZGVjaXNpb24ucmVzb3VyY2VEaWFnbm9zdGljcy5sZW5ndGggPyB7IHJlc291cmNlRGlhZ25vc3RpY3M6IHN0cnVjdHVyZWRDbG9uZShkZWNpc2lvbi5yZXNvdXJjZURpYWdub3N0aWNzKSB9IDoge30pLCAuLi4oZGVjaXNpb24udG9vbERpYWdub3N0aWNzLmxlbmd0aCA/IHsgdG9vbERpYWdub3N0aWNzOiBzdHJ1Y3R1cmVkQ2xvbmUoZGVjaXNpb24udG9vbERpYWdub3N0aWNzKSB9IDoge30pLCAuLi4oZGVjaXNpb24ub3B0aW9uYWxEaWFnbm9zdGljcy5sZW5ndGggPyB7IG9wdGlvbmFsRGlhZ25vc3RpY3M6IHN0cnVjdHVyZWRDbG9uZShkZWNpc2lvbi5vcHRpb25hbERpYWdub3N0aWNzKSB9IDoge30pLCBjaG9zZW46IHsgY29tbWFuZCwgcmVhc29uOiBkZWNpc2lvbi5yZWFzb24gfSwgb3V0Y29tZTogeyBldmVudHM6IGV2ZW50cy5tYXAoZXZlbnRMYWJlbCksIG5leHRGaW5nZXJwcmludDogYXV0b3BsYXlUcmFjZUZpbmdlcnByaW50KHN0YXRlKSwgc3RhdHVzOiBzdGF0ZS5zdGF0dXMgfSwgcmVzb3VyY2VEZWx0YSwgcHJldmlvdXNIYXNoOiB0cmFjZVJlY29yZHMuYXQoLTEpPy5oYXNoID8/IG51bGwgfSkpXG4gICAgICBmZWF0dXJlSGlzdG9yeSA9IGFwcGVuZFBvbGljeUZlYXR1cmVIaXN0b3J5KGZlYXR1cmVIaXN0b3J5LCB7IHR1cm46IGJlZm9yZS50dXJuLCBjb21tYW5kLCByZWFzb246IGRlY2lzaW9uLnJlYXNvbiwgZXZlbnRzOiBldmVudHMubWFwKGV2ZW50TGFiZWwpLCByZXNvdXJjZURlbHRhIH0pXG4gICAgICBpZiAodHJhY2VMaW1pdCAhPT0gdW5kZWZpbmVkICYmIHRyYWNlLmxlbmd0aCA+IHRyYWNlTGltaXQpIHRyYWNlLnNwbGljZSgwLCB0cmFjZS5sZW5ndGggLSB0cmFjZUxpbWl0KVxuICAgICAgY29tbWFuZHMucHVzaChjb21tYW5kKVxuICAgICAgaWYgKGV2ZW50cy5zb21lKGV2ZW50ID0+IGV2ZW50LnR5cGUgPT09ICdmbG9vcicpKSB7XG4gICAgICAgIGlmIChvcHRpb25zLmNoYWluRmxvb3JzID09PSBmYWxzZSkgeyBvdXRjb21lID0gJ2NvbXBsZXRlJzsgYnJlYWsgfVxuICAgICAgICBjb250ZXh0ID0gY3JlYXRlQXV0b3BsYXlDb250ZXh0KClcbiAgICAgICAgc3RhbGxlZCA9IDBcbiAgICAgIH1cbiAgICAgIGlmIChldmVudHMuc29tZShldmVudCA9PiBldmVudC50eXBlID09PSAnYXJlYUNvbXBsZXRlJykpIHtcbiAgICAgICAgY29uc3QgY29tcGxldGVkID0gc3RhdGUuYXJlYSA/PyBzdGF0ZS5mbG9vci5iaW9tZVxuICAgICAgICBjb21wbGV0ZWRBcmVhcy5wdXNoKGNvbXBsZXRlZClcbiAgICAgICAgY29uc3Qgc3VjY2Vzc29yID0gb3B0aW9ucy5jaGFpbkFyZWFzID09PSBmYWxzZSA/IHVuZGVmaW5lZCA6IG5leHRBcmVhKGNvbXBsZXRlZCwgYXJlYU9yZGVyKVxuICAgICAgICBpZiAoIXN1Y2Nlc3NvcikgeyBvdXRjb21lID0gJ2NvbXBsZXRlJzsgYnJlYWsgfVxuICAgICAgICBjb25zdCBuZXh0ID0gbmV3UnVuKHN0YXRlLnNlZWQsIHN1Y2Nlc3NvciwgMCwgc3RhdGUuaGVybywgc3RhdGUucmVzY3VlZE5wY3MsIFtdLCBhcmVhT3JkZXIsIHN0YXRlLmNhbXBhaWduQ3ljbGUsIHN0YXRlLmNvbXBhbmlvbnMsIHN0YXRlLmNvbXBhbmlvbkRlYXRoTW9kZSA/PyAnaW5qdXJ5JylcbiAgICAgICAgbmV4dC50dXJuID0gc3RhdGUudHVyblxuICAgICAgICBuZXh0LmxpbmVhZ2VFdmVudHMgPSBzdHJ1Y3R1cmVkQ2xvbmUoc3RhdGUubGluZWFnZUV2ZW50cyA/PyBbXSlcbiAgICAgICAgbmV4dC50ZWxlbWV0cnkgPSBzdHJ1Y3R1cmVkQ2xvbmUoc3RhdGUudGVsZW1ldHJ5ISlcbiAgICAgICAgbmV4dC5yZXB1dGF0aW9uID0geyAuLi4oc3RhdGUucmVwdXRhdGlvbiA/PyB7IHRyYWlsZm9sazogMCwga2FtaTogMCB9KSB9XG4gICAgICAgIHN0YXRlID0gbmV4dFxuICAgICAgICBjb250ZXh0ID0gY3JlYXRlQXV0b3BsYXlDb250ZXh0KClcbiAgICAgICAgc3RhbGxlZCA9IDBcbiAgICAgICAgY29udGludWVcbiAgICAgIH1cbiAgICAgIHN0YWxsZWQgPSBzdGF0ZS50dXJuID09PSBiZWZvcmUudHVybiA/IHN0YWxsZWQgKyAxIDogMFxuICAgICAgaWYgKHN0YWxsZWQgPj0gc3RhbGxlZExpbWl0KSB7XG4gICAgICAgIHN0YWxsID0gc3RhbGxTbmFwc2hvdCgpXG4gICAgICAgIG91dGNvbWUgPSAnc3RhbGxlZCdcbiAgICAgICAgYnJlYWtcbiAgICAgIH1cbiAgICB9XG4gICAgaWYgKHN0YXRlLnN0YXR1cyA9PT0gJ2RlYWQnKSBvdXRjb21lID0gJ2RlYWQnXG4gIH0gY2F0Y2ggKGNhdWdodCkge1xuICAgIG91dGNvbWUgPSAnZXJyb3InXG4gICAgZXJyb3IgPSBjYXVnaHQgaW5zdGFuY2VvZiBFcnJvciA/IGNhdWdodC5tZXNzYWdlIDogU3RyaW5nKGNhdWdodClcbiAgfVxuICBjb25zdCBmaW5hbDogQXV0b3BsYXlGaW5hbFN0YXRlID0ge1xuICAgIHN0YXR1czogc3RhdGUuc3RhdHVzLFxuICAgIGFyZWFGbG9vcjogc3RhdGUuYXJlYUZsb29yID8/IHN0YXRlLmZsb29yLmluZGV4ICUgNCxcbiAgICBoZXJvOiB7IHg6IHN0YXRlLmhlcm8ueCwgeTogc3RhdGUuaGVyby55LCBoZWFsdGg6IHN0YXRlLmhlcm8uaGVhbHRoLCBmb2N1czogc3RhdGUuaGVyby5mb2N1cywgZ29sZDogc3RhdGUuaGVyby5nb2xkLCBib21iczogc3RhdGUuaGVyby5ib21icywgcm9wZXM6IHN0YXRlLmhlcm8ucm9wZXMsIGtleXM6IHN0YXRlLmhlcm8ua2V5cyB9LFxuICAgIGV4aXQ6IHsgLi4uc3RhdGUuZmxvb3IuZXhpdCB9LFxuICAgIG9iamVjdGl2ZTogc3RydWN0dXJlZENsb25lKHN0YXRlLmZsb29yLm9iamVjdGl2ZSksXG4gICAgZ3VhcmRpYW5EZWZlYXRlZDogc3RhdGUuZmxvb3IuZ3VhcmRpYW5EZWZlYXRlZCxcbiAgICBleGl0UGF0aDogZXhpdFBhdGhTdGF0ZShzdGF0ZSksXG4gICAgaG9zdGlsZXM6IHN0YXRlLmZsb29yLmFjdG9ycy5maWx0ZXIoYWN0b3IgPT4gYWN0b3IuaG9zdGlsZSAmJiBhY3Rvci5oZWFsdGggPiAwKS5tYXAoYWN0b3IgPT4gKHsgaWQ6IGFjdG9yLmlkLCB4OiBhY3Rvci54LCB5OiBhY3Rvci55LCBoZWFsdGg6IGFjdG9yLmhlYWx0aCwgLi4uKGFjdG9yLmFpID8geyBhaTogYWN0b3IuYWkgfSA6IHt9KSB9KSksXG4gICAgLi4uKHN0YXRlLm1vZGFsID8geyBtb2RhbDogc3RhdGUubW9kYWwua2luZCB9IDoge30pXG4gIH1cbiAgY29uc3QgZmluYWxCaW9tZSA9IHN0YXRlLmFyZWEgPz8gc3RhdGUuZmxvb3IuYmlvbWVcbiAgY29uc3QgZGVidWcgPSB7IG9iamVjdGl2ZUlkOiBjb250ZXh0Lm9iamVjdGl2ZUlkLCBvYmplY3RpdmVUYXJnZXQ6IGNvbnRleHQub2JqZWN0aXZlVGFyZ2V0LCBvYmplY3RpdmVUYXJnZXRDb3VudDogY29udGV4dC5vYmplY3RpdmVUYXJnZXRDb3VudCwgcmVqZWN0ZWRPYmplY3RpdmVUYXJnZXRzOiBbLi4uY29udGV4dC5yZWplY3RlZE9iamVjdGl2ZVRhcmdldHNdLnNvcnQoKSwgYmVzdFN0cmF0ZWdpY0Rpc3RhbmNlOiBjb250ZXh0LmJlc3RTdHJhdGVnaWNEaXN0YW5jZSwgbm9Qcm9ncmVzc1R1cm5zOiBjb250ZXh0Lm5vUHJvZ3Jlc3NUdXJucywgbm9UdXJuQ29tbWFuZHM6IGNvbnRleHQubm9UdXJuQ29tbWFuZHMsIGxvb3BSZWNvdmVyaWVzOiBjb250ZXh0Lmxvb3BSZWNvdmVyaWVzLCByZWNlbnRQb3NpdGlvbnM6IFsuLi5jb250ZXh0LnJlY2VudFBvc2l0aW9uc10gfVxuICBjb25zdCBjYW1wYWlnbkNvbXBsZXRlID0gaXNDb21wbGV0ZUNhbXBhaWduKG91dGNvbWUsIGNvbXBsZXRlZEFyZWFzLCBhcmVhT3JkZXIpXG4gIGNvbnN0IG1ldHJpY3MgPSBzdHJ1Y3R1cmVkQ2xvbmUoc3RhdGUudGVsZW1ldHJ5ISlcbiAgY29uc3QgZXhwbG9yZWRUaWxlcyA9IHN0YXRlLmZsb29yLnRpbGVzLmZpbHRlcih0aWxlID0+IHRpbGUuZXhwbG9yZWQpLmxlbmd0aFxuICBjb25zdCByZXRhaW5lZFJlc291cmNlcyA9IHN0YXRlLmhlcm8uYm9tYnMgKyBzdGF0ZS5oZXJvLnJvcGVzICsgc3RhdGUuaGVyby5rZXlzXG4gIGNvbnN0IHBvbGljeU1ldGFkYXRhID0gY3JlYXRlUG9saWN5UnVuTWV0YWRhdGEocG9saWN5UHJvZmlsZSwgc3RhdGUuc2VlZCwgdHVybkxpbWl0LCBzY29yZVBvbGljeUVwaXNvZGUoeyBjYW1wYWlnbkNvbXBsZXRlLCBvdXRjb21lLCBleHBsb3JlZFRpbGVzLCBtZXRyaWNzLCByZXRhaW5lZFJlc291cmNlcyB9KSlcbiAgY29uc3QgdHJhY2VEb2N1bWVudCA9IGNhcHR1cmVUcmFjZURvY3VtZW50ID8gY3JlYXRlQXV0b3BsYXlUcmFjZURvY3VtZW50KHRyYWNlRXBpc29kZSwgdHJhY2VSZWNvcmRzLCB7IG91dGNvbWUsIHJlYXNvbjogZXJyb3IgPz8gc3RhbGw/Lmxhc3RSZWFzb24gPz8gKG91dGNvbWUgPT09ICdjb21wbGV0ZScgPyAnY29tcGxldGUnIDogb3V0Y29tZSksIHR1cm5zOiBzdGF0ZS50dXJuLCBjYW1wYWlnbkNvbXBsZXRlLCBmaW5hbEZpbmdlcnByaW50OiBhdXRvcGxheVRyYWNlRmluZ2VycHJpbnQoc3RhdGUpIH0pIDogdW5kZWZpbmVkXG4gIHJldHVybiB7IHNlZWQ6IHN0YXRlLnNlZWQsIGJpb21lOiBzdGFydEJpb21lLCBhcmVhT3JkZXI6IFsuLi5hcmVhT3JkZXJdLCBmaW5hbEJpb21lLCBmbG9vcjogc3RhdGUuZmxvb3IuaW5kZXggKyAxLCBtb2RlLCBwb2xpY3ksIGhldXJpc3RpY1Byb2ZpbGU6IGF1dG9wbGF5SGV1cmlzdGljUHJvZmlsZVJlZihoZXVyaXN0aWNQcm9maWxlKSwgcG9saWN5TWV0YWRhdGEsIG91dGNvbWUsIHR1cm5zOiBzdGF0ZS50dXJuLCBjb21tYW5kcywgdHJhY2UsIC4uLih0cmFjZURvY3VtZW50ID8geyB0cmFjZURvY3VtZW50IH0gOiB7fSksIHJlcGxheTogYXV0b3BsYXlSZXBsYXlNZXRhZGF0YShzdGF0ZSwgaW5pdGlhbEhlcm8pLCBtZXRyaWNzLCByZXNvdXJjZU91dGNvbWVzLCB0b29sT3V0Y29tZXMsIG9wdGlvbmFsT3V0Y29tZXMsIHBhcnR5T3V0Y29tZXMsIGZpbmdlcnByaW50OiBmaW5nZXJwcmludChzdGF0ZSksIGZpbmFsLCBjb21wbGV0ZWRBcmVhcywgY2FtcGFpZ25Db21wbGV0ZSwgLi4uKHVuc3VwcG9ydGVkID8geyB1bnN1cHBvcnRlZCB9IDoge30pLCAuLi4ob3B0aW9ucy5pbmNsdWRlU3RhdGUgPyB7IHN0YXRlOiBzdHJ1Y3R1cmVkQ2xvbmUoc3RhdGUpIH0gOiB7fSksIC4uLihvcHRpb25zLmluY2x1ZGVEZWJ1ZyA/IHsgZGVidWcgfSA6IHt9KSwgLi4uKHN0YWxsID8geyBzdGFsbCB9IDoge30pLCAuLi4oZXJyb3IgPyB7IGVycm9yIH0gOiB7fSkgfVxufVxuIl0sIm1hcHBpbmdzIjoiQUFBQSxTQUFTQSxrQkFBa0IsRUFBRUMsZ0JBQWdCLEVBQUVDLDJCQUEyQixFQUFFQyx3QkFBd0IsRUFBRUMsd0JBQXdCLEVBQUVDLHFCQUFxQixFQUFFQyxnQ0FBZ0MsRUFBRUMsMEJBQTBCLFFBQVEsWUFBWTtBQUN2TyxTQUFTQyxNQUFNLEVBQUVDLE9BQU8sUUFBUSxVQUFVO0FBQzFDLFNB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