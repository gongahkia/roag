from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from functools import cache
from pathlib import Path
from typing import Any

import torch
from torch import Tensor, nn
from torch.nn import functional as F
from torch.utils.data import DataLoader, Dataset

from kenjaku.core import TileType
from kenjaku.models.torch_discard import resolve_torch_device
from kenjaku.training import DiscardExample

TRANSFORMER_TILE_TYPES = 34
TRANSFORMER_SEATS = 4
TRANSFORMER_NON_TILE_ID = TRANSFORMER_TILE_TYPES
TRANSFORMER_NON_SEAT_ID = TRANSFORMER_SEATS
TRANSFORMER_TOKEN_TYPE_COUNT = 8
TRANSFORMER_TOKEN_COUNT = TRANSFORMER_TILE_TYPES * 4 + TRANSFORMER_SEATS * 4
MAHJONG_TRANSFORMER_ENCODER_KIND = "mahjong-transformer-encoder-v0"
DISCARD_TRANSFORMER_POLICY_KIND = "discard-transformer-policy-v0"
DISCARD_TRANSFORMER_CHECKPOINT_KIND = "kenjaku-discard-transformer-checkpoint-v0"

TOKEN_HAND_COUNT = 0
TOKEN_VISIBLE_COUNT = 1
TOKEN_UNSEEN_COUNT = 2
TOKEN_DORA_INDICATOR_COUNT = 3
TOKEN_ACTIVE_RIICHI = 4
TOKEN_ACTING_SEAT = 5
TOKEN_DEALER = 6
TOKEN_SCORE = 7


@dataclass(frozen=True, slots=True)
class MahjongTransformerConfig:
    model_dim: int = 64
    num_heads: int = 4
    num_layers: int = 2
    feedforward_dim: int = 128
    dropout: float = 0.1


