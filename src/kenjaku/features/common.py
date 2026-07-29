from __future__ import annotations

from collections.abc import Mapping
from dataclasses import dataclass
from typing import Any

from kenjaku.core import all_tile_types


@dataclass(frozen=True, slots=True)
class FeatureProfile:
    name: str
    model_kind: str
    feature_dim: int
    feature_names: tuple[str, ...] = ()
    includes_tile_efficiency: bool = False
    includes_risk_context: bool = False
    includes_defense_context: bool = False
    includes_defense_context_v1: bool = False


def tile_feature_names() -> tuple[str, ...]:
    return tuple(tile_type.notation for tile_type in all_tile_types())


def profiles_by_kind(
    profiles: Mapping[str, FeatureProfile],
) -> dict[str, FeatureProfile]:
    return {profile.model_kind: profile for profile in profiles.values()}


def require_feature_profile(
    profiles: Mapping[str, FeatureProfile],
    name: str,
    *,
    label: str,
) -> FeatureProfile:
    try:
        return profiles[name]
    except KeyError as exc:
        raise ValueError(f"unsupported {label} feature profile: {name}") from exc


def feature_profile_for_kind(
    profiles: Mapping[str, FeatureProfile],
    kind: Any,
) -> FeatureProfile | None:
    return profiles.get(kind)
