"""Serializable gameplay state and deterministic Hearthford world creation."""

from __future__ import annotations

from collections import deque
from dataclasses import asdict, dataclass, field
import hashlib
import random
import re
from typing import Any

from .content import (
    COMMODITIES,
    CONTACT_NAMES,
    FAMILY_NAMES,
    FIRST_NAMES,
    REGIONAL_CONTEXTS,
    ROLE_EQUIPMENT,
    ROLE_TECHNIQUE,
    ROLES,
)

SAVE_FORMAT = 2
HISTORY_LIMIT = 40
MESSAGE_LIMIT = 8


class StateError(ValueError):
    """Raised when persisted or constructed state violates the save contract."""


@dataclass(frozen=True)
class Position:
    x: int
    y: int


@dataclass
class Person:
    id: str
    name: str
    role: str
    equipment: list[str]
    technique: str
    relationships: dict[str, int]
    learned_techniques: list[str] = field(default_factory=list)
    alive: bool = True
    health: int = 10
    max_health: int = 10
    injury: str = "none"


@dataclass
class Contact:
    id: str
    name: str
    role: str
    disposition: int
    memories: list[str]
    interest: str


@dataclass
class CommodityStack:
    quantity: int
    condition: str


@dataclass
class MarketEntry:
    stock: int
    demand: int


@dataclass
class RoomExit:
    target: str
    position: Position
    arrival: Position


@dataclass
class Room:
    id: str
    name: str
    place: str
    purpose: str
    map_rows: list[str]
    exits: dict[str, RoomExit]
    changes: dict[str, bool | int | str]
    discovered: bool
    discovery: str | None
    depth: int


@dataclass
class Region:
    condition: str
    work: str
    pressure: str
    objective_text: str
    objective_commodity: str
    opportunity_commodity: str
    hazard: str
    rooms: dict[str, Room]
    topology_signature: str
    contact_room: str = "tally_house"
    objective_room: str = "wheelhouse"


@dataclass
class Threat:
    id: str
    name: str
    profile: str
    room_id: str
    position: Position
    health: int
    max_health: int
    status: str = "watching"
    intent: str = "has not noticed you"
    turn: int = 0
    morale: int = 2
    elite: bool = False


@dataclass
class GameState:
    save_format: int
    seed: str
    world_time: int
    household: list[Person]
    active_courier_id: str | None
    contact: Contact
    region: Region
    market: dict[str, MarketEntry]
    vessel_cargo: dict[str, CommodityStack]
    location: str
    current_room: str | None
    position: Position
    expedition_count: int
    returned_expeditions: int
    weapon: str | None
    gear: str | None
    support: str | None
    support_spent: bool
    crossbow_loaded: bool
    owned_weapons: list[str]
    owned_gear: list[str]
    consumables: dict[str, int]
    relics: dict[str, int]
    carried_relic: str | None
    carried_goods: dict[str, CommodityStack]
    objective_status: str
    objective_required: int
    flood_control: str
    pressure_elapsed: int
    noise: int
    threats: list[Threat]
    trade_credit: int
    merchant_present: bool
    merchant_stock: list[str]
    history: list[str] = field(default_factory=list)
    messages: list[str] = field(default_factory=list)
    world_ended: bool = False

    @property
    def courier(self) -> Person | None:
        return next((person for person in self.household if person.id == self.active_courier_id), None)

    @property
    def room(self) -> Room | None:
        return self.region.rooms.get(self.current_room) if self.current_room else None

    @property
    def threat(self) -> Threat:
        """Compatibility convenience: first threat in the current room, then world."""
        local = self.local_threats()
        return local[0] if local else self.threats[0]

    def local_threats(self, *, active_only: bool = False) -> list[Threat]:
        threats = [threat for threat in self.threats if threat.room_id == self.current_room]
        if active_only:
            threats = [threat for threat in threats if threat.status in {"watching", "engaged"}]
        return threats

    def add_message(self, text: str, *, priority: int = 2) -> None:
        text = text.strip()
        if not text or (self.messages and self.messages[-1] == text):
            return
        if priority <= 0 and len(self.messages) >= MESSAGE_LIMIT - 2:
            return
        self.messages.append(text)
        del self.messages[:-MESSAGE_LIMIT]

    def remember(self, text: str) -> None:
        if self.history and self.history[-1] == text:
            return
        self.history.append(text)
        del self.history[:-HISTORY_LIMIT]

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


def normalize_seed(seed: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9 -]", "", seed.strip())
    cleaned = re.sub(r"\s+", " ", cleaned)
    return cleaned[:48] or "Jomon River"


def stage_rng(seed: str, stage: str) -> random.Random:
    digest = hashlib.sha256(f"jomon:2:{normalize_seed(seed)}:{stage}".encode()).digest()
    return random.Random(int.from_bytes(digest[:16], "big"))


