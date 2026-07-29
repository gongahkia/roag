from __future__ import annotations

from typing import Any

from kenjaku.features.common import (
    FeatureProfile,
    profiles_by_kind,
    require_feature_profile,
    tile_feature_names,
)
from kenjaku.features.common import (
    feature_profile_for_kind as _feature_profile_for_kind,
)

RAW_COUNT_FEATURE_PROFILE = "raw-count"
SHANTEN_FEATURE_PROFILE = "shanten"
RISK_CONTEXT_FEATURE_PROFILE = "risk-context"
DEFENSE_CONTEXT_FEATURE_PROFILE = "defense-context"
DEFENSE_CONTEXT_V1_FEATURE_PROFILE = "defense-context-v1"
RAW_COUNT_FEATURE_DIM = 69
RAW_COUNT_MODEL_KIND = "discard-linear-raw-count-v0"
FEATURE_DIM = 76
MODEL_KIND = "discard-linear-v1"
RISK_CONTEXT_FEATURE_DIM = 86
RISK_CONTEXT_MODEL_KIND = "discard-linear-risk-context-v0"
DEFENSE_CONTEXT_FEATURE_DIM = 98
DEFENSE_CONTEXT_MODEL_KIND = "discard-linear-defense-context-v0"
DEFENSE_CONTEXT_V1_FEATURE_DIM = 112
DEFENSE_CONTEXT_V1_MODEL_KIND = "discard-linear-defense-context-v1"

_TILE_FEATURE_NAMES = tile_feature_names()
_RAW_FEATURE_NAMES = (
    "bias",
    *(f"hand_count_{name}" for name in _TILE_FEATURE_NAMES),
    *(f"visible_count_{name}" for name in _TILE_FEATURE_NAMES),
)
_TILE_EFFICIENCY_FEATURE_NAMES = (
    *_RAW_FEATURE_NAMES,
    "candidate_hand_count",
    "candidate_visible_count",
    "candidate_terminal_or_honor",
    "before_shanten",
    "after_shanten",
    "shanten_delta",
    "shanten_preserved_or_improved",
)
_RISK_CONTEXT_FEATURE_NAMES = (
    "self_riichi_active",
    "active_riichi_opponent_fraction",
    "has_active_riichi_opponent",
    "candidate_active_riichi_river_count",
    "candidate_seen_by_active_riichi",
    "candidate_opponent_river_count",
    "candidate_seen_by_any_opponent",
    "candidate_self_river_count",
    "candidate_all_river_count",
    "candidate_unseen_under_active_riichi",
)
_DEFENSE_CONTEXT_FEATURE_NAMES = (
    "candidate_genbutsu",
    "candidate_suji",
    "candidate_kabe",
    "candidate_one_chance",
    "candidate_seen_after_riichi",
    "candidate_seen_before_riichi",
    "active_riichi_max_elapsed_fraction",
    "active_riichi_min_elapsed_fraction",
    "candidate_basic_safe",
    "candidate_unseen_unsafe_under_riichi",
    "candidate_honor",
    "candidate_terminal",
)
_DEFENSE_CONTEXT_V1_FEATURE_NAMES = (
    "candidate_genbutsu_active_fraction",
    "candidate_suji_active_fraction",
    "candidate_seen_after_active_fraction",
    "candidate_seen_before_active_fraction",
    "candidate_kabe_adjacent_wall_fraction",
    "candidate_one_chance_adjacent_fraction",
    "candidate_sotogawa",
    "candidate_terminal_honor_unseen_under_active_riichi",
    "candidate_unseen_non_safe_under_active_riichi",
    "candidate_dora",
    "candidate_visible_dora_indicator",
    "active_ippatsu_fraction",
    "active_tsumogiri_fraction",
    "opponent_meld_tile_fraction",
)

FEATURE_PROFILES = {
    RAW_COUNT_FEATURE_PROFILE: FeatureProfile(
        name=RAW_COUNT_FEATURE_PROFILE,
        model_kind=RAW_COUNT_MODEL_KIND,
        feature_dim=RAW_COUNT_FEATURE_DIM,
    ),
    SHANTEN_FEATURE_PROFILE: FeatureProfile(
        name=SHANTEN_FEATURE_PROFILE,
        model_kind=MODEL_KIND,
        feature_dim=FEATURE_DIM,
        includes_tile_efficiency=True,
    ),
    RISK_CONTEXT_FEATURE_PROFILE: FeatureProfile(
        name=RISK_CONTEXT_FEATURE_PROFILE,
        model_kind=RISK_CONTEXT_MODEL_KIND,
        feature_dim=RISK_CONTEXT_FEATURE_DIM,
        includes_tile_efficiency=True,
        includes_risk_context=True,
    ),
    DEFENSE_CONTEXT_FEATURE_PROFILE: FeatureProfile(
        name=DEFENSE_CONTEXT_FEATURE_PROFILE,
        model_kind=DEFENSE_CONTEXT_MODEL_KIND,
        feature_dim=DEFENSE_CONTEXT_FEATURE_DIM,
        includes_tile_efficiency=True,
        includes_risk_context=True,
        includes_defense_context=True,
    ),
    DEFENSE_CONTEXT_V1_FEATURE_PROFILE: FeatureProfile(
        name=DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
        model_kind=DEFENSE_CONTEXT_V1_MODEL_KIND,
        feature_dim=DEFENSE_CONTEXT_V1_FEATURE_DIM,
        includes_tile_efficiency=True,
        includes_risk_context=True,
        includes_defense_context=True,
        includes_defense_context_v1=True,
    ),
}
FEATURE_PROFILES_BY_KIND = profiles_by_kind(FEATURE_PROFILES)


def feature_profile(name: str) -> FeatureProfile:
    return require_feature_profile(FEATURE_PROFILES, name, label="discard linear")


def feature_profile_for_kind(kind: Any) -> FeatureProfile | None:
    return _feature_profile_for_kind(FEATURE_PROFILES_BY_KIND, kind)


def feature_names(profile: FeatureProfile) -> tuple[str, ...]:
    if profile.name == RAW_COUNT_FEATURE_PROFILE:
        names = _RAW_FEATURE_NAMES
    else:
        names = _TILE_EFFICIENCY_FEATURE_NAMES
        if profile.includes_risk_context:
            names = (*names, *_RISK_CONTEXT_FEATURE_NAMES)
        if profile.includes_defense_context:
            names = (*names, *_DEFENSE_CONTEXT_FEATURE_NAMES)
        if profile.includes_defense_context_v1:
            names = (*names, *_DEFENSE_CONTEXT_V1_FEATURE_NAMES)
    if len(names) != profile.feature_dim:
        raise ValueError("feature names must match feature dimension")
    return names
