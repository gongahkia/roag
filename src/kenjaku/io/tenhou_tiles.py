from __future__ import annotations

from kenjaku.core import Tile, TileType

RED_FIVE_TILE_IDS = {16, 52, 88}


def tenhou_tile(tile_id: int) -> Tile:
    if not 0 <= tile_id < 136:
        raise ValueError(f"Tenhou tile id out of range: {tile_id}")
    return Tile(TileType(tile_id // 4), red=tile_id in RED_FIVE_TILE_IDS)
