"""Bounded spatial inventory, armour, load, and terrain-status rules."""

from __future__ import annotations

import copy
from dataclasses import dataclass, replace
from typing import Iterable

from .state import GameState, Item, Person, Position, TerrainStatus

PACK_WIDTH = 10
PACK_HEIGHT = 6
LOCKER_WIDTH = 18
LOCKER_HEIGHT = 10
BODY_SLOTS = ("head", "torso", "arms", "hands", "legs", "feet")
EQUIPPED_LOCATIONS = ("readied", "secondary", *BODY_SLOTS)

# A working issue, not a class or permanent build. These items use the same
# slots, weight, condition, loss, and replacement rules as discovered gear.
BASIC_COURIER_LOADOUTS: dict[str, tuple[str, str]] = {
    "bargemaster": ("billhook", "rope"),
    "pilot": ("staff", "quiet shoes"),
    "factor": ("cudgel", "trade seals"),
    "carpenter": ("hand axe", "repair tools"),
    "guard": ("spear", "buckler"),
    "healer": ("staff", "rope"),
    "tide runner": ("javelins", "rope"),
    "netwright": ("weighted net", "rope"),
    "charcoal scout": ("longbow", "smoke pot"),
    "resin healer": ("staff", "rope"),
    "quarry climber": ("war hammer", "repair tools"),
    "ridge ward": ("sling", "quiet shoes"),
}

BASIC_COURIER_ARMOUR: dict[str, dict[str, str]] = {
    "carpenter": {"hands": "work gloves"},
    "guard": {
        "head": "boiled cap", "arms": "leather vambraces",
        "feet": "hobnailed boots",
    },
    "healer": {"arms": "linen sleeves"},
    "tide runner": {"feet": "marsh waders"},
    "netwright": {"hands": "tarred gauntlets"},
    "resin healer": {"arms": "linen sleeves"},
    "quarry climber": {"feet": "hobnailed boots"},
    "ridge ward": {"legs": "leather leggings"},
}


@dataclass(frozen=True)
class ItemSpec:
    name: str
    abbreviation: str
    width: int
    height: int
    weight: int
    category: str
    description: str
    slot: str | None = None
    stack_limit: int = 1
    cut: int = 0
    pierce: int = 0
    blunt: int = 0
    coverage: int = 0
    noise: int = 0
    mobility: int = 0
    tags: tuple[str, ...] = ()


@dataclass(frozen=True)
class PlacementPreview:
    cells: frozenset[tuple[int, int]]
    valid: bool
    reason: str
    blockers: tuple[str, ...]
    resulting_weight: int
    resulting_load: str


def _armour(
    name: str,
    abbreviation: str,
    slot: str,
    shape: tuple[int, int],
    weight: int,
    protection: tuple[int, int, int],
    coverage: int,
    description: str,
    *,
    noise: int = 0,
    mobility: int = 0,
    tags: tuple[str, ...] = (),
) -> ItemSpec:
    return ItemSpec(
        name, abbreviation, *shape, weight, "armour", description, slot=slot,
        cut=protection[0], pierce=protection[1], blunt=protection[2],
        coverage=coverage, noise=noise, mobility=mobility, tags=tags,
    )


