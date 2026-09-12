"""Cumulative global difficulty ladder with one disclosed rule per rank."""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class LadderRule:
    rank: int
    name: str
    text: str
    modifier: str
    amount: int


RULES = (
    LadderRule(1, "Rising Static", "All simulated pressure gains are 3% larger.", "pressure_bp", 300),
    LadderRule(2, "Long Corridors", "Every travel action adds 1 pressure.", "travel_pressure_flat", 1),
    LadderRule(3, "Dim Departure", "Begin with 5 less light.", "starting_light", -5),
    LadderRule(4, "Thin Stores", "Begin with 1 fewer supply.", "starting_supplies", -1),
    LadderRule(5, "Known Intruders", "Encounter direction starts at 120 pressure.", "pressure_floor", 120),
    LadderRule(6, "Elite Plating", "Elite health gains 2%.", "elite_health_bp", 200),
    LadderRule(7, "Hunting Orders", "Patrol aggression increases by 1 tile.", "patrol_aggression", 1),
    LadderRule(8, "Altered Elite", "Elites gain one compatible mutation slot.", "elite_mutation_slots", 1),
    LadderRule(9, "Hardened Line", "Normal-enemy health gains 2%.", "normal_health_bp", 200),
    LadderRule(10, "Live Ammunition", "Enemy damage gains 2%.", "enemy_damage_bp", 200),
    LadderRule(11, "Compound Mutation", "Every encounter gains one compatible mutation slot.", "mutation_slots", 1),
    LadderRule(12, "Reserve Doctrine", "Eligible encounters gain one reinforcement ticket.", "reinforcement_tickets", 1),
    LadderRule(13, "Loud Objectives", "Each objective stage adds 3 pressure.", "objective_pressure_flat", 3),
    LadderRule(14, "Regional Reach", "Biome hazards extend by one step.", "hazard_reach", 1),
    LadderRule(15, "Fast Patrols", "Patrol cadence shortens by one tick.", "patrol_cadence", 1),
    LadderRule(16, "Watched Arrival", "Encounter direction starts at WATCHFUL pressure.", "pressure_floor", 120),
    LadderRule(17, "Guardian Reserve", "Guardian health gains 3%.", "guardian_health_bp", 300),
    LadderRule(18, "Apex Mutation", "Final bosses gain one compatible mutation slot.", "boss_mutation_slots", 1),
    LadderRule(19, "Apex Output", "Final-boss damage gains 3%.", "boss_damage_bp", 300),
    LadderRule(20, "Last Protocol", "Boss phase effects are 25% stronger.", "boss_phase_effect_bp", 2500),
)


def validate_rules() -> None:
    if tuple(rule.rank for rule in RULES) != tuple(range(1, 21)):
        raise ValueError("ladder must contain exactly ranks 1..20")
    if any(not rule.name or not rule.text or not rule.modifier or type(rule.amount) is not int for rule in RULES):
        raise ValueError("ladder rules require concise typed modifiers")


def modifier(rank: int, key: str) -> int:
    if type(rank) is not int or not 0 <= rank <= 20:
        raise ValueError("ladder rank must be 0..20")
    return sum(rule.amount for rule in RULES[:rank] if rule.modifier == key)


validate_rules()