def _household(seed: str) -> list[Person]:
    rng = stage_rng(seed, "household")
    first, family = list(FIRST_NAMES), list(FAMILY_NAMES)
    rng.shuffle(first)
    rng.shuffle(family)
    people: list[Person] = []
    for index, role in enumerate(ROLES):
        health = 12 if role == "guard" else 10
        people.append(Person(
            id=f"crew-{index + 1}", name=f"{first[index]} {family[index]}", role=role,
            equipment=list(ROLE_EQUIPMENT[role]), technique=ROLE_TECHNIQUE[role],
            relationships={}, health=health, max_health=health,
        ))
    relation_rng = stage_rng(seed, "relationships")
    for person in people:
        person.relationships = {
            other.id: relation_rng.choice((-1, 0, 0, 1, 1, 2))
            for other in people if other.id != person.id
        }
    return people


def _threats(seed: str) -> list[Threat]:
    names = stage_rng(seed, "threat-names")
    elite = stage_rng(seed, "elite-machinery").randrange(4) == 0
    return [
        Threat("reed-pursuer", names.choice(("bank runner", "cut-purse watch")), "pursuer", "reed_gate", Position(21, 6), 4, 4),
        Threat("eel-boar", "bristleback reed boar", "animal", "eel_cut", Position(22, 5), 5, 5, morale=3),
        Threat("towpath-bow", "towpath crossbow watcher", "ranged", "lower_towpath", Position(21, 6), 3, 3),
        Threat("yard-spear", "displaced levy spearman", "reach", "mill_yard", Position(18, 6), 4, 4, morale=3),
        Threat("yard-bow", "levy bolt carrier", "ranged", "mill_yard", Position(22, 4), 3, 3),
        Threat(
            "wheel-train", "runaway crown wheel" if elite else "unbalanced mill sweep",
            "machinery", "wheelhouse", Position(17, 6), 7 if elite else 5, 7 if elite else 5,
            morale=99, elite=elite,
        ),
    ]


def _region(seed: str) -> tuple[Region, Contact]:
    from .topology import build_rooms

    context = dict(stage_rng(seed, "regional-context").choice(REGIONAL_CONTEXTS))
    rooms, signature = build_rooms(seed)
    contact_rng = stage_rng(seed, "contact")
    contact = Contact(
        id="hearthford-contact", name=contact_rng.choice(CONTACT_NAMES),
        role=contact_rng.choice(("weir keeper", "mill factor", "quay reeve")),
        disposition=contact_rng.choice((-1, 0, 1)), memories=[], interest=context["commodity"],
    )
    return Region(
        condition=context["condition"], work=context["work"], pressure=context["pressure"],
        objective_text=context["objective"], objective_commodity=context["commodity"],
        opportunity_commodity=context["opportunity"], hazard=context["hazard"],
        rooms=rooms, topology_signature=signature,
    ), contact


def create_world(seed: str) -> GameState:
    seed = normalize_seed(seed)
    household = _household(seed)
    region, contact = _region(seed)
    market = {name: MarketEntry(stock=2, demand=1) for name in COMMODITIES}
    market[region.objective_commodity] = MarketEntry(stock=0, demand=4)
    vessel_cargo = {
        "grain": CommodityStack(3, "dry"),
        "salt fish": CommodityStack(2, "sealed"),
        "paper": CommodityStack(1, "dry"),
    }
    relics = {"river-glass ward": 1} if stage_rng(seed, "river-glass").randrange(5) == 0 else {}
    state = GameState(
        save_format=SAVE_FORMAT, seed=seed, world_time=0, household=household,
        active_courier_id=None, contact=contact, region=region, market=market,
        vessel_cargo=vessel_cargo, location="jomon", current_room=None,
        position=Position(3, 4), expedition_count=0, returned_expeditions=0,
        weapon=None, gear=None, support=None, support_spent=False, crossbow_loaded=True,
        owned_weapons=["billhook", "spear", "cudgel", "staff"],
        owned_gear=["buckler", "rope", "quiet shoes", "repair tools", "smoke pot", "cargo harness", "trade seals", "hooded lantern"],
        consumables={}, relics=relics, carried_relic=None, carried_goods={},
        objective_status="unoffered", objective_required=2, flood_control="raised",
        pressure_elapsed=0, noise=0, threats=_threats(seed), trade_credit=0,
        merchant_present=False, merchant_stock=[],
    )
    state.add_message(f"Jomon reaches Hearthford. {region.condition}")
    if relics:
        state.add_message("A finite river-glass ward rests in the household stores.")
    return state


def _position(value: Any, label: str) -> Position:
    if not isinstance(value, dict) or set(value) != {"x", "y"}:
        raise StateError(f"invalid {label}")
    if not all(isinstance(value[key], int) for key in ("x", "y")):
        raise StateError(f"invalid {label}")
    return Position(value["x"], value["y"])


