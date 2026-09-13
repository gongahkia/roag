import unittest

from jomon.actions import interact
from jomon.enemy_equipment import harm_enemy
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.inventory import auto_place, create_item
from jomon.regions import region_reachable, validate_region
from jomon.sanctums import SITES, _site_encounter, _witness, approach, shrine_choice
from jomon.state import create_world, game_state_from_dict
from jomon.world import area_name


class SanctumTests(unittest.TestCase):
    def test_eight_seeded_sites_have_connected_tiers_and_physical_rewards(self):
        footprints = set()
        for index in range(10):
            seed = f"sanctum geometry {index}"
            state = create_world(seed)
            regions = {**state.regions, **{key: build_frontier(seed, key) for key in FRONTIERS}}
            self.assertEqual(set(regions), set(SITES))
            for region in regions.values():
                validate_region(region)
                sites = [box for box in region.containers if "sanctum" in box.id]
                self.assertEqual(len(sites), 2)
                self.assertEqual({box.position.z for box in sites}, {1, 2})
                self.assertTrue(set(region.landmarks[key] for key in (
                    "sanctum_undercroft", "sanctum_entry", "sanctum_ward", "sanctum_boss"
                )) <= region_reachable(region))
                footprints.add((region.id, region.landmarks["sanctum_entry"]))
        self.assertGreaterEqual(len(footprints), 50)

    def test_undercroft_guard_or_witnessed_offering_opens_the_same_stair(self):
        state = create_world("sanctum lower route")
        state.location = "region"
        state.position = state.region.landmarks["sanctum_entry"]
        self.assertIn("sealed", interact(state).message)
        state.position = state.region.landmarks["sanctum_undercroft"]
        self.assertIn("warder", interact(state).message)
        guard = next(actor for actor in state.threats if actor.id == "sanctum:hearthford:lower")
        self.assertEqual(guard.status, "engaged")
        self.assertIn("contests", interact(state).message)
        guard.status = "defeated"
        self.assertTrue(interact(state).time_advanced)
        self.assertEqual(state.region.changes["sanctum:opened_by"], "undercroft")

        offered = create_world("sanctum offering")
        offered.location = "region"
        offered.position = offered.region.landmarks["sanctum_shrine"]
        account = offered.institutions[SITES["hearthford"]["network"]]
        lot = create_item(offered, f"commodity:{account.dependency}", "test offering")
        self.assertTrue(auto_place(offered, lot.id, "pack", owner_id=offered.active_courier_id))
        before = account.trust
        self.assertEqual(interact(offered).overlay, "sanctum")
        self.assertTrue(shrine_choice(offered, "o")[0])
        self.assertEqual(account.trust, min(3, before + 1))
        self.assertEqual(lot.location, "destroyed")
        offered.position = offered.region.landmarks["sanctum_entry"]
        self.assertTrue(interact(offered).time_advanced)
        self.assertIn("Silt-Chancel", area_name(offered))

    def test_boss_persists_and_awards_once(self):
        state = create_world("sanctum boss")
        state.location = "region"
        state.position = state.region.landmarks["sanctum_shrine"]
        self.assertTrue(shrine_choice(state, "b")[0])
        state.position = state.region.landmarks["sanctum_entry"]
        self.assertTrue(interact(state).time_advanced)
        boss = next(actor for actor in state.threats if actor.id == "sanctum:hearthford:boss")
        before_credit, before_strategy = state.trade_credit, state.courier.strategy
        self.assertTrue(harm_enemy(state, boss, 100, "test blow").defeated)
        self.assertEqual(state.trade_credit, before_credit + 4)
        self.assertEqual(state.courier.strategy, min(20, before_strategy + 1))
        harm_enemy(state, boss, 1, "repeated test blow")
        self.assertEqual(state.trade_credit, before_credit + 4)
        loaded = game_state_from_dict(state.to_dict())
        self.assertTrue(loaded.region.changes["sanctum:cleared"])
        self.assertEqual(sum(actor.id == boss.id for actor in loaded.threats), 1)

    def test_site_choice_changes_paired_faction_and_boss_uses_material_duty(self):
        from jomon.ecology import world_options
        from jomon.regions import activate_region

        state = create_world("sanctum paired account")
        state.location = "region"
        account_id = SITES["hearthford"]["network"]
        account = state.institutions[account_id]
        before = account.trust
        self.assertTrue(shrine_choice(state, "b")[0])
        self.assertEqual(account.trust, max(-3, before - 1))
        activate_region(state, "marlbank")
        self.assertEqual(SITES["marlbank"]["network"], account_id)
        self.assertIs(state.institutions[account_id], account)
        self.assertEqual(state.institutions[account_id].trust, max(-3, before - 1))

        self.assertGreaterEqual(len({site["boss"]["duty"] for site in SITES.values()}), 5)
        state.position = state.region.landmarks["sanctum_shrine"]
        self.assertTrue(shrine_choice(state, "b")[0])
        state.position = state.region.landmarks["sanctum_entry"]
        self.assertTrue(interact(state).time_advanced)
        boss = next(actor for actor in state.threats if actor.id == "sanctum:marlbank:boss")
        self.assertEqual(boss.duty, "kindle")
        cell = state.region.materials[f"{boss.objective_position.x},{boss.objective_position.y},2"]
        self.assertGreater(cell.fuel, 0)
        self.assertGreater(boss.supplies, 0)
        self.assertIn("kindle", [choice.action for choice in world_options(state, boss, True)])

        activate_region(state, "greywash")
        state.position = state.region.landmarks["sanctum_shrine"]
        self.assertTrue(shrine_choice(state, "b")[0])
        state.position = state.region.landmarks["sanctum_entry"]
        self.assertTrue(interact(state).time_advanced)
        leader = next(actor for actor in state.threats if actor.id == "sanctum:greywash:boss")
        escort = next(actor for actor in state.threats if actor.id.startswith("sanctum:greywash:escort"))
        escort.health, escort.morale = 2, 0
        self.assertIn("rally ally", [choice.action for choice in world_options(state, leader, True)])

    def test_approach_event_is_bounded_per_expedition_and_old_save_is_upgraded(self):
        state = create_world("sanctum visit")
        state.location = "region"
        state.expedition_count = 1
        state.position = state.region.landmarks["sanctum_shrine"]
        self.assertTrue(approach(state))
        self.assertEqual(approach(state), "")
        self.assertEqual(state.region.changes["sanctum:event_visit"], 1)
        state.expedition_count = 2
        self.assertTrue(approach(state))
        self.assertEqual(state.region.changes["sanctum:event_visit"], 2)
        for visit in range(3, 40):
            state.expedition_count = visit
            approach(state)
        self.assertLessEqual(state.region.changes.get("sanctum:claimants", 0), 3)
        self.assertLessEqual(state.region.changes.get("sanctum:parcels", 0), 2)
        self.assertEqual(game_state_from_dict(state.to_dict()).to_dict(), state.to_dict())

        from jomon.topology import build_region

        old = create_world("sanctum retrofit")
        spatial = build_region(old.seed)
        for field in ("levels", "landmarks", "vertical_links", "containers", "changes", "tile_changes", "geography_signature"):
            setattr(old.region, field, spatial[field])
        loaded = game_state_from_dict(old.to_dict())
        self.assertIn("sanctum_entry", loaded.region.landmarks)
        self.assertEqual(len([box for box in loaded.region.containers if "sanctum" in box.id]), 2)

    def test_room_graphs_vary_and_a_clued_seam_opens_an_optional_loop(self):
        patterns = set()
        for index in range(10):
            state = create_world(f"sanctum route graph {index}")
            region = state.region
            entry = region.landmarks["sanctum_entry"]
            patterns.add(tuple(row[entry.x - 2:entry.x + 13]
                               for row in region.levels["1"][entry.y - 5:entry.y + 6]))
            self.assertEqual(sum("side gallery" in link.name for link in region.vertical_links), 1)
            self.assertEqual(len([box for box in region.containers if "sanctum" in box.id]), 2)
            seam = region.landmarks["sanctum_secret"]
            door = region.landmarks["sanctum_secret_door"]
            self.assertEqual(region.levels["1"][seam.y][seam.x], "*")
            self.assertEqual(region.levels["1"][door.y][door.x], "#")
            state.location = "region"
            state.position = seam
            self.assertTrue(interact(state).time_advanced)
            self.assertEqual(region.tile_changes[f"{door.x},{door.y},1"], "+")
            self.assertFalse(interact(state).changed)
            validate_region(region)
        self.assertGreaterEqual(len(patterns), 6)

    def test_post_boss_site_has_physical_witness_or_finite_elite_contest(self):
        state = create_world("sanctum contest")
        state.location = "region"
        state.position = state.region.landmarks["sanctum_shrine"]
        self.assertTrue(shrine_choice(state, "b")[0])
        state.position = state.region.landmarks["sanctum_entry"]
        self.assertTrue(interact(state).time_advanced)
        boss = next(actor for actor in state.threats if actor.id == "sanctum:hearthford:boss")
        harm_enemy(state, boss, 100, "test blow")
        self.assertEqual(state.region.changes["sanctum:control"], "disputed")
        shrine = state.region.landmarks["sanctum_shrine"]
        self.assertIn("takes the approach", _site_encounter(state, shrine, "elite surveyor"))
        elite = next(actor for actor in state.threats if actor.id.startswith("sanctum:hearthford:elites:"))
        self.assertTrue(elite.elite)
        self.assertEqual(state.region.changes["sanctum:control"], "disputed")
        harm_enemy(state, elite, 100, "test blow")
        self.assertEqual(state.region.changes["sanctum:control"], "open")
        self.assertIn("arrives", _witness(state, shrine))
        visitor = next(person for person in state.contacts["hearthford"]
                       if person.id == "sanctum:hearthford:witness")
        state.position = visitor.position
        self.assertEqual(interact(state).overlay, "sanctum")
        self.assertEqual(game_state_from_dict(state.to_dict()).to_dict(), state.to_dict())

    def test_network_held_site_reuses_one_time_route_shelter(self):
        state = create_world("sanctum route consequence")
        state.location = "region"
        account = state.institutions[SITES["hearthford"]["network"]]
        lot = create_item(state, f"commodity:{account.dependency}", "site offering")
        self.assertTrue(auto_place(state, lot.id, "pack", owner_id=state.active_courier_id))
        self.assertTrue(shrine_choice(state, "o")[0])
        state.position = state.region.landmarks["sanctum_entry"]
        self.assertTrue(interact(state).time_advanced)
        boss = next(actor for actor in state.threats if actor.id == "sanctum:hearthford:boss")
        harm_enemy(state, boss, 100, "test blow")
        self.assertEqual(state.region.changes["sanctum:control"], "network")
        self.assertTrue(shrine_choice(state, "h")[0])
        self.assertIn("network-shelter:network:bank-measures:hearthford", state.vessel_changes)
        self.assertFalse(shrine_choice(state, "h")[0])


if __name__ == "__main__":
    unittest.main()
