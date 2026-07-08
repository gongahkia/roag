"""Kenjaku riichi mahjong AI research toolkit."""

from importlib import import_module

__version__ = "0.1.0"

api = import_module("kenjaku.api")

__all__ = ["__version__", "api"]
