"""Local CPU/MPS inference benchmarks for the multi-action policy model."""

from __future__ import annotations

import math
import platform
import statistics
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from time import perf_counter
from typing import Any

from kenjaku.models.multi_action_policy import (
    MULTI_ACTION_POLICY_ACTION_DIM,
    MULTI_ACTION_POLICY_HEAD_KIND,
    MULTI_ACTION_POLICY_INPUT_DIM,
    MaskedMultiActionPolicyHead,
    MultiActionPolicyConfig,
)
from kenjaku.models.torch_discard import require_torch, resolve_torch_device

MODEL_DEVICE_BENCHMARK_KIND = "kenjaku-model-device-benchmark-v0"
_DEVICE_CHOICES = frozenset({"cpu", "mps"})


@dataclass(frozen=True, slots=True)
class ModelDeviceBenchmarkConfig:
    """Fixed workload settings for local model-device comparisons."""

    hidden_dims: tuple[int, ...] = (128, 256, 512)
    devices: tuple[str, ...] = ("cpu", "mps")
    batch_size: int = 64
    warmup_iterations: int = 10
    measurement_iterations: int = 50

    def __post_init__(self) -> None:
        if not self.hidden_dims or any(
            type(value) is not int or value <= 0 for value in self.hidden_dims
        ):
            raise ValueError("hidden_dims must contain positive integers")
        if len(set(self.hidden_dims)) != len(self.hidden_dims):
            raise ValueError("hidden_dims must be unique")
        if not self.devices or any(device not in _DEVICE_CHOICES for device in self.devices):
            raise ValueError("devices must contain cpu and/or mps")
        if len(set(self.devices)) != len(self.devices):
            raise ValueError("devices must be unique")
        if self.batch_size <= 0:
            raise ValueError("batch_size must be positive")
        if self.warmup_iterations < 0:
            raise ValueError("warmup_iterations must be non-negative")
        if self.measurement_iterations <= 0:
            raise ValueError("measurement_iterations must be positive")


def parse_hidden_dims(value: str) -> tuple[int, ...]:
    """Parse a compact, validated model-width list for the command line."""
    try:
        hidden_dims = tuple(int(part) for part in value.split(",") if part)
    except ValueError as error:
        raise ValueError("hidden_dims must be comma-separated integers") from error
    ModelDeviceBenchmarkConfig(hidden_dims=hidden_dims)
    return hidden_dims


def parse_devices(value: str) -> tuple[str, ...]:
    """Parse the supported local device list for the command line."""
    devices = tuple(part.strip() for part in value.split(",") if part.strip())
    ModelDeviceBenchmarkConfig(devices=devices)
    return devices


def benchmark_multi_action_policy_devices(
    config: ModelDeviceBenchmarkConfig,
) -> dict[str, Any]:
    """Measure synchronized forward-pass latency per width and requested device."""
    torch = require_torch()
    rows: list[dict[str, Any]] = []
    for hidden_dim in config.hidden_dims:
        for device_name in config.devices:
            try:
                device = resolve_torch_device(device_name)
            except ValueError as error:
                rows.append(
                    {
                        "hidden_dim": hidden_dim,
                        "device": device_name,
                        "status": "unavailable",
                        "reason": str(error),
                    }
                )
                continue
            model = MaskedMultiActionPolicyHead(
                MultiActionPolicyConfig(hidden_dim=hidden_dim), seed=0
            ).to(device)
            observations = torch.zeros(
                (config.batch_size, MULTI_ACTION_POLICY_INPUT_DIM),
                dtype=torch.float32,
                device=device,
            )
            legal_masks = torch.ones(
                (config.batch_size, MULTI_ACTION_POLICY_ACTION_DIM),
                dtype=torch.bool,
                device=device,
            )
            durations = _measure_forward_passes(
                torch,
                model,
                observations,
                legal_masks,
                device=device,
                warmup_iterations=config.warmup_iterations,
                measurement_iterations=config.measurement_iterations,
            )
            parameter_count = sum(parameter.numel() for parameter in model.parameters())
            parameter_bytes = sum(
                parameter.numel() * parameter.element_size() for parameter in model.parameters()
            )
            rows.append(
                _measured_row(
                    hidden_dim=hidden_dim,
                    device=device_name,
                    batch_size=config.batch_size,
                    parameter_count=parameter_count,
                    parameter_bytes=parameter_bytes,
                    durations=durations,
                )
            )
    return build_model_device_benchmark_report(config, hardware_metadata(), rows)


