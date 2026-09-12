from __future__ import annotations

import copy
import unittest

from jomon.actions import attack
from jomon.chemistry import fill_flask, pour_flask, predicted_reactions
from jomon.content import WEAPONS
from jomon.expanded_weapons import ARSENAL
from jomon.inventory import ITEM_SPECS, auto_place, create_item
from jomon.magic import cast
from jomon.materials import fields, key
from jomon.production import advance_craft_economy, delegate, gather, make, site_position
from jomon.skill_tree import NODES, buy_node, record_milestone
from jomon.state import Position, create_world, game_state_from_dict, validate_state


class ProgressionProductionTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("cross-train material campaign")

    def carried(self, kind: str, quantity: int = 1):
        state = self.state
        item = create_item(state, kind, "focused integration test", quantity=quantity)
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        return item

    def test_thirty_six_additions_are_physical_and_have_live_combat(self):
        state = self.state
        self.assertEqual(len(WEAPONS), 72)
        self.assertEqual(len(ARSENAL), 36)
        self.assertTrue(all(name in ITEM_SPECS for name in ARSENAL))
        state.location = "region"
        state.position = Position(30, 23)
        target = state.threats[0]
        target.position = Position(31, 23)
        target.status = "watching"
        target.aimed_at = state.active_courier_id
        state.weapon = "watch sap"
        result = attack(state, target.id)
        self.assertTrue(result.time_advanced, result.message)
        self.assertIsNone(target.aimed_at)
        self.assertLess(target.health, target.max_health)

    def test_milestone_cross_training_and_spells_round_trip(self):
        state = self.state
        self.assertEqual(len(NODES), 60)
        self.assertTrue(record_milestone(state, "return:hearthford"))
        self.assertFalse(record_milestone(state, "return:hearthford"))
        self.assertEqual(state.courier.skill_points, 1)
        changed, message = buy_node(state, "attunement")
        self.assertTrue(changed, message)
        self.assertIn("rain-bead", state.courier.known_spells)
        self.assertEqual(state.courier.max_mana, 10)
        state.location = "region"
        state.position = state.region.landmarks["landing"]
        before = state.courier.mana
        changed, message = cast(state, "rain-bead", state.position)
        self.assertTrue(changed, message)
        self.assertEqual(state.courier.mana, before - 1)
        loaded = game_state_from_dict(state.to_dict())
        self.assertIn("rain-bead", loaded.courier.known_spells)
        self.assertEqual(loaded.courier.mana, state.courier.mana)

    def test_freeform_flask_reacts_in_shared_sparse_cell(self):
        state = self.state
        state.location = "region"
        state.position = state.region.landmarks["landing"]
        flask = self.carried("field flask")
        first = self.carried("ingredient:tree resin")
        second = self.carried("ingredient:cinder salt")
        self.assertTrue(fill_flask(state, flask.id, first.id)[0])
        self.assertTrue(fill_flask(state, flask.id, second.id)[0])
        self.assertIn("flame bloom", predicted_reactions(flask.contents))
        changed, message = pour_flask(state, flask.id, state.position)
        self.assertTrue(changed, message)
        self.assertEqual(flask.contents, {})
        self.assertIn(key(state.position), fields(state))
        validate_state(state)

    def test_site_gathering_crafting_and_delegated_ground_output(self):
        state = self.state
        state.location = "region"
        state.position = site_position(state)
        changed, message = gather(state, 0)
        self.assertTrue(changed, message)
        self.assertEqual(state.production["sites"]["hearthford"]["stock"], 3)
        self.carried("ingredient:clay", 2)
        self.carried("commodity:wool", 2)
        changed, message = make(state, "make:smoke bomb kit")
        self.assertTrue(changed, message)
        self.assertIn("smoke bomb kit", state.owned_weapons)
        state.courier.speech = 7
        state.trade_credit = 2
        changed, message = delegate(state, "make:smoke bomb kit")
        self.assertTrue(changed, message)
        self.assertEqual(len(state.production["orders"]), 1)
        state.world_time = 36
        advance_craft_economy(state)
        self.assertEqual(state.production["orders"], [])
        self.assertTrue(any(item.kind == "smoke bomb kit" and item.location == "ground"
                            and item.region_id == "hearthford" for item in state.items))
        validate_state(state)

    def test_format_nine_migration_keeps_jomon_progress(self):
        state = self.state
        state.trade_credit = 13
        raw = copy.deepcopy(state.to_dict())
        raw["save_format"] = 9
        raw.pop("production")
        raw.pop("household_formulas")
        for person in [*raw["household"], *raw["visitors"]]:
            for key_name in ("skill_nodes", "skill_milestones", "skill_points", "taught_nodes", "journal_nodes", "mana", "max_mana", "known_spells", "known_formulas"):
                person.pop(key_name, None)
        loaded = game_state_from_dict(raw)
        self.assertEqual(loaded.trade_credit, 13)
        self.assertEqual(len(loaded.production["sites"]), 8)
        self.assertTrue(loaded.courier.skill_nodes)
        validate_state(loaded)


if __name__ == "__main__":
    unittest.main()
