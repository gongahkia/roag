from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import ContentError, load_catalog, load_rules
from dumbest_dungeon.engine import GameEngine


class BossPhaseTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_every_guardian_and_finale_primary_has_a_phase_contract(self) -> None:
        boss_encounters = [
            encounter for encounter in self.catalog.encounters.values()
            if encounter["kind"] == "boss"
        ]
        for encounter in boss_encounters:
            primary_id = max(
                encounter["enemies"],
                key=lambda enemy_id: self.catalog.enemies[enemy_id]["max_hp"],
            )
            phases = self.catalog.enemies[primary_id].get("phases", [])
            self.assertTrue(phases, encounter["id"])
            self.assertTrue(all(phase["overflow"] == "carry" for phase in phases))

    def test_phase_seals_one_threshold_and_carries_exact_overflow(self) -> None:
        engine = GameEngine.new(self.catalog, 4501)
        engine.start_combat("base:guardian_derelict", "guardian")
        boss = engine.living_enemies()[0]
        attacker = engine.living_heroes()[0]
        boss.block = 0

        engine._damage(boss, boss.max_hp, attacker)

        self.assertEqual(41, boss.hp)
        self.assertEqual(11, boss.block)
        self.assertEqual(1, boss.statuses["riposte"])
        self.assertEqual(41, engine.state.effect_counters[f"boss:overflow:{boss.id}"])
        transition = next(row for row in engine.state.ledger.records if row.kind == "boss_phase")
        self.assertEqual(41, transition.data["overflow_carried"])
        self.assertTrue(any(row.get("event_type") == "boss_phase" for row in engine.resolution.state.trace))
        self.assertIn("Overflow 41 carried", engine.state.log[-1])
        self.assertIn("carried overflow 41", engine.boss_phase_text())

        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        boss.block = 0
        engine._damage(boss, 1, attacker)
        self.assertEqual(0, boss.hp)
        self.assertNotIn(f"boss:overflow:{boss.id}", engine.state.effect_counters)

    def test_phase_forecast_discloses_threshold_and_overflow_rule(self) -> None:
        engine = GameEngine.new(self.catalog, 4502)
        engine.start_combat("base:guardian_cryogenic", "guardian")
        text = engine.boss_phase_text()
        self.assertIn("PHASE SEAL", text)
        self.assertIn("one threshold per hit", text)
        self.assertIn("exact overflow carries", text)

    def test_phase_contract_rejects_unknown_fields_and_bad_thresholds(self) -> None:
        rules = json.loads(json.dumps(self.catalog.rules))
        phase = rules["enemies"]["base:rusted_admiral"]["phases"][0]
        phase["surprise"] = True
        with self.assertRaises(ContentError):
            load_rules(rules)

        rules = json.loads(json.dumps(self.catalog.rules))
        rules["enemies"]["base:rusted_admiral"]["phases"][0]["threshold_bp"] = 10000
        with self.assertRaises(ContentError):
            load_rules(rules)


if __name__ == "__main__":
    unittest.main()
