"""Load inert main-world content from the selected content pack."""

from __future__ import annotations

from dataclasses import asdict, dataclass
import json
from hashlib import sha256
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
REGION_CONTRACT_FORMAT = 3
# The existing contract format applies to the complete selected-pack
# presentation surface, not only the historical regional file name.
PRESENTATION_CONTRACT_FORMAT = REGION_CONTRACT_FORMAT
REGION_PRESENTATION_FILE = "regions.json"
CHARACTER_PRESENTATION_FILE = "characters.json"
ITEM_PRESENTATION_FILE = "items.json"
UI_PRESENTATION_FILE = "ui_text.json"
QUEST_PRESENTATION_FILE = "quests.json"
HISTORY_PRESENTATION_FILE = "history_text.json"
AFTERMATH_PRESENTATION_FILE = "aftermath_text.json"
WORKLINE_PRESENTATION_FILE = "worklines.json"
INTERFERENCE_PRESENTATION_FILE = "interference_text.json"
LEGENDARY_PRESENTATION_FILE = "legendary_text.json"
TOPOLOGY_PRESENTATION_FILE = "topology_text.json"
ACTION_PRESENTATION_FILE = "action_text.json"
VESSEL_PRESENTATION_FILE = "vessel_text.json"
TRAVEL_PRESENTATION_FILE = "travel_text.json"
SHIP_CRISIS_PRESENTATION_FILE = "ship_crisis_text.json"
VEHICLE_PRESENTATION_FILE = "vehicle_text.json"
CHEMISTRY_PRESENTATION_FILE = "chemistry_text.json"
PRODUCTION_PRESENTATION_FILE = "production_text.json"
MAGIC_PRESENTATION_FILE = "magic_text.json"
PROGRESSION_PRESENTATION_FILE = "progression_text.json"
EQUIPMENT_PRESENTATION_FILE = "equipment_text.json"
PREPARATION_PRESENTATION_FILE = "preparation_text.json"
MATERIAL_PRESENTATION_FILE = "material_text.json"
SANCTUM_PRESENTATION_FILE = "sanctum_text.json"
SITUATION_PRESENTATION_FILE = "situation_text.json"
CIRCUIT_PRESENTATION_FILE = "circuit_text.json"
DULLEST_DUNGEON_DIRECTORY = "dullest_dungeon"
DULLEST_DUNGEON_TEXT_FILE = "text.json"
DULLEST_DUNGEON_VISUALS_FILE = "visuals.json"
ECOLOGY_PRESENTATION_FILE = "ecology_text.json"
TAVERN_GAMES_PRESENTATION_FILE = "tavern_games.json"

_AFTERMATH_CONTRACT = tuple(
    f"aftermath.contract.{region}.{kind}"
    for region in ("hearthford", "greywash", "greenwold", "whitecairn", "dunmire", "rillscar", "marlbank", "frostmere")
    for kind in ("supply", "scar")
)
_AFTERMATH_ACTION_CONTRACT = {
    "aftermath.action.back": ({}, {}),
    "aftermath.action.accept": ({}, {"near_witness": (), "unavailable": ()}),
    "aftermath.action.deliver": ({"commodity": ()}, {"near_witness_supply": ("commodity",), "missing_supply": ("commodity",), "unavailable": ()}),
    "aftermath.action.work": ({}, {"field_site": ("site",), "missing_tool": (), "crowded_site": (), "unavailable": ()}),
    "aftermath.action.abandon": ({}, {"near_witness": (), "unavailable": ()}),
    "aftermath.action.settle": ({"replacement": ()}, {"near_witness_copy": (), "missing_copy": (), "unavailable": ()}),
}
_AFTERMATH_RESULT_CONTRACT = {
    "accepted_copy": ("title",), "accepted": ("title", "copy_state", "commodity", "site"),
    "copy_packed": (), "copy_ground": (), "replacement_suffix": (),
    "delivered": ("commodity",), "work_drainage": (), "work_fire": (), "work_support": (),
    "work_recovery_marked": (), "work_recovery_exhausted": (), "work_record": ("topology", "effect", "fire_before", "fire_after", "water_before", "water_after", "support_before", "support_after", "coating_before", "coating_after"),
    "worked": ("topology", "effect", "fire_before", "fire_after", "water_before", "water_after", "support_before", "support_after"),
    "settled_supply": (), "settled_field": ("physical",), "settled_witness": ("courier", "title", "approach"),
    "paid": (), "replacement_paid": (), "abandoned": (), "abandoned_witness": ("courier", "title"),
    "remember": ("title", "outcome"), "missing_contract": (), "invalid_action": (),
    "ledger_cause": ("cause",), "ledger_witness": ("name", "role"), "ledger_site": ("site", "commodity"), "ledger_worksite": ("topology",), "ledger_account": ("status", "approach"), "ledger_answers": (), "ledger_copy": (), "ledger_outcome": ("outcome",),
    "service_shared": (), "service_claimed": (), "actor_shared_goal": (), "actor_shared_reason": (), "actor_claimed_goal": (), "actor_claimed_reason": (),
}

# Stable workline keys and formatting surfaces.  The history catalog continues
# to own container, evidence, reward, and branch mechanics; this contract owns
# only the selected-pack words rendered for those mechanics.
_WORKLINE_REGIONS = ("hearthford", "greywash", "greenwold", "whitecairn")
_WORKLINE_TEMPLATE_CONTRACT = {
    **{f"workline.{region}.title": () for region in _WORKLINE_REGIONS},
    **{f"workline.{region}.evidence": () for region in _WORKLINE_REGIONS},
    **{f"workline.{region}.reward": () for region in _WORKLINE_REGIONS},
    "workline.hearthford.branch.h": (), "workline.hearthford.branch.s": (),
    "workline.greywash.branch.l": (), "workline.greywash.branch.q": (),
    "workline.greenwold.branch.c": (), "workline.greenwold.branch.w": (),
    "workline.whitecairn.branch.b": (), "workline.whitecairn.branch.r": (),
    "workline.requirement.crowded": (), "workline.requirement.hearthford.timber": (),
    "workline.requirement.greywash.oil": (), "workline.requirement.greenwold.cutting": (),
    "workline.requirement.greenwold.brine": (), "workline.requirement.whitecairn.ironwork": (),
    "workline.requirement.whitecairn.release": (),
    "workline.option.near_witness": (), "workline.option.copy.label": ("replacement",),
    "workline.option.copy.replacement": (), "workline.option.copy.requirement": (),
    "workline.option.field.label": (), "workline.option.field.requirement": ("requirement",),
    "workline.option.settle_public.label": (), "workline.option.settle_private.label": (),
    "workline.option.settle.requirement": (), "workline.option.abandon.label": (),
    "workline.option.abandon.requirement": (), "workline.line.none": (),
    "workline.line.witness": ("witness", "status"),
    "workline.line.field": ("x", "y", "z", "requirement"),
    "workline.line.carry": ("evidence",), "workline.line.survey": ("evidence", "x", "y", "z"),
    "workline.line.survey_help": (), "workline.line.resolution": (), "workline.line.general": (),
    "workline.line.available": (), "workline.line.chosen": ("branch",),
    "workline.line.evidence": (), "workline.line.fact": ("consequence",),
    "workline.line.beacon": ("status", "account"),
    "workline.guard.goal": (), "workline.guard.reason": (), "workline.guard.notice": ("actor",),
    "workline.survey.item": ("title", "source"), "workline.survey.original": (),
    "workline.survey.replacement": (), "workline.survey.whitecairn": (),
    "workline.survey.result": ("evidence", "copy_state"), "workline.survey.packed": (),
    "workline.survey.ground": (),
    "workline.result.hearthford.h": (), "workline.result.hearthford.s": (),
    "workline.result.greywash.l": (), "workline.result.greywash.q": (),
    "workline.result.greenwold.c": (), "workline.result.greenwold.w": (),
    "workline.result.whitecairn.b": (), "workline.result.whitecairn.r": (),
    "workline.settle.public": (), "workline.settle.private": (), "workline.settle.actor_intent": (),
    "workline.settle.reward": ("title",), "workline.settle.result": ("consequence", "reward", "location", "optional"),
    "workline.settle.packed": (), "workline.settle.ground": (), "workline.settle.optional": (),
    "workline.resolve.unavailable": (), "workline.open.mark": ("title", "evidence"),
    "workline.open.message": ("title", "witness"), "workline.abandon": (),
    "workline.memory": ("courier", "message"), "workline.beacon.account": ("supplied", "status"),
    "workline.beacon.lit": (), "workline.beacon.dark": (),
    "workline.ui.material_entry": (), "workline.ui.ledger_heading": (),
    "workline.item.evidence.description": ("title",),
}

_INTERFERENCE_EVENT_IDS = (
    "measured-grain-release", "wreck-iron-on-span", "burn-refuge-migration",
    "high-bell-winter-mark", "peat-bank-mill-water", "gorge-fitting-return",
    "fired-drain-to-burn", "winter-wool-on-ridge",
)
_INTERFERENCE_TEMPLATE_CONTRACT = {
    **{f"interference.{event_id}.{field}": ()
       for event_id in _INTERFERENCE_EVENT_IDS
       for field in ("title", "cause", "origin_change", "destination_change")},
    "interference.record": ("title", "cause", "origin_change", "destination_change"),
    "interference.notice": ("title", "origin"),
    "interference.ledger.arrival": ("title", "destination_change"),
    "interference.ledger.departure": ("title", "origin_change"),
}

_ACTION_TEMPLATE_CONTRACT = {
    "combat.threat.notice": ("threat", "intent"),
    "combat.intent.activate.pursuer": (), "combat.intent.activate.reach": (), "combat.intent.activate.ranged": (), "combat.intent.activate.animal": (), "combat.intent.activate.machinery": (), "combat.intent.activate.default": (),
    "combat.intent.activate.elite": ("mode", "charges"), "combat.intent.activate.prey": (), "combat.intent.activate.predator": (), "combat.intent.activate.duty": ("duty",), "combat.intent.machinery.braked": (),
    "combat.damage.hit": ("source", "location", "damage", "protection"), "combat.damage.absorbed": ("armour", "absorbed"), "combat.damage.exposed": ("location",), "combat.damage.defeated": ("source", "courier"),
    "combat.brace.reaction": ("weapon", "outcome", "threat", "protection", "dropped"), "combat.brace.protected": ("protection", "location"), "combat.brace.uncovered": ("location",),
    "combat.threat.intent": ("threat", "intent"), "combat.threat.loses_turn": ("threat", "intent"),
    "combat.threat.alarm": ("threat",), "combat.threat.net_hit": ("threat",), "combat.threat.net_miss": ("threat",), "combat.threat.smoke": ("threat",), "combat.threat.retreat_blocked": ("threat",),
    "combat.threat.escape": ("threat", "loss"), "combat.threat.escape_lost": ("item",), "combat.threat.escape_memory": ("threat", "region", "item"), "combat.threat.cargo": ("threat",), "combat.threat.steal": ("threat", "item"),
    "combat.threat.investigate_empty": ("threat",), "combat.threat.investigate_blocked": ("threat",), "combat.threat.investigate": ("threat",), "combat.threat.route_blocked": ("threat",), "combat.threat.wait": ("threat",),
    "combat.threat.cross_level": ("threat",), "combat.threat.firing_blocked": ("threat",), "combat.threat.firing_line": ("threat",), "combat.threat.shot_empty": ("threat", "x", "y"), "combat.threat.shot_cover": ("threat",), "combat.threat.shot_guard": ("cover", "weapon"), "combat.threat.skirmish": (),
    "combat.threat.animal_mud": ("threat",), "combat.threat.animal_guard": ("threat",), "combat.threat.animal_warning": ("threat",), "combat.threat.guard_denied": ("threat",), "combat.threat.attack_warning": ("threat", "marker"), "combat.threat.no_route": ("threat",),
    "combat.attack.unready": (), "combat.attack.no_target": (), "combat.attack.throwing_axe": (), "combat.attack.flail_prepare": (), "combat.attack.crossbow_unloaded": (), "combat.attack.arbalest_reload": ("remaining",), "combat.attack.handgonne_load": ("remaining",), "combat.attack.no_ammunition": ("ammunition",), "combat.attack.path_blocked": (), "combat.attack.prepare": ("weapon", "threat", "range", "cover"), "combat.attack.weather_spoiled": (),
    "combat.attack.defeat_memory": ("courier", "outcome", "threat", "weapon"), "combat.attack.defeat_armour": ("protection", "location"), "combat.attack.defeat_uncovered": ("location", "verb"), "combat.attack.defeated": ("threat", "weapon", "armour", "recovered"), "combat.attack.hit_armour": ("protection", "location"), "combat.attack.hit_uncovered": ("location",), "combat.attack.hit_injury": ("injury",), "combat.attack.hit": ("weapon", "damage", "armour", "injury", "threat", "health", "maximum"),
    "combat.guard.no_danger": (), "combat.guard.no_powder": (), "combat.guard.gun_loading": ("weapon", "current", "required"), "combat.guard.no_crossbow_ammo": (), "combat.guard.crossbow_reload": (), "combat.guard.no_heavy_bolts": (), "combat.guard.arbalest_reload": ("current", "stage"), "combat.guard.no_brace_target": ("reason",), "combat.guard.base.shielded": (), "combat.guard.base.strong": (), "combat.guard.base.normal": (), "combat.guard.wet.strong": (), "combat.guard.wet.weak": (), "combat.guard.counterbrace": (), "combat.guard.support": (), "combat.guard.brace": ("weapon", "threat"), "combat.guard.brace_expired": ("threat",),
}

# Vessel catalog entries retain drink identity, stock and effect mechanics.  This
# contract is the selected-pack surface for the words around those mechanics.
_VESSEL_DRINK_IDS = (
    "hearth-ale", "winter-juniper", "willow-bitter", "miller-small-beer",
    "stillroom-cordial", "smokeleaf-infusion", "reed-tonic", "ebbglass-measure",
)
_VESSEL_SCHEDULE_LABELS = {
    "ready_departure": "ready for departure",
    "regional_work": "working at a regional site",
    "regional_rest": "resting near home",
    "serving": "serving",
    "sleeping": "sleeping",
    "merchant_present": "trading from a counted berth",
    "merchant_away": "away on a regional circuit",
    "securing_tavern": "securing the tavern",
    "defending_cargo": "defending cargo",
    "bracing_hull": "bracing the hull",
    "answering_crew": "answering named crew",
    "dullest": "playing Dullest Dungeon",
    "draw": "playing Tavern Draw",
    "bones": "playing Quay Bones",
    "eating": "eating",
    "drinking": "drinking",
    "socialising": "socialising",
    "waiting": "waiting",
    "watch": "standing watch",
    "steering": "steering",
    "chart": "consulting chart",
    "repairing": "repairing",
    "cargo": "moving cargo",
    "treating": "treating injuries",
    "resting": "resting",
    "training": "training",
    "working": "working",
    "between_watches": "between watches",
    "between_duties": "between duties",
    "between_routes": "between recorded routes",
}
_VESSEL_REFIT_IDS = (
    "galley-fire-cover", "twin-bilge-strainers", "storm-backstay",
    "cargo-rail-netting", "sounding-keel-shoes", "winter-hatch-felt",
    "signal-mast-shutter", "sickbay-sling-cot", "vessel-field-forge",
    "vessel-glass-still", "vessel-gunworks",
)
_VESSEL_REFIT_STATIONS = ("galley", "bilge", "repair", "storage", "helm", "berths", "lookout")
_HOUSEHOLD_STORY_IDS = ("empty-watch", "repair-share", "eight-waters")
_VESSEL_TEMPLATE_CONTRACT = {
    **{f"vessel.drink.{drink_id}.name": () for drink_id in _VESSEL_DRINK_IDS},
    **{f"vessel.drink.{drink_id}.benefit": () for drink_id in _VESSEL_DRINK_IDS},
    **{f"vessel.drink.{drink_id}.drawback": () for drink_id in _VESSEL_DRINK_IDS},
    **{f"vessel.schedule.{key}": () for key in _VESSEL_SCHEDULE_LABELS},
    "vessel.drink.unavailable": (),
    "vessel.drink.credit_refused": ("bartender",),
    "vessel.drink.insufficient_credit": ("drink", "cost"),
    "vessel.drink.incompatible.bartender": ("bartender", "drink", "incompatible"),
    "vessel.drink.bottle_no_pack": (),
    "vessel.drink.bottled": ("drink",),
    "vessel.drink.served": ("drink", "benefit", "drawback"),
    "vessel.drink.memory": ("drink", "courier"),
    "vessel.drink.bottle_unknown": (),
    "vessel.drink.bottle_incompatible": ("drink", "incompatible"),
    "vessel.drink.bottle_missing": ("drink",),
    "vessel.drink.uncorked": ("drink", "benefit", "drawback"),
    "vessel.drink.expired": ("drink", "drawback"),
    "vessel.drink.status.served.cause": (),
    "vessel.drink.status.bottled.cause": (),
    "vessel.drink.status.consequence": ("benefit", "drawback"),
    "vessel.drink.bottle.origin": ("bartender",),
    "vessel.drink.status.willow.cause": (),
    "vessel.drink.status.willow.consequence": (),
    "vessel.bar.choice.browse": (), "vessel.bar.choice.support": (), "vessel.bar.choice.leave": (),
    "vessel.bar.stock.choice": ("drink", "cost", "stock"), "vessel.bar.stock.requirement": (),
    "vessel.bar.choice.drink": (), "vessel.bar.choice.bottle": (),
    "vessel.bar.drink.requirement": ("cost",), "vessel.bar.bottle.requirement": ("cost",),
    "vessel.bar.profile": ("role", "schedule"), "vessel.bar.opinion": ("opinion",),
    "vessel.bar.stock_note": ("bartender",), "vessel.bar.menu": (), "vessel.bar.support": (), "vessel.bar.leave": (),
    "vessel.bar.credit": ("credit", "season"), "vessel.bar.stock.line": ("index", "drink", "benefit", "drawback", "stock"),
    "vessel.bar.stock.title": ("bartender",), "vessel.bar.stock.prompt": (),
    "vessel.bar.detail.benefit": ("benefit",), "vessel.bar.detail.drawback": ("drawback",),
    "vessel.bar.detail.duration": ("duration", "cost"), "vessel.bar.detail.stock": ("stock",),
    "vessel.bar.detail.drink": (), "vessel.bar.detail.bottle": (),
    **{f"vessel.refit.{refit_id}.name": () for refit_id in _VESSEL_REFIT_IDS},
    **{f"vessel.refit.{refit_id}.effect": () for refit_id in _VESSEL_REFIT_IDS},
    **{f"vessel.refit.{refit_id}.drawback": () for refit_id in _VESSEL_REFIT_IDS},
    **{f"vessel.refit.station.{station}.name": () for station in _VESSEL_REFIT_STATIONS},
    "vessel.refit.status.already": (), "vessel.refit.status.location": (),
    "vessel.refit.status.station": ("station",), "vessel.refit.status.cargo": ("cargo",),
    "vessel.refit.status.credit": ("credit",), "vessel.refit.status.ready": (),
    "vessel.refit.install.unavailable": ("refit", "reason"),
    "vessel.refit.install.result": ("refit", "station", "effect", "cargo", "credit", "actions", "drawback"),
    "vessel.refit.choice.fit": ("refit",), "vessel.refit.choice.back": (),
    "vessel.refit.choice.inspect": (), "vessel.refit.overlay.title": ("station",),
    "vessel.refit.overlay.guidance.preview": (), "vessel.refit.overlay.guidance.cost": (),
    "vessel.refit.overlay.status.installed": (), "vessel.refit.overlay.status.requirement": ("cargo", "credit"),
    "vessel.refit.overlay.row": ("index", "refit", "status"),
    "vessel.refit.overlay.effect": ("effect",), "vessel.refit.overlay.drawback": ("drawback",),
    "vessel.refit.station.inspect": (), "vessel.refit.station.installed": ("refits",),
    "vessel.refit.station.none": (),
    **{f"vessel.story.{story_id}.name": () for story_id in _HOUSEHOLD_STORY_IDS},
    **{f"vessel.story.{story_id}.premise": () for story_id in _HOUSEHOLD_STORY_IDS},
    **{f"vessel.story.{story_id}.requirement": () for story_id in _HOUSEHOLD_STORY_IDS},
    **{f"vessel.story.choice.{story_id}.{choice}": () for story_id, choices in {
        "empty-watch": ("o", "d", "w", "p"),
        "repair-share": ("o", "d", "w", "p"),
        "eight-waters": ("o", "d", "c", "h", "r"),
    }.items() for choice in choices},
    **{f"vessel.story.branch.{story_id}.{choice}": () for story_id, choices in {
        "empty-watch": ("w", "p"), "repair-share": ("w", "p"), "eight-waters": ("c", "h", "r"),
    }.items() for choice in choices},
    "vessel.story.status.unopened": (), "vessel.story.status.active": (), "vessel.story.status.completed": (),
    "vessel.story.station.row": ("index", "story", "status"),
    "vessel.story.station.available": ("key", "label"),
    "vessel.story.station.needs": ("key", "label", "requirement"),
    "vessel.story.line.fact": ("status",), "vessel.story.line.open": ("requirement",),
    "vessel.story.line.empty_watch.dead": ("people",), "vessel.story.line.empty_watch.living": (),
    "vessel.story.line.repair_share": ("integrity", "voyages", "timber"),
    "vessel.story.line.eight_waters": ("regions",), "vessel.story.line.eight_waters.none": (),
    "vessel.story.line.decision": ("branch",), "vessel.story.line.outcome.default": (),
    "vessel.story.line.active": (), "vessel.story.line.unopened": (),
    "vessel.story.resolve.defer": ("story",), "vessel.story.resolve.needs": ("story", "reason"),
    "vessel.story.resolve.open.memory": ("story",), "vessel.story.resolve.open.result": ("story",),
    "vessel.story.resolve.completed": ("story",), "vessel.story.resolve.no_living": (),
    "vessel.story.resolve.timber": (), "vessel.story.resolve.invalid": (),
    "vessel.story.outcome.empty_watch.dead": ("people",), "vessel.story.outcome.empty_watch.living": (),
    "vessel.story.outcome.repair_share.physical": (), "vessel.story.outcome.repair_share.common": (),
    "vessel.story.outcome.eight_waters.common": ("regions",),
    "vessel.story.outcome.eight_waters.household": (), "vessel.story.outcome.eight_waters.separate": (),
    "vessel.story.memory": ("story", "outcome"), "vessel.story.record": ("story", "branch", "outcome"),
}

