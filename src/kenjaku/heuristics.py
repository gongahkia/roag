from __future__ import annotations

from collections.abc import Iterable, Sequence
from dataclasses import dataclass

from kenjaku.core import TENHOU_4P, Action, ActionKind, Tile, TileType, tile_counts
from kenjaku.evaluator import evaluate_hand_value_potential, evaluate_shanten_ukeire
from kenjaku.models.linear_call import (
    CALL_DECISION_KINDS,
    _candidate_proxy,
    _safe_shanten,
    _ukeire_proxy,
)
from kenjaku.simulation.environment import SandboxEnvironmentState, legal_sandbox_actions
from kenjaku.training import CallExample


@dataclass(frozen=True, slots=True)
class HeuristicFactor:
    name: str
    value: float
    contribution: float


@dataclass(frozen=True, slots=True)
class HeuristicDiscardCandidate:
    tile: TileType
    score: float
    factors: tuple[HeuristicFactor, ...]


@dataclass(frozen=True, slots=True)
class HeuristicCallCandidate:
    kind: ActionKind
    score: float
    factors: tuple[HeuristicFactor, ...]


@dataclass(frozen=True, slots=True)
class HeuristicActionCandidate:
    action: Action
    score: float
    factors: tuple[HeuristicFactor, ...]


def rank_discard_heuristic(
    tiles: Iterable[Tile],
    *,
    ruleset: str = TENHOU_4P.name,
    visible_counts: Sequence[int] | None = None,
) -> tuple[HeuristicDiscardCandidate, ...]:
    """Rank legal discard types with structured efficiency and value factors."""
    hand = tuple(tiles)
    counts = tile_counts(hand)
    if len(hand) != 14:
        raise ValueError("discard ranking requires exactly fourteen tiles")
    visible = counts if visible_counts is None else tuple(visible_counts)
    candidates: list[HeuristicDiscardCandidate] = []
    for tile in sorted({item.type for item in hand}, key=lambda item: item.index):
        after_tiles = _remove_one_tile(hand, tile)
        efficiency = evaluate_shanten_ukeire(
            tile_counts(after_tiles),
            ruleset=ruleset,
            visible_counts=visible,
        )
        value = evaluate_hand_value_potential(after_tiles, ruleset=ruleset)
        factors = (
            HeuristicFactor("shanten", float(efficiency.shanten), -float(efficiency.shanten)),
            HeuristicFactor("ukeire", float(efficiency.ukeire), efficiency.ukeire / 16.0),
            HeuristicFactor(
                "bonus_han",
                float(value.visible_dora + value.red_dora),
                float(value.visible_dora + value.red_dora) / 2.0,
            ),
            HeuristicFactor(
                "structural_yaku",
                float(len(value.potential_yaku)),
                len(value.potential_yaku) / 4.0,
            ),
        )
        candidates.append(
            HeuristicDiscardCandidate(
                tile=tile,
                score=sum(factor.contribution for factor in factors),
                factors=factors,
            )
        )
    return tuple(
        sorted(
            candidates,
            key=lambda candidate: (
                _shanten_factor(candidate),
                -candidate.score,
                candidate.tile.index,
            ),
        )
    )


def rank_call_pass_heuristic(example: CallExample) -> tuple[HeuristicCallCandidate, ...]:
    """Rank pass and legal calls with deterministic post-call proxy factors."""
    before_shanten = _safe_shanten(example.hand_counts)
    candidates: list[HeuristicCallCandidate] = []
    for kind in _call_candidate_kinds(example):
        proxy = _candidate_proxy(example, kind, prefer_ukeire_tiebreaker=True)
        after_shanten = _safe_shanten(proxy.after_counts)
        ukeire = _ukeire_proxy(proxy.after_counts)
        factors = (
            HeuristicFactor(
                "after_shanten_proxy",
                float(after_shanten),
                -float(after_shanten),
            ),
            HeuristicFactor(
                "shanten_delta_proxy",
                float(after_shanten - before_shanten),
                float(before_shanten - after_shanten),
            ),
            HeuristicFactor("ukeire_proxy", float(ukeire), ukeire / 16.0),
            HeuristicFactor(
                "consumed_count",
                float(proxy.consumed_count),
                -proxy.consumed_count / 4.0,
            ),
        )
        candidates.append(
            HeuristicCallCandidate(
                kind=kind,
                score=sum(factor.contribution for factor in factors),
                factors=factors,
            )
        )
    return tuple(
        sorted(
            candidates,
            key=lambda candidate: (
                _call_shanten_factor(candidate),
                -candidate.score,
                CALL_DECISION_KINDS.index(candidate.kind),
            ),
        )
    )


