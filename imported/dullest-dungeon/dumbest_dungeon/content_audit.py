"""Reproducible catalog census; structural similarity is a review signal."""

from __future__ import annotations

import json
from collections import Counter, defaultdict
from typing import Any

from . import __version__
from .content import Catalog, load_catalog
from .engine import GameEngine


def effect_shape(value: Any) -> Any:
    """Remove magnitudes, retaining opcode, condition, target and sequencing."""
    if isinstance(value, dict):
        return {key: ("positive" if item > 0 else "negative" if item < 0 else "zero")
                if key in {"amount", "bonus"} and isinstance(item, (int, float))
                else effect_shape(item) for key, item in sorted(value.items())}
    if isinstance(value, (list, tuple)):
        return [effect_shape(item) for item in value]
    return value


def audit_content(catalog: Catalog) -> dict[str, Any]:
    groups: dict[str, list[str]] = defaultdict(list)
    opcodes: Counter[str] = Counter()
    tags: Counter[str] = Counter()
    owners = {}
    for card_id, card in sorted(catalog.cards.items()):
        shape = {key: card.get(key) for key in ("cost", "from_ranks", "target", "target_ranks")}
        shape["effects"] = effect_shape(card["effects"])
        groups[json.dumps(shape, sort_keys=True)].append(card_id)
        opcodes.update(effect["op"] for effect in card["effects"])
        tags.update(card["tags"])
    for hero_id, hero in sorted(catalog.heroes.items()):
        pool = [card for card in catalog.cards.values() if card["hero"] == hero_id]
        owners[hero_id] = {
            "pool": sorted(card["id"] for card in pool),
            "nonstarters": sorted(card["id"] for card in pool if card["id"] not in hero["starter_deck"]),
            "builds": hero["builds"],
            "starter_rank_access": {
                str(rank): sum(rank in catalog.cards[card]["from_ranks"] for card in hero["starter_deck"])
                for rank in range(1, 5)
            },
            "pool_rank_access": {
                str(rank): sum(rank in card["from_ranks"] for card in pool)
                for rank in range(1, 5)
            },
        }
    normal = {enemy for encounter in catalog.encounters.values() if encounter["kind"] == "normal"
              for enemy in encounter["enemies"]}
    elite = {enemy for encounter in catalog.encounters.values() if encounter["kind"] == "elite"
             for enemy in encounter["enemies"]}
    biomes = {}
    for biome in sorted(catalog.biomes):
        enemies = {key for key, enemy in catalog.enemies.items() if biome in enemy.get("biomes", ["derelict"])}
        encounters = [e for e in catalog.encounters.values() if biome in e.get("biomes", ["derelict"])]
        biomes[biome] = {
            "normal_enemies": sorted(enemies & normal),
            "elite_only_enemies": sorted(enemies & (elite - normal)),
            "elite_compatible_enemies": sorted(enemies & elite),
            "templates": dict(sorted(Counter(e["kind"] for e in encounters).items())),
            "formations": sorted({"/".join(e["enemies"]) for e in encounters}),
        }
    sections = ("heroes", "squads", "cards", "enemies", "encounters", "events", "biomes", "worlds",
                "missions", "facilities", "landmarks", "terrains", "terrain_patterns", "boons", "curses", "items")
    return {
        "audit_version": 1,
        "versions": {"engine": __version__, "content_schema": catalog.raw["schema_version"],
                     "save": GameEngine.SAVE_VERSION},
        "counts": {section: len(getattr(catalog, section)) for section in sections},
        "balance": catalog.balance,
        "owners": owners,
        "normalized_card_groups": sorted([ids for ids in groups.values() if len(ids) > 1]),
        "effect_opcodes": dict(sorted(opcodes.items())),
        "tags": dict(sorted(tags.items())),
        "enemy_action_counts": dict(sorted(Counter(len(e["actions"]) for e in catalog.enemies.values()).items())),
        "biomes": biomes,
        "boss_templates": sorted(e["id"] for e in catalog.encounters.values() if e["kind"] == "boss"),
        "notes": [
            "elite-only means present in elite templates and absent from normal templates",
            "structural groups retain energy, ranks, target, signed effects and effect order",
            "a template count is not a count of distinct tactical questions",
            "rank access counts legal owner positions, not sampled hand clogging",
        ],
    }


if __name__ == "__main__":
    print(json.dumps(audit_content(load_catalog()), indent=2, sort_keys=True))
