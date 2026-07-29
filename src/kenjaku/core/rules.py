from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core.tiles import TileType, all_tile_types


@dataclass(frozen=True, slots=True)
class RuleSet:
    """Static tile-set facts needed before scoring/simulation exists."""

    name: str
    players: int
    excluded_tile_types: frozenset[TileType] = frozenset()
    red_five_suits: frozenset[str] = frozenset({"m", "p", "s"})

    def __post_init__(self) -> None:
        if self.players not in {3, 4}:
            raise ValueError("riichi rulesets must have 3 or 4 players")
        invalid_red_suits = self.red_five_suits - {"m", "p", "s"}
        if invalid_red_suits:
            raise ValueError(f"invalid red five suits: {sorted(invalid_red_suits)}")

    @property
    def tile_types(self) -> tuple[TileType, ...]:
        return tuple(tile for tile in all_tile_types() if tile not in self.excluded_tile_types)

    @property
    def type_counts(self) -> tuple[int, ...]:
        counts = [4] * 34
        for tile_type in self.excluded_tile_types:
            counts[tile_type.index] = 0
        return tuple(counts)


TENHOU_4P = RuleSet(name="tenhou-4p", players=4)

TENHOU_3P = RuleSet(
    name="tenhou-3p",
    players=3,
    excluded_tile_types=frozenset(TileType.parse(f"{rank}m") for rank in range(2, 9)),
    red_five_suits=frozenset({"p", "s"}),
)
