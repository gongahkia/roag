"""Bounded route-leg travel and three sporadic aboard-Jomon encounters."""

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
        if forced not in {"raiders", "creature", "lure"}:
            raise ValueError("unknown forced voyage family")
        return forced
    edge = edge_between(state, state.route_current_node, destination)
    exposure = edge.cargo_risk + edge.weather_exposure if edge else 2
    stage = f"voyage:{state.travel_count + 1}:{state.route_current_node}:{destination}:{state.weather}"
    rng = stage_rng(state.seed, stage)
    chance = 1 + min(3, sum(stack.quantity for stack in state.vessel_cargo.values()) // 4) + exposure // 3
    if rng.randrange(12) >= chance:
        return None
    # The lure is intentionally rare; the other families share ordinary voyages.
    roll = rng.randrange(12)
    return "lure" if roll == 0 else "creature" if roll < 5 else "raiders"


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
        state.voyage_detail = {
            "raiders": "Hooked skiffs close on the cargo rail; the boarders intend theft and escape.",
            "creature": "A broad-backed river grazer strikes the rudder while the shoal narrows.",
            "lure": "A finite mineral resonance under the hull makes familiar voices seem to call from the wrong bank.",
        }[event]
        state.add_message(state.voyage_detail, priority=3)
        return True, state.voyage_detail
    _finish_travel(state, "The voyage remains watchful but uneventful.")
    return True, f"Jomon reaches {state.route_nodes[destination].name} after {edge.travel_time} action-clock measures."


def _finish_travel(state: GameState, consequence: str) -> None:
    destination = state.pending_destination
    if destination is None:
        return
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
            from .state import CommodityStack

            grain = state.vessel_cargo.setdefault("grain", CommodityStack(0, "dry"))
            grain.quantity += 1
    state.pending_destination = None
    state.voyage_status = "resolved" if state.voyage_kind else "none"
    state.voyage_detail = consequence
    state.remember(f"Voyage to {node.name}: {consequence}")
    state.add_message(f"{consequence} Jomon makes {node.name}.", priority=3)


def _lose_vessel_cargo(state: GameState) -> str:
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
    success = False
    consequence = ""
    if state.voyage_kind == "raiders":
        if response == "repel" and state.weapon in {"pike", "billhook", "crossbow", "longbow", "staff"}:
            success, consequence = True, "readied reach drives the cargo thieves back before they can disengage"
        elif response == "distract" and (state.gear in {"smoke pot", "trade seals"} or state.support == "factor surety"):
            success, consequence = True, "a material decoy draws the skiffs away from the accountable hold"
        elif response == "yield":
            success, consequence = True, _lose_vessel_cargo(state) + "; the thieves escape without pressing the crew"
        else:
            consequence = _lose_vessel_cargo(state) + "; an exposed crew member suffers a cut arm"
            if state.courier:
                state.courier.health = max(2, state.courier.health - 2)
                state.courier.injury = "cut arm"
                state.courier.injuries["arms"] = "cut arm"
    elif state.voyage_kind == "creature":
        if response == "repel" and state.weapon in {"pike", "spear", "billhook", "javelins"}:
            success, consequence = True, "spaced strikes turn the animal from the rudder without pursuit"
        elif response == "evade" and (state.support == "route survey" or (state.courier and state.courier.technique in {"ebb reader", "sure footing"})):
            success, consequence = True, "the pilot crosses the narrow shoal before the animal can brace"
        elif response == "bait" and state.vessel_cargo.get("salt fish"):
            state.vessel_cargo["salt fish"].quantity -= 1
            if state.vessel_cargo["salt fish"].quantity == 0:
                del state.vessel_cargo["salt fish"]
            success, consequence = True, "one salt-fish lot draws the territorial animal clear"
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
        elif response == "navigate" and (state.support == "route survey" or (state.courier and state.courier.role == "pilot")):
            success, consequence = True, "lead line and chart hold a material course through the lure"
        else:
            consequence = "the lure costs six more action-clock measures and leaves the courier disoriented"
            from .actions import _advance_world

            _advance_world(state, steps=6)
            if state.courier:
                state.courier.injury = "ringing head"
                state.courier.injuries["head"] = "ringing head"
    from .actions import _advance_world

    _advance_world(state)
    state.voyage_status = "resolved"
    family = state.voyage_kind
    _finish_travel(state, consequence)
    state.voyage_kind = None
    state.remember(f"Jomon resolved a {family} voyage by {response}; success {success}.")
    return True, consequence
