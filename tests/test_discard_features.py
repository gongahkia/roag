from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, tile_counts
from kenjaku.training import DiscardExample, discard_shanten_delta, summarize_discard_shanten


class DiscardFeatureTests(unittest.TestCase):
    def test_discard_shanten_delta_tracks_shape_preservation(self) -> None:
        example = _example(
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E 4m 5m 9s",
            "9s",
        )

        delta = discard_shanten_delta(example)

        self.assertEqual(delta.before, 0)
        self.assertEqual(delta.after, 0)
        self.assertEqual(delta.delta, 0)

    def test_summarizes_discard_shanten_deltas(self) -> None:
        examples = [
            _example("1m 2m 3m 1p 2p 3p 1s 2s 3s E E 4m 5m 9s", "9s"),
            _example("1m 2m 3m 1p 2p 3p 1s 2s 3s E E 4m 5m 9s", "4m"),
        ]

        summary = summarize_discard_shanten(examples)

        self.assertEqual(summary["examples"], 2)
        self.assertEqual(summary["average_before"], 0.0)
        self.assertEqual(summary["average_after"], 0.5)
        self.assertEqual(summary["average_delta"], 0.5)
        self.assertEqual(summary["preserved"], 1)
        self.assertEqual(summary["worsened"], 1)

    def test_empty_summary_is_explicit(self) -> None:
        summary = summarize_discard_shanten([])

        self.assertEqual(summary["examples"], 0)
        self.assertIsNone(summary["average_delta"])

    def test_rejects_action_missing_from_hand(self) -> None:
        with self.assertRaises(ValueError):
            discard_shanten_delta(_example("1m 2m 3m", "4m"))


def _example(hand: str, discard: str) -> DiscardExample:
    tiles = tuple(Tile.parse(token) for token in hand.split())
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(tiles),
        visible_counts=tile_counts(tiles),
        action=Action.discard(discard),
    )


if __name__ == "__main__":
    unittest.main()
