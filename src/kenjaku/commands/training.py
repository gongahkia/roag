from __future__ import annotations

import argparse

from kenjaku.commands._registry import handle, register_commands

COMMANDS = ("train-ppo-sandbox", "train-population-sandbox", "training-dashboard")


def register(subparsers: argparse._SubParsersAction[argparse.ArgumentParser]) -> None:
    register_commands(subparsers, COMMANDS)


__all__ = ["handle", "register"]
