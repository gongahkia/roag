import type { MjsonEvent } from "./mjson";

export interface ReplayMeld {
  readonly kind: string;
  readonly tiles: readonly string[];
  readonly from_seat: number | null;
}

export interface ReplayBoardState {
  readonly players: number;
  readonly names: readonly string[];
  readonly round_wind: string | null;
  readonly kyoku: number | null;
  readonly honba: number;
  readonly kyotaku: number;
  readonly dealer_seat: number | null;
  readonly scores: readonly number[];
  readonly hands: readonly (readonly string[])[];
  readonly discards: readonly (readonly string[])[];
  readonly melds: readonly (readonly ReplayMeld[])[];
  readonly kita_tiles: readonly (readonly string[])[];
  readonly dora_indicators: readonly string[];
  readonly riichi_seats: readonly boolean[];
  readonly active_seat: number | null;
  readonly round_finished: boolean;
  readonly game_finished: boolean;
}

export interface ReplayDecisionInspection {
  readonly actor: number | null;
  readonly legal_actions: readonly string[];
}

export interface ReplayTimelineStep {
  readonly index: number;
  readonly event: MjsonEvent;
  readonly label: string;
  readonly state: ReplayBoardState;
  readonly decision: ReplayDecisionInspection | null;
}

interface MutableReplayBoardState {
  players: number;
  names: string[];
  round_wind: string | null;
  kyoku: number | null;
  honba: number;
  kyotaku: number;
  dealer_seat: number | null;
  scores: number[];
  hands: string[][];
  discards: string[][];
  melds: ReplayMeld[][];
  kita_tiles: string[][];
  dora_indicators: string[];
  riichi_seats: boolean[];
  active_seat: number | null;
  round_finished: boolean;
  game_finished: boolean;
}

export function buildReplayTimeline(events: readonly MjsonEvent[]): readonly ReplayTimelineStep[] {
  const state = initialState();
  const timeline: ReplayTimelineStep[] = [];
  for (const [index, event] of events.entries()) {
    applyEvent(state, event);
    timeline.push(
      Object.freeze({
        index,
        event,
        label: describeMjsonEvent(event),
        state: snapshot(state),
        decision: inspectMjsonDecision(event)
      })
    );
  }
  return Object.freeze(timeline);
}

export function describeMjsonEvent(event: MjsonEvent): string {
  const actor = readSeat(event.actor);
  const actorLabel = actor === null ? "" : `Seat ${actor}: `;
  const tile = readString(event.pai);
  switch (event.type) {
    case "start_game":
      return "Game started";
    case "start_kyoku":
      return `Round ${readNumber(event.kyoku) ?? "?"} started`;
    case "tsumo":
      return `${actorLabel}drew ${tile ?? "a tile"}`;
    case "dahai":
      return `${actorLabel}discarded ${tile ?? "a tile"}`;
    case "reach":
      return `${actorLabel}declared riichi`;
    case "reach_accepted":
      return `${actorLabel}riichi accepted`;
    case "hora":
      return `${actorLabel}won${tile === null ? "" : ` on ${tile}`}`;
    case "ryukyoku":
      return "Round drawn";
    case "end_kyoku":
      return "Round ended";
    case "end_game":
      return "Game ended";
    case "request_action":
      return `${actorLabel}action requested`;
    default:
      return `${actorLabel}${event.type}${tile === null ? "" : ` ${tile}`}`;
  }
}

export function inspectMjsonDecision(event: MjsonEvent): ReplayDecisionInspection | null {
  if (event.type !== "request_action") return null;
  const possibleActions = Array.isArray(event.possible_actions) ? event.possible_actions : [];
  const legalActions = possibleActions.flatMap((action) => {
    if (!isRecord(action) || typeof action.type !== "string" || !action.type) return [];
    return [action.type];
  });
  return Object.freeze({
    actor: readSeat(event.actor),
    legal_actions: Object.freeze(legalActions)
  });
}

function initialState(): MutableReplayBoardState {
  return createState(4, []);
}

function applyEvent(state: MutableReplayBoardState, event: MjsonEvent): void {
  switch (event.type) {
    case "start_game":
      applyStartGame(state, event);
      break;
    case "start_kyoku":
      applyStartKyoku(state, event);
      break;
    case "reach":
      setRiichi(state, event);
      break;
    case "reach_accepted":
      updateScores(state, event);
      break;
    case "tsumo":
      addDrawnTile(state, event);
      break;
    case "dahai":
      addDiscard(state, event);
      break;
    case "pon":
    case "chi":
    case "daiminkan":
    case "ankan":
    case "kakan":
    case "kita":
      applyCall(state, event);
      break;
    case "hora":
    case "ryukyoku":
    case "end_kyoku":
      state.round_finished = true;
      break;
    case "end_game":
      state.game_finished = true;
      break;
  }
  const actor = readSeat(event.actor);
  if (actor !== null && actor < state.players) state.active_seat = actor;
}

function applyStartGame(state: MutableReplayBoardState, event: MjsonEvent): void {
  const names = readStringArray(event.names);
  const players = isSupportedPlayerCount(names.length) ? names.length : state.players;
  replaceState(state, createState(players, names));
}

