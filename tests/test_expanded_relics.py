import copy
import unittest

from jomon.actions import use_gear
from jomon.calendar import ACTIONS_PER_DAY, DAYS_PER_SEASON
from jomon.content import RELICS
from jomon.frontiers import FRONTIER_RELICS, FRONTIERS, build_frontier
from jomon.inventory import auto_place, create_item, item_spec, sync_legacy_load
from jomon.materials import ensure_cell, key
from jomon.regional_history import account_for
from jomon.state import Position, Threat, create_world


NEW_RELICS = {
    "flood-mark clasp", "ashglass lens", "quarry echo pin",
    "winter sounding bead", "red-clay seal", "wreck-light prism",
}


class ExpandedRelicTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("expanded relic regression")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location = "region"
        self.state.position = Position(40, 25)
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        for z in (0, 1):
            for y in range(22, 29):
                for x in range(37, 50):
                    self.state.region.tile_changes[f"{x},{y},{z}"] = "."
        for item in self.state.items:
            if item.owner_id == self.state.active_courier_id and item.location in {
                "pack", "readied", "secondary",
            }:
                item.location, item.owner_id = "lost", None
        sync_legacy_load(self.state)

    def carry(self, relic):
        item = create_item(
            self.state, f"relic:{relic}", "focused relic history",
            owner_id=self.state.active_courier_id,
        )
        self.assertTrue(auto_place(
            self.state, item.id, "pack", owner_id=self.state.active_courier_id
        ))
        self.state.relics[relic] = 1
        sync_legacy_load(self.state)
        self.assertEqual(self.state.carried_relic, relic)

    def threat(self, point, *, profile="pursuer", status="watching", group="claim"):
        actor = Threat(
            f"relic-target-{len(self.state.threats)}", "account claimant", profile,
            point, 10, 10, status=status, group=group, home_position=point,
        )
        self.state.threats.append(actor)
        return actor

    def test_flood_mark_clasp_lowers_water_braces_timber_and_fatigues(self):
        self.carry("flood-mark clasp")
        cell = ensure_cell(self.state, Position(41, 25))
        cell.material, cell.coating, cell.water, cell.support = "timber", "wet", 2, 1
        result = use_gear(self.state)
        self.assertIn("lowers 1 nearby water layers and steadies 1", result.message)
        self.assertGreaterEqual(cell.support, 2)
        self.assertIn("fatigued", self.state.terrain_statuses)
        self.assertNotIn("flood-mark clasp", self.state.relics)

    def test_ashglass_clears_smoke_marks_store_and_reveals_courier(self):
        self.carry("ashglass lens")
        self.state.smoke[key(self.state.position)] = 5
        watcher = self.threat(Position(43, 25))
        result = use_gear(self.state)
        self.assertIn("clears 1 smoke fields", result.message)
        self.assertTrue(self.state.treasure_marks["hearthford"])
        self.assertEqual(watcher.last_known_position, self.state.position)
        self.assertNotIn("ashglass lens", self.state.relics)

    def test_quarry_echo_pin_stabilises_warned_support_and_alerts(self):
        self.carry("quarry echo pin")
        cell = ensure_cell(self.state, Position(41, 25))
        cell.material, cell.support = "stone", 0
        cell.collapse_due = self.state.world_time + 1
        watcher = self.threat(Position(43, 25))
        result = use_gear(self.state)
        self.assertIn("seats 1 damaged supports", result.message)
        self.assertEqual((cell.support, cell.collapse_due), (3, 0))
        self.assertEqual(watcher.status, "engaged")

    def test_winter_sounding_freezes_only_in_season_and_applies_chill(self):
        self.carry("winter sounding bead")
        point = Position(41, 25)
        cell = ensure_cell(self.state, point)
        cell.water, cell.fluid = 1, "fresh"
        before = use_gear(self.state)
        self.assertFalse(before.time_advanced)
        self.assertIn("remains unspent", before.message)
        target_day = 3 * DAYS_PER_SEASON - self.state.calendar_origin_day
        self.state.world_time = target_day * ACTIONS_PER_DAY
        result = use_gear(self.state)
        self.assertIn("freezes 1 fresh shallows", result.message)
        self.assertTrue(cell.ice)
        self.assertIn("chilled", self.state.terrain_statuses)

    def test_red_clay_seal_uses_witnessed_trust_and_records_obligation(self):
        self.carry("red-clay seal")
        institution = account_for(self.state)
        institution.trust = 1
        claimant = self.threat(Position(42, 25), status="engaged")
        before = institution.obligation
        result = use_gear(self.state)
        self.assertIn("settles 1 claimants", result.message)
        self.assertEqual(claimant.status, "negotiated")
        self.assertEqual(institution.obligation, before + 2)
        self.assertEqual(self.state.region.changes["red_clay_compact"], institution.id)

    def test_wreck_light_spends_lamp_marks_cache_and_splits_reactions(self):
        self.carry("wreck-light prism")
        animal = self.threat(Position(42, 25), profile="animal")
        lookout = self.threat(Position(43, 25))
        self.state.lamp_oil = 2
        result = use_gear(self.state)
        self.assertIn("One lamp measure is spent", result.message)
        self.assertEqual(self.state.lamp_oil, 1)
        self.assertTrue(self.state.treasure_marks["hearthford"])
        self.assertEqual(animal.status, "evaded")
        self.assertEqual(lookout.last_known_position, self.state.position)

    def test_all_sixteen_relics_are_inspectable_and_sourced_physically(self):
        placed = set()
        for region_id in FRONTIERS:
            region = build_frontier("expanded relic sources", region_id)
            placed.update(
                reward for container in region.containers
                for reward in container.extra_rewards
            )
        self.assertEqual(len(RELICS), 16)
        self.assertEqual(set().union(*map(set, FRONTIER_RELICS.values())), NEW_RELICS)
        self.assertTrue(NEW_RELICS <= placed)
        for relic in RELICS:
            self.assertEqual(item_spec(f"relic:{relic}").description, RELICS[relic])


if __name__ == "__main__":
    unittest.main()
