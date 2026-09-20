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
REGION_CONTRACT_FORMAT = 1
REGION_PRESENTATION_FILE = "regions.json"
CHARACTER_PRESENTATION_FILE = "characters.json"

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
class RegionContractSlot:
    """One engine-owned semantic slot for regional presentation."""

    id: str
    engine_id: str


@dataclass(frozen=True)
class RegionPresentation:
    """Immutable authored presentation for one engine-owned regional slot."""

    id: str
    engine_id: str
    display_name: str
    route_label: str
    short_description: str


@dataclass(frozen=True)
class CharacterContractSlot:
    """One engine-owned slot for a persistent static character."""

    id: str
    engine_id: str
    role_id: str | None


@dataclass(frozen=True)
class RoleContractSlot:
    """One engine-owned role whose ID remains mechanical."""

    id: str
    engine_id: str


@dataclass(frozen=True)
class CharacterPresentation:
    """Immutable selected-pack presentation for one static character."""

    id: str
    engine_id: str
    display_name: str
    short_description: str
    initial_memory: str
    role_label: str = ""


@dataclass(frozen=True)
class RolePresentation:
    """Immutable selected-pack label for one mechanical role ID."""

    id: str
    engine_id: str
    display_label: str


@dataclass(frozen=True)
class ContentPack:
    """Immutable location and identity for one validated main-world pack."""

    id: str
    display_name: str
    format_version: int
    root: Path
    catalog_root: Path
    region_presentations: tuple[RegionPresentation, ...]
    character_presentations: tuple[CharacterPresentation, ...]
    role_presentations: tuple[RolePresentation, ...]
    household_background_template: str

    def catalog_path(self, name: str) -> Path:
        return self.catalog_root / name

    def region_presentation(self, engine_id: str) -> RegionPresentation:
        for presentation in self.region_presentations:
            if presentation.engine_id == engine_id:
                return presentation
        raise KeyError(f"unknown engine region id: {engine_id}")

    def character_presentation(self, semantic_id: str) -> CharacterPresentation:
        for presentation in self.character_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown character semantic id: {semantic_id}")

    def role_presentation(self, engine_id: str) -> RolePresentation:
        for presentation in self.role_presentations:
            if presentation.engine_id == engine_id:
                return presentation
        raise KeyError(f"unknown engine role id: {engine_id}")


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


def _content_contract_document() -> tuple[Path, dict[str, Any]]:
    source = _package_path("content_packs", "contract.json")
    try:
        text = source.read_text(encoding="utf-8")
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise RuntimeError(f"invalid engine content contract at {source}: {exc}") from exc
    if not isinstance(document, dict) or set(document) != {"format_version", "regions", "characters", "roles"}:
        raise RuntimeError(f"invalid engine content contract at {source}: expected format_version, regions, characters, and roles")
    if type(document["format_version"]) is not int or document["format_version"] != REGION_CONTRACT_FORMAT:
        raise RuntimeError(f"invalid engine content contract at {source}: unsupported format_version")
    return source, document


def _region_contract() -> tuple[RegionContractSlot, ...]:
    """Load the small engine-owned regional identity contract."""
    source, document = _content_contract_document()
    regions = document["regions"]
    if not isinstance(regions, list) or not regions:
        raise RuntimeError(f"invalid engine region content contract at {source}: regions must be a non-empty list")
    slots: list[RegionContractSlot] = []
    for index, row in enumerate(regions):
        if not isinstance(row, dict) or set(row) != {"id", "engine_id"}:
            raise RuntimeError(f"invalid engine region content contract at {source}: regions[{index}] must contain id and engine_id")
        semantic_id, engine_id = row["id"], row["engine_id"]
        if (not isinstance(semantic_id, str) or re.fullmatch(r"region\.family_[1-9][0-9]*", semantic_id) is None
                or not isinstance(engine_id, str) or re.fullmatch(r"[a-z][a-z0-9_-]*", engine_id) is None):
            raise RuntimeError(f"invalid engine region content contract at {source}: regions[{index}] has invalid ids")
        slots.append(RegionContractSlot(semantic_id, engine_id))
    if len({slot.id for slot in slots}) != len(slots) or len({slot.engine_id for slot in slots}) != len(slots):
        raise RuntimeError(f"invalid engine region content contract at {source}: region ids must be unique")
    return tuple(slots)


def region_contract() -> tuple[RegionContractSlot, ...]:
    """Return the ordered engine-owned regional presentation contract."""
    return _region_contract()


