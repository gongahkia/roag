from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, ActionKind, Discard, Tile, tile_counts
from kenjaku.io import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouGame,
    TenhouMeld,
    TenhouRyuukyoku,
)

OPEN_CLAIM_KINDS = {ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN}


@dataclass(frozen=True, slots=True)
class DiscardExample:
    """One supervised discard decision extracted from a parsed game."""

    round_index: int
    event_index: int
    seat: int
    dealer: int
    scores: tuple[int, ...]
    hand_counts: tuple[int, ...]
    visible_counts: tuple[int, ...]
    action: Action


def iter_discard_examples(game: TenhouGame) -> Iterator[DiscardExample]:
    """Yield discard examples from Tenhou event streams.

    This Phase 0 builder intentionally fails if a discard cannot be removed from
    the reconstructed hand. It reconstructs through decoded calls but still
    stops at terminal events because hand outcome features are not modeled yet.
    """

    for round_index, round_ in enumerate(game.rounds):
        hands = [list(hand) for hand in round_.starting_hands]
        discards_by_seat: list[list[Discard]] = [[] for _ in round_.starting_hands]
        melds_by_seat: list[list[TenhouMeld]] = [[] for _ in round_.starting_hands]

        for event in round_.events:
            if isinstance(event, TenhouDraw):
                hands[event.seat].append(event.tile)
                continue

            if isinstance(event, TenhouAgari | TenhouRyuukyoku):
                break

            if isinstance(event, TenhouCall):
                _apply_call(event, hands, discards_by_seat, melds_by_seat)
                continue

            if isinstance(event, TenhouDiscard):
                visible_tiles = (
                    *round_.dora_indicators,
                    *_visible_discards(discards_by_seat),
                    *_visible_meld_tiles(melds_by_seat),
                    *hands[event.seat],
                )
                yield DiscardExample(
                    round_index=round_index,
                    event_index=event.event_index,
                    seat=event.seat,
                    dealer=round_.dealer,
                    scores=round_.scores,
                    hand_counts=tile_counts(hands[event.seat]),
                    visible_counts=tile_counts(visible_tiles),
                    action=event.action,
                )
                _remove_tile(hands[event.seat], event.tile)
                discards_by_seat[event.seat].append(
                    Discard(tile=event.tile, tsumogiri=event.tsumogiri)
                )


def _apply_call(
    event: TenhouCall,
    hands: list[list[Tile]],
    discards_by_seat: list[list[Discard]],
    melds_by_seat: list[list[TenhouMeld]],
) -> None:
    for tile in _consumed_tiles(event.meld):
        _remove_tile(hands[event.seat], tile)

    if event.meld.kind in OPEN_CLAIM_KINDS:
        if event.meld.called_tile is None:
            raise ValueError(f"{event.meld.kind.value} call is missing a called tile")
        from_seat = (event.seat + event.meld.from_offset) % len(hands)
        _remove_discard(discards_by_seat[from_seat], event.meld.called_tile)

    if event.meld.kind == ActionKind.KAKAN:
        _replace_pon_with_kakan(melds_by_seat[event.seat], event.meld)
        return

    melds_by_seat[event.seat].append(event.meld)


def _consumed_tiles(meld: TenhouMeld) -> tuple[Tile, ...]:
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


def _replace_pon_with_kakan(melds: list[TenhouMeld], kakan: TenhouMeld) -> None:
    kakan_type = kakan.tiles[0].type
    for index, meld in enumerate(melds):
        if meld.kind == ActionKind.PON and meld.tiles[0].type == kakan_type:
            melds[index] = kakan
            return
    raise ValueError(f"kakan without matching pon: {kakan_type.notation}")


def _visible_discards(discards_by_seat: list[list[Discard]]) -> tuple[Tile, ...]:
    return tuple(discard.tile for seat_discards in discards_by_seat for discard in seat_discards)


def _visible_meld_tiles(melds_by_seat: list[list[TenhouMeld]]) -> tuple[Tile, ...]:
    return tuple(tile for seat_melds in melds_by_seat for meld in seat_melds for tile in meld.tiles)


def _remove_tile(hand: list[Tile], tile: Tile) -> None:
    for index, candidate in enumerate(hand):
        if candidate == tile:
            del hand[index]
            return

    for index, candidate in enumerate(hand):
        if candidate.type == tile.type:
            del hand[index]
            return

    raise ValueError(f"discarded tile {tile.notation} is not in reconstructed hand")


def _remove_discard(discards: list[Discard], tile: Tile) -> None:
    for index in range(len(discards) - 1, -1, -1):
        if discards[index].tile == tile:
            del discards[index]
            return

    for index in range(len(discards) - 1, -1, -1):
        if discards[index].tile.type == tile.type:
            del discards[index]
            return

    raise ValueError(f"called tile {tile.notation} is not in reconstructed river")
