from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import ActionKind
from kenjaku.training import CallExample

CALL_FREQUENCY_MODEL_KIND = "call-frequency-v0"
CALL_LEGAL_FREQUENCY_MODEL_KIND = "call-legal-frequency-v0"
CALL_DECISION_KINDS = (
    ActionKind.PASS,
    ActionKind.CHI,
    ActionKind.PON,
    ActionKind.MINKAN,
)


@dataclass(frozen=True, slots=True)
class CallFrequencyBaseline:
    """Predict the most frequent legal call/pass action kind."""

    counts: tuple[int, ...]

    def __post_init__(self) -> None:
        if len(self.counts) != len(CALL_DECISION_KINDS):
            raise ValueError("call frequency counts have unsupported length")

    @classmethod
    def fit(cls, examples: list[CallExample]) -> CallFrequencyBaseline:
        counts = [0] * len(CALL_DECISION_KINDS)
        for example in examples:
            counts[_kind_index(example.action.kind)] += 1
        return cls(tuple(counts))

    @property
    def kind(self) -> str:
        return CALL_FREQUENCY_MODEL_KIND

    def count_by_kind(self) -> dict[str, int]:
        return {kind.value: self.counts[index] for index, kind in enumerate(CALL_DECISION_KINDS)}

    def predict(self, example: CallExample) -> ActionKind:
        candidates = (ActionKind.PASS, *example.legal_call_kinds)
        return max(
            candidates,
            key=lambda kind: (self.counts[_kind_index(kind)], -_kind_index(kind)),
        )

    def score(self, examples: list[CallExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = sum(self.predict(example) == example.action.kind for example in examples)
        return correct / len(examples)


@dataclass(frozen=True, slots=True)
class CallLegalFrequencyBaseline:
    """Predict the most frequent legal non-pass call kind."""

    counts: tuple[int, ...]

    def __post_init__(self) -> None:
        if len(self.counts) != len(CALL_DECISION_KINDS):
            raise ValueError("call legal-frequency counts have unsupported length")

    @classmethod
    def fit(cls, examples: list[CallExample]) -> CallLegalFrequencyBaseline:
        counts = [0] * len(CALL_DECISION_KINDS)
        for example in examples:
            if example.action.kind != ActionKind.PASS:
                counts[_kind_index(example.action.kind)] += 1
        return cls(tuple(counts))

    @property
    def kind(self) -> str:
        return CALL_LEGAL_FREQUENCY_MODEL_KIND

    def count_by_kind(self) -> dict[str, int]:
        return {kind.value: self.counts[index] for index, kind in enumerate(CALL_DECISION_KINDS)}

    def predict(self, example: CallExample) -> ActionKind:
        if not example.legal_call_kinds:
            return ActionKind.PASS
        return max(
            example.legal_call_kinds,
            key=lambda kind: (self.counts[_kind_index(kind)], -_kind_index(kind)),
        )

    def score(self, examples: list[CallExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = sum(self.predict(example) == example.action.kind for example in examples)
        return correct / len(examples)


def _kind_index(kind: ActionKind) -> int:
    if kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call decision kind: {kind.value}")
    return CALL_DECISION_KINDS.index(kind)
