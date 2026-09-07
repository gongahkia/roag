from __future__ import annotations

import unittest

from jomon.actions import _advance_world, _threat_action, attack, choose_courier, choose_gear, choose_support, choose_weapon, depart, guard
from jomon.content import WEAPONS
from jomon.enemy_ai import select_goal
from jomon.state import Position, Threat, create_world
from jomon.world import JOMON_GANGPLANK, cover_at, line_of_sight, projectile_path


def armed(weapon: str):
    state = create_world(f"ranged {weapon}")
    choose_courier(state, state.household[0].id)
    if weapon not in state.owned_weapons:
        state.owned_weapons.append(weapon)
    choose_weapon(state, weapon)
    choose_gear(state, "rope")
    choose_support(state, "route survey")
    state.position = JOMON_GANGPLANK
    depart(state)
    state.position = Position(40, 25)
    for y in range(15, 36):
        for x in range(25, 61):
            state.region.tile_changes[f"{x},{y},0"] = "."
    return state


def target_at(state, x: int, *, profile: str = "pursuer"):
    threat = Threat("target", "practice obstruction", profile, Position(x, 25), 12, 12, status="engaged", morale=9)
    state.threats = [threat]
    return threat


class PlayerRangeTests(unittest.TestCase):
    def test_arsenal_has_fourteen_physical_families(self):
        self.assertGreaterEqual(len(WEAPONS), 12)
        from jomon.inventory import ITEM_SPECS

        self.assertTrue(set(WEAPONS) <= set(ITEM_SPECS))

    def test_longbow_aims_then_spends_one_arrow(self):
        state = armed("longbow")
        target = target_at(state, 50)
        arrows = state.ammunition_by_type["arrows"]
        first = attack(state)
        self.assertIn("prepare longbow", first.message)
        self.assertEqual(target.health, target.max_health)
        attack(state)
        self.assertLess(target.health, target.max_health)
        self.assertEqual(state.ammunition_by_type["arrows"], arrows - 1)

    def test_sling_is_immediate_and_height_can_press_morale(self):
        state = armed("sling")
        target = target_at(state, 47)
        attack(state)
        self.assertEqual(target.health, 11)
        self.assertEqual(state.ammunition_by_type["sling stones"], 9)

    def test_heavy_crossbow_requires_two_reload_actions(self):
        state = armed("heavy crossbow")
        target = target_at(state, 52)
        attack(state)
        attack(state)
        self.assertEqual(state.weapon_ready, 0)
        self.assertLess(target.health, 12)
        guard(state)
        self.assertEqual(state.weapon_ready, 1)
        guard(state)
        self.assertEqual(state.weapon_ready, 2)

    def test_pike_rejects_adjacent_target_but_controls_reach(self):
        state = armed("pike")
        adjacent = target_at(state, 41)
        self.assertFalse(attack(state).time_advanced)
        adjacent.position = Position(43, 25)
        before = adjacent.position
        self.assertTrue(attack(state).time_advanced)
        self.assertNotEqual(adjacent.position, before)

    def test_weighted_net_changes_movement_instead_of_dealing_damage(self):
        state = armed("weighted net")
        target = target_at(state, 44)
        attack(state)
        self.assertEqual(target.health, target.max_health)
        self.assertIn("cuts free", target.intent)
        self.assertTrue(any("loses a turn" in message for message in state.messages))


class RangedFairnessTests(unittest.TestCase):
    def test_new_awareness_never_deals_immediate_ranged_damage(self):
        state = armed("spear")
        target = target_at(state, 46, profile="ranged")
        target.status, target.role, target.ammunition = "watching", "shooter", 3
        health = state.courier.health
        _advance_world(state)
        self.assertEqual(state.courier.health, health)
        self.assertEqual(target.status, "engaged")
        _advance_world(state)
        self.assertEqual(state.courier.health, health)
        self.assertIsNotNone(target.aimed_at)

    def test_moving_out_of_telegraphed_lane_causes_miss(self):
        state = armed("spear")
        target = target_at(state, 48, profile="ranged")
        target.role, target.ammunition, target.ranged_kind = "shooter", 3, "longbow"
        first = _threat_action(state, target, False)
        self.assertIn("lane", first)
        state.position = Position(40, 26)
        health = state.courier.health
        second = _threat_action(state, target, False)
        self.assertIn("lane empty", second)
        self.assertEqual(state.courier.health, health)

    def test_cover_projectile_path_and_elevation_are_explicit(self):
        state = armed("spear")
        shooter, target = Position(35, 25), Position(40, 25)
        self.assertGreater(len(projectile_path(shooter, target)), 2)
        self.assertEqual(cover_at(state, shooter, target), "open")
        state.region.tile_changes["40,24,0"] = "#"
        self.assertEqual(cover_at(state, shooter, target), "partial")
        upper = Position(39, 25, 1)
        state.region.tile_changes["39,25,1"] = "^"
        self.assertTrue(line_of_sight(state, upper, target))

    def test_ranged_actor_with_no_ammunition_breaks_contact(self):
        state = armed("spear")
        target = target_at(state, 47, profile="ranged")
        target.role, target.ammunition = "shooter", 0
        self.assertEqual(select_goal(state, target).action, "retreat")

    def test_protector_prioritises_a_ranged_ally(self):
        state = armed("spear")
        protector = Threat("guard", "shield carrier", "reach", Position(42, 25), 6, 6, status="engaged", role="protector", group="pair")
        shooter = Threat("bow", "bow carrier", "ranged", Position(46, 25), 4, 4, status="engaged", role="shooter", group="pair", ammunition=3)
        state.threats = [protector, shooter]
        self.assertEqual(select_goal(state, protector).goal, "protect ally")


if __name__ == "__main__":
    unittest.main()
