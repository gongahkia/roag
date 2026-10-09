from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from roag.circuits import cell_key, diagnostic_lines, item_count
from roag.commands import TerrainActionCommand
from roag.engine_components import registered_reaction_rules
from roag.inventory import ensure_initial_field_tool, sync_legacy_load
from roag.navigation import has_navigation_guidance, navigation_targets
from roag.production import (
    CATALOG_VIEWS, RECIPES, catalog_recipe_ids, input_count,
    production_site_lead, site_position,
)
from roag.production_presentation import production_recipe_name
from roag.regions import begin_region
from roag.save import load_game, save_game
from roag.session import GameSession
from roag.state import Position, create_world, validate_state
from roag.terminal import (
    CircuitView, InputEvent, _handle_circuit, _handle_overlay, _overlay_lines,
    _status_lines,
)
from roag.world import position_key


class GameplayCommissioningTests(unittest.TestCase):
    def _catalog_selection(
        self, state, recipe_id: str, view: str = "all",
    ) -> tuple[str, str]:
        recipe_ids = catalog_recipe_ids(state, view)
        index = recipe_ids.index(recipe_id)
        prefix = "craft-catalog" if view == "all" else f"craft-catalog:{view}"
        return f"{prefix}:{index // 8}", "12345678"[index % 8]

    def test_catalog_views_make_the_engine_chain_legible(self):
        state = create_world("gameplay six catalog organization")
        begin_region(state, "hearthford")
        state.position = site_position(state)

        all_ids = catalog_recipe_ids(state)
        engine_ids = catalog_recipe_ids(state, "engine")
        ready_ids = catalog_recipe_ids(state, "ready")
        self.assertLess(len(engine_ids), len(all_ids))
        self.assertTrue({
            "hearthford-ironwork", "hearthford-paper",
            "circuit-rack", "circuit-sensor",
        } <= set(engine_ids))
        self.assertFalse(any(recipe_id.startswith("make:") for recipe_id in engine_ids))
        self.assertTrue(set(ready_ids) <= set(all_ids))
        with self.assertRaises(ValueError):
            catalog_recipe_ids(state, "unknown")

        first_kind, quit_requested = _handle_overlay(
            state, "craft-catalog:0", ord("v"),
        )
        self.assertEqual((first_kind, quit_requested), ("craft-catalog:engine:0", False))
        second_kind, _ = _handle_overlay(state, first_kind, ord("v"))
        self.assertEqual(second_kind, "craft-catalog:ready:0")
        third_kind, _ = _handle_overlay(state, second_kind, ord("v"))
        self.assertEqual(third_kind, "craft-catalog:0")

        title, lines = _overlay_lines(state, first_kind)
        rendered = "\n".join(lines)
        self.assertTrue(title)
        self.assertIn("View: engine chain.", rendered)
        self.assertIn(production_recipe_name("hearthford-ironwork"), rendered)
        _, ready_lines = _overlay_lines(state, second_kind)
        self.assertIn(
            "No plans in this view can use the stations here.",
            "\n".join(ready_lines),
        )
        next_page, _ = _handle_overlay(state, first_kind, ord("n"))
        self.assertEqual(next_page, "craft-catalog:engine:1")
        previous_page, _ = _handle_overlay(state, next_page, ord("p"))
        self.assertEqual(previous_page, first_kind)
        self.assertEqual(CATALOG_VIEWS, ("all", "engine", "ready"))

    def test_unseen_regional_works_have_a_zero_time_field_lead(self):
        state = create_world("gameplay eight opening field lead")
        begin_region(state, "hearthford")
        before = state.to_dict()

        lead = production_site_lead(state)
        self.assertIsNotNone(lead)
        self.assertEqual(lead.landmark_id, "mill")
        self.assertEqual(lead.position, site_position(state))
        self.assertIn(lead.bearing, {"N", "NE", "E", "SE", "S", "SW", "W", "NW"})
        self.assertGreater(lead.distance, 50)
        self.assertEqual(lead.sources, ("iron filings", "spring water"))
        self.assertEqual(lead.stations, ("workshop", "forge"))
        self.assertEqual((lead.stock, lead.timber_carried), (4, 0))
        self.assertTrue(has_navigation_guidance(state))
        self.assertNotIn(
            "landmark:mill", {target.id for target in navigation_targets(state)},
        )

        status = " ".join(_status_lines(state))
        self.assertIn("[T] mill", status)
        self.assertIn(f"{lead.distance} paces {lead.bearing}", status)
        title, lines = _overlay_lines(state, "navigation")
        rendered = " ".join(lines)
        self.assertEqual(title, "FOLLOW A KNOWN LOCAL ROUTE")
        for phrase in (
            "CHARTED REGIONAL WORK",
            "This bearing is not a revealed route",
            "iron filings, spring water",
            "4 draws remain",
            "workshop, forge",
            "Carried timber: 0",
            "standing timber on the way",
            "Once the works is seen",
        ):
            self.assertIn(phrase, rendered)
        self.assertEqual(state.to_dict(), before)

        state.region.seen.append(position_key(lead.position))
        self.assertIsNone(production_site_lead(state))
        self.assertIn(
            "landmark:mill", {target.id for target in navigation_targets(state)},
        )

    def test_fresh_field_resources_commission_a_working_mass_engine(self):
        state = create_world("gameplay five field commissioning")
        pilot = next(person for person in state.household if person.role == "pilot")
        state.active_courier_id = pilot.id
        sync_legacy_load(state)
        self.assertEqual(ensure_initial_field_tool(state), "reed sickle")
        begin_region(state, "hearthford")
        state.weather = "clear"

        # The commissioned engine must not depend on an authored fixture or a
        # pre-existing threat. World advancement may still create danger.
        state.circuits.clear()
        state.threats = []
        state.region_threats["hearthford"] = state.threats
        site = site_position(state)
        self.assertIsNotNone(site)
        state.position = site

        # Exercise ordinary terrain actions and their physical yield rather
        # than granting the recipe's timber directly to the pack.
        session = GameSession(state)
        for dx in (-1, 0, 1):
            target = Position(site.x + dx, site.y + 1, site.z)
            state.region.tile_changes[f"{target.x},{target.y},{target.z}"] = "T"
            first = session.submit(TerrainActionCommand("cut", target))
            second = session.submit(TerrainActionCommand("cut", target))
            self.assertEqual(first.result_id, "terrain.damaged")
            self.assertEqual(second.result_id, "terrain.destroyed")
        self.assertEqual(input_count(state, "commodity:timber"), 3)

        # Use the same terminal overlay controls available to a player for
        # gathering and fabrication. Rendering every required page also guards
        # the catalog path that makes the chain discoverable.
        for key in ("9", "9", "0"):
            _handle_overlay(state, "craft-catalog:0", ord(key))
        self.assertTrue({
            "hearthford-ironwork", "hearthford-paper",
        } <= set(catalog_recipe_ids(state, "ready")))
        for recipe_id in (
            "hearthford-ironwork", "hearthford-paper",
            "circuit-rack", "circuit-sensor",
        ):
            overlay, key = self._catalog_selection(state, recipe_id, "engine")
            title, lines = _overlay_lines(state, overlay)
            self.assertTrue(title)
            self.assertIn(
                production_recipe_name(recipe_id, RECIPES[recipe_id].output),
                "\n".join(lines),
            )
            _handle_overlay(state, overlay, ord(key))

        self.assertEqual(item_count(state, "rack"), 1)
        self.assertEqual(item_count(state, "sensor"), 1)
        self.assertEqual(state.production["sites"]["hearthford"]["stock"], 1)

        # Use actual circuit-terminal build keys. The pair is isolated from the
        # removed fixtures, physically adjacent, and within one cell of the
        # shore site's final acquisition.
        rack_position = Position(site.x + 2, site.y - 1, site.z)
        sensor_position = Position(site.x + 1, site.y - 1, site.z)
        rack_key = cell_key("region:hearthford", rack_position, "surface")
        sensor_key = cell_key("region:hearthford", sensor_position, "surface")
        self.assertFalse(_handle_circuit(
            state, CircuitView(rack_position), InputEvent("key", key=ord("3")),
        ))
        self.assertFalse(_handle_circuit(
            state, CircuitView(sensor_position), InputEvent("key", key=ord("8")),
        ))
        self.assertIn(rack_key, state.circuits)
        self.assertIn(sensor_key, state.circuits)
        self.assertEqual(
            (item_count(state, "rack"), item_count(state, "sensor")), (0, 0),
        )
        sensor_diagnostics = " ".join(diagnostic_lines(
            state, state.circuits[sensor_key],
        ))
        self.assertIn("Engine rack connected", sensor_diagnostics)
        self.assertIn("Mass reaction, range 1", sensor_diagnostics)
        self.assertIn("+1 power and +1 noise", sensor_diagnostics)
        self.assertEqual(
            {rule.trigger_id for rule in registered_reaction_rules(state)},
            {"resource.gained", "terrain.action"},
        )

        # The fourth ordinary site draw is the first post-installation resource
        # fact. Default mass mode converts it into bounded rack charge.
        _handle_overlay(state, "craft-catalog:0", ord("0"))
        self.assertEqual(state.production["sites"]["hearthford"]["stock"], 0)
        self.assertEqual(state.circuits[rack_key].charge, 1)
        self.assertIn(
            "nearby physical acquisition", state.circuits[sensor_key].last_event,
        )
        self.assertEqual(state.world_time, 21)
        self.assertEqual(state.location, "region")
        validate_state(state)

        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(
                state, Path(directory) / "commissioned-engine.json",
            ))
        self.assertEqual(restored.circuits[rack_key].charge, 1)
        self.assertEqual(restored.circuits[sensor_key].mode, "mass")
        validate_state(restored)


if __name__ == "__main__":
    unittest.main()
