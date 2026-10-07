"""Bounded freeform mixtures: physical flasks, sparse cells and disclosed reactions."""

from __future__ import annotations

from .catalog import CatalogError, load_catalog
from .state import GameState, Item, MaterialCell, Position
from .chemistry_presentation import chemistry_format, chemistry_text, reagent_contents_display, reagent_display_name, reaction_list_display

_CATALOG = load_catalog("chemistry.json", ("reagent_ids", "reactions", "environment_reactions"))
_reagents = _CATALOG["reagent_ids"]
if (not isinstance(_reagents, list) or not _reagents
        or any(not isinstance(name, str) or not name for name in _reagents)
        or len(set(_reagents)) != len(_reagents)):
    raise CatalogError("chemistry.json has invalid reagent_ids")
REAGENTS = tuple(_reagents)

_rows = _CATALOG["reactions"]
if not isinstance(_rows, list):
    raise CatalogError("chemistry.json must provide reactions")
REACTION_IDS: tuple[str, ...]
REACTIONS: dict[frozenset[str], tuple[str, str]] = {}
_effects = {"fire", "smoke", "shock", "lime", "ice", "heal", "mana", "glow", "corrode", "seal", "breath"}
for row in _rows:
    if not isinstance(row, dict) or set(row) != {"reagent_ids", "id", "effect"}:
        raise CatalogError("chemistry.json has an invalid reaction")
    pair = row["reagent_ids"]
    if (not isinstance(pair, list) or len(pair) != 2 or any(not isinstance(name, str) or name not in REAGENTS for name in pair)
            or len(set(pair)) != 2 or not isinstance(row["id"], str) or not row["id"]
            or not isinstance(row["effect"], str) or row["effect"] not in _effects):
        raise CatalogError("chemistry.json has an invalid reaction")
    key = frozenset(pair)
    if key in REACTIONS or any(name == row["id"] for name, _ in REACTIONS.values()):
        raise CatalogError("chemistry.json repeats a reaction pair or name")
    REACTIONS[key] = (row["id"], row["effect"])
REACTION_IDS = tuple(reaction_id for reaction_id, _effect in REACTIONS.values())
REACTION_EFFECTS = {reaction_id: effect for reaction_id, effect in REACTIONS.values()}

# Saved pre-presentation values use the bundled default vocabulary, never the
# selected pack.  These explicit tables make legacy normalization conservative.
LEGACY_REAGENT_IDS = {name: name for name in REAGENTS}
LEGACY_REACTION_IDS = {name: name for name in REACTION_IDS}
UNKNOWN_REAGENT_ID = "legacy.unknown.reagent"

ENVIRONMENT_REACTIONS = _CATALOG["environment_reactions"]
if (not isinstance(ENVIRONMENT_REACTIONS, dict) or set(ENVIRONMENT_REACTIONS) != {"water", "fire"}
        or any(not isinstance(name, str) or name not in REAGENTS for name in ENVIRONMENT_REACTIONS.values())):
    raise CatalogError("chemistry.json has invalid environmental reagents")


def carried_flasks(state: GameState) -> list[Item]:
    return [item for item in state.items if item.kind == "field flask" and item.owner_id == state.active_courier_id
            and item.location == "pack" and item.condition > 0]


def carried_ingredients(state: GameState) -> list[Item]:
    return [item for item in state.items if item.kind.startswith("ingredient:") and item.owner_id == state.active_courier_id
            and item.location == "pack" and item.quantity > 0]


def predicted_reactions(reagents: dict[str, int], cell: MaterialCell | None = None) -> list[str]:
    present = {name for name, quantity in reagents.items() if quantity > 0}
    if cell and cell.water:
        present.add("spring water")
    if cell and cell.fire:
        present.add("cinder salt")
    return [name for pair, (name, _) in REACTIONS.items() if pair <= present]