_TRAVEL_TEMPLATE_CONTRACT = {
    "travel.frame.depart": ("origin",), "travel.frame.arrive": ("destination",), "travel.frame.underway": ("hazard",),
    "travel.destination.invalid": (), "travel.destination.active": (), "travel.destination.current": ("destination",),
    "travel.destination.uneventful": (), "travel.destination.arrival": ("destination", "duration"),
    "travel.variant.detail": ("name", "cause", "effect"), "travel.boarders.memory": ("voyage",),
    "travel.finish.memory": ("destination", "consequence"), "travel.finish.message": ("consequence", "destination"),
    "travel.cargo.netting": (), "travel.cargo.empty": (), "travel.cargo.lost": ("cargo",),
    "travel.resolve.none": (), "travel.resolve.deck_required": (), "travel.resolve.obligation_settled": (),
    "travel.result.boarders.counsel": ("institution",), "travel.result.shoal.navigate": ("cost",), "travel.result.shoal.yield": ("damage",),
    "travel.result.driftwood.repel": ("extra",), "travel.result.driftwood.extra": (), "travel.result.driftwood.yield": (),
    "travel.requirement.inspection": ("threshold",), "travel.result.inspection.navigate": (), "travel.result.inspection.signal": (),
    "travel.result.inspection.counsel": ("payment",), "travel.result.inspection.yield": ("loss",), "travel.resolve.response_invalid": (),
    "travel.result.raiders.repel": (), "travel.result.raiders.distract": (), "travel.result.raiders.yield": ("loss", "extra"), "travel.result.raiders.failure": ("loss", "extra"), "travel.result.raiders.extra": ("loss",),
    "travel.result.creature.repel": (), "travel.result.creature.evade": (), "travel.result.creature.bait.single": (), "travel.result.creature.bait.pair": ("required",), "travel.result.creature.failure": (),
    "travel.result.lure.anchor": (), "travel.result.lure.counsel": (), "travel.result.lure.navigate": (), "travel.result.lure.signal": (), "travel.result.lure.failure": (),
    "travel.resolve.memory": ("family", "response", "success"),
    "travel.route.no_leg": (), "travel.route.closed": ("hazard", "season"), "travel.route.integrity": ("required", "current"), "travel.route.reachable": (),
}
_TRAVEL_VARIANT_IDS = ("shortage-skiffs", "displaced-pair", "returning-resonance", "obligation-claim", "marked-shortage-lot", "crosswind-stay", "returning-silt-tongue", "fire-marked-raft", "grease-soaked-store", "worked-seam", "thaw-surge", "counterclaim-inspection")
_TRAVEL_ROUTE_NODES = ("reed-anchor", "charter-market", "ebb-crossing", "coast-refuge", "willow-ferry", "old-lock", "chalk-steps", "storm-post")
_TRAVEL_ROUTE_EDGES = ("h-r", "r-m", "m-g", "g-e", "e-c", "h-w", "w-f", "f-l", "l-k", "k-u", "m-l", "l-s", "s-u", "h-d", "d-w", "h-a", "a-r", "u-i", "k-i", "c-f", "g-f", "s-c", "r-w", "g-s", "f-k")
_TRAVEL_TEMPLATE_CONTRACT.update({
    **{f"travel.variant.{entry}.{field}": () for entry in _TRAVEL_VARIANT_IDS for field in ("name", "cause", "effect", "counterplay")},
    **{f"travel.echo.{entry}.{field}": () for entry in _TRAVEL_VARIANT_IDS for field in ("title", "consequence")},
    **{f"travel.route.node.{entry}.{field}": () for entry in _TRAVEL_ROUTE_NODES for field in ("name", "description")},
    **{f"travel.route.edge.{entry}.hazard": () for entry in _TRAVEL_ROUTE_EDGES},
    **{f"travel.calendar.season.{entry}": () for entry in ("spring", "summer", "autumn", "winter")},
    **{f"travel.calendar.time.{entry}": () for entry in ("dawn", "morning", "afternoon", "evening", "night")},
    **{f"travel.calendar.observance.{entry}": () for entry in ("spring-equinox", "autumn-equinox", "summer-solstice", "winter-solstice")},
    **{f"travel.calendar.route_note.{entry}": () for entry in ("spring", "summer", "autumn", "winter")},
    **{f"travel.route.kind.{entry}": () for entry in ("region", "anchorage", "market", "hazard", "resupply", "unknown", "warning")},
    "travel.calendar.date": ("year", "season", "day", "time"), "travel.calendar.date.observance": ("date", "observance"),
    "travel.calendar.event.season": ("season", "year"), "travel.calendar.event.observance": ("observance",),
    "travel.echo.activation": ("title", "consequence"), "travel.echo.remembered": ("title", "consequence"), "travel.echo.memory": ("title",),
    "travel.echo.actor_memory": ("title", "consequence"), "travel.echo.rival_goal": (), "travel.echo.population": (),
    "travel.route.preview.current": ("name", "description"), "travel.route.preview.moored": (), "travel.route.preview.season": ("season", "note"),
    "travel.route.preview.market": ("market",), "travel.route.preview.market.none": (), "travel.route.preview.no_leg": (),
    "travel.route.preview.unknown": (), "travel.route.preview.leg": ("hazard", "time", "supply"), "travel.route.preview.risk": ("cargo", "weather"),
    "travel.route.preview.contact.region": (), "travel.route.preview.contact.none": (), "travel.route.preview.reachable": (), "travel.route.preview.blocked": ("reason",),
    "travel.route.detail.place": ("description",), "travel.route.detail.market": ("market",), "travel.route.detail.market.none": (), "travel.route.detail.contact.region": (), "travel.route.detail.contact.none": (),
    "travel.route.detail.type": ("kind",), "travel.route.detail.moored": (), "travel.route.detail.no_leg": ("origin",), "travel.route.detail.route": ("hazard", "time", "supply"), "travel.route.detail.risks": ("cargo", "weather"),
    "travel.route.detail.season": ("season", "note"), "travel.route.detail.calendar": ("date",), "travel.route.detail.integrity": ("integrity",), "travel.route.detail.confirm.ready": (), "travel.route.detail.confirm.blocked": (), "travel.route.detail.cancel": (),
    "travel.route.stop.summary": ("supply", "market"), "travel.route.stop.market.none": (), "travel.route.stop.resupply": (), "travel.route.stop.trade": (), "travel.route.stop.soundings": (), "travel.route.stop.guidance": (),
    "travel.route.stop.choice.resupply": ("item", "quantity", "credit"), "travel.route.stop.choice.resupply.requirement": ("credit",), "travel.route.stop.choice.trade": ("item", "quantity", "credit"), "travel.route.stop.choice.trade.requirement": ("item",),
    "travel.route.chart.title": (), "travel.route.chart.current": ("region",), "travel.route.chart.guidance": (), "travel.route.chart.cancel": (), "travel.route.stop.no_cargo": (), "travel.route.hazard.unknown": (),
})

# Crisis mechanics stay in vessel.json; this contract owns only the rendered
# deck-event language selected after those mechanics choose an outcome.
_SHIP_CRISIS_FAMILIES = ("raiders", "creature", "lure", "boarders", "hold-thieves", "storm", "shoal", "driftwood", "galley-fire", "split-seam", "flooded-hold", "inspection")
_SHIP_CRISIS_TEMPLATE_CONTRACT = {
    **{f"crisis.{kind}.title": () for kind in _SHIP_CRISIS_FAMILIES},
    **{f"crisis.{kind}.description": () for kind in _SHIP_CRISIS_FAMILIES},
    "crisis.choice.deck.return": (), "crisis.choice.deck.withdraw": (),
    "crisis.choice.raiders.repel": (), "crisis.choice.raiders.distract": (), "crisis.choice.raiders.yield": (),
    "crisis.choice.creature.repel": (), "crisis.choice.creature.evade": (), "crisis.choice.creature.bait.single": (), "crisis.choice.creature.bait.pair": (),
    "crisis.choice.lure.anchor": (), "crisis.choice.lure.counsel": (), "crisis.choice.lure.navigate": (),
    "crisis.choice.shoal.navigate": (), "crisis.choice.shoal.navigate.variant": (), "crisis.choice.shoal.yield": (), "crisis.choice.shoal.yield.variant": (),
    "crisis.choice.driftwood.repel": (), "crisis.choice.driftwood.repel.variant": (), "crisis.choice.driftwood.yield": (),
    "crisis.choice.inspection.navigate": (), "crisis.choice.inspection.counsel": (), "crisis.choice.inspection.counsel.variant": (), "crisis.choice.inspection.yield": (),
    "crisis.choice.boarders.counsel": (), "crisis.choice.default.withdraw": (), "crisis.choice.tactical.deck": (),
    "crisis.line.variant.cause": ("cause",), "crisis.line.variant.effect": ("effect",), "crisis.line.variant.counterplay": ("counterplay",),
    "crisis.line.integrity": ("integrity", "threats"), "crisis.line.work_position": ("x", "y", "z"), "crisis.line.boarders": (), "crisis.line.pump": (), "crisis.line.help": (), "crisis.line.pending": (),
    "crisis.actor.raider_hook": (), "crisis.actor.raider_ward": (), "crisis.actor.raider_caller": (), "crisis.actor.boarder_bow": (), "crisis.actor.boarder_shield": (), "crisis.actor.boarder_hook": (), "crisis.actor.boarder_claimant": (), "crisis.actor.hold_recoverer": (), "crisis.actor.hold_net": (), "crisis.actor.rudder_grazer": (), "crisis.actor.rudder_mate": (),
    "crisis.goal.cargo_claim": (), "intent.crisis.observe": (), "intent.crisis.netting": (), "intent.crisis.baited": (), "intent.crisis.retreated": (), "intent.crisis.surrendered": (),
    "crisis.begin.invalid": (), "crisis.begin.continues": (), "crisis.begin.alarm.station": ("title", "station"), "crisis.begin.alarm.boarders": ("title",),
    "crisis.finish.hazard_settled": (), "crisis.finish.abandon": ("loss",), "crisis.finish.chronicle": ("voyage", "kind", "message"),
    "crisis.advance.work_undone": ("title", "x", "y", "z", "integrity"), "crisis.advance.assist": ("person", "target"), "crisis.advance.leave": ("threat",), "crisis.finish.decks_clear": (),
    "crisis.station.bait": (), "crisis.station.emergency": (), "crisis.station.gangplank": (), "crisis.station.repair": (), "crisis.station.pump": (), "crisis.station.meal": (), "crisis.station.treat": (),
    "crisis.work.detail.repair": (), "crisis.work.detail.pump": (), "crisis.work.detail.meal": (), "crisis.work.detail.emergency": (), "crisis.work.detail.bait": (), "crisis.work.detail.treat": (),
    "crisis.work.line.header": ("integrity", "detail"), "crisis.work.line.hold": ("cargo", "ready"), "crisis.work.line.cargo": ("item", "quantity"), "crisis.work.line.variant": ("effect", "counterplay"), "crisis.work.line.confirm": (), "crisis.work.no_readied": (), "crisis.work.moored": (), "crisis.work.none": (),
    "crisis.work.invalid_station": (), "crisis.work.bait_requirement": ("required", "suffix"), "crisis.work.bait": ("required", "suffix", "animal"), "crisis.work.repair_requirement": (), "crisis.work.repair": (), "crisis.work.pump_requirement": (), "crisis.work.pump": (), "crisis.work.pump.strainers": (), "crisis.work.meal_requirement": (), "crisis.work.meal": ("food", "restored"), "crisis.work.treat.cot": (), "crisis.work.treat.injury": (), "crisis.work.treat.wool": (), "crisis.work.treat": ("injury", "location"), "crisis.work.emergency.requirement": (), "crisis.work.emergency": (), "crisis.work.emergency.pressure": (), "crisis.work.emergency.accelerated": (),
    "crisis.item.unsecured_shipment": ("voyage",), "crisis.defeat.message": ("text", "outcome", "x", "y", "z"),
}


_VEHICLE_IDS = ("tug", "horse_cart", "steam_crawler", "rootwalker", "aether_glider")
_VEHICLE_TEMPLATE_CONTRACT = {
    **{f"vehicle.{vehicle_id}.name": () for vehicle_id in _VEHICLE_IDS},
    **{f"vehicle.{vehicle_id}.resource": () for vehicle_id in _VEHICLE_IDS},
    **{f"vehicle.domain.{domain}": () for domain in ("water", "road", "rough", "air")},
    "vehicle.board.tug.position": (), "vehicle.board.tug.unavailable": (), "vehicle.board.tug.route": (), "vehicle.board.tug.mooring": (), "vehicle.board.tug.success": (),
    "vehicle.board.region.none": (), "vehicle.board.region.damaged": ("vehicle",), "vehicle.board.region.success": ("vehicle",),
    "vehicle.disembark.none": (), "vehicle.disembark.ground": (), "vehicle.disembark.success": ("vehicle",),
    "vehicle.navigate.none": (), "vehicle.navigate.disabled": (), "vehicle.navigate.landscape": (), "vehicle.navigate.no_charge": (), "vehicle.navigate.blocked": (),
    "vehicle.navigate.result": ("vehicle", "travelled", "suffix", "fuel", "capacity", "resource"), "vehicle.navigate.tug_manual": (), "vehicle.navigate.glider_manual": (), "vehicle.navigate.frame": (),
    "vehicle.service.none": (), "vehicle.service.frame_full": (), "vehicle.service.frame_stand": (), "vehicle.service.frame_jury": (), "vehicle.service.reserve_full": (), "vehicle.service.halt": ("resource",), "vehicle.service.mana_requirement": (), "vehicle.service.mana": (), "vehicle.service.charcoal": (), "vehicle.service.credit": (), "vehicle.service.requirement": (),
    "vehicle.interior.none.title": (), "vehicle.interior.none": (), "vehicle.interior.header": ("vehicle", "domain"), "vehicle.interior.reserve": ("fuel", "capacity", "resource", "condition", "maximum"), "vehicle.interior.location.region": ("region", "x", "y"), "vehicle.interior.location.water": ("x", "y"), "vehicle.interior.guidance": (), "vehicle.interior.controls": (), "vehicle.interior.tug_marks": (), "vehicle.interior.tug_manual": (),
    "vehicle.interior.draw.status": ("fuel", "capacity", "resource", "condition", "maximum"), "vehicle.interior.draw.location.region": ("region", "x", "y"), "vehicle.interior.draw.location.water": ("x", "y"), "vehicle.interior.draw.controls": (), "vehicle.interior.cargo": (), "vehicle.interior.fixture": (),
    "vehicle.gangplank.choice.ashore": (), "vehicle.gangplank.choice.tug": (), "vehicle.gangplank.requirement": (), "vehicle.gangplank.title": (), "vehicle.gangplank.line.ashore": ("region",), "vehicle.gangplank.line.tug": (), "vehicle.gangplank.line.crossing": (),
}


# Raw chemistry values are stable engine IDs.  They remain deliberately
# compatible with persisted ingredient:<reagent> item kinds and formula journals;
# selected packs only supply the text shown for those IDs.
_CHEMISTRY_REAGENT_IDS = (
    "cinder salt", "tree resin", "brine", "lime dust", "frostwort", "smoke leaf",
    "healing herb", "glow spore", "peat oil", "iron filings", "spring water",
    "spark salt", "iron ore", "clay",
)
_CHEMISTRY_REACTION_IDS = (
    "flame bloom", "oil flare", "smoke bloom", "conductive flash", "caustic slurry",
    "freezing wash", "healing draft", "attunement draft", "luminous seal",
    "corrosive grit", "hardening wash", "breath tonic", "resin mortar", "brine rime",
    "phosphor dust", "shrapnel spark", "peat haze", "salt-lime slurry",
)

def _chemistry_slot(prefix: str, engine_id: str) -> str:
    return f"chemistry.{prefix}.{engine_id.replace(' ', '_').replace('-', '_')}.name"

_CHEMISTRY_TEMPLATE_CONTRACT = {
    **{_chemistry_slot("reagent", engine_id): () for engine_id in _CHEMISTRY_REAGENT_IDS},
    **{_chemistry_slot("reaction", engine_id): () for engine_id in _CHEMISTRY_REACTION_IDS},
    "chemistry.reagent.legacy_unknown.name": (),
    "chemistry.ingredient.description": (),
    "chemistry.provenance.initial_flask": (),
    "chemistry.provenance.distilled": ("flask",),
    "chemistry.fill.carried": (),
    "chemistry.fill.unavailable": (),
    "chemistry.fill.result": ("measures", "reagent", "flask", "contents"),
    "chemistry.fill.reaction_potential": ("reactions",),
    "chemistry.distill.source": (),
    "chemistry.distill.requirement": (),
    "chemistry.distill.pack": (),
    "chemistry.distill.result": ("courier", "reagent", "flask"),
    "chemistry.pour.carried": (),
    "chemistry.pour.range": (),
    "chemistry.pour.ground": (),
    "chemistry.pour.capacity": (),
    "chemistry.pour.result": ("courier", "flask", "x", "y"),
    "chemistry.pour.prediction": ("reactions",),
    "chemistry.drink.carried": (),
    "chemistry.drink.unknown": (),
    "chemistry.drink.dangerous": (),
    "chemistry.drink.result": ("courier", "reactions", "flask"),
    "chemistry.status.clear_breath.cause": (),
    "chemistry.status.clear_breath.consequence": (),
    "chemistry.overlay.flasks.title": (),
    "chemistry.overlay.flasks.row": ("index", "flask", "contents", "measures"),
    "chemistry.overlay.flasks.journal": ("practiced", "household"),
    "chemistry.overlay.flasks.guidance": (),
    "chemistry.overlay.flasks.return": (),
    "chemistry.overlay.distill.title": (),
    "chemistry.overlay.distill.status": ("flask", "contents"),
    "chemistry.overlay.distill.option": ("index", "reagent"),
    "chemistry.overlay.distill.return": (),
    "chemistry.overlay.fill.title": (),
    "chemistry.overlay.fill.option": ("index", "item", "reagent", "quantity"),
    "chemistry.overlay.fill.pages": ("page", "pages"),
    "chemistry.overlay.pour.title": (),
    "chemistry.overlay.drink.title": (),
    "chemistry.overlay.target": ("x", "y", "z"),
    "chemistry.overlay.flask_prediction": ("index", "flask", "contents", "reactions"),
    "chemistry.overlay.empty": (),
    "chemistry.overlay.none": (),
    "chemistry.overlay.none_yet": (),
    "chemistry.overlay.no_longer_carried": (),
}


# Recipe IDs, station IDs, inputs, outputs, yields, and work-order state are
# engine-owned.  These semantic slots select only the words shown for them.
_PRODUCTION_RECIPE_IDS = (
    'field-flask',
    'iron-billet',
    'field-dressing',
    'smoke-bombs',
    'pitch-bombs',
    'lime-bombs',
    'brine-bombs',
    'thunder-bombs',
    'resin-bombs',
    'healing-draft',
    'attunement-draft',
    'breath-tonic',
    'counted-charges',
    'hearthford-splints',
    'greywash-gauntlets',
    'greenwold-vest',
    'whitecairn-sleeves',
    'dunmire-pattens',
    'marlbank-apron',
    'rillscar-cleats',
    'frostmere-coat',
    'fletched-arrows',
    'quarrel-case',
    'sling-shot-pouch',
    'circuit-trace',
    'circuit-via',
    'circuit-rack',
    'circuit-cell',
    'circuit-switch',
    'circuit-lamp',
    'circuit-gate',
    'circuit-drain',
    'circuit-sensor',
    'circuit-relay',
    'circuit-counter',
    'circuit-piston',
    'circuit-crate',

)
_PRODUCTION_STATION_IDS = ("portable", "workshop", "forge", "smelter", "still", "brewery", "gunworks")
_PRODUCTION_TEMPLATE_CONTRACT = {
    **{f"production.recipe.{recipe_id}.name": () for recipe_id in _PRODUCTION_RECIPE_IDS},
    **{f"production.station.{station_id}.name": () for station_id in _PRODUCTION_STATION_IDS},
    "production.recipe.fabricate": ("item",),
    "production.status.station": ("station",),
    "production.status.empty_flask": (),
    "production.status.needs": ("inputs",),
    "production.status.ready": (),
    "production.make.unknown": (),
    "production.make.space": (),
    "production.make.result": ("courier", "recipe", "station"),
    "production.provenance.work": ("courier", "station"),
    "production.provenance.masterwork": ("work",),
    "production.gather.invalid": (),
    "production.gather.exhausted": (),
    "production.gather.pack": (),
    "production.gather.provenance": ("region",),
    "production.gather.result": ("courier", "reagent", "stock"),
    "production.delegate.location": (),
    "production.delegate.competency": (),
    "production.delegate.capacity": (),
    "production.delegate.recipe": (),
    "production.delegate.credit": (),
    "production.delegate.result": ("worker", "recipe"),
    "production.provenance.order": ("worker",),
    "production.shift.supported": (),
    "production.shift.unsupported": (),
    "production.shift.default_institution": (),
    "production.shift.record": ("day", "stock", "institution", "status"),
    "production.order.record": ("day", "recipe", "worker", "region", "x", "y"),
    "production.overlay.catalog.title": (),
    "production.overlay.catalog.stations": ("stations", "page", "pages"),
    "production.overlay.catalog.recipe": ("index", "recipe", "quantity", "output", "status"),
    "production.overlay.catalog.requirements": ("requirements",),
    "production.overlay.catalog.requirement_item": ("quantity", "item", "held"),
    "production.overlay.catalog.guidance": (),
    "production.overlay.catalog.gather": ("first", "second", "stock"),
}


# Spell IDs, effects, costs, ranges, targets, and durations are engine-owned.
# These slots select only selected-pack rendering for magic results.
_MAGIC_SPELL_IDS = ('ember-spark', 'rain-bead', 'mender-thread', 'wind-nudge', 'smoke-call', 'salt-scour', 'ice-lace', 'ash-shot', 'stone-stitch', 'hearth-ward', 'clear-breath', 'lime-haze', 'echo-decoy', 'quiet-veil', 'river-pull', 'reed-snare', 'ember-sweep', 'storm-chord', 'deep-wash', 'iron-echo', 'fireball', 'frostbolt', 'lightning-bolt', 'magic-missile')
_MAGIC_EFFECT_IDS = ('fire', 'water', 'heal', 'push', 'smoke', 'salt', 'ice', 'blunt', 'support', 'ward', 'cleanse', 'lime', 'decoy', 'quiet', 'pull', 'bind', 'thunder', 'pierce')
_MAGIC_TEMPLATE_CONTRACT = {
    **{f"magic.spell.{spell_id}.name": () for spell_id in _MAGIC_SPELL_IDS},
    **{f"magic.spell.{spell_id}.description": ("effect", "power", "radius", "cost", "reach") for spell_id in _MAGIC_SPELL_IDS},
    **{f"magic.effect.{effect}.name": () for effect in _MAGIC_EFFECT_IDS},
    "magic.status.unlearned": (), "magic.status.tavern": (),
    "magic.status.mana": ("cost", "mana"), "magic.status.self": (),
    "magic.status.range": ("reach",), "magic.status.enemy": (), "magic.status.ready": (),
    "magic.cast.invalid_ground": (), "magic.cast.material_budget": (),
    "magic.cast.detail.heal": ("before", "after"), "magic.cast.detail.ward": (),
    "magic.cast.detail.cleanse": ("statuses",), "magic.cast.detail.no_exposure": (),
    "magic.cast.detail.quiet": ("duration",), "magic.cast.detail.enemy": ("target", "x", "y"),
    "magic.cast.detail.cell": ("count", "x", "y"),
    "magic.cast.result": ("courier", "spell", "cost", "detail"),
    "magic.status.clear_breath.cause": (), "magic.status.clear_breath.consequence": (),
    "magic.status.quiet_veil.cause": (), "magic.status.quiet_veil.consequence": (),
    "intent.magic.push": (), "intent.magic.pull": (), "intent.magic.bind": (),
    "intent.magic.defeated": ("spell", "location"),
    "magic.rest.berth.invalid": (), "magic.rest.full": (),
    "magic.rest.berth.result": ("courier", "mana"),
    "magic.shrine.invalid": (), "magic.shrine.entrance": (), "magic.shrine.threat": (),
    "magic.shrine.used": (), "magic.shrine.result": ("courier", "before", "restored"),
    "magic.target.detail": ("distance", "reach", "z", "mana", "max_mana", "cost"),
    "magic.target.header": ("spell", "description"),
    "magic.target.legality": ("status", "reason"), "magic.target.ready": (),
    "magic.target.blocked": (), "magic.target.observed": ("target", "intent"), "magic.target.empty": (),
    "magic.spellbook.title": (), "magic.spellbook.summary": ("courier", "mana", "max_mana"),
    "magic.spellbook.tier": ("index", "discipline", "learned"), "magic.spellbook.guidance": (),
    "magic.spellbook.row": ("index", "spell", "status", "description"),
    "magic.spellbook.status.ready": (), "magic.spellbook.status.unlearned": (),
    "magic.spellbook.tier.guidance": (), "magic.selection.unlearned": (),
}


