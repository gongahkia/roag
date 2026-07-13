from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.simulation import (
    SYNTHETIC_MATCH_MANIFEST_V1_FIELDS,
    SYNTHETIC_MATCH_MANIFEST_V1_KIND,
    generate_synthetic_match_manifest,
    write_synthetic_match_manifest,
)


class SyntheticMatchManifestTests(unittest.TestCase):
    def test_generates_deterministic_complete_matches_for_both_rulesets(self) -> None:
        for ruleset in ("tenhou-4p", "tenhou-3p"):
            with self.subTest(ruleset=ruleset):
                manifest = generate_synthetic_match_manifest(
                    match_count=1,
                    seed="synthetic-manifest",
                    ruleset=ruleset,
                )
                same_manifest = generate_synthetic_match_manifest(
                    match_count=1,
                    seed="synthetic-manifest",
                    ruleset=ruleset,
                )

                self.assertEqual(manifest, same_manifest)
                self.assertEqual(tuple(manifest), SYNTHETIC_MATCH_MANIFEST_V1_FIELDS)
                self.assertEqual(manifest["kind"], SYNTHETIC_MATCH_MANIFEST_V1_KIND)
                self.assertEqual(manifest["ruleset"], ruleset)
                self.assertEqual(manifest["match_count"], 1)
                self.assertEqual(manifest["final_summary"]["completed_games"], 1)
                self.assertEqual(manifest["provenance"]["source_kind"], "local_synthetic")
                self.assertEqual(manifest["provenance"]["root_seed"], "synthetic-manifest")
                self.assertTrue(manifest["matches"][0]["completed"])
                self.assertIsNotNone(manifest["matches"][0]["final_result"])
                self.assertTrue(manifest["matches"][0]["trajectory"])

    def test_writes_json_manifest_and_rejects_incomplete_generation(self) -> None:
        manifest = generate_synthetic_match_manifest(
            match_count=1,
            seed="synthetic-write",
            ruleset="tenhou-3p",
        )
        with TemporaryDirectory() as directory:
            path = Path(directory) / "nested" / "manifest.json"
            write_synthetic_match_manifest(path, manifest)
            self.assertEqual(json.loads(path.read_text(encoding="utf-8")), manifest)

        with self.assertRaisesRegex(ValueError, "did not complete"):
            generate_synthetic_match_manifest(
                match_count=1,
                seed="synthetic-incomplete",
                max_rounds=1,
            )


if __name__ == "__main__":
    unittest.main()
