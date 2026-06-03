from __future__ import annotations

from collections.abc import Iterator
from dataclasses import dataclass

from kenjaku.core import Action, tile_counts
from kenjaku.io import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouGame,
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
                )
                state.apply_discard(event)
