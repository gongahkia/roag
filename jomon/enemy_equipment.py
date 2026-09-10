"""Physical hostile equipment and shared injury consequences.

This is deliberately a narrow composition layer over Item and Threat. It does
not introduce a second inventory or a full anatomical simulation.
"""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position, Threat, stage_rng


REMOVED = {"defeated", "disabled", "retreated", "evaded", "negotiated"}
BODY_SLOTS = ("head", "torso", "arms", "hands", "legs", "feet")


@dataclass(frozen=True)
class EnemyHarm:
    amount: int
    location: str
    protection: str
    injury: str
    defeated: bool
    dropped: str = ""


REGIONAL_ARMOUR = {
    "hearthford": ("boiled cap", "quilted jack"),
    "greywash": ("reed brim", "reedscale vest"),
    "greenwold": ("felt hood", "quilted jack"),
    "whitecairn": ("ridge visor", "riveted coat"),
    "dunmire": ("reed brim", "cork-backed coat"),
    "rillscar": ("ridge visor", "riveted coat"),
    "marlbank": ("kiln face wrap", "kiln apron"),
    "frostmere": ("felt hood", "winter felt coat"),
    "jomon": ("boiled cap", "quilted jack"),
}


def _weapon_kind(actor: Threat) -> str:
    if actor.profile == "ranged":
        return actor.ranged_kind if actor.ranged_kind in {
            "crossbow", "longbow", "sling", "heavy crossbow", "handgonne",
        } else "crossbow"
    return {
        "protector": "spear",
        "controller": "weighted net",
        "flanker": "paired knives",
        "thief": "billhook",
        "lookout": "staff",
        "elite": "pike",
        "shooter": "crossbow",
        "suppressor": "staff sling",
    }.get(actor.role, "cudgel" if actor.profile == "pursuer" else "spear")


def physical_actor(actor: Threat) -> bool:
    return actor.profile not in {"animal", "machinery"}


def actor_items(state: GameState, actor: Threat) -> list:
    return [
        item for item in state.items
        if item.owner_id == actor.id
        and item.location in {"readied", "secondary", *BODY_SLOTS, "enemy"}
        and item.condition > 0
    ]


def readied_weapon(state: GameState, actor: Threat):
    return next(
        (
            item for item in state.items
            if item.owner_id == actor.id and item.location == "readied"
            and item.condition > 0
        ),
        None,
    )


def issue_enemy_equipment(state: GameState, actor: Threat, region_id: str) -> None:
    """Issue one persistent loadout; later loss never causes replacement."""
    if actor.uses_physical_equipment or not physical_actor(actor):
        return
    from .inventory import create_item, item_spec

    weapon = create_item(
        state, _weapon_kind(actor),
        f"{actor.group or actor.allegiance or region_id} working issue carried by {actor.name}",
        location="readied", owner_id=actor.id,
        condition=85 if actor.elite else 70,
    )
    if item_spec(weapon.kind).category != "weapon":
        raise ValueError(f"enemy weapon {weapon.kind!r} is not physical weapon content")
    head, torso = REGIONAL_ARMOUR.get(region_id, REGIONAL_ARMOUR["hearthford"])
    create_item(
        state, head, f"weathered {region_id} protection worn by {actor.name}",
        location="head", owner_id=actor.id, condition=75 if actor.elite else 55,
    )
    create_item(
        state, torso, f"working protection worn by {actor.name}",
        location="torso", owner_id=actor.id, condition=80 if actor.elite else 60,
    )
    actor.uses_physical_equipment = True


def initialise_enemy_equipment(state: GameState, *, fresh: bool) -> None:
    """Populate new worlds and safely retrofit only still-present old actors."""
    for region_id, actors in state.region_threats.items():
        region = state.regions[region_id]
        for actor in actors:
            marker = f"enemy_kit:{actor.id}"
            if region.changes.get(marker):
                continue
            if fresh or actor.status not in REMOVED:
                issue_enemy_equipment(state, actor, region_id)
            region.changes[marker] = 1


def armour_item(state: GameState, actor: Threat, location: str):
    return next(
        (
            item for item in state.items
            if item.owner_id == actor.id and item.location == location
            and item.condition > 0
        ),
        None,
    )


