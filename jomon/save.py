"""One-version atomic JSON persistence."""

from __future__ import annotations

import json
import os
from pathlib import Path

from .state import GameState, StateError, game_state_from_dict

SAVE_NAME = "jomon-save.json"


class SaveError(RuntimeError):
    """A clear user-facing save/load failure."""


def data_directory() -> Path:
    override = os.environ.get("JOMON_DATA_DIR")
    if override:
        return Path(override).expanduser()
    xdg = os.environ.get("XDG_DATA_HOME")
    if xdg:
        return Path(xdg).expanduser() / "jomon"
    return Path.home() / ".local" / "share" / "jomon"


def save_path() -> Path:
    return data_directory() / SAVE_NAME


def save_game(state: GameState, path: Path | None = None) -> Path:
    target = path or save_path()
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_name(f".{target.name}.tmp")
    try:
        with temporary.open("w", encoding="utf-8") as handle:
            json.dump(state.to_dict(), handle, ensure_ascii=True, indent=2, sort_keys=True)
            handle.write("\n")
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary, target)
    except OSError as exc:
        try:
            temporary.unlink(missing_ok=True)
        except OSError:
            pass
        raise SaveError(f"could not save Jomon: {exc}") from exc
    return target


def load_game(path: Path | None = None) -> GameState:
    target = path or save_path()
    try:
        with target.open("r", encoding="utf-8") as handle:
            data = json.load(handle)
        return game_state_from_dict(data)
    except FileNotFoundError as exc:
        raise SaveError("no Jomon save exists") from exc
    except json.JSONDecodeError as exc:
        raise SaveError(f"save is corrupt JSON: {exc.msg}") from exc
    except (OSError, StateError) as exc:
        raise SaveError(str(exc)) from exc


def valid_save_exists(path: Path | None = None) -> bool:
    try:
        load_game(path)
        return True
    except SaveError:
        return False
