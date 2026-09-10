"""Four authored geographic families, generated lazily at their chart moorings."""

from __future__ import annotations

import math

from .content import COMMODITIES, PASSIVES
from .regions import _border, _carve, _grid, _levels, _rect, _road, _signature, region_reachable, validate_region
from .state import ActorSchedule, Contact, Container, MarketEntry, MaterialCell, Position, Region, Threat, VerticalLink, stage_rng
from .work_weapons import WORK_WEAPONS

FRONTIERS = {
    "dunmire": ("Dunmire Peat Isles", 96, 56, "peat", "charcoal", "timber", "fen overtopping", "Yara Silt", "peat steward", "Deren Wick", "causeway keeper"),
    "rillscar": ("Rillscar Iron Gorge", 112, 52, "ironstone", "ironwork", "charcoal", "tailrace fracture", "Halen Crag", "cutworks factor", "Vessa Ore", "bridge witness"),
    "marlbank": ("Marlbank Clay Terraces", 104, 60, "clay", "grain", "timber", "irrigation release", "Mera Loam", "terrace keeper", "Odrin Kiln", "potters' speaker"),
    "frostmere": ("Frostmere Braided Estuary", 108, 58, "gravel", "salt fish", "wool", "ice-channel breakup", "Sarin Rill", "net-house keeper", "Brenna Floe", "winter pilot"),
}

FRONTIER_DISCOVERIES = {
    "dunmire": ("fen sledge", "fire rake tooth", "pitch cup"),
    "rillscar": ("ice awl", "counterbrace pin", "signal mirror"),
    "marlbank": ("limewash seal", "smoke braid", "market weights"),
    "frostmere": ("shingle skids", "salvage tally"),
}

FRONTIER_RELICS = {
    "dunmire": ("flood-mark clasp", "ashglass lens"),
    "rillscar": ("quarry echo pin",),
    "marlbank": ("red-clay seal",),
    "frostmere": ("winter sounding bead", "wreck-light prism"),
}


def _fen(seed, width, height):
    rng = stage_rng(seed, "dunmire:islands")
    ground = _grid(width, height, "~")
    centres = [(14, 28), (29, 17), (49, 13), (71, 20), (82, 39), (57, 44), (31, 43), (51, 29)]
    centres = [(x + rng.randrange(-3, 4), y + rng.randrange(-3, 4)) for x, y in centres]
    for cx, cy in centres:
        rx, ry = rng.randrange(10, 15), rng.randrange(6, 10)
        for y in range(max(1, cy - ry - 2), min(height - 1, cy + ry + 3)):
            for x in range(max(1, cx - rx - 2), min(width - 1, cx + rx + 3)):
                radius = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if radius < .65:
                    ground[y][x] = ";" if rng.randrange(5) else "T"
                elif radius < 1:
                    ground[y][x] = "m"
                elif radius < 1.3 and ground[y][x] == "~":
                    ground[y][x] = ","
    points = [Position(x, y) for x, y in centres]
    _road(ground, points[:7] + points[:1], seed, "dunmire:raised-causeways")
    _road(ground, [points[2], points[7], points[5]], seed, "dunmire:peat-cut")
    return ground, points


