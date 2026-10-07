"""Physical gathering, station-gated fabrication and coarse witnessed work orders."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import CatalogError, load_catalog
from .expanded_weapons import ARSENAL
from .item_presentation import item_display_name_or_legacy
from .production_presentation import production_format, production_recipe_name, production_station_name, production_text
from .state import GameState, Position


_CATALOG = load_catalog("production.json", ("sources", "site_keys", "shore_stations", "recipes"))


def _regional_pairs(value: object, name: str) -> dict[str, tuple[str, str]]:
    if not isinstance(value, dict) or not value:
        raise CatalogError(f"production.json has invalid {name}")
    result = {}
    for region, pair in value.items():
        if (not isinstance(region, str) or not isinstance(pair, list) or len(pair) != 2
                or any(not isinstance(part, str) or not part for part in pair)):
            raise CatalogError(f"production.json has invalid {name}")
        result[region] = tuple(pair)
    return result


SOURCES = _regional_pairs(_CATALOG["sources"], "sources")
SHORE_STATIONS = _regional_pairs(_CATALOG["shore_stations"], "shore stations")
SITE_KEYS = _CATALOG["site_keys"]
if (not isinstance(SITE_KEYS, dict) or set(SITE_KEYS) != set(SOURCES)
        or set(SHORE_STATIONS) != set(SOURCES)
        or any(not isinstance(key, str) or not key for key in SITE_KEYS.values())):
    raise CatalogError("production.json has mismatched regional sites")


@dataclass(frozen=True)
class Recipe:
    id: str
    station: str
    inputs: tuple[tuple[str, int], ...]
    output: str
    quantity: int = 1
    contents: tuple[str, ...] = ()


RECIPES: dict[str, Recipe] = {}


def _add(recipe: Recipe) -> None:
    if recipe.id in RECIPES:
        raise CatalogError(f"duplicate production recipe: {recipe.id}")
    RECIPES[recipe.id] = recipe


def _recipe_from_data(row: object) -> Recipe:
    required = {"id", "station", "inputs", "output", "quantity", "contents"}
    if not isinstance(row, dict) or set(row) != required:
        raise CatalogError("production.json has an invalid recipe record")
    if (any(not isinstance(row[key], str) or not row[key] for key in ("id", "station", "output"))
            or row["id"].startswith("make:") or type(row["quantity"]) is not int or row["quantity"] < 1):
        raise CatalogError("production.json has invalid recipe identity or yield")
    inputs, contents = row["inputs"], row["contents"]
    if (not isinstance(inputs, list) or not inputs or not isinstance(contents, list)
            or any(not isinstance(part, list) or len(part) != 2 or not isinstance(part[0], str)
                   or not part[0] or type(part[1]) is not int or part[1] < 1 for part in inputs)
            or any(not isinstance(name, str) or not name for name in contents)
            or (contents and row["output"] != "field flask")):
        raise CatalogError("production.json has invalid recipe materials")
    return Recipe(row["id"], row["station"],
                  tuple((kind, quantity) for kind, quantity in inputs),
                  row["output"], row["quantity"], tuple(contents))


if not isinstance(_CATALOG["recipes"], list):
    raise CatalogError("production.json must provide recipes")
for _recipe_data in _CATALOG["recipes"]:
    _add(_recipe_from_data(_recipe_data))

for name, weapon in ARSENAL.items():
    station, inputs = {
        "blade": ("forge", (("component:iron billet", 1), ("commodity:timber", 1))),
        "reach": ("forge", (("component:iron billet", 1), ("commodity:timber", 1))),
        "impact": ("forge", (("component:iron billet", 1), ("commodity:timber", 1))),
        "bow": ("workshop", (("commodity:timber", 1), ("commodity:wool", 1))),
        "gun": ("gunworks", (("component:iron billet", 2), ("commodity:timber", 1), ("commodity:paper", 1))),
        "device": ("portable", (("ingredient:clay", 1), ("commodity:wool", 1))),
    }[weapon.family]
    _add(Recipe(f"make:{name}", station, inputs, name))


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
        return False, production_format("production.status.station", station=production_station_name(recipe.station))
    if recipe.contents and not any(not flask.contents for flask in state.items
                                   if flask.kind == "field flask" and flask.location == "pack"
                                   and flask.owner_id == state.active_courier_id):
        return False, production_text("production.status.empty_flask")
    missing = [f"{quantity - input_count(state, kind)} {item_display_name_or_legacy(kind)}" for kind, quantity in recipe.inputs
               if input_count(state, kind) < quantity]
    return (False, production_format("production.status.needs", inputs=", ".join(missing))) if missing else (True, production_text("production.status.ready"))


def make(state: GameState, recipe_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item, record_acquisition, sync_legacy_load
    from .skill_tree import record_milestone

    if recipe_id not in RECIPES:
        return False, production_text("production.make.unknown")
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
        provenance = production_format("production.provenance.work", courier=state.courier.name,
                                       station=production_station_name(recipe.station))
        masterwork = recipe.output in ARSENAL and has_node(state.courier, "masterwork")
        if masterwork:
            provenance = production_format("production.provenance.masterwork", work=provenance)
        output = create_item(state, recipe.output, provenance, quantity=quantity, masterwork=masterwork)
        if not auto_place(state, output.id, "pack", owner_id=state.active_courier_id):
            if state.location != "jomon" or not auto_place(state, output.id, "locker"):
                transaction.cancel(state)
                return False, production_text("production.make.space")
        record_acquisition(state, output)
    sync_legacy_load(state)
    domain = "brewing" if recipe.contents else "smelting" if recipe.station == "smelter" else "smithing" if recipe.station in {"forge", "gunworks", "workshop"} else "portable"
    record_milestone(state, f"craft:{domain}")
    _advance_world(state, steps=3 if recipe.station in {"forge", "gunworks", "smelter"} else 2)
    message = production_format("production.make.result", courier=state.courier.name,
                                recipe=production_recipe_name(recipe.id, recipe.output),
                                station=production_station_name(recipe.station))
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def gather(state: GameState, choice: int) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item
    from .skill_tree import record_milestone

    initialise_production(state)
    if not at_shore_site(state) or choice not in (0, 1):
        return False, production_text("production.gather.invalid")
    site = state.production["sites"][state.active_region_id]
    if site["stock"] <= 0:
        return False, production_text("production.gather.exhausted")
    name = SOURCES[state.active_region_id][choice]
    transaction = InventoryTransaction.begin(state)
    from .region_presentation import region_display_name

    item = create_item(state, f"ingredient:{name}", production_format(
        "production.gather.provenance", region=region_display_name(state.active_region_id)))
    if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        transaction.cancel(state)
        return False, production_text("production.gather.pack")
    site["stock"] -= 1
    site["draws"] += 1
    record_milestone(state, f"production:{state.active_region_id}")
    _advance_world(state)
    from .chemistry_presentation import reagent_display_name

    message = production_format("production.gather.result", courier=state.courier.name,
                                reagent=reagent_display_name(name), stock=site["stock"])
    state.add_message(message, priority=3)
    return True, message


def delegate(state: GameState, recipe_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .character import effective_competency
    from .inventory import sync_legacy_load
    from .skill_tree import has_node, record_milestone

    initialise_production(state)
    if not at_shore_site(state):
        return False, production_text("production.delegate.location")
    if effective_competency(state.courier, "speech") < 7 and not has_node(state.courier, "work-order"):
        return False, production_text("production.delegate.competency")
    if len(state.production["orders"]) >= 6:
        return False, production_text("production.delegate.capacity")
    recipe = RECIPES.get(recipe_id)
    if recipe is None or recipe.contents:
        return False, production_text("production.delegate.recipe")
    legal, reason = recipe_status(state, recipe_id)
    if not legal:
        return False, reason
    if state.trade_credit < 1:
        return False, production_text("production.delegate.credit")
    for kind, quantity in recipe.inputs:
        _consume_input(state, kind, quantity)
    state.trade_credit -= 1
    sync_legacy_load(state)
    witness = state.contacts[state.active_region_id][-1]
    state.production["orders"].append({"region": state.active_region_id, "recipe": recipe_id,
                                        "ready_day": state.world_time // 36 + 1, "worker": witness.id})
    record_milestone(state, "social:delegation")
    _advance_world(state)
    message = production_format("production.delegate.result", worker=witness.name,
                                recipe=production_recipe_name(recipe.id, recipe.output))
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
            state.region.changes["production_shift"] = production_format(
                "production.shift.record", day=today, stock=site["stock"],
                institution=institution.name if institution else production_text("production.shift.default_institution"),
                status=production_text("production.shift.supported" if supported else "production.shift.unsupported"),
            )
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
        from .region_presentation import region_display_name

        worker = next(contact.name for contact in state.contacts[order["region"]]
                      if contact.id == order["worker"])
        item = create_item(state, recipe.output,
                           production_format("production.provenance.order", worker=worker),
                           location="ground", quantity=recipe.quantity)
        item.region_id, item.ground_position = order["region"], point
        record = production_format("production.order.record", day=today,
                                   recipe=production_recipe_name(recipe.id, recipe.output), worker=worker,
                                   region=region_display_name(order["region"]), x=point.x, y=point.y)
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
