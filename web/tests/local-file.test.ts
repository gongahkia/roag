import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  MAX_LOCAL_TRAJECTORY_BYTES,
  readLocalMjsonFile,
  validateLocalMjsonFile,
  type LocalTextFile
} from "../src/lib/local-file";

const fixture = readFileSync(
  fileURLToPath(new URL("../../data/fixtures/mjai/events_4p.mjson", import.meta.url)),
  "utf8"
);

test("validates and reads a local MJSON file without exposing its content", async () => {
  const file = localFile("fixture.mjson", fixture, "application/json");

  const result = await readLocalMjsonFile(file);

  assert.deepEqual(validateLocalMjsonFile(file), []);
  assert.equal(result.events.length, 11);
  assert.equal(result.errors.length, 0);
});

test("rejects remote-like names, unsupported types, and bounded input failures", async () => {
  assert.deepEqual(validateLocalMjsonFile(localFile("https://example.invalid/a.mjson", "{}")), [
    "file name must be a local .mjson or .jsonl name"
  ]);
  assert.deepEqual(validateLocalMjsonFile(localFile("fixture.txt", "{}", "text/html")), [
    "file name must be a local .mjson or .jsonl name",
    "file type must be JSON, JSONL, or plain text"
  ]);
  const oversized = localFile(
    "fixture.mjson",
    "{}",
    "application/json",
    MAX_LOCAL_TRAJECTORY_BYTES + 1
  );
  await assert.rejects(readLocalMjsonFile(oversized), /exceeds/);
});

test("browser source has no network transport APIs", () => {
  const app = readFileSync(fileURLToPath(new URL("../src/App.tsx", import.meta.url)), "utf8");
  const localFile = readFileSync(
    fileURLToPath(new URL("../src/lib/local-file.ts", import.meta.url)),
    "utf8"
  );
  const onnxInference = readFileSync(
    fileURLToPath(new URL("../src/lib/onnx-inference.ts", import.meta.url)),
    "utf8"
  );

  assert.doesNotMatch(app + localFile + onnxInference, /\bfetch\s*\(|XMLHttpRequest|WebSocket/);
});

function localFile(
  name: string,
  contents: string,
  type = "",
  size = new TextEncoder().encode(contents).byteLength
): LocalTextFile {
  return { name, size, type, text: async () => contents };
}
