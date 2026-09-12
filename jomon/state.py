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

SAVE_FORMAT = 9
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
    strategy: int = 0
    speech: int = 0
    wayfinding: int = 0
    fieldcraft: int = 0
    craft: int = 0


@dataclass
class Contact:
    id: str
    name: str
    role: str
    disposition: int
    memories: list[str]
    interest: str
    region_id: str = "hearthford"
    position: Position | None = None


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
    extra_rewards: list[str] = field(default_factory=list)
    legendary_id: str | None = None


@dataclass
class MaterialCell:
    material: str = "soil"
    water: int = 0
    fluid: str = "fresh"
    fire: int = 0
    fuel: int = 0
    smoke: int = 0
    coating: str = ""
    support: int = 3
    collapse_due: int = 0
    ice: bool = False


@dataclass
class RegionalEvent:
    id: str
    previous: str | None
    kind: str
    material: str
    place: str
    witness: str
    institution: str
    evidence: str
    account: str
    consequence: str


@dataclass
class Institution:
    id: str
    name: str
    region_id: str
    dependency: str
    production: str
    goal: str
    dispute: str
    trust: int = 0
    obligation: int = 0
    confidence: int = 0
    last_day: int = 0
    relationships: dict[str, str] = field(default_factory=dict)
    service: str = ""
    opposition_reason: str = ""
    witnessed_acts: list[str] = field(default_factory=list)


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
    id: str = "hearthford"
    name: str = "Hearthford"
    process_name: str = "river weather"
    process_thresholds: list[int] = field(default_factory=lambda: [45, 70, 95])
    process_stage: int = 0
    local_elapsed: int = 0
    local_objective_status: str = "unoffered"
    local_objective_changed: bool = False
    materials: dict[str, MaterialCell] = field(default_factory=dict)
    material_cursor: int = 0
    generation_facts: dict[str, str | int] = field(default_factory=dict)
    regional_history: list[RegionalEvent] = field(default_factory=list)


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
    role: str = "interceptor"
    goal: str = "patrol"
    goal_reason: str = "following its assigned route"
    vision: int = 8
    hearing: int = 7
    last_known_position: Position | None = None
    home_position: Position | None = None
    objective_position: Position | None = None
    group: str = ""
    ammunition: int = 0
    reload_turns: int = 0
    alarmed: bool = False
    carrying_item_id: str | None = None
    stalled_turns: int = 0
    region_id: str = "hearthford"
    capabilities: list[str] = field(default_factory=list)
    ranged_kind: str = "crossbow"
    aimed_at: Position | None = None
    allegiance: str = ""
    ecology: str = ""
    duty: str = ""
    supplies: int = 0
    target_actor_id: str | None = None
    marked_position: Position | None = None
    reaction: str = ""
    glyph: str = ""
    injuries: dict[str, str] = field(default_factory=dict)
    conditions: dict[str, int] = field(default_factory=dict)
    uses_physical_equipment: bool = False


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
    pinned: bool = False
    merged_into: str | None = None
    fitted_to: str | None = None
    legendary_id: str | None = None


@dataclass
class TerrainStatus:
    cause: str
    remaining: int
    consequence: str


@dataclass
class SoundEvent:
    position: Position
    strength: int
    age: int = 0


@dataclass
class GroupAlert:
    position: Position
    raised_turn: int
    source_id: str


@dataclass
class RouteNode:
    id: str
    name: str
    x: int
    y: int
    kind: str
    description: str
    region_id: str | None = None
    known: bool = True
    market_interest: str = ""
    supply: int = 0
    risk: int = 0
    integrity_required: int = 0
    seasonal_note: str = ""


@dataclass
class RouteEdge:
    id: str
    first: str
    second: str
    travel_time: int
    supply_cost: int
    cargo_risk: int
    weather_exposure: int
    integrity_required: int = 0
    closed_seasons: list[str] = field(default_factory=list)
    hazard: str = "ordinary working water"


@dataclass
class ActorSchedule:
    actor_id: str
    area: str
    position: Position
    activity: str
    next_boundary: int
    destination_area: str
    destination: Position
    available: bool = True
    disposition: int = 0
    last_update: int = 0


@dataclass
class SocialIncident:
    id: str
    kind: str
    participants: list[str]
    cause: str
    status: str
    created_turn: int


@dataclass
class QuestProgress:
    stage: int = 0
    status: str = "available"
    branch: str = ""
    decisions: list[str] = field(default_factory=list)
    optional_done: bool = False
    cache_marked: bool = False
    consequence: str = ""


@dataclass(frozen=True)
class LegendaryObject:
    id: str
    name: str
    region_id: str
    base_kind: str
    maker: str
    institution_id: str
    historical_event_id: str
    provenance: str
    major_effect: str
    tradeoff: str
    interested_party: str
    clue: str
    tags: tuple[str, ...]
    range_bonus: int
    material_verbs: tuple[str, ...]


