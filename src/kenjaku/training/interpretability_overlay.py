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
from kenjaku.frontend_static import html_document, motion_primitives_css, static_base_css, theme_css
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
    title = str(report.get("title", "Kenjaku Interpretability Overlay"))
    escaped_title = escape(title)
    decisions = report.get("decisions")
    decision_items = decisions if isinstance(decisions, list) else []
    payload = _overlay_json_payload(report, decision_items)
    body = f"""
  <header>
    <p class="eyebrow">Kenjaku local</p>
    <h1>{escaped_title}</h1>
  </header>
  <main>
    <section class="summary">
{_summary_html(report)}
    </section>
{_controls_html()}
    <section id="decision-list" class="decision-list" aria-live="polite"></section>
    <script id="overlay-data" type="application/json">{payload}</script>
  </main>
"""
    return html_document(
        title=title,
        body_html=body,
        inline_css=(static_base_css(), theme_css(), motion_primitives_css(), _overlay_css()),
        inline_script=_overlay_js(),
        body_class="interpretability-overlay kj-arcade-shell",
    )


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
        None if current_shanten is None or next_shanten is None else next_shanten - current_shanten
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
    return _is_decision_snapshot(value) and value.get("decision_type") == "discard"


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
            f"{escape(str(label))}={count}" for label, count in sorted(source_labels.items())
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
            '<label>Round<select id="round-filter"></select></label>',
            '<label>Seat<select id="seat-filter"></select></label>',
            '<label>Discard<select id="tile-filter"></select></label>',
            '<label>Shanten<select id="shanten-filter"></select></label>',
            "</section>",
            '<section class="signal-tabs" aria-label="Signal focus">',
            '<button type="button" data-mode="all" class="is-active">All</button>',
            '<button type="button" data-mode="risk">Risk</button>',
            '<button type="button" data-mode="efficiency">Efficiency</button>',
            '<button type="button" data-mode="points">Points</button>',
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
const state = {page: 1, filtered: decisions.slice(), mode: "all"};
const controls = {
  search: document.getElementById("search"),
  round: document.getElementById("round-filter"),
  seat: document.getElementById("seat-filter"),
  tile: document.getElementById("tile-filter"),
  shanten: document.getElementById("shanten-filter"),
  modeButtons: Array.from(document.querySelectorAll("[data-mode]")),
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
  controls.list.dataset.mode = state.mode;
  controls.pageInfo.textContent = `Page ${state.page} of ${pages}`;
  controls.resultCount.textContent = `${state.filtered.length} matching decisions`;
  controls.prev.disabled = state.page <= 1;
  controls.next.disabled = state.page >= pages;
}

function renderDecision(decision) {
  const alternatives = Array.isArray(decision.top_alternatives)
    ? decision.top_alternatives
    : [];
  const cards = alternatives.length
    ? alternatives.map((alternative, index) => renderAlternative(alternative, index)).join("")
    : '<p class="empty">No alternatives</p>';
  const dora = Array.isArray(decision.dora_indicators)
    ? decision.dora_indicators.join(" ")
    : "";
  const observedClass = shantenClass(decision.actual_shanten_delta);
  return `<section class="decision">
    <header class="decision-head">
      <div>
        <h2>Decision ${Number(decision.index || 0) + 1}</h2>
        <p>
          round=${escapeHtml(field(decision.round_index))}
          event=${escapeHtml(field(decision.event_index))}
          seat=${escapeHtml(field(decision.seat))}
        </p>
      </div>
      <div class="observed ${observedClass}">
        <span>Observed discard</span>
        <strong>${escapeHtml(field(decision.actual_discard)) || "n/a"}</strong>
        <em>delta ${formatDelta(decision.actual_shanten_delta)}</em>
      </div>
    </header>
    <div class="context-grid">
      <p><span>Hand</span>${escapeHtml(field(decision.hand_pattern))}</p>
      <p><span>Dora</span>${escapeHtml(dora || "none")}</p>
      <code>${escapeHtml(field(decision.row_id))}</code>
    </div>
    <div class="alternative-grid">${cards}</div>
  </section>`;
}

