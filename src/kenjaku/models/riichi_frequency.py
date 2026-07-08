from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import ActionKind
from kenjaku.features.riichi import RIICHI_DECISION_KINDS
from kenjaku.training import RiichiExample

RIICHI_FREQUENCY_MODEL_KIND = "riichi-frequency-v0"


@dataclass(frozen=True, slots=True)
class RiichiFrequencyBaseline:
    """Predict the most frequent riichi/pass decision."""

    counts: tuple[int, ...]

    def __post_init__(self) -> None:
        if len(self.counts) != len(RIICHI_DECISION_KINDS):
            raise ValueError("riichi frequency counts have unsupported length")

    @classmethod
    def fit(cls, examples: list[RiichiExample]) -> RiichiFrequencyBaseline:
        counts = [0] * len(RIICHI_DECISION_KINDS)
        for example in examples:
            counts[_kind_index(example.action.kind)] += 1
        return cls(tuple(counts))

    @property
    def kind(self) -> str:
        return RIICHI_FREQUENCY_MODEL_KIND

    def count_by_kind(self) -> dict[str, int]:
        return {kind.value: self.counts[index] for index, kind in enumerate(RIICHI_DECISION_KINDS)}

    def predict(self, example: RiichiExample) -> ActionKind:
        return max(
            RIICHI_DECISION_KINDS,
            key=lambda kind: (self.counts[_kind_index(kind)], -_kind_index(kind)),
        )

    def score(self, examples: list[RiichiExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = sum(self.predict(example) == example.action.kind for example in examples)
        return correct / len(examples)


def _kind_index(kind: ActionKind) -> int:
    if kind not in RIICHI_DECISION_KINDS:
        raise ValueError(f"unsupported riichi decision kind: {kind.value}")
    return RIICHI_DECISION_KINDS.index(kind)
