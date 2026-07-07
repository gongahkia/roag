from __future__ import annotations

import json
from collections.abc import Iterable, Sequence
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Literal

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.io import TenhouParseFailure
from kenjaku.training.call_examples import CallExample
from kenjaku.training.discard_examples import DiscardExample
from kenjaku.training.riichi_examples import RiichiExample

BC_EXAMPLE_ROW_KIND = "kenjaku-bc-example-v0"
BC_EXAMPLE_MANIFEST_KIND = "kenjaku-bc-example-manifest-v0"
BC_DECISION_TYPES = ("discard", "call", "riichi")
BcDecisionType = Literal["discard", "call", "riichi"]
BcExample = DiscardExample | CallExample | RiichiExample


@dataclass(frozen=True, slots=True)
class BcExampleShard:
    path: Path
    decision_type: BcDecisionType
    examples: int


@dataclass(frozen=True, slots=True)
class BcExampleLoad:
    examples: list[Any]
    total_examples: int
    example_files: tuple[Path, ...]
    source_files: tuple[Path, ...]
    parse_failures: tuple[TenhouParseFailure, ...]
    game_counts: dict[str, int]
    decision_counts: dict[str, int]
    manifest_paths: tuple[Path, ...]
    source: dict[str, str | None]


def parse_bc_decision_types(value: str) -> tuple[BcDecisionType, ...]:
    selected: list[BcDecisionType] = []
    for raw_name in value.split(","):
        name = raw_name.strip()
        if not name:
            continue
        if name not in BC_DECISION_TYPES:
            raise ValueError(f"unsupported BC decision type: {name}")
        decision_type = _decision_type(name)
        if decision_type not in selected:
            selected.append(decision_type)
    if not selected:
        raise ValueError("--actions must select at least one decision type")
    return tuple(selected)


def bc_example_to_payload(example: BcExample) -> dict[str, Any]:
    if isinstance(example, DiscardExample):
        return _discard_example_to_payload(example)
    if isinstance(example, CallExample):
        return _call_example_to_payload(example)
    if isinstance(example, RiichiExample):
        return _riichi_example_to_payload(example)
    raise TypeError(f"unsupported BC example type: {type(example).__name__}")


def bc_example_from_payload(decision_type: str, payload: Any) -> BcExample:
    if not isinstance(payload, dict):
        raise ValueError("BC example payload must be an object")
    if decision_type == "discard":
        return _discard_example_from_payload(payload)
    if decision_type == "call":
        return _call_example_from_payload(payload)
    if decision_type == "riichi":
        return _riichi_example_from_payload(payload)
    raise ValueError(f"unsupported BC decision type: {decision_type}")


def write_bc_example_row(
    handle: Any,
    *,
    decision_type: BcDecisionType,
    source_file: Path,
    source_file_index: int,
    sequence_index: int,
    example: BcExample,
) -> None:
    payload = {
        "kind": BC_EXAMPLE_ROW_KIND,
        "decision_type": decision_type,
        "source_file": str(source_file),
        "source_file_index": source_file_index,
        "sequence_index": sequence_index,
        "example": bc_example_to_payload(example),
    }
    handle.write(json.dumps(payload, sort_keys=True, separators=(",", ":")) + "\n")


def read_bc_examples(
    paths: Sequence[str | Path],
    *,
    decision_type: BcDecisionType,
    limit: int | None = None,
) -> BcExampleLoad:
    if limit is not None and limit < 0:
        raise ValueError("limit must be non-negative")
    example_files, manifest_paths = bc_example_files(paths, decision_type=decision_type)
    if not example_files:
        raise FileNotFoundError(f"no {decision_type} BC example JSONL files found")

    examples: list[Any] = []
    total_examples = 0
    for file in example_files:
        with file.open(encoding="utf-8") as handle:
            for line_number, line in enumerate(handle, start=1):
                if not line.strip():
                    continue
                row = json.loads(line)
                row_type = _row_decision_type(row, file=file, line_number=line_number)
                if row_type != decision_type:
                    continue
                total_examples += 1
                if limit is None or len(examples) < limit:
                    examples.append(bc_example_from_payload(row_type, row.get("example")))

    source_files, parse_failures, game_counts, decision_counts, source = _manifest_metadata(
        manifest_paths,
    )
    if not source_files:
        source_files = example_files
    if not decision_counts:
        decision_counts = {decision_type: total_examples}
    elif decision_type not in decision_counts:
        decision_counts[decision_type] = total_examples

    return BcExampleLoad(
        examples=examples,
        total_examples=total_examples,
        example_files=example_files,
        source_files=source_files,
        parse_failures=parse_failures,
        game_counts=game_counts,
        decision_counts=decision_counts,
        manifest_paths=manifest_paths,
        source=source,
    )


