"""The tavern's office fantasy, adapted from the imported card catalog."""

from __future__ import annotations

from dataclasses import dataclass
from functools import lru_cache

from .content import load_catalog


# These are job titles in the fictional Company of Necessary Copies, not Jomon professions.
OFFICE_ROLES = {
    "warden": "Middle Manager", "engineer": "Facilities Engineer",
    "medic": "Wellness Officer", "scout": "Mailroom Runner",
    "breacher": "Meeting Scheduler", "psion": "Consultant",
    "quartermaster": "Procurement Clerk", "operative": "Compliance Agent",
    "biologist": "Breakroom Botanist", "synth": "Office Printer",
    "duelist": "Corporate Lawyer", "artillerist": "Presentation Lead",
    "chaplain": "Morale Officer", "hacker": "Spreadsheet Analyst",
    "pilot": "Elevator Operator", "cryonaut": "Climate Technician",
    "horticulturist": "Office Gardener", "foundryman": "Copier Mechanic",
    "reactor_saint": "Budget Controller", "mycologist": "Mold Inspector",
    "diver": "Basement Archivist", "stormcaller": "Outage Coordinator",
    "archivist": "Records Clerk", "voidwalker": "Remote Worker",
    "bonewright": "Ergonomics Rep",
}

OFFICE_ACTIONS = {
    "warden": "Write-Up|Firm Handshake|Escalation|Closed-Door Chat|Desk Reassignment|Mandatory Meeting|Hold the Agenda|Last Friday|Counter-Signature|Chain of Command|Marked for Review|Executive Override",
    "engineer": "Hot Desk|Reroute Cable|Rolling Chair|Overtime Switch|Printer Jam|Locked Drawer|Patch the Chair|Feedback Form|Foam Divider|Anchor the Desk|Unplug and Replug|Emergency Extension",
    "medic": "Wellness Check|Fresh Water|Breathing Break|Triage Queue|Coffee Boost|Cold Compress|Group Sharing|Approved Pill|Clean the Wound|First-Aid Cart|Calm Memo|All-Staff Check-In",
    "scout": "Urgent Post|Read the Room|Hallway Shortcut|Fresh Stamps|Priority Mail|Loose Leaflets|Back Stair|Perfect Address|Thread the Cubicles|Rolling Cart|Floor Directory|Impossible Delivery",
    "breacher": "Book the Room|Agenda Blast|Cancel Meeting|Kick Open Door|Privacy Screen|Sprint to Room|Standing Meeting|Door Wedge|Double Booking|Soft Launch|Calendar Redline",
    "psion": "Thought Leadership|Dread Survey|Neural Note|Synergy Circle|Clarity Deck|Feedback Spiral|Future Roadmap|Slide Shear|Shared Pain Point|Echo Chamber|Mass Brainstorm",
    "quartermaster": "Supply Drawer|Snack Pack|Stapler Cache|Reallocate Budget|Covering Memo|Contingency Plan|Inventory Audit|Planned Obsolescence|Emergency Biscuits|Bounty Allocation|Empty the Cupboard",
    "operative": "Quiet Complaint|Exploit Loophole|Privacy Screen|Side Channel|Wiretap Meeting|Termination Form|Vanish into HR|Painted Exit|Dead Drop Box|Septic Rumor|Ghost Employee",
    "biologist": "Herbal Tea|Culture Dish|Aloe Gel|Specimen Survey|Spore Cloud|Adaptive Snack|Sample Collection|Incubation Tray|Shock Caffeine|Tagged Lunch|Controlled Experiment",
    "synth": "Toner Fist|Hard Copy|Power Strip|Guardian Printer|Paper Feed|Reboot Queue|Mirror Finish|Toner Surge|Paper Shield|Charge Divider|Error Matrix|Parallel Print",
    "duelist": "Legal Notice|Sidestep Clause|Counterclaim|Opening Statement|Challenge Filing|Perfect Form|Appeal to Policy|Tempo Objection|Passing Remark|Red Pen|Death by Footnote",
    "artillerist": "Opening Slide|Spotter Pointer|Brace Projector|Scatter Chart|Overpressure Pitch|Kill the Meeting|Shatter Graph|Slide Storm|Range of Estimates|Breach the Deck|Danger Close-Up",
    "chaplain": "Moral Censure|Team Chant|Absolve Error|Martyr Shift|Last Respects|Office Fervor|Carry the Burden|Bless the Room|Votive Mug|Scar Hymn|Shared Apology",
    "hacker": "Test Cell|Jam Formula|Ghost Sheet|Master Worksheet|Nested Formula|Fork Tab|Hidden Column|Read the Formula|Too Many Columns|Broken Macro|Pivot at Noon",
    "pilot": "Elevator Shot|Evasive Stop|Formation Ride|Slingshot Lift|Broadside Doors|Emergency Floor|Crosswind Shaft|Wingover Lobby|Gravity Assist|Escort Ride|Full Ascent",
    "cryonaut": "Ice Machine|Thermal Mug|Cold Call|Whiteout Memo|Preserve Lunch|Brittle Point|Cold Storage|Flash Freeze|Thermostat War|Stasis Meeting|Frosty Reception|Absolute Chill",
    "horticulturist": "Thorny Note|Cubicle Canopy|Sap Graft|Root the Desk|Sunward Window|Grafted Thorns|Pruning Cut|Verdant Cycle|Seed Barbs|Thorn Crown|Pollinator Break|Overgrowth",
    "foundryman": "Copier Hammer|Slag Tray|Cast-Off Toner|Temper Plate|Overheat Copier|Hammer Fall|Quench Ink|Molten Guard|Annealed Wall|White Heat|Slag Press|Pour the Mold",
    "reactor_saint": "Budget Lash|Containment Ledger|Quarterly Forecast|Critical Spend|Depreciation Prayer|Chain Approval|Emergency Freeze|Expense Receipt|Prompt Approval|Lead Accountant|Shortfall Index|Year-End Gospel",
    "mycologist": "Mold Needle|Fruiting Fridge|Shared Spores|Sleepy Vent|Molt Carpet|Rot Harvest|Symbiotic Mesh|Spore Exchange|Burst Cap|Mold Transfer|Tracking Spores|Fruiting Apocalypse",
    "diver": "Harpoon Staple|Pressure Suit|Decompress File|Undertow Shelf|Drop Ballast|Crushing Depth|Lifeline String|Pressure Lock|Keelhaul Cart|Buddy Breath|Signal Buoy|Blackwater Archive",
    "stormcaller": "Loose Outlet|Surge Protector|Chain Outage|Drafty Vent|Ground Wire|Fuse Path|Extension Cord|Aftershock|Forked Cable|Breaker Shield|Static Carpet|Flickering Light",
    "archivist": "Paper Cut|Index Weakness|Redaction|Footnote|Restore Record|Cross Reference|Errata|Forbidden Index|Citation Chain|Second Draft|Case Study|Rewrite Outcome",
    "voidwalker": "Unread Message|Delayed Reply|Muted Meeting|Out of Office|Timezone Inversion|Calendar Collapse|Shift Swap|Nothing to Attach|Blinking Status|No Reception|Blank Screen|Last-Minute Reply",
    "bonewright": "Chair Blade|Ossify Posture|Field Splint|Rattling Desk|Last Scaffold|Bone Saw|Cage of Ribs|Borrowed Marrow|Splinter Volley|Bone Lattice|Catalogued Bone|Walking Ossuary",
}