# Capabilities remain implemented directly in actions.py. This table is physical
# data: shape, mass, body location, protection, and terrain traits.
ITEM_SPECS: dict[str, ItemSpec] = {
    "billhook": ItemSpec("Billhook", "BH", 1, 4, 5, "weapon", "Hooks actors, ropes, and machinery."),
    "spear": ItemSpec("Ash spear", "SP", 1, 5, 4, "weapon", "Braces and attacks at reach."),
    "cudgel": ItemSpec("Leadwood cudgel", "CU", 1, 3, 4, "weapon", "Dazes and breaks a guarded stance."),
    "staff": ItemSpec("River staff", "ST", 1, 5, 3, "weapon", "Sweeps space and supports mobile guard."),
    "hand axe": ItemSpec("Hand axe", "AX", 2, 3, 5, "weapon", "Cleaves guards and selected timber."),
    "crossbow": ItemSpec("Windlass crossbow", "XB", 3, 2, 7, "weapon", "A prepared ranged shot with reload commitment."),
    "longbow": ItemSpec("Yew longbow", "LB", 1, 6, 4, "weapon", "Long prepared fire; weather and movement matter."),
    "sling": ItemSpec("Shepherd's sling", "SL", 2, 1, 1, "weapon", "Quick high-arcing cast with loose shot."),
    "heavy crossbow": ItemSpec("Trestle arbalest", "AR", 4, 2, 11, "weapon", "A severe long lane with setup and slow reload."),
    "pike": ItemSpec("Boarding pike", "PK", 1, 6, 6, "weapon", "Brace and control at exceptional reach."),
    "paired knives": ItemSpec("Paired short knives", "KN", 2, 2, 2, "weapon", "Mobile paired cuts at intimate range."),
    "javelins": ItemSpec("Bundle of javelins", "JV", 2, 4, 7, "weapon", "Finite throws with a remaining close thrust."),
    "war hammer": ItemSpec("Quarry war hammer", "WH", 2, 3, 8, "weapon", "Break protection and drive a target backward."),
    "weighted net": ItemSpec("Weighted net and knife", "NT", 3, 3, 6, "weapon", "Restrain at range before closing with a knife."),
    "buckler": ItemSpec("Buckler", "BU", 2, 2, 4, "gear", "Turns one readable close attack."),
    "rope": ItemSpec("Tarred rope", "RO", 1, 4, 4, "gear", "Climbing, recovery, restraint, and route control."),
    "quiet shoes": ItemSpec("Reed-soled shoes", "QS", 2, 1, 1, "gear", "A quiet secondary pair for deliberate crossings."),
    "repair tools": ItemSpec("Repair tools", "RT", 2, 2, 5, "gear", "Alter machinery, structures, and objectives."),
    "smoke pot": ItemSpec("Smoke pot", "SM", 2, 2, 3, "gear", "Finite sight denial and distraction."),
    "cargo harness": ItemSpec("Cargo harness", "CH", 2, 3, 6, "gear", "Carries weight securely but occupies pack room."),
    "trade seals": ItemSpec("Witnessed trade seals", "TS", 2, 1, 1, "gear", "Material proof for negotiation."),
    "hooded lantern": ItemSpec("Hooded lantern", "HL", 2, 2, 3, "gear", "Finite controlled light and animal deterrence."),

    # Three alternatives at each of the six intentionally coarse body locations.
    "felt hood": _armour("Felt hood", "FH", "head", (2, 1), 1, (1, 0, 1), 2, "Warm, quiet, and poor against points.", tags=("warm", "smoke-filter")),
    "boiled cap": _armour("Boiled-leather cap", "BC", "head", (2, 2), 3, (2, 1, 2), 2, "Weatherproof working head protection.", tags=("weatherproof",)),
    "kettle helm": _armour("Kettle helm", "KH", "head", (3, 2), 6, (3, 3, 3), 3, "Broad coverage, loud and sight-restricting.", noise=1, mobility=1, tags=("face-cover",)),
    "quilted jack": _armour("Quilted jack", "QJ", "torso", (3, 3), 5, (1, 1, 3), 2, "Flexible blunt protection that absorbs rain.", tags=("warm", "absorbent")),
    "reedscale vest": _armour("Lacquered reedscale vest", "RV", "torso", (3, 3), 6, (2, 2, 1), 2, "Light cut protection that sheds water.", tags=("weatherproof", "buoyant")),
    "riveted coat": _armour("Riveted coat", "RC", "torso", (4, 3), 11, (4, 3, 3), 3, "Strong protection; heavy when soaked.", noise=2, mobility=2, tags=("metal", "water-heavy")),
    "linen sleeves": _armour("Bound linen sleeves", "LS", "arms", (2, 2), 1, (1, 0, 1), 1, "Quiet cover against thorns.", tags=("thornproof",)),
    "leather vambraces": _armour("Leather vambraces", "LV", "arms", (2, 2), 3, (2, 1, 2), 2, "Tool-friendly arm protection.", tags=("tool-grip",)),
    "splinted arms": _armour("Splinted armguards", "SA", "arms", (3, 2), 6, (3, 3, 3), 3, "Strong guard support with reduced reach.", noise=1, mobility=1, tags=("brace",)),
    "work gloves": _armour("Work gloves", "WG", "hands", (2, 1), 1, (1, 0, 1), 2, "Grip rope and rough mechanisms.", tags=("grip", "thornproof")),
    "mail mitts": _armour("Mail mitts", "MM", "hands", (2, 2), 4, (3, 2, 2), 3, "Excellent cut cover; slow to reload.", noise=1, mobility=1, tags=("metal",)),
    "tarred gauntlets": _armour("Tarred hide gauntlets", "TG", "hands", (2, 2), 3, (2, 1, 2), 2, "Weatherproof grip around salt and wet rope.", tags=("grip", "weatherproof", "saltproof")),
    "wool chausses": _armour("Wool chausses", "WC", "legs", (2, 3), 2, (1, 0, 1), 2, "Warm, quiet, and liable to hold water.", tags=("warm", "absorbent")),
    "leather leggings": _armour("Leather leggings", "LL", "legs", (2, 3), 4, (2, 1, 2), 2, "Thorn and weather protection.", tags=("thornproof", "weatherproof")),
    "brigandine cuisses": _armour("Brigandine cuisses", "BQ", "legs", (3, 3), 8, (3, 3, 3), 3, "Heavy leg cover that worsens steep footing.", noise=1, mobility=2, tags=("metal",)),
    "reed shoes": _armour("Reed shoes", "RS", "feet", (2, 1), 1, (0, 0, 1), 1, "Quiet on dry paths, poor on sharp stone.", tags=("quiet",)),
    "hobnailed boots": _armour("Hobnailed boots", "HB", "feet", (2, 2), 4, (2, 2, 2), 3, "Grip scree and resist sharp ground; noisy on boards.", noise=1, tags=("scree-grip", "sharp-proof")),
    "marsh waders": _armour("Oiled marsh waders", "MW", "feet", (3, 2), 5, (1, 1, 2), 3, "Stay dry in mud and shallows but slip on rock.", mobility=1, tags=("mudproof", "weatherproof", "deep-water")),
}


