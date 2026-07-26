from __future__ import annotations

from typing import Any

KENJAKU_ARCADE_THEME_VERSION = "kenjaku-arcade-card-theme-v0"
KENJAKU_ARCADE_THEME_IP_NOTE = (
    "Original Kenjaku arcade-card direction. Do not use Balatro assets, logos, exact layouts, "
    "text, or protected trade dress."
)

KENJAKU_ARCADE_THEME_CSS = """
/* kenjaku arcade-card theme v0
   original dark table/card direction; no Balatro assets, logos, exact layouts, or trade dress */
:root {
  color-scheme: dark;
  --kj-bg-void: #080a12;
  --kj-bg-grid: #101525;
  --kj-table-felt: #0d684d;
  --kj-table-rail: #302044;
  --kj-table-line: #39d49f;
  --kj-surface: #151a29;
  --kj-surface-raised: #20283a;
  --kj-surface-sunken: #0d111d;
  --kj-card: #f5f1de;
  --kj-card-ink: #171923;
  --kj-card-muted: #6c6455;
  --kj-card-edge: #c8b883;
  --kj-tile-man: #e84d5b;
  --kj-tile-pin: #2f76d2;
  --kj-tile-sou: #30a56d;
  --kj-tile-honor: #b064d8;
  --kj-chip-gold: #ffc857;
  --kj-chip-orange: #f08a3c;
  --kj-score-positive: #66f0a3;
  --kj-score-negative: #ff6b78;
  --kj-score-neutral: #d7dce8;
  --kj-action: #67d6ff;
  --kj-action-strong: #fff06a;
  --kj-disabled: #59606f;
  --kj-error: #ff4d6d;
  --kj-warning: #ffb13d;
  --kj-success: #43e18b;
  --kj-focus-ring: #8cecff;
  --kj-shadow-hard: 0 4px 0 #050711;
  --kj-shadow-glow: 0 0 0 1px rgba(140, 236, 255, 0.35), 0 0 22px rgba(57, 212, 159, 0.18);
  --kj-radius-sm: 4px;
  --kj-radius-md: 6px;
  --kj-radius-lg: 8px;
  --kj-breakpoint-mobile: 760px;
  --kj-min-control: 34px;
  --kj-readable-muted: rgba(215, 220, 232, 0.72);
  --kj-motion-fast: 90ms;
  --kj-motion-normal: 160ms;
  --kj-motion-slow: 260ms;
  --kj-ease-snap: cubic-bezier(.2, .9, .25, 1);
}

.kj-arcade-shell {
  min-height: 100vh;
  background:
    linear-gradient(transparent 23px, rgba(255, 255, 255, 0.035) 24px),
    linear-gradient(90deg, transparent 23px, rgba(255, 255, 255, 0.035) 24px),
    var(--kj-bg-void);
  background-size: 24px 24px, 24px 24px, auto;
  color: var(--kj-score-neutral);
  font: 15px/1.45 -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

.kj-table-surface {
  background: linear-gradient(145deg, var(--kj-table-felt), #073d32);
  border: 2px solid rgba(57, 212, 159, 0.46);
  border-radius: var(--kj-radius-lg);
  box-shadow: inset 0 0 0 6px rgba(48, 32, 68, 0.72), var(--kj-shadow-hard);
}

.kj-card,
.kj-panel {
  background: var(--kj-surface);
  border: 1px solid rgba(215, 220, 232, 0.16);
  border-radius: var(--kj-radius-md);
  box-shadow: var(--kj-shadow-hard);
  min-width: 0;
  overflow-wrap: anywhere;
}

.kj-tile {
  display: inline-grid;
  place-items: center;
  min-width: 34px;
  min-height: 46px;
  padding: 4px;
  background: linear-gradient(180deg, #fff8df, var(--kj-card));
  border: 1px solid var(--kj-card-edge);
  border-radius: var(--kj-radius-sm);
  color: var(--kj-card-ink);
  font-weight: 800;
  box-shadow: 0 3px 0 #8d7d52;
  transition:
    transform var(--kj-motion-fast) var(--kj-ease-snap),
    box-shadow var(--kj-motion-fast) var(--kj-ease-snap),
    filter var(--kj-motion-fast) var(--kj-ease-snap);
}

.kj-tile:hover,
.kj-card:hover,
.kj-action-badge:hover {
  transform: translateY(-2px);
  filter: brightness(1.08);
}

.kj-tile--man { color: var(--kj-tile-man); }
.kj-tile--pin { color: var(--kj-tile-pin); }
.kj-tile--sou { color: var(--kj-tile-sou); }
.kj-tile--honor { color: var(--kj-tile-honor); }

.kj-tile.is-selected,
.kj-card.is-selected {
  box-shadow: var(--kj-shadow-glow);
  outline: 2px solid var(--kj-action-strong);
}

.kj-tile.is-error,
.kj-card.is-error {
  border-color: var(--kj-error);
  box-shadow: 0 0 0 2px rgba(255, 77, 109, 0.28), var(--kj-shadow-hard);
}

.kj-hud {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  color: var(--kj-score-neutral);
}

.kj-chip,
.kj-score-chip {
  display: inline-flex;
  gap: 6px;
  align-items: center;
  border: 1px solid rgba(255, 200, 87, 0.55);
  border-radius: 999px;
  background: linear-gradient(180deg, rgba(255, 200, 87, 0.22), rgba(240, 138, 60, 0.12));
  color: var(--kj-chip-gold);
  font-weight: 800;
  padding: 4px 8px;
  max-width: 100%;
  min-width: 0;
  overflow-wrap: anywhere;
}

.kj-action-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 32px;
  border: 1px solid rgba(103, 214, 255, 0.52);
  border-radius: var(--kj-radius-sm);
  background: rgba(103, 214, 255, 0.13);
  color: var(--kj-action);
  font-weight: 800;
  padding: 4px 9px;
  max-width: 100%;
  min-width: 0;
  overflow-wrap: anywhere;
  transition:
    transform var(--kj-motion-fast) var(--kj-ease-snap),
    background var(--kj-motion-fast) var(--kj-ease-snap);
}

.kj-control,
input,
select,
textarea {
  min-height: var(--kj-min-control);
}

.kj-action-badge.is-selected,
.kj-action-badge[aria-pressed="true"] {
  background: rgba(255, 240, 106, 0.18);
  border-color: var(--kj-action-strong);
  color: var(--kj-action-strong);
  box-shadow: var(--kj-shadow-glow);
}

.kj-action-badge.is-disabled,
.kj-action-badge[aria-disabled="true"],
.kj-tile.is-disabled,
.kj-card.is-disabled,
button:disabled {
  cursor: not-allowed;
  opacity: 0.52;
  filter: grayscale(0.35);
  transform: none;
}

.kj-state--success { color: var(--kj-success); }
.kj-state--warning { color: var(--kj-warning); }
.kj-state--error,
.kj-state--danger { color: var(--kj-error); }
.kj-score--positive { color: var(--kj-score-positive); }
.kj-score--negative { color: var(--kj-score-negative); }
.kj-score--neutral { color: var(--kj-score-neutral); }

.kj-focusable:focus-visible,
.kj-action-badge:focus-visible,
.kj-tile:focus-visible,
button:focus-visible,
a:focus-visible,
input:focus-visible,
select:focus-visible,
textarea:focus-visible,
[tabindex]:not([tabindex="-1"]):focus-visible {
  outline: 3px solid var(--kj-focus-ring);
  outline-offset: 3px;
}

@media (prefers-reduced-motion: reduce) {
  :root {
    --kj-motion-fast: 1ms;
    --kj-motion-normal: 1ms;
    --kj-motion-slow: 1ms;
  }

  .kj-tile,
  .kj-card,
  .kj-action-badge,
  .kj-chip,
  .kj-score-chip {
    animation: none;
    transition: none;
    transform: none;
  }
}

@media (prefers-contrast: more) {
  :root {
    --kj-readable-muted: #f2f5ff;
    --kj-shadow-glow: 0 0 0 2px var(--kj-focus-ring);
  }

  .kj-card,
  .kj-panel,
  .kj-tile,
  .kj-chip,
  .kj-score-chip,
  .kj-action-badge {
    border-width: 2px;
  }

  .kj-focusable:focus-visible,
  .kj-action-badge:focus-visible,
  .kj-tile:focus-visible,
  button:focus-visible,
  a:focus-visible,
  input:focus-visible,
  select:focus-visible,
  textarea:focus-visible,
  [tabindex]:not([tabindex="-1"]):focus-visible {
    outline-width: 4px;
  }
}

@media (max-width: 760px) {
  .kj-hud {
    align-items: stretch;
  }

  .kj-action-badge,
  .kj-chip,
  .kj-score-chip {
    justify-content: center;
  }
}
""".strip()


