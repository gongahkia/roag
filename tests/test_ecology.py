import copy
import unittest

from jomon.actions import _advance_world, _threat_action, attack
from jomon.content import ENEMY_ARCHETYPES, FRONTIER_ACTORS
from jomon.ecology import ACTOR_BUDGET, active_actors, opposed, world_options
from jomon.encounters import frontier_population, threat_from_archetype
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.inventory import create_item, release_enemy_possession
from jomon.materials import advance_materials, affect_body
from jomon.regions import activate_region, region_reachable
from jomon.state import MaterialCell, Position, Threat, create_world, game_state_from_dict
from jomon.terminal import observed_life_lines
from jomon.world import position_key


class EcologyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("finite ecological duties")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location = "region"
        self.state.position = Position(32, 20)
        self.state.threats.clear()
        self.state.region.materials.clear()
        self.state.smoke.clear()
        for y in range(12, 35):
            for x in range(24, 56):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def actor(self, kind, point=Position(38, 20)):
        actor = threat_from_archetype(kind, point, encounter_id="exercise", group="same work")
        actor.status = "engaged"
        self.state.threats.append(actor)
        return actor

    def test_every_new_archetype_has_an_exercised_production_action(self):
        for row in FRONTIER_ACTORS:
            with self.subTest(archetype=row[0]):
                self.setUp()
                state = self.state
                actor = self.actor(row[0])
                actor.objective_position = Position(39, 20)
                cell = MaterialCell(material="timber", fire=1, fuel=3, water=2, support=1)
                state.region.materials["39,20,0"] = cell
                if actor.duty in {"kindle", "cut support"}:
                    cell.fire, cell.water = 0, 0
                if actor.duty in {"heal", "rally", "escort"}:
                    ally = Threat("wounded", "wounded carrier", "pursuer", Position(39, 20), 1, 5, group=actor.group, morale=0)
                    state.threats.append(ally)
                if actor.duty == "hunt":
                    state.threats.append(Threat("prey", "observed prey", "animal", Position(39, 20), 3, 3, ecology="prey", allegiance="prey"))
                if actor.duty == "scavenge":
                    item = create_item(state, "passive:witness token", "abandoned by an adult", location="ground")
                    item.region_id, item.ground_position = state.active_region_id, Position(39, 20)
                old_position = actor.position
                old_supplies = actor.supplies
                message = _threat_action(state, actor, False)
                self.assertTrue(actor.goal_reason)
                if actor.duty in {"kindle", "cut support"}:
                    self.assertIn("prepares", message)
                    _threat_action(state, actor, False)
                    self.assertTrue(cell.fire if actor.duty == "kindle" else cell.collapse_due)
                    self.assertEqual(actor.supplies, old_supplies - 1)
                elif actor.duty == "quench":
                    self.assertEqual(cell.fire, 0)
                elif actor.duty == "brace":
                    self.assertEqual(cell.support, 3)
                elif actor.duty == "drain":
                    self.assertEqual(cell.water, 0)
                elif actor.duty == "heal":
                    self.assertEqual(ally.health, 3)
                elif actor.duty in {"rally", "escort"}:
                    self.assertEqual(ally.morale, 1)
                elif actor.duty == "hunt":
                    prey = state.threats[-1]
                    before_health = prey.health
                    _threat_action(state, actor, False)
                    self.assertLess(prey.health, before_health)
                elif actor.duty == "scavenge":
                    self.assertEqual(item.location, "enemy")
                    self.assertEqual(actor.carrying_item_id, item.id)
                else:
                    self.assertEqual(actor.ecology, "prey")
                    self.assertNotEqual(actor.position, old_position)

    def test_firefighter_changes_actual_material_and_runs_out(self):
        actor = self.actor("fen-pail")
        cell = MaterialCell(material="timber", fire=2, fuel=5)
        self.state.region.materials["39,20,0"] = cell
        before = actor.supplies
        message = _threat_action(self.state, actor, False)
        self.assertIn("quench", message)
        self.assertEqual(cell.fire, 0)
        self.assertGreater(cell.water, 0)
        self.assertEqual(actor.supplies, before - 1)
        actor.supplies, cell.fire = 0, 2
        _threat_action(self.state, actor, False)
        self.assertEqual(cell.fire, 2)

    def test_preparation_can_be_interrupted_or_defeated_by_water(self):
        actor = self.actor("fen-cinder")
        actor.objective_position = Position(39, 20)
        _threat_action(self.state, actor, False)
        actor.intent = "disrupted by an ordinary hook"
        _threat_action(self.state, actor, False)
        self.assertEqual(actor.reaction, "")
        self.assertIsNone(actor.marked_position)
        _threat_action(self.state, actor, False)
        self.state.region.materials["39,20,0"].water = 1
        message = _threat_action(self.state, actor, False)
        self.assertIn("Water defeats", message)
        self.assertEqual(self.state.region.materials["39,20,0"].fire, 0)

    def test_predator_changes_target_to_visible_prey_not_hidden_courier(self):
        state = self.state
        hunter = self.actor("fen-lynx")
        prey = self.actor("fen-hare", Position(39, 20))
        state.position = Position(70, 40)
        before = prey.health
        first = _threat_action(state, hunter, False)
        self.assertIn(prey.name, first)
        self.assertEqual(prey.health, before)
        _threat_action(state, hunter, False)
        self.assertLess(prey.health, before)
        self.assertIsNone(hunter.last_known_position)

    def test_rival_groups_fight_but_same_allegiance_does_not(self):
        ward = self.actor("fen-bracer")
        raider = self.actor("fen-recoverer", Position(39, 20))
        self.assertTrue(opposed(ward, raider))
        _threat_action(self.state, ward, False)
        _threat_action(self.state, ward, False)
        self.assertLess(raider.health, raider.max_health)
        raider.allegiance = ward.allegiance
        self.assertFalse(opposed(ward, raider))

    def test_group_alerts_do_not_transmit_across_the_whole_region(self):
        from jomon.enemy_ai import perceive, raise_group_alert

        caller = self.actor("gorge-caller")
        distant = self.actor("gorge-escort", Position(70, 40))
        distant.status = "watching"
        raise_group_alert(self.state, caller)
        self.assertEqual(distant.status, "watching")
        self.assertIsNone(perceive(self.state, distant)[1])
        self.assertIsNone(distant.last_known_position)

    def test_untargeted_attack_does_not_pick_a_peaceful_grazer(self):
        prey = self.actor("fen-hare", Position(33, 20))
        before = self.state.to_dict()
        attack(self.state)
        self.assertEqual(prey.health, prey.max_health)
        self.assertEqual(self.state.world_time, before["world_time"])
        attack(self.state, prey.id)
        self.assertLess(prey.health, prey.max_health)

    def test_quest_assigns_an_adult_guard_not_a_grazing_animal(self):
        from jomon.actions import decide_objective

        state = create_world("working fen")
        activate_region(state, "dunmire")
        state.location, state.position = "region", state.contact.position
        decide_objective(state, "alter")
        guard_id = state.region.changes["quest_guard_id"]
        actor = next(actor for actor in state.threats if actor.id == guard_id)
        self.assertNotEqual(actor.profile, "animal")
        self.assertEqual(actor.home_position, state.region.landmarks["objective"])

    def test_prey_flees_and_does_not_attack_courier(self):
        actor = self.actor("fen-hare", Position(33, 20))
        before = self.state.courier.health
        old = actor.position
        _threat_action(self.state, actor, False)
        self.assertNotEqual(actor.position, old)
        self.assertEqual(self.state.courier.health, before)
        self.assertEqual(actor.goal, "avoid hunters")

    def test_scavenged_unique_item_drops_after_fire_or_rival_defeat(self):
        state = self.state
        thief = self.actor("fen-recoverer")
        item = create_item(state, "passive:witness token", "fallen pack", location="ground")
        item.region_id, item.ground_position = state.active_region_id, Position(39, 20)
        _threat_action(state, thief, False)
        self.assertEqual(item.location, "enemy")
        self.assertEqual(thief.carrying_item_id, item.id)
        affect_body(state, thief, "fire", 10, thief.position)
        self.assertEqual(item.location, "ground")
        self.assertEqual(item.ground_position, thief.position)
        self.assertIsNone(thief.carrying_item_id)
        self.assertEqual(release_enemy_possession(state, thief), "")

    def test_fire_escape_does_not_follow_unseen_courier(self):
        actor = self.actor("fen-recoverer")
        self.state.region.materials[position_key(actor.position)] = MaterialCell(material="timber", fire=2, fuel=4)
        self.state.position = Position(70, 40)
        old = actor.position
        _threat_action(self.state, actor, False)
        self.assertNotEqual(actor.position, old)
        self.assertIsNone(actor.last_known_position)

    def test_watching_actor_works_without_courier_awareness(self):
        state = self.state
        actor = self.actor("fen-bracer", Position(38, 20))
        actor.status, actor.vision = "watching", 4
        cell = MaterialCell(material="timber", support=1)
        state.region.materials["39,20,0"] = cell
        _advance_world(state)
        self.assertEqual(cell.support, 3)
        self.assertIsNone(actor.last_known_position)

    def test_far_actors_are_bounded_and_do_not_advance_offline(self):
        state = self.state
        for index in range(40):
            state.threats.append(Threat(str(index), "bounded observer", "animal", Position(34 + index % 5, 20 + index // 5), 3, 3, ecology="prey"))
        self.assertEqual(len(active_actors(state)), ACTOR_BUDGET)
        actor = state.threats[-1]
        actor.position = Position(90, 45)
        before = copy.deepcopy(actor)
        _advance_world(state)
        self.assertEqual(actor, before)

    def test_existing_and_new_actor_state_round_trips_without_resurrection(self):
        state = self.state
        actor = self.actor("gorge-cutter")
        actor.objective_position = Position(39, 20)
        _threat_action(state, actor, False)
        self.assertIsNotNone(actor.marked_position)
        data = state.to_dict()
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.to_dict(), data)
        self.assertNotIn("_actor_chunks", data)

    def test_inspection_lists_only_visible_actors_and_real_counters(self):
        actor = self.actor("fen-pail")
        hidden = self.actor("fen-cinder", Position(90, 40))
        before = self.state.to_dict()
        lines = " ".join(observed_life_lines(self.state))
        self.assertIn(actor.name, lines)
        self.assertNotIn(hidden.name, lines)
        self.assertIn("COUNTERS:", lines)
        self.assertEqual(self.state.to_dict(), before)


class EcologyGenerationTests(unittest.TestCase):
    def test_new_populations_are_finite_reachable_and_seeded(self):
        seen = set()
        for seed in range(12):
            for region_id in FRONTIERS:
                region = build_frontier(str(seed), region_id)
                actors = frontier_population(str(seed), region)
                self.assertEqual(actors, frontier_population(str(seed), region))
                self.assertEqual(len(actors), 8)
                self.assertEqual(len({a.position for a in actors}), 8)
                reachable = region_reachable(region)
                self.assertTrue(all(a.position in reachable for a in actors))
                self.assertTrue(all(a.status == "watching" and a.aimed_at is None for a in actors))
                self.assertTrue({"prey", "predator"} <= {a.ecology for a in actors})
                seen.update(a.id.split(":", 1)[-1] for a in actors)
        self.assertTrue({row[0] for row in FRONTIER_ACTORS} <= seen)
