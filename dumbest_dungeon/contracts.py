"""Read-only runtime records for validated content; JSON never supplies code."""

from __future__ import annotations

from enum import StrEnum
from typing import Any


def _immutable(*args: Any, **kwargs: Any) -> None:
    raise TypeError("runtime content is immutable")


class FrozenList(list):
    __setitem__ = __delitem__ = __iadd__ = __imul__ = _immutable
    append = clear = extend = insert = pop = remove = reverse = sort = _immutable

    def __copy__(self):
        return self

    def __deepcopy__(self, memo):
        return self


class FrozenRecord(dict):
    __setitem__ = __delitem__ = __ior__ = _immutable
    clear = pop = popitem = setdefault = update = _immutable

    def __copy__(self):
        return self

    def __deepcopy__(self, memo):
        return self


def freeze(value: Any) -> Any:
    if isinstance(value, FrozenRecord | FrozenList):
        return value
    if isinstance(value, dict):
        return FrozenRecord((key, freeze(item)) for key, item in value.items())
    if isinstance(value, list):
        return FrozenList(freeze(item) for item in value)
    return value


class Opcode(StrEnum):
    DAMAGE = "damage"
    BLOCK = "block"
    HEAL = "heal"
    STRESS = "stress"
    MOVE = "move"
    GUARD = "guard"
    STATUS = "status"
    DRAW = "draw"
    DISCARD = "discard"
    ENERGY = "energy"
    CLEANSE = "cleanse"


class Target(StrEnum):
    ENEMY = "enemy"
    ALL_ENEMIES = "all_enemies"
    SELF = "self"
    ALLY = "ally"
    ALL_ALLIES = "all_allies"


class Effect(FrozenRecord):
    """A registered opcode with validated operands, compatible with existing lookups."""

    @property
    def opcode(self) -> Opcode:
        return Opcode(self["op"])

    @property
    def amount(self) -> int:
        return self.get("amount", 0)

    @property
    def target(self) -> Target | None:
        return Target(self["target"]) if "target" in self else None


class Definition(FrozenRecord):
    @property
    def id(self) -> str:
        return self["id"]

    @property
    def name(self) -> str:
        return self.get("name", self.id)


class Technique(Definition):
    @property
    def owner(self) -> str:
        return self["hero"]

    @property
    def cost(self) -> int:
        return self["cost"]

    @property
    def ranks(self) -> tuple[int, ...]:
        return tuple(self["from_ranks"])

    @property
    def effects(self) -> tuple[Effect, ...]:
        return tuple(self["effects"])


class EnemyAction(FrozenRecord):
    @property
    def effects(self) -> tuple[Effect, ...]:
        return tuple(self["effects"])


class Enemy(Definition):
    @property
    def max_hp(self) -> int:
        return self["max_hp"]

    @property
    def actions(self) -> tuple[EnemyAction, ...]:
        return tuple(self["actions"])


def effect_records(effects: list[dict]) -> FrozenList:
    return FrozenList(Effect((key, freeze(value)) for key, value in effect.items()) for effect in effects)


def runtime_definition(section: str, value: dict) -> Definition:
    fields = {key: freeze(item) for key, item in value.items()}
    if section == "cards":
        for key in ("effects", "upgrade_effects"):
            fields[key] = effect_records(value[key])
        return Technique(fields)
    if section == "enemies":
        fields["actions"] = FrozenList(
            EnemyAction({**action, "effects": effect_records(action["effects"])})
            for action in value["actions"]
        )
        return Enemy(fields)
    return Definition(fields)
