"""Leakage-free multi-task training over heuristic distillation trajectories."""

from __future__ import annotations

import math
import random
from collections import Counter
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.models.multi_action_policy import (
    MELD_ARGUMENT_ACTIONS,
    MELD_SELECTION_DIM,
    TILE_ARGUMENT_ACTIONS,
    TILE_ARGUMENT_DIM,
    MaskedMultiActionPolicyHead,
    MultiActionPolicyConfig,
    meld_selection_index,
    meld_selection_mask,
)
from kenjaku.models.torch_discard import require_torch_modules, resolve_torch_device
from kenjaku.schema import (
    LEGAL_ACTION_MASK_V1_DIM,
    LEGAL_ACTION_MASK_V1_KYUSHU_INDEX,
    LEGAL_ACTION_MASK_V1_PASS_INDEX,
    LEGAL_ACTION_MASK_V1_RIICHI_INDEX,
    LEGAL_ACTION_MASK_V1_TILE_ACTION_OFFSETS,
    LEGAL_ACTION_MASK_V1_TSUMO_INDEX,
    OBSERVATION_V1_TENSOR_DIM,
    ActionV1,
    LegalActionMaskV1,
    MeldV1,
    ObservationV1,
    action_v1_mask_index,
    observation_v1_tensor,
)

BEHAVIOR_DISTILLATION_TRAINER_KIND = "multi-task-behavior-distillation-trainer-v0"
HEURISTIC_DISTILLATION_FAMILIES = ("discard", "call_pass", "special_action")
_TASK_NAMES = ("action", "tile", "meld", "value")
_SINGLE_ACTION_KINDS = {
    LEGAL_ACTION_MASK_V1_PASS_INDEX: ActionKind.PASS.value,
    LEGAL_ACTION_MASK_V1_TSUMO_INDEX: ActionKind.TSUMO.value,
    LEGAL_ACTION_MASK_V1_RIICHI_INDEX: ActionKind.RIICHI.value,
    LEGAL_ACTION_MASK_V1_KYUSHU_INDEX: ActionKind.KYUSHU.value,
}


@dataclass(frozen=True, slots=True)
class BehaviorDistillationExample:
    """One top-ranked heuristic target and its legal masks."""

    family: str
    observation: tuple[float, ...]
    action_mask: tuple[bool, ...]
    action_target: int
    tile_action: str | None = None
    tile_mask: tuple[bool, ...] | None = None
    tile_target: int | None = None
    meld_mask: tuple[bool, ...] | None = None
    meld_target: int | None = None
    value_target: float | None = None

    def __post_init__(self) -> None:
        if self.family not in HEURISTIC_DISTILLATION_FAMILIES:
            raise ValueError("unsupported distillation family: " + self.family)
        if len(self.observation) != OBSERVATION_V1_TENSOR_DIM:
            raise ValueError(f"observation must contain {OBSERVATION_V1_TENSOR_DIM} values")
        if not all(isinstance(value, float) and math.isfinite(value) for value in self.observation):
            raise ValueError("observation values must be finite floats")
        _validate_mask(self.action_mask, LEGAL_ACTION_MASK_V1_DIM, "action_mask")
        _validate_target(self.action_target, self.action_mask, "action_target")
        _validate_tile_fields(self.tile_action, self.tile_mask, self.tile_target)
        _validate_meld_fields(self.meld_mask, self.meld_target)
        if self.value_target is not None and (
            not isinstance(self.value_target, (int, float))
            or isinstance(self.value_target, bool)
            or not math.isfinite(float(self.value_target))
        ):
            raise ValueError("value_target must be a finite number or null")


@dataclass(slots=True)
class BehaviorDistillationTrainingResult:
    """Trained model and unweighted per-task training metrics."""

    model: Any
    device: str
    train_metrics: dict[str, int | float | None]
    eval_metrics: dict[str, int | float | None]
    history: list[dict[str, Any]]


def distillation_examples_from_manifest(
    manifest: Mapping[str, Any],
) -> tuple[BehaviorDistillationExample, ...]:
    """Build top-ranked, leakage-free multi-task examples from one valid manifest."""
    _require_valid_manifest(manifest)
    ruleset = _required_str(manifest, "ruleset", context="manifest")
    players = _required_int(manifest, "players", context="manifest")
    trajectories = _required_array(manifest, "trajectories", context="manifest")
    examples: list[BehaviorDistillationExample] = []
    for index, trajectory in enumerate(trajectories):
        if not isinstance(trajectory, Mapping):
            raise ValueError(f"trajectory {index} must be an object")
        examples.extend(
            _trajectory_examples(trajectory, index=index, ruleset=ruleset, players=players)
        )
    if not examples:
        raise ValueError("distillation manifest has no ranked examples")
    return tuple(examples)


