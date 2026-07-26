import {
  GAME_SAVE_KIND,
  GAME_STATE_KIND,
  type GameAction,
  type GameConfig,
  type GameEvent,
  type GameFrame,
  type GameMeld,
  type GameSave,
  type GameSnapshot,
  type GameState,
  type PendingDiscard,
  type PlayerCount,
  type ScoreLine,
  type ScoreSummary,
  type TerminalResult,
  type Tile,
  type Wind
} from "./types";

export const TILE_TYPES = [
  "1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m",
  "1p", "2p", "3p", "4p", "5p", "6p", "7p", "8p", "9p",
  "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s",
  "E", "S", "W", "N", "P", "F", "C"
] as const;

const TERMINALS_AND_HONORS = new Set(["1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W", "N", "P", "F", "C"]);
const WINDS: readonly Wind[] = ["E", "S", "W", "N"];
const HONORS = ["E", "S", "W", "N", "P", "F", "C"] as const;
const TILE_INDEX = new Map<string, number>(TILE_TYPES.map((tile, index) => [tile, index]));

export function createGame(config: GameConfig): GameState {
  assertConfig(config);
  const random = createRandom(config.seed);
  const deck = shuffle(buildWall(config.players), random);
  const deadWall = deck.splice(0, 14);
  const hands = Array.from({ length: config.players }, () => [] as Tile[]);
  for (let tileIndex = 0; tileIndex < 13; tileIndex += 1) {
    for (let seat = 0; seat < config.players; seat += 1) hands[seat].push(popTile(deck));
  }
  const names = Array.from({ length: config.players }, (_, seat) => config.names?.[seat] || defaultName(seat, config.humanSeat));
  const state: GameState = {
    kind: GAME_STATE_KIND,
    version: 0,
    id: `game-${hash(config.seed).toString(16).padStart(8, "0")}`,
    seed: config.seed,
    players: config.players,
    humanSeat: config.humanSeat,
    names: freezeArray(names),
    phase: "draw",
    roundWind: "E",
    handNumber: 1,
    dealerSeat: 0,
    currentSeat: 0,
    turn: 0,
    wall: freezeArray(deck),
    deadWall: freezeArray(deadWall),
    doraIndicators: freezeArray(deadWall.slice(4, 5)),
    hands: freezeNested(hands.map(sortTiles)),
    discards: freezeNested(emptySeatArrays(config.players)),
    melds: freezeNestedMelds(emptyMeldArrays(config.players)),
    kitaTiles: freezeNested(emptySeatArrays(config.players)),
    points: freezeArray(Array.from({ length: config.players }, () => config.players === 3 ? 35000 : 25000)),
    riichiSeats: freezeArray(Array.from({ length: config.players }, () => false)),
    riichiSticks: 0,
    drawnTile: null,
    pendingDiscard: null,
    terminal: null,
    history: freezeArray([]),
    timeline: freezeArray([])
  };
  const started = transition(state, {}, {
    kind: "game_started",
    seat: null,
    action: null,
    message: `${config.players}-player local table seeded ${config.seed}.`
  });
  return drawForCurrentSeat(started);
}

export function legalActions(state: GameState, seat = state.currentSeat): readonly GameAction[] {
  if (state.phase === "terminal" || seat !== state.currentSeat) return freezeArray([]);
  if (state.phase === "discard") return legalTurnActions(state, seat);
  if (state.phase === "reaction") return legalReactionActions(state, seat);
  return freezeArray([]);
}

export function applyAction(state: GameState, action: GameAction): GameState {
  if (state.phase === "terminal") throw new Error("game is terminal");
  const legal = legalActions(state);
  if (!legal.some((candidate) => actionKey(candidate) === actionKey(action))) {
    throw new Error(`illegal action: ${actionKey(action)}`);
  }
  switch (action.kind) {
    case "discard":
      return applyDiscard(state, action.tile);
    case "riichi":
      return applyRiichi(state);
    case "tsumo":
      return applyTsumo(state);
    case "ron":
      return applyRon(state);
    case "pass":
      return applyPass(state);
    case "chi":
    case "pon":
    case "minkan":
      return applyCall(state, action);
    case "ankan":
      return applyKan(state, action.tile, "ankan");
    case "kakan":
      return applyKan(state, action.tile, "kakan");
    case "kita":
      return applyKita(state, action.tile);
  }
}

