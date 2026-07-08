from __future__ import annotations

import json
from collections.abc import Sequence
from pathlib import Path
from typing import Any, cast

from kenjaku.core import ActionKind, Tile
from kenjaku.io.tenhou_dataset import tenhou_xml_files
from kenjaku.io.tenhou_meld import TenhouMeld
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
    parse_tenhou_xml_file,
)

MJAI_EVENT_TYPES = {
    "start_game",
    "start_kyoku",
    "tsumo",
    "dahai",
    "pon",
    "chi",
    "daiminkan",
    "ankan",
    "kakan",
    "reach",
    "reach_accepted",
    "hora",
    "ryukyoku",
    "end_kyoku",
    "end_game",
}


def to_mjai_events(game: TenhouGame) -> list[dict[str, Any]]:
    players = _player_count(game)
    events: list[dict[str, Any]] = [
        {
            "type": "start_game",
            "names": [f"player_{seat}" for seat in range(players)],
            "kyoku_first": 0,
            "aka_flag": True,
        }
    ]
    for round_ in game.rounds:
        events.append(_start_kyoku_event(round_))
        for event in round_.events:
            events.extend(_mjai_events_for_tenhou_event(event, players=players))
        events.append({"type": "end_kyoku"})
    events.append({"type": "end_game"})
    return events


def read_mjai_events(path: Path) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        raw_payload = json.loads(line)
        if not isinstance(raw_payload, dict):
            raise ValueError(f"MJAI event line must be an object: {path}")
        payload = cast(dict[str, Any], raw_payload)
        event_type = payload.get("type")
        if not isinstance(event_type, str) or event_type not in MJAI_EVENT_TYPES:
            raise ValueError(f"unsupported MJAI event type: {event_type!r}")
        events.append(payload)
    return events


def write_mjai_events(path: Path, events: Sequence[dict[str, Any]]) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as handle:
        for event in events:
            handle.write(json.dumps(event, separators=(",", ":"), ensure_ascii=False))
            handle.write("\n")
    return len(events)


def write_tenhou_mjai_files(paths: Sequence[str | Path], output_dir: Path) -> tuple[Path, ...]:
    output_dir.mkdir(parents=True, exist_ok=True)
    output_paths: list[Path] = []
    for file in tenhou_xml_files(paths):
        game = parse_tenhou_xml_file(file)
        output_path = _unique_output_path(output_dir / f"{file.stem}.mjson", output_paths)
        write_mjai_events(output_path, to_mjai_events(game))
        output_paths.append(output_path)
    if not output_paths:
        raise ValueError("no Tenhou XML files found")
    return tuple(output_paths)


def _player_count(game: TenhouGame) -> int:
    for round_ in game.rounds:
        if round_.starting_hands:
            return len(round_.starting_hands)
    return 4


def _start_kyoku_event(round_: TenhouRound) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "type": "start_kyoku",
        "bakaze": _wind(round_.round_wind),
        "kyoku": round_.kyoku,
        "honba": round_.honba,
        "kyotaku": round_.kyotaku,
        "oya": round_.dealer,
        "scores": list(round_.scores),
        "tehais": [[_tile(tile) for tile in hand] for hand in round_.starting_hands],
    }
    if round_.dora_indicators:
        payload["dora_marker"] = _tile(round_.dora_indicators[0])
    return payload


def _mjai_events_for_tenhou_event(
    event: TenhouEvent,
    *,
    players: int,
) -> list[dict[str, Any]]:
    if isinstance(event, TenhouDraw):
        return [{"type": "tsumo", "actor": event.seat, "pai": _tile(event.tile)}]
    if isinstance(event, TenhouDiscard):
        return [
            {
                "type": "dahai",
                "actor": event.seat,
                "pai": _tile(event.tile),
                "tsumogiri": event.tsumogiri,
            }
        ]
    if isinstance(event, TenhouCall):
        return [_call_event(event, players=players)]
    if isinstance(event, TenhouReach):
        payload: dict[str, Any] = {
            "type": "reach" if event.step == 1 else "reach_accepted",
            "actor": event.seat,
        }
        if event.step != 1 and event.scores is not None:
            payload["scores"] = list(event.scores)
        return [payload]
    if isinstance(event, TenhouAgari):
        return [_hora_event(event)]
    return [_ryukyoku_event(event)]


