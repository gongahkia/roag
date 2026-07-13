from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.heuristics import rank_call_pass_heuristic, rank_discard_heuristic
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
