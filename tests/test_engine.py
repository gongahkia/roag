from __future__ import annotations

from collections import defaultdict
import random
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import (
    Actor,
    CardInstance,
    GameEngine,
    RuleError,
    WALKABLE_TILES,
    WORLD_LAYOUTS,
)


class EngineTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()
        self.engine = GameEngine.new(self.catalog, 4242)
        # Archive only changes opening hand size, keeping unrelated combat-rule
        # tests isolated from formation, block, and status environments.
        self.engine.room().biome_id = "archive"

    def complete_objective(
        self,
        engine: GameEngine,
        objective,
        approach_id: str,
    ) -> None:
        engine.state.party_x, engine.state.party_y = objective.x, objective.y
        engine._resolve_exploration_tile()
        engine.begin_objective(approach_id)
        while not objective.completed:
            engine.state.party_x, engine.state.party_y = engine.objective_position(objective)
            engine._resolve_exploration_tile()
            engine.advance_objective()

    def test_seed_reproduces_map_and_run_state(self) -> None:
        other = GameEngine.new(self.catalog, 4242)
        other.room().biome_id = "archive"
        self.assertEqual(self.engine.snapshot(), other.snapshot())
        different = GameEngine.new(self.catalog, 4243)
        self.assertNotEqual(self.engine.world_tiles(), different.world_tiles())

    def test_tutorial_is_a_deterministic_bounded_expedition(self) -> None:
        first = GameEngine.tutorial(self.catalog)
        second = GameEngine.tutorial(self.catalog)
        self.assertEqual(first.snapshot(), second.snapshot())
        self.assertTrue(first.state.tutorial)
        self.assertEqual("exploration", first.state.phase)
        self.assertEqual(
            self.catalog.squads["bulkhead_basics"]["formation"],
            [hero.id for hero in first.living_heroes()],
        )
        active = [patrol for patrol in first.state.patrols if patrol.active]
        self.assertEqual(1, len(active))
        self.assertEqual((active[0].x, active[0].y), first.tutorial_destination())
        path = first.path_to(*first.tutorial_destination())
        self.assertLessEqual(first.path_cost(path), first.maximum_navigation_distance())

    def test_tutorial_route_opens_a_real_positional_combo_combat(self) -> None:
        engine = GameEngine.tutorial(self.catalog)
        with self.assertRaisesRegex(RuleError, "limited to the highlighted"):
            engine.path_to(*engine._neighbors((engine.state.party_x, engine.state.party_y))[0])
        engine.advance_tutorial(0, 1)
        destination = engine.tutorial_destination()
        for step in engine.path_to(*destination):
            engine.step_exploration(*step)
        self.assertEqual("combat", engine.state.phase)
        self.assertEqual(2, engine.state.tutorial_stage)
        self.assertEqual(
            ["mag_boots", "field_dressing", "scan", "arc_welder", "brace"],
            [card.card_id for card in engine.state.hand],
        )
        self.assertTrue(engine.state.hand[1].upgraded)
        warden = next(hero for hero in engine.living_heroes() if hero.id == "warden")
        engineer = next(hero for hero in engine.living_heroes() if hero.id == "engineer")
        self.assertEqual((2, 1), (warden.rank, engineer.rank))
        self.assertTrue(warden.deaths_door)
        self.assertEqual(
            ["Gamma Brand", "Containment Blow"],
            [intent["action"] for intent in engine.state.intents],
        )

        engine.advance_tutorial(2, 3)
        engine.play_card(0, warden.id)
        self.assertEqual((1, 2), (warden.rank, engineer.rank))
        self.assertEqual(4, engine.state.tutorial_stage)
        engine.advance_tutorial(4, 5)
        field_dressing = next(
            index for index, card in enumerate(engine.state.hand)
            if card.card_id == "field_dressing"
        )
        engine.play_card(field_dressing, warden.id)
        self.assertFalse(warden.deaths_door)
        self.assertNotIn("wound", warden.statuses)
        events: list[dict] = []
        engine.end_turn(events.append)
        self.assertEqual(6, engine.state.tutorial_stage)
        self.assertEqual(2, len(events))

    def test_all_world_layouts_and_biome_encounter_pools_generate(self) -> None:
        worlds = set()
        layouts = set()
        formations: dict[str, dict[tuple[str, ...], set[tuple[str, ...]]]] = defaultdict(
            lambda: defaultdict(set)
        )
        plans: set[str] = set()
        for seed in range(200):
            engine = GameEngine.new(self.catalog, seed)
            world = self.catalog.worlds[engine.state.world_id]
            worlds.add(engine.state.world_id)
            layouts.add(tuple(tuple(position) for position in engine.state.room_positions))
            self.assertEqual(set(engine.state.biome_ids), {room.biome_id for room in engine.state.rooms})
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
                self.assertEqual(
                    engine._formation_plan(self.catalog, room.enemy_ids),
                    room.encounter_plan,
                )
                plans.add(room.encounter_plan)
                formations[room.biome_id][tuple(sorted(room.enemy_ids))].add(tuple(room.enemy_ids))
        self.assertEqual(set(self.catalog.worlds), worlds)
        self.assertEqual(6, len(layouts))
        self.assertEqual(set(self.catalog.biomes), set(formations))
        for biome_id, selections in formations.items():
            self.assertGreaterEqual(len(selections), 2, biome_id)
            self.assertTrue(any(len(orders) > 1 for orders in selections.values()), biome_id)
        self.assertGreaterEqual(len(plans), 4)

    def test_branching_layout_is_a_trunk_with_backtracking_branches(self) -> None:
        _, edges = WORLD_LAYOUTS["branching"]
        self.assertEqual(11, sum(map(len, edges.values())) // 2)
        self.assertEqual(
            {0, 2, 3, 5, 6, 8, 9, 11},
            {node for node, links in edges.items() if len(links) == 1},
        )
        self.assertTrue(all(len(edges[node]) == 4 for node in (1, 4, 7)))
        for seed in (2, 14):
            engine = GameEngine.new(self.catalog, seed)
            self.assertEqual("branching", self.catalog.worlds[engine.state.world_id]["layout"])
            self.assertEqual(engine.snapshot(), GameEngine.from_snapshot(self.catalog, engine.snapshot()).snapshot())

    def test_all_biomes_generate_reachable_hazards_and_optional_objectives(self) -> None:
        seen_biomes: set[str] = set()
        seen_mixtures: set[tuple[str, ...]] = set()
        patterns = {
            pattern["biome"]: pattern
            for pattern in self.catalog.terrain_patterns.values()
        }
        for seed in range(200):
            engine = GameEngine.new(self.catalog, seed)
            selected = set(engine.state.biome_ids)
            seen_biomes |= selected
            seen_mixtures.add(tuple(engine.state.biome_ids))
            self.assertEqual(4, len(selected))
            self.assertEqual(selected, {objective.biome_id for objective in engine.state.objectives})
            self.assertEqual(selected, {landmark.biome_id for landmark in engine.state.landmarks})
            self.assertEqual(4, len(engine.state.landmarks))
            self.assertEqual(8, len(engine.state.hazards))
            for biome_id in selected:
                self.assertEqual(
                    2,
                    sum(hazard.biome_id == biome_id for hazard in engine.state.hazards),
                )
                self.assertTrue(
                    any(
                        glyph == patterns[biome_id]["glyph"] and engine.biome_at(x, y) == biome_id
                        for y, row in enumerate(engine.world_tiles())
                        for x, glyph in enumerate(row)
                    ),
                    f"seed {seed} has no {biome_id} secondary terrain",
                )
            origin = engine.room_position(0)
            for feature in engine.state.objectives + engine.state.hazards:
                self.assertTrue(engine._find_path(origin, (feature.x, feature.y)))
            for landmark in engine.state.landmarks:
                self.assertEqual(9, len(landmark.cells))
                self.assertTrue(all(engine.is_walkable(*cell) for cell in landmark.cells))
                self.assertTrue(engine.feature_is_known(landmark.id))
            boss = next(
                patrol
                for patrol in engine.state.patrols
                if engine.room(patrol.room_id).kind == "boss"
            )
            self.assertFalse(boss.active)
        self.assertEqual(set(self.catalog.biomes), seen_biomes)
        self.assertGreater(len(seen_mixtures), len(self.catalog.worlds))

    def test_biome_specific_terrain_glyph_defines_region_identity(self) -> None:
        x, y = self.engine.room_position(1)
        original_biome = self.engine.biome_at(x, y)
        replacement = next(
            biome for biome in self.engine.state.biome_ids if biome != original_biome
        )
        row = self.engine.state.world_tiles[y]
        self.engine.state.world_tiles[y] = (
            row[:x] + self.catalog.biomes[replacement]["glyph"] + row[x + 1:]
        )
        self.assertEqual(replacement, self.engine.biome_at(x, y))

        self.engine.state.world_tiles[y] = row[:x] + "," + row[x + 1:]
        nearest = min(
            self.engine.state.rooms,
            key=lambda room: abs(x - self.engine.room_position(room.id)[0])
            + abs(y - self.engine.room_position(room.id)[1]),
        )
        self.assertEqual(nearest.biome_id, self.engine.biome_at(x, y))

    def test_access_objectives_offer_routes_and_gate_the_overseer(self) -> None:
        boss = next(
            patrol
            for patrol in self.engine.state.patrols
            if self.engine.room(patrol.room_id).kind == "boss"
        )
        self.engine.state.party_x, self.engine.state.party_y = self.engine.room_position(11)
        self.engine._resolve_exploration_tile()
        self.assertEqual("exploration", self.engine.state.phase)
        self.assertFalse(boss.active)
        self.assertIn("Apex seal rejects", self.engine.state.log[-1])

        self.engine.state.supplies = 9
        for objective in self.engine.state.objectives[:2]:
            approach = self.engine.mission_definition(objective.biome_id)["approaches"][0]
            self.complete_objective(self.engine, objective, approach["id"])
        self.assertTrue(self.engine.boss_unlocked())
        self.assertTrue(boss.active)
        self.assertEqual(2, self.engine.completed_objectives())
        self.assertTrue(any(not objective.completed for objective in self.engine.state.objectives))

    def test_each_biome_hazard_resolves_deterministically(self) -> None:
        seeds_by_biome: dict[str, int] = {}
        for seed in range(100):
            engine = GameEngine.new(self.catalog, seed)
            for biome_id in engine.state.biome_ids:
                seeds_by_biome.setdefault(biome_id, seed)
        self.assertEqual(set(self.catalog.biomes), set(seeds_by_biome))
        for biome_id, seed in seeds_by_biome.items():
            with self.subTest(biome=biome_id):
                first = GameEngine.new(self.catalog, seed)
                second = GameEngine.new(self.catalog, seed)
                for engine in (first, second):
                    hazard = next(item for item in engine.state.hazards if item.biome_id == biome_id)
                    engine.state.party_x, engine.state.party_y = hazard.x, hazard.y
                    engine._resolve_exploration_tile()
                    self.assertEqual("hazard", engine.state.phase)
                    self.assertIn([hazard.x, hazard.y], hazard.triggered_cells)
                    self.assertFalse(hazard.triggered)
                    engine.finish_hazard()
                self.assertEqual(first.snapshot(), second.snapshot())

    def test_biome_hazard_footprint_persists_until_each_cell_triggers(self) -> None:
        hazard = self.engine.state.hazards[0]
        self.assertEqual(3, len(hazard.cells))
        for index, cell in enumerate(hazard.cells):
            self.engine.state.party_x, self.engine.state.party_y = cell
            self.engine._resolve_exploration_tile()
            if self.engine.state.phase == "defeat":
                self.fail("generated three-cell hazard unexpectedly wiped a fresh party")
            self.assertEqual("hazard", self.engine.state.phase)
            self.assertEqual(index + 1, len(hazard.triggered_cells))
            self.assertEqual(index == 2, hazard.triggered)
            self.assertEqual(index < 2, hazard.active)
            self.engine.finish_hazard()

    def test_suppressed_hazard_stays_inert_and_round_trips(self) -> None:
        engine = GameEngine.new(self.catalog, 4242)
        hazard = engine.state.hazards[0]
        first_cell = hazard.cells[0]
        engine.state.party_x, engine.state.party_y = first_cell
        engine._resolve_exploration_tile()
        engine.finish_hazard()
        engine.suppress_hazard(hazard.id, "test facility")
        restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        restored_hazard = next(item for item in restored.state.hazards if item.id == hazard.id)
        self.assertFalse(restored_hazard.active)
        self.assertTrue(restored_hazard.triggered)
        self.assertEqual("test facility", restored_hazard.suppressed_by)
        remaining = next(cell for cell in restored_hazard.cells if cell not in restored_hazard.triggered_cells)
        restored.state.party_x, restored.state.party_y = remaining
        restored._resolve_exploration_tile()
        self.assertEqual("exploration", restored.state.phase)

    def test_biome_facility_offers_irreversible_disclosed_tradeoff(self) -> None:
        engine = GameEngine.new(self.catalog, 4242)
        facility = next(
            item for item in engine.state.facilities
            if any(
                effect["op"] == "suppress_hazard"
                for option in engine.facility_definition(item)["options"]
                for effect in option["effects"]
            )
        )
        definition = engine.facility_definition(facility)
        suppress = next(
            option
            for option in definition["options"]
            if any(effect["op"] == "suppress_hazard" for effect in option["effects"])
        )
        active_before = sum(
            hazard.active and hazard.biome_id == facility.biome_id
            for hazard in engine.state.hazards
        )
        engine.state.party_x, engine.state.party_y = facility.x, facility.y
        engine._resolve_exploration_tile()
        self.assertEqual("facility", engine.state.phase)
        self.assertEqual((True, "available"), engine.facility_option_available(facility, suppress["id"]))
        engine.resolve_facility(suppress["id"])
        self.assertTrue(facility.used)
        self.assertEqual(suppress["id"], facility.outcome)
        self.assertEqual(
            active_before - 1,
            sum(
                hazard.active and hazard.biome_id == facility.biome_id
                for hazard in engine.state.hazards
            ),
        )
        engine._resolve_exploration_tile()
        self.assertEqual("exploration", engine.state.phase)

    def test_facility_reveal_is_regional_and_round_trips(self) -> None:
        engine = None
        for seed in range(100):
            candidate = GameEngine.new(self.catalog, seed)
            if any(
                option["effects"][0]["op"] == "reveal_biome"
                for facility in candidate.state.facilities
                for option in candidate.facility_definition(facility)["options"]
            ):
                engine = candidate
                break
        self.assertIsNotNone(engine)
        assert engine is not None
        facility = next(
            item
            for item in engine.state.facilities
            if any(
                effect["op"] == "reveal_biome"
                for option in engine.facility_definition(item)["options"]
                for effect in option["effects"]
            )
        )
        option = next(
            option
            for option in engine.facility_definition(facility)["options"]
            if any(effect["op"] == "reveal_biome" for effect in option["effects"])
        )
        engine.state.party_x, engine.state.party_y = facility.x, facility.y
        engine._resolve_exploration_tile()
        engine.resolve_facility(option["id"])
        expected_hazards = {
            hazard.id for hazard in engine.state.hazards if hazard.biome_id == facility.biome_id
        }
        self.assertTrue(expected_hazards <= set(engine.state.known_feature_ids))
        restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        restored_facility = next(item for item in restored.state.facilities if item.id == facility.id)
        self.assertTrue(restored_facility.used)
        self.assertEqual(option["id"], restored_facility.outcome)

    def test_partially_triggered_hazard_round_trips(self) -> None:
        engine = GameEngine.new(self.catalog, 4242)
        hazard = engine.state.hazards[0]
        engine.state.party_x, engine.state.party_y = hazard.cells[0]
        engine._resolve_exploration_tile()
        restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        restored_hazard = next(item for item in restored.state.hazards if item.id == hazard.id)
        self.assertTrue(restored_hazard.active)
        self.assertFalse(restored_hazard.triggered)
        self.assertEqual(hazard.triggered_cells, restored_hazard.triggered_cells)

    def test_biome_travel_costs_visibility_patrols_and_combat_rules(self) -> None:
        signatures = set()
        for biome_id, biome in self.catalog.biomes.items():
            mechanics = biome["mechanics"]
            signatures.add(
                (
                    mechanics["hazard"]["effect"],
                    mechanics["traversal"]["cost"],
                    mechanics["patrol"]["behavior"],
                    mechanics["visibility"]["patrol_radius"],
                    mechanics["combat"]["name"],
                    self.engine.mission_definition(biome_id)["approaches"][0]["outcome"],
                )
            )
            engine = GameEngine.new(self.catalog, 900 + len(signatures))
            engine.room().biome_id = biome_id
            original_ranks = [hero.rank for hero in engine.living_heroes()]
            engine.start_combat("lost_shift")
            self.assertIn(mechanics["combat"]["name"], engine.state.log[-1])
            if any(effect["op"] == "reverse" for effect in mechanics["combat"]["effects"]):
                self.assertEqual(list(reversed(original_ranks)), [hero.rank for hero in engine.state.heroes])
        self.assertEqual(len(self.catalog.biomes), len(signatures))

    def test_all_patrol_doctrines_generate_deterministic_serialized_routes(self) -> None:
        seen: set[str] = set()
        for seed in range(30):
            engine = GameEngine.new(self.catalog, seed)
            for patrol in engine.state.patrols:
                seen.add(patrol.doctrine)
                profile = engine.biome_mechanics(engine.room(patrol.room_id).biome_id)["patrol"]
                self.assertEqual(profile["behavior"], patrol.doctrine)
                self.assertEqual(list(engine.room_position(patrol.room_id)), patrol.route[0])
                if patrol.doctrine in {"circuit", "migrate", "stalk", "sweep"}:
                    self.assertGreater(len(patrol.route), 1)
            restored = GameEngine.from_snapshot(self.catalog, engine.snapshot())
            self.assertEqual(engine.snapshot(), restored.snapshot())
        self.assertEqual(
            {"circuit", "erratic", "hunt", "migrate", "roam", "sentry", "stalk", "sweep"},
            seen,
        )

    def test_route_patrol_circulates_while_sentry_holds_home(self) -> None:
        engines = [GameEngine.new(self.catalog, seed) for seed in range(20)]
        route_engine = next(
            engine
            for engine in engines
            if any(patrol.doctrine in {"circuit", "migrate", "stalk", "sweep"} for patrol in engine.state.patrols)
        )
        route_patrol = next(
            patrol
            for patrol in route_engine.state.patrols
            if patrol.doctrine in {"circuit", "migrate", "stalk", "sweep"}
        )
        for patrol in route_engine.state.patrols:
            patrol.active = patrol.id == route_patrol.id
        distances = route_engine._distances_from((route_patrol.x, route_patrol.y))
        far = max(distances, key=distances.get)
        route_engine.state.party_x, route_engine.state.party_y = far
        route_engine.state.exploration_steps = 6
        route_engine._advance_patrols()
        self.assertEqual(route_patrol.route[1], [route_patrol.x, route_patrol.y])

        sentry_engine = next(
            engine for engine in engines if any(patrol.doctrine == "sentry" for patrol in engine.state.patrols)
        )
        sentry = next(patrol for patrol in sentry_engine.state.patrols if patrol.doctrine == "sentry")
        for patrol in sentry_engine.state.patrols:
            patrol.active = patrol.id == sentry.id
        home = sentry_engine.room_position(sentry.room_id)
        sentry.x, sentry.y = sentry_engine._neighbors(home)[0]
        distances = sentry_engine._distances_from(home)
        far = max(distances, key=distances.get)
        sentry_engine.state.party_x, sentry_engine.state.party_y = far
        sentry_engine.state.exploration_steps = 6
        sentry_engine._advance_patrols()
        self.assertEqual(home, (sentry.x, sentry.y))

    def test_committing_objective_temporarily_alerts_its_biome_patrols(self) -> None:
        objective = next(
            item
            for item in self.engine.state.objectives
            if any(
                patrol.active and self.engine.room(patrol.room_id).biome_id == item.biome_id
                for patrol in self.engine.state.patrols
            )
        )
        self.engine.state.party_x, self.engine.state.party_y = objective.x, objective.y
        self.engine._resolve_exploration_tile()
        approach = self.engine.mission_definition(objective.biome_id)["approaches"][1]
        self.engine.begin_objective(approach["id"])
        alerted = [
            patrol
            for patrol in self.engine.state.patrols
            if patrol.active and self.engine.room(patrol.room_id).biome_id == objective.biome_id
        ]
        self.assertTrue(alerted)
        self.assertTrue(all(patrol.alert == 8 for patrol in alerted))

    def test_every_biome_mission_has_two_telegraphed_map_routes(self) -> None:
        for mission in self.catalog.missions.values():
            with self.subTest(biome=mission["biome"]):
                self.assertEqual(2, len(mission["approaches"]))
                self.assertNotEqual(
                    mission["approaches"][0]["telegraph"]["travel"],
                    mission["approaches"][1]["telegraph"]["travel"],
                )
                self.assertNotEqual(
                    mission["approaches"][0]["outcome"],
                    mission["approaches"][1]["outcome"],
                )

    def test_reduced_party_can_complete_optional_objective_stages(self) -> None:
        hero = self.engine.living_heroes()[0]
        hero.hp = 0
        hero.deaths_door = False
        self.engine._hero_died(hero)
        objective = self.engine.state.objectives[0]
        approach = self.engine.mission_definition(objective.biome_id)["approaches"][1]
        self.complete_objective(self.engine, objective, approach["id"])
        self.assertTrue(objective.completed)
        self.assertEqual(approach["outcome"], objective.outcome)
        self.assertEqual(3, len(self.engine.living_heroes()))

    def test_card_biome_affinity_adds_bounded_potency(self) -> None:
        engine = GameEngine.new(self.catalog, 4, start_in_hub=True)
        engine.state.hub_selection = ["cryonaut", "warden", "medic", "scout"]
        engine.begin_expedition()
        engine.state.rooms[0].biome_id = "archive"
        engine.start_combat("lost_shift")
        target = engine.living_enemies()[0]
        cryonaut = next(hero for hero in engine.living_heroes() if hero.id == "cryonaut")
        engine._move(cryonaut, 2 - cryonaut.rank)
        engine.state.hand = [CardInstance("ice_pick")]
        engine.state.energy = 3
        engine.play_card(0, target.id)
        self.assertEqual(target.max_hp - 6, target.hp)

        target.hp = target.max_hp
        engine.state.rooms[0].biome_id = "cryogenic"
        engine.state.hand = [CardInstance("ice_pick")]
        engine.state.energy = 3
        target.statuses.pop("vulnerable", None)
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
            self.assertTrue(
                {self.catalog.biomes[biome_id]["glyph"] for biome_id in engine.state.biome_ids}
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

    def test_static_knowledge_persists_but_hidden_traps_do_not_leak(self) -> None:
        visible = next(pickup for pickup in self.engine.state.pickups if not pickup.hidden)
        hidden = next(pickup for pickup in self.engine.state.pickups if pickup.hidden)
        self.engine.state.party_x, self.engine.state.party_y = visible.x, visible.y
        self.engine._update_perception()
        self.assertTrue(self.engine.feature_is_known(visible.id))
        self.assertFalse(self.engine.feature_is_known(hidden.id))
        self.engine.state.party_x, self.engine.state.party_y = self.engine.room_position(0)
        self.assertTrue(self.engine.feature_is_known(visible.id))

    def test_route_intel_reports_known_risk_without_hidden_traps(self) -> None:
        origin = (self.engine.state.party_x, self.engine.state.party_y)
        path = self.engine._find_path(origin, self.engine.room_position(1))[:4]
        hazard = self.engine.state.hazards[0]
        hazard.x, hazard.y = path[1]
        hazard.cells[0] = list(path[1])
        hazard.biome_id = self.engine.biome_at(*path[1])
        self.engine.state.known_feature_ids.append(hazard.id)
        hidden = next(pickup for pickup in self.engine.state.pickups if pickup.hidden)
        hidden.x, hidden.y = path[0]
        intel = self.engine.route_intel(path)
        self.assertEqual(1, intel["known_hazards"])
        self.assertEqual(self.engine.path_cost(path), intel["ticks"])
        self.assertNotIn(hidden.id, self.engine.state.known_feature_ids)

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
        self.assertEqual(1, self.engine._hero_effect_value(hero, "boon", "damage_draw"))
        self.engine.acquire_boon(hero.id, "hunters_rhythm")
        self.assertEqual(2, self.engine._hero_effect_value(hero, "boon", "damage_draw"))
        self.engine.acquire_boon(hero.id, "hunters_rhythm")
        self.assertEqual(2, self.engine._hero_effect_value(hero, "boon", "damage_draw"))

    def test_hunters_rhythm_turns_damage_sequence_into_draw(self) -> None:
        warden = self.engine.living_heroes()[0]
        self.engine.acquire_boon(warden.id, "hunters_rhythm")
        self.engine.acquire_boon(warden.id, "hunters_rhythm")
        self.engine.start_combat("lost_shift")
        target = self.engine.living_enemies()[0]
        target.max_hp = target.hp = 200
        self.engine.state.hand = [CardInstance("baton_strike"), CardInstance("baton_strike")]
        self.engine.state.draw_pile = [CardInstance("brace"), CardInstance("brace")]
        self.engine.state.energy = 10
        self.engine.play_card(0, target.id)
        self.engine.play_card(0, target.id)
        self.assertEqual(2, len(self.engine.state.hand))
        self.assertEqual(0, len(self.engine.state.draw_pile))

    def test_frayed_focus_forces_damage_card_sequencing(self) -> None:
        warden = self.engine.living_heroes()[0]
        self.engine.acquire_curse(warden.id, "frayed_focus")
        self.engine.start_combat("lost_shift")
        target = self.engine.living_enemies()[0]
        self.engine.state.hand = [
            CardInstance("baton_strike"),
            CardInstance("brace"),
            CardInstance("arc_welder"),
        ]
        self.engine.state.energy = 10
        self.engine.play_card(0, target.id)
        self.assertEqual(["brace"], [card.card_id for card in self.engine.state.hand])
        self.assertIn("Frayed Focus discards Arc Welder", self.engine.state.log[-1])

    def test_tremors_tax_only_the_first_block_techniques(self) -> None:
        warden = self.engine.living_heroes()[0]
        self.engine.acquire_curse(warden.id, "tremors")
        self.engine.start_combat("lost_shift")
        first = CardInstance("brace")
        self.assertEqual(2, self.engine.card_cost(first))
        self.engine.state.hand = [first]
        self.engine.state.energy = 10
        self.engine.play_card(0, warden.id)
        self.assertEqual(1, self.engine.card_cost(CardInstance("brace")))

    def test_focusing_lens_rewards_focus_techniques_with_draw(self) -> None:
        self.engine.acquire_item("focusing_lens")
        self.engine.start_combat("lost_shift")
        scout = next(hero for hero in self.engine.living_heroes() if hero.id == "scout")
        self.engine.state.hand = [CardInstance("deadeye")]
        self.engine.state.draw_pile = [CardInstance("brace")]
        self.engine.state.energy = 10
        self.engine.play_card(0, scout.id)
        self.assertEqual(["brace"], [card.card_id for card in self.engine.state.hand])
        self.assertIn("Focusing Lens", self.engine.state.log[-1])

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
        with self.assertRaisesRegex(RuleError, "maximum reach is 18"):
            self.engine.path_to(*distant)
        path = self.engine.path_to(*full_path[:2][-1])
        with self.assertRaisesRegex(RuleError, "one floor tile"):
            self.engine.step_exploration(*path[1])
        self.engine.step_exploration(*path[0])
        self.engine.step_exploration(*path[1])
        self.assertEqual(self.engine.path_cost(path), self.engine.state.travel_ticks)
        self.assertEqual(100 - self.engine.state.travel_ticks // 2, self.engine.state.light)

    def test_travel_cost_comes_from_the_actual_terrain_glyph(self) -> None:
        origin = (self.engine.state.party_x, self.engine.state.party_y)
        x, y = self.engine._neighbors(origin)[0]
        biome_cost = self.engine.biome_mechanics(self.engine.biome_at(x, y))["traversal"]["cost"]
        row = self.engine.state.world_tiles[y]
        self.engine.state.world_tiles[y] = row[:x] + "=" + row[x + 1:]
        self.assertEqual("Service rail", self.engine.terrain_at(x, y)["name"])
        self.assertEqual(1, self.engine.movement_cost(x, y))
        if biome_cost != 1:
            self.assertNotEqual(biome_cost, self.engine.movement_cost(x, y))

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
        self.engine.room(patrol.room_id).biome_id = "derelict"
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
            for upgraded in (False, True):
                with self.subTest(card=card_id, upgraded=upgraded):
                    party = [definition["hero"]]
                    party.extend(hero_id for hero_id in self.catalog.heroes if hero_id not in party)
                    engine = GameEngine.new(self.catalog, 7, start_in_hub=True)
                    engine.state.hub_selection = party[:4]
                    engine.begin_expedition()
                    engine.start_combat("security")
                    actor = next(hero for hero in engine.state.heroes if hero.id == definition["hero"])
                    desired = definition["from_ranks"][0]
                    engine._move(actor, desired - actor.rank)
                    engine.state.hand = [CardInstance(card_id, upgraded=upgraded)]
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

    def test_card_rewards_offer_distinct_tradeoffs_and_reproduce(self) -> None:
        first = GameEngine.new(self.catalog, 731)
        second = GameEngine.new(self.catalog, 731)
        rewards = first._generate_card_rewards(4)
        self.assertEqual(rewards, second._generate_card_rewards(4))
        self.assertEqual(4, len(rewards))
        self.assertEqual(4, len(set(rewards)))

        deck_tags = {
            tag
            for card in first.state.deck
            for tag in first.card_tags(card.card_id)
        }
        desired = {
            f"payoff:{tag.split(':', 1)[1]}"
            for tag in deck_tags
            if tag.startswith("setup:")
        } | {
            f"setup:{tag.split(':', 1)[1]}"
            for tag in deck_tags
            if tag.startswith("payoff:")
        }
        self.assertTrue(first.card_tags(rewards[0]) & desired)
        self.assertGreaterEqual(len({first._reward_shape(card_id) for card_id in rewards}), 3)
        contexts = [first.reward_context(card_id) for card_id in rewards]
        self.assertGreaterEqual(len({kind for kind, _description in contexts}), 2)
        self.assertTrue(all(description for _kind, description in contexts))

    def test_card_reward_may_be_skipped_without_growing_the_deck(self) -> None:
        self.engine.state.rewards = self.engine._generate_card_rewards(3)
        self.engine.state.phase = "reward"
        deck_before = list(self.engine.state.deck)
        self.engine.choose_reward(None)
        self.assertEqual(deck_before, self.engine.state.deck)
        self.assertEqual([], self.engine.state.rewards)
        self.assertEqual("exploration", self.engine.state.phase)

    def test_workshop_transformation_favors_rank_legal_cross_crew_bridges(self) -> None:
        source_index = next(
            index
            for index, card in enumerate(self.engine.state.deck)
            if card.card_id == "baton_strike"
        )
        options = self.engine.transformation_options(source_index)
        self.assertEqual(options, self.engine.transformation_options(source_index))
        self.assertTrue(options)
        self.assertTrue(
            all(self.catalog.cards[card_id]["hero"] == "warden" for card_id in options)
        )
        self.assertTrue(
            any("setup:marked" in self.engine.card_tags(card_id) for card_id in options)
        )
        warden = next(hero for hero in self.engine.living_heroes() if hero.id == "warden")
        self.assertTrue(
            all(warden.rank in self.catalog.cards[card_id]["from_ranks"] for card_id in options)
        )

        room = self.engine.room(1)
        room.kind = "upgrade"
        room.resolved = False
        self.engine.state.current_room = room.id
        self.engine.state.phase = "service"
        self.engine.state.service_type = "upgrade"
        deck_size = len(self.engine.state.deck)
        self.engine.state.deck[source_index].upgraded = True
        self.engine.service("transform", source_index, replacement_id=options[0])
        self.assertEqual(deck_size, len(self.engine.state.deck))
        self.assertEqual(options[0], self.engine.state.deck[source_index].card_id)
        self.assertFalse(self.engine.state.deck[source_index].upgraded)
        self.assertEqual("exploration", self.engine.state.phase)

    def test_every_archetype_can_transform_a_starter_into_a_nonstarter(self) -> None:
        hero_ids = list(self.catalog.heroes)
        for hero_id in hero_ids:
            definition = self.catalog.heroes[hero_id]
            rank = definition["preferred_ranks"][0]
            party = [candidate for candidate in hero_ids if candidate != hero_id][:3]
            party.insert(rank - 1, hero_id)
            engine = GameEngine.new(self.catalog, 12, start_in_hub=True)
            engine.state.hub_selection = party
            engine.begin_expedition()
            starter_ids = set(definition["starter_deck"])
            source_index = next(
                index
                for index, card in enumerate(engine.state.deck)
                if card.card_id in starter_ids
            )
            options = engine.transformation_options(source_index)
            self.assertTrue(
                any(card_id not in starter_ids for card_id in options),
                f"{hero_id} has no non-starter transformation",
            )

    def test_transformation_comparison_exposes_every_changed_dimension(self) -> None:
        comparison = self.engine.transformation_comparison("brace", "breach")
        self.assertEqual("Brace", comparison["source"])
        self.assertEqual("Breach", comparison["destination"])
        self.assertEqual((1, 2), comparison["cost"])
        self.assertEqual(([1, 2], [1]), comparison["ranks"])
        self.assertEqual((["block"], ["damage", "status:vulnerable"]), comparison["effects"])
        self.assertIn("setup:vulnerable", comparison["added_tags"])
        self.assertIn("block", comparison["removed_tags"])

    def test_upgraded_conditional_rescue_cleans_wound(self) -> None:
        self.engine.start_combat("lost_shift")
        medic = next(hero for hero in self.engine.living_heroes() if hero.id == "medic")
        target = self.engine.living_heroes()[0]
        target.hp = 0
        target.deaths_door = True
        target.statuses["wound"] = 3
        self.engine.state.hand = [CardInstance("field_dressing", upgraded=True)]
        self.engine.state.energy = 3
        self.engine.play_card(0, target.id)
        self.assertEqual(10, target.hp)
        self.assertEqual(4, target.block)
        self.assertFalse(target.deaths_door)
        self.assertNotIn("wound", target.statuses)
        self.assertEqual(3, medic.rank)

    def test_hub_rejects_a_fifth_crew_member(self) -> None:
        engine = GameEngine.new(self.catalog, 74, start_in_hub=True)
        with self.assertRaisesRegex(RuleError, "only four"):
            engine.toggle_hub_crew("breacher")

    def test_curated_squad_sets_formation_without_departing(self) -> None:
        engine = GameEngine.new(self.catalog, 74, start_in_hub=True)
        squad = self.catalog.squads["wound_ward"]
        engine.select_curated_squad("wound_ward")
        self.assertEqual(squad["formation"], engine.state.hub_selection)
        self.assertEqual("hub", engine.state.phase)
        engine.begin_expedition()
        self.assertEqual(squad["formation"], [hero.id for hero in engine.living_heroes()])

    def test_party_warnings_explain_but_do_not_block_awkward_formations(self) -> None:
        engine = GameEngine.new(self.catalog, 75, start_in_hub=True)
        engine.state.hub_selection = ["scout", "operative", "hacker", "artillerist"]
        warnings = engine.party_warnings()
        self.assertTrue(any("prefers" in warning for warning in warnings))
        self.assertTrue(any("No defender" in warning for warning in warnings))
        self.assertTrue(any("No support" in warning for warning in warnings))
        engine.begin_expedition()
        self.assertEqual("exploration", engine.state.phase)

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

    def test_one_death_continues_combat_and_removes_owned_cards(self) -> None:
        self.engine.start_combat("lost_shift")
        hero = self.engine.state.heroes[0]
        self.engine.acquire_curse(hero.id, "static_prayer")
        owned = lambda card: (
            card.bound_hero_id == hero.id
            if card.card_id in self.catalog.curses
            else self.catalog.cards[card.card_id]["hero"] == hero.id
        )
        self.engine.state.hand = [CardInstance("baton_strike")]
        self.engine.state.draw_pile = [CardInstance("breach")]
        self.engine.state.discard_pile = [CardInstance("brace")]
        self.engine._damage(hero, hero.max_hp)
        self.assertTrue(hero.deaths_door)
        original = self.catalog.balance["death_chance"]
        self.catalog.balance["death_chance"] = 1.0
        try:
            self.engine._damage(hero, 1)
        finally:
            self.catalog.balance["death_chance"] = original
        self.assertEqual("combat", self.engine.state.phase)
        self.assertFalse(hero.alive)
        self.assertEqual(0, hero.rank)
        self.assertEqual([1, 2, 3], [actor.rank for actor in self.engine.living_heroes()])
        for zone in (
            self.engine.state.deck,
            self.engine.state.hand,
            self.engine.state.draw_pile,
            self.engine.state.discard_pile,
        ):
            self.assertFalse(any(owned(card) for card in zone))
        self.assertNotIn("static_prayer", self.engine.state.curses.get(hero.id, {}))
        rewards = self.engine._generate_card_rewards(4)
        self.assertFalse(any(self.catalog.cards[card_id]["hero"] == hero.id for card_id in rewards))

    def test_full_party_wipe_ends_run(self) -> None:
        self.engine.start_combat("lost_shift")
        original = self.catalog.balance["death_chance"]
        self.catalog.balance["death_chance"] = 1.0
        try:
            for hero in list(self.engine.living_heroes()):
                hero.hp = 0
                hero.deaths_door = True
                self.engine._damage(hero, 1)
        finally:
            self.catalog.balance["death_chance"] = original
        self.assertEqual("defeat", self.engine.state.phase)
        self.assertEqual([], self.engine.living_heroes())

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
        self.assertEqual("combat", self.engine.state.phase)

    def test_enemy_action_playback_is_sequential_and_state_neutral(self) -> None:
        animated = GameEngine.new(self.catalog, 812)
        silent = GameEngine.new(self.catalog, 812)
        animated.room().biome_id = silent.room().biome_id = "archive"
        animated.start_combat("lost_shift")
        silent.start_combat("lost_shift")
        events: list[dict] = []
        animated.end_turn(events.append)
        silent.end_turn()
        self.assertEqual(silent.snapshot(), animated.snapshot())
        self.assertEqual(sorted(event["actor_rank"] for event in events), [
            event["actor_rank"] for event in events
        ])
        self.assertTrue(events)
        for event in events:
            self.assertTrue(event["actor_name"])
            self.assertTrue(event["action"])
            self.assertTrue(event["target_labels"])
            self.assertIn("changes", event)

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

    def test_enemy_guard_redirects_damage_from_combo_enabler(self) -> None:
        self.engine.start_combat(
            "hydro_graft_watch",
            enemy_ids=["graft_sentinel", "pollen_nurse"],
        )
        sentinel, nurse = self.engine.living_enemies()
        attacker = self.engine.living_heroes()[0]
        self.engine._apply_effect(sentinel, [nurse], {"op": "guard", "amount": 2})
        self.assertEqual(sentinel.id, nurse.guarded_by)
        sentinel_hp = sentinel.hp
        nurse_hp = nurse.hp
        self.engine._damage(nurse, 6, attacker)
        self.assertEqual(sentinel_hp - 6, sentinel.hp)
        self.assertEqual(nurse_hp, nurse.hp)

    def test_enemy_ally_support_does_not_target_itself(self) -> None:
        self.engine.start_combat(
            "hydro_graft_watch",
            enemy_ids=["graft_sentinel", "pollen_nurse"],
        )
        sentinel, nurse = self.engine.living_enemies()
        nurse.hp = 1
        target = self.engine._enemy_targets("weakest_ally", sentinel)
        self.assertEqual([nurse.id], [actor.id for actor in target])

    def test_intent_target_is_frozen_and_repetition_is_discouraged(self) -> None:
        self.engine.start_combat("lost_shift", enemy_ids=["hollow_crew"])
        enemy = self.engine.living_enemies()[0]
        medic = next(hero for hero in self.engine.living_heroes() if hero.id == "medic")
        scout = next(hero for hero in self.engine.living_heroes() if hero.id == "scout")
        medic.stress = 60
        target = self.engine._enemy_targets("stressed", enemy)[0]
        self.engine.state.intents = [
            {
                "enemy_rank": enemy.rank,
                "enemy_id": enemy.id,
                "action": "Familiar Face",
                "target_rule": "stressed",
                "target_ids": [target.id],
                "target_labels": [self.engine._intent_target_label(target)],
            }
        ]
        scout.stress = 90
        self.engine._enemy_phase()
        self.assertEqual(70, medic.stress)
        self.assertEqual(90, scout.stress)

        action = next(
            action
            for action in self.catalog.enemies["hollow_crew"]["actions"]
            if action["name"] == "Pipe Swing"
        )
        baseline = self.engine._enemy_action_weight(enemy, action)
        enemy.last_action = action["name"]
        enemy.action_repeats = 2
        self.assertLess(self.engine._enemy_action_weight(enemy, action), baseline)

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
        self.engine.state.party_x, self.engine.state.party_y = self.engine.room_position(1)
        self.engine._resolve_exploration_tile()
        self.engine.service("upgrade", 0)
        self.assertTrue(self.engine.state.deck[0].upgraded)
        self.assertTrue(room.resolved)


if __name__ == "__main__":
    unittest.main()
