from __future__ import annotations

import itertools
import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.simulation import (
    SandboxEnvironmentState,
    apply_call_action,
    apply_reaction_pass_action,
    apply_ron_actions,
    legal_reaction_actions,
)


class ReactionPriorityPermutationTests(unittest.TestCase):
    def test_all_ron_submission_orders_resolve_by_discard_priority(self) -> None:
        for ruleset, players in (("tenhou-4p", 4), ("tenhou-3p", 3)):
            for source_seat in range(players):
                reaction_seats = _reaction_seats(source_seat, players)
                for winner_count in range(1, len(reaction_seats) + 1):
                    for winners in itertools.combinations(reaction_seats, winner_count):
                        for submitted in itertools.permutations(winners):
                            with self.subTest(
                                ruleset=ruleset,
                                source_seat=source_seat,
                                submitted=submitted,
                            ):
                                terminal = apply_ron_actions(
                                    _ron_reaction_state(
                                        ruleset=ruleset,
                                        players=players,
                                        source_seat=source_seat,
                                    ),
                                    tuple((seat, _ron_action()) for seat in submitted),
                                )

                                if players == 4 and winner_count == 3:
                                    self.assertEqual(terminal.terminal_reason, "triple_ron")
                                    self.assertEqual(terminal.winner_seats, ())
                                else:
                                    expected = tuple(
                                        seat for seat in reaction_seats if seat in submitted
                                    )
                                    self.assertEqual(terminal.terminal_reason, "ron")
                                    self.assertEqual(terminal.winner_seats, expected)
                                    self.assertEqual(terminal.winner_seat, expected[0])

    def test_chi_priority_is_limited_to_next_seat_for_every_discarder(self) -> None:
        for source_seat in range(4):
            with self.subTest(source_seat=source_seat):
                state = _chi_reaction_state(source_seat=source_seat)
                next_seat = (source_seat + 1) % 4

                for seat in _reaction_seats(source_seat, 4):
                    actions = legal_reaction_actions(state, seat=seat)
                    if seat == next_seat:
                        self.assertEqual(actions, (_chi_action(), Action.pass_()))
                    else:
                        self.assertEqual(actions, (Action.pass_(),))

    def test_ron_blocks_chi_until_that_reaction_passes_for_every_discarder(self) -> None:
        for source_seat in range(4):
            with self.subTest(source_seat=source_seat):
                state = _chi_and_ron_reaction_state(source_seat=source_seat)
                chi_seat = (source_seat + 1) % 4
                ron_seat = (source_seat + 2) % 4

                with self.assertRaisesRegex(
                    ValueError, f"ron reactions are pending: {ron_seat}"
                ):
                    apply_call_action(state, seat=chi_seat, action=_chi_action())
                after_ron_pass = apply_reaction_pass_action(state, seat=ron_seat)
                called, _meld = apply_call_action(
                    after_ron_pass, seat=chi_seat, action=_chi_action()
                )

                self.assertEqual(called.current_seat, chi_seat)
                self.assertTrue(called.needs_discard)


def _ron_reaction_state(
    *, ruleset: str, players: int, source_seat: int
) -> SandboxEnvironmentState:
    reactions = _reaction_seats(source_seat, players)
    hands = [()] * players
    for seat in reactions:
        hands[seat] = _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s P P P 5m")
    return SandboxEnvironmentState(
        ruleset=ruleset,
        players=players,
        wall=(),
        hands=tuple(hands),
        pending_discard=Tile.parse("5m"),
        pending_discard_seat=source_seat,
        pending_reaction_seats=reactions,
    )


def _chi_reaction_state(*, source_seat: int) -> SandboxEnvironmentState:
    hands = [()] * 4
    for seat in _reaction_seats(source_seat, 4):
        hands[seat] = _tiles("1m 2m")
    return SandboxEnvironmentState(
        ruleset="tenhou-4p",
        players=4,
        wall=(),
        hands=tuple(hands),
        pending_discard=Tile.parse("3m"),
        pending_discard_seat=source_seat,
        pending_reaction_seats=_reaction_seats(source_seat, 4),
    )


def _chi_and_ron_reaction_state(*, source_seat: int) -> SandboxEnvironmentState:
    hands = [()] * 4
    hands[(source_seat + 1) % 4] = _tiles("1m 2m")
    hands[(source_seat + 2) % 4] = _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s P P P 3m")
    return SandboxEnvironmentState(
        ruleset="tenhou-4p",
        players=4,
        wall=(),
        hands=tuple(hands),
        pending_discard=Tile.parse("3m"),
        pending_discard_seat=source_seat,
        pending_reaction_seats=_reaction_seats(source_seat, 4),
    )


def _reaction_seats(source_seat: int, players: int) -> tuple[int, ...]:
    return tuple((source_seat + offset) % players for offset in range(1, players))


def _ron_action() -> Action:
    return Action(ActionKind.RON, TileType.parse("5m"))


def _chi_action() -> Action:
    return Action(
        ActionKind.CHI,
        TileType.parse("3m"),
        consumed=(Tile.parse("1m"), Tile.parse("2m")),
    )


def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())
