import copy
import unittest

from jomon.actions import effective_weapon_range, interact
from jomon.frontiers import FRONTIERS
from jomon.inventory import item_spec, terrain_status_for
from jomon.legendary import active_legend, validate_legends
from jomon.materials import handle_material
from jomon.regions import activate_region
from jomon.state import MaterialCell, Position, create_world, game_state_from_dict
from jomon.workshop import effective_spec


class LegendaryObjectTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        state = create_world("finite working legends")
        for region_id in FRONTIERS:
            activate_region(state, region_id)
        cls.base = state

    def setUp(self):
        self.state = copy.deepcopy(self.base)

    def _recover(self, region_id):
        state = self.state
        activate_region(state, region_id)
        state.location = "region"
        state.threats.clear()
        state.region_threats[region_id] = state.threats
        cache = state.region.containers[-1]
        cache.requirement = None
        state.position = cache.position
        state.auto_place_enabled = False
        result = interact(state)
        item = next(item for item in state.items if item.legendary_id == f"legend:{region_id}")
        return result, cache, item

    def _ready(self, item):
        for old in self.state.items:
            if old.owner_id == self.state.active_courier_id and old.location == "readied":
                old.location, old.owner_id = "locker", None
        item.location, item.owner_id, item.container_id = "readied", self.state.active_courier_id, None
        self.state.weapon = item.kind

    def test_eight_seeded_objects_reference_real_history_people_and_caches(self):
        validate_legends(self.state)
        self.assertEqual(len(self.state.legendary_objects), 8)
        self.assertEqual(self.state.to_dict(), create_expanded("finite working legends").to_dict())
        for region_id, region in self.state.regions.items():
            legend = self.state.legendary_objects[f"legend:{region_id}"]
            self.assertEqual(region.containers[-1].legendary_id, legend.id)
            self.assertIn(legend.historical_event_id, {event.id for event in region.regional_history})
            self.assertIn(legend.maker, legend.provenance)
            self.assertIn(region.containers[-1].id, legend.clue)
            self.assertGreaterEqual(len(legend.tags), 2)

    def test_recovery_creates_one_named_physical_object_with_inspectable_tradeoff(self):
        result, cache, item = self._recover("greywash")
        legend = self.state.legendary_objects[item.legendary_id]
        self.assertTrue(result.changed)
        self.assertIn(legend.name, result.message)
        self.assertEqual(item.location, "container")
        spec = effective_spec(self.state, item)
        self.assertEqual(spec.name, legend.name)
        self.assertIn(legend.major_effect, spec.description)
        self.assertIn(legend.tradeoff, spec.description)
        before = len([owned for owned in self.state.items if owned.legendary_id == legend.id])
        self.assertFalse(interact(self.state).changed)
        self.assertEqual(len([owned for owned in self.state.items if owned.legendary_id == legend.id]), before)
        self.assertEqual(game_state_from_dict(self.state.to_dict()).to_dict(), self.state.to_dict())

    def test_named_capabilities_change_range_terrain_and_material_work(self):
        _, _, white = self._recover("whitecairn")
        self._ready(white)
        base_range = 14  # the ordinary trestle arbalest's catalogue range
        self.assertEqual(effective_weapon_range(self.state), base_range + 1)
        self.assertEqual(active_legend(self.state).id, "legend:whitecairn")
        point = Position(self.state.position.x + 1, self.state.position.y, self.state.position.z)
        self.state.region.materials[f"{point.x},{point.y},{point.z}"] = MaterialCell(material="timber", support=1)
        self.state.gear = None
        self.state.courier.technique = ""
        self.assertTrue(handle_material(self.state, "brace", point)[0])

        frost = copy.deepcopy(self.base)
        self.state = frost
        _, _, spear = self._recover("frostmere")
        self._ready(spear)
        self.assertIsNone(terrain_status_for(self.state, "_"))
        self.assertIn("ice-grip", effective_spec(self.state, spear).tags)
        self.assertNotIn("ice-grip", item_spec("spear").tags)


def create_expanded(seed):
    state = create_world(seed)
    for region_id in FRONTIERS:
        activate_region(state, region_id)
    return state


if __name__ == "__main__":
    unittest.main()
