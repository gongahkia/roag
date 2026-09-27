"""Frontend-only settings and user save locations; never part of GameState."""
from __future__ import annotations

from dataclasses import dataclass
import json
import os
from pathlib import Path
import re

SETTINGS_FORMAT = 1
RENDERERS = ("debug", "ascii")


@dataclass(frozen=True)
class AppSettings:
    renderer: str = "debug"


def _home() -> Path:
    return Path.home()


def config_directory() -> Path:
    if os.name == "nt":
        return Path(os.environ.get("APPDATA", _home() / "AppData" / "Roaming")) / "Jomon"
    if os.sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / "Jomon"
    return Path(os.environ.get("XDG_CONFIG_HOME", _home() / ".config")) / "jomon"


def data_directory() -> Path:
    if os.name == "nt":
        return Path(os.environ.get("LOCALAPPDATA", _home() / "AppData" / "Local")) / "Jomon"
    if os.sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / "Jomon"
    return Path(os.environ.get("XDG_DATA_HOME", _home() / ".local" / "share")) / "jomon"


def settings_path() -> Path:
    return Path(os.environ.get("JOMON_SETTINGS_PATH", config_directory() / "settings.json"))


def save_directory() -> Path:
    return Path(os.environ.get("JOMON_SAVE_DIR", data_directory() / "saves"))


def default_save_path() -> Path:
    return save_directory() / "continue.json"


def load_app_settings(path: Path | None = None) -> AppSettings:
    path = path or settings_path()
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
        if (not isinstance(value, dict) or value.get("format") != SETTINGS_FORMAT
                or value.get("renderer") not in RENDERERS):
            raise ValueError
        return AppSettings(value["renderer"])
    except (OSError, ValueError, json.JSONDecodeError):
        return AppSettings()


def resolve_renderer(cli_renderer: str | None, settings: AppSettings) -> str:
    """Apply the documented per-launch CLI > stored > Debug precedence."""
    if cli_renderer == "graphical":
        return "debug"
    return cli_renderer if cli_renderer in RENDERERS else settings.renderer


def save_app_settings(settings: AppSettings, path: Path | None = None) -> Path:
    if settings.renderer not in RENDERERS:
        raise ValueError("unsupported renderer setting")
    path = path or settings_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps({"format": SETTINGS_FORMAT, "renderer": settings.renderer}, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)
    return path


def safe_save_name(value: str) -> str | None:
    value = value.strip()
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9 _.-]{0,63}", value):
        return None
    return value


def save_path_for_name(name: str) -> Path | None:
    safe = safe_save_name(name)
    return save_directory() / f"{safe}.json" if safe else None


def available_saves(root: Path | None = None) -> tuple[Path, ...]:
    root = root or save_directory()
    if not root.is_dir():
        return ()
    return tuple(sorted((path for path in root.glob("*.json") if path.is_file()), key=lambda path: path.stat().st_mtime, reverse=True))
