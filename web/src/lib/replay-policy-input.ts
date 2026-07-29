import {
  MULTI_ACTION_ONNX_LEGAL_MASK_DIM,
  MULTI_ACTION_ONNX_OBSERVATION_DIM
} from "./onnx-inference";
import type { MjsonEvent } from "./mjson";
import type { ReplayBoardState, ReplayTimelineStep } from "./replay";

const TILE_TYPES = [
  "1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m",
  "1p", "2p", "3p", "4p", "5p", "6p", "7p", "8p", "9p",
  "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s",
  "E", "S", "W", "N", "P", "F", "C"
] as const;
const TILE_INDEX: ReadonlyMap<string, number> = new Map(TILE_TYPES.map((tile, index) => [tile, index]));
const TILE_ACTION_OFFSETS = {
  discard: 0,
  ron: 34,
  chi: 68,
  pon: 102,
  minkan: 136,
  ankan: 170,
  kakan: 204,
  kita: 238
} as const;
const SINGLE_ACTION_INDICES = {
  pass: 272,
  tsumo: 273,
  riichi: 274,
  kyushu: 275
} as const;
const ROUND_WINDS = ["E", "S", "W", "N"] as const;

export type ReplayPolicyActionKind = keyof typeof TILE_ACTION_OFFSETS | keyof typeof SINGLE_ACTION_INDICES;

export interface ReplayPolicyAction {
  readonly kind: ReplayPolicyActionKind;
  readonly tile: string | null;
  readonly label: string;
}

export interface ReplayPolicyInput {
  readonly ruleset: "tenhou-3p" | "tenhou-4p";
  readonly observation: readonly number[];
  readonly legalActionMask: readonly boolean[];
  readonly actionsByIndex: readonly (ReplayPolicyAction | null)[];
}

export interface ReplayPolicySelection {
  readonly action: ReplayPolicyAction;
  readonly index: number;
  readonly score: number;
}

export function buildReplayPolicyInput(step: ReplayTimelineStep): ReplayPolicyInput {
  if (step.event.type !== "request_action") throw new Error("select an MJSON request_action event");
  const seat = readSeat(step.event.actor);
  if (seat === null || seat >= step.state.players) throw new Error("request_action actor must be a replay seat");
  const actions = readPossibleActions(step.event);
  if (actions.length === 0) throw new Error("request_action must contain legal actions");
  const ruleset = step.state.players === 3 ? "tenhou-3p" : "tenhou-4p";
  const policyActions = actions.map((action) => mjsonActionToPolicyAction(action, step.state, ruleset));
  const legalActionMask = Array<boolean>(MULTI_ACTION_ONNX_LEGAL_MASK_DIM).fill(false);
  const actionsByIndex = Array<ReplayPolicyAction | null>(MULTI_ACTION_ONNX_LEGAL_MASK_DIM).fill(null);
  for (const action of policyActions) {
    const index = actionIndex(action);
    legalActionMask[index] = true;
    actionsByIndex[index] ??= action;
  }
  const observation = observationTensor(step.state, seat);
  return Object.freeze({
    ruleset,
    observation: Object.freeze(observation),
    legalActionMask: Object.freeze(legalActionMask),
    actionsByIndex: Object.freeze(actionsByIndex)
  });
}

export function selectReplayPolicyAction(
  input: ReplayPolicyInput,
  logits: ArrayLike<number>
): ReplayPolicySelection {
  if (logits.length !== MULTI_ACTION_ONNX_LEGAL_MASK_DIM) {
    throw new Error("ONNX model returned invalid action logits");
  }
  let selected: ReplayPolicySelection | null = null;
  for (let index = 0; index < input.legalActionMask.length; index += 1) {
    if (!input.legalActionMask[index]) continue;
    const score = logits[index];
    const action = input.actionsByIndex[index];
    if (action === null || !Number.isFinite(score)) continue;
    if (selected === null || score > selected.score) selected = { action, index, score };
  }
  if (selected === null) throw new Error("ONNX model returned no finite legal score");
  return Object.freeze(selected);
}

