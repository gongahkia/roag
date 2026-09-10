"""Slow, local production-world audit. JSON stdout; progress on stderr."""

from __future__ import annotations

import argparse
from collections import Counter, deque
import hashlib
import json
import sys
import time

from .calendar import SEASONS
from .content import ENEMY_ARCHETYPES, validate_commodity_content
from .ecology import opposed, validate_ecology
from .encounters import production_encounter_groups
from .frontiers import FRONTIERS, ensure_frontier
from .quests import QUESTS
from .regional_history import validate_accounts
from .regions import region_reachable
from .state import Position, create_world, game_state_from_dict


def expanded_world(seed):
    state = create_world(seed)
    for region_id in FRONTIERS:
        ensure_frontier(state, region_id)
    return state


def digest(state):
    encoded = json.dumps(state.to_dict(), sort_keys=True, separators=(",", ":")).encode()
    return hashlib.sha256(encoded).hexdigest()


def topology(region, reachable):
    ground = {(point.x, point.y) for point in reachable if point.z == 0}
    edges = sum((x + 1, y) in ground for x, y in ground) + sum((x, y + 1) in ground for x, y in ground)
    open_cells = sum(all((x + dx, y + dy) in ground for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))) for x, y in ground)
    # A cycle rank describes topology, not whether a human finds a route fun.
    return {
        "ground_accessible": len(ground), "ground_open_ratio": round(open_cells / (region.width * region.height), 4),
        "ground_cycle_rank_lower_bound": max(0, edges - len(ground) + 1),
        "used_levels": len({point.z for point in reachable}),
    }


def inspect_world(state):
    """Inspect real placements and links, not independent encounter samples."""
    failures, metrics = [], {}
    validate_commodity_content()
    validate_accounts(state)
    validate_ecology(state)
    for region_id, region in state.regions.items():
        reachable = region_reachable(region)
        required = {point for key, point in region.landmarks.items() if key in {"landing", "contact", "second_contact", "objective", "control", "cave_entrance", "elevated", "high_view"}}
        required.update(container.position for container in region.containers)
        if not required <= reachable:
            failures.append(f"{region_id}: unreachable objective, contact or container")
        if len({point.z for point in reachable}) < 3:
            failures.append(f"{region_id}: fewer than three connected used elevations")
        for link in region.vertical_links:
            if link.first not in reachable or link.second not in reachable or abs(link.first.z - link.second.z) != 1:
                failures.append(f"{region_id}: broken vertical link")
        actors = state.region_threats[region_id]
        positions = [actor.position for actor in actors if actor.status in {"watching", "engaged", "dormant"}]
        if len(set(positions)) != len(positions):
            failures.append(f"{region_id}: overlapping initial actors")
        if any(point not in reachable for point in positions):
            failures.append(f"{region_id}: unreachable actor")
        if any(actor.status == "engaged" or actor.aimed_at or actor.reaction for actor in actors):
            failures.append(f"{region_id}: precommitted opening attack")
        landing = region.landmarks["landing"]
        if any(abs(point.x - landing.x) + abs(point.y - landing.y) + abs(point.z - landing.z) < 4 for point in positions):
            failures.append(f"{region_id}: unsafe landing placement")
        for actor in actors:
            for ally in actors:
                if actor.group and actor.group == ally.group and opposed(actor, ally):
                    failures.append(f"{region_id}: opposed ecological interests share an alert group")
        definition = QUESTS.get(region_id)
        if not definition or definition["cache"] not in {container.id for container in region.containers}:
            failures.append(f"{region_id}: missing quest cache")
        contacts = state.contacts[region_id]
        if len(contacts) < 2 or any(contact.region_id != region_id for contact in contacts):
            failures.append(f"{region_id}: invalid named participants")
        for event in region.regional_history:
            if event.evidence not in {container.id for container in region.containers}:
                try:
                    point = Position(*map(int, event.evidence.split(",")))
                except (TypeError, ValueError):
                    failures.append(f"{region_id}: invalid historical evidence {event.evidence}")
                else:
                    if not (0 <= point.x < region.width and 0 <= point.y < region.height and str(point.z) in region.levels):
                        failures.append(f"{region_id}: evidence outside region")
        metric = topology(region, reachable)
        metric.update(containers=len(region.containers), actors=len(actors), histories=len(region.regional_history))
        metrics[region_id] = metric
        if metric["ground_open_ratio"] < .1 or not metric["ground_cycle_rank_lower_bound"]:
            failures.append(f"{region_id}: insufficient open ground or loops")
    for season in SEASONS:
        seen, queue = {"hearthford"}, deque(["hearthford"])
        while queue:
            node = queue.popleft()
            for edge in state.route_edges:
                if season in edge.closed_seasons or edge.integrity_required > 10:
                    continue
                target = edge.second if edge.first == node else edge.first if edge.second == node else None
                if target and target not in seen:
                    seen.add(target)
                    queue.append(target)
        if not set(QUESTS) <= seen:
            failures.append(f"{season}: an established region lacks a seasonal return route")
    return failures, metrics


