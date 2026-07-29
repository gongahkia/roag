from __future__ import annotations

import argparse
from functools import lru_cache
from typing import Any

from kenjaku.commands import _legacy


@lru_cache(maxsize=1)
def _legacy_command_registry() -> tuple[dict[str, argparse.ArgumentParser], dict[str, Any]]:
    legacy_parser = _legacy.build_parser()
    for action in legacy_parser._actions:
        choices = getattr(action, "choices", None)
        name_parser_map = getattr(action, "_name_parser_map", None)
        choices_actions = getattr(action, "_choices_actions", None)
        if isinstance(choices, dict) and isinstance(name_parser_map, dict):
            actions_by_name = {
                choice.dest: choice
                for choice in choices_actions or ()
                if hasattr(choice, "dest")
            }
            return name_parser_map, actions_by_name
    raise RuntimeError("legacy parser has no subcommand registry")


def register_commands(
    subparsers: argparse._SubParsersAction[argparse.ArgumentParser],
    commands: tuple[str, ...],
) -> None:
    legacy_parsers, legacy_actions = _legacy_command_registry()
    for command in commands:
        subparsers._name_parser_map[command] = legacy_parsers[command]
        subparsers.choices[command] = legacy_parsers[command]
        subparsers._choices_actions.append(legacy_actions[command])


def handle(args: argparse.Namespace) -> int:
    if not hasattr(args, "func"):
        raise RuntimeError("command module received args without func")
    return args.func(args)
