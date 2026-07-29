from __future__ import annotations

import json
from dataclasses import dataclass
from html import escape
from pathlib import Path
from typing import Any, cast

from kenjaku.core import TileType, shanten, tile_counts
from kenjaku.frontend_static import (
    html_document,
    motion_primitives_css,
    static_base_css,
    theme_css,
)
from kenjaku.io import (
    TenhouCall,
    TenhouDiscard,
    TenhouGame,
    TenhouReach,
    parse_tenhou_xml_file,
)
from kenjaku.models import DiscardFrequencyBaseline, DiscardLinearModel
from kenjaku.training import (
    DiscardExample,
    candidate_defense_risk,
    discard_shanten_delta,
    iter_discard_examples,
)
from kenjaku.training.decision_snapshots import build_decision_snapshots
from kenjaku.training.interpretability_overlay import build_interpretability_overlay

REVIEW_GAME_REPORT_KIND = "kenjaku-review-game-v0"


@dataclass(frozen=True, slots=True)
class _Policy:
    name: str
    model: DiscardFrequencyBaseline | DiscardLinearModel


def build_review_game_report(
    input_xml: str | Path,
    *,
    player: int,
    model: str | Path,
) -> dict[str, Any]:
    if player not in range(4):
        raise ValueError("player must be 0, 1, 2, or 3")
    input_path = Path(input_xml)
    game = parse_tenhou_xml_file(input_path)
    policy = _load_policy(model)
    snapshots = build_decision_snapshots(
        game,
        decision_types=("discard", "call", "riichi"),
        input_paths=[input_path],
        source={"label": "review-game", "command": None, "date": None},
    )
    player_discard_snapshots = [
        snapshot
        for snapshot in snapshots
        if snapshot.get("decision_type") == "discard" and snapshot.get("seat") == player
    ]
    overlay = build_interpretability_overlay(player_discard_snapshots, min_decisions=0)
    examples = [
        example
        for example in iter_discard_examples(game)
        if example.seat == player
    ]
    overlay_by_key = {
        (decision.get("round_index"), decision.get("event_index"), decision.get("seat")): decision
        for decision in overlay["decisions"]
        if isinstance(decision, dict)
    }
    decisions = [
        _review_decision(
            example,
            policy=policy,
            overlay=overlay_by_key.get((example.round_index, example.event_index, example.seat)),
        )
        for example in examples
    ]
    cumulative_delta = sum(decision["expected_value_delta"] for decision in decisions)
    matches = sum(1 for decision in decisions if decision["matches_model"])
    return {
        "kind": REVIEW_GAME_REPORT_KIND,
        "input_xml": str(input_path),
        "player": player,
        "model": policy.name,
        "rounds": [
            _round_summary(game, round_index, player)
            for round_index in range(len(game.rounds))
        ],
        "decisions": decisions,
        "summary": {
            "rounds": len(game.rounds),
            "decisions": len(decisions),
            "matches_model": matches,
            "matches_model_rate": None if not decisions else matches / len(decisions),
            "expected_value_delta": cumulative_delta,
        },
        "overlay": {
            "kind": overlay["kind"],
            "policy_kind": overlay["policy_kind"],
            "decision_count": overlay["decision_count"],
            "disclaimer": overlay["disclaimer"],
        },
        "disclaimer": (
            "Expected point impact and deal-in risk are local heuristic review signals, "
            "not calibrated engine-strength values."
        ),
    }


