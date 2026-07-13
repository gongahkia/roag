from __future__ import annotations

import json
from collections import Counter
from collections.abc import Mapping, Sequence
from pathlib import Path
from typing import Any

from kenjaku.core import TENHOU_4P, Action, ActionKind, Tile, TileType, tile_counts
from kenjaku.simulation.config import SandboxRuleConfig
from kenjaku.simulation.synthetic_matches import (
    SYNTHETIC_MATCH_MANIFEST_V1_KIND,
    generate_synthetic_match_manifest,
)
from kenjaku.training import CallExample

HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND = (
    "kenjaku-heuristic-distillation-trajectory-manifest-v1"
)
HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_FIELDS = (
    "kind",
    "ruleset",
    "players",
    "source",
    "trajectory_count",
    "trajectories",
    "summary",
)
_RANKING_FAMILIES = ("discard", "call_pass", "special_action")


def generate_heuristic_distillation_trajectory_manifest(
    *,
    match_count: int,
    seed: int | str,
    ruleset: str = TENHOU_4P.name,
    max_rounds: int | None = None,
    max_turns_per_round: int = 512,
) -> dict[str, Any]:
    """Generate deterministic complete synthetic trajectories with heuristic labels."""
    source = generate_synthetic_match_manifest(
        match_count=match_count,
        seed=seed,
        ruleset=ruleset,
        max_rounds=max_rounds,
        max_turns_per_round=max_turns_per_round,
    )
    return build_heuristic_distillation_trajectory_manifest(source)


def build_heuristic_distillation_trajectory_manifest(
    source_manifest: Mapping[str, Any],
) -> dict[str, Any]:
    """Attach deterministic heuristic rankings to complete synthetic trajectories."""
    source, config = _source_metadata(source_manifest)
    trajectories: list[dict[str, Any]] = []
    family_candidate_counts: Counter[str] = Counter()
    unranked_action_count = 0
    matches = source_manifest.get("matches")
    if not isinstance(matches, list):
        raise ValueError("source matches must be an array")
    for match in matches:
        if not isinstance(match, Mapping):
            raise ValueError("source matches must be objects")
        if match.get("completed") is not True:
            raise ValueError("heuristic trajectories require complete synthetic matches")
        game = match.get("game")
        trajectory = match.get("trajectory")
        if type(game) is not int or not isinstance(trajectory, list):
            raise ValueError("source match requires integer game and trajectory array")
        for decision in trajectory:
            if not isinstance(decision, Mapping):
                raise ValueError("source trajectory decisions must be objects")
            record = _trajectory_record(
                game=game,
                decision=decision,
                ruleset=source["ruleset"],
                players=source["players"],
                riichi_deposit_points=config.riichi_deposit_points,
            )
            trajectories.append(record)
            for family, ranking in record["heuristic_rankings"].items():
                family_candidate_counts[family] += len(ranking)
            unranked_action_count += len(record["unranked_actions"])
    manifest = {
        "kind": HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND,
        "ruleset": source["ruleset"],
        "players": source["players"],
        "source": source,
        "trajectory_count": len(trajectories),
        "trajectories": trajectories,
        "summary": {
            "family_candidate_counts": {
                family: family_candidate_counts[family] for family in _RANKING_FAMILIES
            },
            "unranked_action_count": unranked_action_count,
        },
    }
    return json.loads(json.dumps(manifest))


