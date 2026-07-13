from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.evaluator import (
    evaluate_defense_risk,
    evaluate_hand_value_potential,
    evaluate_legal_defense_risks,
    evaluate_shanten_ukeire,
)
from kenjaku.training import DiscardExample


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

    def test_evaluates_visible_bonus_and_structural_value_potential(self) -> None:
        result = evaluate_hand_value_potential(
            _tiles("2p 3p 4p 5p 0p E E E 2p 3p 4p 6p"),
            dora_indicators=(Tile.parse("4p"),),
            seat_wind=TileType.parse("E"),
        )

        self.assertEqual(result.visible_dora, 2)
        self.assertEqual(result.red_dora, 1)
        self.assertEqual(result.yakuhai_triplets, (TileType.parse("E"),))
        self.assertEqual(result.flush_candidate, "honitsu")
        self.assertEqual(result.potential_yaku, ("yakuhai", "honitsu"))

    def test_uses_sanma_one_nine_dora_wrap_and_rejects_excluded_tiles(self) -> None:
        result = evaluate_hand_value_potential(
            _tiles("1m 9m 9m 1p 2p 3p 1s 2s 3s E E E"),
            ruleset="tenhou-3p",
            dora_indicators=(Tile.parse("1m"),),
        )

        self.assertEqual(result.visible_dora, 2)
        with self.assertRaisesRegex(ValueError, "unavailable"):
            evaluate_hand_value_potential(_tiles("5m"), ruleset="tenhou-3p")

    def test_exposes_defense_risk_bands_and_ranked_factors(self) -> None:
        example = _defense_example(opponent_river=("4m",), hand=("4m", "5m", "7m"))

        genbutsu = evaluate_defense_risk(example, TileType.parse("4m"))
        candidates = evaluate_legal_defense_risks(example)

        self.assertLess(genbutsu.risk, candidates[0].risk)
        self.assertEqual(genbutsu.risk_band, "low")
        self.assertIn("genbutsu", genbutsu.safety_factors)
        self.assertFalse(genbutsu.calibrated_probability)
        self.assertEqual([candidate.tile.notation for candidate in candidates], ["5m", "7m", "4m"])


def _counts(text: str) -> tuple[int, ...]:
    counts = [0] * 34
    for token in text.split():
        counts[TileType.parse(token).index] += 1
    return tuple(counts)


def _tiles(text: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in text.split())


def _defense_example(*, opponent_river: tuple[str, ...], hand: tuple[str, ...]) -> DiscardExample:
    hand_tiles = _tiles(" ".join(hand))
    opponent_tiles = _tiles(" ".join(opponent_river))
    rivers = ((), opponent_tiles, (), ())
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(hand_tiles),
        visible_counts=tile_counts((*hand_tiles, *opponent_tiles)),
        action=Action.discard(hand_tiles[0].type),
        active_riichi_seats=(False, True, False, False),
        river_counts_by_seat=tuple(tile_counts(river) for river in rivers),
        rivers_by_seat=rivers,
        riichi_declared_turns=(None, 0, None, None),
        riichi_declared_event_indices=(None, 1, None, None),
    )


if __name__ == "__main__":
    unittest.main()
