from __future__ import annotations

from collections import defaultdict
import random
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import Actor, CardInstance, GameEngine, RuleError, WALKABLE_TILES


class EngineTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()
        self.engine = GameEngine.new(self.catalog, 4242)

    def test_seed_reproduces_map_and_run_state(self) -> None:
        other = GameEngine.new(self.catalog, 4242)
        self.assertEqual(self.engine.snapshot(), other.snapshot())
        different = GameEngine.new(self.catalog, 4243)
        self.assertNotEqual(self.engine.world_tiles(), different.world_tiles())

    def test_all_world_layouts_and_biome_encounter_pools_generate(self) -> None:
        worlds = set()
        layouts = set()
        formations: dict[str, dict[tuple[str, ...], set[tuple[str, ...]]]] = defaultdict(
            lambda: defaultdict(set)
        )
        for seed in range(200):
            engine = GameEngine.new(self.catalog, seed)
            world = self.catalog.worlds[engine.state.world_id]
            worlds.add(engine.state.world_id)
            layouts.add(tuple(tuple(position) for position in engine.state.room_positions))
            self.assertEqual(set(world["biomes"]), {room.biome_id for room in engine.state.rooms})
            for room in engine.state.rooms:
                if room.kind not in {"fight", "elite"}:
                    continue
                encounter = self.catalog.encounters[room.content_id]
                self.assertIn(room.biome_id, encounter.get("biomes", ["derelict"]))
                self.assertTrue(2 <= len(room.enemy_ids) <= 4)
                self.assertTrue(
                    all(
                        room.biome_id in self.catalog.enemies[enemy_id].get("biomes", ["derelict"])
                        for enemy_id in room.enemy_ids
                    )
                )
                total_hp = sum(self.catalog.enemies[enemy_id]["max_hp"] for enemy_id in room.enemy_ids)
                minimum, maximum = (40, 60) if room.kind == "fight" else (62, 100)
                self.assertTrue(minimum <= total_hp <= maximum)
                formations[room.biome_id][tuple(sorted(room.enemy_ids))].add(tuple(room.enemy_ids))
        self.assertEqual(set(self.catalog.worlds), worlds)
        self.assertEqual(6, len(layouts))
        self.assertEqual(set(self.catalog.biomes), set(formations))
        for biome_id, selections in formations.items():
            self.assertGreaterEqual(len(selections), 2, biome_id)
            self.assertTrue(any(len(orders) > 1 for orders in selections.values()), biome_id)

    def test_card_biome_affinity_adds_bounded_potency(self) -> None:
        engine = GameEngine.new(self.catalog, 4, start_in_hub=True)
        engine.state.hub_selection = ["cryonaut", "warden", "medic", "scout"]
        engine.begin_expedition()
        engine.state.rooms[0].biome_id = "derelict"
        engine.start_combat("lost_shift")
        target = engine.living_enemies()[0]
        engine.state.hand = [CardInstance("ice_pick")]
        engine.state.energy = 3
        engine.play_card(0, target.id)
        self.assertEqual(target.max_hp - 6, target.hp)

        target.hp = target.max_hp
        engine.state.rooms[0].biome_id = "cryogenic"
        engine.state.hand = [CardInstance("ice_pick")]
        engine.state.energy = 3
        engine.play_card(0, target.id)
        self.assertEqual(target.max_hp - 8, target.hp)

    def test_generated_content_and_top_down_map_are_connected(self) -> None:
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
            start = engine.room_position(0)
            tiles = engine.world_tiles()
            self.assertEqual(35, len(tiles))
            self.assertTrue(all(len(row) == 117 for row in tiles))
            terrain = set("".join(tiles))
            world = self.catalog.worlds[engine.state.world_id]
            self.assertTrue(
                {self.catalog.biomes[biome_id]["glyph"] for biome_id in world["biomes"]}
                <= terrain
            )
            reachable = {start}
            frontier = [start]
            while frontier:
                current = frontier.pop()
                for neighbor in engine._neighbors(current):
                    if neighbor not in reachable:
                        reachable.add(neighbor)
                        frontier.append(neighbor)
            walkable = {
                (x, y)
                for y, row in enumerate(tiles)
                for x, character in enumerate(row)
                if character in WALKABLE_TILES
            }
            self.assertEqual(walkable, reachable)
            for room in engine.state.rooms:
                destination = engine.room_position(room.id)
                self.assertTrue(engine.is_walkable(*destination))
                self.assertTrue(destination == start or engine._find_path(start, destination))

    def test_discovery_distribution_is_seeded_and_spans_the_map(self) -> None:
        for seed in range(50):
            engine = GameEngine.new(self.catalog, seed)
            kinds = [pickup.kind for pickup in engine.state.pickups]
            self.assertEqual(3, kinds.count("boon"))
            self.assertEqual(5, kinds.count("item"))
            self.assertEqual(2, kinds.count("bargain"))
            self.assertEqual(2, kinds.count("trap"))
            self.assertTrue(all(pickup.hidden == (pickup.kind == "trap") for pickup in engine.state.pickups))
            bands = [sum(left <= pickup.x <= right for pickup in engine.state.pickups) for left, right in ((0, 38), (39, 77), (78, 116))]
            self.assertEqual([4, 4, 4], bands)
            item_ids = [pickup.payload["item_id"] for pickup in engine.state.pickups if pickup.kind == "item"]
            self.assertEqual(4, len(set(item_ids)))

    def test_visible_discovery_grants_stackable_item(self) -> None:
        pickup = next(item for item in self.engine.state.pickups if item.kind == "item")
        neighbor = self.engine._neighbors((pickup.x, pickup.y))[0]
        self.engine.state.party_x, self.engine.state.party_y = neighbor
        self.engine.step_exploration(pickup.x, pickup.y)
        self.assertEqual("discovery", self.engine.state.phase)
        item_id = pickup.payload["item_id"]
        self.engine.resolve_item_pickup()
        self.assertEqual(1, self.engine.state.items[item_id])
        self.assertTrue(pickup.resolved)

    def test_effect_stacks_apply_curves_and_caps(self) -> None:
        hero = self.engine.living_heroes()[0]
        for _ in range(10):
            self.engine.acquire_item("survey_relay")
        self.assertEqual(24, self.engine.maximum_navigation_distance())
        self.engine.acquire_boon(hero.id, "hunters_rhythm")
        first = self.engine._outgoing_damage(hero, 100)
        self.engine.acquire_boon(hero.id, "hunters_rhythm")
        second = self.engine._outgoing_damage(hero, 100)
        self.assertGreater(second, first)
        self.assertLess(second - first, first - 100)

    def test_curse_card_triggers_and_camp_treatment_removes_one_stack(self) -> None:
        hero = self.engine.living_heroes()[0]
        self.engine.acquire_curse(hero.id, "static_prayer")
        self.engine.acquire_curse(hero.id, "static_prayer")
        self.engine.state.phase = "combat"
        self.engine.state.draw_pile = [CardInstance("static_prayer", bound_hero_id=hero.id)]
        self.engine._draw(1)
        self.assertEqual(4, hero.stress)
        self.assertEqual([], self.engine.valid_targets(0))
        with self.assertRaisesRegex(RuleError, "cannot be played"):
            self.engine.play_card(0)

        self.engine.state.phase = "service"
        self.engine.state.service_type = "camp"
        supplies = self.engine.state.supplies
        self.engine.service("treat", hero_id=hero.id, curse_id="static_prayer")
        self.assertEqual(1, self.engine.state.curses[hero.id]["static_prayer"])
        self.assertEqual(
            1,
            sum(card.card_id == "static_prayer" for card in self.engine.state.deck),
        )
        self.assertEqual(supplies - 2, self.engine.state.supplies)

    def test_pathfinding_and_step_costs(self) -> None:
        with self.assertRaisesRegex(RuleError, "floor tile"):
            self.engine.path_to(0, 0)
        distant = self.engine.room_position(11)
        full_path = self.engine._find_path(
            (self.engine.state.party_x, self.engine.state.party_y),
            distant,
        )
        self.assertGreater(len(full_path), 100)
        with self.assertRaisesRegex(RuleError, "maximum reach of 18"):
            self.engine.path_to(*distant)
        path = self.engine.path_to(*full_path[:2][-1])
        with self.assertRaisesRegex(RuleError, "one floor tile"):
            self.engine.step_exploration(*path[1])
        self.engine.step_exploration(*path[0])
        self.engine.step_exploration(*path[1])
        self.assertEqual(99, self.engine.state.light)

    def test_patrol_contact_opens_combat_and_victory_clears_it(self) -> None:
        patrol = next(item for item in self.engine.state.patrols if self.engine.room(item.room_id).kind != "boss")
        expected_formation = list(self.engine.room(patrol.room_id).enemy_ids)
        for other in self.engine.state.patrols:
            other.active = other is patrol
        destination = self.engine._neighbors((self.engine.state.party_x, self.engine.state.party_y))[0]
        patrol.x, patrol.y = destination
        self.engine.step_exploration(*destination)
        self.assertEqual("combat", self.engine.state.phase)
        self.assertEqual(patrol.id, self.engine.state.active_patrol_id)
        self.assertEqual(expected_formation, [enemy.definition_id for enemy in self.engine.state.enemies])
        for enemy in list(self.engine.living_enemies()):
            self.engine._damage(enemy, enemy.max_hp)
        self.engine._combat_victory()
        self.assertFalse(patrol.active)
        self.assertTrue(self.engine.room(patrol.room_id).resolved)
        self.assertIsNone(self.engine.state.active_patrol_id)

    def test_nearby_patrol_advances_after_party_step(self) -> None:
        patrol = self.engine.state.patrols[0]
        for other in self.engine.state.patrols:
            other.active = other is patrol
        patrol.x, patrol.y = self.engine.state.party_x + 3, self.engine.state.party_y
        before_position = (patrol.x, patrol.y)
        before = len(self.engine._find_path((patrol.x, patrol.y), (self.engine.state.party_x, self.engine.state.party_y)))
        self.engine.step_exploration(self.engine.state.party_x, self.engine.state.party_y - 1)
        after = len(self.engine._find_path((patrol.x, patrol.y), (self.engine.state.party_x, self.engine.state.party_y)))
        self.assertNotEqual(before_position, (patrol.x, patrol.y))
        self.assertLessEqual(after, before)

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
        self.engine.state.hand = [CardInstance("breach")]
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

    def test_stunned_hero_cannot_play_until_turn_ends(self) -> None:
        self.engine.start_combat("lost_shift")
        warden = next(hero for hero in self.engine.state.heroes if hero.id == "warden")
        warden.statuses["stun"] = 1
        self.engine.state.hand = [CardInstance("baton_strike")]
        with self.assertRaisesRegex(RuleError, "stunned"):
            self.engine.play_card(0, self.engine.living_enemies()[0].id)
        self.engine.state.intents = []
        self.engine.end_turn()
        self.assertNotIn("stun", warden.statuses)

    def test_upgraded_effect_is_used(self) -> None:
        self.engine.start_combat("lost_shift")
        self.engine.state.hand = [CardInstance("baton_strike", upgraded=True)]
        self.engine.state.energy = 3
        target = self.engine.living_enemies()[0]
        self.engine.play_card(0, target.id)
        self.assertEqual(target.max_hp - 10, target.hp)

    def test_dodge_negates_one_direct_hit_and_riposte_counters(self) -> None:
        self.engine.start_combat("lost_shift")
        hero = self.engine.living_heroes()[0]
        enemy = self.engine.living_enemies()[0]
        hero.statuses["dodge"] = 2
        initial_hp = hero.hp
        self.engine._damage(hero, 8, enemy)
        self.assertEqual(initial_hp, hero.hp)
        self.assertNotIn("dodge", hero.statuses)

        hero.statuses["riposte"] = 2
        enemy_hp = enemy.hp
        self.engine._damage(hero, 1, enemy)
        self.assertEqual(enemy_hp - 4, enemy.hp)

    def test_chaplain_can_cleanse_negative_statuses(self) -> None:
        engine = GameEngine.new(self.catalog, 81, start_in_hub=True)
        engine.state.hub_selection = ["warden", "engineer", "chaplain", "scout"]
        engine.begin_expedition()
        engine.start_combat("lost_shift")
        chaplain = next(hero for hero in engine.state.heroes if hero.id == "chaplain")
        chaplain.statuses.update({"weak": 2, "wound": 3, "marked": 2})
        engine.state.hand = [CardInstance("absolve")]
        engine.state.energy = 3
        engine.play_card(0, chaplain.id)
        self.assertFalse({"weak", "wound", "marked"} & chaplain.statuses.keys())

    def test_enemy_intents_follow_actor_after_front_enemy_dies(self) -> None:
        self.engine.start_combat("drones")
        front = self.engine.living_enemies()[0]
        self.engine._damage(front, front.max_hp)
        self.engine.state.hand = []
        self.engine.end_turn()
        self.assertIn(self.engine.state.phase, {"combat", "defeat"})

    def test_enemy_intent_weights_coordinate_setup_exploit_and_support(self) -> None:
        self.engine.start_combat(
            "lost_shift",
            enemy_ids=["rad_acolyte", "control_rod"],
        )
        acolyte, control_rod = self.engine.living_enemies()
        gamma_brand = next(
            action
            for action in self.catalog.enemies["rad_acolyte"]["actions"]
            if action["name"] == "Gamma Brand"
        )
        containment = next(
            action
            for action in self.catalog.enemies["control_rod"]["actions"]
            if action["name"] == "Containment Blow"
        )
        regulate = next(
            action
            for action in self.catalog.enemies["control_rod"]["actions"]
            if action["name"] == "Regulate"
        )
        self.assertGreater(
            self.engine._enemy_action_weight(acolyte, gamma_brand, formation_exploits={"marked"}),
            self.engine._enemy_action_weight(acolyte, gamma_brand),
        )
        self.assertGreater(
            self.engine._enemy_action_weight(control_rod, containment, planned_statuses={"marked"}),
            self.engine._enemy_action_weight(control_rod, containment),
        )
        self.assertEqual(gamma_brand["target"], containment["target"])
        setup_first = sum(
            GameEngine._arrange_enemy_formation(
                self.catalog,
                random.Random(seed),
                ["control_rod", "rad_acolyte"],
            ).index("rad_acolyte")
            == 0
            for seed in range(100)
        )
        self.assertGreaterEqual(setup_first, 80)
        full_health_weight = self.engine._enemy_action_weight(control_rod, regulate)
        acolyte.hp = 4
        wounded_ally_weight = self.engine._enemy_action_weight(control_rod, regulate)
        self.assertGreater(wounded_ally_weight, full_health_weight)

    def test_enemy_status_exploit_adds_declared_combo_damage(self) -> None:
        self.engine.start_combat("lost_shift", enemy_ids=["control_rod"])
        enemy = self.engine.living_enemies()[0]
        target = self.engine.living_heroes()[0]
        target.max_hp = target.hp = 100
        effect = {"op": "damage", "amount": 8, "bonus_status": "marked", "bonus": 4}
        self.engine._apply_effect(enemy, [target], effect)
        self.assertEqual(92, target.hp)
        target.hp = 100
        target.statuses["marked"] = 2
        self.engine._apply_effect(enemy, [target], effect)
        self.assertEqual(88, target.hp)

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
        for pickup in self.engine.state.pickups:
            pickup.resolved = True
        self.engine.move_to(1)
        self.engine.service("upgrade", 0)
        self.assertTrue(self.engine.state.deck[0].upgraded)
        self.assertTrue(room.resolved)


if __name__ == "__main__":
    unittest.main()