def _character_contract() -> tuple[CharacterContractSlot, ...]:
    source, document = _content_contract_document()
    characters = document["characters"]
    if not isinstance(characters, list) or not characters:
        raise RuntimeError(f"invalid engine character content contract at {source}: characters must be a non-empty list")
    slots: list[CharacterContractSlot] = []
    for index, row in enumerate(characters):
        if not isinstance(row, dict) or set(row) != {"id", "engine_id", "role_id"}:
            raise RuntimeError(
                f"invalid engine character content contract at {source}: characters[{index}] must contain id, engine_id, and role_id"
            )
        semantic_id, engine_id, role_id = row["id"], row["engine_id"], row["role_id"]
        if (not isinstance(semantic_id, str) or re.fullmatch(r"npc\.[a-z][a-z0-9_.-]*", semantic_id) is None
                or not isinstance(engine_id, str) or re.fullmatch(r"[a-z][a-z0-9_-]*", engine_id) is None
                or role_id is not None and (not isinstance(role_id, str) or re.fullmatch(r"[a-z][a-z0-9_-]*", role_id) is None)):
            raise RuntimeError(f"invalid engine character content contract at {source}: characters[{index}] has invalid ids")
        slots.append(CharacterContractSlot(semantic_id, engine_id, role_id))
    if len({slot.id for slot in slots}) != len(slots) or len({slot.engine_id for slot in slots}) != len(slots):
        raise RuntimeError(f"invalid engine character content contract at {source}: character ids must be unique")
    return tuple(slots)


def character_contract() -> tuple[CharacterContractSlot, ...]:
    """Return engine-owned static-character slots."""
    return _character_contract()


def _role_contract() -> tuple[RoleContractSlot, ...]:
    source, document = _content_contract_document()
    roles = document["roles"]
    if not isinstance(roles, list) or not roles:
        raise RuntimeError(f"invalid engine role content contract at {source}: roles must be a non-empty list")
    slots: list[RoleContractSlot] = []
    for index, row in enumerate(roles):
        if not isinstance(row, dict) or set(row) != {"id", "engine_id"}:
            raise RuntimeError(f"invalid engine role content contract at {source}: roles[{index}] must contain id and engine_id")
        semantic_id, engine_id = row["id"], row["engine_id"]
        if (not isinstance(semantic_id, str) or re.fullmatch(r"role\.[a-z][a-z0-9_.-]*", semantic_id) is None
                or not isinstance(engine_id, str) or re.fullmatch(r"[a-z][a-z0-9_-]*", engine_id) is None):
            raise RuntimeError(f"invalid engine role content contract at {source}: roles[{index}] has invalid ids")
        slots.append(RoleContractSlot(semantic_id, engine_id))
    if len({slot.id for slot in slots}) != len(slots) or len({slot.engine_id for slot in slots}) != len(slots):
        raise RuntimeError(f"invalid engine role content contract at {source}: role ids must be unique")
    character_roles = {slot.role_id for slot in _character_contract() if slot.role_id is not None}
    if not character_roles <= {slot.engine_id for slot in slots}:
        raise RuntimeError(f"invalid engine role content contract at {source}: character role reference is unknown")
    return tuple(slots)


def role_contract() -> tuple[RoleContractSlot, ...]:
    """Return engine-owned household/service role slots."""
    return _role_contract()


def _region_presentations(root: Path, pack_id: str) -> tuple[RegionPresentation, ...]:
    source = root / REGION_PRESENTATION_FILE
    try:
        text = source.read_text(encoding="utf-8")
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(
            f"invalid regional presentation for content pack {pack_id!r} at {source}: {exc}"
        ) from exc
    if not isinstance(document, dict) or set(document) != {"regions"}:
        raise ContentPackError(
            f"invalid regional presentation for content pack {pack_id!r} at {source}: expected regions"
        )
    rows = document["regions"]
    if not isinstance(rows, dict):
        raise ContentPackError(
            f"invalid regional presentation for content pack {pack_id!r} at {source}: regions must be an object"
        )
    slots = region_contract()
    expected = {slot.id for slot in slots}
    actual = set(rows)
    if actual != expected:
        missing, unknown = sorted(expected - actual), sorted(actual - expected)
        details = []
        if missing:
            details.append("missing required region slots " + ", ".join(missing))
        if unknown:
            details.append("unknown region slots " + ", ".join(unknown))
        raise ContentPackError(
            f"invalid regional presentation for content pack {pack_id!r} at {source}: " + "; ".join(details)
        )
    required = {"display_name", "route_label", "short_description"}
    presentations: list[RegionPresentation] = []
    for slot in slots:
        row = rows[slot.id]
        path = f"regions.{slot.id}"
        if not isinstance(row, dict) or set(row) != required:
            missing, unknown = (required - set(row), set(row) - required) if isinstance(row, dict) else (required, set())
            details = []
            if missing:
                details.append("missing " + ", ".join(sorted(missing)))
            if unknown:
                details.append("unknown " + ", ".join(sorted(unknown)))
            if not details:
                details.append("must be an object")
            raise ContentPackError(
                f"invalid regional presentation for content pack {pack_id!r} at {source}: {path} "
                + "; ".join(details)
            )
        for field in required:
            if not isinstance(row[field], str) or not row[field].strip():
                raise ContentPackError(
                    f"invalid regional presentation for content pack {pack_id!r} at {source}: "
                    f"{path}.{field} must be a non-empty string"
                )
        presentations.append(RegionPresentation(slot.id, slot.engine_id, **row))
    return tuple(presentations)


