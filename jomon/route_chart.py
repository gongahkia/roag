"""Authored seed-varied route graph and inspectable leg consequences."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .calendar import calendar_at, seasonal_route_note
from .catalog import VESSEL_SECTIONS, load_catalog
from .state import GameState, RouteEdge, RouteNode, stage_rng


_ROUTES = load_catalog("vessel.json", VESSEL_SECTIONS)
REGION_NODES = dict(_ROUTES["region_nodes"])


def build_route_graph(seed: str) -> tuple[dict[str, RouteNode], list[RouteEdge]]:
    """Build one bounded navigational network; optional links vary by seed."""
    nodes = {node_id: RouteNode(**row) for node_id, row in _ROUTES["route_nodes"].items()}
    specifications = list(_ROUTES["route_edges"])
    rng = stage_rng(seed, "route-links")
    optional = _ROUTES["optional_route_edges"]
    specifications.extend(row for row in optional if rng.randrange(3) != 0)
    edges = [
        RouteEdge(edge_id, first, second, time, supply, cargo, weather, max(0, cargo + weather - 2), list(closed), hazard)
        for edge_id, first, second, time, supply, cargo, weather, closed, hazard in specifications
    ]
    return nodes, edges


def extend_route_chart(state: GameState) -> None:
    """Add only expansion moorings and legs; preserve surveyed and changed edges."""
    nodes, edges = build_route_graph(state.seed)
    for node_id, node in nodes.items():
        state.route_nodes.setdefault(node_id, node)
    existing = {edge.id for edge in state.route_edges}
    expansion = {"h-d", "d-w", "h-a", "a-r", "u-i", "k-i", "c-f", "g-f", "s-c"}
    state.route_edges.extend(edge for edge in edges if edge.id in expansion - existing)


def initialise_route_chart(state: GameState) -> None:
    nodes, edges = build_route_graph(state.seed)
    state.route_nodes, state.route_edges = nodes, edges
    state.route_current_node = state.active_region_id
    state.route_known = sorted({"hearthford", "reed-anchor", "charter-market", "willow-ferry", state.active_region_id})
    state.traversed_route_edges = list(dict.fromkeys(state.traversed_route_edges))


def edge_between(state: GameState, first: str, second: str) -> RouteEdge | None:
    return next((edge for edge in state.route_edges if {edge.first, edge.second} == {first, second}), None)


def leg_travel_time(state: GameState, edge: RouteEdge) -> int:
    from .character import effective_competency

    guide = state.courier
    return max(1, edge.travel_time - (min(2, effective_competency(guide, "wayfinding") // 5) if guide else 0)
               - int(bool(guide and "deep-pilotage" in guide.skill_nodes)))


def neighbours(state: GameState, node_id: str, *, reachable_only: bool = False) -> list[str]:
    result = []
    for edge in state.route_edges:
        if edge.first == node_id:
            result.append(edge.second)
        elif edge.second == node_id:
            result.append(edge.first)
    if reachable_only:
        result = [node for node in result if route_availability(state, node)[0]]
    return sorted(result)


def route_availability(state: GameState, destination: str) -> tuple[bool, str]:
    edge = edge_between(state, state.route_current_node, destination)
    if edge is None:
        return False, "No charted leg joins those nodes."
    season = calendar_at(state).season
    if season in edge.closed_seasons:
        return False, f"{edge.hazard.title()} is closed in {season}."
    if state.vessel_integrity < edge.integrity_required:
        return False, f"Jomon needs integrity {edge.integrity_required}; current {state.vessel_integrity}."
    return True, "reachable"


def route_preview(state: GameState, destination: str) -> list[str]:
    node = state.route_nodes[destination]
    if destination == state.route_current_node:
        market = f"Market interest: {node.market_interest}." if node.market_interest else "No known cargo buyer."
        return [
            f"{node.name}: {node.description}",
            "Jomon is moored here; choose a connected node to preview a leg.",
            f"Season: {calendar_at(state).season}; {seasonal_route_note(state)}.",
            market,
        ]
    edge = edge_between(state, state.route_current_node, destination)
    if edge is None:
        return ["No direct charted leg."]
    available, reason = route_availability(state, destination)
    market = f"Market interest: {node.market_interest}." if node.market_interest else "No known cargo buyer."
    contact = "Established regional contacts." if node.region_id else "No permanent regional expedition here."
    known = node.description if destination in state.route_known or node.known else "Soundings incomplete; details unknown."
    return [
        f"{node.name}: {known}",
        f"Leg: {edge.hazard}; {leg_travel_time(state, edge)} actions; supplies {edge.supply_cost}.",
        f"Cargo exposure {edge.cargo_risk}/3; weather exposure {edge.weather_exposure}/4.",
        f"Season: {calendar_at(state).season}; {seasonal_route_note(state)}.",
        f"{market} {contact}",
        "REACHABLE" if available else f"BLOCKED — {reason}",
    ]


def chart_move(state: GameState, current: str, dx: int, dy: int) -> str:
    """Choose the connected node most closely aligned with an input direction."""
    origin = state.route_nodes[current]
    candidates = []
    for node_id in neighbours(state, current):
        node = state.route_nodes[node_id]
        vx, vy = node.x - origin.x, node.y - origin.y
        forward = vx * dx + vy * dy
        if forward <= 0:
            continue
        sideways = abs(vx * dy - vy * dx)
        candidates.append((-forward, sideways, node_id))
    return min(candidates)[2] if candidates else current


def connected_nodes(state: GameState) -> set[str]:
    start = next(iter(state.route_nodes))
    queue, seen = deque([start]), {start}
    while queue:
        for node in neighbours(state, queue.popleft()):
            if node not in seen:
                seen.add(node)
                queue.append(node)
    return seen


def validate_route_chart(state: GameState) -> None:
    if not state.route_nodes or connected_nodes(state) != set(state.route_nodes):
        raise ValueError("route chart is disconnected")
    ids = [edge.id for edge in state.route_edges]
    if len(ids) != len(set(ids)):
        raise ValueError("route edge identities must be unique")
    if any(edge.first not in state.route_nodes or edge.second not in state.route_nodes or edge.first == edge.second for edge in state.route_edges):
        raise ValueError("route edge endpoint is invalid")
    if set(REGION_NODES) - set(state.route_nodes) or state.route_current_node not in state.route_nodes:
        raise ValueError("route chart omits a required regional mooring")