def write_review_game_html(path: str | Path, report: dict[str, Any]) -> None:
    output_path = Path(path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(format_review_game_html(report), encoding="utf-8")


def format_review_game_html(report: dict[str, Any]) -> str:
    title = f"Kenjaku Review Game - player {report.get('player')}"
    payload = _json_script_payload(report)
    raw_rounds = report.get("rounds")
    raw_decisions = report.get("decisions")
    raw_summary = report.get("summary")
    rounds = cast(list[Any], raw_rounds) if isinstance(raw_rounds, list) else []
    decisions = cast(list[Any], raw_decisions) if isinstance(raw_decisions, list) else []
    summary = cast(dict[str, Any], raw_summary) if isinstance(raw_summary, dict) else {}
    visible_decisions = [decision for decision in decisions[:80] if isinstance(decision, dict)]
    decision_note = (
        ""
        if len(decisions) <= len(visible_decisions)
        else (
            f'<p class="muted">Showing first {len(visible_decisions)} '
            f"of {len(decisions)} decisions.</p>"
        )
    )
    body = "\n".join(
        [
            '<main class="review-shell">',
            '<header class="review-header">',
            f"<h1>{escape(title)}</h1>",
            f'<span class="kj-score-chip">Model {escape(str(report.get("model")))}</span>',
            "</header>",
            _summary_html(summary),
            '<section class="rounds-panel kj-panel">',
            "<h2>Round Timeline</h2>",
            '<div class="round-grid">',
            *(_round_html(round_) for round_ in rounds if isinstance(round_, dict)),
            "</div>",
            "</section>",
            (
                f'<section class="decision-list" data-total-decisions="{len(decisions)}" '
                f'data-rendered-decisions="{len(visible_decisions)}">'
            ),
            "<h2>Decision Review</h2>",
            decision_note,
            '<div class="decision-grid">',
            *(_decision_html(decision) for decision in visible_decisions),
            "</div>",
            "</section>",
            f'<script id="review-data" type="application/json">{payload}</script>',
            "</main>",
        ]
    )
    return html_document(
        title=title,
        body_html=body,
        inline_css=(static_base_css(), theme_css(), motion_primitives_css(), _review_css()),
        body_class="review-page kj-arcade-shell",
    )


def _json_script_payload(payload: dict[str, Any]) -> str:
    return (
        json.dumps(payload, sort_keys=True, separators=(",", ":"))
        .replace("&", "\\u0026")
        .replace("<", "\\u003c")
        .replace(">", "\\u003e")
    )


def _load_policy(model: str | Path) -> _Policy:
    model_text = str(model)
    if model_text == "frequency":
        return _Policy(name="frequency", model=DiscardFrequencyBaseline((0,) * 34))
    path = Path(model)
    if not path.exists():
        raise ValueError(f"model checkpoint not found: {path}")
    return _Policy(name=f"linear-discard:{path}", model=DiscardLinearModel.load(path))


def _review_decision(
    example: DiscardExample,
    *,
    policy: _Policy,
    overlay: dict[str, Any] | None,
) -> dict[str, Any]:
    actual_tile = example.action.tile
    if actual_tile is None:
        raise ValueError("discard examples must have tile actions")
    ranked = _ranked_candidates(example, policy)
    top_tile = ranked[0]["tile"] if ranked else None
    overlay_candidates = _overlay_candidates(overlay)
    top = [
        _candidate_payload(candidate, overlay_candidates, example)
        for candidate in ranked[:3]
    ]
    actual_metric = overlay_candidates.get(actual_tile.notation, {})
    best_metric = overlay_candidates.get(str(top_tile), {})
    actual_impact = _float_value(actual_metric.get("expected_point_impact"))
    best_impact = _float_value(best_metric.get("expected_point_impact"))
    shanten_delta = discard_shanten_delta(example)
    risk = _float_value(actual_metric.get("estimated_deal_in_risk"))
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "seat": example.seat,
        "chosen_action": {"kind": "discard", "tile": actual_tile.notation},
        "model_top_tile": top_tile,
        "matches_model": actual_tile.notation == top_tile,
        "top_alternatives": top,
        "shanten": {
            "before": shanten_delta.before,
            "after": shanten_delta.after,
            "delta": shanten_delta.delta,
        },
        "deal_in_risk": risk,
        "expected_point_impact": actual_impact,
        "expected_value_delta": actual_impact - best_impact,
    }