def fill_flask(state: GameState, flask_id: str, ingredient_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import sync_legacy_load

    flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
    ingredient = next((item for item in carried_ingredients(state) if item.id == ingredient_id), None)
    if flask is None or ingredient is None:
        return False, chemistry_text("chemistry.fill.carried")
    reagent = ingredient.kind.split(":", 1)[1]
    if reagent not in REAGENTS or sum(flask.contents.values()) >= 4:
        return False, chemistry_text("chemistry.fill.unavailable")
    before = predicted_reactions(flask.contents)
    measures = min(2 if state.courier and "safe-decant" in state.courier.skill_nodes else 1,
                   4 - sum(flask.contents.values()), 4 - flask.contents.get(reagent, 0))
    flask.contents[reagent] = flask.contents.get(reagent, 0) + measures
    ingredient.quantity -= 1
    if ingredient.quantity == 0:
        ingredient.location, ingredient.owner_id = "destroyed", None
    sync_legacy_load(state)
    after = predicted_reactions(flask.contents)
    from .skill_tree import record_milestone

    if after:
        record_milestone(state, "craft:alchemy")
    _advance_world(state)
    message = chemistry_format(
        "chemistry.fill.result", measures=measures, reagent=reagent_display_name(reagent),
        flask=flask.id, contents=reagent_contents_display(flask.contents),
    )
    if set(after) - set(before):
        message += chemistry_format("chemistry.fill.reaction_potential", reactions=reaction_list_display(sorted(set(after) - set(before))))
    state.add_message(message, priority=3)
    return True, message


def distill_flask(state: GameState, flask_id: str, reagent: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item
    from .production import stations_here
    from .skill_tree import has_node, record_milestone

    flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
    if flask is None or flask.contents.get(reagent, 0) <= 0:
        return False, chemistry_text("chemistry.distill.source")
    if not has_node(state.courier, "controlled-distil") or "still" not in stations_here(state):
        return False, chemistry_text("chemistry.distill.requirement")
    transaction = InventoryTransaction.begin(state)
    flask.contents[reagent] -= 1
    if flask.contents[reagent] == 0:
        del flask.contents[reagent]
    output = create_item(state, f"ingredient:{reagent}", chemistry_format("chemistry.provenance.distilled", flask=flask_id))
    if not auto_place(state, output.id, "pack", owner_id=state.active_courier_id):
        transaction.cancel(state)
        return False, chemistry_text("chemistry.distill.pack")
    record_milestone(state, "craft:alchemy")
    _advance_world(state, steps=2)
    message = chemistry_format("chemistry.distill.result", courier=state.courier.name, reagent=reagent_display_name(reagent), flask=flask_id)
    state.add_message(message, priority=3)
    return True, message


def pour_flask(state: GameState, flask_id: str, point: Position) -> tuple[bool, str]:
    from .actions import _advance_world
    from .materials import ensure_cell
    from .world import courier_sees, distance

    flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
    if flask is None or not flask.contents:
        return False, chemistry_text("chemistry.pour.carried")
    if distance(state.position, point) > 1 or not courier_sees(state, point):
        return False, chemistry_text("chemistry.pour.range")
    cell = ensure_cell(state, point)
    if cell is None:
        return False, chemistry_text("chemistry.pour.ground")
    combined = cell.reagents.copy()
    for name, quantity in flask.contents.items():
        combined[name] = combined.get(name, 0) + quantity
    if len(combined) > 4 or sum(combined.values()) > 8 or any(quantity > 4 for quantity in combined.values()):
        return False, chemistry_text("chemistry.pour.capacity")
    warning = predicted_reactions(combined, cell)
    cell.reagents = combined
    flask.contents.clear()
    _advance_world(state)
    message = chemistry_format("chemistry.pour.result", courier=state.courier.name, flask=flask.id, x=point.x, y=point.y)
    message += chemistry_format("chemistry.pour.prediction", reactions=reaction_list_display(warning) if warning else chemistry_text("chemistry.overlay.none_yet"))
    state.add_message(message, priority=3)
    return True, message


def drink_flask(state: GameState, flask_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import add_status

    flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
    if flask is None or not flask.contents:
        return False, chemistry_text("chemistry.drink.carried")
    reactions = predicted_reactions(flask.contents)
    drinkable_effects = {"heal", "mana", "breath"}
    if not reactions or not any(REACTION_EFFECTS[reaction_id] in drinkable_effects for reaction_id in reactions):
        return False, chemistry_text("chemistry.drink.unknown")
    if any(REACTION_EFFECTS[reaction_id] not in drinkable_effects for reaction_id in reactions):
        return False, chemistry_text("chemistry.drink.dangerous")
    familiar = set(state.courier.known_formulas) | set(state.household_formulas)
    for reaction_id in reactions:
        effect = REACTION_EFFECTS[reaction_id]
        if effect == "heal":
            bonus = int("substance-sense" in state.courier.skill_nodes) + int("field-triage" in state.courier.skill_nodes)
            state.courier.health = min(state.courier.max_health, state.courier.health + 3 + bonus + int(reaction_id in familiar))
        elif effect == "mana":
            state.courier.mana = min(state.courier.max_mana, state.courier.mana + 3 + int("catalyst-brewing" in state.courier.skill_nodes) + int(reaction_id in familiar))
        elif effect == "breath":
            state.terrain_statuses.pop("smoke-inhalation", None)
            if "antitoxin" in state.courier.skill_nodes:
                state.terrain_statuses.pop("salt-grit", None)
                state.terrain_statuses.pop("lime-grit", None)
            add_status(state, "clear-breath", chemistry_text("chemistry.status.clear_breath.cause"), 4 + int(reaction_id in familiar), chemistry_text("chemistry.status.clear_breath.consequence"))
    flask.contents.clear()
    for name in reactions:
        if name not in state.courier.known_formulas:
            state.courier.known_formulas.append(name)
    _advance_world(state)
    message = chemistry_format("chemistry.drink.result", courier=state.courier.name, reactions=reaction_list_display(reactions), flask=flask.id)
    state.add_message(message, priority=3)
    return True, message


def react_cell(state: GameState, point: Position, cell: MaterialCell) -> str | None:
    """Resolve at most one reaction per selected sparse cell and action."""
    from .materials import _expose

    if cell.reaction_due > state.world_time:
        return None
    warned_flash = cell.reaction_due > 0
    cell.reaction_due = 0
    present = {name for name, quantity in cell.reagents.items() if quantity > 0}
    virtual = set()
    if cell.water:
        virtual.add("spring water")
    if cell.fire:
        virtual.add("cinder salt")
    flash_pair = frozenset(("brine", "spark salt"))
    pair = (flash_pair if warned_flash and flash_pair <= present else
            next((candidate for candidate in REACTIONS if candidate <= present | virtual and candidate & present), None))
    if pair is None:
        return None
    name, effect = REACTIONS[pair]
    for reagent in pair & present:
        cell.reagents[reagent] -= 1
        if cell.reagents[reagent] == 0:
            del cell.reagents[reagent]
    if effect == "fire":
        cell.fire, cell.fuel = max(2, cell.fire), max(3, cell.fuel)
    elif effect == "smoke":
        cell.smoke = max(3, cell.smoke)
    elif effect == "shock":
        _expose(state, point, "debris", 2)
        cell.smoke = max(2, cell.smoke)
    elif effect == "lime":
        cell.coating, cell.smoke = "lime", max(2, cell.smoke)
    elif effect == "ice":
        cell.water, cell.ice = max(1, cell.water), True
    elif effect == "heal":
        if state.position == point and state.courier:
            state.courier.health = min(state.courier.max_health, state.courier.health + 1)
    elif effect == "mana":
        if state.position == point and state.courier:
            state.courier.mana = min(state.courier.max_mana, state.courier.mana + 1)
    elif effect == "glow":
        cell.coating = "glow"
    elif effect == "corrode":
        _expose(state, point, "salt", 1)
    elif effect == "seal":
        cell.support = min(3, cell.support + 1)
    elif effect == "breath":
        cell.smoke = max(0, cell.smoke - 2)
    if state.courier and name not in state.courier.known_formulas:
        from .world import courier_sees

        if courier_sees(state, point):
            state.courier.known_formulas.append(name)
    return name



def legacy_reagent_id(value: object) -> str:
    """Map only known bundled-default legacy reagent values to stable IDs."""
    return LEGACY_REAGENT_IDS.get(value, UNKNOWN_REAGENT_ID) if isinstance(value, str) else UNKNOWN_REAGENT_ID


def legacy_reaction_id(value: object) -> str | None:
    """Map only known bundled-default legacy formula names; never guess."""
    return LEGACY_REACTION_IDS.get(value) if isinstance(value, str) else None


def _migrate_reagent_mapping(reagents: dict[str, int]) -> dict[str, int]:
    migrated: dict[str, int] = {}
    for name, quantity in reagents.items():
        stable_id = legacy_reagent_id(name)
        migrated[stable_id] = migrated.get(stable_id, 0) + quantity
    return migrated


def migrate_legacy_chemistry_state(state: GameState) -> None:
    """Normalize old saved chemistry names without consulting selected-pack prose."""
    for item in state.items:
        if item.contents:
            item.contents = _migrate_reagent_mapping(item.contents)
    for cells in [state.vessel_materials, *(region.materials for region in state.regions.values())]:
        for cell in cells.values():
            if cell.reagents:
                cell.reagents = _migrate_reagent_mapping(cell.reagents)
    for person in [*state.household, *state.visitors, state.bartender, state.merchant]:
        known: list[str] = []
        for value in person.known_formulas:
            stable_id = legacy_reaction_id(value)
            if stable_id is not None and stable_id not in known:
                known.append(stable_id)
        person.known_formulas = known
    household: list[str] = []
    for value in state.household_formulas:
        stable_id = legacy_reaction_id(value)
        if stable_id is not None and stable_id not in household:
            household.append(stable_id)
    state.household_formulas = household

def validate_chemistry(state: GameState) -> None:
    for item in state.items:
        if item.contents and item.kind != "field flask":
            raise ValueError("only physical flasks can hold freeform reagents")
        if (not isinstance(item.contents, dict) or len(item.contents) > 4
                or any(name not in {*REAGENTS, UNKNOWN_REAGENT_ID} or type(quantity) is not int or not 1 <= quantity <= 4
                       for name, quantity in item.contents.items())
                or sum(item.contents.values()) > 4):
            raise ValueError("invalid flask mixture")
    for cells in [state.vessel_materials, *(region.materials for region in state.regions.values())]:
        for cell in cells.values():
            if (not isinstance(cell.reagents, dict) or len(cell.reagents) > 4
                    or any(name not in {*REAGENTS, UNKNOWN_REAGENT_ID} or type(quantity) is not int or not 1 <= quantity <= 4
                           for name, quantity in cell.reagents.items())
                    or sum(cell.reagents.values()) > 8):
                raise ValueError("invalid sparse-cell chemistry")