def drop_enemy_equipment(state: GameState, actor: Threat) -> str:
    from .inventory import item_spec

    dropped = []
    for item in actor_items(state, actor):
        if item.location == "enemy" and item.id == actor.carrying_item_id:
            continue
        item.location, item.owner_id, item.container_id = "ground", None, None
        item.region_id, item.ground_position = state.spatial_id, actor.position
        dropped.append(item_spec(item.kind).name)
    if not dropped:
        return ""
    return " Physical kit falls: " + ", ".join(dropped) + "."


def hit_location(state: GameState, actor: Threat, damage_kind: str, source: str) -> str:
    exposed = (
        ("torso", "arms", "head", "legs")
        if damage_kind == "pierce"
        else ("arms", "torso", "legs", "head", "hands", "feet")
    )
    rng = stage_rng(
        state.seed,
        f"enemy-hit:{state.world_time}:{actor.id}:{source}:{actor.position}",
    )
    return exposed[rng.randrange(len(exposed))]


def harm_enemy(
    state: GameState,
    actor: Threat,
    amount: int,
    source: str,
    *,
    damage_kind: str = "blunt",
    location: str | None = None,
) -> EnemyHarm:
    """Apply the same physical protection/injury reducer from any damage source."""
    from .inventory import item_spec
    from .workshop import effective_spec

    location = location or hit_location(state, actor, damage_kind, source)
    armour = armour_item(state, actor, location)
    protection = 0
    protection_name = "uncovered"
    if armour:
        spec = effective_spec(state, armour)
        protection = {
            "cut": spec.cut, "pierce": spec.pierce, "blunt": spec.blunt,
        }.get(damage_kind, 0)
        if armour.condition <= 25:
            protection = max(0, protection - 1)
        protection_name = spec.name
    absorbed = min(max(0, amount - 1), protection)
    dealt = max(0, amount - absorbed)
    if armour and absorbed:
        armour.condition = max(0, armour.condition - 6 - absorbed * 4)
        if armour.condition == 0:
            armour.location, armour.owner_id = "destroyed", None
            protection_name += " (broken)"
    actor.health = max(0, actor.health - dealt)
    injury = ""
    if actor.uses_physical_equipment and actor.health and (
        dealt >= 2 or actor.health <= actor.max_health // 2
    ):
        injury = {
            "head": "concussed",
            "torso": "winded",
            "arms": "wounded arm",
            "hands": "damaged hand",
            "legs": "strained leg",
            "feet": "wounded foot",
        }[location]
        actor.injuries[location] = injury
        actor.morale -= 1
    dropped = ""
    if actor.health == 0:
        actor.status = "defeated"
        actor.intent = f"fell after {source} struck {location}"
        from .inventory import release_enemy_possession

        dropped = release_enemy_possession(state, actor) + drop_enemy_equipment(state, actor)
    return EnemyHarm(dealt, location, protection_name, injury, actor.health == 0, dropped)


def attack_penalty(state: GameState, actor: Threat) -> int:
    weapon = readied_weapon(state, actor)
    penalty = int(any(slot in actor.injuries for slot in ("head", "arms", "hands")))
    if (
        actor.conditions.get("smoking")
        or actor.conditions.get("chilled")
        or (actor.profile == "ranged" and actor.conditions.get("wet"))
    ):
        penalty += 1
    if actor.uses_physical_equipment and weapon and weapon.condition <= 25:
        penalty += 1
    return min(2, penalty)


def wear_readied_weapon(state: GameState, actor: Threat, amount: int = 1) -> None:
    weapon = readied_weapon(state, actor)
    if weapon:
        weapon.condition = max(0, weapon.condition - amount)
        if weapon.condition == 0:
            weapon.location, weapon.owner_id = "destroyed", None
            actor.intent = "its physical weapon has broken; seeks another or withdraws"


def tick_enemy_conditions(state: GameState) -> None:
    for actor in state.combatants:
        for condition in list(actor.conditions):
            actor.conditions[condition] -= 1
            if actor.conditions[condition] <= 0:
                del actor.conditions[condition]


def recover_ground_weapon(state: GameState, actor: Threat, point: Position) -> str:
    from .inventory import item_spec

    item = next(
        (
            item for item in state.items
            if item.location == "ground" and item.region_id == state.spatial_id
            and item.ground_position == point and item.condition > 0
            and item_spec(item.kind).category == "weapon"
        ),
        None,
    )
    if item is None:
        return ""
    item.location, item.owner_id, item.ground_position = "readied", actor.id, None
    actor.morale = max(1, actor.morale)
    actor.intent = f"readies the fallen {item_spec(item.kind).name}"
    return f"The {actor.name} {actor.intent}; that exact weapon is no longer on the ground."
