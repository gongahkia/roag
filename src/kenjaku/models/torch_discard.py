from __future__ import annotations

import importlib
from collections.abc import Sequence
from dataclasses import dataclass
from functools import cache
from pathlib import Path
from typing import Any

from kenjaku.core import TileType
from kenjaku.training import DiscardExample

DISCARD_MLP_INPUT_DIM = 68
DISCARD_MLP_OUTPUT_DIM = 34
DISCARD_MLP_MODEL_KIND = "discard-mlp-v0"
DISCARD_MLP_REPORT_KIND = "kenjaku-discard-mlp-report-v0"
DISCARD_MLP_CHECKPOINT_KIND = "kenjaku-discard-mlp-checkpoint-v0"
TORCH_EXTRA_HINT = "install with `pip install kenjaku[ml]`"
TORCH_REQUIRED_MESSAGE = f"PyTorch is required; {TORCH_EXTRA_HINT}"


def require_torch() -> Any:
    return _require_torch_modules()[0]


def _require_torch_modules() -> tuple[Any, Any, Any, Any, Any]:
    try:
        torch = importlib.import_module("torch")
        nn = importlib.import_module("torch.nn")
        functional = importlib.import_module("torch.nn.functional")
        data = importlib.import_module("torch.utils.data")
    except ImportError as error:
        raise ImportError(TORCH_REQUIRED_MESSAGE) from error
    return torch, nn, functional, data.DataLoader, data.Dataset


@cache
def _torch_discard_classes() -> tuple[Any, Any]:
    torch, nn, _functional, _data_loader, dataset_base = _require_torch_modules()

    class DiscardTensorDataset(dataset_base):
        """Torch dataset for supervised discard decisions."""

        __module__ = __name__

        def __init__(self, examples: Sequence[DiscardExample]) -> None:
            self.examples = list(examples)

        def __len__(self) -> int:
            return len(self.examples)

        def __getitem__(self, index: int) -> tuple[Any, Any, Any]:
            example = self.examples[index]
            target = example.action.tile
            if target is None:
                raise ValueError("discard examples must have a target tile")
            state = _state_tensor(example)
            legal_mask = torch.tensor(
                [count > 0 for count in example.hand_counts],
                dtype=torch.bool,
            )
            target_index = torch.tensor(target.index, dtype=torch.long)
            return state, legal_mask, target_index

    class DiscardMlp(nn.Module):
        """Small masked-logit MLP for discard tile classification."""

        __module__ = __name__

        def __init__(
            self,
            *,
            input_dim: int = DISCARD_MLP_INPUT_DIM,
            hidden_dim: int = 128,
        ) -> None:
            super().__init__()
            self.kind = DISCARD_MLP_MODEL_KIND
            self.input_dim = input_dim
            self.hidden_dim = hidden_dim
            self.output_dim = DISCARD_MLP_OUTPUT_DIM
            self.net = nn.Sequential(
                nn.Linear(input_dim, hidden_dim),
                nn.ReLU(),
                nn.Linear(hidden_dim, DISCARD_MLP_OUTPUT_DIM),
            )

        def forward(self, state: Any, legal_mask: Any) -> Any:
            logits = self.net(state)
            return logits.masked_fill(~legal_mask, -1.0e9)

    globals()["DiscardTensorDataset"] = DiscardTensorDataset
    globals()["DiscardMlp"] = DiscardMlp
    return DiscardMlp, DiscardTensorDataset


def __getattr__(name: str) -> Any:
    if name in {"DiscardMlp", "DiscardTensorDataset"}:
        discard_mlp, dataset = _torch_discard_classes()
        return discard_mlp if name == "DiscardMlp" else dataset
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")


@dataclass(slots=True)
class DiscardMlpTrainingResult:
    model: Any
    device: str
    train_metrics: dict[str, int | float | None]
    eval_metrics: dict[str, int | float | None]
    history: list[dict[str, Any]]
    best_epoch: int
    selection_split: str
    best_metrics: dict[str, dict[str, int | float | None]]
    best_model_state: dict[str, Any]


