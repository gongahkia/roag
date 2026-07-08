from __future__ import annotations

from typing import Any

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.hand_analysis import parse_hand_tiles
from kenjaku.training import (
    DiscardExample,
    actual_discard_has_kabe,
    actual_discard_has_one_chance,
    actual_discard_has_sotogawa,
    actual_discard_has_suji,
    actual_discard_is_genbutsu,
    actual_discard_seen_after_riichi,
    actual_discard_seen_before_riichi,
    candidate_defense_risk,
)

SAFETY_ADVISOR_KIND = "kenjaku-safety-advisor-v0"
SAFETY_ADVISOR_DISCLAIMER = (
    "Heuristic riichi-defense signals for ranking candidate discards; not calibrated deal-in odds."
)


def build_safety_advisor_report(
    *,
    hand: str,
    river: str,
    active_riichi: str,
    seat: int = 0,
) -> dict[str, Any]:
    if not 0 <= seat <= 3:
        raise ValueError("seat must be 0..3")
    hand_tiles = parse_hand_tiles(hand)
    river_tiles = () if not river.strip() else parse_hand_tiles(river)
    active = parse_active_riichi(active_riichi, seat=seat)
    hand_counts = tile_counts(hand_tiles)
    visible_counts = tile_counts((*hand_tiles, *river_tiles))
    if any(count > 4 for count in visible_counts):
        raise ValueError("hand plus river cannot contain more than four copies of a tile")

    candidates = []
    for index, count in enumerate(hand_counts):
        if count <= 0:
            continue
        tile_type = TileType(index)
        example = _example(
            hand_counts,
            visible_counts,
            river_tiles=river_tiles,
            active_riichi_seats=active,
            seat=seat,
            action=Action.discard(tile_type),
        )
        risk = candidate_defense_risk(example, tile_type)
        features = _feature_payload(example)
        candidates.append(
            {
                "tile": tile_type.notation,
                "copies": count,
                "safety_score": round(1.0 - risk.risk, 6),
                "estimated_deal_in_risk": round(risk.risk, 6),
                "calibrated_probability": risk.calibrated_probability,
                "active_riichi_opponents": risk.active_riichi_opponents,
                **features,
                "safety_reasons": list(risk.safety_reasons),
                "danger_reasons": list(risk.danger_reasons),
                "reasons": _candidate_reasons(features, risk),
            }
        )
    candidates.sort(
        key=lambda candidate: (
            float(candidate["estimated_deal_in_risk"]),
            -float(candidate["safety_score"]),
            str(candidate["tile"]),
        )
    )
    return {
        "kind": SAFETY_ADVISOR_KIND,
        "hand_tiles": [tile.notation for tile in hand_tiles],
        "river_tiles": [tile.notation for tile in river_tiles],
        "seat": seat,
        "active_riichi_seats": [index for index, is_active in enumerate(active) if is_active],
        "disclaimer": SAFETY_ADVISOR_DISCLAIMER,
        "candidates": candidates,
    }


def parse_active_riichi(value: str, *, seat: int = 0) -> tuple[bool, bool, bool, bool]:
    active = [False, False, False, False]
    text = value.strip()
    if not text or text.lower() in {"none", "-"}:
        return tuple(active)
    for part in text.split(","):
        token = part.strip()
        if not token:
            continue
        opponent = int(token)
        if not 0 <= opponent <= 3:
            raise ValueError("active riichi seats must be 0..3")
        if opponent == seat:
            continue
        active[opponent] = True
    return tuple(active)


def format_safety_advisor_text(report: dict[str, Any]) -> str:
    lines = [
        f"hand: {' '.join(_string_list(report.get('hand_tiles')))}",
        f"river: {' '.join(_string_list(report.get('river_tiles')))}",
        "active_riichi: " + ",".join(str(seat) for seat in report.get("active_riichi_seats", [])),
        "candidates:",
    ]
    for candidate in report.get("candidates", []):
        if not isinstance(candidate, dict):
            continue
        reasons = ", ".join(_string_list(candidate.get("reasons")))
        lines.append(
            "- "
            f"{candidate['tile']}: "
            f"safety={float(candidate['safety_score']):.3f} "
            f"risk={float(candidate['estimated_deal_in_risk']):.3f} "
            f"reasons={reasons}"
        )
    lines.append(f"disclaimer: {report.get('disclaimer', '')}")
    return "\n".join(lines)


def _example(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    river_tiles: tuple[Tile, ...],
    active_riichi_seats: tuple[bool, bool, bool, bool],
    seat: int,
    action: Action,
) -> DiscardExample:
    zero_counts = tuple([0] * 34)
    river_counts = tile_counts(river_tiles)
    rivers_by_seat = tuple(
        river_tiles if is_active and index != seat else ()
        for index, is_active in enumerate(active_riichi_seats)
    )
    river_counts_by_seat = tuple(
        river_counts if is_active and index != seat else zero_counts
        for index, is_active in enumerate(active_riichi_seats)
    )
    riichi_turns = tuple(
        len(river_tiles) if is_active and index != seat else None
        for index, is_active in enumerate(active_riichi_seats)
    )
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=seat,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=hand_counts,
        visible_counts=visible_counts,
        action=action,
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=riichi_turns,
        riichi_declared_event_indices=(None, None, None, None),
    )


def _feature_payload(example: DiscardExample) -> dict[str, bool]:
    return {
        "genbutsu": actual_discard_is_genbutsu(example),
        "suji": actual_discard_has_suji(example),
        "kabe": actual_discard_has_kabe(example),
        "one_chance": actual_discard_has_one_chance(example),
        "sotogawa": actual_discard_has_sotogawa(example),
        "seen_after_riichi": actual_discard_seen_after_riichi(example),
        "seen_before_riichi": actual_discard_seen_before_riichi(example),
    }


def _candidate_reasons(features: dict[str, bool], risk: Any) -> list[str]:
    reasons = [
        name
        for name in (
            "genbutsu",
            "suji",
            "kabe",
            "one_chance",
            "sotogawa",
            "seen_after_riichi",
            "seen_before_riichi",
        )
        if features[name]
    ]
    reasons.extend(reason for reason in risk.danger_reasons if reason not in reasons)
    if not reasons:
        reasons.append("no_safety_signal")
    return reasons


def _string_list(value: Any) -> list[str]:
    if not isinstance(value, list):
        return []
    return [item for item in value if isinstance(item, str)]
