from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.simulation import (
    HONBA_RON_POINTS,
    RIICHI_DEPOSIT_POINTS,
    SandboxEnvironmentState,
    apply_discard_action,
    apply_riichi_action,
    apply_ron_actions,
    draw_for_current_seat,
)


class MultiRonPaymentTests(unittest.TestCase):
    def test_double_ron_allocates_honba_and_sticks_to_priority_winner_for_both_rulesets(
        self,
    ) -> None:
        for ruleset, players in (("tenhou-4p", 4), ("tenhou-3p", 3)):
            with self.subTest(ruleset=ruleset):
                state = _multi_ron_state(ruleset=ruleset, players=players)
                terminal = apply_ron_actions(state, ((2, _ron()), (1, _ron())))
                priority, secondary = terminal.terminal_score_estimates
                priority_payment = priority.ron_payment + priority.honba_payment
                secondary_payment = secondary.ron_payment + secondary.honba_payment

                self.assertEqual(terminal.winner_seats, (1, 2))
                self.assertEqual(priority.honba_payment, 2 * HONBA_RON_POINTS)
                self.assertEqual(secondary.honba_payment, 0)
                self.assertEqual(priority.riichi_stick_points, 2 * RIICHI_DEPOSIT_POINTS)
                self.assertEqual(secondary.riichi_stick_points, 0)
                self.assertEqual(terminal.riichi_sticks, 0)
                self.assertEqual(
                    terminal.terminal_point_deltas,
                    (
                        -priority_payment - secondary_payment,
                        priority_payment + priority.riichi_stick_points,
                        secondary_payment,
                        *(() if players == 3 else (0,)),
                    ),
                )

    def test_multi_ron_refunds_only_the_unaccepted_new_riichi_stick(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("1m"), Tile.parse("9s")),
            points=(25000, 25000, 25000, 25000),
            riichi_sticks=2,
            honba=2,
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s E E E 5m"),
                _wait_on_nine_sou(),
                _wait_on_nine_sou(),
                (),
            ),
        )
        declared = apply_riichi_action(draw_for_current_seat(state), Action(ActionKind.RIICHI))
        reaction_state, _discard = apply_discard_action(declared, Action.discard("9s"))
        terminal = apply_ron_actions(reaction_state, ((2, _ron("9s")), (1, _ron("9s"))))
        priority, secondary = terminal.terminal_score_estimates
        priority_payment = priority.ron_payment + priority.honba_payment
        secondary_payment = secondary.ron_payment + secondary.honba_payment

        self.assertEqual(reaction_state.riichi_sticks, 3)
        self.assertEqual(terminal.winner_seats, (1, 2))
        self.assertEqual(priority.riichi_stick_points, 2 * RIICHI_DEPOSIT_POINTS)
        self.assertEqual(secondary.riichi_stick_points, 0)
        self.assertEqual(priority.honba_payment, 2 * HONBA_RON_POINTS)
        self.assertEqual(secondary.honba_payment, 0)
        self.assertEqual(terminal.points[0], 25000 - priority_payment - secondary_payment)
        self.assertEqual(sum(terminal.terminal_point_deltas), 3 * RIICHI_DEPOSIT_POINTS)
        self.assertEqual(terminal.riichi_sticks, 0)


def _multi_ron_state(*, ruleset: str, players: int) -> SandboxEnvironmentState:
    return SandboxEnvironmentState(
        ruleset=ruleset,
        players=players,
        wall=(),
        points=(25000,) * players,
        riichi_sticks=2,
        honba=2,
        hands=(
            (),
            _wait_on_five_man(),
            _wait_on_five_man(),
            *(() if players == 3 else ((),)),
        ),
        pending_discard=Tile.parse("5m"),
        pending_discard_seat=0,
        pending_reaction_seats=(1, 2),
    )


def _ron(tile: str = "5m") -> Action:
    return Action(ActionKind.RON, TileType.parse(tile))


def _wait_on_five_man() -> tuple[Tile, ...]:
    return _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s P P P 5m")


def _wait_on_nine_sou() -> tuple[Tile, ...]:
    return _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s P P P 9s")


def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())
