from __future__ import annotations

import random
from dataclasses import dataclass
from hashlib import blake2b
from typing import Any

from kenjaku.core import (
    TENHOU_3P,
    TENHOU_4P,
    Action,
    ActionKind,
    Meld,
    RuleSet,
    ScoreResult,
    Tile,
    TileType,
    score_riichi_hand,
    shanten_for_tiles,
    winning_hand_shapes_for_tiles,
)

SANDBOX_ENVIRONMENT_KIND = "kenjaku-sandbox-environment-v0"
SANDBOX_RULESETS = ("tenhou-4p", "tenhou-3p")
SANDBOX_INITIAL_POINTS = 25000
SANDBOX_3P_INITIAL_POINTS = 35000
SANDBOX_RETURN_POINTS = 30000
SANDBOX_3P_RETURN_POINTS = 40000
SANDBOX_4P_UMA_BY_RANK = (20.0, 10.0, -10.0, -20.0)
SANDBOX_3P_UMA_BY_RANK = (20.0, 0.0, -20.0)
RIICHI_DEPOSIT_POINTS = 1000
HONBA_RON_POINTS = 300
HONBA_TSUMO_POINTS_PER_LOSER = 100
SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL = 3000
SANDBOX_DEAD_WALL_TILES = 14
SANDBOX_3P_REPLACEMENT_TILES = 8
SANDBOX_3P_NON_REPLACEMENT_DEAD_WALL_TILES = (
    SANDBOX_DEAD_WALL_TILES - SANDBOX_3P_REPLACEMENT_TILES
)
SANDBOX_INITIAL_DORA_INDICATORS = 1
SANDBOX_SCORE_PAYMENT_MODEL = "exact-riichi-score-v0"
SANDBOX_KITA_TILE = TileType.parse("N")
SANDBOX_SEAT_WINDS = (
    TileType.parse("E"),
    TileType.parse("S"),
    TileType.parse("W"),
    TileType.parse("N"),
)
SANDBOX_INITIAL_ROUND_WIND = SANDBOX_SEAT_WINDS[0]
SANDBOX_ALL_LAST_ROUND_WIND = SANDBOX_SEAT_WINDS[1]
SANDBOX_MAX_SUDDEN_DEATH_ROUND_WIND = SANDBOX_SEAT_WINDS[2]
SANDBOX_ROUND_WINDS = SANDBOX_SEAT_WINDS
SANDBOX_DRAGON_TILES_ORDER = (
    TileType.parse("P"),
    TileType.parse("F"),
    TileType.parse("C"),
)
SANDBOX_DRAGON_TILES = frozenset(
    SANDBOX_DRAGON_TILES_ORDER
)
SANDBOX_YAKU_HAN = {
    "chiitoitsu": 2,
    "double_riichi": 2,
    "nagashi_mangan": 5,
    "riichi": 1,
    "ippatsu": 1,
    "menzen_tsumo": 1,
    "rinshan": 1,
    "haitei": 1,
    "honroutou": 2,
    "houtei": 1,
    "chankan": 1,
    "tanyao": 1,
    "toitoi": 2,
    "yakuhai": 1,
}
SANDBOX_LIMIT_BASE_POINTS = {
    "mangan": 2000,
    "haneman": 3000,
    "baiman": 4000,
    "sanbaiman": 6000,
    "yakuman": 8000,
}
SANDBOX_LIMIT_RON_POINTS = {
    "mangan": 8000,
    "haneman": 12000,
    "baiman": 16000,
    "sanbaiman": 24000,
    "yakuman": 32000,
}
SANDBOX_LIMIT_DEALER_RON_POINTS = {
    "mangan": 12000,
    "haneman": 18000,
    "baiman": 24000,
    "sanbaiman": 36000,
    "yakuman": 48000,
}
SANDBOX_LIMIT_TSUMO_CHILD_POINTS = {
    "mangan": 2000,
    "haneman": 3000,
    "baiman": 4000,
    "sanbaiman": 6000,
    "yakuman": 8000,
}
SANDBOX_LIMIT_TSUMO_DEALER_POINTS = {
    "mangan": 4000,
    "haneman": 6000,
    "baiman": 8000,
    "sanbaiman": 12000,
    "yakuman": 16000,
}
SANDBOX_LIMIT_TSUMO_POINTS_PER_LOSER = SANDBOX_LIMIT_TSUMO_CHILD_POINTS
SANDBOX_RULESET_BY_NAME = {
    TENHOU_4P.name: TENHOU_4P,
    TENHOU_3P.name: TENHOU_3P,
}
SANDBOX_ABORTIVE_DRAW_REASONS = frozenset(
    {
        "kyuushu_kyuuhai",
        "four_winds",
        "four_riichi",
        "four_kans",
        "triple_ron",
    }
)


@dataclass(frozen=True, slots=True)
class SandboxScoreEstimate:
    seat: int
    win_kind: str
    yaku: tuple[str, ...]
    yaku_han: int
    bonus_han: int
    visible_dora_count: int
    ura_dora_count: int
    red_dora_count: int
    kita_dora_count: int
    han: int
    fu: int | None
    limit: str | None
    base_points: int
    is_dealer: bool
    ron_payment: int | None
    tsumo_payment_per_loser: int | None
    tsumo_child_payment: int | None
    tsumo_dealer_payment: int | None
    honba_payment: int
    riichi_stick_points: int
    payment_model: str = SANDBOX_SCORE_PAYMENT_MODEL

    def to_payload(self) -> dict[str, Any]:
        return {
            "seat": self.seat,
            "win_kind": self.win_kind,
            "yaku": list(self.yaku),
            "yaku_han": self.yaku_han,
            "bonus_han": self.bonus_han,
            "visible_dora_count": self.visible_dora_count,
            "ura_dora_count": self.ura_dora_count,
            "red_dora_count": self.red_dora_count,
            "kita_dora_count": self.kita_dora_count,
            "han": self.han,
            "fu": self.fu,
            "limit": self.limit,
            "base_points": self.base_points,
            "is_dealer": self.is_dealer,
            "ron_payment": self.ron_payment,
            "tsumo_payment_per_loser": self.tsumo_payment_per_loser,
            "tsumo_child_payment": self.tsumo_child_payment,
            "tsumo_dealer_payment": self.tsumo_dealer_payment,
            "honba_payment": self.honba_payment,
            "riichi_stick_points": self.riichi_stick_points,
            "payment_model": self.payment_model,
        }


@dataclass(frozen=True, slots=True)
class SandboxFinalResult:
    reason: str
    points: tuple[int, ...]
    placement: tuple[int, ...]
    ranks: tuple[int, ...]
    return_points: int
    oka_points: int
    uma_by_rank: tuple[float, ...]
    scores: tuple[float, ...]

    def to_payload(self) -> dict[str, Any]:
        return {
            "reason": self.reason,
            "points": list(self.points),
            "placement": list(self.placement),
            "ranks": list(self.ranks),
            "return_points": self.return_points,
            "oka_points": self.oka_points,
            "uma_by_rank": list(self.uma_by_rank),
            "scores": list(self.scores),
        }


