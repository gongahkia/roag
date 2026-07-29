import assert from "node:assert/strict";
import test from "node:test";

import { parseMjsonTrajectory } from "../src/lib/mjson";
import {
  buildReplayPolicyInput,
  selectReplayPolicyAction
} from "../src/lib/replay-policy-input";
import { buildReplayTimeline } from "../src/lib/replay";

test("encodes a deterministic four-player discard request into the ONNX contract", () => {
  const timeline = buildReplayTimeline(parseMjsonTrajectory(fourPlayerRequest()).events);
  const input = buildReplayPolicyInput(timeline[3]!);

  assert.equal(input.ruleset, "tenhou-4p");
  assert.equal(input.observation.length, 546);
  assert.equal(input.legalActionMask.length, 276);
  assert.equal(input.observation[43], 0.25);
  assert.equal(input.observation[516], 0);
  assert.equal(input.observation[519], 69 / 136);
  assert.equal(input.legalActionMask[0], true);
  assert.equal(input.legalActionMask[9], true);
  assert.equal(input.legalActionMask[272], true);
  assert.deepEqual(selectReplayPolicyAction(input, Array(276).fill(1)), {
    action: { kind: "discard", tile: "1m", label: "discard 1m" },
    index: 0,
    score: 1
  });
});

test("zero-pads Sanma rows and maps a Kita request", () => {
  const timeline = buildReplayTimeline(parseMjsonTrajectory([
    '{"type":"start_game","names":["a","b","c"]}',
    '{"type":"start_kyoku","bakaze":"E","kyoku":1,"honba":0,"kyotaku":0,"oya":0,"scores":[35000,35000,35000],"tehais":[["N","1m","9m","1p","2p","3p","4p","5p","6p","7p","8p","9p","E"],["1m","9m","1p","2p","3p","4p","5p","6p","7p","8p","9p","S","N"],["1m","9m","1s","2s","3s","4s","5s","6s","7s","8s","9s","W","E"]],"dora_marker":"1s"}',
    '{"type":"tsumo","actor":0,"pai":"N"}',
    '{"type":"request_action","actor":0,"possible_actions":[{"type":"kita","pai":"N"}]}'
  ].join("\n")).events);
  const input = buildReplayPolicyInput(timeline[3]!);

  assert.equal(input.ruleset, "tenhou-3p");
  assert.deepEqual(input.observation.slice(170, 204), Array(34).fill(0));
  assert.equal(input.legalActionMask[268], true);
});

test("rejects a request action that cannot provide a legal mask coordinate", () => {
  const timeline = buildReplayTimeline(parseMjsonTrajectory([
    '{"type":"start_game","names":["a","b","c","d"]}',
    '{"type":"start_kyoku","bakaze":"E","kyoku":1,"scores":[25000,25000,25000,25000],"tehais":[["1m"],["1p"],["2p"],["3p"]]}',
    '{"type":"request_action","actor":0,"possible_actions":[{"type":"dahai"}]}'
  ].join("\n")).events);

  assert.throws(() => buildReplayPolicyInput(timeline[2]!), /requires pai/);
});

test("rejects a reaction request with unavailable ObservationV1 state", () => {
  const timeline = buildReplayTimeline(parseMjsonTrajectory([
    '{"type":"start_game","names":["a","b","c","d"]}',
    '{"type":"start_kyoku","bakaze":"E","kyoku":1,"honba":0,"kyotaku":0,"oya":0,"scores":[25000,25000,25000,25000],"tehais":[["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","2p","3p","4p"],["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","2p","3p","4p"],["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","2p","3p","4p"],["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","2p","3p","4p"]],"dora_marker":"1s"}',
    '{"type":"tsumo","actor":0,"pai":"1m"}',
    '{"type":"dahai","actor":0,"pai":"1m","tsumogiri":true}',
    '{"type":"request_action","actor":1,"possible_actions":[{"type":"none"}]}'
  ].join("\n")).events);

  assert.throws(() => buildReplayPolicyInput(timeline[4]!), /fully observed direct/);
});

function fourPlayerRequest(): string {
  return [
    '{"type":"start_game","names":["a","b","c","d"]}',
    '{"type":"start_kyoku","bakaze":"E","kyoku":1,"honba":0,"kyotaku":0,"oya":0,"scores":[25000,25000,25000,25000],"tehais":[["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","2p","3p","4p"],["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","1p","2p","4p"],["1m","2m","3m","4m","5m","6m","7m","8m","9m","2p","3p","4p","5p"],["1m","2m","3m","4m","5m","6m","7m","8m","9m","1p","2p","3p","4p"]],"dora_marker":"1s"}',
    '{"type":"tsumo","actor":0,"pai":"1p"}',
    '{"type":"request_action","actor":0,"possible_actions":[{"type":"dahai","pai":"1m","tsumogiri":false},{"type":"dahai","pai":"1p","tsumogiri":true},{"type":"none"}]}'
  ].join("\n");
}
