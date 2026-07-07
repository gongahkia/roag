from __future__ import annotations

import json
import os
from collections import Counter
from collections.abc import Sequence
from html import escape
from pathlib import Path
from typing import Any
from urllib.parse import quote

from kenjaku.io import TenhouGame, TenhouParseFailure
from kenjaku.training.history import normalize_training_history

CALL_BENCHMARK_REPORT_KIND = "kenjaku-call-benchmark-report-v0"
BENCHMARK_SUMMARY_KIND = "kenjaku-benchmark-summary-v0"
DEAL_IN_BENCHMARK_REPORT_KIND = "kenjaku-deal-in-benchmark-report-v0"
DISCARD_BENCHMARK_REPORT_KIND = "kenjaku-discard-benchmark-report-v0"
DISCARD_BENCHMARK_SUMMARY_KIND = "kenjaku-discard-benchmark-summary-v0"
DISCARD_DISAGREEMENT_REPORT_KIND = "kenjaku-discard-disagreements-v0"
DISCARD_DISAGREEMENT_SUMMARY_KIND = "kenjaku-discard-disagreement-summary-v0"
DISCARD_LINEAR_REPORT_KIND = "kenjaku-discard-linear-report-v0"
DISCARD_MLP_BENCHMARK_REPORT_KIND = "kenjaku-discard-mlp-benchmark-report-v0"
DISCARD_MLP_REPORT_KIND = "kenjaku-discard-mlp-report-v0"
DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND = (
    "kenjaku-discard-transformer-benchmark-report-v0"
)
DISCARD_TRANSFORMER_REPORT_KIND = "kenjaku-discard-transformer-report-v0"
PUBLIC_BENCHMARK_DASHBOARD_KIND = "kenjaku-public-benchmark-dashboard-v0"
RIICHI_BENCHMARK_REPORT_KIND = "kenjaku-riichi-benchmark-report-v0"
TENHOU_INSPECT_REPORT_KIND = "kenjaku-tenhou-inspect-report-v0"
DISCARD_MLP_BENCHMARK_MODEL_ORDER = (
    "frequency",
    "risk_context_linear",
    "defense_context_linear",
    "discard_mlp",
)
DISCARD_TRANSFORMER_BENCHMARK_MODEL_ORDER = (
    "frequency",
    "risk_context_linear",
    "defense_context_linear",
    "discard_transformer",
)
DISCARD_BENCHMARK_MODEL_ORDER = (
    "frequency",
    "raw_count_linear",
    "linear",
    "risk_context_linear",
    "defense_context_linear",
    "defense_context_v1_linear",
)
DISCARD_BENCHMARK_BUCKETS = {
    "active_riichi_yes": ("by_active_opponent_riichi", "yes"),
    "actual_genbutsu_yes": ("by_actual_discard_genbutsu", "yes"),
    "actual_suji_yes": ("by_actual_discard_suji", "yes"),
    "actual_kabe_yes": ("by_actual_discard_kabe", "yes"),
    "actual_one_chance_yes": ("by_actual_discard_one_chance", "yes"),
    "seen_before_riichi_yes": ("by_actual_discard_seen_before_riichi", "yes"),
    "seen_after_riichi_yes": ("by_actual_discard_seen_after_riichi", "yes"),
    "shanten_worsened": ("by_shanten_delta", "worsened"),
}
DISCARD_BENCHMARK_BUCKET_MODELS = (
    "risk_context_linear",
    "defense_context_linear",
    "defense_context_v1_linear",
)
PUBLIC_BENCHMARK_METRIC_DEFINITIONS = (
    {
        "name": "accuracy",
        "definition": "Correct predictions divided by evaluated examples.",
    },
    {
        "name": "balanced_accuracy",
        "definition": "Mean recall across positive and negative classes for imbalanced labels.",
    },
    {
        "name": "recall",
        "definition": "Share of true target examples recovered by the model or policy.",
    },
    {
        "name": "pass_recall",
        "definition": (
            "Share of true pass or non-action examples recovered by call/riichi policies."
        ),
    },
    {
        "name": "brier_score",
        "definition": "Mean squared error of probability predictions; lower is better.",
    },
    {
        "name": "log_loss",
        "definition": (
            "Negative log likelihood of labels under predicted probabilities; lower is better."
        ),
    },
    {
        "name": "eval_loss",
        "definition": "Held-out training loss reported by neural benchmark runs; lower is better.",
    },
)


def build_tenhou_inspect_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
) -> dict[str, Any]:
    return {
        "kind": TENHOU_INSPECT_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def build_discard_linear_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    model_kind: str,
    feature_dim: int,
    epochs: int,
    learning_rate: float,
    l2: float,
    train_accuracy: float,
    eval_accuracy: float | None,
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    model_path: Path | None,
    source: dict[str, str | None],
) -> dict[str, Any]:
    return {
        "kind": DISCARD_LINEAR_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "model": {
            "kind": model_kind,
            "feature_dim": feature_dim,
        },
        "training": {
            "epochs": epochs,
            "learning_rate": learning_rate,
            "l2": l2,
        },
        "metrics": {
            "train_accuracy": train_accuracy,
            "eval_accuracy": eval_accuracy,
        },
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
        "artifacts": {
            "model_path": None if model_path is None else str(model_path),
        },
    }


def build_discard_mlp_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    model_kind: str,
    input_dim: int,
    hidden_dim: int,
    output_dim: int,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    device: str,
    seed: int,
    train_metrics: dict[str, int | float | None],
    eval_metrics: dict[str, int | float | None],
    history: list[dict[str, Any]],
    best_epoch: int,
    selection_split: str,
    best_metrics: dict[str, dict[str, int | float | None]],
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    checkpoint_path: Path | None,
    source: dict[str, str | None],
) -> dict[str, Any]:
    return {
        "kind": DISCARD_MLP_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "model": {
            "kind": model_kind,
            "input_dim": input_dim,
            "hidden_dim": hidden_dim,
            "output_dim": output_dim,
        },
        "training": {
            "epochs": epochs,
            "batch_size": batch_size,
            "learning_rate": learning_rate,
            "device": device,
            "seed": seed,
            "history": history,
            "best_epoch": best_epoch,
            "selection_split": selection_split,
        },
        "metrics": {
            "train": train_metrics,
            "eval": eval_metrics,
            "best": best_metrics,
        },
        "training_history": normalize_training_history(
            history,
            step_key="epoch",
            step_unit="epoch",
            metric_roots=("metrics",),
        ),
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
        "artifacts": {
            "checkpoint_path": None if checkpoint_path is None else str(checkpoint_path),
        },
    }


def build_discard_mlp_benchmark_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    models: dict[str, dict[str, Any]],
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    checkpoint_path: Path | None,
    source: dict[str, str | None],
) -> dict[str, Any]:
    mlp_eval = _nested_model_metric(models, "discard_mlp", "metrics", "best", "eval", "accuracy")
    if mlp_eval is None:
        mlp_eval = _nested_model_metric(models, "discard_mlp", "metrics", "eval", "accuracy")
    return {
        "kind": DISCARD_MLP_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "models": models,
        "deltas": {
            "mlp_eval_accuracy_lift_over_frequency": _optional_delta(
                mlp_eval,
                _model_metric(models, "frequency", "eval_accuracy"),
            ),
            "mlp_eval_accuracy_lift_over_risk_context": _optional_delta(
                mlp_eval,
                _model_metric(models, "risk_context_linear", "eval_accuracy"),
            ),
            "mlp_eval_accuracy_lift_over_defense_context": _optional_delta(
                mlp_eval,
                _model_metric(models, "defense_context_linear", "eval_accuracy"),
            ),
        },
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
        "artifacts": {
            "checkpoint_path": None if checkpoint_path is None else str(checkpoint_path),
        },
    }


