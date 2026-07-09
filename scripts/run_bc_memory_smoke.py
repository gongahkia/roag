#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import shlex
import sys
from collections.abc import Sequence
from pathlib import Path

from run_with_memory_guard import run_with_memory_guard


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    _prepare_environment(tracemalloc=args.tracemalloc)
    command = _export_command(args)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    print("bc_memory_smoke_command=" + shlex.join(command), file=sys.stderr)
    return run_with_memory_guard(
        command,
        max_rss_kib=args.max_rss_mb * 1024,
        poll_interval=args.poll_interval,
        grace_seconds=args.grace_seconds,
    )


def _export_command(args: argparse.Namespace) -> list[str]:
    command = [
        args.python,
        "-m",
        "kenjaku",
        "export-bc-examples",
        *(str(path) for path in args.paths),
        "--output-dir",
        str(args.output_dir),
        "--actions",
        args.actions,
        "--shard-size",
        str(args.shard_size),
        "--limit-per-type",
        str(args.limit_per_type),
        "--jobs",
        str(args.jobs),
        "--source-label",
        args.source_label,
        "--source-command",
        " ".join(sys.argv),
    ]
    if args.overwrite:
        command.append("--overwrite")
    if args.skip_errors:
        command.append("--skip-errors")
    if args.parse_cache is not None:
        command.extend(("--parse-cache", str(args.parse_cache)))
    return command


def _prepare_environment(*, tracemalloc: bool) -> None:
    src_path = str(Path("src").resolve())
    current = os.environ.get("PYTHONPATH")
    os.environ["PYTHONPATH"] = src_path if not current else f"{src_path}{os.pathsep}{current}"
    if tracemalloc:
        os.environ["PYTHONTRACEMALLOC"] = "1"


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Run a bounded export-bc-examples memory smoke under the RSS guard."
    )
    parser.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories for the smoke run",
    )
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--actions", default="discard,call,riichi")
    parser.add_argument("--shard-size", type=int, default=50000)
    parser.add_argument("--limit-per-type", type=int, default=1000)
    parser.add_argument("--jobs", type=int, default=1)
    parser.add_argument("--parse-cache", type=Path)
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--skip-errors", action="store_true")
    parser.add_argument("--max-rss-mb", type=int, default=4096)
    parser.add_argument("--poll-interval", type=float, default=0.5)
    parser.add_argument("--grace-seconds", type=float, default=2.0)
    parser.add_argument("--tracemalloc", action="store_true")
    parser.add_argument("--python", default=sys.executable)
    parser.add_argument("--source-label", default="todo-102-memory-smoke")
    return parser


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