@dataclass(frozen=True, slots=True)
class SandboxEnvironmentState:
    ruleset: str
    players: int
    wall: tuple[Tile, ...]
    hands: tuple[tuple[Tile, ...], ...]
    dead_wall: tuple[Tile, ...] = ()
    dora_indicators: tuple[Tile, ...] = ()
    ura_dora_indicators: tuple[Tile, ...] = ()
    discards: tuple[tuple[Tile, ...], ...] = ()
    melds: tuple[tuple[Meld, ...], ...] = ()
    kita_tiles: tuple[tuple[Tile, ...], ...] = ()
    points: tuple[int, ...] = ()
    riichi_sticks: int = 0
    honba: int = 0
    dealer_seat: int = 0
    round_wind: TileType = SANDBOX_INITIAL_ROUND_WIND
    current_seat: int = 0
    turn: int = 0
    drawn_tile: Tile | None = None
    rinshan_draw: bool = False
    last_draw_was_final_live_wall: bool = False
    needs_discard: bool = False
    pending_discard: Tile | None = None
    pending_discard_seat: int | None = None
    pending_chankan_tile: Tile | None = None
    pending_chankan_seat: int | None = None
    pending_chankan_kind: ActionKind | None = None
    pending_kita_tile: Tile | None = None
    pending_kita_seat: int | None = None
    pending_abortive_draw_reason: str | None = None
    abortive_draw_after_discard_reason: str | None = None
    pending_reaction_seats: tuple[int, ...] = ()
    temporary_furiten_seats: tuple[int, ...] = ()
    riichi_seats: tuple[int, ...] = ()
    double_riichi_seats: tuple[int, ...] = ()
    riichi_pending_discard_seats: tuple[int, ...] = ()
    pending_riichi_declaration_discard_seat: int | None = None
    ippatsu_seats: tuple[int, ...] = ()
    riichi_furiten_seats: tuple[int, ...] = ()
    terminal_reason: str | None = None
    winner_seat: int | None = None
    winner_seats: tuple[int, ...] = ()
    winning_tile: Tile | None = None
    winning_shapes: tuple[str, ...] = ()
    winning_shapes_by_seat: tuple[tuple[int, tuple[str, ...]], ...] = ()
    winning_yaku: tuple[str, ...] = ()
    winning_yaku_by_seat: tuple[tuple[int, tuple[str, ...]], ...] = ()
    winning_ippatsu_seats: tuple[int, ...] = ()
    winning_rinshan_seats: tuple[int, ...] = ()
    terminal_rewards: tuple[float, ...] = ()
    terminal_point_deltas: tuple[int, ...] = ()
    terminal_score_estimates: tuple[SandboxScoreEstimate, ...] = ()
    exhaustive_draw_tenpai_seats: tuple[int, ...] = ()
    exhaustive_draw_noten_seats: tuple[int, ...] = ()
    final_result: SandboxFinalResult | None = None

    def __post_init__(self) -> None:
        if self.ruleset not in SANDBOX_RULESET_BY_NAME:
            raise ValueError("unsupported sandbox environment ruleset: " + self.ruleset)
        rules = SANDBOX_RULESET_BY_NAME[self.ruleset]
        if self.players != rules.players:
            raise ValueError("players must match ruleset")
        if len(self.hands) != self.players:
            raise ValueError("hands must match player count")
        if len(self.dora_indicators) > len(self.dead_wall):
            raise ValueError("dora indicators cannot exceed dead wall size")
        if len(self.ura_dora_indicators) > len(self.dead_wall):
            raise ValueError("ura dora indicators cannot exceed dead wall size")
        if self.discards and len(self.discards) != self.players:
            raise ValueError("discards must match player count")
        if self.melds and len(self.melds) != self.players:
            raise ValueError("melds must match player count")
        if self.kita_tiles and len(self.kita_tiles) != self.players:
            raise ValueError("kita tiles must match player count")
        if any(self.kita_tiles) and self.ruleset != TENHOU_3P.name:
            raise ValueError("kita tiles are only supported for tenhou-3p")
        if any(
            tile.type != SANDBOX_KITA_TILE for seat_tiles in self.kita_tiles for tile in seat_tiles
        ):
            raise ValueError("kita tiles must all be north tiles")
        if self.points and len(self.points) != self.players:
            raise ValueError("point count must match player count")
        if self.riichi_sticks < 0:
            raise ValueError("riichi sticks cannot be negative")
        if self.honba < 0:
            raise ValueError("honba cannot be negative")
        if not 0 <= self.dealer_seat < self.players:
            raise ValueError("dealer_seat outside player range")
        if self.round_wind not in SANDBOX_ROUND_WINDS:
            raise ValueError("round_wind must be an honor wind")
        if not 0 <= self.current_seat < self.players:
            raise ValueError("current_seat outside player range")
        if self.needs_discard and self.drawn_tile is not None:
            raise ValueError("needs_discard cannot be set with a drawn tile")
        if self.rinshan_draw and self.drawn_tile is None:
            raise ValueError("rinshan draw requires a drawn tile")
        if self.rinshan_draw and self.last_draw_was_final_live_wall:
            raise ValueError("rinshan draw cannot also be a live-wall draw")
        pending_windows = (
            int(self.pending_discard is not None)
            + int(self.pending_chankan_tile is not None)
            + int(self.pending_kita_tile is not None)
        )
        if pending_windows > 1:
            raise ValueError("only one pending reaction window is supported")
        if self.needs_discard and pending_windows:
            raise ValueError("needs_discard cannot be set during pending reactions")
        if self.pending_discard is None and self.pending_discard_seat is not None:
            raise ValueError("pending discard seat requires a pending discard")
        if self.pending_chankan_tile is None and self.pending_chankan_seat is not None:
            raise ValueError("pending chankan seat requires a pending chankan tile")
        if self.pending_chankan_tile is None and self.pending_chankan_kind is not None:
            raise ValueError("pending chankan kind requires a pending chankan tile")
        if self.pending_chankan_tile is not None and self.pending_chankan_kind is None:
            raise ValueError("pending chankan kind is required")
        if self.pending_chankan_kind not in (None, ActionKind.ANKAN, ActionKind.KAKAN):
            raise ValueError("pending chankan kind must be ankan or kakan")
        if self.pending_kita_tile is None and self.pending_kita_seat is not None:
            raise ValueError("pending kita seat requires a pending kita tile")
        if (
            self.pending_abortive_draw_reason is not None
            and self.pending_abortive_draw_reason not in SANDBOX_ABORTIVE_DRAW_REASONS
        ):
            raise ValueError("unsupported pending abortive draw reason")
        if (
            self.abortive_draw_after_discard_reason is not None
            and self.abortive_draw_after_discard_reason not in SANDBOX_ABORTIVE_DRAW_REASONS
        ):
            raise ValueError("unsupported abortive draw after discard reason")
        if pending_windows == 0 and self.pending_reaction_seats:
            raise ValueError("pending reaction seats require a pending reaction window")
        if self.pending_abortive_draw_reason is not None and self.pending_discard is None:
            raise ValueError("pending abortive draw requires a pending discard")
        if (
            self.abortive_draw_after_discard_reason is not None
            and self.drawn_tile is None
            and not self.needs_discard
        ):
            raise ValueError("abortive draw after discard requires a pending discard obligation")
        if self.pending_discard is not None:
            if self.pending_discard_seat is None:
                raise ValueError("pending discard seat is required")
            if not 0 <= self.pending_discard_seat < self.players:
                raise ValueError("pending discard seat outside player range")
            if self.pending_discard_seat in self.pending_reaction_seats:
                raise ValueError("discarding seat cannot react to its own discard")
            if any(not 0 <= seat < self.players for seat in self.pending_reaction_seats):
                raise ValueError("pending reaction seat outside player range")
        if self.pending_chankan_tile is not None:
            if self.pending_chankan_seat is None:
                raise ValueError("pending chankan seat is required")
            if not 0 <= self.pending_chankan_seat < self.players:
                raise ValueError("pending chankan seat outside player range")
            if self.pending_chankan_seat in self.pending_reaction_seats:
                raise ValueError("kan seat cannot react to its own added kan")
            if any(not 0 <= seat < self.players for seat in self.pending_reaction_seats):
                raise ValueError("pending reaction seat outside player range")
        if self.pending_kita_tile is not None:
            if self.ruleset != TENHOU_3P.name:
                raise ValueError("pending kita reactions are only supported for tenhou-3p")
            if self.pending_kita_tile.type != SANDBOX_KITA_TILE:
                raise ValueError("pending kita tile must be a north tile")
            if self.pending_kita_seat is None:
                raise ValueError("pending kita seat is required")
            if not 0 <= self.pending_kita_seat < self.players:
                raise ValueError("pending kita seat outside player range")
            if self.pending_kita_seat in self.pending_reaction_seats:
                raise ValueError("kita seat cannot react to its own kita")
            if any(not 0 <= seat < self.players for seat in self.pending_reaction_seats):
                raise ValueError("pending reaction seat outside player range")
        if any(not 0 <= seat < self.players for seat in self.temporary_furiten_seats):
            raise ValueError("temporary furiten seat outside player range")
        if len(set(self.temporary_furiten_seats)) != len(self.temporary_furiten_seats):
            raise ValueError("temporary furiten seats must be unique")
        if any(not 0 <= seat < self.players for seat in self.riichi_seats):
            raise ValueError("riichi seat outside player range")
        if len(set(self.riichi_seats)) != len(self.riichi_seats):
            raise ValueError("riichi seats must be unique")
        if any(not 0 <= seat < self.players for seat in self.double_riichi_seats):
            raise ValueError("double riichi seat outside player range")
        if len(set(self.double_riichi_seats)) != len(self.double_riichi_seats):
            raise ValueError("double riichi seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.double_riichi_seats):
            raise ValueError("double riichi seats must also be riichi seats")
        if any(not 0 <= seat < self.players for seat in self.riichi_pending_discard_seats):
            raise ValueError("riichi pending discard seat outside player range")
        if len(set(self.riichi_pending_discard_seats)) != len(
            self.riichi_pending_discard_seats
        ):
            raise ValueError("riichi pending discard seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.riichi_pending_discard_seats):
            raise ValueError("riichi pending discard seats must also be riichi seats")
        if self.pending_riichi_declaration_discard_seat is not None:
            if self.pending_discard is None:
                raise ValueError("pending riichi declaration seat requires a pending discard")
            if (
                self.pending_discard_seat
                != self.pending_riichi_declaration_discard_seat
            ):
                raise ValueError("pending riichi declaration seat must match discard seat")
            if not 0 <= self.pending_riichi_declaration_discard_seat < self.players:
                raise ValueError("pending riichi declaration seat outside player range")
            if self.pending_riichi_declaration_discard_seat not in self.riichi_seats:
                raise ValueError("pending riichi declaration seat must also be riichi")
        if any(not 0 <= seat < self.players for seat in self.ippatsu_seats):
            raise ValueError("ippatsu seat outside player range")
        if len(set(self.ippatsu_seats)) != len(self.ippatsu_seats):
            raise ValueError("ippatsu seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.ippatsu_seats):
            raise ValueError("ippatsu seats must also be riichi seats")
        if any(not 0 <= seat < self.players for seat in self.riichi_furiten_seats):
            raise ValueError("riichi furiten seat outside player range")
        if len(set(self.riichi_furiten_seats)) != len(self.riichi_furiten_seats):
            raise ValueError("riichi furiten seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.riichi_furiten_seats):
            raise ValueError("riichi furiten seats must also be riichi seats")
        if any(not 0 <= seat < self.players for seat in self.winning_ippatsu_seats):
            raise ValueError("winning ippatsu seat outside player range")
        if len(set(self.winning_ippatsu_seats)) != len(self.winning_ippatsu_seats):
            raise ValueError("winning ippatsu seats must be unique")
        if any(seat not in self.winner_seats for seat in self.winning_ippatsu_seats):
            raise ValueError("winning ippatsu seats must also be winner seats")
        if any(not 0 <= seat < self.players for seat in self.winning_rinshan_seats):
            raise ValueError("winning rinshan seat outside player range")
        if len(set(self.winning_rinshan_seats)) != len(self.winning_rinshan_seats):
            raise ValueError("winning rinshan seats must be unique")
        if any(seat not in self.winner_seats for seat in self.winning_rinshan_seats):
            raise ValueError("winning rinshan seats must also be winner seats")
        if self.winning_yaku and not self.winner_seats:
            raise ValueError("winning yaku require winner seats")
        if any(not 0 <= seat < self.players for seat, _yaku in self.winning_yaku_by_seat):
            raise ValueError("winning yaku seat outside player range")
        if any(seat not in self.winner_seats for seat, _yaku in self.winning_yaku_by_seat):
            raise ValueError("winning yaku seats must also be winner seats")
        if self.terminal_rewards and len(self.terminal_rewards) != self.players:
            raise ValueError("terminal reward count must match player count")
        if self.terminal_point_deltas and len(self.terminal_point_deltas) != self.players:
            raise ValueError("terminal point-delta count must match player count")
        if self.terminal_score_estimates and not self.winner_seats:
            raise ValueError("terminal score estimates require winner seats")
        if any(not 0 <= estimate.seat < self.players for estimate in self.terminal_score_estimates):
            raise ValueError("terminal score estimate seat outside player range")
        if any(
            estimate.seat not in self.winner_seats
            for estimate in self.terminal_score_estimates
        ):
            raise ValueError("terminal score estimate seats must also be winner seats")
        if any(not 0 <= seat < self.players for seat in self.exhaustive_draw_tenpai_seats):
            raise ValueError("exhaustive draw tenpai seat outside player range")
        if len(set(self.exhaustive_draw_tenpai_seats)) != len(
            self.exhaustive_draw_tenpai_seats
        ):
            raise ValueError("exhaustive draw tenpai seats must be unique")
        if any(not 0 <= seat < self.players for seat in self.exhaustive_draw_noten_seats):
            raise ValueError("exhaustive draw noten seat outside player range")
        if len(set(self.exhaustive_draw_noten_seats)) != len(
            self.exhaustive_draw_noten_seats
        ):
            raise ValueError("exhaustive draw noten seats must be unique")
        if set(self.exhaustive_draw_tenpai_seats) & set(self.exhaustive_draw_noten_seats):
            raise ValueError("exhaustive draw tenpai and noten seats cannot overlap")
        if (
            self.terminal_reason != "wall_exhausted"
            and (self.exhaustive_draw_tenpai_seats or self.exhaustive_draw_noten_seats)
        ):
            raise ValueError("exhaustive draw seats require wall exhaustion")
        if self.final_result is not None:
            if self.terminal_reason is None:
                raise ValueError("final result requires a terminal state")
            if len(self.final_result.points) != self.players:
                raise ValueError("final result point count must match player count")
            if sorted(self.final_result.placement) != list(range(self.players)):
                raise ValueError("final result placement must include every seat")
            if len(self.final_result.ranks) != self.players:
                raise ValueError("final result rank count must match player count")
            if len(self.final_result.uma_by_rank) != self.players:
                raise ValueError("final result uma count must match player count")
            if len(self.final_result.scores) != self.players:
                raise ValueError("final result score count must match player count")

    def current_hand(self) -> tuple[Tile, ...]:
        return self.hands[self.current_seat]

    def hand_sizes(self) -> list[int]:
        return [len(hand) for hand in self.hands]

    def to_payload(self) -> dict[str, Any]:
        return {
            "kind": SANDBOX_ENVIRONMENT_KIND,
            "ruleset": self.ruleset,
            "players": self.players,
            "turn": self.turn,
            "current_seat": self.current_seat,
            "wall_remaining": len(self.wall),
            "dead_wall_remaining": len(self.dead_wall),
            "dora_indicators": [tile.notation for tile in self.dora_indicators],
            "ura_dora_indicators": [
                tile.notation for tile in self.ura_dora_indicators
            ],
            "hand_sizes": self.hand_sizes(),
            "points": list(_points_by_seat(self)),
            "riichi_sticks": self.riichi_sticks,
            "honba": self.honba,
            "dealer_seat": self.dealer_seat,
            "round_wind": self.round_wind.notation,
            "discards": [
                [tile.notation for tile in seat_discards]
                for seat_discards in _discards_by_seat(self)
            ],
            "melds": _meld_payloads(self),
            "kita_tiles": [
                [tile.notation for tile in seat_tiles]
                for seat_tiles in _kita_tiles_by_seat(self)
            ],
            "kita_counts": [len(seat_tiles) for seat_tiles in _kita_tiles_by_seat(self)],
            "drawn_tile": None if self.drawn_tile is None else self.drawn_tile.notation,
            "rinshan_draw": self.rinshan_draw,
            "last_draw_was_final_live_wall": self.last_draw_was_final_live_wall,
            "needs_discard": self.needs_discard,
            "pending_discard": (
                None if self.pending_discard is None else self.pending_discard.notation
            ),
            "pending_discard_seat": self.pending_discard_seat,
            "pending_chankan_tile": (
                None if self.pending_chankan_tile is None else self.pending_chankan_tile.notation
            ),
            "pending_chankan_seat": self.pending_chankan_seat,
            "pending_chankan_kind": (
                None if self.pending_chankan_kind is None else self.pending_chankan_kind.value
            ),
            "pending_kita_tile": (
                None if self.pending_kita_tile is None else self.pending_kita_tile.notation
            ),
            "pending_kita_seat": self.pending_kita_seat,
            "pending_abortive_draw_reason": self.pending_abortive_draw_reason,
            "abortive_draw_after_discard_reason": self.abortive_draw_after_discard_reason,
            "pending_reaction_seats": list(self.pending_reaction_seats),
            "temporary_furiten_seats": list(self.temporary_furiten_seats),
            "riichi_seats": list(self.riichi_seats),
            "double_riichi_seats": list(self.double_riichi_seats),
            "riichi_pending_discard_seats": list(self.riichi_pending_discard_seats),
            "pending_riichi_declaration_discard_seat": (
                self.pending_riichi_declaration_discard_seat
            ),
            "ippatsu_seats": list(self.ippatsu_seats),
            "riichi_furiten_seats": list(self.riichi_furiten_seats),
            "terminal_reason": self.terminal_reason,
            "winner_seat": self.winner_seat,
            "winner_seats": list(self.winner_seats),
            "winning_tile": None if self.winning_tile is None else self.winning_tile.notation,
            "winning_shapes": list(self.winning_shapes),
            "winning_shapes_by_seat": [
                {"seat": seat, "shapes": list(shapes)}
                for seat, shapes in self.winning_shapes_by_seat
            ],
            "winning_yaku": list(self.winning_yaku),
            "winning_yaku_by_seat": [
                {"seat": seat, "yaku": list(yaku)}
                for seat, yaku in self.winning_yaku_by_seat
            ],
            "winning_ippatsu_seats": list(self.winning_ippatsu_seats),
            "winning_rinshan_seats": list(self.winning_rinshan_seats),
            "terminal_rewards": list(self.terminal_rewards),
            "terminal_point_deltas": list(self.terminal_point_deltas),
            "terminal_score_estimates": [
                estimate.to_payload() for estimate in self.terminal_score_estimates
            ],
            "exhaustive_draw_tenpai_seats": list(self.exhaustive_draw_tenpai_seats),
            "exhaustive_draw_noten_seats": list(self.exhaustive_draw_noten_seats),
            "game_finished": self.final_result is not None,
            "final_result": (
                None if self.final_result is None else self.final_result.to_payload()
            ),
        }


def initial_sandbox_environment(
    *,
    ruleset: str = "tenhou-4p",
    seed: str | int = "kenjaku-sandbox-v0",
) -> SandboxEnvironmentState:
    rules = resolve_sandbox_ruleset(ruleset)
    rng = random.Random(_seed_int(seed))
    wall = _shuffled_wall(rng, rules=rules)
    hands = tuple(
        tuple(wall.pop() for _tile in range(13))
        for _seat in range(rules.players)
    )
    dead_wall = tuple(wall.pop() for _tile in range(SANDBOX_DEAD_WALL_TILES))
    return SandboxEnvironmentState(
        ruleset=rules.name,
        players=rules.players,
        wall=tuple(wall),
        hands=hands,
        dead_wall=dead_wall,
        dora_indicators=dead_wall[:SANDBOX_INITIAL_DORA_INDICATORS],
        ura_dora_indicators=(),
        discards=tuple(() for _seat in range(rules.players)),
        melds=tuple(() for _seat in range(rules.players)),
        kita_tiles=tuple(() for _seat in range(rules.players)),
        points=tuple(_initial_points_for_ruleset(rules.name) for _seat in range(rules.players)),
        dealer_seat=0,
        round_wind=SANDBOX_INITIAL_ROUND_WIND,
        current_seat=0,
    )


def next_round_sandbox_environment(
    state: SandboxEnvironmentState,
    *,
    seed: str | int,
) -> SandboxEnvironmentState:
    if state.terminal_reason is None:
        raise ValueError("next round requires a terminal sandbox state")
    if state.terminal_reason == "max_turns":
        raise ValueError("cannot advance artificial max-turn terminal")
    dealer_repeats = _dealer_repeats_after_terminal(state)
    next_dealer = (
        state.dealer_seat
        if dealer_repeats
        else (state.dealer_seat + 1) % state.players
    )
    next_honba = state.honba + 1 if _terminal_carries_honba(state) else 0
    next_round_wind = _next_round_wind_after_terminal(
        state,
        dealer_repeats=dealer_repeats,
        next_dealer=next_dealer,
    )
    game_end_reason = _sandbox_game_end_reason(
        state,
        dealer_repeats=dealer_repeats,
        next_dealer=next_dealer,
        next_round_wind=next_round_wind,
    )
    if game_end_reason is not None:
        return _replace_state(
            state,
            final_result=_sandbox_final_result(state, reason=game_end_reason),
        )
    return _new_sandbox_round_state(
        state,
        seed=seed,
        dealer_seat=next_dealer,
        round_wind=next_round_wind,
        honba=next_honba,
    )


def draw_for_current_seat(
    state: SandboxEnvironmentState,
    *,
    stop_on_tsumo: bool = False,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("pending reactions must be resolved before drawing")
    if state.needs_discard:
        raise ValueError("current seat must discard before drawing")
    if state.drawn_tile is not None:
        raise ValueError("current seat has already drawn")
    if not state.wall:
        return _replace_state(state, **_terminal_wall_exhausted_updates(state))

    draw = state.wall[-1]
    next_wall = state.wall[:-1]
    hands = [list(hand) for hand in state.hands]
    hands[state.current_seat].append(draw)
    next_state = _replace_state(
        state,
        wall=next_wall,
        hands=tuple(tuple(hand) for hand in hands),
        drawn_tile=draw,
        rinshan_draw=False,
        last_draw_was_final_live_wall=(len(state.wall) == 1 and state.turn > 0),
        temporary_furiten_seats=_without_seat(
            state.temporary_furiten_seats,
            state.current_seat,
        ),
    )
    if stop_on_tsumo:
        legal_tsumo = legal_tsumo_actions(next_state)
        if legal_tsumo:
            return apply_tsumo_action(next_state, legal_tsumo[0])
    return next_state


def legal_sandbox_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int | None = None,
    include_tsumo: bool = True,
    include_riichi: bool = True,
    include_ankan: bool = True,
    include_kakan: bool = True,
    include_kita: bool = True,
    include_kyuushu: bool = True,
    include_ron: bool = True,
    include_calls: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if state.pending_discard is not None:
        if seat is None:
            raise ValueError("seat is required for pending discard reactions")
        return legal_reaction_actions(
            state,
            seat=seat,
            include_ron=include_ron,
            include_calls=include_calls,
        )
    if state.pending_chankan_tile is not None:
        if seat is None:
            raise ValueError("seat is required for pending chankan reactions")
        return legal_chankan_reaction_actions(
            state,
            seat=seat,
            include_ron=include_ron,
        )
    if state.pending_kita_tile is not None:
        if seat is None:
            raise ValueError("seat is required for pending kita reactions")
        return legal_kita_reaction_actions(
            state,
            seat=seat,
            include_ron=include_ron,
        )
    if state.drawn_tile is None and not state.needs_discard:
        raise ValueError("current seat must draw before acting")
    if seat is not None and seat != state.current_seat:
        raise ValueError("seat does not match current acting seat")
    actions: list[Action] = []
    if not state.needs_discard:
        if include_tsumo:
            actions.extend(legal_tsumo_actions(state))
        if include_riichi:
            actions.extend(legal_riichi_actions(state))
        if include_ankan:
            actions.extend(legal_ankan_actions(state))
        if include_kakan:
            actions.extend(legal_kakan_actions(state))
        if include_kita:
            actions.extend(legal_kita_actions(state))
        if include_kyuushu:
            actions.extend(legal_kyuushu_kyuuhai_actions(state))
    actions.extend(legal_discard_actions(state))
    return tuple(actions)


def legal_tsumo_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot tsumo during a pending reaction")
    if state.drawn_tile is None:
        raise ValueError("current seat must draw before tsumo")
    shapes = _winning_shapes_for_state(state, seat=state.current_seat)
    if not shapes:
        return ()
    if not _winning_yaku_for_state(
        state,
        seat=state.current_seat,
        winning_tile=None,
        shapes=shapes,
        win_kind="tsumo",
    ):
        return ()
    return (Action(ActionKind.TSUMO),)


def legal_riichi_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot riichi during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before riichi")
    if _is_riichi(state, seat=state.current_seat):
        return ()
    if _melds_by_seat(state)[state.current_seat]:
        return ()
    if _points_by_seat(state)[state.current_seat] < RIICHI_DEPOSIT_POINTS:
        return ()
    if not state.wall:
        return ()
    if not _has_riichi_tenpai_discard(state):
        return ()
    return (Action(ActionKind.RIICHI),)


def legal_ankan_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot closed kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before closed kan")

    rules = SANDBOX_RULESET_BY_NAME[state.ruleset]
    hand = state.current_hand()
    counts = _hand_type_counts(hand)
    actions: list[Action] = []
    for tile_type in rules.tile_types:
        if counts[tile_type.index] >= 4:
            if _is_riichi(
                state,
                seat=state.current_seat,
            ) and not _post_riichi_ankan_preserves_waits(state, tile_type=tile_type):
                continue
            actions.append(
                Action(
                    ActionKind.ANKAN,
                    tile_type,
                    consumed=_first_tiles_of_type(hand, tile_type, 4),
                )
            )
    return tuple(actions)


def legal_kakan_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot added kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before added kan")
    if _is_riichi(state, seat=state.current_seat):
        return ()

    hand = state.current_hand()
    counts = _hand_type_counts(hand)
    promotable_types = sorted(
        {
            tile_type
            for meld in _melds_by_seat(state)[state.current_seat]
            if meld.kind is ActionKind.PON
            for tile_type in (_pon_meld_tile_type(meld),)
            if counts[tile_type.index] >= 1
        },
        key=lambda tile_type: tile_type.index,
    )
    return tuple(
        Action(
            ActionKind.KAKAN,
            tile_type,
            consumed=_first_tiles_of_type(hand, tile_type, 1),
        )
        for tile_type in promotable_types
    )


def legal_kita_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot call kita during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before kita")
    if state.ruleset != TENHOU_3P.name:
        return ()
    if _is_riichi(state, seat=state.current_seat):
        if state.drawn_tile.type != SANDBOX_KITA_TILE:
            return ()
        return (
            Action(
                ActionKind.KITA,
                SANDBOX_KITA_TILE,
                consumed=(state.drawn_tile,),
            ),
        )

    if not any(tile.type == SANDBOX_KITA_TILE for tile in state.current_hand()):
        return ()
    north_tiles = _first_tiles_of_type(state.current_hand(), SANDBOX_KITA_TILE, 1)
    return (
        Action(
            ActionKind.KITA,
            SANDBOX_KITA_TILE,
            consumed=north_tiles,
        ),
    )


def legal_kyuushu_kyuuhai_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot declare kyuushu kyuuhai during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before kyuushu kyuuhai")
    if not _is_kyuushu_kyuuhai_abortive_draw(state):
        return ()
    return (Action(ActionKind.KYUSHU),)


def legal_discard_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("pending reactions must be resolved before discarding")
    if state.drawn_tile is None and not state.needs_discard:
        raise ValueError("current seat must draw before discarding")
    if _is_post_riichi_discard_locked(state, seat=state.current_seat):
        if state.drawn_tile is None:
            raise ValueError("post-riichi discard requires a drawn tile")
        return (Action.discard(state.drawn_tile.type, tsumogiri=True),)
    tile_types = sorted({tile.type for tile in state.current_hand()}, key=lambda tile: tile.index)
    return tuple(Action.discard(tile_type) for tile_type in tile_types)


def apply_riichi_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot riichi during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before riichi")
    if action.kind is not ActionKind.RIICHI:
        raise ValueError("sandbox environment only supports riichi actions here")
    if action not in legal_riichi_actions(state):
        raise ValueError("riichi action is not legal for this state")
    points = list(_points_by_seat(state))
    points[state.current_seat] -= RIICHI_DEPOSIT_POINTS
    double_riichi_seats = state.double_riichi_seats
    if _is_double_riichi_declaration(state):
        double_riichi_seats = _with_seat(double_riichi_seats, state.current_seat)
    return _replace_state(
        state,
        points=tuple(points),
        riichi_sticks=state.riichi_sticks + 1,
        riichi_seats=_with_seat(state.riichi_seats, state.current_seat),
        double_riichi_seats=double_riichi_seats,
        riichi_pending_discard_seats=_with_seat(
            state.riichi_pending_discard_seats,
            state.current_seat,
        ),
        ippatsu_seats=_with_seat(state.ippatsu_seats, state.current_seat),
    )


def apply_ankan_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[SandboxEnvironmentState, Meld]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot closed kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before closed kan")
    if action.kind is not ActionKind.ANKAN:
        raise ValueError("sandbox environment only supports closed kan actions here")
    if action not in legal_ankan_actions(state):
        raise ValueError("closed kan action is not legal for this state")

    hands = [list(hand) for hand in state.hands]
    for tile in action.consumed:
        _remove_tile(hands[state.current_seat], tile)

    meld = Meld(
        kind=ActionKind.ANKAN,
        tiles=action.consumed,
        called_tile=None,
        from_seat=None,
    )
    melds = [list(seat_melds) for seat_melds in _melds_by_seat(state)]
    melds[state.current_seat].append(meld)
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "melds": tuple(tuple(seat_melds) for seat_melds in melds),
        "drawn_tile": None,
        "rinshan_draw": False,
        "last_draw_was_final_live_wall": False,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_reaction_seats": (),
    }

    chankan_seats = _legal_kokushi_ron_seats_for_tile(
        state,
        tile=action.consumed[0],
        candidate_seats=tuple(seat for seat in range(state.players) if seat != state.current_seat),
    )
    if chankan_seats:
        updates["pending_chankan_tile"] = action.consumed[0]
        updates["pending_chankan_seat"] = state.current_seat
        updates["pending_chankan_kind"] = ActionKind.ANKAN
        updates["pending_reaction_seats"] = chankan_seats
        return _replace_state(state, **updates), meld

    updates["ippatsu_seats"] = ()
    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
    )
    _set_four_kans_abortive_draw_after_discard(
        updates,
        melds=tuple(tuple(seat_melds) for seat_melds in melds),
    )

    return _replace_state(state, **updates), meld


def apply_kakan_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[SandboxEnvironmentState, Meld]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot added kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before added kan")
    if action.kind is not ActionKind.KAKAN:
        raise ValueError("sandbox environment only supports added kan actions here")
    if action not in legal_kakan_actions(state):
        raise ValueError("added kan action is not legal for this state")

    hands = [list(hand) for hand in state.hands]
    for tile in action.consumed:
        _remove_tile(hands[state.current_seat], tile)

    melds = [list(seat_melds) for seat_melds in _melds_by_seat(state)]
    promoted_meld = _promote_pon_meld_to_kakan(
        melds[state.current_seat],
        tile_type=_action_tile(action),
        added_tile=action.consumed[0],
    )
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "melds": tuple(tuple(seat_melds) for seat_melds in melds),
        "drawn_tile": None,
        "rinshan_draw": False,
        "last_draw_was_final_live_wall": False,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_reaction_seats": (),
    }

    chankan_seats = _legal_ron_seats_for_tile(
        state,
        tile=action.consumed[0],
        candidate_seats=tuple(seat for seat in range(state.players) if seat != state.current_seat),
        win_kind="chankan",
    )
    if chankan_seats:
        updates["pending_chankan_tile"] = action.consumed[0]
        updates["pending_chankan_seat"] = state.current_seat
        updates["pending_chankan_kind"] = ActionKind.KAKAN
        updates["pending_reaction_seats"] = chankan_seats
        return _replace_state(state, **updates), promoted_meld

    updates["ippatsu_seats"] = ()
    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
    )
    _set_four_kans_abortive_draw_after_discard(
        updates,
        melds=tuple(tuple(seat_melds) for seat_melds in melds),
    )

    return _replace_state(state, **updates), promoted_meld


def apply_kita_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot call kita during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before kita")
    if action.kind is not ActionKind.KITA:
        raise ValueError("sandbox environment only supports kita actions here")
    if action not in legal_kita_actions(state):
        raise ValueError("kita action is not legal for this state")

    hands = [list(hand) for hand in state.hands]
    kita_tile = action.consumed[0] if action.consumed else Tile(SANDBOX_KITA_TILE)
    _remove_tile(hands[state.current_seat], kita_tile)
    kita_tiles = [list(seat_tiles) for seat_tiles in _kita_tiles_by_seat(state)]
    kita_tiles[state.current_seat].append(kita_tile)
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "kita_tiles": tuple(tuple(seat_tiles) for seat_tiles in kita_tiles),
        "drawn_tile": None,
        "rinshan_draw": False,
        "last_draw_was_final_live_wall": False,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_chankan_tile": None,
        "pending_chankan_seat": None,
        "pending_chankan_kind": None,
        "pending_kita_tile": None,
        "pending_kita_seat": None,
        "pending_reaction_seats": (),
    }
    kita_ron_seats = _legal_ron_seats_for_tile(
        state,
        tile=kita_tile,
        candidate_seats=tuple(seat for seat in range(state.players) if seat != state.current_seat),
        win_kind="ron",
    )
    if kita_ron_seats:
        updates["pending_kita_tile"] = kita_tile
        updates["pending_kita_seat"] = state.current_seat
        updates["pending_reaction_seats"] = kita_ron_seats
        return _replace_state(state, **updates)

    updates["ippatsu_seats"] = ()
    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
        reveal_kan_dora=False,
    )
    return _replace_state(state, **updates)


