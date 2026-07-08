from __future__ import annotations

from kenjaku.core import ActionKind
from kenjaku.features.common import tile_feature_names

RIICHI_DECISION_KINDS = (ActionKind.PASS, ActionKind.RIICHI)
RIICHI_LINEAR_MODEL_KIND = "riichi-linear-v0"
_TILE_FEATURE_NAMES = tile_feature_names()
RIICHI_LINEAR_FEATURE_NAMES = (
    "bias",
    *(f"candidate_{kind.value}" for kind in RIICHI_DECISION_KINDS),
    *(f"hand_count_{name}" for name in _TILE_FEATURE_NAMES),
    *(f"visible_count_{name}" for name in _TILE_FEATURE_NAMES),
    "seat_score_fraction",
    "seat_has_riichi_stick_points",
    "score_delta_to_top",
    "score_rank_fraction",
    "seat_is_dealer",
    "seat_turn_fraction",
    "own_riichi_active",
    "active_riichi_opponent_fraction",
    "has_active_riichi_opponent",
    "self_river_count_fraction",
    "opponent_river_count_fraction",
    "all_river_count_fraction",
    "closed_shanten_proxy",
    "closed_tenpai_proxy",
    "terminal_honor_count",
    "pair_count",
    "triplet_count",
    "unique_tile_count",
)
RIICHI_LINEAR_FEATURE_DIM = len(RIICHI_LINEAR_FEATURE_NAMES)
