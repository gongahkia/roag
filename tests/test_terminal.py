from __future__ import annotations

import unittest
from unittest.mock import patch

from jomon.actions import interact
from jomon.actions import depart, guard
from jomon.vessel import BARTENDER_POSITION
from jomon.state import MaterialCell, Position, create_world
from jomon.terminal import (
    INVENTORY_HELP_LINES,
    BASE_HELP_LINES,
    ChoiceOption,
    InputEvent,
    OverlayView,
    _handle_overlay_view,
    SEMANTIC_ROLES,
    dialogue_choices,
    dialogue_choice_lines,
    event_feed_lines,
    event_colour_role,
    _handle_overlay,
    information_colour_role,
    semantic_colour_plan,
    semantic_role,
    status_colour_role,
    suspend_terminal,
    play,
    terrain_colour_role,
    visible_threats,
)
from jomon.main import _centered_x, _set_cursor_visibility, landing_notice_layout
from jomon.world import field_of_view, find_tile


class InventoryLayoutTests(unittest.TestCase):
    def test_control_legend_fits_the_eighty_column_layout(self):
        self.assertEqual(len(INVENTORY_HELP_LINES), 2)
        self.assertTrue(all(len(line) <= 78 for line in INVENTORY_HELP_LINES))
        joined = " ".join(INVENTORY_HELP_LINES)
        for command in ("Enter", "R rotate", "Space", "T transfer", "E equip", "O pack", "P pin", "Z auto", "[] body", "D drop", "C confirm", "Esc cancel"):
            self.assertIn(command, joined)

    def test_base_legend_exposes_polish_verbs_at_minimum_width(self):
        self.assertEqual(len(BASE_HELP_LINES), 2)
        self.assertTrue(all(len(line) <= 78 for line in BASE_HELP_LINES))
        joined = " ".join(BASE_HELP_LINES)
        for command in ("; look", "T follow", "M mastery", "A aim", "O actors"):
            self.assertIn(command, joined)