export function advanceAutomated(
  state: GameState,
  choose: (current: GameState, legal: readonly GameAction[]) => GameAction,
  options: { readonly includeHuman?: boolean; readonly limit?: number } = {}
): GameState {
  const includeHuman = options.includeHuman ?? false;
  const limit = options.limit ?? 512;
  let current = state;
  for (let step = 0; step < limit; step += 1) {
    if (current.phase === "terminal") return current;
    if (current.phase === "draw") {
      current = drawForCurrentSeat(current);
      continue;
    }
    if (!includeHuman && current.currentSeat === current.humanSeat) return current;
    const legal = legalActions(current);
    if (legal.length === 0) throw new Error("active browser game state has no legal action");
    current = applyAction(current, choose(current, legal));
  }
  throw new Error("automated game progression exceeded the action limit");
}

export function scoreRound(
  state: GameState,
  input: { readonly winner: number; readonly fromSeat: number | null; readonly winKind: "tsumo" | "ron" }
): ScoreSummary | null {
  const winnerTiles = [
    ...state.hands[input.winner],
    ...(input.winKind === "ron" && state.pendingDiscard ? [state.pendingDiscard.tile] : [])
  ];
  const melds = state.melds[input.winner];
  if (!isWinningShape(winnerTiles, melds)) return null;
  const yaku = scoreLines(state, input.winner, winnerTiles, melds, input.winKind);
  const han = yaku.reduce((total, line) => total + line.han, 0);
  if (han === 0) return null;
  const fu = isSevenPairs(winnerTiles, melds) ? 25 : 30;
  const basePoints = basePointValue(han, fu);
  const payments = Array.from({ length: state.players }, () => 0);
  if (input.winKind === "ron") {
    if (input.fromSeat === null) return null;
    const multiplier = input.winner === state.dealerSeat ? 6 : 4;
    const payment = roundUp100(basePoints * multiplier) + state.handNumber * 0;
    payments[input.fromSeat] -= payment;
    payments[input.winner] += payment + state.riichiSticks * 1000;
  } else {
    for (let seat = 0; seat < state.players; seat += 1) {
      if (seat === input.winner) continue;
      const multiplier = input.winner === state.dealerSeat ? 2 : seat === state.dealerSeat ? 2 : 1;
      const payment = roundUp100(basePoints * multiplier);
      payments[seat] -= payment;
      payments[input.winner] += payment;
    }
    payments[input.winner] += state.riichiSticks * 1000;
  }
  return Object.freeze({
    winner: input.winner,
    fromSeat: input.fromSeat,
    winKind: input.winKind,
    yaku: freezeArray(yaku),
    han,
    fu,
    basePoints,
    payments: freezeArray(payments),
    total: payments[input.winner]
  });
}

export function serializeGame(state: GameState): GameSave {
  return Object.freeze({ kind: GAME_SAVE_KIND, savedAt: Date.now(), state: cloneState(state) });
}

export function deserializeGame(value: unknown): GameState {
  if (!isRecord(value) || value.kind !== GAME_SAVE_KIND || !isRecord(value.state)) {
    throw new Error("invalid browser game save");
  }
  const state = value.state as unknown as GameState;
  if (state.kind !== GAME_STATE_KIND || !isPlayerCount(state.players) || !Number.isInteger(state.humanSeat)) {
    throw new Error("invalid browser game state");
  }
  if (state.humanSeat < 0 || state.humanSeat >= state.players || !Array.isArray(state.hands)) {
    throw new Error("invalid browser game player state");
  }
  return cloneState({ ...state, timeline: Array.isArray(state.timeline) ? state.timeline : [] });
}

export function gameStateDigest(state: GameState): string {
  const material = JSON.stringify({
    version: state.version,
    seed: state.seed,
    phase: state.phase,
    currentSeat: state.currentSeat,
    turn: state.turn,
    wall: state.wall,
    hands: state.hands,
    discards: state.discards,
    melds: state.melds,
    points: state.points,
    pendingDiscard: state.pendingDiscard,
    terminal: state.terminal
  });
  return hash(material).toString(16).padStart(8, "0");
}

export function restoreTimelineFrame(state: GameState, index: number): GameState {
  if (!Number.isInteger(index) || index < 0 || index >= state.timeline.length) {
    throw new Error("timeline frame is outside the local game history");
  }
  const timeline = state.timeline.slice(0, index + 1);
  const snapshot = cloneSnapshot(timeline[index].state);
  return Object.freeze({
    ...snapshot,
    history: freezeArray(timeline.map((frame) => cloneEvent(frame.event))),
    timeline: freezeArray(timeline.map((frame) => Object.freeze({ event: cloneEvent(frame.event), state: cloneSnapshot(frame.state) })))
  });
}