class MahjongStateTransformerEncoder(nn.Module):
    """Transformer encoder over fixed, player-perspective mahjong state tokens."""

    def __init__(self, config: MahjongTransformerConfig | None = None) -> None:
        super().__init__()
        self.kind = MAHJONG_TRANSFORMER_ENCODER_KIND
        self.config = config or MahjongTransformerConfig()
        if self.config.model_dim <= 0:
            raise ValueError("model_dim must be positive")
        if self.config.num_heads <= 0:
            raise ValueError("num_heads must be positive")
        if self.config.num_layers <= 0:
            raise ValueError("num_layers must be positive")
        if self.config.feedforward_dim <= 0:
            raise ValueError("feedforward_dim must be positive")
        if self.config.model_dim % self.config.num_heads != 0:
            raise ValueError("model_dim must be divisible by num_heads")
        if not 0.0 <= self.config.dropout < 1.0:
            raise ValueError("dropout must be in [0.0, 1.0)")

        token_type_ids, tile_ids, seat_ids = _token_metadata()
        self.register_buffer(
            "token_type_ids",
            torch.tensor(token_type_ids, dtype=torch.long),
            persistent=False,
        )
        self.register_buffer(
            "tile_ids",
            torch.tensor(tile_ids, dtype=torch.long),
            persistent=False,
        )
        self.register_buffer(
            "seat_ids",
            torch.tensor(seat_ids, dtype=torch.long),
            persistent=False,
        )
        self.value_projection = nn.Linear(1, self.config.model_dim)
        self.token_type_embedding = nn.Embedding(
            TRANSFORMER_TOKEN_TYPE_COUNT,
            self.config.model_dim,
        )
        self.tile_embedding = nn.Embedding(
            TRANSFORMER_TILE_TYPES + 1,
            self.config.model_dim,
        )
        self.seat_embedding = nn.Embedding(
            TRANSFORMER_SEATS + 1,
            self.config.model_dim,
        )
        self.position_embedding = nn.Embedding(
            TRANSFORMER_TOKEN_COUNT,
            self.config.model_dim,
        )
        layer = nn.TransformerEncoderLayer(
            d_model=self.config.model_dim,
            nhead=self.config.num_heads,
            dim_feedforward=self.config.feedforward_dim,
            dropout=self.config.dropout,
            activation="gelu",
            batch_first=True,
            norm_first=True,
        )
        self.encoder = nn.TransformerEncoder(layer, num_layers=self.config.num_layers)

    def forward(self, state_values: Tensor) -> Tensor:
        x, squeeze = self._embedded_tokens(state_values)
        encoded = self.encoder(x)
        return encoded.squeeze(0) if squeeze else encoded

    def pooled(self, state_values: Tensor) -> Tensor:
        encoded = self.forward(state_values)
        if encoded.ndim == 2:
            return encoded.mean(dim=0)
        return encoded.mean(dim=1)

    def attention_weights(self, state_values: Tensor) -> list[Tensor]:
        x, squeeze = self._embedded_tokens(state_values)
        weights: list[Tensor] = []
        for layer in self.encoder.layers:
            if layer.norm_first:
                attention_input = layer.norm1(x)
                attention_output, layer_weights = layer.self_attn(
                    attention_input,
                    attention_input,
                    attention_input,
                    need_weights=True,
                    average_attn_weights=False,
                )
                x = x + layer.dropout1(attention_output)
                x = x + layer._ff_block(layer.norm2(x))
            else:
                attention_output, layer_weights = layer.self_attn(
                    x,
                    x,
                    x,
                    need_weights=True,
                    average_attn_weights=False,
                )
                x = layer.norm1(x + layer.dropout1(attention_output))
                x = layer.norm2(x + layer._ff_block(x))
            weights.append(layer_weights.squeeze(0) if squeeze else layer_weights)
        return weights

    def _embedded_tokens(self, state_values: Tensor) -> tuple[Tensor, bool]:
        state_values, squeeze = _batched_state_values(state_values)
        batch_size = state_values.shape[0]
        positions = torch.arange(
            TRANSFORMER_TOKEN_COUNT,
            device=state_values.device,
            dtype=torch.long,
        )
        token_type_ids = self.token_type_ids.to(state_values.device).expand(batch_size, -1)
        tile_ids = self.tile_ids.to(state_values.device).expand(batch_size, -1)
        seat_ids = self.seat_ids.to(state_values.device).expand(batch_size, -1)
        return (
            self.value_projection(state_values.unsqueeze(-1))
            + self.token_type_embedding(token_type_ids)
            + self.tile_embedding(tile_ids)
            + self.seat_embedding(seat_ids)
            + self.position_embedding(positions).unsqueeze(0)
        ), squeeze


class DiscardTransformerPolicy(nn.Module):
    """Masked-logit discard policy head over the mahjong transformer encoder."""

    def __init__(
        self,
        config: MahjongTransformerConfig | None = None,
        *,
        value_head: bool = False,
    ) -> None:
        super().__init__()
        self.kind = DISCARD_TRANSFORMER_POLICY_KIND
        self.encoder = MahjongStateTransformerEncoder(config)
        self.output_dim = TRANSFORMER_TILE_TYPES
        self.input_tokens = TRANSFORMER_TOKEN_COUNT
        self.has_value_head = value_head
        self.policy_head = nn.Linear(self.encoder.config.model_dim, self.output_dim)
        self.value_head = (
            nn.Linear(self.encoder.config.model_dim, 1)
            if self.has_value_head
            else None
        )

    def forward(self, state_values: Tensor, legal_mask: Tensor) -> Tensor | tuple[Tensor, Tensor]:
        pooled = self.encoder.pooled(state_values)
        if pooled.ndim == 1:
            pooled = pooled.unsqueeze(0)
        legal_mask = _batched_legal_mask(legal_mask, batch_size=pooled.shape[0])
        logits = self.policy_head(pooled)
        masked_logits = logits.masked_fill(~legal_mask.to(logits.device), -1.0e9)
        if self.value_head is None:
            return masked_logits
        return masked_logits, self.value_head(pooled).squeeze(-1)

    def attention_weights(self, state_values: Tensor) -> list[Tensor]:
        return self.encoder.attention_weights(state_values)


