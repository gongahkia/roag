from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from typing import Any

from kenjaku.core import Tile, TileType, all_tile_types
from kenjaku.features import discard as discard_features
from kenjaku.features.discard import (
    feature_names as _feature_names,
)
from kenjaku.features.discard import (
    feature_profile as _feature_profile,
)
from kenjaku.features.discard import (
    feature_profile_for_kind as _feature_profile_for_kind,
)
from kenjaku.models._linear_base import (
    LinearModel,
    feature_summary_payload,
    fit_softmax_sgd,
    logits_for_candidates,
    model_payload,
    numeric_summary,
    parse_weight_matrix,
    require_features_by_label,
    weight_matrix_payload,
)
from kenjaku.models._linear_discard_features import (
    feature_vectors as _feature_vectors,
)
from kenjaku.models._linear_discard_features import (
    legal_indices as _legal_indices,
)
from kenjaku.models._linear_discard_features import (
    prepare_examples as _prepare_examples,
)
from kenjaku.training import DiscardExample

DEFENSE_CONTEXT_FEATURE_DIM = discard_features.DEFENSE_CONTEXT_FEATURE_DIM
DEFENSE_CONTEXT_FEATURE_PROFILE = discard_features.DEFENSE_CONTEXT_FEATURE_PROFILE
DEFENSE_CONTEXT_MODEL_KIND = discard_features.DEFENSE_CONTEXT_MODEL_KIND
DEFENSE_CONTEXT_V1_FEATURE_DIM = discard_features.DEFENSE_CONTEXT_V1_FEATURE_DIM
DEFENSE_CONTEXT_V1_FEATURE_PROFILE = discard_features.DEFENSE_CONTEXT_V1_FEATURE_PROFILE
DEFENSE_CONTEXT_V1_MODEL_KIND = discard_features.DEFENSE_CONTEXT_V1_MODEL_KIND
FEATURE_DIM = discard_features.FEATURE_DIM
MODEL_KIND = discard_features.MODEL_KIND
RAW_COUNT_FEATURE_DIM = discard_features.RAW_COUNT_FEATURE_DIM
RAW_COUNT_FEATURE_PROFILE = discard_features.RAW_COUNT_FEATURE_PROFILE
RAW_COUNT_MODEL_KIND = discard_features.RAW_COUNT_MODEL_KIND
RISK_CONTEXT_FEATURE_DIM = discard_features.RISK_CONTEXT_FEATURE_DIM
RISK_CONTEXT_FEATURE_PROFILE = discard_features.RISK_CONTEXT_FEATURE_PROFILE
RISK_CONTEXT_MODEL_KIND = discard_features.RISK_CONTEXT_MODEL_KIND
SHANTEN_FEATURE_PROFILE = discard_features.SHANTEN_FEATURE_PROFILE


