from __future__ import annotations

from collections.abc import Sequence
from typing import Any

from kenjaku.core import TileType

PPO_SANDBOX_POLICY_KIND = "sandbox-linear-ppo-actor-critic-v0"
PPO_DECISION_TYPES = ("discard", "pass", "call", "kan", "kita", "riichi", "ron")
PPO_STATE_DIM = 58
PPO_ACTION_KIND_OFFSETS = {
    "discard": 0,
    "ron": 34,
    "chi": 68,
    "pon": 102,
    "minkan": 136,
    "ankan": 170,
    "kakan": 204,
    "kita": 238,
}
PPO_PASS_ACTION_INDEX = 272
PPO_TSUMO_ACTION_INDEX = 273
PPO_RIICHI_ACTION_INDEX = 274
PPO_KYUSHU_ACTION_INDEX = 275
PPO_ACTION_DIM = 276


def ppo_state_features(entry: dict[str, Any]) -> list[float]:
    state = entry["state"]
    points = list(state.get("points", []))[:4]
    points.extend(0 for _seat in range(4 - len(points)))
    drawn_tile = state.get("drawn_tile")
    drawn_one_hot = [0.0] * 34
    if isinstance(drawn_tile, str):
        drawn_one_hot[TileType.parse(drawn_tile).index] = 1.0
    round_wind = str(state.get("round_wind", "E"))
    round_wind_one_hot = [1.0 if round_wind == wind else 0.0 for wind in ("E", "S", "W", "N")]
    decision_type = str(entry.get("decision_type", "pass"))
    decision_one_hot = [
        1.0 if decision_type == candidate else 0.0 for candidate in PPO_DECISION_TYPES
    ]
    pending_reactions = state.get("pending_reaction_seats", [])
    features = [
        float(state.get("turn", 0)) / 256.0,
        float(state.get("current_seat", 0)) / 3.0,
        float(entry.get("seat", 0)) / 3.0,
        float(state.get("dealer_seat", 0)) / 3.0,
        float(state.get("honba", 0)) / 8.0,
        float(state.get("wall_remaining", 0)) / 80.0,
        1.0 if state.get("needs_discard") else 0.0,
        float(len(pending_reactions)) / 4.0,
        float(len(state.get("points", []))) / 4.0,
        *round_wind_one_hot,
        *(float(point) / 100000.0 for point in points),
        *drawn_one_hot,
        *decision_one_hot,
    ]
    if len(features) != PPO_STATE_DIM:
        raise ValueError(f"PPO state must have {PPO_STATE_DIM} features")
    return features


def ppo_legal_action_mask(actions: Sequence[dict[str, Any]]) -> list[bool]:
    mask = [False] * PPO_ACTION_DIM
    for action in actions:
        mask[ppo_action_index(action)] = True
    if not any(mask):
        raise ValueError("PPO legal action mask cannot be empty")
    return mask


def ppo_action_index(action: dict[str, Any]) -> int:
    kind = str(action["kind"])
    if kind in PPO_ACTION_KIND_OFFSETS:
        tile = action.get("tile")
        if not isinstance(tile, str):
            raise ValueError(f"{kind} action requires tile payload")
        return PPO_ACTION_KIND_OFFSETS[kind] + TileType.parse(tile).index
    if kind == "pass":
        return PPO_PASS_ACTION_INDEX
    if kind == "tsumo":
        return PPO_TSUMO_ACTION_INDEX
    if kind == "riichi":
        return PPO_RIICHI_ACTION_INDEX
    if kind == "kyushu":
        return PPO_KYUSHU_ACTION_INDEX
    raise ValueError("unsupported PPO action kind: " + kind)
