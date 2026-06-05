from __future__ import annotations

import argparse
import json
from collections.abc import Callable, Iterable, Sequence
from pathlib import Path
from typing import Any

from kenjaku import __version__
from kenjaku.core import ActionKind, Tile, TileType
from kenjaku.experiments import (
    build_call_benchmark_report,
    build_discard_benchmark_report_from_models,
    build_discard_benchmark_summary,
    build_discard_disagreement_summary,
    build_discard_linear_report,
    build_riichi_benchmark_report,
    build_tenhou_inspect_report,
    format_discard_benchmark_summary,
    format_discard_disagreement_summary,
    write_json_report,
)
from kenjaku.io import parse_tenhou_xml_dataset
from kenjaku.models import (
    CALL_DECISION_KINDS,
    CALL_LINEAR_V1_FEATURE_PROFILE,
    DEFENSE_CONTEXT_FEATURE_PROFILE,
    DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
    RAW_COUNT_FEATURE_PROFILE,
    RIICHI_DECISION_KINDS,
    RISK_CONTEXT_FEATURE_PROFILE,
    SHANTEN_FEATURE_PROFILE,
    CallFrequencyBaseline,
    CallLegalFrequencyBaseline,
    CallLinearModel,
    DiscardFrequencyBaseline,
    DiscardLinearModel,
    RiichiFrequencyBaseline,
    RiichiLinearModel,
)
from kenjaku.training import (
    actual_discard_has_kabe,
    actual_discard_has_one_chance,
    actual_discard_has_suji,
    actual_discard_is_genbutsu,
    actual_discard_seen_after_riichi,
    actual_discard_seen_before_riichi,
    CallExample,
    deterministic_split,
    discard_shanten_delta,
    DiscardExample,
    has_active_riichi_opponent,
    iter_call_examples,
    iter_discard_examples,
    iter_riichi_examples,
    RiichiExample,
    summarize_discard_predictions,
    summarize_discard_shanten,
)

DISCARD_BENCHMARK_MODEL_ORDER = (
    "frequency",
    "raw_count_linear",
    "linear",
    "risk_context_linear",
    "defense_context_linear",
    "defense_context_v1_linear",
)
DISCARD_BENCHMARK_FAST_MODELS = (
    "frequency",
    "linear",
    "risk_context_linear",
    "defense_context_linear",
)
DISCARD_LINEAR_FEATURE_PROFILES = {
    "raw_count_linear": RAW_COUNT_FEATURE_PROFILE,
    "linear": SHANTEN_FEATURE_PROFILE,
    "risk_context_linear": RISK_CONTEXT_FEATURE_PROFILE,
    "defense_context_linear": DEFENSE_CONTEXT_FEATURE_PROFILE,
    "defense_context_v1_linear": DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
}
DISAGREEMENT_REQUIRED_MODELS = (
    "risk_context_linear",
    "defense_context_linear",
    "defense_context_v1_linear",
)
DISAGREEMENT_TAGS = (
    "defense_signal",
    "efficiency_like",
    "close_logit",
    "active_riichi",
    "safe_tile_candidate",
    "no_obvious_signal",
)
DISAGREEMENT_SAFE_TILE_BUCKETS = (
    "genbutsu",
    "suji",
    "kabe",
    "one_chance",
    "seen_after_riichi",
)
DISAGREEMENT_CLOSE_LOGIT_MARGIN = 0.25


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="kenjaku",
        description="Riichi mahjong AI research toolkit.",
    )
    parser.add_argument("--version", action="store_true", help="print version and exit")
    subparsers = parser.add_subparsers(dest="command")

    inspect_tenhou = subparsers.add_parser(
        "inspect-tenhou",
        help="parse a Tenhou XML file and print Phase 0 dataset counts",
    )
    inspect_tenhou.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    inspect_tenhou.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON inspection report artifact",
    )
    inspect_tenhou.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_source_args(inspect_tenhou)
    inspect_tenhou.set_defaults(func=_inspect_tenhou)

    train_baseline = subparsers.add_parser(
        "train-discard-baseline",
        help="fit the deterministic discard frequency baseline on one Tenhou XML file",
    )
    train_baseline.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    train_baseline.add_argument(
        "--skip-errors",
        action="store_true",
        help="skip files that fail Tenhou XML parsing",
    )
    train_baseline.set_defaults(func=_train_discard_baseline)

    train_linear = subparsers.add_parser(
        "train-discard-linear",
        help="fit the tiny dependency-free linear discard model on one Tenhou XML file",
    )
    train_linear.add_argument("paths", nargs="+", type=Path, help="Tenhou XML files or directories")
    train_linear.add_argument("--epochs", type=int, default=25, help="training epochs")
    train_linear.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="SGD learning rate",
    )
    train_linear.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="L2 regularization strength",
    )
    train_linear.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    train_linear.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    train_linear.add_argument(
        "--output",
        type=Path,
        help="optional path for the trained JSON model artifact",
    )
    train_linear.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON training report artifact",
    )
    train_linear.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_source_args(train_linear)
    train_linear.set_defaults(func=_train_discard_linear)

    benchmark_discard = subparsers.add_parser(
        "benchmark-discard",
        help="compare deterministic discard baselines on one train/eval split",
    )
    benchmark_discard.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_discard.add_argument("--epochs", type=int, default=25, help="linear model epochs")
    benchmark_discard.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="linear model SGD learning rate",
    )
    benchmark_discard.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="linear model L2 regularization strength",
    )
    benchmark_discard.add_argument(
        "--models",
        default="all",
        help=(
            "discard benchmark models: all, fast, or comma-separated model names "
            "(frequency, raw_count_linear, linear, risk_context_linear, "
            "defense_context_linear, defense_context_v1_linear)"
        ),
    )
    benchmark_discard.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_discard.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_discard.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON benchmark report artifact",
    )
    benchmark_discard.add_argument(
        "--disagreements",
        type=Path,
        help="optional path for a JSON model-disagreement diagnostic artifact",
    )
    benchmark_discard.add_argument(
        "--max-disagreements",
        type=int,
        default=100,
        help="maximum stored examples per disagreement category",
    )
    benchmark_discard.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_source_args(benchmark_discard)
    benchmark_discard.set_defaults(func=_benchmark_discard)

    benchmark_summary = subparsers.add_parser(
        "benchmark-report-summary",
        help="summarize one or more discard benchmark JSON reports",
    )
    benchmark_summary.add_argument(
        "reports",
        nargs="+",
        type=Path,
        help="discard benchmark report JSON files",
    )
    benchmark_summary.add_argument(
        "--json",
        action="store_true",
        help="emit the summary as JSON instead of text",
    )
    benchmark_summary.set_defaults(func=_benchmark_report_summary)

    disagreement_summary = subparsers.add_parser(
        "disagreement-report-summary",
        help="summarize one or more discard disagreement JSON reports",
    )
    disagreement_summary.add_argument(
        "reports",
        nargs="+",
        type=Path,
        help="discard disagreement report JSON files",
    )
    disagreement_summary.add_argument(
        "--json",
        action="store_true",
        help="emit the summary as JSON instead of text",
    )
    disagreement_summary.add_argument(
        "--examples",
        type=int,
        default=0,
        help="append this many stored representative examples per category in text mode",
    )
    disagreement_summary.add_argument(
        "--tags",
        action="store_true",
        help="append deterministic disagreement tag counts from stored examples",
    )
    disagreement_summary.set_defaults(func=_disagreement_report_summary)

    benchmark_call = subparsers.add_parser(
        "benchmark-call",
        help="compare deterministic call/pass baselines on one train/eval split",
    )
    benchmark_call.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_call.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_call.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_call.add_argument("--epochs", type=int, default=25, help="call linear model epochs")
    benchmark_call.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="call linear model SGD learning rate",
    )
    benchmark_call.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="call linear model L2 regularization strength",
    )
    benchmark_call.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON call benchmark report artifact",
    )
    benchmark_call.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_source_args(benchmark_call)
    benchmark_call.set_defaults(func=_benchmark_call)

    benchmark_riichi = subparsers.add_parser(
        "benchmark-riichi",
        help="compare deterministic riichi/pass baselines on one train/eval split",
    )
    benchmark_riichi.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_riichi.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_riichi.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_riichi.add_argument("--epochs", type=int, default=25, help="riichi linear model epochs")
    benchmark_riichi.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="riichi linear model SGD learning rate",
    )
    benchmark_riichi.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="riichi linear model L2 regularization strength",
    )
    benchmark_riichi.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON riichi benchmark report artifact",
    )
    benchmark_riichi.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_source_args(benchmark_riichi)
    benchmark_riichi.set_defaults(func=_benchmark_riichi)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.version:
        print(f"kenjaku {__version__}")
        return 0

    if hasattr(args, "func"):
        return args.func(args)

    parser.print_help()
    return 0


