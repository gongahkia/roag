from __future__ import annotations

import unittest

from kenjaku.commands._legacy import build_parser
from kenjaku.simulation import (
    choose_heuristic_sandbox_action,
    draw_for_current_seat,
    format_self_play_match_report,
    initial_sandbox_environment,
    legal_sandbox_actions,
    run_self_play_match_sandbox,
)


class HeuristicOpponentTests(unittest.TestCase):
    def test_selects_deterministic_legal_actions_for_4p_and_sanma(self) -> None:
        for ruleset in ("tenhou-4p", "tenhou-3p"):
            with self.subTest(ruleset=ruleset):
                state = draw_for_current_seat(initial_sandbox_environment(ruleset=ruleset, seed=8))
                legal_actions = legal_sandbox_actions(state, seat=0)
                first = choose_heuristic_sandbox_action(state, seat=0, legal_actions=legal_actions)
                second = choose_heuristic_sandbox_action(state, seat=0, legal_actions=legal_actions)

                self.assertIn(first, legal_actions)
                self.assertEqual(first, second)

    def test_runs_mixed_heuristic_opponents_deterministically(self) -> None:
        kwargs = {
            "games": 1,
            "max_rounds": 1,
            "max_turns_per_round": 24,
            "seed": "mixed-opponents",
            "ron_policy": "pass",
            "heuristic_seats": (1, 3),
            "include_trajectories": True,
        }
        first = run_self_play_match_sandbox(**kwargs)
        second = run_self_play_match_sandbox(**kwargs)

        self.assertEqual(first, second)
        self.assertEqual(first["opponents"]["heuristic_seats"], [1, 3])
        self.assertEqual(
            first["opponents"]["seat_policy_kind"],
            ["configured", "heuristic", "configured", "heuristic"],
        )
        self.assertTrue(first["capabilities"]["mixed_heuristic_opponents"])
        self.assertGreater(first["decisions"], 0)
        self.assertIn("opponents: 0=configured 1=heuristic", format_self_play_match_report(first))

    def test_validates_seat_configuration_and_cli_parser(self) -> None:
        with self.assertRaisesRegex(ValueError, "valid seat"):
            run_self_play_match_sandbox(
                games=1,
                max_rounds=1,
                max_turns_per_round=1,
                seed="bad-seat",
                heuristic_seats=(3,),
                ruleset="tenhou-3p",
            )
        args = build_parser().parse_args(
            ["self-play-match-sandbox", "--heuristic-seats", "0,2"]
        )

        self.assertEqual(args.heuristic_seats, "0,2")

    def test_runs_mixed_sanma_opponent(self) -> None:
        report = run_self_play_match_sandbox(
            games=1,
            max_rounds=1,
            max_turns_per_round=24,
            seed="mixed-sanma-opponents",
            ruleset="tenhou-3p",
            ron_policy="pass",
            heuristic_seats=(1,),
        )

        self.assertEqual(report["players"], 3)
        self.assertEqual(report["opponents"]["heuristic_seats"], [1])
        self.assertEqual(
            report["opponents"]["seat_policy_kind"],
            ["configured", "heuristic", "configured"],
        )


if __name__ == "__main__":
    unittest.main()
