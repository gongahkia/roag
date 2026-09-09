from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine, RuleError
from dumbest_dungeon.telemetry import RunLedger, decision_counts


class LedgerTests(unittest.TestCase):
    def test_records_are_ordered_detached_and_round_trip_without_rng_calls(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        random_state = engine.rng.getstate()
        count = len(engine.state.ledger.records)
        payload = ["brace", "breach"]
        engine.record("card_offer", "reward", cards=payload)
        payload.clear()
        engine.record("card_pick", "reward", card="brace")
        self.assertEqual([count + 1, count + 2], [row.sequence for row in engine.state.ledger.records[-2:]])
        self.assertEqual(["brace", "breach"], engine.state.ledger.records[-2].data["cards"])
        self.assertEqual(random_state, engine.rng.getstate())
        loaded = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_missing_or_misordered_records_are_rejected(self) -> None:
        ledger = RunLedger()
        ledger.record("card_play", "brace", 0, 1, owner="warden")
        raw = ledger.snapshot()
        raw["records"][0]["sequence"] = 2
        with self.assertRaisesRegex(ValueError, "invalid run record"):
            RunLedger.from_snapshot(raw)
        engine = GameEngine.new(load_catalog(), 42)
        snapshot = engine.snapshot()
        del snapshot["state"]["ledger"]
        with self.assertRaises(RuleError):
            GameEngine.from_snapshot(engine.catalog, snapshot)

    def test_version_27_migration_discloses_unrecorded_history(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        raw = engine.snapshot()
        raw["save_version"] = 27
        del raw["state"]["ledger"]
        loaded = GameEngine.from_snapshot(engine.catalog, raw)
        self.assertEqual(0, loaded.state.ledger.incomplete_before_tick)
        self.assertEqual([], loaded.state.ledger.records)

    def test_offer_skip_and_play_are_distinct_recorded_signals(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        offered = engine._generate_card_rewards(3)
        engine.state.phase = "reward"
        engine.state.rewards = offered
        engine.choose_reward(None)
        counts = decision_counts(engine.state.ledger)
        for identity in offered:
            self.assertEqual({"offered": 1, "skipped": 1}, counts[identity])
        engine.start_combat("lost_shift")
        engine.state.hand = [CardInstance("brace")]
        engine.play_card(0, "warden")
        self.assertEqual(1, decision_counts(engine.state.ledger)["brace"]["play"])
        before = engine.state.ledger.snapshot()
        with self.assertRaises(RuleError):
            engine.play_card(99)
        self.assertEqual(before, engine.state.ledger.snapshot())

    def test_owner_death_records_deck_copies_once_and_continuation(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        hero = engine.living_heroes()[0]
        owned = [card.card_id for card in engine.state.deck if engine.catalog.cards[card.card_id]["hero"] == hero.id]
        engine.start_combat("lost_shift")
        hero.hp = 0
        hero.deaths_door = False
        engine._hero_died(hero)
        lost = [row.source_id for row in engine.state.ledger.records if row.kind == "card_lost"]
        self.assertEqual(sorted(owned), sorted(lost))
        death = next(row for row in engine.state.ledger.records if row.kind == "crew_death")
        self.assertEqual(3, len(death.data["survivors"]))
        self.assertEqual("combat", engine.state.phase)

    def test_damage_records_absorption_overkill_and_exact_source_without_new_randomness(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.start_combat("lost_shift")
        enemy = engine.living_enemies()[0]
        enemy.hp, enemy.block = 10, 3
        random_state = engine.rng.getstate()
        with engine.attribution("test:explosive"):
            engine._damage(enemy, 20, engine.living_heroes()[0])
        record = next(row for row in reversed(engine.state.ledger.records) if row.kind == "damage")
        self.assertEqual("test:explosive", record.source_id)
        self.assertEqual((3, 10, 7), (record.data["absorbed"], record.data["hp_loss"], record.data["overkill"]))
        self.assertEqual(random_state, engine.rng.getstate())

    def test_healing_and_status_refresh_report_actual_results(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        hero = engine.living_heroes()[0]
        hero.hp -= 2
        with engine.attribution("test:recovery"):
            engine._heal(hero, 10)
            engine._add_status(hero, "focus", 3)
            engine._add_status(hero, "focus", 1)
        healing = next(row for row in engine.state.ledger.records if row.kind == "healing")
        self.assertEqual((2, 8), (healing.data["amount"], healing.data["overheal"]))
        refresh = engine.state.ledger.records[-1]
        self.assertEqual((3, 1, 3), (refresh.data["previous"], refresh.data["amount"], refresh.data["result"]))

    def test_card_and_enemy_effects_keep_distinct_sources(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.start_combat("lost_shift")
        engine.state.hand = [CardInstance("baton_strike")]
        engine.play_card(0, engine.living_enemies()[0].id)
        engine.end_turn()
        sources = {row.source_id for row in engine.state.ledger.records if row.kind == "damage"}
        self.assertIn("baton_strike", sources)
        self.assertTrue(any("/" in source for source in sources))

    def test_route_inspection_does_not_record_world_actions(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        before = engine.snapshot()
        route = engine._find_path((engine.state.party_x, engine.state.party_y), engine.room_position(1))
        engine.route_intel(route)
        self.assertEqual(before, engine.snapshot())
        engine.step_exploration(*route[0])
        record = next(row for row in engine.state.ledger.records if row.kind == "travel")
        self.assertEqual(list(route[0]), record.data["position"])
        self.assertEqual(engine.state.travel_ticks, record.data["ticks"])


if __name__ == "__main__":
    unittest.main()
