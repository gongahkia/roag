from __future__ import annotations

import json
from collections import Counter
from collections.abc import Sequence
from pathlib import Path
from typing import Any

from kenjaku.io import TenhouGame, TenhouParseFailure

CALL_BENCHMARK_REPORT_KIND = "kenjaku-call-benchmark-report-v0"
BENCHMARK_SUMMARY_KIND = "kenjaku-benchmark-summary-v0"
DISCARD_BENCHMARK_REPORT_KIND = "kenjaku-discard-benchmark-report-v0"
DISCARD_BENCHMARK_SUMMARY_KIND = "kenjaku-discard-benchmark-summary-v0"
DISCARD_DISAGREEMENT_REPORT_KIND = "kenjaku-discard-disagreements-v0"
DISCARD_DISAGREEMENT_SUMMARY_KIND = "kenjaku-discard-disagreement-summary-v0"
DISCARD_LINEAR_REPORT_KIND = "kenjaku-discard-linear-report-v0"
RIICHI_BENCHMARK_REPORT_KIND = "kenjaku-riichi-benchmark-report-v0"
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


def build_discard_benchmark_report_from_models(
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
        "models": models,
        "ablation": _discard_ablation(models),
        "discard_shanten": discard_shanten,
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def build_call_benchmark_report(
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
    parse_failures: Sequence[TenhouParseFailure],
    source: dict[str, str | None],
) -> dict[str, Any]:
    return {
        "kind": CALL_BENCHMARK_REPORT_KIND,
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
        "parse_failures": _parse_failure_payload(parse_failures),
    }


def build_riichi_benchmark_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
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
) -> dict[str, Any]:
    return {
        "kind": RIICHI_BENCHMARK_REPORT_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        **_game_counts(game),
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
        if all(report["target"] == "discard" for report in reports)
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
        elif report["target"] in {"call", "riichi"}:
            _append_binary_benchmark_summary_lines(lines, report)
        else:
            raise ValueError(f"unsupported benchmark summary target: {report['target']}")
    return "\n".join(lines)


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


def _read_json_report(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"report must be a JSON object: {path}")
    return payload


def _summarize_benchmark_report(path: Path, payload: dict[str, Any]) -> dict[str, Any]:
    kind = payload.get("kind")
    if kind == DISCARD_BENCHMARK_REPORT_KIND:
        return _summarize_discard_benchmark_report(path, payload)
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
    return {
        "path": str(path),
        "target": target,
        "report_kind": payload["kind"],
        "source": payload["source"],
        "xml_file_count": payload["xml_file_count"],
        "rounds": payload["rounds"],
        "examples": payload[examples_key],
        "split": payload["split"],
        "models": models,
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


def _format_optional_float(value: float | None) -> str:
    return "n/a" if value is None else f"{value:.4f}"


def _format_optional_delta(value: float | None) -> str:
    return "n/a" if value is None else f"{value:+.4f}"
