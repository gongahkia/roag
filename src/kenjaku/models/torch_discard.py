from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass

import torch
from torch import Tensor, nn
from torch.nn import functional as F
from torch.utils.data import DataLoader, Dataset

from kenjaku.core import TileType
from kenjaku.training import DiscardExample

DISCARD_MLP_INPUT_DIM = 68
DISCARD_MLP_OUTPUT_DIM = 34
DISCARD_MLP_MODEL_KIND = "discard-mlp-v0"
DISCARD_MLP_REPORT_KIND = "kenjaku-discard-mlp-report-v0"


class DiscardTensorDataset(Dataset):
    """Torch dataset for supervised discard decisions."""

    def __init__(self, examples: Sequence[DiscardExample]) -> None:
        self.examples = list(examples)

    def __len__(self) -> int:
        return len(self.examples)

    def __getitem__(self, index: int) -> tuple[Tensor, Tensor, Tensor]:
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

    def forward(self, state: Tensor, legal_mask: Tensor) -> Tensor:
        logits = self.net(state)
        return logits.masked_fill(~legal_mask, -1.0e9)


@dataclass(slots=True)
class DiscardMlpTrainingResult:
    model: DiscardMlp
    device: str
    train_metrics: dict[str, int | float | None]
    eval_metrics: dict[str, int | float | None]


def discard_data_loader(
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    shuffle: bool = False,
    seed: int = 0,
) -> DataLoader:
    dataset = DiscardTensorDataset(examples)
    generator = torch.Generator()
    generator.manual_seed(seed)
    return DataLoader(
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

    model = DiscardMlp(hidden_dim=hidden_dim).to(resolved_device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=learning_rate)

    train_loader = discard_data_loader(
        train_examples,
        batch_size=batch_size,
        shuffle=True,
        seed=seed,
    )
    for _ in range(epochs):
        model.train()
        for state, legal_mask, target in train_loader:
            state = state.to(resolved_device)
            legal_mask = legal_mask.to(resolved_device)
            target = target.to(resolved_device)
            optimizer.zero_grad(set_to_none=True)
            loss = F.cross_entropy(model(state, legal_mask), target)
            loss.backward()
            optimizer.step()

    return DiscardMlpTrainingResult(
        model=model,
        device=resolved_device.type,
        train_metrics=evaluate_discard_mlp(
            model,
            train_examples,
            batch_size=batch_size,
            device=resolved_device,
        ),
        eval_metrics=evaluate_discard_mlp(
            model,
            eval_examples,
            batch_size=batch_size,
            device=resolved_device,
        ),
    )


def evaluate_discard_mlp(
    model: DiscardMlp,
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    device: str | torch.device,
) -> dict[str, int | float | None]:
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
    model: DiscardMlp,
    examples: Sequence[DiscardExample],
    *,
    batch_size: int,
    device: str | torch.device,
) -> list[TileType]:
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


def resolve_torch_device(requested: str) -> torch.device:
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


def _state_tensor(example: DiscardExample) -> Tensor:
    values = [
        *(count / 4.0 for count in example.hand_counts),
        *(count / 4.0 for count in example.visible_counts),
    ]
    if len(values) != DISCARD_MLP_INPUT_DIM:
        raise ValueError(f"discard MLP state must have {DISCARD_MLP_INPUT_DIM} features")
    return torch.tensor(values, dtype=torch.float32)
