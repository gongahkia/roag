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
    shutil.copy(DEFAULT_PACK_ROOT / "worklines.json", root / "worklines.json")
    shutil.copy(DEFAULT_PACK_ROOT / "interference_text.json", root / "interference_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "legendary_text.json", root / "legendary_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "topology_text.json", root / "topology_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "action_text.json", root / "action_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "vessel_text.json", root / "vessel_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "travel_text.json", root / "travel_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "ship_crisis_text.json", root / "ship_crisis_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "vehicle_text.json", root / "vehicle_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "chemistry_text.json", root / "chemistry_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "production_text.json", root / "production_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "magic_text.json", root / "magic_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "progression_text.json", root / "progression_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "equipment_text.json", root / "equipment_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "preparation_text.json", root / "preparation_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "material_text.json", root / "material_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "sanctum_text.json", root / "sanctum_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "situation_text.json", root / "situation_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "circuit_text.json", root / "circuit_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "ecology_text.json", root / "ecology_text.json")
    shutil.copy(DEFAULT_PACK_ROOT / "tavern_games.json", root / "tavern_games.json")
    shutil.copytree(DEFAULT_PACK_ROOT / "dullest_dungeon", root / "dullest_dungeon")
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
    source = root / "dullest_dungeon" / "text.json"
    dungeon_text = json.loads(source.read_text(encoding="utf-8"))
    dungeon_text["cards"]["bone_saw"]["name"] = "Fixture Paper Saw"
    dungeon_text["cards"]["bone_saw"]["description"] = "Fixture paperwork cuts through the same ranks."
    dungeon_text["heroes"]["warden"]["name"] = "Fixture Supervisor"
    dungeon_text["enemies"]["rad_acolyte"]["name"] = "Fixture Rival"
    dungeon_text["ui"]["game_title"] = "FIXTURE TABLE"
    dungeon_text["narration"]["opening"] = "Fixture Bureau opens the unchanged expedition."
    dungeon_text["narration"]["action"] = "{actor} files a fixture action: {action}."
    dungeon_text["ui"]["pending_camp_recover"] = "Fixture recovery keeps the same HP and stress values"
    source.write_text(json.dumps(dungeon_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "dullest_dungeon" / "visuals.json"
    dungeon_visuals = json.loads(source.read_text(encoding="utf-8"))
    dungeon_visuals["office_sprites"]["warden"][0] = "[FIXED]"
    source.write_text(json.dumps(dungeon_visuals, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")

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
    source = root / "tavern_games.json"
    tavern_games = json.loads(source.read_text(encoding="utf-8"))
    tavern_games["text"]["draw.opening"] = "Fixture cards arrive under the same mechanical draw."
    tavern_games["text"]["dice.opening"] = "Fixture bones keep the same three rounds."
    tavern_games["text"]["draw.ui.title.active"] = "FIXTURE DRAW / TABLE"
    tavern_games["rank_labels"][0] = "Fixture high hand"
    tavern_games["cards"]["edge"] = "#-------#"
    tavern_games["dice"]["1"][0] = "#-------#"
    source.write_text(json.dumps(tavern_games, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
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
    aftermath["actions"]["aftermath.action.accept"]["label"] = "Fixture accept copy"
    aftermath["actions"]["aftermath.action.deliver"]["requirements"]["missing_supply"] = "Fixture needs {commodity}."
    aftermath["actions"]["aftermath.action.work"]["requirements"]["field_site"] = "Fixture work at {site}."
    aftermath["actions"]["aftermath.action.settle"]["requirements"]["missing_copy"] = "Fixture replacement copy required."
    aftermath["results"]["accepted"] = "Fixture accepts {title}: {copy_state} Supply {commodity} at {site}."
    aftermath["results"]["delivered"] = "Fixture delivery of {commodity} awaits settlement."
    aftermath["results"]["settled_supply"] = "Fixture settlement restores the same stock and demand."
    aftermath["results"]["ledger_cause"] = "FIXTURE CAUSE — {cause}."
    aftermath["results"]["abandoned"] = "Fixture witness records the same failed promise."
    source.write_text(json.dumps(aftermath, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "worklines.json"
    worklines = json.loads(source.read_text(encoding="utf-8"))
    worklines["text"].update({
        "workline.hearthford.title": "Fixture Water Works",
        "workline.hearthford.branch.h": "Fixture shore the watch approach",
        "workline.line.survey_help": "Fixture survey guidance keeps the same optional evidence credit.",
        "workline.requirement.hearthford.timber": "fixture timber requirement for the same framing.",
        "workline.result.hearthford.h": "Fixture timber produces the same firm shared route.",
        "workline.settle.public": "Fixture commons preserve the same trust and route effects.",
    })
    source.write_text(json.dumps(worklines, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "interference_text.json"
    interference = json.loads(source.read_text(encoding="utf-8"))
    interference["text"].update({
        "interference.measured-grain-release.title": "Fixture Grain Passage",
        "interference.measured-grain-release.cause": "Fixture settled work releases the same seed shipment",
        "interference.measured-grain-release.origin_change": "fixture mill lot leaves under the same measure",
        "interference.measured-grain-release.destination_change": "fixture seed demand eases at the same unloading stair",
        "interference.notice": "FIXTURE INTERFERENCE — {title}: work from {origin} reaches this arrival.",
        "interference.record": "FIXTURE RECORD: {title}; {cause}; {origin_change}; {destination_change}.",
        "interference.ledger.arrival": "FIXTURE ARRIVAL — {title}: {destination_change}.",
    })
    source.write_text(json.dumps(interference, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "legendary_text.json"
    legendary = json.loads(source.read_text(encoding="utf-8"))
    legendary["text"].update({
        "legendary.object.noun.0": "Fixture Measure",
        "legendary.object.name": "{maker}'s Fixture {noun}",
        "legendary.object.provenance": "Fixture-made by {maker} for {institution} after {crisis}; {repair} Claim: {dispute}.",
        "legendary.object.clue": "Fixture trail: {account} ends at {cache_name} ({cache_id}).",
        "legendary.arc.common-work-rivet.display_name": "Fixture Rivet",
        "legendary.arc.common-work-rivet.description": "Fixture repair still applies the same fatigue and support mechanics.",
        "legendary.arc.common-work-rivet.result": "Fixture rivet repairs {equipment} items and seats {supports} supports with the same fatigue.",
    })
    source.write_text(json.dumps(legendary, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "topology_text.json"
    topology = json.loads(source.read_text(encoding="utf-8"))
    topology["text"].update({
        "topology.hearthford.zone.millworks": "Fixture Waterworks",
        "topology.hearthford.landmark.mill": "fixture engine house",
        "topology.hearthford.link.mill_ladder": "fixture service climb",
        "topology.hearthford.container.cellar": "Fixture submerged strongbox",
        "topology.greywash.zone.salt_pans": "Fixture brine terraces",
        "topology.greywash.container.quay": "Fixture tide coffer",
        "topology.greywash.contact.1.name": "Fixture Ebbwarden",
        "topology.greenwold.zone.resin_yard": "Fixture pitch enclosure",
        "topology.greenwold.link.canopy_ladder": "fixture bough ascent",
        "topology.greenwold.contact.1.role": "fixture charcoal steward",
        "topology.whitecairn.zone.quarry_face": "Fixture stone shelf",
        "topology.whitecairn.container.tower": "Fixture bell cache",
        "topology.whitecairn.landmark.bell_tower": "fixture signal tower",
        "topology.dunmire.zone.old_scar": "Fixture peat scar",
        "topology.dunmire.container.quay": "Fixture fen coffer",
        "topology.rillscar.landmark.works": "fixture span works",
        "topology.rillscar.link.work_stair": "fixture cliff ascent",
        "topology.marlbank.zone.industrial_works": "Fixture firing court",
        "topology.marlbank.contact.1.role": "fixture terrace steward",
        "topology.frostmere.zone.far_shore": "Fixture ice channel",
        "topology.frostmere.condition": "Fixture gravel and water preserve the same estuary geometry.",
        "topology.hearthford.landform.0.name": "Fixture reed shelf",
        "topology.hearthford.landform.upper": "Fixture upper watch",
        "topology.hearthford.landform.traveller.name": "Fixture surveyor",
        "topology.hearthford.discovery.reed-silt.name": "Fixture survey cache",
        "topology.hearthford.discovery.reed-silt.clue": "fixture clue beside the same silt mark",
        "topology.landform.link.upper": "Fixture link: {structure}",
        "topology.landform.traveller.arrival": "FIXTURE field arrival: {traveller} at {x},{y}.",
        "topology.discovery.reveal": "FIXTURE discovery {cache}: {clue}.",
    })
    source.write_text(json.dumps(topology, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "vessel_text.json"
    vessel_text = json.loads(source.read_text(encoding="utf-8"))
    vessel_text["text"].update({
        "vessel.drink.hearth-ale.name": "Fixture Hearth Measure",
        "vessel.drink.hearth-ale.benefit": "fixture guard effect",
        "vessel.schedule.serving": "fixture service watch",
        "vessel.bar.choice.browse": "Browse fixture measures",
        "vessel.drink.served": "Fixture drink {drink}: {benefit}; drawback: {drawback}.",
    })
    source.write_text(json.dumps(vessel_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "travel_text.json"
    travel_text = json.loads(source.read_text(encoding="utf-8"))
    travel_text["text"].update({
        "travel.frame.depart": "FIXTURE VESSEL leaves {origin}",
        "travel.destination.arrival": "FIXTURE ARRIVES {destination} after {duration} measures.",
        "travel.requirement.inspection": "FIXTURE INSPECTION needs {threshold} trust or paper.",
        "travel.result.raiders.repel": "FIXTURE REACH repels the same cargo thieves.",
        "travel.finish.message": "FIXTURE {consequence} reaches {destination}.",
    })
    source.write_text(json.dumps(travel_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "ship_crisis_text.json"
    crisis_text = json.loads(source.read_text(encoding="utf-8"))
    crisis_text["text"].update({
        "crisis.raiders.title": "Fixture Rail Claim",
        "crisis.choice.raiders.repel": "Fixture repel the same reach threat",
        "crisis.begin.alarm.boarders": "FIXTURE DECK ALARM: {title}. The same clock advances; observe or withdraw.",
        "crisis.work.repair": "Fixture timber restores the same three integrity.",
        "crisis.finish.abandon": "FIXTURE withdrawal: {loss}; the same hull damage remains.",
        "intent.crisis.observe": "watches the fixture deck before committing",
    })
    source.write_text(json.dumps(crisis_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "vehicle_text.json"
    vehicle_text = json.loads(source.read_text(encoding="utf-8"))
    vehicle_text["text"].update({
        "vehicle.tug.name": "Fixture water tractor",
        "vehicle.tug.resource": "fixture reserve",
        "vehicle.board.tug.success": "Fixture boards the same water vehicle.",
        "vehicle.navigate.result": "Fixture {vehicle} crosses {travelled} mark{suffix}; {fuel}/{capacity} {resource} remains.",
        "vehicle.interior.controls": "Fixture controls retain the same fixture actions.",
    })
    source.write_text(json.dumps(vehicle_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "chemistry_text.json"
    chemistry_text = json.loads(source.read_text(encoding="utf-8"))
    chemistry_text["text"].update({
        "chemistry.reagent.tree_resin.name": "fixture resin",
        "chemistry.reaction.resin_mortar.name": "fixture mortar",
        "chemistry.reagent.healing_herb.name": "fixture herb",
        "chemistry.reaction.healing_draft.name": "fixture restorative",
        "chemistry.fill.result": "FIXTURE fill {measures} {reagent} into {flask}: {contents}.",
        "chemistry.fill.reaction_potential": " FIXTURE potential: {reactions}.",
        "chemistry.drink.result": "FIXTURE {courier} drinks {reactions} from {flask}.",
        "chemistry.pour.prediction": " FIXTURE reaction forecast: {reactions}.",
        "chemistry.overlay.flasks.title": "FIXTURE FLASKS",
    })
    source.write_text(json.dumps(chemistry_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "production_text.json"
    production_text = json.loads(source.read_text(encoding="utf-8"))
    production_text["text"].update({
        "production.recipe.field-dressing.name": "Fixture field remedy",
        "production.recipe.fabricate": "Fixture fabricate {item}",
        "production.make.result": "FIXTURE {courier} completes {recipe} at {station}.",
        "production.gather.result": "FIXTURE {courier} takes {reagent}; {stock} fixture lots remain.",
        "production.delegate.result": "FIXTURE {worker} accepts {recipe}.",
        "production.order.record": "FIXTURE day {day}: {recipe} by {worker} at {region} ({x},{y}).",
        "production.provenance.masterwork": "FIXTURE superior {work}",
        "production.overlay.catalog.title": "FIXTURE WORKING PLANS",
    })
    source.write_text(json.dumps(production_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "magic_text.json"
    magic_text = json.loads(source.read_text(encoding="utf-8"))
    magic_text["text"].update({
        "magic.spell.ember-spark.name": "Fixture coal spark",
        "magic.spell.ash-shot.name": "Fixture ash shot",
        "magic.spell.ember-spark.description": "fixture {effect} {power}; radius {radius}; {cost} mana, reach {reach}",
        "magic.cast.result": "FIXTURE {courier} works {spell} ({cost} mana): {detail}.",
        "magic.status.mana": "FIXTURE needs {cost} mana; {mana} remains",
        "magic.rest.berth.result": "FIXTURE {courier} restores mana to {mana}.",
        "intent.magic.push": "fixture wind displacement",
    })
    source.write_text(json.dumps(magic_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "progression_text.json"
    progression_text = json.loads(source.read_text(encoding="utf-8"))
    progression_text["text"].update({
        "progression.branch.blades.name": "Fixture Blades",
        "progression.node.edge-measure.name": "Fixture Edge Measure",
        "progression.practice.bank_water_cadence.name": "fixture water cadence",
        "progression.manoeuvre.braced-advance.name": "Fixture Braced Advance",
        "progression.skill.buy.learned": "FIXTURE {courier} learns {node}: {description}.",
        "progression.journal.write.result": "FIXTURE {courier} records {node} in {journal}.",
        "progression.skill.teach.result": "FIXTURE {teacher} demonstrates {node} to {recipient}.",
        "progression.choice.journal.write.requirement": "fixture lesson and paper",
        "progression.choice.manoeuvre.label": "Use fixture active manoeuvre",
        "progression.person.practice_effect": "FIXTURE practice {practice}: {description}",
    })
    source.write_text(json.dumps(progression_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "equipment_text.json"
    equipment_text = json.loads(source.read_text(encoding="utf-8"))
    equipment_text["text"].update({
        "equipment.weapon.work.forked_pike.name": "Fixture fork pike",
        "equipment.fitting.quiet_binding.name": "Fixture quiet binding",
        "equipment.strike.forked_pike": "fixture fork fixes {target}",
        "equipment.workshop.install.result": "FIXTURE workshop seats {fitting} on {item}; {cost} credit, two actions. {effect}",
        "equipment.overlay.station.title": "FIXTURE EQUIPMENT BAY",
    })
    source.write_text(json.dumps(equipment_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "preparation_text.json"
    preparation_text = json.loads(source.read_text(encoding="utf-8"))
    preparation_text["text"].update({
        "preparation.waterline.name": "Fixture Tide Chalk",
        "preparation.waterline.description": "Fixture waterline wording keeps the same layers.",
        "preparation.waterline.condition": "fixture nearby water",
        "preparation.apply.success": "FIXTURE {preparation}: {detail}.",
        "preparation.overlay.guidance": "FIXTURE field guidance keeps the same action clock.",
    })
    source.write_text(json.dumps(preparation_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "material_text.json"
    material_text = json.loads(source.read_text(encoding="utf-8"))
    material_text["text"].update({
        "material.name.timber": "fixture timber",
        "material.inspect.fact": "FIXTURE MATERIAL {x},{y} z{z}: {material}; coating {coating}.",
        "material.handle.result": "FIXTURE {verb} {material} at {coordinate}; unchanged one action.",
        "material.collapse.warning": "FIXTURE SUPPORT at {coordinate} warns on the unchanged clock.",
        "material.inspection.advice.fire": "FIXTURE ADVICE — {speaker}: unchanged fire simulation needs an unchanged response.",
    })
    source.write_text(json.dumps(material_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "sanctum_text.json"
    sanctum_text = json.loads(source.read_text(encoding="utf-8"))
    sanctum_text["text"].update({
        "sanctum.hearthford.name": "Fixture Silt Hall",
        "sanctum.hearthford.theme": "fixture flood account",
        "sanctum.hearthford.witness.name": "Fixture Mera",
        "sanctum.hearthford.boss.name": "Fixture Abbot",
        "sanctum.link.entry": "fixture threshold stair",
        "sanctum.cache.ward": "Fixture {sanctum} cache",
        "sanctum.choice.offering": "FIXTURE offering of {commodity} preserves the same account effect for {account}.",
        "sanctum.record.cleared": "FIXTURE {sanctum} cleared: four credits; {strategy}.",
    })
    source.write_text(json.dumps(sanctum_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "situation_text.json"
    situation_text = json.loads(source.read_text(encoding="utf-8"))
    situation_text["text"].update({
        "situation.hearthford:steady:reed-tally.title": "Fixture Reed Account",
        "situation.hearthford:steady:reed-tally.group.0": "fixture reed workers",
        "situation.hearthford:steady:reed-tally.duty": "fixture duty keeps the same physical work",
        "situation.hearthford:steady:reed-tally.choice.tool": "fixture brace the same tally bank",
        "situation.hearthford:steady:reed-tally.consequence": "fixture margin keeps the same consequence",
        "situation.notice.activation": "FIXTURE SITUATION — {title}: {duty}; {material}.",
        "situation.record.resolved": "FIXTURE {title} resolves by {outcome}; {consequence}.",
        "situation.report.unfiled": "FIXTURE report remains unfiled.",
    })
    source.write_text(json.dumps(situation_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "circuit_text.json"
    circuit_text = json.loads(source.read_text(encoding="utf-8"))
    circuit_text["text"].update({
        "circuit.part.trace.name": "Fixture filament",
        "circuit.part.trace.description": "Fixture insulation preserves the same signal rules.",
        "circuit.place.success": "FIXTURE fitted {part} at {x},{y},{z} ({layer}).",
        "circuit.diagnostic.pulse": "FIXTURE pulse {phase} -> {next_phase}; {inputs} live input(s); {links} links{remaining}.",
        "circuit.event.drained": "FIXTURE drained {amount} unchanged water measures",
        "circuit.terminal.heading": "FIXTURE CIRCUITS {layer} {x},{y},{z} {description}",
    })
    source.write_text(json.dumps(circuit_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "ecology_text.json"
    ecology_text = json.loads(source.read_text(encoding="utf-8"))
    ecology_text["text"].update({
        "ecology.actor.fen-lynx.name": "Fixture reed hunter",
        "ecology.goal.hunt_prey": "fixture hunt retains the same target",
        "ecology.reason.rival": "fixture {target} preserves the same {ecology} dispute",
        "ecology.result.rival_strike": "FIXTURE {actor} strikes {rival}; {health} remains.",
        "frontier.elite.fen-marshal.name": "Fixture flood marshal",
        "frontier.elite.fen-marshal.counterplay": "fixture height and the same spill control",
        "frontier.telegraph.surge": "FIXTURE prepares {mode} at {position}; the same control remains available",
        "frontier.result.surge": "FIXTURE water still crosses the same three paces.",
        "forecast.line.danger": "FIXTURE DANGER: {actor} — {action}.",
        "forecast.line.counter": "FIXTURE COUNTERS: {counter}.",
    })
    source.write_text(json.dumps(ecology_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
    source = root / "action_text.json"
    action_text = json.loads(source.read_text(encoding="utf-8"))
    action_text["text"].update({
        "intent.elite.floodgate.sluice_telegraph": "marks FIXTURE sluice {x},{y}",
        "combat.elite.floodgate.telegraph": "FIXTURE FLOODGATE {threat}: {intent}.",
        "combat.elite.floodgate.safe": "FIXTURE SURGE misses the same moved courier.",
        "intent.machinery.sweep_telegraph": "marks FIXTURE sweep {lane}",
        "combat.machinery.telegraph": "FIXTURE MACHINE {threat}: {lane}.",
        "intent.controller.net_telegraph": "casts FIXTURE net at {x},{y}",
        "combat.threat.net_miss": "FIXTURE NET {threat} misses the same repositioned courier.",
        "social.negotiate.success": "FIXTURE TERMS settle {count}; other groups stay independent.{drawback}",
        "social.negotiate.no_terms": "FIXTURE TERMS REQUIRE MATERIAL LEVERAGE.",
        "social.objective.refused": "FIXTURE {courier} declines {region}'s request.",
        "social.recruit.accepted": "FIXTURE {visitor} takes a berth.",
        "social.incident.mediate": "FIXTURE mediation seats {first} and {second}.",
        "action.setup.support.prepared": "FIXTURE PREPARED {support}.",
        "action.item.willow_dressing.used": "FIXTURE DRESSING restores {amount}{method}.",
        "action.environment.hearthford.control": "FIXTURE CONTROL is {control}.",
        "action.route.unavailable": "FIXTURE ROUTE SERVICE is unavailable.",
    })
    source.write_text(json.dumps(action_text, ensure_ascii=True, indent=2) + "\n", encoding="utf-8")
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


def workline_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.inventory import auto_place, create_item, sync_legacy_load; "
            "from jomon.regions import activate_region; from jomon.state import create_world; "
            "from jomon.worklines import carried_evidence, field_site, lines, options, resolve, survey_site; "
            "state = create_world('workline-pack-proof'); activate_region(state, 'hearthford'); state.location = 'region'; state.threats.clear(); state.world_time = 8; state.trade_credit = 12; "
            "contact = state.contacts['hearthford'][1]; state.position = state.actor_schedules[contact.id].position; opening = resolve(state, 'h'); opening_options = options(state); opening_lines = lines(state); state.position = survey_site(state); survey = resolve(state, 'e'); survey_lines = lines(state); "
            "item = create_item(state, 'commodity:timber', 'workline pack proof'); auto_place(state, item.id, 'pack', owner_id=state.active_courier_id); sync_legacy_load(state); state.position = field_site(state); work = resolve(state, 'f'); "
            "state.position = state.actor_schedules[contact.id].position; settlement = resolve(state, 'p'); quest = state.worklines['hearthford']; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'opening': opening.message, 'survey': survey.message, 'work': work.message, 'settlement': settlement.message, 'lines': lines(state), 'opening_lines': opening_lines, 'survey_lines': survey_lines, "
            "'opening_options': [(row[0], row[2], row[3]) for row in opening_options], "
            "'mechanics': [quest.stage, quest.status, quest.branch, state.region.changes.get('raised_watch_approach'), state.trade_credit, state.market['grain'].stock, state.items[-1].kind, state.items[-1].location, bool(carried_evidence(state))]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def interference_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.frontiers import ensure_frontier; from jomon.interference import INTERFERENCES, lines_for_region; "
            "from jomon.regions import activate_region; from jomon.situations import BY_REGION_BAND; from jomon.state import create_world; "
            "state = create_world('interference-pack-proof'); ensure_frontier(state, 'marlbank'); row = next(row for row in INTERFERENCES if row.id == 'measured-grain-release'); "
            "situation = BY_REGION_BAND[row.origin, 'steady']; state.regions[row.origin].changes['micro-site:resolved:' + situation.id] = True; "
            "source = state.regional_markets[row.origin][row.cargo]; destination = state.regional_markets[row.destination][row.cargo]; before = [source.stock, destination.stock, destination.demand]; activate_region(state, row.destination); "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'notice': state.messages[-1], 'chronicle': state.chronicle[-1], 'ledger': lines_for_region(state, row.destination), "
            "'mechanics': [row.id, row.kind, row.origin, row.destination, row.cargo, before, source.stock, destination.stock, destination.demand, state.vessel_changes['interference:' + row.id], state.institutions['work:' + row.destination].confidence, sorted(key for key in state.regions[row.origin].changes if key.startswith('interference-out:')), sorted(key for key in state.regions[row.destination].changes if key.startswith('interference-in:'))]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def legendary_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.arc_relics import use_arc_relic; from jomon.frontiers import FRONTIERS; from jomon.inventory import auto_place, create_item, item_spec, sync_legacy_load; "
            "from jomon.materials import fields; from jomon.regions import activate_region; from jomon.state import MaterialCell, Position, create_world; "
            "state = create_world('legendary-pack-proof'); [activate_region(state, region) for region in FRONTIERS]; "
            "legends = sorted((legend.id, legend.region_id, legend.base_kind, legend.institution_id, legend.historical_event_id, legend.tags, legend.range_bonus, legend.material_verbs) for legend in state.legendary_objects.values()); "
            "presented = sorted((legend.id, legend.name, legend.provenance, legend.clue) for legend in state.legendary_objects.values()); "
            "state.location = 'region'; state.position = Position(40, 24); item = create_item(state, 'relic:common-work rivet', 'legendary fixture'); auto_place(state, item.id, 'pack', owner_id=state.active_courier_id); state.relics['common-work rivet'] = 1; state.carried_relic = 'common-work rivet'; sync_legacy_load(state); state.carried_relic = 'common-work rivet'; "
            "worn = next(item for item in state.items if item.location == 'readied' and item.owner_id == state.active_courier_id); worn.condition = 50; fields(state)['41,24,0'] = MaterialCell(material='timber', support=1, collapse_due=10); used = use_arc_relic(state, 'common-work rivet'); "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, 'legends': legends, 'presented': presented, 'relic': [item_spec('relic:common-work rivet').name, item_spec('relic:common-work rivet').description, used[1]], 'mechanics': [used[0], worn.condition, fields(state)['41,24,0'].support, fields(state)['41,24,0'].collapse_due, state.relics, state.carried_relic, state.legendary_objects['legend:hearthford'].base_kind]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def topology_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.navigation import navigation_targets; from jomon.state import create_world; "
            "state=create_world('topology-pack-proof'); region=state.region; state.location='region'; state.position=region.landmarks['landing']; "
            "state.region.seen=[f'{point.x},{point.y},{point.z}' for point in region.landmarks.values()] + [f'{box.position.x},{box.position.y},{box.position.z}' for box in region.containers] + [f'{point.x},{point.y},{point.z}' for link in region.vertical_links for point in (link.first, link.second)]; "
            "targets=navigation_targets(state); "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'mechanics': [region.id, region.width, region.height, region.levels, sorted((key, point.x, point.y, point.z) for key, point in region.landmarks.items()), sorted((link.id, link.first.x, link.first.y, link.first.z, link.second.x, link.second.y, link.second.z) for link in region.vertical_links), sorted((box.id, box.position.x, box.position.y, box.position.z, box.reward, box.requirement, tuple(box.extra_rewards)) for box in region.containers), region.geography_signature], "
            "'presentation': [list(region.zones), [(link.id, link.name) for link in region.vertical_links], [(box.id, box.name) for box in region.containers], [(target.id, target.label) for target in targets if target.id.startswith(('landmark:mill', 'link:hearthford:mill_ladder', 'container:cellar'))]]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def regional_generator_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.navigation import navigation_targets; from jomon.regions import activate_region; from jomon.state import create_world; "
            "state=create_world('regional-generator-pack-proof'); mechanics={}; presentation={}; "
            "[(activate_region(state, region_id), setattr(state, 'location', 'region'), setattr(state, 'position', state.region.landmarks['landing']), setattr(state.region, 'seen', [f'{point.x},{point.y},{point.z}' for point in state.region.landmarks.values()] + [f'{box.position.x},{box.position.y},{box.position.z}' for box in state.region.containers] + [f'{point.x},{point.y},{point.z}' for link in state.region.vertical_links for point in (link.first, link.second)]), mechanics.update({region_id: [state.region.id, state.region.width, state.region.height, state.region.levels, sorted((key, point.x, point.y, point.z) for key, point in state.region.landmarks.items()), sorted((link.id, link.first.x, link.first.y, link.first.z, link.second.x, link.second.y, link.second.z) for link in state.region.vertical_links), sorted((box.id, box.position.x, box.position.y, box.position.z, box.reward, box.requirement, tuple(box.extra_rewards)) for box in state.region.containers), sorted((contact.id, contact.disposition, contact.interest, contact.position.x, contact.position.y, contact.position.z) for contact in state.contacts[region_id]), sorted((threat.id, threat.profile, threat.position.x, threat.position.y, threat.position.z, threat.health, threat.elite, threat.group) for threat in state.region_threats[region_id]), state.region.geography_signature]}), presentation.update({region_id: [[state.region.condition, state.region.work, state.region.pressure, state.region.objective_text, state.region.hazard, state.region.process_name], list(state.region.zones), [(link.id, link.name) for link in state.region.vertical_links if link.id.startswith(region_id + ':')], [(box.id, box.name) for box in state.region.containers], [(contact.id, contact.name, contact.role) for contact in state.contacts[region_id]], [(target.id, target.label) for target in navigation_targets(state) if target.id.startswith('landmark:')] ]})) for region_id in ('greywash', 'greenwold', 'whitecairn')]; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, 'mechanics': mechanics, 'presentation': presentation}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def frontier_generator_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.frontiers import ensure_frontier; from jomon.navigation import navigation_targets; from jomon.regions import activate_region; from jomon.state import create_world; "
            "state=create_world('frontier-generator-pack-proof'); mechanics={}; presentation={}; "
            "[(ensure_frontier(state, region_id), activate_region(state, region_id), setattr(state, 'location', 'region'), setattr(state, 'position', state.region.landmarks['landing']), setattr(state.region, 'seen', [f'{point.x},{point.y},{point.z}' for point in state.region.landmarks.values()] + [f'{box.position.x},{box.position.y},{box.position.z}' for box in state.region.containers] + [f'{point.x},{point.y},{point.z}' for link in state.region.vertical_links for point in (link.first, link.second)]), mechanics.update({region_id: [state.region.id, state.region.width, state.region.height, state.region.levels, sorted((key, point.x, point.y, point.z) for key, point in state.region.landmarks.items()), sorted((link.id, link.first.x, link.first.y, link.first.z, link.second.x, link.second.y, link.second.z) for link in state.region.vertical_links), sorted((box.id, box.position.x, box.position.y, box.position.z, box.reward, box.requirement, tuple(box.extra_rewards)) for box in state.region.containers), sorted((contact.id, contact.disposition, contact.interest, contact.position.x, contact.position.y, contact.position.z) for contact in state.contacts[region_id]), sorted((threat.id, threat.profile, threat.position.x, threat.position.y, threat.position.z, threat.health, threat.elite, threat.group) for threat in state.region_threats[region_id]), state.region.geography_signature]}), presentation.update({region_id: [[state.region.condition, state.region.work, state.region.pressure, state.region.objective_text, state.region.hazard, state.region.process_name], list(state.region.zones), [(link.id, link.name) for link in state.region.vertical_links if link.id.startswith(region_id + ':')], [(box.id, box.name) for box in state.region.containers], [(contact.id, contact.name, contact.role, contact.memories) for contact in state.contacts[region_id]], [(target.id, target.label) for target in navigation_targets(state) if target.id.startswith('landmark:')] ]})) for region_id in ('dunmire', 'rillscar', 'marlbank', 'frostmere')]; "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, 'mechanics': mechanics, 'presentation': presentation}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
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


def social_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.actions import choose_courier, decide_objective, intervene_socially, negotiate, recruit_person; from jomon.state import Position, SocialIncident, Threat, create_world; "
            "state=create_world('social-pack-proof'); courier=next(p for p in state.household if p.role == 'factor'); choose_courier(state,courier.id); visitor=next(p for p in state.visitors if state.visitor_status[p.id]=='visiting'); region,markers,_=__import__('jomon.people',fromlist=['RECRUIT_REQUIREMENTS']).RECRUIT_REQUIREMENTS[visitor.id]; state.regions[region].changes[markers[0]]=True; recruit=recruit_person(state,visitor.id); "
            "state.location='region'; state.position=state.region.landmarks['contact']; refused=decide_objective(state,'refuse'); state.objective_status='unoffered'; state.position=Position(40,25); threat=Threat('terms','toll runner','pursuer',Position(41,25),4,4,status='engaged'); state.threats=[threat]; negotiated=negotiate(state); "
            "state.location='jomon'; state.jomon_space='tavern'; first,second=state.household[:2]; state.pending_incident=SocialIncident('fixture-incident','argument',[first.id,second.id],'fixture cause','pending',0); mediated=intervene_socially(state,'mediate'); "
            "print(json.dumps({'pack': __import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id, 'mechanics': {'recruit':[visitor.id,state.visitor_status[visitor.id]], 'objective':state.objective_status, 'negotiation':[threat.id,threat.status,threat.intent_id,state.courier.speech], 'incident':[first.relationships.get(second.id,0),second.relationships.get(first.id,0),state.pending_incident]}, 'messages':[recruit.message,refused.message,negotiated.message,mediated.message], 'intent':threat.intent}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def remaining_action_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.actions import choose_courier, choose_support, use_gear, _control_interaction, use_route_stop; from jomon.state import Position, Threat, create_world; "
            "state=create_world('remaining-action-pack-proof'); courier=state.household[0]; choose_courier(state,courier.id); prepared=choose_support(state,'porter watch'); state.location='region'; state.position=Position(42,25); state.threats=[Threat('fixture','fixture threat','pursuer',Position(43,25),4,4,status='engaged')]; state.courier.injury='wounded foot'; state.consumables['willow dressing']=1; dressed=use_gear(state); control=_control_interaction(state); state.location='jomon'; state.route_current_node='jomon'; routes=use_route_stop(state,'sound'); "
            "print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id, 'messages':[prepared.message,dressed.message,control.message,routes.message], 'mechanics':{'support':state.support,'health':state.courier.health,'injury':state.courier.injury,'flood':state.flood_control,'routes':list(state.route_known),'consumables':dict(state.consumables),'time':state.world_time}}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def combat_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.actions import _threat_action; from jomon.state import Position, Threat, create_world; "
            "state=create_world('combat-pack-proof'); state.location='region'; state.position=Position(42,25); "
            "flood=Threat('floodgate-claimant','floodgate claimant','reach',Position(45,25),8,8,status='engaged',elite=True); state.threats=[flood]; flood_warning=_threat_action(state,flood,False); state.position=Position(42,26); flood_result=_threat_action(state,flood,False); flood_mechanics=[flood.id,flood.intent_id,flood.turn,sorted(state.water.items())]; "
            "machine=Threat('wheel','runaway crown wheel','machinery',Position(45,22),7,7,status='engaged',elite=True,morale=99); state.threats=[machine]; state.position=Position(42,22); machine_warning=_threat_action(state,machine,False); state.position=Position(42,23); machine_result=_threat_action(state,machine,False); machine_mechanics=[machine.id,machine.intent_id,machine.turn,machine.health,machine.status]; "
            "net=Threat('net','mudflat netter','reach',Position(45,25),5,5,status='engaged',role='controller'); state.threats=[net]; state.position=Position(42,25); net_warning=_threat_action(state,net,False); state.position=Position(42,26); net_result=_threat_action(state,net,False); net_mechanics=[net.id,net.intent_id,net.turn,net.aimed_at,sorted(state.terrain_statuses)]; "
            "print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id,'messages':[flood_warning,flood_result,machine_warning,machine_result,net_warning,net_result],'mechanics':[flood_mechanics,machine_mechanics,net_mechanics]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def production_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.inventory import auto_place, create_item, item_spec; from jomon.production import advance_craft_economy, delegate, gather, make, site_position; from jomon.state import create_world; from jomon.terminal import _overlay_lines; "
            "state=create_world('production-pack-proof'); state.courier.skill_nodes.append('masterwork'); state.location='region'; state.position=site_position(state); first=gather(state, 0); "
            "[auto_place(state, create_item(state, kind, 'fixture input', quantity=quantity).id, 'pack', owner_id=state.active_courier_id) for kind, quantity in (('ingredient:healing herb', 1), ('ingredient:clay', 2), ('commodity:wool', 3))]; "
            "dressing=make(state, 'field-dressing'); made=make(state, 'make:smoke bomb kit'); state.courier.speech=7; state.trade_credit=2; delegated=delegate(state, 'make:smoke bomb kit'); state.world_time=36; advance_craft_economy(state); catalog=_overlay_lines(state, 'craft-catalog:0'); "
            "masterwork=next(item for item in state.items if item.kind == 'smoke bomb kit' and item.location == 'pack'); print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id, 'messages':[first[1],dressing[1],made[1],masterwork.provenance,delegated[1],state.production['records'][-1],catalog[0]], 'mechanics':{'sources':state.production['sites']['hearthford'], 'orders':state.production['orders'], 'output':sorted((item.kind,item.quantity,item.location,item.region_id,item.masterwork) for item in state.items if item.kind in {'smoke bomb kit', 'consumable:willow dressing'}), 'credit':state.trade_credit, 'time':state.world_time, 'recipes':['field-dressing','make:smoke bomb kit']}}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def chemistry_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.chemistry import drink_flask, fill_flask, predicted_reactions; "
            "from jomon.inventory import auto_place, create_item, item_spec; from jomon.state import create_world; "
            "state=create_world('chemistry-pack-proof'); flask=create_item(state, 'field flask', 'fixture flask'); "
            "herb=create_item(state, 'ingredient:healing herb', 'fixture herb'); water=create_item(state, 'ingredient:spring water', 'fixture water'); "
            "[auto_place(state, item.id, 'pack', owner_id=state.active_courier_id) for item in (flask, herb, water)]; "
            "first=fill_flask(state, flask.id, herb.id); second=fill_flask(state, flask.id, water.id); reaction=predicted_reactions(flask.contents); before=state.courier.health; drank=drink_flask(state, flask.id); "
            "print(json.dumps({'pack': __import__('jomon.catalog', fromlist=['selected_content_pack']).selected_content_pack().id, "
            "'messages': [first[1], second[1], drank[1]], 'mechanics': [reaction, state.courier.known_formulas, before, state.courier.health, flask.contents, herb.location, water.location, state.world_time, flask.kind, herb.kind, water.kind]}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


def progression_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.manoeuvres import BY_ID, known; from jomon.practices import practice_display_name; from jomon.progression_presentation import progression_format, progression_text, technique_display_name; from jomon.skill_tree import buy_node, record_milestone; from jomon.state import create_world; from jomon.terminal import _overlay_lines; "
            "state=create_world('progression-pack-proof'); record_milestone(state,'return:hearthford'); bought=buy_node(state,'edge-measure'); state.courier.learned_techniques.extend(('practice.bank_water_cadence','practice.personal:bargemaster')); manoeuvres=known(state); overlay=_overlay_lines(state,'skill-tree'); mastery=_overlay_lines(state,'mastery'); print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id, 'messages':[bought[1], practice_display_name('practice.bank_water_cadence'), BY_ID['braced-advance'].name, overlay[0], overlay[1], mastery[0], progression_format('progression.journal.write.result',courier=state.courier.name,node='Edge measure',journal='journal-1'), progression_format('progression.skill.teach.result',teacher='Arel',node='Edge measure',recipient='Bryn'), progression_text('progression.choice.journal.write.requirement'), technique_display_name('practice.personal:bargemaster')], 'mechanics':{'nodes':state.courier.skill_nodes,'practices':state.courier.learned_techniques,'manoeuvres':[row.id for row in manoeuvres],'effects':[row.practice for row in manoeuvres],'points':state.courier.skill_points,'milestones':state.courier.skill_milestones,'personal_id':'practice.personal:bargemaster'}}))",
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
        accept = pack.aftermath_action_presentation("aftermath.action.accept")
        self.assertEqual(accept.label, "Fixture accept copy")
        self.assertEqual(
            dict(pack.aftermath_action_presentation("aftermath.action.deliver").requirements)["missing_supply"],
            "Fixture needs {commodity}.",
        )
        self.assertEqual(
            pack.aftermath_result_presentation("accepted").text,
            "Fixture accepts {title}: {copy_state} Supply {commodity} at {site}.",
        )
        self.assertEqual(
            pack.aftermath_result_presentation("ledger_cause").text,
            "FIXTURE CAUSE — {cause}.",
        )

    def test_alternate_pack_changes_workline_presentation_not_workline_mechanics(self):
        default = workline_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = workline_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["opening_options"], alternate["opening_options"])
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("Fixture Water Works", alternate["opening"])
        self.assertIn("Fixture survey guidance", " ".join(alternate["opening_lines"]))
        self.assertIn("Fixture timber produces", alternate["work"])
        self.assertIn("Fixture commons preserve", alternate["settlement"])
        self.assertIn("The Houses Above the Race", default["opening"])
        self.assertEqual(default["work"], "Timber raises the watch-house approach; flooded ground becomes a firm shared route.")
        self.assertIn("Local commons: the work remains in place", default["settlement"])

    def test_workline_presentation_validation_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "worklines.json"
            original = source.read_text(encoding="utf-8")
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*worklines\.json"):
                    load_content_pack(root)
                source.write_text(original, encoding="utf-8")
            with self.subTest("unknown key"):
                document = json.loads(original)
                document["text"]["workline.unknown"] = "unexpected"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*unknown workline keys"):
                    load_content_pack(root)
            with self.subTest("invalid placeholder"):
                document = json.loads(original)
                document["text"]["workline.line.witness"] = "Witness {witness.name}."
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*workline\.line\.witness.*malformed template"):
                    load_content_pack(root)

    def test_alternate_pack_changes_interference_presentation_not_arrival_mechanics(self):
        default = interference_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = interference_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("FIXTURE INTERFERENCE", alternate["notice"])
        self.assertIn("FIXTURE RECORD", alternate["chronicle"])
        self.assertIn("FIXTURE ARRIVAL", " ".join(alternate["ledger"]))
        self.assertIn("INTERFERENCE — The measured grain release", default["notice"])
        self.assertIn("The measured grain release: Hearthford's settled bank work", default["chronicle"])

    def test_interference_presentation_validation_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "interference_text.json"
            original = source.read_text(encoding="utf-8")
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*interference_text\.json"):
                    load_content_pack(root)
                source.write_text(original, encoding="utf-8")
            with self.subTest("unknown key"):
                document = json.loads(original)
                document["text"]["interference.unknown"] = "unexpected"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*unknown interference keys"):
                    load_content_pack(root)
            with self.subTest("invalid placeholder"):
                document = json.loads(original)
                document["text"]["interference.notice"] = "Warning {title.name}."
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*interference\.notice.*malformed template"):
                    load_content_pack(root)

    def test_alternate_pack_changes_legendary_and_arc_relic_presentation_not_mechanics(self):
        default = legendary_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = legendary_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["legends"], alternate["legends"])
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("Fixture", " ".join(part for row in alternate["presented"] for part in row[1:]))
        self.assertEqual(alternate["relic"][0], "Fixture Rivet")
        self.assertIn("Fixture repair", alternate["relic"][1])
        self.assertIn("Fixture rivet repairs", alternate["relic"][2])
        self.assertIn("Common-Work Rivet", default["relic"][0])

    def test_legendary_presentation_validation_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "legendary_text.json"
            original = source.read_text(encoding="utf-8")
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*legendary_text\.json"):
                    load_content_pack(root)
                source.write_text(original, encoding="utf-8")
            with self.subTest("unknown key"):
                document = json.loads(original)
                document["text"]["legendary.unknown"] = "unexpected"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*unknown legendary keys"):
                    load_content_pack(root)
            with self.subTest("invalid placeholder"):
                document = json.loads(original)
                document["text"]["legendary.object.name"] = "{maker.name}"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*legendary\.object\.name.*malformed template"):
                    load_content_pack(root)

    def test_default_legendary_presentation_preserves_existing_text(self):
        from jomon.arc_relics import ARC_RELIC_DESCRIPTIONS
        from jomon.legendary_presentation import legendary_format, legendary_text

        self.assertEqual(legendary_format("legendary.object.name", maker="Asha", noun="Measure"), "Asha's Measure")
        self.assertEqual(
            legendary_format("legendary.object.effect", epithet="braced", tags="iron", range="", verbs="brace"),
            "Its braced construction grants iron handling; it can brace material where appropriate.",
        )
        self.assertEqual(
            legendary_text("legendary.arc.common-work-rivet.result"),
            "The master rivet repairs {equipment} worn items and seats {supports} supports; shared resistance leaves three-action fatigue.",
        )
        self.assertEqual(
            ARC_RELIC_DESCRIPTIONS["common-work rivet"],
            "A master rivet repairs worn equipment and nearby supports together, but the resisting work leaves the bearer fatigued.",
        )

    def test_alternate_pack_changes_hearthford_topology_presentation_not_generation(self):
        default = topology_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = topology_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("Fixture Waterworks", alternate["presentation"][0])
        self.assertIn(["hearthford:mill_ladder", "fixture service climb"], alternate["presentation"][1])
        self.assertIn(["cellar", "Fixture submerged strongbox"], alternate["presentation"][2])
        self.assertIn("fixture engine house", " ".join(label for _id, label in alternate["presentation"][3]))
        self.assertIn("Hearthford millworks", default["presentation"][0])

    def test_default_regional_generator_presentation_preserves_existing_text(self):
        from jomon.state import create_world

        state = create_world("regional-generator-default-text")
        expected = {
            "greywash": (
                "Wind and tide expose a wreck road only while the flats drain.",
                "working tide", "Salt pans", "Quayside salt coffer", "Edda Marr", "salt reeve",
            ),
            "greenwold": (
                "A shifting forest wind carries an illicit charcoal burn toward medicine coppice.",
                "shifting burn wind", "Raised burnworks", "Canopy cache", "Nera Holt", "charcoal reeve",
            ),
            "whitecairn": (
                "Repeated quarry bells warn of a ridge cut that is becoming unstable.",
                "quarry instability", "Quarry face", "Bell parapet chest", "Pera Chalk", "quarry factor",
            ),
        }
        for region_id, (condition, process, zone, container, contact_name, role) in expected.items():
            with self.subTest(region_id=region_id):
                region = state.regions[region_id]
                self.assertEqual(region.condition, condition)
                self.assertEqual(region.process_name, process)
                self.assertIn(zone, region.zones)
                self.assertIn(container, [box.name for box in region.containers])
                self.assertEqual((state.contacts[region_id][0].name, state.contacts[region_id][0].role), (contact_name, role))

    def test_alternate_pack_changes_regional_generator_presentation_not_generation(self):
        default = regional_generator_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = regional_generator_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("Fixture brine terraces", alternate["presentation"]["greywash"][1])
        self.assertIn(["greywash-quay", "Fixture tide coffer"], alternate["presentation"]["greywash"][3])
        self.assertIn("Fixture pitch enclosure", alternate["presentation"]["greenwold"][1])
        self.assertIn(["greenwold:canopy_ladder", "fixture bough ascent"], alternate["presentation"]["greenwold"][2])
        self.assertIn("Fixture stone shelf", alternate["presentation"]["whitecairn"][1])
        self.assertIn(["whitecairn-tower", "Fixture bell cache"], alternate["presentation"]["whitecairn"][3])
        self.assertIn("fixture signal tower", " ".join(label for _id, label in alternate["presentation"]["whitecairn"][5]))

    def test_default_frontier_generator_presentation_preserves_existing_text(self):
        from jomon.frontiers import ensure_frontier
        from jomon.state import create_world

        state = create_world("frontier-generator-default-text")
        expected = {
            "dunmire": ("Peat and seasonal water shape Dunmire Peat Isles.", "fen overtopping", "Dunmire Quay tool chest", "Yara Silt", "peat steward"),
            "rillscar": ("Ironstone and seasonal water shape Rillscar Iron Gorge.", "tailrace fracture", "Rillscar Quay tool chest", "Halen Crag", "cutworks factor"),
            "marlbank": ("Clay and seasonal water shape Marlbank Clay Terraces.", "irrigation release", "Marlbank Quay tool chest", "Mera Loam", "terrace keeper"),
            "frostmere": ("Gravel and seasonal water shape Frostmere Braided Estuary.", "ice-channel breakup", "Frostmere Quay tool chest", "Sarin Rill", "net-house keeper"),
        }
        for region_id, (condition, process, container, contact_name, role) in expected.items():
            with self.subTest(region_id=region_id):
                ensure_frontier(state, region_id)
                region = state.regions[region_id]
                self.assertEqual(region.condition, condition)
                self.assertEqual(region.process_name, process)
                self.assertIn(container, [box.name for box in region.containers])
                self.assertEqual((state.contacts[region_id][0].name, state.contacts[region_id][0].role), (contact_name, role))

    def test_alternate_pack_changes_frontier_generator_presentation_not_generation(self):
        default = frontier_generator_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = frontier_generator_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("Fixture peat scar", alternate["presentation"]["dunmire"][1])
        self.assertIn(["dunmire-quay", "Fixture fen coffer"], alternate["presentation"]["dunmire"][3])
        self.assertIn(["rillscar:work_stair", "fixture cliff ascent"], alternate["presentation"]["rillscar"][2])
        self.assertIn("fixture span works", " ".join(label for _id, label in alternate["presentation"]["rillscar"][5]))
        self.assertIn("Fixture firing court", alternate["presentation"]["marlbank"][1])
        self.assertIn("fixture terrace steward", " ".join(row[2] for row in alternate["presentation"]["marlbank"][4]))
        self.assertIn("Fixture ice channel", alternate["presentation"]["frostmere"][1])
        self.assertEqual(alternate["presentation"]["frostmere"][0][0], "Fixture gravel and water preserve the same estuary geometry.")

    def test_topology_presentation_validation_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "topology_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing key": lambda value: value["text"].pop("topology.hearthford.container.cellar"),
                "wrong type": lambda value: value["text"].update({"topology.hearthford.container.cellar": 1}),
                "empty value": lambda value: value["text"].update({"topology.hearthford.container.cellar": ""}),
                "malformed template": lambda value: value["text"].update({"topology.hearthford.zone.millworks": "{"}),
                "unknown placeholder": lambda value: value["text"].update({"topology.hearthford.zone.millworks": "{zone}"}),
                "missing regional slot": lambda value: value["text"].pop("topology.greywash.container.quay"),
                "wrong regional type": lambda value: value["text"].update({"topology.greenwold.contact.1.name": 1}),
                "missing frontier slot": lambda value: value["text"].pop("topology.dunmire.contact.1.memory"),
                "wrong frontier type": lambda value: value["text"].update({"topology.frostmere.process.applied": 1}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*topology_text\.json"):
                        load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*topology_text\.json"):
                    load_content_pack(root)
                source.write_text(original, encoding="utf-8")
            with self.subTest("unknown key"):
                document = json.loads(original)
                document["text"]["topology.hearthford.extra"] = "unexpected"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*unknown topology keys"):
                    load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(
                    original.replace('"topology.hearthford.zone.millworks"', '"topology.hearthford.zone.settlement"', 1),
                    encoding="utf-8",
                )
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*topology_text\.json"):
                    load_content_pack(root)

    def test_default_topology_presentation_preserves_existing_text(self):
        from jomon.topology_presentation import topology_text

        self.assertEqual(topology_text("topology.hearthford.zone.millworks"), "Hearthford millworks")
        self.assertEqual(topology_text("topology.hearthford.link.mill_ladder"), "mill ladder")
        self.assertEqual(topology_text("topology.hearthford.container.cellar"), "Buried mill strongbox")
        self.assertEqual(topology_text("topology.greywash.zone.salt_pans"), "Salt pans")
        self.assertEqual(topology_text("topology.greenwold.link.canopy_ladder"), "canopy ladder")
        self.assertEqual(topology_text("topology.whitecairn.container.tower"), "Bell parapet chest")
        self.assertEqual(topology_text("topology.greywash.contact.1.name"), "Edda Marr")

    def test_default_social_action_presentation_preserves_existing_text(self):
        from jomon.action_presentation import action_format, action_text

        self.assertEqual(action_text("social.negotiate.no_terms"), "You lack witnessed seals, material surety, paper, or valuable leverage.")
        self.assertEqual(action_format("social.objective.refused", courier="Iris", region="Hearthford"), "Iris refuses Hearthford's difficult request.")
        self.assertEqual(action_format("social.recruit.accepted", visitor="Mara"), "Mara accepts a berth aboard Jomon.")

    def test_alternate_pack_changes_social_presentation_not_mechanics(self):
        default = social_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = social_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertEqual(default["intent"], alternate["intent"])
        self.assertIn("FIXTURE", alternate["messages"][0])
        self.assertIn("FIXTURE", alternate["messages"][1])
        self.assertIn("FIXTURE TERMS", alternate["messages"][2])
        self.assertIn("FIXTURE mediation", alternate["messages"][3])

    def test_default_action_presentation_preserves_specialized_combat_text(self):
        from jomon.action_presentation import action_format, action_text

        self.assertEqual(
            action_format("intent.elite.floodgate.sluice_telegraph", x=42, y=25),
            "marks the mill crossing at 42,25 for a sluice surge",
        )
        self.assertEqual(
            action_format(
                "combat.elite.floodgate.telegraph",
                threat="floodgate claimant", intent="marks the mill crossing at 42,25 for a sluice surge",
            ),
            "The floodgate claimant marks the mill crossing at 42,25 for a sluice surge; climb, move, guard, or dog the control.",
        )
        self.assertEqual(action_text("combat.machinery.sweep_source"), "The mill sweep")

    def test_action_presentation_contract_rejects_invalid_pack_data(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "action_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing key": lambda value: value["text"].pop("combat.elite.floodgate.telegraph"),
                "wrong type": lambda value: value["text"].update({"combat.elite.floodgate.safe": 1}),
                "empty text": lambda value: value["text"].update({"combat.machinery.safe": ""}),
                "malformed template": lambda value: value["text"].update({"intent.controller.net_telegraph": "{"}),
                "unknown placeholder": lambda value: value["text"].update({"intent.controller.net_telegraph": "{other}"}),
                "missing placeholder": lambda value: value["text"].update({"intent.controller.net_telegraph": "fixture net"}),
                "unknown key": lambda value: value["text"].update({"combat.elite.extra": "unexpected"}),
                "missing social key": lambda value: value["text"].pop("social.negotiate.success"),
                "invalid social placeholder": lambda value: value["text"].update({"social.negotiate.success": "terms {other}"}),
                "missing remaining-action key": lambda value: value["text"].pop("action.item.lamp_wick.used"),
                "invalid remaining-action placeholder": lambda value: value["text"].update({"action.item.flood_mark.used": "fixture {other}"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    content = json.loads(original)
                    mutate(content)
                    source.write_text(json.dumps(content), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*action_text\.json"):
                        load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(
                    original.replace('"combat.elite.floodgate.safe"', '"combat.elite.floodgate.telegraph"', 1),
                    encoding="utf-8",
                )
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*action_text\.json"):
                    load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*action_text\.json"):
                    load_content_pack(root)

    def test_alternate_pack_changes_specialized_combat_presentation_not_mechanics(self):
        default = combat_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = combat_presentation_snapshot(environment)

        self.assertEqual(default["pack"], "default")
        self.assertEqual(alternate["pack"], "fixture-alternate")
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertNotEqual(default["messages"], alternate["messages"])
        self.assertIn("FIXTURE FLOODGATE", alternate["messages"][0])
        self.assertIn("FIXTURE MACHINE", alternate["messages"][2])
        self.assertIn("FIXTURE NET", alternate["messages"][5])

    def test_default_remaining_action_presentation_preserves_existing_text(self):
        from jomon.action_presentation import action_format

        self.assertEqual(action_format("action.item.lamp_wick.used"), "A dry wick restores two measures of sheltered light.")
        self.assertEqual(action_format("action.environment.hearthford.control", control="lowered"), "The sluice is lowered; water crosses culvert and ground openings, changing route safety.")

    def test_alternate_pack_changes_remaining_action_presentation_not_mechanics(self):
        default = remaining_action_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = remaining_action_presentation_snapshot(environment)

        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("FIXTURE PREPARED", alternate["messages"][0])
        self.assertIn("FIXTURE DRESSING", alternate["messages"][1])
        self.assertIn("FIXTURE CONTROL", alternate["messages"][2])
        self.assertIn("FIXTURE ROUTE SERVICE", alternate["messages"][3])

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


    def test_default_production_presentation_preserves_existing_text(self):
        from jomon.production_presentation import production_format, production_recipe_name, production_station_name

        self.assertEqual(production_recipe_name("field-dressing"), "Prepare a field dressing")
        self.assertEqual(production_station_name("forge"), "forge")
        self.assertEqual(production_format("production.make.result", courier="Rowan", recipe="a kit", station="portable"),
                         "Rowan completes a kit at the portable; every input was physically spent.")

    def test_alternate_pack_changes_production_presentation_not_mechanics(self):
        default = production_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = production_presentation_snapshot(environment)

        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("FIXTURE", alternate["messages"][0])
        self.assertIn("Fixture field remedy", alternate["messages"][1])
        self.assertIn("Fixture fabricate", alternate["messages"][2])
        self.assertIn("FIXTURE superior", alternate["messages"][3])
        self.assertIn("FIXTURE", alternate["messages"][4])
        self.assertIn("FIXTURE day", alternate["messages"][5])
        self.assertEqual(alternate["messages"][6], "FIXTURE WORKING PLANS")

    def test_production_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "production_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing recipe": lambda value: value["text"].pop("production.recipe.field-dressing.name"),
                "unknown key": lambda value: value["text"].update({"production.recipe.extra.name": "extra"}),
                "empty text": lambda value: value["text"].update({"production.make.result": ""}),
                "unknown placeholder": lambda value: value["text"].update({"production.make.result": "made {other}"}),
                "missing placeholder": lambda value: value["text"].update({"production.make.result": "made {courier}"}),
                "malformed template": lambda value: value["text"].update({"production.gather.result": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*production_text\.json"):
                        load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"production.gather.result"', '"production.make.result"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*production_text\.json"):
                    load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*production_text\.json"):
                    load_content_pack(root)

    def test_default_chemistry_presentation_preserves_existing_text(self):
        from jomon.chemistry_presentation import chemistry_format, chemistry_text, reaction_display_name

        self.assertEqual(chemistry_text("chemistry.reagent.healing_herb.name"), "healing herb")
        self.assertEqual(reaction_display_name("healing draft"), "healing draft")
        self.assertEqual(
            chemistry_format("chemistry.fill.result", measures=1, reagent="healing herb", flask="item-00001", contents="{'healing herb': 1}"),
            "1 healing herb measure(s) enter item-00001; contents {'healing herb': 1}.",
        )

    def test_alternate_pack_changes_chemistry_presentation_not_mechanics(self):
        default = chemistry_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = chemistry_presentation_snapshot(environment)

        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("FIXTURE fill", alternate["messages"][0])
        self.assertIn("fixture restorative", alternate["messages"][1])
        self.assertIn("FIXTURE", alternate["messages"][2])

    def test_chemistry_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "chemistry_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing key": lambda value: value["text"].pop("chemistry.reagent.healing_herb.name"),
                "unknown key": lambda value: value["text"].update({"chemistry.reagent.extra.name": "extra"}),
                "empty text": lambda value: value["text"].update({"chemistry.drink.result": ""}),
                "unknown placeholder": lambda value: value["text"].update({"chemistry.drink.result": "drink {other}"}),
                "missing placeholder": lambda value: value["text"].update({"chemistry.fill.result": "fixture fill"}),
                "malformed template": lambda value: value["text"].update({"chemistry.pour.result": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*chemistry_text\.json"):
                        load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"chemistry.fill.result"', '"chemistry.drink.result"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*chemistry_text\.json"):
                    load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*chemistry_text\.json"):
                    load_content_pack(root)


    def test_default_progression_presentation_preserves_existing_text(self):
        from jomon.progression_presentation import manoeuvre_display, practice_display_name, progression_format

        self.assertEqual(practice_display_name("practice.bank_water_cadence"), "bank-water cadence")
        self.assertEqual(manoeuvre_display("braced-advance", "name"), "Braced advance")
        self.assertEqual(
            progression_format("progression.skill.buy.learned", courier="Rowan", node="Edge measure", description="Hold a guard."),
            "Rowan learns Edge measure: Hold a guard..",
        )

    def test_alternate_pack_changes_progression_presentation_not_mechanics(self):
        default = progression_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = progression_presentation_snapshot(environment)

        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("FIXTURE", alternate["messages"][0])
        self.assertEqual(alternate["messages"][1], "fixture water cadence")
        self.assertEqual(alternate["messages"][2], "Fixture Braced Advance")
        self.assertEqual(alternate["messages"][3], "COURIER SKILLS")
        self.assertIn("Fixture Blades", "\n".join(alternate["messages"][4]))
        self.assertEqual(alternate["messages"][5], "PRACTISED MANOEUVRES")
        self.assertIn("FIXTURE", alternate["messages"][6])
        self.assertIn("FIXTURE", alternate["messages"][7])
        self.assertEqual(alternate["messages"][8], "fixture lesson and paper")
        self.assertEqual(alternate["messages"][9], "seasoned fixture navigator")
        self.assertEqual(alternate["mechanics"]["personal_id"], "practice.personal:bargemaster")

    def test_progression_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "progression_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing branch": lambda value: value["text"].pop("progression.branch.blades.name"),
                "missing node": lambda value: value["text"].pop("progression.node.edge-measure.name"),
                "missing practice": lambda value: value["text"].pop("progression.practice.bank_water_cadence.name"),
                "missing manoeuvre": lambda value: value["text"].pop("progression.manoeuvre.braced-advance.name"),
                "unknown key": lambda value: value["text"].update({"progression.node.extra.name": "extra"}),
                "empty text": lambda value: value["text"].update({"progression.manoeuvre.braced-advance.name": ""}),
                "non-string": lambda value: value["text"].update({"progression.person.personal_effect": 2}),
                "unknown placeholder": lambda value: value["text"].update({"progression.skill.buy.learned": "learns {other}"}),
                "missing placeholder": lambda value: value["text"].update({"progression.skill.buy.learned": "learns {courier}"}),
                "malformed template": lambda value: value["text"].update({"progression.person.practice_effect": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*progression_text\.json"):
                        load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"progression.node.edge-measure.name"', '"progression.skill.buy.invalid"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*progression_text\.json"):
                    load_content_pack(root)
            with self.subTest("missing file"):
                source.unlink()
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*progression_text\.json"):
                    load_content_pack(root)


# Advanced-equipment presentation stays separate from raw inventory and fitting IDs.
def equipment_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.actions import attack; from jomon.inventory import create_item, item_spec, sync_legacy_load; from jomon.state import Position, Threat, create_world; from jomon.work_weapons import WORK_WEAPONS; from jomon.expanded_weapons import ARSENAL; from jomon.workshop import FITTINGS, WORKBENCH, install; "
            "state=create_world('equipment-pack-proof'); state.location='region'; state.position=Position(40,25); state.world_time=8; state.weather='clear'; state.threats=[]; state.region_threats['hearthford']=state.threats; [state.region.tile_changes.__setitem__(f'{x},{y},{z}', '.') for z in (-1,0,1) for y in range(20,31) for x in range(34,51)]; [setattr(item,'location','lost') for item in state.items if item.owner_id==state.active_courier_id and item.location in {'pack','readied','secondary'}]; item=create_item(state,'forked pike','fixture weapon',location='readied',owner_id=state.active_courier_id); sync_legacy_load(state); target=Threat('equipment-proof','proof target','reach',Position(42,25),20,20,status='engaged',morale=10,home_position=Position(42,25)); state.threats.append(target); result=attack(state,target.id); mechanics={'weapon_kind':state.weapon,'target_id':target.id,'intent_id':target.intent_id,'health':target.health,'morale':target.morale,'time':state.world_time,'position':[target.position.x,target.position.y,target.position.z]}; state.location='jomon'; state.jomon_space='vessel'; state.position=WORKBENCH; state.trade_credit=10; parent=create_item(state,'longbow','fixture parent',location='readied',owner_id=state.active_courier_id); sync_legacy_load(state); before=state.trade_credit; changed,message=install(state,parent.id,'quiet binding'); mechanics.update({'fitting_kind':next(part.kind for part in state.items if part.location=='fitted' and part.fitted_to==parent.id),'installed':changed,'credit_spent':before-state.trade_credit,'parent':parent.kind,'fitting_stock':state.vessel_changes['fitting_stock:quiet binding']}); print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id,'messages':[WORK_WEAPONS['forked pike'].name, WORK_WEAPONS['forked pike'].description, ARSENAL['river sabre'].display_name, FITTINGS['quiet binding'].name, FITTINGS['quiet binding'].effect, result.message, message], 'mechanics':mechanics}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class EquipmentPresentationTests(unittest.TestCase):
    def test_default_equipment_presentation_preserves_existing_text(self):
        from jomon.equipment_presentation import equipment_text, fitting_name, work_weapon_description, work_weapon_name
        self.assertEqual(work_weapon_name("forked pike"), "Forked ward pike")
        self.assertEqual(work_weapon_description("forked pike"), "Pins the target and one neighbour across its forward line, buying one turn against a pair. Adjacent foes are inside the forks.")
        self.assertEqual(fitting_name("quiet binding"), "Quiet binding")
        self.assertEqual(equipment_text("equipment.workshop.install.location"), "Approach the lower workshop with a known fitting.")

    def test_alternate_pack_changes_equipment_presentation_not_mechanics(self):
        default = equipment_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = equipment_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertEqual(alternate["messages"][0], "Fixture fork pike")
        self.assertEqual(alternate["messages"][3], "Fixture quiet binding")
        self.assertIn("fixture fork fixes", alternate["messages"][5])
        self.assertIn("FIXTURE workshop", alternate["messages"][6])

    def test_equipment_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "equipment_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing weapon": lambda value: value["text"].pop("equipment.weapon.work.forked_pike.name"),
                "missing fitting": lambda value: value["text"].pop("equipment.fitting.quiet_binding.name"),
                "unknown key": lambda value: value["text"].update({"equipment.weapon.extra.name": "extra"}),
                "empty text": lambda value: value["text"].update({"equipment.workshop.install.location": ""}),
                "non-string": lambda value: value["text"].update({"equipment.workshop.install.location": 3}),
                "unknown placeholder": lambda value: value["text"].update({"equipment.workshop.install.result": "fits {other}"}),
                "missing placeholder": lambda value: value["text"].update({"equipment.workshop.install.result": "fits {fitting}"}),
                "malformed template": lambda value: value["text"].update({"equipment.strike.forked_pike": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*equipment_text\.json"):
                        load_content_pack(root)
            with self.subTest("unsupported field"):
                document = json.loads(original)
                document["extra"] = "not allowed"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*equipment_text\.json"):
                    load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"equipment.strike.forked_pike"', '"equipment.workshop.install.location"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*equipment_text\.json"):
                    load_content_pack(root)


def preparation_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.inventory import auto_place, create_item, sync_legacy_load; from jomon.preparations import apply_preparation, carried_preparations, preparation_status; from jomon.state import MaterialCell, Position, create_world; from jomon.terminal import _overlay_lines; "
            "state=create_world('preparation-pack-proof'); state.location='region'; state.position=Position(40,24); state.region.materials.clear(); item=create_item(state,'consumable:preparation.waterline','fixture preparation'); auto_place(state,item.id,'pack',owner_id=state.active_courier_id); sync_legacy_load(state); carried=carried_preparations(state); ready=preparation_status(state,'preparation.waterline'); overlay=_overlay_lines(state,'field-use'); state.region.materials['40,24,0']=MaterialCell(water=3); changed,message=apply_preparation(state,'preparation.waterline'); mechanics={'preparation_id':'preparation.waterline','item_kind':item.kind,'carried_before':carried, 'ready_before':ready[0], 'changed':changed, 'water':state.region.materials['40,24,0'].water, 'marker':state.region.changes['preparation-used:preparation.waterline'], 'consumables':dict(state.consumables), 'time':state.world_time}; print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id, 'messages':[overlay[1][0], overlay[1][-1], message], 'mechanics':mechanics}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class PreparationPresentationTests(unittest.TestCase):
    def test_default_preparation_presentation_preserves_existing_text(self):
        from jomon.preparation_presentation import preparation_display_name, preparation_text

        self.assertEqual(preparation_display_name("preparation.waterline"), "Race-Gate Chalk")
        self.assertEqual(preparation_text("preparation.waterline.condition"), "nearby released or material water")

    def test_alternate_pack_changes_preparation_presentation_not_mechanics(self):
        default = preparation_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = preparation_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        self.assertIn("Fixture Tide Chalk", alternate["messages"][0])
        self.assertIn("FIXTURE field guidance", alternate["messages"][1])
        self.assertIn("FIXTURE Fixture Tide Chalk", alternate["messages"][2])

    def test_preparation_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "preparation_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing preparation": lambda value: value["text"].pop("preparation.waterline.name"),
                "missing text field": lambda value: value.pop("text"),
                "unknown key": lambda value: value["text"].update({"preparation.extra.name": "extra"}),
                "empty text": lambda value: value["text"].update({"preparation.waterline.description": ""}),
                "non-string": lambda value: value["text"].update({"preparation.waterline.condition": 3}),
                "unknown placeholder": lambda value: value["text"].update({"preparation.apply.success": "uses {other}"}),
                "missing placeholder": lambda value: value["text"].update({"preparation.apply.success": "uses {preparation}"}),
                "malformed template": lambda value: value["text"].update({"preparation.result.waterline": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*preparation_text\.json"):
                        load_content_pack(root)
            with self.subTest("unsupported field"):
                document = json.loads(original)
                document["extra"] = "not allowed"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*preparation_text\.json"):
                    load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"preparation.waterline.name"', '"preparation.waterline.description"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*preparation_text\.json"):
                    load_content_pack(root)


def material_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.materials import advance_materials, handle_material, inspect_material, key; from jomon.state import MaterialCell, Position, create_world; "
            "state=create_world('material-pack-proof'); state.location='region'; state.position=Position(40,25); state.weather='clear'; state.world_time=0; state.region.materials.clear(); point=Position(41,25); state.weapon='billhook'; cell=MaterialCell(material='timber', support=1, reagents={'tree resin':1, 'lime dust':1}); collapse=MaterialCell(material='timber', support=0); state.region.materials[key(point)]=cell; state.region.materials['42,25,0']=collapse; inspection=inspect_material(state,point); processed=advance_materials(state); changed,message=handle_material(state,'brace',point); mechanics={'material_id':cell.material,'reagents':dict(cell.reagents),'support':cell.support,'collapse_due':collapse.collapse_due,'processed':processed,'changed':changed,'time':state.world_time,'cell':vars(cell).copy(),'collapse':vars(collapse).copy()}; print(json.dumps({'messages':[inspection[0],inspection[3],*state.messages,message], 'mechanics':mechanics}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)

def sanctum_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.actions import interact; from jomon.inventory import auto_place, create_item, item_spec; from jomon.sanctums import inspect_lines, shrine_choice; from jomon.state import create_world; "
            "state=create_world('sanctum-pack-proof'); state.location='region'; region=state.region; state.position=region.landmarks['sanctum_shrine']; account=state.institutions[__import__('jomon.sanctums',fromlist=['SITES']).SITES[state.active_region_id]['network']]; lot=create_item(state, f'commodity:{account.dependency}', 'sanctum fixture offering'); auto_place(state,lot.id,'pack',owner_id=state.active_courier_id); before=[account.trust,account.obligation]; lines=inspect_lines(state); offered=shrine_choice(state,'o'); state.position=region.landmarks['sanctum_entry']; entered=interact(state); boss=next(actor for actor in state.threats if actor.id=='sanctum:hearthford:boss'); mechanics={'site':region.id,'network':account.id,'entry':[region.landmarks['sanctum_entry'].x,region.landmarks['sanctum_entry'].y], 'links':sorted((link.id,link.first.x,link.first.y,link.first.z,link.second.x,link.second.y,link.second.z) for link in region.vertical_links if link.id.startswith('sanctum:')), 'caches':sorted((box.id,box.reward,box.requirement,tuple(box.extra_rewards)) for box in region.containers if '-sanctum-' in box.id), 'offered':[offered[0],offered[2],before,account.trust,account.obligation,region.changes['sanctum:opened_by']], 'boss':[boss.id,boss.profile,boss.role,boss.goal,boss.duty,boss.health,boss.glyph,boss.archetype_id,tuple(boss.capabilities)], 'inhabited':region.changes['sanctum:inhabited'], 'time':state.world_time}; print(json.dumps({'presentation':[lines,offered[1],entered.message,boss.name,[(link.id,link.name) for link in region.vertical_links if link.id.startswith('sanctum:')],[(box.id,box.name) for box in region.containers if '-sanctum-' in box.id]],'mechanics':mechanics}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class SanctumPresentationTests(unittest.TestCase):
    def test_default_sanctum_presentation_preserves_existing_text(self):
        from jomon.sanctum_presentation import sanctum_display_name, sanctum_text
        self.assertEqual(sanctum_display_name("hearthford"), "The Silt-Chancel")
        self.assertEqual(sanctum_text("sanctum.link.gallery"), "side gallery stair")

    def test_alternate_sanctum_presentation_changes_text_not_mechanics(self):
        default = sanctum_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ); environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = sanctum_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        rendered = "\n".join(map(str, alternate["presentation"]))
        self.assertIn("Fixture Silt Hall", rendered)
        self.assertIn("fixture threshold stair", rendered)
        self.assertIn("Fixture Abbot", rendered)
        self.assertIn("FIXTURE offering", rendered)

    def test_sanctum_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "sanctum_text.json"; original = source.read_text(encoding="utf-8")
            cases = {
                "missing site": lambda value: value["text"].pop("sanctum.hearthford.name"),
                "missing text": lambda value: value.pop("text"),
                "unknown slot": lambda value: value["text"].update({"sanctum.extra": "extra"}),
                "empty": lambda value: value["text"].update({"sanctum.link.entry": ""}),
                "non-string": lambda value: value["text"].update({"sanctum.link.entry": 3}),
                "unknown placeholder": lambda value: value["text"].update({"sanctum.cache.ward": "{other}"}),
                "missing placeholder": lambda value: value["text"].update({"sanctum.cache.ward": "cache"}),
                "malformed": lambda value: value["text"].update({"sanctum.cache.ward": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document=json.loads(original); mutate(document); source.write_text(json.dumps(document),encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*sanctum_text\.json"):
                        load_content_pack(root)
            with self.subTest("unknown field"):
                document=json.loads(original); document["extra"]="no"; source.write_text(json.dumps(document),encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*sanctum_text\.json"):
                    load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"sanctum.link.entry"', '"sanctum.link.gallery"', 1),encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*sanctum_text\.json"):
                    load_content_pack(root)


class MaterialPresentationTests(unittest.TestCase):
    def test_default_material_presentation_preserves_existing_text(self):
        from jomon.material_presentation import material_display_name, material_text

        self.assertEqual(material_display_name("timber"), "timber")
        self.assertEqual(material_text("material.handle.ignite_requirement"), "Ignition needs dry fuel and one measure of lamp oil.")

    def test_alternate_pack_changes_material_and_chemistry_display_not_mechanics(self):
        default = material_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ)
            environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = material_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        rendered = "\n".join(alternate["messages"])
        self.assertIn("FIXTURE MATERIAL", rendered)
        self.assertIn("fixture timber", rendered)
        self.assertIn("fixture resin", rendered)
        self.assertIn("FIXTURE SUPPORT", rendered)
        self.assertIn("FIXTURE brace", rendered)

    def test_material_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "material_text.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "missing material": lambda value: value["text"].pop("material.name.timber"),
                "missing text field": lambda value: value.pop("text"),
                "unknown key": lambda value: value["text"].update({"material.name.extra": "extra"}),
                "empty text": lambda value: value["text"].update({"material.inspect.prediction": ""}),
                "non-string": lambda value: value["text"].update({"material.inspect.prediction": 3}),
                "unknown placeholder": lambda value: value["text"].update({"material.handle.result": "uses {other}"}),
                "missing placeholder": lambda value: value["text"].update({"material.handle.result": "uses {verb}"}),
                "malformed template": lambda value: value["text"].update({"material.handle.result": "{"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original)
                    mutate(document)
                    source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*material_text\.json"):
                        load_content_pack(root)
            with self.subTest("unsupported field"):
                document = json.loads(original)
                document["extra"] = "not allowed"
                source.write_text(json.dumps(document), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*material_text\.json"):
                    load_content_pack(root)
            with self.subTest("duplicate key"):
                source.write_text(original.replace('"material.name.timber"', '"material.handle.ignite_requirement"', 1), encoding="utf-8")
                with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*material_text\.json"):
                    load_content_pack(root)


if __name__ == "__main__":
    unittest.main()


def landform_discovery_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run(
        [
            sys.executable,
            "-c",
            "import json; from jomon.discoveries import discovery_clue, discovery_name; from jomon.landscape_variation import _spawn_traveller, link_name, pocket_name, structure_name, traveller_name; from jomon.navigation import navigation_targets; from jomon.state import create_world; "
            "state=create_world('landform-pack-proof'); state.location='region'; region=state.region; state.position=region.landmarks['landing']; region.seen=[f'{point.x},{point.y},{point.z}' for point in region.landmarks.values()] + [f'{box.position.x},{box.position.y},{box.position.z}' for box in region.containers] + [f'{point.x},{point.y},{point.z}' for link in region.vertical_links for point in (link.first,link.second)]; traveller=_spawn_traveller(state,region.landmarks['landform_0']); caches=[box for box in region.containers if box.id.startswith(region.id+'-') and box.hidden]; mechanics={'region':region.id,'signature':region.geography_signature,'landmarks':sorted((key,point.x,point.y,point.z) for key,point in region.landmarks.items() if key.startswith('landform_') or key in ('field_upper','field_lower')),'facts':sorted((key,value) for key,value in region.generation_facts.items() if key.startswith('landform:') or key in ('field_upper','field_lower')),'links':sorted((link.id,link.first.x,link.first.y,link.first.z,link.second.x,link.second.y,link.second.z) for link in region.vertical_links if link.id.startswith('landform:')),'caches':sorted((box.id,box.position.x,box.position.y,box.position.z,box.reward,box.requirement) for box in caches),'contacts':sorted((person.id,person.position.x,person.position.y,person.position.z,person.interest) for person in state.contacts[region.id] if person.id.startswith('landform:'))}; presentation={'pockets':[pocket_name(region.id,index) for index in range(4)],'structures':[structure_name(region.id,'field_upper'),structure_name(region.id,'field_lower')],'traveller':traveller_name(region.id),'links':[(link.id,link_name(region.id,link)) for link in region.vertical_links if link.id.startswith('landform:')],'caches':[(box.id,discovery_name(region.id,box),discovery_clue(region.id,box)) for box in caches],'targets':[(target.id,target.label) for target in navigation_targets(state) if target.id.startswith(('landmark:landform_','link:landform:','container:hearthford-'))],'arrival':traveller}; print(json.dumps({'pack':__import__('jomon.catalog',fromlist=['selected_content_pack']).selected_content_pack().id,'mechanics':mechanics,'presentation':presentation}))",
        ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class LandformDiscoveryPresentationTests(unittest.TestCase):
    def test_default_landform_discovery_presentation_preserves_existing_text(self):
        from jomon.discoveries import discovery_clue, discovery_name
        from jomon.landscape_variation import pocket_name, structure_name, traveller_name
        from jomon.state import create_world

        state = create_world("landform-default-text")
        cache = next(box for box in state.region.containers if box.id == "hearthford-reed-silt")
        self.assertEqual(pocket_name("hearthford", 0), "silt braid")
        self.assertEqual(structure_name("hearthford", "field_upper"), "old survey scaffold")
        self.assertEqual(traveller_name("hearthford"), "Pella Reed, flood surveyor")
        self.assertEqual(discovery_name("hearthford", cache), "Reed-silt survey roll")
        self.assertEqual(discovery_clue("hearthford", cache), "fresh reed cuts beside an older silt mark")

    def test_alternate_pack_changes_landform_discovery_presentation_not_mechanics(self):
        default = landform_discovery_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ); environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = landform_discovery_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        rendered = str(alternate["presentation"])
        self.assertIn("Fixture reed shelf", rendered)
        self.assertIn("Fixture upper watch", rendered)
        self.assertIn("Fixture surveyor", rendered)
        self.assertIn("Fixture survey cache", rendered)
        self.assertIn("FIXTURE field arrival", rendered)

    def test_landform_discovery_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "topology_text.json"; original = source.read_text(encoding="utf-8")
            cases = {
                "missing landform": lambda value: value["text"].pop("topology.hearthford.landform.0.name"),
                "unknown landform": lambda value: value["text"].update({"topology.hearthford.landform.extra": "extra"}),
                "empty discovery": lambda value: value["text"].update({"topology.hearthford.discovery.reed-silt.clue": ""}),
                "malformed template": lambda value: value["text"].update({"topology.landform.event.threat": "{"}),
                "missing placeholder": lambda value: value["text"].update({"topology.landform.event.threat": "A {actor} appears."}),
                "unknown placeholder": lambda value: value["text"].update({"topology.discovery.reveal": "{other}"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document=json.loads(original); mutate(document); source.write_text(json.dumps(document),encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*topology_text\.json"):
                        load_content_pack(root)


def situation_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run([
        sys.executable, "-c",
        "import json; from jomon.actions import depart; from jomon.quests import use_secondary_service; from jomon.situations import BY_REGION_BAND, choices, inspect_lines, resolve; from jomon.state import create_world; "
        "state=create_world('situation-pack-proof'); state.weapon='billhook';state.gear='repair tools';depart(state);row=BY_REGION_BAND['hearthford','steady'];before=[state.rope_uses,state.market[state.region.objective_commodity].demand];labels=choices(state,row.id);changed,message,steps=resolve(state,row.id,'t');inspection=inspect_lines(state,row.id);reported,report=use_secondary_service(state,'p'); mechanics={'id':row.id,'region':row.region_id,'band':row.band,'anchor':row.anchor,'effects':row.material_effects,'choices':[key for key,*_ in labels],'outcome_id':state.region.changes['micro-site:outcome_id:'+row.id],'resolved':state.region.changes['micro-site:resolved:'+row.id],'report_id':state.region.changes['micro-site:report:'+row.id],'reported':reported,'rope':state.rope_uses,'demand':state.market[state.region.objective_commodity].demand,'before':before,'point':state.region.changes['micro-site:point:'+row.id],'time':state.world_time}; print(json.dumps({'presentation':[row.name,row.groups,row.duty,labels,message,inspection,report,state.contact.memories[-1]],'mechanics':mechanics}))"
    ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False)
    if result.returncode: raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class SituationPresentationTests(unittest.TestCase):
    def test_default_situation_presentation_preserves_existing_text(self):
        from jomon.situations import BY_REGION_BAND
        row=BY_REGION_BAND['hearthford','steady']
        self.assertEqual(row.name, 'The reed-bank tally')
        self.assertEqual(row.answers[0], 'lay a tool-marked dry path')

    def test_alternate_situation_presentation_changes_text_not_mechanics(self):
        default=situation_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root=alternate_pack(Path(directory)/'fixture'); environment=dict(os.environ);environment['JOMON_CONTENT_PACK']=str(root)
            alternate=situation_presentation_snapshot(environment)
        self.assertEqual(default['mechanics'],alternate['mechanics'])
        rendered=str(alternate['presentation'])
        self.assertIn('Fixture Reed Account',rendered)
        self.assertIn('fixture reed workers',rendered)
        self.assertIn('fixture brace the same tally bank',rendered)
        self.assertIn('FIXTURE',rendered)
        self.assertIn('FIXTURE report remains unfiled.', rendered)

    def test_situation_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root=alternate_pack(Path(directory)/'fixture');source=root/'situation_text.json';original=source.read_text(encoding='utf-8')
            cases={
                'missing situation':lambda v:v['text'].pop('situation.hearthford:steady:reed-tally.title'),
                'unknown situation':lambda v:v['text'].update({'situation.extra.title':'extra'}),
                'missing text field':lambda v:v.pop('text'),
                'empty':lambda v:v['text'].update({'situation.hearthford:steady:reed-tally.duty':''}),
                'non-string':lambda v:v['text'].update({'situation.hearthford:steady:reed-tally.duty':3}),
                'malformed':lambda v:v['text'].update({'situation.notice.activation':'{'}),
                'missing placeholder':lambda v:v['text'].update({'situation.record.resolved':'{title}'}),
                'unknown placeholder':lambda v:v['text'].update({'situation.record.resolved':'{title} {other} {consequence}'}),
            }
            for name,mutate in cases.items():
                with self.subTest(name):
                    document=json.loads(original);mutate(document);source.write_text(json.dumps(document),encoding='utf-8')
                    with self.assertRaisesRegex(ContentPackError,r'fixture-alternate.*situation_text\.json'):
                        load_content_pack(root)
            source.write_text(original.replace('"situation.notice.activation"','"situation.record.resolved"',1),encoding='utf-8')
            with self.assertRaisesRegex(ContentPackError,r'fixture-alternate.*situation_text\.json'):
                load_content_pack(root)


def circuit_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run([
        sys.executable, "-c",
        "import json; from jomon.circuits import advance_circuits, cell_key, diagnostic_lines, place; from jomon.circuit_presentation import circuit_format; from jomon.inventory import auto_place, create_item, item_spec; from jomon.state import CircuitCell, Position, create_world; "
        "state=create_world('circuit-pack-proof');state.location='region';state.position=Position(20,20,0);state.circuits={};[state.region.tile_changes.__setitem__(f'{x},{y},0','.') for y in range(18,23) for x in range(18,27)];item=create_item(state,'circuit:trace','proof stock');auto_place(state,item.id,'pack',owner_id=state.active_courier_id);placed,message=place(state,Position(21,20,0),'buried','trace');rack=CircuitCell('region:hearthford',Position(20,20,0),'surface','rack',charge=1);trace=CircuitCell('region:hearthford',Position(21,20,0),'surface','trace');drain=CircuitCell('region:hearthford',Position(22,20,0),'surface','drain');state.circuits[cell_key(rack.space,rack.position,rack.layer)]=rack;state.circuits[cell_key(trace.space,trace.position,trace.layer)]=trace;state.circuits[cell_key(drain.space,drain.position,drain.layer)]=drain;state.water['22,20,0']=2;state.world_time=5;advance_circuits(state);state.world_time+=1;advance_circuits(state);state.world_time+=1;advance_circuits(state);state.world_time+=1;advance_circuits(state);mechanics={'placed':placed,'kind':state.circuits[cell_key('region:hearthford',Position(21,20,0),'buried')].kind,'position':state.circuits[cell_key('region:hearthford',Position(21,20,0),'buried')].position.__dict__,'layer':'buried','items':sum(item.quantity for item in state.items if item.kind=='circuit:trace'),'rack_charge':rack.charge,'drain_active_until':drain.active_until,'water':state.water.get('22,20,0',0),'time':state.world_time,'keys':sorted(state.circuits)};presentation={'place':message,'diagnostics':diagnostic_lines(state,drain),'event':drain.last_event,'item_name':item_spec('circuit:trace').name,'heading':circuit_format('circuit.terminal.heading',layer='BURIED',x=21,y=20,z=0,description='trace')};print(json.dumps({'mechanics':mechanics,'presentation':presentation}))"
    ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False)
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class CircuitPresentationTests(unittest.TestCase):
    def test_default_circuit_presentation_preserves_part_text(self):
        from jomon.circuit_presentation import circuit_part_description, circuit_part_name
        self.assertEqual(circuit_part_name("trace"), "Lacquered conductor")
        self.assertEqual(circuit_part_description("trace"), "An insulated iron trace for a surface or buried circuit run.")

    def test_alternate_circuit_presentation_changes_text_not_mechanics(self):
        default = circuit_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ); environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = circuit_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        rendered = str(alternate["presentation"])
        self.assertIn("fixture filament", rendered)
        self.assertIn("Fixture filament", rendered)
        self.assertIn("FIXTURE fitted", rendered)
        self.assertIn("FIXTURE pulse", rendered)
        self.assertIn("FIXTURE drained", rendered)
        self.assertIn("FIXTURE CIRCUITS", rendered)

    def test_circuit_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "circuit_text.json"; original = source.read_text(encoding="utf-8")
            cases = {
                "missing part": lambda value: value["text"].pop("circuit.part.trace.name"),
                "unknown slot": lambda value: value["text"].update({"circuit.part.extra.name": "extra"}),
                "missing field": lambda value: value.pop("text"),
                "empty": lambda value: value["text"].update({"circuit.terminal.heading": ""}),
                "non-string": lambda value: value["text"].update({"circuit.terminal.heading": 3}),
                "malformed": lambda value: value["text"].update({"circuit.place.success": "{"}),
                "missing placeholder": lambda value: value["text"].update({"circuit.place.success": "{part}"}),
                "unknown placeholder": lambda value: value["text"].update({"circuit.place.success": "{part} {other} {x} {y} {z} {layer}"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original); mutate(document); source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*circuit_text\.json"):
                        load_content_pack(root)
            source.write_text(original.replace('"circuit.part.trace.name"', '"circuit.part.trace.description"', 1), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*circuit_text\.json"):
                load_content_pack(root)


def ecology_presentation_snapshot(environment: dict[str, str]) -> dict[str, object]:
    result = subprocess.run([
        sys.executable, "-c",
        "import json; from jomon.combat_forecast import forecast_lines, observed_forecasts; from jomon.ecology import resolve_world_action; from jomon.encounters import threat_from_archetype; from jomon.frontier_elites import elite_action; from jomon.state import MaterialCell, Position, Threat, create_world; from jomon.enemy_ai import select_goal; state=create_world('ecology-pack-proof');state.location='region';state.position=Position(30,24);state.threats=[];[state.region.tile_changes.__setitem__(f'{x},{y},0','.') for y in range(20,29) for x in range(26,38)];elite=threat_from_archetype('fen-marshal',Position(35,24),encounter_id='frontier-elite',group='proof');elite.status='engaged';elite.morale=10;elite.home_position=elite.position;state.threats=[elite];state.region.materials['35,24,0']=MaterialCell(material='timber',support=2);first=elite_action(state,elite,False);forecast=observed_forecasts(state);second=elite_action(state,elite,False);hunter=threat_from_archetype('fen-lynx',Position(31,24),encounter_id='ecology',group='proof');prey=Threat('prey','proof hare','animal',Position(32,24),4,4,status='engaged',ecology='prey');hunter.status='engaged';state.threats=[hunter,prey];decision=select_goal(state,hunter);ecology=resolve_world_action(state,hunter,decision);mechanics={'decision_action':decision.action,'decision_goal_id':decision.goal_id,'elite_archetype':elite.archetype_id,'elite_intent_id':elite.intent_id,'water':[state.region.materials[f'{x},24,0'].water for x in (29,30,31)],'supplies':elite.supplies,'forecast_target':forecast[0].target.__dict__ if forecast else None,'forecast_intent_id':forecast[0].intent_id if forecast else '', 'hunter_archetype':hunter.archetype_id,'hunter_intent_id':hunter.intent_id,'hunter_goal_id':hunter.goal_id,'prey_health':prey.health};presentation={'first':first,'forecast':forecast_lines(forecast[0]) if forecast else [],'second':second,'hunter':hunter.name,'ecology':ecology};print(json.dumps({'mechanics':mechanics,'presentation':presentation}))"
    ], cwd=ROOT, env=environment, text=True, capture_output=True, check=False)
    if result.returncode:
        raise AssertionError(result.stderr)
    return json.loads(result.stdout)


class EcologyPresentationTests(unittest.TestCase):
    def test_alternate_ecology_and_elite_presentation_changes_text_not_mechanics(self):
        default = ecology_presentation_snapshot(dict(os.environ))
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            environment = dict(os.environ); environment["JOMON_CONTENT_PACK"] = str(root)
            alternate = ecology_presentation_snapshot(environment)
        self.assertEqual(default["mechanics"], alternate["mechanics"])
        rendered = str(alternate["presentation"])
        for phrase in ("Fixture flood marshal", "FIXTURE prepares", "FIXTURE water", "FIXTURE DANGER", "Fixture reed hunter"):
            self.assertIn(phrase, rendered)

    def test_ecology_presentation_contract_rejects_invalid_authoring(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "ecology_text.json"; original = source.read_text(encoding="utf-8")
            cases = {
                "missing ecology slot": lambda value: value["text"].pop("ecology.actor.fen-lynx.name"),
                "unknown slot": lambda value: value["text"].update({"ecology.extra": "extra"}),
                "missing field": lambda value: value.pop("text"),
                "empty": lambda value: value["text"].update({"frontier.result.surge": ""}),
                "non-string": lambda value: value["text"].update({"frontier.result.surge": 3}),
                "malformed": lambda value: value["text"].update({"frontier.intent.prepare": "{"}),
                "missing placeholder": lambda value: value["text"].update({"frontier.intent.prepare": "prepares {mode}"}),
                "unknown placeholder": lambda value: value["text"].update({"frontier.intent.prepare": "{mode} {position} {other}"}),
            }
            for name, mutate in cases.items():
                with self.subTest(name):
                    document = json.loads(original); mutate(document); source.write_text(json.dumps(document), encoding="utf-8")
                    with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ecology_text\.json"):
                        load_content_pack(root)
            source.write_text(original.replace('"frontier.result.surge"', '"frontier.result.brine"', 1), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, r"fixture-alternate.*ecology_text\.json"):
                load_content_pack(root)

class TavernGamesPresentationTests(unittest.TestCase):
    def test_tavern_game_text_and_visuals_are_selected_pack_presentation(self):
        default = bundled_default_pack()
        with tempfile.TemporaryDirectory() as directory:
            alternate = load_content_pack(alternate_pack(Path(directory) / "fixture"))
        self.assertNotEqual(default.tavern_games.text("draw.opening"), alternate.tavern_games.text("draw.opening"))
        self.assertEqual(default.tavern_games.rank_labels[0], "High card")
        self.assertEqual(alternate.tavern_games.rank_labels[0], "Fixture high hand")
        self.assertNotEqual(default.tavern_games.card_edge, alternate.tavern_games.card_edge)
        self.assertNotEqual(dict(default.tavern_games.dice_faces)[1], dict(alternate.tavern_games.dice_faces)[1])


class DullestDungeonPresentationTests(unittest.TestCase):
    def test_nested_dd_contract_and_presentation_are_selected_pack_owned(self):
        default = bundled_default_pack()
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            alternate = load_content_pack(root)
            self.assertEqual(default.dullest_dungeon.text("cards.bone_saw.name"), "Bone Saw")
            self.assertEqual(alternate.dullest_dungeon.text("cards.bone_saw.name"), "Fixture Paper Saw")
            self.assertNotEqual(dict(default.dullest_dungeon.sprites)["warden"], dict(alternate.dullest_dungeon.sprites)["warden"])
            source = root / "dullest_dungeon" / "text.json"
            original = source.read_text(encoding="utf-8")
            bad = json.loads(original); bad["cards"].pop("bone_saw")
            source.write_text(json.dumps(bad), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "Dullest Dungeon presentation"):
                load_content_pack(root)
            bad = json.loads(original); bad["ui"]["unexpected"] = "extra"
            source.write_text(json.dumps(bad), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "Dullest Dungeon presentation"):
                load_content_pack(root)
            bad = json.loads(original); bad["narration"]["action"] = "{actor} acts."
            source.write_text(json.dumps(bad), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "missing or unknown placeholders"):
                load_content_pack(root)
            bad = json.loads(original); bad["narration"]["action"] = "{actor.name} does {action}."
            source.write_text(json.dumps(bad), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "unsupported placeholder"):
                load_content_pack(root)
            source.write_text(original, encoding="utf-8")
            visual = root / "dullest_dungeon" / "visuals.json"
            bad_visual = json.loads(visual.read_text(encoding="utf-8")); bad_visual["office_sprites"]["warden"] = ["bad"]
            visual.write_text(json.dumps(bad_visual), encoding="utf-8")
            with self.assertRaisesRegex(ContentPackError, "Dullest Dungeon presentation"):
                load_content_pack(root)

    def test_dd_alternate_fiction_keeps_active_match_mechanics(self):
        script = (
            "import json; from jomon.dumbest_dungeon.expedition import new_match, pending_choice_labels; "
            "from jomon.dumbest_dungeon.content import load_catalog; c=load_catalog(); roles=list(c.heroes); "
            "m=new_match('dd-pack-proof','crew-a','crew-b',roles[:4],roles[4:8]); "
            "print(json.dumps({'mechanics':{'world':m['world_id'],'rng':m['rng'],'board':m['board'],'teams':m['teams'],'files':m['files'],'patrols':m['patrols']},'log':m['log']}))"
        )
        default = subprocess.run([sys.executable, "-c", script], cwd=ROOT, text=True, capture_output=True, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            env = dict(os.environ); env["JOMON_CONTENT_PACK"] = str(root)
            alternate = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=env, text=True, capture_output=True, check=True)
        first, second = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(first["mechanics"], second["mechanics"])
        self.assertNotEqual(first["log"], second["log"])

    def test_dd_fiction_changes_keep_independent_rules_fingerprint(self):
        script = (
            "import json; from jomon.dumbest_dungeon.content import load_catalog; "
            "from jomon.dumbest_dungeon.presentation import card_name, role_name, office_sprites; "
            "from jomon.catalog import content_pack_presentation_fingerprint; "
            "c=load_catalog(); print(json.dumps({'rules':c.manifest.fingerprint,'card':card_name('bone_saw'),'role':role_name('warden'),'sprite':office_sprites()['warden'][0],'pack':content_pack_presentation_fingerprint()}))"
        )
        default = subprocess.run([sys.executable, "-c", script], cwd=ROOT, text=True, capture_output=True, check=True)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            env = dict(os.environ); env["JOMON_CONTENT_PACK"] = str(root)
            alternate = subprocess.run([sys.executable, "-c", script], cwd=ROOT, env=env, text=True, capture_output=True, check=True)
        first, second = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(first["rules"], second["rules"])
        self.assertNotEqual(first["pack"], second["pack"])
        self.assertNotEqual(first["card"], second["card"])
        self.assertNotEqual(first["role"], second["role"])
        self.assertNotEqual(first["sprite"], second["sprite"])
