"""Run-local, three-action class kits for the combat-first Roag loop.

Each class has exactly one ordinary attack, one movement action, and one
signature action.  Build items never create another active slot; they modify
these actions or react to their outcomes through :mod:`roag.run_items`.
"""

from __future__ import annotations

from dataclasses import dataclass
from types import MappingProxyType
from typing import TYPE_CHECKING, Mapping

if TYPE_CHECKING:
    from .state import GameState


@dataclass(frozen=True)
class RunClassDefinition:
    id: str
    name: str
    description: str
    basic_description: str
    weapon: str
    secondary: str
    ammunition: str | None
    starting_ammunition: int
    maximum_health: int
    movement_id: str
    movement_name: str
    movement_description: str
    movement_cooldown: int
    signature_id: str
    signature_name: str
    signature_description: str
    signature_cooldown: int


_DEFINITIONS = (
    RunClassDefinition(
        "breaker", "Breaker",
        "Close pressure; breach cover and turn collisions into damage.",
        "Strong adjacent strike",
        "war hammer", "repair tools", None, 0, 14,
        "charge", "Charge",
        "Move into a nearby threat and drive it back. [B]", 3,
        "slam", "Slam",
        "Damage adjacent enemies and crack suitable nearby terrain. [X]", 4,
    ),
    RunClassDefinition(
        "marksman", "Marksman",
        "Find firing lanes, align shots, and vault over cover.",
        "Precise ranged shot",
        "staff sling", "quiet shoes", "sling stones", 12, 10,
        "vault", "Vault",
        "Cross a short visible obstruction or gap to a legal landing. [B]", 3,
        "piercing_shot", "Piercing shot",
        "Fire through a visible lane, striking each enemy in order. [X]", 4,
    ),
    RunClassDefinition(
        "trickster", "Trickster",
        "Skirmish up close; swap places and misdirect pursuers.",
        "Quick close strike",
        "paired knives", "rope", None, 0, 10,
        "exchange", "Exchange",
        "Swap with one eligible visible threat. [B]", 3,
        "decoy", "Decoy",
        "Place a short-lived decoy that nearby enemies investigate. [X]", 4,
    ),
    RunClassDefinition(
        "sapper", "Sapper",
        "Plan delayed blasts, hop clear, and deny enemy routes.",
        "Modest ranged shot",
        "sling", "smoke pot", "sling stones", 12, 11,
        "hop", "Propulsion hop",
        "Hop to a legal nearby landing without taking another turn. [B]", 2,
        "charge", "Timed charge",
        "Place a charge that detonates after two world turns. [X]", 4,
    ),
)

RUN_CLASSES: Mapping[str, RunClassDefinition] = MappingProxyType(
    {definition.id: definition for definition in _DEFINITIONS}
)
DEFAULT_RUN_CLASS_ID = "breaker"


def class_definition(class_id: str) -> RunClassDefinition:
    try:
        return RUN_CLASSES[class_id]
    except KeyError as exc:
        raise ValueError(f"unknown run class {class_id!r}") from exc


def active_class(state: "GameState") -> RunClassDefinition | None:
    run = state.run
    if run is None:
        return None
    return RUN_CLASSES.get(run.class_id)


def apply_class_kit(state: "GameState", class_id: str) -> RunClassDefinition:
    """Issue a deterministic fresh-run kit without changing household identity.

    Replaced Roag working items are kept in the locker.  The kit has no weight
    loophole: its physical weapon, secondary and ammunition are ordinary
    inventory objects, while run boons remain the only non-spatial build state.
    """
    definition = class_definition(class_id)
    courier = state.courier
    if courier is None or state.run is None:
        raise ValueError("a run class requires an active courier and run")

    from .inventory import (
        AMMUNITION_ITEMS, auto_place, create_item, equipped_item, sync_legacy_load,
    )

    # A direct ``start_run`` call is also used by deterministic scenario tests
    # and by non-terminal clients.  Remove the previous disposable issue before
    # issuing a new one so spare sling stones, tools, and equipment cannot leak
    # from one run build into the next.
    for item in state.items:
        if (
            item.owner_id == courier.id
            and item.provenance.startswith("run class issue:")
        ):
            item.location, item.owner_id = "lost", None

    for slot in ("readied", "secondary"):
        previous = equipped_item(state, slot, courier.id)
        if previous is None:
            continue
        previous.location, previous.owner_id = "lost", None
        if not auto_place(state, previous.id, "locker"):
            raise RuntimeError("the Roag locker cannot stow a replaced class-kit item")

    provenance = f"run class issue: {definition.id}"
    create_item(
        state, definition.weapon, provenance,
        location="readied", owner_id=courier.id,
    )
    create_item(
        state, definition.secondary, provenance,
        location="secondary", owner_id=courier.id,
    )
    # Every class can make deliberate use of the common destructible terrain
    # rules without spending an active slot.  This ordinary packed tool is
    # distinct from the fixed combat kit and remains subject to pack limits.
    if definition.secondary != "repair tools" and not any(
        item.kind == "repair tools" and item.owner_id == courier.id
        and item.location == "pack"
        for item in state.items
    ):
        tool = create_item(state, "repair tools", provenance, owner_id=courier.id)
        if not auto_place(state, tool.id, "pack", owner_id=courier.id):
            raise RuntimeError("the class terrain tool does not fit the courier pack")
    if definition.ammunition is not None:
        ammunition = create_item(
            state, AMMUNITION_ITEMS[definition.ammunition], provenance,
            owner_id=courier.id, quantity=definition.starting_ammunition,
        )
        if not auto_place(state, ammunition.id, "pack", owner_id=courier.id):
            raise RuntimeError("the class ammunition does not fit the courier pack")

    state.weapon, state.gear = definition.weapon, definition.secondary
    state.weapon_ready = 2 if definition.weapon == "handgonne" else 1
    state.crossbow_loaded = True
    state.aimed_target = None
    courier.max_health = definition.maximum_health
    courier.health = definition.maximum_health
    courier.alive = True
    courier.injury = "none"
    courier.injuries.clear()
    state.run.class_id = definition.id
    state.run.movement_cooldown = 0
    state.run.signature_cooldown = 0
    state.run.movement_uses = 0
    state.run.signature_uses = 0
    sync_legacy_load(state)
    return definition
