import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

import {
  actionKey,
  advanceAutomated,
  applyAction,
  createGame,
  deserializeGame,
  gameStateDigest,
  legalActions,
  restoreTimelineFrame,
  scoreRound,
  serializeGame,
  undoLastHumanAction
} from "../src/game/engine";
import { analyzeHumanDecisions } from "../src/game/analysis";
import { decideAction, DEFAULT_POLICY, validatePolicyArtifact } from "../src/game/policy";
import type { GameState } from "../src/game/types";

test("createGame is deterministic and deals a complete browser-owned table", () => {
  const first = createGame({ players: 4, humanSeat: 0, seed: "deterministic-browser-table" });
  const second = createGame({ players: 4, humanSeat: 0, seed: "deterministic-browser-table" });
  assert.equal(gameStateDigest(first), gameStateDigest(second));
  assert.equal(first.hands[0].length, 14);
  assert.deepEqual(first.hands.slice(1).map((hand) => hand.length), [13, 13, 13]);
  assert.equal(first.wall.length, 69);
  assert.equal(first.deadWall.length, 14);
  assert.equal(first.phase, "discard");
});

test("discard enters the ordered reaction window and passes advance the wall", () => {
  const initial = createGame({ players: 4, humanSeat: 0, seed: "reaction-window" });
  const discard = legalActions(initial).find((action) => action.kind === "discard");
  assert.ok(discard);
  let current = applyAction(initial, discard);
  assert.equal(current.phase, "reaction");
  assert.equal(current.pendingDiscard?.fromSeat, 0);
  assert.deepEqual(current.pendingDiscard?.reactionSeats, [1, 2, 3]);
  current = applyAction(current, { kind: "pass" });
  current = applyAction(current, { kind: "pass" });
  current = applyAction(current, { kind: "pass" });
  assert.equal(current.phase, "draw");
  const settled = advanceAutomated(current, (state, actions) => decideAction(state, actions).selected.action);
  assert.equal(settled.currentSeat, 0);
});

test("the local policy can complete a hand for every seat", () => {
  const initial = createGame({ players: 4, humanSeat: 0, seed: "all-seat-autoplay" });
  const completed = advanceAutomated(initial, (state, actions) => decideAction(state, actions).selected.action, {
    includeHuman: true,
    limit: 512
  });
  assert.equal(completed.phase, "terminal");
  assert.ok(completed.history.some((event) => event.seat === 0 && event.action !== null));
  assert.ok(completed.history.some((event) => event.seat === 1 && event.action !== null));
  assert.ok(completed.history.some((event) => event.seat === 2 && event.action !== null));
  assert.ok(completed.history.some((event) => event.seat === 3 && event.action !== null));
});

test("partial automated progress permits a paced all-seat UI loop", () => {
  const initial = createGame({ players: 4, humanSeat: 0, seed: "paced-all-seat-autoplay" });
  const progressed = advanceAutomated(initial, (state, actions) => decideAction(state, actions).selected.action, {
    includeHuman: true,
    limit: 1,
    allowPartial: true
  });
  assert.equal(progressed.version, initial.version + 1);
  assert.equal(progressed.currentSeat, 1);
  assert.equal(progressed.phase, "reaction");
});

test("policy review identifies a recorded Seat 0 divergence without mutating history", () => {
  const initial = createGame({ players: 4, humanSeat: 0, seed: "policy-review-divergence" });
  const ranked = decideAction(initial, legalActions(initial));
  const alternative = ranked.choices.at(-1);
  assert.ok(alternative);
  const afterAction = applyAction(initial, alternative.action);
  const beforeDigest = gameStateDigest(afterAction);
  const review = analyzeHumanDecisions(afterAction, DEFAULT_POLICY);
  assert.equal(review.decisions.length, 1);
  assert.equal(actionKey(review.decisions[0].action), actionKey(alternative.action));
  assert.equal(actionKey(review.decisions[0].recommendedAction), actionKey(ranked.selected.action));
  assert.ok(review.decisions[0].policyLoss > 0);
  assert.notEqual(review.decisions[0].severity, "best");
  assert.equal(review.totalPolicyLoss, review.decisions[0].policyLoss);
  assert.equal(gameStateDigest(afterAction), beforeDigest);
});

test("policy review marks the exact heuristic action as best", () => {
  const initial = createGame({ players: 4, humanSeat: 0, seed: "policy-review-best" });
  const selected = decideAction(initial, legalActions(initial)).selected.action;
  const review = analyzeHumanDecisions(applyAction(initial, selected), DEFAULT_POLICY);
  assert.equal(review.decisions.length, 1);
  assert.equal(review.decisions[0].policyLoss, 0);
  assert.equal(review.decisions[0].severity, "best");
  assert.equal(review.counts.best, 1);
});

