from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import Actor, CardInstance, GameEngine, RuleError


class EngineTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()
        self.engine = GameEngine.new(self.catalog, 4242)

    def test_seed_reproduces_map_and_run_state(self) -> None:
        other = GameEngine.new(self.catalog, 4242)
        self.assertEqual(self.engine.snapshot(), other.snapshot())

    def test_generated_maps_are_connected_and_have_required_rooms(self) -> None:
        for seed in range(50):
            engine = GameEngine.new(self.catalog, seed)
            visited = {0}
            pending = [0]
            while pending:
                current = pending.pop()
                for neighbor in engine.state.rooms[current].neighbors:
                    if neighbor not in visited:
                        visited.add(neighbor)
                        pending.append(neighbor)
            self.assertEqual(set(range(12)), visited)
            kinds = {room.kind for room in engine.state.rooms}
            self.assertTrue({"start", "fight", "event", "camp", "upgrade", "elite", "cache", "boss"} <= kinds)

    def test_travel_only_accepts_connected_room(self) -> None:
        with self.assertRaisesRegex(RuleError, "not connected"):
            self.engine.move_to(11)
        self.engine.state.rooms[1].resolved = True
        self.engine.move_to(1)
        self.assertEqual(1, self.engine.state.current_room)
        self.assertEqual(94, self.engine.state.light)

    def test_card_damage_and_rank_restrictions(self) -> None:
        self.engine.start_combat("lost_shift")
        self.engine.state.hand = [CardInstance("baton_strike")]
        self.engine.state.energy = 3
        target = self.engine.living_enemies()[0]
        initial = target.hp
        self.engine.play_card(0, target.id)
        self.assertEqual(initial - 7, target.hp)

        warden = next(hero for hero in self.engine.state.heroes if hero.id == "warden")
        self.engine._move(warden, 2)
        self.engine.state.hand = [CardInstance("baton_strike")]
        with self.assertRaisesRegex(RuleError, "valid rank"):
            self.engine.play_card(0, target.id)

    def test_all_cards_can_resolve_with_a_legal_target(self) -> None:
        for card_id, definition in self.catalog.cards.items():
            with self.subTest(card=card_id):
                party = [definition["hero"]]
                party.extend(hero_id for hero_id in self.catalog.heroes if hero_id not in party)
                engine = GameEngine.new(self.catalog, 7, start_in_hub=True)
                engine.state.hub_selection = party[:4]
                engine.begin_expedition()
                engine.start_combat("security")
                actor = next(hero for hero in engine.state.heroes if hero.id == definition["hero"])
                desired = definition["from_ranks"][0]
                engine._move(actor, desired - actor.rank)
                engine.state.hand = [CardInstance(card_id)]
                engine.state.energy = 20
                targets = engine.valid_targets(0)
                self.assertTrue(targets)
                engine.play_card(0, targets[0])

    def test_hub_builds_selected_party_deck_and_filters_rewards(self) -> None:
        engine = GameEngine.new(self.catalog, 73, start_in_hub=True)
        self.assertEqual("hub", engine.state.phase)
        self.assertEqual([], engine.state.heroes)
        for hero_id in list(engine.state.hub_selection):
            engine.toggle_hub_crew(hero_id)
        party = ["breacher", "synth", "biologist", "operative"]
        for hero_id in party:
            engine.toggle_hub_crew(hero_id)
        engine.reorder_hub_crew("operative", -1)
        expected = ["breacher", "synth", "operative", "biologist"]
        engine.begin_expedition()
        self.assertEqual(expected, [hero.id for hero in engine.state.heroes])
        self.assertEqual([1, 2, 3, 4], [hero.rank for hero in engine.state.heroes])
        self.assertEqual(20, len(engine.state.deck))
        engine.start_combat("lost_shift")
        for enemy in list(engine.living_enemies()):
            engine._damage(enemy, enemy.max_hp)
        engine._combat_victory()
        reward_heroes = {self.catalog.cards[card_id]["hero"] for card_id in engine.state.rewards}
        self.assertTrue(reward_heroes <= set(expected))

    def test_hub_rejects_a_fifth_crew_member(self) -> None:
        engine = GameEngine.new(self.catalog, 74, start_in_hub=True)
        with self.assertRaisesRegex(RuleError, "only four"):
            engine.toggle_hub_crew("breacher")

    def test_all_enemy_actions_can_resolve(self) -> None:
        for enemy_id, definition in self.catalog.enemies.items():
            for action in definition["actions"]:
                with self.subTest(enemy=enemy_id, action=action["name"]):
                    engine = GameEngine.new(self.catalog, 11)
                    engine.start_combat("lost_shift")
                    for hero in engine.state.heroes:
                        hero.max_hp = hero.hp = 999
                    enemy = Actor(
                        f"{enemy_id}:1",
                        definition["name"],
                        definition["max_hp"],
                        definition["max_hp"],
                        1,
                        "enemy",
                        definition_id=enemy_id,
                    )
                    engine.state.enemies = [enemy]
                    engine.state.intents = [{"enemy_rank": 1, "enemy_id": enemy.id, "action": action["name"]}]
                    engine._enemy_phase()

    def test_all_event_branches_resolve(self) -> None:
        for event_id, event in self.catalog.events.items():
            for choice_index in range(len(event["choices"])):
                with self.subTest(event=event_id, choice=choice_index):
                    engine = GameEngine.new(self.catalog, 19)
                    room = engine.state.rooms[0]
                    room.content_id = event_id
                    room.resolved = False
                    engine.state.phase = "event"
                    engine.state.current_event = event_id
                    engine.state.supplies = 99
                    engine.choose_event(choice_index)
                    self.assertTrue(room.resolved)
                    self.assertIn(engine.state.phase, {"exploration", "reward"})

    def test_deaths_door_can_kill_and_ends_run(self) -> None:
        hero = self.engine.state.heroes[0]
        self.engine._damage(hero, hero.max_hp)
        self.assertTrue(hero.deaths_door)
        original = self.catalog.balance["death_chance"]
        self.catalog.balance["death_chance"] = 1.0
        try:
            self.engine._damage(hero, 1)
        finally:
            self.catalog.balance["death_chance"] = original
        self.assertEqual("defeat", self.engine.state.phase)

    def test_stress_affliction_then_collapse(self) -> None:
        hero = self.engine.state.heroes[0]
        self.engine._change_stress(hero, 100)
        self.assertIsNotNone(hero.affliction)
        self.assertEqual(50, hero.stress)
        self.engine._change_stress(hero, 50)
        self.assertTrue(hero.deaths_door)

    def test_upgraded_effect_is_used(self) -> None:
        self.engine.start_combat("lost_shift")
        self.engine.state.hand = [CardInstance("baton_strike", upgraded=True)]
        self.engine.state.energy = 3
        target = self.engine.living_enemies()[0]
        self.engine.play_card(0, target.id)
        self.assertEqual(target.max_hp - 10, target.hp)

    def test_enemy_intents_follow_actor_after_front_enemy_dies(self) -> None:
        self.engine.start_combat("drones")
        front = self.engine.living_enemies()[0]
        self.engine._damage(front, front.max_hp)
        self.engine.state.hand = []
        self.engine.end_turn()
        self.assertIn(self.engine.state.phase, {"combat", "defeat"})

    def test_guard_lasts_for_configured_enemy_phases(self) -> None:
        self.engine.start_combat("lost_shift")
        medic = next(hero for hero in self.engine.state.heroes if hero.id == "medic")
        self.engine.state.hand = [CardInstance("interpose")]
        self.engine.state.energy = 3
        self.engine.play_card(0, medic.id)
        self.assertEqual(2, medic.guard_turns)
        self.engine.state.intents = []
        self.engine.end_turn()
        self.assertEqual(1, medic.guard_turns)

    def test_supply_and_service_progression(self) -> None:
        hero = self.engine.state.heroes[0]
        hero.hp -= 12
        supplies = self.engine.state.supplies
        self.engine.use_supply("heal")
        self.assertEqual(supplies - 1, self.engine.state.supplies)
        self.assertGreater(hero.hp, hero.max_hp - 12)

        room = self.engine.state.rooms[1]
        room.kind = "upgrade"
        room.resolved = False
        self.engine.move_to(1)
        self.engine.service("upgrade", 0)
        self.assertTrue(self.engine.state.deck[0].upgraded)
        self.assertTrue(room.resolved)


if __name__ == "__main__":
    unittest.main()
