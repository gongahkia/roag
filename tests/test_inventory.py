from __future__ import annotations

import copy
from pathlib import Path
import tempfile
import unittest

from jomon.actions import choose_courier
from jomon.inventory import (
    BODY_SLOTS,
    ITEM_SPECS,
    apply_terrain_status,
    auto_place,
    can_place,
    create_item,
    equip_item,
    load_state,
    occupied_cells,
    protection_at,
    rotate_item,
    transfer_to_grid,
    unequip_item,
)
from jomon.save import load_game, save_game
from jomon.state import SAVE_FORMAT, game_state_from_dict, create_world


def active_state(seed: str = "spatial inventory"):
    state = create_world(seed)
    choose_courier(state, state.household[0].id)
    return state


class SpatialInventoryTests(unittest.TestCase):
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
        first = game_state_from_dict(copy.deepcopy(legacy))
        second = game_state_from_dict(copy.deepcopy(legacy))
        self.assertEqual(first.save_format, SAVE_FORMAT)
        self.assertEqual(first.to_dict(), second.to_dict())
        kinds = {item.kind for item in first.items if item.location not in {"lost", "destroyed"}}
        self.assertTrue(set(first.owned_weapons) <= kinds)
        self.assertTrue(set(first.owned_gear) <= kinds)

