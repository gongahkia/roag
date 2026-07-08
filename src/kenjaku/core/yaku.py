from __future__ import annotations

from collections import Counter
from collections.abc import Callable, Iterable, Sequence
from dataclasses import dataclass

from kenjaku.core.actions import ActionKind
from kenjaku.core.scoring import WIN_KIND_CHANKAN, WIN_KIND_RON, WIN_KINDS
from kenjaku.core.state import Meld
from kenjaku.core.tiles import Tile, TileType, tile_counts

YAKU_CLOSED_ONLY = "closed_only"
YAKU_OPEN_ALLOWED = "open_allowed"
YAKU_OPEN_REDUCED = "open_reduced"

DRAGON_TYPES = frozenset(TileType.parse(token) for token in ("P", "F", "C"))
WIND_TYPES = frozenset(TileType.parse(token) for token in ("E", "S", "W", "N"))
GREEN_TYPES = frozenset(TileType.parse(token) for token in ("2s", "3s", "4s", "6s", "8s", "F"))


@dataclass(frozen=True, slots=True)
class YakuDefinition:
    name: str
    closed_han: int | None
    open_han: int | None
    category: str
    yakuman_multiplier: int = 0

    @property
    def is_yakuman(self) -> bool:
        return self.yakuman_multiplier > 0

    def han_for_closed_state(self, *, is_closed: bool) -> int:
        if self.is_yakuman:
            return 0
        han = self.closed_han if is_closed else self.open_han
        if han is None:
            return 0
        return han


@dataclass(frozen=True, slots=True)
class YakuResult:
    name: str
    han: int
    yakuman_multiplier: int = 0

    @property
    def is_yakuman(self) -> bool:
        return self.yakuman_multiplier > 0


@dataclass(frozen=True, slots=True)
class _Group:
    kind: str
    tiles: tuple[int, ...]
    concealed: bool
    quad: bool = False

    @property
    def tile_type(self) -> TileType:
        return TileType(self.tiles[0])

    @property
    def is_sequence(self) -> bool:
        return self.kind == "sequence"

    @property
    def is_pair(self) -> bool:
        return self.kind == "pair"

    @property
    def is_triplet_like(self) -> bool:
        return self.kind == "triplet"