def _inspect_tenhou(args: argparse.Namespace) -> int:
    dataset = parse_tenhou_xml_dataset(args.paths, skip_errors=args.skip_errors)
    game = dataset.game
    discards = sum(len(round_.discards) for round_ in game.rounds)
    discard_examples = list(iter_discard_examples(game))
    call_examples = sum(1 for _ in iter_call_examples(game))

    print(f"rounds: {len(game.rounds)}")
    print(f"discards: {discards}")
    print(f"discard_examples: {len(discard_examples)}")
    print(f"call_examples: {call_examples}")
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.report is not None:
        report = build_tenhou_inspect_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(discard_examples),
            call_examples=call_examples,
            discard_shanten=summarize_discard_shanten(discard_examples),
            parse_failures=dataset.failures,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _train_discard_baseline(args: argparse.Namespace) -> int:
    game = parse_tenhou_xml_dataset(args.paths, skip_errors=args.skip_errors).game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    model = DiscardFrequencyBaseline.fit(examples)
    print(f"examples: {len(examples)}")
    print(f"top_discard: {model.top_tile.notation}")
    print(f"training_accuracy: {model.score(examples):.4f}")
    return 0


def _train_discard_linear(args: argparse.Namespace) -> int:
    dataset = parse_tenhou_xml_dataset(args.paths, skip_errors=args.skip_errors)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
        l2=args.l2,
    )
    train_accuracy = model.score(train_examples)
    eval_accuracy = model.score(eval_examples) if eval_examples else None

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"train_accuracy: {train_accuracy:.4f}")
    print(f"eval_accuracy: {_format_optional_accuracy(eval_accuracy)}")
    if args.output is not None:
        model.save(args.output)
        print(f"model_path: {args.output}")
    if args.report is not None:
        report = build_discard_linear_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            model_kind=model.kind,
            feature_dim=model.feature_dim,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            train_accuracy=train_accuracy,
            eval_accuracy=eval_accuracy,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            model_path=args.output,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _benchmark_discard(args: argparse.Namespace) -> int:
    if args.max_disagreements < 0:
        raise SystemExit("--max-disagreements must be non-negative")
    selected_model_names = _parse_discard_benchmark_models(args.models)
    if args.disagreements is not None:
        missing = [
            model_name
            for model_name in DISAGREEMENT_REQUIRED_MODELS
            if model_name not in selected_model_names
        ]
        if missing:
            raise SystemExit(
                "--disagreements requires selected models: " + ", ".join(missing)
            )
    dataset = parse_tenhou_xml_dataset(args.paths, skip_errors=args.skip_errors)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    model_payloads: dict[str, dict[str, Any]] = {}
    linear_models: dict[str, DiscardLinearModel] = {}

    if "frequency" in selected_model_names:
        frequency_model = DiscardFrequencyBaseline.fit(train_examples)
        model_payloads["frequency"] = _discard_frequency_payload(
            frequency_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            include_analysis=args.report is not None,
        )

    for model_name in selected_model_names:
        if model_name not in DISCARD_LINEAR_FEATURE_PROFILES:
            continue
        model = DiscardLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile=DISCARD_LINEAR_FEATURE_PROFILES[model_name],
        )
        linear_models[model_name] = model
        model_payloads[model_name] = _discard_linear_payload(
            model_name,
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            include_analysis=args.report is not None,
        )

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_discard_benchmark_metrics(model_payloads)
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.report is not None:
        report = build_discard_benchmark_report_from_models(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    if args.disagreements is not None:
        disagreement_report = _build_disagreement_report(
            eval_examples,
            linear_models=linear_models,
            max_per_category=args.max_disagreements,
        )
        write_json_report(args.disagreements, disagreement_report)
        print(f"disagreements_path: {args.disagreements}")
    return 0


def _parse_discard_benchmark_models(value: str) -> tuple[str, ...]:
    if value == "all":
        return DISCARD_BENCHMARK_MODEL_ORDER
    if value == "fast":
        return DISCARD_BENCHMARK_FAST_MODELS

    selected: list[str] = []
    for raw_name in value.split(","):
        model_name = raw_name.strip()
        if not model_name:
            continue
        if model_name not in DISCARD_BENCHMARK_MODEL_ORDER:
            raise SystemExit(f"unsupported discard benchmark model: {model_name}")
        if model_name not in selected:
            selected.append(model_name)
    if not selected:
        raise SystemExit("--models must select at least one model")
    return tuple(selected)


def _discard_frequency_payload(
    model: DiscardFrequencyBaseline,
    *,
    train_examples: list[DiscardExample],
    eval_examples: list[DiscardExample],
    include_analysis: bool,
) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "metrics": {
            "train_accuracy": model.score(train_examples),
            "eval_accuracy": model.score(eval_examples) if eval_examples else None,
        },
    }
    if include_analysis:
        payload["eval_analysis"] = summarize_discard_predictions(
            eval_examples,
            lambda example: model.predict(example.hand_counts),
        )
    return payload


