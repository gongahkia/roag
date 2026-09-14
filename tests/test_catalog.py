from __future__ import annotations

from dataclasses import asdict
import json
import unittest

from jomon.catalog import (
    ACTOR_SECTIONS, ARC_RELIC_SECTIONS, AFTERMATH_SECTIONS, CHARACTER_SECTIONS,
    EQUIPMENT_SECTIONS, GEOGRAPHY_SECTIONS, HISTORY_SECTIONS, PRACTICE_SECTIONS,
    RECRUITMENT_SECTIONS, VESSEL_SECTIONS, WORLD_TEXT_SECTIONS,
    CatalogError, decode_catalog, load_catalog,
)
from jomon.aftermath import (
    AFTERMATH_LINES, AFTERMATH_TOPOLOGIES, DRAINAGE_TOPOLOGIES,
    FIRE_TOPOLOGIES, RECOVERY_TOPOLOGIES, SUPPORT_TOPOLOGIES,
)
from jomon.arc_relics import ARC_RELIC_DESCRIPTIONS, ARC_RELICS
from jomon.character import (
    ANCESTRIES, ATTRIBUTE_FOR_COMPETENCY, ATTRIBUTE_POINTS, COMPETENCIES,
    COMPETENCY_POINTS, ORIGINS, ORIGIN_PRACTICE, PEOPLE_COMPETENCIES,
    PEOPLE_EFFECTS, ROLE_ATTRIBUTES, ROLE_COMPETENCIES, TRAITS,
)
from jomon.chemistry import ENVIRONMENT_REACTIONS, REACTIONS, REAGENTS
from jomon.content import (
    COMMODITIES, ENEMY_ARCHETYPES, INTERFACE_LEDGERS, RECRUIT_TEMPLATES,
    validate_commodity_content,
)
from jomon.echoes import ECHOES
from jomon.enemy_equipment import REGIONAL_ARMOUR as ENEMY_REGIONAL_ARMOUR
from jomon.expanded_weapons import ARSENAL, BOMB_AMMUNITION
from jomon.frontier_elites import AFTERMATH_ELITES, ELITE_ROWS, NAMED_RIVALS
from jomon.frontiers import FRONTIER_DISCOVERIES, FRONTIER_RELICS, FRONTIERS
from jomon.geography import FIELD_SECRETS
from jomon.household_stories import STORIES
from jomon.inspection import TERRAIN_NAMES
from jomon.interference import INTERFERENCES
from jomon.inventory import (
    AMMUNITION_ITEMS, BASIC_COURIER_ARMOUR, BASIC_COURIER_LOADOUTS,
    ITEM_SPECS, REGIONAL_ARMOUR, WEAPON_AMMUNITION, item_spec,
)
from jomon.magic import SPELL_ROWS
from jomon.main import SEED_WORDS
from jomon.navigation import LANDMARK_LABELS
from jomon.preparations import PREPARATIONS
from jomon.practices import AFTERMATH_REGION_PRACTICE, NETWORK_CONTACT_PRACTICE, PRACTICES
from jomon.production import RECIPES, SHORE_STATIONS, SITE_KEYS, SOURCES
from jomon.quests import ADDITIONAL_ARCS, ARC_REGIONS, ARC_TITLE, FIELD_REPORT_RESPONSES, QUESTS, QUEST_REWARDS
from jomon.route_chart import REGION_NODES, build_route_graph
from jomon.ship_crises import HAZARD_STATIONS, VOYAGES
from jomon.situations import AFTERWORK_SAMPLES, SITUATIONS, validate_situations
from jomon.skill_tree import BRANCHES, NODES, ROLE_ROOTS
from jomon.legendary import CRISIS_TAG, LEGEND_BASES
from jomon.people import RECRUIT_REQUIREMENTS
from jomon.regional_history import (
    INSTITUTION_SERVICES, INSTITUTION_TIES, NETWORK_ACCOUNTS, NETWORK_CONTACTS,
    WORKING_ACCOUNTS,
)
from jomon.vessel_refits import REFITS
from jomon.vessel import DRINKS
from jomon.vehicles import SPECS as VEHICLE_SPECS, VEHICLE_REGIONS
from jomon.voyage_variants import VARIANTS
from jomon.work_weapons import POT_AMMUNITION, WORK_WEAPONS
from jomon.workshop import FITTINGS
from jomon.worklines import WORKLINES


