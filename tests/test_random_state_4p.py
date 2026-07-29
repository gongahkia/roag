from __future__ import annotations

import unittest

from kenjaku.core import ActionKind
from kenjaku.simulation import (
    generate_legal_random_state_4p,
    legal_sandbox_actions,
)


class LegalRandomStateFourPlayerTests(unittest.TestCase):
    def test_generation_is_seeded_and_reaches_legal_four_player_decisions(self) -> None:
        for seed, decisions in (("random-4p-a", 0), ("random-4p-b", 16), ("random-4p-c", 64)):
            with self.subTest(seed=seed, decisions=decisions):
                state = generate_legal_random_state_4p(seed=seed, decisions=decisions)
                same_state = generate_legal_random_state_4p(seed=seed, decisions=decisions)
                seat = (
                    state.pending_reaction_seats[0]
                    if state.pending_reaction_seats
                    else state.current_seat
                )
                actions = legal_sandbox_actions(state, seat=seat)

                self.assertEqual(state, same_state)
                self.assertEqual(state.ruleset, "tenhou-4p")
                self.assertEqual(state.players, 4)
                self.assertIsNone(state.terminal_reason)
                self.assertTrue(actions)
                self.assertFalse(any(action.kind is ActionKind.KITA for action in actions))

    def test_rejects_invalid_decision_counts(self) -> None:
        with self.assertRaisesRegex(ValueError, "non-negative integer"):
            generate_legal_random_state_4p(seed="bad", decisions=-1)
        with self.assertRaisesRegex(ValueError, "non-negative integer"):
            generate_legal_random_state_4p(seed="bad", decisions=True)


if __name__ == "__main__":
    unittest.main()