def _discard_linear_payload(
    model_name: str,
    model: DiscardLinearModel,
    *,
    train_examples: list[DiscardExample],
    eval_examples: list[DiscardExample],
    epochs: int,
    learning_rate: float,
    l2: float,
    include_analysis: bool,
) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "kind": model.kind,
        "feature_dim": model.feature_dim,
        "training": {
            "epochs": epochs,
            "learning_rate": learning_rate,
            "l2": l2,
        },
        "metrics": {
            "train_accuracy": model.score(train_examples),
            "eval_accuracy": model.score(eval_examples) if eval_examples else None,
        },
    }
    if include_analysis:
        payload["eval_analysis"] = summarize_discard_predictions(
            eval_examples,
            lambda example: _predict_discard_model(model_name, model, example),
        )
        payload["weight_summary"] = model.weight_summary()
        payload["feature_summary"] = model.feature_summary(eval_examples)
    return payload


def _predict_discard_model(
    model_name: str,
    model: DiscardLinearModel,
    example: DiscardExample,
) -> TileType:
    if model_name in {"raw_count_linear", "linear"}:
        return model.predict(example.hand_counts, example.visible_counts)
    if model_name == "risk_context_linear":
        return model.predict(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
        )
    if model_name == "defense_context_linear":
        return model.predict(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
            rivers_by_seat=example.rivers_by_seat,
            riichi_declared_turns=example.riichi_declared_turns,
            riichi_declared_event_indices=example.riichi_declared_event_indices,
        )
    if model_name == "defense_context_v1_linear":
        return model.predict(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
            rivers_by_seat=example.rivers_by_seat,
            riichi_declared_turns=example.riichi_declared_turns,
            riichi_declared_event_indices=example.riichi_declared_event_indices,
            meld_counts_by_seat=example.meld_counts_by_seat,
            dora_indicators=example.dora_indicators,
            last_discard_tsumogiri_by_seat=example.last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=example.ippatsu_active_seats,
        )
    raise ValueError(f"unsupported discard linear model: {model_name}")


def _print_discard_benchmark_metrics(model_payloads: dict[str, dict[str, Any]]) -> None:
    for model_name in DISCARD_BENCHMARK_MODEL_ORDER:
        if model_name not in model_payloads:
            continue
        metrics = model_payloads[model_name]["metrics"]
        train_accuracy = metrics["train_accuracy"]
        eval_accuracy = metrics["eval_accuracy"]
        print(f"{model_name}_train_accuracy: {train_accuracy:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(eval_accuracy)}")
        if model_name == "linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "raw_count_linear"),
            )
            print(f"linear_eval_lift_over_raw_count: {_format_optional_delta(delta)}")
        elif model_name == "risk_context_linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "linear"),
            )
            print(f"risk_context_linear_eval_lift_over_linear: {_format_optional_delta(delta)}")
        elif model_name == "defense_context_linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "risk_context_linear"),
            )
            print(
                "defense_context_linear_eval_lift_over_risk_context: "
                f"{_format_optional_delta(delta)}"
            )
        elif model_name == "defense_context_v1_linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "defense_context_linear"),
            )
            print(
                "defense_context_v1_linear_eval_lift_over_defense_context: "
                f"{_format_optional_delta(delta)}"
            )


