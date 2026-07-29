from __future__ import annotations

from collections.abc import Iterable
from dataclasses import dataclass
from functools import cache

SUITED_OFFSETS = {"m": 0, "p": 9, "s": 18}
SUITED_RANGES = {
    "m": range(0, 9),
    "p": range(9, 18),
    "s": range(18, 27),
}
HONOR_TOKENS = ("E", "S", "W", "N", "P", "F", "C")
HONOR_TO_INDEX = {token: 27 + offset for offset, token in enumerate(HONOR_TOKENS)}
INDEX_TO_HONOR = {index: token for token, index in HONOR_TO_INDEX.items()}


@dataclass(frozen=True, order=True, slots=True)
class TileType:
    """One of the 34 logical riichi tile types."""

    index: int

    def __post_init__(self) -> None:
        if not 0 <= self.index < 34:
            raise ValueError(f"tile type index out of range: {self.index}")

    @classmethod
    def parse(cls, token: str) -> TileType:
        token = token.strip()
        if token in HONOR_TO_INDEX:
            return cls(HONOR_TO_INDEX[token])
        if len(token) != 2:
            raise ValueError(f"invalid tile type token: {token!r}")

        rank_text, suit = token
        if suit not in SUITED_OFFSETS:
            raise ValueError(f"invalid tile suit: {suit!r}")
        if rank_text == "0":
            rank = 5
        elif rank_text.isdigit():
            rank = int(rank_text)
        else:
            raise ValueError(f"invalid tile rank: {rank_text!r}")
        if not 1 <= rank <= 9:
            raise ValueError(f"tile rank out of range: {rank}")
        return cls(SUITED_OFFSETS[suit] + rank - 1)

    @property
    def suit(self) -> str:
        if self.index in SUITED_RANGES["m"]:
            return "m"
        if self.index in SUITED_RANGES["p"]:
            return "p"
        if self.index in SUITED_RANGES["s"]:
            return "s"
        return "z"

    @property
    def rank(self) -> int | None:
        if self.suit == "z":
            return None
        return self.index - SUITED_OFFSETS[self.suit] + 1

    @property
    def notation(self) -> str:
        if self.suit == "z":
            return INDEX_TO_HONOR[self.index]
        rank = self.rank
        assert rank is not None
        return f"{rank}{self.suit}"

    @property
    def is_terminal(self) -> bool:
        return self.rank in {1, 9}

    @property
    def is_honor(self) -> bool:
        return self.suit == "z"

    @property
    def is_terminal_or_honor(self) -> bool:
        return self.is_terminal or self.is_honor


@dataclass(frozen=True, order=True, slots=True)
class Tile:
    """A physical visible tile.

    Red fives are represented as physical tiles whose logical type is the
    matching five tile.
    """

    type: TileType
    red: bool = False

    def __post_init__(self) -> None:
        if self.red and self.type.rank != 5:
            raise ValueError("only suited five tiles can be red")

    @classmethod
    def parse(cls, token: str) -> Tile:
        token = token.strip()
        if len(token) == 3 and token.endswith("r"):
            tile_type = TileType.parse(token[:2])
            return cls(tile_type, red=True)
        if len(token) == 2 and token[0] == "0":
            tile_type = TileType.parse(token)
            return cls(tile_type, red=True)
        return cls(TileType.parse(token))

    @property
    def notation(self) -> str:
        if self.red:
            rank = self.type.rank
            assert rank == 5
            return f"0{self.type.suit}"
        return self.type.notation


@cache
def all_tile_types() -> tuple[TileType, ...]:
    return tuple(TileType(index) for index in range(34))


def tile_counts(tiles: Iterable[Tile | TileType]) -> tuple[int, ...]:
    counts = [0] * 34
    for tile in tiles:
        tile_type = tile.type if isinstance(tile, Tile) else tile
        counts[tile_type.index] += 1
    return tuple(counts)
