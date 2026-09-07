from __future__ import annotations

import unittest

from jomon.actions import _threat_action, choose_courier, choose_gear, choose_support, choose_weapon, depart, emit_sound
from jomon.enemy_ai import next_path_step, perceive, select_goal
from jomon.inventory import auto_place, create_item, record_acquisition
from jomon.state import Position, Threat, create_world
from jomon.world import JOMON_GANGPLANK


def active_region(seed: str = "bounded enemy decisions"):
    state = create_world(seed)
    choose_courier(state, state.household[0].id)
    choose_weapon(state, "spear")
    choose_gear(state, "rope")
    choose_support(state, "route survey")
    state.position = JOMON_GANGPLANK
    depart(state)
    state.position = Position(40, 25)
    for y in range(5, 46):
        for x in range(20, 76):
            state.region.tile_changes[f"{x},{y},0"] = "."
    return state


class GoalSelectionTests(unittest.TestCase):
    def test_perception_records_then_investigates_only_last_known_position(self):
        state = active_region("last known")
        threat = Threat("scout", "route scout", "pursuer", Position(36, 25), 5, 5, status="engaged")
        state.threats = [threat]
        visible, target, _ = perceive(state, threat)
        self.assertTrue(visible)
        self.assertEqual(target, state.position)
        old = state.position
        state.position = Position(48, 32)
        state.region.tile_changes["42,29,0"] = "#"
        decision = select_goal(state, threat)
        self.assertEqual(decision.goal, "investigate")
        self.assertEqual(decision.target, old)
        self.assertNotEqual(decision.target, state.position)

    def test_sound_origin_does_not_reveal_current_courier_position(self):
        state = active_region("sound facts")
        state.position = Position(70, 40)
        threat = Threat("listener", "reed listener", "pursuer", Position(40, 25), 5, 5, status="engaged", hearing=8)
        state.threats = [threat]
        origin = Position(43, 25)
        emit_sound(state, 3, origin)
        decision = select_goal(state, threat)
        self.assertEqual(decision.action, "investigate")
        self.assertEqual(decision.target, origin)
        self.assertNotEqual(decision.target, state.position)

    def test_injury_and_morale_change_goal_to_retreat(self):
        state = active_region("morale retreat")
        threat = Threat("hurt", "hurt guard", "reach", Position(42, 25), 2, 8, status="engaged", morale=1)
        state.threats = [threat]
        decision = select_goal(state, threat)
        self.assertEqual((decision.goal, decision.action), ("break contact", "retreat"))


class NavigationAndGroupTests(unittest.TestCase):
    def test_obstacle_aware_step_routes_around_wall(self):
        state = active_region("obstacle route")
        threat = Threat("walker", "route walker", "pursuer", Position(30, 20), 5, 5, status="engaged")
        state.threats = [threat]
        target = Position(35, 20)
        for y in range(18, 22):
            state.region.tile_changes[f"32,{y},0"] = "#"
        state.region.tile_changes["32,22,0"] = "."
        positions = []
        for _ in range(10):
            step = next_path_step(state, threat, target)
            if step == threat.position:
                break
            threat.position = step
            positions.append(step)
        self.assertTrue(any(point.y >= 22 for point in positions))
        self.assertLessEqual(max(abs(threat.position.x - target.x), abs(threat.position.y - target.y)), 1)

    def test_repeated_blocked_pursuit_is_suppressed(self):
        state = active_region("blocked prose")
        threat = Threat("blocked", "blocked runner", "pursuer", Position(40, 25), 5, 5, status="engaged", last_known_position=Position(45, 25))
        state.position, state.threats = Position(45, 25), [threat]
        for point in (Position(39, 25), Position(41, 25), Position(40, 24), Position(40, 26)):
            state.region.tile_changes[f"{point.x},{point.y},0"] = "#"
        first = _threat_action(state, threat, False)
        second = _threat_action(state, threat, False)
        self.assertTrue(first)
        self.assertEqual(second, "")

    def test_lookout_alerts_only_its_group(self):
        state = active_region("group alert")
        lookout = Threat("look", "road lookout", "pursuer", Position(42, 25), 4, 4, status="engaged", role="lookout", group="red")
        ally = Threat("ally", "road ally", "reach", Position(50, 25), 4, 4, status="watching", group="red")
        stranger = Threat("other", "unrelated guard", "reach", Position(50, 30), 4, 4, status="watching", group="blue")
        state.threats = [lookout, ally, stranger]
        message = _threat_action(state, lookout, False)
        self.assertIn("alarm", message)
        self.assertEqual(ally.status, "engaged")
        self.assertEqual(ally.last_known_position, state.position)
        self.assertEqual(stranger.status, "watching")

    def test_thief_takes_physical_treasure_then_changes_to_escape(self):
        state = active_region("cargo theft")
        treasure = create_item(state, "passive:witness token", "test coffer", owner_id=state.active_courier_id)
        self.assertTrue(auto_place(state, treasure.id, "pack", owner_id=state.active_courier_id))
        record_acquisition(state, treasure)
        thief = Threat(
            "thief", "cargo runner", "pursuer", Position(41, 25), 5, 5,
            status="engaged", role="thief", home_position=Position(30, 25),
            capabilities=["steal", "escape"],
        )
        state.threats = [thief]
        self.assertIn("takes", _threat_action(state, thief, False))
        self.assertEqual(treasure.location, "enemy")
        self.assertEqual(thief.carrying_item_id, treasure.id)
        self.assertEqual(select_goal(state, thief).action, "escape")


if __name__ == "__main__":
    unittest.main()
