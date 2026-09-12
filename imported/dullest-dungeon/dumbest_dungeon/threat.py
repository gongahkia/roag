"""Integer encounter threat estimates, composition costs, and dimensional limits."""

from __future__ import annotations

from dataclasses import asdict, dataclass
from typing import Any, Iterable


@dataclass(frozen=True)
class Threat:
    durability: int = 0
    sustained: int = 0
    burst: int = 0
    control: int = 0
    sustain: int = 0
    reach: int = 0
    tempo: int = 0

    def __post_init__(self) -> None:
        if any(type(value) is not int or value < 0 for value in asdict(self).values()):
            raise ValueError("threat dimensions must be nonnegative integers")

    def __add__(self, other: Threat) -> Threat:
        if not isinstance(other, Threat):
            return NotImplemented
        return Threat(**{name: value + getattr(other, name) for name, value in asdict(self).items()})

    def within(self, ceilings: Threat) -> bool:
        return all(value <= getattr(ceilings, name) for name, value in asdict(self).items())

    @property
    def total(self) -> int:
        return sum(asdict(self).values())


@dataclass(frozen=True)
class ThreatBudget:
    total: int
    ceilings: Threat

    def __post_init__(self) -> None:
        if type(self.total) is not int or self.total < 0 or not isinstance(self.ceilings, Threat):
            raise ValueError("invalid encounter threat budget")

    def permits(self, threat: Threat) -> bool:
        return threat.total <= self.total and threat.within(self.ceilings)


THREAT_BUDGETS = {
    "normal": ThreatBudget(205, Threat(65, 58, 34, 20, 32, 15, 22)),
    "elite": ThreatBudget(330, Threat(108, 72, 52, 28, 48, 18, 34)),
}

CONTROL_VALUES = {
    "marked": 1,
    "weak": 2,
    "vulnerable": 2,
    "wound": 2,
    "stun": 4,
}


def _crew_target_width(target: str) -> int:
    return 4 if target == "all_heroes" else 1


def enemy_threat(definition: dict[str, Any]) -> Threat:
    """Estimate three expected phases and one credible opening phase."""
    actions = definition["actions"]
    total_weight = sum(int(action["weight"]) for action in actions)
    weighted_damage = 0
    burst = control = sustain = reach = tempo = 0
    for action in actions:
        coverage = _crew_target_width(action["target"])
        damage = sum(
            int(effect.get("amount", 0)) + int(effect.get("bonus", 0))
            for effect in action["effects"]
            if effect["op"] == "damage"
        ) * coverage
        weighted_damage += int(action["weight"]) * damage
        burst = max(burst, damage)
        action_control = sum(
            CONTROL_VALUES.get(effect.get("status"), 1) * int(effect.get("amount", 1))
            for effect in action["effects"]
            if effect["op"] == "status" and action["target"] != "self"
        )
        action_control += sum(
            2 * abs(int(effect.get("amount", 0)))
            for effect in action["effects"]
            if effect["op"] == "move"
        )
        control = max(control, action_control)
        action_sustain = sum(
            int(effect.get("amount", 0))
            for effect in action["effects"]
            if effect["op"] in {"heal", "block"}
        ) + sum(4 for effect in action["effects"] if effect["op"] == "guard")
        sustain = max(sustain, action_sustain)
        reach = max(
            reach,
            4 if action["target"] == "all_heroes"
            else 2 if action["target"] in {"back", "marked", "wounded", "stressed"}
            else 1,
        )
        tempo = max(tempo, (3 if damage else 0) + (2 if action_control else 0)
                    + (1 if action_sustain else 0))
    sustained = (3 * weighted_damage + total_weight - 1) // total_weight
    return Threat(
        durability=int(definition["max_hp"]), sustained=sustained, burst=burst,
        control=control, sustain=sustain, reach=reach, tempo=tempo,
    )


def formation_threat(catalog: Any, enemy_ids: Iterable[str]) -> Threat:
    """Price a formation plus readable coordination synergies."""
    identities = tuple(enemy_ids)
    definitions = [catalog.enemies[enemy_id] for enemy_id in identities]
    result = Threat()
    setups: set[str] = set()
    exploits: set[str] = set()
    controllers = 0
    has_guard = False
    has_striker = False
    for definition in definitions:
        result += enemy_threat(definition)
        local_control = False
        for action in definition["actions"]:
            targets_crew = action["target"] in {
                "front", "back", "all_heroes", "stressed", "deaths_door", "marked", "wounded"
            }
            for effect in action["effects"]:
                if effect["op"] == "status" and targets_crew:
                    setups.add(effect["status"])
                    local_control = True
                elif effect["op"] == "damage":
                    has_striker = True
                    if effect.get("bonus_status"):
                        exploits.add(effect["bonus_status"])
                elif effect["op"] in {"move", "guard"}:
                    local_control |= effect["op"] == "move"
                    has_guard |= effect["op"] == "guard"
        controllers += local_control

    addition = Threat()
    if setups & exploits:
        addition += Threat(sustained=6, burst=4, control=2, tempo=3)
    if has_guard and has_striker:
        addition += Threat(durability=3, sustain=5, tempo=2)
    if controllers > 1:
        addition += Threat(control=2 * (controllers - 1), tempo=controllers - 1)
    if len(identities) >= 3 and len(set(identities)) < len(identities):
        addition += Threat(burst=2, reach=1, tempo=1)
    return result + addition


def threat_budget(kind: str) -> ThreatBudget:
    try:
        return THREAT_BUDGETS[kind]
    except KeyError as exc:
        raise ValueError("threat budget kind must be normal or elite") from exc
