"""Small authored tables for Hearthford's material choices and encounters."""

from __future__ import annotations

from .arc_relics import ARC_RELIC_DESCRIPTIONS
from .catalog import ACTOR_SECTIONS, CatalogError, WORLD_TEXT_SECTIONS, load_catalog
from .expanded_weapons import ARSENAL, BOMB_AMMUNITION
from .frontier_elites import ELITE_DEFINITIONS
from .preparations import PREPARATIONS
from .work_weapons import WORK_WEAPONS

_PEOPLE = load_catalog("people.json", ("REGIONAL_CONTEXTS", "FIRST_NAMES", "FAMILY_NAMES", "ROLES", "ROLE_EQUIPMENT", "ROLE_TECHNIQUE", "RECRUIT_TEMPLATES", "CONTACT_NAMES"))
_GOODS = load_catalog("goods.json", ("COMMODITIES", "COMMODITY_LOGISTICS", "WEAPONS", "GEAR", "SUPPORTS", "DISCOVERIES", "RELICS", "PASSIVES", "MERCHANT_ITEMS"))
_ACTORS = load_catalog("actors.json", ACTOR_SECTIONS)
_WORLD_TEXT = load_catalog("world_text.json", WORLD_TEXT_SECTIONS)


def _text_rows(value: object, name: str) -> tuple[str, ...]:
    if not isinstance(value, list) or any(not isinstance(part, str) for part in value):
        raise CatalogError(f"{name} must be a list of text")
    return tuple(value)


def _tuple_rows(value: object, name: str, width: int) -> tuple[tuple, ...]:
    if not isinstance(value, list) or any(not isinstance(row, list) or len(row) != width for row in value):
        raise CatalogError(f"{name} has invalid record width")
    return tuple(tuple(row) for row in value)


def _dict_rows(value: object, name: str) -> tuple[dict, ...]:
    if not isinstance(value, list) or any(not isinstance(row, dict) for row in value):
        raise CatalogError(f"{name} has invalid records")
    return tuple(value)


def _tuple_map(value: object, name: str, width: int) -> dict[str, tuple]:
    if (not isinstance(value, dict) or any(not isinstance(key, str) or not isinstance(row, list)
                                          or len(row) != width for key, row in value.items())):
        raise CatalogError(f"{name} has invalid records")
    return {key: tuple(row) for key, row in value.items()}


def _dict_map(value: object, name: str) -> dict[str, dict]:
    if (not isinstance(value, dict) or any(not isinstance(key, str) or not isinstance(row, dict)
                                          for key, row in value.items())):
        raise CatalogError(f"{name} has invalid records")
    return value


def _text_map(value: object, name: str) -> dict[str, str]:
    if (not isinstance(value, dict) or any(not isinstance(key, str) or not isinstance(line, str)
                                          for key, line in value.items())):
        raise CatalogError(f"{name} has invalid text")
    return value


def _interface_ledgers(value: object) -> dict[str, tuple[str, ...] | str]:
    required = {"inventory", "target", "route", "base", "look"}
    if not isinstance(value, dict) or set(value) != required:
        raise CatalogError("interface_ledgers has invalid sections")
    rows = {"inventory", "route", "base"}
    if (any(not isinstance(value[key], list) or any(not isinstance(line, str) or not line for line in value[key])
            for key in rows)
            or any(not isinstance(value[key], str) or not value[key] for key in required - rows)):
        raise CatalogError("interface_ledgers has invalid text")
    return {
        key: tuple(value[key]) if key in rows else value[key]
        for key in required
    }

COMMODITIES = _dict_map(_GOODS["COMMODITIES"], "COMMODITIES")

COMMODITY_LOGISTICS = _dict_map(_GOODS["COMMODITY_LOGISTICS"], "COMMODITY_LOGISTICS")
for logistics in COMMODITY_LOGISTICS.values():
    buyers = logistics.get("buyers")
    if not isinstance(buyers, list) or any(not isinstance(buyer, str) for buyer in buyers):
        raise CatalogError("COMMODITY_LOGISTICS has invalid buyers")
    logistics["buyers"] = tuple(buyers)
