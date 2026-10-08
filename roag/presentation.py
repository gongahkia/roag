"""Transient presentation state that never participates in simulation."""

from __future__ import annotations

from dataclasses import dataclass
import hashlib
import os

from .state import Position


PRESENTATION_FRAME_MS = 100
WATER_GLYPHS = ("~", "≈", "≋", "≈")
_DISABLED_VALUES = frozenset({"0", "false", "no", "off"})


def presentation_enabled() -> bool:
    """Return whether the terminal should use timed presentation frames."""
    return os.environ.get("ROAG_PRESENTATION", "on").strip().lower() not in _DISABLED_VALUES


@dataclass
class EffectState:
    """UI-owned clock and transient effect state for one play session.

    It deliberately contains no ``GameState`` reference and is never saved.
    Later presentation effects can grow here without becoming simulation input.
    """

    enabled: bool = True
    elapsed_ms: int = 0
    seed_offset: int = 0

    @classmethod
    def for_seed(cls, seed: str, *, enabled: bool = True) -> "EffectState":
        digest = hashlib.blake2b(seed.encode("utf-8"), digest_size=2).digest()
        return cls(enabled=enabled, seed_offset=int.from_bytes(digest, "big"))

    @property
    def frame_index(self) -> int:
        return self.elapsed_ms // PRESENTATION_FRAME_MS

    def advance(self, milliseconds: int = PRESENTATION_FRAME_MS) -> None:
        if milliseconds < 0:
            raise ValueError("presentation time cannot move backwards")
        if self.enabled:
            self.elapsed_ms += milliseconds

    def glyph(self, authoritative_glyph: str, position: Position) -> str:
        """Compose an ambient glyph without changing authoritative terrain."""
        if not self.enabled or authoritative_glyph != "~":
            return authoritative_glyph
        offset = self.seed_offset + position.x * 3 + position.y * 5 + position.z * 7
        return WATER_GLYPHS[(self.frame_index + offset) % len(WATER_GLYPHS)]