def _character_presentations(
    root: Path, pack_id: str,
) -> tuple[tuple[CharacterPresentation, ...], tuple[RolePresentation, ...], str]:
    source = root / CHARACTER_PRESENTATION_FILE
    try:
        text = source.read_text(encoding="utf-8")
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(
            f"invalid character presentation for content pack {pack_id!r} at {source}: {exc}"
        ) from exc
    if not isinstance(document, dict) or set(document) != {"characters", "roles", "household"}:
        raise ContentPackError(
            f"invalid character presentation for content pack {pack_id!r} at {source}: "
            "expected characters, roles, and household"
        )
    characters, roles, household = document["characters"], document["roles"], document["household"]
    if not isinstance(characters, dict) or not isinstance(roles, dict) or not isinstance(household, dict):
        raise ContentPackError(
            f"invalid character presentation for content pack {pack_id!r} at {source}: "
            "characters, roles, and household must be objects"
        )
    character_slots, role_slots = character_contract(), role_contract()
    expected_characters, expected_roles = {slot.id for slot in character_slots}, {slot.id for slot in role_slots}
    for label, rows, expected in (("characters", characters, expected_characters), ("roles", roles, expected_roles)):
        if set(rows) == expected:
            continue
        missing, unknown = sorted(expected - set(rows)), sorted(set(rows) - expected)
        details = []
        if missing:
            details.append("missing required " + label + " " + ", ".join(missing))
        if unknown:
            details.append("unknown " + label + " " + ", ".join(unknown))
        raise ContentPackError(
            f"invalid character presentation for content pack {pack_id!r} at {source}: " + "; ".join(details)
        )
    presentations: list[CharacterPresentation] = []
    for slot in character_slots:
        row = characters[slot.id]
        required = {"display_name", "short_description", "initial_memory"}
        if slot.role_id is None:
            required.add("role_label")
        path = f"characters.{slot.id}"
        if not isinstance(row, dict) or set(row) != required:
            missing, unknown = (required - set(row), set(row) - required) if isinstance(row, dict) else (required, set())
            details = []
            if missing:
                details.append("missing " + ", ".join(sorted(missing)))
            if unknown:
                details.append("unknown " + ", ".join(sorted(unknown)))
            if not details:
                details.append("must be an object")
            raise ContentPackError(
                f"invalid character presentation for content pack {pack_id!r} at {source}: {path} "
                + "; ".join(details)
            )
        if any(not isinstance(value, str) or not value.strip() for value in row.values()):
            raise ContentPackError(
                f"invalid character presentation for content pack {pack_id!r} at {source}: "
                f"{path} fields must be non-empty strings"
            )
        presentations.append(CharacterPresentation(slot.id, slot.engine_id, **row))
    role_presentations: list[RolePresentation] = []
    for slot in role_slots:
        row = roles[slot.id]
        path = f"roles.{slot.id}"
        if not isinstance(row, dict) or set(row) != {"display_label"}:
            raise ContentPackError(
                f"invalid character presentation for content pack {pack_id!r} at {source}: "
                f"{path} must contain exactly display_label"
            )
        display_label = row["display_label"]
        if not isinstance(display_label, str) or not display_label.strip():
            raise ContentPackError(
                f"invalid character presentation for content pack {pack_id!r} at {source}: "
                f"{path}.display_label must be a non-empty string"
            )
        role_presentations.append(RolePresentation(slot.id, slot.engine_id, display_label))
    if set(household) != {"character_creation_background"}:
        raise ContentPackError(
            f"invalid character presentation for content pack {pack_id!r} at {source}: "
            "household must contain exactly character_creation_background"
        )
    template = household["character_creation_background"]
    fields = {"ancestry", "origin", "role_label"}
    if (not isinstance(template, str) or not template.strip()
            or {field for field in fields if "{" + field + "}" not in template}):
        raise ContentPackError(
            f"invalid character presentation for content pack {pack_id!r} at {source}: "
            "household.character_creation_background must contain ancestry, origin, and role_label placeholders"
        )
    return tuple(presentations), tuple(role_presentations), template


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
    characters, roles, household_template = _character_presentations(root, pack_id)
    return ContentPack(
        pack_id, display_name, format_version, root, catalog_root,
        _region_presentations(root, pack_id), characters, roles, household_template,
    )


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
