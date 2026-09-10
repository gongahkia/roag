from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError


class InfusionTests(unittest.TestCase):
    def fixture(self, card_id: str, infusion_id: str, *, pressure: int = 0):
        engine = GameEngine.new(load_catalog(), 620, start_in_hub=True)
        engine.begin_expedition()
        card = engine._new_card(card_id)
        card.infusion_id = infusion_id
        engine.state.deck.append(card)
        engine.state.pressure = pressure
        engine.start_combat("lost_shift")
        self.put_in_hand(engine, card.copy_id)
        owner_id = engine.catalog.cards[card_id]["hero"]
        owner = next(hero for hero in engine.state.heroes if hero.id == owner_id)
        desired = engine.card_origin_ranks(card)[0]
        occupant = next((hero for hero in engine.living_heroes()
                         if hero.id != owner.id and hero.rank == desired), None)
        if occupant is not None:
            owner.rank, occupant.rank = occupant.rank, owner.rank
        return engine, card.copy_id, owner

    @staticmethod
    def put_in_hand(engine: GameEngine, copy_id: int) -> None:
        selected = next(card for card in engine.state.deck if card.copy_id == copy_id)
        engine.state.hand = [engine._clone_card(selected)]
        engine.state.draw_pile = []
        engine.state.discard_pile = [
            engine._clone_card(card) for card in engine.state.deck
            if card.copy_id != copy_id
        ]
        engine.state.energy = 20

    @staticmethod
    def enemy(engine: GameEngine):
        target = engine.living_enemies()[0]
        target.block = 0
        target.statuses.clear()
        return target

    def test_every_definition_has_eligible_cards_and_one_copy_accepts_only_one(self) -> None:
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 621)
        for infusion_id in catalog.infusions:
            self.assertTrue(
                any(engine.infusion_compatible(card_id, infusion_id) for card_id in catalog.cards),
                infusion_id,
            )
        card = engine.state.deck[0]
        index = 0
        engine.state.phase = "service"
        engine.state.service_type = "upgrade"
        offered = engine.infusion_options(index)
        engine.service("infusion", index, infusion_id=offered[0])
        self.assertEqual(offered[0], card.infusion_id)
        record = next(row for row in engine.state.ledger.records if row.kind == "card_infused")
        self.assertEqual(offered, record.data["offered"])
        engine.state.phase = "service"
        engine.state.service_type = "upgrade"
        with self.assertRaisesRegex(RuleError, "cannot receive|offered"):
            engine.service("infusion", index, infusion_id=offered[-1])

    def test_retain_exhaust_opening_and_rank_access_change_card_zones(self) -> None:
        retained, copy_id, _ = self.fixture("brace", "base:memory_lacquer")
        retained.end_turn()
        self.assertTrue(any(card.copy_id == copy_id for card in retained.state.hand))

        exhausted, copy_id, _ = self.fixture("baton_strike", "base:flash_paper")
        target = self.enemy(exhausted)
        exhausted.play_card(0, target.id)
        exhausted.state.draw_pile = []
        exhausted._draw_primary(100)
        self.assertTrue(any(card.copy_id == copy_id for card in exhausted.state.discard_pile))
        self.assertFalse(any(card.copy_id == copy_id for card in exhausted.state.hand))

        ranked, copy_id, owner = self.fixture("brace", "base:hinged_grip")
        self.assertEqual((1, 2, 3, 4), ranked.card_origin_ranks(ranked.state.hand[0]))
        owner.rank = 4
        ranked.play_card(0, owner.id)

        opening = GameEngine.new(load_catalog(), 622)
        card = opening._new_card("crossguard")
        card.infusion_id = "base:first_index"
        opening.state.deck.append(card)
        opening.start_combat("lost_shift")
        self.assertEqual(card.copy_id, opening.state.hand[0].copy_id)

    def test_four_conditional_discounts_are_scoped_and_never_negative(self) -> None:
        cases = (
            ("baton_strike", "base:breach_contact", lambda engine, owner: setattr(owner, "rank", 1)),
            ("snap_shot", "base:rear_bus", lambda engine, owner: setattr(owner, "rank", 4)),
            ("brace", "base:suture_wire", lambda engine, owner: owner.statuses.__setitem__("wound", 2)),
            ("brace", "base:empty_chair", lambda engine, owner: setattr(engine.state.heroes[-1], "hp", 0)),
        )
        for card_id, infusion_id, condition in cases:
            engine, _, owner = self.fixture(card_id, infusion_id)
            card = engine.state.hand[0]
            baseline = engine.catalog.cards[card_id]["cost"]
            condition(engine, owner)
            self.assertEqual(max(0, baseline - 1), engine.card_cost(card), infusion_id)

    def test_echo_draw_movement_cleanse_and_front_focus_use_finite_limits(self) -> None:
        echo, copy_id, _ = self.fixture("baton_strike", "base:carbon_copy")
        target = self.enemy(echo)
        target.max_hp = target.hp = 50
        hp = target.hp
        echo.play_card(0, target.id)
        self.assertEqual(hp - 14, target.hp)
        echoed = [row for row in echo.state.ledger.records
                  if row.kind == "infusion_trigger" and row.source_id == "base:carbon_copy"]
        self.assertEqual(1, len(echoed))
        card = next(card for card in echo.state.discard_pile if card.copy_id == copy_id)
        echo.state.discard_pile.remove(card)
        echo.state.hand = [card]
        echo.state.energy = 20
        hp = target.hp
        echo.play_card(0, target.id)
        self.assertEqual(hp - 7, target.hp)

        drawing, _, _ = self.fixture("brace", "base:followthrough_ink")
        before = len(drawing.state.hand)
        drawing.play_card(0, drawing.living_heroes()[0].id)
        self.assertEqual(before, len(drawing.state.hand))

        moving, _, owner = self.fixture("shield_rush", "base:kinetic_return")
        target = self.enemy(moving)
        energy = moving.state.energy
        moving.play_card(0, target.id)
        self.assertEqual(energy, moving.state.energy)

        cleansing, _, owner = self.fixture("brace", "base:sterile_edge")
        owner.statuses["weak"] = 2
        cleansing.play_card(0, owner.id)
        self.assertNotIn("weak", owner.statuses)

        focusing, _, owner = self.fixture("baton_strike", "base:point_lead")
        owner.rank = 1
        focusing.play_card(0, self.enemy(focusing).id)
        self.assertEqual(1, owner.statuses["focus"])

    def test_mark_transfer_and_pressure_bonus_are_source_attributed(self) -> None:
        marking, _, _ = self.fixture("baton_strike", "base:tracer_salt")
        target = self.enemy(marking)
        marking.play_card(0, target.id)
        self.assertEqual(2, target.statuses["marked"])

        transfer, _, owner = self.fixture("crossguard", "base:burden_thread")
        ally = next(hero for hero in transfer.living_heroes() if hero.id != owner.id)
        ally.statuses["wound"] = 2
        transfer.play_card(0, ally.id)
        self.assertEqual(1, ally.statuses["wound"])
        self.assertEqual(1, owner.statuses["wound"])

        pressured, _, _ = self.fixture("baton_strike", "base:hunted_alloy", pressure=480)
        target = self.enemy(pressured)
        hp = target.hp
        pressured.play_card(0, target.id)
        self.assertEqual(hp - 10, target.hp)
        trigger = next(row for row in pressured.state.ledger.records
                       if row.kind == "infusion_trigger")
        self.assertEqual("base:hunted_alloy", trigger.source_id)

    def test_save_and_transform_preserve_or_explicitly_clear_copy_modifier(self) -> None:
        engine, copy_id, _ = self.fixture("crossguard", "base:memory_lacquer")
        restored = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        self.assertEqual(engine.snapshot(), restored.snapshot())
        engine.state.phase = "service"
        engine.state.service_type = "upgrade"
        index = next(index for index, card in enumerate(engine.state.deck)
                     if card.copy_id == copy_id)
        replacement = engine.transformation_options(index)[0]
        engine.service("transform", index, replacement_id=replacement)
        self.assertIsNone(engine.state.deck[index].infusion_id)
        event = next(row for row in engine.state.ledger.records if row.kind == "card_transformed")
        self.assertEqual("base:memory_lacquer", event.data["lost_infusion"])

    def test_pending_infusion_and_invalid_copy_modifiers_are_strictly_saved(self) -> None:
        engine, copy_id, _ = self.fixture("baton_strike", "base:carbon_copy")
        target = self.enemy(engine)
        engine.play_card(0, target.id, resolve=False)
        restored = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        engine.resolve_pending()
        restored.resolve_pending()
        self.assertEqual(engine.snapshot(), restored.snapshot())

        bad = GameEngine.new(load_catalog(), 623)
        bad.start_combat("lost_shift")
        copy_id = bad.state.hand[0].copy_id
        raw = bad.snapshot()
        for zone in ("deck", "hand", "draw_pile", "discard_pile"):
            for card in raw["state"][zone]:
                if card["copy_id"] == copy_id:
                    card["infusion_id"] = "base:missing"
        with self.assertRaisesRegex(RuleError, "card-copy"):
            GameEngine.from_snapshot(bad.catalog, raw)


if __name__ == "__main__":
    unittest.main()
