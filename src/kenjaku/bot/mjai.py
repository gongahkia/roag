from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Protocol, TextIO, cast

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.models import DiscardFrequencyBaseline, DiscardLinearModel
from kenjaku.schema import (
    CheckpointCompatibilityV1,
    CheckpointManifestV1,
    CheckpointRequirementV1,
    SemanticVersion,
)
from kenjaku.training import DiscardExample

BOT_POLICY_TYPES = {
    "auto",
    "frequency",
    "linear-discard",
    "mlp",
    "mlp-discard",
    "transformer",
    "transformer-discard",
}
_BOT_CHECKPOINT_MODEL_KINDS = {
    "linear-discard": "discard-linear-v1",
    "mlp": "discard-mlp-v0",
    "mlp-discard": "discard-mlp-v0",
    "transformer": "discard-transformer-v0",
    "transformer-discard": "discard-transformer-v0",
}


class MjaiDiscardPolicy(Protocol):
    def choose_tile(self, state: BotState, legal_tiles: tuple[TileType, ...]) -> TileType: ...


@dataclass(slots=True)
class BotState:
    player_id: int
    hand: list[str] = field(default_factory=list)
    visible_tiles: list[str] = field(default_factory=list)
    rivers: list[list[str]] = field(default_factory=lambda: [[], [], [], []])
    active_riichi: list[bool] = field(default_factory=lambda: [False, False, False, False])
    scores: tuple[int, ...] = (25000, 25000, 25000, 25000)
    dealer: int = 0
    dora_markers: list[str] = field(default_factory=list)
    last_tsumo: str | None = None

    def observe(self, event: dict[str, Any]) -> None:
        event_type = event.get("type")
        if event_type == "start_game":
            seat = event.get("id")
            if isinstance(seat, int):
                self.player_id = seat
            return
        if event_type == "start_kyoku":
            self._start_kyoku(event)
            return
        if event_type == "tsumo":
            self._tsumo(event)
            return
        if event_type == "dahai":
            self._dahai(event)
            return
        if event_type in {"chi", "pon", "daiminkan", "ankan", "kakan"}:
            self._call(event)
            return
        if event_type == "reach":
            actor = event.get("actor")
            if isinstance(actor, int) and 0 <= actor < len(self.active_riichi):
                self.active_riichi[actor] = True
            return
        if event_type in {"end_kyoku", "end_game", "ryukyoku", "hora"}:
            self.last_tsumo = None

    def hand_counts(self) -> tuple[int, ...]:
        return _counts_from_tokens(self.hand)

    def visible_counts(self) -> tuple[int, ...]:
        return _counts_from_tokens((*self.visible_tiles, *self.hand))

    def river_counts_by_seat(self) -> tuple[tuple[int, ...], ...]:
        return tuple(_counts_from_tokens(river) for river in self.rivers)

    def rivers_by_seat(self) -> tuple[tuple[Tile, ...], ...]:
        return tuple(tuple(_tiles_from_tokens(river)) for river in self.rivers)

    def discard_example(self, target: TileType) -> DiscardExample:
        return DiscardExample(
            round_index=0,
            event_index=0,
            seat=self.player_id,
            dealer=self.dealer,
            scores=self.scores,
            hand_counts=self.hand_counts(),
            visible_counts=self.visible_counts(),
            action=Action.discard(target),
            active_riichi_seats=tuple(self.active_riichi),
            river_counts_by_seat=self.river_counts_by_seat(),
            rivers_by_seat=self.rivers_by_seat(),
            riichi_declared_turns=(None, None, None, None),
            riichi_declared_event_indices=(None, None, None, None),
            meld_counts_by_seat=((0,) * 34, (0,) * 34, (0,) * 34, (0,) * 34),
            dora_indicators=tuple(_tiles_from_tokens(self.dora_markers)),
            last_discard_tsumogiri_by_seat=(None, None, None, None),
            ippatsu_active_seats=(False, False, False, False),
        )

    def _start_kyoku(self, event: dict[str, Any]) -> None:
        self.rivers = [[], [], [], []]
        self.active_riichi = [False, False, False, False]
        self.last_tsumo = None
        self.dealer = int(event.get("oya", 0))
        scores = event.get("scores")
        if isinstance(scores, list) and all(isinstance(score, int) for score in scores):
            self.scores = tuple(cast(list[int], scores))
        tehais = event.get("tehais")
        self.hand = []
        if isinstance(tehais, list) and self.player_id < len(tehais):
            hand = tehais[self.player_id]
            if isinstance(hand, list):
                self.hand = [tile for tile in hand if isinstance(tile, str) and tile != "?"]
        self.visible_tiles = []
        dora_marker = event.get("dora_marker")
        self.dora_markers = [dora_marker] if isinstance(dora_marker, str) else []
        self.visible_tiles.extend(self.dora_markers)

    def _tsumo(self, event: dict[str, Any]) -> None:
        actor = event.get("actor")
        pai = event.get("pai")
        if (
            isinstance(actor, int)
            and actor == self.player_id
            and isinstance(pai, str)
            and pai != "?"
        ):
            self.hand.append(pai)
            self.last_tsumo = pai

    def _dahai(self, event: dict[str, Any]) -> None:
        actor = event.get("actor")
        pai = event.get("pai")
        if not isinstance(actor, int) or not isinstance(pai, str):
            return
        if 0 <= actor < len(self.rivers) and pai != "?":
            self.rivers[actor].append(pai)
            self.visible_tiles.append(pai)
        if actor == self.player_id:
            _remove_tile(self.hand, pai)
            self.last_tsumo = None

    def _call(self, event: dict[str, Any]) -> None:
        actor = event.get("actor")
        if not isinstance(actor, int):
            return
        consumed = event.get("consumed")
        if isinstance(consumed, list):
            for token in consumed:
                if isinstance(token, str) and token != "?":
                    self.visible_tiles.append(token)
                    if actor == self.player_id:
                        _remove_tile(self.hand, token)
        if actor == self.player_id:
            self.last_tsumo = None


