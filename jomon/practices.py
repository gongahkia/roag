"""Sixteen bounded learned practices earned through reciprocal world work."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import CatalogError, PRACTICE_SECTIONS, load_catalog
from .state import GameState


@dataclass(frozen=True)
class Practice:
    name: str
    region_id: str
    source: str
    effect: str
    description: str


_CATALOG = load_catalog("practices.json", PRACTICE_SECTIONS)
_rows = _CATALOG["practices"]
if (not isinstance(_rows, list) or len(_rows) != 16
        or any(not isinstance(row, list) or len(row) != 5
               or any(not isinstance(value, str) or not value for value in row)
               for row in _rows)
        or len({row[0] for row in _rows}) != len(_rows)):
    raise CatalogError("practices.json has invalid practice rows")
PRACTICES = {row[0]: Practice(*row) for row in _rows}

_network = _CATALOG["network_contacts"]
_aftermath = _CATALOG["aftermath_regions"]
if (not isinstance(_network, dict) or len(_network) != 8
        or any(not isinstance(contact, str) or not contact.startswith("network-contact-")
               or not isinstance(name, str) or name not in PRACTICES for contact, name in _network.items())
        or not isinstance(_aftermath, dict) or len(_aftermath) != 8
        or any(not isinstance(region, str) or not isinstance(name, str) or name not in PRACTICES
               for region, name in _aftermath.items())):
    raise CatalogError("practices.json has invalid sources")
NETWORK_CONTACT_PRACTICE = _network
AFTERMATH_REGION_PRACTICE = _aftermath


def learned_effects(state: GameState) -> set[str]:
    known = set(state.courier.learned_techniques) if state.courier else set()
    return {practice.effect for name, practice in PRACTICES.items() if name in known}


def has_effect(state: GameState, effect: str) -> bool:
    return effect in learned_effects(state)


def teach_network_practice(state: GameState, contact_id: str) -> tuple[bool, str]:
    name = NETWORK_CONTACT_PRACTICE.get(contact_id)
    if name is None or state.courier is None:
        return False, "No embodied network instruction is available here."
    if name in state.courier.learned_techniques:
        return False, f"{state.courier.name} already knows {name}."
    state.courier.learned_techniques.append(name)
    practice = PRACTICES[name]
    state.remember(f"{state.courier.name} learned {name} through {contact_id}: {practice.description}")
    return True, f"{state.courier.name} learns {name}: {practice.description}"


def teach_aftermath_practice(state: GameState, region_id: str) -> str:
    if state.courier is None:
        return ""
    name = AFTERMATH_REGION_PRACTICE[region_id]
    if name in state.courier.learned_techniques:
        return ""
    state.courier.learned_techniques.append(name)
    practice = PRACTICES[name]
    text = f" {state.courier.name} learns {name} from the completed aftermath: {practice.description}"
    state.remember(text.strip())
    return text


def validate_practices() -> None:
    if len(PRACTICES) != 16 or len({practice.effect for practice in PRACTICES.values()}) != 16:
        raise ValueError("practices must provide sixteen distinct mechanical effects")
    if set(NETWORK_CONTACT_PRACTICE.values()) | set(AFTERMATH_REGION_PRACTICE.values()) != set(PRACTICES):
        raise ValueError("every practice needs one bounded production source")
    if set(NETWORK_CONTACT_PRACTICE) != {
        f"network-contact-{region_id}" for region_id in AFTERMATH_REGION_PRACTICE
    }:
        raise ValueError("network practice witnesses do not cover every region")


validate_practices()