def systemic_audit(samples=1000, start=0, *, progress=False):
    if samples < 1 or start < 0:
        raise ValueError("audit needs positive samples and a nonnegative start")
    began = time.perf_counter()
    failures, counts, roles, compositions, budgets = [], Counter(), Counter(), Counter(), Counter()
    geography = {region_id: set() for region_id in QUESTS}
    limits = {}
    timings = []
    for index in range(start, start + samples):
        seed = f"systemic-audit-{index:04d}"
        tick = time.perf_counter()
        try:
            state = expanded_world(seed)
            original = digest(state)
            duplicate = expanded_world(seed)
            if digest(duplicate) != original:
                failures.append(f"{seed}: generation is not reproducible")
            del duplicate
            errors, metrics = inspect_world(state)
            failures.extend(f"{seed}: {error}" for error in errors)
            reloaded = game_state_from_dict(state.to_dict())
            if digest(reloaded) != original:
                failures.append(f"{seed}: persistence changed recorded state")
            del reloaded
            for region_id, region in state.regions.items():
                geography[region_id].add(region.geography_signature)
                for key, value in metrics[region_id].items():
                    name = f"{region_id}:{key}"
                    old = limits.get(name, [value, value])
                    limits[name] = [min(old[0], value), max(old[1], value)]
                actors = state.region_threats[region_id]
                counts.update(actor.name for actor in actors)
                groups = {}
                for actor in actors:
                    groups.setdefault(actor.group or actor.id, []).append(actor)
                for group in groups.values():
                    roles["+".join(sorted(actor.role for actor in group))] += 1
                compositions[(region_id, tuple(sorted(actor.name for actor in actors)))] += 1
                if any(data["region"] == region_id and not data.get("elite") for data in ENEMY_ARCHETYPES.values()):
                    for plan in production_encounter_groups(seed, region_id):
                        budgets[f"{region_id}:{plan.pressure_band}:{plan.spent}/{plan.budget}"] += 1
                        if plan.spent > plan.budget:
                            failures.append(f"{seed}:{region_id}: over-budget production group")
            del state
        except (ValueError, RuntimeError, KeyError, TypeError) as exc:
            failures.append(f"{seed}: {type(exc).__name__}: {exc}")
        timings.append(time.perf_counter() - tick)
        if progress and (index - start + 1) % 25 == 0:
            print(f"{index - start + 1}/{samples} worlds; {len(failures)} failures; {time.perf_counter() - began:.1f}s", file=sys.stderr, flush=True)
    return {
        "samples": samples, "start": start, "regions_per_world": len(QUESTS),
        "checks": ["production generation replay", "aligned access", "container and objective access", "initial actor placement", "opening preparation", "opposed group rejection", "named history references", "seasonal chart return", "physical save round trip", "production budgets", "open-area and cycle metrics"],
        "not_measured": ["human tactical counterplay", "quest branch completion", "material-stage alternate routes", "native terminal latency", "mechanically distinct content count"],
        "failures": failures, "archetype_placement_frequency": dict(sorted(counts.items())),
        "role_combinations": dict(sorted(roles.items())), "budget_distribution": dict(sorted(budgets.items())),
        "unique_production_compositions": len(compositions), "most_repeated_composition": max(compositions.values(), default=0),
        "unique_geographies": {key: len(values) for key, values in geography.items()},
        "metric_min_max": limits, "elapsed_seconds": round(time.perf_counter() - began, 3),
        "worst_seed_seconds": round(max(timings), 3),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seeds", type=int, default=1000)
    parser.add_argument("--start", type=int, default=0)
    args = parser.parse_args()
    result = systemic_audit(args.seeds, args.start, progress=True)
    print(json.dumps(result, indent=2, sort_keys=True))
    raise SystemExit(bool(result["failures"]))


if __name__ == "__main__":
    main()
