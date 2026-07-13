"""Versioned public observation payloads for ruleset-neutral policy consumers."""

from __future__ import annotations

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import TYPE_CHECKING, Any, Self

from kenjaku.core import TENHOU_3P, TENHOU_4P, ActionKind, Tile

if TYPE_CHECKING:
    from kenjaku.simulation.environment import SandboxEnvironmentState


OBSERVATION_V1_KIND = "kenjaku-observation-v1"
OBSERVATION_V1_FIELDS = (
    "kind",
    "ruleset",
    "players",
    "seat",
    "turn",
    "current_seat",
    "dealer_seat",
    "round_wind",
    "honba",
    "riichi_sticks",
    "points",
    "wall_remaining",
    "dead_wall_remaining",
    "hand",
    "hand_sizes",
    "discards",
    "melds",
    "kita_tiles",
    "dora_indicators",
    "drawn_tile",
    "needs_discard",
    "rinshan_draw",
    "last_draw_was_final_live_wall",
    "pending_discard",
    "pending_discard_seat",
    "pending_chankan_tile",
    "pending_chankan_seat",
    "pending_chankan_kind",
    "pending_kita_tile",
    "pending_kita_seat",
    "pending_abortive_draw_reason",
    "abortive_draw_after_discard_reason",
    "pending_reaction_seats",
    "riichi_seats",
    "double_riichi_seats",
    "riichi_pending_discard_seats",
    "ippatsu_seats",
    "terminal_reason",
    "game_finished",
)
_RULESETS = {TENHOU_4P.name: TENHOU_4P, TENHOU_3P.name: TENHOU_3P}
_MELD_TILE_COUNTS = {
    ActionKind.CHI.value: 3,
    ActionKind.PON.value: 3,
    ActionKind.MINKAN.value: 4,
    ActionKind.ANKAN.value: 4,
    ActionKind.KAKAN.value: 4,
}


@dataclass(frozen=True, slots=True)
class MeldV1:
    """One public meld, preserving only table-visible tile information."""

    kind: str
    tiles: tuple[str, ...]
    called_tile: str | None
    from_seat: int | None

    def __post_init__(self) -> None:
        expected_tile_count = _MELD_TILE_COUNTS.get(self.kind)
        if expected_tile_count is None:
            raise ValueError("unsupported ObservationV1 meld kind: " + self.kind)
        if len(self.tiles) != expected_tile_count:
            raise ValueError(f"{self.kind} meld must contain {expected_tile_count} tiles")
        _validate_tile_sequence(self.tiles, ruleset=None, field="meld tiles")
        if self.called_tile is not None:
            _validate_tile(self.called_tile, ruleset=None, field="called_tile")
        if self.from_seat is not None:
            _validate_non_negative_int(self.from_seat, "meld from_seat")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, ("kind", "tiles", "called_tile", "from_seat"), "meld")
        return cls(
            kind=_read_str(payload, "kind"),
            tiles=_read_string_tuple(payload, "tiles"),
            called_tile=_read_optional_str(payload, "called_tile"),
            from_seat=_read_optional_int(payload, "from_seat"),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": self.kind,
            "tiles": list(self.tiles),
            "called_tile": self.called_tile,
            "from_seat": self.from_seat,
        }


