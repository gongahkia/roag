import unittest

from dumbest_dungeon.pressure import ACTION_PRESSURE, BANDS, PressureSource, action_price, pressure_band, pressure_status


class PressureContractTests(unittest.TestCase):
    def test_bands_are_named_monotonic_and_forecast_the_next_threshold(self) -> None:
        self.assertEqual(sorted(band.threshold for band in BANDS), [band.threshold for band in BANDS])
        self.assertEqual(len(BANDS), len({band.id for band in BANDS}))
        for index, band in enumerate(BANDS):
            self.assertEqual(band, pressure_band(band.threshold))
            status = pressure_status(band.threshold)
            self.assertEqual(band.id, status["band"])
            self.assertEqual(0, status["progress"])
            if index + 1 < len(BANDS):
                self.assertEqual(BANDS[index + 1].threshold, status["next_threshold"])
                self.assertEqual(BANDS[index + 1].threshold - band.threshold, status["remaining"])
                self.assertEqual(BANDS[index + 1].forecast, status["forecast"])

    def test_only_registered_simulation_actions_have_integer_prices(self) -> None:
        self.assertEqual(set(PressureSource), set(ACTION_PRESSURE))
        self.assertEqual(3, action_price(PressureSource.TRAVEL, 3))
        self.assertEqual(16, action_price(PressureSource.ENEMY_ROUND, 2))
        self.assertEqual(0, action_price(PressureSource.EVENT, 99))
        for invalid in (-1, True, 1.5):
            with self.assertRaises(ValueError):
                action_price(PressureSource.TRAVEL, invalid)
        with self.assertRaises(ValueError):
            pressure_status(-1)


if __name__ == "__main__":
    unittest.main()
