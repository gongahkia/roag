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
HISTORY_PRESENTATION_FILE = "history_text.json"
AFTERMATH_PRESENTATION_FILE = "aftermath_text.json"
WORKLINE_PRESENTATION_FILE = "worklines.json"
INTERFERENCE_PRESENTATION_FILE = "interference_text.json"
LEGENDARY_PRESENTATION_FILE = "legendary_text.json"
TOPOLOGY_PRESENTATION_FILE = "topology_text.json"
ACTION_PRESENTATION_FILE = "action_text.json"
VESSEL_PRESENTATION_FILE = "vessel_text.json"

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
    "combat.intent.activate.elite": ("mode", "charges"), "combat.intent.activate.prey": (), "combat.intent.activate.predator": (), "combat.intent.activate.duty": ("duty",),
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
}

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
    if not isinstance(document, dict) or set(document) != {"format_version", "regions", "characters", "roles", "items", "ui", "quests", "services", "history", "aftermath", "worklines", "interference", "legendary", "topology", "actions", "vessel"}:
        raise RuntimeError(f"invalid engine content contract at {source}: expected format_version, regions, characters, roles, items, ui, quests, services, history, aftermath, worklines, interference, legendary, topology, actions, and vessel")
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


def _validate_quest_service_template(source: Path, pack_id: str, path: str, text: object, placeholders: tuple[str, ...], *, allow_empty: bool = False) -> str:
    if not isinstance(text, str) or (not allow_empty and not text.strip()):
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path} must be a non-empty string")
    formatter = string.Formatter()
    try:
        fields = []
        for _, field, spec, conversion in formatter.parse(text):
            if field is not None:
                if spec or conversion or re.fullmatch(r"[a-z][a-z0-9_]*", field) is None:
                    raise ValueError("unsupported placeholder")
                fields.append(field)
    except ValueError as exc:
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path} has malformed template: {exc}") from exc
    if set(fields) != set(placeholders) or len(fields) != len(placeholders):
        raise ContentPackError(f"invalid quest presentation for content pack {pack_id!r} at {source}: {path} must contain exactly placeholders {', '.join(placeholders) or 'none'}")
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
            expected_effects = {"mill hearing", "shoreline measure", "smoke spoor", "bell interval"}
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
    aftermath, aftermath_openings, aftermath_actions, aftermath_results = _aftermath_presentations(root, pack_id)
    return ContentPack(
        pack_id, display_name, format_version, root, catalog_root,
        _region_presentations(root, pack_id), characters, roles, items, ui, quests, services, history, aftermath, aftermath_openings, aftermath_actions, aftermath_results, worklines, interference, legendary, topology, actions, vessel, household_template,
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
