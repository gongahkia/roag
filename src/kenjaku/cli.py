from __future__ import annotations

import argparse
import sys

from kenjaku import __version__
from kenjaku.commands import COMMAND_MODULES, _legacy
from kenjaku.logging import configure_logging
from kenjaku.repro_report import configure_report_provenance
from kenjaku.reproducibility import pin_seeds

_call_example_from_payload = _legacy._call_example_from_payload
_call_example_to_payload = _legacy._call_example_to_payload
_call_examples_signature = _legacy._call_examples_signature
_call_metrics = _legacy._call_metrics
_disagreement_record = _legacy._disagreement_record
_limit_call_examples = _legacy._limit_call_examples
_summarize_call_predictions = _legacy._summarize_call_predictions


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="kenjaku",
        description="Riichi mahjong AI research toolkit.",
    )
    parser.add_argument("--version", action="store_true", help="print version and exit")
    parser.add_argument(
        "--global-seed",
        help="seed Python, NumPy, and PyTorch RNGs before running a subcommand",
    )
    parser.add_argument(
        "--log-level",
        choices=("debug", "info", "warning", "error"),
        default="warning",
        help="minimum log level for diagnostic events",
    )
    parser.add_argument(
        "--log-format",
        choices=("text", "json"),
        default="text",
        help="diagnostic log format",
    )
    subparsers = parser.add_subparsers(dest="command")
    for module in COMMAND_MODULES:
        module.register(subparsers)
    return parser


def main(argv: list[str] | None = None) -> int:
    effective_argv = sys.argv[1:] if argv is None else argv
    configure_report_provenance(["kenjaku", *effective_argv])
    args = build_parser().parse_args(argv)
    configure_logging(args.log_level, json=args.log_format == "json")
    if args.global_seed is not None:
        pin_seeds(args.global_seed)

    if args.version:
        print(f"kenjaku {__version__}")
        return 0

    if hasattr(args, "func"):
        return args.func(args)

    build_parser().print_help()
    return 0


__all__ = [
    "_call_example_from_payload",
    "_call_example_to_payload",
    "_call_examples_signature",
    "_call_metrics",
    "_disagreement_record",
    "_limit_call_examples",
    "_summarize_call_predictions",
    "build_parser",
    "main",
]


if __name__ == "__main__":
    raise SystemExit(main())
