import assert from "node:assert/strict";
import test from "node:test";

import { ASCII_GLYPHS, createAsciiFrame, createTableCutsceneFrame } from "../src/lib/ascii-renderer";

test("builds deterministic bounded 1-bit ASCII frames", () => {
  const first = createAsciiFrame({ columns: 48, rows: 20, seed: "east-1" });
  const repeated = createAsciiFrame({ columns: 48, rows: 20, seed: "east-1" });
  const changed = createAsciiFrame({ columns: 48, rows: 20, seed: "south-4" });

  assert.deepEqual(first, repeated);
  assert.notDeepEqual(first, changed);
  assert.equal(first.length, 20);
  assert.equal(first.every((line) => line.length === 48), true);
  assert.equal(first.join("").includes("@"), true);
  assert.equal(
    Array.from(first.join("")).every((glyph) => ASCII_GLYPHS.includes(glyph)),
    true
  );
});

test("rejects invalid ASCII frame dimensions", () => {
  assert.deepEqual(createAsciiFrame({ columns: 0, rows: 20, seed: "invalid" }), []);
  assert.deepEqual(createAsciiFrame({ columns: 20, rows: -1, seed: "invalid" }), []);
});

test("builds a deterministic active-seat cutscene around a four-player table", () => {
  const options = {
    columns: 72,
    rows: 28,
    seed: "cutscene-east-1",
    players: 4 as const,
    names: ["You", "Shimocha", "Toimen", "Kamicha"],
    activeSeat: 2,
    eventKind: "discard" as const
  };
  const first = createTableCutsceneFrame(options);
  const changedSeat = createTableCutsceneFrame({ ...options, activeSeat: 0 });
  assert.deepEqual(first, createTableCutsceneFrame(options));
  assert.equal(first.length, 28);
  assert.ok(first.some((line) => line.includes(">TOIMEN<")));
  assert.ok(first.some((line) => line.includes("[YOU]")));
  assert.ok(first.some((line) => line.includes("[ DISCARD ]")));
  assert.notDeepEqual(first, changedSeat);
});
