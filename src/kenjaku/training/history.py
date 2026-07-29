from __future__ import annotations

from collections.abc import Mapping, Sequence
from typing import Any

TRAINING_HISTORY_KIND = "kenjaku-training-history-v0"


def normalize_training_history(
    rows: Sequence[Mapping[str, Any]],
    *,
    step_key: str,
    step_unit: str,
    metric_roots: Sequence[str],
) -> dict[str, Any]:
    records: list[dict[str, Any]] = []
    curves: dict[str, list[dict[str, int | float]]] = {}
    for row in rows:
        step = int(row[step_key])
        metrics = _record_metrics(row, metric_roots)
        records.append({"step": step, "metrics": metrics})
        for metric_root in metric_roots:
            value = row.get(metric_root)
            if isinstance(value, Mapping):
                for name, metric_value in _numeric_leaves(value):
                    curves.setdefault(name, []).append({"step": step, "value": metric_value})
    return {
        "kind": TRAINING_HISTORY_KIND,
        "step_unit": step_unit,
        "records": records,
        "curves": dict(sorted(curves.items())),
    }


def _record_metrics(row: Mapping[str, Any], metric_roots: Sequence[str]) -> dict[str, Any]:
    if len(metric_roots) == 1:
        value = row.get(metric_roots[0])
        if isinstance(value, Mapping):
            return dict(value)
    return {metric_root: row[metric_root] for metric_root in metric_roots if metric_root in row}


def _numeric_leaves(
    value: Mapping[str, Any],
    prefix: tuple[str, ...] = (),
) -> list[tuple[str, float]]:
    leaves: list[tuple[str, float]] = []
    for key, item in value.items():
        path = (*prefix, str(key))
        if isinstance(item, Mapping):
            leaves.extend(_numeric_leaves(item, path))
        elif isinstance(item, int | float) and not isinstance(item, bool):
            leaves.append(("_".join(path), float(item)))
    return leaves
