from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import shutil
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest import mock

from kenjaku.cli import (
    _call_example_from_payload,
    _call_example_to_payload,
    _call_examples_signature,
    _disagreement_record,
    _limit_call_examples,
    main,
)
from kenjaku.core import Action, ActionKind, Tile, TileType, tile_counts
from kenjaku.experiments import build_discard_mlp_benchmark_report
from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import CallExample, DiscardExample

__all__ = [
    "Action",
    "ActionKind",
    "CallExample",
    "CliCommandTests",
    "DiscardExample",
    "Path",
    "TemporaryDirectory",
    "Tile",
    "TileType",
    "_call_example_from_payload",
    "_call_example_to_payload",
    "_call_examples_signature",
    "_disagreement_record",
    "_limit_call_examples",
    "build_discard_mlp_benchmark_report",
    "contextlib",
    "importlib",
    "io",
    "json",
    "main",
    "mock",
    "parse_tenhou_xml_file",
    "shutil",
    "subprocess",
    "sys",
    "tile_counts",
]


class CliCommandTests(unittest.TestCase):
    pass
