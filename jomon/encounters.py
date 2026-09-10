"""Authored regional encounter composition and offline balance evidence."""

from __future__ import annotations

from collections import Counter
from dataclasses import dataclass
import json

from .content import ENEMY_ARCHETYPES
from .state import Position, Threat, stage_rng

REGION_IDS = ("greywash", "greenwold", "whitecairn")
PRESSURE_BUDGET = {"steady": 3, "strained": 5, "critical": 8}
COMPLEMENTARY_ROLES = {
    "lookout": {"flanker", "protector", "shooter"},
    "shooter": {"protector", "flanker", "controller"},
    "skirmisher": {"thief", "controller"},
    "protector": {"shooter", "suppressor"},
    "suppressor": {"flanker", "protector"},
    "controller": {"shooter", "thief"},
    "thief": {"skirmisher", "controller"},
    "territorial": {"lookout", "controller"},
    "tracker": {"shooter", "flanker"},
    "flanker": {"shooter", "suppressor"},
}

STANDARD_SIGNATURE_FIELDS = (
    "profile", "role", "goal", "duty", "ecology", "ranged_kind", "range",
    "supplies", "vision", "hearing", "morale",
)


def roster_audit() -> dict[str, object]:
    """Validate mechanics-driving rows rather than treating names as variety."""
    standard = {
        identity: data for identity, data in ENEMY_ARCHETYPES.items()
        if not data.get("elite")
    }
    elite = {
        identity: data for identity, data in ENEMY_ARCHETYPES.items()
        if data.get("elite")
    }
    signatures: dict[tuple[object, ...], list[str]] = {}
    invalid = []
    for identity, data in standard.items():
        signature = tuple(data.get(field) for field in STANDARD_SIGNATURE_FIELDS)
        signatures.setdefault(signature, []).append(identity)
        glyph = str(data.get("glyph", ""))
        required = all(data.get(field) not in {None, ""} for field in (
            "region", "name", "profile", "role", "goal", "capability",
            "reaction", "terrain", "counterplay",
        ))
        if (
            not required or len(glyph) != 1 or not glyph.isascii()
            or " or " not in str(data.get("counterplay", ""))
            or int(data.get("morale", -1)) < 1
        ):
            invalid.append(identity)
    duplicate_signatures = [
        identities for identities in signatures.values() if len(identities) > 1
    ]
    glyphs = Counter(str(data.get("glyph", "")) for data in standard.values())
    duplicate_glyphs = sorted(glyph for glyph, count in glyphs.items() if count > 1)
    from .frontier_elites import ELITE_DEFINITIONS

    return {
        "standard_archetypes": len(standard),
        "mechanically_distinct_signatures": len(signatures),
        "elite_catalogue": len(elite),
        "elite_situations": len(elite) + 1,  # Hearthford crown wheel is map-authored.
        "named_recurring_rivals": sum(bool(data.get("named")) for data in ELITE_DEFINITIONS.values()),
        "regions": dict(sorted(Counter(str(data["region"]) for data in standard.values()).items())),
        "invalid_standard_rows": sorted(invalid),
        "duplicate_mechanical_signatures": duplicate_signatures,
        "duplicate_standard_glyphs": duplicate_glyphs,
    }


def validate_roster() -> None:
    report = roster_audit()
    if (
        report["standard_archetypes"] < 48
        or report["mechanically_distinct_signatures"] < 48
        or report["elite_situations"] < 16
        or report["named_recurring_rivals"] < 4
        or report["invalid_standard_rows"]
        or report["duplicate_mechanical_signatures"]
        or report["duplicate_standard_glyphs"]
        or any(count < 6 for count in report["regions"].values())
    ):
        raise ValueError(f"enemy roster acceptance failed: {report}")


@dataclass(frozen=True)
class EncounterPlan:
    region_id: str
    pressure_band: str
    archetypes: tuple[str, ...]
    budget: int
    spent: int