# Skill/practice/manoeuvre IDs remain engine-owned; these slots only render them.
_PROGRESSION_TEMPLATE_CONTRACT = {'progression.branch.blades.name': (), 'progression.node.edge-measure.name': (), 'progression.node.edge-measure.description': (), 'progression.node.guard-feint.name': (), 'progression.node.guard-feint.description': (), 'progression.node.slip-cut.name': (), 'progression.node.slip-cut.description': (), 'progression.node.weapon-bind.name': (), 'progression.node.weapon-bind.description': (), 'progression.node.riposte.name': (), 'progression.node.riposte.description': (), 'progression.node.river-duelist.name': (), 'progression.node.river-duelist.description': (), 'progression.branch.reach.name': (), 'progression.node.measured-stance.name': (), 'progression.node.measured-stance.description': (), 'progression.node.countercharge.name': (), 'progression.node.countercharge.description': (), 'progression.node.haft-breaker.name': (), 'progression.node.haft-breaker.description': (), 'progression.node.hook-haul.name': (), 'progression.node.hook-haul.description': (), 'progression.node.line-intercept.name': (), 'progression.node.line-intercept.description': (), 'progression.node.ferryman-wall.name': (), 'progression.node.ferryman-wall.description': (), 'progression.branch.bows.name': (), 'progression.node.sighted-draw.name': (), 'progression.node.sighted-draw.description': (), 'progression.node.quick-nock.name': (), 'progression.node.quick-nock.description': (), 'progression.node.shaft-recovery.name': (), 'progression.node.shaft-recovery.description': (), 'progression.node.called-shot.name': (), 'progression.node.called-shot.description': (), 'progression.node.wind-hold.name': (), 'progression.node.wind-hold.description': (), 'progression.node.moving-volley.name': (), 'progression.node.moving-volley.description': (), 'progression.branch.gunworks.name': (), 'progression.node.charge-handling.name': (), 'progression.node.charge-handling.description': (), 'progression.node.dry-load.name': (), 'progression.node.dry-load.description': (), 'progression.node.braced-tube.name': (), 'progression.node.braced-tube.description': (), 'progression.node.vent-care.name': (), 'progression.node.vent-care.description': (), 'progression.node.smoke-shaping.name': (), 'progression.node.smoke-shaping.description': (), 'progression.node.payload-master.name': (), 'progression.node.payload-master.description': (), 'progression.branch.devices.name': (), 'progression.node.safe-throw.name': (), 'progression.node.safe-throw.description': (), 'progression.node.scatter-bank.name': (), 'progression.node.scatter-bank.description': (), 'progression.node.delayed-fuse.name': (), 'progression.node.delayed-fuse.description': (), 'progression.node.adhesive-coat.name': (), 'progression.node.adhesive-coat.description': (), 'progression.node.line-trap.name': (), 'progression.node.line-trap.description': (), 'progression.node.controlled-chain.name': (), 'progression.node.controlled-chain.description': (), 'progression.branch.spellcraft.name': (), 'progression.node.attunement.name': (), 'progression.node.attunement.description': (), 'progression.node.elemental-shape.name': (), 'progression.node.elemental-shape.description': (), 'progression.node.ward-script.name': (), 'progression.node.ward-script.description': (), 'progression.node.veiling.name': (), 'progression.node.veiling.description': (), 'progression.node.echo-binding.name': (), 'progression.node.echo-binding.description': (), 'progression.node.spell-weave.name': (), 'progression.node.spell-weave.description': (), 'progression.branch.smithing.name': (), 'progression.node.tool-care.name': (), 'progression.node.tool-care.description': (), 'progression.node.fuel-husbandry.name': (), 'progression.node.fuel-husbandry.description': (), 'progression.node.bloom-sorting.name': (), 'progression.node.bloom-sorting.description': (), 'progression.node.armour-fitting.name': (), 'progression.node.armour-fitting.description': (), 'progression.node.gun-assembly.name': (), 'progression.node.gun-assembly.description': (), 'progression.node.masterwork.name': (), 'progression.node.masterwork.description': (), 'progression.branch.alchemy.name': (), 'progression.node.substance-sense.name': (), 'progression.node.substance-sense.description': (), 'progression.node.safe-decant.name': (), 'progression.node.safe-decant.description': (), 'progression.node.field-triage.name': (), 'progression.node.field-triage.description': (), 'progression.node.controlled-distil.name': (), 'progression.node.controlled-distil.description': (), 'progression.node.antitoxin.name': (), 'progression.node.antitoxin.description': (), 'progression.node.catalyst-brewing.name': (), 'progression.node.catalyst-brewing.description': (), 'progression.branch.navigation.name': (), 'progression.node.route-reading.name': (), 'progression.node.route-reading.description': (), 'progression.node.weather-eye.name': (), 'progression.node.weather-eye.description': (), 'progression.node.load-balance.name': (), 'progression.node.load-balance.description': (), 'progression.node.current-rescue.name': (), 'progression.node.current-rescue.description': (), 'progression.node.station-repair.name': (), 'progression.node.station-repair.description': (), 'progression.node.deep-pilotage.name': (), 'progression.node.deep-pilotage.description': (), 'progression.branch.diplomacy.name': (), 'progression.node.careful-terms.name': (), 'progression.node.careful-terms.description': (), 'progression.node.price-sense.name': (), 'progression.node.price-sense.description': (), 'progression.node.mediation.name': (), 'progression.node.mediation.description': (), 'progression.node.teaching.name': (), 'progression.node.teaching.description': (), 'progression.node.work-order.name': (), 'progression.node.work-order.description': (), 'progression.node.guild-broker.name': (), 'progression.node.guild-broker.description': (), 'progression.practice.bank_water_cadence.name': (), 'progression.practice.bank_water_cadence.description': (), 'progression.practice.field_rill_measure.name': (), 'progression.practice.field_rill_measure.description': (), 'progression.practice.wreck_title_hold.name': (), 'progression.practice.wreck_title_hold.description': (), 'progression.practice.span_watch_stance.name': (), 'progression.practice.span_watch_stance.description': (), 'progression.practice.ash_refuge_breathing.name': (), 'progression.practice.ash_refuge_breathing.description': (), 'progression.practice.island_porter_relay.name': (), 'progression.practice.island_porter_relay.description': (), 'progression.practice.ridge_sounding_line.name': (), 'progression.practice.ridge_sounding_line.description': (), 'progression.practice.winter_braid_reading.name': (), 'progression.practice.winter_braid_reading.description': (), 'progression.practice.siltgate_hand.name': (), 'progression.practice.siltgate_hand.description': (), 'progression.practice.ebb_beacon_watch.name': (), 'progression.practice.ebb_beacon_watch.description': (), 'progression.practice.living_firebreak.name': (), 'progression.practice.living_firebreak.description': (), 'progression.practice.honest_stair_breath.name': (), 'progression.practice.honest_stair_breath.description': (), 'progression.practice.peat_brace_seating.name': (), 'progression.practice.peat_brace_seating.description': (), 'progression.practice.two_span_withdrawal.name': (), 'progression.practice.two_span_withdrawal.description': (), 'progression.practice.seed_clay_tread.name': (), 'progression.practice.seed_clay_tread.description': (), 'progression.practice.thaw_net_recovery.name': (), 'progression.practice.thaw_net_recovery.description': (), 'progression.manoeuvre.braced-advance.name': (), 'progression.manoeuvre.braced-advance.setup': (), 'progression.manoeuvre.braced-advance.counter': (), 'progression.manoeuvre.braced-advance.effect': (), 'progression.manoeuvre.quiet-crossing.name': (), 'progression.manoeuvre.quiet-crossing.setup': (), 'progression.manoeuvre.quiet-crossing.counter': (), 'progression.manoeuvre.quiet-crossing.effect': (), 'progression.manoeuvre.hook-and-pass.name': (), 'progression.manoeuvre.hook-and-pass.setup': (), 'progression.manoeuvre.hook-and-pass.counter': (), 'progression.manoeuvre.hook-and-pass.effect': (), 'progression.manoeuvre.shield-bind.name': (), 'progression.manoeuvre.shield-bind.setup': (), 'progression.manoeuvre.shield-bind.counter': (), 'progression.manoeuvre.shield-bind.effect': (), 'progression.manoeuvre.smoke-takedown.name': (), 'progression.manoeuvre.smoke-takedown.setup': (), 'progression.manoeuvre.smoke-takedown.counter': (), 'progression.manoeuvre.smoke-takedown.effect': (), 'progression.manoeuvre.porter-shove.name': (), 'progression.manoeuvre.porter-shove.setup': (), 'progression.manoeuvre.porter-shove.counter': (), 'progression.manoeuvre.porter-shove.effect': (), 'progression.manoeuvre.high-cast.name': (), 'progression.manoeuvre.high-cast.setup': (), 'progression.manoeuvre.high-cast.counter': (), 'progression.manoeuvre.high-cast.effect': (), 'progression.manoeuvre.ice-feint.name': (), 'progression.manoeuvre.ice-feint.setup': (), 'progression.manoeuvre.ice-feint.counter': (), 'progression.manoeuvre.ice-feint.effect': (), 'progression.manoeuvre.flood-turn.name': (), 'progression.manoeuvre.flood-turn.setup': (), 'progression.manoeuvre.flood-turn.counter': (), 'progression.manoeuvre.flood-turn.effect': (), 'progression.manoeuvre.firebreak-cut.name': (), 'progression.manoeuvre.firebreak-cut.setup': (), 'progression.manoeuvre.firebreak-cut.counter': (), 'progression.manoeuvre.firebreak-cut.effect': (), 'progression.manoeuvre.support-set.name': (), 'progression.manoeuvre.support-set.setup': (), 'progression.manoeuvre.support-set.counter': (), 'progression.manoeuvre.support-set.effect': (), 'progression.manoeuvre.controlled-withdrawal.name': (), 'progression.manoeuvre.controlled-withdrawal.setup': (), 'progression.manoeuvre.controlled-withdrawal.counter': (), 'progression.manoeuvre.controlled-withdrawal.effect': (), 'progression.skill.buy.invalid': (), 'progression.skill.buy.already': ('courier', 'node'), 'progression.skill.buy.parents': ('node', 'parents'), 'progression.skill.buy.points': (), 'progression.skill.buy.learned': ('courier', 'node', 'description'), 'progression.skill.milestone': ('courier', 'milestone'), 'progression.skill.teach.invalid': (), 'progression.skill.teach.distance': (), 'progression.skill.teach.known': (), 'progression.skill.teach.limit': (), 'progression.skill.teach.parents': (), 'progression.skill.teach.result': ('teacher', 'node', 'recipient'), 'progression.journal.write.location': (), 'progression.journal.write.choice': (), 'progression.journal.write.paper': (), 'progression.journal.write.space': (), 'progression.journal.write.result': ('courier', 'node', 'journal'), 'progression.journal.study.location': (), 'progression.journal.study.known': (), 'progression.journal.study.limit': (), 'progression.journal.study.parents': (), 'progression.journal.study.result': ('courier', 'node', 'journal'), 'progression.practice.network.unavailable': (), 'progression.practice.network.already': ('courier', 'practice'), 'progression.practice.network.learned': ('courier', 'practice', 'contact', 'description'), 'progression.practice.network.result': ('courier', 'practice', 'description'), 'progression.practice.aftermath.learned': ('courier', 'practice', 'description'), 'progression.manoeuvre.status.unlearned': ('practice',), 'progression.manoeuvre.status.danger': (), 'progression.manoeuvre.status.ready': (), 'progression.manoeuvre.lines.none': (), 'progression.manoeuvre.lines.guidance': (), 'progression.manoeuvre.lines.intro': (), 'progression.manoeuvre.lines.row': ('index', 'manoeuvre', 'status'), 'progression.manoeuvre.lines.effect': ('effect', 'counter'), 'progression.manoeuvre.lines.footer': (), 'progression.manoeuvre.perform.failed': ('manoeuvre', 'reason'), 'progression.manoeuvre.perform.result': ('manoeuvre', 'effect'), 'progression.manoeuvre.memory': ('courier', 'manoeuvre', 'effect'), 'progression.overlay.skill.title': (), 'progression.overlay.skill.summary': ('courier', 'points', 'learned'), 'progression.overlay.skill.branch': ('index', 'branch', 'learned'), 'progression.overlay.skill.guidance': (), 'progression.overlay.branch.summary': ('courier', 'points'), 'progression.overlay.branch.row': ('index', 'node', 'status', 'description'), 'progression.overlay.status.learned': (), 'progression.overlay.status.needs': ('parents',), 'progression.overlay.status.ready': (), 'progression.overlay.status.point': (), 'progression.overlay.mastery.title': (), 'progression.target.mastery': ('manoeuvre', 'effect', 'status'), 'progression.target.ready': (), 'progression.target.needs': ('reason',)}


_PROGRESSION_TEMPLATE_CONTRACT.update({"progression.personal.name": ("role",), "progression.technique.mill_hearing.name": (), "progression.technique.shoreline_measure.name": (), "progression.technique.smoke_spoor.name": (), "progression.technique.bell_interval.name": ()})


# Raw weapon and fitting kinds remain engine-owned inventory/catalog identities.
# This contract only supplies labels, descriptions, and result wording for them.
_EQUIPMENT_WORK_WEAPON_IDS = (
    "pot sling", "throwing axe", "forked pike", "war flail", "spade",
    "shield and hanger", "glaive", "pollaxe", "arming sword", "long knife",
    "boat hook", "flanged mace", "estoc", "felling axe", "quarterstaff",
    "reed sickle", "anchor fluke", "chain hook",
)
_EQUIPMENT_ARSENAL_IDS = (
    "river sabre", "reed cleaver", "court rapier", "crescent knife", "watch backsword",
    "hooked falchion", "river partisan", "three-prong trident", "coppice halberd",
    "recurved naginata", "ferry lance", "iron-shod pole", "quarry morningstar",
    "two-hand maul", "watch sap", "ore pick", "smith's hammer", "knotted club",
    "reed shortbow", "laminated recurve", "horn composite bow", "broadhead hunting bow",
    "war yew bow", "line-caster bow", "matchlock arquebus", "deck swivel gun",
    "fowling piece", "braced long gun", "watch carbine", "signal pistol",
    "smoke bomb kit", "pitch bomb kit", "lime bomb kit", "brine bomb kit",
    "thunder bomb kit", "resin bomb kit",
)
_EQUIPMENT_FAMILY_IDS = ("blade", "reach", "impact", "bow", "gun", "device")
_EQUIPMENT_EFFECT_IDS = ("aim", "armour", "bind", "blunt", "brine", "charge", "cut", "guard", "interrupt", "lime", "morale", "pierce", "pitch", "pull", "push", "quick", "reeds", "resin", "smoke", "sweep", "thunder", "timber")
_EQUIPMENT_FITTING_IDS = (
    "iron heel", "quiet binding", "retrieval cord", "resin seal", "ash wrap",
    "wool lining", "reed lining", "iron scales",
)
_EQUIPMENT_TEMPLATE_CONTRACT = {
    **{f"equipment.weapon.work.{weapon.replace(' ', '_')}.name": () for weapon in _EQUIPMENT_WORK_WEAPON_IDS},
    **{f"equipment.weapon.work.{weapon.replace(' ', '_')}.description": () for weapon in _EQUIPMENT_WORK_WEAPON_IDS},
    **{f"equipment.weapon.arsenal.{weapon.replace(' ', '_')}.name": () for weapon in _EQUIPMENT_ARSENAL_IDS},
    "equipment.weapon.arsenal.description": ("family", "minimum", "reach", "damage", "effects", "ammunition"),
    "equipment.weapon.arsenal.ammunition": ("ammunition",),
    **{f"equipment.effect.{effect}.name": () for effect in _EQUIPMENT_EFFECT_IDS},
    **{f"equipment.family.{family}.name": () for family in _EQUIPMENT_FAMILY_IDS},
    **{f"equipment.fitting.{fitting.replace(' ', '_')}.name": () for fitting in _EQUIPMENT_FITTING_IDS},
    **{f"equipment.fitting.{fitting.replace(' ', '_')}.effect": () for fitting in _EQUIPMENT_FITTING_IDS},
    **{f"equipment.fitting.{fitting.replace(' ', '_')}.drawback": () for fitting in _EQUIPMENT_FITTING_IDS},
    "equipment.pot.invalid_range": ("reach",), "equipment.pot.missing": (), "equipment.pot.no_cell": (),
    "equipment.pot.pitch.wet": (), "equipment.pot.pitch.dry": (), "equipment.pot.lime": (), "equipment.pot.brine": (), "equipment.pot.fitting": ("fitting",),
    "equipment.approach.load": (), "equipment.approach.level": (), "equipment.approach.blocked": (), "equipment.approach.wet": (),
    "equipment.strike.forked_pike": ("target",), "equipment.strike.forked_pike.pair": ("target", "across"), "equipment.strike.war_flail": ("count",),
    "equipment.strike.spade.dust": (), "equipment.strike.spade.none": (), "equipment.strike.throwing_axe.timber": (), "equipment.strike.throwing_axe.ground": (),
    "equipment.strike.shield_hanger": (), "equipment.strike.glaive.clip": ("target",), "equipment.strike.glaive.none": (),
    "equipment.strike.pollaxe.armoured": (), "equipment.strike.pollaxe.unarmoured": (), "equipment.strike.pollaxe.support": (),
    "equipment.strike.arming_sword": (), "equipment.strike.long_knife.interrupt": (), "equipment.strike.long_knife.normal": (),
    "equipment.strike.boat_hook": ("moved", "suffix"), "equipment.strike.flanged_mace": (), "equipment.strike.estoc.armoured": (), "equipment.strike.estoc.unarmoured": (),
    "equipment.strike.felling_axe.cut": (), "equipment.strike.felling_axe.none": (), "equipment.strike.quarterstaff": (),
    "equipment.strike.reed_sickle.cut": (), "equipment.strike.reed_sickle.none": (), "equipment.strike.anchor_fluke.anchored": (), "equipment.strike.anchor_fluke.normal": (), "equipment.strike.chain_hook": (),
    **{f"equipment.ammunition.{ammunition.replace(' ', '_')}.name": () for ammunition in ("arrows", "bolts", "heavy bolts", "shot", "handgonne charges", "pitch pots", "lime pots", "brine pots", "smoke bombs", "pitch bombs", "lime bombs", "brine bombs", "thunder bombs", "resin bombs")},
    "equipment.arsenal.bomb.description": (), "equipment.arsenal.device.range": (), "equipment.arsenal.device.cell": (), "equipment.arsenal.device.material_budget": (), "equipment.arsenal.ammunition.none": ("ammunition",), "equipment.arsenal.ammunition.none_short": ("ammunition",),
    "equipment.arsenal.device.result": ("courier", "ammunition", "x", "y", "effect", "warning", "sound"), "equipment.arsenal.device.warning": (),
    "equipment.arsenal.no_target": (), "equipment.arsenal.gun.loading": ("weapon", "remaining"), "equipment.arsenal.aim.prepare": ("courier", "weapon", "target"),
    "equipment.arsenal.aim.gun_weather": (), "equipment.arsenal.aim.bow_weather": (), "equipment.arsenal.result.defeat": ("weapon", "target", "recovered"),
    "equipment.arsenal.result.hit": ("weapon", "damage", "kind", "target", "health", "maximum"),
    "equipment.arsenal.memory": ("courier", "outcome", "target", "weapon"), "equipment.provenance.fitting_installed": ("target",), "equipment.provenance.fitting_kit": (), "equipment.provenance.tethered_throw": (), "equipment.provenance.enemy_weapon": ("issue", "actor"), "equipment.provenance.enemy_armour": ("region", "actor"), "equipment.provenance.enemy_torso": ("actor",),
    "equipment.workshop.compatible": (), "equipment.workshop.parent_damaged": (), "equipment.workshop.socket_occupied": ("slot",), "equipment.workshop.stock_empty": (),
    "equipment.workshop.credit": ("cost",), "equipment.workshop.ready": (), "equipment.workshop.install.location": (), "equipment.workshop.install.result": ("fitting", "item", "cost", "effect"),
    "equipment.workshop.remove.location": (), "equipment.workshop.remove.requirements": (), "equipment.workshop.remove.pack": (), "equipment.workshop.remove.result": ("fitting",),
    "equipment.workshop.buy.location": (), "equipment.workshop.buy.stock": (), "equipment.workshop.buy.pack": (), "equipment.workshop.buy.result": ("fitting", "cost"),
    "equipment.workshop.repair.invalid": (), "equipment.workshop.repair.credit": (), "equipment.workshop.repair.result": ("item", "condition"),
    "equipment.workshop.describe.item": ("item", "condition", "cut", "pierce", "blunt", "coverage"), "equipment.workshop.describe.part": ("fitting", "condition", "slot"), "equipment.workshop.describe.guidance": (),
    "equipment.fitting.quiet_binding": (), "equipment.fitting.resin_seal": (), "equipment.fitting.ash_wrap": (), "equipment.fitting.retrieval.recovered": (), "equipment.fitting.retrieval.ground": (),
    "equipment.enemy.protection.broken": (), "equipment.enemy.drop": ("items",), "equipment.enemy.recover.intent": ("weapon",), "equipment.enemy.recover.result": ("threat", "intent"),
    "intent.pinned.fork": (), "intent.pinned.fork_line": (), "intent.disrupted.long_knife": (), "intent.disrupted.boat_hook": (), "intent.dazed.flanged_mace": (), "intent.disrupted.quarterstaff": (), "intent.disrupted.anchor_fluke": (), "intent.entangled.chain": (), "intent.entangled.resin": (), "intent.dazed.lime": (), "intent.disrupted.equipment_interrupt": (), "intent.entangled.equipment_bind": (), "intent.defeated.removed": (), "intent.defeated.equipment": ("source", "location"), "intent.recovery.weapon_broken": (), "intent.recovery.weapon_recovered": ("weapon",),
    "equipment.target.pot": (), "equipment.target.device": ("bomb",), "equipment.target.gun_loading": (), "equipment.target.aim.ready": (), "equipment.target.aim.prepare": (),
    "equipment.overlay.slot": ("slot", "item"), "equipment.overlay.slot.empty": (), "equipment.overlay.slot.requirement": (), "equipment.overlay.buy.choice": (), "equipment.overlay.store.row": ("fitting", "price", "stock"), "equipment.overlay.preview": ("fitting", "cost"), "equipment.overlay.remove.choice": ("slot",), "equipment.overlay.repair.choice": (), "equipment.overlay.repair.requirement": (), "equipment.overlay.confirm.choice": (), "equipment.overlay.back.choice": (),
    "equipment.overlay.station.title": (), "equipment.overlay.station.summary": ("credit",), "equipment.overlay.station.guidance": (), "equipment.overlay.store.title": (),
    "equipment.overlay.store.summary": (), "equipment.overlay.store.stock": (), "equipment.overlay.work.title": (), "equipment.overlay.empty": (), "equipment.overlay.item.missing": (), "equipment.overlay.buy.title": (), "equipment.overlay.buy.detail": ("fitting", "effect", "drawback", "width", "height", "weight", "price"),
    "equipment.overlay.fit.title": (), "equipment.overlay.fit.detail": ("fitting", "item", "effect", "drawback", "weight", "cost"), "equipment.overlay.confirm.title": (),
    "equipment.overlay.confirm.guidance": (), "equipment.overlay.confirm.costs": (),
}

_PREPARATION_IDS = (
    "preparation.waterline", "preparation.weapon-repair", "preparation.storm-light",
    "preparation.item-recovery", "preparation.fire-blanket", "preparation.firebrand",
    "preparation.high-sounding", "preparation.footing", "preparation.bank-plug",
    "preparation.ration", "preparation.bridge-dogs", "preparation.aim-break",
    "preparation.drain-tile", "preparation.kiln-sand", "preparation.ice-peg",
    "preparation.thaw",
)
_PREPARATION_RESULT_PLACEHOLDERS = {
    "waterline": ("changed",), "weapon-repair": ("weapon", "before", "condition"),
    "storm-light": ("before", "oil", "cleared"), "item-recovery": ("item",),
    "fire-blanket": ("changed",), "firebrand": ("material", "coordinate"),
    "high-sounding": ("cache",), "footing": ("statuses",),
    "bank-plug": ("material", "coordinate", "before", "after"),
    "ration": ("status", "before", "health"), "bridge-dogs": ("supports",),
    "aim-break": ("targets", "before", "noise"), "drain-tile": ("coordinate",),
    "kiln-sand": ("coordinate", "before", "fuel"), "ice-peg": ("coordinate",),
    "thaw": ("cells", "heat"),
}
_PREPARATION_TEMPLATE_CONTRACT = {
    **{f"{preparation}.name": () for preparation in _PREPARATION_IDS},
    **{f"{preparation}.description": () for preparation in _PREPARATION_IDS},
    **{f"{preparation}.condition": () for preparation in _PREPARATION_IDS},
    "preparation.status.not_carried": (), "preparation.item-recovery.pack_full": (), "preparation.apply.unavailable": ("preparation", "condition"),
    **{f"preparation.result.{mode}": placeholders for mode, placeholders in _PREPARATION_RESULT_PLACEHOLDERS.items()},
    "preparation.result.thaw.lamp": (), "preparation.result.thaw.fire": (),
    "preparation.record.high-sounding": ("cache", "x", "y", "level"),
    "preparation.apply.success": ("preparation", "detail"), "preparation.apply.memory": ("courier", "preparation", "detail"),
    "preparation.grant.provenance": ("topology",), "preparation.grant.packed": (), "preparation.grant.ground": (),
    "preparation.grant.memory": ("courier", "preparation", "topology", "where"), "preparation.grant.result": ("preparation", "where"),
    "preparation.overlay.choice": ("preparation",), "preparation.overlay.row": ("key", "preparation", "description", "status"),
    "preparation.overlay.ready": (), "preparation.overlay.needs": ("condition",), "preparation.overlay.guidance": (),
    "intent.preparation.firebrand": (), "intent.preparation.aim-break": (),
}

