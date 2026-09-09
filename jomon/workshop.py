"""Physical workshop fittings. Explicit effects, one structure and one treatment."""

from __future__ import annotations

from dataclasses import dataclass, replace

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
    "iron heel": Fitting("Iron heel", "structure", (1, 2), 2, ("staff", "spear", "pike", "boar spear", "cudgel"), "Allows F brace, lever and break with the readied shaft; successful work wears the heel.", "Heavy; levering makes two extra noise."),
    "quiet binding": Fitting("Quiet binding", "structure", (1, 2), 1, ("weapon",), "Cuts committed attack sound by two; each attack wears its felt.", "Cannot share the structural socket with a heel or retrieval cord."),
    "retrieval cord": Fitting("Retrieval cord", "structure", (1, 3), 2, ("javelins", "hooked javelin", "weighted net"), "Leaves a spent shaft or net as a physical recoverable item; nearby casts reel back if the pack fits.", "Casting range falls by two; ten committed throws wear out the cord."),
    "resin seal": Fitting("Resin seal", "treatment", (2, 1), 1, ("crossbow", "heavy crossbow", "longbow"), "A sealed string keeps its prepared shot in rain; wet shots wear the treatment.", "Stiff string costs one range; resin accelerates fire damage to the weapon."),
    "ash wrap": Fitting("Ash wrap", "treatment", (1, 2), 1, ("weapon",), "A shot made from smoke retains range despite smoke inhalation; firing from smoke wears the wrap.", "Only the inhalation penalty is relieved: thick smoke still blocks line of fire."),
    "wool lining": Fitting("Wool lining", "lining", (2, 2), 2, ("armour",), "Warm insulation prevents winter-water chilling.", "Absorbent wool adds four wet weight as well as its dry weight."),
    "reed lining": Fitting("Reed lining", "lining", (2, 2), 1, ("feet", "legs"), "Sheds bog mud and gives a usable stance in deep current.", "The bulky liner opens a pierce-protection gap at the fitted location."),
    "iron scales": Fitting("Iron scales", "lining", (2, 2), 3, ("armour",), "Adds local pierce protection and closes a coverage gap.", "One extra noise and mobility burden; no second lining fits."),
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
    return replace(spec, tags=tuple(sorted(tags)), pierce=max(0, spec.pierce + scales - int("reed lining" in names)), coverage=min(3, spec.coverage + scales), noise=spec.noise + scales, mobility=spec.mobility + scales)


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
        return False, "Choose a compatible worn or readied item."
    if target.condition <= 0:
        return False, "Repair the parent item first."
    definition = FITTINGS[name]
    if any(FITTINGS[part.kind.split(":", 1)[1]].slot == definition.slot for part in attached(state, target)):
        return False, f"Remove the existing {definition.slot} fitting first."
    if fitting_source(state, name) is None and not state.vessel_changes.get(f"fitting_stock:{name}", 0):
        return False, "No physical kit or counted workshop stock remains."
    if state.trade_credit < fit_cost(state, name):
        return False, f"Need {fit_cost(state, name)} credit for the kit and bench work."
    return True, "Two actions; fitted mass still counts."


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
    from .inventory import create_item

    if not _bench(state) or name not in FITTINGS:
        return False, "Approach the lower workshop with a known fitting."
    target = _owned_target(state, target_id)
    valid, reason = can_fit(state, target, name)
    if not valid:
        return False, reason
    cost = fit_cost(state, name)
    part = fitting_source(state, name)
    if part is None:
        part = create_item(state, f"fitting:{name}", f"counted workshop kit fitted to {target.id}", location="fitted")
        state.vessel_changes[f"fitting_stock:{name}"] -= 1
    part.location, part.owner_id, part.container_id = "fitted", None, None
    part.fitted_to, part.ground_position, part.region_id = target.id, None, None
    state.trade_credit -= cost
    return _finish(state, f"The workshop fits {name} to {target.kind}; {cost} credit and two actions. {FITTINGS[name].effect}")


