from __future__ import annotations

import unittest

from kenjaku.core import TileType
from kenjaku.evaluator import evaluate_shanten_ukeire


class ShantenUkeireEvaluatorTests(unittest.TestCase):
    def test_counts_exact_remaining_improving_copies(self) -> None:
        hand = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m")

        result = evaluate_shanten_ukeire(hand)

        self.assertEqual(result.ruleset, "tenhou-4p")
        self.assertEqual(result.shanten, 0)
        self.assertEqual(result.improving_tiles, (TileType.parse("5m"),))
        self.assertEqual(result.ukeire, 3)

    def test_visible_tiles_reduce_ukeire_and_sanma_excludes_middle_manzu(self) -> None:
        hand = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m")
        visible = list(hand)
        visible[TileType.parse("5m").index] = 4

        exhausted = evaluate_shanten_ukeire(hand, visible_counts=visible)

        self.assertEqual(exhausted.improving_tiles, ())
        self.assertEqual(exhausted.ukeire, 0)
        with self.assertRaisesRegex(ValueError, "unavailable"):
            evaluate_shanten_ukeire(hand, ruleset="tenhou-3p")
        sanma = evaluate_shanten_ukeire(
            _counts("1m 1m 9m 9m 1p 2p 3p 4p 5p 6p 1s 2s 3s"),
            ruleset="tenhou-3p",
        )
        self.assertEqual(sanma.ruleset, "tenhou-3p")
        excluded_manzu = {f"{rank}m" for rank in range(2, 9)}
        self.assertTrue(
            all(tile.notation not in excluded_manzu for tile in sanma.improving_tiles)
        )

    def test_rejects_non_draw_ready_and_inconsistent_counts(self) -> None:
        hand = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m")
        fourteen_tiles = list(hand)
        fourteen_tiles[TileType.parse("5m").index] += 1
        with self.assertRaisesRegex(ValueError, "at most thirteen"):
            evaluate_shanten_ukeire(fourteen_tiles)
        visible = list(hand)
        visible[0] = 0
        with self.assertRaisesRegex(ValueError, "include the hand"):
            evaluate_shanten_ukeire(hand, visible_counts=visible)


def _counts(text: str) -> tuple[int, ...]:
    counts = [0] * 34
    for token in text.split():
        counts[TileType.parse(token).index] += 1
    return tuple(counts)


if __name__ == "__main__":
    unittest.main()
