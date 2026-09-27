from __future__ import annotations

import unittest
from dataclasses import replace

from jomon.actions import choose_courier
from jomon.inventory import (
    ITEM_SPECS,
    auto_pack,
    auto_place,
    create_item,
    grid_items,
    item_preview,
    occupied_cells,
    pin_item,
    placement_preview,
    transfer_to_grid,
)
from jomon.state import create_world

def prepared_state(seed: str = "inventory usability"):
    state = create_world(seed)
    state.jomon_space = "tavern"
    choose_courier(state, state.household[0].id)
    item = next(item for item in state.items if item.location == "locker" and item.kind == "billhook")
    assert transfer_to_grid(state, item.id, "pack", owner_id=state.active_courier_id)
    return state, item




class PackingAndPaperDollTests(unittest.TestCase):
    def test_auto_pack_is_deterministic_and_preserves_pins(self):
        first, pinned = prepared_state("auto pack")
        second, second_pinned = prepared_state("auto pack")
        pin_item(first, pinned.id, True)
        pin_item(second, second_pinned.id, True)
        original = (pinned.x, pinned.y, pinned.rotated)
        self.assertTrue(auto_pack(first, "pack", owner_id=first.active_courier_id))
        self.assertTrue(auto_pack(second, "pack", owner_id=second.active_courier_id))
        self.assertEqual((pinned.x, pinned.y, pinned.rotated), original)
        self.assertEqual(first.to_dict(), second.to_dict())

    def test_auto_place_chooses_the_same_orientation_and_position(self):
        first, _ = prepared_state("auto place deterministic")
        second, _ = prepared_state("auto place deterministic")
        first_item = create_item(first, "pike", "pickup")
        second_item = create_item(second, "pike", "pickup")
        self.assertTrue(auto_place(first, first_item.id, "pack", owner_id=first.active_courier_id))
        self.assertTrue(auto_place(second, second_item.id, "pack", owner_id=second.active_courier_id))
        self.assertEqual(
            (first_item.x, first_item.y, first_item.rotated),
            (second_item.x, second_item.y, second_item.rotated),
        )




if __name__ == "__main__":
    unittest.main()
