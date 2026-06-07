from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from kenjaku.io import TenhouRound


@dataclass(frozen=True, slots=True)
class RoundOutcome:
    """Terminal round labels parsed from Tenhou metadata."""

    kind: str
    event_index: int
    winner_seats: tuple[int, ...]
    from_seats: tuple[int, ...]
    score_deltas: tuple[int, ...] | None
    win_flags: tuple[bool, ...]
    deal_in_flags: tuple[bool, ...]
    draw_flags: tuple[bool, ...]

    def to_payload(self) -> dict[str, Any]:
        return {
            "kind": self.kind,
            "event_index": self.event_index,
            "winner_seats": list(self.winner_seats),
            "from_seats": list(self.from_seats),
            "score_deltas": None if self.score_deltas is None else list(self.score_deltas),
            "win_flags": list(self.win_flags),
            "deal_in_flags": list(self.deal_in_flags),
            "draw_flags": list(self.draw_flags),
        }


def round_outcome(round_: TenhouRound) -> RoundOutcome | None:
    players = len(round_.scores) if round_.scores else len(round_.starting_hands)
    if round_.agari:
        winners = tuple(agari.winner for agari in round_.agari)
        from_seats = tuple(agari.from_seat for agari in round_.agari)
        score_deltas = next(
            (
                agari.score_deltas
                for agari in reversed(round_.agari)
                if agari.score_deltas is not None
            ),
            None,
        )
        return RoundOutcome(
            kind="agari",
            event_index=round_.agari[-1].event_index,
            winner_seats=winners,
            from_seats=from_seats,
            score_deltas=score_deltas,
            win_flags=tuple(seat in winners for seat in range(players)),
            deal_in_flags=tuple(
                any(
                    agari.from_seat == seat and agari.from_seat != agari.winner
                    for agari in round_.agari
                )
                for seat in range(players)
            ),
            draw_flags=tuple(False for _ in range(players)),
        )
    if round_.ryuukyoku is not None:
        return RoundOutcome(
            kind="ryuukyoku",
            event_index=round_.ryuukyoku.event_index,
            winner_seats=(),
            from_seats=(),
            score_deltas=round_.ryuukyoku.score_deltas,
            win_flags=tuple(False for _ in range(players)),
            deal_in_flags=tuple(False for _ in range(players)),
            draw_flags=tuple(True for _ in range(players)),
        )
    return None


def round_outcome_payload(round_: TenhouRound) -> dict[str, Any] | None:
    outcome = round_outcome(round_)
    if outcome is None:
        return None
    return outcome.to_payload()
