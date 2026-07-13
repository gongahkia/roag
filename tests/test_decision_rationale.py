from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.decision_rationale import extract_heuristic_rationale
from kenjaku.heuristics import (
    HeuristicActionCandidate,
    HeuristicFactor,
    rank_discard_heuristic,
)
from kenjaku.schema import ActionV1, DecisionResultV1


class DecisionRationaleTests(unittest.TestCase):
    def test_extracts_four_player_discard_factors_losslessly_and_deterministically(self) -> None:
        candidate = rank_discard_heuristic(
            _tiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 9p")
        )[0]

        first = extract_heuristic_rationale(candidate)
        second = extract_heuristic_rationale(candidate)
        result = DecisionResultV1(
            decision_id="fixture:discard",
            ruleset="tenhou-4p",
            model_id="heuristic-discard-v1",
            selected_action=ActionV1(
                ruleset="tenhou-4p", action="discard", tile=candidate.tile.notation
            ),
            probability=None,
            rationale=first,
        )

        self.assertEqual(first, second)
        self.assertEqual(
            tuple((factor.factor, factor.value, factor.contribution) for factor in first.factors),
            tuple((factor.name, factor.value, factor.contribution) for factor in candidate.factors),
        )
        self.assertEqual(DecisionResultV1.from_dict(result.to_dict()), result)

    def test_extracts_sanma_kita_evidence(self) -> None:
        candidate = HeuristicActionCandidate(
            action=Action(ActionKind.KITA, TileType.parse("N")),
            score=0.25,
            factors=(HeuristicFactor("replacement_draw", 1.0, 0.25),),
        )

        rationale = extract_heuristic_rationale(
            candidate,
            evidence_by_factor={"replacement_draw": ("North replacement draw",)},
        )

        self.assertEqual(rationale.factors[0].factor, "replacement_draw")
        self.assertEqual(rationale.factors[0].evidence, ("North replacement draw",))

    def test_rejects_unused_or_malformed_evidence(self) -> None:
        candidate = HeuristicActionCandidate(
            action=Action(ActionKind.KITA, TileType.parse("N")),
            score=0.25,
            factors=(HeuristicFactor("replacement_draw", 1.0, 0.25),),
        )

        with self.assertRaisesRegex(ValueError, "unknown factors: risk"):
            extract_heuristic_rationale(candidate, evidence_by_factor={"risk": ()})
        with self.assertRaisesRegex(ValueError, "string sequence"):
            extract_heuristic_rationale(candidate, evidence_by_factor={"replacement_draw": "x"})


def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())


if __name__ == "__main__":
    unittest.main()
