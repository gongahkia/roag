from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.models import (
    CallFrequencyBaseline,
    CallLinearModel,
    DealInLinearModel,
    DiscardFrequencyBaseline,
    DiscardLinearModel,
    RiichiFrequencyBaseline,
    RiichiLinearModel,
)
from kenjaku.training import CallExample, DiscardExample, RiichiExample
from kenjaku.training.deal_in import DealInExample
from kenjaku.training.decision_snapshots import DECISION_SNAPSHOT_KIND

TORCH_EXTRA_HINT = "install with `pip install kenjaku[ml]`"
PREDICT_MODEL_TYPES = (
    "frequency",
    "linear-discard",
    "linear-call",
    "linear-riichi",
    "linear-deal-in",
    "mlp-discard",
    "transformer-discard",
)


@dataclass(frozen=True, slots=True)
class _Predictor:
    model_type: str
    model: Any
    device: str
    batch_size: int


def write_model_predictions(
    *,
    snapshots_path: Path,
    output_path: Path,
    model_type: str,
    checkpoint_path: Path | None,
    device: str = "cpu",
    batch_size: int = 64,
) -> dict[str, int]:
    if model_type not in PREDICT_MODEL_TYPES:
        raise ValueError(f"unsupported prediction model: {model_type}")
    if batch_size <= 0:
        raise ValueError("--batch-size must be positive")

    predictor = _load_predictor(
        model_type=model_type,
        checkpoint_path=checkpoint_path,
        device=device,
        batch_size=batch_size,
    )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    predictions = 0
    malformed = 0
    unsupported = 0
    with (
        snapshots_path.open(encoding="utf-8") as source,
        output_path.open(
            "w",
            encoding="utf-8",
        ) as target,
    ):
        for line in source:
            try:
                snapshot = json.loads(line)
            except json.JSONDecodeError:
                malformed += 1
                continue
            if not _is_decision_snapshot_payload(snapshot):
                malformed += 1
                continue
            try:
                prediction = _prediction_for_snapshot(snapshot, predictor)
            except (KeyError, TypeError, ValueError):
                malformed += 1
                continue
            if prediction is None:
                unsupported += 1
                continue
            target.write(json.dumps(prediction, sort_keys=True) + "\n")
            predictions += 1
    return {
        "predictions": predictions,
        "malformed_snapshot_rows": malformed,
        "unsupported_snapshot_rows": unsupported,
    }


def _load_predictor(
    *,
    model_type: str,
    checkpoint_path: Path | None,
    device: str,
    batch_size: int,
) -> _Predictor:
    if model_type == "frequency":
        return _Predictor(
            model_type=model_type,
            model=_load_frequency_models(checkpoint_path),
            device=device,
            batch_size=batch_size,
        )
    if checkpoint_path is None:
        raise ValueError(f"--checkpoint is required for --model {model_type}")
    if model_type == "linear-discard":
        model = DiscardLinearModel.load(checkpoint_path)
    elif model_type == "linear-call":
        model = CallLinearModel.load(checkpoint_path)
    elif model_type == "linear-riichi":
        model = RiichiLinearModel.load(checkpoint_path)
    elif model_type == "linear-deal-in":
        model = DealInLinearModel.load(checkpoint_path)
    elif model_type == "mlp-discard":
        model = _load_mlp_checkpoint(checkpoint_path, device=device)
    elif model_type == "transformer-discard":
        model = _load_transformer_checkpoint(checkpoint_path, device=device)
    else:
        raise ValueError(f"unsupported prediction model: {model_type}")
    return _Predictor(
        model_type=model_type,
        model=model,
        device=device,
        batch_size=batch_size,
    )


