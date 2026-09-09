import unittest

from jomon.benchmark import distribution


class MeasurementTests(unittest.TestCase):
    def test_percentiles_use_nearest_rank_and_do_not_hide_outliers(self):
        result = distribution(list(range(1, 101)))
        self.assertEqual(result["median_ms"], 50.5)
        self.assertEqual(result["p95_ms"], 95)
        self.assertEqual(result["p99_ms"], 99)
        self.assertEqual(result["worst_ms"], 100)

    def test_empty_measurements_are_not_reported_as_fast(self):
        with self.assertRaises(ValueError):
            distribution([])
