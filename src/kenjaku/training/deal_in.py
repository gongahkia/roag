from __future__ import annotations

from collections import Counter
from collections.abc import Iterator
from dataclasses import dataclass
from typing import Any

from kenjaku.io import TenhouAgari, TenhouDiscard, TenhouGame, TenhouRound
from kenjaku.training.defense_features import active_riichi_opponents
from kenjaku.training.discard_examples import DiscardExample, iter_discard_examples

DEAL_IN_LABEL_SOURCE = "terminal_ron_discard"


@dataclass(frozen=True, slots=True)
class DealInExample:
    """One discard with a direct ron-discard label from the round terminal event."""

    discard: DiscardExample
    dealt_in: bool
    label_source: str
    outcome_kind: str


def iter_deal_in_examples(
    game: TenhouGame,
    *,
    active_riichi_only: bool = False,
) -> Iterator[DealInExample]:
    """Yield directly labeled deal-in examples from parsed Tenhou rounds.

    Positive labels are only assigned to the last discard by the ron source
    immediately before an `AGARI` terminal event. Earlier discards by the same
    eventual losing player remain negative examples.
    """

    positive_event_indices = tuple(
        _round_deal_in_discard_event_indices(round_) for round_ in game.rounds
    )
    outcome_kinds = tuple(_round_outcome_kind(round_) for round_ in game.rounds)

    for discard in iter_discard_examples(game):
        if discard.round_index >= len(outcome_kinds):
            continue
        outcome_kind = outcome_kinds[discard.round_index]
        if outcome_kind is None:
            continue
        if active_riichi_only and not active_riichi_opponents(discard):
            continue
        yield DealInExample(
            discard=discard,
            dealt_in=discard.event_index in positive_event_indices[discard.round_index],
            label_source=DEAL_IN_LABEL_SOURCE,
            outcome_kind=outcome_kind,
        )


def summarize_deal_in_examples(
    examples: tuple[DealInExample, ...] | list[DealInExample],
) -> dict[str, Any]:
    positives = sum(1 for example in examples if example.dealt_in)
    active_examples = sum(1 for example in examples if active_riichi_opponents(example.discard))
    active_positives = sum(
        1
        for example in examples
        if example.dealt_in and active_riichi_opponents(example.discard)
    )
    outcome_kinds = Counter(example.outcome_kind for example in examples)
    return {
        "kind": "kenjaku-deal-in-label-summary-v0",
        "label_source": DEAL_IN_LABEL_SOURCE,
        "examples": len(examples),
        "direct_deal_in_examples": positives,
        "non_deal_in_examples": len(examples) - positives,
        "positive_rate": None if not examples else positives / len(examples),
        "active_riichi_examples": active_examples,
        "active_riichi_deal_in_examples": active_positives,
        "outcome_kinds": dict(sorted(outcome_kinds.items())),
    }


def _round_deal_in_discard_event_indices(round_: TenhouRound) -> frozenset[int]:
    indices: set[int] = set()
    for agari in round_.agari:
        if _is_tsumo(agari):
            continue
        discard = _last_discard_before_event(
            round_,
            seat=agari.from_seat,
            event_index=agari.event_index,
        )
        if discard is not None:
            indices.add(discard.event_index)
    return frozenset(indices)


def _last_discard_before_event(
    round_: TenhouRound,
    *,
    seat: int,
    event_index: int,
) -> TenhouDiscard | None:
    for event in reversed(round_.events[:event_index]):
        if isinstance(event, TenhouDiscard) and event.seat == seat:
            return event
    return None


def _is_tsumo(agari: TenhouAgari) -> bool:
    return agari.from_seat == agari.winner


def _round_outcome_kind(round_: TenhouRound) -> str | None:
    if round_.agari:
        return "agari"
    if round_.ryuukyoku is not None:
        return "ryuukyoku"
    return None
