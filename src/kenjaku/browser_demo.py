from __future__ import annotations

import json
from hashlib import blake2b
from pathlib import Path
from typing import Any

from kenjaku.core import TileType, all_tile_types
from kenjaku.frontend_static import (
    html_document,
    motion_primitives_css,
    motion_primitives_script,
    theme_css,
)
from kenjaku.training.ppo import (
    PPO_ACTION_DIM,
    PPO_ACTION_KIND_OFFSETS,
    PPO_DECISION_TYPES,
    PPO_KYUSHU_ACTION_INDEX,
    PPO_PASS_ACTION_INDEX,
    PPO_RIICHI_ACTION_INDEX,
    PPO_SANDBOX_POLICY_KIND,
    PPO_STATE_DIM,
    PPO_TSUMO_ACTION_INDEX,
    ppo_action_index,
    ppo_legal_action_mask,
    ppo_state_features,
)

BROWSER_DEMO_KIND = "kenjaku-browser-demo-v0"
BROWSER_DEMO_POLICY_KIND = "kenjaku-browser-demo-ppo-policy-v0"
BROWSER_DEMO_MANIFEST_KIND = "kenjaku-browser-demo-manifest-v0"
BROWSER_DEMO_FILES = ("index.html", "styles.css", "demo.js", "policy.json", "manifest.json")
FIXTURE_WALL_SEED = "kenjaku-browser-demo-wall-v0"
DEMO_POLICY_EXPORT_SEED = "kenjaku-browser-demo-ppo-export-v0"
_DRAWN_TILE_FEATURE_OFFSET = 17
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


def browser_demo_policy() -> dict[str, Any]:
    tile_types = tuple(tile.notation for tile in all_tile_types())
    bias = [-0.42 for _action in range(PPO_ACTION_DIM)]
    sparse_weights: list[dict[str, int | float]] = []

    for tile_type in all_tile_types():
        action_index = PPO_ACTION_KIND_OFFSETS["discard"] + tile_type.index
        bias[action_index] = _discard_bias(tile_type)
        sparse_weights.append(
            {
                "action": action_index,
                "feature": _DRAWN_TILE_FEATURE_OFFSET + tile_type.index,
                "value": 0.18,
            }
        )
        sparse_weights.append({"action": action_index, "feature": 6, "value": 0.08})

    bias[PPO_PASS_ACTION_INDEX] = -0.2
    bias[PPO_TSUMO_ACTION_INDEX] = 0.35
    bias[PPO_RIICHI_ACTION_INDEX] = 0.1
    bias[PPO_KYUSHU_ACTION_INDEX] = -0.1

    verification_entry = _demo_policy_verification_entry()
    legal_actions = _discard_actions(verification_entry["state"]["hands"][0])
    logits = browser_demo_policy_logits(verification_entry, legal_actions, bias, sparse_weights)
    selected_action_index = max(range(len(logits)), key=logits.__getitem__)
    return {
        "kind": BROWSER_DEMO_POLICY_KIND,
        "seed": DEMO_POLICY_EXPORT_SEED,
        "description": "Static browser export for the Kenjaku sandbox PPO policy interface.",
        "model": {
            "policy_kind": PPO_SANDBOX_POLICY_KIND,
            "input_dim": PPO_STATE_DIM,
            "hidden_dim": 0,
            "action_dim": PPO_ACTION_DIM,
            "decision_types": list(PPO_DECISION_TYPES),
        },
        "tiles": list(tile_types),
        "actions": {
            "kind_offsets": PPO_ACTION_KIND_OFFSETS,
            "special_indices": {
                "pass": PPO_PASS_ACTION_INDEX,
                "tsumo": PPO_TSUMO_ACTION_INDEX,
                "riichi": PPO_RIICHI_ACTION_INDEX,
                "kyushu": PPO_KYUSHU_ACTION_INDEX,
            },
        },
        "training": {
            "environment": "sandbox",
            "export": "deterministic fixture-safe browser policy",
            "checkpoint": "embedded",
        },
        "model_state": {
            "format": "linear-sparse-v0",
            "bias": bias,
            "weights": sparse_weights,
            "value_bias": 0.0,
            "value_weights": [],
        },
        "verification": {
            "entry": verification_entry,
            "legal_actions": legal_actions,
            "selected_action_index": selected_action_index,
            "selected_action": _action_from_index(selected_action_index),
        },
    }


