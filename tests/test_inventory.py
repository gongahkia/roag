from __future__ import annotations

import copy
from pathlib import Path
import tempfile
import unittest

from jomon.actions import apply_damage, choose_courier, choose_weapon, move
from jomon.content import PASSIVES
from jomon.inventory import (
    BODY_SLOTS,
    ITEM_SPECS,
    apply_terrain_status,
    auto_place,
    can_place,
    create_item,
    basic_courier_kit,
    ensure_courier_basics,
    equip_item,
    equipped_item,
    load_state,
    occupied_cells,
    protection_at,
    rotate_item,
    transfer_to_grid,
    unequip_item,
)
from jomon.save import load_game, save_game
from jomon.state import SAVE_FORMAT, Position, game_state_from_dict, create_world
from jomon.terminal import InventoryView, _handle_inventory
from jomon.world import JOMON_GANGPLANK


def active_state(seed: str = "spatial inventory"):
    state = create_world(seed)
    choose_courier(state, state.household[0].id)
    return state


class SpatialInventoryTests(unittest.TestCase):
    def test_every_initial_courier_has_one_physical_basic_working_kit(self):
        state = create_world("ready household")
        self.assertEqual(state.active_courier_id, state.household[0].id)
        self.assertEqual(state.support, "route survey")
        self.assertEqual(state.position, JOMON_GANGPLANK)
        before = len(state.items)
        for person in state.household:
            expected = basic_courier_kit(person)
            for slot, kind in expected.items():
                item = equipped_item(state, slot, person.id)
                self.assertIsNotNone(item, (person.role, slot))
                self.assertEqual(item.kind, kind)
            self.assertTrue(state.vessel_changes[f"basic_kit:{person.id}"])
            self.assertEqual(ensure_courier_basics(state, person), [])
        self.assertEqual(len(state.items), before)
        weapon = equipped_item(state, "readied", state.household[0].id)
        weapon.location, weapon.owner_id = "lost", None
        self.assertEqual(ensure_courier_basics(state, state.household[0]), [])
        self.assertIsNone(equipped_item(state, "readied", state.household[0].id))
        self.assertEqual(len(state.items), before)

    def test_unprepared_format_five_world_becomes_ready_without_item_loss(self):
        state = create_world("pre-ready format five")
        data = state.to_dict()
        data["active_courier_id"] = None
        data["weapon"] = data["gear"] = data["support"] = None
        data["vessel_changes"] = {
            key: value for key, value in data["vessel_changes"].items()
            if not key.startswith("basic_kit:")
        }
        data["items"] = [
            item for item in data["items"]
            if not item["provenance"].startswith("Jomon working issue")
        ]
        kept_ids = {item["id"] for item in data["items"]}
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.active_courier_id, loaded.household[0].id)
        self.assertEqual(loaded.position, JOMON_GANGPLANK)
        self.assertEqual(loaded.support, "route survey")
        self.assertTrue(kept_ids.issubset({item.id for item in loaded.items}))
        self.assertIsNotNone(equipped_item(loaded, "readied"))
        self.assertIsNotNone(equipped_item(loaded, "secondary"))

    def test_rotated_grid_placement_and_exact_save_round_trip(self):
        state = active_state()
        rope = next(item for item in state.items if item.kind == "rope")
        self.assertTrue(transfer_to_grid(state, rope.id, "pack", owner_id=state.active_courier_id))
        self.assertEqual(len(occupied_cells(rope)), 4)
        self.assertTrue(rotate_item(state, rope.id))
        self.assertTrue(rope.rotated)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        restored = next(item for item in loaded.items if item.id == rope.id)
        self.assertEqual((restored.x, restored.y, restored.rotated), (rope.x, rope.y, True))
        self.assertEqual(loaded.to_dict(), state.to_dict())

    def test_collision_and_boundary_rejection(self):
        state = active_state("collision")
        spear = next(item for item in state.items if item.kind == "spear")
        hook = next(item for item in state.items if item.kind == "billhook")
        self.assertTrue(can_place(state, spear, "pack", 0, 0, owner_id=state.active_courier_id))
        self.assertTrue(transfer_to_grid(state, spear.id, "pack", owner_id=state.active_courier_id))
        self.assertFalse(can_place(state, hook, "pack", spear.x, spear.y, owner_id=state.active_courier_id))
        self.assertFalse(can_place(state, hook, "pack", state.pack_width, 0, owner_id=state.active_courier_id))

    def test_equip_and_unequip_never_lose_item(self):
        state = active_state("equip")
        spear = next(item for item in state.items if item.kind == "spear")
        self.assertTrue(transfer_to_grid(state, spear.id, "pack", owner_id=state.active_courier_id))
        self.assertTrue(equip_item(state, spear.id))
        self.assertEqual((spear.location, state.weapon), ("readied", "spear"))
        self.assertTrue(unequip_item(state, "readied"))
        self.assertEqual(spear.location, "pack")
        self.assertIn(spear.id, {item.id for item in state.items})

    def test_ranged_preparation_rolls_back_when_ammunition_cannot_fit(self):
        state = active_state("transactional ranged preparation")
        owner = state.active_courier_id
        state.owned_weapons.extend(["pike", "longbow"])
        pike = create_item(state, "pike", "test readied weapon")
        longbow = create_item(state, "longbow", "test candidate")
        for item in state.items:
            if item.kind not in {"longbow", "pike", "consumable:fletched arrows"}:
                item.location = "destroyed"
                item.owner_id = None
        pike.location, pike.owner_id = "readied", owner
        state.weapon = "pike"
        for y in range(state.pack_height):
            for x in range(state.pack_width):
                if x == 0:
                    continue
                blocker = create_item(state, "consumable:packing block", "test", owner_id=owner)
                blocker.location, blocker.x, blocker.y = "pack", x, y
        longbow.location = "locker"
        before = copy.deepcopy(state.to_dict())
        result = choose_weapon(state, "longbow")
        self.assertFalse(result.changed)
        after = state.to_dict()
        before.pop("messages")
        after.pop("messages")
        self.assertEqual(after, before)
        self.assertEqual(state.weapon, "pike")

    def test_armour_catalogue_has_three_choices_per_body_location(self):
        armour = [spec for spec in ITEM_SPECS.values() if spec.category == "armour"]
        self.assertGreaterEqual(len(armour), 18)
        for slot in BODY_SLOTS:
            self.assertGreaterEqual(sum(spec.slot == slot for spec in armour), 3)

    def test_armour_protection_and_terrain_tags_are_physical(self):
        state = active_state("protection")
        boots = next(item for item in state.items if item.kind == "hobnailed boots")
        self.assertTrue(transfer_to_grid(state, boots.id, "pack", owner_id=state.active_courier_id))
        self.assertTrue(equip_item(state, boots.id))
        amount, name = protection_at(state, "feet", "pierce")
        self.assertGreater(amount, 0)
        self.assertIn("boots", name.lower())
        self.assertEqual(apply_terrain_status(state, "q"), "")
        self.assertNotEqual(apply_terrain_status(state, "m"), "")
        self.assertIn("bogged", state.terrain_statuses)

    def test_armour_protects_contextual_location_and_degrades(self):
        state = active_state("armour damage kinds")
        helm = create_item(state, "kettle helm", "test armour")
        self.assertTrue(auto_place(state, helm.id, "pack", owner_id=state.active_courier_id))
        self.assertTrue(equip_item(state, helm.id))
        for damage_kind in ("cut", "pierce", "blunt"):
            self.assertGreater(protection_at(state, "head", damage_kind)[0], 0)
        before_health = state.courier.health
        result = apply_damage(state, 4, "A measured bolt", damage_kind="pierce", location="head")
        self.assertEqual(state.courier.health, before_health - 1)
        self.assertLess(helm.condition, 100)
        self.assertIn("Kettle helm absorbs 3", result)

    def test_injured_foot_and_encumbrance_each_cost_time_on_terrain(self):
        state = active_state("injured footing")
        state.location = "region"
        state.position = Position(40, 25)
        state.region.tile_changes["41,25,0"] = "q"
        state.courier.injuries["feet"] = "wounded foot"
        before = state.world_time
        move(state, 1, 0)
        self.assertEqual(state.world_time - before, 2)

        state = active_state("encumbered movement")
        state.location = "region"
        state.position = Position(40, 25)
        state.region.tile_changes["41,25,0"] = "."
        for index in range(4):
            coat = create_item(state, "riveted coat", f"test burden {index}")
            self.assertTrue(auto_place(state, coat.id, "pack", owner_id=state.active_courier_id))
        self.assertEqual(load_state(state), "overloaded")
        before = state.world_time
        move(state, 1, 0)
        self.assertEqual(state.world_time - before, 2)

    def test_container_to_pack_transfer_updates_both_physical_places(self):
        state = active_state("container transfer")
        state.location = "region"
        box = state.region.containers[0]
        item = create_item(state, "consumable:willow dressing", box.name, location="container")
        item.container_id = box.id
        box.item_ids.append(item.id)
        view = InventoryView.begin(state, f"container:{box.id}")
        view.pane = f"container:{box.id}"
        closed, committed = _handle_inventory(state, view, ord("t"))
        self.assertFalse(closed)
        self.assertFalse(committed)
        self.assertEqual((item.location, item.owner_id), ("pack", state.active_courier_id))
        self.assertNotIn(item.id, box.item_ids)

    def test_load_bands_have_distinct_thresholds(self):
        state = active_state("weight")
        self.assertEqual(load_state(state), "light")
        for index in range(4):
            item = create_item(state, "riveted coat", f"test burden {index}", owner_id=state.active_courier_id)
            self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        self.assertEqual(load_state(state), "overloaded")


