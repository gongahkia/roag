from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Meld, Tile, TileType, tile_counts
from kenjaku.simulation import (
    HONBA_RON_POINTS,
    HONBA_TSUMO_POINTS_PER_LOSER,
    RIICHI_DEPOSIT_POINTS,
    SANDBOX_3P_INITIAL_POINTS,
    SANDBOX_ENVIRONMENT_KIND,
    SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL,
    SANDBOX_INITIAL_POINTS,
    SandboxEnvironmentState,
    apply_ankan_action,
    apply_call_action,
    apply_discard_action,
    apply_kakan_action,
    apply_kita_action,
    apply_reaction_pass_action,
    apply_riichi_action,
    apply_ron_action,
    apply_ron_actions,
    apply_tsumo_action,
    draw_for_current_seat,
    initial_sandbox_environment,
    legal_ankan_actions,
    legal_call_actions,
    legal_chankan_reaction_actions,
    legal_chankan_ron_actions,
    legal_discard_actions,
    legal_kakan_actions,
    legal_kita_actions,
    legal_kita_reaction_actions,
    legal_kita_ron_actions,
    legal_reaction_actions,
    legal_riichi_actions,
    legal_ron_actions,
    legal_sandbox_actions,
    legal_tsumo_actions,
    next_round_sandbox_environment,
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
        self.assertEqual(len(first.wall), 70)
        self.assertEqual(len(first.dead_wall), 14)
        self.assertEqual(first.dora_indicators, first.dead_wall[:1])
        self.assertEqual(first.points, (SANDBOX_INITIAL_POINTS,) * 4)
        self.assertEqual(first.riichi_sticks, 0)
        self.assertEqual(first.honba, 0)
        self.assertEqual(first.dealer_seat, 0)
        self.assertEqual(first.round_wind, TileType.parse("E"))
        self.assertIsNone(first.drawn_tile)
        self.assertFalse(first.rinshan_draw)
        self.assertFalse(first.last_draw_was_final_live_wall)
        self.assertEqual(first.to_payload()["kind"], SANDBOX_ENVIRONMENT_KIND)
        self.assertEqual(first.to_payload()["points"], [SANDBOX_INITIAL_POINTS] * 4)
        self.assertEqual(first.to_payload()["riichi_sticks"], 0)
        self.assertEqual(first.to_payload()["honba"], 0)
        self.assertEqual(first.to_payload()["dealer_seat"], 0)
        self.assertEqual(first.to_payload()["round_wind"], "E")
        self.assertFalse(first.to_payload()["rinshan_draw"])
        self.assertFalse(first.to_payload()["last_draw_was_final_live_wall"])
        self.assertEqual(first.to_payload()["dead_wall_remaining"], 14)
        self.assertEqual(
            first.to_payload()["dora_indicators"],
            [first.dora_indicators[0].notation],
        )
        self.assertEqual(first.to_payload()["double_riichi_seats"], [])
        self.assertEqual(first.to_payload()["ippatsu_seats"], [])
        self.assertEqual(first.to_payload()["winning_ippatsu_seats"], [])
        self.assertEqual(first.to_payload()["winning_rinshan_seats"], [])
        self.assertEqual(first.to_payload()["winning_yaku"], [])
        self.assertEqual(first.to_payload()["winning_yaku_by_seat"], [])
        self.assertEqual(first.to_payload()["terminal_rewards"], [])
        self.assertEqual(first.to_payload()["terminal_point_deltas"], [])
        self.assertEqual(first.to_payload()["exhaustive_draw_tenpai_seats"], [])
        self.assertEqual(first.to_payload()["exhaustive_draw_noten_seats"], [])

    def test_double_riichi_state_requires_riichi_state(self) -> None:
        with self.assertRaisesRegex(ValueError, "double riichi seats must also be riichi"):
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(),
                hands=((), (), (), ()),
                double_riichi_seats=(0,),
            )

    def test_draw_legal_actions_and_discard_transition(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-4p", seed="transition")

        drawn = draw_for_current_seat(state)
        actions = legal_discard_actions(drawn)
        next_state, discard = apply_discard_action(drawn, actions[0])

        self.assertEqual(drawn.hand_sizes()[0], 14)
        self.assertFalse(drawn.rinshan_draw)
        self.assertGreaterEqual(len(actions), 1)
        self.assertEqual(actions[0].kind.value, "discard")
        self.assertIsInstance(discard, Tile)
        self.assertEqual(next_state.current_seat, 1)
        self.assertEqual(next_state.turn, 1)
        self.assertEqual(next_state.hand_sizes(), [13, 13, 13, 13])
        self.assertIsNone(next_state.drawn_tile)
        self.assertFalse(next_state.rinshan_draw)
        self.assertFalse(drawn.last_draw_was_final_live_wall)
        self.assertFalse(next_state.last_draw_was_final_live_wall)
        self.assertEqual(next_state.pending_discard, discard)
        self.assertEqual(next_state.pending_discard_seat, 0)
        self.assertEqual(next_state.pending_reaction_seats, (1, 2, 3))
        self.assertEqual(next_state.discards[0], (discard,))
        self.assertEqual(next_state.to_payload()["discards"][0], [discard.notation])
        self.assertEqual(next_state.to_payload()["temporary_furiten_seats"], [])
        self.assertEqual(next_state.to_payload()["riichi_seats"], [])
        self.assertEqual(next_state.to_payload()["riichi_furiten_seats"], [])

        with self.assertRaisesRegex(ValueError, "pending reactions"):
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
        self.assertEqual(drawn.winning_yaku, ("menzen_tsumo", "yakuhai"))
        self.assertEqual(drawn.winning_yaku_by_seat, ((0, ("menzen_tsumo", "yakuhai")),))
        self.assertEqual(drawn.winning_rinshan_seats, ())
        self.assertEqual(drawn.terminal_rewards, (1.0, -1 / 3, -1 / 3, -1 / 3))
        self.assertEqual(drawn.terminal_point_deltas, (3000, -1000, -1000, -1000))
        self.assertEqual(drawn.terminal_score_estimates[0].yaku_han, 2)
        self.assertEqual(drawn.terminal_score_estimates[0].bonus_han, 0)
        self.assertEqual(drawn.terminal_score_estimates[0].han, 2)
        self.assertEqual(drawn.to_payload()["terminal_rewards"], [1.0, -1 / 3, -1 / 3, -1 / 3])
        self.assertEqual(
            drawn.to_payload()["terminal_point_deltas"],
            [3000, -1000, -1000, -1000],
        )
        self.assertEqual(drawn.to_payload()["winning_yaku"], ["menzen_tsumo", "yakuhai"])
        self.assertEqual(
            drawn.to_payload()["winning_yaku_by_seat"],
            [{"seat": 0, "yaku": ["menzen_tsumo", "yakuhai"]}],
        )
        self.assertEqual(drawn.to_payload()["winning_rinshan_seats"], [])
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
        self.assertEqual(terminal.winning_rinshan_seats, ())
        self.assertEqual(terminal.winning_yaku, ("menzen_tsumo", "yakuhai"))
        self.assertEqual(terminal.terminal_rewards, (1.0, -1 / 3, -1 / 3, -1 / 3))
        self.assertEqual(terminal.terminal_point_deltas, (3000, -1000, -1000, -1000))

    def test_haitei_yaku_allows_open_last_live_wall_tsumo(self) -> None:
        chi = Meld(
            ActionKind.CHI,
            _tiles("1m 2m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("6m"),),
            hands=(
                _tiles("1p 2p 3p 1s 2s 3s 4m 5m E E"),
                (),
                (),
                (),
            ),
            melds=((chi,), (), (), ()),
            turn=17,
        )

        drawn = draw_for_current_seat(state)
        tsumo = Action(ActionKind.TSUMO)

        self.assertEqual(drawn.wall, ())
        self.assertTrue(drawn.last_draw_was_final_live_wall)
        self.assertEqual(legal_tsumo_actions(drawn), (tsumo,))

        terminal = apply_tsumo_action(drawn, tsumo)
        estimate = terminal.terminal_score_estimates[0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winning_yaku, ("haitei",))
        self.assertEqual(terminal.winning_yaku_by_seat, ((0, ("haitei",)),))
        self.assertEqual(estimate.yaku_han, 1)
        self.assertEqual(estimate.han, 1)

    def test_houtei_yaku_allows_last_live_wall_ron(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("6m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E S S"),
                _tiles("1p 2p 3p 1s 2s 3s 4m 5m E E 7p 8p 9p"),
                (),
                (),
            ),
            turn=17,
        )
        drawn = draw_for_current_seat(state)

        pending, discard = apply_discard_action(drawn, Action.discard("6m", tsumogiri=True))
        ron = Action(ActionKind.RON, TileType.parse("6m"))

        self.assertEqual(discard, Tile.parse("6m"))
        self.assertEqual(pending.wall, ())
        self.assertTrue(pending.last_draw_was_final_live_wall)
        self.assertEqual(legal_ron_actions(pending, seat=1), (ron,))

        terminal = apply_ron_action(pending, seat=1, action=ron)
        estimate = terminal.terminal_score_estimates[0]

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winning_yaku, ("houtei",))
        self.assertEqual(terminal.winning_yaku_by_seat, ((1, ("houtei",)),))
        self.assertEqual(estimate.yaku_han, 1)
        self.assertEqual(estimate.han, 1)

    def test_houtei_requires_last_live_wall_draw_discard(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("6m 1m 2m 3m 1p 2p 3p 1s 2s 3s E E S"),
                _tiles("1p 2p 3p 1s 2s 3s 4m 5m E E 7p 8p 9p"),
                (),
                (),
            ),
            needs_discard=True,
        )

        pending, _discard = apply_discard_action(state, Action.discard("6m"))

        self.assertEqual(pending.wall, ())
        self.assertFalse(pending.last_draw_was_final_live_wall)
        self.assertEqual(legal_ron_actions(pending, seat=1), ())

    def test_tsumo_yaku_does_not_duplicate_drawn_pair_tile(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s 4m 5m 6m E E"),
                (),
                (),
                (),
            ),
            drawn_tile=Tile.parse("E"),
        )

        terminal = apply_tsumo_action(state, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winning_yaku, ("menzen_tsumo",))
        self.assertEqual(estimate.yaku_han, 1)
        self.assertEqual(estimate.han, 1)

    def test_rinshan_tsumo_metadata_marks_replacement_draw_winner(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 5m"),
                (),
                (),
                (),
            ),
            drawn_tile=Tile.parse("5m"),
            rinshan_draw=True,
        )

        terminal = apply_tsumo_action(state, Action(ActionKind.TSUMO))

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winner_seat, 0)
        self.assertTrue(terminal.rinshan_draw)
        self.assertEqual(terminal.winning_rinshan_seats, (0,))
        self.assertEqual(terminal.winning_yaku, ("menzen_tsumo", "rinshan", "yakuhai"))
        self.assertEqual(terminal.to_payload()["winning_rinshan_seats"], [0])

    def test_rinshan_draw_requires_drawn_tile(self) -> None:
        with self.assertRaisesRegex(ValueError, "rinshan draw requires a drawn tile"):
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(),
                hands=((), (), (), ()),
                rinshan_draw=True,
            )

    def test_open_meld_tsumo_uses_melds_for_standard_shape(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
            melds=((pon,), (), (), ()),
        )

        drawn = draw_for_current_seat(state)
        tsumo_actions = legal_tsumo_actions(drawn)
        terminal = apply_tsumo_action(drawn, tsumo_actions[0])

        self.assertEqual(tsumo_actions, (Action(ActionKind.TSUMO),))
        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("yakuhai",))
        self.assertEqual(terminal.winner_seats, (0,))

    def test_open_meld_ron_uses_melds_for_standard_shape(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                _tiles("1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
            melds=((), (pon,), (), ()),
        )

        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        ron_actions = legal_ron_actions(reaction_state, seat=1)
        terminal = apply_ron_action(reaction_state, seat=1, action=ron_actions[0])

        self.assertEqual(ron_actions, (Action(ActionKind.RON, TileType.parse("5m")),))
        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winner_seats, (1,))
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("yakuhai",))

    def test_rinshan_tsumo_after_ankan_uses_kan_meld_for_standard_shape(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            dead_wall=_tiles("1m 2m 5m"),
            dora_indicators=(Tile.parse("1m"),),
            hands=(
                _tiles("3m 3m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        after_kan, _meld = apply_ankan_action(drawn, legal_ankan_actions(drawn)[0])

        tsumo_actions = legal_tsumo_actions(after_kan)
        terminal = apply_tsumo_action(after_kan, tsumo_actions[0])

        self.assertTrue(after_kan.rinshan_draw)
        self.assertEqual(tsumo_actions, (Action(ActionKind.TSUMO),))
        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("menzen_tsumo", "rinshan", "yakuhai"))
        self.assertEqual(terminal.winning_rinshan_seats, (0,))
        self.assertEqual(terminal.to_payload()["winning_rinshan_seats"], [0])

    def test_riichi_declaration_is_legal_after_draw_when_discard_leaves_tenpai(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        riichi_actions = legal_riichi_actions(drawn)
        turn_actions = legal_sandbox_actions(drawn)
        declared = apply_riichi_action(drawn, riichi_actions[0])
        after_discard, discard = apply_discard_action(declared, Action.discard("9s"))

        self.assertEqual(riichi_actions, (Action(ActionKind.RIICHI),))
        self.assertIn(Action(ActionKind.RIICHI), turn_actions)
        self.assertEqual(declared.riichi_seats, (0,))
        self.assertEqual(declared.double_riichi_seats, (0,))
        self.assertEqual(declared.riichi_pending_discard_seats, (0,))
        self.assertEqual(declared.ippatsu_seats, (0,))
        self.assertEqual(
            declared.points,
            (
                SANDBOX_INITIAL_POINTS - RIICHI_DEPOSIT_POINTS,
                SANDBOX_INITIAL_POINTS,
                SANDBOX_INITIAL_POINTS,
                SANDBOX_INITIAL_POINTS,
            ),
        )
        self.assertEqual(declared.riichi_sticks, 1)
        self.assertEqual(declared.to_payload()["riichi_seats"], [0])
        self.assertEqual(declared.to_payload()["double_riichi_seats"], [0])
        self.assertEqual(declared.to_payload()["riichi_pending_discard_seats"], [0])
        self.assertEqual(declared.to_payload()["ippatsu_seats"], [0])
        self.assertEqual(declared.to_payload()["riichi_sticks"], 1)
        self.assertIn(Action.discard("5m"), legal_discard_actions(declared))
        self.assertEqual(legal_riichi_actions(declared), ())
        self.assertEqual(discard, Tile.parse("9s"))
        self.assertEqual(after_discard.riichi_seats, (0,))
        self.assertEqual(after_discard.double_riichi_seats, (0,))
        self.assertEqual(after_discard.riichi_pending_discard_seats, ())
        self.assertEqual(after_discard.ippatsu_seats, (0,))
        self.assertEqual(after_discard.points, declared.points)
        self.assertEqual(after_discard.riichi_sticks, 1)
        self.assertEqual(after_discard.pending_discard, Tile.parse("9s"))

    def test_later_turn_riichi_declaration_is_not_double_riichi(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
            turn=4,
        )

        drawn = draw_for_current_seat(state)
        declared = apply_riichi_action(drawn, Action(ActionKind.RIICHI))

        self.assertEqual(declared.riichi_seats, (0,))
        self.assertEqual(declared.double_riichi_seats, ())
        self.assertEqual(declared.to_payload()["double_riichi_seats"], [])

    def test_riichi_after_prior_tile_call_is_not_double_riichi(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
            melds=(
                (),
                (
                    Meld(
                        ActionKind.PON,
                        _tiles("C C C"),
                        called_tile=Tile.parse("C"),
                        from_seat=2,
                    ),
                ),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        declared = apply_riichi_action(drawn, Action(ActionKind.RIICHI))

        self.assertEqual(declared.riichi_seats, (0,))
        self.assertEqual(declared.double_riichi_seats, ())

    def test_sanma_riichi_after_prior_kita_is_not_double_riichi(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("E"),),
            hands=(
                _tiles("1p 2p 3p 4p 5p 6p 7p 8p 9p 1s 2s 3s E"),
                (),
                (),
            ),
            kita_tiles=((Tile.parse("N"),), (), ()),
        )

        drawn = draw_for_current_seat(state)
        declared = apply_riichi_action(drawn, Action(ActionKind.RIICHI))

        self.assertEqual(declared.riichi_seats, (0,))
        self.assertEqual(declared.double_riichi_seats, ())

    def test_riichi_declaration_requires_deposit_points(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
            points=(RIICHI_DEPOSIT_POINTS - 100, 25000, 25000, 25000),
        )

        drawn = draw_for_current_seat(state)

        self.assertEqual(legal_riichi_actions(drawn), ())
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_riichi_action(drawn, Action(ActionKind.RIICHI))

    def test_riichi_sticks_transfer_to_ron_winner(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 9s"),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        declared = apply_riichi_action(drawn, Action(ActionKind.RIICHI))
        reaction_state, _discard = apply_discard_action(declared, Action.discard("9s"))
        terminal = apply_ron_action(
            reaction_state,
            seat=1,
            action=Action(ActionKind.RON, TileType.parse("9s")),
        )

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.riichi_sticks, 0)
        self.assertEqual(
            terminal.points,
            (
                SANDBOX_INITIAL_POINTS - RIICHI_DEPOSIT_POINTS - 1000,
                SANDBOX_INITIAL_POINTS + RIICHI_DEPOSIT_POINTS + 1000,
                SANDBOX_INITIAL_POINTS,
                SANDBOX_INITIAL_POINTS,
            ),
        )
        self.assertEqual(terminal.to_payload()["riichi_sticks"], 0)
        self.assertEqual(
            terminal.terminal_point_deltas,
            (-1000, RIICHI_DEPOSIT_POINTS + 1000, 0, 0),
        )
        self.assertEqual(
            terminal.to_payload()["terminal_point_deltas"],
            [-1000, RIICHI_DEPOSIT_POINTS + 1000, 0, 0],
        )

    def test_honba_bonus_applies_to_ron_point_ledger(self) -> None:
        honba = 2
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
            points=(25000, 25000, 25000, 25000),
            honba=honba,
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        terminal = apply_ron_action(
            reaction_state,
            seat=1,
            action=Action(ActionKind.RON, TileType.parse("5m")),
        )
        payment = 1000 + honba * HONBA_RON_POINTS

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.honba, honba)
        self.assertEqual(
            terminal.points,
            (25000 - payment, 25000 + payment, 25000, 25000),
        )
        self.assertEqual(terminal.terminal_point_deltas, (-payment, payment, 0, 0))
        self.assertEqual(terminal.to_payload()["terminal_point_deltas"], [-payment, payment, 0, 0])
        self.assertEqual(terminal.to_payload()["honba"], honba)

    def test_honba_bonus_applies_to_tsumo_point_ledger(self) -> None:
        honba = 3
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
            points=(25000, 25000, 25000, 25000),
            honba=honba,
        )
        drawn = draw_for_current_seat(state)
        terminal = apply_tsumo_action(drawn, Action(ActionKind.TSUMO))
        payment = 1000 + honba * HONBA_TSUMO_POINTS_PER_LOSER

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.honba, honba)
        self.assertEqual(
            terminal.points,
            (25000 + payment * 3, 25000 - payment, 25000 - payment, 25000 - payment),
        )
        self.assertEqual(
            terminal.terminal_point_deltas,
            (payment * 3, -payment, -payment, -payment),
        )
        self.assertEqual(
            terminal.to_payload()["terminal_point_deltas"],
            [payment * 3, -payment, -payment, -payment],
        )
        self.assertEqual(terminal.to_payload()["honba"], honba)

    def test_dealer_ron_uses_oya_payment_estimate(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                (),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
            pending_discard=Tile.parse("5m"),
            pending_discard_seat=0,
            pending_reaction_seats=(1,),
            dealer_seat=1,
        )

        terminal = apply_ron_action(
            state,
            seat=1,
            action=Action(ActionKind.RON, TileType.parse("5m")),
        )
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.terminal_point_deltas, (-1500, 1500, 0, 0))
        self.assertEqual(terminal.points, (23500, 26500, 25000, 25000))
        self.assertTrue(estimate.is_dealer)
        self.assertEqual(estimate.ron_payment, 1500)
        self.assertIsNone(estimate.tsumo_child_payment)
        self.assertIsNone(estimate.tsumo_dealer_payment)
        self.assertTrue(estimate_payload["is_dealer"])
        self.assertEqual(estimate_payload["ron_payment"], 1500)
        self.assertIsNone(estimate_payload["tsumo_child_payment"])
        self.assertIsNone(estimate_payload["tsumo_dealer_payment"])

    def test_nondealer_tsumo_charges_dealer_more_than_children(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                (),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
            current_seat=1,
            dealer_seat=0,
        )

        drawn = draw_for_current_seat(state)
        terminal = apply_tsumo_action(drawn, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.terminal_point_deltas, (-1000, 2000, -500, -500))
        self.assertEqual(terminal.points, (24000, 27000, 24500, 24500))
        self.assertFalse(estimate.is_dealer)
        self.assertEqual(estimate.tsumo_child_payment, 500)
        self.assertEqual(estimate.tsumo_dealer_payment, 1000)
        self.assertEqual(estimate.tsumo_payment_per_loser, 500)
        self.assertFalse(estimate_payload["is_dealer"])
        self.assertEqual(estimate_payload["tsumo_child_payment"], 500)
        self.assertEqual(estimate_payload["tsumo_dealer_payment"], 1000)
        self.assertEqual(estimate_payload["tsumo_payment_per_loser"], 500)

    def test_sanma_tsumo_uses_tsumo_loss_point_estimates(self) -> None:
        nondealer_state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("5p"),),
            hands=(
                (),
                _tiles("1p 2p 3p 1s 2s 3s 7s 8s 9s E E E 5p"),
                (),
            ),
            current_seat=1,
            dealer_seat=0,
        )
        dealer_state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("5p"),),
            hands=(
                _tiles("1p 2p 3p 1s 2s 3s 7s 8s 9s E E E 5p"),
                (),
                (),
            ),
            current_seat=0,
            dealer_seat=0,
        )

        nondealer_terminal = apply_tsumo_action(
            draw_for_current_seat(nondealer_state),
            Action(ActionKind.TSUMO),
        )
        dealer_terminal = apply_tsumo_action(
            draw_for_current_seat(dealer_state),
            Action(ActionKind.TSUMO),
        )
        nondealer_estimate = nondealer_terminal.terminal_score_estimates[0]
        dealer_estimate = dealer_terminal.terminal_score_estimates[0]

        self.assertEqual(nondealer_terminal.terminal_reason, "tsumo")
        self.assertEqual(nondealer_terminal.terminal_point_deltas, (-1000, 1500, -500))
        self.assertEqual(nondealer_terminal.points, (34000, 36500, 34500))
        self.assertFalse(nondealer_estimate.is_dealer)
        self.assertEqual(nondealer_estimate.tsumo_child_payment, 500)
        self.assertEqual(nondealer_estimate.tsumo_dealer_payment, 1000)
        self.assertEqual(dealer_terminal.terminal_reason, "tsumo")
        self.assertEqual(dealer_terminal.terminal_point_deltas, (2000, -1000, -1000))
        self.assertEqual(dealer_terminal.points, (37000, 34000, 34000))
        self.assertTrue(dealer_estimate.is_dealer)
        self.assertEqual(dealer_estimate.tsumo_child_payment, 1000)
        self.assertIsNone(dealer_estimate.tsumo_dealer_payment)

    def test_visible_dora_counts_as_score_estimate_bonus_han(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            dead_wall=(Tile.parse("9m"),),
            dora_indicators=(Tile.parse("9m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        terminal = apply_tsumo_action(drawn, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(estimate.yaku_han, 2)
        self.assertEqual(estimate.visible_dora_count, 1)
        self.assertEqual(estimate.red_dora_count, 0)
        self.assertEqual(estimate.kita_dora_count, 0)
        self.assertEqual(estimate.bonus_han, 1)
        self.assertEqual(estimate.han, 3)
        self.assertEqual(estimate_payload["visible_dora_count"], 1)
        self.assertEqual(estimate_payload["red_dora_count"], 0)
        self.assertEqual(estimate_payload["bonus_han"], 1)
        self.assertEqual(estimate_payload["han"], 3)

    def test_visible_dora_does_not_duplicate_drawn_tsumo_tile(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            dead_wall=(Tile.parse("4m"),),
            dora_indicators=(Tile.parse("4m"),),
            hands=(
                _tiles("1p 2p 3p 1s 2s 3s 7s 8s 9s E E E 5m 5m"),
                (),
                (),
                (),
            ),
            drawn_tile=Tile.parse("5m"),
        )

        terminal = apply_tsumo_action(state, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(estimate.visible_dora_count, 2)
        self.assertEqual(estimate.bonus_han, 2)

    def test_sanma_visible_dora_wraps_one_and_nine_man_indicators(self) -> None:
        cases = (
            ("1m", "9m"),
            ("9m", "1m"),
        )
        for indicator, dora in cases:
            with self.subTest(indicator=indicator, dora=dora):
                state = SandboxEnvironmentState(
                    ruleset="tenhou-3p",
                    players=3,
                    wall=(),
                    dead_wall=(Tile.parse(indicator),),
                    dora_indicators=(Tile.parse(indicator),),
                    hands=(
                        _tiles(
                            "1p 2p 3p 1s 2s 3s 7s 8s 9s "
                            f"E E E {dora} {dora}"
                        ),
                        (),
                        (),
                    ),
                    drawn_tile=Tile.parse(dora),
                )

                terminal = apply_tsumo_action(state, Action(ActionKind.TSUMO))
                estimate = terminal.terminal_score_estimates[0]

                self.assertEqual(terminal.terminal_reason, "tsumo")
                self.assertEqual(terminal.winning_yaku, ("menzen_tsumo", "yakuhai"))
                self.assertEqual(estimate.visible_dora_count, 2)
                self.assertEqual(estimate.bonus_han, 2)

    def test_red_five_counts_as_score_estimate_bonus_han(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 0m"),
                (),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        terminal = apply_tsumo_action(drawn, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(estimate.yaku_han, 2)
        self.assertEqual(estimate.visible_dora_count, 0)
        self.assertEqual(estimate.red_dora_count, 1)
        self.assertEqual(estimate.kita_dora_count, 0)
        self.assertEqual(estimate.bonus_han, 1)
        self.assertEqual(estimate.han, 3)
        self.assertEqual(estimate_payload["visible_dora_count"], 0)
        self.assertEqual(estimate_payload["red_dora_count"], 1)
        self.assertEqual(estimate_payload["bonus_han"], 1)
        self.assertEqual(estimate_payload["han"], 3)

    def test_thirteen_han_score_estimate_uses_kazoe_yakuman_limit(self) -> None:
        dora_indicators = tuple(Tile.parse("9m") for _index in range(11))
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            dead_wall=dora_indicators,
            dora_indicators=dora_indicators,
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
                (),
            ),
        )

        terminal = apply_tsumo_action(
            draw_for_current_seat(state),
            Action(ActionKind.TSUMO),
        )
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(estimate.yaku_han, 2)
        self.assertEqual(estimate.visible_dora_count, 11)
        self.assertEqual(estimate.bonus_han, 11)
        self.assertEqual(estimate.han, 13)
        self.assertEqual(estimate.limit, "yakuman")
        self.assertEqual(estimate.base_points, 8000)
        self.assertEqual(estimate.tsumo_child_payment, 16000)
        self.assertIsNone(estimate.tsumo_dealer_payment)
        self.assertEqual(estimate_payload["limit"], "yakuman")
        self.assertEqual(estimate_payload["base_points"], 8000)
        self.assertEqual(estimate_payload["tsumo_child_payment"], 16000)

    def test_yakuman_score_estimate_does_not_add_bonus_han(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            dead_wall=(Tile.parse("9m"),),
            dora_indicators=(Tile.parse("9m"),),
            hands=(
                _tiles("1m 9m 9m 1p 9p 1s 9s E S W N P F C"),
                (),
                (),
                (),
            ),
            drawn_tile=Tile.parse("C"),
        )

        terminal = apply_tsumo_action(state, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(terminal.winning_shapes, ("kokushi",))
        self.assertIn("kokushi", terminal.winning_yaku)
        self.assertEqual(estimate.visible_dora_count, 1)
        self.assertEqual(estimate.red_dora_count, 0)
        self.assertEqual(estimate.kita_dora_count, 0)
        self.assertEqual(estimate.bonus_han, 0)
        self.assertEqual(estimate.han, 13)
        self.assertEqual(estimate.limit, "yakuman")
        self.assertEqual(estimate_payload["visible_dora_count"], 1)
        self.assertEqual(estimate_payload["bonus_han"], 0)
        self.assertEqual(estimate_payload["han"], 13)

    def test_next_round_after_dealer_tsumo_repeats_dealer_and_increments_honba(self) -> None:
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
            points=(26000, 24000, 25000, 25000),
            honba=2,
            dealer_seat=0,
        )
        terminal = draw_for_current_seat(state, stop_on_tsumo=True)

        next_round = next_round_sandbox_environment(terminal, seed="dealer-repeat")

        self.assertIsNone(next_round.terminal_reason)
        self.assertEqual(next_round.dealer_seat, 0)
        self.assertEqual(next_round.current_seat, 0)
        self.assertEqual(next_round.honba, 3)
        self.assertEqual(next_round.round_wind, TileType.parse("E"))
        self.assertEqual(next_round.points, terminal.points)
        self.assertEqual(next_round.riichi_sticks, 0)
        self.assertEqual(next_round.turn, 0)
        self.assertEqual(next_round.hand_sizes(), [13, 13, 13, 13])
        self.assertEqual(len(next_round.wall), 70)
        self.assertEqual(next_round.discards, ((), (), (), ()))
        self.assertEqual(next_round.to_payload()["dealer_seat"], 0)

    def test_next_round_after_child_ron_rotates_dealer_and_resets_honba(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5m 6p 7p 8s 9s E S"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
            honba=2,
            dealer_seat=0,
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        terminal = apply_ron_action(
            reaction_state,
            seat=1,
            action=legal_ron_actions(reaction_state, seat=1)[0],
        )

        next_round = next_round_sandbox_environment(terminal, seed="dealer-rotate")

        self.assertEqual(next_round.dealer_seat, 1)
        self.assertEqual(next_round.current_seat, 1)
        self.assertEqual(next_round.honba, 0)
        self.assertEqual(next_round.round_wind, TileType.parse("E"))
        self.assertEqual(next_round.points, terminal.points)
        self.assertEqual(next_round.riichi_sticks, 0)

    def test_next_round_after_final_dealer_loss_advances_round_wind(self) -> None:
        terminal = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=((), (), (), ()),
            terminal_reason="ron",
            winner_seat=0,
            winner_seats=(0,),
            points=(28000, 25000, 25000, 22000),
            honba=1,
            dealer_seat=3,
            round_wind=TileType.parse("E"),
        )

        next_round = next_round_sandbox_environment(terminal, seed="south-round")

        self.assertEqual(next_round.dealer_seat, 0)
        self.assertEqual(next_round.current_seat, 0)
        self.assertEqual(next_round.honba, 0)
        self.assertEqual(next_round.round_wind, TileType.parse("S"))
        self.assertEqual(next_round.to_payload()["round_wind"], "S")

    def test_next_round_after_exhaustive_draw_carries_honba_and_riichi_sticks(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            points=(25000, 25000, 25000, 25000),
            riichi_sticks=1,
            honba=2,
            dealer_seat=0,
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E 5m 6m"),
                _tiles("1m 1m 9m 9m 1p 9p 1s 9s E S W P F"),
                _tiles("2m 2m 8m 8m 2p 8p 2s 8s E S W P F"),
                _tiles("3m 3m 7m 7m 3p 7p 3s 7s E S W P F"),
            ),
        )
        terminal = draw_for_current_seat(state)

        next_round = next_round_sandbox_environment(terminal, seed="draw-renchan")

        self.assertEqual(terminal.exhaustive_draw_tenpai_seats, (0,))
        self.assertEqual(next_round.dealer_seat, 0)
        self.assertEqual(next_round.current_seat, 0)
        self.assertEqual(next_round.honba, 3)
        self.assertEqual(next_round.round_wind, TileType.parse("E"))
        self.assertEqual(next_round.riichi_sticks, 1)
        self.assertEqual(next_round.points, terminal.points)

    def test_next_round_after_dealer_noten_draw_rotates_dealer(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            points=(25000, 25000, 25000, 25000),
            honba=1,
            dealer_seat=0,
            hands=(
                _tiles("1m 1m 9m 9m 1p 9p 1s 9s E S W P F"),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E 5m 6m"),
                _tiles("2m 2m 8m 8m 2p 8p 2s 8s E S W P F"),
                _tiles("3m 3m 7m 7m 3p 7p 3s 7s E S W P F"),
            ),
        )
        terminal = draw_for_current_seat(state)

        next_round = next_round_sandbox_environment(terminal, seed="draw-rotate")

        self.assertEqual(terminal.exhaustive_draw_tenpai_seats, (1,))
        self.assertEqual(next_round.dealer_seat, 1)
        self.assertEqual(next_round.current_seat, 1)
        self.assertEqual(next_round.honba, 2)
        self.assertEqual(next_round.round_wind, TileType.parse("E"))

    def test_next_round_rejects_nonterminal_and_artificial_max_turns(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-4p", seed="next-errors")

        with self.assertRaisesRegex(ValueError, "requires a terminal"):
            next_round_sandbox_environment(state, seed="bad")

        terminal = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=((), (), (), ()),
            terminal_reason="max_turns",
        )
        with self.assertRaisesRegex(ValueError, "artificial max-turn"):
            next_round_sandbox_environment(terminal, seed="bad")

    def test_round_wind_yakuhai_uses_state_round_wind(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                (),
                (),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s S S S 5m"),
                (),
            ),
            pending_discard=Tile.parse("5m"),
            pending_discard_seat=0,
            pending_reaction_seats=(2,),
            dealer_seat=0,
            round_wind=TileType.parse("S"),
        )
        ron = Action(ActionKind.RON, TileType.parse("5m"))

        self.assertEqual(legal_ron_actions(state, seat=2), (ron,))

        terminal = apply_ron_action(state, seat=2, action=ron)

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winning_yaku, ("yakuhai",))
        self.assertEqual(terminal.to_payload()["round_wind"], "S")

    def test_ippatsu_window_is_recorded_on_ron_and_tsumo_wins(self) -> None:
        ron_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                (),
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
            pending_discard=Tile.parse("5m"),
            pending_discard_seat=0,
            pending_reaction_seats=(1,),
            riichi_seats=(1,),
            ippatsu_seats=(1,),
        )
        ron_terminal = apply_ron_action(
            ron_state,
            seat=1,
            action=Action(ActionKind.RON, TileType.parse("5m")),
        )
        tsumo_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 5m"),
                (),
                (),
                (),
            ),
            drawn_tile=Tile.parse("5m"),
            riichi_seats=(0,),
            ippatsu_seats=(0,),
        )
        tsumo_terminal = apply_tsumo_action(tsumo_state, Action(ActionKind.TSUMO))

        self.assertEqual(ron_terminal.winning_ippatsu_seats, (1,))
        self.assertEqual(ron_terminal.ippatsu_seats, ())
        self.assertEqual(ron_terminal.to_payload()["winning_ippatsu_seats"], [1])
        self.assertEqual(tsumo_terminal.winning_ippatsu_seats, (0,))
        self.assertEqual(tsumo_terminal.ippatsu_seats, ())
        self.assertEqual(tsumo_terminal.to_payload()["winning_ippatsu_seats"], [0])

    def test_post_riichi_turn_discards_are_locked_to_drawn_tile(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 9s"),
                (),
                (),
                (),
            ),
            drawn_tile=Tile.parse("9s"),
            riichi_seats=(0,),
            ippatsu_seats=(0,),
        )

        actions = legal_discard_actions(state)
        turn_actions = legal_sandbox_actions(state)

        self.assertEqual(actions, (Action.discard("9s", tsumogiri=True),))
        self.assertEqual(turn_actions, actions)
        with self.assertRaisesRegex(ValueError, "post-riichi discard"):
            apply_discard_action(state, Action.discard("5m"))
        with self.assertRaisesRegex(ValueError, "tsumogiri"):
            apply_discard_action(state, Action.discard("9s"))

        after_discard, discard = apply_discard_action(
            state,
            Action.discard("9s", tsumogiri=True),
        )

        self.assertEqual(discard, Tile.parse("9s"))
        self.assertEqual(after_discard.riichi_seats, (0,))
        self.assertEqual(after_discard.riichi_pending_discard_seats, ())
        self.assertEqual(after_discard.ippatsu_seats, ())
        self.assertEqual(after_discard.pending_discard, Tile.parse("9s"))

    def test_riichi_declaration_rejects_open_or_non_tenpai_or_wrong_phase(self) -> None:
        base_hand = _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m")
        open_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(base_hand, (), (), ()),
            melds=(
                (
                    Meld(
                        ActionKind.CHI,
                        _tiles("1m 2m 3m"),
                        called_tile=Tile.parse("3m"),
                        from_seat=3,
                    ),
                ),
                (),
                (),
                (),
            ),
        )
        non_tenpai_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                (),
                (),
                (),
            ),
        )

        with self.assertRaisesRegex(ValueError, "must draw"):
            legal_riichi_actions(open_state)

        open_drawn = draw_for_current_seat(open_state)
        self.assertEqual(legal_riichi_actions(open_drawn), ())
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_riichi_action(open_drawn, Action(ActionKind.RIICHI))

        non_tenpai_drawn = draw_for_current_seat(non_tenpai_state)
        self.assertEqual(legal_riichi_actions(non_tenpai_drawn), ())
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_riichi_action(non_tenpai_drawn, Action(ActionKind.RIICHI))
        with self.assertRaisesRegex(ValueError, "supports riichi"):
            apply_riichi_action(non_tenpai_drawn, Action.pass_())

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
        self.assertEqual(terminal.winning_yaku, ("yakuhai",))
        self.assertEqual(terminal.winning_yaku_by_seat, ((1, ("yakuhai",)),))
        self.assertEqual(terminal.terminal_rewards, (-1.0, 1.0, 0.0, 0.0))

    def test_ron_requires_recognized_sandbox_yaku(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("2m 3m 4m 5p 6p 7p 8s 9s E S W N P"),
                _tiles("1m 1m 1m 2m 3m 4m 2p 3p 4p 2s 3s 4s 5m"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        ron = Action(ActionKind.RON, TileType.parse("5m"))

        self.assertEqual(legal_ron_actions(reaction_state, seat=1), ())
        with self.assertRaisesRegex(ValueError, "no recognized sandbox yaku"):
            apply_ron_action(reaction_state, seat=1, action=ron)

    def test_sanma_north_triplet_is_not_yakuhai_when_used_in_hand(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("5p"),),
            hands=(
                _tiles("1p 1p 1p 2p 3p 4p 5s 6s 7s E S W N"),
                _tiles("N N N 1p 2p 3p 4p 6p 1s 2s 3s 7p 7p"),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5p"))
        ron = Action(ActionKind.RON, TileType.parse("5p"))

        self.assertEqual(legal_ron_actions(reaction_state, seat=1), ())
        with self.assertRaisesRegex(ValueError, "no recognized sandbox yaku"):
            apply_ron_action(reaction_state, seat=1, action=ron)

    def test_riichi_yaku_allows_closed_shape_ron(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("2m 3m 4m 5p 6p 7p 8s 9s E S W N P"),
                _tiles("1m 1m 1m 2m 3m 4m 2p 3p 4p 2s 3s 4s 5m"),
                (),
                (),
            ),
            riichi_seats=(1,),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        ron = Action(ActionKind.RON, TileType.parse("5m"))

        self.assertEqual(legal_ron_actions(reaction_state, seat=1), (ron,))

        terminal = apply_ron_action(reaction_state, seat=1, action=ron)

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("riichi",))

    def test_double_riichi_yaku_replaces_riichi_and_scores_two_han(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                _tiles("1m 2m 3m 7m 8m 9m 1p 2p 3p 1s 2s 3s 5p"),
                (),
                (),
                (),
            ),
            pending_discard=Tile.parse("5p"),
            pending_discard_seat=1,
            pending_reaction_seats=(0,),
            riichi_seats=(0,),
            double_riichi_seats=(0,),
        )
        ron = Action(ActionKind.RON, TileType.parse("5p"))

        self.assertEqual(legal_ron_actions(state, seat=0), (ron,))

        terminal = apply_ron_action(state, seat=0, action=ron)
        estimate = terminal.terminal_score_estimates[0]

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("double_riichi",))
        self.assertEqual(terminal.winning_yaku_by_seat, ((0, ("double_riichi",)),))
        self.assertEqual(estimate.yaku, ("double_riichi",))
        self.assertEqual(estimate.yaku_han, 2)
        self.assertEqual(estimate.bonus_han, 0)
        self.assertEqual(estimate.han, 2)
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["yaku"],
            ["double_riichi"],
        )
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["yaku_han"],
            2,
        )

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

    def test_passing_legal_ron_after_riichi_sets_riichi_furiten(self) -> None:
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
            riichi_seats=(1,),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))

        self.assertEqual(
            legal_ron_actions(reaction_state, seat=1),
            (Action(ActionKind.RON, TileType.parse("5m")),),
        )
        bulk_resolved = pass_pending_discard_reactions(reaction_state)
        self.assertEqual(bulk_resolved.riichi_furiten_seats, (1,))
        self.assertEqual(bulk_resolved.temporary_furiten_seats, ())

        after_ron_pass = apply_reaction_pass_action(
            reaction_state,
            seat=1,
            action=Action.pass_(),
        )
        after_two_passes = apply_reaction_pass_action(after_ron_pass, seat=2)
        resolved = apply_reaction_pass_action(after_two_passes, seat=3)
        next_draw = draw_for_current_seat(resolved)

        self.assertEqual(after_ron_pass.riichi_furiten_seats, (1,))
        self.assertEqual(after_ron_pass.temporary_furiten_seats, ())
        self.assertEqual(after_ron_pass.to_payload()["riichi_seats"], [1])
        self.assertEqual(after_ron_pass.to_payload()["riichi_furiten_seats"], [1])
        self.assertEqual(resolved.riichi_furiten_seats, (1,))
        self.assertEqual(next_draw.current_seat, 1)
        self.assertEqual(next_draw.riichi_furiten_seats, (1,))

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

    def test_riichi_furiten_blocks_ron(self) -> None:
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
            riichi_seats=(1,),
            riichi_furiten_seats=(1,),
        )

        self.assertEqual(legal_ron_actions(state, seat=1), ())
        self.assertEqual(legal_reaction_actions(state, seat=1), (Action.pass_(),))
        with self.assertRaisesRegex(ValueError, "riichi furiten"):
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
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s W W W 5m"),
                (),
            ),
            riichi_sticks=1,
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))

        terminal = apply_ron_actions(
            reaction_state,
            (
                (2, Action(ActionKind.RON, TileType.parse("5m"))),
                (1, Action(ActionKind.RON, TileType.parse("5m"))),
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
        self.assertEqual(terminal.winning_yaku, ("yakuhai",))
        self.assertEqual(
            terminal.winning_yaku_by_seat,
            ((1, ("yakuhai",)), (2, ("yakuhai",))),
        )
        self.assertEqual(terminal.terminal_rewards, (-2.0, 1.0, 1.0, 0.0))
        self.assertEqual(terminal.riichi_sticks, 0)
        self.assertEqual(terminal.terminal_point_deltas, (-2000, 2000, 1000, 0))
        self.assertEqual(
            tuple(estimate.seat for estimate in terminal.terminal_score_estimates),
            (1, 2),
        )
        self.assertEqual(
            tuple(
                estimate.riichi_stick_points
                for estimate in terminal.terminal_score_estimates
            ),
            (1000, 0),
        )
        payload = terminal.to_payload()
        self.assertEqual(payload["winner_seats"], [1, 2])
        self.assertEqual(payload["riichi_sticks"], 0)
        self.assertEqual(payload["terminal_point_deltas"], [-2000, 2000, 1000, 0])
        self.assertEqual(
            payload["winning_shapes_by_seat"],
            [{"seat": 1, "shapes": ["standard"]}, {"seat": 2, "shapes": ["standard"]}],
        )
        self.assertEqual(
            payload["winning_yaku_by_seat"],
            [{"seat": 1, "yaku": ["yakuhai"]}, {"seat": 2, "yaku": ["yakuhai"]}],
        )

    def test_guest_wind_triplet_is_not_yakuhai(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5p"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("W W W 1m 2m 3m 1s 2s 3s 4p 6p 7p 7p"),
                (),
                (),
            ),
        )

        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5p"))
        ron = Action(ActionKind.RON, TileType.parse("5p"))

        self.assertEqual(legal_ron_actions(reaction_state, seat=1), ())
        with self.assertRaisesRegex(ValueError, "no recognized sandbox yaku"):
            apply_ron_action(reaction_state, seat=1, action=ron)

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

    def test_riichi_seat_cannot_call_pending_discard(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=(
                (),
                _tiles("1m 2m 4m 5m 6p 7p 8p 1s 2s 3s E S W"),
                (),
                (),
            ),
            pending_discard=Tile.parse("3m"),
            pending_discard_seat=0,
            pending_reaction_seats=(1,),
            riichi_seats=(1,),
        )
        chi = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("1m"), Tile.parse("2m")),
        )

        self.assertEqual(legal_call_actions(state, seat=1), ())
        self.assertEqual(legal_reaction_actions(state, seat=1), (Action.pass_(),))
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_call_action(state, seat=1, action=chi)

    def test_sanma_call_reactions_disallow_chi_but_keep_pon_and_minkan(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(),
            hands=(
                (),
                _tiles("1p 2p 3p 3p 3p 4p 5p 6p 1s 2s 3s E S"),
                (),
            ),
            pending_discard=Tile.parse("3p"),
            pending_discard_seat=0,
            pending_reaction_seats=(1, 2),
        )
        chi = Action(
            ActionKind.CHI,
            TileType.parse("3p"),
            consumed=(Tile.parse("1p"), Tile.parse("2p")),
        )
        pon = Action(
            ActionKind.PON,
            TileType.parse("3p"),
            consumed=(Tile.parse("3p"), Tile.parse("3p")),
        )
        minkan = Action(
            ActionKind.MINKAN,
            TileType.parse("3p"),
            consumed=(Tile.parse("3p"), Tile.parse("3p"), Tile.parse("3p")),
        )

        call_actions = legal_call_actions(state, seat=1)

        self.assertNotIn(chi, call_actions)
        self.assertEqual(call_actions, (pon, minkan))
        self.assertEqual(legal_reaction_actions(state, seat=1), (pon, minkan, Action.pass_()))
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_call_action(state, seat=1, action=chi)

    def test_sanma_kita_is_not_legal_immediately_after_pon(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(),
            hands=(
                (),
                _tiles("1p 2p 3p 3p 4p 5p 6p 1s 2s 3s E S N"),
                (),
            ),
            pending_discard=Tile.parse("3p"),
            pending_discard_seat=0,
            pending_reaction_seats=(1,),
        )
        pon = Action(
            ActionKind.PON,
            TileType.parse("3p"),
            consumed=(Tile.parse("3p"), Tile.parse("3p")),
        )
        kita = Action(
            ActionKind.KITA,
            TileType.parse("N"),
            consumed=(Tile.parse("N"),),
        )

        called, meld = apply_call_action(state, seat=1, action=pon)
        turn_actions = legal_sandbox_actions(called)

        self.assertEqual(meld.kind, ActionKind.PON)
        self.assertEqual(called.current_seat, 1)
        self.assertTrue(called.needs_discard)
        self.assertNotIn(kita, turn_actions)
        self.assertEqual(turn_actions, legal_discard_actions(called))
        with self.assertRaisesRegex(ValueError, "current seat must draw before kita"):
            legal_kita_actions(called)

    def test_calls_cancel_active_ippatsu_windows(self) -> None:
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
            riichi_seats=(2,),
            ippatsu_seats=(2,),
        )
        drawn = draw_for_current_seat(state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("3m"))
        chi = Action(
            ActionKind.CHI,
            TileType.parse("3m"),
            consumed=(Tile.parse("1m"), Tile.parse("2m")),
        )

        called, _meld = apply_call_action(reaction_state, seat=1, action=chi)

        self.assertEqual(called.ippatsu_seats, ())
        self.assertEqual(called.to_payload()["ippatsu_seats"], [])

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
            wall=(Tile.parse("8s"),),
            dead_wall=_tiles("1m 2m 9s"),
            dora_indicators=(Tile.parse("1m"),),
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
        self.assertTrue(called.rinshan_draw)
        self.assertTrue(called.to_payload()["rinshan_draw"])
        self.assertEqual(called.wall, ())
        self.assertEqual(called.dead_wall, _tiles("1m 2m"))
        self.assertEqual(called.dora_indicators, _tiles("1m 2m"))
        self.assertEqual(called.hand_sizes()[1], 11)
        self.assertEqual(meld.kind, ActionKind.MINKAN)
        self.assertIn(Action.discard("9s"), legal_discard_actions(called))

    def test_ankan_action_uses_simple_replacement_draw(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            dead_wall=_tiles("1m 2m 8s"),
            dora_indicators=(Tile.parse("1m"),),
            hands=(
                _tiles("3m 3m 3m 1p 2p 3p 4p 5p 6p 7s 8s E S"),
                (),
                (),
                (),
            ),
            riichi_seats=(2,),
            ippatsu_seats=(2,),
        )
        drawn = draw_for_current_seat(state)
        ankan = Action(
            ActionKind.ANKAN,
            TileType.parse("3m"),
            consumed=(
                Tile.parse("3m"),
                Tile.parse("3m"),
                Tile.parse("3m"),
                Tile.parse("3m"),
            ),
        )

        self.assertEqual(legal_ankan_actions(drawn), (ankan,))
        self.assertIn(ankan, legal_sandbox_actions(drawn))

        after_kan, meld = apply_ankan_action(drawn, ankan)

        self.assertEqual(after_kan.current_seat, 0)
        self.assertEqual(after_kan.turn, 0)
        self.assertFalse(after_kan.needs_discard)
        self.assertEqual(after_kan.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_kan.rinshan_draw)
        self.assertTrue(after_kan.to_payload()["rinshan_draw"])
        self.assertEqual(after_kan.wall, ())
        self.assertEqual(after_kan.dead_wall, _tiles("1m 2m"))
        self.assertEqual(after_kan.dora_indicators, _tiles("1m 2m"))
        self.assertEqual(after_kan.hand_sizes()[0], 11)
        self.assertEqual(
            sum(tile.type == TileType.parse("3m") for tile in after_kan.hands[0]),
            0,
        )
        self.assertEqual(after_kan.ippatsu_seats, ())
        self.assertEqual(after_kan.to_payload()["ippatsu_seats"], [])
        self.assertEqual(meld.kind, ActionKind.ANKAN)
        self.assertEqual(meld.tiles, ankan.consumed)
        self.assertIsNone(meld.called_tile)
        self.assertIsNone(meld.from_seat)
        self.assertEqual(after_kan.melds[0], (meld,))
        self.assertIn(Action.discard("8s"), legal_discard_actions(after_kan))

    def test_ankan_rejects_without_draw_or_invalid_post_riichi_exception(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            hands=(
                _tiles("3m 3m 3m 1p 2p 3p 4p 5p 6p 7s 8s E S"),
                (),
                (),
                (),
            ),
        )
        ankan = Action(
            ActionKind.ANKAN,
            TileType.parse("3m"),
            consumed=(
                Tile.parse("3m"),
                Tile.parse("3m"),
                Tile.parse("3m"),
                Tile.parse("3m"),
            ),
        )

        with self.assertRaisesRegex(ValueError, "must draw"):
            legal_ankan_actions(state)

        riichi_drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(Tile.parse("3m"),),
                hands=state.hands,
                riichi_seats=(0,),
                ippatsu_seats=(0,),
            )
        )

        self.assertEqual(legal_ankan_actions(riichi_drawn), ())
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_ankan_action(riichi_drawn, ankan)
        with self.assertRaisesRegex(ValueError, "supports closed kan"):
            apply_ankan_action(riichi_drawn, Action.pass_())

    def test_post_riichi_ankan_allows_drawn_quad_when_waits_are_preserved(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("1m"),),
            dead_wall=_tiles("2m 3m 8s"),
            dora_indicators=(Tile.parse("2m"),),
            hands=(
                _tiles("1m 1m 1m 2p 3p 4p 2s 3s 4s E E E 5m"),
                (),
                (),
                (),
            ),
            riichi_seats=(0,),
            ippatsu_seats=(0,),
        )
        drawn = draw_for_current_seat(state)
        ankan = Action(
            ActionKind.ANKAN,
            TileType.parse("1m"),
            consumed=(
                Tile.parse("1m"),
                Tile.parse("1m"),
                Tile.parse("1m"),
                Tile.parse("1m"),
            ),
        )

        self.assertEqual(legal_discard_actions(drawn), (Action.discard("1m", tsumogiri=True),))
        self.assertEqual(legal_ankan_actions(drawn), (ankan,))

        after_kan, meld = apply_ankan_action(drawn, ankan)

        self.assertEqual(meld.kind, ActionKind.ANKAN)
        self.assertEqual(after_kan.melds[0], (meld,))
        self.assertEqual(after_kan.hand_sizes()[0], 11)
        self.assertEqual(after_kan.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_kan.rinshan_draw)
        self.assertEqual(after_kan.ippatsu_seats, ())
        self.assertEqual(
            legal_discard_actions(after_kan),
            (Action.discard("8s", tsumogiri=True),),
        )

    def test_post_riichi_ankan_blocks_drawn_quad_that_changes_waits(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("8m"),),
            dead_wall=_tiles("2m 3m 8s"),
            dora_indicators=(Tile.parse("2m"),),
            hands=(
                _tiles("2m 2m 2m 7m 8m 8m 8m 9m 4p 4p 4p 7s 7s"),
                (),
                (),
                (),
            ),
            riichi_seats=(0,),
            ippatsu_seats=(0,),
        )
        drawn = draw_for_current_seat(state)
        ankan = Action(
            ActionKind.ANKAN,
            TileType.parse("8m"),
            consumed=(
                Tile.parse("8m"),
                Tile.parse("8m"),
                Tile.parse("8m"),
                Tile.parse("8m"),
            ),
        )

        self.assertEqual(legal_discard_actions(drawn), (Action.discard("8m", tsumogiri=True),))
        self.assertEqual(legal_ankan_actions(drawn), ())
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_ankan_action(drawn, ankan)

    def test_ankan_cancels_active_ippatsu_windows(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            dead_wall=_tiles("1m 2m 8s"),
            dora_indicators=(Tile.parse("1m"),),
            hands=(
                _tiles("3m 3m 3m 1p 2p 3p 4p 5p 6p 7s 8s E S"),
                (),
                (),
                (),
            ),
            riichi_seats=(1,),
            ippatsu_seats=(1,),
        )
        drawn = draw_for_current_seat(state)
        ankan = legal_ankan_actions(drawn)[0]

        after_kan, _meld = apply_ankan_action(drawn, ankan)

        self.assertEqual(after_kan.ippatsu_seats, ())

    def test_ankan_opens_kokushi_chankan_window_before_replacement_draw(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("1m"),),
            dead_wall=_tiles("2m 3m 5s"),
            dora_indicators=(Tile.parse("2m"),),
            hands=(
                _tiles("1m 1m 1m 1p 2p 3p 1s 2s 3s E E E 5m"),
                _tiles("9m 1p 9p 1s 9s E E S W N P F C"),
                (),
                (),
            ),
            riichi_seats=(1,),
            ippatsu_seats=(1,),
        )
        drawn = draw_for_current_seat(state)
        ankan = legal_ankan_actions(drawn)[0]

        pending, meld = apply_ankan_action(drawn, ankan)
        ron = Action(ActionKind.RON, TileType.parse("1m"))

        self.assertEqual(meld.kind, ActionKind.ANKAN)
        self.assertIsNone(pending.drawn_tile)
        self.assertFalse(pending.rinshan_draw)
        self.assertEqual(pending.dead_wall, _tiles("2m 3m 5s"))
        self.assertEqual(pending.dora_indicators, _tiles("2m"))
        self.assertEqual(pending.pending_chankan_tile, Tile.parse("1m"))
        self.assertEqual(pending.pending_chankan_seat, 0)
        self.assertEqual(pending.pending_chankan_kind, ActionKind.ANKAN)
        self.assertEqual(pending.to_payload()["pending_chankan_kind"], "ankan")
        self.assertEqual(pending.pending_reaction_seats, (1,))
        self.assertEqual(pending.ippatsu_seats, (1,))
        self.assertEqual(legal_chankan_ron_actions(pending, seat=1), (ron,))

        terminal = apply_ron_action(pending, seat=1, action=ron)

        self.assertEqual(terminal.terminal_reason, "chankan")
        self.assertEqual(terminal.winner_seats, (1,))
        self.assertEqual(terminal.winning_shapes, ("kokushi",))
        self.assertEqual(terminal.winning_yaku, ("kokushi", "riichi", "ippatsu", "chankan"))
        self.assertEqual(terminal.winning_ippatsu_seats, (1,))
        self.assertEqual(terminal.ippatsu_seats, ())
        self.assertIsNone(terminal.pending_chankan_kind)

    def test_ankan_chankan_rejects_non_kokushi_ron_shape(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            dead_wall=_tiles("1m 2m 8s"),
            dora_indicators=(Tile.parse("1m"),),
            hands=(
                _tiles("3m 3m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                _tiles("1m 2m 1p 2p 3p 1s 2s 3s E E E 5m 5m"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)

        after_kan, _meld = apply_ankan_action(drawn, legal_ankan_actions(drawn)[0])

        self.assertIsNone(after_kan.pending_chankan_tile)
        self.assertIsNone(after_kan.pending_chankan_kind)
        self.assertTrue(after_kan.rinshan_draw)
        self.assertEqual(after_kan.drawn_tile, Tile.parse("8s"))

    def test_kakan_action_promotes_pon_and_uses_simple_replacement_draw(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            dead_wall=_tiles("1m 2m 8s"),
            dora_indicators=(Tile.parse("1m"),),
            hands=(
                _tiles("1p 2p 3p 4p 5p 6p 7s 8s E S"),
                (),
                (),
                (),
            ),
            melds=((pon,), (), (), ()),
            riichi_seats=(2,),
            ippatsu_seats=(2,),
        )
        drawn = draw_for_current_seat(state)
        kakan = Action(
            ActionKind.KAKAN,
            TileType.parse("3m"),
            consumed=(Tile.parse("3m"),),
        )

        self.assertEqual(legal_kakan_actions(drawn), (kakan,))
        self.assertIn(kakan, legal_sandbox_actions(drawn))

        after_kan, meld = apply_kakan_action(drawn, kakan)

        self.assertEqual(after_kan.current_seat, 0)
        self.assertEqual(after_kan.turn, 0)
        self.assertFalse(after_kan.needs_discard)
        self.assertEqual(after_kan.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_kan.rinshan_draw)
        self.assertEqual(after_kan.wall, ())
        self.assertEqual(after_kan.dead_wall, _tiles("1m 2m"))
        self.assertEqual(after_kan.dora_indicators, _tiles("1m 2m"))
        self.assertEqual(after_kan.hand_sizes()[0], 11)
        self.assertEqual(
            sum(tile.type == TileType.parse("3m") for tile in after_kan.hands[0]),
            0,
        )
        self.assertEqual(after_kan.ippatsu_seats, ())
        self.assertEqual(after_kan.to_payload()["ippatsu_seats"], [])
        self.assertEqual(meld.kind, ActionKind.KAKAN)
        self.assertEqual(meld.tiles, (*pon.tiles, Tile.parse("3m")))
        self.assertEqual(meld.called_tile, Tile.parse("3m"))
        self.assertEqual(meld.from_seat, 3)
        self.assertEqual(after_kan.melds[0], (meld,))
        self.assertIn(Action.discard("8s"), legal_discard_actions(after_kan))

    def test_kakan_opens_chankan_reaction_window_before_replacement_draw(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("3m"),),
            dead_wall=_tiles("1m 2m 8s"),
            dora_indicators=(Tile.parse("1m"),),
            hands=(
                _tiles("1p 2p 3p 4p 5p 6p 7s 8s E S"),
                _tiles("1m 1m 1m 2p 3p 4p 5s 6s 7s E E E 3m"),
                (),
                (),
            ),
            melds=((pon,), (), (), ()),
            points=(25000, 25000, 25000, 25000),
            riichi_seats=(1,),
            ippatsu_seats=(1,),
        )
        drawn = draw_for_current_seat(state)
        kakan = Action(
            ActionKind.KAKAN,
            TileType.parse("3m"),
            consumed=(Tile.parse("3m"),),
        )

        pending, meld = apply_kakan_action(drawn, kakan)
        ron = Action(ActionKind.RON, TileType.parse("3m"))

        self.assertEqual(meld.kind, ActionKind.KAKAN)
        self.assertIsNone(pending.drawn_tile)
        self.assertFalse(pending.rinshan_draw)
        self.assertFalse(pending.needs_discard)
        self.assertEqual(pending.wall, ())
        self.assertEqual(pending.dead_wall, _tiles("1m 2m 8s"))
        self.assertEqual(pending.dora_indicators, _tiles("1m"))
        self.assertEqual(pending.hand_sizes()[0], 10)
        self.assertEqual(pending.pending_chankan_tile, Tile.parse("3m"))
        self.assertEqual(pending.pending_chankan_seat, 0)
        self.assertEqual(pending.pending_chankan_kind, ActionKind.KAKAN)
        self.assertEqual(pending.pending_reaction_seats, (1,))
        self.assertEqual(pending.ippatsu_seats, (1,))
        self.assertEqual(pending.to_payload()["pending_chankan_tile"], "3m")
        self.assertEqual(pending.to_payload()["pending_chankan_seat"], 0)
        self.assertEqual(pending.to_payload()["pending_chankan_kind"], "kakan")
        self.assertEqual(legal_chankan_ron_actions(pending, seat=1), (ron,))
        self.assertEqual(legal_chankan_reaction_actions(pending, seat=1), (ron, Action.pass_()))
        self.assertEqual(legal_sandbox_actions(pending, seat=1), (ron, Action.pass_()))

        terminal = apply_ron_action(pending, seat=1, action=ron)

        self.assertEqual(terminal.terminal_reason, "chankan")
        self.assertEqual(terminal.winner_seat, 1)
        self.assertEqual(terminal.winner_seats, (1,))
        self.assertEqual(terminal.winning_tile, Tile.parse("3m"))
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("riichi", "ippatsu", "chankan", "yakuhai"))
        self.assertEqual(terminal.winning_ippatsu_seats, (1,))
        self.assertEqual(terminal.ippatsu_seats, ())
        self.assertEqual(terminal.terminal_rewards, (-1.0, 1.0, 0.0, 0.0))
        self.assertIsNone(terminal.pending_chankan_tile)
        self.assertIsNone(terminal.pending_chankan_seat)
        self.assertIsNone(terminal.pending_chankan_kind)
        self.assertEqual(terminal.pending_reaction_seats, ())

    def test_chankan_pass_draws_delayed_replacement_tile(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(Tile.parse("3m"),),
                dead_wall=_tiles("1m 2m 8s"),
                dora_indicators=(Tile.parse("1m"),),
                hands=(
                    _tiles("1p 2p 3p 4p 5p 6p 7s 8s E S"),
                    _tiles("1m 1m 1m 2p 3p 4p 5s 6s 7s E E E 3m"),
                    (),
                    (),
                ),
                melds=((pon,), (), (), ()),
                riichi_seats=(1,),
                ippatsu_seats=(1,),
            )
        )
        pending, _meld = apply_kakan_action(drawn, legal_kakan_actions(drawn)[0])

        after_pass = apply_reaction_pass_action(pending, seat=1, action=Action.pass_())

        self.assertIsNone(after_pass.pending_chankan_tile)
        self.assertIsNone(after_pass.pending_chankan_seat)
        self.assertIsNone(after_pass.pending_chankan_kind)
        self.assertEqual(after_pass.pending_reaction_seats, ())
        self.assertEqual(after_pass.temporary_furiten_seats, ())
        self.assertEqual(after_pass.riichi_furiten_seats, (1,))
        self.assertEqual(after_pass.ippatsu_seats, ())
        self.assertEqual(after_pass.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_pass.rinshan_draw)
        self.assertEqual(after_pass.wall, ())
        self.assertEqual(after_pass.dead_wall, _tiles("1m 2m"))
        self.assertEqual(after_pass.dora_indicators, _tiles("1m 2m"))
        self.assertEqual(after_pass.hand_sizes()[0], 11)
        self.assertIn(Action.discard("8s"), legal_discard_actions(after_pass))
        with self.assertRaisesRegex(ValueError, "no pending chankan reaction"):
            legal_chankan_ron_actions(after_pass, seat=1)

    def test_kakan_rejects_without_draw_or_after_riichi(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("9s"),),
            hands=(
                _tiles("3m 1p 2p 3p 4p 5p 6p 7s 8s E"),
                (),
                (),
                (),
            ),
            melds=((pon,), (), (), ()),
        )
        kakan = Action(
            ActionKind.KAKAN,
            TileType.parse("3m"),
            consumed=(Tile.parse("3m"),),
        )

        with self.assertRaisesRegex(ValueError, "must draw"):
            legal_kakan_actions(state)

        no_pon_drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(Tile.parse("3m"),),
                hands=(_tiles("1p 2p 3p 4p 5p 6p 7s 8s E S"), (), (), ()),
            )
        )
        riichi_drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(Tile.parse("9s"),),
                hands=state.hands,
                melds=state.melds,
                riichi_seats=(0,),
                ippatsu_seats=(0,),
            )
        )
        pending = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=state.hands,
            melds=state.melds,
            pending_discard=Tile.parse("7s"),
            pending_discard_seat=1,
            pending_reaction_seats=(0,),
        )

        self.assertEqual(legal_kakan_actions(no_pon_drawn), ())
        self.assertEqual(legal_kakan_actions(riichi_drawn), ())
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_kakan_action(riichi_drawn, kakan)
        with self.assertRaisesRegex(ValueError, "supports added kan"):
            apply_kakan_action(riichi_drawn, Action.pass_())
        with self.assertRaisesRegex(ValueError, "pending reaction"):
            legal_kakan_actions(pending)

    def test_kakan_without_replacement_draw_exhausts_wall(self) -> None:
        pon = Meld(
            ActionKind.PON,
            _tiles("3m 3m 3m"),
            called_tile=Tile.parse("3m"),
            from_seat=3,
        )
        drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(Tile.parse("3m"),),
                dead_wall=(Tile.parse("1m"),),
                dora_indicators=(Tile.parse("1m"),),
                hands=(_tiles("1p 2p 3p 4p 5p 6p 7s 8s E S"), (), (), ()),
                melds=((pon,), (), (), ()),
            )
        )
        kakan = legal_kakan_actions(drawn)[0]

        terminal, meld = apply_kakan_action(drawn, kakan)

        self.assertEqual(meld.kind, ActionKind.KAKAN)
        self.assertEqual(terminal.terminal_reason, "wall_exhausted")
        self.assertIsNone(terminal.drawn_tile)
        self.assertFalse(terminal.rinshan_draw)
        self.assertEqual(terminal.terminal_rewards, (0.0, 0.0, 0.0, 0.0))
        self.assertEqual(terminal.terminal_point_deltas, (0, 0, 0, 0))

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

    def test_wall_exhaustion_with_no_tenpai_uses_neutral_terminal_rewards(self) -> None:
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
        self.assertEqual(terminal.terminal_point_deltas, (0, 0, 0, 0))
        self.assertEqual(terminal.points, (SANDBOX_INITIAL_POINTS,) * 4)
        self.assertEqual(terminal.exhaustive_draw_tenpai_seats, ())
        self.assertEqual(terminal.exhaustive_draw_noten_seats, (0, 1, 2, 3))
        self.assertEqual(terminal.to_payload()["exhaustive_draw_tenpai_seats"], [])
        self.assertEqual(terminal.to_payload()["exhaustive_draw_noten_seats"], [0, 1, 2, 3])

    def test_wall_exhaustion_applies_basic_tenpai_noten_payments(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            points=(25000, 25000, 25000, 25000),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E 5m 6m"),
                _tiles("1m 1m 9m 9m 1p 9p 1s 9s E S W P F"),
                _tiles("2m 2m 8m 8m 2p 8p 2s 8s E S W P F"),
                _tiles("3m 3m 7m 7m 3p 7p 3s 7s E S W P F"),
            ),
        )

        terminal = draw_for_current_seat(state)

        self.assertEqual(terminal.terminal_reason, "wall_exhausted")
        self.assertEqual(terminal.exhaustive_draw_tenpai_seats, (0,))
        self.assertEqual(terminal.exhaustive_draw_noten_seats, (1, 2, 3))
        self.assertEqual(
            terminal.terminal_point_deltas,
            (
                SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL,
                -SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL // 3,
                -SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL // 3,
                -SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL // 3,
            ),
        )
        self.assertEqual(terminal.points, (28000, 24000, 24000, 24000))
        self.assertEqual(terminal.terminal_rewards, (1.0, -1 / 3, -1 / 3, -1 / 3))
        self.assertEqual(terminal.to_payload()["exhaustive_draw_tenpai_seats"], [0])
        self.assertEqual(terminal.to_payload()["exhaustive_draw_noten_seats"], [1, 2, 3])

    def test_wall_exhaustion_scores_basic_nagashi_mangan(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            points=(25000, 25000, 25000, 25000),
            discards=(
                _tiles("1m 9m E P"),
                _tiles("2m"),
                _tiles("3m"),
                _tiles("4m"),
            ),
            hands=(
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s 5m 6m 7m 8m"),
                (),
                (),
                (),
            ),
            riichi_sticks=1,
            honba=1,
            dealer_seat=0,
        )

        terminal = draw_for_current_seat(state)
        estimate = terminal.terminal_score_estimates[0]
        estimate_payload = terminal.to_payload()["terminal_score_estimates"][0]

        self.assertEqual(terminal.terminal_reason, "nagashi_mangan")
        self.assertEqual(terminal.winner_seat, 0)
        self.assertEqual(terminal.winner_seats, (0,))
        self.assertIsNone(terminal.winning_tile)
        self.assertEqual(terminal.winning_shapes, ())
        self.assertEqual(terminal.winning_yaku, ("nagashi_mangan",))
        self.assertEqual(terminal.winning_yaku_by_seat, ((0, ("nagashi_mangan",)),))
        self.assertEqual(terminal.riichi_sticks, 0)
        self.assertEqual(terminal.terminal_point_deltas, (13300, -4100, -4100, -4100))
        self.assertEqual(terminal.points, (38300, 20900, 20900, 20900))
        self.assertEqual(
            terminal.terminal_rewards,
            (1.0, -4100 / 13300, -4100 / 13300, -4100 / 13300),
        )
        self.assertEqual(terminal.exhaustive_draw_tenpai_seats, ())
        self.assertEqual(terminal.exhaustive_draw_noten_seats, ())
        self.assertEqual(estimate.yaku, ("nagashi_mangan",))
        self.assertEqual(estimate.yaku_han, 5)
        self.assertEqual(estimate.limit, "mangan")
        self.assertEqual(estimate.tsumo_child_payment, 4000)
        self.assertIsNone(estimate.tsumo_dealer_payment)
        self.assertEqual(estimate.riichi_stick_points, 1000)
        self.assertEqual(estimate_payload["yaku"], ["nagashi_mangan"])
        self.assertEqual(estimate_payload["limit"], "mangan")
        self.assertEqual(terminal.to_payload()["exhaustive_draw_tenpai_seats"], [])
        self.assertEqual(terminal.to_payload()["exhaustive_draw_noten_seats"], [])

    def test_wall_exhaustion_scores_multiple_nagashi_mangan_winners(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            points=(25000, 25000, 25000, 25000),
            discards=(
                _tiles("1m 9m E"),
                _tiles("1p 9p S"),
                _tiles("2m"),
                _tiles("3m"),
            ),
            hands=(
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s 5m 6m 7m 8m"),
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s 5m 6m 7m 8m"),
                (),
                (),
            ),
            dealer_seat=0,
        )

        terminal = draw_for_current_seat(state)

        self.assertEqual(terminal.terminal_reason, "nagashi_mangan")
        self.assertEqual(terminal.winner_seat, 0)
        self.assertEqual(terminal.winner_seats, (0, 1))
        self.assertEqual(
            terminal.winning_yaku_by_seat,
            ((0, ("nagashi_mangan",)), (1, ("nagashi_mangan",))),
        )
        self.assertEqual(terminal.terminal_point_deltas, (8000, 4000, -6000, -6000))
        self.assertEqual(terminal.points, (33000, 29000, 19000, 19000))
        self.assertEqual(
            [estimate.yaku for estimate in terminal.terminal_score_estimates],
            [("nagashi_mangan",), ("nagashi_mangan",)],
        )

    def test_wall_exhaustion_rejects_nagashi_for_simple_discard(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            discards=(
                _tiles("1m 5m E"),
                (),
                (),
                (),
            ),
            hands=(
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s 5m 6m 7m 8m"),
                (),
                (),
                (),
            ),
        )

        terminal = draw_for_current_seat(state)

        self.assertEqual(terminal.terminal_reason, "wall_exhausted")
        self.assertEqual(terminal.winner_seats, ())
        self.assertEqual(terminal.winning_yaku, ())

    def test_wall_exhaustion_rejects_nagashi_when_own_discard_was_called(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            discards=(
                _tiles("1m 9m E"),
                (),
                (),
                (),
            ),
            melds=(
                (),
                (
                    Meld(
                        ActionKind.PON,
                        _tiles("E E E"),
                        called_tile=Tile.parse("E"),
                        from_seat=0,
                    ),
                ),
                (),
                (),
            ),
            hands=(
                _tiles("2m 3m 4m 2p 3p 4p 2s 3s 4s 5m 6m 7m 8m"),
                (),
                (),
                (),
            ),
        )

        terminal = draw_for_current_seat(state)

        self.assertEqual(terminal.terminal_reason, "wall_exhausted")
        self.assertEqual(terminal.winner_seats, ())
        self.assertEqual(terminal.winning_yaku, ())

    def test_sanma_environment_excludes_two_to_eight_manzu(self) -> None:
        state = initial_sandbox_environment(ruleset="tenhou-3p", seed="sanma")
        visible = [tile.notation for hand in state.hands for tile in hand]
        wall = [tile.notation for tile in state.wall]
        dead_wall = [tile.notation for tile in state.dead_wall]

        self.assertEqual(state.players, 3)
        self.assertEqual(state.hand_sizes(), [13, 13, 13])
        self.assertEqual(len(state.wall), 55)
        self.assertEqual(len(state.dead_wall), 14)
        self.assertEqual(state.dora_indicators, state.dead_wall[:1])
        self.assertEqual(state.points, (SANDBOX_3P_INITIAL_POINTS,) * 3)
        self.assertEqual(state.to_payload()["points"], [SANDBOX_3P_INITIAL_POINTS] * 3)
        self.assertFalse(any(tile in visible for tile in _excluded_sanma_manzu()))
        self.assertFalse(any(tile in wall for tile in _excluded_sanma_manzu()))
        self.assertFalse(any(tile in dead_wall for tile in _excluded_sanma_manzu()))
        self.assertEqual(state.kita_tiles, ((), (), ()))
        self.assertEqual(state.to_payload()["kita_tiles"], [[], [], []])
        self.assertEqual(state.to_payload()["kita_counts"], [0, 0, 0])

    def test_sanma_full_dead_wall_reserves_only_eight_replacement_tiles(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("N"),),
            dead_wall=_tiles("1p 2p 3p 4p 5p 6p"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)

        terminal = apply_kita_action(drawn, legal_kita_actions(drawn)[0])

        self.assertEqual(terminal.terminal_reason, "wall_exhausted")
        self.assertIsNone(terminal.drawn_tile)
        self.assertFalse(terminal.rinshan_draw)
        self.assertEqual(terminal.dead_wall, _tiles("1p 2p 3p 4p 5p 6p"))
        self.assertEqual(terminal.terminal_point_deltas, (0, 0, 0))
        self.assertEqual(terminal.terminal_rewards, (0.0, 0.0, 0.0))
        self.assertEqual(terminal.kita_tiles, ((Tile.parse("N"),), (), ()))

    def test_sanma_default_points_use_tenhou_three_player_start(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(),
            hands=((), (), ()),
        )

        self.assertEqual(state.points, ())
        self.assertEqual(state.to_payload()["points"], [SANDBOX_3P_INITIAL_POINTS] * 3)

    def test_sanma_kita_uses_replacement_draw_without_kan_dora(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("N"),),
            dead_wall=_tiles("1p 2p 8s"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                (),
                (),
            ),
            riichi_seats=(1,),
            ippatsu_seats=(1,),
        )

        drawn = draw_for_current_seat(state)
        kita = Action(
            ActionKind.KITA,
            TileType.parse("N"),
            consumed=(Tile.parse("N"),),
        )
        actions = legal_kita_actions(drawn)
        turn_actions = legal_sandbox_actions(drawn)
        after_kita = apply_kita_action(drawn, kita)

        self.assertEqual(actions, (kita,))
        self.assertIn(kita, turn_actions)
        self.assertEqual(after_kita.current_seat, 0)
        self.assertEqual(after_kita.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_kita.rinshan_draw)
        self.assertFalse(after_kita.needs_discard)
        self.assertEqual(after_kita.dead_wall, _tiles("1p 2p"))
        self.assertEqual(after_kita.dora_indicators, _tiles("1p"))
        self.assertEqual(after_kita.kita_tiles, ((Tile.parse("N"),), (), ()))
        self.assertEqual(after_kita.hand_sizes(), [14, 0, 0])
        self.assertEqual(after_kita.ippatsu_seats, ())
        self.assertEqual(after_kita.to_payload()["kita_tiles"], [["N"], [], []])
        self.assertEqual(after_kita.to_payload()["kita_counts"], [1, 0, 0])
        self.assertIn(Action.discard("8s"), legal_discard_actions(after_kita))

    def test_sanma_kita_opens_ron_only_reaction_window_before_replacement_draw(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("N"),),
            dead_wall=_tiles("1p 2p 8s"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                _tiles("1p 2p 3p 4p 5p 6p 1s 2s 3s E E E N"),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        kita = legal_kita_actions(drawn)[0]

        pending = apply_kita_action(drawn, kita)
        ron = Action(ActionKind.RON, TileType.parse("N"))

        self.assertIsNone(pending.drawn_tile)
        self.assertFalse(pending.rinshan_draw)
        self.assertFalse(pending.needs_discard)
        self.assertEqual(pending.dead_wall, _tiles("1p 2p 8s"))
        self.assertEqual(pending.dora_indicators, _tiles("1p"))
        self.assertEqual(pending.kita_tiles, ((Tile.parse("N"),), (), ()))
        self.assertEqual(pending.hand_sizes(), [13, 13, 0])
        self.assertEqual(pending.pending_kita_tile, Tile.parse("N"))
        self.assertEqual(pending.pending_kita_seat, 0)
        self.assertEqual(pending.pending_reaction_seats, (1,))
        self.assertEqual(pending.to_payload()["pending_kita_tile"], "N")
        self.assertEqual(pending.to_payload()["pending_kita_seat"], 0)
        self.assertEqual(legal_kita_ron_actions(pending, seat=1), (ron,))
        self.assertEqual(legal_kita_reaction_actions(pending, seat=1), (ron, Action.pass_()))
        self.assertEqual(legal_sandbox_actions(pending, seat=1), (ron, Action.pass_()))
        with self.assertRaisesRegex(ValueError, "no pending discard reaction"):
            legal_call_actions(pending, seat=1)

        terminal = apply_ron_action(pending, seat=1, action=ron)

        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winner_seat, 1)
        self.assertEqual(terminal.winner_seats, (1,))
        self.assertEqual(terminal.winning_tile, Tile.parse("N"))
        self.assertEqual(terminal.winning_shapes, ("standard",))
        self.assertEqual(terminal.winning_yaku, ("yakuhai",))
        self.assertNotIn("chankan", terminal.winning_yaku)
        self.assertIsNone(terminal.pending_kita_tile)
        self.assertIsNone(terminal.pending_kita_seat)
        self.assertIsNone(terminal.pending_chankan_tile)
        self.assertIsNone(terminal.pending_chankan_kind)
        self.assertEqual(terminal.pending_reaction_seats, ())
        self.assertEqual(terminal.terminal_rewards, (-1.0, 1.0, 0.0))

    def test_sanma_kita_ron_preserves_ippatsu_until_reaction_resolves(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("N"),),
            dead_wall=_tiles("1p 2p 8s"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                _tiles("1p 2p 3p 4p 5p 6p 1s 2s 3s E E E N"),
                (),
            ),
            riichi_seats=(1,),
            ippatsu_seats=(1,),
        )
        drawn = draw_for_current_seat(state)
        pending = apply_kita_action(drawn, legal_kita_actions(drawn)[0])
        ron = legal_kita_ron_actions(pending, seat=1)[0]

        terminal = apply_ron_action(pending, seat=1, action=ron)

        self.assertEqual(pending.ippatsu_seats, (1,))
        self.assertEqual(pending.to_payload()["ippatsu_seats"], [1])
        self.assertEqual(terminal.terminal_reason, "ron")
        self.assertEqual(terminal.winning_ippatsu_seats, (1,))
        self.assertEqual(terminal.winning_yaku, ("riichi", "ippatsu", "yakuhai"))
        self.assertEqual(terminal.ippatsu_seats, ())
        self.assertEqual(terminal.to_payload()["winning_ippatsu_seats"], [1])

    def test_sanma_kita_pass_draws_delayed_replacement_tile_without_kan_dora(self) -> None:
        drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-3p",
                players=3,
                wall=(Tile.parse("N"),),
                dead_wall=_tiles("1p 2p 8s"),
                dora_indicators=(Tile.parse("1p"),),
                hands=(
                    _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                    _tiles("1p 2p 3p 4p 5p 6p 1s 2s 3s E E E N"),
                    (),
                ),
            )
        )
        pending = apply_kita_action(drawn, legal_kita_actions(drawn)[0])

        after_pass = apply_reaction_pass_action(pending, seat=1, action=Action.pass_())

        self.assertIsNone(after_pass.pending_kita_tile)
        self.assertIsNone(after_pass.pending_kita_seat)
        self.assertEqual(after_pass.pending_reaction_seats, ())
        self.assertEqual(after_pass.temporary_furiten_seats, (1,))
        self.assertEqual(after_pass.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_pass.rinshan_draw)
        self.assertEqual(after_pass.dead_wall, _tiles("1p 2p"))
        self.assertEqual(after_pass.dora_indicators, _tiles("1p"))
        self.assertEqual(after_pass.hand_sizes(), [14, 13, 0])
        self.assertIn(Action.discard("8s"), legal_discard_actions(after_pass))
        with self.assertRaisesRegex(ValueError, "no pending kita reaction"):
            legal_kita_ron_actions(after_pass, seat=1)

    def test_sanma_kita_pass_clears_ippatsu_after_replacement_draw(self) -> None:
        drawn = draw_for_current_seat(
            SandboxEnvironmentState(
                ruleset="tenhou-3p",
                players=3,
                wall=(Tile.parse("N"),),
                dead_wall=_tiles("1p 2p 8s"),
                dora_indicators=(Tile.parse("1p"),),
                hands=(
                    _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                    _tiles("1p 2p 3p 4p 5p 6p 1s 2s 3s E E E N"),
                    (),
                ),
                riichi_seats=(1,),
                ippatsu_seats=(1,),
            )
        )
        pending = apply_kita_action(drawn, legal_kita_actions(drawn)[0])

        after_pass = apply_reaction_pass_action(pending, seat=1, action=Action.pass_())

        self.assertEqual(pending.ippatsu_seats, (1,))
        self.assertEqual(after_pass.ippatsu_seats, ())
        self.assertEqual(after_pass.riichi_furiten_seats, (1,))
        self.assertEqual(after_pass.drawn_tile, Tile.parse("8s"))
        self.assertTrue(after_pass.rinshan_draw)

    def test_kita_is_not_legal_in_four_player_sandbox(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("N"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                (),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        kita = Action(
            ActionKind.KITA,
            TileType.parse("N"),
            consumed=(Tile.parse("N"),),
        )

        self.assertEqual(legal_kita_actions(drawn), ())
        self.assertNotIn(kita, legal_sandbox_actions(drawn))
        with self.assertRaisesRegex(ValueError, "not legal"):
            apply_kita_action(drawn, kita)
        with self.assertRaisesRegex(ValueError, "kita tiles are only supported"):
            SandboxEnvironmentState(
                ruleset="tenhou-4p",
                players=4,
                wall=(),
                hands=((), (), (), ()),
                kita_tiles=((Tile.parse("N"),), (), (), ()),
            )

    def test_post_riichi_kita_is_allowed_only_for_drawn_north(self) -> None:
        north_state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("N"),),
            dead_wall=_tiles("1p 2p 8s"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                (),
                (),
            ),
            riichi_seats=(0,),
            ippatsu_seats=(0,),
        )
        non_north_state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("8s"),),
            hands=(
                _tiles("1m 9m 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s N"),
                (),
                (),
            ),
            riichi_seats=(0,),
            ippatsu_seats=(0,),
        )

        north_drawn = draw_for_current_seat(north_state)
        non_north_drawn = draw_for_current_seat(non_north_state)
        after_kita = apply_kita_action(north_drawn, legal_kita_actions(north_drawn)[0])

        self.assertEqual(
            legal_kita_actions(north_drawn),
            (
                Action(
                    ActionKind.KITA,
                    TileType.parse("N"),
                    consumed=(Tile.parse("N"),),
                ),
            ),
        )
        self.assertEqual(legal_kita_actions(non_north_drawn), ())
        self.assertEqual(after_kita.riichi_seats, (0,))
        self.assertEqual(after_kita.ippatsu_seats, ())
        self.assertEqual(after_kita.drawn_tile, Tile.parse("8s"))
        self.assertEqual(legal_discard_actions(after_kita), (Action.discard("8s", tsumogiri=True),))

    def test_sanma_kita_tsumo_keeps_kita_as_bonus_han_metadata(self) -> None:
        state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(Tile.parse("N"),),
            dead_wall=_tiles("1p 5m"),
            dora_indicators=(Tile.parse("1p"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(state)
        after_kita = apply_kita_action(drawn, legal_kita_actions(drawn)[0])
        terminal = apply_tsumo_action(after_kita, Action(ActionKind.TSUMO))
        estimate = terminal.terminal_score_estimates[0]

        self.assertEqual(terminal.terminal_reason, "tsumo")
        self.assertEqual(terminal.winning_yaku, ("menzen_tsumo", "rinshan", "yakuhai"))
        self.assertEqual(terminal.winning_rinshan_seats, (0,))
        self.assertEqual(terminal.kita_tiles, ((Tile.parse("N"),), (), ()))
        self.assertEqual(estimate.yaku_han, 3)
        self.assertEqual(estimate.visible_dora_count, 1)
        self.assertEqual(estimate.red_dora_count, 0)
        self.assertEqual(estimate.bonus_han, 2)
        self.assertEqual(estimate.kita_dora_count, 1)
        self.assertEqual(estimate.han, 5)
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["yaku_han"],
            3,
        )
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["visible_dora_count"],
            1,
        )
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["red_dora_count"],
            0,
        )
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["bonus_han"],
            2,
        )
        self.assertEqual(
            terminal.to_payload()["terminal_score_estimates"][0]["kita_dora_count"],
            1,
        )


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
