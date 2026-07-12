from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from functools import lru_cache
from typing import Any, cast

from kenjaku.core import ActionKind, TileType, shanten
from kenjaku.features import call as call_features
from kenjaku.features.call import (
    FeatureProfile as _FeatureProfile,
)
from kenjaku.features.call import (
    feature_profile as _feature_profile,
)
from kenjaku.features.call import (
    feature_profile_for_kind as _feature_profile_for_kind,
)
from kenjaku.models._linear_base import (
    LinearModel,
    PreparedExample,
    fit_softmax_sgd,
    logits_for_candidates,
    model_payload,
    parse_weight_matrix,
    prepared_softmax_example,
    require_features_by_label,
    softmax,
    weight_matrix_payload,
)
from kenjaku.training import CallExample

CALL_DECISION_KINDS = call_features.CALL_DECISION_KINDS
CALL_LINEAR_FEATURE_DIM = call_features.CALL_LINEAR_FEATURE_DIM
CALL_LINEAR_FEATURE_NAMES = call_features.CALL_LINEAR_FEATURE_NAMES
CALL_LINEAR_MODEL_KIND = call_features.CALL_LINEAR_MODEL_KIND
CALL_LINEAR_V0_FEATURE_PROFILE = call_features.CALL_LINEAR_V0_FEATURE_PROFILE
CALL_LINEAR_V1_FEATURE_DIM = call_features.CALL_LINEAR_V1_FEATURE_DIM
CALL_LINEAR_V1_FEATURE_NAMES = call_features.CALL_LINEAR_V1_FEATURE_NAMES
CALL_LINEAR_V1_FEATURE_PROFILE = call_features.CALL_LINEAR_V1_FEATURE_PROFILE
CALL_LINEAR_V1_MODEL_KIND = call_features.CALL_LINEAR_V1_MODEL_KIND
_NON_PASS_CALL_KINDS = (ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN)
_SHANTEN_CACHE_MAXSIZE = 16_384
_UKEIRE_CACHE_MAXSIZE = 16_384


_PreparedCallExample = PreparedExample[ActionKind]


@dataclass(frozen=True, slots=True)
class _CandidateProxy:
    after_counts: tuple[int, ...]
    consumed_count: int
    chi_shape: str | None


@dataclass(frozen=True, slots=True)
class CallLinearModel(LinearModel):
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
        weights = fit_softmax_sgd(
            prepared_examples,
            output_count=len(CALL_DECISION_KINDS),
            feature_dim=profile.feature_dim,
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            index_of=_kind_index,
            example_weight=lambda example: (
                positive_class_weight if example.label != ActionKind.PASS else 1.0
            ),
        )

        return cls(
            weights=weights,
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

    def to_dict(self) -> dict[str, Any]:
        return model_payload(
            kind=self.kind,
            feature_profile=self.feature_profile,
            feature_dim=self.feature_dim,
            epochs=self.epochs,
            learning_rate=self.learning_rate,
            l2=self.l2,
            positive_class_weight=self.positive_class_weight,
            weights=weight_matrix_payload(self.weights),
        )

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> CallLinearModel:
        profile = _feature_profile_for_kind(payload.get("kind"))
        if profile is None:
            raise ValueError("unsupported call linear model kind")
        if payload.get("feature_dim") != profile.feature_dim:
            raise ValueError("unsupported call linear model feature dimension")
        if payload.get("feature_profile", profile.name) != profile.name:
            raise ValueError("call linear model kind/profile mismatch")
        return cls(
            weights=parse_weight_matrix(payload.get("weights")),
            epochs=int(payload["epochs"]),
            learning_rate=float(payload["learning_rate"]),
            l2=float(payload.get("l2", 0.0)),
            feature_profile=profile.name,
            positive_class_weight=float(payload.get("positive_class_weight", 1.0)),
        )

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
                "target": example.label.value,
                "features_by_kind": {
                    kind.value: list(features)
                    for kind, features in require_features_by_label(example).items()
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
        payload = cast(list[Any], payload)
        prepared: list[_PreparedCallExample] = []
        for item in payload:
            if not isinstance(item, dict):
                raise ValueError("prepared example payload items must be objects")
            item = cast(dict[str, Any], item)
            target = ActionKind(str(item.get("target")))
            features_payload = item.get("features_by_kind")
            if not isinstance(features_payload, dict):
                raise ValueError("prepared example features_by_kind must be an object")
            features_payload = cast(dict[str, Any], features_payload)
            features_by_kind = {
                ActionKind(str(kind)): tuple(float(value) for value in cast(list[Any], features))
                for kind, features in features_payload.items()
                if isinstance(features, list)
            }
            if target not in CALL_DECISION_KINDS:
                raise ValueError(f"unsupported prepared call target: {target.value}")
            if not features_by_kind:
                raise ValueError("prepared example must have candidate features")
            prepared.append(
                prepared_softmax_example(label=target, features_by_label=features_by_kind)
            )
        return tuple(prepared)

    def predict(self, example: CallExample) -> ActionKind:
        logits = self.logits_for_example(example)
        return max(logits, key=lambda kind: (logits[kind], -_kind_index(kind)))

    def logits_for_example(self, example: CallExample) -> dict[ActionKind, float]:
        prepared = _prepare_example(example, profile=_feature_profile(self.feature_profile))
        return self.logits_for_prepared(prepared)

    def probabilities_for_example(self, example: CallExample) -> dict[ActionKind, float]:
        return softmax(self.logits_for_example(example))

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
        return logits_for_candidates(
            self.weights,
            require_features_by_label(prepared),
            index_of=_kind_index,
        )

    def probabilities_for_prepared(
        self,
        prepared: _PreparedCallExample,
    ) -> dict[ActionKind, float]:
        return softmax(self.logits_for_prepared(prepared))

    def score(self, examples: list[CallExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        prepared_examples = self.prepare_examples(examples)
        correct = sum(
            self.predict_prepared(prepared) == example.action.kind
            for example, prepared in zip(examples, prepared_examples, strict=True)
        )
        return correct / len(examples)


def _prepare_example(example: CallExample, *, profile: _FeatureProfile) -> _PreparedCallExample:
    if example.action.kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call action kind: {example.action.kind.value}")
    candidates = _candidate_kinds(example)
    if example.action.kind not in candidates:
        candidates = (*candidates, example.action.kind)
    return prepared_softmax_example(
        label=example.action.kind,
        features_by_label={
            kind: _features_for_candidate(example, kind, profile=profile) for kind in candidates
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
    features.extend(
        1.0 if call_kind in example.legal_call_kinds else 0.0 for call_kind in _NON_PASS_CALL_KINDS
    )
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


@lru_cache(maxsize=_UKEIRE_CACHE_MAXSIZE)
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


@lru_cache(maxsize=_SHANTEN_CACHE_MAXSIZE)
def _safe_shanten(counts: tuple[int, ...]) -> int:
    try:
        return shanten(counts)
    except ValueError:
        return 8


def _kind_index(kind: ActionKind) -> int:
    if kind not in CALL_DECISION_KINDS:
        raise ValueError(f"unsupported call decision kind: {kind.value}")
    return CALL_DECISION_KINDS.index(kind)
