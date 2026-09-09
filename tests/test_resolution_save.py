from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.resolution import Listener, Payload
from dumbest_dungeon.triggers import EventType as E, Limiter, LimitKind, TriggerSpec


class ResolutionSaveTests(unittest.TestCase):
    def test_active_queue_and_limiter_continue_with_the_same_engine_state(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        hero = engine.living_heroes()[0]
        hero.hp -= 10
        queue = engine.resolution
        queue.begin(card_token="test:card", combat_token=1, turn_token=1)
        queue.submit(E.HEAL, "test:heal", (hero.id,), Payload(actor_id=hero.id, amount=2))
        listener = Listener(TriggerSpec("test:echo", E.HEAL, (E.HEAL,), limiter=Limiter(LimitKind.RETRIGGERS, 2)), 1, hero.id)
        callbacks = lambda event: (listener,)
        trigger = lambda listener, event, queue: queue.emit(E.HEAL, event.target_ids, event.payload)

        def primary_for(game):
            def primary(event, queue):
                with game.attribution(event.source_id):
                    game._heal(game._actor(event.target_ids[0]), event.payload.amount)
            return primary

        for _ in range(4):
            queue.step(callbacks, primary_for(engine), trigger)
        loaded = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        queue.drain(callbacks, primary_for(engine), trigger)
        loaded.resolution.drain(callbacks, primary_for(loaded), trigger)
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        self.assertEqual(hero.max_hp - 4, hero.hp)

    def test_missing_queue_is_rejected_and_version_28_gets_only_an_empty_queue(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        raw = engine.snapshot()
        del raw["resolution_queue"]
        with self.assertRaises(RuleError):
            GameEngine.from_snapshot(engine.catalog, raw)
        raw["save_version"] = 28
        loaded = GameEngine.from_snapshot(engine.catalog, raw)
        self.assertEqual(engine.state, loaded.state)
        self.assertEqual(engine.resolution.snapshot(), loaded.resolution.snapshot())

    def test_pending_effect_references_are_validated_before_resume(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.resolution.begin()
        engine.resolution.submit(E.DAMAGE, "test:hit", ("missing-actor",), Payload())
        with self.assertRaisesRegex(RuleError, "unknown or repeated actor"):
            GameEngine.from_snapshot(engine.catalog, engine.snapshot())


if __name__ == "__main__":
    unittest.main()
