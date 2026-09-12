"""Point-bought courier identity and inspectable attributes for Jomon people."""

from __future__ import annotations

import re

from .state import ATTRIBUTES, GameState, Person

COMPETENCIES = ("strategy", "speech", "wayfinding", "fieldcraft", "craft")
ATTRIBUTE_POINTS = 8
COMPETENCY_POINTS = 5
ANCESTRIES = ("Human", "Reedfolk", "Stonefolk", "Tidekin")
PEOPLE_EFFECTS = {
    "Human": "specified courier gains +1 more in the trait's competency",
    "Reedfolk": "+1 Fieldcraft; +1 sight in Greywash",
    "Stonefolk": "+1 Craft; +4 carrying weight",
    "Tidekin": "+1 Wayfinding; +1 sight in fog or coastal squalls",
}
PEOPLE_COMPETENCIES = {
    "Reedfolk": "fieldcraft", "Stonefolk": "craft", "Tidekin": "wayfinding",
}
ORIGINS = ("hearthford", "greywash", "greenwold", "whitecairn")
TRAITS = {
    "steady": ("craft", "Patient hands at exact work"),
    "observant": ("fieldcraft", "Notices the useful detail"),
    "diplomatic": ("speech", "Finds a workable term"),
    "methodical": ("strategy", "Plans around limits"),
    "wayfarer": ("wayfinding", "Reads routes from experience"),
}
ORIGIN_PRACTICE = {
    "hearthford": "speech", "greywash": "wayfinding",
    "greenwold": "fieldcraft", "whitecairn": "craft",
}
ATTRIBUTE_FOR_COMPETENCY = {
    "strategy": "intellect", "speech": "presence", "wayfinding": "perception",
    "fieldcraft": "agility", "craft": "intellect",
}
ROLE_ATTRIBUTES = {
    "bargemaster": ("strength", "endurance", "intellect", "presence"),
    "pilot": ("agility", "endurance", "perception", "intellect"),
    "factor": ("presence", "intellect", "perception", "agility"),
    "carpenter": ("strength", "endurance", "perception", "intellect"),
    "guard": ("strength", "agility", "endurance", "perception"),
    "healer": ("endurance", "perception", "intellect", "presence"),
}
ROLE_COMPETENCIES = {
    "bargemaster": ("wayfinding", "speech"),
    "pilot": ("wayfinding", "fieldcraft"),
    "factor": ("speech", "strategy"),
    "carpenter": ("craft", "fieldcraft"),
    "guard": ("fieldcraft", "strategy"),
    "healer": ("craft", "speech"),
}


def default_allocation(role: str) -> tuple[dict[str, int], dict[str, int]]:
    attributes = {name: 6 for name in ATTRIBUTES}
    for name in ROLE_ATTRIBUTES[role]:
        attributes[name] += 2
    competencies = {name: 0 for name in COMPETENCIES}
    primary, secondary = ROLE_COMPETENCIES[role]
    competencies[primary], competencies[secondary] = 3, 2
    return attributes, competencies


def attribute_modifier(person: Person, name: str) -> int:
    return (person.attributes[name] - 6) // 2


def effective_competency(person: Person, name: str) -> int:
    if name not in COMPETENCIES:
        raise ValueError("unknown competency")
    bonus = int(PEOPLE_COMPETENCIES.get(person.ancestry) == name)
    if person.character_specified:
        bonus += attribute_modifier(person, ATTRIBUTE_FOR_COMPETENCY[name])
        bonus += int(TRAITS.get(person.trait, (None,))[0] == name)
        bonus += int(ORIGIN_PRACTICE.get(person.origin) == name)
        bonus += int(person.ancestry == "Human" and TRAITS.get(person.trait, (None,))[0] == name)
    return max(0, min(20, getattr(person, name) + bonus))


def clean_name(value: str) -> str:
    name = " ".join(value.strip().split())
    if (not 2 <= len(name) <= 32 or not any(character.isalpha() for character in name)
            or any(not (character.isalpha() or character in " -'") for character in name)
            or re.search(r"[-']{2}", name)):
        raise ValueError("use 2-32 letters, spaces, hyphens, or apostrophes")
    return name


