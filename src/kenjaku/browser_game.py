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
)
from kenjaku.training.ppo_schema import (
    PPO_ACTION_DIM,
    PPO_ACTION_KIND_OFFSETS,
    PPO_DECISION_TYPES,
    PPO_KYUSHU_ACTION_INDEX,
    PPO_PASS_ACTION_INDEX,
    PPO_RIICHI_ACTION_INDEX,
    PPO_SANDBOX_POLICY_KIND,
    PPO_STATE_DIM,
    PPO_TSUMO_ACTION_INDEX,
    ppo_legal_action_mask,
    ppo_state_features,
)

BROWSER_GAME_KIND = "kenjaku-browser-game-v0"
BROWSER_GAME_POLICY_KIND = "kenjaku-browser-game-ppo-policy-v0"
BROWSER_GAME_MANIFEST_KIND = "kenjaku-browser-game-manifest-v0"
BROWSER_GAME_FILES = ("index.html", "styles.css", "game.js", "policy.json", "manifest.json")
FIXTURE_WALL_SEED = "kenjaku-browser-game-wall-v0"
GAME_POLICY_EXPORT_SEED = "kenjaku-browser-game-ppo-export-v0"
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


def browser_game_policy() -> dict[str, Any]:
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

    verification_entry = _game_policy_verification_entry()
    legal_actions = _discard_actions(verification_entry["state"]["hands"][0])
    logits = browser_game_policy_logits(verification_entry, legal_actions, bias, sparse_weights)
    selected_action_index = max(range(len(logits)), key=logits.__getitem__)
    return {
        "kind": BROWSER_GAME_POLICY_KIND,
        "seed": GAME_POLICY_EXPORT_SEED,
        "description": "Static local gameplay surface for the Kenjaku sandbox PPO policy interface.",
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


def browser_game_policy_logits(
    entry: dict[str, Any],
    legal_actions: list[dict[str, str]],
    bias: list[float] | None = None,
    sparse_weights: list[dict[str, int | float]] | None = None,
) -> list[float]:
    if bias is None or sparse_weights is None:
        payload = browser_game_policy()
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


def _game_policy_verification_entry() -> dict[str, Any]:
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


def write_browser_game(output_dir: str | Path) -> dict[str, Any]:
    target_dir = Path(output_dir)
    target_dir.mkdir(parents=True, exist_ok=True)
    policy = browser_game_policy()
    manifest = {
        "kind": BROWSER_GAME_MANIFEST_KIND,
        "game_kind": BROWSER_GAME_KIND,
        "entrypoint": "index.html",
        "assets": list(BROWSER_GAME_FILES),
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
        "game.js": _game_js(policy),
        "policy.json": json.dumps(policy, indent=2, sort_keys=True) + "\n",
        "manifest.json": json.dumps(manifest, indent=2, sort_keys=True) + "\n",
    }
    for filename, contents in files.items():
        (target_dir / filename).write_text(contents, encoding="utf-8")
    return {
        "kind": BROWSER_GAME_KIND,
        "output_dir": str(target_dir),
        "entrypoint": str(target_dir / "index.html"),
        "files": [str(target_dir / filename) for filename in BROWSER_GAME_FILES],
    }


_INDEX_BODY_HTML = """
  <main class="game-shell">
    <section class="table-view kj-table-surface" aria-label="Mahjong table">
      <canvas id="ascii-field" class="ascii-field" aria-hidden="true"></canvas>
      <header class="table-identity">
        <p class="terminal-kicker">Kenjaku / local policy table</p>
        <p class="terminal-serial">Fixture hand / one-bit simulation</p>
      </header>
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
          <p id="mode-label">User mode</p>
        </div>
      </div>

      <div class="table-arena">
        <div class="opponents" id="opponents"></div>

        <div class="table-core">
          <div class="table-zone wall-zone">
            <p class="label">Wall Counter</p>
            <strong id="wall-meter">0</strong>
          </div>
          <div
            id="discard-target"
            class="table-center discard-target"
            aria-label="Hand state and discard target"
          >
            <div class="discard-target-copy" aria-hidden="true">
              <span>Discard vector</span>
              <strong id="target-preview">Drag a tile to commit</strong>
            </div>
            <div class="center-stack">
              <p class="label">Terminal Result</p>
              <p id="terminal-result" class="result-text">In progress</p>
              <p id="turn-vector" class="turn-vector" aria-live="polite">You → Shimocha</p>
              <div class="mode-controls" aria-label="Gameplay controls">
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
                <button
                  id="motion-button"
                  class="action-button mode-button kj-action-badge kj-motion-lift kj-motion-press"
                  type="button"
                  aria-pressed="true"
                >
                  Motion: On
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
          <div
            id="player-hand"
            class="hand-row"
            role="group"
            aria-label="Your playable tiles"
          ></div>
          <div class="action-strip">
            <p class="label">Legal Actions</p>
            <div id="legal-actions" class="action-rail"></div>
          </div>
          <p id="control-hint" class="control-hint">← → select · Enter discard · drag to table</p>
          <p id="interaction-status" class="sr-only" aria-live="polite"></p>
        </div>
      </div>
    </section>

    <aside class="side-panel" aria-label="Game log">
      <section class="side-card kj-card">
        <h1>Kenjaku</h1>
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
    title="Kenjaku",
    body_html=_INDEX_BODY_HTML,
    stylesheets=("styles.css",),
    scripts=("game.js",),
)


_STYLES_CSS = "\n\n".join((motion_primitives_css(), """

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

.game-shell {
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

.hud-chip .label {
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
  .game-shell {
    grid-template-columns: 1fr;
  }

  .table-view {
    min-height: auto;
  }
}

@media (max-width: 680px) {
  .game-shell {
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
""", """
:root {
  color-scheme: dark;
  --ob-void: #090b08;
  --ob-ink: #f1eddb;
  --ob-ash: #b7b4a5;
  --ob-smoke: #77776d;
  --ob-line: #73736a;
  --ob-panel: rgba(15, 17, 13, 0.9);
  --ob-raised: rgba(29, 30, 23, 0.96);
  --kj-motion-fast: 90ms;
  --kj-motion-normal: 140ms;
  --kj-motion-slow: 220ms;
  --kj-ease-snap: cubic-bezier(.2, .9, .25, 1);
}

html { background: var(--ob-void); }

body {
  margin: 0;
  min-width: 320px;
  min-height: 100vh;
  background:
    linear-gradient(90deg, rgba(241, 237, 219, 0.028) 1px, transparent 1px),
    linear-gradient(rgba(241, 237, 219, 0.028) 1px, transparent 1px),
    var(--ob-void);
  background-size: 8px 8px;
  color: var(--ob-ink);
  font: 14px/1.45 ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

body::before {
  position: fixed;
  z-index: 8;
  inset: 0;
  background: repeating-linear-gradient(
    0deg, rgba(241, 237, 219, 0.022) 0 1px, transparent 1px 4px
  );
  content: "";
  mix-blend-mode: screen;
  pointer-events: none;
}

button { font: inherit; }

button:disabled { cursor: not-allowed; }

h1,
h2,
p,
dl,
dd,
ol { margin: 0; }

h1,
h2,
strong { letter-spacing: -0.045em; }

h1 { font-size: clamp(1.8rem, 3vw, 2.7rem); line-height: 0.9; }

h2 { font-size: 0.9rem; text-transform: uppercase; }

.game-shell {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 306px;
  gap: 1rem;
  min-height: 100vh;
  padding: 1rem;
}

.table-view {
  position: relative;
  isolation: isolate;
  display: grid;
  grid-template-rows: auto auto minmax(0, 1fr);
  gap: 0.85rem;
  min-height: calc(100vh - 2rem);
  overflow: hidden;
  border: 1px solid var(--ob-ink);
  border-radius: 0;
  background: linear-gradient(135deg, rgba(241, 237, 219, 0.11), transparent 55%), var(--ob-void);
  box-shadow: 6px 6px 0 rgba(241, 237, 219, 0.11);
  padding: 1rem;
}

.table-view::before {
  position: absolute;
  z-index: 0;
  inset: 0.45rem;
  border: 1px dashed rgba(241, 237, 219, 0.28);
  border-radius: 0;
  content: "";
  pointer-events: none;
}

.table-view > :not(.ascii-field) { position: relative; z-index: 1; }

.ascii-field {
  position: absolute;
  z-index: 0;
  inset: 0;
  display: block;
  width: 100%;
  height: 100%;
  opacity: 0.78;
  image-rendering: pixelated;
  pointer-events: none;
}

.table-identity {
  display: flex;
  gap: 0.8rem;
  align-items: baseline;
  justify-content: space-between;
  border-bottom: 1px solid var(--ob-line);
  padding: 0 0 0.7rem;
}

.terminal-kicker,
.label {
  color: var(--ob-ash);
  font-size: 0.62rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.terminal-kicker { color: var(--ob-ink); }

.terminal-serial {
  color: var(--ob-smoke);
  font-size: 0.62rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.table-hud,
.table-arena,
.opponents,
.player-console,
.action-strip { display: grid; gap: 0.7rem; }

.table-hud { grid-template-columns: repeat(5, minmax(0, 1fr)); }

.hud-chip {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 0.35rem 0.6rem;
  align-items: baseline;
  min-width: 0;
  border: 1px solid var(--ob-line);
  border-radius: 0;
  background: rgba(9, 11, 8, 0.96);
  color: var(--ob-ink);
  padding: 0.55rem 0.65rem;
}

.hud-chip .label { white-space: nowrap; }

.hud-chip p:last-child,
.hud-chip span {
  overflow: hidden;
  font-size: 0.75rem;
  font-weight: 700;
  text-align: right;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hud-chip .tile {
  justify-self: end;
  width: 1.8rem;
  min-width: 1.8rem;
  height: 1.8rem;
  min-height: 1.8rem;
}

.table-arena { align-content: start; grid-template-rows: auto auto auto; min-height: 0; }

.opponents { grid-template-columns: repeat(3, minmax(0, 1fr)); }

.opponent-seat,
.side-card {
  min-width: 0;
  border: 1px solid var(--ob-line);
  border-radius: 0;
  background: rgba(15, 17, 13, 0.96);
  box-shadow: none;
  padding: 0.75rem;
}

.opponent-seat { display: grid; gap: 0.55rem; }

.opponent-meta { display: flex; gap: 0.5rem; align-items: center; justify-content: space-between; }

.seat-name,
.seat-counter { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.seat-name { font-size: 0.78rem; }

.seat-counter { color: var(--ob-ash); font-size: 0.65rem; }

.table-core {
  display: grid;
  grid-template-columns: minmax(120px, 0.8fr) minmax(210px, 1.25fr) minmax(120px, 0.8fr);
  gap: 0.75rem;
  align-items: stretch;
  margin: clamp(1rem, 4vh, 3.5rem) 0;
  min-height: 210px;
}

.table-zone,
.table-center {
  min-width: 0;
  border: 1px solid var(--ob-line);
  border-radius: 0;
  background: rgba(9, 11, 8, 0.94);
  padding: 0.8rem;
}

.table-zone { display: grid; gap: 0.5rem; align-content: center; min-height: 110px; }

.table-zone strong { font-size: clamp(1rem, 2.2vw, 1.65rem); line-height: 1.05; }

.table-center {
  position: relative;
  display: grid;
  min-height: 180px;
  place-items: center;
  overflow: hidden;
  border-color: var(--ob-ink);
  text-align: center;
}

.table-center::before,
.table-center::after {
  position: absolute;
  color: var(--ob-smoke);
  content: "+";
  font-size: 1.25rem;
}

.table-center::before { top: 0.55rem; left: 0.7rem; }

.table-center::after { right: 0.7rem; bottom: 0.55rem; }

.center-stack { position: relative; z-index: 1; display: grid; justify-items: center; gap: 0.8rem; }

.mode-controls { display: flex; flex-wrap: wrap; justify-content: center; gap: 0.4rem; }

.mode-button { min-width: 72px; }

.result-text {
  color: var(--ob-ink);
  font-size: clamp(1.3rem, 3vw, 2.4rem);
  font-weight: 800;
  line-height: 0.9;
}

.action-button {
  min-height: 32px;
  border: 1px solid var(--ob-line);
  border-radius: 0;
  background: rgba(241, 237, 219, 0.055);
  color: var(--ob-ink);
  cursor: pointer;
  font-size: 0.67rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  padding: 0.45rem 0.6rem;
  text-transform: uppercase;
}

.action-button:hover { border-color: var(--ob-ink); background: rgba(241, 237, 219, 0.15); }

.action-button.is-active {
  border-color: var(--ob-ink);
  background: var(--ob-ink);
  color: var(--ob-void);
}

.action-button.is-blocked,
.action-button:disabled {
  border-color: var(--ob-smoke);
  color: var(--ob-smoke);
  filter: none;
  opacity: 0.58;
  transform: none;
}

.player-console {
  border: 1px solid var(--ob-ink);
  border-radius: 0;
  background: rgba(9, 11, 8, 0.96);
  padding: 0.8rem;
}

.console-header {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  justify-content: space-between;
  min-width: 0;
}

.console-header strong {
  display: block;
  overflow: hidden;
  font-size: 1rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score-chip {
  display: inline-grid;
  min-width: 0;
  border: 1px solid var(--ob-line);
  border-radius: 0;
  background: transparent;
  color: var(--ob-ink);
  font-size: 0.7rem;
  font-weight: 700;
  padding: 0.28rem 0.45rem;
  text-align: center;
  white-space: nowrap;
}

.hand-row,
.action-rail,
.discard-row,
.call-row { display: flex; flex-wrap: wrap; gap: 0.38rem; margin-top: 0.55rem; }

.hand-row { min-height: 3.5rem; }

.action-rail { min-height: 2.5rem; overflow-x: auto; padding: 0.1rem 0 0.3rem; }

.tile {
  display: grid;
  width: 2rem;
  min-width: 2rem;
  height: 2.7rem;
  min-height: 2.7rem;
  place-items: center;
  border: 1px solid var(--ob-ink);
  border-radius: 0;
  background: var(--ob-ink);
  color: var(--ob-void);
  font-size: 0.72rem;
  font-weight: 800;
  line-height: 1;
  box-shadow: 2px 2px 0 var(--ob-smoke);
}

.tile-button { cursor: pointer; padding: 0; }

.tile-button.is-legal { border-color: var(--ob-ink); }

.tile-button.is-selected {
  box-shadow: 3px 3px 0 var(--ob-ink), -2px -2px 0 var(--ob-smoke);
  transform: translateY(-3px);
}

.tile-button.is-blocked,
.tile-button:disabled { opacity: 0.5; transform: none; }

.tile-man,
.tile-pin,
.tile-sou,
.tile-honor { color: var(--ob-void); }

.side-panel { display: grid; align-content: start; gap: 0.75rem; min-width: 0; }

.side-card h1,
.side-card h2 { border-bottom: 1px solid var(--ob-line); padding-bottom: 0.6rem; }

.policy-grid,
.scoreboard { display: grid; gap: 0.6rem; margin-top: 0.75rem; }

.policy-row,
.score-row,
.discard-seat,
.call-seat {
  display: grid;
  gap: 0.5rem;
  border-top: 1px dashed var(--ob-line);
  padding-top: 0.55rem;
}

.score-row { grid-template-columns: 1fr auto; align-items: center; }

.policy-row { grid-template-columns: minmax(66px, 0.7fr) minmax(0, 1.3fr); }

.policy-row dt {
  color: var(--ob-smoke);
  font-size: 0.61rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.policy-row dd { min-width: 0; overflow-wrap: anywhere; font-size: 0.68rem; }

.discard-grid,
.call-grid,
.event-log { display: grid; gap: 0.7rem; margin: 0.75rem 0 0; padding: 0; }

.event-log { list-style-position: inside; max-height: 260px; overflow: auto; }

.event-log li {
  border-top: 1px dashed var(--ob-line);
  color: var(--ob-ash);
  font-size: 0.68rem;
  padding-top: 0.55rem;
}

.muted { color: var(--ob-smoke); font-size: 0.68rem; }

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  margin: -1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
  white-space: nowrap;
}

.discard-target { isolation: isolate; }

.discard-target-copy {
  position: absolute;
  z-index: 0;
  right: 0.7rem;
  bottom: 0.55rem;
  display: grid;
  gap: 0.12rem;
  color: var(--ob-smoke);
  font-size: 0.56rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-align: right;
  text-transform: uppercase;
  transition: color 140ms ease-out, opacity 140ms ease-out;
}

.discard-target-copy strong { color: var(--ob-ash); font-size: 0.64rem; }

.discard-target.is-drag-over {
  border-color: var(--ob-ink);
  box-shadow: inset 0 0 0 2px var(--ob-ink), 0 0 0 4px rgba(241, 237, 219, 0.12);
}

.discard-target.is-drag-over .discard-target-copy,
.discard-target.has-intent .discard-target-copy { color: var(--ob-ink); opacity: 1; }

.discard-target.is-impact {
  animation: game-target-impact 460ms cubic-bezier(.16, .84, .28, 1) both;
}

.center-stack { z-index: 1; }

.turn-vector {
  min-height: 1.1rem;
  border-top: 1px dashed var(--ob-line);
  border-bottom: 1px dashed var(--ob-line);
  color: var(--ob-ash);
  font-size: 0.62rem;
  font-weight: 800;
  letter-spacing: 0.09em;
  padding: 0.28rem 0.55rem;
  text-transform: uppercase;
}

.hand-row {
  align-items: flex-end;
  min-width: 0;
  padding: 0.5rem 0.25rem 0.7rem;
}

.tile-button {
  position: relative;
  transition: transform 140ms cubic-bezier(.16, .84, .28, 1), opacity 140ms ease-out,
    box-shadow 140ms ease-out, filter 140ms ease-out;
  will-change: transform, opacity;
}

.hand-row:hover .tile-button:not(:hover):not(:focus-visible) {
  opacity: 0.72;
  transform: translateY(3px);
}

.tile-button:hover:not(:disabled),
.tile-button:focus-visible { transform: translateY(-11px) rotate(-1.5deg) scale(1.05); }

.tile-button.is-selected:not(:hover):not(:focus-visible) { transform: translateY(-5px); }

.tile-button.is-dragging {
  filter: brightness(1.22);
  opacity: 0.22;
  transform: translateY(-13px) scale(1.08);
}

.control-hint {
  color: var(--ob-smoke);
  font-size: 0.6rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  margin-top: 0.2rem;
  text-transform: uppercase;
}

.table-view.game-impact-heavy {
  animation: game-impact-heavy 440ms cubic-bezier(.18, .72, .2, 1) both;
}

.table-view.game-impact-light {
  animation: game-impact-light 300ms cubic-bezier(.18, .72, .2, 1) both;
}

.tile-flight {
  position: fixed;
  z-index: 90;
  margin: 0;
  pointer-events: none;
  transform-origin: center;
  will-change: transform, filter, opacity;
}

.tile-vector {
  position: fixed;
  z-index: 89;
  height: 0;
  border-top: 1px solid var(--ob-ink);
  box-shadow: 0 0 8px rgba(241, 237, 219, 0.45);
  opacity: 0.86;
  pointer-events: none;
  transform-origin: left center;
}

.tile-vector::after {
  position: absolute;
  top: -4px;
  right: -1px;
  width: 7px;
  height: 7px;
  border-top: 1px solid var(--ob-ink);
  border-right: 1px solid var(--ob-ink);
  content: "";
  transform: rotate(45deg);
}

@keyframes game-impact-heavy {
  0%, 100% { transform: translate3d(0, 0, 0); filter: brightness(1); }
  14% { transform: translate3d(-9px, 3px, 0) rotate(-.18deg); filter: brightness(1.14); }
  28% { transform: translate3d(11px, -4px, 0) rotate(.16deg); }
  43% { transform: translate3d(-7px, 2px, 0) rotate(-.1deg); }
  60% { transform: translate3d(5px, -1px, 0); }
  78% { transform: translate3d(-2px, 1px, 0); }
}

@keyframes game-impact-light {
  0%, 100% { transform: translate3d(0, 0, 0); }
  30% { transform: translate3d(-5px, 1px, 0); }
  60% { transform: translate3d(5px, -1px, 0); }
}

@keyframes game-target-impact {
  0% { box-shadow: inset 0 0 0 0 var(--ob-ink); }
  32% { box-shadow: inset 0 0 0 3px var(--ob-ink), 0 0 0 6px rgba(241, 237, 219, 0.18); }
  100% { box-shadow: inset 0 0 0 0 var(--ob-ink); }
}

body.game-motion-off .kj-motion-lift:hover,
body.game-motion-off .tile-button:hover:not(:disabled),
body.game-motion-off .tile-button:focus-visible,
body.game-motion-off .tile-button.is-selected:not(:hover):not(:focus-visible) {
  filter: none;
  transform: none;
}

body.game-motion-off .tile-button { transition: none; }

button:focus-visible,
[tabindex]:not([tabindex="-1"]):focus-visible { outline: 2px solid #fff; outline-offset: 3px; }

@media (prefers-contrast: more) {
  :root { --ob-line: #f1eddb; --ob-ash: #f1eddb; --ob-smoke: #d8d4c4; }
  body::before { display: none; }
  .table-view,
  .opponent-seat,
  .side-card,
  .table-zone,
  .table-center,
  .player-console { border-width: 2px; }
}

@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation: none !important;
    scroll-behavior: auto !important;
    transition: none !important;
  }
}

@media (max-width: 980px) {
  .game-shell { grid-template-columns: 1fr; }
  .table-view { min-height: auto; }
  .side-panel { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .side-card:first-child { grid-column: 1 / -1; }
}

@media (max-width: 680px) {
  .game-shell { gap: 0.75rem; padding: 0.75rem; }
  .table-view { padding: 0.75rem; }
  .table-identity { display: grid; gap: 0.35rem; }
  .table-hud,
  .opponents { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .table-core { grid-template-columns: 1fr; margin: 0.75rem 0; min-height: 0; }
  .table-zone { min-height: 74px; }
  .table-center { min-height: 150px; }
  .side-panel { grid-template-columns: 1fr; }
  .side-card:first-child { grid-column: auto; }
  .console-header { align-items: flex-start; flex-direction: column; }
}
"""))


_GAME_JS_TEMPLATE = motion_primitives_script() + "\n\n" + """"use strict";

const POLICY = __POLICY_JSON__;
const SEATS = ["You", "Shimocha", "Toimen", "Kamicha"];
const INITIAL_SCORES = [25000, 25000, 25000, 25000];
const DORA_INDICATOR = "P";
const MODE_AUTOPLAY = "autoplay";
const MODE_USER = "user";
const INITIAL_HANDS = [
  ["1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m", "1p", "1p", "E", "E"],
  ["2p", "3p", "4p", "6p", "7p", "8p", "2s", "3s", "4s", "S", "S", "W", "W"],
  ["1s", "1s", "2s", "5s", "6s", "7s", "3m", "3m", "7m", "8m", "9m", "F", "F"],
  ["2m", "2m", "5m", "6m", "7m", "3p", "3p", "5p", "6p", "7p", "C", "C", "N"],
];
const FIXTURE_WALL = __FIXTURE_WALL_JSON__;
const TILE_INDEX = Object.fromEntries(POLICY.tiles.map((tile, index) => [tile, index]));

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
  mode: MODE_USER,
  paused: false,
  timer: null,
  lastDecision: null,
  selectedIndex: 0,
  draggedIndex: null,
  motionEnabled: true,
  log: [],
};
let asciiFieldObserver = null;

function cloneHands() {
  return INITIAL_HANDS.map((hand) => [...hand]);
}

function startHand() {
  clearModelTimer();
  state.hands = cloneHands();
  state.wall = [...FIXTURE_WALL];
  state.discards = [[], [], [], []];
  state.calls = [[], [], [], []];
  state.scores = [...INITIAL_SCORES];
  state.currentSeat = 0;
  state.drawnTile = null;
  state.turn = 1;
  state.terminal = null;
  state.paused = false;
  state.lastDecision = null;
  state.selectedIndex = 0;
  state.draggedIndex = null;
  state.log = [`${policyLabel()} loaded.`, "East 1 begins."];
  drawForCurrentSeat();
  render();
  scheduleModelAction();
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
  if (state.currentSeat === 0) {
    state.selectedIndex = state.hands[0].length - 1;
  }
  state.log.unshift(`${SEATS[state.currentSeat]} draws.`);
}

function clearModelTimer() {
  if (state.timer !== null) {
    window.clearTimeout(state.timer);
    state.timer = null;
  }
}

function scheduleModelAction() {
  clearModelTimer();
  if (state.terminal || state.paused || !isModelSeat(state.currentSeat)) {
    return;
  }
  const delay = state.mode === MODE_AUTOPLAY ? 360 : 520;
  state.timer = window.setTimeout(() => runModelDecision(), delay);
}

function isModelSeat(seat) {
  return state.mode === MODE_AUTOPLAY || seat !== 0;
}

function runModelDecision(options = {}) {
  if (state.terminal || (state.paused && !options.forced)) {
    return;
  }
  if (!isModelSeat(state.currentSeat)) {
    window.KenjakuMotion?.shake(".player-console");
    return;
  }
  const decision = selectModelAction(state.currentSeat);
  if (!decision || decision.action.kind !== "discard") {
    finishExhaustiveDraw();
    render();
    return;
  }
  state.lastDecision = decision;
  discardTile(discardIndexForTile(decision.action.tile, state.hands[state.currentSeat]), {
    decision,
    source: "model",
  });
}

function discardTile(index, options = {}) {
  if (state.terminal) {
    triggerScreenShake("light");
    announce("The hand is complete. Restart to play again.");
    return;
  }
  const hand = state.hands[state.currentSeat];
  if (index < 0 || index >= hand.length) {
    triggerScreenShake("light");
    announce("That tile is no longer available.");
    return;
  }
  clearModelTimer();
  const actingSeat = state.currentSeat;
  const sourceRect = discardSourceRect(actingSeat, index, options.sourceElement);
  const [tile] = hand.splice(index, 1);
  state.draggedIndex = null;
  state.discards[actingSeat].push(tile);
  state.drawnTile = null;
  if (options.source === "model") {
    const confidence = formatPercent(options.decision.probability);
    state.log.unshift(
      `${SEATS[actingSeat]} policy discards ${tile} (${confidence}).`
    );
  } else {
    state.lastDecision = selectModelAction(actingSeat);
    state.log.unshift(`${SEATS[actingSeat]} user discards ${tile}.`);
  }
  state.currentSeat = (state.currentSeat + 1) % SEATS.length;
  if (state.currentSeat === 0) {
    state.turn += 1;
  }
  drawForCurrentSeat();
  render();
  clearTilePreview();
  playDiscardMotion({ sourceRect, seat: actingSeat, tile });
  scheduleModelAction();
  if (motionAllowed()) {
    window.KenjakuMotion?.confirm(actingSeat === 0 ? ".player-console" : "#discard-target");
  }
  announce(`${SEATS[actingSeat]} discards ${tile}.`);
}

function finishExhaustiveDraw() {
  clearModelTimer();
  state.terminal = "Exhaustive draw";
  const tenpaiSeats = [0, 2];
  state.scores = state.scores.map((score, seat) => (
    tenpaiSeats.includes(seat) ? score + 1500 : score - 1500
  ));
  state.log.unshift("The wall is exhausted. Tenpai payments are applied.");
}

function setMode(mode) {
  state.mode = mode;
  state.paused = false;
  state.log.unshift(mode === MODE_AUTOPLAY ? "Autoplay model mode." : "User vs model mode.");
  render();
  scheduleModelAction();
}

function togglePause() {
  state.paused = !state.paused;
  state.log.unshift(state.paused ? "Autoplay paused." : "Autoplay resumed.");
  render();
  scheduleModelAction();
}

function toggleMotion() {
  state.motionEnabled = !state.motionEnabled;
  renderControls();
  announce(`Motion feedback ${state.motionEnabled ? "enabled" : "disabled"}.`);
}

function stepModelAction() {
  clearModelTimer();
  runModelDecision({ forced: true });
}

function render() {
  renderAsciiField();
  renderStatus();
  renderControls();
  renderOpponents();
  renderHand();
  renderActions();
  renderScores();
  renderPolicy();
  renderDiscards();
  renderCalls();
  renderLog();
}

function setupAsciiField() {
  const canvas = document.getElementById("ascii-field");
  if (!canvas) {
    return;
  }
  renderAsciiField();
  if (typeof ResizeObserver === "undefined") {
    window.addEventListener("resize", renderAsciiField);
    return;
  }
  asciiFieldObserver?.disconnect();
  asciiFieldObserver = new ResizeObserver(renderAsciiField);
  asciiFieldObserver.observe(canvas);
}

function renderAsciiField() {
  const canvas = document.getElementById("ascii-field");
  if (!canvas) {
    return;
  }
  const bounds = canvas.getBoundingClientRect();
  if (bounds.width <= 0 || bounds.height <= 0) {
    return;
  }
  const ratio = Math.min(window.devicePixelRatio || 1, 2);
  const columns = Math.max(52, Math.min(144, Math.floor(bounds.width / 7)));
  const rows = Math.max(24, Math.min(58, Math.floor(bounds.height / 11)));
  const frame = createAsciiFrame(columns, rows);
  const characterWidth = bounds.width / columns;
  const characterHeight = bounds.height / rows;
  canvas.width = Math.max(1, Math.floor(bounds.width * ratio));
  canvas.height = Math.max(1, Math.floor(bounds.height * ratio));
  const context = canvas.getContext("2d");
  if (!context) {
    return;
  }
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
  context.clearRect(0, 0, bounds.width, bounds.height);
  context.fillStyle = "#f1eddb";
  const fontSize = Math.max(7, Math.floor(characterHeight * 0.86));
  context.font = `${fontSize}px ui-monospace, SFMono-Regular, Menlo, monospace`;
  context.textBaseline = "top";
  frame.forEach((line, row) => {
    Array.from(line).forEach((glyph, column) => {
      if (glyph === " ") {
        return;
      }
      const x = column * characterWidth;
      const y = row * characterHeight;
      const edge = glyph === "+" || glyph === "-" || glyph === "|" || glyph === "@";
      context.globalAlpha = edge ? 0.14 : 0.035;
      context.fillText(glyph, x + 1, y);
      context.globalAlpha = glyph === "@" ? 0.44 : edge ? 0.24 : 0.09;
      context.fillText(glyph, x, y);
    });
  });
  context.globalAlpha = 1;
}

function createAsciiFrame(columns, rows) {
  const glyphs = " .,:;i1tfLCG08@";
  const frame = Array.from({ length: rows }, () => Array(columns).fill(" "));
  const centerX = Math.floor(columns / 2) + (state.currentSeat - 1) * 2;
  const centerY = Math.floor(rows / 2);
  const seed =
    state.turn * 17 + state.wall.length * 7 + state.currentSeat * 29 + (state.terminal ? 41 : 0);
  for (let row = 0; row < rows; row += 1) {
    for (let column = 0; column < columns; column += 1) {
      const distance = Math.hypot((column - centerX) / columns, (row - centerY) / rows);
      const wave = Math.sin(column * 0.23 + row * 0.19 + seed) * 0.09;
      const density = Math.max(
        0,
        0.27 - distance * 0.62 + wave + asciiNoise(column, row, seed) * 0.13,
      );
      const index = Math.min(glyphs.length - 1, Math.floor(density * glyphs.length * 1.7));
      frame[row][column] = index > 0 ? glyphs[index] : " ";
    }
  }
  drawAsciiBox(frame, 2, 2, columns - 4, rows - 4);
  drawAsciiBox(
    frame,
    Math.floor(columns * 0.27),
    Math.floor(rows * 0.29),
    Math.max(12, Math.floor(columns * 0.46)),
    Math.max(7, Math.floor(rows * 0.42)),
  );
  plotAscii(frame, centerX, centerY, "@");
  plotAscii(frame, centerX - 1, centerY, "+");
  plotAscii(frame, centerX + 1, centerY, "+");
  plotAscii(frame, centerX, centerY - 1, "+");
  plotAscii(frame, centerX, centerY + 1, "+");
  return frame.map((row) => row.join(""));
}

function drawAsciiBox(frame, left, top, width, height) {
  const right = left + width - 1;
  const bottom = top + height - 1;
  if (
    left < 0 || top < 0 || right >= frame[0].length || bottom >= frame.length
    || width < 2 || height < 2
  ) {
    return;
  }
  for (let column = left + 1; column < right; column += 1) {
    frame[top][column] = "-";
    frame[bottom][column] = "-";
  }
  for (let row = top + 1; row < bottom; row += 1) {
    frame[row][left] = "|";
    frame[row][right] = "|";
  }
  frame[top][left] = "+";
  frame[top][right] = "+";
  frame[bottom][left] = "+";
  frame[bottom][right] = "+";
}

function plotAscii(frame, column, row, glyph) {
  if (row >= 0 && row < frame.length && column >= 0 && column < frame[0].length) {
    frame[row][column] = glyph;
  }
}

function asciiNoise(column, row, seed) {
  const value = Math.sin((column + 1) * 12.9898 + (row + 1) * 78.233 + seed * 37.719) * 43758.5453;
  return value - Math.floor(value);
}

function renderStatus() {
  text("wall-count", state.wall.length);
  text("wall-meter", state.wall.length);
  text("turn-label", state.terminal ? "Terminal" : SEATS[state.currentSeat]);
  text("mode-label", modeLabel());
  text("terminal-result", state.terminal || "In progress");
  setScoreText("live-delta", scoreDeltaLabel());
  text("call-zone-summary", callSummaryLabel());
  text("turn-vector", turnVectorLabel());
  document.getElementById("dora-tile").textContent = DORA_INDICATOR;
}

function renderControls() {
  setActiveButton("autoplay-button", state.mode === MODE_AUTOPLAY);
  setActiveButton("user-mode-button", state.mode === MODE_USER);
  const pauseButton = document.getElementById("pause-button");
  pauseButton.textContent = state.paused ? "Resume" : "Pause";
  pauseButton.disabled = Boolean(state.terminal);
  pauseButton.classList.toggle("is-blocked", Boolean(state.terminal));
  const stepButton = document.getElementById("step-button");
  const stepDisabled = Boolean(state.terminal) || !isModelSeat(state.currentSeat);
  stepButton.disabled = stepDisabled;
  stepButton.classList.toggle("is-blocked", stepDisabled);
  const motionButton = document.getElementById("motion-button");
  motionButton.textContent = `Motion: ${state.motionEnabled ? "On" : "Off"}`;
  motionButton.setAttribute("aria-pressed", state.motionEnabled ? "true" : "false");
  motionButton.classList.toggle("is-active", state.motionEnabled);
  document.body.classList.toggle("game-motion-off", !state.motionEnabled);
}

function renderOpponents() {
  const container = document.getElementById("opponents");
  container.replaceChildren(...[1, 2, 3].map((seat) => {
    const panel = document.createElement("section");
    panel.id = `opponent-seat-${seat}`;
    panel.dataset.seat = String(seat);
    panel.className = "opponent-seat kj-card";
    panel.innerHTML = `
      <div class="opponent-meta">
        <strong class="seat-name">${SEATS[seat]}</strong>
        <span class="seat-counter">${state.hands[seat].length} tiles</span>
      </div>
      <p class="label">Model Seat</p>
      <p class="score-chip kj-score-chip">${state.scores[seat].toLocaleString()}</p>
    `;
    return panel;
  }));
}

function renderHand() {
  const container = document.getElementById("player-hand");
  const legal = state.currentSeat === 0 && !state.terminal && !isModelSeat(0);
  state.selectedIndex = normalizedHandIndex(state.selectedIndex);
  container.replaceChildren(...state.hands[0].map((tile, index) => {
    const selected = legal && index === state.selectedIndex;
    const button = document.createElement("button");
    button.id = `hand-tile-${index}`;
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
    button.dataset.handIndex = String(index);
    button.textContent = tile;
    button.disabled = !legal;
    button.draggable = legal;
    button.setAttribute(
      "aria-label",
      `${tile}, tile ${index + 1} of ${state.hands[0].length}. `
        + `${legal ? "Discardable" : "Unavailable"}.`,
    );
    if (legal) {
      button.addEventListener("click", () => discardTile(index, { sourceElement: button }));
      button.addEventListener("keydown", (event) => handleTileKeydown(event, index, button));
      button.addEventListener("focus", () => previewTile(tile));
      button.addEventListener("mouseenter", () => previewTile(tile));
      button.addEventListener("mouseleave", clearTilePreview);
      button.addEventListener("dragstart", (event) => beginTileDrag(event, index, button));
      button.addEventListener("dragend", endTileDrag);
    }
    return button;
  }));
}

function normalizedHandIndex(index) {
  const hand = state.hands[0];
  if (hand.length === 0) {
    return 0;
  }
  return Math.max(0, Math.min(Number(index) || 0, hand.length - 1));
}

function handleTileKeydown(event, index, button) {
  if (!canUserDiscard()) {
    return;
  }
  const keys = {
    ArrowLeft: index - 1,
    ArrowUp: index - 1,
    ArrowRight: index + 1,
    ArrowDown: index + 1,
    Home: 0,
    End: state.hands[0].length - 1,
  };
  if (Object.prototype.hasOwnProperty.call(keys, event.key)) {
    event.preventDefault();
    selectHandTile(keys[event.key]);
    return;
  }
  if (event.key === "Enter" || event.key === " ") {
    event.preventDefault();
    discardTile(index, { sourceElement: button });
  }
}

function selectHandTile(index) {
  state.selectedIndex = normalizedHandIndex(index);
  render();
  const tile = state.hands[0][state.selectedIndex];
  window.requestAnimationFrame(() => {
    document.getElementById(`hand-tile-${state.selectedIndex}`)?.focus();
  });
  previewTile(tile);
  announce(`${tile} selected. Press Enter to discard.`);
}

function canUserDiscard() {
  return state.currentSeat === 0 && !state.terminal && !isModelSeat(0);
}

function previewTile(tile) {
  const target = document.getElementById("discard-target");
  target?.classList.toggle("has-intent", Boolean(tile));
  text("target-preview", tile ? `Discard ${tile} →` : "Drag a tile to commit");
}

function clearTilePreview() {
  if (state.draggedIndex !== null) {
    return;
  }
  document.getElementById("discard-target")?.classList.remove("has-intent");
  text("target-preview", "Drag a tile to commit");
}

function beginTileDrag(event, index, button) {
  if (!canUserDiscard()) {
    event.preventDefault();
    return;
  }
  state.draggedIndex = index;
  button.classList.add("is-dragging");
  const transfer = event.dataTransfer;
  if (transfer) {
    transfer.effectAllowed = "move";
    transfer.setData("text/plain", String(index));
  }
  const tile = state.hands[0][index];
  previewTile(tile);
  announce(`Dragging ${tile}. Release on the table to discard.`);
}

function endTileDrag(event) {
  event?.currentTarget?.classList.remove("is-dragging");
  state.draggedIndex = null;
  document.getElementById("discard-target")?.classList.remove("is-drag-over");
  clearTilePreview();
}

function setupDiscardTarget() {
  const target = document.getElementById("discard-target");
  if (!target) {
    return;
  }
  target.addEventListener("dragenter", (event) => {
    if (state.draggedIndex === null) {
      return;
    }
    event.preventDefault();
    target.classList.add("is-drag-over");
  });
  target.addEventListener("dragover", (event) => {
    if (state.draggedIndex === null) {
      return;
    }
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    target.classList.add("is-drag-over");
  });
  target.addEventListener("dragleave", (event) => {
    if (!target.contains(event.relatedTarget)) {
      target.classList.remove("is-drag-over");
    }
  });
  target.addEventListener("drop", (event) => {
    event.preventDefault();
    const index = state.draggedIndex;
    const source = document.getElementById(`hand-tile-${index}`);
    endTileDrag();
    if (index !== null && canUserDiscard()) {
      discardTile(index, { sourceElement: source });
    }
  });
}

function discardSourceRect(seat, index, sourceElement) {
  const source = sourceElement || (
    seat === 0
      ? document.getElementById(`hand-tile-${index}`)
      : document.getElementById(`opponent-seat-${seat}`)
  );
  if (!source) {
    return null;
  }
  const rect = source.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) {
    return null;
  }
  return {
    height: Math.min(rect.height, 43),
    left: rect.left + (rect.width - Math.min(rect.width, 32)) / 2,
    top: rect.top + (rect.height - Math.min(rect.height, 43)) / 2,
    width: Math.min(rect.width, 32),
  };
}

function playDiscardMotion({ sourceRect, seat, tile }) {
  const target = document.getElementById("discard-target");
  if (!target) {
    return;
  }
  triggerScreenShake(seat === 0 ? "heavy" : "light");
  triggerMotionClass(target, "is-impact", 500);
  if (!motionAllowed() || !sourceRect) {
    return;
  }
  window.requestAnimationFrame(() => {
    const targetRect = target.getBoundingClientRect();
    const endX = targetRect.left + targetRect.width / 2 - sourceRect.width / 2;
    const endY = targetRect.top + targetRect.height / 2 - sourceRect.height / 2;
    const offsetX = endX - sourceRect.left;
    const offsetY = endY - sourceRect.top;
    const ghost = document.createElement("div");
    ghost.className = `tile tile-flight ${tileClass(tile)}`;
    ghost.textContent = tile;
    ghost.style.left = `${sourceRect.left}px`;
    ghost.style.top = `${sourceRect.top}px`;
    ghost.style.width = `${sourceRect.width}px`;
    ghost.style.minWidth = `${sourceRect.width}px`;
    ghost.style.height = `${sourceRect.height}px`;
    ghost.style.minHeight = `${sourceRect.height}px`;
    document.body.appendChild(ghost);
    const vector = createTileVector(sourceRect, endX, endY);
    const flight = ghost.animate([
      { opacity: 1, transform: "translate3d(0, 0, 0) rotate(0deg) scale(1)" },
      {
        offset: 0.62,
        opacity: 1,
        transform: (
          `translate3d(${offsetX * 0.66}px, ${offsetY * 0.66 - 28}px, 0) `
          + "rotate(-8deg) scale(1.14)"
        ),
      },
      {
        opacity: 0.1,
        transform: `translate3d(${offsetX}px, ${offsetY}px, 0) rotate(5deg) scale(0.74)`,
      },
    ], {
      duration: 390,
      easing: "cubic-bezier(.16,.84,.28,1)",
      fill: "forwards",
    });
    flight.addEventListener("finish", () => ghost.remove(), { once: true });
    window.setTimeout(() => vector?.remove(), 400);
  });
}

function createTileVector(sourceRect, endX, endY) {
  const startX = sourceRect.left + sourceRect.width / 2;
  const startY = sourceRect.top + sourceRect.height / 2;
  const destinationX = endX + sourceRect.width / 2;
  const destinationY = endY + sourceRect.height / 2;
  const distance = Math.hypot(destinationX - startX, destinationY - startY);
  if (distance < 1) {
    return null;
  }
  const vector = document.createElement("div");
  vector.className = "tile-vector";
  vector.style.left = `${startX}px`;
  vector.style.top = `${startY}px`;
  vector.style.width = `${distance}px`;
  vector.style.transform = `rotate(${Math.atan2(destinationY - startY, destinationX - startX)}rad)`;
  document.body.appendChild(vector);
  vector.animate([
    { opacity: 0, transform: `${vector.style.transform} scaleX(0)` },
    { offset: 0.24, opacity: 0.86, transform: `${vector.style.transform} scaleX(1)` },
    { opacity: 0, transform: `${vector.style.transform} scaleX(1)` },
  ], { duration: 390, easing: "ease-out", fill: "forwards" });
  return vector;
}

function triggerScreenShake(level) {
  const className = level === "heavy" ? "game-impact-heavy" : "game-impact-light";
  triggerMotionClass(
    document.querySelector(".table-view"),
    className,
    level === "heavy" ? 460 : 320,
  );
}

function triggerMotionClass(element, className, duration) {
  if (!element || !motionAllowed()) {
    return;
  }
  element.classList.remove(className);
  void element.offsetWidth;
  element.classList.add(className);
  window.setTimeout(() => element.classList.remove(className), duration);
}

function motionAllowed() {
  return state.motionEnabled && !window.KenjakuMotion?.prefersReducedMotion?.();
}

function announce(message) {
  text("interaction-status", message);
}

function renderActions() {
  const container = document.getElementById("legal-actions");
  container.replaceChildren();
  if (state.terminal) {
    container.appendChild(actionNode("Hand complete", { disabled: true, blocked: true }));
    return;
  }
  const preview = previewPolicyDecision();
  if (isModelSeat(state.currentSeat)) {
    const label = preview
      ? `Model ${actionLabel(preview.action)}`
      : `${SEATS[state.currentSeat]} is acting`;
    container.appendChild(
      actionNode(label, { active: true, disabled: true })
    );
    return;
  }
  state.hands[0].forEach((tile, index) => {
    const active = preview?.action.kind === "discard" && preview.action.tile === tile;
    const button = actionNode(`Discard ${tile}`, { active, legal: true });
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

function renderPolicy() {
  const preview = previewPolicyDecision();
  text("policy-export", policyLabel());
  if (!preview) {
    text("policy-decision", "None");
    text("policy-confidence", "0%");
    return;
  }
  text("policy-decision", `${SEATS[state.currentSeat]} ${actionLabel(preview.action)}`);
  text("policy-confidence", formatPercent(preview.probability));
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
    options.active ? "is-active" : "",
    options.blocked ? "is-blocked" : "",
  ].filter(Boolean).join(" ");
  button.textContent = label;
  button.disabled = Boolean(options.disabled);
  if (options.disabled) {
    button.setAttribute("aria-disabled", "true");
  }
  return button;
}

function setActiveButton(id, active) {
  const button = document.getElementById(id);
  button.classList.toggle("is-active", active);
  button.setAttribute("aria-pressed", active ? "true" : "false");
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

function modeLabel() {
  if (state.mode === MODE_AUTOPLAY) {
    return state.paused ? "Paused" : "Autoplay";
  }
  return "User mode";
}

function turnVectorLabel() {
  if (state.terminal) {
    return "Terminal state";
  }
  const nextSeat = (state.currentSeat + 1) % SEATS.length;
  return `${SEATS[state.currentSeat]} → ${SEATS[nextSeat]}`;
}

function policyLabel() {
  return `${POLICY.model.policy_kind} ${POLICY.model.input_dim}->${POLICY.model.action_dim}`;
}

function previewPolicyDecision() {
  if (state.terminal || state.hands[state.currentSeat].length === 0) {
    return null;
  }
  return selectModelAction(state.currentSeat);
}

function selectModelAction(seat) {
  const legalActions = legalDiscardActions(state.hands[seat]);
  if (legalActions.length === 0) {
    return null;
  }
  const entry = decisionEntry(seat);
  const features = ppoStateFeatures(entry);
  const mask = legalActionMask(legalActions);
  const logits = policyLogits(features, mask);
  const actionIndex = bestActionIndex(logits, mask);
  const action = actionFromIndex(actionIndex);
  return {
    action,
    actionIndex,
    entry,
    legalActions,
    logit: logits[actionIndex],
    probability: legalProbability(logits, mask, actionIndex),
    seat,
  };
}

function legalDiscardActions(hand) {
  return [...new Set(hand)].map((tile) => ({ kind: "discard", tile }));
}

function decisionEntry(seat) {
  return {
    decision_type: "discard",
    seat,
    state: {
      turn: state.turn,
      current_seat: state.currentSeat,
      round_wind: "E",
      dealer_seat: 0,
      honba: 0,
      points: [...state.scores],
      hands: state.hands.map((hand) => [...hand]),
      hand_sizes: state.hands.map((hand) => hand.length),
      discards: state.discards.map((discards) => [...discards]),
      melds: state.calls.map((calls) => [...calls]),
      dora_indicators: [DORA_INDICATOR],
      wall_remaining: state.wall.length,
      drawn_tile: state.drawnTile,
      needs_discard: true,
      pending_reaction_seats: [],
    },
  };
}

function ppoStateFeatures(entry) {
  const payload = entry.state;
  const points = payload.points.slice(0, 4);
  while (points.length < 4) {
    points.push(0);
  }
  const drawnOneHot = Array(34).fill(0);
  if (typeof payload.drawn_tile === "string") {
    drawnOneHot[tileIndex(payload.drawn_tile)] = 1;
  }
  const roundWindOneHot = ["E", "S", "W", "N"].map((wind) => (
    payload.round_wind === wind ? 1 : 0
  ));
  const decisionOneHot = POLICY.model.decision_types.map((candidate) => (
    entry.decision_type === candidate ? 1 : 0
  ));
  const pending = payload.pending_reaction_seats || [];
  const features = [
    Number(payload.turn || 0) / 256,
    Number(payload.current_seat || 0) / 3,
    Number(entry.seat || 0) / 3,
    Number(payload.dealer_seat || 0) / 3,
    Number(payload.honba || 0) / 8,
    Number(payload.wall_remaining || 0) / 80,
    payload.needs_discard ? 1 : 0,
    pending.length / 4,
    (payload.points || []).length / 4,
    ...roundWindOneHot,
    ...points.map((point) => Number(point) / 100000),
    ...drawnOneHot,
    ...decisionOneHot,
  ];
  if (features.length !== POLICY.model.input_dim) {
    throw new Error(`PPO state must have ${POLICY.model.input_dim} features`);
  }
  return features;
}

function legalActionMask(actions) {
  const mask = Array(POLICY.model.action_dim).fill(false);
  actions.forEach((action) => {
    mask[actionIndex(action)] = true;
  });
  if (!mask.some(Boolean)) {
    throw new Error("PPO legal action mask cannot be empty");
  }
  return mask;
}

function actionIndex(action) {
  if (Object.prototype.hasOwnProperty.call(POLICY.actions.kind_offsets, action.kind)) {
    return POLICY.actions.kind_offsets[action.kind] + tileIndex(action.tile);
  }
  if (Object.prototype.hasOwnProperty.call(POLICY.actions.special_indices, action.kind)) {
    return POLICY.actions.special_indices[action.kind];
  }
  throw new Error(`unsupported PPO action kind: ${action.kind}`);
}

function actionFromIndex(actionIndexValue) {
  for (const [kind, offset] of Object.entries(POLICY.actions.kind_offsets)) {
    if (offset <= actionIndexValue && actionIndexValue < offset + 34) {
      return { kind, tile: POLICY.tiles[actionIndexValue - offset] };
    }
  }
  for (const [kind, index] of Object.entries(POLICY.actions.special_indices)) {
    if (index === actionIndexValue) {
      return { kind };
    }
  }
  throw new Error(`unsupported PPO action index: ${actionIndexValue}`);
}

function policyLogits(features, mask) {
  const logits = POLICY.model_state.bias.map((value, index) => (mask[index] ? value : -1.0e9));
  POLICY.model_state.weights.forEach((weight) => {
    if (mask[weight.action]) {
      logits[weight.action] += weight.value * features[weight.feature];
    }
  });
  return logits;
}

function bestActionIndex(logits, mask) {
  let bestIndex = -1;
  let bestLogit = -Infinity;
  logits.forEach((logit, index) => {
    if (mask[index] && logit > bestLogit) {
      bestIndex = index;
      bestLogit = logit;
    }
  });
  return bestIndex;
}

function legalProbability(logits, mask, actionIndexValue) {
  const legalLogits = logits.filter((_logit, index) => mask[index]);
  const maxLogit = Math.max(...legalLogits);
  const denominator = legalLogits.reduce((total, logit) => (
    total + Math.exp(logit - maxLogit)
  ), 0);
  return Math.exp(logits[actionIndexValue] - maxLogit) / denominator;
}

function discardIndexForTile(tile, hand) {
  for (let index = hand.length - 1; index >= 0; index -= 1) {
    if (hand[index] === tile) {
      return index;
    }
  }
  return -1;
}

function tileIndex(tile) {
  const index = TILE_INDEX[tile];
  if (index === undefined) {
    throw new Error(`unknown tile: ${tile}`);
  }
  return index;
}

function actionLabel(action) {
  if (action.tile) {
    return `${action.kind} ${action.tile}`;
  }
  return action.kind;
}

function formatPercent(value) {
  return `${Math.round(value * 100)}%`;
}

document.getElementById("restart-button").addEventListener("click", startHand);
document.getElementById("autoplay-button").addEventListener("click", () => setMode(MODE_AUTOPLAY));
document.getElementById("user-mode-button").addEventListener("click", () => setMode(MODE_USER));
document.getElementById("pause-button").addEventListener("click", togglePause);
document.getElementById("step-button").addEventListener("click", stepModelAction);
document.getElementById("motion-button").addEventListener("click", toggleMotion);
window.KenjakuGame = {
  policy: POLICY,
  selectModelAction,
  setMode,
  startHand,
  state,
  stepModelAction,
  toggleMotion,
};
setupAsciiField();
setupDiscardTarget();
startHand();
"""


def _game_js(policy: dict[str, Any]) -> str:
    fixture_json = json.dumps(list(fixture_wall()))
    policy_json = json.dumps(policy, separators=(",", ":"), sort_keys=True)
    return _GAME_JS_TEMPLATE.replace("__FIXTURE_WALL_JSON__", fixture_json).replace(
        "__POLICY_JSON__",
        policy_json,
    )
