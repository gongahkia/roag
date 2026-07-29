from __future__ import annotations

import json
from collections import Counter
from collections.abc import Iterable, Mapping
from typing import Any

from kenjaku.core import ActionKind

SYNTHETIC_CORPUS_INTEGRITY_V1_KIND = "kenjaku-synthetic-corpus-integrity-v1"
SYNTHETIC_CORPUS_INTEGRITY_V1_FIELDS = (
    "kind",
    "valid",
    "ruleset",
    "players",
    "trajectory_count",
    "legal_action_count",
    "ranked_action_count",
    "unranked_action_count",
    "action_kind_counts",
    "ranking_family_candidate_counts",
    "errors",
)
_RANKING_FAMILIES = ("discard", "call_pass", "special_action")
_LEAKAGE_FIELDS = frozenset(
    {
        "rewards",
        "final_result",
        "final_points",
        "final_placement",
        "final_ranks",
        "final_scores",
        "outcome",
        "outcomes",
    }
)


def validate_synthetic_corpus_integrity(
    manifest: Mapping[str, Any],
    *,
    required_action_kinds: Iterable[ActionKind | str] = (),
) -> dict[str, Any]:
    """Validate synthetic trajectory legality, label balance, and leakage boundaries."""
    from kenjaku.simulation.heuristic_trajectories import (
        HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_FIELDS,
        HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
    )

    required_kinds = _required_action_kinds(required_action_kinds)
    errors: list[str] = []
    if set(manifest) != set(HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_FIELDS):
        errors.append("manifest fields do not match heuristic trajectory manifest v1")
    if manifest.get("kind") != HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND:
        errors.append("manifest kind is not heuristic trajectory manifest v1")
    ruleset = manifest.get("ruleset")
    players = manifest.get("players")
    if not isinstance(ruleset, str):
        errors.append("manifest ruleset must be a string")
    if type(players) is not int:
        errors.append("manifest players must be an integer")
    trajectories = manifest.get("trajectories")
    if not isinstance(trajectories, list):
        errors.append("manifest trajectories must be an array")
        trajectories = []
    if manifest.get("trajectory_count") != len(trajectories):
        errors.append("manifest trajectory_count does not match trajectories")

    action_kind_counts: Counter[str] = Counter()
    ranking_family_candidate_counts: Counter[str] = Counter()
    legal_action_count = 0
    ranked_action_count = 0
    unranked_action_count = 0
    for index, trajectory in enumerate(trajectories):
        if not isinstance(trajectory, Mapping):
            errors.append(f"trajectory {index} must be an object")
            continue
        counts = _validate_trajectory(trajectory, index=index, errors=errors)
        legal_action_count += counts["legal"]
        ranked_action_count += counts["ranked"]
        unranked_action_count += counts["unranked"]
        action_kind_counts.update(counts["action_kinds"])
        ranking_family_candidate_counts.update(counts["families"])

    for kind in required_kinds:
        if action_kind_counts[kind.value] == 0:
            errors.append("required action kind is absent: " + kind.value)
    summary = manifest.get("summary")
    if isinstance(summary, Mapping):
        expected_family_counts = summary.get("family_candidate_counts")
        if expected_family_counts != {
            family: ranking_family_candidate_counts[family] for family in _RANKING_FAMILIES
        }:
            errors.append("manifest family candidate summary does not match trajectories")
        if summary.get("unranked_action_count") != unranked_action_count:
            errors.append("manifest unranked action summary does not match trajectories")
    else:
        errors.append("manifest summary must be an object")

    return {
        "kind": SYNTHETIC_CORPUS_INTEGRITY_V1_KIND,
        "valid": not errors,
        "ruleset": ruleset,
        "players": players,
        "trajectory_count": len(trajectories),
        "legal_action_count": legal_action_count,
        "ranked_action_count": ranked_action_count,
        "unranked_action_count": unranked_action_count,
        "action_kind_counts": {
            kind.value: action_kind_counts[kind.value] for kind in ActionKind
        },
        "ranking_family_candidate_counts": {
            family: ranking_family_candidate_counts[family] for family in _RANKING_FAMILIES
        },
        "errors": errors,
    }


def _required_action_kinds(
    required_action_kinds: Iterable[ActionKind | str],
) -> tuple[ActionKind, ...]:
    resolved: list[ActionKind] = []
    for value in required_action_kinds:
        kind = ActionKind(value)
        if kind not in resolved:
            resolved.append(kind)
    return tuple(resolved)


