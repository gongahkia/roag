"""Masked policy head over the versioned observation and action schemas."""

from __future__ import annotations

from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any

from kenjaku.core import Action, ActionKind, TileType
from kenjaku.models.torch_discard import require_torch_modules
from kenjaku.schema import LEGAL_ACTION_MASK_V1_DIM, OBSERVATION_V1_TENSOR_DIM

MULTI_ACTION_POLICY_HEAD_KIND = "masked-multi-action-policy-head-v0"
MULTI_ACTION_POLICY_INPUT_DIM = OBSERVATION_V1_TENSOR_DIM
MULTI_ACTION_POLICY_ACTION_DIM = LEGAL_ACTION_MASK_V1_DIM
TILE_ARGUMENT_DIM = 34
TILE_ARGUMENT_ACTIONS = (
    ActionKind.DISCARD.value,
    ActionKind.RON.value,
    ActionKind.CHI.value,
    ActionKind.PON.value,
    ActionKind.MINKAN.value,
    ActionKind.ANKAN.value,
    ActionKind.KAKAN.value,
    ActionKind.KITA.value,
)
MELD_ARGUMENT_ACTIONS = (
    ActionKind.CHI.value,
    ActionKind.PON.value,
    ActionKind.MINKAN.value,
    ActionKind.ANKAN.value,
    ActionKind.KAKAN.value,
)
PLACEMENT_ADJUSTED_VALUE_HEAD_KIND = "placement-adjusted-value-head-v0"

try:
    from torch import nn as _nn
except ImportError:  # pragma: no cover - exercised only without the ml extra
    _nn = None

_TorchModuleBase = _nn.Module if _nn is not None else object


@dataclass(frozen=True, slots=True)
class MultiActionPolicyConfig:
    """Fixed schema dimensions and the shared policy hidden width."""

    input_dim: int = MULTI_ACTION_POLICY_INPUT_DIM
    hidden_dim: int = 256
    action_dim: int = MULTI_ACTION_POLICY_ACTION_DIM

    def __post_init__(self) -> None:
        if self.input_dim != MULTI_ACTION_POLICY_INPUT_DIM:
            raise ValueError(f"input_dim must be {MULTI_ACTION_POLICY_INPUT_DIM}")
        if self.hidden_dim <= 0:
            raise ValueError("hidden_dim must be positive")
        if self.action_dim != MULTI_ACTION_POLICY_ACTION_DIM:
            raise ValueError(f"action_dim must be {MULTI_ACTION_POLICY_ACTION_DIM}")


@dataclass(frozen=True, slots=True)
class MeldSelection:
    """One canonical logical meld composition used by the meld argument head."""

    action: str
    tiles: tuple[TileType, ...]


def _meld_selections() -> tuple[MeldSelection, ...]:
    chi = tuple(
        MeldSelection(
            action=ActionKind.CHI.value,
            tiles=tuple(TileType(offset + rank) for rank in range(start, start + 3)),
        )
        for offset in (0, 9, 18)
        for start in range(7)
    )
    same_type = tuple(
        MeldSelection(action=action, tiles=(TileType(index),) * size)
        for action, size in (
            (ActionKind.PON.value, 3),
            (ActionKind.MINKAN.value, 4),
            (ActionKind.ANKAN.value, 4),
            (ActionKind.KAKAN.value, 1),
        )
        for index in range(TILE_ARGUMENT_DIM)
    )
    return (*chi, *same_type)


MELD_SELECTIONS = _meld_selections()
MELD_SELECTION_DIM = len(MELD_SELECTIONS)
_MELD_SELECTION_INDEX = {
    (selection.action, selection.tiles): index
    for index, selection in enumerate(MELD_SELECTIONS)
}


def meld_selection_index(action: Action) -> int:
    """Return the canonical logical-meld coordinate for a core meld action."""
    kind = action.kind.value
    if kind not in MELD_ARGUMENT_ACTIONS:
        raise ValueError(f"{kind} has no meld argument")
    if action.tile is None:
        raise ValueError(f"{kind} meld action requires a tile")
    if action.kind is ActionKind.CHI:
        if len(action.consumed) != 2:
            raise ValueError("chi meld actions require two consumed tiles")
        tiles = tuple(sorted((action.tile, *(tile.type for tile in action.consumed))))
    elif action.kind is ActionKind.PON:
        tiles = (action.tile,) * 3
    elif action.kind in {ActionKind.MINKAN, ActionKind.ANKAN}:
        tiles = (action.tile,) * 4
    else:
        tiles = (action.tile,)
    try:
        return _MELD_SELECTION_INDEX[(kind, tiles)]
    except KeyError as error:
        raise ValueError(f"unsupported {kind} meld composition") from error