def _room(room_id: str, value: Any) -> Room:
    data = dict(value)
    exits: dict[str, RoomExit] = {}
    for direction, raw in data["exits"].items():
        exit_data = dict(raw)
        exit_data["position"] = _position(exit_data["position"], f"{room_id} exit")
        exit_data["arrival"] = _position(exit_data["arrival"], f"{room_id} arrival")
        exits[direction] = RoomExit(**exit_data)
    data["exits"] = exits
    return Room(**data)


def game_state_from_dict(data: Any) -> GameState:
    if not isinstance(data, dict):
        raise StateError("save root must be an object")
    if data.get("save_format") != SAVE_FORMAT:
        raise StateError(f"incompatible save format; expected {SAVE_FORMAT}")
    try:
        household = [Person(**person) for person in data["household"]]
        contact = Contact(**data["contact"])
        region_data = dict(data["region"])
        region_data["rooms"] = {key: _room(key, value) for key, value in region_data["rooms"].items()}
        region = Region(**region_data)
        market = {key: MarketEntry(**value) for key, value in data["market"].items()}
        vessel = {key: CommodityStack(**value) for key, value in data["vessel_cargo"].items()}
        carried = {key: CommodityStack(**value) for key, value in data["carried_goods"].items()}
        threats: list[Threat] = []
        for raw in data["threats"]:
            threat_data = dict(raw)
            threat_data["position"] = _position(threat_data["position"], "threat position")
            threats.append(Threat(**threat_data))
        state = GameState(
            save_format=data["save_format"], seed=data["seed"], world_time=data["world_time"],
            household=household, active_courier_id=data["active_courier_id"], contact=contact,
            region=region, market=market, vessel_cargo=vessel, location=data["location"],
            current_room=data["current_room"], position=_position(data["position"], "courier position"),
            expedition_count=data["expedition_count"], returned_expeditions=data["returned_expeditions"],
            weapon=data["weapon"], gear=data["gear"], support=data["support"],
            support_spent=data["support_spent"], crossbow_loaded=data["crossbow_loaded"],
            owned_weapons=list(data["owned_weapons"]), owned_gear=list(data["owned_gear"]),
            consumables=dict(data["consumables"]), relics=dict(data["relics"]),
            carried_relic=data["carried_relic"], carried_goods=carried,
            objective_status=data["objective_status"], objective_required=data["objective_required"],
            flood_control=data["flood_control"], pressure_elapsed=data["pressure_elapsed"],
            noise=data["noise"], threats=threats, trade_credit=data["trade_credit"],
            merchant_present=data["merchant_present"], merchant_stock=list(data["merchant_stock"]),
            history=list(data["history"]), messages=list(data["messages"]), world_ended=data["world_ended"],
        )
    except (AttributeError, KeyError, TypeError, ValueError) as exc:
        raise StateError(f"malformed save: {exc}") from exc
    validate_state(state)
    return state


def validate_state(state: GameState) -> None:
    if len(state.household) != 6 or len({person.id for person in state.household}) != 6:
        raise StateError("save must contain the six-person household")
    if state.active_courier_id is not None and state.courier is None:
        raise StateError("active courier is not in the household")
    if set(state.market) != set(COMMODITIES):
        raise StateError("save commodity catalogue is incomplete")
    if state.location not in {"jomon", "region"}:
        raise StateError("invalid location")
    if state.location == "region" and state.current_room not in state.region.rooms:
        raise StateError("invalid current room")
    if state.objective_status not in {"unoffered", "accepted", "refused", "altered", "completed", "failed"}:
        raise StateError("invalid objective state")
    valid_status = {"watching", "engaged", "defeated", "evaded", "negotiated", "disabled", "retreated"}
    if any(threat.status not in valid_status or threat.room_id not in state.region.rooms for threat in state.threats):
        raise StateError("invalid threat state")
    if state.world_time < 0 or state.pressure_elapsed < 0 or state.noise < 0:
        raise StateError("negative clocks are invalid")
    if len(state.history) > HISTORY_LIMIT or len(state.messages) > MESSAGE_LIMIT:
        raise StateError("bounded history exceeded")
    for room in state.region.rooms.values():
        for direction, exit_ in room.exits.items():
            target = state.region.rooms.get(exit_.target)
            if target is None or not any(back.target == room.id for back in target.exits.values()):
                raise StateError(f"non-reciprocal exit in {room.id}:{direction}")
    seen = {"hearthford_quay"}
    queue = deque(seen)
    while queue:
        for exit_ in state.region.rooms[queue.popleft()].exits.values():
            if exit_.target not in seen:
                seen.add(exit_.target)
                queue.append(exit_.target)
    if state.region.objective_room not in seen or len(seen) != len(state.region.rooms):
        raise StateError("required rooms are unreachable")
