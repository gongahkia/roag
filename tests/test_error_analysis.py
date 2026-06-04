from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.training import DiscardExample, summarize_discard_predictions


class DiscardErrorAnalysisTests(unittest.TestCase):
    def test_summarizes_prediction_accuracy_by_bucket(self) -> None:
        examples = [
            _example(["1m", "2m", "3m"], "1m", event_index=10),
            _example(["4p", "5p", "6p"], "4p", event_index=50),
            _example(["E", "E", "C"], "E", event_index=120),
        ]

        summary = summarize_discard_predictions(
            examples,
            lambda example: (
                example.action.tile
                if example.event_index != 50
                else TileType.parse("1m")
            ),
        )

        self.assertEqual(summary["overall"]["examples"], 3)
        self.assertEqual(summary["overall"]["correct"], 2)
        self.assertEqual(summary["overall"]["accuracy"], 2 / 3)
        self.assertEqual(summary["by_tile_family"]["man"]["examples"], 1)
        self.assertEqual(summary["by_tile_family"]["pin"]["examples"], 1)
        self.assertEqual(summary["by_tile_family"]["honor"]["examples"], 1)
        self.assertEqual(summary["by_round_event_phase"]["early"]["correct"], 1)
        self.assertEqual(summary["by_round_event_phase"]["middle"]["correct"], 0)
        self.assertEqual(summary["by_round_event_phase"]["late"]["correct"], 1)
        self.assertEqual(summary["by_seat_turn_phase"]["early"]["correct"], 1)
        self.assertEqual(summary["by_seat_turn_phase"]["middle"]["correct"], 0)
        self.assertEqual(summary["by_seat_turn_phase"]["late"]["correct"], 1)
        self.assertEqual(
            sum(bucket["examples"] for bucket in summary["by_shanten_delta"].values()),
            3,
        )

    def test_empty_summary_keeps_stable_buckets(self) -> None:
        summary = summarize_discard_predictions([], lambda _: TileType.parse("1m"))

        self.assertEqual(summary["overall"]["examples"], 0)
        self.assertIsNone(summary["overall"]["accuracy"])
        self.assertEqual(summary["by_shanten_delta"]["preserved"]["examples"], 0)
        self.assertEqual(summary["by_tile_family"]["sou"]["examples"], 0)
        self.assertEqual(summary["by_round_event_phase"]["late"]["examples"], 0)
        self.assertEqual(summary["by_seat_turn_phase"]["late"]["examples"], 0)


def _example(hand: list[str], discard: str, *, event_index: int) -> DiscardExample:
    tiles = tuple(Tile.parse(tile) for tile in hand)
    return DiscardExample(
        round_index=0,
        event_index=event_index,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(tiles),
        visible_counts=tile_counts(tiles),
        action=Action.discard(discard),
        seat_turn_index=event_index // 8,
    )


if __name__ == "__main__":
    unittest.main()