def item_spec(kind: str) -> ItemSpec:
    if kind in ITEM_SPECS:
        return ITEM_SPECS[kind]
    if kind.startswith("commodity:"):
        name = kind.split(":", 1)[1]
        from .content import COMMODITIES

        bulk = COMMODITIES[name]["bulk"]
        return ItemSpec(name.title(), name[:2].upper(), min(4, bulk), 1, bulk * 2, "cargo", f"Physical {name} cargo.", stack_limit=4)
    if kind.startswith("passive:"):
        name = kind.split(":", 1)[1]
        from .content import PASSIVES

        bulk, description = PASSIVES[name]
        return ItemSpec(name.title(), name[:2].upper(), max(1, bulk), 1, bulk, "passive", description, stack_limit=3)
    if kind.startswith("consumable:"):
        name = kind.split(":", 1)[1]
        return ItemSpec(name.title(), name[:2].upper(), 1, 1, 1, "consumable", "A finite expedition supply.", stack_limit=4)
    if kind.startswith("relic:"):
        name = kind.split(":", 1)[1]
        return ItemSpec(name.title(), "RL", 2, 2, 2, "relic", "A finite, materially strange relic.")
    raise KeyError(f"unknown item kind {kind!r}")


def oriented_size(item: Item) -> tuple[int, int]:
    spec = item_spec(item.kind)
    return (spec.height, spec.width) if item.rotated else (spec.width, spec.height)


def occupied_cells(item: Item) -> set[tuple[int, int]]:
    width, height = oriented_size(item)
    return {(item.x + dx, item.y + dy) for dy in range(height) for dx in range(width)}


def grid_size(state: GameState, location: str) -> tuple[int, int]:
    if location == "pack":
        return state.pack_width, state.pack_height
    if location == "locker":
        return state.locker_width, state.locker_height
    raise ValueError(f"{location} is not a grid")


def grid_items(
    state: GameState,
    location: str,
    *,
    owner_id: str | None = None,
    exclude: str | None = None,
) -> list[Item]:
    return [
        item for item in state.items
        if item.location == location
        and (location != "pack" or item.owner_id == owner_id)
        and item.id != exclude
    ]


def can_place(
    state: GameState,
    item: Item,
    location: str,
    x: int,
    y: int,
    *,
    rotated: bool | None = None,
    owner_id: str | None = None,
) -> bool:
    candidate = replace(
        item,
        location=location,
        x=x,
        y=y,
        rotated=item.rotated if rotated is None else rotated,
        owner_id=owner_id if location == "pack" else None,
    )
    width, height = grid_size(state, location)
    cells = occupied_cells(candidate)
    if not cells or any(cx < 0 or cy < 0 or cx >= width or cy >= height for cx, cy in cells):
        return False
    occupied: set[tuple[int, int]] = set()
    for other in grid_items(state, location, owner_id=owner_id, exclude=item.id):
        occupied.update(occupied_cells(other))
    return cells.isdisjoint(occupied)


def first_fit(
    state: GameState,
    item: Item,
    location: str,
    *,
    owner_id: str | None = None,
) -> tuple[int, int, bool] | None:
    width, height = grid_size(state, location)
    orientations = (item.rotated,) if item_spec(item.kind).width == item_spec(item.kind).height else (item.rotated, not item.rotated)
    for rotated in orientations:
        for y in range(height):
            for x in range(width):
                if can_place(state, item, location, x, y, rotated=rotated, owner_id=owner_id):
                    return x, y, rotated
    return None


def placement_preview(
    state: GameState,
    item: Item,
    location: str,
    x: int,
    y: int,
    *,
    rotated: bool | None = None,
    owner_id: str | None = None,
) -> PlacementPreview:
    candidate = replace(
        item, location=location, x=x, y=y,
        rotated=item.rotated if rotated is None else rotated,
        owner_id=owner_id if location == "pack" else None,
    )
    width, height = grid_size(state, location)
    cells = occupied_cells(candidate)
    outside = any(cx < 0 or cy < 0 or cx >= width or cy >= height for cx, cy in cells)
    blockers = tuple(sorted(
        other.id for other in grid_items(state, location, owner_id=owner_id, exclude=item.id)
        if cells & occupied_cells(other)
    ))
    valid = bool(cells) and not outside and not blockers
    reason = "valid placement" if valid else "out of bounds" if outside else "blocked by " + ", ".join(blockers)
    current_owner_weight = pack_weight(state, owner_id) if owner_id else 0
    already_carried = item.owner_id == owner_id and item.location in {"pack", *EQUIPPED_LOCATIONS}
    resulting = current_owner_weight + (0 if already_carried else item_spec(item.kind).weight * item.quantity)
    capacity = weight_capacity(state) if owner_id == state.active_courier_id else 28
    resulting_load = load_band(resulting, capacity)
    return PlacementPreview(frozenset(cells), valid, reason, blockers, resulting, resulting_load)


def _largest_free_area(width: int, height: int, occupied: set[tuple[int, int]]) -> int:
    remaining = {(x, y) for y in range(height) for x in range(width)} - occupied
    largest = 0
    while remaining:
        seed = min(remaining, key=lambda point: (point[1], point[0]))
        stack, component = [seed], {seed}
        remaining.remove(seed)
        while stack:
            x, y = stack.pop()
            for point in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if point in remaining:
                    remaining.remove(point)
                    component.add(point)
                    stack.append(point)
        largest = max(largest, len(component))
    return largest