def apply_discard_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[SandboxEnvironmentState, Tile]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("pending reactions must be resolved before discarding")
    if state.drawn_tile is None and not state.needs_discard:
        raise ValueError("current seat must draw before discarding")
    if action.kind is not ActionKind.DISCARD or action.tile is None:
        raise ValueError("sandbox environment only supports discard actions")
    if _is_post_riichi_discard_locked(state, seat=state.current_seat):
        if state.drawn_tile is None:
            raise ValueError("post-riichi discard requires a drawn tile")
        if action.tile != state.drawn_tile.type or not action.tsumogiri:
            raise ValueError("post-riichi discard must be a tsumogiri of the drawn tile")

    hands = [list(hand) for hand in state.hands]
    hand = hands[state.current_seat]
    discard_index = _discard_index(hand, action.tile)
    discard = hand.pop(discard_index)
    discards = [list(seat_discards) for seat_discards in _discards_by_seat(state)]
    discards[state.current_seat].append(discard)
    next_seat = (state.current_seat + 1) % state.players
    ippatsu_seats = state.ippatsu_seats
    if _is_post_riichi_discard_locked(state, seat=state.current_seat):
        ippatsu_seats = _without_seat(ippatsu_seats, state.current_seat)
    pending_riichi_declaration_discard_seat = (
        state.current_seat
        if state.current_seat in state.riichi_pending_discard_seats
        else None
    )
    pending_abortive_draw_reason = _abortive_draw_reason_after_discard(
        state,
        discards=tuple(tuple(seat_discards) for seat_discards in discards),
    )
    next_state = _replace_state(
        state,
        hands=tuple(tuple(player_hand) for player_hand in hands),
        discards=tuple(tuple(seat_discards) for seat_discards in discards),
        current_seat=next_seat,
        turn=state.turn + 1,
        drawn_tile=None,
        rinshan_draw=False,
        last_draw_was_final_live_wall=(
            state.last_draw_was_final_live_wall
            and state.drawn_tile is not None
            and not state.rinshan_draw
        ),
        needs_discard=False,
        pending_discard=discard,
        pending_discard_seat=state.current_seat,
        pending_riichi_declaration_discard_seat=pending_riichi_declaration_discard_seat,
        pending_abortive_draw_reason=pending_abortive_draw_reason,
        abortive_draw_after_discard_reason=None,
        pending_reaction_seats=tuple(
            seat for seat in range(state.players) if seat != state.current_seat
        ),
        riichi_pending_discard_seats=_without_seat(
            state.riichi_pending_discard_seats,
            state.current_seat,
        ),
        ippatsu_seats=ippatsu_seats,
    )
    return next_state, discard


