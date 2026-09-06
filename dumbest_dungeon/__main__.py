"""Command-line entry point."""

from __future__ import annotations

import argparse
import curses
import secrets
import sys
from pathlib import Path

from .content import ContentError, load_catalog
from .engine import GameEngine
from .save import default_save_path
from .ui import TerminalUI


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(description="A survival-horror party deckbuilder for the terminal.")
    result.add_argument("--seed", type=int, help="seed used for every new expedition in this session")
    result.add_argument("--save-file", type=Path, default=default_save_path(), help="override the JSON save path")
    result.add_argument("--validate-content", action="store_true", help="validate bundled JSON and exit")
    return result


def main(argv: list[str] | None = None) -> int:
    args = parser().parse_args(argv)
    try:
        catalog = load_catalog()
    except ContentError as exc:
        print(f"content error: {exc}", file=sys.stderr)
        return 2
    if args.validate_content:
        print(
            "Content valid: "
            f"{len(catalog.heroes)} heroes, {len(catalog.cards)} cards, "
            f"{len(catalog.enemies)} enemies, {len(catalog.encounters)} encounters, "
            f"{len(catalog.events)} events, {len(catalog.boons)} boons, "
            f"{len(catalog.curses)} curses, {len(catalog.items)} items."
        )
        return 0

    def new_game() -> GameEngine:
        seed = args.seed if args.seed is not None else secrets.randbits(32)
        return GameEngine.new(catalog, seed, start_in_hub=True)

    try:
        curses.wrapper(lambda screen: TerminalUI(screen, catalog, args.save_file, new_game).run())
    except KeyboardInterrupt:
        return 0
    except curses.error as exc:
        print(f"terminal error: {exc}; run inside an 80x24 or larger interactive terminal", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
