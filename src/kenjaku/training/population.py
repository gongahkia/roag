from __future__ import annotations

import random
from collections import Counter
from pathlib import Path
from typing import Any

from kenjaku.logging import get_logger
from kenjaku.reproducibility import derive_seed, derive_seed_int
from kenjaku.simulation import run_self_play_match_sandbox
from kenjaku.simulation.config import SandboxRuleConfig
from kenjaku.simulation.environment import resolve_sandbox_ruleset
from kenjaku.training.guardrails import DEFAULT_TRAINING_TIMEOUT_SECONDS
from kenjaku.training.ppo import (
    SandboxPpoTrainingResult,
    save_ppo_sandbox_checkpoint,
    train_ppo_sandbox,
)

POPULATION_SANDBOX_REPORT_KIND = "kenjaku-population-sandbox-report-v0"
POPULATION_SANDBOX_SNAPSHOT_KIND = "kenjaku-population-sandbox-snapshot-v0"
logger = get_logger(__name__)


def train_population_sandbox(
    *,
    pool_size: int,
    generations: int,
    candidates_per_generation: int,
    matchups_per_candidate: int,
    total_steps: int,
    max_rounds: int,
    max_turns_per_round: int,
    seed: str,
    ruleset: str = "tenhou-4p",
    rule_config: SandboxRuleConfig | None = None,
    ppo_epochs: int = 1,
    batch_size: int = 64,
    learning_rate: float = 0.001,
    hidden_dim: int = 0,
    evaluation_games: int = 1,
    evaluation_max_rounds: int | None = None,
    evaluation_max_turns_per_round: int | None = None,
    promotion_margin: float = 0.0,
    output_dir: str | Path | None = None,
    timeout_seconds: float | None = DEFAULT_TRAINING_TIMEOUT_SECONDS,
) -> dict[str, Any]:
    if rule_config is not None:
        ruleset = rule_config.ruleset
    if pool_size < 4:
        raise ValueError("pool_size must be at least 4")
    if generations <= 0:
        raise ValueError("generations must be positive")
    if candidates_per_generation <= 0:
        raise ValueError("candidates_per_generation must be positive")
    if matchups_per_candidate <= 0:
        raise ValueError("matchups_per_candidate must be positive")
    if total_steps <= 0:
        raise ValueError("total_steps must be positive")
    if max_rounds <= 0:
        raise ValueError("max_rounds must be positive")
    if max_turns_per_round <= 0:
        raise ValueError("max_turns_per_round must be positive")
    if evaluation_games <= 0:
        raise ValueError("evaluation_games must be positive")
    eval_rounds = max_rounds if evaluation_max_rounds is None else evaluation_max_rounds
    eval_turns = (
        max_turns_per_round
        if evaluation_max_turns_per_round is None
        else evaluation_max_turns_per_round
    )
    if eval_rounds <= 0:
        raise ValueError("evaluation_max_rounds must be positive")
    if eval_turns <= 0:
        raise ValueError("evaluation_max_turns_per_round must be positive")
    if promotion_margin < 0:
        raise ValueError("promotion_margin must be non-negative")

    rules = resolve_sandbox_ruleset(ruleset)
    artifact_dir = None if output_dir is None else Path(output_dir)
    if artifact_dir is not None:
        artifact_dir.mkdir(parents=True, exist_ok=True)

    logger.info(
        "population training start",
        extra={
            "event": "population_training_start",
            "pool_size": pool_size,
            "generations": generations,
            "candidates_per_generation": candidates_per_generation,
        },
    )
    pool: list[dict[str, Any]] = []
    snapshots: list[dict[str, Any]] = []
    matchups: list[dict[str, Any]] = []
    promotion_decisions: list[dict[str, Any]] = []

    for index in range(pool_size):
        logger.debug(
            "population initial snapshot start",
            extra={
                "event": "population_snapshot_start",
                "snapshot_id": f"pool-{index}",
                "generation": 0,
                "role": "initial",
            },
        )
        snapshot = _train_population_snapshot(
            snapshot_id=f"pool-{index}",
            generation=0,
            role="initial",
            seed=derive_seed(seed, "initial", index),
            total_steps=total_steps,
            max_rounds=max_rounds,
            max_turns_per_round=max_turns_per_round,
            ruleset=rules.name,
            rule_config=rule_config,
            ppo_epochs=ppo_epochs,
            batch_size=batch_size,
            learning_rate=learning_rate,
            hidden_dim=hidden_dim,
            artifact_dir=artifact_dir,
            timeout_seconds=timeout_seconds,
        )
        pool.append(snapshot)
        snapshots.append(snapshot)

    for snapshot in pool:
        logger.debug(
            "population initial snapshot evaluation start",
            extra={
                "event": "population_snapshot_evaluation_start",
                "snapshot_id": snapshot["id"],
                "generation": snapshot["generation"],
            },
        )
        evaluation = _evaluate_population_snapshot(
            snapshot,
            pool=pool,
            players=rules.players,
            matchups_per_candidate=matchups_per_candidate,
            seed=derive_seed(seed, "initial-eval", str(snapshot["id"])),
            ruleset=rules.name,
            rule_config=rule_config,
            evaluation_games=evaluation_games,
            evaluation_max_rounds=eval_rounds,
            evaluation_max_turns_per_round=eval_turns,
        )
        snapshot["metrics"] = evaluation["metrics"]
        matchups.extend(evaluation["matchups"])

    for generation in range(1, generations + 1):
        logger.info(
            "population generation start",
            extra={"event": "population_generation_start", "generation": generation},
        )
        for candidate_index in range(candidates_per_generation):
            snapshot_id = f"gen-{generation}-candidate-{candidate_index}"
            logger.debug(
                "population candidate snapshot start",
                extra={
                    "event": "population_snapshot_start",
                    "snapshot_id": snapshot_id,
                    "generation": generation,
                    "role": "candidate",
                },
            )
            candidate = _train_population_snapshot(
                snapshot_id=snapshot_id,
                generation=generation,
                role="candidate",
                seed=derive_seed(seed, "generation", generation, "candidate", candidate_index),
                total_steps=total_steps,
                max_rounds=max_rounds,
                max_turns_per_round=max_turns_per_round,
                ruleset=rules.name,
                rule_config=rule_config,
                ppo_epochs=ppo_epochs,
                batch_size=batch_size,
                learning_rate=learning_rate,
                hidden_dim=hidden_dim,
                artifact_dir=artifact_dir,
                timeout_seconds=timeout_seconds,
            )
            evaluation = _evaluate_population_snapshot(
                candidate,
                pool=pool,
                players=rules.players,
                matchups_per_candidate=matchups_per_candidate,
                seed=derive_seed(
                    seed,
                    "generation",
                    generation,
                    "candidate",
                    candidate_index,
                    "eval",
                ),
                ruleset=rules.name,
                rule_config=rule_config,
                evaluation_games=evaluation_games,
                evaluation_max_rounds=eval_rounds,
                evaluation_max_turns_per_round=eval_turns,
            )
            candidate["metrics"] = evaluation["metrics"]
            snapshots.append(candidate)
            matchups.extend(evaluation["matchups"])

            replace_index, replaced = _worst_snapshot(pool)
            candidate_score = float(candidate["metrics"]["average_score"])
            replaced_score = float(replaced["metrics"]["average_score"])
            promoted = candidate_score > replaced_score + promotion_margin
            decision = {
                "generation": generation,
                "candidate_id": candidate["id"],
                "candidate_average_score": candidate_score,
                "replaced_snapshot_id": replaced["id"] if promoted else None,
                "replaced_average_score": replaced_score if promoted else None,
                "promoted": promoted,
                "reason": (
                    "candidate_average_score_exceeded_pool_floor"
                    if promoted
                    else "candidate_did_not_exceed_pool_floor"
                ),
            }
            if promoted:
                candidate["role"] = "pool"
                replaced["role"] = "replaced"
                pool[replace_index] = candidate
            else:
                candidate["role"] = "rejected"
            promotion_decisions.append(decision)
            logger.info(
                "population promotion decision",
                extra={
                    "event": "population_promotion_decision",
                    "generation": generation,
                    "candidate_id": candidate["id"],
                    "promoted": promoted,
                    "candidate_average_score": candidate_score,
                    "replaced_average_score": replaced_score,
                },
            )

    logger.info(
        "population training complete",
        extra={
            "event": "population_training_complete",
            "snapshots": len(snapshots),
            "matchups": len(matchups),
        },
    )
    return {
        "kind": POPULATION_SANDBOX_REPORT_KIND,
        "seed": seed,
        "seed_provenance": {"derivation": "kenjaku-seed-v1-blake2b"},
        "ruleset": rules.name,
        "players": rules.players,
        "pool_size": pool_size,
        "generations": generations,
        "candidates_per_generation": candidates_per_generation,
        "matchups_per_candidate": matchups_per_candidate,
        "training": {
            "total_steps_per_snapshot": total_steps,
            "max_rounds": max_rounds,
            "max_turns_per_round": max_turns_per_round,
            "ppo_epochs": ppo_epochs,
            "batch_size": batch_size,
            "learning_rate": learning_rate,
            "hidden_dim": hidden_dim,
        },
        "evaluation": {
            "games": evaluation_games,
            "max_rounds": eval_rounds,
            "max_turns_per_round": eval_turns,
            "metric_definitions": {
                "win_rate": "Share of sampled matchups where candidate seat 0 finished rank 1.",
                "average_placement": "Mean candidate rank; lower is better.",
                "deal_in_rate": "Share of sampled hands where candidate seat 0 lost points on ron.",
                "average_score": (
                    "Mean final score for candidate seat 0 from sandbox final results."
                ),
            },
        },
        "replacement_criteria": {
            "metric": "average_score",
            "higher_is_better": True,
            "promotion_margin": promotion_margin,
            "replace": "lowest average_score snapshot in the active pool",
        },
        "pool": sorted(pool, key=lambda snapshot: snapshot["id"]),
        "snapshots": snapshots,
        "matchups": matchups,
        "matchup_counts": _matchup_counts(matchups),
        "promotion_decisions": promotion_decisions,
        "capabilities": {
            "policy_snapshot_pool": True,
            "minimum_four_snapshots": pool_size >= 4,
            "opponent_sampling": True,
            "matchup_metrics": True,
            "promotion_criteria": True,
            "checkpoint_artifacts": artifact_dir is not None,
            "learned_policy_environment_integration": False,
        },
    }


