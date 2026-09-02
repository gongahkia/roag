import type { GameState, InstrumentationEvent } from './types';

export interface PartyReceipt {
  id: string;
  hole: number;
  kind: 'collision' | 'card' | 'recovery' | 'course';
  actor: string;
  target?: string;
  text: string;
}

export interface PartyAward {
  id: string;
  playerId: string;
  glyph: string;
  title: string;
  detail: string;
}

export interface PartyPacingSummary {
  measuredTurns: number;
  medianTurnSeconds?: number;
  p90TurnSeconds?: number;
  medianHoleSeconds?: number;
  withinTurnBudget: boolean;
  withinHoleBudget: boolean;
}

/** A local developer-diagnostics record. Player display names are deliberately excluded. */
export interface PartyDiagnosticsReport {
  schemaVersion: 1;
  sessionId: string;
  exportedAt: string;
  config: Pick<GameState['config'], 'seed' | 'holeCount' | 'humanCount' | 'botCount' | 'ruleset'>;
  progress: { status: GameState['status']; currentHole: number; completedHoles: number };
  players: Array<{ id: string; seat: number; kind: GameState['players'][number]['kind']; totalStrokes: number; currentHoleStrokes: number; completed: boolean }>;
  recipes: GameState['coursePlan'][number]['recipe']['metadata'][];
  connections: GameState['connections'];
  events: InstrumentationEvent[];
  pacing: PartyPacingSummary;
}

const playerName = (state: GameState, id: string | undefined) => state.players.find((player) => player.id === id)?.name ?? id ?? 'the course';
const currentEvents = (state: GameState, hole?: number) => (state.instrumentation?.events ?? []).filter((event) => hole === undefined || event.hole === hole);
const countByPlayer = (events: readonly InstrumentationEvent[], type: InstrumentationEvent['type']) => events
  .filter((event) => event.type === type && event.playerId)
  .reduce<Record<string, number>>((counts, event) => ({ ...counts, [event.playerId!]: (counts[event.playerId!] ?? 0) + 1 }), {});
const sumValuesByPlayer = (events: readonly InstrumentationEvent[], predicate: (event: InstrumentationEvent) => boolean) => events
  .filter((event) => predicate(event) && event.playerId)
  .reduce<Record<string, number>>((counts, event) => ({ ...counts, [event.playerId!]: (counts[event.playerId!] ?? 0) + (event.value ?? 1) }), {});
const peakValuesByPlayer = (events: readonly InstrumentationEvent[], predicate: (event: InstrumentationEvent) => boolean) => events
  .filter((event) => predicate(event) && event.playerId && event.value !== undefined)
  .reduce<Record<string, number>>((peaks, event) => ({ ...peaks, [event.playerId!]: Math.max(peaks[event.playerId!] ?? -Infinity, event.value!) }), {});
const lowestValuesByPlayer = (events: readonly InstrumentationEvent[], predicate: (event: InstrumentationEvent) => boolean) => events
  .filter((event) => predicate(event) && event.playerId && event.value !== undefined)
  .reduce<Record<string, number>>((lows, event) => ({ ...lows, [event.playerId!]: Math.min(lows[event.playerId!] ?? Infinity, event.value!) }), {});
const highestUnused = (counts: Record<string, number>, used: Set<string>) => Object.entries(counts)
  .filter(([id, value]) => value > 0 && !used.has(id))
  .sort(([leftId, leftValue], [rightId, rightValue]) => rightValue - leftValue || leftId.localeCompare(rightId))[0];
const lowestUnused = (counts: Record<string, number>, used: Set<string>) => Object.entries(counts)
  .filter(([id, value]) => Number.isFinite(value) && value >= 0 && !used.has(id))
  .sort(([leftId, leftValue], [rightId, rightValue]) => leftValue - rightValue || leftId.localeCompare(rightId))[0];

