"""Training-data adapters and example builders."""

from kenjaku.training.call_examples import CallExample, iter_call_examples
from kenjaku.training.discard_examples import DiscardExample, iter_discard_examples
from kenjaku.training.discard_features import (
    DiscardShantenDelta,
    discard_shanten_delta,
    summarize_discard_shanten,
)
from kenjaku.training.splits import deterministic_split

__all__ = [
    "CallExample",
    "DiscardExample",
    "DiscardShantenDelta",
    "deterministic_split",
    "discard_shanten_delta",
    "iter_call_examples",
    "iter_discard_examples",
    "summarize_discard_shanten",
]
