"""Deterministic bridges from physical world items into courier inventory."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Item, Position


@dataclass(frozen=True)
class GroundAcquisitionOutcome:
    """Result of one atomic ground-to-pack acquisition attempt."""

    accepted: bool
    result_id: str
    item_ids: tuple[str, ...] = ()


def publish_packed_acquisition(
    state: GameState,
    item: Item,
    position: Position,
) -> bool:
    """Record and publish one first-time physical pack acquisition.

    The persistent legacy acquisition marker makes dropping and picking up the
    same item mechanically inert, preventing zero-time reaction loops.
    """
    if (
        item.location != "pack"
        or item.owner_id != state.active_courier_id
    ):
        raise ValueError("physical acquisition requires the active courier's pack")
    marker = f"acquired:{item.id}"
    already_acquired = bool(state.vessel_changes.get(marker))
    from .inventory import record_acquisition

    record_acquisition(state, item)
    if already_acquired:
        return False
    from .engine_components import resolve_resource_gained

    resolve_resource_gained(
        state,
        item.id,
        item.kind,
        item.quantity,
        position,
    )
    return True


def acquire_ground_items(
    state: GameState,
    item_ids: tuple[str, ...],
) -> GroundAcquisitionOutcome:
    """Pack items at the courier's feet and then publish their acquisition.

    Physical transfer is all-or-nothing. Mechanical acquisition reactions run
    only after every requested item has entered the active courier's pack.
    """
    if (
        type(item_ids) is not tuple
        or not item_ids
        or len(item_ids) > 100
        or any(not isinstance(item_id, str) or not item_id for item_id in item_ids)
        or len(set(item_ids)) != len(item_ids)
        or not state.active_courier_id
        or not state.auto_place_enabled
    ):
        return GroundAcquisitionOutcome(False, "inventory.acquire.unavailable")
    by_id = {item.id: item for item in state.items}
    if any(item_id not in by_id for item_id in item_ids):
        return GroundAcquisitionOutcome(False, "inventory.acquire.unavailable")
    items = [by_id[item_id] for item_id in sorted(item_ids)]
    if any(
        item.location != "ground"
        or item.region_id != state.spatial_id
        or item.ground_position != state.position
        for item in items
    ):
        return GroundAcquisitionOutcome(False, "inventory.acquire.unavailable")

    from .inventory import (
        InventoryTransaction,
        sync_legacy_load,
        transfer_to_grid,
    )

    transaction = InventoryTransaction.begin(state)
    for item in items:
        if not transfer_to_grid(
            state,
            item.id,
            "pack",
            owner_id=state.active_courier_id,
        ):
            transaction.cancel(state)
            return GroundAcquisitionOutcome(False, "inventory.acquire.no_space")
    for item in items:
        publish_packed_acquisition(state, item, state.position)
    sync_legacy_load(state)
    return GroundAcquisitionOutcome(
        True,
        "inventory.acquire.ok",
        tuple(item.id for item in items),
    )