def best_fit(
    state: GameState,
    item: Item,
    location: str,
    *,
    owner_id: str | None = None,
) -> tuple[int, int, bool] | None:
    """Preserve useful contiguous space with deterministic category grouping."""
    width, height = grid_size(state, location)
    peers = grid_items(state, location, owner_id=owner_id, exclude=item.id)
    base_occupied = set().union(*(occupied_cells(other) for other in peers)) if peers else set()
    category = item_spec(item.kind).category
    orientations = (item.rotated,) if item_spec(item.kind).width == item_spec(item.kind).height else (item.rotated, not item.rotated)
    candidates: list[tuple[tuple[int, ...], tuple[int, int, bool]]] = []
    for rotated in orientations:
        for y in range(height):
            for x in range(width):
                preview = placement_preview(state, item, location, x, y, rotated=rotated, owner_id=owner_id)
                if not preview.valid:
                    continue
                occupied = base_occupied | set(preview.cells)
                adjacent = 0
                for other in peers:
                    if item_spec(other.kind).category != category:
                        continue
                    other_cells = occupied_cells(other)
                    adjacent += sum(
                        1 for cx, cy in preview.cells
                        if any((cx + dx, cy + dy) in other_cells for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)))
                    )
                unmoved = int(not (item.location == location and item.x == x and item.y == y and item.rotated == rotated))
                score = (-_largest_free_area(width, height, occupied), -adjacent, unmoved, y, x, int(rotated))
                candidates.append((score, (x, y, rotated)))
    return min(candidates)[1] if candidates else None


def place_item(
    state: GameState,
    item_id: str,
    location: str,
    x: int,
    y: int,
    *,
    rotated: bool | None = None,
    owner_id: str | None = None,
) -> bool:
    item = next(item for item in state.items if item.id == item_id)
    if not can_place(state, item, location, x, y, rotated=rotated, owner_id=owner_id):
        return False
    item.location, item.x, item.y = location, x, y
    item.rotated = item.rotated if rotated is None else rotated
    item.owner_id = owner_id if location == "pack" else None
    item.container_id = None
    item.region_id = None
    item.ground_position = None
    return True


def auto_place(
    state: GameState,
    item_id: str,
    location: str,
    *,
    owner_id: str | None = None,
) -> bool:
    item = next(item for item in state.items if item.id == item_id)
    fit = best_fit(state, item, location, owner_id=owner_id)
    return bool(fit and place_item(state, item_id, location, fit[0], fit[1], rotated=fit[2], owner_id=owner_id))


def combine_stacks(
    state: GameState,
    location: str,
    *,
    owner_id: str | None = None,
    movable_ids: set[str] | None = None,
) -> None:
    items = sorted(grid_items(state, location, owner_id=owner_id), key=lambda item: item.id)
    for destination in items:
        if destination.pinned or destination.location != location:
            continue
        limit = item_spec(destination.kind).stack_limit
        if limit <= 1 or destination.quantity >= limit:
            continue
        for source in items:
            if source.id == destination.id or source.location != location or source.kind != destination.kind or source.pinned:
                continue
            if movable_ids is not None and (source.id not in movable_ids or destination.id not in movable_ids):
                continue
            moved = min(limit - destination.quantity, source.quantity)
            if moved <= 0:
                continue
            destination.quantity += moved
            source.quantity -= moved
            if source.quantity == 0:
                source.location = "destroyed"
                source.owner_id = None
                source.merged_into = destination.id
            if destination.quantity == limit:
                break


def auto_pack(
    state: GameState,
    location: str,
    *,
    owner_id: str | None = None,
    selected_ids: set[str] | None = None,
) -> bool:
    """Commit a complete deterministic layout or restore the exact original."""
    snapshot = copy.deepcopy(state.items)
    try:
        selected = set(selected_ids) if selected_ids is not None else None
        combine_stacks(state, location, owner_id=owner_id, movable_ids=selected)
        movable = [
            item for item in grid_items(state, location, owner_id=owner_id)
            if not item.pinned and (selected is None or item.id in selected)
        ]
        movable.sort(key=lambda item: (
            -item_spec(item.kind).width * item_spec(item.kind).height,
            -max(item_spec(item.kind).width, item_spec(item.kind).height),
            item_spec(item.kind).category,
            item.id,
        ))
        for item in movable:
            item.location = "lost"
        for item in movable:
            fit = best_fit(state, item, location, owner_id=owner_id)
            if not fit or not place_item(state, item.id, location, fit[0], fit[1], rotated=fit[2], owner_id=owner_id):
                raise ValueError("no complete arrangement")
        return True
    except (KeyError, ValueError):
        state.items = snapshot
        return False


def pin_item(state: GameState, item_id: str, pinned: bool | None = None) -> bool:
    item = next((item for item in state.items if item.id == item_id), None)
    if item is None or item.location not in {"pack", "locker"}:
        return False
    item.pinned = not item.pinned if pinned is None else pinned
    return True


