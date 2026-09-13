"""Office-fantasy ASCII portraits used by the tavern's competitive expedition."""

from __future__ import annotations

from functools import lru_cache
import hashlib
from importlib.resources import files

from .content import load_catalog
from .json_data import loads


def _load_office_visuals() -> tuple[dict[str, tuple[str, ...]], dict]:
    try:
        raw = loads(files("dumbest_dungeon").joinpath("data", "office_visuals.json").read_text(encoding="utf-8"))
    except OSError as exc:
        raise ValueError(f"cannot read office visuals: {exc}") from exc
    if not isinstance(raw, dict) or set(raw) != {"office_sprites", "expedition_map_symbols"}:
        raise ValueError("office_visuals.json needs office_sprites and expedition_map_symbols")
    sprites = raw["office_sprites"]
    if not isinstance(sprites, dict) or not sprites or any(
        not isinstance(role, str) or not isinstance(lines, list) or len(lines) != 5
        or any(not isinstance(line, str) or not line.isascii() or not line.isprintable() or len(line) > 9 for line in lines)
        for role, lines in sprites.items()
    ):
        raise ValueError("office_sprites must map roles to five printable ASCII rows")

    def check_symbols(value, context: str) -> None:
        if isinstance(value, dict):
            for key, child in value.items():
                check_symbols(child, f"{context}.{key}")
        elif not isinstance(value, str) or len(value) != 1 or not value.isascii() or not value.isprintable():
            raise ValueError(f"{context} must be one printable ASCII symbol")

    symbols = raw["expedition_map_symbols"]
    if not isinstance(symbols, dict):
        raise ValueError("expedition_map_symbols must be a map")
    check_symbols(symbols, "expedition_map_symbols")
    return {role: tuple(lines) for role, lines in sprites.items()}, symbols


OFFICE_SPRITES, EXPEDITION_MAP_SYMBOLS = _load_office_visuals()


def office_card_glyph(role: str) -> tuple[str, str, str]:
    """Keep the original three-line class-art footprint inside each card."""
    return tuple(line.center(9) for line in OFFICE_SPRITES[role][1:4])


OFFICE_COSTUME_WORDS = {
    "Acid": "Toner", "Arc": "Circuit", "Ballast": "Storage",
    "Bilge": "Basement", "Biomass": "Breakroom", "Bulkhead": "Partition",
    "Coolant": "Watercooler", "Deck": "Floor", "Dosimeter": "Timecard",
    "Frost": "Freezer", "Gene": "Policy", "Gravity": "Elevator",
    "Ion": "Power", "Nanite": "Toner", "Neural": "Memo",
    "Plasma": "Ink", "Rad": "Budget",
    "Repair": "Staple", "Rime": "Freezer", "Scrap": "Paper",
    "Signal": "Memo", "Vent": "Duct",
}


def office_costume_name(enemy_id: str) -> str:
    """The source drawing is a costume or a neutral office creature."""
    name = load_catalog().enemies[enemy_id]["name"]
    return " ".join(OFFICE_COSTUME_WORDS.get(word, word) for word in name.split())


def office_action_name(name: str) -> str:
    """Keep each source action identifiable while translating its setting words."""
    return " ".join(OFFICE_COSTUME_WORDS.get(word, word) for word in name.split())


@lru_cache(maxsize=256)
def rival_costumes(world_seed: int) -> tuple[str, str, str, str]:
    catalog = load_catalog()
    ids = tuple(catalog.art["enemies"])
    offset = int.from_bytes(hashlib.sha256(f"office-costumes:{world_seed}".encode()).digest()[:8], "big") % len(ids)
    return tuple(ids[(offset + 31 * rank) % len(ids)] for rank in range(4))