def _model_eval_accuracy(
    model_payloads: dict[str, dict[str, Any]],
    model_name: str,
) -> float | None:
    model = model_payloads.get(model_name)
    if model is None:
        return None
    return model["metrics"]["eval_accuracy"]


def _benchmark_report_summary(args: argparse.Namespace) -> int:
    summary = build_discard_benchmark_summary(args.reports)
    if args.json:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(format_discard_benchmark_summary(summary))
    return 0


def _disagreement_report_summary(args: argparse.Namespace) -> int:
    if args.examples < 0:
        raise SystemExit("--examples must be non-negative")
    summary = build_discard_disagreement_summary(args.reports)
    tag_summary = _build_disagreement_tag_summary(args.reports) if args.tags else None
    if tag_summary is not None:
        summary["tags"] = tag_summary
    if args.json:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(format_discard_disagreement_summary(summary))
        if tag_summary is not None:
            print()
            print(_format_disagreement_tag_summary(tag_summary))
        if args.examples:
            print()
            print(
                _format_disagreement_examples(
                    args.reports,
                    args.examples,
                    include_tags=args.tags,
                )
            )
    return 0


def _format_disagreement_examples(
    paths: Sequence[Path],
    examples_per_category: int,
    *,
    include_tags: bool = False,
) -> str:
    lines: list[str] = ["examples:"]
    for path in paths:
        payload = json.loads(path.read_text(encoding="utf-8"))
        lines.append(f"report: {path}")
        categories = payload.get("categories", {})
        if not isinstance(categories, dict):
            continue
        for category_name, category in categories.items():
            if not isinstance(category, dict):
                continue
            correct_model, wrong_model = _disagreement_category_models(category_name)
            items = category.get("items", [])
            if not isinstance(items, list) or not items:
                continue
            lines.append(f"{category_name}:")
            for index, item in enumerate(items[:examples_per_category], start=1):
                if not isinstance(item, dict):
                    continue
                lines.extend(
                    _format_disagreement_item(
                        item,
                        index=index,
                        correct_model=correct_model,
                        wrong_model=wrong_model,
                        include_tags=include_tags,
                    )
                )
    return "\n".join(lines)


def _format_disagreement_item(
    item: dict[str, Any],
    *,
    index: int,
    correct_model: str,
    wrong_model: str,
    include_tags: bool = False,
) -> list[str]:
    predictions = item.get("predictions", {})
    if not isinstance(predictions, dict):
        predictions = {}
    actual = item.get("actual_discard", "unknown")
    lines = [
        (
            f"  {index}. round={item.get('round_index', 'n/a')} "
            f"event={item.get('event_index', 'n/a')} seat={item.get('seat', 'n/a')} "
            f"actual={actual} "
            f"correct={correct_model}:{predictions.get(correct_model, 'n/a')} "
            f"wrong={wrong_model}:{predictions.get(wrong_model, 'n/a')}"
        )
    ]
    buckets = item.get("defense_buckets", {})
    if isinstance(buckets, dict) and buckets:
        bucket_parts = [
            f"{name}={'yes' if enabled else 'no'}"
            for name, enabled in sorted(buckets.items())
        ]
        lines.append("     buckets: " + ", ".join(bucket_parts))
    if include_tags:
        tags = _disagreement_item_tags(item, correct_model=correct_model, wrong_model=wrong_model)
        lines.append("     tags: " + ", ".join(tags))
    logit_parts = [
        _format_top_logits(item, model_name)
        for model_name in (correct_model, wrong_model)
    ]
    logit_parts = [part for part in logit_parts if part]
    if logit_parts:
        lines.append("     logits: " + "; ".join(logit_parts))
    return lines


def _build_disagreement_tag_summary(paths: Sequence[Path]) -> dict[str, Any]:
    reports: list[dict[str, Any]] = []
    for path in paths:
        payload = json.loads(path.read_text(encoding="utf-8"))
        report_overall = _empty_disagreement_tag_counts()
        report_categories: dict[str, dict[str, int]] = {}
        categories = payload.get("categories", {})
        if isinstance(categories, dict):
            for category_name, category in categories.items():
                if not isinstance(category, dict):
                    continue
                counts = _empty_disagreement_tag_counts()
                correct_model, wrong_model = _disagreement_category_models(category_name)
                items = category.get("items", [])
                if isinstance(items, list):
                    for item in items:
                        if not isinstance(item, dict):
                            continue
                        tags = _disagreement_item_tags(
                            item,
                            correct_model=correct_model,
                            wrong_model=wrong_model,
                        )
                        _record_disagreement_tags(counts, tags)
                        _record_disagreement_tags(report_overall, tags)
                report_categories[category_name] = counts
        reports.append(
            {
                "path": str(path),
                "categories": report_categories,
                "overall": report_overall,
            }
        )
    return {
        "kind": "kenjaku-discard-disagreement-tags-v0",
        "reports": reports,
    }


def _format_disagreement_tag_summary(summary: dict[str, Any]) -> str:
    lines = ["tags:"]
    for report in summary.get("reports", []):
        if not isinstance(report, dict):
            continue
        lines.append(f"report: {report.get('path', 'unknown')}")
        categories = report.get("categories", {})
        if isinstance(categories, dict):
            for category_name, counts in categories.items():
                if isinstance(counts, dict):
                    lines.append(f"{category_name}: {_format_disagreement_tag_counts(counts)}")
        overall = report.get("overall", {})
        if isinstance(overall, dict):
            lines.append(f"overall: {_format_disagreement_tag_counts(overall)}")
    return "\n".join(lines)


def _format_disagreement_tag_counts(counts: dict[str, Any]) -> str:
    keys = ("stored", *DISAGREEMENT_TAGS)
    return " ".join(f"{key}={int(counts.get(key, 0))}" for key in keys)


