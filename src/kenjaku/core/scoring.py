from __future__ import annotations

from collections.abc import Iterable, Sequence
from dataclasses import dataclass

from kenjaku.core.actions import ActionKind
from kenjaku.core.state import Meld
from kenjaku.core.tiles import Tile, TileType, tile_counts

WIN_KIND_RON = "ron"
WIN_KIND_TSUMO = "tsumo"
WIN_KIND_CHANKAN = "chankan"
WIN_KINDS = (WIN_KIND_RON, WIN_KIND_TSUMO, WIN_KIND_CHANKAN)

LIMIT_BASE_POINTS = {
    "mangan": 2000,
    "haneman": 3000,
    "baiman": 4000,
    "sanbaiman": 6000,
    "yakuman": 8000,
}


@dataclass(frozen=True, slots=True)
class FuComponent:
    reason: str
    fu: int


@dataclass(frozen=True, slots=True)
class FuCalculation:
    fu: int
    unrounded_fu: int
    components: tuple[FuComponent, ...]


@dataclass(frozen=True, slots=True)
class ScoreResult:
    yaku_han: int
    bonus_han: int
    han: int
    fu: int | None
    limit: str | None
    base_points: int
    is_dealer: bool
    win_kind: str
    ron_payment: int | None
    tsumo_child_payment: int | None
    tsumo_dealer_payment: int | None
    tsumo_payment_per_loser: int | None
    honba_payment: int
    riichi_stick_points: int
    total_ron_payment: int | None
    total_tsumo_child_payment: int | None
    total_tsumo_dealer_payment: int | None
    winner_total_points: int
    yakuman_multiplier: int = 0


@dataclass(frozen=True, slots=True)
class _Group:
    kind: str
    tiles: tuple[int, ...]


def score_riichi_hand(
    *,
    yaku_han: int,
    fu: int | None,
    is_dealer: bool,
    win_kind: str,
    bonus_han: int = 0,
    honba: int = 0,
    riichi_sticks: int = 0,
    players: int = 4,
    kiriage: bool = False,
    counted_yakuman: bool = True,
    yakuman_multiplier: int = 0,
) -> ScoreResult:
    if win_kind not in WIN_KINDS:
        raise ValueError(f"unsupported win kind: {win_kind}")
    if yaku_han < 0 or bonus_han < 0:
        raise ValueError("han values must be non-negative")
    if honba < 0 or riichi_sticks < 0:
        raise ValueError("honba and riichi sticks must be non-negative")
    if players < 2:
        raise ValueError("players must be at least 2")
    if yakuman_multiplier < 0:
        raise ValueError("yakuman multiplier must be non-negative")

    han = yaku_han + bonus_han
    if yakuman_multiplier > 0:
        limit = "yakuman"
        base_points = LIMIT_BASE_POINTS[limit] * yakuman_multiplier
        scoring_fu = None
    else:
        if han <= 0:
            raise ValueError("a scored hand must have at least one han")
        if fu is None:
            raise ValueError("fu is required for non-yakuman hands")
        if fu <= 0:
            raise ValueError("fu must be positive")
        scoring_fu = fu
        limit = score_limit(
            han=han,
            fu=fu,
            kiriage=kiriage,
            counted_yakuman=counted_yakuman,
        )
        base_points = LIMIT_BASE_POINTS[limit] if limit is not None else fu * (2 ** (han + 2))

    ron_payment = None
    tsumo_child_payment = None
    tsumo_dealer_payment = None
    tsumo_payment_per_loser = None
    if _is_ron_like(win_kind):
        ron_payment = ceil_to_hundred(base_points * (6 if is_dealer else 4))
    else:
        tsumo_child_payment = ceil_to_hundred(base_points * (2 if is_dealer else 1))
        tsumo_dealer_payment = None if is_dealer else ceil_to_hundred(base_points * 2)
        tsumo_payment_per_loser = tsumo_child_payment

    honba_payment = honba_payment_for_win(win_kind=win_kind, honba=honba)
    riichi_stick_points = riichi_sticks * 1000
    total_ron_payment = None if ron_payment is None else ron_payment + honba_payment
    total_tsumo_child_payment = (
        None if tsumo_child_payment is None else tsumo_child_payment + honba_payment
    )
    total_tsumo_dealer_payment = (
        None if tsumo_dealer_payment is None else tsumo_dealer_payment + honba_payment
    )
    if _is_ron_like(win_kind):
        winner_total_points = (total_ron_payment or 0) + riichi_stick_points
    elif is_dealer:
        winner_total_points = ((total_tsumo_child_payment or 0) * (players - 1)) + (
            riichi_stick_points
        )
    else:
        child_losers = players - 2
        winner_total_points = (
            (total_tsumo_child_payment or 0) * child_losers
            + (total_tsumo_dealer_payment or 0)
            + riichi_stick_points
        )

    return ScoreResult(
        yaku_han=yaku_han,
        bonus_han=bonus_han,
        han=han,
        fu=scoring_fu,
        limit=limit,
        base_points=base_points,
        is_dealer=is_dealer,
        win_kind=win_kind,
        ron_payment=ron_payment,
        tsumo_child_payment=tsumo_child_payment,
        tsumo_dealer_payment=tsumo_dealer_payment,
        tsumo_payment_per_loser=tsumo_payment_per_loser,
        honba_payment=honba_payment,
        riichi_stick_points=riichi_stick_points,
        total_ron_payment=total_ron_payment,
        total_tsumo_child_payment=total_tsumo_child_payment,
        total_tsumo_dealer_payment=total_tsumo_dealer_payment,
        winner_total_points=winner_total_points,
        yakuman_multiplier=yakuman_multiplier,
    )


