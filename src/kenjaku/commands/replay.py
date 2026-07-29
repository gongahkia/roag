from __future__ import annotations

import argparse

from kenjaku.commands._registry import handle, register_commands

COMMANDS = (
    "replay-intake-review",
    "replay-share-plan",
    "replay-public-summary",
    "replay-viewer",
)


def register(subparsers: argparse._SubParsersAction[argparse.ArgumentParser]) -> None:
    register_commands(subparsers, COMMANDS)


__all__ = ["handle", "register"]
