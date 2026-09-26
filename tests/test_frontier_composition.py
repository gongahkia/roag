import unittest
from collections import Counter

from jomon.actions import _threat_action
from jomon.content import ENEMY_ARCHETYPES
from jomon.encounters import frontier_population, production_encounter_groups
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.regions import region_reachable
from jomon.state import Position, Threat, create_world


class FrontierCompositionTests(unittest.TestCase):
    def test_worksites_do_not_duplicate_the_separate_wildlife_encounter(self):
        for region_id in FRONTIERS:
            for seed in range(100):
                with self.subTest(region=region_id, seed=seed):
                    plans = production_encounter_groups(str(seed), region_id)
                    self.assertEqual(plans, production_encounter_groups(str(seed), region_id))
                    actors = [key for plan in plans for key in plan.archetypes]
                    self.assertEqual(len(actors), 6)
                    self.assertTrue(all(ENEMY_ARCHETYPES[key]["profile"] != "animal" for key in actors))
                    self.assertGreaterEqual(len(set(actors)), 3)
                    self.assertLessEqual(max(Counter(actors).values()), 2)
                    for plan in plans:
                        self.assertLessEqual(plan.spent, plan.budget)
                        self.assertEqual(len(set(plan.archetypes)), len(plan.archetypes))
                        loyalties = {ENEMY_ARCHETYPES[key].get("ecology") for key in plan.archetypes}
                        self.assertFalse({"warden", "raider"} <= loyalties)
                        self.assertLessEqual(sum(ENEMY_ARCHETYPES[key]["profile"] == "ranged" for key in plan.archetypes), 2)
                    ranged_pool = [key for key, data in ENEMY_ARCHETYPES.items() if data["region"] == region_id and data["profile"] == "ranged" and not data.get("elite")]
                    if ranged_pool:
                        self.assertTrue(set(ranged_pool) & set(actors))

    def test_actual_placements_keep_two_animals_quiet_start_and_reachable_workers(self):
        for region_id in FRONTIERS:
            region = build_frontier("winter work clothing", region_id)
            actors = frontier_population("winter work clothing", region)
            self.assertEqual(len(actors), 8)
            self.assertEqual(sum(actor.profile == "animal" for actor in actors), 2)
            self.assertEqual(len({actor.position for actor in actors}), len(actors))
            reachable = region_reachable(region)
            for actor in actors:
                self.assertIn(actor.position, reachable)
                self.assertEqual(actor.status, "watching")
                self.assertIsNone(actor.aimed_at)
                start = region.landmarks["landing"]
                self.assertGreater(abs(actor.position.x - start.x) + abs(actor.position.y - start.y), actor.vision)

    def test_two_step_pursuit_stops_beside_not_on_the_courier(self):
        state = create_world("stop outside the body")
        state.location, state.position = "region", Position(30, 20)
        state.threats.clear()
        state.noise = 20
        state.weather = "clear"
        for x in range(28, 36):
            for y in range(18, 23):
                state.region.tile_changes[f"{x},{y},0"] = "."
        actor = Threat("pursuer", "close pursuer", "pursuer", Position(32, 20), 5, 5, status="engaged")
        state.threats.append(actor)
        _threat_action(state, actor, False)
        self.assertEqual(actor.position, Position(31, 20))
        self.assertNotEqual(actor.position, state.position)


if __name__ == "__main__":
    unittest.main()
