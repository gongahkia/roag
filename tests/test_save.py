from __future__ import annotations

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.save import default_save_path, read_save, write_save


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
        original = self.catalog.balance["death_chance"]
        self.catalog.balance["death_chance"] = 1.0
        try:
            engine._damage(hero, 999)
        finally:
            self.catalog.balance["death_chance"] = original
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
