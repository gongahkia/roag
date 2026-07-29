from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import ActionKind
from kenjaku.training import KitaExample

KITA_DECISION_KINDS = (ActionKind.PASS, ActionKind.KITA)
KITA_FREQUENCY_MODEL_KIND = "kita-frequency-v0"


@dataclass(frozen=True, slots=True)
class KitaFrequencyBaseline:
    """Predict the most frequent Sanma kita/pass decision."""

    counts: tuple[int, ...]

    def __post_init__(self) -> None:
        if len(self.counts) != len(KITA_DECISION_KINDS):
            raise ValueError("kita frequency counts have unsupported length")

    @classmethod
    def fit(cls, examples: list[KitaExample]) -> KitaFrequencyBaseline:
        counts = [0] * len(KITA_DECISION_KINDS)
        for example in examples:
            counts[_kind_index(example.action.kind)] += 1
        return cls(tuple(counts))

    @property
    def kind(self) -> str:
        return KITA_FREQUENCY_MODEL_KIND

    def count_by_kind(self) -> dict[str, int]:
        return {kind.value: self.counts[index] for index, kind in enumerate(KITA_DECISION_KINDS)}

    def predict(self, example: KitaExample) -> ActionKind:
        return max(
            KITA_DECISION_KINDS,
            key=lambda kind: (self.counts[_kind_index(kind)], -_kind_index(kind)),
        )

    def score(self, examples: list[KitaExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = sum(self.predict(example) == example.action.kind for example in examples)
        return correct / len(examples)


def _kind_index(kind: ActionKind) -> int:
    if kind not in KITA_DECISION_KINDS:
        raise ValueError(f"unsupported kita decision kind: {kind.value}")
    return KITA_DECISION_KINDS.index(kind)