def _load_frequency_models(checkpoint_path: Path | None) -> dict[str, Any]:
    if checkpoint_path is None:
        return {
            "discard": DiscardFrequencyBaseline(tuple([0] * 34)),
            "call": CallFrequencyBaseline((0, 0, 0, 0)),
            "riichi": RiichiFrequencyBaseline((0, 0)),
        }
    payload = json.loads(checkpoint_path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError("frequency checkpoint must be a JSON object")
    discard_counts = _optional_counts(payload, "discard_counts", 34)
    call_counts = _optional_counts(payload, "call_counts", 4)
    riichi_counts = _optional_counts(payload, "riichi_counts", 2)
    if discard_counts is None and "counts" in payload:
        discard_counts = _optional_counts(payload, "counts", 34)
    return {
        "discard": DiscardFrequencyBaseline(discard_counts or tuple([0] * 34)),
        "call": CallFrequencyBaseline(call_counts or (0, 0, 0, 0)),
        "riichi": RiichiFrequencyBaseline(riichi_counts or (0, 0)),
    }


def _prediction_for_snapshot(
    snapshot: dict[str, Any],
    predictor: _Predictor,
) -> dict[str, Any] | None:
    decision_type = str(snapshot["decision_type"])
    if predictor.model_type == "frequency":
        return _frequency_prediction(snapshot, predictor.model)
    if predictor.model_type == "linear-discard" and decision_type == "discard":
        example = _discard_example_from_snapshot(snapshot)
        tile = predictor.model.predict(example.hand_counts, example.visible_counts)
        return _prediction_row(snapshot, _legal_discard_action(snapshot, tile))
    if predictor.model_type == "linear-call" and decision_type == "call":
        example = _call_example_from_snapshot(snapshot)
        kind = predictor.model.predict(example)
        return _prediction_row(snapshot, _legal_kind_action(snapshot, kind))
    if predictor.model_type == "linear-riichi" and decision_type == "riichi":
        example = _riichi_example_from_snapshot(snapshot)
        kind = predictor.model.predict(example)
        return _prediction_row(snapshot, _legal_kind_action(snapshot, kind))
    if predictor.model_type == "linear-deal-in" and decision_type == "discard":
        return _deal_in_prediction(snapshot, predictor.model)
    if predictor.model_type == "mlp-discard" and decision_type == "discard":
        tile = _predict_mlp_discard(snapshot, predictor)
        return _prediction_row(snapshot, _legal_discard_action(snapshot, tile))
    if predictor.model_type == "transformer-discard" and decision_type == "discard":
        tile = _predict_transformer_discard(snapshot, predictor)
        return _prediction_row(snapshot, _legal_discard_action(snapshot, tile))
    return None


def _frequency_prediction(snapshot: dict[str, Any], models: dict[str, Any]) -> dict[str, Any]:
    decision_type = str(snapshot["decision_type"])
    if decision_type == "discard":
        example = _discard_example_from_snapshot(snapshot)
        tile = models["discard"].predict(example.hand_counts)
        action = _legal_discard_action(snapshot, tile)
    elif decision_type == "call":
        example = _call_example_from_snapshot(snapshot)
        kind = models["call"].predict(example)
        action = _legal_kind_action(snapshot, kind)
    elif decision_type == "riichi":
        example = _riichi_example_from_snapshot(snapshot)
        kind = models["riichi"].predict(example)
        action = _legal_kind_action(snapshot, kind)
    else:
        raise ValueError(f"unsupported snapshot decision_type: {decision_type}")
    return _prediction_row(snapshot, action)


def _deal_in_prediction(
    snapshot: dict[str, Any],
    model: DealInLinearModel,
) -> dict[str, Any]:
    scored = []
    for action in _legal_action_dicts(snapshot):
        if action.get("kind") != ActionKind.DISCARD.value:
            continue
        tile = TileType.parse(str(action["tile"]))
        discard = _discard_example_from_snapshot(snapshot, action=Action.discard(tile))
        example = DealInExample(
            discard=discard,
            dealt_in=False,
            label_source="prediction",
            outcome_kind="unknown",
        )
        scored.append((model.predict_probability(example), tile.notation, action))
    if not scored:
        raise ValueError("discard snapshot has no legal discard actions")
    _probability, _tile, action = min(scored, key=lambda item: (item[0], item[1]))
    return _prediction_row(snapshot, action)


def _predict_mlp_discard(snapshot: dict[str, Any], predictor: _Predictor) -> TileType:
    try:
        from kenjaku.models.torch_discard import predict_discard_tiles, require_torch

        require_torch()
    except ImportError as error:
        raise ValueError(
            f"PyTorch is required for --model mlp-discard; {TORCH_EXTRA_HINT}"
        ) from error
    example = _discard_example_from_snapshot(snapshot)
    return predict_discard_tiles(
        predictor.model,
        [example],
        batch_size=predictor.batch_size,
        device=predictor.device,
    )[0]


def _predict_transformer_discard(snapshot: dict[str, Any], predictor: _Predictor) -> TileType:
    try:
        from kenjaku.models.torch_discard import require_torch
        from kenjaku.models.torch_transformer import predict_discard_tiles

        require_torch()
    except ImportError as error:
        raise ValueError(
            f"PyTorch is required for --model transformer-discard; {TORCH_EXTRA_HINT}"
        ) from error
    example = _discard_example_from_snapshot(snapshot)
    return predict_discard_tiles(
        predictor.model,
        [example],
        batch_size=predictor.batch_size,
        device=predictor.device,
    )[0]


def _load_mlp_checkpoint(path: Path, *, device: str) -> Any:
    try:
        from kenjaku.models.torch_discard import load_discard_mlp_checkpoint, require_torch

        require_torch()
    except ImportError as error:
        raise ValueError(
            f"PyTorch is required for --model mlp-discard; {TORCH_EXTRA_HINT}"
        ) from error
    return load_discard_mlp_checkpoint(path, device=device)


def _load_transformer_checkpoint(path: Path, *, device: str) -> Any:
    try:
        from kenjaku.models.torch_discard import require_torch
        from kenjaku.models.torch_transformer import load_discard_transformer_checkpoint

        require_torch()
    except ImportError as error:
        raise ValueError(
            f"PyTorch is required for --model transformer-discard; {TORCH_EXTRA_HINT}"
        ) from error
    return load_discard_transformer_checkpoint(path, device=device)


def _discard_example_from_snapshot(
    snapshot: dict[str, Any],
    *,
    action: Action | None = None,
) -> DiscardExample:
    actual_action = _action_from_payload(snapshot.get("actual_action"))
    selected_action = actual_action if action is None else action
    return DiscardExample(
        round_index=int(snapshot.get("round_index", 0)),
        event_index=int(snapshot.get("event_index", 0)),
        seat=int(snapshot["seat"]),
        dealer=int(snapshot.get("dealer", 0)),
        scores=_int_tuple(snapshot.get("scores")),
        hand_counts=_counts(snapshot.get("hand_counts"), length=34, label="hand_counts"),
        visible_counts=_counts(
            snapshot.get("visible_counts"),
            length=34,
            label="visible_counts",
        ),
        action=selected_action,
        discard_is_tsumogiri=selected_action.tsumogiri,
        active_riichi_seats=_bool_tuple(snapshot.get("active_riichi_seats")),
        river_counts_by_seat=_river_counts(snapshot.get("river_counts_by_seat")),
        dora_indicators=_tiles(snapshot.get("dora_indicators")),
    )


def _call_example_from_snapshot(snapshot: dict[str, Any]) -> CallExample:
    return CallExample(
        round_index=int(snapshot.get("round_index", 0)),
        event_index=int(snapshot.get("event_index", 0)),
        call_event_index=_optional_int(snapshot.get("call_event_index")),
        seat=int(snapshot["seat"]),
        from_seat=int(snapshot["from_seat"]),
        dealer=int(snapshot.get("dealer", 0)),
        scores=_int_tuple(snapshot.get("scores")),
        discarded_tile=Tile.parse(str(snapshot["discarded_tile"])),
        legal_call_kinds=_legal_call_kinds(snapshot),
        hand_counts=_counts(snapshot.get("hand_counts"), length=34, label="hand_counts"),
        visible_counts=_counts(
            snapshot.get("visible_counts"),
            length=34,
            label="visible_counts",
        ),
        action=_action_from_payload(snapshot.get("actual_action")),
    )


def _riichi_example_from_snapshot(snapshot: dict[str, Any]) -> RiichiExample:
    river_counts = _river_counts(snapshot.get("river_counts_by_seat"))
    return RiichiExample(
        round_index=int(snapshot.get("round_index", 0)),
        event_index=int(snapshot.get("event_index", 0)),
        riichi_event_index=_optional_int(snapshot.get("riichi_event_index")),
        seat=int(snapshot["seat"]),
        dealer=int(snapshot.get("dealer", 0)),
        scores=_int_tuple(snapshot.get("scores")),
        hand_counts=_counts(snapshot.get("hand_counts"), length=34, label="hand_counts"),
        visible_counts=_counts(
            snapshot.get("visible_counts"),
            length=34,
            label="visible_counts",
        ),
        active_riichi_seats=_bool_tuple(snapshot.get("active_riichi_seats")),
        river_counts_by_seat=river_counts,
        seat_turn_index=_seat_turn_index(snapshot, river_counts),
        action=_action_from_payload(snapshot.get("actual_action")),
    )


def _legal_call_kinds(snapshot: dict[str, Any]) -> tuple[ActionKind, ...]:
    kinds = []
    for action in _legal_action_dicts(snapshot):
        kind = ActionKind(str(action.get("kind")))
        if kind not in {ActionKind.PASS, ActionKind.RIICHI}:
            kinds.append(kind)
    return tuple(kinds)


def _legal_discard_action(snapshot: dict[str, Any], tile: TileType) -> dict[str, Any]:
    for action in _legal_action_dicts(snapshot):
        if action.get("kind") == ActionKind.DISCARD.value and action.get("tile") == tile.notation:
            return action
    raise ValueError(f"predicted illegal discard: {tile.notation}")


def _legal_kind_action(snapshot: dict[str, Any], kind: ActionKind) -> dict[str, Any]:
    for action in _legal_action_dicts(snapshot):
        if action.get("kind") == kind.value:
            return action
    raise ValueError(f"predicted illegal action kind: {kind.value}")


def _legal_action_dicts(snapshot: dict[str, Any]) -> list[dict[str, Any]]:
    actions = snapshot.get("legal_actions")
    if not isinstance(actions, list):
        raise ValueError("snapshot legal_actions must be a list")
    return [action for action in actions if isinstance(action, dict)]


def _prediction_row(snapshot: dict[str, Any], action: dict[str, Any]) -> dict[str, Any]:
    row_id = snapshot.get("row_id")
    if not isinstance(row_id, str):
        raise ValueError("snapshot row_id must be a string")
    return {"row_id": row_id, "predicted_action": _normalized_action(action)}


def _action_from_payload(payload: Any) -> Action:
    if not isinstance(payload, dict):
        raise ValueError("snapshot action must be an object")
    kind = ActionKind(str(payload["kind"]))
    tile = None
    if payload.get("tile") is not None:
        tile = TileType.parse(str(payload["tile"]))
    consumed = tuple(Tile.parse(str(tile_value)) for tile_value in payload.get("consumed", []))
    return Action(
        kind=kind,
        tile=tile,
        tsumogiri=bool(payload.get("tsumogiri", False)),
        consumed=consumed,
    )


def _normalized_action(action: dict[str, Any]) -> dict[str, Any]:
    normalized = {"kind": action.get("kind")}
    if "tile" in action:
        normalized["tile"] = action.get("tile")
    if "tsumogiri" in action:
        normalized["tsumogiri"] = bool(action.get("tsumogiri"))
    if "consumed" in action and isinstance(action["consumed"], list):
        normalized["consumed"] = list(action["consumed"])
    return normalized


def _is_decision_snapshot_payload(value: Any) -> bool:
    return (
        isinstance(value, dict)
        and value.get("kind") == DECISION_SNAPSHOT_KIND
        and isinstance(value.get("decision_type"), str)
    )


def _counts(value: Any, *, length: int, label: str) -> tuple[int, ...]:
    if not isinstance(value, list):
        raise ValueError(f"{label} must be a list")
    counts = tuple(int(count) for count in value)
    if len(counts) != length:
        raise ValueError(f"{label} must contain {length} counts")
    return counts


def _optional_counts(payload: dict[str, Any], key: str, length: int) -> tuple[int, ...] | None:
    if key not in payload:
        return None
    return _counts(payload[key], length=length, label=key)


def _river_counts(value: Any) -> tuple[tuple[int, ...], ...]:
    if not isinstance(value, list):
        return ()
    return tuple(_counts(row, length=34, label="river_counts_by_seat") for row in value)


def _tiles(value: Any) -> tuple[Tile, ...]:
    if not isinstance(value, list):
        return ()
    return tuple(Tile.parse(str(tile)) for tile in value)


def _bool_tuple(value: Any) -> tuple[bool, ...]:
    if not isinstance(value, list):
        return ()
    return tuple(bool(item) for item in value)


def _int_tuple(value: Any) -> tuple[int, ...]:
    if not isinstance(value, list):
        return ()
    return tuple(int(item) for item in value)


def _optional_int(value: Any) -> int | None:
    return None if value is None else int(value)


def _seat_turn_index(
    snapshot: dict[str, Any],
    river_counts: tuple[tuple[int, ...], ...],
) -> int:
    if snapshot.get("seat_turn_index") is not None:
        return int(snapshot["seat_turn_index"])
    seat = int(snapshot["seat"])
    if seat < len(river_counts):
        return sum(river_counts[seat])
    return 0