class CatalogTests(unittest.TestCase):
    def test_newly_externalized_authoring_rebuilds_runtime_shapes(self):
        history = load_catalog("history.json", HISTORY_SECTIONS)
        self.assertEqual(history["working_accounts"], json.loads(json.dumps(WORKING_ACCOUNTS)))
        self.assertEqual(history["institution_services"], json.loads(json.dumps(INSTITUTION_SERVICES)))
        self.assertEqual(history["institution_ties"], json.loads(json.dumps(INSTITUTION_TIES)))
        self.assertEqual(history["network_accounts"], json.loads(json.dumps(NETWORK_ACCOUNTS)))
        self.assertEqual(history["network_contacts"], json.loads(json.dumps(NETWORK_CONTACTS)))
        self.assertEqual(history["undertakings"], json.loads(json.dumps(WORKLINES)))
        self.assertEqual(history["legend_bases"], json.loads(json.dumps(LEGEND_BASES)))
        self.assertEqual(history["crisis_tags"], CRISIS_TAG)

        practices = load_catalog("practices.json", PRACTICE_SECTIONS)
        self.assertEqual(practices["practices"], [list(asdict(row).values()) for row in PRACTICES.values()])
        self.assertEqual(practices["network_contacts"], NETWORK_CONTACT_PRACTICE)
        self.assertEqual(practices["aftermath_regions"], AFTERMATH_REGION_PRACTICE)

        profiles = load_catalog("character_profiles.json", CHARACTER_SECTIONS)
        self.assertEqual(profiles["competencies"], list(COMPETENCIES))
        self.assertEqual(profiles["starting_points"], {"attributes": ATTRIBUTE_POINTS, "competencies": COMPETENCY_POINTS})
        self.assertEqual(profiles["origins"], list(ORIGINS))
        self.assertEqual(profiles["traits"], json.loads(json.dumps(TRAITS)))
        self.assertEqual(profiles["origin_practices"], ORIGIN_PRACTICE)
        self.assertEqual(profiles["attribute_competencies"], ATTRIBUTE_FOR_COMPETENCY)
        self.assertEqual(profiles["role_attributes"], json.loads(json.dumps(ROLE_ATTRIBUTES)))
        self.assertEqual(profiles["role_competencies"], json.loads(json.dumps(ROLE_COMPETENCIES)))
        self.assertEqual(
            profiles["ancestries"],
            {name: {"effect": PEOPLE_EFFECTS[name], "competency": PEOPLE_COMPETENCIES.get(name)} for name in ANCESTRIES},
        )

        recruitment = load_catalog("recruitment.json", RECRUITMENT_SECTIONS)
        self.assertEqual(recruitment["requirements"], {
            person_id: {"region": region, "markers": list(markers), "witnessed": witnessed}
            for person_id, (region, markers, witnessed) in RECRUIT_REQUIREMENTS.items()
        })
        relics = load_catalog("arc_relics.json", ARC_RELIC_SECTIONS)
        self.assertEqual(relics["relics"], [
            [arc_id, choice, name, ARC_RELIC_DESCRIPTIONS[name]]
            for (arc_id, choice), name in ARC_RELICS.items()
        ])
        world_text = load_catalog("world_text.json", WORLD_TEXT_SECTIONS)
        self.assertEqual(world_text["seed_words"], list(SEED_WORDS))
        self.assertEqual(world_text["terrain_names"], TERRAIN_NAMES)
        self.assertEqual(world_text["landmark_labels"], LANDMARK_LABELS)
        self.assertEqual(world_text["interface_ledgers"], {
            key: list(value) if isinstance(value, tuple) else value
            for key, value in INTERFACE_LEDGERS.items()
        })
        vessel = load_catalog("vessel.json", VESSEL_SECTIONS)
        self.assertEqual(vessel["drinks"], json.loads(json.dumps([asdict(drink) for drink in DRINKS.values()])))
        vehicles = load_catalog("vehicles.json", ("harbour", "vehicles"))
        self.assertEqual(vehicles["vehicles"], VEHICLE_SPECS)
        self.assertEqual(
            {vehicle_id: spec["region"] for vehicle_id, spec in vehicles["vehicles"].items()},
            VEHICLE_REGIONS,
        )

    def test_specialized_catalogues_rebuild_original_runtime_shapes(self):
        actors = load_catalog("actors.json", ACTOR_SECTIONS)
        self.assertEqual(actors["FRONTIER_ELITES"]["rows"], [list(row) for row in ELITE_ROWS])
        self.assertEqual(set(actors["FRONTIER_ELITES"]["aftermath"]), AFTERMATH_ELITES)
        self.assertEqual(set(actors["FRONTIER_ELITES"]["named"]), NAMED_RIVALS)

        geography = load_catalog("geography.json", GEOGRAPHY_SECTIONS)
        self.assertEqual(geography["FIELD_SECRETS"], FIELD_SECRETS)
        self.assertEqual(set(geography["FRONTIERS"]), set(FRONTIERS))
        self.assertEqual(
            {region: tuple(row["discoveries"]) for region, row in geography["FRONTIERS"].items()},
            FRONTIER_DISCOVERIES,
        )
        self.assertEqual(
            {region: tuple(row["relics"]) for region, row in geography["FRONTIERS"].items()},
            FRONTIER_RELICS,
        )

        vessel = load_catalog("vessel.json", VESSEL_SECTIONS)
        self.assertEqual(vessel["voyages"], json.loads(json.dumps(VOYAGES)))
        self.assertEqual(vessel["hazard_stations"], {kind: asdict(point) for kind, point in HAZARD_STATIONS.items()})
        self.assertEqual(vessel["variants"], [asdict(row) for row in VARIANTS.values()])
        self.assertEqual(vessel["echoes"], [asdict(row) for row in ECHOES])
        self.assertEqual(vessel["refits"], [asdict(row) for row in REFITS.values()])
        self.assertEqual(vessel["region_nodes"], REGION_NODES)
        route_nodes, route_edges = build_route_graph("catalogue-rebuild")
        self.assertEqual(vessel["route_nodes"], {node_id: asdict(node) for node_id, node in route_nodes.items()})
        self.assertEqual([edge.id for edge in route_edges[:len(vessel["route_edges"])]],
                         [row[0] for row in vessel["route_edges"]])
        self.assertEqual(
            [asdict(edge) for edge in route_edges],
            [asdict(edge) for edge in build_route_graph("catalogue-rebuild")[1]],
        )

        aftermath = load_catalog("aftermath.json", AFTERMATH_SECTIONS)
        self.assertEqual(aftermath["lines"], json.loads(json.dumps(AFTERMATH_LINES)))
        self.assertEqual(aftermath["topologies"], json.loads(json.dumps(AFTERMATH_TOPOLOGIES)))
        for name, values in (
            ("drainage_topologies", DRAINAGE_TOPOLOGIES),
            ("fire_topologies", FIRE_TOPOLOGIES),
            ("support_topologies", SUPPORT_TOPOLOGIES),
            ("recovery_topologies", RECOVERY_TOPOLOGIES),
        ):
            self.assertEqual(aftermath[name], sorted(values))
        self.assertEqual(aftermath["preparations"], [asdict(row) for row in PREPARATIONS.values()])
        self.assertEqual(aftermath["household_stories"], [asdict(row) for row in STORIES])
        self.assertEqual(aftermath["interferences"], [asdict(row) for row in INTERFERENCES])

        equipment = load_catalog("equipment.json", EQUIPMENT_SECTIONS)
        self.assertTrue(all(AMMUNITION_ITEMS[name] == kind for name, kind in equipment["ammunition_items"].items()))
        self.assertTrue(all(WEAPON_AMMUNITION[name] == kind for name, kind in equipment["weapon_ammunition"].items()))
        self.assertEqual(equipment["basic_courier_loadouts"], json.loads(json.dumps(BASIC_COURIER_LOADOUTS)))
        self.assertEqual(equipment["basic_courier_armour"], BASIC_COURIER_ARMOUR)
        self.assertEqual(equipment["item_specs"], json.loads(json.dumps({
            kind: asdict(ITEM_SPECS[kind]) for kind in equipment["item_specs"]
        })))
        self.assertEqual(equipment["regional_armour"], json.loads(json.dumps(REGIONAL_ARMOUR)))
        self.assertEqual(equipment["work_weapons"], json.loads(json.dumps({
            name: asdict(row) for name, row in WORK_WEAPONS.items()
        })))
        self.assertEqual(equipment["pot_ammunition"], POT_AMMUNITION)
        self.assertEqual(equipment["arsenal"], json.loads(json.dumps([asdict(row) for row in ARSENAL.values()])))
        self.assertEqual(equipment["bomb_ammunition"], BOMB_AMMUNITION)
        self.assertEqual(equipment["fittings"], json.loads(json.dumps({
            name: asdict(row) for name, row in FITTINGS.items()
        })))
        self.assertEqual(equipment["enemy_regional_armour"], json.loads(json.dumps(ENEMY_REGIONAL_ARMOUR)))

    def test_decoder_rejects_duplicate_keys_nonfinite_numbers_and_wrong_sections(self):
        for text in ('{"rows": 1, "rows": 2}', '{"rows": NaN}', '{"other": []}'):
            with self.subTest(text=text), self.assertRaises(CatalogError):
                decode_catalog(text, "test.json", ("rows",))
        with self.assertRaises(CatalogError):
            load_catalog("../production.json", ("recipes",))

    def test_packaged_records_reconstruct_original_runtime_types_and_order(self):
        situations = load_catalog("situations.json", ("situations", "afterwork_samples"))
        self.assertEqual(situations["situations"], json.loads(json.dumps([asdict(row) for row in SITUATIONS])))
        self.assertEqual(situations["afterwork_samples"], AFTERWORK_SAMPLES)
        production = load_catalog("production.json", ("sources", "site_keys", "shore_stations", "recipes"))
        self.assertEqual(production["sources"], {region: list(names) for region, names in SOURCES.items()})
        self.assertEqual(production["site_keys"], SITE_KEYS)
        self.assertEqual(production["shore_stations"], {region: list(names) for region, names in SHORE_STATIONS.items()})
        authored = json.loads(json.dumps([asdict(row) for row in RECIPES.values() if not row.id.startswith("make:")]))
        self.assertEqual(production["recipes"], authored)
        chemistry = load_catalog("chemistry.json", ("reagents", "reactions", "environment_reactions"))
        self.assertEqual(chemistry["reagents"], list(REAGENTS))
        self.assertEqual(chemistry["reactions"], [
            {"reagents": sorted(pair), "name": name, "effect": effect}
            for pair, (name, effect) in REACTIONS.items()
        ])
        self.assertEqual(chemistry["environment_reactions"], ENVIRONMENT_REACTIONS)
        reports = load_catalog("field_reports.json", ("responses",))
        self.assertEqual(reports["responses"], {region: list(lines) for region, lines in FIELD_REPORT_RESPONSES.items()})
        self.assertEqual(load_catalog("spells.json", ("spells",))["spells"], json.loads(json.dumps(SPELL_ROWS)))
        quest_data = load_catalog("quests.json", ("quests", "rewards", "arc_regions", "arc_title", "additional_arcs"))
        self.assertEqual(quest_data["quests"], json.loads(json.dumps(QUESTS)))
        self.assertEqual(quest_data["rewards"], QUEST_REWARDS)
        self.assertEqual(quest_data["arc_regions"], {str(index): region for index, region in ARC_REGIONS.items()})
        self.assertEqual(quest_data["arc_title"], ARC_TITLE)
        self.assertEqual(quest_data["additional_arcs"], json.loads(json.dumps(ADDITIONAL_ARCS)))
        skill_data = load_catalog("skills.json", ("branches", "role_roots"))
        self.assertEqual(skill_data["branches"], json.loads(json.dumps(BRANCHES)))
        self.assertEqual(skill_data["role_roots"], json.loads(json.dumps(ROLE_ROOTS)))

    def test_cross_catalog_references_remain_playable(self):
        validate_commodity_content()
        validate_situations()
        self.assertEqual(set(AFTERWORK_SAMPLES), set(SOURCES))
        self.assertTrue(all(sample in SOURCES[region] for region, sample in AFTERWORK_SAMPLES.items()))
        self.assertTrue(all(name in REAGENTS for sources in SOURCES.values() for name in sources))
        self.assertTrue(all(item_spec(recipe.output) and all(item_spec(kind) for kind, _ in recipe.inputs)
                            for recipe in RECIPES.values()))
        self.assertEqual(len(ENEMY_ARCHETYPES), 111)
        self.assertEqual(len(RECRUIT_TEMPLATES), 6)
        self.assertEqual(len(COMMODITIES), 8)
        self.assertEqual(len(NODES), 60)
