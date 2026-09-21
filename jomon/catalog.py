"""Load inert main-world content from the selected content pack."""

from __future__ import annotations

from dataclasses import dataclass
import json
from importlib.resources import files
import os
from pathlib import Path
import re
import string
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
    "JOMON_MAP", "seed_words", "terrain_names", "landmark_labels",
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
ITEM_PRESENTATION_FILE = "items.json"
UI_PRESENTATION_FILE = "ui_text.json"
QUEST_PRESENTATION_FILE = "quests.json"

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
    presentation_fields: tuple[str, ...]


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
    short_description: str | None = None
    initial_memory: str | None = None
    build_tendency: str | None = None
    role_label: str | None = None


@dataclass(frozen=True)
class RolePresentation:
    """Immutable selected-pack label for one mechanical role ID."""

    id: str
    engine_id: str
    display_label: str


@dataclass(frozen=True)
class ItemContractSlot:
    """One engine-owned presentation slot for a stable base item identity."""

    id: str
    engine_id: str
    authoring_context: str
    presentation_fields: tuple[str, ...]


@dataclass(frozen=True)
class ItemPresentation:
    """Immutable selected-pack presentation for one base item identity."""

    id: str
    engine_id: str
    display_name: str
    description: str
    short_description: str | None = None


@dataclass(frozen=True)
class UiContractSlot:
    """One engine-owned UI string and its safe formatting surface."""

    id: str
    placeholders: tuple[str, ...]
    max_length: int | None = None


@dataclass(frozen=True)
class UiPresentation:
    """Immutable selected-pack terminal text."""

    id: str
    text: str


@dataclass(frozen=True)
class QuestContractSlot:
    """One engine-owned quest, arc, or evidence presentation identity."""

    id: str
    engine_id: str
    kind: str
    choice_ids: tuple[str, ...] = ()


@dataclass(frozen=True)
class QuestPresentation:
    """Immutable selected-pack prose for one quest-facing engine identity."""

    id: str
    engine_id: str
    kind: str
    title: str
    lead: str | None = None
    evidence_name: str | None = None
    evidence_description: str | None = None
    choices: tuple[tuple[str, str, str], ...] = ()
    arc_choices: tuple[tuple[str, str, str], ...] = ()
    results: tuple[tuple[str, str], ...] = ()


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
    item_presentations: tuple[ItemPresentation, ...]
    ui_presentations: tuple[UiPresentation, ...]
    quest_presentations: tuple[QuestPresentation, ...]
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

    def item_presentation(self, engine_id: str) -> ItemPresentation:
        for presentation in self.item_presentations:
            if presentation.engine_id == engine_id:
                return presentation
        raise KeyError(f"unknown engine item id: {engine_id}")

    def ui_presentation(self, semantic_id: str) -> UiPresentation:
        for presentation in self.ui_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown UI semantic id: {semantic_id}")

    def quest_presentation(self, semantic_id: str) -> QuestPresentation:
        for presentation in self.quest_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown quest semantic id: {semantic_id}")


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
    if not isinstance(document, dict) or set(document) != {"format_version", "regions", "characters", "roles", "items", "ui", "quests"}:
        raise RuntimeError(f"invalid engine content contract at {source}: expected format_version, regions, characters, roles, items, ui, and quests")
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
        if not isinstance(row, dict) or set(row) != {"id", "engine_id", "role_id", "presentation_fields"}:
            raise RuntimeError(
                f"invalid engine character content contract at {source}: characters[{index}] must contain id, engine_id, role_id, and presentation_fields"
            )
        semantic_id, engine_id, role_id, presentation_fields = (
            row["id"], row["engine_id"], row["role_id"], row["presentation_fields"]
        )
        if (not isinstance(semantic_id, str) or re.fullmatch(r"npc\.[a-z][a-z0-9_.-]*", semantic_id) is None
                or not isinstance(engine_id, str) or re.fullmatch(r"[a-z][a-z0-9_-]*", engine_id) is None
                or role_id is not None and (not isinstance(role_id, str) or re.fullmatch(r"[a-z][a-z0-9_-]*", role_id) is None)):
            raise RuntimeError(f"invalid engine character content contract at {source}: characters[{index}] has invalid ids")
        allowed_fields = {"display_name", "role_label", "short_description", "initial_memory", "build_tendency"}
        if (not isinstance(presentation_fields, list) or not presentation_fields
                or any(not isinstance(field, str) or field not in allowed_fields for field in presentation_fields)
                or len(set(presentation_fields)) != len(presentation_fields)
                or "display_name" not in presentation_fields
                or (role_id is not None and "role_label" in presentation_fields)):
            raise RuntimeError(
                f"invalid engine character content contract at {source}: characters[{index}] has invalid presentation_fields"
            )
        slots.append(CharacterContractSlot(semantic_id, engine_id, role_id, tuple(presentation_fields)))
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


