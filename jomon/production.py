"""Physical gathering, station-gated fabrication and coarse witnessed work orders."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position


SOURCES = {
    "hearthford": ("iron filings", "spring water"),
    "greywash": ("brine", "clay"),
    "greenwold": ("tree resin", "healing herb"),
    "whitecairn": ("iron ore", "lime dust"),
    "dunmire": ("peat oil", "smoke leaf"),
    "marlbank": ("clay", "spring water"),
    "rillscar": ("iron ore", "spark salt"),
    "frostmere": ("frostwort", "glow spore"),
}
SITE_KEYS = {
    "hearthford": "mill", "greywash": "saltworks", "greenwold": "resin_yard",
    "whitecairn": "quarry", "dunmire": "works", "marlbank": "works",
    "rillscar": "works", "frostmere": "works",
}
SHORE_STATIONS = {
    "hearthford": ("workshop", "forge"),
    "greywash": ("still", "brewery"),
    "greenwold": ("brewery", "still"),
    "whitecairn": ("smelter", "forge"),
    "dunmire": ("brewery", "still"),
    "marlbank": ("smelter", "forge"),
    "rillscar": ("smelter", "gunworks"),
    "frostmere": ("still", "gunworks"),
}


@dataclass(frozen=True)
class Recipe:
    id: str
    name: str
    station: str
    inputs: tuple[tuple[str, int], ...]
    output: str
    quantity: int = 1
    contents: tuple[str, ...] = ()


RECIPES: dict[str, Recipe] = {}


def _add(recipe: Recipe) -> None:
    RECIPES[recipe.id] = recipe


_add(Recipe("field-flask", "Shape a field flask", "portable", (("ingredient:clay", 1), ("commodity:timber", 1)), "field flask"))
_add(Recipe("iron-billet", "Smelt one iron billet", "smelter", (("ingredient:iron ore", 1), ("commodity:charcoal", 1)), "component:iron billet"))
_add(Recipe("field-dressing", "Prepare a field dressing", "portable", (("ingredient:healing herb", 1), ("commodity:wool", 1)), "consumable:willow dressing"))
for reagent, second in (
    ("smoke", "smoke leaf"), ("pitch", "peat oil"), ("lime", "lime dust"),
    ("brine", "brine"), ("thunder", "spark salt"), ("resin", "tree resin"),
):
    other = "ingredient:iron filings" if reagent == "thunder" else "ingredient:tree resin" if reagent == "pitch" else "ingredient:clay"
    _add(Recipe(f"{reagent}-bombs", f"Pack {reagent} bombs", "portable",
                ((f"ingredient:{second}", 1), (other, 1)), f"consumable:{reagent} bombs", 2))
for name, ingredients in (
    ("healing draft", ("healing herb", "spring water")),
    ("attunement draft", ("glow spore", "spring water")),
    ("breath tonic", ("smoke leaf", "spring water")),
):
    _add(Recipe(name.replace(" ", "-"), f"Brew {name}", "brewery",
                tuple((f"ingredient:{part}", 1) for part in ingredients), "field flask", contents=ingredients))
_add(Recipe("counted-charges", "Prepare fictional spark-salt charges", "gunworks",
            (("ingredient:spark salt", 1), ("commodity:paper", 1)), "consumable:handgonne charges", 3))

from .expanded_weapons import ARSENAL

for name, weapon in ARSENAL.items():
    station, inputs = {
        "blade": ("forge", (("component:iron billet", 1), ("commodity:timber", 1))),
        "reach": ("forge", (("component:iron billet", 1), ("commodity:timber", 1))),
        "impact": ("forge", (("component:iron billet", 1), ("commodity:timber", 1))),
        "bow": ("workshop", (("commodity:timber", 1), ("commodity:wool", 1))),
        "gun": ("gunworks", (("component:iron billet", 2), ("commodity:timber", 1), ("commodity:paper", 1))),
        "device": ("portable", (("ingredient:clay", 1), ("commodity:wool", 1))),
    }[weapon.family]
    _add(Recipe(f"make:{name}", f"Fabricate {name}", station, inputs, name))


def initialise_production(state: GameState) -> None:
    if state.production:
        return
    state.production = {
        "sites": {region: {"stock": 4, "last_day": state.world_time // 36, "draws": 0}
                  for region in SOURCES},
        "orders": [], "records": [],
    }


def site_position(state: GameState, region_id: str | None = None) -> Position | None:
    region_id = region_id or state.active_region_id
    region = state.regions.get(region_id)
    return region.landmarks.get(SITE_KEYS[region_id]) if region and region_id in SITE_KEYS else None


def at_shore_site(state: GameState) -> bool:
    from .world import distance

    point = site_position(state)
    return bool(state.location == "region" and point and distance(state.position, point) <= 2)


def stations_here(state: GameState) -> set[str]:
    from .vessel_refits import installed
    from .world import base_tile

    if state.jomon_space == "tavern" and state.location == "jomon":
        return set()
    stations = {"portable"}
    if at_shore_site(state):
        stations.update(SHORE_STATIONS[state.active_region_id])
    if state.location == "jomon" and state.jomon_space == "vessel" and state.voyage_status != "active":
        tile = base_tile(state, state.position)
        if tile == "W":
            stations.add("workshop")
        for station, refit, tile_required in (
            ("forge", "vessel-field-forge", "R"),
            ("smelter", "vessel-field-forge", "R"),
            ("still", "vessel-glass-still", "G"),
            ("brewery", "vessel-glass-still", "G"),
            ("gunworks", "vessel-gunworks", "S"),
        ):
            if tile == tile_required and installed(state, refit):
                stations.add(station)
    if "forge" in stations and state.courier and "gun-assembly" in state.courier.skill_nodes:
        stations.add("gunworks")
    return stations


def _sources_for(state: GameState, kind: str) -> list:
    allowed = {"pack"} | ({"locker"} if state.location == "jomon" else set())
    return [item for item in state.items if item.kind == kind and item.location in allowed
            and (item.owner_id == state.active_courier_id or item.location == "locker") and item.quantity > 0]


def input_count(state: GameState, kind: str) -> int:
    count = sum(item.quantity for item in _sources_for(state, kind))
    if kind.startswith("commodity:") and state.location == "jomon":
        stack = state.vessel_cargo.get(kind.split(":", 1)[1])
        count += stack.quantity if stack else 0
    return count


def _consume_input(state: GameState, kind: str, quantity: int) -> None:
    remaining = quantity
    for item in _sources_for(state, kind):
        amount = min(item.quantity, remaining)
        item.quantity -= amount
        remaining -= amount
        if item.quantity == 0:
            item.location, item.owner_id = "destroyed", None
        if remaining == 0:
            break
    if remaining and kind.startswith("commodity:") and state.location == "jomon":
        name = kind.split(":", 1)[1]
        state.vessel_cargo[name].quantity -= remaining
        if state.vessel_cargo[name].quantity == 0:
            del state.vessel_cargo[name]


def recipe_status(state: GameState, recipe_id: str) -> tuple[bool, str]:
    recipe = RECIPES[recipe_id]
    if recipe.station not in stations_here(state):
        return False, f"requires a physical {recipe.station} station"
    if recipe.contents and not any(not flask.contents for flask in state.items
                                   if flask.kind == "field flask" and flask.location == "pack"
                                   and flask.owner_id == state.active_courier_id):
        return False, "requires one carried empty field flask"
    missing = [f"{quantity - input_count(state, kind)} {kind}" for kind, quantity in recipe.inputs
               if input_count(state, kind) < quantity]
    return (False, "needs " + ", ".join(missing)) if missing else (True, "ready")


def make(state: GameState, recipe_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item, record_acquisition, sync_legacy_load
    from .skill_tree import record_milestone

    if recipe_id not in RECIPES:
        return False, "Unknown fabrication plan."
    legal, reason = recipe_status(state, recipe_id)
    if not legal:
        return False, reason
    recipe = RECIPES[recipe_id]
    from .skill_tree import has_node

    transaction = InventoryTransaction.begin(state)
    for kind, quantity in recipe.inputs:
        if recipe.id == "iron-billet" and kind == "commodity:charcoal" and has_node(state.courier, "fuel-husbandry"):
            continue
        _consume_input(state, kind, quantity)
    if recipe.contents:
        flask = next(item for item in state.items if item.kind == "field flask" and item.location == "pack"
                     and item.owner_id == state.active_courier_id and not item.contents)
        flask.contents = {name: recipe.contents.count(name) for name in set(recipe.contents)}
    else:
        quantity = recipe.quantity + int(recipe.id == "iron-billet" and has_node(state.courier, "bloom-sorting"))
        provenance = ("masterwork: " if recipe.output in ARSENAL and has_node(state.courier, "masterwork") else "") + f"{state.courier.name}'s {recipe.station} work"
        output = create_item(state, recipe.output, provenance, quantity=quantity)
        if not auto_place(state, output.id, "pack", owner_id=state.active_courier_id):
            if state.location != "jomon" or not auto_place(state, output.id, "locker"):
                transaction.cancel(state)
                return False, "The completed item needs clear pack or Jomon locker cells; nothing was consumed."
        record_acquisition(state, output)
    sync_legacy_load(state)
    domain = "brewing" if recipe.contents else "smelting" if recipe.station == "smelter" else "smithing" if recipe.station in {"forge", "gunworks", "workshop"} else "portable"
    record_milestone(state, f"craft:{domain}")
    _advance_world(state, steps=3 if recipe.station in {"forge", "gunworks", "smelter"} else 2)
    message = f"{state.courier.name} completes {recipe.name} at the {recipe.station}; every input was physically spent."
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def gather(state: GameState, choice: int) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item
    from .skill_tree import record_milestone

    initialise_production(state)
    if not at_shore_site(state) or choice not in (0, 1):
        return False, "Gather at the physical regional works and choose one local source."
    site = state.production["sites"][state.active_region_id]
    if site["stock"] <= 0:
        return False, "The visible work lot is exhausted until the regional supply account renews."
    name = SOURCES[state.active_region_id][choice]
    transaction = InventoryTransaction.begin(state)
    item = create_item(state, f"ingredient:{name}", f"{state.active_region_id} witnessed gathering")
    if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        transaction.cancel(state)
        return False, "No clear pack cell can hold the gathered measure."
    site["stock"] -= 1
    site["draws"] += 1
    record_milestone(state, f"production:{state.active_region_id}")
    _advance_world(state)
    message = f"{state.courier.name} gathers one {name} at the regional works; {site['stock']} source lots remain."
    state.add_message(message, priority=3)
    return True, message


def delegate(state: GameState, recipe_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .character import effective_competency
    from .inventory import sync_legacy_load
    from .skill_tree import has_node, record_milestone

    initialise_production(state)
    if not at_shore_site(state):
        return False, "A witnessed work order needs the courier at the regional works."
    if effective_competency(state.courier, "speech") < 7 and not has_node(state.courier, "work-order"):
        return False, "Delegation needs effective Speech 7 or the Work order skill."
    if len(state.production["orders"]) >= 6:
        return False, "Six open physical orders already occupy the regional accounts."
    recipe = RECIPES.get(recipe_id)
    if recipe is None or recipe.contents:
        return False, "Choose a non-brew production item for a work order."
    legal, reason = recipe_status(state, recipe_id)
    if not legal:
        return False, reason
    if state.trade_credit < 1:
        return False, "A witnessed work order costs one credit."
    for kind, quantity in recipe.inputs:
        _consume_input(state, kind, quantity)
    state.trade_credit -= 1
    sync_legacy_load(state)
    witness = state.contacts[state.active_region_id][-1]
    state.production["orders"].append({"region": state.active_region_id, "recipe": recipe_id,
                                        "ready_day": state.world_time // 36 + 1, "worker": witness.id})
    record_milestone(state, "social:delegation")
    _advance_world(state)
    message = f"{witness.name} accepts {recipe.name}; paid inputs are held in a witnessed order, ready next day at the works."
    state.add_message(message, priority=3)
    return True, message


def advance_craft_economy(state: GameState) -> None:
    from .inventory import create_item

    initialise_production(state)
    today = state.world_time // 36
    for region_id, site in state.production["sites"].items():
        elapsed = min(3, today - site["last_day"])
        if elapsed <= 0:
            continue
        site["last_day"] = today
        institution = state.institutions.get(f"work:{region_id}")
        market = state.regional_markets.get(region_id)
        supported = bool(market and institution and market[institution.dependency].stock > 0)
        if supported:
            site["stock"] = min(8, site["stock"] + elapsed)
        if state.location == "region" and state.active_region_id == region_id:
            state.region.changes["production_shift"] = f"Day {today}: {site['stock']} gathered source lots; {institution.name if institution else 'workers'} {'supplied' if supported else 'awaits inputs'}."
    pending = []
    for order in state.production["orders"]:
        if order["ready_day"] > today:
            pending.append(order)
            continue
        recipe = RECIPES[order["recipe"]]
        point = site_position(state, order["region"])
        if point is None:
            pending.append(order)
            continue
        item = create_item(state, recipe.output, f"{order['worker']}'s witnessed production", location="ground", quantity=recipe.quantity)
        item.region_id, item.ground_position = order["region"], point
        record = f"Day {today}: {recipe.name} by {order['worker']} waits physically at {order['region']} works ({point.x},{point.y})."
        state.production["records"].append(record)
        del state.production["records"][:-20]
        if state.location == "region" and state.active_region_id == order["region"]:
            state.add_message(record, priority=3)
    state.production["orders"] = pending


def validate_production(state: GameState) -> None:
    if not isinstance(state.production, dict) or set(state.production) != {"sites", "orders", "records"}:
        raise ValueError("invalid production ledger")
    sites = state.production["sites"]
    if not isinstance(sites, dict) or set(sites) != set(SOURCES):
        raise ValueError("regional production sites are missing")
    for site in sites.values():
        if (not isinstance(site, dict) or set(site) != {"stock", "last_day", "draws"}
                or any(type(site[key]) is not int or site[key] < 0 for key in site)
                or site["stock"] > 8):
            raise ValueError("invalid bounded production site")
    if (not isinstance(state.production["orders"], list) or not isinstance(state.production["records"], list)
            or len(state.production["orders"]) > 6 or len(state.production["records"]) > 20
            or any(not isinstance(record, str) for record in state.production["records"])):
        raise ValueError("unbounded work orders")
    for order in state.production["orders"]:
        if (not isinstance(order, dict) or set(order) != {"region", "recipe", "ready_day", "worker"}
                or order["region"] not in SOURCES or order["recipe"] not in RECIPES
                or type(order["ready_day"]) is not int or order["ready_day"] < 0
                or order["worker"] not in {contact.id for contact in state.contacts[order["region"]]}):
            raise ValueError("invalid witnessed work order")