def format_population_sandbox_report(report: dict[str, Any]) -> str:
    if report.get("kind") != POPULATION_SANDBOX_REPORT_KIND:
        raise ValueError(f"report kind must be {POPULATION_SANDBOX_REPORT_KIND}")
    lines = [
        f"kind: {report['kind']}",
        f"ruleset: {report['ruleset']}",
        f"pool_size: {report['pool_size']}",
        f"generations: {report['generations']}",
        f"snapshots: {len(report['snapshots'])}",
        f"matchups: {report['matchup_counts']['total']}",
        "pool:",
    ]
    for snapshot in report["pool"]:
        metrics = snapshot["metrics"]
        lines.append(
            "  "
            + f"{snapshot['id']}: "
            + f"avg_score={float(metrics['average_score']):.3f} "
            + f"avg_placement={float(metrics['average_placement']):.3f} "
            + f"win_rate={float(metrics['win_rate']):.3f} "
            + f"deal_in_rate={float(metrics['deal_in_rate']):.3f}"
        )
    lines.append("promotion_decisions:")
    for decision in report["promotion_decisions"]:
        lines.append(
            "  "
            + f"{decision['candidate_id']}: "
            + ("promoted" if decision["promoted"] else "rejected")
            + f" ({decision['reason']})"
        )
    lines.append("capabilities:")
    for name, enabled in report["capabilities"].items():
        lines.append(f"  {name}: {'yes' if enabled else 'no'}")
    return "\n".join(lines)


