from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import TileType
from kenjaku.io import parse_tenhou_xml_file
from kenjaku.models import DiscardFrequencyBaseline
from kenjaku.training import iter_discard_examples

FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")


class FrequencyBaselineTests(unittest.TestCase):
    def test_fit_predict_and_score(self) -> None:
        game = parse_tenhou_xml_file(FIXTURE)
        examples = list(iter_discard_examples(game))

        model = DiscardFrequencyBaseline.fit(examples)

        self.assertEqual(model.counts[TileType.parse("7p").index], 1)
        self.assertEqual(model.counts[TileType.parse("4p").index], 1)
        self.assertEqual(model.top_tile, TileType.parse("4p"))
        self.assertEqual(model.predict(examples[0].hand_counts), TileType.parse("4p"))
        self.assertEqual(model.score(examples), 0.5)

    def test_rejects_empty_hand_prediction(self) -> None:
        model = DiscardFrequencyBaseline(tuple([0] * 34))

        with self.assertRaises(ValueError):
            model.predict(tuple([0] * 34))


if __name__ == "__main__":
    unittest.main()