def apply_kyuushu_kyuuhai_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot declare kyuushu kyuuhai during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before kyuushu kyuuhai")
    if action.kind is not ActionKind.KYUSHU:
        raise ValueError("sandbox environment only supports kyuushu kyuuhai actions here")
    if action not in legal_kyuushu_kyuuhai_actions(state):
        raise ValueError("kyuushu kyuuhai action is not legal for this state")
    return _replace_state(
        state,
        **_terminal_abortive_draw_updates(state, reason="kyuushu_kyuuhai"),
    )


def legal_reaction_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    include_ron: bool = True,
    include_calls: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    actions: list[Action] = []
    if include_ron:
        actions.extend(legal_ron_actions(state, seat=seat))
    if include_calls:
        actions.extend(legal_call_actions(state, seat=seat))
    actions.append(Action.pass_())
    return tuple(actions)


def legal_chankan_reaction_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    include_ron: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_chankan(state)
    _require_reaction_seat(state, seat)
    actions: list[Action] = []
    if include_ron:
        actions.extend(legal_chankan_ron_actions(state, seat=seat))
    actions.append(Action.pass_())
    return tuple(actions)


def legal_kita_reaction_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    include_ron: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_kita(state)
    _require_reaction_seat(state, seat)
    actions: list[Action] = []
    if include_ron:
        actions.extend(legal_kita_ron_actions(state, seat=seat))
    actions.append(Action.pass_())
    return tuple(actions)


def legal_call_actions(state: SandboxEnvironmentState, *, seat: int) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    if state.pending_abortive_draw_reason is not None:
        return ()
    pending_discard = _pending_discard(state)
    pending_discard_seat = _pending_discard_seat(state)
    if _is_riichi(state, seat=seat):
        return ()
    hand = state.hands[seat]
    counts = _hand_type_counts(hand)
    actions: list[Action] = []

    if state.ruleset != TENHOU_3P.name:
        actions.extend(
            _legal_chi_actions(
                hand=hand,
                pending_discard=pending_discard,
                seat=seat,
                pending_discard_seat=pending_discard_seat,
                players=state.players,
            )
        )

    matching = counts[pending_discard.type.index]
    if matching >= 2:
        actions.append(
            Action(
                ActionKind.PON,
                pending_discard.type,
                consumed=_first_tiles_of_type(hand, pending_discard.type, 2),
            )
        )
    if matching >= 3:
        actions.append(
            Action(
                ActionKind.MINKAN,
                pending_discard.type,
                consumed=_first_tiles_of_type(hand, pending_discard.type, 3),
            )
        )
    return tuple(actions)


def legal_ron_actions(state: SandboxEnvironmentState, *, seat: int) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    pending_discard = _pending_discard(state)
    if not _can_ron_tile(state, seat=seat, tile=pending_discard, win_kind="ron"):
        return ()
    return (Action(ActionKind.RON, pending_discard.type),)


def legal_chankan_ron_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_chankan(state)
    _require_reaction_seat(state, seat)
    pending_chankan = _pending_chankan_tile(state)
    can_ron = (
        _can_kokushi_ron_tile(state, seat=seat, tile=pending_chankan)
        if state.pending_chankan_kind is ActionKind.ANKAN
        else _can_ron_tile(state, seat=seat, tile=pending_chankan, win_kind="chankan")
    )
    if not can_ron:
        return ()
    return (Action(ActionKind.RON, pending_chankan.type),)


def legal_kita_ron_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_kita(state)
    _require_reaction_seat(state, seat)
    pending_kita = _pending_kita_tile(state)
    if not _can_ron_tile(state, seat=seat, tile=pending_kita, win_kind="ron"):
        return ()
    return (Action(ActionKind.RON, pending_kita.type),)


def apply_call_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action,
) -> tuple[SandboxEnvironmentState, Meld]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    ron_seats = _pending_ron_seats(state)
    if ron_seats:
        raise ValueError(
            "call action is blocked while ron reactions are pending: "
            + ",".join(str(ron_seat) for ron_seat in ron_seats)
        )
    if action not in legal_call_actions(state, seat=seat):
        raise ValueError("call action is not legal for this pending discard")
    pending_discard = _pending_discard(state)
    pending_discard_seat = _pending_discard_seat(state)
    hands = [list(hand) for hand in state.hands]
    for tile in action.consumed:
        _remove_tile(hands[seat], tile)

    meld = Meld(
        kind=action.kind,
        tiles=(*action.consumed, pending_discard),
        called_tile=pending_discard,
        from_seat=pending_discard_seat,
    )
    melds = [list(seat_melds) for seat_melds in _melds_by_seat(state)]
    melds[seat].append(meld)
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "melds": tuple(tuple(seat_melds) for seat_melds in melds),
        "current_seat": seat,
        "drawn_tile": None,
        "rinshan_draw": False,
        "last_draw_was_final_live_wall": False,
        "needs_discard": action.kind is not ActionKind.MINKAN,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_riichi_declaration_discard_seat": None,
        "pending_reaction_seats": (),
        "ippatsu_seats": (),
    }

    if action.kind is ActionKind.MINKAN:
        _apply_kan_replacement_draw(
            state,
            hands=hands,
            updates=updates,
            seat=seat,
        )
        _set_four_kans_abortive_draw_after_discard(
            updates,
            melds=tuple(tuple(seat_melds) for seat_melds in melds),
        )

    return _replace_state(state, **updates), meld


def apply_reaction_pass_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action | None = None,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    _require_pending_reaction(state)
    _require_reaction_seat(state, seat)
    if action is not None and action.kind is not ActionKind.PASS:
        raise ValueError("reaction pass requires a pass action")
    temporary_furiten_seats = state.temporary_furiten_seats
    riichi_furiten_seats = state.riichi_furiten_seats
    legal_ron = (
        legal_chankan_ron_actions(state, seat=seat)
        if state.pending_chankan_tile is not None
        else (
            legal_kita_ron_actions(state, seat=seat)
            if state.pending_kita_tile is not None
            else legal_ron_actions(state, seat=seat)
        )
    )
    if legal_ron:
        if _is_riichi(state, seat=seat):
            riichi_furiten_seats = _with_seat(riichi_furiten_seats, seat)
        else:
            temporary_furiten_seats = _with_seat(temporary_furiten_seats, seat)
    remaining = tuple(
        reaction_seat for reaction_seat in state.pending_reaction_seats if reaction_seat != seat
    )
    if remaining:
        return _replace_state(
            state,
            pending_reaction_seats=remaining,
            temporary_furiten_seats=temporary_furiten_seats,
            riichi_furiten_seats=riichi_furiten_seats,
        )
    if state.pending_chankan_tile is not None:
        return _finish_chankan_reaction_window(
            state,
            temporary_furiten_seats=temporary_furiten_seats,
            riichi_furiten_seats=riichi_furiten_seats,
        )
    if state.pending_kita_tile is not None:
        return _finish_kita_reaction_window(
            state,
            temporary_furiten_seats=temporary_furiten_seats,
            riichi_furiten_seats=riichi_furiten_seats,
        )
    if state.pending_abortive_draw_reason is not None:
        return _replace_state(
            state,
            **_terminal_abortive_draw_updates(
                state,
                reason=state.pending_abortive_draw_reason,
            ),
        )
    return _replace_state(
        state,
        pending_discard=None,
        pending_discard_seat=None,
        pending_riichi_declaration_discard_seat=None,
        pending_abortive_draw_reason=None,
        pending_reaction_seats=(),
        temporary_furiten_seats=temporary_furiten_seats,
        riichi_furiten_seats=riichi_furiten_seats,
        last_draw_was_final_live_wall=False,
    )


def apply_ron_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action,
) -> SandboxEnvironmentState:
    return apply_ron_actions(state, ((seat, action),))


