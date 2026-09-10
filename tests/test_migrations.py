from __future__ import annotations

import gzip
import json
import unittest
from copy import deepcopy
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.migrations import (
    LEGACY_20_FINGERPRINT,
    MigrationError,
    migrate_run,
    run_26_to_27,
    run_33_to_34,
    run_34_to_35,
)
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

    def test_pressure_migration_is_pure_and_discloses_unknown_prior_actions(self) -> None:
        current = GameEngine.new(load_catalog(), 42).snapshot()
        old = deepcopy(current)
        old["save_version"] = 33
        old["content_manifest"]["engine"] = "0.2.0"
        for field in ("pressure", "pressure_recent", "pressure_incomplete_before_tick"):
            del old["state"][field]
        before = deepcopy(old)
        migrated = run_33_to_34(old)
        self.assertEqual(before, old)
        self.assertEqual(34, migrated["save_version"])
        self.assertEqual("0.3.0", migrated["content_manifest"]["engine"])
        self.assertEqual(0, migrated["state"]["pressure"])
        self.assertEqual([], migrated["state"]["pressure_recent"])
        self.assertEqual(old["state"]["travel_ticks"], migrated["state"]["pressure_incomplete_before_tick"])
        for malformed in (True, 32, 34):
            broken = deepcopy(old)
            broken["save_version"] = malformed
            with self.assertRaises(MigrationError):
                run_33_to_34(broken)

    def test_director_migration_preserves_existing_combat_at_quiet(self) -> None:
        current = GameEngine.new(load_catalog(), 43)
        current.start_combat("lost_shift")
        old = current.snapshot()
        old["save_version"] = 34
        old["content_manifest"]["engine"] = "0.3.0"
        for field in ("encounter_pressure", "encounter_modules", "reinforcement_tickets"):
            del old["state"][field]
        before = deepcopy(old)
        migrated = run_34_to_35(old)
        self.assertEqual(before, old)
        self.assertEqual(0, migrated["state"]["encounter_pressure"])
        self.assertEqual([], migrated["state"]["encounter_modules"])
        self.assertEqual(0, migrated["state"]["reinforcement_tickets"])
        self.assertEqual("0.4.0", migrated["content_manifest"]["engine"])


if __name__ == "__main__":
    unittest.main()
