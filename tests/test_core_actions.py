from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, TileType


class ActionTests(unittest.TestCase):
    def test_discard_helper_parses_tile_type(self) -> None:
        action = Action.discard("0m", tsumogiri=True)

        self.assertEqual(action.kind, ActionKind.DISCARD)
        self.assertEqual(action.tile, TileType.parse("5m"))
        self.assertTrue(action.tsumogiri)

    def test_tile_required_for_discard(self) -> None:
        with self.assertRaises(ValueError):
            Action(ActionKind.DISCARD)

    def test_tsumogiri_only_valid_for_discard(self) -> None:
        with self.assertRaises(ValueError):
            Action(ActionKind.PASS, tsumogiri=True)

    def test_pass_helper(self) -> None:
        self.assertEqual(Action.pass_(), Action(ActionKind.PASS))


if __name__ == "__main__":
    unittest.main()