function renderAlternative(alternative, index) {
  const reasons = Array.isArray(alternative.risk_reasons)
    ? alternative.risk_reasons.join(", ")
    : "";
  const risk = Number(alternative.estimated_deal_in_risk);
  const points = Number(alternative.expected_point_impact);
  const probability = formatPercent(alternative.policy_probability);
  const riskText = formatPercent(risk);
  const pointsText = formatPoints(points);
  const deltaText = formatDelta(alternative.shanten_delta);
  const classes = [
    "alternative-card",
    `rank-${index + 1}`,
    riskClass(risk),
    shantenClass(alternative.shanten_delta),
    pointsClass(points)
  ].join(" ");
  return `<article class="${classes}">
    <div class="rank">#${index + 1}</div>
    <div class="tile">${escapeHtml(field(alternative.tile))}</div>
    <div class="signals">
      <span class="signal signal-policy">
        <span>Policy probability</span><strong>${probability}</strong>
      </span>
      <span class="signal signal-efficiency">
        <span>Shanten delta</span><strong>${deltaText}</strong>
      </span>
      <span class="signal signal-risk">
        <span>Deal-in risk</span><strong>${riskText}</strong>
      </span>
      <span class="signal signal-points">
        <span>Expected point impact</span><strong>${pointsText}</strong>
      </span>
    </div>
    <p class="reasoning">${escapeHtml(reasons || "no risk reason")}</p>
  </article>`;
}

function riskClass(value) {
  if (!Number.isFinite(value)) {
    return "risk-unknown";
  }
  if (value >= 0.34) {
    return "risk-high";
  }
  if (value >= 0.12) {
    return "risk-mid";
  }
  return "risk-low";
}

function shantenClass(value) {
  const number = Number(value);
  if (!Number.isFinite(number)) {
    return "efficiency-unknown";
  }
  if (number < 0) {
    return "efficiency-improves";
  }
  if (number > 0) {
    return "efficiency-worsens";
  }
  return "efficiency-same";
}

function pointsClass(value) {
  if (!Number.isFinite(value)) {
    return "points-unknown";
  }
  if (value > 0) {
    return "points-positive";
  }
  if (value < 0) {
    return "points-negative";
  }
  return "points-neutral";
}

function formatDelta(value) {
  const number = Number(value);
  if (!Number.isFinite(number)) {
    return "unknown";
  }
  return number > 0 ? `+${number}` : String(number);
}

