"""Kenjaku riichi mahjong AI research toolkit."""

from importlib import import_module
from types import ModuleType

__version__ = "0.2.0"

__all__ = ["__version__", "api"]


def __getattr__(name: str) -> ModuleType:
    if name == "api":
        return import_module("kenjaku.api")
    raise AttributeError(name)