def meld_selection_mask(actions: Sequence[Action]) -> tuple[bool, ...]:
    """Encode legal core meld actions into the canonical meld-head mask."""
    mask = [False] * MELD_SELECTION_DIM
    for action in actions:
        if action.kind.value in MELD_ARGUMENT_ACTIONS:
            mask[meld_selection_index(action)] = True
    if not any(mask):
        raise ValueError("meld selection mask cannot be empty")
    return tuple(mask)


def placement_adjusted_outcome(final_result: Mapping[str, Any] | object, *, seat: int) -> float:
    """Return one seat's final score with return, Uma, and Oka adjustments."""
    if not isinstance(seat, int) or isinstance(seat, bool):
        raise ValueError("seat must be an integer")
    if not isinstance(final_result, Mapping):
        to_payload = getattr(final_result, "to_payload", None)
        if not callable(to_payload):
            raise ValueError("final_result must be a final-result payload")
        final_result = to_payload()
        if not isinstance(final_result, Mapping):
            raise ValueError("final_result payload must be an object")
    points = _integer_tuple(final_result, "points")
    placement = _integer_tuple(final_result, "placement")
    uma_by_rank = _number_tuple(final_result, "uma_by_rank")
    return_points = _integer_value(final_result, "return_points")
    oka_points = _integer_value(final_result, "oka_points")
    players = len(points)
    if players not in {3, 4}:
        raise ValueError("final_result must contain three or four players")
    if not 0 <= seat < players:
        raise ValueError("seat outside final_result player range")
    if sorted(placement) != list(range(players)):
        raise ValueError("final_result placement must order every seat exactly once")
    if len(uma_by_rank) != players:
        raise ValueError("final_result Uma count must match player count")
    rank_index = placement.index(seat)
    score = (points[seat] - return_points) / 1000.0 + uma_by_rank[rank_index]
    if rank_index == 0:
        score += oka_points / 1000.0
    return score


