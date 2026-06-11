from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType, tile_counts
from kenjaku.simulation import (
    SANDBOX_ENVIRONMENT_KIND,
    SandboxEnvironmentState,
    apply_call_action,
    apply_discard_action,
    apply_reaction_pass_action,
    apply_ron_action,
    apply_ron_actions,
    apply_tsumo_action,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_call_actions,
    legal_discard_actions,
    legal_reaction_actions,
    legal_ron_actions,
    legal_sandbox_actions,
    legal_tsumo_actions,
    pass_pending_discard_reactions,
)


class SandboxEnvironmentTests(unittest.TestCase):
    def test_initial_environment_is_deterministic(self) -> None:
        first = initial_sandbox_environment(ruleset="tenhou-4p", seed="fixed")
        second = initial_sandbox_environment(ruleset="tenhou-4p", seed="fixed")

        self.assertEqual(first, second)
        self.assertEqual(first.ruleset, "tenhou-4p")
        self.assertEqual(first.players, 4)
        self.assertEqual(first.current_seat, 0)
        self.assertEqual(first.turn, 0)
        self.assertEqual(first.hand_sizes(), [13, 13, 13, 13])
        self.assertEqual(len(first.wall), 84)
        self.assertIsNone(first.drawn_tile)
        self.assertEqual(first.to_payload()["kind"], SANDBOX_ENVIRONMENT_KIND)
        self.assertEqual(first.to_payload()["terminal_rewards"], [])

    def test_draw_legal_actions_and_discard_transition(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-4p", seed="transition")

        drawn = draw_for_current_seat(state)
        actions = legal_discard_actions(drawn)
        next_state, discard = apply_discard_action(drawn, actions[0])

        self.assertEqual(drawn.hand_sizes()[0], 14)
        self.assertGreaterEqual(len(actions), 1)
        self.assertEqual(actions[0].kind.value, "discard")
        self.assertIsInstance(discard, Tile)
        self.assertEqual(next_state.current_seat, 1)
        self.assertEqual(next_state.turn, 1)
        self.assertEqual(next_state.hand_sizes(), [13, 13, 13, 13])
        self.assertIsNone(next_state.drawn_tile)
        self.assertEqual(next_state.pending_discard, discard)
        self.assertEqual(next_state.pending_discard_seat, 0)
        self.assertEqual(next_state.pending_reaction_seats, (1, 2, 3))
        self.assertEqual(next_state.discards[0], (discard,))
        self.assertEqual(next_state.to_payload()["discards"][0], [discard.notation])
        self.assertEqual(next_state.to_payload()["temporary_furiten_seats"], [])

        with self.assertRaisesRegex(ValueError, "pending discard reactions"):
            draw_for_current_seat(next_state)

        advanced = pass_pending_discard_reactions(next_state)
        self.assertIsNone(advanced.pending_discard)
        self.assertIsNone(advanced.pending_discard_seat)
        self.assertEqual(advanced.pending_reaction_seats, ())
        self.assertEqual(advanced.current_seat, 1)

    def test_rejects_discard_before_draw_and_illegal_tile(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-4p", seed="errors")

        with self.assertRaisesRegex(ValueError, "must draw"):
            legal_discard_actions(state)

        drawn = draw_for_current_seat(state)
        with self.assertRaisesRegex(ValueError, "not in current hand"):
            apply_discard_action(drawn, _missing_discard_action(drawn))

    def test_tsumo_detection_marks_terminal_state(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state, stop_on_tsumo=True)

        self.assertEqual(drawn.terminal_reason, "tsumo")
        self.assertEqual(drawn.winner_seat, 0)
        self.assertEqual(drawn.winning_tile, Tile.parse("5m"))
        self.assertEqual(drawn.winning_shapes, ("standard",))
        self.assertEqual(drawn.terminal_rewards, (1.0, -1 / 3, -1 / 3, -1 / 3))
        self.assertEqual(drawn.to_payload()["terminal_rewards"], [1.0, -1 / 3, -1 / 3, -1 / 3])
        with self.assertRaisesRegex(ValueError, "already terminal"):
            legal_discard_actions(drawn)

    def test_tsumo_is_a_legal_action_before_terminal_application(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        tsumo_actions = legal_tsumo_actions(drawn)
        turn_actions = legal_sandbox_actions(drawn)
        terminal = apply_tsumo_action(drawn, tsumo_actions[0])

        self.assertEqual(tsumo_actions, (Action(ActionKind.TSUMO),))
        self.assertEqual(turn_actions[0], Action(ActionKind.TSUMO))
        self.assertIn(Action.discard("5m"), turn_actions)
        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winner_seat, 0)
        self.assertEqual(terminal.terminal_rewards, (1.0, -1 / 3, -1 / 3, -1 / 3))

    def test_ron_is_a_legal_reaction_to_pending_discard(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        reaction_state, discard = apply_discard_action(drawn, Action.discard("5m"))
        ron_actions = legal_ron_actions(reaction_state, seat=1)
        reaction_actions = legal_reaction_actions(reaction_state, seat=1)
        terminal = apply_ron_action(reaction_state, seat=1, action=ron_actions[0])

        self.assertEqual(discard, Tile.parse("5m"))
        self.assertEqual(ron_actions, (Action(ActionKind.RON, TileType.parse("5m")),))
        self.assertEqual(reaction_actions[0], Action(ActionKind.RON, TileType.parse("5m")))
        self.assertEqual(reaction_actions[-1], Action.pass_())
        self.assertEqual(
            legal_sandbox_actions(reaction_state, seat=1),
            (Action(ActionKind.RON, TileType.parse("5m")), Action.pass_()),
        )
        self.assertEqual(legal_reaction_actions(reaction_state, seat=2), (Action.pass_(),))
        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winner_seat, 1)
        self.assertEqual(terminal.winner_seats, (1,))
        self.assertEqual(terminal.winning_tile, Tile.parse("5m"))
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_shapes_by_seat, ((1, ("standard",)),))
        self.assertEqual(terminal.terminal_rewards, (-1.0, 1.0, 0.0, 0.0))

    def test_passing_legal_ron_sets_temporary_furiten_until_next_draw(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"), Tile.parse("5m")),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))

        self.assertEqual(
            legal_ron_actions(reaction_state, seat=1),
            (Action(ActionKind.RON, TileType.parse("5m")),),
        )
        self.assertEqual(
            pass_pending_discard_reactions(reaction_state).temporary_furiten_seats,
            (1,),
        )

        after_ron_pass = apply_reaction_pass_action(
            reaction_state,
            seat=1,
            action=Action.pass_(),
        )
        after_two_passes = apply_reaction_pass_action(after_ron_pass, seat=2)
        resolved = apply_reaction_pass_action(after_two_passes, seat=3)
        next_draw = draw_for_current_seat(resolved)

        self.assertEqual(after_ron_pass.temporary_furiten_seats, (1,))
        self.assertEqual(after_ron_pass.to_payload()["temporary_furiten_seats"], [1])
        self.assertEqual(resolved.temporary_furiten_seats, (1,))
        self.assertEqual(resolved.current_seat, 1)
        self.assertEqual(next_draw.current_seat, 1)
        self.assertEqual(next_draw.temporary_furiten_seats, ())

    def test_temporary_furiten_blocks_ron(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                (),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
            pending_discard=Tile.parse("5m"),
            pending_discard_seat=0,
            pending_reaction_seats=(1,),
            temporary_furiten_seats=(1,),
        )

        self.assertEqual(legal_ron_actions(state, seat=1), ())
        self.assertEqual(legal_reaction_actions(state, seat=1), (Action.pass_(),))
        with self.assertRaisesRegex(ValueError, "temporary furiten"):
            apply_ron_action(
                state,
                seat=1,
                action=Action(ActionKind.RON, TileType.parse("5m")),
            )

    def test_discard_furiten_blocks_ron_but_not_call_reaction(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("5m 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p E S"),
                _tiles("3m 4m 2p 3p 4p 2s 3s 4s E E E S S"),
                (),
                (),
            ),
            discards=(
                (),
                (Tile.parse("2m"),),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        chi = Action(
            ActionKind.CHI,
            TileType.parse("5m"),
            consumed=(Tile.parse("3m"), Tile.parse("4m")),
        )

        self.assertEqual(legal_ron_actions(reaction_state, seat=1), ())
        self.assertIn(chi, legal_call_actions(reaction_state, seat=1))
        self.assertEqual(
            legal_sandbox_actions(reaction_state, seat=1),
            (chi, Action.pass_()),
        )
        with self.assertRaisesRegex(ValueError, "discard furiten"):
            apply_ron_action(
                reaction_state,
                seat=1,
                action=Action(ActionKind.RON, TileType.parse("5m")),
            )

        called, meld = apply_call_action(reaction_state, seat=1, action=chi)

        self.assertEqual(meld.kind, ActionKind.CHI)
        self.assertEqual(called.current_seat, 1)
        self.assertTrue(called.needs_discard)

    def test_multi_ron_resolution_records_all_winners(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s S S S 5m"),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))

        terminal = apply_ron_actions(
            reaction_state,
            (
                (1, Action(ActionKind.RON, TileType.parse("5m"))),
                (2, Action(ActionKind.RON, TileType.parse("5m"))),
            ),
        )

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winner_seat, 1)
        self.assertEqual(terminal.winner_seats, (1, 2))
        self.assertEqual(terminal.winning_tile, Tile.parse("5m"))
        self.assertEqual(
            terminal.winning_shapes_by_seat,
            ((1, ("standard",)), (2, ("standard",))),
        )
        self.assertEqual(terminal.terminal_rewards, (-2.0, 1.0, 1.0, 0.0))
        payload = terminal.to_payload()
        self.assertEqual(payload["winner_seats"], [1, 2])
        self.assertEqual(
            payload["winning_shapes_by_seat"],
            [{"seat": 1, "shapes": ["standard"]}, {"seat": 2, "shapes": ["standard"]}],
        )

    def test_rejects_empty_or_duplicate_multi_ron_resolution(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        ron = Action(ActionKind.RON, TileType.parse("5m"))

        with self.assertRaisesRegex(ValueError, "at least one ron action"):
            apply_ron_actions(reaction_state, ())
        with self.assertRaisesRegex(ValueError, "duplicate ron reaction seat"):
            apply_ron_actions(reaction_state, ((1, ron), (1, ron)))

    def test_rejects_invalid_ron_reactions(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                (),
                (),
            ),
        )

        with self.assertRaisesRegex(ValueError, "no pending discard reaction"):
            legal_ron_actions(state, seat=1)

        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))

        self.assertEqual(legal_ron_actions(reaction_state, seat=1), ())
        with self.assertRaisesRegex(ValueError, "cannot react"):
            legal_ron_actions(reaction_state, seat=0)
        with self.assertRaisesRegex(ValueError, "not a winning ron"):
            apply_ron_action(
                reaction_state,
                seat=1,
                action=Action(ActionKind.RON, TileType.parse("5m")),
            )

    def test_call_actions_are_legal_reactions_to_pending_discard(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("3m 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p E S"),
                _tiles("1m 2m 4m 5m 3m 3m 3m 6p 7p 8p E S W"),
                _tiles("1m 2m 3m 4m 5m 6m 7p 8p 9p E S W N"),
                _tiles("3m 3m 5m 6m 7m 1p 2p 3p 4s 5s 6s E S"),
            ),
        )

        drawn = draw_for_current_seat(state)
        reaction_state, discard = apply_discard_action(drawn, Action.discard("3m"))
        seat_one_actions = legal_call_actions(reaction_state, seat=1)
        seat_three_actions = legal_call_actions(reaction_state, seat=3)
        reaction_actions = legal_reaction_actions(reaction_state, seat=1)

        self.assertEqual(discard, Tile.parse("3m"))
        self.assertIn(
            Action(
                ActionKind.CHI,
                TileType.parse("3m"),
                consumed=(Tile.parse("1m"), Tile.parse("2m")),
            ),
            seat_one_actions,
        )
        self.assertIn(
            Action(
                ActionKind.CHI,
                TileType.parse("3m"),
                consumed=(Tile.parse("2m"), Tile.parse("4m")),
            ),
            seat_one_actions,
        )
        self.assertIn(
            Action(
                ActionKind.PON,
                TileType.parse("3m"),
                consumed=(Tile.parse("3m"), Tile.parse("3m")),
            ),
            seat_one_actions,
        )
        self.assertIn(
            Action(
                ActionKind.MINKAN,
                TileType.parse("3m"),
                consumed=(Tile.parse("3m"), Tile.parse("3m"), Tile.parse("3m")),
            ),
            seat_one_actions,
        )
        self.assertNotIn(ActionKind.CHI, {action.kind for action in seat_three_actions})
        self.assertEqual(reaction_actions[-1], Action.pass_())

    def test_reaction_pass_action_resolves_one_seat_at_a_time(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("3m 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p E S"),
                _tiles("1m 2m 4m 5m 6p 7p 8p 1s 2s 3s E S W"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("3m"))

        after_one_pass = apply_reaction_pass_action(
            reaction_state,
            seat=1,
            action=Action.pass_(),
        )
        after_two_passes = apply_reaction_pass_action(after_one_pass, seat=2)
        resolved = apply_reaction_pass_action(after_two_passes, seat=3)

        self.assertEqual(after_one_pass.pending_reaction_seats, (2, 3))
        self.assertEqual(after_one_pass.pending_discard, Tile.parse("3m"))
        self.assertEqual(after_two_passes.pending_reaction_seats, (3,))
        self.assertIsNone(resolved.pending_discard)
        self.assertIsNone(resolved.pending_discard_seat)
        self.assertEqual(resolved.pending_reaction_seats, ())
        with self.assertRaisesRegex(ValueError, "requires a pass action"):
            apply_reaction_pass_action(
                reaction_state,
                seat=1,
                action=Action.discard("1m"),
            )

    def test_call_action_waits_for_pending_ron_reactions_to_pass(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("3m 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p E S"),
                _tiles("1m 2m 4m 5m 6p 7p 8p 1s 2s 3s E S W"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 3m"),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("3m"))
        chi = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("1m"), Tile.parse("2m")),
        )

        with self.assertRaisesRegex(ValueError, "ron reactions are pending: 2"):
            apply_call_action(reaction_state, seat=1, action=chi)

        after_ron_pass = apply_reaction_pass_action(reaction_state, seat=2)
        called, meld = apply_call_action(after_ron_pass, seat=1, action=chi)

        self.assertEqual(meld.kind, ActionKind.CHI)
        self.assertEqual(called.current_seat, 1)
        self.assertTrue(called.needs_discard)

    def test_apply_chi_call_sets_post_call_discard_state(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("3m 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p E S"),
                _tiles("1m 2m 4m 5m 6p 7p 8p 1s 2s 3s E S W"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("3m"))
        chi = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("1m"), Tile.parse("2m")),
        )

        called, meld = apply_call_action(reaction_state, seat=1, action=chi)
        discard_actions = legal_discard_actions(called)
        after_discard, discard = apply_discard_action(called, Action.discard("4m"))

        self.assertEqual(called.current_seat, 1)
        self.assertTrue(called.needs_discard)
        self.assertIsNone(called.drawn_tile)
        self.assertIsNone(called.pending_discard)
        self.assertEqual(called.hand_sizes()[1], 11)
        self.assertEqual(meld.kind, ActionKind.CHI)
        self.assertEqual(meld.called_tile, Tile.parse("3m"))
        self.assertEqual(meld.from_seat, 0)
        self.assertEqual(called.melds[1], (meld,))
        self.assertIn(Action.discard("4m"), discard_actions)
        self.assertEqual(discard, Tile.parse("4m"))
        self.assertFalse(after_discard.needs_discard)
        self.assertEqual(after_discard.pending_discard, Tile.parse("4m"))
        self.assertEqual(after_discard.pending_discard_seat, 1)

    def test_apply_minkan_call_uses_simple_replacement_draw(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"), Tile.parse("8s")),
            hands=(
                _tiles("3m 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p E S"),
                _tiles("3m 3m 3m 4m 5m 6p 7p 8p 1s 2s 3s E S"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("3m"))
        minkan = Action(
            ActionKind.MINKAN,
            TileType.parse("3m"),
            consumed=(Tile.parse("3m"), Tile.parse("3m"), Tile.parse("3m")),
        )

        called, meld = apply_call_action(reaction_state, seat=1, action=minkan)

        self.assertEqual(called.current_seat, 1)
        self.assertFalse(called.needs_discard)
        self.assertEqual(called.drawn_tile, Tile.parse("9s"))
        self.assertEqual(called.wall, ())
        self.assertEqual(called.hand_sizes()[1], 11)
        self.assertEqual(meld.kind, ActionKind.MINKAN)
        self.assertIn(Action.discard("9s"), legal_discard_actions(called))

    def test_rejects_tsumo_without_draw_or_winning_hand(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                (),
                (),
                (),
            ),
        )

        with self.assertRaisesRegex(ValueError, "must draw"):
            legal_tsumo_actions(state)

        drawn = draw_for_current_seat(state)
        self.assertEqual(legal_tsumo_actions(drawn), ())
        with self.assertRaisesRegex(ValueError, "not a winning tsumo"):
            apply_tsumo_action(drawn, Action(ActionKind.TSUMO))

    def test_wall_exhaustion_uses_neutral_terminal_rewards(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                (),
                (),
                (),
            ),
        )

        terminal = draw_for_current_seat(state)

        self.assertEqual(terminal.terminal_reason, "wall_exhausted")
        self.assertEqual(terminal.terminal_rewards, (0.0, 0.0, 0.0, 0.0))

    def test_sanma_environment_excludes_two_to_eight_manzu(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-3p", seed="sanma")
        visible = [tile.notation for hand in state.hands for tile in hand]
        wall = [tile.notation for tile in state.wall]

        self.assertEqual(state.players, 3)
        self.assertEqual(state.hand_sizes(), [13, 13, 13])
        self.assertEqual(len(state.wall), 69)
        self.assertFalse(any(tile in visible for tile in _excluded_sanma_manzu()))
        self.assertFalse(any(tile in wall for tile in _excluded_sanma_manzu()))


def _missing_discard_action(state: SandboxEnvironmentState) -> Action:
    counts = tile_counts(state.current_hand())
    for index, count in enumerate(counts):
        if count == 0:
            return Action.discard(TileType(index))
    raise AssertionError("test hand unexpectedly contains every tile type")


def _tiles(text: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in text.split())


def _excluded_sanma_manzu() -> tuple[str, ...]:
    return tuple(f"{rank}m" for rank in range(2, 9))


if __name__ == "__main__":
    unittest.main()
