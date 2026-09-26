"""Physical workshop fittings. Explicit effects, one structure and one treatment."""

from __future__ import annotations

from dataclasses import dataclass, replace

from .catalog import EQUIPMENT_SECTIONS, load_catalog
from .equipment_presentation import equipment_format, fitting_drawback, fitting_effect, fitting_name
from .state import GameState, Item, Position, stage_rng


@dataclass(frozen=True)
class Fitting:
    name: str
    slot: str
    shape: tuple[int, int]
    weight: int
    targets: tuple[str, ...]
    effect: str
    drawback: str
    price: int = 3


FITTINGS = {
    name: Fitting(**{**row, "name": fitting_name(name), "effect": fitting_effect(name), "drawback": fitting_drawback(name), "shape": tuple(row["shape"]), "targets": tuple(row["targets"])})
    for name, row in load_catalog("equipment.json", EQUIPMENT_SECTIONS)["fittings"].items()
}
WORKBENCH = Position(39, 5, -1)
SLOTS = ("readied", "secondary", "head", "torso", "arms", "hands", "legs", "feet")


def initialise_workshop(state: GameState) -> None:
    rng = stage_rng(state.seed, "counted workshop fittings")
    for name in FITTINGS:
        state.vessel_changes.setdefault(f"fitting_stock:{name}", rng.randrange(1, 3))


def attached(state: GameState, item: Item, *, working=False) -> list[Item]:
    return [part for part in state.items if part.location == "fitted" and part.fitted_to == item.id and (not working or part.condition > 0 and item.condition > 0)]


def compatible(item: Item, name: str) -> bool:
    from .inventory import item_spec

    definition = FITTINGS.get(name)
    spec = item_spec(item.kind)
    return bool(definition and {item.kind, spec.category, spec.slot} & set(definition.targets))


def active_part(state: GameState, name: str, slot="readied") -> Item | None:
    from .inventory import equipped_item

    parent = equipped_item(state, slot)
    return next((part for part in attached(state, parent, working=True) if part.kind == f"fitting:{name}"), None) if parent else None


def effective_spec(state: GameState, item: Item):
    from .inventory import item_spec

    spec = item_spec(item.kind)
    names = {part.kind.split(":", 1)[1] for part in attached(state, item, working=True)}
    tags = set(spec.tags)
    if "wool lining" in names:
        tags.update(("warm", "water-heavy", "absorbent"))
    if "reed lining" in names:
        tags.update(("mudproof", "deep-water"))
    if "resin seal" in names:
        tags.add("resin-coated")
    scales = int("iron scales" in names)
    from .legendary import legend_for_item
    from .legendary_presentation import legendary_format
    legend = legend_for_item(state, item)
    if legend:
        tags.update(legend.tags)
    return replace(
        spec,
        name=legend.name if legend else spec.name,
        description=(legendary_format("legendary.object.description", provenance=legend.provenance, effect=legend.major_effect, tradeoff=legend.tradeoff, interested=legend.interested_party, clue=legend.clue) if legend else spec.description),
        weight=spec.weight + int(legend is not None),
        tags=tuple(sorted(tags)),
        pierce=max(0, spec.pierce + scales - int("reed lining" in names)),
        coverage=min(3, spec.coverage + scales),
        noise=spec.noise + scales + int(legend is not None),
        mobility=spec.mobility + scales,
    )


def _owned_target(state: GameState, target_id: str) -> Item | None:
    return next((item for item in state.items if item.id == target_id and item.owner_id == state.active_courier_id and item.location in SLOTS), None)


def _bench(state: GameState) -> bool:
    return state.location == "jomon" and state.jomon_space == "vessel" and state.position == WORKBENCH


def fitting_source(state: GameState, name: str) -> Item | None:
    return next((item for item in state.items if item.kind == f"fitting:{name}" and item.condition > 0 and (item.location == "locker" or item.location == "pack" and item.owner_id == state.active_courier_id)), None)


def fit_cost(state: GameState, name: str) -> int:
    return 1 if fitting_source(state, name) else FITTINGS[name].price + 1


