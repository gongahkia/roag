"""Bounded route-leg travel and sporadic aboard-Jomon encounters."""

from __future__ import annotations

from dataclasses import dataclass

from .regions import activate_region, store_active_region
from .state import GameState, stage_rng
from .route_chart import edge_between, route_availability


DESTINATIONS = ("hearthford", "greywash", "greenwold", "whitecairn")


@dataclass(frozen=True)
class TravelFrame:
    step: int
    total: int
    x: int
    y: int
    wake: str
    text: str
    interrupted: bool = False


def travel_animation_frames(
    state: GameState,
    origin: str,
    destination: str,
    *,
    interrupted: bool = False,
) -> list[TravelFrame]:
    """Return presentation frames without mutating the world or reading a clock."""
    first, second = state.route_nodes[origin], state.route_nodes[destination]
    count = max(4, min(8, max(abs(second.x - first.x), abs(second.y - first.y)) // 3))
    frames = []
    for step in range(count + 1):
        x = round(first.x + (second.x - first.x) * step / count)
        y = round(first.y + (second.y - first.y) * step / count)
        paused = interrupted and step == max(1, count // 2)
        frames.append(TravelFrame(
            step, count, x, y, "~" * min(3, step),
            f"Jomon {'departs '+first.name if step == 0 else 'reaches '+second.name if step == count else 'works the '+(edge_between(state, origin, destination).hazard if edge_between(state, origin, destination) else 'route')}",
            paused,
        ))
        if paused:
            break
    return frames


def voyage_for(
    state: GameState,
    destination: str,
    *,
    forced: str | None = None,
) -> str | None:
    """Select one inspectable event from seed, route, weather, cargo, and history."""
    if forced is not None:
        from .ship_crises import VOYAGES
        if forced not in VOYAGES:
            raise ValueError("unknown forced voyage family")
        return forced
    edge = edge_between(state, state.route_current_node, destination)
    exposure = edge.cargo_risk + edge.weather_exposure if edge else 2
    from .calendar import calendar_at
    from .vessel_refits import installed

    if calendar_at(state).season == "winter" and installed(state, "winter-hatch-felt"):
        exposure = max(0, exposure - 1)
    stage = f"voyage:{state.travel_count + 1}:{state.route_current_node}:{destination}:{state.weather}"
    rng = stage_rng(state.seed, stage)
    chance = 1 + min(3, sum(stack.quantity for stack in state.vessel_cargo.values()) // 4) + exposure // 3
    if rng.randrange(12) >= chance:
        return None
    # The lure is intentionally rare; the other families share ordinary voyages.
    roll = rng.randrange(12)
    if roll == 0 or (roll == 1 and installed(state, "sounding-keel-shoes")):
        return "lure"
    if state.travel_count == 0:
        return "creature" if roll < 5 else "raiders"
    pool = ["raiders", "creature", "boarders", "shoal", "driftwood", "inspection"]
    if state.vessel_cargo:
        pool += ["hold-thieves", "galley-fire"]
    if state.vessel_integrity < 8:
        pool += ["split-seam", "flooded-hold"]
    if edge and edge.weather_exposure >= 2:
        pool += ["storm", "flooded-hold", "split-seam"]
    return rng.choice(pool)


def choose_destination(
    state: GameState,
    destination: str,
    *,
    forced_voyage: str | None = None,
) -> tuple[bool, str]:
    """Compatibility name: normal play now confirms one adjacent route leg."""
    if state.location != "jomon" or destination not in state.route_nodes:
        return False, "That destination cannot be set from here."
    if state.voyage_status == "active":
        return False, "Resolve the current voyage danger before changing course."
    if destination == state.route_current_node:
        return False, f"Jomon is already at {state.route_nodes[destination].name}."
    available, reason = route_availability(state, destination)
    if not available:
        return False, reason
    edge = edge_between(state, state.route_current_node, destination)
    assert edge is not None
    store_active_region(state)
    event = voyage_for(state, destination, forced=forced_voyage)
    from .voyage_variants import select_variant

    variant = select_variant(state, event, destination) if event else None
    state.travel_count += 1
    from .actions import _advance_world

    _advance_world(state, steps=edge.travel_time)
    state.pending_destination = destination
    if edge.supply_cost >= 2 and state.vessel_cargo.get("grain"):
        state.vessel_cargo["grain"].quantity -= 1
        if state.vessel_cargo["grain"].quantity <= 0:
            del state.vessel_cargo["grain"]
    if event:
        state.voyage_kind = event
        state.voyage_status = "active"
        from .ship_crises import VOYAGES
        if variant:
            state.vessel_changes["active_voyage_variant"] = variant.id
        else:
            state.vessel_changes.pop("active_voyage_variant", None)
        state.voyage_detail = VOYAGES[event][1]
        if variant:
            state.voyage_detail += f" VARIANT — {variant.name}: {variant.cause} {variant.effect}"
        state.add_message(state.voyage_detail, priority=3)
        return True, state.voyage_detail
    state.vessel_changes.pop("active_voyage_variant", None)
    _finish_travel(state, "The voyage remains watchful but uneventful.")
    return True, f"Jomon reaches {state.route_nodes[destination].name} after {edge.travel_time} action-clock measures."


def _finish_travel(state: GameState, consequence: str) -> None:
    destination = state.pending_destination
    if destination is None:
        return
    from .voyage_variants import record_variant_outcome

    record_variant_outcome(state, consequence)
    origin = state.route_current_node
    edge = edge_between(state, origin, destination)
    state.route_current_node = destination
    if destination not in state.route_known:
        state.route_known.append(destination)
        state.route_known.sort()
    if edge and edge.id not in state.traversed_route_edges:
        state.traversed_route_edges.append(edge.id)
    node = state.route_nodes[destination]
    if node.region_id:
        activate_region(state, node.region_id)
        state.vessel_changes.pop("intermediate_mooring", None)
    else:
        state.vessel_changes["intermediate_mooring"] = destination
        visits = int(state.vessel_changes.get(f"visits:{destination}", 0)) + 1
        state.vessel_changes[f"visits:{destination}"] = visits
        if node.supply and visits == 1:
            state.vessel_changes[f"supply_available:{destination}"] = node.supply
    state.pending_destination = None
    state.voyage_status = "resolved" if state.voyage_kind else "none"
    state.voyage_detail = consequence
    state.remember(f"Voyage to {node.name}: {consequence}")
    state.add_message(f"{consequence} Jomon makes {node.name}.", priority=3)


def _lose_vessel_cargo(state: GameState) -> str:
    from .vessel_refits import installed

    net_marker = f"refit-net-catch:{state.travel_count}"
    if installed(state, "cargo-rail-netting") and not state.vessel_changes.get(net_marker):
        state.vessel_changes[net_marker] = True
        return "fitted cargo-rail netting catches the first loose lot"
    available = [name for name, stack in state.vessel_cargo.items() if stack.quantity]
    if not available:
        return "The hold is too bare to yield material cargo."
    name = sorted(available)[0]
    state.vessel_cargo[name].quantity -= 1
    if state.vessel_cargo[name].quantity == 0:
        del state.vessel_cargo[name]
    return f"one {name} lot is lost"


def resolve_voyage(state: GameState, response: str) -> tuple[bool, str]:
    """Resolve a whole aboard crisis as one accepted, time-bearing action."""
    if state.location != "jomon" or state.voyage_status != "active" or not state.voyage_kind:
        return False, "No voyage crisis is active."
    response = response.lower()
    from .ship_crises import TACTICAL, begin_deck, abandon_deck, _finish
    from .vessel_refits import installed
    from .voyage_variants import active_variant

    variant = active_variant(state, state.voyage_kind)
    if response == "deck":
        return begin_deck(state)
    if state.vessel_changes.get("deck_crisis"):
        if response != "yield":
            return False, "Continue the physical deck action or explicitly withdraw."
        consequence = abandon_deck(state)
        from .actions import _advance_world
        _advance_world(state)
        return True, consequence
    if state.voyage_kind not in {"raiders", "creature", "lure"}:
        from .actions import _advance_world
        kind, cost = state.voyage_kind, 1
        if kind in TACTICAL and response == "yield":
            _advance_world(state)
            return True, abandon_deck(state)
        if kind == "boarders" and response == "counsel" and variant:
            account = next((row for row in sorted(state.institutions.values(), key=lambda row: row.id) if row.obligation > 0), None)
            if account is None:
                return False, "The named obligation was settled before the claimant could present it."
            account.obligation -= 1
            account.witnessed_acts.append(f"Jomon settled a boarding claim on voyage {state.travel_count}")
            del account.witnessed_acts[:-12]
            cost, consequence = 2, f"{account.name} accepts one recorded obligation; the boarding company breaks off without taking cargo."
        elif kind == "shoal" and response == "navigate":
            cost = (3 if installed(state, "sounding-keel-shoes") else 4) + (2 if variant else 0)
            consequence = f"Soundings find a slower silt channel; {cost} measured actions preserve the hull."
        elif kind == "shoal" and response == "yield":
            damage = (1 if installed(state, "sounding-keel-shoes") else 2) + int(bool(variant))
            state.vessel_integrity = max(1, state.vessel_integrity - damage)
            consequence = f"The forced shoal passage scrapes {damage} integrity from the hull."
        elif kind == "driftwood" and response == "repel" and state.rope_uses > 0:
            from .state import CommodityStack
            state.rope_uses -= 1
            state.vessel_cargo.setdefault("timber", CommodityStack(0, "wet")).quantity += 1
            if variant:
                state.vessel_cargo.setdefault("charcoal", CommodityStack(0, "dry but fire-marked")).quantity += 1
            cost = 4 if variant else 3
            consequence = "One rope arrangement catches one wet timber lot" + (" and one fire-marked charcoal lot" if variant else "") + " from the broken raft."
        elif kind == "driftwood" and response == "yield":
            consequence = "The household lets the broken raft pass without claiming its cargo."
        elif kind == "inspection" and response == "navigate":
            signal = installed(state, "signal-mast-shutter")
            threshold = (1 if signal else 2) + int(bool(variant))
            trusted = any(account.trust >= threshold for account in state.institutions.values())
            paper = state.vessel_cargo.get("paper")
            if not trusted and (not paper or paper.quantity <= 0):
                return False, f"Inspection needs {threshold} institutional trust or one counted paper lot."
            if not trusted:
                paper.quantity -= 1
                if not paper.quantity:
                    del state.vessel_cargo["paper"]
            consequence = "Witnessed working claims satisfy the inspection; its memory follows this route."
            if signal:
                consequence += " The fitted signal shutter supplies the named visible answer."
            state.vessel_changes["inspection_witnessed"] = True
        elif kind == "inspection" and response == "counsel" and state.trade_credit >= (3 if variant else 2):
            payment = 3 if variant else 2
            state.trade_credit -= payment
            consequence = f"The patrol records {payment} credit against its inspection account."
        elif kind == "inspection" and response == "yield":
            consequence = "Refused inspection: " + _lose_vessel_cargo(state)
        else:
            return False, "That response lacks the disclosed material or does not address this voyage."
        _advance_world(state, steps=cost)
        _finish(state, consequence)
        return True, consequence
    success = False
    consequence = ""
    if state.voyage_kind == "raiders":
        if response == "repel" and state.weapon in {"pike", "billhook", "crossbow", "longbow", "staff"}:
            success, consequence = True, "readied reach drives the cargo thieves back before they can disengage"
        elif response == "distract" and (state.gear in {"smoke pot", "trade seals"} or state.support == "factor surety"):
            success, consequence = True, "a material decoy draws the skiffs away from the accountable hold"
        elif response == "yield":
            success, consequence = True, _lose_vessel_cargo(state) + "; the thieves escape without pressing the crew"
            if variant:
                consequence += "; " + _lose_vessel_cargo(state)
        else:
            consequence = _lose_vessel_cargo(state) + "; an exposed crew member suffers a cut arm"
            if variant:
                consequence += "; " + _lose_vessel_cargo(state)
            if state.courier:
                state.courier.health = max(2, state.courier.health - 2)
                state.courier.injury = "cut arm"
                state.courier.injuries["arms"] = "cut arm"
    elif state.voyage_kind == "creature":
        if response == "repel" and state.weapon in {"pike", "spear", "billhook", "javelins"}:
            success, consequence = True, "spaced strikes turn the animal from the rudder without pursuit"
        elif response == "evade" and (state.support == "route survey" or (state.courier and state.courier.technique in {"ebb reader", "sure footing"})):
            success, consequence = True, "the pilot crosses the narrow shoal before the animal can brace"
        elif response == "bait" and state.vessel_cargo.get("salt fish") and state.vessel_cargo["salt fish"].quantity >= (2 if variant else 1):
            required = 2 if variant else 1
            state.vessel_cargo["salt fish"].quantity -= required
            if state.vessel_cargo["salt fish"].quantity == 0:
                del state.vessel_cargo["salt fish"]
            success, consequence = True, f"{required} salt-fish lot{'s' if required > 1 else ''} draw the territorial {'pair' if variant else 'animal'} clear"
        else:
            consequence = "the household endures the strike; a timber lot or two health is lost"
            if state.vessel_cargo.get("timber"):
                state.vessel_cargo["timber"].quantity -= 1
            elif state.courier:
                state.courier.health = max(2, state.courier.health - 2)
    else:
        if response == "anchor" and (state.support == "carpenter rig" or (state.courier and state.courier.role == "carpenter")):
            success, consequence = True, "measured anchor chain fixes the real bank while the resonance passes"
        elif response == "counsel" and (state.support == "factor surety" or (state.courier and state.courier.role in {"factor", "healer"})):
            success, consequence = True, "named crew answer one another until the false familiar voices lose force"
        elif response == "navigate" and (state.support == "route survey" or (state.courier and state.courier.role == "pilot") or installed(state, "signal-mast-shutter")):
            success, consequence = True, "lead line and chart hold a material course through the lure"
            if installed(state, "signal-mast-shutter"):
                consequence += "; the shutter answers with Jomon's named signal"
                if variant:
                    state.vessel_changes[f"signal_account:{state.travel_count}"] = True
        else:
            consequence = "the lure costs six more action-clock measures and leaves the courier disoriented"
            from .actions import _advance_world

            _advance_world(state, steps=6)
            if state.courier:
                state.courier.injury = "ringing head"
                state.courier.injuries["head"] = "ringing head"
    from .actions import _advance_world

    if variant and state.voyage_kind == "lure" and response == "navigate" and success:
        _advance_world(state)
    _advance_world(state)
    state.voyage_status = "resolved"
    family = state.voyage_kind
    _finish_travel(state, consequence)
    state.voyage_kind = None
    state.remember(f"Jomon resolved a {family} voyage by {response}; success {success}.")
    return True, consequence
