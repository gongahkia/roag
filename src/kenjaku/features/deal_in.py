from __future__ import annotations

DEAL_IN_LINEAR_MODEL_KIND = "deal-in-linear-v0"
DEAL_IN_LINEAR_FEATURE_NAMES = (
    "bias",
    "heuristic_defense_risk",
    "active_riichi_opponent_fraction",
    "has_active_riichi_opponent",
    "candidate_genbutsu",
    "candidate_suji",
    "candidate_kabe",
    "candidate_one_chance",
    "candidate_sotogawa",
    "candidate_seen_after_riichi",
    "candidate_seen_before_riichi",
    "candidate_visible_count",
    "candidate_unseen_count",
    "candidate_hand_count",
    "discard_is_tsumogiri",
    "candidate_terminal_or_honor",
    "candidate_honor",
    "candidate_terminal",
    "active_riichi_min_elapsed_fraction",
    "active_riichi_max_elapsed_fraction",
    "active_ippatsu_fraction",
    "seat_turn_fraction",
    "seat_is_dealer",
    "seat_score_fraction",
    "safety_reason_fraction",
    "danger_reason_fraction",
)
DEAL_IN_LINEAR_FEATURE_DIM = len(DEAL_IN_LINEAR_FEATURE_NAMES)
