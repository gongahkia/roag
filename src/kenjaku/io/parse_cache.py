from __future__ import annotations

import json
from functools import cache
from hashlib import blake2b
from pathlib import Path
from typing import Any, cast

from kenjaku.core import Tile
from kenjaku.io.tenhou_meld import TenhouMeld, decode_tenhou_meld
from kenjaku.io.tenhou_tiles import tenhou_tile
from kenjaku.io.tenhou_xml import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouEvent,
    TenhouGame,
    TenhouReach,
    TenhouRound,
    TenhouRyuukyoku,
    parse_tenhou_xml,
)

CACHE_KIND = "kenjaku-tenhou-parse-cache-v0"


def parse_tenhou_xml_file_cached(path: str | Path, cache_dir: str | Path) -> TenhouGame:
    """Parse one Tenhou XML file through an opt-in content-addressed JSON cache."""
    source_path = Path(path)
    source_bytes = source_path.read_bytes()
    source_digest = blake2b(source_bytes).hexdigest()
    cache_path = Path(cache_dir) / f"{source_digest}.json"

    if cache_path.is_file():
        try:
            payload = json.loads(cache_path.read_text(encoding="utf-8"))
            return _game_from_cache_payload(payload, source_digest=source_digest)
        except (json.JSONDecodeError, KeyError, TypeError, ValueError):
            pass

    game = parse_tenhou_xml(source_bytes.decode("utf-8"))
    payload = _cache_payload(game, source_digest=source_digest)
    cache_path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = cache_path.with_name(f"{cache_path.name}.tmp")
    temp_path.write_text(
        json.dumps(payload, separators=(",", ":"), sort_keys=True),
        encoding="utf-8",
    )
    temp_path.replace(cache_path)
    return game


def tenhou_game_payload(game: TenhouGame) -> dict[str, Any]:
    return _game_payload(game)


def tenhou_game_from_payload(payload: Any) -> TenhouGame:
    return _game_from_payload(payload)


def _cache_payload(game: TenhouGame, *, source_digest: str) -> dict[str, Any]:
    return {
        "kind": CACHE_KIND,
        "source_digest": source_digest,
        "game": _game_payload(game),
    }


def _game_from_cache_payload(payload: Any, *, source_digest: str) -> TenhouGame:
    payload = _dict(payload)
    if payload["kind"] != CACHE_KIND:
        raise ValueError("unexpected Tenhou parse cache kind")
    if payload["source_digest"] != source_digest:
        raise ValueError("Tenhou parse cache digest mismatch")
    return _game_from_payload(payload["game"])


def _game_payload(game: TenhouGame) -> dict[str, Any]:
    payload: dict[str, Any] = {"rounds": [_round_payload(round_) for round_ in game.rounds]}
    if game.names:
        payload["names"] = list(game.names)
    return payload


def _game_from_payload(payload: Any) -> TenhouGame:
    payload = _dict(payload)
    return TenhouGame(
        rounds=tuple(_round_from_payload(round_) for round_ in payload["rounds"]),
        names=tuple(str(name) for name in payload.get("names", ())),
    )


def _round_payload(round_: TenhouRound) -> dict[str, Any]:
    return {
        "round_index": round_.round_index,
        "round_wind": round_.round_wind,
        "kyoku": round_.kyoku,
        "honba": round_.honba,
        "kyotaku": round_.kyotaku,
        "dealer": round_.dealer,
        "scores": list(round_.scores),
        "starting_hands": [_tiles_payload(hand) for hand in round_.starting_hands],
        "dora_indicators": _tiles_payload(round_.dora_indicators),
        "events": [_event_payload(event) for event in round_.events],
    }


def _round_from_payload(payload: Any) -> TenhouRound:
    payload = _dict(payload)
    events = tuple(_event_from_payload(event) for event in payload["events"])
    draws = tuple(event for event in events if isinstance(event, TenhouDraw))
    discards = tuple(event for event in events if isinstance(event, TenhouDiscard))
    reaches = tuple(event for event in events if isinstance(event, TenhouReach))
    calls = tuple(event for event in events if isinstance(event, TenhouCall))
    agari = tuple(event for event in events if isinstance(event, TenhouAgari))
    ryuukyoku_events = tuple(event for event in events if isinstance(event, TenhouRyuukyoku))
    return TenhouRound(
        round_index=int(payload["round_index"]),
        round_wind=int(payload["round_wind"]),
        kyoku=int(payload["kyoku"]),
        honba=int(payload["honba"]),
        kyotaku=int(payload["kyotaku"]),
        dealer=int(payload["dealer"]),
        scores=_int_tuple(payload["scores"]),
        starting_hands=tuple(_tiles_from_payload(hand) for hand in payload["starting_hands"]),
        dora_indicators=_tiles_from_payload(payload["dora_indicators"]),
        draws=draws,
        discards=discards,
        reaches=reaches,
        calls=calls,
        agari=agari,
        ryuukyoku=ryuukyoku_events[-1] if ryuukyoku_events else None,
        events=events,
    )