@dataclass
class RegionalContract:
    id: str
    title: str
    region_id: str
    cause: str
    topology: str
    participant_id: str
    site: Position
    commodity: str
    status: str = "available"
    stage: int = 0
    approach: str = ""
    token_item_id: str | None = None
    outcome: str = ""


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
    sound_events: list[SoundEvent]
    group_alerts: dict[str, GroupAlert]
    ammunition_by_type: dict[str, int]
    weapon_ready: int
    last_move_turn: int
    regions: dict[str, Region]
    contacts: dict[str, list[Contact]]
    region_threats: dict[str, list[Threat]]
    regional_markets: dict[str, dict[str, MarketEntry]]
    travel_count: int
    pending_destination: str | None
    voyage_kind: str | None
    voyage_status: str
    voyage_detail: str
    jomon_space: str
    vessel_integrity: int
    vessel_changes: dict[str, bool | int | str]
    route_nodes: dict[str, RouteNode]
    route_edges: list[RouteEdge]
    route_current_node: str
    route_known: list[str]
    traversed_route_edges: list[str]
    auto_place_enabled: bool
    actor_schedules: dict[str, ActorSchedule]
    bartender: Person
    merchant: Person
    bartender_stock: dict[str, int]
    drink_effects: dict[str, TerrainStatus]
    calendar_origin_day: int
    calendar_events: list[str]
    chronicle: list[str]
    pending_incident: SocialIncident | None
    last_schedule_turn: int
    questlines: dict[str, QuestProgress] = field(default_factory=dict)
    worklines: dict[str, QuestProgress] = field(default_factory=dict)
    cross_region_arc: QuestProgress = field(
        default_factory=lambda: QuestProgress(status="locked")
    )
    cross_region_arcs: dict[str, QuestProgress] = field(default_factory=dict)
    treasure_marks: dict[str, list[str]] = field(default_factory=dict)
    history: list[str] = field(default_factory=list)
    messages: list[str] = field(default_factory=list)
    world_ended: bool = False
    vessel_materials: dict[str, MaterialCell] = field(default_factory=dict)
    institutions: dict[str, Institution] = field(default_factory=dict)
    vessel_threats: list[Threat] = field(default_factory=list)
    vessel_tiles: dict[str, str] = field(default_factory=dict)
    legendary_objects: dict[str, LegendaryObject] = field(default_factory=dict)
    aftermath_quests: dict[str, QuestProgress] = field(default_factory=dict)
    regional_contracts: dict[str, RegionalContract] = field(default_factory=dict)
    tabletop: dict[str, Any] = field(default_factory=lambda: {"collections": {}, "records": [], "active_match": None})
    tavern_draw: dict[str, Any] = field(default_factory=lambda: {"hand_number": 0, "bankrolls": {}, "active_hand": None, "records": []})

    @property
    def combat_active(self) -> bool:
        return self.location == "region" or (self.location == "jomon" and self.voyage_status == "active" and bool(self.vessel_changes.get("deck_crisis")))

    @property
    def combatants(self) -> list[Threat]:
        return self.vessel_threats if self.location == "jomon" else self.threats

    @property
    def spatial_id(self) -> str:
        return "jomon" if self.location == "jomon" else self.active_region_id

    @property
    def courier(self) -> Person | None:
        return next((person for person in self.household if person.id == self.active_courier_id), None)

    @property
    def threat(self) -> Threat:
        """Compatibility convenience: first threat in the current room, then world."""
        local = self.local_threats()
        return local[0] if local else self.threats[0]

    def local_threats(self, *, active_only: bool = False) -> list[Threat]:
        threats = [threat for threat in self.combatants if threat.position.z == self.position.z]
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
    from .encounters import threat_from_archetype
    from .regions import region_reachable

    alternate_elite = stage_rng(seed, "hearthford-elite-variant").randrange(2) == 1
    road_points = [
        Position(x, y)
        for y in range(19, 31)
        for x in range(28, 64)
        if region.levels["0"][y][x] == "="
    ]
    stride = max(1, len(road_points) // 8)
    patrol = road_points[::stride][:8]
    if len(patrol) < 2:
        patrol = [region.landmarks["landing"], region.landmarks["contact"]]

    def standard(archetype, actor_id, position, health, group, *, status="watching"):
        actor = threat_from_archetype(
            archetype, position, encounter_id="hearthford", group=group,
        )
        actor.id, actor.health, actor.max_health = actor_id, health, health
        actor.status, actor.home_position = status, position
        return actor

    road = standard("hearth-bank-lookout", "road-patrol", patrol[0], 5, "road-watch")
    road.patrol = patrol
    boar = standard("hearth-reed-boar", "reed-boar", Position(54, 38), 5, "reed-wallow")
    roof = standard("hearth-roof-keeper", "tower-bow", Position(47, 10, 2), 4, "road-watch")
    roof.ammunition = 5
    levy = standard("hearth-mill-protector", "mill-spear", Position(74, 24), 5, "mill-levy")
    gantry = standard("hearth-gantry-suppressor", "gantry-bow", Position(80, 20, 1), 4, "mill-levy")
    gantry.ammunition = 7
    reavers = standard("hearth-cargo-reaver", "pressure-reavers", Position(58, 28), 6, "reavers", status="dormant")
    expanded_key = stage_rng(seed, "hearthford-expanded-role").choice((
        "hearth-sluice-runner", "hearth-rope-cutter", "hearth-meadow-kite",
    ))
    expanded = standard(
        expanded_key,
        f"hearthford-expanded:{expanded_key}",
        Position(63, 39),
        4,
        "aftermath-road",
    )
    threats = [
        road, boar, roof, levy, gantry, expanded,
        Threat(
            "floodgate-claimant" if alternate_elite else "wheel-train",
            "floodgate claimant" if alternate_elite else "runaway crown wheel",
            "reach" if alternate_elite else "machinery", Position(82, 27), 7, 7,
            status="dormant", morale=4 if alternate_elite else 99, elite=True,
            role="elite" if alternate_elite else "hazard",
            goal="open disputed sluice" if alternate_elite else "deny lane",
            capabilities=["telegraphed crossing flood"] if alternate_elite else [],
        ),
        reavers,
    ]
    # Hearthford's flood meadow is seed-shaped. Keep authored threats at their
    # intended sites when possible, but deterministically move any point cut
    # off by that shaping to the nearest connected tile. Dormant actors matter
    # here too: an unreachable thief that later wakes is still invalid content.
    reachable = region_reachable(region)
    occupied: set[Position] = set()
    for threat in threats:
        original = threat.position
        if original not in reachable or original in occupied:
            candidates = reachable - occupied
            threat.position = min(
                candidates,
                key=lambda point: (
                    abs(point.z - original.z) * 100
                    + abs(point.x - original.x)
                    + abs(point.y - original.y),
                    point.z,
                    point.y,
                    point.x,
                ),
            )
            if threat.home_position is None or threat.home_position == original:
                threat.home_position = threat.position
        occupied.add(threat.position)
    return threats


def _region(seed: str) -> tuple[Region, Contact]:
    from .topology import build_region

    context = dict(stage_rng(seed, "regional-context").choice(REGIONAL_CONTEXTS))
    spatial = build_region(seed)
    contact_rng = stage_rng(seed, "contact")
    contact = Contact(
        id="hearthford-contact", name=contact_rng.choice(CONTACT_NAMES),
        role=contact_rng.choice(("weir keeper", "mill factor", "quay reeve")),
        disposition=contact_rng.choice((-1, 0, 1)), memories=[], interest=context["commodity"],
        region_id="hearthford", position=spatial["landmarks"]["contact"],
    )
    return Region(
        condition=context["condition"], work=context["work"], pressure=context["pressure"],
        objective_text=context["objective"], objective_commodity=context["commodity"],
        opportunity_commodity=context["opportunity"], hazard=context["hazard"],
        **spatial,
    ), contact


def create_world(seed: str) -> GameState:
    from .vessel import JOMON_GANGPLANK

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
        active_courier_id=household[0].id, active_region_id="hearthford", contact=contact, region=region, market=market,
        vessel_cargo=vessel_cargo, location="jomon", current_room=None,
        position=JOMON_GANGPLANK, expedition_count=0, returned_expeditions=0,
        weapon=None, gear=None, support="route survey", support_spent=False, crossbow_loaded=True,
        owned_weapons=["billhook", "spear", "cudgel", "staff", "hand axe"],
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
        sound_events=[], group_alerts={},
        ammunition_by_type={
            "bolts": 6, "arrows": 8, "sling stones": 10,
            "heavy bolts": 4, "javelins": 4, "nets": 2,
            "handgonne charges": 3,
        },
        weapon_ready=2, last_move_turn=-99,
        regions={}, contacts={}, region_threats={}, regional_markets={},
        travel_count=0, pending_destination=None, voyage_kind=None,
        voyage_status="none", voyage_detail="",
        jomon_space="vessel", vessel_integrity=10, vessel_changes={},
        route_nodes={}, route_edges=[], route_current_node="hearthford",
        route_known=[], traversed_route_edges=[], auto_place_enabled=True,
        actor_schedules={},
        bartender=Person(
            id="bartender-sena", name="Sena Quill", role="bartender",
            equipment=["cellar key", "measuring cup"], technique="measured pour",
            relationships={}, background="Keeps Jomon's common room and knows which regional casks travel safely.",
            build_tendency="material hospitality and firm limits",
            memories=["Sena took the bar on witnessed household shares."],
        ),
        merchant=Person(
            id="merchant-veyra", name="Veyra Bale", role="itinerant deck factor",
            equipment=["oilskin account", "sample hook"], technique="regional lots",
            relationships={}, background=(
                "A coast-and-river factor who visits Jomon only when a recorded "
                "route cycle and regional stock justify the mooring."
            ),
            build_tendency="bounded tools, witnessed exchange, and regional shortages",
            memories=["Veyra first heard Jomon's name in four working markets."],
            available=False,
        ),
        bartender_stock={}, drink_effects={}, calendar_origin_day=0,
        calendar_events=[], chronicle=[], pending_incident=None,
        last_schedule_turn=0,
        aftermath_quests={}, regional_contracts={},
    )
    from .inventory import ensure_household_basics, initialise_inventory, sync_legacy_load
    from .people import initialise_tavern

    initialise_inventory(state)
    from .workshop import initialise_workshop

    initialise_workshop(state)
    initialise_tavern(state)
    ensure_household_basics(state)
    sync_legacy_load(state)
    state.threats = _threats(seed, region)
    state.regions = {"hearthford": region}
    state.contacts = {"hearthford": [contact]}
    state.region_threats = {"hearthford": state.threats}
    state.regional_markets = {"hearthford": state.market}
    from .regions import build_new_regions

    new_regions, new_contacts, new_threats, new_markets = build_new_regions(seed)
    state.regions.update(new_regions)
    state.contacts.update(new_contacts)
    state.region_threats.update(new_threats)
    state.regional_markets.update(new_markets)
    from .enemy_equipment import initialise_enemy_equipment

    initialise_enemy_equipment(state, fresh=True)
    state.contacts["hearthford"].append(
        Contact(
            "hearthford-contact-2", "Tomas Reed", "millwright speaker",
            stage_rng(seed, "hearthford:second-contact").choice((-1, 0, 1)),
            [], region.opportunity_commodity, "hearthford",
            region.landmarks["second_contact"],
        )
    )
    from .quests import initialise_quests

    initialise_quests(state)
    from .aftermath import initialise_aftermath

    initialise_aftermath(state)
    from .route_chart import initialise_route_chart
    from .vessel import initialise_living_vessel

    initialise_route_chart(state)
    from .regional_history import initialise_account, reconcile_network

    for region_id in state.regions:
        initialise_account(state, region_id, new_geography=True)
    reconcile_network(state)
    initialise_living_vessel(state)
    from .situations import initialise_region_sites

    for existing_region in state.regions.values():
        initialise_region_sites(existing_region)
    state.add_message(f"Jomon reaches Hearthford. {region.condition}")
    state.add_message(
        f"{state.courier.name} has the courier watch with a basic working kit. "
        "Press E at the gangplank to depart, or prepare further aboard.",
        priority=3,
    )
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
    migrated["save_format"] = 4
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
    migrated["sound_events"] = []
    migrated["group_alerts"] = {}
    migrated["ammunition_by_type"] = {
        "bolts": migrated.get("ammunition", 6), "arrows": 8,
        "sling stones": 10, "heavy bolts": 4, "javelins": 4, "nets": 2,
    }
    migrated["weapon_ready"] = 2
    migrated["last_move_turn"] = -99
    migrated["regions"] = {"hearthford": migrated["region"]}
    migrated["contacts"] = {"hearthford": [migrated["contact"]]}
    migrated["region_threats"] = {"hearthford": migrated["threats"]}
    migrated["regional_markets"] = {"hearthford": migrated["market"]}
    migrated["travel_count"] = 0
    migrated["pending_destination"] = None
    migrated["voyage_kind"] = None
    migrated["voyage_status"] = "none"
    migrated["voyage_detail"] = ""
    return migrated


