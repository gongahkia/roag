from __future__ import annotations

import copy
import tempfile
import unittest
from dataclasses import dataclass
from pathlib import Path

from roag.circuits import cell_key
from roag.commands import SetAutoPlaceCommand, TerrainActionCommand
from roag.danger import pressure
from roag.inventory import create_item, sync_legacy_load
from roag.regions import begin_region
from roag.runtime_events import CollapseResolved, ThreatSpawned
from roag.save import load_game, save_game
from roag.session import CommandOutcome, GameSession
from roag.state import CircuitCell, Position, create_world, validate_state
from roag.terrain import terrain_at


PLAYER = Position(40, 25, 0)
RACK = Position(39, 24, 0)
SENSOR = Position(40, 24, 0)
REEDS = Position(39, 25, 0)
TIMBERS = (
    Position(41, 25, 0),
    Position(41, 24, 0),
    Position(41, 26, 0),
)


@dataclass(frozen=True)
class FieldLoopCheckpoint:
    outcomes: tuple[CommandOutcome, ...]
    acquisition_event: str
    charge_after_acquisition: int


def coordinate(point: Position) -> str:
    return f"{point.x},{point.y},{point.z}"


def prepare_field_loop(seed: str):
    state = create_world(seed)
    begin_region(state, "hearthford")
    state.weather = "clear"
    state.threats.clear()
    state.region_threats["hearthford"] = state.threats
    state.circuits.clear()
    state.position = PLAYER
    for item in state.items:
        if (
            item.owner_id == state.active_courier_id
            and item.location == "readied"
        ):
            item.location, item.owner_id = "lost", None
    create_item(
        state,
        "felling axe",
        "integrated field-loop fixture",
        location="readied",
        owner_id=state.active_courier_id,
    )
    sync_legacy_load(state)
    for y in range(22, 29):
        for x in range(35, 46):
            state.region.tile_changes[f"{x},{y},0"] = "."
    for point in (REEDS, *TIMBERS):
        state.region.materials.pop(coordinate(point), None)
        state.region.terrain_damage.pop(coordinate(point), None)
    state.region.tile_changes[coordinate(REEDS)] = '"'
    for point in TIMBERS:
        state.region.tile_changes[coordinate(point)] = "T"

    rack_key = cell_key("region:hearthford", RACK, "surface")
    sensor_key = cell_key("region:hearthford", SENSOR, "surface")
    state.circuits[rack_key] = CircuitCell(
        "region:hearthford", RACK, "surface", "rack",
    )
    state.circuits[sensor_key] = CircuitCell(
        "region:hearthford", SENSOR, "surface", "sensor",
        mode="mass", threshold=2,
    )
    return state, rack_key, sensor_key


def run_to_checkpoint(state):
    session = GameSession(state)
    reeds = session.submit(TerrainActionCommand("cut", REEDS))
    rack_key = cell_key("region:hearthford", RACK, "surface")
    sensor_key = cell_key("region:hearthford", SENSOR, "surface")
    acquisition_event = state.circuits[sensor_key].last_event
    charge_after_acquisition = state.circuits[rack_key].charge
    automatic_placement = session.submit(SetAutoPlaceCommand(False))

    first_timber = session.submit(TerrainActionCommand("cut", TIMBERS[0]))
    second_timber_hit = session.submit(TerrainActionCommand("cut", TIMBERS[1]))
    second_timber_break = session.submit(TerrainActionCommand("cut", TIMBERS[1]))
    return FieldLoopCheckpoint(
        (
            reeds,
            automatic_placement,
            first_timber,
            second_timber_hit,
            second_timber_break,
        ),
        acquisition_event,
        charge_after_acquisition,
    )


def finish_field_loop(state):
    session = GameSession(state)
    health_before = state.courier.health
    ids_before = {actor.id for actor in state.threats}
    warning = session.submit(TerrainActionCommand("cut", TIMBERS[2]))
    spawned = tuple(actor for actor in state.threats if actor.id not in ids_before)
    turns_after_arrival = tuple(actor.turn for actor in spawned)
    health_after_arrival = state.courier.health
    completion = session.submit(TerrainActionCommand("cut", TIMBERS[2]))
    return (
        warning,
        completion,
        spawned,
        turns_after_arrival,
        health_before,
        health_after_arrival,
    )


