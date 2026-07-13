from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Meld, Tile
from kenjaku.simulation import (
    SandboxEnvironmentState,
    apply_call_action,
    apply_discard_action,
    apply_kakan_action,
    draw_for_current_seat,
    legal_call_actions,
    legal_kakan_actions,
)


class SanmaKanTimingTests(unittest.TestCase):
    def test_minkan_claim_uses_kan_replacement_and_reveals_kan_dora(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("3p"),),
            dead_wall=_tiles("1p 2p 8s"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                _tiles("3p 3p 3p 1m 9m 1p 2p 4p 5p 6p 7p 1s 2s"),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        pending, _discard = apply_discard_action(drawn, Action.discard("3p"))
        minkan = next(
            action
            for action in legal_call_actions(pending, seat=1)
            if action.kind is ActionKind.MINKAN
        )

        after_kan, meld = apply_call_action(pending, seat=1, action=minkan)

        self.assertEqual(meld.kind, ActionKind.MINKAN)
        self.assertEqual(after_kan.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_kan.rinshan_draw)
        self.assertEqual(after_kan.dora_indicators, _tiles("1p 2p"))
        self.assertEqual(after_kan.to_payload()["kita_tiles"], [[], [], []])

    def test_kakan_uses_same_sanma_kan_timing(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3p 3p 3p"),
            called_tile=Tile.parse("3p"),
            from_seat=2,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("3p"),),
            dead_wall=_tiles("1p 2p 8s"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 4p 5p 6p 7p 1s 2s"),
                (),
                (),
            ),
            melds=((pon,), (), ()),
        )
        drawn = draw_for_current_seat(state)

        after_kan, meld = apply_kakan_action(drawn, legal_kakan_actions(drawn)[0])

        self.assertEqual(meld.kind, ActionKind.KAKAN)
        self.assertEqual(after_kan.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_kan.rinshan_draw)
        self.assertEqual(after_kan.dora_indicators, _tiles("1p 2p"))
        self.assertEqual(after_kan.to_payload()["kita_tiles"], [[], [], []])


def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())
