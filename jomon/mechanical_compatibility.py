"""Canonical compatibility data for the selected main-world catalog environment.

This module deliberately projects decoded catalogs instead of hashing their bytes.
Every catalog section and every record field is classified here.  Additions fail
until a maintainer decides whether they are mechanical, presentation, or inert
metadata and updates this registry.
"""
from __future__ import annotations

from functools import lru_cache
from hashlib import sha256
import json
from typing import Any, Callable

from .catalog import (
    ACTOR_SECTIONS, AFTERMATH_SECTIONS, ARC_RELIC_SECTIONS, CHARACTER_SECTIONS,
    EQUIPMENT_SECTIONS, GEOGRAPHY_SECTIONS, HISTORY_SECTIONS, PRACTICE_SECTIONS,
    RECRUITMENT_SECTIONS, REQUIRED_CATALOGS, VESSEL_SECTIONS, VISUAL_SECTIONS,
    WORLD_TEXT_SECTIONS, load_catalog,
)

MECHANICAL_COMPATIBILITY_VERSION = 1
MECHANICAL_PROJECTION_FORMAT = 1


class MechanicalProjectionError(ValueError):
    """Raised when a catalog has data not explicitly classified for projection."""


def mechanical_compatibility_version() -> int:
    return MECHANICAL_COMPATIBILITY_VERSION


def _error(path: str, message: str) -> MechanicalProjectionError:
    return MechanicalProjectionError(f"mechanical compatibility projection: {path}: {message}")


def _mapping(value: Any, *, path: str, mechanical: set[str], presentation: set[str] = set(), ignored: set[str] = set()) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise _error(path, "expected an object")
    actual = set(value)
    classified = mechanical | presentation | ignored
    if actual != classified:
        details = []
        if actual - classified:
            details.append("unclassified fields " + ", ".join(sorted(actual - classified)))
        if classified - actual:
            details.append("missing classified fields " + ", ".join(sorted(classified - actual)))
        raise _error(path, "; ".join(details))
    return {key: value[key] for key in sorted(mechanical)}


def _row(value: Any, *, path: str, mechanical: set[str], presentation: set[str] = set(), ignored: set[str] = set()) -> dict[str, Any]:
    return _mapping(value, path=path, mechanical=mechanical, presentation=presentation, ignored=ignored)


def _rows(value: Any, *, path: str, project: Callable[[Any, str], Any]) -> list[Any]:
    if not isinstance(value, list):
        raise _error(path, "expected a list")
    return [project(row, f"{path}[{index}]") for index, row in enumerate(value)]


def _tuple_row(value: Any, *, path: str, width: int, mechanical: set[int], presentation: set[int] = set(), ignored: set[int] = set()) -> list[Any]:
    if not isinstance(value, list) or len(value) != width:
        raise _error(path, f"expected a {width}-field list")
    actual = set(range(width))
    classified = mechanical | presentation | ignored
    if classified != actual:
        raise _error(path, "internal positional classification is incomplete")
    return [value[index] for index in sorted(mechanical)]


def _map_rows(value: Any, *, path: str, project: Callable[[Any, str], Any]) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise _error(path, "expected an object")
    return {key: project(row, f"{path}.{key}") for key, row in sorted(value.items())}


