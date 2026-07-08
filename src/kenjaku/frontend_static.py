from __future__ import annotations

import json
from collections.abc import Sequence
from html import escape
from typing import Any

from kenjaku.frontend_theme import KENJAKU_ARCADE_THEME_CSS, KENJAKU_ARCADE_THEME_VERSION

FRONTEND_STATIC_HELPERS_VERSION = "kenjaku-static-frontend-helpers-v0"
_REMOTE_PREFIXES = ("http://", "https://", "//")
_NETWORK_TOKENS = ("http://", "https://", "@import")

BASE_STATIC_CSS = """
* {
  box-sizing: border-box;
}

.kj-static-wrap {
  width: min(1180px, calc(100% - 32px));
  margin: 0 auto;
}

.kj-static-muted {
  color: var(--kj-card-muted, #66717e);
}

.kj-static-eyebrow {
  margin-bottom: 8px;
  color: var(--kj-action, #0f766e);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
}

.kj-badge {
  display: inline-flex;
  align-items: center;
  min-height: 24px;
  border: 1px solid var(--kj-card-edge, #d9e0e6);
  border-radius: var(--kj-radius-sm, 4px);
  padding: 2px 7px;
  font-size: 12px;
  font-weight: 800;
}

.kj-badge--success { color: var(--kj-success, #0f766e); }
.kj-badge--warning { color: var(--kj-warning, #a15c07); }
.kj-badge--error { color: var(--kj-error, #b42318); }

.kj-sort-button {
  all: unset;
  cursor: pointer;
  color: inherit;
  font: inherit;
  text-transform: uppercase;
}

@media (max-width: 760px) {
  .kj-static-wrap {
    width: min(100% - 20px, 1180px);
  }
}
""".strip()


def html_text(value: Any) -> str:
    return escape(str(value), quote=True)


def html_attr(value: Any) -> str:
    return escape(str(value), quote=True)


def theme_css() -> str:
    return KENJAKU_ARCADE_THEME_CSS


def static_base_css(*, include_theme: bool = False) -> str:
    parts = [BASE_STATIC_CSS]
    if include_theme:
        parts.insert(0, theme_css())
    return "\n\n".join(parts)


def html_document(
    *,
    title: str,
    body_html: str,
    stylesheets: Sequence[str] = (),
    scripts: Sequence[str] = (),
    inline_css: Sequence[str] = (),
    inline_script: str = "",
    body_class: str = "",
    lang: str = "en",
) -> str:
    for reference in (*stylesheets, *scripts):
        _reject_remote_reference(reference)
    for css in inline_css:
        assert_no_browser_network(css)
    assert_no_browser_network(body_html)
    if inline_script:
        assert_no_browser_network(inline_script)
    stylesheet_tags = "\n".join(
        f'  <link rel="stylesheet" href="{html_attr(reference)}">' for reference in stylesheets
    )
    style_block = (
        "  <style>\n" + "\n\n".join(inline_css).strip() + "\n  </style>" if inline_css else ""
    )
    script_tags = "\n".join(
        f'  <script src="{html_attr(reference)}"></script>' for reference in scripts
    )
    inline_script_block = (
        f"  <script>\n{inline_script.strip()}\n  </script>" if inline_script else ""
    )
    head_parts = "\n".join(
        part
        for part in (
            '  <meta charset="utf-8">',
            '  <meta name="viewport" content="width=device-width, initial-scale=1">',
            (
                f'  <meta name="generator" content="{FRONTEND_STATIC_HELPERS_VERSION}; '
                f'{KENJAKU_ARCADE_THEME_VERSION}">'
            ),
            f"  <title>{html_text(title)}</title>",
            stylesheet_tags,
            style_block,
        )
        if part
    )
    body_attrs = f' class="{html_attr(body_class)}"' if body_class else ""
    tail_parts = "\n".join(part for part in (script_tags, inline_script_block) if part)
    tail = f"\n{tail_parts}" if tail_parts else ""
    return f"""<!doctype html>
<html lang="{html_attr(lang)}">
<head>
{head_parts}
</head>
<body{body_attrs}>
{body_html.rstrip()}{tail}
</body>
</html>
"""


def sortable_header(column: int, label: str, sort_type: str) -> str:
    return (
        "<th>"
        f'<button class="kj-sort-button" type="button" data-sort-column="{column}" '
        f'data-sort-type="{html_attr(sort_type)}">{html_text(label)}</button>'
        "</th>"
    )


