from __future__ import annotations

import copy
import unittest
from unittest.mock import patch

from jomon.combat_forecast import _definition_for, danger_cells, forecast_lines, observed_forecasts
from jomon.encounters import threat_from_archetype
from jomon.inspection import contextual_hints, inspect_lines, movement_preview
from jomon.actions import move
from jomon.state import MaterialCell, Position, TerrainStatus, Threat, create_world, game_state_from_dict
from jomon.world import field_of_view


class InspectionAndForecastTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("inspection-and-forecast")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        state = self.state
        state.location, state.position = "region", Position(40, 25, 0)
        state.threats = []
        for y in range(22, 29):
            for x in range(36, 49):
                state.region.tile_changes[f"{x},{y},0"] = "."
        field_of_view(state)

    def test_movement_preview_is_zero_time_and_explains_material_pressure(self):
        state = self.state
        target = Position(41, 25, 0)
        state.region.materials["41,25,0"] = MaterialCell(material="timber", fire=2, smoke=3)
        before = state.to_dict()
        preview = movement_preview(state, target)
        self.assertTrue(preview.legal)
        self.assertIn("fire 2/3", preview.consequence)
        self.assertIn("smoke 3/4", preview.consequence)
        self.assertEqual(state.to_dict(), before)

    def test_blocked_movement_names_wall_water_and_recovery_without_time(self):
        state = self.state
        before = state.world_time
        state.region.tile_changes["41,25,0"] = "#"
        wall = move(state, 1, 0)
        self.assertIn("stone wall", wall.message)
        self.assertIn("another lane", wall.message)
        state.region.tile_changes["41,25,0"] = "~"
        water = move(state, 1, 0)
        self.assertIn("Deep water", water.message)
        self.assertIn("shallows", water.message)
        self.assertEqual(state.world_time, before)

    def test_inspection_distinguishes_visible_remembered_and_unknown(self):
        state = self.state
        visible = Position(41, 25, 0)
        remembered = Position(25, 25, 0)
        unknown = Position(100, 40, 0)
        if "25,25,0" not in state.region.seen:
            state.region.seen.append("25,25,0")
        self.assertTrue(inspect_lines(state, visible)[0].startswith("FACT:"))
        remembered_text = " ".join(inspect_lines(state, remembered))
        self.assertIn("REMEMBERED:", remembered_text)
        self.assertIn("terrain only", remembered_text)
        self.assertTrue(inspect_lines(state, unknown)[0].startswith("UNKNOWN:"))

    def test_observed_forecast_has_origin_target_timing_path_and_counter(self):
        state = self.state
        actor = Threat("forecaster", "visible bow carrier", "ranged", Position(46, 25), 8, 8, status="engaged", aimed_at=state.position)
        state.threats.append(actor)
        rows = observed_forecasts(state)
        self.assertEqual(len(rows), 1)
        forecast = rows[0]
        text = " ".join(forecast_lines(forecast))
        for word in ("origin", "target", "after your next action", "PATH", "COUNTERS"):
            self.assertIn(word, text)
        self.assertIn(state.position, danger_cells(state, field_of_view(state, remember=False)))
        self.assertIn("move/cover/G", " ".join(contextual_hints(state)))

    def test_forecast_never_leaks_hidden_actor(self):
        state = self.state
        actor = Threat("hidden", "hidden claimant", "ranged", Position(46, 25), 8, 8, status="engaged", aimed_at=state.position)
        state.threats.append(actor)
        state.region.tile_changes["43,25,0"] = "#"
        self.assertEqual(observed_forecasts(state), ())
        self.assertNotIn(actor.name, " ".join(inspect_lines(state, state.position)))

    def test_danger_cells_honor_renderers_explicit_visibility_snapshot(self):
        state = self.state
        actor = Threat("snapshot", "snapshot bow", "ranged", Position(46, 25), 8, 8, status="engaged", aimed_at=state.position)
        state.threats.append(actor)
        self.assertEqual(danger_cells(state, {state.position}), set())




    def test_forecast_reuses_callers_visibility_snapshot(self):
        state = self.state
        actor = Threat(
            "visible-snapshot", "snapshot bow", "ranged", Position(46, 25),
            8, 8, status="engaged", aimed_at=state.position,
        )
        state.threats.append(actor)
        visible = field_of_view(state, remember=False)
        with patch(
            "jomon.world.field_of_view",
            side_effect=AssertionError("visibility was recomputed"),
        ):
            rows = observed_forecasts(state, visible)
        self.assertEqual([row.actor_id for row in rows], [actor.id])

    def test_catalog_archetype_selects_forecast_after_display_name_changes(self):
        state = self.state
        actor = threat_from_archetype(
            "coast-elite-wreck", Position(46, 25),
            encounter_id="frontier-elite", group="fixture",
        )
        actor.status, actor.aimed_at = "engaged", state.position
        state.threats = [actor]
        expected = _definition_for(actor.archetype_id)
        actor.name = "alternate displayed reeve"
        forecasts = observed_forecasts(state)
        self.assertEqual(actor.archetype_id, "coast-elite-wreck")
        self.assertEqual(_definition_for(actor.archetype_id), expected)
        self.assertEqual(forecasts[0].counter, expected["counterplay"])

    def test_new_threat_archetype_round_trip_is_exact(self):
        state = self.state
        actor = threat_from_archetype(
            "coast-elite-wreck", Position(46, 25),
            encounter_id="round-trip-identity", group="fixture",
        )
        state.threats = [actor]
        state.region_threats[state.active_region_id] = state.threats
        loaded = game_state_from_dict(state.to_dict())
        restored = next(row for row in loaded.threats if row.id == actor.id)
        self.assertEqual(restored.archetype_id, "coast-elite-wreck")
        self.assertEqual(restored, actor)

    def test_old_known_actor_name_recovers_bundled_archetype_only(self):
        state = self.state
        actor = Threat(
            "legacy-actor", "wreck-chain reeve", "ranged", Position(46, 25),
            8, 8, status="engaged",
        )
        state.threats = [actor]
        state.region_threats[state.active_region_id] = state.threats
        data = state.to_dict()
        raw = next(row for row in data["region_threats"][state.active_region_id] if row["id"] == actor.id)
        raw.pop("archetype_id")
        loaded = game_state_from_dict(data)
        restored = next(row for row in loaded.threats if row.id == actor.id)
        self.assertEqual(restored.archetype_id, "coast-elite-wreck")

    def test_old_unknown_actor_name_stays_unidentified_and_forecast_safe(self):
        state = self.state
        actor = Threat(
            "legacy-unknown", "unrecognised old display wording", "ranged",
            Position(46, 25), 8, 8, status="engaged", aimed_at=state.position,
        )
        state.threats = [actor]
        state.region_threats[state.active_region_id] = state.threats
        data = state.to_dict()
        raw = next(row for row in data["region_threats"][state.active_region_id] if row["id"] == actor.id)
        raw.pop("archetype_id")
        loaded = game_state_from_dict(data)
        restored = next(row for row in loaded.threats if row.id == actor.id)
        self.assertEqual(restored.archetype_id, "")
        self.assertEqual(observed_forecasts(loaded)[0].counter, "move, use cover, guard, or interrupt")

    def test_bespoke_threat_can_explicitly_have_no_catalog_archetype(self):
        actor = Threat(
            "crisis-bespoke", "wreck-chain reeve", "ranged", Position(46, 25),
            8, 8, archetype_id="",
        )
        self.assertEqual(actor.archetype_id, "")


if __name__ == "__main__":
    unittest.main()
