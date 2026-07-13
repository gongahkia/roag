"""Bridge the unified masked policy head to sandbox core actions."""

from __future__ import annotations

import math
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any

from kenjaku.core import Action
from kenjaku.models.multi_action_policy import (
    MELD_ARGUMENT_ACTIONS,
    MELD_SELECTION_DIM,
    meld_selection_index,
)
from kenjaku.models.torch_discard import require_torch
from kenjaku.schema import (
    LEGAL_ACTION_MASK_V1_DIM,
    ActionV1,
    LegalActionMaskV1,
    ObservationV1,
    action_v1_mask_index,
    observation_v1_tensor,
)
from kenjaku.simulation.environment import SandboxEnvironmentState, legal_sandbox_actions


@dataclass(frozen=True, slots=True)
class SandboxPolicyInputs:
    """Versioned policy tensors plus the exact legal sandbox actions they represent."""

    observation: ObservationV1
    observation_tensor: tuple[float, ...]
    action_mask: LegalActionMaskV1
    action_candidates: tuple[tuple[int, tuple[Action, ...]], ...]

    def candidates_for_index(self, index: int) -> tuple[Action, ...]:
        for candidate_index, candidates in self.action_candidates:
            if candidate_index == index:
                return candidates
        raise ValueError(f"sandbox policy index is not legal: {index}")


@dataclass(slots=True)
class UnifiedPolicySandboxAdapter:
    """Select one legal sandbox action with a trained unified policy head."""

    model: Any

    def select_action(
        self,
        state: SandboxEnvironmentState,
        *,
        seat: int,
        legal_actions: Sequence[Action] | None = None,
    ) -> Action:
        inputs = sandbox_policy_inputs(state, seat=seat, legal_actions=legal_actions)
        torch = require_torch()
        device = _model_device(self.model)
        observation = torch.tensor(
            [inputs.observation_tensor],
            dtype=torch.float32,
            device=device,
        )
        action_mask = torch.tensor([inputs.action_mask.mask], dtype=torch.bool, device=device)
        with torch.inference_mode():
            action_logits = self.model(observation, action_mask)
            action_scores = tuple(
                float(value) for value in action_logits.squeeze(0).detach().cpu().tolist()
            )
            selected_index = _select_action_index(inputs, action_scores)
            candidates = inputs.candidates_for_index(selected_index)
            meld_scores = _meld_scores(self.model, observation, candidates, torch=torch)
        return select_sandbox_action_from_logits(inputs, action_scores, meld_scores=meld_scores)


def sandbox_policy_inputs(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    legal_actions: Sequence[Action] | None = None,
) -> SandboxPolicyInputs:
    """Encode one sandbox decision into the unified policy's public schemas."""
    observation = ObservationV1.from_sandbox_state(state, seat=seat)
    actions = tuple(
        legal_sandbox_actions(state, seat=seat) if legal_actions is None else legal_actions
    )
    if not actions:
        raise ValueError("sandbox policy requires at least one legal action")
    action_values = tuple(ActionV1.from_core(action, ruleset=state.ruleset) for action in actions)
    action_mask = LegalActionMaskV1.from_actions(action_values, ruleset=state.ruleset)
    grouped: dict[int, list[Action]] = {}
    for action, action_value in zip(actions, action_values, strict=True):
        grouped.setdefault(action_v1_mask_index(action_value), []).append(action)
    return SandboxPolicyInputs(
        observation=observation,
        observation_tensor=observation_v1_tensor(observation),
        action_mask=action_mask,
        action_candidates=tuple(
            (index, tuple(sorted(candidates, key=_action_sort_key)))
            for index, candidates in sorted(grouped.items())
        ),
    )


def select_sandbox_action_from_logits(
    inputs: SandboxPolicyInputs,
    action_logits: Sequence[float],
    *,
    meld_scores: Mapping[int, float] | None = None,
) -> Action:
    """Map legal unified-policy logits back to one deterministic core action."""
    selected_index = _select_action_index(inputs, action_logits)
    candidates = inputs.candidates_for_index(selected_index)
    if len(candidates) == 1 or not all(
        action.kind.value in MELD_ARGUMENT_ACTIONS for action in candidates
    ):
        return candidates[0]
    if meld_scores is None:
        return candidates[0]
    selections = {meld_selection_index(action) for action in candidates}
    missing = selections - set(meld_scores)
    if missing:
        raise ValueError("meld scores must cover every collapsed sandbox meld action")
    selected_meld = max(
        selections,
        key=lambda index: (_finite_score(meld_scores[index], "meld"), -index),
    )
    matching = tuple(
        action for action in candidates if meld_selection_index(action) == selected_meld
    )
    return matching[0]


def _select_action_index(inputs: SandboxPolicyInputs, action_logits: Sequence[float]) -> int:
    if len(action_logits) != LEGAL_ACTION_MASK_V1_DIM:
        raise ValueError(f"action_logits must contain {LEGAL_ACTION_MASK_V1_DIM} values")
    return max(
        (index for index, _candidates in inputs.action_candidates),
        key=lambda index: (_finite_score(action_logits[index], "action"), -index),
    )


def _meld_scores(
    model: Any,
    observation: Any,
    candidates: Sequence[Action],
    *,
    torch: Any,
) -> dict[int, float] | None:
    if len(candidates) < 2 or not all(
        action.kind.value in MELD_ARGUMENT_ACTIONS for action in candidates
    ):
        return None
    mask = [False] * MELD_SELECTION_DIM
    for action in candidates:
        mask[meld_selection_index(action)] = True
    logits = model.meld_logits(
        observation,
        torch.tensor([mask], dtype=torch.bool, device=observation.device),
    )
    values = logits.squeeze(0).detach().cpu()
    return {
        selection: float(values[selection])
        for selection in (meld_selection_index(action) for action in candidates)
    }


def _model_device(model: Any) -> Any:
    try:
        return next(model.parameters()).device
    except StopIteration as error:
        raise ValueError("unified policy model has no parameters") from error


def _finite_score(value: float, name: str) -> float:
    if (
        not isinstance(value, (int, float))
        or isinstance(value, bool)
        or not math.isfinite(float(value))
    ):
        raise ValueError(f"{name} logits for legal actions must be finite")
    return float(value)


def _action_sort_key(action: Action) -> tuple[str, int, bool, tuple[str, ...]]:
    return (
        action.kind.value,
        -1 if action.tile is None else action.tile.index,
        action.tsumogiri,
        tuple(tile.notation for tile in action.consumed),
    )
