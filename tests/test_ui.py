from __future__ import annotations

import curses
import unittest
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
        return self.keys.pop(0)


class AsciiUiTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()
        self.engine = GameEngine.new(self.catalog, 3)
        self.ui = TerminalUI.__new__(TerminalUI)
        self.ui.catalog = self.catalog
        self.ui.engine = self.engine
        self.ui.colour = False
        self.ui.message = ""

    def test_card_preview_is_fixed_size_printable_ascii(self) -> None:
        self.engine.start_combat("vents")
        lines = self.ui._card_lines(self.engine.state.hand[0])
        self.assertEqual(10, len(lines))
        self.assertTrue(all(len(line) == 34 for line in lines))
        self.assertTrue(all(line.isascii() and line.isprintable() for line in lines))
        self.assertTrue(lines[0].startswith("+---"))
        self.assertIn("TARGET:", lines[6])

    def test_minimum_size_battlefield_contains_both_formations(self) -> None:
        self.engine.start_combat("vents")
        screen = FakeScreen()
        self.ui.screen = screen
        card = self.engine.state.hand[0]
        definition = self.catalog.cards[card.card_id]
        self.ui._battlefield(2, definition["hero"], self.engine.valid_targets(0))
        self.ui._draw_card(13, 45, card)
        rendered = screen.text()
        self.assertIn("CREW", rendered)
        self.assertIn("HOST", rendered)
        self.assertIn("TARGET:", rendered)
        self.assertIn(self.catalog.art["heroes"]["warden"][1].strip(), rendered)
        self.assertIn(self.catalog.art["enemies"]["vent_crawler"][2].strip(), rendered)

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

    def test_ship_map_uses_diagonal_forks(self) -> None:
        screen = FakeScreen()
        self.ui.screen = screen
        self.ui._map(4)
        lines = ["".join(row).rstrip() for row in screen.rows[4:7]]
        self.assertIn("/---[??]---\\", lines[0])
        self.assertIn("<00>---[??]", lines[1])
        self.assertIn("\\---[??]---/", lines[2])
        self.assertNotIn("|", "".join(lines))

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


if __name__ == "__main__":
    unittest.main()