function formatPercent(value) {
  return Number.isFinite(value) ? `${(value * 100).toFixed(1)}%` : "";
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
for (const button of controls.modeButtons) {
  button.addEventListener("click", () => {
    state.mode = button.dataset.mode || "all";
    for (const item of controls.modeButtons) {
      item.classList.toggle("is-active", item === button);
    }
    render();
  });
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
:root {
  color-scheme: dark;
  --bg: var(--kj-bg-void);
  --text: var(--kj-score-neutral);
  --muted: rgba(215, 220, 232, 0.72);
  --line: rgba(215, 220, 232, 0.16);
  --panel: rgba(13, 17, 29, 0.72);
  --panel-soft: rgba(8, 10, 18, 0.38);
  --accent: var(--kj-action);
  --risk: var(--kj-score-negative);
  --efficiency: var(--kj-score-positive);
  --points: var(--kj-chip-gold);
}
body {
  margin: 0;
  background: var(--bg);
  color: var(--text);
  font: 14px/1.45 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}
header, main {
  width: min(1180px, calc(100% - 32px));
  margin: 0 auto;
}
body > header { padding: 34px 0 12px; }
h1 { margin: 0; font-size: 32px; line-height: 1.15; letter-spacing: 0; }
h2 { margin: 0; font-size: 17px; letter-spacing: 0; }
p { margin: 0; }
.eyebrow {
  margin-bottom: 8px;
  color: var(--accent);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0;
  text-transform: uppercase;
}
.summary, .controls, .signal-tabs, .pager {
  margin: 0 0 12px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-lg);
  background: var(--panel);
  box-shadow: var(--kj-shadow-hard);
}
.summary dl {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 10px;
  margin: 0 0 8px;
}
.summary dt {
  color: var(--muted);
  font-size: 12px;
  font-weight: 800;
  text-transform: uppercase;
}
.summary dd { margin: 2px 0 0; font-weight: 800; }
.summary p { color: var(--muted); }
.controls {
  display: grid;
  grid-template-columns: 2fr repeat(4, minmax(120px, 1fr));
  gap: 10px;
}
.controls label {
  display: grid;
  gap: 4px;
  color: var(--muted);
  font-size: 12px;
  font-weight: 800;
}
input, select, button { font: inherit; }
input, select {
  width: 100%;
  min-height: 34px;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-sm);
  background: var(--panel-soft);
  color: var(--text);
  padding: 6px 8px;
}
.signal-tabs, .pager {
  display: flex;
  gap: 8px;
  align-items: center;
}
.signal-tabs { flex-wrap: wrap; }
.pager {
  position: sticky;
  top: 0;
  z-index: 1;
}
button {
  min-height: 34px;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-sm);
  background: var(--panel-soft);
  color: var(--text);
  padding: 6px 10px;
}
button:not(:disabled) { cursor: pointer; }
button:not(:disabled):hover, .signal-tabs button.is-active {
  border-color: rgba(103, 214, 255, 0.58);
  color: var(--accent);
  filter: brightness(1.1);
}
button:disabled {
  color: var(--kj-disabled);
  cursor: not-allowed;
}
#page-info { font-weight: 800; }
#result-count { margin-left: auto; color: var(--muted); }
.decision-list {
  min-height: 180px;
  max-height: min(76vh, 920px);
  overflow-y: auto;
  padding-right: 4px;
}
.decision {
  margin: 0 0 18px;
  padding: 16px 0 20px;
  border-top: 1px solid var(--line);
}
.decision-head {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 12px;
  align-items: start;
  margin-bottom: 10px;
}
.decision-head p { color: var(--muted); }
.observed {
  display: grid;
  gap: 2px;
  min-width: 132px;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-md);
  background: var(--panel-soft);
  padding: 8px 10px;
  text-align: right;
}
.observed span, .observed em {
  color: var(--muted);
  font-size: 12px;
  font-style: normal;
}
.observed strong { font-size: 22px; line-height: 1; }
.context-grid {
  display: grid;
  grid-template-columns: 1.4fr .6fr minmax(160px, .9fr);
  gap: 8px;
  margin-bottom: 12px;
}
.context-grid p, code {
  min-width: 0;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-sm);
  background: var(--panel-soft);
  color: var(--muted);
  padding: 8px;
  overflow-wrap: anywhere;
}
.context-grid span {
  display: block;
  color: var(--text);
  font-weight: 800;
}
.alternative-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 10px;
}
.alternative-card {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 8px 10px;
  min-height: 190px;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-md);
  background: var(--panel);
  box-shadow: var(--kj-shadow-hard);
  padding: 12px;
}
.rank {
  grid-row: span 2;
  color: var(--points);
  font-size: 12px;
  font-weight: 900;
}
.tile {
  color: var(--text);
  font-size: 28px;
  font-weight: 900;
  line-height: 1;
}
.signals {
  grid-column: 1 / -1;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 7px;
}
.signal {
  display: grid;
  gap: 2px;
  border: 1px solid var(--line);
  border-radius: var(--kj-radius-sm);
  background: var(--panel-soft);
  padding: 7px;
}
.signal span {
  color: var(--muted);
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
}
.signal strong { font-size: 16px; }
.signal-policy strong { color: var(--accent); }
.signal-risk strong { color: var(--risk); }
.signal-efficiency strong { color: var(--efficiency); }
.signal-points strong { color: var(--points); }
.reasoning {
  grid-column: 1 / -1;
  color: var(--muted);
  overflow-wrap: anywhere;
}
.rank-1 { border-color: rgba(255, 200, 87, 0.58); }
.risk-low .signal-risk strong { color: var(--kj-success); }
.risk-mid .signal-risk strong { color: var(--kj-warning); }
.risk-high .signal-risk strong { color: var(--kj-score-negative); }
.efficiency-improves .signal-efficiency strong,
.efficiency-improves.observed strong { color: var(--kj-success); }
.efficiency-same .signal-efficiency strong,
.efficiency-same.observed strong { color: var(--accent); }
.efficiency-worsens .signal-efficiency strong,
.efficiency-worsens.observed strong { color: var(--kj-score-negative); }
.points-positive .signal-points strong { color: var(--kj-success); }
.points-negative .signal-points strong { color: var(--kj-score-negative); }
.decision-list[data-mode="risk"] .signal:not(.signal-risk),
.decision-list[data-mode="efficiency"] .signal:not(.signal-efficiency),
.decision-list[data-mode="points"] .signal:not(.signal-points) {
  opacity: .48;
}
.empty {
  color: var(--muted);
  padding: 12px;
}
@media (max-width: 760px) {
  header, main { width: min(100% - 20px, 1180px); }
  body > header { padding-top: 22px; }
  h1 { font-size: 27px; }
  .summary dl { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .controls { grid-template-columns: 1fr 1fr; }
  .controls label:first-child { grid-column: 1 / -1; }
  .pager { position: static; flex-wrap: wrap; }
  #result-count { width: 100%; margin-left: 0; }
  .decision-head, .context-grid { grid-template-columns: 1fr; }
  .observed { text-align: left; }
  .signals { grid-template-columns: 1fr; }
}
""".strip()
