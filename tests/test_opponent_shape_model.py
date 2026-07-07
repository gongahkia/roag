from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, tile_counts
from kenjaku.models import (
    OPPONENT_SHAPE_BASELINE_KIND,
    OPPONENT_SHAPE_YAKU,
    OpponentShapeBaseline,
)
from kenjaku.training import DiscardExample


class OpponentShapeBaselineTests(unittest.TestCase):
    def test_predicts_probability_distribution_over_top_ten_yaku(self) -> None:
        model = OpponentShapeBaseline()

        probabilities = model.predict_from_discards(["1m", "9m", "E", "5p"])

        self.assertEqual(model.kind, OPPONENT_SHAPE_BASELINE_KIND)
        self.assertEqual(tuple(probabilities), OPPONENT_SHAPE_YAKU)
        self.assertEqual(len(probabilities), 10)
        self.assertAlmostEqual(sum(probabilities.values()), 1.0)
        self.assertTrue(all(value > 0 for value in probabilities.values()))

    def test_terminal_and_honor_discards_raise_tanyao_prior(self) -> None:
        model = OpponentShapeBaseline()
        terminal_heavy = model.predict_from_discards(["1m", "9m", "E", "S", "1p", "9p"])
        simple_heavy = model.predict_from_discards(["2m", "3m", "4p", "5p", "6s", "8s"])

        self.assertGreater(terminal_heavy["tanyao"], simple_heavy["tanyao"])
        self.assertGreater(terminal_heavy["pinfu"], simple_heavy["pinfu"])

    def test_suit_avoidance_raises_flush_priors(self) -> None:
        model = OpponentShapeBaseline()
        suit_avoidance = model.predict_from_discards(["1m", "2m", "3m", "7p", "8p", "9p"])
        balanced = model.predict_from_discards(["1m", "2p", "3s", "4m", "5p", "6s"])

        self.assertGreater(suit_avoidance["honitsu"], balanced["honitsu"])
        self.assertGreater(suit_avoidance["ittsu"], balanced["ittsu"])

    def test_predicts_from_discard_example_opponent_rivers(self) -> None:
        model = OpponentShapeBaseline()
        example = _example(opponent_river=["1m", "9m", "E", "S"])

        probabilities = model.predict_for_seat(example, 1)
        opponents = model.predict_opponents(example)

        self.assertEqual(probabilities, opponents[1])
        self.assertNotIn(0, opponents)
        self.assertEqual(set(opponents), {1, 2, 3})
        with self.assertRaises(ValueError):
            model.predict_for_seat(example, 4)


def _example(*, opponent_river: list[str]) -> DiscardExample:
    hand = tuple(Tile.parse(tile) for tile in ["2m", "3m"])
    opponent_river_tiles = tuple(Tile.parse(tile) for tile in opponent_river)
    rivers_by_seat = ((), opponent_river_tiles, (), ())
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(hand),
        visible_counts=tile_counts((*hand, *opponent_river_tiles)),
        action=Action.discard("2m"),
        active_riichi_seats=(False, False, False, False),
        river_counts_by_seat=tuple(tile_counts(river) for river in rivers_by_seat),
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=(None, None, None, None),
        riichi_declared_event_indices=(None, None, None, None),
    )


if __name__ == "__main__":
    unittest.main()
