"""Training-data adapters and example builders."""

from kenjaku.training.call_examples import CallExample, iter_call_examples
from kenjaku.training.deal_in import (
    DEAL_IN_LABEL_SOURCE,
    DealInExample,
    iter_deal_in_examples,
    summarize_deal_in_examples,
)
from kenjaku.training.defense_features import (
    active_riichi_opponents,
    actual_discard_has_kabe,
    actual_discard_has_one_chance,
    actual_discard_has_sotogawa,
    actual_discard_has_suji,
    actual_discard_is_genbutsu,
    actual_discard_seen_after_riichi,
    actual_discard_seen_before_riichi,
    candidate_has_kabe,
    candidate_has_one_chance,
    candidate_has_sotogawa,
    candidate_has_suji,
    candidate_is_genbutsu,
    candidate_seen_after_riichi,
    candidate_seen_before_riichi,
    has_active_riichi_opponent,
    max_active_riichi_discards_elapsed,
    min_active_riichi_discards_elapsed,
)
from kenjaku.training.defense_risk import (
    DefenseRiskScore,
    candidate_defense_risk,
    legal_candidate_defense_risks,
    summarize_defense_risk_outcomes,
    summarize_defense_risks,
)
from kenjaku.training.discard_examples import DiscardExample, iter_discard_examples
from kenjaku.training.discard_features import (
    DiscardShantenDelta,
    discard_shanten_delta,
    summarize_discard_shanten,
)
from kenjaku.training.error_analysis import summarize_discard_predictions
from kenjaku.training.outcomes import RoundOutcome, round_outcome, round_outcome_payload
from kenjaku.training.riichi_examples import RiichiExample, iter_riichi_examples
from kenjaku.training.splits import deterministic_split

__all__ = [
    "CallExample",
    "actual_discard_has_kabe",
    "actual_discard_has_one_chance",
    "actual_discard_has_suji",
    "actual_discard_has_sotogawa",
    "actual_discard_is_genbutsu",
    "actual_discard_seen_after_riichi",
    "actual_discard_seen_before_riichi",
    "active_riichi_opponents",
    "candidate_has_kabe",
    "candidate_has_one_chance",
    "candidate_has_suji",
    "candidate_has_sotogawa",
    "candidate_is_genbutsu",
    "candidate_seen_after_riichi",
    "candidate_seen_before_riichi",
    "candidate_defense_risk",
    "DEAL_IN_LABEL_SOURCE",
    "DefenseRiskScore",
    "DiscardExample",
    "DiscardShantenDelta",
    "DealInExample",
    "deterministic_split",
    "discard_shanten_delta",
    "has_active_riichi_opponent",
    "iter_call_examples",
    "iter_deal_in_examples",
    "iter_discard_examples",
    "iter_riichi_examples",
    "legal_candidate_defense_risks",
    "max_active_riichi_discards_elapsed",
    "min_active_riichi_discards_elapsed",
    "RiichiExample",
    "RoundOutcome",
    "round_outcome",
    "round_outcome_payload",
    "summarize_discard_predictions",
    "summarize_discard_shanten",
    "summarize_defense_risk_outcomes",
    "summarize_defense_risks",
    "summarize_deal_in_examples",
]
