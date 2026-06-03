from __future__ import annotations

import unittest

from kenjaku.core import Discard, PlayerState, RoundState, TENHOU_3P, TENHOU_4P, Tile


class StateTests(unittest.TestCase):
    def test_round_state_supports_out_of_order_players(self) -> None:
        state = RoundState(
            players=(
                PlayerState(2),
                PlayerState(0),
                PlayerState(3),
                PlayerState(1),
            )
        )

        self.assertEqual(state.player(3).seat, 3)

    def test_visible_tiles_are_perspective_scoped(self) -> None:
        state = RoundState(
            dora_indicators=(Tile.parse("3p"),),
            players=(
                PlayerState(0, hand=(Tile.parse("1m"), Tile.parse("0m"))),
                PlayerState(1, hand=(Tile.parse("9s"),), discards=(Discard(Tile.parse("E")),)),
                PlayerState(2),
                PlayerState(3),
            ),
        )

        visible = [tile.notation for tile in state.visible_tiles(0)]

        self.assertEqual(visible, ["3p", "1m", "0m", "E"])

    def test_unseen_counts_subtract_visible_tile_types(self) -> None:
        state = RoundState(
            dora_indicators=(Tile.parse("3p"),),
            players=(
                PlayerState(0, hand=(Tile.parse("1m"), Tile.parse("0m"))),
                PlayerState(1, discards=(Discard(Tile.parse("E")),)),
                PlayerState(2),
                PlayerState(3),
            ),
        )

        counts = state.unseen_type_counts(0)

        self.assertEqual(counts[Tile.parse("1m").type.index], 3)
        self.assertEqual(counts[Tile.parse("5m").type.index], 3)
        self.assertEqual(counts[Tile.parse("3p").type.index], 3)
        self.assertEqual(counts[Tile.parse("E").type.index], 3)

    def test_ruleset_player_count_must_match_state(self) -> None:
        with self.assertRaises(ValueError):
            RoundState(rules=TENHOU_3P, players=tuple(PlayerState(i) for i in range(4)))

    def test_tenhou_4p_defaults_are_valid(self) -> None:
        state = RoundState(rules=TENHOU_4P)
        self.assertEqual(len(state.players), 4)


if __name__ == "__main__":
    unittest.main()
