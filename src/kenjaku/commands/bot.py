from __future__ import annotations

import argparse

from kenjaku.commands import _legacy
from kenjaku.commands._registry import handle

COMMANDS = ("bot",)
BOT_POLICY_CHOICES = (
    "auto",
    "frequency",
    "linear-discard",
    "mlp",
    "mlp-discard",
    "transformer",
    "transformer-discard",
)


def register(subparsers: argparse._SubParsersAction[argparse.ArgumentParser]) -> None:
    bot = subparsers.add_parser("bot", help="run a stdin/stdout MJAI bot adapter")
    bot.add_argument(
        "--policy",
        default="frequency",
        help="policy name or checkpoint path; defaults to frequency",
    )
    bot.add_argument(
        "--policy-type",
        choices=BOT_POLICY_CHOICES,
        default="auto",
        help="checkpoint family; auto detects paths",
    )
    bot.add_argument(
        "--player-id",
        type=int,
        required=True,
        help="MJAI seat id, 0 through 3",
    )
    bot.add_argument(
        "--device",
        default="cpu",
        help="PyTorch device for mlp or transformer policies",
    )
    bot.set_defaults(func=_legacy._bot)


__all__ = ["handle", "register"]
