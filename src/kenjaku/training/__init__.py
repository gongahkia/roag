"""Training-data adapters and example builders."""

from kenjaku.training.call_examples import CallExample, iter_call_examples
from kenjaku.training.discard_examples import DiscardExample, iter_discard_examples
from kenjaku.training.discard_features import (
    DiscardShantenDelta,
    discard_shanten_delta,
    summarize_discard_shanten,
)
from kenjaku.training.defense_features import (
    actual_discard_has_kabe,
    actual_discard_has_suji,
    actual_discard_is_genbutsu,
    active_riichi_opponents,
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
from kenjaku.training.error_analysis import summarize_discard_predictions
from kenjaku.training.splits import deterministic_split

__all__ = [
    "CallExample",
    "actual_discard_has_kabe",
    "actual_discard_has_suji",
    "actual_discard_is_genbutsu",
    "active_riichi_opponents",
    "candidate_has_kabe",
    "candidate_has_one_chance",
    "candidate_has_suji",
    "candidate_is_genbutsu",
    "candidate_seen_after_riichi",
    "candidate_seen_before_riichi",
    "DiscardExample",
    "DiscardShantenDelta",
    "deterministic_split",
    "discard_shanten_delta",
    "has_active_riichi_opponent",
    "iter_call_examples",
    "iter_discard_examples",
    "max_active_riichi_discards_elapsed",
    "min_active_riichi_discards_elapsed",
    "summarize_discard_predictions",
    "summarize_discard_shanten",
]