def _pool(region_id: str, *, elite: bool = False) -> list[str]:
    return [
        key for key, value in ENEMY_ARCHETYPES.items()
        if value["region"] == region_id and bool(value.get("elite")) == elite
    ]


def compose_encounter(
    seed: str,
    region_id: str,
    pressure_band: str,
    site_index: int,
    *,
    candidates: tuple[str, ...] | None = None,
    previously_used: tuple[str, ...] = (),
    prefer_ranged: bool = False,
) -> EncounterPlan:
    """Choose one bounded authored group; this is not an encounter language."""
    if not _pool(region_id) or pressure_band not in PRESSURE_BUDGET:
        raise ValueError("unknown regional encounter band")
    rng = stage_rng(seed, f"encounter:{region_id}:{pressure_band}:{site_index}")
    budget = PRESSURE_BUDGET[pressure_band]
    standard = list(candidates) if candidates is not None else _pool(region_id)
    if not standard or any(key not in _pool(region_id) for key in standard):
        raise ValueError("encounter candidates must be regional standard actors")
    rng.shuffle(standard)
    uses = Counter(previously_used)
    standard.sort(key=lambda key: (uses[key], not (prefer_ranged and ENEMY_ARCHETYPES[key]["profile"] == "ranged")))
    chosen = [standard[0]]
    spent = int(ENEMY_ARCHETYPES[chosen[0]]["budget"])
    if site_index == 0:
        return EncounterPlan(region_id, pressure_band, tuple(chosen), budget, spent)

    elite_pool = _pool(region_id, elite=True)
    if candidates is None and elite_pool and pressure_band == "critical" and site_index >= 4 and rng.randrange(6) == 0:
        elite = rng.choice(elite_pool)
        return EncounterPlan(region_id, pressure_band, (elite,), budget, int(ENEMY_ARCHETYPES[elite]["budget"]))

    for candidate in standard[1:]:
        data = ENEMY_ARCHETYPES[candidate]
        loyalties = {ENEMY_ARCHETYPES[key].get("ecology") for key in chosen}
        if (data.get("ecology") == "warden" and "raider" in loyalties
                or data.get("ecology") == "raider" and "warden" in loyalties):
            continue
        cost = int(data["budget"])
        if spent + cost > budget:
            continue
        roles = {str(ENEMY_ARCHETYPES[key]["role"]) for key in chosen}
        role = str(data["role"])
        complements = any(role in COMPLEMENTARY_ROLES.get(existing, set()) or existing in COMPLEMENTARY_ROLES.get(role, set()) for existing in roles)
        ranged_count = sum(ENEMY_ARCHETYPES[key]["profile"] == "ranged" for key in chosen)
        if not complements or (data["profile"] == "ranged" and ranged_count >= 2):
            continue
        chosen.append(candidate)
        spent += cost
        if len(chosen) >= 3:
            break
    return EncounterPlan(region_id, pressure_band, tuple(chosen), budget, spent)


