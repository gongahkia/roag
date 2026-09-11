import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.pressure import pressure_band


class ThreeWayInteractionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_trigger_stack_and_owner_death_matrix(self) -> None:
        for stacks in (1, 3):
            for casualty_id in ("warden", "scout"):
                with self.subTest(stacks=stacks, casualty=casualty_id):
                    engine = GameEngine.new(self.catalog, 700 + stacks)
                    scout = next(hero for hero in engine.state.heroes if hero.id == "scout")
                    for _ in range(stacks):
                        engine.acquire_boon(scout.id, "resonant_circuit")
                    engine.state.deck.extend(engine._new_card("scan") for _ in range(3))
                    engine.state.active_modifiers = ["third_card_reaction"]
                    engine.start_combat("lost_shift")
                    self.assertIn("base:third_bell", engine.state.encounter_modules)
                    casualty = next(hero for hero in engine.state.heroes if hero.id == casualty_id)
                    engine._hero_died(casualty)

                    if casualty_id == "scout":
                        self.assertFalse(any(
                            card.card_id == "scan"
                            for zone in (engine.state.deck, engine.state.hand,
                                         engine.state.draw_pile, engine.state.discard_pile)
                            for card in zone
                        ))
                    else:
                        scans = [card for card in engine.state.deck if card.card_id == "scan"][:3]
                        engine.state.hand = [engine._clone_card(card) for card in scans]
                        engine.state.draw_pile = []
                        scan_copy_ids = {card.copy_id for card in scans}
                        engine.state.discard_pile = [
                            engine._clone_card(card)
                            for card in engine.state.deck
                            if card.copy_id not in scan_copy_ids
                        ]
                        engine.state.energy = 0
                        target = engine.living_enemies()[0]
                        before = target.block
                        for _ in range(3):
                            engine.play_card(0, target.id)
                        self.assertEqual(min(stacks, 2), engine.state.energy)
                        self.assertEqual(before + 5, target.block)
                        self.assertEqual(
                            1,
                            sum(record.kind == "mutation_reaction"
                                and record.source_id == "base:third_bell"
                                for record in engine.state.ledger.records),
                        )
                    restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
                    self.assertEqual(engine.snapshot(), restored.snapshot())

    def test_mutation_formation_and_frozen_intent_matrix(self) -> None:
        formations = (
            ["hollow_crew", "hollow_crew"],
            ["hollow_crew", "acid_spitter", "repair_spider"],
        )
        for pressure in (0, 760, 1100):
            for formation in formations:
                with self.subTest(pressure=pressure, formation=formation):
                    engine = GameEngine.new(self.catalog, 800 + pressure + len(formation))
                    engine.state.pressure = pressure
                    engine.start_combat("lost_shift", enemy_ids=list(formation))
                    frozen = [dict(intent) for intent in engine.state.intents]
                    actors = {enemy.id for enemy in engine.living_enemies()}
                    targets = actors | {hero.id for hero in engine.living_heroes()}
                    self.assertEqual(actors, {intent["enemy_id"] for intent in frozen})
                    self.assertTrue(all(set(intent["target_ids"]) <= targets for intent in frozen))
                    for left_index, left in enumerate(engine.state.encounter_modules):
                        for right in engine.state.encounter_modules[left_index + 1:]:
                            self.assertNotIn(right, self.catalog.mutations[left]["excludes"])
                            self.assertNotIn(left, self.catalog.mutations[right]["excludes"])

                    enemies = engine.living_enemies()
                    enemies[0].rank, enemies[-1].rank = enemies[-1].rank, enemies[0].rank
                    self.assertEqual(frozen, engine.state.intents)
                    restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
                    self.assertEqual(frozen, restored.state.intents)

    def test_save_pressure_threshold_and_objective_state_matrix(self) -> None:
        thresholds = (239, 240, 479, 480, 759, 760, 1099, 1100)
        for pressure in thresholds:
            for objective_state in ("unstarted", "presented", "committed"):
                with self.subTest(pressure=pressure, objective_state=objective_state):
                    engine = GameEngine.new(self.catalog, 900 + pressure)
                    engine.state.pressure = pressure
                    objective = engine.state.objectives[0]
                    if objective_state != "unstarted":
                        engine.state.party_x, engine.state.party_y = objective.x, objective.y
                        engine._resolve_exploration_tile()
                    if objective_state == "committed":
                        approach = engine.mission_definition(objective.biome_id)["approaches"][0]
                        engine.begin_objective(approach["id"])

                    restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
                    self.assertEqual(engine.snapshot(), restored.snapshot())
                    self.assertEqual(pressure_band(pressure), pressure_band(restored.state.pressure))
                    self.assertEqual(objective_state == "presented",
                                     restored.state.phase == "objective")
                    restored_objective = restored.state.objectives[0]
                    self.assertEqual(objective_state == "committed",
                                     restored_objective.approach is not None)


if __name__ == "__main__":
    unittest.main()
