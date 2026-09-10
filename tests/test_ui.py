from __future__ import annotations

import curses
from dataclasses import replace
import unittest
import tempfile
from pathlib import Path
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine
from dumbest_dungeon.ui import TerminalUI
from dumbest_dungeon.history import write_run
from dumbest_dungeon.pressure import PressureSource


class FakeScreen:
    def __init__(self, rows: int = 24, columns: int = 80, keys: list[int] | None = None):
        self.rows = [[" "] * columns for _ in range(rows)]
        self.keys = list(keys or [])
        self.writes: list[tuple[int, int, str, int]] = []
        self.refreshes = 0
        self.nonblocking = False
        self.timeouts: list[int] = []

    def getmaxyx(self) -> tuple[int, int]:
        return len(self.rows), len(self.rows[0])

    def addstr(self, row: int, column: int, text: str, _attribute: int = 0) -> None:
        self.writes.append((row, column, text, _attribute))
        for offset, character in enumerate(text):
            if 0 <= row < len(self.rows) and 0 <= column + offset < len(self.rows[0]):
                self.rows[row][column + offset] = character

    def text(self) -> str:
        return "\n".join("".join(row) for row in self.rows)

    def erase(self) -> None:
        for row in self.rows:
            row[:] = [" "] * len(row)

    def refresh(self) -> None:
        self.refreshes += 1

    def getch(self) -> int:
        return self.keys.pop(0) if self.keys else -1

    def nodelay(self, enabled: bool) -> None:
        self.nonblocking = enabled

    def timeout(self, delay: int) -> None:
        self.timeouts.append(delay)