/** Turns structured instrumentation into short, attributable social receipts. */
export const partyReceiptsFor = (state: GameState, hole = state.hole): PartyReceipt[] => currentEvents(state, hole)
  .flatMap((event, index): PartyReceipt[] => {
    const actor = playerName(state, event.playerId);
    const target = playerName(state, event.targetId);
    if (event.type === 'collision') return [{ id: `${hole}:${index}:collision`, hole, kind: 'collision', actor, target, text: `${actor} banked into ${target}` }];
    if (event.type === 'card') {
      const targetText = event.targetId && event.targetId !== event.playerId ? ` on ${target}` : '';
      return [{ id: `${hole}:${index}:card`, hole, kind: 'card', actor, target: event.targetId ? target : undefined, text: `${actor} played ${event.detail}${targetText}` }];
    }
    if (event.type === 'recovery') return [{ id: `${hole}:${index}:recovery`, hole, kind: 'recovery', actor: 'course', target: actor, text: `the course recovered ${actor} after ${event.detail}` }];
    if (event.type === 'cause') return [{ id: `${hole}:${index}:cause`, hole, kind: 'course', actor, target: event.targetId ? target : undefined, text: event.detail }];
    return [];
  })
  .slice(-4)
  .reverse();

/**
 * End-of-match titles deliberately use recorded physics and game events. A
 * golfer can win only one title, so the final screen celebrates the table
 * instead of repeatedly congratulating a single runaway winner.
 */
export const partyAwardsFor = (state: GameState): PartyAward[] => {
  const events = currentEvents(state);
  const used = new Set<string>();
  const awards: PartyAward[] = [];
  const limit = Math.min(6, state.players.length);
  const addHighest = (id: string, glyph: string, title: string, counts: Record<string, number>, detail: (name: string, value: number) => string) => {
    const winner = highestUnused(counts, used);
    if (!winner || awards.length >= limit) return;
    used.add(winner[0]);
    awards.push({ id, playerId: winner[0], glyph, title, detail: detail(playerName(state, winner[0]), winner[1]) });
  };
  const addLowest = (id: string, glyph: string, title: string, counts: Record<string, number>, detail: (name: string, value: number) => string) => {
    const winner = lowestUnused(counts, used);
    if (!winner || awards.length >= limit) return;
    used.add(winner[0]);
    awards.push({ id, playerId: winner[0], glyph, title, detail: detail(playerName(state, winner[0]), winner[1]) });
  };
  const analysis = (detail: string) => (event: InstrumentationEvent) => event.type === 'shot-analysis' && event.detail === detail;
  const routeShots = (route: string) => events.filter((event) => event.type === 'shot' && event.detail.endsWith(`:${route}`));

  addHighest('pinball-wizard', '✦', 'Pinball Wizard', sumValuesByPlayer(events, analysis('ricochet')), (name, value) => `${name} racked up ${value} wall-and-bumper ricochet${value === 1 ? '' : 's'}.`);
  addHighest('cart-path-menace', '⚑', 'Cart Path Menace', sumValuesByPlayer(events, analysis('hazard')), (name, value) => `${name} set off ${value} course hazard${value === 1 ? '' : 's'}.`);
  addHighest('air-time-champion', '↑', 'Air Time Champion', peakValuesByPlayer(events, analysis('airtime')), (name, value) => `${name} kept one ball airborne for ${value.toFixed(1)} seconds.`);
  addHighest('sand-trap-regular', '▧', 'Sand Trap Regular', sumValuesByPlayer(events, analysis('sand')), (name, value) => `${name} toured ${value} sand tile${value === 1 ? '' : 's'} on purpose. Probably.`);
  addLowest('almost-had-it', '◎', 'Almost Had It', lowestValuesByPlayer(events, analysis('near-miss')), (name, value) => `${name} missed the cup by only ${value.toFixed(2)} tile${value === 1 ? '' : 's'}.`);
  addHighest('bank-shot-bandit', '↯', 'Bank Shot Bandit', countByPlayer(events, 'collision'), (name, value) => `${name} rearranged ${value} rival ball${value === 1 ? '' : 's'}.`);
  addHighest('pocket-menace', '✹', 'Pocket Menace', countByPlayer(events, 'card'), (name, value) => `${name} deployed ${value} Trick Card${value === 1 ? '' : 's'}.`);
  addHighest('whiff-wizard', '◌', 'Whiff Wizard', sumValuesByPlayer(events, analysis('whiff')), (name, value) => `${name} left ${value} shot${value === 1 ? '' : 's'} gloriously unholed.`);
  addHighest('full-send-scientist', '➤', 'Full Send Scientist', peakValuesByPlayer(events, (event) => event.type === 'shot'), (name, value) => `${name} committed to a ${value.toFixed(1)}-power launch.`);
  addHighest('traffic-cone-tourist', '⌁', 'Traffic Cone Tourist', countByPlayer(routeShots('conflict'), 'shot'), (name, value) => `${name} volunteered for contested traffic ${value} time${value === 1 ? '' : 's'}.`);
  addHighest('clubhouse-regular', '¤', 'Clubhouse Regular', countByPlayer(events, 'shop'), (name, value) => `${name} adopted ${value} suspicious merchant bargain${value === 1 ? '' : 's'}.`);
  addHighest('committee-putter', '…', 'Committee Putter', peakValuesByPlayer(events, (event) => event.type === 'turn-duration' && event.detail === 'shot'), (name, value) => `${name} spent ${value.toFixed(1)} seconds consulting the green.`);

  const scorecards = Object.fromEntries(state.players.map((player) => [player.id, player.total]));
  addHighest('scorecard-scare', '☄', 'Scorecard Scare', scorecards, (name, value) => `${name} survived the route with ${value} very memorable strokes.`);
  return awards;
};