def write_heuristic_distillation_trajectory_manifest(
    path: Path,
    manifest: Mapping[str, Any],
) -> None:
    """Write one heuristic distillation trajectory manifest as canonical JSON."""
    if manifest.get("kind") != HEURISTIC_DISTILLATION_TRAJECTORY_MANIFEST_V1_KIND:
        raise ValueError("not a heuristic distillation trajectory manifest v1")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _source_metadata(
    source_manifest: Mapping[str, Any],
) -> tuple[dict[str, Any], SandboxRuleConfig]:
    if source_manifest.get("kind") != SYNTHETIC_MATCH_MANIFEST_V1_KIND:
        raise ValueError("not a synthetic match manifest v1")
    ruleset = source_manifest.get("ruleset")
    players = source_manifest.get("players")
    match_count = source_manifest.get("match_count")
    provenance = source_manifest.get("provenance")
    matches = source_manifest.get("matches")
    if not isinstance(ruleset, str):
        raise ValueError("source ruleset must be a string")
    if type(players) is not int or type(match_count) is not int:
        raise ValueError("source players and match_count must be integers")
    if not isinstance(provenance, Mapping) or not isinstance(matches, list):
        raise ValueError("source provenance and matches must be objects and arrays")
    rule_config = provenance.get("rule_config")
    if not isinstance(rule_config, Mapping):
        raise ValueError("source provenance must include a rule config")
    config = SandboxRuleConfig.from_versioned_payload(rule_config)
    if config.ruleset != ruleset:
        raise ValueError("source ruleset must match rule config")
    if players != len(config.uma_by_rank):
        raise ValueError("source players must match rule config")
    if len(matches) != match_count:
        raise ValueError("source match_count must match matches")
    return (
        {
            "kind": SYNTHETIC_MATCH_MANIFEST_V1_KIND,
            "ruleset": ruleset,
            "players": players,
            "match_count": match_count,
            "provenance": dict(provenance),
        },
        config,
    )


def _trajectory_record(
    *,
    game: int,
    decision: Mapping[str, Any],
    ruleset: str,
    players: int,
    riichi_deposit_points: int,
) -> dict[str, Any]:
    state = decision.get("state")
    legal_payloads = decision.get("legal_actions")
    seat = decision.get("seat")
    if (
        not isinstance(state, Mapping)
        or not isinstance(legal_payloads, list)
        or type(seat) is not int
    ):
        raise ValueError("trajectory decision requires state, legal actions, and seat")
    legal_actions = tuple(_action_from_payload(payload) for payload in legal_payloads)
    rankings = {
        "discard": _discard_ranking(state, seat=seat, ruleset=ruleset, legal_actions=legal_actions),
        "call_pass": _call_pass_ranking(
            state,
            seat=seat,
            players=players,
            legal_actions=legal_actions,
        ),
        "special_action": _special_action_ranking(
            legal_actions,
            riichi_deposit_points=riichi_deposit_points,
        ),
    }
    ranked_actions = {
        _action_key(_action_from_payload(candidate["action"]))
        for ranking in rankings.values()
        for candidate in ranking
    }
    return {
        "game": game,
        "round": _required_int(decision, "round"),
        "step": _required_int(decision, "step"),
        "seat": seat,
        "state": dict(state),
        "legal_actions": [_action_payload(action) for action in legal_actions],
        "heuristic_rankings": rankings,
        "unranked_actions": [
            _action_payload(action)
            for action in legal_actions
            if _action_key(action) not in ranked_actions
        ],
        "chosen_action": _action_payload(_action_from_payload(decision.get("chosen_action"))),
    }


def _discard_ranking(
    state: Mapping[str, Any],
    *,
    seat: int,
    ruleset: str,
    legal_actions: tuple[Action, ...],
) -> list[dict[str, Any]]:
    from kenjaku.heuristics import rank_discard_heuristic

    hand = _state_hand(state, seat)
    if len(hand) != 14:
        return []
    legal_by_tile = {
        action.tile: action for action in legal_actions if action.kind is ActionKind.DISCARD
    }
    candidates = [
        candidate
        for candidate in rank_discard_heuristic(hand, ruleset=ruleset)
        if candidate.tile in legal_by_tile
    ]
    return [
        _ranking_payload(
            action=legal_by_tile[candidate.tile],
            rank=index,
            score=candidate.score,
            factors=candidate.factors,
        )
        for index, candidate in enumerate(candidates, start=1)
    ]