def _item_contract() -> tuple[ItemContractSlot, ...]:
    source, document = _content_contract_document()
    items = document["items"]
    if not isinstance(items, list) or not items:
        raise RuntimeError(f"invalid engine item content contract at {source}: items must be a non-empty list")
    slots: list[ItemContractSlot] = []
    for index, row in enumerate(items):
        required = {"id", "engine_id", "authoring_context", "presentation_fields"}
        if not isinstance(row, dict) or set(row) != required:
            raise RuntimeError(
                f"invalid engine item content contract at {source}: items[{index}] must contain "
                "id, engine_id, authoring_context, and presentation_fields"
            )
        semantic_id = row["id"]
        engine_id = row["engine_id"]
        authoring_context = row["authoring_context"]
        presentation_fields = row["presentation_fields"]
        allowed_fields = {"display_name", "description", "short_description"}
        if (not isinstance(semantic_id, str) or re.fullmatch(r"item\.(?:goods|equipment)_[0-9]{3}", semantic_id) is None
                or not isinstance(engine_id, str) or not engine_id.strip()
                or not isinstance(authoring_context, str) or not authoring_context.strip()
                or not isinstance(presentation_fields, list) or not presentation_fields
                or any(not isinstance(field, str) or field not in allowed_fields for field in presentation_fields)
                or len(set(presentation_fields)) != len(presentation_fields)
                or not {"display_name", "description"} <= set(presentation_fields)):
            raise RuntimeError(f"invalid engine item content contract at {source}: items[{index}] has invalid fields")
        slots.append(ItemContractSlot(semantic_id, engine_id, authoring_context, tuple(presentation_fields)))
    if len({slot.id for slot in slots}) != len(slots) or len({slot.engine_id for slot in slots}) != len(slots):
        raise RuntimeError(f"invalid engine item content contract at {source}: item ids must be unique")
    return tuple(slots)


def item_contract() -> tuple[ItemContractSlot, ...]:
    """Return the ordered engine-owned base-item presentation contract."""
    return _item_contract()


def _ui_contract() -> tuple[UiContractSlot, ...]:
    source, document = _content_contract_document()
    rows = document["ui"]
    if not isinstance(rows, list) or not rows:
        raise RuntimeError(f"invalid engine UI content contract at {source}: ui must be a non-empty list")
    slots: list[UiContractSlot] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict) or set(row) != {"id", "placeholders", "max_length"}:
            raise RuntimeError(f"invalid engine UI content contract at {source}: ui[{index}] must contain id, placeholders, and max_length")
        semantic_id, placeholders, max_length = row["id"], row["placeholders"], row["max_length"]
        if (not isinstance(semantic_id, str) or re.fullmatch(r"ui\.[a-z][a-z0-9_.-]*", semantic_id) is None
                or not isinstance(placeholders, list) or len(set(placeholders)) != len(placeholders)
                or any(not isinstance(name, str) or re.fullmatch(r"[a-z][a-z0-9_]*", name) is None for name in placeholders)
                or max_length is not None and (type(max_length) is not int or max_length < 1)):
            raise RuntimeError(f"invalid engine UI content contract at {source}: ui[{index}] has invalid fields")
        slots.append(UiContractSlot(semantic_id, tuple(placeholders), max_length))
    if len({slot.id for slot in slots}) != len(slots):
        raise RuntimeError(f"invalid engine UI content contract at {source}: UI ids must be unique")
    return tuple(slots)