def item_preview(kind: str) -> tuple[str, str, str]:
    """Compact item art supplied by physical category and authored weapon form."""
    weapon_art = {
        "billhook": ("   _/", "--/  ", " /   "),
        "spear": ("  /\\ ", " /  ", "/   "),
        "cudgel": (" [#] ", "  |  ", "  |  "),
        "staff": ("  /  ", " /   ", "/    "),
        "hand axe": (" /== ", "  |  ", "  |  "),
        "crossbow": ("\\=|=/", "  |  ", " / \\ "),
        "longbow": (")--- ", ")    ", ")--- "),
        "sling": (" o   ", "  \\  ", "   \\ "),
        "heavy crossbow": ("\\===|===/", "    |    ", "   / \\   "),
        "pike": ("----->", "      ", "      "),
        "paired knives": (" /\\  ", " ||  ", " \\/  "),
        "javelins": ("///> ", "///> ", "///> "),
        "war hammer": ("[===]", "  |  ", "  |  "),
        "weighted net": ("#-#-#", "-#-#-", " # \\ "),
    }
    if kind in weapon_art:
        return weapon_art[kind]
    spec = item_spec(kind)
    if spec.category == "armour":
        return {
            "head": (" /---\\ ", "|  o  |", " \\___/ "),
            "torso": (" /| |\\ ", "| === |", " \\___/ "),
            "arms": ("==| |==", "  | |  ", "       "),
            "hands": ("[ ] [ ]", " |   | ", "       "),
            "legs": (" |   | ", " |   | ", "/     \\"),
            "feet": ("       ", "       ", "[_] [_]"),
        }[spec.slot or "torso"]
    if spec.category == "relic":
        return (" .-*-.", "(  ?  )", " `---'")
    if spec.category == "gear":
        return ("+-----+", f"| {spec.abbreviation:^3} |", "+-----+")
    if spec.category == "cargo":
        return ("+====+", f"| {spec.abbreviation:^2} |", "+====+")
    if spec.category == "passive":
        return (" .---.", f"( {spec.abbreviation:^2} )", " '---'")
    return ("  __  ", f" /{spec.abbreviation:^2}\\ ", " \\__/ ")


def rotate_item(state: GameState, item_id: str) -> bool:
    item = next(item for item in state.items if item.id == item_id)
    if item.location not in {"pack", "locker"}:
        return False
    owner = item.owner_id if item.location == "pack" else None
    if not can_place(state, item, item.location, item.x, item.y, rotated=not item.rotated, owner_id=owner):
        return False
    item.rotated = not item.rotated
    return True


def create_item(
    state: GameState,
    kind: str,
    provenance: str,
    *,
    location: str = "lost",
    owner_id: str | None = None,
    quantity: int = 1,
    condition: int = 100,
) -> Item:
    item_spec(kind)
    item = Item(
        id=f"item-{state.next_item_id:05d}", kind=kind, location=location,
        provenance=provenance, owner_id=owner_id, quantity=quantity,
        condition=condition,
    )
    state.next_item_id += 1
    state.items.append(item)
    return item


def basic_courier_kit(person: Person) -> dict[str, str]:
    """Return the small authored working issue for one adult's actual role."""
    weapon, secondary = BASIC_COURIER_LOADOUTS.get(person.role, ("staff", "rope"))
    kit = {
        "readied": weapon,
        "secondary": secondary,
        "head": "felt hood",
        "torso": "quilted jack",
        "feet": "reed shoes",
    }
    kit.update(BASIC_COURIER_ARMOUR.get(person.role, {}))
    return kit


def ensure_courier_basics(state: GameState, person: Person) -> list[str]:
    """Issue missing basics once; later loss or deliberate removal still matters."""
    marker = f"basic_kit:{person.id}"
    if state.vessel_changes.get(marker):
        return []
    issued: list[str] = []
    for location, kind in basic_courier_kit(person).items():
        if equipped_item(state, location, person.id) is not None:
            continue
        create_item(
            state, kind, f"Jomon working issue for {person.role}",
            location=location, owner_id=person.id,
        )
        issued.append(kind)
        spec = item_spec(kind)
        if spec.category == "weapon" and kind not in state.owned_weapons:
            state.owned_weapons.append(kind)
        elif spec.category == "gear" and kind not in state.owned_gear:
            state.owned_gear.append(kind)
    state.vessel_changes[marker] = True
    if person.id == state.active_courier_id:
        sync_legacy_load(state)
    return issued


def ensure_household_basics(state: GameState) -> None:
    for person in state.household:
        if person.alive:
            ensure_courier_basics(state, person)


def transfer_to_grid(
    state: GameState,
    item_id: str,
    location: str,
    *,
    owner_id: str | None = None,
) -> bool:
    item = next(item for item in state.items if item.id == item_id)
    previous = replace(item)
    fit = first_fit(state, item, location, owner_id=owner_id)
    if not fit:
        return False
    if place_item(state, item.id, location, fit[0], fit[1], rotated=fit[2], owner_id=owner_id):
        return True
    item.__dict__.update(previous.__dict__)
    return False


def equipped_item(state: GameState, location: str, owner_id: str | None = None) -> Item | None:
    owner_id = owner_id if owner_id is not None else state.active_courier_id
    return next((item for item in state.items if item.owner_id == owner_id and item.location == location), None)


def equip_item(state: GameState, item_id: str) -> bool:
    item = next(item for item in state.items if item.id == item_id)
    if item.location != "pack" or item.owner_id != state.active_courier_id:
        return False
    spec = item_spec(item.kind)
    destination = "readied" if spec.category == "weapon" else spec.slot if spec.category == "armour" else "secondary" if spec.category == "gear" else None
    if destination is None:
        return False
    previous = equipped_item(state, destination)
    item.location = "held"
    if previous and not transfer_to_grid(state, previous.id, "pack", owner_id=state.active_courier_id):
        item.location = "pack"
        return False
    item.location, item.owner_id, item.x, item.y = destination, state.active_courier_id, 0, 0
    if destination == "readied":
        state.weapon = item.kind
        state.crossbow_loaded = True
        state.aimed_target = None
    elif destination == "secondary":
        state.gear = item.kind
    return True


