import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.contracts import Opcode
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.resolution import Payload
from dumbest_dungeon.triggers import EventType


class MutationRuntimeTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()

    def engine(self, module: str, *, enemies: list[str] | None = None) -> GameEngine:
        engine = GameEngine.new(self.catalog, 907)
        engine.start_combat("lost_shift", enemy_ids=enemies)
        engine.state.encounter_modules = [module]
        return engine

    def test_third_bell_reacts_exactly_once_on_the_third_shared_card(self) -> None:
        engine = self.engine("base:third_bell")
        owner = engine.living_heroes()[0]
        card = next(card for card in self.catalog.cards.values() if card["hero"] == owner.id)
        target = engine.living_enemies()[0]
        for plays in (1, 2, 3, 4):
            engine.state.effect_counters[f"round_cards:{owner.id}"] = plays
            engine.state.effect_counters[f"combat_cards:{owner.id}"] = plays
            engine.resolution.begin(card_token=card["id"], combat_token=1, turn_token=1)
            engine.resolution.submit(
                EventType.CARD_PLAY,
                card["id"],
                (target.id,),
                Payload(actor_id=owner.id, card_id=card["id"], effect_index=0),
            )
            engine.resolve_pending()
        self.assertEqual(5, target.block)
        reactions = [row for row in engine.state.ledger.records
                     if row.kind == "mutation_reaction" and row.source_id == "base:third_bell"]
        self.assertEqual(1, len(reactions))

    def test_death_surge_is_queued_and_root_limited(self) -> None:
        engine = self.engine("base:death_surge")
        victim, survivor = engine.living_enemies()
        engine._damage(victim, victim.hp, engine.living_heroes()[0])
        self.assertEqual(1, survivor.statuses.get("focus"))
        reaction = next(row for row in engine.state.ledger.records
                        if row.kind == "mutation_reaction")
        root = next(row for row in reversed(engine.state.ledger.records)
                    if row.kind == "resolution_root")
        status_event = next(row for row in root.data["trace"]
                            if row.get("source_id") == "base:death_surge"
                            and row.get("event_type") == "status")
        self.assertEqual(reaction.data["event_id"], status_event["parent_event_id"])

    def test_spore_link_wounds_front_crew_after_enemy_death(self) -> None:
        engine = self.engine("biome:spore_link")
        victim = engine.living_enemies()[0]
        hero = engine.living_heroes()[0]
        engine._damage(victim, victim.hp, hero)
        self.assertEqual(1, hero.statuses.get("wound"))
        self.assertTrue(any(row.source_id == "biome:spore_link" and row.kind == "status"
                            for row in engine.state.ledger.records))

    def test_rime_shell_prevents_only_the_first_stun_event(self) -> None:
        engine = self.engine("biome:rime_shell")
        hero, enemy = engine.living_heroes()[0], engine.living_enemies()[0]
        for _ in range(2):
            engine._apply_effect(
                hero,
                [enemy],
                {"op": Opcode.STATUS.value, "status": "stun", "amount": 2},
                source_id="test:stun",
            )
        self.assertEqual(2, enemy.statuses.get("stun"))
        reactions = [row for row in engine.state.ledger.records
                     if row.kind == "mutation_reaction" and row.source_id == "biome:rime_shell"]
        self.assertEqual(1, len(reactions))

    def test_triage_node_repairs_only_the_weakest_hostile(self) -> None:
        engine = self.engine("base:triage_node")
        first, second = engine.living_enemies()
        first.hp -= 7
        second.hp -= 2
        engine._apply_enemy_phase_mutations()
        self.assertEqual(first.max_hp - 4, first.hp)
        self.assertEqual(second.max_hp - 2, second.hp)
        self.assertTrue(any(row.kind == "healing" and row.source_id == "base:triage_node"
                            for row in engine.state.ledger.records))

    def test_clean_room_removes_one_status_in_canonical_order(self) -> None:
        engine = self.engine("base:clean_room")
        target = engine.living_enemies()[0]
        target.hp -= 5
        target.statuses.update({"marked": 2, "wound": 3})
        engine._apply_enemy_phase_mutations()
        self.assertNotIn("marked", target.statuses)
        self.assertEqual(3, target.statuses["wound"])
        record = next(row for row in engine.state.ledger.records
                      if row.kind == "cleanse" and row.source_id == "base:clean_room")
        self.assertEqual("marked", record.data["status"])

    def test_undertow_moves_only_the_front_crew_one_rank(self) -> None:
        engine = self.engine("biome:undertow")
        front, second = engine.living_heroes()[:2]
        engine._apply_enemy_phase_mutations()
        self.assertEqual((2, 1), (front.rank, second.rank))
        self.assertTrue(any(row.kind == "movement" and row.source_id == "biome:undertow"
                            for row in engine.state.ledger.records))

    def test_slag_vent_wounds_only_unblocked_crew(self) -> None:
        engine = self.engine("biome:slag_vent")
        heroes = engine.living_heroes()
        heroes[1].block = 1
        engine._apply_enemy_phase_mutations()
        self.assertEqual([1, None, 1, 1], [hero.statuses.get("wound") for hero in heroes])
        records = [row for row in engine.state.ledger.records
                   if row.kind == "status" and row.source_id == "biome:slag_vent"]
        self.assertEqual(3, len(records))


if __name__ == "__main__":
    unittest.main()