def train_multi_task_behavior_distillation(
    train_examples: Sequence[BehaviorDistillationExample],
    eval_examples: Sequence[BehaviorDistillationExample] = (),
    *,
    config: MultiActionPolicyConfig | None = None,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    device: str = "auto",
    seed: int = 0,
    action_kind_weights: Mapping[ActionKind | str, float] | None = None,
    balance_action_kinds: bool = False,
) -> BehaviorDistillationTrainingResult:
    """Train multi-task labels with optional action-kind imbalance correction."""
    torch, _nn, functional, _data_loader, _dataset_base = require_torch_modules()
    if not train_examples:
        raise ValueError("no distillation training examples found")
    if epochs < 0:
        raise ValueError("epochs must be non-negative")
    if batch_size <= 0:
        raise ValueError("batch_size must be positive")
    if learning_rate <= 0:
        raise ValueError("learning_rate must be positive")
    resolved_action_weights = resolve_action_kind_loss_weights(
        train_examples,
        action_kind_weights=action_kind_weights,
        balance_action_kinds=balance_action_kinds,
    )

    resolved_device = resolve_torch_device(device)
    torch.manual_seed(seed)
    if resolved_device.type == "cuda":
        torch.cuda.manual_seed_all(seed)
    model = MaskedMultiActionPolicyHead(config, seed=seed).to(resolved_device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate)
    history: list[dict[str, Any]] = []

    def record(epoch: int) -> tuple[dict[str, int | float | None], dict[str, int | float | None]]:
        train_metrics = evaluate_multi_task_behavior_distillation(
            model,
            train_examples,
            device=resolved_device,
            action_kind_weights=resolved_action_weights,
        )
        eval_metrics = evaluate_multi_task_behavior_distillation(
            model,
            eval_examples,
            device=resolved_device,
            action_kind_weights=resolved_action_weights,
        )
        history.append({"epoch": epoch, "train": train_metrics, "eval": eval_metrics})
        return train_metrics, eval_metrics

    if epochs == 0:
        train_metrics, eval_metrics = record(0)
    else:
        train_metrics: dict[str, int | float | None] | None = None
        eval_metrics: dict[str, int | float | None] | None = None
        for epoch in range(1, epochs + 1):
            model.train()
            indices = list(range(len(train_examples)))
            random.Random(seed + epoch).shuffle(indices)
            for start in range(0, len(indices), batch_size):
                optimizer.zero_grad(set_to_none=True)
                losses = [
                    loss
                    for example_index in indices[start : start + batch_size]
                    for _task, loss in _loss_terms(
                        model,
                        train_examples[example_index],
                        torch=torch,
                        functional=functional,
                        device=resolved_device,
                        action_kind_weights=resolved_action_weights,
                    )
                ]
                if not losses:
                    raise RuntimeError("distillation batch has no loss terms")
                loss = torch.stack(losses).mean()
                loss.backward()
                optimizer.step()
            train_metrics, eval_metrics = record(epoch)
        if train_metrics is None or eval_metrics is None:
            raise RuntimeError("distillation trainer did not record final metrics")
    return BehaviorDistillationTrainingResult(
        model=model,
        device=resolved_device.type,
        train_metrics=train_metrics,
        eval_metrics=eval_metrics,
        history=history,
    )


def evaluate_multi_task_behavior_distillation(
    model: Any,
    examples: Sequence[BehaviorDistillationExample],
    *,
    device: Any,
    action_kind_weights: Mapping[str, float] | None = None,
) -> dict[str, int | float | None]:
    """Evaluate unweighted mean losses for each available distillation task."""
    torch, _nn, functional, _data_loader, _dataset_base = require_torch_modules()
    sums = {task: 0.0 for task in _TASK_NAMES}
    counts = {task: 0 for task in _TASK_NAMES}
    model.eval()
    with torch.no_grad():
        for example in examples:
            for task, loss in _loss_terms(
                model,
                example,
                torch=torch,
                functional=functional,
                device=device,
                action_kind_weights=action_kind_weights,
            ):
                sums[task] += float(loss.detach().cpu())
                counts[task] += 1
    total_count = sum(counts.values())
    total_loss = sum(sums.values()) / total_count if total_count else None
    metrics: dict[str, int | float | None] = {"examples": len(examples), "loss": total_loss}
    for task in _TASK_NAMES:
        metrics[task + "_examples"] = counts[task]
        metrics[task + "_loss"] = sums[task] / counts[task] if counts[task] else None
    return metrics


