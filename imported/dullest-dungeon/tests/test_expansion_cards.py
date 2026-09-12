import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class ExpansionTechniqueRuntimeTests(unittest.TestCase):
    def test_every_expansion_technique_completes_a_legal_queued_play(self) -> None:
        catalog = load_catalog()
        expansion = [card for card in catalog.cards.values() if card.get("design_role")]
        self.assertEqual(100, len(expansion))
        hero_ids = list(catalog.heroes)
        for index, card in enumerate(expansion):
            with self.subTest(card=card["id"]):
                owner_id = card["hero"]
                companions = [hero_id for hero_id in hero_ids if hero_id != owner_id][:3]
                desired_rank = card["from_ranks"][0]
                formation = companions[:]
                formation.insert(desired_rank - 1, owner_id)
                engine = GameEngine.new(catalog, 10_000 + index, start_in_hub=True)
                engine.state.hub_selection = formation
                engine.begin_expedition()
                engine.start_combat(
                    "lost_shift",
                    enemy_ids=["hollow_crew", "hollow_crew", "hollow_crew", "hollow_crew"],
                )
                for rank, hero_id in enumerate(formation, 1):
                    next(hero for hero in engine.state.heroes if hero.id == hero_id).rank = rank
                owner = next(hero for hero in engine.living_heroes() if hero.id == owner_id)
                owner.stress = 60
                if any(effect.get("condition_actor_state") == "deaths_door"
                       for effect in card["effects"]):
                    owner.hp = 0
                    owner.deaths_door = True
                for hero in engine.living_heroes():
                    hero.stress = max(hero.stress, 60)
                    hero.statuses["wound"] = 2
                    if any(effect.get("condition_target_state") == "deaths_door"
                           for effect in card["effects"]):
                        hero.hp = 0
                        hero.deaths_door = True
                for enemy in engine.living_enemies():
                    enemy.statuses.update({
                        "marked": 2,
                        "vulnerable": 2,
                        "weak": 2,
                        "wound": 2,
                    })
                engine.state.hand = [CardInstance(card["id"])]
                engine.state.draw_pile = []
                engine.state.discard_pile = []
                engine.state.energy = 99
                targets = engine.valid_targets(0)
                self.assertTrue(targets)
                engine.play_card(0, targets[0])
                self.assertIsNone(engine.resolution.state.root_id)
                self.assertTrue(any(record.kind == "card_play" and record.source_id == card["id"]
                                    for record in engine.state.ledger.records))


if __name__ == "__main__":
    unittest.main()