def bc_example_files(
    paths: Sequence[str | Path],
    *,
    decision_type: BcDecisionType,
) -> tuple[tuple[Path, ...], tuple[Path, ...]]:
    files: list[Path] = []
    manifest_paths: list[Path] = []
    for raw_path in paths:
        path = Path(raw_path)
        if path.is_dir():
            manifest = path / "manifest.json"
            if manifest.is_file():
                files.extend(_files_from_manifest(manifest, decision_type=decision_type))
                manifest_paths.append(manifest.resolve())
                continue
            files.extend(sorted(path.rglob(f"{decision_type}-*.jsonl")))
            continue
        if path.is_file() and path.suffix == ".json":
            files.extend(_files_from_manifest(path, decision_type=decision_type))
            manifest_paths.append(path.resolve())
            continue
        if path.is_file():
            files.append(path)
            continue
        raise FileNotFoundError(f"BC example path not found: {path}")
    unique_files = dict.fromkeys(file.resolve() for file in files)
    unique_manifests = dict.fromkeys(manifest.resolve() for manifest in manifest_paths)
    return tuple(sorted(unique_files)), tuple(sorted(unique_manifests))


def build_bc_manifest(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    parsed_files: Sequence[Path],
    parse_failures: Sequence[TenhouParseFailure],
    game_counts: dict[str, int],
    decision_counts: dict[str, int],
    shards: Sequence[BcExampleShard],
    source: dict[str, str | None],
    output_dir: Path,
    shard_size: int,
    source_complete: bool,
) -> dict[str, Any]:
    return {
        "kind": BC_EXAMPLE_MANIFEST_KIND,
        "source": source,
        "input_paths": [str(path) for path in input_paths],
        "xml_files": [str(path) for path in xml_files],
        "xml_file_count": len(xml_files),
        "parsed_xml_files": [str(path) for path in parsed_files],
        "parsed_xml_file_count": len(parsed_files),
        "source_complete": source_complete,
        "game_counts": dict(game_counts),
        "decision_counts": {
            decision_type: int(decision_counts.get(decision_type, 0))
            for decision_type in BC_DECISION_TYPES
        },
        "parse_failures": [
            {
                "path": str(failure.path),
                "error_type": failure.error_type,
                "message": failure.message,
            }
            for failure in parse_failures
        ],
        "shard_size": shard_size,
        "shards": [
            {
                "path": str(shard.path.relative_to(output_dir)),
                "decision_type": shard.decision_type,
                "examples": shard.examples,
            }
            for shard in shards
        ],
    }


def write_bc_manifest(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _files_from_manifest(path: Path, *, decision_type: BcDecisionType) -> tuple[Path, ...]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict) or payload.get("kind") != BC_EXAMPLE_MANIFEST_KIND:
        raise ValueError(f"not a BC example manifest: {path}")
    shards = payload.get("shards")
    if not isinstance(shards, list):
        raise ValueError("BC example manifest shards must be a list")
    files: list[Path] = []
    for shard in shards:
        if not isinstance(shard, dict):
            raise ValueError("BC example manifest shard rows must be objects")
        if shard.get("decision_type") != decision_type:
            continue
        shard_path = Path(str(shard["path"]))
        if not shard_path.is_absolute():
            shard_path = path.parent / shard_path
        files.append(shard_path)
    return tuple(files)


