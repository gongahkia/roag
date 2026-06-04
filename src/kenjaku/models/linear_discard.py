from __future__ import annotations

import json
from collections.abc import Sequence
from dataclasses import dataclass
from math import exp
from pathlib import Path
from typing import Any

from kenjaku.core import Action, Tile, TileType, shanten
from kenjaku.training import DiscardExample
from kenjaku.training.defense_features import (
    candidate_has_kabe,
    candidate_has_one_chance,
    candidate_has_suji,
    candidate_is_genbutsu,
    candidate_seen_after_riichi,
    candidate_seen_before_riichi,
    has_active_riichi_opponent,
    max_active_riichi_discards_elapsed,
    min_active_riichi_discards_elapsed,
)

RAW_COUNT_FEATURE_PROFILE = "raw-count"
SHANTEN_FEATURE_PROFILE = "shanten"
RISK_CONTEXT_FEATURE_PROFILE = "risk-context"
DEFENSE_CONTEXT_FEATURE_PROFILE = "defense-context"
RAW_COUNT_FEATURE_DIM = 69
RAW_COUNT_MODEL_KIND = "discard-linear-raw-count-v0"
FEATURE_DIM = 76
MODEL_KIND = "discard-linear-v1"
RISK_CONTEXT_FEATURE_DIM = 86
RISK_CONTEXT_MODEL_KIND = "discard-linear-risk-context-v0"
DEFENSE_CONTEXT_FEATURE_DIM = 98
DEFENSE_CONTEXT_MODEL_KIND = "discard-linear-defense-context-v0"


@dataclass(frozen=True, slots=True)
class _FeatureProfile:
    name: str
    model_kind: str
    feature_dim: int
    includes_tile_efficiency: bool
    includes_risk_context: bool
    includes_defense_context: bool


@dataclass(frozen=True, slots=True)
class _PreparedExample:
    target: int
    features_by_tile: dict[int, tuple[float, ...]]


_FEATURE_PROFILES = {
    RAW_COUNT_FEATURE_PROFILE: _FeatureProfile(
        name=RAW_COUNT_FEATURE_PROFILE,
        model_kind=RAW_COUNT_MODEL_KIND,
        feature_dim=RAW_COUNT_FEATURE_DIM,
        includes_tile_efficiency=False,
        includes_risk_context=False,
        includes_defense_context=False,
    ),
    SHANTEN_FEATURE_PROFILE: _FeatureProfile(
        name=SHANTEN_FEATURE_PROFILE,
        model_kind=MODEL_KIND,
        feature_dim=FEATURE_DIM,
        includes_tile_efficiency=True,
        includes_risk_context=False,
        includes_defense_context=False,
    ),
    RISK_CONTEXT_FEATURE_PROFILE: _FeatureProfile(
        name=RISK_CONTEXT_FEATURE_PROFILE,
        model_kind=RISK_CONTEXT_MODEL_KIND,
        feature_dim=RISK_CONTEXT_FEATURE_DIM,
        includes_tile_efficiency=True,
        includes_risk_context=True,
        includes_defense_context=False,
    ),
    DEFENSE_CONTEXT_FEATURE_PROFILE: _FeatureProfile(
        name=DEFENSE_CONTEXT_FEATURE_PROFILE,
        model_kind=DEFENSE_CONTEXT_MODEL_KIND,
        feature_dim=DEFENSE_CONTEXT_FEATURE_DIM,
        includes_tile_efficiency=True,
        includes_risk_context=True,
        includes_defense_context=True,
    ),
}
_FEATURE_PROFILES_BY_KIND = {
    profile.model_kind: profile
    for profile in _FEATURE_PROFILES.values()
}


