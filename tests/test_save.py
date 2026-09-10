from __future__ import annotations

import tempfile
from dataclasses import replace
import unittest
from pathlib import Path
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.save import SaveError, default_save_path, read_save, write_save


class AtomicSaveTests(unittest.TestCase):
    def test_failed_replace_preserves_previous_save_and_cleans_temporary(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, {"version": 1})
            with patch("dumbest_dungeon.save.os.replace", side_effect=OSError("disk fault")):
                with self.assertRaisesRegex(SaveError, "disk fault"):
                    write_save(path, {"version": 2})
            self.assertEqual({"version": 1}, read_save(path))
            self.assertEqual([path], list(path.parent.iterdir()))

    def test_failed_file_sync_preserves_previous_save(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, {"version": 1})
            with patch("dumbest_dungeon.save.os.fsync", side_effect=OSError("sync fault")):
                with self.assertRaisesRegex(SaveError, "sync fault"):
                    write_save(path, {"version": 2})
            self.assertEqual({"version": 1}, read_save(path))
            self.assertEqual([path], list(path.parent.iterdir()))

    def test_invalid_data_does_not_touch_previous_save(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, {"version": 1})
            for value in (float("nan"), float("inf"), object()):
                with self.assertRaises(SaveError):
                    write_save(path, {"value": value})
            self.assertEqual({"version": 1}, read_save(path))
            self.assertEqual([path], list(path.parent.iterdir()))

    def test_file_and_directory_are_synced(self) -> None:
        import os

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            with patch("dumbest_dungeon.save.os.fsync", wraps=os.fsync) as sync:
                write_save(path, {"version": 1})
            self.assertEqual(2, sync.call_count)
            self.assertEqual(0o600, path.stat().st_mode & 0o777)

    def test_strict_save_input_rejects_ambiguous_or_nonfinite_data(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            for encoded in ('{"version":1,"version":2}', '{"value":NaN}', '{"value":1e999}', '[]'):
                path.write_text(encoded, encoding="utf-8")
                with self.assertRaises(SaveError):
                    read_save(path)
            path.write_bytes(b"\xff")
            with self.assertRaises(SaveError):
                read_save(path)


class SaveTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()

    @staticmethod
    def complete_objective(engine: GameEngine, objective) -> None:
        approach = engine.mission_definition(objective.biome_id)["approaches"][0]
        engine.state.party_x, engine.state.party_y = objective.x, objective.y
        engine._resolve_exploration_tile()
        engine.begin_objective(approach["id"])
        while not objective.completed:
            engine.state.party_x, engine.state.party_y = engine.objective_position(objective)
            engine._resolve_exploration_tile()
            engine.advance_objective()
        if engine.state.phase == "combat" and engine.state.combat_kind == "guardian":
            for enemy in engine.state.enemies:
                enemy.hp = 0
            engine._combat_victory()
            engine.choose_reward(None)

    def test_default_save_path_uses_public_title_slug(self) -> None:
        with patch.dict("os.environ", {"XDG_STATE_HOME": "/tmp/dullest-state"}):
            self.assertEqual(
                Path("/tmp/dullest-state/dullest-dungeon/run.save.json"),
                default_save_path(),
            )

    def test_exploration_save_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 101)
        hero = engine.living_heroes()[0]
        engine.acquire_boon(hero.id, "iron_benediction")
        engine.acquire_curse(hero.id, "static_prayer")
        engine.acquire_item("survey_relay", 2)
        first_step = engine._find_path(
            (engine.state.party_x, engine.state.party_y),
            engine.room_position(1),
        )[0]
        engine.step_exploration(*first_step)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_pressure_state_is_required_and_strictly_validated(self) -> None:
        engine = GameEngine.new(self.catalog, 101)
        snapshot = engine.snapshot()
        self.assertEqual((0, [], None), (snapshot["state"]["pressure"],
                                        snapshot["state"]["pressure_recent"],
                                        snapshot["state"]["pressure_incomplete_before_tick"]))
        for field in ("pressure", "pressure_recent", "pressure_incomplete_before_tick"):
            broken = engine.snapshot()
            del broken["state"][field]
            with self.assertRaises(RuleError):
                GameEngine.from_snapshot(self.catalog, broken)
        broken = engine.snapshot()
        broken["state"]["pressure"] = -1
        with self.assertRaisesRegex(RuleError, "expedition pressure"):
            GameEngine.from_snapshot(self.catalog, broken)

    def test_frozen_encounter_director_is_required_and_phase_bound(self) -> None:
        engine = GameEngine.new(self.catalog, 102)
        self.assertIsNone(engine.state.encounter_pressure)
        engine.state.pressure = 480
        engine.start_combat("lost_shift")
        self.assertEqual(480, engine.state.encounter_pressure)
        restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(engine.snapshot(), restored.snapshot())
        broken = engine.snapshot()
        broken["state"]["encounter_pressure"] = None
        with self.assertRaisesRegex(RuleError, "frozen encounter director"):
            GameEngine.from_snapshot(self.catalog, broken)
        broken = engine.snapshot()
        broken["state"]["encounter_modules"] = ["base:missing"]
        with self.assertRaisesRegex(RuleError, "frozen encounter director"):
            GameEngine.from_snapshot(self.catalog, broken)

    def test_exploration_knowledge_round_trips(self) -> None:
        engine = GameEngine.new(self.catalog, 111)
        pickup = next(item for item in engine.state.pickups if not item.hidden)
        engine.state.party_x, engine.state.party_y = pickup.x, pickup.y
        engine._update_perception()
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertTrue(loaded.feature_is_known(pickup.id))
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_hub_selection_save_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 100, start_in_hub=True)
        engine.toggle_hub_crew("warden")
        engine.toggle_hub_crew("breacher")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "hub.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        self.assertEqual("hub", loaded.state.phase)
        self.assertEqual(engine.state.hub_selection, loaded.state.hub_selection)
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_tutorial_exploration_round_trip(self) -> None:
        engine = GameEngine.tutorial(self.catalog)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "tutorial.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        self.assertTrue(loaded.state.tutorial)
        self.assertEqual(engine.tutorial_destination(), loaded.tutorial_destination())
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_tutorial_combat_round_trip(self) -> None:
        engine = GameEngine.tutorial(self.catalog)
        engine.advance_tutorial(0, 1)
        destination = engine.tutorial_destination()
        for step in engine.path_to(*destination):
            engine.step_exploration(*step)
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(2, loaded.state.tutorial_stage)
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_hazard_and_objective_state_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 102)
        hazard = engine.state.hazards[0]
        engine.state.party_x, engine.state.party_y = hazard.x, hazard.y
        engine._resolve_exploration_tile()
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual("hazard", loaded.state.phase)
        self.assertEqual(hazard.id, loaded.current_hazard().id)
        loaded.finish_hazard()
        objective = loaded.state.objectives[0]
        loaded.state.party_x, loaded.state.party_y = objective.x, objective.y
        loaded._resolve_exploration_tile()
        reloaded = GameEngine.from_snapshot(self.catalog, loaded.snapshot())
        self.assertEqual("objective", reloaded.state.phase)
        self.assertEqual(objective.id, reloaded.current_objective().id)
        self.assertEqual(loaded.state.landmarks, reloaded.state.landmarks)

    def test_partial_multistage_objective_round_trips(self) -> None:
        engine = GameEngine.new(self.catalog, 112)
        objective = engine.state.objectives[0]
        approach = engine.mission_definition(objective.biome_id)["approaches"][0]
        engine.state.party_x, engine.state.party_y = objective.x, objective.y
        engine._resolve_exploration_tile()
        engine.begin_objective(approach["id"])
        engine.state.party_x, engine.state.party_y = engine.objective_position(objective)
        engine._resolve_exploration_tile()
        engine.advance_objective()
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        restored = loaded.state.objectives[0]
        self.assertEqual(approach["id"], restored.approach)
        self.assertEqual(1, restored.stage)
        self.assertFalse(restored.completed)
        self.assertEqual(engine.objective_position(objective), loaded.objective_position(restored))

    def test_open_core_and_depleted_resources_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 114)
        engine.state.supplies = 99
        for objective in engine.state.objectives[: engine.state.required_objectives]:
            self.complete_objective(engine, objective)
        engine.state.light = 0
        engine.state.supplies = 0
        projection = engine.core_route_projection()

        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertTrue(loaded.boss_unlocked())
        self.assertTrue(loaded.core_patrol().active)
        self.assertEqual((0, 0), (loaded.state.light, loaded.state.supplies))
        self.assertEqual(projection, loaded.core_route_projection())
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_pending_facility_choice_round_trips(self) -> None:
        engine = GameEngine.new(self.catalog, 113)
        facility = engine.state.facilities[0]
        engine.state.party_x, engine.state.party_y = facility.x, facility.y
        engine._resolve_exploration_tile()
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual("facility", loaded.state.phase)
        self.assertEqual(facility.id, loaded.current_facility().id)

    def test_mid_combat_save_preserves_random_stream(self) -> None:
        engine = GameEngine.new(self.catalog, 202)
        patrol = engine.state.patrols[0]
        patrol.x, patrol.y = engine._neighbors((engine.state.party_x, engine.state.party_y))[0]
        engine.step_exploration(patrol.x, patrol.y)
        self.assertEqual(patrol.id, engine.state.active_patrol_id)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        engine.end_turn()
        loaded.end_turn()
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_pending_reward_choices_round_trip_exactly(self) -> None:
        engine = GameEngine.new(self.catalog, 204)
        engine.state.rewards = engine._generate_card_rewards(4)
        engine.state.phase = "reward"
        contexts = [engine.reward_context(card_id) for card_id in engine.state.rewards]
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(engine.state.rewards, loaded.state.rewards)
        self.assertEqual(contexts, [loaded.reward_context(card_id) for card_id in loaded.state.rewards])
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_dead_crew_and_cleaned_deck_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 203)
        engine.start_combat("lost_shift")
        hero = engine.living_heroes()[0]
        hero.hp = 0
        hero.deaths_door = True
        fixture = replace(self.catalog, balance={**self.catalog.balance, "death_chance": 1.0})
        with patch.object(engine, "catalog", fixture):
            engine._damage(hero, 999)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "dead-crew.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        self.assertFalse(next(actor for actor in loaded.state.heroes if actor.id == hero.id).alive)

    def test_invalid_saved_terrain_is_rejected(self) -> None:
        engine = GameEngine.new(self.catalog, 303)
        snapshot = engine.snapshot()
        snapshot["state"]["world_tiles"][0] = "broken"
        with self.assertRaisesRegex(RuleError, "malformed world terrain"):
            GameEngine.from_snapshot(self.catalog, snapshot)

        snapshot = engine.snapshot()
        first_row = snapshot["state"]["world_tiles"][0]
        snapshot["state"]["world_tiles"][0] = "." + first_row[1:]
        with self.assertRaisesRegex(RuleError, "disconnected world terrain"):
            GameEngine.from_snapshot(self.catalog, snapshot)

    def test_saved_layout_must_match_its_world_type(self) -> None:
        snapshot = GameEngine.new(self.catalog, 404).snapshot()
        snapshot["state"]["room_positions"][1][0] += 1
        with self.assertRaisesRegex(RuleError, "positions do not match"):
            GameEngine.from_snapshot(self.catalog, snapshot)

        snapshot = GameEngine.new(self.catalog, 404).snapshot()
        snapshot["state"]["biome_ids"][1] = snapshot["state"]["biome_ids"][0]
        with self.assertRaisesRegex(RuleError, "four-biome selection"):
            GameEngine.from_snapshot(self.catalog, snapshot)

    def test_saved_enemy_formation_is_validated(self) -> None:
        snapshot = GameEngine.new(self.catalog, 405).snapshot()
        room = next(
            room
            for room in snapshot["state"]["rooms"]
            if room["kind"] in {"fight", "elite"}
        )
        room["enemy_ids"] = ["missing-enemy"]
        with self.assertRaisesRegex(RuleError, "invalid enemy formation"):
            GameEngine.from_snapshot(self.catalog, snapshot)

    def test_saved_intent_target_is_validated(self) -> None:
        engine = GameEngine.new(self.catalog, 406)
        engine.start_combat("reactor_meter_pack")
        snapshot = engine.snapshot()
        snapshot["state"]["intents"][0]["target_ids"] = ["missing-target"]
        with self.assertRaisesRegex(RuleError, "malformed enemy intent"):
            GameEngine.from_snapshot(self.catalog, snapshot)


if __name__ == "__main__":
    unittest.main()
