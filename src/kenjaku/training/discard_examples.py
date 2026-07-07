from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, Tile, tile_counts
from kenjaku.io import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouGame,
    TenhouReach,
    TenhouRyuukyoku,
)
from kenjaku.training.reconstruction import ReconstructionState


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
    discard_is_tsumogiri: bool = False
    active_riichi_seats: tuple[bool, ...] = ()
    river_counts_by_seat: tuple[tuple[int, ...], ...] = ()
    seat_turn_index: int = 0
    rivers_by_seat: tuple[tuple[Tile, ...], ...] = ()
    riichi_declared_turns: tuple[int | None, ...] = ()
    riichi_declared_event_indices: tuple[int | None, ...] = ()
    meld_counts_by_seat: tuple[tuple[int, ...], ...] = ()
    meld_tiles_by_seat: tuple[tuple[Tile, ...], ...] = ()
    dora_indicators: tuple[Tile, ...] = ()
    last_discard_tsumogiri_by_seat: tuple[bool | None, ...] = ()
    ippatsu_active_seats: tuple[bool, ...] = ()


def iter_discard_examples(game: TenhouGame) -> Iterator[DiscardExample]:
    """Yield discard examples from Tenhou event streams.

    This Phase 0 builder intentionally fails if a discard cannot be removed from
    the reconstructed hand. It reconstructs through decoded calls but still
    stops at terminal events because hand outcome features are not modeled yet.
    """

    for round_index, round_ in enumerate(game.rounds):
        state = ReconstructionState.from_starting_hands(round_.starting_hands)

        for event in round_.events:
            if isinstance(event, TenhouDraw):
                state.apply_draw(event)
                continue

            if isinstance(event, TenhouAgari | TenhouRyuukyoku):
                break

            if isinstance(event, TenhouReach):
                state.apply_reach(event)
                continue

            if isinstance(event, TenhouCall):
                state.apply_call(event)
                continue

            if isinstance(event, TenhouDiscard):
                visible_tiles = state.visible_tiles(event.seat, round_.dora_indicators)
                yield DiscardExample(
                    round_index=round_index,
                    event_index=event.event_index,
                    seat=event.seat,
                    dealer=round_.dealer,
                    scores=round_.scores,
                    hand_counts=tile_counts(state.hands[event.seat]),
                    visible_counts=tile_counts(visible_tiles),
                    action=event.action,
                    discard_is_tsumogiri=event.tsumogiri,
                    active_riichi_seats=tuple(state.active_riichi),
                    river_counts_by_seat=state.river_counts_by_seat(),
                    seat_turn_index=state.discard_counts_by_seat[event.seat],
                    rivers_by_seat=state.rivers_by_seat(),
                    riichi_declared_turns=tuple(state.riichi_declared_turns),
                    riichi_declared_event_indices=tuple(state.riichi_declared_event_indices),
                    meld_counts_by_seat=state.meld_counts_by_seat(),
                    meld_tiles_by_seat=state.meld_tiles_by_seat(),
                    dora_indicators=round_.dora_indicators,
                    last_discard_tsumogiri_by_seat=state.last_discard_tsumogiri_by_seat(),
                    ippatsu_active_seats=tuple(state.ippatsu_active),
                )
                state.apply_discard(event)
