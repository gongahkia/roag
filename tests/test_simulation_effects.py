from __future__ import annotations

import copy
from dataclasses import FrozenInstanceError
import unittest

from roag.circuits import CELL_CHARGE, cell_key
from roag.inventory import auto_place, create_item
from roag.simulation_effects import (
    ActorRef,
    ActorDefeatedFact,
    ComponentRef,
    ConsumeResource,
    GainCharge,
    LoadRack,
    ReactionRule,
    ResourceGainedFact,
    SimulationFactCollector,
    SpendCharge,
    TerrainActionFact,
    ThreatDetectedFact,
    WithinRange,
    resolve_component_reactions,
)
from roag.state import CircuitCell, Position, create_world, game_state_from_dict


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

    def test_resource_fact_preserves_physical_item_identity_and_quantity(self):
        fact = ResourceGainedFact(
            "courier:test", "item-00042", "ingredient:clay", 2,
            self.space, self.position,
        )
        collector = SimulationFactCollector()
        collector.emit(fact)

        self.assertEqual(collector.freeze(), (fact,))
        self.assertEqual(fact.fact_id, "resource.gained")
        self.assertEqual((fact.item_id, fact.item_kind, fact.quantity), (
            "item-00042", "ingredient:clay", 2,
        ))
        with self.assertRaises(ValueError):
            ResourceGainedFact(
                "courier:test", "item-00042", "ingredient:clay", 0,
                self.space, self.position,
            )

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

    def test_spend_charge_effect_is_bounded_and_reports_consumption(self):
        self.state.circuits[self.key].charge = 1
        fact = TerrainActionFact(
            "courier:test", "cut", "terrain.region.dense_reeds",
            self.space, self.position,
        )
        rule = ReactionRule(
            "test.terrain-power",
            ComponentRef("circuit", self.key),
            "terrain.action",
            SpendCharge(2, ComponentRef("circuit", self.key)),
        )

        result = resolve_component_reactions(self.state, (fact,), (rule,))

        self.assertEqual(self.state.circuits[self.key].charge, 0)
        self.assertEqual(len(result.applications), 1)
        self.assertEqual(result.applications[0].effect_id, "spend_charge")
        self.assertEqual(result.applications[0].requested_amount, 2)
        self.assertEqual(result.applications[0].applied_amount, 1)
        empty = resolve_component_reactions(self.state, (fact,), (rule,))
        self.assertEqual(empty.applications, ())

    def test_resource_effect_consumes_exact_physical_pack_stock_and_audits_kind(self):
        self.state.location = "region"
        owner = self.state.active_courier_id
        resource = create_item(
            self.state, "ingredient:clay", "engine resource", quantity=2,
        )
        self.assertTrue(auto_place(
            self.state, resource.id, "pack", owner_id=owner,
        ))
        fact = ResourceGainedFact(
            owner, resource.id, resource.kind, resource.quantity,
            self.space, self.position,
        )
        rule = ReactionRule(
            "test.consume-clay",
            ComponentRef("circuit", self.key),
            "resource.gained",
            ConsumeResource(resource.kind, 2, ActorRef(owner)),
        )

        result = resolve_component_reactions(self.state, (fact,), (rule,))

        self.assertEqual((resource.location, resource.owner_id), ("destroyed", None))
        self.assertEqual(len(result.applications), 1)
        application = result.applications[0]
        self.assertEqual(application.target, ActorRef(owner))
        self.assertEqual(application.effect_id, "consume_resource")
        self.assertEqual(application.resource_kind, "ingredient:clay")
        self.assertEqual(application.resource_spent, 2)
        self.assertEqual(
            (application.requested_amount, application.applied_amount), (2, 2),
        )
        payload = self.state.to_dict()
        restored = game_state_from_dict(payload)
        restored_resource = next(
            item for item in restored.items if item.id == resource.id
        )
        self.assertEqual(restored_resource.location, "destroyed")
        self.assertNotIn("simulation_facts", payload)
        self.assertNotIn("effect_applications", payload)

    def test_resource_effect_is_all_or_nothing_and_cannot_consume_another_actor(self):
        self.state.location = "region"
        owner = self.state.active_courier_id
        resource = create_item(self.state, "ingredient:clay", "single resource")
        self.assertTrue(auto_place(
            self.state, resource.id, "pack", owner_id=owner,
        ))
        fact = ResourceGainedFact(
            owner, resource.id, resource.kind, resource.quantity,
            self.space, self.position,
        )
        insufficient = ReactionRule(
            "test.insufficient",
            ComponentRef("circuit", self.key),
            "resource.gained",
            ConsumeResource(resource.kind, 2, ActorRef(owner)),
        )
        wrong_actor = ReactionRule(
            "test.wrong-actor",
            ComponentRef("circuit", self.key),
            "resource.gained",
            ConsumeResource(resource.kind, 1, ActorRef("courier:other")),
        )

        result = resolve_component_reactions(
            self.state, (fact,), (insufficient, wrong_actor),
        )

        self.assertEqual(result.applications, ())
        self.assertEqual(
            (resource.location, resource.owner_id, resource.quantity),
            ("pack", owner, 1),
        )

        remote_space = "region:greywash"
        remote_key = cell_key(remote_space, self.position, "surface")
        self.state.circuits[remote_key] = CircuitCell(
            remote_space, self.position, "surface", "rack",
        )
        remote_fact = ResourceGainedFact(
            owner, resource.id, resource.kind, resource.quantity,
            remote_space, self.position,
        )
        remote_rule = ReactionRule(
            "test.inactive-space",
            ComponentRef("circuit", remote_key),
            "resource.gained",
            ConsumeResource(resource.kind, 1, ActorRef(owner)),
        )
        remote = resolve_component_reactions(
            self.state, (remote_fact,), (remote_rule,),
        )
        self.assertEqual(remote.applications, ())
        self.assertEqual((resource.location, resource.quantity), ("pack", 1))

    def test_rack_load_atomically_converts_one_physical_cell_into_full_charge(self):
        self.state.location = "region"
        owner = self.state.active_courier_id
        cell = create_item(self.state, "circuit:cell", "engine fuel")
        self.assertTrue(auto_place(
            self.state, cell.id, "pack", owner_id=owner,
        ))
        fact = ResourceGainedFact(
            owner, cell.id, cell.kind, cell.quantity,
            self.space, self.position,
        )
        rule = ReactionRule(
            "test.load-rack",
            ComponentRef("circuit", self.key),
            "resource.gained",
            LoadRack(ComponentRef("circuit", self.key), ActorRef(owner)),
        )

        result = resolve_component_reactions(self.state, (fact,), (rule,))

        self.assertEqual(self.state.circuits[self.key].charge, CELL_CHARGE)
        self.assertEqual((cell.location, cell.owner_id), ("destroyed", None))
        self.assertEqual(len(result.applications), 1)
        application = result.applications[0]
        self.assertEqual(application.effect_id, "load_rack")
        self.assertEqual(application.target, ComponentRef("circuit", self.key))
        self.assertEqual(
            (application.requested_amount, application.applied_amount),
            (CELL_CHARGE, CELL_CHARGE),
        )
        self.assertEqual(
            (application.resource_kind, application.resource_spent),
            ("circuit:cell", 1),
        )

    def test_rack_load_does_not_spend_a_cell_for_partial_benefit(self):
        self.state.location = "region"
        owner = self.state.active_courier_id
        self.state.circuits[self.key].charge = CELL_CHARGE + 1
        cell = create_item(self.state, "circuit:cell", "engine fuel")
        self.assertTrue(auto_place(
            self.state, cell.id, "pack", owner_id=owner,
        ))
        fact = ResourceGainedFact(
            owner, cell.id, cell.kind, cell.quantity,
            self.space, self.position,
        )
        rule = ReactionRule(
            "test.full-rack",
            ComponentRef("circuit", self.key),
            "resource.gained",
            LoadRack(ComponentRef("circuit", self.key), ActorRef(owner)),
        )

        result = resolve_component_reactions(self.state, (fact,), (rule,))

        self.assertEqual(result.applications, ())
        self.assertEqual(self.state.circuits[self.key].charge, CELL_CHARGE + 1)
        self.assertEqual(
            (cell.location, cell.owner_id, cell.quantity), ("pack", owner, 1),
        )

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
        with self.assertRaises(ValueError):
            SpendCharge(0, ComponentRef("circuit", self.key))
        with self.assertRaises(ValueError):
            ConsumeResource("ingredient:clay", 0, ActorRef("courier:test"))
        with self.assertRaises(ValueError):
            ActorRef("")
        duplicate = (self.rule(), self.rule())
        with self.assertRaises(ValueError):
            resolve_component_reactions(self.state, (self.fact(),), duplicate)
        self.assertEqual(self.state.circuits[self.key].charge, 0)


if __name__ == "__main__":
    unittest.main()