YAKU_DEFINITIONS = {
    "riichi": YakuDefinition("riichi", 1, None, YAKU_CLOSED_ONLY),
    "ippatsu": YakuDefinition("ippatsu", 1, None, YAKU_CLOSED_ONLY),
    "menzen_tsumo": YakuDefinition("menzen_tsumo", 1, None, YAKU_CLOSED_ONLY),
    "pinfu": YakuDefinition("pinfu", 1, None, YAKU_CLOSED_ONLY),
    "iipeikou": YakuDefinition("iipeikou", 1, None, YAKU_CLOSED_ONLY),
    "double_riichi": YakuDefinition("double_riichi", 2, None, YAKU_CLOSED_ONLY),
    "chiitoitsu": YakuDefinition("chiitoitsu", 2, None, YAKU_CLOSED_ONLY),
    "ryanpeikou": YakuDefinition("ryanpeikou", 3, None, YAKU_CLOSED_ONLY),
    "tanyao": YakuDefinition("tanyao", 1, 1, YAKU_OPEN_ALLOWED),
    "yakuhai": YakuDefinition("yakuhai", 1, 1, YAKU_OPEN_ALLOWED),
    "rinshan": YakuDefinition("rinshan", 1, 1, YAKU_OPEN_ALLOWED),
    "haitei": YakuDefinition("haitei", 1, 1, YAKU_OPEN_ALLOWED),
    "houtei": YakuDefinition("houtei", 1, 1, YAKU_OPEN_ALLOWED),
    "chankan": YakuDefinition("chankan", 1, 1, YAKU_OPEN_ALLOWED),
    "toitoi": YakuDefinition("toitoi", 2, 2, YAKU_OPEN_ALLOWED),
    "sanankou": YakuDefinition("sanankou", 2, 2, YAKU_OPEN_ALLOWED),
    "sanshoku_doukou": YakuDefinition("sanshoku_doukou", 2, 2, YAKU_OPEN_ALLOWED),
    "sankantsu": YakuDefinition("sankantsu", 2, 2, YAKU_OPEN_ALLOWED),
    "shousangen": YakuDefinition("shousangen", 2, 2, YAKU_OPEN_ALLOWED),
    "honroutou": YakuDefinition("honroutou", 2, 2, YAKU_OPEN_ALLOWED),
    "sanshoku_doujun": YakuDefinition("sanshoku_doujun", 2, 1, YAKU_OPEN_REDUCED),
    "ittsu": YakuDefinition("ittsu", 2, 1, YAKU_OPEN_REDUCED),
    "chanta": YakuDefinition("chanta", 2, 1, YAKU_OPEN_REDUCED),
    "junchan": YakuDefinition("junchan", 3, 2, YAKU_OPEN_REDUCED),
    "honitsu": YakuDefinition("honitsu", 3, 2, YAKU_OPEN_REDUCED),
    "chinitsu": YakuDefinition("chinitsu", 6, 5, YAKU_OPEN_REDUCED),
    "kokushi": YakuDefinition("kokushi", None, None, YAKU_CLOSED_ONLY, 1),
    "daisangen": YakuDefinition("daisangen", None, None, YAKU_OPEN_ALLOWED, 1),
    "suuankou": YakuDefinition("suuankou", None, None, YAKU_CLOSED_ONLY, 1),
    "shousuushi": YakuDefinition("shousuushi", None, None, YAKU_OPEN_ALLOWED, 1),
    "daisuushi": YakuDefinition("daisuushi", None, None, YAKU_OPEN_ALLOWED, 1),
    "tsuuiisou": YakuDefinition("tsuuiisou", None, None, YAKU_OPEN_ALLOWED, 1),
    "chinroutou": YakuDefinition("chinroutou", None, None, YAKU_OPEN_ALLOWED, 1),
    "ryuuiisou": YakuDefinition("ryuuiisou", None, None, YAKU_OPEN_ALLOWED, 1),
    "chuuren": YakuDefinition("chuuren", None, None, YAKU_CLOSED_ONLY, 1),
    "suukantsu": YakuDefinition("suukantsu", None, None, YAKU_OPEN_ALLOWED, 1),
    "tenhou": YakuDefinition("tenhou", None, None, YAKU_CLOSED_ONLY, 1),
    "chiihou": YakuDefinition("chiihou", None, None, YAKU_CLOSED_ONLY, 1),
    "nagashi_mangan": YakuDefinition("nagashi_mangan", 5, 5, YAKU_OPEN_ALLOWED),
}

SUPPORTED_YAKU_NAMES = tuple(YAKU_DEFINITIONS)
UNSUPPORTED_YAKU_NAMES = (
    "renhou",
    "open_riichi",
    "daisharin",
    "daichisei",
)
YAKUMAN_YAKU_NAMES = tuple(
    name for name, definition in YAKU_DEFINITIONS.items() if definition.is_yakuman
)


def detect_yaku(
    concealed_tiles: Iterable[Tile | TileType],
    *,
    winning_tile: Tile | TileType | None = None,
    win_kind: str,
    melds: Sequence[Meld] = (),
    seat_wind: TileType | str | None = None,
    round_wind: TileType | str | None = None,
    tenhou: bool = False,
    chiihou: bool = False,
) -> tuple[YakuResult, ...]:
    if win_kind not in WIN_KINDS:
        raise ValueError(f"unsupported win kind: {win_kind}")
    concealed = tuple(_tile_type(tile) for tile in concealed_tiles)
    winning_type = _optional_tile_type(winning_tile)
    if winning_type is not None:
        concealed = (*concealed, winning_type)
    meld_groups = tuple(_meld_group(meld) for meld in melds if meld.kind != ActionKind.KITA)
    all_tiles = (*concealed, *_all_meld_tile_types(melds))
    is_closed = _is_closed_hand(melds)
    seat_type = _optional_tile_type(seat_wind)
    round_type = _optional_tile_type(round_wind)

    results: list[YakuResult] = []
    _append_result(results, "tenhou", is_closed=is_closed, enabled=tenhou)
    _append_result(results, "chiihou", is_closed=is_closed, enabled=chiihou)
    _append_result(
        results,
        "chiitoitsu",
        is_closed=is_closed,
        enabled=is_closed and not meld_groups and _is_chiitoitsu(concealed),
    )
    if _is_tanyao(all_tiles):
        _append_result(results, "tanyao", is_closed=is_closed)

    groupings = _standard_groupings_for_yaku(
        concealed,
        meld_groups=meld_groups,
        winning_tile=winning_type,
        win_kind=win_kind,
    )
    for yaku_name in _standard_yaku_names(
        groupings,
        all_tiles=all_tiles,
        is_closed=is_closed,
        winning_tile=winning_type,
        seat_wind=seat_type,
        round_wind=round_type,
    ):
        _append_result(results, yaku_name, is_closed=is_closed)
    if _is_honroutou(all_tiles):
        _append_result(results, "honroutou", is_closed=is_closed)

    _append_repeated_results(
        results,
        "yakuhai",
        count=_yakuhai_count(groupings, seat_wind=seat_type, round_wind=round_type),
        is_closed=is_closed,
    )
    return tuple(results)