function applyStartKyoku(state: MutableReplayBoardState, event: MjsonEvent): void {
  const scores = readNumberArray(event.scores);
  const hands = readTileMatrix(event.tehais);
  const players = inferPlayerCount(scores.length, hands.length, state.players);
  const names = state.names.slice(0, players);
  while (names.length < players) names.push(`Player ${names.length}`);
  const next = createState(players, names);
  next.round_wind = readString(event.bakaze);
  next.kyoku = readNumber(event.kyoku);
  next.honba = readNumber(event.honba) ?? 0;
  next.kyotaku = readNumber(event.kyotaku) ?? 0;
  next.dealer_seat = readSeat(event.oya);
  next.active_seat = next.dealer_seat;
  if (scores.length === players) next.scores = scores;
  if (hands.length === players) next.hands = hands;
  const dora = readString(event.dora_marker);
  if (dora !== null) next.dora_indicators = [dora];
  replaceState(state, next);
}

function setRiichi(state: MutableReplayBoardState, event: MjsonEvent): void {
  const actor = readSeat(event.actor);
  if (actor !== null && actor < state.players) state.riichi_seats[actor] = true;
}

function updateScores(state: MutableReplayBoardState, event: MjsonEvent): void {
  const scores = readNumberArray(event.scores);
  if (scores.length === state.players) state.scores = scores;
}

function addDrawnTile(state: MutableReplayBoardState, event: MjsonEvent): void {
  const actor = readSeat(event.actor);
  const tile = readString(event.pai);
  if (actor !== null && actor < state.players && tile !== null) state.hands[actor].push(tile);
}

function addDiscard(state: MutableReplayBoardState, event: MjsonEvent): void {
  const actor = readSeat(event.actor);
  const tile = readString(event.pai);
  if (actor === null || actor >= state.players || tile === null) return;
  removeTile(state.hands[actor], tile);
  state.discards[actor].push(tile);
}

function applyCall(state: MutableReplayBoardState, event: MjsonEvent): void {
  const actor = readSeat(event.actor);
  if (actor === null || actor >= state.players) return;
  const consumed = readStringArray(event.consumed);
  const tile = readString(event.pai);
  const removed = consumed.length > 0 ? consumed : tile === null ? [] : [tile];
  for (const consumedTile of removed) removeTile(state.hands[actor], consumedTile);
  if (event.type === "kita") {
    state.kita_tiles[actor].push(...removed);
    return;
  }
  const tiles = tile === null ? consumed : [...consumed, tile];
  state.melds[actor].push(
    Object.freeze({ kind: event.type, tiles: Object.freeze(tiles), from_seat: readSeat(event.target) })
  );
}

function createState(players: number, names: readonly string[]): MutableReplayBoardState {
  return {
    players,
    names: Array.from({ length: players }, (_, seat) => names[seat] || `Player ${seat}`),
    round_wind: null,
    kyoku: null,
    honba: 0,
    kyotaku: 0,
    dealer_seat: null,
    scores: Array.from({ length: players }, () => 0),
    hands: seatArrays(players),
    discards: seatArrays(players),
    melds: Array.from({ length: players }, () => []),
    kita_tiles: seatArrays(players),
    dora_indicators: [],
    riichi_seats: Array.from({ length: players }, () => false),
    active_seat: null,
    round_finished: false,
    game_finished: false
  };
}

function replaceState(target: MutableReplayBoardState, source: MutableReplayBoardState): void {
  Object.assign(target, source);
}

function snapshot(state: MutableReplayBoardState): ReplayBoardState {
  return Object.freeze({
    ...state,
    names: Object.freeze([...state.names]),
    scores: Object.freeze([...state.scores]),
    hands: freezeNested(state.hands),
    discards: freezeNested(state.discards),
    melds: freezeNested(state.melds),
    kita_tiles: freezeNested(state.kita_tiles),
    dora_indicators: Object.freeze([...state.dora_indicators]),
    riichi_seats: Object.freeze([...state.riichi_seats])
  });
}

function seatArrays(players: number): string[][] {
  return Array.from({ length: players }, () => []);
}

function freezeNested<T>(values: readonly (readonly T[])[]): readonly (readonly T[])[] {
  return Object.freeze(values.map((value) => Object.freeze([...value])));
}

function inferPlayerCount(...counts: number[]): number {
  return counts.find(isSupportedPlayerCount) ?? 4;
}

function isSupportedPlayerCount(value: number): value is 3 | 4 {
  return value === 3 || value === 4;
}

function removeTile(hand: string[], tile: string): void {
  let index = hand.indexOf(tile);
  if (index < 0) index = hand.findIndex((candidate) => normalizeTile(candidate) === normalizeTile(tile));
  if (index >= 0) hand.splice(index, 1);
}

function normalizeTile(tile: string): string {
  return tile.endsWith("r") ? tile.slice(0, -1) : tile;
}

function readString(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function readNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isInteger(value) ? value : null;
}

function readSeat(value: unknown): number | null {
  const seat = readNumber(value);
  return seat !== null && seat >= 0 ? seat : null;
}

function readStringArray(value: unknown): string[] {
  return Array.isArray(value) && value.every((item) => typeof item === "string") ? [...value] : [];
}

function readNumberArray(value: unknown): number[] {
  return Array.isArray(value) && value.every((item) => typeof item === "number" && Number.isInteger(item))
    ? [...value]
    : [];
}

function readTileMatrix(value: unknown): string[][] {
  return Array.isArray(value) && value.every((hand) => Array.isArray(hand) && hand.every((tile) => typeof tile === "string"))
    ? value.map((hand) => [...hand])
    : [];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