def _call_event(event: TenhouCall, *, players: int) -> dict[str, Any]:
    event_type = {
        ActionKind.CHI: "chi",
        ActionKind.PON: "pon",
        ActionKind.MINKAN: "daiminkan",
        ActionKind.ANKAN: "ankan",
        ActionKind.KAKAN: "kakan",
    }[event.meld.kind]
    payload: dict[str, Any] = {
        "type": event_type,
        "actor": event.seat,
        "consumed": [_tile(tile) for tile in _consumed_tiles(event.meld)],
    }
    if event.meld.kind != ActionKind.ANKAN:
        payload["target"] = _call_from_seat(event, players)
    called_tile = _called_tile(event.meld)
    if called_tile is not None:
        payload["pai"] = _tile(called_tile)
    return payload


def _hora_event(event: TenhouAgari) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "type": "hora",
        "actor": event.winner,
        "target": event.from_seat,
    }
    if event.machi is not None:
        payload["pai"] = _tile(event.machi)
    if event.points is not None:
        payload["hora_points"] = list(event.points)
    if event.score_deltas is not None:
        payload["deltas"] = list(event.score_deltas)
    if event.yaku:
        payload["yakus"] = list(event.yaku)
    if event.dora_indicators:
        payload["dora_markers"] = [_tile(tile) for tile in event.dora_indicators]
    if event.ura_dora_indicators:
        payload["uradora_markers"] = [_tile(tile) for tile in event.ura_dora_indicators]
    return payload


def _ryukyoku_event(event: TenhouRyuukyoku) -> dict[str, Any]:
    payload: dict[str, Any] = {"type": "ryukyoku"}
    if event.reason is not None:
        payload["reason"] = event.reason
    if event.scores is not None:
        payload["scores"] = list(event.scores)
    if event.score_deltas is not None:
        payload["deltas"] = list(event.score_deltas)
    return payload


def _called_tile(meld: TenhouMeld) -> Tile | None:
    return meld.added_tile if meld.kind == ActionKind.KAKAN else meld.called_tile


def _tile(tile: Tile) -> str:
    if tile.red:
        rank = tile.type.rank
        assert rank == 5
        return f"{rank}{tile.type.suit}r"
    return tile.type.notation


def _wind(round_wind: int) -> str:
    return ("E", "S", "W", "N")[round_wind]


def _unique_output_path(path: Path, existing: Sequence[Path]) -> Path:
    if path not in existing:
        return path
    for suffix in range(1, len(existing) + 2):
        candidate = path.with_name(f"{path.stem}-{suffix}{path.suffix}")
        if candidate not in existing:
            return candidate
    raise RuntimeError("failed to allocate MJAI output path")


def _call_from_seat(event: TenhouCall, players: int) -> int:
    return (event.seat + event.meld.from_offset) % players


def _consumed_tiles(meld: TenhouMeld) -> tuple[Tile, ...]:
    if meld.kind in {ActionKind.CHI, ActionKind.PON, ActionKind.MINKAN}:
        if meld.called_tile_id is None:
            raise ValueError(f"{meld.kind.value} call is missing a called tile id")
        return tuple(
            tile
            for tile_id, tile in zip(meld.tile_ids, meld.tiles, strict=True)
            if tile_id != meld.called_tile_id
        )
    if meld.kind == ActionKind.ANKAN:
        return meld.tiles
    if meld.kind == ActionKind.KAKAN:
        if meld.added_tile is None:
            raise ValueError("kakan call is missing an added tile")
        return (meld.added_tile,)
    raise ValueError(f"unsupported Tenhou call kind: {meld.kind.value}")