class SaveMigrationTests(unittest.TestCase):
    def test_version_three_migrates_deterministically_without_deleting_possessions(self):
        original = create_world("format three migration")
        choose_courier(original, original.household[2].id)
        legacy = copy.deepcopy(original.to_dict())
        legacy["save_format"] = 3
        for key in (
            "active_region_id", "items", "next_item_id", "pack_width", "pack_height",
            "locker_width", "locker_height", "terrain_statuses", "objective_evidence",
        ):
            legacy.pop(key)
        for person in legacy["household"]:
            for key in ("injuries", "background", "build_tendency", "home_region", "recruited", "available"):
                person.pop(key)
        for container in legacy["region"]["containers"]:
            container.pop("item_ids")
        legacy["owned_passives"] = {name: 1 for name in PASSIVES}
        legacy["carried_passives"] = {name: 1 for name in PASSIVES}
        legacy_supplies = {f"legacy supply {index}": 1 for index in range(70)}
        legacy["consumables"] = legacy_supplies
        first = game_state_from_dict(copy.deepcopy(legacy))
        second = game_state_from_dict(copy.deepcopy(legacy))
        self.assertEqual(first.save_format, SAVE_FORMAT)
        self.assertEqual(first.to_dict(), second.to_dict())
        kinds = {item.kind for item in first.items if item.location not in {"lost", "destroyed"}}
        self.assertTrue(set(first.owned_weapons) <= kinds)
        self.assertTrue(set(first.owned_gear) <= kinds)
        physical_passives = {
            item.kind.split(":", 1)[1]
            for item in first.items
            if item.kind.startswith("passive:") and item.location in {"pack", "locker"}
        }
        self.assertEqual(physical_passives, set(PASSIVES))
        physical_supplies = {
            item.kind.split(":", 1)[1]
            for item in first.items
            if item.kind.startswith("consumable:") and item.location in {"pack", "locker"}
        }
        self.assertTrue(set(legacy_supplies) <= physical_supplies)
        self.assertTrue(any(
            item.kind.startswith("consumable:") and item.location == "locker"
            for item in first.items
        ))
        self.assertEqual(
            set(first.carried_passives),
            {
                item.kind.split(":", 1)[1]
                for item in first.items
                if item.kind.startswith("passive:") and item.location == "pack"
            },
        )