class DiscardTransformerTensorDataset(Dataset):
    """Torch dataset for transformer discard-policy experiments."""

    def __init__(self, examples: Sequence[DiscardExample]) -> None:
        self.examples = list(examples)

    def __len__(self) -> int:
        return len(self.examples)

    def __getitem__(self, index: int) -> tuple[Tensor, Tensor, Tensor]:
        example = self.examples[index]
        target = example.action.tile
        if target is None:
            raise ValueError("discard examples must have a target tile")
        return (
            transformer_state_tensor(example),
            transformer_legal_mask(example),
            torch.tensor(target.index, dtype=torch.long),
        )


@dataclass(slots=True)
class DiscardTransformerTrainingResult:
    model: DiscardTransformerPolicy
    device: str
    train_metrics: dict[str, int | float | None]
    eval_metrics: dict[str, int | float | None]
    history: list[dict[str, Any]]
    best_epoch: int
    selection_split: str
    best_metrics: dict[str, dict[str, int | float | None]]
    best_model_state: dict[str, Tensor]


def discard_transformer_data_loader(
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    shuffle: bool = False,
    seed: int = 0,
) -> DataLoader:
    dataset = DiscardTransformerTensorDataset(examples)
    generator = torch.Generator()
    generator.manual_seed(seed)
    return DataLoader(
        dataset,
        batch_size=batch_size,
        shuffle=shuffle,
        generator=generator,
    )


def train_discard_transformer(
    train_examples: Sequence[DiscardExample],
    eval_examples: Sequence[DiscardExample],
    *,
    config: MahjongTransformerConfig,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    device: str = "auto",
    seed: int = 0,
    value_head: bool = False,
) -> DiscardTransformerTrainingResult:
    if not train_examples:
        raise ValueError("no train examples found")
    if epochs < 0:
        raise ValueError("epochs must be non-negative")
    if batch_size <= 0:
        raise ValueError("batch_size must be positive")
    if learning_rate <= 0:
        raise ValueError("learning_rate must be positive")

    resolved_device = resolve_torch_device(device)
    torch.manual_seed(seed)
    if resolved_device.type == "cuda":
        torch.cuda.manual_seed_all(seed)

    model = DiscardTransformerPolicy(config, value_head=value_head).to(resolved_device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate)
    train_loader = discard_transformer_data_loader(
        train_examples,
        batch_size=batch_size,
        shuffle=True,
        seed=seed,
    )
    history: list[dict[str, Any]] = []
    selection_split = "eval" if eval_examples else "train"
    best_key: tuple[float, float, int] | None = None
    best_epoch = 0
    best_metrics: dict[str, dict[str, int | float | None]] = {}
    best_model_state: dict[str, Tensor] = {}
    final_train_metrics: dict[str, int | float | None] | None = None
    final_eval_metrics: dict[str, int | float | None] | None = None

    def record_epoch(epoch: int) -> None:
        nonlocal best_epoch
        nonlocal best_key
        nonlocal best_metrics
        nonlocal best_model_state
        nonlocal final_train_metrics
        nonlocal final_eval_metrics

        train_metrics = evaluate_discard_transformer(
            model,
            train_examples,
            batch_size=batch_size,
            device=resolved_device,
        )
        eval_metrics = evaluate_discard_transformer(
            model,
            eval_examples,
            batch_size=batch_size,
            device=resolved_device,
        )
        row = {
            "epoch": epoch,
            "metrics": {
                "train": train_metrics,
                "eval": eval_metrics,
            },
        }
        history.append(row)
        final_train_metrics = train_metrics
        final_eval_metrics = eval_metrics

        key = _selection_key(row, split=selection_split)
        if best_key is None or key > best_key:
            best_key = key
            best_epoch = epoch
            best_metrics = {
                "train": dict(train_metrics),
                "eval": dict(eval_metrics),
            }
            best_model_state = _snapshot_model_state(model)

    if epochs == 0:
        record_epoch(0)
    for epoch in range(1, epochs + 1):
        model.train()
        for state, legal_mask, target in train_loader:
            state = state.to(resolved_device)
            legal_mask = legal_mask.to(resolved_device)
            target = target.to(resolved_device)
            optimizer.zero_grad(set_to_none=True)
            loss = F.cross_entropy(_policy_logits(model(state, legal_mask)), target)
            loss.backward()
            optimizer.step()
        record_epoch(epoch)

    if final_train_metrics is None or final_eval_metrics is None:
        raise RuntimeError("discard transformer training did not record final metrics")

    return DiscardTransformerTrainingResult(
        model=model,
        device=resolved_device.type,
        train_metrics=final_train_metrics,
        eval_metrics=final_eval_metrics,
        history=history,
        best_epoch=best_epoch,
        selection_split=selection_split,
        best_metrics=best_metrics,
        best_model_state=best_model_state,
    )