class AsciiUiTests(unittest.TestCase):
    def test_item_and_boon_rewards_can_be_skipped_with_keyboard(self) -> None:
        for kind in ("item", "boon"):
            engine = GameEngine.new(self.catalog, 3)
            self.ui.engine = engine
            pickup = next(item for item in engine.state.pickups if item.kind == kind)
            engine.state.phase, engine.state.current_pickup_id = "discovery", pickup.id
            keys = ([10] if kind == "boon" else []) + [curses.KEY_DOWN] * (3 if kind == "boon" else 1) + [10]
            self.ui.screen = FakeScreen(keys=keys)
            self.ui._discovery()
            self.assertTrue(pickup.resolved)
            self.assertFalse(engine.state.items)
            self.assertFalse(engine.state.boons)
            self.assertTrue(any(row.kind == "pickup_skipped" for row in engine.state.ledger.records))
            self.assertEqual(engine.snapshot(), GameEngine.from_snapshot(self.catalog, engine.snapshot()).snapshot())

    def test_history_search_scroll_and_close_preserve_simulation(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            self.ui.save_path = Path(directory) / "run.json"
            for index in range(40):
                self.engine.record("item_acquired", f"probe:{index}", gained=1, count=1)
            write_run(self.ui.save_path.parent / "history", self.engine, outcome="abandoned")
            before = self.engine.snapshot()
            self.ui.screen = FakeScreen(keys=[10, curses.KEY_END, 10, 27])
            self.ui._history()
            self.assertEqual(before, self.engine.snapshot())
            self.ui.screen = FakeScreen(keys=[ord("w"), ord("a"), 127, ord("d"), 10])
            self.assertEqual("wd", self.ui._text_input("FILTER", "Search"))
            self.assertEqual(before, self.engine.snapshot())

    def test_elapsed_duration_is_interface_metadata_only(self) -> None:
        before = self.engine.snapshot()
        with patch("dumbest_dungeon.ui.time.monotonic", side_effect=[100.0, 145.9]):
            self.ui._start_session(20)
            self.assertEqual(65, self.ui._session_duration())
        self.assertEqual(before, self.engine.snapshot())

    def setUp(self) -> None:
        self.catalog = load_catalog()
        self.engine = GameEngine.new(self.catalog, 3)
        self.ui = TerminalUI.__new__(TerminalUI)
        self.ui.catalog = self.catalog
        self.ui.engine = self.engine
        self.ui.colour = False
        self.ui.message = ""

    def unlock_core(self) -> None:
        for objective in self.engine.state.objectives[: self.engine.state.required_objectives]:
            objective.completed = True
            objective.outcome = "test_route"
        self.engine.state.objectives[0].facts["guardian"] = "defeated"
        self.engine.core_patrol().active = True

    def test_card_preview_is_portrait_playing_card_ascii(self) -> None:
        self.engine.start_combat("vents")
        lines = self.ui._card_lines(self.engine.state.hand[0])
        self.assertEqual(17, len(lines))
        self.assertTrue(all(len(line) == 22 for line in lines))
        self.assertTrue(all(line.isascii() and line.isprintable() for line in lines))
        self.assertTrue(lines[0].startswith("+---"))
        self.assertIn("TARGET:", lines[10])
        self.assertEqual(lines[1][1], lines[-2][-2])

    def test_card_preview_and_deck_browser_expose_authored_build_tags(self) -> None:
        lines = self.ui._card_lines(CardInstance("arc_welder"))
        self.assertIn("USE-MARK", " ".join(lines))
        self.assertIn("DAMAGE", " ".join(lines))
        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._deck_view()
        self.assertIn("TAGS: DAMAGE", screen.text())

    def test_main_menu_uses_public_title(self) -> None:
        screen = FakeScreen(keys=[curses.KEY_DOWN] * 6 + [10])
        self.ui.screen = screen
        self.ui.save_path = Path("/definitely/missing/dullest-save.json")
        self.ui.new_game = lambda: self.engine
        self.ui.run()
        self.assertIn("DULLEST DUNGEON", screen.text())

    def test_main_menu_launches_the_optional_tutorial(self) -> None:
        screen = FakeScreen(keys=[10] + [curses.KEY_DOWN] * 6 + [10])
        self.ui.screen = screen
        self.ui.save_path = Path("/definitely/missing/dullest-save.json")
        self.ui.new_game = lambda: self.engine
        launched = []

        def capture_tutorial() -> None:
            launched.append(self.ui.engine)
            self.ui.engine = None

        self.ui._game_loop = capture_tutorial
        self.ui.run()
        self.assertEqual(1, len(launched))
        self.assertTrue(launched[0].state.tutorial)

    def test_tutorial_ui_reaches_reward_and_completes_after_deck_inspection(self) -> None:
        self.engine = GameEngine.tutorial(self.catalog)
        self.ui.engine = self.engine
        screen = FakeScreen(keys=[10, 9, 10, -1, -1, -1, -1])
        self.ui.screen = screen
        with patch("dumbest_dungeon.ui.curses.napms"):
            self.ui._exploration()
        self.assertEqual("combat", self.engine.state.phase)
        self.assertEqual(2, self.engine.state.tutorial_stage)

        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._tutorial_combat_lesson()
        self.assertEqual(3, self.engine.state.tutorial_stage)
        self.assertIn("PARTY DECK AND RANKS", screen.text())
        warden = next(hero for hero in self.engine.living_heroes() if hero.id == "warden")
        self.engine.play_card(0, warden.id)
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._tutorial_combat_lesson()
        self.assertEqual(5, self.engine.state.tutorial_stage)
        self.assertIn("INTENTS AND SEQUENCING", screen.text())
        field_dressing = next(
            index for index, card in enumerate(self.engine.state.hand)
            if card.card_id == "field_dressing"
        )
        self.engine.play_card(field_dressing, warden.id)
        self.engine.end_turn()
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._tutorial_combat_lesson()
        self.assertEqual(8, self.engine.state.tutorial_stage)
        for enemy in list(self.engine.living_enemies()):
            self.engine._damage(enemy, enemy.max_hp)
        self.engine._combat_victory()
        self.assertEqual("reward", self.engine.state.phase)

        deck_size = len(self.engine.state.deck)
        screen = FakeScreen(keys=[10, 10])
        self.ui.screen = screen
        self.ui._reward()
        self.assertEqual(deck_size + 1, len(self.engine.state.deck))
        self.assertEqual(9, self.engine.state.tutorial_stage)
        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._deck_view()
        self.assertEqual("tutorial_complete", self.engine.state.phase)
        self.assertEqual(10, self.engine.state.tutorial_stage)

    def test_tutorial_destination_cycle_prioritises_the_training_contact(self) -> None:
        self.ui.engine = GameEngine.tutorial(self.catalog)
        self.assertEqual([self.ui.engine.tutorial_destination()], self.ui._exploration_targets())

    def test_help_scrolls_to_its_final_controls_at_minimum_size(self) -> None:
        screen = FakeScreen(keys=[curses.KEY_END, 10])
        self.ui.screen = screen
        self.ui._help()
        self.assertTrue(any("Explore the current world" in text for _, _, text, _ in screen.writes))
        rendered = screen.text()
        self.assertIn("Controls:", rendered)
        self.assertIn("input otherwise stops at exploration routing.", rendered)
        self.assertIn("Enter/Esc close", rendered)

    def test_reward_screen_describes_tradeoffs_without_recommendations(self) -> None:
        self.engine = GameEngine.new(self.catalog, 731)
        self.ui.engine = self.engine
        self.engine.state.rewards = self.engine._generate_card_rewards(4)
        self.engine.state.phase = "reward"
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._reward()
        rendered = screen.text()
        self.assertIn("SYNERGY", rendered)
        self.assertIn("Uses deck MARKED setup.", rendered)
        self.assertNotIn("CORRECTIVE", rendered)
        self.assertNotIn("WILDCARD", rendered)

    def test_curse_card_has_distinct_unplayable_ascii(self) -> None:
        hero = self.engine.living_heroes()[0]
        self.engine.acquire_curse(hero.id, "static_prayer")
        self.engine.start_combat("vents")
        card = CardInstance("static_prayer", bound_hero_id=hero.id)
        lines = self.ui._card_lines(card)
        self.assertEqual("X", lines[1][1])
        self.assertIn("CURSE", lines[3])
        self.assertIn("unplayable", lines[10])
        self.assertIn("XX", "".join(lines))

    def test_mastered_card_discloses_branch_and_effective_ranks(self) -> None:
        card = CardInstance("crossguard", upgraded=True, mastery="coverage")
        lines = self.ui._card_lines(card)
        self.assertTrue(any("CROSSGUARD+/C" in line for line in lines))
        self.assertIn("FROM 1,2,3", lines[9])
        label = self.ui._card_label(card)
        self.assertIn("+/C", label)
        self.assertIn("Mastery:", label)
        self.assertIn("MASTERY COVERAGE", self.ui._card_tags_note(card))

    def test_infused_card_discloses_marker_rule_and_limit(self) -> None:
        card = CardInstance("brace", infusion_id="base:memory_lacquer")
        lines = self.ui._card_lines(card)
        self.assertIn("INF RETAIN", "".join(lines))
        self.assertIn("[RETAIN]", self.ui._card_label(card))
        note = self.ui._card_tags_note(card)
        self.assertIn("INFUSION RETAIN", note)
        self.assertIn("LIMIT NONE", note)

    def test_combat_hand_is_a_row_of_five_miniature_cards(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._render_combat(0)
        self.assertEqual(5, "".join(screen.rows[13]).count("+------------+"))
        first_card_writes = [write for write in screen.writes if write[0] in range(13, 23) and write[1] == 2]
        self.assertTrue(all(attribute & curses.A_REVERSE for _, _, _, attribute in first_card_writes))

    def test_combat_card_and_crew_inspection_are_keyboard_accessible(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._combat_hand_view()
        self.assertIn("HAND INSPECTION", screen.text())
        self.assertIn("TARGET:", screen.text())

        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._crew_view()
        rendered = screen.text()
        self.assertIn("CREW INSPECTION", rendered)
        self.assertIn("Mara Venn", rendered)
        self.assertIn("Preferred", rendered)
        self.assertIn("R1,2; statuses", rendered)

    def test_minimum_size_battlefield_contains_both_formations(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen()
        self.ui.screen = screen
        card = self.engine.state.hand[0]
        definition = self.catalog.cards[card.card_id]
        self.ui._battlefield(2, definition["hero"], self.engine.valid_targets(0))
        rendered = screen.text()
        self.assertIn("CREW", rendered)
        self.assertIn("HOST", rendered)
        self.assertIn(self.catalog.art["heroes"]["warden"][1].strip(), rendered)
        self.assertIn(self.catalog.art["enemies"]["vent_crawler"][2].strip(), rendered)
        preview = FakeScreen()
        self.ui.screen = preview
        self.ui._draw_card(3, 45, card)
        self.assertIn("TARGET:", preview.text())

    def test_intents_expose_enemy_setup_and_exploit_combos(self) -> None:
        self.engine.start_combat(
            "lost_shift",
            enemy_ids=["rad_acolyte", "control_rod"],
        )
        acolyte, control_rod = self.engine.living_enemies()
        target = self.engine.living_heroes()[0]
        self.engine.state.intents = [
            {
                "enemy_rank": 1,
                "enemy_id": acolyte.id,
                "action": "Gamma Brand",
                "target_rule": "front",
                "target_ids": [target.id],
                "target_labels": ["R1 WARD"],
            },
            {
                "enemy_rank": 2,
                "enemy_id": control_rod.id,
                "action": "Containment Blow",
                "target_rule": "front",
                "target_ids": [target.id],
                "target_labels": ["R1 WARD"],
            },
        ]
        screen = FakeScreen(rows=10)
        self.ui.screen = screen
        self.ui._intents(0, 4)
        rendered = screen.text()
        self.assertIn("SET:MK", rendered)
        self.assertIn("CASH:MK", rendered)
        self.assertIn("D8/12:MK", rendered)
        self.assertIn(">1WARD", rendered)

    def test_intents_include_visible_enemy_damage_modifiers(self) -> None:
        self.engine = GameEngine.tutorial(self.catalog)
        self.ui.engine = self.engine
        self.engine.advance_tutorial(0, 1)
        for step in self.engine.path_to(*self.engine.tutorial_destination()):
            self.engine.step_exploration(*step)
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._render_combat(0)
        rendered = screen.text()
        self.assertIn("Gamma Brand D8", rendered)
        self.assertIn("D10/15:MK", rendered)

    def test_four_enemy_intents_fit_two_rows_with_targets_and_effects(self) -> None:
        self.engine.start_combat("foundry_stoked_line")
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._intents(0, 2)
        writes = [write for write in screen.writes if write[0] in {0, 1}]
        self.assertEqual(4, len(writes))
        self.assertTrue(all(len(text) <= 37 for _, _, text, _ in writes))
        rendered = screen.text()
        for intent in self.engine.state.intents:
            enemy = next(actor for actor in self.engine.living_enemies() if actor.id == intent["enemy_id"])
            actor_mark = "".join(word[0] for word in enemy.name.split()).upper()[:4]
            self.assertIn(f"R{intent['enemy_rank']}{actor_mark}>", rendered)
        self.assertRegex(rendered, r"[DBHSG][0-9]")

    def test_enemy_action_playback_exposes_actor_target_effects_and_speed(self) -> None:
        self.engine.start_combat("lost_shift")
        screen = FakeScreen(keys=[ord("f")])
        self.ui.screen = screen
        self.ui._play_enemy_action(
            {
                "actor_id": "test-enemy",
                "actor_name": "Hull Prophet",
                "actor_rank": 2,
                "action": "Open the Seam",
                "target_labels": ["R1 WARD"],
                "setup": ["marked"],
                "payoff": [],
                "changes": ["Mara Venn -8 HP", "Mara Venn +MARKED 2"],
            }
        )
        rendered = screen.text()
        self.assertIn("ENEMY ACTION Hull Prophet [R2]", rendered)
        self.assertIn("Open the Seam -> R1 WARD", rendered)
        self.assertIn("SET MARKED", rendered)
        self.assertIn("Mara Venn -8 HP", rendered)
        self.assertEqual([self.ui.ENEMY_ACTION_MS, -1], screen.timeouts)
        self.assertTrue(self.ui._enemy_playback_fast)

    def test_enemy_action_playback_can_skip_the_remaining_phase(self) -> None:
        self.engine.start_combat("lost_shift")
        screen = FakeScreen(keys=[ord(" ")])
        self.ui.screen = screen
        event = {
            "actor_id": "test-enemy",
            "actor_name": "Hull Prophet",
            "actor_rank": 2,
            "action": "Open the Seam",
            "target_labels": ["R1 WARD"],
            "setup": [],
            "payoff": ["marked"],
            "changes": ["Mara Venn -8 HP"],
        }
        self.ui._play_enemy_action(event)
        refreshes = screen.refreshes
        self.ui._play_enemy_action(event)
        self.assertTrue(self.ui._skip_enemy_playback)
        self.assertEqual(refreshes, screen.refreshes)

    def test_target_cursor_moves_between_battlefield_sprites(self) -> None:
        self.engine.start_combat("vents")
        self.engine.state.hand = [CardInstance("snap_shot")]
        targets = self.engine.valid_targets(0)
        screen = FakeScreen(keys=[curses.KEY_RIGHT, 10])
        self.ui.screen = screen
        selected = self.ui._target_selector(0, targets)
        self.assertEqual(targets[1], selected)
        actor = next(enemy for enemy in self.engine.living_enemies() if enemy.id == selected)
        column = 43 + (actor.rank - 1) * 9
        self.assertEqual(">", screen.rows[5][column - 1])
        self.assertEqual("<", screen.rows[5][column + 7])

    def test_top_down_map_draws_walls_route_party_target_and_patrol(self) -> None:
        screen = FakeScreen()
        self.ui.screen = screen
        party = (self.engine.state.party_x, self.engine.state.party_y)
        path = self.engine._find_path(party, self.engine.room_position(1))
        self.assertGreaterEqual(len(path), 4)
        patrol = self.engine.state.patrols[0]
        patrol.x, patrol.y = path[2]
        pickup = next(item for item in self.engine.state.pickups if not item.hidden)
        pickup.x, pickup.y = path[1]
        pickup.kind = "boon"
        self.engine._update_perception()
        destination = path[3]
        self.ui._world_map(5, destination)
        rendered = screen.text()
        self.assertIn("#", rendered)
        self.assertIn(".", rendered)
        self.assertIn(":", rendered)
        self.assertIn("@", rendered)
        self.assertIn("X", rendered)
        self.assertIn("e", rendered)
        self.assertIn("+", rendered)

    def test_exploration_hud_prioritises_complete_route_and_biome_state(self) -> None:
        screen = FakeScreen()
        self.ui.screen = screen
        party = (self.engine.state.party_x, self.engine.state.party_y)
        self.ui._render_exploration(party)
        rendered = screen.text()
        biome = self.catalog.biomes[self.engine.current_biome()]["name"]
        self.assertIn("Route  0/18 READY", rendered)
        self.assertIn("L- 0", rendered)
        self.assertIn(self.engine.terrain_at(*party)["name"], rendered)
        self.assertIn("Core SEALED", rendered)
        self.assertIn(biome, rendered)

        wall = next(
            (x, y)
            for y, row in enumerate(self.engine.world_tiles())
            for x, tile in enumerate(row)
            if tile == "#"
        )
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._render_exploration(wall)
        self.assertIn("Route --/18 OUT OF REACH", screen.text())

    def test_aiming_at_perceived_patrol_names_its_doctrine_and_alert(self) -> None:
        patrol = self.engine.state.patrols[0]
        patrol.x, patrol.y = self.engine.state.party_x + 1, self.engine.state.party_y
        patrol.alert = 5
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._render_exploration((patrol.x, patrol.y), focus=(patrol.x, patrol.y))
        rendered = screen.text()
        self.assertIn(f"DOCTRINE {patrol.doctrine.upper()}", rendered)
        self.assertIn("ALERT 5", rendered)

    def test_terminal_size_does_not_change_simulation_knowledge(self) -> None:
        before = list(self.engine.state.known_feature_ids)
        party = (self.engine.state.party_x, self.engine.state.party_y)
        self.ui.screen = FakeScreen()
        self.ui._world_map(self.ui.MAP_ROW, party)
        minimum = list(self.engine.state.known_feature_ids)
        self.ui.screen = FakeScreen(rows=60, columns=140)
        self.ui._world_map(self.ui.MAP_ROW, party)
        self.assertEqual(before, minimum)
        self.assertEqual(minimum, self.engine.state.known_feature_ids)

    def test_destination_cycle_excludes_points_beyond_command_reach(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        distant = max(
            ((objective.x, objective.y) for objective in self.engine.state.objectives),
            key=lambda tile: self.engine.path_cost(self.engine._find_path(party, tile)),
        )
        fixture = replace(self.catalog, balance={**self.catalog.balance, "maximum_navigation_distance": 1})
        with patch.object(self.engine, "catalog", fixture):
            targets = self.ui._exploration_targets()
            self.assertNotIn(distant, targets)
            self.assertTrue(
                all(
                    self.engine.path_cost(self.engine._find_path(party, tile)) <= 1
                    for tile in targets
                )
            )

    def test_open_core_is_a_global_marker_and_first_reachable_cycle_target(self) -> None:
        self.unlock_core()
        core_x, core_y = self.engine.core_position()
        screen = FakeScreen(rows=60, columns=140)
        self.ui.screen = screen
        left, top, width, _ = self.ui._world_map(
            self.ui.MAP_ROW,
            (self.engine.state.party_x, self.engine.state.party_y),
            focus=(core_x, core_y),
        )
        map_column = max(2, (screen.getmaxyx()[1] - width) // 2)
        self.assertEqual("B", screen.rows[self.ui.MAP_ROW + core_y - top][map_column + core_x - left])

        waypoint = self.engine.core_waypoint()
        targets = self.ui._exploration_targets()
        self.assertEqual(waypoint, targets[0])
        party = (self.engine.state.party_x, self.engine.state.party_y)
        self.assertTrue(
            all(
                self.engine.path_cost(self.engine._find_path(party, tile))
                <= self.engine.maximum_navigation_distance()
                for tile in targets
            )
        )

    def test_core_key_selects_only_a_reachable_route_leg(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        self.assertEqual(party, self.ui._select_core_waypoint())
        self.assertIn("Core sealed", self.ui.message)

        self.unlock_core()
        waypoint = self.ui._select_core_waypoint()
        route = self.engine._find_path(party, waypoint)
        self.assertLessEqual(
            self.engine.path_cost(route),
            self.engine.maximum_navigation_distance(),
        )
        self.assertIn("light total", self.ui.message)
        self.assertIn("pressure total", self.ui.message)
        self.assertIn("press Enter", self.ui.message)

    def test_active_objective_is_cycled_as_a_reachable_route_leg(self) -> None:
        objective = self.engine.state.objectives[0]
        approach = self.engine.mission_definition(objective.biome_id)["approaches"][0]
        objective.approach = approach["id"]
        waypoint = self.engine.objective_waypoint(objective)
        party = (self.engine.state.party_x, self.engine.state.party_y)
        self.assertNotEqual(party, waypoint)
        self.assertEqual(waypoint, self.ui._exploration_targets()[0])
        self.assertLessEqual(
            self.engine.path_cost(self.engine._find_path(party, waypoint)),
            self.engine.maximum_navigation_distance(),
        )

        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._render_exploration(waypoint, focus=waypoint)
        rendered = screen.text()
        self.assertIn("NEXT LEG", rendered)
        self.assertIn("TOTAL", rendered)
        self.assertIn(self.catalog.biomes[objective.biome_id]["name"].upper(), rendered)

    def test_destination_cycle_restarts_after_each_route_leg(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        first_leg = (party[0] + 1, party[1])
        next_priority = (party[0] + 2, party[1])
        lower_priority = (party[0] + 3, party[1])
        rendered: list[tuple[int, int]] = []
        travelled: list[tuple[int, int]] = []

        def render(cursor, focus=None):
            rendered.append(cursor)
            return (0, 0, 40, 16)

        def targets():
            if travelled:
                return [next_priority, lower_priority]
            return [first_leg, lower_priority]

        def walk(destination):
            travelled.append(destination)
            self.engine.state.party_x, self.engine.state.party_y = destination

        self.ui.screen = FakeScreen(keys=[9, 10, 9, 27])
        with (
            patch.object(self.ui, "_render_exploration", side_effect=render),
            patch.object(self.ui, "_exploration_targets", side_effect=targets),
            patch.object(self.ui, "_walk_to", side_effect=walk),
            patch.object(self.ui, "_pause"),
        ):
            self.ui._exploration()

        self.assertEqual([first_leg], travelled)
        self.assertEqual(next_priority, rendered[-1])

    def test_mission_status_distinguishes_required_and_optional_objectives(self) -> None:
        sealed = self.ui._mission_status_text()
        self.assertIn("CORE SEALED", sealed)
        self.assertIn("secure 2 more access objectives", sealed)
        self.assertEqual(4, sealed.count("[AVAILABLE]"))

        self.unlock_core()
        opened = self.ui._mission_status_text()
        self.assertIn("CORE OPEN", opened)
        self.assertIn("Press G", opened)
        self.assertEqual(2, opened.count("[OPTIONAL]"))
        self.assertIn("Possible completion benefits", opened)
        self.assertIn("weighted ticks", opened)

    def test_mission_status_is_scrollable_at_minimum_size(self) -> None:
        self.unlock_core()
        screen = FakeScreen(rows=24, columns=80, keys=[curses.KEY_END, 27])
        self.ui.screen = screen
        self.ui._mission_view()
        rendered = screen.text()
        self.assertIn("EXPEDITION STATUS", rendered)
        self.assertIn("OPTIONAL", rendered)
        self.assertIn("Possible completion benefits", rendered)

    def test_core_access_notice_gives_location_route_and_confirmation_controls(self) -> None:
        self.unlock_core()
        body = self.ui._core_access_notice_text()
        core_x, core_y = self.engine.core_position()
        self.assertIn("final path is now open", body)
        self.assertIn(f"{core_x:03},{core_y:02}", body)
        self.assertIn("weighted ticks", body)
        self.assertIn("Press G", body)
        self.assertIn("OPTIONAL", body)
        self.assertIn("until Enter confirms", body)

    def test_completing_second_objective_opens_a_blocking_core_notice(self) -> None:
        first, objective = self.engine.state.objectives[:2]
        first.completed = True
        first.outcome = "test_route"
        first.facts["guardian"] = "defeated"
        approach = self.engine.mission_definition(objective.biome_id)["approaches"][0]
        objective.approach = approach["id"]
        objective.stage = len(approach["stages"]) - 1
        self.engine.state.party_x, self.engine.state.party_y = self.engine.objective_position(objective)
        self.engine.state.current_objective_id = objective.id
        self.engine.state.phase = "objective"
        self.ui.screen = FakeScreen(keys=[10])
        with patch.object(self.ui, "_notice") as notice:
            self.ui._objective()
        self.assertTrue(self.engine.boss_unlocked())
        notice.assert_called_once()
        self.assertEqual("CORE ACCESS OPEN", notice.call_args.args[0])
        self.assertIn("Press G", notice.call_args.args[1])

    def test_unfinished_objective_menu_is_explicitly_optional_after_access(self) -> None:
        self.unlock_core()
        objective = self.engine.state.objectives[2]
        self.engine.state.current_objective_id = objective.id
        self.engine.state.phase = "objective"
        with patch.object(self.ui, "_menu", return_value=None) as menu:
            self.ui._objective()
        body = menu.call_args.args[2]
        self.assertIn("OPTIONAL DETOUR", body)
        self.assertIn("BENEFIT", body)
        self.assertIn("not required", body)

    def test_world_map_viewport_is_bounded_by_world_on_large_terminal(self) -> None:
        screen = FakeScreen(rows=60, columns=140)
        self.ui.screen = screen
        tiles = self.engine.world_tiles()
        party = (self.engine.state.party_x, self.engine.state.party_y)
        origin = self.ui._world_map(self.ui.MAP_ROW, party)
        self.assertEqual((0, 0, len(tiles[0]), len(tiles)), origin)
        map_column = (140 - len(tiles[0])) // 2
        self.assertTrue(
            any(row == self.ui.MAP_ROW and column == map_column for row, column, _, _ in screen.writes)
        )

        with patch(
            "dumbest_dungeon.ui.curses.getmouse",
            return_value=(0, map_column + len(tiles[0]), self.ui.MAP_ROW, 0, curses.BUTTON1_CLICKED),
        ):
            self.assertIsNone(self.ui._mouse_destination(origin))

    def test_map_and_biome_view_explain_objective_hazard_and_core_lock(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        objective = self.engine.state.objectives[0]
        hazard = self.engine.state.hazards[0]
        facility = self.engine.state.facilities[0]
        objective.x, objective.y = party[0] + 2, party[1]
        hazard.x, hazard.y = party[0] + 1, party[1]
        hazard.cells[0] = [hazard.x, hazard.y]
        facility.x, facility.y = party[0] + 2, party[1] + 1
        self.engine.state.known_feature_ids.append(facility.id)
        self.engine.state.room_positions[11] = [party[0] + 3, party[1]]
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._world_map(5, (objective.x, objective.y))
        rendered = screen.text()
        self.assertIn("K", rendered)
        template = self.catalog.landmarks[
            next(item for item in self.engine.state.landmarks if item.biome_id == objective.biome_id).template_id
        ]
        self.assertTrue(any(symbol in rendered for symbol in "".join(template["art"]) if symbol not in " K"))
        self.assertIn("^", rendered)
        self.assertIn("F", rendered)
        self.assertIn("L", rendered)

        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._biome_view()
        rendered = screen.text()
        self.assertIn("TRAVEL", rendered)
        self.assertIn("PATROLS", rendered)
        self.assertIn("COMBAT", rendered)
        self.assertIn("FACILITY", rendered)
        self.assertIn("OBJECTIVE", rendered)

    def test_exploration_reports_persistent_biome_state_at_minimum_size(self) -> None:
        self.ui.screen = FakeScreen(rows=24, columns=80)
        position = (self.engine.state.party_x, self.engine.state.party_y)
        self.ui._render_exploration(position)
        rendered = self.ui.screen.text()
        self.assertIn("OBJECTIVE", rendered)
        self.assertIn("HAZARD TILES", rendered)
        self.assertIn("FACILITY", rendered)

        self.ui.screen = FakeScreen(rows=24, columns=80, keys=[curses.KEY_END, 27])
        self.ui._biome_view()
        rendered = self.ui.screen.text()
        self.assertIn("CURRENT STATE", rendered)
        self.assertIn("unresolved tiles", rendered)

    def test_facility_menu_discloses_cost_risk_and_one_use(self) -> None:
        facility = self.engine.state.facilities[0]
        self.engine.state.party_x, self.engine.state.party_y = facility.x, facility.y
        self.engine._resolve_exploration_tile()
        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._facility()
        self.assertEqual("exploration", self.engine.state.phase)
        rendered = screen.text()
        self.assertIn("COST", rendered)
        self.assertIn("RISK", rendered)
        self.assertIn("ONE USE", rendered)

    def test_facility_recycler_and_targeted_salvage_are_keyboard_operable(self) -> None:
        self.engine.acquire_item("bulkhead_laminate", 2)
        facility = self.engine.state.facilities[0]
        self.engine.state.phase = "facility"
        self.engine.state.current_facility_id = facility.id
        self.ui.screen = FakeScreen(keys=[curses.KEY_DOWN, curses.KEY_DOWN, 10, 10])
        self.ui._facility()
        self.assertEqual(1, self.engine.state.recycler_credits)
        self.assertNotIn("bulkhead_laminate", self.engine.state.items)
        self.assertEqual("recycle", facility.outcome)

        pickup = next(item for item in self.engine.state.pickups if item.kind == "item")
        self.engine.state.phase = "discovery"
        self.engine.state.current_pickup_id = pickup.id
        self.ui.screen = FakeScreen(keys=[curses.KEY_DOWN, 10])
        self.ui._discovery()
        self.assertEqual(0, self.engine.state.recycler_credits)
        self.assertEqual("exploration", self.engine.state.phase)
        self.assertIn("TARGETED SALVAGE", self.ui.screen.text())

    def test_event_menu_discloses_consequences_before_confirmation(self) -> None:
        room = self.engine.room()
        room.kind = "event"
        room.content_id = "sealed_locker"
        room.biome_id = "derelict"
        room.resolved = False
        self.engine.state.current_event = "sealed_locker"
        self.engine.state.phase = "event"
        self.ui.screen = FakeScreen(keys=[10])
        self.ui._event()
        rendered = self.ui.screen.text()
        self.assertIn("RISK GUARDED", rendered)
        self.assertIn("IRREVERSIBLE", rendered)
        self.assertIn("card reward", rendered)

    def test_hazard_notice_and_objective_menu_return_to_exploration(self) -> None:
        hazard = self.engine.state.hazards[0]
        self.engine.state.party_x, self.engine.state.party_y = hazard.x, hazard.y
        self.engine._resolve_exploration_tile()
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._hazard()
        self.assertEqual("exploration", self.engine.state.phase)
        self.assertIn("route has stopped", screen.text())

        objective = self.engine.state.objectives[0]
        self.engine.state.party_x, self.engine.state.party_y = objective.x, objective.y
        self.engine._resolve_exploration_tile()
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._objective()
        self.assertEqual("exploration", self.engine.state.phase)
        self.assertIsNotNone(objective.approach)
        self.assertFalse(objective.completed)
        self.assertIn("RISK", screen.text())
        self.assertIn("IRREVERSIBLE", screen.text())
        while not objective.completed:
            self.engine.state.party_x, self.engine.state.party_y = self.engine.objective_position(objective)
            self.engine._resolve_exploration_tile()
            screen = FakeScreen(keys=[10])
            self.ui.screen = screen
            self.ui._objective()
        self.assertTrue(objective.completed)

    def test_objective_choice_discloses_generated_route_burden(self) -> None:
        objective = self.engine.state.objectives[0]
        self.engine.state.party_x, self.engine.state.party_y = objective.x, objective.y
        self.engine.state.current_objective_id = objective.id
        self.engine.state.phase = "objective"
        self.ui.screen = FakeScreen(keys=[27])
        self.ui._objective()
        rendered = self.ui.screen.text()
        self.assertIn("ROUTE", rendered)
        self.assertIn("TICKS", rendered)
        self.assertIn("KNOWN HAZARDS", rendered)
        self.assertIn("PATROLS", rendered)

    def test_bargain_consequences_are_complete_at_minimum_size(self) -> None:
        pickup = next(item for item in self.engine.state.pickups if item.kind == "bargain")
        self.engine.state.phase = "discovery"
        self.engine.state.current_pickup_id = pickup.id
        hero = self.engine.living_heroes()[0]
        options = self.engine.bargain_options(hero.id)
        screen = FakeScreen(keys=[10, curses.KEY_DOWN, curses.KEY_DOWN, 10])
        self.ui.screen = screen
        self.ui._discovery()
        rendered = screen.text()
        for option in options:
            curse = self.catalog.curses[str(option["curse_id"])]
            self.assertIn(curse["description"].split()[-1], rendered)
        self.assertEqual("exploration", self.engine.state.phase)

    def test_boon_choices_are_complete_at_minimum_size(self) -> None:
        pickup = next(item for item in self.engine.state.pickups if item.kind == "boon")
        self.engine.state.phase = "discovery"
        self.engine.state.current_pickup_id = pickup.id
        hero = self.engine.living_heroes()[0]
        options = self.engine.boon_pickup_options(hero.id)
        screen = FakeScreen(keys=[10, 10])
        self.ui.screen = screen
        self.ui._discovery()
        rendered = screen.text()
        for boon_id in options:
            description = self.catalog.boons[boon_id]["description"]
            self.assertIn(description.split()[-1], rendered)
        self.assertEqual("exploration", self.engine.state.phase)

    def test_workshop_can_transform_a_card_through_ascii_menus(self) -> None:
        room = self.engine.room(1)
        room.kind = "upgrade"
        room.resolved = False
        self.engine.state.current_room = room.id
        self.engine.state.phase = "service"
        self.engine.state.service_type = "upgrade"
        original = self.engine.state.deck[0].card_id
        screen = FakeScreen(keys=[curses.KEY_DOWN, 10, 10, 10])
        self.ui.screen = screen
        self.ui._service()
        self.assertEqual("exploration", self.engine.state.phase)
        self.assertNotEqual(original, self.engine.state.deck[0].card_id)
        rendered = screen.text()
        self.assertIn("CHOOSE A NEW TECHNIQUE", rendered)
        self.assertIn("SOURCE:", rendered)
        self.assertIn("EFFECTS", rendered)
        self.assertIn("TAGS +", rendered)

    def test_upgrade_picker_previews_the_proposed_rules(self) -> None:
        room = self.engine.room(1)
        room.kind = "camp"
        room.resolved = False
        self.engine.state.current_room = room.id
        self.engine.state.phase = "service"
        self.engine.state.service_type = "camp"
        screen = FakeScreen(keys=[curses.KEY_DOWN, 10] + [curses.KEY_DOWN] * 3 + [10])
        self.ui.screen = screen
        self.ui._service()
        self.assertTrue(self.engine.state.deck[3].upgraded)
        rendered = screen.text()
        self.assertIn("PROPOSED UPGRADE", rendered)
        self.assertIn("INTERPOSE+", rendered)
        self.assertIn("BOTH YOU", rendered.upper())

    def test_preview_notes_scroll_without_changing_the_choice(self) -> None:
        screen = FakeScreen(keys=[curses.KEY_NPAGE, 27])
        self.ui.screen = screen
        selected = self.ui._menu(
            "DETAILED COMPARISON",
            ["Keep this choice selected"],
            allow_cancel=True,
            preview_cards=[CardInstance("brace")],
            preview_notes=["FIRST\nSECOND\nTHIRD\nFINAL DETAIL"],
        )
        self.assertIsNone(selected)
        self.assertIn("FINAL DETAIL", screen.text())
        self.assertIn("PgUp/PgDn details", screen.text())

    def test_long_menu_keeps_choices_visible_and_scrolls_all_consequences(self) -> None:
        body = "\n".join(f"Consequence {i}" for i in range(40))
        screen = FakeScreen(keys=[curses.KEY_END, ord("j"), 10])
        self.ui.screen = screen
        before = self.engine.snapshot()
        choice = self.ui._menu("LONG CONTRACT", ["Accept", "Walk away"], body)
        self.assertEqual(1, choice)
        self.assertIn("Consequence 39", screen.text())
        self.assertIn("> Walk away", "\n".join("".join(row) for row in screen.rows))
        self.assertIn("PgUp/PgDn", screen.text())
        self.assertEqual(before, self.engine.snapshot())

    def test_effect_browser_groups_hero_and_party_stacks(self) -> None:
        hero = self.engine.living_heroes()[0]
        self.engine.acquire_boon(hero.id, "iron_benediction")
        self.engine.acquire_curse(hero.id, "glass_bones")
        self.engine.acquire_item("survey_relay", 2)
        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._effects_view()
        rendered = screen.text()
        self.assertIn("RUN EFFECTS", rendered)
        self.assertIn("Iron Benediction x1", rendered)
        self.assertIn("Current:", rendered)

    def test_pressure_browser_discloses_band_forecast_causes_and_history_gap(self) -> None:
        for index in range(9):
            self.engine._advance_pressure(PressureSource.ENEMY_ROUND, 1, f"combat round {index + 1}")
        self.engine.state.pressure_incomplete_before_tick = 0
        before = self.engine.snapshot()
        body = self.ui._pressure_text()
        self.assertIn("CURRENT QUIET", body)
        self.assertIn("NEXT 240", body)
        self.assertIn("DIRECTOR", body)
        self.assertIn("reward choices", body)
        self.assertIn("RECENT CAUSES", body)
        self.assertIn("HISTORY GAP", body)
        screen = FakeScreen(rows=24, columns=80, keys=[curses.KEY_END, 27])
        self.ui.screen = screen
        self.ui._pressure_view()
        rendered = screen.text()
        self.assertIn("EXPEDITION PRESSURE", rendered)
        self.assertIn("RECENT CAUSES", rendered)
        self.assertIn("ENEMY ROUND", rendered)
        self.assertIn("HISTORY GAP", rendered)
        self.assertEqual(before, self.engine.snapshot())

        self.engine.start_combat("lost_shift")
        screen = FakeScreen(rows=24, columns=80, keys=[curses.KEY_END, 27])
        self.ui.screen = screen
        body = self.ui._pressure_text()
        self.ui._pressure_view()
        self.assertIn("THIS COMBAT", body)
        self.assertIn("applies to later encounters", body)

    def test_exploration_hud_and_route_show_exact_pressure(self) -> None:
        screen = FakeScreen(rows=24, columns=80)
        self.ui.screen = screen
        destination = self.engine._neighbors((self.engine.state.party_x, self.engine.state.party_y))[0]
        self.ui._render_exploration(destination)
        rendered = screen.text()
        self.assertIn("Pressure QUIET 0", rendered)
        self.assertIn(f"PR+{self.engine.movement_cost(*destination):2}>QUIET", rendered)

    def test_active_mutation_markers_are_visible_and_inspectable_in_combat(self) -> None:
        self.engine.state.pressure = 1_100
        self.engine.start_combat("lost_shift")
        self.assertTrue(self.engine.state.encounter_modules)
        screen = FakeScreen(rows=24, columns=80)
        self.ui.screen = screen
        self.ui._render_combat(0)
        rendered = screen.text()
        self.assertIn("MUTATIONS", rendered)
        for module in self.engine.state.encounter_modules:
            mutation = self.catalog.mutations[module]
            self.assertIn(mutation["marker"], self.ui._pressure_text())
            self.assertIn(mutation["description"], self.ui._pressure_text())

    def test_pressure_inspection_names_frozen_reinforcement_reserve(self) -> None:
        self.engine.start_combat("lost_shift")
        self.engine.state.encounter_modules = ["base:reinforcement_call"]
        self.engine.state.reinforcement_reserve_id = "hollow_crew"
        self.engine.state.reinforcement_tickets = 1
        text = self.ui._pressure_text()
        self.assertIn("DISCLOSED RESERVE", text)
        self.assertIn(self.catalog.enemies["hollow_crew"]["name"], text)
        self.assertIn("ready", text)

    def test_resource_hud_marks_effect_summary_overflow(self) -> None:
        hero = self.engine.living_heroes()[0]
        self.engine.state.boons[hero.id] = {boon_id: 1 for boon_id in self.catalog.boons}
        self.engine.state.items = {item_id: 1 for item_id in self.catalog.items}
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._resources(2)
        summary = "".join(screen.rows[4]).rstrip()
        self.assertTrue(summary.endswith("..."), summary)

    def test_transient_messages_mark_overflow(self) -> None:
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui.message = "A deliberately long terminal message " * 4
        self.ui._begin("TEST")
        message = "".join(screen.rows[1]).rstrip()
        self.assertTrue(message.endswith("..."), message)
        self.assertEqual("", self.ui.message)

    def test_mouse_click_maps_screen_cell_to_world_destination(self) -> None:
        self.ui.screen = FakeScreen()
        origin = (10, 4, 60, 15)
        with patch(
            "dumbest_dungeon.ui.curses.getmouse",
            return_value=(0, 12, 7, 0, curses.BUTTON1_CLICKED),
        ):
            self.assertEqual((12, 6), self.ui._mouse_destination(origin))

    def test_combat_formation_uses_the_center_of_a_large_terminal(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen(rows=40, columns=140)
        self.ui.screen = screen
        self.ui._battlefield(2, None, [])
        expected_warden_column = 29 + (140 - self.ui.MIN_COLS) // 2
        self.assertTrue(
            any(
                row == 3 and column == expected_warden_column
                for row, column, _, _ in screen.writes
            )
        )

    def test_mouse_requires_second_click_before_auto_walk(self) -> None:
        screen = FakeScreen(keys=[curses.KEY_MOUSE, curses.KEY_MOUSE])
        self.ui.screen = screen
        party = (self.engine.state.party_x, self.engine.state.party_y)
        destination = self.engine._neighbors(party)[0]
        patrol = self.engine.state.patrols[0]
        for other in self.engine.state.patrols:
            other.active = other is patrol
        patrol.x, patrol.y = destination
        mouse_event = (0, destination[0] + 2, destination[1] - 10 + 5, 0, curses.BUTTON1_CLICKED)
        with (
            patch("dumbest_dungeon.ui.curses.getmouse", side_effect=[mouse_event, mouse_event]),
            patch("dumbest_dungeon.ui.curses.napms"),
        ):
            self.ui._exploration()
        self.assertEqual("combat", self.engine.state.phase)
        selected_frames = [write for write in screen.writes if write[2] == "X"]
        self.assertTrue(any(attribute & curses.A_REVERSE for _, _, _, attribute in selected_frames))

    def test_auto_walk_into_patrol_opens_combat(self) -> None:
        screen = FakeScreen()
        self.ui.screen = screen
        party = (self.engine.state.party_x, self.engine.state.party_y)
        destination = self.engine._neighbors(party)[0]
        patrol = self.engine.state.patrols[0]
        for other in self.engine.state.patrols:
            other.active = other is patrol
        patrol.x, patrol.y = destination
        with patch("dumbest_dungeon.ui.curses.napms") as napms:
            self.ui._walk_to(destination)
        self.assertEqual("combat", self.engine.state.phase)
        napms.assert_called_once_with(self.ui.MOVE_FRAME_MS)

    def test_auto_walk_cancel_stops_before_next_atomic_step(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        path = self.engine._find_path(party, self.engine.room_position(1))[:3]
        self.assertEqual(3, len(path))
        control = GameEngine.new(self.catalog, self.engine.state.seed)
        screen = FakeScreen(keys=[-1, ord("x")])
        self.ui.screen = screen
        with patch("dumbest_dungeon.ui.curses.napms"):
            self.ui._walk_to(path[-1])
        control.step_exploration(*path[0])
        self.assertEqual(path[0], (self.engine.state.party_x, self.engine.state.party_y))
        self.assertEqual(1, self.engine.state.exploration_steps)
        self.assertEqual(100, self.engine.state.light)
        self.assertEqual(control.snapshot(), self.engine.snapshot())
        loaded = GameEngine.from_snapshot(self.catalog, self.engine.snapshot())
        self.assertEqual(self.engine.snapshot(), loaded.snapshot())
        self.assertIn("Route cancelled", self.ui.message)
        self.assertFalse(screen.nonblocking)

    def test_right_click_cancels_auto_walk_before_first_step(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        destination = self.engine._neighbors(party)[0]
        screen = FakeScreen(keys=[curses.KEY_MOUSE])
        self.ui.screen = screen
        event = (0, 0, 0, 0, getattr(curses, "BUTTON3_CLICKED", 0))
        if not event[-1]:
            self.skipTest("curses exposes no right-click event on this platform")
        with patch("dumbest_dungeon.ui.curses.getmouse", return_value=event):
            self.ui._walk_to(destination)
        self.assertEqual(party, (self.engine.state.party_x, self.engine.state.party_y))
        self.assertEqual(0, self.engine.state.exploration_steps)

    def test_auto_walk_stops_on_discovery_without_resolving_later_tiles(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        path = self.engine._find_path(party, self.engine.room_position(1))[:3]
        pickup = next(item for item in self.engine.state.pickups if not item.hidden)
        pickup.x, pickup.y = path[1]
        for patrol in self.engine.state.patrols:
            patrol.active = False
        screen = FakeScreen()
        self.ui.screen = screen
        with patch("dumbest_dungeon.ui.curses.napms"):
            self.ui._walk_to(path[-1])
        self.assertEqual("discovery", self.engine.state.phase)
        self.assertEqual(path[1], (self.engine.state.party_x, self.engine.state.party_y))
        self.assertEqual(2, self.engine.state.exploration_steps)

    def test_auto_walk_stops_on_biome_hazard_before_later_patrol_actions(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        path = self.engine._find_path(party, self.engine.room_position(1))[:3]
        hazard = self.engine.state.hazards[0]
        hazard.x, hazard.y = path[1]
        hazard.cells[0] = list(path[1])
        hazard.biome_id = self.engine.biome_at(*path[1])
        for patrol in self.engine.state.patrols:
            patrol.active = False
        screen = FakeScreen()
        self.ui.screen = screen
        with patch("dumbest_dungeon.ui.curses.napms"):
            self.ui._walk_to(path[-1])
        self.assertEqual("hazard", self.engine.state.phase)
        self.assertEqual(path[1], (self.engine.state.party_x, self.engine.state.party_y))
        self.assertEqual(2, self.engine.state.exploration_steps)
        self.assertIn(list(path[1]), hazard.triggered_cells)

    def test_fallen_crew_remains_visible_on_battlefield(self) -> None:
        self.engine.start_combat("vents")
        hero = self.engine.living_heroes()[0]
        hero.hp = 0
        hero.deaths_door = False
        self.engine._hero_died(hero)
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._battlefield(2, None, [])
        self.assertIn("DEAD", screen.text())
        self.assertIn(hero.hero_class[:4].upper(), screen.text())

    def test_hub_renders_roster_and_departs_with_default_party(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.ui.engine = self.engine
        screen = FakeScreen(keys=[10, 10])
        self.ui.screen = screen
        self.ui._hub()
        self.assertEqual("exploration", self.engine.state.phase)
        self.assertEqual(4, len(self.engine.state.heroes))
        self.assertEqual(20, len(self.engine.state.deck))
        rendered = screen.text()
        self.assertIn("CREW THRESHOLD", rendered)
        self.assertIn("Breacher", rendered)
        self.assertIn("Pilot", rendered)
        self.assertIn("READY TO DEPART", rendered)

    def test_hub_scrolls_to_new_biome_crew_at_minimum_size(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.ui.engine = self.engine
        screen = FakeScreen(
            keys=[curses.KEY_DOWN] * len(self.catalog.squads)
            + [10]
            + [curses.KEY_DOWN] * 24
            + [27]
        )
        self.ui.screen = screen
        self.ui._hub()
        rendered = screen.text()
        self.assertIn("Bonewright", rendered)
        self.assertIn(self.catalog.art["heroes"]["bonewright"][1].strip(), rendered)
        self.assertIn("ROLE DEFENDER", rendered)
        self.assertIn("PREFERRED RANKS 1,2", rendered)
        self.assertIn("SIGNATURE", rendered)
        self.assertIn("STRENGTH", rendered)
        self.assertIn("WEAKNESS", rendered)
        self.assertIn("BUILDS", rendered)

    def test_every_curated_squad_is_explained_and_selectable_at_minimum_size(self) -> None:
        for index, squad in enumerate(self.catalog.squads.values()):
            with self.subTest(squad=squad["id"]):
                self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
                self.ui.engine = self.engine
                screen = FakeScreen(keys=[curses.KEY_DOWN] * index + [10])
                self.ui.screen = screen
                self.assertTrue(self.ui._curated_squad_menu())
                self.assertEqual(squad["formation"], self.engine.state.hub_selection)
                rendered = screen.text()
                self.assertIn(squad["name"].upper(), rendered)
                self.assertIn("STRENGTH", rendered)
                self.assertIn("WEAKNESS", rendered)
                self.assertIn("SIGNATURE", rendered)
                for field in ("playstyle", "strength", "weakness", "signature"):
                    self.assertIn(squad[field].split()[-1], rendered)

    def test_custom_selection_shows_nonblocking_party_warning(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.engine.state.hub_selection = ["duelist", "pilot", "biologist", "scout"]
        self.ui.engine = self.engine
        screen = FakeScreen(
            keys=[curses.KEY_DOWN] * len(self.catalog.squads) + [10, 27]
        )
        self.ui.screen = screen
        self.ui._hub()
        self.assertIn("No defender", screen.text())

    def test_hub_combined_deck_preview_shows_formation_and_twenty_cards(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.engine.select_curated_squad("bulkhead_basics")
        self.ui.engine = self.engine
        screen = FakeScreen(keys=[27])
        self.ui.screen = screen
        self.ui._hub_deck_view()
        rendered = screen.text()
        self.assertIn("COMBINED STARTER DECK", rendered)
        self.assertIn("20 starting cards", rendered)
        self.assertIn("R1 Warden", rendered)
        self.assertIn("No serious", rendered)
        self.assertIn("formation warnings", rendered)
        self.assertIn("TARGET:", rendered)

    def test_hub_loadout_toggle_updates_combined_deck_preview(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.engine.select_curated_squad("bulkhead_basics")
        self.engine.select_hub_loadout("warden", "base:warden_field")
        self.ui.engine = self.engine
        self.ui.screen = FakeScreen(keys=[27])
        self.ui._hub_deck_view()
        rendered = self.ui.screen.text()
        self.assertIn("Warden (ADVANCED)", rendered)
        advanced_name = self.catalog.cards[
            self.catalog.loadouts["base:warden_field"]["cards"][0]
        ]["name"]
        self.assertIn(advanced_name, rendered)

    def test_doctrine_menu_discloses_strength_and_liability_before_selection(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.engine.select_curated_squad("bulkhead_basics")
        self.ui.engine = self.engine
        doctrine = next(iter(self.catalog.doctrines.values()))
        self.assertTrue(self.engine.doctrine_compatible(doctrine["id"]))
        self.ui.screen = FakeScreen(keys=[curses.KEY_DOWN, 10])
        self.ui._doctrine_menu()
        rendered = self.ui.screen.text()
        self.assertEqual(doctrine["id"], self.engine.state.doctrine_id)
        self.assertIn("STRENGTH", rendered)
        self.assertIn("LIABILITY", rendered)
        self.assertIn("DESC: NO", rendered)
        self.assertIn(doctrine["strength"].split()[-1], rendered)
        self.assertIn(doctrine["liability"].split()[-1], rendered)

    def test_doctrine_menu_marks_incompatible_rules_without_selecting_them(self) -> None:
        self.engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        self.engine.select_curated_squad("bulkhead_basics")
        self.ui.engine = self.engine
        identities = [None] + list(self.catalog.doctrines)
        target = identities.index("base:control_lattice")
        self.ui.screen = FakeScreen(keys=[curses.KEY_DOWN] * target + [10, 27])
        self.ui._doctrine_menu()
        self.assertIsNone(self.engine.state.doctrine_id)
        self.assertIn("LOCKED FOR THIS PARTY", self.ui.screen.text())

    def test_every_crew_identity_is_fully_readable_at_minimum_size(self) -> None:
        for hero in self.catalog.heroes.values():
            with self.subTest(hero=hero["id"]):
                screen = FakeScreen(keys=[10])
                self.ui.screen = screen
                self.ui._hub_identity_view(hero["id"])
                rendered = screen.text()
                for field in ("signature", "strength", "weakness"):
                    self.assertIn(hero[field].split()[-1], rendered)
                for build in hero["builds"]:
                    self.assertIn(build, rendered)

    def test_affinity_card_preview_names_its_biome_bonus(self) -> None:
        engine = GameEngine.new(self.catalog, 3, start_in_hub=True)
        engine.state.hub_selection = ["cryonaut", "warden", "medic", "scout"]
        engine.begin_expedition()
        self.ui.engine = engine
        lines = self.ui._card_lines(CardInstance("ice_pick"))
        self.assertIn("CRYOGENIC", " ".join(lines).upper())
        self.assertIn("+2", " ".join(lines))

    def test_upgraded_card_preview_uses_changed_rules(self) -> None:
        self.engine.start_combat("vents")
        lines = self.ui._card_lines(CardInstance("interpose", upgraded=True))
        rendered = " ".join(lines)
        self.assertIn("BOTH YOU", rendered.upper())
        self.assertIn("AND THE ALLY GAIN", rendered.upper())
        self.assertIn("BLOCK.", rendered.upper())
        self.assertNotIn("2 rounds", rendered)

    def test_damaged_enemy_gets_reverse_video_flash_and_damage_number(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen()
        self.ui.screen = screen
        before = self.ui._enemy_flash_snapshot()
        enemy = self.engine.living_enemies()[0]
        enemy.hp -= 5
        with patch("dumbest_dungeon.ui.curses.napms") as napms:
            self.ui._flash_damaged_enemies(before)
        self.assertTrue(any(attribute & curses.A_REVERSE for _, _, _, attribute in screen.writes))
        self.assertIn("-5", screen.text())
        self.assertEqual(1, screen.refreshes)
        napms.assert_called_once_with(self.ui.DAMAGE_FLASH_MS)

    def test_buffed_heroes_flash_together_with_change_labels(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen()
        self.ui.screen = screen
        heroes = self.engine.living_heroes()
        heroes[1].hp -= 4
        heroes[2].stress = 20
        before = self.ui._hero_buff_snapshot()
        heroes[0].block += 8
        heroes[1].hp += 4
        heroes[2].stress -= 10
        with patch("dumbest_dungeon.ui.curses.napms") as napms:
            self.ui._flash_buffed_heroes(before)
        rendered = screen.text()
        self.assertIn("B+8", rendered)
        self.assertIn("H+4", rendered)
        self.assertIn("S-10", rendered)
        self.assertEqual(1, screen.refreshes)
        napms.assert_called_once_with(self.ui.DAMAGE_FLASH_MS)


if __name__ == "__main__":
    unittest.main()
