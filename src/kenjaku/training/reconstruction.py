from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import ActionKind, Discard, Tile
from kenjaku.io import TenhouCall, TenhouDiscard, TenhouDraw, TenhouMeld

OPEN_CLAIM_KINDS = {ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN}


@dataclass(slots=True)
class ReconstructionState:
    hands: list[list[Tile]]
    discards_by_seat: list[list[Discard]]
    melds_by_seat: list[list[TenhouMeld]]

    @classmethod
    def from_starting_hands(
        cls,
        starting_hands: tuple[tuple[Tile, ...], ...],
    ) -> ReconstructionState:
        return cls(
            hands=[list(hand) for hand in starting_hands],
            discards_by_seat=[[] for _ in starting_hands],
            melds_by_seat=[[] for _ in starting_hands],
        )

    def visible_tiles(
        self,
        perspective: int,
        dora_indicators: tuple[Tile, ...],
    ) -> tuple[Tile, ...]:
        return (
            *dora_indicators,
            *self.visible_discards(),
            *self.visible_meld_tiles(),
            *self.hands[perspective],
        )

    def visible_discards(self) -> tuple[Tile, ...]:
        return tuple(
            discard.tile for seat_discards in self.discards_by_seat for discard in seat_discards
        )

    def visible_meld_tiles(self) -> tuple[Tile, ...]:
        return tuple(
            tile for seat_melds in self.melds_by_seat for meld in seat_melds for tile in meld.tiles
        )

    def apply_draw(self, event: TenhouDraw) -> None:
        self.hands[event.seat].append(event.tile)

    def apply_discard(self, event: TenhouDiscard) -> None:
        remove_tile(self.hands[event.seat], event.tile)
        self.discards_by_seat[event.seat].append(
            Discard(tile=event.tile, tsumogiri=event.tsumogiri)
        )

    def apply_call(self, event: TenhouCall) -> None:
        for tile in consumed_tiles(event.meld):
            remove_tile(self.hands[event.seat], tile)

        if event.meld.kind in OPEN_CLAIM_KINDS:
            if event.meld.called_tile is None:
                raise ValueError(f"{event.meld.kind.value} call is missing a called tile")
            from_seat = call_from_seat(event, len(self.hands))
            remove_discard(self.discards_by_seat[from_seat], event.meld.called_tile)

        if event.meld.kind == ActionKind.KAKAN:
            replace_pon_with_kakan(self.melds_by_seat[event.seat], event.meld)
            return

        self.melds_by_seat[event.seat].append(event.meld)


def call_from_seat(event: TenhouCall, players: int) -> int:
    return (event.seat + event.meld.from_offset) % players


def consumed_tiles(meld: TenhouMeld) -> tuple[Tile, ...]:
    if meld.kind in OPEN_CLAIM_KINDS:
        if meld.called_tile_id is None:
            raise ValueError(f"{meld.kind.value} call is missing a called tile id")
        return tuple(
            tile
            for tile_id, tile in zip(meld.tile_ids, meld.tiles)
            if tile_id != meld.called_tile_id
        )

    if meld.kind == ActionKind.ANKAN:
        return meld.tiles

    if meld.kind == ActionKind.KAKAN:
        if meld.added_tile is None:
            raise ValueError("kakan call is missing an added tile")
        return (meld.added_tile,)

    raise ValueError(f"unsupported meld kind for hand reconstruction: {meld.kind.value}")


def replace_pon_with_kakan(melds: list[TenhouMeld], kakan: TenhouMeld) -> None:
    kakan_type = kakan.tiles[0].type
    for index, meld in enumerate(melds):
        if meld.kind == ActionKind.PON and meld.tiles[0].type == kakan_type:
            melds[index] = kakan
            return
    raise ValueError(f"kakan without matching pon: {kakan_type.notation}")


def remove_tile(hand: list[Tile], tile: Tile) -> None:
    for index, candidate in enumerate(hand):
        if candidate == tile:
            del hand[index]
            return

    for index, candidate in enumerate(hand):
        if candidate.type == tile.type:
            del hand[index]
            return

    raise ValueError(f"discarded tile {tile.notation} is not in reconstructed hand")


def remove_discard(discards: list[Discard], tile: Tile) -> None:
    for index in range(len(discards) - 1, -1, -1):
        if discards[index].tile == tile:
            del discards[index]
            return

    for index in range(len(discards) - 1, -1, -1):
        if discards[index].tile.type == tile.type:
            del discards[index]
            return

    raise ValueError(f"called tile {tile.notation} is not in reconstructed river")
