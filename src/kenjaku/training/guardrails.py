"""Local resource limits for optimizer-backed training loops."""

from __future__ import annotations

import math
from collections.abc import Callable, Iterable
from dataclasses import dataclass
from time import perf_counter
from typing import Any

DEFAULT_MAX_OPTIMIZER_STATE_BYTES = 512 * 1024 * 1024
DEFAULT_TRAINING_TIMEOUT_SECONDS = 60.0 * 60.0


@dataclass(frozen=True, slots=True)
class OptimizerResourceLimits:
    """Bound local optimizer state allocation and elapsed training time."""

    max_optimizer_state_bytes: int | None = DEFAULT_MAX_OPTIMIZER_STATE_BYTES
    timeout_seconds: float | None = DEFAULT_TRAINING_TIMEOUT_SECONDS

    def __post_init__(self) -> None:
        if self.max_optimizer_state_bytes is not None and (
            type(self.max_optimizer_state_bytes) is not int
            or self.max_optimizer_state_bytes <= 0
        ):
            raise ValueError("max_optimizer_state_bytes must be a positive integer or null")
        if self.timeout_seconds is not None and (
            not isinstance(self.timeout_seconds, (int, float))
            or isinstance(self.timeout_seconds, bool)
            or not math.isfinite(float(self.timeout_seconds))
            or self.timeout_seconds <= 0
        ):
            raise ValueError("timeout_seconds must be a finite positive number or null")

    def enforce_adamw_state_budget(self, parameters: Iterable[Any]) -> int:
        """Reject AdamW state that would exceed the local allocation limit."""
        estimated = estimate_adamw_state_bytes(parameters)
        if (
            self.max_optimizer_state_bytes is not None
            and estimated > self.max_optimizer_state_bytes
        ):
            raise ValueError(
                "estimated AdamW optimizer state exceeds local limit: "
                f"{estimated} > {self.max_optimizer_state_bytes} bytes"
            )
        return estimated

    def deadline(self, *, clock: Callable[[], float] = perf_counter) -> TrainingDeadline:
        """Create one cooperative deadline for a full training invocation."""
        return TrainingDeadline(timeout_seconds=self.timeout_seconds, clock=clock)


@dataclass(slots=True)
class TrainingDeadline:
    """Raise at explicit safe points after the configured local time budget."""

    timeout_seconds: float | None
    clock: Callable[[], float] = perf_counter
    _started_at: float = 0.0

    def __post_init__(self) -> None:
        OptimizerResourceLimits(timeout_seconds=self.timeout_seconds)
        self._started_at = self.clock()

    def check(self, phase: str) -> None:
        if self.timeout_seconds is None:
            return
        elapsed = self.clock() - self._started_at
        if elapsed > self.timeout_seconds:
            raise TimeoutError(f"local training timeout exceeded during {phase}")


def estimate_adamw_state_bytes(parameters: Iterable[Any]) -> int:
    """Estimate AdamW exp_avg and exp_avg_sq tensor allocation in bytes."""
    total = 0
    for parameter in parameters:
        numel = parameter.numel()
        element_size = parameter.element_size()
        if (
            type(numel) is not int
            or numel < 0
            or type(element_size) is not int
            or element_size <= 0
        ):
            raise ValueError("optimizer parameters must expose non-negative numel and element_size")
        total += 2 * numel * element_size
    return total


def mib_to_bytes(value: int) -> int:
    """Convert a positive MiB command-line limit into bytes."""
    if type(value) is not int or value <= 0:
        raise ValueError("max_optimizer_state_mib must be a positive integer")
    return value * 1024 * 1024