@dataclass(frozen=True, slots=True)
class FrequencyBotPolicy:
    model: DiscardFrequencyBaseline = DiscardFrequencyBaseline((0,) * 34)

    def choose_tile(self, state: BotState, legal_tiles: tuple[TileType, ...]) -> TileType:
        hand_counts = [0] * 34
        for tile in legal_tiles:
            hand_counts[tile.index] = max(1, state.hand_counts()[tile.index])
        return self.model.predict(tuple(hand_counts))


@dataclass(frozen=True, slots=True)
class LinearDiscardBotPolicy:
    model: DiscardLinearModel

    def choose_tile(self, state: BotState, legal_tiles: tuple[TileType, ...]) -> TileType:
        logits = self.model.logits(
            state.hand_counts(),
            state.visible_counts(),
            seat=state.player_id,
            active_riichi_seats=tuple(state.active_riichi),
            river_counts_by_seat=state.river_counts_by_seat(),
            rivers_by_seat=state.rivers_by_seat(),
            dora_indicators=tuple(_tiles_from_tokens(state.dora_markers)),
        )
        return max(legal_tiles, key=lambda tile: (logits.get(tile, float("-inf")), -tile.index))


@dataclass(frozen=True, slots=True)
class TorchDiscardBotPolicy:
    model: Any
    model_type: str
    device: str

    def choose_tile(self, state: BotState, legal_tiles: tuple[TileType, ...]) -> TileType:
        target = legal_tiles[0]
        example = state.discard_example(target)
        if self.model_type == "mlp":
            from kenjaku.models.torch_discard import predict_discard_tiles
        else:
            from kenjaku.models.torch_transformer import predict_discard_tiles

        predicted = predict_discard_tiles(
            self.model,
            [example],
            batch_size=1,
            device=self.device,
        )[0]
        return predicted if predicted in legal_tiles else target


