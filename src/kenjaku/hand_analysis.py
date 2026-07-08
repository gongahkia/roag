from __future__ import annotations

import json
from collections.abc import Sequence
from html import escape
from math import exp
from pathlib import Path
from typing import Any

from kenjaku.core import Action, Tile, TileType, shanten, tile_counts
from kenjaku.models import DiscardFrequencyBaseline, DiscardLinearModel
from kenjaku.models.linear_discard import RAW_COUNT_FEATURE_DIM, RAW_COUNT_FEATURE_PROFILE
from kenjaku.training import DiscardExample, candidate_defense_risk

ANALYZE_HAND_KIND = "kenjaku-hand-analysis-v0"
ANALYZE_HAND_TINY_MODEL_KIND = "kenjaku-analyze-hand-tiny-linear-v0"
ANALYZE_HAND_DISCLAIMER = (
    "Sandbox discard estimate from a fixture-scale linear checkpoint plus heuristic overlay; "
    "not a calibrated engine-strength policy."
)
BUNDLED_ANALYZE_HAND_MODEL = (
    Path(__file__).resolve().parents[2]
    / "data"
    / "fixtures"
    / "models"
    / "discard-linear-tiny.json"
)
HONOR_NUMBERS = {
    "1": "E",
    "2": "S",
    "3": "W",
    "4": "N",
    "5": "P",
    "6": "F",
    "7": "C",
}
ROUND_WINDS = {
    "E": "E",
    "EAST": "E",
    "S": "S",
    "SOUTH": "S",
    "W": "W",
    "WEST": "W",
    "N": "N",
    "NORTH": "N",
}


def parse_hand_tiles(text: str) -> tuple[Tile, ...]:
    tiles: list[Tile] = []
    for token in text.replace(",", " ").split():
        tiles.extend(_parse_tile_group(token))
    if not tiles:
        raise ValueError("hand must contain at least one tile")
    return tuple(tiles)


def parse_one_tile(text: str, *, label: str) -> Tile:
    tiles = parse_hand_tiles(text)
    if len(tiles) != 1:
        raise ValueError(f"{label} must contain exactly one tile")
    return tiles[0]


def build_hand_analysis(
    *,
    hand: str,
    drawn: str,
    seat: int,
    round_wind: str,
    dora: Sequence[str] = (),
    model_path: Path | None = None,
) -> dict[str, Any]:
    if not 0 <= seat <= 3:
        raise ValueError("seat must be 0..3")
    parsed_round_wind = _parse_round_wind(round_wind)
    hand_tiles = parse_hand_tiles(hand)
    drawn_tile = parse_one_tile(drawn, label="drawn")
    dora_tiles = tuple(tile for dora_item in dora for tile in parse_hand_tiles(dora_item))
    full_hand = (*hand_tiles, drawn_tile)
    if len(full_hand) > 14:
        raise ValueError("hand plus drawn tile cannot exceed 14 tiles")
    hand_counts = tile_counts(full_hand)
    if any(count > 4 for count in hand_counts):
        raise ValueError("hand plus drawn tile cannot contain more than four copies of a tile")

    model, loaded_model_path = load_analyze_hand_model(model_path)
    visible_counts = tile_counts((*full_hand, *dora_tiles))
    current_shanten = _safe_shanten(hand_counts)
    example = _analysis_example(
        hand_counts,
        visible_counts,
        seat=seat,
        dora_indicators=dora_tiles,
    )
    model_logits, model_kind = _model_logits(model, hand_counts, visible_counts, example)
    candidates = _rank_candidates(
        hand_counts,
        visible_counts,
        current_shanten,
        example,
        model_logits,
    )
    return {
        "kind": ANALYZE_HAND_KIND,
        "hand_tiles": [tile.notation for tile in hand_tiles],
        "drawn_tile": drawn_tile.notation,
        "full_hand_tiles": [tile.notation for tile in full_hand],
        "seat": seat,
        "round_wind": parsed_round_wind,
        "dora_indicators": [tile.notation for tile in dora_tiles],
        "current_shanten": current_shanten,
        "model_kind": model_kind,
        "model_path": str(loaded_model_path) if loaded_model_path is not None else None,
        "disclaimer": ANALYZE_HAND_DISCLAIMER,
        "recommended_discard": candidates[0]["tile"] if candidates else None,
        "candidates": candidates,
    }


def load_analyze_hand_model(
    model_path: Path | None = None,
) -> tuple[DiscardLinearModel | DiscardFrequencyBaseline, Path | None]:
    path = model_path if model_path is not None else BUNDLED_ANALYZE_HAND_MODEL
    if model_path is None and not path.exists():
        return DiscardFrequencyBaseline(tuple([0] * 34)), None
    if not path.exists():
        raise FileNotFoundError(path)
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError("discard checkpoint must be a JSON object")
    if payload.get("kind") == ANALYZE_HAND_TINY_MODEL_KIND:
        model = _tiny_zero_linear_model(payload)
    else:
        model = DiscardLinearModel.from_dict(payload)
    return model, path