def save_discard_transformer_checkpoint(
    result: DiscardTransformerTrainingResult,
    path: str | Path,
    *,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    eval_fraction: float,
    split_seed: str,
    seed: int,
) -> None:
    checkpoint_path = Path(path)
    checkpoint_path.parent.mkdir(parents=True, exist_ok=True)
    torch.save(
        {
            "kind": DISCARD_TRANSFORMER_CHECKPOINT_KIND,
            "model": {
                "kind": result.model.kind,
                "encoder_kind": result.model.encoder.kind,
                "input_tokens": result.model.input_tokens,
                "output_dim": result.model.output_dim,
                "value_head": result.model.has_value_head,
                "config": transformer_config_payload(result.model.encoder.config),
            },
            "training": {
                "epochs": epochs,
                "batch_size": batch_size,
                "learning_rate": learning_rate,
                "device": result.device,
                "seed": seed,
                "eval_fraction": eval_fraction,
                "split_seed": split_seed,
                "history": result.history,
                "best_epoch": result.best_epoch,
                "selection_split": result.selection_split,
            },
            "metrics": {
                "train": result.train_metrics,
                "eval": result.eval_metrics,
                "best": result.best_metrics,
            },
            "model_state_dict": result.best_model_state,
        },
        checkpoint_path,
    )


def load_discard_transformer_checkpoint(
    path: str | Path,
    *,
    device: str | torch.device = "cpu",
) -> DiscardTransformerPolicy:
    checkpoint_path = Path(path)
    resolved_device = torch.device(device)
    payload = torch.load(checkpoint_path, map_location=resolved_device)
    if not isinstance(payload, dict) or payload.get("kind") != DISCARD_TRANSFORMER_CHECKPOINT_KIND:
        raise ValueError("not a discard transformer checkpoint")
    model_payload = payload.get("model")
    if not isinstance(model_payload, dict):
        raise ValueError("checkpoint missing model metadata")
    config_payload = model_payload.get("config")
    if not isinstance(config_payload, dict):
        raise ValueError("checkpoint missing transformer config")
    model = DiscardTransformerPolicy(
        MahjongTransformerConfig(
            model_dim=int(config_payload["model_dim"]),
            num_heads=int(config_payload["num_heads"]),
            num_layers=int(config_payload["num_layers"]),
            feedforward_dim=int(config_payload["feedforward_dim"]),
            dropout=float(config_payload["dropout"]),
        ),
        value_head=bool(model_payload.get("value_head", False)),
    ).to(resolved_device)
    state_dict = payload.get("model_state_dict")
    if not isinstance(state_dict, dict):
        raise ValueError("checkpoint missing model_state_dict")
    model.load_state_dict(state_dict)
    model.eval()
    return model


