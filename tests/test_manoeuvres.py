import copy
from pathlib import Path
import tempfile
import unittest

from jomon.actions import depart
from jomon.inventory import auto_place, create_item
from jomon.manoeuvres import (
    BY_ID, MANOEUVRES, choices, known, lines, perform, status,
    validate_manoeuvres,
)
from jomon.materials import ensure_cell
from jomon.save import load_game, save_game
from jomon.state import Position, create_world
from jomon.terminal import TargetView, _handle_targeting, targeting_lines


class ActiveMasteryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("active-mastery-tests")
        cls.base.weapon, cls.base.gear = "billhook", "repair tools"
        depart(cls.base)

    def ready(self, manoeuvre_id):
        state = copy.deepcopy(self.base)
        row = BY_ID[manoeuvre_id]
        state.courier.learned_techniques = [row.practice]
        target = state.combatants[0]
        target.status = "engaged"
        target.position = Position(state.position.x + 1, state.position.y, state.position.z)
        return state, target

    def test_catalogue_maps_twelve_practices_to_three_contexts(self):
        validate_manoeuvres()
        self.assertEqual(len(MANOEUVRES), 12)
        self.assertEqual({row.mode for row in MANOEUVRES}, {"target", "guard", "field"})

    def test_unlearned_and_failed_setup_are_zero_cost(self):
        state = copy.deepcopy(self.base)
        before = state.to_dict()
        changed, _, steps = perform(state, "shield-bind")
        self.assertFalse(changed)
        self.assertEqual(steps, 0)
        self.assertEqual(state.to_dict(), before)

    def test_targeting_cycles_and_commits_mastery(self):
        state, target = self.ready("hook-and-pass")
        state.weapon = "billhook"
        view = TargetView(target.position, [target.id])
        closed, advanced = _handle_targeting(state, view, ord("m"))
        self.assertFalse(closed)
        self.assertFalse(advanced)
        self.assertEqual(view.mastery_id, "hook-and-pass")
        self.assertIn("MASTERY", " ".join(targeting_lines(state, view, 80)))
        before = state.world_time
        closed, advanced = _handle_targeting(state, view, 10)
        self.assertTrue(closed)
        self.assertTrue(advanced)
        self.assertEqual(state.world_time, before + 1)

    def test_target_manoeuvres_have_production_effects(self):
        for manoeuvre_id in ("braced-advance", "hook-and-pass", "smoke-takedown", "porter-shove"):
            with self.subTest(manoeuvre=manoeuvre_id):
                state, target = self.ready(manoeuvre_id)
                if manoeuvre_id == "braced-advance":
                    state.weapon = "spear"
                elif manoeuvre_id == "hook-and-pass":
                    state.weapon = "billhook"
                elif manoeuvre_id == "smoke-takedown":
                    ensure_cell(state, state.position).smoke = 3
                else:
                    burden = create_item(state, "commodity:grain", "test burden", owner_id=state.active_courier_id, quantity=12)
                    auto_place(state, burden.id, "pack", owner_id=state.active_courier_id)
                before = (target.position, target.health, target.morale, state.position)
                changed, message, steps = perform(state, manoeuvre_id, target.id)
                self.assertTrue(changed, message)
                self.assertEqual(steps, 1)
                self.assertNotEqual((target.position, target.health, target.morale, state.position), before)

    def test_guard_manoeuvres_break_intent_or_restore_structure(self):
        for manoeuvre_id in ("shield-bind", "ice-feint", "support-set", "controlled-withdrawal"):
            with self.subTest(manoeuvre=manoeuvre_id):
                state, target = self.ready(manoeuvre_id)
                target.aimed_at = state.position
                if manoeuvre_id == "shield-bind":
                    state.gear = "buckler"
                elif manoeuvre_id == "ice-feint":
                    ensure_cell(state, state.position).ice = True
                elif manoeuvre_id == "support-set":
                    ensure_cell(state, state.position).support = 1
                else:
                    state.weapon = "spear"
                changed, message, steps = perform(state, manoeuvre_id, target.id)
                self.assertTrue(changed, message)
                self.assertEqual(steps, 1)
                self.assertTrue(state.guarded_step)

    def test_field_manoeuvres_change_movement_water_and_fire(self):
        state, _ = self.ready("quiet-crossing")
        state.noise = 4
        old = state.position
        self.assertTrue(perform(state, "quiet-crossing")[0])
        self.assertNotEqual(state.position, old)
        self.assertLess(state.noise, 4)

        state, _ = self.ready("flood-turn")
        source = Position(state.position.x, state.position.y + 1, state.position.z)
        ensure_cell(state, source).water = 2
        self.assertTrue(perform(state, "flood-turn")[0])
        self.assertEqual(ensure_cell(state, source).water, 1)

        state, _ = self.ready("firebreak-cut")
        point = Position(state.position.x, state.position.y + 1, state.position.z)
        cell = ensure_cell(state, point)
        cell.material, cell.fire, cell.fuel = "timber", 2, 3
        self.assertTrue(perform(state, "firebreak-cut")[0])
        self.assertEqual((cell.fire, cell.fuel), (0, 0))

    def test_high_cast_consumes_physical_ammunition_and_round_trips_record(self):
        state, target = self.ready("high-cast")
        state.weapon = "crossbow"
        state.position = Position(48, 10, 2)
        target.position = Position(48, 10, 1)
        bolts = create_item(state, "consumable:crossbow bolts", "test bolts", owner_id=state.active_courier_id, quantity=2)
        auto_place(state, bolts.id, "pack", owner_id=state.active_courier_id)
        changed, message, _ = perform(state, "high-cast", target.id)
        self.assertTrue(changed, message)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "mastery.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertIn("manoeuvre:last:high-cast", loaded.vessel_changes)

    def test_overlay_lists_setup_counter_and_all_learned_rows(self):
        state = copy.deepcopy(self.base)
        state.courier.learned_techniques = [row.practice for row in MANOEUVRES]
        self.assertEqual(len(known(state)), 12)
        self.assertEqual(len(choices(state)), 12)
        text = " ".join(lines(state))
        self.assertIn("Counter", text)
        self.assertIn("NEEDS", text)


if __name__ == "__main__":
    unittest.main()