def _empty_disagreement_tag_counts() -> dict[str, int]:
    return {
        "stored": 0,
        **{tag: 0 for tag in DISAGREEMENT_TAGS},
    }


def _record_disagreement_tags(counts: dict[str, int], tags: tuple[str, ...]) -> None:
    counts["stored"] += 1
    for tag in tags:
        counts[tag] += 1


def _disagreement_item_tags(
    item: dict[str, Any],
    *,
    correct_model: str,
    wrong_model: str,
) -> tuple[str, ...]:
    buckets = item.get("defense_buckets", {})
    if not isinstance(buckets, dict):
        buckets = {}
    active_riichi = bool(buckets.get("active_riichi_opponent"))
    safe_tile_candidate = any(bool(buckets.get(name)) for name in DISAGREEMENT_SAFE_TILE_BUCKETS)
    defense_signal = active_riichi and safe_tile_candidate
    efficiency_like = _disagreement_preserves_efficiency(item)
    close_logit = _disagreement_close_logit(
        item,
        correct_model=correct_model,
        wrong_model=wrong_model,
    )

    tags: list[str] = []
    if defense_signal:
        tags.append("defense_signal")
    if efficiency_like:
        tags.append("efficiency_like")
    if close_logit:
        tags.append("close_logit")
    if active_riichi:
        tags.append("active_riichi")
    if safe_tile_candidate:
        tags.append("safe_tile_candidate")
    if not tags:
        tags.append("no_obvious_signal")
    return tuple(tags)


def _disagreement_preserves_efficiency(item: dict[str, Any]) -> bool:
    shanten_delta = item.get("shanten_delta", {})
    if not isinstance(shanten_delta, dict):
        return False
    try:
        return float(shanten_delta.get("delta", 1.0)) <= 0.0
    except (TypeError, ValueError):
        return False


def _disagreement_close_logit(
    item: dict[str, Any],
    *,
    correct_model: str,
    wrong_model: str,
) -> bool:
    actual = item.get("actual_discard")
    if not isinstance(actual, str):
        return False
    predictions = item.get("predictions", {})
    if not isinstance(predictions, dict):
        predictions = {}
    return (
        _model_actual_margin_is_close(item, correct_model, actual)
        or _model_error_margin_is_close(
            item,
            wrong_model,
            actual,
            prediction=predictions.get(wrong_model),
        )
    )


def _model_actual_margin_is_close(
    item: dict[str, Any],
    model_name: str,
    actual: str,
) -> bool:
    logits = _logits_by_tile(item, model_name)
    if actual not in logits or len(logits) < 2:
        return False
    best_other = max(logit for tile, logit in logits.items() if tile != actual)
    return abs(logits[actual] - best_other) <= DISAGREEMENT_CLOSE_LOGIT_MARGIN


def _model_error_margin_is_close(
    item: dict[str, Any],
    model_name: str,
    actual: str,
    *,
    prediction: Any,
) -> bool:
    if not isinstance(prediction, str):
        return False
    logits = _logits_by_tile(item, model_name)
    if actual not in logits or prediction not in logits:
        return False
    return abs(logits[prediction] - logits[actual]) <= DISAGREEMENT_CLOSE_LOGIT_MARGIN


def _logits_by_tile(item: dict[str, Any], model_name: str) -> dict[str, float]:
    candidate_logits = item.get("candidate_logits", {})
    if not isinstance(candidate_logits, dict):
        return {}
    entries = candidate_logits.get(model_name, [])
    if not isinstance(entries, list):
        return {}
    parsed: dict[str, float] = {}
    for entry in entries:
        if not isinstance(entry, dict) or "tile" not in entry or "logit" not in entry:
            continue
        try:
            parsed[str(entry["tile"])] = float(entry["logit"])
        except (TypeError, ValueError):
            continue
    return parsed


def _format_top_logits(item: dict[str, Any], model_name: str) -> str:
    candidate_logits = item.get("candidate_logits", {})
    if not isinstance(candidate_logits, dict):
        return ""
    entries = candidate_logits.get(model_name, [])
    if not isinstance(entries, list):
        return ""
    parsed = [
        (str(entry["tile"]), float(entry["logit"]))
        for entry in entries
        if isinstance(entry, dict) and "tile" in entry and "logit" in entry
    ]
    if not parsed:
        return ""
    top = sorted(parsed, key=lambda value: (-value[1], value[0]))[:3]
    return f"{model_name} " + " ".join(
        f"{tile}={logit:.4f}"
        for tile, logit in top
    )


def _disagreement_category_models(category_name: str) -> tuple[str, str]:
    if category_name == "risk_correct_defense_wrong":
        return "risk_context_linear", "defense_context_linear"
    if category_name == "risk_correct_defense_v1_wrong":
        return "risk_context_linear", "defense_context_v1_linear"
    if category_name == "defense_correct_risk_wrong":
        return "defense_context_linear", "risk_context_linear"
    if category_name == "defense_v1_correct_risk_wrong":
        return "defense_context_v1_linear", "risk_context_linear"
    return "correct_model", "wrong_model"


def _benchmark_call(args: argparse.Namespace) -> int:
    dataset = parse_tenhou_xml_dataset(args.paths, skip_errors=args.skip_errors)
    game = dataset.game
    examples = list(iter_call_examples(game))
    if not examples:
        raise SystemExit("no call examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    call_models = {
        "call_frequency": CallFrequencyBaseline.fit(train_examples),
        "call_legal_frequency": CallLegalFrequencyBaseline.fit(train_examples),
        "call_linear": CallLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
        ),
        "call_linear_v1": CallLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
        ),
    }
    model_payloads = {
        model_name: _call_model_payload(
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
        )
        for model_name, model in call_models.items()
    }

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_call_benchmark_metrics(model_payloads)
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.report is not None:
        discard_examples = list(iter_discard_examples(game))
        report = build_call_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(discard_examples),
            call_examples=len(examples),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            parse_failures=dataset.failures,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


