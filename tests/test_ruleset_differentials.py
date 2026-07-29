from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.simulation import (
    SANDBOX_3P_INITIAL_POINTS,
    SANDBOX_INITIAL_POINTS,
    SandboxEnvironmentState,
    initial_sandbox_environment,
    legal_call_actions,
    legal_kita_actions,
    next_round_sandbox_environment,
)


class RulesetDifferentialTests(unittest.TestCase):
    def test_initial_tile_universe_and_points_differ_by_ruleset(self) -> None:
        four_player = initial_sandbox_environment(ruleset="tenhou-4p", seed="ruleset-diff")
        three_player = initial_sandbox_environment(ruleset="tenhou-3p", seed="ruleset-diff")

        self.assertEqual(
            four_player,
            initial_sandbox_environment(ruleset="tenhou-4p", seed="ruleset-diff"),
        )
        self.assertEqual(
            three_player,
            initial_sandbox_environment(ruleset="tenhou-3p", seed="ruleset-diff"),
        )
        self.assertEqual(four_player.players, 4)
        self.assertEqual(four_player.points, (SANDBOX_INITIAL_POINTS,) * 4)
        self.assertEqual(three_player.players, 3)
        self.assertEqual(three_player.points, (SANDBOX_3P_INITIAL_POINTS,) * 3)
        self.assertEqual(_tile_count(four_player, "5m"), 4)
        self.assertEqual(_tile_count(three_player, "5m"), 0)

    def test_chi_and_kita_actions_differ_by_ruleset(self) -> None:
        chi = Action(
            ActionKind.CHI,
            TileType.parse("3p"),
            consumed=(Tile.parse("1p"), Tile.parse("2p")),
        )
        four_player_reaction = _reaction_state(ruleset="tenhou-4p", players=4)
        three_player_reaction = _reaction_state(ruleset="tenhou-3p", players=3)

        self.assertIn(chi, legal_call_actions(four_player_reaction, seat=1))
        self.assertNotIn(chi, legal_call_actions(three_player_reaction, seat=1))

        kita = Action(
            ActionKind.KITA,
            TileType.parse("N"),
            consumed=(Tile.parse("N"),),
        )
        self.assertEqual(legal_kita_actions(_kita_state(ruleset="tenhou-4p", players=4)), ())
        self.assertEqual(legal_kita_actions(_kita_state(ruleset="tenhou-3p", players=3)), (kita,))

    def test_final_placement_config_differ_by_ruleset(self) -> None:
        four_player = next_round_sandbox_environment(
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(),
                hands=((), (), (), ()),
                terminal_reason="ron",
                winner_seat=0,
                winner_seats=(0,),
                points=(32000, 28000, 25000, 14000),
                dealer_seat=3,
                round_wind=TileType.parse("S"),
            ),
            seed="ruleset-diff-final",
        )
        three_player = next_round_sandbox_environment(
            SandboxEnvironmentState(
                ruleset="tenhou-3p",
                players=3,
                wall=(),
                hands=((), (), ()),
                terminal_reason="ron",
                winner_seat=0,
                winner_seats=(0,),
                points=(41000, 35000, 29000),
                dealer_seat=2,
                round_wind=TileType.parse("S"),
            ),
            seed="ruleset-diff-final",
        )

        assert four_player.final_result is not None
        assert three_player.final_result is not None
        self.assertEqual(four_player.final_result.return_points, 30000)
        self.assertEqual(four_player.final_result.uma_by_rank, (20.0, 10.0, -10.0, -20.0))
        self.assertEqual(three_player.final_result.return_points, 40000)
        self.assertEqual(three_player.final_result.uma_by_rank, (20.0, 0.0, -20.0))


def _reaction_state(*, ruleset: str, players: int) -> SandboxEnvironmentState:
    return SandboxEnvironmentState(
        ruleset=ruleset,
        players=players,
        wall=(),
        hands=((), (Tile.parse("1p"), Tile.parse("2p")), *(((),) * (players - 2))),
        pending_discard=Tile.parse("3p"),
        pending_discard_seat=0,
        pending_reaction_seats=(1,),
    )


def _kita_state(*, ruleset: str, players: int) -> SandboxEnvironmentState:
    north = Tile.parse("N")
    return SandboxEnvironmentState(
        ruleset=ruleset,
        players=players,
        wall=(),
        hands=((north,), *(((),) * (players - 1))),
        drawn_tile=north,
    )


def _tile_count(state: SandboxEnvironmentState, notation: str) -> int:
    return sum(
        tile.notation == notation
        for hand in state.hands
        for tile in hand
    ) + sum(tile.notation == notation for tile in state.wall + state.dead_wall)


if __name__ == "__main__":
    unittest.main()
