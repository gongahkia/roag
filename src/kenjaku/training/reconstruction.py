from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import ActionKind, Discard, Tile, tile_counts
from kenjaku.io import TenhouCall, TenhouDiscard, TenhouDraw, TenhouMeld, TenhouReach

OPEN_CLAIM_KINDS = {ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN}


@dataclass(slots=True)
class ReconstructionState:
    hands: list[list[Tile]]
    discards_by_seat: list[list[Discard]]
    melds_by_seat: list[list[TenhouMeld]]
    active_riichi: list[bool]
    discard_counts_by_seat: list[int]
    riichi_declared_turns: list[int | None]
    riichi_declared_event_indices: list[int | None]
    ippatsu_active: list[bool]

    @classmethod
    def from_starting_hands(
        cls,
        starting_hands: tuple[tuple[Tile, ...], ...],
    ) -> ReconstructionState:
        return cls(
            hands=[list(hand) for hand in starting_hands],
            discards_by_seat=[[] for _ in starting_hands],
            melds_by_seat=[[] for _ in starting_hands],
            active_riichi=[False for _ in starting_hands],
            discard_counts_by_seat=[0 for _ in starting_hands],
            riichi_declared_turns=[None for _ in starting_hands],
            riichi_declared_event_indices=[None for _ in starting_hands],
            ippatsu_active=[False for _ in starting_hands],
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

    def meld_counts_by_seat(self) -> tuple[tuple[int, ...], ...]:
        return tuple(
            tile_counts(tile for meld in seat_melds for tile in meld.tiles)
            for seat_melds in self.melds_by_seat
        )

    def meld_tiles_by_seat(self) -> tuple[tuple[Tile, ...], ...]:
        return tuple(
            tuple(tile for meld in seat_melds for tile in meld.tiles)
            for seat_melds in self.melds_by_seat
        )

    def river_counts_by_seat(self) -> tuple[tuple[int, ...], ...]:
        return tuple(
            tile_counts(discard.tile for discard in seat_discards)
            for seat_discards in self.discards_by_seat
        )

    def rivers_by_seat(self) -> tuple[tuple[Tile, ...], ...]:
        return tuple(
            tuple(discard.tile for discard in seat_discards)
            for seat_discards in self.discards_by_seat
        )

    def last_discard_tsumogiri_by_seat(self) -> tuple[bool | None, ...]:
        return tuple(
            None if not seat_discards else seat_discards[-1].tsumogiri
            for seat_discards in self.discards_by_seat
        )

    def apply_draw(self, event: TenhouDraw) -> None:
        self.hands[event.seat].append(event.tile)
        self._clear_elapsed_ippatsu(event.seat)

    def apply_discard(self, event: TenhouDiscard) -> None:
        remove_tile(self.hands[event.seat], event.tile)
        self.discards_by_seat[event.seat].append(
            Discard(tile=event.tile, tsumogiri=event.tsumogiri)
        )
        self.discard_counts_by_seat[event.seat] += 1

    def apply_reach(self, event: TenhouReach) -> None:
        if event.step == 1 and not self.active_riichi[event.seat]:
            self.active_riichi[event.seat] = True
            self.ippatsu_active[event.seat] = True
            self.riichi_declared_turns[event.seat] = self.discard_counts_by_seat[event.seat]
            self.riichi_declared_event_indices[event.seat] = event.event_index

    def apply_call(self, event: TenhouCall) -> None:
        self.ippatsu_active = [False for _ in self.ippatsu_active]
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

    def _clear_elapsed_ippatsu(self, seat: int) -> None:
        riichi_turn = self.riichi_declared_turns[seat]
        if (
            riichi_turn is not None
            and self.ippatsu_active[seat]
            and self.discard_counts_by_seat[seat] > riichi_turn
        ):
            self.ippatsu_active[seat] = False


def call_from_seat(event: TenhouCall, players: int) -> int:
    return (event.seat + event.meld.from_offset) % players


def consumed_tiles(meld: TenhouMeld) -> tuple[Tile, ...]:
    if meld.kind in OPEN_CLAIM_KINDS:
        if meld.called_tile_id is None:
            raise ValueError(f"{meld.kind.value} call is missing a called tile id")
        return tuple(
            tile
            for tile_id, tile in zip(meld.tile_ids, meld.tiles, strict=True)
            if tile_id != meld.called_tile_id
        )

    if meld.kind == ActionKind.ANKAN:
        return meld.tiles

    if meld.kind == ActionKind.KAKAN:
        if meld.added_tile is None:
            raise ValueError("kakan call is missing an added tile")
        return (meld.added_tile,)

    if meld.kind == ActionKind.KITA:
        return meld.tiles

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