def _map_values(value: Any, *, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise _error(path, "expected an object")
    return {key: value[key] for key in sorted(value)}


def _actors(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="actors.json", mechanical={"ENEMY_ARCHETYPES", "FRONTIER_ACTORS", "EXPANDED_STANDARD_ACTORS", "FRONTIER_ELITES", "STANDARD_REACTIONS"}, presentation={"LEGACY_STANDARD_GLYPHS"})
    archetype_mechanical = {"budget", "capability", "hearing", "morale", "profile", "range", "region", "role", "terrain", "vision", "ranged_kind", "elite", "aftermath", "mode", "reward", "named", "supplies", "ecology", "duty", "reaction"}
    archetype_presentation = {"name", "goal", "counterplay", "glyph"}
    def project_archetype(row: Any, path: str) -> dict[str, Any]:
        if not isinstance(row, dict):
            raise _error(path, "expected an object")
        unknown = set(row) - archetype_mechanical - archetype_presentation
        if unknown:
            raise _error(path, "unclassified fields " + ", ".join(sorted(unknown)))
        return {key: row[key] for key in sorted(set(row) & archetype_mechanical)}
    result = {
        "ENEMY_ARCHETYPES": _map_rows(top["ENEMY_ARCHETYPES"], path="actors.json.ENEMY_ARCHETYPES", project=project_archetype),
        "FRONTIER_ACTORS": _rows(top["FRONTIER_ACTORS"], path="actors.json.FRONTIER_ACTORS", project=lambda row, p: _tuple_row(row, path=p, width=14, mechanical={0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 12}, presentation={2, 11, 13})),
        "EXPANDED_STANDARD_ACTORS": _rows(top["EXPANDED_STANDARD_ACTORS"], path="actors.json.EXPANDED_STANDARD_ACTORS", project=lambda row, p: _tuple_row(row, path=p, width=14, mechanical={0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 12}, presentation={2, 11, 13})),
        "STANDARD_REACTIONS": _map_values(top["STANDARD_REACTIONS"], path="actors.json.STANDARD_REACTIONS"),
    }
    elites = _mapping(top["FRONTIER_ELITES"], path="actors.json.FRONTIER_ELITES", mechanical={"rows", "aftermath", "named"})
    result["FRONTIER_ELITES"] = {
        "rows": _rows(elites["rows"], path="actors.json.FRONTIER_ELITES.rows", project=lambda row, p: _tuple_row(row, path=p, width=9, mechanical={0, 1, 3, 4, 8}, presentation={2, 5, 6, 7})),
        "aftermath": sorted(elites["aftermath"]),
        "named": sorted(elites["named"]),
    }
    return result


def _aftermath(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="aftermath.json", mechanical={"topologies", "drainage_topologies", "fire_topologies", "support_topologies", "recovery_topologies", "preparations", "household_stories", "interferences"}, presentation={"lines"})
    return {
        "topologies": _map_values(top["topologies"], path="aftermath.json.topologies"),
        "drainage_topologies": sorted(top["drainage_topologies"]),
        "fire_topologies": sorted(top["fire_topologies"]),
        "support_topologies": sorted(top["support_topologies"]),
        "recovery_topologies": sorted(top["recovery_topologies"]),
        "preparations": _rows(top["preparations"], path="aftermath.json.preparations", project=lambda row, p: _row(row, path=p, mechanical={"id", "mode", "topology"}, presentation={"name", "description", "condition"})),
        "household_stories": _rows(top["household_stories"], path="aftermath.json.household_stories", project=lambda row, p: _row(row, path=p, mechanical={"id"}, presentation={"name", "premise", "requirement"})),
        "interferences": _rows(top["interferences"], path="aftermath.json.interferences", project=lambda row, p: _row(row, path=p, mechanical={"id", "cargo", "destination", "kind", "origin"}, presentation={"title", "cause", "origin_change", "destination_change"})),
    }


def _arc_relics(data: dict[str, Any]) -> dict[str, Any]:
    rows = _mapping(data, path="arc_relics.json", mechanical={"relics"})["relics"]
    return {"relics": _rows(rows, path="arc_relics.json.relics", project=lambda row, p: _tuple_row(row, path=p, width=4, mechanical={0, 1, 2}, presentation={3}))}


def _characters(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="character_profiles.json", mechanical={"competencies", "starting_points", "ancestries", "origins", "traits", "origin_practices", "attribute_competencies", "role_attributes", "role_competencies"})
    return {
        "competencies": list(top["competencies"]), "starting_points": _map_values(top["starting_points"], path="character_profiles.json.starting_points"),
        "ancestries": _map_rows(top["ancestries"], path="character_profiles.json.ancestries", project=lambda row, p: _row(row, path=p, mechanical={"competency"}, presentation={"effect"})),
        "origins": list(top["origins"]),
        "traits": _map_rows(top["traits"], path="character_profiles.json.traits", project=lambda row, p: _tuple_row(row, path=p, width=2, mechanical={0}, presentation={1})),
        "origin_practices": _map_values(top["origin_practices"], path="character_profiles.json.origin_practices"),
        "attribute_competencies": _map_values(top["attribute_competencies"], path="character_profiles.json.attribute_competencies"),
        "role_attributes": _map_values(top["role_attributes"], path="character_profiles.json.role_attributes"),
        "role_competencies": _map_values(top["role_competencies"], path="character_profiles.json.role_competencies"),
    }


def _chemistry(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="chemistry.json", mechanical={"reactions", "environment_reactions", "reagent_ids"})
    return {"reagent_ids": list(top["reagent_ids"]), "reactions": _rows(top["reactions"], path="chemistry.json.reactions", project=lambda row, p: _row(row, path=p, mechanical={"id", "effect", "reagent_ids"})), "environment_reactions": _map_values(top["environment_reactions"], path="chemistry.json.environment_reactions")}


def _circuits(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="circuits.json", mechanical={"parts", "fixtures"})
    return {"parts": _map_rows(top["parts"], path="circuits.json.parts", project=lambda row, p: _row(row, path=p, mechanical={"behavior", "build_key", "glyph", "layers"})), "fixtures": _rows(top["fixtures"], path="circuits.json.fixtures", project=lambda row, p: _row(row, path=p, mechanical={"anchor", "cells", "id", "region"}))}


def _equipment(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="equipment.json", mechanical={"ammunition_items", "weapon_ammunition", "basic_courier_loadouts", "basic_courier_armour", "item_specs", "regional_armour", "work_weapons", "pot_ammunition", "arsenal", "bomb_ammunition", "fittings", "enemy_regional_armour"})
    item_mechanical={"blunt","category","coverage","cut","height","mobility","noise","pierce","slot","stack_limit","tags","weight","width"}
    return {
        "ammunition_items": _map_values(top["ammunition_items"], path="equipment.json.ammunition_items"), "weapon_ammunition": _map_values(top["weapon_ammunition"], path="equipment.json.weapon_ammunition"), "basic_courier_loadouts": _map_values(top["basic_courier_loadouts"], path="equipment.json.basic_courier_loadouts"), "basic_courier_armour": _map_values(top["basic_courier_armour"], path="equipment.json.basic_courier_armour"),
        "item_specs": _map_rows(top["item_specs"], path="equipment.json.item_specs", project=lambda row,p:_row(row,path=p,mechanical=item_mechanical,presentation={"name","description","abbreviation"})),
        "regional_armour": _map_values(top["regional_armour"], path="equipment.json.regional_armour"),
        "work_weapons": _map_rows(top["work_weapons"], path="equipment.json.work_weapons", project=lambda row,p:_row(row,path=p,mechanical={"damage","minimum","noise","reach","regions","shape","weight"},presentation={"name","description"})),
        "pot_ammunition": _map_values(top["pot_ammunition"], path="equipment.json.pot_ammunition"),
        "arsenal": _rows(top["arsenal"],path="equipment.json.arsenal",project=lambda row,p:_row(row,path=p,mechanical={"ammunition","damage","effects","family","minimum","name","noise","reach","regions","shape","weight"})),
        "bomb_ammunition": _map_values(top["bomb_ammunition"], path="equipment.json.bomb_ammunition"),
        "fittings": _map_rows(top["fittings"],path="equipment.json.fittings",project=lambda row,p:_row(row,path=p,mechanical={"price","shape","slot","targets","weight"},presentation={"name","effect","drawback"})),
        "enemy_regional_armour": _map_values(top["enemy_regional_armour"],path="equipment.json.enemy_regional_armour"),
    }


def _geography(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="geography.json",mechanical={"SIDE_ROUTES","GROUND_PATCHES","FIELD_SECRETS","FRONTIERS"})
    return {"SIDE_ROUTES":_map_values(top["SIDE_ROUTES"],path="geography.json.SIDE_ROUTES"),"GROUND_PATCHES":_map_values(top["GROUND_PATCHES"],path="geography.json.GROUND_PATCHES"),"FIELD_SECRETS":_map_rows(top["FIELD_SECRETS"],path="geography.json.FIELD_SECRETS",project=lambda row,p:_rows(row,path=p,project=lambda entry,q:_row(entry,path=q,mechanical={"anchor","id","reward","requirement"}))),"FRONTIERS":_map_rows(top["FRONTIERS"],path="geography.json.FRONTIERS",project=lambda row,p:_row(row,path=p,mechanical={"commodity","contacts","discoveries","height","relics","shortage","width"},presentation={"geology","name","process"}))}


def _goods(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="goods.json",mechanical={"COMMODITIES","COMMODITY_LOGISTICS","WEAPONS","GEAR","SUPPORTS","DISCOVERIES","RELICS","PASSIVES","MERCHANT_ITEMS"})
    return {"COMMODITIES":_map_values(top["COMMODITIES"],path="goods.json.COMMODITIES"),"COMMODITY_LOGISTICS":_map_values(top["COMMODITY_LOGISTICS"],path="goods.json.COMMODITY_LOGISTICS"),"WEAPONS":sorted(top["WEAPONS"]),"GEAR":sorted(top["GEAR"]),"SUPPORTS":sorted(top["SUPPORTS"]),"DISCOVERIES":_map_rows(top["DISCOVERIES"],path="goods.json.DISCOVERIES",project=lambda row,p:_tuple_row(row,path=p,width=2,mechanical={0},presentation={1})),"RELICS":sorted(top["RELICS"]),"PASSIVES":_map_rows(top["PASSIVES"],path="goods.json.PASSIVES",project=lambda row,p:_tuple_row(row,path=p,width=2,mechanical={0},presentation={1})),"MERCHANT_ITEMS":_map_values(top["MERCHANT_ITEMS"],path="goods.json.MERCHANT_ITEMS")}


def _identity_catalog(data: dict[str, Any], *, path: str, sections: set[str], presentation: set[str] = set()) -> dict[str, Any]:
    top=_mapping(data,path=path,mechanical=sections-presentation,presentation=presentation)
    return {key: top[key] for key in sorted(top)}


def _history(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="history.json",mechanical={"working_accounts","institution_services","institution_ties","network_accounts","network_contacts","undertakings","legend_bases","crisis_tags"})
    # These rows predate presentation extraction.  Their stable row structure is
    # still consumed by generation; retain it until an engine schema separates it.
    return {key: top[key] for key in sorted(top)}


def _people(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="people.json",mechanical={"ROLES","ROLE_EQUIPMENT","ROLE_TECHNIQUE"},presentation={"REGIONAL_CONTEXTS","FIRST_NAMES","FAMILY_NAMES","RECRUIT_TEMPLATES","CONTACT_NAMES"})
    return {key: top[key] for key in sorted(top)}


def _practices(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="practices.json",sections={"practices","network_contacts","aftermath_regions"})


def _production(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="production.json",sections={"sources","site_keys","shore_stations","recipes"})


def _quests(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(
        data, path="quests.json",
        mechanical={"quests", "rewards", "arc_regions", "additional_arcs"},
        presentation={"arc_title"},
    )
    def project_quest(row: Any, path: str) -> dict[str, Any]:
        parsed = _row(
            row, path=path, mechanical={"cache", "final"},
            presentation={"title", "lead"},
        )
        return {
            "cache": parsed["cache"],
            # A final row is (control, label, consequence kind, availability,
            # unavailable explanation).  Only the control/kind/availability
            # participate in outcome selection.
            "final": _rows(
                parsed["final"], path=f"{path}.final",
                project=lambda value, child: _tuple_row(
                    value, path=child, width=5, mechanical={0, 2, 3},
                    presentation={1, 4},
                ),
            ),
        }
    def project_arc(row: Any, path: str) -> dict[str, Any]:
        if not isinstance(row, dict):
            raise _error(path, "expected an object")
        required = {"title", "start", "regions", "requires", "evidence"}
        optional = {"requires_aftermath", "public_openings", "environment_choices"}
        actual = set(row)
        if not required <= actual or actual - required - optional:
            raise _error(path, "unclassified or missing fields")
        result = {key: row[key] for key in sorted((required - {"title"}) | (actual & optional))}
        return result
    return {
        "quests": _map_rows(top["quests"], path="quests.json.quests", project=project_quest),
        "rewards": _map_values(top["rewards"], path="quests.json.rewards"),
        "arc_regions": _map_values(top["arc_regions"], path="quests.json.arc_regions"),
        "additional_arcs": _map_rows(top["additional_arcs"], path="quests.json.additional_arcs", project=project_arc),
    }


def _recruitment(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="recruitment.json",sections={"requirements"})


def _sanctums(data: dict[str, Any]) -> dict[str, Any]:
    top = _mapping(data, path="sanctums.json", mechanical={"encounters", "sanctums"})
    def project_site(row: Any, path: str) -> dict[str, Any]:
        parsed = _row(row, path=path, mechanical={"network", "boss"})
        parsed["boss"] = _row(
            parsed["boss"], path=f"{path}.boss",
            mechanical={"profile", "role", "duty", "health", "glyph", "capability_id", "counterplay_id"},
            presentation={"goal", "capability"},
        )
        return parsed
    return {
        "encounters": _map_values(top["encounters"], path="sanctums.json.encounters"),
        "sanctums": _map_rows(top["sanctums"], path="sanctums.json.sanctums", project=project_site),
    }


def _situations(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="situations.json",sections={"situations","afterwork_samples"})


def _skills(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="skills.json",sections={"branches","role_roots"})


def _spells(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="spells.json",sections={"spells"})


def _terrain(data: dict[str, Any]) -> dict[str, Any]:
    return _identity_catalog(data,path="terrain_variation.json",sections={"regions"})


def _vehicles(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="vehicles.json",mechanical={"harbour","vehicles"})
    return {"harbour":top["harbour"],"vehicles":_map_rows(top["vehicles"],path="vehicles.json.vehicles",project=lambda row,p:_row(row,path=p,mechanical={"capacity","condition","deck","domain","glyph","pace","region","resource"},presentation={"name"}))}


def _vessel(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="vessel.json",mechanical={"hazard_stations","variants","echoes","refits","region_nodes","route_nodes","route_edges","optional_route_edges","drinks"},presentation={"voyages"})
    return {"hazard_stations":_map_values(top["hazard_stations"],path="vessel.json.hazard_stations"),"variants":_rows(top["variants"],path="vessel.json.variants",project=lambda row,p:_row(row,path=p,mechanical={"family","id"},presentation={"name","cause","effect","counterplay"})),"echoes":_rows(top["echoes"],path="vessel.json.echoes",project=lambda row,p:_row(row,path=p,mechanical={"kind","variant_id"},presentation={"title","consequence"})),"refits":_rows(top["refits"],path="vessel.json.refits",project=lambda row,p:_row(row,path=p,mechanical={"credit","dependency","id","station"},presentation={"name","effect","drawback"})),"region_nodes":_map_values(top["region_nodes"],path="vessel.json.region_nodes"),"route_nodes":_map_rows(top["route_nodes"],path="vessel.json.route_nodes",project=lambda row,p:_row(row,path=p,mechanical={"id","integrity_required","kind","known","market_interest","region_id","risk","supply","x","y"},presentation={"name","description","seasonal_note"})),"route_edges":_rows(top["route_edges"],path="vessel.json.route_edges",project=lambda row,p:_tuple_row(row,path=p,width=9,mechanical={0,1,2,3,4,5,6,7},presentation={8})),"optional_route_edges":_rows(top["optional_route_edges"],path="vessel.json.optional_route_edges",project=lambda row,p:_tuple_row(row,path=p,width=9,mechanical={0,1,2,3,4,5,6,7},presentation={8})),"drinks":_rows(top["drinks"],path="vessel.json.drinks",project=lambda row,p:_row(row,path=p,mechanical={"cost","duration","id","incompatible","rare"},presentation={"name","benefit","drawback"}))}


def _visuals(data: dict[str, Any]) -> dict[str, Any]:
    # Map geometry and semantic glyph tables affect legality and deterministic
    # placement; decorative card/dice art remains outside the main-world model.
    top=_mapping(data,path="visuals.json",mechanical={"entity_glyphs","semantic_roles","regional_ground_roles","regional_tile_roles","physical_role_overrides","route_node_symbols","material_overlay_symbols","site_symbols","vessel_levels","tavern_map"},presentation={"tavern_cards","tavern_dice"})
    return {key:top[key] for key in sorted(top)}


def _world_text(data: dict[str, Any]) -> dict[str, Any]:
    top=_mapping(data,path="world_text.json",mechanical={"JOMON_MAP","seed_words"},presentation={"terrain_names","landmark_labels"})
    return {key:top[key] for key in sorted(top)}


def _field_reports(data: dict[str, Any]) -> dict[str, Any]:
    _mapping(data,path="field_reports.json",mechanical=set(),presentation={"responses"})
    return {}


_CATALOG_PROJECTORS: dict[str, tuple[tuple[str, ...], Callable[[dict[str, Any]], dict[str, Any]]]] = {
    "actors.json": (ACTOR_SECTIONS, _actors), "aftermath.json": (AFTERMATH_SECTIONS, _aftermath), "arc_relics.json": (ARC_RELIC_SECTIONS, _arc_relics),
    "character_profiles.json": (CHARACTER_SECTIONS, _characters), "chemistry.json": (("reactions", "environment_reactions", "reagent_ids"), _chemistry), "circuits.json": (("parts", "fixtures"), _circuits),
    "equipment.json": (EQUIPMENT_SECTIONS, _equipment), "field_reports.json": (("responses",), _field_reports), "geography.json": (GEOGRAPHY_SECTIONS, _geography), "goods.json": (("COMMODITIES", "COMMODITY_LOGISTICS", "WEAPONS", "GEAR", "SUPPORTS", "DISCOVERIES", "RELICS", "PASSIVES", "MERCHANT_ITEMS"), _goods),
    "history.json": (HISTORY_SECTIONS, _history), "people.json": (("REGIONAL_CONTEXTS", "FIRST_NAMES", "FAMILY_NAMES", "ROLES", "ROLE_EQUIPMENT", "ROLE_TECHNIQUE", "RECRUIT_TEMPLATES", "CONTACT_NAMES"), _people), "practices.json": (PRACTICE_SECTIONS, _practices), "production.json": (("sources", "site_keys", "shore_stations", "recipes"), _production), "quests.json": (("quests", "rewards", "arc_regions", "arc_title", "additional_arcs"), _quests), "recruitment.json": (RECRUITMENT_SECTIONS, _recruitment), "sanctums.json": (("encounters", "sanctums"), _sanctums), "situations.json": (("situations", "afterwork_samples"), _situations), "skills.json": (("branches", "role_roots"), _skills), "spells.json": (("spells",), _spells), "terrain_variation.json": (("regions",), _terrain), "vehicles.json": (("harbour", "vehicles"), _vehicles), "vessel.json": (VESSEL_SECTIONS, _vessel), "visuals.json": (VISUAL_SECTIONS, _visuals), "world_text.json": (WORLD_TEXT_SECTIONS, _world_text),
}
if set(_CATALOG_PROJECTORS) != set(REQUIRED_CATALOGS):
    raise RuntimeError("mechanical compatibility catalog registry does not match required main-world catalogs")


def main_world_mechanical_projection() -> dict[str, Any]:
    """Return the validated, JSON-safe mechanics supplied by the selected pack."""
    catalogs: dict[str, Any] = {}
    for name in sorted(_CATALOG_PROJECTORS):
        sections, projector = _CATALOG_PROJECTORS[name]
        catalogs[name] = projector(load_catalog(name, sections))
    return {"projection_format": MECHANICAL_PROJECTION_FORMAT, "mechanical_compatibility_version": MECHANICAL_COMPATIBILITY_VERSION, "catalogs": catalogs}


@lru_cache(maxsize=1)
def canonical_mechanical_projection_bytes() -> bytes:
    try:
        return json.dumps(main_world_mechanical_projection(), sort_keys=True, separators=(",", ":"), ensure_ascii=True, allow_nan=False).encode("utf-8")
    except (TypeError, ValueError) as exc:
        raise MechanicalProjectionError(f"mechanical compatibility projection is not canonical JSON: {exc}") from exc


@lru_cache(maxsize=1)
def main_world_mechanical_fingerprint() -> str:
    return sha256(canonical_mechanical_projection_bytes()).hexdigest()