def _train_population_snapshot(
    *,
    snapshot_id: str,
    generation: int,
    role: str,
    seed: str,
    total_steps: int,
    max_rounds: int,
    max_turns_per_round: int,
    ruleset: str,
    rule_config: SandboxRuleConfig | None,
    ppo_epochs: int,
    batch_size: int,
    learning_rate: float,
    hidden_dim: int,
    artifact_dir: Path | None,
    timeout_seconds: float | None,
) -> dict[str, Any]:
    result = train_ppo_sandbox(
        total_steps=total_steps,
        rollout_games=1,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
        seed=seed,
        ruleset=ruleset,
        rule_config=rule_config,
        ppo_epochs=ppo_epochs,
        batch_size=batch_size,
        learning_rate=learning_rate,
        hidden_dim=hidden_dim,
        device="cpu",
        torch_seed=derive_seed_int(seed, "training", "model"),
        timeout_seconds=timeout_seconds,
    )
    checkpoint_path = _write_snapshot_checkpoint(
        result,
        snapshot_id=snapshot_id,
        artifact_dir=artifact_dir,
    )
    return {
        "kind": POPULATION_SANDBOX_SNAPSHOT_KIND,
        "id": snapshot_id,
        "generation": generation,
        "role": role,
        "seed": seed,
        "checkpoint_path": None if checkpoint_path is None else str(checkpoint_path),
        "training": {
            "environment_steps": result.report["training"]["environment_steps"],
            "updates": result.report["training"]["updates"],
            "policy_kind": result.report["model"]["kind"],
        },
        "metrics": _empty_population_metrics(),
    }


def _write_snapshot_checkpoint(
    result: SandboxPpoTrainingResult,
    *,
    snapshot_id: str,
    artifact_dir: Path | None,
) -> Path | None:
    if artifact_dir is None:
        return None
    path = artifact_dir / f"{snapshot_id}.json"
    save_ppo_sandbox_checkpoint(result, path)
    return path