def rank_special_action_heuristic(
    state: SandboxEnvironmentState,
    *,
    seat: int | None = None,
) -> tuple[HeuristicActionCandidate, ...]:
    """Rank legal riichi, kan, Kita, hora, and pass actions by fixed priority.

    The result is a deterministic priority heuristic, not a win-probability or
    expected-value estimate.
    """
    candidates: list[HeuristicActionCandidate] = []
    for action in legal_sandbox_actions(state, seat=seat):
        if action.kind not in _SPECIAL_ACTION_KINDS:
            continue
        factors = _special_action_factors(state, action)
        candidates.append(
            HeuristicActionCandidate(
                action=action,
                score=sum(factor.contribution for factor in factors),
                factors=factors,
            )
        )
    return tuple(
        sorted(
            candidates,
            key=lambda candidate: (
                -candidate.score,
                _SPECIAL_ACTION_KIND_ORDER.index(candidate.action.kind),
                -1 if candidate.action.tile is None else candidate.action.tile.index,
            ),
        )
    )


def _remove_one_tile(tiles: tuple[Tile, ...], tile_type: TileType) -> tuple[Tile, ...]:
    for index, tile in enumerate(tiles):
        if tile.type == tile_type and not tile.red:
            return (*tiles[:index], *tiles[index + 1 :])
    for index, tile in enumerate(tiles):
        if tile.type == tile_type:
            return (*tiles[:index], *tiles[index + 1 :])
    raise ValueError("discard tile must be in hand")


def _shanten_factor(candidate: HeuristicDiscardCandidate) -> float:
    return candidate.factors[0].value


def _call_candidate_kinds(example: CallExample) -> tuple[ActionKind, ...]:
    return tuple(
        kind
        for kind in CALL_DECISION_KINDS
        if kind == ActionKind.PASS or kind in example.legal_call_kinds
    )


def _call_shanten_factor(candidate: HeuristicCallCandidate) -> float:
    return candidate.factors[0].value


_SPECIAL_ACTION_KINDS = frozenset(
    {
        ActionKind.RIICHI,
        ActionKind.ANKAN,
        ActionKind.KAKAN,
        ActionKind.KITA,
        ActionKind.RON,
        ActionKind.TSUMO,
        ActionKind.PASS,
    }
)
_SPECIAL_ACTION_KIND_ORDER = (
    ActionKind.TSUMO,
    ActionKind.RON,
    ActionKind.RIICHI,
    ActionKind.ANKAN,
    ActionKind.KAKAN,
    ActionKind.KITA,
    ActionKind.PASS,
)


def _special_action_factors(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[HeuristicFactor, ...]:
    is_hora = action.kind in {ActionKind.RON, ActionKind.TSUMO}
    is_riichi = action.kind is ActionKind.RIICHI
    is_replacement_draw = action.kind in {ActionKind.ANKAN, ActionKind.KAKAN, ActionKind.KITA}
    riichi_cost = state.rule_config.riichi_deposit_points if is_riichi else 0
    return (
        HeuristicFactor("terminal_hora", float(is_hora), 100.0 if is_hora else 0.0),
        HeuristicFactor("riichi_declaration", float(is_riichi), 2.0 if is_riichi else 0.0),
        HeuristicFactor("riichi_deposit_kpoints", riichi_cost / 1000.0, -riichi_cost / 1000.0),
        HeuristicFactor(
            "replacement_draw",
            float(is_replacement_draw),
            0.25 if is_replacement_draw else 0.0,
        ),
        HeuristicFactor("pass_action", float(action.kind is ActionKind.PASS), 0.0),
    )
