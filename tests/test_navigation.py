from __future__ import annotations

import copy
import unittest

from jomon.actions import depart, interact, move
from jomon.navigation import (
    RouteUnavailable,
    advance_route,
    navigation_targets,
    plan_route,
)
from jomon.state import Position, create_world
from jomon.world import JOMON_GANGPLANK, field_of_view, position_key


def regional_state(seed: str):
    state = create_world(seed)
    state.position = JOMON_GANGPLANK
    depart(state)
    for threat in state.threats:
        threat.status = "defeated"
    return state


def remember_everything(state):
    state.region.seen = [
        position_key(Position(x, y, int(z)))
        for z, rows in state.region.levels.items()
        for y, row in enumerate(rows)
        for x, tile in enumerate(row)
        if tile not in {" ", "#", "~", "T"}
    ]


class NavigationTests(unittest.TestCase):
    def test_targets_only_expose_seen_or_explicitly_marked_places(self):
        state = regional_state("known local routes")
        field_of_view(state)
        seen = set(state.region.seen)
        unseen = next(
            container for container in state.region.containers
            if position_key(container.position) not in seen
        )
        self.assertNotIn(
            f"container:{unseen.id}",
            {target.id for target in navigation_targets(state)},
        )
        state.treasure_marks[state.active_region_id].append(unseen.id)
        self.assertIn(
            f"container:{unseen.id}",
            {target.id for target in navigation_targets(state)},
        )
        with self.assertRaisesRegex(RouteUnavailable, "no remembered route"):
            plan_route(state, f"container:{unseen.id}")

    def test_route_steps_are_exactly_equivalent_to_manual_actions(self):
        automatic = regional_state("route replay parity")
        remember_everything(automatic)
        manual = copy.deepcopy(automatic)
        target_id = "landmark:contact"
        plan = plan_route(automatic, target_id)
        index = 0
        while index < len(plan.path):
            result = advance_route(automatic, plan, index)
            self.assertTrue(result.time_advanced)
            self.assertFalse(result.stop_reason and not result.finished)
            index = result.next_index
        previous = manual.position
        for point in plan.path:
            if point.z != previous.z:
                interact(manual)
            else:
                move(manual, point.x - previous.x, point.y - previous.y)
            previous = point
        self.assertEqual(automatic.to_dict(), manual.to_dict())

    def test_route_stops_after_newly_perceived_danger(self):
        state = regional_state("route danger stop")
        remember_everything(state)
        plan = plan_route(state, "landmark:objective")
        self.assertGreater(len(plan.path), 10)
        actor = state.threats[0]
        actor.status = "watching"
        actor.ecology = "raider"
        actor.vision = 2
        actor.hearing = 1
        actor.position = plan.path[9]
        # Replan around the occupied cell while it is still outside perception.
        plan = plan_route(state, "landmark:objective")
        index = 0
        reason = ""
        for _ in range(20):
            result = advance_route(state, plan, index)
            index = result.next_index
            reason = result.stop_reason
            if reason:
                break
        self.assertIn("danger", reason)
        self.assertLess(index, len(plan.path))

    def test_deterministic_route_prefers_clear_ground_over_material_hazard(self):
        state = regional_state("route terrain cost")
        remember_everything(state)
        first = plan_route(state, "landmark:contact")
        again = plan_route(copy.deepcopy(state), "landmark:contact")
        self.assertEqual(first, again)
        self.assertNotIn("O", [
            state.region.levels[str(point.z)][point.y][point.x]
            for point in first.path
        ])


if __name__ == "__main__":
    unittest.main()