_MATERIAL_IDS = ("reeds", "timber", "cloth", "resin", "oil", "charcoal", "soil", "stone", "lime", "ash", "salt")
_COATING_IDS = ("none", "salt", "lime", "ash", "resin", "oil", "wet", "glow")
_MATERIAL_HAZARD_IDS = ("fire", "debris", "smoke", "water", "salt", "lime", "ash")
_MATERIAL_VERB_IDS = ("ignite", "extinguish", "pour", "cut", "brace", "lever", "dig", "break", "push", "pull", "redirect")
_MATERIAL_TEMPLATE_CONTRACT = {
    **{f"material.name.{name}": () for name in _MATERIAL_IDS},
    **{f"material.coating.{name}": () for name in _COATING_IDS},
    **{f"material.hazard.{name}": () for name in _MATERIAL_HAZARD_IDS},
    **{f"material.verb.{name}": () for name in _MATERIAL_VERB_IDS},
    "material.fluid.fresh": (), "material.fluid.salt": (), "material.phase.ice": (), "material.phase.liquid": (),
    "material.status.smoke_inhalation.cause": (), "material.status.smoke_inhalation.consequence": (), "material.status.wet.cause": (), "material.status.wet.consequence": (), "material.status.chilled.cause": (), "material.status.chilled.consequence": (), "material.status.salt_grit.cause": (), "material.status.salt_grit.consequence": (), "material.status.lime_grit.cause": (), "material.status.lime_grit.consequence": (),
    "material.inspect.fact": ("x", "y", "z", "material", "coating"), "material.inspect.water": ("water", "fluid", "phase", "fire", "smoke"), "material.inspect.support.warning": ("support", "beats"), "material.inspect.support.clear": ("support",), "material.inspect.mixture": ("reagents", "reactions"), "material.inspect.prediction": (), "material.inspect.cover": (), "material.inspect.guidance": (),
    "material.item.destroyed.memory": ("item", "item_id", "reaction", "coordinate"), "material.item.destroyed.message": ("item", "reaction"), "intent.material.clear_ground": ("reaction",), "intent.material.break_prepared_lane": ("reaction",), "material.threat.fell": ("threat", "reaction", "coordinate"), "material.armour.heat": (), "material.person.injury.memory": ("reaction", "coordinate"),
    "material.water.quench": ("coordinate",), "material.ice.thaw": ("coordinate",), "material.fire.spread": ("material", "coordinate"), "material.reaction.result": ("reaction", "coordinate"), "material.collapse.warning": ("coordinate",), "material.collapse.memory": ("coordinate",), "material.collapse.result": ("coordinate",),
    "material.handle.invalid_target": (), "material.handle.ignite_requirement": (), "material.handle.tool_requirement": (), "material.handle.coffer_requirement": (), "material.handle.coffer_destination": (), "material.handle.water_requirement": (), "material.handle.no_cell": (), "material.handle.water_spent": (), "material.handle.structure_requirement": (), "material.handle.result": ("verb", "material", "coordinate"), "material.handle.heel": (),
    "material.inspection.blocked.stone": (), "material.inspection.remedy.stone": (), "material.inspection.blocked.timber": (), "material.inspection.remedy.timber": (), "material.inspection.blocked.water": (), "material.inspection.remedy.water": (), "material.inspection.blocked.empty": (), "material.inspection.remedy.empty": (), "material.inspection.blocked.vessel": (), "material.inspection.remedy.vessel": (), "material.inspection.blocked.generic": ("terrain",), "material.inspection.remedy.generic": (),
    "material.inspection.preview.nonadjacent": (), "material.inspection.preview.watching": ("actor",), "material.inspection.preview.watching_remedy": (), "material.inspection.preview.engaged": ("actor",), "material.inspection.preview.engaged_remedy": (), "material.inspection.preview.diagonal": (), "material.inspection.preview.diagonal_remedy": (), "material.inspection.preview.status": ("status", "cause", "effect"), "material.inspection.preview.fire": ("fire",), "material.inspection.preview.smoke": ("smoke",), "material.inspection.preview.collapse": ("beats",), "material.inspection.preview.water": ("fluid", "water"), "material.inspection.preview.door": (), "material.inspection.preview.fall": (), "material.inspection.preview.enter": ("terrain",), "material.inspection.preview.avoid": (),
    "material.inspection.unknown": ("x", "y", "z"), "material.inspection.unknown.detail": (), "material.inspection.no_time": (), "material.inspection.line": ("prefix", "x", "y", "z", "terrain", "tile"), "material.inspection.remembered.detail": (), "material.inspection.remembered.return": (), "material.inspection.actor": ("actor", "health", "maximum", "morale", "intent"), "material.inspection.trace": ("clue",), "material.inspection.container": ("container", "state"), "material.inspection.ground": ("items",), "material.inspection.transition": ("direction", "z"), "material.inspection.prediction": ("legal", "time", "consequence"), "material.inspection.counter": ("remedy",), "material.inspection.footer": (), "material.inspection.prefix.visible": (), "material.inspection.prefix.remembered": (), "material.inspection.time.none": (), "material.inspection.time.one": (), "material.inspection.time.possibly_two": (), "material.inspection.container.opened": (), "material.inspection.container.closed": (), "material.inspection.direction.descend": (), "material.inspection.direction.climb": (), "material.inspection.legal": (), "material.inspection.blocked": (), "material.inspection.circuit": ("fitting", "phase", "detail"), "material.inspection.circuit.rack": ("charge",), "material.inspection.circuit.active": (), "material.inspection.fallback.obstruction": (), "material.inspection.fallback.worked_terrain": (), "material.inspection.fallback.passable_ground": (), "material.overlay.choice.guidance": (), "material.overlay.title": (),
    "material.inspection.hint.danger": ("actor",), "material.inspection.hint.manoeuvre": ("manoeuvre", "effect"), "material.inspection.hint.transition": ("direction", "z"), "material.inspection.hint.map": (), "material.inspection.hint.default": (),
    "material.inspection.advice.forecast": ("speaker", "actor", "counter"), "material.inspection.advice.status": ("speaker", "status", "cause", "consequence"), "material.inspection.advice.fire": ("speaker",), "material.inspection.advice.smoke": ("speaker",), "material.inspection.advice.collapse": ("speaker",), "material.inspection.advice.rain": ("speaker",), "material.inspection.advice.wind": ("speaker",), "material.inspection.advice.winter": ("speaker",), "material.inspection.advice.combo": ("speaker", "combo"), "material.inspection.advice.default": ("speaker",),
}
_SANCTUM_REGIONS = ("hearthford", "greywash", "greenwold", "whitecairn", "dunmire", "rillscar", "marlbank", "frostmere")
_SANCTUM_TEMPLATE_CONTRACT = {
    **{f"sanctum.{region}.{field}": () for region in _SANCTUM_REGIONS for field in ("name", "theme", "witness.name", "boss.name", "boss.capability", "boss.counterplay", "boss.goal")},
    "sanctum.link.entry": (), "sanctum.link.reliquary": (), "sanctum.link.gallery": (),
    "sanctum.cache.ward": ("sanctum",), "sanctum.cache.hoard": ("sanctum",),
    "sanctum.witness.existing": ("witness",), "sanctum.witness.unavailable": (), "sanctum.witness.memory": ("control", "sanctum"), "sanctum.witness.role": (), "sanctum.witness.schedule": (), "sanctum.witness.arrival": ("witness", "x", "y"),
    "sanctum.encounter.limit": (), "sanctum.encounter.occupied": (), "sanctum.encounter.none": (), "sanctum.encounter.arrival": ("actor", "x", "y", "goal"), "sanctum.enter.tier": ("sanctum",),
    "sanctum.undercroft.already": (), "sanctum.undercroft.no_space": (), "sanctum.undercroft.spawned": (), "sanctum.undercroft.contested": (), "sanctum.undercroft.opened": (),
    "sanctum.secret.none": (), "sanctum.secret.open": (), "sanctum.secret.opened": (),
    "sanctum.inspect.standing": ("account", "trust", "obligation"), "sanctum.inspect.no_witness": (), "sanctum.inspect.site": ("sanctum", "theme"), "sanctum.inspect.seal": ("seal", "keeper"), "sanctum.inspect.keeper": ("capability", "counterplay"), "sanctum.inspect.offer": (), "sanctum.inspect.breach": (), "sanctum.inspect.study": (), "sanctum.inspect.secret": ("x", "y"), "sanctum.inspect.aftermath": ("control",), "sanctum.inspect.shelter": (),
    "sanctum.choice.study": ("theme", "event"), "sanctum.choice.account_unavailable": (), "sanctum.choice.shelter_unavailable": (), "sanctum.choice.shelter_witness": (), "sanctum.choice.offered": (), "sanctum.choice.requirement": ("commodity",), "sanctum.choice.offering_memory": ("courier", "commodity", "sanctum"), "sanctum.choice.offering": ("commodity", "account"), "sanctum.choice.breach_unavailable": (), "sanctum.choice.breach_memory": ("courier", "sanctum"), "sanctum.choice.breach": ("account",), "sanctum.choice.invalid": (),
    "sanctum.event.none": (), "sanctum.control.unsettled": (), "sanctum.status.seal.open": (), "sanctum.status.seal.closed": (), "sanctum.status.keeper.defeated": (), "sanctum.status.keeper.unseen": (), "sanctum.status.keeper.present": (), "sanctum.record.courier.unknown": (),
    "sanctum.event.quiet_witnesses": ("sanctum",), "sanctum.event.parcel_unavailable": (), "sanctum.event.parcel_provenance": ("sanctum",), "sanctum.event.parcel": ("commodity", "x", "y"), "sanctum.event.stone": (), "sanctum.event.stone_safe": (),
    "sanctum.record.defeat": ("courier", "boss", "sanctum"), "sanctum.record.cleared": ("sanctum", "strategy"), "sanctum.record.strategy_gained": (), "sanctum.record.strategy_bound": (), "sanctum.record.strategy_none": (), "sanctum.record.claimants_left": ("sanctum", "control"),
    "sanctum.area.tier.one": (), "sanctum.area.tier.two": (), "sanctum.area.undercroft": (),
}

_PROGRESSION_TEMPLATE_CONTRACT.update({'progression.choice.journal.write.label': (), 'progression.choice.journal.write.requirement': (), 'progression.choice.journal.study.label': (), 'progression.choice.journal.study.requirement': (), 'progression.choice.manoeuvre.label': (), 'progression.choice.manoeuvre.requirement': (), 'progression.choice.teach.label': (), 'progression.station.gathering.journal_guidance': (), 'progression.person.learned': ('practices',), 'progression.person.none': (), 'progression.person.practice_effect': ('practice', 'description'), 'progression.person.personal_effect': (), 'progression.person.teach': (), 'progression.household.none': (), 'progression.household.personal_effect': (), 'progression.personal.memory': ('region',), 'progression.personal.development': ('courier', 'practice'), 'progression.character.learned_nodes': ('nodes',), 'progression.character.none': (), 'progression.combat.note.guard_feint': (), 'progression.combat.note.slip_cut': (), 'progression.combat.note.weapon_bind': (), 'progression.combat.note.riposte_pressure': (), 'progression.combat.note.duelist_finish': (), 'progression.combat.note.edge_measure_guard': (), 'progression.combat.note.countercharge': (), 'progression.combat.note.timber_chipped': (), 'progression.combat.note.hook_haul': (), 'progression.combat.note.line_intercepted': (), 'progression.combat.note.ferryman_guard': (), 'progression.combat.note.called_shot': (), 'progression.combat.note.shaft_falls': (), 'progression.combat.note.measured_charge': (), 'progression.combat.note.matched_payload': (), 'progression.combat.note.target_veiled': (), 'progression.combat.note.masterwork_edge': ()})
_PROGRESSION_TEMPLATE_CONTRACT.update({'progression.journal.write.title': (), 'progression.journal.write.summary': ('courier', 'written'), 'progression.journal.write.row': ('index', 'node', 'description'), 'progression.journal.write.page': ('page', 'pages'), 'progression.journal.write.provenance': ('courier',), 'progression.journal.study.title': (), 'progression.journal.study.summary': ('courier', 'inherited'), 'progression.journal.study.row': ('index', 'journal', 'node', 'provenance'), 'progression.journal.study.page': ('page', 'pages'), 'progression.teach.title': ('person',), 'progression.teach.summary': ('person', 'inherited', 'teacher'), 'progression.teach.row': ('index', 'node', 'parents'), 'progression.teach.none': (), 'progression.teach.page': ('page', 'pages')})
_PROGRESSION_TEMPLATE_CONTRACT.update({'progression.combat.shaft.provenance': ()})
_PROGRESSION_TEMPLATE_CONTRACT.update({'progression.choice.manoeuvre.guidance': (), 'progression.manoeuvre.unavailable': ()})

_ACTION_TEMPLATE_CONTRACT.update({'combat.attack.effect.partial_cover': (), 'combat.attack.effect.injury': (), 'combat.attack.effect.thorn': (), 'combat.attack.effect.billhook': (), 'combat.attack.effect.spear': (), 'combat.attack.effect.cudgel': (), 'combat.attack.effect.staff': (), 'combat.attack.effect.axe': (), 'combat.attack.effect.pike': (), 'combat.attack.effect.boar_spear': (), 'combat.attack.effect.knives': (), 'combat.attack.effect.hammer': (), 'combat.attack.effect.net': (), 'combat.attack.effect.net_bind': (), 'combat.attack.effect.net_recover': (), 'combat.attack.effect.hooked_javelin': (), 'combat.attack.effect.retrieval': (), 'combat.attack.effect.handgonne': (), 'combat.attack.effect.high_arc': (), 'combat.attack.effect.high_arc_daze': (), 'combat.attack.effect.smoke_braid': (), 'combat.attack.no_physical_ammunition': ('ammunition',), 'combat.guard.handgonne_loading': ('current', 'required', 'stage'), 'combat.guard.no_engaged': ()})

_ACTION_TEMPLATE_CONTRACT.update({'combat.intent.favors_an_injured_lower_limb_and_loses_ground': (), 'combat.intent.cuts_free_of_the_net_before_acting_again': (), 'combat.intent.cannot_claim_a_publicly_witnessed_and_dogged_sluice': (), 'combat.intent.cannot_claim_witnessed_wreck_property': (), 'combat.intent.cannot_close_the_dogged_tide_chain': (), 'combat.intent.hauls_the_tide_chain_the_three_marked_flats_flood_next_turn': (), 'combat.intent.will_not_burn_the_witnessed_medicine_stand': (), 'combat.intent.loses_control_of_the_crosswind_burn': (), 'combat.intent.drives_smoke_across_three_paces_of_your_current_route': (), 'combat.intent.cannot_break_a_crossing_under_the_honest_warning': (), 'combat.intent.cannot_release_the_braced_rock_face': (), 'combat.intent.signals_allies_toward_your_last_known_position': (), 'combat.intent.hauls_the_marked_net_line': (), 'combat.intent.recovers_the_empty_net_line': (), 'combat.intent.feeds_smoke_into_a_short_lane_from_its_station': (), 'combat.intent.covers_a_wounded_ally_s_marked_withdrawal': (), 'combat.intent.withdraws_toward_cover': (), 'combat.intent.retreated': (), 'combat.intent.escapes_with_visible_stolen_cargo': (), 'combat.intent.watching': (), 'combat.intent.investigates_a_last_known_position': (), 'combat.intent.holds_without_a_perceived_courier_position': (), 'combat.intent.moves_for_a_clear_line': (), 'combat.intent.evaded': (), 'combat.intent.circles_before_another_charge': (), 'combat.intent.lowers_its_head_and_charges_next_turn': (), 'combat.intent.recovers_before_another_attack': (), 'combat.intent.holds_where_the_route_is_blocked': ()})

_ACTION_TEMPLATE_CONTRACT.update({'combat.intent.recovers_position_before_acting_again': (), 'combat.intent.breaks_contact': ('reason',), 'combat.intent.escaped_with_stolen_cargo': (), 'combat.intent.finds_no_courier': (), 'combat.intent.bogged_in_mud': (), 'combat.intent.attack_warning_reach': (), 'combat.intent.attack_warning_melee': ()})
_ACTION_TEMPLATE_CONTRACT.update({'social.courier.unavailable': (), 'social.courier.ready': ('courier',), 'social.courier.selected': ('courier', 'role'), 'social.recruit.face_to_face': (), 'social.recruit.kit': (), 'social.recruit.unavailable': (), 'social.recruit.berths_full': ('capacity',), 'social.recruit.joined_memory': ('visitor', 'witnessed'), 'social.recruit.accepted': ('visitor',), 'social.recruit.defer.unavailable': (), 'social.recruit.defer.success': (), 'social.objective.unavailable': (), 'social.objective.alter_unavailable': (), 'social.objective.invalid_decision': (), 'social.objective.accepted': ('courier',), 'social.objective.refused': ('courier', 'region'), 'social.objective.altered': ('courier',), 'social.objective.completed_memory': ('courier', 'region', 'method'), 'social.objective.completed': ('region', 'method'), 'social.objective.reduced_account': ('courier', 'condition', 'commodity'), 'social.objective.reduced_result': ('condition', 'loss'), 'social.negotiate.unavailable': (), 'social.negotiate.no_human': (), 'social.negotiate.no_terms': (), 'social.negotiate.elite_evidence': (), 'social.negotiate.violence_evidence': (), 'intent.social.negotiated': (), 'social.negotiate.drawback': (), 'social.negotiate.memory': ('courier', 'count'), 'social.negotiate.success': ('count', 'drawback'), 'social.contact.objective': ('contact',), 'social.contact.inspect': ('contact',), 'social.contact.speak': ('contact',), 'social.contact.regional_ready': ('contact',), 'social.contact.arc_ready': ('contact',), 'social.contact.sanctum': ('contact',), 'social.contact.traveller': ('contact',), 'social.incident.pending': ('first', 'second', 'cause'), 'social.incident.record': ('first', 'second', 'kind', 'cause'), 'social.incident.none': (), 'social.incident.mediate': ('first', 'second'), 'social.incident.side_first': ('first', 'second'), 'social.incident.fight': (), 'social.incident.invalid_response': (), 'social.incident.resolved_record': ('kind', 'text', 'cause'), 'social.incident.unavailable': (), 'social.merchant.unavailable': (), 'social.merchant.insufficient_credit': ('cost', 'credit'), 'social.merchant.memory': ('cost', 'item', 'merchant'), 'social.merchant.sold_memory': ('item', 'courier', 'region'), 'social.merchant.purchased': ('item',), 'social.claimant.none': (), 'social.claimant.requirements': (), 'intent.social.claimant_settled': (), 'social.claimant.contact_memory': ('actor',), 'social.claimant.settled': ('contact', 'actor'), 'social.move.person_occupies': ('person',), 'social.interact.person': ('person',), 'social.interact.bartender': ('bartender',)})

_ACTION_TEMPLATE_CONTRACT.update({
    "social.incident.cause.assistance_known": ("helper", "injured", "injury"),
    "social.incident.cause.argument": ("standing",),
    "social.incident.cause.assistance": (),
    "social.incident.cause.shared_meal": (),
})

_ACTION_TEMPLATE_CONTRACT.update({'social.recruit.terms.recruit-maelin': (), 'social.recruit.terms.recruit-jessa': (), 'social.recruit.terms.recruit-orra': (), 'social.recruit.terms.recruit-bran': (), 'social.recruit.terms.recruit-teren': (), 'social.recruit.terms.recruit-sava': (), 'social.incident.response_required': ()})

_ACTION_TEMPLATE_CONTRACT.update({'social.depart.courier_required': (), 'social.merchant.open_stock': (), 'social.objective.replacement_memory': ('courier',), 'social.objective.replacement_released': (), 'social.objective.no_replacement': (), 'social.objective.accept_required': ('region',)})


_ACTION_TEMPLATE_CONTRACT.update({'action.setup.weapon.unavailable': (), 'action.setup.weapon.courier_required': (), 'action.setup.weapon.locker_full': (), 'action.setup.weapon.pack_full': (), 'action.setup.weapon.ammunition_pack_full': (), 'action.setup.weapon.readied': ('weapon',), 'action.setup.gear.unavailable': (), 'action.setup.gear.courier_required': (), 'action.setup.gear.locker_full': (), 'action.setup.gear.pack_full': (), 'action.setup.gear.packed': ('gear',), 'action.setup.support.unavailable': (), 'action.setup.support.prepared': ('support',), 'action.setup.relic.unavailable': (), 'action.setup.relic.carried': ('relic',), 'action.setup.passive.unavailable': (), 'action.setup.passive.stowed': ('passive',), 'action.setup.passive.capacity': ('capacity',), 'action.setup.passive.cells': (), 'action.setup.passive.packed': ('passive', 'quantity'), 'action.depart.invalid': (), 'action.depart.returning_tug': (), 'action.depart.voyage_active': (), 'action.depart.preparation': (), 'action.depart.no_landing': (), 'action.depart.memory': ('count', 'courier', 'region'), 'action.fall.no_landing': (), 'action.fall.sail': (), 'action.fall.gull_cord': (), 'action.fall.cliff_cord': (), 'action.fall.cargo': ('item',), 'action.fall.result': ('damage', 'cargo'), 'action.movement.unavailable': (), 'action.movement.threat_blocks': ('threat',), 'action.movement.blocked': ('reason', 'remedy'), 'action.movement.diagonal_blocked': (), 'action.movement.moving_volley': (), 'action.movement.roof_nail': (), 'action.movement.open_door': (), 'action.movement.mud': (), 'action.movement.armour_noise': (), 'action.movement.injury_delay': (), 'action.movement.water_delay': (), 'action.movement.smokeleaf_water': (), 'action.movement.area_discovered': ('area',), 'action.movement.coppice_shortcut': (), 'action.container.none': (), 'action.container.open': ('container',), 'action.container.empty': (), 'action.container.requirement.rope': (), 'action.container.requirement.light': (), 'action.container.requirement.key': (), 'action.container.rope_exhausted': (), 'action.container.tally': (), 'action.container.practice_tally': (), 'action.container.memory': ('courier', 'container', 'rewards'), 'action.container.opened': ('container', 'rewards', 'tally'), 'action.container.packed': ('items',), 'action.container.left': ('items',), 'action.environment.greywash.control': (), 'action.environment.greenwold.control': (), 'action.environment.whitecairn.control': (), 'action.environment.hearthford.control': ('control',), 'action.environment.furnace.fire': (), 'action.environment.furnace.braked': (), 'action.environment.furnace.smoke': (), 'action.environment.floor.none': (), 'action.environment.floor.requirement': (), 'action.environment.floor.braced': (), 'action.environment.floor.broken': ('fall', 'sounds'), 'action.interact.tug_returned': (), 'action.interact.tug_mooring': (), 'action.interact.tavern_exit': (), 'action.interact.hatch_occupied': (), 'action.interact.crisis_tavern': (), 'action.interact.tavern_enter': (), 'action.interact.voyage_danger': (), 'action.interact.route_destination': (), 'action.interact.stores': (), 'action.interact.hold': (), 'action.interact.chronicle': (), 'action.interact.nothing': (), 'action.interact.sanctum_shrine': (), 'action.interact.situation': (), 'action.interact.sanctum_sealed': (), 'action.interact.climb_overloaded': (), 'action.item.field_only': (), 'action.item.tide_knot': (), 'action.item.ebbglass.unavailable': (), 'action.item.ebbglass.used': (), 'action.item.coalheart.used': (), 'action.item.hollow_bell.used': (), 'action.item.stillwater.used': (), 'action.item.flood_mark.unavailable': (), 'action.item.flood_mark.used': ('lowered', 'steadied'), 'action.item.ashglass.unavailable': (), 'action.item.ashglass.mark': ('cache', 'x', 'y', 'z'), 'action.item.ashglass.used': ('cleared', 'store', 'revealed'), 'action.item.quarry_pin.unavailable': (), 'action.item.quarry_pin.used': ('supports',), 'action.item.winter_bead.season': (), 'action.item.winter_bead.unavailable': (), 'action.item.winter_bead.used': ('count',), 'action.item.red_clay.unavailable': (), 'action.item.red_clay.memory': ('courier', 'institution'), 'action.item.red_clay.used': ('count', 'institution'), 'action.item.wreck_light.unavailable': (), 'action.item.wreck_light.mark': ('cache', 'x', 'y', 'z'), 'action.item.wreck_light.used': ('cache', 'animals', 'humans'), 'action.item.signal_mirror.used': (), 'action.item.smoke_pot.used': (), 'action.item.bird_whistle.used': (), 'action.item.cache_bell.mark': ('cache', 'x', 'y', 'z'), 'action.item.cache_bell.used': ('cache',), 'action.item.lantern.memory': ('courier',), 'action.item.lantern.used': (), 'action.item.willow_dressing.used': ('amount', 'method'), 'action.item.willow_dressing.method': (), 'action.item.smoke_charge.used': (), 'action.item.lamp_wick.used': (), 'action.item.brine_wash.used': (), 'action.item.splint.used': ('location',), 'action.item.green_poultice.used': ('location',), 'action.item.none': (), 'action.retreat.none': (), 'action.retreat.blocked': (), 'action.retreat.used': (), 'action.merchant.locker_full': (), 'action.return.landing': ('region',), 'action.return.disembark': (), 'action.return.tug': (), 'action.return.objective_failed': ('courier',), 'action.return.memory': ('courier',), 'action.return.merchant': (), 'action.return.completed': ('merchant', 'visitors', 'development'), 'action.drink.tavern_required': (), 'action.route.unavailable': (), 'action.route.resupply.exhausted': (), 'action.route.resupply.credit': (), 'action.route.resupply.used': ('node',), 'action.route.trade.unavailable': ('node', 'commodity'), 'action.route.trade.used': ('node', 'commodity'), 'action.route.sound.complete': (), 'action.route.sound.used': ('nodes',), 'action.route.invalid': ()})


_ACTION_TEMPLATE_CONTRACT.update({'action.interact.vertical_blocked': ('threat',), 'action.interact.vertical_used': ('link', 'sanctum', 'structure', 'slow', 'marked'), 'action.interact.vertical.slow': (), 'action.interact.vertical.marked': (), 'action.objective.load_full': (), 'action.objective.secured': ('commodity',), 'action.objective.lost_ground': ('commodity', 'x', 'y', 'z'), 'action.objective.lost_enemy': ('carrier', 'commodity'), 'intent.item.tide_knot': (), 'intent.item.ashglass': (), 'intent.item.red_clay': (), 'intent.item.wreck_light': (), 'intent.item.signal_mirror': (), 'intent.item.smoke_pot': (), 'intent.item.lantern': ()})


_ACTION_TEMPLATE_CONTRACT.update({'action.weather.clear': (), 'action.weather.river_fog': (), 'action.weather.hard_rain': (), 'action.weather.coast_squall': (), 'action.weather.salt_wind': (), 'action.weather.forest_rain': (), 'action.weather.crosswind': (), 'action.weather.ridge_gust': (), 'action.process.greywash.1': (), 'action.process.greywash.2': (), 'action.process.greywash.2_held': (), 'action.process.greywash.3': (), 'action.process.greenwold.1': (), 'action.process.greenwold.2': (), 'action.process.greenwold.2_safe': (), 'action.process.greenwold.3': (), 'action.process.whitecairn.1': (), 'action.process.whitecairn.2': (), 'action.process.whitecairn.2_safe': (), 'action.process.whitecairn.3': (), 'action.process.hearthford': (), 'action.process.objective_changed': ('process',), 'action.process.escalation': ('threat',), 'action.status.coalheart.cause': (), 'action.status.coalheart.consequence': (), 'action.status.flood_mark.cause': (), 'action.status.flood_mark.consequence': (), 'action.status.winter_bead.cause': (), 'action.status.winter_bead.consequence': (), 'action.status.brine.cause': (), 'action.status.brine.consequence': (), 'action.item.origin.household': (), 'action.item.origin.returned': (), 'action.item.origin.container': ('container', 'region'), 'action.item.origin.merchant': (), 'action.fall.source': (), 'action.depart.merchant_away': (), 'action.return.merchant_present': ()})


_ACTION_TEMPLATE_CONTRACT.update({'action.sound.echo_bead': ('level',), 'action.damage.no_courier': (), 'combat.damage.river_glass_ward': (), 'action.vehicle.separated': (), 'action.pressure.increased': ('band',), 'action.interact.tavern_draw.busy': (), 'action.interact.tavern_draw.ready': (), 'action.interact.quay_bones.busy': (), 'action.interact.quay_bones.ready': ('bartender',)})


