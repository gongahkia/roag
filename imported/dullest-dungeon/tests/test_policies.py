from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.policies import Command, Policy, canonical_hash, execute_command, legal_plays, run_policy


class PolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_policy_selection_does_not_mutate_simulation_or_read_draw_order(self) -> None:
        engine = GameEngine.new(self.catalog, 42)
        engine.start_combat("lost_shift")
        before = canonical_hash(engine)
        first = Policy().next_command(engine)
        self.assertEqual(before, canonical_hash(engine))
        engine.state.draw_pile.reverse()
        self.assertEqual(first, Policy().next_command(engine))

    def test_only_public_player_commands_can_be_replayed(self) -> None:
        engine = GameEngine.new(self.catalog, 42)
        before = canonical_hash(engine)
        with self.assertRaisesRegex(RuleError, "not a regression-player command"):
            execute_command(engine, Command("_damage", ("warden", 999)))
        self.assertEqual(before, canonical_hash(engine))

    def test_policy_loop_limit_chooses_descent_then_extraction(self) -> None:
        engine = GameEngine.new(self.catalog, 14)
        engine.state.phase = "post_victory"
        engine.state.base_victory = True
        policy = Policy("explorer", loop_limit=1)
        self.assertEqual(Command("descend_again"), policy.next_command(engine))
        engine.state.loop_depth = 1
        self.assertEqual(Command("extract"), policy.next_command(engine))
        with self.assertRaises(ValueError):
            Policy(loop_limit=-1)

    def test_legal_plays_respect_owner_rank_and_energy(self) -> None:
        engine = GameEngine.new(self.catalog, 42)
        engine.start_combat("lost_shift")
        for command in legal_plays(engine):
            clone = GameEngine.from_snapshot(self.catalog, engine.snapshot())
            execute_command(clone, command)
            self.assertGreaterEqual(clone.state.energy, 0)

    def test_normal_command_run_replays_and_checkpoint_continuation_matches(self) -> None:
        engine = GameEngine.new(self.catalog, 42)
        report = run_policy(engine, Policy("explorer"))
        self.assertEqual("victory", report["outcome"])
        self.assertFalse(report["bounded_stop"])
        replay = GameEngine.new(self.catalog, 42)
        for row in report["commands"]:
            raw = row["command"]
            execute_command(replay, Command(raw["method"], tuple(raw["args"])))
            self.assertEqual(row["hash"], canonical_hash(replay), row["sequence"])
        checkpointed = run_policy(GameEngine.new(self.catalog, 42), Policy("explorer"), checkpoint_every=17)
        self.assertEqual(report["commands"], checkpointed["commands"])
        self.assertEqual(json.dumps(report["final"], sort_keys=True), json.dumps(checkpointed["final"], sort_keys=True))

    def test_bounded_stop_is_not_recorded_as_a_win_or_loss(self) -> None:
        report = run_policy(GameEngine.new(self.catalog, 42), Policy("rusher"), limit=2)
        self.assertTrue(report["bounded_stop"])
        self.assertEqual(2, len(report["commands"]))


if __name__ == "__main__":
    unittest.main()