def _manifest_metadata(
    manifest_paths: Sequence[Path],
) -> tuple[
    tuple[Path, ...],
    tuple[TenhouParseFailure, ...],
    dict[str, int],
    dict[str, int],
    dict[str, str | None],
]:
    source_files: list[Path] = []
    parse_failures: list[TenhouParseFailure] = []
    game_counts: dict[str, int] = {}
    decision_counts: dict[str, int] = {}
    source: dict[str, str | None] = {"label": None, "command": None, "date": None}
    for manifest_path in manifest_paths:
        payload = json.loads(manifest_path.read_text(encoding="utf-8"))
        if not isinstance(payload, dict) or payload.get("kind") != BC_EXAMPLE_MANIFEST_KIND:
            raise ValueError(f"not a BC example manifest: {manifest_path}")
        manifest_source = payload.get("source")
        if isinstance(manifest_source, dict) and source["label"] is None:
            source = {
                "label": _optional_str(manifest_source.get("label")),
                "command": _optional_str(manifest_source.get("command")),
                "date": _optional_str(manifest_source.get("date")),
            }
        source_files.extend(Path(str(path)) for path in payload.get("xml_files", []))
        failures = payload.get("parse_failures", [])
        if isinstance(failures, list):
            parse_failures.extend(_parse_failure_from_payload(item) for item in failures)
        counts = payload.get("game_counts", {})
        if isinstance(counts, dict):
            _add_counts(game_counts, counts)
        decisions = payload.get("decision_counts", {})
        if isinstance(decisions, dict):
            _add_counts(decision_counts, decisions)
    return (
        tuple(dict.fromkeys(source_files)),
        tuple(parse_failures),
        game_counts,
        decision_counts,
        source,
    )


def _row_decision_type(row: Any, *, file: Path, line_number: int) -> BcDecisionType:
    if not isinstance(row, dict) or row.get("kind") != BC_EXAMPLE_ROW_KIND:
        raise ValueError(f"not a BC example row: {file}:{line_number}")
    return _decision_type(str(row.get("decision_type")))


def _decision_type(value: str) -> BcDecisionType:
    if value == "discard":
        return "discard"
    if value == "call":
        return "call"
    if value == "riichi":
        return "riichi"
    raise ValueError(f"unsupported BC decision type: {value}")


def _discard_example_to_payload(example: DiscardExample) -> dict[str, Any]:
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "seat": example.seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "hand_counts": list(example.hand_counts),
        "visible_counts": list(example.visible_counts),
        "action": _action_to_payload(example.action),
        "discard_is_tsumogiri": example.discard_is_tsumogiri,
        "active_riichi_seats": list(example.active_riichi_seats),
        "river_counts_by_seat": _nested_ints_to_payload(example.river_counts_by_seat),
        "seat_turn_index": example.seat_turn_index,
        "rivers_by_seat": [
            [tile.notation for tile in river]
            for river in example.rivers_by_seat
        ],
        "riichi_declared_turns": list(example.riichi_declared_turns),
        "riichi_declared_event_indices": list(example.riichi_declared_event_indices),
        "meld_counts_by_seat": _nested_ints_to_payload(example.meld_counts_by_seat),
        "meld_tiles_by_seat": [
            [tile.notation for tile in meld_tiles]
            for meld_tiles in example.meld_tiles_by_seat
        ],
        "dora_indicators": [tile.notation for tile in example.dora_indicators],
        "last_discard_tsumogiri_by_seat": list(example.last_discard_tsumogiri_by_seat),
        "ippatsu_active_seats": list(example.ippatsu_active_seats),
    }