CallPredictor = Callable[[CallExample], ActionKind]


def _call_model_payload(
    model: CallFrequencyBaseline | CallLegalFrequencyBaseline | CallLinearModel,
    *,
    train_examples: list[CallExample],
    eval_examples: list[CallExample],
    epochs: int,
    learning_rate: float,
    l2: float,
) -> dict[str, Any]:
    train_analysis = _summarize_call_predictions(train_examples, model.predict)
    eval_analysis = _summarize_call_predictions(eval_examples, model.predict)
    train_metrics = _call_metrics(train_analysis)
    eval_metrics = _call_metrics(eval_analysis)
    payload: dict[str, Any] = {
        "kind": model.kind,
        "metrics": {
            "train_accuracy": train_metrics["accuracy"],
            "eval_accuracy": eval_metrics["accuracy"],
            "train_balanced_accuracy": train_metrics["balanced_accuracy"],
            "eval_balanced_accuracy": eval_metrics["balanced_accuracy"],
            "train_macro_recall": train_metrics["macro_recall"],
            "eval_macro_recall": eval_metrics["macro_recall"],
            "train_pass_recall": train_metrics["pass_recall"],
            "eval_pass_recall": eval_metrics["pass_recall"],
            "train_call_recall": train_metrics["call_recall"],
            "eval_call_recall": eval_metrics["call_recall"],
            "train_action_recall": train_metrics["action_recall"],
            "eval_action_recall": eval_metrics["action_recall"],
        },
        "train_analysis": train_analysis,
        "eval_analysis": eval_analysis,
    }
    if isinstance(model, CallLinearModel):
        payload["feature_dim"] = model.feature_dim
        payload["feature_profile"] = model.feature_profile
        payload["training"] = {
            "epochs": epochs,
            "learning_rate": learning_rate,
            "l2": l2,
        }
    else:
        payload["counts"] = model.count_by_kind()
    return payload


def _print_call_benchmark_metrics(model_payloads: dict[str, dict[str, Any]]) -> None:
    for model_name, payload in model_payloads.items():
        metrics = payload["metrics"]
        print(f"{model_name}_train_accuracy: {metrics['train_accuracy']:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(metrics['eval_accuracy'])}")
        print(
            f"{model_name}_eval_balanced_accuracy: "
            f"{_format_optional_accuracy(metrics['eval_balanced_accuracy'])}"
        )
        print(
            f"{model_name}_eval_pass_recall: "
            f"{_format_optional_accuracy(metrics['eval_pass_recall'])}"
        )
        print(
            f"{model_name}_eval_call_recall: "
            f"{_format_optional_accuracy(metrics['eval_call_recall'])}"
        )


def _summarize_call_predictions(
    examples: Sequence[CallExample],
    predict: CallPredictor,
) -> dict[str, Any]:
    buckets: dict[str, Any] = {
        "overall": _empty_call_bucket(),
        "by_call_or_pass": {
            "pass": _empty_call_bucket(),
            "call": _empty_call_bucket(),
        },
        "by_actual_action": {
            kind.value: _empty_call_bucket()
            for kind in CALL_DECISION_KINDS
        },
        "by_legal_call_kinds": {},
    }
    action_distribution = {
        kind.value: 0
        for kind in CALL_DECISION_KINDS
    }

    for example in examples:
        actual = example.action.kind
        prediction = predict(example)
        correct = prediction == actual
        action_distribution[actual.value] += 1
        call_or_pass = "pass" if actual == ActionKind.PASS else "call"
        legal_key = _legal_call_key(example)
        legal_buckets = buckets["by_legal_call_kinds"]
        legal_buckets.setdefault(legal_key, _empty_call_bucket())

        _record_call_bucket(buckets["overall"], correct)
        _record_call_bucket(buckets["by_call_or_pass"][call_or_pass], correct)
        _record_call_bucket(buckets["by_actual_action"][actual.value], correct)
        _record_call_bucket(legal_buckets[legal_key], correct)

    return {
        "action_distribution": action_distribution,
        **_finalize_call_buckets(buckets),
    }


def _call_metrics(analysis: dict[str, Any]) -> dict[str, Any]:
    action_recall = {
        kind.value: analysis["by_actual_action"][kind.value]["accuracy"]
        for kind in CALL_DECISION_KINDS
    }
    pass_recall = analysis["by_call_or_pass"]["pass"]["accuracy"]
    call_recall = analysis["by_call_or_pass"]["call"]["accuracy"]
    return {
        "accuracy": analysis["overall"]["accuracy"],
        "balanced_accuracy": _mean_defined((pass_recall, call_recall)),
        "macro_recall": _mean_defined(action_recall.values()),
        "pass_recall": pass_recall,
        "call_recall": call_recall,
        "action_recall": action_recall,
    }


def _mean_defined(values: Iterable[float | None]) -> float | None:
    defined = [value for value in values if value is not None]
    if not defined:
        return None
    return sum(defined) / len(defined)


def _benchmark_riichi(args: argparse.Namespace) -> int:
    dataset = parse_tenhou_xml_dataset(args.paths, skip_errors=args.skip_errors)
    game = dataset.game
    examples = list(iter_riichi_examples(game))
    if not examples:
        raise SystemExit("no riichi examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    riichi_models = {
        "riichi_frequency": RiichiFrequencyBaseline.fit(train_examples),
        "riichi_linear": RiichiLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
        ),
    }
    model_payloads = {
        model_name: _riichi_model_payload(
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
        )
        for model_name, model in riichi_models.items()
    }

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_riichi_benchmark_metrics(model_payloads)
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.report is not None:
        discard_examples = list(iter_discard_examples(game))
        call_examples = list(iter_call_examples(game))
        report = build_riichi_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(discard_examples),
            call_examples=len(call_examples),
            riichi_examples=len(examples),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            parse_failures=dataset.failures,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