def _gorge(seed, width, height):
    rng = stage_rng(seed, "rillscar:folded-rock")
    ground = _grid(width, height, "r")
    bend, phase = rng.randrange(5, 12), rng.randrange(6)
    for y in range(height):
        centre = width // 2 + round(math.sin((y + phase) / 9) * bend)
        for x in range(width):
            gap = abs(x - centre)
            ground[y][x] = "~" if gap < 3 else "q" if gap < 7 else "." if (x + y // 7) % 16 < 10 else "r"
    points = [Position(12, 26), Position(29, 12), Position(57, 10), Position(91, 17), Position(95, 39), Position(66, 42), Position(32, 40)]
    _road(ground, points + points[:1], seed, "rillscar:two-bridges")
    _road(ground, [points[1], points[6]], seed, "rillscar:western-shelf")
    return ground, points


def _terraces(seed, width, height):
    rng = stage_rng(seed, "marlbank:terrace-water")
    ground = _grid(width, height, ".")
    offset = rng.randrange(3, 8)
    for y in range(1, height - 1):
        for x in range(1, width - 1):
            terrace = (y + x // 18 + offset) % 13
            ground[y][x] = "," if terrace == 0 else "m" if terrace < 3 else ";" if terrace < 10 else "."
            if x > 65 and terrace == 11 and rng.randrange(6) == 0:
                ground[y][x] = "T"
    points = [Position(9, 30), Position(24, 17), Position(50, 11), Position(83, 19), Position(88, 43), Position(58, 49), Position(27, 44)]
    _road(ground, points + points[:1], seed, "marlbank:field-circuit")
    _road(ground, [points[1], Position(45, 31), points[4]], seed, "marlbank:potter-road")
    return ground, points


def _estuary(seed, width, height):
    rng = stage_rng(seed, "frostmere:braids")
    ground = _grid(width, height, ".")
    phases = [rng.randrange(12) for _ in range(3)]
    for y in range(height):
        channels = [24 + branch * 27 + round(math.sin((y + phases[branch]) / 7) * 6) for branch in range(3)]
        for x in range(width):
            gap = min(abs(x - centre) for centre in channels)
            ground[y][x] = "~" if gap < 2 else "," if gap < 4 else "r" if gap < 6 else "."
            if gap > 7 and y % 11 == 4 and rng.randrange(4) == 0:
                ground[y][x] = '"'
    points = [Position(10, 29), Position(19, 13), Position(48, 10), Position(81, 15), Position(94, 42), Position(62, 47), Position(26, 43)]
    _road(ground, points + points[:1], seed, "frostmere:paired-crossings")
    _road(ground, [points[2], Position(50, 28), points[5]], seed, "frostmere:gravel-spine")
    return ground, points


def build_frontier(seed: str, region_id: str) -> Region:
    from .inventory import REGIONAL_ARMOUR

    name, width, height, geology, commodity, shortage, process, *_ = FRONTIERS[region_id]
    ground, anchors = {"dunmire": _fen, "rillscar": _gorge, "marlbank": _terraces, "frostmere": _estuary}[region_id](seed, width, height)
    _border(ground, "#")
    below, upper, roof = (_grid(width, height) for _ in range(3))
    landing, settlement, ruin, works, far_bank, store, cave = anchors[:7]
    # The inhabited district has a courtyard, a market frontage, and a separate
    # workshop. Structures are anchored to their landscape, not encounter rooms.
    _rect(ground, settlement.x - 6, settlement.y - 4, settlement.x + 7, settlement.y + 5)
    _rect(ground, settlement.x + 9, settlement.y + 1, settlement.x + 16, settlement.y + 6)
    second = Position(settlement.x + 12, settlement.y + 3)
    _carve(ground, [landing, Position(settlement.x - 6, settlement.y), settlement, second])
    ground[settlement.y][settlement.x - 6] = "+"
    ground[second.y][settlement.x + 9] = "+"

    # Distinct industrial silhouettes: fen drying racks, narrow gorge spans,
    # stepped kiln court, and a long estuary net loft.
    wx, wy = {"dunmire": (7, 3), "rillscar": (4, 6), "marlbank": (9, 5), "frostmere": (10, 3)}[region_id]
    _rect(ground, works.x - wx, works.y - wy, works.x + wx, works.y + wy)
    _carve(ground, [settlement, ruin, Position(works.x - wx, works.y), works, far_bank, store, cave, landing])
    ground[works.y][works.x - wx] = "+"
    _rect(upper, works.x - wx, works.y - wy, works.x + wx, works.y + wy, "=")
    _rect(roof, works.x - 3, works.y - 2, works.x + 3, works.y + 2, "^")
    ground[works.y][works.x] = ">"
    upper[works.y][works.x] = "<"
    roof_ladder = Position(works.x + 1, works.y, 1)
    upper[roof_ladder.y][roof_ladder.x] = ">"
    roof[roof_ladder.y][roof_ladder.x] = "<"
    links = [VerticalLink(works, Position(works.x, works.y, 1), "work stair"), VerticalLink(roof_ladder, Position(roof_ladder.x, roof_ladder.y, 2), "roof ladder")]

    rng = stage_rng(seed, f"{region_id}:excavation")
    underground_points = [Position(cave.x, cave.y), Position(cave.x + 8, cave.y - 5), Position(store.x, store.y - 3), Position(works.x, works.y)]
    _road(below, underground_points, seed, f"{region_id}:buried-drain")
    for point in underground_points:
        for y in range(max(1, point.y - 2), min(height - 1, point.y + 3)):
            for x in range(max(1, point.x - 3), min(width - 1, point.x + 4)):
                if rng.randrange(6) or (x, y) == (point.x, point.y):
                    below[y][x] = "."
    ground[cave.y][cave.x], below[cave.y][cave.x] = "<", ">"
    links.append(VerticalLink(cave, Position(cave.x, cave.y, -1), "excavated drain stair"))
    # A second drain entrance creates an actual underground reconnecting route.
    ground[store.y][store.x], below[store.y][store.x] = "<", ">"
    _carve(below, [Position(store.x, store.y), underground_points[2]])
    links.append(VerticalLink(store, Position(store.x, store.y, -1), "store cellar ladder"))
    objective = Position(works.x + 2, works.y + 1, 1)
    control = Position(works.x - 2, works.y + 1)
    ground[control.y][control.x] = "&"
    upper[objective.y][objective.x] = "R"
    ground[landing.y][landing.x], ground[settlement.y][settlement.x] = "+", "M"
    ground[second.y][second.x] = "c"
    elevated = Position(works.x + 2, works.y, 2)
    sites = [
        ("quay", "Quay tool chest", Position(landing.x + 2, landing.y), None),
        ("ledger", "Witnessed market coffer", Position(settlement.x + 2, settlement.y + 2), None),
        ("ruin", "Old flood-mark store", ruin, None),
        ("bank", "Far-bank cargo cache", far_bank, None),
        ("cellar", "Sunken cellar chest", Position(store.x, store.y, -1), None),
        ("deep", "Buried drain strongbox", Position(cave.x + 8, cave.y - 5, -1), "rope"),
        ("loft", "Loft survey cabinet", Position(works.x - 2, works.y, 1), None),
        ("crown", "Weatherward roof coffer", Position(works.x - 1, works.y, 2), None),
    ]
    rewards = list(PASSIVES)
    stage_rng(seed, f"{region_id}:physical-rewards").shuffle(rewards)
    containers = []
    layers = {-1: below, 0: ground, 1: upper, 2: roof}
    implements = [key for key, definition in WORK_WEAPONS.items() if region_id in definition.regions]
    for index, (suffix, title, point, requirement) in enumerate(sites):
        if point.z == 0:
            _carve(ground, [point, min(anchors, key=lambda anchor: abs(anchor.x - point.x) + abs(anchor.y - point.y))])
        layers[point.z][point.y][point.x] = "C"
        container = Container(f"{region_id}-{suffix}", f"{name.split()[0]} {title}", point, rewards[index], requirement)
        clothing = REGIONAL_ARMOUR[region_id]
        container.extra_rewards = [clothing[index % len(clothing)], "willow dressing" if index % 3 == 0 else "fletched arrows"]
        if index < len(implements):
            container.extra_rewards.append(implements[index])
        if index < len(FRONTIER_DISCOVERIES[region_id]):
            container.extra_rewards.append(FRONTIER_DISCOVERIES[region_id][index])
        if index < len(FRONTIER_RELICS[region_id]):
            container.extra_rewards.append(FRONTIER_RELICS[region_id][index])
        if index in {0, 1, 2}:
            container.extra_rewards.extend([("sealed pitch pot", "sealed lime pot", "sealed brine pot")[index]] * 2)
        containers.append(container)
    levels = _levels(ground, {-1: below, 1: upper, 2: roof})
    landmarks = {"landing": landing, "contact": settlement, "second_contact": second, "settlement": settlement, "ruin": ruin, "works": works, "far_bank": far_bank, "store": store, "cave_entrance": cave, "objective": objective, "control": control, "elevated": elevated}
    region = Region(
        f"{geology.title()} and seasonal water shape {name}.",
        f"The workers send {commodity} downstream but depend on {shortage}.",
        f"{process.title()} threatens the accountable stores.",
        f"Recover two {commodity} lots from the upper works or secure its material control.",
        commodity, shortage, process, width, height, levels, landmarks,
        {"inhabited court": (settlement.x - 6, settlement.y - 4, settlement.x + 16, settlement.y + 6), "old scar": (ruin.x - 6, ruin.y - 5, ruin.x + 6, ruin.y + 5), "industrial works": (works.x - wx, works.y - wy, works.x + wx, works.y + wy), "far shore": (far_bank.x - 6, far_bank.y - 5, far_bank.x + 6, far_bank.y + 5), "buried drain": (cave.x, cave.y - 5, store.x, store.y)},
        links, containers, {}, {}, [], _signature(levels, landmarks),
        id=region_id, name=name, process_name=process,
        process_thresholds=[65, 115, 170],
    )
    # History leaves both a physical hazard and a current market obligation.
    scar = Position(ruin.x + 1, ruin.y)
    region.materials[f"{scar.x},{scar.y},0"] = MaterialCell(material="timber", support=1)
    region.changes["old_flood_obligation"] = shortage
    region.changes["history_scar"] = f"{scar.x},{scar.y},0"
    validate_region(region)
    return region


def ensure_frontier(state, region_id: str) -> None:
    if region_id in state.regions:
        return
    if region_id not in FRONTIERS:
        raise ValueError("unknown regional mooring")
    region = build_frontier(state.seed, region_id)
    spec = FRONTIERS[region_id]
    contacts = [Contact(f"{region_id}-contact-{index + 1}", spec[7 + index * 2], spec[8 + index * 2], 0, [f"The old flood left a {spec[5]} obligation at the raised store."], spec[5], region_id, region.landmarks["contact" if index == 0 else "second_contact"]) for index in range(2)]
    market = {name: MarketEntry(2, 1) for name in COMMODITIES}
    market[spec[4]] = MarketEntry(1, 3)
    market[spec[5]] = MarketEntry(0, 4)
    from .encounters import frontier_population

    actors = frontier_population(state.seed, region)
    from .frontier_elites import install_elite
    install_elite(state.seed, region, actors)
    state.regions[region_id], state.contacts[region_id] = region, contacts
    state.region_threats[region_id], state.regional_markets[region_id] = actors, market
    for contact in contacts:
        area = f"region:{region_id}"
        state.actor_schedules[contact.id] = ActorSchedule(contact.id, area, contact.position, "working", state.world_time + 16, area, contact.position, last_update=state.world_time)
    from .quests import initialise_quests
    initialise_quests(state)
    from .aftermath import initialise_aftermath
    from .enemy_equipment import initialise_enemy_equipment

    initialise_aftermath(state)
    initialise_enemy_equipment(state, fresh=True)
    from .regional_history import initialise_account, reconcile_network

    initialise_account(state, region_id, new_geography=True)
    reconcile_network(state)


def frontier_process(state) -> list[str]:
    """One persistent landscape intervention, never an endlessly refilled hazard."""
    region = state.region
    if region.process_stage == 1:
        return [{
            "dunmire": "Water laps the drying piles; the raised circuit stays above the peat cut.",
            "rillscar": "The tailrace jars its timber support; two bridges give different approaches.",
            "marlbank": "Kiln steam dries the upper seed beds; the field release will soon be needed.",
            "frostmere": "Ice plates knock against the net loft; pilots mark the sheltered channel.",
        }[region.id]]
    if region.changes.get("frontier_process_applied") or region.changes.get("environment_control_used"):
        return ["The recorded material work holds; the alternate circuit remains available."]
    region.changes["frontier_process_applied"] = True
    point = region.landmarks["ruin"]
    # Hazards use the same sparse cells as courier handling. The container itself
    # and the critical circuit are excluded; damage can be treated permanently.
    for dx in (1, 2, 3):
        cell = MaterialCell(material="soil")
        if region.id == "dunmire":
            cell.water = 2
        elif region.id == "rillscar":
            cell.material, cell.support, cell.collapse_due = "timber", 0, state.world_time + 3
        elif region.id == "marlbank":
            cell.material, cell.fire, cell.fuel, cell.smoke = "charcoal", 1, 3, 2
        else:
            cell.water, cell.ice, cell.fluid = 1, True, "salt"
        region.materials[f"{point.x + dx},{point.y},0"] = cell
    return [{
        "dunmire": "Peat water overtops the old store bank. The raised circuit and buried drain remain.",
        "rillscar": "The old tailrace piles bend: debris will fall in three actions. Both bridges remain.",
        "marlbank": "The unattended charcoal bed flares beside the flood-mark store; smoke travels with the wind.",
        "frostmere": "Shallow sheets gather by the abandoned net store; thaw and load now change footing.",
    }[region.id]]


def control_frontier(state) -> str:
    region = state.region
    region.changes["environment_control_used"] = True
    point = region.landmarks["ruin"]
    for dx in (1, 2, 3):
        cell = region.materials.get(f"{point.x + dx},{point.y},0")
        if cell:
            cell.water = 0
            cell.fire = cell.smoke = 0
            cell.support, cell.collapse_due, cell.ice = 3, 0, False
    return {
        "dunmire": "You open the drying-bank spill. Peat water drains; the winter fuel account needs a new settlement.",
        "rillscar": "You tension the tailrace brace. The warned supports hold, protecting both bridge approaches.",
        "marlbank": "You send the kiln release along the seed ditch. Fire and smoke subside, but the firing loses heat.",
        "frostmere": "You swing the ice boom clear of the net loft. Load can move safely under the marked sounding.",
    }[region.id]


def settle_frontier_claim(state, choice: str) -> str:
    region = state.region
    primary, secondary = state.contacts[region.id]
    market = state.market[region.objective_commodity]
    if region.id == "dunmire":
        if choice == "b":
            control_frontier(state)
            market.stock = max(0, market.stock - 1)
            state.market["charcoal"].demand += 2
            for actor in state.threats:
                if actor.role == "territorial":
                    actor.status = "retreated"
            text = "The breached drying bank gives the bank animals a refuge and spares the inhabited islands; winter fuel grows dearer."
        else:
            region.changes["drying_bank_held"] = True
            market.stock += 3
            state.market["timber"].demand += 2
            text = "The bank holds the winter fuel, but its timber obligation grows and bank animals keep the far shore."
    elif region.id == "rillscar":
        control_frontier(state)
        if choice == "o":
            for actor in state.threats:
                if actor.role == "protector":
                    actor.status = "negotiated"
            market.demand = max(0, market.demand - 2)
            text = "Public tailrace access retires the bridge guard; cutworkers lose a private iron premium."
        else:
            state.trade_credit += 3
            market.stock += 2
            state.market["charcoal"].demand += 1
            text = "Two signed load accounts keep the bridge ward employed and release iron, with a fuel debt to the cutworkers."
    elif region.id == "marlbank":
        if choice == "f":
            control_frontier(state)
            market.stock += 3
            state.market["charcoal"].stock = max(0, state.market["charcoal"].stock - 1)
            text = "Water returns to the seed terraces; grain recovers while the potters lose a kiln firing."
        else:
            region.changes["kiln_claim"] = True
            state.market["grain"].demand += 2
            state.trade_credit += 3
            text = "The pottery account pays Jomon, but rationed fields keep grain scarce and the old firing bed active."
    else:
        control_frontier(state)
        edge = next(edge for edge in state.route_edges if edge.id == "c-f")
        if choice == "l":
            edge.weather_exposure, edge.travel_time = 1, 8
            text = "Marked lee soundings lower voyage exposure but lengthen every future approach to the net houses."
        else:
            direct = next(edge for edge in state.route_edges if edge.id == "g-f")
            direct.closed_seasons = []
            direct.integrity_required = 7
            market.stock += 2
            text = "The witnessed ice cut stays charted in winter for a sound hull; pilots accept greater cargo exposure."
    region.changes[f"claim:{choice}"] = True
    primary.disposition = min(3, primary.disposition + 1)
    secondary.disposition = max(-3, min(3, secondary.disposition + (2 if choice in {"b", "o", "f", "l"} else -1)))
    return text
