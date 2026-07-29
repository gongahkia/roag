from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, ActionKind, shanten, tile_counts
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
class RiichiExample:
    """One supervised binary riichi/pass decision at a discard decision point."""

    round_index: int
    event_index: int
    riichi_event_index: int | None
    seat: int
    dealer: int
    scores: tuple[int, ...]
    hand_counts: tuple[int, ...]
    visible_counts: tuple[int, ...]
    active_riichi_seats: tuple[bool, ...]
    river_counts_by_seat: tuple[tuple[int, ...], ...]
    seat_turn_index: int
    action: Action


def iter_riichi_examples(game: TenhouGame) -> Iterator[RiichiExample]:
    """Yield conservative supervised riichi/pass examples.

    Positive examples are explicit Tenhou step-1 riichi declarations. Negative
    examples are closed, not-yet-riichi discard decisions where the concealed
    hand is in tenpai by the existing closed-hand shanten proxy.
    """

    for round_index, round_ in enumerate(game.rounds):
        state = ReconstructionState.from_starting_hands(round_.starting_hands)

        for event in round_.events:
            if isinstance(event, TenhouDraw):
                state.apply_draw(event)
                continue

            if isinstance(event, TenhouReach):
                if event.step == 1:
                    yield _example(
                        round_index=round_index,
                        round_=round_,
                        state=state,
                        seat=event.seat,
                        event_index=event.event_index,
                        riichi_event_index=event.event_index,
                        action=Action(ActionKind.RIICHI),
                    )
                state.apply_reach(event)
                continue

            if isinstance(event, TenhouDiscard):
                if _is_negative_riichi_candidate(state, event.seat, round_.scores):
                    yield _example(
                        round_index=round_index,
                        round_=round_,
                        state=state,
                        seat=event.seat,
                        event_index=event.event_index,
                        riichi_event_index=None,
                        action=Action.pass_(),
                    )
                state.apply_discard(event)
                continue

            if isinstance(event, TenhouCall):
                state.apply_call(event)
                continue

            if isinstance(event, TenhouAgari | TenhouRyuukyoku):
                break


def _example(
    *,
    round_index: int,
    round_,
    state: ReconstructionState,
    seat: int,
    event_index: int,
    riichi_event_index: int | None,
    action: Action,
) -> RiichiExample:
    return RiichiExample(
        round_index=round_index,
        event_index=event_index,
        riichi_event_index=riichi_event_index,
        seat=seat,
        dealer=round_.dealer,
        scores=round_.scores,
        hand_counts=tile_counts(state.hands[seat]),
        visible_counts=tile_counts(state.visible_tiles(seat, round_.dora_indicators)),
        active_riichi_seats=tuple(state.active_riichi),
        river_counts_by_seat=state.river_counts_by_seat(),
        seat_turn_index=state.discard_counts_by_seat[seat],
        action=action,
    )


def _is_negative_riichi_candidate(
    state: ReconstructionState,
    seat: int,
    scores: tuple[int, ...],
) -> bool:
    if state.active_riichi[seat] or state.melds_by_seat[seat]:
        return False
    if seat < len(scores) and scores[seat] < 10:
        return False
    counts = tile_counts(state.hands[seat])
    if sum(counts) % 3 != 2:
        return False
    return shanten(counts) == 0