def ui_contract() -> tuple[UiContractSlot, ...]:
    """Return engine-owned UI wording and formatting requirements."""
    return _ui_contract()


def _quest_contract() -> tuple[QuestContractSlot, ...]:
    source, document = _content_contract_document()
    rows = document["quests"]
    if not isinstance(rows, list) or not rows:
        raise RuntimeError(f"invalid engine quest content contract at {source}: quests must be a non-empty list")
    slots: list[QuestContractSlot] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict) or set(row) != {"id", "engine_id", "kind", "choice_ids"}:
            raise RuntimeError(f"invalid engine quest content contract at {source}: quests[{index}] must contain id, engine_id, kind, and choice_ids")
        semantic_id, engine_id, kind, choice_ids = row["id"], row["engine_id"], row["kind"], row["choice_ids"]
        if (not isinstance(semantic_id, str) or re.fullmatch(r"quest\.[a-z][a-z0-9_.-]*", semantic_id) is None
                or not isinstance(engine_id, str) or not engine_id
                or kind not in {"regional", "arc", "evidence"}
                or not isinstance(choice_ids, list) or len(set(choice_ids)) != len(choice_ids)
                or any(not isinstance(choice_id, str) or re.fullmatch(r"(?:[a-z]|[0-9]+\.[a-z])", choice_id) is None for choice_id in choice_ids)
                or (kind == "regional" and len(choice_ids) != 2) or (kind == "evidence" and choice_ids)):
            raise RuntimeError(f"invalid engine quest content contract at {source}: quests[{index}] has invalid fields")
        slots.append(QuestContractSlot(semantic_id, engine_id, kind, tuple(choice_ids)))
    if len({slot.id for slot in slots}) != len(slots) or len({(slot.kind, slot.engine_id) for slot in slots}) != len(slots):
        raise RuntimeError(f"invalid engine quest content contract at {source}: quest ids must be unique")
    return tuple(slots)


def quest_contract() -> tuple[QuestContractSlot, ...]:
    """Return engine-owned quest-facing presentation slots."""
    return _quest_contract()


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
        required = set(slot.presentation_fields)
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
        values = {field: row.get(field) for field in {
            "display_name", "short_description", "initial_memory", "build_tendency", "role_label",
        }}
        presentations.append(CharacterPresentation(slot.id, slot.engine_id, **values))
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


def _item_presentations(root: Path, pack_id: str) -> tuple[ItemPresentation, ...]:
    source = root / ITEM_PRESENTATION_FILE
    try:
        text = source.read_text(encoding="utf-8")
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(
            f"invalid item presentation for content pack {pack_id!r} at {source}: {exc}"
        ) from exc
    if not isinstance(document, dict) or set(document) != {"items"} or not isinstance(document["items"], dict):
        raise ContentPackError(
            f"invalid item presentation for content pack {pack_id!r} at {source}: items must be an object"
        )
    rows = document["items"]
    slots = item_contract()
    expected, actual = {slot.id for slot in slots}, set(rows)
    if actual != expected:
        missing, unknown = sorted(expected - actual), sorted(actual - expected)
        details = []
        if missing:
            details.append("missing required item slots " + ", ".join(missing))
        if unknown:
            details.append("unknown item slots " + ", ".join(unknown))
        raise ContentPackError(
            f"invalid item presentation for content pack {pack_id!r} at {source}: " + "; ".join(details)
        )
    presentations: list[ItemPresentation] = []
    for slot in slots:
        row = rows[slot.id]
        required = set(slot.presentation_fields)
        path = f"items.{slot.id}"
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
                f"invalid item presentation for content pack {pack_id!r} at {source}: {path} "
                + "; ".join(details)
            )
        if any(not isinstance(value, str) or not value.strip() for value in row.values()):
            raise ContentPackError(
                f"invalid item presentation for content pack {pack_id!r} at {source}: "
                f"{path} fields must be non-empty strings"
            )
        presentations.append(ItemPresentation(
            slot.id, slot.engine_id, row["display_name"], row["description"], row.get("short_description"),
        ))
    return tuple(presentations)