def build_discard_benchmark_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    frequency_train_accuracy: float,
    frequency_eval_accuracy: float | None,
    frequency_eval_analysis: dict[str, Any],
    raw_count_linear_epochs: int,
    raw_count_linear_learning_rate: float,
    raw_count_linear_model_kind: str,
    raw_count_linear_feature_dim: int,
    raw_count_linear_train_accuracy: float,
    raw_count_linear_eval_accuracy: float | None,
    raw_count_linear_eval_analysis: dict[str, Any],
    linear_epochs: int,
    linear_learning_rate: float,
    linear_model_kind: str,
    linear_feature_dim: int,
    linear_train_accuracy: float,
    linear_eval_accuracy: float | None,
    linear_eval_analysis: dict[str, Any],
    risk_context_linear_epochs: int,
    risk_context_linear_learning_rate: float,
    risk_context_linear_model_kind: str,
    risk_context_linear_feature_dim: int,
    risk_context_linear_train_accuracy: float,
    risk_context_linear_eval_accuracy: float | None,
    risk_context_linear_eval_analysis: dict[str, Any],
    defense_context_linear_epochs: int,
    defense_context_linear_learning_rate: float,
    defense_context_linear_model_kind: str,
    defense_context_linear_feature_dim: int,
    defense_context_linear_train_accuracy: float,
    defense_context_linear_eval_accuracy: float | None,
    defense_context_linear_eval_analysis: dict[str, Any],
    defense_context_v1_linear_epochs: int,
    defense_context_v1_linear_learning_rate: float,
    defense_context_v1_linear_model_kind: str,
    defense_context_v1_linear_feature_dim: int,
    defense_context_v1_linear_train_accuracy: float,
    defense_context_v1_linear_eval_accuracy: float | None,
    defense_context_v1_linear_eval_analysis: dict[str, Any],
    linear_l2: float,
    model_diagnostics: dict[str, dict[str, Any]] | None = None,
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
) -> dict[str, Any]:
    report = {
        "kind": DISCARD_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "models": {
            "frequency": {
                "metrics": {
                    "train_accuracy": frequency_train_accuracy,
                    "eval_accuracy": frequency_eval_accuracy,
                },
                "eval_analysis": frequency_eval_analysis,
            },
            "raw_count_linear": {
                "kind": raw_count_linear_model_kind,
                "feature_dim": raw_count_linear_feature_dim,
                "training": {
                    "epochs": raw_count_linear_epochs,
                    "learning_rate": raw_count_linear_learning_rate,
                    "l2": linear_l2,
                },
                "metrics": {
                    "train_accuracy": raw_count_linear_train_accuracy,
                    "eval_accuracy": raw_count_linear_eval_accuracy,
                },
                "eval_analysis": raw_count_linear_eval_analysis,
            },
            "linear": {
                "kind": linear_model_kind,
                "feature_dim": linear_feature_dim,
                "training": {
                    "epochs": linear_epochs,
                    "learning_rate": linear_learning_rate,
                    "l2": linear_l2,
                },
                "metrics": {
                    "train_accuracy": linear_train_accuracy,
                    "eval_accuracy": linear_eval_accuracy,
                },
                "eval_analysis": linear_eval_analysis,
            },
            "risk_context_linear": {
                "kind": risk_context_linear_model_kind,
                "feature_dim": risk_context_linear_feature_dim,
                "training": {
                    "epochs": risk_context_linear_epochs,
                    "learning_rate": risk_context_linear_learning_rate,
                    "l2": linear_l2,
                },
                "metrics": {
                    "train_accuracy": risk_context_linear_train_accuracy,
                    "eval_accuracy": risk_context_linear_eval_accuracy,
                },
                "eval_analysis": risk_context_linear_eval_analysis,
            },
            "defense_context_linear": {
                "kind": defense_context_linear_model_kind,
                "feature_dim": defense_context_linear_feature_dim,
                "training": {
                    "epochs": defense_context_linear_epochs,
                    "learning_rate": defense_context_linear_learning_rate,
                    "l2": linear_l2,
                },
                "metrics": {
                    "train_accuracy": defense_context_linear_train_accuracy,
                    "eval_accuracy": defense_context_linear_eval_accuracy,
                },
                "eval_analysis": defense_context_linear_eval_analysis,
            },
            "defense_context_v1_linear": {
                "kind": defense_context_v1_linear_model_kind,
                "feature_dim": defense_context_v1_linear_feature_dim,
                "training": {
                    "epochs": defense_context_v1_linear_epochs,
                    "learning_rate": defense_context_v1_linear_learning_rate,
                    "l2": linear_l2,
                },
                "metrics": {
                    "train_accuracy": defense_context_v1_linear_train_accuracy,
                    "eval_accuracy": defense_context_v1_linear_eval_accuracy,
                },
                "eval_analysis": defense_context_v1_linear_eval_analysis,
            },
        },
        "ablation": {
            "train_accuracy_lift_over_raw_count": (
                linear_train_accuracy - raw_count_linear_train_accuracy
            ),
            "eval_accuracy_lift_over_raw_count": _optional_delta(
                linear_eval_accuracy,
                raw_count_linear_eval_accuracy,
            ),
            "risk_context_train_accuracy_lift_over_linear": (
                risk_context_linear_train_accuracy - linear_train_accuracy
            ),
            "risk_context_eval_accuracy_lift_over_linear": _optional_delta(
                risk_context_linear_eval_accuracy,
                linear_eval_accuracy,
            ),
            "defense_context_train_accuracy_lift_over_risk_context": (
                defense_context_linear_train_accuracy - risk_context_linear_train_accuracy
            ),
            "defense_context_eval_accuracy_lift_over_risk_context": _optional_delta(
                defense_context_linear_eval_accuracy,
                risk_context_linear_eval_accuracy,
            ),
            "defense_context_v1_train_accuracy_lift_over_defense_context": (
                defense_context_v1_linear_train_accuracy - defense_context_linear_train_accuracy
            ),
            "defense_context_v1_eval_accuracy_lift_over_defense_context": _optional_delta(
                defense_context_v1_linear_eval_accuracy,
                defense_context_linear_eval_accuracy,
            ),
        },
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
    }
    for model_name, diagnostics in (model_diagnostics or {}).items():
        if model_name in report["models"]:
            report["models"][model_name].update(diagnostics)
    return report


def build_discard_benchmark_report_from_models(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame | None,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    models: dict[str, dict[str, Any]],
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
    game_counts: dict[str, int] | None = None,
) -> dict[str, Any]:
    return {
        "kind": DISCARD_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts_or_cached(game, game_counts),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "models": models,
        "ablation": _discard_ablation(models),
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def build_discard_transformer_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    model_kind: str,
    encoder_kind: str,
    input_tokens: int,
    output_dim: int,
    model_config: dict[str, int | float],
    epochs: int,
    batch_size: int,
    learning_rate: float,
    device: str,
    seed: int,
    train_metrics: dict[str, int | float | None],
    eval_metrics: dict[str, int | float | None],
    history: list[dict[str, Any]],
    best_epoch: int,
    selection_split: str,
    best_metrics: dict[str, dict[str, int | float | None]],
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    checkpoint_path: Path | None,
    source: dict[str, str | None],
    value_head: bool = False,
) -> dict[str, Any]:
    return {
        "kind": DISCARD_TRANSFORMER_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "model": {
            "kind": model_kind,
            "encoder_kind": encoder_kind,
            "input_tokens": input_tokens,
            "output_dim": output_dim,
            "value_head": value_head,
            "config": model_config,
        },
        "training": {
            "epochs": epochs,
            "batch_size": batch_size,
            "learning_rate": learning_rate,
            "device": device,
            "seed": seed,
            "history": history,
            "best_epoch": best_epoch,
            "selection_split": selection_split,
        },
        "metrics": {
            "train": train_metrics,
            "eval": eval_metrics,
            "best": best_metrics,
        },
        "training_history": normalize_training_history(
            history,
            step_key="epoch",
            step_unit="epoch",
            metric_roots=("metrics",),
        ),
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
        "artifacts": {
            "checkpoint_path": None if checkpoint_path is None else str(checkpoint_path),
        },
    }


def build_discard_transformer_benchmark_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    models: dict[str, dict[str, Any]],
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    checkpoint_path: Path | None,
    source: dict[str, str | None],
) -> dict[str, Any]:
    transformer_eval = _nested_model_metric(
        models,
        "discard_transformer",
        "metrics",
        "best",
        "eval",
        "accuracy",
    )
    if transformer_eval is None:
        transformer_eval = _nested_model_metric(
            models,
            "discard_transformer",
            "metrics",
            "eval",
            "accuracy",
        )
    risk_eval = _nested_model_metric(models, "risk_context_linear", "metrics", "eval_accuracy")
    defense_eval = _nested_model_metric(
        models,
        "defense_context_linear",
        "metrics",
        "eval_accuracy",
    )
    frequency_eval = _nested_model_metric(models, "frequency", "metrics", "eval_accuracy")
    return {
        "kind": DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "models": models,
        "deltas": {
            "transformer_eval_accuracy_lift_over_frequency": _optional_delta(
                transformer_eval,
                frequency_eval,
            ),
            "transformer_eval_accuracy_lift_over_risk_context": _optional_delta(
                transformer_eval,
                risk_eval,
            ),
            "transformer_eval_accuracy_lift_over_defense_context": _optional_delta(
                transformer_eval,
                defense_eval,
            ),
        },
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
        "artifacts": {
            "checkpoint_path": None if checkpoint_path is None else str(checkpoint_path),
        },
    }


