"""Serializable gameplay state and deterministic world creation."""

from __future__ import annotations

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
    REGION_MAP,
    REGIONAL_CONTEXTS,
    ROLE_EQUIPMENT,
    ROLE_TECHNIQUE,
    ROLES,
    THREAT_NAMES,
)

SAVE_FORMAT = 1
HISTORY_LIMIT = 40
MESSAGE_LIMIT = 8


class StateError(ValueError):
    """Raised when persisted or constructed state violates the save contract."""


@dataclass
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
class Region:
    condition: str
    work: str
    pressure: str
    objective_text: str
    objective_commodity: str
    opportunity_commodity: str
    hazard: str
    threat_kind: str
    resource_position: Position
    map_rows: list[str]


@dataclass
class Threat:
    name: str
    position: Position
    health: int = 4
    status: str = "watching"
    intent: str = "watches the route"
    turn: int = 0


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
    position: Position
    expedition_count: int
    loadout: str | None
    support: str | None
    support_spent: bool
    inventory: list[str]
    carried_goods: dict[str, CommodityStack]
    objective_status: str
    objective_required: int
    resource_taken: bool
    opportunity_taken: bool
    flood_control: str
    pressure_elapsed: int
    noise: int
    threat: Threat
    relic_charges: int
    history: list[str] = field(default_factory=list)
    messages: list[str] = field(default_factory=list)
    world_ended: bool = False

    @property
    def courier(self) -> Person | None:
        return next((p for p in self.household if p.id == self.active_courier_id), None)

    def add_message(self, text: str) -> None:
        self.messages.append(text)
        del self.messages[:-MESSAGE_LIMIT]

    def remember(self, text: str) -> None:
        self.history.append(text)
        del self.history[:-HISTORY_LIMIT]

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


def normalize_seed(seed: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9 -]", "", seed.strip())
    cleaned = re.sub(r"\s+", " ", cleaned)
    return cleaned[:48] or "Jomon River"


def stage_rng(seed: str, stage: str) -> random.Random:
    digest = hashlib.sha256(f"jomon:1:{normalize_seed(seed)}:{stage}".encode()).digest()
    return random.Random(int.from_bytes(digest[:16], "big"))


def _household(seed: str) -> list[Person]:
    rng = stage_rng(seed, "household")
    first = list(FIRST_NAMES)
    family = list(FAMILY_NAMES)
    rng.shuffle(first)
    rng.shuffle(family)
    people: list[Person] = []
    for index, role in enumerate(ROLES):
        people.append(Person(
            id=f"crew-{index + 1}",
            name=f"{first[index]} {family[index]}",
            role=role,
            equipment=list(ROLE_EQUIPMENT[role]),
            technique=ROLE_TECHNIQUE[role],
            relationships={},
            max_health=12 if role == "guard" else 10,
            health=12 if role == "guard" else 10,
        ))
    relation_rng = stage_rng(seed, "relationships")
    for person in people:
        person.relationships = {
            other.id: relation_rng.choice((-1, 0, 0, 1, 1, 2))
            for other in people if other.id != person.id
        }
    return people


def _region(seed: str) -> tuple[Region, Contact, Threat]:
    context_rng = stage_rng(seed, "regional-context")
    context = dict(context_rng.choice(REGIONAL_CONTEXTS))
    route_rng = stage_rng(seed, "routes-threats")
    resource_positions = (Position(43, 3), Position(43, 9), Position(43, 15))
    resource_position = resource_positions[route_rng.randrange(len(resource_positions))]
    contact_rng = stage_rng(seed, "contact")
    contact = Contact(
        id="hearthford-contact",
        name=contact_rng.choice(CONTACT_NAMES),
        role=contact_rng.choice(("weir keeper", "mill factor", "quay reeve")),
        disposition=contact_rng.choice((-1, 0, 1)),
        memories=[],
        interest=context["commodity"],
    )
    region = Region(
        condition=context["condition"],
        work=context["work"],
        pressure=context["pressure"],
        objective_text=context["objective"],
        objective_commodity=context["commodity"],
        opportunity_commodity=context["opportunity"],
        hazard=context["hazard"],
        threat_kind=route_rng.choice(THREAT_NAMES),
        resource_position=resource_position,
        map_rows=list(REGION_MAP),
    )
    threat = Threat(region.threat_kind, Position(28 + route_rng.randrange(3), 9))
    return region, contact, threat


