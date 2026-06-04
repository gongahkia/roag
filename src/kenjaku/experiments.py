from __future__ import annotations

import json
from collections.abc import Sequence
from pathlib import Path
from typing import Any

from kenjaku.io import TenhouGame, TenhouParseFailure

DISCARD_BENCHMARK_REPORT_KIND = "kenjaku-discard-benchmark-report-v0"
DISCARD_LINEAR_REPORT_KIND = "kenjaku-discard-linear-report-v0"
TENHOU_INSPECT_REPORT_KIND = "kenjaku-tenhou-inspect-report-v0"


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
    discard_shanten: dict[str, int | float | None],
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
) -> dict[str, Any]:
    return {
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
                },
                "metrics": {
                    "train_accuracy": risk_context_linear_train_accuracy,
                    "eval_accuracy": risk_context_linear_eval_accuracy,
                },
                "eval_analysis": risk_context_linear_eval_analysis,
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
        },
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def write_json_report(path: str | Path, payload: dict[str, Any]) -> None:
    report_path = Path(path)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(
        json.dumps(payload, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


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