for commodity, logistics in COMMODITY_LOGISTICS.items():
    COMMODITIES[commodity].update(logistics)


def validate_commodity_content() -> None:
    required = {
        "bulk", "condition", "source", "use", "handling", "failure",
        "buyers", "environment", "quest_use", "equipment_use",
    }
    for name, definition in COMMODITIES.items():
        if set(definition) != required or not all(definition[key] for key in required):
            raise ValueError(f"commodity {name!r} has an incomplete physical lifecycle")
        if not isinstance(definition["bulk"], int) or definition["bulk"] < 1:
            raise ValueError(f"commodity {name!r} has invalid bulk")
        if not isinstance(definition["buyers"], tuple) or len(definition["buyers"]) < 2:
            raise ValueError(f"commodity {name!r} needs at least two material buyers")

REGIONAL_CONTEXTS = _dict_rows(_PEOPLE["REGIONAL_CONTEXTS"], "REGIONAL_CONTEXTS")

FIRST_NAMES = _text_rows(_PEOPLE["FIRST_NAMES"], "FIRST_NAMES")
FAMILY_NAMES = _text_rows(_PEOPLE["FAMILY_NAMES"], "FAMILY_NAMES")
ROLES = _text_rows(_PEOPLE["ROLES"], "ROLES")
ROLE_EQUIPMENT = _tuple_map(_PEOPLE["ROLE_EQUIPMENT"], "ROLE_EQUIPMENT", 2)
ROLE_TECHNIQUE = _text_map(_PEOPLE["ROLE_TECHNIQUE"], "ROLE_TECHNIQUE")

RECRUIT_TEMPLATES = _dict_rows(_PEOPLE["RECRUIT_TEMPLATES"], "RECRUIT_TEMPLATES")

# Behavior stays direct in actions.py rather than becoming an ability schema.
WEAPONS = _tuple_map(_GOODS["WEAPONS"], "WEAPONS", 2)

WEAPONS.update({name: (spec.name, spec.description) for name, spec in WORK_WEAPONS.items()})

WEAPONS.update({name: (name.title(), spec.description) for name, spec in ARSENAL.items()})

GEAR = _tuple_map(_GOODS["GEAR"], "GEAR", 2)

SUPPORTS = _tuple_map(_GOODS["SUPPORTS"], "SUPPORTS", 2)

DISCOVERIES = _tuple_map(_GOODS["DISCOVERIES"], "DISCOVERIES", 2)

DISCOVERIES.update({
    "sealed pitch pot": ("ammunition", "One bulky fire pot for the pot sling; water stops ignition, not fuel loss."),
    "sealed lime pot": ("ammunition", "One abrasive cloud for the pot sling; wet lime remains caustic."),
    "sealed brine pot": ("ammunition", "One sealed pot of salt water to quench, thaw or flood a place in sight."),
})

DISCOVERIES.update({
    name: ("preparation", preparation.description)
    for name, preparation in PREPARATIONS.items()
})

RELICS = _text_map(_GOODS["RELICS"], "RELICS")

RELICS.update(ARC_RELIC_DESCRIPTIONS)

PASSIVES = _tuple_map(_GOODS["PASSIVES"], "PASSIVES", 2)

TREASURE_REWARDS = tuple(PASSIVES) + (
    "willow dressing", "dry smoke charge", "sealed tally",
    "river-glass ward", "tide-knot charm",
)

MERCHANT_ITEMS = _tuple_map(_GOODS["MERCHANT_ITEMS"], "MERCHANT_ITEMS", 2)
MERCHANT_ITEMS.update({key: (4, "weapon") for key in WORK_WEAPONS})
MERCHANT_ITEMS.update({key: (7 if spec.family == "gun" else 5, "weapon") for key, spec in ARSENAL.items()})
MERCHANT_ITEMS.update({kind.split(":", 1)[1]: (2, "consumable") for kind in BOMB_AMMUNITION.values()})
MERCHANT_ITEMS.update({f"sealed {material} pot": (2, "consumable") for material in ("pitch", "lime", "brine")})

