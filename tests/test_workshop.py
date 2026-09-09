import copy
import curses
import unittest

from jomon.actions import apply_damage, attack, effective_weapon_range, interact
from jomon.inventory import (
    InventoryTransaction, armour_mobility, armour_noise, auto_place, create_item,
    drop_item, equipped_item, pack_weight, physical_ammunition, placement_preview,
    protection_at, sync_legacy_load, terrain_status_for,
)
from jomon.materials import affect_body, handle_material
from jomon.state import MaterialCell, Position, StateError, TerrainStatus, Threat, create_world, game_state_from_dict
from jomon.terminal import InputEvent, OverlayView, _draw_dialogue_overlay, _handle_overlay_view, dialogue_choices
from test_information_panels import PanelSink
from jomon.workshop import FITTINGS, WORKBENCH, active_part, attached, buy_kit, install, remove, repair


class WorkshopTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("workshop production")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.position = WORKBENCH
        self.state.trade_credit = 40

    def ready(self, name):
        old = equipped_item(self.state, "readied")
        self.assertTrue(auto_place(self.state, old.id, "pack", owner_id=self.state.active_courier_id))
        item = create_item(self.state, name, "controlled weapon scenario", location="readied", owner_id=self.state.active_courier_id)
        sync_legacy_load(self.state)
        return item

    def field(self):
        state = self.state
        state.location, state.position = "region", Position(28, 20)
        state.threats.clear()
        for y in range(15, 30):
            for x in range(23, 44):
                state.region.tile_changes[f"{x},{y},0"] = "."
        actor = Threat("target", "observed target", "pursuer", Position(30, 20), 20, 20, morale=20, status="watching")
        state.threats.append(actor)
        return actor

    def test_workshop_requires_physical_entry_and_preview_is_zero_time(self):
        state = self.state
        self.assertEqual(interact(state).overlay, "station:workshop")
        before = state.to_dict()
        view = OverlayView("station:workshop")
        _handle_overlay_view(state, view, InputEvent("key", ord("1")))
        self.assertEqual(view.kind, "workshop:slot:readied")
        _handle_overlay_view(state, view, InputEvent("key", ord("b")))
        self.assertIn("workshop:fit:", view.kind)
        _handle_overlay_view(state, view, InputEvent("key", 27))
        self.assertEqual(state.to_dict(), before)
        state.position = Position(63, 10)
        parent = equipped_item(state, "readied")
        self.assertFalse(install(state, parent.id, "quiet binding")[0])

    def test_install_uses_one_physical_kit_and_two_actions(self):
        state = self.state
        parent = equipped_item(state, "readied")
        before_weight, before_time = pack_weight(state), state.world_time
        count = state.vessel_changes["fitting_stock:quiet binding"]
        self.assertTrue(install(state, parent.id, "quiet binding")[0])
        self.assertEqual(state.world_time, before_time + 2)
        self.assertEqual(state.vessel_changes["fitting_stock:quiet binding"], count - 1)
        part = attached(state, parent)[0]
        self.assertEqual(part.location, "fitted")
        self.assertEqual(part.fitted_to, parent.id)
        self.assertEqual(pack_weight(state), before_weight + 1)
        original = state.to_dict()
        self.assertFalse(install(state, parent.id, "quiet binding")[0])
        self.assertEqual(state.to_dict(), original)

    def test_all_eight_kits_are_physical_and_removable(self):
        for name in FITTINGS:
            with self.subTest(fitting=name):
                self.setUp()
                parent = (equipped_item(self.state, "feet") if name == "reed lining" else equipped_item(self.state, "torso") if FITTINGS[name].slot == "lining" else self.ready("staff") if name == "iron heel" else self.ready("javelins") if name == "retrieval cord" else self.ready("longbow") if name in {"resin seal", "ash wrap"} else equipped_item(self.state, "readied"))
                self.assertTrue(buy_kit(self.state, name)[0])
                kit = next(item for item in self.state.items if item.kind == f"fitting:{name}")
                kit.rotated = True
                self.assertTrue(install(self.state, parent.id, name)[0])
                self.assertTrue(remove(self.state, parent.id, FITTINGS[name].slot)[0])
                self.assertEqual(kit.location, "pack")
                self.assertIsNone(kit.fitted_to)
                self.assertEqual(sum(item.id == kit.id for item in self.state.items), 1)

    def test_full_pack_removal_and_purchase_leave_exact_state(self):
        state = self.state
        parent = equipped_item(state, "readied")
        install(state, parent.id, "quiet binding")
        for y in range(6):
            for x in range(10):
                item = create_item(state, "consumable:willow dressing", "full pack fixture", location="pack", owner_id=state.active_courier_id)
                item.x, item.y = x, y
        before = state.to_dict()
        self.assertFalse(remove(state, parent.id, "structure")[0])
        self.assertEqual(state.to_dict(), before)
        self.assertFalse(buy_kit(state, "iron heel")[0])
        self.assertEqual(state.to_dict(), before)

    def test_cancel_restores_attached_items_stock_credit_and_exact_layout(self):
        state = self.state
        before = state.to_dict()
        transaction = InventoryTransaction.begin(state)
        install(state, equipped_item(state, "readied").id, "quiet binding")
        transaction.cancel(state)
        self.assertEqual(state.to_dict(), before)

    def test_drop_recovery_and_parent_loss_keep_attachment_identity(self):
        state = self.state
        parent = equipped_item(state, "readied")
        install(state, parent.id, "quiet binding")
        part = attached(state, parent)[0]
        state.location, state.position = "region", state.region.landmarks["landing"]
        before_weight = pack_weight(state)
        drop_item(state, parent.id)
        self.assertEqual(pack_weight(state), before_weight - 6)
        self.assertEqual(part.fitted_to, parent.id)
        preview = placement_preview(state, parent, "pack", 0, 0, owner_id=state.active_courier_id)
        self.assertEqual(preview.resulting_weight, before_weight)
        self.assertTrue(auto_place(state, parent.id, "pack", owner_id=state.active_courier_id))
        self.assertEqual(pack_weight(state), before_weight)
        parent.location, parent.owner_id = "lost", None
        loaded = game_state_from_dict(state.to_dict())
        recovered = next(item for item in loaded.items if item.id == part.id)
        self.assertEqual(recovered.fitted_to, parent.id)
        self.assertIsNone(active_part(loaded, "quiet binding"))

    def test_invalid_mounts_and_double_sockets_are_rejected(self):
        parent = equipped_item(self.state, "readied")
        install(self.state, parent.id, "quiet binding")
        part = attached(self.state, parent)[0]
        for target in ("missing", part.id):
            data = self.state.to_dict()
            next(item for item in data["items"] if item["id"] == part.id)["fitted_to"] = target
            with self.assertRaises(StateError):
                game_state_from_dict(data)
        data = self.state.to_dict()
        duplicate = copy.deepcopy(next(item for item in data["items"] if item["id"] == part.id))
        duplicate["id"] = "illegal-second-binding"
        data["items"].append(duplicate)
        with self.assertRaises(StateError):
            game_state_from_dict(data)

    def test_two_sockets_coexist_and_worn_out_parts_lose_their_effect(self):
        parent = self.ready("longbow")
        self.assertTrue(install(self.state, parent.id, "quiet binding")[0])
        self.assertTrue(install(self.state, parent.id, "resin seal")[0])
        self.assertEqual(len(attached(self.state, parent)), 2)
        self.assertEqual(effective_weapon_range(self.state), 11)
        active_part(self.state, "resin seal").condition = 0
        self.assertEqual(effective_weapon_range(self.state), 12)
        self.assertIsNotNone(active_part(self.state, "quiet binding"))
        self.assertIsNone(active_part(self.state, "resin seal"))

    def test_treatments_are_not_sold_for_actions_they_cannot_change(self):
        before = self.state.to_dict()
        self.assertFalse(install(self.state, equipped_item(self.state, "readied").id, "ash wrap")[0])
        self.assertEqual(self.state.to_dict(), before)
        parent = self.ready("heavy crossbow")
        self.assertFalse(install(self.state, parent.id, "resin seal")[0])

    def test_quiet_binding_changes_real_attack_sound_and_wears(self):
        state = self.state
        install(state, equipped_item(state, "readied").id, "quiet binding")
        target = self.field()
        result = attack(state, target.id)
        self.assertIn("quiet binding", result.message)
        self.assertEqual(active_part(state, "quiet binding").condition, 99)
        self.assertFalse(any(event.strength > 0 and event.position == state.position for event in state.sound_events))

    def test_iron_heel_enables_staff_material_work(self):
        state = self.state
        parent = self.ready("staff")
        state.gear, state.support = "quiet shoes", None
        install(state, parent.id, "iron heel")
        self.field()
        point = Position(27, 20)
        state.region.materials["27,20,0"] = MaterialCell(material="timber", support=1)
        changed, message = handle_material(state, "brace", point)
        self.assertTrue(changed)
        self.assertEqual(state.region.materials["27,20,0"].support, 3)
        self.assertIn("iron heel", message)
        self.assertEqual(active_part(state, "iron heel").condition, 95)

    def test_retrieval_cord_preserves_finite_physical_ammunition(self):
        state = self.state
        parent = self.ready("javelins")
        install(state, parent.id, "retrieval cord")
        supply = create_item(state, "consumable:throwing javelins", "finite cast test", quantity=3)
        self.assertTrue(auto_place(state, supply.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        target = self.field()
        before = physical_ammunition(state, "javelins")
        self.assertEqual(effective_weapon_range(state), 5)
        result = attack(state, target.id)
        self.assertIn("reels", result.message)
        self.assertEqual(physical_ammunition(state, "javelins"), before)
        self.assertEqual(active_part(state, "retrieval cord").condition, 90)

    def test_resin_seal_preserves_rain_shot_but_burns_faster(self):
        state = self.state
        parent = self.ready("longbow")
        install(state, parent.id, "resin seal")
        supply = create_item(state, "consumable:fletched arrows", "finite shot test", quantity=3)
        auto_place(state, supply.id, "pack", owner_id=state.active_courier_id)
        sync_legacy_load(state)
        target = self.field()
        attack(state, target.id)
        state.weather = "hard rain"
        before = target.health
        result = attack(state, target.id)
        self.assertLess(target.health, before)
        self.assertIn("resin seal", result.message)
        before = parent.condition
        affect_body(state, parent, "fire", 1, state.position)
        self.assertEqual(parent.condition, before - 12)

    def test_ash_wrap_changes_smoke_range_but_not_full_obstruction(self):
        state = self.state
        parent = self.ready("longbow")
        install(state, parent.id, "ash wrap")
        target = self.field()
        state.terrain_statuses["smoke-inhalation"] = TerrainStatus("smoke", 4, "shortened range")
        self.assertEqual(effective_weapon_range(state), 12)
        state.region.tile_changes["29,20,0"] = "#"
        from jomon.world import line_of_sight
        self.assertFalse(line_of_sight(state, state.position, target.position))
        before = state.to_dict()
        self.assertFalse(attack(state, target.id).time_advanced)
        after = state.to_dict()
        after["messages"] = before["messages"]
        self.assertEqual(after, before)
        active_part(state, "ash wrap").condition = 0
        self.assertEqual(effective_weapon_range(state), 10)

    def test_succession_and_thief_defeat_leave_the_same_fitted_parent(self):
        state = self.state
        parent = equipped_item(state, "readied")
        install(state, parent.id, "quiet binding")
        part = attached(state, parent)[0]
        self.field()
        death_site, dead_id = state.position, state.active_courier_id
        state.courier.health, state.courier.injury = 1, "deep cut"
        apply_damage(state, 5, "A fatal measured blow", location="torso")
        self.assertNotEqual(state.active_courier_id, dead_id)
        self.assertEqual(parent.ground_position, death_site)
        self.assertEqual(part.fitted_to, parent.id)
        state.location, state.position = "region", death_site
        thief = Threat("recoverer", "cargo recoverer", "pursuer", Position(29, 20), 1, 1, status="engaged", role="thief", carrying_item_id=parent.id, morale=5)
        parent.location = "enemy"
        state.threats = [thief]
        attack(state, thief.id)
        self.assertEqual(parent.location, "ground")
        self.assertEqual(part.fitted_to, parent.id)
        loaded = game_state_from_dict(state.to_dict())
        self.assertEqual(next(item for item in loaded.items if item.id == part.id).fitted_to, parent.id)

    def test_workshop_choices_and_previews_fit_supported_layouts(self):
        state = self.state
        kinds = ["station:workshop", "workshop:store", "workshop:slot:readied"]
        kinds += ["workshop:buy:" + name for name in FITTINGS]
        for width, height in ((80, 24), (100, 32)):
            for kind in kinds:
                with self.subTest(width=width, kind=kind):
                    sink, view = PanelSink(height, width), OverlayView(kind)
                    _draw_dialogue_overlay(sink, state, view)
                    self.assertEqual(len(view.option_rows), len(dialogue_choices(state, kind)))
                    self.assertTrue(any("credit" in line.lower() for line in sink.writes))
        view, sink = OverlayView("station:workshop"), PanelSink()
        _draw_dialogue_overlay(sink, state, view)
        _handle_overlay_view(state, view, InputEvent("mouse", button="left", y=view.option_rows[0][0], double=True))
        self.assertEqual(view.kind, "workshop:slot:readied")

    def test_linings_change_terrain_weight_coverage_and_mobility(self):
        state = self.state
        torso = equipped_item(state, "torso")
        install(state, torso.id, "wool lining")
        before = pack_weight(state)
        state.terrain_statuses["wet"] = TerrainStatus("water", 4, "heavy wet lining")
        self.assertEqual(pack_weight(state), before + 4)
        feet = equipped_item(state, "feet")
        install(state, feet.id, "reed lining")
        self.assertIsNone(terrain_status_for(state, "m"))
        self.assertIsNone(terrain_status_for(state, "w"))
        head = equipped_item(state, "head")
        noise, mobility, protection = armour_noise(state), armour_mobility(state), protection_at(state, "head", "pierce")[0]
        install(state, head.id, "iron scales")
        self.assertGreater(protection_at(state, "head", "pierce")[0], protection)
        self.assertEqual(armour_noise(state), noise + 1)
        self.assertEqual(armour_mobility(state), mobility + 1)

    def test_repair_has_counted_cost_and_preserves_fitting_wear(self):
        state = self.state
        parent = equipped_item(state, "readied")
        install(state, parent.id, "quiet binding")
        part = attached(state, parent)[0]
        parent.condition, part.condition = 25, 20
        before = state.trade_credit
        self.assertTrue(repair(state, parent.id)[0])
        self.assertEqual(parent.condition, 60)
        self.assertEqual(part.condition, 20)
        self.assertEqual(state.trade_credit, before - 2)

    def test_migration_adds_stock_without_reissuing_items_or_changing_layout(self):
        data = self.state.to_dict()
        data["save_format"] = 6
        data["vessel_changes"] = {key: value for key, value in data["vessel_changes"].items() if not key.startswith("fitting_stock:")}
        first, second = game_state_from_dict(data), game_state_from_dict(data)
        self.assertEqual(first.to_dict(), second.to_dict())
        self.assertEqual(first.to_dict()["items"], data["items"])
        self.assertEqual(len([key for key in first.vessel_changes if key.startswith("fitting_stock:")]), 8)
