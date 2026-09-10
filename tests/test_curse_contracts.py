from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine


class CurseContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def engine_with(self, curse_id: str) -> tuple[GameEngine, object, object]:
        engine = GameEngine.new(self.catalog, 3110)
        engine.start_combat("lost_shift")
        owner = engine.living_heroes()[0]
        target = engine.living_enemies()[0]
        engine.acquire_curse(owner.id, curse_id)
        return engine, owner, target

    def test_conditional_power_is_inert_until_its_disclosed_state(self) -> None:
        cases = (
            ("base:red_debt", lambda engine, owner, target: setattr(owner, "stress", 50), 11),
            ("base:scar_appetite", lambda engine, owner, target: owner.statuses.update(wound=2), 11),
            ("base:quarry_oath", lambda engine, owner, target: target.statuses.update(marked=2), 11),
            ("base:blackout_pact", lambda engine, owner, target: setattr(engine.state, "light", 0), 11),
            ("base:hunted_compact", lambda engine, owner, target: setattr(engine.state, "pressure", 480), 12),
        )
        for curse_id, activate, expected in cases:
            with self.subTest(curse=curse_id):
                engine, owner, target = self.engine_with(curse_id)
                self.assertEqual(10, engine._outgoing_damage(owner, 10, target))
                activate(engine, owner, target)
                self.assertEqual(expected, engine._outgoing_damage(owner, 10, target))
                trigger = next(row for row in reversed(engine.state.ledger.records)
                               if row.kind == "curse_contract_trigger")
                self.assertEqual(curse_id, trigger.source_id)

    def test_last_bastion_adds_block_only_at_deaths_door(self) -> None:
        engine, owner, _ = self.engine_with("base:last_bastion_debt")
        owner.block = 0
        engine._apply_effect(owner, [owner], {"op": "block", "amount": 4}, source_id="test:block")
        self.assertEqual(4, owner.block)
        owner.block = 0
        owner.hp, owner.deaths_door = 0, True
        engine._apply_effect(owner, [owner], {"op": "block", "amount": 4}, source_id="test:block")
        self.assertEqual(7, owner.block)
        trigger = next(row for row in reversed(engine.state.ledger.records)
                       if row.kind == "curse_contract_trigger")
        self.assertEqual("base:last_bastion_debt", trigger.source_id)

    def test_contract_power_is_unique_while_burdens_continue_to_stack(self) -> None:
        engine, owner, _ = self.engine_with("base:red_debt")
        engine.acquire_curse(owner.id, "base:red_debt")
        curse = self.catalog.curses["base:red_debt"]
        power, burden = curse["effects"]
        self.assertEqual(power.contract.value(1), power.contract.value(2))
        self.assertGreater(burden.contract.value(2), burden.contract.value(1))
        detail = engine.effect_description("curse", "base:red_debt", 2)
        self.assertIn("Current:", detail)
        self.assertIn("next:", detail)
        self.assertIn("descendants", detail)


if __name__ == "__main__":
    unittest.main()
