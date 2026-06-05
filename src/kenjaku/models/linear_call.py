from __future__ import annotations

from dataclasses import dataclass
from math import exp

from kenjaku.core import ActionKind, TileType, all_tile_types, shanten
from kenjaku.models.call_frequency import CALL_DECISION_KINDS
from kenjaku.training import CallExample

CALL_LINEAR_MODEL_KIND = "call-linear-v0"

_NON_PASS_CALL_KINDS = (ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN)
_TILE_FEATURE_NAMES = tuple(tile_type.notation for tile_type in all_tile_types())
CALL_LINEAR_FEATURE_NAMES = (
    "bias",
    *(f"candidate_{kind.value}" for kind in CALL_DECISION_KINDS),
    *(f"discarded_{name}" for name in _TILE_FEATURE_NAMES),
    *(f"legal_{kind.value}" for kind in _NON_PASS_CALL_KINDS),
    "legal_call_kind_count",
    *(f"hand_count_{name}" for name in _TILE_FEATURE_NAMES),
    *(f"visible_count_{name}" for name in _TILE_FEATURE_NAMES),
    "discarded_tile_hand_count",
    "from_left",
    "from_across",
    "from_right",
    "seat_is_dealer",
    "from_seat_is_dealer",
    "before_shanten",
    "after_shanten_proxy",
    "shanten_delta_proxy",
)
CALL_LINEAR_FEATURE_DIM = len(CALL_LINEAR_FEATURE_NAMES)


@dataclass(frozen=True, slots=True)
class _PreparedCallExample:
    target: ActionKind
    features_by_kind: dict[ActionKind, tuple[float, ...]]


