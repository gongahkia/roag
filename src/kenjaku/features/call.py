from __future__ import annotations

from typing import Any

from kenjaku.core import ActionKind
from kenjaku.features.common import (
    FeatureProfile,
    profiles_by_kind,
    require_feature_profile,
    tile_feature_names,
)
from kenjaku.features.common import (
    feature_profile_for_kind as _feature_profile_for_kind,
)

CALL_DECISION_KINDS = (
    ActionKind.PASS,
    ActionKind.CHI,
    ActionKind.PON,
    ActionKind.MINKAN,
)
CALL_LINEAR_V0_FEATURE_PROFILE = "v0"
CALL_LINEAR_V1_FEATURE_PROFILE = "v1"
CALL_LINEAR_MODEL_KIND = "call-linear-v0"
CALL_LINEAR_V1_MODEL_KIND = "call-linear-v1"

_NON_PASS_CALL_KINDS = (ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN)
_TILE_FEATURE_NAMES = tile_feature_names()
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

FEATURE_PROFILES = {
    CALL_LINEAR_V0_FEATURE_PROFILE: FeatureProfile(
        name=CALL_LINEAR_V0_FEATURE_PROFILE,
        model_kind=CALL_LINEAR_MODEL_KIND,
        feature_dim=CALL_LINEAR_FEATURE_DIM,
        feature_names=CALL_LINEAR_FEATURE_NAMES,
    ),
    CALL_LINEAR_V1_FEATURE_PROFILE: FeatureProfile(
        name=CALL_LINEAR_V1_FEATURE_PROFILE,
        model_kind=CALL_LINEAR_V1_MODEL_KIND,
        feature_dim=CALL_LINEAR_V1_FEATURE_DIM,
        feature_names=CALL_LINEAR_V1_FEATURE_NAMES,
    ),
}
FEATURE_PROFILES_BY_KIND = profiles_by_kind(FEATURE_PROFILES)


def feature_profile(name: str) -> FeatureProfile:
    return require_feature_profile(FEATURE_PROFILES, name, label="call linear")


def feature_profile_for_kind(kind: Any) -> FeatureProfile | None:
    return _feature_profile_for_kind(FEATURE_PROFILES_BY_KIND, kind)