@dataclass(slots=True)
class MjaiBot:
    state: BotState
    policy: MjaiDiscardPolicy

    def receive_line(self, line: str) -> list[dict[str, Any]]:
        payload = json.loads(line)
        if isinstance(payload, list):
            return [self._receive_event_batch(cast(list[Any], payload))]
        if not isinstance(payload, dict):
            raise ValueError("MJAI input line must be an object or event array")
        event = cast(dict[str, Any], payload)
        if event.get("type") == "request_action":
            return [self._request_action(event)]
        self.state.observe(event)
        return []

    def _receive_event_batch(self, events: list[Any]) -> dict[str, Any]:
        if not events:
            return {"type": "none"}
        parsed_events = [event for event in events if isinstance(event, dict)]
        for event in parsed_events:
            self.state.observe(cast(dict[str, Any], event))
        return self._legacy_action(cast(dict[str, Any], parsed_events[-1]) if parsed_events else {})

    def _request_action(self, request: dict[str, Any]) -> dict[str, Any]:
        possible_actions = request.get("possible_actions")
        if not isinstance(possible_actions, list):
            return self._with_request_id({"type": "none"}, request)
        actions = [dict(action) for action in possible_actions if isinstance(action, dict)]
        for action_type in ("hora", "tsumo", "ron", "ryukyoku", "kyushu"):
            action = _first_action(actions, action_type)
            if action is not None:
                return self._with_request_id(action, request)
        discard_actions = [action for action in actions if action.get("type") == "dahai"]
        if discard_actions:
            return self._with_request_id(self._choose_discard_action(discard_actions), request)
        reach = _first_action(actions, "reach")
        if reach is not None:
            return self._with_request_id(reach, request)
        none = _first_action(actions, "none")
        if none is not None:
            return self._with_request_id(none, request)
        return self._with_request_id(actions[0] if actions else {"type": "none"}, request)

    def _legacy_action(self, last_event: dict[str, Any]) -> dict[str, Any]:
        if last_event.get("type") != "tsumo" or last_event.get("actor") != self.state.player_id:
            return {"type": "none"}
        tile = self._choose_legacy_discard()
        return {
            "type": "dahai",
            "actor": self.state.player_id,
            "pai": tile,
            "tsumogiri": tile == self.state.last_tsumo,
        }

    def _choose_discard_action(self, actions: list[dict[str, Any]]) -> dict[str, Any]:
        legal_pairs = [
            (action, _tile_type_from_action(action))
            for action in actions
            if _tile_type_from_action(action) is not None
        ]
        if not legal_pairs:
            return actions[0]
        legal_tiles = tuple(
            dict.fromkeys(tile for _action, tile in legal_pairs if tile is not None)
        )
        chosen_tile = self.policy.choose_tile(self.state, legal_tiles)
        for action, tile in legal_pairs:
            if tile == chosen_tile and action.get("tsumogiri") is True:
                return action
        for action, tile in legal_pairs:
            if tile == chosen_tile:
                return action
        return legal_pairs[0][0]

    def _choose_legacy_discard(self) -> str:
        legal = tuple(dict.fromkeys(_tile_type_from_token(token) for token in self.state.hand))
        legal_tiles = tuple(tile for tile in legal if tile is not None)
        if not legal_tiles:
            return self.state.last_tsumo or "1m"
        chosen_tile = self.policy.choose_tile(self.state, legal_tiles)
        if (
            self.state.last_tsumo is not None
            and _tile_type_from_token(self.state.last_tsumo) == chosen_tile
        ):
            return self.state.last_tsumo
        for token in self.state.hand:
            if _tile_type_from_token(token) == chosen_tile:
                return token
        return self.state.hand[0]

    def _with_request_id(self, action: dict[str, Any], request: dict[str, Any]) -> dict[str, Any]:
        response = dict(action)
        if response.get("type") != "none":
            response.setdefault("actor", self.state.player_id)
        if "request_id" in request:
            response["request_id"] = request["request_id"]
        return response


