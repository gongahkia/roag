from __future__ import annotations

import copy
import tempfile
import unittest
from pathlib import Path

from roag.circuits import CELL_CHARGE, cell_key, item_count, operate, place
from roag.commands import AttackCommand, TerrainActionCommand
from roag.engine_components import registered_reaction_rules, resolve_actor_defeat
from roag.inventory import auto_place, create_item
from roag.production import RECIPES, gather, input_count, make, site_position
from roag.regions import begin_region
from roag.runtime_events import ActorDefeated, TerrainChanged, TerrainDamaged
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

    def test_defeat_charge_requires_threat_mode_and_physical_rack_connection(self):
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

    def test_registration_ignores_fitted_engines_in_inactive_spaces(self):
        self.fit_engine()
        remote_sensor = CircuitCell(
            "region:greywash", self.sensor_position, "surface", "sensor",
            mode="threat", threshold=2,
        )
        remote_rack = CircuitCell(
            "region:greywash", self.rack_position, "surface", "rack",
        )
        self.state.circuits[
            cell_key(remote_sensor.space, remote_sensor.position, remote_sensor.layer)
        ] = remote_sensor
        self.state.circuits[
            cell_key(remote_rack.space, remote_rack.position, remote_rack.layer)
        ] = remote_rack

        rules = registered_reaction_rules(self.state)

        self.assertEqual(len(rules), 1)
        self.assertEqual(rules[0].source.instance_id, self.sensor_key)

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

    def test_kill_charge_powers_noisier_harvest_and_yield_recovers_charge(self):
        rack, _ = self.fit_engine()
        mass_position = Position(40, 25, 0)
        mass_key = cell_key("region:hearthford", mass_position, "surface")
        self.state.circuits[mass_key] = CircuitCell(
            "region:hearthford", mass_position, "surface", "sensor",
            mode="mass", threshold=2,
        )
        target = self.target()
        self.state.weapon = "hand axe"
        session = GameSession(self.state)

        session.submit(AttackCommand(target.id))
        self.assertEqual(rack.charge, 1)

        terrain_target = Position(41, 25, 0)
        terrain_key = f"{terrain_target.x},{terrain_target.y},{terrain_target.z}"
        self.state.region.tile_changes[terrain_key] = '"'
        noise_before = self.state.noise
        outcome = session.submit(TerrainActionCommand("cut", terrain_target))

        self.assertEqual(outcome.result_id, "terrain.destroyed")
        self.assertEqual(rack.charge, 1)
        self.assertEqual(self.state.noise, noise_before + 3)
        self.assertEqual(
            [type(event) for event in outcome.events],
            [TerrainDamaged, TerrainChanged],
        )
        self.assertEqual(outcome.events[0].amount, 2)
        self.assertTrue(any(
            "spends 1 rack charge" in message for message in self.state.messages
        ))
        self.assertIn(
            "nearby physical acquisition",
            self.state.circuits[mass_key].last_event,
        )
        self.assertTrue(any(
            item.kind == "material:reeds"
            and item.location == "pack"
            and item.quantity == 2
            for item in self.state.items
        ))

    def test_physical_gather_charges_mass_engine_and_resource_remains_recipe_input(self):
        self.state.circuits.clear()
        self.state.position = site_position(self.state)
        sensor_position = self.state.position
        rack_position = Position(sensor_position.x - 1, sensor_position.y, sensor_position.z)
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.state.circuits[rack_key] = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
        )
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor",
            mode="mass", threshold=2,
        )
        self.assertEqual(
            {rule.trigger_id for rule in registered_reaction_rules(self.state)},
            {"resource.gained", "terrain.action"},
        )
        time_before = self.state.world_time

        changed, message = gather(self.state, 0)

        self.assertTrue(changed, message)
        self.assertEqual(self.state.world_time, time_before + 1)
        self.assertEqual(input_count(self.state, "ingredient:iron filings"), 1)
        self.assertEqual(self.state.circuits[rack_key].charge, 1)
        self.assertIn(
            "nearby physical acquisition",
            self.state.circuits[sensor_key].last_event,
        )

        spark_salt = create_item(
            self.state, "ingredient:spark salt", "engine test stock",
        )
        self.assertTrue(auto_place(
            self.state, spark_salt.id, "pack", owner_id=self.state.active_courier_id,
        ))
        self.assertTrue(make(self.state, "thunder-bombs")[0])
        self.assertEqual(input_count(self.state, "ingredient:iron filings"), 0)
        self.assertTrue(any(
            item.kind == "consumable:thunder bombs"
            and item.location == "pack"
            and item.owner_id == self.state.active_courier_id
            for item in self.state.items
        ))

    def test_failed_gather_does_not_trigger_resource_reaction(self):
        from roag.production import initialise_production

        self.state.circuits.clear()
        self.state.position = site_position(self.state)
        sensor_position = self.state.position
        rack_position = Position(sensor_position.x - 1, sensor_position.y, sensor_position.z)
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        rack = CircuitCell("region:hearthford", rack_position, "surface", "rack")
        self.state.circuits[rack_key] = rack
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor", mode="mass",
        )
        initialise_production(self.state)
        self.state.production["sites"]["hearthford"]["stock"] = 0

        changed, _ = gather(self.state, 0)

        self.assertFalse(changed)
        self.assertEqual(rack.charge, 0)

    def test_resource_reaction_is_deterministic_and_only_existing_charge_persists(self):
        self.state.circuits.clear()
        self.state.position = site_position(self.state)
        sensor_position = self.state.position
        rack_position = Position(sensor_position.x - 1, sensor_position.y, sensor_position.z)
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.state.circuits[rack_key] = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
        )
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor", mode="mass",
        )
        first, second = copy.deepcopy(self.state), copy.deepcopy(self.state)

        self.assertEqual(gather(first, 0), gather(second, 0))
        self.assertEqual(first.to_dict(), second.to_dict())
        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(first, Path(directory) / "resource-engine.json"))
        self.assertEqual(restored.circuits[rack_key].charge, 1)
        payload = restored.to_dict()
        self.assertNotIn("simulation_facts", payload)
        self.assertNotIn("resource_gained", payload)

    def test_supply_sensor_atomically_loads_a_newly_crafted_galvanic_cell(self):
        self.state.circuits.clear()
        self.state.position = site_position(self.state)
        sensor_position = self.state.position
        rack_position = Position(
            sensor_position.x - 1, sensor_position.y, sensor_position.z,
        )
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.state.circuits[rack_key] = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
        )
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor",
            mode="supply", threshold=2,
        )
        for kind, quantity in RECIPES["circuit-cell"].inputs:
            item = create_item(
                self.state, kind, "supply sensor test stock", quantity=quantity,
            )
            self.assertTrue(auto_place(
                self.state, item.id, "pack",
                owner_id=self.state.active_courier_id,
            ))
        mirror = copy.deepcopy(self.state)
        time_before = self.state.world_time

        changed, message = make(self.state, "circuit-cell")
        mirror_result = make(mirror, "circuit-cell")

        self.assertTrue(changed, message)
        self.assertEqual((changed, message), mirror_result)
        self.assertEqual(self.state.to_dict(), mirror.to_dict())
        self.assertEqual(self.state.world_time, time_before + 2)
        self.assertEqual(item_count(self.state, "cell"), 0)
        self.assertEqual(self.state.circuits[rack_key].charge, CELL_CHARGE)
        self.assertIn(
            "loads a galvanic cell",
            self.state.circuits[sensor_key].last_event,
        )
        self.assertIn(
            self.state.circuits[sensor_key].last_event, self.state.messages,
        )
        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(
                self.state, Path(directory) / "supply-engine.json",
            ))
        self.assertEqual(restored.circuits[rack_key].charge, CELL_CHARGE)
        self.assertEqual(item_count(restored, "cell"), 0)
        payload = restored.to_dict()
        self.assertNotIn("simulation_facts", payload)
        self.assertNotIn("effect_applications", payload)

    def test_supply_sensor_preserves_cell_when_rack_cannot_accept_full_yield(self):
        self.state.circuits.clear()
        self.state.position = site_position(self.state)
        sensor_position = self.state.position
        rack_position = Position(
            sensor_position.x - 1, sensor_position.y, sensor_position.z,
        )
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        rack = CircuitCell(
            "region:hearthford", rack_position, "surface", "rack",
            charge=CELL_CHARGE + 1,
        )
        self.state.circuits[rack_key] = rack
        self.state.circuits[sensor_key] = CircuitCell(
            "region:hearthford", sensor_position, "surface", "sensor",
            mode="supply", threshold=2,
        )
        for kind, quantity in RECIPES["circuit-cell"].inputs:
            item = create_item(
                self.state, kind, "supply sensor test stock", quantity=quantity,
            )
            self.assertTrue(auto_place(
                self.state, item.id, "pack",
                owner_id=self.state.active_courier_id,
            ))

        changed, message = make(self.state, "circuit-cell")

        self.assertTrue(changed, message)
        self.assertEqual(rack.charge, CELL_CHARGE + 1)
        self.assertEqual(item_count(self.state, "cell"), 1)
        self.assertFalse(any(
            "loads a galvanic cell" in entry for entry in self.state.messages
        ))

    def test_terrain_assistance_requires_range_charge_and_a_valid_base_tool(self):
        rack, _ = self.fit_engine(mode="mass")
        rack.charge = 2
        self.state.position = Position(41, 25, 0)
        terrain_target = Position(42, 25, 0)
        terrain_key = f"{terrain_target.x},{terrain_target.y},{terrain_target.z}"
        self.state.region.tile_changes[terrain_key] = '"'
        session = GameSession(self.state)

        self.state.weapon = "longbow"
        rejected = session.submit(TerrainActionCommand("cut", terrain_target))
        self.assertEqual(rejected.result_id, "terrain.rejected")
        self.assertEqual(rack.charge, 2)

        self.state.weapon = "hand axe"
        self.state.circuits[self.sensor_key].threshold = 1
        accepted = session.submit(TerrainActionCommand("cut", terrain_target))
        self.assertEqual(accepted.result_id, "terrain.damaged")
        self.assertEqual(rack.charge, 2)
        self.assertEqual(accepted.events[0].amount, 1)

    def test_terrain_assistance_spends_only_charge_that_changes_the_result(self):
        rack, _ = self.fit_engine(mode="mass")
        second_position = Position(40, 25, 0)
        second_key = cell_key("region:hearthford", second_position, "surface")
        self.state.circuits[second_key] = CircuitCell(
            "region:hearthford", second_position, "surface", "sensor",
            mode="mass", threshold=2,
        )
        rack.charge = 2
        self.state.auto_place_enabled = False
        terrain_target = Position(41, 25, 0)
        terrain_key = f"{terrain_target.x},{terrain_target.y},{terrain_target.z}"
        self.state.region.tile_changes[terrain_key] = '"'
        self.state.weapon = "hand axe"
        session = GameSession(self.state)

        powered = session.submit(TerrainActionCommand("cut", terrain_target))

        self.assertEqual(powered.result_id, "terrain.destroyed")
        self.assertEqual(powered.events[0].amount, 2)
        self.assertEqual(rack.charge, 1)

        self.state.region.tile_changes[terrain_key] = ";"
        unneeded = session.submit(TerrainActionCommand("cut", terrain_target))
        self.assertEqual(unneeded.result_id, "terrain.destroyed")
        self.assertEqual(unneeded.events[0].amount, 1)
        self.assertEqual(rack.charge, 1)

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

    def test_charged_terrain_action_is_deterministic_and_persists_only_state(self):
        rack, _ = self.fit_engine(mode="mass")
        rack.charge = 1
        terrain_target = Position(41, 25, 0)
        terrain_key = f"{terrain_target.x},{terrain_target.y},{terrain_target.z}"
        self.state.region.tile_changes[terrain_key] = '"'
        self.state.weapon = "hand axe"
        first, second = copy.deepcopy(self.state), copy.deepcopy(self.state)

        first_outcome = GameSession(first).submit(
            TerrainActionCommand("cut", terrain_target),
        )
        second_outcome = GameSession(second).submit(
            TerrainActionCommand("cut", terrain_target),
        )

        self.assertEqual(first_outcome, second_outcome)
        self.assertEqual(first.to_dict(), second.to_dict())
        self.assertEqual(first.circuits[self.rack_key].charge, 1)
        with tempfile.TemporaryDirectory() as directory:
            path = save_game(first, Path(directory) / "terrain-engine.json")
            restored = load_game(path)
        self.assertEqual(restored.circuits[self.rack_key].charge, 1)
        self.assertEqual(restored.region.tile_changes[terrain_key], ".")
        self.assertTrue(any(
            item.kind == "material:reeds"
            and item.location == "pack"
            and item.quantity == 2
            for item in restored.items
        ))
        payload = restored.to_dict()
        self.assertNotIn("simulation_facts", payload)
        self.assertNotIn("reaction_rules", payload)


if __name__ == "__main__":
    unittest.main()
