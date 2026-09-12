"""Physical tavern occupants and bounded voluntary recruitment."""

from __future__ import annotations

from .content import RECRUIT_TEMPLATES
from .state import GameState, Person, Position, stage_rng
from .vessel import HOUSEHOLD_SEATS, VISITOR_SEATS, current_area

BERTH_CAPACITY = 9
PERSONAL_RETURN_MILESTONE = 2


def personal_practice(person: Person) -> str:
    """The inspectable practice earned by this particular household role."""
    return f"seasoned {person.role}"


def record_personal_return(state: GameState, person: Person, region_id: str) -> str:
    """Advance one actor's bounded, embodied expedition development."""
    prefix = "Courier return:"
    previous = sum(memory.startswith(prefix) for memory in person.memories)
    region = state.regions[region_id]
    person.memories.append(
        f"{prefix} survived working passage through {region.name}."
    )
    practice = personal_practice(person)
    if previous + 1 < PERSONAL_RETURN_MILESTONE or practice in person.learned_techniques:
        del person.memories[:-12]
        return ""
    person.learned_techniques.append(practice)
    development = (
        f"{person.name} becomes a {practice} after two returns: four more "
        "weight capacity and a reinforced guard now follow that person, not the office."
    )
    person.memories.append(development)
    del person.memories[:-12]
    state.chronicle.append(development)
    del state.chronicle[:-30]
    state.remember(development)
    return development

RECRUIT_REQUIREMENTS = {
    "recruit-maelin": (
        "greywash", ("tide_held", "safe_salt_delay"),
        "a tide window was held or deliberately delayed",
    ),
    "recruit-jessa": (
        "greywash", ("quest_cache_opened",),
        "the named wreck locker was recovered accountably",
    ),
    "recruit-orra": (
        "greenwold", ("medicine_coppice_saved",),
        "the medicine coppice survived the managed burn",
    ),
    "recruit-bran": (
        "greenwold", ("care_obligation_settled",),
        "a persistent injury was treated through a witnessed obligation",
    ),
    "recruit-teren": (
        "whitecairn", ("quarry_braced",),
        "the quarry was materially braced before another crossing",
    ),
    "recruit-sava": (
        "whitecairn", ("honest_bell",),
        "the false private alarm was replaced by an honest warning",
    ),
}

def create_visitors(seed: str) -> list[Person]:
    visitors: list[Person] = []
    for index, template in enumerate(RECRUIT_TEMPLATES):
        relationships = {
            f"crew-{crew}": stage_rng(seed, f"visitor-relation:{index}:{crew}").choice((-1, 0, 0, 1))
            for crew in range(1, 7)
        }
        visitors.append(Person(
            id=template["id"], name=template["name"], role=template["role"],
            ancestry={"recruit-maelin": "Tidekin", "recruit-orra": "Reedfolk",
                      "recruit-teren": "Stonefolk"}.get(template["id"], "Human"),
            equipment=list(template["equipment"]), technique=template["technique"],
            relationships=relationships, learned_techniques=[], health=10,
            max_health=10, background=template["background"],
            build_tendency=template["build_tendency"], home_region=template["home_region"],
            recruited=False, available=True, memories=[template["memory"]],
            recruitment_terms=template["terms"],
        ))
    return visitors


def initialise_tavern(state: GameState) -> None:
    state.visitors = create_visitors(state.seed)
    state.berth_capacity = BERTH_CAPACITY
    state.visitor_status = {person.id: "away" for person in state.visitors}
    # One deterministic passing visitor teaches recruitment before distant travel.
    first = stage_rng(state.seed, "first-tavern-visitor").randrange(len(state.visitors))
    state.visitor_status[state.visitors[first].id] = "visiting"
    state.tavern_positions = {
        person.id: HOUSEHOLD_SEATS[index]
        for index, person in enumerate(state.household[:6])
    }
    state.tavern_positions[state.visitors[first].id] = VISITOR_SEATS[first]