export function undoLastHumanAction(state: GameState): GameState {
  for (let index = state.timeline.length - 1; index >= 0; index -= 1) {
    const event = state.timeline[index].event;
    if (event.seat === state.humanSeat && event.action !== null) {
      return index === 0 ? state : restoreTimelineFrame(state, index - 1);
    }
  }
  return state;
}

export function actionKey(action: GameAction): string {
  switch (action.kind) {
    case "discard":
    case "ankan":
    case "kakan":
    case "kita":
      return `${action.kind}:${action.tile}`;
    case "chi":
    case "pon":
    case "minkan":
      return `${action.kind}:${[...action.consumed].sort().join(",")}`;
    default:
      return action.kind;
  }
}

export function actionLabel(action: GameAction): string {
  switch (action.kind) {
    case "discard": return `Discard ${action.tile}`;
    case "ankan": return `Closed kan ${action.tile}`;
    case "kakan": return `Added kan ${action.tile}`;
    case "kita": return `Kita ${action.tile}`;
    case "chi": return `Chi ${action.consumed.join(" ")}`;
    case "pon": return `Pon ${action.consumed[0]}`;
    case "minkan": return `Open kan ${action.consumed[0]}`;
    default: return action.kind[0].toUpperCase() + action.kind.slice(1);
  }
}

export function tileGlyph(tile: Tile): string {
  if (tile === "E") return "東";
  if (tile === "S") return "南";
  if (tile === "W") return "西";
  if (tile === "N") return "北";
  if (tile === "P") return "白";
  if (tile === "F") return "發";
  if (tile === "C") return "中";
  const suit = tile.slice(-1);
  const rank = tile.slice(0, -1);
  return `${rank}${suit === "m" ? "萬" : suit === "p" ? "筒" : "索"}`;
}

function legalTurnActions(state: GameState, seat: number): readonly GameAction[] {
  const hand = state.hands[seat];
  const actions: GameAction[] = uniqueTiles(hand).map((tile) => ({ kind: "discard", tile }));
  if (scoreRound(state, { winner: seat, fromSeat: null, winKind: "tsumo" }) !== null) actions.push({ kind: "tsumo" });
  if (canDeclareRiichi(state, seat)) actions.push({ kind: "riichi" });
  for (const tile of uniqueTiles(hand)) {
    if (countTile(hand, tile) === 4) actions.push({ kind: "ankan", tile });
    if (state.players === 3 && tile === "N") actions.push({ kind: "kita", tile });
    if (state.melds[seat].some((meld) => meld.kind === "pon" && sameTile(meld.tiles[0], tile))) {
      actions.push({ kind: "kakan", tile });
    }
  }
  return freezeArray(actions);
}

function legalReactionActions(state: GameState, seat: number): readonly GameAction[] {
  const pending = state.pendingDiscard;
  if (pending === null || pending.reactionSeats[0] !== seat) return freezeArray([]);
  const hand = state.hands[seat];
  const actions: GameAction[] = [{ kind: "pass" }];
  if (scoreRound(state, { winner: seat, fromSeat: pending.fromSeat, winKind: "ron" }) !== null) {
    actions.unshift({ kind: "ron" });
  }
  if (countTile(hand, pending.tile) >= 2) actions.push({ kind: "pon", consumed: takeTiles(hand, pending.tile, 2) });
  if (countTile(hand, pending.tile) >= 3) actions.push({ kind: "minkan", consumed: takeTiles(hand, pending.tile, 3) });
  if (state.players === 4 && seat === nextSeat(pending.fromSeat, state.players)) actions.push(...chiActions(hand, pending.tile));
  return freezeArray(actions);
}

function applyDiscard(state: GameState, tile: Tile): GameState {
  const hands = cloneNested(state.hands);
  removeTile(hands[state.currentSeat], tile);
  const discards = cloneNested(state.discards);
  discards[state.currentSeat].push(tile);
  const next = nextSeat(state.currentSeat, state.players);
  const pending: PendingDiscard = {
    tile,
    fromSeat: state.currentSeat,
    reactionSeats: freezeArray(reactionOrder(state.currentSeat, state.players))
  };
  return transition(state, {
    phase: "reaction",
    currentSeat: pending.reactionSeats[0],
    turn: state.turn + 1,
    hands: freezeNested(hands.map(sortTiles)),
    discards: freezeNested(discards),
    drawnTile: null,
    pendingDiscard: pending
  }, {
    kind: "discard",
    seat: state.currentSeat,
    action: { kind: "discard", tile },
    message: `${state.names[state.currentSeat]} discards ${tileGlyph(tile)}; reaction window opens toward seat ${next}.`
  });
}

