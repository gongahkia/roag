from __future__ import annotations

import json
from collections.abc import Mapping
from dataclasses import dataclass
from pathlib import Path
from typing import Any, cast

from kenjaku.core import TENHOU_3P, TENHOU_4P, TileType

SANDBOX_RULESETS = (TENHOU_4P.name, TENHOU_3P.name)
SANDBOX_RULE_CONFIG_V1_KIND = "kenjaku-sandbox-rule-config-v1"
SANDBOX_RULE_CONFIG_V1_FIELDS = ("kind", "ruleset", "config")
SANDBOX_RULE_CONFIG_FIELDS = (
    "ruleset",
    "initial_points",
    "return_points",
    "uma_by_rank",
    "riichi_deposit_points",
    "honba_ron_points",
    "honba_tsumo_points_per_loser",
    "exhaustive_draw_noten_pool",
    "dead_wall_tiles",
    "replacement_tiles",
    "non_replacement_dead_wall_tiles",
    "initial_dora_indicators",
    "score_payment_model",
    "kita_tile",
    "seat_winds",
    "initial_round_wind",
    "all_last_round_wind",
    "max_sudden_death_round_wind",
    "round_winds",
    "dragon_tiles_order",
    "yaku_han",
    "limit_base_points",
    "limit_ron_points",
    "limit_dealer_ron_points",
    "limit_tsumo_child_points",
    "limit_tsumo_dealer_points",
    "limit_tsumo_points_per_loser",
    "abortive_draw_reasons",
)
_DEFAULT_SEAT_WINDS = (
    TileType.parse("E"),
    TileType.parse("S"),
    TileType.parse("W"),
    TileType.parse("N"),
)
_DEFAULT_DRAGON_TILES_ORDER = (
    TileType.parse("P"),
    TileType.parse("F"),
    TileType.parse("C"),
)
_DEFAULT_YAKU_HAN = {
    "chiitoitsu": 2,
    "chanta": 2,
    "chinitsu": 6,
    "double_riichi": 2,
    "honitsu": 3,
    "iipeikou": 1,
    "ittsu": 2,
    "junchan": 3,
    "nagashi_mangan": 5,
    "pinfu": 1,
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
    "sanankou": 2,
    "sankantsu": 2,
    "sanshoku_doujun": 2,
    "sanshoku_doukou": 2,
    "shousangen": 2,
}
_DEFAULT_LIMIT_BASE_POINTS = {
    "mangan": 2000,
    "haneman": 3000,
    "baiman": 4000,
    "sanbaiman": 6000,
    "yakuman": 8000,
}
_DEFAULT_LIMIT_RON_POINTS = {
    "mangan": 8000,
    "haneman": 12000,
    "baiman": 16000,
    "sanbaiman": 24000,
    "yakuman": 32000,
}
_DEFAULT_LIMIT_DEALER_RON_POINTS = {
    "mangan": 12000,
    "haneman": 18000,
    "baiman": 24000,
    "sanbaiman": 36000,
    "yakuman": 48000,
}
_DEFAULT_LIMIT_TSUMO_CHILD_POINTS = {
    "mangan": 2000,
    "haneman": 3000,
    "baiman": 4000,
    "sanbaiman": 6000,
    "yakuman": 8000,
}
_DEFAULT_LIMIT_TSUMO_DEALER_POINTS = {
    "mangan": 4000,
    "haneman": 6000,
    "baiman": 8000,
    "sanbaiman": 12000,
    "yakuman": 16000,
}
_DEFAULT_ABORTIVE_DRAW_REASONS = frozenset(
    {
        "kyuushu_kyuuhai",
        "four_winds",
        "four_riichi",
        "four_kans",
        "triple_ron",
    }
)


