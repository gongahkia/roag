"""Machine-readable focused audits for content, persistence, replay, and soak."""

from __future__ import annotations

import argparse
import hashlib
import json
import tempfile
import tracemalloc
from pathlib import Path

from .actions import depart, move
from .aftermath import AFTERMATH_LINES
from .build_scenarios import BUILD_SCENARIOS, validate_build_scenarios
from .content import (
    COMMODITIES, DISCOVERIES, ENEMY_ARCHETYPES, GEAR, MERCHANT_ITEMS,
    PASSIVES, RECRUIT_TEMPLATES, RELICS, ROLE_TECHNIQUE, WEAPONS,
    validate_commodity_content,
)
from .encounters import encounter_audit, roster_audit, validate_roster
from .frontiers import FRONTIERS, ensure_frontier
from .inventory import BASIC_COURIER_ARMOUR, BASIC_COURIER_LOADOUTS, ITEM_SPECS
from .legendary import validate_legends
from .living_audit import living_audit
from .practices import PRACTICES, validate_practices
from .preparations import PREPARATIONS, validate_preparations
from .quests import ADDITIONAL_ARCS, QUESTS, quest_reachability_audit
from .save import load_game, save_game
from .ship_crises import TACTICAL, VOYAGES
from .state import HISTORY_LIMIT, MESSAGE_LIMIT, StateError, create_world, game_state_from_dict
from .travel import choose_destination, resolve_voyage
from .vessel import DRINKS
from .worklines import WORKLINES


def expanded_world(seed: str):
    state = create_world(seed)
    for region_id in FRONTIERS:
        ensure_frontier(state, region_id)
    return state


def _digest(state) -> str:
    payload = json.dumps(state.to_dict(), sort_keys=True, separators=(",", ":")).encode()
    return hashlib.sha256(payload).hexdigest()


def _techniques() -> set[str]:
    return (
        set(ROLE_TECHNIQUE.values())
        | {str(template["technique"]) for template in RECRUIT_TEMPLATES}
        | {"mill hearing", "shoreline measure", "smoke spoor", "bell interval"}
        | set(PRACTICES)
    )


def content_audit(seed: str = "content-verification") -> dict[str, object]:
    state = expanded_world(seed)
    validate_commodity_content()
    validate_build_scenarios()
    validate_roster()
    validate_legends(state)
    validate_practices()
    validate_preparations()
    roster = roster_audit()
    armour = {kind for kind, spec in ITEM_SPECS.items() if spec.category == "armour"}
    techniques = _techniques()
    counts = {
        "regions": len(state.regions),
        "standard_enemies": roster["standard_archetypes"],
        "mechanically_distinct_enemies": roster["mechanically_distinct_signatures"],
        "elite_situations": roster["elite_situations"],
        "named_rivals": roster["named_recurring_rivals"],
        "weapons": len(WEAPONS), "armour": len(armour),
        "passives": len(PASSIVES), "techniques": len(techniques),
        "active_passives_and_techniques": len(PASSIVES) + len(techniques),
        "secondary_tools_supplies_drinks": len(GEAR) + len(DISCOVERIES) + len(DRINKS),
        "relics": len(RELICS), "containers": sum(len(region.containers) for region in state.regions.values()),
        "legendary_objects": len(state.legendary_objects),
        "regional_questlines": len(QUESTS) + len(WORKLINES) + len(AFTERMATH_LINES),
        "cross_region_arcs": 1 + len(ADDITIONAL_ARCS),
        "institutions": len(state.institutions),
        "persistent_nonhostile_characters": len(state.household) + len(state.visitors) + sum(len(contacts) for contacts in state.contacts.values()) + 2,
        "commodities": len(COMMODITIES), "voyage_families": len(VOYAGES),
        "tactical_voyage_families": len(TACTICAL), "build_scenarios": len(BUILD_SCENARIOS),
    }
    minima = {
        "regions": 8, "standard_enemies": 72, "mechanically_distinct_enemies": 72,
        "elite_situations": 24, "named_rivals": 8, "weapons": 36,
        "armour": 36, "techniques": 32, "active_passives_and_techniques": 80,
        "secondary_tools_supplies_drinks": 51, "relics": 12,
        "containers": 60, "regional_questlines": 20, "cross_region_arcs": 5,
        "institutions": 12, "persistent_nonhostile_characters": 38,
        "voyage_families": 12, "tactical_voyage_families": 6,
        "build_scenarios": 24,
    }
    failures = [f"{key}: {counts[key]} < {minimum}" for key, minimum in minima.items() if counts[key] < minimum]
    source_failures = []
    container_rewards = {
        reward
        for region in state.regions.values()
        for container in region.containers
        for reward in (container.reward, *container.extra_rewards)
    }
    production_sources = (
        set(MERCHANT_ITEMS) | container_rewards
        | {kind for loadout in BASIC_COURIER_LOADOUTS.values() for kind in loadout}
        | {kind for armour_set in BASIC_COURIER_ARMOUR.values() for kind in armour_set.values()}
    )
    for kind in set(WEAPONS) | armour:
        if kind not in production_sources:
            source_failures.append(f"equipment without physical production path: {kind}")
    for legend in state.legendary_objects.values():
        if not any(container.legendary_id == legend.id for container in state.regions[legend.region_id].containers):
            source_failures.append(f"legend without physical cache: {legend.id}")
    failures.extend(source_failures)
    return {
        "seed": seed, "counts": counts, "minimums": minima,
        "failures": failures, "roster": roster,
        "ids": {
            "regions": sorted(state.regions), "weapons": sorted(WEAPONS),
            "armour": sorted(armour), "passives": sorted(PASSIVES),
            "techniques": sorted(techniques), "relics": sorted(RELICS),
            "preparations": sorted(PREPARATIONS),
            "containers": sorted(container.id for region in state.regions.values() for container in region.containers),
            "quests": sorted([
                *(definition["title"] for definition in QUESTS.values()),
                *(row[0] for row in WORKLINES.values()),
                *(row[0] for row in AFTERMATH_LINES.values()),
            ]),
            "arcs": ["The Four Working Marks", *(str(row["title"]) for row in ADDITIONAL_ARCS.values())],
            "commodities": sorted(COMMODITIES), "voyages": sorted(VOYAGES),
        },
    }


