from __future__ import annotations

import json
from collections import Counter
from collections.abc import Iterable, Sequence
from html import escape
from math import exp
from pathlib import Path
from typing import Any

from kenjaku.core import ActionKind, TileType
from kenjaku.core.shanten import shanten
from kenjaku.training.decision_snapshots import DECISION_SNAPSHOT_KIND

INTERPRETABILITY_OVERLAY_KIND = "kenjaku-interpretability-overlay-v0"
INTERPRETABILITY_POLICY_KIND = "heuristic-discard-overlay-v0"
TOP_ALTERNATIVES = 3
OVERLAY_PAGE_SIZE = 100


def read_interpretability_snapshots(
    path: Path,
    *,
    limit: int | None = None,
) -> tuple[list[dict[str, Any]], dict[str, int]]:
    if limit is not None and limit < 0:
        raise ValueError("limit must be non-negative")

    snapshots: list[dict[str, Any]] = []
    rows = 0
    skipped_rows = 0
    malformed_rows = 0
    with path.open(encoding="utf-8") as handle:
        for line in handle:
            rows += 1
            try:
                payload = json.loads(line)
            except json.JSONDecodeError:
                malformed_rows += 1
                continue
            if not _is_decision_snapshot(payload):
                malformed_rows += 1
                continue
            if not _is_discard_snapshot(payload):
                skipped_rows += 1
                continue
            snapshots.append(payload)
            if limit is not None and len(snapshots) >= limit:
                break

    return snapshots, {
        "rows": rows,
        "discard_snapshots": len(snapshots),
        "skipped_rows": skipped_rows,
        "malformed_rows": malformed_rows,
    }


def build_interpretability_overlay(
    snapshots: Iterable[dict[str, Any]],
    *,
    title: str = "Kenjaku Interpretability Overlay",
    min_decisions: int = 0,
) -> dict[str, Any]:
    if min_decisions < 0:
        raise ValueError("min_decisions must be non-negative")

    decisions: list[dict[str, Any]] = []
    source_labels: Counter[str] = Counter()
    malformed_snapshots = 0
    for index, snapshot in enumerate(snapshots):
        decision = _decision_overlay(snapshot, index=index)
        if decision is None:
            malformed_snapshots += 1
            continue
        decisions.append(decision)
        source_labels[_source_label(snapshot)] += 1

    if len(decisions) < min_decisions:
        raise ValueError(
            f"expected at least {min_decisions} discard decisions, found {len(decisions)}"
        )

    return {
        "kind": INTERPRETABILITY_OVERLAY_KIND,
        "title": title,
        "policy_kind": INTERPRETABILITY_POLICY_KIND,
        "top_alternatives_per_decision": TOP_ALTERNATIVES,
        "page_size": OVERLAY_PAGE_SIZE,
        "decision_count": len(decisions),
        "malformed_snapshots": malformed_snapshots,
        "source_labels": dict(sorted(source_labels.items())),
        "source_paths_rendered": False,
        "public_artifact": True,
        "disclaimer": (
            "Heuristic probabilities, deal-in risk, and point impact are audit signals, "
            "not calibrated engine-strength claims."
        ),
        "decisions": decisions,
    }


