from __future__ import annotations

import argparse
from pathlib import Path

from kenjaku import __version__
from kenjaku.experiments import (
    build_discard_benchmark_report,
    build_discard_linear_report,
    build_tenhou_inspect_report,
    write_json_report,
)
from kenjaku.io import parse_tenhou_xml_dataset
from kenjaku.models import (
    RAW_COUNT_FEATURE_PROFILE,
    SHANTEN_FEATURE_PROFILE,
    DiscardFrequencyBaseline,
    DiscardLinearModel,
)
from kenjaku.training import (
    deterministic_split,
    iter_call_examples,
    iter_discard_examples,
    summarize_discard_shanten,
)


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
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_source_args(benchmark_discard)
    benchmark_discard.set_defaults(func=_benchmark_discard)
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
    frequency_model = DiscardFrequencyBaseline.fit(train_examples)
    raw_count_linear_model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
        feature_profile=RAW_COUNT_FEATURE_PROFILE,
    )
    linear_model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
        feature_profile=SHANTEN_FEATURE_PROFILE,
    )
    frequency_train_accuracy = frequency_model.score(train_examples)
    frequency_eval_accuracy = frequency_model.score(eval_examples) if eval_examples else None
    raw_count_linear_train_accuracy = raw_count_linear_model.score(train_examples)
    raw_count_linear_eval_accuracy = (
        raw_count_linear_model.score(eval_examples)
        if eval_examples
        else None
    )
    linear_train_accuracy = linear_model.score(train_examples)
    linear_eval_accuracy = linear_model.score(eval_examples) if eval_examples else None
    eval_lift_over_raw_count = _optional_delta(
        linear_eval_accuracy,
        raw_count_linear_eval_accuracy,
    )

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"frequency_train_accuracy: {frequency_train_accuracy:.4f}")
    print(f"frequency_eval_accuracy: {_format_optional_accuracy(frequency_eval_accuracy)}")
    print(f"raw_count_linear_train_accuracy: {raw_count_linear_train_accuracy:.4f}")
    print(
        "raw_count_linear_eval_accuracy: "
        f"{_format_optional_accuracy(raw_count_linear_eval_accuracy)}"
    )
    print(f"linear_train_accuracy: {linear_train_accuracy:.4f}")
    print(f"linear_eval_accuracy: {_format_optional_accuracy(linear_eval_accuracy)}")
    print(f"linear_eval_lift_over_raw_count: {_format_optional_delta(eval_lift_over_raw_count)}")
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.report is not None:
        report = build_discard_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            frequency_train_accuracy=frequency_train_accuracy,
            frequency_eval_accuracy=frequency_eval_accuracy,
            raw_count_linear_epochs=args.epochs,
            raw_count_linear_learning_rate=args.learning_rate,
            raw_count_linear_model_kind=raw_count_linear_model.kind,
            raw_count_linear_feature_dim=raw_count_linear_model.feature_dim,
            raw_count_linear_train_accuracy=raw_count_linear_train_accuracy,
            raw_count_linear_eval_accuracy=raw_count_linear_eval_accuracy,
            linear_epochs=args.epochs,
            linear_learning_rate=args.learning_rate,
            linear_model_kind=linear_model.kind,
            linear_feature_dim=linear_model.feature_dim,
            linear_train_accuracy=linear_train_accuracy,
            linear_eval_accuracy=linear_eval_accuracy,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


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
