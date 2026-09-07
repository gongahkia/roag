from __future__ import annotations

import curses
import unittest
from pathlib import Path
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine
from dumbest_dungeon.ui import TerminalUI


class FakeScreen:
    def __init__(self, rows: int = 24, columns: int = 80, keys: list[int] | None = None):
        self.rows = [[" "] * columns for _ in range(rows)]
        self.keys = list(keys or [])
        self.writes: list[tuple[int, int, str, int]] = []
        self.refreshes = 0
        self.nonblocking = False

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


class AsciiUiTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()
        self.engine = GameEngine.new(self.catalog, 3)
        self.ui = TerminalUI.__new__(TerminalUI)
        self.ui.catalog = self.catalog
        self.ui.engine = self.engine
        self.ui.colour = False
        self.ui.message = ""

    def test_card_preview_is_portrait_playing_card_ascii(self) -> None:
        self.engine.start_combat("vents")
        lines = self.ui._card_lines(self.engine.state.hand[0])
        self.assertEqual(17, len(lines))
        self.assertTrue(all(len(line) == 22 for line in lines))
        self.assertTrue(all(line.isascii() and line.isprintable() for line in lines))
        self.assertTrue(lines[0].startswith("+---"))
        self.assertIn("TARGET:", lines[10])
        self.assertEqual(lines[1][1], lines[-2][-2])

    def test_main_menu_uses_public_title(self) -> None:
        screen = FakeScreen(keys=[curses.KEY_DOWN, curses.KEY_DOWN, 10])
        self.ui.screen = screen
        self.ui.save_path = Path("/definitely/missing/dullest-save.json")
        self.ui.new_game = lambda: self.engine
        self.ui.run()
        self.assertIn("DULLEST DUNGEON", screen.text())

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
        self.assertIn("already represented", rendered)
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

    def test_combat_hand_is_a_row_of_five_miniature_cards(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._render_combat(0)
        self.assertEqual(5, "".join(screen.rows[13]).count("+------------+"))
        first_card_writes = [write for write in screen.writes if write[0] in range(13, 23) and write[1] == 2]
        self.assertTrue(all(attribute & curses.A_REVERSE for _, _, _, attribute in first_card_writes))

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
        patrol = self.engine.state.patrols[0]
        patrol.x, patrol.y = party[0] + 3, party[1]
        pickup = next(item for item in self.engine.state.pickups if not item.hidden)
        pickup.x, pickup.y = party[0] + 2, party[1]
        pickup.kind = "boon"
        destination = self.engine.room_position(1)
        self.ui._world_map(5, destination)
        rendered = screen.text()
        self.assertIn("#", rendered)
        self.assertIn(".", rendered)
        self.assertIn(":", rendered)
        self.assertIn("@", rendered)
        self.assertIn("X", rendered)
        self.assertIn("e", rendered)
        self.assertIn("+", rendered)

    def test_world_map_viewport_is_bounded_by_world_on_large_terminal(self) -> None:
        screen = FakeScreen(rows=60, columns=140)
        self.ui.screen = screen
        tiles = self.engine.world_tiles()
        party = (self.engine.state.party_x, self.engine.state.party_y)
        origin = self.ui._world_map(self.ui.MAP_ROW, party)
        self.assertEqual((0, 0, len(tiles[0]), len(tiles)), origin)

        with patch(
            "dumbest_dungeon.ui.curses.getmouse",
            return_value=(0, len(tiles[0]) + 2, self.ui.MAP_ROW, 0, curses.BUTTON1_CLICKED),
        ):
            self.assertIsNone(self.ui._mouse_destination(origin))

    def test_map_and_biome_view_explain_objective_hazard_and_core_lock(self) -> None:
        party = (self.engine.state.party_x, self.engine.state.party_y)
        objective = self.engine.state.objectives[0]
        hazard = self.engine.state.hazards[0]
        objective.x, objective.y = party[0] + 2, party[1]
        hazard.x, hazard.y = party[0] + 1, party[1]
        self.engine.state.room_positions[11] = [party[0] + 3, party[1]]
        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._world_map(5, (objective.x, objective.y))
        rendered = screen.text()
        self.assertIn("K", rendered)
        self.assertIn("^", rendered)
        self.assertIn("L", rendered)

        screen = FakeScreen(keys=[10])
        self.ui.screen = screen
        self.ui._biome_view()
        rendered = screen.text()
        self.assertIn("TRAVEL", rendered)
        self.assertIn("PATROLS", rendered)
        self.assertIn("COMBAT", rendered)
        self.assertIn("OBJECTIVE", rendered)

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
        self.assertTrue(objective.completed)

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

    def test_mouse_click_maps_screen_cell_to_world_destination(self) -> None:
        origin = (10, 4, 60, 15)
        with patch(
            "dumbest_dungeon.ui.curses.getmouse",
            return_value=(0, 12, 7, 0, curses.BUTTON1_CLICKED),
        ):
            self.assertEqual((20, 6), self.ui._mouse_destination(origin))

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
        self.assertTrue(hazard.triggered)

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
        self.assertIn("Bonewright", screen.text())
        self.assertIn(self.catalog.art["heroes"]["bonewright"][1].strip(), screen.text())

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
