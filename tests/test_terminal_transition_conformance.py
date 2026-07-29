from __future__ import annotations

import unittest

from kenjaku.core import TileType
from kenjaku.simulation import SandboxEnvironmentState, next_round_sandbox_environment


class TerminalTransitionConformanceTests(unittest.TestCase):
    def test_abortive_draw_matrix_repeats_dealer_and_preserves_riichi_sticks(self) -> None:
        reasons = ("kyuushu_kyuuhai", "four_winds", "four_riichi", "four_kans", "triple_ron")
        for ruleset, players in (("tenhou-4p", 4), ("tenhou-3p", 3)):
            for reason in reasons:
                with self.subTest(ruleset=ruleset, reason=reason):
                    terminal = SandboxEnvironmentState(
                        ruleset=ruleset,
                        players=players,
                        wall=(),
                        hands=tuple(() for _seat in range(players)),
                        terminal_reason=reason,
                        points=(25000,) * players,
                        riichi_sticks=2,
                        honba=1,
                        dealer_seat=0,
                        round_wind=TileType.parse("E"),
                    )

                    next_round = next_round_sandbox_environment(
                        terminal, seed=f"{ruleset}:{reason}"
                    )

                    self.assertEqual(next_round.dealer_seat, 0)
                    self.assertEqual(next_round.current_seat, 0)
                    self.assertEqual(next_round.honba, 2)
                    self.assertEqual(next_round.riichi_sticks, 2)
                    self.assertEqual(next_round.points, terminal.points)

    def test_exhaustive_draw_matrix_uses_dealer_tenpai_for_rotation(self) -> None:
        for ruleset, players in (("tenhou-4p", 4), ("tenhou-3p", 3)):
            for tenpai_seats, expected_dealer, expected_honba in (
                ((0,), 0, 2),
                ((1,), 1, 2),
            ):
                with self.subTest(ruleset=ruleset, tenpai_seats=tenpai_seats):
                    terminal = SandboxEnvironmentState(
                        ruleset=ruleset,
                        players=players,
                        wall=(),
                        hands=tuple(() for _seat in range(players)),
                        terminal_reason="wall_exhausted",
                        points=(25000,) * players,
                        riichi_sticks=1,
                        honba=1,
                        dealer_seat=0,
                        round_wind=TileType.parse("E"),
                        exhaustive_draw_tenpai_seats=tenpai_seats,
                        exhaustive_draw_noten_seats=tuple(
                            seat for seat in range(players) if seat not in tenpai_seats
                        ),
                    )

                    next_round = next_round_sandbox_environment(
                        terminal, seed=f"{ruleset}:tenpai:{tenpai_seats}"
                    )

                    self.assertEqual(next_round.dealer_seat, expected_dealer)
                    self.assertEqual(next_round.current_seat, expected_dealer)
                    self.assertEqual(next_round.honba, expected_honba)
                    self.assertEqual(next_round.riichi_sticks, 1)
