"""Portable JSON save handling."""

from __future__ import annotations

import json
import os
from pathlib import Path
from typing import Any


class SaveError(OSError):
    """Raised when a save cannot be safely read or written."""


def default_save_path() -> Path:
    root = os.environ.get("XDG_STATE_HOME")
    base = Path(root) if root else Path.home() / ".local" / "state"
    return base / "dullest-dungeon" / "run.save.json"


def write_save(path: Path, snapshot: dict[str, Any]) -> None:
    temporary = path.with_suffix(path.suffix + ".tmp")
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        temporary.write_text(json.dumps(snapshot, indent=2), encoding="utf-8")
        temporary.replace(path)
    except OSError as exc:
        try:
            temporary.unlink(missing_ok=True)
        except OSError:
            pass
        raise SaveError(f"cannot write {path}: {exc}") from exc


def read_save(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SaveError(f"cannot read {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise SaveError(f"save root in {path} is not an object")
    return value
