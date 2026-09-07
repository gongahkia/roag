"""Serializable gameplay state and deterministic Hearthford world creation."""

from __future__ import annotations

from collections import deque
from dataclasses import asdict, dataclass, field
import copy
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

SAVE_FORMAT = 4
HISTORY_LIMIT = 40
MESSAGE_LIMIT = 8


class StateError(ValueError):
    """Raised when persisted or constructed state violates the save contract."""


@dataclass(frozen=True)
class Position:
    x: int
    y: int
    z: int = 0


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
    injuries: dict[str, str] = field(default_factory=dict)
    background: str = "Jomon household"
    build_tendency: str = "practical expedition work"
    home_region: str = "hearthford"
    recruited: bool = False
    available: bool = True
    memories: list[str] = field(default_factory=list)
    recruitment_terms: str = "A free adult may accept or refuse a witnessed berth."


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
class VerticalLink:
    first: Position
    second: Position
    name: str


@dataclass
class Container:
    id: str
    name: str
    position: Position
    reward: str
    requirement: str | None = None
    opened: bool = False
    item_ids: list[str] = field(default_factory=list)


@dataclass
class Region:
    condition: str
    work: str
    pressure: str
    objective_text: str
    objective_commodity: str
    opportunity_commodity: str
    hazard: str
    width: int
    height: int
    levels: dict[str, list[str]]
    landmarks: dict[str, Position]
    zones: dict[str, tuple[int, int, int, int]]
    vertical_links: list[VerticalLink]
    containers: list[Container]
    changes: dict[str, bool | int | str]
    tile_changes: dict[str, str]
    seen: list[str]
    geography_signature: str


@dataclass
class Threat:
    id: str
    name: str
    profile: str
    position: Position
    health: int
    max_health: int
    status: str = "watching"
    intent: str = "has not noticed you"
    turn: int = 0
    morale: int = 2
    elite: bool = False
    patrol: list[Position] = field(default_factory=list)
    patrol_index: int = 0


@dataclass
class Item:
    id: str
    kind: str
    location: str
    provenance: str
    owner_id: str | None = None
    x: int = 0
    y: int = 0
    rotated: bool = False
    quantity: int = 1
    condition: int = 100
    container_id: str | None = None
    region_id: str | None = None
    ground_position: Position | None = None


@dataclass
class TerrainStatus:
    cause: str
    remaining: int
    consequence: str


@dataclass
class GameState:
    save_format: int
    seed: str
    world_time: int
    household: list[Person]
    active_courier_id: str | None
    active_region_id: str
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
    owned_passives: dict[str, int]
    carried_passives: dict[str, int]
    ammunition: int
    lamp_oil: int
    rope_uses: int
    smoke_charges: int
    smoke: dict[str, int]
    water: dict[str, int]
    guarded_step: bool
    aimed_target: str | None
    weather: str
    objective_deadline: int
    objective_changed: bool
    escalation_spawned: bool
    items: list[Item]
    next_item_id: int
    pack_width: int
    pack_height: int
    locker_width: int
    locker_height: int
    terrain_statuses: dict[str, TerrainStatus]
    objective_evidence: list[str]
    visitors: list[Person]
    tavern_positions: dict[str, Position]
    visitor_status: dict[str, str]
    berth_capacity: int
    history: list[str] = field(default_factory=list)
    messages: list[str] = field(default_factory=list)
    world_ended: bool = False

    @property
    def courier(self) -> Person | None:
        return next((person for person in self.household if person.id == self.active_courier_id), None)

    @property
    def threat(self) -> Threat:
        """Compatibility convenience: first threat in the current room, then world."""
        local = self.local_threats()
        return local[0] if local else self.threats[0]

    def local_threats(self, *, active_only: bool = False) -> list[Threat]:
        threats = [threat for threat in self.threats if threat.position.z == self.position.z]
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


