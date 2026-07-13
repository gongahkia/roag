from __future__ import annotations

import unittest

from kenjaku.core import Tile
from kenjaku.heuristics import rank_discard_heuristic


class DiscardHeuristicTests(unittest.TestCase):
    def test_ranks_discard_types_with_structured_efficiency_factors(self) -> None:
        candidates = rank_discard_heuristic(
            _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 9p")
        )

        best = candidates[0]

        self.assertEqual(best.tile.notation, "5m")
        self.assertEqual(tuple(factor.name for factor in best.factors), (
            "shanten",
            "ukeire",
            "bonus_han",
            "structural_yaku",
        ))
        factor_values = {factor.name: factor.value for factor in best.factors}
        self.assertEqual(factor_values["shanten"], 0.0)
        self.assertEqual(factor_values["ukeire"], 3.0)
        self.assertEqual(
            candidates,
            tuple(
                sorted(
                    candidates,
                    key=lambda item: (item.factors[0].value, -item.score, item.tile.index),
                )
            ),
        )

    def test_rejects_non_discard_hand_sizes_and_unavailable_sanma_tiles(self) -> None:
        with self.assertRaisesRegex(ValueError, "exactly fourteen"):
            rank_discard_heuristic(_tiles("1m"))
        with self.assertRaisesRegex(ValueError, "unavailable"):
            rank_discard_heuristic(
                _tiles("5m 1p 1p 1p 1p 2p 2p 2p 2p 3p 3p 3p 3p 4p"),
                ruleset="tenhou-3p",
            )


def _tiles(text: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in text.split())


if __name__ == "__main__":
    unittest.main()
