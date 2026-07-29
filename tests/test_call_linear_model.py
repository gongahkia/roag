from __future__ import annotations

import unittest
from unittest import mock

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.models import (
    CALL_LINEAR_FEATURE_DIM,
    CALL_LINEAR_V1_FEATURE_DIM,
    CALL_LINEAR_V1_FEATURE_PROFILE,
    CallLinearModel,
)
from kenjaku.models.linear_call import (
    _SHANTEN_CACHE_MAXSIZE,
    _UKEIRE_CACHE_MAXSIZE,
    _chi_shape,
    _safe_shanten,
    _ukeire_proxy,
)
from kenjaku.training import CallExample


class CallLinearModelTests(unittest.TestCase):
    def test_fit_predicts_selective_pass_and_call(self) -> None:
        examples = [
            *[
                _example(
                    discarded="1m",
                    legal_call_kinds=(ActionKind.PON,),
                    action=Action(ActionKind.PON, TileType.parse("1m")),
                )
                for _ in range(6)
            ],
            *[
                _example(
                    discarded="2m",
                    legal_call_kinds=(ActionKind.PON,),
                    action=Action.pass_(),
                )
                for _ in range(6)
            ],
        ]

        model = CallLinearModel.fit(examples, epochs=30, learning_rate=0.2)

        self.assertEqual(model.kind, "call-linear-v0")
        self.assertEqual(model.feature_dim, CALL_LINEAR_FEATURE_DIM)
        self.assertEqual(
            model.predict(_example(discarded="1m", legal_call_kinds=(ActionKind.PON,))),
            ActionKind.PON,
        )
        self.assertEqual(
            model.predict(_example(discarded="2m", legal_call_kinds=(ActionKind.PON,))),
            ActionKind.PASS,
        )

    def test_v1_profile_keeps_v0_additive(self) -> None:
        examples = [
            _example(
                discarded="1m",
                legal_call_kinds=(ActionKind.PON,),
                action=Action(ActionKind.PON, TileType.parse("1m")),
            )
            for _ in range(3)
        ]

        v0_model = CallLinearModel.fit(examples, epochs=5, learning_rate=0.2)
        v1_model = CallLinearModel.fit(
            examples,
            epochs=5,
            learning_rate=0.2,
            feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
        )

        self.assertEqual(v0_model.kind, "call-linear-v0")
        self.assertEqual(v0_model.feature_dim, CALL_LINEAR_FEATURE_DIM)
        self.assertEqual(v1_model.kind, "call-linear-v1")
        self.assertEqual(v1_model.feature_dim, CALL_LINEAR_V1_FEATURE_DIM)
        self.assertGreater(v1_model.feature_dim, v0_model.feature_dim)
        self.assertEqual(
            v1_model.feature_names[:CALL_LINEAR_FEATURE_DIM],
            v0_model.feature_names,
        )

    def test_prediction_is_masked_to_legal_call_kinds(self) -> None:
        model = CallLinearModel.fit(
            [
                _example(
                    discarded="1m",
                    legal_call_kinds=(ActionKind.PON,),
                    action=Action(ActionKind.PON, TileType.parse("1m")),
                )
                for _ in range(3)
            ],
            epochs=10,
            learning_rate=0.2,
        )

        prediction = model.predict(_example(discarded="3m", legal_call_kinds=(ActionKind.CHI,)))

        self.assertIn(prediction, {ActionKind.PASS, ActionKind.CHI})

    def test_positive_class_weight_is_recorded_and_validated(self) -> None:
        examples = [
            _example(
                discarded="1m",
                legal_call_kinds=(ActionKind.PON,),
                action=Action(ActionKind.PON, TileType.parse("1m")),
            )
        ]

        model = CallLinearModel.fit(
            examples,
            epochs=5,
            learning_rate=0.2,
            positive_class_weight=3.0,
        )

        self.assertEqual(model.positive_class_weight, 3.0)
        with self.assertRaisesRegex(ValueError, "positive_class_weight must be positive"):
            CallLinearModel.fit(
                examples,
                epochs=5,
                learning_rate=0.2,
                positive_class_weight=0.0,
            )

    def test_probabilities_are_masked_to_legal_call_kinds(self) -> None:
        model = CallLinearModel.fit(
            [
                _example(
                    discarded="1m",
                    legal_call_kinds=(ActionKind.PON,),
                    action=Action(ActionKind.PON, TileType.parse("1m")),
                )
                for _ in range(3)
            ],
            epochs=10,
            learning_rate=0.2,
        )

        probabilities = model.probabilities_for_example(
            _example(discarded="3m", legal_call_kinds=(ActionKind.CHI,))
        )
        logits = model.logits_for_example(
            _example(discarded="3m", legal_call_kinds=(ActionKind.CHI,))
        )

        self.assertEqual(set(probabilities), {ActionKind.PASS, ActionKind.CHI})
        self.assertEqual(set(logits), {ActionKind.PASS, ActionKind.CHI})
        self.assertAlmostEqual(sum(probabilities.values()), 1.0)

    def test_v1_prediction_is_masked_to_legal_call_kinds(self) -> None:
        model = CallLinearModel.fit(
            [
                _example(
                    discarded="1m",
                    legal_call_kinds=(ActionKind.PON,),
                    action=Action(ActionKind.PON, TileType.parse("1m")),
                )
                for _ in range(3)
            ],
            epochs=10,
            learning_rate=0.2,
            feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
        )

        prediction = model.predict(_example(discarded="3m", legal_call_kinds=(ActionKind.CHI,)))

        self.assertIn(prediction, {ActionKind.PASS, ActionKind.CHI})

    def test_chi_shape_variants_are_position_specific(self) -> None:
        self.assertEqual(
            _chi_shape(
                TileType.parse("2m"),
                ((TileType.parse("3m").index, 1), (TileType.parse("4m").index, 1)),
            ),
            "left",
        )
        self.assertEqual(
            _chi_shape(
                TileType.parse("2m"),
                ((TileType.parse("1m").index, 1), (TileType.parse("3m").index, 1)),
            ),
            "middle",
        )
        self.assertEqual(
            _chi_shape(
                TileType.parse("3m"),
                ((TileType.parse("1m").index, 1), (TileType.parse("2m").index, 1)),
            ),
            "right",
        )

    def test_shanten_memoization_is_bounded(self) -> None:
        _safe_shanten.cache_clear()
        with mock.patch("kenjaku.models.linear_call.shanten", return_value=0):
            for index in range(_SHANTEN_CACHE_MAXSIZE + 1):
                _safe_shanten((index,) + (0,) * 33)

        info = _safe_shanten.cache_info()
        self.assertEqual(info.maxsize, _SHANTEN_CACHE_MAXSIZE)
        self.assertEqual(info.currsize, _SHANTEN_CACHE_MAXSIZE)
        self.assertEqual(_ukeire_proxy.cache_info().maxsize, _UKEIRE_CACHE_MAXSIZE)
        _safe_shanten.cache_clear()
        _ukeire_proxy.cache_clear()


def _example(
    *,
    discarded: str,
    legal_call_kinds: tuple[ActionKind, ...],
    action: Action | None = None,
) -> CallExample:
    discarded_tile = Tile.parse(discarded)
    counts = [0] * 34
    counts[discarded_tile.type.index] = 2
    counts[TileType.parse("4p").index] = 1
    counts[TileType.parse("5p").index] = 1
    counts[TileType.parse("6p").index] = 1
    counts[TileType.parse("7s").index] = 1
    counts[TileType.parse("8s").index] = 1
    counts[TileType.parse("9s").index] = 1
    counts[TileType.parse("E").index] = 2
    counts[TileType.parse("S").index] = 2
    visible = [0] * 34
    visible[discarded_tile.type.index] = 1
    return CallExample(
        round_index=0,
        event_index=0,
        call_event_index=None,
        seat=1,
        from_seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        discarded_tile=discarded_tile,
        legal_call_kinds=legal_call_kinds,
        hand_counts=tuple(counts),
        visible_counts=tuple(visible),
        action=action or Action.pass_(),
    )


if __name__ == "__main__":
    unittest.main()