def can_fit(state: GameState, target: Item | None, name: str) -> tuple[bool, str]:
    if target is None or not compatible(target, name):
        return False, equipment_format("equipment.workshop.compatible")
    if target.condition <= 0:
        return False, equipment_format("equipment.workshop.parent_damaged")
    definition = FITTINGS[name]
    if any(FITTINGS[part.kind.split(":", 1)[1]].slot == definition.slot for part in attached(state, target)):
        return False, equipment_format("equipment.workshop.socket_occupied", slot=definition.slot)
    if fitting_source(state, name) is None and not state.vessel_changes.get(f"fitting_stock:{name}", 0):
        return False, equipment_format("equipment.workshop.stock_empty")
    if state.trade_credit < fit_cost(state, name):
        return False, equipment_format("equipment.workshop.credit", cost=fit_cost(state, name))
    return True, equipment_format("equipment.workshop.ready")


def _finish(state: GameState, message: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import sync_legacy_load

    sync_legacy_load(state)
    _advance_world(state)
    _advance_world(state)
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def install(state: GameState, target_id: str, name: str) -> tuple[bool, str]:
    from .inventory import create_item, item_spec

    if not _bench(state) or name not in FITTINGS:
        return False, equipment_format("equipment.workshop.install.location")
    target = _owned_target(state, target_id)
    valid, reason = can_fit(state, target, name)
    if not valid:
        return False, reason
    cost = fit_cost(state, name)
    part = fitting_source(state, name)
    if part is None:
        part = create_item(state, f"fitting:{name}", equipment_format("equipment.provenance.fitting_installed", target=target.id), location="fitted")
        state.vessel_changes[f"fitting_stock:{name}"] -= 1
    part.location, part.owner_id, part.container_id = "fitted", None, None
    part.fitted_to, part.ground_position, part.region_id = target.id, None, None
    state.trade_credit -= cost
    if target and state.courier and "armour-fitting" in state.courier.skill_nodes and item_spec(target.kind).category == "armour":
        target.condition = min(100, target.condition + 5)
    from .skill_tree import record_milestone

    record_milestone(state, "craft:smithing")
    return _finish(state, equipment_format("equipment.workshop.install.result", fitting=FITTINGS[name].name, item=item_spec(target.kind).name, cost=cost, effect=FITTINGS[name].effect))


def remove(state: GameState, target_id: str, socket: str) -> tuple[bool, str]:
    from .inventory import auto_place

    target = _owned_target(state, target_id)
    if not _bench(state) or target is None:
        return False, equipment_format("equipment.workshop.remove.location")
    part = next((part for part in attached(state, target) if FITTINGS[part.kind.split(":", 1)[1]].slot == socket), None)
    if part is None or state.trade_credit < 1:
        return False, equipment_format("equipment.workshop.remove.requirements")
    old = dict(part.__dict__)
    if not auto_place(state, part.id, "pack", owner_id=state.active_courier_id):
        part.__dict__.update(old)
        return False, equipment_format("equipment.workshop.remove.pack")
    part.fitted_to = None
    state.trade_credit -= 1
    return _finish(state, equipment_format("equipment.workshop.remove.result", fitting=FITTINGS[part.kind.split(":", 1)[1]].name))


def buy_kit(state: GameState, name: str) -> tuple[bool, str]:
    from .inventory import auto_place, create_item

    if not _bench(state) or name not in FITTINGS:
        return False, equipment_format("equipment.workshop.buy.location")
    cost = FITTINGS[name].price
    if state.trade_credit < cost or state.vessel_changes.get(f"fitting_stock:{name}", 0) <= 0:
        return False, equipment_format("equipment.workshop.buy.stock")
    item = create_item(state, f"fitting:{name}", equipment_format("equipment.provenance.fitting_kit"))
    if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        state.items.remove(item)
        state.next_item_id -= 1
        return False, equipment_format("equipment.workshop.buy.pack")
    state.vessel_changes[f"fitting_stock:{name}"] -= 1
    state.trade_credit -= cost
    return _finish(state, equipment_format("equipment.workshop.buy.result", fitting=FITTINGS[name].name, cost=cost))


def repair(state: GameState, target_id: str) -> tuple[bool, str]:
    from .character import effective_competency
    from .inventory import item_spec

    target = _owned_target(state, target_id)
    if not _bench(state) or target is None or target.condition >= 100:
        return False, equipment_format("equipment.workshop.repair.invalid")
    if state.trade_credit < 2:
        return False, equipment_format("equipment.workshop.repair.credit")
    craft = effective_competency(state.courier, "craft") if state.courier else 0
    target.condition = min(100, target.condition + 35 + min(10, (craft // 5) * 5)
                           + (5 if state.courier and "tool-care" in state.courier.skill_nodes else 0))
    from .skill_tree import record_milestone

    record_milestone(state, "craft:smithing")
    state.trade_credit -= 2
    if state.courier:
        state.courier.craft = min(20, state.courier.craft + 1)
    return _finish(state, equipment_format("equipment.workshop.repair.result", item=item_spec(target.kind).name, condition=target.condition))


def describe(state: GameState, target: Item) -> list[str]:
    spec = effective_spec(state, target)
    lines = [equipment_format("equipment.workshop.describe.item", item=spec.name, condition=target.condition, cut=spec.cut, pierce=spec.pierce, blunt=spec.blunt, coverage=spec.coverage)]
    lines.extend(equipment_format("equipment.workshop.describe.part", fitting=FITTINGS[part.kind.split(":", 1)[1]].name, condition=part.condition, slot=FITTINGS[part.kind.split(":", 1)[1]].slot) for part in attached(state, target))
    lines.append(equipment_format("equipment.workshop.describe.guidance"))
    return lines


def attack_effects(state: GameState, target: Position, sound: int, ammunition: str | None) -> tuple[int, str]:
    from .inventory import AMMUNITION_ITEMS, auto_place, create_item, sync_legacy_load
    from .world import distance, line_of_sight, position_key

    messages = []
    binding = active_part(state, "quiet binding")
    if binding:
        sound = max(0, sound - 2)
        binding.condition = max(0, binding.condition - 1)
        messages.append(equipment_format("equipment.fitting.quiet_binding"))
    seal = active_part(state, "resin seal")
    if seal and state.weather in {"hard rain", "coast squall", "forest rain"}:
        seal.condition = max(0, seal.condition - 5)
        messages.append(equipment_format("equipment.fitting.resin_seal"))
    wrap = active_part(state, "ash wrap")
    if wrap and (position_key(state.position) in state.smoke or "smoke-inhalation" in state.terrain_statuses):
        wrap.condition = max(0, wrap.condition - 5)
        messages.append(equipment_format("equipment.fitting.ash_wrap"))
    cord = active_part(state, "retrieval cord")
    if cord and ammunition:
        cord.condition = max(0, cord.condition - 10)
        if state.weapon == "hooked javelin":
            recovered = next((item for item in reversed(state.items) if item.location == "ground" and item.kind == AMMUNITION_ITEMS[ammunition] and item.ground_position == target), None)
        else:
            recovered = create_item(state, AMMUNITION_ITEMS[ammunition], equipment_format("equipment.provenance.tethered_throw"), location="ground")
            recovered.region_id, recovered.ground_position = state.spatial_id, target
        if recovered:
            if distance(state.position, target) <= 4 and line_of_sight(state, state.position, target) and auto_place(state, recovered.id, "pack", owner_id=state.active_courier_id):
                sync_legacy_load(state)
                messages.append(equipment_format("equipment.fitting.retrieval.recovered"))
            else:
                messages.append(equipment_format("equipment.fitting.retrieval.ground"))
    return sound, "; ".join(messages)


def validate_fittings(state: GameState) -> None:
    ids = {item.id: item for item in state.items}
    sockets = set()
    for part in state.items:
        if part.location != "fitted":
            if part.fitted_to is not None:
                raise ValueError("unmounted item retains a fitting parent")
            continue
        name = part.kind.removeprefix("fitting:")
        parent = ids.get(part.fitted_to)
        if name not in FITTINGS or parent is None or parent.location == "fitted" or parent.id == part.id or not compatible(parent, name) or part.quantity != 1:
            raise ValueError("invalid physical fitting parent or compatibility")
        socket = (parent.id, FITTINGS[name].slot)
        if socket in sockets:
            raise ValueError("two fittings occupy the same socket")
        sockets.add(socket)
        if part.owner_id is not None or part.container_id is not None:
            raise ValueError("fitted ownership must follow its parent")
    for name in FITTINGS:
        stock = state.vessel_changes.get(f"fitting_stock:{name}", 0)
        if type(stock) is not int or not 0 <= stock <= 2:
            raise ValueError("invalid counted fitting stock")
