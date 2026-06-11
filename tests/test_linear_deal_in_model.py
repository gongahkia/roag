from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.models import (
    DEAL_IN_LINEAR_FEATURE_DIM,
    DEAL_IN_LINEAR_MODEL_KIND,
    DealInLinearModel,
    evaluate_deal_in_probabilities,
    heuristic_deal_in_probabilities,
)
from kenjaku.training import DealInExample, DiscardExample


class DealInLinearModelTests(unittest.TestCase):
    def test_fit_predicts_higher_probability_for_live_riichi_discard(self) -> None:
        safe = _deal_in_example(
            dealt_in=False,
            hand=["4m", "5m"],
            discard="4m",
            opponent_river=["4m"],
        )
        dangerous = _deal_in_example(
            dealt_in=True,
            hand=["5m", "6m"],
            discard="5m",
            opponent_river=["1m"],
        )

        model = DealInLinearModel.fit(
            [safe, dangerous],
            epochs=80,
            learning_rate=0.2,
            positive_class_weight=2.0,
        )

        self.assertEqual(model.kind, DEAL_IN_LINEAR_MODEL_KIND)
        self.assertEqual(model.feature_dim, DEAL_IN_LINEAR_FEATURE_DIM)
        self.assertGreater(
            model.predict_probability(dangerous),
            model.predict_probability(safe),
        )
        metrics = model.evaluate([safe, dangerous], threshold=0.5)
        self.assertEqual(metrics["examples"], 2)
        self.assertEqual(metrics["positives"], 1)
        self.assertIsNotNone(metrics["brier_score"])

    def test_evaluates_probability_sequences_and_heuristic_baseline(self) -> None:
        safe = _deal_in_example(
            dealt_in=False,
            hand=["4m", "5m"],
            discard="4m",
            opponent_river=["4m"],
        )
        dangerous = _deal_in_example(
            dealt_in=True,
            hand=["5m", "6m"],
            discard="5m",
            opponent_river=["1m"],
        )
        examples = [safe, dangerous]

        metrics = evaluate_deal_in_probabilities(examples, [0.1, 0.9], threshold=0.5)
        heuristic = heuristic_deal_in_probabilities(examples)

        self.assertEqual(metrics["true_positive"], 1)
        self.assertEqual(metrics["true_negative"], 1)
        self.assertEqual(metrics["accuracy"], 1.0)
        self.assertLess(heuristic[0], heuristic[1])

    def test_round_trips_model_payload(self) -> None:
        example = _deal_in_example(
            dealt_in=False,
            hand=["4m", "5m"],
            discard="4m",
            opponent_river=["4m"],
        )
        model = DealInLinearModel.fit([example], epochs=0)

        restored = DealInLinearModel.from_dict(model.to_dict())

        self.assertEqual(restored.kind, DEAL_IN_LINEAR_MODEL_KIND)
        self.assertEqual(restored.weights, model.weights)
        with TemporaryDirectory() as directory:
            path = Path(directory) / "deal-in.json"
            model.save(path)
            loaded = DealInLinearModel.load(path)
        self.assertEqual(loaded.weights, model.weights)


def _deal_in_example(
    *,
    dealt_in: bool,
    hand: list[str],
    discard: str,
    opponent_river: list[str],
) -> DealInExample:
    hand_tiles = tuple(Tile.parse(tile) for tile in hand)
    river_tiles = tuple(Tile.parse(tile) for tile in opponent_river)
    rivers_by_seat = ((), river_tiles, (), ())
    return DealInExample(
        discard=DiscardExample(
            round_index=0,
            event_index=0,
            seat=0,
            dealer=0,
            scores=(25000, 25000, 25000, 25000),
            hand_counts=tile_counts(hand_tiles),
            visible_counts=tile_counts((*hand_tiles, *river_tiles)),
            action=Action.discard(TileType.parse(discard)),
            active_riichi_seats=(False, True, False, False),
            river_counts_by_seat=tuple(tile_counts(river) for river in rivers_by_seat),
            seat_turn_index=8,
            rivers_by_seat=rivers_by_seat,
            riichi_declared_turns=(None, 0, None, None),
            riichi_declared_event_indices=(None, 1, None, None),
        ),
        dealt_in=dealt_in,
        label_source="terminal_ron_discard",
        outcome_kind="agari",
    )


if __name__ == "__main__":
    unittest.main()