function observationTensor(
  state: ReplayBoardState,
  seat: number
): number[] {
  if (state.round_wind === null || !ROUND_WINDS.includes(state.round_wind as (typeof ROUND_WINDS)[number])) {
    throw new Error("request_action requires a start_kyoku round wind");
  }
  if (state.dealer_seat === null || state.current_seat === null) {
    throw new Error("request_action requires a start_kyoku dealer and current seat");
  }
  if (!isFullyObservedDirectRequest(state, seat)) {
    throw new Error("MJSON inference requires a fully observed direct decision request");
  }
  const drawnTile = state.current_seat === seat ? state.drawn_tiles[seat] : null;
  const values = [
    ...tileCounts(state.hands[seat]),
    ...tileCounts(drawnTile === null ? [] : [drawnTile]),
    ...relativeTileRows(state, seat, state.discards),
    ...relativeTileRows(state, seat, state.melds.map((melds) => melds.flatMap((meld) => meld.tiles))),
    ...relativeScalars(state, seat, state.kita_tiles.map((tiles) => tiles.length / 4)),
    ...tileCounts(state.dora_indicators, 5),
    ...tileCounts(state.pending_discard === null ? [] : [state.pending_discard]),
    ...tileCounts([]),
    ...tileCounts([]),
    ...relativeOneHot(state, seat, state.current_seat),
    ...relativeOneHot(state, seat, state.dealer_seat),
    ...relativeFlags(state, seat, []),
    ...relativeFlags(state, seat, state.riichi_seats.flatMap((declared, index) => declared ? [index] : [])),
    ...relativeFlags(state, seat, []),
    ...relativeFlags(state, seat, []),
    ...relativeFlags(state, seat, []),
    ...relativeScalars(state, seat, state.scores.map((points) => points / 1000)),
    ...relativeScalars(state, seat, state.hands.map((hand) => hand.length / 14)),
    state.turn / 100,
    state.honba / 10,
    state.kyotaku / 10,
    state.live_wall_remaining / 136,
    state.dead_wall_remaining / 14,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    ...ROUND_WINDS.map((wind) => Number(state.round_wind === wind)),
    ...relativeOneHot(state, seat, state.pending_discard_seat),
    ...relativeOneHot(state, seat, null),
    ...relativeOneHot(state, seat, null),
    0,
    0
  ];
  if (values.length !== MULTI_ACTION_ONNX_OBSERVATION_DIM) {
    throw new Error("MJSON observation tensor layout drifted");
  }
  return values;
}

function isFullyObservedDirectRequest(state: ReplayBoardState, seat: number): boolean {
  return (
    state.has_complete_round_context &&
    state.current_seat === seat &&
    state.drawn_tiles[seat] !== null &&
    state.pending_discard === null &&
    !state.round_finished &&
    !state.game_finished &&
    !state.riichi_seats.some(Boolean) &&
    state.melds.every((melds) => melds.length === 0) &&
    state.kita_tiles.every((tiles) => tiles.length === 0) &&
    state.hands.every((hand, index) => hand.length === (index === seat ? 14 : 13))
  );
}

function mjsonActionToPolicyAction(
  action: Readonly<Record<string, unknown>>,
  state: ReplayBoardState,
  ruleset: "tenhou-3p" | "tenhou-4p"
): ReplayPolicyAction {
  const type = action.type;
  if (typeof type !== "string") throw new Error("request_action entry must have a type");
  const tileAction = (kind: keyof typeof TILE_ACTION_OFFSETS, tile: string): ReplayPolicyAction => {
    const canonical = canonicalTile(tile);
    validateActionForRuleset(kind, canonical, ruleset);
    return Object.freeze({ kind, tile: canonical, label: `${kind} ${canonical}` });
  };
  switch (type) {
    case "dahai":
    case "discard":
      return tileAction("discard", requiredActionTile(action, state));
    case "chi":
      return tileAction("chi", requiredActionTile(action, state));
    case "pon":
      return tileAction("pon", requiredActionTile(action, state));
    case "daiminkan":
    case "minkan":
      return tileAction("minkan", requiredActionTile(action, state));
    case "ankan":
      return tileAction("ankan", requiredActionTile(action, state));
    case "kakan":
      return tileAction("kakan", requiredActionTile(action, state));
    case "kita":
      return tileAction("kita", requiredActionTile(action, state));
    case "ron":
      return tileAction("ron", requiredActionTile(action, state));
    case "hora":
      return state.pending_discard === null
        ? Object.freeze({ kind: "tsumo", tile: null, label: "tsumo" })
        : tileAction("ron", requiredActionTile(action, state));
    case "none":
    case "pass":
      return Object.freeze({ kind: "pass", tile: null, label: "pass" });
    case "tsumo":
      return Object.freeze({ kind: "tsumo", tile: null, label: "tsumo" });
    case "reach":
    case "riichi":
      return Object.freeze({ kind: "riichi", tile: null, label: "riichi" });
    case "ryukyoku":
    case "kyushu":
      return Object.freeze({ kind: "kyushu", tile: null, label: "kyushu" });
    default:
      throw new Error(`unsupported MJSON policy action: ${type}`);
  }
}