def _ranked_candidates(example: DiscardExample, policy: _Policy) -> list[dict[str, Any]]:
    legal_tiles = [TileType(index) for index, count in enumerate(example.hand_counts) if count > 0]
    if isinstance(policy.model, DiscardLinearModel):
        logits = policy.model.logits(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
            rivers_by_seat=example.rivers_by_seat,
            riichi_declared_turns=example.riichi_declared_turns,
            riichi_declared_event_indices=example.riichi_declared_event_indices,
            meld_counts_by_seat=example.meld_counts_by_seat,
            dora_indicators=example.dora_indicators,
            last_discard_tsumogiri_by_seat=example.last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=example.ippatsu_active_seats,
        )
        scores = [(tile, logits.get(tile, float("-inf"))) for tile in legal_tiles]
    else:
        scores = [(tile, float(policy.model.counts[tile.index])) for tile in legal_tiles]
    scores.sort(key=lambda item: (-item[1], item[0].index))
    return [{"tile": tile.notation, "score": score} for tile, score in scores]


def _overlay_candidates(overlay: dict[str, Any] | None) -> dict[str, dict[str, Any]]:
    if not overlay:
        return {}
    candidates = overlay.get("top_alternatives")
    if not isinstance(candidates, list):
        return {}
    return {
        candidate["tile"]: candidate
        for candidate in candidates
        if isinstance(candidate, dict) and isinstance(candidate.get("tile"), str)
    }


def _candidate_payload(
    candidate: dict[str, Any],
    overlay_candidates: dict[str, dict[str, Any]],
    example: DiscardExample,
) -> dict[str, Any]:
    tile = str(candidate["tile"])
    metrics = overlay_candidates.get(tile, {})
    fallback = _candidate_metrics(example, TileType.parse(tile))
    return {
        "tile": tile,
        "model_score": candidate["score"],
        "shanten_delta": metrics.get("shanten_delta", fallback["shanten_delta"]),
        "deal_in_risk": _float_value(
            metrics.get("estimated_deal_in_risk", fallback["deal_in_risk"])
        ),
        "expected_point_impact": _float_value(
            metrics.get("expected_point_impact", fallback["expected_point_impact"])
        ),
    }


def _candidate_metrics(example: DiscardExample, tile: TileType) -> dict[str, int | float | None]:
    before = shanten(example.hand_counts)
    after_counts = list(example.hand_counts)
    after_counts[tile.index] -= 1
    after = shanten(tuple(after_counts)) if after_counts[tile.index] >= 0 else None
    delta = None if after is None else after - before
    risk = candidate_defense_risk(example, tile).risk
    impact = _expected_point_impact(delta, risk)
    return {
        "shanten_delta": delta,
        "deal_in_risk": risk,
        "expected_point_impact": impact,
    }


def _expected_point_impact(shanten_delta: int | None, risk: float) -> int:
    efficiency_points = 0 if shanten_delta is None else -shanten_delta * 1200
    return round(efficiency_points - risk * 8000)


def _round_summary(game: TenhouGame, round_index: int, player: int) -> dict[str, Any]:
    round_ = game.rounds[round_index]
    starting_hand = [_tile_text(tile) for tile in round_.starting_hands[player]]
    discards: list[dict[str, Any]] = []
    calls: list[dict[str, Any]] = []
    riichi_timing: list[int] = []
    for event in round_.events:
        if isinstance(event, TenhouDiscard) and event.seat == player:
            discards.append(
                {
                    "event_index": event.event_index,
                    "tile": _tile_text(event.tile),
                    "tsumogiri": event.tsumogiri,
                }
            )
        if isinstance(event, TenhouCall) and event.seat == player:
            calls.append({"event_index": event.event_index, "kind": event.meld.kind.value})
        if isinstance(event, TenhouReach) and event.seat == player and event.step == 1:
            riichi_timing.append(event.event_index)
    trajectory = [
        {
            "event_index": example.event_index,
            "before": delta.before,
            "after": delta.after,
            "delta": delta.delta,
        }
        for example in iter_discard_examples(TenhouGame((round_,)))
        if example.seat == player
        for delta in (discard_shanten_delta(example),)
    ]
    return {
        "round_index": round_index,
        "hand": starting_hand,
        "hand_shanten": shanten(tile_counts(round_.starting_hands[player])),
        "shanten_trajectory": trajectory,
        "discards": discards,
        "calls": calls,
        "riichi_timing": riichi_timing,
    }


