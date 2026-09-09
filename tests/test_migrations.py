from __future__ import annotations

import gzip
import json
import unittest
from copy import deepcopy
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.migrations import LEGACY_20_FINGERPRINT, MigrationError, migrate_run, run_26_to_27
from dumbest_dungeon.policies import Policy, canonical_hash, execute_command


class MigrationTests(unittest.TestCase):
    @staticmethod
    def legacy():
        path = Path(__file__).resolve().parents[1] / "docs/evidence/pass3/terminal44-before-final-play.json.gz"
        with gzip.open(path, "rt") as stream:
            return json.load(stream)

    def test_golden_terminal_save_migrates_purely_and_finishes_normally(self) -> None:
        legacy = self.legacy()
        before = deepcopy(legacy)
        migrated = run_26_to_27(legacy)
        self.assertEqual(before, legacy)
        self.assertEqual(27, migrated["save_version"])
        self.assertEqual(legacy["state"], migrated["state"])
        self.assertEqual(legacy["rng_state"], migrated["rng_state"])
        self.assertEqual(LEGACY_20_FINGERPRINT, migrated["content_manifest"]["fingerprint"])
        catalog = load_catalog()
        engine = GameEngine.from_snapshot(catalog, legacy)
        checkpoint = GameEngine.from_snapshot(catalog, engine.snapshot())
        policy = Policy()
        for _ in range(5):
            if engine.state.phase == "victory":
                break
            command = policy.next_command(engine)
            execute_command(engine, command)
            execute_command(checkpoint, command)
            self.assertEqual(canonical_hash(engine), canonical_hash(checkpoint))
        self.assertEqual("victory", engine.state.phase)

    def test_migration_accepts_exactly_one_source_version(self) -> None:
        for version in (True, 25, 27, 99):
            raw = self.legacy()
            raw["save_version"] = version
            with self.assertRaises(MigrationError):
                run_26_to_27(raw)
        with self.assertRaises(MigrationError):
            migrate_run({"save_version": 999})

    def test_missing_durable_state_and_incompatible_manifest_are_not_regenerated(self) -> None:
        catalog = load_catalog()
        raw = run_26_to_27(self.legacy())
        for field in ("rng_state", "content_manifest"):
            broken = deepcopy(raw)
            del broken[field]
            with self.assertRaises(RuleError):
                GameEngine.from_snapshot(catalog, broken)
        for field, value in (("fingerprint", "0" * 64), ("enabled_packs", []), ("rng_architecture", 999)):
            broken = deepcopy(raw)
            broken["content_manifest"][field] = value
            with self.assertRaisesRegex(RuleError, "manifest"):
                GameEngine.from_snapshot(catalog, broken)


if __name__ == "__main__":
    unittest.main()