def bot_main(
    argv: list[str] | None = None,
    *,
    stdin: TextIO | None = None,
    stdout: TextIO | None = None,
) -> int:
    parser = _build_bot_parser()
    args = parser.parse_args(argv)
    run_stdio_bot(
        policy_arg=args.policy,
        player_id=args.player_id,
        policy_type=args.policy_type,
        device=args.device,
        checkpoint_manifest=args.checkpoint_manifest,
        stdin=stdin or sys.stdin,
        stdout=stdout or sys.stdout,
    )
    return 0


def run_stdio_bot(
    *,
    policy_arg: str | Path,
    player_id: int,
    policy_type: str = "auto",
    device: str = "cpu",
    checkpoint_manifest: str | Path | None = None,
    stdin: TextIO | None = None,
    stdout: TextIO | None = None,
) -> None:
    if player_id not in range(4):
        raise ValueError("player-id must be 0, 1, 2, or 3")
    policy = load_mjai_policy(
        policy_arg,
        policy_type=policy_type,
        device=device,
        checkpoint_manifest=checkpoint_manifest,
    )
    bot = MjaiBot(BotState(player_id=player_id), policy)
    input_stream = stdin or sys.stdin
    output_stream = stdout or sys.stdout
    for raw_line in input_stream:
        line = raw_line.strip()
        if not line:
            continue
        for response in bot.receive_line(line):
            output_stream.write(json.dumps(response, separators=(",", ":"), ensure_ascii=False))
            output_stream.write("\n")
            output_stream.flush()


def load_mjai_policy(
    policy: str | Path,
    *,
    policy_type: str = "auto",
    device: str = "cpu",
    checkpoint_manifest: str | Path | None = None,
) -> MjaiDiscardPolicy:
    if policy_type not in BOT_POLICY_TYPES:
        raise ValueError(f"unsupported bot policy type: {policy_type}")
    policy_text = str(policy)
    normalized = _normalize_policy_type(policy_text) if policy_type == "auto" else policy_type
    if normalized == "frequency":
        if checkpoint_manifest is not None:
            raise ValueError("frequency policy does not accept checkpoint_manifest")
        return FrequencyBotPolicy()

    path = Path(policy)
    if not path.exists():
        raise ValueError(f"policy checkpoint not found: {path}")
    if normalized == "auto":
        normalized = _detect_policy_type(path, device=device)
    if checkpoint_manifest is not None:
        _load_bot_checkpoint_manifest(checkpoint_manifest, policy_type=normalized)
    if normalized == "linear-discard":
        return LinearDiscardBotPolicy(DiscardLinearModel.load(path))
    if normalized in {"mlp", "mlp-discard"}:
        from kenjaku.models.torch_discard import load_discard_mlp_checkpoint

        return TorchDiscardBotPolicy(
            model=load_discard_mlp_checkpoint(path, device=device),
            model_type="mlp",
            device=device,
        )
    if normalized in {"transformer", "transformer-discard"}:
        from kenjaku.models.torch_transformer import load_discard_transformer_checkpoint

        return TorchDiscardBotPolicy(
            model=load_discard_transformer_checkpoint(path, device=device),
            model_type="transformer",
            device=device,
        )
    raise ValueError(f"unsupported bot policy type: {normalized}")


def _build_bot_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="kenjaku bot")
    parser.add_argument(
        "--policy",
        default="frequency",
        help="policy name or checkpoint path; defaults to frequency",
    )
    parser.add_argument(
        "--policy-type",
        choices=sorted(BOT_POLICY_TYPES),
        default="auto",
        help="checkpoint family; auto detects paths",
    )
    parser.add_argument(
        "--player-id",
        type=int,
        required=True,
        help="MJAI seat id, 0 through 3",
    )
    parser.add_argument(
        "--device",
        default="cpu",
        help="PyTorch device for mlp or transformer policies",
    )
    parser.add_argument(
        "--checkpoint-manifest",
        type=Path,
        help="optional CheckpointManifestV1 JSON required to match the loaded checkpoint family",
    )
    return parser


