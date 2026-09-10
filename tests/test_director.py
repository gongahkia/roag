import unittest

from dumbest_dungeon.director import PROFILES, director_profile, validate_profiles
from dumbest_dungeon.pressure import BANDS


class DirectorContractTests(unittest.TestCase):
    def test_every_visible_band_has_one_bounded_monotonic_profile(self) -> None:
        validate_profiles()
        self.assertEqual([band.id for band in BANDS], list(PROFILES))
        profiles = list(PROFILES.values())
        for earlier, later in zip(profiles, profiles[1:]):
            self.assertLessEqual(earlier.coordination, later.coordination)
            self.assertLessEqual(earlier.mutation_slots, later.mutation_slots)
            self.assertLessEqual(earlier.reward_choices, later.reward_choices)
        self.assertLessEqual(max(profile.enemy_health_bp for profile in profiles), 11_000)
        self.assertLessEqual(max(profile.enemy_damage_bp for profile in profiles), 10_500)

    def test_profile_selection_uses_only_pressure_band_boundaries(self) -> None:
        for band in BANDS:
            self.assertEqual(band.id, director_profile(band.threshold).band)
        self.assertEqual("quiet", director_profile(BANDS[1].threshold - 1).band)


if __name__ == "__main__":
    unittest.main()
