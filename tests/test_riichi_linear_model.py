from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind
from kenjaku.models import RIICHI_LINEAR_FEATURE_DIM, RiichiLinearModel
from kenjaku.training import RiichiExample


class RiichiLinearModelTests(unittest.TestCase):
    def test_fit_predicts_selective_pass_and_riichi(self) -> None:
        examples = [
            *[_example(Action.pass_(), seat_turn_index=1) for _ in range(8)],
            *[_example(Action(ActionKind.RIICHI), seat_turn_index=12) for _ in range(8)],
        ]

        model = RiichiLinearModel.fit(examples, epochs=60, learning_rate=0.2)

        self.assertEqual(model.kind, "riichi-linear-v0")
        self.assertEqual(model.feature_dim, RIICHI_LINEAR_FEATURE_DIM)
        self.assertEqual(
            model.predict(_example(Action.pass_(), seat_turn_index=1)),
            ActionKind.PASS,
        )
        self.assertEqual(
            model.predict(_example(Action(ActionKind.RIICHI), seat_turn_index=12)),
            ActionKind.RIICHI,
        )

    def test_probabilities_cover_pass_and_riichi(self) -> None:
        model = RiichiLinearModel.fit(
            [_example(Action.pass_(), seat_turn_index=1), _example(Action(ActionKind.RIICHI), seat_turn_index=12)],
            epochs=5,
            learning_rate=0.2,
        )

        example = _example(Action.pass_(), seat_turn_index=1)
        probabilities = model.probabilities_for_example(example)
        logits = model.logits_for_example(example)

        self.assertEqual(set(probabilities), {ActionKind.PASS, ActionKind.RIICHI})
        self.assertEqual(set(logits), {ActionKind.PASS, ActionKind.RIICHI})
        self.assertAlmostEqual(sum(probabilities.values()), 1.0)

    def test_positive_class_weight_is_recorded_and_validated(self) -> None:
        examples = [
            _example(Action.pass_(), seat_turn_index=1),
            _example(Action(ActionKind.RIICHI), seat_turn_index=12),
        ]

        model = RiichiLinearModel.fit(
            examples,
            epochs=5,
            learning_rate=0.2,
            positive_class_weight=3.0,
        )

        self.assertEqual(model.positive_class_weight, 3.0)
        with self.assertRaisesRegex(ValueError, "positive_class_weight must be positive"):
            RiichiLinearModel.fit(
                examples,
                epochs=5,
                learning_rate=0.2,
                positive_class_weight=0.0,
            )


def _example(action: Action, *, seat_turn_index: int) -> RiichiExample:
    counts = [0] * 34
    counts[0] = 2
    counts[1] = 2
    counts[2] = 2
    counts[9] = 2
    counts[10] = 2
    counts[18] = 2
    counts[27] = 1
    visible = [0] * 34
    visible[0] = 1
    visible[9] = 1
    return RiichiExample(
        round_index=0,
        event_index=0,
        riichi_event_index=None if action.kind == ActionKind.PASS else 0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tuple(counts),
        visible_counts=tuple(visible),
        active_riichi_seats=(False, False, False, False),
        river_counts_by_seat=tuple((0,) * 34 for _ in range(4)),
        seat_turn_index=seat_turn_index,
        action=action,
    )


if __name__ == "__main__":
    unittest.main()
