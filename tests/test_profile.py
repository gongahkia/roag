from __future__ import annotations

from copy import deepcopy
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.history import run_report
from dumbest_dungeon.profile import (
    ProfileError,
    migrate_profile,
    new_profile,
    read_profile,
    update_profile,
    write_profile,
)


class ProfileTests(unittest.TestCase):
    def test_fresh_profile_has_no_combat_power_or_roster_gate(self) -> None:
        profile = new_profile()
        self.assertEqual(1, profile["unlocked_rank"])
        self.assertEqual(0, profile["best_completed_rank"])
        self.assertEqual(0, profile["base_victories"])
        self.assertNotIn("health", profile)
        self.assertNotIn("damage", profile)
        self.assertNotIn("energy", profile)

    def test_report_update_is_idempotent_and_records_discovery_and_casualty(self) -> None:
        engine = GameEngine.new(load_catalog(), 4601)
        hero = engine.living_heroes()[0]
        engine.state.items["targeting_prism"] = 2
        engine.state.boons[hero.id] = {"blood_price": 1}
        engine.state.curses[hero.id] = {"base:red_debt": 1}
        engine.record("crew_death", hero.id, survivors=[])
        engine.record("encounter_start", "base:guardian_derelict", kind="guardian", plan="guard")
        engine.record("encounter_end", "combat", result="victory", rounds=4)
        report = run_report(engine, outcome="victory")

        updated = update_profile(new_profile(), report)
        repeated = update_profile(updated, report)

        self.assertEqual(updated, repeated)
        self.assertEqual(1, updated["base_victories"])
        self.assertIn("targeting_prism", updated["discoveries"]["items"])
        self.assertIn("blood_price", updated["discoveries"]["boons"])
        self.assertIn("base:red_debt", updated["discoveries"]["curses"])
        self.assertIn("base:guardian_derelict", updated["discoveries"]["guardians"])
        self.assertEqual(hero.id, updated["graveyard"][0]["hero_id"])

    def test_atomic_round_trip_and_strict_version_contract(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "profile.json"
            self.assertEqual(new_profile(), read_profile(path))
            write_profile(path, new_profile())
            self.assertEqual(new_profile(), read_profile(path))
            future = deepcopy(new_profile())
            future["profile_version"] += 1
            with self.assertRaises(ProfileError):
                migrate_profile(future)
            malformed = deepcopy(new_profile())
            malformed["power"] = 9
            with self.assertRaises(ProfileError):
                write_profile(path, malformed)
            self.assertEqual(new_profile(), read_profile(path))

    def test_ranked_victory_unlocks_only_the_next_global_rank(self) -> None:
        engine = GameEngine.new(load_catalog(), 4602, ladder_rank=1)
        report = run_report(engine, outcome="victory")
        profile = update_profile(new_profile(), report)
        self.assertEqual(1, profile["best_completed_rank"])
        self.assertEqual(2, profile["unlocked_rank"])


if __name__ == "__main__":
    unittest.main()
