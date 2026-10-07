"""Selected-pack ecology, elite, and forecast wording for stable identities."""
from __future__ import annotations
from .catalog import selected_content_pack

def ecology_text(semantic_id: str) -> str:
    return selected_content_pack().ecology_presentation(semantic_id).text

def ecology_format(semantic_id: str, /, **values: object) -> str:
    return ecology_text(semantic_id).format(**values)

def ecology_actor_name(archetype_id: str, fallback: str = "") -> str:
    for prefix in ("ecology.actor.", "frontier.elite."):
        try:
            return ecology_text(f"{prefix}{archetype_id}.name")
        except KeyError:
            continue
    return fallback

def ecology_actor_capability(archetype_id: str, fallback: str = "") -> str:
    for prefix in ("ecology.actor.", "frontier.elite."):
        try:
            return ecology_text(f"{prefix}{archetype_id}.capability")
        except KeyError:
            continue
    return fallback

def ecology_actor_counterplay(archetype_id: str, fallback: str = "") -> str:
    for prefix in ("ecology.actor.", "frontier.elite."):
        try:
            return ecology_text(f"{prefix}{archetype_id}.counterplay")
        except KeyError:
            continue
    return fallback


from functools import lru_cache
from importlib.resources import files
import json


@lru_cache(maxsize=1)
def _mechanical_capabilities() -> dict[str, str]:
    raw = json.loads(files("jomon").joinpath("data", "actors.json").read_text(encoding="utf-8"))
    rows: dict[str, str] = {}
    for section in ("FRONTIER_ACTORS", "EXPANDED_STANDARD_ACTORS"):
        rows.update({str(row[0]): str(row[12]) for row in raw[section]})
    rows.update({str(row[0]): str(row[6]) for row in raw["FRONTIER_ELITES"]["rows"]})
    return rows


def ecology_mechanical_capability(archetype_id: str, fallback: str = "") -> str:
    """Return the bundled engine capability token, never pack wording."""
    return _mechanical_capabilities().get(archetype_id, fallback)
