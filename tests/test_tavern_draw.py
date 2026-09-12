"""A separate four-seat, bounded betting game belongs only to the tavern."""

import copy
import tempfile
import unittest
from pathlib import Path

from jomon.actions import interact
from jomon.save import load_game, save_game
from jomon.state import Position, StateError, create_world, game_state_from_dict, validate_state
from jomon.tavern_draw import (
    MAX_EXPOSURE, _npc_bet, _npc_discards, _settle, available_opponents, bet_action,
    close_hand, draw_cards, drive_npcs, evaluate, start_hand,
)
from jomon.tavern_draw_ui import _draw_hand, card_frame, card_name, run_tavern_draw
from jomon.vessel import DRAW_NPC_SEATS, DRAW_PLAYER_SEAT, DRAW_SURFACE, TABLE_PLAYER_SEAT, TAVERN_MAP, _walkable, normalise_schedule_work_positions
from jomon.world import is_walkable


def card(rank, suit):
    return suit * 13 + rank - 2


class TavernDrawTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("four-seat draw")
        self.state.jomon_space = "tavern"
        self.state.position = DRAW_PLAYER_SEAT
        self.opponents = [person.id for person in available_opponents(self.state)[:3]]

    def test_second_table_is_physical_and_dungeon_chair_is_unchanged(self):
        self.assertEqual(TAVERN_MAP[DRAW_PLAYER_SEAT.y][DRAW_PLAYER_SEAT.x], "P")
        self.assertEqual(TAVERN_MAP[TABLE_PLAYER_SEAT.y][TABLE_PLAYER_SEAT.x], "D")
        self.assertEqual(len(DRAW_SURFACE), 65)
        self.assertTrue(is_walkable(self.state, DRAW_PLAYER_SEAT))
        self.assertTrue(all(not is_walkable(self.state, point) for point in DRAW_SURFACE))
        self.assertTrue(all(_walkable("tavern", seat) for seat in DRAW_NPC_SEATS))
        self.assertEqual(interact(self.state).overlay, "tavern-draw")

    def test_free_practice_is_playable_without_starting_credit(self):
        self.assertEqual(self.state.trade_credit, 0)
        hand = start_hand(self.state, self.opponents, wagering=False)
        self.assertEqual(len(hand["players"]), 4)
        self.assertEqual(hand["players"][0], self.state.courier.id)
        self.assertEqual(hand["phase"], "draw")
        self.assertEqual(hand["pot"], 0)
        self.assertEqual(len({card for row in hand["hands"] for card in row}), 20)
        self.assertEqual(self.state.world_time, 1)
        self.assertTrue(all(self.state.actor_schedules[identity].position in DRAW_NPC_SEATS for identity in self.opponents))
        self.assertTrue(all(self.state.actor_schedules[identity].activity == "playing Tavern Draw" for identity in self.opponents))
        validate_state(self.state)

    def test_standard_high_hands_and_ace_low_straight(self):
        hands = [
            [card(rank, suit) for rank, suit in zip((2, 4, 7, 9, 12), (0, 1, 2, 3, 0))],
            [card(rank, suit) for rank, suit in zip((2, 2, 7, 9, 12), (0, 1, 2, 3, 0))],
            [card(rank, suit) for rank, suit in zip((2, 2, 7, 7, 12), (0, 1, 2, 3, 0))],
            [card(rank, suit) for rank, suit in zip((2, 2, 2, 9, 12), (0, 1, 2, 3, 0))],
            [card(rank, suit) for rank, suit in zip((2, 3, 4, 5, 14), (0, 1, 2, 3, 0))],
            [card(rank, 0) for rank in (2, 4, 7, 9, 12)],
            [card(rank, suit) for rank, suit in zip((2, 2, 2, 7, 7), (0, 1, 2, 0, 1))],
            [card(rank, suit) for rank, suit in zip((2, 2, 2, 2, 7), (0, 1, 2, 3, 0))],
            [card(rank, 0) for rank in (2, 3, 4, 5, 6)],
        ]
        self.assertEqual([evaluate(hand)[0] for hand in hands], list(range(9)))
        self.assertEqual(evaluate(hands[4]), (4, 5))
        self.assertGreater(evaluate([card(rank, 0) for rank in (10, 11, 12, 13, 14)]), evaluate(hands[8]))
        self.assertEqual(evaluate([card(rank, suit) for rank, suit in zip((2, 2, 7, 9, 12), (2, 3, 0, 1, 2))]), evaluate(hands[1]))

    def test_wagered_hand_is_zero_sum_and_capped(self):
        self.state.trade_credit = 10
        hand = start_hand(self.state, self.opponents, wagering=True)
        initial = 10 + 3 * 12
        for _ in range(24):
            drive_npcs(self.state)
            if hand["phase"] == "complete":
                break
            if hand["phase"] == "draw":
                draw_cards(self.state, [])
            else:
                bet_action(self.state, "call")
            self.assertEqual(self.state.trade_credit + sum(self.state.tavern_draw["bankrolls"][identity] for identity in self.opponents) + hand["pot"], initial)
            validate_state(self.state)
        self.assertEqual(hand["phase"], "complete")
        self.assertEqual(self.state.trade_credit + sum(self.state.tavern_draw["bankrolls"][identity] for identity in self.opponents), initial)
        self.assertTrue(all(amount <= MAX_EXPOSURE for amount in hand["committed"]))
        self.assertEqual(sum(hand["payouts"]), hand["final_pot"])
        self.assertEqual(len(self.state.tavern_draw["records"]), 1)
        close_hand(self.state)
        self.assertIsNone(self.state.tavern_draw["active_hand"])

    def test_opening_bet_can_be_raised_once_and_reopens_action(self):
        self.state.trade_credit = 10
        hand = start_hand(self.state, self.opponents, wagering=True)
        self.assertEqual(hand["turn"], 0)
        bet_action(self.state, "raise")
        self.assertEqual((hand["current_bet"], hand["raises"], hand["turn"]), (1, 0, 1))
        bet_action(self.state, "raise")
        self.assertEqual((hand["current_bet"], hand["raises"], hand["turn"]), (2, 1, 2))
        self.assertFalse(hand["acted"][0])
        with self.assertRaisesRegex(ValueError, "one raise"):
            bet_action(self.state, "raise")
        self.assertEqual(hand["turn"], 2)
        validate_state(self.state)

    def test_unaffordable_wager_and_unavailable_adult_do_not_mutate(self):
        before = self.state.to_dict()
        with self.assertRaisesRegex(ValueError, "needs 5 credit"):
            start_hand(self.state, self.opponents, wagering=True)
        self.assertEqual(self.state.to_dict(), before)
        with self.assertRaisesRegex(ValueError, "in the tavern"):
            start_hand(self.state, [*self.opponents[:2], "absent"], wagering=False)
        self.assertEqual(self.state.to_dict(), before)
        self.state.trade_credit = 10
        self.state.tavern_draw["bankrolls"][self.opponents[0]] = 4
        before = self.state.to_dict()
        with self.assertRaisesRegex(ValueError, "needs 5 credit"):
            start_hand(self.state, self.opponents, wagering=True)
        self.assertEqual(self.state.to_dict(), before)

    def test_folded_courier_does_not_stop_three_npc_players(self):
        self.state.trade_credit = 8
        hand = start_hand(self.state, self.opponents, wagering=True)
        bet_action(self.state, "fold")
        drive_npcs(self.state)
        self.assertEqual(hand["phase"], "complete")
        self.assertNotIn(0, hand["winners"])
        self.assertEqual(sum(hand["payouts"]), hand["final_pot"])
        validate_state(self.state)

    def test_tied_best_hands_split_pot_with_clockwise_remainder(self):
        self.state.trade_credit = 10
        hand = start_hand(self.state, self.opponents, wagering=True)
        hands = [
            [card(rank, suit) for rank, suit in zip((2, 3, 4, 5, 6), (0, 1, 2, 3, 0))],
            [card(rank, suit) for rank, suit in zip((2, 3, 4, 5, 6), (1, 2, 3, 0, 1))],
            [card(rank, suit) for rank, suit in zip((7, 9, 11, 12, 14), (0, 1, 2, 3, 0))],
            [card(rank, suit) for rank, suit in zip((8, 8, 10, 13, 14), (0, 1, 2, 3, 1))],
        ]
        used = {card for row in hands for card in row}
        self.assertEqual(len(used), 20)
        hand["hands"] = hands
        hand["deck"] = [card for card in range(52) if card not in used]
        hand["pot"] += 1
        hand["committed"][0] += 1
        self.state.trade_credit -= 1
        _settle(self.state, hand)
        self.assertEqual(hand["winners"], [0, 1])
        self.assertEqual(hand["payouts"], [3, 2, 0, 0])
        validate_state(self.state)

    def test_exchange_preserves_unique_deck_and_hides_unseen_cards_from_ai(self):
        hand = start_hand(self.state, self.opponents, wagering=False)
        old = hand["hands"][0][:]
        draw_cards(self.state, [0, 2, 4])
        self.assertEqual(len(hand["hands"][0]), 5)
        self.assertTrue(set(old[index] for index in (0, 2, 4)) <= set(hand["discards"]))
        self.assertEqual(len(set(hand["deck"] + hand["discards"] + [card for row in hand["hands"] for card in row])), 52)
        one = copy.deepcopy(hand)
        two = copy.deepcopy(hand)
        two["hands"][0], two["hands"][2] = two["hands"][2], two["hands"][0]
        one["turn"] = two["turn"] = 1
        self.assertEqual(_npc_discards(one, 1), _npc_discards(two, 1))
        self.assertEqual(_npc_bet(one), _npc_bet(two))

    def test_mid_hand_save_roundtrip_and_tamper_rejection(self):
        self.state.trade_credit = 10
        start_hand(self.state, self.opponents, wagering=True)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.tavern_draw, self.state.tavern_draw)
        self.assertEqual(loaded.trade_credit, self.state.trade_credit)
        loaded.tavern_draw["active_hand"]["hands"][0][0] = loaded.tavern_draw["active_hand"]["hands"][1][0]
        with self.assertRaises(StateError):
            validate_state(loaded)

    def test_old_save_defaults_table_and_reseats_people_off_new_surface(self):
        data = self.state.to_dict()
        data.pop("tavern_draw")
        data["position"] = {"x": 16, "y": 14, "z": 0}
        identity = self.state.household[1].id
        data["actor_schedules"][identity]["position"] = {"x": 18, "y": 14, "z": 0}
        data["actor_schedules"][identity]["destination"] = {"x": 18, "y": 14, "z": 0}
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.tavern_draw["hand_number"], 0)
        self.assertEqual(loaded.position, DRAW_PLAYER_SEAT)
        self.assertNotIn(loaded.actor_schedules[identity].position, DRAW_SURFACE)

    def test_old_save_reseats_even_when_every_draw_chair_is_occupied(self):
        active = self.state.actor_schedules[self.state.active_courier_id]
        active.area = "tavern"
        active.position = DRAW_PLAYER_SEAT
        people = [person.id for person in [*self.state.household, *self.state.visitors]
                  if person.id != self.state.active_courier_id]
        for identity, chair in zip(people[:7], DRAW_NPC_SEATS):
            schedule = self.state.actor_schedules[identity]
            schedule.area = "tavern"
            schedule.position = chair
        displaced = self.state.actor_schedules[people[7]]
        displaced.area = "tavern"
        displaced.position = Position(15, 14, 0)
        normalise_schedule_work_positions(self.state)
        self.assertNotIn(displaced.position, DRAW_SURFACE)
        self.assertNotEqual(displaced.position, DRAW_PLAYER_SEAT)
        self.assertNotIn(displaced.position, DRAW_NPC_SEATS)

    def test_draw_ui_shows_only_own_cards_until_showdown(self):
        class Screen:
            def __init__(self):
                self.drawn = []

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                self.drawn = []

            def refresh(self):
                pass

            def addnstr(self, row, col, value, count, attr=0):
                self.drawn.append(value[:count])

        hand = start_hand(self.state, self.opponents, wagering=False)
        screen = Screen()
        _draw_hand(screen, self.state, set(), "")
        enemy_cards = " ".join(card_name(card) for card in hand["hands"][1])
        self.assertNotIn(enemy_cards, " ".join(screen.drawn))
        self.assertTrue(any("[##]" in line for line in screen.drawn))
        self.assertEqual(len(card_frame(hand["hands"][0][0])), 7)
        drive_npcs(self.state)
        draw_cards(self.state, [])
        drive_npcs(self.state)
        _draw_hand(screen, self.state, set(), "")
        revealed = " ".join(card_name(card) for card in hand["hands"][1])
        self.assertIn(revealed, " ".join(screen.drawn))
        self.assertTrue(any("YOUR NET" in line for line in screen.drawn))
        self.assertTrue(any("[ TAVERN DRAW / SHOWDOWN ]" in line for line in screen.drawn))

    def test_keyboard_can_invite_three_and_pause_a_practice_hand(self):
        class Screen:
            def __init__(self):
                self.keys = iter((ord(" "), ord("j"), ord(" "), ord("j"), ord(" "), 13, ord("q")))

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                pass

            def refresh(self):
                pass

            def addnstr(self, row, col, value, count, attr=0):
                pass

            def getch(self):
                return next(self.keys)

        run_tavern_draw(Screen(), self.state)
        self.assertEqual(self.state.tavern_draw["active_hand"]["players"][1:], self.opponents)
        self.assertEqual(self.state.tavern_draw["active_hand"]["phase"], "draw")
        validate_state(self.state)


if __name__ == "__main__":
    unittest.main()