function requiredActionTile(action: Readonly<Record<string, unknown>>, state: ReplayBoardState): string {
  if (typeof action.pai === "string") return action.pai;
  if (state.pending_discard !== null) return state.pending_discard;
  const consumed = action.consumed;
  if (Array.isArray(consumed) && typeof consumed[0] === "string") return consumed[0];
  throw new Error("MJSON tile action requires pai, consumed tile, or a pending discard");
}

function validateActionForRuleset(
  kind: keyof typeof TILE_ACTION_OFFSETS,
  tile: string,
  ruleset: "tenhou-3p" | "tenhou-4p"
): void {
  if (ruleset === "tenhou-3p" && kind === "chi") throw new Error("chi is unavailable in tenhou-3p");
  if (ruleset === "tenhou-4p" && kind === "kita") throw new Error("kita is unavailable in tenhou-4p");
  if (ruleset === "tenhou-3p" && /^\d+m$/.test(tile) && tile !== "1m" && tile !== "9m") {
    throw new Error(`tile is unavailable in tenhou-3p: ${tile}`);
  }
}

function actionIndex(action: ReplayPolicyAction): number {
  if (action.kind in TILE_ACTION_OFFSETS) {
    if (action.tile === null) throw new Error(`${action.kind} requires a tile`);
    return TILE_ACTION_OFFSETS[action.kind as keyof typeof TILE_ACTION_OFFSETS] + tileIndex(action.tile);
  }
  return SINGLE_ACTION_INDICES[action.kind as keyof typeof SINGLE_ACTION_INDICES];
}

function relativeTileRows(
  state: ReplayBoardState,
  seat: number,
  rows: readonly (readonly string[])[]
): number[] {
  const values: number[] = [];
  for (let offset = 0; offset < 4; offset += 1) {
    values.push(...(offset < state.players ? tileCounts(rows[(seat + offset) % state.players]) : Array(34).fill(0)));
  }
  return values;
}

function relativeScalars(state: ReplayBoardState, seat: number, values: readonly number[]): number[] {
  return Array.from({ length: 4 }, (_, offset) => offset < state.players ? values[(seat + offset) % state.players] : 0);
}

function relativeOneHot(state: ReplayBoardState, seat: number, target: number | null): number[] {
  const values = Array(4).fill(0);
  if (target !== null) values[(target - seat + state.players) % state.players] = 1;
  return values;
}

function relativeFlags(state: ReplayBoardState, seat: number, seats: readonly number[]): number[] {
  return Array.from({ length: 4 }, (_, offset) => offset < state.players && seats.includes((seat + offset) % state.players) ? 1 : 0);
}

function tileCounts(tiles: readonly string[], divisor = 4): number[] {
  const counts = Array(34).fill(0);
  for (const tile of tiles) counts[tileIndex(tile)] += 1;
  return counts.map((count) => count / divisor);
}

function canonicalTile(tile: string): string {
  if (tile === "?") throw new Error("MJSON request has hidden tile data");
  const withoutRedSuffix = tile.endsWith("r") ? tile.slice(0, -1) : tile;
  const canonical = /^0[mps]$/.test(withoutRedSuffix) ? `5${withoutRedSuffix[1]}` : withoutRedSuffix;
  if (!TILE_INDEX.has(canonical)) throw new Error(`unsupported MJSON tile: ${tile}`);
  return canonical;
}

function tileIndex(tile: string): number {
  const index = TILE_INDEX.get(canonicalTile(tile));
  if (index === undefined) throw new Error(`unsupported MJSON tile: ${tile}`);
  return index;
}

function readPossibleActions(event: MjsonEvent): readonly Readonly<Record<string, unknown>>[] {
  return Array.isArray(event.possible_actions)
    ? event.possible_actions.filter(isRecord)
    : [];
}

function readSeat(value: unknown): number | null {
  return typeof value === "number" && Number.isInteger(value) && value >= 0 ? value : null;
}

function isRecord(value: unknown): value is Readonly<Record<string, unknown>> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
