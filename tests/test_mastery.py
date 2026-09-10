from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError


class MasteryTests(unittest.TestCase):
    def mastered_engine(self, branch: str) -> tuple[GameEngine, int]:
        engine = GameEngine.new(load_catalog(), 510, start_in_hub=True)
        engine.begin_expedition()
        card = engine._new_card("crossguard", upgraded=True)
        engine.state.deck.append(card)
        index = len(engine.state.deck) - 1
        engine.state.phase = "service"
        engine.state.service_type = "upgrade"
        engine.service("mastery", index, mastery_branch=branch)
        return engine, card.copy_id

    @staticmethod
    def put_copy_in_hand(engine: GameEngine, copy_id: int) -> None:
        selected = next(card for card in engine.state.deck if card.copy_id == copy_id)
        engine.state.hand = [engine._clone_card(selected)]
        engine.state.draw_pile = []
        engine.state.discard_pile = [
            engine._clone_card(card)
            for card in engine.state.deck
            if card.copy_id != copy_id
        ]

    def test_engine_branch_applies_only_its_authored_effect_and_checkpoints(self) -> None:
        engine, copy_id = self.mastered_engine("engine")
        engine.start_combat("lost_shift")
        self.put_copy_in_hand(engine, copy_id)
        owner = next(hero for hero in engine.state.heroes if hero.id == "warden")
        owner.rank = 1
        owner.block = 0
        target = next(hero for hero in engine.living_heroes() if hero.id != owner.id)
        engine.play_card(0, target.id, resolve=False)
        restored = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        engine.resolve_pending()
        restored.resolve_pending()
        self.assertEqual(engine.snapshot(), restored.snapshot())
        self.assertEqual(12, owner.block)
        effects = [row for row in engine.state.ledger.records if row.kind == "mastery_effect"]
        self.assertEqual([("base:crossguard", 3)], [(row.source_id, row.data["amount"]) for row in effects])
        play = next(row for row in engine.state.ledger.records
                    if row.kind == "card_play" and row.source_id == "crossguard")
        self.assertEqual(copy_id, play.data["copy_id"])
        self.assertEqual("engine", play.data["mastery"])

    def test_coverage_branch_widens_only_origin_access(self) -> None:
        engine, copy_id = self.mastered_engine("coverage")
        card = next(card for card in engine.state.deck if card.copy_id == copy_id)
        self.assertEqual((1, 2, 3), engine.card_origin_ranks(card))
        engine.start_combat("lost_shift")
        self.put_copy_in_hand(engine, copy_id)
        owner = next(hero for hero in engine.state.heroes if hero.id == "warden")
        other = next(hero for hero in engine.living_heroes() if hero.id != owner.id and hero.rank == 3)
        owner.rank, other.rank = 3, owner.rank
        target = next(hero for hero in engine.living_heroes() if hero.id != owner.id)
        engine.play_card(0, target.id)
        self.assertEqual("combat", engine.state.phase)
        self.assertFalse(any(row.kind == "mastery_effect" for row in engine.state.ledger.records))

    def test_choice_is_irreversible_and_transform_discloses_then_clears_it(self) -> None:
        engine, copy_id = self.mastered_engine("engine")
        index = next(index for index, card in enumerate(engine.state.deck)
                     if card.copy_id == copy_id)
        engine.state.phase = "service"
        engine.state.service_type = "upgrade"
        with self.assertRaisesRegex(RuleError, "not eligible"):
            engine.service("mastery", index, mastery_branch="coverage")
        engine.state.phase = "service"
        engine.state.service_type = "upgrade"
        replacement = engine.transformation_options(index)[0]
        engine.service("transform", index, replacement_id=replacement)
        transformed = engine.state.deck[index]
        self.assertEqual(copy_id, transformed.copy_id)
        self.assertFalse(transformed.upgraded)
        self.assertIsNone(transformed.mastery)
        event = next(row for row in engine.state.ledger.records if row.kind == "card_transformed")
        self.assertEqual("engine", event.data["lost_mastery"])

    def test_save_rejects_unknown_or_unupgraded_mastery_and_bad_payload(self) -> None:
        engine, copy_id = self.mastered_engine("engine")
        raw = engine.snapshot()
        card = next(card for card in raw["state"]["deck"] if card["copy_id"] == copy_id)
        for field, value in (("mastery", "unknown"), ("upgraded", False)):
            broken = json.loads(json.dumps(raw))
            target = next(item for item in broken["state"]["deck"] if item["copy_id"] == copy_id)
            target[field] = value
            with self.subTest(field=field), self.assertRaisesRegex(RuleError, "card-copy"):
                GameEngine.from_snapshot(engine.catalog, broken)

        engine.start_combat("lost_shift")
        self.put_copy_in_hand(engine, copy_id)
        owner = next(hero for hero in engine.state.heroes if hero.id == "warden")
        owner.rank = 1
        target = next(hero for hero in engine.living_heroes() if hero.id != owner.id)
        engine.play_card(0, target.id, resolve=False)
        broken = engine.snapshot()
        broken["resolution_queue"]["state"]["pending"][0]["payload"]["card_mastery"] = "unknown"
        with self.assertRaisesRegex(RuleError, "owned card continuation"):
            GameEngine.from_snapshot(engine.catalog, broken)


if __name__ == "__main__":
    unittest.main()
