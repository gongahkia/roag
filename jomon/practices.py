"""Sixteen bounded learned practices selected by stable engine IDs."""
from __future__ import annotations
from dataclasses import dataclass
from .catalog import CatalogError, PRACTICE_SECTIONS, load_catalog
from .progression_presentation import practice_description, practice_display_name, progression_format, progression_text
from .state import GameState

@dataclass(frozen=True)
class Practice:
    id: str
    region_id: str
    source: str
    effect: str
    @property
    def name(self) -> str: return practice_display_name(self.id)
    @property
    def description(self) -> str: return practice_description(self.id)

_CATALOG=load_catalog('practices.json', PRACTICE_SECTIONS); _rows=_CATALOG['practices']
if (not isinstance(_rows,list) or len(_rows)!=16 or any(not isinstance(row,list) or len(row)!=4 or any(not isinstance(v,str) or not v for v in row) for row in _rows) or len({row[0] for row in _rows})!=len(_rows)):
    raise CatalogError('practices.json has invalid practice rows')
PRACTICES={row[0]:Practice(*row) for row in _rows}
# Bundled-default compatibility only; never selected-pack wording.
LEGACY_PRACTICE_IDS={
 'bank-water cadence':'practice.bank_water_cadence','field-rill measure':'practice.field_rill_measure','wreck-title hold':'practice.wreck_title_hold','span-watch stance':'practice.span_watch_stance','ash-refuge breathing':'practice.ash_refuge_breathing','island porter relay':'practice.island_porter_relay','ridge-sounding line':'practice.ridge_sounding_line','winter-braid reading':'practice.winter_braid_reading','siltgate hand':'practice.siltgate_hand','ebb beacon watch':'practice.ebb_beacon_watch','living firebreak':'practice.living_firebreak','honest stair breath':'practice.honest_stair_breath','peat brace seating':'practice.peat_brace_seating','two-span withdrawal':'practice.two_span_withdrawal','seed-clay tread':'practice.seed_clay_tread','thaw-net recovery':'practice.thaw_net_recovery',
}

def stable_practice_id(value: str) -> str:
    if value in PRACTICES or value.startswith(("practice.personal:", "technique.")):
        return value
    if value.startswith("seasoned "):
        return "practice.personal:" + value.removeprefix("seasoned ")
    return LEGACY_PRACTICE_IDS.get(value, value)

def learned_practice_ids(person) -> set[str]:
    return {stable_practice_id(value) for value in person.learned_techniques}

_network=_CATALOG['network_contacts'];_aftermath=_CATALOG['aftermath_regions']
if (not isinstance(_network,dict) or len(_network)!=8 or any(not isinstance(k,str) or not k.startswith('network-contact-') or not isinstance(v,str) or v not in PRACTICES for k,v in _network.items()) or not isinstance(_aftermath,dict) or len(_aftermath)!=8 or any(not isinstance(k,str) or not isinstance(v,str) or v not in PRACTICES for k,v in _aftermath.items())):
 raise CatalogError('practices.json has invalid sources')
NETWORK_CONTACT_PRACTICE=_network; AFTERMATH_REGION_PRACTICE=_aftermath

def learned_effects(state: GameState)->set[str]:
 known=learned_practice_ids(state.courier) if state.courier else set()
 return {p.effect for pid,p in PRACTICES.items() if pid in known}
def has_effect(state,effect:str)->bool:return effect in learned_effects(state)
def teach_network_practice(state,contact_id):
 pid=NETWORK_CONTACT_PRACTICE.get(contact_id)
 if pid is None or state.courier is None:return False,progression_text('progression.practice.network.unavailable')
 name=practice_display_name(pid)
 if pid in learned_practice_ids(state.courier):return False,progression_format('progression.practice.network.already',courier=state.courier.name,practice=name)
 state.courier.learned_techniques.append(pid); desc=practice_description(pid)
 state.remember(progression_format('progression.practice.network.learned',courier=state.courier.name,practice=name,contact=contact_id,description=desc))
 return True,progression_format('progression.practice.network.result',courier=state.courier.name,practice=name,description=desc)
def teach_aftermath_practice(state,region_id):
 if state.courier is None:return ''
 pid=AFTERMATH_REGION_PRACTICE[region_id]
 if pid in learned_practice_ids(state.courier):return ''
 state.courier.learned_techniques.append(pid); text=progression_format('progression.practice.aftermath.learned',courier=state.courier.name,practice=practice_display_name(pid),description=practice_description(pid));state.remember(text.strip());return text
def validate_practices():
 if len(PRACTICES)!=16 or len({p.effect for p in PRACTICES.values()})!=16:raise ValueError('practices must provide sixteen distinct mechanical effects')
 if set(NETWORK_CONTACT_PRACTICE.values())|set(AFTERMATH_REGION_PRACTICE.values())!=set(PRACTICES):raise ValueError('every practice needs one bounded production source')
validate_practices()
