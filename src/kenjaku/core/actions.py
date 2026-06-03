from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum

from kenjaku.core.tiles import Tile, TileType


class ActionKind(StrEnum):
    DISCARD = "discard"
    RIICHI = "riichi"
    CHI = "chi"
    PON = "pon"
    MINKAN = "minkan"
    ANKAN = "ankan"
    KAKAN = "kakan"
    KITA = "kita"
    PASS = "pass"
    TSUMO = "tsumo"
    RON = "ron"
    KYUSHU = "kyushu"


_TILE_REQUIRED = {
    ActionKind.DISCARD,
    ActionKind.CHI,
    ActionKind.PON,
    ActionKind.MINKAN,
    ActionKind.ANKAN,
    ActionKind.KAKAN,
    ActionKind.KITA,
    ActionKind.RON,
}


@dataclass(frozen=True, slots=True)
class Action:
    """A factored player decision.

    `tile` is the target/called/discarded tile type. `consumed` captures extra
    tiles used by chi/pon/kan actions when the parser has that detail.
    """

    kind: ActionKind
    tile: TileType | None = None
    tsumogiri: bool = False
    consumed: tuple[Tile, ...] = ()

    def __post_init__(self) -> None:
        if self.kind in _TILE_REQUIRED and self.tile is None:
            raise ValueError(f"{self.kind.value} actions require a tile")
        if self.kind is not ActionKind.DISCARD and self.tsumogiri:
            raise ValueError("tsumogiri is only valid for discard actions")
        if self.kind in {ActionKind.PASS, ActionKind.RIICHI, ActionKind.KYUSHU} and self.consumed:
            raise ValueError(f"{self.kind.value} actions cannot consume tiles")

    @classmethod
    def discard(cls, tile: TileType | str, *, tsumogiri: bool = False) -> Action:
        parsed = TileType.parse(tile) if isinstance(tile, str) else tile
        return cls(ActionKind.DISCARD, parsed, tsumogiri=tsumogiri)

    @classmethod
    def pass_(cls) -> Action:
        return cls(ActionKind.PASS)
