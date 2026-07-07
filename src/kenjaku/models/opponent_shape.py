from __future__ import annotations

from collections import Counter
from collections.abc import Sequence
from dataclasses import dataclass
from typing import TYPE_CHECKING

from kenjaku.core import Tile, TileType

if TYPE_CHECKING:
    from kenjaku.training import DiscardExample

OPPONENT_SHAPE_BASELINE_KIND = "opponent-shape-baseline-v0"
OPPONENT_SHAPE_YAKU = (
    "riichi",
    "tanyao",
    "pinfu",
    "yakuhai",
    "iipeikou",
    "sanshoku_doujun",
    "ittsu",
    "honitsu",
    "chiitoitsu",
    "toitoi",
)


@dataclass(frozen=True, slots=True)
class OpponentShapeBaseline:
    """Heuristic yaku-shape prior from an opponent river."""

    min_score: float = 0.05

    @property
    def kind(self) -> str:
        return OPPONENT_SHAPE_BASELINE_KIND

    @property
    def yaku_names(self) -> tuple[str, ...]:
        return OPPONENT_SHAPE_YAKU

    def predict_from_discards(
        self,
        discards: Sequence[Tile | TileType | str | int],
    ) -> dict[str, float]:
        features = _river_features(tuple(_tile_type(tile) for tile in discards))
        raw_scores = {
            "riichi": 1.0 + 0.4 * features["progress"],
            "tanyao": 1.0 + 2.8 * features["terminal_honor_discard_rate"],
            "pinfu": (
                1.0
                + 1.2 * features["terminal_honor_discard_rate"]
                + 0.4 * (1.0 - features["duplicate_rate"])
                + 0.3 * features["suit_balance"]
            ),
            "yakuhai": 0.8 + 1.8 * features["honor_kept_rate"],
            "iipeikou": (
                0.7
                + 0.7 * (1.0 - features["duplicate_rate"])
                + 0.4 * features["suit_balance"]
            ),
            "sanshoku_doujun": (
                0.7 + 0.8 * features["suit_balance"] + 0.2 * features["middle_discard_rate"]
            ),
            "ittsu": (
                0.7
                + 1.1 * features["suit_avoidance"]
                + 0.4 * features["terminal_honor_discard_rate"]
            ),
            "honitsu": (
                0.5
                + 2.0 * features["suit_avoidance"]
                + 0.8 * features["honor_kept_rate"]
            ),
            "chiitoitsu": (
                0.5 + 1.5 * features["duplicate_rate"] + 0.4 * features["honor_kept_rate"]
            ),
            "toitoi": (
                0.5
                + 1.0 * features["duplicate_rate"]
                + 0.7 * features["honor_kept_rate"]
                + 0.3 * features["terminal_honor_kept_rate"]
            ),
        }
        total = sum(max(self.min_score, raw_scores[name]) for name in OPPONENT_SHAPE_YAKU)
        return {
            name: max(self.min_score, raw_scores[name]) / total
            for name in OPPONENT_SHAPE_YAKU
        }

    def predict_for_seat(self, example: DiscardExample, seat: int) -> dict[str, float]:
        if seat < 0 or seat >= len(example.rivers_by_seat):
            raise ValueError(f"seat out of range: {seat}")
        return self.predict_from_discards(example.rivers_by_seat[seat])

    def predict_opponents(self, example: DiscardExample) -> dict[int, dict[str, float]]:
        return {
            seat: self.predict_for_seat(example, seat)
            for seat in range(len(example.rivers_by_seat))
            if seat != example.seat
        }


def _river_features(discards: tuple[TileType, ...]) -> dict[str, float]:
    total = len(discards)
    if total == 0:
        return {
            "progress": 0.0,
            "terminal_honor_discard_rate": 0.0,
            "terminal_honor_kept_rate": 1.0,
            "honor_kept_rate": 1.0,
            "middle_discard_rate": 0.0,
            "duplicate_rate": 0.0,
            "suit_avoidance": 0.0,
            "suit_balance": 0.5,
        }

    counts = Counter(tile.index for tile in discards)
    suit_counts = Counter(tile.suit for tile in discards if not tile.is_honor)
    suited_counts = [suit_counts[suit] for suit in ("m", "p", "s")]
    suited_total = sum(suited_counts)
    terminal_honor = sum(tile.is_terminal_or_honor for tile in discards)
    honor = sum(tile.is_honor for tile in discards)
    middle = sum((not tile.is_honor and tile.rank in {4, 5, 6}) for tile in discards)
    duplicates = sum(count - 1 for count in counts.values() if count > 1)
    suit_spread = max(suited_counts, default=0) - min(suited_counts, default=0)
    suit_avoidance = 0.0 if suited_total == 0 else suit_spread / suited_total
    suit_balance = 0.5 if suited_total == 0 else 1.0 - suit_avoidance
    return {
        "progress": min(total / 18.0, 1.0),
        "terminal_honor_discard_rate": terminal_honor / total,
        "terminal_honor_kept_rate": 1.0 - terminal_honor / total,
        "honor_kept_rate": 1.0 - honor / total,
        "middle_discard_rate": middle / total,
        "duplicate_rate": duplicates / total,
        "suit_avoidance": suit_avoidance,
        "suit_balance": suit_balance,
    }


def _tile_type(tile: Tile | TileType | str | int) -> TileType:
    if isinstance(tile, Tile):
        return tile.type
    if isinstance(tile, TileType):
        return tile
    if isinstance(tile, str):
        return TileType.parse(tile)
    return TileType(int(tile))
