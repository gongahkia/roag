"""Point-bought courier identity and inspectable attributes for Jomon people."""

from __future__ import annotations

import re

from .character_presentation import household_background, role_display_name
from .catalog import CHARACTER_SECTIONS, CatalogError, load_catalog
from .state import ATTRIBUTES, GameState, Person

_CATALOG = load_catalog("character_profiles.json", CHARACTER_SECTIONS)
_competencies = _CATALOG["competencies"]
_points = _CATALOG["starting_points"]
_ancestries = _CATALOG["ancestries"]
_origins = _CATALOG["origins"]
_traits = _CATALOG["traits"]
_origin_practices = _CATALOG["origin_practices"]
_attribute_competencies = _CATALOG["attribute_competencies"]
_role_attributes = _CATALOG["role_attributes"]
_role_competencies = _CATALOG["role_competencies"]

if (not isinstance(_competencies, list) or len(_competencies) != 5
        or len(set(_competencies)) != len(_competencies)
        or any(not isinstance(value, str) or not value for value in _competencies)
        or not isinstance(_points, dict) or set(_points) != {"attributes", "competencies"}
        or any(type(value) is not int or value < 0 for value in _points.values())):
    raise CatalogError("character profiles have invalid competencies or point budgets")
COMPETENCIES = tuple(_competencies)
ATTRIBUTE_POINTS = _points["attributes"]
COMPETENCY_POINTS = _points["competencies"]

if (not isinstance(_ancestries, dict) or len(_ancestries) != 4
        or any(not isinstance(name, str) or not isinstance(row, dict) or set(row) != {"effect", "competency"}
               or not isinstance(row["effect"], str) or not row["effect"]
               or row["competency"] is not None and (
                   not isinstance(row["competency"], str) or row["competency"] not in COMPETENCIES
               )
               for name, row in _ancestries.items())
        or not isinstance(_origins, list) or len(_origins) != 4 or len(set(_origins)) != len(_origins)
        or any(not isinstance(origin, str) or not origin for origin in _origins)):
    raise CatalogError("character profiles have invalid ancestries or origins")
ANCESTRIES = tuple(_ancestries)
PEOPLE_EFFECTS = {name: row["effect"] for name, row in _ancestries.items()}
PEOPLE_COMPETENCIES = {name: row["competency"] for name, row in _ancestries.items() if row["competency"]}
ORIGINS = tuple(_origins)

if (not isinstance(_traits, dict) or len(_traits) != 5
        or any(not isinstance(name, str) or not isinstance(row, list) or len(row) != 2
               or not isinstance(row[0], str) or row[0] not in COMPETENCIES
               or not isinstance(row[1], str) or not row[1]
               for name, row in _traits.items())
        or not isinstance(_origin_practices, dict) or set(_origin_practices) != set(ORIGINS)
        or any(not isinstance(name, str) or name not in COMPETENCIES for name in _origin_practices.values())
        or not isinstance(_attribute_competencies, dict) or set(_attribute_competencies) != set(COMPETENCIES)
        or any(not isinstance(name, str) or name not in ATTRIBUTES for name in _attribute_competencies.values())):
    raise CatalogError("character profiles have invalid traits or competency links")
TRAITS = {name: tuple(row) for name, row in _traits.items()}
ORIGIN_PRACTICE = _origin_practices
ATTRIBUTE_FOR_COMPETENCY = _attribute_competencies

if (not isinstance(_role_attributes, dict) or set(_role_attributes) != set(_role_competencies)
        or any(not isinstance(role, str) or not isinstance(values, list) or len(values) != 4
               or any(not isinstance(name, str) or name not in ATTRIBUTES for name in values)
               or len(set(values)) != len(values)
               for role, values in _role_attributes.items())
        or any(not isinstance(values, list) or len(values) != 2
               or any(not isinstance(name, str) or name not in COMPETENCIES for name in values)
               or len(set(values)) != len(values)
               for values in _role_competencies.values())):
    raise CatalogError("character profiles have invalid role profiles")
ROLE_ATTRIBUTES = {role: tuple(values) for role, values in _role_attributes.items()}
ROLE_COMPETENCIES = {role: tuple(values) for role, values in _role_competencies.items()}


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
    person.background = household_background(ancestry, origin.title(), person.role)
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
        f"{person.name} — {role_display_name(person.role)}",
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
        rows.append("No first-watch changes to this adult are recorded.")
    rows.append(f"People's practice: {PEOPLE_EFFECTS.get(person.ancestry, 'No listed effect')}.")
    rows.append(f"Mana: {person.mana}/{person.max_mana}; skill points: {person.skill_points}; milestones: {len(person.skill_milestones)}.")
    from .skill_tree import NODES
    from .progression_presentation import progression_format, progression_text
    rows.append(progression_format("progression.character.learned_nodes", nodes=", ".join(NODES[node].name for node in person.skill_nodes) or progression_text("progression.character.none")))
    rows.extend((f"Technique: {person.technique}", f"Background: {person.background}"))
    return rows
