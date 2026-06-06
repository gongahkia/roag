from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from functools import cache
from math import exp

from kenjaku.core import ActionKind, TileType, all_tile_types, shanten
from kenjaku.models.call_frequency import CALL_DECISION_KINDS
from kenjaku.training import CallExample

CALL_LINEAR_V0_FEATURE_PROFILE = "v0"
CALL_LINEAR_V1_FEATURE_PROFILE = "v1"
CALL_LINEAR_MODEL_KIND = "call-linear-v0"
CALL_LINEAR_V1_MODEL_KIND = "call-linear-v1"

_NON_PASS_CALL_KINDS = (ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN)
_TILE_FEATURE_NAMES = tuple(tile_type.notation for tile_type in all_tile_types())
_CALL_LINEAR_V0_FEATURE_NAMES = (
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
CALL_LINEAR_FEATURE_NAMES = _CALL_LINEAR_V0_FEATURE_NAMES
CALL_LINEAR_FEATURE_DIM = len(CALL_LINEAR_FEATURE_NAMES)
_CALL_LINEAR_V1_EXTRA_FEATURE_NAMES = (
    "candidate_non_pass",
    "candidate_chi_left",
    "candidate_chi_middle",
    "candidate_chi_right",
    "candidate_consumed_fraction",
    "after_call_tile_count",
    "shanten_improved_proxy",
    "shanten_same_proxy",
    "shanten_worsened_proxy",
    "before_ukeire_proxy",
    "after_ukeire_proxy",
    "ukeire_delta_proxy",
    "discarded_visible_count",
    "discarded_unseen_count",
    "discarded_is_honor",
    "discarded_is_terminal",
    "discarded_is_terminal_or_honor",
)
CALL_LINEAR_V1_FEATURE_NAMES = (
    *_CALL_LINEAR_V0_FEATURE_NAMES,
    *_CALL_LINEAR_V1_EXTRA_FEATURE_NAMES,
)
CALL_LINEAR_V1_FEATURE_DIM = len(CALL_LINEAR_V1_FEATURE_NAMES)


@dataclass(frozen=True, slots=True)
class _FeatureProfile:
    name: str
    model_kind: str
    feature_names: tuple[str, ...]

    @property
    def feature_dim(self) -> int:
        return len(self.feature_names)


_FEATURE_PROFILES = {
    CALL_LINEAR_V0_FEATURE_PROFILE: _FeatureProfile(
        name=CALL_LINEAR_V0_FEATURE_PROFILE,
        model_kind=CALL_LINEAR_MODEL_KIND,
        feature_names=CALL_LINEAR_FEATURE_NAMES,
    ),
    CALL_LINEAR_V1_FEATURE_PROFILE: _FeatureProfile(
        name=CALL_LINEAR_V1_FEATURE_PROFILE,
        model_kind=CALL_LINEAR_V1_MODEL_KIND,
        feature_names=CALL_LINEAR_V1_FEATURE_NAMES,
    ),
}


@dataclass(frozen=True, slots=True)
class _PreparedCallExample:
    target: ActionKind
    features_by_kind: dict[ActionKind, tuple[float, ...]]


@dataclass(frozen=True, slots=True)
class _CandidateProxy:
    after_counts: tuple[int, ...]
    consumed_count: int
    chi_shape: str | None


@dataclass(frozen=True, slots=True)
class CallLinearModel:
    """Tiny dependency-free masked softmax model for call/pass decisions."""

    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float
    l2: float = 0.0
    feature_profile: str = CALL_LINEAR_V0_FEATURE_PROFILE
    positive_class_weight: float = 1.0

    def __post_init__(self) -> None:
        profile = _feature_profile(self.feature_profile)
        if len(self.weights) != len(CALL_DECISION_KINDS):
            raise ValueError("call linear model has unsupported output rows")
        if any(len(row) != profile.feature_dim for row in self.weights):
            raise ValueError(f"call linear model rows must have {profile.feature_dim} features")
        if self.epochs < 0:
            raise ValueError("epochs must be non-negative")
        if self.learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if self.l2 < 0:
            raise ValueError("l2 must be non-negative")
        if self.positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

    @classmethod
    def fit(
        cls,
        examples: list[CallExample],
        *,
        epochs: int = 25,
        learning_rate: float = 0.1,
        l2: float = 0.0,
        feature_profile: str = CALL_LINEAR_V0_FEATURE_PROFILE,
        positive_class_weight: float = 1.0,
    ) -> CallLinearModel:
        if not examples:
            raise ValueError("cannot train on zero examples")
        if epochs < 0:
            raise ValueError("epochs must be non-negative")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")
        if positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

        prepared_examples = cls.prepare_examples_for_profile(
            examples,
            feature_profile=feature_profile,
        )
        return cls.fit_prepared(
            prepared_examples,
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            feature_profile=feature_profile,
            positive_class_weight=positive_class_weight,
        )

    @classmethod
    def fit_prepared(
        cls,
        prepared_examples: Sequence[_PreparedCallExample],
        *,
        epochs: int = 25,
        learning_rate: float = 0.1,
        l2: float = 0.0,
        feature_profile: str = CALL_LINEAR_V0_FEATURE_PROFILE,
        positive_class_weight: float = 1.0,
    ) -> CallLinearModel:
        if not prepared_examples:
            raise ValueError("cannot train on zero examples")
        if epochs < 0:
            raise ValueError("epochs must be non-negative")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")
        if positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

        profile = _feature_profile(feature_profile)
        weights = [[0.0] * profile.feature_dim for _ in CALL_DECISION_KINDS]
        for _ in range(epochs):
            for example in prepared_examples:
                _apply_update(
                    weights,
                    example,
                    learning_rate=learning_rate,
                    l2=l2,
                    example_weight=(
                        positive_class_weight
                        if example.target != ActionKind.PASS
                        else 1.0
                    ),
                )

        return cls(
            weights=tuple(tuple(row) for row in weights),
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            feature_profile=profile.name,
            positive_class_weight=positive_class_weight,
        )

    @property
    def kind(self) -> str:
        return _feature_profile(self.feature_profile).model_kind

    @property
    def feature_dim(self) -> int:
        return _feature_profile(self.feature_profile).feature_dim

    @property
    def feature_names(self) -> tuple[str, ...]:
        return _feature_profile(self.feature_profile).feature_names

    @staticmethod
    def feature_names_for_profile(feature_profile: str) -> tuple[str, ...]:
        return _feature_profile(feature_profile).feature_names

    @staticmethod
    def prepare_examples_for_profile(
        examples: Sequence[CallExample],
        *,
        feature_profile: str = CALL_LINEAR_V0_FEATURE_PROFILE,
    ) -> tuple[_PreparedCallExample, ...]:
        profile = _feature_profile(feature_profile)
        return tuple(_prepare_example(example, profile=profile) for example in examples)

    @staticmethod
    def prepared_examples_to_payload(
        prepared_examples: Sequence[_PreparedCallExample],
    ) -> list[dict[str, object]]:
        return [
            {
                "target": example.target.value,
                "features_by_kind": {
                    kind.value: list(features)
                    for kind, features in example.features_by_kind.items()
                },
            }
            for example in prepared_examples
        ]

    @staticmethod
    def prepared_examples_from_payload(
        payload: object,
    ) -> tuple[_PreparedCallExample, ...]:
        if not isinstance(payload, list):
            raise ValueError("prepared examples payload must be a list")
        prepared: list[_PreparedCallExample] = []
        for item in payload:
            if not isinstance(item, dict):
                raise ValueError("prepared example payload items must be objects")
            target = ActionKind(str(item.get("target")))
            features_payload = item.get("features_by_kind")
            if not isinstance(features_payload, dict):
                raise ValueError("prepared example features_by_kind must be an object")
            features_by_kind = {
                ActionKind(str(kind)): tuple(float(value) for value in features)
                for kind, features in features_payload.items()
                if isinstance(features, list)
            }
            if target not in CALL_DECISION_KINDS:
                raise ValueError(f"unsupported prepared call target: {target.value}")
            if not features_by_kind:
                raise ValueError("prepared example must have candidate features")
            prepared.append(
                _PreparedCallExample(
                    target=target,
                    features_by_kind=features_by_kind,
                )
            )
        return tuple(prepared)

    def predict(self, example: CallExample) -> ActionKind:
        logits = self.logits_for_example(example)
        return max(logits, key=lambda kind: (logits[kind], -_kind_index(kind)))

    def logits_for_example(self, example: CallExample) -> dict[ActionKind, float]:
        prepared = _prepare_example(example, profile=_feature_profile(self.feature_profile))
        return self.logits_for_prepared(prepared)

    def probabilities_for_example(self, example: CallExample) -> dict[ActionKind, float]:
        return _softmax(self.logits_for_example(example))

    def prepare_examples(
        self,
        examples: Sequence[CallExample],
    ) -> tuple[_PreparedCallExample, ...]:
        return self.prepare_examples_for_profile(
            examples,
            feature_profile=self.feature_profile,
        )

    def predict_prepared(self, prepared: _PreparedCallExample) -> ActionKind:
        logits = self.logits_for_prepared(prepared)
        return max(logits, key=lambda kind: (logits[kind], -_kind_index(kind)))

    def logits_for_prepared(self, prepared: _PreparedCallExample) -> dict[ActionKind, float]:
        return {
            kind: _dot(self.weights[_kind_index(kind)], features)
            for kind, features in prepared.features_by_kind.items()
        }

    def probabilities_for_prepared(
        self,
        prepared: _PreparedCallExample,
    ) -> dict[ActionKind, float]:
        return _softmax(self.logits_for_prepared(prepared))

    def score(self, examples: list[CallExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        prepared_examples = self.prepare_examples(examples)
        correct = sum(
            self.predict_prepared(prepared) == example.action.kind
            for example, prepared in zip(examples, prepared_examples)
        )
        return correct / len(examples)


def _prepare_example(example: CallExample, *, profile: _FeatureProfile) -> _PreparedCallExample:
    if example.action.kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call action kind: {example.action.kind.value}")
    candidates = _candidate_kinds(example)
    if example.action.kind not in candidates:
        candidates = (*candidates, example.action.kind)
    return _PreparedCallExample(
        target=example.action.kind,
        features_by_kind={
            kind: _features_for_candidate(example, kind, profile=profile)
            for kind in candidates
        },
    )


def _candidate_kinds(example: CallExample) -> tuple[ActionKind, ...]:
    candidates: list[ActionKind] = [ActionKind.PASS]
    for kind in CALL_DECISION_KINDS:
        if kind != ActionKind.PASS and kind in example.legal_call_kinds:
            candidates.append(kind)
    return tuple(candidates)


def _features_for_candidate(
    example: CallExample,
    kind: ActionKind,
    *,
    profile: _FeatureProfile,
) -> tuple[float, ...]:
    discarded_index = example.discarded_tile.type.index
    before_shanten = _safe_shanten(example.hand_counts)
    proxy = _candidate_proxy(
        example,
        kind,
        prefer_ukeire_tiebreaker=profile.name == CALL_LINEAR_V1_FEATURE_PROFILE,
    )
    after_shanten = _safe_shanten(proxy.after_counts)
    players = max(4, len(example.scores), example.seat + 1, example.from_seat + 1)
    from_offset = (example.from_seat - example.seat) % players
    discarded_type = example.discarded_tile.type

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

    if profile.name == CALL_LINEAR_V1_FEATURE_PROFILE:
        before_ukeire = _ukeire_proxy(example.hand_counts)
        after_ukeire = _ukeire_proxy(proxy.after_counts)
        shanten_delta = after_shanten - before_shanten
        features.extend(
            (
                1.0 if kind != ActionKind.PASS else 0.0,
                1.0 if proxy.chi_shape == "left" else 0.0,
                1.0 if proxy.chi_shape == "middle" else 0.0,
                1.0 if proxy.chi_shape == "right" else 0.0,
                proxy.consumed_count / 3.0,
                sum(proxy.after_counts) / 14.0,
                1.0 if shanten_delta < 0 else 0.0,
                1.0 if shanten_delta == 0 else 0.0,
                1.0 if shanten_delta > 0 else 0.0,
                before_ukeire / 34.0,
                after_ukeire / 34.0,
                (after_ukeire - before_ukeire) / 34.0,
                example.visible_counts[discarded_index] / 4.0,
                max(0, 4 - example.visible_counts[discarded_index]) / 4.0,
                1.0 if discarded_type.is_honor else 0.0,
                1.0 if discarded_type.is_terminal else 0.0,
                1.0 if discarded_type.is_terminal_or_honor else 0.0,
            )
        )

    if len(features) != profile.feature_dim:
        raise ValueError("call linear feature construction drifted from feature names")
    return tuple(features)


def _after_shanten_proxy(example: CallExample, kind: ActionKind, before_shanten: int) -> int:
    return _safe_shanten(
        _candidate_proxy(
            example,
            kind,
            prefer_ukeire_tiebreaker=False,
        ).after_counts
    )


def _candidate_proxy(
    example: CallExample,
    kind: ActionKind,
    *,
    prefer_ukeire_tiebreaker: bool,
) -> _CandidateProxy:
    if kind == ActionKind.PASS:
        return _CandidateProxy(
            after_counts=example.hand_counts,
            consumed_count=0,
            chi_shape=None,
        )
    options = _candidate_remainder_options(example, kind)
    if not options:
        return _CandidateProxy(
            after_counts=example.hand_counts,
            consumed_count=0,
            chi_shape=None,
        )

    def key(proxy: _CandidateProxy) -> tuple[int, int, int]:
        ukeire = _ukeire_proxy(proxy.after_counts) if prefer_ukeire_tiebreaker else 0
        return (
            _safe_shanten(proxy.after_counts),
            -ukeire,
            _chi_shape_index(proxy.chi_shape),
        )

    return min(options, key=key)


def _candidate_remainder_counts(
    example: CallExample,
    kind: ActionKind,
) -> tuple[tuple[int, ...], ...]:
    return tuple(proxy.after_counts for proxy in _candidate_remainder_options(example, kind))


def _candidate_remainder_options(
    example: CallExample,
    kind: ActionKind,
) -> tuple[_CandidateProxy, ...]:
    discarded_index = example.discarded_tile.type.index
    if kind == ActionKind.PON:
        return _call_proxy_options(
            example.hand_counts,
            ((discarded_index, 2),),
            chi_shape=None,
        )
    if kind == ActionKind.MINKAN:
        return _call_proxy_options(
            example.hand_counts,
            ((discarded_index, 3),),
            chi_shape=None,
        )
    if kind == ActionKind.CHI:
        return tuple(
            proxy
            for removals in _chi_removals(example.discarded_tile.type, example.hand_counts)
            for proxy in _call_proxy_options(
                example.hand_counts,
                removals,
                chi_shape=_chi_shape(example.discarded_tile.type, removals),
            )
        )
    return ()


def _call_proxy_options(
    counts: tuple[int, ...],
    removals: tuple[tuple[int, int], ...],
    *,
    chi_shape: str | None,
) -> tuple[_CandidateProxy, ...]:
    remainder_options = _remove_counts(counts, removals)
    consumed_count = sum(amount for _, amount in removals)
    return tuple(
        _CandidateProxy(
            after_counts=after_counts,
            consumed_count=consumed_count,
            chi_shape=chi_shape,
        )
        for after_counts in remainder_options
    )


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


def _chi_shape(
    discarded_type: TileType,
    removals: tuple[tuple[int, int], ...],
) -> str | None:
    if discarded_type.is_honor:
        return None
    removed_indices = tuple(index for index, _ in removals)
    if len(removed_indices) != 2:
        return None
    sequence_indices = sorted((*removed_indices, discarded_type.index))
    if sequence_indices != list(range(sequence_indices[0], sequence_indices[0] + 3)):
        return None
    position = sequence_indices.index(discarded_type.index)
    return ("left", "middle", "right")[position]


def _chi_shape_index(chi_shape: str | None) -> int:
    if chi_shape == "left":
        return 0
    if chi_shape == "middle":
        return 1
    if chi_shape == "right":
        return 2
    return 3


@cache
def _ukeire_proxy(counts: tuple[int, ...]) -> int:
    before_shanten = _safe_shanten(counts)
    total = 0
    for index, count in enumerate(counts):
        if count >= 4:
            continue
        next_counts = list(counts)
        next_counts[index] += 1
        if _safe_shanten(tuple(next_counts)) < before_shanten:
            total += 1
    return total


@cache
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
    example_weight: float,
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
        scaled_error = learning_rate * example_weight * error
        if l2 == 0:
            for index, value in enumerate(features):
                if value:
                    row[index] -= scaled_error * value
        else:
            for index, value in enumerate(features):
                row[index] -= learning_rate * (example_weight * error * value + l2 * row[index])


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
    total = 0.0
    for weight, feature in zip(weights, features):
        if feature:
            total += weight * feature
    return total


def _kind_index(kind: ActionKind) -> int:
    if kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call decision kind: {kind.value}")
    return CALL_DECISION_KINDS.index(kind)


def _feature_profile(name: str) -> _FeatureProfile:
    try:
        return _FEATURE_PROFILES[name]
    except KeyError as exc:
        raise ValueError(f"unsupported call linear feature profile: {name}") from exc
