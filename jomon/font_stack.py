"""Frontend-local font discovery and semantic icon fallbacks.

The backend never imports this module.  It deliberately discovers only local
fonts: a player may provide BigBlueTerm, but Jomon never downloads a font at
runtime and remains usable with a standard monospace fallback.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any


BIG_BLUE_FAMILIES = (
    "BigBlueTerm Nerd Font Mono", "BigBlueTerm Nerd Font", "BigBlueTerminal Nerd Font Mono",
    "BigBlueTerminal Nerd Font", "BigBlueTerm", "BigBlueTerminal",
)
NERD_FAMILIES = (
    "Symbols Nerd Font Mono", "JetBrainsMono Nerd Font Mono", "FiraCode Nerd Font Mono",
    "Hack Nerd Font Mono", "Nerd Font",
)
MONOSPACE_FAMILIES = ("DejaVu Sans Mono", "Liberation Mono", "Consolas", "Monospace")


# Icons describe frontend concepts, never gameplay identity.  Their fallback
# labels keep the ASCII renderer legible on an unpatched or minimal font.
ASCII_ICONS: dict[str, tuple[str, str]] = {
    "courier": ("\uf4ff", "@"), "npc": ("\uf007", "NPC"), "threat": ("\uf6e2", "!"),
    "health": ("\uf004", "HP"), "armour": ("\uf132", "ARM"), "inventory": ("\uf466", "INV"),
    "equipment": ("\uf6a7", "EQ"), "weapon": ("\uf6bf", "WPN"), "ranged": ("\uf557", "RNG"),
    "ammunition": ("\uf135", "AMMO"), "magic": ("\uf0d0", "MAG"), "chemistry": ("\uf0c3", "CHEM"),
    "production": ("\uf085", "MAKE"), "preparation": ("\uf0f4", "PREP"), "circuit": ("\uf0e7", "CIR"),
    "vehicle": ("\uf1b9", "VEH"), "vessel": ("\uf21a", "SHIP"), "travel": ("\uf5a0", "GO"),
    "quest": ("\uf46d", "QUEST"), "warning": ("\uf071", "!"), "success": ("\uf00c", "OK"),
    "locked": ("\uf023", "LOCK"), "inspect": ("\uf002", "LOOK"), "interact": ("\uf0a9", "USE"),
    "save": ("\uf0c7", "SAVE"), "load": ("\uf2f1", "LOAD"), "tavern": ("\uf805", "TAVERN"),
    "draw": ("\uf2bb", "DRAW"), "dice": ("\uf522", "DICE"),
}


def _existing_path(value: str | Path | None) -> str | None:
    if value is None:
        return None
    path = Path(value).expanduser()
    return str(path) if path.is_file() else None


def _match(pygame: Any, families: tuple[str, ...]) -> str | None:
    for family in families:
        path = pygame.font.match_font(family)
        if path:
            return path
    return None


@dataclass(frozen=True)
class FontResolution:
    text_path: str | None
    icon_path: str | None
    text_source: str
    icon_source: str


class FontStack:
    """Small Pygame-only font stack with per-icon plain-text fallbacks."""
    def __init__(self, pygame: Any, size: int, *, font_path: str | Path | None = None,
                 icon_font_path: str | Path | None = None):
        self.pygame = pygame
        explicit_text = _existing_path(font_path)
        discovered_text = _match(pygame, BIG_BLUE_FAMILIES)
        text_path = explicit_text or discovered_text or _match(pygame, MONOSPACE_FAMILIES)
        explicit_icon = _existing_path(icon_font_path)
        icon_path = explicit_icon or _match(pygame, NERD_FAMILIES) or text_path
        self.resolution = FontResolution(
            text_path, icon_path,
            "explicit" if explicit_text else "BigBlueTerm" if discovered_text else "monospace fallback",
            "explicit" if explicit_icon else "Nerd Font" if icon_path and icon_path != text_path else "text fallback",
        )
        self.text = pygame.font.Font(text_path, size)
        self.icons = pygame.font.Font(icon_path, size) if icon_path else self.text

    def supports(self, glyph: str) -> bool:
        metrics = self.icons.metrics(glyph)
        return bool(metrics and metrics[0] is not None)

    def icon(self, identity: str) -> str:
        glyph, fallback = ASCII_ICONS[identity]
        return glyph if self.supports(glyph) else fallback

    def render(self, value: str, colour: tuple[int, int, int], *, icon: bool = False) -> Any:
        return (self.icons if icon else self.text).render(value, True, colour)
