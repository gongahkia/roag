"""Deterministic paired-seed sandbox match comparisons."""

from __future__ import annotations

from dataclasses import dataclass
from random import Random
from typing import Any

from kenjaku.reproducibility import derive_seed, derive_seed_int
from kenjaku.simulation.self_play import (
    SELF_PLAY_MATCH_ACTION_POLICIES,
    SELF_PLAY_MATCH_DISCARD_POLICIES,
    SELF_PLAY_MATCH_RON_POLICIES,
    run_self_play_match_sandbox,
)

PAIRED_SEED_MATCH_4P_REPORT_KIND = "kenjaku-paired-seed-match-4p-report-v0"
PAIRED_SEED_MATCH_3P_REPORT_KIND = "kenjaku-paired-seed-match-3p-report-v0"
DEFAULT_PAIRED_MATCH_BOOTSTRAP_RESAMPLES = 10_000
PAIRED_MATCH_BOOTSTRAP_CONFIDENCE_LEVEL = 0.95
PAIRED_MATCH_BOOTSTRAP_METHOD = "paired-bootstrap-percentile"


@dataclass(frozen=True, slots=True)
class PairedMatchPolicy:
    """One deterministic sandbox-policy profile used in a paired comparison."""

    discard_policy: str = "drawn"
    call_policy: str = "pass"
    riichi_policy: str = "pass"
    kan_policy: str = "pass"
    kita_policy: str = "pass"
    ron_policy: str = "pass"
    heuristic_seats: tuple[int, ...] = ()

    def __post_init__(self) -> None:
        if self.discard_policy not in SELF_PLAY_MATCH_DISCARD_POLICIES:
            raise ValueError("unsupported paired discard policy: " + self.discard_policy)
        for name, policy in (
            ("call", self.call_policy),
            ("riichi", self.riichi_policy),
            ("kan", self.kan_policy),
            ("kita", self.kita_policy),
        ):
            if policy not in SELF_PLAY_MATCH_ACTION_POLICIES:
                raise ValueError(f"unsupported paired {name} policy: {policy}")
        if self.ron_policy not in SELF_PLAY_MATCH_RON_POLICIES:
            raise ValueError("unsupported paired ron policy: " + self.ron_policy)
        if any(type(seat) is not int or seat < 0 for seat in self.heuristic_seats):
            raise ValueError("paired heuristic_seats must contain non-negative seat integers")
        if len(set(self.heuristic_seats)) != len(self.heuristic_seats):
            raise ValueError("paired heuristic_seats must not contain duplicates")

    def to_dict(self) -> dict[str, Any]:
        return {
            "discard": self.discard_policy,
            "call": self.call_policy,
            "riichi": self.riichi_policy,
            "kan": self.kan_policy,
            "kita": self.kita_policy,
            "ron": self.ron_policy,
            "heuristic_seats": list(sorted(self.heuristic_seats)),
        }


def run_paired_seed_matches_4p(
    *,
    pairs: int,
    seed: str,
    max_rounds: int,
    max_turns_per_round: int,
    candidate: PairedMatchPolicy,
    baseline: PairedMatchPolicy,
    bootstrap_resamples: int = DEFAULT_PAIRED_MATCH_BOOTSTRAP_RESAMPLES,
) -> dict[str, Any]:
    """Run candidate and baseline 4p profiles on exactly the same match seeds."""
    return _run_paired_seed_matches(
        pairs=pairs,
        seed=seed,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        candidate=candidate,
        baseline=baseline,
        ruleset="tenhou-4p",
        players=4,
        kind=PAIRED_SEED_MATCH_4P_REPORT_KIND,
        seed_stage="paired-seed-match-4p",
        bootstrap_resamples=bootstrap_resamples,
    )


def run_paired_seed_matches_3p(
    *,
    pairs: int,
    seed: str,
    max_rounds: int,
    max_turns_per_round: int,
    candidate: PairedMatchPolicy,
    baseline: PairedMatchPolicy,
    bootstrap_resamples: int = DEFAULT_PAIRED_MATCH_BOOTSTRAP_RESAMPLES,
) -> dict[str, Any]:
    """Run candidate and baseline Sanma profiles on exactly the same match seeds."""
    return _run_paired_seed_matches(
        pairs=pairs,
        seed=seed,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        candidate=candidate,
        baseline=baseline,
        ruleset="tenhou-3p",
        players=3,
        kind=PAIRED_SEED_MATCH_3P_REPORT_KIND,
        seed_stage="paired-seed-match-3p",
        bootstrap_resamples=bootstrap_resamples,
    )