def _ui_presentations(root: Path, pack_id: str) -> tuple[UiPresentation, ...]:
    source = root / UI_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: text must be an object")
    rows = document["text"]
    slots = ui_contract()
    expected, actual = {slot.id for slot in slots}, set(rows)
    if actual != expected:
        details = []
        if expected - actual:
            details.append("missing required UI keys " + ", ".join(sorted(expected - actual)))
        if actual - expected:
            details.append("unknown UI keys " + ", ".join(sorted(actual - expected)))
        raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    presentations: list[UiPresentation] = []
    formatter = string.Formatter()
    for slot in slots:
        text = rows[slot.id]
        if not isinstance(text, str) or not text:
            raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: text.{slot.id} must be a non-empty string")
        if slot.max_length is not None and len(text) > slot.max_length:
            raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: text.{slot.id} exceeds maximum length {slot.max_length}")
        try:
            fields = []
            for _, field_name, format_spec, conversion in formatter.parse(text):
                if field_name is not None:
                    if (field_name not in slot.placeholders or format_spec or conversion
                            or not re.fullmatch(r"[a-z][a-z0-9_]*", field_name)):
                        raise ValueError("unsupported placeholder")
                    fields.append(field_name)
        except ValueError as exc:
            raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: text.{slot.id} has malformed template: {exc}") from exc
        if set(fields) != set(slot.placeholders) or len(fields) != len(slot.placeholders):
            raise ContentPackError(f"invalid UI presentation for content pack {pack_id!r} at {source}: text.{slot.id} must contain exactly placeholders {', '.join(slot.placeholders) or 'none'}")
        presentations.append(UiPresentation(slot.id, text))
    return tuple(presentations)


