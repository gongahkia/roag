"""Bridge destroyed semantic terrain to existing physical inventory."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState
from .terrain_actions import TerrainActionResolution


@dataclass(frozen=True)
class TerrainYieldOutcome:
    """One deterministic physical yield created by a terrain action."""

    item_id: str
    item_kind: str
    quantity: int
    packed: bool


def materialize_terrain_yield(
    state: GameState,
    resolution: TerrainActionResolution,
    provenance: str,
) -> TerrainYieldOutcome | None:
    """Create a configured yield, packing it when possible or grounding it.

    Terrain mutation is never rolled back for inventory capacity. Only an item
    that actually enters the active courier's pack emits ``resource.gained``.
    """
    if (
        not resolution.destroyed
        or resolution.yield_item_kind is None
        or resolution.yield_item_quantity <= 0
    ):
        return None
    from .inventory import auto_place, create_item, sync_legacy_load

    item = create_item(
        state,
        resolution.yield_item_kind,
        provenance,
        quantity=resolution.yield_item_quantity,
    )
    packed = bool(
        state.auto_place_enabled
        and state.active_courier_id
        and auto_place(
            state, item.id, "pack", owner_id=state.active_courier_id,
        )
    )
    if not packed:
        item.location = "ground"
        item.owner_id = None
        item.region_id = state.spatial_id
        item.ground_position = resolution.position
        item.container_id = None
    sync_legacy_load(state)
    if packed:
        from .acquisition import publish_packed_acquisition

        publish_packed_acquisition(
            state,
            item,
            resolution.position,
        )
    return TerrainYieldOutcome(item.id, item.kind, item.quantity, packed)
