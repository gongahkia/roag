import copy
import unittest

from jomon.quests import mark_secondary_lead, use_secondary_service
from jomon.state import Position, Threat, create_world
from jomon.world import cover_at, is_walkable


class CoverAndExhaustionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("loose cover and exhausted rumors")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location, self.state.position = "region", Position(40, 25)
        self.state.threats = []
        for x in range(36, 48):
            for y in range(23, 28):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def test_passable_shutters_and_debris_actually_intercept_low_fire(self):
        state = self.state
        target = Position(44, 25)
        self.assertEqual(cover_at(state, state.position, target), "open")
        state.region.tile_changes["44,25,0"] = "%"
        self.assertTrue(is_walkable(state, target))
        self.assertEqual(cover_at(state, state.position, target), "partial")
        state.region.tile_changes["44,25,0"] = "."
        state.region.tile_changes["43,25,0"] = "%"
        self.assertEqual(cover_at(state, state.position, target), "partial")
        state.region.tile_changes["43,25,0"] = "."
        self.assertEqual(cover_at(state, state.position, target), "open")

    def test_exhausted_cache_rumors_report_no_new_lead_without_crashing(self):
        for container in self.state.region.containers:
            container.opened = True
        self.assertFalse(mark_secondary_lead(self.state))
        changed, text = use_secondary_service(self.state, "c")
        self.assertFalse(changed)
        self.assertIn("no unopened", text.lower())

    def test_actual_arrows_and_stones_share_loose_cover(self):
        from jomon.actions import _threat_action, attack
        from jomon.inventory import auto_place, create_item

        state = self.state
        state.world_time, state.weather, state.weapon = 8, "clear", "sling"
        for item in state.items:
            if item.location == "readied" and item.owner_id == state.active_courier_id:
                item.location, item.owner_id = "lost", None
        create_item(state, "sling", "cover check", location="readied", owner_id=state.active_courier_id)
        stone = create_item(state, "consumable:sling shot pouch", "cover check", quantity=2)
        self.assertTrue(auto_place(state, stone.id, "pack", owner_id=state.active_courier_id))
        actor = Threat("cover-target", "bow ward", "ranged", Position(44, 25), 20, 20, status="engaged", morale=9, ammunition=4, ranged_kind="longbow")
        state.threats.append(actor)
        state.region.tile_changes["44,25,0"] = "%"
        result = attack(state, actor.id)
        self.assertIn("glances from partial cover", result.message)
        self.assertEqual(actor.health, 20)
        actor.position = Position(44, 25)
        actor.aimed_at, actor.reload_turns = state.position, 0
        state.region.tile_changes["40,25,0"] = "%"
        health = state.courier.health
        self.assertIn("partial cover turn", _threat_action(state, actor, False))
        self.assertEqual(state.courier.health, health)

    def test_repeated_rumor_skips_already_marked_containers(self):
        first = len(self.state.treasure_marks["hearthford"])
        self.assertTrue(mark_secondary_lead(self.state))
        self.assertTrue(mark_secondary_lead(self.state))
        self.assertEqual(len(self.state.treasure_marks["hearthford"]), first + 2)
