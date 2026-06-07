from __future__ import annotations

import json
from collections.abc import Iterable, Sequence
from hashlib import blake2b
from pathlib import Path
from typing import Any

from kenjaku.core import Action, ActionKind, Tile, all_tile_types
from kenjaku.io import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouGame,
    TenhouReach,
    TenhouRound,
    TenhouRyuukyoku,
)
from kenjaku.training.call_examples import CallExample, iter_call_examples
from kenjaku.training.discard_examples import DiscardExample, iter_discard_examples
from kenjaku.training.outcomes import round_outcome_payload
from kenjaku.training.reconstruction import call_from_seat, consumed_tiles
from kenjaku.training.riichi_examples import RiichiExample, iter_riichi_examples

DECISION_SNAPSHOT_KIND = "kenjaku-decision-snapshot-v0"
DECISION_SNAPSHOT_TYPES = ("discard", "call", "riichi")


def build_decision_snapshots(
    game: TenhouGame,
    *,
    decision_types: Sequence[str] = DECISION_SNAPSHOT_TYPES,
    limit: int | None = None,
    source: dict[str, str | None] | None = None,
    input_paths: Sequence[Path] = (),
    xml_file_count: int | None = None,
    include_outcome: bool = False,
) -> list[dict[str, Any]]:
    selected_types = tuple(
        _validate_decision_type(decision_type)
        for decision_type in decision_types
    )
    if limit is not None and limit < 0:
        raise ValueError("limit must be non-negative")

    indexed: list[tuple[tuple[int, int, int, int], dict[str, Any]]] = []
    prefix_cache: dict[tuple[int, int, bool], list[dict[str, Any]]] = {}
    input_path_payload = tuple(str(path) for path in input_paths)

    if "discard" in selected_types:
        indexed.extend(
            _discard_snapshot_item(
                example,
                game=game,
                prefix_cache=prefix_cache,
                source=source,
                input_paths=input_path_payload,
                xml_file_count=xml_file_count,
                include_outcome=include_outcome,
            )
            for example in iter_discard_examples(game)
        )
    if "call" in selected_types:
        indexed.extend(
            _call_snapshot_item(
                example,
                game=game,
                prefix_cache=prefix_cache,
                source=source,
                input_paths=input_path_payload,
                xml_file_count=xml_file_count,
                include_outcome=include_outcome,
            )
            for example in iter_call_examples(game)
        )
    if "riichi" in selected_types:
        indexed.extend(
            _riichi_snapshot_item(
                example,
                game=game,
                prefix_cache=prefix_cache,
                source=source,
                input_paths=input_path_payload,
                xml_file_count=xml_file_count,
                include_outcome=include_outcome,
            )
            for example in iter_riichi_examples(game)
        )

    indexed.sort(key=lambda item: item[0])
    snapshots = [snapshot for _, snapshot in indexed]
    if limit is not None:
        return snapshots[:limit]
    return snapshots


