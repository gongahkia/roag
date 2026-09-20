"""Load inert main-world content from the selected content pack."""

from __future__ import annotations

from dataclasses import dataclass
import json
from importlib.resources import files
import os
from pathlib import Path
import re
from typing import Any


VESSEL_SECTIONS = (
    "voyages", "hazard_stations", "variants", "echoes", "refits",
    "region_nodes", "route_nodes", "route_edges", "optional_route_edges",
    "drinks",
)
ACTOR_SECTIONS = (
    "ENEMY_ARCHETYPES", "LEGACY_STANDARD_GLYPHS", "FRONTIER_ACTORS",
    "EXPANDED_STANDARD_ACTORS", "FRONTIER_ELITES", "STANDARD_REACTIONS",
)
GEOGRAPHY_SECTIONS = (
    "SIDE_ROUTES", "GROUND_PATCHES", "FIELD_SECRETS", "FRONTIERS",
)
HISTORY_SECTIONS = (
    "working_accounts", "institution_services", "institution_ties",
    "network_accounts", "network_contacts", "undertakings", "legend_bases",
    "crisis_tags",
)
PRACTICE_SECTIONS = ("practices", "network_contacts", "aftermath_regions")
CHARACTER_SECTIONS = (
    "competencies", "starting_points", "ancestries", "origins", "traits",
    "origin_practices", "attribute_competencies", "role_attributes",
    "role_competencies",
)
ARC_RELIC_SECTIONS = ("relics",)
RECRUITMENT_SECTIONS = ("requirements",)
WORLD_TEXT_SECTIONS = (
    "JOMON_MAP", "HELP_LINES", "seed_words", "terrain_names", "landmark_labels",
    "interface_ledgers", "interface_labels",
)
AFTERMATH_SECTIONS = (
    "lines", "topologies", "drainage_topologies", "fire_topologies",
    "support_topologies", "recovery_topologies", "preparations",
    "household_stories", "interferences",
)
EQUIPMENT_SECTIONS = (
    "ammunition_items", "weapon_ammunition", "basic_courier_loadouts",
    "basic_courier_armour", "item_specs", "regional_armour", "work_weapons",
    "pot_ammunition", "arsenal", "bomb_ammunition", "fittings",
    "enemy_regional_armour",
)
VISUAL_SECTIONS = (
    "entity_glyphs", "semantic_roles", "regional_ground_roles",
    "regional_tile_roles", "physical_role_overrides", "route_node_symbols",
    "material_overlay_symbols", "site_symbols", "tavern_cards",
    "tavern_dice", "vessel_levels", "tavern_map",
)

CONTENT_PACK_FORMAT = 1
CONTENT_PACK_ENVIRONMENT = "JOMON_CONTENT_PACK"

# Main-world modules load these files into module constants. Selecting a pack
# therefore validates that it is complete before any one catalog is consumed.
REQUIRED_CATALOGS = (
    "actors.json", "aftermath.json", "arc_relics.json", "character_profiles.json",
    "chemistry.json", "circuits.json", "equipment.json", "field_reports.json",
    "geography.json", "goods.json", "history.json", "people.json", "practices.json",
    "production.json", "quests.json", "recruitment.json", "sanctums.json",
    "situations.json", "skills.json", "spells.json", "terrain_variation.json",
    "vehicles.json", "vessel.json", "visuals.json", "world_text.json",
)


class CatalogError(ValueError):
    pass


class ContentPackError(CatalogError):
    """Raised when a main-world content pack cannot be selected safely."""


@dataclass(frozen=True)
class ContentPack:
    """Immutable location and identity for one validated main-world pack."""

    id: str
    display_name: str
    format_version: int
    root: Path
    catalog_root: Path

    def catalog_path(self, name: str) -> Path:
        return self.catalog_root / name


_selected_pack: ContentPack | None = None
_catalogs_loaded = False


def _unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result = {}
    for key, value in pairs:
        if key in result:
            raise CatalogError(f"duplicate catalog key: {key}")
        result[key] = value
    return result


def _reject_constant(value: str) -> None:
    raise CatalogError(f"non-finite catalog number: {value}")


def _package_path(*parts: str) -> Path:
    return Path(str(files("jomon").joinpath(*parts)))


def _manifest_document(root: Path) -> dict[str, Any]:
    source = root / "manifest.json"
    try:
        text = source.read_text(encoding="utf-8")
    except OSError as exc:
        raise ContentPackError(f"cannot read content-pack manifest at {source}: {exc}") from exc
    try:
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid content-pack manifest at {source}: {exc}") from exc
    if not isinstance(document, dict):
        raise ContentPackError(f"invalid content-pack manifest at {source}: expected an object")
    required = {"id", "display_name", "format_version"}
    allowed = required | {"catalog_root"}
    if set(document) - allowed or required - set(document):
        missing = sorted(required - set(document))
        unknown = sorted(set(document) - allowed)
        details = []
        if missing:
            details.append("missing " + ", ".join(missing))
        if unknown:
            details.append("unknown " + ", ".join(unknown))
        raise ContentPackError(f"invalid content-pack manifest at {source}: " + "; ".join(details))
    return document


