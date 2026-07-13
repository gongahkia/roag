from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.heuristics import (
    rank_call_pass_heuristic,
    rank_discard_heuristic,
    rank_special_action_heuristic,
)
from kenjaku.simulation import (
    SandboxEnvironmentState,
    apply_discard_action,
    draw_for_current_seat,
)
from kenjaku.training import CallExample


class DiscardHeuristicTests(unittest.TestCase):
    def test_ranks_discard_types_with_structured_efficiency_factors(self) -> None:
        candidates = rank_discard_heuristic(
            _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 9p")
        )

        best = candidates[0]

        self.assertEqual(best.tile.notation, "5m")
        self.assertEqual(tuple(factor.name for factor in best.factors), (
            "shanten",
            "ukeire",
            "bonus_han",
            "structural_yaku",
        ))
        factor_values = {factor.name: factor.value for factor in best.factors}
        self.assertEqual(factor_values["shanten"], 0.0)
        self.assertEqual(factor_values["ukeire"], 3.0)
        self.assertEqual(
            candidates,
            tuple(
                sorted(
                    candidates,
                    key=lambda item: (item.factors[0].value, -item.score, item.tile.index),
                )
            ),
        )

    def test_rejects_non_discard_hand_sizes_and_unavailable_sanma_tiles(self) -> None:
        with self.assertRaisesRegex(ValueError, "exactly fourteen"):
            rank_discard_heuristic(_tiles("1m"))
        with self.assertRaisesRegex(ValueError, "unavailable"):
            rank_discard_heuristic(
                _tiles("5m 1p 1p 1p 1p 2p 2p 2p 2p 3p 3p 3p 3p 4p"),
                ruleset="tenhou-3p",
            )

    def test_ranks_only_legal_call_pass_candidates_with_proxy_factors(self) -> None:
        candidates = rank_call_pass_heuristic(
            _call_example(legal_call_kinds=(ActionKind.CHI, ActionKind.PON))
        )

        self.assertEqual({candidate.kind for candidate in candidates}, {
            ActionKind.PASS,
            ActionKind.CHI,
            ActionKind.PON,
        })
        self.assertEqual(tuple(factor.name for factor in candidates[0].factors), (
            "after_shanten_proxy",
            "shanten_delta_proxy",
            "ukeire_proxy",
            "consumed_count",
        ))
        self.assertEqual(
            candidates,
            tuple(
                sorted(
                    candidates,
                    key=lambda item: (
                        item.factors[0].value,
                        -item.score,
                        (ActionKind.PASS, ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN).index(
                            item.kind
                        ),
                    ),
                )
            ),
        )

    def test_sanma_call_ranking_does_not_invent_chi(self) -> None:
        candidates = rank_call_pass_heuristic(
            _call_example(
                legal_call_kinds=(ActionKind.PON, ActionKind.MINKAN),
                scores=(35000, 35000, 35000),
            )
        )

        self.assertEqual(
            tuple(candidate.kind for candidate in candidates if candidate.kind == ActionKind.CHI),
            (),
        )

    def test_ranks_riichi_and_kan_actions_from_legal_sandbox_actions(self) -> None:
        riichi_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("1m"), Tile.parse("9s")),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s E E E 5m"),
                (),
                (),
                (),
            ),
        )
        ankan_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("1p"),),
            hands=(
                _tiles("1p 1p 1p 2p 3p 4p 5p 6p 7p 1s 2s 3s E"),
                (),
                (),
                (),
            ),
        )
        tsumo_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s E E E 5m"),
                (),
                (),
                (),
            ),
        )

        riichi_candidates = rank_special_action_heuristic(draw_for_current_seat(riichi_state))
        ankan_candidates = rank_special_action_heuristic(draw_for_current_seat(ankan_state))
        tsumo_candidates = rank_special_action_heuristic(draw_for_current_seat(tsumo_state))

        self.assertEqual(riichi_candidates[0].action.kind, ActionKind.RIICHI)
        self.assertIn(ActionKind.ANKAN, {candidate.action.kind for candidate in ankan_candidates})
        self.assertEqual(tsumo_candidates[0].action.kind, ActionKind.TSUMO)
        self.assertEqual(tuple(factor.name for factor in riichi_candidates[0].factors), (
            "terminal_hora",
            "riichi_declaration",
            "riichi_deposit_kpoints",
            "replacement_draw",
            "pass_action",
        ))

    def test_ranks_hora_above_pass_and_limits_kita_to_sanma(self) -> None:
        ron_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(Tile.parse("5m"),),
            hands=(
                _tiles("1m 1m 1m 2m 3m 4m 5p 6p 7p 8s 9s E S"),
                _tiles("1m 2m 3m 1p 2p 3p 2s 3s 4s E E E 5m"),
                (),
                (),
            ),
        )
        drawn = draw_for_current_seat(ron_state)
        reaction_state, _discard = apply_discard_action(drawn, Action.discard("5m"))
        north = Tile.parse("N")
        sanma_kita_state = SandboxEnvironmentState(
            ruleset="tenhou-3p",
            players=3,
            wall=(),
            hands=((north,), (), ()),
            drawn_tile=north,
        )
        four_player_north_state = SandboxEnvironmentState(
            ruleset="tenhou-4p",
            players=4,
            wall=(),
            hands=((north,), (), (), ()),
            drawn_tile=north,
        )

        ron_candidates = rank_special_action_heuristic(reaction_state, seat=1)
        sanma_kita_candidates = rank_special_action_heuristic(sanma_kita_state)
        four_player_north_candidates = rank_special_action_heuristic(four_player_north_state)

        self.assertEqual(
            tuple(candidate.action.kind for candidate in ron_candidates),
            (ActionKind.RON, ActionKind.PASS),
        )
        self.assertGreater(ron_candidates[0].score, ron_candidates[1].score)
        self.assertEqual(
            tuple(candidate.action.kind for candidate in sanma_kita_candidates),
            (ActionKind.KITA,),
        )
        self.assertNotIn(
            ActionKind.KITA,
            {candidate.action.kind for candidate in four_player_north_candidates},
        )


def _tiles(text: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in text.split())


def _call_example(
    *,
    legal_call_kinds: tuple[ActionKind, ...],
    scores: tuple[int, ...] = (25000, 25000, 25000, 25000),
) -> CallExample:
    counts = [0] * 34
    for token in ["2m", "3m", "4m", "5m", "5m", "5m", "6m", "7m", "8m", "1p", "2p", "3p", "E"]:
        counts[TileType.parse(token).index] += 1
    return CallExample(
        round_index=0,
        event_index=0,
        call_event_index=None,
        seat=1,
        from_seat=0,
        dealer=0,
        scores=scores,
        discarded_tile=Tile.parse("5m"),
        legal_call_kinds=legal_call_kinds,
        hand_counts=tuple(counts),
        visible_counts=(0,) * 34,
        action=Action.pass_(),
    )


if __name__ == "__main__":
    unittest.main()