def resolve_action_kind_loss_weights(
    examples: Sequence[BehaviorDistillationExample],
    *,
    action_kind_weights: Mapping[ActionKind | str, float] | None = None,
    balance_action_kinds: bool = False,
) -> dict[str, float]:
    """Resolve explicit and inverse-frequency weights for observed action kinds."""
    explicit = _validated_action_kind_weights(action_kind_weights)
    counts = Counter(_action_kind_for_index(example.action_target) for example in examples)
    if not counts:
        raise ValueError("cannot resolve action weights for zero examples")
    total = sum(counts.values())
    kinds = len(counts)
    return {
        kind: explicit.get(kind, 1.0)
        * (total / (kinds * count) if balance_action_kinds else 1.0)
        for kind, count in counts.items()
    }


def _trajectory_examples(
    trajectory: Mapping[str, Any],
    *,
    index: int,
    ruleset: str,
    players: int,
) -> list[BehaviorDistillationExample]:
    state = trajectory.get("state")
    if not isinstance(state, Mapping):
        raise ValueError(f"trajectory {index} state must be an object")
    seat = _required_int(trajectory, "seat", context=f"trajectory {index}")
    observation = observation_v1_tensor(
        _observation_from_state(state, ruleset=ruleset, players=players, seat=seat)
    )
    legal_actions = _actions_from_payloads(
        _required_array(trajectory, "legal_actions", context=f"trajectory {index}"),
        ruleset=ruleset,
    )
    legal_action_values = tuple(action_value for _action, action_value in legal_actions)
    action_mask = LegalActionMaskV1.from_actions(legal_action_values, ruleset=ruleset).mask
    rankings = trajectory.get("heuristic_rankings")
    if not isinstance(rankings, Mapping):
        raise ValueError(f"trajectory {index} heuristic_rankings must be an object")
    examples: list[BehaviorDistillationExample] = []
    for family in HEURISTIC_DISTILLATION_FAMILIES:
        candidate = _top_candidate(rankings.get(family), trajectory_index=index, family=family)
        if candidate is None:
            continue
        target, target_value = _action_from_payload(candidate.get("action"), ruleset=ruleset)
        if target not in tuple(action for action, _value in legal_actions):
            raise ValueError(f"trajectory {index} {family} target is not legal")
        tile_action, tile_mask, tile_target = _tile_target(target, legal_actions)
        meld_mask, meld_target = _meld_target(target, legal_actions)
        examples.append(
            BehaviorDistillationExample(
                family=family,
                observation=observation,
                action_mask=action_mask,
                action_target=action_v1_mask_index(target_value),
                tile_action=tile_action,
                tile_mask=tile_mask,
                tile_target=tile_target,
                meld_mask=meld_mask,
                meld_target=meld_target,
            )
        )
    return examples


def _top_candidate(value: Any, *, trajectory_index: int, family: str) -> Mapping[str, Any] | None:
    if not isinstance(value, list):
        raise ValueError(f"trajectory {trajectory_index} {family} ranking must be an array")
    if not value:
        return None
    candidates = [candidate for candidate in value if isinstance(candidate, Mapping)]
    if len(candidates) != len(value):
        raise ValueError(f"trajectory {trajectory_index} {family} candidate must be an object")
    try:
        return min(
            candidates,
            key=lambda candidate: _required_int(candidate, "rank", context=family),
        )
    except ValueError as error:
        raise ValueError(
            f"trajectory {trajectory_index} {family} candidate rank is invalid"
        ) from error


def _actions_from_payloads(
    payloads: Sequence[Any],
    *,
    ruleset: str,
) -> tuple[tuple[Action, ActionV1], ...]:
    if not payloads:
        raise ValueError("legal_actions cannot be empty")
    return tuple(_action_from_payload(payload, ruleset=ruleset) for payload in payloads)