def _event_payload(event: TenhouEvent) -> dict[str, Any]:
    if isinstance(event, TenhouDraw):
        return {
            "type": "draw",
            "seat": event.seat,
            "tile_id": event.tile_id,
            "event_index": event.event_index,
        }
    if isinstance(event, TenhouDiscard):
        return {
            "type": "discard",
            "seat": event.seat,
            "tile_id": event.tile_id,
            "tsumogiri": event.tsumogiri,
            "event_index": event.event_index,
            "turn": event.turn,
        }
    if isinstance(event, TenhouReach):
        return {
            "type": "reach",
            "seat": event.seat,
            "step": event.step,
            "event_index": event.event_index,
            "scores": _optional_ints_payload(event.scores),
        }
    if isinstance(event, TenhouCall):
        return {
            "type": "call",
            "seat": event.seat,
            "meld_code": event.meld_code,
            "event_index": event.event_index,
        }
    if isinstance(event, TenhouAgari):
        return {
            "type": "agari",
            "winner": event.winner,
            "from_seat": event.from_seat,
            "event_index": event.event_index,
            "machi": _optional_tile_payload(event.machi),
            "points": _optional_ints_payload(event.points),
            "score_deltas": _optional_ints_payload(event.score_deltas),
            "yaku": list(event.yaku),
            "dora_indicators": _tiles_payload(event.dora_indicators),
            "ura_dora_indicators": _tiles_payload(event.ura_dora_indicators),
        }
    return {
        "type": "ryuukyoku",
        "event_index": event.event_index,
        "reason": event.reason,
        "scores": _optional_ints_payload(event.scores),
        "score_deltas": _optional_ints_payload(event.score_deltas),
    }


def _event_from_payload(payload: Any) -> TenhouEvent:
    payload = _dict(payload)
    event_type = payload["type"]
    if event_type == "draw":
        tile_id = int(payload["tile_id"])
        return TenhouDraw(
            seat=int(payload["seat"]),
            tile_id=tile_id,
            tile=_tenhou_tile(tile_id),
            event_index=int(payload["event_index"]),
        )
    if event_type == "discard":
        tile_id = int(payload["tile_id"])
        return TenhouDiscard(
            seat=int(payload["seat"]),
            tile_id=tile_id,
            tile=_tenhou_tile(tile_id),
            tsumogiri=bool(payload["tsumogiri"]),
            event_index=int(payload["event_index"]),
            turn=int(payload["turn"]),
        )
    if event_type == "reach":
        return TenhouReach(
            seat=int(payload["seat"]),
            step=int(payload["step"]),
            event_index=int(payload["event_index"]),
            scores=_optional_int_tuple(payload["scores"]),
        )
    if event_type == "call":
        meld_code = int(payload["meld_code"])
        return TenhouCall(
            seat=int(payload["seat"]),
            meld_code=meld_code,
            event_index=int(payload["event_index"]),
            meld=_decode_tenhou_meld(meld_code),
        )
    if event_type == "agari":
        return TenhouAgari(
            winner=int(payload["winner"]),
            from_seat=int(payload["from_seat"]),
            event_index=int(payload["event_index"]),
            machi=_optional_tile_from_payload(payload["machi"]),
            points=_optional_int_tuple(payload["points"]),
            score_deltas=_optional_int_tuple(payload["score_deltas"]),
            yaku=_int_tuple(payload["yaku"]),
            dora_indicators=_tiles_from_payload(payload["dora_indicators"]),
            ura_dora_indicators=_tiles_from_payload(payload["ura_dora_indicators"]),
        )
    if event_type == "ryuukyoku":
        return TenhouRyuukyoku(
            event_index=int(payload["event_index"]),
            reason=payload["reason"],
            scores=_optional_int_tuple(payload["scores"]),
            score_deltas=_optional_int_tuple(payload["score_deltas"]),
        )
    raise ValueError(f"unknown cached Tenhou event type: {event_type!r}")


def _tiles_payload(tiles: tuple[Tile, ...]) -> list[str]:
    return [tile.notation for tile in tiles]


def _tiles_from_payload(payload: Any) -> tuple[Tile, ...]:
    return tuple(_tile_from_payload(tile) for tile in payload)


def _optional_tile_payload(tile: Tile | None) -> str | None:
    return None if tile is None else tile.notation


def _optional_tile_from_payload(payload: Any) -> Tile | None:
    if payload is None:
        return None
    return _tile_from_payload(payload)


def _tile_from_payload(payload: Any) -> Tile:
    if not isinstance(payload, str):
        raise ValueError("cached Tenhou tile must be a string")
    return _tile_from_notation(payload)


def _optional_ints_payload(values: tuple[int, ...] | None) -> list[int] | None:
    return None if values is None else list(values)


def _optional_int_tuple(payload: Any) -> tuple[int, ...] | None:
    if payload is None:
        return None
    return _int_tuple(payload)


def _int_tuple(payload: Any) -> tuple[int, ...]:
    return tuple(int(value) for value in payload)


def _dict(payload: Any) -> dict[str, Any]:
    if not isinstance(payload, dict):
        raise ValueError("cached Tenhou parse payload must be an object")
    return cast(dict[str, Any], payload)


@cache
def _tile_from_notation(notation: str) -> Tile:
    return Tile.parse(notation)


@cache
def _tenhou_tile(tile_id: int) -> Tile:
    return tenhou_tile(tile_id)


@cache
def _decode_tenhou_meld(meld_code: int) -> TenhouMeld:
    return decode_tenhou_meld(meld_code)
