"""Headless renderer-neutral application coverage for active tavern games."""
from __future__ import annotations

import copy
import tempfile
import unittest
from dataclasses import FrozenInstanceError
from pathlib import Path

from jomon.actions import interact
from jomon.commands import (
    DiceActionCommand, DrawExchangeCommand, StartTavernGameCommand,
)
from jomon.dumbest_dungeon.expedition import start_match
from jomon.dumbest_dungeon.tabletop import patrons
from jomon.runtime_events import TavernCardsExchanged, TavernDiceRolled, TavernGameStarted
from jomon.save import load_game, save_game
from jomon.session import GameSession
from jomon.state import create_world
from jomon.tavern_dice import drive_npcs as drive_dice, roll, start_match as start_dice
from jomon.tavern_draw import draw_cards, drive_npcs as drive_draw, start_hand
from jomon.tavern_games import ACTIVE_GAME_IDS, active_games
from jomon.vessel import DICE_PLAYER_SEAT, DRAW_PLAYER_SEAT, TABLE_PLAYER_SEAT


def draw_session(seed: str = "tavern-session") -> GameSession:
    session = GameSession.create(seed)
    session._state.jomon_space = "tavern"  # test fixture setup, never frontend code
    session._state.position = DRAW_PLAYER_SEAT
    return session


def dice_session(seed: str = "tavern-session") -> GameSession:
    session = GameSession.create(seed)
    session._state.jomon_space = "tavern"
    session._state.position = DICE_PLAYER_SEAT
    return session


