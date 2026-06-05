from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.models import CallFrequencyBaseline, CallLegalFrequencyBaseline
from kenjaku.training import CallExample


class CallFrequencyModelTests(unittest.TestCase):
    def test_frequency_baseline_can_choose_pass(self) -> None:
        examples = [
            _example((ActionKind.PON,), Action.pass_()),
            _example((ActionKind.PON,), Action.pass_()),
            _example((ActionKind.PON,), Action(ActionKind.PON, TileType.parse("1m"))),
        ]
        model = CallFrequencyBaseline.fit(examples)

        self.assertEqual(model.kind, "call-frequency-v0")
        self.assertEqual(model.predict(_example((ActionKind.PON,), Action.pass_())), ActionKind.PASS)

    def test_legal_frequency_baseline_prefers_legal_non_pass_call(self) -> None:
        examples = [
            _example((ActionKind.PON,), Action(ActionKind.PON, TileType.parse("1m"))),
            _example((ActionKind.PON,), Action(ActionKind.PON, TileType.parse("1m"))),
            _example((ActionKind.CHI,), Action(ActionKind.CHI, TileType.parse("1m"))),
            _example((ActionKind.CHI,), Action.pass_()),
        ]
        model = CallLegalFrequencyBaseline.fit(examples)

        self.assertEqual(model.kind, "call-legal-frequency-v0")
        self.assertEqual(
            model.predict(_example((ActionKind.CHI, ActionKind.PON), Action.pass_())),
            ActionKind.PON,
        )
        self.assertEqual(
            model.predict(_example((ActionKind.CHI,), Action.pass_())),
            ActionKind.CHI,
        )

    def test_legal_frequency_baseline_passes_without_legal_calls(self) -> None:
        model = CallLegalFrequencyBaseline.fit(
            [_example((ActionKind.PON,), Action(ActionKind.PON, TileType.parse("1m")))]
        )

        self.assertEqual(model.predict(_example((), Action.pass_())), ActionKind.PASS)


def _example(
    legal_call_kinds: tuple[ActionKind, ...],
    action: Action,
) -> CallExample:
    return CallExample(
        round_index=0,
        event_index=0,
        call_event_index=None if action.kind == ActionKind.PASS else 1,
        seat=1,
        from_seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        discarded_tile=Tile.parse("1m"),
        legal_call_kinds=legal_call_kinds,
        hand_counts=(0,) * 34,
        visible_counts=(0,) * 34,
        action=action,
    )


if __name__ == "__main__":
    unittest.main()
