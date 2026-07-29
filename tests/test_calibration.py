from __future__ import annotations

import unittest

from kenjaku.training.calibration import (
    CalibrationExample,
    apply_temperature,
    evaluate_decision_family_calibration,
    fit_decision_family_temperatures,
)


class CalibrationTests(unittest.TestCase):
    def test_fits_each_decision_family_and_reports_metrics(self) -> None:
        examples = (
            CalibrationExample("discard", (0.9, 0.1), 1),
            CalibrationExample("discard", (0.8, 0.2), 1),
            CalibrationExample("call_pass", (0.6, 0.4), 0),
        )

        fitted = fit_decision_family_temperatures(examples, temperatures=(0.5, 1.0, 2.0))
        report = evaluate_decision_family_calibration(examples, temperatures=fitted, bins=2)

        self.assertEqual(fitted, {"discard": 2.0, "call_pass": 0.5})
        self.assertEqual(set(report), {"discard", "call_pass"})
        self.assertEqual(report["discard"]["examples"], 2)
        self.assertGreaterEqual(float(report["discard"]["ece"]), 0.0)
        self.assertLessEqual(float(report["call_pass"]["accuracy"]), 1.0)

    def test_temperature_preserves_distribution_and_validates_inputs(self) -> None:
        self.assertEqual(apply_temperature((0.5, 0.5), temperature=2.0), (0.5, 0.5))
        with self.assertRaisesRegex(ValueError, "sum to one"):
            apply_temperature((0.4, 0.4), temperature=1.0)
        with self.assertRaisesRegex(ValueError, "target outside"):
            CalibrationExample("discard", (0.5, 0.5), 2)


if __name__ == "__main__":
    unittest.main()