def remove(state: GameState, target_id: str, socket: str) -> tuple[bool, str]:
    from .inventory import auto_place

    target = _owned_target(state, target_id)
    if not _bench(state) or target is None:
        return False, "Approach the workshop with the equipped parent item."
    part = next((part for part in attached(state, target) if FITTINGS[part.kind.split(":", 1)[1]].slot == socket), None)
    if part is None or state.trade_credit < 1:
        return False, "Removal needs a fitted part and one credit."
    old = dict(part.__dict__)
    if not auto_place(state, part.id, "pack", owner_id=state.active_courier_id):
        part.__dict__.update(old)
        return False, "The removed part does not fit. Repack first; nothing changed."
    part.fitted_to = None
    state.trade_credit -= 1
    return _finish(state, f"Removed {part.kind.split(':', 1)[1]} intact into the pack; one credit and two actions.")


def buy_kit(state: GameState, name: str) -> tuple[bool, str]:
    from .inventory import auto_place, create_item

    if not _bench(state) or name not in FITTINGS:
        return False, "Buy counted kits at the lower workshop."
    cost = FITTINGS[name].price
    if state.trade_credit < cost or state.vessel_changes.get(f"fitting_stock:{name}", 0) <= 0:
        return False, "Insufficient credit or counted stock."
    item = create_item(state, f"fitting:{name}", "counted Jomon workshop kit")
    if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        state.items.remove(item)
        state.next_item_id -= 1
        return False, "The kit stays at the bench: no pack space. No stock or credit spent."
    state.vessel_changes[f"fitting_stock:{name}"] -= 1
    state.trade_credit -= cost
    return _finish(state, f"Bought a physical {name} kit for {cost} credit; two actions of counted work.")


def repair(state: GameState, target_id: str) -> tuple[bool, str]:
    target = _owned_target(state, target_id)
    if not _bench(state) or target is None or target.condition >= 100:
        return False, "Choose damaged worn or readied equipment at the workshop."
    if state.trade_credit < 2:
        return False, "Repair needs two credit for material and two actions."
    target.condition = min(100, target.condition + 35)
    state.trade_credit -= 2
    return _finish(state, f"Repair work brings {target.kind} to {target.condition} condition; two credit, two actions. Fitting wear remains separate.")


def describe(state: GameState, target: Item) -> list[str]:
    spec = effective_spec(state, target)
    lines = [f"{spec.name}: condition {target.condition}; cut/pierce/blunt {spec.cut}/{spec.pierce}/{spec.blunt}; coverage {spec.coverage}."]
    lines.extend(f"{part.kind.split(':', 1)[1]}: condition {part.condition}; {FITTINGS[part.kind.split(':', 1)[1]].slot}." for part in attached(state, target))
    lines.append("Fittings follow the parent through drops, theft and death; removal needs pack room.")
    return lines


def attack_effects(state: GameState, target: Position, sound: int, ammunition: str | None) -> tuple[int, str]:
    from .inventory import AMMUNITION_ITEMS, auto_place, create_item, sync_legacy_load
    from .world import distance, line_of_sight, position_key

    messages = []
    binding = active_part(state, "quiet binding")
    if binding:
        sound = max(0, sound - 2)
        binding.condition = max(0, binding.condition - 1)
        messages.append("quiet binding muffles the committed attack")
    seal = active_part(state, "resin seal")
    if seal and state.weather in {"hard rain", "coast squall", "forest rain"}:
        seal.condition = max(0, seal.condition - 5)
        messages.append("resin seal keeps the wet string prepared")
    wrap = active_part(state, "ash wrap")
    if wrap and (position_key(state.position) in state.smoke or "smoke-inhalation" in state.terrain_statuses):
        wrap.condition = max(0, wrap.condition - 5)
        messages.append("ash wrap steadies the smoky preparation")
    cord = active_part(state, "retrieval cord")
    if cord and ammunition:
        cord.condition = max(0, cord.condition - 10)
        if state.weapon == "hooked javelin":
            recovered = next((item for item in reversed(state.items) if item.location == "ground" and item.kind == AMMUNITION_ITEMS[ammunition] and item.ground_position == target), None)
        else:
            recovered = create_item(state, AMMUNITION_ITEMS[ammunition], "spent throw tethered by a finite retrieval cord", location="ground")
            recovered.region_id, recovered.ground_position = state.active_region_id, target
        if recovered:
            if distance(state.position, target) <= 4 and line_of_sight(state, state.position, target) and auto_place(state, recovered.id, "pack", owner_id=state.active_courier_id):
                sync_legacy_load(state)
                messages.append("retrieval cord reels the physical throw back into the pack")
            else:
                messages.append("the tethered throw remains on the ground for physical recovery")
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
