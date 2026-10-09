"""Deterministic physical interaction with ordinary regional terrain.

This module resolves legality and sparse terrain mutation. It deliberately
does not advance the world clock, emit sound, format presentation text, or
handle authored features such as doors, controls, links, and fragile floors.
"""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position
from .terrain import TerrainDefinition, replace_terrain, terrain_at


PHYSICAL_TERRAIN_ACTIONS = frozenset({"break", "cut", "dig"})


@dataclass(frozen=True)
class TerrainActionResolution:
    accepted: bool
    changed: bool
    destroyed: bool
    result_id: str
    action_id: str
    position: Position
    terrain_id: str | None = None
    replacement_terrain_id: str | None = None
    material_id: str | None = None
    damage: int = 0
    remaining: int = 0
    sound: int = 0
    yield_material: str | None = None
    yield_fuel: int = 0
    assistance_power: int = 0
    assistance_sound: int = 0
    yield_item_kind: str | None = None
    yield_item_quantity: int = 0
    support_loss: int = 0


def supports_terrain_action(
    state: GameState, action_id: str, position: Position,
) -> bool:
    if state.location != "region":
        return False
    definition = terrain_at(state.region, position)
    return definition.destructible and action_id in definition.tool_actions


def routes_terrain_action(
    state: GameState, action_id: str, position: Position,
) -> bool:
    """Whether the material UI should delegate a physical verb to this seam."""
    return (
        state.location == "region"
        and action_id in PHYSICAL_TERRAIN_ACTIONS
        and terrain_at(state.region, position).destructible
    )


def _protected(state: GameState, position: Position, definition: TerrainDefinition) -> bool:
    # Physical definitions decide destructibility.  Authored cells no longer
    # receive blanket immunity; destroying a required run link is handled as
    # an explicit causal loss by run_progression after the action resolves.
    return "protected" in definition.tags


def terrain_action_power(state: GameState, action_id: str) -> int:
    """Return the active courier's authoritative power for a terrain verb.

    This is deliberately the same query used by resolution.  Read-only audits
    and inspection UI must not grow a second, approximate loadout table.
    """
    if state.courier is None:
        return 0
    carried_kinds = {
        item.kind
        for item in state.items
        if item.owner_id == state.active_courier_id
        and item.location == "pack"
    }
    # Retain the legacy mirrors for compatibility with direct reducer fixtures;
    # normal play keeps them synchronized with the physical readied slots.
    carried_kinds.update(kind for kind in (state.weapon, state.gear) if kind)
    specialized = {
        ("cut", "reed sickle"): 2,
        ("cut", "felling axe"): 2,
        ("dig", "spade"): 2,
    }
    power = max(
        (value for (verb, kind), value in specialized.items()
         if verb == action_id and kind in carried_kinds),
        default=0,
    )
    if "spade" in carried_kinds and action_id in {"cut", "dig"}:
        power = max(power, 1)
    if carried_kinds & {"hand axe", "billhook", "war hammer"}:
        power = max(power, 1)
    if "repair tools" in carried_kinds or state.courier.technique == "lever craft":
        power = max(power, 1)
    if state.run is not None and state.run.status == "active":
        from .run_items import effect_value

        power += effect_value(state, "terrain_power")
    if power:
        return power
    from .legendary import permits_material
    from .practices import has_effect

    if permits_material(state, action_id) or has_effect(state, f"material-{action_id}"):
        return 1
    return 0


def resolve_terrain_action(
    state: GameState, action_id: str, position: Position,
) -> TerrainActionResolution:
    """Apply one validated hit without advancing time or producing UI text."""
    from .world import distance, line_of_sight, position_key

    rejected = lambda result_id: TerrainActionResolution(
        False, False, False, result_id, action_id, position,
    )
    if (
        state.location != "region"
        or distance(state.position, position) > 1
        or not line_of_sight(state, state.position, position)
    ):
        return rejected("terrain.invalid_target")
    definition = terrain_at(state.region, position)
    if not definition.destructible or action_id not in definition.tool_actions:
        return rejected("terrain.unsupported")
    if _protected(state, position, definition):
        return rejected("terrain.protected")
    power = terrain_action_power(state, action_id)
    if power <= 0:
        return rejected("terrain.tool_required")

    coordinate = position_key(position)
    previous_damage = state.region.terrain_damage.get(coordinate, 0)
    from .engine_components import resolve_terrain_assistance

    assistance = resolve_terrain_assistance(
        state,
        state.active_courier_id or "courier",
        action_id,
        definition.id,
        position,
        max(0, definition.hardness - previous_damage - power),
    )
    sound_reduction = 0
    if state.run is not None and state.run.status == "active":
        from .run_items import effect_value

        sound_reduction = effect_value(state, "sound_reduction")
    power += assistance.power
    applied = min(power, definition.hardness - previous_damage)
    total = previous_damage + applied
    destroyed = total >= definition.hardness
    replacement = None
    if destroyed:
        if definition.replacement_glyph is None:
            raise RuntimeError(f"destructible terrain {definition.id!r} has no replacement")
        replacement = replace_terrain(
            state.region, position, definition.replacement_glyph,
        )
    else:
        state.region.terrain_damage[coordinate] = total
    return TerrainActionResolution(
        True,
        True,
        destroyed,
        "terrain.destroyed" if destroyed else "terrain.damaged",
        action_id,
        position,
        definition.id,
        replacement.id if replacement is not None else None,
        definition.material,
        applied,
        max(0, definition.hardness - total),
        max(0, definition.action_sound + assistance.sound - sound_reduction),
        definition.yield_material if destroyed else None,
        definition.yield_fuel if destroyed else 0,
        assistance.power,
        assistance.sound,
        definition.yield_item_kind if destroyed else None,
        definition.yield_item_quantity if destroyed else 0,
        definition.support_loss_on_destroy if destroyed else 0,
    )
