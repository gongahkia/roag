"""Typed persistent-effect operands and strictly validated integer stack rules."""

from dataclasses import dataclass
from enum import StrEnum
from fractions import Fraction
from functools import lru_cache

from .stacks import StackMode, StackRule


class EffectKey(StrEnum):
    ADRENAL_BLOCK = "adrenal_block"
    BOON_OFFER_CHOICES = "boon_offer_choices"
    COMBAT_VICTORY_HEAL = "combat_victory_heal"
    COUNTERCURRENT_DRAW = "countercurrent_draw"
    CURSE_DEAD_DRAW = "curse_dead_draw"
    CURSE_DRAW_ENERGY = "curse_draw_energy"
    CURSE_DRAW_MOVE = "curse_draw_move"
    CURSE_DRAW_STRESS = "curse_draw_stress"
    CURSE_DRAW_WOUND = "curse_draw_wound"
    CURSE_HELD_STRESS = "curse_held_stress"
    DAMAGE_DISCARD = "damage_discard"
    DAMAGE_DRAW = "damage_draw"
    DEATH_CHANCE_REDUCTION = "death_chance_reduction"
    DEFLECTION = "deflection"
    FIRST_BLOCK_COST_INCREASE = "first_block_cost_increase"
    FIRST_CARD_COST_INCREASE = "first_card_cost_increase"
    FIRST_ROUND_ENERGY = "first_round_energy"
    FOCUS_DRAW = "focus_draw"
    FORCED_MOVE_BONUS = "forced_move_bonus"
    FORCED_MOVE_REDUCTION = "forced_move_reduction"
    HEALING_BONUS = "healing_bonus"
    HEALING_REDUCTION = "healing_reduction"
    INCOMING_DAMAGE_BONUS = "incoming_damage_bonus"
    INCOMING_DAMAGE_REDUCTION = "incoming_damage_reduction"
    LEAKING_LIGHT = "leaking_light"
    MARKED_DAMAGE_BONUS = "marked_damage_bonus"
    MERCY_BLOCK = "mercy_block"
    NIGHT_TERROR_STRESS = "night_terror_stress"
    OPENING_HAND = "opening_hand"
    PATROL_AGGRESSION_REDUCTION = "patrol_aggression_reduction"
    QUICK_HANDS = "quick_hands"
    RESERVE_ENERGY = "reserve_energy"
    RESONANT_ENERGY = "resonant_energy"
    REWARD_CHOICES = "reward_choices"
    SALVAGE_COPIES = "salvage_copies"
    SCAVENGER_STRESS = "scavenger_stress"
    SECOND_WIND = "second_wind"
    STACKED_START_BLOCK = "stacked_start_block"
    START_BLOCK = "start_block"
    START_DODGE = "start_dodge"
    START_MARKED = "start_marked"
    START_STRESS_RELIEF = "start_stress_relief"
    START_VULNERABLE = "start_vulnerable"
    STRESS_BONUS = "stress_bonus"
    STRESS_REDUCTION = "stress_reduction"
    STRESSED_DAMAGE_BONUS = "stressed_damage_bonus"
    SUPPLY_HEAL_BONUS = "supply_heal_bonus"
    SUPPLY_LIGHT_BONUS = "supply_light_bonus"
    SURVEY_REACH = "survey_reach"
    WOUND_REDUCTION = "wound_reduction"


class Unit(StrEnum):
    COUNT = "count"
    BASIS_POINTS = "basis_points"


FRACTIONAL_KEYS = frozenset({"death_chance_reduction", "healing_bonus", "healing_reduction",
                           "incoming_damage_bonus", "incoming_damage_reduction", "marked_damage_bonus",
                           "stress_bonus", "stress_reduction", "stressed_damage_bonus"})


@dataclass(frozen=True)
class PersistentEffectContract:
    key: EffectKey
    unit: Unit
    stack: StackRule

    def value(self, count: int) -> int | Fraction:
        value = self.stack.value(count)
        if self.stack.mode == StackMode.MULTIPLICATIVE and count:
            value -= 10000
        return Fraction(value, 10000) if self.unit == Unit.BASIS_POINTS else value

    def display(self, value: int) -> str:
        if self.unit == Unit.COUNT:
            return str(value)
        if self.stack.mode == StackMode.MULTIPLICATIVE:
            return f"{value // 10000}.{value % 10000:04d}x"
        whole, fraction = divmod(value, 100)
        return f"{whole}.{fraction:02d}%"


@lru_cache(maxsize=512)
def _compiled(key, unit, mode, amount, cap, every, table, bound, monotonic, converted_from):
    return PersistentEffectContract(EffectKey(key), Unit(unit),
                                    StackRule(StackMode(mode), amount, cap, every, table, bound, monotonic, converted_from))


def persistent_effect(raw: dict) -> PersistentEffectContract:
    if not isinstance(raw, dict) or set(raw) != {"key", "unit", "stack"}:
        raise ValueError("persistent effects require exactly key, unit and stack")
    key, unit = EffectKey(raw["key"]), Unit(raw["unit"])
    if (unit == Unit.BASIS_POINTS) != (key.value in FRACTIONAL_KEYS):
        raise ValueError("persistent effect unit contradicts its registered opcode")
    rule = raw["stack"]
    if not isinstance(rule, dict) or set(rule) - set(StackRule.__dataclass_fields__) or not {"mode", "amount"} <= rule.keys():
        raise ValueError("unknown or missing stack policy fields")
    mode = StackMode(rule["mode"])
    if mode in {StackMode.MULTIPLICATIVE, StackMode.INDEPENDENT_CHANCE} and unit != Unit.BASIS_POINTS:
        raise ValueError("ratio and chance policies require basis points")
    if "every" in rule and mode != StackMode.THRESHOLD:
        raise ValueError("only threshold policies accept every")
    if mode == StackMode.TABLE and rule["amount"] != 0:
        raise ValueError("table amounts belong in the table, not an unused scalar")
    for field in ("amount", "cap"):
        value = rule.get(field)
        if value is not None and (type(value) is not int or not 0 <= value <= 1_000_000_000):
            raise ValueError("authored stack magnitudes must be integers in 0..1000000000")
    table = rule.get("table", [])
    if not isinstance(table, (list, tuple)) or len(table) > 65:
        raise ValueError("authored stack tables support at most 65 entries")
    if any(type(value) is not int or not 0 <= value <= 1_000_000_000 for value in table):
        raise ValueError("authored table values must be integers in 0..1000000000")
    result = _compiled(key, unit, mode, rule["amount"], rule.get("cap"), rule.get("every", 1), tuple(table),
                       rule.get("max_effective_stacks"), rule.get("monotonic", True), rule.get("converted_from"))
    if mode == StackMode.MULTIPLICATIVE and result.stack.cap is not None and result.stack.cap < 10000 + result.stack.amount:
        raise ValueError("multiplicative cap cannot contradict the first-stack factor")
    return result
