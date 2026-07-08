from __future__ import annotations

import argparse

from kenjaku.commands._registry import handle, register_commands

COMMANDS = (
    "benchmark-discard-from-examples",
    "benchmark-discard",
    "benchmark-discard-mlp",
    "benchmark-discard-transformer",
    "benchmark-report-summary",
    "benchmark-dashboard",
    "disagreement-report-summary",
    "benchmark-call",
    "benchmark-call-from-examples",
    "benchmark-riichi",
    "benchmark-kita",
    "benchmark-riichi-from-examples",
)


def register(subparsers: argparse._SubParsersAction[argparse.ArgumentParser]) -> None:
    register_commands(subparsers, COMMANDS)


__all__ = ["handle", "register"]