@dataclass(frozen=True, slots=True)
class SandboxRuleConfig:
    ruleset: str
    initial_points: int
    return_points: int
    uma_by_rank: tuple[float, ...]
    riichi_deposit_points: int
    honba_ron_points: int
    honba_tsumo_points_per_loser: int
    exhaustive_draw_noten_pool: int
    dead_wall_tiles: int
    replacement_tiles: int
    non_replacement_dead_wall_tiles: int
    initial_dora_indicators: int
    score_payment_model: str
    kita_tile: TileType
    seat_winds: tuple[TileType, ...]
    initial_round_wind: TileType
    all_last_round_wind: TileType
    max_sudden_death_round_wind: TileType
    round_winds: tuple[TileType, ...]
    dragon_tiles_order: tuple[TileType, ...]
    yaku_han: dict[str, int]
    limit_base_points: dict[str, int]
    limit_ron_points: dict[str, int]
    limit_dealer_ron_points: dict[str, int]
    limit_tsumo_child_points: dict[str, int]
    limit_tsumo_dealer_points: dict[str, int]
    limit_tsumo_points_per_loser: dict[str, int]
    abortive_draw_reasons: frozenset[str]

    def __post_init__(self) -> None:
        if self.ruleset not in SANDBOX_RULESETS:
            raise ValueError("unsupported sandbox rule config ruleset: " + self.ruleset)
        players = 3 if self.ruleset == TENHOU_3P.name else 4
        if len(self.uma_by_rank) != players:
            raise ValueError("uma_by_rank length must match ruleset players")
        if self.dead_wall_tiles < 0 or self.initial_dora_indicators < 0:
            raise ValueError("wall and dora counts must be non-negative")
        if self.initial_dora_indicators > self.dead_wall_tiles:
            raise ValueError("initial dora indicators cannot exceed dead wall tiles")
        if self.non_replacement_dead_wall_tiles < 0:
            raise ValueError("non replacement dead wall tiles must be non-negative")
        if not self.round_winds:
            raise ValueError("round_winds cannot be empty")

    @property
    def dragon_tiles(self) -> frozenset[TileType]:
        return frozenset(self.dragon_tiles_order)

    def to_payload(self) -> dict[str, Any]:
        return {
            "ruleset": self.ruleset,
            "initial_points": self.initial_points,
            "return_points": self.return_points,
            "uma_by_rank": list(self.uma_by_rank),
            "riichi_deposit_points": self.riichi_deposit_points,
            "honba_ron_points": self.honba_ron_points,
            "honba_tsumo_points_per_loser": self.honba_tsumo_points_per_loser,
            "exhaustive_draw_noten_pool": self.exhaustive_draw_noten_pool,
            "dead_wall_tiles": self.dead_wall_tiles,
            "replacement_tiles": self.replacement_tiles,
            "non_replacement_dead_wall_tiles": self.non_replacement_dead_wall_tiles,
            "initial_dora_indicators": self.initial_dora_indicators,
            "score_payment_model": self.score_payment_model,
            "kita_tile": self.kita_tile.notation,
            "seat_winds": [tile.notation for tile in self.seat_winds],
            "initial_round_wind": self.initial_round_wind.notation,
            "all_last_round_wind": self.all_last_round_wind.notation,
            "max_sudden_death_round_wind": self.max_sudden_death_round_wind.notation,
            "round_winds": [tile.notation for tile in self.round_winds],
            "dragon_tiles_order": [tile.notation for tile in self.dragon_tiles_order],
            "yaku_han": dict(self.yaku_han),
            "limit_base_points": dict(self.limit_base_points),
            "limit_ron_points": dict(self.limit_ron_points),
            "limit_dealer_ron_points": dict(self.limit_dealer_ron_points),
            "limit_tsumo_child_points": dict(self.limit_tsumo_child_points),
            "limit_tsumo_dealer_points": dict(self.limit_tsumo_dealer_points),
            "limit_tsumo_points_per_loser": dict(self.limit_tsumo_points_per_loser),
            "abortive_draw_reasons": sorted(self.abortive_draw_reasons),
        }

    def to_versioned_payload(self) -> dict[str, Any]:
        """Serialize this complete ruleset config in the strict v1 envelope."""
        return {
            "kind": SANDBOX_RULE_CONFIG_V1_KIND,
            "ruleset": self.ruleset,
            "config": self.to_payload(),
        }

    def to_versioned_json(self, *, indent: int | None = None) -> str:
        """Serialize the strict v1 envelope with deterministic key ordering."""
        return json.dumps(self.to_versioned_payload(), indent=indent, sort_keys=True)

    @classmethod
    def from_versioned_payload(cls, payload: Mapping[str, Any]) -> SandboxRuleConfig:
        """Parse one complete strict v1 rule-config envelope."""
        _require_exact_fields(payload, SANDBOX_RULE_CONFIG_V1_FIELDS, "SandboxRuleConfigV1")
        if payload.get("kind") != SANDBOX_RULE_CONFIG_V1_KIND:
            raise ValueError(f"SandboxRuleConfigV1 kind must be {SANDBOX_RULE_CONFIG_V1_KIND}")
        ruleset = payload.get("ruleset")
        config_payload = payload.get("config")
        if not isinstance(ruleset, str):
            raise ValueError("SandboxRuleConfigV1 ruleset must be a string")
        if not isinstance(config_payload, Mapping):
            raise ValueError("SandboxRuleConfigV1 config must be an object")
        _require_exact_fields(
            config_payload,
            SANDBOX_RULE_CONFIG_FIELDS,
            "SandboxRuleConfigV1 config",
        )
        config = _config_from_payload(config_payload)
        if config.ruleset != ruleset:
            raise ValueError("SandboxRuleConfigV1 ruleset must match config ruleset")
        return config


