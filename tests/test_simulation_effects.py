from __future__ import annotations

import copy
from dataclasses import FrozenInstanceError
import unittest

from roag.circuits import CELL_CHARGE, cell_key
from roag.simulation_effects import (
    ActorDefeatedFact,
    ComponentRef,
    GainCharge,
    ReactionRule,
    SimulationFactCollector,
    ThreatDetectedFact,
    WithinRange,
    resolve_component_reactions,
)
from roag.state import CircuitCell, Position, create_world


class SimulationEffectTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("bounded simulation effects")
        self.position = Position(20, 20, 0)
        self.space = "region:hearthford"
        self.key = cell_key(self.space, self.position, "surface")
        self.state.circuits[self.key] = CircuitCell(
            self.space, self.position, "surface", "rack"
        )

    def fact(self) -> ActorDefeatedFact:
        return ActorDefeatedFact("threat:test", "courier:test", self.space, self.position)

    def rule(self, rule_id: str = "test.kill-charge", *, amount: int = 3, priority: int = 0) -> ReactionRule:
        return ReactionRule(
            rule_id,
            ComponentRef("circuit", self.key),
            "actor.defeated",
            GainCharge(amount),
            priority,
        )

    def test_fact_collection_is_ordered_finite_and_freezes(self):
        collector = SimulationFactCollector()
        first = self.fact()
        second = ThreatDetectedFact("threat:other", "sensor:test", self.space, self.position)
        collector.emit(first)
        collector.emit(second)
        self.assertEqual(collector.freeze(), (first, second))
        with self.assertRaises(RuntimeError):
            collector.emit(first)
        with self.assertRaises(RuntimeError):
            collector.freeze()
        with self.assertRaises(FrozenInstanceError):
            first.actor_id = "changed"

    def test_registered_rule_applies_one_bounded_circuit_effect(self):
        result = resolve_component_reactions(self.state, (self.fact(),), (self.rule(),))
        self.assertEqual(self.state.circuits[self.key].charge, 3)
        self.assertEqual(len(result.applications), 1)
        application = result.applications[0]
        self.assertEqual(application.fact_index, 0)
        self.assertEqual(application.fact_id, "actor.defeated")
        self.assertEqual(application.rule_id, "test.kill-charge")
        self.assertEqual(application.target, ComponentRef("circuit", self.key))
        self.assertEqual(application.effect_id, "gain_charge")
        self.assertEqual((application.requested_amount, application.applied_amount), (3, 3))

    def test_charge_effect_respects_existing_circuit_capacity(self):
        self.state.circuits[self.key].charge = 2 * CELL_CHARGE - 1
        result = resolve_component_reactions(self.state, (self.fact(),), (self.rule(amount=5),))
        self.assertEqual(self.state.circuits[self.key].charge, 2 * CELL_CHARGE)
        self.assertEqual(result.applications[0].applied_amount, 1)
        second = resolve_component_reactions(self.state, (self.fact(),), (self.rule(amount=5),))
        self.assertEqual(second.applications, ())

    def test_fact_trigger_and_physical_space_must_match(self):
        detected = ThreatDetectedFact("threat:test", "sensor:test", self.space, self.position)
        wrong_trigger = resolve_component_reactions(self.state, (detected,), (self.rule(),))
        self.assertEqual(wrong_trigger.applications, ())
        remote = ActorDefeatedFact("threat:test", "courier:test", "region:greywash", self.position)
        wrong_space = resolve_component_reactions(self.state, (remote,), (self.rule(),))
        self.assertEqual(wrong_space.applications, ())
        self.assertEqual(self.state.circuits[self.key].charge, 0)

    def test_finite_range_condition_uses_same_level_chebyshev_distance(self):
        rule = ReactionRule(
            "ranged", ComponentRef("circuit", self.key), "actor.defeated",
            GainCharge(1), condition=WithinRange(self.space, Position(20, 20), 2),
        )
        edge = ActorDefeatedFact(
            "threat:edge", "courier:test", self.space, Position(22, 22),
        )
        outside = ActorDefeatedFact(
            "threat:outside", "courier:test", self.space, Position(23, 20),
        )
        different_level = ActorDefeatedFact(
            "threat:level", "courier:test", self.space, Position(20, 20, 1),
        )
        result = resolve_component_reactions(
            self.state, (outside, different_level, edge), (rule,),
        )
        self.assertEqual(self.state.circuits[self.key].charge, 1)
        self.assertEqual([item.fact_index for item in result.applications], [2])

    def test_rule_order_is_deterministic_and_not_registration_order(self):
        first = copy.deepcopy(self.state)
        second = copy.deepcopy(self.state)
        rules = (
            self.rule("rule.z", amount=2, priority=1),
            self.rule("rule.a", amount=1, priority=0),
        )
        one = resolve_component_reactions(first, (self.fact(),), rules)
        two = resolve_component_reactions(second, (self.fact(),), tuple(reversed(rules)))
        self.assertEqual(one, two)
        self.assertEqual(first.to_dict(), second.to_dict())
        self.assertEqual([item.rule_id for item in one.applications], ["rule.a", "rule.z"])

    def test_facts_and_resolution_do_not_enter_serialized_state(self):
        before_keys = set(self.state.to_dict())
        result = resolve_component_reactions(self.state, (self.fact(),), (self.rule(),))
        payload = self.state.to_dict()
        self.assertEqual(set(payload), before_keys)
        self.assertNotIn("facts", payload)
        self.assertNotIn("applications", payload)
        self.assertEqual(result.facts, (self.fact(),))

    def test_missing_or_non_rack_component_is_a_safe_no_op(self):
        missing = ReactionRule(
            "missing", ComponentRef("circuit", "missing"), "actor.defeated", GainCharge(2)
        )
        self.assertEqual(
            resolve_component_reactions(self.state, (self.fact(),), (missing,)).applications,
            (),
        )
        self.state.circuits[self.key].kind = "trace"
        self.assertEqual(
            resolve_component_reactions(self.state, (self.fact(),), (self.rule(),)).applications,
            (),
        )

    def test_invalid_or_duplicate_rules_are_rejected_before_mutation(self):
        with self.assertRaises(ValueError):
            GainCharge(0)
        duplicate = (self.rule(), self.rule())
        with self.assertRaises(ValueError):
            resolve_component_reactions(self.state, (self.fact(),), duplicate)
        self.assertEqual(self.state.circuits[self.key].charge, 0)


if __name__ == "__main__":
    unittest.main()