def _normalize_policy_type(policy: str) -> str:
    aliases = {
        "frequency": "frequency",
        "freq": "frequency",
        "linear": "linear-discard",
        "linear-discard": "linear-discard",
        "mlp": "mlp",
        "mlp-discard": "mlp",
        "transformer": "transformer",
        "transformer-discard": "transformer",
    }
    return aliases.get(policy, "auto")


def _detect_policy_type(path: Path, *, device: str) -> str:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError):
        return _detect_torch_policy_type(path, device=device)
    if not isinstance(payload, dict):
        raise ValueError("policy JSON must contain an object")
    kind = payload.get("kind")
    model_kind = payload.get("model_kind")
    if isinstance(kind, str) and "discard-linear" in kind:
        return "linear-discard"
    if isinstance(model_kind, str) and "discard-linear" in model_kind:
        return "linear-discard"
    raise ValueError("unsupported JSON policy checkpoint")


def _detect_torch_policy_type(path: Path, *, device: str) -> str:
    try:
        from kenjaku.models.torch_discard import require_torch
    except ImportError as error:
        raise ValueError("PyTorch policy checkpoint requires kenjaku[ml]") from error
    torch = require_torch()
    payload = torch.load(path, map_location=torch.device(device))
    if not isinstance(payload, dict):
        raise ValueError("policy checkpoint must contain a dictionary")
    kind = payload.get("kind")
    if kind == "kenjaku-discard-mlp-checkpoint-v0":
        return "mlp"
    if kind == "kenjaku-discard-transformer-checkpoint-v0":
        return "transformer"
    raise ValueError(f"unsupported torch policy checkpoint kind: {kind!r}")


def _load_bot_checkpoint_manifest(
    path: str | Path,
    *,
    policy_type: str,
) -> CheckpointManifestV1:
    expected_model_kind = _BOT_CHECKPOINT_MODEL_KINDS.get(policy_type)
    if expected_model_kind is None:
        raise ValueError(f"unsupported checkpoint manifest policy type: {policy_type}")
    manifest_path = Path(path)
    try:
        payload = json.loads(manifest_path.read_text(encoding="utf-8"))
    except OSError as error:
        raise ValueError(f"checkpoint manifest cannot be read: {manifest_path}") from error
    except json.JSONDecodeError as error:
        raise ValueError(f"checkpoint manifest is invalid JSON: {manifest_path}") from error
    if not isinstance(payload, dict):
        raise ValueError("checkpoint manifest must be a JSON object")
    manifest = CheckpointManifestV1.from_dict(payload)
    manifest.require_compatible(
        CheckpointRequirementV1(
            ruleset="tenhou-4p",
            minimum_model_version=SemanticVersion(1, 0, 0),
            compatibility=CheckpointCompatibilityV1.current(),
            model_kind=expected_model_kind,
        )
    )
    return manifest


def _first_action(actions: list[dict[str, Any]], action_type: str) -> dict[str, Any] | None:
    for action in actions:
        if action.get("type") == action_type:
            return action
    return None


def _tile_type_from_action(action: dict[str, Any]) -> TileType | None:
    pai = action.get("pai")
    return _tile_type_from_token(pai) if isinstance(pai, str) else None


def _tile_type_from_token(token: str) -> TileType | None:
    if token == "?":
        return None
    return Tile.parse(token).type


def _tiles_from_tokens(tokens: list[str] | tuple[str, ...]) -> list[Tile]:
    return [tile for token in tokens if (tile := _tile_from_token(token)) is not None]


def _tile_from_token(token: str) -> Tile | None:
    if token == "?":
        return None
    return Tile.parse(token)


def _counts_from_tokens(tokens: tuple[str, ...] | list[str]) -> tuple[int, ...]:
    return tile_counts(_tiles_from_tokens(tokens))


def _remove_tile(hand: list[str], token: str) -> None:
    if token in hand:
        hand.remove(token)
        return
    tile_type = _tile_type_from_token(token)
    for candidate in list(hand):
        if _tile_type_from_token(candidate) == tile_type:
            hand.remove(candidate)
            return
