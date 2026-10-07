"""Deterministic audit of real build affordances across campaign pressures."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from dataclasses import dataclass

from .actions import RANGED_WEAPONS, WEAPON_RANGES
from .build_scenarios import BUILD_SCENARIOS, BuildScenario
from .content import WEAPONS
from .work_weapons import WORK_WEAPONS


@dataclass(frozen=True)
class ChallengeFamily:
    id: str
    premise: str
    answers: frozenset[str]


CHALLENGES = (
    ChallengeFamily("steady", "ordinary mixed opposition with room to choose a lane", frozenset({"damage", "range", "control", "quiet", "negotiation"})),
    ChallengeFamily("strained", "noise, footing and a second hostile constrain the direct route", frozenset({"range", "guard", "control", "quiet", "mobility", "treatment"})),
    ChallengeFamily("critical", "escape, survival and objective custody matter more than damage", frozenset({"guard", "control", "mobility", "smoke", "treatment", "cargo", "range"})),
    ChallengeFamily("elite", "a warned rule-changing actor controls terrain or protection", frozenset({"anti-armour", "structure", "control", "negotiation", "range", "guard"})),
    ChallengeFamily("voyage", "connected decks combine boarding pressure with cargo and hull work", frozenset({"range", "guard", "control", "cargo", "water", "repair", "negotiation", "navigation"})),
    ChallengeFamily("environment", "fire, flood, smoke, bad ground or support failure changes the route", frozenset({"water", "fire", "smoke", "structure", "terrain", "weather", "treatment", "cargo", "guard"})),
)


def _text_tags(text: str) -> set[str]:
    lower = text.lower()
    vocabulary = {
        "control": ("hook", "pull", "pin", "restraint", "entangle", "knockback", "forced movement", "positional control", "drive"),
        "guard": ("guard", "brace", "shield", "counter-posture", "safer"),
        "structure": ("support", "structure", "floor", "shutter", "machinery", "timber", "break"),
        "anti-armour": ("armour", "elite", "protector", "machinery", "severe"),
        "quiet": ("quiet", "silent", "noise"),
        "mobility": ("mobile", "movement", "advance", "withdraw", "step", "descent", "fall", "approach", "breach", "open a marked"),
        "water": ("water", "flood", "river", "ebb", "current", "brine", "tide"),
        "fire": ("fire", "pitch", "burn", "charcoal", "spark", "heat"),
        "smoke": ("smoke", "ash", "dust"),
        "terrain": ("terrain", "mud", "scree", "ice", "footing", "ground", "height", "elevation", "roof"),
        "weather": ("weather", "rain", "wind", "storm", "winter", "crosswind"),
        "cargo": ("cargo", "load", "porter", "lot", "market"),
        "negotiation": ("negotiat", "terms", "trade", "witness", "account", "confidence"),
        "treatment": ("heal", "field care", "binding", "dressing", "injury"),
        "repair": ("repair", "carpenter", "craft", "brace", "lever"),
        "navigation": ("route", "survey", "sightline", "forecast", "ebb", "wind"),
        "animal": ("animal", "boar", "beast", "charge"),
        "recovery": ("recover", "actual readied", "physical shaft", "retriev"),
        "preparation": ("aim", "reload", "wind-up", "setup", "prepared"),
    }
    return {tag for tag, words in vocabulary.items() if any(word in lower for word in words)}


def weapon_tactical_roles(name: str) -> frozenset[str]:
    """Classify authored mechanics, never nominal damage numbers alone."""
    description = WEAPONS[name][1]
    roles = _text_tags(description)
    reach = WORK_WEAPONS[name].reach if name in WORK_WEAPONS else WEAPON_RANGES.get(name, 1)
    minimum = WORK_WEAPONS[name].minimum if name in WORK_WEAPONS else 1
    if name in RANGED_WEAPONS or reach >= 4:
        roles.add("range")
    if minimum > 1 or "reach" in description or "pace" in description:
        roles.add("spacing")
    roles.add("damage")
    return frozenset(roles)


def build_capabilities(build: BuildScenario) -> frozenset[str]:
    """Project only effects named by real production content and reducers."""
    material = " ".join((
        WEAPONS[build.weapon][1], build.gear, build.support, build.technique,
        " ".join(build.passives), build.expected_combo, build.decision,
        build.production_reducer,
    ))
    tags = set(weapon_tactical_roles(build.weapon)) | _text_tags(material)
    gear_tags = {
        "buckler": {"guard"}, "rope": {"water", "mobility", "control", "navigation"},
        "quiet shoes": {"quiet", "terrain", "mobility"},
        "repair tools": {"repair", "structure"}, "smoke pot": {"smoke", "control"},
        "cargo harness": {"cargo", "guard"}, "trade seals": {"negotiation", "cargo"},
        "hooded lantern": {"fire", "animal", "navigation"},
    }
    support_tags = {
        "route survey": {"navigation", "quiet", "terrain"},
        "field care": {"treatment", "guard"},
        "porter watch": {"cargo", "guard"},
        "carpenter rig": {"repair", "structure", "water"},
        "factor surety": {"negotiation", "cargo"},
    }
    tags.update(gear_tags.get(build.gear, set()))
    tags.update(support_tags.get(build.support, set()))
    if build.elevation:
        tags.update({"terrain", "range"})
    if build.guarded:
        tags.add("guard")
    if build.burdened:
        tags.add("cargo")
    return frozenset(tags)


def build_balance_audit() -> dict[str, object]:
    coverage: dict[str, dict[str, list[str]]] = {}
    challenge_counts: Counter[str] = Counter()
    failures: list[str] = []
    for build in BUILD_SCENARIOS:
        capabilities = build_capabilities(build)
        answers: dict[str, list[str]] = {}
        for challenge in CHALLENGES:
            matched = sorted(capabilities & challenge.answers)
            answers[challenge.id] = matched
            if matched:
                challenge_counts[challenge.id] += 1
            else:
                failures.append(f"{build.id} has no production answer to {challenge.id}")
        coverage[build.id] = {
            "capabilities": sorted(capabilities),
            "answers": answers,
            "decision": [build.decision],
        }
    for challenge in CHALLENGES:
        if challenge_counts[challenge.id] < 6:
            failures.append(f"{challenge.id} is answered by only {challenge_counts[challenge.id]} builds")

    weapon_roles = {name: sorted(weapon_tactical_roles(name)) for name in WEAPONS}
    for name, roles in weapon_roles.items():
        non_damage = set(roles) - {"damage"}
        if not non_damage:
            failures.append(f"{name} has no tactical purpose beyond damage")
    signatures = Counter(tuple(roles) for roles in weapon_roles.values())
    return {
        "builds": len(BUILD_SCENARIOS),
        "challenge_families": len(CHALLENGES),
        "challenge_coverage": dict(sorted(challenge_counts.items())),
        "coverage": coverage,
        "weapons": len(WEAPONS),
        "weapon_roles": weapon_roles,
        "weapon_role_signatures": len(signatures),
        "largest_shared_weapon_signature": max(signatures.values(), default=0),
        "failures": failures,
    }


def validate_build_balance() -> None:
    failures = build_balance_audit()["failures"]
    if failures:
        raise ValueError("; ".join(failures))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    result = build_balance_audit()
    if args.json:
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        print(f"{result['builds']} builds x {result['challenge_families']} pressures; {result['weapons']} weapons")
        print("challenge coverage:", result["challenge_coverage"])
        print("failures:", result["failures"] or "none")
    raise SystemExit(bool(result["failures"]))


if __name__ == "__main__":
    main()
