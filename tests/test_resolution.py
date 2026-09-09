from __future__ import annotations

import json
import unittest

from dumbest_dungeon.resolution import EventQueue, Listener, Payload
from dumbest_dungeon.triggers import EventType as E, Limiter, LimitKind as L, Phase, TriggerSpec


class ResolutionTests(unittest.TestCase):
    def test_conditional_nonactivation_does_not_spend_turn_limit(self) -> None:
        listener = Listener(TriggerSpec("conditional", E.DAMAGE, (E.HEAL,), limiter=Limiter(L.TURN)), 1, "actor")
        queue, healed = EventQueue(), []

        def trigger(listener, event, queue):
            if event.payload.amount < 5:
                return False
            queue.emit(E.HEAL, (), Payload(amount=3))

        for amount in (1, 8, 9):
            queue.begin(combat_token=1, turn_token=1)
            queue.submit(E.DAMAGE, "hit", (), Payload(amount=amount))
            queue.drain(lambda event: (listener,), lambda event, queue: healed.append(event.payload.amount) if event.event_type == E.HEAL else None, trigger)
            queue = EventQueue.from_snapshot(queue.snapshot())
        self.assertEqual([3], healed)

    def test_deferred_continuation_waits_for_descendants_and_survives_checkpoint(self) -> None:
        listener = Listener(TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,), limiter=Limiter(L.ROOT)), 1, "actor")
        outcomes = []
        for checkpoint in (False, True):
            queue, seen = EventQueue(), []
            queue.begin()
            queue.submit(E.CARD_STEP, "step:1", (), Payload())

            def primary(event, queue):
                seen.append(event.source_id)
                if event.event_type == E.CARD_STEP:
                    queue.emit(E.DAMAGE, (), Payload(), source_id="hit")
                    queue.emit(E.CLEANUP, (), Payload(), source_id="finish", deferred=True, mandatory=True)

            while queue.step(lambda event: (listener,), primary, lambda listener, event, queue: queue.emit(E.DAMAGE, (), Payload())):
                if checkpoint:
                    queue = EventQueue.from_snapshot(json.loads(json.dumps(queue.snapshot())))
            queue.finish()
            outcomes.append((seen, queue.snapshot()))
        self.assertEqual(["step:1", "hit", "echo", "finish"], outcomes[0][0])
        self.assertEqual(outcomes[0], outcomes[1])

    def test_phase_priority_creation_and_effect_order_are_canonical(self) -> None:
        listeners = [Listener(TriggerSpec(identity, E.DAMAGE, (), phase=phase, priority=priority), creation, "actor")
                     for identity, phase, priority, creation in (
                         ("z", Phase.AFTER, 0, 1), ("a", Phase.AFTER, 0, 1),
                         ("earlier", Phase.AFTER, -1, 99), ("created", Phase.AFTER, 0, 0),
                         ("before", Phase.BEFORE, 0, 0), ("replace", Phase.REPLACE, 0, 0),
                         ("death", Phase.DEATH, 0, 0), ("cleanup", Phase.CLEANUP, 0, 0))]
        outputs = []
        for items in (listeners, list(reversed(listeners))):
            queue, output = EventQueue(), []
            queue.begin()
            queue.submit(E.DAMAGE, "primary", (), Payload(amount=1000000000000))
            queue.drain(lambda event: tuple(items), lambda event, queue: output.append(event.source_id),
                        lambda listener, event, queue: output.append(listener.spec.id))
            outputs.append(output)
        self.assertEqual(outputs[0], outputs[1])
        self.assertEqual(["replace", "before", "primary", "earlier", "created", "a", "z", "death", "cleanup"], outputs[0])

    def test_listener_snapshot_survives_mutation_during_dispatch(self) -> None:
        listeners = [Listener(TriggerSpec(identity, E.DAMAGE, (), phase=Phase.BEFORE), index, "actor") for index, identity in enumerate(("first", "second"))]
        queue, seen = EventQueue(), []
        queue.begin()
        queue.submit(E.DAMAGE, "hit", (), Payload())

        def trigger(listener, event, queue):
            seen.append(listener.spec.id)
            listeners.clear()

        queue.drain(lambda event: tuple(listeners), lambda event, queue: None, trigger)
        self.assertEqual(["first", "second"], seen)

    def test_root_limiter_stops_a_cycle_without_limiting_damage(self) -> None:
        spec = TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,), limiter=Limiter(L.ROOT))
        listener = Listener(spec, 1, "actor")
        queue, hits = EventQueue(), []
        queue.begin(card_token="copy:1")
        queue.submit(E.DAMAGE, "card", (), Payload(amount=10**50))
        queue.drain(lambda event: (listener,), lambda event, queue: hits.append(event.payload.amount),
                    lambda listener, event, queue: queue.emit(E.DAMAGE, (), event.payload))
        self.assertEqual([10**50, 10**50], hits)
        self.assertEqual([], queue.state.seals)

    def test_budget_seals_runaway_branch_and_preserves_sibling_effect(self) -> None:
        listener = Listener(TriggerSpec("broken_echo", E.DAMAGE, (E.DAMAGE,)), 1, "actor")
        outputs, queue = [], EventQueue(budget=8)
        queue.begin()
        queue.submit(E.DAMAGE, "attack", (), Payload(amount=99))
        queue.submit(E.HEAL, "recovery", (), Payload(amount=7))
        queue.drain(lambda event: (listener,), lambda event, queue: outputs.append((event.source_id, event.payload.amount)),
                    lambda listener, event, queue: queue.emit(E.DAMAGE, (), event.payload))
        self.assertIn(("recovery", 7), outputs)
        self.assertEqual(1, len(queue.state.seals))
        self.assertEqual("CHAIN SEALED", queue.state.seals[0]["message"])
        self.assertTrue(queue.state.seals[0]["ancestry"])
        self.assertIsNone(queue.state.root_id)
        self.assertEqual([], queue.state.pending)

    def test_each_phase_checkpoint_matches_uninterrupted_dispatch(self) -> None:
        listener = Listener(TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,), limiter=Limiter(L.RETRIGGERS, 2)), 1, "actor")
        final = []
        for checkpoint in (False, True):
            queue, output = EventQueue(), []
            queue.begin(card_token="card:copy", combat_token=4, turn_token=2)
            queue.submit(E.DAMAGE, "card", ("target",), Payload(amount=9))
            while queue.step(lambda event: (listener,), lambda event, queue: output.append(event.payload.amount),
                             lambda listener, event, queue: queue.emit(E.DAMAGE, event.target_ids, event.payload)):
                if checkpoint:
                    queue = EventQueue.from_snapshot(json.loads(json.dumps(queue.snapshot())))
            queue.finish()
            final.append((output, queue.snapshot()))
        self.assertEqual(final[0], final[1])

    def test_sealing_keeps_an_independent_automatic_chain_and_mandatory_cleanup(self) -> None:
        listeners = (Listener(TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,)), 1, "actor"),
                     Listener(TriggerSpec("mend", E.HEAL, (E.HEAL,), limiter=Limiter(L.RETRIGGERS, 3)), 2, "actor"))
        outputs, queue = [], EventQueue(budget=4)
        queue.begin()
        queue.submit(E.DAMAGE, "attack", (), Payload())
        queue.submit(E.HEAL, "recovery", (), Payload())

        def primary(event, queue):
            outputs.append(event.event_type)
            if event.event_type == E.DAMAGE:
                queue.emit(E.CLEANUP, (), Payload(), mandatory=True)

        queue.drain(lambda event: listeners, primary,
                    lambda listener, event, queue: queue.emit(event.event_type, (), event.payload))
        self.assertEqual(4, outputs.count(E.HEAL))
        self.assertEqual(outputs.count(E.DAMAGE), outputs.count(E.CLEANUP))
        self.assertEqual(1, len(queue.state.seals))

    def test_family_exclusion_rejects_its_descendants_and_restore_rejects_bad_ancestry(self) -> None:
        listener = Listener(TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,), proc_family="echo",
                                       limiter=Limiter(L.FAMILY, family="echo")), 1, "actor")
        queue, hits = EventQueue(), []
        queue.begin()
        queue.submit(E.DAMAGE, "attack", (), Payload())
        broken = json.loads(json.dumps(queue.snapshot()))
        broken["state"]["pending"][0]["parent_event_id"] = 99
        with self.assertRaisesRegex(ValueError, "ancestry"):
            EventQueue.from_snapshot(broken)
        queue.drain(lambda event: (listener,), lambda event, queue: hits.append(event.event_id),
                    lambda listener, event, queue: queue.emit(E.DAMAGE, (), Payload()))
        self.assertEqual(2, len(hits))

    def test_turn_and_combat_scopes_persist_across_roots(self) -> None:
        listeners = tuple(Listener(TriggerSpec(kind.value, E.DAMAGE, (), limiter=Limiter(kind)), index, "actor")
                          for index, kind in enumerate((L.ROOT, L.CARD, L.TURN, L.COMBAT, L.CHARGES)))
        queue, fired = EventQueue(), []
        for turn in (1, 1, 2):
            queue.begin(card_token="same-copy", combat_token=1, turn_token=turn)
            queue.submit(E.DAMAGE, "card", (), Payload())
            queue.drain(lambda event: listeners, lambda event, queue: None,
                        lambda listener, event, queue: fired.append(listener.spec.id))
        self.assertEqual(3, fired.count(L.ROOT.value))
        self.assertEqual(3, fired.count(L.CARD.value))
        self.assertEqual(2, fired.count(L.TURN.value))
        self.assertEqual(1, fired.count(L.COMBAT.value))
        self.assertEqual(1, fired.count(L.CHARGES.value))

    def test_prevention_and_invalid_nested_dispatch(self) -> None:
        listener = Listener(TriggerSpec("ward", E.DAMAGE, (), phase=Phase.REPLACE), 0, "actor")
        queue, hit = EventQueue(), []
        queue.begin()
        queue.submit(E.DAMAGE, "hit", (), Payload())
        queue.drain(lambda event: (listener,), lambda event, queue: hit.append(1),
                    lambda listener, event, queue: queue.prevent())
        self.assertEqual([], hit)
        queue.begin()
        queue.submit(E.DAMAGE, "hit", (), Payload())
        with self.assertRaisesRegex(ValueError, "recursively dispatch"):
            queue.drain(lambda event: (), lambda event, queue: queue.drain(None, None, None), None)


if __name__ == "__main__":
    unittest.main()
