from __future__ import annotations

import copy
import unittest
from unittest.mock import patch

from jomon.actions import attack
from jomon.chemistry import fill_flask, pour_flask, predicted_reactions
from jomon.content import WEAPONS
from jomon.expanded_weapons import ARSENAL, ammunition_for
from jomon.inventory import AMMUNITION_ITEMS, ITEM_SPECS, auto_place, create_item
from jomon.magic import SPELLS, cast, restore_at_shrine
from jomon.materials import fields, key
from jomon.production import advance_craft_economy, delegate, gather, make, site_position
from jomon.skill_tree import NODES, buy_node, record_milestone, study_journal, write_journal
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

    def test_every_new_arm_resolves_a_legal_hit_or_spatial_throw(self):
        base = self.state
        base.location = "region"
        base.weather = "clear"
        base.position = Position(30, 23)
        for name, weapon in ARSENAL.items():
            with self.subTest(weapon=name), patch("jomon.actions._advance_world"):
                state = copy.deepcopy(base)
                state.weapon = name
                target = state.threats[0]
                target.position = Position(30 + weapon.minimum, 23)
                original_position = target.position
                target.status = "watching"
                state.threats = [target]
                state.weapon_ready = 2
                ammunition = ammunition_for(name)
                if ammunition:
                    physical = create_item(state, AMMUNITION_ITEMS[ammunition], "test shot", quantity=3)
                    self.assertTrue(auto_place(state, physical.id, "pack", owner_id=state.active_courier_id))
                result = attack(state, target.id, target_position=target.position)
                if weapon.family in {"bow", "gun"} and "quick" not in weapon.effects:
                    self.assertTrue(result.time_advanced, result.message)
                    result = attack(state, target.id, target_position=target.position)
                self.assertTrue(result.time_advanced, result.message)
                self.assertTrue(target.health < target.max_health or target.position != original_position
                                or target.morale < 2 or weapon.family == "device")

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

    def test_all_twenty_four_spells_have_a_live_spatial_or_personal_effect(self):
        base = self.state
        base.location = "region"
        base.position = Position(30, 23)
        base.courier.max_mana = base.courier.mana = 40
        base.courier.known_spells = list(SPELLS)
        for spell_id, spell in SPELLS.items():
            with self.subTest(spell=spell_id), patch("jomon.actions._advance_world"):
                state = copy.deepcopy(base)
                target = state.threats[0]
                target.position = Position(32, 23)
                target.status = "watching"
                state.threats = [target]
                point = state.position if spell.target == "self" else target.position
                changed, message = cast(state, spell_id, point)
                self.assertTrue(changed, message)
                self.assertEqual(state.courier.mana, 40 - spell.cost)

    def test_physical_journal_transfers_one_root_without_copying_points(self):
        state = self.state
        state.position = Position(56, 5)
        writer = state.courier
        node = writer.skill_nodes[0]
        changed, message = write_journal(state, node)
        self.assertTrue(changed, message)
        journal = next(item for item in state.items if item.kind == "skill journal" and item.lesson_node == node)
        recipient = next(person for person in state.household if person.id != writer.id and node not in person.skill_nodes)
        state.active_courier_id = recipient.id
        points = recipient.skill_points
        changed, message = study_journal(state, journal.id)
        self.assertTrue(changed, message)
        self.assertIn(node, recipient.skill_nodes)
        self.assertEqual(recipient.skill_points, points + 1)  # the first teaching milestone is independent
        self.assertEqual(recipient.taught_nodes, 1)
        validate_state(state)

    def test_cave_mouth_shrine_has_a_daily_limit(self):
        state = self.state
        record_milestone(state, "return:hearthford")
        self.assertTrue(buy_node(state, "attunement")[0])
        state.location = "region"
        state.position = state.region.landmarks["cave_entrance"]
        state.courier.mana = 1
        with patch("jomon.actions._advance_world"):
            changed, message = restore_at_shrine(state)
            self.assertTrue(changed, message)
            self.assertEqual(state.courier.mana, 4)
            self.assertFalse(restore_at_shrine(state)[0])
        validate_state(state)


if __name__ == "__main__":
    unittest.main()