def production_encounter_groups(seed: str, region_id: str) -> tuple[EncounterPlan, ...]:
    """Build six finite standard actors in progressively riskier authored sites."""
    if any(ENEMY_ARCHETYPES[key].get("ecology") for key in _pool(region_id)):
        # Frontier wildlife already has a separate predator/prey situation.
        # Worksites should teach material duties, not multiply that same pair.
        workers = tuple(key for key in _pool(region_id) if ENEMY_ARCHETYPES[key]["profile"] != "animal")
        used: tuple[str, ...] = ()
        groups = []
        for index in range(6):
            if len(used) >= 6:
                break
            band = "steady" if index == 0 else "strained" if index == 1 else "critical"
            available = tuple(key for key in workers if used.count(key) < 2)
            plan = compose_encounter(seed, region_id, band, index, candidates=available,
                                     previously_used=used, prefer_ranged=index == 1)
            choices = plan.archetypes[:6 - len(used)]
            groups.append(EncounterPlan(region_id, band, choices, plan.budget,
                                        sum(int(ENEMY_ARCHETYPES[key]["budget"]) for key in choices)))
            used += choices
        return tuple(groups)
    groups = [
        compose_encounter(seed, region_id, "steady", 0),
        compose_encounter(seed, region_id, "strained", 1),
        compose_encounter(seed, region_id, "critical", 2),
    ]
    bounded: list[EncounterPlan] = []
    actor_count = 0
    for plan in groups:
        archetypes = plan.archetypes[: max(0, 6 - actor_count)]
        if not archetypes:
            break
        bounded.append(
            EncounterPlan(
                plan.region_id,
                plan.pressure_band,
                archetypes,
                plan.budget,
                sum(int(ENEMY_ARCHETYPES[key]["budget"]) for key in archetypes),
            )
        )
        actor_count += len(archetypes)
    groups = bounded
    site_index = 10
    while actor_count < 6:
        plan = compose_encounter(seed, region_id, "strained", site_index)
        remaining = 6 - actor_count
        archetypes = plan.archetypes[:remaining]
        spent = sum(int(ENEMY_ARCHETYPES[key]["budget"]) for key in archetypes)
        groups.append(
            EncounterPlan(region_id, "strained", archetypes, plan.budget, spent)
        )
        actor_count += len(archetypes)
        site_index += 1
    return tuple(groups)


def threat_from_archetype(
    archetype: str,
    position: Position,
    *,
    encounter_id: str,
    group: str,
) -> Threat:
    data = ENEMY_ARCHETYPES[archetype]
    maximum = 7 if data.get("elite") else 5 if int(data["budget"]) >= 3 else 4
    ranged = data["profile"] == "ranged"
    return Threat(
        id=f"{encounter_id}:{archetype}", name=str(data["name"]), profile=str(data["profile"]),
        position=position, health=maximum, max_health=maximum, morale=int(data["morale"]),
        elite=bool(data.get("elite")), role=str(data["role"]), goal=str(data["goal"]),
        goal_reason=f"regional duty: {data['goal']}", vision=int(data["vision"]),
        hearing=int(data["hearing"]), home_position=position, group=group,
        ammunition=6 if ranged else 0, region_id=str(data["region"]),
        ranged_kind=str(data.get("ranged_kind", "crossbow")),
        capabilities=[str(data["capability"])],
        allegiance=f"{data['region']}:{data.get('ecology', 'guard')}",
        ecology=str(data.get("ecology", "")), duty=str(data.get("duty", "")),
        supplies=int(data.get("supplies", 0)), glyph=str(data.get("glyph", "")),
    )


def frontier_population(seed: str, region) -> list[Threat]:
    """Six budgeted working actors and one observable predator/prey pair."""
    from .regions import region_reachable

    reachable = region_reachable(region)
    occupied = set(region.landmarks.values()) | {box.position for box in region.containers}
    occupied |= {p for link in region.vertical_links for p in (link.first, link.second)}
    actors = []
    group_sites = ("ruin", "works", "far_bank", "store", "cave_entrance", "works")
    for group_index, plan in enumerate(production_encounter_groups(seed, region.id)):
        origin = region.landmarks[group_sites[min(group_index, len(group_sites) - 1)]]
        for archetype in plan.archetypes:
            candidates = reachable - occupied
            point = min(candidates, key=lambda p: (abs(p.z - origin.z) * 100 + abs(p.x - origin.x) + abs(p.y - origin.y), p.z, p.y, p.x))
            actor = threat_from_archetype(archetype, point, encounter_id=f"{region.id}-work-{len(actors)}", group=f"{region.id}-work-{group_index}")
            actor.objective_position = Position(origin.x + 1, origin.y + 1, origin.z)
            if actor.objective_position not in reachable or actor.objective_position in occupied:
                actor.objective_position = point
            occupied.add(point)
            actors.append(actor)
    origin = region.landmarks["far_bank"]
    for ecology, dx in (("prey", -5), ("predator", 5)):
        archetype = next(key for key, data in ENEMY_ARCHETYPES.items() if data["region"] == region.id and data.get("ecology") == ecology)
        point = min(reachable - occupied, key=lambda p: (abs(p.z) * 100 + abs(p.x - origin.x - dx) + abs(p.y - origin.y - 3), p.z, p.y, p.x))
        occupied.add(point)
        actors.append(threat_from_archetype(archetype, point, encounter_id=f"{region.id}-wildlife", group=f"{region.id}-{ecology}"))
    return actors