def _discard_example_from_payload(payload: dict[str, Any]) -> DiscardExample:
    action = _action_from_payload(payload.get("action"))
    return DiscardExample(
        round_index=int(payload["round_index"]),
        event_index=int(payload["event_index"]),
        seat=int(payload["seat"]),
        dealer=int(payload["dealer"]),
        scores=_int_tuple(payload, "scores"),
        hand_counts=_tile_counts_from_payload(payload, "hand_counts"),
        visible_counts=_tile_counts_from_payload(payload, "visible_counts"),
        action=action,
        discard_is_tsumogiri=bool(payload.get("discard_is_tsumogiri", action.tsumogiri)),
        active_riichi_seats=_bool_tuple(payload.get("active_riichi_seats", [])),
        river_counts_by_seat=_nested_counts_from_payload(payload, "river_counts_by_seat"),
        seat_turn_index=int(payload.get("seat_turn_index", 0)),
        rivers_by_seat=_nested_tiles_from_payload(payload, "rivers_by_seat"),
        riichi_declared_turns=_optional_int_tuple(payload.get("riichi_declared_turns", [])),
        riichi_declared_event_indices=_optional_int_tuple(
            payload.get("riichi_declared_event_indices", []),
        ),
        meld_counts_by_seat=_nested_counts_from_payload(payload, "meld_counts_by_seat"),
        meld_tiles_by_seat=_nested_tiles_from_payload(payload, "meld_tiles_by_seat"),
        dora_indicators=_tiles_from_list(payload.get("dora_indicators", [])),
        last_discard_tsumogiri_by_seat=_optional_bool_tuple(
            payload.get("last_discard_tsumogiri_by_seat", []),
        ),
        ippatsu_active_seats=_bool_tuple(payload.get("ippatsu_active_seats", [])),
    )


def _call_example_to_payload(example: CallExample) -> dict[str, Any]:
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "call_event_index": example.call_event_index,
        "seat": example.seat,
        "from_seat": example.from_seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "discarded_tile": example.discarded_tile.notation,
        "legal_call_kinds": [kind.value for kind in example.legal_call_kinds],
        "hand_counts": list(example.hand_counts),
        "visible_counts": list(example.visible_counts),
        "action": _action_to_payload(example.action),
    }


def _call_example_from_payload(payload: dict[str, Any]) -> CallExample:
    return CallExample(
        round_index=int(payload["round_index"]),
        event_index=int(payload["event_index"]),
        call_event_index=_optional_int(payload.get("call_event_index")),
        seat=int(payload["seat"]),
        from_seat=int(payload["from_seat"]),
        dealer=int(payload["dealer"]),
        scores=_int_tuple(payload, "scores"),
        discarded_tile=Tile.parse(str(payload["discarded_tile"])),
        legal_call_kinds=tuple(
            ActionKind(str(kind))
            for kind in _required_list(payload, "legal_call_kinds")
        ),
        hand_counts=_tile_counts_from_payload(payload, "hand_counts"),
        visible_counts=_tile_counts_from_payload(payload, "visible_counts"),
        action=_action_from_payload(payload.get("action")),
    )


def _riichi_example_to_payload(example: RiichiExample) -> dict[str, Any]:
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "riichi_event_index": example.riichi_event_index,
        "seat": example.seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "hand_counts": list(example.hand_counts),
        "visible_counts": list(example.visible_counts),
        "active_riichi_seats": list(example.active_riichi_seats),
        "river_counts_by_seat": _nested_ints_to_payload(example.river_counts_by_seat),
        "seat_turn_index": example.seat_turn_index,
        "action": _action_to_payload(example.action),
    }


def _riichi_example_from_payload(payload: dict[str, Any]) -> RiichiExample:
    return RiichiExample(
        round_index=int(payload["round_index"]),
        event_index=int(payload["event_index"]),
        riichi_event_index=_optional_int(payload.get("riichi_event_index")),
        seat=int(payload["seat"]),
        dealer=int(payload["dealer"]),
        scores=_int_tuple(payload, "scores"),
        hand_counts=_tile_counts_from_payload(payload, "hand_counts"),
        visible_counts=_tile_counts_from_payload(payload, "visible_counts"),
        active_riichi_seats=_bool_tuple(payload.get("active_riichi_seats", [])),
        river_counts_by_seat=_nested_counts_from_payload(payload, "river_counts_by_seat"),
        seat_turn_index=int(payload.get("seat_turn_index", 0)),
        action=_action_from_payload(payload.get("action")),
    )