def kenjaku_arcade_theme_css() -> str:
    return KENJAKU_ARCADE_THEME_CSS


def kenjaku_arcade_theme_contract() -> dict[str, Any]:
    return {
        "version": KENJAKU_ARCADE_THEME_VERSION,
        "ip_note": KENJAKU_ARCADE_THEME_IP_NOTE,
        "css": KENJAKU_ARCADE_THEME_CSS,
        "surfaces": {
            "play": "table, hand, wall, legal-action rail, score HUD",
            "replay-viewer": "timeline rows, decision cards, reward chips",
            "benchmark-dashboard": "comparison cards, sortable tables, score chips",
            "training-dashboard": "metric cards, progress bands, status badges",
            "serve-index": "artifact launchpad cards and local file badges",
            "interpretability-overlay": "ranked decision cards and risk badges",
            "review-game": "round timeline, hand cards, heuristic risk states",
        },
        "classes": {
            "shell": "kj-arcade-shell",
            "table": "kj-table-surface",
            "panel": "kj-panel",
            "card": "kj-card",
            "tile": "kj-tile",
            "hud": "kj-hud",
            "chip": "kj-chip",
            "action_badge": "kj-action-badge",
            "disabled": "is-disabled or aria-disabled=true",
            "error": "kj-state--error or kj-state--danger",
        },
        "integration": {
            "shape": "theme-only; existing generated HTML data contracts do not change",
            "network": "self-contained CSS; no browser network dependency",
        },
        "responsive": {
            "mobile_width": "390px smoke target",
            "desktop_width": "1280px smoke target",
            "breakpoint": "760px",
            "text_overflow": "use overflow-wrap:anywhere or kj-static-truncate for dense labels",
        },
        "accessibility": {
            "focus": "visible focus ring for links, buttons, form controls, and tabindex surfaces",
            "motion": (
                "prefers-reduced-motion disables nonessential animation and transform effects"
            ),
            "contrast": "prefers-contrast: more increases focus and component border weight",
            "controls": "interactive controls keep at least 34px height",
        },
        "browser_support": {
            "baseline": "latest Safari, Chrome, Firefox, and Edge",
            "features": (
                "CSS grid, flexbox, focus-visible, prefers-reduced-motion, prefers-contrast"
            ),
            "fallback": (
                "static HTML remains readable if optional motion or contrast media "
                "queries are ignored"
            ),
        },
    }
