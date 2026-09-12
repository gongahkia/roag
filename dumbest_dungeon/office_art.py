"""Office-fantasy ASCII portraits used by the tavern's competitive expedition."""

from __future__ import annotations

from functools import lru_cache
import hashlib

from .content import load_catalog


OFFICE_SPRITES: dict[str, tuple[str, ...]] = {
    "warden": (" .---. ", " |o o| ", " /|M|\\ ", "  |T|  ", " _/ \\_ "),
    "engineer": (" .---. ", " |o o|=", "-|(W)| ", "  | |  ", " _/ \\_ "),
    "medic": (" .-+-. ", " |o o| ", " /|+|\\ ", "  | |  ", " _/ \\_ "),
    "scout": ("  ___  ", " /o o\\=", "<|@|>  ", "  /|   ", " _/ \\_ "),
    "breacher": (" .-o-. ", " |o o| ", " /|C|\\ ", "  |:|  ", " _/ \\_ "),
    "psion": (" .~~~. ", " |o o| ", "~( ? )~", "  | |  ", " _/ \\_ "),
    "quartermaster": (" .$$$. ", " |o o| ", "/[BOX]\\", "  | |  ", " _/ \\_ "),
    "operative": (" .---. ", " |. .| ", " /[C]\\ ", "  | |  ", " _/ \\_ "),
    "biologist": (" .-v-. ", " |o o| ", " /|Y|\\ ", "  | |  ", " _/ \\_ "),
    "synth": ("[=====]", "|o   o|", "|PRINT|", " |___| ", " _| |_ "),
    "duelist": (" .-=-. ", " |o o| ", " /|L|\\ ", "  | |  ", " _/ \\_ "),
    "artillerist": (" .---. ", " |o o| ", "/[PPT]\\", "  | |  ", " _/ \\_ "),
    "chaplain": ("  ^^^  ", " .o o. ", " /|!|\\ ", "  | |  ", " _/ \\_ "),
    "hacker": (" .---. ", " |0 0| ", "<[=+]=>", "  | |  ", " _/ \\_ "),
    "pilot": ("  ^v^  ", " |o o| ", "<|E|>  ", "  | |  ", " _/ \\_ "),
    "cryonaut": (" .***. ", " |o o| ", " /|*|\\ ", "  | |  ", " _/ \\_ "),
    "horticulturist": (" .vVv. ", " |o o| ", " /|Y|\\ ", "  /|\\  ", " _/ \\_ "),
    "foundryman": (" .###. ", " |o o| ", "O|C|O  ", "  | |  ", " _/ \\_ "),
    "reactor_saint": ("  $$$  ", " .o o. ", " /|%|\\ ", "  | |  ", " _/ \\_ "),
    "mycologist": (" .ooo. ", "(o o o)", " /|m|\\ ", "  | |  ", " _/ \\_ "),
    "diver": (" .---. ", "| o o |", "|FILE |", "  | |  ", " _/ \\_ "),
    "stormcaller": (" \\|+/  ", " .o o. ", "~|!|~  ", "  | |  ", " _/ \\_ "),
    "archivist": (" .---. ", " |o o| ", " /[A]\\ ", " _|_|_ ", "|_____|"),
    "voidwalker": (" '   ' ", "  |o|  ", "-/(O)\\-", "  / \\  ", " '   ' "),
    "bonewright": (" .---. ", " |o o| ", " /|#|\\ ", "  |H|  ", " _/ \\_ "),
}


def office_card_glyph(role: str) -> tuple[str, str, str]:
    """Keep the original three-line class-art footprint inside each card."""
    return tuple(line.center(9) for line in OFFICE_SPRITES[role][1:4])


OFFICE_COSTUME_WORDS = {
    "Acid": "Toner", "Arc": "Circuit", "Ballast": "Storage",
    "Bilge": "Basement", "Biomass": "Breakroom", "Bulkhead": "Partition",
    "Coolant": "Watercooler", "Deck": "Floor", "Dosimeter": "Timecard",
    "Frost": "Freezer", "Gene": "Policy", "Gravity": "Elevator",
    "Ion": "Power", "Nanite": "Toner", "Neural": "Memo",
    "Plasma": "Ink", "Rad": "Budget", "Reactor": "Budget",
    "Repair": "Staple", "Rime": "Freezer", "Scrap": "Paper",
    "Signal": "Memo", "Vent": "Duct", "Void": "Absence",
}


def office_costume_name(enemy_id: str) -> str:
    """Original enemy drawings are rival-department costumes, not PvE units."""
    name = load_catalog().enemies[enemy_id]["name"]
    return " ".join(OFFICE_COSTUME_WORDS.get(word, word) for word in name.split())


@lru_cache(maxsize=256)
def rival_costumes(world_seed: int) -> tuple[str, str, str, str]:
    catalog = load_catalog()
    ids = tuple(catalog.art["enemies"])
    offset = int.from_bytes(hashlib.sha256(f"office-costumes:{world_seed}".encode()).digest()[:8], "big") % len(ids)
    return tuple(ids[(offset + 31 * rank) % len(ids)] for rank in range(4))