def _quest_presentations(root: Path, pack_id: str) -> tuple[QuestPresentation, ...]:
    source = root / QUEST_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    if (not isinstance(document, dict) or set(document) != {"quests", "arc_choices", "arc_results"}
            or not isinstance(document["quests"], dict) or not isinstance(document["arc_choices"], dict)
            or not isinstance(document["arc_results"], dict)):
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: quests, arc_choices, and arc_results must be objects")
    rows = document["quests"]
    slots = quest_contract()
    expected, actual = {slot.id for slot in slots}, set(rows)
    if actual != expected:
        details = []
        if expected - actual:
            details.append("missing required quest slots " + ", ".join(sorted(expected - actual)))
        if actual - expected:
            details.append("unknown quest slots " + ", ".join(sorted(actual - expected)))
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    arc_rows, result_rows = document["arc_choices"], document["arc_results"]
    arc_slots = [slot for slot in slots if slot.kind == "arc"]
    if set(arc_rows) != {slot.engine_id for slot in arc_slots} or set(result_rows) != {slot.engine_id for slot in arc_slots}:
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_choices and arc_results must contain exactly contracted arcs")
    presentations: list[QuestPresentation] = []
    for slot in slots:
        row = rows[slot.id]
        path = f"quests.{slot.id}"
        required = {"title", "lead", "choices"} if slot.kind == "regional" else ({"title"} if slot.kind == "arc" else {"evidence_name", "evidence_description"})
        if not isinstance(row, dict) or set(row) != required:
            missing, unknown = (required - set(row), set(row) - required) if isinstance(row, dict) else (required, set())
            details = []
            if missing:
                details.append("missing " + ", ".join(sorted(missing)))
            if unknown:
                details.append("unknown " + ", ".join(sorted(unknown)))
            if not details:
                details.append("must be an object")
            raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path} " + "; ".join(details))
        text_fields = required - {"choices"}
        if any(not isinstance(row[field], str) or not row[field].strip() for field in text_fields):
            raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path} fields must be non-empty strings")
        choices: tuple[tuple[str, str, str], ...] = ()
        if slot.kind == "regional":
            choice_rows = row["choices"]
            expected_choices = slot.choice_ids
            if not isinstance(choice_rows, dict) or set(choice_rows) != set(expected_choices):
                raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path}.choices must contain exactly {', '.join(expected_choices)}")
            validated = []
            for choice in expected_choices:
                choice_row = choice_rows[choice]
                if not isinstance(choice_row, dict) or set(choice_row) != {"label", "requirement"} or any(
                    not isinstance(value, str) for value in choice_row.values()
                ):
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path}.choices.{choice} must contain string label and requirement")
                if not choice_row["label"].strip():
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path}.choices.{choice}.label must be a non-empty string")
                validated.append((choice, choice_row["label"], choice_row["requirement"]))
            choices = tuple(validated)
        arc_choices: tuple[tuple[str, str, str], ...] = ()
        results: tuple[tuple[str, str], ...] = ()
        if slot.kind == "arc":
            rows_by_stage = arc_rows[slot.engine_id]
            flat = {}
            if not isinstance(rows_by_stage, dict):
                raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_choices.{slot.engine_id} must be an object")
            for stage, stage_rows in rows_by_stage.items():
                if not isinstance(stage, str) or not stage.isdecimal() or not isinstance(stage_rows, dict):
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_choices.{slot.engine_id} has invalid stage")
                for key, choice_row in stage_rows.items():
                    if not isinstance(choice_row, dict) or set(choice_row) != {"label", "requirement"} or any(not isinstance(value, str) for value in choice_row.values()) or not choice_row["label"].strip():
                        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_choices.{slot.engine_id}.{stage}.{key} must contain non-empty label and string requirement")
                    flat[f"{stage}.{key}"] = (choice_row["label"], choice_row["requirement"])
            if set(flat) != set(slot.choice_ids):
                raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_choices.{slot.engine_id} has missing or unknown choice")
            arc_choices = tuple((choice_id, *flat[choice_id]) for choice_id in slot.choice_ids)
            result = result_rows[slot.engine_id]
            final_keys = {choice_id.split(".", 1)[1] for choice_id in slot.choice_ids if choice_id.startswith(("4." if slot.engine_id == "marks" else "3."))}
            lifecycle_keys = {"__unavailable_chapter", "__missing_record", "__replacement", "__start", "__transition", "__settled", "__completed"}
            if slot.engine_id == "marks":
                lifecycle_keys.add("__unavailable_arc")
            if (not isinstance(result, dict) or set(result) != final_keys | lifecycle_keys
                    or any(not isinstance(value, str) or not value.strip() for value in result.values())):
                raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_results.{slot.engine_id} must contain exactly final result keys")
            allowed_lifecycle_fields = {
                "__unavailable_chapter": set(), "__unavailable_arc": set(), "__missing_record": set(),
                "__replacement": {"evidence"},
                "__start": {"arc_title", "next_region"}, "__transition": {"chapter", "current_region", "next_region"},
                "__settled": {"arc_title", "consequence"}, "__completed": {"arc_title", "consequence"},
            }
            if slot.engine_id == "marks":
                allowed_lifecycle_fields["__start"] = set()
            formatter = string.Formatter()
            for key in lifecycle_keys:
                try:
                    fields = {field for _, field, spec, conversion in formatter.parse(result[key]) if field is not None and not spec and not conversion and re.fullmatch(r"[a-z][a-z0-9_]*", field)}
                except ValueError as exc:
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_results.{slot.engine_id}.{key} has malformed template: {exc}") from exc
                if fields != allowed_lifecycle_fields[key]:
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: arc_results.{slot.engine_id}.{key} has invalid placeholders")
            results = tuple((key, result[key]) for key in sorted(result))
        presentations.append(QuestPresentation(
            slot.id, slot.engine_id, slot.kind, row.get("title", ""), row.get("lead"),
            row.get("evidence_name"), row.get("evidence_description"), choices, arc_choices, results,
        ))
    return tuple(presentations)


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
    items = _item_presentations(root, pack_id)
    ui = _ui_presentations(root, pack_id)
    quests = _quest_presentations(root, pack_id)
    return ContentPack(
        pack_id, display_name, format_version, root, catalog_root,
        _region_presentations(root, pack_id), characters, roles, items, ui, quests, household_template,
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
