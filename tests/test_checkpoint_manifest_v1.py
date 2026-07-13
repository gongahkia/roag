from __future__ import annotations

import json
import unittest

from kenjaku.schema import (
    CHECKPOINT_MANIFEST_V1_KIND,
    CheckpointCompatibilityV1,
    CheckpointManifestV1,
    CheckpointRequirementV1,
    SemanticVersion,
)


class CheckpointManifestV1Tests(unittest.TestCase):
    def test_semver_parsing_and_precedence_match_semver_two(self) -> None:
        alpha = SemanticVersion.parse("1.0.0-alpha.1+local")
        release = SemanticVersion.parse("1.0.0")
        build_variant = SemanticVersion.parse("1.0.0+build.2")

        self.assertLess(alpha, release)
        self.assertLess(
            SemanticVersion.parse("1.0.0-alpha.2"), SemanticVersion.parse("1.0.0-alpha.11")
        )
        self.assertLess(
            SemanticVersion.parse("1.0.0-alpha.1"),
            SemanticVersion.parse("1.0.0-alpha.beta"),
        )
        self.assertTrue(release.has_same_precedence(build_variant))
        self.assertEqual(release, build_variant)
        self.assertEqual(str(alpha), "1.0.0-alpha.1+local")
        for invalid in ("1.0", "01.0.0", "1.0.0-01", "1.0.0-"):
            with (
                self.subTest(invalid=invalid),
                self.assertRaisesRegex(ValueError, "semantic version"),
            ):
                SemanticVersion.parse(invalid)

    def test_current_manifest_round_trips_and_satisfies_matching_requirement(self) -> None:
        manifest = CheckpointManifestV1.for_current_schemas(
            checkpoint_id="fixture-policy",
            model_kind="sandbox-policy",
            model_version="1.2.0",
            rulesets=("tenhou-4p", "tenhou-3p"),
        )
        requirement = CheckpointRequirementV1(
            ruleset="tenhou-3p",
            model_kind="sandbox-policy",
            minimum_model_version=SemanticVersion.parse("1.1.9"),
            compatibility=CheckpointCompatibilityV1.current(),
        )
        payload = manifest.to_dict()

        self.assertEqual(payload["kind"], CHECKPOINT_MANIFEST_V1_KIND)
        self.assertEqual(CheckpointManifestV1.from_dict(payload), manifest)
        self.assertEqual(json.loads(manifest.to_json()), payload)
        self.assertTrue(manifest.is_compatible_with(requirement))
        manifest.require_compatible(requirement)

    def test_reports_version_ruleset_and_schema_mismatches(self) -> None:
        manifest = CheckpointManifestV1.for_current_schemas(
            checkpoint_id="fixture-policy",
            model_kind="sandbox-policy",
            model_version="1.2.0-alpha.1",
            rulesets=("tenhou-4p",),
        )
        compatibility = CheckpointCompatibilityV1(
            observation_kind="kenjaku-observation-v2",
            action_kind="kenjaku-action-v1",
            legal_action_mask_kind="kenjaku-legal-action-mask-v1",
            legal_action_mask_dim=276,
            decision_result_kind="kenjaku-decision-result-v1",
        )
        requirement = CheckpointRequirementV1(
            ruleset="tenhou-3p",
            model_kind="different-policy",
            minimum_model_version=SemanticVersion.parse("1.2.0"),
            compatibility=compatibility,
        )

        self.assertEqual(
            manifest.compatibility_errors(requirement),
            (
                "ruleset is unsupported",
                "model_kind differs",
                "model_version is below minimum",
                "compatibility.observation_kind differs",
            ),
        )
        with self.assertRaisesRegex(ValueError, "ruleset is unsupported"):
            manifest.require_compatible(requirement)

    def test_rejects_unknown_manifest_fields(self) -> None:
        manifest = CheckpointManifestV1.for_current_schemas(
            checkpoint_id="fixture-policy",
            model_kind="sandbox-policy",
            model_version="1.0.0",
            rulesets=("tenhou-4p",),
        )
        payload = manifest.to_dict()
        payload["extra"] = True

        with self.assertRaisesRegex(ValueError, "unexpected=extra"):
            CheckpointManifestV1.from_dict(payload)


if __name__ == "__main__":
    unittest.main()