const percentile = (values: readonly number[], percent: number) => {
  if (!values.length) return undefined;
  const index = Math.max(0, Math.min(values.length - 1, Math.ceil(values.length * percent) - 1));
  return [...values].sort((left, right) => left - right)[index];
};

/** These values are observational; they never impose a shot clock. */
export const partyPacingFor = (state: GameState): PartyPacingSummary => {
  const turns = currentEvents(state).filter((event) => event.type === 'turn-duration' && event.detail === 'shot').map((event) => event.value ?? 0).filter((value) => value > 0);
  const holes = currentEvents(state).filter((event) => event.type === 'hole-duration').map((event) => event.value ?? 0).filter((value) => value > 0);
  const medianTurnSeconds = percentile(turns, .5);
  const p90TurnSeconds = percentile(turns, .9);
  const medianHoleSeconds = percentile(holes, .5);
  return {
    measuredTurns: turns.length,
    medianTurnSeconds,
    p90TurnSeconds,
    medianHoleSeconds,
    withinTurnBudget: p90TurnSeconds === undefined || p90TurnSeconds <= 20,
    withinHoleBudget: medianHoleSeconds === undefined || medianHoleSeconds <= 240,
  };
};

/**
 * Produces local developer diagnostics without player display names. The caller
 * owns download/storage; this function only transforms state.
 */
export const partyDiagnosticsReportFor = (state: GameState, sessionId: string, exportedAt: string): PartyDiagnosticsReport => {
  const labels = new Map(state.players.map((player, index) => [player.id, `P${index + 1}`]));
  const anonymousId = (id: string | undefined) => id ? labels.get(id) ?? 'unknown' : undefined;
  return {
    schemaVersion: 1,
    sessionId,
    exportedAt,
    config: {
      seed: state.config.seed,
      holeCount: state.config.holeCount,
      humanCount: state.config.humanCount,
      botCount: state.config.botCount,
      ruleset: state.config.ruleset ?? 'party',
    },
    progress: {
      status: state.status,
      currentHole: state.hole,
      completedHoles: state.status === 'finished' ? state.config.holeCount : Math.max(0, state.hole - 1),
    },
    players: state.players.map((player, index) => ({
      id: labels.get(player.id)!,
      seat: index + 1,
      kind: player.kind,
      totalStrokes: player.total,
      currentHoleStrokes: player.ball.strokes,
      completed: player.ball.complete,
    })),
    recipes: state.coursePlan.map((plan) => plan.recipe.metadata),
    connections: state.connections ? [...state.connections] : undefined,
    events: (state.instrumentation?.events ?? []).map((event) => ({ ...event, playerId: anonymousId(event.playerId), targetId: anonymousId(event.targetId) })),
    pacing: partyPacingFor(state),
  };
};

export const replayPayloadFor = (state: GameState) => JSON.stringify({
  seed: state.config.seed,
  ruleset: state.config.ruleset ?? 'party',
  recipes: state.coursePlan.map((plan) => plan.recipe.metadata),
  connections: state.connections,
}, null, 2);