def _action_from_payload(payload: Any, *, ruleset: str) -> tuple[Action, ActionV1]:
    if not isinstance(payload, Mapping):
        raise ValueError("action must be an object")
    kind = _required_str(payload, "kind", context="action")
    tile = payload.get("tile")
    consumed = _required_array(payload, "consumed", context="action")
    tsumogiri = payload.get("tsumogiri")
    if tile is not None and not isinstance(tile, str):
        raise ValueError("action tile must be a string or null")
    if not isinstance(tsumogiri, bool):
        raise ValueError("action tsumogiri must be a boolean")
    if any(not isinstance(value, str) for value in consumed):
        raise ValueError("action consumed entries must be strings")
    action = Action(
        ActionKind(kind),
        None if tile is None else TileType.parse(tile),
        tsumogiri=tsumogiri,
        consumed=tuple(Tile.parse(value) for value in consumed),
    )
    return action, ActionV1.from_core(action, ruleset=ruleset)


def _tile_target(
    target: Action,
    legal_actions: Sequence[tuple[Action, ActionV1]],
) -> tuple[str | None, tuple[bool, ...] | None, int | None]:
    if target.kind.value not in TILE_ARGUMENT_ACTIONS:
        return None, None, None
    if target.tile is None:
        raise ValueError(f"{target.kind.value} target requires a tile")
    mask = [False] * TILE_ARGUMENT_DIM
    for action, _value in legal_actions:
        if action.kind is target.kind and action.tile is not None:
            mask[action.tile.index] = True
    return target.kind.value, tuple(mask), target.tile.index


def _meld_target(
    target: Action,
    legal_actions: Sequence[tuple[Action, ActionV1]],
) -> tuple[tuple[bool, ...] | None, int | None]:
    if target.kind.value not in MELD_ARGUMENT_ACTIONS:
        return None, None
    mask = meld_selection_mask(tuple(action for action, _value in legal_actions))
    return mask, meld_selection_index(target)


def _observation_from_state(
    state: Mapping[str, Any],
    *,
    ruleset: str,
    players: int,
    seat: int,
) -> ObservationV1:
    hands = _required_array(state, "hands", context="state")
    if not 0 <= seat < len(hands):
        raise ValueError("state hand rows do not include the acting seat")
    current_seat = _required_int(state, "current_seat", context="state")
    drawn_tile = _optional_str(state, "drawn_tile", context="state")
    return ObservationV1(
        ruleset=ruleset,
        players=players,
        seat=seat,
        turn=_required_int(state, "turn", context="state"),
        current_seat=current_seat,
        dealer_seat=_required_int(state, "dealer_seat", context="state"),
        round_wind=_required_str(state, "round_wind", context="state"),
        honba=_required_int(state, "honba", context="state"),
        riichi_sticks=_required_int(state, "riichi_sticks", context="state"),
        points=_integer_tuple(_required_array(state, "points", context="state"), "state points"),
        wall_remaining=_required_int(state, "wall_remaining", context="state"),
        dead_wall_remaining=_required_int(state, "dead_wall_remaining", context="state"),
        hand=_string_tuple(hands[seat], "state acting hand"),
        hand_sizes=_integer_tuple(
            _required_array(state, "hand_sizes", context="state"), "state hand_sizes"
        ),
        discards=_nested_string_tuples(
            _required_array(state, "discards", context="state"), "state discards"
        ),
        melds=_nested_melds(_required_array(state, "melds", context="state")),
        kita_tiles=_nested_string_tuples(
            _required_array(state, "kita_tiles", context="state"), "state kita_tiles"
        ),
        dora_indicators=_string_tuple(
            _required_array(state, "dora_indicators", context="state"), "state dora_indicators"
        ),
        drawn_tile=drawn_tile if seat == current_seat else None,
        needs_discard=_required_bool(state, "needs_discard", context="state"),
        rinshan_draw=_required_bool(state, "rinshan_draw", context="state"),
        last_draw_was_final_live_wall=_required_bool(
            state,
            "last_draw_was_final_live_wall",
            context="state",
        ),
        pending_discard=_optional_str(state, "pending_discard", context="state"),
        pending_discard_seat=_optional_int(state, "pending_discard_seat", context="state"),
        pending_chankan_tile=_optional_str(state, "pending_chankan_tile", context="state"),
        pending_chankan_seat=_optional_int(state, "pending_chankan_seat", context="state"),
        pending_chankan_kind=_optional_str(state, "pending_chankan_kind", context="state"),
        pending_kita_tile=_optional_str(state, "pending_kita_tile", context="state"),
        pending_kita_seat=_optional_int(state, "pending_kita_seat", context="state"),
        pending_abortive_draw_reason=_optional_str(
            state,
            "pending_abortive_draw_reason",
            context="state",
        ),
        abortive_draw_after_discard_reason=_optional_str(
            state,
            "abortive_draw_after_discard_reason",
            context="state",
        ),
        pending_reaction_seats=_integer_tuple(
            _required_array(state, "pending_reaction_seats", context="state"),
            "state pending_reaction_seats",
        ),
        riichi_seats=_integer_tuple(
            _required_array(state, "riichi_seats", context="state"), "state riichi_seats"
        ),
        double_riichi_seats=_integer_tuple(
            _required_array(state, "double_riichi_seats", context="state"),
            "state double_riichi_seats",
        ),
        riichi_pending_discard_seats=_integer_tuple(
            _required_array(state, "riichi_pending_discard_seats", context="state"),
            "state riichi_pending_discard_seats",
        ),
        ippatsu_seats=_integer_tuple(
            _required_array(state, "ippatsu_seats", context="state"), "state ippatsu_seats"
        ),
        terminal_reason=_optional_str(state, "terminal_reason", context="state"),
        game_finished=_required_bool(state, "game_finished", context="state"),
    )


