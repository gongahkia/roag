import copy
import curses
import unittest

from jomon.actions import attack
from jomon.content import ENEMY_ARCHETYPES
from jomon.inventory import auto_place, create_item
from jomon.state import Position, Threat, create_world
from jomon.terminal import (
    InputEvent, TargetView, _draw_targeting, _handle_targeting,
    _status_lines, observed_life_lines, targeting_lines, visible_danger_marks,
)
from jomon.world import courier_sees, field_of_view
from test_information_panels import PanelSink


class TargetVisibilityTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("visible projectile targets")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        state = self.state
        state.location, state.position, state.world_time = "region", Position(40, 25), 8
        state.weather, state.weapon = "clear", "sling"
        state.threats = []
        for z in (-1, 0, 1):
            for y in range(20, 31):
                for x in range(30, 55):
                    state.region.tile_changes[f"{x},{y},{z}"] = "."

    def target(self, point, name="known bow carrier"):
        actor = Threat(f"target-{len(self.state.threats)}", name, "ranged", point, 20, 20, status="engaged", morale=8)
        self.state.threats.append(actor)
        return actor

    def test_tab_and_level_keys_reach_an_open_vertical_lane_but_not_a_floor(self):
        state = self.state
        upper = self.target(Position(41, 25, 1))
        lower = self.target(Position(42, 25))
        self.assertNotIn(upper.id, TargetView.begin(state).target_ids)
        state.region.tile_changes["41,25,1"] = "O"
        self.assertTrue(courier_sees(state, upper.position))
        view = TargetView.begin(state)
        before = state.to_dict()
        _handle_targeting(state, view, 9)
        self.assertEqual(view.cursor, upper.position)
        self.assertIn("[ABOVE]", " ".join(targeting_lines(state, view, 78)))
        _handle_targeting(state, view, curses.KEY_RIGHT)
        self.assertEqual(view.cursor.z, 1)
        _handle_targeting(state, view, ord("<"))
        self.assertEqual(view.cursor, lower.position)
        _handle_targeting(state, view, 27)
        self.assertEqual(state.to_dict(), before)

    def test_fog_and_wall_hide_the_actor_name_and_block_direct_attack(self):
        state = self.state
        state.weapon, state.weather = "heavy crossbow", "river fog"
        actor = self.target(Position(50, 25), "hidden private claimant")
        view = TargetView(actor.position, [actor.id])
        self.assertEqual(TargetView.begin(state).target_ids, [])
        before = state.world_time
        self.assertNotIn(actor.name, " ".join(targeting_lines(state, view, 78)))
        self.assertFalse(attack(state, actor.id).time_advanced)
        self.assertEqual(state.world_time, before)
        state.weather = "clear"
        state.region.tile_changes["45,25,0"] = "#"
        self.assertNotIn(actor.name, " ".join(targeting_lines(state, view, 78)))

    def test_long_names_do_not_displace_ammunition_or_reload_rows_at_either_size(self):
        state = self.state
        actor = self.target(Position(42, 25))
        state.weapon, state.weapon_ready = "heavy crossbow", 1
        view = TargetView(actor.position, [actor.id])
        for height, width in ((24, 80), (32, 100)):
            for data in ENEMY_ARCHETYPES.values():
                actor.name = data["name"]
                sink = PanelSink(height, width)
                _draw_targeting(sink, state, view)
                text = " ".join(sink.writes)
                self.assertIn(actor.name, text)
                self.assertIn("heavy bolts", text)
                self.assertIn("1 reload actions", text)
                self.assertIn("Cover:", text)

    def test_observed_cross_level_aim_is_shown_without_revealing_hidden_actor(self):
        state = self.state
        upper = self.target(Position(41, 25, 1))
        upper.aimed_at = state.position
        state.region.tile_changes["41,25,1"] = "O"
        self.assertIn(upper.name, " ".join(observed_life_lines(state)))
        self.assertIn(state.position, visible_danger_marks(state, field_of_view(state)))
        state.region.tile_changes["41,25,1"] = "."
        self.assertNotIn(upper.name, " ".join(observed_life_lines(state)))
        self.assertNotIn(state.position, visible_danger_marks(state, field_of_view(state)))

    def test_mouse_and_keyboard_cursor_inspection_have_identical_hidden_information(self):
        state = self.state
        actor = self.target(Position(42, 25))
        state.region.tile_changes["41,25,0"] = "#"
        view = TargetView(state.position, [])
        before = state.to_dict()
        _handle_targeting(state, view, InputEvent("mouse", button="left", x=27, y=8))
        self.assertEqual(view.cursor, actor.position)
        self.assertNotIn(actor.name, " ".join(targeting_lines(state, view, 78)))
        self.assertEqual(_handle_targeting(state, view, 10), (False, False))
        self.assertEqual(state.world_time, before["world_time"])
        self.assertEqual(actor.health, 20)

    def test_status_uses_current_weapon_supply_not_the_old_bolt_mirror(self):
        state = self.state
        item = create_item(state, "consumable:sling shot pouch", "counted stones", quantity=3)
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        state.ammunition = 99
        self.assertIn("Ammo 3 stones; oil 6", _status_lines(state))


if __name__ == "__main__":
    unittest.main()