def _validate_trajectory(
    trajectory: Mapping[str, Any],
    *,
    index: int,
    errors: list[str],
) -> dict[str, Any]:
    _find_leakage(trajectory, path=f"trajectories[{index}]", errors=errors)
    legal_actions = trajectory.get("legal_actions")
    if not isinstance(legal_actions, list):
        errors.append(f"trajectory {index} legal_actions must be an array")
        legal_actions = []
    legal_ids = [
        _action_id(
            action,
            context=f"trajectory {index} legal action",
            errors=errors,
        )
        for action in legal_actions
    ]
    legal_id_set = {action_id for action_id in legal_ids if action_id is not None}
    if len(legal_id_set) != len(legal_ids):
        errors.append(f"trajectory {index} legal actions contain duplicates or invalid actions")
    chosen_id = _action_id(
        trajectory.get("chosen_action"),
        context=f"trajectory {index} chosen action",
        errors=errors,
    )
    if chosen_id is not None and chosen_id not in legal_id_set:
        errors.append(f"trajectory {index} chosen action is not legal")

    rankings = trajectory.get("heuristic_rankings")
    if not isinstance(rankings, Mapping) or set(rankings) != set(_RANKING_FAMILIES):
        errors.append(f"trajectory {index} heuristic rankings have unsupported families")
        rankings = {}
    ranked_ids: set[str] = set()
    family_counts: Counter[str] = Counter()
    ranked_count = 0
    for family in _RANKING_FAMILIES:
        candidates = rankings.get(family, [])
        if not isinstance(candidates, list):
            errors.append(f"trajectory {index} {family} ranking must be an array")
            continue
        previous_rank = 0
        family_ids: set[str] = set()
        for candidate in candidates:
            if not isinstance(candidate, Mapping):
                errors.append(f"trajectory {index} {family} candidate must be an object")
                continue
            action_id = _action_id(
                candidate.get("action"),
                context=f"trajectory {index} {family} action",
                errors=errors,
            )
            rank = candidate.get("rank")
            score = candidate.get("score")
            factors = candidate.get("factors")
            if type(rank) is not int or rank < 1 or rank < previous_rank:
                errors.append(f"trajectory {index} {family} ranks are not ordered")
            else:
                previous_rank = rank
            if type(score) not in {int, float} or not isinstance(factors, list):
                errors.append(f"trajectory {index} {family} candidate payload is invalid")
            if action_id is None:
                continue
            if action_id not in legal_id_set:
                errors.append(f"trajectory {index} {family} action is not legal")
            if action_id in family_ids:
                errors.append(f"trajectory {index} {family} action is duplicated")
            family_ids.add(action_id)
            ranked_ids.add(action_id)
            ranked_count += 1
        family_counts[family] += len(candidates)

    unranked_actions = trajectory.get("unranked_actions")
    if not isinstance(unranked_actions, list):
        errors.append(f"trajectory {index} unranked_actions must be an array")
        unranked_actions = []
    unranked_ids = {
        action_id
        for action in unranked_actions
        for action_id in [
            _action_id(
                action,
                context=f"trajectory {index} unranked action",
                errors=errors,
            )
        ]
        if action_id is not None
    }
    if len(unranked_ids) != len(unranked_actions):
        errors.append(f"trajectory {index} unranked actions contain duplicates or invalid actions")
    if ranked_ids & unranked_ids:
        errors.append(f"trajectory {index} action is both ranked and unranked")
    if ranked_ids | unranked_ids != legal_id_set:
        errors.append(f"trajectory {index} action coverage does not match legal actions")
    return {
        "legal": len(legal_actions),
        "ranked": ranked_count,
        "unranked": len(unranked_actions),
        "action_kinds": Counter(
            action["kind"]
            for action, action_id in zip(legal_actions, legal_ids, strict=True)
            if action_id is not None and isinstance(action, Mapping)
        ),
        "families": family_counts,
    }


def _action_id(action: Any, *, context: str, errors: list[str]) -> str | None:
    if not isinstance(action, Mapping):
        errors.append(context + " must be an object")
        return None
    kind = action.get("kind")
    if not isinstance(kind, str):
        errors.append(context + " kind must be a string")
        return None
    try:
        ActionKind(kind)
    except ValueError:
        errors.append(context + " kind is unsupported")
        return None
    return json.dumps(dict(action), sort_keys=True, separators=(",", ":"))


def _find_leakage(value: Any, *, path: str, errors: list[str]) -> None:
    if isinstance(value, Mapping):
        for key, child in value.items():
            child_path = path + "." + str(key)
            if key in _LEAKAGE_FIELDS:
                errors.append("future outcome leakage at " + child_path)
            if key == "terminal_reason" and child is not None:
                errors.append("terminal outcome leakage at " + child_path)
            _find_leakage(child, path=child_path, errors=errors)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            _find_leakage(child, path=f"{path}[{index}]", errors=errors)