def encounter_audit(sample_count: int = 100) -> dict[str, object]:
    frequencies: Counter[str] = Counter()
    role_pairs: Counter[str] = Counter()
    budget_distribution: Counter[str] = Counter()
    ranged = elite = invalid = opening_attacks = unreachable = 0
    repetitions: Counter[tuple[str, ...]] = Counter()
    pressure_counts: Counter[str] = Counter()
    production_frequencies: Counter[str] = Counter()
    production_compositions: Counter[tuple[str, ...]] = Counter()
    for index in range(sample_count):
        seed = f"audit-{index:03d}"
        from .regions import build_new_regions, region_reachable

        regions, _, regional_threats, _ = build_new_regions(seed)
        for region_id, region in regions.items():
            reachable = region_reachable(region)
            unreachable += sum(
                threat.position not in reachable
                for threat in regional_threats[region_id]
            )
            standard = tuple(
                threat.id.split(":", 1)[-1]
                for threat in regional_threats[region_id]
                if not threat.elite
            )
            production_compositions[standard] += 1
            production_frequencies.update(standard)
            placed_elites = [
                threat.id.split(":", 1)[-1]
                for threat in regional_threats[region_id]
                if threat.elite
            ]
            frequencies.update(placed_elites)
            elite += len(placed_elites)
            opening_attacks += sum(
                threat.status == "engaged" and threat.profile == "ranged"
                for threat in regional_threats[region_id]
            )
        for region in REGION_IDS:
            for band in PRESSURE_BUDGET:
                plan = compose_encounter(seed, region, band, index % 6)
                repetitions[plan.archetypes] += 1
                budget_distribution[f"{plan.spent}/{plan.budget}"] += 1
                pressure_counts[band] += len(plan.archetypes)
                roles = sorted(str(ENEMY_ARCHETYPES[key]["role"]) for key in plan.archetypes)
                for left, right in zip(roles, roles[1:]):
                    role_pairs[f"{left}+{right}"] += 1
                for key in plan.archetypes:
                    frequencies[key] += 1
                    data = ENEMY_ARCHETYPES[key]
                    ranged += data["profile"] == "ranged"
                    elite += bool(data.get("elite"))
                if plan.spent > plan.budget or len(set(plan.archetypes)) != len(plan.archetypes):
                    invalid += 1
                # Ranged actors always enter through watching status and aim before firing.
                opening_attacks += 0
    return {
        "samples": sample_count,
        "plans": sample_count * len(REGION_IDS) * len(PRESSURE_BUDGET),
        "archetype_frequency": dict(sorted(frequencies.items())),
        "role_combinations": dict(sorted(role_pairs.items())),
        "threat_budget_distribution": dict(sorted(budget_distribution.items())),
        "ranged_actor_frequency": ranged,
        "elite_frequency": elite,
        "pressure_band_actor_counts": dict(sorted(pressure_counts.items())),
        "invalid_or_forbidden": invalid,
        "unavoidable_opening_attacks": opening_attacks,
        "unique_compositions": len(repetitions),
        "most_repeated_composition": max(repetitions.values(), default=0),
        "unreachable_actors": unreachable,
        "production_archetype_frequency": dict(sorted(production_frequencies.items())),
        "production_unique_compositions": len(production_compositions),
    }


if __name__ == "__main__":
    print(json.dumps(encounter_audit(100), indent=2, sort_keys=True))