def yaku_han_for_names(names: Iterable[str], *, is_closed: bool) -> int:
    return sum(
        YAKU_DEFINITIONS[name].han_for_closed_state(is_closed=is_closed)
        for name in names
        if name in YAKU_DEFINITIONS
    )


def yakuman_multiplier_for_names(names: Iterable[str]) -> int:
    return sum(
        YAKU_DEFINITIONS[name].yakuman_multiplier for name in names if name in YAKU_DEFINITIONS
    )


def _standard_yaku_names(
    groupings: tuple[tuple[_Group, ...], ...],
    *,
    all_tiles: tuple[TileType, ...],
    is_closed: bool,
    winning_tile: TileType | None,
    seat_wind: TileType | None,
    round_wind: TileType | None,
) -> tuple[str, ...]:
    names: list[str] = []
    if any(
        _is_pinfu_grouping(grouping, winning_tile, seat_wind, round_wind) for grouping in groupings
    ):
        names.append("pinfu")
    if is_closed:
        if any(_has_ryanpeikou(grouping) for grouping in groupings):
            names.append("ryanpeikou")
        elif any(_has_iipeikou(grouping) for grouping in groupings):
            names.append("iipeikou")
    if any(_has_sanshoku_doujun(grouping) for grouping in groupings):
        names.append("sanshoku_doujun")
    if any(_has_sanshoku_doukou(grouping) for grouping in groupings):
        names.append("sanshoku_doukou")
    if any(_has_ittsu(grouping) for grouping in groupings):
        names.append("ittsu")
    has_junchan = any(_is_junchan(grouping) for grouping in groupings)
    if has_junchan:
        names.append("junchan")
    elif any(_is_chanta(grouping) for grouping in groupings):
        names.append("chanta")
    if any(_has_toitoi(grouping) for grouping in groupings):
        names.append("toitoi")
    if any(_has_sanankou(grouping) for grouping in groupings):
        names.append("sanankou")
    if any(_has_sankantsu(grouping) for grouping in groupings):
        names.append("sankantsu")
    if any(_has_shousangen(grouping) for grouping in groupings):
        names.append("shousangen")
    names.extend(_flush_yaku_names(all_tiles))
    names.extend(_yakuman_yaku_names(groupings, all_tiles=all_tiles, is_closed=is_closed))
    return tuple(dict.fromkeys(names))


def _yakuman_yaku_names(
    groupings: tuple[tuple[_Group, ...], ...],
    *,
    all_tiles: tuple[TileType, ...],
    is_closed: bool,
) -> tuple[str, ...]:
    names: list[str] = []
    if any(_has_daisangen(grouping) for grouping in groupings):
        names.append("daisangen")
    if any(_has_suuankou(grouping) for grouping in groupings):
        names.append("suuankou")
    if any(_has_shousuushi(grouping) for grouping in groupings):
        names.append("shousuushi")
    if any(_has_daisuushi(grouping) for grouping in groupings):
        names.append("daisuushi")
    if _all_tiles_are(all_tiles, lambda tile: tile.is_honor):
        names.append("tsuuiisou")
    if _all_tiles_are(all_tiles, lambda tile: tile.is_terminal):
        names.append("chinroutou")
    if _all_tiles_are(all_tiles, lambda tile: tile in GREEN_TYPES):
        names.append("ryuuiisou")
    if any(_has_suukantsu(grouping) for grouping in groupings):
        names.append("suukantsu")
    if is_closed and _is_chuuren(all_tiles):
        names.append("chuuren")
    return tuple(names)