class SemanticColourTests(unittest.TestCase):
    def test_full_semantic_plan_is_complete_and_distinct(self):
        plan = semantic_colour_plan(8, 16)
        self.assertEqual(set(plan), set(SEMANTIC_ROLES))
        self.assertNotEqual(plan["player"].pair, plan["hostile"].pair)
        self.assertNotEqual(plan["hostile"].pair, plan["elite"].pair)
        self.assertNotEqual(plan["water"].pair, plan["hazard"].pair)
        expected = {
            "@": "player",
            "a": "ally",
            "M": "neutral",
            "h": "hostile",
            "X": "elite",
            "~": "water",
            "#": "structure",
            ">": "exit",
            "R": "cargo",
            "C": "interactable",
            "m": "hazard",
            "*": "mystical",
        }
        for glyph, role in expected.items():
            self.assertEqual(semantic_role(glyph), role)
        self.assertEqual(semantic_role("v"), "neutral")
        self.assertEqual(semantic_role("s"), "hazard")
        self.assertEqual(semantic_role("s", aboard=True), "interactable")

    def test_limited_colour_fallback_uses_glyph_and_bold_cues(self):
        plan = semantic_colour_plan(0, 0)
        self.assertTrue(
            all(style.pair == 0 and style.foreground is None for style in plan.values())
        )
        self.assertTrue(plan["player"].bold)
        self.assertTrue(plan["hostile"].bold)

    def test_palette_tiers_never_exceed_reported_colours_or_pairs(self):
        for colours, pairs in ((8, 8), (16, 16), (256, 256), (256, 8)):
            plan = semantic_colour_plan(colours, pairs)
            self.assertEqual(set(plan), set(SEMANTIC_ROLES))
            self.assertTrue(all(0 <= style.pair < pairs for style in plan.values()))
            self.assertTrue(all(
                style.foreground is None or 0 <= style.foreground < colours
                for style in plan.values()
            ))
            self.assertEqual(
                len({style.pair for style in plan.values() if style.pair}),
                len({style.foreground for style in plan.values() if style.foreground is not None}),
            )

    def test_capable_terminals_distinguish_regions_materials_items_and_information(self):
        plan = semantic_colour_plan(256, 256)
        region_colours = {
            plan[f"{region}_ground"].foreground
            for region in (
                "hearthford", "greywash", "greenwold", "whitecairn",
                "dunmire", "rillscar", "marlbank", "frostmere",
            )
        }
        self.assertEqual(len(region_colours), 8)
        self.assertEqual(len({plan[name].foreground for name in ("fire", "smoke", "mud", "ice", "deep_water")}), 5)
        self.assertEqual(len({plan[name].foreground for name in ("weapon", "armour", "tool", "technique", "commodity", "relic")}), 6)
        self.assertNotEqual(plan["fact"].foreground, plan["rumour"].foreground)
        self.assertNotEqual(plan["warning"].foreground, plan["success"].foreground)
        ansi_plan = semantic_colour_plan(16, 256)
        self.assertEqual(len({
            ansi_plan[f"{region}_ground"].foreground
            for region in (
                "hearthford", "greywash", "greenwold", "whitecairn",
                "dunmire", "rillscar", "marlbank", "frostmere",
            )
        }), 8)

    def test_terrain_classifier_uses_region_and_sparse_material_state(self):
        self.assertEqual(terrain_colour_role(".", "dunmire"), "dunmire_ground")
        self.assertEqual(terrain_colour_role(".", "frostmere"), "frostmere_ground")
        self.assertEqual(terrain_colour_role("T", "greenwold"), "vegetation")
        self.assertEqual(terrain_colour_role("=", "rillscar"), "road")
        self.assertEqual(terrain_colour_role("~", "greywash"), "deep_water")
        self.assertEqual(terrain_colour_role(",", "marlbank"), "shallow_water")
        self.assertEqual(terrain_colour_role(".", "marlbank", material=MaterialCell(fire=1)), "fire")
        self.assertEqual(terrain_colour_role(".", "marlbank", material=MaterialCell(smoke=3)), "smoke")
        self.assertEqual(terrain_colour_role(".", "rillscar", material=MaterialCell(collapse_due=9)), "collapse")
        self.assertEqual(terrain_colour_role(".", "frostmere", material=MaterialCell(water=1, ice=True)), "ice")
        self.assertEqual(terrain_colour_role(".", "dunmire", material=MaterialCell(water=3)), "deep_water")
        self.assertEqual(terrain_colour_role("@", "dunmire"), "player")

    def test_every_generated_regional_glyph_has_a_palette_role(self):
        from jomon.frontiers import FRONTIERS, build_frontier

        state = create_world("regional-colour-coverage")
        regions = dict(state.regions)
        regions.update({region_id: build_frontier(state.seed, region_id) for region_id in FRONTIERS})
        self.assertEqual(set(regions), {
            "hearthford", "greywash", "greenwold", "whitecairn",
            "dunmire", "rillscar", "marlbank", "frostmere",
        })
        for region_id, region in regions.items():
            glyphs = set("".join(row for rows in region.levels.values() for row in rows))
            for glyph in glyphs - {" "}:
                self.assertIn(
                    terrain_colour_role(glyph, region_id), SEMANTIC_ROLES,
                    f"{region_id} glyph {glyph!r} lacks a semantic colour role",
                )

    def test_information_and_status_classifiers_retain_textual_semantics(self):
        self.assertEqual(information_colour_role("FACT: presently visible."), "fact")
        self.assertEqual(information_colour_role("RUMOUR: a marked shoal."), "rumour")
        self.assertEqual(information_colour_role("FORECAST: frost by dusk."), "forecast")
        self.assertEqual(information_colour_role("BLOCKED: winter ice."), "warning")
        self.assertEqual(information_colour_role("REACHABLE"), "success")
        self.assertEqual(event_colour_role("Water quenches the burning cargo."), "success")
        self.assertEqual(event_colour_role("The support is damaged and may collapse."), "warning")
        self.assertEqual(status_colour_role("COURIER"), "ui_heading")
        self.assertEqual(status_colour_role("Health 10/10; none"), "success")
        self.assertEqual(status_colour_role("Date winter 8; dawn"), "forecast")
        self.assertEqual(status_colour_role("safe 0; clear"), "success")
        self.assertEqual(status_colour_role("Load 22/30 ready"), "cargo")
        self.assertEqual(status_colour_role("Load 41/30 overloaded"), "warning")
        self.assertEqual(status_colour_role("Combo: smoke hunter"), "technique")

    def test_event_feed_compacts_routine_motion_but_keeps_exact_loss_cause(self):
        messages = [
            "The reed runner moves toward your last-known position.",
            "The bank guard moves toward a clear line.",
            "The named courier is injured by falling debris at 4,5,0.",
        ]
        lines = event_feed_lines(messages, 78, 4)
        text = " ".join(lines)
        self.assertIn("ROUTINE MOVEMENT x2", text)
        self.assertIn(messages[-1], text)

    def test_event_feed_never_groups_death_loss_or_collapse(self):
        messages = [
            "The brace collapses after its warned action.",
            "The sling is destroyed by fire at 8,9,0.",
            "One paper lot is lost to floodwater.",
        ]
        text = " ".join(event_feed_lines(messages, 78, 4))
        self.assertNotIn("ROUTINE", text)
        self.assertIn("destroyed by fire", text)
        self.assertIn("lost to floodwater", text)

    def test_remembered_terrain_does_not_leak_moving_actor(self):
        state = create_world("actor memory")
        state.location, state.current_room = "region", "hearthford"
        state.position = Position(40, 25, 0)
        threat = state.threats[0]
        threat.position = Position(41, 25, 0)
        visible = field_of_view(state)
        self.assertIn(threat.position, visible_threats(state, visible))
        old = threat.position
        threat.position = Position(80, 40, 0)
        state.position = Position(70, 40, 0)
        current = field_of_view(state)
        self.assertNotIn(old, visible_threats(state, current))
        self.assertNotIn(threat.position, visible_threats(state, current))