@dataclass(frozen=True, slots=True)
class DiscardLinearModel(LinearModel):
    """Tiny dependency-free softmax model for discard prediction."""

    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float
    feature_profile: str = SHANTEN_FEATURE_PROFILE
    l2: float = 0.0

    def __post_init__(self) -> None:
        profile = _feature_profile(self.feature_profile)
        if len(self.weights) != 34:
            raise ValueError("discard model must have 34 output rows")
        if any(len(row) != profile.feature_dim for row in self.weights):
            raise ValueError(f"discard model rows must have {profile.feature_dim} features")
        if self.l2 < 0:
            raise ValueError("l2 must be non-negative")

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
        prepared_examples = _prepare_examples(examples, profile=profile)
        weights = fit_softmax_sgd(
            prepared_examples,
            output_count=34,
            feature_dim=profile.feature_dim,
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            index_of=_tile_index,
        )

        return cls(
            weights=weights,
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
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
        meld_counts_by_seat: tuple[tuple[int, ...], ...] = (),
        dora_indicators: tuple[Tile, ...] = (),
        last_discard_tsumogiri_by_seat: tuple[bool | None, ...] = (),
        ippatsu_active_seats: tuple[bool, ...] = (),
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
            meld_counts_by_seat=meld_counts_by_seat,
            dora_indicators=dora_indicators,
            last_discard_tsumogiri_by_seat=last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=ippatsu_active_seats,
        )
        logits = logits_for_candidates(self.weights, features_by_tile, index_of=_tile_index)
        return TileType(max(logits, key=lambda tile_index: logits[tile_index]))

    def logits(
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
        meld_counts_by_seat: tuple[tuple[int, ...], ...] = (),
        dora_indicators: tuple[Tile, ...] = (),
        last_discard_tsumogiri_by_seat: tuple[bool | None, ...] = (),
        ippatsu_active_seats: tuple[bool, ...] = (),
    ) -> dict[TileType, float]:
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
            meld_counts_by_seat=meld_counts_by_seat,
            dora_indicators=dora_indicators,
            last_discard_tsumogiri_by_seat=last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=ippatsu_active_seats,
        )
        return {
            TileType(tile_index): logit
            for tile_index, logit in logits_for_candidates(
                self.weights,
                features_by_tile,
                index_of=_tile_index,
            ).items()
        }

    def logits_for_example(self, example: DiscardExample) -> dict[TileType, float]:
        return self.logits(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
            rivers_by_seat=example.rivers_by_seat,
            riichi_declared_turns=example.riichi_declared_turns,
            riichi_declared_event_indices=example.riichi_declared_event_indices,
            meld_counts_by_seat=example.meld_counts_by_seat,
            dora_indicators=example.dora_indicators,
            last_discard_tsumogiri_by_seat=example.last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=example.ippatsu_active_seats,
        )

    def score(self, examples: Sequence[DiscardExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        prepared_examples = _prepare_examples(
            examples,
            profile=_feature_profile(self.feature_profile),
        )
        correct = 0
        for example in prepared_examples:
            features_by_tile = require_features_by_label(example)
            logits = logits_for_candidates(self.weights, features_by_tile, index_of=_tile_index)
            correct += max(logits, key=lambda tile_index: logits[tile_index]) == example.label
        return correct / len(examples)

    @property
    def kind(self) -> str:
        return _feature_profile(self.feature_profile).model_kind

    @property
    def feature_dim(self) -> int:
        return _feature_profile(self.feature_profile).feature_dim

    @property
    def feature_names(self) -> tuple[str, ...]:
        return _feature_names(_feature_profile(self.feature_profile))

    @staticmethod
    def feature_names_for_profile(feature_profile: str) -> tuple[str, ...]:
        return _feature_names(_feature_profile(feature_profile))

    def weight_summary(self) -> dict[str, Any]:
        names = self.feature_names
        return {
            "feature_count": self.feature_dim,
            "features": [
                {
                    "index": index,
                    "name": name,
                    **numeric_summary(
                        (row[index] for row in self.weights),
                        include_max_abs=True,
                    ),
                }
                for index, name in enumerate(names)
            ],
            "overall": numeric_summary(
                (weight for row in self.weights for weight in row),
                include_max_abs=True,
            ),
            "outputs": [
                {
                    "index": tile_type.index,
                    "tile": tile_type.notation,
                    **numeric_summary(self.weights[tile_type.index], include_max_abs=True),
                }
                for tile_type in all_tile_types()
            ],
        }

    def feature_summary(self, examples: Sequence[DiscardExample]) -> dict[str, Any]:
        profile = _feature_profile(self.feature_profile)
        prepared_examples = _prepare_examples(examples, profile=profile)
        names = _feature_names(profile)
        return feature_summary_payload(
            example_count=len(examples),
            feature_names=names,
            vectors=(
                features
                for example in prepared_examples
                for features in require_features_by_label(example).values()
            ),
            candidate_vectors_key="candidate_vectors",
            include_overall=True,
        )

    def to_dict(self) -> dict[str, Any]:
        return model_payload(
            kind=self.kind,
            feature_profile=self.feature_profile,
            feature_dim=self.feature_dim,
            epochs=self.epochs,
            learning_rate=self.learning_rate,
            l2=self.l2,
            weights=weight_matrix_payload(self.weights),
        )

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> DiscardLinearModel:
        profile = _feature_profile_for_kind(payload.get("kind"))
        if profile is None:
            raise ValueError("unsupported discard linear model kind")
        if payload.get("feature_dim") != profile.feature_dim:
            raise ValueError("unsupported discard linear model feature dimension")
        if payload.get("feature_profile", profile.name) != profile.name:
            raise ValueError("discard linear model kind/profile mismatch")

        return cls(
            weights=parse_weight_matrix(payload.get("weights")),
            epochs=int(payload["epochs"]),
            learning_rate=float(payload["learning_rate"]),
            l2=float(payload.get("l2", 0.0)),
            feature_profile=profile.name,
        )

def _tile_index(tile_index: int) -> int:
    return tile_index
