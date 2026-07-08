from __future__ import annotations

import json
from collections.abc import Iterable
from html import escape
from pathlib import Path
from typing import Any

from kenjaku.frontend_static import (
    html_document,
    motion_primitives_css,
    motion_primitives_script,
    static_base_css,
    theme_css,
)

SELF_PLAY_TRAJECTORY_ROW_KIND = "kenjaku-self-play-match-trajectory-row-v0"


def write_self_play_match_trajectory_jsonl(
    path: Path,
    report: dict[str, Any],
) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with path.open("w", encoding="utf-8") as handle:
        for row in iter_self_play_match_trajectory_rows(report):
            count += 1
            handle.write(json.dumps(row, sort_keys=True) + "\n")
    return count


def iter_self_play_match_trajectory_rows(
    report: dict[str, Any],
) -> Iterable[dict[str, Any]]:
    games = report.get("game_summaries")
    if not isinstance(games, list):
        return
    for game in games:
        if not isinstance(game, dict):
            continue
        trajectory = game.get("trajectory")
        if not isinstance(trajectory, list):
            continue
        for index, entry in enumerate(trajectory):
            if not isinstance(entry, dict):
                continue
            yield {
                "kind": SELF_PLAY_TRAJECTORY_ROW_KIND,
                "game": game.get("game_index"),
                "game_seed": game.get("seed"),
                "ruleset": report.get("ruleset"),
                "players": report.get("players"),
                "row_index": index,
                **entry,
            }


def read_self_play_trajectory_jsonl(path: Path) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    with path.open(encoding="utf-8") as handle:
        for line_number, line in enumerate(handle, start=1):
            if not line.strip():
                continue
            payload = json.loads(line)
            if not isinstance(payload, dict):
                raise ValueError(f"trajectory row {line_number} must be a JSON object")
            if payload.get("kind") not in {SELF_PLAY_TRAJECTORY_ROW_KIND, None}:
                raise ValueError(f"not a self-play trajectory row: line {line_number}")
            rows.append(payload)
    if not rows:
        raise ValueError("trajectory JSONL has no rows")
    return rows


