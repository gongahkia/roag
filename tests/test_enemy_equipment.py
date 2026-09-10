from __future__ import annotations

import copy
import unittest

from jomon.actions import _threat_action, attack, depart
from jomon.enemy_equipment import (
    actor_items,
    harm_enemy,
    readied_weapon,
)
from jomon.inventory import create_item
from jomon.materials import _expose
from jomon.state import Position, create_world, game_state_from_dict, validate_state
from jomon.terminal import observed_life_lines
from jomon.world import JOMON_GANGPLANK


def expedition(seed: str):
    state = create_world(seed)
    state.position = JOMON_GANGPLANK
    depart(state)
    return state


class PhysicalEnemyEquipmentTests(unittest.TestCase):
    def test_generated_human_actors_have_real_kits_but_animals_do_not(self):
        state = create_world("shared hostile kits")
        humans = [
            actor for actors in state.region_threats.values() for actor in actors
            if actor.profile not in {"animal", "machinery"}
        ]
        animals = [
            actor for actors in state.region_threats.values() for actor in actors
            if actor.profile == "animal"
        ]

        self.assertTrue(humans)
        self.assertTrue(animals)
        for actor in humans:
            self.assertTrue(actor.uses_physical_equipment)
            self.assertIsNotNone(readied_weapon(state, actor))
            self.assertEqual(len(actor_items(state, actor)), 3)
        self.assertTrue(all(not actor_items(state, actor) for actor in animals))
        validate_state(state)

    def test_shared_harm_wears_armour_records_injury_and_drops_survivors(self):
        state = expedition("hostile armour harm")
        actor = next(actor for actor in state.threats if actor.uses_physical_equipment)
        actor.status, actor.health, actor.max_health = "engaged", 4, 4
        torso = next(item for item in actor_items(state, actor) if item.location == "torso")
        old_condition = torso.condition

        first = harm_enemy(
            state, actor, 3, "test bill", damage_kind="cut", location="torso"
        )

        self.assertLess(torso.condition, old_condition)
        self.assertEqual(actor.injuries["torso"], "winded")
        self.assertEqual(first.protection, "Quilted jack")
        second = harm_enemy(
            state, actor, 20, "finishing test", damage_kind="blunt", location="head"
        )
        self.assertTrue(second.defeated)
        self.assertIn("Physical kit falls", second.dropped)
        self.assertTrue(any(
            item.location == "ground" and item.ground_position == actor.position
            for item in state.items
        ))

    def test_material_exposure_affects_hostile_body_and_equipment_together(self):
        state = expedition("hostile material exposure")
        actor = next(actor for actor in state.threats if actor.uses_physical_equipment)
        actor.status = "engaged"
        state.position = Position(actor.position.x - 5, actor.position.y, actor.position.z)
        before_health = actor.health
        before_conditions = {item.id: item.condition for item in actor_items(state, actor)}

        _expose(state, actor.position, "fire", 2)

        self.assertLess(actor.health, before_health)
        self.assertIn("burning", actor.conditions)
        self.assertTrue(any(
            item.condition < before_conditions[item.id]
            for item in actor_items(state, actor)
            if item.id in before_conditions
        ))

    def test_disarmed_actor_recovers_the_exact_visible_ground_weapon(self):
        state = expedition("hostile weapon recovery")
        actor = next(actor for actor in state.threats if actor.uses_physical_equipment)
        actor.status = "engaged"
        old = readied_weapon(state, actor)
        old.location, old.owner_id = "destroyed", None
        spare = create_item(state, "hand axe", "fallen test weapon", location="ground")
        spare.region_id = state.active_region_id
        spare.ground_position = Position(actor.position.x + 1, actor.position.y, actor.position.z)

        message = _threat_action(state, actor, False)

        self.assertIs(readied_weapon(state, actor), spare)
        self.assertIn("exact weapon", message)

    def test_body_kit_and_conditions_round_trip_exactly_and_are_inspectable(self):
        state = expedition("hostile kit persistence")
        actor = next(actor for actor in state.threats if actor.uses_physical_equipment)
        actor.status = "engaged"
        state.position = Position(actor.position.x - 1, actor.position.y, actor.position.z)
        actor.injuries["hands"] = "damaged hand"
        actor.conditions["wet"] = 4
        before = copy.deepcopy(state.to_dict())

        loaded = game_state_from_dict(before)

        self.assertEqual(loaded.to_dict(), before)
        lines = " ".join(observed_life_lines(loaded))
        self.assertIn("PHYSICAL KIT", lines)
        self.assertIn("damaged hand", lines)
        self.assertIn("wet 4", lines)

    def test_defeat_message_separates_weapon_effect_from_route_outcome(self):
        state = expedition("physical kit pty")
        for actor in state.threats:
            actor.status = "defeated"
        actor = next(actor for actor in state.threats if actor.uses_physical_equipment)
        state.position = state.region.landmarks["landing"]
        actor.position = Position(state.position.x + 1, state.position.y, state.position.z)
        actor.status, actor.health = "engaged", 1

        result = attack(state, actor.id)

        self.assertIn(f"The strike removes the {actor.name} from the route:", result.message)
        self.assertNotIn("pulls the target out of position removes", result.message)


if __name__ == "__main__":
    unittest.main()