class LandingLayoutTests(unittest.TestCase):
    def test_unsupported_cursor_visibility_is_a_safe_fallback(self):
        with patch("jomon.main.curses.curs_set", side_effect=__import__("curses").error):
            self.assertFalse(_set_cursor_visibility(0))

    def test_long_incompatible_save_warning_wraps_and_centres(self):
        warning = "Existing development save unavailable: incompatible format with preserved consequences that cannot be loaded safely"
        for width, height in ((80, 24), (100, 32)):
            layout = landing_notice_layout(width, height, warning, "/a/deliberately/long/save/location/jomon-save.json")
            self.assertGreaterEqual(layout.left, 1)
            self.assertLessEqual(layout.left + layout.width, width - 1)
            self.assertGreaterEqual(layout.top, 7)
            self.assertLessEqual(layout.top + layout.height, height - 1)
            self.assertTrue(all(len(line) <= layout.width - 4 for line in layout.lines))
            self.assertEqual(" ".join(layout.lines), warning)
            self.assertTrue(all(len(line) <= layout.width - 8 for line in layout.path_lines))
            self.assertEqual(_centered_x(width, "J O M O N"), (width - len("J O M O N")) // 2)

    def test_landing_layout_reflows_after_resize(self):
        warning = "Existing development save unavailable: a deliberately long deterministic migration explanation remains complete"
        small = landing_notice_layout(80, 24, warning, "/tmp/save.json")
        large = landing_notice_layout(100, 32, warning, "/tmp/save.json")
        self.assertGreaterEqual(len(small.lines), len(large.lines))
        self.assertNotEqual((small.top, small.left), (large.top, large.left))

    def test_posix_suspend_restores_program_mode_without_game_time(self):
        class Screen:
            def __init__(self):
                self.calls = []
            def keypad(self, value):
                self.calls.append(("keypad", value))
            def timeout(self, value):
                self.calls.append(("timeout", value))
            def refresh(self):
                self.calls.append(("refresh",))

        screen = Screen()
        stopped = []
        with patch("jomon.terminal.curses.def_prog_mode"), patch("jomon.terminal.curses.endwin"), patch("jomon.terminal.curses.reset_prog_mode"):
            supported = suspend_terminal(screen, stop=lambda: stopped.append(True))
        if hasattr(__import__("signal"), "SIGTSTP"):
            self.assertTrue(supported)
            self.assertEqual(stopped, [True])
            self.assertIn(("keypad", True), screen.calls)
            self.assertIn(("timeout", -1), screen.calls)

    def test_play_disables_mouse_and_restores_blocking_input_after_failure(self):
        class Screen:
            def __init__(self):
                self.calls = []
            def keypad(self, value):
                self.calls.append(("keypad", value))
            def timeout(self, value):
                self.calls.append(("timeout", value))

        screen = Screen()
        state = create_world("injected-terminal-failure")
        with (
            patch("jomon.terminal.curses.curs_set"),
            patch("jomon.terminal.curses.set_escdelay"),
            patch("jomon.terminal._init_colours"),
            patch("jomon.terminal._enable_mouse"),
            patch("jomon.terminal.curses.mousemask") as mousemask,
            patch("jomon.terminal._play_loop", side_effect=RuntimeError("injected")),
        ):
            with self.assertRaisesRegex(RuntimeError, "injected"):
                play(screen, state)
        mousemask.assert_called_once_with(0)
        self.assertIn(("timeout", -1), screen.calls)


class TavernMenuTests(unittest.TestCase):
    def test_physical_person_selection_is_zero_time(self):
        state = create_world("physical tavern")
        state.jomon_space = "tavern"
        person = state.household[1]
        seat = state.tavern_positions[person.id]
        state.position = Position(seat.x - 1, seat.y)
        started = state.world_time
        result = interact(state)
        self.assertEqual(result.overlay, f"person:{person.id}")
        overlay, _ = _handle_overlay(state, result.overlay, ord("s"))
        self.assertIsNone(overlay)
        self.assertEqual(state.courier, person)
        self.assertNotIn(person.id, state.tavern_positions)
        self.assertEqual(state.position, seat)
        self.assertEqual(state.world_time, started)


class DialogueChoiceTests(unittest.TestCase):
    def test_local_navigation_choice_returns_a_target_without_time(self):
        state = create_world("terminal local route")
        depart(state)
        state.region.seen.extend(
            f"{point.x},{point.y},{point.z}"
            for point in state.region.landmarks.values()
        )
        view = OverlayView("navigation")
        before = state.world_time

        closed, quit_requested = _handle_overlay_view(
            state, view, InputEvent("key", key=10)
        )

        self.assertTrue(closed)
        self.assertFalse(quit_requested)
        self.assertTrue(view.result.startswith("landmark:"))
        self.assertEqual(state.world_time, before)

    def test_guard_without_nearby_hostile_is_an_explicit_listen_action(self):
        state = create_world("hold and listen")
        depart(state)
        for actor in state.threats:
            actor.status = "defeated"
        before = state.world_time

        result = guard(state)

        self.assertTrue(result.time_advanced)
        self.assertEqual(state.world_time, before + 1)
        self.assertIn("listen", result.message)

    def test_unavailable_long_choice_wraps_without_losing_requirement(self):
        option = ChoiceOption(
            "E",
            "Take the ebb salvage while the chain route is exposed",
            "danger",
            False,
            "open the wreck locker or dog the tide chain",
        )

        lines = dialogue_choice_lines(option, True, 72)

        self.assertGreater(len(lines), 1)
        self.assertTrue(all(len(line) <= 72 for line in lines))
        rendered = " ".join(lines)
        self.assertIn("> [E]", rendered)
        self.assertIn("unavailable", rendered)
        self.assertIn("dog the tide chain", rendered)

    def test_mouse_selects_a_wrapped_choice_on_its_continuation_row(self):
        state = create_world("wrapped dialogue input")
        view = OverlayView("objective", option_rows=[(10, 11), (12, 13), (14, 15)])

        closed, _ = _handle_overlay_view(
            state, view, InputEvent("mouse", x=5, y=13, button="left")
        )

        self.assertFalse(closed)
        self.assertEqual(view.selected, 1)

    def test_options_retain_keys_markers_semantics_and_unavailable_reason(self):
        state = create_world("dialogue semantics")
        state.gear = None
        state.support = None
        state.contact.disposition = 0
        options = dialogue_choices(state, "objective")
        self.assertEqual([option.key for option in options], ["A", "R", "T"])
        self.assertEqual([option.semantic for option in options], ["commitment", "refusal", "commitment"])
        self.assertFalse(options[-1].available)
        self.assertTrue(options[-1].requirement)

    def test_arrow_and_mouse_selection_do_not_advance_time(self):
        state = create_world("dialogue input")
        view = OverlayView("objective", option_rows=[10, 11, 12])
        before = state.world_time
        closed, _ = _handle_overlay_view(state, view, InputEvent("key", key=__import__("curses").KEY_DOWN))
        self.assertFalse(closed)
        self.assertEqual(view.selected, 1)
        closed, _ = _handle_overlay_view(state, view, InputEvent("mouse", x=5, y=10, button="left"))
        self.assertFalse(closed)
        self.assertEqual(view.selected, 0)
        self.assertEqual(state.world_time, before)

    def test_bar_selects_support_but_not_courier_or_equipment(self):
        state = create_world("tavern support")
        courier, weapon, gear = state.courier, state.weapon, state.gear
        state.support = None
        state.jomon_space = "tavern"
        state.position = Position(BARTENDER_POSITION.x - 1, BARTENDER_POSITION.y)
        started = state.world_time
        self.assertEqual(interact(state).overlay, "bartender")
        overlay, _ = _handle_overlay(state, "bartender", ord("s"))
        self.assertEqual(overlay, "tavern:support")
        overlay, _ = _handle_overlay(state, overlay, ord("1"))
        self.assertEqual(overlay, "bartender")
        self.assertIsNotNone(state.support)
        self.assertIs(state.courier, courier)
        self.assertEqual(state.weapon, weapon)
        self.assertEqual(state.gear, gear)
        self.assertEqual(state.world_time, started)


if __name__ == "__main__":
    unittest.main()
