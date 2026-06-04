from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.models import (
    RAW_COUNT_FEATURE_PROFILE,
    RISK_CONTEXT_FEATURE_PROFILE,
    DiscardLinearModel,
)
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

    def test_round_trips_json_artifact(self) -> None:
        examples = [_example(["1m", "2m"], "1m"), _example(["1m", "2m"], "1m")]
        model = DiscardLinearModel.fit(examples, epochs=3, learning_rate=0.2)
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        self.assertEqual(payload["kind"], "discard-linear-v1")
        self.assertEqual(payload["feature_dim"], 76)
        self.assertEqual(model.kind, "discard-linear-v1")
        self.assertEqual(model.feature_dim, 76)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(loaded.score(examples), model.score(examples))

    def test_raw_count_profile_round_trips_json_artifact(self) -> None:
        examples = [_example(["1m", "2m"], "1m"), _example(["1m", "2m"], "1m")]
        model = DiscardLinearModel.fit(
            examples,
            epochs=3,
            learning_rate=0.2,
            feature_profile=RAW_COUNT_FEATURE_PROFILE,
        )
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-raw-count.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        self.assertEqual(payload["kind"], "discard-linear-raw-count-v0")
        self.assertEqual(payload["feature_profile"], "raw-count")
        self.assertEqual(payload["feature_dim"], 69)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(loaded.score(examples), model.score(examples))

    def test_risk_context_profile_round_trips_json_artifact(self) -> None:
        examples = [
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["1m", "9m"],
            ),
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["1m", "9m"],
            ),
        ]
        model = DiscardLinearModel.fit(
            examples,
            epochs=3,
            learning_rate=0.2,
            feature_profile=RISK_CONTEXT_FEATURE_PROFILE,
        )
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-risk-context.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        prediction = model.predict(
            examples[0].hand_counts,
            examples[0].visible_counts,
            seat=examples[0].seat,
            active_riichi_seats=examples[0].active_riichi_seats,
            river_counts_by_seat=examples[0].river_counts_by_seat,
        )

        self.assertEqual(payload["kind"], "discard-linear-risk-context-v0")
        self.assertEqual(payload["feature_profile"], "risk-context")
        self.assertEqual(payload["feature_dim"], 86)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(prediction, TileType.parse("1m"))
        self.assertEqual(loaded.score(examples), model.score(examples))


def _example(
    hand: list[str],
    discard: str,
    *,
    active_riichi_seats: tuple[bool, ...] = (),
    opponent_river: list[str] | None = None,
) -> DiscardExample:
    tiles = tuple(Tile.parse(tile) for tile in hand)
    opponent_river_tiles = tuple(Tile.parse(tile) for tile in opponent_river or [])
    river_counts_by_seat = (
        tuple([0] * 34),
        tile_counts(opponent_river_tiles),
        tuple([0] * 34),
        tuple([0] * 34),
    )
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(tiles),
        visible_counts=tile_counts(tiles),
        action=Action.discard(discard),
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
    )


if __name__ == "__main__":
    unittest.main()