function applyRiichi(state: GameState): GameState {
  const points = [...state.points];
  const riichiSeats = [...state.riichiSeats];
  points[state.currentSeat] -= 1000;
  riichiSeats[state.currentSeat] = true;
  return transition(state, { points: freezeArray(points), riichiSeats: freezeArray(riichiSeats), riichiSticks: state.riichiSticks + 1 }, {
    kind: "riichi",
    seat: state.currentSeat,
    action: { kind: "riichi" },
    message: `${state.names[state.currentSeat]} declares riichi. Choose the declaration discard.`
  });
}

function applyTsumo(state: GameState): GameState {
  const score = scoreRound(state, { winner: state.currentSeat, fromSeat: null, winKind: "tsumo" });
  if (score === null) throw new Error("tsumo requires a scored winning hand");
  return applyWin(state, score, "tsumo", { kind: "tsumo" });
}

function applyRon(state: GameState): GameState {
  const pending = state.pendingDiscard;
  if (pending === null) throw new Error("ron requires a pending discard");
  const score = scoreRound(state, { winner: state.currentSeat, fromSeat: pending.fromSeat, winKind: "ron" });
  if (score === null) throw new Error("ron requires a scored winning hand");
  return applyWin(state, score, "ron", { kind: "ron" });
}

function applyWin(state: GameState, score: ScoreSummary, reason: "tsumo" | "ron", action: GameAction): GameState {
  const points = state.points.map((value, seat) => value + score.payments[seat]);
  const terminal: TerminalResult = { reason, score };
  return transition(state, {
    phase: "terminal",
    points: freezeArray(points),
    riichiSticks: 0,
    terminal,
    pendingDiscard: null
  }, {
    kind: "win",
    seat: score.winner,
    action,
    message: `${state.names[score.winner]} wins by ${reason}: ${score.han} han / ${score.fu} fu / ${score.total} points.`
  });
}

function applyPass(state: GameState): GameState {
  const pending = state.pendingDiscard;
  if (pending === null) throw new Error("pass requires a pending discard");
  const remaining = pending.reactionSeats.slice(1);
  if (remaining.length > 0) {
    return transition(state, {
      currentSeat: remaining[0],
      pendingDiscard: { ...pending, reactionSeats: freezeArray(remaining) }
    }, {
      kind: "pass",
      seat: state.currentSeat,
      action: { kind: "pass" },
      message: `${state.names[state.currentSeat]} passes.`
    });
  }
  return transition(state, {
    phase: "draw",
    currentSeat: nextSeat(pending.fromSeat, state.players),
    pendingDiscard: null
  }, {
    kind: "pass",
    seat: state.currentSeat,
    action: { kind: "pass" },
    message: "All reactions pass; the wall advances."
  });
}

function applyCall(state: GameState, action: Extract<GameAction, { readonly kind: "chi" | "pon" | "minkan" }>): GameState {
  const pending = state.pendingDiscard;
  if (pending === null) throw new Error("call requires a pending discard");
  const hands = cloneNested(state.hands);
  for (const tile of action.consumed) removeTile(hands[state.currentSeat], tile);
  const discards = cloneNested(state.discards);
  discards[pending.fromSeat].pop();
  const melds = cloneMelds(state.melds);
  melds[state.currentSeat].push({ kind: action.kind, tiles: freezeArray([...action.consumed, pending.tile]), fromSeat: pending.fromSeat });
  const drawAfterCall = action.kind === "minkan";
  return transition(state, {
    phase: drawAfterCall ? "draw" : "discard",
    hands: freezeNested(hands.map(sortTiles)),
    discards: freezeNested(discards),
    melds: freezeNestedMelds(melds),
    currentSeat: state.currentSeat,
    drawnTile: null,
    pendingDiscard: null,
    doraIndicators: drawAfterCall ? revealDora(state) : state.doraIndicators
  }, {
    kind: "call",
    seat: state.currentSeat,
    action,
    message: `${state.names[state.currentSeat]} calls ${action.kind} on ${tileGlyph(pending.tile)}.`
  });
}