def tenhou_4p_default() -> SandboxRuleConfig:
    return _default_config(
        ruleset=TENHOU_4P.name,
        initial_points=25000,
        return_points=30000,
        uma_by_rank=(20.0, 10.0, -10.0, -20.0),
        replacement_tiles=0,
    )


def tenhou_3p_default() -> SandboxRuleConfig:
    return _default_config(
        ruleset=TENHOU_3P.name,
        initial_points=35000,
        return_points=40000,
        uma_by_rank=(20.0, 0.0, -20.0),
        replacement_tiles=8,
    )


def default_sandbox_rule_config(ruleset: str) -> SandboxRuleConfig:
    if ruleset == TENHOU_4P.name:
        return tenhou_4p_default()
    if ruleset == TENHOU_3P.name:
        return tenhou_3p_default()
    raise ValueError("unsupported sandbox rule config ruleset: " + ruleset)


def load_sandbox_rule_config(path: str | Path) -> SandboxRuleConfig:
    path = Path(path)
    text = path.read_text(encoding="utf-8")
    payload = _load_yaml(text) if path.suffix.lower() in {".yaml", ".yml"} else json.loads(text)
    if not isinstance(payload, Mapping):
        raise ValueError("sandbox rule config must be a JSON/YAML object")
    return sandbox_rule_config_from_mapping(payload)


def sandbox_rule_config_from_mapping(payload: Mapping[str, Any]) -> SandboxRuleConfig:
    ruleset = str(payload.get("ruleset", TENHOU_4P.name))
    merged = default_sandbox_rule_config(ruleset).to_payload()
    merged.update(dict(payload))
    if "non_replacement_dead_wall_tiles" not in payload and (
        "dead_wall_tiles" in payload or "replacement_tiles" in payload
    ):
        merged["non_replacement_dead_wall_tiles"] = int(merged["dead_wall_tiles"]) - int(
            merged["replacement_tiles"]
        )
    return _config_from_payload(merged)


def _default_config(
    *,
    ruleset: str,
    initial_points: int,
    return_points: int,
    uma_by_rank: tuple[float, ...],
    replacement_tiles: int,
) -> SandboxRuleConfig:
    dead_wall_tiles = 14
    return SandboxRuleConfig(
        ruleset=ruleset,
        initial_points=initial_points,
        return_points=return_points,
        uma_by_rank=uma_by_rank,
        riichi_deposit_points=1000,
        honba_ron_points=300,
        honba_tsumo_points_per_loser=100,
        exhaustive_draw_noten_pool=3000,
        dead_wall_tiles=dead_wall_tiles,
        replacement_tiles=replacement_tiles,
        non_replacement_dead_wall_tiles=dead_wall_tiles - replacement_tiles,
        initial_dora_indicators=1,
        score_payment_model="exact-riichi-score-v0",
        kita_tile=TileType.parse("N"),
        seat_winds=_DEFAULT_SEAT_WINDS,
        initial_round_wind=_DEFAULT_SEAT_WINDS[0],
        all_last_round_wind=_DEFAULT_SEAT_WINDS[1],
        max_sudden_death_round_wind=_DEFAULT_SEAT_WINDS[2],
        round_winds=_DEFAULT_SEAT_WINDS,
        dragon_tiles_order=_DEFAULT_DRAGON_TILES_ORDER,
        yaku_han=dict(_DEFAULT_YAKU_HAN),
        limit_base_points=dict(_DEFAULT_LIMIT_BASE_POINTS),
        limit_ron_points=dict(_DEFAULT_LIMIT_RON_POINTS),
        limit_dealer_ron_points=dict(_DEFAULT_LIMIT_DEALER_RON_POINTS),
        limit_tsumo_child_points=dict(_DEFAULT_LIMIT_TSUMO_CHILD_POINTS),
        limit_tsumo_dealer_points=dict(_DEFAULT_LIMIT_TSUMO_DEALER_POINTS),
        limit_tsumo_points_per_loser=dict(_DEFAULT_LIMIT_TSUMO_CHILD_POINTS),
        abortive_draw_reasons=frozenset(_DEFAULT_ABORTIVE_DRAW_REASONS),
    )