def tavern_people(state: GameState) -> list[Person]:
    people = list(state.household)
    people.extend(
        visitor for visitor in state.visitors
        if state.visitor_status.get(visitor.id) in {"visiting", "deferred"}
    )
    return people


def person_by_id(state: GameState, person_id: str) -> Person | None:
    return next((person for person in [*state.household, *state.visitors] if person.id == person_id), None)


def person_at(state: GameState, position: Position) -> Person | None:
    area = current_area(state)
    occupant_id = next(
        (
            schedule.actor_id for schedule in state.actor_schedules.values()
            if schedule.area == area and schedule.position == position
            and schedule.actor_id != state.active_courier_id
        ),
        None,
    ) if state.actor_schedules else next(
        (person_id for person_id, point in state.tavern_positions.items() if point == position), None
    )
    return person_by_id(state, occupant_id) if occupant_id else None


def adjacent_person(state: GameState) -> Person | None:
    area = current_area(state)
    candidates = []
    for person in tavern_people(state):
        schedule = state.actor_schedules.get(person.id)
        point = schedule.position if schedule and schedule.area == area else state.tavern_positions.get(person.id)
        if point and point.z == state.position.z and max(abs(point.x - state.position.x), abs(point.y - state.position.y)) <= 1:
            candidates.append(person)
    return sorted(candidates, key=lambda person: person.id)[0] if candidates else None


def recruit_visitor(state: GameState, person_id: str) -> tuple[bool, str]:
    visitor = next((person for person in state.visitors if person.id == person_id), None)
    if visitor is None or state.visitor_status.get(person_id) not in {"visiting", "deferred"}:
        return False, "That visitor is not available aboard Jomon."
    if len(state.household) >= state.berth_capacity:
        return False, f"Jomon's {state.berth_capacity} adult berths are full; the invitation can be deferred."
    region_id, markers, witnessed = RECRUIT_REQUIREMENTS[person_id]
    changes = state.regions[region_id].changes
    if not any(changes.get(marker, False) for marker in markers):
        return False, visitor.recruitment_terms
    state.visitors.remove(visitor)
    for member in state.household:
        standing = visitor.relationships.get(member.id, 0)
        member.relationships[visitor.id] = standing
    state.household.append(visitor)
    visitor.recruited = True
    state.visitor_status[person_id] = "joined"
    visitor.memories.append(
        f"{visitor.name} voluntarily joined Jomon after {witnessed}."
    )
    state.remember(visitor.memories[-1])
    if visitor.id in state.actor_schedules:
        state.actor_schedules[visitor.id].activity = "socialising"
    return True, f"{visitor.name} accepts a berth aboard Jomon."


def defer_visitor(state: GameState, person_id: str) -> tuple[bool, str]:
    if state.visitor_status.get(person_id) != "visiting":
        return False, "No invitation is open."
    state.visitor_status[person_id] = "deferred"
    return True, "The invitation remains open for a later return."


def unlock_region_visitors(state: GameState, region_id: str) -> list[str]:
    messages: list[str] = []
    used = set(state.tavern_positions.values())
    for index, visitor in enumerate(state.visitors):
        if visitor.home_region != region_id or state.visitor_status.get(visitor.id) != "away":
            continue
        state.visitor_status[visitor.id] = "visiting"
        preferred = VISITOR_SEATS[index]
        state.tavern_positions[visitor.id] = preferred if preferred not in used else next(point for point in VISITOR_SEATS if point not in used)
        schedule = state.actor_schedules.get(visitor.id)
        if schedule:
            schedule.area = schedule.destination_area = "tavern"
            schedule.position = schedule.destination = state.tavern_positions[visitor.id]
            schedule.activity = "waiting"
        used.add(state.tavern_positions[visitor.id])
        messages.append(f"{visitor.name}, {visitor.role}, is now visiting Jomon's tavern.")
    return messages