function applyKan(state: GameState, tile: Tile, kind: "ankan" | "kakan"): GameState {
  const hands = cloneNested(state.hands);
  const melds = cloneMelds(state.melds);
  if (kind === "ankan") {
    const consumed = takeTiles(hands[state.currentSeat], tile, 4);
    for (const consumedTile of consumed) removeTile(hands[state.currentSeat], consumedTile);
    melds[state.currentSeat].push({ kind, tiles: freezeArray(consumed), fromSeat: null });
  } else {
    removeTile(hands[state.currentSeat], tile);
    const meldIndex = melds[state.currentSeat].findIndex((meld) => meld.kind === "pon" && sameTile(meld.tiles[0], tile));
    if (meldIndex < 0) throw new Error("kakan requires a matching pon");
    const pon = melds[state.currentSeat][meldIndex];
    melds[state.currentSeat][meldIndex] = { kind, tiles: freezeArray([...pon.tiles, tile]), fromSeat: pon.fromSeat };
  }
  return transition(state, {
    phase: "draw",
    hands: freezeNested(hands.map(sortTiles)),
    melds: freezeNestedMelds(melds),
    drawnTile: null,
    doraIndicators: revealDora(state)
  }, {
    kind: "call",
    seat: state.currentSeat,
    action: { kind, tile },
    message: `${state.names[state.currentSeat]} declares ${kind} on ${tileGlyph(tile)} and draws replacement.`
  });
}

function applyKita(state: GameState, tile: Tile): GameState {
  const hands = cloneNested(state.hands);
  const kitaTiles = cloneNested(state.kitaTiles);
  removeTile(hands[state.currentSeat], tile);
  kitaTiles[state.currentSeat].push(tile);
  return transition(state, {
    phase: "draw",
    hands: freezeNested(hands.map(sortTiles)),
    kitaTiles: freezeNested(kitaTiles),
    drawnTile: null
  }, {
    kind: "call",
    seat: state.currentSeat,
    action: { kind: "kita", tile },
    message: `${state.names[state.currentSeat]} exposes North as kita and draws replacement.`
  });
}

function drawForCurrentSeat(state: GameState): GameState {
  if (state.wall.length === 0) {
    const terminal: TerminalResult = { reason: "exhaustive_draw", score: null };
    return transition(state, { phase: "terminal", terminal, pendingDiscard: null }, {
      kind: "draw_game",
      seat: null,
      action: null,
      message: "Live wall exhausted. Round ends in exhaustive draw."
    });
  }
  const wall = [...state.wall];
  const tile = popTile(wall);
  const hands = cloneNested(state.hands);
  hands[state.currentSeat].push(tile);
  return transition(state, {
    phase: "discard",
    wall: freezeArray(wall),
    hands: freezeNested(hands.map(sortTiles)),
    drawnTile: tile,
    pendingDiscard: null
  }, {
    kind: "draw",
    seat: state.currentSeat,
    action: null,
    message: `${state.names[state.currentSeat]} draws ${state.currentSeat === state.humanSeat ? tileGlyph(tile) : "a tile"}.`
  });
}

function canDeclareRiichi(state: GameState, seat: number): boolean {
  if (state.riichiSeats[seat] || state.points[seat] < 1000 || !isClosed(state.melds[seat])) return false;
  for (const discard of uniqueTiles(state.hands[seat])) {
    const remaining = [...state.hands[seat]];
    removeTile(remaining, discard);
    if (TILE_TYPES.some((tile) => isWinningShape([...remaining, tile], state.melds[seat]))) return true;
  }
  return false;
}

function scoreLines(
  state: GameState,
  seat: number,
  hand: readonly Tile[],
  melds: readonly GameMeld[],
  winKind: "tsumo" | "ron"
): ScoreLine[] {
  const allTiles = [...hand, ...melds.flatMap((meld) => meld.tiles)];
  const lines: ScoreLine[] = [];
  if (state.riichiSeats[seat]) lines.push({ name: "Riichi", han: 1 });
  if (winKind === "tsumo" && isClosed(melds)) lines.push({ name: "Menzen tsumo", han: 1 });
  if (allTiles.every((tile) => !TERMINALS_AND_HONORS.has(normalizeTile(tile)))) lines.push({ name: "Tanyao", han: 1 });
  if (isSevenPairs(hand, melds)) lines.push({ name: "Chiitoitsu", han: 2 });
  if (isAllTriplets(hand, melds)) lines.push({ name: "Toitoi", han: 2 });
  const suits = new Set(allTiles.filter(isNumberTile).map((tile) => tile.slice(-1)));
  const hasHonor = allTiles.some((tile) => !isNumberTile(tile));
  if (suits.size === 1 && hasHonor) lines.push({ name: "Honitsu", han: isClosed(melds) ? 3 : 2 });
  if (suits.size === 1 && !hasHonor) lines.push({ name: "Chinitsu", han: isClosed(melds) ? 6 : 5 });
  for (const honor of ["P", "F", "C", WINDS[seat], state.roundWind]) {
    if (countTile(allTiles, honor) >= 3) lines.push({ name: `Yakuhai ${honor}`, han: 1 });
  }
  for (const indicator of state.doraIndicators) {
    const dora = nextDora(indicator);
    const count = countTile(allTiles, dora) + state.kitaTiles[seat].length;
    if (count > 0) lines.push({ name: "Dora", han: count });
  }
  return lines;
}