_ACTION_TEMPLATE_CONTRACT.update({'action.depart.started.tug': ('region',), 'action.depart.started.gangplank': ('region',), 'action.interact.vertical.combat.down': (), 'action.interact.vertical.combat.up': (), 'action.interact.vertical.down': (), 'action.interact.vertical.up': (), 'action.interact.station.G': (), 'action.interact.station.R': (), 'action.interact.station.b': (), 'action.interact.station.U': (), 'action.interact.station.p': (), 'action.interact.station.W': (), 'action.interact.station.S': (), 'action.interact.station.N': (), 'action.interact.station.O': (), 'action.interact.station.T': (), 'action.interact.station.s': ()})

_ARC_RELIC_IDS = ("common-work-rivet", "counterclaim-lodestone", "lee-cloth-brooch", "channel-surety-shuttle")
_LEGENDARY_TEMPLATE_CONTRACT = {
    "legendary.object.noun.0": (), "legendary.object.noun.1": (), "legendary.object.noun.2": (),
    "legendary.object.name": ("maker", "noun"),
    "legendary.object.effect": ("epithet", "tags", "range", "verbs"),
    "legendary.object.tradeoff": (),
    "legendary.object.provenance": ("maker", "institution", "crisis", "repair", "dispute"),
    "legendary.object.clue": ("account", "cache_name", "cache_id"),
    "legendary.object.description": ("provenance", "effect", "tradeoff", "interested", "clue"),
    "legendary.object.ledger": ("clue",),
    **{f"legendary.arc.{relic}.display_name": () for relic in _ARC_RELIC_IDS},
    **{f"legendary.arc.{relic}.description": () for relic in _ARC_RELIC_IDS},
    "legendary.arc.unavailable": (),
    "legendary.arc.common-work-rivet.unavailable": (), "legendary.arc.common-work-rivet.status_cause": (),
    "legendary.arc.common-work-rivet.status_consequence": (), "legendary.arc.common-work-rivet.result": ("equipment", "supports"),
    "legendary.arc.counterclaim-lodestone.unavailable": (), "legendary.arc.counterclaim-lodestone.intent": (),
    "legendary.arc.counterclaim-lodestone.result": ("dropped",),
    "legendary.arc.lee-cloth-brooch.unavailable": (), "legendary.arc.lee-cloth-brooch.result": (),
    "legendary.arc.channel-surety-shuttle.unavailable": (), "legendary.arc.channel-surety-shuttle.cargo": ("cargo",),
    "legendary.arc.channel-surety-shuttle.no_cargo": (), "legendary.arc.channel-surety-shuttle.result": ("water", "x", "y", "cargo"),
    "legendary.arc.grant.provenance": ("arc", "choice"), "legendary.arc.grant.packed": (),
    "legendary.arc.grant.ground": (), "legendary.arc.grant.memory": ("courier", "name", "arc", "where"),
    "legendary.arc.grant.result": ("name", "where"),
}

_TOPOLOGY_TEMPLATE_CONTRACT = {
    "topology.hearthford.zone.settlement": (),
    "topology.hearthford.zone.floodplain": (),
    "topology.hearthford.zone.watch": (),
    "topology.hearthford.zone.millworks": (),
    "topology.hearthford.zone.river_road": (),
    "topology.hearthford.landmark.watchtower": (),
    "topology.hearthford.landmark.mill": (),
    "topology.hearthford.link.culvert_steps": (),
    "topology.hearthford.link.mill_ladder": (),
    "topology.hearthford.link.roof_ladder": (),
    "topology.hearthford.link.watch_ladder": (),
    "topology.hearthford.link.watch_roof_ladder": (),
    "topology.hearthford.container.ruin": (),
    "topology.hearthford.container.reed": (),
    "topology.hearthford.container.road": (),
    "topology.hearthford.container.cave": (),
    "topology.hearthford.container.cellar": (),
    "topology.hearthford.container.gantry": (),
    "topology.hearthford.container.watch": (),
    "topology.hearthford.container.roof": (),
    "topology.hearthford.container.compact": (),
}

_REGIONAL_GENERATOR_SLOTS = {
    "greywash": {
        "zones": ("village", "salt_pans", "dune_road", "wreck_flats", "tide_chain_house"),
        "landmarks": ("landing", "contact", "second_contact", "settlement", "saltworks", "dunes", "wreck", "chain_house", "cave_entrance", "objective", "elevated"),
        "links": ("sea_cave_steps", "signal_mast_ladder", "mast_roof_ladder", "chain_house_stair", "chain_roof_hatch"),
        "containers": ("quay", "pan", "wreck", "cave", "mast", "chain", "tide_account"),
    },
    "greenwold": {
        "zones": ("village", "woodland", "resin_yard", "burnworks", "root_hollows"),
        "landmarks": ("landing", "contact", "second_contact", "settlement", "clearing", "resin_yard", "burn_walk", "root_cellar", "cave_entrance", "watch_tree", "objective", "elevated"),
        "links": ("root_cellar_steps", "watch_tree_ladder", "canopy_ladder", "burn_walk_ladder", "smoke_roof_ladder"),
        "containers": ("village", "clearing", "root", "resin", "watch", "burn", "burn_account"),
    },
    "whitecairn": {
        "zones": ("village", "switchbacks", "quarry_face", "lime_kilns", "bell_ridge"),
        "landmarks": ("landing", "contact", "second_contact", "settlement", "quarry", "lime_kiln", "bell_tower", "sinkhole", "cave_entrance", "objective", "elevated"),
        "links": ("sinkhole_ladder", "quarry_hoist_ladder", "ridge_climbing_pegs", "bell_tower_stair", "bell_parapet_ladder"),
        "containers": ("village", "quarry", "cave", "kiln", "bridge", "tower", "sink_account"),
    },
    **{
        region: {
            "zones": ("inhabited_court", "old_scar", "industrial_works", "far_shore", "buried_drain"),
            "landmarks": ("landing", "contact", "second_contact", "settlement", "ruin", "works", "far_bank", "store", "cave_entrance", "objective", "control", "elevated"),
            "links": ("work_stair", "roof_ladder", "excavated_drain_stair", "store_cellar_ladder"),
            "containers": ("quay", "ledger", "ruin", "bank", "cellar", "deep", "loft", "crown"),
        }
        for region in ("dunmire", "rillscar", "marlbank", "frostmere")
    },
}

_LANDSCAPE_TOPOLOGY_TEMPLATE_CONTRACT = {
    **{f"topology.{region}.landform.{index}.name": () for region in ("hearthford", "greywash", "greenwold", "whitecairn", "dunmire", "rillscar", "marlbank", "frostmere") for index in range(4)},
    **{f"topology.{region}.landform.{field}": () for region in ("hearthford", "greywash", "greenwold", "whitecairn", "dunmire", "rillscar", "marlbank", "frostmere") for field in ("upper", "lower", "traveller.name")},
    **{f"topology.{region}.discovery.{suffix}.{field}": () for region, suffixes in {"hearthford": ("reed-silt","watch-foundation","tailrace-wrap"), "greywash": ("ebb-line","dune-pin","pan-shutter"), "greenwold": ("root-hollow","canopy-fall","pitch-scrape"), "whitecairn": ("scree-seam","bell-shadow","handline-notch"), "dunmire": ("peat-print","bank-notch","reed-roost"), "rillscar": ("span-pin","iron-vein","switchback-tally"), "marlbank": ("seed-line","kiln-mark","culvert-roll"), "frostmere": ("ice-sounding","net-drift","cold-linen")}.items() for suffix in suffixes for field in ("name", "clue")},
    "topology.landform.link.upper": ("structure",), "topology.landform.link.lower": ("structure",), "topology.landform.fact.pocket": ("landform", "x", "y", "glyph"), "topology.landform.fact.structure": ("structure", "x", "y", "z"), "topology.landform.fact.legacy": ("text",),
    "topology.landform.event.limit": (), "topology.landform.event.occupied": (), "topology.landform.event.none": (), "topology.landform.event.threat": ("actor", "x", "y", "goal"), "topology.landform.traveller.existing": ("traveller",), "topology.landform.traveller.unavailable": (), "topology.landform.traveller.role": (), "topology.landform.traveller.memory": ("landform",), "topology.landform.traveller.schedule": (), "topology.landform.traveller.arrival": ("traveller", "x", "y"), "topology.landform.event.stonefall": ("landform", "x", "y"), "topology.landform.event.stonefall.none": (), "topology.landform.event.structure_fall": ("structure", "x", "y"), "topology.landform.event.structure_fall.none": (), "topology.landform.traveller.none": (), "topology.landform.traveller.clue.used": (), "topology.landform.traveller.clue.memory": ("x", "y", "courier"), "topology.landform.traveller.clue.result": ("traveller", "structure", "x", "y"), "topology.landform.traveller.lot.used": (), "topology.landform.traveller.lot.credit": (), "topology.landform.traveller.lot.result": ("commodity", "placement"), "topology.landform.traveller.lot.packed": (), "topology.landform.traveller.lot.ground": (), "topology.landform.traveller.lot.provenance": ("traveller",), "topology.landform.traveller.invalid": (), "topology.landform.overlay.title": ("traveller",), "topology.landform.overlay.summary": ("structure", "commodity"), "topology.landform.overlay.guidance": (), "topology.landform.overlay.clue.label": (), "topology.landform.overlay.clue.requirement": (), "topology.landform.overlay.lot.label": ("commodity",), "topology.landform.overlay.lot.requirement": (), "topology.discovery.reveal": ("cache", "clue"), "topology.discovery.wayfinding": ("courier", "region"),
}
_TOPOLOGY_TEMPLATE_CONTRACT.update(_LANDSCAPE_TOPOLOGY_TEMPLATE_CONTRACT)

_TOPOLOGY_TEMPLATE_CONTRACT.update({
    f"topology.{region}.{field}": ()
    for region in _REGIONAL_GENERATOR_SLOTS
    for field in ("condition", "work", "pressure", "objective", "hazard", "process")
})
_TOPOLOGY_TEMPLATE_CONTRACT.update({
    f"topology.{region}.{category[:-1]}.{slot}": ()
    for region, categories in _REGIONAL_GENERATOR_SLOTS.items()
    for category, slots in categories.items()
    for slot in slots
})
_TOPOLOGY_TEMPLATE_CONTRACT.update({
    f"topology.{region}.contact.{index}.{field}": ()
    for region in _REGIONAL_GENERATOR_SLOTS
    for index in (1, 2)
    for field in ("name", "role")
})
_TOPOLOGY_TEMPLATE_CONTRACT.update({
    f"topology.{region}.contact.{index}.memory": ("shortage",)
    for region in ("dunmire", "rillscar", "marlbank", "frostmere")
    for index in (1, 2)
})
_TOPOLOGY_TEMPLATE_CONTRACT.update({
    f"topology.{region}.process.{result}": ()
    for region in ("dunmire", "rillscar", "marlbank", "frostmere")
    for result in ("stage_one", "applied", "controlled")
})
_TOPOLOGY_TEMPLATE_CONTRACT["topology.frontier.process.held"] = ()

_HISTORY_TEMPLATE_CONTRACT = {
    'history.event.water_and_stone.account': ('climate', 'production', 'dependency'),
    'history.event.water_and_stone.consequence': ('water',),
    'history.event.crisis.account': ('witness', 'crisis', 'landmark'),
    'history.event.crisis.consequence': ('coordinate', 'dependency'),
    'history.event.recovery.account': ('institution', 'recovery', 'crisis'),
    'history.event.recovery.private_consequence': (),
    'history.event.recovery.shared_consequence': (),
    'history.event.contested_occupation.account': ('dispute',),
    'history.event.contested_occupation.consequence': (),
    'history.event.unsettled_account.account': ('witness', 'cache'),
    'history.event.unsettled_account.consequence': ('institution',),
    'history.contact_memory': ('institution', 'crisis', 'recovery', 'dispute'),
    'history.guard_reason': ('crisis', 'institution', 'production'),
    'history.cache_name.flood': ('witness',),
    'history.cache_name.fire': ('witness',),
    'history.cache_name.support_loss': ('witness',),
    'history.route_dependency': ('crisis', 'dependency'),
    'history.network.contact_memory': ('institution', 'region'),
    'history.local_delivery.no_account': (),
    'history.local_delivery.supplied': (),
    'history.local_delivery.missing': ('dependency',),
    'history.local_delivery.act': ('courier', 'dependency', 'day'),
    'history.local_delivery.memory': ('courier', 'dependency', 'institution', 'trust', 'obligation'),
    'history.local_delivery.result': ('weighed',),
    'history.local_delivery.weighed': (),
    'history.network_delivery.no_account': (),
    'history.network_delivery.obligations': ('institution',),
    'history.network_delivery.missing': ('dependency',),
    'history.network_delivery.act': ('courier', 'dependency', 'contact_id', 'day'),
    'history.network_delivery.memory': ('institution', 'dependency', 'region', 'trust'),
    'history.network_delivery.result': ('contact', 'institution'),
    'history.network_shelter.no_account': (),
    'history.network_shelter.open': (),
    'history.network_shelter.requirement': (),
    'history.network_shelter.act': ('region', 'edges'),
    'history.network_shelter.memory': ('institution', 'act'),
    'history.network_shelter.result': ('contact', 'edges'),
    'history.network_service.delivery.label': ('dependency',),
    'history.network_service.delivery.requirement': (),
    'history.network_service.shelter.label': (),
    'history.network_service.shelter.requirement': (),
    'history.network_service.practice.label': ('practice',),
    'history.network_service.practice.requirement': (),
    'history.aftermath.shared': (),
    'history.aftermath.claimed': (),
    'history.relationship.supplies': ('production', 'institution'),
    'history.relationship.depends': ('institution', 'production'),
    'history.relationship.witness': ('institution',),
    'history.relationship.shared': ('institution',),
    'history.production.record': ('day', 'output', 'production', 'dependency', 'stock', 'season'),
    'history.production.notice': ('institution', 'output', 'production', 'status'),
    'history.ledger.no_account': (),
    'history.ledger.fact_region': ('region', 'geology', 'climate'),
    'history.ledger.fact_work': ('production', 'dependency'),
    'history.ledger.institution': ('institution', 'goal'),
    'history.ledger.service': ('service',),
    'history.ledger.dispute': ('dispute', 'opposition'),
    'history.ledger.standing': ('trust', 'obligation', 'confidence'),
    'history.ledger.relation': ('text',),
    'history.ledger.witnessed': ('text',),
    'history.ledger.network': ('institution', 'goal'),
    'history.ledger.network_service': ('service', 'trust', 'obligation'),
    'history.ledger.opposition': ('opposition',),
    'history.ledger.no_recent': (),
    'history.ledger.testimony': ('account',),
    'history.ledger.evidence': ('evidence', 'consequence'),
    'history.ledger.landform': ('text',),
    'history.ledger.reading': (),
    'history.forecast.next': ('remaining', 'process'),
    'history.forecast.changed': ('process',),
    'history.forecast': ('season', 'exposure', 'stage'),
}

# Stable engine service keys and their permitted presentation templates.  These
# are deliberately separate from the human-readable text in quests.json.
_QUEST_SERVICE_CONTRACT = {
    "quest.service.unavailable": ("unavailable", (), (), {"unavailable": ()}),
    "quest.service.cache_mark": ("c", (), (), {"marker": ("name", "x", "y", "level"), "marked": (), "none_left": ()}),
    "quest.service.practical_instruction": ("t", (), (), {"already_known": ("courier", "technique"), "completed": ("contact", "technique", "effect")}),
    "quest.service.treatment": ("h", (), (), {"no_care": (), "remaining_injury": (), "memory": ("courier", "location"), "completed": ("contact", "location")}),
    "quest.service.public_field_report": ("p", ("report_name",), (), {"fallback_report_name": (), "missing_account": (), "missing_outcome": (), "record": ("contact", "report_name", "outcome", "response"), "completed": ("record",)}),
    "quest.service.private_field_report": ("r", ("report_name",), (), {"fallback_report_name": (), "missing_account": (), "missing_outcome": (), "record": ("contact", "report_name", "outcome", "response"), "completed": ("record",)}),
    "quest.service.workline_discussion": ("w", (), (), {}),
    "quest.service.dependency_delivery": ("d", ("dependency",), (), {}),
    "quest.service.aftermath_contracts": ("a", ("open_count",), (), {}),
    "quest.service.frontier_claim": ("s", ("claimant",), (), {}),
}

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
class QuestServicePresentation:
    """Immutable selected-pack wording for a stable secondary service."""

    id: str
    engine_id: str
    label: str
    requirement: str
    results: tuple[tuple[str, str], ...]
    responses: tuple[tuple[str, tuple[str, str]], ...] = ()
    effects: tuple[tuple[str, str], ...] = ()


@dataclass(frozen=True)
class HistoryPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class AftermathPresentation:
    id: str
    title: str
    cause: str


@dataclass(frozen=True)
class AftermathActionPresentation:
    id: str
    label: str
    requirements: tuple[tuple[str, str], ...]


@dataclass(frozen=True)
class AftermathResultPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class WorklinePresentation:
    """Immutable selected-pack wording for a stable workline presentation key."""

    id: str
    text: str


@dataclass(frozen=True)
class InterferencePresentation:
    """Immutable selected-pack wording for a stable interference key."""

    id: str
    text: str


@dataclass(frozen=True)
class LegendaryPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class TopologyPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class ActionPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class VesselPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class TravelPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class ShipCrisisPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class VehiclePresentation:
    id: str
    text: str


@dataclass(frozen=True)
class ChemistryPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class ProductionPresentation:
    id: str
    text: str


_SITUATION_IDS = ('hearthford:steady:reed-tally', 'hearthford:strained:wheel-lane', 'hearthford:critical:flood-watch', 'greywash:steady:pan-birds', 'greywash:strained:wreck-title', 'greywash:critical:chain-ebb', 'greenwold:steady:coppice-deer', 'greenwold:strained:resin-smoke', 'greenwold:critical:burn-refuge', 'whitecairn:steady:wool-step', 'whitecairn:strained:kiln-scree', 'whitecairn:critical:bell-span', 'dunmire:steady:peat-sledge', 'dunmire:strained:bank-fire', 'dunmire:critical:causeway-claim', 'rillscar:steady:ore-goats', 'rillscar:strained:charcoal-span', 'rillscar:critical:tailrace-convoy', 'marlbank:steady:seed-birds', 'marlbank:strained:kiln-water', 'marlbank:critical:terrace-collapse', 'frostmere:steady:net-seals', 'frostmere:strained:wool-thaw', 'frostmere:critical:false-sounding')
_SITUATION_TEMPLATE_CONTRACT = {
    **{f"situation.{situation_id}.{field}": () for situation_id in _SITUATION_IDS for field in ("title", "group.0", "group.1", "duty", "material", "choice.tool", "choice.material", "choice.account", "consequence")},
    "situation.notice.activation": ("title", "duty", "material"), "situation.intent.work": ("title", "material"), "situation.reason.stake": ("group",),
    "situation.inspect.resolved.fact": ("title", "x", "y", "z"), "situation.inspect.resolved.answer": ("outcome",), "situation.inspect.resolved.effect": ("consequence",), "situation.inspect.resolved.return": ("condition",), "situation.inspect.afterwork": ("afterwork",), "situation.inspect.followup": (), "situation.inspect.sample": ("sample",), "situation.inspect.report": ("report",), "situation.inspect.time": (),
    "situation.inspect.visible": ("title", "x", "y", "z"), "situation.inspect.groups": ("first", "second"), "situation.inspect.duty": ("duty",), "situation.inspect.material": ("material",), "situation.inspect.condition": ("condition",), "situation.inspect.choice.tool": ("choice",), "situation.inspect.choice.material": ("choice",), "situation.inspect.choice.account": ("choice",), "situation.inspect.guidance": (),
    "situation.choice.maintain": (), "situation.choice.maintain.requirement": (), "situation.choice.sample": ("sample",), "situation.choice.sample.requirement": (), "situation.choice.tool.requirement": (), "situation.choice.material.requirement": (), "situation.choice.account.requirement": (),
    "situation.resolve.missing": (), "situation.resolve.settled": (), "situation.resolve.requirement": ("requirement",), "situation.resolve.pack": (), "situation.afterwork.provenance": ("title",), "situation.afterwork.sample": ("sample",), "situation.afterwork.maintenance": (), "situation.outcome.material": ("choice", "spent"), "situation.intent.completed": (), "situation.intent.negotiated": (), "situation.record.afterwork": ("title", "courier", "result", "outcome"), "situation.record.resolved": ("title", "outcome", "consequence"), "situation.report.unfiled": (), "situation.condition": ("season", "event", "aftermath"),
    "situation.condition.event.water_and_stone": (), "situation.condition.event.flood": (), "situation.condition.event.fire": (), "situation.condition.event.support_loss": (), "situation.condition.event.shared_repair": (), "situation.condition.event.private_advance": (), "situation.condition.event.contested_occupation": (), "situation.condition.event.unsettled_account": (), "situation.condition.event.none": (), "situation.condition.aftermath.shared": (), "situation.condition.aftermath.claimed": (), "situation.condition.aftermath.none": (),
}


_CIRCUIT_TEMPLATE_CONTRACT = {'circuit.part.trace.name': (), 'circuit.part.trace.description': (), 'circuit.part.via.name': (), 'circuit.part.via.description': (), 'circuit.part.rack.name': (), 'circuit.part.rack.description': (), 'circuit.part.cell.name': (), 'circuit.part.cell.description': (), 'circuit.part.switch.name': (), 'circuit.part.switch.description': (), 'circuit.part.lamp.name': (), 'circuit.part.lamp.description': (), 'circuit.part.gate.name': (), 'circuit.part.gate.description': (), 'circuit.part.drain.name': (), 'circuit.part.drain.description': (), 'circuit.part.sensor.name': (), 'circuit.part.sensor.description': (), 'circuit.part.relay.name': (), 'circuit.part.relay.description': (), 'circuit.part.counter.name': (), 'circuit.part.counter.description': (), 'circuit.part.piston.name': (), 'circuit.part.piston.description': (), 'circuit.part.crate.name': (), 'circuit.part.crate.description': (), 'circuit.target.no_space': (), 'circuit.target.vehicle': (), 'circuit.target.courier': (), 'circuit.target.layer': (), 'circuit.target.distance': (), 'circuit.target.footing': (), 'circuit.place.invalid_part': (), 'circuit.place.register_full': (), 'circuit.place.unsupported_layer': ('part', 'layer'), 'circuit.place.surface_ground': (), 'circuit.place.occupied': (), 'circuit.place.surface_walkable': (), 'circuit.place.blocking_device': (), 'circuit.place.missing_part': ('part',), 'circuit.place.success': ('part', 'x', 'y', 'z', 'layer'), 'circuit.reclaim.empty': (), 'circuit.reclaim.provenance': (), 'circuit.reclaim.pack_full': (), 'circuit.reclaim.success': ('part', 'layer'), 'circuit.operate.empty': (), 'circuit.operate.piston_grip': ('grip',), 'circuit.operate.counter_reset': (), 'circuit.operate.sensor_threshold': ('threshold',), 'circuit.operate.no_secondary': (), 'circuit.operate.switch': ('state',), 'circuit.operate.rack_full': (), 'circuit.operate.rack_need_cell': (), 'circuit.operate.rack_load': ('charge',), 'circuit.operate.piston_active': (), 'circuit.operate.turn': ('part', 'facing'), 'circuit.operate.sensor_mode': ('mode',), 'circuit.operate.counter_threshold': ('threshold',), 'circuit.operate.no_control': (), 'circuit.diagnostic.blocked.switch': ('where',), 'circuit.diagnostic.blocked.sensor': ('mode', 'where', 'threshold'), 'circuit.diagnostic.blocked.relay': ('where', 'facing'), 'circuit.diagnostic.blocked.rack': ('where',), 'circuit.diagnostic.no_open_route': (), 'circuit.diagnostic.crate.summary': ('cargo', 'weight', 'limit'), 'circuit.diagnostic.crate.push': (), 'circuit.diagnostic.crate.cover': (), 'circuit.diagnostic.pulse': ('phase', 'next_phase', 'inputs', 'links', 'remaining'), 'circuit.diagnostic.remaining': ('steps',), 'circuit.diagnostic.sources': ('fittings', 'racks', 'charged'), 'circuit.diagnostic.setting.rack': ('charge', 'maximum', 'interval'), 'circuit.diagnostic.setting.switch': ('state',), 'circuit.diagnostic.setting.sensor': ('mode', 'threshold', 'state'), 'circuit.diagnostic.setting.relay': ('facing',), 'circuit.diagnostic.setting.counter': ('count', 'threshold', 'next_count'), 'circuit.diagnostic.setting.piston': ('facing', 'grip'), 'circuit.diagnostic.setting.device': ('state', 'until'), 'circuit.diagnostic.setting.passive': (), 'circuit.diagnostic.route.source_empty': (), 'circuit.diagnostic.route.source_waiting_device': ('distance', 'limit'), 'circuit.diagnostic.route.source_waiting_none': (), 'circuit.diagnostic.route.source_waiting_blocked': ('reason',), 'circuit.diagnostic.route.source_ready': ('actions',), 'circuit.diagnostic.route.switch_blocked': (), 'circuit.diagnostic.route.sensor_blocked': ('mode', 'threshold'), 'circuit.diagnostic.route.device_active': (), 'circuit.diagnostic.route.source_depleted': (), 'circuit.diagnostic.route.no_charged_rack': (), 'circuit.diagnostic.route.open_far': ('distance', 'limit'), 'circuit.diagnostic.route.open_waiting': (), 'circuit.diagnostic.route.powered': (), 'circuit.diagnostic.route.no_physical': (), 'circuit.diagnostic.last': ('pulse', 'event'), 'circuit.piston.block.terrain': (), 'circuit.piston.block.fitting': (), 'circuit.piston.block.actor': (), 'circuit.piston.block.piston': (), 'circuit.piston.too_many': (), 'circuit.piston.overweight': ('limit',), 'circuit.piston.blocked': ('reason',), 'circuit.piston.extended': ('facing', 'count', 'crate_word'), 'circuit.piston.retracted': (), 'circuit.piston.nothing': (), 'circuit.piston.retract_overweight': ('limit',), 'circuit.piston.retract_blocked': ('reason',), 'circuit.piston.pulled': (), 'circuit.event.depleted': (), 'circuit.event.drained': ('amount',), 'circuit.event.powered': (), 'circuit.event.counter': ('threshold',), 'circuit.phase.wire': (), 'circuit.phase.head': (), 'circuit.phase.tail': (), 'circuit.state.closed': (), 'circuit.state.open': (), 'circuit.state.detected': (), 'circuit.state.clear': (), 'circuit.state.active': (), 'circuit.state.idle': (), 'circuit.grip.sticky': (), 'circuit.grip.push_only': (), 'circuit.terminal.heading': ('layer', 'x', 'y', 'z', 'description'), 'circuit.terminal.empty': (), 'circuit.terminal.inspect_empty': (), 'circuit.terminal.build_option': ('key', 'part', 'count'), 'circuit.terminal.cells': ('count',), 'circuit.terminal.controls': (), 'circuit.terminal.fallback': (), 'circuit.terminal.step': (), 'circuit.value.never': (), 'circuit.value.none': (), 'circuit.verb.close': (), 'circuit.verb.open': (), 'circuit.crate.one': (), 'circuit.crate.many': (), 'circuit.mode.mass': (), 'circuit.mode.water': (), 'circuit.mode.threat': (), 'circuit.facing.north': (), 'circuit.facing.east': (), 'circuit.facing.south': (), 'circuit.facing.west': (), 'circuit.layer.surface': (), 'circuit.layer.buried': (), 'circuit.mode_title.mass': (), 'circuit.mode_title.water': (), 'circuit.mode_title.threat': ()}

