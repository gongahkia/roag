"""Print deterministic local evidence for route and named-actor scheduling."""

from __future__ import annotations

from collections import Counter, deque
import json

from .actions import _advance_world
from .route_chart import build_route_graph
from .state import create_world
from .vessel import validate_living_vessel


def _connected(nodes: set[str], edges: list[object]) -> bool:
    adjacency = {node_id: set() for node_id in nodes}
    for edge in edges:
        adjacency[edge.first].add(edge.second)
        adjacency[edge.second].add(edge.first)
    queue, seen = deque([next(iter(nodes))]), set()
    while queue:
        current = queue.popleft()
        if current in seen:
            continue
        seen.add(current)
        queue.extend(adjacency[current] - seen)
    return seen == nodes


def _schedule_signature(state) -> tuple[tuple[object, ...], ...]:
    return tuple(
        (
            actor_id,
            schedule.area,
            schedule.position,
            schedule.activity,
            schedule.next_boundary,
        )
        for actor_id, schedule in sorted(state.actor_schedules.items())
    )


def living_audit(route_samples: int = 100, schedule_samples: int = 12) -> dict[str, object]:
    edge_counts: Counter[str] = Counter()
    disconnected = nondeterministic_routes = 0
    for index in range(route_samples):
        seed = f"living-audit-{index:03d}"
        nodes, edges = build_route_graph(seed)
        again_nodes, again_edges = build_route_graph(seed)
        nondeterministic_routes += (nodes, edges) != (again_nodes, again_edges)
        disconnected += not _connected(set(nodes), edges)
        edge_counts[str(len(edges))] += 1

    schedule_failures = schedule_overlap = nondeterministic_schedules = 0
    activity_counts: Counter[str] = Counter()
    actor_counts: Counter[str] = Counter()
    for index in range(schedule_samples):
        seed = f"living-schedule-{index:03d}"
        first, second = create_world(seed), create_world(seed)
        validate_living_vessel(first)
        validate_living_vessel(second)
        nondeterministic_schedules += _schedule_signature(first) != _schedule_signature(second)
        _advance_world(first, steps=18)
        _advance_world(second, steps=18)
        nondeterministic_schedules += _schedule_signature(first) != _schedule_signature(second)
        occupied = [
            (schedule.area, schedule.position)
            for schedule in first.actor_schedules.values()
            if schedule.area.startswith(("vessel:", "tavern"))
        ]
        schedule_overlap += len(occupied) != len(set(occupied))
        actor_counts[str(len(first.actor_schedules))] += 1
        activity_counts.update(schedule.activity for schedule in first.actor_schedules.values())
        try:
            validate_living_vessel(first)
        except ValueError:
            schedule_failures += 1
    return {
        "route_samples": route_samples,
        "route_edge_count_distribution": dict(sorted(edge_counts.items())),
        "disconnected_route_graphs": disconnected,
        "nondeterministic_route_graphs": nondeterministic_routes,
        "schedule_samples": schedule_samples,
        "scheduled_actor_count_distribution": dict(sorted(actor_counts.items())),
        "activity_frequency_after_18_actions": dict(sorted(activity_counts.items())),
        "schedule_validation_failures": schedule_failures,
        "vessel_position_overlaps": schedule_overlap,
        "nondeterministic_schedules": nondeterministic_schedules,
    }


def main() -> None:
    print(json.dumps(living_audit(), indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