# Bounded authored roles. Regional placement and budgets live in encounters.py;
# actions remain ordinary Threat decisions rather than data-driven scripts.
ENEMY_ARCHETYPES = _dict_map(_ACTORS["ENEMY_ARCHETYPES"], "ENEMY_ARCHETYPES")

# The older authored regional actors predate per-archetype glyph data. They
# retain their established behavior while gaining unique ASCII fallbacks; the
# renderer still assigns semantic hostile colour and bold independent of glyph.
LEGACY_STANDARD_GLYPHS = _text_map(_ACTORS["LEGACY_STANDARD_GLYPHS"], "LEGACY_STANDARD_GLYPHS")
for identity, glyph in LEGACY_STANDARD_GLYPHS.items():
    ENEMY_ARCHETYPES[identity]["glyph"] = glyph

# Named work and wildlife combinations. Behaviour lives in the bounded ecology
# and combat reducers; these rows contain no executable scripts.
FRONTIER_ACTORS = _tuple_rows(_ACTORS["FRONTIER_ACTORS"], "FRONTIER_ACTORS", 14)
for identity, region, name, profile, role, duty, ecology, vision, hearing, reach, budget, glyph, capability, counterplay in FRONTIER_ACTORS:
    ENEMY_ARCHETYPES[identity] = {
        "region": region, "name": name, "profile": profile, "role": role,
        "goal": duty or "feed within shelter", "vision": vision, "hearing": hearing,
        "range": reach, "capability": capability, "morale": 3 if ecology == "warden" else 2,
        "terrain": region, "counterplay": counterplay, "budget": budget,
        "ecology": ecology, "duty": duty, "glyph": glyph,
        "supplies": 2 if duty in {"heal", "rally"} else 3 if duty in {"brace", "drain", "quench", "kindle", "cut support"} else 0,
        "ranged_kind": {
            "dunmire": "sling", "rillscar": "crossbow",
            "marlbank": "sling", "frostmere": "longbow",
        }.get(region, "sling"),
    }

# Three further authored roles per region. They use the same bounded ecology,
# equipment, perception and material-duty reducers as the retained roster.
EXPANDED_STANDARD_ACTORS = _tuple_rows(_ACTORS["EXPANDED_STANDARD_ACTORS"], "EXPANDED_STANDARD_ACTORS", 14)
for identity, region, name, profile, role, duty, ecology, vision, hearing, reach, budget, glyph, capability, counterplay in EXPANDED_STANDARD_ACTORS:
    ENEMY_ARCHETYPES[identity] = {
        "region": region, "name": name, "profile": profile, "role": role,
        "goal": f"{duty or 'hold'} {identity}", "vision": vision,
        "hearing": hearing, "range": reach, "capability": capability,
        "morale": 3 if role == "protector" else 2, "terrain": region,
        "counterplay": counterplay, "budget": budget, "ecology": ecology,
        "duty": "" if duty == "feed" else duty, "glyph": glyph,
        "supplies": 3 if duty in {"quench", "brace", "drain", "kindle", "cut support"} else 0,
        "ranged_kind": "sling",
    }

ENEMY_ARCHETYPES.update(ELITE_DEFINITIONS)

STANDARD_REACTIONS = _text_map(_ACTORS["STANDARD_REACTIONS"], "STANDARD_REACTIONS")
for data in ENEMY_ARCHETYPES.values():
    if not data.get("elite"):
        data.setdefault("reaction", STANDARD_REACTIONS[str(data["role"])])

CONTACT_NAMES = _text_rows(_PEOPLE["CONTACT_NAMES"], "CONTACT_NAMES")

JOMON_MAP = _text_rows(_WORLD_TEXT["JOMON_MAP"], "JOMON_MAP")

HELP_LINES = _text_rows(_WORLD_TEXT["HELP_LINES"], "HELP_LINES")

INTERFACE_LEDGERS = _interface_ledgers(_WORLD_TEXT["interface_ledgers"])
