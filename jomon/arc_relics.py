"""Four finite rule-breaking tools earned from the two aftermath arcs."""

from __future__ import annotations


ARC_RELICS = {
    ("repairs", "m"): "common-work rivet",
    ("repairs", "j"): "counterclaim lodestone",
    ("refuges", "p"): "lee-cloth brooch",
    ("refuges", "c"): "channel-surety shuttle",
}

ARC_RELIC_DESCRIPTIONS = {
    "common-work rivet": "A finite master rivet repairs worn equipment and nearby supports together, but the resisting work leaves the bearer fatigued.",
    "counterclaim lodestone": "A strange ironstone strips nearby human weapons into one recoverable pile—including the courier's readied weapon—and rings an alarm.",
    "lee-cloth brooch": "A finite sailcloth clasp makes an eight-action lee against storm movement, sight and spoiled aim; its snap alerts nearby listeners.",
    "channel-surety shuttle": "A weighted line carries the courier over a straight marked water lane; the heaviest cargo stays at the launch and the landing is loud.",
}


def lee_sheltered(state) -> bool:
    changes = state.region.changes if state.location == "region" else state.vessel_changes
    return int(changes.get(f"lee-shelter-until:{state.spatial_id}", 0)) > state.world_time


def _spend(state, name: str) -> None:
    from .inventory import consume_carried

    state.relics[name] -= 1
    if not state.relics[name]:
        del state.relics[name]
    state.carried_relic = None
    if not consume_carried(state, f"relic:{name}"):
        raise RuntimeError("selected arc relic has no matching physical item")


def _channel_lane(state):
    from .materials import fields, key
    from .state import Position
    from .world import base_tile, is_walkable

    def watery(point):
        cell = fields(state).get(key(point))
        return bool(cell and cell.water) or key(point) in state.water or base_tile(state, point) in {"~", ",", "w"}

    lanes = []
    for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)):
        water = []
        for step in range(1, 6):
            point = Position(state.position.x + dx * step, state.position.y + dy * step, state.position.z)
            if watery(point) and step <= 4:
                water.append(point)
                continue
            if len(water) >= 2 and is_walkable(state, point, ignore_threat=True):
                lanes.append((len(water), dx, dy, point, tuple(water)))
            break
    return max(lanes, key=lambda lane: (lane[0], lane[1], lane[2]), default=None)


