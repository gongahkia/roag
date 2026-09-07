"""Physical tavern occupants and bounded voluntary recruitment."""

from __future__ import annotations

from .content import RECRUIT_TEMPLATES
from .state import GameState, Person, Position, stage_rng

BERTH_CAPACITY = 9

HOUSEHOLD_SEATS = (
    Position(32, 3), Position(40, 3), Position(32, 5),
    Position(40, 5), Position(32, 7), Position(40, 7),
)
VISITOR_SEATS = (
    Position(24, 9), Position(32, 9), Position(40, 9),
    Position(24, 11), Position(32, 11), Position(40, 11),
)


def create_visitors(seed: str) -> list[Person]:
    visitors: list[Person] = []
    for index, template in enumerate(RECRUIT_TEMPLATES):
        relationships = {
            f"crew-{crew}": stage_rng(seed, f"visitor-relation:{index}:{crew}").choice((-1, 0, 0, 1))
            for crew in range(1, 7)
        }
        visitors.append(Person(
            id=template["id"], name=template["name"], role=template["role"],
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
    occupant_id = next((person_id for person_id, point in state.tavern_positions.items() if point == position), None)
    return person_by_id(state, occupant_id) if occupant_id else None


def adjacent_person(state: GameState) -> Person | None:
    candidates = [
        person for person in tavern_people(state)
        if person.id in state.tavern_positions
        and max(
            abs(state.tavern_positions[person.id].x - state.position.x),
            abs(state.tavern_positions[person.id].y - state.position.y),
        ) <= 1
    ]
    return sorted(candidates, key=lambda person: person.id)[0] if candidates else None


def recruit_visitor(state: GameState, person_id: str) -> tuple[bool, str]:
    visitor = next((person for person in state.visitors if person.id == person_id), None)
    if visitor is None or state.visitor_status.get(person_id) not in {"visiting", "deferred"}:
        return False, "That visitor is not available aboard Jomon."
    if len(state.household) >= state.berth_capacity:
        return False, f"Jomon's {state.berth_capacity} adult berths are full; the invitation can be deferred."
    if state.trade_credit < 2 and not state.region.changes.get(f"recruit:{person_id}"):
        return False, visitor.recruitment_terms
    if not state.region.changes.get(f"recruit:{person_id}"):
        state.trade_credit -= 2
    state.visitors.remove(visitor)
    for member in state.household:
        standing = visitor.relationships.get(member.id, 0)
        member.relationships[visitor.id] = standing
    state.household.append(visitor)
    visitor.recruited = True
    state.visitor_status[person_id] = "joined"
    visitor.memories.append(f"{visitor.name} voluntarily joined Jomon after witnessed terms.")
    state.remember(visitor.memories[-1])
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
        used.add(state.tavern_positions[visitor.id])
        messages.append(f"{visitor.name}, {visitor.role}, is now visiting Jomon's tavern.")
    return messages
