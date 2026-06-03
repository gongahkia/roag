from __future__ import annotations

from dataclasses import dataclass
from html.parser import HTMLParser
from pathlib import Path

from kenjaku.core import Action, Tile, TileType

DRAW_TAG_TO_SEAT = {"T": 0, "U": 1, "V": 2, "W": 3}
DISCARD_TAG_TO_SEAT = {"D": 0, "E": 1, "F": 2, "G": 3}
RED_FIVE_TILE_IDS = {16, 52, 88}


@dataclass(frozen=True, slots=True)
class TenhouDraw:
    seat: int
    tile_id: int
    tile: Tile
    event_index: int


@dataclass(frozen=True, slots=True)
class TenhouDiscard:
    seat: int
    tile_id: int
    tile: Tile
    tsumogiri: bool
    turn: int

    @property
    def action(self) -> Action:
        return Action.discard(self.tile.type, tsumogiri=self.tsumogiri)


TenhouEvent = TenhouDraw | TenhouDiscard


@dataclass(frozen=True, slots=True)
class TenhouRound:
    dealer: int
    scores: tuple[int, ...]
    starting_hands: tuple[tuple[Tile, ...], ...]
    dora_indicators: tuple[Tile, ...]
    draws: tuple[TenhouDraw, ...]
    discards: tuple[TenhouDiscard, ...]
    events: tuple[TenhouEvent, ...]


@dataclass(frozen=True, slots=True)
class TenhouGame:
    rounds: tuple[TenhouRound, ...]


@dataclass(frozen=True, slots=True)
class _ParsedEvent:
    tag: str
    attrib: dict[str, str]


@dataclass(frozen=True, slots=True)
class _RoundBuilder:
    dealer: int
    scores: tuple[int, ...]
    starting_hands: tuple[tuple[Tile, ...], ...]
    dora_indicators: list[Tile]
    draws: list[TenhouDraw]
    discards: list[TenhouDiscard]
    events: list[TenhouEvent]
    last_draws: dict[int, int]

    def freeze(self) -> TenhouRound:
        return TenhouRound(
            dealer=self.dealer,
            scores=self.scores,
            starting_hands=self.starting_hands,
            dora_indicators=tuple(self.dora_indicators),
            draws=tuple(self.draws),
            discards=tuple(self.discards),
            events=tuple(self.events),
        )


def tenhou_tile(tile_id: int) -> Tile:
    if not 0 <= tile_id < 136:
        raise ValueError(f"Tenhou tile id out of range: {tile_id}")
    return Tile(TileType(tile_id // 4), red=tile_id in RED_FIVE_TILE_IDS)


def parse_tenhou_xml_file(path: str | Path) -> TenhouGame:
    return parse_tenhou_xml(Path(path).read_text(encoding="utf-8"))


def parse_tenhou_xml(xml_text: str) -> TenhouGame:
    rounds: list[TenhouRound] = []
    current: _RoundBuilder | None = None

    for event in _parse_events(xml_text):
        tag = event.tag
        if tag == "INIT":
            if current is not None:
                rounds.append(current.freeze())
            current = _parse_init(event)
            continue

        if current is None:
            continue

        if tag == "DORA":
            current.dora_indicators.append(tenhou_tile(_required_int(event, "hai")))
            continue

        draw_seat = _seat_from_tag(tag, DRAW_TAG_TO_SEAT)
        if draw_seat is not None:
            tile_id = _tile_id_from_tag(tag)
            draw = TenhouDraw(
                seat=draw_seat,
                tile_id=tile_id,
                tile=tenhou_tile(tile_id),
                event_index=len(current.events),
            )
            current.draws.append(draw)
            current.events.append(draw)
            current.last_draws[draw_seat] = tile_id
            continue

        discard_seat = _seat_from_tag(tag, DISCARD_TAG_TO_SEAT)
        if discard_seat is not None:
            tile_id = _tile_id_from_tag(tag)
            discard = TenhouDiscard(
                seat=discard_seat,
                tile_id=tile_id,
                tile=tenhou_tile(tile_id),
                tsumogiri=current.last_draws.get(discard_seat) == tile_id,
                turn=len(current.discards),
            )
            current.discards.append(discard)
            current.events.append(discard)
            current.last_draws.pop(discard_seat, None)

    if current is not None:
        rounds.append(current.freeze())

    return TenhouGame(rounds=tuple(rounds))


def _parse_events(xml_text: str) -> tuple[_ParsedEvent, ...]:
    parser = _TenhouEventParser()
    parser.feed(xml_text)
    parser.close()
    return tuple(parser.events)


class _TenhouEventParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.events: list[_ParsedEvent] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self._append_event(tag, attrs)

    def handle_startendtag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self._append_event(tag, attrs)

    def _append_event(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self.events.append(
            _ParsedEvent(
                tag=tag.upper(),
                attrib={name: value or "" for name, value in attrs},
            )
        )


def _parse_init(event: _ParsedEvent) -> _RoundBuilder:
    return _RoundBuilder(
        dealer=_required_int(event, "oya"),
        scores=_parse_scores(_required_attr(event, "ten")),
        starting_hands=tuple(_parse_hand(_required_attr(event, f"hai{seat}")) for seat in range(4)),
        dora_indicators=[],
        draws=[],
        discards=[],
        events=[],
        last_draws={},
    )


def _parse_scores(raw_scores: str) -> tuple[int, ...]:
    return tuple(int(score) * 100 for score in raw_scores.split(","))


def _parse_hand(raw_tiles: str) -> tuple[Tile, ...]:
    return tuple(tenhou_tile(int(tile_id)) for tile_id in raw_tiles.split(",") if tile_id)


def _required_attr(event: _ParsedEvent, name: str) -> str:
    value = event.attrib.get(name)
    if value is None:
        raise ValueError(f"{event.tag} missing required attribute {name!r}")
    return value


def _required_int(event: _ParsedEvent, name: str) -> int:
    return int(_required_attr(event, name))


def _seat_from_tag(tag: str, mapping: dict[str, int]) -> int | None:
    if not tag:
        return None
    return mapping.get(tag[0])


def _tile_id_from_tag(tag: str) -> int:
    try:
        return int(tag[1:])
    except ValueError as error:
        raise ValueError(f"invalid Tenhou tile event tag: {tag!r}") from error
