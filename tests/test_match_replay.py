from __future__ import annotations

import json
import unittest

from kenjaku.simulation import (
    SELF_PLAY_MATCH_REPLAY_VALIDATION_KIND,
    reconstruct_self_play_match_report,
    run_self_play_match_sandbox,
    validate_self_play_match_replay,
)


class SelfPlayMatchReplayTests(unittest.TestCase):
    def test_reconstructs_deterministic_complete_matches_for_both_rulesets(self) -> None:
        for ruleset, max_rounds in (("tenhou-4p", 12), ("tenhou-3p", 9)):
            with self.subTest(ruleset=ruleset):
                report = run_self_play_match_sandbox(
                    games=1,
                    max_rounds=max_rounds,
                    max_turns_per_round=512,
                    seed="replay-validation",
                    ruleset=ruleset,
                    ron_policy="pass",
                    include_trajectories=True,
                )

                reconstructed = reconstruct_self_play_match_report(report)
                validation = validate_self_play_match_replay(report)

                self.assertTrue(report["game_summaries"][0]["completed"])
                self.assertEqual(reconstructed, report)
                self.assertEqual(validation["kind"], SELF_PLAY_MATCH_REPLAY_VALIDATION_KIND)
                self.assertTrue(validation["valid"])
                self.assertEqual(validation["checked_decisions"], report["decisions"])
                self.assertEqual(validation["mismatches"], [])
                json_round_trip = validate_self_play_match_replay(json.loads(json.dumps(report)))
                self.assertTrue(json_round_trip["valid"])

    def test_detects_replayed_trajectory_mutation(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=1,
            max_turns_per_round=8,
            seed="replay-mutation",
            include_trajectories=True,
        )
        mutated = json.loads(json.dumps(report))
        mutated["game_summaries"][0]["trajectory"][0]["chosen_action"]["tile"] = "N"

        validation = validate_self_play_match_replay(mutated)

        self.assertFalse(validation["valid"])
        self.assertEqual(validation["mismatches"], ["game_summaries"])

    def test_reconstructs_mixed_heuristic_opponents(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=1,
            max_turns_per_round=12,
            seed="replay-heuristic-opponents",
            ron_policy="pass",
            heuristic_seats=(1, 3),
            include_trajectories=True,
        )

        self.assertEqual(reconstruct_self_play_match_report(report), report)
        self.assertTrue(validate_self_play_match_replay(report)["valid"])

    def test_rejects_reports_without_trajectories(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=1,
            max_turns_per_round=8,
            seed="replay-without-trajectory",
        )

        with self.assertRaisesRegex(ValueError, "requires trajectories"):
            validate_self_play_match_replay(report)


if __name__ == "__main__":
    unittest.main()
