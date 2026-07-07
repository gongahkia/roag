from __future__ import annotations

import json
from collections.abc import Iterable
from html import escape
from pathlib import Path
from typing import Any

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
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" href="data:,">
  <title>{safe_title}</title>
  <style>
{_viewer_css()}
  </style>
</head>
<body>
  <main>
    <header>
      <h1>{safe_title}</h1>
      <p id="summary"></p>
    </header>
    <section class="toolbar" aria-label="Timeline controls">
      <input id="timeline" type="range" min="0" value="0">
      <output id="turn-label"></output>
      <div id="hand-toggles" class="hand-toggles"></div>
    </section>
    <section class="annotation" aria-label="Action annotation">
      <h2 id="action-title"></h2>
      <p id="action-detail"></p>
    </section>
    <section id="board" class="board" aria-label="Tile board"></section>
  </main>
  <script id="replay-data" type="application/json">{payload}</script>
  <script>
{_viewer_js()}
  </script>
</body>
</html>
"""


def _json_script_payload(payload: dict[str, Any]) -> str:
    return (
        json.dumps(payload, sort_keys=True, separators=(",", ":"))
        .replace("&", "\\u0026")
        .replace("<", "\\u003c")
        .replace(">", "\\u003e")
    )


def _viewer_css() -> str:
    return """
:root{color-scheme:light;--bg:#f6f8fa;--panel:#fff;--ink:#17202a;--muted:#5d6875;--line:#d9dee5;--accent:#0f766e}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink)}
body{font:14px/1.45 -apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}
main{width:min(1180px,calc(100% - 32px));margin:0 auto;padding:28px 0 48px}
h1{margin:0 0 6px;font-size:28px;line-height:1.15}
h2{margin:0 0 6px;font-size:18px}
p{margin:0;color:var(--muted)}
.toolbar,.annotation,.seat{background:var(--panel);border:1px solid var(--line);border-radius:8px}
.toolbar,.annotation{margin:14px 0;padding:14px}
.toolbar{display:grid;grid-template-columns:1fr auto;gap:12px;align-items:center}
#timeline{width:100%}
.hand-toggles{grid-column:1/-1;display:flex;gap:10px;flex-wrap:wrap}
.hand-toggles label{display:flex;gap:6px;align-items:center;color:var(--muted)}
.board{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:12px}
.seat{padding:12px}
.seat.current{border-color:var(--accent);box-shadow:0 0 0 2px rgba(15,118,110,.12)}
.seat h3{display:flex;justify-content:space-between;gap:8px;margin:0 0 8px;font-size:16px}
.tile-row{display:flex;flex-wrap:wrap;gap:4px;margin:8px 0 10px;min-height:30px}
.tile{display:inline-flex;align-items:center;justify-content:center;min-width:26px;height:30px}
.tile{border:1px solid #b7c0c8;border-radius:4px;background:#fff;font-weight:700}
.tile.back{background:#dfe6ed;color:#7b8794}
.meta{color:var(--muted);font-size:12px}
@media(max-width:720px){main{width:calc(100% - 20px);padding-top:18px}}
@media(max-width:720px){.toolbar{grid-template-columns:1fr}}
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
  board.replaceChildren(...Array.from({length: playerCount(row)}, (_, seat) => {
    return renderSeat(currentState, seat);
  }));
}

function renderSeat(currentState, seat) {
  const section = document.createElement("section");
  const currentSeat = Number(currentState.current_seat);
  section.className = seat === currentSeat ? "seat current" : "seat";
  const points = Array.isArray(currentState.points) ? currentState.points[seat] : "";
  section.innerHTML = `<h3><span>Player ${seat}</span><span>${field(points)}</span></h3>`;
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
  span.className = tile === "?" ? "tile back" : "tile";
  span.textContent = field(tile);
  return span;
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