def browser_demo_policy_logits(
    entry: dict[str, Any],
    legal_actions: list[dict[str, str]],
    bias: list[float] | None = None,
    sparse_weights: list[dict[str, int | float]] | None = None,
) -> list[float]:
    if bias is None or sparse_weights is None:
        payload = browser_demo_policy()
        model_state = payload["model_state"]
        bias = list(model_state["bias"])
        sparse_weights = list(model_state["weights"])
    features = ppo_state_features(entry)
    mask = ppo_legal_action_mask(legal_actions)
    logits = [-1.0e9 for _action in range(PPO_ACTION_DIM)]
    for action, legal in enumerate(mask):
        if legal:
            logits[action] = float(bias[action])
    for weight in sparse_weights:
        action = int(weight["action"])
        if mask[action]:
            logits[action] += float(weight["value"]) * features[int(weight["feature"])]
    return logits


def _discard_bias(tile_type: TileType) -> float:
    if tile_type.is_honor:
        return 0.42
    if tile_type.is_terminal:
        return 0.26
    if tile_type.rank in {2, 8}:
        return 0.08
    if tile_type.rank in {3, 7}:
        return -0.02
    if tile_type.rank in {4, 6}:
        return -0.08
    return -0.16


def _discard_actions(hand: list[str]) -> list[dict[str, str]]:
    return [{"kind": "discard", "tile": tile} for tile in dict.fromkeys(hand)]


def _demo_policy_verification_entry() -> dict[str, Any]:
    return {
        "decision_type": "discard",
        "seat": 0,
        "state": {
            "turn": 1,
            "current_seat": 0,
            "round_wind": "E",
            "dealer_seat": 0,
            "honba": 0,
            "points": [25000, 25000, 25000, 25000],
            "hands": [
                ["1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m", "1p", "1p", "E", "E", "P"],
                [],
                [],
                [],
            ],
            "wall_remaining": len(fixture_wall()) - 1,
            "drawn_tile": "P",
            "needs_discard": True,
            "pending_reaction_seats": [],
        },
    }


def _action_from_index(action_index: int) -> dict[str, str]:
    for kind, offset in PPO_ACTION_KIND_OFFSETS.items():
        if offset <= action_index < offset + 34:
            return {"kind": kind, "tile": TileType(action_index - offset).notation}
    special_actions = {
        PPO_PASS_ACTION_INDEX: "pass",
        PPO_TSUMO_ACTION_INDEX: "tsumo",
        PPO_RIICHI_ACTION_INDEX: "riichi",
        PPO_KYUSHU_ACTION_INDEX: "kyushu",
    }
    return {"kind": special_actions[action_index]}