def use_arc_relic(state, name: str) -> tuple[bool, str]:
    """Apply one selected relic; callers own the ordinary action-clock advance."""
    if state.carried_relic != name or not state.relics.get(name):
        return False, "That finite arc relic is not physically selected."
    from .actions import add_status, emit_sound
    from .materials import fields, point_at
    from .world import distance

    if name == "common-work rivet":
        equipment = [
            item for item in state.items
            if item.owner_id == state.active_courier_id
            and item.location in {"readied", "secondary", "head", "torso", "arms", "hands", "legs", "feet"}
            and item.condition < 100
        ][:6]
        supports = [
            cell for coordinate, cell in fields(state).items()
            if distance(state.position, point_at(coordinate)) <= 5
            and cell.material in {"timber", "stone"}
            and (cell.support < 3 or cell.collapse_due)
        ][:3]
        if not equipment and not supports:
            return False, "No worn physical equipment or nearby damaged support can take the common-work rivet; it remains whole."
        for item in equipment:
            item.condition = min(100, item.condition + 20)
        for cell in supports:
            cell.support = min(3, cell.support + 1)
            cell.collapse_due = 0
        _spend(state, name)
        add_status(state, "fatigued", "the common-work rivet's resisting repair", 3, "movement may take another action; rest or wait it out")
        return True, f"The master rivet repairs {len(equipment)} worn items and seats {len(supports)} supports; shared resistance leaves three-action fatigue."

    if name == "counterclaim lodestone":
        from .enemy_equipment import readied_weapon
        from .inventory import equipped_item, sync_legacy_load

        targets = [
            actor for actor in state.combatants
            if actor.profile not in {"animal", "machinery"}
            and actor.status in {"watching", "engaged"}
            and distance(state.position, actor.position) <= 8
            and readied_weapon(state, actor)
        ][:3]
        if not targets:
            return False, "No nearby embodied claimant carries a physical weapon; the lodestone remains quiet."
        origin = state.position
        dropped = 0
        for actor in targets:
            weapon = readied_weapon(state, actor)
            weapon.location, weapon.owner_id = "ground", None
            weapon.region_id, weapon.ground_position = state.spatial_id, origin
            actor.morale -= 1
            actor.intent = "disarmed into the lodestone pile; deciding whether to recover or withdraw"
            dropped += 1
        own = equipped_item(state, "readied")
        if own:
            own.location, own.owner_id = "ground", None
            own.region_id, own.ground_position = state.spatial_id, origin
            dropped += 1
        sync_legacy_load(state)
        _spend(state, name)
        sounds = emit_sound(state, 6)
        return True, " ".join([
            f"The lodestone strips {dropped} physical weapons into one recoverable pile, including the courier's; every nearby claimant hears the iron report.",
            *sounds,
        ])

    if name == "lee-cloth brooch":
        if state.weather not in {"river fog", "hard rain", "coast squall", "forest rain", "crosswind", "ridge gust", "salt wind"}:
            return False, "No present wind, rain or fog can fill the lee cloth; the brooch remains fastened."
        changes = state.region.changes if state.location == "region" else state.vessel_changes
        changes[f"lee-shelter-until:{state.spatial_id}"] = state.world_time + 9
        _spend(state, name)
        sounds = emit_sound(state, 3)
        return True, " ".join([
            "The clasp raises an eight-action local lee: storm movement, sight and prepared aim remain usable, while the snapping cloth reports the shelter.",
            *sounds,
        ])

    lane = _channel_lane(state)
    if state.location != "region" or lane is None:
        return False, "The shuttle needs a straight lane of two to four marked water cells and a dry landing; it remains wound."
    origin = state.position
    _, _, _, landing, water = lane
    cargo = [
        item for item in state.items
        if item.owner_id == state.active_courier_id and item.location == "pack"
        and item.kind.startswith("commodity:")
    ]
    dropped = max(cargo, key=lambda item: (item.quantity, item.kind), default=None)
    if dropped:
        dropped.location, dropped.owner_id = "ground", None
        dropped.region_id, dropped.ground_position = state.spatial_id, origin
    state.position = landing
    state.aimed_target = None
    from .inventory import sync_legacy_load

    sync_legacy_load(state)
    _spend(state, name)
    sounds = emit_sound(state, 5, landing)
    cargo_text = f"; {dropped.kind.split(':', 1)[1]} remains physically at the launch" if dropped else "; no cargo was carried to pay the surety"
    return True, " ".join([
        f"The shuttle crosses {len(water)} water cells to {landing.x},{landing.y}{cargo_text}. The landing line rings.",
        *sounds,
    ])


def grant_arc_relic(state, arc_id: str, choice: str) -> str:
    from .inventory import auto_place, create_item, record_acquisition

    name = ARC_RELICS[(arc_id, choice)]
    item = create_item(state, f"relic:{name}", f"{arc_id} arc ending {choice}")
    if auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        record_acquisition(state, item)
        where = "packed and selected if no other relic is readied"
    else:
        item.location, item.region_id, item.ground_position = "ground", state.spatial_id, state.position
        where = "left physically beside the final witness"
    state.remember(f"{state.courier.name} received {name} from the {arc_id} ending; {where}.")
    return f" The final witness issues {name}, {where}."


def validate_arc_relics() -> None:
    if set(ARC_RELICS) != {("repairs", "m"), ("repairs", "j"), ("refuges", "p"), ("refuges", "c")}:
        raise ValueError("the two aftermath arcs need four explicit ending relics")
    if set(ARC_RELICS.values()) != set(ARC_RELIC_DESCRIPTIONS):
        raise ValueError("every arc relic needs one distinct production ending and description")


validate_arc_relics()