function isWinningShape(hand: readonly Tile[], melds: readonly GameMeld[]): boolean {
  if (isSevenPairs(hand, melds) || isKokushi(hand, melds)) return true;
  return isStandardWinning(hand, melds.length);
}

function isSevenPairs(hand: readonly Tile[], melds: readonly GameMeld[]): boolean {
  if (melds.length > 0 || hand.length !== 14) return false;
  return uniqueTiles(hand).length === 7 && uniqueTiles(hand).every((tile) => countTile(hand, tile) === 2);
}

function isKokushi(hand: readonly Tile[], melds: readonly GameMeld[]): boolean {
  if (melds.length > 0 || hand.length !== 14) return false;
  const unique = new Set(hand.map(normalizeTile));
  return TERMINALS_AND_HONORS.size === 13 && [...TERMINALS_AND_HONORS].every((tile) => unique.has(tile)) && unique.size === 13;
}

function isStandardWinning(hand: readonly Tile[], meldCount: number): boolean {
  if (hand.length !== 2 + 3 * (4 - meldCount)) return false;
  const counts = tileCounts(hand);
  for (let pair = 0; pair < counts.length; pair += 1) {
    if (counts[pair] < 2) continue;
    const next = [...counts];
    next[pair] -= 2;
    if (canMakeGroups(next)) return true;
  }
  return false;
}

function canMakeGroups(counts: number[]): boolean {
  const first = counts.findIndex((count) => count > 0);
  if (first < 0) return true;
  if (counts[first] >= 3) {
    counts[first] -= 3;
    if (canMakeGroups(counts)) return true;
    counts[first] += 3;
  }
  if (first < 27 && first % 9 <= 6 && counts[first + 1] > 0 && counts[first + 2] > 0) {
    counts[first] -= 1;
    counts[first + 1] -= 1;
    counts[first + 2] -= 1;
    if (canMakeGroups(counts)) return true;
    counts[first] += 1;
    counts[first + 1] += 1;
    counts[first + 2] += 1;
  }
  return false;
}

function isAllTriplets(hand: readonly Tile[], melds: readonly GameMeld[]): boolean {
  if (melds.some((meld) => meld.kind === "chi")) return false;
  if (hand.length !== 2 + 3 * (4 - melds.length)) return false;
  return uniqueTiles(hand).filter((tile) => countTile(hand, tile) % 3 !== 0).length === 1;
}

function chiActions(hand: readonly Tile[], pendingTile: Tile): GameAction[] {
  if (!isNumberTile(pendingTile)) return [];
  const suit = pendingTile.slice(-1);
  const rank = Number(pendingTile[0]);
  const actions: GameAction[] = [];
  for (const start of [rank - 2, rank - 1, rank]) {
    if (start < 1 || start + 2 > 9) continue;
    const sequence = [start, start + 1, start + 2].map((value) => `${value}${suit}`);
    const consumed = sequence.filter((tile) => !sameTile(tile, pendingTile));
    if (consumed.every((tile) => countTile(hand, tile) >= 1)) actions.push({ kind: "chi", consumed: freezeArray(consumed) });
  }
  return actions;
}

function revealDora(state: GameState): readonly Tile[] {
  const next = state.deadWall[4 + state.doraIndicators.length];
  return next ? freezeArray([...state.doraIndicators, next]) : state.doraIndicators;
}