DEPARTMENTS = (
    "Records Retention", "Human Resources", "Facilities", "Accounts Payable",
    "The Mailroom", "The Breakroom", "Compliance", "Procurement",
    "Information Technology", "The Boardroom", "The Basement",
)

INFUSION_NAMES = (
    "Sticky Note", "Shred After Reading", "Long Staple", "Front Desk Pass",
    "Back Office Pass", "Sick-Day Form", "Empty Chair Policy", "Morning Memo",
    "Carbon Copy", "Follow-Up Email", "Rolling Chair Rebate", "Hand Sanitizer",
    "First Impression", "Red Ink", "Burden Transfer", "Deadline Panic",
)
DOCTRINE_NAMES = (
    "Calendar Tetris", "Marked for Review", "Office Flu", "Desk Rotation",
    "Wellness Inversion", "Flowchart Prison", "Inbox Zero", "Exit Interview",
    "Narrow Presentation", "Cover the Vacancy", "Triage Rhythm",
)


@dataclass(frozen=True)
class OfficeCard:
    id: str
    name: str
    role: str
    cost: int
    target: str
    effects: tuple[dict, ...]
    description: str


def _effect_text(effect: dict) -> str:
    op, amount = effect["op"], effect.get("amount", 0)
    status = effect.get("status", "marked")
    office_status = {
        "marked": "flagged", "wound": "paper cut", "weak": "frazzled",
        "vulnerable": "under review", "focus": "caffeinated",
        "riposte": "reply-all", "dodge": "out to lunch", "stun": "meeting hold",
    }.get(status, status)
    return {
        "damage": f"deal {amount} paperwork pressure",
        "block": f"gain {amount} cover",
        "heal": f"restore {amount} composure",
        "stress": f"add {amount} stress",
        "move": f"reposition up to {max(1, min(3, abs(amount)))} desks",
        "guard": "guard a colleague",
        "status": f"apply {status} ({office_status})",
        "draw": f"draw {amount} card(s)",
        "discard": f"discard {amount} card(s)",
        "energy": f"gain {amount} energy",
        "cleanse": "clear a condition",
    }[op]


@lru_cache(maxsize=1)
def office_catalog() -> tuple[dict[str, dict], dict[str, OfficeCard]]:
    source = load_catalog()
    roles = {}
    cards = {}
    for hero_id, hero in source.heroes.items():
        if hero_id not in OFFICE_ROLES:
            continue
        roles[hero_id] = {
            "name": OFFICE_ROLES[hero_id], "max_hp": 12 + hero["max_hp"] // 7,
            "starter_deck": list(hero["starter_deck"]),
            "signature": f"A {OFFICE_ROLES[hero_id].lower()} with a deeply implausible authority over paperwork.",
        }
        originals = [card for card in source.cards.values() if card["hero"] == hero_id]
        titles = OFFICE_ACTIONS[hero_id].split("|")
        if len(titles) != len(originals):
            raise ValueError(f"office action count differs for {hero_id}")
        for original, title in zip(originals, titles):
            effects = tuple(dict(effect) for effect in original["effects"])
            description = "; ".join(_effect_text(effect) for effect in effects).capitalize() + "."
            cards[original["id"]] = OfficeCard(
                original["id"], title, hero_id, original["cost"], original["target"],
                effects, description,
            )
    if len(roles) != 25 or len(cards) != 290:
        raise ValueError("the complete office roster must be available")
    return roles, cards