def write_decision_snapshots_jsonl(path: str | Path, snapshots: Iterable[dict[str, Any]]) -> int:
    output_path = Path(path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with output_path.open("w", encoding="utf-8") as handle:
        for snapshot in snapshots:
            count += 1
            handle.write(json.dumps(snapshot, sort_keys=True) + "\n")
    return count


def _discard_snapshot_item(
    example: DiscardExample,
    *,
    game: TenhouGame,
    prefix_cache: dict[tuple[int, int, bool], list[dict[str, Any]]],
    source: dict[str, str | None] | None,
    input_paths: Sequence[str],
    xml_file_count: int | None,
    include_outcome: bool,
) -> tuple[tuple[int, int, int, int], dict[str, Any]]:
    round_ = game.rounds[example.round_index]
    snapshot = _base_snapshot(
        decision_type="discard",
        round_=round_,
        example=example,
        game=game,
        prefix_cache=prefix_cache,
        include_decision_event=False,
        source=source,
        input_paths=input_paths,
        xml_file_count=xml_file_count,
        include_outcome=include_outcome,
    )
    snapshot["legal_actions"] = _discard_legal_actions(example.hand_counts)
    snapshot["actual_action"] = _action_payload(example.action)
    return (example.round_index, example.event_index, 0, example.seat), snapshot


def _call_snapshot_item(
    example: CallExample,
    *,
    game: TenhouGame,
    prefix_cache: dict[tuple[int, int, bool], list[dict[str, Any]]],
    source: dict[str, str | None] | None,
    input_paths: Sequence[str],
    xml_file_count: int | None,
    include_outcome: bool,
) -> tuple[tuple[int, int, int, int], dict[str, Any]]:
    round_ = game.rounds[example.round_index]
    snapshot = _base_snapshot(
        decision_type="call",
        round_=round_,
        example=example,
        game=game,
        prefix_cache=prefix_cache,
        include_decision_event=True,
        source=source,
        input_paths=input_paths,
        xml_file_count=xml_file_count,
        include_outcome=include_outcome,
    )
    snapshot["from_seat"] = example.from_seat
    snapshot["discarded_tile"] = _tile_payload(example.discarded_tile)
    snapshot["call_event_index"] = example.call_event_index
    snapshot["legal_actions"] = [
        _action_payload(Action.pass_()),
        *(
            _action_payload(Action(kind, example.discarded_tile.type))
            for kind in example.legal_call_kinds
        ),
    ]
    snapshot["actual_action"] = _action_payload(example.action)
    return (example.round_index, example.event_index, 2, example.seat), snapshot


def _riichi_snapshot_item(
    example: RiichiExample,
    *,
    game: TenhouGame,
    prefix_cache: dict[tuple[int, int, bool], list[dict[str, Any]]],
    source: dict[str, str | None] | None,
    input_paths: Sequence[str],
    xml_file_count: int | None,
    include_outcome: bool,
) -> tuple[tuple[int, int, int, int], dict[str, Any]]:
    round_ = game.rounds[example.round_index]
    snapshot = _base_snapshot(
        decision_type="riichi",
        round_=round_,
        example=example,
        game=game,
        prefix_cache=prefix_cache,
        include_decision_event=False,
        source=source,
        input_paths=input_paths,
        xml_file_count=xml_file_count,
        include_outcome=include_outcome,
    )
    snapshot["riichi_event_index"] = example.riichi_event_index
    snapshot["legal_actions"] = [
        _action_payload(Action.pass_()),
        _action_payload(Action(ActionKind.RIICHI)),
    ]
    snapshot["actual_action"] = _action_payload(example.action)
    return (example.round_index, example.event_index, 1, example.seat), snapshot


def _base_snapshot(
    *,
    decision_type: str,
    round_: TenhouRound,
    example: DiscardExample | CallExample | RiichiExample,
    game: TenhouGame,
    prefix_cache: dict[tuple[int, int, bool], list[dict[str, Any]]],
    include_decision_event: bool,
    source: dict[str, str | None] | None,
    input_paths: Sequence[str],
    xml_file_count: int | None,
    include_outcome: bool,
) -> dict[str, Any]:
    snapshot = {
        "kind": DECISION_SNAPSHOT_KIND,
        "row_id": _snapshot_row_id(
            input_paths=input_paths,
            round_index=example.round_index,
            event_index=example.event_index,
            decision_type=decision_type,
            seat=example.seat,
        ),
        "decision_type": decision_type,
        "source": source or {},
        "input_paths": list(input_paths),
        "xml_file_count": xml_file_count,
        "round_index": example.round_index,
        "event_index": example.event_index,
        "seat": example.seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "hand_counts": list(example.hand_counts),
        "visible_counts": list(example.visible_counts),
        "dora_indicators": [_tile_payload(tile) for tile in round_.dora_indicators],
        "active_riichi_seats": list(getattr(example, "active_riichi_seats", ())),
        "river_counts_by_seat": [
            list(counts)
            for counts in getattr(example, "river_counts_by_seat", ())
        ],
        "mjai_events": _mjai_events_prefix(
            game,
            example.round_index,
            example.event_index,
            include_event=include_decision_event,
            cache=prefix_cache,
        ),
    }
    if include_outcome:
        snapshot["terminal_outcome"] = round_outcome_payload(round_)
    return snapshot


def _snapshot_row_id(
    *,
    input_paths: Sequence[str],
    round_index: int,
    event_index: int,
    decision_type: str,
    seat: int,
) -> str:
    source = json.dumps(list(input_paths), sort_keys=True)
    source_digest = blake2b(source.encode("utf-8"), digest_size=8).hexdigest()
    return f"{source_digest}:r{round_index}:e{event_index}:{decision_type}:s{seat}"


def _discard_legal_actions(hand_counts: Sequence[int]) -> list[dict[str, Any]]:
    tile_types = all_tile_types()
    return [
        {
            "kind": ActionKind.DISCARD.value,
            "tile": tile_types[index].notation,
            "copies": count,
        }
        for index, count in enumerate(hand_counts)
        if count > 0
    ]


def _action_payload(action: Action) -> dict[str, Any]:
    payload: dict[str, Any] = {"kind": action.kind.value}
    if action.tile is not None:
        payload["tile"] = action.tile.notation
    if action.kind == ActionKind.DISCARD:
        payload["tsumogiri"] = action.tsumogiri
    if action.consumed:
        payload["consumed"] = [_tile_payload(tile) for tile in action.consumed]
    return payload


def _mjai_events_prefix(
    game: TenhouGame,
    round_index: int,
    event_index: int,
    *,
    include_event: bool,
    cache: dict[tuple[int, int, bool], list[dict[str, Any]]],
) -> list[dict[str, Any]]:
    key = (round_index, event_index, include_event)
    if key in cache:
        return cache[key]

    round_ = game.rounds[round_index]
    events = [_mjai_start_kyoku(round_, round_index)]
    for event in round_.events:
        if event.event_index > event_index or (
            event.event_index == event_index and not include_event
        ):
            break
        events.extend(_mjai_event(event, players=len(round_.starting_hands)))
    cache[key] = events
    return events


def _mjai_start_kyoku(round_: TenhouRound, round_index: int) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "type": "start_kyoku",
        "bakaze": "E",
        "kyoku": round_index + 1,
        "honba": 0,
        "kyotaku": 0,
        "oya": round_.dealer,
        "scores": list(round_.scores),
        "tehais": [
            [_mjai_tile(tile) for tile in hand]
            for hand in round_.starting_hands
        ],
    }
    if round_.dora_indicators:
        payload["dora_marker"] = _mjai_tile(round_.dora_indicators[0])
    return payload


