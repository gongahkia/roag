from __future__ import annotations

import unittest
from unittest.mock import patch

from roag.commands import TerrainActionCommand
from roag.inventory import (
    ensure_initial_field_tool, load_state, sync_legacy_load, transfer_to_grid,
)
from roag.main import _start_new_world
from roag.opening_audit import opening_audit, opening_profile
from roag.session import GameSession
from roag.state import Position, create_world, validate_state
from roag.terrain_actions import terrain_action_power


class OpeningAuditTests(unittest.TestCase):
    def test_profile_is_deterministic_and_uses_actual_direct_start(self):
        first = opening_profile("opening audit deterministic")
        second = opening_profile("opening audit deterministic")

        self.assertEqual(first, second)
        self.assertEqual(first.region_id, "hearthford")
        self.assertEqual((first.world_time, first.expedition_count), (0, 1))
        self.assertGreater(first.visible_cells, 0)
        self.assertIsNotNone(first.nearest_destructible_steps)
        self.assertIsNotNone(first.production_site_steps)

    def test_generated_openings_report_loadout_and_engine_access_gaps(self):
        result = opening_audit(4)

        self.assertEqual(result["failures"], [])
        self.assertEqual(
            result["terrain_access_samples_by_role"],
            {role: 4 for role in (
                "bargemaster", "pilot", "factor", "carpenter", "guard", "healer",
            )},
        )
        self.assertEqual(
            result["core_engine_ready_samples_by_role"],
            {role: 0 for role in (
                "bargemaster", "pilot", "factor", "carpenter", "guard", "healer",
            )},
        )
        self.assertEqual(len(result["gaps"]), 1)
        self.assertLessEqual(result["nearest_destructible_steps"]["max"], 10)
        self.assertGreaterEqual(result["nearest_active_threat_steps"]["min"], 12)
        self.assertEqual(
            result["initial_threats"], {"min": 8, "median": 8.0, "max": 8},
        )
        self.assertEqual(
            result["initial_active_threats"],
            {"min": 7, "median": 7.0, "max": 7},
        )

        for profile in result["profiles"]:
            self.assertTrue(
                {"terrain.region.mud", "terrain.region.standing_timber"}
                <= {row[0] for row in profile["destructible_cells"]},
            )
            self.assertEqual(
                profile["production_sources"], ("iron filings", "spring water"),
            )
            self.assertEqual(profile["shore_stations"], ("workshop", "forge"))
            for role in profile["roles"]:
                self.assertEqual(role["optimistic_circuit_recipes"], ())
                missing = dict(role["core_engine_missing_inputs"])
                self.assertTrue(missing["circuit-rack"])
                self.assertTrue(missing["circuit-sensor"])

    def test_new_run_issues_one_physical_tool_without_replacing_role_kit(self):
        state = create_world("gameplay three initial field tool")
        pilot = next(person for person in state.household if person.role == "pilot")
        state.active_courier_id = pilot.id
        sync_legacy_load(state)
        original_load = load_state(state)
        self.assertEqual((state.weapon, state.gear), ("staff", "quiet shoes"))
        self.assertEqual(terrain_action_power(state, "cut"), 0)

        screen = object()
        with patch("roag.main.run_character_creation", return_value=True), \
                patch("roag.main.play") as play:
            self.assertTrue(_start_new_world(screen, state))

        tools = [
            item for item in state.items
            if item.kind == "reed sickle" and item.owner_id == pilot.id
            and item.location == "pack"
        ]
        self.assertEqual(len(tools), 1)
        self.assertTrue(state.vessel_changes[f"acquired:{tools[0].id}"])
        self.assertEqual((state.weapon, state.gear), ("staff", "quiet shoes"))
        self.assertEqual(load_state(state), original_load)
        self.assertEqual(terrain_action_power(state, "cut"), 2)
        self.assertEqual((state.world_time, state.expedition_count), (0, 1))
        self.assertEqual(state.position, state.region.landmarks["landing"])
        play.assert_called_once_with(screen, state)
        validate_state(state)

        self.assertIsNone(ensure_initial_field_tool(state))
        self.assertEqual(
            sum(item.kind == "reed sickle" and item.owner_id == pilot.id
                for item in state.items),
            1,
        )

    def test_pack_tool_is_authoritative_for_work_but_not_from_locker(self):
        state = create_world("gameplay three carried tool authority")
        pilot = next(person for person in state.household if person.role == "pilot")
        state.active_courier_id = pilot.id
        sync_legacy_load(state)
        self.assertEqual(ensure_initial_field_tool(state), "reed sickle")
        tool = next(
            item for item in state.items
            if item.kind == "reed sickle" and item.owner_id == pilot.id
        )
        from roag.regions import begin_region

        begin_region(state, "hearthford")
        landing = state.position
        target = Position(landing.x - 1, landing.y, landing.z)
        state.region.tile_changes[f"{target.x},{target.y},{target.z}"] = "T"
        outcome = GameSession(state).submit(TerrainActionCommand("cut", target))
        self.assertTrue(outcome.changed)
        self.assertEqual(state.world_time, 1)

        state.region.terrain_damage.clear()
        self.assertTrue(transfer_to_grid(state, tool.id, "locker"))
        self.assertEqual(terrain_action_power(state, "cut"), 0)
        before = state.world_time
        rejected = GameSession(state).submit(TerrainActionCommand("cut", target))
        self.assertFalse(rejected.changed)
        self.assertEqual(state.world_time, before)

    def test_field_issue_is_role_bounded_and_preserves_every_load_band(self):
        state = create_world("gameplay three role bounded issue")
        needs_issue = {"pilot", "factor", "guard", "healer"}
        for person in state.household:
            state.active_courier_id = person.id
            sync_legacy_load(state)
            load_before = load_state(state)
            issued = ensure_initial_field_tool(state)
            tools = [
                item for item in state.items
                if item.kind == "reed sickle" and item.owner_id == person.id
                and item.location == "pack"
            ]
            self.assertEqual(
                issued, "reed sickle" if person.role in needs_issue else None,
            )
            self.assertEqual(len(tools), int(person.role in needs_issue))
            self.assertEqual(load_state(state), load_before)
            self.assertGreater(terrain_action_power(state, "cut"), 0)
            self.assertIsNone(ensure_initial_field_tool(state))

    def test_invalid_sample_ranges_are_rejected(self):
        with self.assertRaises(ValueError):
            opening_audit(0)
        with self.assertRaises(ValueError):
            opening_audit(1, start=-1)


if __name__ == "__main__":
    unittest.main()
