"""Physical, seeded field caches that reward reading local terrain."""

from __future__ import annotations

from .state import Container, GameState, Position, Region, stage_rng


# Each clue names a visible geological or work trace; the cache itself is
# ordinary packable loot and retains its state through the region save.
DISCOVERIES = {
    "hearthford": (
        ("cave_entrance", "reed-silt", "Reed-silt survey roll", "willow dressing", "light", "fresh reed cuts beside an older silt mark"),
        ("watchtower", "watch-foundation", "Watch foundation pocket", "boar spear", "key", "a loose foundation stone below the old watch"),
    ),
    "greywash": (
        ("wreck", "ebb-line", "Ebb-line salvage pouch", "brine wash", "light", "a line of dry salt ending at buried tackle"),
        ("dunes", "dune-pin", "Dune survey pin box", "fletched arrows", "rope", "a marked pin half hidden in wind-cut sand"),
    ),
    "greenwold": (
        ("root_cellar", "root-hollow", "Root-hollow medicine wrap", "pine resin dressing", "light", "a cut root with fresh charcoal below its bark"),
        ("watch_tree", "canopy-fall", "Canopy-fall hunting bundle", "throwing javelins", "rope", "a snapped cord leading away from the watch tree"),
    ),
    "whitecairn": (
        ("sinkhole", "scree-seam", "Scree-seam brace packet", "limestone wedge", "light", "a seam of pale chips at the sink edge"),
        ("bell_tower", "bell-shadow", "Bell-shadow tool roll", "splint roll", "key", "a false bell mark scratched into exposed stone"),
    ),
    "dunmire": (
        ("cave_entrance", "peat-print", "Peat-print dry packet", "sealed pitch pot", "light", "a dry boot print crossing wet peat"),
        ("ruin", "bank-notch", "Bank-notch wicker wrap", "willow dressing", "rope", "an old water notch above the present fen line"),
    ),
    "rillscar": (
        ("far_bank", "span-pin", "Span-pin survey case", "fletched arrows", "rope", "a second bridge pin driven into the rock"),
        ("cave_entrance", "iron-vein", "Iron-vein bracing bundle", "splint roll", "light", "dark iron dust beneath a fresh stone chip"),
    ),
    "marlbank": (
        ("ruin", "seed-line", "Seed-line lime packet", "sealed lime pot", "light", "seed rows bending around a buried clay lid"),
        ("store", "kiln-mark", "Kiln-mark tally pouch", "market weights", "key", "a potter's mark on the cool side of the terrace"),
    ),
    "frostmere": (
        ("far_bank", "ice-sounding", "Ice-sounding pilot wrap", "brine wash", "rope", "two winter soundings cut into one gravel bar"),
        ("cave_entrance", "net-drift", "Net-drift salvage packet", "fletched arrows", "light", "a net weight caught well above the tide"),
    ),
}


def install_discoveries(region: Region, seed: str) -> None:
    from .regions import region_reachable

    reachable = region_reachable(region)
    protected = {point for point in region.landmarks.values() if point.z == 0}
    protected |= {item.position for item in region.containers if item.position.z == 0}
    protected |= {point for link in region.vertical_links for point in (link.first, link.second) if point.z == 0}
    for anchor_key, suffix, name, reward, requirement, clue in DISCOVERIES[region.id]:
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
        region.containers.append(Container(
            f"{region.id}-{suffix}", name, position, reward, requirement,
            hidden=True, discovered=False, clue=clue,
        ))
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
            messages.append(f"A field trace resolves into {container.name}: {container.clue}. The cache is now marked; E opens it on site.")
            if not state.region.changes.get("field_discovery_trained"):
                state.region.changes["field_discovery_trained"] = True
                state.courier.wayfinding = min(20, state.courier.wayfinding + 1)
                messages.append(f"{state.courier.name} gains one Wayfinding from the first field discovery in {state.region.name}.")
    return messages
