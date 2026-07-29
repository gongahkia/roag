from __future__ import annotations

from dataclasses import dataclass

from kenjaku.core import ActionKind, Tile
from kenjaku.io.tenhou_tiles import tenhou_tile


@dataclass(frozen=True, slots=True)
class TenhouMeld:
    """Decoded Tenhou `m` attribute for chi, pon, kakan, minkan, and ankan calls."""

    kind: ActionKind
    from_offset: int
    tile_ids: tuple[int, ...]
    tiles: tuple[Tile, ...]
    called_tile_id: int | None = None
    called_tile: Tile | None = None
    added_tile_id: int | None = None
    added_tile: Tile | None = None


def decode_tenhou_meld(meld_code: int) -> TenhouMeld:
    """Decode Tenhou's compact 16-bit meld representation."""

    if not 0 <= meld_code < 1 << 16:
        raise ValueError(f"Tenhou meld code out of range: {meld_code}")

    if meld_code & 0x4:
        return _decode_chi(meld_code)
    if meld_code & 0x18:
        return _decode_pon_or_kakan(meld_code)
    if meld_code & 0x20:
        return _decode_nuki(meld_code)
    return _decode_kan(meld_code)


def _decode_chi(meld_code: int) -> TenhouMeld:
    copy_offsets = (
        (meld_code >> 3) & 0x3,
        (meld_code >> 5) & 0x3,
        (meld_code >> 7) & 0x3,
    )
    encoded_base_and_called = (meld_code >> 10) & 0x3F
    called_index = encoded_base_and_called % 3
    encoded_base = encoded_base_and_called // 3
    base = (encoded_base // 7) * 9 + encoded_base % 7
    if base >= 27 or base % 9 > 6:
        raise ValueError(f"invalid Tenhou chi base in meld code: {meld_code}")

    tile_ids = tuple((base + offset) * 4 + copy for offset, copy in enumerate(copy_offsets))
    return _build_meld(
        kind=ActionKind.CHI,
        from_offset=meld_code & 0x3,
        tile_ids=tile_ids,
        called_tile_id=tile_ids[called_index],
    )


def _decode_pon_or_kakan(meld_code: int) -> TenhouMeld:
    is_pon = bool(meld_code & 0x8)
    is_kakan = bool(meld_code & 0x10)
    if is_pon == is_kakan:
        raise ValueError(f"invalid Tenhou pon/kakan flags in meld code: {meld_code}")

    unused_copy = (meld_code >> 5) & 0x3
    encoded_base_and_called = (meld_code >> 9) & 0x7F
    called_index = encoded_base_and_called % 3
    base = encoded_base_and_called // 3
    _validate_tile_type_index(base, meld_code)

    pon_tile_ids = tuple(base * 4 + copy for copy in range(4) if copy != unused_copy)
    called_tile_id = pon_tile_ids[called_index]
    if is_pon:
        return _build_meld(
            kind=ActionKind.PON,
            from_offset=meld_code & 0x3,
            tile_ids=pon_tile_ids,
            called_tile_id=called_tile_id,
        )

    added_tile_id = base * 4 + unused_copy
    return _build_meld(
        kind=ActionKind.KAKAN,
        from_offset=meld_code & 0x3,
        tile_ids=tuple(base * 4 + copy for copy in range(4)),
        called_tile_id=called_tile_id,
        added_tile_id=added_tile_id,
    )


def _decode_kan(meld_code: int) -> TenhouMeld:
    from_offset = meld_code & 0x3
    encoded_base_and_called = (meld_code >> 8) & 0xFF
    called_index = encoded_base_and_called % 4
    base = encoded_base_and_called // 4
    _validate_tile_type_index(base, meld_code)

    tile_ids = tuple(base * 4 + copy for copy in range(4))
    return _build_meld(
        kind=ActionKind.ANKAN if from_offset == 0 else ActionKind.MINKAN,
        from_offset=from_offset,
        tile_ids=tile_ids,
        called_tile_id=None if from_offset == 0 else tile_ids[called_index],
    )


def _decode_nuki(meld_code: int) -> TenhouMeld:
    tile_id = (meld_code >> 8) & 0xFF
    if not 0 <= tile_id < 136:
        raise ValueError(f"invalid Tenhou nuki tile in meld code: {meld_code}")
    return _build_meld(
        kind=ActionKind.KITA,
        from_offset=0,
        tile_ids=(tile_id,),
        called_tile_id=None,
    )


def _build_meld(
    *,
    kind: ActionKind,
    from_offset: int,
    tile_ids: tuple[int, ...],
    called_tile_id: int | None,
    added_tile_id: int | None = None,
) -> TenhouMeld:
    return TenhouMeld(
        kind=kind,
        from_offset=from_offset,
        tile_ids=tile_ids,
        tiles=tuple(tenhou_tile(tile_id) for tile_id in tile_ids),
        called_tile_id=called_tile_id,
        called_tile=None if called_tile_id is None else tenhou_tile(called_tile_id),
        added_tile_id=added_tile_id,
        added_tile=None if added_tile_id is None else tenhou_tile(added_tile_id),
    )


def _validate_tile_type_index(tile_type_index: int, meld_code: int) -> None:
    if not 0 <= tile_type_index < 34:
        raise ValueError(f"invalid Tenhou tile base in meld code: {meld_code}")
