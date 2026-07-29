from __future__ import annotations

import random

from kenjaku.core import TENHOU_3P, TENHOU_4P, Action, ActionKind
from kenjaku.reproducibility import derive_seed, derive_seed_int
from kenjaku.simulation.environment import (
    SandboxEnvironmentState,
    apply_ankan_action,
    apply_call_action,
    apply_discard_action,
    apply_kakan_action,
    apply_kita_action,
    apply_kyuushu_kyuuhai_action,
    apply_reaction_pass_action,
    apply_riichi_action,
    apply_ron_action,
    apply_tsumo_action,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_sandbox_actions,
)


def generate_legal_random_state_4p(
    *,
    seed: int | str,
    decisions: int = 16,
) -> SandboxEnvironmentState:
    return _generate_legal_random_state(
        ruleset=TENHOU_4P.name,
        seed=seed,
        decisions=decisions,
    )


def generate_legal_random_state_3p(
    *,
    seed: int | str,
    decisions: int = 16,
) -> SandboxEnvironmentState:
    return _generate_legal_random_state(
        ruleset=TENHOU_3P.name,
        seed=seed,
        decisions=decisions,
    )


def _generate_legal_random_state(
    *,
    ruleset: str,
    seed: int | str,
    decisions: int,
) -> SandboxEnvironmentState:
    if isinstance(decisions, bool) or not isinstance(decisions, int) or decisions < 0:
        raise ValueError("decisions must be a non-negative integer")
    rng = random.Random(derive_seed_int(seed, "legal-random-state", ruleset))
    reset = 0
    state = _initial_state(ruleset=ruleset, seed=seed, reset=reset)
    for _decision in range(decisions):
        state, reset = _ready_state(state, ruleset=ruleset, seed=seed, reset=reset)
        seat = _acting_seat(state)
        transitions = tuple(
            transition
            for action in legal_sandbox_actions(state, seat=seat)
            for transition in (_apply_action(state, seat=seat, action=action),)
            if transition.terminal_reason is None
        )
        if not transitions:
            reset += 1
            state = _initial_state(ruleset=ruleset, seed=seed, reset=reset)
            continue
        state = transitions[rng.randrange(len(transitions))]
    state, _reset = _ready_state(state, ruleset=ruleset, seed=seed, reset=reset)
    return state


def _initial_state(*, ruleset: str, seed: int | str, reset: int) -> SandboxEnvironmentState:
    return initial_sandbox_environment(
        ruleset=ruleset,
        seed=derive_seed(seed, "legal-random-state", ruleset, reset),
    )


def _ready_state(
    state: SandboxEnvironmentState,
    *,
    ruleset: str,
    seed: int | str,
    reset: int,
) -> tuple[SandboxEnvironmentState, int]:
    while True:
        if state.terminal_reason is not None:
            reset += 1
            state = _initial_state(ruleset=ruleset, seed=seed, reset=reset)
            continue
        if state.pending_reaction_seats or state.drawn_tile is not None or state.needs_discard:
            return state, reset
        state = draw_for_current_seat(state)


def _acting_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_reaction_seats:
        return state.pending_reaction_seats[0]
    return state.current_seat


def _apply_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action,
) -> SandboxEnvironmentState:
    if action.kind is ActionKind.DISCARD:
        next_state, _discard = apply_discard_action(state, action)
        return next_state
    if action.kind is ActionKind.PASS:
        return apply_reaction_pass_action(state, seat=seat)
    if action.kind is ActionKind.RON:
        return apply_ron_action(state, seat=seat, action=action)
    if action.kind is ActionKind.TSUMO:
        return apply_tsumo_action(state, action)
    if action.kind is ActionKind.RIICHI:
        return apply_riichi_action(state, action)
    if action.kind is ActionKind.ANKAN:
        next_state, _meld = apply_ankan_action(state, action)
        return next_state
    if action.kind is ActionKind.KAKAN:
        next_state, _meld = apply_kakan_action(state, action)
        return next_state
    if action.kind is ActionKind.KITA:
        return apply_kita_action(state, action)
    if action.kind is ActionKind.KYUSHU:
        return apply_kyuushu_kyuuhai_action(state, action)
    if action.kind in {ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN}:
        next_state, _meld = apply_call_action(state, seat=seat, action=action)
        return next_state
    raise ValueError("unsupported legal random-state action: " + action.kind.value)
