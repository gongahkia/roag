from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import TileType
from kenjaku.training import DiscardExample


@dataclass(frozen=True, slots=True)
class DiscardFrequencyBaseline:
    """Predict the globally most frequent observed discard that is legal."""

    counts: tuple[int, ...]

    def __post_init__(self) -> None:
        if len(self.counts) != 34:
            raise ValueError("discard frequency counts must have length 34")

    @classmethod
    def fit(cls, examples: list[DiscardExample]) -> DiscardFrequencyBaseline:
        counts = [0] * 34
        for example in examples:
            if example.action.tile is None:
                raise ValueError("discard examples must have tile actions")
            counts[example.action.tile.index] += 1
        return cls(tuple(counts))

    @property
    def top_tile(self) -> TileType:
        return TileType(max(range(34), key=lambda index: (self.counts[index], -index)))

    def predict(self, hand_counts: tuple[int, ...]) -> TileType:
        if len(hand_counts) != 34:
            raise ValueError("hand counts must have length 34")
        legal_indices = [index for index, count in enumerate(hand_counts) if count > 0]
        if not legal_indices:
            raise ValueError("cannot predict a discard from an empty hand")
        return TileType(max(legal_indices, key=lambda index: (self.counts[index], -index)))

    def score(self, examples: list[DiscardExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = 0
        for example in examples:
            if example.action.tile is None:
                raise ValueError("discard examples must have tile actions")
            correct += self.predict(example.hand_counts) == example.action.tile
        return correct / len(examples)