def format_hand_analysis_text(report: dict[str, Any]) -> str:
    lines = [
        f"hand: {' '.join(_string_list(report.get('full_hand_tiles')))}",
        f"seat: {int(report.get('seat', 0))}",
        f"round: {report.get('round_wind', 'E')}",
        f"current_shanten: {_format_optional_int(report.get('current_shanten'))}",
        f"model: {report.get('model_kind', 'unknown')}",
        f"recommended_discard: {report.get('recommended_discard')}",
        "candidates:",
    ]
    for candidate in report.get("candidates", []):
        if not isinstance(candidate, dict):
            continue
        reasons = ", ".join(_string_list(candidate.get("reasons")))
        lines.append(
            "- "
            f"{candidate['tile']}: "
            f"p={float(candidate['policy_probability']):.4f} "
            f"shanten={_format_optional_int(candidate.get('resulting_shanten'))} "
            f"delta={_format_optional_int(candidate.get('shanten_delta'))} "
            f"risk={float(candidate['estimated_deal_in_risk']):.3f} "
            f"points={int(candidate['expected_point_impact'])} "
            f"reasons={reasons}"
        )
    lines.append(f"disclaimer: {report.get('disclaimer', '')}")
    return "\n".join(lines)


def format_hand_analysis_html(report: dict[str, Any]) -> str:
    title = "Kenjaku Hand Analysis"
    hand_text = escape(" ".join(_string_list(report.get("full_hand_tiles"))))
    recommended = escape(str(report.get("recommended_discard")))
    rows = []
    for candidate in report.get("candidates", []):
        if not isinstance(candidate, dict):
            continue
        rows.append(
            "<tr>"
            f"<td>{escape(str(candidate['tile']))}</td>"
            f"<td>{float(candidate['policy_probability']):.4f}</td>"
            f"<td>{escape(_format_optional_int(candidate.get('resulting_shanten')))}</td>"
            f"<td>{escape(_format_optional_int(candidate.get('shanten_delta')))}</td>"
            f"<td>{float(candidate['estimated_deal_in_risk']):.3f}</td>"
            f"<td>{int(candidate['expected_point_impact'])}</td>"
            f"<td>{escape(', '.join(_string_list(candidate.get('reasons'))))}</td>"
            "</tr>"
        )
    return "\n".join(
        [
            "<!doctype html>",
            '<html lang="en">',
            "<head>",
            '<meta charset="utf-8">',
            '<meta name="viewport" content="width=device-width, initial-scale=1">',
            f"<title>{title}</title>",
            "<style>",
            "body{font-family:system-ui,sans-serif;margin:2rem;line-height:1.4}",
            "table{border-collapse:collapse;width:100%;max-width:1100px}",
            "th,td{border:1px solid #d0d7de;padding:.45rem;text-align:left}",
            "th{background:#f6f8fa}",
            "</style>",
            "</head>",
            "<body>",
            f"<h1>{title}</h1>",
            f"<p><strong>Hand</strong>: {hand_text}</p>",
            f"<p><strong>Recommended discard</strong>: {recommended}</p>",
            "<table>",
            "<thead><tr><th>Tile</th><th>Policy</th><th>Shanten</th>"
            "<th>Delta</th><th>Risk</th><th>Point impact</th><th>Reasons</th></tr></thead>",
            f"<tbody>{''.join(rows)}</tbody>",
            "</table>",
            f"<p>{escape(str(report.get('disclaimer', '')))}</p>",
            "</body>",
            "</html>",
        ]
    )


def _rank_candidates(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    current_shanten: int | None,
    example: DiscardExample,
    model_logits: dict[TileType, float],
) -> list[dict[str, Any]]:
    candidates: list[dict[str, Any]] = []
    for index, count in enumerate(hand_counts):
        if count <= 0:
            continue
        tile_type = TileType(index)
        after_counts = list(hand_counts)
        after_counts[index] -= 1
        resulting_shanten = _safe_shanten(tuple(after_counts))
        shanten_delta = (
            None
            if current_shanten is None or resulting_shanten is None
            else resulting_shanten - current_shanten
        )
        risk = candidate_defense_risk(example, tile_type)
        expected_point_impact = _expected_point_impact(shanten_delta, risk.risk)
        heuristic_logit = _candidate_logit(
            resulting_shanten,
            shanten_delta,
            risk.risk,
            expected_point_impact,
        )
        model_logit = model_logits.get(tile_type, 0.0)
        combined_logit = model_logit + heuristic_logit
        candidates.append(
            {
                "tile": tile_type.notation,
                "policy_probability": 0.0,
                "model_logit": round(model_logit, 6),
                "heuristic_logit": round(heuristic_logit, 6),
                "combined_logit": round(combined_logit, 6),
                "resulting_shanten": resulting_shanten,
                "shanten_delta": shanten_delta,
                "estimated_deal_in_risk": round(risk.risk, 6),
                "expected_point_impact": expected_point_impact,
                "reasons": _candidate_reasons(shanten_delta, risk),
            }
        )

    probabilities = _softmax([float(candidate["combined_logit"]) for candidate in candidates])
    for candidate, probability in zip(candidates, probabilities, strict=True):
        candidate["policy_probability"] = round(probability, 6)
    return sorted(
        candidates,
        key=lambda candidate: (
            -float(candidate["policy_probability"]),
            99 if candidate["resulting_shanten"] is None else int(candidate["resulting_shanten"]),
            float(candidate["estimated_deal_in_risk"]),
            str(candidate["tile"]),
        ),
    )


