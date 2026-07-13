"""Deterministic heuristic opponents for mixed sandbox self-play."""

from __future__ import annotations

from collections.abc import Sequence

from kenjaku.core import Action, ActionKind, tile_counts
from kenjaku.simulation.environment import SandboxEnvironmentState, legal_sandbox_actions
from kenjaku.training import CallExample


def choose_heuristic_sandbox_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    legal_actions: Sequence[Action] | None = None,
) -> Action:
    """Return one legal sandbox action using fixed special, call, and discard heuristics."""
    actions = tuple(
        legal_sandbox_actions(state, seat=seat) if legal_actions is None else legal_actions
    )
    if not actions:
        raise ValueError("heuristic opponent requires at least one legal action")
    if state.pending_discard is not None:
        return _choose_reaction_action(state, seat=seat, actions=actions)
    from kenjaku.heuristics import rank_special_action_heuristic

    special = rank_special_action_heuristic(state, seat=seat)
    for candidate in special:
        if candidate.action in actions:
            return candidate.action
    discard_actions = tuple(action for action in actions if action.kind is ActionKind.DISCARD)
    if discard_actions:
        return _choose_discard_action(state, seat=seat, actions=discard_actions)
    return min(actions, key=_action_sort_key)


def _choose_reaction_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    actions: tuple[Action, ...],
) -> Action:
    from kenjaku.heuristics import rank_special_action_heuristic

    special = rank_special_action_heuristic(state, seat=seat)
    for candidate in special:
        if candidate.action.kind is ActionKind.RON and candidate.action in actions:
            return candidate.action
    call_actions = tuple(
        action
        for action in actions
        if action.kind in {ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN}
    )
    if call_actions:
        return _choose_call_action(state, seat=seat, actions=actions, call_actions=call_actions)
    pass_actions = tuple(action for action in actions if action.kind is ActionKind.PASS)
    if pass_actions:
        return min(pass_actions, key=_action_sort_key)
    return min(actions, key=_action_sort_key)


def _choose_call_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    actions: tuple[Action, ...],
    call_actions: tuple[Action, ...],
) -> Action:
    from kenjaku.heuristics import rank_call_pass_heuristic

    pending_discard = state.pending_discard
    pending_discard_seat = state.pending_discard_seat
    if pending_discard is None or pending_discard_seat is None:
        raise ValueError("heuristic call action requires a pending discard and source seat")
    legal_call_kinds = tuple(
        sorted({action.kind for action in call_actions}, key=lambda kind: kind.value)
    )
    example = CallExample(
        round_index=0,
        event_index=0,
        call_event_index=None,
        seat=seat,
        from_seat=pending_discard_seat,
        dealer=state.dealer_seat,
        scores=state.points,
        discarded_tile=pending_discard,
        legal_call_kinds=legal_call_kinds,
        hand_counts=tile_counts(state.hands[seat]),
        visible_counts=(0,) * 34,
        action=Action.pass_(),
    )
    actions_by_kind: dict[ActionKind, tuple[Action, ...]] = {
        kind: tuple(
            sorted((action for action in actions if action.kind is kind), key=_action_sort_key)
        )
        for kind in (*legal_call_kinds, ActionKind.PASS)
    }
    for candidate in rank_call_pass_heuristic(example):
        choices = actions_by_kind.get(candidate.kind, ())
        if choices:
            return choices[0]
    raise RuntimeError("heuristic call ranking omitted every legal action")


def _choose_discard_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    actions: tuple[Action, ...],
) -> Action:
    hand = state.hands[seat]
    if len(hand) == 14:
        from kenjaku.heuristics import rank_discard_heuristic

        legal_by_tile = {action.tile: action for action in actions}
        for candidate in rank_discard_heuristic(hand, ruleset=state.ruleset):
            action = legal_by_tile.get(candidate.tile)
            if action is not None:
                return action
    return min(actions, key=_action_sort_key)


def _action_sort_key(action: Action) -> tuple[str, int, bool, tuple[str, ...]]:
    return (
        action.kind.value,
        -1 if action.tile is None else action.tile.index,
        action.tsumogiri,
        tuple(tile.notation for tile in action.consumed),
    )
