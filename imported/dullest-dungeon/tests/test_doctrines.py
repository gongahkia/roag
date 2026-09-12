from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class DoctrineRuntimeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def engine_for(self, mode: str, *, combat: bool = True) -> GameEngine:
        doctrine = next(item for item in self.catalog.doctrines.values() if item["mode"] == mode)
        engine = GameEngine.new(self.catalog, 2610, start_in_hub=True)
        compatible_squad = next(
            squad for squad in self.catalog.squads.values()
            if self._squad_compatible(engine, squad["id"], doctrine["id"])
        )
        engine.select_curated_squad(compatible_squad["id"])
        engine.select_doctrine(doctrine["id"])
        engine.begin_expedition()
        if combat:
            engine.start_combat("lost_shift")
        return engine

    @staticmethod
    def _squad_compatible(engine: GameEngine, squad_id: str, doctrine_id: str) -> bool:
        engine.select_curated_squad(squad_id)
        return engine.doctrine_compatible(doctrine_id)

    def card_with(self, engine: GameEngine, opcode: str, *, positive: bool | None = None) -> tuple[str, dict, list[dict]]:
        owners = {hero.id for hero in engine.state.heroes}
        for card_id, definition in self.catalog.cards.items():
            if definition["hero"] not in owners:
                continue
            effects = definition["effects"]
            if any(
                effect["op"] == opcode
                and (positive is None or (int(effect.get("amount", 0)) > 0) == positive)
                for effect in effects
            ):
                return card_id, definition, effects
        self.fail(f"configured party has no {opcode} card")

    def test_mark_wound_and_control_duration_apply_once_per_turn(self) -> None:
        cases = (("mark", "marked"), ("wound", "wound"), ("control", "weak"))
        for mode, status in cases:
            with self.subTest(mode=mode):
                engine = self.engine_for(mode)
                definition = next(card for card in self.catalog.cards.values()
                                  if card["hero"] in {hero.id for hero in engine.state.heroes})
                effect = {"op": "status", "status": status, "amount": 1}
                first = engine._doctrine_modify_effect(definition["id"], definition, effect)
                second = engine._doctrine_modify_effect(definition["id"], definition, effect)
                self.assertEqual(2, first["amount"])
                self.assertEqual(1, second["amount"])

    def test_mark_wound_and_control_liabilities_reduce_their_disclosed_effects(self) -> None:
        cases = (("mark", "damage"), ("wound", "heal"), ("control", "damage"))
        for mode, opcode in cases:
            with self.subTest(mode=mode):
                engine = self.engine_for(mode)
                card_id, definition, _ = self.card_with(engine, opcode)
                if mode == "mark":
                    definition = dict(definition, tags=[tag for tag in definition["tags"] if tag != "payoff:marked"])
                result = engine._doctrine_modify_effect(
                    card_id, definition, {"op": opcode, "amount": 4}
                )
                self.assertEqual(3, result["amount"])

    def test_guard_stress_and_artillery_modify_arithmetic_exactly(self) -> None:
        guard = self.engine_for("guard")
        guard.state.heroes[1].guarded_by = guard.state.heroes[0].id
        definition = self.catalog.cards[self.card_with(guard, "block")[0]]
        self.assertEqual(6, guard._doctrine_modify_effect(
            definition["id"], definition, {"op": "block", "amount": 4}
        )["amount"])

        stress = self.engine_for("stress")
        definition = self.catalog.cards[self.card_with(stress, "stress")[0]]
        self.assertEqual(-2, stress._doctrine_modify_effect(
            definition["id"], definition, {"op": "stress", "amount": -4}
        )["amount"])

        artillery = self.engine_for("artillery")
        definition = self.catalog.cards[self.card_with(artillery, "damage")[0]]
        self.assertEqual(6, artillery._doctrine_modify_effect(
            definition["id"], dict(definition, target="all_enemies"), {"op": "damage", "amount": 4}
        )["amount"])
        self.assertEqual(3, artillery._doctrine_modify_effect(
            definition["id"], dict(definition, target="enemy"), {"op": "damage", "amount": 4}
        )["amount"])

    def test_dance_guard_last_stand_casualty_and_triage_costs_are_explicit(self) -> None:
        dance = self.engine_for("dance")
        card_id, definition, effects = next(
            (card_id, definition, definition["effects"])
            for card_id, definition in self.catalog.cards.items()
            if definition["hero"] in {hero.id for hero in dance.state.heroes}
            and all(effect["op"] != "move" for effect in definition["effects"])
            and definition["cost"] > 0
        )
        card = CardInstance(card_id)
        base = definition["cost"]
        self.assertEqual(base + 1, dance.card_cost(card))
        actor = dance._actor(definition["hero"])
        dance._doctrine_card_committed(actor, card_id, definition, effects)
        self.assertEqual(base, dance.card_cost(card))

        guard = self.engine_for("guard")
        card_id, definition, _ = self.card_with(guard, "move")
        self.assertEqual(definition["cost"] + 1, guard.card_cost(CardInstance(card_id)))

        last = self.engine_for("death_door")
        card_id, definition, _ = next(
            (card_id, definition, definition["effects"])
            for card_id, definition in self.catalog.cards.items()
            if definition["hero"] in {hero.id for hero in last.state.heroes} and definition["cost"] > 0
        )
        owner = last._actor(definition["hero"])
        healthy_cost = last.card_cost(CardInstance(card_id))
        self.assertEqual(definition["cost"] + 1, healthy_cost)
        owner.hp, owner.deaths_door = 0, True
        self.assertEqual(max(0, definition["cost"] - 1), last.card_cost(CardInstance(card_id)))

        casualty = self.engine_for("casualty")
        card_id, definition, _ = next(
            (card_id, definition, definition["effects"])
            for card_id, definition in self.catalog.cards.items()
            if definition["hero"] in {hero.id for hero in casualty.state.heroes}
        )
        self.assertEqual(definition["cost"] + 1, casualty.card_cost(CardInstance(card_id)))

        triage = self.engine_for("triage")
        card_id, definition, effects = self.card_with(triage, "damage")
        triage.state.effect_counters["doctrine:triage:tax"] = 1
        self.assertEqual(definition["cost"] + 1, triage.card_cost(CardInstance(card_id)))
        triage._doctrine_card_committed(triage._actor(definition["hero"]), card_id, definition, effects)
        self.assertNotIn("doctrine:triage:tax", triage.state.effect_counters)

    def test_dance_discard_stress_and_triage_follow_throughs_are_once_per_turn(self) -> None:
        for mode, opcode in (("dance", "move"), ("discard", "discard"), ("stress", "stress"), ("triage", "heal")):
            with self.subTest(mode=mode):
                engine = self.engine_for(mode)
                card_id, definition, effects = self.card_with(
                    engine, opcode, positive=True if mode == "stress" else None
                )
                actor = engine._actor(definition["hero"])
                hand_before, energy_before = len(engine.state.hand), engine.state.energy
                engine._doctrine_after_card(actor, card_id, definition, effects)
                engine._doctrine_after_card(actor, card_id, definition, effects)
                if mode in {"dance", "discard"}:
                    self.assertEqual(hand_before + 1, len(engine.state.hand))
                elif mode == "stress":
                    self.assertEqual(1, actor.statuses.get("focus"))
                else:
                    self.assertEqual(energy_before + 1, engine.state.energy)
                    self.assertEqual(1, engine.state.effect_counters["doctrine:triage:tax"])
                triggers = [row for row in engine.state.ledger.records
                            if row.kind == "doctrine_trigger" and row.data.get("effect") != "cost"]
                self.assertEqual(1, len(triggers))

    def test_discard_opening_hand_is_four(self) -> None:
        engine = self.engine_for("discard")
        self.assertEqual(4, len(engine.state.hand))

    def test_cost_inspection_is_pure_and_trigger_counters_round_trip(self) -> None:
        engine = self.engine_for("dance")
        card = next(
            card for card in engine.state.deck
            if all(effect["op"] != "move" for effect in engine.card_definition(card)["effects"])
        )
        before = engine.snapshot()
        self.assertEqual(engine.card_cost(card), engine.card_cost(card))
        self.assertEqual(before, engine.snapshot())
        definition = engine.card_definition(card)
        actor = engine._actor(definition["hero"])
        engine._doctrine_card_committed(actor, card.card_id, definition, definition["effects"])
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_casualty_drill_blocks_survivors_draws_and_keeps_play_alive(self) -> None:
        engine = self.engine_for("casualty")
        fallen = engine.state.heroes[0]
        fallen.hp = 0
        before = len(engine.state.hand)
        engine._hero_died(fallen)
        survivors = engine.living_heroes()
        self.assertEqual("combat", engine.state.phase)
        self.assertTrue(all(hero.block == 3 for hero in survivors))
        self.assertGreaterEqual(len(engine.state.hand), before - 4)
        trigger = next(row for row in engine.state.ledger.records
                       if row.kind == "doctrine_trigger" and row.data.get("mode") == "casualty")
        self.assertEqual(3, trigger.data["block"])
        self.assertEqual(1, trigger.data["draw"])


if __name__ == "__main__":
    unittest.main()
