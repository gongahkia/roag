from __future__ import annotations

import json
import os
from collections.abc import Mapping, Sequence
from html import escape
from pathlib import Path
from typing import Any
from urllib.parse import quote

TRAINING_DASHBOARD_KIND = "kenjaku-training-dashboard-v0"
STEP_KEYS = ("epoch", "update", "step", "iteration")
RUN_ID_KEYS = ("run_id", "run", "experiment", "experiment_id", "name")
METRIC_ROOTS = ("metrics", "train", "eval", "validation", "timing", "losses", "evaluation")
HPARAM_ROOTS = (
    "hyperparameters",
    "hparams",
    "params",
    "config",
    "training_config",
    "model_config",
    "model",
    "training",
)
HPARAM_KEYS = (
    "batch_size",
    "clip_epsilon",
    "device",
    "dropout",
    "epochs",
    "eval_fraction",
    "gae_lambda",
    "gamma",
    "hidden_dim",
    "learning_rate",
    "lr",
    "model",
    "model_dim",
    "model_kind",
    "num_heads",
    "num_layers",
    "ppo_epochs",
    "rollout_games",
    "seed",
    "split_seed",
    "total_steps",
    "value_coef",
    "weight_decay",
)
METADATA_KEYS = {
    *STEP_KEYS,
    *RUN_ID_KEYS,
    *HPARAM_ROOTS,
    *HPARAM_KEYS,
    "created_at",
    "generated_at",
    "kind",
    "path",
    "source",
    "source_path",
    "timestamp",
}
PREFERRED_SUMMARY_METRICS = {
    "train_loss": ("train_loss", "loss_train", "training_loss"),
    "eval_loss": ("eval_loss", "validation_loss", "val_loss", "loss_eval"),
    "accuracy": (
        "eval_accuracy",
        "validation_accuracy",
        "accuracy",
        "action_accuracy",
        "evaluation_action_accuracy",
        "train_accuracy",
    ),
    "epoch_time": (
        "epoch_time",
        "epoch_seconds",
        "duration_seconds",
        "elapsed_seconds",
        "timing_epoch_seconds",
        "seconds",
    ),
}


def build_training_dashboard(
    paths: Sequence[Path],
    *,
    version: str,
    generated_at: str,
    title: str = "Kenjaku Training Dashboard",
) -> dict[str, Any]:
    runs = _training_runs(paths)
    return {
        "kind": TRAINING_DASHBOARD_KIND,
        "title": title,
        "kenjaku_version": version,
        "generated_at": generated_at,
        "runs": runs,
        "metric_columns": _metric_columns(runs),
        "hparam_columns": _hparam_columns(runs),
    }