RiichiPredictor = Callable[[RiichiExample], ActionKind]


def _riichi_model_payload(
    model: RiichiFrequencyBaseline | RiichiLinearModel,
    *,
    train_examples: list[RiichiExample],
    eval_examples: list[RiichiExample],
    epochs: int,
    learning_rate: float,
    l2: float,
) -> dict[str, Any]:
    train_analysis = _summarize_riichi_predictions(train_examples, model.predict)
    eval_analysis = _summarize_riichi_predictions(eval_examples, model.predict)
    train_metrics = _riichi_metrics(train_analysis)
    eval_metrics = _riichi_metrics(eval_analysis)
    payload: dict[str, Any] = {
        "kind": model.kind,
        "metrics": {
            "train_accuracy": train_metrics["accuracy"],
            "eval_accuracy": eval_metrics["accuracy"],
            "train_balanced_accuracy": train_metrics["balanced_accuracy"],
            "eval_balanced_accuracy": eval_metrics["balanced_accuracy"],
            "train_pass_recall": train_metrics["pass_recall"],
            "eval_pass_recall": eval_metrics["pass_recall"],
            "train_riichi_recall": train_metrics["riichi_recall"],
            "eval_riichi_recall": eval_metrics["riichi_recall"],
            "train_action_recall": train_metrics["action_recall"],
            "eval_action_recall": eval_metrics["action_recall"],
        },
        "train_analysis": train_analysis,
        "eval_analysis": eval_analysis,
    }
    if isinstance(model, RiichiLinearModel):
        payload["feature_dim"] = model.feature_dim
        payload["training"] = {
            "epochs": epochs,
            "learning_rate": learning_rate,
            "l2": l2,
        }
    else:
        payload["counts"] = model.count_by_kind()
    return payload


def _print_riichi_benchmark_metrics(model_payloads: dict[str, dict[str, Any]]) -> None:
    for model_name, payload in model_payloads.items():
        metrics = payload["metrics"]
        print(f"{model_name}_train_accuracy: {metrics['train_accuracy']:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(metrics['eval_accuracy'])}")
        print(
            f"{model_name}_eval_balanced_accuracy: "
            f"{_format_optional_accuracy(metrics['eval_balanced_accuracy'])}"
        )
        print(
            f"{model_name}_eval_pass_recall: "
            f"{_format_optional_accuracy(metrics['eval_pass_recall'])}"
        )
        print(
            f"{model_name}_eval_riichi_recall: "
            f"{_format_optional_accuracy(metrics['eval_riichi_recall'])}"
        )


def _summarize_riichi_predictions(
    examples: Sequence[RiichiExample],
    predict: RiichiPredictor,
) -> dict[str, Any]:
    buckets: dict[str, Any] = {
        "overall": _empty_call_bucket(),
        "by_actual_action": {
            kind.value: _empty_call_bucket()
            for kind in RIICHI_DECISION_KINDS
        },
    }
    action_distribution = {
        kind.value: 0
        for kind in RIICHI_DECISION_KINDS
    }

    for example in examples:
        actual = example.action.kind
        prediction = predict(example)
        correct = prediction == actual
        action_distribution[actual.value] += 1
        _record_call_bucket(buckets["overall"], correct)
        _record_call_bucket(buckets["by_actual_action"][actual.value], correct)

    return {
        "action_distribution": action_distribution,
        **_finalize_call_buckets(buckets),
    }


def _riichi_metrics(analysis: dict[str, Any]) -> dict[str, Any]:
    action_recall = {
        kind.value: analysis["by_actual_action"][kind.value]["accuracy"]
        for kind in RIICHI_DECISION_KINDS
    }
    pass_recall = action_recall[ActionKind.PASS.value]
    riichi_recall = action_recall[ActionKind.RIICHI.value]
    return {
        "accuracy": analysis["overall"]["accuracy"],
        "balanced_accuracy": _mean_defined((pass_recall, riichi_recall)),
        "pass_recall": pass_recall,
        "riichi_recall": riichi_recall,
        "action_recall": action_recall,
    }


def _legal_call_key(example: CallExample) -> str:
    if not example.legal_call_kinds:
        return "none"
    return ",".join(kind.value for kind in example.legal_call_kinds)


def _empty_call_bucket() -> dict[str, int | float | None]:
    return {
        "examples": 0,
        "correct": 0,
        "accuracy": None,
    }


def _record_call_bucket(bucket: dict[str, int | float | None], correct: bool) -> None:
    bucket["examples"] = int(bucket["examples"] or 0) + 1
    bucket["correct"] = int(bucket["correct"] or 0) + int(correct)


def _finalize_call_buckets(value: Any) -> Any:
    if _is_call_bucket(value):
        examples = int(value["examples"])
        correct = int(value["correct"])
        return {
            "examples": examples,
            "correct": correct,
            "accuracy": None if examples == 0 else correct / examples,
        }
    if isinstance(value, dict):
        return {
            key: _finalize_call_buckets(child)
            for key, child in value.items()
        }
    return value


def _is_call_bucket(value: Any) -> bool:
    return (
        isinstance(value, dict)
        and set(value) == {"examples", "correct", "accuracy"}
    )