def calculate_fu(
    concealed_tiles: Iterable[Tile | TileType],
    *,
    winning_tile: Tile | TileType,
    win_kind: str,
    melds: Sequence[Meld] = (),
    seat_wind: TileType | str | None = None,
    round_wind: TileType | str | None = None,
    yaku: Sequence[str] = (),
) -> FuCalculation:
    if win_kind not in WIN_KINDS:
        raise ValueError(f"unsupported win kind: {win_kind}")
    concealed = tuple(_tile_type(tile) for tile in concealed_tiles)
    winning_type = _tile_type(winning_tile)
    yaku_names = set(yaku)
    if "chiitoitsu" in yaku_names:
        return FuCalculation(
            fu=25,
            unrounded_fu=25,
            components=(FuComponent("chiitoitsu fixed fu", 25),),
        )

    meld_groups = tuple(_meld_group(meld) for meld in melds if meld.kind != ActionKind.KITA)
    standard_melds = tuple(group for group in meld_groups if group.kind != "kita")
    concealed_melds_needed = 4 - len(standard_melds)
    if concealed_melds_needed < 0:
        raise ValueError("too many melds for a standard hand")
    counts = tile_counts(concealed)
    candidates = _standard_groupings(counts, concealed_melds_needed)
    if not candidates:
        raise ValueError("concealed tiles do not form a standard winning hand with melds")

    seat_type = _optional_tile_type(seat_wind)
    round_type = _optional_tile_type(round_wind)
    best = max(
        (
            _fu_for_grouping(
                grouping,
                meld_groups=standard_melds,
                winning_tile=winning_type,
                win_kind=win_kind,
                seat_wind=seat_type,
                round_wind=round_type,
                yaku_names=yaku_names,
            )
            for grouping in candidates
        ),
        key=lambda calculation: (calculation.fu, calculation.unrounded_fu),
    )
    return best