_ECOLOGY_TEMPLATE_CONTRACT = {'ecology.actor.fen-pail.name': (), 'ecology.actor.fen-pail.capability': (), 'ecology.actor.fen-pail.counterplay': (), 'ecology.actor.fen-cinder.name': (), 'ecology.actor.fen-cinder.capability': (), 'ecology.actor.fen-cinder.counterplay': (), 'ecology.actor.fen-lynx.name': (), 'ecology.actor.fen-lynx.capability': (), 'ecology.actor.fen-lynx.counterplay': (), 'ecology.actor.fen-hare.name': (), 'ecology.actor.fen-hare.capability': (), 'ecology.actor.fen-hare.counterplay': (), 'ecology.actor.fen-bracer.name': (), 'ecology.actor.fen-bracer.capability': (), 'ecology.actor.fen-bracer.counterplay': (), 'ecology.actor.fen-recoverer.name': (), 'ecology.actor.fen-recoverer.capability': (), 'ecology.actor.fen-recoverer.counterplay': (), 'ecology.actor.gorge-cutter.name': (), 'ecology.actor.gorge-cutter.capability': (), 'ecology.actor.gorge-cutter.counterplay': (), 'ecology.actor.gorge-escort.name': (), 'ecology.actor.gorge-escort.capability': (), 'ecology.actor.gorge-escort.counterplay': (), 'ecology.actor.gorge-surgeon.name': (), 'ecology.actor.gorge-surgeon.capability': (), 'ecology.actor.gorge-surgeon.counterplay': (), 'ecology.actor.gorge-marmot.name': (), 'ecology.actor.gorge-marmot.capability': (), 'ecology.actor.gorge-marmot.counterplay': (), 'ecology.actor.gorge-cat.name': (), 'ecology.actor.gorge-cat.capability': (), 'ecology.actor.gorge-cat.counterplay': (), 'ecology.actor.gorge-caller.name': (), 'ecology.actor.gorge-caller.capability': (), 'ecology.actor.gorge-caller.counterplay': (), 'ecology.actor.terrace-drainer.name': (), 'ecology.actor.terrace-drainer.capability': (), 'ecology.actor.terrace-drainer.counterplay': (), 'ecology.actor.terrace-kiln.name': (), 'ecology.actor.terrace-kiln.capability': (), 'ecology.actor.terrace-kiln.counterplay': (), 'ecology.actor.terrace-hare.name': (), 'ecology.actor.terrace-hare.capability': (), 'ecology.actor.terrace-hare.counterplay': (), 'ecology.actor.terrace-thief.name': (), 'ecology.actor.terrace-thief.capability': (), 'ecology.actor.terrace-thief.counterplay': (), 'ecology.actor.terrace-netter.name': (), 'ecology.actor.terrace-netter.capability': (), 'ecology.actor.terrace-netter.counterplay': (), 'ecology.actor.terrace-fox.name': (), 'ecology.actor.terrace-fox.capability': (), 'ecology.actor.terrace-fox.counterplay': (), 'ecology.actor.estuary-drainer.name': (), 'ecology.actor.estuary-drainer.capability': (), 'ecology.actor.estuary-drainer.counterplay': (), 'ecology.actor.estuary-caller.name': (), 'ecology.actor.estuary-caller.capability': (), 'ecology.actor.estuary-caller.counterplay': (), 'ecology.actor.estuary-hunter.name': (), 'ecology.actor.estuary-hunter.capability': (), 'ecology.actor.estuary-hunter.counterplay': (), 'ecology.actor.estuary-hare.name': (), 'ecology.actor.estuary-hare.capability': (), 'ecology.actor.estuary-hare.counterplay': (), 'ecology.actor.estuary-escort.name': (), 'ecology.actor.estuary-escort.capability': (), 'ecology.actor.estuary-escort.counterplay': (), 'ecology.actor.estuary-mender.name': (), 'ecology.actor.estuary-mender.capability': (), 'ecology.actor.estuary-mender.counterplay': (), 'ecology.actor.hearth-sluice-runner.name': (), 'ecology.actor.hearth-sluice-runner.capability': (), 'ecology.actor.hearth-sluice-runner.counterplay': (), 'ecology.actor.hearth-rope-cutter.name': (), 'ecology.actor.hearth-rope-cutter.capability': (), 'ecology.actor.hearth-rope-cutter.counterplay': (), 'ecology.actor.hearth-meadow-kite.name': (), 'ecology.actor.hearth-meadow-kite.capability': (), 'ecology.actor.hearth-meadow-kite.counterplay': (), 'ecology.actor.coast-wreck-gull.name': (), 'ecology.actor.coast-wreck-gull.capability': (), 'ecology.actor.coast-wreck-gull.counterplay': (), 'ecology.actor.coast-chain-mender.name': (), 'ecology.actor.coast-chain-mender.capability': (), 'ecology.actor.coast-chain-mender.counterplay': (), 'ecology.actor.coast-brine-pourer.name': (), 'ecology.actor.coast-brine-pourer.capability': (), 'ecology.actor.coast-brine-pourer.counterplay': (), 'ecology.actor.forest-cinder-crow.name': (), 'ecology.actor.forest-cinder-crow.capability': (), 'ecology.actor.forest-cinder-crow.counterplay': (), 'ecology.actor.forest-well-runner.name': (), 'ecology.actor.forest-well-runner.capability': (), 'ecology.actor.forest-well-runner.counterplay': (), 'ecology.actor.forest-coppice-escort.name': (), 'ecology.actor.forest-coppice-escort.capability': (), 'ecology.actor.forest-coppice-escort.counterplay': (), 'ecology.actor.upland-lantern-taker.name': (), 'ecology.actor.upland-lantern-taker.capability': (), 'ecology.actor.upland-lantern-taker.counterplay': (), 'ecology.actor.upland-face-bracer.name': (), 'ecology.actor.upland-face-bracer.capability': (), 'ecology.actor.upland-face-bracer.counterplay': (), 'ecology.actor.upland-crag-goat.name': (), 'ecology.actor.upland-crag-goat.capability': (), 'ecology.actor.upland-crag-goat.counterplay': (), 'ecology.actor.fen-peat-raker.name': (), 'ecology.actor.fen-peat-raker.capability': (), 'ecology.actor.fen-peat-raker.counterplay': (), 'ecology.actor.fen-board-mender.name': (), 'ecology.actor.fen-board-mender.capability': (), 'ecology.actor.fen-board-mender.counterplay': (), 'ecology.actor.fen-marsh-harrier.name': (), 'ecology.actor.fen-marsh-harrier.capability': (), 'ecology.actor.fen-marsh-harrier.counterplay': (), 'ecology.actor.gorge-hoist-thief.name': (), 'ecology.actor.gorge-hoist-thief.capability': (), 'ecology.actor.gorge-hoist-thief.counterplay': (), 'ecology.actor.gorge-lime-bracer.name': (), 'ecology.actor.gorge-lime-bracer.capability': (), 'ecology.actor.gorge-lime-bracer.counterplay': (), 'ecology.actor.gorge-raven.name': (), 'ecology.actor.gorge-raven.capability': (), 'ecology.actor.gorge-raven.counterplay': (), 'ecology.actor.terrace-rill-dogger.name': (), 'ecology.actor.terrace-rill-dogger.capability': (), 'ecology.actor.terrace-rill-dogger.counterplay': (), 'ecology.actor.terrace-ash-thief.name': (), 'ecology.actor.terrace-ash-thief.capability': (), 'ecology.actor.terrace-ash-thief.counterplay': (), 'ecology.actor.terrace-rook.name': (), 'ecology.actor.terrace-rook.capability': (), 'ecology.actor.terrace-rook.counterplay': (), 'ecology.actor.estuary-ice-bracer.name': (), 'ecology.actor.estuary-ice-bracer.capability': (), 'ecology.actor.estuary-ice-bracer.counterplay': (), 'ecology.actor.estuary-wreck-taker.name': (), 'ecology.actor.estuary-wreck-taker.capability': (), 'ecology.actor.estuary-wreck-taker.counterplay': (), 'ecology.actor.estuary-ice-otter.name': (), 'ecology.actor.estuary-ice-otter.capability': (), 'ecology.actor.estuary-ice-otter.counterplay': (), 'ecology.actor.hearth-sluice-duellist.name': (), 'ecology.actor.hearth-sluice-duellist.capability': (), 'ecology.actor.hearth-sluice-duellist.counterplay': (), 'ecology.actor.coast-shingle-crab.name': (), 'ecology.actor.coast-shingle-crab.capability': (), 'ecology.actor.coast-shingle-crab.counterplay': (), 'ecology.actor.forest-ash-trapper.name': (), 'ecology.actor.forest-ash-trapper.capability': (), 'ecology.actor.forest-ash-trapper.counterplay': (), 'ecology.actor.cairn-bell-shrike.name': (), 'ecology.actor.cairn-bell-shrike.capability': (), 'ecology.actor.cairn-bell-shrike.counterplay': (), 'ecology.actor.mire-peat-tusker.name': (), 'ecology.actor.mire-peat-tusker.capability': (), 'ecology.actor.mire-peat-tusker.counterplay': (), 'ecology.actor.gorge-span-harrier.name': (), 'ecology.actor.gorge-span-harrier.capability': (), 'ecology.actor.gorge-span-harrier.counterplay': (), 'ecology.actor.terrace-kiln-slinger.name': (), 'ecology.actor.terrace-kiln-slinger.capability': (), 'ecology.actor.terrace-kiln-slinger.counterplay': (), 'ecology.actor.estuary-glaze-wolf.name': (), 'ecology.actor.estuary-glaze-wolf.capability': (), 'ecology.actor.estuary-glaze-wolf.counterplay': (), 'ecology.actor.hearth-levee-warden.name': (), 'ecology.actor.hearth-levee-warden.capability': (), 'ecology.actor.hearth-levee-warden.counterplay': (), 'ecology.actor.coast-salt-marten.name': (), 'ecology.actor.coast-salt-marten.capability': (), 'ecology.actor.coast-salt-marten.counterplay': (), 'ecology.actor.forest-briar-slinger.name': (), 'ecology.actor.forest-briar-slinger.capability': (), 'ecology.actor.forest-briar-slinger.counterplay': (), 'ecology.actor.upland-scree-porter.name': (), 'ecology.actor.upland-scree-porter.capability': (), 'ecology.actor.upland-scree-porter.counterplay': (), 'ecology.actor.fen-rush-keeper.name': (), 'ecology.actor.fen-rush-keeper.capability': (), 'ecology.actor.fen-rush-keeper.counterplay': (), 'ecology.actor.gorge-fault-hound.name': (), 'ecology.actor.gorge-fault-hound.capability': (), 'ecology.actor.gorge-fault-hound.counterplay': (), 'ecology.actor.terrace-clay-watch.name': (), 'ecology.actor.terrace-clay-watch.capability': (), 'ecology.actor.terrace-clay-watch.counterplay': (), 'ecology.actor.estuary-brine-cutter.name': (), 'ecology.actor.estuary-brine-cutter.capability': (), 'ecology.actor.estuary-brine-cutter.counterplay': (), 'frontier.elite.fen-marshal.name': (), 'frontier.elite.fen-marshal.capability': (), 'frontier.elite.fen-marshal.counterplay': (), 'frontier.elite.fen-stack.name': (), 'frontier.elite.fen-stack.capability': (), 'frontier.elite.fen-stack.counterplay': (), 'frontier.elite.gorge-cordmaster.name': (), 'frontier.elite.gorge-cordmaster.capability': (), 'frontier.elite.gorge-cordmaster.counterplay': (), 'frontier.elite.gorge-convoy.name': (), 'frontier.elite.gorge-convoy.capability': (), 'frontier.elite.gorge-convoy.counterplay': (), 'frontier.elite.terrace-reeve.name': (), 'frontier.elite.terrace-reeve.capability': (), 'frontier.elite.terrace-reeve.counterplay': (), 'frontier.elite.terrace-shutters.name': (), 'frontier.elite.terrace-shutters.capability': (), 'frontier.elite.terrace-shutters.counterplay': (), 'frontier.elite.estuary-pilot.name': (), 'frontier.elite.estuary-pilot.capability': (), 'frontier.elite.estuary-pilot.counterplay': (), 'frontier.elite.estuary-drum.name': (), 'frontier.elite.estuary-drum.capability': (), 'frontier.elite.estuary-drum.counterplay': (), 'frontier.elite.hearth-lockhand.name': (), 'frontier.elite.hearth-lockhand.capability': (), 'frontier.elite.hearth-lockhand.counterplay': (), 'frontier.elite.coast-wreckward.name': (), 'frontier.elite.coast-wreckward.capability': (), 'frontier.elite.coast-wreckward.counterplay': (), 'frontier.elite.forest-ashstep.name': (), 'frontier.elite.forest-ashstep.capability': (), 'frontier.elite.forest-ashstep.counterplay': (), 'frontier.elite.upland-bellrope.name': (), 'frontier.elite.upland-bellrope.capability': (), 'frontier.elite.upland-bellrope.counterplay': (), 'frontier.elite.fen-pump-train.name': (), 'frontier.elite.fen-pump-train.capability': (), 'frontier.elite.fen-pump-train.counterplay': (), 'frontier.elite.gorge-wedge-crane.name': (), 'frontier.elite.gorge-wedge-crane.capability': (), 'frontier.elite.gorge-wedge-crane.counterplay': (), 'frontier.elite.terrace-slip-wheel.name': (), 'frontier.elite.terrace-slip-wheel.capability': (), 'frontier.elite.terrace-slip-wheel.counterplay': (), 'frontier.elite.estuary-ice-boom.name': (), 'frontier.elite.estuary-ice-boom.capability': (), 'frontier.elite.estuary-ice-boom.counterplay': (), 'ecology.goal.leave_danger': (), 'ecology.goal.avoid_hunters': (), 'ecology.goal.graze': (), 'ecology.goal.preserve_ground': (), 'ecology.goal.alter_route': (), 'ecology.goal.rescue_ally': (), 'ecology.goal.rally_ally': (), 'ecology.goal.retrieve_item': (), 'ecology.goal.hunt_prey': (), 'ecology.goal.oppose_rival': (), 'ecology.goal.escort': (), 'ecology.reason.hazard': (), 'ecology.reason.hunter': (), 'ecology.reason.graze': (), 'ecology.reason.preserve': ('duty',), 'ecology.reason.alter': (), 'ecology.reason.treat': (), 'ecology.reason.rally': (), 'ecology.reason.retrieve': (), 'ecology.reason.rival': ('target', 'ecology'), 'ecology.reason.escort': (), 'ecology.intent.open_door': (), 'ecology.intent.graze': (), 'ecology.intent.move': ('goal', 'reason'), 'ecology.intent.avoid': ('reason',), 'ecology.intent.rival_prepare': ('rival',), 'ecology.intent.rival_disrupted': ('actor',), 'ecology.intent.escort': ('ally',), 'ecology.intent.ally_disrupted': (), 'ecology.intent.aid': ('supply', 'ally'), 'ecology.intent.carry': ('item',), 'ecology.intent.prepare': ('action', 'position'), 'ecology.intent.work': ('action', 'position'), 'ecology.result.leave_hazard': ('actor',), 'ecology.result.leave_hunter': ('actor',), 'ecology.result.move': ('actor', 'goal'), 'ecology.result.rival_prepare': ('actor', 'intent'), 'ecology.result.rival_empty': ('actor',), 'ecology.result.rival_defeat': ('actor', 'rival', 'dropped'), 'ecology.result.rival_strike': ('actor', 'rival', 'health'), 'ecology.result.aid': ('actor', 'intent'), 'ecology.result.carry': ('actor', 'intent'), 'ecology.result.prepare': ('actor', 'intent'), 'ecology.result.water_defeat': ('actor',), 'ecology.result.work': ('actor', 'intent'), 'ecology.record.rival_killed': ('actor', 'rival', 'position', 'ecology'), 'frontier.intent.control_removed': (), 'frontier.intent.withdraw': (), 'frontier.intent.recover': (), 'frontier.intent.depleted': (), 'frontier.intent.hold': ('reason',), 'frontier.intent.wait': (), 'frontier.intent.prepare': ('mode', 'position'), 'frontier.intent.blocked': (), 'frontier.intent.spent': ('mode', 'supplies'), 'frontier.intent.return': (), 'frontier.result.stand_down': ('actor',), 'frontier.result.withdraw': ('actor',), 'frontier.result.recover': ('actor', 'intent'), 'frontier.result.depleted': ('actor',), 'frontier.result.prepare': ('actor', 'intent'), 'frontier.result.blocked': ('actor',), 'frontier.result.no_footing': ('actor',), 'frontier.result.guard_intercept': ('helper', 'amount'), 'frontier.result.surge': (), 'frontier.result.smoulder_quenched': (), 'frontier.result.smoulder': (), 'frontier.result.sever_fall': (), 'frontier.result.sever_weaken': (), 'frontier.result.sever_protected': (), 'frontier.result.convoy': (), 'frontier.result.firing': ('count',), 'frontier.result.shutters': (), 'frontier.result.brine_frozen': (), 'frontier.result.brine': (), 'frontier.result.backwash': (), 'frontier.result.salvage_empty': (), 'frontier.result.salvage': (), 'frontier.result.firebreak': ('broken',), 'frontier.result.counterfall': (), 'frontier.result.siphon': ('moved',), 'frontier.result.lever': (), 'frontier.result.slip': (), 'frontier.result.boom': ('frozen', 'cover'), 'frontier.result.haul_caught': (), 'frontier.result.haul_miss': (), 'frontier.record.install': ('actor', 'x', 'y', 'z'), 'frontier.record.outcome': ('actor', 'status', 'position', 'region', 'courier'), 'frontier.record.reward': ('x', 'y', 'z'), 'frontier.record.return': ('actor', 'commodity'), 'forecast.counter.default': (), 'forecast.timing.after_action': (), 'forecast.timing.recovering': ('steps', 'unit'), 'forecast.timing.visible': (), 'forecast.unit.step': (), 'forecast.unit.steps': (), 'forecast.target.none': (), 'forecast.path.none': (), 'forecast.path.more': (), 'forecast.line.danger': ('actor', 'action'), 'forecast.line.forecast': ('x', 'y', 'z', 'target', 'timing'), 'forecast.line.path': ('path',), 'forecast.line.counter': ('counter',), 'frontier.damage.shutters': (), 'frontier.damage.counterfall': (), 'frontier.status.slip.cause': (), 'frontier.status.slip.consequence': (), 'frontier.status.haul.cause': (), 'frontier.status.haul.consequence': (), 'frontier.provenance.reward': ('actor',), 'frontier.telegraph.surge': ('mode', 'position'), 'frontier.action.surge.spent': ('mode', 'supplies'), 'frontier.telegraph.smoulder': ('mode', 'position'), 'frontier.action.smoulder.spent': ('mode', 'supplies'), 'frontier.telegraph.sever': ('mode', 'position'), 'frontier.action.sever.spent': ('mode', 'supplies'), 'frontier.telegraph.convoy': ('mode', 'position'), 'frontier.action.convoy.spent': ('mode', 'supplies'), 'frontier.telegraph.firing': ('mode', 'position'), 'frontier.action.firing.spent': ('mode', 'supplies'), 'frontier.telegraph.shutters': ('mode', 'position'), 'frontier.action.shutters.spent': ('mode', 'supplies'), 'frontier.telegraph.brine': ('mode', 'position'), 'frontier.action.brine.spent': ('mode', 'supplies'), 'frontier.telegraph.haul': ('mode', 'position'), 'frontier.action.haul.spent': ('mode', 'supplies'), 'frontier.telegraph.backwash': ('mode', 'position'), 'frontier.action.backwash.spent': ('mode', 'supplies'), 'frontier.telegraph.salvage': ('mode', 'position'), 'frontier.action.salvage.spent': ('mode', 'supplies'), 'frontier.telegraph.firebreak': ('mode', 'position'), 'frontier.action.firebreak.spent': ('mode', 'supplies'), 'frontier.telegraph.counterfall': ('mode', 'position'), 'frontier.action.counterfall.spent': ('mode', 'supplies'), 'frontier.telegraph.siphon': ('mode', 'position'), 'frontier.action.siphon.spent': ('mode', 'supplies'), 'frontier.telegraph.lever': ('mode', 'position'), 'frontier.action.lever.spent': ('mode', 'supplies'), 'frontier.telegraph.slip': ('mode', 'position'), 'frontier.action.slip.spent': ('mode', 'supplies'), 'frontier.telegraph.boom': ('mode', 'position'), 'frontier.action.boom.spent': ('mode', 'supplies')}

@dataclass(frozen=True)
class EcologyPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class MagicPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class ProgressionPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class EquipmentPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class PreparationPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class MaterialPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class SanctumPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class SituationPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class CircuitPresentation:
    id: str
    text: str


@dataclass(frozen=True)
class TavernGamesPresentation:
    """Immutable selected-pack wording and visuals for draw and dice."""
    text_slots: tuple[tuple[str, str], ...]
    rank_labels: tuple[str, ...]
    card_ranks: tuple[str, ...]
    card_suits: tuple[str, ...]
    card_edge: str
    card_selected_edge: str
    card_frame: tuple[str, ...]
    dice_faces: tuple[tuple[int, tuple[str, ...]], ...]

    def text(self, slot: str) -> str:
        for identity, value in self.text_slots:
            if identity == slot:
                return value
        raise KeyError(f"unknown tavern-game presentation slot: {slot}")


