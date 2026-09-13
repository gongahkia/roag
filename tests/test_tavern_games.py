"""Cross-table contracts stay shared without merging the three game engines."""

from __future__ import annotations

import copy
import unittest

from jomon.actions import interact
from jomon.state import StateError, create_world, game_state_from_dict, validate_state
from jomon.tavern_dice import ROUNDS, close_match, hold, start_match
from jomon.tavern_draw import start_hand
from jomon.tavern_games import active_games, available_opponents, invited_opponents, npc_credit, seated_game_opponents
from jomon.vessel import DICE_NPC_SEATS, DICE_PLAYER_SEAT, DRAW_PLAYER_SEAT, TABLE_PLAYER_SEAT


class TavernGameBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("shared tavern tables")
        self.state.jomon_space = "tavern"
        self.opponents = [person.id for person in available_opponents(self.state)[:3]]

    def test_one_game_blocks_other_tables_and_corrupt_overlap_on_load(self):
        self.state.position = DRAW_PLAYER_SEAT
        start_hand(self.state, self.opponents, wagering=False)
        self.assertEqual(active_games(self.state), ("draw",))
        self.state.position = DICE_PLAYER_SEAT
        self.assertIsNone(interact(self.state).overlay)
        with self.assertRaisesRegex(ValueError, "other active tavern game"):
            start_match(self.state, self.opponents)

        separate = create_world("shared tavern tables")
        separate.jomon_space = "tavern"
        separate.position = DICE_PLAYER_SEAT
        start_match(separate, self.opponents)
        self.state.tavern_dice = copy.deepcopy(separate.tavern_dice)
        with self.assertRaisesRegex(StateError, "more than one tavern game"):
            validate_state(self.state)
        with self.assertRaisesRegex(StateError, "more than one tavern game"):
            game_state_from_dict(self.state.to_dict())

    def test_npc_prize_remains_available_at_draw_table_after_save(self):
        self.state.position = DICE_PLAYER_SEAT
        match = start_match(self.state, self.opponents)
        self.assertEqual(seated_game_opponents(self.state), set(self.opponents))
        match["round"], match["turn"] = ROUNDS - 1, 3
        match["scores"] = [0, 50, 0, 0]
        match["roll_count"] = 1
        hold(self.state)
        self.assertEqual(npc_credit(self.state, self.opponents[0]), 14)
        self.assertEqual(seated_game_opponents(self.state), set())
        close_match(self.state)
        self.state.position = DRAW_PLAYER_SEAT
        self.state.trade_credit = 10
        start_hand(self.state, self.opponents, wagering=True)
        self.assertEqual(npc_credit(self.state, self.opponents[0]), 13)
        loaded = game_state_from_dict(self.state.to_dict())
        self.assertEqual(npc_credit(loaded, self.opponents[0]), 13)
        validate_state(loaded)

    def test_unavailable_chairs_do_not_mutate_invited_adults(self):
        self.state.position = DICE_PLAYER_SEAT
        before = self.state.to_dict()
        with self.assertRaisesRegex(ValueError, "three free opponent chairs"):
            invited_opponents(self.state, self.opponents, DICE_NPC_SEATS[:2], "bones")
        self.assertEqual(self.state.to_dict(), before)
        self.state.position = TABLE_PLAYER_SEAT
        self.assertEqual(active_games(self.state), ())


if __name__ == "__main__":
    unittest.main()