function transition(
  state: GameState,
  updates: Partial<Omit<GameState, "version" | "history" | "kind" | "id">>,
  event: Omit<GameEvent, "id" | "version">
): GameState {
  const version = state.version + 1;
  const nextEvent: GameEvent = Object.freeze({
    ...event,
    id: `${state.id}:${String(version).padStart(6, "0")}`,
    version
  });
  const next = { ...state, ...updates, version, history: freezeArray([...state.history, nextEvent]) };
  const frame: GameFrame = Object.freeze({ event: nextEvent, state: cloneSnapshot(next) });
  return Object.freeze({ ...next, timeline: freezeArray([...state.timeline, frame]) });
}

function cloneState(state: GameState): GameState {
  return Object.freeze({
    ...cloneSnapshot(state),
    history: freezeArray(state.history.map(cloneEvent)),
    timeline: freezeArray(state.timeline.map((frame) => Object.freeze({ event: cloneEvent(frame.event), state: cloneSnapshot(frame.state) })))
  });
}

function cloneSnapshot(state: GameSnapshot): GameSnapshot {
  return Object.freeze({
    ...state,
    names: freezeArray(state.names),
    wall: freezeArray(state.wall),
    deadWall: freezeArray(state.deadWall),
    doraIndicators: freezeArray(state.doraIndicators),
    hands: freezeNested(state.hands),
    discards: freezeNested(state.discards),
    melds: freezeNestedMelds(state.melds),
    kitaTiles: freezeNested(state.kitaTiles),
    points: freezeArray(state.points),
    riichiSeats: freezeArray(state.riichiSeats),
    pendingDiscard: state.pendingDiscard === null ? null : Object.freeze({ ...state.pendingDiscard, reactionSeats: freezeArray(state.pendingDiscard.reactionSeats) }),
    terminal: state.terminal === null ? null : Object.freeze({ ...state.terminal, score: cloneScore(state.terminal.score) })
  });
}

function cloneEvent(event: GameEvent): GameEvent {
  return Object.freeze({ ...event, action: event.action === null ? null : cloneAction(event.action) });
}

function cloneScore(score: ScoreSummary | null): ScoreSummary | null {
  if (score === null) return null;
  return Object.freeze({ ...score, yaku: freezeArray(score.yaku.map((line) => Object.freeze({ ...line }))), payments: freezeArray(score.payments) });
}

function cloneAction(action: GameAction): GameAction {
  if (action.kind === "chi" || action.kind === "pon" || action.kind === "minkan") return Object.freeze({ ...action, consumed: freezeArray(action.consumed) });
  return Object.freeze({ ...action });
}

function buildWall(players: PlayerCount): Tile[] {
  const excluded = players === 3 ? new Set(["2m", "3m", "4m", "5m", "6m", "7m", "8m"]) : new Set<string>();
  return TILE_TYPES.flatMap((tile) => excluded.has(tile) ? [] : [tile, tile, tile, tile]);
}

function assertConfig(config: GameConfig): void {
  if (!isPlayerCount(config.players)) throw new Error("players must be 3 or 4");
  if (!Number.isInteger(config.humanSeat) || config.humanSeat < 0 || config.humanSeat >= config.players) throw new Error("humanSeat must be a player seat");
  if (!config.seed.trim()) throw new Error("seed must not be empty");
}

function isPlayerCount(value: unknown): value is PlayerCount {
  return value === 3 || value === 4;
}

function defaultName(seat: number, humanSeat: number): string {
  return seat === humanSeat ? "You" : ["Shimocha", "Toimen", "Kamicha"][seat > humanSeat ? seat - 1 : seat] || `Model ${seat}`;
}

function createRandom(seed: string): () => number {
  let value = hash(seed) || 1;
  return () => {
    value |= 0;
    value = value + 0x6d2b79f5 | 0;
    let mixed = Math.imul(value ^ value >>> 15, 1 | value);
    mixed = mixed + Math.imul(mixed ^ mixed >>> 7, 61 | mixed) ^ mixed;
    return ((mixed ^ mixed >>> 14) >>> 0) / 4_294_967_296;
  };
}

function shuffle<T>(items: readonly T[], random: () => number): T[] {
  const next = [...items];
  for (let index = next.length - 1; index > 0; index -= 1) {
    const target = Math.floor(random() * (index + 1));
    [next[index], next[target]] = [next[target], next[index]];
  }
  return next;
}

function hash(value: string): number {
  let result = 2_166_136_261;
  for (const character of value) {
    result ^= character.charCodeAt(0);
    result = Math.imul(result, 16_777_619);
  }
  return result >>> 0;
}

function popTile(wall: Tile[]): Tile {
  const tile = wall.pop();
  if (tile === undefined) throw new Error("wall is empty");
  return tile;
}

