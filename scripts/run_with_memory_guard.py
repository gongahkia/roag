#!/usr/bin/env python3
from __future__ import annotations

import argparse
import math
import os
import re
import signal
import subprocess
import sys
import time
from collections.abc import Sequence

EXIT_MEMORY_LIMIT = 137
_FOOTPRINT_PATTERN = re.compile(r"^Physical footprint:\s*([0-9.]+)\s*([KMGT]?)$", re.MULTILINE)


def main(argv: Sequence[str] | None = None) -> int:
    parser = _parser()
    args = parser.parse_args(argv)
    command = _command(args.command)
    if not command:
        parser.error("missing command after --")
    return run_with_memory_guard(
        command,
        max_memory_kib=args.max_rss_mb * 1024,
        poll_interval=args.poll_interval,
        grace_seconds=args.grace_seconds,
    )


def run_with_memory_guard(
    command: Sequence[str],
    *,
    max_memory_kib: int,
    poll_interval: float,
    grace_seconds: float,
) -> int:
    if max_memory_kib <= 0:
        raise ValueError("max_memory_kib must be positive")
    if poll_interval <= 0:
        raise ValueError("poll_interval must be positive")
    process = subprocess.Popen(command, start_new_session=True)
    process_group = process.pid
    peak_memory_kib = 0
    try:
        while True:
            memory_kib = _process_group_memory_kib(process_group)
            peak_memory_kib = max(peak_memory_kib, memory_kib)
            if memory_kib > max_memory_kib:
                _terminate_process_group(process_group, process, grace_seconds=grace_seconds)
                print(
                    "memory guard exceeded: "
                    f"memory_mb={memory_kib / 1024:.1f} "
                    f"limit_mb={max_memory_kib / 1024:.1f}",
                    file=sys.stderr,
                )
                return EXIT_MEMORY_LIMIT

            return_code = process.poll()
            if return_code is not None:
                print(f"peak_memory_mb={peak_memory_kib / 1024:.1f}", file=sys.stderr)
                return return_code
            time.sleep(poll_interval)
    except KeyboardInterrupt:
        _terminate_process_group(process_group, process, grace_seconds=grace_seconds)
        return 130


def _process_group_memory_kib(process_group: int) -> int:
    pids = _process_group_pids(process_group)
    if sys.platform == "darwin":
        footprints = [_macos_physical_footprint_kib(pid) for pid in pids]
        if all(footprint is not None for footprint in footprints):
            return sum(footprint for footprint in footprints if footprint is not None)
    return _process_group_rss_kib(process_group)


def _process_group_pids(process_group: int) -> tuple[int, ...]:
    result = subprocess.run(
        ["ps", "-o", "pid=", "-g", str(process_group)],
        text=True,
        capture_output=True,
        check=False,
    )
    pids = tuple(int(value) for value in result.stdout.split() if value.strip().isdigit())
    return pids or (process_group,)


def _process_group_rss_kib(process_group: int) -> int:
    result = subprocess.run(
        ["ps", "-o", "rss=", "-g", str(process_group)],
        text=True,
        capture_output=True,
        check=False,
    )
    rss_values = [int(value) for value in result.stdout.split() if value.strip().isdigit()]
    return sum(rss_values)


def _macos_physical_footprint_kib(pid: int) -> int | None:
    try:
        result = subprocess.run(
            ["vmmap", "-summary", str(pid)],
            text=True,
            capture_output=True,
            check=False,
            timeout=5,
        )
    except (OSError, subprocess.TimeoutExpired):
        return None
    if result.returncode != 0:
        return None
    return _parse_macos_physical_footprint_kib(result.stdout)


def _parse_macos_physical_footprint_kib(output: str) -> int | None:
    match = _FOOTPRINT_PATTERN.search(output)
    if match is None:
        return None
    value = float(match.group(1))
    unit = match.group(2)
    multiplier = {"": 1 / 1024, "K": 1, "M": 1024, "G": 1024**2, "T": 1024**3}[unit]
    return math.ceil(value * multiplier)


def _terminate_process_group(
    process_group: int,
    process: subprocess.Popen[bytes],
    *,
    grace_seconds: float,
) -> None:
    try:
        os.killpg(process_group, signal.SIGTERM)
    except ProcessLookupError:
        return
    try:
        process.wait(timeout=grace_seconds)
        return
    except subprocess.TimeoutExpired:
        pass
    try:
        os.killpg(process_group, signal.SIGKILL)
    except ProcessLookupError:
        return
    process.wait(timeout=grace_seconds)


def _command(command: Sequence[str]) -> list[str]:
    values = list(command)
    if values and values[0] == "--":
        return values[1:]
    return values


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Run a command and terminate its process group if memory exceeds a cap."
    )
    parser.add_argument("--max-rss-mb", type=int, required=True)
    parser.add_argument("--poll-interval", type=float, default=0.5)
    parser.add_argument("--grace-seconds", type=float, default=2.0)
    parser.add_argument("command", nargs=argparse.REMAINDER)
    return parser


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
