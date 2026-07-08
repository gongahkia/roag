from __future__ import annotations

import unittest

from kenjaku.training.population import (
    POPULATION_SANDBOX_REPORT_KIND,
    _candidate_metrics_from_match_report,
    _empty_population_metrics,
    _matchup_counts,
    _sample_opponents,
    _stable_seed,
    _worst_snapshot,
    format_population_sandbox_report,
)


class PopulationTests(unittest.TestCase):
    def test_sample_opponents_excludes_candidate_and_is_deterministic(self) -> None:
        pool = [_snapshot("a"), _snapshot("b"), _snapshot("c")]

        first = _sample_opponents(pool[0], pool=pool, players=4, seed="fixed")
        second = _sample_opponents(pool[0], pool=pool, players=4, seed="fixed")

        self.assertEqual(
            [opponent["id"] for opponent in first], [opponent["id"] for opponent in second]
        )
        self.assertEqual(len(first), 3)
        self.assertNotIn("a", {opponent["id"] for opponent in first})

    def test_candidate_metrics_from_match_report_counts_rank_score_and_deal_in(self) -> None:
        report = {
            "game_summaries": [
                {
                    "final_result": {"ranks": [1, 2, 3, 4], "scores": [32000, 25000, 22000, 21000]},
                    "round_summaries": [
                        {"terminal_reason": "ron", "terminal_point_deltas": [-8000, 8000, 0, 0]}
                    ],
                },
                {"final_result": None, "round_summaries": []},
            ],
        }

        metrics = _candidate_metrics_from_match_report(report, players=4)

        self.assertEqual(metrics["games"], 2)
        self.assertEqual(metrics["wins"], 1)
        self.assertEqual(metrics["deal_ins"], 1)
        self.assertEqual(metrics["placement_sum"], 5.0)

    def test_worst_snapshot_tiebreaks_by_id(self) -> None:
        index, snapshot = _worst_snapshot([_snapshot("b", score=1.0), _snapshot("a", score=1.0)])

        self.assertEqual(index, 1)
        self.assertEqual(snapshot["id"], "a")

    def test_matchup_counts_tracks_candidate_and_opponents(self) -> None:
        counts = _matchup_counts(
            [
                {"candidate_id": "a", "opponent_ids": ["b", "c"]},
                {"candidate_id": "b", "opponent_ids": ["a", "c"]},
            ],
        )

        self.assertEqual(counts["total"], 2)
        self.assertEqual(counts["by_snapshot"]["a"]["as_candidate"], 1)
        self.assertEqual(counts["by_snapshot"]["c"]["as_opponent"], 2)

    def test_format_population_report_rejects_bad_kind(self) -> None:
        with self.assertRaisesRegex(ValueError, POPULATION_SANDBOX_REPORT_KIND):
            format_population_sandbox_report({"kind": "bad"})

    def test_empty_population_metrics_and_stable_seed_are_deterministic(self) -> None:
        self.assertEqual(_empty_population_metrics()["average_score"], 0.0)
        self.assertEqual(_stable_seed("same"), _stable_seed("same"))
        self.assertNotEqual(_stable_seed("same"), _stable_seed("other"))


def _snapshot(snapshot_id: str, *, score: float = 0.0) -> dict[str, object]:
    return {
        "id": snapshot_id,
        "metrics": {
            "average_score": score,
            "average_placement": 1.0,
            "win_rate": 0.0,
            "deal_in_rate": 0.0,
        },
    }


if __name__ == "__main__":
    unittest.main()