class MaskedMultiActionPolicyHead(_TorchModuleBase):
    """MLP policy logits with every illegal shared action coordinate excluded."""

    def __init__(
        self,
        config: MultiActionPolicyConfig | None = None,
        *,
        seed: int | None = None,
    ) -> None:
        torch, nn, _functional, _data_loader, _dataset_base = require_torch_modules()
        super().__init__()
        self.kind = MULTI_ACTION_POLICY_HEAD_KIND
        self.config = config or MultiActionPolicyConfig()
        self.input_dim = self.config.input_dim
        self.hidden_dim = self.config.hidden_dim
        self.action_dim = self.config.action_dim
        self._torch = torch
        if seed is None:
            self._init_modules(nn)
        else:
            with torch.random.fork_rng():
                torch.manual_seed(seed)
                self._init_modules(nn)

    def forward(self, observations: Any, legal_masks: Any) -> Any:
        """Return logits with illegal actions set to negative infinity."""
        observations, squeeze = self._batched_observations(observations)
        legal_masks = self._batched_legal_masks(legal_masks, batch_size=observations.shape[0])
        logits = self.net(observations)
        legal_masks = legal_masks.to(logits.device)
        masked_logits = logits.masked_fill(~legal_masks, float("-inf"))
        return masked_logits.squeeze(0) if squeeze else masked_logits

    def probabilities(self, observations: Any, legal_masks: Any) -> Any:
        """Return a normalized distribution over legal action coordinates only."""
        return self._torch.softmax(self.forward(observations, legal_masks), dim=-1)

    def encode(self, observations: Any) -> Any:
        """Encode one or more versioned observation tensors for auxiliary heads."""
        observations, squeeze = self._batched_observations(observations)
        encoded = self.net[:2](observations)
        return encoded.squeeze(0) if squeeze else encoded

    def tile_logits(self, observations: Any, action: ActionKind | str, legal_masks: Any) -> Any:
        """Return masked logical-tile logits for one tile-bearing action kind."""
        return self.argument_heads.tile_logits(self.encode(observations), action, legal_masks)

    def meld_logits(self, observations: Any, legal_masks: Any) -> Any:
        """Return masked canonical-meld logits for legal meld compositions."""
        return self.argument_heads.meld_logits(self.encode(observations), legal_masks)

    def value(self, observations: Any) -> Any:
        """Predict the actor's placement-adjusted final-score outcome."""
        return self.value_head(self.encode(observations))

    def _init_modules(self, nn: Any) -> None:
        self.net = _policy_network(nn, self.input_dim, self.hidden_dim, self.action_dim)
        self.argument_heads = ActionSpecificArgumentHeads(self.hidden_dim)
        self.value_head = PlacementAdjustedValueHead(self.hidden_dim)

    def _batched_observations(self, observations: Any) -> tuple[Any, bool]:
        if not hasattr(observations, "ndim"):
            raise ValueError("observations must be a torch tensor")
        if observations.ndim == 1:
            if observations.shape[0] != self.input_dim:
                raise ValueError(f"observations must have {self.input_dim} features")
            return observations.unsqueeze(0), True
        if observations.ndim != 2:
            raise ValueError("observations must have rank 1 or 2")
        if observations.shape[1] != self.input_dim:
            raise ValueError(f"observations must have {self.input_dim} features")
        return observations, False

    def _batched_legal_masks(self, legal_masks: Any, *, batch_size: int) -> Any:
        if not hasattr(legal_masks, "ndim"):
            raise ValueError("legal_masks must be a torch boolean tensor")
        if legal_masks.dtype != self._torch.bool:
            raise ValueError("legal_masks must be a torch boolean tensor")
        if legal_masks.ndim == 1:
            if batch_size != 1:
                raise ValueError("one-dimensional legal_masks require one observation")
            legal_masks = legal_masks.unsqueeze(0)
        elif legal_masks.ndim != 2:
            raise ValueError("legal_masks must have rank 1 or 2")
        if legal_masks.shape != (batch_size, self.action_dim):
            raise ValueError(
                f"legal_masks must have shape ({batch_size}, {self.action_dim})"
            )
        if not bool(legal_masks.any(dim=1).all().item()):
            raise ValueError("each legal_masks row must enable at least one action")
        return legal_masks


class ActionSpecificArgumentHeads(_TorchModuleBase):
    """Conditional tile and logical-meld heads over a shared policy representation."""

    def __init__(self, hidden_dim: int) -> None:
        torch, nn, _functional, _data_loader, _dataset_base = require_torch_modules()
        super().__init__()
        if hidden_dim <= 0:
            raise ValueError("hidden_dim must be positive")
        self.hidden_dim = hidden_dim
        self._torch = torch
        self.tile_heads = nn.ModuleDict(
            {action: nn.Linear(hidden_dim, TILE_ARGUMENT_DIM) for action in TILE_ARGUMENT_ACTIONS}
        )
        self.meld_head = nn.Linear(hidden_dim, MELD_SELECTION_DIM)

    def tile_logits(self, representations: Any, action: ActionKind | str, legal_masks: Any) -> Any:
        """Return masked logical-tile logits for the selected action kind."""
        action_name = _tile_argument_action(action)
        representations, squeeze = self._batched_representations(representations)
        legal_masks = self._batched_mask(
            legal_masks,
            batch_size=representations.shape[0],
            width=TILE_ARGUMENT_DIM,
            name="tile legal_masks",
        )
        logits = self.tile_heads[action_name](representations)
        logits = logits.masked_fill(~legal_masks.to(logits.device), float("-inf"))
        return logits.squeeze(0) if squeeze else logits

    def meld_logits(self, representations: Any, legal_masks: Any) -> Any:
        """Return masked logits over canonical chi, pon, and kan compositions."""
        representations, squeeze = self._batched_representations(representations)
        legal_masks = self._batched_mask(
            legal_masks,
            batch_size=representations.shape[0],
            width=MELD_SELECTION_DIM,
            name="meld legal_masks",
        )
        logits = self.meld_head(representations)
        logits = logits.masked_fill(~legal_masks.to(logits.device), float("-inf"))
        return logits.squeeze(0) if squeeze else logits

    def _batched_representations(self, representations: Any) -> tuple[Any, bool]:
        if not hasattr(representations, "ndim"):
            raise ValueError("representations must be a torch tensor")
        if representations.ndim == 1:
            if representations.shape[0] != self.hidden_dim:
                raise ValueError(f"representations must have {self.hidden_dim} features")
            return representations.unsqueeze(0), True
        if representations.ndim != 2:
            raise ValueError("representations must have rank 1 or 2")
        if representations.shape[1] != self.hidden_dim:
            raise ValueError(f"representations must have {self.hidden_dim} features")
        return representations, False

    def _batched_mask(self, masks: Any, *, batch_size: int, width: int, name: str) -> Any:
        if not hasattr(masks, "ndim") or masks.dtype != self._torch.bool:
            raise ValueError(f"{name} must be a torch boolean tensor")
        if masks.ndim == 1:
            if batch_size != 1:
                raise ValueError(f"one-dimensional {name} require one representation")
            masks = masks.unsqueeze(0)
        elif masks.ndim != 2:
            raise ValueError(f"{name} must have rank 1 or 2")
        if masks.shape != (batch_size, width):
            raise ValueError(f"{name} must have shape ({batch_size}, {width})")
        if not bool(masks.any(dim=1).all().item()):
            raise ValueError(f"each {name} row must enable at least one argument")
        return masks


