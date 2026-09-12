"""Personal, cross-trainable courier skills and finite milestone points."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Person


@dataclass(frozen=True)
class SkillNode:
    id: str
    branch: str
    name: str
    parents: tuple[str, ...]
    effect: str


BRANCHES = {
    "blades": ("Blades", (
        ("edge-measure", "Edge measure", "guard after a blade strike"),
        ("guard-feint", "Guard feint", "strip one guarded stance before cutting"),
        ("slip-cut", "Slip cut", "make a short guarded step after cutting"),
        ("weapon-bind", "Weapon bind", "interrupt a prepared close weapon"),
        ("riposte", "Riposte", "counter a committed close attack while guarding"),
        ("river-duelist", "River duelist", "chain one safe reposition after a counter"),
    )),
    "reach": ("Reach and impact", (
        ("measured-stance", "Measured stance", "hold one more pace of close threat"),
        ("countercharge", "Countercharge", "punish an approaching enemy's committed lane"),
        ("haft-breaker", "Haft breaker", "weaken adjacent timber or protection with impact"),
        ("hook-haul", "Hook haul", "draw a target across one open pace"),
        ("line-intercept", "Line intercept", "stop an enemy crossing a guarded lane"),
        ("ferryman-wall", "Ferryman's wall", "hold a guarded zone against two enemies"),
    )),
    "bows": ("Bows and throwing", (
        ("sighted-draw", "Sighted draw", "gain one pace of aimed range"),
        ("quick-nock", "Quick nock", "shorten one bow reload commitment"),
        ("shaft-recovery", "Shaft recovery", "recover one spent physical throw"),
        ("called-shot", "Called shot", "target an exposed body location"),
        ("wind-hold", "Wind hold", "keep one aimed lane in adverse wind"),
        ("moving-volley", "Moving volley", "retain one ranged aim across a safe step"),
    )),
    "gunworks": ("Gunworks", (
        ("charge-handling", "Charge handling", "inspect charge and wet-powder risk"),
        ("dry-load", "Dry load", "shield one loaded charge from rain"),
        ("braced-tube", "Braced tube", "extend a rested gun lane by one pace"),
        ("vent-care", "Vent care", "shorten one gun reload commitment"),
        ("smoke-shaping", "Smoke shaping", "place powder smoke toward the target"),
        ("payload-master", "Payload master", "select a compatible physical gun payload"),
    )),
    "devices": ("Devices and fieldcraft", (
        ("safe-throw", "Safe throw", "preview a device's full danger footprint"),
        ("scatter-bank", "Scatter bank", "spread a smoke payload into a second cell"),
        ("delayed-fuse", "Delayed fuse", "place a warned one-turn device"),
        ("adhesive-coat", "Adhesive coat", "make a poured coating last one more action"),
        ("line-trap", "Line trap", "restrain one crossing enemy with a physical line"),
        ("controlled-chain", "Controlled chain", "limit a reaction to a chosen safe boundary"),
    )),
    "spellcraft": ("Spellcraft", (
        ("attunement", "Attunement", "increase the personal mana reserve by two"),
        ("elemental-shape", "Elemental shape", "place an elemental spell on a visible cell"),
        ("ward-script", "Ward script", "protect a marked ally or material cell"),
        ("veiling", "Veiling", "make one visible decoy or concealment effect"),
        ("echo-binding", "Echo binding", "sustain a short-lived summoned echo"),
        ("spell-weave", "Spell weave", "join two learned effects at their summed mana cost"),
    )),
    "smithing": ("Smithing and fabrication", (
        ("tool-care", "Tool care", "improve a physical repair by five condition"),
        ("fuel-husbandry", "Fuel husbandry", "save one fuel in a prepared smelt"),
        ("bloom-sorting", "Bloom sorting", "recover one usable fragment from slag"),
        ("armour-fitting", "Armour fitting", "seat a crafted fitting without extra wear"),
        ("gun-assembly", "Gun assembly", "fit one replaceable gun component"),
        ("masterwork", "Masterwork", "choose one bounded property for a made weapon"),
    )),
    "alchemy": ("Alchemy and medicine", (
        ("substance-sense", "Substance sense", "inspect known material properties"),
        ("safe-decant", "Safe decant", "retain one measure during a careful transfer"),
        ("field-triage", "Field triage", "prepare a basic finite wound dressing"),
        ("controlled-distil", "Controlled distil", "separate one contaminant at a still"),
        ("antitoxin", "Antitoxin", "neutralise one disclosed poison condition"),
        ("catalyst-brewing", "Catalyst brewing", "stabilise a volatile uncanny mixture"),
    )),
    "navigation": ("Navigation and seamanship", (
        ("route-reading", "Route reading", "inspect one more route consequence"),
        ("weather-eye", "Weather eye", "see one additional weather warning"),
        ("load-balance", "Load balance", "carry four more physical weight"),
        ("current-rescue", "Current rescue", "secure one adjacent water recovery"),
        ("station-repair", "Station repair", "shorten one vessel work action"),
        ("deep-pilotage", "Deep pilotage", "open one surveyed route shortcut"),
    )),
    "diplomacy": ("Diplomacy and trade", (
        ("careful-terms", "Careful terms", "include one more witness in a negotiation"),
        ("price-sense", "Price sense", "inspect both market inputs and buyers"),
        ("mediation", "Mediation", "improve a witnessed social settlement"),
        ("teaching", "Teaching", "pass a practiced node to another courier"),
        ("work-order", "Work order", "delegate one physical production job"),
        ("guild-broker", "Guild broker", "settle one additional regional production term"),
    )),
}

NODES: dict[str, SkillNode] = {}
for branch_id, (branch_name, definitions) in BRANCHES.items():
    ids = [definition[0] for definition in definitions]
    for index, (node_id, name, effect) in enumerate(definitions):
        parents = () if index == 0 else (ids[0],) if index in (1, 2) else (
            (ids[1],) if index == 3 else (ids[2],) if index == 4 else (ids[3], ids[4])
        )
        NODES[node_id] = SkillNode(node_id, branch_id, name, parents, effect)

ROLE_ROOTS = {
    "bargemaster": ("route-reading", "careful-terms"),
    "pilot": ("route-reading", "sighted-draw"),
    "factor": ("careful-terms", "route-reading"),
    "carpenter": ("tool-care", "safe-throw"),
    "guard": ("edge-measure", "measured-stance"),
    "healer": ("substance-sense", "attunement"),
    "tide runner": ("route-reading", "safe-throw"),
    "netwright": ("measured-stance", "tool-care"),
    "charcoal scout": ("sighted-draw", "safe-throw"),
    "resin healer": ("substance-sense", "attunement"),
    "quarry climber": ("measured-stance", "tool-care"),
    "ridge ward": ("sighted-draw", "route-reading"),
}

REGIONS = ("hearthford", "greywash", "greenwold", "whitecairn", "dunmire", "rillscar", "marlbank", "frostmere")
MILESTONES = {
    *(f"return:{region}" for region in REGIONS),
    *(f"trade:{region}" for region in REGIONS),
    *(f"production:{region}" for region in REGIONS),
    *(f"combat:{family}" for family in ("blades", "reach", "bows", "gunworks", "devices", "spellcraft")),
    *(f"craft:{domain}" for domain in ("portable", "smelting", "smithing", "alchemy", "brewing")),
    *(f"social:{context}" for context in ("mediation", "contract", "market", "teaching", "delegation")),
}


def has_node(person: Person | None, node_id: str) -> bool:
    return bool(person and node_id in person.skill_nodes)


def seed_role_nodes(person: Person) -> None:
    for node_id in ROLE_ROOTS.get(person.role, ()):
        if node_id not in person.skill_nodes:
            person.skill_nodes.append(node_id)
            if node_id == "attunement":
                from .magic import grant_tier

                grant_tier(person, node_id)


def buy_node(state: GameState, node_id: str) -> tuple[bool, str]:
    person = state.courier
    node = NODES.get(node_id)
    if person is None or node is None:
        return False, "Choose an offered skill node for a living courier."
    if node_id in person.skill_nodes:
        return False, f"{person.name} already knows {node.name}."
    if any(parent not in person.skill_nodes for parent in node.parents):
        return False, f"{node.name} needs {', '.join(NODES[parent].name for parent in node.parents)}."
    if person.skill_points < 1:
        return False, "No unspent milestone point remains."
    person.skill_points -= 1
    person.skill_nodes.append(node_id)
    if node_id in {"attunement", "elemental-shape", "ward-script", "veiling", "echo-binding", "spell-weave"}:
        from .magic import grant_tier

        grant_tier(person, node_id)
    state.remember(f"{person.name} learns {node.name}: {node.effect}.")
    return True, f"{person.name} learns {node.name}: {node.effect}."


def record_milestone(state: GameState, milestone: str) -> bool:
    person = state.courier
    if person is None or milestone not in MILESTONES or milestone in person.skill_milestones:
        return False
    person.skill_milestones.append(milestone)
    person.skill_points += 1
    state.add_message(f"{person.name} earns one skill point: {milestone.replace(':', ' / ')}.")
    return True


def teach_node(state: GameState, recipient_id: str, node_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .people import person_by_id
    from .vessel import current_area
    from .world import distance

    teacher = state.courier
    recipient = person_by_id(state, recipient_id)
    schedule = state.actor_schedules.get(recipient_id)
    if teacher is None or recipient is None or recipient.id == teacher.id or not has_node(teacher, "teaching"):
        return False, "A courier with the Teaching skill must choose another named adult."
    if state.location != "jomon" or not schedule or schedule.area != current_area(state) or distance(state.position, schedule.position) > 1:
        return False, "Teach face to face within one pace aboard Jomon."
    if node_id not in teacher.skill_nodes or node_id in recipient.skill_nodes:
        return False, "The teacher must know a node the recipient has not learned."
    if recipient.taught_nodes >= 2:
        return False, "This adult has already inherited two taught nodes."
    if any(parent not in recipient.skill_nodes for parent in NODES[node_id].parents):
        return False, "The recipient needs the listed prerequisite nodes first."
    recipient.skill_nodes.append(node_id)
    recipient.taught_nodes += 1
    if node_id in {"attunement", "elemental-shape", "ward-script", "veiling", "echo-binding", "spell-weave"}:
        from .magic import grant_tier

        grant_tier(recipient, node_id)
    record_milestone(state, "social:teaching")
    _advance_world(state, steps=2)
    message = f"{teacher.name} teaches {NODES[node_id].name} to {recipient.name}; one of two inherited lessons is used."
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def journals_at_hand(state: GameState):
    return [item for item in state.items if item.kind == "skill journal" and item.lesson_node in NODES
            and (item.location == "locker" or item.location == "pack" and item.owner_id == state.active_courier_id)]


def _at_gathering(state: GameState) -> bool:
    from .world import base_tile

    return state.location == "jomon" and state.jomon_space == "vessel" and state.voyage_status != "active" and base_tile(state, state.position) == "T"


def write_journal(state: GameState, node_id: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item, sync_legacy_load
    from .production import _consume_input, input_count

    person = state.courier
    if not _at_gathering(state) or person is None:
        return False, "Write a lesson at Jomon's physical common deck while moored."
    if node_id not in person.skill_nodes or node_id in person.journal_nodes or len(person.journal_nodes) >= 3:
        return False, "Choose one learned, unwritten node; each adult can preserve three."
    if input_count(state, "commodity:paper") < 1:
        return False, "A written lesson consumes one physical paper lot."
    transaction = InventoryTransaction.begin(state)
    _consume_input(state, "commodity:paper", 1)
    item = create_item(state, "skill journal", f"{person.name}'s witnessed lesson")
    item.lesson_node = node_id
    if not auto_place(state, item.id, "locker"):
        transaction.cancel(state)
        return False, "Jomon's locker needs one free place for the journal; no paper was spent."
    person.journal_nodes.append(node_id)
    sync_legacy_load(state)
    _advance_world(state, steps=2)
    message = f"{person.name} writes {NODES[node_id].name} into physical journal {item.id}; one paper lot is spent."
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def study_journal(state: GameState, item_id: str) -> tuple[bool, str]:
    from .actions import _advance_world

    person = state.courier
    item = next((item for item in journals_at_hand(state) if item.id == item_id), None)
    if not _at_gathering(state) or person is None or item is None:
        return False, "Study a physical journal at Jomon's common deck."
    node_id = item.lesson_node
    if node_id in person.skill_nodes:
        return False, "This courier already knows the journal's lesson."
    if person.taught_nodes >= 2:
        return False, "This courier has already inherited two taught or written nodes."
    if any(parent not in person.skill_nodes for parent in NODES[node_id].parents):
        return False, "Study the prerequisite lessons before this journal."
    person.skill_nodes.append(node_id)
    person.taught_nodes += 1
    if node_id in {"attunement", "elemental-shape", "ward-script", "veiling", "echo-binding", "spell-weave"}:
        from .magic import grant_tier

        grant_tier(person, node_id)
    record_milestone(state, "social:teaching")
    _advance_world(state, steps=2)
    message = f"{person.name} studies {NODES[node_id].name} from {item.id}; the physical journal remains for another adult."
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def validate_skill_journals(state: GameState) -> None:
    for item in state.items:
        if item.kind == "skill journal" and item.lesson_node not in NODES:
            raise ValueError("skill journal lacks a valid written node")
        if item.kind != "skill journal" and item.lesson_node is not None:
            raise ValueError("unrelated item carries a skill lesson")


def validate_skills(person: Person) -> None:
    from .chemistry import REACTIONS
    from .magic import SPELLS, SPELL_TIERS

    earned_spells = {spell for node_id in person.skill_nodes for spell in SPELL_TIERS.get(node_id, ())}
    formulas = {name for name, _ in REACTIONS.values()}
    if (type(person.skill_points) is not int or not 0 <= person.skill_points <= len(MILESTONES) + 2
            or not isinstance(person.skill_nodes, list)
            or len(person.skill_nodes) != len(set(person.skill_nodes))
            or any(node_id not in NODES for node_id in person.skill_nodes)
            or len(person.skill_milestones) != len(set(person.skill_milestones))
            or any(key not in MILESTONES for key in person.skill_milestones)
            or type(person.taught_nodes) is not int or not 0 <= person.taught_nodes <= 2
            or len(person.journal_nodes) > 3
            or len(person.journal_nodes) != len(set(person.journal_nodes))
            or any(node_id not in person.skill_nodes for node_id in person.journal_nodes)
            or not isinstance(person.known_spells, list)
            or len(person.known_spells) != len(set(person.known_spells))
            or any(spell not in SPELLS or spell not in earned_spells for spell in person.known_spells)
            or not isinstance(person.known_formulas, list)
            or len(person.known_formulas) != len(set(person.known_formulas))
            or any(formula not in formulas for formula in person.known_formulas)):
        raise ValueError("invalid personal skill tree")
    if any(any(parent not in person.skill_nodes for parent in NODES[node_id].parents)
           for node_id in person.skill_nodes):
        raise ValueError("skill node lacks its prerequisites")


if len(NODES) != 60 or len(MILESTONES) != 40:
    raise RuntimeError("skill tree needs sixty nodes and forty finite milestones")
