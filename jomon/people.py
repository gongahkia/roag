"""Physical tavern occupants and bounded voluntary recruitment."""

from __future__ import annotations

from .catalog import CatalogError, RECRUITMENT_SECTIONS, load_catalog
from .action_presentation import action_format
from .progression_presentation import progression_format
from .content import RECRUIT_TEMPLATES
from .state import GameState, Person, Position, stage_rng
from .people_presentation import recruit_presentation
from .vessel import HOUSEHOLD_SEATS, VISITOR_SEATS, current_area

BERTH_CAPACITY = 9
PERSONAL_RETURN_MILESTONE = 2
_TEMPLATE_FIELDS = {"id", "role", "technique", "home_region", "equipment"}
if (len(RECRUIT_TEMPLATES) != 6
        or any(set(template) != _TEMPLATE_FIELDS
               or any(not isinstance(template[key], str) or not template[key]
                      for key in _TEMPLATE_FIELDS - {"equipment"})
               or not isinstance(template["equipment"], list) or len(template["equipment"]) != 2
               or any(not isinstance(item, str) or not item for item in template["equipment"])
               for template in RECRUIT_TEMPLATES)
        or len({template["id"] for template in RECRUIT_TEMPLATES}) != len(RECRUIT_TEMPLATES)):
    raise CatalogError("people.json has invalid mechanical recruit templates")


def personal_practice(person: Person) -> str:
    """The inspectable practice earned by this particular household role."""
    return f"practice.personal:{person.role}"


_LEGACY_PERSONAL_RETURN_MEMORIES = frozenset(
    f"Courier return: survived working passage through {region}."
    for region in (
        "Hearthford", "Greywash Tidal Reach", "Greenwold Charcoal March",
        "Whitecairn Limestone Rise", "Dunmire Peat Isles", "Rillscar Iron Gorge",
        "Marlbank Clay Terraces", "Frostmere Braided Estuary",
    )
)


def normalize_personal_return_state(state: GameState) -> None:
    """Perform one exact bundled-default recovery for saves lacking a counter."""
    for person in [*state.household, *state.visitors]:
        key = f"personal-return:{person.id}"
        if key not in state.vessel_changes:
            recovered = sum(memory in _LEGACY_PERSONAL_RETURN_MEMORIES for memory in person.memories)
            if recovered:
                state.vessel_changes[key] = recovered


def record_personal_return(state: GameState, person: Person, region_id: str) -> str:
    """Advance one actor's bounded, embodied expedition development."""
    counter_key = f"personal-return:{person.id}"
    previous = int(state.vessel_changes.get(counter_key, 0))
    state.vessel_changes[counter_key] = previous + 1
    region = state.regions[region_id]
    person.memories.append(progression_format("progression.personal.memory", region=region.name))
    practice = personal_practice(person)
    from .practices import learned_practice_ids
    if previous + 1 < PERSONAL_RETURN_MILESTONE or practice in learned_practice_ids(person):
        del person.memories[:-12]
        return ""
    person.learned_techniques.append(practice)
    from .progression_presentation import technique_display_name
    development = progression_format(
        "progression.personal.development", courier=person.name,
        practice=technique_display_name(practice),
    )
    person.memories.append(development)
    del person.memories[:-12]
    state.chronicle.append(development)
    del state.chronicle[:-30]
    state.remember(development)
    return development

_requirements = load_catalog("recruitment.json", RECRUITMENT_SECTIONS)["requirements"]
_REQUIREMENT_FIELDS = {"region", "markers", "witnessed"}
if (not isinstance(_requirements, dict) or set(_requirements) != {row["id"] for row in RECRUIT_TEMPLATES}
        or any(not isinstance(visitor_id, str) or not isinstance(row, dict) or set(row) != _REQUIREMENT_FIELDS
               or not isinstance(row["region"], str) or not row["region"]
               or not isinstance(row["markers"], list) or not row["markers"]
               or any(not isinstance(marker, str) or not marker for marker in row["markers"])
               or not isinstance(row["witnessed"], str) or not row["witnessed"]
               for visitor_id, row in _requirements.items())):
    raise CatalogError("recruitment.json has invalid visitor requirements")
RECRUIT_REQUIREMENTS = {
    visitor_id: (row["region"], tuple(row["markers"]), row["witnessed"])
    for visitor_id, row in _requirements.items()
}