def persistence_audit(seed: str = "persistence-verification") -> dict[str, object]:
    state = expanded_world(seed)
    current = state.to_dict()
    with tempfile.TemporaryDirectory(prefix="jomon-persistence-") as directory:
        path = Path(directory) / "world.json"
        save_game(state, path)
        loaded = load_game(path)
        round_trip = loaded.to_dict() == current
        save_bytes = path.stat().st_size
    legacy = json.loads(json.dumps(current))
    legacy["save_format"] = 6
    legacy.pop("legendary_objects", None)
    legacy.pop("cross_region_arcs", None)
    legacy.pop("institutions", None)
    for region in legacy["regions"].values():
        region.pop("materials", None)
        region.pop("material_cursor", None)
        region.pop("generation_facts", None)
        region.pop("regional_history", None)
        for container in region["containers"]:
            container.pop("legendary_id", None)
    for item in legacy["items"]:
        item.pop("legendary_id", None)
    migrated = game_state_from_dict(legacy)
    identities_before = [(item["id"], item["kind"], item["location"], item.get("owner_id")) for item in legacy["items"]]
    identities_after = [(item.id, item.kind, item.location, item.owner_id) for item in migrated.items]
    corruption_rejected = False
    broken = migrated.to_dict()
    broken["active_courier_id"] = "missing-courier"
    try:
        game_state_from_dict(broken)
    except StateError:
        corruption_rejected = True
    failures = []
    if not round_trip:
        failures.append("format-7 JSON round trip changed state")
    if identities_after != identities_before:
        failures.append("format-6 migration moved, replaced, or reissued possessions")
    if len(migrated.legendary_objects) != len(migrated.regions):
        failures.append("format-6 migration did not reconstruct the immutable legend manifest")
    if not corruption_rejected:
        failures.append("malformed courier reference was accepted")
    return {
        "seed": seed, "format": migrated.save_format, "save_bytes": save_bytes,
        "format_7_round_trip": round_trip,
        "format_6_item_identities_preserved": identities_after == identities_before,
        "format_6_regions_preserved": set(migrated.regions) == set(legacy["regions"]),
        "corruption_rejected": corruption_rejected, "failures": failures,
    }