def apply_ron_actions(
    state: SandboxEnvironmentState,
    seat_actions: tuple[tuple[int, Action], ...],
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    _require_pending_reaction(state)
    pending_tile = _pending_ron_tile(state)
    pending_source_seat = _pending_ron_source_seat(state)
    terminal_reason = "chankan" if state.pending_chankan_tile is not None else "ron"
    if not seat_actions:
        raise ValueError("at least one ron action is required")

    seen: set[int] = set()
    winner_seats: list[int] = []
    winning_shapes_by_seat: list[tuple[int, tuple[str, ...]]] = []
    winning_yaku_by_seat: list[tuple[int, tuple[str, ...]]] = []
    for seat, action in seat_actions:
        _require_reaction_seat(state, seat)
        if seat in seen:
            raise ValueError(f"duplicate ron reaction seat: {seat}")
        seen.add(seat)
        if action.kind is not ActionKind.RON or action.tile != pending_tile.type:
            raise ValueError("sandbox environment only supports matching ron actions here")
        shapes = _winning_shapes_for_state(state, seat=seat, winning_tile=pending_tile)
        if not shapes:
            raise ValueError("reacting hand is not a winning ron")
        if state.pending_chankan_kind is ActionKind.ANKAN and "kokushi" not in shapes:
            raise ValueError("concealed kan can only be robbed by kokushi")
        yaku = _winning_yaku_for_state(
            state,
            seat=seat,
            winning_tile=pending_tile,
            shapes=shapes,
            win_kind=terminal_reason,
        )
        if not yaku:
            raise ValueError("winning hand has no recognized sandbox yaku")
        if _is_discard_furiten(state, seat=seat):
            raise ValueError("reacting hand is in discard furiten")
        if _is_temporary_furiten(state, seat=seat):
            raise ValueError("reacting hand is in temporary furiten")
        if _is_riichi_furiten(state, seat=seat):
            raise ValueError("reacting hand is in riichi furiten")
        winner_seats.append(seat)
        winning_shapes_by_seat.append((seat, shapes))
        winning_yaku_by_seat.append((seat, yaku))

    winner_order = _reaction_priority_by_source(
        source_seat=pending_source_seat,
        players=state.players,
    )
    winner_priority = {seat: priority for priority, seat in enumerate(winner_order)}
    winner_seats = sorted(winner_seats, key=winner_priority.__getitem__)
    winning_shapes_by_seat = sorted(
        winning_shapes_by_seat,
        key=lambda seat_shapes: winner_priority[seat_shapes[0]],
    )
    winning_yaku_by_seat = sorted(
        winning_yaku_by_seat,
        key=lambda seat_yaku: winner_priority[seat_yaku[0]],
    )

    if len(winner_seats) == 3:
        return _replace_state(
            state,
            **_terminal_abortive_draw_updates(state, reason="triple_ron"),
        )

    point_updates = _terminal_win_point_updates(
        state,
        winner_seats=tuple(winner_seats),
        discarder_seat=pending_source_seat,
        winning_tile=pending_tile,
        win_kind=terminal_reason,
        winning_yaku_by_seat=tuple(winning_yaku_by_seat),
        riichi_declaration_discard_seat=state.pending_riichi_declaration_discard_seat,
    )
    return _replace_state(
        state,
        pending_discard=None,
        pending_discard_seat=None,
        pending_chankan_tile=None,
        pending_chankan_seat=None,
        pending_chankan_kind=None,
        pending_kita_tile=None,
        pending_kita_seat=None,
        pending_riichi_declaration_discard_seat=None,
        pending_abortive_draw_reason=None,
        abortive_draw_after_discard_reason=None,
        pending_reaction_seats=(),
        terminal_reason=terminal_reason,
        winner_seat=winner_seats[0],
        winner_seats=tuple(winner_seats),
        winning_tile=pending_tile,
        winning_shapes=winning_shapes_by_seat[0][1],
        winning_shapes_by_seat=tuple(winning_shapes_by_seat),
        winning_yaku=winning_yaku_by_seat[0][1],
        winning_yaku_by_seat=tuple(winning_yaku_by_seat),
        winning_ippatsu_seats=tuple(
            seat for seat in winner_seats if seat in state.ippatsu_seats
        ),
        winning_rinshan_seats=(),
        ippatsu_seats=(),
        terminal_rewards=_multi_ron_rewards(
            winner_seats=tuple(winner_seats),
            discarder_seat=pending_source_seat,
            players=state.players,
        ),
        **point_updates,
    )


def pass_pending_discard_reactions(state: SandboxEnvironmentState) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    _require_pending_discard(state)
    temporary_furiten_seats = state.temporary_furiten_seats
    riichi_furiten_seats = state.riichi_furiten_seats
    for seat in state.pending_reaction_seats:
        if legal_ron_actions(state, seat=seat):
            if _is_riichi(state, seat=seat):
                riichi_furiten_seats = _with_seat(riichi_furiten_seats, seat)
            else:
                temporary_furiten_seats = _with_seat(temporary_furiten_seats, seat)
    if state.pending_abortive_draw_reason is not None:
        return _replace_state(
            state,
            **_terminal_abortive_draw_updates(
                state,
                reason=state.pending_abortive_draw_reason,
            ),
        )
    return _replace_state(
        state,
        pending_discard=None,
        pending_discard_seat=None,
        pending_riichi_declaration_discard_seat=None,
        pending_abortive_draw_reason=None,
        pending_reaction_seats=(),
        temporary_furiten_seats=temporary_furiten_seats,
        riichi_furiten_seats=riichi_furiten_seats,
        last_draw_was_final_live_wall=False,
    )


def apply_tsumo_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot tsumo during a pending reaction")
    if state.drawn_tile is None:
        raise ValueError("current seat must draw before tsumo")
    if action.kind is not ActionKind.TSUMO:
        raise ValueError("sandbox environment only supports tsumo actions here")
    shapes = _winning_shapes_for_state(state, seat=state.current_seat)
    if not shapes:
        raise ValueError("current hand is not a winning tsumo")
    yaku = _winning_yaku_for_state(
        state,
        seat=state.current_seat,
        winning_tile=None,
        shapes=shapes,
        win_kind="tsumo",
    )
    if not yaku:
        raise ValueError("winning hand has no recognized sandbox yaku")
    point_updates = _terminal_win_point_updates(
        state,
        winner_seats=(state.current_seat,),
        discarder_seat=None,
        winning_tile=None,
        win_kind="tsumo",
        winning_yaku_by_seat=((state.current_seat, yaku),),
    )
    return _replace_state(
        state,
        terminal_reason="tsumo",
        winner_seat=state.current_seat,
        winner_seats=(state.current_seat,),
        winning_tile=state.drawn_tile,
        winning_shapes=shapes,
        winning_shapes_by_seat=((state.current_seat, shapes),),
        winning_yaku=yaku,
        winning_yaku_by_seat=((state.current_seat, yaku),),
        winning_ippatsu_seats=(
            (state.current_seat,) if state.current_seat in state.ippatsu_seats else ()
        ),
        winning_rinshan_seats=((state.current_seat,) if state.rinshan_draw else ()),
        ippatsu_seats=(),
        terminal_rewards=_tsumo_rewards(state.current_seat, state.players),
        **point_updates,
    )


def resolve_sandbox_ruleset(ruleset: str) -> RuleSet:
    try:
        return SANDBOX_RULESET_BY_NAME[ruleset]
    except KeyError as error:
        raise ValueError("unsupported sandbox environment ruleset: " + ruleset) from error


def _discard_index(hand: list[Tile], tile_type: TileType) -> int:
    for index, tile in enumerate(hand):
        if tile.type == tile_type:
            return index
    raise ValueError(f"discard tile is not in current hand: {tile_type.notation}")


def _action_tile(action: Action) -> TileType:
    if action.tile is None:
        raise ValueError(f"{action.kind.value} action requires a tile")
    return action.tile


def _promote_pon_meld_to_kakan(
    melds: list[Meld],
    *,
    tile_type: TileType,
    added_tile: Tile,
) -> Meld:
    for index, meld in enumerate(melds):
        if meld.kind is not ActionKind.PON or _pon_meld_tile_type(meld) != tile_type:
            continue
        promoted = Meld(
            kind=ActionKind.KAKAN,
            tiles=(*meld.tiles, added_tile),
            called_tile=meld.called_tile,
            from_seat=meld.from_seat,
        )
        melds[index] = promoted
        return promoted
    raise ValueError(f"no pon meld can be promoted for {tile_type.notation}")


def _pon_meld_tile_type(meld: Meld) -> TileType:
    if meld.kind is not ActionKind.PON:
        raise ValueError("meld is not a pon")
    if meld.called_tile is not None:
        return meld.called_tile.type
    return meld.tiles[0].type


def _remove_tile(hand: list[Tile], tile: Tile) -> None:
    for index, candidate in enumerate(hand):
        if candidate == tile:
            del hand[index]
            return

    for index, candidate in enumerate(hand):
        if candidate.type == tile.type:
            del hand[index]
            return

    raise ValueError(f"tile is not in current hand: {tile.notation}")


def _hand_without_exact_tile(hand: tuple[Tile, ...], tile: Tile) -> tuple[Tile, ...] | None:
    remaining = list(hand)
    try:
        remaining.remove(tile)
    except ValueError:
        return None
    return tuple(remaining)


def _hand_without_tiles(
    hand: tuple[Tile, ...],
    tiles: tuple[Tile, ...],
) -> tuple[Tile, ...] | None:
    remaining = list(hand)
    for tile in tiles:
        try:
            remaining.remove(tile)
        except ValueError:
            for index, candidate in enumerate(remaining):
                if candidate.type == tile.type:
                    del remaining[index]
                    break
            else:
                return None
    return tuple(remaining)


def _winning_shapes_for_complete_tiles(tiles: tuple[Tile, ...]) -> tuple[str, ...]:
    if len(tiles) != 14:
        return ()
    return winning_hand_shapes_for_tiles(tiles)


def _winning_shapes_for_state(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None = None,
) -> tuple[str, ...]:
    concealed_tiles = state.hands[seat]
    if winning_tile is not None:
        concealed_tiles = (*concealed_tiles, winning_tile)
    return _winning_shapes_for_concealed_and_melds(
        concealed_tiles,
        _melds_by_seat(state)[seat],
    )


def _winning_shapes_for_concealed_and_melds(
    concealed_tiles: tuple[Tile, ...],
    melds: tuple[Meld, ...],
) -> tuple[str, ...]:
    if not melds:
        return _winning_shapes_for_complete_tiles(concealed_tiles)

    full_tiles = (*concealed_tiles, *_standard_shape_tiles_for_melds(melds))
    if len(full_tiles) != 14:
        return ()
    shapes = winning_hand_shapes_for_tiles(full_tiles)
    return tuple(shape for shape in shapes if shape == "standard")


def _winning_yaku_for_state(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
    shapes: tuple[str, ...],
    win_kind: str,
) -> tuple[str, ...]:
    yaku: list[str] = []
    melds = _melds_by_seat(state)[seat]
    full_tiles = _full_yaku_tiles(state, seat=seat, winning_tile=winning_tile)

    if "kokushi" in shapes:
        yaku.append("kokushi")
    if "chiitoitsu" in shapes:
        yaku.append("chiitoitsu")
    if _is_double_riichi(state, seat=seat):
        yaku.append("double_riichi")
    elif _is_riichi(state, seat=seat):
        yaku.append("riichi")
    if seat in state.ippatsu_seats:
        yaku.append("ippatsu")
    if win_kind == "tsumo" and _is_closed_hand_for_yaku(melds):
        yaku.append("menzen_tsumo")
    if win_kind == "tsumo" and state.rinshan_draw:
        yaku.append("rinshan")
    if win_kind == "tsumo" and _is_haitei_draw(state):
        yaku.append("haitei")
    if win_kind == "ron" and _is_houtei_discard(state):
        yaku.append("houtei")
    if win_kind == "chankan":
        yaku.append("chankan")
    if _is_tanyao_yaku(full_tiles):
        yaku.append("tanyao")
    if _has_sandbox_yakuhai(
        full_tiles,
        ruleset=state.ruleset,
        seat=seat,
        dealer_seat=state.dealer_seat,
        round_wind=state.round_wind,
        players=state.players,
    ):
        yaku.append("yakuhai")
    if _is_toitoi_yaku(full_tiles, melds=melds, shapes=shapes):
        yaku.append("toitoi")
    if _is_honroutou_yaku(full_tiles, shapes=shapes):
        yaku.append("honroutou")
    return tuple(yaku)


def _full_yaku_tiles(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
) -> tuple[Tile, ...]:
    concealed_tiles = state.hands[seat]
    if winning_tile is not None:
        concealed_tiles = (*concealed_tiles, winning_tile)
    meld_tiles = tuple(tile for meld in _melds_by_seat(state)[seat] for tile in meld.tiles)
    return (*concealed_tiles, *meld_tiles)


def _is_closed_hand_for_yaku(melds: tuple[Meld, ...]) -> bool:
    return all(meld.kind is ActionKind.ANKAN for meld in melds)


def _is_haitei_draw(state: SandboxEnvironmentState) -> bool:
    return (
        state.last_draw_was_final_live_wall
        and state.drawn_tile is not None
        and not state.rinshan_draw
        and not state.wall
    )


def _is_houtei_discard(state: SandboxEnvironmentState) -> bool:
    return (
        state.last_draw_was_final_live_wall
        and state.pending_discard is not None
        and not state.wall
    )


def _is_tanyao_yaku(tiles: tuple[Tile, ...]) -> bool:
    return bool(tiles) and all(not tile.type.is_terminal_or_honor for tile in tiles)


def _has_sandbox_yakuhai(
    tiles: tuple[Tile, ...],
    *,
    ruleset: str,
    seat: int,
    dealer_seat: int,
    round_wind: TileType,
    players: int,
) -> bool:
    counts = _hand_type_counts(tiles)
    return any(
        _is_sandbox_yakuhai_type(
            TileType(index),
            ruleset=ruleset,
            seat=seat,
            dealer_seat=dealer_seat,
            round_wind=round_wind,
            players=players,
        )
        and count >= 3
        for index, count in enumerate(counts)
    )


def _is_toitoi_yaku(
    tiles: tuple[Tile, ...],
    *,
    melds: tuple[Meld, ...],
    shapes: tuple[str, ...],
) -> bool:
    if "standard" not in shapes:
        return False
    if any(meld.kind is ActionKind.CHI for meld in melds):
        return False
    positive_counts = tuple(count for count in _hand_type_counts(tiles) if count > 0)
    return positive_counts.count(2) == 1 and all(
        count in {2, 3, 4} for count in positive_counts
    )


def _is_honroutou_yaku(
    tiles: tuple[Tile, ...],
    *,
    shapes: tuple[str, ...],
) -> bool:
    if "standard" not in shapes and "chiitoitsu" not in shapes:
        return False
    if not tiles or any(not tile.type.is_terminal_or_honor for tile in tiles):
        return False
    has_terminal = any(tile.type.is_terminal for tile in tiles)
    has_honor = any(tile.type.is_honor for tile in tiles)
    return has_terminal and has_honor


def _is_sandbox_yakuhai_type(
    tile_type: TileType,
    *,
    ruleset: str,
    seat: int,
    dealer_seat: int,
    round_wind: TileType,
    players: int,
) -> bool:
    if ruleset == TENHOU_3P.name and tile_type == SANDBOX_KITA_TILE:
        return False
    if tile_type in SANDBOX_DRAGON_TILES:
        return True
    if tile_type == round_wind:
        return True
    return tile_type == _sandbox_seat_wind(
        seat=seat,
        dealer_seat=dealer_seat,
        players=players,
    )


def _sandbox_seat_wind(*, seat: int, dealer_seat: int, players: int) -> TileType:
    relative_seat = (seat - dealer_seat) % players
    return SANDBOX_SEAT_WINDS[relative_seat]


def _standard_shape_tiles_for_melds(melds: tuple[Meld, ...]) -> tuple[Tile, ...]:
    return tuple(tile for meld in melds for tile in meld.tiles[:3])


def _has_riichi_tenpai_discard(state: SandboxEnvironmentState) -> bool:
    hand = state.current_hand()
    if len(hand) != 14:
        return False
    for discard_type in {tile.type for tile in hand}:
        candidate = list(hand)
        discard_index = _discard_index(candidate, discard_type)
        candidate.pop(discard_index)
        if shanten_for_tiles(candidate) == 0:
            return True
    return False


def _post_riichi_ankan_preserves_waits(
    state: SandboxEnvironmentState,
    *,
    tile_type: TileType,
) -> bool:
    seat = state.current_seat
    if not _is_post_riichi_discard_locked(state, seat=seat):
        return False
    if state.drawn_tile is None or state.drawn_tile.type != tile_type:
        return False

    melds = _melds_by_seat(state)[seat]
    before_hand = _hand_without_exact_tile(state.current_hand(), state.drawn_tile)
    if before_hand is None:
        return False
    before_waits = _winning_wait_types_for_hand_and_melds(
        state,
        hand=before_hand,
        melds=melds,
    )
    if not before_waits:
        return False

    consumed = _first_tiles_of_type(state.current_hand(), tile_type, 4)
    after_hand = _hand_without_tiles(state.current_hand(), consumed)
    if after_hand is None:
        return False
    after_waits = _winning_wait_types_for_hand_and_melds(
        state,
        hand=after_hand,
        melds=(
            *melds,
            Meld(
                kind=ActionKind.ANKAN,
                tiles=consumed,
                called_tile=None,
                from_seat=None,
            ),
        ),
    )
    return set(before_waits) == set(after_waits)


def _abortive_draw_reason_after_discard(
    state: SandboxEnvironmentState,
    *,
    discards: tuple[tuple[Tile, ...], ...],
) -> str | None:
    if state.abortive_draw_after_discard_reason is not None:
        return state.abortive_draw_after_discard_reason
    if _is_four_winds_abortive_draw(state, discards=discards):
        return "four_winds"
    if _is_four_riichi_abortive_draw(state):
        return "four_riichi"
    return None


def _is_kyuushu_kyuuhai_abortive_draw(state: SandboxEnvironmentState) -> bool:
    if state.drawn_tile is None or state.needs_discard:
        return False
    first_turn = (state.current_seat - state.dealer_seat) % state.players
    if state.turn != first_turn:
        return False
    if any(_melds_by_seat(state)) or any(_kita_tiles_by_seat(state)):
        return False
    terminal_or_honor_types = {
        tile.type for tile in state.current_hand() if tile.type.is_terminal_or_honor
    }
    return len(terminal_or_honor_types) >= 9


def _is_four_winds_abortive_draw(
    state: SandboxEnvironmentState,
    *,
    discards: tuple[tuple[Tile, ...], ...],
) -> bool:
    if state.players != 4:
        return False
    if state.turn + 1 != state.players:
        return False
    if any(_melds_by_seat(state)) or any(_kita_tiles_by_seat(state)):
        return False
    if len(discards) != state.players or any(len(seat_discards) != 1 for seat_discards in discards):
        return False
    discard_types = tuple(seat_discards[0].type for seat_discards in discards)
    return discard_types[0] in SANDBOX_SEAT_WINDS and len(set(discard_types)) == 1


def _is_four_riichi_abortive_draw(state: SandboxEnvironmentState) -> bool:
    return (
        state.players == 4
        and state.current_seat in state.riichi_pending_discard_seats
        and len(state.riichi_seats) == state.players
    )


def _set_four_kans_abortive_draw_after_discard(
    updates: dict[str, Any],
    *,
    melds: tuple[tuple[Meld, ...], ...],
) -> None:
    if updates.get("terminal_reason") is not None:
        return
    if _is_four_kans_abortive_draw(melds):
        updates["abortive_draw_after_discard_reason"] = "four_kans"


def _is_four_kans_abortive_draw(melds: tuple[tuple[Meld, ...], ...]) -> bool:
    kan_seats = tuple(
        seat
        for seat, seat_melds in enumerate(melds)
        for meld in seat_melds
        if meld.kind in {ActionKind.MINKAN, ActionKind.ANKAN, ActionKind.KAKAN}
    )
    return len(kan_seats) >= 4 and len(set(kan_seats)) > 1


def _require_non_terminal(state: SandboxEnvironmentState) -> None:
    if state.terminal_reason is not None:
        raise ValueError("environment is already terminal")


def _has_pending_reaction(state: SandboxEnvironmentState) -> bool:
    return (
        state.pending_discard is not None
        or state.pending_chankan_tile is not None
        or state.pending_kita_tile is not None
    )


def _can_ron_tile(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    tile: Tile,
    win_kind: str,
) -> bool:
    shapes = _winning_shapes_for_state(state, seat=seat, winning_tile=tile)
    if not shapes:
        return False
    if not _winning_yaku_for_state(
        state,
        seat=seat,
        winning_tile=tile,
        shapes=shapes,
        win_kind=win_kind,
    ):
        return False
    if _is_discard_furiten(state, seat=seat):
        return False
    if _is_temporary_furiten(state, seat=seat):
        return False
    return not _is_riichi_furiten(state, seat=seat)


def _legal_ron_seats_for_tile(
    state: SandboxEnvironmentState,
    *,
    tile: Tile,
    candidate_seats: tuple[int, ...],
    win_kind: str,
) -> tuple[int, ...]:
    return tuple(
        seat
        for seat in candidate_seats
        if _can_ron_tile(state, seat=seat, tile=tile, win_kind=win_kind)
    )


def _legal_kokushi_ron_seats_for_tile(
    state: SandboxEnvironmentState,
    *,
    tile: Tile,
    candidate_seats: tuple[int, ...],
) -> tuple[int, ...]:
    return tuple(
        seat
        for seat in candidate_seats
        if _can_kokushi_ron_tile(state, seat=seat, tile=tile)
    )


def _reaction_priority_by_source(*, source_seat: int, players: int) -> tuple[int, ...]:
    return tuple((source_seat + offset) % players for offset in range(1, players))


def _can_kokushi_ron_tile(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    tile: Tile,
) -> bool:
    if "kokushi" not in _winning_shapes_for_state(state, seat=seat, winning_tile=tile):
        return False
    if _is_discard_furiten(state, seat=seat):
        return False
    if _is_temporary_furiten(state, seat=seat):
        return False
    return not _is_riichi_furiten(state, seat=seat)


def _apply_kan_replacement_draw(
    state: SandboxEnvironmentState,
    *,
    hands: list[list[Tile]],
    updates: dict[str, Any],
    seat: int,
    reveal_kan_dora: bool = True,
) -> None:
    if not 0 <= seat < state.players:
        raise ValueError("replacement draw seat outside player range")
    if not _has_dead_wall_replacement_tile(state):
        updates["terminal_reason"] = "wall_exhausted"
        updates["terminal_rewards"] = _neutral_rewards(state.players)
        updates["terminal_point_deltas"] = _neutral_point_deltas(state.players)
        updates["rinshan_draw"] = False
        updates["last_draw_was_final_live_wall"] = False
        return

    dora_indicators = state.dora_indicators
    if reveal_kan_dora:
        kan_dora = _next_kan_dora_indicator(state)
        if kan_dora is not None:
            dora_indicators = (*dora_indicators, kan_dora)

    replacement_draw = state.dead_wall[-1]
    hands[seat].append(replacement_draw)
    updates["dead_wall"] = state.dead_wall[:-1]
    updates["dora_indicators"] = dora_indicators
    updates["hands"] = tuple(tuple(hand) for hand in hands)
    updates["drawn_tile"] = replacement_draw
    updates["rinshan_draw"] = True
    updates["last_draw_was_final_live_wall"] = False


def _has_dead_wall_replacement_tile(state: SandboxEnvironmentState) -> bool:
    if (
        state.ruleset == TENHOU_3P.name
        and len(state.dead_wall) >= SANDBOX_3P_NON_REPLACEMENT_DEAD_WALL_TILES
    ):
        return len(state.dead_wall) > SANDBOX_3P_NON_REPLACEMENT_DEAD_WALL_TILES
    return len(state.dead_wall) > len(state.dora_indicators)


def _next_kan_dora_indicator(state: SandboxEnvironmentState) -> Tile | None:
    if len(state.dead_wall) - len(state.dora_indicators) < 2:
        return None
    return state.dead_wall[len(state.dora_indicators)]


def _finish_chankan_reaction_window(
    state: SandboxEnvironmentState,
    *,
    temporary_furiten_seats: tuple[int, ...],
    riichi_furiten_seats: tuple[int, ...],
) -> SandboxEnvironmentState:
    updates: dict[str, Any] = {
        "pending_chankan_tile": None,
        "pending_chankan_seat": None,
        "pending_chankan_kind": None,
        "pending_reaction_seats": (),
        "temporary_furiten_seats": temporary_furiten_seats,
        "riichi_furiten_seats": riichi_furiten_seats,
        "ippatsu_seats": (),
    }
    hands = [list(hand) for hand in state.hands]
    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
    )
    _set_four_kans_abortive_draw_after_discard(
        updates,
        melds=_melds_by_seat(state),
    )
    return _replace_state(state, **updates)


