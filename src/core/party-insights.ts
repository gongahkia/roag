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

/** A shareable local-playtest record. Player display names are deliberately excluded. */
export interface PartyTelemetryReport {
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
const highestUnused = (counts: Record<string, number>, used: Set<string>) => Object.entries(counts)
  .filter(([id, value]) => value > 0 && !used.has(id))
  .sort(([leftId, leftValue], [rightId, rightValue]) => rightValue - leftValue || leftId.localeCompare(rightId))[0];

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

/** Keeps final awards factual and positive: at most one receipt-backed award per golfer. */
export const partyAwardsFor = (state: GameState): PartyAward[] => {
  const events = currentEvents(state);
  const used = new Set<string>();
  const awards: PartyAward[] = [];
  const add = (id: string, title: string, counts: Record<string, number>, noun: string) => {
    const winner = highestUnused(counts, used);
    if (!winner || awards.length >= 3) return;
    used.add(winner[0]);
    awards.push({ id, playerId: winner[0], title, detail: `${playerName(state, winner[0])} logged ${winner[1]} ${noun}${winner[1] === 1 ? '' : 's'}.` });
  };
  add('collision-artist', 'collision artist', countByPlayer(events, 'collision'), 'direct ball hit');
  add('course-author', 'course author', countByPlayer(events, 'slot-action'), 'public slot choice');
  add('trick-artist', 'trick artist', countByPlayer(events, 'card'), 'Trick Card play');
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
 * Produces evidence that can be shared with the repository without participant
 * names. The caller owns download/storage; this function only transforms state.
 */
export const partyTelemetryReportFor = (state: GameState, sessionId: string, exportedAt: string): PartyTelemetryReport => {
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
