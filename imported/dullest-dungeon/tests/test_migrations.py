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
    run_35_to_36,
    run_36_to_37,
    run_37_to_38,
    run_38_to_39,
    run_39_to_40,
    run_40_to_41,
    run_41_to_42,
    run_42_to_43,
    run_43_to_44,
    run_44_to_45,
    run_45_to_46,
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
        for field in ("encounter_pressure", "encounter_modules", "reinforcement_tickets", "reinforcement_reserve_id"):
            del old["state"][field]
        before = deepcopy(old)
        migrated = run_34_to_35(old)
        self.assertEqual(before, old)
        self.assertEqual(0, migrated["state"]["encounter_pressure"])
        self.assertEqual([], migrated["state"]["encounter_modules"])
        self.assertEqual(0, migrated["state"]["reinforcement_tickets"])
        self.assertEqual("0.4.0", migrated["content_manifest"]["engine"])

    def test_named_rng_migration_changes_only_version_markers(self) -> None:
        current = GameEngine.new(load_catalog(), 44).snapshot()
        old = deepcopy(current)
        old["save_version"] = 35
        old["content_manifest"]["engine"] = "0.4.0"
        old["content_manifest"]["rng_architecture"] = 1
        before = deepcopy(old)
        migrated = run_35_to_36(old)
        self.assertEqual(before, old)
        self.assertEqual(36, migrated["save_version"])
        self.assertEqual("0.5.0", migrated["content_manifest"]["engine"])
        self.assertEqual(2, migrated["content_manifest"]["rng_architecture"])
        self.assertEqual(old["state"], migrated["state"])
        self.assertEqual(old["rng_state"], migrated["rng_state"])

    def test_reinforcement_reserve_migration_is_pure_and_does_not_invent_identity(self) -> None:
        current = GameEngine.new(load_catalog(), 45).snapshot()
        old = deepcopy(current)
        old["save_version"] = 36
        old["content_manifest"]["engine"] = "0.5.0"
        del old["state"]["reinforcement_reserve_id"]
        del old["state"]["next_card_copy_id"]
        for zone in ("deck", "hand", "draw_pile", "discard_pile"):
            for card in old["state"][zone]:
                for field in ("copy_id", "mastery", "infusion_id"):
                    del card[field]
        before = deepcopy(old)
        migrated = run_36_to_37(old)
        self.assertEqual(before, old)
        self.assertEqual(37, migrated["save_version"])
        self.assertEqual("0.6.0", migrated["content_manifest"]["engine"])
        self.assertIsNone(migrated["state"]["reinforcement_reserve_id"])
        self.assertEqual(old["rng_state"], migrated["rng_state"])
        broken = deepcopy(old)
        broken["state"]["reinforcement_tickets"] = 1
        with self.assertRaises(MigrationError):
            run_36_to_37(broken)

    def test_card_identity_migration_is_pure_and_maps_combat_copies(self) -> None:
        current = GameEngine.new(load_catalog(), 46)
        current.start_combat("lost_shift")
        old = current.snapshot()
        old["save_version"] = 37
        old["content_manifest"]["engine"] = "0.6.0"
        del old["state"]["next_card_copy_id"]
        for zone in ("deck", "hand", "draw_pile", "discard_pile"):
            for card in old["state"][zone]:
                for field in ("copy_id", "mastery", "infusion_id"):
                    del card[field]
        before = deepcopy(old)
        migrated = run_37_to_38(old)
        self.assertEqual(before, old)
        self.assertEqual(38, migrated["save_version"])
        self.assertEqual("0.7.0", migrated["content_manifest"]["engine"])
        deck_ids = [card["copy_id"] for card in migrated["state"]["deck"]]
        combat_ids = [
            card["copy_id"]
            for zone in ("hand", "draw_pile", "discard_pile")
            for card in migrated["state"][zone]
        ]
        self.assertEqual(list(range(1, len(deck_ids) + 1)), deck_ids)
        self.assertEqual(sorted(deck_ids), sorted(combat_ids))
        self.assertEqual(len(deck_ids) + 1, migrated["state"]["next_card_copy_id"])
        self.assertTrue(all(
            card["mastery"] is None and card["infusion_id"] is None
            for zone in ("deck", "hand", "draw_pile", "discard_pile")
            for card in migrated["state"][zone]
        ))

    def test_card_identity_migration_rejects_unmatched_combat_copy(self) -> None:
        current = GameEngine.new(load_catalog(), 47)
        current.start_combat("lost_shift")
        old = current.snapshot()
        old["save_version"] = 37
        old["content_manifest"]["engine"] = "0.6.0"
        del old["state"]["next_card_copy_id"]
        for zone in ("deck", "hand", "draw_pile", "discard_pile"):
            for card in old["state"][zone]:
                for field in ("copy_id", "mastery", "infusion_id"):
                    del card[field]
        old["state"]["hand"][0]["card_id"] = "not_in_deck"
        with self.assertRaisesRegex(MigrationError, "do not match"):
            run_37_to_38(old)

    def test_mastery_payload_migration_is_pure_and_explicit(self) -> None:
        current = GameEngine.new(load_catalog(), 48)
        current.start_combat("lost_shift")
        current.play_card(0, current.valid_targets(0)[0], resolve=False)
        old = current.snapshot()
        old["save_version"] = 38
        old["content_manifest"]["engine"] = "0.7.0"
        old["resolution_queue"]["state"]["schema"] = 3
        events = list(old["resolution_queue"]["state"]["pending"])
        if old["resolution_queue"]["state"]["active"] is not None:
            events.append(old["resolution_queue"]["state"]["active"]["event"])
        for event in events:
            del event["payload"]["card_mastery"]
            del event["payload"]["card_copy_id"]
            del event["payload"]["card_infusion"]
        before = deepcopy(old)
        migrated = run_38_to_39(old)
        self.assertEqual(before, old)
        self.assertEqual(39, migrated["save_version"])
        self.assertEqual("0.8.0", migrated["content_manifest"]["engine"])
        self.assertEqual(4, migrated["resolution_queue"]["state"]["schema"])
        self.assertTrue(all(event["payload"]["card_mastery"] is None
                            for event in migrated["resolution_queue"]["state"]["pending"]))
        with self.assertRaises(MigrationError):
            run_38_to_39(migrated)

    def test_infusion_payload_migration_is_pure_and_explicit(self) -> None:
        current = GameEngine.new(load_catalog(), 49)
        current.start_combat("lost_shift")
        current.play_card(0, current.valid_targets(0)[0], resolve=False)
        old = current.snapshot()
        old["save_version"] = 39
        old["content_manifest"]["engine"] = "0.8.0"
        old["resolution_queue"]["state"]["schema"] = 4
        events = list(old["resolution_queue"]["state"]["pending"])
        if old["resolution_queue"]["state"]["active"] is not None:
            events.append(old["resolution_queue"]["state"]["active"]["event"])
        for event in events:
            del event["payload"]["card_copy_id"]
            del event["payload"]["card_infusion"]
        before = deepcopy(old)
        migrated = run_39_to_40(old)
        self.assertEqual(before, old)
        self.assertEqual(40, migrated["save_version"])
        self.assertEqual("0.9.0", migrated["content_manifest"]["engine"])
        self.assertEqual(5, migrated["resolution_queue"]["state"]["schema"])
        self.assertTrue(all(
            event["payload"]["card_copy_id"] == 0
            and event["payload"]["card_infusion"] is None
            for event in migrated["resolution_queue"]["state"]["pending"]
        ))
        with self.assertRaises(MigrationError):
            run_39_to_40(migrated)

    def test_party_configuration_migration_is_pure_and_explicit(self) -> None:
        current = GameEngine.new(load_catalog(), 50, start_in_hub=True)
        old = current.snapshot()
        old["save_version"] = 40
        old["content_manifest"]["engine"] = "0.9.0"
        del old["state"]["hub_loadouts"]
        del old["state"]["doctrine_id"]
        before = deepcopy(old)
        migrated = run_40_to_41(old)
        self.assertEqual(before, old)
        self.assertEqual(41, migrated["save_version"])
        self.assertEqual("1.0.0", migrated["content_manifest"]["engine"])
        self.assertEqual({}, migrated["state"]["hub_loadouts"])
        self.assertIsNone(migrated["state"]["doctrine_id"])
        with self.assertRaises(MigrationError):
            run_40_to_41(migrated)

    def test_recycler_credit_migration_is_pure_and_explicit(self) -> None:
        current = GameEngine.new(load_catalog(), 51)
        old = current.snapshot()
        old["save_version"] = 41
        old["content_manifest"]["engine"] = "1.0.0"
        del old["state"]["recycler_credits"]
        before = deepcopy(old)
        migrated = run_41_to_42(old)
        self.assertEqual(before, old)
        self.assertEqual(42, migrated["save_version"])
        self.assertEqual("1.1.0", migrated["content_manifest"]["engine"])
        self.assertEqual(0, migrated["state"]["recycler_credits"])
        with self.assertRaises(MigrationError):
            run_41_to_42(migrated)

    def test_ladder_migration_preserves_old_runs_at_base_rank(self) -> None:
        current = GameEngine.new(load_catalog(), 52)
        old = current.snapshot()
        old["save_version"] = 42
        old["content_manifest"]["engine"] = "1.1.0"
        del old["state"]["ladder_rank"]
        before = deepcopy(old)
        migrated = run_42_to_43(old)
        self.assertEqual(before, old)
        self.assertEqual(43, migrated["save_version"])
        self.assertEqual("1.2.0", migrated["content_manifest"]["engine"])
        self.assertEqual(0, migrated["state"]["ladder_rank"])
        with self.assertRaises(MigrationError):
            run_42_to_43(migrated)

    def test_challenge_migration_preserves_standard_base_pack(self) -> None:
        current = GameEngine.new(load_catalog(), 53)
        old = current.snapshot()
        old["save_version"] = 43
        old["content_manifest"]["engine"] = "1.2.0"
        for key in ("expedition_mode", "active_modifiers", "enabled_packs"):
            del old["state"][key]
        before = deepcopy(old)
        migrated = run_43_to_44(old)
        self.assertEqual(before, old)
        self.assertEqual(44, migrated["save_version"])
        self.assertEqual("standard", migrated["state"]["expedition_mode"])
        self.assertEqual([], migrated["state"]["active_modifiers"])
        self.assertEqual(["base:core"], migrated["state"]["enabled_packs"])

    def test_loop_migration_preserves_pre_victory_run(self) -> None:
        current = GameEngine.new(load_catalog(), 54)
        old = current.snapshot()
        old["save_version"] = 44
        old["content_manifest"]["engine"] = "1.3.0"
        for key in (
            "base_victory", "base_victory_archived", "loop_depth",
            "archived_loop_depth", "score", "boss_sequence",
        ):
            del old["state"][key]
        before = deepcopy(old)
        migrated = run_44_to_45(old)
        self.assertEqual(before, old)
        self.assertEqual(45, migrated["save_version"])
        self.assertFalse(migrated["state"]["base_victory"])
        self.assertEqual([], migrated["state"]["boss_sequence"])

    def test_threat_budget_migration_preserves_frozen_legacy_rooms(self) -> None:
        current = GameEngine.new(load_catalog(), 55)
        old = current.snapshot()
        old["save_version"] = 45
        old["content_manifest"]["engine"] = "1.4.0"
        del old["state"]["encounter_budget_version"]
        before = deepcopy(old)
        migrated = run_45_to_46(old)
        self.assertEqual(before, old)
        self.assertEqual(46, migrated["save_version"])
        self.assertEqual("1.5.0", migrated["content_manifest"]["engine"])
        self.assertEqual(0, migrated["state"]["encounter_budget_version"])
        self.assertEqual(old["state"]["rooms"], migrated["state"]["rooms"])
        restored = GameEngine.from_snapshot(current.catalog, old)
        self.assertEqual(0, restored.state.encounter_budget_version)
        with self.assertRaises(MigrationError):
            run_45_to_46(migrated)


if __name__ == "__main__":
    unittest.main()
