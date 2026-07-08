from __future__ import annotations

import json
from hashlib import blake2b
from pathlib import Path
from typing import Any

from kenjaku.frontend_static import html_document

BROWSER_DEMO_KIND = "kenjaku-browser-demo-v0"
BROWSER_DEMO_FILES = ("index.html", "styles.css", "demo.js")
FIXTURE_WALL_SEED = "kenjaku-browser-demo-wall-v0"
_FIXTURE_WALL_POOL = (
    "5p",
    "9s",
    "4m",
    "N",
    "6s",
    "1p",
    "8m",
    "P",
    "7p",
    "4s",
    "2m",
    "C",
    "9p",
    "5s",
    "1m",
    "F",
)


def fixture_wall(seed: str = FIXTURE_WALL_SEED) -> tuple[str, ...]:
    keyed_tiles = []
    for index, tile in enumerate(_FIXTURE_WALL_POOL):
        key = blake2b(f"{seed}:{index}:{tile}".encode(), digest_size=8).hexdigest()
        keyed_tiles.append((key, tile))
    return tuple(tile for _key, tile in sorted(keyed_tiles))


def write_browser_demo(output_dir: str | Path) -> dict[str, Any]:
    target_dir = Path(output_dir)
    target_dir.mkdir(parents=True, exist_ok=True)
    files = {
        "index.html": _INDEX_HTML,
        "styles.css": _STYLES_CSS,
        "demo.js": _DEMO_JS,
    }
    for filename, contents in files.items():
        (target_dir / filename).write_text(contents, encoding="utf-8")
    return {
        "kind": BROWSER_DEMO_KIND,
        "output_dir": str(target_dir),
        "entrypoint": str(target_dir / "index.html"),
        "files": [str(target_dir / filename) for filename in BROWSER_DEMO_FILES],
    }


_INDEX_BODY_HTML = """
  <main class="demo-shell">
    <section class="table-view" aria-label="Mahjong table">
      <div class="table-status">
        <div>
          <p class="label">Round</p>
          <p id="round-label">East 1</p>
        </div>
        <div>
          <p class="label">Dora</p>
          <div id="dora-tile" class="tile tile-honor">P</div>
        </div>
        <div>
          <p class="label">Wall</p>
          <p><span id="wall-count">0</span> tiles</p>
        </div>
        <div>
          <p class="label">Turn</p>
          <p id="turn-label">You</p>
        </div>
      </div>

      <div class="opponents" id="opponents"></div>

      <div class="center-lane">
        <div>
          <p class="label">Terminal Result</p>
          <p id="terminal-result">In progress</p>
        </div>
        <button id="restart-button" type="button">Restart Hand</button>
      </div>

      <div class="player-area">
        <div>
          <p class="label">Your Hand</p>
          <div id="player-hand" class="hand-row"></div>
        </div>
        <div>
          <p class="label">Legal Actions</p>
          <div id="legal-actions" class="action-row"></div>
        </div>
      </div>
    </section>

    <aside class="side-panel" aria-label="Game log">
      <section>
        <h1>Kenjaku Demo</h1>
        <dl id="scoreboard" class="scoreboard"></dl>
      </section>
      <section>
        <h2>Discards</h2>
        <div id="discard-grid" class="discard-grid"></div>
      </section>
      <section>
        <h2>Calls</h2>
        <div id="call-grid" class="call-grid"></div>
      </section>
      <section>
        <h2>Log</h2>
        <ol id="event-log" class="event-log"></ol>
      </section>
    </aside>
  </main>
"""


_INDEX_HTML = html_document(
    title="Kenjaku Browser Demo",
    body_html=_INDEX_BODY_HTML,
    stylesheets=("styles.css",),
    scripts=("demo.js",),
)