def write_interpretability_overlay_html(path: Path, report: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(format_interpretability_overlay_html(report), encoding="utf-8")


def format_interpretability_overlay_html(report: dict[str, Any]) -> str:
    title = escape(str(report.get("title", "Kenjaku Interpretability Overlay")))
    decisions = report.get("decisions")
    decision_items = decisions if isinstance(decisions, list) else []
    payload = _overlay_json_payload(report, decision_items)
    parts = [
        "<!doctype html>",
        '<html lang="en">',
        "<head>",
        '<meta charset="utf-8">',
        '<meta name="viewport" content="width=device-width, initial-scale=1">',
        '<link rel="icon" href="data:,">',
        f"<title>{title}</title>",
        "<style>",
        _overlay_css(),
        "</style>",
        "</head>",
        "<body>",
        "<main>",
        f"<h1>{title}</h1>",
        '<section class="summary">',
        _summary_html(report),
        "</section>",
        _controls_html(),
        '<section id="decision-list" class="decision-list" aria-live="polite"></section>',
        f'<script id="overlay-data" type="application/json">{payload}</script>',
        "<script>",
        _overlay_js(),
        "</script>",
    ]
    parts.extend(["</main>", "</body>", "</html>"])
    return "\n".join(parts)


def _decision_overlay(snapshot: dict[str, Any], *, index: int) -> dict[str, Any] | None:
    if not _is_discard_snapshot(snapshot):
        return None
    hand_counts = _counts(snapshot.get("hand_counts"))
    visible_counts = _counts(snapshot.get("visible_counts"))
    if hand_counts is None or visible_counts is None:
        return None
    legal_actions = snapshot.get("legal_actions")
    if not isinstance(legal_actions, list):
        return None
    seat = snapshot.get("seat")
    if not isinstance(seat, int):
        return None
    current_shanten = _safe_shanten(hand_counts)
    candidates = []
    for action in legal_actions:
        candidate = _candidate_overlay(
            snapshot,
            action,
            hand_counts,
            visible_counts,
            current_shanten,
        )
        if candidate is not None:
            candidates.append(candidate)
    if not candidates:
        return None

    probabilities = _softmax([candidate["logit"] for candidate in candidates])
    for candidate, probability in zip(candidates, probabilities, strict=True):
        candidate["policy_probability"] = probability
        del candidate["logit"]
    candidates.sort(
        key=lambda candidate: (
            -float(candidate["policy_probability"]),
            str(candidate["tile"]),
        )
    )
    actual_action = snapshot.get("actual_action")
    actual_tile = (
        actual_action.get("tile")
        if isinstance(actual_action, dict) and isinstance(actual_action.get("tile"), str)
        else None
    )
    actual_candidate = next(
        (candidate for candidate in candidates if candidate["tile"] == actual_tile),
        None,
    )
    actual_shanten_delta = (
        None if actual_candidate is None else actual_candidate.get("shanten_delta")
    )
    dora_indicators = _string_list(snapshot.get("dora_indicators"))
    hand_tiles = _tile_names_from_counts(hand_counts)
    return {
        "index": index,
        "row_id": snapshot.get("row_id") if isinstance(snapshot.get("row_id"), str) else "",
        "round_index": snapshot.get("round_index"),
        "event_index": snapshot.get("event_index"),
        "seat": seat,
        "actual_discard": actual_tile,
        "current_shanten": current_shanten,
        "actual_shanten_delta": actual_shanten_delta,
        "shanten_delta_bin": _shanten_delta_bin(actual_shanten_delta),
        "hand_tiles": hand_tiles,
        "hand_pattern": " ".join(hand_tiles),
        "dora_indicators": dora_indicators,
        "top_alternatives": candidates[:TOP_ALTERNATIVES],
    }


def _candidate_overlay(
    snapshot: dict[str, Any],
    action: Any,
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    current_shanten: int | None,
) -> dict[str, Any] | None:
    if not isinstance(action, dict) or action.get("kind") != ActionKind.DISCARD.value:
        return None
    tile_text = action.get("tile")
    if not isinstance(tile_text, str):
        return None
    try:
        tile_type = TileType.parse(tile_text)
    except ValueError:
        return None
    if hand_counts[tile_type.index] <= 0:
        return None

    after_counts = list(hand_counts)
    after_counts[tile_type.index] -= 1
    next_shanten = _safe_shanten(tuple(after_counts))
    shanten_delta = (
        None
        if current_shanten is None or next_shanten is None
        else next_shanten - current_shanten
    )
    risk, reasons = _estimated_deal_in_risk(snapshot, tile_type, visible_counts)
    expected_point_impact = _expected_point_impact(shanten_delta, risk)
    logit = _candidate_logit(next_shanten, shanten_delta, risk, expected_point_impact)
    return {
        "tile": tile_type.notation,
        "resulting_shanten": next_shanten,
        "shanten_delta": shanten_delta,
        "estimated_deal_in_risk": risk,
        "expected_point_impact": expected_point_impact,
        "risk_reasons": reasons,
        "logit": logit,
    }


def _estimated_deal_in_risk(
    snapshot: dict[str, Any],
    tile_type: TileType,
    visible_counts: tuple[int, ...],
) -> tuple[float, list[str]]:
    seat = snapshot.get("seat")
    active = snapshot.get("active_riichi_seats")
    active_opponents = [
        index
        for index, is_active in enumerate(active if isinstance(active, list) else [])
        if is_active and index != seat
    ]
    visible = visible_counts[tile_type.index]
    if not active_opponents:
        return 0.03, ["no active riichi opponent"]

    risk = 0.42 + 0.08 * max(0, len(active_opponents) - 1)
    reasons = [f"{len(active_opponents)} active riichi opponent(s)"]
    river_counts = _river_counts(snapshot.get("river_counts_by_seat"))
    if _is_genbutsu(tile_type.index, active_opponents, river_counts):
        return 0.01, ["genbutsu against active riichi"]

    if visible >= 4:
        risk -= 0.24
        reasons.append("all copies visible")
    elif visible == 3:
        risk -= 0.16
        reasons.append("one-chance by visible count")
    elif visible <= 1:
        risk += 0.08
        reasons.append("few visible copies")

    if _has_suji(tile_type.index, active_opponents, river_counts):
        risk -= 0.07
        reasons.append("suji candidate")
    if tile_type.is_terminal_or_honor:
        risk -= 0.03
        reasons.append("terminal/honor adjustment")

    return _clamp_probability(risk), reasons


def _candidate_logit(
    next_shanten: int | None,
    shanten_delta: int | None,
    risk: float,
    expected_point_impact: int,
) -> float:
    shanten_term = -2.0 if next_shanten is None else -0.85 * next_shanten
    delta_term = 0.0 if shanten_delta is None else -0.95 * shanten_delta
    point_term = expected_point_impact / 8000.0
    return shanten_term + delta_term + point_term - 1.60 * risk


def _expected_point_impact(shanten_delta: int | None, risk: float) -> int:
    efficiency_points = 0 if shanten_delta is None else -shanten_delta * 1200
    risk_points = risk * 8000
    return round(efficiency_points - risk_points)


def _softmax(logits: Sequence[float]) -> list[float]:
    if not logits:
        return []
    offset = max(logits)
    weights = [exp(logit - offset) for logit in logits]
    total = sum(weights)
    return [weight / total for weight in weights]


def _safe_shanten(counts: Sequence[int]) -> int | None:
    try:
        return shanten(counts)
    except ValueError:
        return None


def _is_discard_snapshot(value: Any) -> bool:
    return (
        _is_decision_snapshot(value)
        and value.get("decision_type") == "discard"
    )


def _is_decision_snapshot(value: Any) -> bool:
    return isinstance(value, dict) and value.get("kind") == DECISION_SNAPSHOT_KIND


def _counts(value: Any) -> tuple[int, ...] | None:
    if not isinstance(value, list) or len(value) != 34:
        return None
    if not all(isinstance(count, int) and count >= 0 for count in value):
        return None
    return tuple(value)


def _tile_names_from_counts(counts: Sequence[int]) -> list[str]:
    names: list[str] = []
    for index, count in enumerate(counts):
        names.extend(TileType(index).notation for _ in range(count))
    return names


def _string_list(value: Any) -> list[str]:
    if not isinstance(value, list):
        return []
    return [item for item in value if isinstance(item, str)]


def _shanten_delta_bin(value: Any) -> str:
    if not isinstance(value, int):
        return "unknown"
    if value < 0:
        return "improves"
    if value > 0:
        return "worsens"
    return "same"


def _river_counts(value: Any) -> tuple[tuple[int, ...], ...]:
    if not isinstance(value, list):
        return ()
    rows = []
    for row in value:
        counts = _counts(row)
        if counts is None:
            return ()
        rows.append(counts)
    return tuple(rows)


def _is_genbutsu(
    tile_index: int,
    active_opponents: Sequence[int],
    river_counts: Sequence[Sequence[int]],
) -> bool:
    return any(
        opponent < len(river_counts) and river_counts[opponent][tile_index] > 0
        for opponent in active_opponents
    )


def _has_suji(
    tile_index: int,
    active_opponents: Sequence[int],
    river_counts: Sequence[Sequence[int]],
) -> bool:
    if tile_index >= 27:
        return False
    suit_start = tile_index - tile_index % 9
    suji_indices = [
        candidate
        for candidate in (tile_index - 3, tile_index + 3)
        if suit_start <= candidate < suit_start + 9
    ]
    return any(
        opponent < len(river_counts)
        and any(river_counts[opponent][candidate] > 0 for candidate in suji_indices)
        for opponent in active_opponents
    )


def _source_label(snapshot: dict[str, Any]) -> str:
    source = snapshot.get("source")
    if isinstance(source, dict) and isinstance(source.get("label"), str):
        return source["label"]
    return "unknown"


def _clamp_probability(value: float) -> float:
    return min(0.95, max(0.0, value))


def _summary_html(report: dict[str, Any]) -> str:
    source_labels = report.get("source_labels")
    if isinstance(source_labels, dict) and source_labels:
        source_text = ", ".join(
            f"{escape(str(label))}={count}"
            for label, count in sorted(source_labels.items())
        )
    else:
        source_text = "unknown=0"
    return "\n".join(
        [
            "<dl>",
            f"<div><dt>Decisions</dt><dd>{int(report.get('decision_count', 0))}</dd></div>",
            "<div><dt>Policy</dt><dd>"
            f"{escape(str(report.get('policy_kind', 'unknown')))}</dd></div>",
            "<div><dt>Top alternatives</dt><dd>"
            f"{int(report.get('top_alternatives_per_decision', 0))}</dd></div>",
            "<div><dt>Page size</dt><dd>"
            f"{int(report.get('page_size', OVERLAY_PAGE_SIZE))}</dd></div>",
            f"<div><dt>Sources</dt><dd>{source_text}</dd></div>",
            "</dl>",
            f"<p>{escape(str(report.get('disclaimer', '')))}</p>",
        ]
    )


def _controls_html() -> str:
    return "\n".join(
        [
            '<section class="controls" aria-label="Filters">',
            "<label>Search"
            '<input id="search" type="search" placeholder="hand pattern or dora tile">'
            "</label>",
            "<label>Round<select id=\"round-filter\"></select></label>",
            "<label>Seat<select id=\"seat-filter\"></select></label>",
            "<label>Discard<select id=\"tile-filter\"></select></label>",
            "<label>Shanten<select id=\"shanten-filter\"></select></label>",
            "</section>",
            '<section class="pager" aria-label="Pagination">',
            '<button id="prev-page" type="button">Prev</button>',
            '<span id="page-info"></span>',
            '<button id="next-page" type="button">Next</button>',
            '<span id="result-count"></span>',
            "</section>",
        ]
    )


def _overlay_json_payload(
    report: dict[str, Any],
    decisions: Sequence[Any],
) -> str:
    payload = {
        "page_size": int(report.get("page_size", OVERLAY_PAGE_SIZE)),
        "decisions": decisions,
    }
    return (
        json.dumps(payload, sort_keys=True, separators=(",", ":"))
        .replace("&", "\\u0026")
        .replace("<", "\\u003c")
        .replace(">", "\\u003e")
    )


def _overlay_js() -> str:
    return r"""
const data = JSON.parse(document.getElementById("overlay-data").textContent);
const pageSize = data.page_size || 100;
const decisions = Array.isArray(data.decisions) ? data.decisions : [];
const state = {page: 1, filtered: decisions.slice()};
const controls = {
  search: document.getElementById("search"),
  round: document.getElementById("round-filter"),
  seat: document.getElementById("seat-filter"),
  tile: document.getElementById("tile-filter"),
  shanten: document.getElementById("shanten-filter"),
  prev: document.getElementById("prev-page"),
  next: document.getElementById("next-page"),
  pageInfo: document.getElementById("page-info"),
  resultCount: document.getElementById("result-count"),
  list: document.getElementById("decision-list")
};

function field(value) {
  return value === null || value === undefined ? "" : String(value);
}

function unique(values) {
  return Array.from(new Set(values.filter(Boolean))).sort((a, b) => {
    return a.localeCompare(b, undefined, {numeric: true});
  });
}

function fillSelect(select, label, values) {
  select.innerHTML = `<option value="">All ${label}</option>`;
  for (const value of values) {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = value;
    select.appendChild(option);
  }
}

function initFilters() {
  fillSelect(controls.round, "rounds", unique(decisions.map(d => field(d.round_index))));
  fillSelect(controls.seat, "seats", unique(decisions.map(d => field(d.seat))));
  fillSelect(controls.tile, "discards", unique(decisions.map(d => field(d.actual_discard))));
  fillSelect(controls.shanten, "shanten bins", ["improves", "same", "worsens", "unknown"]);
}

function matchesSearch(decision, query) {
  if (!query) {
    return true;
  }
  const dora = Array.isArray(decision.dora_indicators)
    ? decision.dora_indicators.join(" ")
    : "";
  return `${field(decision.hand_pattern)} ${dora}`.toLowerCase().includes(query);
}

function applyFilters() {
  const query = controls.search.value.trim().toLowerCase();
  state.filtered = decisions.filter(decision => {
    return matchesSearch(decision, query)
      && (!controls.round.value || field(decision.round_index) === controls.round.value)
      && (!controls.seat.value || field(decision.seat) === controls.seat.value)
      && (!controls.tile.value || field(decision.actual_discard) === controls.tile.value)
      && (!controls.shanten.value || field(decision.shanten_delta_bin) === controls.shanten.value);
  });
  state.page = 1;
  render();
}

function render() {
  const pages = Math.max(1, Math.ceil(state.filtered.length / pageSize));
  state.page = Math.min(Math.max(1, state.page), pages);
  const start = (state.page - 1) * pageSize;
  const pageItems = state.filtered.slice(start, start + pageSize);
  controls.list.innerHTML = pageItems.map(renderDecision).join("");
  controls.pageInfo.textContent = `Page ${state.page} of ${pages}`;
  controls.resultCount.textContent = `${state.filtered.length} matching decisions`;
  controls.prev.disabled = state.page <= 1;
  controls.next.disabled = state.page >= pages;
}

function renderDecision(decision) {
  const alternatives = Array.isArray(decision.top_alternatives)
    ? decision.top_alternatives
    : [];
  const rows = alternatives.length
    ? alternatives.map(renderAlternative).join("")
    : '<tr><td colspan="6">No alternatives</td></tr>';
  const dora = Array.isArray(decision.dora_indicators)
    ? decision.dora_indicators.join(" ")
    : "";
  return `<section class="decision">
    <header>
      <h2>Decision ${Number(decision.index || 0) + 1}</h2>
      <p>
        round=${escapeHtml(field(decision.round_index))}
        event=${escapeHtml(field(decision.event_index))}
        seat=${escapeHtml(field(decision.seat))}
        actual=${escapeHtml(field(decision.actual_discard))}
        shanten=${escapeHtml(field(decision.current_shanten))}
        delta=${escapeHtml(field(decision.actual_shanten_delta))}
      </p>
      <p>hand=${escapeHtml(field(decision.hand_pattern))}</p>
      <p>dora=${escapeHtml(dora || "none")}</p>
      <code>${escapeHtml(field(decision.row_id))}</code>
    </header>
    <table>
      <thead><tr>
        <th>Tile</th><th>Policy probability</th><th>Shanten delta</th>
        <th>Deal-in risk</th><th>Expected point impact</th><th>Reasoning</th>
      </tr></thead>
      <tbody>${rows}</tbody>
    </table>
  </section>`;
}

function renderAlternative(alternative) {
  const reasons = Array.isArray(alternative.risk_reasons)
    ? alternative.risk_reasons.join(", ")
    : "";
  return `<tr>
    <td>${escapeHtml(field(alternative.tile))}</td>
    <td>${formatPercent(alternative.policy_probability)}</td>
    <td>${escapeHtml(field(alternative.shanten_delta))}</td>
    <td>${formatPercent(alternative.estimated_deal_in_risk)}</td>
    <td>${formatPoints(alternative.expected_point_impact)}</td>
    <td>${escapeHtml(reasons)}</td>
  </tr>`;
}

function formatPercent(value) {
  return typeof value === "number" ? `${(value * 100).toFixed(1)}%` : "";
}

function formatPoints(value) {
  if (!Number.isInteger(value)) {
    return "";
  }
  return value >= 0 ? `+${value}` : String(value);
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, character => {
    return {
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#39;"
    }[character];
  });
}

const filterControls = [
  controls.search,
  controls.round,
  controls.seat,
  controls.tile,
  controls.shanten
];
for (const control of filterControls) {
  control.addEventListener("input", applyFilters);
}
controls.prev.addEventListener("click", () => {
  state.page -= 1;
  render();
});
controls.next.addEventListener("click", () => {
  state.page += 1;
  render();
});

initFilters();
render();
""".strip()


def _overlay_css() -> str:
    return """
:root{color-scheme:light;--ink:#1b1f24;--muted:#57606a;--line:#d0d7de;--bg:#f6f8fa;--accent:#0969da}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink)}
body{font:14px/1.45 -apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}
main{width:min(1180px,calc(100% - 32px));margin:0 auto;padding:28px 0 48px}
h1{margin:0 0 16px;font-size:28px;line-height:1.15}
h2{margin:0;font-size:16px}
.summary,.controls,.pager,.decision{background:#fff;border:1px solid var(--line);border-radius:8px}
.summary,.controls,.pager,.decision{margin:0 0 12px;padding:14px}
.summary dl{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr))}
.summary dl{gap:10px;margin:0 0 8px}
.summary dt{color:var(--muted);font-size:12px;text-transform:uppercase}
.summary dd{margin:2px 0 0;font-weight:650}
.summary p{margin:0;color:var(--muted)}
.controls{display:grid;grid-template-columns:2fr repeat(4,minmax(120px,1fr));gap:10px}
.controls label{display:grid;gap:4px;color:var(--muted);font-size:12px;font-weight:650}
input,select,button{font:inherit}
input,select{width:100%;min-height:34px;border:1px solid var(--line)}
input,select{border-radius:6px;padding:6px 8px}
.pager{display:flex;gap:10px;align-items:center;position:sticky;top:0;z-index:1}
button{min-height:34px;border:1px solid var(--line);border-radius:6px;background:#fff}
button{padding:6px 10px}
button:not(:disabled){cursor:pointer;color:var(--accent)}
button:disabled{color:#8c959f;background:#f6f8fa}
#page-info{font-weight:650}
#result-count{margin-left:auto;color:var(--muted)}
.decision-list{min-height:180px}
.decision header{display:grid;grid-template-columns:1fr;gap:4px;margin-bottom:10px}
.decision p{margin:0;color:var(--muted)}
code{display:block;max-width:100%;overflow:hidden;text-overflow:ellipsis;color:var(--muted)}
table{width:100%;border-collapse:collapse;table-layout:fixed;background:#fff}
th,td{border-top:1px solid var(--line);padding:8px;text-align:left}
th,td{vertical-align:top;word-break:break-word}
th{color:var(--muted);font-size:12px;font-weight:650}
td:nth-child(1){font-weight:700;color:var(--accent)}
@media(max-width:760px){main{width:calc(100% - 20px);padding-top:18px}}
@media(max-width:760px){.summary dl{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media(max-width:760px){.controls{grid-template-columns:1fr 1fr}}
@media(max-width:760px){.controls label:first-child{grid-column:1/-1}}
@media(max-width:760px){.pager{position:static;flex-wrap:wrap}}
@media(max-width:760px){#result-count{width:100%;margin-left:0}}
@media(max-width:760px){th,td{padding:6px;font-size:12px}}
""".strip()