def _action_to_payload(action: Action) -> dict[str, Any]:
    payload: dict[str, Any] = {"kind": action.kind.value}
    if action.tile is not None:
        payload["tile"] = action.tile.notation
    if action.kind == ActionKind.DISCARD:
        payload["tsumogiri"] = action.tsumogiri
    if action.consumed:
        payload["consumed"] = [tile.notation for tile in action.consumed]
    return payload


def _action_from_payload(payload: Any) -> Action:
    if not isinstance(payload, dict):
        raise ValueError("BC example action must be an object")
    kind = ActionKind(str(payload["kind"]))
    tile = None
    if payload.get("tile") is not None:
        tile = TileType.parse(str(payload["tile"]))
    consumed = tuple(Tile.parse(str(tile)) for tile in payload.get("consumed", []))
    return Action(
        kind=kind,
        tile=tile,
        tsumogiri=bool(payload.get("tsumogiri", False)),
        consumed=consumed,
    )


def _parse_failure_from_payload(payload: Any) -> TenhouParseFailure:
    if not isinstance(payload, dict):
        raise ValueError("parse failure payload must be an object")
    return TenhouParseFailure(
        path=Path(str(payload["path"])),
        error_type=str(payload["error_type"]),
        message=str(payload["message"]),
    )


def _add_counts(target: dict[str, int], counts: dict[str, Any]) -> None:
    for key, value in counts.items():
        target[str(key)] = target.get(str(key), 0) + int(value)


def _nested_ints_to_payload(values: Iterable[Iterable[int]]) -> list[list[int]]:
    return [[int(value) for value in row] for row in values]


def _int_tuple(payload: dict[str, Any], key: str) -> tuple[int, ...]:
    return tuple(int(value) for value in _required_list(payload, key))


def _bool_tuple(value: Any) -> tuple[bool, ...]:
    if not isinstance(value, list):
        raise ValueError("expected a bool list")
    return tuple(bool(item) for item in value)


def _optional_bool_tuple(value: Any) -> tuple[bool | None, ...]:
    if not isinstance(value, list):
        raise ValueError("expected an optional bool list")
    return tuple(None if item is None else bool(item) for item in value)


def _optional_int_tuple(value: Any) -> tuple[int | None, ...]:
    if not isinstance(value, list):
        raise ValueError("expected an optional int list")
    return tuple(None if item is None else int(item) for item in value)


def _nested_counts_from_payload(payload: dict[str, Any], key: str) -> tuple[tuple[int, ...], ...]:
    value = payload.get(key, [])
    if not isinstance(value, list):
        raise ValueError(f"{key} must be a list")
    return tuple(tuple(int(item) for item in row) for row in value)


def _nested_tiles_from_payload(payload: dict[str, Any], key: str) -> tuple[tuple[Tile, ...], ...]:
    value = payload.get(key, [])
    if not isinstance(value, list):
        raise ValueError(f"{key} must be a list")
    return tuple(_tiles_from_list(row) for row in value)


def _tiles_from_list(value: Any) -> tuple[Tile, ...]:
    if not isinstance(value, list):
        raise ValueError("expected a tile list")
    return tuple(Tile.parse(str(tile)) for tile in value)


def _tile_counts_from_payload(payload: dict[str, Any], key: str) -> tuple[int, ...]:
    counts = _int_tuple(payload, key)
    if len(counts) != 34:
        raise ValueError(f"{key} must contain 34 tile counts")
    return counts


def _required_list(payload: dict[str, Any], key: str) -> list[Any]:
    value = payload.get(key)
    if not isinstance(value, list):
        raise ValueError(f"{key} must be a list")
    return value


def _optional_int(value: Any) -> int | None:
    if value is None:
        return None
    return int(value)


def _optional_str(value: Any) -> str | None:
    if value is None:
        return None
    return str(value)
