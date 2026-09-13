"""Integer stack policies with exact current/next results and authored bounds."""

from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum


class StackMode(StrEnum):
    LINEAR = "linear"
    MULTIPLICATIVE = "multiplicative"
    INDEPENDENT_CHANCE = "independent_chance"
    HYPERBOLIC = "hyperbolic"
    THRESHOLD = "threshold"
    REFRESH = "duration_refresh"
    UNIQUE = "unique"
    CONVERSION = "conversion"
    TABLE = "table"


@dataclass(frozen=True)
class StackRule:
    mode: StackMode
    amount: int
    cap: int | None = None
    every: int = 1
    table: tuple[int, ...] = ()
    max_effective_stacks: int | None = None
    monotonic: bool = True
    converted_from: str | None = None

    def __post_init__(self) -> None:
        if not isinstance(self.mode, StackMode) or type(self.amount) is not int or self.amount < 0:
            raise ValueError("stack policy requires a registered mode and nonnegative integer amount")
        if self.cap is not None and (type(self.cap) is not int or self.cap < 0):
            raise ValueError("stack cap must be a nonnegative integer")
        if type(self.every) is not int or self.every < 1 or type(self.monotonic) is not bool:
            raise ValueError("invalid threshold or monotonic promise")
        if self.max_effective_stacks is not None and (type(self.max_effective_stacks) is not int or not 1 <= self.max_effective_stacks <= 10000):
            raise ValueError("effective-stack bound must be an authored integer in 1..10000")
        if self.mode in {StackMode.MULTIPLICATIVE, StackMode.INDEPENDENT_CHANCE} and self.max_effective_stacks is None:
            raise ValueError("exponential stack policies must disclose an effective-stack bound")
        if self.mode == StackMode.INDEPENDENT_CHANCE and self.amount > 10000:
            raise ValueError("chance is expressed in 0..10000 basis points")
        if self.mode == StackMode.HYPERBOLIC and (self.cap is None or self.cap < self.amount or self.amount == 0):
            raise ValueError("hyperbolic policy requires a positive first stack and soft cap")
        if not isinstance(self.table, tuple) or any(type(value) is not int or value < 0 for value in self.table):
            raise ValueError("stack table must contain nonnegative integers")
        if self.mode == StackMode.TABLE:
            if not self.table or self.table[0] != 0:
                raise ValueError("authored tables start with the zero-stack result")
            if self.monotonic and any(after < before for before, after in zip(self.table, self.table[1:])):
                raise ValueError("stack table contradicts its monotonic promise")
        elif self.table:
            raise ValueError("only table policies accept a table")
        if (self.mode == StackMode.CONVERSION and (not isinstance(self.converted_from, str) or not self.converted_from)
            or self.mode != StackMode.CONVERSION and self.converted_from is not None):
            raise ValueError("conversion policy must disclose the retired stack identity")

    def value(self, count: int) -> int:
        if type(count) is not int or count < 0:
            raise ValueError("stack count must be a nonnegative integer")
        count = min(count, self.max_effective_stacks) if self.max_effective_stacks else count
        if count == 0:
            return 0
        if self.mode in {StackMode.LINEAR, StackMode.CONVERSION}:
            result = self.amount * count
        elif self.mode == StackMode.MULTIPLICATIVE:
            result = (10000 + self.amount) ** count // 10000 ** (count - 1)
        elif self.mode == StackMode.INDEPENDENT_CHANCE:
            result = 10000 - ((10000 - self.amount) ** count + 10000 ** (count - 1) - 1) // 10000 ** (count - 1)
        elif self.mode == StackMode.HYPERBOLIC:
            result = self.cap * self.amount * count // (self.cap + self.amount * (count - 1))
        elif self.mode == StackMode.THRESHOLD:
            result = self.amount * (count // self.every)
        elif self.mode in {StackMode.REFRESH, StackMode.UNIQUE}:
            result = self.amount
        else:
            result = self.table[min(count, len(self.table) - 1)]
        return min(result, self.cap) if self.cap is not None else result

    def preview(self, count: int) -> dict[str, int | str | bool | None]:
        current, following = self.value(count), self.value(count + 1)
        return {"mode": self.mode.value, "count": count, "current": current, "next": following,
                "change": following - current, "cap": self.cap, "soft_cap": self.mode == StackMode.HYPERBOLIC,
                "effective_stack_bound": self.max_effective_stacks,
                "converted_from": self.converted_from, "formula": self.formula()}

    def formula(self) -> str:
        formulas = {
            StackMode.LINEAR: f"{self.amount} x stacks",
            StackMode.MULTIPLICATIVE: f"floor(10000 x (1 + {self.amount}/10000)^stacks) basis points",
            StackMode.INDEPENDENT_CHANCE: f"floor(10000 x (1 - (1 - {self.amount}/10000)^stacks)) basis points",
            StackMode.HYPERBOLIC: f"floor({self.cap} x {self.amount} x stacks / ({self.cap} + {self.amount} x (stacks - 1)))",
            StackMode.THRESHOLD: f"{self.amount} per {self.every} stacks",
            StackMode.REFRESH: f"refresh duration to {self.amount}; do not add duration",
            StackMode.UNIQUE: f"unique rule: {self.amount} while present",
            StackMode.CONVERSION: f"replace {self.converted_from}; {self.amount} per converted stack",
            StackMode.TABLE: " / ".join(map(str, self.table)),
        }
        return formulas[self.mode]