def _config_from_payload(payload: Mapping[str, Any]) -> SandboxRuleConfig:
    return SandboxRuleConfig(
        ruleset=str(payload["ruleset"]),
        initial_points=int(payload["initial_points"]),
        return_points=int(payload["return_points"]),
        uma_by_rank=_float_tuple(payload["uma_by_rank"]),
        riichi_deposit_points=int(payload["riichi_deposit_points"]),
        honba_ron_points=int(payload["honba_ron_points"]),
        honba_tsumo_points_per_loser=int(payload["honba_tsumo_points_per_loser"]),
        exhaustive_draw_noten_pool=int(payload["exhaustive_draw_noten_pool"]),
        dead_wall_tiles=int(payload["dead_wall_tiles"]),
        replacement_tiles=int(payload["replacement_tiles"]),
        non_replacement_dead_wall_tiles=int(payload["non_replacement_dead_wall_tiles"]),
        initial_dora_indicators=int(payload["initial_dora_indicators"]),
        score_payment_model=str(payload["score_payment_model"]),
        kita_tile=_tile_type(payload["kita_tile"]),
        seat_winds=_tile_tuple(payload["seat_winds"]),
        initial_round_wind=_tile_type(payload["initial_round_wind"]),
        all_last_round_wind=_tile_type(payload["all_last_round_wind"]),
        max_sudden_death_round_wind=_tile_type(payload["max_sudden_death_round_wind"]),
        round_winds=_tile_tuple(payload["round_winds"]),
        dragon_tiles_order=_tile_tuple(payload["dragon_tiles_order"]),
        yaku_han=_int_dict(payload["yaku_han"]),
        limit_base_points=_int_dict(payload["limit_base_points"]),
        limit_ron_points=_int_dict(payload["limit_ron_points"]),
        limit_dealer_ron_points=_int_dict(payload["limit_dealer_ron_points"]),
        limit_tsumo_child_points=_int_dict(payload["limit_tsumo_child_points"]),
        limit_tsumo_dealer_points=_int_dict(payload["limit_tsumo_dealer_points"]),
        limit_tsumo_points_per_loser=_int_dict(payload["limit_tsumo_points_per_loser"]),
        abortive_draw_reasons=frozenset(
            str(reason) for reason in _sequence(payload["abortive_draw_reasons"])
        ),
    )


def _tile_type(value: Any) -> TileType:
    if isinstance(value, TileType):
        return value
    return TileType.parse(str(value))


def _tile_tuple(value: Any) -> tuple[TileType, ...]:
    return tuple(_tile_type(item) for item in _sequence(value))


def _float_tuple(value: Any) -> tuple[float, ...]:
    return tuple(float(item) for item in _sequence(value))


def _int_dict(value: Any) -> dict[str, int]:
    if not isinstance(value, Mapping):
        raise ValueError("sandbox rule config mapping fields must be objects")
    return {str(key): int(item) for key, item in value.items()}


def _sequence(value: Any) -> tuple[Any, ...]:
    if not isinstance(value, list | tuple | set | frozenset):
        raise ValueError("sandbox rule config sequence fields must be arrays")
    return tuple(value)


def _require_exact_fields(payload: Mapping[str, Any], fields: tuple[str, ...], name: str) -> None:
    actual = set(payload)
    expected = set(fields)
    if actual == expected:
        return
    missing = sorted(expected - actual)
    unexpected = sorted(actual - expected)
    details = []
    if missing:
        details.append("missing=" + ",".join(missing))
    if unexpected:
        details.append("unexpected=" + ",".join(unexpected))
    raise ValueError(f"{name} fields must match v1 schema: {'; '.join(details)}")


def _load_yaml(text: str) -> Any:
    try:
        import yaml  # type: ignore[import-untyped]
    except ImportError as exc:
        raise ValueError("YAML sandbox rule configs require PyYAML") from exc
    return cast(Any, yaml.safe_load(text))
