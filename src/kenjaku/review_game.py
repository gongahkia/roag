from __future__ import annotations

import json
from dataclasses import dataclass
from html import escape
from pathlib import Path
from typing import Any, cast

from kenjaku.core import TileType, shanten, tile_counts
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
    payload = json.dumps(report, separators=(",", ":"), sort_keys=True)
    raw_rounds = report.get("rounds")
    raw_decisions = report.get("decisions")
    raw_summary = report.get("summary")
    rounds = cast(list[Any], raw_rounds) if isinstance(raw_rounds, list) else []
    decisions = cast(list[Any], raw_decisions) if isinstance(raw_decisions, list) else []
    summary = cast(dict[str, Any], raw_summary) if isinstance(raw_summary, dict) else {}
    return "\n".join(
        [
            "<!doctype html>",
            '<html lang="en">',
            "<head>",
            '<meta charset="utf-8">',
            '<meta name="viewport" content="width=device-width, initial-scale=1">',
            f"<title>{escape(title)}</title>",
            "<style>",
            _review_css(),
            "</style>",
            "</head>",
            "<body>",
            "<main>",
            f"<h1>{escape(title)}</h1>",
            _summary_html(summary),
            "<h2>Rounds</h2>",
            *(_round_html(round_) for round_ in rounds if isinstance(round_, dict)),
            "<h2>Decisions</h2>",
            *(_decision_html(decision) for decision in decisions if isinstance(decision, dict)),
            f'<script id="review-data" type="application/json">{escape(payload)}</script>',
            "</main>",
            "</body>",
            "</html>",
        ]
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
    return (
        '<section class="summary">'
        f"<p>Rounds: {escape(str(summary.get('rounds', 0)))}</p>"
        f"<p>Decisions: {escape(str(summary.get('decisions', 0)))}</p>"
        f"<p>Matches model: {escape(_rate_text(summary.get('matches_model_rate')))}</p>"
        f"<p>Expected value delta: {escape(_number_text(summary.get('expected_value_delta')))}</p>"
        "</section>"
    )


def _round_html(round_: dict[str, Any]) -> str:
    return (
        '<section class="round">'
        f"<h3>Round {escape(str(round_.get('round_index')))}</h3>"
        f"<p>Hand: {escape(' '.join(_string_list(round_.get('hand'))))}</p>"
        f"<p>Discards: {escape(' '.join(_event_tiles(round_.get('discards'))))}</p>"
        f"<p>Calls: {escape(str(len(_list(round_.get('calls')))))}</p>"
        "<p>Riichi timing: "
        f"{escape(', '.join(str(value) for value in _list(round_.get('riichi_timing'))))}"
        "</p>"
        "</section>"
    )


def _decision_html(decision: dict[str, Any]) -> str:
    alternatives = decision.get("top_alternatives")
    alternative_items = alternatives if isinstance(alternatives, list) else []
    rows = "".join(
        f"<li>{escape(str(item.get('tile')))} "
        f"score={escape(_number_text(item.get('model_score')))} "
        f"risk={escape(_number_text(item.get('deal_in_risk')))} "
        f"impact={escape(_number_text(item.get('expected_point_impact')))}</li>"
        for item in alternative_items
        if isinstance(item, dict)
    )
    chosen = decision.get("chosen_action")
    chosen_tile = chosen.get("tile") if isinstance(chosen, dict) else None
    return (
        '<section class="decision">'
        f"<h3>Event {escape(str(decision.get('event_index')))}</h3>"
        f"<p>Chosen: {escape(str(chosen_tile))}</p>"
        f"<p>Model top: {escape(str(decision.get('model_top_tile')))}</p>"
        f"<p>Matches model: {escape(str(decision.get('matches_model')))}</p>"
        f"<p>Shanten delta: {escape(str(_dict_value(decision.get('shanten'), 'delta')))}</p>"
        f"<p>Deal-in risk: {escape(_number_text(decision.get('deal_in_risk')))}</p>"
        "<p>Expected point impact: "
        f"{escape(_number_text(decision.get('expected_point_impact')))}</p>"
        f"<ol>{rows}</ol>"
        "</section>"
    )


def _review_css() -> str:
    return """
body{font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;margin:0;color:#1c1c1a}
body{background:#f7f7f4}
main{max-width:1120px;margin:0 auto;padding:24px}
h1,h2,h3{margin:0 0 10px}
.summary{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr))}
.summary{gap:8px;margin:16px 0}
.summary p,.round,.decision{border:1px solid #d6d3ca;background:#fff;padding:12px;border-radius:6px}
.round,.decision{margin:10px 0}
ol{margin:8px 0 0;padding-left:24px}
""".strip()


def _event_tiles(value: Any) -> list[str]:
    return [
        str(item.get("tile"))
        for item in _list(value)
        if isinstance(item, dict) and isinstance(item.get("tile"), str)
    ]


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
