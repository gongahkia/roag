from __future__ import annotations

from kenjaku.commands import (
    bc_examples,
    benchmarks,
    bot,
    deal_in,
    defense,
    discard_training,
    external_baselines,
    hand_analysis,
    interpretability,
    placement,
    play,
    predictions,
    replay,
    self_play,
    snapshots,
    status,
    tenhou,
    training,
)

COMMAND_MODULES = (
    status,
    play,
    replay,
    self_play,
    training,
    tenhou,
    bot,
    defense,
    deal_in,
    placement,
    hand_analysis,
    snapshots,
    interpretability,
    predictions,
    external_baselines,
    discard_training,
    bc_examples,
    benchmarks,
)

__all__ = ["COMMAND_MODULES"]
