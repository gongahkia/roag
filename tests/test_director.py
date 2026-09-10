import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.director import PROFILES, director_profile, validate_profiles
from dumbest_dungeon.engine import GameEngine
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

    def test_encounter_health_damage_and_coordination_use_frozen_entry_band(self) -> None:
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 201)
        engine.state.pressure = 760
        engine.start_combat("lost_shift", enemy_ids=["control_rod"])
        enemy = engine.living_enemies()[0]
        base_hp = catalog.enemies["control_rod"]["max_hp"]
        self.assertEqual((base_hp * 10_500 + 5_000) // 10_000, enemy.max_hp)
        self.assertEqual(103, engine._outgoing_damage(enemy, 100))
        action = next(action for action in catalog.enemies["control_rod"]["actions"]
                      if action["name"] == "Containment Blow")
        engine.living_heroes()[0].statuses["marked"] = 1
        lockdown_weight = engine._enemy_action_weight(enemy, action)

        engine.state.pressure = 1_100
        self.assertEqual("lockdown", engine.current_director().band)
        self.assertEqual(103, engine._outgoing_damage(enemy, 100))
        self.assertEqual(lockdown_weight, engine._enemy_action_weight(enemy, action))

    def test_higher_pressure_strengthens_patrol_hazard_and_reward_jobs_separately(self) -> None:
        catalog = load_catalog()
        quiet = GameEngine.new(catalog, 202)
        hunted = GameEngine.new(catalog, 202)
        hunted.state.pressure = 480
        quiet.start_combat("lost_shift")
        hunted.start_combat("lost_shift")
        for enemy in quiet.state.enemies + hunted.state.enemies:
            enemy.hp = 0
        quiet._combat_victory()
        hunted._combat_victory()
        self.assertEqual(3, len(quiet.state.rewards))
        self.assertEqual(4, len(hunted.state.rewards))

        hazard_engine = GameEngine.new(catalog, 203)
        hazard = next(item for item in hazard_engine.state.hazards if item.biome_id == "cryogenic")
        hazard_engine.state.party_x, hazard_engine.state.party_y = hazard.cells[0]
        hazard_engine.state.pressure = 760
        hazard_engine._trigger_biome_hazard(hazard)
        self.assertTrue(all(hero.statuses.get("weak") == 3 for hero in hazard_engine.living_heroes()))

        self.assertGreater(director_profile(760).patrol_aggression,
                           director_profile(0).patrol_aggression)
        self.assertGreater(director_profile(760).patrol_cadence_reduction,
                           director_profile(0).patrol_cadence_reduction)


if __name__ == "__main__":
    unittest.main()