def write_self_play_replay_viewer_html(
    path: Path,
    rows: list[dict[str, Any]],
    *,
    title: str = "Kenjaku Self-Play Replay Viewer",
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(format_self_play_replay_viewer_html(rows, title=title), encoding="utf-8")


def format_self_play_replay_viewer_html(
    rows: list[dict[str, Any]],
    *,
    title: str = "Kenjaku Self-Play Replay Viewer",
) -> str:
    payload = _json_script_payload({"rows": rows})
    safe_title = escape(title, quote=True)
    body = f"""
  <main class="replay-shell">
    <header class="replay-header">
      <h1>{safe_title}</h1>
      <p id="summary" class="summary-chip kj-score-chip"></p>
    </header>
    <section class="toolbar kj-panel" aria-label="Timeline controls">
      <input id="timeline" type="range" min="0" value="0">
      <output id="turn-label" class="turn-label kj-chip"></output>
      <div id="hand-toggles" class="hand-toggles"></div>
    </section>
    <section class="timeline-layout">
      <aside class="turn-timeline kj-card" aria-label="Turn timeline">
        <h2>Turn Timeline</h2>
        <ol id="event-window" class="event-window"></ol>
      </aside>
      <section class="annotation kj-card" aria-label="Selected event">
        <h2 id="action-title"></h2>
        <p id="action-detail"></p>
      </section>
    </section>
    <section id="board" class="board seat-grid" aria-label="Seat HUD"></section>
  </main>
  <script id="replay-data" type="application/json">{payload}</script>
"""
    return html_document(
        title=title,
        body_html=body,
        inline_css=(static_base_css(), theme_css(), motion_primitives_css(), _viewer_css()),
        inline_script="\n\n".join((motion_primitives_script(), _viewer_js())),
        body_class="replay-page kj-arcade-shell",
    )


def _json_script_payload(payload: dict[str, Any]) -> str:
    return (
        json.dumps(payload, sort_keys=True, separators=(",", ":"))
        .replace("&", "\\u0026")
        .replace("<", "\\u003c")
        .replace(">", "\\u003e")
    )


def _viewer_css() -> str:
    return """
.replay-shell{width:min(1180px,calc(100% - 32px));margin:0 auto;padding:24px 0 44px}
.replay-header{display:flex;align-items:flex-end;justify-content:space-between;gap:16px;margin-bottom:14px}
h1,h2,h3,p,ol{margin:0}
h1{font-size:30px;line-height:1.1}
h2{font-size:17px}
.summary-chip,.turn-label{white-space:nowrap}
.toolbar{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:12px}
.toolbar{align-items:center;padding:12px}
#timeline{width:100%;accent-color:var(--kj-action)}
.hand-toggles{grid-column:1/-1;display:flex;gap:8px;flex-wrap:wrap}
.hand-toggles label{display:inline-flex;gap:6px;align-items:center}
.hand-toggles label{border:1px solid rgba(103,214,255,.4)}
.hand-toggles label{border-radius:var(--kj-radius-sm);padding:5px 8px}
.hand-toggles label{color:var(--kj-action);font-weight:800}
.timeline-layout{display:grid;grid-template-columns:300px minmax(0,1fr);gap:12px;margin:12px 0}
.turn-timeline,.annotation,.seat{padding:12px}
.event-window{display:grid;gap:6px;margin-top:10px;padding:0;list-style:none}
.event-window button{width:100%;min-height:34px;text-align:left}
.event-window button.is-selected{border-color:var(--kj-action-strong)}
.event-window button.is-selected{color:var(--kj-action-strong);box-shadow:var(--kj-shadow-glow)}
.annotation{display:grid;align-content:start;gap:10px;min-height:138px}
.annotation h2{color:var(--kj-action-strong);font-size:24px}
.annotation p{color:var(--kj-score-neutral)}
.seat-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:12px}
.seat{display:grid;gap:10px;min-width:0}
.seat.current{border-color:var(--kj-action);box-shadow:var(--kj-shadow-glow)}
.seat h3{display:flex;align-items:center;justify-content:space-between;gap:8px;font-size:16px}
.tile-row{display:flex;flex-wrap:wrap;gap:4px;min-height:34px}
.tile{width:30px;height:38px;min-width:30px;min-height:38px;font-size:13px}
.tile.back{background:var(--kj-surface-raised);color:var(--kj-disabled)}
.tile-man{color:var(--kj-tile-man)}
.tile-pin{color:var(--kj-tile-pin)}
.tile-sou{color:var(--kj-tile-sou)}
.tile-honor{color:var(--kj-tile-honor)}
.meta{color:var(--kj-card-muted);font-size:12px}
@media(max-width:820px){
  .replay-shell{width:calc(100% - 20px);padding-top:14px}
  .replay-header{align-items:flex-start;flex-direction:column}
  .toolbar,.timeline-layout{grid-template-columns:1fr}
}
""".strip()


def _viewer_js() -> str:
    return r"""
const data = JSON.parse(document.getElementById("replay-data").textContent);
const rows = Array.isArray(data.rows) ? data.rows : [];
const state = {index: 0, revealed: new Set()};
const timeline = document.getElementById("timeline");
const turnLabel = document.getElementById("turn-label");
const board = document.getElementById("board");
const summary = document.getElementById("summary");
const actionTitle = document.getElementById("action-title");
const actionDetail = document.getElementById("action-detail");
const handToggles = document.getElementById("hand-toggles");
const eventWindow = document.getElementById("event-window");

timeline.max = Math.max(0, rows.length - 1);
summary.textContent = `${rows.length} trajectory rows`;
initHandToggles();
timeline.addEventListener("input", () => {
  state.index = Number(timeline.value);
  render();
});
render();
window.KenjakuReplayViewer = {rows, render};

function initHandToggles() {
  const players = playerCount(rows[0]);
  handToggles.replaceChildren(...Array.from({length: players}, (_, seat) => {
    const label = document.createElement("label");
    const input = document.createElement("input");
    input.type = "checkbox";
    input.addEventListener("input", () => {
      if (input.checked) {
        state.revealed.add(seat);
      } else {
        state.revealed.delete(seat);
      }
      render();
    });
    label.append(input, `Reveal P${seat}`);
    return label;
  }));
}

function render() {
  const row = rows[state.index] || {};
  const currentState = row.state || {};
  const action = row.chosen_action || {};
  timeline.value = String(state.index);
  turnLabel.textContent = `Turn ${state.index + 1} / ${rows.length}`;
  actionTitle.textContent = `${field(row.decision_type)} by seat ${field(row.seat)}`;
  actionDetail.textContent = [
    `game=${field(row.game)}`,
    `round=${field(row.round)}`,
    `step=${field(row.step)}`,
    `action=${formatAction(action)}`,
    `legal=${Array.isArray(row.legal_actions) ? row.legal_actions.length : 0}`
  ].join(" | ");
  renderEventWindow();
  board.replaceChildren(...Array.from({length: playerCount(row)}, (_, seat) => {
    return renderSeat(currentState, seat);
  }));
}

function renderEventWindow() {
  const radius = 6;
  const start = Math.max(0, state.index - radius);
  const end = Math.min(rows.length, state.index + radius + 1);
  eventWindow.replaceChildren(...rows.slice(start, end).map((row, offset) => {
    const index = start + offset;
    const action = row.chosen_action || {};
    const item = document.createElement("li");
    const button = document.createElement("button");
    button.type = "button";
    button.className = [
      "kj-action-badge",
      "kj-motion-lift",
      index === state.index ? "is-selected" : "",
    ].filter(Boolean).join(" ");
    button.textContent = `${index + 1}. S${field(row.seat)} ${formatAction(action)}`;
    button.addEventListener("click", () => {
      state.index = index;
      render();
    });
    item.appendChild(button);
    return item;
  }));
}

function renderSeat(currentState, seat) {
  const section = document.createElement("section");
  const currentSeat = Number(currentState.current_seat);
  section.className = seat === currentSeat ? "seat kj-card current" : "seat kj-card";
  const points = Array.isArray(currentState.points) ? currentState.points[seat] : "";
  section.innerHTML = (
    `<h3><span>Seat ${seat}</span><span class="kj-score-chip">${field(points)}</span></h3>`
  );
  section.append(
    tileBlock("Hand", handTiles(currentState, seat)),
    tileBlock("Discards", nestedTiles(currentState.discards, seat)),
    metaBlock(currentState, seat)
  );
  return section;
}

function tileBlock(label, tiles) {
  const wrap = document.createElement("div");
  const title = document.createElement("div");
  title.className = "meta";
  title.textContent = label;
  const row = document.createElement("div");
  row.className = "tile-row";
  row.replaceChildren(...tiles.map(tile => tileElement(tile)));
  wrap.append(title, row);
  return wrap;
}

function handTiles(currentState, seat) {
  const hands = Array.isArray(currentState.hands) ? currentState.hands : [];
  const hand = Array.isArray(hands[seat]) ? hands[seat] : [];
  if (state.revealed.has(seat)) {
    return hand;
  }
  const sizes = Array.isArray(currentState.hand_sizes) ? currentState.hand_sizes : [];
  return Array.from({length: Number(sizes[seat] || hand.length || 0)}, () => "?");
}

function nestedTiles(value, seat) {
  return Array.isArray(value) && Array.isArray(value[seat]) ? value[seat] : [];
}

function tileElement(tile) {
  const span = document.createElement("span");
  span.className = tile === "?" ? "tile kj-tile back" : `tile kj-tile ${tileClass(tile)}`;
  span.textContent = field(tile);
  return span;
}

function tileClass(tile) {
  const value = String(tile);
  if (value.endsWith("m")) {
    return "tile-man";
  }
  if (value.endsWith("p")) {
    return "tile-pin";
  }
  if (value.endsWith("s")) {
    return "tile-sou";
  }
  return "tile-honor";
}

function metaBlock(currentState, seat) {
  const div = document.createElement("p");
  div.className = "meta";
  const pending = Array.isArray(currentState.pending_reaction_seats)
    ? currentState.pending_reaction_seats.join(",")
    : "";
  div.textContent = [
    `wall=${field(currentState.wall_remaining)}`,
    `dealer=${field(currentState.dealer_seat)}`,
    `pending=${pending || "none"}`,
    `rewards=${rewardText(seat)}`
  ].join(" | ");
  return div;
}

function rewardText(seat) {
  const row = rows[state.index] || {};
  return Array.isArray(row.rewards) ? field(row.rewards[seat]) : "";
}

function playerCount(row) {
  if (row && Number.isInteger(row.players)) {
    return row.players;
  }
  const currentState = row?.state || {};
  if (Array.isArray(currentState.points)) {
    return currentState.points.length;
  }
  return 4;
}

function formatAction(action) {
  const tile = action.tile ? ` ${action.tile}` : "";
  return `${field(action.kind)}${tile}`;
}

function field(value) {
  return value === null || value === undefined ? "" : String(value);
}
""".strip()
