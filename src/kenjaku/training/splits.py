from __future__ import annotations

from collections.abc import Sequence
from hashlib import blake2b
from typing import TypeVar

T = TypeVar("T")


def deterministic_split(
    items: Sequence[T],
    *,
    eval_fraction: float = 0.2,
    seed: str = "kenjaku-v0",
) -> tuple[list[T], list[T]]:
    """Split a sequence into train/eval subsets using stable content-free hashing."""

    if not 0 <= eval_fraction < 1:
        raise ValueError("eval_fraction must be in the range [0, 1)")
    if not items:
        return [], []

    eval_count = round(len(items) * eval_fraction)
    if eval_fraction > 0 and len(items) > 1:
        eval_count = max(1, eval_count)
    eval_count = min(eval_count, len(items) - 1)

    ranked_indices = sorted(
        range(len(items)),
        key=lambda index: _stable_digest(f"{seed}:{index}"),
    )
    eval_indices = set(ranked_indices[:eval_count])
    train = [item for index, item in enumerate(items) if index not in eval_indices]
    evaluation = [item for index, item in enumerate(items) if index in eval_indices]
    return train, evaluation


def _stable_digest(text: str) -> int:
    digest = blake2b(text.encode("utf-8"), digest_size=8).digest()
    return int.from_bytes(digest, byteorder="big")
