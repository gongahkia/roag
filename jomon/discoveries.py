"""Physical, seeded field caches that reward reading local terrain."""

from __future__ import annotations

from .geography import FIELD_SECRETS
from .state import Container, GameState, Region, stage_rng
from .topology_presentation import topology_text


def _entry(region_id: str, suffix: str) -> dict[str, str] | None:
    return next((row for row in FIELD_SECRETS.get(region_id, ()) if row["id"] == suffix), None)


def _suffix(region_id: str, container_id: str) -> str | None:
    prefix = f"{region_id}-"
    if not container_id.startswith(prefix):
        return None
    suffix = container_id[len(prefix):]
    return suffix if _entry(region_id, suffix) is not None else None


def discovery_name(region_id: str, container: Container) -> str:
    suffix = _suffix(region_id, container.id)
    return topology_text(f"topology.{region_id}.discovery.{suffix}.name") if suffix else container.name


def discovery_clue(region_id: str, container: Container) -> str:
    suffix = _suffix(region_id, container.id)
    return topology_text(f"topology.{region_id}.discovery.{suffix}.clue") if suffix else container.clue


def install_discoveries(region: Region, seed: str) -> None:
    from .regions import region_reachable

    reachable = region_reachable(region)
    protected = {point for point in region.landmarks.values() if point.z == 0}
    protected |= {item.position for item in region.containers if item.position.z == 0}
    protected |= {point for link in region.vertical_links for point in (link.first, link.second) if point.z == 0}
    for row in FIELD_SECRETS[region.id]:
        anchor_key, suffix = row["anchor"], row["id"]
        anchor = region.landmarks[anchor_key]
        if anchor.z:
            raise RuntimeError(f"{region.id} discovery anchor is not on the ground")
        candidates = [
            point for point in reachable
            if point.z == 0 and region.levels["0"][point.y][point.x] in {".", "m", "t", "r", "q", ",", ";", ":"}
            and 5 <= abs(point.x - anchor.x) + abs(point.y - anchor.y) <= 13
            and all(abs(point.x - other.x) + abs(point.y - other.y) >= 3 for other in protected)
        ]
        if not candidates:
            raise RuntimeError(f"{region.id} has no reachable discovery site near {anchor_key}")
        rng = stage_rng(seed, f"{region.id}:discovery:{suffix}")
        candidates.sort(key=lambda point: (point.y, point.x))
        position = candidates[rng.randrange(len(candidates))]
        prototype = Container(
            f"{region.id}-{suffix}", "", position, row["reward"], row["requirement"],
            hidden=True, discovered=False, clue="",
        )
        prototype.name, prototype.clue = discovery_name(region.id, prototype), discovery_clue(region.id, prototype)
        region.containers.append(prototype)
        protected.add(position)


def reveal_nearby(state: GameState) -> list[str]:
    if state.location != "region" or state.courier is None:
        return []
    from .character import effective_competency

    radius = 2 if state.courier.attributes["perception"] >= 9 or effective_competency(state.courier, "wayfinding") >= 6 else 1
    messages = []
    for container in state.region.containers:
        if not container.hidden or container.discovered or container.position.z != state.position.z:
            continue
        if max(abs(container.position.x - state.position.x), abs(container.position.y - state.position.y)) <= radius:
            container.discovered = True
            key = f"{container.position.x},{container.position.y},{container.position.z}"
            if key not in state.region.seen:
                state.region.seen.append(key)
            messages.append(topology_text("topology.discovery.reveal").format(
                cache=discovery_name(state.active_region_id, container),
                clue=discovery_clue(state.active_region_id, container),
            ))
            if not state.region.changes.get("field_discovery_trained"):
                state.region.changes["field_discovery_trained"] = True
                state.courier.wayfinding = min(20, state.courier.wayfinding + 1)
                messages.append(topology_text("topology.discovery.wayfinding").format(
                    courier=state.courier.name, region=state.region.name,
                ))
    return messages