def _loss_terms(
    model: Any,
    example: BehaviorDistillationExample,
    *,
    torch: Any,
    functional: Any,
    device: Any,
    action_kind_weights: Mapping[str, float] | None = None,
) -> tuple[tuple[str, Any], ...]:
    observation = torch.tensor([example.observation], dtype=torch.float32, device=device)
    action_mask = torch.tensor([example.action_mask], dtype=torch.bool, device=device)
    action_target = torch.tensor([example.action_target], dtype=torch.long, device=device)
    weight = (action_kind_weights or {}).get(_action_kind_for_index(example.action_target), 1.0)
    terms: list[tuple[str, Any]] = [
        (
            "action",
            functional.cross_entropy(model(observation, action_mask), action_target) * weight,
        )
    ]
    if example.tile_action is not None:
        assert example.tile_mask is not None
        assert example.tile_target is not None
        tile_mask = torch.tensor([example.tile_mask], dtype=torch.bool, device=device)
        tile_target = torch.tensor([example.tile_target], dtype=torch.long, device=device)
        terms.append(
            (
                "tile",
                functional.cross_entropy(
                    model.tile_logits(observation, example.tile_action, tile_mask),
                    tile_target,
                )
                * weight,
            )
        )
    if example.meld_mask is not None:
        assert example.meld_target is not None
        meld_mask = torch.tensor([example.meld_mask], dtype=torch.bool, device=device)
        meld_target = torch.tensor([example.meld_target], dtype=torch.long, device=device)
        terms.append(
            (
                "meld",
                functional.cross_entropy(model.meld_logits(observation, meld_mask), meld_target)
                * weight,
            )
        )
    if example.value_target is not None:
        value_target = torch.tensor([example.value_target], dtype=torch.float32, device=device)
        terms.append(("value", functional.mse_loss(model.value(observation), value_target)))
    return tuple(terms)


def _validated_action_kind_weights(
    values: Mapping[ActionKind | str, float] | None,
) -> dict[str, float]:
    if values is None:
        return {}
    weights: dict[str, float] = {}
    for key, value in values.items():
        kind = ActionKind(key).value
        if not isinstance(value, (int, float)) or isinstance(value, bool) or value <= 0:
            raise ValueError("action kind weights must be positive numbers")
        weights[kind] = float(value)
    return weights


def _action_kind_for_index(index: int) -> str:
    for kind, offset in LEGAL_ACTION_MASK_V1_TILE_ACTION_OFFSETS.items():
        if offset <= index < offset + TILE_ARGUMENT_DIM:
            return kind
    kind = _SINGLE_ACTION_KINDS.get(index)
    if kind is None:
        raise ValueError(f"unsupported action target index: {index}")
    return kind


def _require_valid_manifest(manifest: Mapping[str, Any]) -> None:
    from kenjaku.simulation import validate_synthetic_corpus_integrity

    report = validate_synthetic_corpus_integrity(manifest)
    if report["valid"]:
        return
    errors = report["errors"]
    if not isinstance(errors, list) or not errors:
        raise ValueError("invalid distillation manifest")
    raise ValueError("invalid distillation manifest: " + str(errors[0]))


