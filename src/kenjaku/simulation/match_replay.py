from __future__ import annotations

import json
from collections.abc import Mapping
from typing import Any

from kenjaku.simulation.config import SandboxRuleConfig
from kenjaku.simulation.self_play import (
    SELF_PLAY_MATCH_REPORT_KIND,
    run_self_play_match_sandbox,
)

SELF_PLAY_MATCH_REPLAY_VALIDATION_KIND = "kenjaku-self-play-match-replay-validation-v0"


def reconstruct_self_play_match_report(report: Mapping[str, Any]) -> dict[str, Any]:
    if report.get("kind") != SELF_PLAY_MATCH_REPORT_KIND:
        raise ValueError("not a self-play match report")
    ruleset = _required_string(report, "ruleset")
    rule_config = SandboxRuleConfig.from_versioned_payload(_required_mapping(report, "rule_config"))
    if rule_config.ruleset != ruleset:
        raise ValueError("report ruleset must match rule config")
    policies = _required_mapping(report, "policies")
    opponents = _required_mapping(report, "opponents")
    return run_self_play_match_sandbox(
        games=_required_positive_int(report, "games"),
        max_rounds=_required_positive_int(report, "max_rounds"),
        max_turns_per_round=_required_positive_int(report, "max_turns_per_round"),
        seed=_required_string(report, "seed"),
        ruleset=ruleset,
        rule_config=rule_config,
        discard_policy=_policy_value(policies, "discard"),
        call_policy=_policy_value(policies, "call"),
        riichi_policy=_policy_value(policies, "riichi"),
        kan_policy=_policy_value(policies, "kan"),
        kita_policy=_policy_value(policies, "kita"),
        ron_policy=_policy_value(policies, "ron"),
        heuristic_seats=_heuristic_seats(opponents),
        include_trajectories=True,
    )


def validate_self_play_match_replay(report: Mapping[str, Any]) -> dict[str, Any]:
    _require_trajectories(report)
    reconstructed = reconstruct_self_play_match_report(report)
    mismatches = [
        field
        for field in _REPLAYED_REPORT_FIELDS
        if _canonical_json(report.get(field)) != _canonical_json(reconstructed.get(field))
    ]
    return {
        "kind": SELF_PLAY_MATCH_REPLAY_VALIDATION_KIND,
        "valid": not mismatches,
        "ruleset": reconstructed["ruleset"],
        "players": reconstructed["players"],
        "games": reconstructed["games"],
        "checked_decisions": reconstructed["decisions"],
        "mismatches": mismatches,
    }


_REPLAYED_REPORT_FIELDS = (
    "seed",
    "games",
    "max_rounds",
    "max_turns_per_round",
    "ruleset",
    "rule_config",
    "players",
    "policies",
    "opponents",
    "decisions",
    "rounds",
    "final_reasons",
    "final_summary",
    "game_summaries",
)


def _required_string(payload: Mapping[str, Any], field: str) -> str:
    value = payload.get(field)
    if not isinstance(value, str):
        raise ValueError("report " + field + " must be a string")
    return value


def _required_positive_int(payload: Mapping[str, Any], field: str) -> int:
    value = payload.get(field)
    if type(value) is not int or value <= 0:
        raise ValueError("report " + field + " must be a positive integer")
    return value


def _required_mapping(payload: Mapping[str, Any], field: str) -> Mapping[str, Any]:
    value = payload.get(field)
    if not isinstance(value, Mapping):
        raise ValueError("report " + field + " must be an object")
    return value


def _policy_value(policies: Mapping[str, Any], field: str) -> str:
    value = policies.get(field)
    if not isinstance(value, str):
        raise ValueError("report policy " + field + " must be a string")
    return value


def _heuristic_seats(opponents: Mapping[str, Any]) -> tuple[int, ...]:
    seats = opponents.get("heuristic_seats")
    if not isinstance(seats, list) or any(type(seat) is not int for seat in seats):
        raise ValueError("report opponents heuristic_seats must be an integer array")
    return tuple(seats)


def _require_trajectories(report: Mapping[str, Any]) -> None:
    games = report.get("game_summaries")
    if not isinstance(games, list) or any(
        not isinstance(game, Mapping) or not isinstance(game.get("trajectory"), list)
        for game in games
    ):
        raise ValueError("self-play match replay validation requires trajectories")


def _canonical_json(value: Any) -> str:
    return json.dumps(value, sort_keys=True, separators=(",", ":"))
