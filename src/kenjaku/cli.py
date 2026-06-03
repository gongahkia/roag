from __future__ import annotations

import argparse

from kenjaku import __version__


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="kenjaku",
        description="Riichi mahjong AI research toolkit.",
    )
    parser.add_argument("--version", action="store_true", help="print version and exit")
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.version:
        print(f"kenjaku {__version__}")
        return 0

    parser.print_help()
    return 0
