"""Selected-Jomon-pack presentation resolver for the in-world tavern game."""
from __future__ import annotations

from collections.abc import Mapping
from functools import lru_cache
from typing import Iterator

from jomon.catalog import selected_content_pack


def dd_text(slot: str, /, **values: object) -> str:
    """Render a declared DD presentation slot with explicit scalar values."""
    template = selected_content_pack().dullest_dungeon.text(slot)
    return template.format(**values)


def _slots(prefix: str, field: str | None = None) -> tuple[tuple[str, str], ...]:
    needle = prefix + "."
    suffix = f".{field}" if field else ""
    rows = []
    for slot, value in selected_content_pack().dullest_dungeon.text_slots:
        if not slot.startswith(needle):
            continue
        rest = slot[len(needle):]
        if field:
            if rest.endswith(suffix) and "." not in rest[:-len(suffix)]:
                rows.append((rest[:-len(suffix)], value))
        elif "." not in rest:
            rows.append((rest, value))
    return tuple(rows)


class DdTextMap(Mapping[str, str]):
    """Read-only selected-pack map for stable DD identities, never mechanics."""
    def __init__(self, prefix: str, field: str | None = None):
        self.prefix, self.field = prefix, field
    def __getitem__(self, key: str) -> str:
        suffix = f".{self.field}" if self.field else ""
        return dd_text(f"{self.prefix}.{key}{suffix}")
    def __iter__(self) -> Iterator[str]: return iter(key for key, _ in _slots(self.prefix, self.field))
    def __len__(self) -> int: return len(_slots(self.prefix, self.field))


def role_name(hero_id: str) -> str: return dd_text(f"heroes.{hero_id}.name")
def card_name(card_id: str) -> str: return dd_text(f"cards.{card_id}.name")
def card_description(card_id: str) -> str: return dd_text(f"cards.{card_id}.description")
def enemy_name(enemy_id: str) -> str: return dd_text(f"enemies.{enemy_id}.name")
def action_name(action_id: str) -> str: return dd_text(f"actions.{action_id}.name")
def world_name(world_id: str) -> str: return dd_text(f"worlds.{world_id}.name")
def biome_name(biome_id: str) -> str: return dd_text(f"biomes.{biome_id}.name")
def department_name(department_id: str) -> str: return dd_text(f"departments.{department_id}")
def event_name(event_id: str) -> str: return dd_text(f"events.{event_id}.name")
def event_description(event_id: str) -> str: return dd_text(f"events.{event_id}.description")
def squad_name(squad_id: str) -> str: return dd_text(f"squads.{squad_id}")
def status_name(status_id: str) -> str: return dd_text(f"statuses.{status_id}")
def target_name(target_id: str) -> str: return dd_text(f"targets.{target_id}")
def doctrine_name(doctrine_id: str) -> str: return dd_text(f"doctrines.{doctrine_id}")
def infusion_name(infusion_id: str) -> str: return dd_text(f"infusions.{infusion_id}")
def catalog_text(family: str, identity: str, field: str = "name") -> str:
    return dd_text(f"catalog.{family}.{identity}.{field}")


def office_sprites() -> dict[str, tuple[str, ...]]:
    return dict(selected_content_pack().dullest_dungeon.sprites)


def map_symbols() -> dict[str, object]:
    result: dict[str, object] = {}
    for slot, value in selected_content_pack().dullest_dungeon.map_symbols:
        current = result
        parts = slot.split(".")
        for part in parts[:-1]:
            current = current.setdefault(part, {})  # type: ignore[assignment]
        current[parts[-1]] = value
    return result


def title_art() -> tuple[str, ...]: return selected_content_pack().dullest_dungeon.title_art