def create_visitors(seed: str) -> list[Person]:
    visitors: list[Person] = []
    for index, template in enumerate(RECRUIT_TEMPLATES):
        presentation = recruit_presentation(template["id"])
        relationships = {
            f"crew-{crew}": stage_rng(seed, f"visitor-relation:{index}:{crew}").choice((-1, 0, 0, 1))
            for crew in range(1, 7)
        }
        visitors.append(Person(
            id=template["id"], name=presentation["name"], role=template["role"],
            ancestry=presentation["ancestry"],
            equipment=list(template["equipment"]), technique=presentation["technique"],
            relationships=relationships, learned_techniques=[], health=10,
            max_health=10, background=presentation["background"],
            build_tendency=presentation["build_tendency"], home_region=template["home_region"],
            recruited=False, available=True, memories=[presentation["memory"]],
            recruitment_terms=presentation["terms"],
            people_presentation_id=template["id"],
        ))
    return visitors


def refresh_current_people_presentation(state: GameState) -> None:
    """Render known current people slots through the active pack after load.

    Absence of a slot means an older save cannot be identified without parsing
    prose; its stored wording remains a legacy current-state fallback.
    """
    from .people_presentation import contact_name, contact_role, household_family_name, household_first_name, regional_context
    for person in [*state.household, *state.visitors]:
        if person.given_name_slot is not None and person.family_name_slot is not None:
            try:
                person.name = f"{household_first_name(int(person.given_name_slot.removeprefix('first_')))} {household_family_name(int(person.family_name_slot.removeprefix('family_')))}"
            except (ValueError, KeyError):
                # A malformed optional slot is not a license to reinterpret
                # historical text.  State validation retains the safe fallback.
                pass
        if person.people_presentation_id:
            try:
                presentation = recruit_presentation(person.people_presentation_id)
            except KeyError:
                continue
            person.name = presentation["name"]
            person.ancestry = presentation["ancestry"]
            person.background = presentation["background"]
            person.build_tendency = presentation["build_tendency"]
            person.recruitment_terms = presentation["terms"]
    for contacts in state.contacts.values():
        for contact in contacts:
            if contact.name_slot:
                try:
                    contact.name = contact_name(int(contact.name_slot.removeprefix("contact_")))
                except (ValueError, KeyError):
                    pass
            if contact.role_slot:
                try:
                    contact.role = contact_role(int(contact.role_slot.removeprefix("role_")))
                except (ValueError, KeyError):
                    pass
    for region in state.regions.values():
        if region.people_context_slot:
            try:
                context = regional_context(int(region.people_context_slot.removeprefix("context_")))
            except (ValueError, KeyError):
                continue
            region.condition = context["condition"]
            region.work = context["work"]
            region.pressure = context["pressure"]
            region.objective_text = context["objective"]
            region.hazard = context["hazard"]


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
        return False, action_format("social.recruit.unavailable")
    if len(state.household) >= state.berth_capacity:
        return False, action_format("social.recruit.berths_full", capacity=state.berth_capacity)
    region_id, markers, witnessed = RECRUIT_REQUIREMENTS[person_id]
    changes = state.regions[region_id].changes
    if not any(changes.get(marker, False) for marker in markers):
        return False, action_format(f"social.recruit.terms.{visitor.id}")
    state.visitors.remove(visitor)
    for member in state.household:
        standing = visitor.relationships.get(member.id, 0)
        member.relationships[visitor.id] = standing
    state.household.append(visitor)
    visitor.recruited = True
    state.visitor_status[person_id] = "joined"
    visitor.memories.append(
        action_format("social.recruit.joined_memory", visitor=visitor.name, witnessed=witnessed)
    )
    state.remember(visitor.memories[-1])
    if visitor.id in state.actor_schedules:
        state.actor_schedules[visitor.id].activity = "socialising"
    return True, action_format("social.recruit.accepted", visitor=visitor.name)


def defer_visitor(state: GameState, person_id: str) -> tuple[bool, str]:
    if state.visitor_status.get(person_id) != "visiting":
        return False, action_format("social.recruit.defer.unavailable")
    state.visitor_status[person_id] = "deferred"
    return True, action_format("social.recruit.defer.success")


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
        from .character_presentation import role_display_name
        messages.append(action_format("social.recruit.visiting", visitor=visitor.name, role=role_display_name(visitor.role)))
    return messages
