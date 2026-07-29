from __future__ import annotations

import unittest

from kenjaku.commands._legacy import build_parser
from kenjaku.training.guardrails import (
    OptimizerResourceLimits,
    TrainingDeadline,
    estimate_adamw_state_bytes,
    mib_to_bytes,
)


class TrainingGuardrailTests(unittest.TestCase):
    def test_estimates_and_limits_adamw_state_before_optimizer_creation(self) -> None:
        parameters = (_Parameter(3, 4), _Parameter(5, 2))

        self.assertEqual(estimate_adamw_state_bytes(parameters), 44)
        self.assertEqual(
            OptimizerResourceLimits(max_optimizer_state_bytes=44, timeout_seconds=None)
            .enforce_adamw_state_budget(parameters),
            44,
        )
        with self.assertRaisesRegex(ValueError, "exceeds local limit"):
            limits = OptimizerResourceLimits(max_optimizer_state_bytes=43, timeout_seconds=None)
            limits.enforce_adamw_state_budget(parameters)

    def test_deadline_and_cli_limits_validate(self) -> None:
        clock = _Clock((10.0, 11.1))
        deadline = TrainingDeadline(timeout_seconds=1.0, clock=clock)

        with self.assertRaisesRegex(TimeoutError, "optimizer step"):
            deadline.check("optimizer step")
        self.assertEqual(mib_to_bytes(2), 2 * 1024 * 1024)
        with self.assertRaisesRegex(ValueError, "positive"):
            mib_to_bytes(0)
        with self.assertRaisesRegex(ValueError, "finite positive"):
            OptimizerResourceLimits(timeout_seconds=0.0)

    def test_training_parsers_expose_local_limits(self) -> None:
        parser = build_parser()
        mlp = parser.parse_args(
            [
                "train-discard-mlp",
                "fixture.xml",
                "--max-optimizer-state-mib",
                "64",
                "--timeout-seconds",
                "2",
            ]
        )
        ppo = parser.parse_args(["train-ppo-sandbox", "--timeout-seconds", "3"])

        self.assertEqual(mlp.max_optimizer_state_mib, 64)
        self.assertEqual(mlp.timeout_seconds, 2.0)
        self.assertEqual(ppo.timeout_seconds, 3.0)


class _Parameter:
    def __init__(self, numel: int, element_size: int) -> None:
        self._numel = numel
        self._element_size = element_size

    def numel(self) -> int:
        return self._numel

    def element_size(self) -> int:
        return self._element_size


class _Clock:
    def __init__(self, values: tuple[float, ...]) -> None:
        self._values = iter(values)

    def __call__(self) -> float:
        return next(self._values)


if __name__ == "__main__":
    unittest.main()