@dataclass(frozen=True)
class DullestDungeonPresentation:
    """Immutable selected-pack fiction for the in-world tavern game."""
    text_slots: tuple[tuple[str, str], ...]
    sprites: tuple[tuple[str, tuple[str, ...]], ...]
    map_symbols: tuple[tuple[str, str], ...]
    title_art: tuple[str, ...]

    def text(self, slot: str) -> str:
        for identity, value in self.text_slots:
            if identity == slot:
                return value
        raise KeyError(f"unknown Dullest Dungeon presentation slot: {slot}")


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
    quest_service_presentations: tuple[QuestServicePresentation, ...]
    history_presentations: tuple[HistoryPresentation, ...]
    aftermath_presentations: tuple[AftermathPresentation, ...]
    aftermath_openings: tuple[tuple[str, str, str, str], ...]
    aftermath_action_presentations: tuple[AftermathActionPresentation, ...]
    aftermath_result_presentations: tuple[AftermathResultPresentation, ...]
    workline_presentations: tuple[WorklinePresentation, ...]
    interference_presentations: tuple[InterferencePresentation, ...]
    legendary_presentations: tuple[LegendaryPresentation, ...]
    topology_presentations: tuple[TopologyPresentation, ...]
    action_presentations: tuple[ActionPresentation, ...]
    vessel_presentations: tuple[VesselPresentation, ...]
    travel_presentations: tuple[TravelPresentation, ...]
    ship_crisis_presentations: tuple[ShipCrisisPresentation, ...]
    vehicle_presentations: tuple[VehiclePresentation, ...]
    chemistry_presentations: tuple[ChemistryPresentation, ...]
    production_presentations: tuple[ProductionPresentation, ...]
    magic_presentations: tuple[MagicPresentation, ...]
    progression_presentations: tuple[ProgressionPresentation, ...]
    equipment_presentations: tuple[EquipmentPresentation, ...]
    preparation_presentations: tuple[PreparationPresentation, ...]
    material_presentations: tuple[MaterialPresentation, ...]
    sanctum_presentations: tuple[SanctumPresentation, ...]
    situation_presentations: tuple[SituationPresentation, ...]
    circuit_presentations: tuple[CircuitPresentation, ...]
    ecology_presentations: tuple[EcologyPresentation, ...]
    tavern_games: TavernGamesPresentation
    dullest_dungeon: DullestDungeonPresentation
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

    def quest_service_presentation(self, semantic_id: str) -> QuestServicePresentation:
        for presentation in self.quest_service_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown quest service semantic id: {semantic_id}")

    def history_presentation(self, semantic_id: str) -> HistoryPresentation:
        for presentation in self.history_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown history presentation id: {semantic_id}")

    def action_presentation(self, semantic_id: str) -> ActionPresentation:
        for presentation in self.action_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown action presentation id: {semantic_id}")

    def vessel_presentation(self, semantic_id: str) -> VesselPresentation:
        for presentation in self.vessel_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown vessel presentation id: {semantic_id}")

    def travel_presentation(self, semantic_id: str) -> TravelPresentation:
        for presentation in self.travel_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown travel presentation id: {semantic_id}")

    def ship_crisis_presentation(self, semantic_id: str) -> ShipCrisisPresentation:
        for presentation in self.ship_crisis_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown ship crisis presentation id: {semantic_id}")

    def vehicle_presentation(self, semantic_id: str) -> VehiclePresentation:
        for presentation in self.vehicle_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown vehicle presentation id: {semantic_id}")

    def chemistry_presentation(self, semantic_id: str) -> ChemistryPresentation:
        for presentation in self.chemistry_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown chemistry presentation id: {semantic_id}")

    def production_presentation(self, semantic_id: str) -> ProductionPresentation:
        for presentation in self.production_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown production presentation id: {semantic_id}")

    def magic_presentation(self, semantic_id: str) -> MagicPresentation:
        for presentation in self.magic_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown magic presentation id: {semantic_id}")

    def progression_presentation(self, semantic_id: str) -> ProgressionPresentation:
        for presentation in self.progression_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown progression presentation id: {semantic_id}")

    def equipment_presentation(self, semantic_id: str) -> EquipmentPresentation:
        for presentation in self.equipment_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown equipment presentation id: {semantic_id}")

    def preparation_presentation(self, semantic_id: str) -> PreparationPresentation:
        for presentation in self.preparation_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown preparation presentation id: {semantic_id}")

    def material_presentation(self, semantic_id: str) -> MaterialPresentation:
        for presentation in self.material_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown material presentation id: {semantic_id}")

    def sanctum_presentation(self, semantic_id: str) -> SanctumPresentation:
        for presentation in self.sanctum_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown sanctum presentation id: {semantic_id}")

    def situation_presentation(self, semantic_id: str) -> SituationPresentation:
        for presentation in self.situation_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown situation presentation id: {semantic_id}")

    def circuit_presentation(self, semantic_id: str) -> CircuitPresentation:
        for presentation in self.circuit_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown circuit presentation id: {semantic_id}")

    def ecology_presentation(self, semantic_id: str) -> EcologyPresentation:
        for presentation in self.ecology_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown ecology presentation id: {semantic_id}")

    def aftermath_presentation(self, semantic_id: str) -> AftermathPresentation:
        for presentation in self.aftermath_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown aftermath presentation id: {semantic_id}")

    def aftermath_opening(self, region_id: str) -> str:
        for region, text, _notice, _memory in self.aftermath_openings:
            if region == region_id:
                return text
        raise KeyError(f"unknown aftermath opening region: {region_id}")

    def aftermath_opening_text(self, region_id: str, field: str) -> str:
        for region, title, notice, memory in self.aftermath_openings:
            if region == region_id:
                return {"title": title, "notice": notice, "memory": memory}[field]
        raise KeyError(f"unknown aftermath opening region: {region_id}")

    def aftermath_action_presentation(self, semantic_id: str) -> AftermathActionPresentation:
        for presentation in self.aftermath_action_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown aftermath action id: {semantic_id}")

    def aftermath_result_presentation(self, semantic_id: str) -> AftermathResultPresentation:
        for presentation in self.aftermath_result_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown aftermath result id: {semantic_id}")

    def workline_presentation(self, semantic_id: str) -> WorklinePresentation:
        for presentation in self.workline_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown workline presentation id: {semantic_id}")

    def interference_presentation(self, semantic_id: str) -> InterferencePresentation:
        for presentation in self.interference_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown interference presentation id: {semantic_id}")

    def legendary_presentation(self, semantic_id: str) -> LegendaryPresentation:
        for presentation in self.legendary_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown legendary presentation id: {semantic_id}")

    def topology_presentation(self, semantic_id: str) -> TopologyPresentation:
        for presentation in self.topology_presentations:
            if presentation.id == semantic_id:
                return presentation
        raise KeyError(f"unknown topology presentation id: {semantic_id}")


_selected_pack: ContentPack | None = None
_catalogs_loaded = False


def content_pack_presentation_fingerprint(pack: ContentPack | None = None) -> str:
    """Return diagnostic provenance for validated presentation, never mechanics."""
    pack = pack or selected_content_pack()
    data = asdict(pack)
    data.pop("root")
    data.pop("catalog_root")
    data["presentation_contract_format"] = PRESENTATION_CONTRACT_FORMAT
    encoded = json.dumps(
        data, sort_keys=True, separators=(",", ":"), ensure_ascii=True,
        allow_nan=False,
    ).encode("utf-8")
    return sha256(encoded).hexdigest()


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
    if not isinstance(document, dict) or set(document) != {"format_version", "regions", "characters", "roles", "items", "ui", "quests", "services", "history", "aftermath", "worklines", "interference", "legendary", "topology", "actions", "vessel", "travel", "ship_crisis", "vehicle", "chemistry", "production", "magic", "progression", "equipment", "preparations", "materials", "sanctums", "situations", "circuits", "ecology", "tavern_games", "dullest_dungeon"}:
        raise RuntimeError(f"invalid engine content contract at {source}: expected format_version, regions, characters, roles, items, ui, quests, services, history, aftermath, worklines, interference, legendary, topology, actions, vessel, travel, ship_crisis, vehicle, chemistry, production, magic, progression, equipment, preparations, materials, sanctums, situations, circuits, ecology, tavern_games, and dullest_dungeon")
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


def _service_contract_ids() -> tuple[str, ...]:
    source, document = _content_contract_document()
    rows = document["services"]
    if (not isinstance(rows, list) or len(rows) != len(_QUEST_SERVICE_CONTRACT)
            or {row.get("id") for row in rows if isinstance(row, dict)} != set(_QUEST_SERVICE_CONTRACT)
            or any(not isinstance(row, dict) or set(row) != {"id", "engine_id"}
                   or row["engine_id"] != _QUEST_SERVICE_CONTRACT[row["id"]][0] for row in rows)):
        raise RuntimeError(f"invalid engine service content contract at {source}: services must contain exactly stable service ids and engine ids")
    return tuple(row["id"] for row in rows)


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


def _validate_quest_service_template(source: Path, pack_id: str, path: str, text: object, placeholders: tuple[str, ...], *, allow_empty: bool = False, presentation_name: str = "quest") -> str:
    if not isinstance(text, str) or (not allow_empty and not text.strip()):
        raise ContentPackError(f"invalid {presentation_name} presentation for content pack {pack_id!r} at {source}: {path} must be a non-empty string")
    formatter = string.Formatter()
    try:
        fields = []
        for _, field, spec, conversion in formatter.parse(text):
            if field is not None:
                if spec or conversion or re.fullmatch(r"[a-z][a-z0-9_]*", field) is None:
                    raise ValueError("unsupported placeholder")
                fields.append(field)
    except ValueError as exc:
        raise ContentPackError(f"invalid {presentation_name} presentation for content pack {pack_id!r} at {source}: {path} has malformed template: {exc}") from exc
    if set(fields) != set(placeholders) or len(fields) != len(placeholders):
        raise ContentPackError(f"invalid {presentation_name} presentation for content pack {pack_id!r} at {source}: {path} must contain exactly placeholders {', '.join(placeholders) or 'none'}")
    return text


def _quest_service_presentations(root: Path, pack_id: str, rows: object) -> tuple[QuestServicePresentation, ...]:
    source = root / QUEST_PRESENTATION_FILE
    _service_contract_ids()
    if not isinstance(rows, dict) or set(rows) != set(_QUEST_SERVICE_CONTRACT):
        expected = set(_QUEST_SERVICE_CONTRACT)
        actual = set(rows) if isinstance(rows, dict) else set()
        details = []
        if expected - actual:
            details.append("missing required service slots " + ", ".join(sorted(expected - actual)))
        if actual - expected:
            details.append("unknown service slots " + ", ".join(sorted(actual - expected)))
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: " + "; ".join(details or ["services must be an object"]))
    presentations = []
    for semantic_id, (engine_id, label_fields, requirement_fields, expected_results) in _QUEST_SERVICE_CONTRACT.items():
        row = rows[semantic_id]
        required = {"results"} if engine_id == "unavailable" else {"label", "requirement", "results"}
        if engine_id in {"p", "r"}:
            required.add("responses")
        if engine_id == "t":
            required.add("effects")
        if not isinstance(row, dict) or set(row) != required:
            raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: services.{semantic_id} has missing or unknown fields")
        label = "" if engine_id == "unavailable" else _validate_quest_service_template(source, pack_id, f"services.{semantic_id}.label", row["label"], label_fields)
        requirement = "" if engine_id == "unavailable" else _validate_quest_service_template(source, pack_id, f"services.{semantic_id}.requirement", row["requirement"], requirement_fields, allow_empty=True)
        result_rows = row["results"]
        if not isinstance(result_rows, dict) or set(result_rows) != set(expected_results):
            raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: services.{semantic_id}.results has missing or unknown result keys")
        results = tuple((key, _validate_quest_service_template(source, pack_id, f"services.{semantic_id}.results.{key}", result_rows[key], fields)) for key, fields in expected_results.items())
        responses: tuple[tuple[str, tuple[str, str]], ...] = ()
        if engine_id in {"p", "r"}:
            response_rows = row["responses"]
            region_ids = {slot.engine_id for slot in region_contract()}
            if not isinstance(response_rows, dict) or set(response_rows) != region_ids:
                raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: services.{semantic_id}.responses must contain every contracted region")
            parsed = []
            for region_id in sorted(region_ids):
                value = response_rows[region_id]
                if not isinstance(value, str) or not value.strip():
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: services.{semantic_id}.responses.{region_id} must be a non-empty string")
                parsed.append((region_id, (value, value)))
            responses = tuple(parsed)
        effects: tuple[tuple[str, str], ...] = ()
        if engine_id == "t":
            effect_rows = row["effects"]
            expected_effects = {"technique.mill_hearing", "technique.shoreline_measure", "technique.smoke_spoor", "technique.bell_interval"}
            if not isinstance(effect_rows, dict) or set(effect_rows) != expected_effects or any(not isinstance(value, str) or not value.strip() for value in effect_rows.values()):
                raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: services.{semantic_id}.effects must contain exactly contracted techniques")
            effects = tuple((key, effect_rows[key]) for key in sorted(expected_effects))
        presentations.append(QuestServicePresentation(semantic_id, engine_id, label, requirement, results, responses, effects))
    return tuple(presentations)


def _quest_presentations(root: Path, pack_id: str) -> tuple[tuple[QuestPresentation, ...], tuple[QuestServicePresentation, ...]]:
    source = root / QUEST_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    if (not isinstance(document, dict) or set(document) != {"quests", "arc_choices", "arc_results", "services"}
            or not isinstance(document["quests"], dict) or not isinstance(document["arc_choices"], dict)
            or not isinstance(document["arc_results"], dict)):
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: quests, arc_choices, arc_results, and services must be objects")
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
        core_regional = slot.kind == "regional"
        required = ({"title", "lead", "choices", "results"} if core_regional else {"title", "lead", "choices"}) if slot.kind == "regional" else ({"title"} if slot.kind == "arc" else {"evidence_name", "evidence_description"})
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
        text_fields = required - {"choices", "results"}
        if any(not isinstance(row[field], str) or not row[field].strip() for field in text_fields):
            raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path} fields must be non-empty strings")
        choices: tuple[tuple[str, str, str], ...] = ()
        results: tuple[tuple[str, str], ...] = ()
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
            if core_regional:
                regional_result_rows = row["results"]
                if (not isinstance(regional_result_rows, dict) or set(regional_result_rows) != set(expected_choices)
                        or any(not isinstance(value, str) or not value.strip() for value in regional_result_rows.values())):
                    raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path}.results must contain exactly regional branch results")
                results = tuple((choice, regional_result_rows[choice]) for choice in expected_choices)
        arc_choices: tuple[tuple[str, str, str], ...] = ()
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
    return tuple(presentations), _quest_service_presentations(root, pack_id, document["services"])


def _history_presentations(root: Path, pack_id: str) -> tuple[HistoryPresentation, ...]:
    source = root / HISTORY_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid history presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    source_contract, engine_contract = _content_contract_document()
    contract_rows = engine_contract["history"]
    expected_contract = [
        {"id": key, "placeholders": list(placeholders)}
        for key, placeholders in _HISTORY_TEMPLATE_CONTRACT.items()
    ]
    if contract_rows != expected_contract:
        raise RuntimeError(f"invalid engine history content contract at {source_contract}: history does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid history presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_HISTORY_TEMPLATE_CONTRACT):
        missing, unknown = set(_HISTORY_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_HISTORY_TEMPLATE_CONTRACT)
        details = []
        if missing:
            details.append("missing required history keys " + ", ".join(sorted(missing)))
        if unknown:
            details.append("unknown history keys " + ", ".join(sorted(unknown)))
        raise ContentPackError(f"invalid history presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(HistoryPresentation(
        key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)
    ) for key, placeholders in _HISTORY_TEMPLATE_CONTRACT.items())


def _workline_presentations(root: Path, pack_id: str) -> tuple[WorklinePresentation, ...]:
    source = root / WORKLINE_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid workline presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [
        {"id": key, "placeholders": list(placeholders)}
        for key, placeholders in _WORKLINE_TEMPLATE_CONTRACT.items()
    ]
    if engine_contract.get("worklines") != expected_contract:
        raise RuntimeError(f"invalid engine workline content contract at {contract_source}: worklines does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid workline presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_WORKLINE_TEMPLATE_CONTRACT):
        missing, unknown = set(_WORKLINE_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_WORKLINE_TEMPLATE_CONTRACT)
        details = []
        if missing:
            details.append("missing required workline keys " + ", ".join(sorted(missing)))
        if unknown:
            details.append("unknown workline keys " + ", ".join(sorted(unknown)))
        raise ContentPackError(f"invalid workline presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(WorklinePresentation(
        key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)
    ) for key, placeholders in _WORKLINE_TEMPLATE_CONTRACT.items())


def _interference_presentations(root: Path, pack_id: str) -> tuple[InterferencePresentation, ...]:
    source = root / INTERFERENCE_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid interference presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [
        {"id": key, "placeholders": list(placeholders)}
        for key, placeholders in _INTERFERENCE_TEMPLATE_CONTRACT.items()
    ]
    if engine_contract.get("interference") != expected_contract:
        raise RuntimeError(f"invalid engine interference content contract at {contract_source}: interference does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid interference presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_INTERFERENCE_TEMPLATE_CONTRACT):
        missing, unknown = set(_INTERFERENCE_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_INTERFERENCE_TEMPLATE_CONTRACT)
        details = []
        if missing:
            details.append("missing required interference keys " + ", ".join(sorted(missing)))
        if unknown:
            details.append("unknown interference keys " + ", ".join(sorted(unknown)))
        raise ContentPackError(f"invalid interference presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(InterferencePresentation(
        key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)
    ) for key, placeholders in _INTERFERENCE_TEMPLATE_CONTRACT.items())


