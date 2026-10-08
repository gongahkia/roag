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
    if "protected" in definition.tags:
        return True
    if position in state.region.landmarks.values():
        return True
    if any(position in {link.first, link.second} for link in state.region.vertical_links):
        return True
    return any(container.position == position for container in state.region.containers)


def _tool_power(state: GameState, action_id: str) -> int:
    if state.courier is None:
        return 0
    specialized = {
        ("cut", "reed sickle"): 2,
        ("cut", "felling axe"): 2,
        ("dig", "spade"): 2,
    }
    if (action_id, state.weapon) in specialized:
        return specialized[(action_id, state.weapon)]
    if state.weapon == "spade" and action_id in {"cut", "dig"}:
        return 1
    if state.weapon in {"hand axe", "billhook", "war hammer"}:
        return 1
    if state.gear == "repair tools" or state.courier.technique == "lever craft":
        return 1
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
    power = _tool_power(state, action_id)
    if power <= 0:
        return rejected("terrain.tool_required")

    coordinate = position_key(position)
    previous_damage = state.region.terrain_damage.get(coordinate, 0)
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
        definition.action_sound,
        definition.yield_material if destroyed else None,
        definition.yield_fuel if destroyed else 0,
    )
