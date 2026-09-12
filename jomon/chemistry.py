"""Bounded freeform mixtures: physical flasks, sparse cells and disclosed reactions."""

from __future__ import annotations

from .state import GameState, Item, MaterialCell, Position

REAGENTS = (
    "cinder salt", "tree resin", "brine", "lime dust", "frostwort", "smoke leaf",
    "healing herb", "glow spore", "peat oil", "iron filings", "spring water", "spark salt",
    "iron ore", "clay",
)
REACTIONS = {
    frozenset(("cinder salt", "tree resin")): ("flame bloom", "fire"),
    frozenset(("cinder salt", "peat oil")): ("oil flare", "fire"),
    frozenset(("smoke leaf", "cinder salt")): ("smoke bloom", "smoke"),
    frozenset(("brine", "spark salt")): ("conductive flash", "shock"),
    frozenset(("lime dust", "spring water")): ("caustic slurry", "lime"),
    frozenset(("frostwort", "spring water")): ("freezing wash", "ice"),
    frozenset(("healing herb", "spring water")): ("healing draft", "heal"),
    frozenset(("glow spore", "spring water")): ("attunement draft", "mana"),
    frozenset(("glow spore", "tree resin")): ("luminous seal", "glow"),
    frozenset(("iron filings", "brine")): ("corrosive grit", "corrode"),
    frozenset(("tree resin", "lime dust")): ("hardening wash", "seal"),
    frozenset(("smoke leaf", "spring water")): ("breath tonic", "breath"),
}
ENVIRONMENT_REACTIONS = {
    "water": "spring water",
    "fire": "cinder salt",
}


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
        return False, "Both the flask and ingredient must be physically carried."
    reagent = ingredient.kind.split(":", 1)[1]
    if reagent not in REAGENTS or sum(flask.contents.values()) >= 4:
        return False, "That reagent is unknown or the four-measure flask is full."
    before = predicted_reactions(flask.contents)
    flask.contents[reagent] = flask.contents.get(reagent, 0) + 1
    ingredient.quantity -= 1
    if ingredient.quantity == 0:
        ingredient.location, ingredient.owner_id = "destroyed", None
    sync_legacy_load(state)
    after = predicted_reactions(flask.contents)
    from .skill_tree import record_milestone

    if after:
        record_milestone(state, "craft:alchemy")
    _advance_world(state)
    message = f"One {reagent} measure enters {flask.id}; contents {flask.contents}."
    if set(after) - set(before):
        message += " Sealed reaction potential: " + ", ".join(sorted(set(after) - set(before))) + "."
    state.add_message(message, priority=3)
    return True, message


def pour_flask(state: GameState, flask_id: str, point: Position) -> tuple[bool, str]:
    from .actions import _advance_world
    from .materials import ensure_cell
    from .world import courier_sees, distance

    flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
    if flask is None or not flask.contents:
        return False, "Carry a filled field flask before pouring."
    if distance(state.position, point) > 1 or not courier_sees(state, point):
        return False, "Pour into a visible cell within one pace."
    cell = ensure_cell(state, point)
    if cell is None:
        return False, "This cell has no remaining material budget."
    combined = cell.reagents.copy()
    for name, quantity in flask.contents.items():
        combined[name] = combined.get(name, 0) + quantity
    if len(combined) > 4 or sum(combined.values()) > 8 or any(quantity > 4 for quantity in combined.values()):
        return False, "That sparse cell cannot hold the full mixture."
    warning = predicted_reactions(combined, cell)
    cell.reagents = combined
    flask.contents.clear()
    _advance_world(state)
    message = f"{state.courier.name} pours {flask.id} at {point.x},{point.y}."
    message += " Predicted reactions: " + (", ".join(warning) if warning else "none yet") + "."
    state.add_message(message, priority=3)
    return True, message


def drink_flask(state: GameState, flask_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import add_status

    flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
    if flask is None or not flask.contents:
        return False, "Carry a filled field flask before drinking."
    reactions = predicted_reactions(flask.contents)
    beneficial = {"healing draft", "attunement draft", "breath tonic"}
    if not reactions or not any(name in beneficial for name in reactions):
        return False, "This is not a known drinkable preparation; inspect or pour it instead."
    if any(name not in beneficial for name in reactions):
        return False, "The flask also holds a dangerous reaction; do not drink the mixture."
    if "healing draft" in reactions:
        state.courier.health = min(state.courier.max_health, state.courier.health + 3)
    if "attunement draft" in reactions:
        state.courier.mana = min(state.courier.max_mana, state.courier.mana + 3)
    if "breath tonic" in reactions:
        state.terrain_statuses.pop("smoke-inhalation", None)
        add_status(state, "clear-breath", "prepared smoke-leaf tonic", 4, "resists one smoke exposure")
    flask.contents.clear()
    for name in reactions:
        if name not in state.courier.known_formulas:
            state.courier.known_formulas.append(name)
    _advance_world(state)
    message = f"{state.courier.name} drinks {', '.join(reactions)} from {flask.id}; the finite measures are spent."
    state.add_message(message, priority=3)
    return True, message


def react_cell(state: GameState, point: Position, cell: MaterialCell) -> str | None:
    """Resolve at most one reaction per selected sparse cell and action."""
    from .materials import _expose

    if cell.reaction_due > state.world_time:
        return None
    cell.reaction_due = 0
    present = {name for name, quantity in cell.reagents.items() if quantity > 0}
    virtual = set()
    if cell.water:
        virtual.add("spring water")
    if cell.fire:
        virtual.add("cinder salt")
    pair = next((pair for pair in REACTIONS if pair <= present | virtual and pair & present), None)
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
        cell.smoke = max(1, cell.smoke)
    elif effect == "corrode":
        _expose(state, point, "salt", 1)
    elif effect == "seal":
        cell.support = min(3, cell.support + 1)
    elif effect == "breath":
        cell.smoke = max(1, cell.smoke)
    return name


def validate_chemistry(state: GameState) -> None:
    for item in state.items:
        if item.contents and item.kind != "field flask":
            raise ValueError("only physical flasks can hold freeform reagents")
        if (not isinstance(item.contents, dict) or len(item.contents) > 4
                or any(name not in REAGENTS or type(quantity) is not int or not 1 <= quantity <= 4
                       for name, quantity in item.contents.items())
                or sum(item.contents.values()) > 4):
            raise ValueError("invalid flask mixture")
    for cells in [state.vessel_materials, *(region.materials for region in state.regions.values())]:
        for cell in cells.values():
            if (not isinstance(cell.reagents, dict) or len(cell.reagents) > 4
                    or any(name not in REAGENTS or type(quantity) is not int or not 1 <= quantity <= 4
                           for name, quantity in cell.reagents.items())
                    or sum(cell.reagents.values()) > 8):
                raise ValueError("invalid sparse-cell chemistry")
