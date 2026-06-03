from __future__ import annotations

from dataclasses import dataclass
from html.parser import HTMLParser
from pathlib import Path

from kenjaku.core import Action, Tile
from kenjaku.io.tenhou_meld import TenhouMeld, decode_tenhou_meld
from kenjaku.io.tenhou_tiles import tenhou_tile

DRAW_TAG_TO_SEAT = {"T": 0, "U": 1, "V": 2, "W": 3}
DISCARD_TAG_TO_SEAT = {"D": 0, "E": 1, "F": 2, "G": 3}


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
    event_index: int
    turn: int

    @property
    def action(self) -> Action:
        return Action.discard(self.tile.type, tsumogiri=self.tsumogiri)


@dataclass(frozen=True, slots=True)
class TenhouReach:
    seat: int
    step: int
    event_index: int
    scores: tuple[int, ...] | None = None


@dataclass(frozen=True, slots=True)
class TenhouCall:
    seat: int
    meld_code: int
    event_index: int
    meld: TenhouMeld


@dataclass(frozen=True, slots=True)
class TenhouAgari:
    winner: int
    from_seat: int
    event_index: int
    machi: Tile | None = None
    points: tuple[int, ...] | None = None
    yaku: tuple[int, ...] = ()
    dora_indicators: tuple[Tile, ...] = ()
    ura_dora_indicators: tuple[Tile, ...] = ()


@dataclass(frozen=True, slots=True)
class TenhouRyuukyoku:
    event_index: int
    reason: str | None = None
    scores: tuple[int, ...] | None = None


TenhouEvent = (
    TenhouDraw | TenhouDiscard | TenhouReach | TenhouCall | TenhouAgari | TenhouRyuukyoku
)


@dataclass(frozen=True, slots=True)
class TenhouRound:
    dealer: int
    scores: tuple[int, ...]
    starting_hands: tuple[tuple[Tile, ...], ...]
    dora_indicators: tuple[Tile, ...]
    draws: tuple[TenhouDraw, ...]
    discards: tuple[TenhouDiscard, ...]
    reaches: tuple[TenhouReach, ...]
    calls: tuple[TenhouCall, ...]
    agari: tuple[TenhouAgari, ...]
    ryuukyoku: TenhouRyuukyoku | None
    events: tuple[TenhouEvent, ...]


@dataclass(frozen=True, slots=True)
class TenhouGame:
    rounds: tuple[TenhouRound, ...]


@dataclass(frozen=True, slots=True)
class _ParsedEvent:
    tag: str
    attrib: dict[str, str]


@dataclass(slots=True)
class _RoundBuilder:
    dealer: int
    scores: tuple[int, ...]
    starting_hands: tuple[tuple[Tile, ...], ...]
    dora_indicators: list[Tile]
    draws: list[TenhouDraw]
    discards: list[TenhouDiscard]
    reaches: list[TenhouReach]
    calls: list[TenhouCall]
    agari: list[TenhouAgari]
    ryuukyoku: TenhouRyuukyoku | None
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
            reaches=tuple(self.reaches),
            calls=tuple(self.calls),
            agari=tuple(self.agari),
            ryuukyoku=self.ryuukyoku,
            events=tuple(self.events),
        )


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
                event_index=len(current.events),
                turn=len(current.discards),
            )
            current.discards.append(discard)
            current.events.append(discard)
            current.last_draws.pop(discard_seat, None)
            continue

        if tag == "REACH":
            reach = TenhouReach(
                seat=_required_int(event, "who"),
                step=_required_int(event, "step"),
                event_index=len(current.events),
                scores=_parse_optional_scores(event.attrib.get("ten")),
            )
            current.reaches.append(reach)
            current.events.append(reach)
            continue

        if tag == "N":
            meld_code = _required_int(event, "m")
            call = TenhouCall(
                seat=_required_int(event, "who"),
                meld_code=meld_code,
                event_index=len(current.events),
                meld=decode_tenhou_meld(meld_code),
            )
            current.calls.append(call)
            current.events.append(call)
            current.last_draws.pop(call.seat, None)
            continue

        if tag == "AGARI":
            agari = TenhouAgari(
                winner=_required_int(event, "who"),
                from_seat=_required_int(event, "fromwho"),
                event_index=len(current.events),
                machi=_parse_optional_tile(event.attrib.get("machi")),
                points=_parse_int_tuple(event.attrib.get("ten")),
                yaku=_parse_int_tuple(event.attrib.get("yaku")) or (),
                dora_indicators=_parse_tile_tuple(event.attrib.get("dorahai")),
                ura_dora_indicators=_parse_tile_tuple(event.attrib.get("uradorahai")),
            )
            current.agari.append(agari)
            current.events.append(agari)
            continue

        if tag == "RYUUKYOKU":
            ryuukyoku = TenhouRyuukyoku(
                event_index=len(current.events),
                reason=event.attrib.get("type"),
                scores=_parse_optional_scores(event.attrib.get("ten")),
            )
            current.ryuukyoku = ryuukyoku
            current.events.append(ryuukyoku)

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
        reaches=[],
        calls=[],
        agari=[],
        ryuukyoku=None,
        events=[],
        last_draws={},
    )


def _parse_scores(raw_scores: str) -> tuple[int, ...]:
    return tuple(int(score) * 100 for score in raw_scores.split(","))


def _parse_optional_scores(raw_scores: str | None) -> tuple[int, ...] | None:
    if raw_scores is None:
        return None
    return _parse_scores(raw_scores)


def _parse_int_tuple(raw_values: str | None) -> tuple[int, ...] | None:
    if not raw_values:
        return None
    return tuple(int(value) for value in raw_values.split(",") if value)


def _parse_optional_tile(raw_tile: str | None) -> Tile | None:
    if raw_tile is None:
        return None
    return tenhou_tile(int(raw_tile))


def _parse_tile_tuple(raw_tiles: str | None) -> tuple[Tile, ...]:
    if not raw_tiles:
        return ()
    return tuple(tenhou_tile(int(tile_id)) for tile_id in raw_tiles.split(",") if tile_id)


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