def replay_audit(samples: int = 25) -> dict[str, object]:
    script = ((1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (-1, -1)) * 2
    failures = []
    for index in range(samples):
        seed = f"replay-audit-{index:04d}"
        first = create_world(seed)
        depart(first)
        for dx, dy in script[:6]:
            move(first, dx, dy)
        resumed = game_state_from_dict(first.to_dict())
        for dx, dy in script[6:]:
            move(first, dx, dy)
            move(resumed, dx, dy)
        if _digest(first) != _digest(resumed):
            failures.append(seed)
    return {"samples": samples, "actions_per_sample": len(script), "failures": failures}


def encounter_verification(samples: int = 100) -> dict[str, object]:
    report = encounter_audit(samples)
    report["failures"] = [
        label for label, value in (
            ("invalid encounter budgets or duplicate groups", report["invalid_or_forbidden"]),
            ("unavoidable opening attacks", report["unavoidable_opening_attacks"]),
            ("unreachable placed actors", report["unreachable_actors"]),
        ) if value
    ]
    return report


def quest_verification(samples: int = 25) -> dict[str, object]:
    report = quest_reachability_audit(samples)
    report["failures"] = list(report["unreachable_or_invalid"])
    return report


def living_verification(samples: int = 100) -> dict[str, object]:
    report = living_audit(samples, max(2, min(12, samples // 8)))
    report["failures"] = [
        key for key in (
            "disconnected_route_graphs", "nondeterministic_route_graphs",
            "schedule_validation_failures", "vessel_position_overlaps",
            "nondeterministic_schedules",
        ) if report[key]
    ]
    return report


def generation_verification(samples: int = 1000) -> dict[str, object]:
    from .systemic_audit import systemic_audit

    return systemic_audit(samples, progress=False)


def memory_soak(legs: int = 80) -> dict[str, object]:
    if legs < 4:
        raise ValueError("soak needs at least four route legs")
    state = create_world("route-memory-soak")
    tracemalloc.start()
    sizes = []
    current_samples = []
    for index in range(legs):
        destination = "reed-anchor" if state.route_current_node == "hearthford" else "hearthford"
        changed, reason = choose_destination(state, destination, forced_voyage="shoal")
        if not changed:
            raise RuntimeError(reason)
        resolved, reason = resolve_voyage(state, "navigate")
        if not resolved:
            raise RuntimeError(reason)
        if index >= legs // 2:
            sizes.append(len(json.dumps(state.to_dict(), sort_keys=True)))
            current_samples.append(tracemalloc.get_traced_memory()[0])
    current, peak = tracemalloc.get_traced_memory()
    tracemalloc.stop()
    bounds = {
        "history": len(state.history), "messages": len(state.messages),
        "sound_events": len(state.sound_events), "group_alerts": len(state.group_alerts),
        "route_marks": len(state.traversed_route_edges), "items": len(state.items),
        "regional_material_cells": sum(len(region.materials) for region in state.regions.values()),
        "vessel_material_cells": len(state.vessel_materials),
    }
    failures = []
    if bounds["history"] > HISTORY_LIMIT or bounds["messages"] > MESSAGE_LIMIT:
        failures.append("bounded narrative logs grew past their caps")
    if max(sizes) - min(sizes) > 4096:
        failures.append("serialized state grew materially after the soak warmup")
    if current_samples[-1] - min(current_samples) > 512_000:
        failures.append("traced live memory grew by more than 500 KiB after warmup")
    return {
        "legs": legs, "bounds": bounds, "serialized_bytes_min": min(sizes),
        "serialized_bytes_max": max(sizes), "traced_current_bytes": current,
        "traced_peak_bytes": peak, "post_warmup_growth_bytes": current_samples[-1] - min(current_samples),
        "failures": failures,
    }


AUDITS = {
    "content": content_audit,
    "persistence": persistence_audit,
    "replay": replay_audit,
    "encounter": encounter_verification,
    "generation": generation_verification,
    "living": living_verification,
    "quest": quest_verification,
    "soak": memory_soak,
}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("audit", choices=(*AUDITS, "all"))
    parser.add_argument("--samples", type=int, default=None)
    args = parser.parse_args()
    if args.audit == "all":
        samples = args.samples or 25
        report = {
            "content": content_audit(), "persistence": persistence_audit(),
            "replay": replay_audit(samples), "encounter": encounter_verification(samples),
            "generation": generation_verification(samples), "living": living_verification(samples),
            "quest": quest_verification(samples), "soak": memory_soak(max(8, samples)),
        }
    else:
        function = AUDITS[args.audit]
        report = function(args.samples) if args.samples is not None and args.audit in {"replay", "encounter", "generation", "living", "quest", "soak"} else function()
    print(json.dumps(report, indent=2, sort_keys=True))
    failures = report.get("failures", []) if isinstance(report, dict) else []
    if args.audit == "all":
        failures = [failure for section in report.values() if isinstance(section, dict) for failure in section.get("failures", [])]
    raise SystemExit(bool(failures))


if __name__ == "__main__":
    main()
