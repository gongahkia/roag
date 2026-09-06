from __future__ import annotations

import json
from pathlib import Path
import tempfile
import unittest

from jomon.actions import (
    _advance_world,
    apply_damage,
    attack,
    choose_courier,
    choose_gear,
    choose_passive,
    choose_support,
    choose_weapon,
    decide_objective,
    depart,
    guard,
    interact,
    merchant_stock_for,
    merchant_visit_due,
    move,
    negotiate,
    purchase_merchant_item,
    return_to_jomon,
)
from jomon.content import COMMODITIES, GEAR, PASSIVES, SUPPORTS, WEAPONS
from jomon.save import SaveError, load_game, save_game
from jomon.state import CommodityStack, Position, SAVE_FORMAT, Threat, create_world
from jomon.world import JOMON_GANGPLANK, build_combinations, pressure


def prepared(
    seed: str = "hearthford test",
    *,
    role: str = "bargemaster",
    weapon: str = "spear",
    gear: str = "rope",
    support: str = "route survey",
):
    state = create_world(seed)
    courier = next(person for person in state.household if person.role == role)
    choose_courier(state, courier.id)
    if weapon not in state.owned_weapons:
        state.owned_weapons.append(weapon)
    if gear not in state.owned_gear:
        state.owned_gear.append(gear)
    choose_weapon(state, weapon)
    choose_gear(state, gear)
    choose_support(state, support)
    state.position = JOMON_GANGPLANK
    depart(state)
    return state


def quiet(state):
    for threat in state.threats:
        threat.status = "defeated"


class TimeAndBuildTests(unittest.TestCase):
    def test_inspection_and_blocked_movement_are_zero_time(self):
        state = create_world("no clock")
        started = state.world_time
        self.assertFalse(move(state, -99, 0).time_advanced)
        self.assertEqual(state.world_time, started)

    def test_movement_and_accepted_decision_advance_action_clock(self):
        state = prepared()
        quiet(state)
        started = state.world_time
        move(state, 1, 0)
        self.assertEqual(state.world_time, started + 1)
        state.position = state.region.landmarks["contact"]
        decide_objective(state, "accept")
        self.assertEqual(state.world_time, started + 2)

    def test_catalogues_and_six_system_combinations(self):
        self.assertEqual(len(WEAPONS), 6)
        self.assertEqual(len(GEAR), 8)
        self.assertEqual(len(SUPPORTS), 5)
        self.assertEqual(len(PASSIVES), 12)
        cases = (
            ("pilot", "staff", "quiet shoes", "route survey", {}, "surveyed soft-step"),
            ("guard", "billhook", "rope", "route survey", {}, "mobile hook"),
            ("guard", "spear", "buckler", "route survey", {}, "shielded set stance"),
            ("factor", "crossbow", "trade seals", "factor surety", {"waxed bowstring": 1}, "weatherproof aim"),
            ("carpenter", "hand axe", "repair tools", "carpenter rig", {"mill-tooth wedge": 1}, "controlled breach"),
            ("healer", "staff", "rope", "field care", {"salted dressing": 1}, "deep field binding"),
            ("pilot", "staff", "smoke pot", "route survey", {"smoke lens": 1}, "smoke walker"),
            ("pilot", "staff", "rope", "route survey", {"river hooks": 1}, "flood rig"),
        )
        for role, weapon, gear, support, passives, expected in cases:
            with self.subTest(expected=expected):
                state = prepared(
                    expected,
                    role=role,
                    weapon=weapon,
                    gear=gear,
                    support=support,
                )
                state.carried_passives = passives
                self.assertIn(expected, build_combinations(state))

    def test_tavern_passive_selection_is_limited_and_zero_time(self):
        state = create_world("passive packing")
        state.owned_passives = {
            "river hooks": 2,
            "echo bead": 1,
            "high tread": 1,
        }
        started = state.world_time
        choose_passive(state, "river hooks")
        choose_passive(state, "river hooks")
        choose_passive(state, "echo bead")
        blocked = choose_passive(state, "high tread")
        self.assertFalse(blocked.changed)
        self.assertEqual(state.carried_passives["river hooks"], 2)
        self.assertEqual(state.world_time, started)
        self.assertLessEqual(sum(PASSIVES[name][0] for name in state.carried_passives), 5)

    def test_pressure_is_legible_and_critical_spawns_threat(self):
        state = prepared()
        state.pressure_elapsed, state.noise = 300, 8
        state.carried_passives = {"witness token": 1}
        self.assertEqual(pressure(state).band, "critical")
        self.assertFalse(state.escalation_spawned)
        _advance_world(state)
        self.assertTrue(state.escalation_spawned)
        self.assertNotEqual(
            next(t for t in state.threats if t.id == "pressure-reavers").status,
            "dormant",
        )

    def test_deadline_changes_only_after_actions(self):
        state = prepared()
        quiet(state)
        state.objective_deadline = state.pressure_elapsed + 1
        demand = state.market[state.region.objective_commodity].demand
        self.assertFalse(state.objective_changed)
        move(state, 1, 0)
        self.assertTrue(state.objective_changed)
        self.assertEqual(state.market[state.region.objective_commodity].demand, demand + 1)

    def test_routine_movement_does_not_displace_danger_log(self):
        state = prepared()
        quiet(state)
        state.position = Position(40, 26, 0)
        marker = "A crossbow keeper aims and fires next turn."
        state.messages = [marker]
        for _ in range(4):
            move(state, 1, 0)
            move(state, -1, 0)
        self.assertIn(marker, state.messages)
        self.assertFalse(any(message == "You move." for message in state.messages))


