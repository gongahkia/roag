"""Fixed-width numeric encoding for actor-private ObservationV1 values."""

from __future__ import annotations

from collections.abc import Iterable

from kenjaku.core import Tile
from kenjaku.schema.observation_v1 import ObservationV1

OBSERVATION_V1_TENSOR_KIND = "kenjaku-observation-v1-tensor-v1"
OBSERVATION_V1_TENSOR_LAYOUT = (
    ("hand_counts", 34),
    ("drawn_tile", 34),
    ("discard_counts_by_relative_seat", 136),
    ("meld_tile_counts_by_relative_seat", 136),
    ("kita_counts_by_relative_seat", 4),
    ("dora_indicator_counts", 34),
    ("pending_tile_kinds", 102),
    ("current_and_dealer_relative_seats", 8),
    ("relative_seat_statuses", 20),
    ("points_by_relative_seat", 4),
    ("hand_sizes_by_relative_seat", 4),
    ("table_scalars", 5),
    ("state_flags", 7),
    ("round_wind", 4),
    ("pending_source_relative_seats", 12),
    ("pending_chankan_kind", 2),
)
OBSERVATION_V1_TENSOR_DIM = sum(width for _name, width in OBSERVATION_V1_TENSOR_LAYOUT)
_MAX_SEATS = 4
_ROUND_WINDS = ("E", "S", "W", "N")


def observation_v1_tensor(observation: ObservationV1) -> tuple[float, ...]:
    """Encode an ObservationV1 as a deterministic 546-element float tuple.

    Seat-indexed features are actor-relative: index zero is always the
    observation actor, and the fourth slot is zero-padded for Sanma.
    """
    values: list[float] = []
    values.extend(_tile_count_features(observation.hand))
    values.extend(_tile_count_features((), selected=observation.drawn_tile))
    values.extend(_relative_tile_rows(observation, observation.discards))
    values.extend(
        _relative_tile_rows(
            observation,
            tuple(
                tuple(tile for meld in seat_melds for tile in meld.tiles)
                for seat_melds in observation.melds
            ),
        )
    )
    values.extend(
        _relative_scalar_rows(observation, (len(tiles) / 4.0 for tiles in observation.kita_tiles))
    )
    values.extend(_tile_count_features(observation.dora_indicators, divisor=5.0))
    values.extend(_tile_count_features((), selected=observation.pending_discard))
    values.extend(_tile_count_features((), selected=observation.pending_chankan_tile))
    values.extend(_tile_count_features((), selected=observation.pending_kita_tile))
    values.extend(_relative_one_hot(observation, observation.current_seat))
    values.extend(_relative_one_hot(observation, observation.dealer_seat))
    for seats in (
        observation.pending_reaction_seats,
        observation.riichi_seats,
        observation.double_riichi_seats,
        observation.riichi_pending_discard_seats,
        observation.ippatsu_seats,
    ):
        values.extend(_relative_seat_flags(observation, seats))
    values.extend(
        _relative_scalar_rows(observation, (points / 1000.0 for points in observation.points))
    )
    values.extend(
        _relative_scalar_rows(observation, (size / 14.0 for size in observation.hand_sizes))
    )
    values.extend(
        (
            observation.turn / 100.0,
            observation.honba / 10.0,
            observation.riichi_sticks / 10.0,
            observation.wall_remaining / 136.0,
            observation.dead_wall_remaining / 14.0,
        )
    )
    values.extend(
        (
            float(observation.needs_discard),
            float(observation.rinshan_draw),
            float(observation.last_draw_was_final_live_wall),
            float(observation.game_finished),
            float(observation.pending_abortive_draw_reason is not None),
            float(observation.abortive_draw_after_discard_reason is not None),
            float(observation.terminal_reason is not None),
        )
    )
    values.extend(float(observation.round_wind == wind) for wind in _ROUND_WINDS)
    for seat in (
        observation.pending_discard_seat,
        observation.pending_chankan_seat,
        observation.pending_kita_seat,
    ):
        values.extend(_relative_one_hot(observation, seat))
    values.extend(
        (
            float(observation.pending_chankan_kind == "ankan"),
            float(observation.pending_chankan_kind == "kakan"),
        )
    )
    if len(values) != OBSERVATION_V1_TENSOR_DIM:
        raise ValueError("ObservationV1 tensor layout drifted")
    return tuple(values)


encode_observation_v1 = observation_v1_tensor


def _tile_count_features(
    tiles: Iterable[str],
    *,
    selected: str | None = None,
    divisor: float = 4.0,
) -> list[float]:
    counts = [0] * 34
    for notation in tiles:
        counts[Tile.parse(notation).type.index] += 1
    if selected is not None:
        counts[Tile.parse(selected).type.index] += 1
    return [count / divisor for count in counts]


def _relative_tile_rows(
    observation: ObservationV1,
    rows: tuple[tuple[str, ...], ...],
) -> list[float]:
    values: list[float] = []
    for offset in range(_MAX_SEATS):
        if offset >= observation.players:
            values.extend((0.0,) * 34)
            continue
        values.extend(_tile_count_features(rows[(observation.seat + offset) % observation.players]))
    return values


def _relative_scalar_rows(
    observation: ObservationV1,
    values: Iterable[float],
) -> list[float]:
    by_seat = tuple(values)
    result: list[float] = []
    for offset in range(_MAX_SEATS):
        result.append(
            0.0
            if offset >= observation.players
            else by_seat[(observation.seat + offset) % observation.players]
        )
    return result


def _relative_one_hot(observation: ObservationV1, seat: int | None) -> list[float]:
    values = [0.0] * _MAX_SEATS
    if seat is not None:
        values[(seat - observation.seat) % observation.players] = 1.0
    return values


def _relative_seat_flags(observation: ObservationV1, seats: tuple[int, ...]) -> list[float]:
    return [
        float((observation.seat + offset) % observation.players in seats)
        if offset < observation.players
        else 0.0
        for offset in range(_MAX_SEATS)
    ]
