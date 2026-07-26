from __future__ import annotations

import json
import unittest
from pathlib import Path

from kenjaku.core import ActionKind
from kenjaku.simulation.environment import (
    apply_discard_action,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_sandbox_actions,
)


class BrowserConformanceFixtureTests(unittest.TestCase):
    def test_python_sandbox_discard_fixture_is_current(self) -> None:
        fixture_path = (
            Path(__file__).parents[1] / "web/tests/fixtures/python-sandbox-discard-v1.json"
        )
        fixture = json.loads(fixture_path.read_text(encoding="utf-8"))
        drawn = draw_for_current_seat(initial_sandbox_environment(seed=fixture["seed"]))
        actions = legal_sandbox_actions(drawn)
        discard = next(
            action
            for action in actions
            if action.kind is ActionKind.DISCARD and action.tile is not None
            and action.tile.notation == fixture["action"]["tile"]
        )
        next_state, discarded = apply_discard_action(drawn, discard)

        self.assertEqual(fixture["before"]["hand"], [tile.notation for tile in drawn.hands[0]])
        self.assertEqual(fixture["before"]["wall_remaining"], len(drawn.wall))
        self.assertEqual(fixture["after"]["discard"], discarded.notation)
        self.assertEqual(fixture["after"]["hand"], [tile.notation for tile in next_state.hands[0]])
        self.assertEqual(fixture["after"]["current_seat"], next_state.current_seat)
        self.assertEqual(fixture["after"]["turn"], next_state.turn)
        self.assertEqual(fixture["after"]["wall_remaining"], len(next_state.wall))
        self.assertEqual(fixture["after"]["pending_discard"], next_state.pending_discard.notation)
        self.assertEqual(
            fixture["after"]["pending_reaction_seats"],
            list(next_state.pending_reaction_seats),
        )