def _threats(seed: str, region: Region) -> list[Threat]:
    names = stage_rng(seed, "threat-names")
    elite = stage_rng(seed, "elite-machinery").randrange(4) == 0
    patrol = [Position(x, 20) for x in range(30, 45)] + [Position(x, 21) for x in range(44, 29, -1)]
    return [
        Threat("road-patrol", names.choice(("bank runner", "toll watch")), "pursuer", patrol[0], 5, 5, patrol=patrol),
        Threat("reed-boar", "bristleback reed boar", "animal", Position(54, 38), 5, 5, morale=3),
        Threat("tower-bow", "watch-roof crossbow keeper", "ranged", Position(47, 10, 2), 4, 4),
        Threat("mill-spear", "displaced mill levy", "reach", Position(74, 24), 5, 5, morale=3),
        Threat("gantry-bow", "gantry bolt carrier", "ranged", Position(80, 20, 1), 4, 4),
        Threat(
            "wheel-train", "runaway crown wheel" if elite else "unbalanced mill sweep",
            "machinery", Position(82, 27), 7 if elite else 5, 7 if elite else 5,
            morale=99, elite=elite,
        ),
        Threat("pressure-reavers", "valuable-seeking river reavers", "reach", Position(58, 28), 6, 6, status="dormant", morale=4),
    ]


def _region(seed: str) -> tuple[Region, Contact]:
    from .topology import build_region

    context = dict(stage_rng(seed, "regional-context").choice(REGIONAL_CONTEXTS))
    spatial = build_region(seed)
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
        **spatial,
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
        active_courier_id=None, active_region_id="hearthford", contact=contact, region=region, market=market,
        vessel_cargo=vessel_cargo, location="jomon", current_room=None,
        position=Position(3, 4, 0), expedition_count=0, returned_expeditions=0,
        weapon=None, gear=None, support=None, support_spent=False, crossbow_loaded=True,
        owned_weapons=["billhook", "spear", "cudgel", "staff"],
        owned_gear=["buckler", "rope", "quiet shoes", "repair tools", "smoke pot", "trade seals"],
        consumables={}, relics=relics, carried_relic=None, carried_goods={},
        objective_status="unoffered", objective_required=2, flood_control="raised",
        pressure_elapsed=0, noise=0, threats=[], trade_credit=0,
        merchant_present=False, merchant_stock=[],
        owned_passives={}, carried_passives={}, ammunition=6, lamp_oil=6,
        rope_uses=3, smoke_charges=1, smoke={}, water={}, guarded_step=False,
        aimed_target=None, weather="clear", objective_deadline=150,
        objective_changed=False, escalation_spawned=False,
        items=[], next_item_id=1, pack_width=10, pack_height=6,
        locker_width=18, locker_height=10, terrain_statuses={},
        objective_evidence=[],
        visitors=[], tavern_positions={}, visitor_status={}, berth_capacity=9,
    )
    from .inventory import initialise_inventory
    from .people import initialise_tavern

    initialise_inventory(state)
    initialise_tavern(state)
    state.threats = _threats(seed, region)
    state.add_message(f"Jomon reaches Hearthford. {region.condition}")
    if relics:
        state.add_message("A finite river-glass ward rests in the household stores.")
    return state


def _position(value: Any, label: str) -> Position:
    if not isinstance(value, dict) or set(value) != {"x", "y", "z"}:
        raise StateError(f"invalid {label}")
    if not all(isinstance(value[key], int) for key in ("x", "y", "z")):
        raise StateError(f"invalid {label}")
    return Position(value["x"], value["y"], value["z"])