def _validate_mask(mask: tuple[bool, ...], width: int, name: str) -> None:
    if (
        not isinstance(mask, tuple)
        or len(mask) != width
        or not all(isinstance(value, bool) for value in mask)
    ):
        raise ValueError(f"{name} must be an immutable {width}-boolean mask")
    if not any(mask):
        raise ValueError(f"{name} cannot be empty")


def _validate_target(target: int, mask: tuple[bool, ...], name: str) -> None:
    if not isinstance(target, int) or isinstance(target, bool) or not 0 <= target < len(mask):
        raise ValueError(f"{name} is outside its mask")
    if not mask[target]:
        raise ValueError(f"{name} must be enabled by its mask")


def _validate_tile_fields(
    action: str | None,
    mask: tuple[bool, ...] | None,
    target: int | None,
) -> None:
    if (action, mask, target) == (None, None, None):
        return
    if not isinstance(action, str) or mask is None or target is None:
        raise ValueError("tile fields must be all present or all null")
    if action not in TILE_ARGUMENT_ACTIONS:
        raise ValueError("tile_action has no tile argument head")
    _validate_mask(mask, TILE_ARGUMENT_DIM, "tile_mask")
    _validate_target(target, mask, "tile_target")


def _validate_meld_fields(mask: tuple[bool, ...] | None, target: int | None) -> None:
    if mask is None and target is None:
        return
    if mask is None or target is None:
        raise ValueError("meld fields must be both present or both null")
    _validate_mask(mask, MELD_SELECTION_DIM, "meld_mask")
    _validate_target(target, mask, "meld_target")


def _required_array(payload: Mapping[str, Any], field: str, *, context: str) -> Sequence[Any]:
    value = payload.get(field)
    if isinstance(value, (str, bytes)) or not isinstance(value, Sequence):
        raise ValueError(f"{context} {field} must be an array")
    return value


def _required_str(payload: Mapping[str, Any], field: str, *, context: str) -> str:
    value = payload.get(field)
    if not isinstance(value, str):
        raise ValueError(f"{context} {field} must be a string")
    return value


def _optional_str(payload: Mapping[str, Any], field: str, *, context: str) -> str | None:
    value = payload.get(field)
    if value is None:
        return None
    if not isinstance(value, str):
        raise ValueError(f"{context} {field} must be a string or null")
    return value


def _required_int(payload: Mapping[str, Any], field: str, *, context: str) -> int:
    value = payload.get(field)
    if not isinstance(value, int) or isinstance(value, bool):
        raise ValueError(f"{context} {field} must be an integer")
    return value


def _optional_int(payload: Mapping[str, Any], field: str, *, context: str) -> int | None:
    value = payload.get(field)
    if value is None:
        return None
    if not isinstance(value, int) or isinstance(value, bool):
        raise ValueError(f"{context} {field} must be an integer or null")
    return value


def _required_bool(payload: Mapping[str, Any], field: str, *, context: str) -> bool:
    value = payload.get(field)
    if not isinstance(value, bool):
        raise ValueError(f"{context} {field} must be a boolean")
    return value


def _integer_tuple(values: Any, name: str) -> tuple[int, ...]:
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(name + " must be an array")
    if any(not isinstance(value, int) or isinstance(value, bool) for value in values):
        raise ValueError(name + " entries must be integers")
    return tuple(values)


def _string_tuple(values: Any, name: str) -> tuple[str, ...]:
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(name + " must be an array")
    if any(not isinstance(value, str) for value in values):
        raise ValueError(name + " entries must be strings")
    return tuple(values)


def _nested_string_tuples(values: Sequence[Any], name: str) -> tuple[tuple[str, ...], ...]:
    return tuple(_string_tuple(value, name + " row") for value in values)


def _nested_melds(values: Sequence[Any]) -> tuple[tuple[MeldV1, ...], ...]:
    rows: list[tuple[MeldV1, ...]] = []
    for row in values:
        if isinstance(row, (str, bytes)) or not isinstance(row, Sequence):
            raise ValueError("state melds rows must be arrays")
        if not all(isinstance(meld, Mapping) for meld in row):
            raise ValueError("state meld entries must be objects")
        rows.append(tuple(MeldV1.from_dict(meld) for meld in row))
    return tuple(rows)