def _call_pass_ranking(
    state: Mapping[str, Any],
    *,
    seat: int,
    players: int,
    legal_actions: tuple[Action, ...],
) -> list[dict[str, Any]]:
    from kenjaku.heuristics import rank_call_pass_heuristic

    legal_call_kinds = tuple(
        kind
        for kind in (ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN)
        if any(action.kind is kind for action in legal_actions)
    )
    if not legal_call_kinds:
        return []
    pending_discard = state.get("pending_discard")
    pending_discard_seat = state.get("pending_discard_seat")
    if not isinstance(pending_discard, str) or type(pending_discard_seat) is not int:
        raise ValueError("call trajectory state requires pending discard and source seat")
    example = CallExample(
        round_index=0,
        event_index=0,
        call_event_index=None,
        seat=seat,
        from_seat=pending_discard_seat,
        dealer=_required_int(state, "dealer_seat"),
        scores=(0,) * players,
        discarded_tile=Tile.parse(pending_discard),
        legal_call_kinds=legal_call_kinds,
        hand_counts=tile_counts(_state_hand(state, seat)),
        visible_counts=(0,) * 34,
        action=Action.pass_(),
    )
    actions_by_kind: dict[ActionKind, list[Action]] = {}
    for action in legal_actions:
        actions_by_kind.setdefault(action.kind, []).append(action)
    payload: list[dict[str, Any]] = []
    for rank, candidate in enumerate(rank_call_pass_heuristic(example), start=1):
        for action in actions_by_kind.get(candidate.kind, []):
            payload.append(
                _ranking_payload(
                    action=action,
                    rank=rank,
                    score=candidate.score,
                    factors=candidate.factors,
                )
            )
    return payload


def _special_action_ranking(
    legal_actions: tuple[Action, ...],
    *,
    riichi_deposit_points: int,
) -> list[dict[str, Any]]:
    from kenjaku.heuristics import _rank_special_actions

    return [
        _ranking_payload(
            action=candidate.action,
            rank=rank,
            score=candidate.score,
            factors=candidate.factors,
        )
        for rank, candidate in enumerate(
            _rank_special_actions(
                legal_actions,
                riichi_deposit_points=riichi_deposit_points,
            ),
            start=1,
        )
    ]


def _ranking_payload(
    *,
    action: Action,
    rank: int,
    score: float,
    factors: Sequence[Any],
) -> dict[str, Any]:
    return {
        "action": _action_payload(action),
        "rank": rank,
        "score": score,
        "factors": [
            {
                "name": factor.name,
                "value": factor.value,
                "contribution": factor.contribution,
            }
            for factor in factors
        ],
    }


def _state_hand(state: Mapping[str, Any], seat: int) -> tuple[Tile, ...]:
    hands = state.get("hands")
    if not isinstance(hands, list) or not 0 <= seat < len(hands):
        raise ValueError("trajectory state must include the acting hand")
    hand = hands[seat]
    if not isinstance(hand, list) or any(not isinstance(tile, str) for tile in hand):
        raise ValueError("trajectory hand must be a tile string array")
    return tuple(Tile.parse(tile) for tile in hand)


def _action_from_payload(payload: Any) -> Action:
    if not isinstance(payload, Mapping):
        raise ValueError("trajectory action must be an object")
    kind = payload.get("kind")
    tile = payload.get("tile")
    consumed = payload.get("consumed")
    tsumogiri = payload.get("tsumogiri", False)
    if not isinstance(kind, str) or (tile is not None and not isinstance(tile, str)):
        raise ValueError("trajectory action kind and tile are invalid")
    if not isinstance(consumed, list) or any(not isinstance(item, str) for item in consumed):
        raise ValueError("trajectory action consumed tiles are invalid")
    if not isinstance(tsumogiri, bool):
        raise ValueError("trajectory action tsumogiri is invalid")
    return Action(
        ActionKind(kind),
        None if tile is None else TileType.parse(tile),
        tsumogiri=tsumogiri,
        consumed=tuple(Tile.parse(item) for item in consumed),
    )


def _action_payload(action: Action) -> dict[str, Any]:
    return {
        "kind": action.kind.value,
        "tile": None if action.tile is None else action.tile.notation,
        "tsumogiri": action.tsumogiri,
        "consumed": [tile.notation for tile in action.consumed],
    }


def _action_key(action: Action) -> tuple[str, str | None, bool, tuple[str, ...]]:
    return (
        action.kind.value,
        None if action.tile is None else action.tile.notation,
        action.tsumogiri,
        tuple(tile.notation for tile in action.consumed),
    )


def _required_int(payload: Mapping[str, Any], field: str) -> int:
    value = payload.get(field)
    if type(value) is not int:
        raise ValueError("trajectory " + field + " must be an integer")
    return value