def apply_character_spec(state: GameState, *, crew_index: int, name: str, ancestry: str,
                         origin: str, trait: str, attributes: dict[str, int],
                         competencies: dict[str, int]) -> Person:
    from .inventory import equipped_item, sync_legacy_load
    from .vessel import JOMON_GANGPLANK

    if (state.world_time != 0 or state.expedition_count != 0 or state.location != "jomon"
            or state.jomon_space != "vessel" or state.position != JOMON_GANGPLANK):
        raise ValueError("character specification belongs to new-world creation")
    if type(crew_index) is not int or not 0 <= crew_index < len(state.household):
        raise ValueError("choose an existing household adult")
    person = state.household[crew_index]
    if not person.alive or not person.available:
        raise ValueError("that household adult cannot take the watch")
    name = clean_name(name)
    if any(other.id != person.id and other.name.casefold() == name.casefold()
           for other in [*state.household, *state.visitors, state.bartender, state.merchant]):
        raise ValueError("another named person already has that name")
    if ancestry not in ANCESTRIES or origin not in ORIGINS or trait not in TRAITS:
        raise ValueError("choose an offered ancestry, origin, and trait")
    if (not isinstance(attributes, dict) or set(attributes) != set(ATTRIBUTES)
            or any(type(value) is not int or not 4 <= value <= 10 for value in attributes.values())
            or sum(attributes.values()) != 6 * 6 + ATTRIBUTE_POINTS):
        raise ValueError("spend exactly eight attribute points; each score must be 4-10")
    if (not isinstance(competencies, dict) or set(competencies) != set(COMPETENCIES)
            or any(type(value) is not int or not 0 <= value <= 5 for value in competencies.values())
            or sum(competencies.values()) != COMPETENCY_POINTS):
        raise ValueError("spend exactly five starting competency points")
    old_name = state.courier.name
    chosen_name = person.name
    old_id = state.active_courier_id
    if old_id != person.id:
        previous = state.actor_schedules[old_id]
        selected = state.actor_schedules[person.id]
        previous.area = previous.destination_area = "tavern"
        previous.position = previous.destination = selected.position
        previous.activity = "between watches"
        selected.area = selected.destination_area = "vessel:0"
        selected.position = selected.destination = JOMON_GANGPLANK
        selected.activity = "ready for departure"
        state.tavern_positions[old_id] = previous.position
        state.tavern_positions.pop(person.id, None)
        state.active_courier_id = person.id
    person.name = name
    person.ancestry = ancestry
    person.origin = origin
    person.trait = trait
    person.character_specified = True
    person.home_region = origin
    person.background = f"A {ancestry} adult raised in {origin.title()}; now Jomon's {person.role}."
    person.attributes = attributes.copy()
    for skill, value in competencies.items():
        setattr(person, skill, value)
    health = (12 if person.role == "guard" else 10) + attribute_modifier(person, "endurance") * 2
    person.max_health = max(4, health)
    person.health = person.max_health
    state.position = JOMON_GANGPLANK
    readied = equipped_item(state, "readied", person.id)
    secondary = equipped_item(state, "secondary", person.id)
    state.weapon = readied.kind if readied else None
    state.gear = secondary.kind if secondary else None
    sync_legacy_load(state)
    state.messages = [line.replace(old_name, name).replace(chosen_name, name) for line in state.messages]
    state.history = [line.replace(old_name, name).replace(chosen_name, name) for line in state.history]
    return person


def character_sheet(person: Person) -> list[str]:
    rows = [
        f"{person.name} — {person.role}",
        f"People: {person.ancestry}; origin: {person.origin.title()}; trait: {person.trait}",
        f"Health: {person.health}/{person.max_health}; injury: {person.injury}",
        "ATTRIBUTES / 4-12 (6 is an ordinary baseline)",
    ]
    explanations = {
        "strength": "2 carrying weight per modifier",
        "agility": "fieldcraft and quiet passage",
        "endurance": "2 starting health per modifier",
        "perception": "sight and wayfinding",
        "intellect": "strategy and craft",
        "presence": "speech and mediation",
    }
    for name in ATTRIBUTES:
        modifier = attribute_modifier(person, name)
        rows.append(f"{name.title():12} {person.attributes[name]:2} ({modifier:+d}) — {explanations[name]}")
    rows.append("COMPETENCIES / 0-20 (earned; effective includes profile)")
    for name in COMPETENCIES:
        rows.append(f"{name.title():12} {getattr(person, name):2} base / {effective_competency(person, name):2} effective")
    if person.character_specified:
        rows.append(f"Origin practice: +1 {ORIGIN_PRACTICE.get(person.origin, 'none')}; trait: {TRAITS[person.trait][1]}.")
    else:
        rows.append("This generated adult has no player-assigned starting point buy.")
    rows.append(f"People's practice: {PEOPLE_EFFECTS.get(person.ancestry, 'No listed effect')}.")
    rows.append(f"Mana: {person.mana}/{person.max_mana}; skill points: {person.skill_points}; milestones: {len(person.skill_milestones)}.")
    rows.append("Skill tree: " + (", ".join(person.skill_nodes) or "none") + ". Press P outside this page to cross-train.")
    rows.extend((f"Technique: {person.technique}", f"Background: {person.background}"))
    return rows
