from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.models import DiscardLinearModel
from kenjaku.training import DiscardExample


class LinearDiscardModelTests(unittest.TestCase):
    def test_fit_predict_and_score(self) -> None:
        examples = [
            _example(["1m", "2m", "3m"], "1m"),
            _example(["1m", "2m", "3m"], "1m"),
            _example(["4p", "5p", "6p"], "6p"),
            _example(["4p", "5p", "6p"], "6p"),
        ]

        model = DiscardLinearModel.fit(examples, epochs=20, learning_rate=0.2)

        self.assertEqual(
            model.predict(examples[0].hand_counts, examples[0].visible_counts),
            TileType.parse("1m"),
        )
        self.assertEqual(
            model.predict(examples[2].hand_counts, examples[2].visible_counts),
            TileType.parse("6p"),
        )
        self.assertEqual(model.score(examples), 1.0)

    def test_rejects_empty_training_set(self) -> None:
        with self.assertRaises(ValueError):
            DiscardLinearModel.fit([])

    def test_rejects_empty_hand_prediction(self) -> None:
        model = DiscardLinearModel.fit([_example(["1m"], "1m")])

        with self.assertRaises(ValueError):
            model.predict(tuple([0] * 34), tuple([0] * 34))


def _example(hand: list[str], discard: str) -> DiscardExample:
    tiles = tuple(Tile.parse(tile) for tile in hand)
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
