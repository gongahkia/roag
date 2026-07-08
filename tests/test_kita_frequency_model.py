from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, TileType
from kenjaku.models import KitaFrequencyBaseline
from kenjaku.training import KitaExample


class KitaFrequencyModelTests(unittest.TestCase):
    def test_frequency_baseline_can_choose_pass_or_kita(self) -> None:
        pass_heavy = KitaFrequencyBaseline.fit(
            [
                _example(Action.pass_()),
                _example(Action.pass_()),
                _example(_kita_action()),
            ]
        )
        kita_heavy = KitaFrequencyBaseline.fit(
            [_example(_kita_action()), _example(_kita_action())]
        )

        self.assertEqual(pass_heavy.kind, "kita-frequency-v0")
        self.assertEqual(pass_heavy.predict(_example(_kita_action())), ActionKind.PASS)
        self.assertEqual(kita_heavy.predict(_example(Action.pass_())), ActionKind.KITA)
        self.assertEqual(kita_heavy.count_by_kind(), {"pass": 0, "kita": 2})


def _example(action: Action) -> KitaExample:
    return KitaExample(
        round_index=0,
        event_index=0,
        kita_event_index=None if action.kind == ActionKind.PASS else 0,
        draw_event_index=0,
        seat=0,
        dealer=0,
        scores=(35000, 35000, 35000),
        hand_counts=(0,) * 34,
        visible_counts=(0,) * 34,
        active_riichi_seats=(False, False, False),
        river_counts_by_seat=tuple((0,) * 34 for _ in range(3)),
        seat_turn_index=0,
        action=action,
    )


def _kita_action() -> Action:
    return Action(ActionKind.KITA, TileType.parse("N"))


if __name__ == "__main__":
    unittest.main()
