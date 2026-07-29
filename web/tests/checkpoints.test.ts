import assert from "node:assert/strict";
import test from "node:test";

import {
  BrowserCheckpointRegistry,
  CURRENT_CHECKPOINT_COMPATIBILITY_V1,
  checkpointCompatibilityErrors,
  compareSemanticVersions,
  createCheckpointRequirementV1,
  parseCheckpointManifestV1,
  parseCheckpointManifestV1Json,
  parseSemanticVersion,
  semanticVersionToString
} from "../src/lib/checkpoints";

function manifest(
  checkpoint_id: string,
  model_version = "1.2.0",
  rulesets: string[] = ["tenhou-4p", "tenhou-3p"]
) {
  return {
    kind: "kenjaku-checkpoint-manifest-v1",
    checkpoint_id,
    model_kind: "multi-action-policy",
    model_version,
    rulesets,
    compatibility: { ...CURRENT_CHECKPOINT_COMPATIBILITY_V1 }
  };
}

test("parses strict checkpoint manifests and Semantic Versioning precedence", () => {
  const parsed = parseCheckpointManifestV1(manifest("checkpoint-a", "1.2.3-alpha.2+build.7"));

  assert.equal(semanticVersionToString(parsed.model_version), "1.2.3-alpha.2+build.7");
  assert.equal(
    compareSemanticVersions(parseSemanticVersion("1.0.0-rc.1"), parseSemanticVersion("1.0.0")),
    -1
  );
  assert.equal(
    compareSemanticVersions(parseSemanticVersion("1.0.0+first"), parseSemanticVersion("1.0.0+second")),
    0
  );
  assert.throws(
    () => parseCheckpointManifestV1({ ...manifest("checkpoint-a"), extra: true }),
    /unexpected=extra/
  );
  assert.throws(() => parseCheckpointManifestV1(manifest("checkpoint-a", "01.2.3")), /invalid semantic version/);
  assert.throws(() => parseCheckpointManifestV1Json("not JSON"), /valid JSON/);
});

test("reports Python-schema-compatible errors for unsupported 4p and Sanma requirements", () => {
  const fourPlayer = parseCheckpointManifestV1(manifest("four-player", "1.0.0", ["tenhou-4p"]));
  const sanmaRequirement = createCheckpointRequirementV1({
    ruleset: "tenhou-3p",
    minimum_model_version: "1.1.0",
    model_kind: "other-policy",
    compatibility: {
      ...CURRENT_CHECKPOINT_COMPATIBILITY_V1,
      legal_action_mask_dim: 275
    }
  });

  assert.deepEqual(checkpointCompatibilityErrors(fourPlayer, sanmaRequirement), [
    "ruleset is unsupported",
    "model_kind differs",
    "model_version is below minimum",
    "compatibility.legal_action_mask_dim differs"
  ]);
});

test("selects a deterministic compatible checkpoint and retains rejections", () => {
  const registry = new BrowserCheckpointRegistry<string>();
  registry.register({ manifest: parseCheckpointManifestV1(manifest("zeta", "1.1.0")), value: "zeta.onnx" });
  registry.register({ manifest: parseCheckpointManifestV1(manifest("alpha", "1.2.0")), value: "alpha.onnx" });
  registry.register({
    manifest: parseCheckpointManifestV1(manifest("four-player", "2.0.0", ["tenhou-4p"])),
    value: "four-player.onnx"
  });

  const resolution = registry.resolve(
    createCheckpointRequirementV1({
      ruleset: "tenhou-3p",
      minimum_model_version: "1.0.0",
      compatibility: CURRENT_CHECKPOINT_COMPATIBILITY_V1
    })
  );

  assert.equal(resolution.checkpoint?.manifest.checkpoint_id, "alpha");
  assert.equal(resolution.checkpoint?.value, "alpha.onnx");
  assert.deepEqual(resolution.rejections, [
    { checkpoint_id: "four-player", errors: ["ruleset is unsupported", "model_version major differs"] }
  ]);
  assert.deepEqual(registry.list().map((entry) => entry.manifest.checkpoint_id), [
    "alpha",
    "four-player",
    "zeta"
  ]);
  assert.throws(
    () => registry.register({ manifest: parseCheckpointManifestV1(manifest("alpha")), value: "duplicate.onnx" }),
    /already registered/
  );
});
