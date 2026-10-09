from __future__ import annotations

import copy
import tempfile
import unittest
from pathlib import Path

from roag.circuits import cell_key
from roag.commands import AcquireGroundItemsCommand
from roag.inventory import create_item, place_item, sync_legacy_load
from roag.regions import begin_region
from roag.save import load_game, save_game
from roag.session import GameSession
from roag.state import CircuitCell, Position, create_world
from roag.terminal import InputEvent, InventoryView, _handle_inventory


class FieldAcquisitionTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("field acquisition")
        begin_region(self.state, "hearthford")
        self.state.weather = "clear"
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        self.state.circuits.clear()
        self.state.position = Position(40, 25, 0)
        for item in self.state.items:
            if (
                item.location == "pack"
                and item.owner_id == self.state.active_courier_id
            ):
                item.location = "lost"
                item.owner_id = None
        sync_legacy_load(self.state)

    def ground_item(self, kind: str = "ingredient:clay"):
        item = create_item(self.state, kind, "field acquisition fixture")
        item.location = "ground"
        item.region_id = self.state.spatial_id
        item.ground_position = self.state.position
        return item

    def fit_mass_engine(self):
        sensor_position = self.state.position
        rack_position = Position(
            sensor_position.x - 1,
            sensor_position.y,
            sensor_position.z,
        )
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.state.circuits[rack_key] = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
        )
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor",
            mode="mass", threshold=2,
        )
        return rack_key, sensor_key

    def test_command_acquires_once_without_advancing_world(self):
        item = self.ground_item()
        rack_key, sensor_key = self.fit_mass_engine()
        before_time = self.state.world_time

        outcome = GameSession(self.state).submit(
            AcquireGroundItemsCommand((item.id,)),
        )

        self.assertTrue(outcome.accepted)
        self.assertTrue(outcome.changed)
        self.assertFalse(outcome.time_advanced)
        self.assertEqual(outcome.result_id, "inventory.acquire.ok")
        self.assertEqual(outcome.events, ())
        self.assertEqual(outcome.event_batch.steps, ())
        self.assertEqual(self.state.world_time, before_time)
        self.assertEqual(
            (item.location, item.owner_id, item.region_id, item.ground_position),
            ("pack", self.state.active_courier_id, None, None),
        )
        self.assertTrue(self.state.vessel_changes[f"acquired:{item.id}"])
        self.assertEqual(self.state.circuits[rack_key].charge, 1)
        self.assertIn(
            "nearby physical acquisition",
            self.state.circuits[sensor_key].last_event,
        )

        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(
                self.state, Path(directory) / "field-acquisition.json",
            ))
        restored_item = next(other for other in restored.items if other.id == item.id)
        self.assertEqual(
            (restored_item.location, restored_item.owner_id),
            ("pack", restored.active_courier_id),
        )
        self.assertEqual(restored.circuits[rack_key].charge, 1)

    def test_drop_and_reacquire_does_not_repeat_resource_reaction(self):
        item = self.ground_item()
        rack_key, _ = self.fit_mass_engine()
        session = GameSession(self.state)
        self.assertTrue(session.submit(
            AcquireGroundItemsCommand((item.id,)),
        ).changed)
        self.assertEqual(self.state.circuits[rack_key].charge, 1)

        item.location = "ground"
        item.owner_id = None
        item.region_id = self.state.spatial_id
        item.ground_position = self.state.position
        self.assertTrue(session.submit(
            AcquireGroundItemsCommand((item.id,)),
        ).changed)

        self.assertEqual(self.state.circuits[rack_key].charge, 1)

    def test_multi_item_acquisition_is_deterministic_and_atomic(self):
        first = self.ground_item()
        second = self.ground_item("ingredient:spark salt")
        for y in range(6):
            for x in range(10):
                if (x, y) == (9, 5):
                    continue
                filler = create_item(
                    self.state,
                    "ingredient:clay",
                    f"pack filler {x},{y}",
                )
                self.assertTrue(place_item(
                    self.state,
                    filler.id,
                    "pack",
                    x,
                    y,
                    owner_id=self.state.active_courier_id,
                ))
        before = copy.deepcopy(self.state.to_dict())
        outcome = GameSession(self.state).submit(
            AcquireGroundItemsCommand((second.id, first.id)),
        )
        self.assertFalse(outcome.accepted)
        self.assertEqual(outcome.result_id, "inventory.acquire.no_space")
        self.assertEqual(self.state.to_dict(), before)

        self.state.items = [
            item for item in self.state.items
            if not item.provenance.startswith("pack filler")
        ]
        left, right = copy.deepcopy(self.state), copy.deepcopy(self.state)
        left_outcome = GameSession(left).submit(
            AcquireGroundItemsCommand((second.id, first.id)),
        )
        right_outcome = GameSession(right).submit(
            AcquireGroundItemsCommand((second.id, first.id)),
        )
        self.assertEqual(left_outcome, right_outcome)
        self.assertEqual(left.to_dict(), right.to_dict())
        self.assertEqual(left_outcome.target_id, f"{first.id},{second.id}")

    def test_invalid_or_remote_item_is_rejected_without_mutation(self):
        item = self.ground_item()
        item.ground_position = Position(41, 25, 0)
        session = GameSession(self.state)
        before = copy.deepcopy(self.state.to_dict())
        commands = (
            AcquireGroundItemsCommand((item.id,)),
            AcquireGroundItemsCommand(()),
            AcquireGroundItemsCommand((item.id, item.id)),
            AcquireGroundItemsCommand(("missing",)),
        )
        for command in commands:
            with self.subTest(command=command):
                outcome = session.submit(command)
                self.assertFalse(outcome.accepted)
                self.assertFalse(outcome.changed)
                self.assertFalse(outcome.time_advanced)
                self.assertEqual(outcome.event_batch.steps, ())
                self.assertEqual(self.state.to_dict(), before)
        self.assertEqual(session.revision, 0)

        item.ground_position = self.state.position
        self.state.auto_place_enabled = False
        before = copy.deepcopy(self.state.to_dict())
        outcome = session.submit(AcquireGroundItemsCommand((item.id,)))
        self.assertFalse(outcome.accepted)
        self.assertEqual(outcome.result_id, "inventory.acquire.unavailable")
        self.assertEqual(self.state.to_dict(), before)

    def test_terminal_ground_transfer_uses_session_command(self):
        item = self.ground_item()
        rack_key, _ = self.fit_mass_engine()
        session = GameSession(self.state)
        view = InventoryView.begin(self.state, "ground")
        view.pane = "ground"
        before_time = self.state.world_time

        closed, committed = _handle_inventory(
            self.state,
            view,
            InputEvent("key", key=ord("t")),
            session,
        )

        self.assertFalse(closed)
        self.assertFalse(committed)
        self.assertTrue(view.transaction.changed)
        self.assertEqual(session.revision, 1)
        self.assertEqual(item.location, "pack")
        self.assertEqual(self.state.circuits[rack_key].charge, 1)
        self.assertEqual(self.state.world_time, before_time)

    def test_terminal_cancel_restores_acquisition_and_engine_reaction(self):
        item = self.ground_item()
        rack_key, _ = self.fit_mass_engine()
        session = GameSession(self.state)
        view = InventoryView.begin(self.state, "ground")
        view.pane = "ground"
        _handle_inventory(
            self.state,
            view,
            InputEvent("key", key=ord("t")),
            session,
        )

        closed, committed = _handle_inventory(
            self.state,
            view,
            InputEvent("key", key=27),
            session,
        )

        self.assertTrue(closed)
        self.assertFalse(committed)
        restored_item = next(
            other for other in self.state.items if other.id == item.id
        )
        self.assertEqual(
            (restored_item.location, restored_item.region_id,
             restored_item.ground_position),
            ("ground", "hearthford", self.state.position),
        )
        self.assertFalse(self.state.vessel_changes.get(f"acquired:{item.id}"))
        self.assertEqual(self.state.circuits[rack_key].charge, 0)


if __name__ == "__main__":
    unittest.main()
