"""Masked policy head over the versioned observation and action schemas."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from kenjaku.models.torch_discard import require_torch_modules
from kenjaku.schema import LEGAL_ACTION_MASK_V1_DIM, OBSERVATION_V1_TENSOR_DIM

MULTI_ACTION_POLICY_HEAD_KIND = "masked-multi-action-policy-head-v0"
MULTI_ACTION_POLICY_INPUT_DIM = OBSERVATION_V1_TENSOR_DIM
MULTI_ACTION_POLICY_ACTION_DIM = LEGAL_ACTION_MASK_V1_DIM

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
            self.net = _policy_network(nn, self.input_dim, self.hidden_dim, self.action_dim)
        else:
            with torch.random.fork_rng():
                torch.manual_seed(seed)
                self.net = _policy_network(nn, self.input_dim, self.hidden_dim, self.action_dim)

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


def _policy_network(nn: Any, input_dim: int, hidden_dim: int, action_dim: int) -> Any:
    return nn.Sequential(
        nn.Linear(input_dim, hidden_dim),
        nn.ReLU(),
        nn.Linear(hidden_dim, action_dim),
    )