def write_browser_demo(output_dir: str | Path) -> dict[str, Any]:
    target_dir = Path(output_dir)
    target_dir.mkdir(parents=True, exist_ok=True)
    policy = browser_demo_policy()
    manifest = {
        "kind": BROWSER_DEMO_MANIFEST_KIND,
        "demo_kind": BROWSER_DEMO_KIND,
        "entrypoint": "index.html",
        "assets": list(BROWSER_DEMO_FILES),
        "policy": {
            "kind": policy["kind"],
            "policy_kind": policy["model"]["policy_kind"],
            "input_dim": policy["model"]["input_dim"],
            "action_dim": policy["model"]["action_dim"],
        },
    }
    files = {
        "index.html": _INDEX_HTML,
        "styles.css": _STYLES_CSS,
        "demo.js": _demo_js(policy),
        "policy.json": json.dumps(policy, indent=2, sort_keys=True) + "\n",
        "manifest.json": json.dumps(manifest, indent=2, sort_keys=True) + "\n",
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
    <section class="table-view kj-table-surface" aria-label="Mahjong table">
      <div class="table-hud kj-hud" aria-label="Round status">
        <div class="hud-chip kj-chip">
          <p class="label">Round</p>
          <p id="round-label">East 1</p>
        </div>
        <div class="hud-chip kj-chip">
          <p class="label">Dora</p>
          <div id="dora-tile" class="tile kj-tile tile-honor">P</div>
        </div>
        <div class="hud-chip kj-chip">
          <p class="label">Wall</p>
          <p><span id="wall-count">0</span> tiles</p>
        </div>
        <div class="hud-chip kj-chip">
          <p class="label">Turn</p>
          <p id="turn-label">You</p>
        </div>
        <div class="hud-chip kj-chip">
          <p class="label">Mode</p>
          <p id="mode-label">Autoplay</p>
        </div>
      </div>

      <div class="table-arena">
        <div class="opponents" id="opponents"></div>

        <div class="table-core">
          <div class="table-zone wall-zone">
            <p class="label">Wall Counter</p>
            <strong id="wall-meter">0</strong>
          </div>
          <div class="table-center" aria-label="Hand state">
            <div class="center-stack">
              <p class="label">Terminal Result</p>
              <p id="terminal-result" class="result-text">In progress</p>
              <div class="mode-controls" aria-label="Demo controls">
                <button
                  id="autoplay-button"
                  class="action-button mode-button kj-action-badge kj-motion-lift kj-motion-press"
                  type="button"
                >
                  Autoplay
                </button>
                <button
                  id="user-mode-button"
                  class="action-button mode-button kj-action-badge kj-motion-lift kj-motion-press"
                  type="button"
                >
                  User vs Model
                </button>
                <button
                  id="pause-button"
                  class="action-button mode-button kj-action-badge kj-motion-lift kj-motion-press"
                  type="button"
                >
                  Pause
                </button>
                <button
                  id="step-button"
                  class="action-button mode-button kj-action-badge kj-motion-lift kj-motion-press"
                  type="button"
                >
                  Step
                </button>
              </div>
              <button
                id="restart-button"
                class="action-button kj-action-badge kj-motion-lift kj-motion-press"
                type="button"
              >
                Restart Hand
              </button>
            </div>
          </div>
          <div class="table-zone call-zone">
            <p class="label">Call Zone</p>
            <strong id="call-zone-summary">No open calls</strong>
          </div>
        </div>

        <div class="player-console kj-panel">
          <div class="console-header">
            <div>
              <p class="label">Your Hand</p>
              <strong>Discard rail</strong>
            </div>
            <span id="live-delta" class="score-chip kj-score-chip">+0</span>
          </div>
          <div id="player-hand" class="hand-row"></div>
          <div class="action-strip">
            <p class="label">Legal Actions</p>
            <div id="legal-actions" class="action-rail"></div>
          </div>
        </div>
      </div>
    </section>

    <aside class="side-panel" aria-label="Game log">
      <section class="side-card kj-card">
        <h1>Kenjaku Demo</h1>
        <dl id="scoreboard" class="scoreboard"></dl>
      </section>
      <section class="side-card kj-card policy-panel">
        <h2>Policy</h2>
        <dl class="policy-grid">
          <div class="policy-row">
            <dt>Export</dt>
            <dd id="policy-export">Loading</dd>
          </div>
          <div class="policy-row">
            <dt>Decision</dt>
            <dd id="policy-decision">None</dd>
          </div>
          <div class="policy-row">
            <dt>Confidence</dt>
            <dd id="policy-confidence">0%</dd>
          </div>
        </dl>
      </section>
      <section class="side-card kj-card">
        <h2>Discards</h2>
        <div id="discard-grid" class="discard-grid"></div>
      </section>
      <section class="side-card kj-card">
        <h2>Calls</h2>
        <div id="call-grid" class="call-grid"></div>
      </section>
      <section class="side-card kj-card">
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


_STYLES_CSS = "\n\n".join((theme_css(), motion_primitives_css(), """

* {
  box-sizing: border-box;
}

html {
  background: var(--kj-bg-void);
}

body {
  margin: 0;
  min-height: 100vh;
  background: var(--kj-bg-void);
  color: var(--kj-score-neutral);
  font: 15px/1.45 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

button {
  font: inherit;
}

button:disabled {
  cursor: not-allowed;
}

h1,
h2,
p,
dl,
dd,
ol {
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
  grid-template-columns: minmax(0, 1fr) 320px;
  gap: 16px;
  min-height: 100vh;
  padding: 16px;
}

.table-view {
  position: relative;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  gap: 14px;
  min-height: calc(100vh - 32px);
  overflow: hidden;
  padding: 18px;
}

.table-view::before {
  position: absolute;
  inset: 8px;
  border: 1px dashed rgba(255, 255, 255, 0.14);
  border-radius: 6px;
  content: "";
  pointer-events: none;
}

.table-hud,
.table-arena,
.opponents,
.player-console,
.action-strip {
  display: grid;
  gap: 10px;
}

.table-hud {
  grid-template-columns: repeat(5, minmax(0, 1fr));
  position: relative;
  z-index: 1;
}

.hud-chip {
  justify-content: space-between;
  min-width: 0;
  border-radius: var(--kj-radius-sm);
  background: rgba(8, 10, 18, 0.5);
  color: var(--kj-score-neutral);
}

.hud-chip p:last-child,
.hud-chip span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.table-arena {
  align-content: start;
  grid-template-rows: auto auto auto;
  min-height: 0;
  position: relative;
  z-index: 1;
}

.opponents {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.opponent-seat {
  display: grid;
  gap: 8px;
  min-width: 0;
  border: 1px solid rgba(215, 220, 232, 0.16);
  border-radius: var(--kj-radius-md);
  background: rgba(8, 10, 18, 0.44);
  padding: 10px;
}

.opponent-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.seat-name,
.seat-counter {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.table-core {
  display: grid;
  align-items: center;
  grid-template-columns: minmax(110px, 0.8fr) minmax(180px, 1.2fr) minmax(110px, 0.8fr);
  gap: 12px;
  margin: clamp(16px, 6vh, 74px) 0;
  min-height: 220px;
}

.table-zone,
.table-center {
  min-width: 0;
  border: 1px solid rgba(215, 220, 232, 0.16);
  border-radius: var(--kj-radius-md);
  background: rgba(8, 10, 18, 0.34);
  padding: 12px;
}

.table-zone {
  display: grid;
  gap: 8px;
  min-height: 112px;
  align-content: center;
}

.table-zone strong {
  font-size: 24px;
  line-height: 1.1;
}

.table-center {
  display: grid;
  place-items: center;
  min-height: 180px;
  text-align: center;
}

.center-stack {
  display: grid;
  justify-items: center;
  gap: 12px;
}

.mode-controls {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 6px;
}

.mode-button {
  min-width: 72px;
}

.result-text {
  color: var(--kj-action-strong);
  font-size: 28px;
  font-weight: 900;
  line-height: 1.1;
}

.action-button {
  min-height: 34px;
  cursor: pointer;
}

.action-button.is-active {
  border-color: var(--kj-action-strong);
  color: var(--kj-action-strong);
}

.action-button.is-blocked {
  border-color: rgba(89, 96, 111, 0.66);
  color: var(--kj-disabled);
  filter: grayscale(0.35);
}

.player-console {
  border-color: rgba(215, 220, 232, 0.18);
  background: rgba(13, 17, 29, 0.72);
  padding: 12px;
}

.console-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-width: 0;
}

.console-header strong {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-chip {
  white-space: nowrap;
}

.side-panel {
  display: grid;
  align-content: start;
  gap: 12px;
  min-width: 0;
}

.side-card {
  padding: 12px;
}

.policy-grid {
  display: grid;
  gap: 8px;
  margin-top: 12px;
}

.policy-row {
  display: grid;
  grid-template-columns: 92px 1fr;
  gap: 8px;
  border-top: 1px solid var(--line);
  padding-top: 8px;
}

.policy-row dt {
  color: rgba(215, 220, 232, 0.72);
  font-size: 12px;
  font-weight: 800;
  text-transform: uppercase;
}

.policy-row dd {
  min-width: 0;
  overflow-wrap: anywhere;
}

.label {
  color: rgba(215, 220, 232, 0.72);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0;
  text-transform: uppercase;
}

.hand-row,
.action-rail,
.discard-row,
.call-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
}

.hand-row {
  min-height: 58px;
}

.action-rail {
  min-height: 42px;
  overflow-x: auto;
  padding: 2px 0 4px;
}

.tile {
  width: 36px;
  height: 50px;
  min-width: 36px;
  min-height: 50px;
  font-size: 15px;
  line-height: 1;
}

.tile-button {
  cursor: pointer;
  padding: 0;
}

.tile-button.is-legal {
  border-color: var(--kj-action);
}

.tile-button.is-selected {
  transform: translateY(-4px);
}

.tile-button.is-blocked,
.tile-button:disabled {
  opacity: 0.52;
  transform: none;
}

.tile-man {
  color: var(--kj-tile-man);
}

.tile-pin {
  color: var(--kj-tile-pin);
}

.tile-sou {
  color: var(--kj-tile-sou);
}

.tile-honor {
  color: var(--kj-tile-honor);
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
  align-items: center;
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
  color: var(--kj-card-muted);
}

@media (max-width: 980px) {
  .demo-shell {
    grid-template-columns: 1fr;
  }

  .table-view {
    min-height: auto;
  }
}

@media (max-width: 680px) {
  .demo-shell {
    gap: 10px;
    padding: 10px;
  }

  .table-view {
    padding: 12px;
  }

  .table-hud,
  .opponents {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .table-core {
    grid-template-columns: 1fr;
    margin: 10px 0;
    min-height: 0;
  }

  .table-zone {
    min-height: 76px;
  }

  .table-zone strong {
    font-size: 21px;
  }

  .table-center {
    min-height: 124px;
  }

  .result-text {
    font-size: 23px;
  }

  .console-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .side-panel {
    gap: 10px;
  }
}
"""))


_DEMO_JS = motion_primitives_script() + "\n\n" + """"use strict";

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
    window.KenjakuMotion?.shake(".table-center");
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
  window.KenjakuMotion?.confirm(".player-console");
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
  text("wall-meter", state.wall.length);
  text("turn-label", state.terminal ? "Terminal" : SEATS[state.currentSeat]);
  text("terminal-result", state.terminal || "In progress");
  setScoreText("live-delta", scoreDeltaLabel());
  text("call-zone-summary", callSummaryLabel());
  document.getElementById("dora-tile").textContent = DORA_INDICATOR;
}

function renderOpponents() {
  const container = document.getElementById("opponents");
  container.replaceChildren(...[1, 2, 3].map((seat) => {
    const panel = document.createElement("section");
    panel.className = "opponent-seat kj-card";
    panel.innerHTML = `
      <div class="opponent-meta">
        <strong class="seat-name">${SEATS[seat]}</strong>
        <span class="seat-counter">${state.hands[seat].length} tiles</span>
      </div>
      <p class="label">Score</p>
      <p class="score-chip kj-score-chip">${state.scores[seat].toLocaleString()}</p>
    `;
    return panel;
  }));
}

function renderHand() {
  const container = document.getElementById("player-hand");
  container.replaceChildren(...state.hands[0].map((tile, index) => {
    const legal = state.currentSeat === 0 && !state.terminal;
    const selected = legal && index === state.hands[0].length - 1 && state.drawnTile === tile;
    const button = document.createElement("button");
    button.type = "button";
    button.className = [
      "tile",
      "tile-button",
      "kj-tile",
      "kj-motion-lift",
      "kj-motion-press",
      tileClass(tile),
      legal ? "is-legal" : "is-blocked",
      selected ? "is-selected kj-motion-selected-pulse" : "",
    ].filter(Boolean).join(" ");
    button.textContent = tile;
    button.disabled = !legal;
    button.addEventListener("click", () => discardTile(index));
    return button;
  }));
}

function renderActions() {
  const container = document.getElementById("legal-actions");
  container.replaceChildren();
  if (state.terminal) {
    container.appendChild(actionNode("Hand complete", { disabled: true, blocked: true }));
    return;
  }
  if (state.currentSeat !== 0) {
    container.appendChild(
      actionNode(`${SEATS[state.currentSeat]} is acting`, { disabled: true, blocked: true })
    );
    return;
  }
  state.hands[0].forEach((tile, index) => {
    const button = actionNode(`Discard ${tile}`, { legal: true });
    button.addEventListener("click", () => discardTile(index));
    container.appendChild(button);
  });
}

function renderScores() {
  const scoreboard = document.getElementById("scoreboard");
  scoreboard.replaceChildren(...SEATS.map((seatName, seat) => {
    const row = document.createElement("div");
    row.className = "score-row";
    row.innerHTML = `
      <dt class="seat-name">${seatName}</dt>
      <dd class="score-chip kj-score-chip">${state.scores[seat].toLocaleString()}</dd>
    `;
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
  node.className = `tile kj-tile ${tileClass(tile)}`;
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

function actionNode(label, options = {}) {
  const button = document.createElement("button");
  button.type = "button";
  button.className = [
    "action-button",
    "kj-action-badge",
    "kj-motion-lift",
    "kj-motion-press",
    options.legal ? "is-legal" : "",
    options.blocked ? "is-blocked" : "",
  ].filter(Boolean).join(" ");
  button.textContent = label;
  button.disabled = Boolean(options.disabled);
  if (options.disabled) {
    button.setAttribute("aria-disabled", "true");
  }
  return button;
}

function scoreDeltaLabel() {
  const delta = state.scores[0] - INITIAL_SCORES[0];
  return `${delta >= 0 ? "+" : ""}${delta.toLocaleString()}`;
}

function setScoreText(id, value) {
  const element = document.getElementById(id);
  if (window.KenjakuMotion) {
    window.KenjakuMotion.countUp(element, value);
    return;
  }
  element.textContent = value;
}

function callSummaryLabel() {
  const calls = state.calls.reduce((count, seatCalls) => count + seatCalls.length, 0);
  return calls === 0 ? "No open calls" : `${calls} open calls`;
}

document.getElementById("restart-button").addEventListener("click", startHand);
window.KenjakuDemo = { startHand, state };
startHand();
""".replace("__FIXTURE_WALL_JSON__", json.dumps(list(fixture_wall())))
