"""Training-data adapters and example builders."""

from importlib import import_module
from typing import TYPE_CHECKING, Any

from kenjaku.training.bc_examples import (
    BC_DECISION_TYPES,
    BC_EXAMPLE_MANIFEST_KIND,
    BC_EXAMPLE_ROW_KIND,
    BcExampleLoad,
    BcExampleShard,
    bc_example_from_payload,
    bc_example_to_payload,
    build_bc_manifest,
    parse_bc_decision_types,
    read_bc_examples,
    write_bc_example_row,
    write_bc_manifest,
)
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
    actual_discard_is_tsumogiri,
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
from kenjaku.training.kita_examples import KitaExample, iter_kita_examples
from kenjaku.training.outcomes import RoundOutcome, round_outcome, round_outcome_payload
from kenjaku.training.riichi_examples import RiichiExample, iter_riichi_examples
from kenjaku.training.splits import deterministic_split

if TYPE_CHECKING:
    from kenjaku.training.population import (
        POPULATION_SANDBOX_REPORT_KIND,
        POPULATION_SANDBOX_SNAPSHOT_KIND,
        format_population_sandbox_report,
        train_population_sandbox,
    )
    from kenjaku.training.ppo import (
        PPO_ACTION_DIM,
        PPO_SANDBOX_CHECKPOINT_KIND,
        PPO_SANDBOX_POLICY_KIND,
        PPO_SANDBOX_REPORT_KIND,
        PPO_STATE_DIM,
        SandboxLinearPpoActorCritic,
        SandboxPpoRollout,
        SandboxPpoTrainingResult,
        SandboxPpoTransition,
        collect_ppo_sandbox_rollout,
        evaluate_ppo_sandbox_policy,
        format_ppo_sandbox_report,
        load_ppo_sandbox_checkpoint,
        ppo_action_index,
        ppo_legal_action_mask,
        ppo_state_features,
        save_ppo_sandbox_checkpoint,
        train_ppo_sandbox,
    )

_LAZY_EXPORT_MODULES = {
    "POPULATION_SANDBOX_REPORT_KIND": "kenjaku.training.population",
    "POPULATION_SANDBOX_SNAPSHOT_KIND": "kenjaku.training.population",
    "format_population_sandbox_report": "kenjaku.training.population",
    "train_population_sandbox": "kenjaku.training.population",
    "PPO_ACTION_DIM": "kenjaku.training.ppo",
    "PPO_SANDBOX_CHECKPOINT_KIND": "kenjaku.training.ppo",
    "PPO_SANDBOX_POLICY_KIND": "kenjaku.training.ppo",
    "PPO_SANDBOX_REPORT_KIND": "kenjaku.training.ppo",
    "PPO_STATE_DIM": "kenjaku.training.ppo",
    "SandboxLinearPpoActorCritic": "kenjaku.training.ppo",
    "SandboxPpoRollout": "kenjaku.training.ppo",
    "SandboxPpoTrainingResult": "kenjaku.training.ppo",
    "SandboxPpoTransition": "kenjaku.training.ppo",
    "collect_ppo_sandbox_rollout": "kenjaku.training.ppo",
    "evaluate_ppo_sandbox_policy": "kenjaku.training.ppo",
    "format_ppo_sandbox_report": "kenjaku.training.ppo",
    "load_ppo_sandbox_checkpoint": "kenjaku.training.ppo",
    "ppo_action_index": "kenjaku.training.ppo",
    "ppo_legal_action_mask": "kenjaku.training.ppo",
    "ppo_state_features": "kenjaku.training.ppo",
    "save_ppo_sandbox_checkpoint": "kenjaku.training.ppo",
    "train_ppo_sandbox": "kenjaku.training.ppo",
}


def __getattr__(name: str) -> Any:
    module_name = _LAZY_EXPORT_MODULES.get(name)
    if module_name is None:
        raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
    value = getattr(import_module(module_name), name)
    globals()[name] = value
    return value

__all__ = [
    "BC_DECISION_TYPES",
    "BC_EXAMPLE_MANIFEST_KIND",
    "BC_EXAMPLE_ROW_KIND",
    "BcExampleLoad",
    "BcExampleShard",
    "CallExample",
    "collect_ppo_sandbox_rollout",
    "actual_discard_has_kabe",
    "actual_discard_has_one_chance",
    "actual_discard_has_suji",
    "actual_discard_has_sotogawa",
    "actual_discard_is_genbutsu",
    "actual_discard_is_tsumogiri",
    "actual_discard_seen_after_riichi",
    "actual_discard_seen_before_riichi",
    "active_riichi_opponents",
    "bc_example_from_payload",
    "bc_example_to_payload",
    "build_bc_manifest",
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
    "evaluate_ppo_sandbox_policy",
    "format_ppo_sandbox_report",
    "format_population_sandbox_report",
    "has_active_riichi_opponent",
    "iter_call_examples",
    "iter_deal_in_examples",
    "iter_discard_examples",
    "iter_kita_examples",
    "iter_riichi_examples",
    "KitaExample",
    "parse_bc_decision_types",
    "legal_candidate_defense_risks",
    "load_ppo_sandbox_checkpoint",
    "max_active_riichi_discards_elapsed",
    "min_active_riichi_discards_elapsed",
    "PPO_ACTION_DIM",
    "POPULATION_SANDBOX_REPORT_KIND",
    "POPULATION_SANDBOX_SNAPSHOT_KIND",
    "PPO_SANDBOX_CHECKPOINT_KIND",
    "PPO_SANDBOX_POLICY_KIND",
    "PPO_SANDBOX_REPORT_KIND",
    "PPO_STATE_DIM",
    "ppo_action_index",
    "ppo_legal_action_mask",
    "ppo_state_features",
    "read_bc_examples",
    "RiichiExample",
    "RoundOutcome",
    "SandboxLinearPpoActorCritic",
    "SandboxPpoRollout",
    "SandboxPpoTrainingResult",
    "SandboxPpoTransition",
    "round_outcome",
    "round_outcome_payload",
    "save_ppo_sandbox_checkpoint",
    "summarize_discard_predictions",
    "summarize_discard_shanten",
    "summarize_defense_risk_outcomes",
    "summarize_defense_risks",
    "summarize_deal_in_examples",
    "train_ppo_sandbox",
    "train_population_sandbox",
    "write_bc_example_row",
    "write_bc_manifest",
]