_STYLES_CSS = """* {
  box-sizing: border-box;
}

:root {
  color-scheme: light;
  --felt: #0f6a4f;
  --felt-dark: #084534;
  --surface: #f7f7f3;
  --panel: #ffffff;
  --ink: #17202a;
  --muted: #60707f;
  --line: #d8ded8;
  --pin: #bb2d3b;
  --sou: #157347;
  --man: #1d4ed8;
  --honor: #6f42c1;
  --accent: #c77800;
}

body {
  margin: 0;
  min-height: 100vh;
  background: #e9ece6;
  color: var(--ink);
  font: 15px/1.45 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

button {
  border: 0;
  border-radius: 6px;
  background: var(--accent);
  color: #ffffff;
  cursor: pointer;
  font: inherit;
  font-weight: 700;
  padding: 9px 12px;
}

button:disabled {
  cursor: default;
  opacity: 0.45;
}

h1,
h2,
p {
  margin: 0;
}

h1 {
  font-size: 24px;
}

h2 {
  font-size: 16px;
}

.demo-shell {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 340px;
  gap: 18px;
  min-height: 100vh;
  padding: 18px;
}

.table-view {
  display: grid;
  grid-template-rows: auto auto 1fr auto;
  gap: 16px;
  min-height: calc(100vh - 36px);
  border-radius: 8px;
  background: var(--felt);
  box-shadow: inset 0 0 0 6px var(--felt-dark);
  color: #ffffff;
  padding: 22px;
}

.table-status,
.opponents,
.player-area,
.center-lane {
  display: grid;
  gap: 12px;
}

.table-status {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.table-status > div,
.center-lane,
.player-area {
  border: 1px solid rgba(255, 255, 255, 0.24);
  border-radius: 8px;
  background: rgba(0, 0, 0, 0.14);
  padding: 12px;
}

.opponents {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.opponent-seat {
  border-radius: 8px;
  background: rgba(0, 0, 0, 0.18);
  padding: 12px;
}

.opponent-meta {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.center-lane {
  align-items: center;
  grid-template-columns: 1fr auto;
  min-height: 120px;
}

.player-area {
  grid-template-columns: 1fr;
}

.side-panel {
  display: grid;
  align-content: start;
  gap: 12px;
}

.side-panel section {
  border: 1px solid var(--line);
  border-radius: 8px;
  background: var(--panel);
  padding: 14px;
}

.label {
  color: rgba(255, 255, 255, 0.72);
  font-size: 12px;
  font-weight: 800;
  text-transform: uppercase;
}

.side-panel .label {
  color: var(--muted);
}

.hand-row,
.action-row,
.discard-row,
.call-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
}

.tile {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 50px;
  border: 1px solid #c6c9c2;
  border-radius: 5px;
  background: var(--surface);
  box-shadow: 0 2px 0 #c9cbc5;
  color: var(--ink);
  font-weight: 800;
}

.tile-button {
  padding: 0;
}

.tile-man {
  color: var(--man);
}

.tile-pin {
  color: var(--pin);
}

.tile-sou {
  color: var(--sou);
}

.tile-honor {
  color: var(--honor);
}

.scoreboard {
  display: grid;
  gap: 8px;
  margin: 12px 0 0;
}

.score-row,
.discard-seat,
.call-seat {
  display: grid;
  gap: 6px;
  border-top: 1px solid var(--line);
  padding-top: 8px;
}

.score-row {
  grid-template-columns: 1fr auto;
}

.discard-grid,
.call-grid,
.event-log {
  display: grid;
  gap: 10px;
  margin: 12px 0 0;
  padding: 0;
}

.event-log {
  list-style-position: inside;
  max-height: 260px;
  overflow: auto;
}

.event-log li {
  border-top: 1px solid var(--line);
  padding-top: 8px;
}

.muted {
  color: var(--muted);
}

@media (max-width: 920px) {
  .demo-shell {
    grid-template-columns: 1fr;
  }

  .table-view {
    min-height: auto;
  }
}

@media (max-width: 640px) {
  .demo-shell {
    padding: 10px;
  }

  .table-status,
  .opponents {
    grid-template-columns: 1fr;
  }

  .center-lane {
    grid-template-columns: 1fr;
  }
}
"""