def _migrate_v3(data: dict[str, Any]) -> dict[str, Any]:
    """Map the one supported Python development format without losing goods."""
    migrated = copy.deepcopy(data)
    migrated["save_format"] = SAVE_FORMAT
    migrated["active_region_id"] = "hearthford"
    migrated["items"] = []
    migrated["next_item_id"] = 1
    migrated["pack_width"], migrated["pack_height"] = 10, 6
    migrated["locker_width"], migrated["locker_height"] = 18, 10
    migrated["terrain_statuses"] = {}
    migrated["objective_evidence"] = []
    for person in migrated.get("household", []):
        person.setdefault("injuries", {})
        person.setdefault("background", "Jomon household")
        person.setdefault("build_tendency", "practical expedition work")
        person.setdefault("home_region", "hearthford")
        person.setdefault("recruited", False)
        person.setdefault("available", True)
        person.setdefault("memories", [])
        person.setdefault("recruitment_terms", "A free adult may accept or refuse a witnessed berth.")
    for container in migrated.get("region", {}).get("containers", []):
        container.setdefault("item_ids", [])
    migrated["visitors"] = []
    migrated["tavern_positions"] = {}
    migrated["visitor_status"] = {}
    migrated["berth_capacity"] = 9
    return migrated


def game_state_from_dict(data: Any) -> GameState:
    if not isinstance(data, dict):
        raise StateError("save root must be an object")
    migrated_v3 = data.get("save_format") == 3
    if migrated_v3:
        data = _migrate_v3(data)
    if data.get("save_format") != SAVE_FORMAT:
        raise StateError(f"incompatible save format; expected {SAVE_FORMAT}")
    try:
        household = [Person(**person) for person in data["household"]]
        visitors = [Person(**person) for person in data.get("visitors", [])]
        contact = Contact(**data["contact"])
        region_data = dict(data["region"])
        region_data["landmarks"] = {key: _position(value, f"{key} landmark") for key, value in region_data["landmarks"].items()}
        region_data["zones"] = {key: tuple(value) for key, value in region_data["zones"].items()}
        links: list[VerticalLink] = []
        for raw in region_data["vertical_links"]:
            links.append(VerticalLink(_position(raw["first"], "link first"), _position(raw["second"], "link second"), raw["name"]))
        region_data["vertical_links"] = links
        containers: list[Container] = []
        for raw in region_data["containers"]:
            container = dict(raw)
            container["position"] = _position(container["position"], "container")
            containers.append(Container(**container))
        region_data["containers"] = containers
        region = Region(**region_data)
        market = {key: MarketEntry(**value) for key, value in data["market"].items()}
        vessel = {key: CommodityStack(**value) for key, value in data["vessel_cargo"].items()}
        carried = {key: CommodityStack(**value) for key, value in data["carried_goods"].items()}
        threats: list[Threat] = []
        for raw in data["threats"]:
            threat_data = dict(raw)
            threat_data["position"] = _position(threat_data["position"], "threat position")
            threat_data["patrol"] = [_position(value, "patrol position") for value in threat_data.get("patrol", [])]
            threats.append(Threat(**threat_data))
        items: list[Item] = []
        for raw in data["items"]:
            item_data = dict(raw)
            if item_data.get("ground_position") is not None:
                item_data["ground_position"] = _position(item_data["ground_position"], "ground item")
            items.append(Item(**item_data))
        terrain_statuses = {
            name: TerrainStatus(**value) for name, value in data["terrain_statuses"].items()
        }
        state = GameState(
            save_format=data["save_format"], seed=data["seed"], world_time=data["world_time"],
            household=household, active_courier_id=data["active_courier_id"], active_region_id=data["active_region_id"], contact=contact,
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
            owned_passives=dict(data["owned_passives"]), carried_passives=dict(data["carried_passives"]),
            ammunition=data["ammunition"], lamp_oil=data["lamp_oil"], rope_uses=data["rope_uses"],
            smoke_charges=data["smoke_charges"], smoke=dict(data["smoke"]), water=dict(data["water"]),
            guarded_step=data["guarded_step"], aimed_target=data["aimed_target"], weather=data["weather"],
            objective_deadline=data["objective_deadline"], objective_changed=data["objective_changed"],
            escalation_spawned=data["escalation_spawned"],
            items=items, next_item_id=data["next_item_id"], pack_width=data["pack_width"],
            pack_height=data["pack_height"], locker_width=data["locker_width"],
            locker_height=data["locker_height"], terrain_statuses=terrain_statuses,
            objective_evidence=list(data["objective_evidence"]),
            visitors=visitors,
            tavern_positions={key: _position(value, "tavern occupant") for key, value in data.get("tavern_positions", {}).items()},
            visitor_status=dict(data.get("visitor_status", {})), berth_capacity=data.get("berth_capacity", 9),
            history=list(data["history"]), messages=list(data["messages"]), world_ended=data["world_ended"],
        )
        if migrated_v3:
            from .inventory import initialise_inventory, reconcile_legacy_carried
            from .people import initialise_tavern

            initialise_inventory(state)
            reconcile_legacy_carried(state)
            initialise_tavern(state)
        elif not state.tavern_positions:
            from .people import initialise_tavern

            initialise_tavern(state)
    except (AttributeError, KeyError, TypeError, ValueError) as exc:
        raise StateError(f"malformed save: {exc}") from exc
    validate_state(state)
    return state