def build_call_benchmark_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame | None,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    models: dict[str, dict[str, Any]],
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
    call_examples_total: int | None = None,
    example_limit: int | None = None,
    example_limit_strategy: str | None = None,
    timing: dict[str, float] | None = None,
    feature_cache: dict[str, Any] | None = None,
    example_cache: dict[str, Any] | None = None,
    game_counts: dict[str, int] | None = None,
) -> dict[str, Any]:
    return {
        "kind": CALL_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts_or_cached(game, game_counts),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "call_examples_total": (
            call_examples
            if call_examples_total is None
            else call_examples_total
        ),
        "example_limit": example_limit,
        "example_limit_strategy": example_limit_strategy,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "models": models,
        "timing": timing,
        "feature_cache": feature_cache,
        "example_cache": example_cache,
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def build_riichi_benchmark_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame | None,
    discard_examples: int,
    call_examples: int,
    riichi_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    models: dict[str, dict[str, Any]],
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
    game_counts: dict[str, int] | None = None,
) -> dict[str, Any]:
    return {
        "kind": RIICHI_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts_or_cached(game, game_counts),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "riichi_examples": riichi_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "models": models,
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def write_json_report(path: str | Path, payload: dict[str, Any]) -> None:
    report_path = Path(path)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(
        json.dumps(payload, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


def build_discard_benchmark_summary(paths: Sequence[Path]) -> dict[str, Any]:
    reports = [
        _summarize_benchmark_report(path, _read_json_report(path))
        for path in paths
    ]
    summary_kind = (
        DISCARD_BENCHMARK_SUMMARY_KIND
        if all(str(report["target"]).startswith("discard") for report in reports)
        else BENCHMARK_SUMMARY_KIND
    )
    return {
        "kind": summary_kind,
        "reports": reports,
    }


def format_discard_benchmark_summary(summary: dict[str, Any]) -> str:
    if summary.get("kind") not in {DISCARD_BENCHMARK_SUMMARY_KIND, BENCHMARK_SUMMARY_KIND}:
        raise ValueError("not a benchmark summary")

    lines: list[str] = []
    for report in summary["reports"]:
        if lines:
            lines.append("")
        lines.append(f"report: {report['path']}")
        source = report["source"]
        source_label = source.get("label") or "unknown"
        source_date = source.get("date") or "unknown"
        lines.append(f"source: {source_label} ({source_date})")
        if report["target"] == "discard":
            _append_discard_benchmark_summary_lines(lines, report)
        elif report["target"] == "discard_mlp":
            _append_discard_mlp_summary_lines(lines, report)
        elif report["target"] == "discard_mlp_benchmark":
            _append_discard_mlp_benchmark_summary_lines(lines, report)
        elif report["target"] == "discard_transformer":
            _append_discard_transformer_summary_lines(lines, report)
        elif report["target"] == "discard_transformer_benchmark":
            _append_discard_transformer_benchmark_summary_lines(lines, report)
        elif report["target"] in {"call", "riichi"}:
            _append_binary_benchmark_summary_lines(lines, report)
        elif report["target"] == "deal_in":
            _append_deal_in_benchmark_summary_lines(lines, report)
        else:
            raise ValueError(f"unsupported benchmark summary target: {report['target']}")
    return "\n".join(lines)


def build_public_benchmark_dashboard(
    paths: Sequence[Path],
    *,
    version: str,
    generated_at: str,
    title: str = "Kenjaku Offline Benchmark Dashboard",
) -> dict[str, Any]:
    return {
        "kind": PUBLIC_BENCHMARK_DASHBOARD_KIND,
        "title": title,
        "kenjaku_version": version,
        "generated_at": generated_at,
        "scope": {
            "offline_benchmarks_only": True,
            "live_rank_tracking": {
                "included": False,
                "requires_explicit_permission": True,
                "note": (
                    "Live ladder rank tracking is intentionally absent and requires "
                    "explicit platform permission before it can be published."
                ),
            },
        },
        "metric_definitions": list(PUBLIC_BENCHMARK_METRIC_DEFINITIONS),
        "summary": build_discard_benchmark_summary(paths),
    }


def format_public_benchmark_dashboard_html(
    dashboard: dict[str, Any],
    *,
    link_base_dir: Path | None = None,
) -> str:
    if dashboard.get("kind") != PUBLIC_BENCHMARK_DASHBOARD_KIND:
        raise ValueError("not a public benchmark dashboard")

    title = _html_text(dashboard["title"])
    version = _html_text(dashboard["kenjaku_version"])
    generated_at = _html_text(dashboard["generated_at"])
    metric_definitions = "\n".join(
        _dashboard_metric_definition_item(definition)
        for definition in dashboard["metric_definitions"]
    )
    comparison = _dashboard_comparison_section(dashboard["summary"]["reports"])
    reports = "\n".join(
        _dashboard_report_section(report, link_base_dir=link_base_dir)
        for report in dashboard["summary"]["reports"]
    )

    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" href="data:,">
  <title>{title}</title>
  <style>
    :root {{
      color-scheme: light;
      --bg: #f7f8fa;
      --text: #17202a;
      --muted: #5d6875;
      --line: #d9dee5;
      --panel: #ffffff;
      --accent: #0f766e;
      --accent-soft: #d9f4ee;
      --warn: #a15c07;
      --good: #0f766e;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font: 15px/1.5 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }}
    header, main, footer {{
      width: min(1120px, calc(100% - 32px));
      margin: 0 auto;
    }}
    header {{ padding: 40px 0 20px; }}
    h1 {{ margin: 0 0 10px; font-size: 34px; line-height: 1.15; }}
    h2 {{ margin: 0 0 14px; font-size: 22px; }}
    h3 {{ margin: 0 0 12px; font-size: 18px; }}
    p {{ margin: 0 0 10px; }}
    a {{ color: var(--accent); }}
    .eyebrow {{
      margin-bottom: 8px;
      color: var(--accent);
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0;
      text-transform: uppercase;
    }}
    .lede {{ max-width: 760px; color: var(--muted); font-size: 17px; }}
    .notice {{
      margin: 18px 0 0;
      padding: 12px 14px;
      border: 1px solid #a9d9d1;
      border-radius: 8px;
      background: var(--accent-soft);
      color: #164e46;
    }}
    section {{
      margin: 22px 0;
      padding: 22px;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--panel);
    }}
    .summary-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
      gap: 10px;
    }}
    .summary-item {{
      padding: 10px 0;
      border-bottom: 1px solid var(--line);
    }}
    .summary-item dt {{
      color: var(--muted);
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
    }}
    .summary-item dd {{ margin: 4px 0 0; }}
    table {{
      width: 100%;
      border-collapse: collapse;
      margin-top: 10px;
      font-size: 14px;
    }}
    th, td {{
      padding: 9px 8px;
      border-bottom: 1px solid var(--line);
      text-align: left;
      vertical-align: top;
    }}
    th {{ color: var(--muted); font-size: 12px; text-transform: uppercase; }}
    th button {{
      all: unset;
      cursor: pointer;
      color: inherit;
      font: inherit;
      text-transform: uppercase;
    }}
    .model-kind, .muted {{ color: var(--muted); }}
    .diff-best {{ color: var(--good); font-weight: 700; }}
    .diff-down {{ color: var(--warn); font-weight: 700; }}
    .sparkline {{ width: 96px; height: 24px; display: block; }}
    .sparkline path {{ fill: none; stroke: var(--accent); stroke-width: 2; }}
    .sparkline circle {{ fill: var(--accent); }}
    .artifact-list {{ margin: 8px 0 0; padding-left: 18px; }}
    .metric-definitions {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 12px;
    }}
    .metric-definitions div {{
      padding-bottom: 10px;
      border-bottom: 1px solid var(--line);
    }}
    .metric-definitions dt {{ font-weight: 700; }}
    .metric-definitions dd {{ margin: 4px 0 0; color: var(--muted); }}
    footer {{ padding: 10px 0 36px; color: var(--muted); }}
    @media (max-width: 720px) {{
      header, main, footer {{ width: min(100% - 20px, 1120px); }}
      h1 {{ font-size: 28px; }}
      section {{ padding: 16px; overflow-x: auto; }}
      table {{ min-width: 760px; }}
    }}
  </style>
</head>
<body>
  <header>
    <p class="eyebrow">Kenjaku {version}</p>
    <h1>{title}</h1>
    <p class="lede">
      Offline benchmark progress generated from local report artifacts. This page
      does not include, imply, or automate live ladder rank tracking.
    </p>
    <p class="notice">
      Live rank tracking: not included. Publishing live platform rank or ladder
      data requires explicit platform permission before collection or display.
    </p>
  </header>
  <main>
    <section>
      <h2>Metric Definitions</h2>
      <dl class="metric-definitions">
        {metric_definitions}
      </dl>
    </section>
    {comparison}
    {reports}
  </main>
  <footer>Generated at {generated_at}</footer>
  <script>
{_dashboard_sort_script()}
  </script>
</body>
</html>
"""


def _dashboard_metric_definition_item(definition: dict[str, Any]) -> str:
    return (
        "<div>"
        f"<dt>{_html_text(definition['name'])}</dt>"
        f"<dd>{_html_text(definition['definition'])}</dd>"
        "</div>"
    )


def _dashboard_comparison_section(reports: Sequence[dict[str, Any]]) -> str:
    rows = _dashboard_comparison_rows(reports)
    best_eval_accuracy = _max_numeric([row["best_eval_accuracy"] for row in rows])
    headers = "\n".join(
        [
            _dashboard_sortable_header(0, "Report", "text"),
            _dashboard_sortable_header(1, "Target", "text"),
            _dashboard_sortable_header(2, "Eval Examples", "number"),
            _dashboard_sortable_header(3, "Best Model", "text"),
            _dashboard_sortable_header(4, "Best Eval", "number"),
            _dashboard_sortable_header(5, "Vs Best", "number"),
            _dashboard_sortable_header(6, "Balanced", "number"),
            _dashboard_sortable_header(7, "Recall", "number"),
            _dashboard_sortable_header(8, "Models", "number"),
            "<th>Eval Sparkline</th>",
        ]
    )
    body = "\n".join(
        _dashboard_comparison_row(row, best_eval_accuracy=best_eval_accuracy)
        for row in rows
    )
    return f"""
    <section>
      <h2>Report Comparison</h2>
      <p class="muted">
        Side-by-side summary across supplied benchmark reports. Click a column
        header to sort.
      </p>
      <table id="comparison-table">
        <thead>
          <tr>
            {headers}
          </tr>
        </thead>
        <tbody>
          {body}
        </tbody>
      </table>
    </section>
"""


def _dashboard_sortable_header(column: int, label: str, sort_type: str) -> str:
    return (
        "<th>"
        f'<button type="button" data-sort-column="{column}" '
        f'data-sort-type="{sort_type}">{_html_text(label)}</button>'
        "</th>"
    )


def _dashboard_comparison_rows(reports: Sequence[dict[str, Any]]) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for index, report in enumerate(reports, start=1):
        model_rows = _dashboard_model_rows(report)
        best_model = _dashboard_best_model_row(model_rows)
        source = report.get("source", {})
        source_label = source.get("label") if isinstance(source, dict) else None
        source_date = source.get("date") if isinstance(source, dict) else None
        eval_values = [
            value
            for value in (_numeric(row.get("eval_accuracy")) for row in model_rows)
            if value is not None
        ]
        rows.append(
            {
                "report": f"Report {index}" if not source_label else str(source_label),
                "source_date": source_date or "unknown",
                "path": report["path"],
                "target": report["target"],
                "eval_examples": _dashboard_eval_examples(report),
                "best_model": best_model.get("name"),
                "best_eval_accuracy": best_model.get("eval_accuracy"),
                "balanced_accuracy": best_model.get("balanced_accuracy"),
                "target_recall": best_model.get("target_recall"),
                "model_count": len(model_rows),
                "sparkline_values": eval_values,
            }
        )
    return rows


def _dashboard_best_model_row(rows: Sequence[dict[str, Any]]) -> dict[str, Any]:
    best: dict[str, Any] = {}
    best_score: tuple[float, float] | None = None
    for row in rows:
        eval_accuracy = _numeric(row.get("eval_accuracy"))
        if eval_accuracy is None:
            continue
        balanced = _numeric(row.get("balanced_accuracy")) or -1.0
        score = (eval_accuracy, balanced)
        if best_score is None or score > best_score:
            best = row
            best_score = score
    return best


def _dashboard_comparison_row(
    row: dict[str, Any],
    *,
    best_eval_accuracy: float | None,
) -> str:
    best_eval = _numeric(row.get("best_eval_accuracy"))
    diff = (
        None
        if best_eval is None or best_eval_accuracy is None
        else best_eval - best_eval_accuracy
    )
    diff_class = _dashboard_diff_class(diff)
    return (
        "<tr>"
        f'<td data-sort="{_html_text(row["report"])}">'
        f"{_html_text(row['report'])}<br>"
        f'<span class="muted">{_html_text(row["source_date"])}</span><br>'
        f'<span class="muted">{_html_text(Path(str(row["path"])).name)}</span></td>'
        f'<td data-sort="{_html_text(row["target"])}">{_html_text(row["target"])}</td>'
        f'<td data-sort="{_sort_value(row["eval_examples"])}">'
        f"{_html_metric(row['eval_examples'])}</td>"
        f'<td data-sort="{_html_text(row["best_model"] or "")}">'
        f"{_html_text(row['best_model'] or 'n/a')}</td>"
        f'<td data-sort="{_sort_value(best_eval)}">{_html_metric(best_eval)}</td>'
        f'<td class="{diff_class}" data-sort="{_sort_value(diff)}">'
        f"{_dashboard_diff_text(diff)}</td>"
        f'<td data-sort="{_sort_value(row["balanced_accuracy"])}">'
        f"{_html_metric(row['balanced_accuracy'])}</td>"
        f'<td data-sort="{_sort_value(row["target_recall"])}">'
        f"{_html_metric(row['target_recall'])}</td>"
        f'<td data-sort="{_sort_value(row["model_count"])}">'
        f"{_html_metric(row['model_count'])}</td>"
        f"<td>{_dashboard_sparkline(row['sparkline_values'])}</td>"
        "</tr>"
    )


def _dashboard_eval_examples(report: dict[str, Any]) -> int | None:
    split = report.get("split")
    if isinstance(split, dict) and isinstance(split.get("eval_examples"), int):
        return split["eval_examples"]
    metrics = report.get("metrics")
    if isinstance(metrics, dict):
        eval_metrics = metrics.get("eval")
        if isinstance(eval_metrics, dict) and isinstance(eval_metrics.get("examples"), int):
            return eval_metrics["examples"]
    return None


def _dashboard_sparkline(values: Sequence[float]) -> str:
    if not values:
        return '<span class="muted">n/a</span>'
    width = 96
    height = 24
    if len(values) == 1:
        x = width // 2
        y = height // 2
        return (
            f'<svg class="sparkline" viewBox="0 0 {width} {height}" '
            'role="img" aria-label="single model eval score">'
            f'<circle cx="{x}" cy="{y}" r="3"></circle></svg>'
        )
    low = min(values)
    high = max(values)
    span = high - low
    points = []
    for index, value in enumerate(values):
        x = index * (width - 4) / (len(values) - 1) + 2
        y = height / 2 if span == 0 else height - 2 - ((value - low) / span) * (height - 4)
        points.append(f"{x:.1f},{y:.1f}")
    return (
        f'<svg class="sparkline" viewBox="0 0 {width} {height}" '
        'role="img" aria-label="model eval score sparkline">'
        f'<path d="M {" L ".join(points)}"></path></svg>'
    )


def _dashboard_diff_class(diff: float | None) -> str:
    if diff is None:
        return "muted"
    if abs(diff) < 0.0005:
        return "diff-best"
    return "diff-down"


def _dashboard_diff_text(diff: float | None) -> str:
    if diff is None:
        return "n/a"
    if abs(diff) < 0.0005:
        return "best"
    return _format_optional_delta(diff)


def _max_numeric(values: Sequence[Any]) -> float | None:
    numeric_values = [_numeric(value) for value in values]
    defined = [value for value in numeric_values if value is not None]
    return None if not defined else max(defined)


def _numeric(value: Any) -> float | None:
    return float(value) if isinstance(value, (int, float)) else None


def _sort_value(value: Any) -> str:
    numeric = _numeric(value)
    if numeric is None:
        return ""
    return f"{numeric:.12f}"


def _dashboard_sort_script() -> str:
    return r"""
(() => {
  const table = document.getElementById("comparison-table");
  if (!table) {
    return;
  }
  const body = table.tBodies[0];
  for (const button of table.querySelectorAll("[data-sort-column]")) {
    button.addEventListener("click", () => {
      const column = Number(button.dataset.sortColumn);
      const type = button.dataset.sortType || "text";
      const current = button.dataset.sortDirection === "asc" ? "desc" : "asc";
      button.dataset.sortDirection = current;
      const rows = Array.from(body.rows);
      rows.sort((left, right) => {
        const leftValue = left.cells[column]?.dataset.sort || "";
        const rightValue = right.cells[column]?.dataset.sort || "";
        if (type === "number") {
          return compareNumber(leftValue, rightValue, current);
        }
        return compareText(leftValue, rightValue, current);
      });
      for (const row of rows) {
        body.appendChild(row);
      }
    });
  }
})();

function compareNumber(left, right, direction) {
  const leftNumber = left === "" ? Number.NEGATIVE_INFINITY : Number(left);
  const rightNumber = right === "" ? Number.NEGATIVE_INFINITY : Number(right);
  const result = leftNumber - rightNumber;
  return direction === "asc" ? result : -result;
}

function compareText(left, right, direction) {
  const result = left.localeCompare(right, undefined, {numeric: true});
  return direction === "asc" ? result : -result;
}
""".strip()


def _dashboard_report_section(
    report: dict[str, Any],
    *,
    link_base_dir: Path | None,
) -> str:
    source = report["source"]
    source_label = source.get("label") or "unknown"
    source_date = source.get("date") or "unknown"
    overview_rows = "\n".join(
        _dashboard_summary_item(label, value)
        for label, value in (
            ("Target", report["target"]),
            ("Report kind", report["report_kind"]),
            ("Source", f"{source_label} ({source_date})"),
            ("Dataset slice", _dashboard_dataset_slice(report)),
            ("Split", _dashboard_split(report["split"])),
        )
    )
    model_rows = "\n".join(_dashboard_model_row(row) for row in _dashboard_model_rows(report))
    artifact_items = "\n".join(
        _dashboard_artifact_item(name, path, link_base_dir=link_base_dir)
        for name, path in _dashboard_artifacts(report)
    )
    selected_policy = _dashboard_selected_policy(report)
    selected_policy_block = (
        f'<p class="muted">{_html_text(selected_policy)}</p>'
        if selected_policy is not None
        else ""
    )

    return f"""
    <section>
      <h2>{_html_text(report['target'].replace('_', ' ').title())}</h2>
      <dl class="summary-grid">
        {overview_rows}
      </dl>
      {selected_policy_block}
      <h3>Latest Eval Scores</h3>
      <table>
        <thead>
          <tr>
            <th>Model</th>
            <th>Eval Accuracy</th>
            <th>Balanced</th>
            <th>Target Recall</th>
            <th>Pass Recall</th>
            <th>Brier</th>
            <th>Log Loss</th>
            <th>Best Eval</th>
            <th>Eval Loss</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          {model_rows}
        </tbody>
      </table>
      <h3>Artifacts</h3>
      <ul class="artifact-list">
        {artifact_items}
      </ul>
    </section>
"""


def _dashboard_summary_item(label: str, value: str) -> str:
    return (
        '<div class="summary-item">'
        f"<dt>{_html_text(label)}</dt>"
        f"<dd>{_html_text(value)}</dd>"
        "</div>"
    )


def _dashboard_dataset_slice(report: dict[str, Any]) -> str:
    parts = [
        f"{report.get('xml_file_count', 0)} XML files",
        f"{report.get('rounds', 0)} rounds",
    ]
    for key, label in (
        ("discard_examples", "discard examples"),
        ("call_examples", "call examples"),
        ("riichi_examples", "riichi examples"),
        ("deal_in_examples", "deal-in examples"),
    ):
        if key in report:
            parts.append(f"{report[key]} {label}")
    if "examples" in report:
        parts.append(f"{report['examples']} {report['target']} examples")
    return ", ".join(parts)


def _dashboard_split(split: dict[str, Any]) -> str:
    return (
        f"seed={split.get('seed')}, "
        f"eval_fraction={split.get('eval_fraction')}, "
        f"train={split.get('train_examples')}, "
        f"eval={split.get('eval_examples')}"
    )


def _dashboard_model_rows(report: dict[str, Any]) -> list[dict[str, Any]]:
    if report["target"] == "deal_in":
        metrics = report["metrics"]["eval"]
        model = report["model"]
        training = report["training"]
        return [
            {
                "name": model.get("kind", "deal-in model"),
                "kind": model.get("kind"),
                "eval_accuracy": metrics.get("accuracy"),
                "balanced_accuracy": metrics.get("balanced_accuracy"),
                "target_recall": metrics.get("recall"),
                "pass_recall": metrics.get("specificity"),
                "brier_score": metrics.get("brier_score"),
                "log_loss": metrics.get("log_loss"),
                "best_eval_accuracy": None,
                "eval_loss": None,
                "notes": (
                    f"feature_dim={model.get('feature_dim')} "
                    f"threshold={training.get('threshold')}"
                ),
            }
        ]

    target = report["target"]
    rows: list[dict[str, Any]] = []
    for model_name, model in report.get("models", {}).items():
        metrics = model.get("metrics")
        if not isinstance(metrics, dict):
            metrics = {}
        notes = _dashboard_model_notes(model)
        rows.append(
            {
                "name": model_name,
                "kind": model.get("kind"),
                "eval_accuracy": model.get("eval_accuracy", metrics.get("eval_accuracy")),
                "balanced_accuracy": model.get("eval_balanced_accuracy"),
                "target_recall": _dashboard_target_recall(target, model),
                "pass_recall": model.get("eval_pass_recall"),
                "brier_score": None,
                "log_loss": None,
                "best_eval_accuracy": metrics.get("best_eval_accuracy"),
                "eval_loss": metrics.get("eval_loss"),
                "notes": notes,
            }
        )
    return rows


def _dashboard_model_notes(model: dict[str, Any]) -> str:
    notes: list[str] = []
    for key in (
        "feature_dim",
        "input_dim",
        "hidden_dim",
        "output_dim",
        "encoder_kind",
        "input_tokens",
    ):
        if model.get(key) is not None:
            notes.append(f"{key}={model[key]}")
    if model.get("policy_threshold") is not None:
        notes.append(f"threshold={model['policy_threshold']}")
    if model.get("policy_threshold_source") is not None:
        notes.append(f"threshold_source={model['policy_threshold_source']}")
    training = model.get("training")
    if isinstance(training, dict) and training.get("best_epoch") is not None:
        notes.append(f"best_epoch={training['best_epoch']}")
    return " ".join(notes) if notes else "n/a"


def _dashboard_target_recall(target: str, model: dict[str, Any]) -> Any:
    if target == "call":
        return model.get("eval_call_recall")
    if target == "riichi":
        return model.get("eval_riichi_recall")
    return None


def _dashboard_model_row(row: dict[str, Any]) -> str:
    kind = row.get("kind") or "unknown"
    return (
        "<tr>"
        f"<td>{_html_text(row['name'])}<br>"
        f'<span class="model-kind">{_html_text(kind)}</span></td>'
        f"<td>{_html_metric(row['eval_accuracy'])}</td>"
        f"<td>{_html_metric(row['balanced_accuracy'])}</td>"
        f"<td>{_html_metric(row['target_recall'])}</td>"
        f"<td>{_html_metric(row['pass_recall'])}</td>"
        f"<td>{_html_metric(row['brier_score'])}</td>"
        f"<td>{_html_metric(row['log_loss'])}</td>"
        f"<td>{_html_metric(row['best_eval_accuracy'])}</td>"
        f"<td>{_html_metric(row['eval_loss'])}</td>"
        f"<td>{_html_text(row['notes'])}</td>"
        "</tr>"
    )


def _dashboard_selected_policy(report: dict[str, Any]) -> str | None:
    selected_policy = report.get("selected_policy")
    if not isinstance(selected_policy, dict):
        return None
    model_name = selected_policy.get("model_name")
    if model_name is None:
        return None
    return (
        "Selected call policy: "
        f"{model_name} "
        f"balanced={_format_optional_float(selected_policy.get('eval_balanced_accuracy'))} "
        f"call_recall={_format_optional_float(selected_policy.get('eval_call_recall'))} "
        f"pass_recall={_format_optional_float(selected_policy.get('eval_pass_recall'))} "
        f"eval={_format_optional_float(selected_policy.get('eval_accuracy'))}"
    )


def _dashboard_artifacts(report: dict[str, Any]) -> list[tuple[str, str]]:
    artifacts = [("Report JSON", report["path"])]
    for name, value in report.get("artifacts", {}).items():
        if value:
            artifacts.append((name.replace("_", " ").title(), str(value)))
    return artifacts


def _dashboard_artifact_item(
    name: str,
    path: str,
    *,
    link_base_dir: Path | None,
) -> str:
    link_target = _dashboard_link_target(path, link_base_dir=link_base_dir)
    href = escape(
        quote(link_target.replace("\\", "/"), safe="/:#?&=%._~+-"),
        quote=True,
    )
    return (
        f'<li>{_html_text(name)}: '
        f'<a href="{href}">{_html_text(link_target)}</a></li>'
    )


def _dashboard_link_target(path: str, *, link_base_dir: Path | None) -> str:
    href = path
    if not path.startswith(("http://", "https://")) and link_base_dir is not None:
        path_obj = Path(path)
        if path_obj.is_absolute():
            try:
                href = os.path.relpath(path_obj, link_base_dir)
            except ValueError:
                href = path
    return href


def _html_metric(value: Any) -> str:
    if value is None:
        return "n/a"
    if isinstance(value, (int, float)):
        return _html_text(_format_optional_float(float(value)))
    return _html_text(value)


def _html_text(value: Any) -> str:
    return escape(str(value), quote=True)


def build_discard_disagreement_summary(paths: Sequence[Path]) -> dict[str, Any]:
    return {
        "kind": DISCARD_DISAGREEMENT_SUMMARY_KIND,
        "reports": [
            _summarize_discard_disagreement_report(path, _read_json_report(path))
            for path in paths
        ],
    }


def format_discard_disagreement_summary(summary: dict[str, Any]) -> str:
    if summary.get("kind") != DISCARD_DISAGREEMENT_SUMMARY_KIND:
        raise ValueError("not a discard disagreement summary")

    lines: list[str] = []
    for report in summary["reports"]:
        if lines:
            lines.append("")
        lines.append(f"report: {report['path']}")
        lines.append(
            f"examples: {report['examples']} eval, "
            f"max_per_category={report['max_per_category']}"
        )
        for category_name, category in report["categories"].items():
            lines.append(
                f"{category_name}: count={category['count']} "
                f"stored={category['stored_items']}"
            )
            bucket_parts = [
                f"{name}={stats['true']}/{stats['examples']}"
                for name, stats in category["defense_buckets"].items()
                if stats["true"]
            ]
            if bucket_parts:
                lines.append("  defense_buckets: " + ", ".join(bucket_parts))
            pair_parts = [
                (
                    f"actual={pair['actual']} correct={pair['correct_prediction']} "
                    f"wrong={pair['wrong_prediction']} ({pair['examples']})"
                )
                for pair in category["actual_prediction_pairs"][:3]
            ]
            if pair_parts:
                lines.append("  common_pairs: " + "; ".join(pair_parts))
            margins = category["logit_margins"]
            correct_margin = _summary_mean(margins["correct_model_actual_margin"])
            wrong_margin = _summary_mean(margins["wrong_model_error_margin"])
            lines.append(
                "  margins: "
                f"correct_model_actual={_format_optional_float(correct_margin)} "
                f"wrong_model_error={_format_optional_float(wrong_margin)}"
            )
    return "\n".join(lines)


def _game_counts(game: TenhouGame) -> dict[str, int]:
    return {
        "rounds": len(game.rounds),
        "draws": sum(len(round_.draws) for round_ in game.rounds),
        "discards": sum(len(round_.discards) for round_ in game.rounds),
        "reaches": sum(len(round_.reaches) for round_ in game.rounds),
        "calls": sum(len(round_.calls) for round_ in game.rounds),
        "wins": sum(len(round_.agari) for round_ in game.rounds),
        "exhaustive_draws": sum(round_.ryuukyoku is not None for round_ in game.rounds),
    }


def _game_counts_or_cached(
    game: TenhouGame | None,
    game_counts: dict[str, int] | None,
) -> dict[str, int]:
    if game is not None:
        return _game_counts(game)
    if game_counts is None:
        raise ValueError("game_counts are required when game is not available")
    required = (
        "rounds",
        "draws",
        "discards",
        "reaches",
        "calls",
        "wins",
        "exhaustive_draws",
    )
    return {key: int(game_counts[key]) for key in required}


def _read_json_report(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"report must be a JSON object: {path}")
    return payload


def _summarize_benchmark_report(path: Path, payload: dict[str, Any]) -> dict[str, Any]:
    kind = payload.get("kind")
    if kind == DISCARD_BENCHMARK_REPORT_KIND:
        return _summarize_discard_benchmark_report(path, payload)
    if kind == DISCARD_MLP_REPORT_KIND:
        return _summarize_discard_mlp_report(path, payload)
    if kind == DISCARD_MLP_BENCHMARK_REPORT_KIND:
        return _summarize_discard_mlp_benchmark_report(path, payload)
    if kind == DISCARD_TRANSFORMER_REPORT_KIND:
        return _summarize_discard_transformer_report(path, payload)
    if kind == DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND:
        return _summarize_discard_transformer_benchmark_report(path, payload)
    if kind == DEAL_IN_BENCHMARK_REPORT_KIND:
        return _summarize_deal_in_benchmark_report(path, payload)
    if kind == CALL_BENCHMARK_REPORT_KIND:
        return _summarize_binary_benchmark_report(path, payload, target="call")
    if kind == RIICHI_BENCHMARK_REPORT_KIND:
        return _summarize_binary_benchmark_report(path, payload, target="riichi")
    raise ValueError(f"not a benchmark report: {path}")


def _summarize_discard_benchmark_report(path: Path, payload: dict[str, Any]) -> dict[str, Any]:
    if payload.get("kind") != DISCARD_BENCHMARK_REPORT_KIND:
        raise ValueError(f"not a discard benchmark report: {path}")
    models = {
        model_name: {
            "kind": model_payload.get("kind"),
            "feature_dim": model_payload.get("feature_dim"),
            "train_accuracy": model_payload["metrics"]["train_accuracy"],
            "eval_accuracy": model_payload["metrics"]["eval_accuracy"],
        }
        for model_name, model_payload in payload["models"].items()
    }
    return {
        "path": str(path),
        "target": "discard",
        "report_kind": DISCARD_BENCHMARK_REPORT_KIND,
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "discard_examples": payload["discard_examples"],
        "call_examples": payload["call_examples"],
        "split": payload["split"],
        "models": models,
        "ablation": payload["ablation"],
        "buckets": _summary_buckets(payload["models"]),
    }


def _summarize_binary_benchmark_report(
    path: Path,
    payload: dict[str, Any],
    *,
    target: str,
) -> dict[str, Any]:
    recall_key = f"eval_{target}_recall"
    examples_key = f"{target}_examples"
    models = {}
    for model_name, model_payload in payload["models"].items():
        metrics = model_payload["metrics"]
        policy = model_payload.get("policy")
        calibration = model_payload.get("calibration")
        training = model_payload.get("training", {})
        models[model_name] = {
            "kind": model_payload.get("kind"),
            "feature_dim": model_payload.get("feature_dim"),
            "train_accuracy": metrics["train_accuracy"],
            "eval_accuracy": metrics["eval_accuracy"],
            "eval_balanced_accuracy": metrics.get("eval_balanced_accuracy"),
            "eval_pass_recall": metrics.get("eval_pass_recall"),
            recall_key: metrics.get(recall_key),
            "policy_threshold": (
                policy.get("threshold")
                if isinstance(policy, dict)
                else None
            ),
            "policy_threshold_source": (
                policy.get("threshold_source")
                if isinstance(policy, dict)
                else None
            ),
            "train_best_threshold": _calibration_best_threshold(calibration, "train"),
            "eval_best_threshold": _calibration_best_threshold(calibration, "eval"),
            "positive_class_weight": training.get("positive_class_weight"),
        }
    summary = {
        "path": str(path),
        "target": target,
        "report_kind": payload["kind"],
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "examples": payload[examples_key],
        "examples_total": payload.get(f"{target}_examples_total"),
        "example_limit": payload.get("example_limit"),
        "example_limit_strategy": payload.get("example_limit_strategy"),
        "split": payload["split"],
        "models": models,
    }
    if target == "call":
        summary["selected_policy"] = _selected_call_policy(models)
    return summary


def _summarize_deal_in_benchmark_report(path: Path, payload: dict[str, Any]) -> dict[str, Any]:
    if payload.get("kind") != DEAL_IN_BENCHMARK_REPORT_KIND:
        raise ValueError(f"not a deal-in benchmark report: {path}")
    model_eval = payload["metrics"]["eval"]
    heuristic_eval = payload["heuristic_risk_baseline"]["eval"]
    calibration = payload.get("calibration")
    return {
        "path": str(path),
        "target": "deal_in",
        "report_kind": DEAL_IN_BENCHMARK_REPORT_KIND,
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "deal_in_examples": payload["deal_in_examples"],
        "label_summary": payload["label_summary"],
        "filters": payload.get("filters", {}),
        "split": payload["split"],
        "model": payload["model"],
        "training": payload["training"],
        "calibration": {
            "target": calibration.get("target") if isinstance(calibration, dict) else None,
            "train_best_threshold": _calibration_best_threshold(calibration, "train"),
            "eval_best_threshold": _calibration_best_threshold(calibration, "eval"),
        },
        "metrics": {
            "train": _deal_in_metrics_summary(payload["metrics"]["train"]),
            "eval": _deal_in_metrics_summary(model_eval),
        },
        "heuristic_risk_baseline": {
            "calibrated_probability": payload["heuristic_risk_baseline"].get(
                "calibrated_probability"
            ),
            "train": _deal_in_metrics_summary(payload["heuristic_risk_baseline"]["train"]),
            "eval": _deal_in_metrics_summary(heuristic_eval),
        },
        "deltas": {
            "eval_brier_score_vs_heuristic": _optional_delta(
                _metric(model_eval, "brier_score"),
                _metric(heuristic_eval, "brier_score"),
            ),
            "eval_log_loss_vs_heuristic": _optional_delta(
                _metric(model_eval, "log_loss"),
                _metric(heuristic_eval, "log_loss"),
            ),
            "eval_accuracy_vs_heuristic": _optional_delta(
                _metric(model_eval, "accuracy"),
                _metric(heuristic_eval, "accuracy"),
            ),
            "eval_balanced_accuracy_vs_heuristic": _optional_delta(
                _metric(model_eval, "balanced_accuracy"),
                _metric(heuristic_eval, "balanced_accuracy"),
            ),
        },
    }


def _deal_in_metrics_summary(metrics: dict[str, Any]) -> dict[str, Any]:
    return {
        "examples": metrics.get("examples"),
        "positives": metrics.get("positives"),
        "negatives": metrics.get("negatives"),
        "positive_rate": metrics.get("positive_rate"),
        "accuracy": metrics.get("accuracy"),
        "balanced_accuracy": metrics.get("balanced_accuracy"),
        "precision": metrics.get("precision"),
        "recall": metrics.get("recall"),
        "specificity": metrics.get("specificity"),
        "brier_score": metrics.get("brier_score"),
        "log_loss": metrics.get("log_loss"),
        "predicted_positive_rate": metrics.get("predicted_positive_rate"),
    }


def _summarize_discard_mlp_report(path: Path, payload: dict[str, Any]) -> dict[str, Any]:
    if payload.get("kind") != DISCARD_MLP_REPORT_KIND:
        raise ValueError(f"not a discard MLP report: {path}")
    return {
        "path": str(path),
        "target": "discard_mlp",
        "report_kind": DISCARD_MLP_REPORT_KIND,
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "discard_examples": payload["discard_examples"],
        "call_examples": payload["call_examples"],
        "split": payload["split"],
        "model": payload["model"],
        "training": _mlp_training_summary(payload["training"]),
        "metrics": _mlp_metrics_summary(payload["metrics"]),
        "deltas": {},
    }


def _summarize_discard_transformer_report(
    path: Path,
    payload: dict[str, Any],
) -> dict[str, Any]:
    if payload.get("kind") != DISCARD_TRANSFORMER_REPORT_KIND:
        raise ValueError(f"not a discard transformer report: {path}")
    return {
        "path": str(path),
        "target": "discard_transformer",
        "report_kind": DISCARD_TRANSFORMER_REPORT_KIND,
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "discard_examples": payload["discard_examples"],
        "call_examples": payload["call_examples"],
        "split": payload["split"],
        "model": payload["model"],
        "training": _mlp_training_summary(payload["training"]),
        "metrics": _mlp_metrics_summary(payload["metrics"]),
        "deltas": {},
        "artifacts": payload.get("artifacts", {}),
    }


def _summarize_discard_transformer_benchmark_report(
    path: Path,
    payload: dict[str, Any],
) -> dict[str, Any]:
    if payload.get("kind") != DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND:
        raise ValueError(f"not a discard transformer benchmark report: {path}")
    models: dict[str, Any] = {}
    for model_name, model_payload in payload["models"].items():
        if model_name == "discard_transformer":
            models[model_name] = {
                "kind": model_payload.get("kind"),
                "encoder_kind": model_payload.get("encoder_kind"),
                "input_tokens": model_payload.get("input_tokens"),
                "output_dim": model_payload.get("output_dim"),
                "value_head": model_payload.get("value_head", False),
                "config": model_payload.get("config", {}),
                "training": _mlp_training_summary(model_payload["training"]),
                "metrics": _mlp_metrics_summary(model_payload["metrics"]),
            }
        else:
            models[model_name] = {
                "kind": model_payload.get("kind"),
                "feature_dim": model_payload.get("feature_dim"),
                "train_accuracy": model_payload["metrics"]["train_accuracy"],
                "eval_accuracy": model_payload["metrics"]["eval_accuracy"],
            }
    return {
        "path": str(path),
        "target": "discard_transformer_benchmark",
        "report_kind": DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND,
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "discard_examples": payload["discard_examples"],
        "call_examples": payload["call_examples"],
        "split": payload["split"],
        "models": models,
        "deltas": payload.get("deltas", {}),
        "artifacts": payload.get("artifacts", {}),
    }


def _summarize_discard_mlp_benchmark_report(
    path: Path,
    payload: dict[str, Any],
) -> dict[str, Any]:
    if payload.get("kind") != DISCARD_MLP_BENCHMARK_REPORT_KIND:
        raise ValueError(f"not a discard MLP benchmark report: {path}")
    models: dict[str, Any] = {}
    for model_name, model_payload in payload["models"].items():
        if model_name == "discard_mlp":
            models[model_name] = {
                "kind": model_payload.get("kind"),
                "input_dim": model_payload.get("input_dim"),
                "hidden_dim": model_payload.get("hidden_dim"),
                "output_dim": model_payload.get("output_dim"),
                "training": _mlp_training_summary(model_payload["training"]),
                "metrics": _mlp_metrics_summary(model_payload["metrics"]),
            }
        else:
            models[model_name] = {
                "kind": model_payload.get("kind"),
                "feature_dim": model_payload.get("feature_dim"),
                "train_accuracy": model_payload["metrics"]["train_accuracy"],
                "eval_accuracy": model_payload["metrics"]["eval_accuracy"],
            }
    return {
        "path": str(path),
        "target": "discard_mlp_benchmark",
        "report_kind": DISCARD_MLP_BENCHMARK_REPORT_KIND,
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "discard_examples": payload["discard_examples"],
        "call_examples": payload["call_examples"],
        "split": payload["split"],
        "models": models,
        "deltas": payload.get("deltas", {}),
        "artifacts": payload.get("artifacts", {}),
    }


def _append_discard_benchmark_summary_lines(lines: list[str], report: dict[str, Any]) -> None:
    split = report["split"]
    lines.append(
        "examples: "
        f"{report['discard_examples']} total, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    lines.append("models:")
    for model_name in DISCARD_BENCHMARK_MODEL_ORDER:
        if model_name not in report["models"]:
            continue
        model = report["models"][model_name]
        train = _format_optional_float(model["train_accuracy"])
        eval_ = _format_optional_float(model["eval_accuracy"])
        lines.append(f"  {model_name}: train={train} eval={eval_}")
    lines.append("ablation:")
    for name, value in report["ablation"].items():
        lines.append(f"  {name}: {_format_optional_delta(value)}")
    lines.append("selected_buckets:")
    for bucket_name, model_stats in report["buckets"].items():
        parts = []
        for model_name in DISCARD_BENCHMARK_BUCKET_MODELS:
            if model_name not in model_stats:
                continue
            stats = model_stats[model_name]
            accuracy = _format_optional_float(stats["accuracy"])
            parts.append(f"{model_name}={accuracy}/{stats['examples']}")
        if parts:
            lines.append(f"  {bucket_name}: " + ", ".join(parts))


def _append_binary_benchmark_summary_lines(lines: list[str], report: dict[str, Any]) -> None:
    target = report["target"]
    split = report["split"]
    lines.append(
        "examples: "
        f"{report['examples']} {target}, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    if report.get("example_limit") is not None:
        lines.append(
            "example_limit: "
            f"{report['example_limit']} of {report.get('examples_total', report['examples'])} "
            f"strategy={report.get('example_limit_strategy', 'unknown')}"
        )
    lines.append("models:")
    recall_key = f"eval_{target}_recall"
    for model_name, model in report["models"].items():
        parts = [
            f"eval={_format_optional_float(model['eval_accuracy'])}",
            f"balanced={_format_optional_float(model['eval_balanced_accuracy'])}",
            f"pass_recall={_format_optional_float(model['eval_pass_recall'])}",
            f"{target}_recall={_format_optional_float(model[recall_key])}",
        ]
        if model["policy_threshold"] is not None:
            parts.append(f"threshold={float(model['policy_threshold']):.2f}")
        if model["policy_threshold_source"] is not None:
            parts.append(f"source={model['policy_threshold_source']}")
        if model["train_best_threshold"] is not None:
            parts.append(f"train_best={float(model['train_best_threshold']):.2f}")
        if model["eval_best_threshold"] is not None:
            parts.append(f"eval_best={float(model['eval_best_threshold']):.2f}")
        if model["positive_class_weight"] is not None:
            parts.append(f"weight={float(model['positive_class_weight']):.2f}")
        lines.append(f"  {model_name}: " + " ".join(parts))
    selected_policy = report.get("selected_policy")
    if isinstance(selected_policy, dict) and selected_policy.get("model_name") is not None:
        parts = [
            f"model={selected_policy['model_name']}",
            f"balanced={_format_optional_float(selected_policy['eval_balanced_accuracy'])}",
            f"{target}_recall={_format_optional_float(selected_policy[f'eval_{target}_recall'])}",
            f"pass_recall={_format_optional_float(selected_policy['eval_pass_recall'])}",
            f"eval={_format_optional_float(selected_policy['eval_accuracy'])}",
        ]
        lines.append("selected_policy: " + " ".join(parts))


def _append_deal_in_benchmark_summary_lines(lines: list[str], report: dict[str, Any]) -> None:
    split = report["split"]
    labels = report["label_summary"]
    lines.append(
        "examples: "
        f"{report['deal_in_examples']} direct-labeled, "
        f"{labels['direct_deal_in_examples']} positive, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    lines.append(
        "labels: "
        f"positive_rate={_format_optional_float(labels['positive_rate'])} "
        f"active_riichi={labels['active_riichi_examples']} "
        f"active_riichi_positive={labels['active_riichi_deal_in_examples']}"
    )
    model = report["model"]
    training = report["training"]
    calibration = report.get("calibration", {})
    lines.append(
        "model: "
        f"{model['kind']} feature_dim={model['feature_dim']} "
        f"epochs={training['epochs']} "
        f"learning_rate={float(training['learning_rate']):.6g} "
        f"weight={float(training['positive_class_weight']):.2f} "
        f"threshold={float(training['threshold']):.2f}"
    )
    if isinstance(calibration, dict):
        train_best = calibration.get("train_best_threshold")
        eval_best = calibration.get("eval_best_threshold")
        lines.append(
            "calibration: "
            f"target={calibration.get('target')} "
            f"train_best={_format_optional_threshold(train_best)} "
            f"eval_best={_format_optional_threshold(eval_best)}"
        )
    _append_deal_in_metric_line(lines, "eval", report["metrics"]["eval"])
    _append_deal_in_metric_line(
        lines,
        "heuristic_eval",
        report["heuristic_risk_baseline"]["eval"],
    )
    lines.append("deltas:")
    for name, value in report["deltas"].items():
        lines.append(f"  {name}: {_format_optional_delta(value)}")


def _append_deal_in_metric_line(
    lines: list[str],
    label: str,
    metrics: dict[str, Any],
) -> None:
    lines.append(
        f"{label}: "
        f"accuracy={_format_optional_float(metrics['accuracy'])} "
        f"balanced={_format_optional_float(metrics['balanced_accuracy'])} "
        f"recall={_format_optional_float(metrics['recall'])} "
        f"specificity={_format_optional_float(metrics['specificity'])} "
        f"brier={_format_optional_float(metrics['brier_score'])} "
        f"log_loss={_format_optional_float(metrics['log_loss'])}"
    )


def _append_discard_mlp_summary_lines(lines: list[str], report: dict[str, Any]) -> None:
    split = report["split"]
    lines.append(
        "examples: "
        f"{report['discard_examples']} total, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    model = report["model"]
    training = report["training"]
    metrics = report["metrics"]
    lines.append(
        "model: "
        f"{model['kind']} hidden_dim={model['hidden_dim']} "
        f"device={training['device']} epochs={training['epochs']} "
        f"batch_size={training['batch_size']} "
        f"learning_rate={float(training['learning_rate']):.6g} seed={training['seed']}"
    )
    lines.append(
        "metrics: "
        f"train={_format_optional_float(metrics['train_accuracy'])} "
        f"eval={_format_optional_float(metrics['eval_accuracy'])} "
        f"train_loss={_format_optional_float(metrics['train_loss'])} "
        f"eval_loss={_format_optional_float(metrics['eval_loss'])}"
    )
    lines.append(
        "best: "
        f"epoch={training['best_epoch']} split={training['selection_split']} "
        f"eval={_format_optional_float(metrics['best_eval_accuracy'])} "
        f"eval_loss={_format_optional_float(metrics['best_eval_loss'])}"
    )


def _append_discard_transformer_summary_lines(
    lines: list[str],
    report: dict[str, Any],
) -> None:
    split = report["split"]
    lines.append(
        "examples: "
        f"{report['discard_examples']} total, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    model = report["model"]
    config = model["config"]
    training = report["training"]
    metrics = report["metrics"]
    lines.append(
        "model: "
        f"{model['kind']} encoder={model['encoder_kind']} "
        f"tokens={model['input_tokens']} dim={config['model_dim']} "
        f"heads={config['num_heads']} layers={config['num_layers']} "
        f"ff={config['feedforward_dim']} dropout={float(config['dropout']):.4g}"
    )
    lines.append(
        "training: "
        f"device={training['device']} epochs={training['epochs']} "
        f"batch_size={training['batch_size']} "
        f"learning_rate={float(training['learning_rate']):.6g} seed={training['seed']}"
    )
    lines.append(
        "metrics: "
        f"train={_format_optional_float(metrics['train_accuracy'])} "
        f"eval={_format_optional_float(metrics['eval_accuracy'])} "
        f"train_loss={_format_optional_float(metrics['train_loss'])} "
        f"eval_loss={_format_optional_float(metrics['eval_loss'])}"
    )
    lines.append(
        "best: "
        f"epoch={training['best_epoch']} split={training['selection_split']} "
        f"eval={_format_optional_float(metrics['best_eval_accuracy'])} "
        f"eval_loss={_format_optional_float(metrics['best_eval_loss'])}"
    )


def _append_discard_mlp_benchmark_summary_lines(
    lines: list[str],
    report: dict[str, Any],
) -> None:
    split = report["split"]
    lines.append(
        "examples: "
        f"{report['discard_examples']} total, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    lines.append("models:")
    for model_name in DISCARD_MLP_BENCHMARK_MODEL_ORDER:
        if model_name not in report["models"]:
            continue
        model = report["models"][model_name]
        if model_name == "discard_mlp":
            metrics = model["metrics"]
            training = model["training"]
            lines.append(
                "  discard_mlp: "
                f"eval={_format_optional_float(metrics['eval_accuracy'])} "
                f"best_eval={_format_optional_float(metrics['best_eval_accuracy'])} "
                f"loss={_format_optional_float(metrics['eval_loss'])} "
                f"best_epoch={training['best_epoch']} "
                f"hidden_dim={model['hidden_dim']} device={training['device']}"
            )
        else:
            lines.append(
                f"  {model_name}: "
                f"train={_format_optional_float(model['train_accuracy'])} "
                f"eval={_format_optional_float(model['eval_accuracy'])}"
            )
    lines.append("deltas:")
    for name, value in report["deltas"].items():
        lines.append(f"  {name}: {_format_optional_delta(value)}")


def _append_discard_transformer_benchmark_summary_lines(
    lines: list[str],
    report: dict[str, Any],
) -> None:
    split = report["split"]
    lines.append(
        "examples: "
        f"{report['discard_examples']} total, "
        f"{split['train_examples']} train, "
        f"{split['eval_examples']} eval"
    )
    lines.append("models:")
    for model_name in DISCARD_TRANSFORMER_BENCHMARK_MODEL_ORDER:
        if model_name not in report["models"]:
            continue
        model = report["models"][model_name]
        if model_name == "discard_transformer":
            metrics = model["metrics"]
            config = model["config"]
            lines.append(
                "  discard_transformer: "
                f"eval={_format_optional_float(metrics['eval_accuracy'])} "
                f"best_eval={_format_optional_float(metrics['best_eval_accuracy'])} "
                f"loss={_format_optional_float(metrics['eval_loss'])} "
                f"dim={config.get('model_dim')} "
                f"heads={config.get('num_heads')} "
                f"layers={config.get('num_layers')}"
            )
        else:
            lines.append(
                f"  {model_name}: "
                f"train={_format_optional_float(model['train_accuracy'])} "
                f"eval={_format_optional_float(model['eval_accuracy'])}"
            )
    lines.append("deltas:")
    for name, value in report["deltas"].items():
        lines.append(f"  {name}: {_format_optional_delta(value)}")


def _calibration_best_threshold(calibration: Any, split: str) -> float | None:
    if not isinstance(calibration, dict):
        return None
    split_payload = calibration.get(split)
    if not isinstance(split_payload, dict):
        return None
    best = split_payload.get("best")
    if not isinstance(best, dict) or best.get("threshold") is None:
        return None
    return float(best["threshold"])


def _selected_call_policy(models: dict[str, Any]) -> dict[str, Any]:
    candidates: list[tuple[tuple[float, float, float, float, str], str, dict[str, Any]]] = []
    for model_name, model in models.items():
        balanced = model.get("eval_balanced_accuracy")
        if balanced is None:
            continue
        call_recall = model.get("eval_call_recall")
        pass_recall = model.get("eval_pass_recall")
        eval_accuracy = model.get("eval_accuracy")
        key = (
            _metric_sort_value(balanced),
            _metric_sort_value(call_recall),
            _metric_sort_value(pass_recall),
            _metric_sort_value(eval_accuracy),
            model_name,
        )
        candidates.append((key, model_name, model))
    if not candidates:
        return {
            "model_name": None,
            "eval_accuracy": None,
            "eval_balanced_accuracy": None,
            "eval_pass_recall": None,
            "eval_call_recall": None,
        }
    _key, model_name, model = max(candidates, key=lambda item: item[0])
    return {
        "model_name": model_name,
        "eval_accuracy": model.get("eval_accuracy"),
        "eval_balanced_accuracy": model.get("eval_balanced_accuracy"),
        "eval_pass_recall": model.get("eval_pass_recall"),
        "eval_call_recall": model.get("eval_call_recall"),
    }


def _metric_sort_value(value: Any) -> float:
    if value is None:
        return float("-inf")
    return float(value)


def _mlp_training_summary(training: dict[str, Any]) -> dict[str, Any]:
    return {
        "epochs": training["epochs"],
        "batch_size": training["batch_size"],
        "learning_rate": training["learning_rate"],
        "device": training["device"],
        "seed": training["seed"],
        "best_epoch": training["best_epoch"],
        "selection_split": training["selection_split"],
    }


def _mlp_metrics_summary(metrics: dict[str, Any]) -> dict[str, Any]:
    train = metrics.get("train", {})
    eval_ = metrics.get("eval", {})
    best = metrics.get("best", {})
    best_train = best.get("train", {}) if isinstance(best, dict) else {}
    best_eval = best.get("eval", {}) if isinstance(best, dict) else {}
    return {
        "train_accuracy": train.get("accuracy"),
        "eval_accuracy": eval_.get("accuracy"),
        "train_loss": train.get("loss"),
        "eval_loss": eval_.get("loss"),
        "best_train_accuracy": best_train.get("accuracy"),
        "best_eval_accuracy": best_eval.get("accuracy"),
        "best_train_loss": best_train.get("loss"),
        "best_eval_loss": best_eval.get("loss"),
    }


def _nested_model_metric(
    models: dict[str, Any],
    model_name: str,
    *keys: str,
) -> float | None:
    value: Any = models.get(model_name)
    for key in keys:
        if not isinstance(value, dict):
            return None
        value = value.get(key)
    if value is None:
        return None
    return float(value)


def _summarize_discard_disagreement_report(
    path: Path,
    payload: dict[str, Any],
) -> dict[str, Any]:
    if payload.get("kind") != DISCARD_DISAGREEMENT_REPORT_KIND:
        raise ValueError(f"not a discard disagreement report: {path}")
    return {
        "path": str(path),
        "examples": payload["examples"],
        "max_per_category": payload["max_per_category"],
        "categories": {
            category_name: _summarize_disagreement_category(category_name, category)
            for category_name, category in payload["categories"].items()
        },
    }


def _summarize_disagreement_category(
    category_name: str,
    category: dict[str, Any],
) -> dict[str, Any]:
    correct_model, wrong_model = _disagreement_category_models(category_name)
    items = category.get("items", [])
    if not isinstance(items, list):
        raise ValueError(f"disagreement category items must be a list: {category_name}")

    bucket_counts: dict[str, dict[str, int | float | None]] = {}
    pair_counts: Counter[tuple[str, str, str]] = Counter()
    correct_margins: list[float] = []
    wrong_margins: list[float] = []

    for item in items:
        if not isinstance(item, dict):
            continue
        for bucket_name, enabled in item.get("defense_buckets", {}).items():
            stats = bucket_counts.setdefault(bucket_name, {"true": 0, "false": 0})
            stats["true" if enabled else "false"] += 1
        actual = str(item.get("actual_discard"))
        predictions = item.get("predictions", {})
        if isinstance(predictions, dict):
            correct_prediction = str(predictions.get(correct_model))
            wrong_prediction = str(predictions.get(wrong_model))
            pair_counts[(actual, correct_prediction, wrong_prediction)] += 1
            correct_margin = _correct_model_margin(item, correct_model, actual)
            wrong_margin = _wrong_model_margin(item, wrong_model, actual, wrong_prediction)
            if correct_margin is not None:
                correct_margins.append(correct_margin)
            if wrong_margin is not None:
                wrong_margins.append(wrong_margin)

    return {
        "count": category["count"],
        "stored_items": len(items),
        "correct_model": correct_model,
        "wrong_model": wrong_model,
        "defense_buckets": {
            bucket_name: _boolean_bucket_summary(stats)
            for bucket_name, stats in sorted(bucket_counts.items())
        },
        "actual_prediction_pairs": [
            {
                "actual": actual,
                "correct_prediction": correct_prediction,
                "wrong_prediction": wrong_prediction,
                "examples": count,
            }
            for (actual, correct_prediction, wrong_prediction), count in sorted(
                pair_counts.items(),
                key=lambda pair: (-pair[1], pair[0]),
            )
        ],
        "logit_margins": {
            "correct_model_actual_margin": _numeric_summary(correct_margins),
            "wrong_model_error_margin": _numeric_summary(wrong_margins),
        },
    }


def _summary_buckets(models: dict[str, Any]) -> dict[str, Any]:
    return {
        bucket_label: {
            model_name: _bucket_stats(
                models[model_name]["eval_analysis"],
                analysis_key=analysis_key,
                bucket=bucket,
            )
            for model_name in DISCARD_BENCHMARK_BUCKET_MODELS
            if model_name in models
        }
        for bucket_label, (analysis_key, bucket) in DISCARD_BENCHMARK_BUCKETS.items()
    }


def _bucket_stats(
    analysis: dict[str, Any],
    *,
    analysis_key: str,
    bucket: str,
) -> dict[str, int | float | None]:
    stats = analysis.get(analysis_key, {}).get(bucket)
    if stats is None:
        return {"accuracy": None, "correct": 0, "examples": 0}
    return {
        "accuracy": stats["accuracy"],
        "correct": stats["correct"],
        "examples": stats["examples"],
    }


def _discard_ablation(models: dict[str, Any]) -> dict[str, float | None]:
    return {
        "train_accuracy_lift_over_raw_count": _optional_delta(
            _model_metric(models, "linear", "train_accuracy"),
            _model_metric(models, "raw_count_linear", "train_accuracy"),
        ),
        "eval_accuracy_lift_over_raw_count": _optional_delta(
            _model_metric(models, "linear", "eval_accuracy"),
            _model_metric(models, "raw_count_linear", "eval_accuracy"),
        ),
        "risk_context_train_accuracy_lift_over_linear": _optional_delta(
            _model_metric(models, "risk_context_linear", "train_accuracy"),
            _model_metric(models, "linear", "train_accuracy"),
        ),
        "risk_context_eval_accuracy_lift_over_linear": _optional_delta(
            _model_metric(models, "risk_context_linear", "eval_accuracy"),
            _model_metric(models, "linear", "eval_accuracy"),
        ),
        "defense_context_train_accuracy_lift_over_risk_context": _optional_delta(
            _model_metric(models, "defense_context_linear", "train_accuracy"),
            _model_metric(models, "risk_context_linear", "train_accuracy"),
        ),
        "defense_context_eval_accuracy_lift_over_risk_context": _optional_delta(
            _model_metric(models, "defense_context_linear", "eval_accuracy"),
            _model_metric(models, "risk_context_linear", "eval_accuracy"),
        ),
        "defense_context_v1_train_accuracy_lift_over_defense_context": _optional_delta(
            _model_metric(models, "defense_context_v1_linear", "train_accuracy"),
            _model_metric(models, "defense_context_linear", "train_accuracy"),
        ),
        "defense_context_v1_eval_accuracy_lift_over_defense_context": _optional_delta(
            _model_metric(models, "defense_context_v1_linear", "eval_accuracy"),
            _model_metric(models, "defense_context_linear", "eval_accuracy"),
        ),
    }


def _model_metric(
    models: dict[str, Any],
    model_name: str,
    metric_name: str,
) -> float | None:
    model = models.get(model_name)
    if model is None:
        return None
    return model.get("metrics", {}).get(metric_name)


def _disagreement_category_models(category_name: str) -> tuple[str, str]:
    if category_name == "risk_correct_defense_wrong":
        return "risk_context_linear", "defense_context_linear"
    if category_name == "risk_correct_defense_v1_wrong":
        return "risk_context_linear", "defense_context_v1_linear"
    if category_name == "defense_correct_risk_wrong":
        return "defense_context_linear", "risk_context_linear"
    if category_name == "defense_v1_correct_risk_wrong":
        return "defense_context_v1_linear", "risk_context_linear"
    raise ValueError(f"unsupported disagreement category: {category_name}")


def _boolean_bucket_summary(stats: dict[str, int | float | None]) -> dict[str, int | float | None]:
    true_count = int(stats.get("true", 0) or 0)
    false_count = int(stats.get("false", 0) or 0)
    examples = true_count + false_count
    return {
        "true": true_count,
        "false": false_count,
        "examples": examples,
        "true_rate": None if examples == 0 else true_count / examples,
    }


def _correct_model_margin(
    item: dict[str, Any],
    model_name: str,
    actual: str,
) -> float | None:
    logits = _item_logits(item, model_name)
    if actual not in logits or len(logits) < 2:
        return None
    best_other = max(
        logit
        for tile, logit in logits.items()
        if tile != actual
    )
    return logits[actual] - best_other


def _wrong_model_margin(
    item: dict[str, Any],
    model_name: str,
    actual: str,
    wrong_prediction: str,
) -> float | None:
    logits = _item_logits(item, model_name)
    if actual not in logits or wrong_prediction not in logits:
        return None
    return logits[wrong_prediction] - logits[actual]


def _item_logits(item: dict[str, Any], model_name: str) -> dict[str, float]:
    candidate_logits = item.get("candidate_logits", {})
    if not isinstance(candidate_logits, dict):
        return {}
    model_logits = candidate_logits.get(model_name, [])
    if not isinstance(model_logits, list):
        return {}
    parsed: dict[str, float] = {}
    for entry in model_logits:
        if isinstance(entry, dict) and "tile" in entry and "logit" in entry:
            parsed[str(entry["tile"])] = float(entry["logit"])
    return parsed


def _numeric_summary(values: Sequence[float]) -> dict[str, float | int | None]:
    if not values:
        return {"count": 0, "min": None, "max": None, "mean": None}
    return {
        "count": len(values),
        "min": min(values),
        "max": max(values),
        "mean": sum(values) / len(values),
    }


def _summary_mean(summary: dict[str, Any]) -> float | None:
    mean = summary.get("mean")
    return None if mean is None else float(mean)


def _parse_failure_payload(failures: Sequence[TenhouParseFailure]) -> dict[str, Any]:
    return {
        "count": len(failures),
        "items": [
            {
                "path": str(failure.path),
                "error_type": failure.error_type,
                "message": failure.message,
            }
            for failure in failures
        ],
    }


def _optional_delta(left: float | None, right: float | None) -> float | None:
    if left is None or right is None:
        return None
    return left - right


def _metric(payload: dict[str, Any], key: str) -> float | None:
    value = payload.get(key)
    return None if value is None else float(value)


def _format_optional_float(value: float | None) -> str:
    return "n/a" if value is None else f"{value:.4f}"


def _format_optional_threshold(value: float | None) -> str:
    return "n/a" if value is None else f"{value:.2f}"


def _format_optional_delta(value: float | None) -> str:
    return "n/a" if value is None else f"{value:+.4f}"
