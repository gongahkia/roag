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
        ("edge-measure", "Edge measure", "blade strikes leave a guarded stance"),
        ("guard-feint", "Guard feint", "blade strikes gain one harm against protectors"),
        ("slip-cut", "Slip cut", "blade strikes gain one harm after a guarded step"),
        ("weapon-bind", "Weapon bind", "blade strikes spoil a marked enemy aim"),
        ("riposte", "Riposte", "guarded blade strikes press one extra morale"),
        ("river-duelist", "River duelist", "blade strikes gain one harm against wavering morale"),
    )),
    "reach": ("Reach and impact", (
        ("measured-stance", "Measured stance", "reach arms gain one maximum pace"),
        ("countercharge", "Countercharge", "reach strikes gain one harm against engaged enemies"),
        ("haft-breaker", "Haft breaker", "reach strikes chip timber support beneath targets"),
        ("hook-haul", "Hook haul", "reach strikes pull the target one open pace"),
        ("line-intercept", "Line intercept", "reach strikes bind one targeted enemy action"),
        ("ferryman-wall", "Ferryman's wall", "reach strikes leave a guarded stance"),
    )),
    "bows": ("Bows and throwing", (
        ("sighted-draw", "Sighted draw", "bows gain one pace of maximum range"),
        ("quick-nock", "Quick nock", "bows fire without a separate aim action"),
        ("shaft-recovery", "Shaft recovery", "recover one spent physical throw"),
        ("called-shot", "Called shot", "bow shots gain one harm against elites"),
        ("wind-hold", "Wind hold", "rain no longer spoils a prepared bow shot"),
        ("moving-volley", "Moving volley", "a safe step retains bow aim"),
    )),
    "gunworks": ("Gunworks", (
        ("charge-handling", "Charge handling", "gun shots gain one harm when fully loaded"),
        ("dry-load", "Dry load", "wet weather no longer spoils gun aim"),
        ("braced-tube", "Braced tube", "guarded gun lanes gain one maximum pace"),
        ("vent-care", "Vent care", "gun reloads need one fewer guarded action"),
        ("smoke-shaping", "Smoke shaping", "gun shots also veil their target cell with smoke"),
        ("payload-master", "Payload master", "gun shots gain one harm against rigid protection"),
    )),
    "devices": ("Devices and fieldcraft", (
        ("safe-throw", "Safe throw", "device throws gain one maximum pace"),
        ("scatter-bank", "Scatter bank", "spread a smoke payload into a second cell"),
        ("delayed-fuse", "Delayed fuse", "thunder bombs become warned next-action mineral flashes"),
        ("adhesive-coat", "Adhesive coat", "pitch and resin bombs leave two more fuel"),
        ("line-trap", "Line trap", "resin bombs also bind adjacent enemies"),
        ("controlled-chain", "Controlled chain", "thunder bombs press one more adjacent morale"),
    )),
    "spellcraft": ("Spellcraft", (
        ("attunement", "Attunement", "increase the personal mana reserve by two"),
        ("elemental-shape", "Elemental shape", "learn smoke, salt, ice and ash spell forms"),
        ("ward-script", "Ward script", "learn support, ward, cleanse and lime spell forms"),
        ("veiling", "Veiling", "learn decoy, quiet, pull and snare spell forms"),
        ("echo-binding", "Echo binding", "learn sweep, storm, wash and iron spell forms"),
        ("spell-weave", "Spell weave", "learn four high-cost classic spell forms"),
    )),
    "smithing": ("Smithing and fabrication", (
        ("tool-care", "Tool care", "improve a physical repair by five condition"),
        ("fuel-husbandry", "Fuel husbandry", "one charcoal lot is spared in a prepared smelt"),
        ("bloom-sorting", "Bloom sorting", "a prepared smelt yields one extra iron billet"),
        ("armour-fitting", "Armour fitting", "fitted armour gains five condition after installation"),
        ("gun-assembly", "Gun assembly", "guns can be fabricated at a physical forge"),
        ("masterwork", "Masterwork", "newly made personal weapons gain one harm"),
    )),
    "alchemy": ("Alchemy and medicine", (
        ("substance-sense", "Substance sense", "healing drafts restore one extra health"),
        ("safe-decant", "Safe decant", "one ingredient gives two flask measures when room permits"),
        ("field-triage", "Field triage", "healing drafts restore one more health"),
        ("controlled-distil", "Controlled distil", "remove one chosen flask reagent at a still"),
        ("antitoxin", "Antitoxin", "breath tonic also clears salt and lime exposure"),
        ("catalyst-brewing", "Catalyst brewing", "attunement drafts restore one extra mana"),
    )),
    "navigation": ("Navigation and seamanship", (
        ("route-reading", "Route reading", "gain one sight radius in known regional ground"),
        ("weather-eye", "Weather eye", "gain one sight radius in fog and rain"),
        ("load-balance", "Load balance", "carry four more physical weight"),
        ("current-rescue", "Current rescue", "winter water no longer adds a new chill"),
        ("station-repair", "Station repair", "vessel refit installations need one fewer action"),
        ("deep-pilotage", "Deep pilotage", "charted route legs need one fewer travel action"),
    )),
    "diplomacy": ("Diplomacy and trade", (
        ("careful-terms", "Careful terms", "negotiated groups include one more listener"),
        ("price-sense", "Price sense", "merchant purchases cost one less credit"),
        ("mediation", "Mediation", "tavern mediation improves relationship settlement"),
        ("teaching", "Teaching", "pass a practiced node to another courier"),
        ("work-order", "Work order", "delegate one physical production job"),
        ("guild-broker", "Guild broker", "witnessed regional supply deliveries earn one extra credit"),
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


def weapon_family(name: str | None) -> str | None:
    from .expanded_weapons import ARSENAL

    if name in ARSENAL:
        return ARSENAL[name].family
    if name in {"crossbow", "longbow", "sling", "heavy crossbow", "staff sling", "javelins", "hooked javelin"}:
        return "bow"
    if name == "handgonne":
        return "gun"
    if name in {"pot sling", "weighted net"}:
        return "device"
    if name in {"hand axe", "paired knives", "throwing axe", "long knife", "arming sword", "glaive", "felling axe", "reed sickle", "war flail"}:
        return "blade"
    if name in {"billhook", "spear", "pike", "boar spear", "cudgel", "staff", "war hammer", "forked pike", "spade", "shield and hanger", "pollaxe", "boat hook", "flanged mace", "estoc", "quarterstaff", "anchor fluke", "chain hook"}:
        return "reach"
    return None


def apply_weapon_skills(state: GameState, target, damage: int, *, weapon_name: str | None = None) -> tuple[int, str, bool]:
    """Apply cross-trainable physical branches at the ordinary strike boundary."""
    from .actions import _step_toward
    from .inventory import create_item
    from .materials import ensure_cell, material_at

    person = state.courier
    family = weapon_family(weapon_name or state.weapon)
    if person is None or family is None:
        return damage, "", False
    notes = []
    guarding_before = state.guarded_step
    guards_response = False
    if family == "blade":
        if has_node(person, "guard-feint") and target.role == "protector":
            damage += 1
            notes.append("guard feint")
        if has_node(person, "slip-cut") and guarding_before:
            damage += 1
            notes.append("slip cut")
        if has_node(person, "weapon-bind") and target.aimed_at is not None:
            target.aimed_at = None
            target.intent = "marked aim spoiled by a blade bind"
            notes.append("weapon bind")
        if has_node(person, "riposte") and guarding_before:
            target.morale -= 1
            notes.append("riposte pressure")
        if has_node(person, "river-duelist") and target.morale <= 2:
            damage += 1
            notes.append("duelist's finish")
        if has_node(person, "edge-measure"):
            state.guarded_step = guards_response = True
            notes.append("edge measure guard")
    elif family in {"reach", "impact"}:
        if has_node(person, "countercharge") and target.turn > 0:
            damage += 1
            notes.append("countercharge")
        if has_node(person, "haft-breaker") and material_at(state, target.position) == "timber":
            cell = ensure_cell(state, target.position)
            if cell:
                cell.support = max(0, cell.support - 1)
                notes.append("timber chipped")
        if has_node(person, "hook-haul"):
            target.position = _step_toward(state, target, state.position)
            notes.append("hook haul")
        if has_node(person, "line-intercept"):
            target.intent = "entangled by a trained intercept; loses a turn"
            notes.append("line intercepted")
        if has_node(person, "ferryman-wall"):
            state.guarded_step = guards_response = True
            notes.append("ferryman guard")
    elif family == "bow":
        if has_node(person, "called-shot") and target.elite:
            damage += 1
            notes.append("called shot")
        if has_node(person, "shaft-recovery"):
            from .inventory import AMMUNITION_ITEMS, WEAPON_AMMUNITION

            ammunition = WEAPON_AMMUNITION.get(weapon_name or state.weapon)
            if ammunition in {"arrows", "bolts", "heavy bolts", "javelins"}:
                shaft = create_item(state, AMMUNITION_ITEMS[ammunition], "recoverable shaft from learned shot", location="ground")
                shaft.region_id, shaft.ground_position = state.spatial_id, target.position
                notes.append("shaft falls recoverably")
    elif family == "gun":
        if has_node(person, "charge-handling"):
            damage += 1
            notes.append("measured charge")
        if has_node(person, "payload-master") and (target.elite or target.role == "protector" or target.profile == "machinery"):
            damage += 1
            notes.append("matched payload")
        if has_node(person, "smoke-shaping"):
            cell = ensure_cell(state, target.position)
            if cell:
                cell.smoke = max(2, cell.smoke)
                notes.append("target veiled by shot smoke")
    return damage, "; ".join(notes), guards_response


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
