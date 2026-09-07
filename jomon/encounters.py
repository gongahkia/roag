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
) -> EncounterPlan:
    """Choose one bounded authored group; this is not an encounter language."""
    if region_id not in REGION_IDS or pressure_band not in PRESSURE_BUDGET:
        raise ValueError("unknown regional encounter band")
    rng = stage_rng(seed, f"encounter:{region_id}:{pressure_band}:{site_index}")
    budget = PRESSURE_BUDGET[pressure_band]
    standard = _pool(region_id)
    rng.shuffle(standard)
    chosen = [standard[0]]
    spent = int(ENEMY_ARCHETYPES[chosen[0]]["budget"])
    if site_index == 0:
        return EncounterPlan(region_id, pressure_band, tuple(chosen), budget, spent)

    elite_pool = _pool(region_id, elite=True)
    if pressure_band == "critical" and site_index >= 4 and rng.randrange(6) == 0:
        elite = elite_pool[0]
        return EncounterPlan(region_id, pressure_band, (elite,), budget, int(ENEMY_ARCHETYPES[elite]["budget"]))

    first_role = str(ENEMY_ARCHETYPES[chosen[0]]["role"])
    for candidate in standard[1:]:
        data = ENEMY_ARCHETYPES[candidate]
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
    )


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