class WeaponAndThreatTests(unittest.TestCase):
    def weapon_state(self, weapon: str, gap: int = 1):
        state = prepared(f"weapon {weapon}", weapon=weapon)
        quiet(state)
        state.position = Position(40, 25, 0)
        for x in range(39, 47):
            for y in range(24, 27):
                state.region.tile_changes[f"{x},{y},0"] = "."
        target = Threat(
            "target", "route opponent", "pursuer",
            Position(40 + gap, 25, 0), 12, 12, status="engaged", morale=5
        )
        state.threats = [target]
        return state, target

    def test_weapon_actions_are_fundamentally_distinct(self):
        state, target = self.weapon_state("spear", 2)
        before = target.position
        attack(state)
        self.assertNotEqual(target.position, before)

        state, target = self.weapon_state("billhook", 2)
        before = target.position
        attack(state)
        self.assertLess(target.position.x, before.x)

        state, target = self.weapon_state("cudgel")
        attack(state)
        self.assertLess(target.morale, 5)
        self.assertGreater(target.position.x, 41)

        state, target = self.weapon_state("hand axe")
        attack(state)
        self.assertEqual(target.health, 9)

        state, target = self.weapon_state("crossbow", 5)
        attack(state)
        self.assertEqual(target.health, 12)
        attack(state)
        self.assertLess(target.health, 12)
        self.assertFalse(state.crossbow_loaded)

    def test_staff_sweeps_mixed_adjacent_group_and_guards_movement(self):
        state, first = self.weapon_state("staff")
        second = Threat(
            "second", "spear carrier", "reach", Position(40, 26, 0),
            4, 4, status="engaged"
        )
        state.threats.append(second)
        attack(state)
        self.assertLess(first.health, first.max_health)
        self.assertLess(second.health, second.max_health)
        self.assertTrue(state.guarded_step)

    def test_patrol_moves_can_be_avoided_or_drawn_by_sound(self):
        state = prepared("patrol")
        patrol = next(threat for threat in state.threats if threat.id == "road-patrol")
        state.position = Position(20, 35, 0)
        before = patrol.position
        _advance_world(state)
        self.assertNotEqual(patrol.position, before)
        self.assertEqual(patrol.status, "watching")
        state.position = Position(patrol.position.x, patrol.position.y + 4, 0)
        from jomon.actions import emit_sound
        emit_sound(state, 3)
        self.assertEqual(patrol.status, "engaged")

    def test_negotiation_and_mud_are_noncombat_resolutions(self):
        state = prepared(role="factor", gear="trade seals", support="factor surety")
        quiet(state)
        human = Threat(
            "terms", "toll runner", "pursuer", Position(41, 25), 4, 4,
            status="engaged"
        )
        state.position, state.threats = Position(40, 25, 0), [human]
        self.assertTrue(negotiate(state).time_advanced)
        self.assertEqual(human.status, "negotiated")

        state = prepared("mud evade")
        quiet(state)
        animal = Threat(
            "boar", "reed boar", "animal", Position(54, 38), 5, 5,
            status="engaged", intent="lowers its head and charges next turn"
        )
        state.position, state.threats = Position(54, 39, 0), [animal]
        _advance_world(state)
        self.assertEqual(animal.status, "evaded")

    def test_elite_machinery_uses_timing_and_material_disable(self):
        state = prepared("elite", gear="repair tools")
        quiet(state)
        elite = Threat(
            "elite", "runaway crown wheel", "machinery", Position(82, 27),
            7, 7, status="engaged", elite=True, morale=99
        )
        state.position, state.threats = Position(80, 24, 0), [elite]
        health = state.courier.health
        _advance_world(state)
        self.assertIn("next turn", elite.intent)
        state.position = Position(86, 30, 0)
        state.region.tile_changes["86,30,0"] = "f"
        interact(state)
        self.assertEqual(elite.status, "disabled")
        self.assertEqual(state.courier.health, health)


