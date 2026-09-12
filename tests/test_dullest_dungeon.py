"""The tabletop game is a bounded, resumable part of Jomon."""

import copy
import curses
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.office_content import office_catalog
from dumbest_dungeon.tabletop import (
    _board, _knockout, _path, collection_for, end_turn, finish_match, new_match,
    patron_turn, patrons, play_card, start_match,
)
from dumbest_dungeon.tabletop_ui import run_tabletop
from jomon.actions import interact
from jomon.save import load_game, save_game
from jomon.state import Position, SAVE_FORMAT, create_world, game_state_from_dict, validate_state


class TabletopRulesTests(unittest.TestCase):
    def setUp(self):
        self.roles = list(office_catalog()[0])
        self.match = new_match("tabletop-rules", "crew-1", "crew-2", self.roles[:4], self.roles[4:8])

    def test_all_imported_roles_and_cards_have_office_faces(self):
        roles, cards = office_catalog()
        self.assertEqual((len(roles), len(cards)), (25, 290))
        self.assertTrue(all(card.name and card.description and card.role in roles for card in cards.values()))

    def test_six_boards_are_mirrored_and_each_side_can_reach_both_files(self):
        for layout in range(6):
            match = new_match(f"layout-{layout}", "crew-1", "crew-2", self.roles[:4], self.roles[4:8])
            match["board"] = _board(layout)
            self.assertTrue(all(row == row[::-1] for row in match["board"]))
            self.assertTrue(all(match["board"][piece["y"]][piece["x"]] != "#" for piece in match["pieces"]))
            for piece in (match["pieces"][0], match["pieces"][4]):
                for file in match["files"]:
                    self.assertTrue(_path(match, (piece["x"], piece["y"]), tuple(file["home"]), piece["id"]))

    def test_commute_is_a_card_action_and_invalid_move_is_atomic(self):
        before = copy.deepcopy(self.match)
        with self.assertRaises(ValueError):
            play_card(self.match, -1, "0:0", destination=(0, 0))
        self.assertEqual(self.match, before)
        play_card(self.match, -1, "0:0", destination=(5, 5))
        self.assertEqual((self.match["pieces"][0]["x"], self.match["energy"]), (5, 2))

    def test_worker_specific_card_mastery_and_treatment_change_real_effects(self):
        match = self.match
        side = match["sides"][0]
        side["hand"] = ["crossguard"]
        side["masteries"]["crossguard"] = "engine"
        side["infusions"]["crossguard"] = "base:followthrough_ink"
        before_draw = len(side["draw"])
        with self.assertRaisesRegex(ValueError, "another office worker"):
            play_card(match, 0, "0:1", "0:0")
        play_card(match, 0, "0:0", "0:1")
        self.assertEqual(match["pieces"][0]["block"], 9)
        self.assertTrue(match["pieces"][1]["guard"])
        self.assertEqual(len(side["draw"]), before_draw - 1)

    def test_group_card_hits_each_rival_and_conditional_card_waits_for_injury(self):
        match = self.match
        match["sides"][0]["hand"] = ["suppressing_burst", "last_bulwark"]
        match["sides"][0]["doctrine"] = "base:mark_window"
        for index, rival in enumerate(match["pieces"][4:]):
            rival["x"], rival["y"] = 6 + index, 5
        play_card(match, 0, "0:0", "1:0")
        self.assertTrue(all(piece["hp"] < piece["max_hp"] for piece in match["pieces"][4:]))
        self.assertEqual(match["energy"], 1)
        play_card(match, 0, "0:0")
        self.assertEqual(match["pieces"][0]["block"], 0)

    def test_steal_hold_scoring_and_knockout_drops_file(self):
        match = self.match
        runner = match["pieces"][0]
        runner["x"], runner["y"] = 27, 7
        play_card(match, -1, runner["id"], destination=(28, 7))
        self.assertEqual(match["files"][1]["carrier"], runner["id"])
        runner["x"], runner["y"] = 12, 7
        end_turn(match)
        self.assertEqual(match["pending_score"], 0)
        self.assertEqual(match["scores"], [0, 0])
        _knockout(match, runner)
        self.assertEqual(match["files"][1]["dropped"], [12, 7])
        end_turn(match)
        self.assertEqual(match["scores"], [0, 0])
        self.assertEqual(runner["respawn"], 1)

    def test_uninterrupted_approval_scores_after_rival_turn(self):
        match = self.match
        runner = match["pieces"][0]
        runner["x"], runner["y"] = 27, 7
        play_card(match, -1, runner["id"], destination=(28, 7))
        runner["x"], runner["y"] = 12, 7
        end_turn(match)
        end_turn(match)
        self.assertEqual(match["scores"], [1, 0])
        self.assertIsNone(match["files"][1]["carrier"])

    def test_patron_uses_legal_card_actions_and_overtime_finishes(self):
        match = self.match
        while match["winner"] is None:
            end_turn(match)
            if match["winner"] is None:
                patron_turn(match)
        self.assertIn(match["winner"], (0, 1, "draw"))
        self.assertLessEqual(match["round"], 23)
        self.assertTrue(any("files" in line for line in match["log"]) or sum(match["scores"]) > 0)


class TavernIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("tabletop-integration")
        self.state.jomon_space = "tavern"
        self.state.position = Position(31, 4)

    def test_table_is_physical_and_start_costs_exactly_one_world_action(self):
        self.assertEqual(interact(self.state).overlay, "tabletop")
        self.assertTrue(patrons(self.state))
        before = self.state.world_time
        match = start_match(self.state, patrons(self.state)[0].id)
        self.assertEqual(self.state.world_time, before + 1)
        play_card(match, -1, "0:0", destination=(5, 5))
        end_turn(match)
        patron_turn(match)
        self.assertEqual(self.state.world_time, before + 1)

    def test_atomic_parent_save_resumes_match_rng_and_queue(self):
        match = start_match(self.state, patrons(self.state)[0].id)
        play_card(match, -1, "0:0", destination=(5, 5))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            resumed = load_game(path)
        self.assertEqual(resumed.tabletop["active_match"], self.state.tabletop["active_match"])
        end_turn(self.state.tabletop["active_match"])
        patron_turn(self.state.tabletop["active_match"])
        end_turn(resumed.tabletop["active_match"])
        patron_turn(resumed.tabletop["active_match"])
        self.assertEqual(resumed.tabletop["active_match"], self.state.tabletop["active_match"])

    def test_format_seven_migrates_without_replacing_jomon_state(self):
        original = self.state.to_dict()
        original["save_format"] = 7
        original.pop("tabletop")
        for person in original["household"]:
            person.pop("strategy")
        original["bartender"].pop("strategy")
        original["merchant"].pop("strategy")
        for person in original["visitors"]:
            person.pop("strategy")
        migrated = game_state_from_dict(original)
        self.assertEqual(migrated.save_format, SAVE_FORMAT)
        self.assertEqual(migrated.region.levels, self.state.region.levels)
        self.assertEqual(migrated.items, self.state.items)
        self.assertEqual(migrated.tabletop, {"collections": {}, "records": [], "active_match": None})

    def test_first_win_per_patron_season_only_grants_bounded_existing_rewards(self):
        patron = patrons(self.state)[0]
        courier = self.state.courier
        initial_credit = self.state.trade_credit
        initial_relation = patron.relationships.get(courier.id, 0)
        collection_for(self.state, courier.id)
        first = start_match(self.state, patron.id)
        first["winner"] = 0
        self.assertEqual(finish_match(self.state), "win")
        self.assertEqual(self.state.trade_credit, initial_credit + 1)
        self.assertEqual(courier.strategy, 1)
        self.assertEqual(patron.relationships[courier.id], min(3, initial_relation + 1))
        second = start_match(self.state, patron.id)
        second["winner"] = 0
        finish_match(self.state)
        self.assertEqual(self.state.trade_credit, initial_credit + 1)
        self.assertEqual(courier.strategy, 1)
        validate_state(self.state)

    def test_keyboard_can_start_play_and_leave_a_resumable_match(self):
        class Screen:
            def __init__(self):
                self.keys = iter((13, ord("0"), curses.KEY_RIGHT, 13, ord("q")))
                self.drawn = []

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                pass

            def refresh(self):
                pass

            def addnstr(self, y, x, value, count, attr=0):
                self.drawn.append(value[:count])

            def getch(self):
                return next(self.keys)

        screen = Screen()
        run_tabletop(screen, self.state)
        self.assertIsNotNone(self.state.tabletop["active_match"])
        self.assertEqual(self.state.tabletop["active_match"]["pieces"][0]["x"], 5)
        self.assertTrue(any("DULLEST DUNGEON" in line for line in screen.drawn))


if __name__ == "__main__":
    unittest.main()
