"""Authored seed-varied route graph and inspectable leg consequences."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .calendar import calendar_at, seasonal_route_note
from .state import GameState, RouteEdge, RouteNode, stage_rng


REGION_NODES = {
    "hearthford": "hearthford",
    "greywash": "greywash",
    "greenwold": "greenwold",
    "whitecairn": "whitecairn",
}


def _node(
    node_id: str,
    name: str,
    point: tuple[int, int],
    kind: str,
    description: str,
    **kwargs,
) -> RouteNode:
    return RouteNode(node_id, name, point[0], point[1], kind, description, **kwargs)


def build_route_graph(seed: str) -> tuple[dict[str, RouteNode], list[RouteEdge]]:
    """Build one bounded navigational network; optional links vary by seed."""
    nodes = {
        "hearthford": _node("hearthford", "Hearthford", (8, 7), "region", "mill quay and river market", region_id="hearthford", market_interest="ironwork", supply=2),
        "reed-anchor": _node("reed-anchor", "Reed Anchor", (19, 4), "anchorage", "sheltered reeds, potable water, little trade", supply=3, risk=1),
        "charter-market": _node("charter-market", "Charter Market", (31, 8), "market", "a witnessed landing with cargo buyers", market_interest="paper", supply=2, risk=1),
        "greywash": _node("greywash", "Greywash", (46, 4), "region", "tidal salt reach and wreck road", region_id="greywash", market_interest="timber", risk=2),
        "ebb-crossing": _node("ebb-crossing", "Ebb Crossing", (58, 8), "hazard", "a fast tidal cut, safest near a turning tide", risk=3, seasonal_note="winter storms often close this reach"),
        "coast-refuge": _node("coast-refuge", "Coast Refuge", (70, 5), "anchorage", "stone lee and a repair crane", supply=1, integrity_required=4),
        "willow-ferry": _node("willow-ferry", "Willow Ferry", (24, 14), "resupply", "ferry garden and cordage exchange", supply=3, market_interest="grain"),
        "greenwold": _node("greenwold", "Greenwold", (40, 17), "region", "forest burnworks and resin trails", region_id="greenwold", market_interest="salt fish", risk=2),
        "old-lock": _node("old-lock", "Old Lock", (52, 14), "hazard", "damaged gates with a narrow tow", risk=3, integrity_required=6),
        "chalk-steps": _node("chalk-steps", "Chalk Steps", (62, 18), "unknown", "an incompletely sounded upland landing", known=False, risk=2),
        "whitecairn": _node("whitecairn", "Whitecairn", (75, 15), "region", "limestone terraces, quarry, and high bridge", region_id="whitecairn", market_interest="wool", risk=3),
        "storm-post": _node("storm-post", "Storm Post", (64, 11), "warning", "a staffed signal pole above exposed water", risk=2),
    }
    specifications = [
        ("h-r", "hearthford", "reed-anchor", 4, 1, 1, 1, [], "soft spring bank"),
        ("r-m", "reed-anchor", "charter-market", 4, 1, 1, 1, [], "reed channel"),
        ("m-g", "charter-market", "greywash", 5, 2, 2, 2, [], "open estuary"),
        ("g-e", "greywash", "ebb-crossing", 4, 2, 3, 3, ["winter"], "tidal cut"),
        ("e-c", "ebb-crossing", "coast-refuge", 4, 1, 2, 3, ["winter"], "outer coast"),
        ("h-w", "hearthford", "willow-ferry", 5, 1, 1, 1, [], "willow backwater"),
        ("w-f", "willow-ferry", "greenwold", 5, 2, 1, 1, [], "forest water"),
        ("f-l", "greenwold", "old-lock", 5, 1, 2, 2, [], "burn canal"),
        ("l-k", "old-lock", "chalk-steps", 5, 2, 2, 2, ["spring"], "damaged upland lock"),
        ("k-u", "chalk-steps", "whitecairn", 4, 1, 2, 2, [], "chalk tributary"),
        ("m-l", "charter-market", "old-lock", 6, 2, 2, 2, [], "long carrier canal"),
        ("l-s", "old-lock", "storm-post", 4, 1, 2, 3, [], "exposed reach"),
        ("s-u", "storm-post", "whitecairn", 5, 2, 3, 3, ["winter"], "ridge water"),
    ]
    rng = stage_rng(seed, "route-links")
    optional = [
        ("r-w", "reed-anchor", "willow-ferry", 5, 1, 1, 1, [], "quiet reed loop"),
        ("g-s", "greywash", "storm-post", 6, 2, 3, 4, ["winter"], "weatherward coast"),
        ("f-k", "greenwold", "chalk-steps", 7, 2, 2, 2, ["spring"], "timber portage"),
    ]
    specifications.extend(row for row in optional if rng.randrange(3) != 0)
    edges = [
        RouteEdge(edge_id, first, second, time, supply, cargo, weather, max(0, cargo + weather - 2), list(closed), hazard)
        for edge_id, first, second, time, supply, cargo, weather, closed, hazard in specifications
    ]
    return nodes, edges


def initialise_route_chart(state: GameState) -> None:
    nodes, edges = build_route_graph(state.seed)
    state.route_nodes, state.route_edges = nodes, edges
    state.route_current_node = state.active_region_id
    state.route_known = sorted({"hearthford", "reed-anchor", "charter-market", "willow-ferry", state.active_region_id})
    state.traversed_route_edges = list(dict.fromkeys(state.traversed_route_edges))


def edge_between(state: GameState, first: str, second: str) -> RouteEdge | None:
    return next((edge for edge in state.route_edges if {edge.first, edge.second} == {first, second}), None)


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
        f"Leg: {edge.hazard}; {edge.travel_time} actions; supplies {edge.supply_cost}.",
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
