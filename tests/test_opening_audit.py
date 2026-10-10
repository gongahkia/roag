from __future__ import annotations

import unittest
from unittest.mock import patch

from roag.commands import TerrainActionCommand
from roag.inventory import (
    auto_place, create_item, ensure_initial_field_tool, load_state,
    sync_legacy_load, transfer_to_grid,
)
from roag.main import _start_new_world
from roag.opening_audit import opening_audit, opening_profile
from roag.profile import PlayerProfile
from roag.production import gather, input_count, make, site_position
from roag.regions import begin_region
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

    def test_generated_openings_report_field_engine_access_for_every_role(self):
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
            {role: 4 for role in (
                "bargemaster", "pilot", "factor", "carpenter", "guard", "healer",
            )},
        )
        self.assertEqual(result["gaps"], [])
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
                self.assertTrue(
                    {"circuit-rack", "circuit-sensor"}
                    <= set(role["optimistic_circuit_recipes"]),
                )
                missing = dict(role["core_engine_missing_inputs"])
                self.assertEqual(missing["circuit-rack"], ())
                self.assertEqual(missing["circuit-sensor"], ())

    def test_hearthford_sources_and_three_timber_make_core_engine_parts(self):
        state = create_world("gameplay four field production closure")
        pilot = next(person for person in state.household if person.role == "pilot")
        state.active_courier_id = pilot.id
        sync_legacy_load(state)
        self.assertEqual(ensure_initial_field_tool(state), "reed sickle")
        begin_region(state, "hearthford")
        state.position = site_position(state)

        with patch("roag.actions._advance_world"):
            for source_choice in (0, 0, 1):
                changed, message = gather(state, source_choice)
                self.assertTrue(changed, message)

            timber = create_item(
                state, "commodity:timber", "harvested in Hearthford", quantity=3,
            )
            self.assertTrue(auto_place(
                state, timber.id, "pack", owner_id=state.active_courier_id,
            ))

            for recipe_id in (
                "hearthford-ironwork", "hearthford-paper",
                "circuit-rack", "circuit-sensor",
            ):
                changed, message = make(state, recipe_id)
                self.assertTrue(changed, message)

        self.assertEqual(state.location, "region")
        self.assertEqual(state.production["sites"]["hearthford"]["stock"], 1)
        for kind in (
            "ingredient:iron filings", "ingredient:spring water",
            "commodity:timber", "commodity:ironwork", "commodity:paper",
        ):
            self.assertEqual(input_count(state, kind), 0)
        carried = {
            item.kind: item.quantity
            for item in state.items
            if item.location == "pack" and item.owner_id == state.active_courier_id
        }
        self.assertEqual(carried["circuit:rack"], 1)
        self.assertEqual(carried["circuit:sensor"], 1)
        validate_state(state)

    def test_new_run_issues_the_selected_fixed_class_kit(self):
        state = create_world("gameplay three initial field tool")
        pilot = next(person for person in state.household if person.role == "pilot")
        state.active_courier_id = pilot.id
        sync_legacy_load(state)
        self.assertEqual((state.weapon, state.gear), ("staff", "quiet shoes"))
        self.assertEqual(terrain_action_power(state, "cut"), 0)

        screen = object()
        with patch("roag.main._read_run_class", return_value="breaker"), \
                patch("roag.main.play") as play:
            self.assertTrue(_start_new_world(screen, state))

        tools = [
            item for item in state.items
            if item.kind == "reed sickle" and item.owner_id == pilot.id
            and item.location == "pack"
        ]
        self.assertEqual(len(tools), 1)
        self.assertTrue(state.vessel_changes[f"acquired:{tools[0].id}"])
        self.assertEqual((state.weapon, state.gear), ("war hammer", "repair tools"))
        self.assertEqual(state.run.class_id, "breaker")
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

    def test_settled_run_retries_in_a_fresh_world_with_the_last_class(self):
        state = create_world("gameplay retry source")
        retry = create_world("gameplay retry destination")
        profile = PlayerProfile(last_run_class="breaker")
        calls = []

        def resolve_play(_screen, active):
            calls.append(active)
            if len(calls) == 1:
                active.run.status = "defeat"
                active.world_ended = True

        screen = object()
        with patch("roag.main._read_run_class", return_value="sapper"), \
                patch("roag.main._read_run_completion", return_value="retry"), \
                patch("roag.main.create_world", return_value=retry), \
                patch("roag.profile.load_profile", return_value=profile), \
                patch("roag.profile.save_profile"), \
                patch("roag.profile.persist_run_profile"), \
                patch("roag.main.play", side_effect=resolve_play):
            self.assertTrue(_start_new_world(screen, state))

        self.assertEqual(len(calls), 2)
        self.assertIs(calls[0], state)
        self.assertIs(calls[1], retry)
        self.assertEqual(calls[0].run.class_id, "sapper")
        self.assertEqual(calls[1].run.class_id, "sapper")
        self.assertFalse(calls[1].run.item_stacks)

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
