"""Policy-choice agreement benchmarks against evaluator-factor ablations."""

from __future__ import annotations

from collections.abc import Mapping
from typing import Any

from kenjaku.core import ActionKind, Tile
from kenjaku.heuristics import EvaluatorFactorAblation, rank_discard_heuristic
from kenjaku.simulation import validate_synthetic_corpus_integrity

POLICY_HEURISTIC_ABLATION_BENCHMARK_KIND = "kenjaku-policy-heuristic-ablation-benchmark-v0"
_ABLATIONS = {
    "full": EvaluatorFactorAblation(),
    "without_shanten_ukeire": EvaluatorFactorAblation(shanten_ukeire=False),
    "without_hand_value": EvaluatorFactorAblation(hand_value=False),
    "without_defense_risk": EvaluatorFactorAblation(defense_risk=False),
    "without_placement_endgame": EvaluatorFactorAblation(placement_endgame=False),
}


def build_policy_heuristic_ablation_benchmark(manifest: Mapping[str, Any]) -> dict[str, Any]:
    """Compare recorded discard choices with full and ablated heuristic top choices."""
    integrity = validate_synthetic_corpus_integrity(manifest)
    if not integrity["valid"]:
        raise ValueError("invalid distillation manifest: " + str(integrity["errors"][0]))
    ruleset = manifest.get("ruleset")
    trajectories = manifest.get("trajectories")
    if not isinstance(ruleset, str) or not isinstance(trajectories, list):
        raise ValueError("distillation manifest ruleset and trajectories are required")
    counts = {name: 0 for name in _ABLATIONS}
    agreement = {name: 0 for name in _ABLATIONS}
    for trajectory in trajectories:
        if not isinstance(trajectory, Mapping):
            continue
        chosen = trajectory.get("chosen_action")
        state = trajectory.get("state")
        legal_actions = trajectory.get("legal_actions")
        seat = trajectory.get("seat")
        if (
            not isinstance(chosen, Mapping)
            or not isinstance(state, Mapping)
            or type(seat) is not int
        ):
            continue
        if (
            chosen.get("kind") != ActionKind.DISCARD.value
            or not isinstance(chosen.get("tile"), str)
        ):
            continue
        hands = state.get("hands")
        if not isinstance(hands, list) or not 0 <= seat < len(hands):
            continue
        hand = hands[seat]
        if not isinstance(hand, list) or any(not isinstance(tile, str) for tile in hand):
            continue
        tiles = tuple(Tile.parse(tile) for tile in hand)
        if len(tiles) != 14:
            continue
        if not isinstance(legal_actions, list):
            continue
        legal_tiles = {
            Tile.parse(action["tile"]).type
            for action in legal_actions
            if isinstance(action, Mapping)
            and action.get("kind") == ActionKind.DISCARD.value
            and isinstance(action.get("tile"), str)
        }
        if not legal_tiles:
            continue
        chosen_tile = Tile.parse(chosen["tile"]).type
        for name, ablation in _ABLATIONS.items():
            ranked = rank_discard_heuristic(tiles, ruleset=ruleset, ablation=ablation)
            ranked = tuple(candidate for candidate in ranked if candidate.tile in legal_tiles)
            if not ranked:
                continue
            counts[name] += 1
            agreement[name] += int(ranked[0].tile == chosen_tile)
    rows = {
        name: {
            "examples": counts[name],
            "agreement": None if counts[name] == 0 else agreement[name] / counts[name],
        }
        for name in _ABLATIONS
    }
    full = rows["full"]["agreement"]
    return {
        "kind": POLICY_HEURISTIC_ABLATION_BENCHMARK_KIND,
        "ruleset": ruleset,
        "rows": rows,
        "agreement_delta_from_full": {
            name: None if full is None or row["agreement"] is None else row["agreement"] - full
            for name, row in rows.items()
            if name != "full"
        },
    }
