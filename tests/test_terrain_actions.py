from __future__ import annotations

import copy
import tempfile
import unittest
from pathlib import Path

from roag.circuits import cell_key
from roag.commands import AdvanceWorldCommand, TerrainActionCommand
from roag.inventory import item_spec
from roag.materials import handle_material, key, material_at
from roag.regions import begin_region, validate_region
from roag.runtime_events import CollapseResolved, TerrainChanged, TerrainDamaged
from roag.save import load_game, save_game
from roag.session import GameSession
from roag.state import CircuitCell, Position, StateError, create_world, game_state_from_dict
from roag.terrain import replace_terrain, terrain_at


class TerrainActionTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("world three terrain actions")
        begin_region(self.state, "hearthford")
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        self.state.position = Position(40, 25, 0)
        self.target = Position(41, 25, 0)
        self.coordinate = key(self.target)
        self.state.region.tile_changes[self.coordinate] = '"'
        self.state.region.materials.pop(self.coordinate, None)
        self.state.region.terrain_damage.clear()

    def test_session_records_partial_damage_then_replaces_dense_reeds(self):
        session = GameSession(self.state)
        before_time = self.state.world_time

        first = session.submit(TerrainActionCommand("cut", self.target))
        self.assertEqual(first.result_id, "terrain.damaged")
        self.assertEqual(self.state.world_time, before_time + 1)
        self.assertEqual(self.state.region.terrain_damage[self.coordinate], 1)
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, '"')
        self.assertEqual([type(event) for event in first.events], [TerrainDamaged])
        self.assertEqual(first.events[0].remaining, 1)
        self.assertIn("1 resistance remains", self.state.messages[-1])
        self.assertEqual(len(first.event_batch.steps), 1)
        self.assertEqual(first.event_batch.steps[0].events, ())
        self.assertEqual(self.state.noise, 2)
        self.assertEqual(self.state.sound_events[-1].position, self.target)
        self.assertFalse(any(
            item.kind == "material:reeds" and item.location != "destroyed"
            for item in self.state.items
        ))

        second = session.submit(TerrainActionCommand("cut", self.target))
        self.assertEqual(second.result_id, "terrain.destroyed")
        self.assertEqual(self.state.world_time, before_time + 2)
        self.assertNotIn(self.coordinate, self.state.region.terrain_damage)
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, ".")
        self.assertEqual(
            [type(event) for event in second.events],
            [TerrainDamaged, TerrainChanged],
        )
        self.assertEqual(second.events[-1].previous_terrain_id, "terrain.region.dense_reeds")
        self.assertEqual(second.events[-1].terrain_id, "terrain.region.hearthford.ground")
        self.assertIn("terrain gives way", self.state.messages[-1])
        yielded = self.state.region.materials[self.coordinate]
        self.assertEqual((yielded.material, yielded.fuel), ("reeds", 2))
        physical = next(
            item for item in self.state.items
            if item.kind == "material:reeds" and item.location == "pack"
        )
        self.assertEqual(
            (physical.owner_id, physical.quantity),
            (self.state.active_courier_id, 2),
        )
        self.assertTrue(self.state.vessel_changes[f"acquired:{physical.id}"])
        self.assertEqual(item_spec(physical.kind).name, "reeds")
        self.assertIn("Recovered 2 x reeds into the pack", self.state.messages[-1])

        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(
                self.state, Path(directory) / "terrain-yield.json",
            ))
        restored_physical = next(
            item for item in restored.items if item.id == physical.id
        )
        self.assertEqual(
            (restored_physical.kind, restored_physical.location,
             restored_physical.owner_id, restored_physical.quantity),
            ("material:reeds", "pack", restored.active_courier_id, 2),
        )

    def test_ordinary_reeds_and_mud_remain_one_action_material_work(self):
        self.state.region.tile_changes[self.coordinate] = ";"
        before = self.state.world_time
        changed, _ = handle_material(self.state, "cut", self.target)
        self.assertTrue(changed)
        self.assertEqual(self.state.world_time, before + 1)
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, ".")
        reeds = next(
            item for item in self.state.items
            if item.kind == "material:reeds" and item.location == "pack"
        )
        self.assertEqual(reeds.quantity, 1)
        self.assertEqual(
            (self.state.region.materials[self.coordinate].support,
             self.state.region.materials[self.coordinate].collapse_due),
            (3, 0),
        )

        self.state.region.tile_changes[self.coordinate] = "m"
        self.state.region.materials.pop(self.coordinate, None)
        changed, _ = handle_material(self.state, "dig", self.target)
        self.assertTrue(changed)
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, ".")
        self.assertEqual(self.state.region.materials[self.coordinate].material, "soil")
        self.assertEqual(
            (self.state.region.materials[self.coordinate].support,
             self.state.region.materials[self.coordinate].collapse_due),
            (3, 0),
        )
        clay = next(
            item for item in self.state.items
            if item.kind == "ingredient:clay" and item.location == "pack"
        )
        self.assertEqual(clay.quantity, 1)
        self.assertIn("harvested clay from terrain", clay.provenance)

    def test_standing_timber_requires_sustained_loud_work_and_yields_cargo(self):
        self.state.region.tile_changes[self.coordinate] = "T"
        self.state.weapon = "felling axe"
        self.state.auto_place_enabled = False
        self.state.circuits.clear()
        session = GameSession(self.state)
        before_time = self.state.world_time

        self.assertEqual(material_at(self.state, self.target), "timber")

        first = session.submit(TerrainActionCommand("cut", self.target))

        self.assertEqual(first.result_id, "terrain.damaged")
        self.assertEqual(self.state.world_time, before_time + 1)
        self.assertEqual(self.state.region.terrain_damage[self.coordinate], 2)
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, "T")
        self.assertNotIn(self.coordinate, self.state.region.materials)
        self.assertEqual(self.state.noise, 4)
        self.assertEqual(self.state.sound_events[-1].strength, 4)
        self.assertEqual([type(event) for event in first.events], [TerrainDamaged])
        self.assertEqual((first.events[0].amount, first.events[0].remaining), (2, 1))

        second = session.submit(TerrainActionCommand("cut", self.target))

        self.assertEqual(second.result_id, "terrain.destroyed")
        self.assertEqual(self.state.world_time, before_time + 2)
        self.assertEqual(self.state.noise, 8)
        self.assertNotIn(self.coordinate, self.state.region.terrain_damage)
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, ".")
        self.assertEqual(
            [type(event) for event in second.events],
            [TerrainDamaged, TerrainChanged],
        )
        self.assertEqual((second.events[0].amount, second.events[0].remaining), (1, 0))
        residue = self.state.region.materials[self.coordinate]
        self.assertEqual((residue.material, residue.fuel), ("timber", 4))
        self.assertEqual(
            (residue.support, residue.collapse_due),
            (0, self.state.world_time + 2),
        )
        timber = next(
            item for item in self.state.items
            if item.kind == "commodity:timber" and item.location == "ground"
        )
        self.assertEqual(
            (timber.quantity, timber.region_id, timber.ground_position),
            (1, "hearthford", self.target),
        )

        validate_region(self.state.region)
        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(
                self.state, Path(directory) / "standing-timber.json",
            ))
        restored_timber = next(item for item in restored.items if item.id == timber.id)
        self.assertEqual(terrain_at(restored.region, self.target).glyph, ".")
        self.assertEqual(
            (restored_timber.kind, restored_timber.location,
             restored_timber.region_id, restored_timber.ground_position),
            ("commodity:timber", "ground", "hearthford", self.target),
        )
        restored_residue = restored.region.materials[self.coordinate]
        self.assertEqual(
            (restored_residue.support, restored_residue.collapse_due),
            (residue.support, residue.collapse_due),
        )

    def test_destroyed_standing_timber_uses_delayed_braceable_collapse(self):
        self.state.region.tile_changes[self.coordinate] = "T"
        self.state.weapon = "felling axe"
        session = GameSession(self.state)
        session.submit(TerrainActionCommand("cut", self.target))
        destroyed = session.submit(TerrainActionCommand("cut", self.target))

        cell = self.state.region.materials[self.coordinate]
        self.assertEqual((cell.support, cell.collapse_due), (0, self.state.world_time + 2))
        self.assertFalse(any(isinstance(event, CollapseResolved) for event in destroyed.events))
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, ".")

        braced = copy.deepcopy(self.state)
        braced.gear = "repair tools"
        changed, _ = handle_material(braced, "brace", self.target)
        self.assertTrue(changed)
        braced_cell = braced.region.materials[self.coordinate]
        self.assertEqual((braced_cell.support, braced_cell.collapse_due), (3, 0))
        GameSession(braced).submit(AdvanceWorldCommand(steps=2))
        self.assertEqual(terrain_at(braced.region, self.target).glyph, ".")

        first_wait = session.submit(AdvanceWorldCommand())
        self.assertFalse(any(isinstance(event, CollapseResolved) for event in first_wait.events))
        collapse = session.submit(AdvanceWorldCommand())
        self.assertEqual(
            [type(event) for event in collapse.event_batch.steps[0].events],
            [CollapseResolved, TerrainChanged],
        )
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, "%")
        collapsed = self.state.region.materials[self.coordinate]
        self.assertEqual(
            (collapsed.material, collapsed.support, collapsed.collapse_due),
            ("stone", 3, 0),
        )

    def test_unpacked_yield_remains_on_ground_and_emits_no_resource_reaction(self):
        self.state.region.tile_changes[self.coordinate] = "m"
        self.state.weapon = "spade"
        self.state.auto_place_enabled = False
        self.state.circuits.clear()
        rack_position = Position(39, 25, 0)
        sensor_position = self.state.position
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.state.circuits[rack_key] = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
        )
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor",
            mode="mass", threshold=2,
        )

        outcome = GameSession(self.state).submit(
            TerrainActionCommand("dig", self.target),
        )

        self.assertEqual(outcome.result_id, "terrain.destroyed")
        clay = next(item for item in self.state.items if item.kind == "ingredient:clay")
        self.assertEqual(
            (clay.location, clay.owner_id, clay.region_id, clay.ground_position),
            ("ground", None, "hearthford", self.target),
        )
        self.assertEqual(self.state.circuits[rack_key].charge, 0)
        self.assertIn("remains on the ground", self.state.messages[-1])

    def test_packed_terrain_yield_emits_resource_fact_for_mass_engine(self):
        self.state.region.tile_changes[self.coordinate] = "m"
        self.state.weapon = "spade"
        self.state.circuits.clear()
        rack_position = Position(39, 25, 0)
        sensor_position = self.state.position
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.state.circuits[rack_key] = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
        )
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor",
            mode="mass", threshold=2,
        )

        outcome = GameSession(self.state).submit(
            TerrainActionCommand("dig", self.target),
        )

        self.assertEqual(outcome.result_id, "terrain.destroyed")
        clay = next(
            item for item in self.state.items
            if item.kind == "ingredient:clay" and item.location == "pack"
        )
        self.assertEqual(clay.owner_id, self.state.active_courier_id)
        self.assertEqual(self.state.circuits[rack_key].charge, 1)
        self.assertIn(
            "nearby physical acquisition",
            self.state.circuits[sensor_key].last_event,
        )

    def test_wrong_tool_unsupported_verb_and_authored_position_are_zero_time(self):
        session = GameSession(self.state)
        self.state.weapon = "longbow"
        before = (
            self.state.world_time,
            self.state.noise,
            dict(self.state.region.tile_changes),
            dict(self.state.region.terrain_damage),
            list(self.state.sound_events),
        )
        rejected = session.submit(TerrainActionCommand("cut", self.target))
        self.assertEqual(rejected.result_id, "terrain.rejected")
        self.assertEqual(rejected.event_batch.steps, ())
        self.assertEqual((
            self.state.world_time,
            self.state.noise,
            self.state.region.tile_changes,
            self.state.region.terrain_damage,
            self.state.sound_events,
        ), before)

        self.state.weapon = "billhook"
        rejected = session.submit(TerrainActionCommand("break", self.target))
        self.assertEqual(rejected.result_id, "terrain.rejected")
        self.assertEqual((
            self.state.world_time,
            self.state.noise,
            self.state.region.tile_changes,
            self.state.region.terrain_damage,
            self.state.sound_events,
        ), before)

        changed, _ = handle_material(self.state, "break", self.target)
        self.assertFalse(changed)
        self.assertEqual((
            self.state.world_time,
            self.state.noise,
            self.state.region.tile_changes,
            self.state.region.terrain_damage,
            self.state.sound_events,
        ), before)

        landing = self.state.region.landmarks["landing"]
        self.state.position = Position(landing.x - 1, landing.y, landing.z)
        landing_key = key(landing)
        self.state.region.tile_changes[landing_key] = ";"
        resolved = session.submit(TerrainActionCommand("cut", landing))
        self.assertEqual(resolved.result_id, "terrain.destroyed")
        self.assertEqual(self.state.run.status, "defeat")
        self.assertIn("required route", self.state.run.failure_reason)

    def test_other_terrain_replacement_discards_damage_to_previous_identity(self):
        session = GameSession(self.state)
        session.submit(TerrainActionCommand("cut", self.target))
        self.assertEqual(self.state.region.terrain_damage[self.coordinate], 1)

        replacement = replace_terrain(self.state.region, self.target, "m")

        self.assertEqual(replacement.id, "terrain.region.mud")
        self.assertEqual(terrain_at(self.state.region, self.target).glyph, "m")
        self.assertNotIn(self.coordinate, self.state.region.terrain_damage)

    def test_partial_damage_round_trips_and_old_format_fifteen_defaults_empty(self):
        session = GameSession(self.state)
        session.submit(TerrainActionCommand("cut", self.target))
        validate_region(self.state.region)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "terrain-damage.json"
            save_game(self.state, path)
            restored = load_game(path)
        self.assertEqual(restored.region.terrain_damage, {self.coordinate: 1})
        self.assertEqual(terrain_at(restored.region, self.target).glyph, '"')

        legacy = create_world("format fifteen without terrain damage").to_dict()
        for region in legacy["regions"].values():
            region.pop("terrain_damage", None)
        restored_legacy = game_state_from_dict(legacy)
        self.assertTrue(all(not region.terrain_damage for region in restored_legacy.regions.values()))

    def test_invalid_sparse_damage_is_rejected_by_save_validation(self):
        self.state.region.terrain_damage[self.coordinate] = 2
        with self.assertRaises(RuntimeError):
            validate_region(self.state.region)
        payload = self.state.to_dict()
        with self.assertRaises(StateError):
            game_state_from_dict(payload)

    def test_same_state_and_command_produce_equal_state_and_events(self):
        self.state.region.tile_changes[self.coordinate] = "m"
        self.state.weapon = "spade"
        first_state, second_state = copy.deepcopy(self.state), copy.deepcopy(self.state)
        first = GameSession(first_state).submit(TerrainActionCommand("dig", self.target))
        second = GameSession(second_state).submit(TerrainActionCommand("dig", self.target))
        self.assertEqual(first, second)
        self.assertEqual(first_state.to_dict(), second_state.to_dict())

    def test_malformed_command_is_rejected_without_state_change(self):
        session = GameSession(self.state)
        before = self.state.to_dict()
        outcome = session.submit(TerrainActionCommand("explode", self.target))
        self.assertEqual(outcome.result_id, "terrain.invalid")
        self.assertEqual(self.state.to_dict(), before)


if __name__ == "__main__":
    unittest.main()
