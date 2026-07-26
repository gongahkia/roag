import { actionKey, actionLabel, TILE_TYPES } from "./engine";
import {
  GAME_POLICY_KIND,
  type GameAction,
  type GameState,
  type PolicyArtifact,
  type PolicyChoice,
  type PolicyDecision,
  type Tile
} from "./types";

export const DEFAULT_POLICY: PolicyArtifact = Object.freeze({
  kind: GAME_POLICY_KIND,
  version: "local-linear-v1",
  name: "Local shape policy",
  weights: Object.freeze({
    terminalDiscard: 0.74,
    honorDiscard: 0.62,
    isolatedDiscard: 0.44,
    duplicateKeep: 0.56,
    sequenceKeep: 0.38,
    riichi: 2.3,
    chi: 0.82,
    pon: 0.61,
    kan: 0.45,
    pass: 0.35
  })
});

export function decideAction(
  state: GameState,
  legal: readonly GameAction[],
  policy: PolicyArtifact = DEFAULT_POLICY
): PolicyDecision {
  if (legal.length === 0) throw new Error("policy requires legal actions");
  const scored = legal.map((action) => scoreAction(state, action, policy));
  const highest = Math.max(...scored.map((choice) => choice.score));
  const total = scored.reduce((sum, choice) => sum + Math.exp(choice.score - highest), 0);
  const choices = scored
    .map((choice) => Object.freeze({ ...choice, probability: Math.exp(choice.score - highest) / total }))
    .sort((left, right) => right.score - left.score || actionKey(left.action).localeCompare(actionKey(right.action)));
  return Object.freeze({
    policy: Object.freeze({ name: policy.name, version: policy.version }),
    seat: state.currentSeat,
    choices: Object.freeze(choices),
    selected: choices[0]
  });
}

export async function loadPolicyArtifact(url = "./policies/local-shape-policy-v1.json"): Promise<PolicyArtifact> {
  const response = await fetch(url, { cache: "force-cache" });
  if (!response.ok) throw new Error(`policy asset failed to load (${response.status})`);
  return validatePolicyArtifact(await response.json());
}

export function validatePolicyArtifact(value: unknown): PolicyArtifact {
  if (!isRecord(value) || value.kind !== GAME_POLICY_KIND || typeof value.version !== "string" || typeof value.name !== "string" || !isRecord(value.weights)) {
    throw new Error("policy asset does not match the browser policy contract");
  }
  const weightNames = [
    "terminalDiscard", "honorDiscard", "isolatedDiscard", "duplicateKeep", "sequenceKeep",
    "riichi", "chi", "pon", "kan", "pass"
  ] as const;
  const weights = {} as Record<(typeof weightNames)[number], number>;
  for (const name of weightNames) {
    const candidate = value.weights[name];
    if (typeof candidate !== "number" || !Number.isFinite(candidate)) throw new Error(`policy weight is invalid: ${name}`);
    weights[name] = candidate;
  }
  return Object.freeze({
    kind: GAME_POLICY_KIND,
    version: value.version,
    name: value.name,
    weights: Object.freeze(weights)
  });
}

function scoreAction(state: GameState, action: GameAction, policy: PolicyArtifact): Omit<PolicyChoice, "probability"> {
  const rationale: string[] = [];
  let score = deterministicTieBreak(state, action);
  switch (action.kind) {
    case "tsumo":
    case "ron":
      score += 100;
      rationale.push("terminal win is always selected");
      break;
    case "riichi":
      score += policy.weights.riichi;
      rationale.push("closed tenpai receives riichi pressure");
      break;
    case "pass":
      score += policy.weights.pass;
      rationale.push("reaction policy retains hand flexibility");
      break;
    case "chi":
      score += policy.weights.chi + action.consumed.reduce((sum, tile) => sum + tileConnectivity(state.hands[state.currentSeat], tile) * 0.12, 0);
      rationale.push("sequence call scored from local tile connectivity");
      break;
    case "pon":
      score += policy.weights.pon;
      rationale.push("triplet call scored from duplicate consolidation");
      break;
    case "minkan":
    case "ankan":
    case "kakan":
      score += policy.weights.kan;
      rationale.push("kan has a low replacement-draw preference");
      break;
    case "kita":
      score += policy.weights.kan;
      rationale.push("kita exchanges North for a replacement draw");
      break;
    case "discard": {
      const hand = state.hands[state.currentSeat];
      const type = normalizeTile(action.tile);
      const rank = Number(type[0]);
      const honor = !/[mps]$/.test(type);
      const terminal = honor || rank === 1 || rank === 9;
      const copies = count(hand, type);
      const connectivity = tileConnectivity(hand, type);
      if (terminal) {
        score += policy.weights.terminalDiscard;
        rationale.push("terminal or honor carries discard pressure");
      }
      if (honor) {
        score += policy.weights.honorDiscard;
        rationale.push("honor is less sequence-connected");
      }
      if (copies === 1 && connectivity === 0) {
        score += policy.weights.isolatedDiscard;
        rationale.push("isolated singleton is expendable");
      }
      if (copies > 1) {
        score -= policy.weights.duplicateKeep * (copies - 1);
        rationale.push("duplicate structure is retained");
      }
      if (connectivity > 0) {
        score -= policy.weights.sequenceKeep * connectivity;
        rationale.push("sequence adjacency is retained");
      }
      break;
    }
  }
  return Object.freeze({ action, score, probability: 0, rationale: Object.freeze(rationale.length > 0 ? rationale : [actionLabel(action)]) });
}

function tileConnectivity(hand: readonly Tile[], tile: Tile): number {
  const normalized = normalizeTile(tile);
  if (!/[mps]$/.test(normalized)) return 0;
  const rank = Number(normalized[0]);
  const suit = normalized[1];
  return [-2, -1, 1, 2]
    .filter((offset) => rank + offset >= 1 && rank + offset <= 9)
    .reduce((sum, offset) => sum + count(hand, `${rank + offset}${suit}`), 0);
}

function deterministicTieBreak(state: GameState, action: GameAction): number {
  const source = `${state.seed}:${state.turn}:${state.currentSeat}:${actionKey(action)}`;
  let value = 0;
  for (const character of source) value = Math.imul(value ^ character.charCodeAt(0), 16_777_619);
  return ((value >>> 0) / 4_294_967_296) * 0.0001;
}

function normalizeTile(tile: Tile): Tile {
  return tile.startsWith("0") ? `5${tile.slice(-1)}` : tile;
}

function count(hand: readonly Tile[], target: Tile): number {
  return hand.filter((tile) => normalizeTile(tile) === target).length;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function policyActionIndex(action: GameAction): number {
  const offsets = { discard: 0, ron: 34, chi: 68, pon: 102, minkan: 136, ankan: 170, kakan: 204, kita: 238 } as const;
  if (action.kind === "discard" || action.kind === "ron" || action.kind === "ankan" || action.kind === "kakan" || action.kind === "kita") {
    const tile = action.kind === "ron" ? "1m" : action.tile;
    return offsets[action.kind] + Math.max(0, TILE_TYPES.indexOf(normalizeTile(tile) as typeof TILE_TYPES[number]));
  }
  if (action.kind === "chi" || action.kind === "pon" || action.kind === "minkan") {
    return offsets[action.kind] + Math.max(0, TILE_TYPES.indexOf(normalizeTile(action.consumed[0] ?? "1m") as typeof TILE_TYPES[number]));
  }
  if (action.kind === "pass") return 272;
  if (action.kind === "tsumo") return 273;
  return 274;
}