function nextSeat(seat: number, players: PlayerCount): number {
  return (seat + 1) % players;
}

function reactionOrder(discarder: number, players: PlayerCount): number[] {
  return Array.from({ length: players - 1 }, (_, index) => (discarder + index + 1) % players);
}

function tileCounts(tiles: readonly Tile[]): number[] {
  const counts = Array.from({ length: TILE_TYPES.length }, () => 0);
  for (const tile of tiles) {
    const index = TILE_INDEX.get(normalizeTile(tile));
    if (index !== undefined) counts[index] += 1;
  }
  return counts;
}

function countTile(tiles: readonly Tile[], tile: Tile): number {
  return tiles.filter((candidate) => sameTile(candidate, tile)).length;
}

function takeTiles(tiles: readonly Tile[], tile: Tile, count: number): Tile[] {
  const result = tiles.filter((candidate) => sameTile(candidate, tile)).slice(0, count);
  if (result.length !== count) throw new Error(`missing ${count} copies of ${tile}`);
  return result;
}

function removeTile(tiles: Tile[], tile: Tile): void {
  const index = tiles.findIndex((candidate) => sameTile(candidate, tile));
  if (index < 0) throw new Error(`tile not found: ${tile}`);
  tiles.splice(index, 1);
}

function uniqueTiles(tiles: readonly Tile[]): Tile[] {
  return [...new Map(tiles.map((tile) => [normalizeTile(tile), tile])).values()].sort(tileCompare);
}

function normalizeTile(tile: Tile): Tile {
  return tile.startsWith("0") ? `5${tile.slice(-1)}` : tile;
}

function sameTile(left: Tile, right: Tile): boolean {
  return normalizeTile(left) === normalizeTile(right);
}

function sortTiles(tiles: readonly Tile[]): Tile[] {
  return [...tiles].sort(tileCompare);
}

function tileCompare(left: Tile, right: Tile): number {
  return (TILE_INDEX.get(normalizeTile(left)) ?? 99) - (TILE_INDEX.get(normalizeTile(right)) ?? 99) || left.localeCompare(right);
}

function isNumberTile(tile: Tile): boolean {
  return /^[0-9][mps]$/.test(tile);
}

function isClosed(melds: readonly GameMeld[]): boolean {
  return melds.every((meld) => meld.kind === "ankan");
}

function nextDora(indicator: Tile): Tile {
  const normalized = normalizeTile(indicator);
  if (HONORS.includes(normalized as (typeof HONORS)[number])) {
    if (["E", "S", "W", "N"].includes(normalized)) return WINDS[(WINDS.indexOf(normalized as Wind) + 1) % WINDS.length];
    const dragons = ["P", "F", "C"];
    return dragons[(dragons.indexOf(normalized) + 1) % dragons.length];
  }
  const rank = Number(normalized[0]);
  return `${rank === 9 ? 1 : rank + 1}${normalized[1]}`;
}

function basePointValue(han: number, fu: number): number {
  if (han >= 13) return 8000;
  if (han >= 11) return 6000;
  if (han >= 8) return 4000;
  if (han >= 6) return 3000;
  const raw = fu * 2 ** (han + 2);
  return han >= 5 || raw >= 2000 ? 2000 : raw;
}

function roundUp100(value: number): number {
  return Math.ceil(value / 100) * 100;
}

function emptySeatArrays(players: PlayerCount): Tile[][] {
  return Array.from({ length: players }, () => []);
}

function emptyMeldArrays(players: PlayerCount): GameMeld[][] {
  return Array.from({ length: players }, () => []);
}

function cloneNested(values: readonly (readonly Tile[])[]): Tile[][] {
  return values.map((tiles) => [...tiles]);
}

function cloneMelds(values: readonly (readonly GameMeld[])[]): GameMeld[][] {
  return values.map((melds) => melds.map((meld) => ({ ...meld, tiles: [...meld.tiles] })));
}

function freezeArray<T>(values: readonly T[]): readonly T[] {
  return Object.freeze([...values]);
}

function freezeNested(values: readonly (readonly Tile[])[]): readonly (readonly Tile[])[] {
  return Object.freeze(values.map((tiles) => freezeArray(tiles)));
}

function freezeNestedMelds(values: readonly (readonly GameMeld[])[]): readonly (readonly GameMeld[])[] {
  return Object.freeze(
    values.map((melds) => freezeArray(melds.map((meld) => Object.freeze({ ...meld, tiles: freezeArray(meld.tiles) }))))
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
