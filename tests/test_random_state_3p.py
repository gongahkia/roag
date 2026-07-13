from __future__ import annotations

import unittest

from kenjaku.core import ActionKind, Tile
from kenjaku.simulation import (
    SandboxEnvironmentState,
    generate_legal_random_state_3p,
    legal_sandbox_actions,
)


class LegalRandomStateSanmaTests(unittest.TestCase):
    def test_generation_is_seeded_and_reaches_legal_sanma_decisions(self) -> None:
        for seed, decisions in (("random-3p-a", 0), ("random-3p-b", 16), ("random-3p-c", 64)):
            with self.subTest(seed=seed, decisions=decisions):
                state = generate_legal_random_state_3p(seed=seed, decisions=decisions)
                same_state = generate_legal_random_state_3p(seed=seed, decisions=decisions)
                seat = (
                    state.pending_reaction_seats[0]
                    if state.pending_reaction_seats
                    else state.current_seat
                )
                actions = legal_sandbox_actions(state, seat=seat)

                self.assertEqual(state, same_state)
                self.assertEqual(state.ruleset, "tenhou-3p")
                self.assertEqual(state.players, 3)
                self.assertIsNone(state.terminal_reason)
                self.assertTrue(actions)
                self.assertFalse(any(action.kind is ActionKind.CHI for action in actions))
                self.assertFalse(
                    any(_is_excluded_manzu(tile.notation) for tile in _state_tiles(state))
                )

    def test_rejects_invalid_decision_counts(self) -> None:
        with self.assertRaisesRegex(ValueError, "non-negative integer"):
            generate_legal_random_state_3p(seed="bad", decisions=-1)


def _state_tiles(state: SandboxEnvironmentState) -> tuple[Tile, ...]:
    return (
        *state.wall,
        *state.dead_wall,
        *(tile for hand in state.hands for tile in hand),
        *(tile for discards in state.discards for tile in discards),
        *(tile for melds in state.melds for meld in melds for tile in meld.tiles),
        *(tile for kita_tiles in state.kita_tiles for tile in kita_tiles),
    )


def _is_excluded_manzu(notation: str) -> bool:
    return notation in {f"{rank}m" for rank in range(2, 9)}


if __name__ == "__main__":
    unittest.main()