_DEMO_JS = """"use strict";

const SEATS = ["You", "Shimocha", "Toimen", "Kamicha"];
const INITIAL_SCORES = [25000, 25000, 25000, 25000];
const DORA_INDICATOR = "P";
const INITIAL_HANDS = [
  ["1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m", "1p", "1p", "E", "E"],
  ["2p", "3p", "4p", "6p", "7p", "8p", "2s", "3s", "4s", "S", "S", "W", "W"],
  ["1s", "1s", "2s", "5s", "6s", "7s", "3m", "3m", "7m", "8m", "9m", "F", "F"],
  ["2m", "2m", "5m", "6m", "7m", "3p", "3p", "5p", "6p", "7p", "C", "C", "N"],
];
const FIXTURE_WALL = __FIXTURE_WALL_JSON__;

const state = {
  hands: [],
  wall: [],
  discards: [[], [], [], []],
  calls: [[], [], [], []],
  scores: [...INITIAL_SCORES],
  currentSeat: 0,
  drawnTile: null,
  turn: 1,
  terminal: null,
  log: [],
};

function cloneHands() {
  return INITIAL_HANDS.map((hand) => [...hand]);
}

function startHand() {
  state.hands = cloneHands();
  state.wall = [...FIXTURE_WALL];
  state.discards = [[], [], [], []];
  state.calls = [[], [], [], []];
  state.scores = [...INITIAL_SCORES];
  state.currentSeat = 0;
  state.drawnTile = null;
  state.turn = 1;
  state.terminal = null;
  state.log = ["East 1 begins."];
  drawForCurrentSeat();
  render();
}

function drawForCurrentSeat() {
  if (state.terminal) {
    return;
  }
  if (state.wall.length === 0) {
    finishExhaustiveDraw();
    return;
  }
  const tile = state.wall.shift();
  state.drawnTile = tile;
  state.hands[state.currentSeat].push(tile);
  state.log.unshift(`${SEATS[state.currentSeat]} draws.`);
  if (state.currentSeat !== 0) {
    window.setTimeout(botDiscard, 260);
  }
}

function botDiscard() {
  if (state.terminal || state.currentSeat === 0) {
    return;
  }
  const hand = state.hands[state.currentSeat];
  const discardIndex = chooseBotDiscard(hand);
  discardTile(discardIndex);
}

function chooseBotDiscard(hand) {
  const honorIndex = hand.findIndex((tile) => !tile.endsWith("m")
    && !tile.endsWith("p")
    && !tile.endsWith("s"));
  if (honorIndex >= 0) {
    return honorIndex;
  }
  return hand.length - 1;
}

function discardTile(index) {
  if (state.terminal) {
    return;
  }
  const hand = state.hands[state.currentSeat];
  const [tile] = hand.splice(index, 1);
  state.discards[state.currentSeat].push(tile);
  state.drawnTile = null;
  state.log.unshift(`${SEATS[state.currentSeat]} discards ${tile}.`);
  state.currentSeat = (state.currentSeat + 1) % SEATS.length;
  if (state.currentSeat === 0) {
    state.turn += 1;
  }
  drawForCurrentSeat();
  render();
}

function finishExhaustiveDraw() {
  state.terminal = "Exhaustive draw";
  const tenpaiSeats = [0, 2];
  state.scores = state.scores.map((score, seat) => (
    tenpaiSeats.includes(seat) ? score + 1500 : score - 1500
  ));
  state.log.unshift("The wall is exhausted. Tenpai payments are applied.");
}

function render() {
  renderStatus();
  renderOpponents();
  renderHand();
  renderActions();
  renderScores();
  renderDiscards();
  renderCalls();
  renderLog();
}

function renderStatus() {
  text("wall-count", state.wall.length);
  text("turn-label", state.terminal ? "Terminal" : SEATS[state.currentSeat]);
  text("terminal-result", state.terminal || "In progress");
  document.getElementById("dora-tile").textContent = DORA_INDICATOR;
}

function renderOpponents() {
  const container = document.getElementById("opponents");
  container.replaceChildren(...[1, 2, 3].map((seat) => {
    const panel = document.createElement("section");
    panel.className = "opponent-seat";
    panel.innerHTML = `
      <div class="opponent-meta">
        <strong>${SEATS[seat]}</strong>
        <span>${state.hands[seat].length} tiles</span>
      </div>
      <p class="label">Score</p>
      <p>${state.scores[seat].toLocaleString()}</p>
    `;
    return panel;
  }));
}

function renderHand() {
  const container = document.getElementById("player-hand");
  container.replaceChildren(...state.hands[0].map((tile, index) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `tile tile-button ${tileClass(tile)}`;
    button.textContent = tile;
    button.disabled = state.currentSeat !== 0 || Boolean(state.terminal);
    button.addEventListener("click", () => discardTile(index));
    return button;
  }));
}

function renderActions() {
  const container = document.getElementById("legal-actions");
  container.replaceChildren();
  if (state.terminal) {
    container.appendChild(textNode("Hand complete"));
    return;
  }
  if (state.currentSeat !== 0) {
    container.appendChild(textNode(`${SEATS[state.currentSeat]} is acting`));
    return;
  }
  state.hands[0].forEach((tile, index) => {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = `Discard ${tile}`;
    button.addEventListener("click", () => discardTile(index));
    container.appendChild(button);
  });
}

function renderScores() {
  const scoreboard = document.getElementById("scoreboard");
  scoreboard.replaceChildren(...SEATS.map((seatName, seat) => {
    const row = document.createElement("div");
    row.className = "score-row";
    row.innerHTML = `<dt>${seatName}</dt><dd>${state.scores[seat].toLocaleString()}</dd>`;
    return row;
  }));
}

function renderDiscards() {
  const grid = document.getElementById("discard-grid");
  grid.replaceChildren(...SEATS.map((seatName, seat) => {
    const section = document.createElement("section");
    section.className = "discard-seat";
    section.innerHTML = `<strong>${seatName}</strong>`;
    const row = document.createElement("div");
    row.className = "discard-row";
    row.replaceChildren(...state.discards[seat].map(renderTile));
    section.appendChild(row);
    return section;
  }));
}

function renderCalls() {
  const grid = document.getElementById("call-grid");
  grid.replaceChildren(...SEATS.map((seatName, seat) => {
    const section = document.createElement("section");
    section.className = "call-seat";
    section.innerHTML = `<strong>${seatName}</strong>`;
    const row = document.createElement("div");
    row.className = "call-row";
    if (state.calls[seat].length === 0) {
      const empty = document.createElement("span");
      empty.className = "muted";
      empty.textContent = "None";
      row.appendChild(empty);
    } else {
      row.replaceChildren(...state.calls[seat].map(renderTile));
    }
    section.appendChild(row);
    return section;
  }));
}

function renderLog() {
  const log = document.getElementById("event-log");
  log.replaceChildren(...state.log.slice(0, 12).map((entry) => {
    const item = document.createElement("li");
    item.textContent = entry;
    return item;
  }));
}

function renderTile(tile) {
  const node = document.createElement("span");
  node.className = `tile ${tileClass(tile)}`;
  node.textContent = tile;
  return node;
}

function tileClass(tile) {
  if (tile.endsWith("m")) {
    return "tile-man";
  }
  if (tile.endsWith("p")) {
    return "tile-pin";
  }
  if (tile.endsWith("s")) {
    return "tile-sou";
  }
  return "tile-honor";
}

function text(id, value) {
  document.getElementById(id).textContent = String(value);
}

function textNode(value) {
  const span = document.createElement("span");
  span.textContent = value;
  return span;
}

document.getElementById("restart-button").addEventListener("click", startHand);
window.KenjakuDemo = { startHand, state };
startHand();
""".replace("__FIXTURE_WALL_JSON__", json.dumps(list(fixture_wall())))
