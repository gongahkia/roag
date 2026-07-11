from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, ActionKind, TileType, tile_counts
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

KITA_TILE_TYPE = TileType.parse("N")


@dataclass(frozen=True, slots=True)
class KitaExample:
    """One supervised Sanma kita/pass decision after a draw."""

    round_index: int
    event_index: int
    kita_event_index: int | None
    draw_event_index: int
    seat: int
    dealer: int
    scores: tuple[int, ...]
    hand_counts: tuple[int, ...]
    visible_counts: tuple[int, ...]
    active_riichi_seats: tuple[bool, ...]
    river_counts_by_seat: tuple[tuple[int, ...], ...]
    seat_turn_index: int
    action: Action


@dataclass(frozen=True, slots=True)
class _PendingKitaDecision:
    round_index: int
    draw_event_index: int
    seat: int


def iter_kita_examples(game: TenhouGame) -> Iterator[KitaExample]:
    """Yield Sanma kita/pass examples for draw states with a North tile."""

    for round_index, round_ in enumerate(game.rounds):
        if not _is_sanma_starting_hands(round_.starting_hands):
            continue

        state = ReconstructionState.from_starting_hands(round_.starting_hands)
        pending: _PendingKitaDecision | None = None

        for event in round_.events:
            if isinstance(event, TenhouDraw):
                state.apply_draw(event)
                pending = (
                    _PendingKitaDecision(round_index, event.event_index, event.seat)
                    if _has_kita_tile(state, event.seat)
                    else None
                )
                continue

            if (
                isinstance(event, TenhouCall)
                and event.meld.kind is ActionKind.KITA
                and pending is not None
                and pending.seat == event.seat
            ):
                yield _example(
                    pending=pending,
                    event_index=event.event_index,
                    kita_event_index=event.event_index,
                    round_=round_,
                    state=state,
                    action=Action(
                        ActionKind.KITA,
                        KITA_TILE_TYPE,
                        consumed=event.meld.tiles,
                    ),
                )
                state.apply_call(event)
                pending = None
                continue

            if (
                pending is not None
                and isinstance(event, TenhouDiscard | TenhouReach | TenhouCall)
                and event.seat == pending.seat
            ):
                yield _example(
                    pending=pending,
                    event_index=event.event_index,
                    kita_event_index=None,
                    round_=round_,
                    state=state,
                    action=Action.pass_(),
                )
                pending = None

            if isinstance(event, TenhouCall):
                state.apply_call(event)
                continue

            if isinstance(event, TenhouDiscard):
                state.apply_discard(event)
                continue

            if isinstance(event, TenhouReach):
                state.apply_reach(event)
                continue

            if isinstance(event, TenhouAgari | TenhouRyuukyoku):
                break


def _example(
    *,
    pending: _PendingKitaDecision,
    event_index: int,
    kita_event_index: int | None,
    round_,
    state: ReconstructionState,
    action: Action,
) -> KitaExample:
    return KitaExample(
        round_index=pending.round_index,
        event_index=event_index,
        kita_event_index=kita_event_index,
        draw_event_index=pending.draw_event_index,
        seat=pending.seat,
        dealer=round_.dealer,
        scores=round_.scores,
        hand_counts=tile_counts(state.hands[pending.seat]),
        visible_counts=tile_counts(state.visible_tiles(pending.seat, round_.dora_indicators)),
        active_riichi_seats=tuple(state.active_riichi),
        river_counts_by_seat=state.river_counts_by_seat(),
        seat_turn_index=state.discard_counts_by_seat[pending.seat],
        action=action,
    )


def _has_kita_tile(state: ReconstructionState, seat: int) -> bool:
    return any(tile.type == KITA_TILE_TYPE for tile in state.hands[seat])


def _is_sanma_starting_hands(starting_hands: tuple[tuple[object, ...], ...]) -> bool:
    if len(starting_hands) == 3:
        return True
    return len(starting_hands) == 4 and not starting_hands[3]
