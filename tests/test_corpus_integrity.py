from __future__ import annotations

import copy
import unittest

from kenjaku.simulation import (
    HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
    SYNTHETIC_CORPUS_INTEGRITY_V1_FIELDS,
    SYNTHETIC_CORPUS_INTEGRITY_V1_KIND,
    validate_synthetic_corpus_integrity,
)


class SyntheticCorpusIntegrityTests(unittest.TestCase):
    def test_validates_legality_balance_and_leakage_free_manifest(self) -> None:
        report = validate_synthetic_corpus_integrity(
            _manifest(),
            required_action_kinds=("discard", "riichi"),
        )

        self.assertEqual(tuple(report), SYNTHETIC_CORPUS_INTEGRITY_V1_FIELDS)
        self.assertEqual(report["kind"], SYNTHETIC_CORPUS_INTEGRITY_V1_KIND)
        self.assertTrue(report["valid"])
        self.assertEqual(report["legal_action_count"], 2)
        self.assertEqual(report["ranked_action_count"], 2)
        self.assertEqual(report["unranked_action_count"], 0)
        self.assertEqual(report["action_kind_counts"]["discard"], 1)
        self.assertEqual(report["action_kind_counts"]["riichi"], 1)

    def test_detects_illegal_labels_balance_gaps_and_future_outcomes(self) -> None:
        manifest = _manifest()
        trajectory = manifest["trajectories"][0]
        trajectory["heuristic_rankings"]["discard"][0]["action"] = _action("pass")
        trajectory["rewards"] = [1.0, -1.0, 0.0, 0.0]

        report = validate_synthetic_corpus_integrity(
            manifest,
            required_action_kinds=("kita",),
        )

        self.assertFalse(report["valid"])
        self.assertIn("trajectory 0 discard action is not legal", report["errors"])
        self.assertIn("future outcome leakage at trajectories[0].rewards", report["errors"])
        self.assertIn("required action kind is absent: kita", report["errors"])


def _manifest() -> dict[str, object]:
    discard = _action("discard", "5m")
    riichi = _action("riichi")
    return {
        "kind": HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
        "ruleset": "tenhou-4p",
        "players": 4,
        "source": {},
        "trajectory_count": 1,
        "trajectories": [
            {
                "game": 0,
                "round": 0,
                "step": 0,
                "seat": 0,
                "state": {"terminal_reason": None},
                "legal_actions": [discard, riichi],
                "heuristic_rankings": {
                    "discard": [_candidate(discard, rank=1)],
                    "call_pass": [],
                    "special_action": [_candidate(riichi, rank=1)],
                },
                "unranked_actions": [],
                "chosen_action": discard,
            }
        ],
        "summary": {
            "family_candidate_counts": {
                "discard": 1,
                "call_pass": 0,
                "special_action": 1,
            },
            "unranked_action_count": 0,
        },
    }


def _action(kind: str, tile: str | None = None) -> dict[str, object]:
    return {"kind": kind, "tile": tile, "tsumogiri": False, "consumed": []}


def _candidate(action: dict[str, object], *, rank: int) -> dict[str, object]:
    return {
        "action": copy.deepcopy(action),
        "rank": rank,
        "score": 1.0,
        "factors": [],
    }


if __name__ == "__main__":
    unittest.main()