def _summary_html(summary: dict[str, Any]) -> str:
    items = (
        ("Rounds", summary.get("rounds", 0)),
        ("Decisions", summary.get("decisions", 0)),
        ("Matches model", _rate_text(summary.get("matches_model_rate"))),
        ("Expected value delta", _number_text(summary.get("expected_value_delta"))),
    )
    body = "".join(
        '<div class="summary-card">'
        f"<dt>{escape(label)}</dt>"
        f"<dd>{escape(str(value))}</dd>"
        "</div>"
        for label, value in items
    )
    return '<section class="summary kj-panel" aria-label="Review summary">' + body + "</section>"


def _round_html(round_: dict[str, Any]) -> str:
    hand_tiles = _tile_spans(_string_list(round_.get("hand")))
    discard_tiles = _tile_spans(_event_tiles(round_.get("discards")))
    discard_body = discard_tiles or '<span class="muted">none</span>'
    calls_chip = (
        f'<span class="kj-chip">Calls {escape(str(len(_list(round_.get("calls")))))}</span>'
    )
    riichi_chip = (
        f'<span class="kj-chip">Riichi '
        f'{escape(str(len(_list(round_.get("riichi_timing")))))}</span>'
    )
    return (
        '<section class="round kj-card">'
        f"<h3>Round {escape(str(round_.get('round_index')))}</h3>"
        '<p class="meta">Starting hand</p>'
        f'<div class="tile-row">{hand_tiles}</div>'
        '<p class="meta">Discards</p>'
        f'<div class="tile-row">{discard_body}</div>'
        '<div class="round-stats">'
        f"{calls_chip}"
        f"{riichi_chip}"
        "</div>"
        "</section>"
    )


def _decision_html(decision: dict[str, Any]) -> str:
    alternatives = decision.get("top_alternatives")
    alternative_items = alternatives if isinstance(alternatives, list) else []
    rows = "".join(
        '<li class="alternative-card">'
        f'<span class="tile kj-tile {_tile_class(str(item.get("tile")))}">'
        f'{escape(str(item.get("tile")))}</span>'
        f"<span>score={escape(_number_text(item.get('model_score')))}</span>"
        f"<span>risk={escape(_number_text(item.get('deal_in_risk')))}</span>"
        f"<span>impact={escape(_number_text(item.get('expected_point_impact')))}</span>"
        "</li>"
        for item in alternative_items
        if isinstance(item, dict)
    )
    chosen = decision.get("chosen_action")
    chosen_tile = chosen.get("tile") if isinstance(chosen, dict) else None
    return (
        '<section class="decision kj-card">'
        f"<h3>Event {escape(str(decision.get('event_index')))}</h3>"
        '<div class="decision-beats">'
        f'<span class="kj-action-badge">Chosen {escape(str(chosen_tile))}</span>'
        f'<span class="kj-action-badge">Model {escape(str(decision.get("model_top_tile")))}</span>'
        f'<span class="kj-action-badge">Match {escape(str(decision.get("matches_model")))}</span>'
        "</div>"
        '<dl class="decision-metrics">'
        "<div><dt>Shanten delta</dt><dd>"
        f"{escape(str(_dict_value(decision.get('shanten'), 'delta')))}</dd></div>"
        "<div><dt>Deal-in risk</dt><dd>"
        f"{escape(_number_text(decision.get('deal_in_risk')))}</dd></div>"
        "<div><dt>Expected point impact</dt><dd>"
        f"{escape(_number_text(decision.get('expected_point_impact')))}</dd></div>"
        "</dl>"
        f'<ol class="alternative-list">{rows}</ol>'
        "</section>"
    )