def _finish_kita_reaction_window(
    state: SandboxEnvironmentState,
    *,
    temporary_furiten_seats: tuple[int, ...],
    riichi_furiten_seats: tuple[int, ...],
) -> SandboxEnvironmentState:
    updates: dict[str, Any] = {
        "pending_kita_tile": None,
        "pending_kita_seat": None,
        "pending_reaction_seats": (),
        "temporary_furiten_seats": temporary_furiten_seats,
        "riichi_furiten_seats": riichi_furiten_seats,
        "ippatsu_seats": (),
    }
    hands = [list(hand) for hand in state.hands]
    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
        reveal_kan_dora=False,
    )
    return _replace_state(state, **updates)


def _replace_state(state: SandboxEnvironmentState, **updates: Any) -> SandboxEnvironmentState:
    payload = {
        "ruleset": state.ruleset,
        "players": state.players,
        "wall": state.wall,
        "hands": state.hands,
        "dead_wall": state.dead_wall,
        "dora_indicators": state.dora_indicators,
        "ura_dora_indicators": state.ura_dora_indicators,
        "discards": state.discards,
        "melds": state.melds,
        "kita_tiles": state.kita_tiles,
        "points": state.points,
        "riichi_sticks": state.riichi_sticks,
        "honba": state.honba,
        "dealer_seat": state.dealer_seat,
        "round_wind": state.round_wind,
        "current_seat": state.current_seat,
        "turn": state.turn,
        "drawn_tile": state.drawn_tile,
        "rinshan_draw": state.rinshan_draw,
        "last_draw_was_final_live_wall": state.last_draw_was_final_live_wall,
        "needs_discard": state.needs_discard,
        "pending_discard": state.pending_discard,
        "pending_discard_seat": state.pending_discard_seat,
        "pending_chankan_tile": state.pending_chankan_tile,
        "pending_chankan_seat": state.pending_chankan_seat,
        "pending_chankan_kind": state.pending_chankan_kind,
        "pending_kita_tile": state.pending_kita_tile,
        "pending_kita_seat": state.pending_kita_seat,
        "pending_abortive_draw_reason": state.pending_abortive_draw_reason,
        "abortive_draw_after_discard_reason": state.abortive_draw_after_discard_reason,
        "pending_reaction_seats": state.pending_reaction_seats,
        "temporary_furiten_seats": state.temporary_furiten_seats,
        "riichi_seats": state.riichi_seats,
        "double_riichi_seats": state.double_riichi_seats,
        "riichi_pending_discard_seats": state.riichi_pending_discard_seats,
        "pending_riichi_declaration_discard_seat": (
            state.pending_riichi_declaration_discard_seat
        ),
        "ippatsu_seats": state.ippatsu_seats,
        "riichi_furiten_seats": state.riichi_furiten_seats,
        "terminal_reason": state.terminal_reason,
        "winner_seat": state.winner_seat,
        "winner_seats": state.winner_seats,
        "winning_tile": state.winning_tile,
        "winning_shapes": state.winning_shapes,
        "winning_shapes_by_seat": state.winning_shapes_by_seat,
        "winning_yaku": state.winning_yaku,
        "winning_yaku_by_seat": state.winning_yaku_by_seat,
        "winning_ippatsu_seats": state.winning_ippatsu_seats,
        "winning_rinshan_seats": state.winning_rinshan_seats,
        "terminal_rewards": state.terminal_rewards,
        "terminal_point_deltas": state.terminal_point_deltas,
        "terminal_score_estimates": state.terminal_score_estimates,
        "exhaustive_draw_tenpai_seats": state.exhaustive_draw_tenpai_seats,
        "exhaustive_draw_noten_seats": state.exhaustive_draw_noten_seats,
        "final_result": state.final_result,
    }
    payload.update(updates)
    return SandboxEnvironmentState(**payload)


def _new_sandbox_round_state(
    state: SandboxEnvironmentState,
    *,
    seed: str | int,
    dealer_seat: int,
    round_wind: TileType,
    honba: int,
) -> SandboxEnvironmentState:
    rules = SANDBOX_RULESET_BY_NAME[state.ruleset]
    rng = random.Random(_seed_int(seed))
    wall = _shuffled_wall(rng, rules=rules)
    hands = tuple(tuple(wall.pop() for _tile in range(13)) for _seat in range(state.players))
    dead_wall = tuple(wall.pop() for _tile in range(SANDBOX_DEAD_WALL_TILES))
    return SandboxEnvironmentState(
        ruleset=state.ruleset,
        players=state.players,
        wall=tuple(wall),
        hands=hands,
        dead_wall=dead_wall,
        dora_indicators=dead_wall[:SANDBOX_INITIAL_DORA_INDICATORS],
        ura_dora_indicators=(),
        discards=tuple(() for _seat in range(state.players)),
        melds=tuple(() for _seat in range(state.players)),
        kita_tiles=tuple(() for _seat in range(state.players)),
        points=_points_by_seat(state),
        riichi_sticks=state.riichi_sticks,
        honba=honba,
        dealer_seat=dealer_seat,
        round_wind=round_wind,
        current_seat=dealer_seat,
    )


def _dealer_repeats_after_terminal(state: SandboxEnvironmentState) -> bool:
    if state.terminal_reason in SANDBOX_ABORTIVE_DRAW_REASONS:
        return True
    if state.terminal_reason in {"ron", "tsumo", "chankan", "nagashi_mangan"}:
        return state.dealer_seat in state.winner_seats
    if state.terminal_reason == "wall_exhausted":
        return state.dealer_seat in state.exhaustive_draw_tenpai_seats
    raise ValueError("unsupported terminal reason for next round: " + str(state.terminal_reason))


def _terminal_carries_honba(state: SandboxEnvironmentState) -> bool:
    if state.terminal_reason in SANDBOX_ABORTIVE_DRAW_REASONS:
        return True
    if state.terminal_reason == "wall_exhausted":
        return True
    if state.terminal_reason in {"ron", "tsumo", "chankan", "nagashi_mangan"}:
        return state.dealer_seat in state.winner_seats
    raise ValueError(
        "unsupported terminal reason for honba progression: " + str(state.terminal_reason)
    )


def _next_round_wind_after_terminal(
    state: SandboxEnvironmentState,
    *,
    dealer_repeats: bool,
    next_dealer: int,
) -> TileType:
    if dealer_repeats or next_dealer != 0:
        return state.round_wind
    round_index = SANDBOX_ROUND_WINDS.index(state.round_wind)
    return SANDBOX_ROUND_WINDS[(round_index + 1) % len(SANDBOX_ROUND_WINDS)]


def _sandbox_game_end_reason(
    state: SandboxEnvironmentState,
    *,
    dealer_repeats: bool,
    next_dealer: int,
    next_round_wind: TileType,
) -> str | None:
    points = _points_by_seat(state)
    if any(point < 0 for point in points):
        return "bankruptcy"
    if _is_all_last_round(state):
        if dealer_repeats:
            if _terminal_can_agari_or_tenpai_yame(state) and _top_seat(points) == state.dealer_seat:
                return "all_last_dealer_top"
            return None
        if _top_points(points) >= _return_points_for_ruleset(state.ruleset):
            return "all_last_return"
        return None
    if state.round_wind == SANDBOX_MAX_SUDDEN_DEATH_ROUND_WIND:
        if dealer_repeats:
            return None
        if _top_points(points) >= _return_points_for_ruleset(state.ruleset):
            return "sudden_death_return"
        if next_dealer == 0 and next_round_wind != SANDBOX_MAX_SUDDEN_DEATH_ROUND_WIND:
            return "sudden_death_max_round"
    return None