def evaluate_discard_transformer(
    model: DiscardTransformerPolicy,
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    device: str | torch.device,
) -> dict[str, int | float | None]:
    if not examples:
        return {"examples": 0, "loss": None, "accuracy": None}
    resolved_device = torch.device(device)
    loader = discard_transformer_data_loader(examples, batch_size=batch_size)
    model.eval()
    total_loss = 0.0
    total_correct = 0
    total_examples = 0
    with torch.no_grad():
        for state, legal_mask, target in loader:
            state = state.to(resolved_device)
            legal_mask = legal_mask.to(resolved_device)
            target = target.to(resolved_device)
            logits = _policy_logits(model(state, legal_mask))
            loss = F.cross_entropy(logits, target, reduction="sum")
            total_loss += float(loss.detach().cpu())
            total_correct += int((logits.argmax(dim=1) == target).sum().detach().cpu())
            total_examples += int(target.numel())
    return {
        "examples": total_examples,
        "loss": total_loss / total_examples,
        "accuracy": total_correct / total_examples,
    }


def predict_discard_tiles(
    model: DiscardTransformerPolicy,
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    device: str | torch.device,
) -> list[TileType]:
    if not examples:
        return []
    resolved_device = torch.device(device)
    loader = discard_transformer_data_loader(examples, batch_size=batch_size)
    predictions: list[TileType] = []
    model.eval()
    with torch.no_grad():
        for state, legal_mask, _target in loader:
            output = model(state.to(resolved_device), legal_mask.to(resolved_device))
            logits = _policy_logits(output)
            predictions.extend(TileType(int(index)) for index in logits.argmax(dim=1).cpu())
    return predictions


def transformer_config_payload(config: MahjongTransformerConfig) -> dict[str, int | float]:
    return {
        "model_dim": config.model_dim,
        "num_heads": config.num_heads,
        "num_layers": config.num_layers,
        "feedforward_dim": config.feedforward_dim,
        "dropout": config.dropout,
    }


def transformer_state_tensor(example: DiscardExample) -> Tensor:
    values = _state_values(example)
    if len(values) != TRANSFORMER_TOKEN_COUNT:
        raise ValueError(f"transformer state must have {TRANSFORMER_TOKEN_COUNT} tokens")
    return torch.tensor(values, dtype=torch.float32)


def transformer_legal_mask(example: DiscardExample) -> Tensor:
    return torch.tensor([count > 0 for count in example.hand_counts], dtype=torch.bool)


def transformer_state_payload(example: DiscardExample) -> dict[str, Any]:
    token_type_ids, tile_ids, seat_ids = _token_metadata()
    values = _state_values(example)
    return {
        "kind": "kenjaku-transformer-state-v0",
        "token_count": TRANSFORMER_TOKEN_COUNT,
        "tokens": [
            {
                "index": index,
                "token_type": _token_type_name(token_type),
                "tile": None if tile_id == TRANSFORMER_NON_TILE_ID else TileType(tile_id).notation,
                "seat": None if seat_id == TRANSFORMER_NON_SEAT_ID else seat_id,
                "value": values[index],
            }
            for index, (token_type, tile_id, seat_id) in enumerate(
                zip(token_type_ids, tile_ids, seat_ids, strict=True)
            )
        ],
    }


def _state_values(example: DiscardExample) -> tuple[float, ...]:
    dora_counts = [0] * TRANSFORMER_TILE_TYPES
    for tile in example.dora_indicators:
        dora_counts[tile.type.index] += 1
    values: list[float] = []
    values.extend(count / 4.0 for count in example.hand_counts)
    values.extend(count / 4.0 for count in example.visible_counts)
    values.extend(max(0, 4 - count) / 4.0 for count in example.visible_counts)
    values.extend(count / 4.0 for count in dora_counts)
    for seat in range(TRANSFORMER_SEATS):
        values.append(
            1.0
            if seat < len(example.active_riichi_seats) and example.active_riichi_seats[seat]
            else 0.0
        )
    for seat in range(TRANSFORMER_SEATS):
        values.append(1.0 if seat == example.seat else 0.0)
    for seat in range(TRANSFORMER_SEATS):
        values.append(1.0 if seat == example.dealer else 0.0)
    for seat in range(TRANSFORMER_SEATS):
        score = example.scores[seat] if seat < len(example.scores) else 25000
        values.append(score / 50000.0)
    return tuple(values)


