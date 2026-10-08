from __future__ import annotations

import copy
import tempfile
import unittest
from pathlib import Path

from roag.circuits import cell_key, operate, place
from roag.commands import AttackCommand
from roag.engine_components import registered_reaction_rules, resolve_actor_defeat
from roag.inventory import auto_place, create_item, item_count
from roag.production import RECIPES, make
from roag.regions import begin_region
from roag.runtime_events import ActorDefeated
from roag.save import load_game, save_game
from roag.session import GameSession
from roag.state import CircuitCell, Position, Threat, create_world


class EngineComponentTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("engine component vertical slice")
        begin_region(self.state, "hearthford")
        self.state.weather = "clear"
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        self.state.circuits.clear()
        self.state.position = Position(40, 25, 0)
        for y in range(22, 28):
            for x in range(36, 44):
                self.state.region.tile_changes[f"{x},{y},0"] = "."
        self.rack_position = Position(39, 24, 0)
        self.sensor_position = Position(40, 24, 0)
        self.rack_key = cell_key("region:hearthford", self.rack_position, "surface")
        self.sensor_key = cell_key("region:hearthford", self.sensor_position, "surface")

    def fit_engine(self, *, mode: str = "threat", connected: bool = True):
        rack_position = self.rack_position if connected else Position(36, 22, 0)
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        rack = CircuitCell("region:hearthford", rack_position, "surface", "rack")
        sensor = CircuitCell(
            "region:hearthford", self.sensor_position, "surface", "sensor",
            mode=mode, threshold=2,
        )
        self.state.circuits[rack_key] = rack
        self.state.circuits[self.sensor_key] = sensor
        return rack, sensor

    def target(self, position: Position = Position(41, 25, 0)) -> Threat:
        target = Threat(
            "engine-target", "engine target", "pursuer", position, 1, 1,
            status="engaged", morale=8,
        )
        self.state.threats.append(target)
        return target

    def test_physical_recipe_placement_configuration_and_combat_reaction(self):
        mill = self.state.region.landmarks["mill"]
        self.state.position = mill
        resources = (
            ("commodity:ironwork", 2),
            ("commodity:timber", 1),
            ("commodity:paper", 1),
        )
        for kind, quantity in resources:
            item = create_item(self.state, kind, "engine test stock", quantity=quantity)
            self.assertTrue(auto_place(
                self.state, item.id, "pack", owner_id=self.state.active_courier_id,
            ))
        self.assertTrue(make(self.state, "circuit-rack")[0])
        self.assertTrue(make(self.state, "circuit-sensor")[0])
        self.assertEqual(RECIPES["circuit-rack"].output, "circuit:rack")
        self.assertEqual(RECIPES["circuit-sensor"].output, "circuit:sensor")
        self.assertEqual(item_count(self.state, "rack"), 1)
        self.assertEqual(item_count(self.state, "sensor"), 1)

        self.state.position = Position(40, 25, 0)
        self.assertTrue(place(self.state, self.rack_position, "surface", "rack")[0])
        self.assertTrue(place(self.state, self.sensor_position, "surface", "sensor")[0])
        self.assertTrue(operate(self.state, self.sensor_position, "surface")[0])
        self.assertTrue(operate(self.state, self.sensor_position, "surface")[0])
        self.assertEqual(self.state.circuits[self.sensor_key].mode, "threat")
        self.assertEqual(len(registered_reaction_rules(self.state)), 1)

        target = self.target()
        self.state.weapon = "hand axe"
        outcome = GameSession(self.state).submit(AttackCommand(target.id))

        self.assertEqual(target.status, "defeated")
        self.assertEqual(self.state.circuits[self.rack_key].charge, 1)
        self.assertTrue(any(
            isinstance(event, ActorDefeated) for event in outcome.events
        ))
        self.assertIn("recovers 1 rack charge", self.state.circuits[self.sensor_key].last_event)
        self.assertIn(
            self.state.circuits[self.sensor_key].last_event,
            self.state.messages,
        )

    def test_registration_requires_threat_mode_and_physical_rack_connection(self):
        rack, _ = self.fit_engine(mode="mass")
        target = self.target()
        resolve_actor_defeat(
            self.state, target.id, self.state.active_courier_id, target.position,
        )
        self.assertEqual(rack.charge, 0)

        self.state.circuits.clear()
        rack, _ = self.fit_engine(connected=False)
        self.assertEqual(registered_reaction_rules(self.state), ())
        resolve_actor_defeat(
            self.state, target.id, self.state.active_courier_id, target.position,
        )
        self.assertEqual(rack.charge, 0)

    def test_defeat_must_be_in_sensor_range_and_on_its_level(self):
        rack, _ = self.fit_engine()
        for position in (Position(43, 24, 0), Position(40, 24, 1)):
            result = resolve_actor_defeat(
                self.state, "remote", self.state.active_courier_id, position,
            )
            self.assertEqual(result.resolution.applications, ())
        self.assertEqual(rack.charge, 0)

    def test_multiple_sensors_stack_deterministically_but_rack_capacity_is_bounded(self):
        rack, _ = self.fit_engine()
        second_position = Position(39, 25, 0)
        second_key = cell_key("region:hearthford", second_position, "surface")
        self.state.circuits[second_key] = CircuitCell(
            "region:hearthford", second_position, "surface", "sensor",
            mode="threat", threshold=2,
        )
        target = self.target()
        result = resolve_actor_defeat(
            self.state, target.id, self.state.active_courier_id, target.position,
        )
        self.assertEqual(rack.charge, 2)
        self.assertEqual(len(result.resolution.applications), 2)
        self.assertEqual(
            [row.rule_id for row in result.resolution.applications],
            sorted(row.rule_id for row in result.resolution.applications),
        )

        rack.charge = 48
        full = resolve_actor_defeat(
            self.state, "another", self.state.active_courier_id, target.position,
        )
        self.assertEqual(full.resolution.applications, ())
        self.assertEqual(rack.charge, 48)

    def test_non_courier_damage_does_not_synthesize_a_player_defeat_fact(self):
        from roag.enemy_equipment import harm_enemy

        rack, _ = self.fit_engine()
        target = self.target()
        harm_enemy(self.state, target, 2, "environmental collapse")
        self.assertTrue(target.health == 0)
        self.assertEqual(rack.charge, 0)

    def test_same_state_and_attack_are_deterministic_and_charge_round_trips(self):
        self.fit_engine()
        target = self.target()
        self.state.weapon = "hand axe"
        first, second = copy.deepcopy(self.state), copy.deepcopy(self.state)

        first_outcome = GameSession(first).submit(AttackCommand(target.id))
        second_outcome = GameSession(second).submit(AttackCommand(target.id))

        self.assertEqual(first.to_dict(), second.to_dict())
        self.assertEqual(first_outcome, second_outcome)
        with tempfile.TemporaryDirectory() as directory:
            path = save_game(first, Path(directory) / "engine.json")
            restored = load_game(path)
        self.assertEqual(restored.circuits[self.rack_key].charge, 1)
        payload = restored.to_dict()
        self.assertNotIn("simulation_facts", payload)
        self.assertNotIn("reaction_rules", payload)


if __name__ == "__main__":
    unittest.main()
