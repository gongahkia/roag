from __future__ import annotations

import unittest

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.training import (
    DiscardExample,
    active_riichi_opponents,
    candidate_has_kabe,
    candidate_has_one_chance,
    candidate_has_sotogawa,
    candidate_has_suji,
    candidate_is_genbutsu,
    candidate_seen_after_riichi,
    candidate_seen_before_riichi,
    has_active_riichi_opponent,
    max_active_riichi_discards_elapsed,
    min_active_riichi_discards_elapsed,
)


class DefenseFeatureTests(unittest.TestCase):
    def test_tracks_active_riichi_opponents_and_genbutsu(self) -> None:
        example = _example(opponent_river=["1m", "4m", "9p"], riichi_turn=1)

        self.assertEqual(active_riichi_opponents(example), (1,))
        self.assertTrue(has_active_riichi_opponent(example))
        self.assertTrue(candidate_is_genbutsu(example, TileType.parse("1m")))
        self.assertFalse(candidate_is_genbutsu(example, TileType.parse("2m")))

    def test_suji_uses_active_riichi_opponent_rivers(self) -> None:
        example = _example(opponent_river=["4m"], riichi_turn=0)

        self.assertTrue(candidate_has_suji(example, TileType.parse("7m")))
        self.assertTrue(candidate_has_suji(example, TileType.parse("1m")))
        self.assertFalse(candidate_has_suji(example, TileType.parse("8m")))
        self.assertFalse(candidate_has_suji(example, TileType.parse("E")))

    def test_kabe_and_one_chance_use_visible_counts(self) -> None:
        kabe = _example(visible_tiles=["4p", "4p", "4p", "4p"])
        one_chance = _example(visible_tiles=["4s", "4s", "4s"])

        self.assertTrue(candidate_has_kabe(kabe, TileType.parse("5p")))
        self.assertFalse(candidate_has_one_chance(kabe, TileType.parse("5p")))
        self.assertFalse(candidate_has_kabe(one_chance, TileType.parse("5s")))
        self.assertTrue(candidate_has_one_chance(one_chance, TileType.parse("5s")))

    def test_riichi_chronology_splits_before_and_after(self) -> None:
        example = _example(opponent_river=["1m", "4m", "9p"], riichi_turn=1)

        self.assertTrue(candidate_seen_before_riichi(example, TileType.parse("1m")))
        self.assertFalse(candidate_seen_after_riichi(example, TileType.parse("1m")))
        self.assertFalse(candidate_seen_before_riichi(example, TileType.parse("4m")))
        self.assertTrue(candidate_seen_after_riichi(example, TileType.parse("4m")))
        self.assertEqual(min_active_riichi_discards_elapsed(example), 2)
        self.assertEqual(max_active_riichi_discards_elapsed(example), 2)

    def test_sotogawa_uses_before_riichi_outer_tiles(self) -> None:
        example = _example(opponent_river=["5m", "6p"], riichi_turn=2)

        self.assertTrue(candidate_has_sotogawa(example, TileType.parse("1m")))
        self.assertTrue(candidate_has_sotogawa(example, TileType.parse("9p")))
        self.assertFalse(candidate_has_sotogawa(example, TileType.parse("4m")))
        self.assertFalse(candidate_has_sotogawa(example, TileType.parse("E")))


def _example(
    *,
    opponent_river: list[str] | None = None,
    visible_tiles: list[str] | None = None,
    riichi_turn: int | None = 0,
) -> DiscardExample:
    hand = tuple(Tile.parse(tile) for tile in ["1m", "2m"])
    opponent_river_tiles = tuple(Tile.parse(tile) for tile in opponent_river or [])
    visible = tuple(Tile.parse(tile) for tile in visible_tiles or [])
    rivers_by_seat = (
        (),
        opponent_river_tiles,
        (),
        (),
    )
    river_counts_by_seat = tuple(tile_counts(river) for river in rivers_by_seat)
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(hand),
        visible_counts=tile_counts((*hand, *opponent_river_tiles, *visible)),
        action=Action.discard("1m"),
        active_riichi_seats=(False, True, False, False),
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=(None, riichi_turn, None, None),
        riichi_declared_event_indices=(None, 3, None, None),
    )


if __name__ == "__main__":
    unittest.main()
