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

    def test_territorial_actor_returns_at_its_boundary(self):
        state = active_region("territorial boundary")
        threat = Threat(
            "wallow", "territorial tusker", "animal", Position(42, 25),
            5, 5, status="engaged", role="territorial", vision=20,
            home_position=Position(40, 25), capabilities=["stops beyond its wallow"],
        )
        state.position, state.threats = Position(48, 25), [threat]
        decision = select_goal(state, threat)
        self.assertEqual((decision.goal, decision.action), ("defend territory", "return"))

    def test_ranged_actor_selects_and_physically_uses_elevation(self):
        state = active_region("seek actual elevation")
        link = next(link for link in state.region.vertical_links if link.first.z < link.second.z)
        lower, upper = link.first, link.second
        threat = Threat(
            "height", "dune hunter", "ranged", lower, 5, 5,
            status="engaged", role="shooter", goal="hold distance", vision=20,
            ammunition=3, capabilities=["seeks dune height before aiming"],
        )
        state.position = Position(lower.x + 5, lower.y, lower.z)
        state.region.tile_changes[f"{state.position.x},{state.position.y},{state.position.z}"] = "."
        state.threats = [threat]
        decision = select_goal(state, threat)
        self.assertEqual(decision.action, "seek elevation")
        _threat_action(state, threat, False)
        self.assertEqual(threat.position, upper)

    def test_smoke_tender_materially_closes_a_lane(self):
        state = active_region("feed bounded smoke")
        threat = Threat(
            "smoke", "burn smoke-tender", "ranged", Position(45, 25),
            5, 5, status="engaged", role="suppressor", vision=20,
            ammunition=3, capabilities=["feeds smoke into a watched clearing"],
        )
        state.threats = [threat]
        self.assertEqual(select_goal(state, threat).action, "feed smoke")
        self.assertIn("bounded smoke lane", _threat_action(state, threat, False))
        self.assertTrue(state.smoke)


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

    def test_lost_contact_returns_home_instead_of_tracking_hidden_courier(self):
        state = active_region("no omniscient fallback")
        threat = Threat(
            "guard", "route guard", "reach", Position(38, 25), 5, 5,
            status="engaged", home_position=Position(30, 25),
        )
        state.threats = [threat]
        state.position = Position(70, 40)
        message = _threat_action(state, threat, False)
        self.assertLess(threat.position.x, 38)
        self.assertIn("guarded position", message)

    def test_protector_physically_intercepts_for_ranged_ally(self):
        state = active_region("material protector")
        state.position = Position(42, 25)
        protector = Threat(
            "guard", "shield carrier", "reach", Position(46, 25), 6, 6,
            status="engaged", role="protector", group="pair",
        )
        shooter = Threat(
            "bow", "bow carrier", "ranged", Position(50, 25), 4, 4,
            status="engaged", role="shooter", group="pair", ammunition=3,
        )
        state.threats = [protector, shooter]
        message = _threat_action(state, protector, False)
        self.assertEqual(protector.position, Position(47, 25))
        self.assertIn("ranged ally", message)

    def test_protector_covers_a_wounded_ally_before_attacking(self):
        state = active_region("cover wounded ally")
        state.position = Position(42, 25)
        protector = Threat(
            "guard", "shield carrier", "reach", Position(46, 25), 6, 6,
            status="engaged", role="protector", group="pair",
        )
        wounded = Threat(
            "hurt", "hurt runner", "pursuer", Position(48, 25), 1, 5,
            status="engaged", role="flanker", group="pair", morale=1,
        )
        state.threats = [protector, wounded]
        self.assertEqual(select_goal(state, protector).action, "cover retreat")
        message = _threat_action(state, protector, False)
        self.assertIn("wounded ally", message)
        self.assertEqual(wounded.goal, "break contact")

    def test_flanker_uses_visible_side_target(self):
        state = active_region("bounded flank")
        state.position = Position(42, 25)
        flanker = Threat(
            "side", "coppice side runner", "pursuer", Position(47, 25), 5, 5,
            status="engaged", role="flanker",
        )
        state.threats = [flanker]
        decision = select_goal(state, flanker)
        self.assertEqual(decision.action, "flank")
        self.assertNotEqual(decision.target, state.position)
        self.assertIn("side approach", _threat_action(state, flanker, False))

    def test_controller_telegraphs_a_cell_and_misses_reposition(self):
        state = active_region("bounded controller")
        state.position = Position(42, 25)
        controller = Threat(
            "net", "mudflat netter", "reach", Position(45, 25), 5, 5,
            status="engaged", role="controller",
        )
        state.threats = [controller]
        first = _threat_action(state, controller, False)
        self.assertIn("leave the marked cell", first)
        state.position = Position(42, 26)
        second = _threat_action(state, controller, False)
        self.assertIn("empty ground", second)
        self.assertNotIn("net-drag", state.terrain_statuses)


if __name__ == "__main__":
    unittest.main()
