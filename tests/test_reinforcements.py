from __future__ import annotations

import copy
import json
import tempfile
import unittest
from pathlib import Path

from roag.commands import AdvanceWorldCommand
from roag.content import ENEMY_ARCHETYPES
from roag.danger import (
    REINFORCEMENT_COUNT_KEY,
    REINFORCEMENT_INTERVAL,
    REINFORCEMENT_LAST_TURN_KEY,
    REINFORCEMENT_MIN_DISTANCE,
    evaluate_danger_step,
)
from roag.ecology import (
    ACTIVE_RADIUS, ACTOR_BUDGET, REGIONAL_ACTOR_LIMIT, active_actors,
)
from roag.regions import begin_region, region_reachable
from roag.runtime_events import ThreatSpawned
from roag.save import load_game, save_game
from roag.session import GameSession
from roag.state import Position, Threat, create_world
from roag.world import distance, field_of_view, is_walkable, line_of_sight


class ReinforcementDirectorTests(unittest.TestCase):
    def state_at_pressure(self, seed="danger reinforcements"):
        state = create_world(seed)
        begin_region(state, "hearthford")
        state.carried_passives.clear()
        state.carried_goods.clear()
        state.noise = 20
        return state

    def reinforcement_action(self, state):
        return next(
            action for action in evaluate_danger_step(state)
            if action.action_id == "danger.spawn_reinforcement"
        )

    def test_evaluation_is_pure_deterministic_and_selects_a_fair_cell(self):
        first = self.state_at_pressure("reinforcement deterministic")
        second = copy.deepcopy(first)
        visible = field_of_view(first, remember=False)
        before = first.to_dict()

        first_action = self.reinforcement_action(first)
        second_action = self.reinforcement_action(second)

        self.assertEqual(first_action, second_action)
        self.assertEqual(first.to_dict(), before)
        self.assertIsNotNone(first_action.position)
        point = first_action.position
        self.assertIn(point, region_reachable(first.region, first.position))
        self.assertTrue(is_walkable(first, point, ignore_threat=True))
        self.assertNotIn(point, visible)
        self.assertFalse(line_of_sight(first, first.position, point))
        self.assertGreaterEqual(distance(first.position, point), REINFORCEMENT_MIN_DISTANCE)
        self.assertLessEqual(distance(first.position, point), ACTIVE_RADIUS)
        self.assertNotIn(point, {actor.position for actor in first.threats})

    def test_every_named_region_has_a_legal_standard_reinforcement(self):
        state = create_world("reinforcement regional coverage")
        for region_id in (
            "hearthford", "greywash", "greenwold", "whitecairn",
            "dunmire", "rillscar", "marlbank", "frostmere",
        ):
            with self.subTest(region_id=region_id):
                begin_region(state, region_id)
                state.carried_passives.clear()
                state.carried_goods.clear()
                state.noise = 20
                action = self.reinforcement_action(state)
                self.assertEqual(
                    ENEMY_ARCHETYPES[action.archetype_id]["region"], region_id,
                )
                self.assertFalse(ENEMY_ARCHETYPES[action.archetype_id].get("elite"))
                self.assertIn(
                    action.position, region_reachable(state.region, state.position),
                )

    def test_spawn_is_step_scoped_persistent_and_cannot_attack_same_tick(self):
        state = self.state_at_pressure("reinforcement event")
        health_before = state.courier.health
        ids_before = {actor.id for actor in state.threats}

        session = GameSession(state)
        outcome = session.submit(AdvanceWorldCommand())

        spawned = [actor for actor in state.threats if actor.id not in ids_before]
        self.assertEqual(len(spawned), 1)
        actor = spawned[0]
        self.assertEqual(state.courier.health, health_before)
        self.assertEqual(actor.status, "engaged")
        self.assertEqual(actor.turn, 0)
        self.assertEqual(actor.last_known_position, state.position)
        self.assertEqual(len(outcome.event_batch.steps), 1)
        self.assertEqual(len(outcome.event_batch.steps[0].events), 1)
        event = outcome.event_batch.steps[0].events[0]
        self.assertIsInstance(event, ThreatSpawned)
        self.assertEqual(
            (event.actor_id, event.archetype_id, event.position, event.pressure_band),
            (actor.id, actor.archetype_id, actor.position, "strained"),
        )
        self.assertEqual(outcome.events.count(event), 1)
        self.assertIs(state.region_threats[state.active_region_id], state.threats)
        self.assertEqual(state.region.changes[REINFORCEMENT_COUNT_KEY], 1)
        self.assertEqual(
            state.region.changes[REINFORCEMENT_LAST_TURN_KEY], state.world_time,
        )

        session.submit(AdvanceWorldCommand())
        self.assertGreater(actor.turn, 0)

    def test_same_seed_and_action_produce_identical_state_and_event(self):
        first = self.state_at_pressure("reinforcement replay")
        second = copy.deepcopy(first)

        first_outcome = GameSession(first).submit(AdvanceWorldCommand())
        second_outcome = GameSession(second).submit(AdvanceWorldCommand())

        self.assertEqual(first.to_dict(), second.to_dict())
        self.assertEqual(first_outcome.event_batch, second_outcome.event_batch)

    def test_cadence_prevents_immediate_replacement_then_allows_next_arrival(self):
        state = self.state_at_pressure("reinforcement cadence")
        session = GameSession(state)
        session.submit(AdvanceWorldCommand())
        first_count = len(state.threats)
        state.noise = 40

        session.submit(AdvanceWorldCommand(
            steps=REINFORCEMENT_INTERVAL["critical"] - 1,
        ))
        self.assertEqual(len(state.threats), first_count)

        outcome = session.submit(AdvanceWorldCommand())

        self.assertEqual(len(state.threats), first_count + 1)
        self.assertEqual(
            sum(isinstance(event, ThreatSpawned) for event in outcome.events), 1,
        )

    def test_active_and_regional_population_limits_defer_spawning(self):
        active_limited = self.state_at_pressure("reinforcement active limit")
        active_limited.threats.clear()
        for index in range(ACTOR_BUDGET):
            active_limited.threats.append(Threat(
                f"active-{index}", "active fixture", "pursuer",
                Position(active_limited.position.x + 1, active_limited.position.y),
                4, 4, status="watching",
            ))
        self.assertEqual(len(active_actors(active_limited)), ACTOR_BUDGET)
        self.assertFalse(any(
            action.action_id == "danger.spawn_reinforcement"
            for action in evaluate_danger_step(active_limited)
        ))

        region_limited = self.state_at_pressure("reinforcement region limit")
        region_limited.threats.extend(
            Threat(
                f"retired-{index}", "retired fixture", "pursuer",
                region_limited.position, 0, 4, status="defeated",
            )
            for index in range(REGIONAL_ACTOR_LIMIT - len(region_limited.threats))
        )
        self.assertEqual(len(region_limited.threats), REGIONAL_ACTOR_LIMIT)
        self.assertFalse(any(
            action.action_id == "danger.spawn_reinforcement"
            for action in evaluate_danger_step(region_limited)
        ))

    def test_spawned_actor_and_equipment_round_trip_but_event_does_not(self):
        state = self.state_at_pressure("reinforcement persistence")
        outcome = GameSession(state).submit(AdvanceWorldCommand())
        event = next(event for event in outcome.events if isinstance(event, ThreatSpawned))
        actor = next(actor for actor in state.threats if actor.id == event.actor_id)
        equipment_ids = sorted(
            item.id for item in state.items if item.owner_id == actor.id
        )

        begin_region(state, "greywash")
        begin_region(state, "hearthford")
        self.assertEqual(
            next(candidate for candidate in state.threats if candidate.id == actor.id),
            actor,
        )

        with tempfile.TemporaryDirectory() as directory:
            path = save_game(state, Path(directory) / "reinforcement.json")
            raw = path.read_text(encoding="utf-8")
            payload = json.loads(raw)
            restored = load_game(path)

        restored_actor = next(
            candidate for candidate in restored.threats if candidate.id == actor.id
        )
        self.assertEqual(restored_actor, actor)
        self.assertEqual(
            sorted(item.id for item in restored.items if item.owner_id == actor.id),
            equipment_ids,
        )
        self.assertEqual(
            restored.region.changes[REINFORCEMENT_COUNT_KEY], 1,
        )
        self.assertNotIn("ThreatSpawned", raw)
        self.assertNotIn("event_batch", payload)


if __name__ == "__main__":
    unittest.main()
