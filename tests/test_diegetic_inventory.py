from __future__ import annotations

import copy
import curses
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
from jomon.terminal import (
    InputEvent,
    InventoryView,
    _handle_inventory,
    _transfer_inventory_items,
    normalise_input,
    paper_doll_layout,
)


def prepared_state(seed: str = "inventory usability"):
    state = create_world(seed)
    state.jomon_space = "tavern"
    choose_courier(state, state.household[0].id)
    item = next(item for item in state.items if item.location == "locker" and item.kind == "billhook")
    assert transfer_to_grid(state, item.id, "pack", owner_id=state.active_courier_id)
    return state, item


class PreviewAndMouseTests(unittest.TestCase):
    def test_lift_rotation_and_escape_leave_committed_source_exact(self):
        state, item = prepared_state()
        original = copy.deepcopy(item)
        view = InventoryView.begin(state)
        view.cursor_x, view.cursor_y = item.x, item.y
        _handle_inventory(state, view, 10)
        self.assertEqual(view.held_id, item.id)
        self.assertEqual(item.location, "pack")
        _handle_inventory(state, view, ord("r"))
        self.assertNotEqual(view.held_rotated, original.rotated)
        self.assertEqual(item.rotated, original.rotated)
        _handle_inventory(state, view, 27)
        self.assertIsNone(view.held_id)
        self.assertEqual(item, original)

    def test_preview_reports_rotation_collision_and_resulting_load(self):
        state, item = prepared_state("preview")
        clear = placement_preview(
            state, item, "pack", item.x, item.y,
            rotated=not item.rotated, owner_id=state.active_courier_id,
        )
        self.assertEqual(clear.cells, frozenset(occupied_cells(replace(item, rotated=not item.rotated))))
        self.assertIn(clear.reason, {"valid placement", "out of bounds"})
        blocker = create_item(state, "sling", "test", owner_id=state.active_courier_id)
        blocker.location, blocker.x, blocker.y = "pack", item.x, item.y
        blocked = placement_preview(state, item, "pack", item.x, item.y, owner_id=state.active_courier_id)
        self.assertFalse(blocked.valid)
        self.assertIn(blocker.id, blocked.blockers)
        self.assertIn(blocked.resulting_load, {"light", "laden", "encumbered", "overloaded"})

    def test_mouse_click_matches_keyboard_lift_and_unsupported_is_safe(self):
        state, item = prepared_state("mouse inventory")
        mouse = create_world("mouse inventory")
        mouse.jomon_space = "tavern"
        choose_courier(mouse, mouse.household[0].id)
        same = next(candidate for candidate in mouse.items if candidate.id == item.id)
        transfer_to_grid(mouse, same.id, "pack", owner_id=mouse.active_courier_id)
        keyboard_view = InventoryView.begin(state)
        keyboard_view.cursor_x, keyboard_view.cursor_y = item.x, item.y
        mouse_view = InventoryView.begin(mouse)
        _handle_inventory(state, keyboard_view, 10)
        _handle_inventory(mouse, mouse_view, InputEvent("mouse", x=2 + same.x * 2, y=3 + same.y, button="left"))
        self.assertEqual(keyboard_view.held_id, mouse_view.held_id)

        def unavailable():
            raise curses.error("no mouse")

        self.assertEqual(normalise_input(curses.KEY_MOUSE, unavailable).kind, "unsupported-mouse")

    def test_mouse_places_and_right_click_rotates_only_the_live_ghost(self):
        state, item = prepared_state("mouse placement")
        view = InventoryView.begin(state)
        _handle_inventory(
            state, view,
            InputEvent("mouse", x=2 + item.x * 2, y=3 + item.y, button="left"),
        )
        committed_rotation = item.rotated
        _handle_inventory(state, view, InputEvent("mouse", x=2, y=3, button="right"))
        self.assertNotEqual(view.held_rotated, committed_rotation)
        self.assertEqual(item.rotated, committed_rotation)
        destination = next(
            (x, y)
            for y in range(state.pack_height)
            for x in range(state.pack_width)
            if placement_preview(
                state, item, "pack", x, y, rotated=view.held_rotated,
                owner_id=state.active_courier_id,
            ).valid
            and (x, y) != (item.x, item.y)
        )
        _handle_inventory(
            state, view,
            InputEvent("mouse", x=2 + destination[0] * 2, y=3 + destination[1], button="left"),
        )
        self.assertIsNone(view.held_id)
        self.assertEqual((item.x, item.y, item.rotated), (*destination, view.held_rotated))

    def test_mouse_bitmask_normalises_click_without_terminal_dependency(self):
        event = normalise_input(
            curses.KEY_MOUSE,
            lambda: (0, 9, 7, 0, curses.BUTTON1_CLICKED),
        )
        self.assertEqual((event.kind, event.button, event.x, event.y), ("mouse", "left", 9, 7))


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

    def test_auto_place_failure_and_bulk_transfer_lose_nothing(self):
        state, _ = prepared_state("bulk")
        locker_items = grid_items(state, "locker")[:2]
        before_ids = {item.id for item in state.items}
        view = InventoryView.begin(state)
        view.pane = "locker"
        self.assertTrue(_transfer_inventory_items(state, view, locker_items))
        self.assertEqual({item.id for item in state.items}, before_ids)
        self.assertTrue(all(item.location == "pack" for item in locker_items))
        oversized = create_item(state, "pike", "failure test")
        state.pack_width = state.pack_height = 1
        self.assertFalse(auto_place(state, oversized.id, "pack", owner_id=state.active_courier_id))
        self.assertEqual(oversized.location, "lost")

    def test_every_authored_item_has_preview_and_paper_doll_has_all_slots(self):
        state, _ = prepared_state("paper doll")
        for kind in ITEM_SPECS:
            with self.subTest(kind=kind):
                self.assertEqual(len(item_preview(kind)), 3)
                self.assertTrue(any(line.strip() for line in item_preview(kind)))
        doll = "\n".join(paper_doll_layout(state))
        for marker in ("W [", "T [", "P [PACK]"):
            self.assertIn(marker, doll)
        self.assertEqual(len(paper_doll_layout(state)), 8)


if __name__ == "__main__":
    unittest.main()