@dataclass(frozen=True, slots=True)
class DiscardLinearModel:
    """Tiny dependency-free softmax model for discard prediction."""

    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float
    feature_profile: str = SHANTEN_FEATURE_PROFILE

    def __post_init__(self) -> None:
        profile = _feature_profile(self.feature_profile)
        if len(self.weights) != 34:
            raise ValueError("discard model must have 34 output rows")
        if any(len(row) != profile.feature_dim for row in self.weights):
            raise ValueError(f"discard model rows must have {profile.feature_dim} features")

    @classmethod
    def fit(
        cls,
        examples: Sequence[DiscardExample],
        *,
        epochs: int = 25,
        learning_rate: float = 0.1,
        l2: float = 0.0,
        feature_profile: str = SHANTEN_FEATURE_PROFILE,
    ) -> DiscardLinearModel:
        if not examples:
            raise ValueError("cannot train on zero examples")
        if epochs <= 0:
            raise ValueError("epochs must be positive")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")

        profile = _feature_profile(feature_profile)
        weights = [[0.0] * profile.feature_dim for _ in range(34)]
        prepared_examples = _prepare_examples(examples, profile=profile)
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
            feature_profile=profile.name,
        )

    def predict(
        self,
        hand_counts: tuple[int, ...],
        visible_counts: tuple[int, ...],
        *,
        seat: int = 0,
        active_riichi_seats: tuple[bool, ...] = (),
        river_counts_by_seat: tuple[tuple[int, ...], ...] = (),
        rivers_by_seat: tuple[tuple[Tile, ...], ...] = (),
        riichi_declared_turns: tuple[int | None, ...] = (),
        riichi_declared_event_indices: tuple[int | None, ...] = (),
    ) -> TileType:
        legal_indices = _legal_indices(hand_counts)
        features_by_tile = _feature_vectors(
            hand_counts,
            visible_counts,
            legal_indices,
            profile=_feature_profile(self.feature_profile),
            seat=seat,
            active_riichi_seats=active_riichi_seats,
            river_counts_by_seat=river_counts_by_seat,
            rivers_by_seat=rivers_by_seat,
            riichi_declared_turns=riichi_declared_turns,
            riichi_declared_event_indices=riichi_declared_event_indices,
        )
        logits = _logits(self.weights, features_by_tile)
        return TileType(max(logits, key=logits.get))

    def score(self, examples: Sequence[DiscardExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        prepared_examples = _prepare_examples(
            examples,
            profile=_feature_profile(self.feature_profile),
        )
        correct = 0
        for example in prepared_examples:
            logits = _logits(self.weights, example.features_by_tile)
            correct += max(logits, key=logits.get) == example.target
        return correct / len(examples)

    @property
    def kind(self) -> str:
        return _feature_profile(self.feature_profile).model_kind

    @property
    def feature_dim(self) -> int:
        return _feature_profile(self.feature_profile).feature_dim

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": self.kind,
            "feature_profile": self.feature_profile,
            "feature_dim": self.feature_dim,
            "epochs": self.epochs,
            "learning_rate": self.learning_rate,
            "weights": [list(row) for row in self.weights],
        }

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> DiscardLinearModel:
        profile = _feature_profile_for_kind(payload.get("kind"))
        if profile is None:
            raise ValueError("unsupported discard linear model kind")
        if payload.get("feature_dim") != profile.feature_dim:
            raise ValueError("unsupported discard linear model feature dimension")
        if payload.get("feature_profile", profile.name) != profile.name:
            raise ValueError("discard linear model kind/profile mismatch")

        weights_payload = payload.get("weights")
        if not isinstance(weights_payload, list):
            raise ValueError("model payload missing weights")
        weights = tuple(_parse_weight_row(row) for row in weights_payload)
        return cls(
            weights=weights,
            epochs=int(payload["epochs"]),
            learning_rate=float(payload["learning_rate"]),
            feature_profile=profile.name,
        )

    def save(self, path: str | Path) -> None:
        Path(path).write_text(
            json.dumps(self.to_dict(), indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )

    @classmethod
    def load(cls, path: str | Path) -> DiscardLinearModel:
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError("model artifact must contain a JSON object")
        return cls.from_dict(payload)


def _apply_update(
    weights: list[list[float]],
    example: _PreparedExample,
    *,
    learning_rate: float,
    l2: float,
) -> None:
    probabilities = _softmax(_logits(weights, example.features_by_tile))
    for tile_index, probability in probabilities.items():
        error = probability - (1.0 if tile_index == example.target else 0.0)
        row = weights[tile_index]
        features = example.features_by_tile[tile_index]
        for feature_index, feature_value in enumerate(features):
            regularization = l2 * row[feature_index]
            row[feature_index] -= learning_rate * (error * feature_value + regularization)


def _prepare_examples(
    examples: Sequence[DiscardExample],
    *,
    profile: _FeatureProfile,
) -> list[_PreparedExample]:
    prepared: list[_PreparedExample] = []
    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        legal_indices = _legal_indices(example.hand_counts)
        target = example.action.tile.index
        if target not in legal_indices:
            raise ValueError("discard action must be legal for the example hand")
        prepared.append(
            _PreparedExample(
                target=target,
                features_by_tile=_feature_vectors(
                    example.hand_counts,
                    example.visible_counts,
                    legal_indices,
                    profile=profile,
                    seat=example.seat,
                    active_riichi_seats=example.active_riichi_seats,
                    river_counts_by_seat=example.river_counts_by_seat,
                    rivers_by_seat=example.rivers_by_seat,
                    riichi_declared_turns=example.riichi_declared_turns,
                    riichi_declared_event_indices=example.riichi_declared_event_indices,
                ),
            )
        )
    return prepared


def _feature_vectors(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    legal_indices: tuple[int, ...],
    *,
    profile: _FeatureProfile,
    seat: int = 0,
    active_riichi_seats: tuple[bool, ...] = (),
    river_counts_by_seat: tuple[tuple[int, ...], ...] = (),
    rivers_by_seat: tuple[tuple[Tile, ...], ...] = (),
    riichi_declared_turns: tuple[int | None, ...] = (),
    riichi_declared_event_indices: tuple[int | None, ...] = (),
) -> dict[int, tuple[float, ...]]:
    if len(hand_counts) != 34:
        raise ValueError("hand counts must have length 34")
    if len(visible_counts) != 34:
        raise ValueError("visible counts must have length 34")
    if not profile.includes_tile_efficiency:
        features = _raw_features(hand_counts, visible_counts)
        return {tile_index: features for tile_index in legal_indices}

    before_shanten = shanten(hand_counts)
    return {
        tile_index: _features(
            hand_counts,
            visible_counts,
            tile_index=tile_index,
            before_shanten=before_shanten,
            profile=profile,
            seat=seat,
            active_riichi_seats=active_riichi_seats,
            river_counts_by_seat=river_counts_by_seat,
            rivers_by_seat=rivers_by_seat,
            riichi_declared_turns=riichi_declared_turns,
            riichi_declared_event_indices=riichi_declared_event_indices,
        )
        for tile_index in legal_indices
    }


def _features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    before_shanten: int,
    profile: _FeatureProfile,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
) -> tuple[float, ...]:
    after_counts = list(hand_counts)
    after_counts[tile_index] -= 1
    after_shanten = shanten(tuple(after_counts))
    shanten_delta = after_shanten - before_shanten
    features = (
        1.0,
        *(count / 4.0 for count in hand_counts),
        *(count / 4.0 for count in visible_counts),
        hand_counts[tile_index] / 4.0,
        visible_counts[tile_index] / 4.0,
        1.0 if _is_terminal_or_honor(tile_index) else 0.0,
        before_shanten / 8.0,
        after_shanten / 8.0,
        float(shanten_delta),
        1.0 if shanten_delta <= 0 else 0.0,
    )
    if profile.includes_risk_context:
        features = (
            *features,
            *_risk_context_features(
                visible_counts,
                tile_index=tile_index,
                seat=seat,
                active_riichi_seats=active_riichi_seats,
                river_counts_by_seat=river_counts_by_seat,
            ),
        )
    if profile.includes_defense_context:
        features = (
            *features,
            *_defense_context_features(
                hand_counts,
                visible_counts,
                tile_index=tile_index,
                seat=seat,
                active_riichi_seats=active_riichi_seats,
                river_counts_by_seat=river_counts_by_seat,
                rivers_by_seat=rivers_by_seat,
                riichi_declared_turns=riichi_declared_turns,
                riichi_declared_event_indices=riichi_declared_event_indices,
            ),
        )
    return features


def _example_for_candidate_context(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
) -> DiscardExample:
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=seat,
        dealer=0,
        scores=(),
        hand_counts=hand_counts,
        visible_counts=visible_counts,
        action=Action.discard(TileType(tile_index)),
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=riichi_declared_turns,
        riichi_declared_event_indices=riichi_declared_event_indices,
    )


