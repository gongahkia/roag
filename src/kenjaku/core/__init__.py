"""Core riichi mahjong domain primitives."""

from kenjaku.core.actions import Action, ActionKind
from kenjaku.core.agari import (
    WINNING_HAND_SHAPES,
    is_winning_hand,
    is_winning_hand_for_tiles,
    winning_hand_shapes,
    winning_hand_shapes_for_tiles,
)
from kenjaku.core.rules import TENHOU_3P, TENHOU_4P, RuleSet
from kenjaku.core.scoring import (
    FuCalculation,
    FuComponent,
    ScoreResult,
    calculate_fu,
    ceil_to_hundred,
    honba_payment_for_win,
    round_fu,
    score_limit,
    score_riichi_hand,
)
from kenjaku.core.shanten import (
    chiitoitsu_shanten,
    kokushi_shanten,
    shanten,
    shanten_for_tiles,
    standard_shanten,
)
from kenjaku.core.state import Discard, Meld, PlayerState, RoundState
from kenjaku.core.tiles import Tile, TileType, all_tile_types, tile_counts

__all__ = [
    "Action",
    "ActionKind",
    "calculate_fu",
    "ceil_to_hundred",
    "chiitoitsu_shanten",
    "Discard",
    "FuCalculation",
    "FuComponent",
    "honba_payment_for_win",
    "is_winning_hand",
    "is_winning_hand_for_tiles",
    "kokushi_shanten",
    "Meld",
    "PlayerState",
    "RoundState",
    "round_fu",
    "RuleSet",
    "ScoreResult",
    "score_limit",
    "score_riichi_hand",
    "TENHOU_3P",
    "TENHOU_4P",
    "Tile",
    "TileType",
    "all_tile_types",
    "shanten",
    "shanten_for_tiles",
    "standard_shanten",
    "tile_counts",
    "WINNING_HAND_SHAPES",
    "winning_hand_shapes",
    "winning_hand_shapes_for_tiles",
]
