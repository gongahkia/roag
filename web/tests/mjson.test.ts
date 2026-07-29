import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { parseMjsonTrajectory, validateMjsonEvent } from "../src/lib/mjson";

const fixture = fileURLToPath(new URL("../../data/fixtures/mjai/events_4p.mjson", import.meta.url));

test("parses the checked-in four-player MJSON fixture", () => {
  const result = parseMjsonTrajectory(readFileSync(fixture, "utf8"));

  assert.equal(result.errors.length, 0);
  assert.equal(result.events.length, 11);
  assert.equal(result.events[0]?.type, "start_game");
  assert.equal(result.events.at(-1)?.type, "end_game");
});

test("retains independent valid lines and reports line-local validation errors", () => {
  const result = parseMjsonTrajectory(
    [
      '{"type":"start_game"}',
      '{"type":"dahai","actor":0,"pai":"1m","tsumogiri":"yes"}',
      '{"type":"kita","actor":2,"pai":"N","consumed":["N"]}',
      "not-json"
    ].join("\n")
  );

  assert.deepEqual(result.events.map((event) => event.type), ["start_game", "kita"]);
  assert.deepEqual(
    result.errors,
    [
      { line: 2, code: "invalid_event", message: "dahai tsumogiri must be a boolean" },
      { line: 4, code: "invalid_json", message: "invalid JSON object" }
    ]
  );
});

test("rejects unsupported event types and invalid action requests", () => {
  assert.deepEqual(validateMjsonEvent({ type: "upload", url: "https://example.invalid" }), [
    "event type is unsupported"
  ]);
  assert.deepEqual(validateMjsonEvent({ type: "request_action", possible_actions: ["pass"] }), [
    "request_action possible_actions entries must be action objects"
  ]);
});