def load_content_pack(path: str | Path) -> ContentPack:
    """Load one complete external or bundled pack without selecting it."""
    root = Path(path).expanduser()
    try:
        root = root.resolve(strict=True)
    except OSError as exc:
        raise ContentPackError(f"cannot resolve content pack at {root}: {exc}") from exc
    if not root.is_dir():
        raise ContentPackError(f"content pack at {root} is not a directory")
    document = _manifest_document(root)
    pack_id = document["id"]
    display_name = document["display_name"]
    format_version = document["format_version"]
    if not isinstance(pack_id, str) or re.fullmatch(r"[a-z][a-z0-9_.-]*", pack_id) is None:
        raise ContentPackError(f"invalid content-pack manifest at {root / 'manifest.json'}: invalid id")
    if not isinstance(display_name, str) or not display_name.strip():
        raise ContentPackError(f"invalid content-pack manifest at {root / 'manifest.json'}: invalid display_name")
    if type(format_version) is not int or format_version != CONTENT_PACK_FORMAT:
        raise ContentPackError(
            f"invalid content-pack manifest at {root / 'manifest.json'}: "
            f"format_version must be {CONTENT_PACK_FORMAT}"
        )
    catalog_root_value = document.get("catalog_root", "data")
    if not isinstance(catalog_root_value, str) or not catalog_root_value:
        raise ContentPackError(f"invalid content-pack manifest at {root / 'manifest.json'}: invalid catalog_root")
    catalog_root_path = Path(catalog_root_value)
    if catalog_root_path.is_absolute():
        raise ContentPackError(f"invalid content-pack manifest at {root / 'manifest.json'}: catalog_root must be relative")
    catalog_root = (root / catalog_root_path).resolve()
    if not catalog_root.is_dir():
        raise ContentPackError(f"content pack {pack_id!r} at {root}: catalog root {catalog_root} is not a directory")
    missing_catalogs = [name for name in REQUIRED_CATALOGS if not (catalog_root / name).is_file()]
    if missing_catalogs:
        raise ContentPackError(
            f"content pack {pack_id!r} at {root}: missing required catalog "
            + ", ".join(missing_catalogs)
        )
    return ContentPack(pack_id, display_name, format_version, root, catalog_root)


def bundled_default_pack() -> ContentPack:
    """Return the shipped pack, whose catalogs remain in ``jomon/data`` for now."""
    return load_content_pack(_package_path("content_packs", "default"))


def selected_content_pack() -> ContentPack:
    """Return the pack selected before the first main-world catalog is loaded."""
    global _selected_pack
    if _selected_pack is None:
        override = os.environ.get(CONTENT_PACK_ENVIRONMENT)
        _selected_pack = load_content_pack(override) if override else bundled_default_pack()
    return _selected_pack


def select_content_pack_from_environment() -> ContentPack:
    """Establish environment selection during startup before importing ``main``."""
    return selected_content_pack()


def select_content_pack(path: str | Path | None = None) -> ContentPack:
    """Explicitly select a pack before catalog-consuming modules are imported."""
    global _selected_pack
    requested = bundled_default_pack() if path is None else load_content_pack(path)
    if _catalogs_loaded and _selected_pack != requested:
        selected = _selected_pack.id if _selected_pack else "unknown"
        raise ContentPackError(
            f"cannot select content pack {requested.id!r} after catalogs loaded from {selected!r}; "
            "select it before importing main-world content"
        )
    _selected_pack = requested
    return requested


def decode_catalog(text: str, name: str, sections: tuple[str, ...]) -> dict[str, Any]:
    try:
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (ValueError, RecursionError) as exc:
        raise CatalogError(f"invalid {name}: {exc}") from exc
    if not isinstance(document, dict) or set(document) != set(sections):
        raise CatalogError(f"{name} must contain exactly {', '.join(sections)}")
    return document


def load_catalog(name: str, sections: tuple[str, ...]) -> dict[str, Any]:
    global _catalogs_loaded
    if not name.endswith(".json") or "/" in name or "\\" in name:
        raise CatalogError("catalog names must be local JSON files")
    pack = selected_content_pack()
    source = pack.catalog_path(name)
    try:
        text = source.read_text(encoding="utf-8")
    except OSError as exc:
        raise CatalogError(f"cannot read {name} from content pack {pack.id!r} at {source}: {exc}") from exc
    _catalogs_loaded = True
    try:
        return decode_catalog(text, name, sections)
    except CatalogError as exc:
        raise CatalogError(f"invalid {name} in content pack {pack.id!r} at {source}: {exc}") from exc
