from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, Discard, Tile, tile_counts
from kenjaku.io import TenhouDiscard, TenhouDraw, TenhouGame


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
    """Yield discard examples from draw/discard-only Tenhou event streams.

    This Phase 0 builder intentionally fails if a discard cannot be removed from
    the reconstructed hand. Calls, kans, riichi declarations, wins, and abortive
    draws will be added as parser coverage expands.
    """

    for round_index, round_ in enumerate(game.rounds):
        hands = [list(hand) for hand in round_.starting_hands]
        discards_by_seat: list[list[Discard]] = [[] for _ in round_.starting_hands]

        for event in round_.events:
            if isinstance(event, TenhouDraw):
                hands[event.seat].append(event.tile)
                continue

            if isinstance(event, TenhouDiscard):
                past_discards = tuple(
                    discard.tile for seat_discards in discards_by_seat for discard in seat_discards
                )
                visible_tiles = (
                    *round_.dora_indicators,
                    *past_discards,
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
