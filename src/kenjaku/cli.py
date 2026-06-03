from __future__ import annotations

import argparse
from pathlib import Path

from kenjaku import __version__
from kenjaku.io import parse_tenhou_xml_file
from kenjaku.models import DiscardFrequencyBaseline, DiscardLinearModel
from kenjaku.training import deterministic_split, iter_call_examples, iter_discard_examples


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
    inspect_tenhou.add_argument("path", type=Path, help="path to a Tenhou XML file")
    inspect_tenhou.set_defaults(func=_inspect_tenhou)

    train_baseline = subparsers.add_parser(
        "train-discard-baseline",
        help="fit the deterministic discard frequency baseline on one Tenhou XML file",
    )
    train_baseline.add_argument("path", type=Path, help="path to a Tenhou XML file")
    train_baseline.set_defaults(func=_train_discard_baseline)

    train_linear = subparsers.add_parser(
        "train-discard-linear",
        help="fit the tiny dependency-free linear discard model on one Tenhou XML file",
    )
    train_linear.add_argument("path", type=Path, help="path to a Tenhou XML file")
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
    train_linear.set_defaults(func=_train_discard_linear)
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
    game = parse_tenhou_xml_file(args.path)
    discards = sum(len(round_.discards) for round_ in game.rounds)
    discard_examples = sum(1 for _ in iter_discard_examples(game))
    call_examples = sum(1 for _ in iter_call_examples(game))

    print(f"rounds: {len(game.rounds)}")
    print(f"discards: {discards}")
    print(f"discard_examples: {discard_examples}")
    print(f"call_examples: {call_examples}")
    return 0


def _train_discard_baseline(args: argparse.Namespace) -> int:
    game = parse_tenhou_xml_file(args.path)
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    model = DiscardFrequencyBaseline.fit(examples)
    print(f"examples: {len(examples)}")
    print(f"top_discard: {model.top_tile.notation}")
    print(f"training_accuracy: {model.score(examples):.4f}")
    return 0


def _train_discard_linear(args: argparse.Namespace) -> int:
    game = parse_tenhou_xml_file(args.path)
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
    )
    model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
    )

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"train_accuracy: {model.score(train_examples):.4f}")
    if eval_examples:
        print(f"eval_accuracy: {model.score(eval_examples):.4f}")
    else:
        print("eval_accuracy: n/a")
    return 0
