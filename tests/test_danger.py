from __future__ import annotations

import copy
import unittest

from roag.actions import _advance_world
from roag.danger import (
    DangerAction,
    DangerForecast,
    Pressure,
    REINFORCEMENT_LAST_TURN_KEY,
    apply_danger_actions,
    danger_forecast,
    evaluate_band_transition,
    evaluate_danger_step,
    evaluate_region_entry,
    pressure,
    resolve_danger_step,
)
from roag.ecology import ACTOR_BUDGET
from roag.regions import begin_region
from roag.state import Position, create_world
from roag.situations import BY_REGION_BAND
from roag.world import Pressure as LegacyPressure
from roag.world import pressure as legacy_pressure


class DangerDirectorTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("danger director boundary")
        begin_region(self.state, "hearthford")
        self.state.carried_passives.clear()
        self.state.carried_goods.clear()
        self.state.noise = 0

    @staticmethod
    def suppress_reinforcements(state):
        state.region.changes[REINFORCEMENT_LAST_TURN_KEY] = state.world_time

    def test_pressure_profile_keeps_exact_legacy_formula_and_adapter(self):
        landing = self.state.region.landmarks["landing"]
        self.state.position = Position(landing.x + 28, landing.y, 1)
        self.state.pressure_elapsed = 180
        self.state.noise = 5
        self.state.carried_passives = {"first": 1, "second": 1}

        profile = pressure(self.state)

        self.assertEqual(
            profile,
            Pressure(180, 4, 5, 2, 18, "critical", 12, 2),
        )
        self.assertIs(Pressure, LegacyPressure)
        self.assertIs(pressure, legacy_pressure)

    def test_forecast_exposes_only_pressure_drivers_threshold_and_cadence(self):
        steady = danger_forecast(self.state)
        self.assertEqual(
            steady,
            DangerForecast(pressure(self.state), "strained", 10, None),
        )

        self.state.noise = 20
        strained = danger_forecast(self.state)
        self.assertEqual(strained.next_band, "critical")
        self.assertEqual(strained.points_to_next_band, 8)
        self.assertEqual(strained.response_due_in, 0)

        self.state.region.changes[REINFORCEMENT_LAST_TURN_KEY] = self.state.world_time
        self.assertEqual(danger_forecast(self.state).response_due_in, 36)

    def test_step_evaluation_is_pure_deterministic_and_wakes_one_existing_actor(self):
        first, second = copy.deepcopy(self.state), copy.deepcopy(self.state)
        for state in (first, second):
            state.pressure_elapsed = 324
            self.suppress_reinforcements(state)
        first_before, second_before = first.to_dict(), second.to_dict()

        first_actions = evaluate_danger_step(first)
        second_actions = evaluate_danger_step(second)

        self.assertEqual(first_actions, second_actions)
        self.assertEqual(first.to_dict(), first_before)
        self.assertEqual(second.to_dict(), second_before)
        self.assertEqual(len(first_actions), 1)
        self.assertEqual(first_actions[0].action_id, "danger.critical_escalation")
        target_id = first_actions[0].target_id
        self.assertIsNotNone(target_id)
        ids_before = [actor.id for actor in first.threats]

        messages = apply_danger_actions(first, first_actions)

        self.assertTrue(first.escalation_spawned)
        self.assertTrue(first.region.changes["escalation_spawned"])
        self.assertEqual([actor.id for actor in first.threats], ids_before)
        self.assertNotEqual(
            next(actor for actor in first.threats if actor.id == target_id).status,
            "dormant",
        )
        self.assertEqual(len(messages), 1)
        self.assertEqual(evaluate_danger_step(first), ())

    def test_critical_escalation_remains_consumed_without_a_dormant_actor(self):
        self.state.pressure_elapsed = 324
        self.suppress_reinforcements(self.state)
        for actor in self.state.threats:
            actor.status = "defeated"
        actions = evaluate_danger_step(self.state)
        self.assertEqual(
            actions,
            (DangerAction("danger.critical_escalation", "critical"),),
        )

        outcome = resolve_danger_step(self.state)

        self.assertTrue(self.state.escalation_spawned)
        self.assertTrue(self.state.region.changes["escalation_spawned"])
        self.assertEqual(outcome.messages, ())

    def test_band_transition_is_separate_pure_policy_then_authored_application(self):
        self.state.pressure_elapsed = 180
        before = self.state.to_dict()

        actions = evaluate_band_transition(self.state, "steady")

        self.assertEqual(
            actions,
            (DangerAction("danger.band_transition", "strained"),),
        )
        self.assertEqual(self.state.to_dict(), before)
        messages = apply_danger_actions(self.state, actions)
        self.assertEqual(
            self.state.region.changes["situation:active"],
            BY_REGION_BAND["hearthford", "strained"].id,
        )
        self.assertEqual(len(messages), 1)
        self.assertIn("strained", messages[0])

    def test_region_entry_situation_is_a_zero_time_director_action(self):
        before_time = self.state.world_time
        self.state.carried_passives = {f"valuable-{index}": 1 for index in range(12)}
        before = self.state.to_dict()

        actions = evaluate_region_entry(self.state)

        self.assertEqual(
            actions,
            (DangerAction("danger.activate_situation", "steady"),),
        )
        self.assertEqual(self.state.to_dict(), before)
        messages = apply_danger_actions(self.state, actions)
        self.assertEqual(messages, ())
        self.assertEqual(self.state.world_time, before_time)
        self.assertEqual(
            self.state.region.changes["situation:active"],
            BY_REGION_BAND["hearthford", "steady"].id,
        )

    def test_world_step_preserves_wake_before_final_band_announcement(self):
        self.state.pressure_elapsed = 323
        self.suppress_reinforcements(self.state)
        dormant = next(actor for actor in self.state.threats if actor.status == "dormant")
        for actor in self.state.threats:
            if actor is not dormant:
                actor.status = "defeated"
        health_before = self.state.courier.health
        ids_before = [actor.id for actor in self.state.threats]
        time_before = self.state.world_time

        _advance_world(self.state)

        self.assertEqual(self.state.world_time, time_before + 1)
        self.assertEqual(self.state.courier.health, health_before)
        self.assertEqual([actor.id for actor in self.state.threats], ids_before)
        self.assertNotEqual(dormant.status, "dormant")
        self.assertTrue(self.state.escalation_spawned)
        escalation_index = next(
            index for index, message in enumerate(self.state.messages)
            if "High pressure wakes" in message
        )
        transition_index = next(
            index for index, message in enumerate(self.state.messages)
            if "Pressure becomes critical" in message
        )
        self.assertLess(escalation_index, transition_index)

    def test_multi_step_action_announces_only_final_transition(self):
        self.state.pressure_elapsed = 178
        for actor in self.state.threats:
            actor.status = "defeated"

        _advance_world(self.state, steps=3)

        announcements = [
            message for message in self.state.messages
            if message.startswith("Pressure becomes ")
        ]
        self.assertEqual(pressure(self.state).band, "strained")
        self.assertEqual(len(announcements), 1)
        self.assertIn("strained", announcements[0])

    def test_legacy_escalation_adds_no_population_when_reinforcement_deferred(self):
        before_keys = set(self.state.to_dict())
        before_ids = [actor.id for actor in self.state.threats]
        self.state.pressure_elapsed = 324
        self.suppress_reinforcements(self.state)

        outcome = resolve_danger_step(self.state)

        self.assertEqual(set(self.state.to_dict()), before_keys)
        self.assertNotIn("danger", self.state.to_dict())
        self.assertEqual([actor.id for actor in self.state.threats], before_ids)
        self.assertLessEqual(len(outcome.actions), 1)
        self.assertEqual(ACTOR_BUDGET, 24)


if __name__ == "__main__":
    unittest.main()
