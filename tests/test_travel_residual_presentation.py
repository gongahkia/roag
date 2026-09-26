from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import ContentPackError, load_content_pack
from jomon.calendar import calendar_at
from jomon.state import create_world
from jomon.travel_presentation import echo_title, route_edge_hazard, variant_display_name
from tests.test_content_packs import ROOT, alternate_pack


class TravelResidualPresentationTests(unittest.TestCase):
    def test_default_residual_presentation_uses_stable_keys(self):
        self.assertEqual(variant_display_name("shortage-skiffs"), "Shortage skiffs")
        self.assertEqual(echo_title("shortage-skiffs"), "Hooks remembered at the next mooring")
        route = create_world("travel-residual-edge")
        edge = next(edge for edge in route.route_edges if edge.id == "h-r")
        self.assertEqual(route_edge_hazard(edge), "soft spring bank")
        state = create_world("travel-residual-observance")
        state.calendar_origin_day = 0
        self.assertEqual(calendar_at(state, 0).observance_id, "spring-equinox")
        self.assertEqual(calendar_at(state, 0).observance, "spring equinox")

    def test_invalid_residual_travel_entries_are_rejected(self):
        cases = (
            (lambda text: text.pop("travel.variant.shortage-skiffs.name"), "missing required travel keys"),
            (lambda text: text.pop("travel.echo.shortage-skiffs.title"), "missing required travel keys"),
            (lambda text: text.pop("travel.route.node.reed-anchor.name"), "missing required travel keys"),
            (lambda text: text.pop("travel.calendar.observance.spring-equinox"), "missing required travel keys"),
            (lambda text: text.__setitem__("travel.route.edge.unknown.hazard", "Unknown"), "unknown travel keys"),
            (lambda text: text.__setitem__("travel.variant.shortage-skiffs.name", ""), "must be a non-empty string"),
            (lambda text: text.__setitem__("travel.echo.shortage-skiffs.title", 3), "must be a non-empty string"),
            (lambda text: text.__setitem__("travel.calendar.date", "Year {year"), "malformed template"),
            (lambda text: text.__setitem__("travel.route.preview.leg", "{hazard} {time}"), "must contain exactly placeholders"),
            (lambda text: text.__setitem__("travel.route.preview.leg", "{hazard} {time} {supply} {unknown}"), "must contain exactly placeholders"),
        )
        for change, reason in cases:
            with self.subTest(reason=reason), tempfile.TemporaryDirectory() as directory:
                root = alternate_pack(Path(directory) / "fixture")
                source = root / "travel_text.json"
                document = json.loads(source.read_text(encoding="utf-8"))
                change(document["text"])
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, reason):
                    load_content_pack(root)

    def test_duplicate_residual_travel_key_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "travel_text.json"
            raw = source.read_text(encoding="utf-8")
            needle = '"travel.variant.shortage-skiffs.name": "Shortage skiffs"'
            self.assertIn(needle, raw)
            source.write_text(raw.replace(needle, needle + ',\n    "travel.variant.shortage-skiffs.name": "Duplicate"', 1), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "duplicate catalog key"):
                load_content_pack(root)

    def test_alternate_pack_changes_residual_words_not_travel_mechanics(self):
        script = (
            "import json; "
            "from jomon.calendar import calendar_at, seasonal_stock_modifier; "
            "from jomon.echoes import apply_later_echoes; "
            "from jomon.route_chart import edge_between, neighbours, route_availability, route_preview; "
            "from jomon.state import CommodityStack, create_world; "
            "from jomon.travel import choose_destination, resolve_voyage; "
            "from jomon.voyage_variants import active_variant, select_variant; "
            "variant=create_world('travel-residual-variant'); variant.weapon='pike'; variant.vessel_cargo['grain']=CommodityStack(3,'dry'); "
            "entry=next(iter(next(iter(variant.regional_markets.values())).values())); entry.stock=0; entry.demand=4; "
            "selected=select_variant(variant,'raiders','reed-anchor'); opened=choose_destination(variant,'reed-anchor',forced_voyage='raiders'); resolved=resolve_voyage(variant,'repel'); "
            "variant_state={'id':selected.id,'family':selected.family,'opened':opened[0],'resolved':resolved[0],'travel_count':variant.travel_count,'world_time':variant.world_time,'node':variant.route_current_node,'status':variant.voyage_status,'integrity':variant.vessel_integrity,'variant_state':variant.vessel_changes.get('voyage_variant:1')}; "
            "variant_text=[selected.name,variant.voyage_detail,resolved[1]]; "
            "variant.travel_count=2; applied=apply_later_echoes(variant); echo=applied[0]; "
            "echo_state={'source':echo.variant_id,'kind':echo.kind,'marker':bool(variant.vessel_changes.get('voyage_echo:'+echo.variant_id)),'deck_scars':sorted(k for k in variant.vessel_changes if k.startswith('deck_scar:')),'route_risk':variant.route_nodes['reed-anchor'].risk,'accounts':sorted((key,value.obligation) for key,value in variant.institutions.items())}; "
            "echo_text=[echo.title,echo.consequence,variant.chronicle[-1],variant.region.changes.get('voyage-echo:'+echo.variant_id)]; "
            "route=create_world('travel-residual-route'); edge=edge_between(route,'hearthford','reed-anchor'); before=(route.world_time,route.route_current_node); moved=choose_destination(route,'reed-anchor'); "
            "route_state={'nodes':sorted(route.route_nodes),'edge':(edge.id,edge.first,edge.second,edge.travel_time,edge.supply_cost,edge.cargo_risk,edge.weather_exposure,tuple(edge.closed_seasons)),'neighbours':neighbours(create_world('travel-residual-route'),'hearthford'),'before':before,'moved':moved[0],'after':(route.world_time,route.route_current_node),'known':sorted(route.route_known)}; "
            "route_text=route_preview(create_world('travel-residual-route'),'reed-anchor'); "
            "calendar=create_world('travel-residual-calendar'); calendar.calendar_origin_day=0; date=calendar_at(calendar,0); "
            "calendar_state=(date.year,date.season,date.day,date.time_of_day,date.observance_id,seasonal_stock_modifier(calendar,'reed-tonic')); calendar_text=(date.label,date.observance); "
            "print(json.dumps({'mechanics':{'variant':variant_state,'echo':echo_state,'route':route_state,'calendar':calendar_state},'presentation':{'variant':variant_text,'echo':echo_text,'route':route_text,'calendar':calendar_text}}))"
        )
        default = subprocess.run([sys.executable, "-c", script], cwd=ROOT, text=True, capture_output=True, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "travel_text.json"
            document = json.loads(source.read_text(encoding="utf-8"))
            text = document["text"]
            text.update({
                "travel.variant.shortage-skiffs.name": "Fixture shortage fleet",
                "travel.variant.shortage-skiffs.cause": "Fixture cause preserves the shortage qualifier.",
                "travel.variant.shortage-skiffs.effect": "Fixture effect preserves the third raider.",
                "travel.variant.shortage-skiffs.counterplay": "Fixture counterplay preserves all choices.",
                "travel.echo.shortage-skiffs.title": "Fixture echo title",
                "travel.echo.shortage-skiffs.consequence": "fixture echo consequence preserves the same effect",
                "travel.echo.activation": "FIXTURE ECHO {title}: {consequence}.",
                "travel.route.node.reed-anchor.name": "Fixture Reed Mooring",
                "travel.route.node.reed-anchor.description": "fixture route description",
                "travel.route.edge.h-r.hazard": "fixture channel hazard",
                "travel.route.preview.current": "FIXTURE PREVIEW {name}: {description}",
                "travel.calendar.season.spring": "Fixture Spring",
                "travel.calendar.observance.spring-equinox": "fixture spring marker",
                "travel.calendar.route_note.spring": "fixture seasonal route note",
                "travel.calendar.date": "Fixture year {year} / {season} / {day} / {time}",
            })
            source.write_text(json.dumps(document), encoding="utf-8")
            alternate = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=dict(os.environ, JOMON_CONTENT_PACK=str(root)), text=True, capture_output=True, check=True)
        default_data, alternate_data = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(default_data["mechanics"], alternate_data["mechanics"])
        self.assertNotEqual(default_data["presentation"], alternate_data["presentation"])
        self.assertIn("Fixture shortage fleet", " ".join(default for default in alternate_data["presentation"]["variant"] if default))
        self.assertIn("Fixture echo title", " ".join(value for value in alternate_data["presentation"]["echo"] if value))
        self.assertIn("Fixture Reed Mooring", " ".join(alternate_data["presentation"]["route"]))
        self.assertIn("Fixture year", alternate_data["presentation"]["calendar"][0])


if __name__ == "__main__":
    unittest.main()