def prepare_kind(state: GameState, kind: str) -> bool:
    """Compatibility preparation path backed by the physical locker and pack."""
    owner = state.active_courier_id
    if owner is None:
        return False
    spec = item_spec(kind)
    destination = "readied" if spec.category == "weapon" else "secondary"
    current = equipped_item(state, destination, owner)
    if current and current.kind == kind:
        return True
    candidate = next(
        (
            item for item in state.items
            if item.kind == kind
            and (
                (item.location == "pack" and item.owner_id == owner)
                or item.location == "locker"
            )
        ),
        None,
    )
    if candidate is None:
        return False
    if candidate.location == "locker" and not transfer_to_grid(state, candidate.id, "pack", owner_id=owner):
        return False
    return equip_item(state, candidate.id)


def unequip_item(state: GameState, location: str) -> bool:
    item = equipped_item(state, location)
    if item is None or not transfer_to_grid(state, item.id, "pack", owner_id=state.active_courier_id):
        return False
    if location == "readied":
        state.weapon = None
    elif location == "secondary":
        state.gear = None
    return True


def drop_item(state: GameState, item_id: str) -> bool:
    item = next(item for item in state.items if item.id == item_id)
    if state.location != "region" or item.owner_id != state.active_courier_id:
        return False
    item.location = "ground"
    item.owner_id = None
    item.region_id = state.active_region_id
    item.ground_position = state.position
    item.container_id = None
    if item.kind == state.weapon:
        state.weapon = None
    if item.kind == state.gear:
        state.gear = None
    return True


def lose_matching_carried(state: GameState, kinds: Iterable[str] | None = None) -> list[str]:
    owner = state.active_courier_id
    allowed = set(kinds) if kinds is not None else None
    lost: list[str] = []
    for item in state.items:
        if item.owner_id != owner or item.location != "pack":
            continue
        if allowed is not None and item.kind not in allowed:
            continue
        item.location, item.owner_id = "lost", None
        lost.append(item_spec(item.kind).name)
    return lost


def consume_carried(state: GameState, kind: str, quantity: int = 1) -> bool:
    owner = state.active_courier_id
    remaining = quantity
    candidates = [
        item for item in state.items
        if item.owner_id == owner and item.location in {"pack", "readied", "secondary"} and item.kind == kind
    ]
    if sum(item.quantity for item in candidates) < quantity:
        return False
    for item in candidates:
        taken = min(remaining, item.quantity)
        item.quantity -= taken
        remaining -= taken
        if item.quantity == 0:
            item.location, item.owner_id = "destroyed", None
        if remaining == 0:
            break
    sync_legacy_load(state)
    return True


def sync_legacy_load(state: GameState) -> None:
    """Keep the small existing action vocabulary aligned with physical items."""
    owner = state.active_courier_id
    readied = equipped_item(state, "readied", owner)
    secondary = equipped_item(state, "secondary", owner)
    state.weapon = readied.kind if readied else None
    state.gear = secondary.kind if secondary else None
    passives: dict[str, int] = {}
    consumables: dict[str, int] = {}
    goods: dict[str, object] = {}
    relic: str | None = None
    from .state import CommodityStack
    from .content import COMMODITIES

    for item in state.items:
        if item.owner_id != owner or item.location != "pack":
            continue
        if item.kind.startswith("passive:"):
            name = item.kind.split(":", 1)[1]
            passives[name] = passives.get(name, 0) + item.quantity
        elif item.kind.startswith("consumable:"):
            name = item.kind.split(":", 1)[1]
            consumables[name] = consumables.get(name, 0) + item.quantity
        elif item.kind.startswith("commodity:"):
            name = item.kind.split(":", 1)[1]
            goods[name] = CommodityStack(item.quantity, COMMODITIES[name]["condition"])
        elif item.kind.startswith("relic:") and relic is None:
            relic = item.kind.split(":", 1)[1]
    state.carried_passives = passives
    state.consumables = consumables
    state.carried_goods = goods  # type: ignore[assignment]
    state.carried_relic = relic


def record_acquisition(state: GameState, item: Item) -> None:
    if item.kind.startswith("passive:"):
        name = item.kind.split(":", 1)[1]
        state.owned_passives[name] = state.owned_passives.get(name, 0) + item.quantity
    elif item.kind.startswith("relic:"):
        name = item.kind.split(":", 1)[1]
        state.relics[name] = state.relics.get(name, 0) + item.quantity
    elif item_spec(item.kind).category == "weapon" and item.kind not in state.owned_weapons:
        state.owned_weapons.append(item.kind)
    elif item_spec(item.kind).category == "gear" and item.kind not in state.owned_gear:
        state.owned_gear.append(item.kind)
    elif item.kind.startswith("consumable:"):
        name = item.kind.split(":", 1)[1]
        ammunition = {
            "fletched arrows": ("arrows", 4),
            "sling shot pouch": ("sling stones", 6),
            "quarrel case": ("heavy bolts", 2),
            "casting net bundle": ("nets", 1),
        }.get(name)
        if ammunition:
            kind, amount = ammunition
            state.ammunition_by_type[kind] = state.ammunition_by_type.get(kind, 0) + amount
    sync_legacy_load(state)


