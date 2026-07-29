from __future__ import annotations

import unittest

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.decision_rationale import extract_heuristic_rationale, render_decision_rationale
from kenjaku.heuristics import (
    HeuristicActionCandidate,
    HeuristicFactor,
    rank_discard_heuristic,
)
from kenjaku.schema import ActionV1, DecisionFactorV1, DecisionRationaleV1, DecisionResultV1


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

    def test_renders_ordered_signed_factors_and_evidence_deterministically(self) -> None:
        rationale = DecisionRationaleV1(
            factors=(
                DecisionFactorV1(
                    factor="replacement_draw",
                    value=1,
                    contribution=0.25,
                    evidence=("North replacement draw",),
                ),
                DecisionFactorV1(factor="defense_risk", value=0.12, contribution=-0.2),
                DecisionFactorV1(factor="placement_uma", value=0, contribution=0),
            )
        )

        first = render_decision_rationale(rationale)
        second = render_decision_rationale(rationale)

        self.assertEqual(first, second)
        self.assertEqual(
            first,
            "replacement draw supported this action (+0.25; value 1). "
            "Evidence: North replacement draw. "
            "defense risk opposed this action (-0.2; value 0.12). "
            "placement uma was neutral (0; value 0).",
        )

    def test_renders_empty_rationale_and_rejects_other_values(self) -> None:
        self.assertEqual(
            render_decision_rationale(DecisionRationaleV1()),
            "No structured rationale factors are available.",
        )
        with self.assertRaisesRegex(ValueError, "DecisionRationaleV1"):
            render_decision_rationale("rationale")


def _tiles(value: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in value.split())


if __name__ == "__main__":
    unittest.main()
