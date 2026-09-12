"""Quay Bones has physical seats, bounded turns, and a finite real purse."""

import copy
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from jomon.actions import interact
from jomon.save import load_game, save_game
from jomon.state import Position, StateError, create_world, game_state_from_dict, validate_state
from jomon.tavern_dice import (
    MAX_ROLLS, ROUNDS, available_dice_opponents, close_match, drive_npcs,
    hold, roll, start_match,
)
from jomon.tavern_dice_ui import _draw_match, run_tavern_dice
from jomon.tavern_games_ui import dice_face
from jomon.vessel import DICE_NPC_SEATS, DICE_PLAYER_SEAT, DICE_SURFACE, DRAW_PLAYER_SEAT, TABLE_PLAYER_SEAT, TAVERN_MAP
from jomon.world import is_walkable


class QuayBonesTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("quay bones test")
        self.state.jomon_space = "tavern"
        self.state.position = DICE_PLAYER_SEAT
        self.opponents = [person.id for person in available_dice_opponents(self.state)[:3]]

    def test_third_table_is_physical_and_other_two_remain(self):
        self.assertEqual(TAVERN_MAP[DICE_PLAYER_SEAT.y][DICE_PLAYER_SEAT.x], "Q")
        self.assertEqual(TAVERN_MAP[DRAW_PLAYER_SEAT.y][DRAW_PLAYER_SEAT.x], "P")
        self.assertEqual(TAVERN_MAP[TABLE_PLAYER_SEAT.y][TABLE_PLAYER_SEAT.x], "D")
        self.assertTrue(is_walkable(self.state, DICE_PLAYER_SEAT))
        self.assertTrue(all(not is_walkable(self.state, point) for point in DICE_SURFACE))
        self.assertEqual(interact(self.state).overlay, "tavern-dice")

    def test_zero_credit_courier_can_start_and_save(self):
        self.assertEqual(self.state.trade_credit, 0)
        match = start_match(self.state, self.opponents)
        self.assertEqual((match["round"], match["turn"], self.state.world_time), (0, 0, 1))
        self.assertTrue(all(self.state.actor_schedules[identity].position in DICE_NPC_SEATS for identity in self.opponents))
        validate_state(self.state)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.tavern_dice, self.state.tavern_dice)

    def test_one_busts_and_double_forces_another_roll(self):
        match = start_match(self.state, self.opponents)
        with patch("jomon.tavern_dice._random", side_effect=(2, 2)):
            self.assertEqual(roll(self.state), (3, 3))
        self.assertEqual(match["turn_total"], 6)
        self.assertTrue(match["forced"])
        with self.assertRaisesRegex(ValueError, "double"):
            hold(self.state)
        with patch("jomon.tavern_dice._random", side_effect=(0, 3)):
            self.assertEqual(roll(self.state), (1, 4))
        self.assertEqual(match["scores"][0], 0)
        self.assertEqual(match["busts"][0], 1)
        self.assertEqual(match["turn"], 1)
        validate_state(self.state)

    def test_three_rounds_complete_with_bounded_rolls(self):
        match = start_match(self.state, self.opponents)
        for _ in range(ROUNDS * 4 * (MAX_ROLLS + 1)):
            drive_npcs(self.state)
            if match["phase"] == "complete":
                break
            if match["turn"] == 0:
                if match["roll_count"] and not match["forced"] and match["turn_total"] >= 12:
                    hold(self.state)
                else:
                    roll(self.state)
            validate_state(self.state)
        self.assertEqual(match["phase"], "complete")
        self.assertEqual(match["round"], ROUNDS)
        self.assertTrue(match["winners"])
        self.assertEqual(len(self.state.tavern_dice["records"]), 1)
        close_match(self.state)
        self.assertIsNone(self.state.tavern_dice["active_match"])

    def test_player_win_moves_only_counted_purse_credit(self):
        match = start_match(self.state, self.opponents)
        match["round"] = ROUNDS - 1
        match["turn"] = 3
        match["scores"] = [50, 20, 10, 0]
        match["roll_count"] = 1
        hold(self.state)
        self.assertEqual(match["winners"], [0])
        self.assertEqual((match["prize"], self.state.trade_credit, self.state.tavern_dice["purse"]), (2, 2, 22))
        validate_state(self.state)

    def test_tie_does_not_pay_and_empty_purse_stays_empty(self):
        match = start_match(self.state, self.opponents)
        match["round"] = ROUNDS - 1
        match["turn"] = 3
        match["scores"] = [30, 30, 10, 0]
        match["roll_count"] = 1
        hold(self.state)
        self.assertEqual((match["winners"], match["prize"], self.state.tavern_dice["purse"]), ([0, 1], 0, 24))
        close_match(self.state)
        self.state.tavern_dice["purse"] = 0
        second = start_match(self.state, self.opponents)
        second["round"] = ROUNDS - 1
        second["turn"] = 3
        second["scores"] = [30, 20, 10, 0]
        second["roll_count"] = 1
        hold(self.state)
        self.assertEqual((second["prize"], self.state.trade_credit), (0, 0))

    def test_other_tavern_games_cannot_open_during_a_live_contest(self):
        start_match(self.state, self.opponents)
        self.state.position = DRAW_PLAYER_SEAT
        self.assertIsNone(interact(self.state).overlay)
        self.state.position = TABLE_PLAYER_SEAT
        self.assertIsNone(interact(self.state).overlay)

    def test_old_save_defaults_and_reseats_displaced_visitor(self):
        data = self.state.to_dict()
        data.pop("tavern_dice")
        identity = self.state.household[1].id
        data["actor_schedules"][identity]["position"] = {"x": 50, "y": 15, "z": 0}
        data["actor_schedules"][identity]["destination"] = {"x": 50, "y": 15, "z": 0}
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.tavern_dice["purse"], 24)
        self.assertNotIn(loaded.actor_schedules[identity].position, DICE_SURFACE)

    def test_corrupt_match_is_rejected(self):
        start_match(self.state, self.opponents)
        broken = copy.deepcopy(self.state)
        broken.tavern_dice["active_match"]["last_dice"] = [7, 1]
        with self.assertRaises(StateError):
            validate_state(broken)

    def test_visible_dice_and_keyboard_lobby(self):
        self.assertEqual(len(dice_face(6)), 5)

        class Screen:
            def __init__(self):
                self.keys = iter((ord(" "), ord("j"), ord(" "), ord("j"), ord(" "), 13, ord("q")))
                self.drawn = []

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                self.drawn = []

            def refresh(self):
                pass

            def addnstr(self, row, col, value, count, attr=0):
                self.drawn.append(value[:count])

            def getch(self):
                return next(self.keys)

        screen = Screen()
        run_tavern_dice(screen, self.state)
        self.assertEqual(self.state.tavern_dice["active_match"]["players"][1:], self.opponents)
        match = self.state.tavern_dice["active_match"]
        match["last_dice"] = [4, 6]
        match["scores"][0] = 30
        _draw_match(screen, self.state)
        self.assertTrue(any("+-------+" in line for line in screen.drawn))
        self.assertTrue(any("#" in line for line in screen.drawn))


if __name__ == "__main__":
    unittest.main()