test("the browser core consumes the frozen Python discard trace shape", () => {
  const fixture = JSON.parse(readFileSync(new URL("./fixtures/python-sandbox-discard-v1.json", import.meta.url), "utf8")) as {
    seed: string;
    before: { hand: string[]; wall_remaining: number; legal_actions: { kind: string; tile: string }[] };
    action: { kind: "discard"; tile: string };
    after: { current_seat: number; turn: number; wall_remaining: number; hand: string[]; pending_discard: string; pending_reaction_seats: number[] };
  };
  const base = createGame({ players: 4, humanSeat: 0, seed: fixture.seed });
  const state: GameState = {
    ...base,
    phase: "discard",
    currentSeat: 0,
    wall: base.wall.slice(0, fixture.before.wall_remaining),
    hands: [fixture.before.hand, ...base.hands.slice(1)],
    discards: [[], [], [], []],
    melds: [[], [], [], []],
    drawnTile: fixture.before.hand.at(-1) ?? null,
    pendingDiscard: null,
    terminal: null
  };
  const action = { kind: fixture.action.kind, tile: fixture.action.tile } as const;
  const expectedLegal = new Set(fixture.before.legal_actions.map((candidate) => `${candidate.kind}:${candidate.tile}`));
  const actualLegal = new Set(legalActions(state).filter((candidate) => candidate.kind === "discard").map(actionKey));
  assert.deepEqual(actualLegal, expectedLegal);
  const next = applyAction(state, action);
  assert.equal(next.currentSeat, fixture.after.current_seat);
  assert.equal(next.turn, fixture.after.turn);
  assert.equal(next.wall.length, fixture.after.wall_remaining);
  assert.equal(next.pendingDiscard?.tile, fixture.after.pending_discard);
  assert.deepEqual(next.pendingDiscard?.reactionSeats, fixture.after.pending_reaction_seats);
  assert.deepEqual([...next.hands[0]].sort(), [...fixture.after.hand].sort());
});

test("scoreRound recognizes local chiitoitsu and produces browser-owned payments", () => {
  const base = createGame({ players: 4, humanSeat: 0, seed: "score-round" });
  const state: GameState = {
    ...base,
    phase: "discard",
    currentSeat: 0,
    hands: [["2m", "2m", "3m", "3m", "4p", "4p", "5p", "5p", "6s", "6s", "7s", "7s", "P", "P"], ...base.hands.slice(1)],
    melds: [[], [], [], []],
    doraIndicators: [],
    riichiSeats: [false, false, false, false]
  };
  const score = scoreRound(state, { winner: 0, fromSeat: null, winKind: "tsumo" });
  assert.ok(score);
  assert.equal(score.han, 3);
  assert.equal(score.fu, 25);
  assert.equal(score.payments.reduce((sum, value) => sum + value, 0), 0);
  assert.ok(score.yaku.some((line) => line.name === "Chiitoitsu"));
});

test("Sanma reaction actions do not expose chi", () => {
  const base = createGame({ players: 3, humanSeat: 0, seed: "sanma-no-chi" });
  const state: GameState = {
    ...base,
    phase: "reaction",
    currentSeat: 1,
    hands: [[...base.hands[0]], ["2m", "3m", ...base.hands[1].slice(2)], [...base.hands[2]]],
    pendingDiscard: { tile: "1m", fromSeat: 0, reactionSeats: [1, 2] }
  };
  assert.ok(!legalActions(state).some((action) => action.kind === "chi"));
});

test("policy ranks only legal actions and game saves round-trip", () => {
  const state = createGame({ players: 4, humanSeat: 0, seed: "policy-save" });
  const legal = legalActions(state);
  const decision = decideAction(state, legal);
  assert.ok(legal.some((action) => actionKey(action) === actionKey(decision.selected.action)));
  const restored = deserializeGame(serializeGame(state));
  assert.equal(gameStateDigest(restored), gameStateDigest(state));
});

test("the shipped browser policy asset validates against the local policy contract", () => {
  const artifact = validatePolicyArtifact(JSON.parse(readFileSync(new URL("../public/policies/local-shape-policy-v1.json", import.meta.url), "utf8")));
  assert.equal(artifact.version, "local-linear-v1");
  assert.equal(artifact.name, "Local shape policy");
});

test("timeline snapshots support replay restoration and bounded local undo", () => {
  const initial = createGame({ players: 4, humanSeat: 0, seed: "timeline-undo" });
  const initialDigest = gameStateDigest(initial);
  const discard = legalActions(initial).find((action) => action.kind === "discard");
  assert.ok(discard);
  const afterDiscard = applyAction(initial, discard);
  assert.equal(afterDiscard.timeline.length, initial.timeline.length + 1);
  assert.equal(restoreTimelineFrame(afterDiscard, 1).version, initial.version);
  assert.equal(gameStateDigest(undoLastHumanAction(afterDiscard)), initialDigest);
});
