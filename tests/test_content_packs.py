from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

from jomon.catalog import (
    ContentPackError,
    WORLD_TEXT_SECTIONS,
    bundled_default_pack,
    character_contract,
    item_contract,
    load_catalog,
    load_content_pack,
    region_contract,
    role_contract,
    ui_contract,
)


ROOT = Path(__file__).parents[1]
DATA_ROOT = ROOT / "jomon" / "data"
DEFAULT_PACK_ROOT = ROOT / "jomon" / "content_packs" / "default"


def write_manifest(root: Path, document: str) -> None:
    root.mkdir(parents=True, exist_ok=True)
    (root / "manifest.json").write_text(document, encoding="utf-8")


def alternate_pack(root: Path) -> Path:
    """Build a complete external fixture without committing copied game data."""
    shutil.copytree(DATA_ROOT, root / "data")
    shutil.copy(DEFAULT_PACK_ROOT / "regions.json", root / "regions.json")
    shutil.copy(DEFAULT_PACK_ROOT / "characters.json", root / "characters.json")
    shutil.copy(DEFAULT_PACK_ROOT / "items.json", root / "items.json")
    shutil.copy(DEFAULT_PACK_ROOT / "ui_text.json", root / "ui_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "quests.json", root / "quests.json")
    shutil.copy(DEFAULT_PACK_ROOT / "history_text.json", root / "history_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "aftermath_text.json", root / "aftermath_text.json")
    write_manifest(
        root,
        '{"id": "fixture-alternate", "display_name": "Fixture Alternate", "format_version": 1}',
    )
    source = root / "data" / "world_text.json"
    world_text = json.loads(source.read_text(encoding="utf-8"))
    world_text["seed_words"][0] = "fixture-reed"
    source.write_text(json.dumps(world_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "regions.json"
    regions = json.loads(source.read_text(encoding="utf-8"))
    regions["regions"]["region.family_1"] = {
        "display_name": "Fixture Hearth",
        "route_label": "Fixture Ford",
        "short_description": "fixture river presentation",
    }
    source.write_text(json.dumps(regions, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "characters.json"
    characters = json.loads(source.read_text(encoding="utf-8"))
    characters["characters"]["npc.ship_bartender"] = {
        "display_name": "Fixture Host",
        "short_description": "Keeps the fixture common room supplied.",
        "initial_memory": "Fixture Host took the fixture bar.",
        "build_tendency": "fixture hospitality",
    }
    characters["characters"]["npc.ship_merchant"] = {
        "display_name": "Fixture Trader",
        "short_description": "Visits on the fixture route cycle.",
        "initial_memory": "Fixture Trader knows the fixture markets.",
        "build_tendency": "fixture exchange",
    }
    characters["characters"]["npc.hearthford_second_contact"] = {
        "display_name": "Fixture Miller",
        "role_label": "fixture mill speaker",
    }
    characters["roles"]["role.household_bargemaster"] = {"display_label": "fixture navigator"}
    characters["roles"]["role.ship_merchant"] = {"display_label": "fixture deck trader"}
    source.write_text(json.dumps(characters, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "items.json"
    items = json.loads(source.read_text(encoding="utf-8"))
    items["items"]["item.equipment_002"]["display_name"] = "Fixture Spear"
    items["items"]["item.equipment_044"]["display_name"] = "Fixture Waders"
    items["items"]["item.goods_014"]["display_name"] = "Fixture Dressing"
    items["items"]["item.goods_050"]["display_name"] = "Fixture Rain Cape"
    source.write_text(json.dumps(items, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "ui_text.json"
    ui = json.loads(source.read_text(encoding="utf-8"))
    ui["text"].update({
        "ui.title.game": "F I X T U R E",
        "ui.label.stores_ledger": "FIXTURE PACK",
        "ui.help.general.01": "Fixture help explains the unchanged lookout control.",
        "ui.notice.rumour.label": "HEARSAY",
        "ui.notice.warning.label": "CAUTION",
        "ui.tavern.draw.title": "FIXTURE DRAW",
    })
    source.write_text(json.dumps(ui, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "quests.json"
    quests = json.loads(source.read_text(encoding="utf-8"))
    quests["quests"]["quest.regional.hearthford"]["title"] = "Fixture Water Claim"
    quests["quests"]["quest.regional.hearthford"]["lead"] = "Fixture witness marks the fixture cache beneath the fixture road."
    quests["quests"]["quest.regional.hearthford"]["choices"]["l"]["label"] = "Make the fixture public settlement"
    quests["quests"]["quest.arc.banks"] = {"title": "Fixture Bank Accord"}
    quests["quests"]["quest.evidence.banks"] = {
        "evidence_name": "fixture bank record",
        "evidence_description": "A fixture physical record that keeps the same evidence mechanics.",
    }
    quests["arc_choices"]["banks"]["0"]["p"]["label"] = "Publish the fixture bank record"
    quests["arc_choices"]["marks"]["0"]["o"]["label"] = "Open the fixture marks"
    quests["arc_choices"]["soundings"]["3"]["s"]["requirement"] = "recover the fixture sounding record"
    quests["arc_results"]["banks"]["o"] = "Fixture banks ease the same routes."
    quests["arc_results"]["soundings"]["s"] = "Fixture soundings preserve the same winter route effects."
    quests["services"]["quest.service.cache_mark"]["label"] = "Fixture cache service"
    quests["services"]["quest.service.treatment"]["requirement"] = "fixture care is unnecessary"
    quests["services"]["quest.service.practical_instruction"]["results"]["completed"] = "Fixture tutor {contact} grants {technique}: {effect}."
    quests["services"]["quest.service.treatment"]["results"]["completed"] = "Fixture healer {contact} treats the {location} injury; the care takes time and leaves an obligation."
    quests["services"]["quest.service.public_field_report"]["results"]["completed"] = "{record} Fixture trust is recorded."
    source.write_text(json.dumps(quests, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "history_text.json"
    history_text = json.loads(source.read_text(encoding="utf-8"))
    history_text["text"]["history.event.crisis.account"] = "Fixture witness {witness} records {crisis} at the {landmark}."
    history_text["text"]["history.network_service.shelter.label"] = "Open the fixture shelter route"
    source.write_text(json.dumps(history_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "aftermath_text.json"
    aftermath = json.loads(source.read_text(encoding="utf-8"))
    aftermath["contracts"]["aftermath.contract.hearthford.supply"]["title"] = "Fixture Flood Marks"
    aftermath["contracts"]["aftermath.contract.greywash.scar"]["title"] = "Fixture Wreck Title"
    source.write_text(json.dumps(aftermath, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    return root


def world_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.state import create_world; "
            "from jomon.terminal import RouteChartView, route_detail_lines, _status_lines; "
            "state = create_world('regional-pack-proof'); node = state.route_nodes['hearthford']; "
            "state.location = 'region'; state.position = state.region.landmarks['landing']; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'region': [state.region.id, state.region.name], "
            "'route': [node.id, node.name, node.description], "
            "'route_detail': route_detail_lines(state, RouteChartView('hearthford'), 80), "
            "'status': _status_lines(state), 'signature': state.region.geography_signature, "
            "'levels': state.region.levels, "
            "'landmarks': sorted((key, point.x, point.y, point.z) for key, point in state.region.landmarks.items()), "
            "'edges': [(edge.id, edge.first, edge.second, edge.travel_time, edge.supply_cost, edge.cargo_risk, edge.weather_exposure) for edge in state.route_edges]}))",
        ],
        cwd=ROOT,
        env=environment,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def character_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.state import create_world; from jomon.terminal import _overlay_lines, _status_lines; "
            "state = create_world('character-pack-proof'); bartender = state.bartender; merchant = state.merchant; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'bartender': [bartender.id, bartender.name, bartender.role, bartender.equipment, bartender.technique, bartender.background, bartender.memories, bartender.build_tendency], "
            "'merchant': [merchant.id, merchant.name, merchant.role, merchant.equipment, merchant.technique, merchant.background, merchant.memories, merchant.build_tendency], "
            "'second_contact': [(contact.id, contact.name, contact.role, contact.disposition, contact.interest) for contact in state.contacts['hearthford'] if contact.id == 'hearthford-contact-2'][0], "
            "'household': [(person.id, person.role, person.equipment, person.technique) for person in state.household], "
            "'bartender_schedule': [state.actor_schedules[bartender.id].area, state.actor_schedules[bartender.id].position.x, state.actor_schedules[bartender.id].position.y, state.actor_schedules[bartender.id].activity], "
            "'merchant_schedule': [state.actor_schedules[merchant.id].area, state.actor_schedules[merchant.id].position.x, state.actor_schedules[merchant.id].position.y, state.actor_schedules[merchant.id].activity], "
            "'stock': state.bartender_stock, 'signature': state.region.geography_signature, "
            "'bartender_overlay': _overlay_lines(state, 'bartender'), 'merchant_overlay': _overlay_lines(state, 'merchant'), 'status': _status_lines(state)}))",
        ],
        cwd=ROOT,
        env=environment,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def item_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import copy, json; from jomon.actions import choose_weapon, move, purchase_merchant_item; "
            "from jomon.inventory import create_item, item_spec; "
            "from jomon.state import Position, create_world, game_state_from_dict; from jomon.terminal import _overlay_lines; from jomon.world import base_tile, is_walkable; "
            "state = create_world('item-pack-proof'); initial_items = sorted((item.kind, item.quantity, item.location, item.owner_id, item.x, item.y) for item in state.items); initial_stock = list(state.merchant_stock); state.location = 'jomon'; state.jomon_space = 'vessel'; "
            "state.trade_credit = 10; state.merchant_present = True; state.merchant_stock = ['willow dressing']; merchant = _overlay_lines(state, 'merchant')[1]; "
            "rain = create_item(state, 'passive:rain cape', 'item pack proof'); chosen = choose_weapon(state, 'spear'); purchased = purchase_merchant_item(state, 'willow dressing'); "
            "state.location = 'region'; state.weather = 'hard rain'; start, dx, dy = next((Position(x, y, 0), dx, dy) for y in range(state.region.height) for x in range(state.region.width) for dx, dy in ((1, 0), (0, 1)) if is_walkable(state, Position(x, y, 0)) and is_walkable(state, Position(x + dx, y + dy, 0)) and base_tile(state, Position(x + dx, y + dy, 0)) not in {'m', 'r', 'q', 't', 'w', ','}); "
            "cape, bare = copy.deepcopy(state), copy.deepcopy(state); cape.position = bare.position = start; cape.carried_passives = {'rain cape': 1}; bare.carried_passives = {}; cape_before, bare_before = cape.world_time, bare.world_time; move(cape, dx, dy); move(bare, dx, dy); "
            "saved = game_state_from_dict(state.to_dict()); state.location = 'jomon'; state.jomon_space = 'vessel'; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'names': [item_spec('spear').name, item_spec('marsh waders').name, item_spec('consumable:willow dressing').name, item_spec('passive:rain cape').name], "
            "'kinds': [next(item.kind for item in state.items if item.kind == 'spear'), rain.kind, next(item.kind for item in state.items if item.kind == 'consumable:willow dressing')], 'initial_items': initial_items, 'initial_stock': initial_stock, "
            "'equip': [chosen.changed, state.weapon], 'purchase': [purchased.changed, state.merchant_stock], "
            "'weather_steps': [cape.world_time - cape_before, bare.world_time - bare_before], 'saved_kinds': sorted(set(item.kind for item in saved.items if item.kind in {'spear', 'passive:rain cape', 'consumable:willow dressing'})), "
            "'merchant': merchant, 'inventory': _overlay_lines(state, 'inventory')[1]}))",
        ],
        cwd=ROOT,
        env=environment,
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def ui_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.state import create_world; from jomon.terminal import _overlay_lines, event_feed_lines, information_colour_role; from jomon.ui_presentation import notice_kind, render_notice, ui_text; "
            "state = create_world('ui-pack-proof'); print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'title': ui_text('ui.title.game'), 'inventory': _overlay_lines(state, 'inventory')[0], 'help': ui_text('ui.help.general.01'), "
            "'notices': event_feed_lines(['RUMOUR: old save claim', 'WARNING: old save risk'], 80, 4), "
            "'kinds': [notice_kind('RUMOUR: old save claim'), notice_kind('WARNING: old save risk')], "
            "'roles': [information_colour_role('RUMOUR: old save claim'), information_colour_role('WARNING: old save risk')], "
            "'mechanics': [state.seed, state.household[0].role, state.weapon]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def quest_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.inventory import item_spec; from jomon.quests import QUESTS, ADDITIONAL_ARCS, regional_resolution_options, arc_options, resolve_arc_choice; "
            "from jomon.quest_presentation import regional_quest_lead, regional_quest_title, arc_title, evidence_display_name; "
            "from jomon.state import create_world; from jomon.frontiers import ensure_frontier; from jomon.terminal import _overlay_lines; "
            "state=create_world('quest-pack-proof'); ensure_frontier(state, 'dunmire'); state.location='region'; state.cross_region_arcs['banks'].status='available'; bank_options=arc_options(state); bank_result=resolve_arc_choice(state, 'p'); "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'title': regional_quest_title('hearthford'), 'lead': regional_quest_lead('hearthford'), "
            "'arc': arc_title('banks'), 'evidence': [evidence_display_name('banks'), item_spec('consumable:bound bank roll').name], "
            "'engine': [QUESTS['hearthford']['cache'], ADDITIONAL_ARCS['banks']['evidence']], 'choices': regional_resolution_options(state), "
            "'bank_options': bank_options, 'bank_result': [bank_result, state.cross_region_arcs['banks'].stage, state.cross_region_arcs['banks'].branch], 'overlay': _overlay_lines(state, 'quest:regional')[0], 'seed': state.seed}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class ContentPackTests(unittest.TestCase):
    def test_default_pack_keeps_existing_catalog_root_and_data(self):
        pack = bundled_default_pack()
        self.assertEqual(pack.id, "default")
        self.assertEqual(pack.catalog_root, DATA_ROOT)
        expected = json.loads((DATA_ROOT / "world_text.json").read_text(encoding="utf-8"))
        self.assertEqual(load_catalog("world_text.json", WORLD_TEXT_SECTIONS), expected)
        self.assertEqual(
            [(slot.id, slot.engine_id) for slot in region_contract()],
            [
                ("region.family_1", "hearthford"),
                ("region.family_2", "greywash"),
                ("region.family_3", "greenwold"),
                ("region.family_4", "whitecairn"),
                ("region.family_5", "dunmire"),
                ("region.family_6", "rillscar"),
                ("region.family_7", "marlbank"),
                ("region.family_8", "frostmere"),
            ],
        )
        self.assertEqual(
            [(slot.id, slot.engine_id, slot.role_id) for slot in character_contract()],
            [
                ("npc.ship_bartender", "bartender-sena", "bartender"),
                ("npc.ship_merchant", "merchant-veyra", "merchant"),
                ("npc.hearthford_second_contact", "hearthford-contact-2", None),
            ],
        )
        self.assertEqual(
            [(slot.id, slot.engine_id) for slot in role_contract()],
            [
                ("role.household_bargemaster", "bargemaster"),
                ("role.household_pilot", "pilot"),
                ("role.household_factor", "factor"),
                ("role.household_carpenter", "carpenter"),
                ("role.household_guard", "guard"),
                ("role.household_healer", "healer"),
                ("role.ship_bartender", "bartender"),
                ("role.ship_merchant", "merchant"),
            ],
        )
        slots = item_contract()
        self.assertEqual(len(slots), 154)
        self.assertEqual(len(ui_contract()), 79)
        self.assertEqual(
            [(slot.id, slot.engine_id) for slot in slots if slot.engine_id in {
                "spear", "marsh waders", "willow dressing", "rain cape",
            }],
            [
                ("item.equipment_002", "spear"),
                ("item.equipment_044", "marsh waders"),
                ("item.goods_014", "willow dressing"),
                ("item.goods_050", "rain cape"),
            ],
        )

    def test_item_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "items.json"
            cases = {
                "missing required item": lambda value: value["items"].pop("item.goods_050"),
                "unknown item": lambda value: value["items"].update({"item.goods_999": value["items"].pop("item.goods_050")} ),
                "missing display field": lambda value: value["items"]["item.equipment_002"].pop("display_name"),
                "wrong display field type": lambda value: value["items"]["item.equipment_002"].update({"display_name": 1}),
                "empty display field": lambda value: value["items"]["item.equipment_002"].update({"display_name": ""}),
                "unknown mechanical field": lambda value: value["items"]["item.equipment_002"].update({"engine_id": "renamed-spear"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads((DEFAULT_PACK_ROOT / "items.json").read_text(encoding="utf-8"))
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*items\.json"):
                        load_content_pack(root)

            with self.subTest("duplicate semantic item"):
                content = (DEFAULT_PACK_ROOT / "items.json").read_text(encoding="utf-8")
                source.write_text(content.replace('"item.goods_050"', '"item.goods_049"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*item\.goods_049"):
                    load_content_pack(root)

            with self.subTest("malformed JSON"):
                source.write_text("{", encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*items\.json"):
                    load_content_pack(root)

            with self.subTest("missing presentation file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*items\.json"):
                    load_content_pack(root)

    def test_ui_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "ui_text.json"
            cases = {
                "missing key": lambda value: value["text"].pop("ui.title.game"),
                "unknown key": lambda value: value["text"].update({"ui.extra": "extra"}),
                "wrong type": lambda value: value["text"].update({"ui.title.game": 1}),
                "empty text": lambda value: value["text"].update({"ui.title.game": ""}),
                "malformed template": lambda value: value["text"].update({"ui.start.save_path": "Save {path"}),
                "unknown placeholder": lambda value: value["text"].update({"ui.start.save_path": "Save: {other}"}),
                "missing placeholder": lambda value: value["text"].update({"ui.start.save_path": "Save"}),
                "too wide label": lambda value: value["text"].update({"ui.notice.warning.label": "x" * 17}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads((DEFAULT_PACK_ROOT / "ui_text.json").read_text(encoding="utf-8"))
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ui_text\.json"):
                        load_content_pack(root)
            with self.subTest("duplicate key"):
                content = (DEFAULT_PACK_ROOT / "ui_text.json").read_text(encoding="utf-8")
                source.write_text(content.replace('"ui.title.subtitle"', '"ui.title.game"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ui_text\.json"):
                    load_content_pack(root)
            with self.subTest("malformed JSON"):
                source.write_text("{", encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ui_text\.json"):
                    load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ui_text\.json"):
                    load_content_pack(root)

    def test_quest_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "quests.json"
            cases = {
                "missing quest": lambda value: value["quests"].pop("quest.regional.hearthford"),
                "unknown quest": lambda value: value["quests"].update({"quest.regional.extra": {"title": "x", "lead": "y"}}),
                "missing lead": lambda value: value["quests"]["quest.regional.hearthford"].pop("lead"),
                "wrong title type": lambda value: value["quests"]["quest.regional.hearthford"].update({"title": 1}),
                "empty evidence": lambda value: value["quests"]["quest.evidence.banks"].update({"evidence_name": ""}),
                "mechanical field": lambda value: value["quests"]["quest.arc.banks"].update({"engine_id": "other"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads((DEFAULT_PACK_ROOT / "quests.json").read_text(encoding="utf-8"))
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*quests\.json"):
                        load_content_pack(root)
            with self.subTest("duplicate semantic slot"):
                content = (DEFAULT_PACK_ROOT / "quests.json").read_text(encoding="utf-8")
                source.write_text(content.replace('"quest.arc.banks"', '"quest.arc.marks"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*quest\.arc\.marks"):
                    load_content_pack(root)
            with self.subTest("malformed JSON"):
                source.write_text("{", encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*quests\.json"):
                    load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*quests\.json"):
                    load_content_pack(root)

    def test_default_item_presentation_matches_legacy_base_catalog_text(self):
        from jomon.content import SUPPORTS
        from jomon.inventory import item_spec

        goods = json.loads((DATA_ROOT / "goods.json").read_text(encoding="utf-8"))
        equipment = json.loads((DATA_ROOT / "equipment.json").read_text(encoding="utf-8"))
        for engine_id, row in equipment["item_specs"].items():
            with self.subTest(engine_id=engine_id):
                self.assertEqual((item_spec(engine_id).name, item_spec(engine_id).description), (row["name"], row["description"]))
        for engine_id, row in goods["SUPPORTS"].items():
            self.assertEqual(SUPPORTS[engine_id], tuple(row))
        for engine_id, row in goods["DISCOVERIES"].items():
            self.assertEqual((item_spec(f"consumable:{engine_id}").name, item_spec(f"consumable:{engine_id}").description), (engine_id.title(), row[1]))
        for engine_id, description in goods["RELICS"].items():
            self.assertEqual((item_spec(f"relic:{engine_id}").name, item_spec(f"relic:{engine_id}").description), (engine_id.title(), description))
        for engine_id, row in goods["PASSIVES"].items():
            self.assertEqual((item_spec(f"passive:{engine_id}").name, item_spec(f"passive:{engine_id}").description), (engine_id.title(), row[1]))
        for engine_id, row in goods["COMMODITIES"].items():
            logistics = goods["COMMODITY_LOGISTICS"][engine_id]
            description = (
                f"Physical {engine_id} cargo from {row['source']}, used for {row['use']}. "
                f"Handling: {logistics['handling']}. Failure: {logistics['failure']}. "
                f"Material behavior: {logistics['environment']}. Contract use: {logistics['quest_use']}. "
                f"Equipment use: {logistics['equipment_use']}. Buyers: {', '.join(logistics['buyers'])}."
            )
            self.assertEqual((item_spec(f"commodity:{engine_id}").name, item_spec(f"commodity:{engine_id}").description), (engine_id.title(), description))

    def test_alternate_pack_changes_ui_text_and_preserves_legacy_notice_kinds(self):
        default = ui_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = ui_presentation_snapshot(environment)
        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["title"], "J O M O N")
        self.assertEqual(alternate["title"], "F I X T U R E")
        self.assertEqual(alternate["inventory"], "FIXTURE PACK")
        self.assertIn("Fixture help", alternate["help"])
        self.assertIn("HEARSAY: old save claim", " ".join(alternate["notices"]))
        self.assertIn("CAUTION: old save risk", " ".join(alternate["notices"]))
        self.assertEqual(default["kinds"], alternate["kinds"])
        self.assertEqual(default["roles"], alternate["roles"])
        self.assertEqual(default["mechanics"], alternate["mechanics"])

    def test_alternate_pack_changes_quest_presentation_not_quest_keys(self):
        default = quest_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = quest_presentation_snapshot(environment)
        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["title"], "The Mill Race Compact")
        self.assertEqual(alternate["title"], "Fixture Water Claim")
        self.assertIn("Fixture witness", alternate["lead"])
        self.assertEqual(alternate["arc"], "Fixture Bank Accord")
        self.assertEqual(alternate["evidence"], ["fixture bank record", "fixture bank record"])
        self.assertEqual(default["engine"], alternate["engine"])
        self.assertEqual(default["seed"], alternate["seed"])
        self.assertEqual([(row[0], row[2:]) for row in default["choices"]], [(row[0], row[2:]) for row in alternate["choices"]])
        self.assertEqual(alternate["choices"][0][1], "Make the fixture public settlement")
        self.assertEqual([(row[0], row[2:]) for row in default["bank_options"]], [(row[0], row[2:]) for row in alternate["bank_options"]])
        self.assertEqual(alternate["bank_options"][0][1], "Publish the fixture bank record")
        self.assertTrue(alternate["bank_result"][0][0])
        self.assertIn("Fixture Bank Accord", alternate["bank_result"][0][1])
        self.assertEqual(default["bank_result"][1:], alternate["bank_result"][1:])
        self.assertEqual(alternate["overlay"], "FIXTURE WATER CLAIM")

    def test_alternate_pack_changes_secondary_service_presentation(self):
        with tempfile.TemporaryDirectory() as directory:
            pack = load_content_pack(alternate_pack(Path(directory) / "fixture"))
        cache = pack.quest_service_presentation("quest.service.cache_mark")
        training = pack.quest_service_presentation("quest.service.practical_instruction")
        treatment = pack.quest_service_presentation("quest.service.treatment")
        self.assertEqual(cache.engine_id, "c")
        self.assertEqual(cache.label, "Fixture cache service")
        self.assertIn(("completed", "Fixture tutor {contact} grants {technique}: {effect}."), training.results)
        self.assertIn(("completed", "Fixture healer {contact} treats the {location} injury; the care takes time and leaves an obligation."), treatment.results)

    def test_alternate_pack_changes_history_presentation_with_stable_templates(self):
        with tempfile.TemporaryDirectory() as directory:
            pack = load_content_pack(alternate_pack(Path(directory) / "fixture"))
        self.assertEqual(
            pack.history_presentation("history.event.crisis.account").text,
            "Fixture witness {witness} records {crisis} at the {landmark}.",
        )
        self.assertEqual(
            pack.history_presentation("history.network_service.shelter.label").text,
            "Open the fixture shelter route",
        )

    def test_alternate_pack_changes_aftermath_contract_titles(self):
        with tempfile.TemporaryDirectory() as directory:
            pack = load_content_pack(alternate_pack(Path(directory) / "fixture"))
        self.assertEqual(
            pack.aftermath_presentation("aftermath.contract.hearthford.supply").title,
            "Fixture Flood Marks",
        )
        self.assertEqual(
            pack.aftermath_presentation("aftermath.contract.hearthford.supply").cause,
            "{dependency} stock is {stock} after {aftermath_title}; the next scheduled shift consumes a real lot at {site}",
        )

    def test_default_quest_presentation_matches_existing_catalog_copy(self):
        from jomon.quest_presentation import regional_choice_presentation, regional_quest_lead, regional_quest_title

        catalog = json.loads((DATA_ROOT / "quests.json").read_text(encoding="utf-8"))
        for region_id, row in catalog["quests"].items():
            self.assertEqual(regional_quest_title(region_id), row["title"])
            self.assertEqual(regional_quest_lead(region_id), row["lead"])
            for choice, label, _semantic, _available, requirement in row["final"]:
                self.assertEqual(regional_choice_presentation(region_id, choice), (label, requirement))

    def test_ui_formatter_accepts_only_the_engine_placeholder_contract(self):
        from jomon.ui_presentation import ui_format

        self.assertEqual(ui_format("ui.start.save_path", path="save.json"), "Save: save.json")
        with self.assertRaises(ValueError):
            ui_format("ui.start.save_path", other="save.json")

    def test_invalid_pack_manifests_and_missing_catalogs_name_the_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.subTest("missing manifest"):
                with self.assertRaisesRegex(ContentPackError, r"manifest.*" + str(root)):
                    load_content_pack(root)

            with self.subTest("invalid JSON"):
                write_manifest(root, "{")
                with self.assertRaisesRegex(ContentPackError, r"invalid content-pack manifest"):
                    load_content_pack(root)

            with self.subTest("missing ID"):
                write_manifest(root, '{"display_name":"Broken", "format_version":1}')
                with self.assertRaisesRegex(ContentPackError, r"missing id"):
                    load_content_pack(root)

            with self.subTest("duplicate manifest key"):
                write_manifest(root, '{"id":"one", "id":"two", "display_name":"Broken", "format_version":1}')
                with self.assertRaisesRegex(ContentPackError, r"duplicate catalog key"):
                    load_content_pack(root)

            with self.subTest("missing catalog"):
                write_manifest(root, '{"id":"broken", "display_name":"Broken", "format_version":1}')
                (root / "data").mkdir()
                with self.assertRaisesRegex(ContentPackError, r"broken.*missing required catalog.*world_text\.json"):
                    load_content_pack(root)

    def test_regional_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "regions.json"

            cases = {
                "missing required slot": lambda value: value["regions"].pop("region.family_8"),
                "unknown slot": lambda value: value["regions"].update({"region.family_9": value["regions"].pop("region.family_8")}),
                "missing display field": lambda value: value["regions"]["region.family_1"].pop("display_name"),
                "wrong display field type": lambda value: value["regions"]["region.family_1"].update({"display_name": 1}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads((DEFAULT_PACK_ROOT / "regions.json").read_text(encoding="utf-8"))
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*regions\.json"):
                        load_content_pack(root)

            with self.subTest("duplicate semantic slot"):
                content = (DEFAULT_PACK_ROOT / "regions.json").read_text(encoding="utf-8")
                source.write_text(
                    content.replace('"region.family_2"', '"region.family_1"', 1), encoding="utf-8"
                )
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*region\.family_1"):
                    load_content_pack(root)

            with self.subTest("malformed JSON"):
                source.write_text("{", encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*regions\.json"):
                    load_content_pack(root)

            with self.subTest("missing presentation file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*regions\.json"):
                    load_content_pack(root)

    def test_character_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "characters.json"
            cases = {
                "missing required character": lambda value: value["characters"].pop("npc.ship_merchant"),
                "unknown character": lambda value: value["characters"].update({"npc.ship_extra": value["characters"].pop("npc.ship_merchant")}),
                "missing display field": lambda value: value["characters"]["npc.ship_bartender"].pop("display_name"),
                "missing build tendency": lambda value: value["characters"]["npc.ship_bartender"].pop("build_tendency"),
                "wrong display field type": lambda value: value["characters"]["npc.ship_bartender"].update({"display_name": 1}),
                "empty display field": lambda value: value["characters"]["npc.ship_bartender"].update({"display_name": ""}),
                "unknown field": lambda value: value["characters"]["npc.ship_bartender"].update({"biography": "extra"}),
                "missing contact role label": lambda value: value["characters"]["npc.hearthford_second_contact"].pop("role_label"),
                "unknown role": lambda value: value["roles"].update({"role.ship_extra": value["roles"].pop("role.ship_bartender")}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads((DEFAULT_PACK_ROOT / "characters.json").read_text(encoding="utf-8"))
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*characters\.json"):
                        load_content_pack(root)

            with self.subTest("duplicate semantic character"):
                content = (DEFAULT_PACK_ROOT / "characters.json").read_text(encoding="utf-8")
                source.write_text(content.replace('"npc.ship_merchant"', '"npc.ship_bartender"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*npc\.ship_bartender"):
                    load_content_pack(root)

            with self.subTest("malformed JSON"):
                source.write_text("{", encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*characters\.json"):
                    load_content_pack(root)

            with self.subTest("missing presentation file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*characters\.json"):
                    load_content_pack(root)

    def test_environment_selects_complete_alternate_pack_before_main_import(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            self.assertEqual(load_content_pack(root).catalog_root, root / "data")

            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            result = subprocess.run(
                [
                    sys.executable,
                    "-c",
                    "import jomon.main; from jomon.catalog import selected_content_pack; "
                    "print(selected_content_pack().id); print(jomon.main.SEED_WORDS[0])",
                ],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.splitlines(), ["fixture-alternate", "fixture-reed"])

        # The subprocess owns selection state, so the fixture cannot leak into
        # the test process or another test module.
        self.assertNotEqual(os.environ.get("JOMON_CONTENT_PACK"), str(root))

    def test_alternate_pack_changes_regional_presentation_not_generation_or_ids(self):
        default = world_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = world_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["region"][0], alternate["region"][0])
        self.assertEqual(alternate["region"], ["hearthford", "Fixture Hearth"])
        self.assertEqual(alternate["route"][:2], ["hearthford", "Fixture Ford"])
        self.assertIn("fixture river presentation", alternate["route"][2])
        self.assertIn("FIXTURE FORD", " ".join(alternate["route_detail"]))
        self.assertIn("Fixture Hearth", " ".join(alternate["status"]))
        for field in ("signature", "levels", "landmarks", "edges"):
            self.assertEqual(default[field], alternate[field], field)
        self.assertEqual(default["region"][1], "Hearthford")
        self.assertEqual(default["route"][:2], ["hearthford", "Hearthford"])

    def test_alternate_pack_changes_static_character_presentation_not_services(self):
        default = character_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = character_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["bartender"][:1], alternate["bartender"][:1])
        self.assertEqual(default["merchant"][:1], alternate["merchant"][:1])
        self.assertEqual(default["bartender"][2:5], alternate["bartender"][2:5])
        self.assertEqual(default["merchant"][3:5], alternate["merchant"][3:5])
        self.assertEqual(default["second_contact"][0], alternate["second_contact"][0])
        self.assertEqual(default["second_contact"][3:], alternate["second_contact"][3:])
        self.assertEqual(alternate["bartender"][1], "Fixture Host")
        self.assertEqual(alternate["merchant"][1:3], ["Fixture Trader", "merchant"])
        self.assertEqual(alternate["second_contact"][1:3], ["Fixture Miller", "fixture mill speaker"])
        self.assertIn("FIXTURE HOST", " ".join(alternate["bartender_overlay"][0:1]))
        self.assertIn("Fixture Host", " ".join(alternate["bartender_overlay"][1]))
        self.assertIn("FIXTURE TRADER", " ".join(alternate["merchant_overlay"][0:1]))
        self.assertIn("fixture deck trader", " ".join(alternate["merchant_overlay"][1]))
        self.assertEqual(alternate["bartender"][7], "fixture hospitality")
        self.assertEqual(alternate["merchant"][7], "fixture exchange")
        self.assertIn("fixture navigator", " ".join(alternate["status"]))
        for field in ("household", "bartender_schedule", "merchant_schedule", "stock", "signature"):
            self.assertEqual(default[field], alternate[field], field)
        self.assertEqual(default["bartender"][1], "Sena Quill")
        self.assertEqual(default["merchant"][1:3], ["Veyra Bale", "merchant"])
        self.assertIn("itinerant deck factor", " ".join(default["merchant_overlay"][1]))
        self.assertEqual(default["second_contact"][1:3], ["Tomas Reed", "millwright speaker"])

    def test_alternate_pack_changes_base_item_presentation_not_identity_or_rules(self):
        default = item_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = item_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["names"], ["Ash spear", "Oiled marsh waders", "Willow Dressing", "Rain Cape"])
        self.assertEqual(alternate["names"], ["Fixture Spear", "Fixture Waders", "Fixture Dressing", "Fixture Rain Cape"])
        self.assertEqual(default["kinds"], alternate["kinds"])
        self.assertEqual(alternate["kinds"], ["spear", "passive:rain cape", "consumable:willow dressing"])
        self.assertEqual(default["initial_items"], alternate["initial_items"])
        self.assertEqual(default["initial_stock"], alternate["initial_stock"])
        self.assertEqual(default["equip"], alternate["equip"])
        self.assertEqual(default["purchase"], alternate["purchase"])
        self.assertEqual(default["weather_steps"], alternate["weather_steps"])
        self.assertEqual(alternate["weather_steps"], [1, 2])
        self.assertEqual(default["saved_kinds"], alternate["saved_kinds"])
        self.assertEqual(alternate["saved_kinds"], ["consumable:willow dressing", "passive:rain cape", "spear"])
        self.assertIn("Fixture Dressing", " ".join(alternate["merchant"]))
        self.assertIn("Fixture Spear", " ".join(alternate["inventory"]))
        self.assertIn("Fixture Dressing", " ".join(alternate["inventory"]))
        self.assertIn("Fixture Rain Cape", alternate["names"])
        self.assertIn("Ash spear", " ".join(default["inventory"]))

    def test_old_item_save_keeps_kind_and_renders_through_selected_pack(self):
        environment = dict(os.environ)
        environment.pop("JOMON_CONTENT_PACK", None)
        generated = subprocess.run(
            [sys.executable, "-c", "import json; from jomon.state import create_world; print(json.dumps(create_world('legacy item save').to_dict()))"],
            cwd=ROOT,
            env=environment,
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertEqual(generated.returncode, 0, generated.stderr)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            save = Path(directory) / "legacy.json"
            save.write_text(generated.stdout, encoding="utf-8")
            environment["JOMON_CONTENT_PACK"] = str(root)
            loaded = subprocess.run(
                [
                    sys.executable,
                    "-c",
                    "import json, sys; from jomon.inventory import item_spec; from jomon.state import game_state_from_dict; "
                    "state = game_state_from_dict(json.load(open(sys.argv[1], encoding='utf-8'))); "
                    "item = next(item for item in state.items if item.kind == 'spear'); print(json.dumps([item.kind, item_spec(item.kind).name]))",
                    str(save),
                ],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertEqual(loaded.returncode, 0, loaded.stderr)
        self.assertEqual(json.loads(loaded.stdout), ["spear", "Fixture Spear"])

    def test_alternate_pack_loads_legacy_saved_region_names_without_rewriting_them(self):
        default_environment = dict(os.environ)
        default_environment.pop("JOMON_CONTENT_PACK", None)
        generated = subprocess.run(
            [sys.executable, "-c", "import json; from jomon.state import create_world; print(json.dumps(create_world('legacy region save').to_dict()))"],
            cwd=ROOT,
            env=default_environment,
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertEqual(generated.returncode, 0, generated.stderr)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            save = Path(directory) / "legacy.json"
            legacy = json.loads(generated.stdout)
            legacy["merchant"]["role"] = "itinerant deck factor"
            save.write_text(json.dumps(legacy), encoding="utf-8")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            loaded = subprocess.run(
                [
                    sys.executable,
                    "-c",
                    "import json, sys; from jomon.state import game_state_from_dict; "
                    "state = game_state_from_dict(json.load(open(sys.argv[1], encoding='utf-8'))); "
                    "print(json.dumps([state.region.name, state.route_nodes['hearthford'].name, "
                    "state.bartender.name, state.merchant.name, state.merchant.role]))",
                    str(save),
                ],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertEqual(loaded.returncode, 0, loaded.stderr)
        self.assertEqual(
            json.loads(loaded.stdout),
            ["Hearthford", "Hearthford", "Sena Quill", "Veyra Bale", "itinerant deck factor"],
        )

    def test_selected_pack_reports_its_catalog_schema_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            (root / "data" / "world_text.json").write_text("{}\n", encoding="utf-8")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            result = subprocess.run(
                [sys.executable, "-c", "import jomon.main"],
                cwd=ROOT,
                env=environment,
                text=True,
                capture_output=True,
                check=False,
            )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("invalid world_text.json in content pack 'fixture-alternate'", result.stderr)
        self.assertIn("must contain exactly", result.stderr)


if __name__ == "__main__":
    unittest.main()