def _sandbox_final_result(
    state: SandboxEnvironmentState,
    *,
    reason: str,
) -> SandboxFinalResult:
    points = list(_points_by_seat(state))
    top_seat = _top_seat(tuple(points))
    points[top_seat] += state.riichi_sticks * RIICHI_DEPOSIT_POINTS
    final_points = tuple(points)
    placement = _final_placement(final_points)
    ranks = [0] * state.players
    for rank, seat in enumerate(placement, start=1):
        ranks[seat] = rank
    return_points = _return_points_for_ruleset(state.ruleset)
    oka_points = (return_points - _initial_points_for_ruleset(state.ruleset)) * state.players
    uma_by_rank = _uma_by_rank_for_ruleset(state.ruleset)
    scores = [0.0] * state.players
    for rank_index, seat in enumerate(placement):
        score = (final_points[seat] - return_points) / 1000
        score += uma_by_rank[rank_index]
        if rank_index == 0:
            score += oka_points / 1000
        scores[seat] = score
    return SandboxFinalResult(
        reason=reason,
        points=final_points,
        placement=placement,
        ranks=tuple(ranks),
        return_points=return_points,
        oka_points=oka_points,
        uma_by_rank=uma_by_rank,
        scores=tuple(scores),
    )


def _is_all_last_round(state: SandboxEnvironmentState) -> bool:
    return (
        state.round_wind == SANDBOX_ALL_LAST_ROUND_WIND
        and state.dealer_seat == state.players - 1
    )


def _terminal_can_agari_or_tenpai_yame(state: SandboxEnvironmentState) -> bool:
    return state.terminal_reason not in SANDBOX_ABORTIVE_DRAW_REASONS


def _return_points_for_ruleset(ruleset: str) -> int:
    if ruleset == TENHOU_3P.name:
        return SANDBOX_3P_RETURN_POINTS
    return SANDBOX_RETURN_POINTS


def _uma_by_rank_for_ruleset(ruleset: str) -> tuple[float, ...]:
    if ruleset == TENHOU_3P.name:
        return SANDBOX_3P_UMA_BY_RANK
    return SANDBOX_4P_UMA_BY_RANK


def _final_placement(points: tuple[int, ...]) -> tuple[int, ...]:
    return tuple(sorted(range(len(points)), key=lambda seat: (-points[seat], seat)))


def _top_seat(points: tuple[int, ...]) -> int:
    return _final_placement(points)[0]


def _top_points(points: tuple[int, ...]) -> int:
    return points[_top_seat(points)]


def _shuffled_wall(rng: random.Random, *, rules: RuleSet) -> list[Tile]:
    wall = [
        Tile(TileType(tile_type))
        for tile_type, count in enumerate(rules.type_counts)
        for _copy in range(count)
    ]
    rng.shuffle(wall)
    return wall


def _seed_int(seed: str | int) -> int:
    if isinstance(seed, int):
        return seed
    digest = blake2b(seed.encode(), digest_size=8).digest()
    return int.from_bytes(digest, "big")


def _melds_by_seat(state: SandboxEnvironmentState) -> tuple[tuple[Meld, ...], ...]:
    if state.melds:
        return state.melds
    return tuple(() for _seat in range(state.players))


def _kita_tiles_by_seat(state: SandboxEnvironmentState) -> tuple[tuple[Tile, ...], ...]:
    if state.kita_tiles:
        return state.kita_tiles
    return tuple(() for _seat in range(state.players))


def _discards_by_seat(state: SandboxEnvironmentState) -> tuple[tuple[Tile, ...], ...]:
    if state.discards:
        return state.discards
    return tuple(() for _seat in range(state.players))


def _points_by_seat(state: SandboxEnvironmentState) -> tuple[int, ...]:
    if state.points:
        return state.points
    return tuple(_initial_points_for_ruleset(state.ruleset) for _seat in range(state.players))


def _initial_points_for_ruleset(ruleset: str) -> int:
    if ruleset == TENHOU_3P.name:
        return SANDBOX_3P_INITIAL_POINTS
    return SANDBOX_INITIAL_POINTS


def _terminal_win_point_updates(
    state: SandboxEnvironmentState,
    *,
    winner_seats: tuple[int, ...],
    discarder_seat: int | None,
    winning_tile: Tile | None,
    win_kind: str,
    winning_yaku_by_seat: tuple[tuple[int, tuple[str, ...]], ...],
    riichi_declaration_discard_seat: int | None = None,
) -> dict[str, Any]:
    before_points = _points_by_seat(state)
    if not winner_seats:
        return {"terminal_point_deltas": _neutral_point_deltas(state.players)}
    points = list(before_points)
    riichi_sticks_for_winners = state.riichi_sticks
    if riichi_declaration_discard_seat is not None:
        points[riichi_declaration_discard_seat] += RIICHI_DEPOSIT_POINTS
        riichi_sticks_for_winners -= 1
        if riichi_sticks_for_winners < 0:
            raise ValueError("pending riichi declaration refund exceeds riichi sticks")
    yaku_by_seat = dict(winning_yaku_by_seat)
    estimates: list[SandboxScoreEstimate] = []
    for winner_index, winner_seat in enumerate(winner_seats):
        riichi_stick_points = (
            riichi_sticks_for_winners * RIICHI_DEPOSIT_POINTS
            if winner_index == 0
            else 0
        )
        estimate = _sandbox_score_estimate(
            seat=winner_seat,
            players=state.players,
            is_dealer=winner_seat == state.dealer_seat,
            win_kind=win_kind,
            yaku=yaku_by_seat.get(winner_seat, ()),
            visible_dora_count=_visible_dora_count(
                state,
                seat=winner_seat,
                winning_tile=winning_tile,
            ),
            ura_dora_count=_ura_dora_count(
                state,
                seat=winner_seat,
                winning_tile=winning_tile,
            ),
            red_dora_count=_red_dora_count(
                state,
                seat=winner_seat,
                winning_tile=winning_tile,
            ),
            kita_dora_count=len(_kita_tiles_by_seat(state)[winner_seat]),
            honba=state.honba,
            riichi_stick_points=riichi_stick_points,
        )
        estimates.append(estimate)

        if discarder_seat is None:
            child_payment = estimate.tsumo_child_payment
            if child_payment is None:
                raise ValueError("tsumo score estimate must include child payment")
            for seat in range(state.players):
                if seat == winner_seat:
                    continue
                payment = child_payment
                if (
                    seat == state.dealer_seat
                    and estimate.tsumo_dealer_payment is not None
                ):
                    payment = estimate.tsumo_dealer_payment
                total_payment = payment + estimate.honba_payment
                points[seat] -= total_payment
                points[winner_seat] += total_payment
        else:
            payment = estimate.ron_payment
            if payment is None:
                raise ValueError("ron score estimate must include ron payment")
            payment += estimate.honba_payment
            points[winner_seat] += payment
            points[discarder_seat] -= payment
        if riichi_stick_points:
            points[winner_seat] += riichi_stick_points

    point_deltas = tuple(
        after - before for after, before in zip(points, before_points, strict=True)
    )
    return {
        "points": tuple(points),
        "riichi_sticks": 0,
        "terminal_point_deltas": point_deltas,
        "terminal_score_estimates": tuple(estimates),
    }


def _terminal_wall_exhausted_updates(state: SandboxEnvironmentState) -> dict[str, Any]:
    nagashi_seats = _nagashi_mangan_seats(state)
    if nagashi_seats:
        winning_yaku_by_seat = tuple(
            (seat, ("nagashi_mangan",)) for seat in nagashi_seats
        )
        point_updates = _terminal_win_point_updates(
            state,
            winner_seats=nagashi_seats,
            discarder_seat=None,
            winning_tile=None,
            win_kind="tsumo",
            winning_yaku_by_seat=winning_yaku_by_seat,
        )
        return {
            "terminal_reason": "nagashi_mangan",
            "winner_seat": nagashi_seats[0],
            "winner_seats": nagashi_seats,
            "winning_shapes": (),
            "winning_shapes_by_seat": tuple((seat, ()) for seat in nagashi_seats),
            "winning_yaku": winning_yaku_by_seat[0][1],
            "winning_yaku_by_seat": winning_yaku_by_seat,
            "winning_ippatsu_seats": (),
            "winning_rinshan_seats": (),
            "ippatsu_seats": (),
            "terminal_rewards": _point_delta_rewards(
                point_updates["terminal_point_deltas"]
            ),
            "last_draw_was_final_live_wall": False,
            "exhaustive_draw_tenpai_seats": (),
            "exhaustive_draw_noten_seats": (),
            **point_updates,
        }
    before_points = _points_by_seat(state)
    tenpai_seats = _exhaustive_draw_tenpai_seats(state)
    noten_seats = tuple(seat for seat in range(state.players) if seat not in tenpai_seats)
    point_deltas = _exhaustive_draw_point_deltas(
        players=state.players,
        tenpai_seats=tenpai_seats,
    )
    points = tuple(
        before + delta for before, delta in zip(before_points, point_deltas, strict=True)
    )
    return {
        "terminal_reason": "wall_exhausted",
        "terminal_rewards": _point_delta_rewards(point_deltas),
        "terminal_point_deltas": point_deltas,
        "points": points,
        "last_draw_was_final_live_wall": False,
        "exhaustive_draw_tenpai_seats": tenpai_seats,
        "exhaustive_draw_noten_seats": noten_seats,
    }


def _terminal_abortive_draw_updates(
    state: SandboxEnvironmentState,
    *,
    reason: str,
) -> dict[str, Any]:
    if reason not in SANDBOX_ABORTIVE_DRAW_REASONS:
        raise ValueError("unsupported abortive draw reason: " + reason)
    return {
        "terminal_reason": reason,
        "winner_seat": None,
        "winner_seats": (),
        "winning_tile": None,
        "winning_shapes": (),
        "winning_shapes_by_seat": (),
        "winning_yaku": (),
        "winning_yaku_by_seat": (),
        "winning_ippatsu_seats": (),
        "winning_rinshan_seats": (),
        "terminal_rewards": _neutral_rewards(state.players),
        "terminal_point_deltas": _neutral_point_deltas(state.players),
        "terminal_score_estimates": (),
        "drawn_tile": None,
        "rinshan_draw": False,
        "last_draw_was_final_live_wall": False,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_chankan_tile": None,
        "pending_chankan_seat": None,
        "pending_chankan_kind": None,
        "pending_kita_tile": None,
        "pending_kita_seat": None,
        "pending_riichi_declaration_discard_seat": None,
        "pending_abortive_draw_reason": None,
        "abortive_draw_after_discard_reason": None,
        "pending_reaction_seats": (),
        "temporary_furiten_seats": (),
        "riichi_pending_discard_seats": (),
        "ippatsu_seats": (),
        "riichi_furiten_seats": (),
        "exhaustive_draw_tenpai_seats": (),
        "exhaustive_draw_noten_seats": (),
    }


def _nagashi_mangan_seats(state: SandboxEnvironmentState) -> tuple[int, ...]:
    called_from_seats = {
        meld.from_seat
        for seat_melds in _melds_by_seat(state)
        for meld in seat_melds
        if meld.from_seat is not None
    }
    return tuple(
        seat
        for seat, discards in enumerate(_discards_by_seat(state))
        if discards
        and seat not in called_from_seats
        and all(tile.type.is_terminal_or_honor for tile in discards)
    )


def _exhaustive_draw_tenpai_seats(state: SandboxEnvironmentState) -> tuple[int, ...]:
    return tuple(
        seat for seat in range(state.players) if _winning_wait_types(state, seat=seat)
    )


def _exhaustive_draw_point_deltas(
    *,
    players: int,
    tenpai_seats: tuple[int, ...],
) -> tuple[int, ...]:
    tenpai_count = len(tenpai_seats)
    if tenpai_count == 0 or tenpai_count == players:
        return _neutral_point_deltas(players)
    noten_count = players - tenpai_count
    tenpai_payment = SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL // tenpai_count
    noten_payment = SANDBOX_EXHAUSTIVE_DRAW_NOTEN_POOL // noten_count
    tenpai = set(tenpai_seats)
    return tuple(tenpai_payment if seat in tenpai else -noten_payment for seat in range(players))


def _visible_dora_count(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
) -> int:
    return _dora_count_for_indicators(
        state,
        indicators=state.dora_indicators,
        seat=seat,
        winning_tile=winning_tile,
    )


def _ura_dora_count(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
) -> int:
    if not _is_riichi(state, seat=seat):
        return 0
    active_indicators = state.ura_dora_indicators[: len(state.dora_indicators)]
    return _dora_count_for_indicators(
        state,
        indicators=active_indicators,
        seat=seat,
        winning_tile=winning_tile,
    )


def _dora_count_for_indicators(
    state: SandboxEnvironmentState,
    *,
    indicators: tuple[Tile, ...],
    seat: int,
    winning_tile: Tile | None,
) -> int:
    if not indicators:
        return 0
    dora_type_counts = [0] * 34
    for indicator in indicators:
        dora_type_counts[
            _dora_type_for_indicator(indicator.type, ruleset=state.ruleset).index
        ] += 1
    return sum(
        dora_type_counts[tile.type.index]
        for tile in _full_yaku_tiles(state, seat=seat, winning_tile=winning_tile)
    )


def _red_dora_count(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
) -> int:
    rules = SANDBOX_RULESET_BY_NAME[state.ruleset]
    return sum(
        1
        for tile in _full_yaku_tiles(state, seat=seat, winning_tile=winning_tile)
        if tile.red and tile.type.suit in rules.red_five_suits
    )


