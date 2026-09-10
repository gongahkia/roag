from __future__ import annotations

import json
import unittest
from copy import deepcopy

from dumbest_dungeon.content import load_catalog, load_legacy_catalog
from dumbest_dungeon.contracts import Opcode
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.migrations import MigrationError, migrate_run, run_29_to_30
from dumbest_dungeon.resolution import Payload
from dumbest_dungeon.triggers import EventType as E


class ResolutionSaveTests(unittest.TestCase):
    def test_raw_damage_payload_migration_is_pure_and_strict(self) -> None:
        engine = GameEngine.new(load_legacy_catalog(), 42)
        hero = engine.living_heroes()[0]
        engine.resolution.begin()
        engine.resolution.submit(E.DAMAGE, "test:hit", (hero.id,), Payload(actor_id=hero.id, opcode=Opcode.DAMAGE, amount=1))
        old = engine.snapshot()
        old["save_version"] = 29
        del old["content_rules"]
        for field in (
            "pressure", "pressure_recent", "pressure_incomplete_before_tick",
            "encounter_pressure", "encounter_modules", "reinforcement_tickets",
        ):
            del old["state"][field]
        old["content_manifest"]["engine"] = "0.1.0"
        old["content_manifest"]["rng_architecture"] = 1
        old["resolution_queue"]["state"]["schema"] = 1
        event = old["resolution_queue"]["state"]["pending"][0]
        del event["deferred"]
        for key in ("raw_damage", "card_upgraded", "effect_index"):
            del event["payload"][key]
        unchanged = deepcopy(old)
        migrated = run_29_to_30(old)
        self.assertEqual(unchanged, old)
        self.assertEqual(30, migrated["save_version"])
        self.assertIsNone(migrate_run(old)["content_rules"])
        restored = GameEngine.from_snapshot(engine.catalog, old).snapshot()
        expected = engine.snapshot()
        expected["state"]["pressure_incomplete_before_tick"] = engine.state.travel_ticks
        self.assertEqual(expected, restored)
        with self.assertRaises(MigrationError):
            run_29_to_30(migrated)
        malformed = engine.snapshot()
        del malformed["resolution_queue"]["state"]["pending"][0]["payload"]["raw_damage"]
        with self.assertRaisesRegex(RuleError, "payload fields"):
            GameEngine.from_snapshot(engine.catalog, malformed)

    def test_live_riposte_checkpoint_retains_listener_snapshot_and_exclusion(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.start_combat("lost_shift")
        hero, enemy = engine.living_heroes()[0], engine.living_enemies()[0]
        hero.statuses["riposte"] = enemy.statuses["riposte"] = 2
        hero.block = 0
        enemy.statuses["dodge"] = 2
        enemy_hp = enemy.hp
        queue = engine.resolution
        queue.begin(combat_token=1, turn_token=1)
        queue.submit(E.DAMAGE, "test:hit", (hero.id,), Payload(actor_id=enemy.id, opcode=Opcode.DAMAGE, amount=1, raw_damage=True))
        for _ in range(3):
            queue.step(engine._resolution_listeners, engine._resolve_event, engine._resolve_trigger)
        loaded = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        engine.resolve_pending()
        loaded.resolve_pending()
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        self.assertEqual(enemy_hp - 4, enemy.hp)
        self.assertEqual(2, enemy.statuses["dodge"])
        hits = [row for row in engine.state.ledger.records if row.kind == "damage"]
        self.assertEqual(2, len(hits))
        self.assertEqual("status:riposte:" + hero.id, hits[-1].source_id)
        self.assertEqual(1, hits[-1].data["depth"])
        self.assertEqual(hits[0].data["root_action_id"], hits[-1].data["root_action_id"])

    def test_guard_riposte_uses_actual_defender_and_no_counter_on_absorbed_damage(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.start_combat("lost_shift")
        guard, protected = engine.living_heroes()[:2]
        enemy = engine.living_enemies()[0]
        protected.guarded_by = guard.id
        protected.guard_turns = 2
        guard.statuses["riposte"] = protected.statuses["riposte"] = 2
        guard.block = 4
        hp = enemy.hp
        engine._damage(protected, 4, enemy)
        self.assertEqual(hp, enemy.hp)
        engine._damage(protected, 1, enemy)
        self.assertEqual(hp - 4, enemy.hp)
        hits = [row for row in engine.state.ledger.records if row.source_id.startswith("status:riposte:") and row.kind == "damage"]
        self.assertEqual(["status:riposte:" + guard.id], [row.source_id for row in hits])

    def test_unknown_saved_listener_is_rejected(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        hero = engine.living_heroes()[0]
        hero.statuses["riposte"] = 2
        queue = engine.resolution
        queue.begin()
        queue.submit(E.DAMAGE, "test:hit", (hero.id,), Payload(opcode=Opcode.DAMAGE, amount=1, raw_damage=True))
        queue.step(engine._resolution_listeners, engine._resolve_event, engine._resolve_trigger)
        snapshot = engine.snapshot()
        snapshot["resolution_queue"]["state"]["active"]["listeners"][0]["spec"]["id"] = "unknown:trigger"
        with self.assertRaisesRegex(RuleError, "unregistered listener"):
            GameEngine.from_snapshot(engine.catalog, snapshot)

    def test_missing_queue_is_rejected_and_version_28_gets_only_an_empty_queue(self) -> None:
        engine = GameEngine.new(load_legacy_catalog(), 42)
        raw = engine.snapshot()
        del raw["resolution_queue"]
        with self.assertRaises(RuleError):
            GameEngine.from_snapshot(engine.catalog, raw)
        raw["save_version"] = 28
        del raw["content_rules"]
        for field in (
            "pressure", "pressure_recent", "pressure_incomplete_before_tick",
            "encounter_pressure", "encounter_modules", "reinforcement_tickets",
        ):
            del raw["state"][field]
        raw["content_manifest"]["engine"] = "0.1.0"
        raw["content_manifest"]["rng_architecture"] = 1
        loaded = GameEngine.from_snapshot(engine.catalog, raw)
        expected = deepcopy(engine.state)
        expected.pressure_incomplete_before_tick = engine.state.travel_ticks
        self.assertEqual(expected, loaded.state)
        self.assertEqual(engine.resolution.snapshot(), loaded.resolution.snapshot())

    def test_pending_effect_references_are_validated_before_resume(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.resolution.begin()
        engine.resolution.submit(E.DAMAGE, "test:hit", ("missing-actor",), Payload())
        with self.assertRaisesRegex(RuleError, "unknown or repeated actor"):
            GameEngine.from_snapshot(engine.catalog, engine.snapshot())

    def test_compound_effect_skips_targets_killed_by_its_previous_effect(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.start_combat("lost_shift")
        actor = engine.living_heroes()[0]
        targets = engine.living_enemies()
        dead = targets[0]
        dead.hp = 0
        engine._apply_effect(actor, targets, {"op": "status", "status": "vulnerable", "amount": 2})
        self.assertNotIn("vulnerable", dead.statuses)
        self.assertTrue(all(enemy.statuses.get("vulnerable") == 2 for enemy in targets[1:]))
        self.assertIsNone(engine.resolution.state.root_id)

    def test_live_primary_effect_resumes_through_the_engine_handler(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        hero = engine.living_heroes()[0]
        hero.hp -= 5
        engine.resolution.begin(card_token="brace")
        engine.resolution.submit(E.HEAL, "test:recover", (hero.id,), Payload(actor_id=hero.id, opcode=Opcode.HEAL, amount=3))
        engine.resolution.step(engine._resolution_listeners, engine._resolve_event, engine._resolve_trigger)
        loaded = GameEngine.from_snapshot(engine.catalog, engine.snapshot())
        engine.resolve_pending()
        loaded.resolve_pending()
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        self.assertEqual(hero.max_hp - 2, hero.hp)


if __name__ == "__main__":
    unittest.main()
