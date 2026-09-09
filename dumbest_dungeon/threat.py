"""Integer threat-vector contracts. Generation calibration supplies the limits."""

from __future__ import annotations

from dataclasses import asdict, dataclass


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