def _dora_type_for_indicator(indicator: TileType, *, ruleset: str) -> TileType:
    if (
        ruleset == TENHOU_3P.name
        and indicator.suit == "m"
        and indicator.rank in {1, 9}
    ):
        return TileType.parse("9m" if indicator.rank == 1 else "1m")
    if indicator.suit in {"m", "p", "s"}:
        rank = indicator.rank
        assert rank is not None
        next_rank = 1 if rank == 9 else rank + 1
        return TileType.parse(f"{next_rank}{indicator.suit}")
    if indicator in SANDBOX_SEAT_WINDS:
        wind_index = SANDBOX_SEAT_WINDS.index(indicator)
        return SANDBOX_SEAT_WINDS[(wind_index + 1) % len(SANDBOX_SEAT_WINDS)]
    dragon_index = SANDBOX_DRAGON_TILES_ORDER.index(indicator)
    return SANDBOX_DRAGON_TILES_ORDER[
        (dragon_index + 1) % len(SANDBOX_DRAGON_TILES_ORDER)
    ]


def _sandbox_score_estimate(
    *,
    seat: int,
    players: int,
    is_dealer: bool,
    win_kind: str,
    yaku: tuple[str, ...],
    visible_dora_count: int,
    ura_dora_count: int,
    red_dora_count: int,
    kita_dora_count: int,
    honba: int,
    riichi_stick_points: int,
) -> SandboxScoreEstimate:
    bonus_han = visible_dora_count + ura_dora_count + red_dora_count + kita_dora_count
    if "nagashi_mangan" in yaku:
        score = score_riichi_hand(
            yaku_han=SANDBOX_YAKU_HAN["nagashi_mangan"],
            bonus_han=0,
            fu=30,
            is_dealer=is_dealer,
            win_kind=win_kind,
            honba=honba,
            riichi_sticks=riichi_stick_points // RIICHI_DEPOSIT_POINTS,
            players=players,
        )
        return _score_estimate_from_result(
            seat=seat,
            yaku=yaku,
            visible_dora_count=visible_dora_count,
            ura_dora_count=ura_dora_count,
            red_dora_count=red_dora_count,
            kita_dora_count=kita_dora_count,
            score=score,
        )
    if "kokushi" in yaku:
        score = score_riichi_hand(
            yaku_han=13,
            bonus_han=0,
            fu=None,
            is_dealer=is_dealer,
            win_kind=win_kind,
            honba=honba,
            riichi_sticks=riichi_stick_points // RIICHI_DEPOSIT_POINTS,
            players=players,
            yakuman_multiplier=1,
        )
        return _score_estimate_from_result(
            seat=seat,
            yaku=yaku,
            visible_dora_count=visible_dora_count,
            ura_dora_count=ura_dora_count,
            red_dora_count=red_dora_count,
            kita_dora_count=kita_dora_count,
            score=score,
        )

    yaku_han = sum(SANDBOX_YAKU_HAN.get(yaku_name, 0) for yaku_name in yaku)
    fu = 25 if "chiitoitsu" in yaku else 30
    score = score_riichi_hand(
        yaku_han=yaku_han,
        bonus_han=bonus_han,
        fu=fu,
        is_dealer=is_dealer,
        win_kind=win_kind,
        honba=honba,
        riichi_sticks=riichi_stick_points // RIICHI_DEPOSIT_POINTS,
        players=players,
    )
    return _score_estimate_from_result(
        seat=seat,
        yaku=yaku,
        visible_dora_count=visible_dora_count,
        ura_dora_count=ura_dora_count,
        red_dora_count=red_dora_count,
        kita_dora_count=kita_dora_count,
        score=score,
    )


def _score_estimate_from_result(
    *,
    seat: int,
    yaku: tuple[str, ...],
    visible_dora_count: int,
    ura_dora_count: int,
    red_dora_count: int,
    kita_dora_count: int,
    score: ScoreResult,
) -> SandboxScoreEstimate:
    return SandboxScoreEstimate(
        seat=seat,
        win_kind=score.win_kind,
        yaku=yaku,
        yaku_han=score.yaku_han,
        bonus_han=score.bonus_han,
        visible_dora_count=visible_dora_count,
        ura_dora_count=ura_dora_count,
        red_dora_count=red_dora_count,
        kita_dora_count=kita_dora_count,
        han=score.han,
        fu=score.fu,
        limit=score.limit,
        base_points=score.base_points,
        is_dealer=score.is_dealer,
        ron_payment=score.ron_payment,
        tsumo_payment_per_loser=score.tsumo_payment_per_loser,
        tsumo_child_payment=score.tsumo_child_payment,
        tsumo_dealer_payment=score.tsumo_dealer_payment,
        honba_payment=score.honba_payment,
        riichi_stick_points=score.riichi_stick_points,
    )


def _sandbox_limit_ron_payment(*, limit: str, is_dealer: bool) -> int:
    if is_dealer:
        return SANDBOX_LIMIT_DEALER_RON_POINTS[limit]
    return SANDBOX_LIMIT_RON_POINTS[limit]


def _sandbox_limit_tsumo_child_payment(*, limit: str, is_dealer: bool) -> int:
    if is_dealer:
        return SANDBOX_LIMIT_TSUMO_DEALER_POINTS[limit]
    return SANDBOX_LIMIT_TSUMO_CHILD_POINTS[limit]


def _sandbox_limit_tsumo_dealer_payment(*, limit: str, is_dealer: bool) -> int | None:
    if is_dealer:
        return None
    return SANDBOX_LIMIT_TSUMO_DEALER_POINTS[limit]


def _sandbox_honba_payment(*, win_kind: str, honba: int) -> int:
    if win_kind == "tsumo":
        return honba * HONBA_TSUMO_POINTS_PER_LOSER
    return honba * HONBA_RON_POINTS


def _sandbox_score_limit(*, han: int, base_points: int) -> str | None:
    if han >= 13:
        return "yakuman"
    if han >= 11:
        return "sanbaiman"
    if han >= 8:
        return "baiman"
    if han >= 6:
        return "haneman"
    if han >= 5 or base_points >= 2000:
        return "mangan"
    return None


def _ceil_to_hundred(points: int) -> int:
    return ((points + 99) // 100) * 100


def _meld_payloads(state: SandboxEnvironmentState) -> list[list[dict[str, Any]]]:
    return [
        [
            {
                "kind": meld.kind.value,
                "tiles": [tile.notation for tile in meld.tiles],
                "called_tile": None if meld.called_tile is None else meld.called_tile.notation,
                "from_seat": meld.from_seat,
            }
            for meld in seat_melds
        ]
        for seat_melds in _melds_by_seat(state)
    ]


def _hand_type_counts(hand: tuple[Tile, ...]) -> tuple[int, ...]:
    counts = [0] * 34
    for tile in hand:
        counts[tile.type.index] += 1
    return tuple(counts)


def _legal_chi_actions(
    *,
    hand: tuple[Tile, ...],
    pending_discard: Tile,
    seat: int,
    pending_discard_seat: int,
    players: int,
) -> tuple[Action, ...]:
    discarded_type = pending_discard.type
    if seat != (pending_discard_seat + 1) % players or discarded_type.is_honor:
        return ()
    rank = discarded_type.rank
    if rank is None:
        return ()
    suit_start = discarded_type.index - rank + 1
    actions: list[Action] = []
    for start_rank in range(max(1, rank - 2), min(7, rank) + 1):
        needed_ranks = tuple(
            candidate for candidate in range(start_rank, start_rank + 3) if candidate != rank
        )
        needed_types = tuple(TileType(suit_start + needed_rank - 1) for needed_rank in needed_ranks)
        consumed = _maybe_first_tiles_of_types(hand, needed_types)
        if consumed is not None:
            actions.append(Action(ActionKind.CHI, discarded_type, consumed=consumed))
    return tuple(actions)


def _maybe_first_tiles_of_types(
    hand: tuple[Tile, ...],
    tile_types: tuple[TileType, ...],
) -> tuple[Tile, ...] | None:
    consumed: list[Tile] = []
    used_indices: set[int] = set()
    for tile_type in tile_types:
        for index, tile in enumerate(hand):
            if index in used_indices or tile.type != tile_type:
                continue
            consumed.append(tile)
            used_indices.add(index)
            break
        else:
            return None
    return tuple(consumed)


def _first_tiles_of_type(
    hand: tuple[Tile, ...],
    tile_type: TileType,
    count: int,
) -> tuple[Tile, ...]:
    tiles = tuple(tile for tile in hand if tile.type == tile_type)
    if len(tiles) < count:
        raise ValueError(f"not enough {tile_type.notation} tiles in hand")
    return tiles[:count]


def _tsumo_rewards(winner_seat: int, players: int) -> tuple[float, ...]:
    loser_reward = -1.0 / (players - 1)
    return tuple(1.0 if seat == winner_seat else loser_reward for seat in range(players))


def _multi_ron_rewards(
    *,
    winner_seats: tuple[int, ...],
    discarder_seat: int,
    players: int,
) -> tuple[float, ...]:
    winners = set(winner_seats)
    return tuple(
        1.0 if seat in winners else -float(len(winners)) if seat == discarder_seat else 0.0
        for seat in range(players)
    )


def _neutral_rewards(players: int) -> tuple[float, ...]:
    return tuple(0.0 for _seat in range(players))


def _point_delta_rewards(point_deltas: tuple[int, ...]) -> tuple[float, ...]:
    max_delta = max((abs(delta) for delta in point_deltas), default=0)
    if max_delta == 0:
        return _neutral_rewards(len(point_deltas))
    return tuple(delta / max_delta for delta in point_deltas)


def _neutral_point_deltas(players: int) -> tuple[int, ...]:
    return tuple(0 for _seat in range(players))


def _pending_ron_seats(state: SandboxEnvironmentState) -> tuple[int, ...]:
    pending_discard = _pending_discard(state)
    return _legal_ron_seats_for_tile(
        state,
        tile=pending_discard,
        candidate_seats=state.pending_reaction_seats,
        win_kind="ron",
    )


def _is_discard_furiten(state: SandboxEnvironmentState, *, seat: int) -> bool:
    own_discard_types = {tile.type for tile in _discards_by_seat(state)[seat]}
    if not own_discard_types:
        return False
    wait_types = _winning_wait_types(state, seat=seat)
    return any(wait_type in own_discard_types for wait_type in wait_types)


def _is_temporary_furiten(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.temporary_furiten_seats


def _is_riichi(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.riichi_seats


def _is_double_riichi(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.double_riichi_seats


def _is_double_riichi_declaration(state: SandboxEnvironmentState) -> bool:
    first_turn = (state.current_seat - state.dealer_seat) % state.players
    return (
        state.turn == first_turn
        and not any(_melds_by_seat(state))
        and not any(_kita_tiles_by_seat(state))
    )


def _is_post_riichi_discard_locked(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return _is_riichi(state, seat=seat) and seat not in state.riichi_pending_discard_seats


def _is_riichi_furiten(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.riichi_furiten_seats


def _winning_wait_types(state: SandboxEnvironmentState, *, seat: int) -> tuple[TileType, ...]:
    hand = state.hands[seat]
    melds = _melds_by_seat(state)[seat]
    if len(hand) != 13 - 3 * len(melds):
        return ()
    return _winning_wait_types_for_hand_and_melds(state, hand=hand, melds=melds)


def _winning_wait_types_for_hand_and_melds(
    state: SandboxEnvironmentState,
    *,
    hand: tuple[Tile, ...],
    melds: tuple[Meld, ...],
) -> tuple[TileType, ...]:
    owned_counts = _hand_type_counts(
        (*hand, *(tile for meld in melds for tile in meld.tiles))
    )
    rules = SANDBOX_RULESET_BY_NAME[state.ruleset]
    waits: list[TileType] = []
    for tile_type in rules.tile_types:
        if owned_counts[tile_type.index] >= rules.type_counts[tile_type.index]:
            continue
        if _winning_shapes_for_concealed_and_melds((*hand, Tile(tile_type)), melds):
            waits.append(tile_type)
    return tuple(waits)


def _with_seat(seats: tuple[int, ...], seat: int) -> tuple[int, ...]:
    if seat in seats:
        return seats
    return tuple(sorted((*seats, seat)))


def _without_seat(seats: tuple[int, ...], seat: int) -> tuple[int, ...]:
    return tuple(candidate for candidate in seats if candidate != seat)


def _require_pending_reaction(state: SandboxEnvironmentState) -> None:
    if not _has_pending_reaction(state):
        raise ValueError("no pending reaction")


def _require_pending_discard(state: SandboxEnvironmentState) -> None:
    if state.pending_discard is None:
        raise ValueError("no pending discard reaction")


def _require_pending_chankan(state: SandboxEnvironmentState) -> None:
    if state.pending_chankan_tile is None:
        raise ValueError("no pending chankan reaction")


def _require_pending_kita(state: SandboxEnvironmentState) -> None:
    if state.pending_kita_tile is None:
        raise ValueError("no pending kita reaction")


def _require_reaction_seat(state: SandboxEnvironmentState, seat: int) -> None:
    if not 0 <= seat < state.players:
        raise ValueError("reaction seat outside player range")
    if seat not in state.pending_reaction_seats:
        raise ValueError("seat cannot react to this pending reaction")


def _pending_discard(state: SandboxEnvironmentState) -> Tile:
    if state.pending_discard is None:
        raise ValueError("no pending discard reaction")
    return state.pending_discard


def _pending_discard_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_discard_seat is None:
        raise ValueError("no pending discard seat")
    return state.pending_discard_seat


def _pending_chankan_tile(state: SandboxEnvironmentState) -> Tile:
    if state.pending_chankan_tile is None:
        raise ValueError("no pending chankan reaction")
    return state.pending_chankan_tile


def _pending_chankan_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_chankan_seat is None:
        raise ValueError("no pending chankan seat")
    return state.pending_chankan_seat


def _pending_kita_tile(state: SandboxEnvironmentState) -> Tile:
    if state.pending_kita_tile is None:
        raise ValueError("no pending kita reaction")
    return state.pending_kita_tile


def _pending_kita_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_kita_seat is None:
        raise ValueError("no pending kita seat")
    return state.pending_kita_seat


def _pending_ron_tile(state: SandboxEnvironmentState) -> Tile:
    if state.pending_discard is not None:
        return state.pending_discard
    if state.pending_chankan_tile is not None:
        return _pending_chankan_tile(state)
    return _pending_kita_tile(state)


def _pending_ron_source_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_discard is not None:
        return _pending_discard_seat(state)
    if state.pending_chankan_tile is not None:
        return _pending_chankan_seat(state)
    return _pending_kita_seat(state)