def format_training_dashboard_html(
    dashboard: dict[str, Any],
    *,
    link_base_dir: Path | None = None,
) -> str:
    if dashboard.get("kind") != TRAINING_DASHBOARD_KIND:
        raise ValueError("not a training dashboard")
    title = _html_text(dashboard["title"])
    version = _html_text(dashboard["kenjaku_version"])
    generated_at = _html_text(dashboard["generated_at"])
    runs = dashboard["runs"]
    metric_columns = dashboard["metric_columns"]
    hparam_columns = dashboard["hparam_columns"]
    summary = _summary_section(runs)
    comparison = _comparison_section(
        runs,
        metric_columns=metric_columns,
        hparam_columns=hparam_columns,
        link_base_dir=link_base_dir,
    )
    run_sections = "\n".join(_run_section(run) for run in runs)
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" href="data:,">
  <title>{title}</title>
  <style>
    :root {{
      color-scheme: light;
      --bg: #f6f8f9;
      --text: #18212b;
      --muted: #66717e;
      --line: #d9e0e6;
      --panel: #ffffff;
      --accent: #0f766e;
      --accent-2: #b42318;
      --accent-3: #946200;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font: 15px/1.48 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }}
    header, main, footer {{
      width: min(1180px, calc(100% - 32px));
      margin: 0 auto;
    }}
    header {{ padding: 34px 0 16px; }}
    h1 {{ margin: 0 0 8px; font-size: 32px; line-height: 1.15; letter-spacing: 0; }}
    h2 {{ margin: 0 0 12px; font-size: 22px; letter-spacing: 0; }}
    h3 {{ margin: 0 0 10px; font-size: 17px; letter-spacing: 0; }}
    p {{ margin: 0 0 10px; }}
    a {{ color: var(--accent); }}
    section {{
      margin: 22px 0;
      padding-top: 20px;
      border-top: 1px solid var(--line);
    }}
    .eyebrow {{
      margin-bottom: 8px;
      color: var(--accent);
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0;
      text-transform: uppercase;
    }}
    .muted {{ color: var(--muted); }}
    .summary-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
      gap: 12px 18px;
    }}
    .summary-item {{
      padding-bottom: 10px;
      border-bottom: 1px solid var(--line);
    }}
    .summary-item dt {{
      color: var(--muted);
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0;
      text-transform: uppercase;
    }}
    .summary-item dd {{ margin: 4px 0 0; }}
    table {{
      width: 100%;
      border-collapse: collapse;
      margin-top: 10px;
      font-size: 14px;
    }}
    th, td {{
      padding: 9px 8px;
      border-bottom: 1px solid var(--line);
      text-align: left;
      vertical-align: top;
    }}
    th {{ color: var(--muted); font-size: 12px; letter-spacing: 0; text-transform: uppercase; }}
    th button {{
      all: unset;
      cursor: pointer;
      color: inherit;
      font: inherit;
      text-transform: uppercase;
    }}
    .metric {{
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }}
    .charts {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 12px;
    }}
    .chart {{
      margin: 0;
      padding: 12px;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: var(--panel);
    }}
    .chart figcaption {{
      display: flex;
      justify-content: space-between;
      gap: 12px;
      margin-bottom: 8px;
      font-weight: 700;
    }}
    .chart svg {{
      display: block;
      width: 100%;
      height: auto;
    }}
    .axis {{ stroke: #b7c0ca; stroke-width: 1; }}
    .grid {{ stroke: #e5eaef; stroke-width: 1; }}
    .line {{ fill: none; stroke: var(--accent); stroke-width: 2.4; }}
    .line-alt {{ stroke: var(--accent-2); }}
    .line-time {{ stroke: var(--accent-3); }}
    .dot {{ fill: var(--accent); }}
    footer {{ padding: 8px 0 34px; color: var(--muted); }}
    @media (max-width: 760px) {{
      header, main, footer {{ width: min(100% - 20px, 1180px); }}
      h1 {{ font-size: 27px; }}
      section {{ overflow-x: auto; }}
      table {{ min-width: 860px; }}
      .chart {{ min-width: 260px; }}
    }}
  </style>
</head>
<body>
  <header>
    <p class="eyebrow">Kenjaku {version}</p>
    <h1>{title}</h1>
    <p class="muted">
      Generated from local epoch metrics JSONL files. No browser network access required.
    </p>
  </header>
  <main>
    {summary}
    {comparison}
    {run_sections}
  </main>
  <footer>Generated at {generated_at}</footer>
  <script>
{_sort_script()}
  </script>
</body>
</html>
"""


def _training_runs(paths: Sequence[Path]) -> list[dict[str, Any]]:
    grouped: dict[tuple[str, str], list[dict[str, Any]]] = {}
    for path in paths:
        source = Path(path)
        rows = _read_jsonl(source)
        fallback_run_id = _fallback_run_id(source)
        for line_number, payload in rows:
            run_id = _run_id(payload, fallback=fallback_run_id)
            step_key, step = _step(payload, source=source, line_number=line_number)
            metrics = _metrics(payload)
            hparams = _hparams(payload)
            grouped.setdefault((str(source), run_id), []).append(
                {
                    "step": step,
                    "step_unit": step_key,
                    "line_number": line_number,
                    "metrics": metrics,
                    "hparams": hparams,
                }
            )
    runs = [
        _run_payload(run_id, source_path, rows)
        for (source_path, run_id), rows in sorted(grouped.items())
    ]
    if not runs:
        raise ValueError("no training metric rows found")
    return runs


def _read_jsonl(path: Path) -> list[tuple[int, dict[str, Any]]]:
    rows: list[tuple[int, dict[str, Any]]] = []
    try:
        with path.open("r", encoding="utf-8") as handle:
            for line_number, line in enumerate(handle, start=1):
                stripped = line.strip()
                if not stripped:
                    continue
                try:
                    payload = json.loads(stripped)
                except json.JSONDecodeError as error:
                    raise ValueError(f"{path}:{line_number}: invalid JSON") from error
                if not isinstance(payload, dict):
                    raise ValueError(f"{path}:{line_number}: row must be a JSON object")
                rows.append((line_number, payload))
    except FileNotFoundError as error:
        raise ValueError(f"metrics file not found: {path}") from error
    return rows


def _run_payload(
    run_id: str,
    source_path: str,
    rows: Sequence[dict[str, Any]],
) -> dict[str, Any]:
    ordered = sorted(rows, key=lambda row: (float(row["step"]), int(row["line_number"])))
    curves: dict[str, list[dict[str, float]]] = {}
    hparams: dict[str, Any] = {}
    for row in ordered:
        hparams.update(row["hparams"])
        for metric, value in row["metrics"].items():
            curves.setdefault(metric, []).append({"step": float(row["step"]), "value": value})
    final_metrics = ordered[-1]["metrics"] if ordered else {}
    first_step = float(ordered[0]["step"]) if ordered else None
    last_step = float(ordered[-1]["step"]) if ordered else None
    return {
        "id": run_id,
        "fragment": _fragment(run_id, source_path),
        "source_path": source_path,
        "row_count": len(ordered),
        "step_unit": ordered[-1]["step_unit"] if ordered else "step",
        "first_step": first_step,
        "last_step": last_step,
        "hparams": hparams,
        "final_metrics": final_metrics,
        "curves": dict(sorted(curves.items())),
    }


def _run_id(payload: Mapping[str, Any], *, fallback: str) -> str:
    for key in RUN_ID_KEYS:
        value = payload.get(key)
        if isinstance(value, str) and value:
            return value
        if isinstance(value, int | float) and not isinstance(value, bool):
            return str(value)
    return fallback


def _fallback_run_id(path: Path) -> str:
    name = path.name
    for suffix in (".metrics.jsonl", ".jsonl"):
        if name.endswith(suffix):
            return name[: -len(suffix)]
    return path.stem


def _step(
    payload: Mapping[str, Any],
    *,
    source: Path,
    line_number: int,
) -> tuple[str, float]:
    for key in STEP_KEYS:
        value = payload.get(key)
        if isinstance(value, int | float) and not isinstance(value, bool):
            return key, float(value)
    raise ValueError(f"{source}:{line_number}: row missing numeric epoch/update/step")


def _metrics(payload: Mapping[str, Any]) -> dict[str, float]:
    metrics: dict[str, float] = {}
    for root in METRIC_ROOTS:
        value = payload.get(root)
        if isinstance(value, Mapping):
            prefix = () if root == "metrics" else (root,)
            metrics.update(_numeric_leaves(value, prefix=prefix))
    for key, value in payload.items():
        if key in METADATA_KEYS or key in METRIC_ROOTS:
            continue
        if isinstance(value, int | float) and not isinstance(value, bool):
            metrics[str(key)] = float(value)
    return dict(sorted(metrics.items()))


def _hparams(payload: Mapping[str, Any]) -> dict[str, Any]:
    hparams: dict[str, Any] = {}
    for root in HPARAM_ROOTS:
        value = payload.get(root)
        if isinstance(value, Mapping):
            hparams.update(_simple_leaves(value))
    for key in HPARAM_KEYS:
        value = payload.get(key) if key in payload else None
        if key in payload and _is_simple(value):
            hparams[key] = value
    return dict(sorted(hparams.items()))


def _numeric_leaves(
    value: Mapping[str, Any],
    *,
    prefix: tuple[str, ...] = (),
) -> dict[str, float]:
    leaves: dict[str, float] = {}
    for key, item in value.items():
        path = (*prefix, str(key))
        if isinstance(item, Mapping):
            leaves.update(_numeric_leaves(item, prefix=path))
        elif isinstance(item, int | float) and not isinstance(item, bool):
            leaves["_".join(path)] = float(item)
    return leaves


def _simple_leaves(
    value: Mapping[str, Any],
    *,
    prefix: tuple[str, ...] = (),
) -> dict[str, Any]:
    leaves: dict[str, Any] = {}
    for key, item in value.items():
        if str(key) in {"history", "metrics"}:
            continue
        path = (*prefix, str(key))
        if isinstance(item, Mapping):
            leaves.update(_simple_leaves(item, prefix=path))
        elif _is_simple(item):
            leaves["_".join(path)] = item
    return leaves


def _is_simple(value: Any) -> bool:
    return value is None or isinstance(value, str | int | float | bool)


def _metric_columns(runs: Sequence[dict[str, Any]]) -> list[dict[str, str]]:
    columns: list[dict[str, str]] = []
    for label, names in (
        ("Train Loss", PREFERRED_SUMMARY_METRICS["train_loss"]),
        ("Eval Loss", PREFERRED_SUMMARY_METRICS["eval_loss"]),
        ("Accuracy", PREFERRED_SUMMARY_METRICS["accuracy"]),
        ("Epoch Time", PREFERRED_SUMMARY_METRICS["epoch_time"]),
    ):
        name = _first_existing_metric(runs, names)
        if name is not None:
            columns.append({"label": label, "metric": name})
    return columns


def _hparam_columns(runs: Sequence[dict[str, Any]]) -> list[str]:
    columns = sorted({key for run in runs for key in run["hparams"]})
    preferred = [key for key in HPARAM_KEYS if key in columns]
    remaining = [key for key in columns if key not in preferred]
    return [*preferred, *remaining]


def _first_existing_metric(
    runs: Sequence[dict[str, Any]],
    names: Sequence[str],
) -> str | None:
    available = {metric for run in runs for metric in run["curves"]}
    for name in names:
        if name in available:
            return name
    return None


def _summary_section(runs: Sequence[dict[str, Any]]) -> str:
    steps = [run["last_step"] for run in runs if run.get("last_step") is not None]
    metrics = sorted({metric for run in runs for metric in run["curves"]})
    items = (
        ("Runs", str(len(runs))),
        ("Metric Rows", str(sum(int(run["row_count"]) for run in runs))),
        ("Max Step", _format_number(max(steps) if steps else None)),
        ("Curves", str(len(metrics))),
    )
    body = "\n".join(_summary_item(label, value) for label, value in items)
    return f"""
    <section aria-label="training dashboard summary">
      <h2>Summary</h2>
      <dl class="summary-grid">
        {body}
      </dl>
    </section>
"""


def _comparison_section(
    runs: Sequence[dict[str, Any]],
    *,
    metric_columns: Sequence[dict[str, str]],
    hparam_columns: Sequence[str],
    link_base_dir: Path | None,
) -> str:
    headers = [
        _sortable_header(0, "Run", "text"),
        _sortable_header(1, "Source", "text"),
        _sortable_header(2, "Rows", "number"),
        _sortable_header(3, "Last Step", "number"),
    ]
    for index, column in enumerate(metric_columns, start=4):
        headers.append(_sortable_header(index, column["label"], "number"))
    offset = 4 + len(metric_columns)
    for index, column in enumerate(hparam_columns, start=offset):
        headers.append(_sortable_header(index, column.replace("_", " ").title(), "text"))
    rows = "\n".join(
        _comparison_row(
            run,
            metric_columns=metric_columns,
            hparam_columns=hparam_columns,
            link_base_dir=link_base_dir,
        )
        for run in runs
    )
    return f"""
    <section>
      <h2>Runs</h2>
      <p class="muted">Click any column header to sort.</p>
      <table id="training-runs-table">
        <thead>
          <tr>
            {"".join(headers)}
          </tr>
        </thead>
        <tbody>
          {rows}
        </tbody>
      </table>
    </section>
"""


def _comparison_row(
    run: dict[str, Any],
    *,
    metric_columns: Sequence[dict[str, str]],
    hparam_columns: Sequence[str],
    link_base_dir: Path | None,
) -> str:
    cells = [
        (
            f'<td data-sort="{_html_attr(run["id"])}">'
            f'<a href="#{_html_attr(run["fragment"])}">{_html_text(run["id"])}</a></td>'
        ),
        (
            f'<td data-sort="{_html_attr(Path(run["source_path"]).name)}">'
            f"{_source_link(run['source_path'], link_base_dir=link_base_dir)}</td>"
        ),
        _metric_cell(run["row_count"]),
        _metric_cell(run["last_step"]),
    ]
    for column in metric_columns:
        cells.append(_metric_cell(run["final_metrics"].get(column["metric"])))
    for column in hparam_columns:
        value = run["hparams"].get(column)
        cells.append(
            f'<td data-sort="{_html_attr(_sort_text(value))}">{_html_text(_display(value))}</td>'
        )
    return f"<tr>{''.join(cells)}</tr>"


def _run_section(run: dict[str, Any]) -> str:
    charts = "\n".join(_chart_figure(run, metric) for metric in _chart_metrics(run))
    if not charts:
        charts = '<p class="muted">No numeric curves found.</p>'
    overview = "\n".join(
        _summary_item(label, value)
        for label, value in (
            ("Source", Path(run["source_path"]).name),
            ("Step Unit", run["step_unit"]),
            ("First Step", _format_number(run["first_step"])),
            ("Last Step", _format_number(run["last_step"])),
            ("Rows", str(run["row_count"])),
        )
    )
    return f"""
    <section id="{_html_attr(run["fragment"])}">
      <h2>{_html_text(run["id"])}</h2>
      <dl class="summary-grid">
        {overview}
      </dl>
      <div class="charts">
        {charts}
      </div>
    </section>
"""


def _chart_metrics(run: dict[str, Any]) -> list[str]:
    curves = run["curves"]
    selected: list[str] = []
    for names in PREFERRED_SUMMARY_METRICS.values():
        for name in names:
            if name in curves and name not in selected:
                selected.append(name)
                break
    for name in sorted(curves):
        if name not in selected and len(selected) < 8:
            selected.append(name)
    return selected


def _chart_figure(run: dict[str, Any], metric: str) -> str:
    points = run["curves"][metric]
    latest = points[-1]["value"] if points else None
    return (
        '<figure class="chart">'
        f"<figcaption><span>{_html_text(metric)}</span>"
        f'<span class="muted">{_html_text(_format_number(latest))}</span></figcaption>'
        f"{_line_chart(points, metric=metric)}"
        "</figure>"
    )


def _line_chart(points: Sequence[dict[str, float]], *, metric: str) -> str:
    width = 440
    height = 180
    left = 42
    right = 12
    top = 14
    bottom = 30
    if not points:
        return ""
    xs = [point["step"] for point in points]
    ys = [point["value"] for point in points]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    x_span = max_x - min_x
    y_span = max_y - min_y

    def sx(value: float) -> float:
        return left + ((value - min_x) / x_span) * (width - left - right) if x_span else width / 2

    def sy(value: float) -> float:
        return (
            top + (height - top - bottom) / 2
            if not y_span
            else (height - bottom - ((value - min_y) / y_span) * (height - top - bottom))
        )

    coords = [(sx(point["step"]), sy(point["value"])) for point in points]
    path = " ".join(f"{x:.1f},{y:.1f}" for x, y in coords)
    line_class = _line_class(metric)
    if len(coords) == 1:
        shape = (
            f'<circle class="dot" cx="{coords[0][0]:.1f}" cy="{coords[0][1]:.1f}" r="4"></circle>'
        )
    else:
        shape = f'<polyline class="line {line_class}" points="{path}"></polyline>'
    min_x_text = _html_text(_format_number(min_x))
    max_x_text = _html_text(_format_number(max_x))
    min_y_text = _html_text(_format_number(min_y))
    max_y_text = _html_text(_format_number(max_y))
    x2 = width - right
    y2 = height - bottom
    max_x_label = (
        f'<text x="{x2}" y="{height - 8}" fill="#66717e" '
        f'font-size="11" text-anchor="end">{max_x_text}</text>'
    )
    return f"""
        <svg viewBox="0 0 {width} {height}" role="img" aria-label="{_html_attr(metric)} chart">
          <line class="grid" x1="{left}" y1="{top}" x2="{width - right}" y2="{top}"></line>
          <line class="grid" x1="{left}" y1="{y2}" x2="{x2}" y2="{y2}"></line>
          <line class="axis" x1="{left}" y1="{top}" x2="{left}" y2="{height - bottom}"></line>
          <line class="axis" x1="{left}" y1="{y2}" x2="{x2}" y2="{y2}"></line>
          {shape}
          <text x="{left}" y="{height - 8}" fill="#66717e" font-size="11">{min_x_text}</text>
          {max_x_label}
          <text x="4" y="{top + 4}" fill="#66717e" font-size="11">{max_y_text}</text>
          <text x="4" y="{y2}" fill="#66717e" font-size="11">{min_y_text}</text>
        </svg>
"""


def _line_class(metric: str) -> str:
    if "time" in metric or "second" in metric:
        return "line-time"
    if "eval" in metric or "accuracy" in metric:
        return "line-alt"
    return ""


def _summary_item(label: str, value: str) -> str:
    return (
        f'<div class="summary-item"><dt>{_html_text(label)}</dt><dd>{_html_text(value)}</dd></div>'
    )


def _sortable_header(column: int, label: str, sort_type: str) -> str:
    return (
        "<th>"
        f'<button type="button" data-sort-column="{column}" data-sort-type="{sort_type}">'
        f"{_html_text(label)}</button></th>"
    )


def _source_link(path: str, *, link_base_dir: Path | None) -> str:
    target = _link_target(path, link_base_dir=link_base_dir)
    href = escape(quote(target.replace("\\", "/"), safe="/:#?&=%._~+-"), quote=True)
    return f'<a href="{href}">{_html_text(target)}</a>'


def _link_target(path: str, *, link_base_dir: Path | None) -> str:
    if path.startswith(("http://", "https://")) or link_base_dir is None:
        return path
    path_obj = Path(path)
    if path_obj.is_absolute():
        try:
            return os.path.relpath(path_obj, link_base_dir)
        except ValueError:
            return path
    return path


def _metric_cell(value: Any) -> str:
    return (
        f'<td class="metric" data-sort="{_html_attr(_sort_number(value))}">'
        f"{_html_text(_format_number(value))}</td>"
    )


def _sort_number(value: Any) -> str:
    return "" if not isinstance(value, int | float) else f"{float(value):.12f}"


def _sort_text(value: Any) -> str:
    return "" if value is None else str(value)


def _display(value: Any) -> str:
    return "n/a" if value is None else str(value)


def _format_number(value: Any) -> str:
    if value is None:
        return "n/a"
    if not isinstance(value, int | float):
        return str(value)
    number = float(value)
    if number.is_integer():
        return str(int(number))
    if abs(number) >= 100:
        return f"{number:.2f}"
    if abs(number) >= 1:
        return f"{number:.4f}"
    return f"{number:.6f}".rstrip("0").rstrip(".")


def _fragment(run_id: str, source_path: str) -> str:
    raw = f"{run_id}-{Path(source_path).stem}".lower()
    chars = [char if char.isalnum() else "-" for char in raw]
    return "run-" + "-".join("".join(chars).split("-"))


def _html_text(value: Any) -> str:
    return escape(str(value), quote=True)


def _html_attr(value: Any) -> str:
    return escape(str(value), quote=True)


def _sort_script() -> str:
    return r"""
(() => {
  const table = document.getElementById("training-runs-table");
  if (!table) {
    return;
  }
  const body = table.tBodies[0];
  for (const button of table.querySelectorAll("[data-sort-column]")) {
    button.addEventListener("click", () => {
      const column = Number(button.dataset.sortColumn);
      const type = button.dataset.sortType || "text";
      const direction = button.dataset.sortDirection === "asc" ? "desc" : "asc";
      button.dataset.sortDirection = direction;
      const rows = Array.from(body.rows);
      rows.sort((left, right) => {
        const leftValue = left.cells[column]?.dataset.sort || "";
        const rightValue = right.cells[column]?.dataset.sort || "";
        if (type === "number") {
          return compareNumber(leftValue, rightValue, direction);
        }
        return compareText(leftValue, rightValue, direction);
      });
      for (const row of rows) {
        body.appendChild(row);
      }
    });
  }
})();

function compareNumber(left, right, direction) {
  const leftNumber = left === "" ? Number.NEGATIVE_INFINITY : Number(left);
  const rightNumber = right === "" ? Number.NEGATIVE_INFINITY : Number(right);
  const result = leftNumber - rightNumber;
  return direction === "asc" ? result : -result;
}

function compareText(left, right, direction) {
  const result = left.localeCompare(right, undefined, {numeric: true});
  return direction === "asc" ? result : -result;
}
""".strip()