def create_world(seed: str) -> GameState:
    seed = normalize_seed(seed)
    household = _household(seed)
    region, contact, threat = _region(seed)
    market = {name: MarketEntry(stock=2, demand=1) for name in COMMODITIES}
    market[region.objective_commodity] = MarketEntry(stock=0, demand=4)
    vessel_cargo = {
        "grain": CommodityStack(3, "dry"),
        "salt fish": CommodityStack(2, "sealed"),
        "paper": CommodityStack(1, "dry"),
    }
    relic = 1 if stage_rng(seed, "relic").randrange(5) == 0 else 0
    state = GameState(
        save_format=SAVE_FORMAT,
        seed=seed,
        world_time=0,
        household=household,
        active_courier_id=None,
        contact=contact,
        region=region,
        market=market,
        vessel_cargo=vessel_cargo,
        location="jomon",
        position=Position(3, 4),
        expedition_count=0,
        loadout=None,
        support=None,
        support_spent=False,
        inventory=[],
        carried_goods={},
        objective_status="unoffered",
        objective_required=2,
        resource_taken=False,
        opportunity_taken=False,
        flood_control="raised",
        pressure_elapsed=0,
        noise=0,
        threat=threat,
        relic_charges=relic,
    )
    state.add_message(f"Jomon reaches Hearthford. {region.condition}")
    if relic:
        state.add_message("A finite river-glass ward rests in the household stores.")
    return state


def _position(value: Any, label: str) -> Position:
    if not isinstance(value, dict) or set(value) != {"x", "y"}:
        raise StateError(f"invalid {label}")
    if not all(isinstance(value[key], int) for key in ("x", "y")):
        raise StateError(f"invalid {label}")
    return Position(value["x"], value["y"])


def game_state_from_dict(data: Any) -> GameState:
    if not isinstance(data, dict):
        raise StateError("save root must be an object")
    if data.get("save_format") != SAVE_FORMAT:
        raise StateError(f"incompatible save format; expected {SAVE_FORMAT}")
    try:
        household = [Person(**person) for person in data["household"]]
        contact = Contact(**data["contact"])
        region_data = dict(data["region"])
        region_data["resource_position"] = _position(region_data["resource_position"], "resource position")
        region = Region(**region_data)
        market = {key: MarketEntry(**value) for key, value in data["market"].items()}
        vessel = {key: CommodityStack(**value) for key, value in data["vessel_cargo"].items()}
        carried = {key: CommodityStack(**value) for key, value in data["carried_goods"].items()}
        threat_data = dict(data["threat"])
        threat_data["position"] = _position(threat_data["position"], "threat position")
        threat = Threat(**threat_data)
        state = GameState(
            save_format=data["save_format"], seed=data["seed"], world_time=data["world_time"],
            household=household, active_courier_id=data["active_courier_id"], contact=contact,
            region=region, market=market, vessel_cargo=vessel,
            location=data["location"], position=_position(data["position"], "courier position"),
            expedition_count=data["expedition_count"], loadout=data["loadout"], support=data["support"],
            support_spent=data["support_spent"], inventory=list(data["inventory"]), carried_goods=carried,
            objective_status=data["objective_status"], objective_required=data["objective_required"],
            resource_taken=data["resource_taken"], opportunity_taken=data["opportunity_taken"],
            flood_control=data["flood_control"], pressure_elapsed=data["pressure_elapsed"], noise=data["noise"],
            threat=threat, relic_charges=data["relic_charges"], history=list(data["history"]),
            messages=list(data["messages"]), world_ended=data["world_ended"],
        )
    except (KeyError, TypeError, ValueError) as exc:
        raise StateError(f"malformed save: {exc}") from exc
    validate_state(state)
    return state


def validate_state(state: GameState) -> None:
    if len(state.household) != 6 or len({p.id for p in state.household}) != 6:
        raise StateError("save must contain the six-person household")
    if state.active_courier_id is not None and state.courier is None:
        raise StateError("active courier is not in the household")
    if set(state.market) != set(COMMODITIES):
        raise StateError("save commodity catalogue is incomplete")
    if len(state.region.map_rows) != len(REGION_MAP):
        raise StateError("invalid regional map")
    if state.location not in {"jomon", "region"}:
        raise StateError("invalid location")
    if state.objective_status not in {"unoffered", "accepted", "refused", "altered", "completed", "failed"}:
        raise StateError("invalid objective state")
    if state.threat.status not in {"watching", "engaged", "defeated", "evaded", "negotiated"}:
        raise StateError("invalid threat state")
    if state.world_time < 0 or state.pressure_elapsed < 0 or state.noise < 0:
        raise StateError("negative clocks are invalid")
    if len(state.history) > HISTORY_LIMIT or len(state.messages) > MESSAGE_LIMIT:
        raise StateError("bounded history exceeded")
