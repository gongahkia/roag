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
    linear_epochs: int,
    linear_learning_rate: float,
    linear_train_accuracy: float,
    linear_eval_accuracy: float | None,
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
            },
            "linear": {
                "training": {
                    "epochs": linear_epochs,
                    "learning_rate": linear_learning_rate,
                },
                "metrics": {
                    "train_accuracy": linear_train_accuracy,
                    "eval_accuracy": linear_eval_accuracy,
                },
            },
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