def _legendary_presentations(root: Path, pack_id: str) -> tuple[LegendaryPresentation, ...]:
    source = root / LEGENDARY_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid legendary presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _LEGENDARY_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("legendary") != expected_contract:
        raise RuntimeError(f"invalid engine legendary content contract at {contract_source}: legendary does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid legendary presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_LEGENDARY_TEMPLATE_CONTRACT):
        missing, unknown = set(_LEGENDARY_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_LEGENDARY_TEMPLATE_CONTRACT)
        details = []
        if missing:
            details.append("missing required legendary keys " + ", ".join(sorted(missing)))
        if unknown:
            details.append("unknown legendary keys " + ", ".join(sorted(unknown)))
        raise ContentPackError(f"invalid legendary presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(LegendaryPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _LEGENDARY_TEMPLATE_CONTRACT.items())



def _action_presentations(root: Path, pack_id: str) -> tuple[ActionPresentation, ...]:
    source = root / ACTION_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid action presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _ACTION_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("actions") != expected_contract:
        raise RuntimeError(f"invalid engine action content contract at {contract_source}: actions does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid action presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_ACTION_TEMPLATE_CONTRACT):
        missing, unknown = set(_ACTION_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_ACTION_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required action keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown action keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid action presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(ActionPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _ACTION_TEMPLATE_CONTRACT.items())


def _vessel_presentations(root: Path, pack_id: str) -> tuple[VesselPresentation, ...]:
    source = root / VESSEL_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid vessel presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _VESSEL_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("vessel") != expected_contract:
        raise RuntimeError(f"invalid engine vessel content contract at {contract_source}: vessel does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid vessel presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_VESSEL_TEMPLATE_CONTRACT):
        missing, unknown = set(_VESSEL_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_VESSEL_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required vessel keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown vessel keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid vessel presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(VesselPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _VESSEL_TEMPLATE_CONTRACT.items())


def _travel_presentations(root: Path, pack_id: str) -> tuple[TravelPresentation, ...]:
    source = root / TRAVEL_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid travel presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _TRAVEL_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("travel") != expected_contract:
        raise RuntimeError(f"invalid engine travel content contract at {contract_source}: travel does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid travel presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_TRAVEL_TEMPLATE_CONTRACT):
        missing, unknown = set(_TRAVEL_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_TRAVEL_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required travel keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown travel keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid travel presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(TravelPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _TRAVEL_TEMPLATE_CONTRACT.items())


def _vehicle_presentations(root: Path, pack_id: str) -> tuple[VehiclePresentation, ...]:
    source = root / VEHICLE_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid vehicle presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _VEHICLE_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("vehicle") != expected_contract:
        raise RuntimeError(f"invalid engine vehicle content contract at {contract_source}: vehicle does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid vehicle presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_VEHICLE_TEMPLATE_CONTRACT):
        missing, unknown = set(_VEHICLE_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_VEHICLE_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required vehicle keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown vehicle keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid vehicle presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(VehiclePresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _VEHICLE_TEMPLATE_CONTRACT.items())


def _ship_crisis_presentations(root: Path, pack_id: str) -> tuple[ShipCrisisPresentation, ...]:
    source = root / SHIP_CRISIS_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid ship crisis presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _SHIP_CRISIS_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("ship_crisis") != expected_contract:
        raise RuntimeError(f"invalid engine ship crisis content contract at {contract_source}: ship_crisis does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid ship crisis presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_SHIP_CRISIS_TEMPLATE_CONTRACT):
        missing, unknown = set(_SHIP_CRISIS_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_SHIP_CRISIS_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required ship crisis keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown ship crisis keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid ship crisis presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(ShipCrisisPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _SHIP_CRISIS_TEMPLATE_CONTRACT.items())


def _chemistry_presentations(root: Path, pack_id: str) -> tuple[ChemistryPresentation, ...]:
    source = root / CHEMISTRY_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid chemistry presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _CHEMISTRY_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("chemistry") != expected_contract:
        raise RuntimeError(f"invalid engine chemistry content contract at {contract_source}: chemistry does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid chemistry presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_CHEMISTRY_TEMPLATE_CONTRACT):
        missing, unknown = set(_CHEMISTRY_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_CHEMISTRY_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required chemistry keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown chemistry keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid chemistry presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(ChemistryPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _CHEMISTRY_TEMPLATE_CONTRACT.items())


def _production_presentations(root: Path, pack_id: str) -> tuple[ProductionPresentation, ...]:
    source = root / PRODUCTION_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid production presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _PRODUCTION_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("production") != expected_contract:
        raise RuntimeError(f"invalid engine production content contract at {contract_source}: production does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid production presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_PRODUCTION_TEMPLATE_CONTRACT):
        missing, unknown = set(_PRODUCTION_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_PRODUCTION_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required production keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown production keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid production presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(ProductionPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders)) for key, placeholders in _PRODUCTION_TEMPLATE_CONTRACT.items())


def _magic_presentations(root: Path, pack_id: str) -> tuple[MagicPresentation, ...]:
    source = root / MAGIC_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid magic presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _MAGIC_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("magic") != expected_contract:
        raise RuntimeError(f"invalid engine magic content contract at {contract_source}: magic does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid magic presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_MAGIC_TEMPLATE_CONTRACT):
        missing, unknown = set(_MAGIC_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_MAGIC_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required magic keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown magic keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid magic presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(MagicPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="magic")) for key, placeholders in _MAGIC_TEMPLATE_CONTRACT.items())


def _progression_presentations(root: Path, pack_id: str) -> tuple[ProgressionPresentation, ...]:
    source = root / PROGRESSION_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid progression presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _PROGRESSION_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("progression") != expected:
        raise RuntimeError(f"invalid engine progression content contract at {contract_source}: progression does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid progression presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_PROGRESSION_TEMPLATE_CONTRACT):
        missing, unknown = set(_PROGRESSION_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_PROGRESSION_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required progression keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown progression keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid progression presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(ProgressionPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="progression")) for key, placeholders in _PROGRESSION_TEMPLATE_CONTRACT.items())

def _equipment_presentations(root: Path, pack_id: str) -> tuple[EquipmentPresentation, ...]:
    source = root / EQUIPMENT_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid equipment presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _EQUIPMENT_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("equipment") != expected:
        raise RuntimeError(f"invalid engine equipment content contract at {contract_source}: equipment does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid equipment presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_EQUIPMENT_TEMPLATE_CONTRACT):
        missing, unknown = set(_EQUIPMENT_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_EQUIPMENT_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required equipment keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown equipment keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid equipment presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(EquipmentPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="equipment")) for key, placeholders in _EQUIPMENT_TEMPLATE_CONTRACT.items())


def _preparation_presentations(root: Path, pack_id: str) -> tuple[PreparationPresentation, ...]:
    source = root / PREPARATION_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid preparation presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _PREPARATION_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("preparations") != expected:
        raise RuntimeError(f"invalid engine preparation content contract at {contract_source}: preparations does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid preparation presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_PREPARATION_TEMPLATE_CONTRACT):
        missing, unknown = set(_PREPARATION_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_PREPARATION_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required preparation keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown preparation keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid preparation presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(PreparationPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="preparation")) for key, placeholders in _PREPARATION_TEMPLATE_CONTRACT.items())


def _material_presentations(root: Path, pack_id: str) -> tuple[MaterialPresentation, ...]:
    source = root / MATERIAL_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid material presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _MATERIAL_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("materials") != expected:
        raise RuntimeError(f"invalid engine material content contract at {contract_source}: materials does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid material presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_MATERIAL_TEMPLATE_CONTRACT):
        missing, unknown = set(_MATERIAL_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_MATERIAL_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required material keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown material keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid material presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(MaterialPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="material")) for key, placeholders in _MATERIAL_TEMPLATE_CONTRACT.items())


def _sanctum_presentations(root: Path, pack_id: str) -> tuple[SanctumPresentation, ...]:
    source = root / SANCTUM_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid sanctum presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _SANCTUM_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("sanctums") != expected:
        raise RuntimeError(f"invalid engine sanctum content contract at {contract_source}: sanctums does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid sanctum presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_SANCTUM_TEMPLATE_CONTRACT):
        missing, unknown = set(_SANCTUM_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_SANCTUM_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required sanctum keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown sanctum keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid sanctum presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(SanctumPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="sanctum")) for key, placeholders in _SANCTUM_TEMPLATE_CONTRACT.items())

def _topology_presentations(root: Path, pack_id: str) -> tuple[TopologyPresentation, ...]:
    source = root / TOPOLOGY_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid topology presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected_contract = [
        {"id": key, "placeholders": list(placeholders)}
        for key, placeholders in _TOPOLOGY_TEMPLATE_CONTRACT.items()
    ]
    if engine_contract.get("topology") != expected_contract:
        raise RuntimeError(f"invalid engine topology content contract at {contract_source}: topology does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid topology presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_TOPOLOGY_TEMPLATE_CONTRACT):
        missing = set(_TOPOLOGY_TEMPLATE_CONTRACT) - set(rows)
        unknown = set(rows) - set(_TOPOLOGY_TEMPLATE_CONTRACT)
        details = []
        if missing:
            details.append("missing required topology keys " + ", ".join(sorted(missing)))
        if unknown:
            details.append("unknown topology keys " + ", ".join(sorted(unknown)))
        raise ContentPackError(f"invalid topology presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(
        TopologyPresentation(
            key,
            _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders),
        )
        for key, placeholders in _TOPOLOGY_TEMPLATE_CONTRACT.items()
    )



def _situation_presentations(root: Path, pack_id: str) -> tuple[SituationPresentation, ...]:
    source = root / SITUATION_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid situation presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _SITUATION_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("situations") != expected:
        raise RuntimeError(f"invalid engine situation content contract at {contract_source}: situations does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid situation presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_SITUATION_TEMPLATE_CONTRACT):
        missing, unknown = set(_SITUATION_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_SITUATION_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required situation keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown situation keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid situation presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(SituationPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="situation")) for key, placeholders in _SITUATION_TEMPLATE_CONTRACT.items())


def _circuit_presentations(root: Path, pack_id: str) -> tuple[CircuitPresentation, ...]:
    source = root / CIRCUIT_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid circuit presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _CIRCUIT_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("circuits") != expected:
        raise RuntimeError(f"invalid engine circuit content contract at {contract_source}: circuits does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid circuit presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_CIRCUIT_TEMPLATE_CONTRACT):
        missing, unknown = set(_CIRCUIT_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_CIRCUIT_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required circuit keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown circuit keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid circuit presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(CircuitPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="circuit")) for key, placeholders in _CIRCUIT_TEMPLATE_CONTRACT.items())



def _ecology_presentations(root: Path, pack_id: str) -> tuple[EcologyPresentation, ...]:
    source = root / ECOLOGY_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid ecology presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(placeholders)} for key, placeholders in _ECOLOGY_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("ecology") != expected:
        raise RuntimeError(f"invalid engine ecology content contract at {contract_source}: ecology does not match engine template contract")
    if not isinstance(document, dict) or set(document) != {"text"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid ecology presentation for content pack {pack_id!r} at {source}: expected text object")
    rows = document["text"]
    if set(rows) != set(_ECOLOGY_TEMPLATE_CONTRACT):
        missing, unknown = set(_ECOLOGY_TEMPLATE_CONTRACT) - set(rows), set(rows) - set(_ECOLOGY_TEMPLATE_CONTRACT)
        details = ([] if not missing else ["missing required ecology keys " + ", ".join(sorted(missing))]) + ([] if not unknown else ["unknown ecology keys " + ", ".join(sorted(unknown))])
        raise ContentPackError(f"invalid ecology presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    return tuple(EcologyPresentation(key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], placeholders, presentation_name="ecology")) for key, placeholders in _ECOLOGY_TEMPLATE_CONTRACT.items())

def _aftermath_presentations(root: Path, pack_id: str) -> tuple[tuple[AftermathPresentation, ...], tuple[tuple[str, str, str, str], ...], tuple[AftermathActionPresentation, ...], tuple[AftermathResultPresentation, ...]]:
    source = root / AFTERMATH_PRESENTATION_FILE
    contract_source, engine_contract = _content_contract_document()
    if engine_contract["aftermath"] != list(_AFTERMATH_CONTRACT):
        raise RuntimeError(f"invalid engine aftermath content contract at {contract_source}: aftermath must contain exactly stable contract slots")
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    if not isinstance(document, dict) or set(document) != {"contracts", "openings", "actions", "results"} or not isinstance(document["contracts"], dict) or not isinstance(document["openings"], dict) or not isinstance(document["actions"], dict) or not isinstance(document["results"], dict):
        raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: expected contracts, openings, actions, and results objects")
    rows = document["contracts"]
    if set(rows) != set(_AFTERMATH_CONTRACT):
        missing, unknown = set(_AFTERMATH_CONTRACT) - set(rows), set(rows) - set(_AFTERMATH_CONTRACT)
        details = []
        if missing:
            details.append("missing required contract slots " + ", ".join(sorted(missing)))
        if unknown:
            details.append("unknown contract slots " + ", ".join(sorted(unknown)))
        raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: " + "; ".join(details))
    presentations = []
    for key in _AFTERMATH_CONTRACT:
        row = rows[key]
        if not isinstance(row, dict) or set(row) != {"title", "cause"} or any(not isinstance(row[field], str) or not row[field].strip() for field in row):
            raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: contracts.{key} must contain non-empty title and cause only")
        placeholders = ("dependency", "stock", "aftermath_title", "site") if key.endswith(".supply") else ("crisis", "branch", "site")
        cause = _validate_quest_service_template(source, pack_id, f"contracts.{key}.cause", row["cause"], placeholders)
        presentations.append(AftermathPresentation(key, row["title"], cause))
    regions = {key.split(".")[2] for key in _AFTERMATH_CONTRACT}
    if (set(document["openings"]) != regions or any(not isinstance(value, dict) or set(value) != {"title", "notice", "memory"}
            or any(not isinstance(text, str) or not text.strip() for text in value.values())
            for value in document["openings"].values())):
        raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: openings must contain every contracted region")
    for region in regions:
        for field, placeholders in {"notice": {"title"}, "memory": {"branch", "region"}}.items():
            _validate_quest_service_template(source, pack_id, f"openings.{region}.{field}", document["openings"][region][field], tuple(sorted(placeholders)))
    action_rows = document["actions"]
    if set(action_rows) != set(_AFTERMATH_ACTION_CONTRACT):
        raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: actions has missing or unknown action slots")
    actions = []
    for action_id, (label_fields, requirement_fields) in _AFTERMATH_ACTION_CONTRACT.items():
        row = action_rows[action_id]
        if not isinstance(row, dict) or set(row) != {"label", "requirements"} or not isinstance(row["requirements"], dict):
            raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: actions.{action_id} must contain label and requirements")
        label = _validate_quest_service_template(source, pack_id, f"actions.{action_id}.label", row["label"], tuple(label_fields))
        if set(row["requirements"]) != set(requirement_fields):
            raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: actions.{action_id}.requirements has missing or unknown reasons")
        requirements = tuple((reason, _validate_quest_service_template(source, pack_id, f"actions.{action_id}.requirements.{reason}", row["requirements"][reason], fields)) for reason, fields in requirement_fields.items())
        actions.append(AftermathActionPresentation(action_id, label, requirements))
    result_rows = document["results"]
    if set(result_rows) != set(_AFTERMATH_RESULT_CONTRACT):
        raise ContentPackError(f"invalid aftermath presentation for content pack {pack_id!r} at {source}: results has missing or unknown result slots")
    results = tuple(AftermathResultPresentation(
        key, _validate_quest_service_template(source, pack_id, f"results.{key}", result_rows[key], fields)
    ) for key, fields in _AFTERMATH_RESULT_CONTRACT.items())
    return tuple(presentations), tuple((region, document["openings"][region]["title"], document["openings"][region]["notice"], document["openings"][region]["memory"]) for region in sorted(regions)), tuple(actions), results


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



def _dd_template_fields(value: str, path: str) -> frozenset[str]:
    try:
        fields = {field for _, field, _, _ in string.Formatter().parse(value) if field}
    except ValueError as exc:
        raise ValueError(f"{path} has malformed template: {exc}") from exc
    allowed = {"actor", "action", "card", "side", "kind", "biome", "amount", "resource", "target", "department", "operation", "room", "names", "encounter", "mission", "enemy", "source", "destination"}
    if fields - allowed or any("." in field or "[" in field for field in fields):
        raise ValueError(f"{path} has unsupported placeholder")
    return frozenset(fields)


def _dd_flatten(value: Any, path: str = "") -> list[tuple[str, str]]:
    if isinstance(value, dict):
        result: list[tuple[str, str]] = []
        for key, child in value.items():
            if not isinstance(key, str) or not key:
                raise ValueError("presentation keys must be non-empty strings")
            result.extend(_dd_flatten(child, f"{path}.{key}" if path else key))
        return result
    if not isinstance(value, str) or not value:
        raise ValueError(f"{path} must be a non-empty string")
    _dd_template_fields(value, path)
    return [(path, value)]


def _dd_document(path: Path, pack_id: str) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid Dullest Dungeon presentation for content pack {pack_id!r} at {path}: {exc}") from exc


_TAVERN_GAMES_TEMPLATE_CONTRACT = {
    'draw.opening': (),
    'draw.ante': ('ante', 'maximum'),
    'draw.settle': ('winners', 'pot', 'rank'),
    'draw.fold': ('player',),
    'draw.call': ('player', 'action', 'amount'),
    'draw.raise': ('player', 'action', 'amount'),
    'draw.first_bet_closed': (),
    'draw.exchange': ('player', 'count'),
    'draw.final_bet': (),
    'draw.uncontested': (),
    'draw.phase.bet1': (),
    'draw.phase.draw': (),
    'draw.phase.bet2': (),
    'draw.phase.complete': (),
    'dice.opening': (),
    'dice.settle.win': ('winners', 'score', 'prize'),
    'dice.settle.tie': ('winners', 'score'),
    'dice.bust': ('player',),
    'dice.bank': ('player', 'points'),
    'dice.roll': ('player', 'first', 'second', 'total'),
    'draw.ui.rules': (),
    'draw.ui.practice': ('maximum',),
    'draw.ui.credit': ('credit', 'stakes'),
    'draw.ui.wagered': (),
    'draw.ui.free_practice': (),
    'draw.ui.invite': ('selected',),
    'draw.ui.seats_filled': (),
    'draw.ui.empty': (),
    'draw.ui.showing': ('first', 'last', 'total'),
    'draw.ui.title.complete': (),
    'draw.ui.title.active': (),
    'draw.ui.hand': ('number', 'phase'),
    'draw.ui.pot': ('pot', 'credit', 'stakes'),
    'draw.ui.dealer': ('dealer', 'acting'),
    'draw.ui.none': (),
    'draw.ui.folded': (),
    'draw.ui.paid': ('paid', 'status'),
    'draw.ui.in': (),
    'draw.ui.out': (),
    'draw.ui.outcome': ('outcome', 'winners', 'net'),
    'draw.ui.win': (),
    'draw.ui.clear': (),
    'draw.ui.selected': ('count',),
    'draw.ui.draw_controls': (),
    'draw.ui.call': ('due', 'raises'),
    'draw.ui.bet_controls': (),
    'draw.ui.disclaimer': ('maximum',),
    'dice.ui.title.lobby': ('bartender',),
    'dice.ui.rules.1': (),
    'dice.ui.rules.2': (),
    'dice.ui.rules.3': (),
    'dice.ui.purse': ('purse', 'prize'),
    'dice.ui.credit': ('credit',),
    'dice.ui.invite': ('selected',),
    'dice.ui.empty': ('bartender',),
    'dice.ui.seats_filled': (),
    'dice.ui.controls': (),
    'dice.ui.title.complete': (),
    'dice.ui.round': ('round', 'rounds', 'purse', 'credit'),
    'dice.ui.busts': ('count',),
    'dice.ui.hidden': (),
    'dice.ui.turn_pot': ('total',),
    'dice.ui.rolls': ('count', 'maximum'),
    'dice.ui.double': (),
    'dice.ui.outcome': ('outcome', 'winners', 'prize'),
    'dice.ui.win': (),
    'dice.ui.result': (),
    'dice.ui.clear': (),
    'dice.ui.acting': ('player',),
    'dice.ui.disclaimer': ('bartender',),
}

def _tavern_games_presentation(root: Path, pack_id: str) -> TavernGamesPresentation:
    source = root / TAVERN_GAMES_PRESENTATION_FILE
    try:
        document = json.loads(source.read_text(encoding="utf-8"), object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (OSError, ValueError, RecursionError) as exc:
        raise ContentPackError(f"invalid tavern-game presentation for content pack {pack_id!r} at {source}: {exc}") from exc
    contract_source, engine_contract = _content_contract_document()
    expected = [{"id": key, "placeholders": list(values)} for key, values in _TAVERN_GAMES_TEMPLATE_CONTRACT.items()]
    if engine_contract.get("tavern_games") != expected:
        raise RuntimeError(f"invalid engine tavern-game content contract at {contract_source}")
    if not isinstance(document, dict) or set(document) != {"text", "rank_labels", "cards", "dice"} or not isinstance(document["text"], dict):
        raise ContentPackError(f"invalid tavern-game presentation for content pack {pack_id!r} at {source}: expected text, rank_labels, cards, and dice")
    rows = document["text"]
    if set(rows) != set(_TAVERN_GAMES_TEMPLATE_CONTRACT):
        raise ContentPackError(f"invalid tavern-game presentation for content pack {pack_id!r} at {source}: missing or unknown text slots")
    text_slots = tuple((key, _validate_quest_service_template(source, pack_id, f"text.{key}", rows[key], fields, presentation_name="tavern-game")) for key, fields in _TAVERN_GAMES_TEMPLATE_CONTRACT.items())
    ranks = document["rank_labels"]
    cards, dice = document["cards"], document["dice"]
    if (not isinstance(ranks, list) or len(ranks) != 9 or any(not isinstance(row, str) or not row.strip() for row in ranks)
            or not isinstance(cards, dict) or set(cards) != {"ranks", "suits", "edge", "selected_edge", "frame"}
            or not isinstance(cards["ranks"], list) or len(cards["ranks"]) != 13 or not isinstance(cards["suits"], list) or len(cards["suits"]) != 4
            or any(not isinstance(row, str) or not row for row in [*cards["ranks"], *cards["suits"], cards["edge"], cards["selected_edge"]])
            or not isinstance(cards["frame"], list) or len(cards["frame"]) != 7 or any(not isinstance(row, str) or not row for row in cards["frame"])
            or not isinstance(dice, dict) or set(dice) != {str(value) for value in range(1, 7)}
            or any(not isinstance(rows, list) or len(rows) != 5 or any(not isinstance(row, str) or not row for row in rows) for rows in dice.values())):
        raise ContentPackError(f"invalid tavern-game visuals for content pack {pack_id!r} at {source}")
    return TavernGamesPresentation(text_slots, tuple(ranks), tuple(cards["ranks"]), tuple(cards["suits"]), cards["edge"], cards["selected_edge"], tuple(cards["frame"]), tuple((int(key), tuple(dice[key])) for key in sorted(dice, key=int)))


def _dullest_dungeon_presentation(root: Path, pack_id: str) -> DullestDungeonPresentation:
    directory = root / DULLEST_DUNGEON_DIRECTORY
    text_path = directory / DULLEST_DUNGEON_TEXT_FILE
    visuals_path = directory / DULLEST_DUNGEON_VISUALS_FILE
    text = _dd_document(text_path, pack_id)
    default = _dd_document(_package_path("content_packs", "default", DULLEST_DUNGEON_DIRECTORY, DULLEST_DUNGEON_TEXT_FILE), "default")
    try:
        slots = _dd_flatten(text)
        expected_slots = _dd_flatten(default)
        expected = {key for key, _ in expected_slots}
        actual = {key for key, _ in slots}
        if actual != expected:
            raise ValueError("text has missing or unknown slots")
        expected_fields = {key: _dd_template_fields(value, key) for key, value in expected_slots}
        for key, value in slots:
            if _dd_template_fields(value, key) != expected_fields[key]:
                raise ValueError(f"text.{key} has missing or unknown placeholders")
        visuals = _dd_document(visuals_path, pack_id)
        expected_visuals = _dd_document(_package_path("content_packs", "default", DULLEST_DUNGEON_DIRECTORY, DULLEST_DUNGEON_VISUALS_FILE), "default")
        if set(visuals) != set(expected_visuals) or set(visuals) != {"office_sprites", "expedition_map_symbols", "title_art"}:
            raise ValueError("visuals has missing or unknown sections")
        sprites = visuals["office_sprites"]
        expected_sprites = expected_visuals["office_sprites"]
        if not isinstance(sprites, dict) or set(sprites) != set(expected_sprites):
            raise ValueError("visuals.office_sprites has missing or unknown roles")
        frozen_sprites = []
        for role, lines in sprites.items():
            if not isinstance(lines, list) or len(lines) != 5 or any(not isinstance(line, str) or not line.isascii() or not line.isprintable() or len(line) > 9 for line in lines):
                raise ValueError(f"visuals.office_sprites.{role} must contain five printable ASCII rows")
            frozen_sprites.append((role, tuple(lines)))
        def symbols(value: Any, path: str = "") -> list[tuple[str, str]]:
            if isinstance(value, dict):
                return [pair for key, child in value.items() for pair in symbols(child, f"{path}.{key}" if path else key)]
            if not isinstance(value, str) or len(value) != 1 or not value.isascii() or not value.isprintable():
                raise ValueError(f"visuals.expedition_map_symbols.{path} must be one printable ASCII symbol")
            return [(path, value)]
        visual_symbols = symbols(visuals["expedition_map_symbols"])
        if {key for key, _ in visual_symbols} != {key for key, _ in symbols(expected_visuals["expedition_map_symbols"])}:
            raise ValueError("visuals.expedition_map_symbols has missing or unknown slots")
        title = visuals["title_art"]
        if not isinstance(title, list) or len(title) != 6 or any(not isinstance(line, str) or not line.isascii() or not line.isprintable() or len(line) > 72 for line in title):
            raise ValueError("visuals.title_art must contain six printable ASCII rows up to 72 columns")
        return DullestDungeonPresentation(tuple(sorted(slots)), tuple(sorted(frozen_sprites)), tuple(sorted(visual_symbols)), tuple(title))
    except ValueError as exc:
        raise ContentPackError(f"invalid Dullest Dungeon presentation for content pack {pack_id!r} at {directory}: {exc}") from exc


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
    quests, services = _quest_presentations(root, pack_id)
    history = _history_presentations(root, pack_id)
    worklines = _workline_presentations(root, pack_id)
    interference = _interference_presentations(root, pack_id)
    legendary = _legendary_presentations(root, pack_id)
    topology = _topology_presentations(root, pack_id)
    actions = _action_presentations(root, pack_id)
    vessel = _vessel_presentations(root, pack_id)
    travel = _travel_presentations(root, pack_id)
    ship_crisis = _ship_crisis_presentations(root, pack_id)
    vehicle = _vehicle_presentations(root, pack_id)
    chemistry = _chemistry_presentations(root, pack_id)
    production = _production_presentations(root, pack_id)
    magic = _magic_presentations(root, pack_id)
    progression = _progression_presentations(root, pack_id)
    equipment = _equipment_presentations(root, pack_id)
    preparations = _preparation_presentations(root, pack_id)
    materials = _material_presentations(root, pack_id)
    sanctums = _sanctum_presentations(root, pack_id)
    situations = _situation_presentations(root, pack_id)
    circuits = _circuit_presentations(root, pack_id)
    ecology = _ecology_presentations(root, pack_id)
    tavern_games = _tavern_games_presentation(root, pack_id)
    dullest_dungeon = _dullest_dungeon_presentation(root, pack_id)
    aftermath, aftermath_openings, aftermath_actions, aftermath_results = _aftermath_presentations(root, pack_id)
    return ContentPack(
        pack_id, display_name, format_version, root, catalog_root,
        _region_presentations(root, pack_id), characters, roles, items, ui, quests, services, history, aftermath, aftermath_openings, aftermath_actions, aftermath_results, worklines, interference, legendary, topology, actions, vessel, travel, ship_crisis, vehicle, chemistry, production, magic, progression, equipment, preparations, materials, sanctums, situations, circuits, ecology, tavern_games, dullest_dungeon, household_template,
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

_ACTION_TEMPLATE_CONTRACT.update({
    "intent.brace.retreated": (), "intent.brace.checked": (),
    "intent.weapon.recover": ("weapon",),
    "intent.elite.floodgate.sluice_telegraph": ("x", "y"),
    "intent.elite.reeve.cover_telegraph": ("x", "y"),
    "intent.elite.tracker.resin_telegraph": ("x", "y"),
    "intent.elite.bellward.floor_telegraph": ("x", "y", "z"),
    "intent.elite.false_bell.rockfall_telegraph": ("x", "y"),
    "intent.machinery.sweep_telegraph": ("lane",),
    "intent.controller.net_telegraph": ("x", "y"),
    "intent.ranged.reloading": ("weapon", "remaining"),
    "intent.ranged.reloaded": ("weapon",),
    "intent.ai.intercept": (), "intent.ai.patrol": (), "intent.ai.return": (),
    "intent.ai.approach": (), "intent.ai.flank": (), "intent.ai.seek_elevation": (),
    "intent.ranged.tracks_sound": (), "intent.ranged.must_reload": ("weapon",),
    "intent.ranged.aim_telegraph": ("weapon", "x", "y"),
    "intent.advance.fast": (), "intent.advance.normal": (),
    "combat.elite.floodgate.leverage_denied": (),
    "combat.elite.floodgate.telegraph": ("threat", "intent"),
    "combat.elite.floodgate.sluice_source": (), "combat.elite.floodgate.safe": (),
    "combat.elite.reeve.leverage_denied": (),
    "combat.elite.reeve.telegraph": ("threat", "intent"),
    "combat.elite.reeve.sling_source": (), "combat.elite.reeve.safe": (),
    "combat.elite.tide_chain.leverage_denied": (), "combat.elite.tide_chain.source": (),
    "combat.elite.tide_chain.safe": (), "combat.elite.tracker.leverage_denied": (),
    "combat.elite.tracker.resin_result": (), "combat.elite.ash_cloak.leverage_denied": (),
    "combat.elite.ash_cloak.smoke": ("threat", "intent"),
    "combat.elite.bellward.leverage_denied": (),
    "combat.elite.bellward.telegraph": ("threat", "intent"),
    "combat.elite.bellward.floor_break": ("fall",), "combat.elite.bellward.floor_safe": (),
    "combat.elite.false_bell.leverage_denied": (),
    "combat.elite.false_bell.rockfall_source": (), "combat.elite.false_bell.safe": (),
    "combat.machinery.telegraph": ("threat", "lane"),
    "combat.machinery.crown_wheel_source": (), "combat.machinery.sweep_source": (),
    "combat.machinery.safe": ("lane",),
    "combat.threat.ranged_source": ("threat", "weapon"),
    "combat.threat.animal_charge_source": ("threat",),
    "combat.threat.melee_source": ("threat",),
    "combat.threat.attack_warning.reach": ("threat",),
    "combat.threat.attack_warning.melee": ("threat",),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "combat.defeat.loss.porter_watch": (),
    "combat.defeat.loss.protected_cargo": ("protection", "kept"),
    "combat.defeat.loss.cargo": ("cargo",), "combat.defeat.loss.items": ("items",),
    "combat.defeat.loss.preserved_cargo": ("item",),
    "combat.defeat.objective_failed": ("courier", "region"),
    "combat.defeat.memory.died": ("courier", "hazard"),
    "combat.defeat.no_successor": ("text", "loss"),
    "combat.defeat.successor": ("text", "loss", "successor"),
    "combat.defeat.memory.escaped": ("courier",),
    "combat.defeat.injured": ("text", "loss"),
    "combat.damage.field_care": (),
    "combat.brace.reason.weapon": (), "combat.brace.reason.not_visible": (),
    "combat.brace.reason.cross_levels": (), "combat.brace.reason.minimum_range": ("minimum",),
    "combat.brace.reason.too_far": (), "combat.brace.reason.ready": (),
    "combat.brace.outcome.defeated": (), "combat.brace.outcome.checked": ("damage",),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "combat.goal.break_contact.weapon_lost": (), "combat.goal.cover_retreat": (),
    "combat.status.resin.cause": (), "combat.status.resin.consequence": (),
    "combat.status.net.cause": (), "combat.status.net.consequence": (),
    "combat.status.lane.marked.cause": (), "combat.status.lane.marked.consequence": (),
    "combat.status.lane.cover.cause": (), "combat.status.lane.cover.consequence": (),
    "combat.threat.animal_mud.memory": ("courier", "threat"),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "combat.machinery.lane.default": (), "combat.machinery.lane.outer": (),
    "combat.machinery.lane.inner": (),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "intent.disrupted.billhook": (), "intent.disrupted.spear_spacing": (),
    "intent.dazed.cudgel": (), "intent.disrupted.pike_brace": (),
    "intent.pinned.crossbar": (), "intent.entangled.net": (),
    "intent.disrupted.hooked_shaft": (), "intent.dazed.sling": (),
    "intent.route.removed": (),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "combat.attack.weapon.crossbow": (), "combat.attack.weapon.longbow": (),
    "combat.attack.weapon.sling": (), "combat.attack.weapon.heavy_crossbow": (),
    "combat.attack.weapon.javelins": (), "combat.attack.weapon.weighted_net": (),
    "combat.attack.weapon.staff_sling": (), "combat.attack.weapon.hooked_javelin": (),
    "combat.attack.weapon.handgonne": (), "combat.attack.weapon.throwing_axe": (),
    "combat.attack.weapon.billhook": (), "combat.attack.weapon.spear": (),
    "combat.attack.weapon.cudgel": (), "combat.attack.weapon.staff": (),
    "combat.attack.weapon.hand_axe": (), "combat.attack.weapon.pike": (),
    "combat.attack.weapon.paired_knives": (), "combat.attack.weapon.war_hammer": (),
    "combat.attack.weapon.boar_spear": (),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "combat.attack.weapon.anchor_fluke": (),
    "combat.attack.weapon.arming_sword": (),
    "combat.attack.weapon.boat_hook": (),
    "combat.attack.weapon.chain_hook": (),
    "combat.attack.weapon.estoc": (),
    "combat.attack.weapon.felling_axe": (),
    "combat.attack.weapon.flanged_mace": (),
    "combat.attack.weapon.forked_pike": (),
    "combat.attack.weapon.glaive": (),
    "combat.attack.weapon.long_knife": (),
    "combat.attack.weapon.pollaxe": (),
    "combat.attack.weapon.pot_sling": (),
    "combat.attack.weapon.quarterstaff": (),
    "combat.attack.weapon.reed_sickle": (),
    "combat.attack.weapon.shield_and_hanger": (),
    "combat.attack.weapon.spade": (),
    "combat.attack.weapon.war_flail": (),
})

_ACTION_TEMPLATE_CONTRACT.update({
    "combat.attack.legendary_weapon": ("legend", "weapon"),
    "combat.attack.outcome.defeated": (), "combat.attack.outcome.drove_off": (),
})
_ACTION_TEMPLATE_CONTRACT.update({'combat.intent.skill.weapon_bind': (), 'combat.intent.skill.line_intercept': ()})