def _run_paired_seed_matches(
    *,
    pairs: int,
    seed: str,
    max_rounds: int,
    max_turns_per_round: int,
    candidate: PairedMatchPolicy,
    baseline: PairedMatchPolicy,
    ruleset: str,
    players: int,
    kind: str,
    seed_stage: str,
    bootstrap_resamples: int,
) -> dict[str, Any]:
    if pairs <= 0:
        raise ValueError("pairs must be positive")
    if max_rounds <= 0:
        raise ValueError("max_rounds must be positive")
    if max_turns_per_round <= 0:
        raise ValueError("max_turns_per_round must be positive")
    if bootstrap_resamples <= 0:
        raise ValueError("bootstrap_resamples must be positive")
    _validate_profile_seats(candidate, players=players)
    _validate_profile_seats(baseline, players=players)
    rows: list[dict[str, Any]] = []
    for pair_index in range(pairs):
        pair_seed = derive_seed(seed, seed_stage, pair_index)
        candidate_report = _run_profile(
            candidate,
            seed=pair_seed,
            max_rounds=max_rounds,
            max_turns_per_round=max_turns_per_round,
            ruleset=ruleset,
        )
        baseline_report = _run_profile(
            baseline,
            seed=pair_seed,
            max_rounds=max_rounds,
            max_turns_per_round=max_turns_per_round,
            ruleset=ruleset,
        )
        rows.append(_paired_row(pair_index, pair_seed, candidate_report, baseline_report))
    completed = [row for row in rows if row["completed"]]
    confidence_intervals = _bootstrap_score_delta_confidence_intervals(
        completed,
        players=players,
        seed=derive_seed(seed, "paired-seed-match-bootstrap", ruleset),
        resamples=bootstrap_resamples,
    )
    return {
        "kind": kind,
        "ruleset": ruleset,
        "players": players,
        "seed": seed,
        "seed_provenance": {"derivation": "kenjaku-seed-v1-blake2b"},
        "pairs": pairs,
        "candidate_policy": candidate.to_dict(),
        "baseline_policy": baseline.to_dict(),
        "max_rounds": max_rounds,
        "max_turns_per_round": max_turns_per_round,
        "rows": rows,
        "summary": {
            "completed_pairs": len(completed),
            "incomplete_pairs": pairs - len(completed),
            "mean_placement_adjusted_score_delta_by_seat": _mean_score_delta(
                completed,
                players=players,
            ),
            "placement_adjusted_score_delta_ci_by_seat": confidence_intervals,
        },
    }


def _run_profile(
    profile: PairedMatchPolicy,
    *,
    seed: str,
    max_rounds: int,
    max_turns_per_round: int,
    ruleset: str,
) -> dict[str, Any]:
    return run_self_play_match_sandbox(
        games=1,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        seed=seed,
        ruleset=ruleset,
        discard_policy=profile.discard_policy,
        call_policy=profile.call_policy,
        riichi_policy=profile.riichi_policy,
        kan_policy=profile.kan_policy,
        kita_policy=profile.kita_policy,
        ron_policy=profile.ron_policy,
        heuristic_seats=profile.heuristic_seats,
    )


def _paired_row(
    pair_index: int,
    seed: str,
    candidate_report: dict[str, Any],
    baseline_report: dict[str, Any],
) -> dict[str, Any]:
    candidate_game = candidate_report["game_summaries"][0]
    baseline_game = baseline_report["game_summaries"][0]
    candidate_scores = candidate_game["final_scores"]
    baseline_scores = baseline_game["final_scores"]
    completed = bool(candidate_game["completed"] and baseline_game["completed"])
    score_delta = (
        [
            float(candidate) - float(baseline)
            for candidate, baseline in zip(candidate_scores, baseline_scores, strict=True)
        ]
        if completed
        else None
    )
    return {
        "pair": pair_index,
        "seed": seed,
        "completed": completed,
        "candidate": _game_outcome(candidate_game),
        "baseline": _game_outcome(baseline_game),
        "placement_adjusted_score_delta_by_seat": score_delta,
    }


def _game_outcome(game: dict[str, Any]) -> dict[str, Any]:
    return {
        "completed": game["completed"],
        "terminal_reason": game["terminal_reason"],
        "final_scores": game["final_scores"],
        "final_ranks": game["final_ranks"],
        "final_placement": game["final_placement"],
    }


def _mean_score_delta(rows: list[dict[str, Any]], *, players: int) -> list[float] | None:
    if not rows:
        return None
    return [
        sum(float(row["placement_adjusted_score_delta_by_seat"][seat]) for row in rows) / len(rows)
        for seat in range(players)
    ]


def _bootstrap_score_delta_confidence_intervals(
    rows: list[dict[str, Any]],
    *,
    players: int,
    seed: str,
    resamples: int,
) -> list[dict[str, Any]] | None:
    if not rows:
        return None
    rng = Random(derive_seed_int(seed, "resamples"))
    sample_size = len(rows)
    samples = [[] for _ in range(players)]
    for _ in range(resamples):
        sampled_rows = [rows[rng.randrange(sample_size)] for _ in range(sample_size)]
        for seat in range(players):
            deltas = [
                float(row["placement_adjusted_score_delta_by_seat"][seat]) for row in sampled_rows
            ]
            samples[seat].append(sum(deltas) / sample_size)
    lower_percentile = (1.0 - PAIRED_MATCH_BOOTSTRAP_CONFIDENCE_LEVEL) / 2.0
    upper_percentile = 1.0 - lower_percentile
    return [
        {
            "level": PAIRED_MATCH_BOOTSTRAP_CONFIDENCE_LEVEL,
            "method": PAIRED_MATCH_BOOTSTRAP_METHOD,
            "resamples": resamples,
            "low": _nearest_rank_percentile(samples[seat], lower_percentile),
            "high": _nearest_rank_percentile(samples[seat], upper_percentile),
        }
        for seat in range(players)
    ]


def _nearest_rank_percentile(values: list[float], percentile: float) -> float:
    ordered = sorted(values)
    index = max(0, int(percentile * len(ordered) - 1e-12))
    return ordered[index]


def _validate_profile_seats(profile: PairedMatchPolicy, *, players: int) -> None:
    if any(seat >= players for seat in profile.heuristic_seats):
        raise ValueError(f"paired heuristic_seats must contain {players}p seat integers")
