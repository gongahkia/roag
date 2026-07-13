import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { buildReplayTimeline, describeMjsonEvent } from "../src/lib/replay";
import { parseMjsonTrajectory } from "../src/lib/mjson";

const fixture = readFileSync(
  fileURLToPath(new URL("../../data/fixtures/mjai/events_4p.mjson", import.meta.url)),
  "utf8"
);

test("builds immutable deterministic board states for the four-player fixture", () => {
  const parsed = parseMjsonTrajectory(fixture);
  const timeline = buildReplayTimeline(parsed.events);

  assert.equal(timeline.length, 11);
  assert.equal(timeline[1]?.label, "Round 1 started");
  assert.deepEqual(timeline[1]?.state.scores, [25000, 25000, 25000, 25000]);
  assert.equal(timeline[5]?.state.hands[0]?.length, 13);
  assert.deepEqual(timeline[5]?.state.discards[0], ["1p"]);
  assert.deepEqual(timeline[6]?.state.melds[1], [
    { kind: "pon", tiles: ["1p", "1p", "1p"], from_seat: 0 }
  ]);
  assert.equal(timeline.at(-1)?.state.game_finished, true);
  assert.throws(() => (timeline[1]?.state.hands[0] as string[]).push("1m"), /read only|object is not extensible/);
});

test("retains Sanma kita state and request-action inspection", () => {
  const parsed = parseMjsonTrajectory(
    [
      '{"type":"start_game","names":["a","b","c"]}',
      '{"type":"start_kyoku","bakaze":"E","kyoku":1,"scores":[35000,35000,35000],"tehais":[["N"],["1m"],["2m"]]}',
      '{"type":"kita","actor":0,"pai":"N","consumed":["N"]}',
      '{"type":"request_action","actor":0,"possible_actions":[{"type":"dahai"},{"type":"none"}]}'
    ].join("\n")
  );
  const timeline = buildReplayTimeline(parsed.events);

  assert.equal(timeline[2]?.state.players, 3);
  assert.deepEqual(timeline[2]?.state.kita_tiles[0], ["N"]);
  assert.deepEqual(timeline[3]?.decision, { actor: 0, legal_actions: ["dahai", "none"] });
  assert.equal(describeMjsonEvent(parsed.events[2]!), "Seat 0: kita N");
});