def _standard_groupings_for_yaku(
    concealed_tiles: tuple[TileType, ...],
    *,
    meld_groups: tuple[_Group, ...],
    winning_tile: TileType | None,
    win_kind: str,
) -> tuple[tuple[_Group, ...], ...]:
    standard_melds = tuple(group for group in meld_groups if group.kind != "kita")
    concealed_melds_needed = 4 - len(standard_melds)
    if concealed_melds_needed < 0:
        return ()
    groupings = _standard_groupings(tile_counts(concealed_tiles), concealed_melds_needed)
    return tuple(
        (
            *_mark_ron_triplet(
                grouping,
                winning_tile=winning_tile,
                win_kind=win_kind,
            ),
            *standard_melds,
        )
        for grouping in groupings
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
            groupings.append((_Group("pair", (pair_index, pair_index), True), *melds))
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
        group = _Group("triplet", (first, first, first), True)
        results.extend(
            (group, *child) for child in _meld_groupings(tuple(working), melds_needed - 1)
        )
    if _can_sequence(counts, first):
        working = list(counts)
        for index in (first, first + 1, first + 2):
            working[index] -= 1
        group = _Group("sequence", (first, first + 1, first + 2), True)
        results.extend(
            (group, *child) for child in _meld_groupings(tuple(working), melds_needed - 1)
        )
    return tuple(results)


def _mark_ron_triplet(
    grouping: tuple[_Group, ...],
    *,
    winning_tile: TileType | None,
    win_kind: str,
) -> tuple[_Group, ...]:
    if winning_tile is None or win_kind not in {WIN_KIND_RON, WIN_KIND_CHANKAN}:
        return grouping
    return tuple(
        _Group(group.kind, group.tiles, False, group.quad)
        if group.is_triplet_like and group.tile_type == winning_tile
        else group
        for group in grouping
    )


def _is_pinfu_grouping(
    grouping: tuple[_Group, ...],
    winning_tile: TileType | None,
    seat_wind: TileType | None,
    round_wind: TileType | None,
) -> bool:
    if winning_tile is None:
        return False
    if not all(group.is_sequence or group.is_pair for group in grouping):
        return False
    pair = next(group for group in grouping if group.is_pair)
    if _pair_is_value(pair.tile_type, seat_wind=seat_wind, round_wind=round_wind):
        return False
    return _wait_fu(grouping, winning_tile) == 0


def _has_iipeikou(grouping: tuple[_Group, ...]) -> bool:
    return _identical_sequence_pair_count(grouping) == 1


def _has_ryanpeikou(grouping: tuple[_Group, ...]) -> bool:
    return _identical_sequence_pair_count(grouping) >= 2


def _identical_sequence_pair_count(grouping: tuple[_Group, ...]) -> int:
    sequence_counts = Counter(
        group.tiles for group in grouping if group.is_sequence and group.concealed
    )
    return sum(count // 2 for count in sequence_counts.values())


def _has_sanshoku_doujun(grouping: tuple[_Group, ...]) -> bool:
    by_rank: dict[int, set[str]] = {}
    for group in grouping:
        if not group.is_sequence:
            continue
        first = TileType(group.tiles[0])
        rank = first.rank
        if rank is None:
            continue
        by_rank.setdefault(rank, set()).add(first.suit)
    return any(suits == {"m", "p", "s"} for suits in by_rank.values())


def _has_sanshoku_doukou(grouping: tuple[_Group, ...]) -> bool:
    by_rank: dict[int, set[str]] = {}
    for group in grouping:
        if not group.is_triplet_like:
            continue
        tile_type = group.tile_type
        if tile_type.rank is None:
            continue
        by_rank.setdefault(tile_type.rank, set()).add(tile_type.suit)
    return any(suits == {"m", "p", "s"} for suits in by_rank.values())


def _has_ittsu(grouping: tuple[_Group, ...]) -> bool:
    by_suit: dict[str, set[int]] = {}
    for group in grouping:
        if not group.is_sequence:
            continue
        tile_type = TileType(group.tiles[0])
        rank = tile_type.rank
        if rank is not None:
            by_suit.setdefault(tile_type.suit, set()).add(rank)
    return any({1, 4, 7}.issubset(ranks) for ranks in by_suit.values())


def _is_chanta(grouping: tuple[_Group, ...]) -> bool:
    return any(group.is_sequence for group in grouping) and all(
        _group_has_terminal_or_honor(group) for group in grouping
    )


def _is_junchan(grouping: tuple[_Group, ...]) -> bool:
    return any(group.is_sequence for group in grouping) and all(
        _group_has_terminal(group) and not _group_has_honor(group) for group in grouping
    )


def _has_toitoi(grouping: tuple[_Group, ...]) -> bool:
    return _triplet_count(grouping) == 4


def _has_sanankou(grouping: tuple[_Group, ...]) -> bool:
    return _concealed_triplet_count(grouping) >= 3


def _has_sankantsu(grouping: tuple[_Group, ...]) -> bool:
    return _quad_count(grouping) >= 3


def _has_shousangen(grouping: tuple[_Group, ...]) -> bool:
    dragon_triplets = _typed_triplet_count(grouping, DRAGON_TYPES)
    dragon_pair = any(group.is_pair and group.tile_type in DRAGON_TYPES for group in grouping)
    return dragon_triplets == 2 and dragon_pair


def _has_daisangen(grouping: tuple[_Group, ...]) -> bool:
    return _typed_triplet_count(grouping, DRAGON_TYPES) == 3


def _has_suuankou(grouping: tuple[_Group, ...]) -> bool:
    return _triplet_count(grouping) == 4 and _concealed_triplet_count(grouping) == 4


def _has_shousuushi(grouping: tuple[_Group, ...]) -> bool:
    wind_triplets = _typed_triplet_count(grouping, WIND_TYPES)
    wind_pair = any(group.is_pair and group.tile_type in WIND_TYPES for group in grouping)
    return wind_triplets == 3 and wind_pair


def _has_daisuushi(grouping: tuple[_Group, ...]) -> bool:
    return _typed_triplet_count(grouping, WIND_TYPES) == 4


def _has_suukantsu(grouping: tuple[_Group, ...]) -> bool:
    return _quad_count(grouping) == 4


def _flush_yaku_names(tiles: tuple[TileType, ...]) -> tuple[str, ...]:
    if not tiles:
        return ()
    suits = {tile.suit for tile in tiles if not tile.is_honor}
    has_honor = any(tile.is_honor for tile in tiles)
    if len(suits) != 1:
        return ()
    return ("honitsu",) if has_honor else ("chinitsu",)


def _is_chuuren(tiles: tuple[TileType, ...]) -> bool:
    if len(tiles) != 14 or any(tile.is_honor for tile in tiles):
        return False
    suits = {tile.suit for tile in tiles}
    if len(suits) != 1:
        return False
    counts = Counter(tile.rank for tile in tiles)
    if counts[1] < 3 or counts[9] < 3:
        return False
    return all(counts[rank] >= 1 for rank in range(2, 9))


def _yakuhai_count(
    groupings: tuple[tuple[_Group, ...], ...],
    *,
    seat_wind: TileType | None,
    round_wind: TileType | None,
) -> int:
    return max(
        (
            sum(
                1
                for group in grouping
                if group.is_triplet_like
                and _triplet_is_yakuhai(group.tile_type, seat_wind, round_wind)
            )
            for grouping in groupings
        ),
        default=0,
    )


def _triplet_is_yakuhai(
    tile_type: TileType,
    seat_wind: TileType | None,
    round_wind: TileType | None,
) -> bool:
    return (
        tile_type in DRAGON_TYPES
        or (seat_wind is not None and tile_type == seat_wind)
        or (round_wind is not None and tile_type == round_wind)
    )


def _pair_is_value(
    tile_type: TileType,
    *,
    seat_wind: TileType | None,
    round_wind: TileType | None,
) -> bool:
    return _triplet_is_yakuhai(tile_type, seat_wind, round_wind)


def _is_chiitoitsu(tiles: tuple[TileType, ...]) -> bool:
    counts = tuple(count for count in tile_counts(tiles) if count > 0)
    return len(tiles) == 14 and len(counts) == 7 and all(count == 2 for count in counts)


def _is_tanyao(tiles: tuple[TileType, ...]) -> bool:
    return bool(tiles) and all(not tile.is_terminal_or_honor for tile in tiles)


def _is_honroutou(tiles: tuple[TileType, ...]) -> bool:
    return (
        bool(tiles)
        and all(tile.is_terminal_or_honor for tile in tiles)
        and any(tile.is_terminal for tile in tiles)
        and any(tile.is_honor for tile in tiles)
    )


def _all_tiles_are(
    tiles: tuple[TileType, ...],
    predicate: Callable[[TileType], bool],
) -> bool:
    return bool(tiles) and all(predicate(tile) for tile in tiles)


def _group_has_terminal_or_honor(group: _Group) -> bool:
    return any(TileType(index).is_terminal_or_honor for index in group.tiles)


def _group_has_terminal(group: _Group) -> bool:
    return any(TileType(index).is_terminal for index in group.tiles)


def _group_has_honor(group: _Group) -> bool:
    return any(TileType(index).is_honor for index in group.tiles)


def _triplet_count(grouping: tuple[_Group, ...]) -> int:
    return sum(1 for group in grouping if group.is_triplet_like)


def _concealed_triplet_count(grouping: tuple[_Group, ...]) -> int:
    return sum(1 for group in grouping if group.is_triplet_like and group.concealed)


def _quad_count(grouping: tuple[_Group, ...]) -> int:
    return sum(1 for group in grouping if group.quad)


def _typed_triplet_count(grouping: tuple[_Group, ...], tile_types: frozenset[TileType]) -> int:
    return sum(1 for group in grouping if group.is_triplet_like and group.tile_type in tile_types)


def _wait_fu(grouping: tuple[_Group, ...], winning_tile: TileType) -> int:
    for group in grouping:
        if group.is_pair and group.tiles[0] == winning_tile.index:
            return 2
    for group in grouping:
        if not group.is_sequence or winning_tile.index not in group.tiles:
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
        return _Group("sequence", tuple(sorted(tile_indices)), False)
    if meld.kind == ActionKind.PON:
        return _Group("triplet", tile_indices[:3], False)
    if meld.kind == ActionKind.MINKAN:
        return _Group("triplet", tile_indices[:3], False, True)
    if meld.kind == ActionKind.ANKAN:
        return _Group("triplet", tile_indices[:3], True, True)
    if meld.kind == ActionKind.KAKAN:
        return _Group("triplet", tile_indices[:3], False, True)
    if meld.kind == ActionKind.KITA:
        return _Group("kita", tile_indices, False)
    raise ValueError(f"unsupported meld kind for yaku: {meld.kind.value}")


def _all_meld_tile_types(melds: Sequence[Meld]) -> tuple[TileType, ...]:
    return tuple(tile.type for meld in melds for tile in meld.tiles if meld.kind != ActionKind.KITA)


def _is_closed_hand(melds: Sequence[Meld]) -> bool:
    return all(meld.kind is ActionKind.ANKAN for meld in melds)


def _append_result(
    results: list[YakuResult],
    name: str,
    *,
    is_closed: bool,
    enabled: bool = True,
) -> None:
    if not enabled:
        return
    definition = YAKU_DEFINITIONS[name]
    results.append(
        YakuResult(
            name=name,
            han=definition.han_for_closed_state(is_closed=is_closed),
            yakuman_multiplier=definition.yakuman_multiplier,
        )
    )


def _append_repeated_results(
    results: list[YakuResult],
    name: str,
    *,
    count: int,
    is_closed: bool,
) -> None:
    for _index in range(count):
        _append_result(results, name, is_closed=is_closed)


def _can_sequence(counts: tuple[int, ...], tile_index: int) -> bool:
    return (
        tile_index < 27
        and tile_index % 9 <= 6
        and counts[tile_index + 1] > 0
        and counts[tile_index + 2] > 0
    )


def _tile_type(tile: Tile | TileType) -> TileType:
    return tile.type if isinstance(tile, Tile) else tile


def _optional_tile_type(tile: Tile | TileType | str | None) -> TileType | None:
    if tile is None:
        return None
    if isinstance(tile, Tile):
        return tile.type
    if isinstance(tile, TileType):
        return tile
    return TileType.parse(tile)