@cache
def _token_metadata() -> tuple[tuple[int, ...], tuple[int, ...], tuple[int, ...]]:
    token_types: list[int] = []
    tile_ids: list[int] = []
    seat_ids: list[int] = []

    def add_tile_block(token_type: int) -> None:
        for tile_id in range(TRANSFORMER_TILE_TYPES):
            token_types.append(token_type)
            tile_ids.append(tile_id)
            seat_ids.append(TRANSFORMER_NON_SEAT_ID)

    add_tile_block(TOKEN_HAND_COUNT)
    add_tile_block(TOKEN_VISIBLE_COUNT)
    add_tile_block(TOKEN_UNSEEN_COUNT)
    add_tile_block(TOKEN_DORA_INDICATOR_COUNT)

    for token_type in (TOKEN_ACTIVE_RIICHI, TOKEN_ACTING_SEAT, TOKEN_DEALER, TOKEN_SCORE):
        for seat_id in range(TRANSFORMER_SEATS):
            token_types.append(token_type)
            tile_ids.append(TRANSFORMER_NON_TILE_ID)
            seat_ids.append(seat_id)

    if len(token_types) != TRANSFORMER_TOKEN_COUNT:
        raise ValueError("transformer token metadata drifted from token count")
    return tuple(token_types), tuple(tile_ids), tuple(seat_ids)


def _batched_state_values(state_values: Tensor) -> tuple[Tensor, bool]:
    if state_values.ndim == 1:
        if state_values.shape[0] != TRANSFORMER_TOKEN_COUNT:
            raise ValueError(f"state values must have {TRANSFORMER_TOKEN_COUNT} tokens")
        return state_values.unsqueeze(0), True
    if state_values.ndim == 2 and state_values.shape[1] == TRANSFORMER_TOKEN_COUNT:
        return state_values, False
    raise ValueError(f"state values must have shape ({TRANSFORMER_TOKEN_COUNT},) or batch-first")


def _batched_legal_mask(legal_mask: Tensor, *, batch_size: int) -> Tensor:
    if legal_mask.ndim == 1:
        if legal_mask.shape[0] != TRANSFORMER_TILE_TYPES:
            raise ValueError(f"legal mask must have {TRANSFORMER_TILE_TYPES} entries")
        return legal_mask.unsqueeze(0).expand(batch_size, -1)
    if legal_mask.ndim == 2 and tuple(legal_mask.shape) == (
        batch_size,
        TRANSFORMER_TILE_TYPES,
    ):
        return legal_mask
    raise ValueError(f"legal mask must have shape ({TRANSFORMER_TILE_TYPES},) or batch-first")


def _selection_key(row: dict[str, Any], *, split: str) -> tuple[float, float, int]:
    metrics = row["metrics"][split]
    accuracy = metrics["accuracy"]
    loss = metrics["loss"]
    accuracy_key = float(accuracy) if accuracy is not None else float("-inf")
    loss_key = -float(loss) if loss is not None else float("-inf")
    return (accuracy_key, loss_key, -int(row["epoch"]))


def _policy_logits(output: Tensor | tuple[Tensor, Tensor]) -> Tensor:
    if isinstance(output, tuple):
        return output[0]
    return output


def _snapshot_model_state(model: DiscardTransformerPolicy) -> dict[str, Tensor]:
    return {
        name: value.detach().cpu().clone()
        for name, value in model.state_dict().items()
    }


def _token_type_name(token_type: int) -> str:
    names = {
        TOKEN_HAND_COUNT: "hand_count",
        TOKEN_VISIBLE_COUNT: "visible_count",
        TOKEN_UNSEEN_COUNT: "unseen_count",
        TOKEN_DORA_INDICATOR_COUNT: "dora_indicator_count",
        TOKEN_ACTIVE_RIICHI: "active_riichi",
        TOKEN_ACTING_SEAT: "acting_seat",
        TOKEN_DEALER: "dealer",
        TOKEN_SCORE: "score",
    }
    return names[token_type]