def discard_data_loader(
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    shuffle: bool = False,
    seed: int = 0,
) -> Any:
    torch, _nn, _functional, data_loader, _dataset_base = _require_torch_modules()
    _discard_mlp, dataset_class = _torch_discard_classes()
    dataset = dataset_class(examples)
    generator = torch.Generator()
    generator.manual_seed(seed)
    return data_loader(
        dataset,
        batch_size=batch_size,
        shuffle=shuffle,
        generator=generator,
    )


def train_discard_mlp(
    train_examples: Sequence[DiscardExample],
    eval_examples: Sequence[DiscardExample],
    *,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    hidden_dim: int,
    device: str = "auto",
    seed: int = 0,
) -> DiscardMlpTrainingResult:
    torch, _nn, functional, _data_loader, _dataset_base = _require_torch_modules()
    discard_mlp_class, _dataset_class = _torch_discard_classes()
    if not train_examples:
        raise ValueError("no train examples found")
    if epochs < 0:
        raise ValueError("epochs must be non-negative")
    if batch_size <= 0:
        raise ValueError("batch_size must be positive")
    if learning_rate <= 0:
        raise ValueError("learning_rate must be positive")
    if hidden_dim <= 0:
        raise ValueError("hidden_dim must be positive")

    resolved_device = resolve_torch_device(device)
    torch.manual_seed(seed)
    if resolved_device.type == "cuda":
        torch.cuda.manual_seed_all(seed)

    model = discard_mlp_class(hidden_dim=hidden_dim).to(resolved_device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate)

    train_loader = discard_data_loader(
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
    best_model_state: dict[str, Any] = {}
    final_train_metrics: dict[str, int | float | None] | None = None
    final_eval_metrics: dict[str, int | float | None] | None = None

    def record_epoch(epoch: int) -> None:
        nonlocal best_epoch
        nonlocal best_key
        nonlocal best_metrics
        nonlocal best_model_state
        nonlocal final_train_metrics
        nonlocal final_eval_metrics

        train_metrics = evaluate_discard_mlp(
            model,
            train_examples,
            batch_size=batch_size,
            device=resolved_device,
        )
        eval_metrics = evaluate_discard_mlp(
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
            loss = functional.cross_entropy(model(state, legal_mask), target)
            loss.backward()
            optimizer.step()
        record_epoch(epoch)

    if final_train_metrics is None or final_eval_metrics is None:
        raise RuntimeError("discard MLP training did not record final metrics")

    return DiscardMlpTrainingResult(
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


def save_discard_mlp_checkpoint(
    result: DiscardMlpTrainingResult,
    path: str | Path,
    *,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    eval_fraction: float,
    split_seed: str,
    seed: int,
) -> None:
    torch = require_torch()
    checkpoint_path = Path(path)
    checkpoint_path.parent.mkdir(parents=True, exist_ok=True)
    torch.save(
        {
            "kind": DISCARD_MLP_CHECKPOINT_KIND,
            "model": {
                "kind": result.model.kind,
                "input_dim": result.model.input_dim,
                "hidden_dim": result.model.hidden_dim,
                "output_dim": result.model.output_dim,
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


def load_discard_mlp_checkpoint(
    path: str | Path,
    *,
    device: Any = "cpu",
) -> Any:
    torch = require_torch()
    discard_mlp_class, _dataset_class = _torch_discard_classes()
    checkpoint_path = Path(path)
    resolved_device = torch.device(device)
    try:
        payload = torch.load(checkpoint_path, map_location=resolved_device, weights_only=True)
    except TypeError:
        payload = torch.load(checkpoint_path, map_location=resolved_device)
    if not isinstance(payload, dict) or payload.get("kind") != DISCARD_MLP_CHECKPOINT_KIND:
        raise ValueError("not a discard MLP checkpoint")
    model_payload = payload.get("model")
    if not isinstance(model_payload, dict):
        raise ValueError("checkpoint missing model metadata")
    model = discard_mlp_class(
        input_dim=int(model_payload.get("input_dim", DISCARD_MLP_INPUT_DIM)),
        hidden_dim=int(model_payload["hidden_dim"]),
    ).to(resolved_device)
    state_dict = payload.get("model_state_dict")
    if not isinstance(state_dict, dict):
        raise ValueError("checkpoint missing model_state_dict")
    model.load_state_dict(state_dict)
    model.eval()
    return model


def evaluate_discard_mlp(
    model: Any,
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    device: Any,
) -> dict[str, int | float | None]:
    torch, _nn, functional, _data_loader, _dataset_base = _require_torch_modules()
    if not examples:
        return {"examples": 0, "loss": None, "accuracy": None}
    resolved_device = torch.device(device)
    loader = discard_data_loader(examples, batch_size=batch_size)
    model.eval()
    total_loss = 0.0
    total_correct = 0
    total_examples = 0
    with torch.no_grad():
        for state, legal_mask, target in loader:
            state = state.to(resolved_device)
            legal_mask = legal_mask.to(resolved_device)
            target = target.to(resolved_device)
            logits = model(state, legal_mask)
            loss = functional.cross_entropy(logits, target, reduction="sum")
            total_loss += float(loss.detach().cpu())
            total_correct += int((logits.argmax(dim=1) == target).sum().detach().cpu())
            total_examples += int(target.numel())
    return {
        "examples": total_examples,
        "loss": total_loss / total_examples,
        "accuracy": total_correct / total_examples,
    }


def predict_discard_tiles(
    model: Any,
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    device: Any,
) -> list[TileType]:
    torch = require_torch()
    if not examples:
        return []
    resolved_device = torch.device(device)
    loader = discard_data_loader(examples, batch_size=batch_size)
    predictions: list[TileType] = []
    model.eval()
    with torch.no_grad():
        for state, legal_mask, _target in loader:
            logits = model(state.to(resolved_device), legal_mask.to(resolved_device))
            predictions.extend(TileType(int(index)) for index in logits.argmax(dim=1).cpu())
    return predictions


def resolve_torch_device(requested: str) -> Any:
    torch = require_torch()
    if requested == "auto":
        if torch.cuda.is_available():
            return torch.device("cuda")
        mps_backend = getattr(torch.backends, "mps", None)
        if mps_backend is not None and mps_backend.is_available():
            return torch.device("mps")
        return torch.device("cpu")
    if requested == "cuda":
        if not torch.cuda.is_available():
            raise ValueError("CUDA is not available")
        return torch.device("cuda")
    if requested == "mps":
        mps_backend = getattr(torch.backends, "mps", None)
        if mps_backend is None or not mps_backend.is_available():
            raise ValueError("MPS is not available")
        return torch.device("mps")
    if requested == "cpu":
        return torch.device("cpu")
    raise ValueError(f"unsupported torch device: {requested}")


def _selection_key(row: dict[str, Any], *, split: str) -> tuple[float, float, int]:
    metrics = row["metrics"][split]
    accuracy = metrics["accuracy"]
    loss = metrics["loss"]
    accuracy_key = float(accuracy) if accuracy is not None else float("-inf")
    loss_key = -float(loss) if loss is not None else float("-inf")
    return (accuracy_key, loss_key, -int(row["epoch"]))


def _snapshot_model_state(model: Any) -> dict[str, Any]:
    return {
        name: value.detach().cpu().clone()
        for name, value in model.state_dict().items()
    }


def _state_tensor(example: DiscardExample) -> Any:
    torch = require_torch()
    values = [
        *(count / 4.0 for count in example.hand_counts),
        *(count / 4.0 for count in example.visible_counts),
    ]
    if len(values) != DISCARD_MLP_INPUT_DIM:
        raise ValueError(f"discard MLP state must have {DISCARD_MLP_INPUT_DIM} features")
    return torch.tensor(values, dtype=torch.float32)