def score_limit(
    *,
    han: int,
    fu: int,
    kiriage: bool = False,
    counted_yakuman: bool = True,
) -> str | None:
    if han <= 0:
        raise ValueError("han must be positive")
    if fu <= 0:
        raise ValueError("fu must be positive")
    if counted_yakuman and han >= 13:
        return "yakuman"
    if han >= 11:
        return "sanbaiman"
    if han >= 8:
        return "baiman"
    if han >= 6:
        return "haneman"
    if han >= 5:
        return "mangan"
    if kiriage and (han, fu) in {(4, 30), (3, 60)}:
        return "mangan"
    base_points = fu * (2 ** (han + 2))
    if base_points >= LIMIT_BASE_POINTS["mangan"]:
        return "mangan"
    return None


def ceil_to_hundred(points: int) -> int:
    if points < 0:
        raise ValueError("points must be non-negative")
    return ((points + 99) // 100) * 100


def round_fu(fu: int) -> int:
    if fu <= 0:
        raise ValueError("fu must be positive")
    return ((fu + 9) // 10) * 10


def honba_payment_for_win(*, win_kind: str, honba: int) -> int:
    if honba < 0:
        raise ValueError("honba must be non-negative")
    if win_kind == WIN_KIND_TSUMO:
        return honba * 100
    if _is_ron_like(win_kind):
        return honba * 300
    raise ValueError(f"unsupported win kind: {win_kind}")


def _is_ron_like(win_kind: str) -> bool:
    return win_kind in {WIN_KIND_RON, WIN_KIND_CHANKAN}


def _fu_for_grouping(
    grouping: tuple[_Group, ...],
    *,
    meld_groups: tuple[_Group, ...],
    winning_tile: TileType,
    win_kind: str,
    seat_wind: TileType | None,
    round_wind: TileType | None,
    yaku_names: set[str],
) -> FuCalculation:
    components = [FuComponent("base", 20)]
    closed_hand = all(group.kind == "ankan" for group in meld_groups)
    if closed_hand and win_kind == WIN_KIND_RON:
        components.append(FuComponent("closed ron", 10))

    pair_group = next(group for group in grouping if group.kind == "pair")
    pair_fu = _pair_fu(pair_group.tiles[0], seat_wind=seat_wind, round_wind=round_wind)
    if pair_fu:
        components.append(FuComponent("value pair", pair_fu))

    wait_fu = _wait_fu(grouping, winning_tile)
    if wait_fu:
        components.append(FuComponent("wait", wait_fu))

    for group in (*grouping, *meld_groups):
        meld_fu = _group_fu(group, winning_tile=winning_tile, win_kind=win_kind)
        if meld_fu:
            components.append(FuComponent(group.kind, meld_fu))

    inferred_pinfu = (
        closed_hand
        and pair_fu == 0
        and wait_fu == 0
        and all(group.kind in {"sequence", "pair"} for group in grouping)
        and all(group.kind == "sequence" for group in meld_groups)
    )
    has_pinfu = "pinfu" in yaku_names or inferred_pinfu
    if win_kind == WIN_KIND_TSUMO and not has_pinfu:
        components.append(FuComponent("tsumo", 2))

    unrounded = sum(component.fu for component in components)
    if unrounded == 20 and not closed_hand:
        components.append(FuComponent("open pinfu shape", 2))
        unrounded += 2
    return FuCalculation(
        fu=round_fu(unrounded),
        unrounded_fu=unrounded,
        components=tuple(components),
    )


def _standard_groupings(
    counts: Sequence[int],
    melds_needed: int,
) -> tuple[tuple[_Group, ...], ...]:
    checked = tuple(counts)
    groupings: list[tuple[_Group, ...]] = []
    for pair_index, count in enumerate(checked):
        if count < 2:
            continue
        working = list(checked)
        working[pair_index] -= 2
        for melds in _meld_groupings(tuple(working), melds_needed):
            groupings.append((_Group("pair", (pair_index, pair_index)), *melds))
    return tuple(dict.fromkeys(groupings))


def _meld_groupings(
    counts: tuple[int, ...],
    melds_needed: int,
) -> tuple[tuple[_Group, ...], ...]:
    if melds_needed == 0:
        return ((),) if not any(counts) else ()
    first = next((index for index, count in enumerate(counts) if count), None)
    if first is None:
        return ()

    results: list[tuple[_Group, ...]] = []
    if counts[first] >= 3:
        working = list(counts)
        working[first] -= 3
        group = _Group("triplet", (first, first, first))
        results.extend(
            (group, *child) for child in _meld_groupings(tuple(working), melds_needed - 1)
        )
    if _can_sequence(counts, first):
        working = list(counts)
        for index in (first, first + 1, first + 2):
            working[index] -= 1
        group = _Group("sequence", (first, first + 1, first + 2))
        results.extend(
            (group, *child) for child in _meld_groupings(tuple(working), melds_needed - 1)
        )
    return tuple(results)


def _group_fu(group: _Group, *, winning_tile: TileType, win_kind: str) -> int:
    if group.kind in {"pair", "sequence", "kita"}:
        return 0
    tile_index = group.tiles[0]
    terminal_or_honor = TileType(tile_index).is_terminal_or_honor
    if group.kind in {"minkan", "kakan"}:
        return 16 if terminal_or_honor else 8
    if group.kind == "ankan":
        return 32 if terminal_or_honor else 16
    if group.kind == "pon":
        return 4 if terminal_or_honor else 2
    if group.kind == "triplet":
        closed = not (win_kind == WIN_KIND_RON and winning_tile.index == tile_index)
        if closed:
            return 8 if terminal_or_honor else 4
        return 4 if terminal_or_honor else 2
    raise ValueError(f"unsupported group kind: {group.kind}")


def _pair_fu(
    tile_index: int,
    *,
    seat_wind: TileType | None,
    round_wind: TileType | None,
) -> int:
    tile_type = TileType(tile_index)
    fu = 0
    if tile_type.notation in {"P", "F", "C"}:
        fu += 2
    if seat_wind is not None and tile_type == seat_wind:
        fu += 2
    if round_wind is not None and tile_type == round_wind:
        fu += 2
    return fu


def _wait_fu(grouping: tuple[_Group, ...], winning_tile: TileType) -> int:
    for group in grouping:
        if group.kind == "pair" and group.tiles[0] == winning_tile.index:
            return 2
    for group in grouping:
        if group.kind != "sequence" or winning_tile.index not in group.tiles:
            continue
        ranks = [TileType(index).rank for index in group.tiles]
        if winning_tile.rank is None or any(rank is None for rank in ranks):
            continue
        first_rank = min(rank for rank in ranks if rank is not None)
        if winning_tile.rank == first_rank + 1:
            return 2
        if first_rank == 1 and winning_tile.rank == 3:
            return 2
        if first_rank == 7 and winning_tile.rank == 7:
            return 2
    return 0


def _meld_group(meld: Meld) -> _Group:
    tile_indices = tuple(tile.type.index for tile in meld.tiles)
    if meld.kind == ActionKind.CHI:
        return _Group("sequence", tuple(sorted(tile_indices)))
    if meld.kind == ActionKind.PON:
        return _Group("pon", tile_indices)
    if meld.kind == ActionKind.MINKAN:
        return _Group("minkan", tile_indices)
    if meld.kind == ActionKind.ANKAN:
        return _Group("ankan", tile_indices)
    if meld.kind == ActionKind.KAKAN:
        return _Group("kakan", tile_indices)
    if meld.kind == ActionKind.KITA:
        return _Group("kita", tile_indices)
    raise ValueError(f"unsupported meld kind for scoring: {meld.kind.value}")


def _can_sequence(counts: tuple[int, ...], tile_index: int) -> bool:
    return (
        tile_index < 27
        and tile_index % 9 <= 6
        and counts[tile_index + 1] > 0
        and counts[tile_index + 2] > 0
    )


def _tile_type(tile: Tile | TileType) -> TileType:
    return tile.type if isinstance(tile, Tile) else tile


def _optional_tile_type(tile: TileType | str | None) -> TileType | None:
    if tile is None:
        return None
    if isinstance(tile, TileType):
        return tile
    return TileType.parse(tile)
