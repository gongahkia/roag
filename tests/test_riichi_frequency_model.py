from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind
from kenjaku.models import RiichiFrequencyBaseline
from kenjaku.training import RiichiExample


class RiichiFrequencyModelTests(unittest.TestCase):
    def test_frequency_baseline_can_choose_pass_or_riichi(self) -> None:
        pass_heavy = RiichiFrequencyBaseline.fit(
            [
                _example(Action.pass_()),
                _example(Action.pass_()),
                _example(Action(ActionKind.RIICHI)),
            ]
        )
        riichi_heavy = RiichiFrequencyBaseline.fit(
            [_example(Action(ActionKind.RIICHI)), _example(Action(ActionKind.RIICHI))]
        )

        self.assertEqual(pass_heavy.kind, "riichi-frequency-v0")
        self.assertEqual(pass_heavy.predict(_example(Action(ActionKind.RIICHI))), ActionKind.PASS)
        self.assertEqual(
            riichi_heavy.predict(_example(Action.pass_())),
            ActionKind.RIICHI,
        )


def _example(action: Action) -> RiichiExample:
    return RiichiExample(
        round_index=0,
        event_index=0,
        riichi_event_index=None if action.kind == ActionKind.PASS else 0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=(0,) * 34,
        visible_counts=(0,) * 34,
        active_riichi_seats=(False, False, False, False),
        river_counts_by_seat=tuple((0,) * 34 for _ in range(4)),
        seat_turn_index=0,
        action=action,
    )


if __name__ == "__main__":
    unittest.main()