def _review_css() -> str:
    return """
.review-shell{width:min(1180px,calc(100% - 32px));margin:0 auto;padding:24px 0 44px}
.review-header{display:flex;align-items:flex-end;justify-content:space-between;gap:16px;margin-bottom:14px}
h1,h2,h3,p,dl,dd,ol{margin:0}
h1{font-size:30px;line-height:1.1}
h2{font-size:20px}
.summary{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr))}
.summary{gap:10px;margin:14px 0;padding:12px}
.summary-card{border:1px solid rgba(215,220,232,.12)}
.summary-card{border-radius:var(--kj-radius-sm);padding:10px}
.summary-card dt,.meta{color:var(--kj-card-muted);font-size:12px}
.summary-card dt,.meta{font-weight:800;text-transform:uppercase}
.summary-card dd{margin-top:4px;color:var(--kj-action-strong);font-size:20px;font-weight:900}
.rounds-panel{margin:14px 0;padding:12px}
.round-grid,.decision-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px;margin-top:12px}
.round,.decision{display:grid;gap:10px;padding:12px;min-width:0}
.tile-row{display:flex;flex-wrap:wrap;gap:4px;min-height:34px}
.tile{width:30px;height:38px;min-width:30px;min-height:38px;font-size:13px}
.tile-man{color:var(--kj-tile-man)}
.tile-pin{color:var(--kj-tile-pin)}
.tile-sou{color:var(--kj-tile-sou)}
.tile-honor{color:var(--kj-tile-honor)}
.round-stats,.decision-beats{display:flex;flex-wrap:wrap;gap:6px}
.decision-list{margin-top:14px}
.decision-list h2{margin-bottom:8px}
.decision-metrics{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}
.decision-metrics div{border-top:1px solid rgba(215,220,232,.14);padding-top:8px}
.decision-metrics dt{color:var(--kj-card-muted);font-size:12px}
.decision-metrics dt{font-weight:800;text-transform:uppercase}
.decision-metrics dd{margin-top:4px}
.alternative-list{display:grid;gap:6px;list-style:none;padding:0}
.alternative-card{display:grid;grid-template-columns:auto repeat(3,minmax(0,1fr))}
.alternative-card{align-items:center;gap:8px;font-size:13px}
.muted{color:var(--kj-card-muted)}
@media(max-width:720px){
  .review-shell{width:calc(100% - 20px);padding-top:14px}
  .review-header{align-items:flex-start;flex-direction:column}
  .decision-metrics{grid-template-columns:1fr}
  .alternative-card{grid-template-columns:1fr}
}
""".strip()


def _event_tiles(value: Any) -> list[str]:
    return [
        str(item.get("tile"))
        for item in _list(value)
        if isinstance(item, dict) and isinstance(item.get("tile"), str)
    ]


def _tile_spans(tiles: list[str]) -> str:
    return "".join(
        f'<span class="tile kj-tile {_tile_class(tile)}">{escape(tile)}</span>' for tile in tiles
    )


def _tile_class(tile: str) -> str:
    if tile.endswith("m"):
        return "tile-man"
    if tile.endswith("p"):
        return "tile-pin"
    if tile.endswith("s"):
        return "tile-sou"
    return "tile-honor"


def _string_list(value: Any) -> list[str]:
    return [str(item) for item in value] if isinstance(value, list) else []


def _list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def _dict_value(value: Any, key: str) -> Any:
    return value.get(key) if isinstance(value, dict) else None


def _float_value(value: Any) -> float:
    return float(value) if isinstance(value, int | float) else 0.0


def _rate_text(value: Any) -> str:
    return "n/a" if not isinstance(value, int | float) else f"{value:.3f}"


def _number_text(value: Any) -> str:
    return "n/a" if not isinstance(value, int | float) else f"{value:.3f}"


def _tile_text(tile: Any) -> str:
    return getattr(tile, "notation", str(tile))
