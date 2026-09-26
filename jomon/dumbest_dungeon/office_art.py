"""Resolver-backed DD visual helpers; selected-pack art is never mechanical."""
from __future__ import annotations
from functools import lru_cache
import hashlib
from .presentation import action_name, enemy_name, office_sprites, map_symbols
from .content import load_catalog

OFFICE_SPRITES = office_sprites()
EXPEDITION_MAP_SYMBOLS = map_symbols()

def office_card_glyph(role: str) -> tuple[str, str, str]:
    return tuple(line.center(9) for line in office_sprites()[role][1:4])

def office_costume_name(enemy_id: str) -> str: return enemy_name(enemy_id)
def office_action_name(action_id: str) -> str: return action_name(action_id)

@lru_cache(maxsize=256)
def rival_costumes(world_seed: int) -> tuple[str, str, str, str]:
    ids = tuple(sorted(load_catalog().enemies))
    offset = int.from_bytes(hashlib.sha256(f"office-costumes:{world_seed}".encode()).digest()[:8], "big") % len(ids)
    return tuple(ids[(offset + 31 * rank) % len(ids)] for rank in range(4))