@dataclass
class InventoryTransaction:
    """One reversible inventory operation, independent of cursor movement."""

    items: list[Item]
    next_item_id: int
    weapon: str | None
    gear: str | None
    owned_passives: dict[str, int]
    relics: dict[str, int]
    carried_passives: dict[str, int]
    consumables: dict[str, int]
    carried_goods: dict[str, object]
    carried_relic: str | None
    auto_place_enabled: bool
    changed: bool = False

    @classmethod
    def begin(cls, state: GameState) -> "InventoryTransaction":
        return cls(
            copy.deepcopy(state.items), state.next_item_id, state.weapon, state.gear,
            copy.deepcopy(state.owned_passives), copy.deepcopy(state.relics),
            copy.deepcopy(state.carried_passives), copy.deepcopy(state.consumables),
            copy.deepcopy(state.carried_goods), state.carried_relic,
            state.auto_place_enabled,
        )

    def cancel(self, state: GameState) -> None:
        state.items = copy.deepcopy(self.items)
        state.next_item_id = self.next_item_id
        state.weapon, state.gear = self.weapon, self.gear
        state.owned_passives = copy.deepcopy(self.owned_passives)
        state.relics = copy.deepcopy(self.relics)
        state.carried_passives = copy.deepcopy(self.carried_passives)
        state.consumables = copy.deepcopy(self.consumables)
        state.carried_goods = copy.deepcopy(self.carried_goods)  # type: ignore[assignment]
        state.carried_relic = self.carried_relic
        state.auto_place_enabled = self.auto_place_enabled


def pack_weight(state: GameState, owner_id: str | None = None) -> int:
    owner_id = owner_id if owner_id is not None else state.active_courier_id
    weight = sum(
        item_spec(item.kind).weight * item.quantity
        for item in state.items
        if item.owner_id == owner_id and item.location in {"pack", *EQUIPPED_LOCATIONS}
    )
    if "wet" in state.terrain_statuses:
        weight += sum(
            4 for item in state.items
            if item.owner_id == owner_id
            and item.location in EQUIPPED_LOCATIONS
            and "water-heavy" in item_spec(item.kind).tags
        )
    return weight


def weight_capacity(state: GameState) -> int:
    courier = state.courier
    capacity = 34 if courier and courier.role in {"guard", "carpenter", "bargemaster"} else 28
    if state.support == "porter watch":
        capacity += 8
    if state.gear == "cargo harness":
        capacity += 10
    return capacity


def load_state(state: GameState) -> str:
    return load_band(pack_weight(state), max(1, weight_capacity(state)))


def load_band(weight: int, limit: int) -> str:
    if weight * 2 <= limit:
        return "light"
    if weight * 4 <= limit * 3:
        return "laden"
    if weight <= limit:
        return "encumbered"
    return "overloaded"


LOAD_EFFECTS = {
    "light": "quiet movement; normal climb and retreat",
    "laden": "some surfaces add noise",
    "encumbered": "movement may take two actions; weak floors and retreat are risky",
    "overloaded": "cannot climb; deep water and retreat are dangerous",
}


def armour_at(state: GameState, location: str) -> Item | None:
    return equipped_item(state, location)


def protection_at(state: GameState, location: str, damage_kind: str) -> tuple[int, str]:
    item = armour_at(state, location)
    if not item:
        return 0, "uncovered"
    spec = item_spec(item.kind)
    protection = {"cut": spec.cut, "pierce": spec.pierce, "blunt": spec.blunt}.get(damage_kind, 0)
    if item.condition <= 25:
        protection = max(0, protection - 1)
    return protection, spec.name


def degrade_armour(state: GameState, location: str, amount: int = 8) -> None:
    item = armour_at(state, location)
    if item:
        item.condition = max(0, item.condition - amount)


def add_status(state: GameState, name: str, cause: str, turns: int, consequence: str) -> bool:
    existing = state.terrain_statuses.get(name)
    changed = existing is None or existing.remaining < turns
    state.terrain_statuses[name] = TerrainStatus(cause, max(turns, existing.remaining if existing else 0), consequence)
    return changed


def tick_statuses(state: GameState) -> list[str]:
    ended: list[str] = []
    for name in list(state.terrain_statuses):
        status = state.terrain_statuses[name]
        status.remaining -= 1
        if status.remaining <= 0:
            del state.terrain_statuses[name]
            ended.append(f"{name.replace('-', ' ').title()} clears.")
    return ended


def worn_tags(state: GameState) -> set[str]:
    tags: set[str] = set()
    for location in BODY_SLOTS:
        item = armour_at(state, location)
        if item:
            tags.update(item_spec(item.kind).tags)
    return tags


def terrain_status_for(state: GameState, tile: str) -> tuple[str, str, int, str] | None:
    tags = worn_tags(state)
    burden = load_state(state)
    if tile == "m" and "mudproof" not in tags:
        turns = 1 if "reed-tonic" in state.drink_effects else 3
        return "bogged", "deep mud", turns, "movement is slower; evasion and retreat worsen"
    if tile in {",", "~"} and "weatherproof" not in tags:
        return "wet", "floodwater", 6, "heavy armour weighs more and cold exposure grows"
    if tile == "r" and "scree-grip" not in tags:
        return "poor-footing", "unstable scree", 3, "guard, climbing, and falling are less safe"
    if tile == "q" and "sharp-proof" not in tags:
        return "cut-feet", "sharp limestone", 4, "foot injury risk and movement noise increase"
    if tile == "t" and not {"thornproof"} <= tags:
        return "thorn-scratched", "dense thorn growth", 4, "exposed limbs hinder guard and quiet passage"
    if tile == "s" and "smoke-filter" not in tags and "face-cover" not in tags and "smokeleaf-infusion" not in state.drink_effects:
        return "smoke-inhalation", "rising smoke", 4, "sight and endurance are reduced"
    if tile == ":" and "saltproof" not in tags:
        return "salt-grit", "windblown salt", 4, "aim and exposed hands are impaired"
    if tile == "w" and "deep-water" not in tags:
        consequence = "overloaded couriers risk being swept away" if burden == "overloaded" else "movement and guard are slowed"
        return "current", "deep current", 2, consequence
    return None


