import copy
import unittest

from jomon.actions import interact, move
from jomon.frontiers import FRONTIERS
from jomon.content import COMMODITIES, validate_commodity_content
from jomon.inventory import create_item, auto_place, item_spec, sync_legacy_load
from jomon.materials import _expose, handle_material
from jomon.quests import use_secondary_service
from jomon.regional_history import (
    NETWORK_ACCOUNTS, NETWORK_CONTACTS, account_for, advance_production,
    deliver_dependency, ledger_lines, open_network_shelter,
    deliver_network_dependency, validate_accounts,
)
from jomon.regions import activate_region
from jomon.state import MaterialCell, Position, StateError, create_world, game_state_from_dict
from jomon.world import sight_radius


class WorkingHistoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("working account tests")

    def setUp(self):
        self.state = copy.deepcopy(self.base)

    def test_seeded_chains_reference_real_contacts_places_and_caches(self):
        state = self.state
        self.assertEqual(state.to_dict(), create_world(state.seed).to_dict())
        for region_id in FRONTIERS:
            activate_region(state, region_id)
        validate_accounts(state)
        self.assertEqual(len(state.institutions), 12)
        self.assertEqual(sum(len(r.regional_history) for r in state.regions.values()), 40)
        for region in state.regions.values():
            self.assertEqual(region.regional_history[-1].evidence, region.containers[-1].id)
            self.assertTrue(region.materials)
            self.assertTrue(any("repairs depend on" in n.description for n in state.route_nodes.values() if n.region_id == region.id))

    def test_institutions_form_material_network_with_services_and_opposition(self):
        state = self.state
        for region_id in FRONTIERS:
            activate_region(state, region_id)
        for account in state.institutions.values():
            self.assertTrue(account.relationships)
            self.assertTrue(account.service)
            self.assertTrue(account.opposition_reason)
            self.assertTrue(set(account.relationships) <= set(state.institutions))
        hearth = state.institutions["work:hearthford"]
        white = state.institutions["work:whitecairn"]
        self.assertIn(white.id, hearth.relationships)
        self.assertIn("ironwork", hearth.relationships[white.id])

    def test_histories_vary_real_materials_work_and_guard_accounts(self):
        other = create_world("another working history")
        self.assertNotEqual(self.state.region.generation_facts, other.region.generation_facts)
        self.assertNotEqual({k: r.materials for k, r in self.state.regions.items()}, {k: r.materials for k, r in other.regions.items()})
        for region_id, institution in (
            (a.region_id, a) for a in self.state.institutions.values()
            if a.id.startswith("work:")
        ):
            region = self.state.regions[region_id]
            self.assertEqual(region.generation_facts["dependency"], institution.dependency)
            self.assertTrue(any(t.group == institution.id for t in self.state.region_threats[region_id]))

    def test_v6_fixture_adds_testimony_without_repainting_or_reissuing(self):
        data = self.state.to_dict()
        data["save_format"] = 6
        data.pop("institutions")
        for region in data["regions"].values():
            region.pop("generation_facts")
            region.pop("regional_history")
        first = game_state_from_dict(data)
        self.assertEqual(first.to_dict(), game_state_from_dict(data).to_dict())
        self.assertEqual(first.to_dict()["items"], data["items"])
        for region_id, old in data["regions"].items():
            for field in ("levels", "tile_changes", "containers", "materials", "seen"):
                self.assertEqual(first.to_dict()["regions"][region_id][field], old[field])
            self.assertEqual(first.regions[region_id].generation_facts["evidence_mode"], "inherited testimony")

    def test_production_consumes_real_stock_on_clock_boundaries_only(self):
        state = self.state
        account = account_for(state)
        need = state.market[account.dependency]
        product = state.market[account.production]
        need.stock, product.stock = 3, 0
        before = state.to_dict()
        advance_production(state)
        self.assertEqual(state.to_dict(), before)
        for other in state.institutions.values():
            if other.id != account.id:
                other.last_day = 1
        state.world_time = 36
        advance_production(state)
        self.assertEqual((need.stock, product.stock), (2, 1))
        before = state.to_dict()
        self.assertEqual(game_state_from_dict(before).to_dict(), before)
        advance_production(state)
        self.assertEqual(state.to_dict(), before)

    def test_remote_catch_up_is_bounded_and_does_not_replay_history(self):
        state = self.state
        state.world_time = 360000
        advance_production(state)
        self.assertEqual(len(state.institutions), 8)
        self.assertTrue(all(account.last_day == 10000 for account in state.institutions.values()))
        self.assertTrue(all(len(region.regional_history) == 5 for region in state.regions.values()))

    def test_four_networks_have_eight_distinct_embodied_witnesses(self):
        state = self.state
        for region_id in FRONTIERS:
            activate_region(state, region_id)
        self.assertEqual(
            {key for key in state.institutions if key.startswith("network:")},
            set(NETWORK_ACCOUNTS),
        )
        self.assertEqual(len(NETWORK_CONTACTS), 8)
        for institution_id, region_id, contact_id, *_ in NETWORK_CONTACTS:
            contact = next(
                candidate for candidate in state.contacts[region_id]
                if candidate.id == contact_id
            )
            self.assertEqual(contact.position, state.actor_schedules[contact.id].position)
            self.assertIn(f"work:{region_id}", state.institutions[institution_id].relationships)
            self.assertNotIn(
                contact.position,
                {actor.position for actor in state.region_threats[region_id]},
            )

    def test_network_supply_opens_one_persistent_safer_route_service(self):
        state = self.state
        contact_id = "network-contact-hearthford"
        institution = state.institutions["network:bank-measures"]
        item = create_item(
            state, f"commodity:{institution.dependency}",
            "network service test",
        )
        self.assertTrue(auto_place(
            state, item.id, "pack", owner_id=state.active_courier_id,
        ))
        before_edges = [
            (edge.id, edge.cargo_risk, edge.weather_exposure)
            for edge in state.route_edges if "hearthford" in {edge.first, edge.second}
        ]
        self.assertTrue(deliver_network_dependency(state, contact_id)[0])
        self.assertEqual((item.location, institution.trust), ("destroyed", 1))
        self.assertTrue(open_network_shelter(state, contact_id)[0])
        after_edges = [
            (edge.id, edge.cargo_risk, edge.weather_exposure)
            for edge in state.route_edges if "hearthford" in {edge.first, edge.second}
        ]
        self.assertNotEqual(after_edges, before_edges)
        self.assertEqual(institution.obligation, 1)
        self.assertFalse(open_network_shelter(state, contact_id)[0])
        restored = game_state_from_dict(state.to_dict())
        self.assertEqual(restored.to_dict(), state.to_dict())

    def test_network_account_uses_the_same_bounded_daily_market_reducer(self):
        state = self.state
        network = state.institutions["network:bank-measures"]
        for institution in state.institutions.values():
            institution.last_day = 1
        network.last_day = 0
        market = state.regional_markets[network.region_id]
        market[network.dependency].stock = 3
        market[network.production].stock = 0
        state.world_time = 36
        advance_production(state)
        self.assertEqual(
            (market[network.dependency].stock, market[network.production].stock),
            (2, 1),
        )

    def test_existing_format_seven_state_adds_networks_without_moving_people(self):
        data = self.state.to_dict()
        old_contacts = {
            region_id: [contact["position"] for contact in contacts[:2]]
            for region_id, contacts in data["contacts"].items()
        }
        data["institutions"] = {
            key: value for key, value in data["institutions"].items()
            if key.startswith("work:")
        }
        for region_id in data["contacts"]:
            data["contacts"][region_id] = data["contacts"][region_id][:2]
        data["actor_schedules"] = {
            key: value for key, value in data["actor_schedules"].items()
            if not key.startswith("network-contact-")
        }
        loaded = game_state_from_dict(data)
        self.assertEqual(
            {
                region_id: [
                    {
                        "x": contact.position.x, "y": contact.position.y,
                        "z": contact.position.z,
                    }
                    for contact in contacts[:2]
                ]
                for region_id, contacts in loaded.contacts.items()
            },
            old_contacts,
        )
        self.assertEqual(len(loaded.institutions), 8)
        self.assertTrue(all(len(contacts) == 3 for contacts in loaded.contacts.values()))
        self.assertEqual(game_state_from_dict(loaded.to_dict()).to_dict(), loaded.to_dict())

    def test_physical_supply_contract_unlocks_bounded_injury_care(self):
        state = self.state
        account = account_for(state)
        state.market[account.dependency].stock = 0
        for _ in range(2):
            item = create_item(state, f"commodity:{account.dependency}", "working account test")
            self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
            self.assertTrue(deliver_dependency(state)[0])
            self.assertEqual(item.location, "destroyed")
        self.assertEqual(account.trust, 2)
        state.trade_credit = 0
        state.courier.injuries["feet"] = "split sole"
        old_obligation = account.obligation
        self.assertTrue(use_secondary_service(state, "h")[0])
        self.assertNotIn("feet", state.courier.injuries)
        self.assertEqual(account.obligation, old_obligation + 1)
        self.assertEqual(len(account.witnessed_acts), 2)
        self.assertIn("delivered a witnessed", account.witnessed_acts[-1])

    def test_commodity_lifecycle_is_inspectable_and_carried_condition_is_physical(self):
        validate_commodity_content()
        for name, definition in COMMODITIES.items():
            description = item_spec(f"commodity:{name}").description
            self.assertIn(definition["handling"], description)
            self.assertIn(definition["failure"], description)
            self.assertGreaterEqual(len(definition["buyers"]), 2)
        first = create_item(self.state, "commodity:grain", "condition test", owner_id=self.state.active_courier_id, quantity=1)
        second = create_item(self.state, "commodity:grain", "condition test", owner_id=self.state.active_courier_id, quantity=2)
        self.assertTrue(auto_place(self.state, first.id, "pack", owner_id=self.state.active_courier_id))
        self.assertTrue(auto_place(self.state, second.id, "pack", owner_id=self.state.active_courier_id))
        second.condition = 60
        sync_legacy_load(self.state)
        self.assertEqual(self.state.carried_goods["grain"].quantity, 3)
        self.assertEqual(self.state.carried_goods["grain"].condition, "weathered dry")

    def test_material_exposure_updates_cargo_condition_and_roundtrips(self):
        state = self.state
        state.location = "region"
        state.position = Position(40, 24)
        item = create_item(state, "commodity:paper", "wet account", owner_id=state.active_courier_id, quantity=1)
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        _expose(state, state.position, "water", 8)
        self.assertEqual(state.carried_goods["paper"].condition, "weathered dry")
        restored = game_state_from_dict(state.to_dict())
        self.assertEqual(restored.carried_goods["paper"].condition, "weathered dry")
        self.assertEqual(restored.to_dict(), state.to_dict())

    def test_damaged_objective_cargo_is_accepted_with_a_visible_reduced_account(self):
        state = self.state
        state.location = "region"
        state.objective_status = "accepted"
        schedule = state.actor_schedules[state.contact.id]
        state.position = schedule.position
        commodity = state.region.objective_commodity
        item = create_item(state, f"commodity:{commodity}", "damaged objective", owner_id=state.active_courier_id, quantity=2)
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        item.condition = 40
        sync_legacy_load(state)
        result = interact(state)
        self.assertTrue(result.changed)
        self.assertEqual(state.objective_status, "completed")
        self.assertIn("accepted under pressure", result.message)
        self.assertTrue(state.institutions["work:hearthford"].witnessed_acts)

    def test_ledger_is_zero_time_and_distinguishes_testimony_forecast(self):
        state = self.state
        before = state.to_dict()
        lines = ledger_lines(state)
        self.assertTrue(any(line.startswith("FACT") for line in lines))
        self.assertTrue(any(line.startswith("TESTIMONY") for line in lines))
        self.assertTrue(any(line.startswith("FORECAST") for line in lines))
        self.assertEqual(state.to_dict(), before)

    def test_broken_history_chain_and_unknown_institution_are_rejected(self):
        for field, value in (("previous", "missing event"), ("institution", "unknown workers")):
            data = self.state.to_dict()
            data["regions"]["hearthford"]["regional_history"][0][field] = value
            with self.assertRaises(StateError):
                game_state_from_dict(data)

    def test_local_structure_lessons_enable_tool_free_bracing_and_quiet_control(self):
        for technique in ("mill hearing", "bell interval"):
            state = copy.deepcopy(self.base)
            state.location = "region"
            state.position = Position(40, 24)
            state.threats.clear()
            state.weapon, state.gear = "staff", "buckler"
            state.courier.learned_techniques.append(technique)
            state.region.materials["40,24,0"] = MaterialCell(material="timber", support=1)
            self.assertTrue(handle_material(state, "brace", state.position)[0])
            self.assertEqual(state.region.materials["40,24,0"].support, 3)
            activate_region(state, "dunmire")
            state.position = state.region.landmarks["control"]
            state.threats.clear()
            before = state.noise
            self.assertTrue(interact(state).time_advanced)
            self.assertEqual(state.noise, before)

    def test_smoke_and_shore_lessons_change_sight_and_crossing(self):
        state = self.state
        state.location = "region"
        state.position = Position(40, 24)
        state.threats.clear()
        state.smoke["40,24,0"] = 4
        state.courier.learned_techniques.append("smoke spoor")
        self.assertGreaterEqual(sight_radius(state), 5)
        state.smoke.clear()
        state.weather = "coast squall"
        before = sight_radius(state)
        state.courier.learned_techniques.append("shoreline measure")
        self.assertEqual(sight_radius(state), before + 2)
        state.water["41,24,0"] = 4
        state.region.tile_changes["41,24,0"] = "."
        state.gear, state.courier.technique = "buckler", "quiet passage"
        clock = state.world_time
        self.assertTrue(move(state, 1, 0).time_advanced)
        self.assertEqual(state.world_time, clock + 1)


if __name__ == "__main__":
    unittest.main()