def _defense_context_features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
) -> tuple[float, ...]:
    example = _example_for_candidate_context(
        hand_counts,
        visible_counts,
        tile_index=tile_index,
        seat=seat,
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=riichi_declared_turns,
        riichi_declared_event_indices=riichi_declared_event_indices,
    )
    has_riichi = has_active_riichi_opponent(example)
    genbutsu = candidate_is_genbutsu(example, tile_index)
    suji = candidate_has_suji(example, tile_index)
    kabe = candidate_has_kabe(example, tile_index)
    one_chance = candidate_has_one_chance(example, tile_index)
    seen_after_riichi = candidate_seen_after_riichi(example, tile_index)
    seen_before_riichi = candidate_seen_before_riichi(example, tile_index)
    max_elapsed = max_active_riichi_discards_elapsed(example)
    min_elapsed = min_active_riichi_discards_elapsed(example)
    unseen_count = max(0, 4 - visible_counts[tile_index])
    tile_type = TileType(tile_index)

    return (
        1.0 if genbutsu else 0.0,
        1.0 if suji else 0.0,
        1.0 if kabe else 0.0,
        1.0 if one_chance else 0.0,
        1.0 if seen_after_riichi else 0.0,
        1.0 if seen_before_riichi else 0.0,
        max_elapsed / 18.0,
        min_elapsed / 18.0,
        1.0 if genbutsu or suji or kabe else 0.0,
        unseen_count / 4.0 if has_riichi and not (genbutsu or suji or kabe) else 0.0,
        1.0 if tile_type.is_honor else 0.0,
        1.0 if tile_type.is_terminal else 0.0,
    )


