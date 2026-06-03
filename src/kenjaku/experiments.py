from __future__ import annotations

import json
from collections.abc import Sequence
from pathlib import Path
from typing import Any

from kenjaku.io import TenhouGame

DISCARD_LINEAR_REPORT_KIND = "kenjaku-discard-linear-report-v0"


def build_discard_linear_report(
    *,
    input_paths: Sequence[Path],
    xml_files: Sequence[Path],
    game: TenhouGame,
    discard_examples: int,
    call_examples: int,
    split_seed: str,
    eval_fraction: float,
    train_examples: int,
    eval_examples: int,
    epochs: int,
    learning_rate: float,
    train_accuracy: float,
    eval_accuracy: float | None,
    model_path: Path | None,
) -> dict[str, Any]:
    return {
        "kind": DISCARD_LINEAR_REPORT_KIND,
        "input_paths": [str(path) for path in input_paths],
        "xml_file_count": len(xml_files),
        "rounds": len(game.rounds),
        "discards": sum(len(round_.discards) for round_ in game.rounds),
        "discard_examples": discard_examples,
        "call_examples": call_examples,
        "split": {
            "seed": split_seed,
            "eval_fraction": eval_fraction,
            "train_examples": train_examples,
            "eval_examples": eval_examples,
        },
        "training": {
            "epochs": epochs,
            "learning_rate": learning_rate,
        },
        "metrics": {
            "train_accuracy": train_accuracy,
            "eval_accuracy": eval_accuracy,
        },
        "artifacts": {
            "model_path": None if model_path is None else str(model_path),
        },
    }


def write_json_report(path: str | Path, payload: dict[str, Any]) -> None:
    report_path = Path(path)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(
        json.dumps(payload, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