def _migrate_v4(data: dict[str, Any]) -> dict[str, Any]:
    """Add diegetic vessel state without altering format-4 consequences."""
    migrated = copy.deepcopy(data)
    migrated["save_format"] = 5
    for item in migrated.get("items", []):
        item.setdefault("pinned", False)
        item.setdefault("merged_into", None)
    migrated.setdefault("jomon_space", "vessel")
    migrated.setdefault("vessel_integrity", 10)
    migrated.setdefault("vessel_changes", {})
    migrated.setdefault("route_nodes", {})
    migrated.setdefault("route_edges", [])
    migrated.setdefault("route_current_node", migrated.get("active_region_id", "hearthford"))
    migrated.setdefault("route_known", [])
    migrated.setdefault("traversed_route_edges", [])
    migrated.setdefault("auto_place_enabled", True)
    migrated.setdefault("actor_schedules", {})
    migrated.setdefault("bartender_stock", {})
    migrated.setdefault("drink_effects", {})
    migrated.setdefault("calendar_origin_day", 0)
    migrated.setdefault("calendar_events", [])
    migrated.setdefault("chronicle", list(migrated.get("history", [])[-12:]))
    migrated.setdefault("pending_incident", None)
    migrated.setdefault("last_schedule_turn", migrated.get("world_time", 0))
    migrated.setdefault("bartender", {
        "id": "bartender-sena", "name": "Sena Quill", "role": "bartender",
        "equipment": ["cellar key", "measuring cup"],
        "technique": "measured pour", "relationships": {},
        "learned_techniques": [], "alive": True, "health": 10,
        "max_health": 10, "injury": "none", "injuries": {},
        "background": "Keeps Jomon's common room and knows which regional casks travel safely.",
        "build_tendency": "material hospitality and firm limits",
        "home_region": "hearthford", "recruited": False,
        "available": True,
        "memories": ["Sena took the bar on witnessed household shares."],
        "recruitment_terms": "Sena is a household worker, not a recruitable visitor.",
    })
    if migrated.get("location") == "jomon":
        old = migrated.get("position", {"x": 3, "y": 4, "z": 0})
        migrated["position"] = (
            {"x": 61, "y": 10, "z": 0}
            if old.get("x") == 47 and old.get("y") == 8
            else {"x": 4, "y": 10, "z": 0}
        )
    return migrated