class TavernSessionTests(unittest.TestCase):
    def test_draw_commands_views_events_and_reducer_equivalence(self):
        direct = create_world("draw-session-equivalence")
        direct.jomon_space, direct.position = "tavern", DRAW_PLAYER_SEAT
        session = GameSession(copy.deepcopy(direct))
        opponents = tuple(row.actor_id for row in session.tavern_draw_view().available_opponents[:3])

        start_hand(direct, list(opponents), wagering=False)
        drive_draw(direct)
        outcome = session.submit(StartTavernGameCommand("draw", opponents, False))
        self.assertEqual(outcome.result_id, "tavern.game.started")
        self.assertIsInstance(outcome.events[0], TavernGameStarted)
        self.assertEqual(session._state.tavern_draw, direct.tavern_draw)
        self.assertEqual(session._state.trade_credit, direct.trade_credit)

        view = session.tavern_draw_view()
        self.assertTrue(view.active)
        self.assertEqual(len(view.hand), 5)
        self.assertEqual({card.card_id for card in view.hand}, {f"draw.card.{card}" for card in direct.tavern_draw["active_hand"]["hands"][0]})
        self.assertFalse(hasattr(view, "deck"))
        with self.assertRaises(FrozenInstanceError):
            view.active = False  # type: ignore[misc]
        self.assertIn("draw.exchange", view.legal_actions)

        draw_cards(direct, [])
        drive_draw(direct)
        exchanged = session.submit(DrawExchangeCommand(()))
        self.assertEqual(exchanged.result_id, "tavern.draw.exchanged")
        self.assertTrue(any(isinstance(event, TavernCardsExchanged) for event in exchanged.events))
        self.assertEqual(session._state.tavern_draw, direct.tavern_draw)
        self.assertEqual(session._state.trade_credit, direct.trade_credit)

    def test_dice_commands_views_events_and_reducer_equivalence(self):
        direct = create_world("dice-session-equivalence")
        direct.jomon_space, direct.position = "tavern", DICE_PLAYER_SEAT
        session = GameSession(copy.deepcopy(direct))
        opponents = tuple(row.actor_id for row in session.tavern_dice_view().available_opponents[:3])

        start_dice(direct, list(opponents))
        drive_dice(direct)
        started = session.submit(StartTavernGameCommand("dice", opponents))
        self.assertEqual(started.result_id, "tavern.game.started")
        self.assertEqual(session._state.tavern_dice, direct.tavern_dice)

        view = session.tavern_dice_view()
        self.assertTrue(view.active)
        self.assertEqual(view.legal_actions, ("dice.roll",))
        roll(direct)
        drive_dice(direct)
        rolled = session.submit(DiceActionCommand("dice.roll"))
        self.assertEqual(rolled.result_id, "tavern.dice.resolved")
        self.assertTrue(any(isinstance(event, TavernDiceRolled) for event in rolled.events))
        self.assertEqual(session._state.tavern_dice, direct.tavern_dice)
        self.assertEqual(session._state.trade_credit, direct.trade_credit)

        repeat = dice_session("dice-session-equivalence")
        opponents = tuple(row.actor_id for row in repeat.tavern_dice_view().available_opponents[:3])
        repeat.submit(StartTavernGameCommand("dice", opponents))
        self.assertEqual(repeat.submit(DiceActionCommand("dice.roll")).events, rolled.events)

    def test_dormant_dd_is_not_active_but_round_trips_and_does_not_block_games(self):
        state = create_world("dormant-dd")
        state.jomon_space, state.position = "tavern", TABLE_PLAYER_SEAT
        start_match(state, patrons(state)[0].id)
        payload = copy.deepcopy(state.tabletop)
        self.assertIn("dullest", active_games(state))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "dormant.json"
            save_game(state, path)
            loaded = load_game(path)
            self.assertEqual(loaded.tabletop, payload)
            save_game(loaded, path)
            reloaded = load_game(path)
        self.assertEqual(reloaded.tabletop, payload)

        session = GameSession(reloaded)
        before = copy.deepcopy(reloaded.to_dict())
        retired = session.submit(StartTavernGameCommand("dullest", ()))
        self.assertEqual(retired.result_id, "tavern.game.retired")
        self.assertEqual(reloaded.to_dict(), before)
        self.assertEqual(ACTIVE_GAME_IDS, frozenset({"draw", "dice"}))
        reloaded.position = DRAW_PLAYER_SEAT
        view = session.tavern_draw_view()
        self.assertEqual(session.submit(StartTavernGameCommand("draw", tuple(row.actor_id for row in view.available_opponents[:3]))).result_id, "tavern.game.started")

    def test_retired_table_has_no_normal_interaction_overlay(self):
        state = create_world("retired-table")
        state.jomon_space, state.position = "tavern", TABLE_PLAYER_SEAT
        result = interact(state)
        self.assertIsNone(result.overlay)
        self.assertFalse(result.changed)

    def test_session_tavern_save_round_trips_active_draw_and_dice_without_ui_state(self):
        draw = draw_session("session-tavern-save")
        opponents = tuple(row.actor_id for row in draw.tavern_draw_view().available_opponents[:3])
        self.assertEqual(draw.submit(StartTavernGameCommand("draw", opponents)).result_id, "tavern.game.started")
        dice = dice_session("session-tavern-save-dice")
        opponents = tuple(row.actor_id for row in dice.tavern_dice_view().available_opponents[:3])
        self.assertEqual(dice.submit(StartTavernGameCommand("dice", opponents)).result_id, "tavern.game.started")
        with tempfile.TemporaryDirectory() as directory:
            draw_path, dice_path = Path(directory) / "draw.json", Path(directory) / "dice.json"
            draw.save(draw_path); dice.save(dice_path)
            resumed_draw, resumed_dice = GameSession.load(draw_path), GameSession.load(dice_path)
        self.assertEqual(resumed_draw.tavern_draw_view(), draw.tavern_draw_view())
        self.assertEqual(resumed_dice.tavern_dice_view(), dice.tavern_dice_view())
        self.assertNotIn("runtime_events", draw._state.to_dict())
        self.assertNotIn("runtime_events", dice._state.to_dict())


if __name__ == "__main__":
    unittest.main()
