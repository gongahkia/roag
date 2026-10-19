"""Entry point: python3 -m regolith (no installation required)."""

from __future__ import annotations

import argparse
import json
import locale
import sys
from pathlib import Path

from .simulation import DEFAULT_SAVE, Simulation


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(description="REGOLITH / a cellular-automata moon colony in your terminal")
    result.add_argument("--seed", type=int, default=42, help="World generation seed (default: 42)")
    result.add_argument("--load", action="store_true", help="Continue the saved colony")
    result.add_argument("--save", type=Path, default=DEFAULT_SAVE, metavar="PATH", help="Save file location")
    result.add_argument("--ascii", action="store_true", help="Use ASCII map and border glyphs")
    result.add_argument("--skip-intro", action="store_true", help="Skip the sectioned tutorial and open the paused outpost")
    result.add_argument("--headless", type=int, metavar="TICKS", help="Run without a terminal and print a JSON summary")
    result.add_argument("--demo", action="store_true", help="Dispatch the first rescue and queue mining and construction")
    return result


def demo_orders(sim: Simulation):
    cx, cy = sim.world.center
    if sim.distress:
        sim.dispatch_expedition(sim.distress[0].id)
    for dy in range(3):
        for dx in range(3):
            for ox, oy in ((-7, -8), (5, 8), (12, -5)):
                sim.designate(cx + ox + dx, cy + oy + dy, "dig")
    # Deck pads adjacent to the habitat make arrays part of its power island.
    sim.designate(cx + 6, cy + 1, "build", "solar")
    sim.designate(cx + 7, cy + 1, "build", "solar")


def main(argv: list[str] | None = None) -> int:
    args = parser().parse_args(argv)
    if args.headless is not None and args.headless < 0:
        parser().error("--headless must be nonnegative")
    if args.headless is None and (not sys.stdin.isatty() or not sys.stdout.isatty()):
        print("Regolith needs an interactive terminal. Try: python3 -m regolith --headless 480 --demo", file=sys.stderr)
        return 2
    try:
        sim = Simulation.load(args.save) if args.load else Simulation(args.seed)
        if args.demo:
            demo_orders(sim)
        if args.headless is not None:
            sim.step(args.headless)
            print(json.dumps(sim.summary(), indent=2))
            return 0
        locale.setlocale(locale.LC_ALL, "")
        from .tui import play
        play(sim, args.save, args.ascii, not args.skip_intro and not args.load)
        return 0
    except (OSError, ValueError, KeyError, TypeError, OverflowError) as exc:
        print(f"Regolith: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