def _migrate_v5(data: dict[str, Any]) -> dict[str, Any]:
    """Adopt physical finite resources without resetting prior consequences."""
    migrated = copy.deepcopy(data)
    migrated["save_format"] = 6
    migrated.setdefault("vessel_changes", {})
    migrated["vessel_changes"].setdefault("format_6_physical_resources", True)
    migrated.setdefault("questlines", {})
    migrated.setdefault("cross_region_arc", {"status": "locked"})
    migrated.setdefault("treasure_marks", {})
    migrated.setdefault("merchant", asdict(Person(
        id="merchant-veyra", name="Veyra Bale", role="itinerant deck factor",
        equipment=["oilskin account", "sample hook"], technique="regional lots",
        relationships={}, background="A coast-and-river factor who visits Jomon on a recorded route cycle.",
        build_tendency="bounded regional exchange",
        memories=["Veyra first heard Jomon's name in four working markets."],
        available=False,
    )))
    return migrated


def _migrate_v6(data: dict[str, Any]) -> dict[str, Any]:
    """Add sparse fields without repainting geography or reissuing possessions."""
    migrated = copy.deepcopy(data)
    migrated["save_format"] = 7
    migrated.setdefault("vessel_materials", {})
    for region in migrated.get("regions", {}).values():
        region.setdefault("materials", {})
        region.setdefault("material_cursor", 0)
    return migrated


