from __future__ import annotations

import json
from collections.abc import Sequence
from pathlib import Path
from typing import Any

from kenjaku.io import TenhouGame, TenhouParseFailure

DISCARD_BENCHMARK_REPORT_KIND = "kenjaku-discard-benchmark-report-v0"
DISCARD_BENCHMARK_SUMMARY_KIND = "kenjaku-discard-benchmark-summary-v0"
DISCARD_LINEAR_REPORT_KIND = "kenjaku-discard-linear-report-v0"
TENHOU_INSPECT_REPORT_KIND = "kenjaku-tenhou-inspect-report-v0"
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


def write_json_report(path: str | Path, payload: dict[str, Any]) -> None:
    report_path = Path(path)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(
        json.dumps(payload, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


def build_discard_benchmark_summary(paths: Sequence[Path]) -> dict[str, Any]:
    return {
        "kind": DISCARD_BENCHMARK_SUMMARY_KIND,
        "reports": [
            _summarize_discard_benchmark_report(path, _read_json_report(path))
            for path in paths
        ],
    }


def format_discard_benchmark_summary(summary: dict[str, Any]) -> str:
    if summary.get("kind") != DISCARD_BENCHMARK_SUMMARY_KIND:
        raise ValueError("not a discard benchmark summary")

    lines: list[str] = []
    for report in summary["reports"]:
        if lines:
            lines.append("")
        lines.append(f"report: {report['path']}")
        source = report["source"]
        source_label = source.get("label") or "unknown"
        source_date = source.get("date") or "unknown"
        lines.append(f"source: {source_label} ({source_date})")
        split = report["split"]
        lines.append(
            "examples: "
            f"{report['discard_examples']} total, "
            f"{split['train_examples']} train, "
            f"{split['eval_examples']} eval"
        )
        lines.append("models:")
        for model_name in DISCARD_BENCHMARK_MODEL_ORDER:
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
                stats = model_stats[model_name]
                accuracy = _format_optional_float(stats["accuracy"])
                parts.append(f"{model_name}={accuracy}/{stats['examples']}")
            lines.append(f"  {bucket_name}: " + ", ".join(parts))
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


def _read_json_report(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"report must be a JSON object: {path}")
    return payload


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


def _summary_buckets(models: dict[str, Any]) -> dict[str, Any]:
    return {
        bucket_label: {
            model_name: _bucket_stats(
                models[model_name]["eval_analysis"],
                analysis_key=analysis_key,
                bucket=bucket,
            )
            for model_name in DISCARD_BENCHMARK_BUCKET_MODELS
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


def _format_optional_float(value: float | None) -> str:
    return "n/a" if value is None else f"{value:.4f}"


def _format_optional_delta(value: float | None) -> str:
    return "n/a" if value is None else f"{value:+.4f}"
