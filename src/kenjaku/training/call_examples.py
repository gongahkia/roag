from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, ActionKind, Tile, tile_counts
from kenjaku.io import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouGame,
    TenhouReach,
    TenhouRyuukyoku,
)
from kenjaku.training.reconstruction import (
    OPEN_CLAIM_KINDS,
    ReconstructionState,
    call_from_seat,
    consumed_tiles,
)


@dataclass(frozen=True, slots=True)
class CallExample:
    """One supervised call/pass decision after another player's discard."""

    round_index: int
    event_index: int
    call_event_index: int | None
    seat: int
    from_seat: int
    dealer: int
    scores: tuple[int, ...]
    discarded_tile: Tile
    legal_call_kinds: tuple[ActionKind, ...]
    hand_counts: tuple[int, ...]
    visible_counts: tuple[int, ...]
    action: Action


def iter_call_examples(game: TenhouGame) -> Iterator[CallExample]:
    """Yield call/pass examples for legal chi/pon/kan opportunities."""

    for round_index, round_ in enumerate(game.rounds):
        state = ReconstructionState.from_starting_hands(round_.starting_hands)
        pending_discard: TenhouDiscard | None = None

        for event in round_.events:
            if isinstance(event, TenhouCall):
                if pending_discard is not None:
                    call = event if _claims_discard(event, pending_discard, state) else None
                    yield from _call_window_examples(
                        round_index=round_index,
                        round_=round_,
                        state=state,
                        discard=pending_discard,
                        call=call,
                    )
                    pending_discard = None
                state.apply_call(event)
                continue

            if pending_discard is not None:
                if not isinstance(event, TenhouAgari | TenhouRyuukyoku):
                    yield from _call_window_examples(
                        round_index=round_index,
                        round_=round_,
                        state=state,
                        discard=pending_discard,
                        call=None,
                    )
                pending_discard = None

            if isinstance(event, TenhouDraw):
                state.apply_draw(event)
                continue

            if isinstance(event, TenhouAgari | TenhouRyuukyoku):
                break

            if isinstance(event, TenhouReach):
                state.apply_reach(event)
                continue

            if isinstance(event, TenhouDiscard):
                state.apply_discard(event)
                pending_discard = event


def _call_window_examples(
    *,
    round_index: int,
    round_,
    state: ReconstructionState,
    discard: TenhouDiscard,
    call: TenhouCall | None,
) -> Iterator[CallExample]:
    for seat in range(len(state.hands)):
        if seat == discard.seat:
            continue

        legal_kinds = _legal_call_kinds(seat, discard, state)
        if call is not None and seat == call.seat:
            yield _example(
                round_index=round_index,
                round_=round_,
                state=state,
                discard=discard,
                seat=seat,
                legal_call_kinds=_ensure_legal_kind(legal_kinds, call.meld.kind),
                action=_call_action(call),
                call_event_index=call.event_index,
            )
            continue

        if legal_kinds:
            yield _example(
                round_index=round_index,
                round_=round_,
                state=state,
                discard=discard,
                seat=seat,
                legal_call_kinds=legal_kinds,
                action=Action.pass_(),
                call_event_index=None,
            )


def _example(
    *,
    round_index: int,
    round_,
    state: ReconstructionState,
    discard: TenhouDiscard,
    seat: int,
    legal_call_kinds: tuple[ActionKind, ...],
    action: Action,
    call_event_index: int | None,
) -> CallExample:
    return CallExample(
        round_index=round_index,
        event_index=discard.event_index,
        call_event_index=call_event_index,
        seat=seat,
        from_seat=discard.seat,
        dealer=round_.dealer,
        scores=round_.scores,
        discarded_tile=discard.tile,
        legal_call_kinds=legal_call_kinds,
        hand_counts=tile_counts(state.hands[seat]),
        visible_counts=tile_counts(state.visible_tiles(seat, round_.dora_indicators)),
        action=action,
    )


def _claims_discard(
    call: TenhouCall,
    discard: TenhouDiscard,
    state: ReconstructionState,
) -> bool:
    if call.meld.kind not in OPEN_CLAIM_KINDS or call.meld.called_tile is None:
        return False
    return (
        call_from_seat(call, len(state.hands)) == discard.seat
        and call.meld.called_tile.type == discard.tile.type
    )


def _call_action(call: TenhouCall) -> Action:
    if call.meld.kind not in OPEN_CLAIM_KINDS:
        raise ValueError(f"{call.meld.kind.value} is not a discard-claim call")
    if call.meld.called_tile is None:
        raise ValueError(f"{call.meld.kind.value} call is missing a called tile")
    return Action(
        kind=call.meld.kind,
        tile=call.meld.called_tile.type,
        consumed=consumed_tiles(call.meld),
    )


def _ensure_legal_kind(
    legal_kinds: tuple[ActionKind, ...],
    actual_kind: ActionKind,
) -> tuple[ActionKind, ...]:
    if actual_kind in legal_kinds:
        return legal_kinds
    return (*legal_kinds, actual_kind)


def _legal_call_kinds(
    seat: int,
    discard: TenhouDiscard,
    state: ReconstructionState,
) -> tuple[ActionKind, ...]:
    if seat == discard.seat:
        return ()

    kinds: list[ActionKind] = []
    hand_counts = tile_counts(state.hands[seat])
    discarded_type = discard.tile.type
    matching_tiles = hand_counts[discarded_type.index]

    if _can_chi(seat, discard, hand_counts, len(state.hands)):
        kinds.append(ActionKind.CHI)
    if matching_tiles >= 2:
        kinds.append(ActionKind.PON)
    if matching_tiles >= 3:
        kinds.append(ActionKind.MINKAN)

    return tuple(kinds)


def _can_chi(
    seat: int,
    discard: TenhouDiscard,
    hand_counts: tuple[int, ...],
    players: int,
) -> bool:
    discarded_type = discard.tile.type
    if players == 3:
        return False
    if seat != (discard.seat + 1) % players or discarded_type.is_honor:
        return False

    rank = discarded_type.rank
    assert rank is not None
    suit_start = discarded_type.index - rank + 1
    for start_rank in range(max(1, rank - 2), min(7, rank) + 1):
        needed_ranks = tuple(
            candidate for candidate in range(start_rank, start_rank + 3) if candidate != rank
        )
        if all(hand_counts[suit_start + needed_rank - 1] > 0 for needed_rank in needed_ranks):
            return True
    return False