@dataclass(frozen=True, slots=True)
class ObservationV1:
    """Actor-private hand plus table-public state, serialized without hidden state."""

    ruleset: str
    players: int
    seat: int
    turn: int
    current_seat: int
    dealer_seat: int
    round_wind: str
    honba: int
    riichi_sticks: int
    points: tuple[int, ...]
    wall_remaining: int
    dead_wall_remaining: int
    hand: tuple[str, ...]
    hand_sizes: tuple[int, ...]
    discards: tuple[tuple[str, ...], ...]
    melds: tuple[tuple[MeldV1, ...], ...]
    kita_tiles: tuple[tuple[str, ...], ...]
    dora_indicators: tuple[str, ...]
    drawn_tile: str | None
    needs_discard: bool
    rinshan_draw: bool
    last_draw_was_final_live_wall: bool
    pending_discard: str | None
    pending_discard_seat: int | None
    pending_chankan_tile: str | None
    pending_chankan_seat: int | None
    pending_chankan_kind: str | None
    pending_kita_tile: str | None
    pending_kita_seat: int | None
    pending_abortive_draw_reason: str | None
    abortive_draw_after_discard_reason: str | None
    pending_reaction_seats: tuple[int, ...]
    riichi_seats: tuple[int, ...]
    double_riichi_seats: tuple[int, ...]
    riichi_pending_discard_seats: tuple[int, ...]
    ippatsu_seats: tuple[int, ...]
    terminal_reason: str | None
    game_finished: bool

    def __post_init__(self) -> None:
        rules = _RULESETS.get(self.ruleset)
        if rules is None:
            raise ValueError("unsupported ObservationV1 ruleset: " + self.ruleset)
        if self.players != rules.players:
            raise ValueError("ObservationV1 players must match ruleset")
        _validate_non_negative_ints(
            {
                "turn": self.turn,
                "honba": self.honba,
                "riichi_sticks": self.riichi_sticks,
                "wall_remaining": self.wall_remaining,
                "dead_wall_remaining": self.dead_wall_remaining,
            }
        )
        _validate_seat(self.seat, self.players, "seat")
        _validate_seat(self.current_seat, self.players, "current_seat")
        _validate_seat(self.dealer_seat, self.players, "dealer_seat")
        _validate_tile(self.round_wind, ruleset=None, field="round_wind")
        if self.round_wind not in {"E", "S", "W", "N"}:
            raise ValueError("round_wind must be E, S, W, or N")
        _validate_player_vector(
            self.points,
            self.players,
            "points",
            lambda value: _validate_int(value, "point"),
        )
        _validate_player_vector(
            self.hand_sizes,
            self.players,
            "hand_sizes",
            lambda value: _validate_non_negative_int(value, "hand_size"),
        )
        _validate_player_vector(
            self.discards,
            self.players,
            "discards",
            lambda values: _validate_tile_sequence(values, ruleset=self.ruleset, field="discard"),
        )
        _validate_player_vector(
            self.melds,
            self.players,
            "melds",
            lambda values: _validate_melds(values, players=self.players, ruleset=self.ruleset),
        )
        _validate_player_vector(
            self.kita_tiles,
            self.players,
            "kita_tiles",
            lambda values: _validate_tile_sequence(values, ruleset=self.ruleset, field="kita tile"),
        )
        _validate_tile_sequence(self.hand, ruleset=self.ruleset, field="hand")
        _validate_tile_sequence(self.dora_indicators, ruleset=self.ruleset, field="dora indicator")
        if len(self.hand) != self.hand_sizes[self.seat]:
            raise ValueError("ObservationV1 hand must match own hand_size")
        if self.ruleset != TENHOU_3P.name and any(self.kita_tiles):
            raise ValueError("kita_tiles are only valid for tenhou-3p")
        if any(tile != "N" for seat_tiles in self.kita_tiles for tile in seat_tiles):
            raise ValueError("kita_tiles must contain only N")
        _validate_optional_tile(self.drawn_tile, self.ruleset, "drawn_tile")
        if self.drawn_tile is not None and self.seat != self.current_seat:
            raise ValueError("ObservationV1 cannot expose another seat's drawn_tile")
        _validate_bool(self.needs_discard, "needs_discard")
        _validate_bool(self.rinshan_draw, "rinshan_draw")
        _validate_bool(self.last_draw_was_final_live_wall, "last_draw_was_final_live_wall")
        _validate_bool(self.game_finished, "game_finished")
        _validate_optional_tile(self.pending_discard, self.ruleset, "pending_discard")
        _validate_optional_tile(self.pending_chankan_tile, self.ruleset, "pending_chankan_tile")
        _validate_optional_tile(self.pending_kita_tile, self.ruleset, "pending_kita_tile")
        _validate_optional_seat(self.pending_discard_seat, self.players, "pending_discard_seat")
        _validate_optional_seat(self.pending_chankan_seat, self.players, "pending_chankan_seat")
        _validate_optional_seat(self.pending_kita_seat, self.players, "pending_kita_seat")
        if self.pending_discard is None and self.pending_discard_seat is not None:
            raise ValueError("pending_discard_seat requires pending_discard")
        if self.pending_chankan_tile is None and self.pending_chankan_seat is not None:
            raise ValueError("pending_chankan_seat requires pending_chankan_tile")
        if self.pending_kita_tile is None and self.pending_kita_seat is not None:
            raise ValueError("pending_kita_seat requires pending_kita_tile")
        if self.pending_chankan_kind not in {None, ActionKind.ANKAN.value, ActionKind.KAKAN.value}:
            raise ValueError("pending_chankan_kind must be ankan, kakan, or null")
        if self.pending_chankan_tile is None and self.pending_chankan_kind is not None:
            raise ValueError("pending_chankan_kind requires pending_chankan_tile")
        if self.pending_chankan_tile is not None and self.pending_chankan_kind is None:
            raise ValueError("pending_chankan_tile requires pending_chankan_kind")
        pending_windows = sum(
            tile is not None
            for tile in (
                self.pending_discard,
                self.pending_chankan_tile,
                self.pending_kita_tile,
            )
        )
        if pending_windows > 1:
            raise ValueError("ObservationV1 supports one pending reaction window")
        if pending_windows == 0 and self.pending_reaction_seats:
            raise ValueError("pending_reaction_seats require a pending reaction window")
        _validate_optional_str(
            self.pending_abortive_draw_reason,
            "pending_abortive_draw_reason",
        )
        _validate_optional_str(
            self.abortive_draw_after_discard_reason,
            "abortive_draw_after_discard_reason",
        )
        for field, seats in (
            ("pending_reaction_seats", self.pending_reaction_seats),
            ("riichi_seats", self.riichi_seats),
            ("double_riichi_seats", self.double_riichi_seats),
            ("riichi_pending_discard_seats", self.riichi_pending_discard_seats),
            ("ippatsu_seats", self.ippatsu_seats),
        ):
            _validate_seat_tuple(seats, self.players, field)
        _validate_optional_str(self.terminal_reason, "terminal_reason")

    @classmethod
    def from_sandbox_state(cls, state: SandboxEnvironmentState, *, seat: int) -> Self:
        """Create an actor-specific observation without serializing hidden state."""
        _validate_seat(seat, state.players, "seat")
        payload = state.to_payload()
        return cls(
            ruleset=state.ruleset,
            players=state.players,
            seat=seat,
            turn=state.turn,
            current_seat=state.current_seat,
            dealer_seat=state.dealer_seat,
            round_wind=state.round_wind.notation,
            honba=state.honba,
            riichi_sticks=state.riichi_sticks,
            points=tuple(state.points),
            wall_remaining=len(state.wall),
            dead_wall_remaining=len(state.dead_wall),
            hand=tuple(tile.notation for tile in state.hands[seat]),
            hand_sizes=tuple(state.hand_sizes()),
            discards=tuple(tuple(seat_discards) for seat_discards in payload["discards"]),
            melds=tuple(
                tuple(MeldV1.from_dict(meld) for meld in seat_melds)
                for seat_melds in payload["melds"]
            ),
            kita_tiles=tuple(tuple(seat_tiles) for seat_tiles in payload["kita_tiles"]),
            dora_indicators=tuple(payload["dora_indicators"]),
            drawn_tile=(
                None
                if state.drawn_tile is None or seat != state.current_seat
                else state.drawn_tile.notation
            ),
            needs_discard=state.needs_discard,
            rinshan_draw=state.rinshan_draw,
            last_draw_was_final_live_wall=state.last_draw_was_final_live_wall,
            pending_discard=(
                None if state.pending_discard is None else state.pending_discard.notation
            ),
            pending_discard_seat=state.pending_discard_seat,
            pending_chankan_tile=(
                None if state.pending_chankan_tile is None else state.pending_chankan_tile.notation
            ),
            pending_chankan_seat=state.pending_chankan_seat,
            pending_chankan_kind=(
                None if state.pending_chankan_kind is None else state.pending_chankan_kind.value
            ),
            pending_kita_tile=(
                None if state.pending_kita_tile is None else state.pending_kita_tile.notation
            ),
            pending_kita_seat=state.pending_kita_seat,
            pending_abortive_draw_reason=state.pending_abortive_draw_reason,
            abortive_draw_after_discard_reason=state.abortive_draw_after_discard_reason,
            pending_reaction_seats=state.pending_reaction_seats,
            riichi_seats=state.riichi_seats,
            double_riichi_seats=state.double_riichi_seats,
            riichi_pending_discard_seats=state.riichi_pending_discard_seats,
            ippatsu_seats=state.ippatsu_seats,
            terminal_reason=state.terminal_reason,
            game_finished=state.final_result is not None,
        )

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, OBSERVATION_V1_FIELDS, "ObservationV1")
        if _read_str(payload, "kind") != OBSERVATION_V1_KIND:
            raise ValueError(f"ObservationV1 kind must be {OBSERVATION_V1_KIND}")
        return cls(
            ruleset=_read_str(payload, "ruleset"),
            players=_read_int(payload, "players"),
            seat=_read_int(payload, "seat"),
            turn=_read_int(payload, "turn"),
            current_seat=_read_int(payload, "current_seat"),
            dealer_seat=_read_int(payload, "dealer_seat"),
            round_wind=_read_str(payload, "round_wind"),
            honba=_read_int(payload, "honba"),
            riichi_sticks=_read_int(payload, "riichi_sticks"),
            points=_read_int_tuple(payload, "points"),
            wall_remaining=_read_int(payload, "wall_remaining"),
            dead_wall_remaining=_read_int(payload, "dead_wall_remaining"),
            hand=_read_string_tuple(payload, "hand"),
            hand_sizes=_read_int_tuple(payload, "hand_sizes"),
            discards=_read_nested_string_tuples(payload, "discards"),
            melds=_read_nested_meld_tuples(payload, "melds"),
            kita_tiles=_read_nested_string_tuples(payload, "kita_tiles"),
            dora_indicators=_read_string_tuple(payload, "dora_indicators"),
            drawn_tile=_read_optional_str(payload, "drawn_tile"),
            needs_discard=_read_bool(payload, "needs_discard"),
            rinshan_draw=_read_bool(payload, "rinshan_draw"),
            last_draw_was_final_live_wall=_read_bool(payload, "last_draw_was_final_live_wall"),
            pending_discard=_read_optional_str(payload, "pending_discard"),
            pending_discard_seat=_read_optional_int(payload, "pending_discard_seat"),
            pending_chankan_tile=_read_optional_str(payload, "pending_chankan_tile"),
            pending_chankan_seat=_read_optional_int(payload, "pending_chankan_seat"),
            pending_chankan_kind=_read_optional_str(payload, "pending_chankan_kind"),
            pending_kita_tile=_read_optional_str(payload, "pending_kita_tile"),
            pending_kita_seat=_read_optional_int(payload, "pending_kita_seat"),
            pending_abortive_draw_reason=_read_optional_str(
                payload,
                "pending_abortive_draw_reason",
            ),
            abortive_draw_after_discard_reason=_read_optional_str(
                payload,
                "abortive_draw_after_discard_reason",
            ),
            pending_reaction_seats=_read_int_tuple(payload, "pending_reaction_seats"),
            riichi_seats=_read_int_tuple(payload, "riichi_seats"),
            double_riichi_seats=_read_int_tuple(payload, "double_riichi_seats"),
            riichi_pending_discard_seats=_read_int_tuple(
                payload,
                "riichi_pending_discard_seats",
            ),
            ippatsu_seats=_read_int_tuple(payload, "ippatsu_seats"),
            terminal_reason=_read_optional_str(payload, "terminal_reason"),
            game_finished=_read_bool(payload, "game_finished"),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": OBSERVATION_V1_KIND,
            "ruleset": self.ruleset,
            "players": self.players,
            "seat": self.seat,
            "turn": self.turn,
            "current_seat": self.current_seat,
            "dealer_seat": self.dealer_seat,
            "round_wind": self.round_wind,
            "honba": self.honba,
            "riichi_sticks": self.riichi_sticks,
            "points": list(self.points),
            "wall_remaining": self.wall_remaining,
            "dead_wall_remaining": self.dead_wall_remaining,
            "hand": list(self.hand),
            "hand_sizes": list(self.hand_sizes),
            "discards": [list(seat_discards) for seat_discards in self.discards],
            "melds": [[meld.to_dict() for meld in seat_melds] for seat_melds in self.melds],
            "kita_tiles": [list(seat_tiles) for seat_tiles in self.kita_tiles],
            "dora_indicators": list(self.dora_indicators),
            "drawn_tile": self.drawn_tile,
            "needs_discard": self.needs_discard,
            "rinshan_draw": self.rinshan_draw,
            "last_draw_was_final_live_wall": self.last_draw_was_final_live_wall,
            "pending_discard": self.pending_discard,
            "pending_discard_seat": self.pending_discard_seat,
            "pending_chankan_tile": self.pending_chankan_tile,
            "pending_chankan_seat": self.pending_chankan_seat,
            "pending_chankan_kind": self.pending_chankan_kind,
            "pending_kita_tile": self.pending_kita_tile,
            "pending_kita_seat": self.pending_kita_seat,
            "pending_abortive_draw_reason": self.pending_abortive_draw_reason,
            "abortive_draw_after_discard_reason": self.abortive_draw_after_discard_reason,
            "pending_reaction_seats": list(self.pending_reaction_seats),
            "riichi_seats": list(self.riichi_seats),
            "double_riichi_seats": list(self.double_riichi_seats),
            "riichi_pending_discard_seats": list(self.riichi_pending_discard_seats),
            "ippatsu_seats": list(self.ippatsu_seats),
            "terminal_reason": self.terminal_reason,
            "game_finished": self.game_finished,
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def _require_exact_fields(payload: Mapping[str, Any], fields: Sequence[str], name: str) -> None:
    actual = set(payload)
    expected = set(fields)
    if actual == expected:
        return
    missing = sorted(expected - actual)
    unexpected = sorted(actual - expected)
    details = []
    if missing:
        details.append("missing=" + ",".join(missing))
    if unexpected:
        details.append("unexpected=" + ",".join(unexpected))
    raise ValueError(f"{name} fields must match v1 schema: {'; '.join(details)}")


def _read_str(payload: Mapping[str, Any], field: str) -> str:
    value = payload[field]
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string")
    return value


def _read_optional_str(payload: Mapping[str, Any], field: str) -> str | None:
    value = payload[field]
    if value is None:
        return None
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string or null")
    return value


def _read_int(payload: Mapping[str, Any], field: str) -> int:
    value = payload[field]
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{field} must be an integer")
    return value


def _read_optional_int(payload: Mapping[str, Any], field: str) -> int | None:
    value = payload[field]
    if value is None:
        return None
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{field} must be an integer or null")
    return value


def _read_bool(payload: Mapping[str, Any], field: str) -> bool:
    value = payload[field]
    if not isinstance(value, bool):
        raise ValueError(f"{field} must be a boolean")
    return value


def _read_sequence(payload: Mapping[str, Any], field: str) -> Sequence[Any]:
    value = payload[field]
    if isinstance(value, (str, bytes)) or not isinstance(value, Sequence):
        raise ValueError(f"{field} must be an array")
    return value


def _read_string_tuple(payload: Mapping[str, Any], field: str) -> tuple[str, ...]:
    return tuple(_read_array_str(value, field) for value in _read_sequence(payload, field))


def _read_int_tuple(payload: Mapping[str, Any], field: str) -> tuple[int, ...]:
    return tuple(_read_array_int(value, field) for value in _read_sequence(payload, field))


def _read_nested_string_tuples(
    payload: Mapping[str, Any],
    field: str,
) -> tuple[tuple[str, ...], ...]:
    nested: list[tuple[str, ...]] = []
    for values in _read_sequence(payload, field):
        if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
            raise ValueError(f"{field} entries must be arrays")
        nested.append(tuple(_read_array_str(value, field) for value in values))
    return tuple(nested)


def _read_nested_meld_tuples(
    payload: Mapping[str, Any],
    field: str,
) -> tuple[tuple[MeldV1, ...], ...]:
    nested: list[tuple[MeldV1, ...]] = []
    for values in _read_sequence(payload, field):
        if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
            raise ValueError(f"{field} entries must be arrays")
        melds: list[MeldV1] = []
        for value in values:
            if not isinstance(value, Mapping):
                raise ValueError(f"{field} entries must contain objects")
            melds.append(MeldV1.from_dict(value))
        nested.append(tuple(melds))
    return tuple(nested)


def _read_array_str(value: Any, field: str) -> str:
    if not isinstance(value, str):
        raise ValueError(f"{field} entries must be strings")
    return value


def _read_array_int(value: Any, field: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{field} entries must be integers")
    return value


def _validate_non_negative_ints(values: Mapping[str, int]) -> None:
    for field, value in values.items():
        _validate_non_negative_int(value, field)


def _validate_non_negative_int(value: Any, field: str) -> None:
    _validate_int(value, field)
    if value < 0:
        raise ValueError(f"{field} must be non-negative")


def _validate_int(value: Any, field: str) -> None:
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{field} must be an integer")


def _validate_bool(value: Any, field: str) -> None:
    if not isinstance(value, bool):
        raise ValueError(f"{field} must be a boolean")


def _validate_seat(seat: Any, players: int, field: str) -> None:
    _validate_int(seat, field)
    if not 0 <= seat < players:
        raise ValueError(f"{field} outside player range")


def _validate_optional_seat(seat: int | None, players: int, field: str) -> None:
    if seat is not None:
        _validate_seat(seat, players, field)


def _validate_seat_tuple(seats: tuple[int, ...], players: int, field: str) -> None:
    for seat in seats:
        _validate_seat(seat, players, field)
    if len(set(seats)) != len(seats):
        raise ValueError(f"{field} must not contain duplicate seats")


def _validate_player_vector(
    values: Sequence[Any],
    players: int,
    field: str,
    validate: Any,
) -> None:
    if len(values) != players:
        raise ValueError(f"{field} must contain one entry per player")
    for value in values:
        validate(value)


def _validate_tile_sequence(values: Sequence[str], *, ruleset: str | None, field: str) -> None:
    for value in values:
        _validate_tile(value, ruleset=ruleset, field=field)


def _validate_tile(value: str, *, ruleset: str | None, field: str) -> None:
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a tile string")
    tile = Tile.parse(value)
    if ruleset is not None and tile.type not in _RULESETS[ruleset].tile_types:
        raise ValueError(f"{field} tile is unavailable in {ruleset}: {value}")


def _validate_optional_tile(value: str | None, ruleset: str, field: str) -> None:
    if value is not None:
        _validate_tile(value, ruleset=ruleset, field=field)


def _validate_optional_str(value: str | None, field: str) -> None:
    if value is not None and not isinstance(value, str):
        raise ValueError(f"{field} must be a string or null")


def _validate_melds(values: Sequence[MeldV1], *, players: int, ruleset: str) -> None:
    for meld in values:
        if not isinstance(meld, MeldV1):
            raise ValueError("melds must contain MeldV1 values")
        _validate_tile_sequence(meld.tiles, ruleset=ruleset, field="meld tile")
        _validate_optional_tile(meld.called_tile, ruleset, "meld called_tile")
        _validate_optional_seat(meld.from_seat, players, "meld from_seat")