def _migrate_v7(data: dict[str, Any]) -> dict[str, Any]:
    migrated = copy.deepcopy(data)
    migrated["save_format"] = 8
    migrated["tabletop"] = {"collections": {}, "records": [], "active_match": None}
    return migrated


def _migrate_v8(data: dict[str, Any]) -> dict[str, Any]:
    """Discard the superseded small-board game without touching Jomon progress."""
    migrated = copy.deepcopy(data)
    migrated["save_format"] = 9
    migrated["tabletop"] = {"collections": {}, "records": [], "active_match": None}
    return migrated


def game_state_from_dict(data: Any) -> GameState:
    if not isinstance(data, dict):
        raise StateError("save root must be an object")
    migrated_v3 = data.get("save_format") == 3
    if migrated_v3:
        data = _migrate_v3(data)
    migrated_v4 = data.get("save_format") == 4
    if migrated_v4:
        data = _migrate_v4(data)
    migrated_v5 = data.get("save_format") == 5
    if migrated_v5:
        data = _migrate_v5(data)
    if data.get("save_format") == 6:
        data = _migrate_v6(data)
    if data.get("save_format") == 7:
        data = _migrate_v7(data)
    if data.get("save_format") == 8:
        data = _migrate_v8(data)
    if data.get("save_format") != SAVE_FORMAT:
        raise StateError(f"incompatible save format; expected {SAVE_FORMAT}")
    raw_region_threats = data.get(
        "region_threats", {"hearthford": data.get("threats", [])}
    )
    retrofit_enemy_equipment = any(
        "uses_physical_equipment" not in actor
        for actors in raw_region_threats.values()
        for actor in actors
    )
    try:
        def parse_contact(raw: dict[str, Any]) -> Contact:
            values = dict(raw)
            if values.get("position") is not None:
                values["position"] = _position(values["position"], "contact position")
            return Contact(**values)

        def parse_region(raw: dict[str, Any]) -> Region:
            values = dict(raw)
            values["materials"] = {key: MaterialCell(**cell) for key, cell in values.get("materials", {}).items()}
            values["regional_history"] = [RegionalEvent(**event) for event in values.get("regional_history", [])]
            values["landmarks"] = {key: _position(value, f"{key} landmark") for key, value in values["landmarks"].items()}
            values["zones"] = {key: tuple(value) for key, value in values["zones"].items()}
            values["vertical_links"] = [
                VerticalLink(_position(link["first"], "link first"), _position(link["second"], "link second"), link["name"])
                for link in values["vertical_links"]
            ]
            containers: list[Container] = []
            for raw_container in values["containers"]:
                container = dict(raw_container)
                container["position"] = _position(container["position"], "container")
                containers.append(Container(**container))
            values["containers"] = containers
            return Region(**values)

        def parse_threat(raw: dict[str, Any]) -> Threat:
            values = dict(raw)
            values["position"] = _position(values["position"], "threat position")
            values["patrol"] = [_position(value, "patrol position") for value in values.get("patrol", [])]
            for key in ("last_known_position", "home_position", "objective_position", "aimed_at", "marked_position"):
                if values.get(key) is not None:
                    values[key] = _position(values[key], f"threat {key}")
            return Threat(**values)

        household = [Person(**person) for person in data["household"]]
        visitors = [Person(**person) for person in data.get("visitors", [])]
        active_region_id = data["active_region_id"]
        regions = {
            key: parse_region(value)
            for key, value in data.get("regions", {"hearthford": data["region"]}).items()
        }
        contacts = {
            key: [parse_contact(value) for value in values]
            for key, values in data.get("contacts", {"hearthford": [data["contact"]]}).items()
        }
        hearthford = regions.get("hearthford")
        if hearthford and "second_contact" not in hearthford.landmarks:
            primary = hearthford.landmarks["contact"]
            hearthford.landmarks["second_contact"] = Position(
                primary.x - 3, primary.y + 2, primary.z
            )
        if hearthford and len(contacts.get("hearthford", [])) == 1:
            contacts["hearthford"].append(
                Contact(
                    "hearthford-contact-2", "Tomas Reed", "millwright speaker",
                    stage_rng(data["seed"], "hearthford:second-contact").choice((-1, 0, 1)),
                    [], hearthford.opportunity_commodity, "hearthford",
                    hearthford.landmarks["second_contact"],
                )
            )
        region_threats = {
            key: [parse_threat(value) for value in values]
            for key, values in data.get("region_threats", {"hearthford": data["threats"]}).items()
        }
        regional_markets = {
            region_id: {key: MarketEntry(**value) for key, value in values.items()}
            for region_id, values in data.get("regional_markets", {"hearthford": data["market"]}).items()
        }
        region = regions[active_region_id]
        contact = contacts[active_region_id][0]
        threats = region_threats[active_region_id]
        market = regional_markets[active_region_id]
        vessel = {key: CommodityStack(**value) for key, value in data["vessel_cargo"].items()}
        carried = {key: CommodityStack(**value) for key, value in data["carried_goods"].items()}
        items: list[Item] = []
        for raw in data["items"]:
            item_data = dict(raw)
            if item_data.get("ground_position") is not None:
                item_data["ground_position"] = _position(item_data["ground_position"], "ground item")
            items.append(Item(**item_data))
        terrain_statuses = {
            name: TerrainStatus(**value) for name, value in data["terrain_statuses"].items()
        }
        sound_events = [
            SoundEvent(_position(value["position"], "sound event"), value["strength"], value.get("age", 0))
            for value in data.get("sound_events", [])
        ]
        group_alerts = {
            name: GroupAlert(_position(value["position"], "group alert"), value["raised_turn"], value["source_id"])
            for name, value in data.get("group_alerts", {}).items()
        }
        route_nodes = {
            key: RouteNode(**value) for key, value in data.get("route_nodes", {}).items()
        }
        route_edges = [RouteEdge(**value) for value in data.get("route_edges", [])]
        actor_schedules = {}
        for key, raw in data.get("actor_schedules", {}).items():
            values = dict(raw)
            values["position"] = _position(values["position"], "scheduled actor")
            values["destination"] = _position(values["destination"], "schedule destination")
            actor_schedules[key] = ActorSchedule(**values)
        pending_incident = None
        if data.get("pending_incident"):
            pending_incident = SocialIncident(**data["pending_incident"])
        questlines = {
            region_id: QuestProgress(**value)
            for region_id, value in data.get("questlines", {}).items()
        }
        cross_region_arc = QuestProgress(
            **data.get("cross_region_arc", {"status": "locked"})
        )
        legendary_objects = {}
        for key, raw in data.get("legendary_objects", {}).items():
            values = dict(raw)
            values["tags"] = tuple(values["tags"])
            values["material_verbs"] = tuple(values["material_verbs"])
            legendary_objects[key] = LegendaryObject(**values)
        regional_contracts = {}
        for key, raw in data.get("regional_contracts", {}).items():
            values = dict(raw)
            values["site"] = _position(values["site"], "regional contract site")
            regional_contracts[key] = RegionalContract(**values)
        state = GameState(
            save_format=data["save_format"], seed=data["seed"], world_time=data["world_time"],
            household=household, active_courier_id=data["active_courier_id"], active_region_id=active_region_id, contact=contact,
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
            sound_events=sound_events, group_alerts=group_alerts,
            ammunition_by_type=dict(data.get("ammunition_by_type", {"bolts": data.get("ammunition", 6)})),
            weapon_ready=data.get("weapon_ready", 2), last_move_turn=data.get("last_move_turn", -99),
            regions=regions, contacts=contacts, region_threats=region_threats,
            regional_markets=regional_markets,
            travel_count=data.get("travel_count", 0),
            pending_destination=data.get("pending_destination"),
            voyage_kind=data.get("voyage_kind"),
            voyage_status=data.get("voyage_status", "none"),
            voyage_detail=data.get("voyage_detail", ""),
            jomon_space=data.get("jomon_space", "vessel"),
            vessel_integrity=data.get("vessel_integrity", 10),
            vessel_changes=dict(data.get("vessel_changes", {})),
            route_nodes=route_nodes, route_edges=route_edges,
            route_current_node=data.get("route_current_node", active_region_id),
            route_known=list(data.get("route_known", [])),
            traversed_route_edges=list(data.get("traversed_route_edges", [])),
            auto_place_enabled=data.get("auto_place_enabled", True),
            actor_schedules=actor_schedules,
            bartender=Person(**data["bartender"]),
            merchant=Person(**data.get("merchant", {
                "id": "merchant-veyra", "name": "Veyra Bale",
                "role": "itinerant deck factor", "equipment": [],
                "technique": "regional lots", "relationships": {},
                "available": False,
            })),
            bartender_stock=dict(data.get("bartender_stock", {})),
            drink_effects={
                name: TerrainStatus(**value)
                for name, value in data.get("drink_effects", {}).items()
            },
            calendar_origin_day=data.get("calendar_origin_day", 0),
            calendar_events=list(data.get("calendar_events", [])),
            chronicle=list(data.get("chronicle", [])),
            pending_incident=pending_incident,
            last_schedule_turn=data.get("last_schedule_turn", data["world_time"]),
            questlines=questlines,
            worklines={key: QuestProgress(**value) for key, value in data.get("worklines", {}).items()},
            cross_region_arc=cross_region_arc,
            cross_region_arcs={key: QuestProgress(**value) for key, value in data.get("cross_region_arcs", {}).items()},
            treasure_marks={
                region_id: list(marks)
                for region_id, marks in data.get("treasure_marks", {}).items()
            },
            history=list(data["history"]), messages=list(data["messages"]), world_ended=data["world_ended"],
            vessel_materials={key: MaterialCell(**cell) for key, cell in data.get("vessel_materials", {}).items()},
            vessel_threats=[parse_threat(value) for value in data.get("vessel_threats", [])],
            vessel_tiles=dict(data.get("vessel_tiles", {})),
            institutions={key: Institution(**value) for key, value in data.get("institutions", {}).items()},
            legendary_objects=legendary_objects,
            aftermath_quests={
                key: QuestProgress(**value)
                for key, value in data.get("aftermath_quests", {}).items()
            },
            regional_contracts=regional_contracts,
            tabletop=data.get("tabletop", {"collections": {}, "records": [], "active_match": None}),
            tavern_draw=data.get("tavern_draw", {"hand_number": 0, "bankrolls": {}, "active_hand": None, "records": []}),
        )
        if migrated_v3:
            from .inventory import initialise_inventory, reconcile_legacy_carried, sync_legacy_load
            from .people import initialise_tavern

            initialise_inventory(state)
            reconcile_legacy_carried(state)
            sync_legacy_load(state)
            initialise_tavern(state)
        elif not state.tavern_positions:
            from .people import initialise_tavern

            initialise_tavern(state)
        initialise_living = migrated_v4 or not state.route_nodes or not state.actor_schedules
        if migrated_v4 or not state.route_nodes:
            from .route_chart import initialise_route_chart

            initialise_route_chart(state)
        missing_regions = {"hearthford", "greywash", "greenwold", "whitecairn"} - set(state.regions)
        if missing_regions:
            from .regions import build_new_regions

            new_regions, new_contacts, new_threats, new_markets = build_new_regions(state.seed)
            for region_id in missing_regions:
                state.regions[region_id] = new_regions[region_id]
                state.contacts[region_id] = new_contacts[region_id]
                state.region_threats[region_id] = new_threats[region_id]
                state.regional_markets[region_id] = new_markets[region_id]
        from .route_chart import extend_route_chart

        extend_route_chart(state)
        from .workshop import initialise_workshop

        initialise_workshop(state)
        from .regional_history import initialise_account, reconcile_network

        for region_id in state.regions:
            initialise_account(state, region_id, new_geography=False)
        reconcile_network(state)
        from .quests import initialise_quests

        initialise_quests(state)
        if initialise_living:
            from .vessel import initialise_living_vessel

            initialise_living_vessel(state, migrated=migrated_v4)
        from .vessel import JOMON_GANGPLANK, normalise_schedule_work_positions

        normalise_schedule_work_positions(state)
        if state.location == "jomon":
            from .inventory import ensure_household_basics, sync_legacy_load

            if state.active_courier_id is None and not state.world_ended:
                ready = next((person for person in state.household if person.alive), None)
                if ready is not None:
                    state.active_courier_id = ready.id
                    state.jomon_space = "vessel"
                    state.position = JOMON_GANGPLANK
                    state.tavern_positions.pop(ready.id, None)
                    schedule = state.actor_schedules.get(ready.id)
                    if schedule:
                        schedule.area = schedule.destination_area = "vessel:0"
                        schedule.position = schedule.destination = JOMON_GANGPLANK
                        schedule.activity = "ready for departure"
            ensure_household_basics(state)
            if state.active_courier_id and state.support is None:
                state.support = "route survey"
            sync_legacy_load(state)
        if migrated_v5:
            from .inventory import reconcile_format_five_resources
            from .regions import add_format_six_containers

            reconcile_format_five_resources(state)
            add_format_six_containers(state)
        if state.location == "region":
            from .regions import reconstruct_regional_process

            reconstruct_regional_process(state)
        if retrofit_enemy_equipment:
            from .enemy_equipment import initialise_enemy_equipment

            initialise_enemy_equipment(state, fresh=False)
        from .aftermath import initialise_aftermath

        initialise_aftermath(state)
    except (AttributeError, KeyError, RuntimeError, TypeError, ValueError) as exc:
        raise StateError(f"malformed save: {exc}") from exc
    validate_state(state)
    return state


