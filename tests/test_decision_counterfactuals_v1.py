from __future__ import annotations

import json
import unittest

from kenjaku.decision_rationale import build_decision_counterfactuals
from kenjaku.schema import (
    DECISION_COUNTERFACTUALS_V1_KIND,
    ActionV1,
    DecisionCounterfactualsV1,
    DecisionCounterfactualV1,
    DecisionFactorV1,
    DecisionRationaleV1,
    DecisionResultV1,
)


class DecisionCounterfactualsV1Tests(unittest.TestCase):
    def test_round_trips_ordered_four_player_alternatives(self) -> None:
        decision = _decision(
            "tenhou-4p",
            ActionV1(ruleset="tenhou-4p", action="discard", tile="5p"),
        )
        payload = DecisionCounterfactualsV1(
            decision=decision,
            selected_score=1.5,
            top_alternatives=(
                _alternative("tenhou-4p", "1p", 1.0, -0.5),
                _alternative("tenhou-4p", "9p", 0.5, -1.0),
            ),
        )

        encoded = payload.to_dict()

        self.assertEqual(encoded["kind"], DECISION_COUNTERFACTUALS_V1_KIND)
        self.assertEqual(DecisionCounterfactualsV1.from_dict(encoded), payload)
        self.assertEqual(json.loads(payload.to_json()), encoded)

    def test_builds_deterministic_top_alternatives_and_sanma_kita_counterfactual(self) -> None:
        decision = _decision(
            "tenhou-4p",
            ActionV1(ruleset="tenhou-4p", action="discard", tile="5p"),
        )
        rationale = _rationale()
        first = build_decision_counterfactuals(
            decision,
            selected_score=1.5,
            alternatives=(
                (ActionV1(ruleset="tenhou-4p", action="discard", tile="9p"), 1.0, rationale),
                (ActionV1(ruleset="tenhou-4p", action="discard", tile="1p"), 1.0, rationale),
                (ActionV1(ruleset="tenhou-4p", action="discard", tile="2p"), 1.25, rationale),
            ),
            limit=2,
        )
        second = build_decision_counterfactuals(
            decision,
            selected_score=1.5,
            alternatives=(
                (ActionV1(ruleset="tenhou-4p", action="discard", tile="9p"), 1.0, rationale),
                (ActionV1(ruleset="tenhou-4p", action="discard", tile="1p"), 1.0, rationale),
                (ActionV1(ruleset="tenhou-4p", action="discard", tile="2p"), 1.25, rationale),
            ),
            limit=2,
        )
        sanma = build_decision_counterfactuals(
            _decision("tenhou-3p", ActionV1(ruleset="tenhou-3p", action="kita", tile="N")),
            selected_score=0.25,
            alternatives=((ActionV1(ruleset="tenhou-3p", action="pass"), 0.0, rationale),),
        )

        self.assertEqual(first, second)
        self.assertEqual(
            [item.action.tile for item in first.top_alternatives],
            ["2p", "1p"],
        )
        self.assertEqual([item.score_delta for item in first.top_alternatives], [-0.25, -0.5])
        self.assertEqual(sanma.top_alternatives[0].action.action, "pass")
        self.assertEqual(sanma.top_alternatives[0].score_delta, -0.25)

    def test_rejects_invalid_deltas_order_and_actions(self) -> None:
        decision = _decision(
            "tenhou-4p",
            ActionV1(ruleset="tenhou-4p", action="discard", tile="5p"),
        )
        with self.assertRaisesRegex(ValueError, "score_delta"):
            DecisionCounterfactualsV1(
                decision=decision,
                selected_score=1.5,
                top_alternatives=(_alternative("tenhou-4p", "1p", 1.0, -0.25),),
            )
        with self.assertRaisesRegex(ValueError, "ordered"):
            DecisionCounterfactualsV1(
                decision=decision,
                selected_score=1.5,
                top_alternatives=(
                    _alternative("tenhou-4p", "9p", 0.5, -1.0),
                    _alternative("tenhou-4p", "1p", 1.0, -0.5),
                ),
            )
        with self.assertRaisesRegex(ValueError, "cannot repeat"):
            build_decision_counterfactuals(
                decision,
                selected_score=1.5,
                alternatives=((decision.selected_action, 1.0, _rationale()),),
            )
        with self.assertRaisesRegex(ValueError, "ruleset"):
            build_decision_counterfactuals(
                decision,
                selected_score=1.5,
                alternatives=((ActionV1(ruleset="tenhou-3p", action="pass"), 1.0, _rationale()),),
            )


def _decision(ruleset: str, action: ActionV1) -> DecisionResultV1:
    return DecisionResultV1(
        decision_id="fixture:decision",
        ruleset=ruleset,
        model_id="heuristic-v1",
        selected_action=action,
        probability=None,
        rationale=_rationale(),
    )


def _alternative(
    ruleset: str,
    tile: str,
    score: float,
    score_delta: float,
) -> DecisionCounterfactualV1:
    return DecisionCounterfactualV1(
        action=ActionV1(ruleset=ruleset, action="discard", tile=tile),
        score=score,
        score_delta=score_delta,
        rationale=_rationale(),
    )


def _rationale() -> DecisionRationaleV1:
    return DecisionRationaleV1(
        factors=(DecisionFactorV1(factor="ukeire", value=8, contribution=0.5),)
    )


if __name__ == "__main__":
    unittest.main()
