from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.simulation import SandboxEnvironmentState, draw_for_current_seat, legal_ankan_actions


class PostRiichiKanLegalityTests(unittest.TestCase):
    def test_tenhou_four_player_ankan_allows_shape_change_when_waits_are_preserved(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("2m"),),
            hands=(
                _tiles("2m 2m 2m 3m 3m 3m 4m 4m 4m 2p 3p 4p 5p"),
                (),
                (),
                (),
            ),
            riichi_seats=(0,),
        )
        drawn = draw_for_current_seat(state)
        ankan = _ankan("2m")

        self.assertEqual(legal_ankan_actions(drawn), (ankan,))

    def test_tenhou_four_player_ankan_rejects_okuri_kan(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("8s"),),
            hands=(
                _tiles("2m 2m 2m 2m 3m 3m 3m 4m 4m 4m 2p 3p 4p"),
                (),
                (),
                (),
            ),
            riichi_seats=(0,),
        )

        self.assertEqual(legal_ankan_actions(draw_for_current_seat(state)), ())

    def test_ankan_requires_exactly_four_copies(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("2m"),),
            hands=(
                _tiles("2m 2m 2m 2m 3m 3m 3m 4m 4m 4m 2p 3p 4p"),
                (),
                (),
                (),
            ),
        )

        self.assertNotIn(_ankan("2m"), legal_ankan_actions(draw_for_current_seat(state)))


def _ankan(tile: str) -> Action:
    parsed = Tile.parse(tile)
    return Action(
        ActionKind.ANKAN,
        TileType.parse(tile),
        consumed=(parsed, parsed, parsed, parsed),
    )


def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())