def _evaluate_population_snapshot(
    snapshot: dict[str, Any],
    *,
    pool: list[dict[str, Any]],
    players: int,
    matchups_per_candidate: int,
    seed: str,
    ruleset: str,
    rule_config: SandboxRuleConfig | None,
    evaluation_games: int,
    evaluation_max_rounds: int,
    evaluation_max_turns_per_round: int,
) -> dict[str, Any]:
    matchup_payloads: list[dict[str, Any]] = []
    wins = 0
    placements = 0.0
    deal_ins = 0
    scores = 0.0
    evaluated_games = 0
    for matchup_index in range(matchups_per_candidate):
        opponents = _sample_opponents(
            snapshot,
            pool=pool,
            players=players,
            seed=derive_seed(seed, "matchup", matchup_index),
        )
        report = run_self_play_match_sandbox(
            games=evaluation_games,
            max_rounds=evaluation_max_rounds,
            max_turns_per_round=evaluation_max_turns_per_round,
            seed=derive_seed(seed, "matchup", matchup_index, "rollout"),
            ruleset=ruleset,
            rule_config=rule_config,
            ron_policy="pass",
        )
        metrics = _candidate_metrics_from_match_report(report, players=players)
        wins += int(metrics["wins"])
        placements += float(metrics["placement_sum"])
        deal_ins += int(metrics["deal_ins"])
        scores += float(metrics["score_sum"])
        evaluated_games += int(metrics["games"])
        matchup_payloads.append(
            {
                "candidate_id": snapshot["id"],
                "opponent_ids": [opponent["id"] for opponent in opponents],
                "games": metrics["games"],
                "wins": metrics["wins"],
                "placement_sum": metrics["placement_sum"],
                "deal_ins": metrics["deal_ins"],
                "score_sum": metrics["score_sum"],
                "final_reasons": report["final_reasons"],
            }
        )
    denominator = evaluated_games or 1
    return {
        "metrics": {
            "games": evaluated_games,
            "win_rate": wins / denominator,
            "average_placement": placements / denominator,
            "deal_in_rate": deal_ins / denominator,
            "average_score": scores / denominator,
        },
        "matchups": matchup_payloads,
    }


def _sample_opponents(
    snapshot: dict[str, Any],
    *,
    pool: list[dict[str, Any]],
    players: int,
    seed: str,
) -> list[dict[str, Any]]:
    candidates = [candidate for candidate in pool if candidate["id"] != snapshot["id"]]
    if not candidates:
        candidates = list(pool)
    rng = random.Random(_stable_seed(seed))
    opponents: list[dict[str, Any]] = []
    while len(opponents) < players - 1:
        opponents.append(candidates[rng.randrange(len(candidates))])
    return opponents


def _candidate_metrics_from_match_report(
    report: dict[str, Any],
    *,
    players: int,
) -> dict[str, int | float]:
    wins = 0
    placement_sum = 0.0
    deal_ins = 0
    score_sum = 0.0
    games = 0
    for game in report["game_summaries"]:
        games += 1
        final_result = game.get("final_result")
        if final_result is None:
            placement_sum += players
        else:
            rank = int(final_result["ranks"][0])
            placement_sum += rank
            wins += int(rank == 1)
            score_sum += float(final_result["scores"][0])
        for round_summary in game.get("round_summaries", []):
            if round_summary.get("terminal_reason") != "ron":
                continue
            deltas = round_summary.get("terminal_point_deltas") or []
            if deltas and int(deltas[0]) < 0:
                deal_ins += 1
    return {
        "games": games,
        "wins": wins,
        "placement_sum": placement_sum,
        "deal_ins": deal_ins,
        "score_sum": score_sum,
    }


def _worst_snapshot(pool: list[dict[str, Any]]) -> tuple[int, dict[str, Any]]:
    return min(
        enumerate(pool),
        key=lambda item: (float(item[1]["metrics"]["average_score"]), item[1]["id"]),
    )


def _matchup_counts(matchups: list[dict[str, Any]]) -> dict[str, Any]:
    as_candidate: Counter[str] = Counter()
    as_opponent: Counter[str] = Counter()
    for matchup in matchups:
        as_candidate[str(matchup["candidate_id"])] += 1
        for opponent_id in matchup["opponent_ids"]:
            as_opponent[str(opponent_id)] += 1
    snapshot_ids = sorted(set(as_candidate) | set(as_opponent))
    return {
        "total": len(matchups),
        "by_snapshot": {
            snapshot_id: {
                "as_candidate": as_candidate[snapshot_id],
                "as_opponent": as_opponent[snapshot_id],
            }
            for snapshot_id in snapshot_ids
        },
    }


def _empty_population_metrics() -> dict[str, int | float]:
    return {
        "games": 0,
        "win_rate": 0.0,
        "average_placement": 0.0,
        "deal_in_rate": 0.0,
        "average_score": 0.0,
    }


def _stable_seed(seed: str) -> int:
    return derive_seed_int(seed, "opponent-sampling")