@dataclass(frozen=True, slots=True)
class CallLinearModel:
    """Tiny dependency-free masked softmax model for call/pass decisions."""

    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float
    l2: float = 0.0

    def __post_init__(self) -> None:
        if len(self.weights) != len(CALL_DECISION_KINDS):
            raise ValueError("call linear model has unsupported output rows")
        if any(len(row) != CALL_LINEAR_FEATURE_DIM for row in self.weights):
            raise ValueError(f"call linear model rows must have {CALL_LINEAR_FEATURE_DIM} features")
        if self.epochs <= 0:
            raise ValueError("epochs must be positive")
        if self.learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if self.l2 < 0:
            raise ValueError("l2 must be non-negative")

    @classmethod
    def fit(
        cls,
        examples: list[CallExample],
        *,
        epochs: int = 25,
        learning_rate: float = 0.1,
        l2: float = 0.0,
    ) -> CallLinearModel:
        if not examples:
            raise ValueError("cannot train on zero examples")
        if epochs <= 0:
            raise ValueError("epochs must be positive")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")

        weights = [[0.0] * CALL_LINEAR_FEATURE_DIM for _ in CALL_DECISION_KINDS]
        prepared_examples = [_prepare_example(example) for example in examples]
        for _ in range(epochs):
            for example in prepared_examples:
                _apply_update(
                    weights,
                    example,
                    learning_rate=learning_rate,
                    l2=l2,
                )

        return cls(
            weights=tuple(tuple(row) for row in weights),
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
        )

    @property
    def kind(self) -> str:
        return CALL_LINEAR_MODEL_KIND

    @property
    def feature_dim(self) -> int:
        return CALL_LINEAR_FEATURE_DIM

    def predict(self, example: CallExample) -> ActionKind:
        prepared = _prepare_example(example)
        logits = {
            kind: _dot(self.weights[_kind_index(kind)], features)
            for kind, features in prepared.features_by_kind.items()
        }
        return max(logits, key=lambda kind: (logits[kind], -_kind_index(kind)))

    def score(self, examples: list[CallExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = sum(self.predict(example) == example.action.kind for example in examples)
        return correct / len(examples)


def _prepare_example(example: CallExample) -> _PreparedCallExample:
    if example.action.kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call action kind: {example.action.kind.value}")
    candidates = _candidate_kinds(example)
    if example.action.kind not in candidates:
        candidates = (*candidates, example.action.kind)
    return _PreparedCallExample(
        target=example.action.kind,
        features_by_kind={
            kind: _features_for_candidate(example, kind)
            for kind in candidates
        },
    )


def _candidate_kinds(example: CallExample) -> tuple[ActionKind, ...]:
    candidates: list[ActionKind] = [ActionKind.PASS]
    for kind in CALL_DECISION_KINDS:
        if kind != ActionKind.PASS and kind in example.legal_call_kinds:
            candidates.append(kind)
    return tuple(candidates)


def _features_for_candidate(example: CallExample, kind: ActionKind) -> tuple[float, ...]:
    discarded_index = example.discarded_tile.type.index
    before_shanten = _safe_shanten(example.hand_counts)
    after_shanten = _after_shanten_proxy(example, kind, before_shanten)
    players = max(4, len(example.scores), example.seat + 1, example.from_seat + 1)
    from_offset = (example.from_seat - example.seat) % players

    features: list[float] = [1.0]
    features.extend(1.0 if kind == candidate else 0.0 for candidate in CALL_DECISION_KINDS)
    features.extend(1.0 if index == discarded_index else 0.0 for index in range(34))
    features.extend(1.0 if call_kind in example.legal_call_kinds else 0.0 for call_kind in _NON_PASS_CALL_KINDS)
    features.append(len(example.legal_call_kinds) / len(_NON_PASS_CALL_KINDS))
    features.extend(count / 4.0 for count in example.hand_counts)
    features.extend(count / 4.0 for count in example.visible_counts)
    features.append(example.hand_counts[discarded_index] / 4.0)
    features.extend(1.0 if from_offset == offset else 0.0 for offset in (1, 2, 3))
    features.append(1.0 if example.seat == example.dealer else 0.0)
    features.append(1.0 if example.from_seat == example.dealer else 0.0)
    features.append(before_shanten / 8.0)
    features.append(after_shanten / 8.0)
    features.append((after_shanten - before_shanten) / 8.0)

    if len(features) != CALL_LINEAR_FEATURE_DIM:
        raise ValueError("call linear feature construction drifted from feature names")
    return tuple(features)


def _after_shanten_proxy(example: CallExample, kind: ActionKind, before_shanten: int) -> int:
    if kind == ActionKind.PASS:
        return before_shanten
    remainder_options = _candidate_remainder_counts(example, kind)
    if not remainder_options:
        return before_shanten
    return min(_safe_shanten(counts) for counts in remainder_options)


def _candidate_remainder_counts(
    example: CallExample,
    kind: ActionKind,
) -> tuple[tuple[int, ...], ...]:
    discarded_index = example.discarded_tile.type.index
    if kind == ActionKind.PON:
        return _remove_counts(example.hand_counts, ((discarded_index, 2),))
    if kind == ActionKind.MINKAN:
        return _remove_counts(example.hand_counts, ((discarded_index, 3),))
    if kind == ActionKind.CHI:
        return tuple(
            counts
            for removals in _chi_removals(example.discarded_tile.type, example.hand_counts)
            for counts in _remove_counts(example.hand_counts, removals)
        )
    return ()


def _chi_removals(
    discarded_type: TileType,
    hand_counts: tuple[int, ...],
) -> tuple[tuple[tuple[int, int], ...], ...]:
    if discarded_type.is_honor:
        return ()
    rank = discarded_type.rank
    assert rank is not None
    suit_start = discarded_type.index - rank + 1
    removals: list[tuple[tuple[int, int], ...]] = []
    for start_rank in range(max(1, rank - 2), min(7, rank) + 1):
        needed = tuple(
            suit_start + needed_rank - 1
            for needed_rank in range(start_rank, start_rank + 3)
            if needed_rank != rank
        )
        if all(hand_counts[index] > 0 for index in needed):
            removals.append(tuple((index, 1) for index in needed))
    return tuple(removals)


def _remove_counts(
    counts: tuple[int, ...],
    removals: tuple[tuple[int, int], ...],
) -> tuple[tuple[int, ...], ...]:
    next_counts = list(counts)
    for tile_index, amount in removals:
        if next_counts[tile_index] < amount:
            return ()
        next_counts[tile_index] -= amount
    return (tuple(next_counts),)


def _safe_shanten(counts: tuple[int, ...]) -> int:
    try:
        return shanten(counts)
    except ValueError:
        return 8


def _apply_update(
    weights: list[list[float]],
    example: _PreparedCallExample,
    *,
    learning_rate: float,
    l2: float,
) -> None:
    logits = {
        kind: _dot(weights[_kind_index(kind)], features)
        for kind, features in example.features_by_kind.items()
    }
    probabilities = _softmax(logits)
    for kind, features in example.features_by_kind.items():
        row = weights[_kind_index(kind)]
        target = 1.0 if kind == example.target else 0.0
        error = probabilities[kind] - target
        for index, value in enumerate(features):
            row[index] -= learning_rate * (error * value + l2 * row[index])


def _softmax(logits: dict[ActionKind, float]) -> dict[ActionKind, float]:
    max_logit = max(logits.values())
    exp_values = {
        kind: exp(logit - max_logit)
        for kind, logit in logits.items()
    }
    total = sum(exp_values.values())
    return {
        kind: value / total
        for kind, value in exp_values.items()
    }


def _dot(weights: tuple[float, ...] | list[float], features: tuple[float, ...]) -> float:
    return sum(weight * feature for weight, feature in zip(weights, features))


def _kind_index(kind: ActionKind) -> int:
    if kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call decision kind: {kind.value}")
    return CALL_DECISION_KINDS.index(kind)