def sortable_table_script(table_id: str) -> str:
    encoded_table_id = json.dumps(table_id)
    return f"""
(() => {{
  const table = document.getElementById({encoded_table_id});
  if (!table) {{
    return;
  }}
  const body = table.tBodies[0];
  for (const button of table.querySelectorAll("[data-sort-column]")) {{
    button.addEventListener("click", () => {{
      const column = Number(button.dataset.sortColumn);
      const type = button.dataset.sortType || "text";
      const direction = button.dataset.sortDirection === "asc" ? "desc" : "asc";
      button.dataset.sortDirection = direction;
      const rows = Array.from(body.rows);
      rows.sort((left, right) => {{
        const leftValue = left.cells[column]?.dataset.sort || "";
        const rightValue = right.cells[column]?.dataset.sort || "";
        if (type === "number") {{
          return compareStaticNumber(leftValue, rightValue, direction);
        }}
        return compareStaticText(leftValue, rightValue, direction);
      }});
      for (const row of rows) {{
        body.appendChild(row);
      }}
    }});
  }}
}})();

function compareStaticNumber(left, right, direction) {{
  const leftNumber = left === "" ? Number.NEGATIVE_INFINITY : Number(left);
  const rightNumber = right === "" ? Number.NEGATIVE_INFINITY : Number(right);
  const result = leftNumber - rightNumber;
  return direction === "asc" ? result : -result;
}}

function compareStaticText(left, right, direction) {{
  const result = left.localeCompare(right, undefined, {{numeric: true}});
  return direction === "asc" ? result : -result;
}}
""".strip()


def badge(label: str, *, tone: str = "neutral") -> str:
    tone_class = "" if tone == "neutral" else f" kj-badge--{html_attr(tone)}"
    return f'<span class="kj-badge{tone_class}">{html_text(label)}</span>'


def summary_item(label: str, value: Any, *, class_name: str = "summary-item") -> str:
    return (
        f'<div class="{html_attr(class_name)}"><dt>{html_text(label)}</dt>'
        f"<dd>{html_text(value)}</dd></div>"
    )


def line_chart_svg(
    points: Sequence[tuple[float, float]],
    *,
    label: str,
    line_class: str = "line",
    dot_class: str = "dot",
    axis_class: str = "axis",
    grid_class: str = "grid",
    text_fill: str = "#66717e",
    width: int = 440,
    height: int = 180,
) -> str:
    if not points:
        return ""
    left = 42
    right = 12
    top = 14
    bottom = 30
    xs = [point[0] for point in points]
    ys = [point[1] for point in points]
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

    coords = [(sx(x), sy(y)) for x, y in points]
    if len(coords) == 1:
        shape = (
            f'<circle class="{html_attr(dot_class)}" cx="{coords[0][0]:.1f}" '
            f'cy="{coords[0][1]:.1f}" r="4"></circle>'
        )
    else:
        path = " ".join(f"{x:.1f},{y:.1f}" for x, y in coords)
        shape = f'<polyline class="{html_attr(line_class)}" points="{path}"></polyline>'
    x2 = width - right
    y2 = height - bottom
    fill = html_attr(text_fill)
    min_x_text = _chart_number(min_x)
    max_x_text = _chart_number(max_x)
    min_y_text = _chart_number(min_y)
    max_y_text = _chart_number(max_y)
    min_x_label = (
        f'<text x="{left}" y="{height - 8}" fill="{fill}" '
        f'font-size="11">{min_x_text}</text>'
    )
    max_x_label = (
        f'<text x="{x2}" y="{height - 8}" fill="{fill}" '
        f'font-size="11" text-anchor="end">{max_x_text}</text>'
    )
    max_y_label = f'<text x="4" y="{top + 4}" fill="{fill}" font-size="11">{max_y_text}</text>'
    min_y_label = f'<text x="4" y="{y2}" fill="{fill}" font-size="11">{min_y_text}</text>'
    return f"""
        <svg viewBox="0 0 {width} {height}" role="img" aria-label="{html_attr(label)}">
          <line class="{html_attr(grid_class)}" x1="{left}" y1="{top}" x2="{x2}" y2="{top}"></line>
          <line class="{html_attr(grid_class)}" x1="{left}" y1="{y2}" x2="{x2}" y2="{y2}"></line>
          <line class="{html_attr(axis_class)}" x1="{left}" y1="{top}" x2="{left}" y2="{y2}"></line>
          <line class="{html_attr(axis_class)}" x1="{left}" y1="{y2}" x2="{x2}" y2="{y2}"></line>
          {shape}
          {min_x_label}
          {max_x_label}
          {max_y_label}
          {min_y_label}
        </svg>
"""


def assert_no_browser_network(contents: str) -> None:
    lowered = contents.lower()
    for token in _NETWORK_TOKENS:
        if token in lowered:
            raise ValueError(f"static frontend asset contains browser network token: {token}")


def _reject_remote_reference(reference: str) -> None:
    lowered = reference.lower()
    if lowered.startswith(_REMOTE_PREFIXES):
        raise ValueError(f"static frontend reference must be local: {reference}")


def _chart_number(value: float) -> str:
    number = float(value)
    if number.is_integer():
        return str(int(number))
    if abs(number) >= 100:
        return f"{number:.2f}"
    if abs(number) >= 1:
        return f"{number:.4f}"
    return f"{number:.6f}".rstrip("0").rstrip(".")
