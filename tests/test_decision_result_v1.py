from __future__ import annotations

import json
import math
import unittest

from kenjaku.schema import (
    DECISION_RESULT_V1_KIND,
    ActionV1,
    DecisionFactorV1,
    DecisionRationaleV1,
    DecisionResultV1,
)


class DecisionResultV1Tests(unittest.TestCase):
    def test_round_trips_structured_four_player_rationale(self) -> None:
        result = DecisionResultV1(
            decision_id="fixture:r0:e3:s1",
            ruleset="tenhou-4p",
            model_id="heuristic-discard-v1",
            selected_action=ActionV1(ruleset="tenhou-4p", action="discard", tile="5p"),
            probability=0.75,
            rationale=DecisionRationaleV1(
                factors=(
                    DecisionFactorV1(
                        factor="shanten_delta",
                        value=0,
                        contribution=0.4,
                        evidence=("preserves tenpai",),
                    ),
                    DecisionFactorV1(
                        factor="deal_in_risk",
                        value=0.12,
                        contribution=-0.2,
                    ),
                )
            ),
        )
        payload = result.to_dict()

        self.assertEqual(payload["kind"], DECISION_RESULT_V1_KIND)
        self.assertEqual(DecisionResultV1.from_dict(payload), result)
        self.assertEqual(json.loads(result.to_json()), payload)

    def test_supports_sanma_kita_and_uncalibrated_scores(self) -> None:
        result = DecisionResultV1(
            decision_id="sanma:1",
            ruleset="tenhou-3p",
            model_id="sandbox-policy-v0",
            selected_action=ActionV1(ruleset="tenhou-3p", action="kita", tile="N"),
            probability=None,
            rationale=DecisionRationaleV1(),
        )

        self.assertEqual(DecisionResultV1.from_dict(result.to_dict()), result)

    def test_rejects_mismatches_nonfinite_values_and_duplicate_factors(self) -> None:
        with self.assertRaisesRegex(ValueError, "must match selected_action"):
            DecisionResultV1(
                decision_id="r1",
                ruleset="tenhou-4p",
                model_id="m1",
                selected_action=ActionV1(ruleset="tenhou-3p", action="pass"),
                probability=None,
                rationale=DecisionRationaleV1(),
            )
        with self.assertRaisesRegex(ValueError, r"within \[0, 1\]"):
            DecisionResultV1(
                decision_id="r1",
                ruleset="tenhou-4p",
                model_id="m1",
                selected_action=ActionV1(ruleset="tenhou-4p", action="pass"),
                probability=1.1,
                rationale=DecisionRationaleV1(),
            )
        with self.assertRaisesRegex(ValueError, "must be finite"):
            DecisionFactorV1(factor="risk", value=math.nan, contribution=0)
        factor = DecisionFactorV1(factor="risk", value=0.1, contribution=-0.2)
        with self.assertRaisesRegex(ValueError, "must be unique"):
            DecisionRationaleV1(factors=(factor, factor))

    def test_rejects_unknown_result_fields(self) -> None:
        result = _result()
        payload = result.to_dict()
        payload["extra"] = True

        with self.assertRaisesRegex(ValueError, "unexpected=extra"):
            DecisionResultV1.from_dict(payload)


def _result() -> DecisionResultV1:
    return DecisionResultV1(
        decision_id="r1",
        ruleset="tenhou-4p",
        model_id="m1",
        selected_action=ActionV1(ruleset="tenhou-4p", action="pass"),
        probability=1.0,
        rationale=DecisionRationaleV1(),
    )


if __name__ == "__main__":
    unittest.main()
