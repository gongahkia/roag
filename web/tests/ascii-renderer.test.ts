import assert from "node:assert/strict";
import test from "node:test";

import { ASCII_GLYPHS, createAsciiFrame } from "../src/lib/ascii-renderer";

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