def _raw_features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
) -> tuple[float, ...]:
    return (
        1.0,
        *(count / 4.0 for count in hand_counts),
        *(count / 4.0 for count in visible_counts),
    )


def _risk_context_features(
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
) -> tuple[float, ...]:
    players = _context_player_count(seat, active_riichi_seats, river_counts_by_seat)
    active_opponents = tuple(
        candidate_seat
        for candidate_seat in range(players)
        if candidate_seat != seat and _active_riichi_at(active_riichi_seats, candidate_seat)
    )
    opponent_seats = tuple(
        candidate_seat for candidate_seat in range(players) if candidate_seat != seat
    )
    active_riichi_river_count = _sum_river_count(
        river_counts_by_seat,
        active_opponents,
        tile_index,
    )
    opponent_river_count = _sum_river_count(river_counts_by_seat, opponent_seats, tile_index)
    self_river_count = _river_count(river_counts_by_seat, seat, tile_index)
    all_river_count = _sum_river_count(river_counts_by_seat, range(players), tile_index)
    unseen_count = max(0, 4 - visible_counts[tile_index])
    opponent_denominator = max(1, players - 1)

    return (
        1.0 if _active_riichi_at(active_riichi_seats, seat) else 0.0,
        len(active_opponents) / opponent_denominator,
        1.0 if active_opponents else 0.0,
        active_riichi_river_count / 4.0,
        1.0 if active_riichi_river_count > 0 else 0.0,
        opponent_river_count / 4.0,
        1.0 if opponent_river_count > 0 else 0.0,
        self_river_count / 4.0,
        all_river_count / 4.0,
        unseen_count / 4.0 if active_opponents else 0.0,
    )


def _legal_indices(hand_counts: tuple[int, ...]) -> tuple[int, ...]:
    legal_indices = tuple(index for index, count in enumerate(hand_counts) if count > 0)
    if not legal_indices:
        raise ValueError("cannot predict a discard from an empty hand")
    return legal_indices


def _logits(
    weights: Sequence[Sequence[float]],
    features_by_tile: dict[int, tuple[float, ...]],
) -> dict[int, float]:
    return {
        tile_index: sum(weight * feature for weight, feature in zip(weights[tile_index], features))
        for tile_index, features in features_by_tile.items()
    }


def _softmax(logits: dict[int, float]) -> dict[int, float]:
    max_logit = max(logits.values())
    exp_values = {
        tile_index: exp(logit - max_logit)
        for tile_index, logit in logits.items()
    }
    denominator = sum(exp_values.values())
    return {
        tile_index: value / denominator
        for tile_index, value in exp_values.items()
    }


def _parse_weight_row(row: Any) -> tuple[float, ...]:
    if not isinstance(row, list):
        raise ValueError("model weight rows must be lists")
    return tuple(float(value) for value in row)


def _is_terminal_or_honor(tile_index: int) -> bool:
    return tile_index >= 27 or tile_index % 9 in {0, 8}


def _context_player_count(
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
) -> int:
    return max(4, seat + 1, len(active_riichi_seats), len(river_counts_by_seat))


def _active_riichi_at(active_riichi_seats: tuple[bool, ...], seat: int) -> bool:
    return seat < len(active_riichi_seats) and active_riichi_seats[seat]


def _sum_river_count(
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    seats: Sequence[int],
    tile_index: int,
) -> int:
    return sum(_river_count(river_counts_by_seat, seat, tile_index) for seat in seats)


def _river_count(
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    seat: int,
    tile_index: int,
) -> int:
    if seat >= len(river_counts_by_seat):
        return 0
    counts = river_counts_by_seat[seat]
    if len(counts) != 34:
        raise ValueError("river count rows must have length 34")
    return counts[tile_index]


def _feature_profile(name: str) -> _FeatureProfile:
    try:
        return _FEATURE_PROFILES[name]
    except KeyError as exc:
        raise ValueError(f"unsupported discard linear feature profile: {name}") from exc


def _feature_profile_for_kind(kind: Any) -> _FeatureProfile | None:
    return _FEATURE_PROFILES_BY_KIND.get(kind)