class PlacementAdjustedValueHead(_TorchModuleBase):
    """Scalar regression head for actor-relative placement-adjusted final scores."""

    def __init__(self, hidden_dim: int) -> None:
        _torch, nn, _functional, _data_loader, _dataset_base = require_torch_modules()
        super().__init__()
        if hidden_dim <= 0:
            raise ValueError("hidden_dim must be positive")
        self.kind = PLACEMENT_ADJUSTED_VALUE_HEAD_KIND
        self.hidden_dim = hidden_dim
        self.head = nn.Linear(hidden_dim, 1)

    def forward(self, representations: Any) -> Any:
        """Return one scalar value per representation, preserving single-input shape."""
        if not hasattr(representations, "ndim"):
            raise ValueError("representations must be a torch tensor")
        if representations.ndim == 1:
            if representations.shape[0] != self.hidden_dim:
                raise ValueError(f"representations must have {self.hidden_dim} features")
            return self.head(representations).squeeze(-1)
        if representations.ndim != 2:
            raise ValueError("representations must have rank 1 or 2")
        if representations.shape[1] != self.hidden_dim:
            raise ValueError(f"representations must have {self.hidden_dim} features")
        return self.head(representations).squeeze(-1)


def _tile_argument_action(action: ActionKind | str) -> str:
    action_name = action.value if isinstance(action, ActionKind) else action
    if action_name not in TILE_ARGUMENT_ACTIONS:
        raise ValueError(f"{action_name} has no tile argument head")
    return action_name


def _integer_tuple(payload: Mapping[str, Any], field: str) -> tuple[int, ...]:
    values = payload.get(field)
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"final_result {field} must be an array")
    if any(not isinstance(value, int) or isinstance(value, bool) for value in values):
        raise ValueError(f"final_result {field} entries must be integers")
    return tuple(values)


def _number_tuple(payload: Mapping[str, Any], field: str) -> tuple[float, ...]:
    values = payload.get(field)
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"final_result {field} must be an array")
    if any(not isinstance(value, (int, float)) or isinstance(value, bool) for value in values):
        raise ValueError(f"final_result {field} entries must be numbers")
    return tuple(float(value) for value in values)


def _integer_value(payload: Mapping[str, Any], field: str) -> int:
    value = payload.get(field)
    if not isinstance(value, int) or isinstance(value, bool):
        raise ValueError(f"final_result {field} must be an integer")
    return value


def _policy_network(nn: Any, input_dim: int, hidden_dim: int, action_dim: int) -> Any:
    return nn.Sequential(
        nn.Linear(input_dim, hidden_dim),
        nn.ReLU(),
        nn.Linear(hidden_dim, action_dim),
    )