def _build_disagreement_report(
    examples: Sequence[DiscardExample],
    *,
    linear_models: dict[str, DiscardLinearModel],
    max_per_category: int,
) -> dict[str, object]:
    categories: dict[str, dict[str, object]] = {
        "risk_correct_defense_wrong": {"count": 0, "items": []},
        "risk_correct_defense_v1_wrong": {"count": 0, "items": []},
        "defense_correct_risk_wrong": {"count": 0, "items": []},
        "defense_v1_correct_risk_wrong": {"count": 0, "items": []},
    }
    model_names = (
        "risk_context_linear",
        "defense_context_linear",
        "defense_context_v1_linear",
    )

    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        logits_by_model = {
            model_name: linear_models[model_name].logits_for_example(example)
            for model_name in model_names
        }
        predictions = {
            model_name: max(logits, key=logits.get)
            for model_name, logits in logits_by_model.items()
        }
        correct = {
            model_name: prediction == example.action.tile
            for model_name, prediction in predictions.items()
        }
        record: dict[str, object] | None = None
        category_matches = {
            "risk_correct_defense_wrong": (
                correct["risk_context_linear"]
                and not correct["defense_context_linear"]
            ),
            "risk_correct_defense_v1_wrong": (
                correct["risk_context_linear"]
                and not correct["defense_context_v1_linear"]
            ),
            "defense_correct_risk_wrong": (
                correct["defense_context_linear"]
                and not correct["risk_context_linear"]
            ),
            "defense_v1_correct_risk_wrong": (
                correct["defense_context_v1_linear"]
                and not correct["risk_context_linear"]
            ),
        }
        for category, matches in category_matches.items():
            if not matches:
                continue
            payload = categories[category]
            payload["count"] = int(payload["count"]) + 1
            items = payload["items"]
            assert isinstance(items, list)
            if len(items) < max_per_category:
                if record is None:
                    record = _disagreement_record(
                        example,
                        predictions=predictions,
                        correct=correct,
                        logits_by_model=logits_by_model,
                    )
                items.append(record)

    return {
        "kind": "kenjaku-discard-disagreements-v0",
        "examples": len(examples),
        "max_per_category": max_per_category,
        "categories": categories,
    }


def _disagreement_record(
    example: DiscardExample,
    *,
    predictions: dict[str, TileType],
    correct: dict[str, bool],
    logits_by_model: dict[str, dict[TileType, float]],
) -> dict[str, object]:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    shanten_delta = discard_shanten_delta(example)
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "seat": example.seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "actual_discard": example.action.tile.notation,
        "predictions": {
            model_name: tile_type.notation
            for model_name, tile_type in predictions.items()
        },
        "correct": correct,
        "shanten_delta": {
            "before": shanten_delta.before,
            "after": shanten_delta.after,
            "delta": shanten_delta.delta,
        },
        "defense_buckets": _actual_discard_defense_buckets(example),
        "hand_counts": _tile_count_payload(example.hand_counts),
        "visible_counts": _tile_count_payload(example.visible_counts),
        "active_riichi_seats": list(example.active_riichi_seats),
        "riichi_declared_turns": list(example.riichi_declared_turns),
        "rivers_by_seat": _tiles_by_seat_payload(example.rivers_by_seat),
        "meld_tiles_by_seat": _tiles_by_seat_payload(example.meld_tiles_by_seat),
        "dora_indicators": _tile_payload(example.dora_indicators),
        "last_discard_tsumogiri_by_seat": list(example.last_discard_tsumogiri_by_seat),
        "ippatsu_active_seats": list(example.ippatsu_active_seats),
        "candidate_logits": {
            model_name: _logits_payload(logits)
            for model_name, logits in logits_by_model.items()
        },
    }


def _actual_discard_defense_buckets(example: DiscardExample) -> dict[str, bool]:
    return {
        "active_riichi_opponent": has_active_riichi_opponent(example),
        "genbutsu": actual_discard_is_genbutsu(example),
        "suji": actual_discard_has_suji(example),
        "kabe": actual_discard_has_kabe(example),
        "one_chance": actual_discard_has_one_chance(example),
        "seen_before_riichi": actual_discard_seen_before_riichi(example),
        "seen_after_riichi": actual_discard_seen_after_riichi(example),
    }


def _tile_count_payload(counts: tuple[int, ...]) -> list[dict[str, int | str]]:
    return [
        {
            "tile": TileType(index).notation,
            "count": count,
        }
        for index, count in enumerate(counts)
        if count
    ]


def _tiles_by_seat_payload(rivers_by_seat: tuple[tuple[Tile, ...], ...]) -> list[list[str]]:
    return [_tile_payload(tiles) for tiles in rivers_by_seat]


def _tile_payload(tiles: tuple[Tile, ...]) -> list[str]:
    return [tile.notation for tile in tiles]


def _logits_payload(logits: dict[TileType, float]) -> list[dict[str, float | str]]:
    return [
        {
            "tile": tile_type.notation,
            "logit": logits[tile_type],
        }
        for tile_type in sorted(logits)
    ]


def _add_source_args(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--source-label",
        help="human-readable source label recorded in JSON reports",
    )
    parser.add_argument(
        "--source-command",
        help="local data command or manifest reference recorded in JSON reports",
    )
    parser.add_argument(
        "--source-date",
        help="source date or date range recorded in JSON reports",
    )


def _source_metadata(args: argparse.Namespace) -> dict[str, str | None]:
    return {
        "label": args.source_label,
        "command": args.source_command,
        "date": args.source_date,
    }


def _format_optional_accuracy(accuracy: float | None) -> str:
    return "n/a" if accuracy is None else f"{accuracy:.4f}"


def _format_optional_delta(delta: float | None) -> str:
    return "n/a" if delta is None else f"{delta:+.4f}"


def _optional_delta(left: float | None, right: float | None) -> float | None:
    if left is None or right is None:
        return None
    return left - right