def _mjai_event(event: Any, *, players: int) -> list[dict[str, Any]]:
    if isinstance(event, TenhouDraw):
        return [{"type": "tsumo", "actor": event.seat, "pai": _mjai_tile(event.tile)}]
    if isinstance(event, TenhouDiscard):
        return [
            {
                "type": "dahai",
                "actor": event.seat,
                "pai": _mjai_tile(event.tile),
                "tsumogiri": event.tsumogiri,
            }
        ]
    if isinstance(event, TenhouReach):
        if event.step == 1:
            return [{"type": "reach", "actor": event.seat}]
        return [{"type": "reach_accepted", "actor": event.seat}]
    if isinstance(event, TenhouCall):
        return [_mjai_call_event(event, players=players)]
    if isinstance(event, TenhouAgari):
        payload: dict[str, Any] = {
            "type": "hora",
            "actor": event.winner,
            "target": event.from_seat,
        }
        if event.machi is not None:
            payload["pai"] = _mjai_tile(event.machi)
        return [payload]
    if isinstance(event, TenhouRyuukyoku):
        payload = {"type": "ryukyoku"}
        if event.reason is not None:
            payload["reason"] = event.reason
        return [payload]
    return []


def _mjai_call_event(event: TenhouCall, *, players: int) -> dict[str, Any]:
    event_type = {
        ActionKind.CHI: "chi",
        ActionKind.PON: "pon",
        ActionKind.MINKAN: "daiminkan",
        ActionKind.ANKAN: "ankan",
        ActionKind.KAKAN: "kakan",
    }.get(event.meld.kind, event.meld.kind.value)
    payload: dict[str, Any] = {
        "type": event_type,
        "actor": event.seat,
        "target": call_from_seat(event, players),
        "consumed": [_tile_payload(tile) for tile in consumed_tiles(event.meld)],
    }
    called_tile = event.meld.called_tile or event.meld.added_tile
    if called_tile is not None:
        payload["pai"] = _mjai_tile(called_tile)
    return payload


def _tile_payload(tile: Tile) -> str:
    return tile.notation


def _mjai_tile(tile: Tile) -> str:
    if tile.red:
        rank = tile.type.rank
        assert rank == 5
        return f"{rank}{tile.type.suit}r"
    return tile.type.notation


def _validate_decision_type(decision_type: str) -> str:
    if decision_type not in DECISION_SNAPSHOT_TYPES:
        raise ValueError(f"unsupported decision type: {decision_type}")
    return decision_type
