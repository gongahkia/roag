from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.simulation import (
    SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_FIELDS,
    SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND,
    build_synthetic_match_split_manifest,
    generate_synthetic_match_manifest,
    write_synthetic_match_split_manifest,
)


class SyntheticMatchSplitManifestTests(unittest.TestCase):
    def test_splits_complete_sanma_matches_by_game_without_leakage(self) -> None:
        source = generate_synthetic_match_manifest(
            match_count=3,
            seed="synthetic-split-source",
            ruleset="tenhou-3p",
        )
        split = build_synthetic_match_split_manifest(source, split_seed="synthetic-split")

        self.assertEqual(
            split,
            build_synthetic_match_split_manifest(source, split_seed="synthetic-split"),
        )
        self.assertEqual(tuple(split), SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_FIELDS)
        self.assertEqual(split["kind"], SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND)
        self.assertEqual(split["source"]["ruleset"], "tenhou-3p")
        self.assertEqual(split["source"]["provenance"], source["provenance"])
        assignment_sets = [
            set(split["assignments"][name]) for name in ("train", "validation", "test")
        ]
        self.assertEqual(set.union(*assignment_sets), {0, 1, 2})
        self.assertTrue(all(len(assignment) == 1 for assignment in assignment_sets))
        self.assertFalse(assignment_sets[0] & assignment_sets[1])
        self.assertFalse(assignment_sets[0] & assignment_sets[2])
        self.assertFalse(assignment_sets[1] & assignment_sets[2])

    def test_writes_manifest_and_rejects_invalid_sources_and_fractions(self) -> None:
        source = generate_synthetic_match_manifest(
            match_count=3,
            seed="synthetic-split-write",
            ruleset="tenhou-3p",
        )
        split = build_synthetic_match_split_manifest(source, split_seed="synthetic-split-write")
        with TemporaryDirectory() as directory:
            path = Path(directory) / "nested" / "split.json"
            write_synthetic_match_split_manifest(path, split)
            self.assertEqual(json.loads(path.read_text(encoding="utf-8")), split)

        with self.assertRaisesRegex(ValueError, "at least three"):
            build_synthetic_match_split_manifest(
                {**source, "matches": source["matches"][:2], "match_count": 2},
                split_seed="bad",
            )
        with self.assertRaisesRegex(ValueError, "sum to one"):
            build_synthetic_match_split_manifest(
                source,
                split_seed="bad",
                fractions=(0.7, 0.2, 0.2),
            )


if __name__ == "__main__":
    unittest.main()