def build_model_device_benchmark_report(
    config: ModelDeviceBenchmarkConfig,
    hardware: Mapping[str, str | None],
    rows: Sequence[Mapping[str, Any]],
) -> dict[str, Any]:
    """Build a serializable device benchmark report from measured rows."""
    expected = {
        (hidden_dim, device) for hidden_dim in config.hidden_dims for device in config.devices
    }
    found = {(row.get("hidden_dim"), row.get("device")) for row in rows}
    if found != expected or len(rows) != len(expected):
        raise ValueError("benchmark rows must cover each hidden_dim/device pair exactly once")
    normalized_rows = [_validate_benchmark_row(row) for row in rows]
    return {
        "kind": MODEL_DEVICE_BENCHMARK_KIND,
        "hardware": dict(hardware),
        "model": {
            "kind": MULTI_ACTION_POLICY_HEAD_KIND,
            "input_dim": MULTI_ACTION_POLICY_INPUT_DIM,
            "action_dim": MULTI_ACTION_POLICY_ACTION_DIM,
        },
        "workload": {
            "hidden_dims": list(config.hidden_dims),
            "devices": list(config.devices),
            "batch_size": config.batch_size,
            "warmup_iterations": config.warmup_iterations,
            "measurement_iterations": config.measurement_iterations,
        },
        "rows": normalized_rows,
    }


def hardware_metadata() -> dict[str, str | None]:
    """Return host identifiers needed to interpret a local CPU/MPS benchmark."""
    macos_version = platform.mac_ver()[0]
    processor = platform.processor()
    return {
        "system": platform.system() or None,
        "machine": platform.machine() or None,
        "processor": processor or None,
        "macos_version": macos_version or None,
    }


def _measure_forward_passes(
    torch: Any,
    model: Any,
    observations: Any,
    legal_masks: Any,
    *,
    device: Any,
    warmup_iterations: int,
    measurement_iterations: int,
) -> tuple[float, ...]:
    model.eval()
    with torch.inference_mode():
        for _ in range(warmup_iterations):
            model(observations, legal_masks)
        _synchronize(torch, device)
        durations: list[float] = []
        for _ in range(measurement_iterations):
            started_at = perf_counter()
            model(observations, legal_masks)
            _synchronize(torch, device)
            durations.append((perf_counter() - started_at) * 1000.0)
    return tuple(durations)


def _synchronize(torch: Any, device: Any) -> None:
    if device.type == "mps":
        torch.mps.synchronize()


def _measured_row(
    *,
    hidden_dim: int,
    device: str,
    batch_size: int,
    parameter_count: int,
    parameter_bytes: int,
    durations: Sequence[float],
) -> dict[str, Any]:
    if not durations or any(not math.isfinite(value) or value <= 0 for value in durations):
        raise ValueError("benchmark durations must be finite positive milliseconds")
    median_ms = statistics.median(durations)
    return {
        "hidden_dim": hidden_dim,
        "device": device,
        "status": "ok",
        "parameter_count": parameter_count,
        "parameter_bytes": parameter_bytes,
        "latency_ms": {
            "mean": statistics.fmean(durations),
            "median": median_ms,
            "p95": _nearest_rank_percentile(durations, 0.95),
        },
        "throughput_examples_per_second": batch_size * 1000.0 / median_ms,
    }


def _validate_benchmark_row(row: Mapping[str, Any]) -> dict[str, Any]:
    hidden_dim = row.get("hidden_dim")
    device = row.get("device")
    status = row.get("status")
    if type(hidden_dim) is not int or hidden_dim <= 0 or device not in _DEVICE_CHOICES:
        raise ValueError("benchmark row has an invalid hidden_dim or device")
    if status == "unavailable":
        reason = row.get("reason")
        if not isinstance(reason, str) or not reason:
            raise ValueError("unavailable benchmark rows require a reason")
        return {"hidden_dim": hidden_dim, "device": device, "status": status, "reason": reason}
    if status != "ok":
        raise ValueError("benchmark row status must be ok or unavailable")
    parameter_count = row.get("parameter_count")
    parameter_bytes = row.get("parameter_bytes")
    latency = row.get("latency_ms")
    throughput = row.get("throughput_examples_per_second")
    if (
        type(parameter_count) is not int
        or parameter_count <= 0
        or type(parameter_bytes) is not int
        or parameter_bytes <= 0
        or not isinstance(latency, Mapping)
        or not isinstance(throughput, (int, float))
        or isinstance(throughput, bool)
        or not math.isfinite(float(throughput))
        or float(throughput) <= 0
    ):
        raise ValueError("measured benchmark row is invalid")
    latency_values = {name: latency.get(name) for name in ("mean", "median", "p95")}
    if any(
        not isinstance(value, (int, float))
        or isinstance(value, bool)
        or not math.isfinite(float(value))
        or float(value) <= 0
        for value in latency_values.values()
    ):
        raise ValueError("benchmark latency values must be finite positive milliseconds")
    return {
        "hidden_dim": hidden_dim,
        "device": device,
        "status": status,
        "parameter_count": parameter_count,
        "parameter_bytes": parameter_bytes,
        "latency_ms": {name: float(value) for name, value in latency_values.items()},
        "throughput_examples_per_second": float(throughput),
    }


def _nearest_rank_percentile(values: Sequence[float], percentile: float) -> float:
    ordered = sorted(values)
    index = math.ceil(percentile * len(ordered)) - 1
    return ordered[index]