class PersistenceAndDefeatTests(unittest.TestCase):
    def test_chest_depletion_and_returned_discovery_persist(self):
        state = prepared("persistent chest")
        quiet(state)
        chest = next(item for item in state.region.containers if item.requirement is None)
        chest.reward = "reed sole wraps"
        state.position = chest.position
        interact(state)
        self.assertTrue(chest.opened)
        self.assertEqual(state.carried_passives["reed sole wraps"], 1)
        state.position = state.region.landmarks["landing"]
        return_to_jomon(state)
        self.assertEqual(state.owned_passives["reed sole wraps"], 1)
        self.assertTrue(chest.opened)

    def test_contextual_injury_loss_and_permanent_succession(self):
        state = prepared("injury")
        state.carried_goods["paper"] = CommodityStack(1, "dry")
        state.carried_passives["echo bead"] = 1
        state.courier.health = 2
        result = apply_damage(state, 3, "The bolt")
        self.assertEqual(state.location, "jomon")
        self.assertEqual(state.courier.injury, "deep cut")
        self.assertFalse(state.carried_passives)
        self.assertIn("deep cut", result)

        state = prepared("death")
        dead = state.courier
        dead.health, dead.injury = 1, "bruised ribs"
        result = apply_damage(state, 3, "The flood")
        self.assertFalse(dead.alive)
        self.assertNotEqual(state.active_courier_id, dead.id)
        self.assertIn("succeeds", result)

    def test_merchant_timing_stock_and_purchase_are_deterministic(self):
        seed = "merchant river"
        due = [count for count in range(1, 7) if merchant_visit_due(seed, count)]
        self.assertEqual(len(due), 2)
        state = create_world(seed)
        state.returned_expeditions = due[0]
        state.objective_status = "completed"
        second = create_world(seed)
        second.returned_expeditions = due[0]
        second.objective_status = "completed"
        self.assertEqual(merchant_stock_for(state), merchant_stock_for(second))
        state.merchant_present = True
        state.merchant_stock = merchant_stock_for(state)
        state.trade_credit = 10
        item = state.merchant_stock[0]
        purchase_merchant_item(state, item)
        self.assertNotIn(item, state.merchant_stock)

    def test_objective_return_save_reload_and_changed_second_expedition(self):
        state = prepared("changed second", gear="repair tools", support="carpenter rig")
        quiet(state)
        state.position = state.region.landmarks["contact"]
        decide_objective(state, "alter")
        state.position = Position(82, 42, -1)
        interact(state)
        state.position = state.region.landmarks["contact"]
        interact(state)
        self.assertEqual(state.objective_status, "completed")
        changed_demand = state.market[state.region.objective_commodity].demand
        state.position = state.region.landmarks["landing"]
        return_to_jomon(state)
        with tempfile.TemporaryDirectory() as directory:
            target = Path(directory) / "jomon.json"
            save_game(state, target)
            restored = load_game(target)
        self.assertEqual(restored, state)
        restored.position = JOMON_GANGPLANK
        depart(restored)
        self.assertEqual(restored.expedition_count, 2)
        self.assertTrue(restored.region.changes["mill_stabilised"])
        self.assertEqual(
            restored.market[restored.region.objective_commodity].demand,
            changed_demand,
        )

    def test_corrupt_and_incompatible_save_rejection(self):
        with tempfile.TemporaryDirectory() as directory:
            target = Path(directory) / "jomon.json"
            target.write_text("{bad", encoding="utf-8")
            with self.assertRaisesRegex(SaveError, "corrupt JSON"):
                load_game(target)
            target.write_text(
                json.dumps({"save_format": SAVE_FORMAT - 1}), encoding="utf-8"
            )
            with self.assertRaisesRegex(SaveError, "incompatible save format"):
                load_game(target)

    def test_all_eight_goods_remain_physical(self):
        self.assertEqual(len(COMMODITIES), 8)
        for definition in COMMODITIES.values():
            self.assertGreater(definition["bulk"], 0)
            self.assertTrue(
                definition["condition"] and definition["source"] and definition["use"]
            )


if __name__ == "__main__":
    unittest.main()
