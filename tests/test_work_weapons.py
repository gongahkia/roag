import copy
import unittest

from jomon.actions import _open_container, _threat_action, attack, merchant_stock_for, move
from jomon.content import WEAPONS
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.inventory import (
    AMMUNITION_ITEMS, auto_place, create_item, equip_item, equipped_item,
    item_preview, physical_ammunition, prepare_kind, sync_legacy_load,
)
from jomon.materials import ensure_cell, handle_material, key
from jomon.state import Position, Threat, create_world, game_state_from_dict
from jomon.terminal import InputEvent, TargetView, _handle_targeting, targeting_lines
from jomon.work_weapons import POT_AMMUNITION, WORK_WEAPONS


class WorkingWeaponTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("working implement regression")

    def setUp(self):
        state = self.state = copy.deepcopy(self.base)
        state.location, state.position, state.world_time = "region", Position(40, 25), 8
        state.weather = "clear"
        state.threats = []
        state.region_threats["hearthford"] = state.threats
        for z in (-1, 0, 1):
            for y in range(20, 31):
                for x in range(34, 51):
                    state.region.tile_changes[f"{x},{y},{z}"] = "."
        for item in state.items:
            if item.owner_id == state.active_courier_id and item.location in {"pack", "readied", "secondary"}:
                item.location, item.owner_id = "lost", None
        state.gear = None

    def wield(self, kind):
        item = create_item(self.state, kind, "weapon exercise", location="readied", owner_id=self.state.active_courier_id)
        sync_legacy_load(self.state)
        return item

    def supply(self, name):
        item = create_item(self.state, AMMUNITION_ITEMS[name], "finite test load", quantity=2)
        self.assertTrue(auto_place(self.state, item.id, "pack", owner_id=self.state.active_courier_id))
        return item

    def actor(self, point):
        actor = Threat(f"work-target-{len(self.state.threats)}", "working opponent", "reach", point, 20, 20, status="engaged", morale=10, home_position=point)
        self.state.threats.append(actor)
        return actor

    def test_pot_payload_cycle_and_cancel_are_exactly_zero_time(self):
        self.wield("pot sling")
        for name in POT_AMMUNITION:
            self.supply(name)
        before = self.state.to_dict()
        view = TargetView.begin(self.state)
        self.assertEqual(view.ammunition, "pitch pots")
        for expected in ("lime pots", "brine pots", "pitch pots"):
            self.assertEqual(_handle_targeting(self.state, view, ord("P")), (False, False))
            self.assertEqual(view.ammunition, expected)
            self.assertIn(expected, " ".join(targeting_lines(self.state, view, 78)))
        self.assertEqual(_handle_targeting(self.state, view, 27), (True, False))
        self.assertEqual(before, self.state.to_dict())

    def test_three_pots_consume_the_selected_physical_stack_and_react(self):
        for name in POT_AMMUNITION:
            with self.subTest(payload=name):
                self.setUp()
                state = self.state
                self.wield("pot sling")
                self.supply(name)
                point = Position(44, 25)
                cell = ensure_cell(state, point)
                if name == "brine pots":
                    cell.material, cell.fire, cell.fuel = "timber", 2, 5
                view = TargetView(point, [], ammunition=name)
                self.assertEqual(_handle_targeting(state, view, 10), (True, True))
                self.assertEqual(state.world_time, 9)
                self.assertEqual(physical_ammunition(state, name), 1)
                if name == "pitch pots":
                    self.assertGreater(cell.fire, 0)
                    self.assertEqual(cell.coating, "oil")
                elif name == "lime pots":
                    self.assertGreater(cell.smoke, 0)
                    self.assertEqual(cell.coating, "lime")
                else:
                    self.assertEqual(cell.fire, 0)
                    self.assertGreater(cell.water, 0)
                    self.assertEqual(cell.fluid, "salt")
                loaded = game_state_from_dict(state.to_dict())
                self.assertEqual(loaded.to_dict(), state.to_dict())

    def test_pot_range_cover_and_weather_reject_without_consumption(self):
        self.wield("pot sling")
        self.supply("pitch pots")
        for point in (Position(42, 25), Position(47, 25)):
            self.assertFalse(attack(self.state, target_position=point).time_advanced)
        self.state.region.tile_changes["42,25,0"] = "#"
        self.assertFalse(attack(self.state, target_position=Position(44, 25)).time_advanced)
        self.assertEqual(physical_ammunition(self.state, "pitch pots"), 2)
        self.assertEqual(self.state.world_time, 8)

    def test_mouse_can_select_an_empty_pot_landing_and_enter_confirms(self):
        self.wield("pot sling")
        self.supply("brine pots")
        view = TargetView.begin(self.state)
        _handle_targeting(self.state, view, InputEvent("mouse", button="left", x=29, y=8))
        self.assertEqual(view.cursor, Position(44, 25))
        self.assertEqual(self.state.world_time, 8)
        self.assertEqual(_handle_targeting(self.state, view, 10), (True, True))

    def test_throwing_axe_is_the_same_recoverable_item_not_an_ammo_copy(self):
        item = self.wield("throwing axe")
        target = self.actor(Position(44, 25))
        ensure_cell(self.state, target.position).material = "timber"
        original = target.position
        before = len(self.state.items)
        result = attack(self.state, target.id)
        self.assertTrue(result.time_advanced)
        self.assertEqual((item.location, item.owner_id, item.ground_position), ("ground", None, original))
        self.assertIsNone(self.state.weapon)
        self.assertFalse(prepare_kind(self.state, "throwing axe"))
        self.assertEqual(len(self.state.items), before)
        self.assertEqual(self.state.region.materials[key(original)].support, 2)
        self.state.position = original
        self.assertTrue(auto_place(self.state, item.id, "pack", owner_id=self.state.active_courier_id))
        self.assertTrue(equip_item(self.state, item.id))
        self.assertIs(equipped_item(self.state, "readied"), item)
        self.assertEqual(game_state_from_dict(self.state.to_dict()).to_dict(), self.state.to_dict())

    def test_forked_pike_disrupts_two_bodies_but_not_an_adjacent_one(self):
        self.wield("forked pike")
        target = self.actor(Position(42, 25))
        other = self.actor(Position(42, 26))
        positions = target.position, other.position
        result = attack(self.state, target.id)
        self.assertIn("both", result.message)
        self.assertEqual((target.position, other.position), positions)
        self.assertIn("recovers position", other.intent)
        target.position = Position(41, 25)
        self.assertFalse(attack(self.state, target.id).time_advanced)

    def test_existing_crossbar_pin_also_consumes_an_enemy_turn(self):
        target = self.actor(Position(42, 25))
        target.intent = "pinned outside close range by the crossbar brace"
        before = target.position
        self.assertIn("loses a turn", _threat_action(self.state, target, False))
        self.assertEqual(target.position, before)

    def test_flail_needs_exposed_windup_before_wide_sweep(self):
        self.wield("war flail")
        target = self.actor(Position(42, 25))
        other = self.actor(Position(42, 26))
        result = attack(self.state, target.id)
        self.assertIn("wind the flail", result.message)
        self.assertEqual((target.health, other.health), (20, 20))
        result = attack(self.state, target.id)
        self.assertIn("sweeps 1 other", result.message)
        self.assertLess(target.health, 20)
        self.assertEqual(other.health, 18)

    def test_moving_abandons_flail_windup_even_with_roof_nail(self):
        self.wield("war flail")
        self.state.position = Position(40, 25, 1)
        target = self.actor(Position(42, 25, 1))
        self.state.aimed_target = target.id
        self.state.carried_passives["roof nail"] = 1
        self.assertTrue(move(self.state, 0, 1).time_advanced)
        self.assertIsNone(self.state.aimed_target)

    def test_spade_dry_dust_and_wet_failure_are_physical(self):
        for wet in (False, True):
            with self.subTest(wet=wet):
                self.setUp()
                self.wield("spade")
                cell = ensure_cell(self.state, self.state.position)
                cell.material, cell.water = "soil", int(wet)
                target = self.actor(Position(41, 25))
                impact = target.position
                result = attack(self.state, target.id)
                self.assertIn("no dust" if wet else "dry bank dust", result.message)
                if not wet:
                    self.assertGreater(self.state.region.materials[key(impact)].smoke, 0)

    def test_spade_is_a_cutting_tool_not_a_free_structural_brace(self):
        self.wield("spade")
        self.state.courier.learned_techniques = []
        self.state.courier.technique = "river sense"
        point = Position(41, 25)
        cell = ensure_cell(self.state, point)
        cell.material = "reeds"
        self.assertTrue(handle_material(self.state, "cut", point)[0])
        self.assertFalse(handle_material(self.state, "brace", point)[0])

    def test_shield_closes_dry_lane_but_injury_or_water_deny_charge(self):
        self.wield("shield and hanger")
        target = self.actor(Position(43, 25))
        self.assertTrue(attack(self.state, target.id).time_advanced)
        self.assertEqual(self.state.position, Position(42, 25))
        self.assertLess(target.health, 20)
        self.state.position = Position(40, 25)
        target.position = Position(43, 25)
        self.state.courier.injuries["feet"] = "wounded foot"
        self.assertFalse(attack(self.state, target.id).time_advanced)
        self.state.courier.injuries.clear()
        ensure_cell(self.state, Position(41, 25)).water = 1
        self.assertFalse(attack(self.state, target.id).time_advanced)

    def test_twelve_additional_forms_have_distinct_production_effects(self):
        cases = (
            ("glaive", Position(42, 25), "clips", None),
            ("pollaxe", Position(42, 25), "rigid protection", "timber"),
            ("arming sword", Position(41, 25), "counter-posture", None),
            ("long knife", Position(41, 25), "spoils the marked aim", None),
            ("boat hook", Position(43, 25), "hauls the target 2 paces", "water"),
            ("flanged mace", Position(41, 25), "break three morale", None),
            ("estoc", Position(42, 25), "rigid gap", None),
            ("felling axe", Position(41, 25), "two timber support", "timber"),
            ("quarterstaff", Position(42, 25), "drives the target back", None),
            ("reed sickle", Position(41, 25), "loose dry fuel", "reeds"),
            ("anchor fluke", Position(42, 25), "anchors the pull", "anchor"),
            ("chain hook", Position(42, 25), "entangles without direct harm", None),
        )
        for weapon, point, expected, terrain in cases:
            with self.subTest(weapon=weapon):
                self.setUp()
                self.wield(weapon)
                target = self.actor(point)
                if weapon in {"pollaxe", "estoc"}:
                    target.role = "protector"
                if weapon == "long knife":
                    target.aimed_at = self.state.position
                if terrain in {"timber", "reeds"}:
                    cell = ensure_cell(self.state, point)
                    cell.material, cell.support = terrain, 3
                elif terrain == "water":
                    ensure_cell(self.state, point).water = 2
                elif terrain == "anchor":
                    ensure_cell(self.state, self.state.position).water = 2
                if weapon == "glaive":
                    second = self.actor(Position(point.x, point.y + 1))
                before_health, before_morale = target.health, target.morale
                result = attack(self.state, target.id)
                self.assertTrue(result.time_advanced)
                self.assertIn(expected, result.message)
                if weapon == "glaive":
                    self.assertEqual(second.health, 19)
                elif weapon in {"pollaxe", "estoc"}:
                    self.assertEqual(target.health, before_health - (4 if weapon == "pollaxe" else 4))
                elif weapon == "flanged mace":
                    self.assertLessEqual(target.morale, before_morale - 3)
                elif weapon == "chain hook":
                    self.assertEqual(target.health, before_health)
                if terrain == "timber":
                    expected_support = 2 if weapon == "pollaxe" else 1
                    self.assertEqual(self.state.region.materials[key(point)].support, expected_support)
                elif terrain == "reeds":
                    self.assertEqual(self.state.region.materials[key(point)].material, "soil")

    def test_all_eighteen_working_weapons_have_finite_sources_and_previews(self):
        found = set()
        for region_id in FRONTIERS:
            region = build_frontier("working weapon sources", region_id)
            rewards = {reward for chest in region.containers for reward in chest.extra_rewards}
            found.update(rewards & set(WORK_WEAPONS))
            self.assertTrue({"sealed pitch pot", "sealed lime pot", "sealed brine pot"} <= rewards)
        self.assertEqual(found, set(WORK_WEAPONS))
        for weapon in WORK_WEAPONS:
            self.assertEqual(len(item_preview(weapon)), 3)
        self.assertEqual(len(WORK_WEAPONS), 18)
        self.assertEqual(len(WEAPONS), 36)

    def test_normal_opening_preserves_weapon_when_auto_place_is_disabled(self):
        from jomon.regions import activate_region
        activate_region(self.state, "dunmire")
        self.state.location = "region"
        self.state.threats.clear()
        self.state.auto_place_enabled = False
        chest = self.state.region.containers[0]
        self.state.position = chest.position
        result = _open_container(self.state)
        self.assertTrue(result.time_advanced)
        item = next(i for i in self.state.items if i.id in chest.item_ids and i.kind == "pot sling")
        self.assertEqual(item.location, "container")
        count = len(self.state.items)
        _open_container(self.state)
        self.assertEqual(len(self.state.items), count)
        self.assertTrue(chest.opened)
        stock = merchant_stock_for(self.state)
        self.assertTrue(set(stock) & set(WORK_WEAPONS))


if __name__ == "__main__":
    unittest.main()
