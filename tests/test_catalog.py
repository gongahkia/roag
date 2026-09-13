from __future__ import annotations

from dataclasses import asdict
import json
import unittest

from jomon.catalog import CatalogError, decode_catalog, load_catalog
from jomon.chemistry import ENVIRONMENT_REACTIONS, REACTIONS, REAGENTS
from jomon.content import COMMODITIES, ENEMY_ARCHETYPES, RECRUIT_TEMPLATES, validate_commodity_content
from jomon.inventory import item_spec
from jomon.magic import SPELL_ROWS
from jomon.production import RECIPES, SHORE_STATIONS, SITE_KEYS, SOURCES
from jomon.quests import ADDITIONAL_ARCS, ARC_REGIONS, ARC_TITLE, FIELD_REPORT_RESPONSES, QUESTS, QUEST_REWARDS
from jomon.situations import AFTERWORK_SAMPLES, SITUATIONS, validate_situations


class CatalogTests(unittest.TestCase):
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

    def test_cross_catalog_references_remain_playable(self):
        validate_commodity_content()
        validate_situations()
        self.assertEqual(set(AFTERWORK_SAMPLES), set(SOURCES))
        self.assertTrue(all(sample in SOURCES[region] for region, sample in AFTERWORK_SAMPLES.items()))
        self.assertTrue(all(name in REAGENTS for sources in SOURCES.values() for name in sources))
        self.assertTrue(all(item_spec(recipe.output) and all(item_spec(kind) for kind, _ in recipe.inputs)
                            for recipe in RECIPES.values()))
        self.assertEqual(len(ENEMY_ARCHETYPES), 95)
        self.assertEqual(len(RECRUIT_TEMPLATES), 6)
        self.assertEqual(len(COMMODITIES), 8)