def apply_terrain_status(state: GameState, tile: str) -> str:
    result = terrain_status_for(state, tile)
    if not result:
        return ""
    name, cause, turns, consequence = result
    messages = []
    if add_status(state, name, cause, turns, consequence):
        messages.append(f"{name.replace('-', ' ').title()} from {cause}: {consequence}.")
    if tile in {",", "~", "w"}:
        from .calendar import calendar_at

        if (
            calendar_at(state).season == "winter"
            and "warm" not in worn_tags(state)
            and "winter-juniper" not in state.drink_effects
            and add_status(state, "chilled", "winter water", 8, "aim, treatment, and recovery are slower")
        ):
            messages.append("Winter water chills exposed clothing; aim and recovery slow.")
    return " ".join(messages)


def validate_inventory(state: GameState) -> None:
    ids = [item.id for item in state.items]
    if len(ids) != len(set(ids)):
        raise ValueError("item identities must be unique")
    people = {person.id for person in state.household}
    valid_locations = {"pack", "locker", "readied", "secondary", *BODY_SLOTS, "container", "ground", "enemy", "lost", "destroyed"}
    for item in state.items:
        item_spec(item.kind)
        if item.location not in valid_locations:
            raise ValueError(f"invalid item location {item.location}")
        if item.location in {"pack", "readied", "secondary", *BODY_SLOTS} and item.owner_id not in people:
            raise ValueError("carried item has no valid owner")
        if item.location == "container" and not item.container_id:
            raise ValueError("container item has no container")
        if item.location == "ground" and (not item.region_id or item.ground_position is None):
            raise ValueError("ground item has no regional position")
    for location, owner in (("locker", None), *(('pack', person.id) for person in state.household)):
        seen: set[tuple[int, int]] = set()
        width, height = grid_size(state, location)
        for item in grid_items(state, location, owner_id=owner):
            cells = occupied_cells(item)
            if any(x < 0 or y < 0 or x >= width or y >= height for x, y in cells) or not cells.isdisjoint(seen):
                raise ValueError(f"invalid {location} placement")
            seen.update(cells)


def initialise_inventory(state: GameState) -> None:
    """Create a deterministic, physically placed household starting store."""
    starting = [
        *state.owned_weapons, *state.owned_gear,
        "felt hood", "quilted jack", "leather vambraces", "work gloves",
        "wool chausses", "hobnailed boots",
    ]
    for kind in dict.fromkeys(starting):
        item = create_item(state, kind, "Jomon household stores")
        if not auto_place(state, item.id, "locker"):
            raise RuntimeError("initial Jomon locker is too small")
    for name, quantity in (
        ("crossbow bolts", 6), ("fletched arrows", 8),
        ("sling shot pouch", 10), ("quarrel case", 4),
        ("casting net bundle", 2),
    ):
        item = create_item(
            state, f"consumable:{name}", "Jomon counted ammunition",
            quantity=quantity,
        )
        if not auto_place(state, item.id, "locker"):
            raise RuntimeError("initial Jomon locker cannot hold physical ammunition")


def legacy_kind(name: str, category: str) -> str:
    if category == "passive":
        return f"passive:{name}"
    if category == "consumable":
        return f"consumable:{name}"
    if category == "relic":
        return f"relic:{name}"
    return name


def reconcile_legacy_carried(state: GameState) -> None:
    """Create physical instances for old mirrors only when one does not exist."""
    owner = state.active_courier_id
    if not owner:
        return
    desired: list[tuple[str, int, str]] = []
    if state.weapon:
        desired.append((state.weapon, 1, "readied"))
    if state.gear:
        desired.append((state.gear, 1, "secondary"))
    desired.extend((f"passive:{name}", count, "pack") for name, count in state.carried_passives.items())
    desired.extend((f"consumable:{name}", count, "pack") for name, count in state.consumables.items())
    desired.extend((f"commodity:{name}", stack.quantity, "pack") for name, stack in state.carried_goods.items())
    if state.carried_relic:
        desired.append((f"relic:{state.carried_relic}", 1, "pack"))
    for kind, quantity, location in desired:
        existing = [item for item in state.items if item.owner_id == owner and item.kind == kind and item.location in {"pack", "readied", "secondary"}]
        missing = quantity - sum(item.quantity for item in existing)
        if missing <= 0:
            continue
        stored = next(
            (item for item in state.items if item.kind == kind and item.location == "locker"),
            None,
        )
        if stored and quantity == 1:
            if location in {"readied", "secondary"}:
                stored.location, stored.owner_id = location, owner
                continue
            if transfer_to_grid(state, stored.id, "pack", owner_id=owner):
                continue
        item = create_item(state, kind, "migrated format-3 possession", owner_id=owner, quantity=missing)
        if location in {"readied", "secondary"} and not equipped_item(state, location, owner):
            item.location = location
        elif not auto_place(state, item.id, "pack", owner_id=owner):
            item.owner_id = None
            if not auto_place(state, item.id, "locker"):
                raise RuntimeError("migrated possessions exceed bounded Jomon storage")