class GameplayRedesignLoopTests(unittest.TestCase):
    def test_integrated_field_loop_is_deterministic_and_saveable(self):
        state, rack_key, sensor_key = prepare_field_loop(
            "gameplay redesign integrated field loop",
        )
        mirror = copy.deepcopy(state)

        self.assertEqual(state.location, "region")
        self.assertEqual(state.current_room, "hearthford")
        self.assertEqual((state.world_time, state.expedition_count), (0, 1))
        self.assertTrue(state.region.seen)
        self.assertEqual(state.to_dict(), mirror.to_dict())
        pressure_before = pressure(state)

        outcomes = run_to_checkpoint(state)
        mirror_outcomes = run_to_checkpoint(mirror)

        self.assertEqual(outcomes, mirror_outcomes)
        self.assertEqual(state.to_dict(), mirror.to_dict())
        reeds, placement, first_timber, first_hit, second_timber = outcomes.outcomes
        self.assertEqual(reeds.result_id, "terrain.destroyed")
        self.assertFalse(placement.time_advanced)
        self.assertEqual(state.world_time, 4)
        self.assertEqual(state.noise, 15)
        self.assertGreater(pressure(state).score, pressure_before.score)
        self.assertEqual((pressure_before.score, pressure(state).score), (2, 9))
        self.assertEqual(pressure(state).band, "steady")
        self.assertTrue(any(
            item.kind == "material:reeds"
            and item.location == "pack"
            and item.owner_id == state.active_courier_id
            for item in state.items
        ))
        self.assertEqual(state.circuits[rack_key].charge, 0)
        self.assertEqual(outcomes.charge_after_acquisition, 1)
        self.assertIn("nearby physical acquisition", outcomes.acquisition_event)
        self.assertIn(
            "spends 1 rack charge",
            state.circuits[sensor_key].last_event,
        )
        self.assertEqual(first_timber.events[0].amount, 3)
        self.assertEqual(first_hit.result_id, "terrain.damaged")
        self.assertEqual(second_timber.result_id, "terrain.destroyed")
        self.assertEqual(terrain_at(state.region, TIMBERS[0]).glyph, "%")
        self.assertTrue(any(
            isinstance(event, CollapseResolved)
            for event in second_timber.event_batch.steps[0].events
        ))
        self.assertEqual(
            sum(
                item.quantity for item in state.items
                if item.kind == "commodity:timber" and item.location == "ground"
            ),
            2,
        )

        with tempfile.TemporaryDirectory() as directory:
            path = save_game(
                state, Path(directory) / "gameplay-redesign-loop.json",
            )
            raw_save = path.read_text(encoding="utf-8")
            restored = load_game(path)
        self.assertNotIn("event_batch", raw_save)
        self.assertNotIn("simulation_facts", raw_save)
        self.assertEqual(restored.to_dict(), state.to_dict())

        continuation = finish_field_loop(state)
        restored_continuation = finish_field_loop(restored)

        self.assertEqual(continuation, restored_continuation)
        (
            warning,
            completion,
            spawned,
            turns_after_arrival,
            health_before,
            health_after_arrival,
        ) = continuation
        self.assertEqual(state.to_dict(), restored.to_dict())
        self.assertEqual(pressure(state).score, 13)
        self.assertEqual(pressure(state).band, "strained")
        self.assertEqual(len(spawned), 2)
        self.assertEqual(turns_after_arrival, (0, 0))
        self.assertEqual(health_after_arrival, health_before)
        self.assertEqual(
            sum(
                isinstance(event, ThreatSpawned)
                for event in warning.event_batch.steps[0].events
            ),
            2,
        )
        self.assertTrue(all(actor.turn == 1 for actor in spawned))
        self.assertEqual(completion.result_id, "terrain.destroyed")
        self.assertTrue(any(
            isinstance(event, CollapseResolved)
            for event in completion.event_batch.steps[0].events
        ))
        self.assertEqual(terrain_at(state.region, TIMBERS[1]).glyph, "%")
        self.assertEqual(terrain_at(state.region, TIMBERS[2]).glyph, ".")
        validate_state(state)


if __name__ == "__main__":
    unittest.main()
