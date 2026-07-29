from __future__ import annotations

import importlib.util
import unittest

from kenjaku.commands._legacy import build_parser
from kenjaku.device_benchmark import (
    MODEL_DEVICE_BENCHMARK_KIND,
    ModelDeviceBenchmarkConfig,
    benchmark_multi_action_policy_devices,
    build_model_device_benchmark_report,
    parse_devices,
    parse_hidden_dims,
)

TORCH_AVAILABLE = importlib.util.find_spec("torch") is not None


class ModelDeviceBenchmarkTests(unittest.TestCase):
    def test_parses_configuration_and_builds_complete_report(self) -> None:
        config = ModelDeviceBenchmarkConfig(
            hidden_dims=parse_hidden_dims("64,128"),
            devices=parse_devices("cpu,mps"),
            batch_size=8,
            warmup_iterations=1,
            measurement_iterations=2,
        )
        report = build_model_device_benchmark_report(
            config,
            {
                "system": "Darwin",
                "machine": "arm64",
                "processor": "Apple M3",
                "macos_version": "26",
            },
            (
                _measured_row(hidden_dim=64, device="cpu"),
                {
                    "hidden_dim": 64,
                    "device": "mps",
                    "status": "unavailable",
                    "reason": "MPS is not available",
                },
                _measured_row(hidden_dim=128, device="cpu"),
                _measured_row(hidden_dim=128, device="mps"),
            ),
        )

        self.assertEqual(report["kind"], MODEL_DEVICE_BENCHMARK_KIND)
        self.assertEqual(report["workload"]["hidden_dims"], [64, 128])
        self.assertEqual(report["rows"][1]["status"], "unavailable")

    def test_rejects_invalid_configuration_and_incomplete_rows(self) -> None:
        with self.assertRaisesRegex(ValueError, "unique"):
            ModelDeviceBenchmarkConfig(hidden_dims=(64, 64))
        with self.assertRaisesRegex(ValueError, "comma-separated"):
            parse_hidden_dims("64,bad")
        with self.assertRaisesRegex(ValueError, "cpu and/or mps"):
            parse_devices("cuda")
        with self.assertRaisesRegex(ValueError, "cover"):
            build_model_device_benchmark_report(
                ModelDeviceBenchmarkConfig(hidden_dims=(64,), devices=("cpu", "mps")),
                {},
                (_measured_row(hidden_dim=64, device="cpu"),),
            )

    def test_parser_registers_policy_device_benchmark(self) -> None:
        args = build_parser().parse_args(
            [
                "benchmark-policy-device",
                "--hidden-dims",
                "64,128",
                "--devices",
                "cpu",
                "--measurement-iterations",
                "2",
            ]
        )

        self.assertEqual(args.hidden_dims, "64,128")
        self.assertEqual(args.devices, "cpu")
        self.assertEqual(args.measurement_iterations, 2)

    @unittest.skipUnless(TORCH_AVAILABLE, "PyTorch is not available")
    def test_runs_a_small_cpu_measurement(self) -> None:
        report = benchmark_multi_action_policy_devices(
            ModelDeviceBenchmarkConfig(
                hidden_dims=(8,),
                devices=("cpu",),
                batch_size=2,
                warmup_iterations=1,
                measurement_iterations=2,
            )
        )

        self.assertEqual(report["rows"][0]["status"], "ok")
        self.assertGreater(report["rows"][0]["latency_ms"]["median"], 0.0)


def _measured_row(*, hidden_dim: int, device: str) -> dict[str, object]:
    return {
        "hidden_dim": hidden_dim,
        "device": device,
        "status": "ok",
        "parameter_count": 10,
        "parameter_bytes": 40,
        "latency_ms": {"mean": 1.1, "median": 1.0, "p95": 1.2},
        "throughput_examples_per_second": 8_000.0,
    }


if __name__ == "__main__":
    unittest.main()
