"""Portable JSON save handling."""

from __future__ import annotations

import json
import os
import tempfile
from pathlib import Path
from typing import Any

from .json_data import JsonDataError, loads


class SaveError(OSError):
    """Raised when a save cannot be safely read or written."""


def default_save_path() -> Path:
    root = os.environ.get("XDG_STATE_HOME")
    base = Path(root) if root else Path.home() / ".local" / "state"
    return base / "dullest-dungeon" / "run.save.json"


def write_save(path: Path, snapshot: dict[str, Any]) -> None:
    temporary = None
    try:
        encoded = json.dumps(snapshot, indent=2, allow_nan=False) + "\n"
        path.parent.mkdir(parents=True, exist_ok=True)
        descriptor, name = tempfile.mkstemp(prefix=f".{path.name}.", suffix=".tmp", dir=path.parent)
        temporary = Path(name)
        with os.fdopen(descriptor, "w", encoding="utf-8") as stream:
            stream.write(encoded)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
        temporary = None
        directory = os.open(path.parent, os.O_RDONLY)
        try:
            os.fsync(directory)
        finally:
            os.close(directory)
    except (OSError, ValueError, TypeError, UnicodeError) as exc:
        raise SaveError(f"cannot write {path}: {exc}") from exc
    finally:
        if temporary is not None:
            try:
                temporary.unlink(missing_ok=True)
            except OSError:
                pass


def read_save(path: Path) -> dict[str, Any]:
    try:
        value = loads(path.read_text(encoding="utf-8"))
    except (OSError, JsonDataError, UnicodeError) as exc:
        raise SaveError(f"cannot read {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise SaveError(f"save root in {path} is not an object")
    return value
