from __future__ import annotations

import argparse
from pathlib import Path

from kenjaku import __version__
from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import iter_discard_examples


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
    examples = sum(1 for _ in iter_discard_examples(game))

    print(f"rounds: {len(game.rounds)}")
    print(f"discards: {discards}")
    print(f"discard_examples: {examples}")
    return 0