def validate_state(state: GameState) -> None:
    if len(state.household) < 6 or len({person.id for person in state.household}) != len(state.household):
        raise StateError("save must retain the six-person household and unique recruits")
    if state.active_courier_id is not None and state.courier is None:
        raise StateError("active courier is not in the household")
    all_people = [*state.household, *state.visitors]
    if len({person.id for person in all_people}) != len(all_people):
        raise StateError("household and visitor identities must be unique")
    if state.berth_capacity < 6 or len(state.household) > state.berth_capacity:
        raise StateError("invalid Jomon berth occupancy")
    if len(set(state.tavern_positions.values())) != len(state.tavern_positions):
        raise StateError("two tavern occupants share one position")
    from .content import JOMON_MAP

    if any(
        person_id not in {person.id for person in all_people}
        or not (0 <= point.y < len(JOMON_MAP) and 0 <= point.x < len(JOMON_MAP[point.y]))
        for person_id, point in state.tavern_positions.items()
    ):
        raise StateError("invalid tavern occupant position")
    if set(state.market) != set(COMMODITIES):
        raise StateError("save commodity catalogue is incomplete")
    if state.location not in {"jomon", "region"}:
        raise StateError("invalid location")
    if state.location == "region" and str(state.position.z) not in state.region.levels:
        raise StateError("invalid z-level")
    if state.objective_status not in {"unoffered", "accepted", "refused", "altered", "completed", "failed"}:
        raise StateError("invalid objective state")
    valid_status = {"dormant", "watching", "engaged", "defeated", "evaded", "negotiated", "disabled", "retreated"}
    if any(threat.status not in valid_status or str(threat.position.z) not in state.region.levels for threat in state.threats):
        raise StateError("invalid threat state")
    if state.world_time < 0 or state.pressure_elapsed < 0 or state.noise < 0:
        raise StateError("negative clocks are invalid")
    if len(state.history) > HISTORY_LIMIT or len(state.messages) > MESSAGE_LIMIT:
        raise StateError("bounded history exceeded")
    if set(state.region.levels) != {"-1", "0", "1", "2"}:
        raise StateError("region must contain four aligned levels")
    if any(len(rows) != state.region.height or any(len(row) != state.region.width for row in rows) for rows in state.region.levels.values()):
        raise StateError("regional level dimensions are invalid")
    if any(count < 0 for count in (*state.owned_passives.values(), *state.carried_passives.values())):
        raise StateError("negative passive count")
    try:
        from .inventory import validate_inventory

        validate_inventory(state)
    except (KeyError, ValueError) as exc:
        raise StateError(f"invalid spatial inventory: {exc}") from exc
    from .world import connected_required_map
    if not connected_required_map(state):
        raise StateError("required Hearthford landmarks are unreachable")
