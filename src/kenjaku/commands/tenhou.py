from __future__ import annotations

import argparse

from kenjaku.commands._registry import handle, register_commands

COMMANDS = ("inspect-tenhou", "tenhou-to-mjai")


def register(subparsers: argparse._SubParsersAction[argparse.ArgumentParser]) -> None:
    register_commands(subparsers, COMMANDS)


__all__ = ["handle", "register"]