def validate_state(state: GameState) -> None:
    from .materials import validate_materials
    from .regional_history import validate_accounts
    from .ecology import validate_ecology
    from .worklines import validate as validate_worklines
    from .legendary import validate_legends
    from .aftermath import validate_aftermath

    try:
        validate_materials(state)
        validate_accounts(state)
        validate_ecology(state)
        validate_worklines(state)
        validate_legends(state)
        validate_aftermath(state)
    except ValueError as exc:
        raise StateError(f"invalid material state: {exc}") from exc
    from dumbest_dungeon.expedition import validate_expedition

    try:
        validate_expedition(state)
    except (KeyError, TypeError, ValueError) as exc:
        raise StateError(f"invalid Dullest Dungeon state: {exc}") from exc
    from .tavern_draw import validate_tavern_draw

    try:
        validate_tavern_draw(state)
    except (KeyError, TypeError, ValueError) as exc:
        raise StateError(f"invalid tavern draw state: {exc}") from exc
    if len(state.household) < 6 or len({person.id for person in state.household}) != len(state.household):
        raise StateError("save must retain the six-person household and unique recruits")
    if state.active_courier_id is not None and state.courier is None:
        raise StateError("active courier is not in the household")
    all_people = [*state.household, *state.visitors, state.merchant]
    if len({person.id for person in all_people}) != len(all_people):
        raise StateError("household and visitor identities must be unique")
    if any(type(getattr(person, skill)) is not int or not 0 <= getattr(person, skill) <= 20
           for person in [*all_people, state.bartender]
           for skill in ("strategy", "speech", "wayfinding", "fieldcraft", "craft")):
        raise StateError("invalid person competency")
    if state.berth_capacity < 6 or len(state.household) > state.berth_capacity:
        raise StateError("invalid Jomon berth occupancy")
    if len(set(state.tavern_positions.values())) != len(state.tavern_positions):
        raise StateError("two tavern occupants share one position")
    from .vessel import TAVERN_MAP, VESSEL_LEVELS, validate_living_vessel

    if any(
        person_id not in {person.id for person in all_people}
        or not (0 <= point.y < len(TAVERN_MAP) and 0 <= point.x < len(TAVERN_MAP[point.y]))
        for person_id, point in state.tavern_positions.items()
    ):
        raise StateError("invalid tavern occupant position")
    if set(state.market) != set(COMMODITIES):
        raise StateError("save commodity catalogue is incomplete")
    if state.location not in {"jomon", "region"}:
        raise StateError("invalid location")
    if state.jomon_space not in {"vessel", "tavern"}:
        raise StateError("invalid Jomon space")
    if state.location == "jomon" and state.jomon_space == "vessel":
        rows = VESSEL_LEVELS.get(state.position.z)
        if rows is None or not (0 <= state.position.y < len(rows) and 0 <= state.position.x < len(rows[state.position.y])):
            raise StateError("invalid vessel position")
    if state.location == "jomon" and state.jomon_space == "tavern" and not (
        0 <= state.position.y < len(TAVERN_MAP)
        and 0 <= state.position.x < len(TAVERN_MAP[state.position.y])
    ):
        raise StateError("invalid tavern position")
    if state.pending_destination is not None and state.pending_destination not in state.route_nodes:
        raise StateError("invalid pending destination")
    from .ship_crises import VOYAGES, validate_ship
    try:
        validate_ship(state)
    except (TypeError, ValueError) as exc:
        raise StateError(f"invalid vessel crisis state: {exc}") from exc
    if state.voyage_kind not in {None, *VOYAGES} or state.voyage_status not in {"none", "active", "resolved"}:
        raise StateError("invalid voyage state")
    if state.location == "region" and str(state.position.z) not in state.region.levels:
        raise StateError("invalid z-level")
    if state.objective_status not in {"unoffered", "accepted", "refused", "altered", "completed", "failed"}:
        raise StateError("invalid objective state")
    valid_status = {"dormant", "watching", "engaged", "defeated", "evaded", "negotiated", "disabled", "retreated"}
    if any(threat.status not in valid_status or str(threat.position.z) not in state.region.levels for threat in state.threats):
        raise StateError("invalid threat state")
    from .frontiers import FRONTIERS

    original_regions = {"hearthford", "greywash", "greenwold", "whitecairn"}
    expected_regions = set(state.regions)
    if not original_regions <= expected_regions or expected_regions - original_regions - set(FRONTIERS):
        raise StateError("unknown or missing regional geography")
    if set(state.contacts) != expected_regions or set(state.region_threats) != expected_regions or set(state.regional_markets) != expected_regions:
        raise StateError("regional persistence is incomplete")
    if set(state.questlines) != expected_regions or set(state.treasure_marks) != expected_regions:
        raise StateError("authored regional quest persistence is incomplete")
    valid_quest_status = {"available", "active", "refused", "resolution", "completed"}
    if any(
        quest.status not in valid_quest_status or not 0 <= quest.stage <= 3
        for quest in state.questlines.values()
    ):
        raise StateError("invalid regional quest state")
    if state.cross_region_arc.status not in {
        "locked", "available", "active", "completed",
    } or not 0 <= state.cross_region_arc.stage <= 5:
        raise StateError("invalid cross-region arc state")
    from .quests import ADDITIONAL_ARCS

    if set(state.cross_region_arcs) != set(ADDITIONAL_ARCS) or any(
        arc.status not in {"locked", "available", "active", "completed"}
        or not 0 <= arc.stage <= 4
        for arc in state.cross_region_arcs.values()
    ):
        raise StateError("invalid additional cross-region arc state")
    from .regions import validate_region

    try:
        for region in state.regions.values():
            validate_region(region)
        for region_id, threats in state.region_threats.items():
            if any(threat.status not in valid_status or str(threat.position.z) not in state.regions[region_id].levels for threat in threats):
                raise ValueError(f"invalid {region_id} threat")
    except (RuntimeError, ValueError) as exc:
        raise StateError(str(exc)) from exc
    if state.world_time < 0 or state.pressure_elapsed < 0 or state.noise < 0:
        raise StateError("negative clocks are invalid")
    if state.vessel_integrity < 0 or state.calendar_origin_day < 0:
        raise StateError("invalid vessel or calendar state")
    try:
        from .route_chart import validate_route_chart

        validate_route_chart(state)
        validate_living_vessel(state)
    except ValueError as exc:
        raise StateError(str(exc)) from exc
    try:
        from .echoes import validate_echo_state
        from .household_stories import validate_story_state
        from .interference import validate_interference_state
        from .manoeuvres import validate_manoeuvre_state
        from .situations import validate_situation_state

        validate_situation_state(state)
        validate_manoeuvre_state(state)
        validate_interference_state(state)
        validate_echo_state(state)
        validate_story_state(state)
    except ValueError as exc:
        raise StateError(f"invalid possibility state: {exc}") from exc
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