def _candidate_reasons(shanten_delta: int | None, risk: Any) -> list[str]:
    reasons = []
    if shanten_delta is None:
        reasons.append("shanten_unknown")
    elif shanten_delta < 0:
        reasons.append("improves_shanten")
    elif shanten_delta == 0:
        reasons.append("preserves_shanten")
    else:
        reasons.append("worsens_shanten")
    reasons.extend(risk.safety_reasons)
    reasons.extend(risk.danger_reasons)
    return reasons


def _model_logits(
    model: DiscardLinearModel | DiscardFrequencyBaseline,
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    example: DiscardExample,
) -> tuple[dict[TileType, float], str]:
    if isinstance(model, DiscardLinearModel):
        return (
            model.logits(
                hand_counts,
                visible_counts,
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
            ),
            model.kind,
        )

    preferred = model.predict(hand_counts)
    return (
        {
            TileType(index): 0.1 if index == preferred.index else 0.0
            for index, count in enumerate(hand_counts)
            if count > 0
        },
        "discard-frequency-fallback-v0",
    )


def _analysis_example(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    seat: int,
    dora_indicators: tuple[Tile, ...],
) -> DiscardExample:
    zero_counts = tuple([0] * 34)
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=seat,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=hand_counts,
        visible_counts=visible_counts,
        action=Action.discard(TileType(0)),
        active_riichi_seats=(False, False, False, False),
        river_counts_by_seat=(zero_counts, zero_counts, zero_counts, zero_counts),
        rivers_by_seat=((), (), (), ()),
        riichi_declared_turns=(None, None, None, None),
        riichi_declared_event_indices=(None, None, None, None),
        meld_counts_by_seat=(zero_counts, zero_counts, zero_counts, zero_counts),
        dora_indicators=dora_indicators,
        last_discard_tsumogiri_by_seat=(None, None, None, None),
        ippatsu_active_seats=(False, False, False, False),
    )


def _tiny_zero_linear_model(payload: dict[str, Any]) -> DiscardLinearModel:
    if payload.get("feature_profile") != RAW_COUNT_FEATURE_PROFILE:
        raise ValueError("tiny hand-analysis checkpoint must use raw-count features")
    if int(payload.get("feature_dim", -1)) != RAW_COUNT_FEATURE_DIM:
        raise ValueError("tiny hand-analysis checkpoint has unsupported feature dimension")
    if payload.get("weights") != "zeros":
        raise ValueError("tiny hand-analysis checkpoint only supports zero weights")
    weights = tuple(tuple(0.0 for _ in range(RAW_COUNT_FEATURE_DIM)) for _ in range(34))
    return DiscardLinearModel(
        weights=weights,
        epochs=int(payload.get("epochs", 0)),
        learning_rate=float(payload.get("learning_rate", 0.1)),
        feature_profile=RAW_COUNT_FEATURE_PROFILE,
        l2=float(payload.get("l2", 0.0)),
    )


def _parse_tile_group(token: str) -> tuple[Tile, ...]:
    token = token.strip()
    if not token:
        return ()
    if token in HONOR_NUMBERS.values():
        return (Tile.parse(token),)
    if token[-1] in {"m", "p", "s"} and token[:-1].isdigit():
        return tuple(Tile.parse(f"{rank}{token[-1]}") for rank in token[:-1])
    if token[-1] == "z" and token[:-1].isdigit():
        honors = []
        for rank in token[:-1]:
            try:
                honors.append(Tile.parse(HONOR_NUMBERS[rank]))
            except KeyError as error:
                raise ValueError(f"invalid honor rank in token: {token!r}") from error
        return tuple(honors)
    try:
        return (Tile.parse(token),)
    except (KeyError, ValueError) as error:
        raise ValueError(f"invalid tile token: {token!r}") from error


def _parse_round_wind(value: str) -> str:
    token = value.strip().upper()
    try:
        return ROUND_WINDS[token]
    except KeyError as error:
        raise ValueError("round must be E, S, W, N, East, South, West, or North") from error


def _safe_shanten(counts: Sequence[int]) -> int | None:
    try:
        return shanten(counts)
    except ValueError:
        return None


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


def _format_optional_int(value: Any) -> str:
    return "unknown" if value is None else str(int(value))


def _string_list(value: Any) -> list[str]:
    if not isinstance(value, list):
        return []
    return [item for item in value if isinstance(item, str)]
