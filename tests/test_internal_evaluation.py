from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.internal_evaluation import (
    INTERNAL_EVALUATION_MANIFEST_KIND,
    build_frozen_internal_evaluation_manifest,
    validate_frozen_internal_evaluation_manifest,
    write_frozen_internal_evaluation_manifest,
)


class InternalEvaluationManifestTests(unittest.TestCase):
    def test_builds_deterministic_frozen_plans_for_both_rulesets(self) -> None:
        first = _manifest()
        second = _manifest()

        self.assertEqual(first, second)
        self.assertEqual(first["kind"], INTERNAL_EVALUATION_MANIFEST_KIND)
        self.assertEqual(
            [plan["ruleset"] for plan in first["rulesets"]],
            ["tenhou-4p", "tenhou-3p"],
        )
        self.assertEqual(first["rulesets"][0]["paired_match"]["heuristic_baseline"]["players"], 4)
        self.assertEqual(first["rulesets"][1]["paired_match"]["heuristic_baseline"]["players"], 3)
        validate_frozen_internal_evaluation_manifest(first)

    def test_detects_tampering_and_refuses_replacement(self) -> None:
        manifest = _manifest()
        with TemporaryDirectory() as directory:
            path = Path(directory) / "manifest.json"
            write_frozen_internal_evaluation_manifest(path, manifest)
            write_frozen_internal_evaluation_manifest(path, manifest)
            replacement = _manifest(seed="different")
            with self.assertRaisesRegex(ValueError, "refusing to replace"):
                write_frozen_internal_evaluation_manifest(path, replacement)

        manifest["rulesets"][0]["paired_match"]["pairs"] = 3
        with self.assertRaisesRegex(ValueError, "fingerprint mismatch"):
            validate_frozen_internal_evaluation_manifest(manifest)

    def test_validates_required_positive_bounds(self) -> None:
        with self.assertRaisesRegex(ValueError, "pairs must be positive"):
            build_frozen_internal_evaluation_manifest(
                checkpoint_id="candidate-v1",
                seed="fixed",
                pairs=0,
                max_rounds=1,
                max_turns_per_round=1,
            )


def _manifest(*, seed: str = "fixed") -> dict[str, object]:
    return build_frozen_internal_evaluation_manifest(
        checkpoint_id="candidate-v1",
        seed=seed,
        pairs=2,
        max_rounds=12,
        max_turns_per_round=512,
        bootstrap_resamples=17,
    )


if __name__ == "__main__":
    unittest.main()
