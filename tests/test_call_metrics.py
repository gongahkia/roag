from __future__ import annotations

import unittest

from kenjaku.cli import _call_metrics, _summarize_call_predictions
from kenjaku.core import Action, ActionKind, Tile
from kenjaku.training import CallExample


class CallMetricsTests(unittest.TestCase):
    def test_empty_examples_have_empty_metrics(self) -> None:
        analysis = _summarize_call_predictions([], lambda example: ActionKind.PASS)
        metrics = _call_metrics(analysis)

        self.assertIsNone(metrics["accuracy"])
        self.assertIsNone(metrics["balanced_accuracy"])
        self.assertIsNone(metrics["macro_recall"])
        self.assertIsNone(metrics["pass_recall"])
        self.assertIsNone(metrics["call_recall"])

    def test_one_sided_pass_bucket_does_not_force_call_recall(self) -> None:
        examples = [_example(Action.pass_())]
        analysis = _summarize_call_predictions(examples, lambda example: ActionKind.PASS)
        metrics = _call_metrics(analysis)

        self.assertEqual(metrics["accuracy"], 1.0)
        self.assertEqual(metrics["balanced_accuracy"], 1.0)
        self.assertEqual(metrics["macro_recall"], 1.0)
        self.assertEqual(metrics["pass_recall"], 1.0)
        self.assertIsNone(metrics["call_recall"])


def _example(action: Action) -> CallExample:
    return CallExample(
        round_index=0,
        event_index=0,
        call_event_index=None,
        seat=1,
        from_seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        discarded_tile=Tile.parse("1m"),
        legal_call_kinds=(ActionKind.PON,),
        hand_counts=(0,) * 34,
        visible_counts=(0,) * 34,
        action=action,
    )


if __name__ == "__main__":
    unittest.main()
